using System.Collections.Generic;
using Archery.Bows;
using Archery.Building;
using Archery.Combat;
using Archery.Core;
using Archery.Defense;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Archery.Upgrades
{
    // Le deuxième lot de pièces et les duos (refonte roguelike, étape 5, 26 octobre ; Docs/Refonte_Roguelike.md, partie 8) :
    // - pointe éclatée : à l'impact sur un ennemi, la flèche se brise en 4 éclats qui continuent derrière lui ;
    // - pointe de chasseur : la flèche marque sa cible (un anneau doré au-dessus) ; tout ce qui la touche fait double ; à sa
    //   mort, la marque saute sur l'ennemi le plus proche ;
    // - empennage rebond : 3 rebonds sur le sol et les murs avant de se planter (5 et 7 aux niveaux 2 et 3) ;
    // - empennage plume : plus de chute (et 10 % plus vite par niveau) ; empennage de plomb : 2,5 fois plus lourde, et une
    //   onde de choc là où elle touche le décor ;
    // - empennage spectral : elle traverse le décor (pas les ennemis, ni le gong ou les cibles) ;
    // - fût-pieu : plantée au sol, elle blesse et ralentit les ennemis qui passent dessus pendant 8 s ;
    // - fût détonateur : tirer sur une de ses flèches plantées la fait éclater en 6 flèches qui visent les ennemis proches ;
    // - fût suspendu : la flèche s'arrête en l'air au bout de 0,3 s et attend 3 s ; la gâchette de l'autre main (le clic droit
    //   sans casque) les relâche toutes, vers le point que vise cette main ;
    // - corde de transe : chaque tir parfait d'affilée fait +15 % de dégâts et accélère l'anneau ; au cinquième, la flèche
    //   dorée traverse tout ; un tir qui n'est pas parfait remet à zéro ;
    // - corde du faucon : à pleine tension, le temps ralentit (×0,4, 1,5 s, une fois par tension) ;
    // - talismans : le sniper (+2 % par mètre au-delà de 15 m, la moitié sous 10 m), le pacte de verre (dégâts ×2 dans
    //   PlayerUpgrades, un tir rouge coûte 5 PV), l'ingénieur (une flèche dans sa baliste ou sa tour de givre la surcharge
    //   5 s, un tir parfait à travers une barricade la répare), le duelliste (coup au corps à corps ×4, qui porte la pointe et
    //   rend le tir suivant parfait), le bourreau (un tir à la tête qui laisse un ennemi sous 25 % de ses PV le tue net ; un
    //   boss sous 10 %).
    // Réglages du joueur du 26 octobre : voir les constantes (et Docs/Refonte_Roguelike.md, partie « Avancement »).
    // Les duos (ArrowDuos) : trou noir (dans UpdateFuses), tempête (UpdateLinks), faux (UpdateBoomerang), essaim (ici, le fût
    // suspendu), blizzard (Freeze), œil de faucon (ici, la corde du faucon). Les failles du katana passent par OnRiftHit.
    public partial class ArrowPieces
    {
        // --- Réglages du deuxième lot (à ajuster au test) ---

        // Pointe éclatée : les éclats (un de plus par niveau), leur part des dégâts de la flèche (et par niveau), leur écart (°),
        // la part de la vitesse de la flèche qu'ils gardent.
        const int k_Splinters = 4;
        const float k_SplinterDamage = 0.4f;
        const float k_SplinterDamagePerLevel = 0.1f;
        const float k_SplinterSpread = 12f;
        const float k_SplinterSpeed = 0.8f;

        // Pointe de chasseur : les dégâts en plus sur la cible marquée (1 : le double) et par niveau (+100/150/200 % depuis le
        // 26 octobre, réglage du joueur) ; la distance (m) où saute la marque.
        const float k_MarkBonus = 1f;
        const float k_MarkBonusPerLevel = 0.5f;
        const float k_MarkJump = 12f;

        // Bourreau : la part de ses PV sous laquelle une tête touchée meurt, aux niveaux 1, 2 et 3, pour un ennemi et pour un boss
        // (26 octobre, réglages du joueur : les boss aussi) ; l'explosion de sa tête (part de la flèche, rayon). Les dégâts de la
        // flèche comptent : ils sont retirés avant le test.
        static readonly Vector3 k_ExecuteThreshold = new Vector3(0.25f, 0.37f, 0.5f);
        static readonly Vector3 k_ExecuteBossThreshold = new Vector3(0.1f, 0.15f, 0.2f);

        // MK, le dragon du boss final, à part (26 octobre au soir, réglage du joueur).
        static readonly Vector3 k_ExecuteDragonThreshold = new Vector3(0.05f, 0.075f, 0.1f);
        const float k_ExecuteBlastDamage = 0.4f;
        const float k_ExecuteBlastRadius = 2f;

        // Empennage rebond : les rebonds (3, 5, 7 depuis le 26 octobre, réglage du joueur : deux de plus par niveau), la part de la
        // vitesse gardée.
        const int k_Bounces = 3;
        const int k_BouncesPerLevel = 2;
        const float k_BounceSpeed = 0.8f;

        // La vitesse (m/s) au moins d'une flèche de la pluie qui rebondit (27 octobre).
        const float k_RainBounceSpeed = 5f;

        // Empennages plume et de plomb : la gravité des flèches ; l'onde du plomb (part des dégâts, rayon, et par niveau).
        const float k_FeatherGravity = 0.001f;
        const float k_LeadGravity = 2.5f;
        const float k_LeadDamage = 0.6f;
        const float k_LeadDamagePerLevel = 0.2f;
        const float k_LeadRadius = 2.5f;
        const float k_LeadRadiusPerLevel = 0.5f;

        // Empennage spectral : les dégâts en plus par niveau au-delà du premier.
        const float k_SpectralDamagePerLevel = 0.15f;

        // Fût-pieu : la durée (s) et par niveau, le pas des dégâts (s), leur part des dégâts du tir (et par niveau), la portée (m),
        // les pieux à la fois. Réglages du joueur du 26 octobre : 20 pieux à tous les niveaux (6, 8, 10 avant), 2 m de portée
        // (1 m), des dégâts divisés par deux.
        const float k_SpikeTime = 8f;
        const float k_SpikeTimePerLevel = 2f;
        const float k_SpikeTick = 0.3f;
        const float k_SpikeDamage = 0.175f;
        const float k_SpikeDamagePerLevel = 0.075f;
        const float k_SpikeRadius = 2f;
        const int k_MaxSpikes = 20;

        // Fût détonateur : les flèches (et par niveau), leur part des dégâts (et par niveau), leur vitesse (m/s), la distance (m)
        // jusqu'où elles visent, la distance (m) d'un tir qui le déclenche, les détonateurs à la fois.
        const int k_DetonatorArrows = 6;
        const int k_DetonatorArrowsPerLevel = 2;
        const float k_DetonatorDamage = 0.6f;
        const float k_DetonatorDamagePerLevel = 0.2f;
        const float k_DetonatorSpeed = 25f;
        const float k_DetonatorAimRange = 10f;
        const float k_DetonatorRadius = 0.3f;
        const int k_MaxDetonators = 100;

        // Un impact (sur un ennemi ou le décor) à moins de tant de m d'un détonateur le déclenche aussi (27 octobre : une flèche
        // plantée dans un mannequin ne se déclenchait jamais, la flèche qui le touchait s'y plantait avant que son trajet soit
        // regardé ; le billard en souffrait). Les détonateurs au plus : 100 depuis le 27 octobre (réglage du joueur ; 10 avant).
        const float k_DetonatorHitRadius = 0.6f;

        // Fût suspendu : le moment de l'arrêt (s ; 0,15 depuis le 27 octobre, réglage du joueur : deux fois plus tôt), l'attente (s)
        // et par niveau, la portée (m) de la visée de la main qui les relâche
        // (26 octobre, demandé par le joueur : elles partent là où vise cette main) et leur vitesse au moins (m/s) ; l'essaim : le
        // rayon de l'orbite (m), sa vitesse (°/s), la distance (m) des ennemis visés, l'intervalle (s) entre deux flèches, leur
        // vitesse (m/s).
        const float k_SuspendDelay = 0.15f;
        const float k_HoverTime = 3f;
        const float k_HoverTimePerLevel = 1f;
        const float k_ReleaseAimRange = 80f;
        const float k_ReleaseMinSpeed = 30f;
        const float k_OrbitRadius = 1.3f;
        const float k_OrbitSpeed = 90f;
        const float k_SwarmRange = 18f;
        const float k_SwarmInterval = 0.35f;
        const float k_SwarmSpeed = 30f;

        // Corde de transe : les dégâts en plus par tir parfait d'affilée (et par niveau), la vitesse de l'anneau en plus, les tirs au plus.
        const float k_TranceDamage = 0.15f;
        const float k_TranceDamagePerLevel = 0.05f;
        const float k_TranceRing = 0.08f;
        const int k_TranceMax = 5;

        // Corde du faucon : la vitesse du temps (et par niveau), la durée (s, et par niveau), la tension qui le déclenche, et
        // celle sous laquelle il se réarme. Depuis le 26 octobre (réglage du joueur), plus de recharge : une fois par tension.
        const float k_FalconScale = 0.4f;
        const float k_FalconScalePerLevel = 0.05f;
        const float k_FalconTime = 1.5f;
        const float k_FalconTimePerLevel = 0.5f;
        const float k_FalconDraw = 0.98f;
        const float k_FalconRearm = 0.5f;

        // Sniper : la moitié des dégâts sous 10 m, normal à 15 m, puis tant par mètre (et par niveau).
        const float k_SniperNear = 10f;
        const float k_SniperFar = 15f;
        const float k_SniperNearFactor = 0.5f;
        const float k_SniperPerMeter = 0.02f;
        const float k_SniperPerMeterPerLevel = 0.01f;

        // Pacte de verre : les PV perdus par tir raté. Ingénieur : la durée de la surcharge (s) et par niveau, la cadence de tir
        // de la baliste surchargée aux niveaux 1, 2 et 3 (26 octobre, réglage du joueur ; ×2 avant), et la part des PV d'une
        // barricade que répare un tir parfait qui la traverse (et par niveau ; le 26 octobre). Duelliste : les dégâts du coup au
        // corps à corps (×) et par niveau.
        const float k_GlassPactCost = 5f;
        const float k_OverchargeTime = 5f;
        const float k_OverchargeTimePerLevel = 2f;
        static readonly Vector3 k_OverchargeRate = new Vector3(2f, 4f, 6f);
        const float k_BarricadeRepair = 0.1f;
        const float k_BarricadeRepairPerLevel = 0.05f;
        const float k_DuelistMelee = 4f;
        // (×4 / ×8 / ×12 depuis le 27 octobre, réglage du joueur ; ×4 / ×5 / ×6 avant.)
        const float k_DuelistMeleePerLevel = 4f;

        // Duos : la chance de la foudre de la tempête à chaque pas de l'arc et ses dégâts (part du tir) ; le rayon du blizzard ; les
        // marques de l'œil de faucon (au plus), leur distance aux yeux (m), leur taille (m), la portée (m).
        const float k_TempestChance = 0.3f;
        const float k_TempestDamage = 0.75f;
        const float k_BlizzardRadius = 3f;
        const int k_MaxHawkMarks = 10;
        const float k_HawkMarkDistance = 2.5f;
        const float k_HawkMarkSize = 0.3f;

        // L'œil de faucon (27 octobre, réglages du joueur) : les dégâts sur les points faibles (×), la taille au moins des marques
        // (en part de leur taille à 2,5 m : les ennemis lointains restent visibles).
        const float k_HawkWeakPointDamage = 1.5f;
        const float k_HawkMinScale = 0.2f;
        const float k_HawkRange = 60f;

        static readonly Color k_MarkColor = new Color(1f, 0.78f, 0.25f);
        static readonly Color k_TranceColor = new Color(1f, 0.82f, 0.3f);
        static readonly Color k_DuoColor = new Color(1f, 0.82f, 0.3f);
        static readonly Color k_SpikeColor = new Color(0.95f, 0.8f, 0.55f);
        static readonly Color k_LeadColor = new Color(0.8f, 0.72f, 0.62f);
        static readonly Color k_OverchargeColor = new Color(0.45f, 0.9f, 1f);
        static readonly Color k_ExecuteColor = new Color(1f, 0.25f, 0.2f);

        class Spike
        {
            public Arrow Arrow;
            public Vector3 Point;
            public float EndAt;
            public float Damage;
            public ShotGrade Grade;
        }

        class Detonator
        {
            public Arrow Arrow;
            public Vector3 Point;
            public float Damage;
            public ShotGrade Grade;

            // Sa flèche rappelée (Arrow vide) : il reste là jusqu'à ce moment, et crépite (27 octobre).
            public float DetachedUntil;
            public float NextSpark;
        }

        // Le billard : un détonateur visé par une flèche de détonation, et quand elle arrive.
        struct BilliardShot
        {
            public Detonator Detonator;
            public float At;
        }

        readonly List<BilliardShot> m_BilliardShots = new List<BilliardShot>();
        readonly List<Detonator> m_BilliardDue = new List<Detonator>();
        readonly List<Detonator> m_DetonateSoon = new List<Detonator>();
        const float k_BilliardRange = 15f;
        const int k_BilliardMax = 4;
        const float k_BilliardSpeed = 30f;

        readonly List<Spike> m_Spikes = new List<Spike>();
        readonly List<Detonator> m_Detonators = new List<Detonator>();
        readonly List<Detonator> m_Detonating = new List<Detonator>();
        readonly List<Enemy> m_Exclude = new List<Enemy>();
        readonly List<Structure> m_Overcharged = new List<Structure>();
        readonly List<Transform> m_HawkMarks = new List<Transform>();
        readonly Dictionary<Enemy, Collider> m_WeakPoints = new Dictionary<Enemy, Collider>();
        readonly List<Flight> m_HoverList = new List<Flight>();
        readonly object m_FalconToken = new object();
        bool m_TrackingChild;
        float m_NextSpikeTick;
        Enemy m_Marked;
        LineRenderer m_MarkRing;
        bool m_InMarkBonus;
        int m_Trance;
        bool m_FalconActive;
        float m_FalconUntil;
        bool m_FalconArmed = true;
        float m_NextSwarmShot;
        float m_NextOrbitAngle;
        float m_NextOverchargeSpark;
        // Un bit par duo (long depuis le 27 octobre : plus de 31 duos possibles).
        long m_ActiveDuos;
        bool m_DuosReady;
        Material m_HawkMaterial;

        void EnableLot2()
        {
            Enemy.Damaged += OnEnemyDamaged;
            Enemy.Killed += OnEnemyKilled;
            Bow.GoldenPerfectZone = TranceGolden;
        }

        void DisableLot2()
        {
            Enemy.Damaged -= OnEnemyDamaged;
            Enemy.Killed -= OnEnemyKilled;
            if (Bow.GoldenPerfectZone == (System.Func<bool>)TranceGolden)
                Bow.GoldenPerfectZone = null;
            Hitbox.WeakPointDamageMultiplier = 1f;
            SlowMotion.End(m_FalconToken);
            m_FalconActive = false;
            Arrow.MeleeDamageMultiplier = 1f;
            Bow.GravityMultiplier = 1f;
            if (PlayerUpgrades.Instance != null)
                PlayerUpgrades.Instance.BonusRingSpeed = 0f;
            m_Spikes.Clear();
            m_Detonators.Clear();
            m_BilliardShots.Clear();
            m_DetonateSoon.Clear();
            m_Overcharged.Clear();
            SetMark(null);
            foreach (var mark in m_HawkMarks)
            {
                if (mark != null)
                    mark.gameObject.SetActive(false);
            }
        }

        // ---------------------------------------------------------------- au tir : transe, pacte de verre

        void OnShotStart(in ShotInfo shot)
        {
            // Pacte de verre : un tir raté (le rouge de l'anneau) coûte des PV.
            if (Level(UpgradeEffect.GlassPact) > 0 && shot.Grade == ShotGrade.Miss)
            {
                var player = PlayerHealth.Instance;
                if (player != null && player.IsAlive)
                {
                    player.Health.TakeDamage(new DamageInfo
                    {
                        Amount = k_GlassPactCost,
                        Point = shot.Origin,
                        Direction = Vector3.down,
                        Source = this,
                    });
                }
            }

            // Corde de transe : les tirs parfaits d'affilée.
            if (Level(UpgradeEffect.Trance) > 0)
            {
                if (shot.Grade == ShotGrade.Perfect)
                {
                    m_Trance = Mathf.Min(k_TranceMax, m_Trance + 1);
                    FloatingText.Spawn(shot.Origin + Vector3.up * 0.25f, m_Trance >= k_TranceMax ? "Transe !" : $"Transe {m_Trance}",
                                       k_TranceColor, 0.55f, 0.8f);
                }
                else
                {
                    if (m_Trance >= 2)
                        FloatingText.Spawn(shot.Origin + Vector3.up * 0.25f, "Transe perdue", new Color(0.7f, 0.7f, 0.75f), 0.5f, 0.8f);
                    m_Trance = 0;
                }
            }
            else
            {
                m_Trance = 0;
            }

            if (PlayerUpgrades.Instance != null)
                PlayerUpgrades.Instance.BonusRingSpeed = k_TranceRing * m_Trance;
        }

        // La corde de transe : un tir parfait maintenant serait une flèche dorée (le cinquième d'affilée, et tous ceux d'après tant
        // que les parfaits s'enchaînent : la transe reste à 5) ; la zone du parfait de l'anneau est alors dorée (27 octobre).
        bool TranceGolden() => Level(UpgradeEffect.Trance) > 0 && m_Trance >= k_TranceMax - 1;

        // Une flèche suivie : la transe et l'empennage spectral changent ses dégâts, le sniper part de la moitié, le fût suspendu
        // l'arrêtera bientôt. Les flèches nées d'une pièce (éclats, détonations) n'ont rien de tout ça.
        void TrackLot2(Arrow arrow, Flight flight)
        {
            flight.SuspendAt = 0f;
            if (flight.IsChild)
                return;

            var multiplier = 1f;
            var trance = Level(UpgradeEffect.Trance);
            if (trance > 0 && m_Trance > 0)
                multiplier *= 1f + (k_TranceDamage + k_TranceDamagePerLevel * (trance - 1)) * m_Trance;

            var spectral = Level(UpgradeEffect.Spectral);
            if (spectral > 0)
            {
                arrow.PassesThroughScenery = true;
                multiplier *= 1f + k_SpectralDamagePerLevel * (spectral - 1);
            }

            if (!Mathf.Approximately(multiplier, 1f))
            {
                arrow.Damage *= multiplier;
                flight.Damage *= multiplier;
            }

            // Au cinquième tir parfait d'affilée, la flèche dorée traverse tout (et, avec le duo « Supernova », elle explose fort).
            if (trance > 0 && m_Trance >= k_TranceMax)
            {
                arrow.AddPierce(20);
                arrow.SetTrailColor(k_TranceColor);
                flight.Golden = true;
            }

            if (Level(UpgradeEffect.Sniper) > 0)
            {
                arrow.Damage *= k_SniperNearFactor;
                flight.SniperFactor = k_SniperNearFactor;
            }

            if (Level(UpgradeEffect.Suspended) > 0)
                flight.SuspendAt = Time.time + k_SuspendDelay;
        }

        // ---------------------------------------------------------------- chaque image

        void UpdateLot2()
        {
            // Plume et plomb : la gravité des flèches (et l'aide à la visée) ; duelliste : le coup au corps à corps.
            Bow.GravityMultiplier = Level(UpgradeEffect.Feather) > 0 ? k_FeatherGravity : Level(UpgradeEffect.Lead) > 0 ? k_LeadGravity : 1f;
            var duelist = Level(UpgradeEffect.Duelist);
            Arrow.MeleeDamageMultiplier = duelist > 0 ? k_DuelistMelee + k_DuelistMeleePerLevel * (duelist - 1) : 1f;

            // La corde de transe retirée (dans l'atelier) : l'anneau reprend sa vitesse tout de suite.
            if (m_Trance > 0 && Level(UpgradeEffect.Trance) == 0)
            {
                m_Trance = 0;
                if (PlayerUpgrades.Instance != null)
                    PlayerUpgrades.Instance.BonusRingSpeed = 0f;
            }

            UpdateSpikes();
            UpdateDetonators();
            UpdateHovering();
            UpdateMark();
            UpdateFalcon();
            UpdateOvercharged();
            UpdateDuos();
        }

        // ---------------------------------------------------------------- en vol : sniper, fût suspendu, détonateur

        // Les dégâts suivent la distance parcourue (multiplicatif : les ennemis traversés gardent leur effet).
        static void UpdateSniper(Flight flight, int level)
        {
            var distance = Vector3.Distance(flight.Origin, flight.Arrow.transform.position);
            float factor;
            if (distance < k_SniperNear)
                factor = k_SniperNearFactor;
            else if (distance < k_SniperFar)
                factor = Mathf.Lerp(k_SniperNearFactor, 1f, (distance - k_SniperNear) / (k_SniperFar - k_SniperNear));
            else
                factor = 1f + (k_SniperPerMeter + k_SniperPerMeterPerLevel * (level - 1)) * (distance - k_SniperFar);

            if (Mathf.Approximately(factor, flight.SniperFactor))
                return;

            flight.Arrow.Damage = flight.Arrow.Damage / Mathf.Max(0.01f, flight.SniperFactor) * factor;
            flight.SniperFactor = factor;
        }

        // Fût suspendu : la flèche s'arrête au bout de 0,3 s et attend ; avec l'essaim, elle tourne autour du joueur. Vrai tant
        // qu'elle attend.
        bool UpdateSuspended(Flight flight, int level, float deltaTime)
        {
            if (flight.IsChild || flight.Released || flight.SuspendAt <= 0f)
                return false;

            var arrow = flight.Arrow;
            if (!flight.Hovering)
            {
                if (Time.time < flight.SuspendAt)
                    return false;

                flight.SavedVelocity = arrow.Velocity;
                arrow.Hover(true);
                flight.Hovering = true;
                flight.HoverUntil = Time.time + k_HoverTime + k_HoverTimePerLevel * (level - 1);
                flight.OrbitAngle = m_NextOrbitAngle;
                m_NextOrbitAngle += 47f;
                return true;
            }

            if (Time.time >= flight.HoverUntil)
            {
                Release(flight, flight.SavedVelocity);
                return false;
            }

            if (ArrowDuos.IsActive(ArrowDuos.Swarm))
            {
                // L'essaim : elle glisse sur une orbite autour du joueur, pointe vers l'extérieur.
                flight.OrbitAngle += k_OrbitSpeed * deltaTime;
                var angle = flight.OrbitAngle * Mathf.Deg2Rad;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var target = ReturnPoint() + Vector3.up * 0.2f + outward * k_OrbitRadius;
                var position = Vector3.Lerp(arrow.transform.position, target, 1f - Mathf.Exp(-6f * deltaTime));
                arrow.HoverAt(position, Quaternion.LookRotation(outward));
            }

            return true;
        }

        static void Release(Flight flight, Vector3 velocity)
        {
            flight.Hovering = false;
            flight.Released = true;
            if (flight.Arrow == null)
                return;

            flight.Arrow.Hover(false);
            flight.Arrow.Velocity = velocity;
            flight.Origin = flight.Arrow.transform.position;
            flight.LaunchTime = Time.time;
        }

        // La gâchette de l'autre main (le clic droit sans casque) relâche toutes les flèches suspendues ; l'essaim en lance une
        // de temps en temps sur l'ennemi le plus proche. Celles d'un fût suspendu remplacé repartent.
        void UpdateHovering()
        {
            m_HoverList.Clear();
            foreach (var flight in m_Flights.Values)
            {
                if (flight.Hovering && flight.Arrow != null && flight.Arrow.IsHovering)
                    m_HoverList.Add(flight);
            }

            UpdateAimRay(m_HoverList.Count > 0 && Level(UpgradeEffect.Suspended) > 0);
            if (m_HoverList.Count == 0)
                return;

            // Le fût suspendu retiré : elles repartent comme elles allaient.
            if (Level(UpgradeEffect.Suspended) == 0)
            {
                foreach (var flight in m_HoverList)
                    Release(flight, flight.SavedVelocity.sqrMagnitude > 1f ? flight.SavedVelocity : flight.Arrow.transform.forward * k_SwarmSpeed);
                return;
            }

            // La gâchette de l'autre main : toutes partent vers le point que vise cette main (26 octobre, demandé par le joueur).
            if (OtherHandPressed(out var aim))
            {
                var aimed = AimedPoint(aim);
                foreach (var flight in m_HoverList)
                {
                    var speed = Mathf.Max(k_ReleaseMinSpeed, flight.SavedVelocity.magnitude);
                    var toTarget = aimed - flight.Arrow.transform.position;
                    Release(flight, toTarget.sqrMagnitude > 0.01f ? toTarget.normalized * speed : aim.forward * speed);
                }

                Sfx.Play(PieceSounds.Whoosh, ReturnPoint(), 0.7f, 1.2f, 0.5f);
                DownpourRelease(aimed, m_HoverList);
                return;
            }

            if (!ArrowDuos.IsActive(ArrowDuos.Swarm) || Time.time < m_NextSwarmShot)
                return;

            m_Exclude.Clear();
            var target = SpecialArrows.NearestEnemy(ReturnPoint(), k_SwarmRange, m_Exclude);
            if (target == null)
                return;

            var chosen = m_HoverList[0];
            var direction = (target.Center - chosen.Arrow.transform.position).normalized;
            Release(chosen, direction * k_SwarmSpeed);
            m_NextSwarmShot = Time.time + k_SwarmInterval;
        }

        // Le rayon du fût suspendu (27 octobre, demandé par le joueur) : tant que des flèches attendent en l'air, un fin rayon part
        // de la main libre (celle dont la gâchette les relâche) jusqu'au point qu'elle vise, avec un cercle à ce point ; sans
        // casque, seulement le cercle, là où vise la caméra.
        const int k_AimMarkerPoints = 20;
        const float k_AimMarkerRadius = 0.25f;
        static readonly Color k_AimRayColor = new Color(0.6f, 0.9f, 1f, 0.55f);
        LineRenderer m_AimRay;
        LineRenderer m_AimMarker;

        void UpdateAimRay(bool show)
        {
            var aim = show ? FreeHandAim() : null;
            if (aim == null)
            {
                if (m_AimRay != null)
                    m_AimRay.enabled = false;
                if (m_AimMarker != null)
                    m_AimMarker.enabled = false;
                return;
            }

            if (m_AimRay == null)
            {
                m_AimRay = AimLine("Rayon du fût suspendu", false, 2, 0.006f);
                m_AimMarker = AimLine("Cible du fût suspendu", true, k_AimMarkerPoints, 0.012f);
            }

            var point = AimedPoint(aim);
            var desktop = DesktopControls.Instance;
            var onDesktop = desktop != null && desktop.IsActive && aim == desktop.CameraTransform;
            m_AimRay.enabled = !onDesktop;
            if (!onDesktop)
            {
                m_AimRay.SetPosition(0, aim.position + aim.forward * 0.05f);
                m_AimRay.SetPosition(1, point);
            }

            // Le cercle fait face à la main, un peu en avant de l'obstacle pour ne pas s'y enfoncer.
            var forward = aim.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude < 1e-4f)
                right = Vector3.right;
            right.Normalize();
            var up = Vector3.Cross(forward, right);
            var center = point - forward * 0.05f;
            var radius = k_AimMarkerRadius * Mathf.Clamp(Vector3.Distance(aim.position, point) / 10f, 0.5f, 3f);
            m_AimMarker.enabled = true;
            for (var i = 0; i < k_AimMarkerPoints; i++)
            {
                var angle = i * 2f * Mathf.PI / k_AimMarkerPoints;
                m_AimMarker.SetPosition(i, center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius);
            }
        }

        LineRenderer AimLine(string name, bool loop, int points, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = points;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = EffectMaterial;
            line.startColor = k_AimRayColor;
            line.endColor = k_AimRayColor;
            return line;
        }

        // La main libre, celle qui relâche les flèches suspendues (pas celle de l'arc) ; sans casque, la caméra.
        Transform FreeHandAim()
        {
            var desktop = DesktopControls.Instance;
            if (desktop != null && desktop.IsActive)
                return desktop.CameraTransform;

            var rig = PlayerRig.Instance;
            if (rig == null)
                return null;
            foreach (var candidate in rig.Hands)
            {
                if (candidate != null && candidate.activateInput != null && !ReferenceEquals(candidate, m_ShotHand))
                    return candidate.transform;
            }

            return null;
        }

        // La gâchette de l'autre main vient d'être pressée ; aim : cette main (sans casque, la caméra et le clic droit).
        bool OtherHandPressed(out Transform aim)
        {
            aim = null;
            var rig = PlayerRig.Instance;
            if (rig != null)
            {
                foreach (var hand in rig.Hands)
                {
                    if (hand == null || hand.activateInput == null || ReferenceEquals(hand, m_ShotHand))
                        continue;
                    if (hand.activateInput.ReadWasPerformedThisFrame())
                    {
                        aim = hand.transform;
                        return true;
                    }
                }
            }

            var desktop = DesktopControls.Instance;
            var mouse = Mouse.current;
            if (desktop == null || !desktop.IsActive || desktop.CameraTransform == null || mouse == null || !mouse.rightButton.wasPressedThisFrame)
                return false;

            aim = desktop.CameraTransform;
            return true;
        }

        // Le point que vise la main : le premier obstacle ou ennemi devant elle (pas le joueur lui-même), sinon loin devant.
        static Vector3 AimedPoint(Transform aim)
        {
            var origin = aim.position + aim.forward * 0.3f;
            var count = Physics.RaycastNonAlloc(origin, aim.forward, s_AimHits, k_ReleaseAimRange, Physics.DefaultRaycastLayers,
                                                QueryTriggerInteraction.Ignore);
            var rig = PlayerRig.Instance;
            var best = k_ReleaseAimRange;
            for (var i = 0; i < count; i++)
            {
                var hit = s_AimHits[i];
                if (hit.collider == null || hit.distance >= best || ArrowIgnore.IsIgnored(hit.collider) ||
                    (rig != null && hit.collider.transform.IsChildOf(rig.transform)) || hit.collider.GetComponentInParent<Arrow>() != null)
                    continue;
                best = hit.distance;
            }

            return origin + aim.forward * best;
        }

        static readonly RaycastHit[] s_AimHits = new RaycastHit[16];

        // Une flèche en vol passe près d'une flèche plantée du fût détonateur.
        void FindDetonator(Flight flight)
        {
            var tip = flight.Arrow.TipPosition;
            foreach (var detonator in m_Detonators)
            {
                if (detonator.Arrow == flight.Arrow || m_Detonating.Contains(detonator))
                    continue;
                if (DistanceToSegment(DetonatorCenter(detonator), flight.PreviousTip, tip) < k_DetonatorRadius)
                    m_Detonating.Add(detonator);
            }
        }

        // L'impact d'une flèche près d'un détonateur (plantée dans un ennemi : il bouge avec lui) le déclenche ; il éclate à l'image
        // suivante (UpdateDetonators), hors de l'impact. Les flèches nées d'une pièce : seulement avec le billard.
        void DetonatorsNear(in ArrowHit hit, Flight flight)
        {
            if (m_Detonators.Count == 0 || (flight.IsChild && !ArrowDuos.IsActive(ArrowDuos.Billiard)))
                return;

            foreach (var detonator in m_Detonators)
            {
                if (detonator.Arrow == hit.Arrow || m_DetonateSoon.Contains(detonator))
                    continue;
                if ((DetonatorCenter(detonator) - hit.Point).sqrMagnitude < k_DetonatorHitRadius * k_DetonatorHitRadius)
                    m_DetonateSoon.Add(detonator);
            }
        }

        static Vector3 DetonatorCenter(Detonator detonator) =>
            StillAt(detonator.Arrow, detonator.Point)
                ? detonator.Arrow.transform.position + detonator.Arrow.transform.forward * (detonator.Arrow.Length * 0.5f)
                : detonator.Point;

        // La flèche plantée éclate : des flèches partent sur les ennemis proches, les autres tout autour.
        void Detonate(Detonator detonator)
        {
            if (!m_Detonators.Remove(detonator))
                return;

            var center = DetonatorCenter(detonator);
            if (StillAt(detonator.Arrow, detonator.Point))
                detonator.Arrow.Despawn();

            var arrows = SpecialArrows.Instance;
            if (arrows == null)
                return;

            var level = Mathf.Max(1, Level(UpgradeEffect.Detonator));
            var count = k_DetonatorArrows + k_DetonatorArrowsPerLevel * (level - 1);
            var damage = detonator.Damage * (k_DetonatorDamage + k_DetonatorDamagePerLevel * (level - 1));
            DetonationVolley(center, damage, detonator.Grade, count);
            if (ArrowDuos.IsActive(ArrowDuos.Billiard))
                BilliardChain(center, damage, detonator.Grade);
            Sfx.Play(arrows.ExplosionClip, center, 0.6f, 1.3f);
            FloatingText.Spawn(center + Vector3.up * 0.4f, "Détonation !", k_FuseColor, 0.6f, 0.8f);
        }

        // La volée d'une détonation : des flèches partent sur les ennemis proches, les autres tout autour (aussi le duo
        // « Fragmentation », à chaque rebond, 27 octobre).
        void DetonationVolley(Vector3 center, float damage, ShotGrade grade, int count)
        {
            var arrows = SpecialArrows.Instance;
            if (arrows == null)
                return;

            m_Exclude.Clear();
            m_TrackingChild = true;
            for (var i = 0; i < count; i++)
            {
                var target = SpecialArrows.NearestEnemy(center, k_DetonatorAimRange, m_Exclude);
                Vector3 direction;
                if (target != null)
                {
                    m_Exclude.Add(target);
                    direction = (target.Center + Vector3.up * 0.3f - center).normalized;
                }
                else
                {
                    direction = Quaternion.Euler(-10f, i * 360f / count, 0f) * Vector3.forward;
                }

                arrows.LaunchChild(center + direction * 0.3f, direction, k_DetonatorSpeed, damage, grade, k_FuseColor, 1f);
            }

            m_TrackingChild = false;
            LightningBolt.Burst(center, 0.8f, k_FuseColor, EffectMaterial, 8);
        }

        // ---------------------------------------------------------------- plantée : pieu, détonateur

        void OnStuckLot2(Arrow arrow, Flight flight, Enemy enemy)
        {
            if (flight.IsChild)
                return;

            var spike = Level(UpgradeEffect.Spike);
            if (spike > 0 && enemy == null)
            {
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

            if (Level(UpgradeEffect.Detonator) > 0)
            {
                while (m_Detonators.Count >= k_MaxDetonators)
                    m_Detonators.RemoveAt(0);
                m_Detonators.Add(new Detonator { Arrow = arrow, Point = arrow.TipPosition, Damage = flight.Damage, Grade = flight.Grade });
            }
        }

        // Les pieux blessent et ralentissent ce qui marche dessus (les volants passent au-dessus).
        void UpdateSpikes()
        {
            var now = Time.time;
            // Un pieu dont la flèche a été rappelée (Arrow vide) reste jusqu'au bout de son temps (27 octobre).
            for (var i = m_Spikes.Count - 1; i >= 0; i--)
            {
                var spike = m_Spikes[i];
                if (now >= spike.EndAt || (spike.Arrow != null && !StillAt(spike.Arrow, spike.Point)))
                    m_Spikes.RemoveAt(i);
            }

            if (m_Spikes.Count == 0 || now < m_NextSpikeTick)
                return;

            m_NextSpikeTick = now + k_SpikeTick;
            var level = Mathf.Max(1, Level(UpgradeEffect.Spike));
            var share = k_SpikeDamage + k_SpikeDamagePerLevel * (level - 1);
            m_Enemies.Clear();
            m_Enemies.AddRange(Enemy.Alive);
            foreach (var spike in m_Spikes)
            {
                foreach (var enemy in m_Enemies)
                {
                    if (enemy == null || !enemy.IsAlive)
                        continue;

                    var offset = enemy.transform.position - spike.Point;
                    if (Mathf.Abs(offset.y) > 1.2f)
                        continue;
                    offset.y = 0f;
                    if (offset.sqrMagnitude > k_SpikeRadius * k_SpikeRadius)
                        continue;

                    var point = enemy.transform.position + Vector3.up * 0.3f;
                    Hurt(enemy, spike.Damage * share, point, Vector3.up, new ArrowHit { Point = point, Grade = spike.Grade, IsShot = true });
                    enemy.Slow(0.8f, 0.5f);
                    LightningBolt.Spawn(spike.Point, spike.Point + Vector3.up * 0.6f, k_SpikeColor, EffectMaterial, 0.03f, 0.12f);
                }
            }
        }

        // Les détonateurs dont la flèche a disparu s'effacent.
        void UpdateDetonators()
        {
            // Un détonateur dont la flèche a été rappelée (Arrow vide) reste un moment là où elle était, et crépite (27 octobre).
            for (var i = m_Detonators.Count - 1; i >= 0; i--)
            {
                var detonator = m_Detonators[i];
                if (detonator.Arrow == null)
                {
                    if (Time.time >= detonator.DetachedUntil)
                    {
                        m_Detonators.RemoveAt(i);
                    }
                    else if (Time.time >= detonator.NextSpark)
                    {
                        detonator.NextSpark = Time.time + 0.3f;
                        LightningBolt.Burst(detonator.Point, 0.2f, k_FuseColor, EffectMaterial, 2);
                    }
                }
                else if (!StillAt(detonator.Arrow, detonator.Point))
                {
                    m_Detonators.RemoveAt(i);
                }
                else
                {
                    detonator.Point = detonator.Arrow.TipPosition;
                }
            }

            // Les détonateurs déclenchés par un impact.
            if (m_DetonateSoon.Count > 0)
            {
                m_BilliardDue.Clear();
                m_BilliardDue.AddRange(m_DetonateSoon);
                m_DetonateSoon.Clear();
                foreach (var detonator in m_BilliardDue)
                {
                    if (m_Detonators.Contains(detonator))
                        Detonate(detonator);
                }
            }

            // Le billard : les détonateurs visés éclatent à l'arrivée de leur flèche (s'ils sont encore là).
            if (m_BilliardShots.Count == 0)
                return;

            var now = Time.time;
            m_BilliardDue.Clear();
            for (var i = m_BilliardShots.Count - 1; i >= 0; i--)
            {
                if (now < m_BilliardShots[i].At)
                    continue;
                m_BilliardDue.Add(m_BilliardShots[i].Detonator);
                m_BilliardShots.RemoveAt(i);
            }

            foreach (var detonator in m_BilliardDue)
            {
                if (m_Detonators.Contains(detonator))
                    Detonate(detonator);
            }
        }

        // Le duo « Billard » (revu le 27 octobre ; le joueur : « les déclenchements sont un peu aléatoires » : les flèches d'une
        // détonation visaient les ennemis et ne faisaient éclater un autre détonateur qu'en passant par hasard à moins de 30 cm) :
        // chaque détonation lance aussi une flèche droit sur chaque autre détonateur planté à moins de k_BilliardRange m (au plus
        // k_BilliardMax), qui le fait éclater à son arrivée : en chaîne.
        void BilliardChain(Vector3 center, float damage, ShotGrade grade)
        {
            var arrows = SpecialArrows.Instance;
            if (arrows == null)
                return;

            var sent = 0;
            foreach (var other in m_Detonators)
            {
                if (sent >= k_BilliardMax)
                    break;
                if (m_BilliardShots.Exists(shot => shot.Detonator == other))
                    continue;

                var offset = DetonatorCenter(other) - center;
                var distance = offset.magnitude;
                if (distance < 0.3f || distance > k_BilliardRange)
                    continue;

                var direction = offset / distance;
                m_TrackingChild = true;
                arrows.LaunchChild(center + direction * 0.3f, direction, k_BilliardSpeed, damage, grade, k_FuseColor, 0.001f);
                m_TrackingChild = false;
                m_BilliardShots.Add(new BilliardShot { Detonator = other, At = Time.time + distance / k_BilliardSpeed });
                sent++;
            }
        }

        // ---------------------------------------------------------------- à l'impact

        // Duelliste : le coup de flèche au corps à corps porte la pointe (la flèche est suivie dès ce coup), et le tir suivant
        // est parfait. Vrai si la flèche est maintenant suivie.
        bool OnMeleeHit(in ArrowHit hit)
        {
            if (Level(UpgradeEffect.Duelist) == 0)
                return false;

            Track(hit.Arrow, hit.Damage, hit.Grade);
            m_QueuedPerfects = Mathf.Min(m_QueuedPerfects + 1, k_MaxQueuedPerfects);
            var enemy = hit.Collider.GetComponentInParent<Enemy>();
            var arrows = SpecialArrows.Instance;
            if (arrows != null && enemy != null)
                arrows.Impact(arrows.RollEffects(hit.Grade), hit, enemy);
            FloatingText.Spawn(hit.Point + Vector3.up * 0.3f, "Touché ! Tir parfait", k_CatchColor, 0.6f, 0.9f);
            return m_Flights.ContainsKey(hit.Arrow);
        }

        // Le décor : l'ingénieur (ses bâtiments), le plomb (une onde de choc), le rebond, le retour du boomerang (26 octobre).
        // Le gong, les cibles et les menus ne font rien de tout ça (une cible renvoie quand même le boomerang).
        void OnSceneryHit(in ArrowHit hit, Flight flight)
        {
            var structure = hit.Collider.GetComponentInParent<Structure>();
            if (structure != null)
            {
                var engineer = Level(UpgradeEffect.Engineer);
                if (engineer > 0 && !flight.Charged && structure.IsBuilt && (structure is Ballista || structure is FrostTower))
                {
                    flight.Charged = true;
                    OverchargeBuilding(structure, engineer, hit.Point);
                }

                BoomerangOffScenery(hit, flight);
                return;
            }

            var handler = hit.Collider.GetComponentInParent<IArrowHitHandler>();
            if (handler != null)
            {
                if (!(handler is IArrowAbsorber))
                    BoomerangOffScenery(hit, flight);
                return;
            }

            if (BoomerangOffScenery(hit, flight))
                return;

            var lead = Level(UpgradeEffect.Lead);
            if (lead > 0 && !flight.IsChild)
            {
                var radius = k_LeadRadius + k_LeadRadiusPerLevel * (lead - 1);
                LightningBolt.Burst(hit.Point, radius * 0.6f, k_LeadColor, EffectMaterial, 10);
                var arrows = SpecialArrows.Instance;
                if (arrows != null)
                    Sfx.Play(arrows.ExplosionClip, hit.Point, 0.5f, 0.6f);
                AreaDamage(hit.Point, flight.Damage * (k_LeadDamage + k_LeadDamagePerLevel * (lead - 1)), radius, hit);
                IronRain(hit, flight);
            }

            // Depuis le 27 octobre, les flèches nées d'une pièce (éclats, petites flèches de la sentinelle, des détonations, graines
            // de pieux) ne rebondissent plus : comme pour le plomb et le boomerang, et pour que les duos du rebond ne se relancent
            // pas en cascade.
            var bounce = Level(UpgradeEffect.Bounce);
            if (bounce > 0 && !flight.IsChild && flight.Bounces < k_Bounces + k_BouncesPerLevel * (bounce - 1))
            {
                var velocity = Vector3.Reflect(hit.Direction * hit.Speed, hit.Normal) * k_BounceSpeed;

                // Une flèche de la pluie tombe souvent doucement : elle rebondit quand même, assez fort pour qu'on le voie (27
                // octobre, demandé par le joueur : « fais en sorte que le rebond marche pour la pluie »).
                if (flight.IsRain && velocity.sqrMagnitude < k_RainBounceSpeed * k_RainBounceSpeed)
                    velocity = (velocity.sqrMagnitude > 1e-4f ? velocity.normalized : hit.Normal) * k_RainBounceSpeed;

                if (velocity.sqrMagnitude > 4f)
                {
                    flight.Bounces++;
                    hit.Arrow.Bounce(velocity);
                    Sfx.Play(PieceSounds.Whoosh, hit.Point, flight.IsRain ? 0.15f : 0.35f, 1.6f);
                    OnBounce(hit, flight);
                }
            }
        }

        // Un ennemi touché : éclats, marque, bourreau.
        void OnEnemyHit(in ArrowHit hit, Flight flight, Enemy enemy)
        {
            var splinter = Level(UpgradeEffect.Splinter);
            if (splinter > 0 && !flight.IsChild)
                SpawnSplinters(hit, splinter);
            if (Level(UpgradeEffect.HunterMark) > 0 && enemy.IsAlive)
                SetMark(enemy);
            if (Level(UpgradeEffect.Executioner) > 0)
                TryExecute(enemy, hit, hit.Collider.TryGetComponent(out Hitbox hitbox) && hitbox.Zone == HitZone.Head);
        }

        // Pointe éclatée : des éclats en cône, depuis l'autre côté de l'ennemi.
        void SpawnSplinters(in ArrowHit hit, int level)
        {
            var arrows = SpecialArrows.Instance;
            if (arrows == null)
                return;

            var count = k_Splinters + (level - 1);
            var damage = hit.Damage * (k_SplinterDamage + k_SplinterDamagePerLevel * (level - 1));
            var direction = hit.Direction.sqrMagnitude > 1e-4f ? hit.Direction.normalized : hit.Arrow.transform.forward;
            var aim = Quaternion.LookRotation(direction);
            var origin = hit.Point + direction * 0.8f;
            var speed = Mathf.Max(10f, hit.Speed * k_SplinterSpeed);
            m_TrackingChild = true;
            for (var i = 0; i < count; i++)
            {
                var spread = aim * Quaternion.Euler(0f, 0f, i * 360f / count) * Quaternion.Euler(k_SplinterSpread, 0f, 0f) * Vector3.forward;
                arrows.LaunchChild(origin, spread, speed, damage, hit.Grade, hit.Arrow.TrailColor, hit.Arrow.GravityScale);
            }

            m_TrackingChild = false;
        }

        // Bourreau : une tête touchée sous le seuil de PV meurt net, sa tête explose ; un boss aussi, sous un seuil plus bas
        // (26 octobre). Les dégâts de la flèche sont déjà retirés (la zone de touche les compte avant Arrow.AnyHit) : une
        // flèche qui laisse l'ennemi sous le seuil le tue.
        void TryExecute(Enemy enemy, in ArrowHit hit, bool head)
        {
            if (!head || enemy == null || !enemy.IsAlive)
                return;

            var level = Mathf.Max(1, Level(UpgradeEffect.Executioner));
            var health = enemy.Health;
            var thresholds = IsDragon(enemy) ? k_ExecuteDragonThreshold : IsBoss(enemy) ? k_ExecuteBossThreshold : k_ExecuteThreshold;
            var threshold = SpecialArrows.ByLevel(thresholds, level);

            // Le duo « Chasse à l'homme » (26 octobre au soir) : la cible marquée s'exécute sous un seuil plus haut (×1,5 depuis le
            // 27 octobre au soir, doublé avant).
            if (enemy == m_Marked && ArrowDuos.IsActive(ArrowDuos.Manhunt))
                threshold *= k_ManhuntThreshold;
            if (health.Current > health.Max * threshold)
                return;

            Hurt(enemy, health.Current + 1f, hit.Point, hit.Direction, hit, HitZone.Head);
            LightningBolt.Burst(hit.Point, 0.8f, k_ExecuteColor, EffectMaterial, 8);
            var arrows = SpecialArrows.Instance;
            if (arrows != null)
                Sfx.Play(arrows.ExplosionClip, hit.Point, 0.4f, 1.5f);
            FloatingText.Spawn(hit.Point + Vector3.up * 0.4f, "Exécuté !", k_ExecuteColor, 0.7f, 0.9f);
            AreaDamage(hit.Point, hit.Damage * k_ExecuteBlastDamage, k_ExecuteBlastRadius, hit);
        }

        // Des dégâts autour d'un point, moitié moins au bord (comme une explosion, sans son effet).
        void AreaDamage(Vector3 center, float damage, float radius, in ArrowHit hit)
        {
            if (damage <= 0f)
                return;

            m_Enemies.Clear();
            m_Enemies.AddRange(Enemy.Alive);
            foreach (var enemy in m_Enemies)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var point = enemy.Center;
                var distance = Vector3.Distance(point, center);
                if (distance > radius)
                    continue;

                var direction = point - center;
                Hurt(enemy, damage * Mathf.Lerp(1f, 0.5f, distance / radius), point, direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.up, hit);
            }
        }

        // Le duo « Tempête » : un éclair tombe du ciel sur l'ennemi.
        void CallLightning(Enemy enemy, float damage, in ArrowHit hit)
        {
            var center = enemy.Center;
            var arrows = SpecialArrows.Instance;
            var color = arrows != null ? arrows.LightningColor : k_LinkColor;
            LightningBolt.Spawn(center + new Vector3(UnityEngine.Random.Range(-2f, 2f), 16f, UnityEngine.Random.Range(-2f, 2f)), center, color,
                                EffectMaterial, 0.1f, 0.3f);
            if (arrows != null)
                Sfx.Play(arrows.LightningClip, center, 0.6f);
            Hurt(enemy, damage, center, Vector3.down, hit);
            enemy.Slow(0.6f, 1.5f);
        }

        // ---------------------------------------------------------------- pointe de chasseur : la marque

        void SetMark(Enemy enemy)
        {
            if (m_Marked == enemy)
                return;

            m_Marked = enemy;
            if (enemy == null)
            {
                if (m_MarkRing != null)
                {
                    m_MarkRing.gameObject.SetActive(false);
                    m_FreeRings.Push(m_MarkRing);
                    m_MarkRing = null;
                }

                return;
            }

            if (m_MarkRing == null)
                m_MarkRing = TakeRing();
            FloatingText.Spawn(enemy.Center + Vector3.up * 0.9f, "Marqué", k_MarkColor, 0.55f, 0.8f);
        }

        // Un anneau doré tourne au-dessus de la cible marquée.
        void UpdateMark()
        {
            if (m_Marked != null && (Level(UpgradeEffect.HunterMark) == 0 || !m_Marked.IsAlive))
                SetMark(null);
            if (m_Marked == null || m_MarkRing == null)
                return;

            var radius = 0.45f + 0.05f * Mathf.Sin(Time.time * 6f);
            DrawCircle(m_MarkRing, m_Marked.Center + Vector3.up * 1.1f, radius, k_MarkColor, 0.95f);
        }

        // Tout ce qui touche la cible marquée fait plus de dégâts (une seconde fois, sans repasser ici).
        void OnEnemyDamaged(Enemy enemy, DamageInfo info)
        {
            if (m_InMarkBonus || enemy == null || enemy != m_Marked || info.Amount <= 0f)
                return;

            var level = Level(UpgradeEffect.HunterMark);
            if (level == 0)
                return;

            m_InMarkBonus = true;
            try
            {
                var bonus = info;
                bonus.Amount = info.Amount * (k_MarkBonus + k_MarkBonusPerLevel * (level - 1));
                bonus.Source = this;
                enemy.Health.TakeDamage(bonus);
            }
            finally
            {
                m_InMarkBonus = false;
            }
        }

        // À sa mort, la marque saute sur l'ennemi le plus proche.
        void OnEnemyKilled(Enemy enemy, DamageInfo info)
        {
            if (enemy == null || enemy != m_Marked)
                return;

            m_Exclude.Clear();
            m_Exclude.Add(enemy);
            var next = SpecialArrows.NearestEnemy(enemy.Center, k_MarkJump, m_Exclude);
            m_Marked = null;
            if (next != null)
                SetMark(next);
            else
                SetMark(null);
        }

        // ---------------------------------------------------------------- corde du faucon, œil de faucon

        void UpdateFalcon()
        {
            var level = Level(UpgradeEffect.Falcon);
            if (m_Bow == null && level > 0)
                m_Bow = FindAnyObjectByType<Bow>();
            var bow = level > 0 ? m_Bow : null;
            var now = Time.unscaledTime;

            // Une fois par tension : il se réarme quand la corde est relâchée (ou presque).
            if (bow == null || !bow.HasArrow || bow.DrawRatio < k_FalconRearm)
                m_FalconArmed = true;

            if (m_FalconActive)
            {
                if (now >= m_FalconUntil || bow == null || !bow.HasArrow || SlowMotion.IsPaused)
                {
                    m_FalconActive = false;
                    SlowMotion.End(m_FalconToken);
                    Sfx.Play(PieceSounds.SlowOut, ReturnPoint(), 0.5f, 1f, 0f);
                }
            }
            else if (bow != null && bow.HasArrow && bow.DrawRatio >= k_FalconDraw && m_FalconArmed && !SlowMotion.IsPaused)
            {
                m_FalconActive = true;
                m_FalconArmed = false;
                m_FalconUntil = now + k_FalconTime + k_FalconTimePerLevel * (level - 1);
                SlowMotion.Begin(m_FalconToken, Mathf.Max(0.2f, k_FalconScale - k_FalconScalePerLevel * (level - 1)));
                Sfx.Play(PieceSounds.SlowIn, ReturnPoint(), 0.5f, 1.15f, 0f);
            }

            UpdateHawkMarks(m_FalconActive && ArrowDuos.IsActive(ArrowDuos.HawkEye));
        }

        // Le duo « Œil de faucon » : le cadre doré des points faibles des boss (le sceau du thème), plus brillant, sur le point
        // faible (sinon la tête) de chaque ennemi proche, posé entre lui et les yeux, à 2,5 m : rien ne le cache, et sa taille dit
        // la distance (27 octobre, demandé par le joueur : avant, un petit losange doré). Tant que le duo est formé, les points
        // faibles prennent aussi 1,5 fois plus de dégâts (Hitbox.WeakPointDamageMultiplier).
        void UpdateHawkMarks(bool show)
        {
            Hitbox.WeakPointDamageMultiplier = ArrowDuos.IsActive(ArrowDuos.HawkEye) ? k_HawkWeakPointDamage : 1f;
            var count = 0;
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (show && head != null)
            {
                var eyes = head.position;
                foreach (var enemy in Enemy.Alive)
                {
                    if (count >= k_MaxHawkMarks)
                        break;
                    if (enemy == null || !enemy.IsAlive)
                        continue;

                    var offset = WeakPointOf(enemy) - eyes;
                    var distance = offset.magnitude;
                    if (distance > k_HawkRange || distance < k_HawkMarkDistance + 0.5f)
                        continue;

                    var mark = HawkMark(count++);
                    mark.SetPositionAndRotation(eyes + offset / distance * k_HawkMarkDistance,
                                                Quaternion.LookRotation(offset) * Quaternion.Euler(0f, 0f, Time.unscaledTime * 40f));
                    mark.localScale = Vector3.one * Mathf.Max(k_HawkMinScale, k_HawkMarkDistance / distance);
                    if (!mark.gameObject.activeSelf)
                        mark.gameObject.SetActive(true);
                }
            }

            for (var i = count; i < m_HawkMarks.Count; i++)
            {
                if (m_HawkMarks[i] != null && m_HawkMarks[i].gameObject.activeSelf)
                    m_HawkMarks[i].gameObject.SetActive(false);
            }
        }

        Vector3 WeakPointOf(Enemy enemy)
        {
            if (!m_WeakPoints.TryGetValue(enemy, out var collider) || collider == null)
            {
                if (m_WeakPoints.Count > 64)
                    m_WeakPoints.Clear();

                collider = null;
                foreach (var hitbox in enemy.GetComponentsInChildren<Hitbox>())
                {
                    if (hitbox == null || !hitbox.TryGetComponent<Collider>(out var candidate))
                        continue;
                    if (hitbox.Zone == HitZone.WeakPoint || (collider == null && hitbox.Zone == HitZone.Head))
                        collider = candidate;
                }

                m_WeakPoints[enemy] = collider;
            }

            return collider != null && collider.enabled ? collider.bounds.center : enemy.Center;
        }

        // Une marque : le sceau du thème (un halo plus grand et pâle derrière, le cadre brillant devant) ; sans thème, le losange
        // d'avant.
        Transform HawkMark(int index)
        {
            while (m_HawkMarks.Count <= index)
            {
                var mark = new GameObject("Œil de faucon").transform;
                mark.SetParent(transform, false);
                var theme = UiTheme.Load();
                if (theme != null)
                {
                    var glow = theme.panelBorderColor;
                    glow.a = 0.35f;
                    UiThemer.Seal(mark, "Halo", theme, k_HawkMarkSize * 1.35f, Vector3.zero, glow);
                    UiThemer.Seal(mark, "Cadre", theme, k_HawkMarkSize, Vector3.zero, Color.Lerp(theme.panelBorderColor, Color.white, 0.55f));
                }
                else
                {
                    mark.gameObject.AddComponent<MeshFilter>().sharedMesh = PieceLook.Diamond("Œil de faucon", k_HawkMarkSize * 0.5f, k_HawkMarkSize * 0.4f, 4);
                    var renderer = mark.gameObject.AddComponent<MeshRenderer>();
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    if (m_HawkMaterial == null && EffectMaterial != null)
                    {
                        m_HawkMaterial = new Material(EffectMaterial) { name = "Œil de faucon" };
                        m_HawkMaterial.SetColor("_BaseColor", new Color(1.6f, 1.25f, 0.4f, 0.95f));
                    }

                    renderer.sharedMaterial = m_HawkMaterial;
                }

                mark.gameObject.SetActive(false);
                m_HawkMarks.Add(mark);
            }

            return m_HawkMarks[index];
        }

        // ---------------------------------------------------------------- ingénieur, duos

        // Une baliste ou une tour de givre surchargée par l'ingénieur (une flèche qui la touche ou traverse son volume).
        void OverchargeBuilding(Structure structure, int level, Vector3 point)
        {
            structure.Overcharge(k_OverchargeTime + k_OverchargeTimePerLevel * (level - 1), SpecialArrows.ByLevel(k_OverchargeRate, level));
            if (!m_Overcharged.Contains(structure))
                m_Overcharged.Add(structure);
            FloatingText.Spawn(point + Vector3.up * 0.4f, "Surchargée !", k_OverchargeColor, 0.6f, 0.9f);
            var special = SpecialArrows.Instance;
            if (special != null)
                Sfx.Play(special.LightningClip, point, 0.7f, 1.3f);
        }

        // Ingénieur (26 octobre) : une flèche qui traverse une barricade debout la répare un peu (une fois par flèche). Les flèches
        // passent à travers les planches sans les toucher : on regarde le trajet de la pointe depuis le pas d'avant. Depuis le 27
        // octobre (demandé par le joueur : plus d'effet réservé aux tirs parfaits), toute flèche répare ; avant, seul un tir
        // parfait réparait.
        void TryRepairBarricade(Flight flight, int level)
        {
            if (flight.Repaired || flight.IsChild)
                return;

            var tip = flight.Arrow.TipPosition;
            foreach (var barricade in Barricade.All)
            {
                if (barricade == null || !barricade.Intersects(flight.PreviousTip, tip))
                    continue;

                flight.Repaired = true;
                var repaired = barricade.Repair(k_BarricadeRepair + k_BarricadeRepairPerLevel * (level - 1));
                if (repaired > 0f)
                    FloatingText.Spawn(tip + Vector3.up * 0.3f, $"Réparée +{repaired:0} PV", k_OverchargeColor, 0.6f, 0.9f);
                return;
            }
        }

        // Les bâtiments surchargés crépitent.
        void UpdateOvercharged()
        {
            if (m_Overcharged.Count == 0)
                return;

            m_Overcharged.RemoveAll(structure => structure == null || !structure.IsOvercharged);
            if (Time.time < m_NextOverchargeSpark)
                return;

            m_NextOverchargeSpark = Time.time + 0.25f;
            foreach (var structure in m_Overcharged)
                LightningBolt.Burst(structure.transform.position + Vector3.up * 2f, 0.7f, k_OverchargeColor, EffectMaterial, 3);
        }

        // Un duo vient de se former : il s'annonce (pas pendant les épreuves, où les améliorations sont mises de côté).
        void UpdateDuos()
        {
            var upgrades = PlayerUpgrades.Instance;
            if (upgrades == null || upgrades.Suspended)
                return;

            var mask = 0L;
            for (var i = 0; i < ArrowDuos.All.Count; i++)
            {
                if (ArrowDuos.IsActive(ArrowDuos.All[i]))
                    mask |= 1L << i;
            }

            if (mask == m_ActiveDuos)
                return;

            var formed = m_DuosReady ? mask & ~m_ActiveDuos : 0L;
            m_ActiveDuos = mask;
            m_DuosReady = true;
            for (var i = 0; i < ArrowDuos.All.Count; i++)
            {
                if ((formed & (1L << i)) == 0)
                    continue;

                var duo = ArrowDuos.All[i];
                HeadMessages.Show($"Duo : {duo.Name} !\n<size=60%>{duo.Description}</size>", k_DuoColor, 4.5f);
                Sfx.Play(PieceSounds.Duo, ReturnPoint(), 0.8f, 1f, 0f);
            }
        }

        // ---------------------------------------------------------------- les failles du katana

        /// <summary>
        /// Une faille du katana touche un ennemi (MatrixRift) : les pièces de pointe qui ont un sens pour elle (givre, chasseur,
        /// bourreau) ; et à son premier ennemi touché, la mèche et l'aimant, là où elle a frappé.
        /// </summary>
        public void OnRiftHit(Enemy enemy, in ArrowHit hit, HitZone zone, bool firstImpact)
        {
            if (enemy == null)
                return;

            if (Level(UpgradeEffect.HunterMark) > 0 && enemy.IsAlive)
                SetMark(enemy);
            if (Level(UpgradeEffect.Executioner) > 0)
                TryExecute(enemy, hit, zone == HitZone.Head);
            FrostHit(enemy, hit);
            if (!firstImpact)
                return;

            var magnet = Level(UpgradeEffect.Magnet);
            var radius = k_MagnetRadius + k_MagnetRadiusPerLevel * (Mathf.Max(1, magnet) - 1);
            if (ArrowDuos.IsActive(ArrowDuos.BlackHole))
            {
                // Le trou noir naît là où la faille a frappé.
                MakeRoomForFuse();
                AddMagnetAt(hit.Point, null, k_BlackHoleTime, radius, k_BlackHolePull, hit.Damage, hit.Grade);
                m_Fuses.Add(new Fuse
                {
                    Point = hit.Point,
                    ExplodeAt = Time.time + k_BlackHoleTime,
                    Damage = hit.Damage,
                    Grade = hit.Grade,
                    IsBlackHole = true,
                    Hole = BlackHole.Spawn(hit.Point, k_BlackHoleTime, EffectMaterial, transform),
                });
                return;
            }

            if (Level(UpgradeEffect.Fuse) > 0)
            {
                MakeRoomForFuse();
                m_Fuses.Add(new Fuse { Point = hit.Point, ExplodeAt = Time.time + k_FuseDelay, Damage = hit.Damage, Grade = hit.Grade });
            }

            if (magnet > 0)
                AddMagnetAt(hit.Point, null, k_MagnetTime + k_MagnetTimePerLevel * (magnet - 1), radius, 1f, hit.Damage, hit.Grade);
        }
    }
}
