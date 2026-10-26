using System;
using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Defense;
using Archery.Difficulty;
using Archery.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Enemies
{
    /// <summary>
    /// Un ennemi (GDD, section 10). Par défaut, il marche avec un NavMeshAgent vers la tour et la frappe,
    /// ou attaque le joueur s'il est au sol tout près. Meurt quand son <see cref="Health"/> tombe à 0.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Avec un <see cref="EnemyRangedAttack"/>, il tire des projectiles au lieu de frapper (tireur).</item>
    /// <item>Avec un <see cref="EnemyBehaviour"/>, c'est lui qui gère le déplacement et l'attaque (volant) :
    /// le NavMeshAgent n'est alors pas nécessaire.</item>
    /// </list>
    /// L'Animator est optionnel. S'il existe, il peut utiliser les paramètres Speed (float),
    /// Attack, Hit et Die (triggers) ; ceux qui manquent sont simplement ignorés.
    /// À son apparition, l'ennemi applique la difficulté (PV, vitesse, dégâts, taille de la tête)
    /// et le renforcement de la vague en cours (<see cref="EnemyScaling"/>).
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class Enemy : MonoBehaviour
    {
        enum State
        {
            Moving,
            Attacking,
            Fleeing,
            Dead,
        }

        static readonly int k_SpeedId = Animator.StringToHash("Speed");
        static readonly int k_AttackId = Animator.StringToHash("Attack");
        static readonly int k_HitId = Animator.StringToHash("Hit");
        static readonly int k_DieId = Animator.StringToHash("Die");

        static readonly List<Enemy> s_Alive = new List<Enemy>();

        [SerializeField]
        EnemyDefinition m_Definition;

        [Tooltip("Animator du modèle. Laisser vide pour le chercher dans les enfants.")]
        [SerializeField]
        Animator m_Animator;

        [Tooltip("Temps (s) avant que le corps disparaisse.")]
        [SerializeField]
        float m_CorpseLifetime = 4f;

        [Tooltip("Multiplie la vitesse quand l'ennemi s'enfuit à la fin de la vague.")]
        [SerializeField]
        float m_FleeSpeedMultiplier = 1.5f;

        [Tooltip("Hauteur (m) du centre du corps au-dessus du pivot de l'ennemi (0 pour un volant) : " +
                 "visée automatique des flèches, éclairs, explosions.")]
        [SerializeField]
        float m_CenterHeight = 1.2f;

        [SerializeField]
        AudioClip m_AttackClip;

        [SerializeField]
        AudioClip m_DeathClip;

        NavMeshAgent m_Agent;
        EnemyBehaviour m_Behaviour;
        EnemyRangedAttack m_RangedAttack;
        Health m_Health;
        Collider[] m_Colliders = Array.Empty<Collider>();
        EnemyDefinition m_FallbackDefinition;
        State m_State = State.Moving;
        Health m_Target;
        Vector3 m_TargetPoint;
        float m_RepathTimer;
        float m_AttackCooldown;
        float m_HitDelay = -1f;
        float m_DeathTime;
        Quaternion m_DeathRotation;
        Vector3 m_FleePoint;
        float m_FleeTime;
        float m_MoveSpeed;
        float m_RunAnimationSpeed = 1f;
        float m_SlowMultiplier = 1f;
        float m_FrozenTimer;
        float m_SlowTimer;
        float m_FadingSlow;
        float m_FadingDuration;
        float m_FadingAge;
        Vector3 m_Push;
        bool m_HasSpeed;
        bool m_HasAttack;
        bool m_HasHit;
        bool m_HasDie;

        /// <summary>Ennemis vivants dans la scène.</summary>
        public static IReadOnlyList<Enemy> Alive => s_Alive;

        /// <summary>Un ennemi vient d'être touché (score, combo…).</summary>
        public static event Action<Enemy, DamageInfo> Damaged;

        /// <summary>Un ennemi vient de mourir (score, argent…). Le DamageInfo décrit le coup fatal.</summary>
        public static event Action<Enemy, DamageInfo> Killed;

        public EnemyDefinition Definition => Def;
        public Health Health => m_Health;
        public bool IsAlive => m_State != State.Dead;
        public bool IsFleeing => m_State == State.Fleeing;

        /// <summary>Centre du corps : là où visent la visée automatique, les éclairs et les explosions.</summary>
        public Vector3 Center => transform.TransformPoint(0f, m_CenterHeight, 0f);

        /// <summary>Multiplicateur de vitesse durable (1 = vitesse normale). Pour un ralentissement passager, voir <see cref="Slow"/>.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>
        /// Vitesse de l'animation de course par rapport à celle des ennemis normaux (1). Un boss géant fait de grandes
        /// enjambées : il la règle plus bas (<see cref="Boss"/>).
        /// </summary>
        public float RunAnimationScale { get; set; } = 1f;

        /// <summary>Vitesse effective (m/s) : difficulté, vague, multiplicateur durable et ralentissement en cours.</summary>
        public float CurrentSpeed => m_MoveSpeed * SpeedMultiplier * SlowMultiplier;

        /// <summary>
        /// Ralentissement en cours (1 = aucun, 0,75 = 25 % plus lent) : le plus fort entre <see cref="Slow"/> et
        /// <see cref="SlowFading"/>, dans la limite de <see cref="MaxSlow"/>.
        /// </summary>
        public float SlowMultiplier => Mathf.Max(1f - Mathf.Clamp01(MaxSlow), Mathf.Min(m_SlowMultiplier, 1f - FadingStrength));

        /// <summary>
        /// Où en est le ralentissement qui s'estompe (<see cref="SlowFading"/>) : 1 au coup, puis de moins en moins, au même
        /// rythme que lui, jusqu'à 0 (la teinte bleutée du dragon le suit).
        /// </summary>
        public float FadingSlowLevel => m_FadingSlow > 0f ? FadingStrength / m_FadingSlow : 0f;

        // Part de vitesse retirée en ce moment par un ralentissement qui s'estompe : forte au coup, elle décroît au carré.
        float FadingStrength
        {
            get
            {
                if (m_FadingAge >= m_FadingDuration)
                    return 0f;
                var left = 1f - m_FadingAge / m_FadingDuration;
                return m_FadingSlow * left * left;
            }
        }

        /// <summary>
        /// Part de sa vitesse qu'un ralentissement peut lui retirer, tous ralentissements confondus (1 = sans limite ;
        /// 0,25 = jamais plus de 25 % plus lent, comme le dragon du boss final).
        /// </summary>
        public float MaxSlow { get; set; } = 1f;

        /// <summary>Multiplicateur des dégâts infligés : celui de la difficulté au départ.</summary>
        public float DamageMultiplier { get; set; } = 1f;

        EnemyDefinition Def
        {
            get
            {
                if (m_Definition != null)
                    return m_Definition;
                if (m_FallbackDefinition == null)
                    m_FallbackDefinition = ScriptableObject.CreateInstance<EnemyDefinition>();
                return m_FallbackDefinition;
            }
        }

        void Awake()
        {
            m_Agent = GetComponent<NavMeshAgent>();
            m_Behaviour = GetComponent<EnemyBehaviour>();
            m_RangedAttack = GetComponent<EnemyRangedAttack>();
            m_Health = GetComponent<Health>();
            m_Colliders = GetComponentsInChildren<Collider>(true);
            if (m_Animator == null)
                m_Animator = GetComponentInChildren<Animator>();
            // Toujours animé, même quand Unity croit le modèle hors de vue (ses bornes sont souvent fausses, comme pour
            // le mage, agrandi ×100) : sinon il glisse, figé.
            if (m_Animator != null)
                m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            CacheAnimatorParameters();

            if (m_Definition == null)
                Debug.LogError("Enemy : aucune Enemy Definition assignée, valeurs par défaut utilisées.", this);

            WarnAboutCollidersWithoutHitbox();

            // Difficulté, puis renforcement selon la vague (courbe exponentielle).
            var difficulty = DifficultyManager.Current;
            m_Health.ResetHealth(Def.maxHealth * difficulty.enemyHealth * EnemyScaling.Health);
            m_MoveSpeed = Def.moveSpeed * difficulty.enemySpeed * EnemyScaling.Speed;
            m_RunAnimationSpeed = EnemyScaling.Speed;
            DamageMultiplier = difficulty.damageTaken * EnemyScaling.Damage;
            ScaleHeadHitboxes(difficulty.headSize);

            if (m_Agent != null)
            {
                m_Agent.speed = m_MoveSpeed;
                m_Agent.stoppingDistance = Def.attackRange * 0.8f;
            }
            else if (m_Behaviour == null)
            {
                Debug.LogError("Enemy : il faut un NavMeshAgent, ou un comportement particulier (Flying Enemy…).", this);
            }
        }

        void OnEnable()
        {
            if (m_State != State.Dead)
                s_Alive.Add(this);
            m_Health.Damaged += OnDamaged;
            m_Health.Died += OnDied;
        }

        void OnDisable()
        {
            s_Alive.Remove(this);
            m_Health.Damaged -= OnDamaged;
            m_Health.Died -= OnDied;
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            if (m_State == State.Dead)
            {
                UpdateCorpse(deltaTime);
                return;
            }

            if (m_SlowTimer > 0f)
            {
                m_SlowTimer -= deltaTime;
                if (m_SlowTimer <= 0f)
                    m_SlowMultiplier = 1f;
            }

            if (m_FadingAge < m_FadingDuration)
                m_FadingAge += deltaTime;

            // Gelé (pointe de givre, 26 octobre) : figé sur place, sans bouger, attaquer ni s'animer ; la poussée (l'aimant)
            // le déplace quand même.
            if (m_FrozenTimer > 0f)
            {
                m_FrozenTimer -= deltaTime;
                if (m_FrozenTimer > 0f)
                {
                    UpdatePush(deltaTime);
                    Stop();
                    if (m_Animator != null)
                        m_Animator.speed = 0f;
                    return;
                }

                if (m_Animator != null)
                    m_Animator.speed = 1f;
            }

            UpdatePush(deltaTime);

            if (m_State == State.Fleeing)
            {
                if (m_Behaviour != null)
                    m_Behaviour.TickFlee(deltaTime);
                else
                    UpdateFlee(deltaTime);
                UpdateAnimation();
                return;
            }

            if (m_Behaviour != null)
            {
                m_Behaviour.Tick(deltaTime);
                UpdateAnimation();
                return;
            }

            ChooseTarget();
            UpdatePendingHit(deltaTime);

            // Un ennemi qui attaque ne repart qu'une fois nettement hors de portée : sinon, à la limite (un tireur à 25 m
            // du joueur, qui bouge sans cesse avec le casque), il s'arrêtait et repartait plusieurs fois par seconde.
            var range = Def.attackRange + (m_State == State.Attacking ? AttackRangeSlack : 0f);
            if (m_Target == null)
                Stop();
            else if (HorizontalDistance(transform.position, m_TargetPoint) <= range)
                UpdateAttack(deltaTime);
            else
                UpdateMove(deltaTime);

            UpdateAnimation();
        }

        // Paramètre Speed de l'Animator. En course, l'animation accélère avec les vagues, pour que les pieds ne glissent pas.
        void UpdateAnimation()
        {
            if (m_Animator == null || BehaviourControlsAnimator)
                return;

            if (m_HasSpeed)
                m_Animator.SetFloat(k_SpeedId, CurrentVelocity.magnitude);
            m_Animator.speed = m_State == State.Moving || m_State == State.Fleeing ? m_RunAnimationSpeed * RunAnimationScale : 1f;
        }

        bool BehaviourControlsAnimator => m_Behaviour != null && m_Behaviour.ControlsAnimator;

        // Marge de la portée pendant une attaque : 12 % (3 m pour un tireur), au moins 25 cm (corps à corps, où le coup
        // porte encore jusqu'à 30 cm au-delà de la portée).
        float AttackRangeSlack => Mathf.Max(0.25f, Def.attackRange * 0.12f);

        Vector3 CurrentVelocity
        {
            get
            {
                if (m_Behaviour != null)
                    return m_Behaviour.Velocity;
                return m_Agent != null && m_Agent.enabled ? m_Agent.velocity : Vector3.zero;
            }
        }

        /// <summary>
        /// Cible : d'abord une barricade qui lui barre la route ; puis le joueur pour un ennemi qui le vise toujours
        /// (tireur) ; sinon le joueur s'il est au sol et proche, puis la tour, puis le joueur (tour détruite).
        /// </summary>
        void ChooseTarget()
        {
            var player = PlayerHealth.Instance;
            var tower = Tower.Instance;
            var playerAvailable = player != null && player.IsAlive;
            m_Target = null;

            var barricade = Barricade.FindBlocking(transform.position);
            if (barricade != null)
            {
                m_Target = barricade.Health;
                m_TargetPoint = barricade.ClosestPoint(transform.position);
                return;
            }

            if (playerAvailable && Def.targetsPlayer)
            {
                m_Target = player.Health;
                m_TargetPoint = player.BodyPosition;
                return;
            }

            if (playerAvailable && Def.playerAggroRange > 0f)
            {
                var body = player.BodyPosition;
                var sameLevel = Mathf.Abs(body.y - transform.position.y) < 1.5f;
                if (sameLevel && HorizontalDistance(transform.position, body) <= Def.playerAggroRange)
                {
                    m_Target = player.Health;
                    m_TargetPoint = body;
                    return;
                }
            }

            if (tower != null && tower.IsStanding)
            {
                m_Target = tower.Health;
                m_TargetPoint = tower.ClosestPoint(transform.position);
                return;
            }

            if (playerAvailable)
            {
                m_Target = player.Health;
                m_TargetPoint = player.BodyPosition;
            }
        }

        void UpdateMove(float deltaTime)
        {
            m_State = State.Moving;
            if (m_Agent == null || !m_Agent.isOnNavMesh)
                return;

            m_Agent.isStopped = false;
            m_Agent.updateRotation = true;
            m_Agent.speed = CurrentSpeed;

            m_RepathTimer -= deltaTime;
            if (m_RepathTimer <= 0f)
            {
                m_Agent.SetDestination(m_TargetPoint);
                m_RepathTimer = 0.25f;
            }
        }

        void UpdateAttack(float deltaTime)
        {
            m_State = State.Attacking;
            Stop();
            FaceTarget(deltaTime);

            m_AttackCooldown -= deltaTime;
            if (m_AttackCooldown > 0f || m_HitDelay >= 0f)
                return;

            // L'attaque démarre ; le coup porte après le délai de préparation. Une boule chargée (liche) commence tout
            // de suite à se former, et c'est elle qui lance l'animation, au moment du lancer.
            m_AttackCooldown = Def.attackInterval;
            var charged = m_RangedAttack != null && m_RangedAttack.Charges;
            m_HitDelay = charged ? 0f : Def.attackWindup;
            if (m_HasAttack && !charged)
                m_Animator.SetTrigger(k_AttackId);
        }

        void UpdatePendingHit(float deltaTime)
        {
            if (m_HitDelay < 0f)
                return;

            m_HitDelay -= deltaTime;
            if (m_HitDelay > 0f)
                return;

            m_HitDelay = -1f;

            if (m_Target == null)
                return;

            // Tireur : le projectile part, c'est lui qui touchera (ou non).
            if (m_RangedAttack != null)
            {
                m_RangedAttack.Fire(m_Target, Def.attackDamage * DamageMultiplier);
                return;
            }

            // Le coup ne touche que si la cible est encore à portée.
            if (HorizontalDistance(transform.position, m_TargetPoint) > Def.attackRange + 0.3f)
                return;

            DealDamage(m_Target, m_TargetPoint);
        }

        /// <summary>Inflige les dégâts d'attaque de l'ennemi (difficulté et vague comprises) à cette cible.</summary>
        public void DealDamage(Health target, Vector3 point)
        {
            if (target == null || !target.IsAlive)
                return;

            var direction = point - transform.position;
            target.TakeDamage(new DamageInfo
            {
                Amount = Def.attackDamage * DamageMultiplier,
                Zone = HitZone.Body,
                Point = point,
                Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward,
                Source = this,
            });
            Sfx.Play(m_AttackClip, point, 0.9f);
        }

        /// <summary>Lance l'animation d'attaque, s'il y en a une.</summary>
        public void PlayAttackAnimation()
        {
            if (m_HasAttack)
                m_Animator.SetTrigger(k_AttackId);
        }

        /// <summary>Retire l'ennemi de la partie sans le tuer (fin de sa fuite).</summary>
        public void Despawn() => Destroy(gameObject);

        void Stop()
        {
            if (m_Agent == null)
                return;

            if (m_Agent.isOnNavMesh && !m_Agent.isStopped)
            {
                m_Agent.isStopped = true;
                m_Agent.velocity = Vector3.zero;
            }

            m_Agent.updateRotation = false;
        }

        void FaceTarget(float deltaTime)
        {
            var direction = m_TargetPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 360f * deltaTime);
        }

        /// <summary>
        /// Fin de vague : l'ennemi arrête d'attaquer et court vers la sortie, puis disparaît.
        /// On peut encore le tuer pendant sa fuite.
        /// </summary>
        public void Flee(Vector3 exitPoint)
        {
            if (m_State == State.Dead || m_State == State.Fleeing)
                return;

            m_State = State.Fleeing;
            m_FleePoint = exitPoint;
            m_FleeTime = 0f;
            m_HitDelay = -1f;
            m_Target = null;

            if (m_Behaviour != null)
            {
                m_Behaviour.BeginFlee(exitPoint);
                return;
            }

            if (m_Agent == null || !m_Agent.isOnNavMesh)
                return;

            // Il court jusqu'à la sortie elle-même : un tireur s'arrêterait sinon à sa distance de tir (20 m).
            m_Agent.stoppingDistance = 0.5f;
            m_Agent.isStopped = false;
            m_Agent.updateRotation = true;
            m_Agent.speed = CurrentSpeed * m_FleeSpeedMultiplier;
            m_Agent.SetDestination(exitPoint);
        }

        /// <summary>
        /// Ralentit l'ennemi pendant un moment (flèche de foudre). 0,6 = 40 % plus lent.
        /// Si plusieurs ralentissements se cumulent, le plus fort et le plus long l'emportent, dans la limite de
        /// <see cref="MaxSlow"/>.
        /// </summary>
        public void Slow(float multiplier, float duration)
        {
            if (m_State == State.Dead || duration <= 0f)
                return;

            multiplier = Mathf.Max(Mathf.Clamp01(multiplier), 1f - Mathf.Clamp01(MaxSlow));
            m_SlowMultiplier = Mathf.Min(m_SlowTimer > 0f ? m_SlowMultiplier : 1f, multiplier);
            m_SlowTimer = Mathf.Max(m_SlowTimer, duration);
        }

        /// <summary>L'ennemi est gelé (pointe de givre, 26 octobre) : figé, sans attaquer.</summary>
        public bool IsFrozen => m_FrozenTimer > 0f && m_State != State.Dead;

        /// <summary>
        /// Gèle l'ennemi pendant <paramref name="duration"/> s (pointe de givre, 26 octobre) : figé sur place, animation
        /// arrêtée, sans attaquer. Un ennemi dont le ralentissement est limité (<see cref="MaxSlow"/> : le dragon) est
        /// seulement ralenti, dans sa limite. Renvoie vrai s'il est vraiment gelé.
        /// </summary>
        public bool Freeze(float duration)
        {
            if (m_State == State.Dead || duration <= 0f)
                return false;

            if (MaxSlow < 1f)
            {
                Slow(0f, duration);
                return false;
            }

            m_FrozenTimer = Mathf.Max(m_FrozenTimer, duration);
            return true;
        }

        /// <summary>Dégèle l'ennemi tout de suite (il vient d'éclater en éclats de glace).</summary>
        public void Thaw()
        {
            if (m_FrozenTimer <= 0f)
                return;

            m_FrozenTimer = 0f;
            if (m_Animator != null)
                m_Animator.speed = 1f;
        }

        /// <summary>
        /// Repousse l'ennemi (24 octobre : le coup de poing de la main de la matrice, avec le katana) : il glisse à
        /// <paramref name="velocity"/> (m/s, à plat), de moins en moins vite, en restant sur le NavMesh. Les volants reculent
        /// aussi. Un nouveau coup remplace le précédent s'il est plus fort.
        /// </summary>
        public void Push(Vector3 velocity)
        {
            if (m_State == State.Dead)
                return;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > m_Push.sqrMagnitude)
                m_Push = velocity;
        }

        void UpdatePush(float deltaTime)
        {
            if (m_Push.sqrMagnitude < 0.01f)
            {
                m_Push = Vector3.zero;
                return;
            }

            var step = m_Push * deltaTime;
            if (m_Agent != null && m_Agent.enabled && m_Agent.isOnNavMesh)
                m_Agent.Move(step);
            else if (m_Behaviour != null)
                transform.position += step;
            m_Push *= Mathf.Exp(-6f * deltaTime);
        }

        /// <summary>
        /// Ralentissement qui s'estompe vite (boule de glace d'une tour de givre, arène du boss final) : <paramref name="multiplier"/>
        /// au coup (0,7 = 30 % plus lent, dans la limite de <see cref="MaxSlow"/>), puis de moins en moins, au carré,
        /// jusqu'à plus rien après <paramref name="duration"/> s. Un nouveau coup repart du plus fort des deux.
        /// </summary>
        public void SlowFading(float multiplier, float duration)
        {
            if (m_State == State.Dead || duration <= 0f)
                return;

            var strength = 1f - Mathf.Clamp01(multiplier);
            if (strength < FadingStrength)
                return;

            m_FadingSlow = strength;
            m_FadingDuration = duration;
            m_FadingAge = 0f;
        }

        void UpdateFlee(float deltaTime)
        {
            m_FleeTime += deltaTime;
            if (HorizontalDistance(transform.position, m_FleePoint) < 2f || m_FleeTime > 15f)
                Destroy(gameObject);
        }

        void OnDamaged(Health health, DamageInfo info)
        {
            Damaged?.Invoke(this, info);
            if (m_HasHit && health.IsAlive && !BehaviourControlsAnimator)
                m_Animator.SetTrigger(k_HitId);
        }

        void OnDied(Health health, DamageInfo info)
        {
            if (m_State == State.Dead)
                return;

            m_State = State.Dead;
            s_Alive.Remove(this);
            m_HitDelay = -1f;

            if (m_Agent != null)
            {
                if (m_Agent.isOnNavMesh)
                    m_Agent.isStopped = true;
                m_Agent.enabled = false;
            }

            if (m_Behaviour != null)
                m_Behaviour.OnDied();

            // Les flèches traversent les corps.
            foreach (var collider in m_Colliders)
            {
                if (collider != null)
                    collider.enabled = false;
            }

            m_DeathTime = 0f;
            m_DeathRotation = transform.rotation;
            if (!BehaviourControlsAnimator)
            {
                if (m_HasSpeed)
                    m_Animator.SetFloat(k_SpeedId, 0f);

                // Avec une animation de mort, on la joue ; sinon on fige la pose pour qu'il ne court plus en tombant.
                if (m_HasDie)
                {
                    m_Animator.speed = 1f;
                    m_Animator.SetTrigger(k_DieId);
                }
                else if (m_Animator != null)
                {
                    m_Animator.speed = 0f;
                }
            }

            Sfx.Play(m_DeathClip, transform.position + Vector3.up, 0.9f);
            Killed?.Invoke(this, info);
        }

        void UpdateCorpse(float deltaTime)
        {
            m_DeathTime += deltaTime;
            var handled = m_Behaviour != null && m_Behaviour.TickCorpse(deltaTime);

            // Sans animation de mort (ni chute gérée par le comportement), l'ennemi bascule en arrière.
            if (!m_HasDie && !handled && !BehaviourControlsAnimator)
            {
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_DeathTime / 0.5f));
                transform.rotation = m_DeathRotation * Quaternion.Euler(-80f * t, 0f, 0f);
            }

            // Puis le corps s'enfonce dans le sol et disparaît.
            if (m_DeathTime > m_CorpseLifetime - 1f)
                transform.position += Vector3.down * (0.8f * deltaTime);
            if (m_DeathTime >= m_CorpseLifetime)
                Destroy(gameObject);
        }

        void CacheAnimatorParameters()
        {
            if (m_Animator == null || m_Animator.runtimeAnimatorController == null)
                return;

            foreach (var parameter in m_Animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Float && parameter.nameHash == k_SpeedId)
                    m_HasSpeed = true;
                else if (parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    m_HasAttack |= parameter.nameHash == k_AttackId;
                    m_HasHit |= parameter.nameHash == k_HitId;
                    m_HasDie |= parameter.nameHash == k_DieId;
                }
            }
        }

        // Difficulté : tête plus grosse en Facile, plus petite en Difficile et Impossible.
        void ScaleHeadHitboxes(float scale)
        {
            if (Mathf.Approximately(scale, 1f))
                return;

            foreach (var hitbox in GetComponentsInChildren<Hitbox>(true))
            {
                if (hitbox.Zone == HitZone.Head)
                    hitbox.SetSizeMultiplier(scale);
            }
        }

        // Une flèche qui touche un collider sans Hitbox se plante sans faire de dégâts : on prévient.
        void WarnAboutCollidersWithoutHitbox()
        {
            foreach (var collider in m_Colliders)
            {
                if (collider != null && !collider.isTrigger && collider.GetComponentInParent<Hitbox>() == null)
                    Debug.LogWarning($"Enemy : le collider « {collider.name} » n'a pas de Hitbox, les flèches qui le touchent ne feront pas de dégâts.", collider);
            }
        }

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Alive.Clear();
            Damaged = null;
            Killed = null;
        }
    }
}
