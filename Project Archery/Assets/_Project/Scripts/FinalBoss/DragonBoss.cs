using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Player;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>Les phases du dragon (GDD, section 23).</summary>
    public enum DragonPhase
    {
        /// <summary>Il dort pendant l'intro de l'arène.</summary>
        Asleep,

        /// <summary>Phase 1, au sol : il frappe si on est trop près et crache du feu.</summary>
        Ground,

        /// <summary>Phase 2, en vol : boules de feu et passes en piqué enflammé.</summary>
        Air,

        /// <summary>Phase 3, de retour au sol, enragé : plus rapide, plus fort, rochers du plafond, lave qui déborde.</summary>
        Enraged,

        Dead,
    }

    /// <summary>
    /// Le dragon rouge, boss final (GDD, section 23). Comportement d'un <see cref="Enemy"/> (PV, difficulté, score) :
    /// il dort pendant l'intro, se réveille au signal de l'<see cref="ArenaDirector"/>, puis enchaîne trois phases
    /// selon ses PV (par tiers).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Il joue lui-même les animations du modèle, par leur nom (Animator « DragonBoss », sans transitions).</item>
    /// <item>Ses zones de touche sont créées au lancement sur les os du modèle : tête (×2), cou, corps, pattes, et un
    /// point faible cyan sous la poitrine (×3). <see cref="m_HitboxScale"/> les agrandit ou les réduit toutes.</item>
    /// <item>Au sol, il contourne les piliers ; son souffle s'arrête sur eux : on peut s'y abriter.</item>
    /// <item>Les dégâts de toutes ses attaques partent de l'« Attack Damage » de sa définition, multiplié par la difficulté
    /// et la vague, comme les autres ennemis.</item>
    /// </list>
    /// </remarks>
    [DefaultExecutionOrder(-50)]   // zones de touche créées avant l'Awake d'Enemy, qui les recense
    public class DragonBoss : EnemyBehaviour
    {
        enum State
        {
            Sleeping,
            Waking,
            Moving,
            Melee,
            Breath,
            TakeOff,
            Flying,
            Dive,
            Landing,
            Roar,
            Idle,
            Dead,
        }

        [Header("Modèle")]
        [Tooltip("Animator du modèle. Vide : celui des enfants.")]
        [SerializeField]
        Animator m_Animator;

        [Tooltip("Contrôleur sans transitions, avec les états du pack (Sleep, Scream, Walk, Run, Flame Attack…).")]
        [SerializeField]
        RuntimeAnimatorController m_Controller;

        [SerializeField]
        string m_HeadBone = "Head";

        [SerializeField]
        string m_NeckBone = "Neck03";

        [SerializeField]
        string m_NeckBaseBone = "Neck01";

        [SerializeField]
        string m_ChestBone = "Chest";

        [SerializeField]
        string m_SpineBone = "Spine01";

        [Tooltip("Rayon (m) du corps au sol : distance gardée avec les piliers et le bord de l'arène.")]
        [SerializeField]
        float m_BodyRadius = 2.5f;

        [Tooltip("Multiplie la taille de toutes les zones de touche (à régler si les flèches traversent le dragon ou le touchent dans le vide).")]
        [SerializeField]
        float m_HitboxScale = 1f;

        [SerializeField]
        Material m_WeakPointMaterial;

        [SerializeField]
        Material m_FireMaterial;

        [Tooltip("Matériau du cœur des boules de feu.")]
        [SerializeField]
        Material m_FireballMaterial;

        [Tooltip("Matériau des cercles d'alerte au sol (rochers, piqués).")]
        [SerializeField]
        Material m_WarningMaterial;

        [Tooltip("Matériau des rochers qui tombent.")]
        [SerializeField]
        Material m_RockMaterial;

        [SerializeField]
        DragonArenaBuilder m_Arena;

        [Header("Phases (part des PV)")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_AirPhaseAt = 0.66f;

        [Range(0f, 1f)]
        [SerializeField]
        float m_EnragedPhaseAt = 0.33f;

        [Tooltip("Durée (s) du rugissement du réveil, avant le combat.")]
        [SerializeField]
        float m_WakeRoarTime = 2.6f;

        [Header("Au sol")]
        [Tooltip("Vitesse de rotation (°/s).")]
        [SerializeField]
        float m_TurnSpeed = 100f;

        [Tooltip("Portée du coup (m) en plus de la distance du pivot à la tête.")]
        [SerializeField]
        float m_MeleeExtraReach = 1.8f;

        [Tooltip("Temps (s) entre deux coups au corps à corps.")]
        [SerializeField]
        float m_MeleeCooldown = 1.6f;

        [Tooltip("Dégâts du coup de griffe et de la morsure, en part de l'« Attack Damage ».")]
        [SerializeField]
        Vector2 m_MeleeDamage = new Vector2(1f, 1.25f);

        [SerializeField]
        float m_BreathRange = 15f;

        [Tooltip("Demi-angle (°) du cône de feu.")]
        [SerializeField]
        float m_BreathAngle = 20f;

        [SerializeField]
        float m_BreathDuration = 2.2f;

        [SerializeField]
        float m_BreathCooldown = 7f;

        [Tooltip("Dégâts du souffle par seconde, en part de l'« Attack Damage ».")]
        [SerializeField]
        float m_BreathDamage = 0.45f;

        [Tooltip("Vitesse (°/s) à laquelle le souffle suit le joueur : assez lente pour qu'on puisse courir derrière un pilier.")]
        [SerializeField]
        float m_BreathTracking = 30f;

        [Header("En vol")]
        [SerializeField]
        float m_FlyAltitude = 10f;

        [SerializeField]
        float m_OrbitRadius = 15f;

        [SerializeField]
        float m_FlySpeed = 8f;

        [SerializeField]
        float m_FireballInterval = 2.4f;

        [SerializeField]
        float m_FireballSpeed = 18f;

        [Tooltip("Dégâts d'une boule de feu, en part de l'« Attack Damage ».")]
        [SerializeField]
        float m_FireballDamage = 0.5f;

        [SerializeField]
        float m_FireballRadius = 2.6f;

        [SerializeField]
        float m_FireballSize = 0.7f;

        [Tooltip("Temps (s) entre deux passes en piqué.")]
        [SerializeField]
        float m_DiveInterval = 13f;

        [SerializeField]
        float m_DiveAltitude = 3.8f;

        [SerializeField]
        float m_DiveSpeed = 15f;

        [Tooltip("Dégâts par seconde de la traînée de feu, en part de l'« Attack Damage ».")]
        [SerializeField]
        float m_TrailDamage = 0.35f;

        [Header("Enragé (phase 3)")]
        [SerializeField]
        float m_RageSpeed = 1.45f;

        [Tooltip("Ses attaques reviennent plus vite (1,5 = 50 % plus souvent).")]
        [SerializeField]
        float m_RageAttackRate = 1.5f;

        [SerializeField]
        float m_RageDamage = 1.3f;

        [Tooltip("Temps (s) entre deux rugissements qui font tomber des rochers.")]
        [SerializeField]
        float m_RageRoarInterval = 12f;

        [SerializeField]
        int m_RockCount = 5;

        [Tooltip("Dégâts d'un rocher, en part de l'« Attack Damage ».")]
        [SerializeField]
        float m_RockDamage = 0.6f;

        [SerializeField]
        float m_RockRadius = 2.2f;

        [Tooltip("Temps (s) entre le cercle d'alerte et l'impact.")]
        [SerializeField]
        float m_RockWarning = 1.5f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_RoarClip;

        [Tooltip("Son joué en même temps que le rugissement (cor, tambours…).")]
        [SerializeField]
        AudioClip m_RoarLayerClip;

        [SerializeField]
        AudioClip m_FireLoopClip;

        [SerializeField]
        AudioClip m_BiteClip;

        [SerializeField]
        AudioClip m_ClawClip;

        [SerializeField]
        AudioClip m_StepClip;

        [SerializeField]
        AudioClip m_WingClip;

        [SerializeField]
        AudioClip m_FireballClip;

        [SerializeField]
        AudioClip m_ExplosionClip;

        [SerializeField]
        AudioClip m_RockClip;

        [SerializeField]
        AudioClip m_DeathClip;

        readonly List<Collider> m_Hitboxes = new List<Collider>();
        Transform m_Head;
        Transform m_Neck;
        Transform m_NeckBase;
        Transform m_Chest;
        Transform m_Spine;
        ParticleSystem m_Breath;
        AudioSource m_FireSource;
        Light m_BreathLight;
        State m_State = State.Sleeping;
        string m_Playing;
        float m_StateTime;
        float m_Reach = 6f;
        float m_MeleeTimer;
        float m_BreathTimer = 3f;
        float m_FireballTimer;
        float m_DiveTimer;
        float m_RageRoarTimer;
        float m_BreathTick;
        bool m_HitDone;
        bool m_Firing;
        int m_MeleeKind;
        Vector3 m_BreathAim;
        Vector3 m_Velocity;
        Vector3 m_LastPosition;
        float m_OrbitAngle;
        Vector3 m_FromPosition;
        Vector3 m_ToPosition;
        Vector3 m_DiveStart;
        Vector3 m_DiveEnd;
        float m_NextTrail;
        Transform m_WarningLine;
        Vector3 m_PlayerVelocity;
        Vector3 m_LastPlayerFeet;
        DragonPhase m_Phase = DragonPhase.Asleep;

        public static DragonBoss Instance { get; private set; }

        public DragonPhase Phase => m_Phase;

        /// <summary>Le dragon change de phase (réveil, envol, retour enragé, mort).</summary>
        public event Action<DragonPhase> PhaseChanged;

        public override Vector3 Velocity => m_Velocity;
        public override bool ControlsAnimator => true;
        public override bool NeedsNavMesh => false;

        float BaseDamage => Enemy.Definition.attackDamage * Enemy.DamageMultiplier * (m_Phase == DragonPhase.Enraged ? m_RageDamage : 1f);
        float AttackRate => m_Phase == DragonPhase.Enraged ? m_RageAttackRate : 1f;
        bool IsAirborne => m_State == State.TakeOff || m_State == State.Flying || m_State == State.Dive || m_State == State.Landing;
        Vector3 Center => m_Arena != null ? m_Arena.Center : Vector3.zero;
        float FloorY => m_Arena != null ? m_Arena.FloorY : transform.position.y;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            if (m_Arena == null)
                m_Arena = FindAnyObjectByType<DragonArenaBuilder>();
            if (m_Animator == null)
                m_Animator = GetComponentInChildren<Animator>();
            if (m_Animator != null)
            {
                if (m_Controller != null)
                    m_Animator.runtimeAnimatorController = m_Controller;
                m_Animator.applyRootMotion = false;
                m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            m_Head = FindBone(m_HeadBone);
            m_Neck = FindBone(m_NeckBone);
            m_NeckBase = FindBone(m_NeckBaseBone);
            m_Chest = FindBone(m_ChestBone);
            m_Spine = FindBone(m_SpineBone);
            BuildHitboxes();
            BuildBreath();
            SetHitboxesEnabled(false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            Play("Sleep", 0f);
            m_LastPosition = transform.position;
            m_LastPlayerFeet = ArenaPlayer.Feet;
        }

        // ---------------------------------------------------------------- réveil (appelé par l'arène)

        /// <summary>Fin de l'intro : le dragon se réveille et rugit, puis le combat commence.</summary>
        public void WakeUp()
        {
            if (m_State != State.Sleeping)
                return;

            // Ce qu'il aurait reçu pendant l'intro ne compte pas.
            Enemy.Health.ResetHealth(Enemy.Health.Max);
            SetHitboxesEnabled(true);
            SetPhase(DragonPhase.Ground);
            Roar(State.Waking);
        }

        // ---------------------------------------------------------------- une image

        public override void Tick(float deltaTime)
        {
            TrackPlayer(deltaTime);
            m_StateTime += deltaTime;
            m_MeleeTimer -= deltaTime * AttackRate;
            m_BreathTimer -= deltaTime * AttackRate;

            if (m_State != State.Sleeping && m_State != State.Dead && !ArenaPlayer.IsAlive)
            {
                StopBreath();
                if (!IsAirborne)
                    EnterState(State.Idle, "Idle01");
            }

            switch (m_State)
            {
                case State.Sleeping:
                    break;
                case State.Waking:
                case State.Roar:
                    UpdateRoar();
                    break;
                case State.Moving:
                    UpdateMoving(deltaTime);
                    break;
                case State.Melee:
                    UpdateMelee(deltaTime);
                    break;
                case State.Breath:
                    UpdateBreath(deltaTime);
                    break;
                case State.TakeOff:
                    UpdateTakeOff();
                    break;
                case State.Flying:
                    UpdateFlying(deltaTime);
                    break;
                case State.Dive:
                    UpdateDive(deltaTime);
                    break;
                case State.Landing:
                    UpdateLanding();
                    break;
                case State.Idle:
                    if (ArenaPlayer.IsAlive)
                        EnterState(State.Moving);
                    break;
            }

            m_Velocity = deltaTime > 0f ? (transform.position - m_LastPosition) / deltaTime : Vector3.zero;
            m_LastPosition = transform.position;
        }

        void LateUpdate()
        {
            // Le souffle part de la gueule, après l'animation de l'image.
            if (m_Breath != null && m_Firing)
            {
                m_Breath.transform.SetPositionAndRotation(MouthPosition, Quaternion.LookRotation(m_BreathAim));
                if (m_BreathLight != null)
                    m_BreathLight.transform.position = MouthPosition + m_BreathAim * 3f;
            }
        }

        void TrackPlayer(float deltaTime)
        {
            var feet = ArenaPlayer.Feet;
            if (deltaTime > 0f)
                m_PlayerVelocity = Vector3.Lerp(m_PlayerVelocity, (feet - m_LastPlayerFeet) / deltaTime, 1f - Mathf.Exp(-deltaTime * 4f));
            m_LastPlayerFeet = feet;
        }

        // ---------------------------------------------------------------- au sol

        // Point de décision : changement de phase, rugissement, attaque ou déplacement.
        void UpdateMoving(float deltaTime)
        {
            if (CheckPhaseChange())
                return;

            if (m_Phase == DragonPhase.Enraged)
            {
                m_RageRoarTimer -= deltaTime;
                if (m_RageRoarTimer <= 0f)
                {
                    m_RageRoarTimer = m_RageRoarInterval;
                    Roar(State.Roar);
                    DropRocks();
                    return;
                }
            }

            var target = ArenaPlayer.Feet;
            var distance = HorizontalDistance(transform.position, target);
            var facing = FacingAngle(target);

            if (distance <= m_Reach && m_MeleeTimer <= 0f && facing < 50f)
            {
                StartMelee();
                return;
            }

            if (distance > m_Reach * 0.9f && distance <= m_BreathRange && m_BreathTimer <= 0f && facing < 30f && HasLineOfSight(MouthPosition, ArenaPlayer.Chest))
            {
                StartBreath();
                return;
            }

            // Avance vers le joueur en contournant les piliers ; s'arrête à portée de coup.
            var speed = Enemy.CurrentSpeed * (m_Phase == DragonPhase.Enraged ? m_RageSpeed : 1f);
            var direction = Steer(transform.position, target);
            TurnTowards(direction, m_TurnSpeed * (m_Phase == DragonPhase.Enraged ? 1.4f : 1f), deltaTime);
            if (distance > m_Reach * 0.8f && Vector3.Angle(transform.forward, direction) < 70f)
            {
                transform.position += transform.forward * (speed * deltaTime);
                Play(speed > 3.2f ? "Run" : "Walk");
            }
            else
            {
                Play("Idle01");
            }

            KeepInsideArena();
        }

        void StartMelee()
        {
            m_MeleeKind = UnityEngine.Random.value < 0.5f ? 0 : 1;
            m_HitDone = false;
            EnterState(State.Melee, m_MeleeKind == 0 ? "Claw Attack" : "Basic Attack", 0.1f, restart: true);
        }

        void UpdateMelee(float deltaTime)
        {
            TurnTowards(ArenaPlayer.Feet - transform.position, m_TurnSpeed * 0.5f, deltaTime);
            var windup = (m_MeleeKind == 0 ? 0.55f : 0.45f) / AttackRate;
            if (!m_HitDone && m_StateTime >= windup)
            {
                m_HitDone = true;
                var target = ArenaPlayer.Feet;
                if (HorizontalDistance(transform.position, target) <= m_Reach + 0.5f && FacingAngle(target) < 75f)
                {
                    var multiplier = m_MeleeKind == 0 ? m_MeleeDamage.x : m_MeleeDamage.y;
                    ArenaPlayer.Damage(BaseDamage * multiplier, ArenaPlayer.Chest, Enemy);
                    ArenaPlayer.Rumble(0.7f, 0.2f);
                }

                Sfx.Play(m_MeleeKind == 0 ? m_ClawClip : m_BiteClip, MouthPosition, 1f, UnityEngine.Random.Range(0.6f, 0.75f));
            }

            if (m_StateTime >= windup + 0.9f / AttackRate)
            {
                m_MeleeTimer = m_MeleeCooldown;
                EnterState(State.Moving);
            }
        }

        void StartBreath()
        {
            m_BreathAim = (ArenaPlayer.Chest - MouthPosition).normalized;
            EnterState(State.Breath, "Flame Attack", 0.15f, restart: true);
        }

        void UpdateBreath(float deltaTime)
        {
            const float windup = 0.7f;
            if (!m_Firing && m_StateTime >= windup && m_StateTime < windup + m_BreathDuration)
                StartFiring();

            if (m_Firing)
            {
                // La flamme suit le joueur lentement : on a le temps de se mettre à l'abri.
                var wanted = ClampToFacing((ArenaPlayer.Chest - MouthPosition).normalized, 40f);
                m_BreathAim = Vector3.RotateTowards(m_BreathAim, wanted, m_BreathTracking * Mathf.Deg2Rad * deltaTime, 0f);
                TurnTowards(ArenaPlayer.Feet - transform.position, m_BreathTracking * 0.6f, deltaTime);
                DamageInCone(deltaTime, MouthPosition, m_BreathAim, m_BreathRange, m_BreathAngle, m_BreathDamage);
            }

            if (m_StateTime >= windup + m_BreathDuration)
                StopBreath();

            if (m_StateTime >= windup + m_BreathDuration + 0.7f)
            {
                m_BreathTimer = m_BreathCooldown;
                EnterState(State.Moving);
            }
        }

        void StartFiring()
        {
            m_Firing = true;
            m_BreathTick = 0f;
            if (m_Breath != null)
            {
                m_Breath.transform.SetPositionAndRotation(MouthPosition, Quaternion.LookRotation(m_BreathAim));
                m_Breath.Play();
            }

            if (m_FireSource != null && m_FireLoopClip != null)
            {
                m_FireSource.clip = m_FireLoopClip;
                m_FireSource.Play();
            }

            if (m_BreathLight != null)
                m_BreathLight.enabled = true;
        }

        void StopBreath()
        {
            if (!m_Firing)
                return;

            m_Firing = false;
            if (m_Breath != null)
                m_Breath.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (m_FireSource != null)
                m_FireSource.Stop();
            if (m_BreathLight != null)
                m_BreathLight.enabled = false;
        }

        // Dégâts dans le cône de feu, toutes les 0,2 s, si rien (un pilier) ne protège le joueur.
        void DamageInCone(float deltaTime, Vector3 origin, Vector3 direction, float range, float halfAngle, float damagePerSecond)
        {
            m_BreathTick -= deltaTime;
            if (m_BreathTick > 0f || !ArenaPlayer.IsAlive)
                return;

            const float tick = 0.2f;
            m_BreathTick = tick;
            var chest = ArenaPlayer.Chest;
            var toPlayer = chest - origin;
            if (toPlayer.magnitude > range || Vector3.Angle(direction, toPlayer) > halfAngle + 6f)
                return;
            if (!HasLineOfSight(origin, chest))
                return;

            ArenaPlayer.Damage(BaseDamage * damagePerSecond * tick, chest, Enemy);
            ArenaPlayer.Rumble(0.35f, 0.1f);
        }

        // ---------------------------------------------------------------- phases

        bool CheckPhaseChange()
        {
            var health = Enemy.Health.Normalized;
            if (m_Phase == DragonPhase.Ground && health <= m_AirPhaseAt)
            {
                SetPhase(DragonPhase.Air);
                Roar(State.TakeOff);
                return true;
            }

            if (m_Phase == DragonPhase.Air && health <= m_EnragedPhaseAt)
            {
                BeginLanding();
                return true;
            }

            return false;
        }

        // Rugissement : le dragon s'arrête, rugit, et la suite dépend de l'état demandé (réveil, envol, rage).
        void Roar(State next)
        {
            StopBreath();
            EnterState(next, "Scream", 0.2f, restart: true);
            var mouth = MouthPosition;
            Sfx.Play(m_RoarClip, mouth, 1f, 0.45f, 0.6f);
            Sfx.Play(m_RoarLayerClip, mouth, 0.9f, 0.85f, 0.4f);
            ArenaPlayer.Rumble(0.6f, 0.6f);
            if (next == State.TakeOff)
                m_FromPosition = transform.position;
        }

        void UpdateRoar()
        {
            var duration = m_State == State.Waking ? m_WakeRoarTime : 1.6f;
            if (m_StateTime < duration)
                return;

            if (m_State == State.Waking)
            {
                // La portée de ses coups dépend de la taille du modèle : mesurée une fois debout.
                if (m_Head != null)
                    m_Reach = Mathf.Max(3f, HorizontalDistance(transform.position, m_Head.position) + m_MeleeExtraReach);
                m_BreathTimer = 2f;
            }

            EnterState(State.Moving);
        }

        // Envol : rugissement, puis il décolle et rejoint son cercle de vol.
        void UpdateTakeOff()
        {
            const float roarTime = 1.8f;
            const float liftTime = 2.4f;
            if (m_StateTime < roarTime)
                return;

            if (m_Playing != "Take Off")
            {
                Play("Take Off", 0.2f);
                Sfx.Play(m_WingClip, transform.position, 1f, 0.5f);
                var offset = transform.position - Center;
                offset.y = 0f;
                m_OrbitAngle = Mathf.Atan2(offset.x, offset.z);
                m_ToPosition = OrbitPoint(m_OrbitAngle);
            }

            var t = Mathf.Clamp01((m_StateTime - roarTime) / liftTime);
            var eased = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(m_FromPosition, m_ToPosition, eased);
            TurnTowards(Tangent(m_OrbitAngle), m_TurnSpeed, Time.deltaTime);
            if (t >= 1f)
            {
                m_FireballTimer = m_FireballInterval;
                m_DiveTimer = m_DiveInterval * 0.6f;
                EnterState(State.Flying, "Fly Forward");
            }
        }

        void UpdateFlying(float deltaTime)
        {
            if (CheckPhaseChange())
                return;

            // Tourne autour de l'arène, au-dessus des piliers.
            m_OrbitAngle += m_FlySpeed / Mathf.Max(1f, m_OrbitRadius) * deltaTime;
            var wanted = OrbitPoint(m_OrbitAngle);
            transform.position = Vector3.MoveTowards(transform.position, wanted, m_FlySpeed * 1.5f * deltaTime);
            TurnTowards(Tangent(m_OrbitAngle), m_TurnSpeed, deltaTime);

            m_FireballTimer -= deltaTime;
            if (m_FireballTimer <= 0f && ArenaPlayer.IsAlive)
            {
                m_FireballTimer = m_FireballInterval;
                ShootFireball();
            }

            m_DiveTimer -= deltaTime;
            if (m_DiveTimer <= 0f && ArenaPlayer.IsAlive && TryPlanDive())
                return;

            if (m_StateTime > 0.9f && m_Playing == "Fly Flame Attack")
                Play("Fly Forward", 0.3f);
        }

        void ShootFireball()
        {
            Play("Fly Flame Attack", 0.15f, restart: true);
            m_StateTime = 0f;

            var mouth = MouthPosition;
            var chest = ArenaPlayer.Chest;
            var flight = Vector3.Distance(mouth, chest) / Mathf.Max(1f, m_FireballSpeed);
            var aim = chest + m_PlayerVelocity * flight;
            var velocity = (aim - mouth).normalized * m_FireballSpeed;
            DragonFireball.Launch(mouth, velocity, BaseDamage * m_FireballDamage, m_FireballRadius, m_FireballSize,
                                  m_FireballMaterial, m_FireMaterial, m_ExplosionClip, this);
            Sfx.Play(m_FireballClip, mouth, 1f, 0.7f);
        }

        // Une passe en piqué : une ligne qui passe par le joueur et ne croise aucun pilier.
        bool TryPlanDive()
        {
            var player = ArenaPlayer.Feet;
            player.y = FloorY;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var angle = UnityEngine.Random.value * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                var start = player - direction * m_OrbitRadius;
                var end = player + direction * m_OrbitRadius;
                if (!LineClearOfPillars(player - direction * 6f, player + direction * 6f, m_BodyRadius * 0.7f))
                    continue;

                m_DiveStart = new Vector3(start.x, FloorY + m_FlyAltitude, start.z);
                m_DiveEnd = new Vector3(end.x, FloorY + m_FlyAltitude, end.z);
                m_FromPosition = transform.position;
                m_DiveTimer = m_DiveInterval;
                m_NextTrail = 0.3f;
                ShowWarningLine(player - direction * 7f, player + direction * 7f);
                Sfx.Play(m_RoarClip, transform.position, 1f, 0.55f, 0.6f);
                EnterState(State.Dive, "Fly Glide", 0.3f);
                return true;
            }

            m_DiveTimer = 3f;
            return false;
        }

        void UpdateDive(float deltaTime)
        {
            const float approachTime = 1.6f;
            if (m_StateTime < approachTime)
            {
                // Se place au début de la ligne, en prenant de l'élan.
                var t = Mathf.SmoothStep(0f, 1f, m_StateTime / approachTime);
                transform.position = Vector3.Lerp(m_FromPosition, m_DiveStart, t);
                TurnTowards(m_DiveEnd - m_DiveStart, m_TurnSpeed * 2f, deltaTime);
                return;
            }

            var length = Vector3.Distance(m_DiveStart, m_DiveEnd);
            var progress = Mathf.Clamp01((m_StateTime - approachTime) * m_DiveSpeed / Mathf.Max(1f, length));
            var position = Vector3.Lerp(m_DiveStart, m_DiveEnd, progress);
            var dip = Mathf.Sin(progress * Mathf.PI);
            position.y = FloorY + Mathf.Lerp(m_FlyAltitude, m_DiveAltitude, dip);
            transform.position = position;
            TurnTowards(m_DiveEnd - m_DiveStart, m_TurnSpeed * 2f, deltaTime);

            // Au plus bas, il crache une ligne de feu au sol.
            var burning = progress > 0.22f && progress < 0.78f;
            if (burning && !m_Firing)
            {
                Play("Fly Flame Attack", 0.15f);
                m_BreathAim = (transform.forward + Vector3.down * 0.9f).normalized;
                StartFiring();
            }
            else if (!burning && m_Firing)
            {
                StopBreath();
                Play("Fly Forward", 0.3f);
                HideWarningLine();
            }

            if (m_Firing)
            {
                m_BreathAim = (transform.forward + Vector3.down * 0.9f).normalized;
                DamageInCone(deltaTime, MouthPosition, m_BreathAim, m_DiveAltitude + 4f, m_BreathAngle, m_BreathDamage);
                m_NextTrail -= deltaTime;
                if (m_NextTrail <= 0f)
                {
                    m_NextTrail = 1.4f / Mathf.Max(1f, m_DiveSpeed);
                    var ground = MouthPosition + m_BreathAim * 3f;
                    ground.y = FloorY;
                    FireTrail.Spawn(ground, 1.6f, 3.5f, BaseDamage * m_TrailDamage, m_FireMaterial, Enemy);
                }
            }

            if (progress >= 1f)
            {
                StopBreath();
                HideWarningLine();
                var offset = transform.position - Center;
                offset.y = 0f;
                m_OrbitAngle = Mathf.Atan2(offset.x, offset.z);
                m_FireballTimer = m_FireballInterval;
                EnterState(State.Flying, "Fly Forward");
            }
        }

        // Retour au sol, enragé : il se pose loin du joueur, puis rugit.
        void BeginLanding()
        {
            StopBreath();
            HideWarningLine();
            var away = Center - ArenaPlayer.Feet;
            away.y = 0f;
            var landing = Center + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward) * 6f;
            landing.y = FloorY;
            m_FromPosition = transform.position;
            m_ToPosition = landing;
            EnterState(State.Landing, "Fly Glide", 0.3f);
        }

        void UpdateLanding()
        {
            const float glideTime = 2f;
            const float landTime = 1.5f;
            var above = m_ToPosition + Vector3.up * 4f;
            if (m_StateTime < glideTime)
            {
                transform.position = Vector3.Lerp(m_FromPosition, above, Mathf.SmoothStep(0f, 1f, m_StateTime / glideTime));
                TurnTowards(above - m_FromPosition, m_TurnSpeed, Time.deltaTime);
                return;
            }

            if (m_Playing != "Land")
                Play("Land", 0.2f);

            var t = Mathf.Clamp01((m_StateTime - glideTime) / landTime);
            transform.position = Vector3.Lerp(above, m_ToPosition, t * t);
            TurnTowards(ArenaPlayer.Feet - transform.position, m_TurnSpeed, Time.deltaTime);
            if (t < 1f)
                return;

            Sfx.Play(m_StepClip, transform.position, 1f, 0.5f);
            ArenaPlayer.Rumble(0.5f, 0.3f);
            SetPhase(DragonPhase.Enraged);
            m_RageRoarTimer = m_RageRoarInterval * 0.5f;
            Roar(State.Roar);
            DropRocks();
        }

        void DropRocks()
        {
            if (m_Arena == null)
                return;

            var player = ArenaPlayer.Feet;
            player.y = FloorY;
            for (var i = 0; i < m_RockCount; i++)
            {
                var point = player;
                if (i > 0)
                {
                    // Les autres tombent autour du joueur, puis n'importe où.
                    var spread = i < 3 ? 6f : m_Arena.LavaRingStart - 2f;
                    var found = false;
                    for (var attempt = 0; attempt < 8 && !found; attempt++)
                    {
                        var offset = UnityEngine.Random.insideUnitCircle * spread;
                        var candidate = (i < 3 ? player : Center) + new Vector3(offset.x, 0f, offset.y);
                        candidate.y = FloorY;
                        if (m_Arena.IsFreeGround(candidate, m_RockRadius * 0.5f))
                        {
                            point = candidate;
                            found = true;
                        }
                    }

                    if (!found)
                        continue;
                }

                var ceiling = m_Arena.CeilingPoint(point, out _);
                FallingRock.Spawn(point, ceiling, m_RockWarning + i * 0.15f, BaseDamage * m_RockDamage, m_RockRadius,
                                  m_RockMaterial, m_WarningMaterial, m_FireMaterial, m_RockClip, Enemy);
            }
        }

        // ---------------------------------------------------------------- fin de vague et mort

        public override void BeginFlee(Vector3 exitPoint)
        {
            // Le joueur est mort : le dragon reste là, en vol ou au sol, et rugit une dernière fois.
            StopBreath();
            HideWarningLine();
            if (IsAirborne)
                Play("Fly Float", 0.3f);
            else
                Play("Scream", 0.3f, restart: true);
        }

        public override void TickFlee(float deltaTime)
        {
        }

        public override void OnDied()
        {
            StopBreath();
            HideWarningLine();
            var wasAirborne = IsAirborne;
            m_State = State.Dead;
            SetPhase(DragonPhase.Dead);
            Play("Die", 0.15f, restart: true);
            Sfx.Play(m_DeathClip, MouthPosition, 1f, 0.4f, 0.6f);
            Sfx.Play(m_RoarClip, MouthPosition, 1f, 0.35f, 0.6f);
            ArenaPlayer.Rumble(0.8f, 0.8f);
            m_FromPosition = transform.position;
            m_StateTime = wasAirborne ? 0f : 10f;
        }

        public override bool TickCorpse(float deltaTime)
        {
            // Mort en vol : il tombe jusqu'au sol.
            if (transform.position.y > FloorY + 0.01f)
            {
                m_StateTime += deltaTime;
                var y = Mathf.MoveTowards(transform.position.y, FloorY, (2f + m_StateTime * 9.81f) * deltaTime);
                transform.position = new Vector3(transform.position.x, y, transform.position.z);
                if (Mathf.Approximately(y, FloorY))
                    Sfx.Play(m_StepClip, transform.position, 1f, 0.4f);
            }

            return true;
        }

        // ---------------------------------------------------------------- outils

        void SetPhase(DragonPhase phase)
        {
            if (m_Phase == phase)
                return;

            m_Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        void EnterState(State state, string animation = null, float fade = 0.25f, bool restart = false)
        {
            m_State = state;
            m_StateTime = 0f;
            if (!string.IsNullOrEmpty(animation))
                Play(animation, fade, restart);
        }

        void Play(string state, float fade = 0.25f, bool restart = false)
        {
            if (m_Animator == null || (!restart && m_Playing == state))
                return;

            m_Playing = state;
            m_Animator.CrossFadeInFixedTime(state, fade, 0, 0f);
        }

        Vector3 MouthPosition
        {
            get
            {
                if (m_Head == null)
                    return transform.position + transform.forward * 3f + Vector3.up * 2.5f;
                var neck = m_Neck != null ? m_Neck.position : m_Head.position - transform.forward;
                var snout = m_Head.position - neck;
                return m_Head.position + snout * 0.8f;
            }
        }

        // Le souffle part vers l'avant du dragon, jamais dans son dos.
        Vector3 ClampToFacing(Vector3 direction, float maxAngle)
        {
            var forward = transform.forward;
            return Vector3.Angle(forward, direction) <= maxAngle
                ? direction
                : Vector3.RotateTowards(forward, direction, maxAngle * Mathf.Deg2Rad, 0f);
        }

        void TurnTowards(Vector3 direction, float degreesPerSecond, float deltaTime)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f)
                return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), degreesPerSecond * deltaTime);
        }

        float FacingAngle(Vector3 target)
        {
            var direction = target - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude < 1e-4f ? 0f : Vector3.Angle(transform.forward, direction);
        }

        // Direction vers la cible, déviée autour des piliers qui sont sur le chemin.
        Vector3 Steer(Vector3 from, Vector3 target)
        {
            var direction = target - from;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f)
                return transform.forward;
            direction.Normalize();
            if (m_Arena == null)
                return direction;

            foreach (var pillar in m_Arena.Pillars)
            {
                var toPillar = pillar.Position - from;
                toPillar.y = 0f;
                var ahead = Vector3.Dot(toPillar, direction);
                if (ahead < 0f || ahead > 9f)
                    continue;

                var lateral = toPillar - direction * ahead;
                var clearance = pillar.Radius + m_BodyRadius;
                if (lateral.magnitude >= clearance)
                    continue;

                var away = lateral.sqrMagnitude > 1e-3f ? -lateral.normalized : Vector3.Cross(Vector3.up, direction);
                direction = (direction + away * (1.6f * (1f - lateral.magnitude / clearance))).normalized;
            }

            return direction;
        }

        // Garde le dragon sur le sol de l'arène et hors des piliers.
        void KeepInsideArena()
        {
            if (m_Arena == null)
                return;

            var position = transform.position;
            var offset = position - Center;
            offset.y = 0f;
            var limit = m_Arena.LavaRingStart - m_BodyRadius * 0.5f;
            if (offset.magnitude > limit)
                position = Center + offset.normalized * limit;

            foreach (var pillar in m_Arena.Pillars)
            {
                var away = position - pillar.Position;
                away.y = 0f;
                var minimum = pillar.Radius + m_BodyRadius * 0.6f;
                if (away.magnitude < minimum && away.sqrMagnitude > 1e-4f)
                    position = pillar.Position + away.normalized * minimum;
            }

            position.y = FloorY;
            transform.position = position;
        }

        bool LineClearOfPillars(Vector3 a, Vector3 b, float margin)
        {
            if (m_Arena == null)
                return true;

            foreach (var pillar in m_Arena.Pillars)
            {
                var segment = b - a;
                segment.y = 0f;
                var toPillar = pillar.Position - a;
                toPillar.y = 0f;
                var t = Mathf.Clamp01(Vector3.Dot(toPillar, segment) / Mathf.Max(1e-4f, segment.sqrMagnitude));
                var closest = a + segment * t;
                closest.y = pillar.Position.y;
                if (Vector3.Distance(closest, pillar.Position) < pillar.Radius + margin)
                    return false;
            }

            return true;
        }

        // Rien ne protège le joueur : pas de pilier, de rocher ou de paroi entre la gueule et lui.
        bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            var distance = direction.magnitude;
            if (distance < 0.1f)
                return true;

            var hits = Physics.RaycastAll(from, direction / distance, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                var other = hit.collider;
                if (other == null || ArrowIgnore.IsIgnored(other) || other.transform.IsChildOf(transform))
                    continue;
                if (other.GetComponentInParent<PlayerRig>() != null || other.GetComponentInParent<Arrow>() != null || other.GetComponentInParent<DragonFireball>() != null)
                    continue;
                return false;
            }

            return true;
        }

        Vector3 OrbitPoint(float angle) =>
            Center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * m_OrbitRadius + Vector3.up * (m_FlyAltitude + Mathf.Sin(Time.time * 0.8f) * 0.6f);

        static Vector3 Tangent(float angle) => new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle));

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void ShowWarningLine(Vector3 from, Vector3 to)
        {
            HideWarningLine();
            if (m_WarningMaterial == null)
                return;

            var builder = new MeshBuilder();
            var direction = to - from;
            direction.y = 0f;
            var side = Vector3.Cross(Vector3.up, direction.normalized) * 1.4f;
            var lift = Vector3.up * 0.07f;
            var red = new Color(1f, 0.2f, 0.05f, 0.45f);
            builder.Quad(from - side + lift, to - side + lift, to + side + lift, from + side + lift, red);

            var go = new GameObject("Alerte de piqué");
            go.AddComponent<MeshFilter>().sharedMesh = builder.ToMesh("Alerte de piqué");
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_WarningMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_WarningLine = go.transform;
        }

        void HideWarningLine()
        {
            if (m_WarningLine == null)
                return;

            var filter = m_WarningLine.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                Destroy(filter.sharedMesh);
            Destroy(m_WarningLine.gameObject);
            m_WarningLine = null;
        }

        // ---------------------------------------------------------------- construction

        Transform FindBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName))
                return null;

            var bone = FindDeep(transform, boneName);
            if (bone == null)
                Debug.LogWarning($"DragonBoss : os « {boneName} » introuvable dans le modèle.", this);
            return bone;
        }

        static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var found = FindDeep(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        void SetHitboxesEnabled(bool enabled)
        {
            foreach (var hitbox in m_Hitboxes)
            {
                if (hitbox != null)
                    hitbox.enabled = enabled;
            }
        }

        // Zones de touche accrochées aux os : leur taille suit l'écart entre les os, donc la taille du modèle.
        void BuildHitboxes()
        {
            var health = GetComponent<Health>();
            var scale = Mathf.Max(0.1f, m_HitboxScale);

            if (m_Head != null && m_Neck != null)
            {
                var length = Vector3.Distance(m_Head.position, m_Neck.position);
                var snout = (m_Head.position - m_Neck.position).normalized;
                AddSphere("Zone tête", m_Head, m_Head.position + snout * length * 0.5f, length * 0.75f * scale, HitZone.Head, 2f, health);
            }

            AddChain(new[] { "Neck01", "Neck02", "Neck03", m_HeadBone }, 0.35f, HitZone.Body, health, scale);
            if (m_Spine != null && m_Chest != null)
            {
                var length = Vector3.Distance(m_Spine.position, m_Chest.position);
                AddCapsule("Zone corps", m_Spine, m_Spine.position, m_Chest.position, length * 0.65f * scale, HitZone.Body, 1f, health);
                AddSphere("Zone poitrine", m_Chest, m_Chest.position, length * 0.7f * scale, HitZone.Body, 1f, health);
            }

            AddChain(new[] { "UpperArm_L", "LowerArm_L", "Hand_L" }, 0.3f, HitZone.Body, health, scale);
            AddChain(new[] { "UpperArm_R", "RowerArm_R", "Hand_R" }, 0.3f, HitZone.Body, health, scale);
            AddChain(new[] { "UpperLeg_L", "LowerLeg_L", "Feet_L" }, 0.3f, HitZone.Body, health, scale);
            AddChain(new[] { "UpperReg_R", "RowerReg_R", "Feet_R" }, 0.3f, HitZone.Body, health, scale);
            BuildWeakPoint(health, scale);
        }

        // Point faible : une boule cyan qui pulse sous la poitrine (dégâts ×3), comme les autres boss.
        void BuildWeakPoint(Health health, float scale)
        {
            if (m_Chest == null || m_Spine == null)
                return;

            var bodyLength = Vector3.Distance(m_Spine.position, m_Chest.position);
            var forward = m_NeckBase != null ? (m_NeckBase.position - m_Chest.position).normalized : transform.forward;
            var position = m_Chest.position + forward * bodyLength * 0.3f - transform.up * bodyLength * 0.45f;
            var radius = bodyLength * 0.28f * scale;

            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Point faible";
            orb.transform.SetParent(m_Chest, true);
            orb.transform.position = position;
            orb.transform.localScale = Vector3.one;
            orb.transform.localScale = Vector3.one * (radius * 2f) / Mathf.Max(1e-4f, orb.transform.lossyScale.x);
            var renderer = orb.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = m_WeakPointMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            orb.AddComponent<Pulse>();

            var collider = orb.GetComponent<SphereCollider>();
            collider.radius = 0.6f;   // un peu plus grand que la boule visible (rayon 0,5)
            var hitbox = orb.AddComponent<Hitbox>();
            hitbox.Configure(HitZone.WeakPoint, 3f, health);
            m_Hitboxes.Add(collider);
        }

        void AddChain(string[] bones, float radiusFactor, HitZone zone, Health health, float scale)
        {
            for (var i = 0; i < bones.Length - 1; i++)
            {
                var from = FindDeep(transform, bones[i]);
                var to = FindDeep(transform, bones[i + 1]);
                if (from == null || to == null)
                    continue;

                var length = Vector3.Distance(from.position, to.position);
                if (length < 1e-3f)
                    continue;
                AddCapsule($"Zone {bones[i]}", from, from.position, to.position, length * radiusFactor * scale, zone, 1f, health);
            }
        }

        void AddSphere(string name, Transform bone, Vector3 center, float radius, HitZone zone, float multiplier, Health health)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bone, false);
            go.transform.position = center;
            var collider = go.AddComponent<SphereCollider>();
            collider.radius = radius / Mathf.Max(1e-4f, go.transform.lossyScale.x);
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Configure(zone, multiplier, health);
            m_Hitboxes.Add(collider);
        }

        void AddCapsule(string name, Transform bone, Vector3 from, Vector3 to, float radius, HitZone zone, float multiplier, Health health)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bone, false);
            go.transform.position = (from + to) * 0.5f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
            var lossy = Mathf.Max(1e-4f, go.transform.lossyScale.x);
            var collider = go.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = radius / lossy;
            collider.height = (Vector3.Distance(from, to) + radius * 2f) / lossy;
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Configure(zone, multiplier, health);
            m_Hitboxes.Add(collider);
        }

        void BuildBreath()
        {
            if (m_FireMaterial == null)
                return;

            m_Breath = FireEffects.CreateBreath(transform, m_FireMaterial, m_BreathRange, m_BreathAngle);

            var lightObject = new GameObject("Lueur du souffle");
            lightObject.transform.SetParent(transform, false);
            m_BreathLight = lightObject.AddComponent<Light>();
            m_BreathLight.type = LightType.Point;
            m_BreathLight.color = new Color(1f, 0.5f, 0.15f);
            m_BreathLight.range = 14f;
            m_BreathLight.intensity = 4f;
            m_BreathLight.shadows = LightShadows.None;
            m_BreathLight.enabled = false;

            m_FireSource = gameObject.AddComponent<AudioSource>();
            m_FireSource.loop = true;
            m_FireSource.playOnAwake = false;
            m_FireSource.spatialBlend = 1f;
            m_FireSource.minDistance = 4f;
            m_FireSource.maxDistance = 40f;
            m_FireSource.volume = 1f;
            m_FireSource.pitch = 0.75f;
            m_FireSource.outputAudioMixerGroup = Sfx.Output;
        }
    }
}
