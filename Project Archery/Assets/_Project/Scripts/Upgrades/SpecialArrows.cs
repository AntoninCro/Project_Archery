using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Defense;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Upgrades
{
    /// <summary>
    /// Flèches spéciales des améliorations (GDD, section 6.2), d'après les <see cref="PlayerUpgrades"/> :
    /// <list type="bullet">
    /// <item>au tir : multitir (flèches en plus, en éventail) et tir écho (la flèche se répète, depuis là où elle est rendue) ;</item>
    /// <item>pour chaque flèche, y compris les flèches en plus : foudre, explosion, glace et perçage, et déluge (la flèche se
    /// divise en vol) ;</item>
    /// <item>en vol : auto-visée ;</item>
    /// <item>à l'impact : foudre et chaîne d'éclairs, explosion, zone de glace, ricochet.</item>
    /// </list>
    /// Depuis le 26 octobre (réglages du joueur), plus de dés pour la foudre, la glace, l'explosion et le perçage : ils jouent à
    /// chaque tir que permet leur déclencheur (tir parfait ou non, réglage <i>Trigger</i> du catalogue), avec des valeurs par
    /// niveau (champs « … By Level »). Le multitir, l'écho et le déluge gardent leurs nombres moyens (valeurs du catalogue).
    /// Les flèches nées d'une division en vol ne se divisent pas à leur tour.
    /// Les failles du katana s'en servent aussi (24 octobre) : <see cref="RollEffects"/>, <see cref="Impact"/>,
    /// <see cref="Blast"/> et les réglages de l'écho, du déluge, du ricochet et de l'auto-visée ; le katana fait le reste
    /// (<see cref="Archery.Combat.Katana"/>, <see cref="Archery.Combat.MatrixRift"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class SpecialArrows : MonoBehaviour
    {
        [Header("Flèches en plus (multitir, écho, déluge)")]
        [Tooltip("Angle (°) entre deux flèches d'une même volée du multitir (2 depuis le 26 octobre au soir, demandé par le " +
                 "joueur : le multitir serré, la corde triple large ; 4 avant).")]
        [SerializeField]
        float m_SplitAngle = 2f;

        [Tooltip("Demi-ouverture maximale (°) de l'éventail : avec beaucoup de flèches, elles se resserrent.")]
        [SerializeField]
        float m_MaxFanAngle = 25f;

        [Tooltip("Écart (m) de la flèche « jumelle » : avec un nombre pair de flèches, celle qui n'a pas de paire " +
                 "part juste à côté de la flèche du milieu, dans la même direction.")]
        [SerializeField]
        float m_TwinOffset = 0.2f;

        [Tooltip("Délai (s) entre deux échos de la volée.")]
        [SerializeField]
        float m_EchoDelay = 0.25f;

        [Tooltip("Optionnel : son du tir écho (par exemple bow_release).")]
        [SerializeField]
        AudioClip m_EchoClip;

        [Tooltip("Moment (s après son départ) où une flèche du déluge se divise, tiré entre ces deux valeurs.")]
        [SerializeField]
        Vector2 m_DelugeSplitTime = new Vector2(0.15f, 0.3f);

        [Tooltip("Écart (°) des flèches nées d'une division en vol.")]
        [SerializeField]
        float m_DelugeSpread = 5f;

        [Tooltip("Optionnel : son de la division en vol.")]
        [SerializeField]
        AudioClip m_DelugeClip;

        [Tooltip("Sécurité pour la fluidité du jeu : au-delà de ce nombre de flèches en vol, on n'en ajoute plus (1000 depuis le " +
                 "27 octobre au soir, choix du joueur : la marée de flèches est voulue ; c'est le budget d'effets qui garde le jeu fluide).")]
        [SerializeField]
        int m_MaxArrowsInFlight = 1000;

        // Depuis le 26 octobre (réglages du joueur), la foudre, la chaîne d'éclairs et la glace jouent à chaque tir parfait,
        // l'explosion et le perçage à chaque tir, sans tirage au sort ; leurs valeurs sont données par niveau (x, y, z : niveaux
        // 1, 2 et 3 ; au-delà, la progression continue).

        [Header("Foudre")]
        [Tooltip("Dégâts de l'éclair de la flèche de foudre, en part des dégâts de la flèche, aux niveaux 1, 2 et 3.")]
        [SerializeField]
        Vector3 m_LightningDamageByLevel = new Vector3(0.5f, 1f, 1.5f);

        [Tooltip("Dégâts de chaque éclair de la chaîne d'éclairs (elle appelle elle-même l'éclair), en part des dégâts de la " +
                 "flèche, aux niveaux 1, 2 et 3 (30 / 60 / 90 % depuis le 27 octobre, réglage du joueur).")]
        [SerializeField]
        Vector3 m_ChainDamageByLevel = new Vector3(0.3f, 0.6f, 0.9f);

        [Tooltip("Vitesse des ennemis foudroyés (0,6 = 40 % plus lents).")]
        [Range(0.1f, 1f)]
        [SerializeField]
        float m_LightningSlow = 0.6f;

        [SerializeField]
        float m_LightningSlowDuration = 2f;

        [Tooltip("Si la flèche ne touche pas d'ennemi, l'éclair frappe l'ennemi le plus proche dans ce rayon (m).")]
        [SerializeField]
        float m_LightningSearchRadius = 2.5f;

        [Tooltip("Distance maximale (m) d'un rebond de la chaîne d'éclairs.")]
        [SerializeField]
        float m_ChainRadius = 8f;

        [SerializeField]
        Color m_LightningColor = new Color(0.45f, 0.75f, 1f);

        [SerializeField]
        AudioClip m_LightningClip;

        // Affaiblie le 26 octobre au soir (demandé par le joueur) : elle joue à chaque tir ; 4/5/6 m et 100/150/200 % le matin.
        [Header("Explosion")]
        [Tooltip("Rayon (m) de l'explosion de la flèche explosive, aux niveaux 1, 2 et 3.")]
        [SerializeField]
        Vector3 m_ExplosionRadiusByLevel = new Vector3(3.5f, 4f, 4.5f);

        [Tooltip("Dégâts au centre de l'explosion, en part des dégâts de la flèche (moitié moins au bord), aux niveaux 1, 2 et 3.")]
        [SerializeField]
        Vector3 m_ExplosionDamageByLevel = new Vector3(0.75f, 1f, 1.25f);

        [SerializeField]
        Color m_ExplosionColor = new Color(1f, 0.5f, 0.12f);

        [Tooltip("Optionnel : effet (système de particules…) créé à l'endroit de l'explosion, détruit au bout de 3 s. Sert " +
                 "seulement sans les deux matériaux ci-dessous.")]
        [SerializeField]
        GameObject m_ExplosionEffect;

        [Tooltip("La vraie explosion (26 octobre au soir, ExplosionFx) : le matériau du feu, additif (Materials/Fire_Particles). " +
                 "Vide : l'ancien effet.")]
        [SerializeField]
        Material m_ExplosionFireMaterial;

        [Tooltip("La vraie explosion : le matériau de la fumée (Art/Grenade/Grenade fumee). Vide : l'ancien effet.")]
        [SerializeField]
        Material m_ExplosionSmokeMaterial;

        [SerializeField]
        AudioClip m_ExplosionClip;

        [Header("Glace")]
        [Tooltip("Rayon (m) de la zone de glace, aux niveaux 1, 2 et 3.")]
        [SerializeField]
        Vector3 m_FrostRadiusByLevel = new Vector3(3f, 4f, 5f);

        [Tooltip("Durée (s) de la zone de glace.")]
        [SerializeField]
        float m_FrostDuration = 5f;

        [Tooltip("Ralentissement des ennemis sur la glace (0,4 = 40 % plus lents), aux niveaux 1, 2 et 3.")]
        [SerializeField]
        Vector3 m_FrostSlowByLevel = new Vector3(0.4f, 0.5f, 0.6f);

        [SerializeField]
        Color m_FrostColor = new Color(0.6f, 0.88f, 1f);

        [SerializeField]
        AudioClip m_FrostClip;

        [Header("Perçage")]
        [Tooltip("Ennemis traversés par chaque flèche, à chaque tir, aux niveaux 1, 2 et 3 (99 : elle traverse tout).")]
        [SerializeField]
        Vector3 m_PierceByLevel = new Vector3(2f, 4f, 99f);

        [Header("Auto-visée")]
        [Tooltip("Distance maximale (m) entre la flèche et l'ennemi vers lequel elle dévie, au niveau 1 (+3 m par niveau depuis le " +
                 "27 octobre, réglage du joueur : 12 / 15 / 18 m).")]
        [SerializeField]
        float m_HomingRange = 12f;

        const float k_HomingRangePerLevel = 3f;

        [Tooltip("Angle maximal (°) entre la direction de la flèche et l'ennemi : la flèche ne fait jamais demi-tour.")]
        [SerializeField]
        float m_HomingAngle = 35f;

        [Header("Ricochet")]
        [Tooltip("Distance maximale (m) jusqu'au prochain ennemi.")]
        [SerializeField]
        float m_RicochetRange = 12f;

        [Tooltip("Part de la vitesse gardée après un rebond (1 depuis le 27 octobre, réglage du joueur ; 0,85 avant).")]
        [Range(0.3f, 1f)]
        [SerializeField]
        float m_RicochetSpeed = 1f;

        [SerializeField]
        AudioClip m_RicochetClip;

        [Header("Rendu")]
        [Tooltip("Matériau des éclairs et de la glace (shader « Archery/UnlitVertexColor »). Vide : créé en jeu.")]
        [SerializeField]
        Material m_EffectMaterial;

        /// <summary>Les effets à l'impact, tirés au sort pour chaque flèche (et chaque faille du katana).</summary>
        [System.Flags]
        public enum Effect
        {
            None = 0,
            Lightning = 1,
            Explosive = 2,
            Frost = 4,
        }

        // Ce qu'on sait d'une flèche tirée : celle de l'arc, ou une flèche en plus.
        class Tracked
        {
            public Effect Effects;
            public float Damage;
            public ShotGrade Grade;
            public int BasePierce;
            public Color Trail;
            public bool OnFire;
            public int Splits;
            public float SplitTime;
            public readonly List<Enemy> RicochetHits = new List<Enemy>();

            // Le tir ricochet : l'ennemi vers lequel la flèche a été déviée (elle le suit, 27 octobre).
            public Enemy RicochetTarget;
        }

        // Tir ricochet (27 octobre ; le joueur : « les déclenchements sont un peu aléatoires » : la flèche déviée partait vers la
        // position de l'ennemi au moment du rebond et ratait ceux qui marchent) : la flèche déviée suit sa cible, à ce virage (°/s).
        const float k_RicochetTurnRate = 720f;

        // Corde triple (26 octobre) : écart (°) entre deux flèches de l'éventail (9 depuis le soir, demandé par le joueur ; 6
        // avant).
        const float k_TripleSpread = 9f;

        // Le duo « Étoile filante » (revu le 27 octobre, demandé par le joueur après un test au casque : avant, autant de flèches
        // que la corde triple sur un cône de 9°, au niveau 1 une au ciel et une dans le sol, on ne voyait pas de couronne) : trois
        // fois plus de flèches que la corde triple seule (6/12/18), sur un cercle de k_CrownRadius m autour du point visé (trouvé
        // par un rayon, borné ; rien devant : k_CrownDefaultDistance).
        const int k_CrownMultiplier = 3;
        const float k_CrownRadius = 2.5f;
        const float k_CrownDefaultDistance = 25f;
        static readonly Vector2 k_CrownDistance = new Vector2(10f, 40f);
        static readonly RaycastHit[] s_CrownHits = new RaycastHit[16];

        // Le duo « Supernova » : combien l'explosion de la flèche dorée est plus forte et plus large.
        const float k_SupernovaScale = 2f;

        // Effets d'explosion réutilisés (26 octobre : la pointe à mèche en fait beaucoup ; avant, chacun était créé puis
        // détruit, un suspect de l'audit de performance du Quest). Au-delà, la plus ancienne repart.
        const int k_MaxExplosionEffects = 6;

        // Une volée à répéter (tir écho). Depuis le 26 octobre (demandé par le joueur), l'écho part de la flèche qu'il répète, là
        // où elle est rendue et dans sa direction : Origin, Direction et Speed suivent la flèche (Source) tant qu'elle vole, puis
        // gardent son dernier état (plantée, elle a touché : l'écho repart de juste avant l'impact).
        struct PendingEcho
        {
            public Arrow Source;
            public Vector3 FanAxis;
            public int Triple;
            public float Time;
            public Vector3 Origin;
            public Vector3 Direction;
            public float Speed;
            public float Damage;
            public ShotGrade Grade;
            public int Pierce;
            public Color Trail;
            public bool OnFire;
            public int ArrowCount;
            public float Gravity;
            public bool Crown;
        }

        readonly Dictionary<Arrow, Tracked> m_Tracked = new Dictionary<Arrow, Tracked>();

        // Auto-visée : les zones de touche de chaque ennemi visé, et les obstacles entre la flèche et la zone.
        const float k_OwnBodyMargin = 0.6f;
        static readonly RaycastHit[] s_SightHits = new RaycastHit[16];
        readonly Dictionary<Enemy, Hitbox[]> m_Hitboxes = new Dictionary<Enemy, Hitbox[]>();
        readonly Stack<Tracked> m_Free = new Stack<Tracked>();
        readonly List<Arrow> m_Splitting = new List<Arrow>();
        readonly List<PendingEcho> m_Echoes = new List<PendingEcho>();
        readonly List<Enemy> m_Enemies = new List<Enemy>();
        readonly List<Enemy> m_Struck = new List<Enemy>();
        readonly List<GameObject> m_ExplosionPool = new List<GameObject>();
        int m_NextExplosion;
        Arrow m_LastLaunched;

        /// <summary>Les flèches spéciales de la scène : les failles du katana s'en servent aussi (24 octobre).</summary>
        public static SpecialArrows Instance { get; private set; }

        /// <summary>Délai (s) entre deux échos d'un tir.</summary>
        public float EchoDelay => m_EchoDelay;

        /// <summary>Moment (s après le départ) où un tir du déluge se divise, tiré entre ces deux valeurs.</summary>
        public Vector2 DelugeSplitTime => m_DelugeSplitTime;

        /// <summary>Distance maximale (m) jusqu'au prochain ennemi d'un ricochet.</summary>
        public float RicochetRange => m_RicochetRange;

        /// <summary>Distance (m) et angle (°) jusqu'où l'auto-visée cherche un ennemi devant le tir.</summary>
        /// <summary>La portée de l'auto-visée au niveau de la pièce équipée (12 / 15 / 18 m avec les réglages de la scène).</summary>
        public float HomingRange
        {
            get
            {
                var upgrades = PlayerUpgrades.Instance;
                var level = upgrades != null ? upgrades.Stacks(UpgradeEffect.Homing) : 0;
                return m_HomingRange + k_HomingRangePerLevel * Mathf.Max(0, level - 1);
            }
        }

        public float HomingAngle => m_HomingAngle;

        // Prêtés aux pièces de la flèche (ArrowPieces, 26 octobre).
        public Material EffectMaterial => m_EffectMaterial;
        public Color FrostColor => m_FrostColor;
        public Color LightningColor => m_LightningColor;
        public AudioClip FrostClip => m_FrostClip;
        public AudioClip LightningClip => m_LightningClip;
        public AudioClip ExplosionClip => m_ExplosionClip;

        // Les matériaux de l'explosion, repris par la mine du tower defense (28 octobre).
        public Material ExplosionFireMaterial => m_ExplosionFireMaterial;
        public Material ExplosionSmokeMaterial => m_ExplosionSmokeMaterial;

        /// <summary>
        /// Une flèche née d'une pièce (26 octobre : les éclats de la pointe éclatée, les flèches du fût détonateur) : une flèche
        /// en plus, qui tire ses propres effets au sort mais ne se divise pas. La renvoie (null s'il y a trop de flèches en vol).
        /// </summary>
        public Arrow LaunchChild(Vector3 origin, Vector3 direction, float speed, float damage, ShotGrade grade, Color trail, float gravity)
        {
            if (!HasRoomFor(1))
                return null;

            m_LastLaunched = null;
            LaunchExtra(origin, direction, speed, damage, grade, 0, trail, false, false, gravity);
            return m_LastLaunched;
        }

        /// <summary>
        /// Une flèche en plus qui compte comme un tir à part entière, avec toutes les pièces (26 octobre au soir : la flèche
        /// fantôme de la corde fantôme) : elle peut se diviser, et ne casse pas le combo si elle rate. Null s'il y a trop de flèches.
        /// </summary>
        public Arrow LaunchExtraShot(Vector3 origin, Vector3 direction, float speed, float damage, ShotGrade grade, Color trail, float gravity)
        {
            if (!HasRoomFor(1))
                return null;

            m_LastLaunched = null;
            LaunchExtra(origin, direction, speed, damage, grade, 0, trail, true, false, gravity);
            return m_LastLaunched;
        }

        void OnEnable()
        {
            Instance = this;

            // Les pièces de la flèche (26 octobre) vivent à côté, sans rien à poser dans la scène : ce qu'elles font, et ce qui
            // se voit sur la flèche et l'arc.
            if (!TryGetComponent<ArrowPieces>(out _))
                gameObject.AddComponent<ArrowPieces>();
            if (!TryGetComponent<PieceLook>(out _))
                gameObject.AddComponent<PieceLook>();
            Bow.ShotFired += OnShotFired;
            Arrow.AnyHit += OnArrowHit;
            Arrow.ShotEnded += OnShotEnded;
            ArrowRain.RainArrowLaunched += OnRainArrow;
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
            Bow.ShotFired -= OnShotFired;
            Arrow.AnyHit -= OnArrowHit;
            Arrow.ShotEnded -= OnShotEnded;
            ArrowRain.RainArrowLaunched -= OnRainArrow;
            m_Tracked.Clear();
            m_Echoes.Clear();
        }

        // Une flèche de la pluie (corde de pluie, arc légendaire) porte les pièces, comme une flèche en plus (27 octobre) : les
        // effets d'impact, le perçage, les pièces qui agissent en vol ou plantées. Elle ne se divise pas (déluge) et ne relance ni
        // volée, ni écho, ni pluie (ceux-là partent du tir de l'arc) : pas de boucle. Au-delà du plafond de flèches suivies, elle
        // tombe sans rien.
        void OnRainArrow(Arrow arrow, float damage, ShotGrade grade)
        {
            if (arrow == null || !HasRoomFor(1))
                return;

            Prepare(arrow, damage, grade, arrow.PierceLeft, arrow.TrailColor, false);
            var pieces = ArrowPieces.Instance;
            if (pieces != null)
                pieces.MarkRainArrow(arrow);
        }

        void Update()
        {
            UpdateEchoes();
            UpdateSplits();
        }

        void FixedUpdate()
        {
            UpdateHoming(Time.fixedDeltaTime);
            UpdateRicochets(Time.fixedDeltaTime);
        }

        // Les flèches déviées par le tir ricochet suivent leur cible, sans tomber (la gravité du pas suivant est annulée d'avance).
        void UpdateRicochets(float deltaTime)
        {
            foreach (var pair in m_Tracked)
            {
                var state = pair.Value;
                var target = state.RicochetTarget;
                if (target == null)
                    continue;

                var arrow = pair.Key;
                if (arrow == null || arrow.CurrentState != Arrow.State.Flying || !target.IsAlive)
                {
                    state.RicochetTarget = null;
                    continue;
                }

                var velocity = arrow.Velocity;
                var speed = velocity.magnitude;
                var toTarget = Center(target) - arrow.TipPosition;
                if (speed < 1f || toTarget.sqrMagnitude < 1e-4f)
                    continue;

                var turned = Vector3.RotateTowards(velocity / speed, toTarget.normalized, k_RicochetTurnRate * Mathf.Deg2Rad * deltaTime, 0f);
                arrow.Velocity = turned * speed - Physics.gravity * (arrow.GravityScale * deltaTime);
            }
        }

        // --- Au tir ---

        void OnShotFired(Bow bow, ShotInfo shot)
        {
            var upgrades = PlayerUpgrades.Instance;
            if (upgrades == null || shot.Arrow == null)
                return;

            // Les pièces de la flèche savent d'où part ce tir (l'arme, la main) avant que ses flèches soient préparées.
            var pieces = ArrowPieces.Instance;
            if (pieces != null)
                pieces.BeginShot(shot);

            // Perçage de l'arc lui-même (arc runique), avant le tirage au sort des améliorations.
            var basePierce = shot.Arrow.PierceLeft;
            var trail = shot.Arrow.TrailColor;
            var volley = 1 + PlayerUpgrades.RollCount(upgrades.MultishotAverageFor(shot.Grade));
            var triple = Mathf.RoundToInt(upgrades.Total(UpgradeEffect.Triple));
            var axis = FanAxis(shot);
            var onFire = shot.Arrow.IsOnFire;
            var gravity = shot.Arrow.GravityScale;

            Prepare(shot.Arrow, shot.Damage, shot.Grade, basePierce, trail, true);
            Fan(shot.Origin, shot.Direction, shot.Speed, shot.Damage, shot.Grade, basePierce, trail, volley, onFire, gravity);

            // Le duo « Étoile filante » (26 octobre au soir, revu le 27) : en l'air ou en glissade, avec le talisman d'acrobate, la
            // corde triple tire en couronne autour du point visé ; l'écho la répète.
            var starfall = triple > 0 && pieces != null && pieces.StarfallReady && ArrowDuos.IsActive(ArrowDuos.Starfall);
            if (starfall)
            {
                Crown(shot.Origin, shot.Direction, shot.Speed, shot.Damage, shot.Grade, basePierce, trail, triple, onFire, gravity);
                LightningBolt.Burst(shot.Origin + shot.Direction.normalized * 1.5f, 0.5f, new Color(1f, 0.9f, 0.55f), m_EffectMaterial, 8);
            }
            else
                FanAround(shot.Origin, shot.Direction, axis, shot.Speed, shot.Damage, shot.Grade, basePierce, trail, triple, onFire, gravity);

            // Tir écho : la même volée, un instant plus tard, autant de fois que tiré au sort.
            var echoes = PlayerUpgrades.RollCount(upgrades.EchoAverageFor(shot.Grade));
            for (var i = 1; i <= echoes; i++)
            {
                m_Echoes.Add(new PendingEcho
                {
                    Source = shot.Arrow,
                    Time = Time.time + m_EchoDelay * i,
                    Origin = shot.Origin,
                    Direction = shot.Direction,
                    Speed = shot.Speed,
                    Damage = shot.Damage,
                    Grade = shot.Grade,
                    Pierce = basePierce,
                    Trail = trail,
                    OnFire = onFire,
                    ArrowCount = volley,
                    Gravity = gravity,
                    FanAxis = axis,
                    Triple = triple,
                    Crown = starfall,
                });
            }
        }

        // L'axe autour duquel s'ouvre l'éventail de la corde triple : le haut de l'arme (les branches de l'arc, le dessus de
        // l'arbalète), à angle droit de la visée ; sans arme connue, la verticale.
        static Vector3 FanAxis(in ShotInfo shot)
        {
            var direction = shot.Direction.sqrMagnitude > 1e-4f ? shot.Direction.normalized : Vector3.forward;
            var axis = Vector3.ProjectOnPlane(shot.Aim != null ? shot.Aim.up : Vector3.up, direction);
            return axis.sqrMagnitude > 1e-4f ? axis.normalized : Vector3.up;
        }

        // Corde triple (26 octobre) : les flèches en plus, par paires, de part et d'autre de la flèche de l'arc, en tournant
        // autour de l'axe des branches : l'arc droit, l'éventail est à plat (contre les groupes) ; l'arc couché, il est
        // vertical (contre les volants, une grande cible).
        void FanAround(Vector3 origin, Vector3 direction, Vector3 axis, float speed, float damage, ShotGrade grade, int pierce, Color trail,
                       int extras, bool onFire, float gravity)
        {
            if (extras <= 0 || direction.sqrMagnitude < 1e-4f)
                return;

            direction.Normalize();
            for (var i = 1; 2 * i - 1 <= extras && HasRoomFor(2); i++)
            {
                var angle = i * k_TripleSpread;
                LaunchExtra(origin, Quaternion.AngleAxis(angle, axis) * direction, speed, damage, grade, pierce, trail, true, onFire, gravity);
                if (2 * i <= extras)
                    LaunchExtra(origin, Quaternion.AngleAxis(-angle, axis) * direction, speed, damage, grade, pierce, trail, true, onFire, gravity);
            }
        }

        // La couronne du duo « Étoile filante » : trois fois les flèches en plus de la corde triple, sur un cercle autour du point
        // visé, à angle droit de la visée (la première à droite, à hauteur d'ennemi). Toutes ont la même vitesse et tombent pareil :
        // le cercle reste centré sur la flèche de l'arc. Elles ne se divisent pas (déluge) : 18 flèches qui se divisent pèseraient
        // trop sur le Quest.
        void Crown(Vector3 origin, Vector3 direction, float speed, float damage, ShotGrade grade, int pierce, Color trail, int extras,
                   bool onFire, float gravity)
        {
            if (extras <= 0 || direction.sqrMagnitude < 1e-4f)
                return;

            direction.Normalize();
            var center = origin + direction * CrownDistance(origin, direction);
            var right = Vector3.Cross(Vector3.up, direction);
            if (right.sqrMagnitude < 1e-4f)
                right = Vector3.Cross(Vector3.forward, direction);
            right.Normalize();
            var up = Vector3.Cross(direction, right);
            var count = extras * k_CrownMultiplier;
            for (var i = 0; i < count && HasRoomFor(1); i++)
            {
                var angle = i * 2f * Mathf.PI / count;
                var target = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * k_CrownRadius;
                LaunchExtra(origin, target - origin, speed, damage, grade, pierce, trail, false, onFire, gravity);
            }
        }

        // Le point visé de la couronne : le premier obstacle droit devant (comme une flèche : sans les triggers ni les planches
        // ignorées), borné ; rien devant (un tir vers le ciel) : 25 m.
        static float CrownDistance(Vector3 origin, Vector3 direction)
        {
            var count = Physics.RaycastNonAlloc(origin, direction, s_CrownHits, k_CrownDistance.y, Physics.DefaultRaycastLayers,
                                                QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var hit = s_CrownHits[i];
                if (hit.collider == null || ArrowIgnore.IsIgnored(hit.collider) || hit.distance < 0.5f)
                    continue;
                best = Mathf.Min(best, hit.distance);
            }

            return best == float.MaxValue ? k_CrownDefaultDistance : Mathf.Clamp(best, k_CrownDistance.x, k_CrownDistance.y);
        }

        // Les flèches en plus d'une volée de « total » flèches (celle du milieu est déjà partie, tout droit).
        // Elles se placent par paires, à gauche et à droite, à égalité : la volée reste centrée sur la visée.
        // Avec un nombre pair de flèches, celle qui n'a pas de paire part juste à côté de celle du milieu, dans la même direction.
        void Fan(Vector3 origin, Vector3 direction, float speed, float damage, ShotGrade grade, int pierce, Color trail, int total, bool onFire,
                 float gravity)
        {
            if (total <= 1 || direction.sqrMagnitude < 1e-4f)
                return;

            direction.Normalize();
            var aim = Quaternion.LookRotation(direction);
            var extras = total - 1;
            if (extras % 2 == 1)
                LaunchExtra(origin + TwinOffset(aim), direction, speed, damage, grade, pierce, trail, true, onFire, gravity);

            var pairs = extras / 2;
            var step = pairs > 0 ? Mathf.Min(m_SplitAngle, m_MaxFanAngle / pairs) : 0f;
            for (var i = 1; i <= pairs && HasRoomFor(2); i++)
            {
                LaunchExtra(origin, aim * Quaternion.Euler(0f, i * step, 0f) * Vector3.forward, speed, damage, grade, pierce, trail, true, onFire, gravity);
                LaunchExtra(origin, aim * Quaternion.Euler(0f, -i * step, 0f) * Vector3.forward, speed, damage, grade, pierce, trail, true, onFire, gravity);
            }
        }

        // La flèche jumelle part à côté de celle du milieu, à gauche ou à droite au hasard.
        Vector3 TwinOffset(Quaternion aim) => aim * new Vector3(Random.value < 0.5f ? -m_TwinOffset : m_TwinOffset, 0f, 0f);

        bool HasRoomFor(int arrows) => m_Tracked.Count + arrows <= m_MaxArrowsInFlight;

        // Une flèche en plus : elle ne casse pas le combo si elle rate, et tire ses propres effets au sort.
        void LaunchExtra(Vector3 origin, Vector3 direction, float speed, float damage, ShotGrade grade, int pierce, Color trail, bool canSplit,
                         bool onFire, float gravity)
        {
            var pool = ArrowPool.Instance;
            if (pool == null || direction.sqrMagnitude < 1e-4f || m_Tracked.Count >= m_MaxArrowsInFlight)
                return;

            direction.Normalize();
            var arrow = pool.Get();
            m_LastLaunched = arrow;
            arrow.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
            arrow.Launch(new ArrowLaunch
            {
                Velocity = direction * speed,
                Damage = damage,
                Grade = grade,
                Pierce = pierce,
                TrailColor = trail,
                IsShot = true,
                IsExtra = true,
                GravityScale = gravity,
            });
            if (onFire && Brazier.Instance != null)
                Brazier.Instance.IgniteArrow(arrow);
            Prepare(arrow, damage, grade, pierce, trail, canSplit);
        }

        // Tire au sort ce que fera cette flèche : foudre, explosion, glace, perçage, division en vol.
        // basePierce : le perçage de l'arc, sans les améliorations (il a déjà été donné à la flèche au lancement).
        void Prepare(Arrow arrow, float damage, ShotGrade grade, int basePierce, Color trail, bool canSplit)
        {
            if (m_Tracked.TryGetValue(arrow, out var previous))
                Release(arrow, previous);

            var state = m_Free.Count > 0 ? m_Free.Pop() : new Tracked();
            state.Effects = Effect.None;
            state.Damage = damage;
            state.Grade = grade;
            state.BasePierce = basePierce;
            state.Trail = trail;
            state.OnFire = arrow.IsOnFire;
            state.Splits = 0;
            state.RicochetHits.Clear();
            state.RicochetTarget = null;
            m_Tracked[arrow] = state;

            var upgrades = PlayerUpgrades.Instance;
            if (upgrades == null)
                return;

            state.Effects = RollEffects(grade);
            if (state.Effects != Effect.None)
                arrow.SetTrailColor(ColorOf(state.Effects));

            arrow.AddPierce(PierceFor(grade));

            if (canSplit)
            {
                state.Splits = PlayerUpgrades.RollCount(upgrades.DelugeAverageFor(grade));
                state.SplitTime = Time.time + Random.Range(m_DelugeSplitTime.x, m_DelugeSplitTime.y);
            }

            var pieces = ArrowPieces.Instance;
            if (pieces != null)
                pieces.Track(arrow, damage, grade);
        }

        /// <summary>
        /// La valeur d'un réglage « par niveau » (x, y, z : niveaux 1, 2 et 3) ; au-delà du niveau 3, la progression des deux
        /// derniers continue. 0 sans niveau.
        /// </summary>
        public static float ByLevel(Vector3 values, int level)
        {
            if (level <= 0)
                return 0f;
            return level switch
            {
                1 => values.x,
                2 => values.y,
                3 => values.z,
                _ => values.z + (values.z - values.y) * (level - 3),
            };
        }

        // L'objet joue pour un tir de cette qualité : équipé, et à ce tir (seulement aux tirs parfaits si son déclencheur le dit).
        static bool Plays(PlayerUpgrades upgrades, UpgradeEffect effect, ShotGrade grade) =>
            upgrades.Stacks(effect) > 0 && (!upgrades.IsPerfectOnly(effect) || grade == ShotGrade.Perfect);

        /// <summary>
        /// Les effets à l'impact d'un tir de cette qualité (foudre, explosion, glace), d'après les améliorations. Depuis le
        /// 26 octobre (réglages du joueur), plus de tirage au sort : chaque objet joue à tous les tirs que son déclencheur
        /// permet (la foudre, la chaîne d'éclairs et la glace aux tirs parfaits, l'explosion à chaque tir).
        /// </summary>
        public Effect RollEffects(ShotGrade grade)
        {
            var upgrades = PlayerUpgrades.Instance;
            if (upgrades == null)
                return Effect.None;

            var effects = Effect.None;

            // La chaîne d'éclairs appelle l'éclair elle-même (la flèche de foudre est aussi une pointe : jamais les deux).
            if (Plays(upgrades, UpgradeEffect.Lightning, grade) || Plays(upgrades, UpgradeEffect.ChainLightning, grade))
                effects |= Effect.Lightning;
            if (Plays(upgrades, UpgradeEffect.Explosive, grade))
                effects |= Effect.Explosive;
            if (Plays(upgrades, UpgradeEffect.Frost, grade))
                effects |= Effect.Frost;
            return effects;
        }

        /// <summary>Ennemis qu'une flèche (ou une faille du katana) de cette qualité traverse en plus : le perçage, par niveau.</summary>
        public int PierceFor(ShotGrade grade)
        {
            var upgrades = PlayerUpgrades.Instance;
            if (upgrades == null || !Plays(upgrades, UpgradeEffect.Piercing, grade))
                return 0;
            return Mathf.RoundToInt(ByLevel(m_PierceByLevel, upgrades.Stacks(UpgradeEffect.Piercing)));
        }

        /// <summary>La couleur d'un tir qui porte ces effets (la traînée d'une flèche, les bords d'une faille).</summary>
        public Color ColorOf(Effect effect)
        {
            if ((effect & Effect.Explosive) != 0)
                return m_ExplosionColor;
            if ((effect & Effect.Lightning) != 0)
                return m_LightningColor;
            return m_FrostColor;
        }

        void UpdateEchoes()
        {
            for (var i = m_Echoes.Count - 1; i >= 0; i--)
            {
                var echo = m_Echoes[i];

                // L'écho suit la flèche qu'il répète, tant qu'elle vole (arrêtée en l'air, elle garde sa dernière direction).
                var source = echo.Source;
                if (source != null && source.CurrentState == Arrow.State.Flying)
                {
                    echo.Origin = source.transform.position;
                    var velocity = source.Velocity;
                    if (velocity.sqrMagnitude > 1f)
                    {
                        echo.Speed = velocity.magnitude;
                        echo.Direction = velocity / echo.Speed;
                    }

                    m_Echoes[i] = echo;
                }

                if (Time.time < echo.Time)
                    continue;

                m_Echoes.RemoveAt(i);
                LaunchExtra(echo.Origin, echo.Direction, echo.Speed, echo.Damage, echo.Grade, echo.Pierce, echo.Trail, true, echo.OnFire, echo.Gravity);
                Fan(echo.Origin, echo.Direction, echo.Speed, echo.Damage, echo.Grade, echo.Pierce, echo.Trail, echo.ArrowCount, echo.OnFire,
                    echo.Gravity);
                if (echo.Crown)
                    Crown(echo.Origin, echo.Direction, echo.Speed, echo.Damage, echo.Grade, echo.Pierce, echo.Trail, echo.Triple, echo.OnFire,
                          echo.Gravity);
                else
                    FanAround(echo.Origin, echo.Direction, echo.FanAxis, echo.Speed, echo.Damage, echo.Grade, echo.Pierce, echo.Trail, echo.Triple,
                              echo.OnFire, echo.Gravity);
                Sfx.Play(m_EchoClip, echo.Origin, 0.7f, 1.1f);
            }
        }

        // Déluge : au moment tiré au sort, la flèche se divise. Elle continue tout droit, et les nouvelles flèches
        // se placent autour d'elle comme dans une volée : par paires (dans un plan tiré au sort), plus une jumelle s'il en reste une.
        void UpdateSplits()
        {
            if (m_Tracked.Count == 0)
                return;

            m_Splitting.Clear();
            foreach (var pair in m_Tracked)
            {
                if (pair.Value.Splits > 0 && Time.time >= pair.Value.SplitTime)
                    m_Splitting.Add(pair.Key);
            }

            foreach (var arrow in m_Splitting)
            {
                if (!m_Tracked.TryGetValue(arrow, out var state))
                    continue;

                var splits = state.Splits;
                state.Splits = 0;
                if (arrow == null || arrow.CurrentState != Arrow.State.Flying)
                    continue;

                var velocity = arrow.Velocity;
                var speed = velocity.magnitude;
                if (speed < 1f)
                    continue;

                var direction = velocity / speed;
                var aim = Quaternion.LookRotation(direction);
                var position = arrow.transform.position;
                if (splits % 2 == 1)
                    LaunchExtra(position + TwinOffset(aim), direction, speed, state.Damage, state.Grade, state.BasePierce, state.Trail, false, state.OnFire,
                                arrow.GravityScale);

                var pairs = splits / 2;
                for (var i = 1; i <= pairs && HasRoomFor(2); i++)
                {
                    var plane = aim * Quaternion.AngleAxis(Random.Range(0f, 180f), Vector3.forward);
                    var angle = m_DelugeSpread * i / pairs;
                    LaunchExtra(position, plane * Quaternion.Euler(0f, angle, 0f) * Vector3.forward, speed,
                                state.Damage, state.Grade, state.BasePierce, state.Trail, false, state.OnFire, arrow.GravityScale);
                    LaunchExtra(position, plane * Quaternion.Euler(0f, -angle, 0f) * Vector3.forward, speed,
                                state.Damage, state.Grade, state.BasePierce, state.Trail, false, state.OnFire, arrow.GravityScale);
                }

                Sfx.Play(m_DelugeClip, position, 0.6f, Random.Range(0.95f, 1.1f));
            }
        }

        void OnShotEnded(Arrow arrow)
        {
            if (m_Tracked.TryGetValue(arrow, out var state))
                Release(arrow, state);

            // Les échos de cette flèche gardent son dernier état : reprise par le pool, elle servira à un autre tir.
            for (var i = 0; i < m_Echoes.Count; i++)
            {
                if (m_Echoes[i].Source != arrow)
                    continue;

                var echo = m_Echoes[i];
                echo.Source = null;
                m_Echoes[i] = echo;
            }
        }

        void Release(Arrow arrow, Tracked state)
        {
            m_Tracked.Remove(arrow);
            state.RicochetHits.Clear();
            m_Free.Push(state);
        }

        // --- En vol ---

        // Auto-visée : la direction tourne doucement vers l'ennemi le plus proche devant la flèche, sur son point faible,
        // sinon sur sa tête (un bon tireur n'est pas dévié de la tête vers le ventre).
        void UpdateHoming(float deltaTime)
        {
            var upgrades = PlayerUpgrades.Instance;
            var turnRate = upgrades != null ? upgrades.HomingTurnRate : 0f;
            if (turnRate <= 0f)
                return;

            var maxRadians = turnRate * Mathf.Deg2Rad * deltaTime;
            foreach (var pair in m_Tracked)
            {
                var arrow = pair.Key;
                if (arrow == null || arrow.CurrentState != Arrow.State.Flying)
                    continue;

                var velocity = arrow.Velocity;
                var speed = velocity.magnitude;
                if (speed < 1f)
                    continue;

                var direction = velocity / speed;
                var position = arrow.transform.position;
                var target = HomingTarget(position, direction);
                if (target == null)
                    continue;

                var toTarget = (AimPoint(target, position) - position).normalized;
                arrow.Velocity = Vector3.RotateTowards(direction, toTarget, maxRadians, 0f) * speed;
            }
        }

        // Point visé : le point faible, sinon la tête, sinon le centre ; seulement s'il se voit depuis la flèche (un point
        // faible dans le dos ne fait pas tourner la flèche dans le ventre).
        Vector3 AimPoint(Enemy enemy, Vector3 from)
        {
            if (!m_Hitboxes.TryGetValue(enemy, out var hitboxes))
            {
                if (m_Hitboxes.Count > 64)
                    m_Hitboxes.Clear();
                hitboxes = enemy.GetComponentsInChildren<Hitbox>();
                m_Hitboxes[enemy] = hitboxes;
            }

            if (TryAimAt(enemy, hitboxes, HitZone.WeakPoint, from, out var point) || TryAimAt(enemy, hitboxes, HitZone.Head, from, out point))
                return point;
            return Center(enemy);
        }

        static bool TryAimAt(Enemy enemy, Hitbox[] hitboxes, HitZone zone, Vector3 from, out Vector3 point)
        {
            foreach (var hitbox in hitboxes)
            {
                if (hitbox == null || hitbox.Zone != zone || !hitbox.isActiveAndEnabled)
                    continue;
                if (!hitbox.TryGetComponent<Collider>(out var collider) || !collider.enabled)
                    continue;

                point = collider.bounds.center;
                if (InSight(enemy, collider, from, point))
                    return true;
            }

            point = default;
            return false;
        }

        // Rien ne cache le point entre la flèche et lui. Le corps du même ennemi a un peu de marge : il englobe souvent
        // le bord de la tête ou de la gemme.
        static bool InSight(Enemy enemy, Collider target, Vector3 from, Vector3 point)
        {
            var offset = point - from;
            var distance = offset.magnitude;
            if (distance < 0.05f)
                return true;

            var count = Physics.RaycastNonAlloc(from, offset / distance, s_SightHits, distance, Physics.DefaultRaycastLayers,
                                                QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = s_SightHits[i];
                if (hit.collider == null || hit.collider == target || ArrowIgnore.IsIgnored(hit.collider))
                    continue;

                var margin = hit.collider.GetComponentInParent<Enemy>() == enemy ? k_OwnBodyMargin : 0.05f;
                if (hit.distance < distance - margin)
                    return false;
            }

            return true;
        }

        Enemy HomingTarget(Vector3 position, Vector3 direction)
        {
            Enemy best = null;
            var bestDistance = HomingRange;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null)
                    continue;

                var offset = Center(enemy) - position;
                var distance = offset.magnitude;
                if (distance > bestDistance || Vector3.Angle(direction, offset) > m_HomingAngle)
                    continue;

                best = enemy;
                bestDistance = distance;
            }

            return best;
        }

        // --- À l'impact ---

        void OnArrowHit(ArrowHit hit)
        {
            if (hit.Arrow == null || !hit.IsShot || !m_Tracked.TryGetValue(hit.Arrow, out var state))
                return;

            var enemy = hit.Collider != null ? hit.Collider.GetComponentInParent<Enemy>() : null;

            // Chaque effet ne se déclenche qu'une fois, même si la flèche traverse ou rebondit.
            var effects = state.Effects;
            state.Effects = Effect.None;
            Impact(effects, hit, enemy);

            if (enemy != null)
                TryRicochet(hit, enemy, state);
        }

        /// <summary>
        /// Les effets d'un tir qui touche (<paramref name="hit"/>, sur <paramref name="enemy"/> s'il y en a un) : foudre et
        /// chaîne d'éclairs, explosion, zone de glace. Pour les flèches, et pour les failles du katana (24 octobre).
        /// </summary>
        public void Impact(Effect effects, in ArrowHit hit, Enemy enemy)
        {
            if ((effects & Effect.Lightning) != 0)
                StrikeLightning(hit, enemy);
            if ((effects & Effect.Explosive) != 0)
                Explode(hit);
            if ((effects & Effect.Frost) != 0)
                Freeze(hit, enemy);
        }

        /// <summary>
        /// Une explosion qui n'est pas celle d'une amélioration (la fusion de failles du katana) : <paramref name="damage"/> au
        /// centre, moitié moins au bord du rayon <paramref name="radius"/> (m), avec l'effet et le son de l'explosion.
        /// </summary>
        public void Blast(Vector3 point, float damage, float radius, Color color, in ArrowHit hit)
        {
            ExplosionVisual(point, radius, color);
            Sfx.Play(m_ExplosionClip, point);

            m_Enemies.Clear();
            m_Enemies.AddRange(Enemy.Alive);
            foreach (var enemy in m_Enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var center = Center(enemy);
                var distance = Vector3.Distance(center, point);
                if (distance > radius)
                    continue;

                var direction = center - point;
                Hurt(enemy, damage * Mathf.Lerp(1f, 0.5f, distance / radius), center,
                     direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.up, hit);
            }
        }

        /// <summary>
        /// Ce qu'on voit d'une explosion de ce rayon : la vraie explosion (feu, fumée, poussière, étincelles : ExplosionFx, le
        /// 26 octobre au soir), ou, sans ses matériaux, l'ancien effet (des éclairs et le prefab).
        /// </summary>
        public void ExplosionVisual(Vector3 point, float radius, Color color)
        {
            if (m_ExplosionFireMaterial != null && m_ExplosionSmokeMaterial != null)
            {
                ExplosionFx.Play(point, radius, m_ExplosionFireMaterial, m_ExplosionSmokeMaterial, transform);
                return;
            }

            LightningBolt.Burst(point, radius * 0.6f, color, m_EffectMaterial);
            PlayExplosionEffect(point);
        }

        // Un effet d'explosion de la réserve : un libre (éteint à la fin de ses particules), sinon un nouveau, sinon le plus ancien.
        // Le prefab se détruit seul à la fin : ses copies s'éteignent à la place, pour resservir.
        void PlayExplosionEffect(Vector3 point)
        {
            if (m_ExplosionEffect == null)
                return;

            m_ExplosionPool.RemoveAll(effect => effect == null);
            GameObject chosen = null;
            foreach (var effect in m_ExplosionPool)
            {
                if (!effect.activeSelf)
                {
                    chosen = effect;
                    break;
                }
            }

            if (chosen == null && m_ExplosionPool.Count < k_MaxExplosionEffects)
            {
                chosen = Instantiate(m_ExplosionEffect);
                chosen.SetActive(false);
                foreach (var particles in chosen.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particles.main;
                    main.stopAction = ParticleSystemStopAction.Disable;
                }

                m_ExplosionPool.Add(chosen);
            }

            if (chosen == null)
                chosen = m_ExplosionPool[m_NextExplosion++ % m_ExplosionPool.Count];

            // Rallumé, il rejoue ses particules (Play On Awake).
            chosen.SetActive(false);
            chosen.transform.SetPositionAndRotation(point, Quaternion.identity);
            chosen.SetActive(true);
        }

        // L'éclair : celui de la flèche de foudre (sans rebond), ou celui de la chaîne d'éclairs, qui rebondit (26 octobre : leurs
        // dégâts par niveau ; avant, au-delà de 100 % de chance, l'éclair devenait plus fort).
        void StrikeLightning(in ArrowHit hit, Enemy hitEnemy)
        {
            var upgrades = PlayerUpgrades.Instance;
            var bounces = upgrades != null ? upgrades.ChainCount : 0;
            var share = upgrades != null
                ? ByLevel(m_LightningDamageByLevel, upgrades.Stacks(UpgradeEffect.Lightning)) +
                  ByLevel(m_ChainDamageByLevel, upgrades.Stacks(UpgradeEffect.ChainLightning))
                : 0f;
            var damage = hit.Damage * share;
            var slow = m_LightningSlow;
            var slowDuration = m_LightningSlowDuration;

            m_Struck.Clear();
            var target = hitEnemy != null ? hitEnemy : NearestEnemy(hit.Point, m_LightningSearchRadius, m_Struck);
            var targetPoint = target != null ? Center(target) : hit.Point;

            // Le premier éclair tombe du ciel, puis la chaîne rebondit d'ennemi en ennemi.
            var from = targetPoint + new Vector3(Random.Range(-3f, 3f), 18f, Random.Range(-3f, 3f));
            LightningBolt.Spawn(from, targetPoint, m_LightningColor, m_EffectMaterial, 0.12f, 0.35f);
            Sfx.Play(m_LightningClip, targetPoint);

            for (var strikes = 0; target != null && strikes <= bounces; strikes++)
            {
                m_Struck.Add(target);
                if (target.IsAlive)
                {
                    Hurt(target, damage, targetPoint, Vector3.down, hit);
                    target.Slow(slow, slowDuration);
                }

                if (strikes == bounces)
                    break;

                var next = NearestEnemy(targetPoint, m_ChainRadius, m_Struck);
                if (next == null)
                    break;

                var nextPoint = Center(next);
                LightningBolt.Spawn(targetPoint, nextPoint, m_LightningColor, m_EffectMaterial, 0.08f, 0.3f);
                target = next;
                targetPoint = nextPoint;
            }
        }

        // L'explosion de la flèche explosive, plus forte et plus large à chaque niveau (26 octobre). Le duo « Supernova » (le soir) :
        // la flèche dorée de la corde de transe explose deux fois plus fort et plus large.
        void Explode(in ArrowHit hit)
        {
            var upgrades = PlayerUpgrades.Instance;
            var level = Mathf.Max(1, upgrades != null ? upgrades.Stacks(UpgradeEffect.Explosive) : 1);
            var radius = ByLevel(m_ExplosionRadiusByLevel, level);
            var centerDamage = ByLevel(m_ExplosionDamageByLevel, level);
            var point = hit.Point;
            var pieces = ArrowPieces.Instance;
            if (pieces != null && pieces.IsSupernova(hit.Arrow))
            {
                radius *= k_SupernovaScale;
                centerDamage *= k_SupernovaScale;
                LightningBolt.Burst(point, radius * 0.5f, new Color(1f, 0.85f, 0.35f), m_EffectMaterial, 16);
            }

            ExplosionVisual(point, radius, m_ExplosionColor);
            Sfx.Play(m_ExplosionClip, point);

            // Copie de la liste : les ennemis tués en sortent pendant la boucle.
            m_Enemies.Clear();
            m_Enemies.AddRange(Enemy.Alive);
            foreach (var enemy in m_Enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var center = Center(enemy);
                var distance = Vector3.Distance(center, point);
                if (distance > radius)
                    continue;

                // Les mêmes dégâts sur toute la zone, ceux du centre (27 octobre au soir, demandé par le joueur ; de 100 % au centre à
                // 50 % au bord avant).
                var direction = center - point;
                Hurt(enemy, hit.Damage * centerDamage, center,
                     direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.up, hit);
            }
        }

        // La glace se pose au sol : sous l'ennemi touché, sinon sur le sol où marchent les ennemis (NavMesh). Plus large et plus
        // glissante à chaque niveau (26 octobre).
        void Freeze(in ArrowHit hit, Enemy hitEnemy)
        {
            Vector3 ground;
            if (hitEnemy != null)
                ground = hitEnemy.transform.position;
            else if (NavMesh.SamplePosition(hit.Point, out var navHit, 3f, NavMesh.AllAreas))
                ground = navHit.position;
            else
                return;

            var upgrades = PlayerUpgrades.Instance;
            var level = Mathf.Max(1, upgrades != null ? upgrades.Stacks(UpgradeEffect.Frost) : 1);
            IceZone.Spawn(ground, ByLevel(m_FrostRadiusByLevel, level), m_FrostDuration,
                          1f - Mathf.Clamp(ByLevel(m_FrostSlowByLevel, level), 0f, 0.9f), m_FrostColor, m_EffectMaterial);
            Sfx.Play(m_FrostClip, ground + Vector3.up * 0.5f);
        }

        // Tir ricochet : au lieu de se planter, la flèche repart vers un autre ennemi proche.
        void TryRicochet(in ArrowHit hit, Enemy enemy, Tracked state)
        {
            var upgrades = PlayerUpgrades.Instance;
            var maxBounces = upgrades != null ? upgrades.RicochetCount : 0;
            if (maxBounces <= 0)
                return;

            // Le premier ennemi touché n'est pas un rebond : avec N rebonds, la flèche touche N + 1 ennemis.
            state.RicochetTarget = null;
            if (!state.RicochetHits.Contains(enemy))
                state.RicochetHits.Add(enemy);
            if (state.RicochetHits.Count > maxBounces)
                return;

            var next = NearestEnemy(hit.Point, m_RicochetRange, state.RicochetHits);
            if (next == null)
                return;

            state.RicochetTarget = next;

            // On vise un peu au-dessus de l'ennemi pour compenser la chute de la flèche.
            var speed = Mathf.Max(10f, hit.Speed * m_RicochetSpeed);
            var target = Center(next);
            var time = Vector3.Distance(hit.Point, target) / speed;
            target += Vector3.up * (0.5f * -Physics.gravity.y * hit.Arrow.GravityScale * time * time);
            hit.Arrow.Deflect((target - hit.Point).normalized * speed);
            Sfx.Play(m_RicochetClip, hit.Point, 0.8f);
        }

        // La source n'est pas une flèche : ces dégâts ne font ni points de touche ni combo, mais l'élimination compte.
        void Hurt(Enemy enemy, float amount, Vector3 point, Vector3 direction, in ArrowHit hit)
        {
            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = amount,
                Zone = HitZone.Body,
                Grade = hit.Grade,
                Distance = hit.TravelDistance,
                Point = point,
                Direction = direction,
                Source = this,
            });
        }

        /// <summary>L'ennemi vivant le plus proche de <paramref name="point"/>, à <paramref name="radius"/> m au plus, hors de <paramref name="exclude"/>.</summary>
        public static Enemy NearestEnemy(Vector3 point, float radius, List<Enemy> exclude)
        {
            Enemy best = null;
            var bestDistance = radius;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || exclude.Contains(enemy))
                    continue;

                var distance = Vector3.Distance(Center(enemy), point);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    best = enemy;
                }
            }

            return best;
        }

        static Vector3 Center(Enemy enemy) => enemy.Center;
    }
}
