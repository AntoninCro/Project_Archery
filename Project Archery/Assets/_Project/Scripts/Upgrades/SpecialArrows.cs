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
    /// <item>au tir : multitir (flèches en plus, en éventail) et tir écho (la volée se répète) ;</item>
    /// <item>pour chaque flèche, y compris les flèches en plus : foudre, explosion, glace et perçage tirés au sort,
    /// et déluge (la flèche se divise en vol) ;</item>
    /// <item>en vol : auto-visée ;</item>
    /// <item>à l'impact : foudre et chaîne d'éclairs, explosion, zone de glace, ricochet.</item>
    /// </list>
    /// Tout se cumule sans limite : au-delà de 100 % de chance, un effet devient plus fort.
    /// Les flèches nées d'une division en vol ne se divisent pas à leur tour.
    /// Les failles du katana s'en servent aussi (24 octobre) : <see cref="RollEffects"/>, <see cref="Impact"/>,
    /// <see cref="Blast"/> et les réglages de l'écho, du déluge, du ricochet et de l'auto-visée ; le katana fait le reste
    /// (<see cref="Archery.Combat.Katana"/>, <see cref="Archery.Combat.MatrixRift"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class SpecialArrows : MonoBehaviour
    {
        [Header("Flèches en plus (multitir, écho, déluge)")]
        [Tooltip("Angle (°) entre deux flèches d'une même volée.")]
        [SerializeField]
        float m_SplitAngle = 4f;

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

        [Tooltip("Sécurité pour la fluidité du jeu : au-delà de ce nombre de flèches en vol, on n'en ajoute plus.")]
        [SerializeField]
        int m_MaxArrowsInFlight = 150;

        [Header("Foudre")]
        [Tooltip("Dégâts de l'éclair, en fraction des dégâts de la flèche (doublés à 200 % de chance, etc.).")]
        [SerializeField]
        float m_LightningDamage = 0.75f;

        [Tooltip("Vitesse des ennemis foudroyés (0,6 = 40 % plus lents). Plus forte au-delà de 100 % de chance.")]
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

        [Tooltip("Avec la chaîne d'éclairs mais sans la flèche de foudre : chance qu'une flèche appelle la foudre.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_ChainOnlyLightningChance = 0.2f;

        [SerializeField]
        Color m_LightningColor = new Color(0.45f, 0.75f, 1f);

        [SerializeField]
        AudioClip m_LightningClip;

        [Header("Explosion")]
        [SerializeField]
        float m_ExplosionRadius = 3.5f;

        [Tooltip("Dégâts au centre de l'explosion, en fraction des dégâts de la flèche (moitié moins au bord).")]
        [SerializeField]
        float m_ExplosionDamage = 1f;

        [SerializeField]
        Color m_ExplosionColor = new Color(1f, 0.5f, 0.12f);

        [Tooltip("Optionnel : effet (système de particules…) créé à l'endroit de l'explosion, détruit au bout de 3 s.")]
        [SerializeField]
        GameObject m_ExplosionEffect;

        [SerializeField]
        AudioClip m_ExplosionClip;

        [Header("Glace")]
        [Tooltip("Rayon (m) de la zone de glace.")]
        [SerializeField]
        float m_FrostRadius = 3f;

        [Tooltip("Durée (s) de la zone de glace.")]
        [SerializeField]
        float m_FrostDuration = 5f;

        [Tooltip("Vitesse des ennemis sur la glace (0,5 = deux fois plus lents). Plus forte au-delà de 100 % de chance.")]
        [Range(0.1f, 1f)]
        [SerializeField]
        float m_FrostSlow = 0.5f;

        [SerializeField]
        Color m_FrostColor = new Color(0.6f, 0.88f, 1f);

        [SerializeField]
        AudioClip m_FrostClip;

        [Header("Auto-visée")]
        [Tooltip("Distance maximale (m) entre la flèche et l'ennemi vers lequel elle dévie.")]
        [SerializeField]
        float m_HomingRange = 12f;

        [Tooltip("Angle maximal (°) entre la direction de la flèche et l'ennemi : la flèche ne fait jamais demi-tour.")]
        [SerializeField]
        float m_HomingAngle = 35f;

        [Header("Ricochet")]
        [Tooltip("Distance maximale (m) jusqu'au prochain ennemi.")]
        [SerializeField]
        float m_RicochetRange = 12f;

        [Tooltip("Part de la vitesse gardée après un rebond.")]
        [Range(0.3f, 1f)]
        [SerializeField]
        float m_RicochetSpeed = 0.85f;

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
        }

        // Corde triple (26 octobre) : écart (°) entre deux flèches de l'éventail.
        const float k_TripleSpread = 6f;

        // Effets d'explosion réutilisés (26 octobre : la pointe à mèche en fait beaucoup ; avant, chacun était créé puis
        // détruit, un suspect de l'audit de performance du Quest). Au-delà, la plus ancienne repart.
        const int k_MaxExplosionEffects = 6;

        // Une volée à répéter (tir écho).
        struct PendingEcho
        {
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
        public float HomingRange => m_HomingRange;

        public float HomingAngle => m_HomingAngle;

        // Prêtés aux pièces de la flèche (ArrowPieces, 26 octobre).
        public Material EffectMaterial => m_EffectMaterial;
        public Color FrostColor => m_FrostColor;
        public Color LightningColor => m_LightningColor;
        public AudioClip FrostClip => m_FrostClip;
        public AudioClip LightningClip => m_LightningClip;
        public AudioClip ExplosionClip => m_ExplosionClip;

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
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
            Bow.ShotFired -= OnShotFired;
            Arrow.AnyHit -= OnArrowHit;
            Arrow.ShotEnded -= OnShotEnded;
            m_Tracked.Clear();
            m_Echoes.Clear();
        }

        void Update()
        {
            UpdateEchoes();
            UpdateSplits();
        }

        void FixedUpdate() => UpdateHoming(Time.fixedDeltaTime);

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
            FanAround(shot.Origin, shot.Direction, axis, shot.Speed, shot.Damage, shot.Grade, basePierce, trail, triple, onFire, gravity);

            // Tir écho : la même volée, un instant plus tard, autant de fois que tiré au sort.
            var echoes = PlayerUpgrades.RollCount(upgrades.EchoAverageFor(shot.Grade));
            for (var i = 1; i <= echoes; i++)
            {
                m_Echoes.Add(new PendingEcho
                {
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
            m_Tracked[arrow] = state;

            var upgrades = PlayerUpgrades.Instance;
            if (upgrades == null)
                return;

            state.Effects = RollEffects(grade);
            if (state.Effects != Effect.None)
                arrow.SetTrailColor(ColorOf(state.Effects));

            arrow.AddPierce(PlayerUpgrades.RollCount(upgrades.PierceAverageFor(grade)));

            if (canSplit)
            {
                state.Splits = PlayerUpgrades.RollCount(upgrades.DelugeAverageFor(grade));
                state.SplitTime = Time.time + Random.Range(m_DelugeSplitTime.x, m_DelugeSplitTime.y);
            }

            var pieces = ArrowPieces.Instance;
            if (pieces != null)
                pieces.Track(arrow, damage, grade);
        }

        static bool Roll(float chance) => chance > 0f && Random.value < chance;

        /// <summary>
        /// Tire au sort les effets à l'impact d'un tir de cette qualité (foudre, explosion, glace), d'après les améliorations :
        /// un objet qui ne joue qu'aux tirs parfaits ne compte que pour eux.
        /// </summary>
        public Effect RollEffects(ShotGrade grade)
        {
            var upgrades = PlayerUpgrades.Instance;
            if (upgrades == null)
                return Effect.None;

            var effects = Effect.None;
            var lightningChance = upgrades.ChanceFor(UpgradeEffect.Lightning, grade);

            // La chaîne d'éclairs sans la flèche de foudre appelle l'éclair elle-même : à chaque tir parfait si elle ne joue
            // qu'au parfait, sinon une flèche sur cinq.
            if (upgrades.Stacks(UpgradeEffect.Lightning) == 0 && upgrades.ChainCount > 0)
                lightningChance = !upgrades.IsPerfectOnly(UpgradeEffect.ChainLightning) ? m_ChainOnlyLightningChance
                                  : grade == ShotGrade.Perfect ? 1f : 0f;
            if (Roll(lightningChance))
                effects |= Effect.Lightning;
            if (Roll(upgrades.ChanceFor(UpgradeEffect.Explosive, grade)))
                effects |= Effect.Explosive;
            if (Roll(upgrades.ChanceFor(UpgradeEffect.Frost, grade)))
                effects |= Effect.Frost;
            return effects;
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
                if (Time.time < echo.Time)
                    continue;

                m_Echoes.RemoveAt(i);
                LaunchExtra(echo.Origin, echo.Direction, echo.Speed, echo.Damage, echo.Grade, echo.Pierce, echo.Trail, true, echo.OnFire, echo.Gravity);
                Fan(echo.Origin, echo.Direction, echo.Speed, echo.Damage, echo.Grade, echo.Pierce, echo.Trail, echo.ArrowCount, echo.OnFire,
                    echo.Gravity);
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
            var bestDistance = m_HomingRange;
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
            LightningBolt.Burst(point, radius * 0.6f, color, m_EffectMaterial);
            Sfx.Play(m_ExplosionClip, point);
            PlayExplosionEffect(point);

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

        // Au-delà de 100 % de chance, l'éclair fait plus de dégâts et ralentit plus fort, plus longtemps.
        void StrikeLightning(in ArrowHit hit, Enemy hitEnemy)
        {
            var upgrades = PlayerUpgrades.Instance;
            var bounces = upgrades != null ? upgrades.ChainCount : 0;
            var overflow = upgrades != null ? upgrades.LightningOverflow : 0f;
            var damage = hit.Damage * m_LightningDamage * (1f + overflow);
            var slow = m_LightningSlow / (1f + 0.5f * overflow);
            var slowDuration = m_LightningSlowDuration * (1f + 0.5f * overflow);

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

        // Au-delà de 100 % de chance, l'explosion fait plus de dégâts et touche plus loin.
        void Explode(in ArrowHit hit)
        {
            var upgrades = PlayerUpgrades.Instance;
            var overflow = upgrades != null ? upgrades.ExplosiveOverflow : 0f;
            var radius = m_ExplosionRadius * (1f + 0.25f * overflow);
            var point = hit.Point;

            LightningBolt.Burst(point, radius * 0.6f, m_ExplosionColor, m_EffectMaterial);
            Sfx.Play(m_ExplosionClip, point);
            PlayExplosionEffect(point);

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

                var falloff = Mathf.Lerp(1f, 0.5f, distance / radius);
                var direction = center - point;
                Hurt(enemy, hit.Damage * m_ExplosionDamage * (1f + overflow) * falloff, center,
                     direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.up, hit);
            }
        }

        // La glace se pose au sol : sous l'ennemi touché, sinon sur le sol où marchent les ennemis (NavMesh).
        // Au-delà de 100 % de chance, elle ralentit plus fort, plus longtemps et sur une plus grande zone.
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
            var overflow = upgrades != null ? upgrades.FrostOverflow : 0f;
            IceZone.Spawn(ground, m_FrostRadius * (1f + 0.25f * overflow), m_FrostDuration * (1f + 0.5f * overflow),
                          m_FrostSlow / (1f + overflow), m_FrostColor, m_EffectMaterial);
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
            state.RicochetHits.Add(enemy);
            if (state.RicochetHits.Count > maxBounces)
                return;

            var next = NearestEnemy(hit.Point, m_RicochetRange, state.RicochetHits);
            if (next == null)
                return;

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
