using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Building;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Locomotion;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Upgrades
{
    /// <summary>
    /// Les pièces de la flèche du premier lot (refonte roguelike, étape 3, 26 octobre ; <c>Docs/Refonte_Roguelike.md</c>) :
    /// <list type="bullet">
    /// <item>empennage boomerang : à 22 m (ou au bout de 1,1 s, ou dès qu'elle touche le décor), la flèche fait demi-tour à
    /// plat et revient vers le joueur en ralentissant, en traversant des ennemis à l'aller et au retour ; elle s'arrête devant
    /// lui un moment ; rattrapée (elle passe près d'une main), elle disparaît et le tir suivant est parfait ;</item>
    /// <item>empennage télékinésie : tant que la gâchette de la main qui tient l'arme reste appuyée (le clic droit sans
    /// casque), les flèches du tir ralentissent et tournent comme cette main, pendant 3 s ;</item>
    /// <item>pointe à mèche : la flèche plantée crépite, puis explose 1 s plus tard (avec le fût aimant : un trou noir) ;</item>
    /// <item>pointe de givre : un tir parfait gèle l'ennemi touché (un boss de vague est ralenti de moitié) ; un ennemi gelé
    /// touché éclate en éclats de glace qui blessent autour ;</item>
    /// <item>fût aimant : la flèche plantée attire les ennemis autour d'elle (pas les boss) ;</item>
    /// <item>fût conducteur : deux flèches plantées dans le décor, à moins de 8 m, sont reliées par un arc électrique qui
    /// blesse ce qui le traverse ;</item>
    /// <item>talisman d'acrobate : en l'air (saut aux bras, chute) et en glissade, le temps ralentit et chaque tir est
    /// parfait.</item>
    /// </list>
    /// La corde triple est dans <see cref="SpecialArrows"/> (une volée, comme le multitir). Chaque niveau renforce la pièce ;
    /// les réglages sont les constantes en tête de ce script. Seulement pour les flèches de l'arc et des arbalètes : les
    /// failles du katana volent seules vers leur cible et ne se plantent pas. Créé tout seul par <see cref="SpecialArrows"/>,
    /// sur le même objet. Les flèches lui sont confiées par <see cref="SpecialArrows"/> (<see cref="BeginShot"/>, puis
    /// <see cref="Track"/> pour chacune). Le deuxième lot et les duos sont dans <c>ArrowPieces.Lot2.cs</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public partial class ArrowPieces : MonoBehaviour
    {
        // --- Réglages (à ajuster au test) ---

        // Empennage boomerang : la distance (m) ou le temps (s) du demi-tour, le virage (°/s), la vitesse du retour (m/s), celle
        // de l'arrivée et la distance (m) où elle commence à ralentir, le freinage (m/s²) depuis sa vitesse de départ, le temps
        // au-delà duquel elle ne revient plus (elle retombe), la distance (m) où elle s'arrête devant toi et le temps (s) qu'elle
        // y attend, la distance d'une main qui la rattrape (m), les rebonds sur le décor au plus, les ennemis traversés à chaque
        // passage par niveau, et les tirs parfaits gardés en réserve au plus. Revu le 26 octobre après le test du joueur :
        // elle piquait vers le sol avant de revenir, revenait trop vite, et se rattrapait à peine.
        const float k_BoomerangRange = 22f;
        const float k_BoomerangOutTime = 1.1f;
        const float k_BoomerangTurnRate = 540f;
        const float k_BoomerangReturnSpeed = 14f;
        const float k_BoomerangArriveSpeed = 6f;
        const float k_BoomerangSlowRadius = 4f;
        const float k_BoomerangBrake = 60f;
        const float k_BoomerangReturnTime = 4f;
        const float k_BoomerangStopDistance = 0.6f;
        const float k_BoomerangWait = 1.5f;

        // Le duo « Faux » : le temps (s) où la flèche attend devant le joueur avant son deuxième aller-retour (27 octobre, réglage
        // du joueur ; avant, elle repartait tout de suite).
        const float k_ScythePause = 0.5f;
        const float k_CatchRadius = 0.7f;
        const int k_BoomerangSceneryReturns = 3;
        const int k_BoomerangPiercePerLevel = 1;
        const int k_MaxQueuedPerfects = 3;

        // Empennage télékinésie : le virage (°/s), la durée (s) après le tir, la part de sa vitesse que garde la flèche guidée aux
        // niveaux 1, 2 et 3, la vitesse minimale (m/s), et combien la flèche tourne quand la main tourne (2 : la main tourne de
        // 20°, la flèche de 40° ; sans casque, la caméra, ×1). Réglages du joueur du 26 octobre : le virage ne monte plus avec le
        // niveau, la flèche ralentit à la place (deux fois plus lente au niveau 1).
        const float k_SteerRate = 160f;
        const float k_SteerTime = 3f;
        static readonly Vector3 k_SteerSpeedShare = new Vector3(0.5f, 0.4f, 0.3f);
        const float k_SteerMinSpeed = 6f;
        const float k_SteerGain = 2f;

        // Pointe à mèche : le délai (s), les dégâts au centre en part du tir (moitié au bord) et le rayon (m), avec ce qui
        // s'ajoute par niveau ; les mèches en même temps au plus (100 depuis le 27 octobre, réglage du joueur ; 12 avant) ;
        // l'intervalle (s) des étincelles.
        const float k_FuseDelay = 1f;
        const float k_FuseDamage = 1f;
        const float k_FuseDamagePerLevel = 0.25f;
        const float k_FuseRadius = 3.5f;
        const float k_FuseRadiusPerLevel = 0.75f;
        const int k_MaxFuses = 100;
        const float k_FuseSparkInterval = 0.2f;

        // Pointe de givre : la durée du gel (s) et par niveau, le ralentissement d'un boss de vague à la place, les dégâts des
        // éclats en part de la flèche qui le brise (et par niveau), leur rayon (m).
        const float k_FreezeTime = 1.5f;
        const float k_FreezeTimePerLevel = 0.5f;
        const float k_BossFreezeSlow = 0.5f;
        const float k_ShatterDamage = 0.8f;
        const float k_ShatterDamagePerLevel = 0.3f;
        const float k_ShatterRadius = 3f;

        // Fût aimant : le rayon (m), la durée (s), et par niveau ; la vitesse (m/s) à laquelle il tire les ennemis, au bord ;
        // les aimants en même temps au plus (9 depuis le 26 octobre, réglage du joueur ; 3 avant).
        const float k_MagnetRadius = 6f;
        const float k_MagnetRadiusPerLevel = 1.5f;
        const float k_MagnetTime = 2.5f;
        const float k_MagnetTimePerLevel = 0.5f;
        const float k_PullSpeed = 4f;
        const int k_MaxMagnets = 9;

        // Le duo « Trou noir » (revu le 26 octobre) : le temps (s) où le trou noir aspire avant d'exploser, et combien il tire
        // plus fort qu'un aimant.
        const float k_BlackHoleTime = 1.5f;
        const float k_BlackHolePull = 1.75f;

        // Son explosion, en fois celle de la mèche (27 octobre, réglage du joueur ; ×1 avant), deux fois plus large.
        const float k_BlackHoleDamage = 1.5f;

        // Fût conducteur : la distance (m) entre deux flèches reliées, la durée (s) et par niveau, les dégâts par seconde en part
        // du tir (et par niveau) à chaque ennemi traversé, le pas (s) des dégâts, l'épaisseur (m) de l'arc, sa hauteur au-dessus
        // de l'encoche, les liens et les flèches gardés au plus (100 depuis le 27 octobre, réglage du joueur ; 12 liens et 16
        // flèches le 26 octobre ; 4 liens avant), le scintillement (s).
        const float k_LinkRange = 8f;
        const float k_LinkTime = 6f;
        const float k_LinkTimePerLevel = 1.5f;
        const float k_LinkDamage = 0.8f;
        const float k_LinkDamagePerLevel = 0.4f;
        const float k_LinkTick = 0.25f;
        const float k_LinkRadius = 0.45f;
        const float k_LinkHeight = 0.25f;
        const int k_MaxLinks = 100;
        const int k_MaxNodes = 100;
        const float k_LinkFlicker = 0.08f;

        // Talisman d'acrobate : la vitesse du temps au niveau 1 (0,1 de moins par niveau, 0,3 au plus bas), le temps qu'il dure
        // encore après l'atterrissage ou la fin de la glissade (s, temps réel), et la part de sa vitesse de départ où la
        // glissade s'arrête. Réglages du joueur du 26 octobre : 0,5 / 0,4 / 0,3 (0,7 / 0,6 / 0,5 avant), et une glissade qui
        // s'arrête à mi-vitesse au lieu d'aller jusqu'au bout.
        const float k_AcrobatScale = 0.5f;
        const float k_AcrobatScalePerLevel = 0.1f;
        const float k_AcrobatGrace = 0.25f;
        const float k_AcrobatSlideEnd = 0.5f;

        static readonly Color k_FuseColor = new Color(1f, 0.55f, 0.15f);
        static readonly Color k_MagnetColor = new Color(0.78f, 0.45f, 1f);
        static readonly Color k_LinkColor = new Color(0.45f, 0.85f, 1f);
        static readonly Color k_CatchColor = new Color(0.55f, 1f, 0.6f);
        static readonly Color k_FrozenTint = new Color(0.55f, 0.82f, 1f);
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly Collider[] s_Overlaps = new Collider[32];

        // Une flèche du joueur suivie depuis son départ : celle de l'arme et les flèches en plus.
        class Flight
        {
            public Arrow Arrow;
            public float Damage;
            public ShotGrade Grade;
            public Transform Aim;
            public IXRSelectInteractor Hand;
            public Vector3 Origin;
            public float LaunchTime;
            public bool Returning;
            public bool ReturnDone;
            public float ReturnStart;
            public float ReturnSpeed;
            public Vector3 PreviousTip;

            // Le boomerang (26 octobre) : le côté du demi-tour (+1 à droite), les rebonds sur le décor, l'attente devant le joueur.
            public float ReturnSide;
            public int SceneryReturns;
            public bool Waiting;
            public float WaitUntil;

            // La faux attend devant le joueur avant son deuxième aller-retour ; une flèche de la pluie (27 octobre).
            public bool ScythePausing;
            public bool IsRain;

            // La télékinésie : la gâchette est tenue ; l'orientation de la main et la direction de la flèche à l'appui ; la vitesse
            // de la flèche guidée (ralentie une fois, à la première prise).
            public bool Steering;
            public Quaternion SteerReference;
            public Vector3 SteerDirection;
            public float SteerSpeed;

            // L'ingénieur (26 octobre) : la flèche a déjà réparé une barricade ; surchargé un bâtiment (27 octobre).
            public bool Repaired;
            public bool Charged;

            // Le deuxième lot (ArrowPieces.Lot2) : une flèche née d'une pièce (éclat, détonation), les rebonds faits, le
            // facteur du sniper déjà appliqué, les allers-retours de la faux et la direction du premier, le fût suspendu.
            public bool IsChild;
            public int Bounces;
            public float SniperFactor;
            public int Trips;
            public Vector3 FirstDirection;
            public bool Hovering;
            public bool Released;
            public float SuspendAt;
            public float HoverUntil;
            public Vector3 SavedVelocity;
            public float OrbitAngle;

            // Le troisième lot (ArrowPieces.Lot3, 26 octobre au soir) : la flèche dorée de la transe (supernova), le piqué (la
            // cible, il a commencé), le rappel (la flèche rappelée revient), une graine de la pluie de fer (elle devient un pieu),
            // le rail (le dernier point de sa traînée).
            public bool Golden;
            public bool Dived;
            public Enemy DiveTarget;
            public bool Recalled;
            public readonly List<Enemy> RecallHits = new List<Enemy>();
            public bool SpikeSeed;
            public bool Rail;
            public Vector3 RailPoint;
            public float RailNext;

            // Le duo « Nuage » (27 octobre) : quand la flèche s'arrête dans le ciel (0 : jamais).
            public float CloudAt;

            // La corde de pluie revue (27 octobre) : la flèche sème des flèches ; la distance (m) parcourue, celle de la prochaine
            // flèche, les flèches déjà semées.
            public bool RainTrail;
            public float RainTraveled;
            public float RainNextAt;
            public int RainDrops;
        }

        class Fuse
        {
            public Arrow Arrow;
            public Vector3 Point;
            public float ExplodeAt;
            public float NextSpark;
            public float Damage;
            public ShotGrade Grade;

            // Le duo « Trou noir » : la mèche est un trou noir, et ce que l'on voit, jusqu'à l'explosion (null s'il y en a trop).
            public bool IsBlackHole;
            public BlackHole Hole;
        }

        class Magnet
        {
            public Arrow Arrow;
            public Vector3 Point;
            public float EndAt;
            public float Radius;
            public float Pull = 1f;
            public LineRenderer Ring;

            // Les dégâts et la qualité du tir qui l'a planté : le duo « Orage magnétique » (26 octobre au soir).
            public float Damage;
            public ShotGrade Grade;
        }

        class Node
        {
            public Vector3 Point;
            public float EndAt;
            public float Damage;
            public ShotGrade Grade;
        }

        class Link
        {
            public Node A;
            public Node B;
            public float EndAt;
            public float NextFlicker;

            // Le duo « Réseau » (26 octobre au soir) : l'arc relie la flèche à ce bâtiment, qu'il surcharge.
            public Structure Building;
        }

        class Frozen
        {
            public float Until;
            public Renderer[] Renderers;
        }

        readonly Dictionary<Arrow, Flight> m_Flights = new Dictionary<Arrow, Flight>();
        readonly Stack<Flight> m_FreeFlights = new Stack<Flight>();
        readonly List<Arrow> m_Caught = new List<Arrow>();
        readonly List<IXRSelectInteractor> m_CatchHands = new List<IXRSelectInteractor>();
        readonly List<Fuse> m_Fuses = new List<Fuse>();
        readonly List<Magnet> m_Magnets = new List<Magnet>();
        readonly Stack<LineRenderer> m_FreeRings = new Stack<LineRenderer>();
        readonly List<Node> m_Nodes = new List<Node>();
        readonly List<Link> m_Links = new List<Link>();
        readonly Dictionary<Enemy, Frozen> m_Frozen = new Dictionary<Enemy, Frozen>();
        readonly Dictionary<Enemy, bool> m_IsBoss = new Dictionary<Enemy, bool>();
        readonly List<Enemy> m_Thawed = new List<Enemy>();
        readonly List<Enemy> m_Enemies = new List<Enemy>();
        readonly HashSet<Enemy> m_LinkHits = new HashSet<Enemy>();
        MaterialPropertyBlock m_FrozenBlock;
        Transform m_ShotAim;
        IXRSelectInteractor m_ShotHand;
        Bow m_Bow;
        int m_QueuedPerfects;
        float m_NextLinkTick;
        float m_NextSteerPulse;
        float m_LastSteerTime = -1f;
        AudioSource m_SteerHum;
        ArmSwingLocomotion m_Locomotion;
        float m_NextLocomotionSearch;
        bool m_AcrobatActive;
        float m_AcrobatUntil;

        public static ArrowPieces Instance { get; private set; }

        /// <summary>Tirs parfaits en réserve (le duelliste ; sans casque, les flèches boomerang rattrapées), pas encore tirés.</summary>
        public int QueuedPerfects => m_QueuedPerfects;

        // Les flèches boomerang rattrapées en main (27 octobre) : elles sont parfaites, elles seules.
        readonly HashSet<Arrow> m_PerfectArrows = new HashSet<Arrow>();
        readonly List<Arrow> m_ForgetPerfect = new List<Arrow>();

        void OnEnable()
        {
            Instance = this;
            Arrow.AnyHit += OnArrowHit;
            Arrow.Stuck += OnArrowStuck;
            Arrow.ShotEnded += OnShotEnded;
            Bow.ClaimPerfectShot = ClaimPerfect;
            Bow.PerfectPending = PeekPerfect;
            EnableLot2();
            EnableLot3();
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
            Arrow.AnyHit -= OnArrowHit;
            Arrow.Stuck -= OnArrowStuck;
            Arrow.ShotEnded -= OnShotEnded;
            if (Bow.ClaimPerfectShot == (Func<Arrow, bool>)ClaimPerfect)
                Bow.ClaimPerfectShot = null;
            if (Bow.PerfectPending == (Func<Arrow, bool>)PeekPerfect)
                Bow.PerfectPending = null;
            m_PerfectArrows.Clear();

            SlowMotion.End(this);
            m_AcrobatActive = false;
            if (m_SteerHum != null)
                m_SteerHum.Stop();
            m_Flights.Clear();
            foreach (var fuse in m_Fuses)
            {
                if (fuse.Hole != null)
                    fuse.Hole.Collapse();
            }

            m_Fuses.Clear();
            m_Nodes.Clear();
            m_Links.Clear();
            while (m_Magnets.Count > 0)
                RemoveMagnet(m_Magnets.Count - 1);
            foreach (var frozen in m_Frozen.Values)
                Tint(frozen, false);
            m_Frozen.Clear();
            DisableLot2();
            DisableLot3();
        }

        static int Level(UpgradeEffect effect)
        {
            var upgrades = PlayerUpgrades.Instance;
            return upgrades != null ? upgrades.Stacks(effect) : 0;
        }

        // Les pièces qui suivent les flèches (sinon, rien à suivre) : la plupart, sauf la plume, le faucon et le pacte de verre.
        static readonly UpgradeEffect[] k_TrackedEffects =
        {
            UpgradeEffect.Boomerang, UpgradeEffect.Telekinesis, UpgradeEffect.Fuse, UpgradeEffect.FrostTip, UpgradeEffect.Magnet,
            UpgradeEffect.Conductor, UpgradeEffect.Splinter, UpgradeEffect.HunterMark, UpgradeEffect.Executioner, UpgradeEffect.Bounce,
            UpgradeEffect.Lead, UpgradeEffect.Spectral, UpgradeEffect.Spike, UpgradeEffect.Detonator, UpgradeEffect.Suspended,
            UpgradeEffect.Trance, UpgradeEffect.Sniper, UpgradeEffect.Engineer, UpgradeEffect.Duelist,
            UpgradeEffect.Siege, UpgradeEffect.Dive, UpgradeEffect.Recall, UpgradeEffect.Sentinel, UpgradeEffect.Feather,
            UpgradeEffect.Rain,
        };

        static bool HasFlightPiece()
        {
            foreach (var effect in k_TrackedEffects)
            {
                if (Level(effect) > 0)
                    return true;
            }

            return false;
        }

        /// <summary>Un tir part : l'arme et la main qui le tirent servent à toutes ses flèches, échos compris.</summary>
        public void BeginShot(in ShotInfo shot)
        {
            m_ShotAim = shot.Aim;
            m_ShotHand = shot.Hand;
            OnShotStart(shot);
            OnShotStartLot3(shot);
        }

        /// <summary>Une flèche du joueur vient de partir (celle de l'arme, ou une flèche en plus) : elle est suivie jusqu'à la fin de son vol.</summary>
        public void Track(Arrow arrow, float damage, ShotGrade grade)
        {
            if (arrow == null || !HasFlightPiece())
                return;

            if (!m_Flights.TryGetValue(arrow, out var flight))
            {
                flight = m_FreeFlights.Count > 0 ? m_FreeFlights.Pop() : new Flight();
                m_Flights[arrow] = flight;
            }

            flight.Arrow = arrow;
            flight.Damage = damage;
            flight.Grade = grade;
            flight.Aim = m_ShotAim;
            flight.Hand = m_ShotHand;
            flight.Origin = arrow.transform.position;
            flight.LaunchTime = Time.time;
            flight.Returning = false;
            flight.ReturnDone = false;
            flight.SceneryReturns = 0;
            flight.Waiting = false;
            flight.ScythePausing = false;
            flight.IsRain = false;
            flight.Steering = false;
            flight.SteerSpeed = 0f;
            flight.Repaired = false;
            flight.Charged = false;
            flight.PreviousTip = arrow.TipPosition;
            flight.IsChild = m_TrackingChild;
            flight.Bounces = 0;
            flight.SniperFactor = 1f;
            flight.Trips = 0;
            flight.FirstDirection = arrow.Velocity.sqrMagnitude > 1e-4f ? arrow.Velocity.normalized : arrow.transform.forward;
            flight.Hovering = false;
            flight.Released = false;
            flight.Golden = false;
            flight.Dived = false;
            flight.DiveTarget = null;
            flight.Recalled = false;
            flight.RecallHits.Clear();
            flight.SpikeSeed = false;
            flight.Rail = false;
            flight.CloudAt = 0f;
            flight.RainTrail = false;

            var boomerang = Level(UpgradeEffect.Boomerang);
            if (boomerang > 0)
                arrow.AddPierce(k_BoomerangPiercePerLevel * boomerang);
            TrackLot2(arrow, flight);
            TrackLot3(arrow, flight);
        }

        void OnShotEnded(Arrow arrow)
        {
            if (!m_Flights.TryGetValue(arrow, out var flight))
                return;

            m_Flights.Remove(arrow);
            flight.Arrow = null;
            flight.Aim = null;
            flight.Hand = null;
            m_FreeFlights.Push(flight);
        }

        // La flèche boomerang rattrapée elle-même (27 octobre), le talisman d'acrobate en l'air, un parfait en réserve du duelliste :
        // ce tir compte comme parfait (Bow.ClaimPerfectShot). La flèche rattrapée passe avant la réserve.
        bool ClaimPerfect(Arrow arrow)
        {
            if (arrow != null && m_PerfectArrows.Remove(arrow))
                return true;
            if (m_AcrobatActive)
                return true;
            if (m_QueuedPerfects <= 0)
                return false;

            m_QueuedPerfects--;
            return true;
        }

        // Ce que ClaimPerfect accorderait, sans rien consommer (Bow.PerfectPending, 27 octobre) : l'anneau s'affiche alors
        // entièrement vert. À garder identique à ClaimPerfect.
        bool PeekPerfect(Arrow arrow) => (arrow != null && m_PerfectArrows.Contains(arrow)) || m_AcrobatActive || m_QueuedPerfects > 0;

        // Les flèches rattrapées ne restent parfaites que tant qu'elles sont en main ou encochées : rendue au pool (lâchée, rangée),
        // une flèche ne l'est plus (le pool la redonnerait au carquois).
        void ForgetPerfectArrows()
        {
            if (m_PerfectArrows.Count == 0)
                return;

            m_ForgetPerfect.Clear();
            foreach (var arrow in m_PerfectArrows)
            {
                if (arrow == null || (arrow.CurrentState != Arrow.State.Held && arrow.CurrentState != Arrow.State.Nocked))
                    m_ForgetPerfect.Add(arrow);
            }

            foreach (var arrow in m_ForgetPerfect)
                m_PerfectArrows.Remove(arrow);
        }

        void Update()
        {
            UpdateBoomerangCatch();
            ForgetPerfectArrows();
            UpdateAcrobat();
            UpdateSteerHum();
            UpdateFuses();
            UpdateMagnets();
            UpdateLinks();
            UpdateFrozen();
            UpdateLot2();
            UpdateLot3();
        }

        // ---------------------------------------------------------------- en vol : boomerang et télékinésie

        void FixedUpdate()
        {
            if (m_Flights.Count == 0)
                return;

            var boomerang = Level(UpgradeEffect.Boomerang);
            var steer = Level(UpgradeEffect.Telekinesis);
            var sniper = Level(UpgradeEffect.Sniper);
            var suspended = Level(UpgradeEffect.Suspended);
            var engineer = Level(UpgradeEffect.Engineer);
            var detonators = m_Detonators.Count > 0;
            var lot3 = NeedsFixedLot3();
            if (boomerang == 0 && steer == 0 && sniper == 0 && suspended == 0 && engineer == 0 && !detonators && !lot3)
                return;

            // Le duo « Billard » : les flèches nées d'une pièce (détonations, éclats) font aussi éclater les détonateurs.
            var billiard = detonators && ArrowDuos.IsActive(ArrowDuos.Billiard);

            var deltaTime = Time.fixedDeltaTime;
            IXRSelectInteractor steeringHand = null;
            var steering = false;
            m_Caught.Clear();
            m_CatchHands.Clear();
            m_Detonating.Clear();
            foreach (var flight in m_Flights.Values)
            {
                var arrow = flight.Arrow;
                if (arrow == null || arrow.CurrentState != Arrow.State.Flying)
                    continue;

                // Arrêtée en l'air (fût suspendu) : rien d'autre ne la touche tant qu'elle attend.
                if (suspended > 0 && UpdateSuspended(flight, suspended, deltaTime))
                {
                    flight.PreviousTip = arrow.TipPosition;
                    continue;
                }

                if (sniper > 0)
                    UpdateSniper(flight, sniper);

                if (boomerang > 0)
                {
                    UpdateBoomerang(flight, boomerang, deltaTime);
                    if (flight.Returning && !flight.ReturnDone && TryCatch(flight, out var hand))
                    {
                        m_Caught.Add(arrow);
                        m_CatchHands.Add(hand);
                    }
                }
                else if (steer > 0 && SteerTarget(flight, out var aim))
                {
                    Steer(flight, aim, steer, deltaTime);
                    steering = true;
                    steeringHand = flight.Hand;
                }

                if (detonators && (!flight.IsChild || billiard))
                    FindDetonator(flight);
                if (lot3)
                    FixedLot3(flight, deltaTime);
                if (engineer > 0)
                {
                    TryRepairBarricade(flight, engineer);
                    TryOverchargeBuilding(flight, engineer);
                }
                flight.PreviousTip = arrow.TipPosition;
            }

            // Après la boucle : rattraper une flèche la renvoie au pool, ce qui la retire des flèches suivies ; une détonation
            // lance des flèches, qui s'ajoutent aux flèches suivies.
            for (var i = 0; i < m_Caught.Count; i++)
                Catch(m_Caught[i], m_CatchHands[i]);
            foreach (var detonator in m_Detonating)
                Detonate(detonator);
            FlushBoomerangTurns();
            FlushRainDrops();

            if (steering)
                m_LastSteerTime = Time.unscaledTime;
            if (steering && steeringHand != null && Time.unscaledTime >= m_NextSteerPulse)
            {
                m_NextSteerPulse = Time.unscaledTime + 0.1f;
                Haptics.Pulse(steeringHand, 0.15f, 0.04f);
            }
        }

        // Le bourdonnement de la télékinésie, tant qu'une flèche est guidée (une boucle qui monte et s'éteint en douceur).
        void UpdateSteerHum()
        {
            var steering = Time.unscaledTime - m_LastSteerTime < 0.15f;
            if (!steering && (m_SteerHum == null || !m_SteerHum.isPlaying))
                return;

            if (m_SteerHum == null)
            {
                m_SteerHum = gameObject.AddComponent<AudioSource>();
                m_SteerHum.clip = PieceSounds.SteerHum;
                m_SteerHum.loop = true;
                m_SteerHum.playOnAwake = false;
                m_SteerHum.spatialBlend = 0f;
                m_SteerHum.volume = 0f;
            }

            m_SteerHum.outputAudioMixerGroup = Sfx.Output;
            m_SteerHum.volume = Mathf.MoveTowards(m_SteerHum.volume, steering ? 0.35f : 0f, Time.unscaledDeltaTime * 2f);
            if (steering && !m_SteerHum.isPlaying)
                m_SteerHum.Play();
            else if (!steering && m_SteerHum.volume <= 0f)
                m_SteerHum.Stop();
        }

        // À l'aller, la flèche vole normalement ; passé la distance (ou le temps) du demi-tour, ou dès qu'elle touche le décor
        // (BoomerangOffScenery), elle revient vers le joueur, sans tomber, et peut de nouveau toucher les ennemis traversés à l'aller.
        // Revu le 26 octobre après le test du joueur : elle fait demi-tour à plat, sur le côté (elle virait par le chemin le plus
        // court, souvent par en dessous, et piquait vers le sol) ; elle freine jusqu'à 14 m/s, puis 6 m/s en arrivant ; elle
        // s'arrête devant le joueur et l'attend un moment, puis retombe.
        void UpdateBoomerang(Flight flight, int level, float deltaTime)
        {
            var arrow = flight.Arrow;
            if (!flight.Returning)
            {
                var far = (arrow.transform.position - flight.Origin).sqrMagnitude >= k_BoomerangRange * k_BoomerangRange;
                if (!far && Time.time - flight.LaunchTime < k_BoomerangOutTime)
                    return;

                BeginReturn(flight, level);
            }

            if (flight.ReturnDone)
                return;

            // Le duo « Faux » : arrêtée devant le joueur une demi-seconde, elle repart pour son deuxième aller-retour.
            if (flight.ScythePausing)
            {
                if (Time.time < flight.WaitUntil)
                    return;

                flight.ScythePausing = false;
                arrow.Hover(false);
                StartSecondTrip(flight, level, deltaTime);
                return;
            }

            // Arrêtée devant le joueur, elle attend d'être rattrapée, puis retombe.
            if (flight.Waiting)
            {
                if (Time.time < flight.WaitUntil)
                    return;

                flight.Waiting = false;
                flight.ReturnDone = true;
                arrow.Hover(false);
                return;
            }

            var toTarget = ReturnPoint() - arrow.TipPosition;
            var distance = toTarget.magnitude;
            var arrived = distance < k_BoomerangStopDistance;
            if (arrived || Time.time - flight.ReturnStart > k_BoomerangReturnTime)
            {
                // Une flèche de la pluie (27 octobre, demandé par le joueur) : elle ne s'arrête pas devant le joueur et ne se reprend
                // pas ; arrivée, elle disparaît (après la boucle des vols, comme une flèche rappelée).
                if (flight.IsRain)
                {
                    flight.ReturnDone = true;
                    if (arrived && !m_RecallArrived.Contains(arrow))
                    {
                        m_RecallArrived.Add(arrow);
                        LightningBolt.Burst(arrow.TipPosition, 0.2f, k_CatchColor, EffectMaterial, 3);
                    }

                    return;
                }

                // Le duo « Faux » : revenue (ou manquée), elle repart pour un deuxième aller-retour, dans la direction du premier ;
                // revenue, elle attend d'abord une demi-seconde devant le joueur (27 octobre, demandé par le joueur).
                if (flight.Trips == 0 && ArrowDuos.IsActive(ArrowDuos.Scythe))
                {
                    if (arrived)
                    {
                        flight.ScythePausing = true;
                        flight.WaitUntil = Time.time + k_ScythePause;
                        arrow.Hover(true);
                        return;
                    }

                    StartSecondTrip(flight, level, deltaTime);
                    return;
                }

                if (arrived)
                {
                    flight.Waiting = true;
                    flight.WaitUntil = Time.time + k_BoomerangWait;
                    arrow.Hover(true);
                    return;
                }

                flight.ReturnDone = true;
                return;
            }

            // Elle freine depuis sa vitesse de départ, jusqu'à celle du retour, puis plus encore en approchant.
            var wanted = Mathf.Lerp(k_BoomerangArriveSpeed, k_BoomerangReturnSpeed, Mathf.Clamp01(distance / k_BoomerangSlowRadius));
            flight.ReturnSpeed = Mathf.MoveTowards(flight.ReturnSpeed, wanted, k_BoomerangBrake * deltaTime);

            // Droit sur le joueur, en ligne droite (27 octobre, réglage du joueur : avant, elle virait en arc de cercle sur le côté,
            // k_BoomerangTurnRate). La gravité du pas physique qui suit est annulée d'avance.
            arrow.Velocity = toTarget / distance * flight.ReturnSpeed - Physics.gravity * (arrow.GravityScale * deltaTime);
        }

        // Le deuxième aller-retour de la faux, dans la direction du premier.
        void StartSecondTrip(Flight flight, int level, float deltaTime)
        {
            var arrow = flight.Arrow;
            flight.Trips = 1;
            flight.Returning = false;
            flight.Origin = arrow.transform.position;
            flight.LaunchTime = Time.time;
            arrow.ResetPierced();
            arrow.AddPierce(k_BoomerangPiercePerLevel * level);
            arrow.Velocity = flight.FirstDirection * k_BoomerangReturnSpeed * 2f - Physics.gravity * (arrow.GravityScale * deltaTime);
        }

        // Le demi-tour commence : le côté où elle vire (celui où se trouve le joueur, sinon la droite), les ennemis à retraverser.
        void BeginReturn(Flight flight, int level)
        {
            var arrow = flight.Arrow;
            var velocity = arrow.Velocity;
            flight.Returning = true;
            flight.ReturnStart = Time.time;
            flight.ReturnSpeed = Mathf.Max(k_BoomerangReturnSpeed, velocity.magnitude);
            var cross = Vector3.Cross(velocity, ReturnPoint() - arrow.TipPosition).y;
            flight.ReturnSide = cross < -0.01f ? -1f : 1f;
            arrow.ResetPierced();
            arrow.AddPierce(k_BoomerangPiercePerLevel * level);

            // Le souffle du demi-tour, pour la flèche de l'arme seulement (pas un par flèche de la volée).
            if (!arrow.IsExtra)
                Sfx.Play(PieceSounds.Whoosh, arrow.transform.position, 0.8f, UnityEngine.Random.Range(0.95f, 1.1f));

            OnBoomerangTurn(flight);
        }

        /// <summary>
        /// Boomerang (26 octobre, demandé par le joueur) : la flèche qui touche le sol ou un obstacle repart vers le joueur au lieu
        /// de se planter (trois fois au plus). À appeler pendant <see cref="Arrow.AnyHit"/>. Vrai si elle repart.
        /// </summary>
        bool BoomerangOffScenery(in ArrowHit hit, Flight flight)
        {
            var level = Level(UpgradeEffect.Boomerang);
            if (level == 0 || flight.IsChild || flight.ReturnDone || flight.Waiting || flight.SceneryReturns >= k_BoomerangSceneryReturns)
                return false;

            flight.SceneryReturns++;
            if (!flight.Returning)
                BeginReturn(flight, level);
            else
                flight.ReturnStart = Time.time;

            // Vers le joueur ; si la surface est entre eux (le joueur derrière le mur), elle rebondit d'abord, puis vire vers lui.
            var toPlayer = (ReturnPoint() - hit.Point).normalized;
            var direction = Vector3.Dot(toPlayer, hit.Normal) > 0.1f ? toPlayer : Vector3.Reflect(hit.Direction, hit.Normal).normalized;
            hit.Arrow.Bounce(direction * Mathf.Min(flight.ReturnSpeed, k_BoomerangReturnSpeed * 1.5f));
            return true;
        }

        // Le haut de la poitrine du joueur.
        static Vector3 ReturnPoint()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head != null)
                return head.position - Vector3.up * 0.4f;

            var desktop = DesktopControls.Instance;
            return desktop != null && desktop.CameraTransform != null ? desktop.CameraTransform.position - Vector3.up * 0.3f : Vector3.zero;
        }

        // Sans casque, la flèche se rattrape seule en passant près de la caméra (le tir suivant est parfait). Au casque, c'est
        // UpdateBoomerangCatch, sur une pression de la poignée.
        bool TryCatch(Flight flight, out IXRSelectInteractor hand)
        {
            hand = null;
            var desktop = DesktopControls.Instance;
            if (flight.IsRain || desktop == null || !desktop.IsActive || desktop.CameraTransform == null)
                return false;

            var tip = flight.Arrow.TipPosition;
            return (tip - ReturnPoint()).sqrMagnitude <= 9f && DistanceToSegment(desktop.CameraTransform.position, flight.PreviousTip, tip) < 1f;
        }

        // Au casque (revu le 27 octobre, demandé par le joueur) : on rattrape une flèche boomerang en pressant la poignée de la main
        // qui prend les flèches (pas celle de l'arc, ni en la gardant serrée), la main vide, près de la flèche qui revient ou qui
        // attend devant soi. Une pression prend une seule flèche, la plus proche ; elle arrive dans la main, comme du carquois, et
        // c'est elle qui sera parfaite (pas le tir suivant).
        void UpdateBoomerangCatch()
        {
            if (Level(UpgradeEffect.Boomerang) == 0 || m_Flights.Count == 0)
                return;

            var desktop = DesktopControls.Instance;
            var rig = PlayerRig.Instance;
            if ((desktop != null && desktop.IsActive) || rig == null)
                return;

            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            var bowHand = m_Bow != null ? m_Bow.BowHand : null;
            foreach (var hand in rig.Hands)
            {
                if (hand == null || hand.selectInput == null || hand.hasSelection || ReferenceEquals(hand, bowHand) ||
                    !hand.selectInput.ReadWasPerformedThisFrame())
                    continue;

                // La plus proche de la main, à portée ; la boucle d'abord, la prise ensuite (elle retire la flèche des vols suivis).
                var position = hand.transform.position;
                Flight best = null;
                var bestDistance = k_CatchRadius;
                foreach (var flight in m_Flights.Values)
                {
                    var arrow = flight.Arrow;
                    if (arrow == null || flight.IsChild || flight.IsRain || !flight.Returning || flight.ReturnDone ||
                        arrow.CurrentState != Arrow.State.Flying || ReferenceEquals(hand, flight.Hand))
                        continue;

                    var distance = Mathf.Min(DistanceToSegment(position, arrow.transform.position, arrow.TipPosition),
                                             DistanceToSegment(position, flight.PreviousTip, arrow.TipPosition));
                    if (distance < bestDistance)
                    {
                        best = flight;
                        bestDistance = distance;
                    }
                }

                if (best != null)
                    CatchIntoHand(best.Arrow, hand);
            }
        }

        // La flèche rattrapée arrive dans la main, comme du carquois, et sera parfaite (Bow.ClaimPerfectShot). Une autre arme que
        // l'arc (Akimbow, katana), ou plus de flèche à donner : le tir suivant est parfait, comme avant.
        void CatchIntoHand(Arrow arrow, XRBaseInputInteractor hand)
        {
            if (arrow == null || arrow.CurrentState != Arrow.State.Flying)
                return;

            var point = arrow.TipPosition;
            arrow.Despawn();
            var pool = ArrowPool.Instance;
            var given = pool != null && m_Bow != null && !m_Bow.IsReplaced ? pool.Get() : null;
            if (given != null && !given.TryPutInHand(hand))
            {
                given.Despawn();
                given = null;
            }

            if (given != null)
                m_PerfectArrows.Add(given);
            else
                m_QueuedPerfects = Mathf.Min(m_QueuedPerfects + 1, k_MaxQueuedPerfects);

            Haptics.Pulse(hand, 0.8f, 0.1f);
            if (m_Bow != null)
                Sfx.Play(m_Bow.PerfectClip, point, 0.6f, 1.2f);
            FloatingText.Spawn(point, given != null ? "Rattrapée ! Flèche parfaite" : "Rattrapée ! Tir parfait", k_CatchColor, 0.7f, 1f);
        }

        static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var segment = b - a;
            var lengthSquared = segment.sqrMagnitude;
            var t = lengthSquared > 1e-6f ? Mathf.Clamp01(Vector3.Dot(point - a, segment) / lengthSquared) : 0f;
            return Vector3.Distance(point, a + segment * t);
        }

        void Catch(Arrow arrow, IXRSelectInteractor hand)
        {
            if (arrow == null || arrow.CurrentState != Arrow.State.Flying)
                return;

            var point = arrow.TipPosition;
            arrow.Despawn();
            m_QueuedPerfects = Mathf.Min(m_QueuedPerfects + 1, k_MaxQueuedPerfects);
            if (hand != null)
                Haptics.Pulse(hand, 0.8f, 0.1f);

            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            if (m_Bow != null)
                Sfx.Play(m_Bow.PerfectClip, point, 0.6f, 1.2f);
            FloatingText.Spawn(point, "Rattrapée ! Tir parfait", k_CatchColor, 0.7f, 1f);
        }

        // Télékinésie : tant que la gâchette de la main qui tient l'arme est tenue (le clic droit sans casque), la flèche tourne
        // comme la main a tourné depuis l'appui (deux fois plus), à partir de sa direction à l'appui ; la main immobile, elle file
        // droit. Relâcher puis rappuyer reprend la main où elle est. Revu le 26 octobre après le test du joueur : la flèche
        // suivait l'avant du modèle de l'arc, qui ne vise pas où l'on tire, et montait toujours.
        static bool SteerTarget(Flight flight, out Vector3 aim)
        {
            aim = default;

            // Seule la flèche tirée par l'arme se guide (27 octobre, réglage du joueur : avant, aussi les flèches de la pluie, du
            // multitir, de la corde triple, de l'écho, la flèche fantôme et les flèches nées d'une pièce).
            if (flight.IsChild || flight.IsRain || flight.Arrow == null || flight.Arrow.IsExtra || flight.Returning ||
                Time.time - flight.LaunchTime > k_SteerTime || !SteerInput(flight, out var pose, out var gain))
            {
                flight.Steering = false;
                return false;
            }

            if (!flight.Steering)
            {
                var velocity = flight.Arrow.Velocity;
                flight.Steering = true;
                flight.SteerReference = pose;
                flight.SteerDirection = velocity.sqrMagnitude > 0.01f ? velocity.normalized : flight.Arrow.transform.forward;

                // À la première prise, la flèche ralentit (26 octobre : deux fois moins vite au niveau 1, plus lente ensuite).
                if (flight.SteerSpeed <= 0f)
                {
                    var share = SpecialArrows.ByLevel(k_SteerSpeedShare, Mathf.Max(1, Level(UpgradeEffect.Telekinesis)));
                    flight.SteerSpeed = Mathf.Max(k_SteerMinSpeed, velocity.magnitude * share);
                }
            }

            (pose * Quaternion.Inverse(flight.SteerReference)).ToAngleAxis(out var angle, out var axis);
            if (angle > 180f)
                angle -= 360f;
            aim = Mathf.Abs(angle) < 0.01f || float.IsNaN(axis.x)
                ? flight.SteerDirection
                : Quaternion.AngleAxis(Mathf.Clamp(angle * gain, -175f, 175f), axis) * flight.SteerDirection;
            return true;
        }

        // La gâchette tenue, et l'orientation de ce qui guide : la main qui tient l'arme (plus fort : k_SteerGain), ou la caméra
        // sans casque (le clic droit).
        static bool SteerInput(Flight flight, out Quaternion pose, out float gain)
        {
            pose = Quaternion.identity;
            gain = 1f;
            if (flight.Hand is XRBaseInputInteractor hand)
            {
                if (hand.activateInput == null || !hand.activateInput.ReadIsPerformed())
                    return false;

                pose = hand.transform.rotation;
                gain = k_SteerGain;
                return true;
            }

            var desktop = DesktopControls.Instance;
            var mouse = Mouse.current;
            if (flight.Hand != null || desktop == null || !desktop.IsActive || desktop.CameraTransform == null || mouse == null ||
                !mouse.rightButton.isPressed)
                return false;

            pose = desktop.CameraTransform.rotation;
            return true;
        }

        static void Steer(Flight flight, Vector3 aim, int level, float deltaTime)
        {
            var arrow = flight.Arrow;
            var velocity = arrow.Velocity;
            var speed = velocity.magnitude;
            if (speed < 0.5f || aim.sqrMagnitude < 1e-4f)
                return;

            var direction = Vector3.RotateTowards(velocity / speed, aim.normalized, k_SteerRate * Mathf.Deg2Rad * deltaTime, 0f);
            speed = flight.SteerSpeed > 0f ? flight.SteerSpeed : speed;

            // Guidée, elle ne tombe plus : la gravité du pas physique qui suit est annulée d'avance.
            arrow.Velocity = direction * speed - Physics.gravity * (arrow.GravityScale * deltaTime);
        }

        // ---------------------------------------------------------------- plantée : mèche, aimant, conducteur

        void OnArrowStuck(Arrow arrow, Collider collider)
        {
            if (!m_Flights.TryGetValue(arrow, out var flight))
                return;

            var enemy = collider != null ? collider.GetComponentInParent<Enemy>() : null;
            var magnet = Level(UpgradeEffect.Magnet);

            // Le duo « Trou noir » (revu le 26 octobre, demandé par le joueur) : la flèche plantée devient un trou noir qui aspire,
            // plus fort et plus longtemps qu'une mèche n'attend, puis explose deux fois plus large.
            if (ArrowDuos.IsActive(ArrowDuos.BlackHole))
            {
                MakeRoomForFuse();
                var radius = k_MagnetRadius + k_MagnetRadiusPerLevel * (Mathf.Max(1, magnet) - 1);
                AddMagnetAt(arrow.TipPosition, arrow, k_BlackHoleTime, radius, k_BlackHolePull, flight.Damage, flight.Grade);
                m_Fuses.Add(new Fuse
                {
                    Arrow = arrow,
                    Point = arrow.TipPosition,
                    ExplodeAt = Time.time + k_BlackHoleTime,
                    Damage = flight.Damage,
                    Grade = flight.Grade,
                    IsBlackHole = true,
                    Hole = BlackHole.Spawn(arrow.TipPosition, k_BlackHoleTime, EffectMaterial, transform),
                });
            }
            else
            {
                if (Level(UpgradeEffect.Fuse) > 0)
                {
                    MakeRoomForFuse();
                    m_Fuses.Add(new Fuse
                    {
                        Arrow = arrow,
                        Point = arrow.TipPosition,
                        ExplodeAt = Time.time + k_FuseDelay,
                        Damage = flight.Damage,
                        Grade = flight.Grade,
                    });

                    // La mèche crépite (trois à la fois au plus : au-delà, on ne les distingue plus).
                    if (m_Fuses.Count <= 3)
                        Sfx.Play(PieceSounds.Sizzle, arrow.TipPosition, 0.5f, UnityEngine.Random.Range(0.9f, 1.1f));
                }

                if (magnet > 0)
                    AddMagnet(arrow, magnet, flight);
            }

            // L'arc électrique relie les flèches plantées dans le décor (celles plantées dans un ennemi bougent avec lui).
            var conductor = Level(UpgradeEffect.Conductor);
            if (conductor > 0 && enemy == null)
                AddNode(arrow, flight, conductor);

            OnStuckLot2(arrow, flight, enemy);
            OnStuckLot3(arrow, flight, enemy);
        }

        // La flèche plantée suit ce qu'elle a touché ; reprise par le pool (ou replantée ailleurs), on garde son dernier point.
        static bool StillAt(Arrow arrow, Vector3 point) =>
            arrow != null && arrow.CurrentState == Arrow.State.Stuck && (arrow.TipPosition - point).sqrMagnitude < 9f;

        // Pointe à mèche : des étincelles orange crépitent sur la flèche, puis elle explose (et disparaît).
        void UpdateFuses()
        {
            for (var i = m_Fuses.Count - 1; i >= 0; i--)
            {
                var fuse = m_Fuses[i];
                var attached = fuse.Arrow != null && StillAt(fuse.Arrow, fuse.Point);
                if (attached)
                    fuse.Point = fuse.Arrow.TipPosition;
                if (fuse.Hole != null)
                    fuse.Hole.transform.position = fuse.Point;

                if (Time.time < fuse.ExplodeAt)
                {
                    if (!fuse.IsBlackHole && Time.time >= fuse.NextSpark)
                    {
                        fuse.NextSpark = Time.time + k_FuseSparkInterval;
                        LightningBolt.Burst(fuse.Point, 0.22f, k_FuseColor, EffectMaterial, 2);
                    }

                    continue;
                }

                m_Fuses.RemoveAt(i);
                ExplodeFuse(fuse);
            }
        }

        // La mèche explose (et sa flèche, encore plantée, disparaît).
        void ExplodeFuse(Fuse fuse)
        {
            if (StillAt(fuse.Arrow, fuse.Point))
                fuse.Arrow.Despawn();

            var arrows = SpecialArrows.Instance;
            if (arrows == null)
                return;

            var level = Mathf.Max(1, Level(UpgradeEffect.Fuse));
            var hit = new ArrowHit { Point = fuse.Point, Direction = Vector3.down, Damage = fuse.Damage, Grade = fuse.Grade, IsShot = true };
            var radius = k_FuseRadius + k_FuseRadiusPerLevel * (level - 1);
            var damage = fuse.Damage * (k_FuseDamage + k_FuseDamagePerLevel * (level - 1));

            // Le duo « Trou noir » : le trou noir se referme, et l'explosion est deux fois plus large, et 1,5 fois plus forte (27
            // octobre, réglage du joueur).
            if (fuse.IsBlackHole)
            {
                if (fuse.Hole != null)
                    fuse.Hole.Collapse();
                fuse.Hole = null;
                radius *= 2f;
                damage *= k_BlackHoleDamage;
            }

            arrows.Blast(fuse.Point, damage, radius, k_FuseColor, hit);
        }

        // Plus de k_MaxFuses mèches : la plus ancienne explose tout de suite, avant de laisser la place (27 octobre, réglage du
        // joueur ; avant, une flèche de plus n'avait pas de mèche).
        void MakeRoomForFuse()
        {
            while (m_Fuses.Count >= k_MaxFuses)
            {
                var oldest = m_Fuses[0];
                m_Fuses.RemoveAt(0);
                ExplodeFuse(oldest);
            }
        }

        // Fût aimant : un anneau violet se resserre sans cesse autour de la flèche ; les ennemis au sol et les volants proches
        // glissent vers elle (la poussée des ennemis, Enemy.Push), sauf les boss.
        void AddMagnet(Arrow arrow, int level, Flight flight) =>
            AddMagnetAt(arrow.TipPosition, arrow, k_MagnetTime + k_MagnetTimePerLevel * (level - 1), k_MagnetRadius + k_MagnetRadiusPerLevel * (level - 1),
                        1f, flight.Damage, flight.Grade);

        // Un aimant à ce point (qui suit la flèche plantée, s'il y en a une) : le fût aimant, le trou noir (qui tire plus fort :
        // pull), la faille du katana. damage, grade : le tir qui l'a planté (l'orage magnétique).
        void AddMagnetAt(Vector3 point, Arrow arrow, float duration, float radius, float pull = 1f, float damage = 0f,
                         ShotGrade grade = ShotGrade.None)
        {
            if (m_Magnets.Count >= k_MaxMagnets)
                RemoveMagnet(0);

            var magnet = new Magnet
            {
                Arrow = arrow,
                Point = point,
                EndAt = Time.time + duration,
                Radius = radius,
                Pull = pull,
                Ring = TakeRing(),
                Damage = damage,
                Grade = grade,
            };
            m_Magnets.Add(magnet);

            var arrows = SpecialArrows.Instance;
            if (arrows != null)
                Sfx.Play(arrows.LightningClip, magnet.Point, 0.4f, 0.55f);
        }

        void UpdateMagnets()
        {
            for (var i = m_Magnets.Count - 1; i >= 0; i--)
            {
                var magnet = m_Magnets[i];
                if (Time.time >= magnet.EndAt)
                {
                    MagnetStorm(magnet.Point, magnet.Radius, magnet.Damage, magnet.Grade);
                    RemoveMagnet(i);
                    continue;
                }

                if (StillAt(magnet.Arrow, magnet.Point))
                    magnet.Point = magnet.Arrow.TipPosition;

                foreach (var enemy in Enemy.Alive)
                {
                    if (enemy == null || !enemy.IsAlive || IsBoss(enemy))
                        continue;

                    var offset = magnet.Point - enemy.transform.position;
                    offset.y = 0f;
                    var distance = offset.magnitude;
                    if (distance > magnet.Radius || distance < 0.8f)
                        continue;

                    enemy.Push(offset / distance * (k_PullSpeed * magnet.Pull * Mathf.Lerp(0.6f, 1f, distance / magnet.Radius)));
                }

                if (magnet.Ring != null)
                {
                    var phase = Mathf.Repeat(Time.time * 1.4f, 1f);
                    DrawCircle(magnet.Ring, magnet.Point, magnet.Radius * (1f - phase), k_MagnetColor, 1f - phase * 0.7f);
                }
            }
        }

        void RemoveMagnet(int index)
        {
            var ring = m_Magnets[index].Ring;
            m_Magnets.RemoveAt(index);
            if (ring == null)
                return;

            ring.gameObject.SetActive(false);
            m_FreeRings.Push(ring);
        }

        LineRenderer TakeRing()
        {
            while (m_FreeRings.Count > 0)
            {
                var free = m_FreeRings.Pop();
                if (free != null)
                {
                    free.gameObject.SetActive(true);
                    return free;
                }
            }

            var line = new GameObject("Anneau de l'aimant").AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 32;
            line.widthMultiplier = 0.06f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = EffectMaterial;
            return line;
        }

        static void DrawCircle(LineRenderer line, Vector3 center, float radius, Color color, float alpha)
        {
            color.a = alpha;
            line.startColor = line.endColor = color;
            var count = line.positionCount;
            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2f / count;
                line.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius));
            }
        }

        // Fût conducteur : la nouvelle flèche se relie aux deux plus proches (à moins de 8 m) ; le plus ancien lien s'efface
        // au-delà de quatre.
        void AddNode(Arrow arrow, Flight flight, int level) =>
            _ = AddNodeAt(arrow.transform.position + Vector3.up * k_LinkHeight, flight.Damage, flight.Grade, level);

        // Un nœud à ce point, sans flèche plantée (27 octobre : le duo « Haute tension », un point électrique à chaque rebond ; le
        // duo « Fil électrique », au demi-tour du boomerang).
        Node AddNodeAt(Vector3 point, float damage, ShotGrade grade, int level)
        {
            var now = Time.time;
            var node = new Node
            {
                Point = point,
                EndAt = now + k_LinkTime + k_LinkTimePerLevel * (level - 1),
                Damage = damage,
                Grade = grade,
            };

            Node first = null;
            Node second = null;
            var firstDistance = k_LinkRange;
            var secondDistance = k_LinkRange;
            foreach (var other in m_Nodes)
            {
                if (other.EndAt <= now)
                    continue;

                var distance = Vector3.Distance(other.Point, node.Point);
                if (distance < firstDistance)
                {
                    second = first;
                    secondDistance = firstDistance;
                    first = other;
                    firstDistance = distance;
                }
                else if (distance < secondDistance)
                {
                    second = other;
                    secondDistance = distance;
                }
            }

            if (first != null)
                AddLink(node, first);
            if (second != null)
                AddLink(node, second);
            LinkToBuilding(node);

            m_Nodes.Add(node);
            if (m_Nodes.Count > k_MaxNodes)
                m_Nodes.RemoveAt(0);
            return node;
        }

        Link AddLink(Node a, Node b)
        {
            if (m_Links.Count >= k_MaxLinks)
                m_Links.RemoveAt(0);

            var link = new Link { A = a, B = b, EndAt = Mathf.Min(a.EndAt, b.EndAt) };
            m_Links.Add(link);
            var arrows = SpecialArrows.Instance;
            if (arrows != null)
                Sfx.Play(arrows.LightningClip, (a.Point + b.Point) * 0.5f, 0.5f, 1.4f);
            return link;
        }

        // Les arcs scintillent (des éclairs courts, réutilisés) et blessent, à chaque pas, chaque ennemi qui en traverse un.
        void UpdateLinks()
        {
            if (m_Links.Count == 0)
            {
                if (m_Nodes.Count > 0 && m_Nodes[m_Nodes.Count - 1].EndAt <= Time.time)
                    m_Nodes.Clear();
                return;
            }

            var now = Time.time;
            m_Links.RemoveAll(link => link.EndAt <= now);
            foreach (var link in m_Links)
            {
                if (now < link.NextFlicker)
                    continue;

                link.NextFlicker = now + k_LinkFlicker;
                LightningBolt.Spawn(link.A.Point, link.B.Point, k_LinkColor, EffectMaterial, 0.05f, k_LinkFlicker * 1.6f);
            }

            if (now < m_NextLinkTick)
                return;

            m_NextLinkTick = now + k_LinkTick;
            var level = Mathf.Max(1, Level(UpgradeEffect.Conductor));
            var share = (k_LinkDamage + k_LinkDamagePerLevel * (level - 1)) * k_LinkTick;
            var tempest = ArrowDuos.IsActive(ArrowDuos.Tempest);
            m_LinkHits.Clear();
            foreach (var link in m_Links)
            {
                var count = Physics.OverlapCapsuleNonAlloc(link.A.Point, link.B.Point, k_LinkRadius, s_Overlaps, Physics.DefaultRaycastLayers,
                                                           QueryTriggerInteraction.Ignore);
                for (var i = 0; i < count; i++)
                {
                    var enemy = s_Overlaps[i] != null ? s_Overlaps[i].GetComponentInParent<Enemy>() : null;
                    if (enemy == null || !enemy.IsAlive || !m_LinkHits.Add(enemy))
                        continue;

                    var center = enemy.Center;
                    var damage = (link.A.Damage + link.B.Damage) * 0.5f;
                    var hit = new ArrowHit { Point = center, Grade = link.A.Grade, IsShot = true };
                    Hurt(enemy, damage * share, center, Vector3.up, hit);

                    // Le duo « Tempête » : de temps en temps, la foudre tombe sur ce qui traverse l'arc.
                    if (tempest && UnityEngine.Random.value < k_TempestChance)
                        CallLightning(enemy, damage * k_TempestDamage, hit);
                    FrostFence(enemy);
                }
            }

            UpdateNetwork();
        }

        // ---------------------------------------------------------------- à l'impact : givre (et le deuxième lot)

        void OnArrowHit(ArrowHit hit)
        {
            if (hit.Arrow == null || !hit.IsShot || hit.Collider == null)
                return;

            // Un coup de flèche au corps à corps (la flèche est encore en main) : seulement avec le talisman du duelliste, qui
            // le fait suivre comme un tir.
            if (hit.Arrow.CurrentState == Arrow.State.Held && !OnMeleeHit(hit))
                return;
            if (!m_Flights.TryGetValue(hit.Arrow, out var flight))
                return;

            DetonatorsNear(hit, flight);
            var enemy = hit.Collider.GetComponentInParent<Enemy>();
            if (enemy == null)
            {
                OnSceneryHit(hit, flight);
                return;
            }

            OnEnemyHit(hit, flight, enemy);
            FrostHit(enemy, hit);
        }

        // Pointe de givre : un tir parfait gèle, un ennemi gelé éclate (les flèches, et les failles du katana).
        void FrostHit(Enemy enemy, in ArrowHit hit)
        {
            var level = Level(UpgradeEffect.FrostTip);
            if (level == 0)
                return;

            // Depuis le 27 octobre (demandé par le joueur : plus d'effet réservé aux tirs parfaits), chaque flèche gèle un ennemi qui
            // ne l'est pas ; avant, seul un tir parfait gelait. La suivante le fait éclater.
            if (enemy.IsFrozen || (m_Frozen.TryGetValue(enemy, out var frozen) && frozen.Until > Time.time))
                Shatter(enemy, hit, level);
            else if (enemy.IsAlive)
                Freeze(enemy, level, true);
        }

        // Le duo « Blizzard » : un ennemi gelé gèle aussi ses voisins (une fois, sans chaîne).
        void Freeze(Enemy enemy, int level, bool spread)
        {
            Freeze(enemy, level);
            if (!spread || !ArrowDuos.IsActive(ArrowDuos.Blizzard))
                return;

            m_Enemies.Clear();
            m_Enemies.AddRange(Enemy.Alive);
            foreach (var other in m_Enemies)
            {
                if (other != null && other != enemy && other.IsAlive && !IsBoss(other) &&
                    (other.Center - enemy.Center).sqrMagnitude < k_BlizzardRadius * k_BlizzardRadius)
                    Freeze(other, level);
            }
        }

        void Freeze(Enemy enemy, int level) => FreezeFor(enemy, k_FreezeTime + k_FreezeTimePerLevel * (Mathf.Max(1, level) - 1));

        // Gèle l'ennemi pendant ce temps (s) ; un boss est seulement ralenti (aussi la clôture de givre, 26 octobre au soir).
        void FreezeFor(Enemy enemy, float duration)
        {
            if (IsBoss(enemy))
                enemy.Slow(k_BossFreezeSlow, duration);
            else
                enemy.Freeze(duration);

            // Le dragon (ralentissement limité) garde ses couleurs : il a déjà son voile de givre.
            if (!m_Frozen.TryGetValue(enemy, out var frozen))
            {
                frozen = new Frozen { Renderers = enemy.MaxSlow < 1f ? Array.Empty<Renderer>() : enemy.GetComponentsInChildren<Renderer>() };
                m_Frozen[enemy] = frozen;
                Tint(frozen, true);
            }

            frozen.Until = Mathf.Max(frozen.Until, Time.time + duration);
            var arrows = SpecialArrows.Instance;
            if (arrows != null)
            {
                Sfx.Play(arrows.FrostClip, enemy.Center, 0.8f, 1.25f);
                FloatingText.Spawn(enemy.Center + Vector3.up * 0.6f, "Gelé !", arrows.FrostColor, 0.6f, 0.9f);
            }
        }

        // L'ennemi gelé éclate : des éclats de glace blessent tout ce qui est autour (lui compris), et il dégèle.
        void Shatter(Enemy enemy, in ArrowHit hit, int level)
        {
            var center = enemy.Center;
            var glacier = GlacierUnder(enemy);
            enemy.Thaw();
            if (m_Frozen.TryGetValue(enemy, out var frozen))
            {
                Tint(frozen, false);
                m_Frozen.Remove(enemy);
            }

            var arrows = SpecialArrows.Instance;
            var color = arrows != null ? arrows.FrostColor : k_FrozenTint;
            LightningBolt.Burst(center, k_ShatterRadius * 0.6f, color, EffectMaterial, 14);
            if (arrows != null)
                Sfx.Play(arrows.FrostClip, center, 1f, 0.75f);
            FloatingText.Spawn(center + Vector3.up * 0.6f, "Éclat !", color, 0.7f, 0.9f);

            var damage = hit.Damage * (k_ShatterDamage + k_ShatterDamagePerLevel * (level - 1));
            m_Enemies.Clear();
            m_Enemies.AddRange(Enemy.Alive);
            foreach (var other in m_Enemies)
            {
                if (other == null || !other.IsAlive)
                    continue;

                var otherCenter = other.Center;
                var distance = Vector3.Distance(otherCenter, center);
                if (distance > k_ShatterRadius)
                    continue;

                var direction = otherCenter - center;
                Hurt(other, damage * Mathf.Lerp(1f, 0.5f, distance / k_ShatterRadius), otherCenter,
                     direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.up, hit);
            }

            if (glacier != null)
                GlacierFreeze(enemy, glacier, level);
        }

        // Les ennemis gelés reprennent leurs couleurs à la fin du gel (ou disparus).
        void UpdateFrozen()
        {
            if (m_Frozen.Count == 0)
                return;

            m_Thawed.Clear();
            foreach (var pair in m_Frozen)
            {
                if (pair.Key == null || !pair.Key.IsAlive || Time.time >= pair.Value.Until)
                    m_Thawed.Add(pair.Key);
            }

            foreach (var enemy in m_Thawed)
            {
                Tint(m_Frozen[enemy], false);
                m_Frozen.Remove(enemy);
            }
        }

        // Un bleu glacé sur les maillages de l'ennemi (ses matériaux gardent leurs textures), retiré au dégel.
        void Tint(Frozen frozen, bool on)
        {
            if (on && m_FrozenBlock == null)
            {
                m_FrozenBlock = new MaterialPropertyBlock();
                m_FrozenBlock.SetColor(k_BaseColorId, k_FrozenTint);
                m_FrozenBlock.SetColor(k_ColorId, k_FrozenTint);
            }

            foreach (var renderer in frozen.Renderers)
            {
                if (renderer != null && (renderer is SkinnedMeshRenderer || renderer is MeshRenderer))
                    renderer.SetPropertyBlock(on ? m_FrozenBlock : null);
            }
        }

        // ---------------------------------------------------------------- talisman d'acrobate

        // En l'air ou en glissade (et un instant après), le temps ralentit ; ClaimPerfect rend alors chaque tir parfait.
        void UpdateAcrobat()
        {
            var level = Level(UpgradeEffect.Acrobat);
            var active = false;
            if (level > 0 && m_Locomotion == null && Time.unscaledTime >= m_NextLocomotionSearch)
            {
                m_NextLocomotionSearch = Time.unscaledTime + 1f;
                m_Locomotion = FindAnyObjectByType<ArmSwingLocomotion>();
            }

            // La glissade s'arrête à mi-vitesse tant que le talisman est équipé (26 octobre).
            if (m_Locomotion != null)
                m_Locomotion.SlideEndSpeedShare = level > 0 ? k_AcrobatSlideEnd : 0f;

            if (level > 0 && !SlowMotion.IsPaused)
            {
                var desktop = DesktopControls.Instance;
                if ((m_Locomotion != null && (m_Locomotion.IsSliding || m_Locomotion.IsAirborne)) || (desktop != null && desktop.IsAirborne))
                    m_AcrobatUntil = Time.unscaledTime + k_AcrobatGrace;
                active = Time.unscaledTime < m_AcrobatUntil;
            }

            if (active == m_AcrobatActive)
                return;

            m_AcrobatActive = active;
            if (active)
                SlowMotion.Begin(this, Mathf.Max(0.3f, k_AcrobatScale - k_AcrobatScalePerLevel * (level - 1)));
            else
                SlowMotion.End(this);

            // Le ralenti s'entend (dans la tête du joueur) : il commence, puis il finit.
            var head = PlayerRig.Instance != null ? PlayerRig.Instance.Head : null;
            Sfx.Play(active ? PieceSounds.SlowIn : PieceSounds.SlowOut, head != null ? head.position : transform.position, 0.55f, 1f, 0f);
        }

        // ---------------------------------------------------------------- outils

        static Material EffectMaterial => SpecialArrows.Instance != null ? SpecialArrows.Instance.EffectMaterial : null;

        bool IsBoss(Enemy enemy)
        {
            if (!m_IsBoss.TryGetValue(enemy, out var boss))
            {
                if (m_IsBoss.Count > 128)
                    m_IsBoss.Clear();
                boss = enemy.MaxSlow < 1f || enemy.GetComponent<Boss>() != null;
                m_IsBoss[enemy] = boss;
            }

            return boss;
        }

        // La source n'est pas une flèche : ces dégâts ne font ni points de touche ni combo, mais l'élimination compte.
        void Hurt(Enemy enemy, float amount, Vector3 point, Vector3 direction, in ArrowHit hit, HitZone zone = HitZone.Body)
        {
            if (amount <= 0f)
                return;

            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = amount,
                Zone = zone,
                Grade = hit.Grade,
                Distance = hit.TravelDistance,
                Point = point,
                Direction = direction,
                Source = this,
            });
        }
    }
}
