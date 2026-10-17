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

        /// <summary>Phase 1, au sol : boules de feu, souffle, coup de patte, morsure.</summary>
        Ground,

        /// <summary>Phase 2, en vol : boules de feu, triple ruée enflammée, souffle depuis un perchoir.</summary>
        Air,

        /// <summary>Phase 3, enragé : il passe vite du sol au vol, attaque plus vite, et fait son salto.</summary>
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
    /// <item>Presque tout s'esquive en bougeant bien : la boule de feu (se déplacer), le coup de patte (sauter), le
    /// souffle (passer sur son flanc ou derrière un pilier), les ruées (sortir de la bande rouge), le salto (quitter
    /// l'endroit où l'on était). Seule la morsure, tout contre sa gueule, est presque inévitable.</item>
    /// <item>Il joue lui-même les animations du modèle, par leur nom (Animator « DragonBoss », sans transitions). La tête
    /// levée de la charge du souffle, le coup de patte et le salto « raté » sont des poses ajoutées par le code.</item>
    /// <item>Les ralentissements (flèches de glace…) ne lui retirent jamais plus de 25 % de sa vitesse (Max Slow) ; ils
    /// ralentissent tout : déplacements, animations et attaques. Enragé, tout va plus vite (Rage Tempo).</item>
    /// <item>Zones de touche créées au lancement sur les os du modèle : tête (×2), cou, corps, pattes, et un point faible
    /// cyan sous la poitrine (×3). <see cref="m_HitboxScale"/> les agrandit ou les réduit toutes.</item>
    /// <item>Les dégâts de toutes ses attaques partent de l'« Attack Damage » de sa définition, multiplié par la difficulté
    /// et la vague, comme les autres ennemis.</item>
    /// </list>
    /// </remarks>
    [DefaultExecutionOrder(-50)]   // zones de touche créées avant l'Awake d'Enemy, qui les recense
    public partial class DragonBoss : EnemyBehaviour
    {
        enum State
        {
            Sleeping,
            IntroDash,
            IntroWait,
            IntroBreath,
            Waking,
            Roar,
            Moving,
            Sweep,
            Bite,
            Slam,
            Spit,
            BreathCharge,
            Breath,
            TakeOff,
            Flying,
            Dash,
            PerchFlight,
            Perched,
            Leap,
            Salto,
            Recover,
            Landing,
            Idle,
            Dead,
        }

        enum PerchPurpose
        {
            Breath,
            Salto,
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

        [Tooltip("Os de la mâchoire, qui s'ouvre pendant les charges.")]
        [SerializeField]
        string m_JawBone = "Jaw01";

        [Tooltip("Rayon (m) du corps au sol : distance gardée avec les piliers et le bord de l'arène.")]
        [SerializeField]
        float m_BodyRadius = 2.5f;

        [Tooltip("Multiplie la taille de toutes les zones de touche (à régler si les flèches traversent le dragon ou le touchent dans le vide).")]
        [SerializeField]
        float m_HitboxScale = 1f;

        [SerializeField]
        Material m_WeakPointMaterial;

        [Tooltip("Couleur de la gemme de son point faible (cyan glacé : elle tranche avec le feu).")]
        [SerializeField]
        Color m_WeakPointColor = new Color(0.45f, 0.9f, 1f);

        [Tooltip("Matériau des flammes (particules).")]
        [SerializeField]
        Material m_FireMaterial;

        [Tooltip("Matériau des boules de feu et de la lueur dans la gueule (Dragon_Fireball).")]
        [SerializeField]
        Material m_FireballMaterial;

        [Tooltip("Matériau des alertes au sol (bandes des ruées, cercle du salto).")]
        [SerializeField]
        Material m_WarningMaterial;

        [SerializeField]
        ArenaShape m_Arena;

        [Header("Yeux")]
        [Tooltip("Matériau des yeux, rouge brillant (Dragon_Eyes). Vide : pas d'yeux brillants.")]
        [SerializeField]
        Material m_EyeMaterial;

        [Tooltip("Rayon (m) d'un œil.")]
        [SerializeField]
        float m_EyeRadius = 0.09f;

        [Tooltip("Décalage (m) des yeux depuis leur os : X vers l'extérieur de la tête, Y vers le haut, Z vers l'avant.")]
        [SerializeField]
        Vector3 m_EyeOffset = new Vector3(0.05f, 0.02f, 0.03f);

        [Tooltip("Intensité de la petite lumière rouge que les yeux jettent sur le museau.")]
        [SerializeField]
        float m_EyeLightIntensity = 1.5f;

        [Header("Givre")]
        [Tooltip("Voile bleuté posé sur le dragon quand une boule de glace le touche (Frost_Overlay). Vide : pas de teinte.")]
        [SerializeField]
        Material m_FrostOverlayMaterial;

        [Tooltip("Force de la teinte au coup (0 à 1) ; elle s'efface au même rythme que le ralentissement.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_FrostTint = 0.6f;

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

        [Header("Rythme")]
        [Tooltip("Part de sa vitesse que les ralentissements peuvent lui retirer, toutes flèches confondues (0,25 = 25 % au plus).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_MaxSlow = 0.25f;

        [Tooltip("Répit (s) après une attaque au sol, avant la suivante.")]
        [SerializeField]
        float m_Recovery = 0.7f;

        [Header("Au sol")]
        [Tooltip("Vitesse de rotation (°/s).")]
        [SerializeField]
        float m_TurnSpeed = 90f;

        [Tooltip("Portée de la tête (m) en plus de la distance du pivot à la tête : en deçà, le joueur est « proche ».")]
        [SerializeField]
        float m_MeleeExtraReach = 1.8f;

        [Tooltip("Distance (m) qu'il garde avec le joueur pour lui cracher ses boules de feu.")]
        [SerializeField]
        float m_PreferredDistance = 10f;

        [Tooltip("Après une attaque, chance qu'il fonce au contact pendant quelques secondes.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_CloseInChance = 0.35f;

        [Tooltip("Vitesse (m/s) à laquelle l'animation de marche avance quand elle est jouée normalement : l'animation est " +
                 "ralentie ou accélérée pour que les pattes suivent le sol. 0 : mesurée sur l'animation au lancement.")]
        [SerializeField]
        float m_WalkStrideSpeed;

        [Tooltip("Pareil pour la course. 0 : mesurée au lancement.")]
        [SerializeField]
        float m_RunStrideSpeed;

        [Tooltip("Au-delà de cette vitesse (m/s), il court au lieu de marcher.")]
        [SerializeField]
        float m_RunFrom = 3.6f;

        [Header("Boule de feu (son attaque la plus courante)")]
        [Tooltip("Diamètre (m) d'une boule de feu.")]
        [SerializeField]
        float m_FireballSize = 1.8f;

        [Tooltip("Vitesse (m/s) au sol : elle part vers l'endroit où est le joueur au tir, on l'esquive en se déplaçant.")]
        [SerializeField]
        float m_FireballSpeed = 11f;

        [Tooltip("Vitesse (m/s) depuis les airs.")]
        [SerializeField]
        float m_AirFireballSpeed = 14f;

        [Tooltip("Dégâts d'un coup direct, en part de l'« Attack Damage ». À côté de l'explosion, 25 à 50 % de ces dégâts.")]
        [SerializeField]
        float m_FireballDamage = 0.55f;

        [Tooltip("Rayon (m) de l'explosion quand elle s'écrase à côté du joueur.")]
        [SerializeField]
        float m_FireballSplash = 2f;

        [Tooltip("Temps (s) pendant lequel elle se forme dans sa gueule avant de partir.")]
        [SerializeField]
        float m_SpitCharge = 0.7f;

        [Tooltip("Temps (s) entre deux boules de feu au sol.")]
        [SerializeField]
        float m_SpitCooldown = 2.6f;

        [Tooltip("Temps (s) entre deux boules de feu en vol.")]
        [SerializeField]
        float m_AirFireballInterval = 2.2f;

        [Header("Coup de patte (proche : sauter pour l'esquiver)")]
        [Tooltip("Rayon (m) balayé au ras du sol devant lui.")]
        [SerializeField]
        float m_SweepReach = 6.5f;

        [Tooltip("Temps (s) pendant lequel il lève la patte.")]
        [SerializeField]
        float m_SweepWindup = 0.9f;

        [Tooltip("Durée (s) du balayage, d'un côté à l'autre.")]
        [SerializeField]
        float m_SweepTime = 0.4f;

        [Tooltip("Dégâts, en part de l'« Attack Damage ».")]
        [SerializeField]
        float m_SweepDamage = 0.7f;

        [SerializeField]
        float m_SweepCooldown = 5f;

        [Tooltip("Hauteur (m) des pieds au-dessus du sol pour passer par-dessus la patte.")]
        [SerializeField]
        float m_JumpClearance = 0.3f;

        [Tooltip("Il avance de quelques mètres (m) pendant le balayage.")]
        [SerializeField]
        float m_SweepLunge = 1.5f;

        [Header("Morsure (tout près de sa gueule)")]
        [Tooltip("Distance (m) entre sa gueule et le joueur pour qu'il croque.")]
        [SerializeField]
        float m_BiteRange = 2.8f;

        [Tooltip("Temps (s) avant que les mâchoires se referment : très court, on l'évite difficilement.")]
        [SerializeField]
        float m_BiteWindup = 0.4f;

        [SerializeField]
        float m_BiteDamage = 0.9f;

        [SerializeField]
        float m_BiteCooldown = 3f;

        [Header("Dressé, puis il frappe le sol : anneau de feu (sauter pour l'esquiver)")]
        [SerializeField]
        float m_SlamCooldown = 13f;

        [Tooltip("Temps (s) pendant lequel il se dresse sur ses pattes arrière.")]
        [SerializeField]
        float m_SlamRearTime = 1.1f;

        [Tooltip("Angle (°) dont il se dresse.")]
        [SerializeField]
        float m_SlamRearAngle = 40f;

        [Tooltip("Durée (s) de sa chute sur ses pattes avant.")]
        [SerializeField]
        float m_SlamFallTime = 0.22f;

        [Tooltip("Vitesse (m/s) à laquelle l'anneau de feu s'élargit.")]
        [SerializeField]
        float m_SlamRingSpeed = 10f;

        [Tooltip("Hauteur (m) du mur de feu.")]
        [SerializeField]
        float m_SlamRingHeight = 0.6f;

        [Tooltip("Hauteur (m) des pieds au-dessus du sol pour passer par-dessus l'anneau.")]
        [SerializeField]
        float m_SlamClearance = 0.4f;

        [Tooltip("Dégâts, en part de l'« Attack Damage ».")]
        [SerializeField]
        float m_SlamDamage = 0.6f;

        [Header("Souffle")]
        [SerializeField]
        float m_BreathRange = 16f;

        [Tooltip("Portée (m) du souffle depuis un pilier ou le plateau.")]
        [SerializeField]
        float m_PerchBreathRange = 28f;

        [Tooltip("Demi-angle (°) du cône de feu.")]
        [SerializeField]
        float m_BreathAngle = 16f;

        [Tooltip("Durée (s) de la charge : la tête levée vers la voûte, une lumière de plus en plus vive dans la gueule.")]
        [SerializeField]
        float m_BreathCharge = 3.2f;

        [Tooltip("Angle (°) dont il lève la tête pendant la charge.")]
        [SerializeField]
        float m_ChargePitch = 65f;

        [SerializeField]
        float m_BreathDuration = 2.2f;

        [SerializeField]
        float m_BreathCooldown = 10f;

        [Tooltip("Dégâts du souffle par seconde, en part de l'« Attack Damage ».")]
        [SerializeField]
        float m_BreathDamage = 0.4f;

        [Tooltip("Vitesse (°/s) à laquelle le souffle suit le joueur : on le distance en tournant autour du dragon, de près.")]
        [SerializeField]
        float m_BreathTracking = 18f;

        [Header("En vol")]
        [SerializeField]
        float m_FlyAltitude = 13.5f;

        [SerializeField]
        float m_OrbitRadius = 20f;

        [SerializeField]
        float m_FlySpeed = 8f;

        [Tooltip("Durée (s) de l'envol.")]
        [SerializeField]
        float m_LiftTime = 2.4f;

        [Header("Ruées enflammées (en vol)")]
        [Tooltip("Nombre de ruées enchaînées.")]
        [SerializeField]
        int m_DashCount = 3;

        [SerializeField]
        float m_DashCooldown = 14f;

        [Tooltip("Temps (s) entre l'apparition de la bande rouge au sol et la ruée (un peu moins pour les suivantes).")]
        [SerializeField]
        float m_DashWarning = 1.1f;

        [SerializeField]
        float m_DashSpeed = 24f;

        [Tooltip("Hauteur (m) de la ruée (plus haut s'il doit passer au-dessus d'un pilier).")]
        [SerializeField]
        float m_DashAltitude = 7.5f;

        [Tooltip("Longueur (m) d'une ruée.")]
        [SerializeField]
        float m_DashLength = 32f;

        [Tooltip("Largeur (m) de la bande de feu.")]
        [SerializeField]
        float m_DashWidth = 3.5f;

        [Tooltip("Dégâts quand le feu passe sur le joueur, en part de l'« Attack Damage ».")]
        [SerializeField]
        float m_DashDamage = 0.6f;

        [Tooltip("Dégâts par seconde du feu qui reste au sol, en part de l'« Attack Damage ».")]
        [SerializeField]
        float m_DashFireDamage = 0.25f;

        [Tooltip("Durée (s) du feu au sol après une ruée.")]
        [SerializeField]
        float m_DashFireTime = 1.5f;

        [Header("Perchoir (phase 2)")]
        [Tooltip("Temps (s) entre deux souffles depuis un pilier ou le bord du plateau.")]
        [SerializeField]
        float m_PerchCooldown = 17f;

        [Tooltip("Temps (s) posé avant de charger son souffle.")]
        [SerializeField]
        float m_PerchHold = 0.4f;

        [Header("Enragé (phase 3)")]
        [Tooltip("Tout va plus vite : déplacements, animations et attaques (1,3 = 30 %).")]
        [SerializeField]
        float m_RageTempo = 1.3f;

        [SerializeField]
        float m_RageDamage = 1.1f;

        [Tooltip("Temps (s) au sol avant de s'envoler.")]
        [SerializeField]
        float m_RageGroundTime = 11f;

        [Tooltip("Temps (s) en vol avant de revenir au sol (par son salto).")]
        [SerializeField]
        float m_RageAirTime = 9f;

        [Header("Salto (phase 3)")]
        [Tooltip("Temps (s) entre deux saltos lancés depuis le sol (il en fait aussi un à chaque retour au sol).")]
        [SerializeField]
        float m_SaltoCooldown = 22f;

        [SerializeField]
        int m_SaltoFlips = 3;

        [Tooltip("Durée (s) du triple salto, raide comme un piquet.")]
        [SerializeField]
        float m_SaltoFlipTime = 1.8f;

        [Tooltip("Hauteur (m) gagnée au-dessus du perchoir (moins si la voûte est basse).")]
        [SerializeField]
        float m_SaltoRise = 7f;

        [Tooltip("Arrêt (s) entre le salto et le piqué.")]
        [SerializeField]
        float m_SaltoPause = 0.6f;

        [SerializeField]
        float m_SaltoDiveSpeed = 30f;

        [Tooltip("Rayon (m) de l'impact, autour de l'endroit où était le joueur au début de l'attaque.")]
        [SerializeField]
        float m_SaltoRadius = 4f;

        [SerializeField]
        float m_SaltoDamage = 1.3f;

        [Tooltip("Temps (s) pendant lequel il se relève après l'impact.")]
        [SerializeField]
        float m_SaltoRecover = 1.4f;

        [Header("Sons")]
        [Tooltip("Rugissement sonore au réveil, à la fin de l'intro. Décoché : il se réveille en silence (les rugissements du combat restent).")]
        [SerializeField]
        bool m_WakeRoarSound;

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
        DragonPhase m_Phase = DragonPhase.Asleep;
        string m_Playing;
        float m_StateTime;
        int m_Step;
        float m_Tempo = 1f;
        float m_StrideRate = 1f;
        float m_Reach = 6f;
        bool m_HitDone;
        bool m_Firing;
        Action m_AfterRoar;
        Vector3 m_Velocity;
        Vector3 m_LastPosition;
        Vector3 m_FromPosition;
        Vector3 m_ToPosition;
        float m_CorpseY;
        float m_FallTime;

        // Comptes à rebours, en temps du dragon (ralenti par la glace, accéléré par la rage).
        float m_AttackDelay;
        float m_SpitTimer;
        float m_BreathTimer;
        float m_SweepTimer;
        float m_BiteTimer;
        float m_SlamTimer = 8f;
        float m_SaltoTimer;
        float m_DashTimer;
        float m_PerchTimer;
        float m_AirFireballTimer;
        float m_CloseInTimer;
        float m_FormTimer;

        public static DragonBoss Instance { get; private set; }

        public DragonPhase Phase => m_Phase;

        /// <summary>Le dragon change de phase (réveil, envol, rage, mort).</summary>
        public event Action<DragonPhase> PhaseChanged;

        /// <summary>Ses pattes avant viennent de frapper le sol (slam), à ce point (monde).</summary>
        public event Action<Vector3> Slammed;

        // Pour les lumières de l'arène (ArenaLights) : le sol chauffe avant le souffle, les craquelures flashent avant le slam.

        /// <summary>Charge du souffle (0 à 1) pendant qu'il inspire, -1 sinon.</summary>
        public float BreathChargeProgress => m_State == State.BreathCharge ? Mathf.Clamp01(m_StateTime / Mathf.Max(0.1f, m_BreathCharge)) : -1f;

        /// <summary>Il crache son feu en ce moment.</summary>
        public bool IsBreathing => m_Firing;

        /// <summary>Direction du souffle en cours.</summary>
        public Vector3 BreathDirection => m_BreathAim;

        /// <summary>Le bout de sa gueule (monde).</summary>
        public Vector3 Mouth => MouthPosition;

        /// <summary>Portée (m) et demi-angle (°) du souffle.</summary>
        public float BreathReach => BreathRange;

        public float BreathHalfAngle => m_BreathAngle;

        /// <summary>Le slam se prépare : 0 quand il commence à se dresser, 1 quand ses pattes touchent le sol ; -1 sinon.</summary>
        public float SlamProgress => m_State == State.Slam && m_Step == 0
            ? Mathf.Clamp01(m_StateTime / Mathf.Max(0.1f, m_SlamRearTime + m_SlamFallTime))
            : -1f;

        public override Vector3 Velocity => m_Velocity;
        public override bool ControlsAnimator => true;
        public override bool NeedsNavMesh => false;

        float BaseDamage => Enemy.Definition.attackDamage * Enemy.DamageMultiplier * (m_Phase == DragonPhase.Enraged ? m_RageDamage : 1f);

        // Vitesse du temps du dragon : ralenti par la glace (25 % au plus), accéléré par la rage.
        float Tempo => Mathf.Max(1f - m_MaxSlow, Enemy.SlowMultiplier) * (m_Phase == DragonPhase.Enraged ? m_RageTempo : 1f);

        // Vitesse de marche, sans le ralentissement (il passe par le temps du dragon).
        float WalkSpeed => Enemy.CurrentSpeed / Mathf.Max(0.05f, Enemy.SlowMultiplier);

        bool IsAirborne => m_State == State.TakeOff || m_State == State.Flying || m_State == State.Dash || m_State == State.PerchFlight ||
                           m_State == State.Landing || m_State == State.Leap || m_State == State.Salto;

        Vector3 Center => m_Arena != null ? m_Arena.Center : Vector3.zero;
        float FloorY => m_Arena != null ? m_Arena.FloorY : transform.position.y;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            Enemy.MaxSlow = m_MaxSlow;
            if (m_Arena == null)
                m_Arena = FindAnyObjectByType<ArenaShape>();
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
            CaptureRestPose();
            BuildHitboxes();
            BuildBreath();
            BuildEyes();
            BuildMouthGlow();
            BuildFrostTint();
            MeasureStrides();
            SetHitboxesEnabled(false);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            DropHeld();
            HideStrip();
            HideCircle();
        }

        /// <summary>Le dragon est caché : aucun de ses rendus n'est dessiné, ni son ombre (intro, avant les cristaux).</summary>
        public bool IsHidden { get; private set; }

        readonly List<Renderer> m_HiddenRenderers = new List<Renderer>();

        /// <summary>Cache ou montre tout le dragon (modèle, yeux, point faible…), ombre comprise.</summary>
        public void SetVisible(bool visible)
        {
            if (visible != IsHidden)
                return;

            IsHidden = !visible;
            if (m_EyeLight != null)
                m_EyeLight.enabled = visible;
            if (!visible)
            {
                foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.enabled)
                        continue;
                    renderer.enabled = false;
                    m_HiddenRenderers.Add(renderer);
                }

                return;
            }

            foreach (var renderer in m_HiddenRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            m_HiddenRenderers.Clear();
        }

        // ---------------------------------------------------------------- une image

        public override void Tick(float deltaTime)
        {
            m_Tempo = Tempo;
            var dt = deltaTime * m_Tempo;
            m_StateTime += dt;
            CountDown(dt);
            m_StrideRate = 1f;

            // Les poses ajoutées (tête levée, patte, salto) sont redemandées à chaque image par l'état en cours.
            ClearPose();
            UpdatePlayerCollision();

            if (!InIntro && m_State != State.Dead && !ArenaPlayer.IsAlive)
                CalmDown();

            switch (m_State)
            {
                case State.Sleeping:
                    break;
                case State.IntroDash:
                    UpdateIntroDash(dt);
                    break;
                case State.IntroWait:
                    UpdateIntroWait(dt);
                    break;
                case State.IntroBreath:
                    UpdateIntroBreath(dt);
                    break;
                case State.Waking:
                    UpdateWaking();
                    break;
                case State.Roar:
                    UpdateRoar();
                    break;
                case State.Moving:
                    UpdateMoving(dt);
                    break;
                case State.Sweep:
                    UpdateSweep(dt);
                    break;
                case State.Bite:
                    UpdateBite(dt);
                    break;
                case State.Slam:
                    UpdateSlam(dt);
                    break;
                case State.Spit:
                    UpdateSpit(dt);
                    break;
                case State.BreathCharge:
                    UpdateBreathCharge(dt);
                    break;
                case State.Breath:
                    UpdateBreath(dt);
                    break;
                case State.TakeOff:
                    UpdateTakeOff(dt);
                    break;
                case State.Flying:
                    UpdateFlying(dt);
                    break;
                case State.Dash:
                    UpdateDash(dt);
                    break;
                case State.PerchFlight:
                    UpdatePerchFlight(dt);
                    break;
                case State.Perched:
                    UpdatePerched(dt);
                    break;
                case State.Leap:
                    UpdateLeap(dt);
                    break;
                case State.Salto:
                    UpdateSalto(dt);
                    break;
                case State.Recover:
                    UpdateRecover(dt);
                    break;
                case State.Landing:
                    UpdateLanding(dt);
                    break;
                case State.Idle:
                    if (ArenaPlayer.IsAlive)
                        EnterState(State.Moving);
                    break;
            }

            UpdateWarnings(deltaTime);
            if (m_Animator != null && m_State != State.Sleeping)
                m_Animator.speed = m_Tempo * m_StrideRate;

            m_Velocity = deltaTime > 0f ? (transform.position - m_LastPosition) / deltaTime : Vector3.zero;
            m_LastPosition = transform.position;
        }

        void CountDown(float dt)
        {
            m_AttackDelay -= dt;
            m_SpitTimer -= dt;
            m_BreathTimer -= dt;
            m_SweepTimer -= dt;
            m_BiteTimer -= dt;
            m_SlamTimer -= dt;
            m_SaltoTimer -= dt;
            m_DashTimer -= dt;
            m_PerchTimer -= dt;
            m_AirFireballTimer -= dt;
            m_CloseInTimer -= dt;
            m_FormTimer -= dt;
        }

        // Le joueur est mort : plus d'attaque. Au sol il s'arrête ; en vol, il tourne au-dessus de l'arène.
        void CalmDown()
        {
            if (m_State == State.Idle || m_State == State.Flying || m_State == State.Perched)
                return;

            StopAttacks();
            if (IsAirborne)
                ResumeOrbit();
            else
                EnterState(State.Idle, "Idle01");
        }

        // ---------------------------------------------------------------- phases

        bool CheckPhaseChange()
        {
            var health = Enemy.Health.Normalized;
            if (m_Phase == DragonPhase.Ground && health <= m_AirPhaseAt)
            {
                // Il rugit, puis s'envole.
                SetPhase(DragonPhase.Air);
                StopAttacks();
                Roar(BeginLift);
                return true;
            }

            if (m_Phase == DragonPhase.Air && health <= m_EnragedPhaseAt)
            {
                // Il se pose sur un perchoir, rugit, et revient au sol par son salto (sans perchoir : il se pose et rugit).
                SetPhase(DragonPhase.Enraged);
                StopAttacks();
                m_RoarOnPerch = true;
                if (!TryFlyToPerch(PerchPurpose.Salto))
                    BeginLanding();
                return true;
            }

            return false;
        }

        // Rugissement : le dragon s'arrête et rugit, puis enchaîne.
        void Roar(Action then)
        {
            StopAttacks();
            m_AfterRoar = then;
            EnterState(State.Roar, "Scream", 0.2f, restart: true);
            PlayRoarSound();
        }

        void PlayRoarSound()
        {
            var mouth = MouthPosition;
            Sfx.Play(m_RoarClip, mouth, 1f, 0.45f, 0.6f);
            Sfx.Play(m_RoarLayerClip, mouth, 0.9f, 0.85f, 0.4f);
            ArenaPlayer.Rumble(0.6f, 0.6f);
        }

        void UpdateRoar()
        {
            if (m_StateTime < 1.8f)
                return;

            var next = m_AfterRoar;
            m_AfterRoar = null;
            if (next != null)
                next();
            else
                EnterState(State.Moving);
        }

        // Arrête tout ce qui est en cours : souffle, boule de feu dans la gueule, alertes au sol ; dressé, il se remet à plat.
        void StopAttacks()
        {
            if (m_State == State.Slam)
                SetRearPose(0f);
            StopBreath();
            StopInhale();
            DropHeld();
            HideStrip();
            HideCircle();
        }

        // ---------------------------------------------------------------- fin de vague et mort

        public override void BeginFlee(Vector3 exitPoint)
        {
            // Le joueur est mort : le dragon reste là, en vol ou au sol, et rugit une dernière fois.
            StopAttacks();
            ClearPose();
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
            StopAttacks();
            ClearPose();
            m_TPose = 0f;
            m_Paw = 0f;
            m_NeckPitch = 0f;
            m_Jaw = 0f;
            m_Glow = 0f;
            m_Rear = 0f;

            // Mort en plein salto ou en piqué : il se remet droit avant de tomber.
            var forward = transform.forward;
            forward.y = 0f;
            transform.rotation = Quaternion.LookRotation(forward.sqrMagnitude > 0.01f ? forward.normalized : m_SaltoYaw * Vector3.forward);
            m_CorpseY = GroundBelow(transform.position);
            m_FallTime = 0f;

            m_State = State.Dead;
            SetPhase(DragonPhase.Dead);
            Play("Die", 0.15f, restart: true);
            Sfx.Play(m_DeathClip, MouthPosition, 1f, 0.4f, 0.6f);
            Sfx.Play(m_RoarClip, MouthPosition, 1f, 0.35f, 0.6f);
            ArenaPlayer.Rumble(0.8f, 0.8f);
        }

        public override bool TickCorpse(float deltaTime)
        {
            if (m_Animator != null)
                m_Animator.speed = 1f;

            // Mort en vol ou sur un perchoir : il tombe jusqu'à ce qui est sous lui.
            if (transform.position.y > m_CorpseY + 0.01f)
            {
                m_FallTime += deltaTime;
                var y = Mathf.MoveTowards(transform.position.y, m_CorpseY, (2f + m_FallTime * 9.81f) * deltaTime);
                transform.position = new Vector3(transform.position.x, y, transform.position.z);
                if (Mathf.Approximately(y, m_CorpseY))
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

        void EnterState(State state, string animation = null, float fade = 0.25f, bool restart = false, float offset = 0f)
        {
            m_State = state;
            m_StateTime = 0f;
            m_Step = 0;
            ClearPose();
            if (!string.IsNullOrEmpty(animation))
                Play(animation, fade, restart, offset);
        }

        void Play(string state, float fade = 0.25f, bool restart = false, float offset = 0f)
        {
            if (m_Animator == null || (!restart && m_Playing == state))
                return;

            m_Playing = state;
            m_Animator.CrossFadeInFixedTime(state, fade, 0, offset);
        }

        // Fin d'une attaque au sol : un répit, et parfois il fonce au contact.
        void EndGroundAttack(float recovery = -1f)
        {
            m_AttackDelay = recovery >= 0f ? recovery : m_Recovery;
            if (UnityEngine.Random.value < m_CloseInChance)
                m_CloseInTimer = 6f;
            EnterState(State.Moving);
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

        // Direction du museau (de la nuque vers le bout de la gueule).
        Vector3 SnoutDirection
        {
            get
            {
                if (m_Head == null || m_Neck == null)
                    return transform.forward;
                var snout = m_Head.position - m_Neck.position;
                return snout.sqrMagnitude > 1e-4f ? snout.normalized : transform.forward;
            }
        }

        // Le feu part vers l'avant du dragon (à maxYaw degrés près sur les côtés), jamais dans son dos. En hauteur, il suit
        // le joueur : depuis un perchoir, il crache vers le bas.
        Vector3 ClampToFacing(Vector3 direction, float maxYaw)
        {
            var flat = new Vector3(direction.x, 0f, direction.z);
            if (flat.sqrMagnitude < 1e-4f)
                return direction;

            var forward = Flat(transform.forward);
            var yaw = Mathf.Clamp(Vector3.SignedAngle(forward, flat, Vector3.up), -maxYaw, maxYaw);
            var pitch = Mathf.Clamp(Mathf.Atan2(direction.y, flat.magnitude) * Mathf.Rad2Deg, -75f, 30f) * Mathf.Deg2Rad;
            var horizontal = Quaternion.AngleAxis(yaw, Vector3.up) * forward;
            return (horizontal * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch)).normalized;
        }

        static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.forward;
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

        // Hauteur du sol (monde) sous ce point de l'arène.
        float GroundHeight(Vector3 point) => m_Arena != null ? m_Arena.GroundHeightAt(point) : FloorY;

        // Hauteur des pieds du joueur au-dessus du sol : il saute.
        float PlayerLift
        {
            get
            {
                var feet = ArenaPlayer.Feet;
                return feet.y - GroundHeight(feet);
            }
        }

        // Le point du sol sous le joueur.
        Vector3 PlayerGround()
        {
            var feet = ArenaPlayer.Feet;
            feet.y = GroundHeight(feet);
            return feet;
        }

        // Ce qu'il y a sous ce point (sol, pilier, plateau), sans compter le dragon lui-même.
        float GroundBelow(Vector3 point)
        {
            var hits = Physics.RaycastAll(point + Vector3.up, Vector3.down, 80f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.NegativeInfinity;
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform) || ArrowIgnore.IsIgnored(hit.collider))
                    continue;
                if (hit.collider.GetComponentInParent<PlayerRig>() != null)
                    continue;
                best = Mathf.Max(best, hit.point.y);
            }

            return float.IsNegativeInfinity(best) ? GroundHeight(point) : Mathf.Min(best, point.y);
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

            var position = m_Arena.Confine(transform.position, m_BodyRadius * 0.5f);

            foreach (var pillar in m_Arena.Pillars)
            {
                var away = position - pillar.Position;
                away.y = 0f;
                var minimum = pillar.Radius + m_BodyRadius * 0.6f;
                if (away.magnitude < minimum && away.sqrMagnitude > 1e-4f)
                    position = pillar.Position + away.normalized * minimum;
            }

            position.y = m_Arena.GroundHeightAt(position);
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

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
