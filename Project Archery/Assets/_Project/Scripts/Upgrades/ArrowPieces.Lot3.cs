using System.Collections.Generic;
using Archery.Bows;
using Archery.Building;
using Archery.Core;
using Archery.Enemies;
using Archery.FinalBoss;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Upgrades
{
    // Le troisième lot de pièces et ses duos (26 octobre au soir : proposés par Claude, choisis par le joueur ;
    // Docs/Refonte_Roguelike.md, partie 14) :
    // - corde de siège : au bout de l'anneau (le vert clair), garder la tension charge le tir, jusqu'à 2,5 fois les dégâts d'un
    //   tir parfait et 3 ennemis traversés en 2 s ; vaut aussi pour le laser de l'arc cosmique (Bow.HeldChargeMultiplier) ;
    // - corde de pluie : la flèche de l'arc sème des flèches tout le long de sa traînée, selon la distance parcourue (revue le 27
    //   octobre ; avant, un tir vers le ciel éclatait à son sommet) ; avec l'arc légendaire, sa pluie est multipliée (×2, ×3, ×4) ;
    // - corde fantôme : dans la seconde après un tir, attraper la corde à vide (la poignée, près de l'encoche), la tirer et la
    //   lâcher décoche une flèche fantôme, avec les pièces ; sans casque, le clic du milieu ;
    // - empennage piqué : tirée vers le haut, la flèche monte un instant (ou jusqu'à son sommet) puis pique sur l'ennemi le plus
    //   proche de la visée (revu le 27 octobre) ;
    // - empennage de rappel : fermer le poing de la main libre (devant soi, loin de la corde et du carquois) rappelle les
    //   flèches plantées (aussi dans un ennemi, qu'elles blessent en s'arrachant) : elles reviennent vers le joueur et blessent
    //   les ennemis qu'elles frôlent (revu le 27 octobre) ; sans casque, Ctrl gauche ;
    // - fût-sentinelle : plantée au sol, la flèche tire chaque seconde une petite flèche sur l'ennemi le plus proche ;
    // - talisman de l'avare : dans PlayerUpgrades (les dégâts suivent l'or gardé).
    // Les duos : orage magnétique (MagnetStorm), lance-grenades (GrenadeBounce), chasse à l'homme (TryExecute), pluie de fer
    // (IronRain), billard (FixedUpdate), clôture de givre (FrostFence), réseau (LinkToBuilding), rail (Rail), glacier
    // (GlacierFreeze, revu le 27 octobre) ; supernova et étoile filante sont dans SpecialArrows.
    public partial class ArrowPieces
    {
        // --- Réglages du troisième lot (à ajuster au test) ---

        // Corde de siège : le temps (s) d'une charge complète ; les dégâts à pleine charge, en fois ceux d'un tir parfait, aux niveaux
        // 1, 2 et 3 ; les ennemis traversés en plus à pleine charge ; l'intervalle (s) des étincelles pendant la charge.
        const float k_SiegeChargeTime = 2f;
        static readonly Vector3 k_SiegeMax = new Vector3(2.5f, 3f, 3.5f);
        static readonly Vector3 k_SiegePierce = new Vector3(3f, 4f, 5f);
        const float k_SiegeSparkInterval = 0.1f;

        // Corde de pluie, revue le 27 octobre (demandé par le joueur) : la flèche de l'arc sème des flèches tout le long de sa
        // traînée, une tous les tant de m parcourus (niveaux 1, 2, 3 : à la distance, pas au temps), à partir de tant de m de
        // l'arc (pas sur le joueur), tant au plus par tir ; chacune tombe d'un peu sous la flèche, en gardant une part de sa vitesse
        // à plat, et prend une part des dégâts du tir. Avec l'arc légendaire, sa pluie est multipliée par (1 + niveau) au lieu de
        // la traînée. Le nuage et l'averse se servent encore de l'angle, du nombre de flèches et du rayon de l'ancienne pluie.
        // (Une tous les 4 / 3 / 2 m, à partir de 3 m de l'arc, depuis le 27 octobre au soir, réglages du joueur ; 3 / 2,25 / 1,5 et
        // 4 m avant.)
        static readonly Vector3 k_RainStep = new Vector3(4f, 3f, 2f);
        const float k_RainStart = 3f;
        static readonly Vector3 k_RainMaxDrops = new Vector3(16f, 24f, 32f);
        const float k_RainCarry = 0.3f;
        const float k_RainDropSpeed = 4f;
        const float k_RainMinPitch = 45f;
        static readonly Vector3 k_RainArrows = new Vector3(8f, 12f, 16f);
        const float k_RainRadius = 3.5f;
        const float k_RainRadiusPerLevel = 0.5f;
        // (35 % des dégâts du tir depuis le 27 octobre au soir, équilibrage du joueur ; 45 % avant.)
        const float k_RainDamage = 0.35f;

        // Corde fantôme : le temps (s) après un tir pour attraper la corde, la distance (m) de la main à l'encoche, la longueur
        // (m) à tirer, le temps (s) au plus où on la garde, la part des dégâts du tir (niveaux 1, 2, 3).
        const float k_GhostWindow = 1.2f;
        const float k_GhostGrab = 0.22f;
        const float k_GhostPull = 0.3f;
        const float k_GhostMaxHold = 3f;
        // (120 / 150 / 200 % depuis le 27 octobre, réglage du joueur ; 60 / 75 / 90 % avant.)
        static readonly Vector3 k_GhostDamage = new Vector3(1.2f, 1.5f, 2f);

        // Empennage piqué : l'angle (°) au-dessus de l'horizontale d'un tir qui pique, la distance (m) du tireur où il cherche un
        // ennemi (niveaux 1, 2, 3) et l'écart (°) à la visée, le temps (s) après le tir où il pique (ou à son sommet, s'il vient
        // avant), les dégâts du piqué (×), sa vitesse (m/s) et son virage (°/s).
        // Revu le 27 octobre (le joueur : « l'empennage piqué ne semble pas fonctionner ») : avant, il attendait le sommet de la
        // trajectoire (à pleine vitesse, plus de 80 m plus loin : la flèche touchait presque toujours quelque chose avant) et
        // cherchait un ennemi à 12-18 m de son point de chute (rien, si loin).
        const float k_DiveMinPitch = 15f;
        static readonly Vector3 k_DiveRange = new Vector3(40f, 50f, 60f);
        const float k_DiveCone = 30f;
        const float k_DiveDelay = 0.45f;
        static readonly Vector3 k_DiveDamage = new Vector3(1.5f, 1.75f, 2f);
        const float k_DiveSpeed = 40f;
        const float k_DiveTurnRate = 900f;

        // Empennage de rappel : les flèches plantées gardées au plus, la part des dégâts du tir de la flèche rappelée (niveaux 1,
        // 2, 3), sa vitesse (m/s), son virage (°/s), la distance (m) où elle disparaît en arrivant.
        // (100 depuis le 27 octobre, réglage du joueur ; 24 avant.)
        const int k_MaxRecall = 100;
        static readonly Vector3 k_RecallDamage = new Vector3(0.8f, 1f, 1.2f);
        const float k_RecallSpeed = 28f;
        const float k_RecallTurnRate = 720f;
        const float k_RecallArrive = 1.2f;

        // Depuis le 27 octobre (le joueur : « le rappel ne semble pas faire de dégâts » ; la flèche devait toucher l'ennemi au rayon
        // près, et une flèche plantée dans un ennemi n'était jamais rappelée) : la flèche rappelée blesse tout ennemi à moins de
        // k_RecallRadius m de son trajet, une fois chacun, et une flèche plantée dans un ennemi le blesse en s'arrachant.
        const float k_RecallRadius = 0.9f;

        // Un détonateur dont la flèche a été rappelée reste ce temps (s) là où elle était.
        const float k_DetachedDetonatorTime = 6f;

        // Le duo « Nuage » (27 octobre, à ajuster au test) : la flèche s'arrête au bout de tant de s, pleut pendant tant de s, une
        // averse toutes les tant de s ; en tout, tant de fois les flèches de la corde de pluie. Le duo « Averse » : les flèches de
        // chaque petite pluie (niveaux 1, 2, 3 de la corde de pluie), son rayon (m), l'écart (m) entre les pluies des flèches.
        // Il se forme aussi dès que la flèche est k_CloudHeight m au-dessus du tir (sans gravité, elle monterait de 30 m en 1 s),
        // puis avance doucement (m/s) dans la direction du tir pendant qu'il pleut.
        const float k_CloudDelay = 1f;
        const float k_CloudHeight = 12f;
        const float k_CloudDrift = 5f;
        const float k_CloudTime = 2.5f;
        const float k_CloudInterval = 0.25f;
        const float k_CloudShare = 2f;
        static readonly Vector3 k_DownpourArrows = new Vector3(3f, 4f, 5f);
        const float k_DownpourRadius = 2.5f;
        const float k_DownpourSpread = 1.5f;

        class RainCloud
        {
            public Arrow Arrow;
            public Vector3 Point;
            public Vector3 Drift;
            public float EndAt;
            public float NextDrop;
            public float Damage;
            public ShotGrade Grade;
        }

        readonly List<RainCloud> m_Clouds = new List<RainCloud>();
        Arrow m_CloudArrow;

        // La corde de pluie : la flèche de l'arc du tir en cours, et les flèches à lâcher après la boucle des vols.
        struct RainDrop
        {
            public Vector3 From;
            public Vector3 Velocity;
            public float Damage;
            public ShotGrade Grade;
        }

        readonly List<RainDrop> m_RainDrops = new List<RainDrop>();
        readonly List<RainDrop> m_DropsToPlay = new List<RainDrop>();
        Arrow m_RainArrow;
        static readonly Color k_RecallHitColor = new Color(0.55f, 0.92f, 1f);

        // Fût-sentinelle : les sentinelles à la fois et leur durée (s) (niveaux 1, 2, 3), l'intervalle (s) entre deux tirs, la
        // portée (m), la part des dégâts du tir de chaque petite flèche (niveaux 1, 2, 3), leur vitesse (m/s) et leur gravité.
        // (100 à tous les niveaux depuis le 27 octobre, réglage du joueur ; 4 / 5 / 6 avant : les flèches de la corde de pluie les
        // remplaçaient sans cesse, avant même leur premier tir.)
        static readonly Vector3 k_MaxSentinels = new Vector3(100f, 100f, 100f);
        static readonly Vector3 k_SentinelTime = new Vector3(5f, 6.5f, 8f);
        const float k_SentinelInterval = 1f;
        const float k_SentinelRange = 15f;
        static readonly Vector3 k_SentinelDamage = new Vector3(0.3f, 0.4f, 0.5f);
        const float k_SentinelSpeed = 35f;
        const float k_SentinelGravity = 0.3f;

        // Duos : l'orage magnétique (part des dégâts du tir par éclair, ennemis au plus), le lance-grenades (part des dégâts de la
        // mèche à chaque rebond, au rayon de la mèche), la chasse à l'homme (le seuil multiplié), la pluie de fer (pieux, distance
        // en m), la clôture de givre (gel en s, pas avant tant de s pour le même ennemi), le rail (vitesse ×, durée de la traînée en
        // s, pas en s entre deux points, part des dégâts par coup, intervalle des coups en s, rayon en m).
        const float k_MagnetStormDamage = 0.5f;
        // (100 ennemis et 75 % depuis le 27 octobre, réglages du joueur ; 8 et 50 % avant.)
        const int k_MagnetStormTargets = 100;
        const float k_GrenadeBounceDamage = 0.75f;
        // (Seuil ×1,5 depuis le 27 octobre au soir, équilibrage du joueur : doublé, une tête marquée sous 50 % mourait d'un coup.)
        const float k_ManhuntThreshold = 1.5f;
        const int k_IronRainSpikes = 3;
        static readonly Vector2 k_IronRainDistance = new Vector2(1.5f, 2.5f);
        const float k_FenceFreeze = 1f;
        const float k_FenceCooldown = 3f;
        const float k_RailSpeed = 3f;
        const float k_RailLife = 1f;
        const float k_RailStep = 0.04f;
        const float k_RailDamage = 0.25f;
        const float k_RailTick = 0.2f;
        const float k_RailRadius = 0.5f;
        const int k_MaxRailSegments = 60;

        // Les duos du rebond (27 octobre, à ajuster au test) : la patrouille (petites flèches par rebond, niveaux 1, 2, 3 de la
        // sentinelle), les chausse-trappes (graines de pieux par rebond au sol, leur part des dégâts), le flipper (durée en s de
        // l'aimant, son rayon en part de celui du fût aimant, sa force), la fragmentation (flèches par rebond, niveaux 1, 2, 3 du
        // détonateur, leur part des dégâts d'une détonation).
        // (2 / 3 / 4 petites flèches, et des graines à 100 % des dégâts du tir, depuis le 27 octobre, réglages du joueur ; 1 / 1 / 2
        // et 50 % avant.)
        static readonly Vector3 k_PatrolShots = new Vector3(2f, 3f, 4f);
        const int k_CaltropSeeds = 2;
        const float k_CaltropDamage = 1f;
        const float k_PinballTime = 1f;
        const float k_PinballRadius = 0.5f;
        const float k_PinballPull = 1.5f;
        static readonly Vector3 k_FragmentArrows = new Vector3(2f, 3f, 4f);
        const float k_FragmentDamage = 0.5f;

        // Les duos du boomerang (27 octobre, à ajuster au test) : le retour de flamme (part des dégâts de la mèche), le tourbillon
        // (durée en s de l'aimant, sa force), l'escorte (petites flèches, niveaux 1, 2, 3 de la sentinelle), le semeur (graines,
        // leur part des dégâts), la mitraille (flèches, niveaux 1, 2, 3 du détonateur, leur part des dégâts d'une détonation).
        // Un seul demi-tour par flèche (deux avec la faux) : plus fort qu'à chaque rebond.
        const float k_BackfireDamage = 0.75f;
        const float k_LiveWireGap = 1f;
        const float k_WhirlwindTime = 2f;
        const float k_WhirlwindPull = 1.25f;
        static readonly Vector3 k_EscortShots = new Vector3(2f, 3f, 4f);
        const int k_SowerSeeds = 3;
        const float k_SowerDamage = 0.6f;
        static readonly Vector3 k_ShrapnelArrows = new Vector3(3f, 4f, 5f);
        const float k_ShrapnelDamage = 0.75f;

        static readonly Color k_SiegeColor = new Color(1f, 0.88f, 0.45f);
        static readonly Color k_RainColor = new Color(0.55f, 0.8f, 1f);
        static readonly Color k_GhostColor = new Color(0.6f, 0.95f, 1f, 0.8f);
        static readonly Color k_DiveColor = new Color(1f, 0.6f, 0.25f);
        static readonly Color k_RecallColor = new Color(0.4f, 0.85f, 1f);
        static readonly Color k_SentinelColor = new Color(0.95f, 0.85f, 0.45f);
        static readonly Color k_RailColor = new Color(0.8f, 0.97f, 1f);

        class Planted
        {
            public Arrow Arrow;
            public Vector3 Point;
            public float Damage;
            public ShotGrade Grade;

            // L'ennemi dans lequel elle est plantée (elle bouge avec lui), ou null (le décor).
            public Enemy Enemy;
        }

        class Sentinel
        {
            public Arrow Arrow;
            public Vector3 Point;
            public float EndAt;
            public float NextShot;
            public float Damage;
            public ShotGrade Grade;
        }

        class RailSegment
        {
            public Vector3 A;
            public Vector3 B;
            public float EndAt;
            public float Damage;
            public ShotGrade Grade;
        }

        // Un demi-tour du boomerang dont les duos attendent la fin de la boucle des vols.
        struct BoomerangTurn
        {
            public Arrow Arrow;
            public Vector3 Point;
            public float Damage;
            public ShotGrade Grade;
        }

        readonly List<BoomerangTurn> m_BoomerangTurns = new List<BoomerangTurn>();
        readonly List<BoomerangTurn> m_TurnsToPlay = new List<BoomerangTurn>();
        readonly List<Planted> m_Recallable = new List<Planted>();
        readonly List<Detonator> m_RecallDetonations = new List<Detonator>();
        readonly List<Arrow> m_RecallArrived = new List<Arrow>();
        readonly List<Sentinel> m_Sentinels = new List<Sentinel>();
        readonly List<RailSegment> m_RailSegments = new List<RailSegment>();
        readonly HashSet<Enemy> m_RailHits = new HashSet<Enemy>();
        readonly Dictionary<Enemy, float> m_FenceFrozen = new Dictionary<Enemy, float>();
        readonly Dictionary<Enemy, bool> m_IsDragon = new Dictionary<Enemy, bool>();
        readonly List<Structure> m_Buildings = new List<Structure>();
        float m_NextBuildingSearch;
        float m_NextRailTick;
        float m_ShotHeld;
        float m_NextSiegeSpark;
        bool m_SiegeFull;

        // La corde de siège avec les Akimbow : les arbalètes, et celles déjà à pleine charge.
        Akimbow m_Akimbow;
        float m_NextAkimboSearch;
        readonly bool[] m_AkimboFull = new bool[2];

        // La corde fantôme : le tir qu'elle répète, le temps qui reste pour attraper la corde, la main qui la tient.
        bool m_GhostReady;
        float m_GhostUntil;
        float m_GhostSpeed;
        float m_GhostDamage;
        ShotGrade m_GhostGrade;
        float m_GhostGravity;
        XRBaseInputInteractor m_GhostHand;
        float m_GhostGrabbedAt;
        float m_NextGhostLine;
        AimGuide m_GhostGuide;

        /// <summary>Le talisman d'acrobate ralentit le temps en ce moment (le joueur est en l'air ou glisse).</summary>
        public bool AcrobatActive => m_AcrobatActive;

        // Le duo « Étoile filante » (27 octobre) : la couronne part encore un instant après l'atterrissage ou la fin de la glissade
        // (en VR, on lâche souvent juste après ; le ralenti, lui, finit au bout de 0,25 s).
        const float k_StarfallGrace = 0.75f;

        /// <summary>Le duo « Étoile filante » tire en couronne : en l'air, en glissade, ou depuis moins de 0,75 s.</summary>
        public bool StarfallReady => m_AcrobatActive || Time.unscaledTime < m_AcrobatUntil + k_StarfallGrace;

        /// <summary>
        /// Cette flèche est la flèche dorée de la corde de transe et le duo « Supernova » est formé : son explosion est plus forte
        /// (SpecialArrows.Explode).
        /// </summary>
        public bool IsSupernova(Arrow arrow) =>
            arrow != null && ArrowDuos.IsActive(ArrowDuos.Supernova) && m_Flights.TryGetValue(arrow, out var flight) && flight.Golden;

        void EnableLot3()
        {
            Bow.HeldChargeMultiplier = SiegeCharge;
            ArrowPool.StuckArrowReclaimed += OnStuckArrowReclaimed;
        }

        void DisableLot3()
        {
            ArrowPool.StuckArrowReclaimed -= OnStuckArrowReclaimed;
            if (Bow.HeldChargeMultiplier == (System.Func<float, float>)SiegeCharge)
                Bow.HeldChargeMultiplier = null;
            m_Recallable.Clear();
            m_RecallArrived.Clear();
            m_BoomerangTurns.Clear();
            m_Clouds.Clear();
            m_CloudArrow = null;
            m_RainDrops.Clear();
            m_RainArrow = null;
            m_Sentinels.Clear();
            m_RailSegments.Clear();
            m_FenceFrozen.Clear();
            m_GhostReady = false;
            m_GhostHand = null;
            HideGhostGuide();
        }

        // ---------------------------------------------------------------- au tir

        void OnShotStartLot3(in ShotInfo shot)
        {
            m_ShotHeld = shot.HeldTime;

            // Corde de pluie : l'arc légendaire voit sa pluie multipliée ; sinon, un tir vers le ciel fait la sienne.
            var rain = Level(UpgradeEffect.Rain);
            if (rain > 0 && shot.Arrow != null)
            {
                if (ArrowRain.IsTracked(shot.Arrow))
                {
                    ArrowRain.Multiply(shot.Arrow, 1f + rain);
                }
                else if (shot.Direction.y >= Mathf.Sin(k_RainMinPitch * Mathf.Deg2Rad) && ArrowDuos.IsActive(ArrowDuos.Cloud))
                {
                    // Le duo « Nuage » : la flèche plume (sans sommet) s'arrêtera dans le ciel (TrackLot3).
                    m_CloudArrow = shot.Arrow;
                }
                else
                {
                    // Revu le 27 octobre (demandé par le joueur) : tout tir sème des flèches le long de sa traînée (TrackLot3,
                    // RainTrailStep) ; avant, seul un tir vers le ciel éclatait à son sommet.
                    m_RainArrow = shot.Arrow;
                }
            }

            // Corde fantôme : un tir de l'arc (pas des arbalètes) ouvre la fenêtre pour attraper la corde à vide.
            if (Level(UpgradeEffect.Ghost) > 0 && shot.Arrow != null)
            {
                if (m_Bow == null)
                    m_Bow = FindAnyObjectByType<Bow>();
                if (m_Bow != null && shot.Aim != null && shot.Aim.IsChildOf(m_Bow.transform))
                {
                    m_GhostReady = true;
                    m_GhostUntil = Time.unscaledTime + k_GhostWindow;
                    m_GhostSpeed = shot.Speed;
                    m_GhostDamage = shot.Damage;
                    m_GhostGrade = shot.Grade;
                    m_GhostGravity = shot.Arrow.GravityScale;
                    m_GhostHand = null;
                }
            }
        }

        // La charge de la corde de siège (Bow.HeldChargeMultiplier) : les dégâts du tir selon le temps passé au bout de l'anneau.
        // Le vert clair a des dégâts moindres qu'un tir parfait : à pleine charge, ce sont ceux d'un tir parfait, multipliés.
        float SiegeCharge(float held)
        {
            var level = Level(UpgradeEffect.Siege);
            if (level == 0)
                return 1f;

            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            var tuning = m_Bow != null ? m_Bow.ShotTuning : ShotTuning.Fallback;
            var heldDamage = Mathf.Max(0.01f, tuning.Get(ShotGrade.Held).damage);
            var full = SpecialArrows.ByLevel(k_SiegeMax, level) * tuning.Get(ShotGrade.Perfect).damage / heldDamage;
            return Mathf.Lerp(1f, Mathf.Max(1f, full), Mathf.Clamp01(held / k_SiegeChargeTime));
        }

        /// <summary>La corde de siège est équipée : les failles du katana mûrissent (27 octobre).</summary>
        public bool SiegeEquipped => Level(UpgradeEffect.Siege) > 0;

        /// <summary>
        /// La corde de siège avec le katana (27 octobre, choisi par le joueur : « les failles mûrissent ») : le multiplicateur des
        /// dégâts d'une faille ouverte depuis <paramref name="age"/> s, de ×1 à son ouverture jusqu'à ×2,5 / 3 / 3,5 au bout de
        /// 2 s ; elle ne peut que gagner avec le temps. 1 sans la corde.
        /// </summary>
        public float RiftCharge(float age)
        {
            var level = Level(UpgradeEffect.Siege);
            return level == 0 ? 1f : Mathf.Lerp(1f, SpecialArrows.ByLevel(k_SiegeMax, level), Mathf.Clamp01(age / k_SiegeChargeTime));
        }

        // Une flèche suivie : la charge du siège (des ennemis traversés en plus), le rail.
        void TrackLot3(Arrow arrow, Flight flight)
        {
            if (flight.IsChild)
                return;

            // Le duo « Nuage » : la flèche de l'arc s'arrêtera dans le ciel (pas suspendue par le fût suspendu en plus).
            if (arrow == m_CloudArrow)
            {
                m_CloudArrow = null;
                flight.CloudAt = Time.time + k_CloudDelay;
                flight.SuspendAt = 0f;
            }

            // La corde de pluie : la flèche de l'arc sème des flèches le long de sa traînée.
            if (arrow == m_RainArrow)
            {
                m_RainArrow = null;
                flight.RainTrail = true;
                flight.RainTraveled = 0f;
                flight.RainNextAt = k_RainStart;
                flight.RainDrops = 0;
            }

            var siege = Level(UpgradeEffect.Siege);
            if (siege > 0 && m_ShotHeld > 0f)
                arrow.AddPierce(Mathf.RoundToInt(SpecialArrows.ByLevel(k_SiegePierce, siege) * Mathf.Clamp01(m_ShotHeld / k_SiegeChargeTime)));

            // Le duo « Rail » : un trait de lumière, trois fois plus rapide, qui traverse tout et laisse une traînée qui blesse.
            if (ArrowDuos.IsActive(ArrowDuos.Rail))
            {
                arrow.Velocity *= k_RailSpeed;
                arrow.AddPierce(99);
                arrow.SetTrailColor(k_RailColor);
                flight.Rail = true;
                flight.RailPoint = arrow.TipPosition;
                flight.RailNext = Time.time + k_RailStep;
            }
        }

        // ---------------------------------------------------------------- en vol : piqué, rappel, rail

        bool NeedsFixedLot3() => Level(UpgradeEffect.Dive) > 0 || Level(UpgradeEffect.Recall) > 0 || ArrowDuos.IsActive(ArrowDuos.Rail) ||
                                 ArrowDuos.IsActive(ArrowDuos.Cloud) || Level(UpgradeEffect.Rain) > 0;

        void FixedLot3(Flight flight, float deltaTime)
        {
            var arrow = flight.Arrow;

            // Le nuage : la flèche s'arrête là ; elle pleut depuis Update (UpdateClouds : ses flèches s'ajoutent aux flèches
            // suivies, pas pendant cette boucle).
            if (flight.CloudAt > 0f && (Time.time >= flight.CloudAt || arrow.transform.position.y - flight.Origin.y >= k_CloudHeight))
            {
                flight.CloudAt = 0f;
                var drift = flight.FirstDirection;
                drift.y = 0f;
                arrow.Hover(true);
                m_Clouds.Add(new RainCloud
                {
                    Arrow = arrow,
                    Point = arrow.transform.position,
                    Drift = drift.sqrMagnitude > 1e-4f ? drift.normalized * k_CloudDrift : Vector3.zero,
                    EndAt = Time.time + k_CloudTime,
                    NextDrop = Time.time,
                    Damage = flight.Damage,
                    Grade = flight.Grade,
                });
                return;
            }

            // Rappelée : elle revient vers le joueur, sans tomber, et disparaît en arrivant (après la boucle des vols).
            if (flight.Recalled)
            {
                var toPlayer = ReturnPoint() - arrow.TipPosition;
                var distance = toPlayer.magnitude;
                if (distance < k_RecallArrive)
                {
                    if (!m_RecallArrived.Contains(arrow))
                        m_RecallArrived.Add(arrow);
                    return;
                }

                SteerTo(arrow, toPlayer / distance, k_RecallSpeed, k_RecallTurnRate, deltaTime);
                RecallSweep(flight);
                return;
            }

            var dive = Level(UpgradeEffect.Dive);
            if (dive > 0 && !flight.IsChild && !flight.Returning)
                UpdateDive(flight, dive, deltaTime);

            // Après le piqué : la pluie suit la flèche jusque sur sa cible.
            if (flight.RainTrail)
                RainTrailStep(flight);

            if (flight.Rail && Time.time >= flight.RailNext)
            {
                var tip = arrow.TipPosition;
                if (m_RailSegments.Count >= k_MaxRailSegments)
                    m_RailSegments.RemoveAt(0);
                m_RailSegments.Add(new RailSegment { A = flight.RailPoint, B = tip, EndAt = Time.time + k_RailLife, Damage = flight.Damage, Grade = flight.Grade });
                LightningBolt.Spawn(flight.RailPoint, tip, k_RailColor, EffectMaterial, 0.05f, k_RailLife);
                flight.RailPoint = tip;
                flight.RailNext = Time.time + k_RailStep;
            }
        }

        // Empennage piqué : un tir vers le haut monte un instant, puis la flèche fond sur l'ennemi le plus proche de la visée (à
        // défaut, le plus proche de son point de chute).
        void UpdateDive(Flight flight, int level, float deltaTime)
        {
            var arrow = flight.Arrow;
            if (!flight.Dived)
            {
                if (flight.FirstDirection.y < Mathf.Sin(k_DiveMinPitch * Mathf.Deg2Rad))
                    return;
                if (arrow.Velocity.y > 0f && Time.time - flight.LaunchTime < k_DiveDelay)
                    return;

                flight.Dived = true;
                var target = DiveTargetFor(flight, level);
                if (target == null)
                    return;

                var bonus = SpecialArrows.ByLevel(k_DiveDamage, level);
                arrow.Damage *= bonus;
                flight.Damage *= bonus;
                flight.DiveTarget = target;
                arrow.SetTrailColor(k_DiveColor);
                Sfx.Play(PieceSounds.Whoosh, arrow.transform.position, 0.9f, 0.8f);
            }

            var chosen = flight.DiveTarget;
            if (chosen == null)
                return;
            if (!chosen.IsAlive)
            {
                flight.DiveTarget = null;
                return;
            }

            var toTarget = chosen.Center - arrow.TipPosition;
            if (toTarget.sqrMagnitude > 0.01f)
                SteerTo(arrow, toTarget.normalized, Mathf.Max(k_DiveSpeed, arrow.Velocity.magnitude), k_DiveTurnRate, deltaTime);
        }

        // L'ennemi du piqué : dans la direction du tir (à moins de 30° de la visée, à plat), à portée du tireur, le plus proche de
        // la ligne de visée ; sinon le plus proche du point de chute.
        Enemy DiveTargetFor(Flight flight, int level)
        {
            var forward = flight.FirstDirection;
            forward.y = 0f;
            var range = SpecialArrows.ByLevel(k_DiveRange, level);
            Enemy best = null;
            var bestAngle = k_DiveCone;
            if (forward.sqrMagnitude > 1e-4f)
            {
                foreach (var enemy in Enemy.Alive)
                {
                    if (enemy == null || !enemy.IsAlive)
                        continue;

                    var offset = enemy.Center - flight.Origin;
                    offset.y = 0f;
                    var distance = offset.magnitude;
                    if (distance < 2f || distance > range)
                        continue;

                    var angle = Vector3.Angle(forward, offset);
                    if (angle < bestAngle)
                    {
                        best = enemy;
                        bestAngle = angle;
                    }
                }
            }

            if (best != null)
                return best;

            m_Exclude.Clear();
            return SpecialArrows.NearestEnemy(LandingPoint(flight.Arrow), 15f, m_Exclude);
        }

        // Où retombera la flèche, à peu près : sous son sommet, le sol, et le temps de la chute.
        static Vector3 LandingPoint(Arrow arrow)
        {
            var position = arrow.transform.position;
            var velocity = arrow.Velocity;
            var ground = position.y - 6f;
            if (Physics.Raycast(position, Vector3.down, out var hit, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                ground = hit.point.y;

            var gravity = Mathf.Max(0.1f, -Physics.gravity.y * arrow.GravityScale);
            var height = Mathf.Max(0f, position.y - ground);
            var time = (velocity.y + Mathf.Sqrt(velocity.y * velocity.y + 2f * gravity * height)) / gravity;
            return new Vector3(position.x + velocity.x * time, ground, position.z + velocity.z * time);
        }

        // La flèche tourne vers cette direction à cette vitesse, sans tomber (la gravité du pas suivant est annulée d'avance).
        static void SteerTo(Arrow arrow, Vector3 direction, float speed, float turnRate, float deltaTime)
        {
            var velocity = arrow.Velocity;
            var current = velocity.sqrMagnitude > 0.01f ? velocity.normalized : direction;
            var turned = Vector3.RotateTowards(current, direction, turnRate * Mathf.Deg2Rad * deltaTime, 0f);
            arrow.Velocity = turned * speed - Physics.gravity * (arrow.GravityScale * deltaTime);
        }

        // ---------------------------------------------------------------- plantée : rappel, sentinelle, pluie de fer

        void OnStuckLot3(Arrow arrow, Flight flight, Enemy enemy)
        {
            // Une graine de la pluie de fer devient un pieu.
            if (flight.SpikeSeed)
            {
                if (enemy == null)
                    AddSpike(arrow, flight);
                return;
            }

            if (flight.IsChild)
                return;

            if (Level(UpgradeEffect.Recall) > 0)
            {
                while (m_Recallable.Count >= k_MaxRecall)
                    m_Recallable.RemoveAt(0);
                m_Recallable.Add(new Planted { Arrow = arrow, Point = arrow.TipPosition, Damage = flight.Damage, Grade = flight.Grade, Enemy = enemy });
            }

            var sentinel = Level(UpgradeEffect.Sentinel);
            if (sentinel > 0 && enemy == null)
            {
                var max = Mathf.RoundToInt(SpecialArrows.ByLevel(k_MaxSentinels, sentinel));
                while (m_Sentinels.Count >= max)
                    m_Sentinels.RemoveAt(0);
                m_Sentinels.Add(new Sentinel
                {
                    Arrow = arrow,
                    Point = arrow.TipPosition,
                    EndAt = Time.time + SpecialArrows.ByLevel(k_SentinelTime, sentinel),
                    NextShot = Time.time + 0.3f,
                    Damage = flight.Damage,
                    Grade = flight.Grade,
                });
            }
        }

        // La réserve reprend une flèche plantée (plus de 100 à la fois, la plus ancienne) : ce qu'elle faisait reste en place, comme
        // après un rappel (27 octobre : avec la pluie, les sentinelles disparaissaient avec leur flèche au bout d'un ou deux tirs).
        void OnStuckArrowReclaimed(Arrow arrow) => DetachPlanted(arrow, false);

        // Un pieu du fût-pieu (comme ArrowPieces.Lot2, OnStuckLot2), pour une graine de la pluie de fer.
        void AddSpike(Arrow arrow, Flight flight)
        {
            var spike = Mathf.Max(1, Level(UpgradeEffect.Spike));
            while (m_Spikes.Count >= k_MaxSpikes)
                m_Spikes.RemoveAt(0);
            m_Spikes.Add(new Spike
            {
                Arrow = arrow,
                Point = arrow.TipPosition,
                EndAt = Time.time + k_SpikeTime + k_SpikeTimePerLevel * (spike - 1),
                Damage = flight.Damage,
                Grade = flight.Grade,
            });
        }

        // ---------------------------------------------------------------- chaque image

        void UpdateLot3()
        {
            // Les flèches rappelées arrivées disparaissent (hors de la boucle des vols, qui les suit).
            foreach (var arrow in m_RecallArrived)
            {
                if (arrow != null && arrow.CurrentState == Arrow.State.Flying)
                    arrow.Despawn();
            }

            m_RecallArrived.Clear();

            FlushBoomerangTurns();
            FlushRainDrops();
            UpdateClouds();
            UpdateSiegeCharge();
            UpdateGhost();
            UpdateRecallInput();
            UpdateSentinels();
            UpdateRail();
        }

        // Corde de siège : pendant la charge, des étincelles dorées de plus en plus grosses à la pointe, une vibration qui monte,
        // et un tintement à pleine charge.
        void UpdateSiegeCharge()
        {
            var siege = Level(UpgradeEffect.Siege);
            if (siege == 0)
                return;
            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            if (m_Bow != null && m_Bow.IsReplaced)
            {
                UpdateAkimboSiege();
                return;
            }

            var arrow = m_Bow != null ? m_Bow.NockedArrow : null;
            var held = m_Bow != null ? m_Bow.HeldTime : 0f;
            if (arrow == null || held <= 0f)
            {
                m_SiegeFull = false;
                return;
            }

            var charge = Mathf.Clamp01(held / k_SiegeChargeTime);
            if (Time.time >= m_NextSiegeSpark)
            {
                m_NextSiegeSpark = Time.time + k_SiegeSparkInterval;
                LightningBolt.Burst(arrow.TipPosition, 0.06f + 0.22f * charge, Color.Lerp(k_SiegeColor, Color.white, charge), EffectMaterial,
                                    2 + Mathf.RoundToInt(6f * charge));
                if (arrow.Hand != null)
                    Haptics.Pulse(arrow.Hand, 0.08f + 0.35f * charge, 0.03f);
            }

            if (charge >= 1f && !m_SiegeFull)
            {
                m_SiegeFull = true;
                Sfx.Play(m_Bow.PerfectClip, arrow.TipPosition, 0.6f, 1.5f);
                FloatingText.Spawn(arrow.TipPosition + Vector3.up * 0.15f, "Chargée !", k_SiegeColor, 0.45f, 0.7f);
            }
        }

        // La corde de siège avec les Akimbow (27 octobre, demandé par le joueur) : chaque arbalète qui attend au bout de son anneau se
        // charge (Akimbow.TryFire) ; des étincelles grossissent à son bout, « Chargée ! » à pleine charge.
        void UpdateAkimboSiege()
        {
            if (m_Akimbow == null && Time.unscaledTime >= m_NextAkimboSearch)
            {
                m_NextAkimboSearch = Time.unscaledTime + 1f;
                m_Akimbow = FindAnyObjectByType<Akimbow>();
            }

            if (m_Akimbow == null)
                return;

            var spark = Time.time >= m_NextSiegeSpark;
            if (spark)
                m_NextSiegeSpark = Time.time + k_SiegeSparkInterval;
            for (var i = 0; i < m_Akimbow.CrossbowCount && i < m_AkimboFull.Length; i++)
            {
                if (!m_Akimbow.TryGetHeld(i, out var tip, out var held))
                {
                    m_AkimboFull[i] = false;
                    continue;
                }

                var charge = Mathf.Clamp01(held / k_SiegeChargeTime);
                if (spark)
                    LightningBolt.Burst(tip, 0.06f + 0.22f * charge, Color.Lerp(k_SiegeColor, Color.white, charge), EffectMaterial,
                                        2 + Mathf.RoundToInt(6f * charge));
                if (charge >= 1f && !m_AkimboFull[i])
                {
                    m_AkimboFull[i] = true;
                    Sfx.Play(m_Bow.PerfectClip, tip, 0.5f, 1.5f);
                    FloatingText.Spawn(tip + Vector3.up * 0.15f, "Chargée !", k_SiegeColor, 0.4f, 0.7f);
                }
            }
        }

        // Corde fantôme : la main libre attrape la corde à vide près de l'encoche, la tire, la lâche.
        void UpdateGhost()
        {
            if (!m_GhostReady || Level(UpgradeEffect.Ghost) == 0 || m_Bow == null)
            {
                m_GhostReady = false;
                HideGhostGuide();
                return;
            }

            var desktop = DesktopControls.Instance;
            var mouse = Mouse.current;
            if (desktop != null && desktop.IsActive)
            {
                if (Time.unscaledTime > m_GhostUntil)
                    m_GhostReady = false;
                else if (mouse != null && mouse.middleButton.wasPressedThisFrame)
                    FireGhost(1f);
                if (m_GhostReady)
                    ShowGhostGuide(1f, 1f);
                else
                    HideGhostGuide();
                return;
            }

            var stringPoint = m_Bow.StringPoint;
            if (m_GhostHand == null)
            {
                HideGhostGuide();
                if (Time.unscaledTime > m_GhostUntil)
                {
                    m_GhostReady = false;
                    return;
                }

                var rig = PlayerRig.Instance;
                if (rig == null)
                    return;
                foreach (var hand in rig.Hands)
                {
                    if (hand == null || hand.selectInput == null || hand.hasSelection || ReferenceEquals(hand, m_ShotHand))
                        continue;
                    if (hand.selectInput.ReadWasPerformedThisFrame() && Vector3.Distance(hand.transform.position, stringPoint) < k_GhostGrab)
                    {
                        m_GhostHand = hand;
                        m_GhostGrabbedAt = Time.unscaledTime;
                        Haptics.Pulse(hand, 0.3f, 0.04f);
                        break;
                    }
                }

                return;
            }

            // La corde fantôme : un fil pâle de l'encoche à la main.
            var handPosition = m_GhostHand.transform.position;
            var pull = Vector3.Dot(stringPoint - handPosition, m_Bow.AimForward);
            if (Time.unscaledTime >= m_NextGhostLine)
            {
                m_NextGhostLine = Time.unscaledTime + 0.05f;
                LightningBolt.Spawn(stringPoint, handPosition, k_GhostColor, EffectMaterial, 0.012f, 0.07f);
                Haptics.Pulse(m_GhostHand, 0.05f + 0.25f * Mathf.Clamp01(pull / k_GhostPull), 0.02f);
            }

            if (m_GhostHand.selectInput.ReadIsPerformed() && Time.unscaledTime - m_GhostGrabbedAt < k_GhostMaxHold)
            {
                ShowGhostGuide(Mathf.Clamp(pull / k_GhostPull, 1f, 1.5f), Mathf.Clamp01(pull / k_GhostPull));
                return;
            }

            var released = m_GhostHand.selectInput.ReadIsPerformed() == false;
            m_GhostHand = null;
            m_GhostReady = false;
            HideGhostGuide();
            if (released && pull >= k_GhostPull)
                FireGhost(Mathf.Clamp(pull / k_GhostPull, 1f, 1.5f));
        }

        // L'aide à la visée du tir fantôme (27 octobre ; le joueur : « la corde fantôme n'a pas d'aide à la visée ») : la
        // trajectoire qu'aurait la flèche fantôme lâchée maintenant, de plus en plus nette à mesure qu'on tire la corde (sans
        // casque, pendant toute la fenêtre du clic du milieu).
        void ShowGhostGuide(float power, float strength)
        {
            if (m_Bow == null)
                return;
            if (m_GhostGuide == null)
            {
                m_GhostGuide = new GameObject("Aide à la visée fantôme").AddComponent<AimGuide>();
                m_GhostGuide.transform.SetParent(transform, false);
            }

            var direction = m_Bow.AimForward;
            m_GhostGuide.Show(m_Bow.StringPoint + direction * 0.45f, direction, m_GhostSpeed * Mathf.Min(1.2f, power), k_GhostColor,
                              0.35f + 0.65f * strength, 0f, m_GhostGravity);
        }

        void HideGhostGuide()
        {
            if (m_GhostGuide != null)
                m_GhostGuide.Hide();
        }

        // La flèche fantôme part de l'arc, là où il vise, avec les pièces (une flèche en plus : elle ne casse pas le combo).
        void FireGhost(float power)
        {
            var arrows = SpecialArrows.Instance;
            if (arrows == null || m_Bow == null)
                return;

            m_GhostReady = false;
            var level = Mathf.Max(1, Level(UpgradeEffect.Ghost));
            var direction = m_Bow.AimForward;
            var origin = m_Bow.StringPoint + direction * 0.45f;
            var arrow = arrows.LaunchExtraShot(origin, direction, m_GhostSpeed * Mathf.Min(1.2f, power),
                                               m_GhostDamage * SpecialArrows.ByLevel(k_GhostDamage, level), m_GhostGrade, k_GhostColor, m_GhostGravity);
            if (arrow == null)
                return;

            Sfx.Play(PieceSounds.Whoosh, origin, 0.8f, 1.35f);
            FloatingText.Spawn(origin + Vector3.up * 0.2f, "Fantôme !", k_GhostColor, 0.45f, 0.7f);
        }

        // Empennage de rappel : la main libre se ferme devant soi (pas au carquois, ni à la hanche, ni sur la corde) ; sans
        // casque, Ctrl gauche.
        void UpdateRecallInput()
        {
            if (Level(UpgradeEffect.Recall) == 0)
            {
                m_Recallable.Clear();
                return;
            }

            if (m_Recallable.Count == 0)
                return;

            var keyboard = Keyboard.current;
            var desktop = DesktopControls.Instance;
            if (desktop != null && desktop.IsActive)
            {
                if (keyboard != null && keyboard.leftCtrlKey.wasPressedThisFrame && !DesktopHand.IsTypingText)
                    Recall();
                return;
            }

            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            foreach (var hand in rig.Hands)
            {
                // Une main qui attrape quelque chose (le marteau, un objet survolé) ne rappelle rien.
                if (hand == null || hand.selectInput == null || hand.hasSelection || hand.hasHover || ReferenceEquals(hand, m_ShotHand) ||
                    !hand.selectInput.ReadWasPerformedThisFrame())
                    continue;

                var position = hand.transform.position;
                var local = head.InverseTransformPoint(position);
                if (local.z < 0.1f || local.y < -0.45f)
                    continue;
                if (m_Bow != null && Vector3.Distance(position, m_Bow.StringPoint) < k_GhostGrab + 0.08f)
                    continue;

                Haptics.Pulse(hand, 0.5f, 0.08f);
                Recall();
                return;
            }
        }

        // Toutes les flèches plantées reviennent vers le joueur : elles traversent les ennemis, et seulement eux.
        void Recall()
        {
            var arrows = SpecialArrows.Instance;
            if (arrows == null)
                return;

            var level = Mathf.Max(1, Level(UpgradeEffect.Recall));
            var target = ReturnPoint();
            var count = 0;
            var explosive = ArrowDuos.IsActive(ArrowDuos.ExplosiveRecall);
            m_RecallDetonations.Clear();
            m_TrackingChild = true;
            foreach (var planted in m_Recallable)
            {
                if (!StillPlanted(planted))
                    continue;

                // Ce que la flèche faisait plantée reste là où elle était ; avec le retour explosif, mèche et détonateur partent.
                DetachPlanted(planted.Arrow, explosive);

                var stuck = planted.Arrow;
                var from = stuck.transform.position + Vector3.up * 0.2f;
                var direction = (target - from).normalized;
                var damage = planted.Damage * SpecialArrows.ByLevel(k_RecallDamage, level);

                // Plantée dans un ennemi : elle le blesse en s'arrachant.
                var yanked = planted.Enemy != null && planted.Enemy.IsAlive ? planted.Enemy : null;
                if (yanked != null)
                    RecallHurt(yanked, damage, planted.Grade, direction);

                stuck.Despawn();
                var child = arrows.LaunchChild(from, direction, k_RecallSpeed, damage, planted.Grade, k_RecallColor, 0.001f);
                if (child == null)
                    continue;

                child.Intangible = true;
                if (m_Flights.TryGetValue(child, out var flight))
                {
                    flight.Recalled = true;
                    if (yanked != null)
                        flight.RecallHits.Add(yanked);
                }

                count++;
            }

            m_TrackingChild = false;
            m_Recallable.Clear();
            if (count > 0)
                Sfx.Play(PieceSounds.Whoosh, target, 0.9f, 0.7f, 0.5f);

            // Après la boucle : une détonation lance ses propres flèches (et remet m_TrackingChild à faux).
            foreach (var detonator in m_RecallDetonations)
            {
                if (m_Detonators.Contains(detonator))
                    Detonate(detonator);
            }

            m_RecallDetonations.Clear();
        }

        // Le rappel emporte la flèche, pas ce qu'elle faisait (27 octobre, demandé par le joueur) : la sentinelle tire encore, le pieu
        // blesse encore, le détonateur attend encore (k_DetachedDetonatorTime s) là où elle était ; la mèche, l'aimant et les arcs du
        // conducteur restaient déjà. Le duo « Retour explosif » (explosive) : la mèche explose tout de suite, le détonateur éclate.
        void DetachPlanted(Arrow arrow, bool explosive)
        {
            if (arrow == null)
                return;

            var now = Time.time;
            foreach (var fuse in m_Fuses)
            {
                if (fuse.Arrow != arrow)
                    continue;
                fuse.Arrow = null;
                if (explosive)
                    fuse.ExplodeAt = now;
            }

            foreach (var magnet in m_Magnets)
            {
                if (magnet.Arrow == arrow)
                    magnet.Arrow = null;
            }

            foreach (var spike in m_Spikes)
            {
                if (spike.Arrow == arrow)
                    spike.Arrow = null;
            }

            foreach (var sentinel in m_Sentinels)
            {
                if (sentinel.Arrow == arrow)
                    sentinel.Arrow = null;
            }

            foreach (var detonator in m_Detonators)
            {
                if (detonator.Arrow != arrow)
                    continue;
                detonator.Arrow = null;
                detonator.DetachedUntil = now + k_DetachedDetonatorTime;
                if (explosive)
                    m_RecallDetonations.Add(detonator);
            }
        }

        // Toujours plantée là : dans le décor, au même endroit ; dans un ennemi, toujours sur lui (elle bouge avec lui). Une flèche
        // rendue au pool puis tirée de nouveau ne compte plus.
        static bool StillPlanted(Planted planted)
        {
            var arrow = planted.Arrow;
            if (planted.Enemy == null)
                return StillAt(arrow, planted.Point);

            return arrow != null && arrow.CurrentState == Arrow.State.Stuck && planted.Enemy != null &&
                   (arrow.TipPosition - planted.Enemy.Center).sqrMagnitude < 9f;
        }

        // La flèche rappelée blesse les ennemis qu'elle frôle (entre sa pointe d'avant et celle de maintenant), une fois chacun.
        void RecallSweep(Flight flight)
        {
            var tip = flight.Arrow.TipPosition;
            var travel = tip - flight.PreviousTip;
            var direction = travel.sqrMagnitude > 1e-6f ? travel.normalized : flight.Arrow.transform.forward;
            m_Enemies.Clear();
            m_Enemies.AddRange(Enemy.Alive);
            foreach (var enemy in m_Enemies)
            {
                if (enemy == null || !enemy.IsAlive || flight.RecallHits.Contains(enemy) ||
                    DistanceToSegment(enemy.Center, flight.PreviousTip, tip) > k_RecallRadius)
                    continue;

                flight.RecallHits.Add(enemy);
                RecallHurt(enemy, flight.Damage, flight.Grade, direction);
            }
        }

        void RecallHurt(Enemy enemy, float damage, ShotGrade grade, Vector3 direction)
        {
            var center = enemy.Center;
            Hurt(enemy, damage, center, direction, new ArrowHit { Point = center, Direction = direction, Grade = grade, IsShot = true, Damage = damage });
            LightningBolt.Burst(center, 0.35f, k_RecallHitColor, EffectMaterial, 6);
            Sfx.Play(PieceSounds.Whoosh, center, 0.5f, 1.4f);
        }

        // Fût-sentinelle : chaque sentinelle tire une petite flèche sur l'ennemi le plus proche, de temps en temps.
        void UpdateSentinels()
        {
            if (m_Sentinels.Count == 0)
                return;

            var arrows = SpecialArrows.Instance;
            var level = Mathf.Max(1, Level(UpgradeEffect.Sentinel));
            var now = Time.time;
            for (var i = m_Sentinels.Count - 1; i >= 0; i--)
            {
                // Une sentinelle dont la flèche a été rappelée (Arrow vide) tire encore de là où elle était (27 octobre).
                var sentinel = m_Sentinels[i];
                if (now >= sentinel.EndAt || (sentinel.Arrow != null && !StillAt(sentinel.Arrow, sentinel.Point)) || arrows == null)
                {
                    m_Sentinels.RemoveAt(i);
                    continue;
                }

                if (now < sentinel.NextShot)
                    continue;

                sentinel.NextShot = now + k_SentinelInterval;
                m_Exclude.Clear();
                var muzzle = sentinel.Arrow != null ? sentinel.Arrow.transform.position + Vector3.up * 0.15f : sentinel.Point + Vector3.up * 0.4f;
                SentinelShot(muzzle, sentinel.Damage * SpecialArrows.ByLevel(k_SentinelDamage, level), sentinel.Grade);
            }
        }

        // Une petite flèche de sentinelle, de ce point, sur l'ennemi le plus proche (hors m_Exclude) ; rend l'ennemi visé (aussi le
        // duo « Patrouille »).
        Enemy SentinelShot(Vector3 muzzle, float damage, ShotGrade grade)
        {
            var arrows = SpecialArrows.Instance;
            var enemy = arrows != null ? SpecialArrows.NearestEnemy(muzzle, k_SentinelRange, m_Exclude) : null;
            if (enemy == null)
                return null;

            // On vise un peu au-dessus, pour la chute de la petite flèche.
            var aim = enemy.Center;
            var time = Vector3.Distance(muzzle, aim) / k_SentinelSpeed;
            aim += Vector3.up * (0.5f * -Physics.gravity.y * k_SentinelGravity * time * time);
            m_TrackingChild = true;
            arrows.LaunchChild(muzzle, (aim - muzzle).normalized, k_SentinelSpeed, damage, grade, k_SentinelColor, k_SentinelGravity);
            m_TrackingChild = false;
            LightningBolt.Burst(muzzle, 0.15f, k_SentinelColor, EffectMaterial, 3);
            return enemy;
        }

        // ---------------------------------------------------------------- duos

        // Le duo « Orage magnétique » : à la fin d'une aspiration, un éclair frappe chaque ennemi dans son rayon.
        void MagnetStorm(Vector3 point, float radius, float damage, ShotGrade grade)
        {
            if (damage <= 0f || !ArrowDuos.IsActive(ArrowDuos.MagneticStorm))
                return;

            var hit = new ArrowHit { Point = point, Grade = grade, IsShot = true };
            var struck = 0;
            m_Enemies.Clear();
            m_Enemies.AddRange(Enemy.Alive);
            foreach (var enemy in m_Enemies)
            {
                if (struck >= k_MagnetStormTargets)
                    break;
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var offset = enemy.transform.position - point;
                offset.y = 0f;
                if (offset.sqrMagnitude > radius * radius)
                    continue;

                CallLightning(enemy, damage * k_MagnetStormDamage, hit);
                struck++;
            }
        }

        // Chaque rebond de l'empennage rebond (27 octobre, demandé par le joueur : le rebond fait un duo avec les pièces qui agissent
        // plantées, pas seulement la mèche). Un seul duo de fût à la fois (les cinq partenaires sont des fûts), plus le lance-grenades
        // (la pointe). Les flèches nées d'une pièce ne rebondissent pas : pas de cascade.
        void OnBounce(in ArrowHit hit, Flight flight)
        {
            if (flight.IsChild)
                return;

            GrenadeBounce(hit, flight);
            if (ArrowDuos.IsActive(ArrowDuos.Patrol))
                PatrolBounce(hit, flight);
            if (ArrowDuos.IsActive(ArrowDuos.Caltrops) && hit.Normal.y > 0.5f)
                SowSpikes(hit, flight, k_CaltropSeeds, k_CaltropDamage);
            if (ArrowDuos.IsActive(ArrowDuos.HighVoltage))
                AddNodeAt(hit.Point + hit.Normal * 0.3f + Vector3.up * k_LinkHeight, flight.Damage, flight.Grade, Mathf.Max(1, Level(UpgradeEffect.Conductor)));
            if (ArrowDuos.IsActive(ArrowDuos.Pinball))
                PinballBounce(hit, flight);
            if (ArrowDuos.IsActive(ArrowDuos.Fragmentation))
                FragmentationBounce(hit, flight);
            if (ArrowDuos.IsActive(ArrowDuos.IceRink) && SpecialArrows.Instance != null)
                SpecialArrows.Instance.Impact(SpecialArrows.Effect.Frost, hit, null);
        }

        // Le duo « Patrouille » : à chaque rebond, la flèche tire une petite flèche de sentinelle sur l'ennemi le plus proche.
        void PatrolBounce(in ArrowHit hit, Flight flight)
        {
            var level = Mathf.Max(1, Level(UpgradeEffect.Sentinel));
            var shots = Mathf.RoundToInt(SpecialArrows.ByLevel(k_PatrolShots, level));
            var muzzle = hit.Point + hit.Normal * 0.3f + Vector3.up * 0.15f;
            m_Exclude.Clear();
            for (var i = 0; i < shots; i++)
            {
                var target = SentinelShot(muzzle, flight.Damage * SpecialArrows.ByLevel(k_SentinelDamage, level), flight.Grade);
                if (target == null)
                    break;
                m_Exclude.Add(target);
            }
        }

        // Le duo « Flipper » : chaque rebond attire un instant les ennemis vers lui (un aimant sans flèche, plus petit, qui tire fort).
        void PinballBounce(in ArrowHit hit, Flight flight)
        {
            var level = Mathf.Max(1, Level(UpgradeEffect.Magnet));
            AddMagnetAt(hit.Point, null, k_PinballTime, (k_MagnetRadius + k_MagnetRadiusPerLevel * (level - 1)) * k_PinballRadius, k_PinballPull,
                        flight.Damage, flight.Grade);
        }

        // Le duo « Fragmentation » : à chaque rebond, quelques flèches du détonateur partent sur les ennemis proches.
        void FragmentationBounce(in ArrowHit hit, Flight flight)
        {
            var level = Mathf.Max(1, Level(UpgradeEffect.Detonator));
            var damage = flight.Damage * (k_DetonatorDamage + k_DetonatorDamagePerLevel * (level - 1)) * k_FragmentDamage;
            DetonationVolley(hit.Point + hit.Normal * 0.3f, damage, flight.Grade, Mathf.RoundToInt(SpecialArrows.ByLevel(k_FragmentArrows, level)));
        }

        // Le duo « Lance-grenades » : la flèche explose à chaque rebond, au rayon de la mèche.
        void GrenadeBounce(in ArrowHit hit, Flight flight)
        {
            if (flight.IsChild || !ArrowDuos.IsActive(ArrowDuos.GrenadeLauncher))
                return;

            var arrows = SpecialArrows.Instance;
            if (arrows == null)
                return;

            FuseBlast(hit.Point, flight.Damage, k_GrenadeBounceDamage, hit);
        }

        // Une explosion de la mèche, au rayon de la mèche, à cette part de ses dégâts (le lance-grenades, le retour de flamme).
        void FuseBlast(Vector3 point, float shotDamage, float share, in ArrowHit hit)
        {
            var arrows = SpecialArrows.Instance;
            if (arrows == null)
                return;

            var level = Mathf.Max(1, Level(UpgradeEffect.Fuse));
            var radius = k_FuseRadius + k_FuseRadiusPerLevel * (level - 1);
            var damage = shotDamage * (k_FuseDamage + k_FuseDamagePerLevel * (level - 1)) * share;
            arrows.Blast(point, damage, radius, k_FuseColor, hit);
        }

        // Les duos du boomerang (27 octobre, demandé par le joueur : comme ceux du rebond) : au demi-tour (au bout de sa course, en
        // l'air, ou contre le décor qui la renvoie), la flèche boomerang laisse l'effet de la pièce qui agit plantée, puisqu'elle ne
        // se plante presque jamais. Avec la faux (deux allers-retours), à chaque demi-tour. Un seul duo de fût à la fois (les cinq
        // partenaires sont des fûts), plus le retour de flamme (la pointe).
        // Le demi-tour se décide souvent dans la boucle des vols (FixedUpdate) : les flèches lancées par ces duos s'ajouteraient aux
        // flèches suivies pendant qu'on les parcourt. Les effets attendent donc la fin de la boucle (FlushBoomerangTurns).
        void OnBoomerangTurn(Flight flight)
        {
            if (flight.IsChild || flight.Arrow == null)
                return;
            if (!ArrowDuos.IsActive(ArrowDuos.Backfire) && !ArrowDuos.IsActive(ArrowDuos.Whirlwind) && !ArrowDuos.IsActive(ArrowDuos.Escort) &&
                !ArrowDuos.IsActive(ArrowDuos.LiveWire) && !ArrowDuos.IsActive(ArrowDuos.Sower) && !ArrowDuos.IsActive(ArrowDuos.Shrapnel))
                return;

            m_BoomerangTurns.Add(new BoomerangTurn
            {
                Arrow = flight.Arrow,
                Point = flight.Arrow.TipPosition,
                Damage = flight.Damage,
                Grade = flight.Grade,
            });
        }

        // Les demi-tours en attente : après la boucle des vols, et à chaque image (ceux du décor, pendant un impact).
        void FlushBoomerangTurns()
        {
            if (m_BoomerangTurns.Count == 0)
                return;

            m_TurnsToPlay.Clear();
            m_TurnsToPlay.AddRange(m_BoomerangTurns);
            m_BoomerangTurns.Clear();
            foreach (var turn in m_TurnsToPlay)
            {
                var hit = new ArrowHit { Arrow = turn.Arrow, Point = turn.Point, Normal = Vector3.up, Grade = turn.Grade, IsShot = true, Damage = turn.Damage };
                if (ArrowDuos.IsActive(ArrowDuos.Backfire))
                    FuseBlast(turn.Point, turn.Damage, k_BackfireDamage, hit);
                if (ArrowDuos.IsActive(ArrowDuos.Whirlwind))
                    WhirlwindTurn(turn);
                if (ArrowDuos.IsActive(ArrowDuos.Escort))
                    EscortTurn(turn);
                if (ArrowDuos.IsActive(ArrowDuos.LiveWire))
                    LiveWireTurn(turn);
                if (ArrowDuos.IsActive(ArrowDuos.Sower))
                    SowerTurn(turn);
                if (ArrowDuos.IsActive(ArrowDuos.Shrapnel))
                    ShrapnelTurn(turn);
            }

            m_TurnsToPlay.Clear();
        }

        // Le duo « Tourbillon » : un aimant au sol sous le demi-tour, qui tire les ennemis vers lui un instant.
        void WhirlwindTurn(BoomerangTurn turn)
        {
            var level = Mathf.Max(1, Level(UpgradeEffect.Magnet));
            AddMagnetAt(GroundBelow(turn.Point), null, k_WhirlwindTime, k_MagnetRadius + k_MagnetRadiusPerLevel * (level - 1), k_WhirlwindPull,
                        turn.Damage, turn.Grade);
        }

        // Le duo « Escorte » : des petites flèches de sentinelle partent du demi-tour, chacune sur un ennemi différent.
        void EscortTurn(BoomerangTurn turn)
        {
            var level = Mathf.Max(1, Level(UpgradeEffect.Sentinel));
            var shots = Mathf.RoundToInt(SpecialArrows.ByLevel(k_EscortShots, level));
            m_Exclude.Clear();
            for (var i = 0; i < shots; i++)
            {
                var target = SentinelShot(turn.Point, turn.Damage * SpecialArrows.ByLevel(k_SentinelDamage, level), turn.Grade);
                if (target == null)
                    break;
                m_Exclude.Add(target);
            }
        }

        // Le duo « Fil électrique » : un arc électrique relie le demi-tour au joueur, le temps d'un lien du conducteur : il blesse
        // tout ce qui traverse la ligne du tir. Il part à 1 m du joueur (27 octobre, réglage du joueur : avant, du point de départ
        // de la flèche, l'arc passait sous ses yeux). Le point du demi-tour reste un nœud, pour les flèches conductrices plantées
        // autour.
        void LiveWireTurn(BoomerangTurn turn)
        {
            var node = AddNodeAt(turn.Point, turn.Damage, turn.Grade, Mathf.Max(1, Level(UpgradeEffect.Conductor)));
            var player = ReturnPoint();
            var toTurn = turn.Point - player;
            var gap = Mathf.Min(k_LiveWireGap, toTurn.magnitude * 0.5f);
            var start = player + (toTurn.sqrMagnitude > 1e-4f ? toTurn.normalized : Vector3.forward) * gap;
            AddLink(node, new Node { Point = start, EndAt = node.EndAt, Damage = node.Damage, Grade = node.Grade });
        }

        // Le duo « Semeur » : du demi-tour, des graines tombent en cloche autour du point du sol en dessous, et deviennent des pieux.
        void SowerTurn(BoomerangTurn turn)
        {
            var ground = GroundBelow(turn.Point);
            var height = Mathf.Max(0f, turn.Point.y - ground.y);
            var time = Mathf.Clamp(Mathf.Sqrt(2f * height / Mathf.Max(0.1f, -Physics.gravity.y)) + 0.2f, 0.5f, 2f);
            SowSpikesAt(turn.Point, ground, time, turn.Damage * k_SowerDamage, turn.Grade, k_SowerSeeds);
        }

        // Le duo « Mitraille » : au demi-tour, une volée du détonateur part sur les ennemis proches.
        void ShrapnelTurn(BoomerangTurn turn)
        {
            var level = Mathf.Max(1, Level(UpgradeEffect.Detonator));
            var damage = turn.Damage * (k_DetonatorDamage + k_DetonatorDamagePerLevel * (level - 1)) * k_ShrapnelDamage;
            DetonationVolley(turn.Point, damage, turn.Grade, Mathf.RoundToInt(SpecialArrows.ByLevel(k_ShrapnelArrows, level)));
        }

        // La corde de pluie : tous les tant de m parcourus, une flèche part d'un peu sous la flèche et tombe. Elles attendent la fin
        // de la boucle des vols (FlushRainDrops) : elles s'ajoutent aux flèches suivies.
        void RainTrailStep(Flight flight)
        {
            var arrow = flight.Arrow;
            var tip = arrow.TipPosition;
            flight.RainTraveled += Vector3.Distance(flight.PreviousTip, tip);
            var level = Mathf.Max(1, Level(UpgradeEffect.Rain));
            var step = Mathf.Max(0.5f, SpecialArrows.ByLevel(k_RainStep, level));
            var max = Mathf.RoundToInt(SpecialArrows.ByLevel(k_RainMaxDrops, level));
            var carry = arrow.Velocity * k_RainCarry;
            carry.y = 0f;
            while (flight.RainTraveled >= flight.RainNextAt && flight.RainDrops < max)
            {
                flight.RainNextAt += step;
                flight.RainDrops++;
                var offset = Random.insideUnitCircle * 0.4f;
                m_RainDrops.Add(new RainDrop
                {
                    From = tip + new Vector3(offset.x, -0.3f, offset.y),
                    Velocity = carry + Vector3.down * k_RainDropSpeed,
                    Damage = flight.Damage,
                    Grade = flight.Grade,
                });
            }

            if (flight.RainDrops >= max)
                flight.RainTrail = false;
        }

        void FlushRainDrops()
        {
            if (m_RainDrops.Count == 0)
                return;

            m_DropsToPlay.Clear();
            m_DropsToPlay.AddRange(m_RainDrops);
            m_RainDrops.Clear();
            var rain = RainOf(1, 0f);
            foreach (var drop in m_DropsToPlay)
                ArrowRain.Drop(drop.From, drop.Velocity, rain, drop.Damage, drop.Grade);
            m_DropsToPlay.Clear();
        }

        // Le duo « Nuage » : la flèche arrêtée dans le ciel pleut par petites averses, puis se dissipe.
        void UpdateClouds()
        {
            if (m_Clouds.Count == 0)
                return;

            var now = Time.time;
            var level = Mathf.Max(1, Level(UpgradeEffect.Rain));
            var drops = Mathf.Max(1, Mathf.RoundToInt(k_CloudTime / k_CloudInterval));
            var perDrop = Mathf.Max(1, Mathf.CeilToInt(SpecialArrows.ByLevel(k_RainArrows, level) * k_CloudShare / drops));
            for (var i = m_Clouds.Count - 1; i >= 0; i--)
            {
                var cloud = m_Clouds[i];
                var arrow = cloud.Arrow;
                if (arrow == null || arrow.CurrentState != Arrow.State.Flying || !arrow.IsHovering)
                {
                    m_Clouds.RemoveAt(i);
                    continue;
                }

                if (now >= cloud.EndAt)
                {
                    m_Clouds.RemoveAt(i);
                    LightningBolt.Burst(cloud.Point, 1f, k_RainColor, EffectMaterial, 10);
                    arrow.Despawn();
                    continue;
                }

                cloud.Point += cloud.Drift * Time.deltaTime;
                arrow.HoverAt(cloud.Point, arrow.transform.rotation);

                if (now < cloud.NextDrop)
                    continue;

                if (cloud.NextDrop <= cloud.EndAt - k_CloudTime + 0.01f)
                    Sfx.Play(PieceSounds.Whoosh, cloud.Point, 0.9f, 0.8f);
                cloud.NextDrop = now + k_CloudInterval;
                ArrowRain.RainFromCloud(cloud.Point, RainOf(perDrop, k_RainRadius + k_RainRadiusPerLevel * (level - 1)), cloud.Damage, cloud.Grade);
                LightningBolt.Burst(cloud.Point, 0.9f, k_RainColor, EffectMaterial, 6);
            }
        }

        /// <summary>
        /// Une flèche de la pluie vient d'être préparée (SpecialArrows) : elle porte les pièces, mais ne se suspend pas (27 octobre :
        /// avec le duo « Averse », chaque gâchette relancerait de plus en plus de pluies) ; avec le boomerang, elle ne s'arrête pas
        /// devant le joueur et ne se reprend pas.
        /// </summary>
        public void MarkRainArrow(Arrow arrow)
        {
            if (arrow == null || !m_Flights.TryGetValue(arrow, out var flight))
                return;

            flight.SuspendAt = 0f;
            flight.IsRain = true;
        }

        // Le duo « Averse » : relâchée par la gâchette, chaque flèche suspendue fait aussi pleuvoir sur le point visé (un peu à
        // côté pour chacune). Appelé par UpdateHovering (Lot2), hors de la boucle des vols.
        void DownpourRelease(Vector3 aimed, List<Flight> released)
        {
            if (!ArrowDuos.IsActive(ArrowDuos.Downpour))
                return;

            var level = Mathf.Max(1, Level(UpgradeEffect.Rain));
            var rain = RainOf(Mathf.RoundToInt(SpecialArrows.ByLevel(k_DownpourArrows, level)), k_DownpourRadius);
            for (var i = 0; i < released.Count; i++)
            {
                var offset = i == 0 ? Vector2.zero : Random.insideUnitCircle * k_DownpourSpread;
                ArrowRain.RainAt(aimed + new Vector3(offset.x, 0f, offset.y), rain, released[i].Damage, released[i].Grade);
            }
        }

        static ArrowRain.RainSettings RainOf(int arrows, float radius) => new ArrowRain.RainSettings
        {
            Arrows = arrows,
            Radius = radius,
            Damage = k_RainDamage,
            Color = k_RainColor,
            SplitClip = PieceSounds.Whoosh,
        };

        // Le sol sous ce point (à 30 m au plus) ; rien dessous : 2 m plus bas.
        static Vector3 GroundBelow(Vector3 point)
        {
            if (Physics.Raycast(point + Vector3.up * 0.1f, Vector3.down, out var ground, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return ground.point;
            return point - Vector3.up * 2f;
        }

        // Le duo « Pluie de fer » : l'onde de choc du plomb lance 3 flèches en cloche autour d'elle, qui deviennent des pieux.
        void IronRain(in ArrowHit hit, Flight flight)
        {
            if (flight.IsChild || !ArrowDuos.IsActive(ArrowDuos.IronRain))
                return;

            SowSpikes(hit, flight, k_IronRainSpikes, 0.6f);
        }

        // Des graines lancées en cloche autour du point, qui deviennent des pieux en se plantant (la pluie de fer, les
        // chausse-trappes) ; damageShare : leur part des dégâts du tir.
        void SowSpikes(in ArrowHit hit, Flight flight, int count, float damageShare) =>
            SowSpikesAt(hit.Point + hit.Normal * 0.3f + Vector3.up * 0.2f, hit.Point, 0.5f, flight.Damage * damageShare, flight.Grade, count);

        // Les graines partent de from et retombent en time s autour de center (au sol) ; aussi le duo « Semeur », du demi-tour du
        // boomerang, en l'air.
        void SowSpikesAt(Vector3 from, Vector3 center, float time, float damage, ShotGrade grade, int count)
        {
            var arrows = SpecialArrows.Instance;
            if (arrows == null)
                return;

            var start = Random.Range(0f, 360f);
            m_TrackingChild = true;
            for (var i = 0; i < count; i++)
            {
                var angle = (start + i * 360f / count) * Mathf.Deg2Rad;
                var distance = Random.Range(k_IronRainDistance.x, k_IronRainDistance.y);
                var to = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                var velocity = (to - from - 0.5f * time * time * Physics.gravity) / time;
                var child = arrows.LaunchChild(from, velocity.normalized, velocity.magnitude, damage, grade, k_SpikeColor, 1f);
                if (child != null && m_Flights.TryGetValue(child, out var seed))
                    seed.SpikeSeed = true;
            }

            m_TrackingChild = false;
        }

        // Le duo « Clôture de givre » : un ennemi qui traverse un arc électrique gèle un instant (pas plus d'une fois toutes les 3 s).
        void FrostFence(Enemy enemy)
        {
            if (!ArrowDuos.IsActive(ArrowDuos.FrostFence) || enemy.IsFrozen)
                return;
            if (m_FenceFrozen.TryGetValue(enemy, out var until) && Time.time < until)
                return;

            if (m_FenceFrozen.Count > 64)
                m_FenceFrozen.Clear();
            m_FenceFrozen[enemy] = Time.time + k_FenceCooldown;
            FreezeFor(enemy, k_FenceFreeze);
        }

        // Le duo « Réseau » : la flèche plantée se relie aussi à la baliste (ou la tour de givre) construite la plus proche.
        void LinkToBuilding(Node node)
        {
            if (!ArrowDuos.IsActive(ArrowDuos.Network))
                return;

            RefreshBuildings();
            Structure best = null;
            var bestDistance = k_LinkRange;
            foreach (var structure in m_Buildings)
            {
                if (structure == null || !structure.IsBuilt)
                    continue;
                var distance = Vector3.Distance(BuildingPoint(structure), node.Point);
                if (distance < bestDistance)
                {
                    best = structure;
                    bestDistance = distance;
                }
            }

            if (best == null)
                return;

            var link = AddLink(node, new Node { Point = BuildingPoint(best), EndAt = node.EndAt, Damage = node.Damage, Grade = node.Grade });
            link.Building = best;
        }

        static Vector3 BuildingPoint(Structure structure) => structure.transform.position + Vector3.up * 2f;

        // Les balistes et les tours de givre de la scène (le réseau, l'ingénieur), recherchées toutes les 2 s au plus.
        void RefreshBuildings()
        {
            if (Time.time < m_NextBuildingSearch)
                return;

            m_NextBuildingSearch = Time.time + 2f;
            m_Buildings.Clear();
            foreach (var structure in FindObjectsByType<Structure>())
            {
                if (structure is Ballista || structure is FrostTower)
                    m_Buildings.Add(structure);
            }
        }

        // Le talisman de l'ingénieur (27 octobre ; le joueur : « les flèches ne peuvent pas se planter dans les tourelles » : la
        // baliste et la tour de givre n'ont pas de collider, les flèches les traversaient sans les toucher) : une flèche dont le
        // trajet traverse le volume d'une baliste ou d'une tour de givre construite la surcharge, une fois par flèche.
        void TryOverchargeBuilding(Flight flight, int level)
        {
            if (flight.Charged || flight.IsChild)
                return;

            var from = flight.PreviousTip;
            var tip = flight.Arrow.TipPosition;
            var segment = tip - from;
            var length = segment.magnitude;
            if (length < 1e-4f)
                return;

            RefreshBuildings();
            var ray = new Ray(from, segment / length);
            foreach (var structure in m_Buildings)
            {
                if (structure == null || !structure.IsBuilt)
                    continue;

                var bounds = structure.ArrowBounds;
                if (!bounds.Contains(tip) && !(bounds.IntersectRay(ray, out var distance) && distance <= length))
                    continue;

                flight.Charged = true;
                OverchargeBuilding(structure, level, tip);
                return;
            }
        }

        // Les bâtiments reliés restent surchargés tant que le lien dure (appelé à chaque pas des arcs).
        void UpdateNetwork()
        {
            var engineer = Mathf.Max(1, Level(UpgradeEffect.Engineer));
            foreach (var link in m_Links)
            {
                var building = link.Building;
                if (building == null || !building.IsBuilt)
                    continue;

                building.Overcharge(k_LinkTick * 2f, SpecialArrows.ByLevel(k_OverchargeRate, engineer));
                if (!m_Overcharged.Contains(building))
                    m_Overcharged.Add(building);
            }
        }

        // Le duo « Rail » : la traînée blesse ce qui la touche, à chaque pas.
        void UpdateRail()
        {
            if (m_RailSegments.Count == 0)
                return;

            var now = Time.time;
            m_RailSegments.RemoveAll(segment => segment.EndAt <= now);
            if (now < m_NextRailTick)
                return;

            m_NextRailTick = now + k_RailTick;
            m_RailHits.Clear();
            foreach (var segment in m_RailSegments)
            {
                var count = Physics.OverlapCapsuleNonAlloc(segment.A, segment.B, k_RailRadius, s_Overlaps, Physics.DefaultRaycastLayers,
                                                           QueryTriggerInteraction.Ignore);
                for (var i = 0; i < count; i++)
                {
                    var enemy = s_Overlaps[i] != null ? s_Overlaps[i].GetComponentInParent<Enemy>() : null;
                    if (enemy == null || !enemy.IsAlive || !m_RailHits.Add(enemy))
                        continue;

                    var center = enemy.Center;
                    Hurt(enemy, segment.Damage * k_RailDamage, center, Vector3.up, new ArrowHit { Point = center, Grade = segment.Grade, IsShot = true });
                }
            }
        }

        // Le duo « Glacier » (revu le 27 octobre : avant, un ennemi sur la glace gelait au premier tir, même pas parfait ; depuis que
        // la pointe de givre gèle à chaque flèche, cela ne changeait plus rien) : un ennemi qui éclate sur une zone de glace gèle tous
        // les autres ennemis de cette zone. La flèche suivante les fait éclater à leur tour : la glace devient un champ de mines.
        IceZone GlacierUnder(Enemy enemy) => ArrowDuos.IsActive(ArrowDuos.Glacier) ? IceZone.At(enemy.transform.position) : null;

        void GlacierFreeze(Enemy shattered, IceZone zone, int level)
        {
            var frozen = 0;
            m_Enemies.Clear();
            m_Enemies.AddRange(Enemy.Alive);
            foreach (var other in m_Enemies)
            {
                if (other == null || other == shattered || !other.IsAlive || other.IsFrozen || !zone.Contains(other.transform.position))
                    continue;

                Freeze(other, level);
                frozen++;
            }

            if (frozen == 0)
                return;

            var arrows = SpecialArrows.Instance;
            var color = arrows != null ? arrows.FrostColor : k_FrozenTint;
            FloatingText.Spawn(zone.transform.position + Vector3.up * 1.6f, $"Glacier ! ×{frozen}", color, 0.7f, 1f);
            LightningBolt.Burst(zone.transform.position + Vector3.up * 0.3f, 1.2f, color, EffectMaterial, 16);
        }

        // MK, le dragon du boss final (son propre seuil d'exécution).
        bool IsDragon(Enemy enemy)
        {
            if (!m_IsDragon.TryGetValue(enemy, out var dragon))
            {
                if (m_IsDragon.Count > 128)
                    m_IsDragon.Clear();
                dragon = enemy.GetComponent<DragonBoss>() != null;
                m_IsDragon[enemy] = dragon;
            }

            return dragon;
        }
    }
}
