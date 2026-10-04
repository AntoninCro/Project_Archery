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
    /// Ennemi au sol (GDD, section 10) : marche vers la tour avec un NavMeshAgent et la frappe,
    /// ou attaque le joueur s'il est au sol tout près. Meurt quand son <see cref="Health"/> tombe à 0.
    /// </summary>
    /// <remarks>
    /// L'Animator est optionnel. S'il existe, il peut utiliser les paramètres Speed (float),
    /// Attack, Hit et Die (triggers) ; ceux qui manquent sont simplement ignorés.
    /// À son apparition, l'ennemi applique la difficulté (PV, vitesse, dégâts, taille de la tête)
    /// et le renforcement de la vague en cours (<see cref="EnemyScaling"/>).
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
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

        [SerializeField]
        AudioClip m_AttackClip;

        [SerializeField]
        AudioClip m_DeathClip;

        NavMeshAgent m_Agent;
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
        float m_SlowTimer;
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

        /// <summary>Multiplicateur de vitesse durable (1 = vitesse normale). Pour un ralentissement passager, voir <see cref="Slow"/>.</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        /// <summary>Vitesse effective : difficulté, multiplicateur durable et ralentissement en cours.</summary>
        float CurrentSpeed => m_MoveSpeed * SpeedMultiplier * m_SlowMultiplier;

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
            m_Health = GetComponent<Health>();
            m_Colliders = GetComponentsInChildren<Collider>(true);
            if (m_Animator == null)
                m_Animator = GetComponentInChildren<Animator>();
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

            m_Agent.speed = m_MoveSpeed;
            m_Agent.stoppingDistance = Def.attackRange * 0.8f;
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

            if (m_State == State.Fleeing)
            {
                UpdateFlee(deltaTime);
                UpdateAnimation();
                return;
            }

            ChooseTarget();
            UpdatePendingHit(deltaTime);

            if (m_Target == null)
                Stop();
            else if (HorizontalDistance(transform.position, m_TargetPoint) <= Def.attackRange)
                UpdateAttack(deltaTime);
            else
                UpdateMove(deltaTime);

            UpdateAnimation();
        }

        // Paramètre Speed de l'Animator. En course, l'animation accélère avec les vagues, pour que les pieds ne glissent pas.
        void UpdateAnimation()
        {
            if (m_Animator == null)
                return;

            if (m_HasSpeed)
                m_Animator.SetFloat(k_SpeedId, m_Agent.enabled ? m_Agent.velocity.magnitude : 0f);
            m_Animator.speed = m_State == State.Moving || m_State == State.Fleeing ? m_RunAnimationSpeed : 1f;
        }

        /// <summary>
        /// Cible : le joueur s'il est au sol et proche, sinon la tour, sinon le joueur (tour détruite).
        /// </summary>
        void ChooseTarget()
        {
            var player = PlayerHealth.Instance;
            var tower = Tower.Instance;
            var playerAvailable = player != null && player.IsAlive;
            m_Target = null;

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
            if (!m_Agent.isOnNavMesh)
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

            // L'attaque démarre ; le coup porte après le délai de préparation.
            m_AttackCooldown = Def.attackInterval;
            m_HitDelay = Def.attackWindup;
            if (m_HasAttack)
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

            // Le coup ne touche que si la cible est encore à portée.
            if (m_Target == null || HorizontalDistance(transform.position, m_TargetPoint) > Def.attackRange + 0.3f)
                return;

            var direction = m_TargetPoint - transform.position;
            m_Target.TakeDamage(new DamageInfo
            {
                Amount = Def.attackDamage * DamageMultiplier,
                Zone = HitZone.Body,
                Point = m_TargetPoint,
                Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward,
                Source = this,
            });
            Sfx.Play(m_AttackClip, m_TargetPoint, 0.9f);
        }

        void Stop()
        {
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

            if (!m_Agent.isOnNavMesh)
                return;

            m_Agent.isStopped = false;
            m_Agent.updateRotation = true;
            m_Agent.speed = CurrentSpeed * m_FleeSpeedMultiplier;
            m_Agent.SetDestination(exitPoint);
        }

        /// <summary>
        /// Ralentit l'ennemi pendant un moment (flèche de foudre). 0,6 = 40 % plus lent.
        /// Si plusieurs ralentissements se cumulent, le plus fort et le plus long l'emportent.
        /// </summary>
        public void Slow(float multiplier, float duration)
        {
            if (m_State == State.Dead || duration <= 0f)
                return;

            m_SlowMultiplier = Mathf.Min(m_SlowTimer > 0f ? m_SlowMultiplier : 1f, Mathf.Clamp01(multiplier));
            m_SlowTimer = Mathf.Max(m_SlowTimer, duration);
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
            if (m_HasHit && health.IsAlive)
                m_Animator.SetTrigger(k_HitId);
        }

        void OnDied(Health health, DamageInfo info)
        {
            if (m_State == State.Dead)
                return;

            m_State = State.Dead;
            s_Alive.Remove(this);
            m_HitDelay = -1f;

            if (m_Agent.isOnNavMesh)
                m_Agent.isStopped = true;
            m_Agent.enabled = false;

            // Les flèches traversent les corps.
            foreach (var collider in m_Colliders)
            {
                if (collider != null)
                    collider.enabled = false;
            }

            m_DeathTime = 0f;
            m_DeathRotation = transform.rotation;
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

            Sfx.Play(m_DeathClip, transform.position + Vector3.up, 0.9f);
            Killed?.Invoke(this, info);
        }

        void UpdateCorpse(float deltaTime)
        {
            m_DeathTime += deltaTime;

            // Sans animation de mort, l'ennemi bascule en arrière.
            if (!m_HasDie)
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
