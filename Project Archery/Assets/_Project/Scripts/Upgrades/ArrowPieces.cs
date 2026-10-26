using System;
using System.Collections.Generic;
using Archery.Bows;
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
    /// <item>empennage boomerang : à 22 m (ou au bout de 1,1 s), la flèche fait demi-tour et revient vers le joueur, en
    /// traversant des ennemis à l'aller et au retour ; rattrapée (elle passe près d'une main), elle disparaît et le tir
    /// suivant est parfait ;</item>
    /// <item>empennage télékinésie : tant que la gâchette de la main qui tient l'arme reste appuyée (le clic droit sans
    /// casque), les flèches du tir suivent la direction où pointe l'arme, pendant 3 s ;</item>
    /// <item>pointe à mèche : la flèche plantée crépite, puis explose 1 s plus tard ;</item>
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

        // Empennage boomerang : la distance (m) ou le temps (s) du demi-tour, le virage (°/s), la vitesse du retour (au moins),
        // le temps au-delà duquel elle ne revient plus (elle retombe), la distance d'une main qui la rattrape (m), les ennemis
        // traversés à chaque passage par niveau, et les tirs parfaits gardés en réserve au plus.
        const float k_BoomerangRange = 22f;
        const float k_BoomerangOutTime = 1.1f;
        const float k_BoomerangTurnRate = 540f;
        const float k_BoomerangMinSpeed = 20f;
        const float k_BoomerangReturnTime = 3f;
        const float k_CatchRadius = 0.4f;
        const int k_BoomerangPiercePerLevel = 1;
        const int k_MaxQueuedPerfects = 3;

        // Empennage télékinésie : le virage (°/s) au niveau 1 et par niveau en plus, la durée (s) après le tir, la part de vitesse
        // perdue par seconde, la vitesse minimale (m/s), et combien la flèche tourne quand la main tourne (2 : la main tourne de
        // 20°, la flèche de 40° ; sans casque, la caméra, ×1).
        const float k_SteerRate = 110f;
        const float k_SteerRatePerLevel = 45f;
        const float k_SteerTime = 3f;
        const float k_SteerSlowdown = 0.12f;
        const float k_SteerMinSpeed = 12f;
        const float k_SteerGain = 2f;

        // Pointe à mèche : le délai (s), les dégâts au centre en part du tir (moitié au bord) et le rayon (m), avec ce qui
        // s'ajoute par niveau ; les mèches en même temps au plus ; l'intervalle (s) des étincelles.
        const float k_FuseDelay = 1f;
        const float k_FuseDamage = 1f;
        const float k_FuseDamagePerLevel = 0.25f;
        const float k_FuseRadius = 3.5f;
        const float k_FuseRadiusPerLevel = 0.75f;
        const int k_MaxFuses = 12;
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
        // les aimants en même temps au plus.
        const float k_MagnetRadius = 6f;
        const float k_MagnetRadiusPerLevel = 1.5f;
        const float k_MagnetTime = 2.5f;
        const float k_MagnetTimePerLevel = 0.5f;
        const float k_PullSpeed = 4f;
        const int k_MaxMagnets = 3;

        // Fût conducteur : la distance (m) entre deux flèches reliées, la durée (s) et par niveau, les dégâts par seconde en part
        // du tir (et par niveau) à chaque ennemi traversé, le pas (s) des dégâts, l'épaisseur (m) de l'arc, sa hauteur au-dessus
        // de l'encoche, les liens et les flèches gardés au plus, le scintillement (s).
        const float k_LinkRange = 8f;
        const float k_LinkTime = 6f;
        const float k_LinkTimePerLevel = 1.5f;
        const float k_LinkDamage = 0.8f;
        const float k_LinkDamagePerLevel = 0.4f;
        const float k_LinkTick = 0.25f;
        const float k_LinkRadius = 0.45f;
        const float k_LinkHeight = 0.25f;
        const int k_MaxLinks = 4;
        const int k_MaxNodes = 8;
        const float k_LinkFlicker = 0.08f;

        // Talisman d'acrobate : la vitesse du temps au niveau 1 (0,1 de moins par niveau, 0,3 au plus bas), et le temps qu'il dure
        // encore après l'atterrissage ou la fin de la glissade (s, temps réel).
        const float k_AcrobatScale = 0.7f;
        const float k_AcrobatScalePerLevel = 0.1f;
        const float k_AcrobatGrace = 0.25f;

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

            // La télékinésie : la gâchette est tenue ; l'orientation de la main et la direction de la flèche à l'appui.
            public bool Steering;
            public Quaternion SteerReference;
            public Vector3 SteerDirection;

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
        }

        class Fuse
        {
            public Arrow Arrow;
            public Vector3 Point;
            public float ExplodeAt;
            public float NextSpark;
            public float Damage;
            public ShotGrade Grade;
        }

        class Magnet
        {
            public Arrow Arrow;
            public Vector3 Point;
            public float EndAt;
            public float Radius;
            public LineRenderer Ring;
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

        /// <summary>Tirs parfaits gagnés en rattrapant des flèches boomerang, pas encore tirés.</summary>
        public int QueuedPerfects => m_QueuedPerfects;

        void OnEnable()
        {
            Instance = this;
            Arrow.AnyHit += OnArrowHit;
            Arrow.Stuck += OnArrowStuck;
            Arrow.ShotEnded += OnShotEnded;
            Bow.ClaimPerfectShot = ClaimPerfect;
            EnableLot2();
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
            Arrow.AnyHit -= OnArrowHit;
            Arrow.Stuck -= OnArrowStuck;
            Arrow.ShotEnded -= OnShotEnded;
            if (Bow.ClaimPerfectShot == (Func<bool>)ClaimPerfect)
                Bow.ClaimPerfectShot = null;

            SlowMotion.End(this);
            m_AcrobatActive = false;
            if (m_SteerHum != null)
                m_SteerHum.Stop();
            m_Flights.Clear();
            m_Fuses.Clear();
            m_Nodes.Clear();
            m_Links.Clear();
            while (m_Magnets.Count > 0)
                RemoveMagnet(m_Magnets.Count - 1);
            foreach (var frozen in m_Frozen.Values)
                Tint(frozen, false);
            m_Frozen.Clear();
            DisableLot2();
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
            flight.Steering = false;
            flight.PreviousTip = arrow.TipPosition;
            flight.IsChild = m_TrackingChild;
            flight.Bounces = 0;
            flight.SniperFactor = 1f;
            flight.Trips = 0;
            flight.FirstDirection = arrow.Velocity.sqrMagnitude > 1e-4f ? arrow.Velocity.normalized : arrow.transform.forward;
            flight.Hovering = false;
            flight.Released = false;

            var boomerang = Level(UpgradeEffect.Boomerang);
            if (boomerang > 0)
                arrow.AddPierce(k_BoomerangPiercePerLevel * boomerang);
            TrackLot2(arrow, flight);
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

        // Une flèche boomerang rattrapée, ou le talisman d'acrobate en l'air : ce tir compte comme parfait (Bow.ClaimPerfectShot).
        bool ClaimPerfect()
        {
            if (m_AcrobatActive)
                return true;
            if (m_QueuedPerfects <= 0)
                return false;

            m_QueuedPerfects--;
            return true;
        }

        void Update()
        {
            UpdateAcrobat();
            UpdateSteerHum();
            UpdateFuses();
            UpdateMagnets();
            UpdateLinks();
            UpdateFrozen();
            UpdateLot2();
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
            var detonators = m_Detonators.Count > 0;
            if (boomerang == 0 && steer == 0 && sniper == 0 && suspended == 0 && !detonators)
                return;

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

                if (detonators && !flight.IsChild)
                    FindDetonator(flight);
                flight.PreviousTip = arrow.TipPosition;
            }

            // Après la boucle : rattraper une flèche la renvoie au pool, ce qui la retire des flèches suivies ; une détonation
            // lance des flèches, qui s'ajoutent aux flèches suivies.
            for (var i = 0; i < m_Caught.Count; i++)
                Catch(m_Caught[i], m_CatchHands[i]);
            foreach (var detonator in m_Detonating)
                Detonate(detonator);

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

        // À l'aller, la flèche vole normalement ; passé la distance (ou le temps) du demi-tour, elle vire vers le joueur à vitesse
        // constante, sans tomber, et peut de nouveau toucher les ennemis traversés à l'aller. Si elle le manque, elle retombe.
        void UpdateBoomerang(Flight flight, int level, float deltaTime)
        {
            var arrow = flight.Arrow;
            var velocity = arrow.Velocity;
            var speed = velocity.magnitude;
            if (!flight.Returning)
            {
                var far = (arrow.transform.position - flight.Origin).sqrMagnitude >= k_BoomerangRange * k_BoomerangRange;
                if (!far && Time.time - flight.LaunchTime < k_BoomerangOutTime)
                    return;

                flight.Returning = true;
                flight.ReturnStart = Time.time;
                flight.ReturnSpeed = Mathf.Max(k_BoomerangMinSpeed, speed);
                arrow.ResetPierced();
                arrow.AddPierce(k_BoomerangPiercePerLevel * level);

                // Le souffle du demi-tour, pour la flèche de l'arme seulement (pas un par flèche de la volée).
                if (!arrow.IsExtra)
                    Sfx.Play(PieceSounds.Whoosh, arrow.transform.position, 0.8f, UnityEngine.Random.Range(0.95f, 1.1f));
            }

            if (flight.ReturnDone)
                return;

            var toTarget = ReturnPoint() - arrow.TipPosition;
            var distance = toTarget.magnitude;
            if (distance < 0.5f || Time.time - flight.ReturnStart > k_BoomerangReturnTime)
            {
                // Le duo « Faux » : manquée, elle repart pour un deuxième aller-retour, dans la direction du premier.
                if (flight.Trips == 0 && ArrowDuos.IsActive(ArrowDuos.Scythe))
                {
                    flight.Trips = 1;
                    flight.Returning = false;
                    flight.Origin = arrow.transform.position;
                    flight.LaunchTime = Time.time;
                    arrow.ResetPierced();
                    arrow.AddPierce(k_BoomerangPiercePerLevel * level);
                    arrow.Velocity = flight.FirstDirection * flight.ReturnSpeed - Physics.gravity * (arrow.GravityScale * deltaTime);
                    return;
                }

                flight.ReturnDone = true;
                return;
            }

            var direction = speed > 0.1f ? velocity / speed : toTarget / distance;
            var turned = Vector3.RotateTowards(direction, toTarget / distance, k_BoomerangTurnRate * Mathf.Deg2Rad * deltaTime, 0f);

            // La gravité du pas physique qui suit est annulée d'avance.
            arrow.Velocity = turned * flight.ReturnSpeed - Physics.gravity * (arrow.GravityScale * deltaTime);
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

        // La flèche passe près d'une main (entre sa pointe d'avant et celle de maintenant, pour ne pas la rater à pleine vitesse) ;
        // sans casque, près de la caméra.
        bool TryCatch(Flight flight, out IXRSelectInteractor hand)
        {
            hand = null;
            var arrow = flight.Arrow;
            var tip = arrow.TipPosition;
            var nock = arrow.transform.position;
            if ((tip - ReturnPoint()).sqrMagnitude > 9f)
                return false;

            var desktop = DesktopControls.Instance;
            if (desktop != null && desktop.IsActive)
                return desktop.CameraTransform != null && DistanceToSegment(desktop.CameraTransform.position, flight.PreviousTip, tip) < 1f;

            var rig = PlayerRig.Instance;
            if (rig == null)
                return false;

            foreach (var candidate in rig.Hands)
            {
                if (candidate == null)
                    continue;

                var position = candidate.transform.position;
                if (DistanceToSegment(position, flight.PreviousTip, tip) < k_CatchRadius || DistanceToSegment(position, nock, tip) < k_CatchRadius)
                {
                    hand = candidate;
                    return true;
                }
            }

            return false;
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
            if (flight.Returning || Time.time - flight.LaunchTime > k_SteerTime || !SteerInput(flight, out var pose, out var gain))
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

            var rate = (k_SteerRate + k_SteerRatePerLevel * (level - 1)) * Mathf.Deg2Rad * deltaTime;
            var direction = Vector3.RotateTowards(velocity / speed, aim.normalized, rate, 0f);
            speed = Mathf.Max(k_SteerMinSpeed, speed * (1f - k_SteerSlowdown * deltaTime));

            // Guidée, elle ne tombe plus : la gravité du pas physique qui suit est annulée d'avance.
            arrow.Velocity = direction * speed - Physics.gravity * (arrow.GravityScale * deltaTime);
        }

        // ---------------------------------------------------------------- plantée : mèche, aimant, conducteur

        void OnArrowStuck(Arrow arrow, Collider collider)
        {
            if (!m_Flights.TryGetValue(arrow, out var flight))
                return;

            var enemy = collider != null ? collider.GetComponentInParent<Enemy>() : null;
            if (Level(UpgradeEffect.Fuse) > 0 && m_Fuses.Count < k_MaxFuses)
            {
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

            var magnet = Level(UpgradeEffect.Magnet);
            if (magnet > 0)
                AddMagnet(arrow, magnet);

            // L'arc électrique relie les flèches plantées dans le décor (celles plantées dans un ennemi bougent avec lui).
            var conductor = Level(UpgradeEffect.Conductor);
            if (conductor > 0 && enemy == null)
                AddNode(arrow, flight, conductor);

            OnStuckLot2(arrow, flight, enemy);
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
                var attached = StillAt(fuse.Arrow, fuse.Point);
                if (attached)
                    fuse.Point = fuse.Arrow.TipPosition;

                if (Time.time < fuse.ExplodeAt)
                {
                    if (Time.time >= fuse.NextSpark)
                    {
                        fuse.NextSpark = Time.time + k_FuseSparkInterval;
                        LightningBolt.Burst(fuse.Point, 0.22f, k_FuseColor, EffectMaterial, 2);
                    }

                    continue;
                }

                m_Fuses.RemoveAt(i);
                if (attached)
                    fuse.Arrow.Despawn();

                var arrows = SpecialArrows.Instance;
                if (arrows == null)
                    continue;

                var level = Mathf.Max(1, Level(UpgradeEffect.Fuse));
                var hit = new ArrowHit { Point = fuse.Point, Direction = Vector3.down, Damage = fuse.Damage, Grade = fuse.Grade, IsShot = true };
                var radius = k_FuseRadius + k_FuseRadiusPerLevel * (level - 1);

                // Le duo « Trou noir » : l'explosion aspire encore une seconde, et son rayon double.
                if (ArrowDuos.IsActive(ArrowDuos.BlackHole))
                {
                    radius *= 2f;
                    AddMagnetAt(fuse.Point, null, 1f, radius);
                }

                arrows.Blast(fuse.Point, fuse.Damage * (k_FuseDamage + k_FuseDamagePerLevel * (level - 1)), radius, k_FuseColor, hit);
            }
        }

        // Fût aimant : un anneau violet se resserre sans cesse autour de la flèche ; les ennemis au sol et les volants proches
        // glissent vers elle (la poussée des ennemis, Enemy.Push), sauf les boss.
        void AddMagnet(Arrow arrow, int level) =>
            AddMagnetAt(arrow.TipPosition, arrow, k_MagnetTime + k_MagnetTimePerLevel * (level - 1), k_MagnetRadius + k_MagnetRadiusPerLevel * (level - 1));

        // Un aimant à ce point (qui suit la flèche plantée, s'il y en a une) : le fût aimant, le trou noir, la faille du katana.
        void AddMagnetAt(Vector3 point, Arrow arrow, float duration, float radius)
        {
            if (m_Magnets.Count >= k_MaxMagnets)
                RemoveMagnet(0);

            var magnet = new Magnet
            {
                Arrow = arrow,
                Point = point,
                EndAt = Time.time + duration,
                Radius = radius,
                Ring = TakeRing(),
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

                    enemy.Push(offset / distance * (k_PullSpeed * Mathf.Lerp(0.6f, 1f, distance / magnet.Radius)));
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
        void AddNode(Arrow arrow, Flight flight, int level)
        {
            var now = Time.time;
            var node = new Node
            {
                Point = arrow.transform.position + Vector3.up * k_LinkHeight,
                EndAt = now + k_LinkTime + k_LinkTimePerLevel * (level - 1),
                Damage = flight.Damage,
                Grade = flight.Grade,
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

            m_Nodes.Add(node);
            if (m_Nodes.Count > k_MaxNodes)
                m_Nodes.RemoveAt(0);
        }

        void AddLink(Node a, Node b)
        {
            if (m_Links.Count >= k_MaxLinks)
                m_Links.RemoveAt(0);

            m_Links.Add(new Link { A = a, B = b, EndAt = Mathf.Min(a.EndAt, b.EndAt) });
            var arrows = SpecialArrows.Instance;
            if (arrows != null)
                Sfx.Play(arrows.LightningClip, (a.Point + b.Point) * 0.5f, 0.5f, 1.4f);
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
                }
            }
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

            if (enemy.IsFrozen || (m_Frozen.TryGetValue(enemy, out var frozen) && frozen.Until > Time.time))
                Shatter(enemy, hit, level);
            else if (hit.Grade == ShotGrade.Perfect && enemy.IsAlive)
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

        void Freeze(Enemy enemy, int level)
        {
            var duration = k_FreezeTime + k_FreezeTimePerLevel * (level - 1);
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
            if (level > 0 && !SlowMotion.IsPaused)
            {
                if (m_Locomotion == null && Time.unscaledTime >= m_NextLocomotionSearch)
                {
                    m_NextLocomotionSearch = Time.unscaledTime + 1f;
                    m_Locomotion = FindAnyObjectByType<ArmSwingLocomotion>();
                }

                if (m_Locomotion != null && (m_Locomotion.IsSliding || m_Locomotion.IsAirborne))
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
