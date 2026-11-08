using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.FinalBoss;
using Archery.Player;
using Archery.UI;
using Archery.Upgrades;
using Archery.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Building
{
    /// <summary>
    /// L'ultime <b>Grand Manège</b> (2 novembre, tard ; <c>Docs/Refonte_TowerDefense.md</c>, 12.44 et 12.51) : le niveau 4 du Gonfleur,
    /// du Colisée ou des Montagnes russes, la famille de la fête foraine. Un carrousel géant de chevaux de bois, perché sur son mât
    /// doré (les ennemis passent dessous), son chapiteau rayé, ses ampoules qui s'allument en chenille, son orgue qui fume.
    /// <list type="bullet">
    /// <item><b>L'éveil</b> : bâti, il sort de terre en tournant comme une boîte à musique ; ses ampoules s'allument, l'orgue joue, le
    /// ciel devient une nuit de fête, le feu d'artifice part du toit, les chevaux se cabrent, et l'onde de confettis renverse tout
    /// autour. Puis le ciel redevient celui de la partie.</item>
    /// <item><b>La ronde</b> (seul, sans arrêt) : toutes les 1,2 s, un ruban doré attrape un ennemi à sa portée et le pose sur le
    /// manège : il tourne parmi les chevaux, pris de tournis.</item>
    /// <item><b>La grande éjection</b> (au 5e coup de la ronde, qu'on voit venir : le manège s'emballe, les ampoules clignotent, une
    /// fusée monte) : tous ceux qui tournent sont éjectés d'un coup, en vrille, sur le groupe le plus serré ; chacun y retombe sous un
    /// bouquet de feu d'artifice.</item>
    /// <item><b>Les chevaux</b> (tes flèches) : chaque flèche tirée qui touche un ennemi à sa portée (ou le sol près de lui), et un de
    /// ses grands chevaux saute du manège et galope dans les airs jusqu'à lui : il le piétine, un bouquet de feu d'artifice éclate
    /// au-dessus, l'onde dorée renverse tout autour. La pluie de flèches et le multishot l'emballent : 5 chevaux par seconde au plus.</item>
    /// <item><b>La charge</b> : toutes les 25 s, avec assez d'ennemis sur la voie : tous les chevaux sautent sur la voie et la
    /// descendent au galop vers la forêt, sous le ciel de fête ; ils piétinent et projettent tout ce qu'ils croisent.</item>
    /// <item><b>Dans l'arène de MK</b> (il suit le joueur, comme les balistes et les tours de givre) : ses chevaux suivent tes flèches
    /// dans MK, son ruban le fouette, et au 5e coup de la ronde, tous ses chevaux le chargent : MK est étourdi.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Le joueur, le 2 novembre, tard : de nos propositions, il prend la ronde (« les ennemis tournent et sont éjectés », 12.44), la
    /// grande éjection pour le grand coup régulier, le feu d'artifice pour signe de chaque coup, et MK étourdi. Fait d'emblée avec tout
    /// ce que le Colosse a appris (<c>Docs/Ultimes_Cahier.md</c>) : les trois couches du rythme (la ronde seule et continue ; la grande
    /// éjection qu'on voit venir ; un cheval par flèche qui touche, 5 par seconde au plus), des coups à l'échelle du modèle (un cheval
    /// lancé est aussi grand que ceux du manège), l'apparition, le ciel passager (<see cref="SkySettings.Fete"/>, pendant l'éveil et
    /// la charge seulement), les boss des vagues blessés comme les autres plus une part de leurs PV, MK avec son effet à lui.
    /// <para>Pour le casque : un cheval lancé est une copie de l'un des siens, fondue en un seul maillage par matériau (faite une fois,
    /// puis réutilisée) ; le feu d'artifice, les paillettes, les étincelles et la poussière viennent de systèmes de particules à lui,
    /// déplacés là où ça frappe ; les ampoules sont en trois groupes, trois matériaux animés (la chenille), sans rien de plus par
    /// ampoule ; le coup complet (la lumière, le grand coup, les manettes) au plus toutes les 0,5 s, l'éclair du ciel toutes les 1,2 s.</para>
    /// <para>Le modèle (<c>Tools/allure_tours/ultimes.py</c>, « Grand Manège ») : z vers l'avant, x en travers ; le plateau tourne
    /// (m_Rotor), ses chevaux sur leurs barres dorées montent et descendent. Les cotes du plateau sont ici en constantes
    /// (k_DeckHeight, k_SeatRadius, k_RimRadius…) : changer l'un, c'est changer l'autre.</para>
    /// </remarks>
    public class UltimeManege : TowerUltimate
    {
        // Les cotes du modèle (repère du manège, avant sa taille) : le dessus du plateau, où sont posés ceux qui tournent, et leur
        // rayon (entre les grands chevaux) ; le bas du chapiteau, d'où part le ruban ; les sabots d'un cheval sous son repère.
        const float k_DeckHeight = 4.0f;
        const float k_SeatRadius = 6.0f;
        const float k_RimRadius = 7.2f;
        const float k_RimHeight = 11.0f;
        const float k_HoofDrop = 1.35f;

        // Il tourne (°/s) : au repos ; de plus par flèche qui attend son cheval (l'emballement), au plus ; pendant la grande
        // éjection et la charge.
        const float k_Spin = 22f;
        const float k_ArrowSpin = 14f;
        const float k_MaxArrowSpin = 70f;
        const float k_BoostSpin = 140f;

        // Les chevaux montent et descendent sur leurs barres (m, à 1 : la taille du modèle), à … Hz au repos.
        const float k_Bob = 0.45f;
        const float k_BobRate = 0.45f;

        // Le ruban : il va jusqu'à sa cible en … s, l'amène sur le manège en … s, puis se retire.
        const float k_Reach = 0.22f;
        const float k_Pull = 0.6f;
        const float k_Retract = 0.18f;

        // Une flèche attend son cheval au plus … s (sa cible est loin, ou morte).
        const float k_OrderLife = 2f;

        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Le modèle")]
        [Tooltip("Le manège qui tourne (repère au pied du mât) : le plateau, ses chevaux, le mât, l'orgue, le chapiteau.")]
        [SerializeField]
        Transform m_Rotor;

        [Tooltip("Les grands chevaux (repère au milieu de la selle, sur leur barre, tournés dans le sens du manège) : ils montent et " +
                 "descendent ; ceux qu'il lance en sont des copies.")]
        [SerializeField]
        Transform[] m_Horses = System.Array.Empty<Transform>();

        [Tooltip("Les petits chevaux du milieu : ils montent et descendent ; dans l'arène, des copies tournent autour de la tête de MK " +
                 "étourdi.")]
        [SerializeField]
        Transform[] m_Foals = System.Array.Empty<Transform>();

        [Tooltip("Le sommet du chapiteau : les fusées en partent.")]
        [SerializeField]
        Transform m_Crown;

        [Tooltip("Le haut des tuyaux de l'orgue : ils fument.")]
        [SerializeField]
        Transform[] m_Pipes = System.Array.Empty<Transform>();

        [Tooltip("Le matériau des ampoules (Ampoule_de_manege) : il est copié en trois en jeu (la chenille : « Ampoule A », « B », « C »).")]
        [SerializeField]
        Material m_BulbMaterial;

        [Tooltip("L'onde des coups : une lumière transparente (Rayon_du_jugement), copiée et teinte en jeu.")]
        [SerializeField]
        Material m_RingMaterial;

        [Tooltip("Les éclats qui volent à l'éveil.")]
        [SerializeField]
        Material m_DebrisMaterial;

        [Header("L'éveil")]
        [Tooltip("Il sort de terre en tournant : de cette profondeur (m), en … s.")]
        [SerializeField]
        float m_WakeDepth = 17f;

        [SerializeField]
        float m_RiseTime = 2.6f;

        [Tooltip("L'onde de confettis à la fin de l'éveil : elle renverse les ennemis à … m et leur fait … de dégâts.")]
        [SerializeField]
        float m_WakeRadius = 22f;

        [SerializeField]
        float m_WakeDamage = 100f;

        [Header("La portée")]
        [Tooltip("Portée (m, à plat, autour du mât) : ta flèche doit toucher plus près ; le ruban y cherche ses cibles.")]
        [SerializeField]
        float m_Range = 30f;

        [Header("La ronde (seul)")]
        [Tooltip("Un coup de la ronde toutes les … s : le ruban attrape un ennemi au sol à sa portée, le plus avancé vers ta tour, et le " +
                 "pose sur le manège (les parchemins d'adresse, la surcharge l'accélèrent).")]
        [SerializeField]
        float m_RoundInterval = 1.2f;

        [Tooltip("Le ruban qui l'attrape : dégâts (vague 1, en Normal) ; un boss ou un volant n'est jamais attrapé : le ruban le fouette.")]
        [SerializeField]
        float m_CatchDamage = 60f;

        [Tooltip("Le tournis : dégâts par seconde à ceux qui tournent sur le manège. 70 (au tableau d'équilibrage, 12.51 : seule, la " +
                 "ronde vaut un trio moyen au niveau 3).")]
        [SerializeField]
        float m_RideDamage = 70f;

        [Tooltip("Places sur le manège (entre les grands chevaux) : pleines, le ruban attend la grande éjection.")]
        [SerializeField]
        int m_Seats = 8;

        [Header("La grande éjection (au 5e coup)")]
        [Tooltip("Au … e coup de la ronde, ceux qui tournent sont éjectés d'un coup (sans personne sur le manège, il attend le premier).")]
        [SerializeField]
        int m_RoundsPerEjection = 5;

        [Tooltip("Le temps qu'on la voie venir (s) : le manège s'emballe, les ampoules clignotent, l'orgue sonne la fanfare, une fusée monte.")]
        [SerializeField]
        float m_EjectWindup = 0.9f;

        [Tooltip("Chaque éjecté retombe sur le groupe le plus serré : dégâts autour de lui (au centre ; moitié au bord), rayon (m) ; ceux " +
                 "qui survivent sont projetés en vrille, loin (m/s), vers le haut (m/s).")]
        [SerializeField]
        float m_EjectDamage = 100f;

        [SerializeField]
        float m_EjectRadius = 5f;

        [SerializeField]
        float m_EjectPush = 8f;

        [SerializeField]
        float m_EjectLift = 11f;

        [Tooltip("Ce que l'éjecté prend lui-même en retombant (en plus de sa chute).")]
        [SerializeField]
        float m_EjectSelfDamage = 200f;

        [Tooltip("Un boss des vagues sous un éjecté prend ses dégâts, et perd en plus cette part de ses PV max (il n'est jamais projeté). " +
                 "MK, dans l'arène : la part de sa charge (Daze Boss Share).")]
        [SerializeField]
        float m_BossShare = 0.01f;

        [Header("Les chevaux (tes flèches)")]
        [Tooltip("Au plus un cheval toutes les … s (0,2 : 5 par seconde, le plafond du joueur pour le casque).")]
        [SerializeField]
        float m_GallopInterval = 0.2f;

        [Tooltip("Les flèches qui attendent leur cheval, au plus : les plus anciennes s'effacent (il suit tes derniers tirs).")]
        [SerializeField]
        int m_QueueSize = 8;

        [Tooltip("Une flèche dans le sol : le cheval va sur l'ennemi au sol le plus proche, à … m au plus ; sinon, rien.")]
        [SerializeField]
        float m_SeekRadius = 5f;

        [Tooltip("Le piétinement : dégâts au centre (vague 1, en Normal ; moitié au bord), rayon (m) ; ceux qui survivent sont projetés " +
                 "en vrille, loin (m/s), vers le haut (m/s). Ceux des marteaux du Colosse (le joueur les a validés).")]
        [SerializeField]
        float m_HorseDamage = 160f;

        [SerializeField]
        float m_HorseRadius = 6f;

        [SerializeField]
        float m_HorsePush = 8f;

        [SerializeField]
        float m_HorseLift = 12f;

        [Tooltip("La taille d'un cheval lancé (fois celle des siens : 1, aussi grand qu'eux).")]
        [SerializeField]
        float m_HorseScale = 1f;

        [Tooltip("Ce qu'un boss perd en plus sous un cheval (part de ses PV max) : à 5 par seconde, 1 % par seconde au plus. MK, dans " +
                 "l'arène : cette part seulement.")]
        [SerializeField]
        float m_HorseBossShare = 0.002f;

        [Tooltip("Le coup complet (la lumière, le grand coup, les manettes) au plus toutes les … s ; entre deux, chaque cheval a son " +
                 "bouquet, son onde, ses confettis. Un éclair de couleur dans le ciel, au plus toutes les … s (pas un ciel qui clignote).")]
        [SerializeField]
        float m_HeavyInterval = 0.5f;

        [SerializeField]
        float m_SkyFlashInterval = 1.2f;

        [Tooltip("Ce qu'un boss perd sous le ruban (part de ses PV max).")]
        [SerializeField]
        float m_SmallBossShare = 0.001f;

        [Header("Le feu d'artifice")]
        [Tooltip("Le bouquet éclate à … m au-dessus du coup (à 1 : la taille du modèle).")]
        [SerializeField]
        float m_FireworkHeight = 11f;

        [Tooltip("La force de l'éclair dans le ciel aux grands moments (0 à 1).")]
        [SerializeField]
        float m_StrikeFlash = 0.35f;

        [Tooltip("Les couleurs des bouquets (au hasard).")]
        [SerializeField]
        Color[] m_FireworkColors =
        {
            new Color(1f, 0.25f, 0.35f), new Color(1f, 0.8f, 0.25f), new Color(0.35f, 0.7f, 1f), new Color(0.45f, 1f, 0.45f),
            new Color(0.85f, 0.4f, 1f), new Color(1f, 0.55f, 0.15f),
        };

        [Header("La charge")]
        [Tooltip("Temps (s) entre deux charges ; il faut au moins Charge Min Enemies ennemis au sol sur la voie, entre lui et la forêt.")]
        [SerializeField]
        float m_ChargeInterval = 25f;

        [SerializeField]
        int m_ChargeMinEnemies = 6;

        [Tooltip("Les chevaux descendent la voie vers la forêt : sur … m, à … m/s, sur … m de large.")]
        [SerializeField]
        float m_ChargeLength = 70f;

        [SerializeField]
        float m_ChargeSpeed = 12f;

        [SerializeField]
        float m_ChargeWidth = 7f;

        [Tooltip("Chaque ennemi qu'ils croisent : ces dégâts, puis projeté vers la forêt (m/s), en l'air.")]
        [SerializeField]
        float m_ChargeDamage = 90f;

        [SerializeField]
        float m_ChargePush = 12f;

        [Header("Le ciel de fête")]
        [Tooltip("Sa force (0 à 1) pendant la charge (l'éveil : 1) ; le reste du temps, le ciel est celui de la partie.")]
        [SerializeField]
        float m_SkyCharge = 0.85f;

        [Header("Dans l'arène de MK")]
        [Tooltip("Sa taille dans l'arène (fois la sienne) : la voûte de la grotte est basse près des parois (22,5 m, le plateau à 5 m).")]
        [SerializeField]
        float m_ArenaScale = 0.65f;

        [Tooltip("La charge étourdit MK pendant … s, au plus toutes les … s (entre deux, la ronde attend).")]
        [SerializeField]
        float m_DazeTime = 3f;

        [SerializeField]
        float m_DazeInterval = 12f;

        [Tooltip("Ce que MK perd sous la charge (part de ses PV max).")]
        [SerializeField]
        float m_DazeBossShare = 0.03f;

        [Header("Sons")]
        [Tooltip("L'orgue du manège, en boucle, doux : il suit la vitesse du manège (plus aigu quand il s'emballe).")]
        [SerializeField]
        AudioClip m_OrganClip;

        [Tooltip("La fanfare : l'éveil, la charge.")]
        [SerializeField]
        AudioClip m_FanfareClip;

        [Tooltip("La fanfare courte (sa première phrase) : la grande éjection, qui revient toutes les 7 s environ (pas par-dessus la " +
                 "fanfare entière).")]
        [SerializeField]
        AudioClip m_FlourishClip;

        [Tooltip("Le galop, en boucle : la charge.")]
        [SerializeField]
        AudioClip m_GallopClip;

        [Tooltip("Le hennissement : les chevaux se cabrent (l'éveil, la charge).")]
        [SerializeField]
        AudioClip m_NeighClip;

        [Tooltip("La fusée qui monte (le sifflement).")]
        [SerializeField]
        AudioClip m_RocketClip;

        [Tooltip("Le bouquet qui éclate : le signe de chaque coup.")]
        [SerializeField]
        AudioClip m_BurstClip;

        [Tooltip("Le sabot qui frappe (le bois) : chaque cheval qui piétine, chaque éjecté qui retombe.")]
        [SerializeField]
        AudioClip m_StompClip;

        [Tooltip("Le ruban qui claque.")]
        [SerializeField]
        AudioClip m_CatchClip;

        [Tooltip("Le souffle : un cheval qui saute, les éjectés qui partent.")]
        [SerializeField]
        AudioClip m_WhooshClip;

        [Tooltip("Le grand impact : la fin de l'éveil, le grand coup des chevaux.")]
        [SerializeField]
        AudioClip m_BoomClip;

        // Ce qu'il lance (un cheval) : réutilisé d'un lancer à l'autre. Un cheval reste planté un instant après son coup (LandedAt),
        // cabré, puis s'efface dans une gerbe de paillettes.
        class Shot
        {
            public GameObject Go;
            public TrailRenderer Trail;
            public Transform Source;
            public Vector3 Scale;
            public bool Flying;
            public float LandedAt = -1f;
            public bool InUse => Flying || LandedAt >= 0f;
            public Vector3 From;
            public Vector3 Point;
            public Enemy Enemy;
            public DragonBoss Dragon;
            public bool Air;
            public bool Charge;
            public float Born;
            public float Duration;
            public float Arc;
            public float Rock;
            public float Power = 1f;
        }

        // Une flèche qui attend son cheval : sa cible (un ennemi suivi, ou MK), où elle a touché, sa force.
        struct Order
        {
            public Enemy Enemy;
            public DragonBoss Dragon;
            public Vector3 Point;
            public float At;
            public float Power;
        }

        // Le ruban en train d'attraper (ou de fouetter) sa cible, puis de se retirer (RetractAt).
        class Catch
        {
            public Enemy Enemy;
            public DragonBoss Dragon;
            public float At;
            public bool Grabbed;
            public bool Held;
            public int Seat;
            public Vector3 Start;
            public Vector3 Tip;
            public float RetractAt = -1f;
        }

        // Un ennemi qui tourne sur le manège, à sa place.
        class Rider
        {
            public Enemy Enemy;
            public int Seat;
            public float Yaw;
            public float NextHurt;
        }

        // Un éjecté en vol, et où il doit retomber.
        class Ejected
        {
            public Enemy Enemy;
            public Vector3 Landing;
            public Vector3 Last;
            public float Until;
        }

        // Un cheval de la charge, à sa place dans la troupe (en travers de la voie, et son rang).
        class Charger
        {
            public GameObject Go;
            public Transform Source;
            public Vector3 Scale;
            public float Across;
            public float Behind;
            public float Phase;
        }

        class Rocket
        {
            public Transform Body;
            public TrailRenderer Trail;
            public bool Active;
            public Vector3 From;
            public Vector3 To;
            public float Born;
            public float Duration;
            public Color Color;
            public float Size;
        }

        class Debris
        {
            public Transform Body;
            public Vector3 Velocity;
            public Vector3 Spin;
            public float Until;
        }

        class Ring
        {
            public Transform Body;
            public Material Material;
            public Color Color;
            public float Born;
            public float Duration;
            public float Radius;
        }

        class FlashLight
        {
            public Light Light;
            public float Born;
            public float Duration;
            public float Intensity;
        }

        static readonly Color k_Gold = new Color(1f, 0.82f, 0.35f, 1f);
        static readonly Color k_Glitter = new Color(1f, 0.92f, 0.6f, 1f);
        static readonly Color k_Dust = new Color(0.72f, 0.66f, 0.56f, 1f);
        static readonly Color k_Title = new Color(1f, 0.55f, 0.85f);
        static readonly Color k_Shock = new Color(1f, 0.8f, 0.4f, 0.85f);
        static readonly Color k_BulbWarm = new Color(1f, 0.82f, 0.5f, 1f);

        static Mesh s_RingMesh;
        static Material s_TrailMaterial;

        readonly List<Enemy> m_Scratch = new List<Enemy>();
        readonly List<Rider> m_Riders = new List<Rider>();
        readonly List<Ejected> m_Ejected = new List<Ejected>();
        readonly List<Shot> m_Shots = new List<Shot>();
        readonly List<Order> m_Orders = new List<Order>();
        readonly List<Charger> m_Chargers = new List<Charger>();
        readonly List<Rocket> m_Rockets = new List<Rocket>();
        readonly List<Debris> m_Debris = new List<Debris>();
        readonly List<Ring> m_Rings = new List<Ring>();
        readonly List<FlashLight> m_Flashes = new List<FlashLight>();
        readonly List<ParticleSystem> m_Loops = new List<ParticleSystem>();
        readonly HashSet<Enemy> m_ChargeHit = new HashSet<Enemy>();
        readonly Dictionary<Transform, (Mesh Mesh, Material[] Materials)> m_Copies = new Dictionary<Transform, (Mesh, Material[])>();
        readonly List<Mesh> m_Fused = new List<Mesh>();
        readonly List<GameObject> m_Dizzy = new List<GameObject>();
        readonly Material[] m_Bulbs = new Material[3];
        readonly List<Renderer>[] m_BulbRenderers = { new List<Renderer>(), new List<Renderer>(), new List<Renderer>() };
        readonly bool[] m_SeatTaken = new bool[16];

        SkySettings m_Sky;
        float m_SkyWeight;
        ParticleSystem m_FireworkFx;
        ParticleSystem m_SparkFx;
        ParticleSystem m_DustFx;
        ParticleSystem m_ConfettiFx;
        Light m_CanopyLight;
        AudioSource m_OrganAudio;
        AudioSource m_GallopAudio;
        LineRenderer m_Ribbon;

        Quaternion m_RotorRest = Quaternion.identity;
        Pose[] m_HorseRest = System.Array.Empty<Pose>();
        Pose[] m_FoalRest = System.Array.Empty<Pose>();
        float m_RotorAngle;
        float m_SpinSpeed = k_Spin;
        float m_BobPhase;
        float m_SpinBoostUntil = -1f;
        float m_Rear;
        float m_RearUntil = -1f;

        // L'éveil (−1 : fini, ou pas commencé) ; l'éclat des ampoules (0 : éteintes), leurs sursauts, la chenille.
        float m_WakeAt = -1f;
        bool m_Awake;
        int m_WakeStep;
        float m_NextWakeFx;
        Vector3 m_Ground;
        float m_Glow;
        float m_GlowFlare;
        float m_Chase;

        // La ronde : le prochain coup, les coups depuis la dernière grande éjection, le ruban en cours, la grande éjection qui vient.
        float m_NextRound;
        int m_Rounds;
        Catch m_Catch;
        float m_EjectAt = -1f;

        float m_LastGallop = -100f;
        float m_FanfareUntil;
        float m_NextHeavy;
        float m_NextSkyFlash;

        // La charge : le départ, ce qu'elle a parcouru (m ; −1 : rien), sa voie, ses prochains effets.
        MapScan.Lane m_ChargeLane;
        float m_ChargeStart;
        float m_ChargeAt = -1f;
        float m_ChargeLeap = -1f;
        float m_NextCharge;
        float m_NextChargeFx;
        float m_NextChargeFirework;
        Vector3 m_ChargeSoundAt;

        // Dans l'arène : la prochaine charge sur MK, ses étoiles.
        float m_NextDaze;
        float m_DazedUntil = -1f;
        DragonBoss m_DazedDragon;
        bool m_ArenaHint;

        protected override float ArenaScale => m_ArenaScale;

        float Size => Mathf.Max(0.3f, transform.lossyScale.y);

        Vector3 CrownPoint => m_Crown != null ? m_Crown.position : transform.position + Vector3.up * 17f * Size;

        Vector3 Hub => transform.position;

        // La fanfare courte ; sans elle, la fanfare.
        AudioClip Flourish => m_FlourishClip != null ? m_FlourishClip : m_FanfareClip;

        // La fanfare entière (l'éveil, la charge) : la courte ne joue pas par-dessus.
        void PlayFanfare()
        {
            Sfx.Play(m_FanfareClip, CrownPoint, 1f, 1f);
            if (m_FanfareClip != null)
                m_FanfareUntil = Time.time + m_FanfareClip.length;
        }

        protected override void Awake()
        {
            base.Awake();
            if (m_Rotor != null)
                m_RotorRest = m_Rotor.localRotation;
            m_HorseRest = RestOf(m_Horses);
            m_FoalRest = RestOf(m_Foals);
            m_Sky = SkySettings.Fete();
            FuseModel();
        }

        // Pour le casque : ses centaines de pièces (des formes simples) fondues au lancement, avant le plan bleu. Chaque cheval en un
        // maillage (il monte et descend seul ; ceux qu'il lance en sont des copies) ; le reste du manège, qui tourne d'un bloc, en un
        // maillage par matériau, ses ampoules à part, par groupe de la chenille (« Ampoule A », « B », « C »).
        void FuseModel()
        {
            if (m_Rotor == null)
                return;

            var moving = new HashSet<Transform>();
            foreach (var part in m_Horses)
            {
                if (part != null && moving.Add(part))
                    FuseInto(part, part.GetComponentsInChildren<MeshFilter>(true), part.name);
            }

            foreach (var part in m_Foals)
            {
                if (part != null && moving.Add(part))
                    FuseInto(part, part.GetComponentsInChildren<MeshFilter>(true), part.name);
            }

            var groups = new Dictionary<string, List<MeshFilter>>();
            foreach (var filter in m_Rotor.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.GetComponent<MeshRenderer>() == null || UnderAny(filter.transform, moving))
                    continue;
                var name = filter.gameObject.name;
                var key = name.StartsWith("Ampoule ") && name.Length >= 9 ? name.Substring(0, 9) : "Manège fondu";
                if (!groups.TryGetValue(key, out var list))
                    groups[key] = list = new List<MeshFilter>();
                list.Add(filter);
            }

            foreach (var group in groups)
            {
                var go = new GameObject(group.Key);
                go.transform.SetParent(m_Rotor, false);
                FuseInto(go.transform, group.Value, group.Key);
            }
        }

        static bool UnderAny(Transform t, HashSet<Transform> parents)
        {
            for (var p = t; p != null; p = p.parent)
            {
                if (parents.Contains(p))
                    return true;
            }

            return false;
        }

        // Ces maillages, fondus en un seul sur cette pièce (un sous-maillage par matériau) ; les pièces d'avant s'éteignent et partent.
        void FuseInto(Transform target, IList<MeshFilter> filters, string name)
        {
            var (mesh, materials) = Combine(filters, target.worldToLocalMatrix, name);
            if (mesh == null)
                return;

            foreach (var filter in filters)
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                    Destroy(renderer);
                }

                Destroy(filter);
            }

            target.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            target.gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
            m_Fused.Add(mesh);
        }

        static Pose[] RestOf(Transform[] parts)
        {
            var rest = new Pose[parts.Length];
            for (var i = 0; i < parts.Length; i++)
                rest[i] = parts[i] != null ? new Pose(parts[i].localPosition, parts[i].localRotation) : Pose.identity;
            return rest;
        }

        void OnEnable()
        {
            Arrow.AnyHit += OnArrowHit;

            // Dans l'arène, au début du combat (ArenaTurrets le rallume) : ce qu'il sait faire contre MK.
            if (InArena && IsBuilt && !m_ArenaHint)
            {
                m_ArenaHint = true;
                HeadMessages.Show("Le Grand Manège est là !\n<size=65%>Touche MK : ses chevaux suivent tes flèches ; au 5e tour, ils le " +
                                  "chargent et l'étourdissent</size>", k_Title, 5f);
            }
        }

        void OnDisable()
        {
            Arrow.AnyHit -= OnArrowHit;
            ReleaseSky();
        }

        protected override void OnDestroy()
        {
            StopAll();
            ClearEffects();
            ReleaseSky();
            foreach (var material in m_Bulbs)
            {
                if (material != null)
                    Destroy(material);
            }

            foreach (var copy in m_Copies.Values)
            {
                if (copy.Mesh != null)
                    Destroy(copy.Mesh);
            }

            m_Copies.Clear();
            foreach (var mesh in m_Fused)
            {
                if (mesh != null)
                    Destroy(mesh);
            }

            m_Fused.Clear();
            base.OnDestroy();
        }

        protected override void OnLevelChanged(int level)
        {
            base.OnLevelChanged(level);
            if (level >= 1 && !m_Awake && m_WakeAt < 0f)
                BeginWake();
        }

        public override void EnterArena()
        {
            StopAll();
            DestroyShots();
            DestroyChargers();
            FinishWake();
            base.EnterArena();
            m_NextDaze = 0f;
            m_Rounds = 0;
            m_ArenaHint = false;
        }

        public override void LeaveArena()
        {
            StopAll();
            DestroyShots();
            DestroyChargers();
            base.LeaveArena();
        }

        protected override void Update()
        {
            base.Update();
            UpdateEffects();
            UpdateShots();
            UpdateRockets();
            UpdateRiders();
            UpdateEjected();
            UpdateDizzy();
            if (!IsBuilt || IsUnderConstruction)
                return;

            UpdateSky();
            if (m_WakeAt >= 0f)
            {
                UpdateWake();
                Animate();
                UpdateLights();
                return;
            }

            UpdateRound();
            UpdateGallops();
            Animate();
            UpdateLights();
            UpdateOrgan();
            if (InArena)
                return;

            UpdateCharge();
            UpdateGallopSound(m_ChargeAt >= 0f || m_ChargeLeap >= 0f);
            if (m_ChargeAt < 0f && m_ChargeLeap < 0f && Time.time >= m_NextCharge)
                TryCharge();
        }

        // ---------------------------------------------------------------- l'éveil

        // Bâti (au marteau, ou par la vitrine) : il s'enfonce, éteint, et l'éveil commence.
        void BeginWake()
        {
            m_WakeAt = Time.time;
            m_WakeStep = 0;
            m_Awake = false;
            m_Glow = 0f;
            m_SpinSpeed = 180f;
            m_Ground = transform.parent != null ? transform.parent.position : transform.position;
            var local = transform.localPosition;
            transform.localPosition = new Vector3(local.x, -m_WakeDepth, local.z);
            SetupBulbs();
            UpdateLights();

            HeadMessages.Show("Le Grand Manège s'éveille !", k_Title, 4f);
            Sfx.Play(m_BoomClip, m_Ground, 0.7f, 0.6f);
            Shockwave(m_Ground, 10f, 0.8f, k_Shock);
            Rumble(m_Ground, 0.5f);
        }

        // Il sort de terre en tournant, de moins en moins vite ; ses ampoules s'allument, l'orgue joue ; le feu d'artifice part du
        // toit ; les chevaux se cabrent ; l'onde de confettis.
        void UpdateWake()
        {
            var t = Time.time - m_WakeAt;
            var rise = Mathf.Clamp01(t / Mathf.Max(0.1f, m_RiseTime));
            var local = transform.localPosition;
            transform.localPosition = new Vector3(local.x, -m_WakeDepth * (1f - EaseOut(rise)), local.z);
            m_SpinSpeed = Mathf.Lerp(180f, k_Spin * 2f, rise);

            if (rise < 1f && Time.time >= m_NextWakeFx)
            {
                // La terre qui vole, la poussière et les confettis qui jaillissent autour de lui ; le sol tremble.
                m_NextWakeFx = Time.time + 0.14f;
                var around = m_Ground + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(2f, 6.5f);
                ThrowDebris(around, 3, 0.4f);
                Dust(around + Vector3.up * 0.3f, 6);
                Confetti(around + Vector3.up * 0.5f, 30, 7f);
                Rumble(m_Ground, 0.3f);
            }

            if (m_WakeStep == 0 && t >= 0.5f)
            {
                m_WakeStep = 1;
                LaunchRocket(m_Ground + Vector3.up * 1f, m_Ground + new Vector3(Random.Range(-8f, 8f), 26f, Random.Range(-8f, 8f)), 1.3f);
            }

            if (m_WakeStep == 1 && t >= m_RiseTime)
            {
                // Debout : ses ampoules s'allument, l'orgue sonne, ses confettis, ses paillettes et sa vapeur partent.
                m_WakeStep = 2;
                transform.localPosition = new Vector3(local.x, 0f, local.z);
                Dust(m_Ground + Vector3.up * 0.5f, 40);
                Confetti(m_Ground + Vector3.up * 2f, 160, 12f);
                PlayFanfare();
                StartLoops();
                m_GlowFlare = 1.5f;
            }

            m_Glow = t < m_RiseTime ? 0f : Mathf.Clamp01((t - m_RiseTime) / 0.6f);

            if (m_WakeStep == 2 && t >= m_RiseTime + 0.6f)
            {
                // Le feu d'artifice part du toit, en éventail.
                m_WakeStep = 3;
                for (var i = 0; i < 6; i++)
                {
                    var angle = i * 60f + Random.Range(-15f, 15f);
                    var to = m_Ground + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * Random.Range(9f, 16f) * Size +
                             Vector3.up * Random.Range(28f, 36f) * Size;
                    LaunchRocket(CrownPoint, to, 1.5f);
                }
            }

            if (m_WakeStep == 3 && t >= m_RiseTime + 1.4f)
            {
                // Les chevaux se cabrent et hennissent.
                m_WakeStep = 4;
                Rear(0.9f);
                Sfx.Play(m_NeighClip, CrownPoint, 1f, Random.Range(0.95f, 1.05f));
            }

            if (m_WakeStep == 4 && t >= m_RiseTime + 2.1f)
            {
                m_WakeStep = 5;
                WakeBlast();
            }

            if (t >= m_RiseTime + 2.7f)
                FinishWake();
        }

        // L'onde de confettis de la fin de l'éveil : elle renverse tout à la ronde, sous un anneau de bouquets.
        void WakeBlast()
        {
            var point = m_Ground;
            Shockwave(point, m_WakeRadius, 0.8f, k_Shock);
            Confetti(point + Vector3.up * 3f, 260, 16f);
            DustRing(point, 7f, 8);
            for (var i = 0; i < 6; i++)
            {
                var angle = i * Mathf.PI / 3f;
                Firework(point + new Vector3(Mathf.Cos(angle) * 12f, 20f, Mathf.Sin(angle) * 12f) * Size, RandomColor(), 1.3f);
            }

            FlashAt(point + Vector3.up * 6f, k_Gold, 10f, 50f, 0.8f);
            SkyFlash(1f, true);
            Sfx.Play(m_BoomClip, point, 1f, 0.8f);
            Sfx.Play(m_BurstClip, point, 1f, 0.8f);
            Rumble(point, 1f);
            HeadMessages.Show("LE GRAND MANÈGE !", k_Title, 2.5f);

            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(point, m_WakeRadius));
            foreach (var enemy in m_Scratch)
            {
                if (enemy.IsBoss)
                {
                    HurtBoss(enemy, m_WakeDamage, m_BossShare, point);
                    continue;
                }

                if (Hurt(enemy, m_WakeDamage, point, enemy.Center - point) && enemy.CanBeLaunched)
                    KnockDown(enemy, point, 10f, 10f);
            }
        }

        void FinishWake()
        {
            if (m_WakeAt < 0f && m_Awake)
                return;

            m_WakeAt = -1f;
            m_Awake = true;
            m_Glow = 1f;
            var local = transform.localPosition;
            transform.localPosition = new Vector3(local.x, 0f, local.z);
            m_NextRound = Time.time + 0.6f;
            m_Rounds = 0;
            m_NextCharge = Time.time + 10f;
            SetupBulbs();
            StartLoops();
        }

        // ---------------------------------------------------------------- la ronde

        // Un coup de la ronde après l'autre : le ruban attrape un ennemi (au 5e, la grande éjection). Le ruban et la grande éjection
        // ont leur temps : le coup suivant attend qu'ils aient fini.
        void UpdateRound()
        {
            if (m_Catch != null)
                UpdateCatch();

            if (m_EjectAt >= 0f)
            {
                if (Time.time < m_EjectAt)
                    return;
                m_EjectAt = -1f;
                if (InArena)
                    ChargeDragon();
                else
                    Eject();
                m_Rounds = 0;
                m_NextRound = Time.time + NextInterval();
                return;
            }

            if (m_Catch != null || Time.time < m_NextRound)
                return;

            m_NextRound = Time.time + NextInterval();
            if (InArena)
            {
                // Dans l'arène : le ruban fouette MK ; au 5e coup, la charge l'étourdit (s'il peut l'être).
                var dragon = ArenaDragon();
                if (dragon == null)
                    return;
                if (m_Rounds >= m_RoundsPerEjection - 1 && Time.time >= m_NextDaze)
                {
                    BeginEjection();
                    return;
                }

                m_Rounds = Mathf.Min(m_Rounds + 1, Mathf.Max(1, m_RoundsPerEjection) - 1);
                StartCatch(null, dragon);
                return;
            }

            if (m_Rounds >= m_RoundsPerEjection - 1 && m_Riders.Count > 0)
            {
                BeginEjection();
                return;
            }

            var target = CatchTarget(out var whip);
            if (target != null)
                StartCatch(target, null);
            else if (whip != null)
                StartCatch(whip, null);

            // Le coup compte dès qu'il y a quelqu'un sur le manège, ou qu'il en attrape un (il tourne).
            if (m_Riders.Count > 0 || target != null)
                m_Rounds = Mathf.Min(m_Rounds + 1, Mathf.Max(1, m_RoundsPerEjection) - 1);
        }

        float NextInterval()
        {
            var interval = m_RoundInterval / Mathf.Max(0.1f, OverchargeRate);
            return Mathf.Max(k_Reach + k_Pull + k_Retract + 0.05f, interval);
        }

        // Qui attraper : l'ennemi au sol à sa portée le plus avancé vers ta tour (qu'on peut projeter, pas déjà pris) ; sinon, un
        // boss ou un volant à sa portée, que le ruban fouette.
        Enemy CatchTarget(out Enemy whip)
        {
            whip = null;
            if (FreeSeat() < 0)
                return null;

            var tower = Archery.Defense.Tower.Instance;
            var home = tower != null ? tower.transform.position : Hub;
            var range = m_Range * RangeScale;
            Enemy best = null;
            var bestGap = float.MaxValue;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsTrainingDummy || enemy.IsAlly || IsRiding(enemy))
                    continue;
                var flat = enemy.transform.position - Hub;
                flat.y = 0f;
                if (flat.sqrMagnitude > range * range)
                    continue;

                if (!enemy.CanBeLaunched || enemy.IsAirborne)
                {
                    if (whip == null && (enemy.IsBoss || enemy.IsFlyer))
                        whip = enemy;
                    continue;
                }

                var gap = (enemy.transform.position - home).sqrMagnitude;
                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = enemy;
                }
            }

            return best;
        }

        bool IsRiding(Enemy enemy)
        {
            foreach (var rider in m_Riders)
            {
                if (rider.Enemy == enemy)
                    return true;
            }

            if (m_Catch != null && m_Catch.Enemy == enemy)
                return true;
            foreach (var ejected in m_Ejected)
            {
                if (ejected.Enemy == enemy)
                    return true;
            }

            return false;
        }

        int FreeSeat()
        {
            var seats = Mathf.Clamp(m_Seats, 1, m_SeatTaken.Length);
            for (var i = 0; i < seats; i++)
            {
                if (!m_SeatTaken[i] && (m_Catch == null || m_Catch.Seat != i))
                    return i;
            }

            return -1;
        }

        void StartCatch(Enemy enemy, DragonBoss dragon)
        {
            m_Catch = new Catch { Enemy = enemy, Dragon = dragon, At = Time.time, Seat = dragon == null ? FreeSeat() : -1 };
            m_Catch.Tip = RimPoint(CatchPoint(m_Catch));
            Sfx.Play(m_WhooshClip, m_Catch.Tip, 0.5f, Random.Range(1.3f, 1.5f));
        }

        static Vector3 CatchPoint(Catch c)
        {
            if (c.Dragon != null)
                return c.Dragon.BodyPoint;
            return c.Enemy != null ? c.Enemy.Center : Vector3.zero;
        }

        // Le bord du chapiteau du côté de ce point : le ruban en part.
        Vector3 RimPoint(Vector3 toward)
        {
            var flat = toward - Hub;
            flat.y = 0f;
            var direction = flat.sqrMagnitude > 1e-4f ? flat.normalized : transform.forward;
            return Hub + direction * (k_RimRadius * Size) + Vector3.up * (k_RimHeight * Size);
        }

        // Le ruban : il file jusqu'à sa cible, l'attrape (« Dans la ronde ! ») et l'amène sur sa place, en cloche ; un boss, un volant
        // ou MK, il le fouette. Puis il se retire.
        void UpdateCatch()
        {
            var c = m_Catch;
            if (c.RetractAt >= 0f)
            {
                // Il se retire vers le chapiteau, et s'efface.
                var r = (Time.time - c.RetractAt) / k_Retract;
                var back = RimPoint(c.Tip);
                if (r >= 1f)
                {
                    HideRibbon();
                    m_Catch = null;
                    return;
                }

                DrawRibbon(back, Vector3.Lerp(c.Tip, back, Smooth(r)), 1f - r);
                return;
            }

            var t = Time.time - c.At;
            var alive = c.Dragon != null ? c.Dragon.isActiveAndEnabled : c.Enemy != null && c.Enemy.IsAlive;
            var target = alive ? CatchPoint(c) : c.Tip;
            var rim = RimPoint(target);
            if (t < k_Reach)
            {
                c.Tip = Vector3.Lerp(rim, target, Smooth(t / k_Reach));
                DrawRibbon(rim, c.Tip, 1f);
                return;
            }

            if (!c.Grabbed)
            {
                c.Grabbed = true;
                c.Tip = target;
                Grab(c);
                if (!c.Held)
                {
                    c.RetractAt = Time.time;
                    return;
                }
            }

            if (c.Enemy == null || !c.Enemy.IsAlive || !c.Enemy.IsHeld)
            {
                // Mort en chemin : il tombe, le ruban le lâche.
                if (c.Enemy != null)
                    c.Enemy.Release(Vector3.down);
                c.Held = false;
                c.RetractAt = Time.time;
                return;
            }

            // En cloche, de là où il était jusqu'à sa place sur le manège (qui tourne), en tournant sur lui-même.
            var s = Mathf.Clamp01((t - k_Reach) / k_Pull);
            var seat = SeatPoint(c.Seat, 0f);
            var top = Vector3.Lerp(c.Start, seat, 0.5f) + Vector3.up * (3f * Size);
            var a = Vector3.Lerp(c.Start, top, Smooth(s));
            var b = Vector3.Lerp(top, seat, Smooth(s));
            c.Enemy.transform.position = Vector3.Lerp(a, b, Smooth(s));
            c.Enemy.transform.Rotate(Vector3.up, 540f * Time.deltaTime, Space.World);
            c.Enemy.KeepHeld();
            c.Tip = c.Enemy.Center;
            DrawRibbon(rim, c.Tip, 1f);
            if (s < 1f)
                return;

            // Il est sur le manège : il tourne avec lui.
            c.Held = false;
            c.RetractAt = Time.time;
            m_SeatTaken[c.Seat] = true;
            m_Riders.Add(new Rider { Enemy = c.Enemy, Seat = c.Seat, Yaw = c.Enemy.transform.eulerAngles.y, NextHurt = Time.time + 0.25f });
            Glitter(c.Enemy.Center, 30);
            Sfx.Play(m_StompClip, seat, 0.5f, 1.4f);
        }

        // Le ruban touche : il attrape (un ennemi qu'on peut projeter, une place libre), ou fouette (un boss, un volant, MK).
        void Grab(Catch c)
        {
            var point = c.Tip;
            Glitter(point, 24);
            Sfx.Play(m_CatchClip, point, 0.8f, Random.Range(1.05f, 1.2f));
            if (c.Dragon != null)
            {
                HurtShare(c.Dragon.GetComponentInParent<Enemy>(), m_SmallBossShare, point);
                Sparks(point, 30);
                return;
            }

            var enemy = c.Enemy;
            if (enemy == null || !enemy.IsAlive)
                return;

            if (enemy.IsBoss || enemy.IsFlyer || !enemy.CanBeLaunched || enemy.IsAirborne || c.Seat < 0 || m_SeatTaken[c.Seat])
            {
                // Le fouet : ses dégâts (un boss : plus une part de ses PV max).
                if (enemy.IsBoss)
                    HurtBoss(enemy, m_CatchDamage, m_SmallBossShare, point);
                else
                    Hurt(enemy, m_CatchDamage, point, Vector3.up);
                Sparks(point, 30);
                return;
            }

            if (!Hurt(enemy, m_CatchDamage, point, Vector3.up) || !enemy.Hold(this))
                return;

            c.Held = true;
            c.Start = enemy.transform.position;
            FloatingText.Spawn(enemy.Center + Vector3.up, "Dans la ronde !", k_Gold, 1f, 1f);
        }

        // La place d'un ennemi sur le manège (entre deux grands chevaux), qui tourne avec lui ; il monte et descend un peu.
        Vector3 SeatPoint(int seat, float bob)
        {
            var count = Mathf.Max(1, Mathf.Min(m_Seats, m_SeatTaken.Length));
            var angle = (seat + 0.5f) * Mathf.PI * 2f / count;
            var local = new Vector3(Mathf.Sin(angle) * k_SeatRadius, k_DeckHeight + bob, Mathf.Cos(angle) * k_SeatRadius);
            return m_Rotor != null ? m_Rotor.TransformPoint(local) : transform.TransformPoint(local);
        }

        // Ceux qui tournent : à leur place, pris de tournis (ils tournent sur eux-mêmes, et prennent des dégâts) ; un mort tombe.
        void UpdateRiders()
        {
            for (var i = m_Riders.Count - 1; i >= 0; i--)
            {
                var rider = m_Riders[i];
                if (rider.Enemy == null || !rider.Enemy.IsAlive || !rider.Enemy.IsHeld)
                {
                    if (rider.Enemy != null)
                        rider.Enemy.Release(Vector3.down);
                    FreeRider(i);
                    continue;
                }

                var bob = Mathf.Sin(m_BobPhase * Mathf.PI * 2f + rider.Seat * 1.3f) * 0.2f;
                rider.Enemy.transform.position = SeatPoint(rider.Seat, bob);
                rider.Yaw += 260f * Time.deltaTime;
                rider.Enemy.transform.rotation = Quaternion.Euler(0f, rider.Yaw, 0f);
                rider.Enemy.KeepHeld();

                if (Time.time < rider.NextHurt)
                    continue;
                rider.NextHurt = Time.time + 0.25f;
                Hurt(rider.Enemy, m_RideDamage * 0.25f, rider.Enemy.Center, Vector3.up);
                if (Random.value < 0.3f)
                    Glitter(rider.Enemy.Center + Vector3.up * 0.8f, 6);
            }
        }

        void FreeRider(int index)
        {
            var seat = m_Riders[index].Seat;
            if (seat >= 0 && seat < m_SeatTaken.Length)
                m_SeatTaken[seat] = false;
            m_Riders.RemoveAt(index);
        }

        // ---------------------------------------------------------------- la grande éjection

        // Au 5e coup : on la voit venir (le manège s'emballe, les ampoules clignotent ensemble, la fanfare, une fusée qui monte).
        void BeginEjection()
        {
            m_EjectAt = Time.time + m_EjectWindup;
            m_SpinBoostUntil = m_EjectAt + 0.6f;
            m_GlowFlare = Mathf.Max(m_GlowFlare, 1f);
            if (Time.time >= m_FanfareUntil)
                Sfx.Play(Flourish, CrownPoint, 0.8f, Random.Range(0.98f, 1.02f));
            LaunchRocket(CrownPoint, CrownPoint + Vector3.up * (12f * Size) + Random.insideUnitSphere * 2f, m_EjectWindup);
        }

        // Tous ceux qui tournent partent d'un coup, en vrille, chacun vers sa place autour du groupe le plus serré (sans groupe à sa
        // portée : sur la voie, vers la forêt).
        void Eject()
        {
            if (m_Riders.Count == 0)
                return;

            var target = Densest(out var found);
            if (!found)
            {
                var lane = LaneNearest(Hub, null, out var along, out _);
                target = lane != null ? lane.PointAt(Mathf.Max(0f, along - 16f * Size), out _) : Hub + ForestwardAt(Hub) * 16f;
                target.y = GroundY(target, 1.5f);
            }

            var count = m_Riders.Count;
            var turn = Random.Range(0f, Mathf.PI * 2f);
            for (var i = count - 1; i >= 0; i--)
            {
                var rider = m_Riders[i];
                var enemy = rider.Enemy;
                FreeRider(i);
                if (enemy == null || !enemy.IsAlive)
                    continue;

                // Sa place : autour du centre du groupe (les premiers au milieu), puis la cloche qui y mène.
                var angle = turn + i * Mathf.PI * 2f / count;
                var landing = target + (count > 1 ? new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (1.2f + 0.5f * (i % 3)) : Vector3.zero);
                landing.y = GroundY(landing, 1.5f);
                var from = enemy.transform.position;
                var flat = landing - from;
                flat.y = 0f;
                var time = Mathf.Clamp(0.9f + flat.magnitude / 22f, 1f, 2.2f);
                var gravity = -Physics.gravity.y;
                var velocity = flat / time + Vector3.up * ((landing.y - from.y + 0.5f * gravity * time * time) / time);
                enemy.Release(velocity, false, 1f);
                enemy.Tumble(new Vector3(Random.Range(-540f, 540f), Random.Range(-360f, 360f), Random.Range(-540f, 540f)));
                m_Ejected.Add(new Ejected { Enemy = enemy, Landing = landing, Last = from, Until = Time.time + time + 1.5f });
                FloatingText.Spawn(enemy.Center + Vector3.up, "Éjecté !", k_Title, 1.1f, 1f);
            }

            // Au-dessus du manège, le bouquet de la grande éjection ; le souffle.
            var crown = CrownPoint;
            for (var i = 0; i < 3; i++)
                Firework(crown + Vector3.up * (4f * Size) + Random.insideUnitSphere * (4f * Size), RandomColor(), 1.1f);
            Confetti(SeatPoint(0, 0f), 80, 10f);
            Sfx.Play(m_WhooshClip, crown, 1f, 0.7f);
            Sfx.Play(m_BurstClip, crown, 0.9f, 0.9f);
            SkyFlash(m_StrikeFlash, true);
            Rumble(Hub, 0.4f);
        }

        // Les éjectés volent vers leur place (guidés : ils y retombent) ; chacun y fait son coup en touchant le sol.
        void UpdateEjected()
        {
            for (var i = m_Ejected.Count - 1; i >= 0; i--)
            {
                var ejected = m_Ejected[i];
                var enemy = ejected.Enemy;
                var landed = enemy == null || !enemy.IsAlive || !enemy.IsAirborne || Time.time >= ejected.Until;
                if (!landed)
                {
                    ejected.Last = enemy.transform.position;
                    enemy.GuideFlight(ejected.Landing, 40f);
                    continue;
                }

                m_Ejected.RemoveAt(i);
                if (enemy != null && enemy.IsAlive)
                    Hurt(enemy, m_EjectSelfDamage, enemy.Center, Vector3.down);
                EjectImpact(enemy != null ? enemy.transform.position : ejected.Last, enemy);
            }
        }

        // Un éjecté retombe : les dégâts autour (lui compris à part), le bouquet au-dessus, l'onde dorée, les confettis.
        void EjectImpact(Vector3 point, Enemy self)
        {
            Smash(point, false, m_EjectDamage, m_EjectRadius, m_EjectPush, m_EjectLift, m_BossShare, self);
            var heavy = Time.time >= m_NextHeavy;
            if (heavy)
                m_NextHeavy = Time.time + m_HeavyInterval;
            Firework(point + Vector3.up * (m_FireworkHeight * Size), RandomColor(), 1f);
            Shockwave(point, m_EjectRadius * 2f, 0.45f, k_Shock);
            DustRing(point, m_EjectRadius * 0.6f, 3);
            Confetti(point + Vector3.up * 0.5f, 40, 8f);
            Sparks(point + Vector3.up * 0.4f, 30);
            Sfx.Play(m_StompClip, point, 0.9f, Random.Range(0.55f, 0.7f));
            Sfx.Play(m_BurstClip, point, heavy ? 0.9f : 0.6f, Random.Range(0.95f, 1.15f));
            if (!heavy)
                return;
            FlashAt(point + Vector3.up * 3f, RandomColor(), 7f, 28f, 0.4f);
            Sfx.Play(m_BoomClip, point, 0.6f, Random.Range(1.1f, 1.3f));
            Rumble(point, 0.5f);
        }

        // Le groupe le plus serré à sa portée (au sol, ni ceux qui tournent ni ceux qui volent déjà) : son centre.
        Vector3 Densest(out bool found)
        {
            m_Scratch.Clear();
            foreach (var enemy in EnemiesNear(Hub, m_Range * RangeScale))
            {
                if (!IsRiding(enemy))
                    m_Scratch.Add(enemy);
            }

            Enemy best = null;
            var bestCount = -1;
            foreach (var enemy in m_Scratch)
            {
                var count = 0;
                foreach (var other in m_Scratch)
                {
                    var gap = other.transform.position - enemy.transform.position;
                    gap.y = 0f;
                    if (gap.sqrMagnitude < 16f)
                        count++;
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    best = enemy;
                }
            }

            found = best != null;
            return found ? best.transform.position : Hub;
        }

        // ---------------------------------------------------------------- les chevaux (tes flèches)

        // Une flèche tirée qui touche (celles de la pluie de flèches et du multishot comprises) : un cheval de plus à lancer. Sur la
        // carte, l'ennemi touché à sa portée, ou l'ennemi au sol le plus proche d'une flèche dans le sol ; dans l'arène, MK seulement.
        void OnArrowHit(ArrowHit hit)
        {
            if (!hit.IsShot || !IsBuilt || IsUnderConstruction || !m_Awake)
                return;

            if (InArena)
            {
                var dragon = hit.Collider != null ? hit.Collider.GetComponentInParent<DragonBoss>() : null;
                if (dragon != null)
                    Enqueue(null, dragon, hit.Point, ShotPower(hit));
                return;
            }

            var flat = hit.Point - Hub;
            flat.y = 0f;
            var range = m_Range * RangeScale;
            if (flat.sqrMagnitude > range * range)
                return;

            var enemy = hit.Collider != null ? hit.Collider.GetComponentInParent<Enemy>() : null;
            if (enemy != null && (!enemy.IsAlive || enemy.IsTrainingDummy || enemy.IsAlly))
                enemy = null;
            if (enemy == null)
                enemy = NearestGround(hit.Point, m_SeekRadius);
            if (enemy != null)
                Enqueue(enemy, null, hit.Point, ShotPower(hit));
        }

        // La file pleine (la pluie de flèches), la plus faible laisse sa place, la plus ancienne à force égale (3 novembre : un
        // cheval piétine selon la force de sa flèche, TowerUltimate.ShotPower).
        void Enqueue(Enemy enemy, DragonBoss dragon, Vector3 point, float power)
        {
            while (m_Orders.Count >= Mathf.Max(1, m_QueueSize))
            {
                var weakest = 0;
                for (var i = 1; i < m_Orders.Count; i++)
                {
                    if (m_Orders[i].Power < m_Orders[weakest].Power)
                        weakest = i;
                }

                if (m_Orders[weakest].Power > power)
                    return;
                m_Orders.RemoveAt(weakest);
            }

            m_Orders.Add(new Order { Enemy = enemy, Dragon = dragon, Point = point, At = Time.time, Power = power });
        }

        static Enemy NearestGround(Vector3 point, float radius)
        {
            Enemy best = null;
            var bestGap = radius * radius;
            foreach (var enemy in EnemiesNear(point, radius))
            {
                var gap = enemy.transform.position - point;
                gap.y = 0f;
                if (gap.sqrMagnitude > bestGap)
                    continue;
                bestGap = gap.sqrMagnitude;
                best = enemy;
            }

            return best;
        }

        static Vector3 OrderPoint(Order order)
        {
            if (order.Dragon != null)
                return order.Dragon.BodyPoint;
            if (order.Enemy != null)
                return order.Enemy.IsFlyer ? order.Enemy.Center : order.Enemy.transform.position;
            return order.Point;
        }

        // Un cheval par flèche, au plus un toutes les Gallop Interval s : il part de celui des siens qui est le plus près de sa cible.
        void UpdateGallops()
        {
            while (m_Orders.Count > 0 && Time.time - m_Orders[0].At > k_OrderLife)
                m_Orders.RemoveAt(0);
            if (m_Orders.Count == 0 || Time.time < m_LastGallop + Mathf.Max(0.05f, m_GallopInterval))
                return;

            var order = m_Orders[0];
            m_Orders.RemoveAt(0);
            m_LastGallop = Time.time;
            var point = OrderPoint(order);
            var source = NearestHorse(point);
            if (source == null)
                return;
            var air = order.Dragon != null || (order.Enemy != null && order.Enemy.IsFlyer);
            var shot = Throw(source, source.position, order.Enemy, order.Dragon, point, air);
            if (shot != null)
                shot.Power = order.Power;
            Sfx.Play(m_WhooshClip, source.position, 0.7f, Random.Range(0.75f, 0.9f));
            Glitter(source.position, 20);
        }

        Transform NearestHorse(Vector3 point)
        {
            Transform best = null;
            var bestGap = float.MaxValue;
            foreach (var horse in m_Horses)
            {
                if (horse == null)
                    continue;
                var gap = (horse.position - point).sqrMagnitude;
                if (gap < bestGap)
                {
                    bestGap = gap;
                    best = horse;
                }
            }

            return best;
        }

        // ---------------------------------------------------------------- ce qu'il lance

        // Un cheval saute du manège vers ce point (qui suit sa cible) : il galope dans les airs, en cloche, en se balançant.
        Shot Throw(Transform source, Vector3 from, Enemy enemy, DragonBoss dragon, Vector3 point, bool air)
        {
            var shot = TakeShot(source);
            if (shot == null)
                return null;

            var flat = point - from;
            flat.y = 0f;
            var distance = flat.magnitude;
            shot.Enemy = enemy;
            shot.Dragon = dragon;
            shot.Air = air;
            shot.Charge = false;
            shot.Power = 1f;
            shot.From = from;
            shot.Point = point;
            shot.Born = Time.time;
            shot.LandedAt = -1f;
            shot.Duration = 0.45f + distance / 38f;
            shot.Arc = 4f * Size + distance * 0.28f;
            shot.Rock = Random.Range(0f, 1f);
            shot.Go.transform.SetPositionAndRotation(from, source.rotation);
            shot.Go.transform.localScale = shot.Scale;
            shot.Go.SetActive(true);
            if (shot.Trail != null)
            {
                shot.Trail.Clear();
                shot.Trail.emitting = true;
            }

            shot.Flying = true;
            return shot;
        }

        Shot TakeShot(Transform source)
        {
            foreach (var shot in m_Shots)
            {
                if (shot.Source == source && !shot.InUse && shot.Go != null)
                    return shot;
            }

            var go = MakeCopy(source, "Cheval lancé du Manège");
            if (go == null)
                return null;
            go.SetActive(false);
            var scale = source.lossyScale * m_HorseScale;
            var made = new Shot { Go = go, Source = source, Scale = scale, Trail = AddTrail(go.transform, 1.2f * scale.y, 0.35f) };
            m_Shots.Add(made);
            return made;
        }

        // Ce qui vole suit sa cible et galope (il se balance comme un cheval à bascule), puis frappe en bout de course, debout ; il
        // reste planté 0,35 s, cabré, puis s'efface dans une gerbe de paillettes.
        void UpdateShots()
        {
            foreach (var shot in m_Shots)
            {
                if (!shot.InUse)
                    continue;
                if (shot.Go == null)
                {
                    shot.Flying = false;
                    shot.LandedAt = -1f;
                    continue;
                }

                var body = shot.Go.transform;
                if (!shot.Flying)
                {
                    var since = Time.time - shot.LandedAt;
                    var rear = Mathf.Sin(Mathf.Clamp01(since / 0.35f) * Mathf.PI) * 28f;
                    var flatForward = body.forward;
                    flatForward.y = 0f;
                    if (flatForward.sqrMagnitude > 1e-4f)
                        body.rotation = Quaternion.LookRotation(flatForward.normalized) * Quaternion.Euler(-rear, 0f, 0f);
                    if (since > 0.35f)
                        body.localScale = shot.Scale * Mathf.Clamp01(1f - (since - 0.35f) / 0.25f);
                    if (since < 0.6f)
                        continue;
                    Glitter(body.position, 40);
                    shot.LandedAt = -1f;
                    shot.Go.SetActive(false);
                    continue;
                }

                if (shot.Dragon != null)
                    shot.Point = shot.Dragon.BodyPoint;
                else if (shot.Enemy != null && shot.Enemy.IsAlive)
                    shot.Point = shot.Air ? shot.Enemy.Center : shot.Enemy.transform.position;

                var hoof = k_HoofDrop * shot.Scale.y;
                var end = shot.Air ? shot.Point : shot.Point + Vector3.up * hoof;
                var t = Mathf.Clamp01((Time.time - shot.Born) / Mathf.Max(0.05f, shot.Duration));
                var position = Vector3.Lerp(shot.From, end, t) + Vector3.up * (shot.Arc * 4f * t * (1f - t));
                var heading = end - shot.From;
                heading.y = 0f;
                var facing = heading.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(heading.normalized) : body.rotation;
                var rock = t < 0.85f ? Mathf.Sin((Time.time - shot.Born) * 14f + shot.Rock * 6f) * 22f - 12f * (1f - 2f * t) : 0f;
                body.SetPositionAndRotation(position, Quaternion.Slerp(body.rotation, facing * Quaternion.Euler(rock, 0f, 0f),
                                                                         Time.deltaTime * 18f));
                if (t < 1f)
                    continue;

                shot.Flying = false;
                if (shot.Trail != null)
                    shot.Trail.emitting = false;
                if (!shot.Air)
                {
                    body.SetPositionAndRotation(end, facing);
                    shot.LandedAt = Time.time;
                }
                else
                {
                    Glitter(body.position, 40);
                    shot.Go.SetActive(false);
                }

                HorseImpact(shot.Point, shot.Air, shot.Dragon, shot.Charge, shot.Power);
            }
        }

        void DestroyShots()
        {
            foreach (var shot in m_Shots)
            {
                if (shot.Go != null)
                    Destroy(shot.Go);
            }

            m_Shots.Clear();
        }

        // Une copie d'un de ses chevaux : ses pièces fondues en un seul maillage par matériau (fait une fois par cheval, puis réutilisé).
        GameObject MakeCopy(Transform source, string name)
        {
            if (source == null)
                return null;

            if (!m_Copies.TryGetValue(source, out var copy) || copy.Mesh == null)
            {
                var filters = new List<MeshFilter>();
                foreach (var filter in source.GetComponentsInChildren<MeshFilter>(true))
                {
                    var part = filter.GetComponent<MeshRenderer>();
                    if (part != null && part.enabled)
                        filters.Add(filter);
                }

                copy = Combine(filters, source.worldToLocalMatrix, source.name);
                if (copy.Mesh == null)
                    return null;
                m_Copies[source] = copy;
            }

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = copy.Mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = copy.Materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // Ces maillages en un seul, dans le repère donné (toLocal) : un sous-maillage par matériau.
        static (Mesh, Material[]) Combine(IList<MeshFilter> filters, Matrix4x4 toLocal, string name)
        {
            var groups = new Dictionary<Material, List<CombineInstance>>();
            var order = new List<Material>();
            foreach (var filter in filters)
            {
                var renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
                if (renderer == null || filter.sharedMesh == null)
                    continue;
                var materials = renderer.sharedMaterials;
                var matrix = toLocal * filter.transform.localToWorldMatrix;
                for (var i = 0; i < filter.sharedMesh.subMeshCount && i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null)
                        continue;
                    if (!groups.TryGetValue(material, out var list))
                    {
                        list = new List<CombineInstance>();
                        groups[material] = list;
                        order.Add(material);
                    }

                    list.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = i, transform = matrix });
                }
            }

            if (order.Count == 0)
                return (null, null);

            var parts = new CombineInstance[order.Count];
            var temporary = new List<Mesh>();
            for (var i = 0; i < order.Count; i++)
            {
                var part = new Mesh { indexFormat = IndexFormat.UInt32 };
                part.CombineMeshes(groups[order[i]].ToArray(), true, true);
                temporary.Add(part);
                parts[i] = new CombineInstance { mesh = part, transform = Matrix4x4.identity };
            }

            var mesh = new Mesh { name = "Grand Manège (" + name + ")", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts, false, false);
            mesh.RecalculateBounds();
            foreach (var part in temporary)
                Destroy(part);
            return (mesh, order.ToArray());
        }

        // ---------------------------------------------------------------- les coups

        // Dans le rayon d'un coup : au sol, à plat et à peu près à la même hauteur ; en l'air (un volant), dans une boule plus petite.
        static bool Near(Enemy enemy, Vector3 point, float radius, bool air)
        {
            var offset = (air ? enemy.Center : enemy.transform.position) - point;
            var height = offset.y;
            offset.y = 0f;
            return air ? offset.sqrMagnitude + height * height <= radius * radius * 0.5f
                : offset.sqrMagnitude <= radius * radius && height > -1.5f && height < 4f;
        }

        // Les dégâts d'un coup : moitié au bord ; ceux qui survivent volent en vrille. Un boss des vagues prend les mêmes dégâts (comme
        // le Colosse, 12.50), et en plus une part de ses PV max ; il n'est jamais projeté. Ceux qui tournent sur le manège, et
        // <paramref name="except"/>, n'y sont pas.
        void Smash(Vector3 point, bool air, float damage, float radius, float push, float lift, float bossShare, Enemy except = null)
        {
            m_Scratch.Clear();
            foreach (var enemy in AliveEnemies())
            {
                if (enemy != null && enemy != except && enemy.IsAlive && !enemy.IsHeld && Near(enemy, point, radius, air))
                    m_Scratch.Add(enemy);
            }

            foreach (var enemy in m_Scratch)
            {
                var flat = enemy.transform.position - point;
                flat.y = 0f;
                var amount = damage * (1f - 0.5f * Mathf.Clamp01(flat.magnitude / Mathf.Max(0.1f, radius)));
                if (enemy.IsBoss)
                {
                    HurtBoss(enemy, amount, bossShare, point);
                    continue;
                }

                if (!Hurt(enemy, amount, point, enemy.Center - point) || enemy.IsFlyer || !enemy.CanBeLaunched || push + lift <= 0f)
                    continue;
                if (KnockDown(enemy, point, push, lift))
                    enemy.Tumble(new Vector3(Random.Range(-360f, 360f), Random.Range(-180f, 180f), Random.Range(-360f, 360f)));
            }
        }

        // Un cheval piétine (comme les marteaux du Colosse, que le joueur a validés) : le bouquet de feu d'artifice éclate au-dessus,
        // c'est le signe de chaque coup ; l'onde dorée, les confettis, les étincelles ; tout ce qui survit vole en vrille. Le coup
        // complet (la lumière, le grand coup, les manettes) au plus toutes les Heavy Interval s. Ses dégâts (et sa part des PV d'un
        // boss) suivent la force de la flèche qui l'a lancé (power, 3 novembre ; 1 pour la charge).
        void HorseImpact(Vector3 point, bool air, DragonBoss dragon, bool charge, float power)
        {
            if (dragon != null)
            {
                HurtShare(dragon.GetComponentInParent<Enemy>(), charge ? 0f : m_HorseBossShare * power, point);
                if (charge)
                    DazeDragon(dragon);
            }
            else
            {
                Smash(point, air, m_HorseDamage * power, m_HorseRadius, m_HorsePush, m_HorseLift, m_HorseBossShare * power);
            }

            var heavy = Time.time >= m_NextHeavy;
            if (heavy)
                m_NextHeavy = Time.time + m_HeavyInterval;
            var color = RandomColor();
            Firework(point + Vector3.up * ((air ? 4f : m_FireworkHeight) * Size), color, 1f);
            if (heavy)
                SkyFlash(m_StrikeFlash * 0.6f, false);
            Sparks(point + Vector3.up * (0.6f * Size), 50);
            Sfx.Play(m_StompClip, point, 1f, Random.Range(0.5f, 0.62f));
            Sfx.Play(m_BurstClip, point, heavy ? 0.9f : 0.55f, Random.Range(0.9f, 1.15f));
            if (heavy)
            {
                FlashAt(point + Vector3.up * 3f, color, 8f, 30f, 0.4f);
                Sfx.Play(m_BoomClip, point, 0.6f, Random.Range(1.05f, 1.25f));
                Rumble(point, 0.6f);
            }

            if (air || dragon != null)
                return;

            Shockwave(point, m_HorseRadius * 2f, 0.4f, k_Shock);
            DustRing(point, m_HorseRadius * 0.6f, 4);
            Confetti(point + Vector3.up * 0.6f, 60, 9f);
        }

        // Un boss des vagues sous un coup du Manège : ses dégâts, comme les autres, puis la part de ses PV max (qui ne le tue jamais).
        void HurtBoss(Enemy enemy, float damage, float share, Vector3 point)
        {
            if (enemy == null || !enemy.IsAlive)
                return;
            if (Hurt(enemy, damage, point, enemy.Center - point))
                HurtShare(enemy, share, point);
        }

        // Une part des PV max d'un boss, sans le tuer d'un coup (12.44).
        void HurtShare(Enemy enemy, float share, Vector3 point)
        {
            if (enemy == null || !enemy.IsAlive || share <= 0f)
                return;

            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = Mathf.Min(enemy.Health.Max * share, Mathf.Max(0f, enemy.Health.Current - 1f)),
                Zone = HitZone.Body,
                Point = enemy.Center,
                Direction = Vector3.down,
                Source = this,
            });
        }

        // ---------------------------------------------------------------- la charge

        // Assez d'ennemis au sol sur la voie la plus proche, entre lui et la forêt : la charge.
        void TryCharge()
        {
            var lane = LaneNearest(Hub, null, out var along, out var distance);
            if (lane == null || distance > m_Range * RangeScale)
            {
                m_NextCharge = Time.time + 2f;
                return;
            }

            var count = 0;
            foreach (var enemy in GroundEnemies())
            {
                var gap = lane.DistanceTo(enemy.transform.position, out var at);
                if (gap <= m_ChargeWidth * 0.5f + 1f && at <= along + 2f && at >= along - m_ChargeLength)
                    count++;
            }

            if (count < m_ChargeMinEnemies)
            {
                m_NextCharge = Time.time + 1f;
                return;
            }

            m_ChargeLane = lane;
            m_ChargeStart = along;
            m_ChargeHit.Clear();
            m_ChargeLeap = Time.time;
            m_NextCharge = Time.time + m_ChargeInterval / Mathf.Max(0.1f, OverchargeRate);
            m_SpinBoostUntil = Time.time + 1.2f;
            m_GlowFlare = 1.5f;
            HeadMessages.Show("La charge !", k_Title, 2.5f);
            PlayFanfare();
            Sfx.Play(m_NeighClip, CrownPoint, 1f, Random.Range(0.95f, 1.05f));
            Rear(0.6f);
            for (var i = 0; i < 3; i++)
                LaunchRocket(CrownPoint, CrownPoint + new Vector3(Random.Range(-10f, 10f), Random.Range(10f, 16f), Random.Range(-10f, 10f)) * Size, 1f);
            SpawnChargers();
        }

        // Les chevaux de la charge : une copie de chacun des grands chevaux, posés à leur place, prêts à sauter.
        void SpawnChargers()
        {
            var count = Mathf.Max(1, m_Horses.Length);
            if (m_Chargers.Count == 0)
            {
                for (var i = 0; i < m_Horses.Length; i++)
                {
                    var source = m_Horses[i];
                    var go = MakeCopy(source, "Cheval de la charge");
                    if (go == null)
                        continue;
                    go.SetActive(false);
                    m_Chargers.Add(new Charger { Go = go, Source = source, Scale = source.lossyScale * m_HorseScale });
                }
            }

            // Deux rangs, en travers de la voie.
            var perRow = Mathf.CeilToInt(count / 2f);
            for (var i = 0; i < m_Chargers.Count; i++)
            {
                var charger = m_Chargers[i];
                var row = i / perRow;
                var column = i % perRow;
                charger.Across = perRow > 1 ? Mathf.Lerp(-0.4f, 0.4f, column / (float)(perRow - 1)) * m_ChargeWidth : 0f;
                charger.Behind = row * 4f * Size + (column % 2) * 1.2f * Size;
                charger.Phase = Random.Range(0f, 1f);
                charger.Go.transform.SetPositionAndRotation(charger.Source.position, charger.Source.rotation);
                charger.Go.transform.localScale = charger.Scale;
                charger.Go.SetActive(true);
                Glitter(charger.Source.position, 20);
            }
        }

        // Le départ (ils sautent du manège jusqu'à la voie, 0,8 s), puis le galop vers la forêt : ils piétinent et projettent tout ce
        // qu'ils croisent ; la poussière, les confettis, un bouquet au-dessus de la troupe.
        void UpdateCharge()
        {
            if (m_ChargeLane == null)
                return;

            if (m_ChargeLeap >= 0f)
            {
                var s = Mathf.Clamp01((Time.time - m_ChargeLeap) / 0.8f);
                m_ChargeSoundAt = ChargerPoint(null, 0f, out _);
                foreach (var charger in m_Chargers)
                {
                    if (charger.Go == null)
                        continue;
                    var place = ChargerPoint(charger, 0f, out var forest);
                    var from = charger.Source.position;
                    var position = Vector3.Lerp(from, place, Smooth(s)) + Vector3.up * (6f * Size * 4f * s * (1f - s));
                    charger.Go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forest) * Quaternion.Euler(-15f * (1f - s), 0f, 0f));
                }

                if (s < 1f)
                    return;

                // Ils touchent la voie : le grand coup, et ils partent.
                m_ChargeLeap = -1f;
                m_ChargeAt = 0f;
                var landing = ChargerPoint(m_Chargers.Count > 0 ? m_Chargers[0] : null, 0f, out _);
                Shockwave(landing, m_ChargeWidth * 1.5f, 0.5f, k_Shock);
                DustRing(landing, m_ChargeWidth * 0.5f, 6);
                FlashAt(landing + Vector3.up * 3f, k_Gold, 8f, 30f, 0.5f);
                Sfx.Play(m_StompClip, landing, 1f, 0.45f);
                Sfx.Play(m_BoomClip, landing, 0.8f, 1f);
                Rumble(landing, 0.8f);
                return;
            }

            if (m_ChargeAt < 0f)
                return;

            m_ChargeAt += m_ChargeSpeed * Time.deltaTime;
            var front = ChargerPoint(null, 0f, out var direction);
            m_ChargeSoundAt = front;
            foreach (var charger in m_Chargers)
            {
                if (charger.Go == null)
                    continue;
                var place = ChargerPoint(charger, m_ChargeAt, out var forest);
                var gallop = Mathf.Abs(Mathf.Sin((Time.time * 2.6f + charger.Phase) * Mathf.PI)) * 0.6f * Size;
                var rock = Mathf.Sin((Time.time * 2.6f + charger.Phase) * Mathf.PI * 2f) * 14f;
                charger.Go.transform.SetPositionAndRotation(place + Vector3.up * gallop, Quaternion.LookRotation(forest) * Quaternion.Euler(rock, 0f, 0f));
            }

            // Ceux qu'ils croisent : piétinés, projetés vers la forêt (une fois chacun).
            front = ChargerPoint(null, m_ChargeAt, out direction);
            m_ChargeSoundAt = front;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(front, m_ChargeWidth * 0.5f + 1f));
            foreach (var enemy in m_Scratch)
            {
                if (IsRiding(enemy) || !m_ChargeHit.Add(enemy))
                    continue;
                if (enemy.IsBoss)
                {
                    HurtBoss(enemy, m_ChargeDamage, m_BossShare, front);
                    continue;
                }

                if (!Hurt(enemy, m_ChargeDamage, front, direction))
                    continue;
                FloatingText.Spawn(enemy.Center + Vector3.up, "Piétiné !", k_Gold, 1.1f, 1f);
                if (enemy.CanBeLaunched && enemy.Launch(direction * m_ChargePush + Vector3.up * 7f, this))
                    enemy.Tumble(new Vector3(Random.Range(-360f, 360f), Random.Range(-180f, 180f), Random.Range(-360f, 360f)));
            }

            if (Time.time >= m_NextChargeFx)
            {
                m_NextChargeFx = Time.time + 0.08f;
                var side = Vector3.Cross(Vector3.up, direction);
                Dust(front - direction * (2f * Size) + side * Random.Range(-0.5f, 0.5f) * m_ChargeWidth + Vector3.up * 0.3f, 5);
                Confetti(front + Vector3.up * 2f, 10, 5f);
            }

            if (Time.time >= m_NextChargeFirework)
            {
                m_NextChargeFirework = Time.time + 0.55f;
                Firework(front + Vector3.up * (m_FireworkHeight * Size) + Random.insideUnitSphere * 3f, RandomColor(), 1f);
                Sfx.Play(m_BurstClip, front, 0.7f, Random.Range(0.95f, 1.15f));
            }

            var along = m_ChargeStart - m_ChargeAt;
            if (m_ChargeAt < m_ChargeLength && along > 0f)
                return;

            // Au bout : ils s'effacent dans une gerbe de paillettes.
            foreach (var charger in m_Chargers)
            {
                if (charger.Go == null)
                    continue;
                Burst(charger.Go.transform.position + Vector3.up, k_Glitter, 50, 7f, 0.25f, 1f, 0.3f);
                charger.Go.SetActive(false);
            }

            Firework(front + Vector3.up * (m_FireworkHeight * Size), RandomColor(), 1.4f);
            m_ChargeAt = -1f;
            m_ChargeLane = null;
        }

        // La place d'un cheval de la troupe (sans cheval : le milieu du front), à … m de son départ ; et vers la forêt (à plat).
        Vector3 ChargerPoint(Charger charger, float traveled, out Vector3 forest)
        {
            var behind = charger != null ? charger.Behind : 0f;
            var along = Mathf.Clamp(m_ChargeStart - traveled + behind, 0f, m_ChargeLane.Length);
            var point = m_ChargeLane.PointAt(along, out var direction);
            forest = -direction;
            forest.y = 0f;
            forest = forest.sqrMagnitude > 1e-4f ? forest.normalized : ForestwardAt(point);
            if (charger != null)
                point += Vector3.Cross(Vector3.up, forest) * charger.Across;
            point.y = GroundY(point, 1.5f) + (charger != null ? k_HoofDrop * charger.Scale.y : 0f);
            return point;
        }

        void DestroyChargers()
        {
            foreach (var charger in m_Chargers)
            {
                if (charger.Go != null)
                    Destroy(charger.Go);
            }

            m_Chargers.Clear();
        }

        // Le galop, en boucle : il suit la troupe, monte vite, et s'éteint doucement quand elle s'efface.
        void UpdateGallopSound(bool on)
        {
            m_GallopAudio = UpdateLoopAudio(m_GallopAudio, m_GallopClip, on, m_ChargeSoundAt, 1f, 1f, "Galop de la charge");
        }

        // ---------------------------------------------------------------- dans l'arène de MK

        static DragonBoss ArenaDragon()
        {
            var dragon = DragonBoss.Instance;
            if (dragon == null || !dragon.isActiveAndEnabled || dragon.IsHidden)
                return null;
            var enemy = dragon.GetComponentInParent<Enemy>();
            return enemy != null && enemy.IsAlive ? dragon : null;
        }

        // Au 5e coup de la ronde, tous ses chevaux chargent MK dans les airs, l'un après l'autre (« ils chargent MK », 12.44).
        void ChargeDragon()
        {
            var dragon = ArenaDragon();
            if (dragon == null)
                return;

            HeadMessages.Show("La charge !", k_Title, 2f);
            Sfx.Play(m_NeighClip, CrownPoint, 1f, 1f);
            Rear(0.5f);
            var first = true;
            foreach (var horse in m_Horses)
            {
                if (horse == null)
                    continue;
                var shot = Throw(horse, horse.position, null, dragon, dragon.BodyPoint, true);
                if (shot == null)
                    continue;
                shot.Charge = first;
                shot.Duration += Random.Range(0f, 0.35f);
                first = false;
            }
        }

        // Le premier cheval de la charge touche MK : étourdi (s'il peut l'être en ce moment ; sinon, la charge suivante).
        void DazeDragon(DragonBoss dragon)
        {
            if (Time.time < m_NextDaze || !dragon.Daze(m_DazeTime))
                return;

            m_NextDaze = Time.time + m_DazeInterval;
            m_DazedDragon = dragon;
            m_DazedUntil = Time.time + m_DazeTime;
            HurtShare(dragon.GetComponentInParent<Enemy>(), m_DazeBossShare, dragon.BodyPoint);
            HeadMessages.Show("Étourdi !", k_Title, 2f);
            FlashAt(dragon.BodyPoint, k_Gold, 8f, 30f, 0.5f);
            Sfx.Play(m_BoomClip, dragon.BodyPoint, 0.9f, 1f);
            ArenaPlayer.Rumble(0.6f, 0.3f);
            ShowDizzy(true);
        }

        // Autour de la tête de MK étourdi : des étoiles dorées et des petits chevaux qui tournent.
        void UpdateDizzy()
        {
            if (m_DazedDragon == null || m_DazedUntil < 0f)
                return;

            if (Time.time >= m_DazedUntil || !m_DazedDragon.isActiveAndEnabled || !m_DazedDragon.IsDazed)
            {
                m_DazedUntil = -1f;
                m_DazedDragon = null;
                ShowDizzy(false);
                return;
            }

            var head = m_DazedDragon.HeadPoint + Vector3.up * 1.6f;
            const float radius = 2.6f;
            var turn = Time.time * 200f;
            for (var i = 0; i < m_Dizzy.Count; i++)
            {
                if (m_Dizzy[i] == null)
                    continue;
                var angle = (turn + i * 360f / m_Dizzy.Count) * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle), 0.15f * Mathf.Sin(angle * 3f), Mathf.Sin(angle)) * radius;
                m_Dizzy[i].transform.SetPositionAndRotation(head + offset, Quaternion.LookRotation(new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle))));
            }

            if (Random.value < Time.deltaTime * 20f)
            {
                var angle = (turn + Random.Range(0f, 360f)) * Mathf.Deg2Rad;
                Glitter(head + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius, 4);
            }
        }

        void ShowDizzy(bool on)
        {
            if (on && m_Dizzy.Count == 0)
            {
                foreach (var foal in m_Foals)
                {
                    var go = MakeCopy(foal, "Petit cheval qui tourne");
                    if (go == null)
                        continue;
                    // Un petit cheval de 1,3 m, quelle que soit la taille du Manège dans l'arène.
                    go.transform.localScale = Vector3.one * 0.32f;
                    m_Dizzy.Add(go);
                }
            }

            foreach (var go in m_Dizzy)
            {
                if (go != null)
                    go.SetActive(on);
            }
        }

        // ---------------------------------------------------------------- le manège qui tourne, ses ampoules, son orgue

        // Il tourne (plus vite quand tes flèches attendent leurs chevaux, et à la grande éjection) ; les chevaux montent et descendent
        // sur leurs barres ; cabrés un instant aux grands moments.
        void Animate()
        {
            var target = k_Spin + Mathf.Min(k_MaxArrowSpin, m_Orders.Count * k_ArrowSpin);
            if (Time.time < m_SpinBoostUntil || m_EjectAt >= 0f)
                target = k_BoostSpin;
            if (m_WakeAt < 0f)
                m_SpinSpeed = Mathf.MoveTowards(m_SpinSpeed, target, (target > m_SpinSpeed ? 160f : 60f) * Time.deltaTime);
            m_RotorAngle = Mathf.Repeat(m_RotorAngle + m_SpinSpeed * Time.deltaTime, 360f);
            if (m_Rotor != null)
                m_Rotor.localRotation = m_RotorRest * Quaternion.Euler(0f, m_RotorAngle, 0f);

            m_BobPhase += Time.deltaTime * k_BobRate * Mathf.Sqrt(Mathf.Max(0.2f, m_SpinSpeed / k_Spin));
            if (m_RearUntil >= 0f && Time.time >= m_RearUntil)
                m_RearUntil = -1f;
            m_Rear = Mathf.MoveTowards(m_Rear, m_RearUntil >= 0f ? 1f : 0f, Time.deltaTime * 4f);
            var rear = Quaternion.Euler(-30f * Smooth(m_Rear), 0f, 0f);
            for (var i = 0; i < m_Horses.Length; i++)
                Bob(m_Horses[i], m_HorseRest[i], i * 0.75f, k_Bob, rear);
            for (var i = 0; i < m_Foals.Length; i++)
                Bob(m_Foals[i], m_FoalRest[i], i * 1.1f + 0.4f, k_Bob * 0.7f, rear);
        }

        // Un cheval sur sa barre : il monte et descend ; tourné dans le sens du manège (le modèle le pose ainsi), cabré autour de son
        // axe X aux grands moments.
        void Bob(Transform horse, Pose rest, float offset, float amplitude, Quaternion rear)
        {
            if (horse == null)
                return;
            horse.localPosition = rest.position + Vector3.up * (Mathf.Sin(m_BobPhase * Mathf.PI * 2f + offset) * amplitude);
            horse.localRotation = rest.rotation * rear;
        }

        void Rear(float duration)
        {
            m_RearUntil = Time.time + duration;
        }

        // Ses ampoules en trois groupes (« Ampoule A », « B », « C ») : trois copies du matériau, qu'on anime (la chenille).
        void SetupBulbs()
        {
            if (m_BulbMaterial == null)
                return;

            for (var k = 0; k < 3; k++)
            {
                if (m_Bulbs[k] == null)
                {
                    m_Bulbs[k] = new Material(m_BulbMaterial) { name = "Ampoules du Manège (" + "ABC"[k] + ")" };
                    m_Bulbs[k].EnableKeyword("_EMISSION");
                }

                m_BulbRenderers[k].Clear();
            }

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var name = renderer.gameObject.name;
                if (!name.StartsWith("Ampoule ") || name.Length < 9)
                    continue;
                var k = "ABC".IndexOf(name[8]);
                if (k < 0)
                    continue;
                renderer.sharedMaterial = m_Bulbs[k];
                m_BulbRenderers[k].Add(renderer);
            }
        }

        // La chenille : un groupe après l'autre, plus vite quand il s'emballe ; toutes ensemble, et fort, aux grands moments. La
        // lumière du chapiteau change doucement de couleur.
        void UpdateLights()
        {
            m_GlowFlare = Mathf.MoveTowards(m_GlowFlare, 0f, Time.deltaTime * 1.2f);
            var fast = m_SpinSpeed / k_Spin;
            m_Chase += Time.deltaTime * (2.2f + 0.6f * Mathf.Min(fast, 5f));
            var flash = m_EjectAt >= 0f ? (Mathf.Repeat(Time.time * 8f, 1f) < 0.5f ? 1f : 0.3f) : -1f;
            for (var k = 0; k < 3; k++)
            {
                var material = m_Bulbs[k];
                if (material == null)
                    continue;
                var on = flash >= 0f ? flash : 0.3f + 0.7f * Mathf.Max(0f, Mathf.Cos((m_Chase - k / 3f) * Mathf.PI * 2f));
                var level = m_Glow * on * (1f + m_GlowFlare);
                material.SetColor(k_EmissionColorId, k_BulbWarm * (4f * level));
                material.SetColor(k_BaseColorId, Color.Lerp(new Color(0.35f, 0.3f, 0.22f), Color.white, Mathf.Clamp01(level)));
            }

            if (m_CanopyLight != null)
            {
                var hue = 0.5f + 0.5f * Mathf.Sin(Time.time * 0.6f);
                m_CanopyLight.color = Color.Lerp(new Color(1f, 0.75f, 0.45f), new Color(1f, 0.45f, 0.75f), hue);
                m_CanopyLight.intensity = 2.6f * m_Glow * (1f + 0.6f * m_GlowFlare);
            }
        }

        // L'orgue, en boucle et doux, au pied du mât : plus aigu quand le manège s'emballe.
        void UpdateOrgan()
        {
            m_OrganAudio = UpdateLoopAudio(m_OrganAudio, m_OrganClip, m_Awake && !InArena, Hub + Vector3.up * (5f * Size), 0.55f,
                                           Mathf.Clamp(Mathf.Pow(m_SpinSpeed / k_Spin, 0.15f), 1f, 1.3f), "Orgue du Manège");
        }

        // Un son en boucle à ce point : il monte vite, et s'éteint doucement.
        AudioSource UpdateLoopAudio(AudioSource source, AudioClip clip, bool on, Vector3 at, float volume, float pitch, string name)
        {
            if (clip == null)
                return source;

            if (source == null)
            {
                if (!on)
                    return null;
                var go = new GameObject(name);
                source = go.AddComponent<AudioSource>();
                source.clip = clip;
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 1f;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 6f;
                source.maxDistance = 70f;
                source.dopplerLevel = 0f;
                source.outputAudioMixerGroup = Sfx.Output;
                source.volume = 0f;
                source.time = Random.Range(0f, clip.length * 0.9f);
                source.Play();
            }

            source.transform.position = at;
            source.pitch = pitch;
            source.volume = Mathf.MoveTowards(source.volume, on ? volume : 0f, Time.deltaTime * (on ? 2f : 0.8f));
            if (on || source.volume > 0f)
                return source;
            Destroy(source.gameObject);
            return null;
        }

        // Les particules qui ne s'arrêtent pas : les confettis qui tombent du chapiteau, les paillettes du sommet, la vapeur de
        // l'orgue ; et la lumière du chapiteau.
        void StartLoops()
        {
            if (m_Loops.Count > 0)
                return;

            if (m_Rotor != null)
            {
                var rim = new Vector3(0f, k_RimHeight - 0.6f, 0f);
                Loop("Confettis du chapiteau", m_Rotor, rim, new Vector3(90f, 0f, 0f), 14f, new Vector2(0.2f, 0.8f), new Vector2(0.12f, 0.22f),
                     new Vector2(3f, 4.5f), 0.12f, ParticleSystemShapeType.Circle, k_RimRadius, 0f, k_Gold, k_Gold, true);
                if (m_CanopyLight == null)
                {
                    var go = new GameObject("Lumière du chapiteau");
                    go.transform.SetParent(m_Rotor, false);
                    go.transform.localPosition = new Vector3(0f, k_DeckHeight + 4f, 0f);
                    m_CanopyLight = go.AddComponent<Light>();
                    m_CanopyLight.type = LightType.Point;
                    m_CanopyLight.range = 20f;
                    m_CanopyLight.shadows = LightShadows.None;
                }
            }

            if (m_Crown != null)
                Loop("Paillettes du sommet", m_Crown, Vector3.zero, new Vector3(-90f, 0f, 0f), 10f, new Vector2(1.5f, 3.5f), new Vector2(0.06f, 0.14f),
                     new Vector2(1.6f, 2.6f), 0.4f, ParticleSystemShapeType.Cone, 0.4f, 25f, k_Glitter, k_Gold);

            foreach (var pipe in m_Pipes)
            {
                if (pipe != null)
                    Loop("Vapeur de l'orgue", pipe, Vector3.zero, new Vector3(-90f, 0f, 0f), 3f, new Vector2(0.8f, 1.6f), new Vector2(0.4f, 0.8f),
                         new Vector2(1.4f, 2.2f), -0.05f, ParticleSystemShapeType.Cone, 0.1f, 10f, Color.white, new Color(0.85f, 0.85f, 0.9f, 1f),
                         false, 2.2f);
            }
        }

        // Un système de particules qui tourne en boucle, attaché à cette pièce (dans le monde : il laisse sa traînée). Les confettis :
        // de toutes les couleurs, qui tournent sur eux-mêmes, et tombent du bord du cercle seulement.
        void Loop(string name, Transform parent, Vector3 at, Vector3 euler, float rate, Vector2 speed, Vector2 size, Vector2 life,
                  float gravity, ParticleSystemShapeType shapeType, float radius, float coneAngle, Color from, Color to, bool confetti = false,
                  float grow = 1f)
        {
            var arrows = SpecialArrows.Instance;
            var material = arrows != null ? arrows.EffectMaterial : null;
            if (material == null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(euler);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x * Size, size.y * Size);
            main.startColor = confetti ? ConfettiColors() : new ParticleSystem.MinMaxGradient(from, to);
            main.gravityModifier = gravity;
            main.maxParticles = 250;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            if (confetti)
            {
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                var spin = particles.rotationOverLifetime;
                spin.enabled = true;
                spin.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            }

            var emission = particles.emission;
            emission.rateOverTime = rate;
            var shape = particles.shape;
            shape.shapeType = shapeType;
            shape.angle = coneAngle;
            shape.radius = radius;
            if (shapeType == ParticleSystemShapeType.Circle)
                shape.radiusThickness = 0f;
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            colors.color = fade;
            if (grow > 1f)
            {
                var sizes = particles.sizeOverLifetime;
                sizes.enabled = true;
                sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, grow));
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            particles.Play();
            m_Loops.Add(particles);
        }

        ParticleSystem.MinMaxGradient ConfettiColors()
        {
            var gradient = new Gradient();
            var keys = new GradientColorKey[Mathf.Min(8, m_FireworkColors.Length + 1)];
            for (var i = 0; i < keys.Length; i++)
                keys[i] = new GradientColorKey(i == 0 ? k_Gold : m_FireworkColors[i - 1], i / (float)Mathf.Max(1, keys.Length - 1));
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            gradient.mode = GradientMode.Fixed;
            return new ParticleSystem.MinMaxGradient(gradient) { mode = ParticleSystemGradientMode.RandomColor };
        }

        Color RandomColor() => m_FireworkColors != null && m_FireworkColors.Length > 0
            ? m_FireworkColors[Random.Range(0, m_FireworkColors.Length)] : k_Gold;

        // ---------------------------------------------------------------- le ciel de fête

        // Passager (le joueur, 12.48, pour le Colosse : « cela doit être temporaire ») : pendant l'éveil, puis le temps d'une charge ;
        // le reste du temps, et dans l'arène de MK, le ciel est celui de la partie.
        void UpdateSky()
        {
            var sky = SkyController.Instance;
            if (sky == null)
                return;

            var target = InArena ? 0f : m_WakeAt >= 0f ? 1f : m_ChargeAt >= 0f || m_ChargeLeap >= 0f ? m_SkyCharge : 0f;
            m_SkyWeight = Mathf.MoveTowards(m_SkyWeight, target, Time.deltaTime * (target > m_SkyWeight ? 0.8f : 0.25f));
            sky.SetOverlay(m_SkyWeight > 0.001f ? m_Sky : null, m_SkyWeight);
        }

        void ReleaseSky()
        {
            m_SkyWeight = 0f;
            var sky = SkyController.Instance;
            if (sky != null)
                sky.SetOverlay(null, 0f);
        }

        // Un éclair dans le ciel de la partie (rien dans l'arène) : au plus un toutes les Sky Flash Interval s, sauf aux grands
        // moments (force).
        void SkyFlash(float strength, bool force)
        {
            if (InArena || (!force && Time.time < m_NextSkyFlash))
                return;
            var sky = SkyController.Instance;
            if (sky != null)
                sky.Strike(strength);
            m_NextSkyFlash = Time.time + m_SkyFlashInterval;
        }

        // ---------------------------------------------------------------- le feu d'artifice

        // Un bouquet qui éclate à ce point (le signe de chaque coup) : une sphère d'étoiles de sa couleur, un cœur blanc, des
        // paillettes qui retombent. Un seul système de particules, déplacé là où ça éclate.
        void Firework(Vector3 point, Color color, float size)
        {
            if (m_FireworkFx == null)
                m_FireworkFx = MakeFireworks();
            if (m_FireworkFx == null)
                return;

            var scale = Size * size;
            var main = m_FireworkFx.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(7f * scale, 12f * scale);
            var emit = new ParticleSystem.EmitParams
            {
                position = point,
                applyShapeToPosition = true,
                startColor = color,
                startSize = 0.45f * Mathf.Sqrt(scale),
            };
            m_FireworkFx.Emit(emit, Mathf.RoundToInt(120 * size));
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f * scale, 5f * scale);
            emit.startColor = Color.Lerp(color, Color.white, 0.75f);
            emit.startSize = 0.6f * Mathf.Sqrt(scale);
            m_FireworkFx.Emit(emit, Mathf.RoundToInt(30 * size));
            Glitter(point, Mathf.RoundToInt(20 * size));
        }

        ParticleSystem MakeFireworks()
        {
            var arrows = SpecialArrows.Instance;
            var material = arrows != null ? arrows.EffectMaterial : null;
            if (material == null)
                return null;

            var go = new GameObject("Feu d'artifice du Manège");
            go.transform.SetParent(transform, false);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.8f);
            main.startSize = 0.45f;
            main.gravityModifier = 0.35f;
            main.maxParticles = 4000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            // Les étoiles partent vite, puis l'air les freine (la gerbe s'ouvre, puis retombe).
            var limit = particles.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 1000f;
            limit.dampen = 0f;
            limit.drag = 2f;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            colors.color = fade;
            var sizes = particles.sizeOverLifetime;
            sizes.enabled = true;
            sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            particles.Play();
            return particles;
        }

        // Une fusée monte, en sifflant, avec sa traînée dorée, et éclate en bouquet en haut.
        void LaunchRocket(Vector3 from, Vector3 to, float duration)
        {
            Rocket rocket = null;
            foreach (var r in m_Rockets)
            {
                if (!r.Active && r.Body != null)
                {
                    rocket = r;
                    break;
                }
            }

            if (rocket == null)
            {
                var go = Primitive(PrimitiveType.Sphere, "Fusée du Manège", m_BulbMaterial);
                go.transform.localScale = Vector3.one * 0.35f;
                rocket = new Rocket { Body = go.transform, Trail = AddTrail(go.transform, 0.35f, 0.5f) };
                m_Rockets.Add(rocket);
            }

            rocket.Active = true;
            rocket.From = from;
            rocket.To = to;
            rocket.Born = Time.time;
            rocket.Duration = Mathf.Max(0.2f, duration);
            rocket.Color = RandomColor();
            rocket.Size = 1.2f;
            rocket.Body.position = from;
            rocket.Body.gameObject.SetActive(true);
            rocket.Trail.Clear();
            rocket.Trail.emitting = true;
            Sfx.Play(m_RocketClip, from, 0.8f, Random.Range(0.9f, 1.15f));
        }

        void UpdateRockets()
        {
            foreach (var rocket in m_Rockets)
            {
                if (!rocket.Active || rocket.Body == null)
                    continue;

                var t = Mathf.Clamp01((Time.time - rocket.Born) / rocket.Duration);
                var wobble = new Vector3(Mathf.Sin(t * 23f), 0f, Mathf.Cos(t * 19f)) * (0.4f * (1f - t));
                rocket.Body.position = Vector3.Lerp(rocket.From, rocket.To, 1f - (1f - t) * (1f - t)) + wobble;
                if (t < 1f)
                    continue;

                rocket.Active = false;
                rocket.Trail.emitting = false;
                rocket.Body.gameObject.SetActive(false);
                Firework(rocket.To, rocket.Color, rocket.Size);
                FlashAt(rocket.To, rocket.Color, 6f, 40f, 0.5f);
                SkyFlash(m_StrikeFlash * 0.7f, false);
                Sfx.Play(m_BurstClip, rocket.To, 1f, Random.Range(0.85f, 1f));
            }
        }

        // ---------------------------------------------------------------- les effets

        void UpdateEffects()
        {
            var dt = Time.deltaTime;
            for (var i = m_Debris.Count - 1; i >= 0; i--)
            {
                var debris = m_Debris[i];
                if (debris.Body == null || Time.time >= debris.Until)
                {
                    if (debris.Body != null)
                        Destroy(debris.Body.gameObject);
                    m_Debris.RemoveAt(i);
                    continue;
                }

                debris.Velocity += Vector3.down * (20f * dt);
                debris.Body.position += debris.Velocity * dt;
                debris.Body.Rotate(debris.Spin * dt, Space.Self);
            }

            for (var i = m_Rings.Count - 1; i >= 0; i--)
            {
                var ring = m_Rings[i];
                var t = (Time.time - ring.Born) / ring.Duration;
                if (ring.Body == null || t >= 1f)
                {
                    if (ring.Body != null)
                        Destroy(ring.Body.gameObject);
                    if (ring.Material != null)
                        Destroy(ring.Material);
                    m_Rings.RemoveAt(i);
                    continue;
                }

                var radius = ring.Radius * EaseOut(t);
                ring.Body.localScale = new Vector3(radius, 1f, radius);
                var fade = Mathf.Pow(1f - t, 1.5f);
                var color = ring.Color;
                color.a *= fade;
                ring.Material.SetColor(k_BaseColorId, color);
                ring.Material.SetColor(k_EmissionColorId, (Color)(new Vector4(ring.Color.r, ring.Color.g, ring.Color.b, 1f) * (5f * fade)));
            }

            for (var i = m_Flashes.Count - 1; i >= 0; i--)
            {
                var flash = m_Flashes[i];
                var t = (Time.time - flash.Born) / flash.Duration;
                if (flash.Light == null || t >= 1f)
                {
                    if (flash.Light != null)
                        Destroy(flash.Light.gameObject);
                    m_Flashes.RemoveAt(i);
                    continue;
                }

                flash.Light.intensity = flash.Intensity * (1f - t) * (1f - t);
            }
        }

        // Le ruban doré : de son bord du chapiteau jusqu'à sa pointe, en vagues qui s'apaisent quand il est tendu.
        void DrawRibbon(Vector3 from, Vector3 to, float alpha)
        {
            if (m_Ribbon == null)
            {
                var go = new GameObject("Ruban du Manège");
                m_Ribbon = go.AddComponent<LineRenderer>();
                m_Ribbon.positionCount = 18;
                m_Ribbon.useWorldSpace = true;
                m_Ribbon.sharedMaterial = TrailMaterial;
                m_Ribbon.shadowCastingMode = ShadowCastingMode.Off;
                m_Ribbon.receiveShadows = false;
                m_Ribbon.numCapVertices = 2;
            }

            m_Ribbon.enabled = true;
            m_Ribbon.widthMultiplier = 0.32f * Size;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(k_Gold, 0f), new GradientColorKey(new Color(1f, 0.95f, 0.75f), 0.5f),
                                     new GradientColorKey(k_Gold, 1f) },
                             new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha, 1f) });
            m_Ribbon.colorGradient = gradient;
            var span = to - from;
            var side = Vector3.Cross(span, Vector3.up);
            side = side.sqrMagnitude > 1e-4f ? side.normalized : Vector3.right;
            var count = m_Ribbon.positionCount;
            for (var i = 0; i < count; i++)
            {
                var f = i / (float)(count - 1);
                var wave = Mathf.Sin(f * Mathf.PI * 3f - Time.time * 14f) * Mathf.Sin(f * Mathf.PI) * 0.6f * Size;
                var sag = Mathf.Sin(f * Mathf.PI) * 1.2f * Size;
                m_Ribbon.SetPosition(i, Vector3.Lerp(from, to, f) + side * wave + Vector3.down * sag);
            }
        }

        void HideRibbon()
        {
            if (m_Ribbon != null)
                m_Ribbon.enabled = false;
        }

        // L'onde : un anneau de lumière qui s'élargit au ras du sol, et s'efface.
        void Shockwave(Vector3 point, float radius, float duration, Color color)
        {
            if (m_RingMaterial == null)
                return;

            var go = new GameObject("Onde du Manège");
            go.transform.position = point + Vector3.up * 0.2f;
            go.AddComponent<MeshFilter>().sharedMesh = RingMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var material = new Material(m_RingMaterial) { name = "Onde du Manège (en jeu)" };
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            go.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
            m_Rings.Add(new Ring { Body = go.transform, Material = material, Color = color, Born = Time.time, Duration = duration, Radius = radius });
        }

        // La terre qui vole à l'éveil : des éclats lancés en l'air, en tournant.
        void ThrowDebris(Vector3 point, int count, float strength)
        {
            for (var i = 0; i < count; i++)
            {
                var go = Primitive(PrimitiveType.Cube, "Éclat", m_DebrisMaterial);
                var size = Random.Range(0.25f, 0.7f);
                go.transform.localScale = new Vector3(size, size * Random.Range(0.6f, 1f), size * Random.Range(0.6f, 1.2f));
                go.transform.SetPositionAndRotation(point + Vector3.up * 0.3f + Random.insideUnitSphere * 0.8f, Random.rotation);
                var out_ = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;
                m_Debris.Add(new Debris
                {
                    Body = go.transform,
                    Velocity = (out_ * Random.Range(3f, 9f) + Vector3.up * Random.Range(7f, 14f)) * strength,
                    Spin = new Vector3(Random.Range(-500f, 500f), Random.Range(-500f, 500f), Random.Range(-500f, 500f)),
                    Until = Time.time + Random.Range(1.2f, 1.9f),
                });
            }
        }

        // Un éclat de lumière qui s'éteint vite (seulement aux grands coups : c'est un objet de plus).
        void FlashAt(Vector3 point, Color color, float intensity, float range, float duration)
        {
            var go = new GameObject("Éclat de lumière");
            go.transform.position = point;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range * Size;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            m_Flashes.Add(new FlashLight { Light = light, Born = Time.time, Duration = duration, Intensity = intensity });
        }

        // Le sol tremble sous les mains du joueur, d'autant plus qu'il est près.
        static void Rumble(Vector3 point, float strength)
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
                return;
            var distance = Vector3.Distance(rig.BodyPosition, point);
            var amplitude = strength * Mathf.Clamp01(1.2f - distance / 50f);
            if (amplitude > 0.05f)
                ArenaPlayer.Rumble(amplitude, 0.25f);
        }

        // Une traînée de paillettes dorées derrière ce qu'il lance.
        static TrailRenderer AddTrail(Transform at, float width, float time)
        {
            var trail = at.gameObject.AddComponent<TrailRenderer>();
            trail.time = time;
            trail.minVertexDistance = 0.2f;
            trail.widthMultiplier = width;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.97f, 0.8f), 0f), new GradientColorKey(k_Gold, 0.4f),
                                     new GradientColorKey(new Color(1f, 0.45f, 0.75f), 1f) },
                             new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.sharedMaterial = TrailMaterial;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            return trail;
        }

        // Les étincelles, les paillettes, la poussière et les confettis des coups : des systèmes de particules à lui, déplacés là où
        // ça frappe (pas un objet de plus par coup, il en tombe beaucoup).
        void Sparks(Vector3 point, int count) => EmitAt(ref m_SparkFx, 0, point, count);

        void Glitter(Vector3 point, int count) => EmitAt(ref m_SparkFx, 0, point, count);

        void Dust(Vector3 point, int count) => EmitAt(ref m_DustFx, 1, point, count);

        void Confetti(Vector3 point, int count, float speed)
        {
            if (m_ConfettiFx == null)
                m_ConfettiFx = MakeEmitter(2);
            if (m_ConfettiFx == null)
                return;
            var main = m_ConfettiFx.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            m_ConfettiFx.transform.position = point;
            m_ConfettiFx.Emit(count);
        }

        // La poussière en anneau autour d'un coup.
        void DustRing(Vector3 point, float radius, int count)
        {
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f;
                Dust(point + new Vector3(Mathf.Cos(angle) * radius, 0.3f, Mathf.Sin(angle) * radius), count);
            }
        }

        void EmitAt(ref ParticleSystem system, int kind, Vector3 point, int count)
        {
            if (system == null)
                system = MakeEmitter(kind);
            if (system == null)
                return;
            system.transform.position = point;
            system.Emit(count);
        }

        // 0 : les étincelles et paillettes dorées ; 1 : la poussière ; 2 : les confettis de toutes les couleurs.
        ParticleSystem MakeEmitter(int kind)
        {
            var arrows = SpecialArrows.Instance;
            var material = arrows != null ? arrows.EffectMaterial : null;
            if (material == null)
                return null;

            var go = new GameObject(kind == 0 ? "Paillettes des coups" : kind == 1 ? "Poussière des coups" : "Confettis des coups");
            go.transform.SetParent(transform, false);
            // La poussière part en demi-sphère, vers le haut.
            if (kind == 1)
                go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.duration = 1f;
            switch (kind)
            {
                case 0:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 12f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
                    main.startColor = new ParticleSystem.MinMaxGradient(k_Gold, k_Glitter);
                    main.gravityModifier = 0.7f;
                    main.maxParticles = 900;
                    break;
                case 1:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
                    main.startColor = new ParticleSystem.MinMaxGradient(k_Dust, new Color(0.55f, 0.5f, 0.45f, 1f));
                    main.gravityModifier = -0.05f;
                    main.maxParticles = 300;
                    break;
                default:
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 9f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.3f);
                    main.startColor = ConfettiColors();
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    main.gravityModifier = 0.25f;
                    main.maxParticles = 1500;
                    var spin = particles.rotationOverLifetime;
                    spin.enabled = true;
                    spin.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
                    var drag = particles.limitVelocityOverLifetime;
                    drag.enabled = true;
                    drag.limit = 1000f;
                    drag.dampen = 0f;
                    drag.drag = 1.6f;
                    drag.multiplyDragByParticleSize = false;
                    drag.multiplyDragByParticleVelocity = false;
                    break;
            }

            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = kind == 1 ? ParticleSystemShapeType.Hemisphere : ParticleSystemShapeType.Sphere;
            shape.radius = kind == 1 ? 0.6f : 0.3f;
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colors.color = fade;
            if (kind == 1)
            {
                var sizes = particles.sizeOverLifetime;
                sizes.enabled = true;
                sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 2.2f));
            }

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            particles.Play();
            return particles;
        }

        GameObject Primitive(PrimitiveType type, string name, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            var renderer = go.GetComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        // Tout s'arrête : ceux qui tournent retombent, les flèches en attente sont oubliées, ce qui vole disparaît, la charge aussi.
        void StopAll()
        {
            for (var i = m_Riders.Count - 1; i >= 0; i--)
            {
                if (m_Riders[i].Enemy != null)
                    m_Riders[i].Enemy.Release(Vector3.down);
                FreeRider(i);
            }

            if (m_Catch != null && m_Catch.Held && m_Catch.Enemy != null)
                m_Catch.Enemy.Release(Vector3.down);
            m_Catch = null;
            HideRibbon();
            m_Ejected.Clear();
            for (var i = 0; i < m_SeatTaken.Length; i++)
                m_SeatTaken[i] = false;

            foreach (var shot in m_Shots)
            {
                shot.Flying = false;
                shot.LandedAt = -1f;
                if (shot.Go != null)
                    shot.Go.SetActive(false);
            }

            foreach (var charger in m_Chargers)
            {
                if (charger.Go != null)
                    charger.Go.SetActive(false);
            }

            foreach (var rocket in m_Rockets)
            {
                rocket.Active = false;
                if (rocket.Body != null)
                    rocket.Body.gameObject.SetActive(false);
            }

            m_Orders.Clear();
            m_EjectAt = -1f;
            m_Rounds = 0;
            m_ChargeAt = -1f;
            m_ChargeLeap = -1f;
            m_ChargeLane = null;
            m_SpinBoostUntil = -1f;
            m_DazedUntil = -1f;
            m_DazedDragon = null;
            ShowDizzy(false);
            if (m_GallopAudio != null)
                Destroy(m_GallopAudio.gameObject);
            m_GallopAudio = null;
            if (m_OrganAudio != null)
                Destroy(m_OrganAudio.gameObject);
            m_OrganAudio = null;
        }

        // Les effets laissés dans le monde (ce qu'il lance, la charge, les fusées, le ruban, les ondes, les lumières), quand il disparaît.
        void ClearEffects()
        {
            DestroyShots();
            DestroyChargers();
            foreach (var rocket in m_Rockets)
            {
                if (rocket.Body != null)
                    Destroy(rocket.Body.gameObject);
            }

            m_Rockets.Clear();
            foreach (var go in m_Dizzy)
            {
                if (go != null)
                    Destroy(go);
            }

            m_Dizzy.Clear();
            if (m_Ribbon != null)
                Destroy(m_Ribbon.gameObject);
            m_Ribbon = null;

            foreach (var debris in m_Debris)
            {
                if (debris.Body != null)
                    Destroy(debris.Body.gameObject);
            }

            foreach (var ring in m_Rings)
            {
                if (ring.Body != null)
                    Destroy(ring.Body.gameObject);
                if (ring.Material != null)
                    Destroy(ring.Material);
            }

            foreach (var flash in m_Flashes)
            {
                if (flash.Light != null)
                    Destroy(flash.Light.gameObject);
            }

            m_Debris.Clear();
            m_Rings.Clear();
            m_Flashes.Clear();
        }

        // Un anneau plat de rayon 1 (de 0,8 à 1), tourné vers le haut (on le voit d'en haut : la tour, le sol).
        static Mesh RingMesh
        {
            get
            {
                if (s_RingMesh != null)
                    return s_RingMesh;

                const int segments = 48;
                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                for (var i = 0; i <= segments; i++)
                {
                    var angle = i * Mathf.PI * 2f / segments;
                    var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    vertices.Add(direction * 0.8f);
                    vertices.Add(direction);
                }

                for (var i = 0; i < segments; i++)
                {
                    var a = i * 2;
                    triangles.AddRange(new[] { a, a + 2, a + 1, a + 2, a + 3, a + 1 });
                }

                s_RingMesh = new Mesh { name = "Onde du Manège" };
                s_RingMesh.SetVertices(vertices);
                s_RingMesh.SetTriangles(triangles, 0);
                s_RingMesh.RecalculateNormals();
                s_RingMesh.RecalculateBounds();
                return s_RingMesh;
            }
        }

        // Le matériau des traînées et du ruban : couleurs par sommet, sans lumière (celui des éclairs).
        static Material TrailMaterial
        {
            get
            {
                if (s_TrailMaterial != null)
                    return s_TrailMaterial;
                var shader = Shader.Find("Archery/UnlitVertexColor");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");
                s_TrailMaterial = new Material(shader) { name = "Traînée du Manège (en jeu)" };
                return s_TrailMaterial;
            }
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        static float EaseOut(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        /// <summary>La portée dessinée pendant la pose (3 novembre, le placement lisible).</summary>
        public override float ReachAt(int level) => m_Range;
    }
}
