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
    /// L'ultime <b>Colosse de forge</b> (2 novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.44, 12.46 à 12.48) : le niveau 4 de
    /// l'Enclume ou de la Grande bombarde, la famille de la forge et de la poudre. Un géant de fer agenouillé, une fournaise dans la
    /// poitrine, des veines de fonte qui luisent ; sa masse de forge dans la main gauche, son grand marteau dans la droite.
    /// <list type="bullet">
    /// <item><b>L'éveil</b> : bâti, il sort de terre dans une mare de fonte, ses yeux et sa fournaise s'allument, le ciel devient
    /// un orage de braise ; il lève son marteau, la foudre le frappe, et il l'abat : l'onde renverse tout autour. Puis le ciel
    /// redevient celui de la partie.</item>
    /// <item><b>La forge, de la main gauche</b> : il bat le lingot sur son enclume sans arrêt ; chaque coup lance un éclat de fonte
    /// sur un ennemi à sa portée, et le lingot chauffe. Au 5e coup, il lève sa masse, la foudre la frappe, et le lingot chauffé à
    /// blanc part sur le groupe le plus serré : la foudre tombe avec lui, l'onde de choc, la roche qui vole, le sol fendu de
    /// fonte.</item>
    /// <item><b>Les marteaux, de la main droite</b> : chaque flèche tirée qui touche un ennemi à sa portée (ou le sol près de lui),
    /// et il y lance un marteau aussi grand que le sien, chargé de foudre : comme le marteau d'avant, la foudre tombe avec lui, l'onde
    /// de choc, tout autour vole en l'air, et il reste planté un instant. La pluie de flèches et le multishot le déchaînent : 5
    /// marteaux par seconde au plus.</item>
    /// <item><b>La coulée de fonte</b> : sa fournaise déborde ; un torrent de fonte coule de sa poitrine, puis une rivière de fonte
    /// descend toute la voie vers la forêt, sa vague en tête : elle brûle et repousse tout ce qu'elle touche, puis refroidit.</item>
    /// <item><b>Dans l'arène de MK</b> (il suit le joueur, comme les balistes et les tours de givre) : ses marteaux suivent tes
    /// flèches dans MK, ses éclats aussi, et son lingot le plaque au sol.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Le joueur, le 2 novembre au soir, après son essai : l'attaque et la coulée manquaient de puissance ; le modèle devait être plus
    /// fini (texturé, lumineux, des particules), avec une apparition, le ciel qui change, un éclair à chaque coup de marteau : « on
    /// doit ressentir sa puissance » (12.47). Puis, au deuxième essai (12.48) : le visuel et le son vont mieux, mais un seul coup de
    /// temps en temps ne fait sentir ni sa puissance ni le nombre. De nos pistes, il prend la forge continue et son idée d'un marteau
    /// par flèche, plafonnée pour le casque : « d'une main il frappe sa forge, de l'autre il envoie des marteaux à chaque flèche (cap
    /// à 5 par seconde) » ; et le ciel de forge doit être passager. Le ciel est celui de <see cref="SkySettings.Forge"/>, posé
    /// par-dessus celui de la difficulté (<see cref="SkyController.SetOverlay"/>), pendant l'éveil et la coulée seulement. Ce qu'il
    /// lance est réutilisé d'un lancer à l'autre, et les étincelles et la poussière de ses coups viennent de deux systèmes de
    /// particules à lui : beaucoup de coups, peu de coût. Les boss ne sont jamais projetés ni tués d'un coup. Le modèle : z vers
    /// l'avant (la forêt sur la voie, la voie au bord), x en travers ; posé sur la voie, les ennemis passent entre ses genoux. Le
    /// buste ne se tourne plus (la masse doit retomber sur l'enclume) : le bras du marteau se tourne seul, à l'épaule. Les veines,
    /// les yeux, la fournaise, les cheminées, le lingot : le matériau animé de la fonte (<c>Archery/MoltenIron</c>).
    /// </remarks>
    public class UltimeColosse : TowerUltimate
    {
        // Les angles du bras du marteau (degrés autour de son axe X, depuis le repos où il pend ; négatif : vers l'avant) : levé
        // par-dessus la tête, un peu en arrière ; le coup sec qui lance un marteau, vers l'avant et en haut. À l'épaule, il se
        // tourne vers sa cible (degrés ; négatif : vers le dedans, où est sa tête).
        const float k_RaisedAngle = -205f;
        const float k_SnapAngle = -135f;
        const float k_MinYaw = -35f;
        const float k_MaxYaw = 110f;

        // Le bras de la forge (depuis le repos, sa masse sur le lingot) : levé pour un coup, par-dessus la tête pour le grand. Les
        // temps (s) : lever la masse, la tenir en haut, l'abattre ; ceux du grand coup.
        const float k_ForgeRaised = -95f;
        const float k_ForgeBigRaised = -165f;
        const float k_StrikeRaise = 0.42f;
        const float k_StrikeHold = 0.04f;
        const float k_StrikeSlam = 0.1f;
        const float k_BigRaise = 0.85f;
        const float k_BigHold = 0.2f;
        const float k_BigSlam = 0.14f;

        // Une flèche attend son marteau au plus … s (sa cible est loin, ou morte).
        const float k_OrderLife = 2f;

        static readonly int k_GlowId = Shader.PropertyToID("_Glow");
        static readonly int k_CoolId = Shader.PropertyToID("_Cool");
        static readonly int k_EdgesId = Shader.PropertyToID("_Edges");
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Header("Le modèle")]
        [Tooltip("Le buste (repère aux hanches) : il se penche en arrière quand la masse se lève, et revient au coup.")]
        [SerializeField]
        Transform m_Torso;

        [Tooltip("Le bras du marteau (repère à l'épaule droite) : il pend au repos, tourne autour de son axe X pour lever le marteau, " +
                 "et à l'épaule vers sa cible.")]
        [SerializeField]
        Transform m_Arm;

        [Tooltip("Le marteau, dans le poing (repère au poing, sa tête vers le bas au repos) : les marteaux lancés en sont des copies.")]
        [SerializeField]
        Transform m_Hammer;

        [Tooltip("Le milieu de la tête du marteau.")]
        [SerializeField]
        Transform m_HammerHead;

        [Tooltip("Le bras de la forge (repère à l'épaule gauche) : penché vers l'avant au repos, sa masse posée sur le lingot ; il " +
                 "tourne autour de son axe X pour frapper.")]
        [SerializeField]
        Transform m_LeftArm;

        [Tooltip("Le milieu de la tête de la masse de forge.")]
        [SerializeField]
        Transform m_ForgeHead;

        [Tooltip("Le lingot posé sur l'enclume (de fonte) : il chauffe à chaque coup, puis part au grand coup.")]
        [SerializeField]
        Transform m_Ingot;

        [Tooltip("La fournaise de la poitrine : la fonte en coule.")]
        [SerializeField]
        Transform m_Furnace;

        [Tooltip("Le haut des cheminées du dos : elles fument.")]
        [SerializeField]
        Transform[] m_Chimneys = System.Array.Empty<Transform>();

        [Tooltip("La fonte (Fonte_en_fusion, shader Archery/MoltenIron) : les veines du géant, le lingot, ce qu'il lance, le jet, la " +
                 "rivière, le sol fendu.")]
        [SerializeField]
        Material m_MoltenMaterial;

        [Tooltip("L'onde de choc : une lumière transparente (Rayon_du_jugement), copiée et teinte en jeu.")]
        [SerializeField]
        Material m_RingMaterial;

        [Tooltip("Les éclats de roche qui volent à l'impact.")]
        [SerializeField]
        Material m_DebrisMaterial;

        [Header("L'éveil")]
        [Tooltip("Il sort de terre : de cette profondeur (m), en … s.")]
        [SerializeField]
        float m_WakeDepth = 14f;

        [SerializeField]
        float m_RiseTime = 2.4f;

        [Tooltip("Le marteau abattu à la fin de l'éveil : l'onde renverse les ennemis à … m et leur fait … de dégâts.")]
        [SerializeField]
        float m_WakeRadius = 22f;

        [SerializeField]
        float m_WakeDamage = 100f;

        [Header("La portée")]
        [Tooltip("Portée (m, à plat, autour du géant) : ta flèche doit toucher plus près ; la forge y cherche ses cibles.")]
        [SerializeField]
        float m_Range = 30f;

        [Header("La forge (main gauche)")]
        [Tooltip("Un coup de masse sur l'enclume toutes les … s ; … fois moins souvent sans ennemi à sa portée.")]
        [SerializeField]
        float m_ForgeInterval = 1.2f;

        [SerializeField]
        float m_ForgeIdleSlow = 2.5f;

        [Tooltip("Chaque coup lance un éclat de fonte sur un ennemi au sol à sa portée : dégâts au centre (vague 1, en Normal ; " +
                 "moitié au bord), rayon (m). 70 dans 3 m (12.48, au tableau d'équilibrage : seule, la forge vaut un trio moyen au " +
                 "niveau 3).")]
        [SerializeField]
        float m_ChunkDamage = 70f;

        [SerializeField]
        float m_ChunkRadius = 3f;

        [Tooltip("Au … e coup, le lingot chauffé à blanc part sur le groupe le plus serré : le grand coup (sans ennemi, il le garde " +
                 "chaud pour le coup suivant).")]
        [SerializeField]
        int m_StrikesPerIngot = 5;

        [Header("Le grand coup (le lingot)")]
        [Tooltip("Dégâts au centre de l'impact (vague 1, en Normal) ; moitié au bord. 450 : plus qu'un marteau, c'est le sommet de la " +
                 "forge (la grande enclume de l'Enclume : 350).")]
        [SerializeField]
        float m_Damage = 450f;

        [Tooltip("Rayon (m) de l'impact.")]
        [SerializeField]
        float m_Radius = 7f;

        [Tooltip("Ceux qui survivent sont projetés : loin de l'impact (m/s), et vers le haut (m/s).")]
        [SerializeField]
        float m_Push = 10f;

        [SerializeField]
        float m_Lift = 14f;

        [Tooltip("Un boss des vagues sous le lingot prend ses dégâts, et perd en plus cette part de ses PV max (il n'est jamais " +
                 "projeté) : 1,5 % toutes les 6 s environ, 0,25 % par seconde. MK, dans l'arène : la part seulement (Pin Boss Share).")]
        [SerializeField]
        float m_BossShare = 0.015f;

        [Tooltip("Le lingot et les marteaux laissent le sol en feu : rayon (m), durée (s), dégâts par seconde.")]
        [SerializeField]
        float m_ScorchRadius = 2.5f;

        [SerializeField]
        float m_ScorchTime = 3f;

        [SerializeField]
        float m_ScorchBurn = 15f;

        [Header("Les marteaux (main droite)")]
        [Tooltip("Au plus un marteau toutes les … s (0,2 : 5 par seconde, le plafond choisi par le joueur pour le casque).")]
        [SerializeField]
        float m_ThrowInterval = 0.2f;

        [Tooltip("Les flèches qui attendent leur marteau, au plus : les plus anciennes s'effacent (il suit tes derniers tirs).")]
        [SerializeField]
        int m_QueueSize = 8;

        [Tooltip("Une flèche dans le sol : le marteau va sur l'ennemi au sol le plus proche, à … m au plus ; sinon, rien.")]
        [SerializeField]
        float m_SeekRadius = 5f;

        [Tooltip("Dégâts au centre (vague 1, en Normal ; moitié au bord), rayon (m) ; ceux qui survivent sont projetés en vrille : " +
                 "loin (m/s), vers le haut (m/s). Ceux du marteau d'avant (le joueur, 12.48 : « garder les mêmes dégâts »).")]
        [SerializeField]
        float m_HammerDamage = 160f;

        [SerializeField]
        float m_HammerRadius = 6f;

        [SerializeField]
        float m_HammerPush = 8f;

        [SerializeField]
        float m_HammerLift = 12f;

        [Tooltip("La taille d'un marteau lancé (fois celle du sien : 1, aussi imposant que le sien).")]
        [SerializeField]
        float m_HammerScale = 1f;

        [Tooltip("Ce qu'un boss perd en plus sous un marteau (part de ses PV max) : à 5 marteaux par seconde, 1 % par seconde au plus. " +
                 "MK, dans l'arène : cette part seulement (12.50).")]
        [SerializeField]
        float m_HammerBossShare = 0.002f;

        [Tooltip("Le coup complet (l'éclat de lumière, le tonnerre, la roche qui vole, le sol fendu et en feu) au plus toutes les … s ; " +
                 "entre deux, chaque marteau a sa foudre, son onde, son explosion, sa poussière. L'éclair dans le ciel, au plus toutes " +
                 "les … s (pour le casque, et pas un ciel qui clignote).")]
        [SerializeField]
        float m_HeavyInterval = 0.5f;

        [SerializeField]
        float m_SkyFlashInterval = 1.2f;

        [Tooltip("Ce qu'un boss perd sous un éclat de la forge (part de ses PV max).")]
        [SerializeField]
        float m_SmallBossShare = 0.001f;

        [Header("La foudre")]
        [Tooltip("La couleur des éclairs : sur la masse levée, puis sur le grand coup ; les petits éclairs des marteaux.")]
        [SerializeField]
        Color m_BoltColor = new Color(0.7f, 0.8f, 1f, 1f);

        [Tooltip("La force de l'éclair dans le ciel au grand coup (0 à 1 : tout le ciel s'éclaire).")]
        [SerializeField]
        float m_StrikeFlash = 0.5f;

        [Tooltip("D'où tombe la foudre (m au-dessus de sa cible).")]
        [SerializeField]
        float m_BoltHeight = 45f;

        [Header("La coulée de fonte")]
        [Tooltip("Temps (s) entre deux coulées ; il faut au moins Flow Min Enemies ennemis au sol sur la voie, entre lui et la forêt.")]
        [SerializeField]
        float m_FlowInterval = 25f;

        [SerializeField]
        int m_FlowMinEnemies = 6;

        [Tooltip("La rivière : sa longueur (m, le long de la voie, vers la forêt), sa vitesse (m/s), sa largeur (m).")]
        [SerializeField]
        float m_FlowLength = 70f;

        [SerializeField]
        float m_FlowSpeed = 12f;

        [SerializeField]
        float m_FlowWidth = 7f;

        [Tooltip("Sa vague de tête : dégâts à ceux qu'elle touche, puis leur brûlure (en tout, sur 3 s) ; ils sont repoussés vers la " +
                 "forêt (m/s), en l'air.")]
        [SerializeField]
        float m_FlowDamage = 90f;

        [SerializeField]
        float m_FlowBurn = 50f;

        [SerializeField]
        float m_FlowPush = 12f;

        [Tooltip("Ceux qui marchent dans la rivière encore chaude brûlent : dégâts par seconde.")]
        [SerializeField]
        float m_RiverBurn = 30f;

        [Tooltip("La fonte coule de la poitrine pendant … s avant de descendre la voie ; la rivière met … s à refroidir.")]
        [SerializeField]
        float m_PourTime = 1.4f;

        [SerializeField]
        float m_FlowStay = 6f;

        [Header("Le ciel de forge")]
        [Tooltip("Sa force (0 à 1) pendant la coulée (l'éveil : 1) ; le reste du temps, le ciel est celui de la partie (12.48).")]
        [SerializeField]
        float m_SkyFlow = 0.85f;

        [Header("Dans l'arène de MK")]
        [Tooltip("Sa taille dans l'arène (fois la sienne) : la voûte de la grotte est basse près des parois (22,5 m, le plateau à " +
                 "5 m) ; à 0,65, il fait 11,6 m, son marteau levé 15 m.")]
        [SerializeField]
        float m_ArenaScale = 0.65f;

        [Tooltip("Le lingot plaque MK au sol pendant … s, au plus toutes les … s (entre deux, le grand coup attend).")]
        [SerializeField]
        float m_PinTime = 3f;

        [SerializeField]
        float m_PinInterval = 12f;

        [Tooltip("Ce que MK perd sous le lingot (part de ses PV max).")]
        [SerializeField]
        float m_PinBossShare = 0.03f;

        [Header("Sons")]
        [Tooltip("Un marteau lancé : le souffle (plus aigu).")]
        [SerializeField]
        AudioClip m_ThrowClip;

        [Tooltip("Le coup qui frappe : le grand coup, et chaque marteau lancé (plus aigu, moins fort).")]
        [SerializeField]
        AudioClip m_SlamClip;

        [Tooltip("Le fer battu au grand coup, sur l'enclume.")]
        [SerializeField]
        AudioClip m_CatchClip;

        [Tooltip("Chaque coup de la masse sur l'enclume : le fer qui sonne, plus aigu à mesure que le lingot chauffe.")]
        [SerializeField]
        AudioClip m_ClangClip;

        [Tooltip("La fournaise qui déborde : le souffle et le grondement (l'éveil, le début de la coulée).")]
        [SerializeField]
        AudioClip m_PourClip;

        [Tooltip("Le sol qui grésille : sous le lingot et les éclats, et là où la fonte touche le sol.")]
        [SerializeField]
        AudioClip m_SizzleClip;

        [Tooltip("La coulée : un son en boucle, qui suit le jet puis la vague, et s'éteint avec elle.")]
        [SerializeField]
        AudioClip m_FlowClip;

        [Tooltip("Le tonnerre de la foudre.")]
        [SerializeField]
        AudioClip m_ThunderClip;

        [Tooltip("Le grand impact : le marteau abattu à l'éveil, et le grand coup.")]
        [SerializeField]
        AudioClip m_BoomClip;

        enum ShotKind
        {
            Hammer,
            Chunk,
            Ingot,
        }

        // Ce qu'il lance (un marteau, un éclat, le lingot) : réutilisé d'un lancer à l'autre. Un marteau reste planté un instant
        // après son coup (LandedAt), puis s'enfonce dans le sol.
        class Shot
        {
            public ShotKind Kind;
            public GameObject Go;
            public TrailRenderer Trail;
            public bool Flying;
            public float LandedAt = -1f;
            public float HeadOffset;
            public bool InUse => Flying || LandedAt >= 0f;
            public Vector3 From;
            public Vector3 Point;
            public Enemy Enemy;
            public DragonBoss Dragon;
            public bool Air;
            public float Born;
            public float Duration;
            public float Arc;
            public Vector3 SpinAxis;
            public float Spin;
            public float NextCrackle;
            public float Power = 1f;
        }

        // Une flèche qui attend son marteau : sa cible (un ennemi suivi, ou MK) et où elle a touché.
        struct Order
        {
            public Enemy Enemy;
            public DragonBoss Dragon;
            public Vector3 Point;
            public float At;
            public float Power;
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

        class Crack
        {
            public Renderer Renderer;
            public float Born;
            public float Life;
        }

        class FlashLight
        {
            public Light Light;
            public float Born;
            public float Duration;
            public float Intensity;
        }

        // Une rivière de fonte : une bande qui suit la voie, une section par mètre, du géant vers la forêt.
        class River
        {
            public GameObject Go;
            public Mesh Mesh;
            public Renderer Renderer;
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>();
            public MapScan.Lane Lane;
            public float Start;
            public float Length;
            public float CoolStart = -1f;
            public float NextBurn;
        }

        static readonly Color k_Molten = new Color(1f, 0.5f, 0.12f, 1f);
        static readonly Color k_Sparks = new Color(1f, 0.75f, 0.3f, 1f);
        static readonly Color k_Dust = new Color(0.72f, 0.66f, 0.56f, 1f);
        static readonly Color k_Smoke = new Color(0.3f, 0.29f, 0.29f, 1f);
        static readonly Color k_Title = new Color(1f, 0.62f, 0.25f);
        static readonly Color k_Shock = new Color(1f, 0.55f, 0.2f, 0.85f);
        static readonly float[] k_Across = { -1f, -0.45f, 0.45f, 1f };

        static Mesh s_RingMesh;
        static Material s_TrailMaterial;

        readonly List<Enemy> m_Scratch = new List<Enemy>();
        readonly List<Enemy> m_Burning = new List<Enemy>();
        readonly HashSet<Enemy> m_FlowHit = new HashSet<Enemy>();
        readonly List<Renderer> m_GlowRenderers = new List<Renderer>();
        readonly List<ParticleSystem> m_Loops = new List<ParticleSystem>();
        readonly List<Debris> m_Debris = new List<Debris>();
        readonly List<Ring> m_Rings = new List<Ring>();
        readonly List<Crack> m_Cracks = new List<Crack>();
        readonly List<FlashLight> m_Flashes = new List<FlashLight>();
        readonly List<River> m_Rivers = new List<River>();
        readonly List<Shot> m_Shots = new List<Shot>();
        readonly List<Order> m_Orders = new List<Order>();
        MaterialPropertyBlock m_Block;
        SkySettings m_Sky;
        float m_SkyWeight;
        ParticleSystem m_SparkFx;
        ParticleSystem m_DustFx;
        Renderer m_IngotRenderer;

        Quaternion m_TorsoRest = Quaternion.identity;
        Quaternion m_ArmRest = Quaternion.identity;
        Quaternion m_LeftArmRest = Quaternion.identity;
        float m_Lean;

        // L'éveil (−1 : fini, ou pas commencé) ; l'éclat de la fonte du géant (0 : éteinte), et ses sursauts.
        float m_WakeAt = -1f;
        bool m_Awake;
        int m_WakeStep;
        float m_NextWakeFx;
        Vector3 m_Ground;
        float m_Glow;
        float m_GlowFlare;
        Light m_FurnaceLight;

        // La forge : le coup en cours (−1 : il attend le suivant), s'il est le grand ; la chaleur du lingot (les coups depuis le
        // dernier grand) ; le lingot parti (un neuf revient à …).
        float m_ForgeAngle;
        float m_ForgeAt = -1f;
        float m_NextForge;
        bool m_ForgeBig;
        bool m_ForgeCharged;
        int m_Heat;
        float m_IngotBack = -1f;

        // Les marteaux : le bras levé (0 à 1), son angle et sa rotation à l'épaule, le coup sec en cours (−1 : aucun), le dernier
        // lancer.
        float m_Raise;
        float m_ArmAngle;
        float m_ArmYaw;
        float m_ArmYawGoal;
        float m_SnapAt = -1f;
        bool m_Snapped;
        float m_LastThrow = -100f;

        // La foudre appelée sur le marteau levé (une fois par salve), le dernier coup complet, le dernier éclair dans le ciel.
        bool m_Called;
        float m_NextCall;
        float m_NextHeavy;
        float m_NextSkyFlash;

        float m_NextPin;
        bool m_ArenaHint;

        // La coulée : la fonte verse de la poitrine (m_PourUntil), puis la vague descend la voie (m_FlowAt, en m ; −1 : rien).
        MapScan.Lane m_FlowLane;
        float m_FlowStart;
        float m_FlowAt = -1f;
        float m_PourUntil = -1f;
        float m_NextFlow;
        float m_NextSection;
        float m_NextFlowFx;
        float m_NextFrontFire;
        River m_River;
        Transform m_Jet;
        Transform m_Crest;
        Light m_FlowLight;
        AudioSource m_FlowAudio;
        Vector3 m_FlowSoundAt;

        protected override float ArenaScale => m_ArenaScale;

        Vector3 FurnacePoint => m_Furnace != null ? m_Furnace.position : transform.position + Vector3.up * 11f;

        Vector3 HeadPoint => m_HammerHead != null ? m_HammerHead.position : transform.position + Vector3.up * 2f;

        Vector3 ForgeHeadPoint => m_ForgeHead != null ? m_ForgeHead.position : IngotPoint + Vector3.up * 3f;

        // Le dessus du lingot, où tombe la masse.
        Vector3 IngotPoint => m_Ingot != null ? m_Ingot.position + Vector3.up * (m_Ingot.lossyScale.y * 0.5f)
            : transform.TransformPoint(new Vector3(-3.9f, 3.4f, 5.4f));

        protected override void Awake()
        {
            base.Awake();
            if (m_Torso != null)
                m_TorsoRest = m_Torso.localRotation;
            if (m_Arm != null)
                m_ArmRest = m_Arm.localRotation;
            if (m_LeftArm != null)
                m_LeftArmRest = m_LeftArm.localRotation;
            m_Sky = SkySettings.Forge();
            m_Block = new MaterialPropertyBlock();
            m_IngotRenderer = m_Ingot != null ? m_Ingot.GetComponent<Renderer>() : null;

            // Ce qui luit avec le géant : tout ce qui est fait de fonte (veines, yeux, fournaise, cheminées, runes), sauf le lingot,
            // qui suit sa propre chaleur.
            if (m_MoltenMaterial != null)
            {
                foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.sharedMaterial == m_MoltenMaterial && renderer != m_IngotRenderer)
                        m_GlowRenderers.Add(renderer);
                }
            }
        }

        void OnEnable()
        {
            Arrow.AnyHit += OnArrowHit;

            // Dans l'arène, au début du combat (ArenaTurrets le rallume) : ce qu'il sait faire contre MK.
            if (InArena && IsBuilt && !m_ArenaHint)
            {
                m_ArenaHint = true;
                HeadMessages.Show("Le Colosse de forge est là !\n<size=65%>Touche MK : ses marteaux suivent tes flèches, son lingot " +
                                  "le plaque au sol</size>", k_Title, 5f);
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
            FinishWake();
            base.EnterArena();
            m_NextPin = 0f;
            m_ArenaHint = false;
        }

        public override void LeaveArena()
        {
            StopAll();
            DestroyShots();
            base.LeaveArena();
        }

        protected override void Update()
        {
            base.Update();
            UpdateEffects();
            UpdateShots();
            if (!IsBuilt || IsUnderConstruction)
                return;

            UpdateSky();
            if (m_WakeAt >= 0f)
            {
                UpdateWake();
                Animate();
                UpdateGlow();
                return;
            }

            UpdateForge();
            UpdateBarrage();
            Animate();
            UpdateGlow();
            if (InArena)
                return;

            UpdatePour();
            UpdateFlow();
            UpdateFlowSound(m_PourUntil >= 0f || m_FlowAt >= 0f);
            if (m_FlowAt < 0f && m_PourUntil < 0f && Time.time >= m_NextFlow)
                TryFlow();
        }

        // ---------------------------------------------------------------- l'éveil

        // Bâti (au marteau, ou par la vitrine) : il s'enfonce, éteint, et l'éveil commence.
        void BeginWake()
        {
            m_WakeAt = Time.time;
            m_WakeStep = 0;
            m_Awake = false;
            m_Glow = 0f;
            m_Ground = transform.parent != null ? transform.parent.position : transform.position;
            var local = transform.localPosition;
            transform.localPosition = new Vector3(local.x, -m_WakeDepth, local.z);
            ApplyGlow(0f);

            HeadMessages.Show("Le Colosse de forge s'éveille !", k_Title, 4f);
            Sfx.Play(m_PourClip, m_Ground, 1f, 0.6f);
            Sfx.Play(m_BoomClip, m_Ground, 0.8f, 0.5f);
            if (m_MoltenMaterial != null)
                GroundPatch.Spawn(m_Ground, 6f, m_RiseTime + 4f, 1f, m_MoltenMaterial, this).Ignite(m_ScorchBurn * Strength, m_RiseTime + 3f);
            Cracks(m_Ground, 10, 4f, 9f, m_RiseTime + 5f);
            Rumble(m_Ground, 0.5f);
        }

        // Il sort de terre ; sa fonte s'allume ; il lève son marteau, la foudre le frappe ; il l'abat.
        void UpdateWake()
        {
            var t = Time.time - m_WakeAt;
            var rise = Mathf.Clamp01(t / Mathf.Max(0.1f, m_RiseTime));
            var local = transform.localPosition;
            transform.localPosition = new Vector3(local.x, -m_WakeDepth * (1f - EaseOut(rise)), local.z);

            if (rise < 1f && Time.time >= m_NextWakeFx)
            {
                // La roche qui vole, la poussière, la fonte qui jaillit autour de lui ; le sol tremble.
                m_NextWakeFx = Time.time + 0.14f;
                var around = m_Ground + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(2f, 5.5f);
                ThrowDebris(around, 3, 0.4f);
                Burst(around + Vector3.up * 0.3f, k_Dust, 26, 7f, 0.9f, 1.2f, 0.9f, false);
                Burst(around + Vector3.up * 0.2f, k_Molten, 14, 6f, 0.35f, 0.8f, 1.1f, false);
                Rumble(m_Ground, 0.3f);
            }

            if (m_WakeStep == 0 && t >= 0.5f)
            {
                m_WakeStep = 1;
                SkyStrike(0.6f);
                Lightning(m_Ground + new Vector3(Random.Range(-12f, 12f), 0f, Random.Range(-12f, 12f)), 0.4f, 2);
            }

            if (m_WakeStep == 1 && t >= m_RiseTime)
            {
                // Debout : sa fonte s'allume, la fournaise rugit et crache le feu.
                m_WakeStep = 2;
                transform.localPosition = new Vector3(local.x, 0f, local.z);
                Burst(m_Ground + Vector3.up * 0.5f, k_Dust, 80, 12f, 1.2f, 1.6f, 0.8f, false);
                Explosion(FurnacePoint, 3f);
                Sfx.Play(m_PourClip, FurnacePoint, 1f, 0.8f);
                StartLoops();
                m_GlowFlare = 1.5f;
            }

            m_Glow = t < m_RiseTime ? 0f : Mathf.Clamp01((t - m_RiseTime) / 0.8f);
            if (t >= m_RiseTime + 0.8f && t < m_RiseTime + 1.6f)
                m_ArmAngle = Mathf.Lerp(0f, k_RaisedAngle, Smooth((t - m_RiseTime - 0.8f) / 0.8f));

            if (m_WakeStep == 2 && t >= m_RiseTime + 1.45f)
            {
                // Il appelle la foudre : le grand éclair sur le marteau levé.
                m_WakeStep = 3;
                Lightning(HeadPoint, 1f, 4);
                SkyStrike(1f);
                Sfx.Play(m_ThunderClip, HeadPoint, 1f, 0.7f);
                m_GlowFlare = 2f;
            }

            if (t >= m_RiseTime + 1.6f && t < m_RiseTime + 1.85f)
                m_ArmAngle = Mathf.Lerp(k_RaisedAngle, 0f, Sq((t - m_RiseTime - 1.6f) / 0.25f));

            if (m_WakeStep == 3 && t >= m_RiseTime + 1.85f)
            {
                m_WakeStep = 4;
                m_ArmAngle = 0f;
                WakeSlam();
            }

            if (t >= m_RiseTime + 2.8f)
                FinishWake();
        }

        // Le marteau abattu au réveil : la foudre, l'onde qui renverse tout à la ronde, le sol fendu.
        void WakeSlam()
        {
            var point = HeadPoint;
            point.y = GroundY(point, 1.5f);
            Lightning(point, 0.8f, 3);
            SkyStrike(1f);
            Shockwave(point, m_WakeRadius, 0.7f);
            Cracks(point, 14, 4f, 10f, 5f);
            ThrowDebris(point, 22, 1f);
            Explosion(point + Vector3.up * 0.5f, 5f);
            Burst(point + Vector3.up * 0.6f, k_Sparks, 160, 16f, 0.3f, 1.1f, 0.8f);
            FlashAt(point + Vector3.up * 3f, k_Molten, 14f, 45f, 0.7f);
            Sfx.Play(m_BoomClip, point, 1f, 0.8f);
            Sfx.Play(m_SlamClip, point, 1f, 0.4f);
            Sfx.Play(m_ThunderClip, point, 1f, 0.9f);
            Rumble(point, 1f);
            HeadMessages.Show("LE COLOSSE DE FORGE !", k_Title, 2.5f);

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
            m_ArmAngle = 0f;
            m_NextForge = Time.time + 0.6f;
            m_Heat = 0;
            m_NextFlow = Time.time + 10f;
            StartLoops();
        }

        // ---------------------------------------------------------------- la forge (main gauche)

        // Un coup après l'autre : il lève sa masse, et l'abat sur le lingot. Le grand coup : plus haut, plus lent, la foudre sur la
        // masse levée.
        void UpdateForge()
        {
            if (m_IngotBack >= 0f && Time.time >= m_IngotBack)
            {
                // Un lingot neuf, encore sombre, sur l'enclume.
                m_IngotBack = -1f;
                if (m_Ingot != null)
                    m_Ingot.gameObject.SetActive(true);
            }

            if (m_ForgeAt < 0f)
            {
                m_ForgeAngle = 0f;
                m_Lean = 0f;
                if (Time.time < m_NextForge)
                    return;
                m_ForgeAt = Time.time;
                m_ForgeBig = m_Heat >= m_StrikesPerIngot - 1 && m_IngotBack < 0f && BigTarget(out _, out _);
                m_ForgeCharged = false;
            }

            var raise = m_ForgeBig ? k_BigRaise : k_StrikeRaise;
            var hold = m_ForgeBig ? k_BigHold : k_StrikeHold;
            var slam = m_ForgeBig ? k_BigSlam : k_StrikeSlam;
            var t = Time.time - m_ForgeAt;
            float lift;
            if (t < raise)
                lift = Smooth(t / raise);
            else if (t < raise + hold)
                lift = 1f;
            else
                lift = 1f - Sq((t - raise - hold) / slam);
            m_ForgeAngle = (m_ForgeBig ? k_ForgeBigRaised : k_ForgeRaised) * lift;
            m_Lean = (m_ForgeBig ? -4f : -2f) * lift;

            if (m_ForgeBig && !m_ForgeCharged && t >= raise * 0.9f)
                ChargeForge();
            if (t < raise + hold + slam)
                return;

            m_ForgeAngle = 0f;
            m_Lean = 0f;
            m_ForgeAt = -1f;
            Strike(m_ForgeBig);

            // Le coup suivant : à son rythme (les parchemins d'adresse, la surcharge l'accélèrent), plus lent sans personne à portée.
            var interval = m_ForgeInterval / Mathf.Max(0.1f, OverchargeRate);
            if (!InArena && EnemiesNear(transform.position, m_Range * RangeScale).Count == 0)
                interval *= m_ForgeIdleSlow;
            m_NextForge = Time.time + Mathf.Max(0.08f, interval - (k_StrikeRaise + k_StrikeHold + k_StrikeSlam));
        }

        // La cible du grand coup : le groupe le plus serré à sa portée ; dans l'arène, MK, si le lingot peut le plaquer.
        bool BigTarget(out Enemy enemy, out DragonBoss dragon)
        {
            enemy = null;
            dragon = null;
            if (InArena)
            {
                dragon = ArenaDragon();
                return dragon != null && Time.time >= m_NextPin;
            }

            enemy = Densest();
            return enemy != null;
        }

        // MK, s'il est là et en vie.
        static DragonBoss ArenaDragon()
        {
            var dragon = DragonBoss.Instance;
            if (dragon == null || !dragon.isActiveAndEnabled || dragon.IsHidden)
                return null;
            var enemy = dragon.GetComponentInParent<Enemy>();
            return enemy != null && enemy.IsAlive ? dragon : null;
        }

        // L'ennemi au sol à sa portée qui a le plus de voisins (à 4 m).
        Enemy Densest()
        {
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(transform.position, m_Range * RangeScale));
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

            return best;
        }

        // En haut du grand geste, la foudre frappe la masse levée (12.47 : « un éclair à chaque coup de marteau »).
        void ChargeForge()
        {
            m_ForgeCharged = true;
            var head = ForgeHeadPoint;
            Lightning(head, 0.45f, 2);
            SkyFlash(m_StrikeFlash * 0.6f, true);
            Sfx.Play(m_ThunderClip, head, 0.6f, Random.Range(1.1f, 1.3f));
            m_GlowFlare = Mathf.Max(m_GlowFlare, 1f);
        }

        // La masse tombe sur le lingot : les étincelles, le fer qui sonne, plus aigu à mesure qu'il chauffe ; un éclat de fonte part.
        // Le grand coup : l'onde autour de l'enclume, et le lingot chauffé à blanc part.
        void Strike(bool big)
        {
            var point = IngotPoint;
            var heat = Mathf.Clamp01((m_Heat + 1f) / Mathf.Max(1, m_StrikesPerIngot));
            Sparks(point, big ? 140 : 24 + Mathf.RoundToInt(30f * heat));
            Sfx.Play(m_ClangClip, point, big ? 1f : 0.7f, big ? 0.9f : 1.15f + 0.2f * heat + Random.Range(-0.03f, 0.03f));
            m_GlowFlare = Mathf.Max(m_GlowFlare, big ? 1.5f : 0.3f);
            if (!big)
            {
                m_Heat = Mathf.Min(m_Heat + 1, Mathf.Max(1, m_StrikesPerIngot) - 1);
                LaunchChunk(point);
                return;
            }

            Sfx.Play(m_CatchClip, point, 1f, 0.6f);
            Sfx.Play(m_BoomClip, point, 0.7f, 1.2f);
            Shockwave(point, 9f * Mathf.Max(0.3f, transform.lossyScale.y), 0.4f);
            FlashAt(point + Vector3.up * 2f, k_Molten, 8f, 25f, 0.4f);
            Rumble(point, 0.5f);

            // La cible a pu changer pendant le geste ; plus personne : il garde son lingot chauffé à blanc pour le coup suivant.
            if (!BigTarget(out var enemy, out var dragon))
                return;
            m_Heat = 0;
            LaunchIngot(point, enemy, dragon);
        }

        // Un éclat de fonte saute du lingot sur un ennemi au sol à sa portée, au hasard (dans l'arène : sur MK).
        void LaunchChunk(Vector3 from)
        {
            if (InArena)
            {
                var dragon = ArenaDragon();
                if (dragon != null)
                    Throw(ShotKind.Chunk, from, null, dragon, dragon.BodyPoint, true);
                return;
            }

            var near = EnemiesNear(transform.position, m_Range * RangeScale);
            if (near.Count == 0)
                return;
            var enemy = near[Random.Range(0, near.Count)];
            Throw(ShotKind.Chunk, from, enemy, null, enemy.transform.position, false);
        }

        // Le grand coup : le lingot chauffé à blanc part en cloche ; un neuf le remplace sur l'enclume après sa chute.
        void LaunchIngot(Vector3 from, Enemy enemy, DragonBoss dragon)
        {
            var target = dragon != null ? dragon.BodyPoint : enemy.transform.position;
            var shot = Throw(ShotKind.Ingot, from, enemy, dragon, target, dragon != null);
            if (shot == null)
                return;
            if (m_Ingot != null)
                m_Ingot.gameObject.SetActive(false);
            m_IngotBack = Time.time + shot.Duration + 0.6f;
        }

        // ---------------------------------------------------------------- les marteaux (main droite)

        // Une flèche tirée qui touche (celles de la pluie de flèches et du multishot comprises) : un marteau de plus à lancer. Sur la
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

            var flat = hit.Point - transform.position;
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

        void Enqueue(Enemy enemy, DragonBoss dragon, Vector3 point, float power)
        {
            // La file pleine, la plus faible s'efface (3 novembre : un marteau frappe selon la force de sa flèche,
            // TowerUltimate.ShotPower) ; à force égale, la plus ancienne : il suit tes derniers tirs.
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

        // L'ennemi au sol le plus proche de ce point, à ce rayon au plus.
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

        // Le bras du marteau : levé tant que des flèches attendent ; un coup sec par marteau, au plus un toutes les Throw Interval s
        // (d'un lâcher au suivant) ; sans flèche depuis un moment, il le repose.
        void UpdateBarrage()
        {
            while (m_Orders.Count > 0 && Time.time - m_Orders[0].At > k_OrderLife)
                m_Orders.RemoveAt(0);

            var busy = m_Orders.Count > 0 || m_SnapAt >= 0f;
            if (busy)
            {
                m_Raise = Mathf.MoveTowards(m_Raise, 1f, Time.deltaTime / 0.3f);
                if (m_Raise >= 1f && !m_Called)
                    CallLightning();
            }
            else if (Time.time - m_LastThrow > 0.9f)
            {
                m_Raise = Mathf.MoveTowards(m_Raise, 0f, Time.deltaTime / 0.6f);
            }

            if (m_Raise <= 0f)
                m_Called = false;

            var interval = Mathf.Max(0.1f, m_ThrowInterval);
            var snap = 0f;
            if (m_SnapAt >= 0f)
            {
                var s = (Time.time - m_SnapAt) / interval;
                if (!m_Snapped && s >= 0.5f)
                {
                    m_Snapped = true;
                    ThrowNext();
                }

                if (s >= 1f)
                    m_SnapAt = -1f;
                else
                    snap = Mathf.Sin(Mathf.PI * Mathf.Clamp01(s));
            }
            else if (m_Raise >= 1f && m_Orders.Count > 0 && Time.time >= m_LastThrow + interval * 0.5f)
            {
                // Le coup sec dure un intervalle, le marteau part en son milieu : un lâcher tous les intervalles au plus.
                m_SnapAt = Time.time;
                m_Snapped = false;
                AimArm(OrderPoint(m_Orders[0]));
            }

            m_ArmAngle = Mathf.Lerp(0f, k_RaisedAngle, Smooth(m_Raise)) + (k_SnapAngle - k_RaisedAngle) * snap;
            if (m_Raise <= 0f)
                m_ArmYawGoal = 0f;
            m_ArmYaw = Mathf.MoveTowardsAngle(m_ArmYaw, m_ArmYawGoal, 500f * Time.deltaTime);
        }

        // Le bras du marteau se tourne, à l'épaule, vers ce point (dans les limites de l'épaule).
        void AimArm(Vector3 point)
        {
            if (m_Arm == null || m_Arm.parent == null)
                return;
            var local = m_Arm.parent.InverseTransformPoint(point) - m_Arm.localPosition;
            if (local.x * local.x + local.z * local.z > 0.25f)
                m_ArmYawGoal = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, k_MinYaw, k_MaxYaw);
        }

        // Le marteau levé pour une salve : il appelle la foudre, qui le frappe (au plus toutes les 4 s) ; ses marteaux partent chargés.
        void CallLightning()
        {
            m_Called = true;
            if (Time.time < m_NextCall)
                return;

            m_NextCall = Time.time + 4f;
            var head = HeadPoint;
            Lightning(head, 0.45f, 2);
            SkyFlash(m_StrikeFlash * 0.6f, false);
            Sfx.Play(m_ThunderClip, head, 0.6f, Random.Range(1.1f, 1.3f));
            m_GlowFlare = Mathf.Max(m_GlowFlare, 1f);
        }

        // Au bout du coup sec, un marteau part de son poing (une copie du sien, aussi grande) vers la flèche la plus ancienne.
        void ThrowNext()
        {
            if (m_Orders.Count == 0)
                return;

            var order = m_Orders[0];
            m_Orders.RemoveAt(0);
            m_LastThrow = Time.time;
            var air = order.Dragon != null || (order.Enemy != null && order.Enemy.IsFlyer);
            var from = m_Hammer != null ? m_Hammer.position : HeadPoint;
            var shot = Throw(ShotKind.Hammer, from, order.Enemy, order.Dragon, OrderPoint(order), air);
            if (shot != null)
                shot.Power = order.Power;
            Sfx.Play(m_ThrowClip, HeadPoint, 0.8f, Random.Range(0.7f, 0.85f));
        }

        // ---------------------------------------------------------------- ce qu'il lance

        // Il lance un marteau, un éclat ou le lingot, en cloche, vers ce point (qui suit sa cible).
        Shot Throw(ShotKind kind, Vector3 from, Enemy enemy, DragonBoss dragon, Vector3 point, bool air)
        {
            var shot = TakeShot(kind);
            if (shot == null)
                return null;

            var scale = Mathf.Max(0.3f, transform.lossyScale.y);
            var flat = point - from;
            flat.y = 0f;
            var distance = flat.magnitude;
            shot.Enemy = enemy;
            shot.Dragon = dragon;
            shot.Air = air;
            shot.From = from;
            shot.Point = point;
            shot.Born = Time.time;
            shot.LandedAt = -1f;
            shot.NextCrackle = 0f;
            shot.Power = 1f;
            shot.SpinAxis = distance > 0.01f ? Vector3.Cross(Vector3.up, flat / distance) : transform.right;
            Quaternion rotation;
            switch (kind)
            {
                case ShotKind.Hammer:
                    // Le vol du marteau d'avant (12.47) : il tourne sur lui-même, haut, puis se remet tête en bas pour frapper.
                    shot.Duration = 0.35f + distance / 40f;
                    shot.Arc = 3f * scale + distance * 0.25f;
                    shot.Spin = 900f;
                    shot.HeadOffset = (m_Hammer != null && m_HammerHead != null ? Vector3.Distance(m_Hammer.position, m_HammerHead.position)
                        : 4.6f * scale) * m_HammerScale;
                    rotation = m_Hammer != null ? m_Hammer.rotation : Quaternion.identity;
                    break;
                case ShotKind.Chunk:
                    shot.Duration = 0.5f + distance / 30f;
                    shot.Arc = 4f * scale + distance * 0.3f;
                    shot.Spin = 600f;
                    rotation = Random.rotation;
                    break;
                default:
                    shot.Duration = 0.8f + distance / 28f;
                    shot.Arc = 9f * scale + distance * 0.35f;
                    shot.Spin = 280f;
                    rotation = m_Ingot != null ? m_Ingot.rotation : transform.rotation;
                    break;
            }

            shot.Go.transform.SetPositionAndRotation(from, rotation);
            shot.Go.SetActive(true);
            if (shot.Trail != null)
            {
                shot.Trail.Clear();
                shot.Trail.emitting = true;
            }

            shot.Flying = true;
            return shot;
        }

        Shot TakeShot(ShotKind kind)
        {
            foreach (var shot in m_Shots)
            {
                if (shot.Kind == kind && !shot.InUse && shot.Go != null)
                    return shot;
            }

            var made = MakeShot(kind);
            if (made != null)
                m_Shots.Add(made);
            return made;
        }

        // Un objet à lancer de plus : une copie de son marteau (Hammer Scale : aussi grande), un éclat ou un lingot de fonte ; une
        // traînée de feu.
        Shot MakeShot(ShotKind kind)
        {
            GameObject go;
            Transform trailAt;
            float width;
            var scale = Mathf.Max(0.3f, transform.lossyScale.y);
            if (kind == ShotKind.Hammer)
            {
                if (m_Hammer == null)
                    return null;
                go = Instantiate(m_Hammer.gameObject);
                go.name = "Marteau lancé du Colosse";
                go.transform.localScale = m_Hammer.lossyScale * m_HammerScale;
                foreach (var collider in go.GetComponentsInChildren<Collider>(true))
                    Destroy(collider);
                // Les gouttes de fonte du sien restent au sien.
                foreach (var particles in go.GetComponentsInChildren<ParticleSystem>(true))
                    Destroy(particles.gameObject);
                var head = m_HammerHead != null ? FindChild(go.transform, m_HammerHead.name) : null;
                trailAt = head != null ? head : go.transform;
                width = 1.4f * m_Hammer.lossyScale.y * m_HammerScale;
            }
            else
            {
                if (m_MoltenMaterial == null)
                    return null;
                var ingot = kind == ShotKind.Ingot;
                go = Primitive(PrimitiveType.Cube, ingot ? "Lingot chauffé à blanc" : "Éclat de fonte", m_MoltenMaterial);
                go.transform.localScale = !ingot ? new Vector3(0.7f, 0.45f, 0.9f) * scale
                    : m_Ingot != null ? m_Ingot.lossyScale * 1.5f : new Vector3(2.2f, 0.5f, 0.9f) * scale;
                SetMolten(go.GetComponent<Renderer>(), 0f, ingot ? 2.6f : 1.8f, 0f);
                trailAt = go.transform;
                width = (ingot ? 1.4f : 0.5f) * scale;
            }

            go.SetActive(false);
            var time = kind == ShotKind.Hammer ? 0.25f : kind == ShotKind.Chunk ? 0.3f : 0.5f;
            return new Shot { Kind = kind, Go = go, Trail = AddTrail(trailAt, width, time) };
        }

        // Ce qui vole suit sa cible, tourne sur lui-même, et frappe en bout de course ; les marteaux et le lingot, chargés par la
        // foudre, crépitent. Un marteau reste planté 0,3 s, la tête dans le sol, puis s'y enfonce.
        void UpdateShots()
        {
            var scale = Mathf.Max(0.3f, transform.lossyScale.y);
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
                    if (since > 0.3f)
                        body.position += Vector3.down * (shot.HeadOffset * 4.8f * Time.deltaTime);
                    if (since < 0.65f)
                        continue;
                    shot.LandedAt = -1f;
                    shot.Go.SetActive(false);
                    continue;
                }

                if (shot.Dragon != null)
                    shot.Point = shot.Dragon.BodyPoint;
                else if (shot.Enemy != null && shot.Enemy.IsAlive)
                    shot.Point = shot.Air ? shot.Enemy.Center : shot.Enemy.transform.position;

                var hammer = shot.Kind == ShotKind.Hammer;
                var pose = Quaternion.identity;
                var end = hammer ? HammerEnd(shot, out pose) : shot.Point;
                var t = Mathf.Clamp01((Time.time - shot.Born) / Mathf.Max(0.05f, shot.Duration));
                body.position = Vector3.Lerp(shot.From, end, t) + Vector3.up * (shot.Arc * 4f * t * (1f - t));
                if (hammer && t >= 0.8f)
                    body.rotation = Quaternion.Slerp(body.rotation, pose, Time.deltaTime * 20f);
                else
                    body.Rotate(shot.SpinAxis, shot.Spin * Time.deltaTime, Space.World);
                if (shot.Kind != ShotKind.Chunk && Time.time >= shot.NextCrackle)
                {
                    shot.NextCrackle = Time.time + (hammer ? 0.12f : 0.07f);
                    var at = hammer ? body.position - body.up * shot.HeadOffset : body.position;
                    LightningBolt.Burst(at, (hammer ? 1.6f * m_HammerScale : 2f) * scale, m_BoltColor, null, hammer ? 1 : 2);
                }

                if (t < 1f)
                    continue;

                shot.Flying = false;
                if (shot.Trail != null)
                    shot.Trail.emitting = false;
                if (hammer && !shot.Air)
                {
                    body.SetPositionAndRotation(end, pose);
                    shot.LandedAt = Time.time;
                }
                else
                {
                    shot.Go.SetActive(false);
                }

                Land(shot);
            }
        }

        // Où est le repère d'un marteau à son coup : la tête sur la cible (posée au sol, ou dedans en l'air), le manche vers le haut,
        // le long de sa tête dans le sens du vol.
        Vector3 HammerEnd(Shot shot, out Quaternion rotation)
        {
            var flat = shot.Point - shot.From;
            flat.y = 0f;
            rotation = Quaternion.LookRotation(flat.sqrMagnitude > 1e-4f ? flat.normalized : transform.forward, Vector3.up);
            var half = shot.Air ? 0f : 0.8f * (m_Hammer != null ? m_Hammer.lossyScale.y : 1f) * m_HammerScale;
            return shot.Point + Vector3.up * (shot.HeadOffset + half);
        }

        void Land(Shot shot)
        {
            switch (shot.Kind)
            {
                case ShotKind.Hammer:
                    HammerImpact(shot.Point, shot.Air, shot.Dragon, shot.Power);
                    break;
                case ShotKind.Chunk:
                    ChunkImpact(shot.Point, shot.Air, shot.Dragon);
                    break;
                default:
                    if (shot.Dragon != null)
                        PinDragon(shot.Dragon);
                    else
                        Impact(shot.Point, shot.Air);
                    break;
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

        // Les dégâts d'un coup : moitié au bord ; ceux qui survivent volent en vrille. Un boss des vagues prend les mêmes dégâts (le
        // joueur, 12.50 : « les dégâts de la tour devraient aussi marcher sur les boss »), et en plus une part de ses PV max (12.44) ;
        // il n'est jamais projeté. MK, dans l'arène, ne passe pas par ici : il garde sa part de PV seulement.
        void Smash(Vector3 point, bool air, float damage, float radius, float push, float lift, float bossShare)
        {
            m_Scratch.Clear();
            foreach (var enemy in AliveEnemies())
            {
                if (enemy != null && enemy.IsAlive && Near(enemy, point, radius, air))
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

        // Un marteau frappe (le joueur, 12.48 : « bien plus imposants, comme la précédente version, et garder les mêmes dégâts ») :
        // la foudre tombe avec lui, l'onde de choc, l'explosion, la poussière en anneau ; tout ce qui survit vole en vrille. Le coup
        // complet (l'éclat de lumière, le tonnerre, la roche qui vole, le sol fendu et en feu, les manettes) au plus toutes les Heavy
        // Interval s : il en tombe jusqu'à 5 par seconde, et le casque doit suivre (les éclairs et les explosions sont réutilisés). Ses
        // dégâts (et sa part des PV d'un boss) suivent la force de la flèche qui l'a lancé (power, 3 novembre).
        void HammerImpact(Vector3 point, bool air, DragonBoss dragon, float power)
        {
            // MK garde sa part de PV seulement (le joueur, 12.50 : « pas MK, on garde son effet actuel »).
            if (dragon != null)
                HurtShare(dragon.GetComponentInParent<Enemy>(), m_HammerBossShare * power, point);
            else
                Smash(point, air, m_HammerDamage * power, m_HammerRadius, m_HammerPush, m_HammerLift, m_HammerBossShare * power);

            var heavy = Time.time >= m_NextHeavy;
            if (heavy)
                m_NextHeavy = Time.time + m_HeavyInterval;
            var scale = Mathf.Max(0.3f, transform.lossyScale.y);
            Lightning(point, 0.6f, 1, heavy);
            if (heavy)
                SkyFlash(m_StrikeFlash * 0.7f, false);
            Sparks(point + Vector3.up * (0.6f * scale), 80);
            Explosion(point + Vector3.up * 0.5f, 3f * scale);
            Sfx.Play(m_SlamClip, point, 0.9f, Random.Range(0.5f, 0.62f));
            Sfx.Play(m_BoomClip, point, heavy ? 0.75f : 0.5f, Random.Range(1f, 1.2f));
            if (heavy)
            {
                Sfx.Play(m_ThunderClip, point, 0.7f, Random.Range(0.95f, 1.15f));
                Rumble(point, 0.6f);
            }

            if (air || dragon != null)
                return;

            Shockwave(point, m_HammerRadius * 2f, 0.4f);
            DustRing(point, m_HammerRadius * 0.6f, 4);
            if (!heavy)
                return;
            ThrowDebris(point, 8, 0.6f);
            Cracks(point, 6, 2f, 5f, 3f);
            Sfx.Play(m_SizzleClip, point, 0.6f, Random.Range(0.9f, 1.1f));
            if (m_MoltenMaterial != null)
                GroundPatch.Spawn(point, m_ScorchRadius, m_ScorchTime, 1f, m_MoltenMaterial, this).Ignite(m_ScorchBurn * Strength, m_ScorchTime);
        }

        // Un éclat de la forge touche : les dégâts autour (un boss, MK : une part de ses PV), les étincelles, la poussière, le
        // grésillement. Peu de chose : il en tombe un à chaque coup de masse.
        void ChunkImpact(Vector3 point, bool air, DragonBoss dragon)
        {
            if (dragon != null)
                HurtShare(dragon.GetComponentInParent<Enemy>(), m_SmallBossShare, point);
            else
                Smash(point, air, m_ChunkDamage, m_ChunkRadius, 0f, 0f, m_SmallBossShare);

            var scale = Mathf.Max(0.3f, transform.lossyScale.y);
            Sparks(point + Vector3.up * (0.4f * scale), 24);
            if (!air)
                Dust(point + Vector3.up * (0.2f * scale), 5);
            Sfx.Play(m_SizzleClip, point, 0.4f, Random.Range(1.1f, 1.3f));
        }

        // Le grand coup : le lingot chauffé à blanc tombe, la foudre avec lui ; au sol, l'onde, la roche qui vole, le sol fendu de
        // fonte, et tout autour vole en l'air ; en l'air (un volant), autour de lui.
        void Impact(Vector3 point, bool air)
        {
            Smash(point, air, m_Damage, m_Radius, m_Push, m_Lift, m_BossShare);
            Lightning(point, 0.7f, 3);
            SkyFlash(m_StrikeFlash, true);
            Burst(point + Vector3.up * 0.6f, k_Sparks, 110, 15f, 0.28f, 1f, 0.9f);
            FlashAt(point + Vector3.up * 2.5f, k_Molten, 9f, 32f, 0.5f);
            Sfx.Play(m_SlamClip, point, 1f, Random.Range(0.45f, 0.55f));
            Sfx.Play(m_BoomClip, point, 0.85f, Random.Range(0.95f, 1.1f));
            Sfx.Play(m_ThunderClip, point, 0.9f, Random.Range(0.9f, 1.05f));
            Rumble(point, 0.8f);
            if (air)
                return;

            // Au sol : l'onde de choc, la poussière en anneau, la roche qui vole, le feu, le sol fendu de fonte qui grésille.
            Shockwave(point, m_Radius * 2.2f, 0.45f);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f;
                var at = point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * m_Radius * 0.6f;
                Burst(at + Vector3.up * 0.2f, k_Dust, 26, 10f, 0.9f, 1.2f, 1.2f, false);
            }

            ThrowDebris(point, 14, 0.7f);
            Explosion(point + Vector3.up * 0.5f, 3.5f);
            Cracks(point, 8, 2.5f, 6f, 3.5f);
            Sfx.Play(m_SizzleClip, point, 0.8f, Random.Range(0.85f, 1.05f));
            if (m_MoltenMaterial != null)
                GroundPatch.Spawn(point, m_ScorchRadius, m_ScorchTime, 1f, m_MoltenMaterial, this).Ignite(m_ScorchBurn * Strength, m_ScorchTime);
        }

        // Un boss des vagues sous un coup du Colosse : ses dégâts, comme les autres (12.50), puis la part de ses PV max (12.44 ;
        // celle-ci ne le tue jamais).
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

        // Dans l'arène : le lingot plaque MK au sol (12.44 : « il plaque MK au sol »), la foudre avec lui.
        void PinDragon(DragonBoss dragon)
        {
            var point = dragon.BodyPoint;
            HurtShare(dragon.GetComponentInParent<Enemy>(), m_PinBossShare, point);
            if (dragon.Pin(m_PinTime))
            {
                m_NextPin = Time.time + m_PinInterval;
                HeadMessages.Show("Plaqué au sol !", k_Title, 2f);
            }

            Lightning(point, 0.7f, 3);
            Burst(point, k_Sparks, 100, 15f, 0.3f, 1f, 0.8f);
            Burst(point, k_Dust, 40, 8f, 0.9f, 1.2f, 0.6f);
            FlashAt(point + Vector3.up * 2f, k_Molten, 8f, 30f, 0.5f);
            Sfx.Play(m_SlamClip, point, 1f, 0.4f);
            Sfx.Play(m_BoomClip, point, 0.9f, 1f);
            Sfx.Play(m_ThunderClip, point, 0.9f, 1f);
            ArenaPlayer.Rumble(0.7f, 0.3f);
        }

        // Le buste qui se penche, le bras du marteau (tourné à l'épaule vers sa cible), le bras de la forge.
        void Animate()
        {
            if (m_Torso != null)
                m_Torso.localRotation = m_TorsoRest * Quaternion.Euler(m_Lean, 0f, 0f);
            if (m_Arm != null)
                m_Arm.localRotation = Quaternion.Euler(0f, m_ArmYaw, 0f) * m_ArmRest * Quaternion.Euler(m_ArmAngle, 0f, 0f);
            if (m_LeftArm != null)
                m_LeftArm.localRotation = m_LeftArmRest * Quaternion.Euler(m_ForgeAngle, 0f, 0f);
        }

        // Une traînée de feu derrière ce qu'il lance.
        static TrailRenderer AddTrail(Transform at, float width, float time)
        {
            var trail = at.gameObject.AddComponent<TrailRenderer>();
            trail.time = time;
            trail.minVertexDistance = 0.2f;
            trail.widthMultiplier = width;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f), new GradientColorKey(k_Molten, 0.4f),
                                     new GradientColorKey(new Color(0.6f, 0.1f, 0.02f), 1f) },
                             new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.sharedMaterial = TrailMaterial;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            return trail;
        }

        // Les étincelles et la poussière des coups : deux systèmes de particules à lui, déplacés là où ça frappe (pas un objet de
        // plus par coup, il en tombe beaucoup).
        void Sparks(Vector3 point, int count) => EmitAt(ref m_SparkFx, true, point, count);

        void Dust(Vector3 point, int count) => EmitAt(ref m_DustFx, false, point, count);

        // La poussière en anneau autour d'un coup.
        void DustRing(Vector3 point, float radius, int count)
        {
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f;
                Dust(point + new Vector3(Mathf.Cos(angle) * radius, 0.3f, Mathf.Sin(angle) * radius), count);
            }
        }

        void EmitAt(ref ParticleSystem system, bool sparks, Vector3 point, int count)
        {
            if (system == null)
                system = MakeEmitter(sparks);
            if (system == null)
                return;
            system.transform.position = point;
            system.Emit(count);
        }

        ParticleSystem MakeEmitter(bool sparks)
        {
            var arrows = SpecialArrows.Instance;
            var material = arrows != null ? arrows.EffectMaterial : null;
            if (material == null)
                return null;

            var go = new GameObject(sparks ? "Étincelles des coups" : "Poussière des coups");
            go.transform.SetParent(transform, false);
            // La poussière part en demi-sphère, vers le haut.
            if (!sparks)
                go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = sparks ? new ParticleSystem.MinMaxCurve(0.35f, 0.9f) : new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = sparks ? new ParticleSystem.MinMaxCurve(4f, 15f) : new ParticleSystem.MinMaxCurve(1.5f, 5f);
            main.startSize = sparks ? new ParticleSystem.MinMaxCurve(0.1f, 0.28f) : new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
            main.startColor = sparks ? new ParticleSystem.MinMaxGradient(k_Sparks, new Color(1f, 0.95f, 0.8f, 1f))
                : new ParticleSystem.MinMaxGradient(k_Dust, k_Smoke);
            main.gravityModifier = sparks ? 0.9f : -0.05f;
            main.maxParticles = sparks ? 800 : 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = sparks ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Hemisphere;
            shape.radius = sparks ? 0.3f : 0.6f;
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colors.color = fade;
            if (!sparks)
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

        // ---------------------------------------------------------------- sa fonte qui luit, ses particules, sa lumière

        // L'éclat de la fonte du géant : il palpite, et s'emballe un instant (l'éveil, la foudre, la coulée).
        void UpdateGlow()
        {
            m_GlowFlare = Mathf.MoveTowards(m_GlowFlare, 0f, Time.deltaTime * 1.2f);
            var pouring = m_PourUntil >= 0f;
            var flicker = 1f + 0.08f * Mathf.Sin(Time.time * 7.3f) + 0.05f * Mathf.Sin(Time.time * 17.1f);
            var glow = m_Glow * flicker * (1f + m_GlowFlare * 0.6f + (pouring ? 0.5f : 0f));
            ApplyGlow(glow);

            if (m_FurnaceLight != null)
                m_FurnaceLight.intensity = 2.4f * glow;

            // Le lingot : rouge sombre, croûté, à son arrivée ; chauffé à blanc au coup d'avant le grand.
            if (m_IngotRenderer != null)
            {
                var heat = Mathf.Clamp01(m_Heat / (float)Mathf.Max(1, m_StrikesPerIngot - 1));
                SetMolten(m_IngotRenderer, Mathf.Lerp(0.55f, 0f, heat), Mathf.Lerp(0.7f, 2.4f, heat) * m_Glow * flicker, 0f);
            }
        }

        void ApplyGlow(float glow)
        {
            foreach (var renderer in m_GlowRenderers)
            {
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(m_Block);
                m_Block.SetFloat(k_GlowId, glow);
                renderer.SetPropertyBlock(m_Block);
            }
        }

        // Les particules qui ne s'arrêtent pas : les braises de la fournaise, la fumée et les étincelles des cheminées, les gouttes
        // de fonte du marteau ; et la lumière de la fournaise.
        void StartLoops()
        {
            if (m_Loops.Count > 0)
                return;

            if (m_Furnace != null)
            {
                Loop("Braises de la fournaise", m_Furnace, Vector3.zero, 16f, new Vector2(0.5f, 1.6f), new Vector2(0.06f, 0.16f),
                     new Vector2(1.6f, 2.6f), -0.25f, k_Sparks, k_Molten, 0.9f, 0f, 1f);
                if (m_FurnaceLight == null)
                {
                    var go = new GameObject("Lumière de la fournaise");
                    go.transform.SetParent(m_Furnace, false);
                    go.transform.localPosition = Vector3.forward * 1.2f;
                    m_FurnaceLight = go.AddComponent<Light>();
                    m_FurnaceLight.type = LightType.Point;
                    m_FurnaceLight.color = new Color(1f, 0.5f, 0.18f);
                    m_FurnaceLight.range = 16f;
                    m_FurnaceLight.shadows = LightShadows.None;
                }
            }

            foreach (var chimney in m_Chimneys)
            {
                if (chimney == null)
                    continue;
                Loop("Fumée", chimney, new Vector3(-90f, 0f, 0f), 5f, new Vector2(1.5f, 3f), new Vector2(1.2f, 2.4f),
                     new Vector2(3f, 4.5f), -0.03f, k_Smoke, new Color(0.18f, 0.16f, 0.16f, 1f), 0.3f, 12f, 2.5f);
                Loop("Étincelles", chimney, new Vector3(-90f, 0f, 0f), 4f, new Vector2(3f, 5.5f), new Vector2(0.05f, 0.12f),
                     new Vector2(1f, 1.8f), 0.3f, k_Sparks, k_Molten, 0.3f, 20f, 1f);
            }

            if (m_HammerHead != null)
                Loop("Gouttes de fonte", m_HammerHead, Vector3.zero, 6f, new Vector2(0.2f, 0.8f), new Vector2(0.05f, 0.11f),
                     new Vector2(0.8f, 1.4f), 0.6f, k_Sparks, k_Molten, 0.9f, 0f, 1f);
        }

        // Un système de particules qui tourne en boucle, attaché à cette pièce (dans le monde : il laisse sa traînée).
        void Loop(string name, Transform parent, Vector3 euler, float rate, Vector2 speed, Vector2 size, Vector2 life, float gravity,
                  Color from, Color to, float radius, float coneAngle, float grow)
        {
            var arrows = SpecialArrows.Instance;
            var material = arrows != null ? arrows.EffectMaterial : null;
            if (material == null)
                return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startColor = new ParticleSystem.MinMaxGradient(from, to);
            main.gravityModifier = gravity;
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = rate;
            var shape = particles.shape;
            shape.shapeType = coneAngle > 0f ? ParticleSystemShapeType.Cone : ParticleSystemShapeType.Sphere;
            shape.angle = coneAngle;
            shape.radius = radius;
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
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

        // ---------------------------------------------------------------- le ciel de forge

        // Passager (le joueur, 12.48 : « cela doit être temporaire ») : pendant l'éveil, puis le temps d'une coulée ; le reste du
        // temps, et dans l'arène de MK, le ciel est celui de la partie.
        void UpdateSky()
        {
            var sky = SkyController.Instance;
            if (sky == null)
                return;

            var target = InArena ? 0f : m_WakeAt >= 0f ? 1f : m_PourUntil >= 0f || m_FlowAt >= 0f ? m_SkyFlow : 0f;
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

        // Un éclair dans le ciel de la partie (rien dans l'arène) : au plus un toutes les Sky Flash Interval s, sauf ceux de la masse
        // levée et du grand coup (force), qui passent toujours.
        void SkyFlash(float strength, bool force)
        {
            if (InArena || (!force && Time.time < m_NextSkyFlash))
                return;
            SkyStrike(strength);
            m_NextSkyFlash = Time.time + m_SkyFlashInterval;
        }

        static void SkyStrike(float strength)
        {
            var sky = SkyController.Instance;
            if (sky != null)
                sky.Strike(strength);
        }

        // ---------------------------------------------------------------- la coulée de fonte

        // Assez d'ennemis au sol sur la voie la plus proche, entre lui et la forêt : la fonte déborde de la poitrine.
        void TryFlow()
        {
            var lane = LaneNearest(transform.position, null, out var along, out var distance);
            if (lane == null || distance > m_Range * RangeScale)
            {
                m_NextFlow = Time.time + 2f;
                return;
            }

            var count = 0;
            foreach (var enemy in GroundEnemies())
            {
                var gap = lane.DistanceTo(enemy.transform.position, out var at);
                if (gap <= m_FlowWidth * 0.5f + 1f && at <= along + 2f && at >= along - m_FlowLength)
                    count++;
            }

            if (count < m_FlowMinEnemies)
            {
                m_NextFlow = Time.time + 1f;
                return;
            }

            m_FlowLane = lane;
            m_FlowStart = along;
            m_FlowHit.Clear();
            m_PourUntil = Time.time + m_PourTime;
            m_NextFlow = Time.time + m_FlowInterval / Mathf.Max(0.1f, OverchargeRate);
            HeadMessages.Show("La coulée de fonte !", k_Title, 2.5f);
            Sfx.Play(m_PourClip, FurnacePoint, 1f, 0.9f);
            Explosion(FurnacePoint, 2.5f);
            Burst(FurnacePoint, k_Sparks, 80, 9f, 0.2f, 1f, 0.6f);
            SkyStrike(0.5f);
            m_GlowFlare = 1.5f;
            FlowLight(FurnacePoint, 6f);
        }

        // Le torrent de fonte coule de la fournaise jusqu'au sol, sur la voie : il éclabousse, fume, flambe.
        void UpdatePour()
        {
            if (m_PourUntil < 0f)
                return;

            var landing = m_FlowLane.PointAt(Mathf.Clamp(m_FlowStart, 0f, m_FlowLane.Length), out _);
            landing.y = GroundY(landing, 1.5f);
            m_FlowSoundAt = landing;
            var from = FurnacePoint;
            if (m_Jet == null && m_MoltenMaterial != null)
            {
                // La fonte touche le sol : il grésille.
                Sfx.Play(m_SizzleClip, landing, 1f, 0.8f);
                m_Jet = Primitive(PrimitiveType.Cylinder, "Torrent de fonte", m_MoltenMaterial).transform;
            }

            if (m_Jet != null)
            {
                // Il s'épaissit en coulant, puis s'amincit à la fin.
                var left = m_PourUntil - Time.time;
                var width = Mathf.Lerp(0.3f, 1.9f, Mathf.Clamp01((m_PourTime - left) / 0.35f)) * Mathf.Clamp01(left / 0.3f + 0.25f);
                var span = landing - from;
                m_Jet.position = (from + landing) * 0.5f;
                m_Jet.rotation = Quaternion.FromToRotation(Vector3.up, span.sqrMagnitude > 1e-4f ? span.normalized : Vector3.down);
                m_Jet.localScale = new Vector3(width, span.magnitude * 0.5f, width) * Mathf.Max(0.2f, transform.lossyScale.y);
                SetMolten(m_Jet.GetComponent<Renderer>(), 0f, 1.7f, 0f);
            }

            if (Time.time >= m_NextFlowFx)
            {
                m_NextFlowFx = Time.time + 0.06f;
                Burst(landing + Vector3.up * 0.3f, k_Molten, 22, 8f, 0.5f, 0.8f, 1f, false);
                Burst(landing + Vector3.up * 1.5f, k_Smoke, 6, 3f, 1.6f, 2f, -0.15f);
            }

            if (Time.time >= m_NextFrontFire)
            {
                m_NextFrontFire = Time.time + 0.3f;
                Explosion(landing + Vector3.up * 0.4f, 2.2f);
            }

            FlowLight(Vector3.Lerp(from, landing, 0.7f), 6f);
            if (Time.time < m_PourUntil)
                return;

            // Elle a fini de verser : la rivière part le long de la voie, sa vague en tête.
            m_PourUntil = -1f;
            m_FlowAt = 0f;
            m_NextSection = 0f;
            if (m_Jet != null)
                Destroy(m_Jet.gameObject);
            m_Jet = null;
            m_River = NewRiver(m_FlowLane, m_FlowStart);
            if (m_MoltenMaterial != null)
                m_Crest = Primitive(PrimitiveType.Sphere, "Vague de fonte", m_MoltenMaterial).transform;
        }

        // La vague descend la voie vers la forêt : elle brûle et repousse ceux qu'elle touche ; la rivière s'allonge derrière elle.
        void UpdateFlow()
        {
            if (m_FlowAt < 0f || m_FlowLane == null)
                return;

            m_FlowAt += m_FlowSpeed * Time.deltaTime;
            var along = Mathf.Clamp(m_FlowStart - m_FlowAt, 0f, m_FlowLane.Length);
            var front = m_FlowLane.PointAt(along, out var direction);
            front.y = GroundY(front, 1.5f);
            m_FlowSoundAt = front;
            var forest = -direction;
            forest.y = 0f;
            forest = forest.sqrMagnitude > 1e-4f ? forest.normalized : ForestwardAt(front);
            var side = Vector3.Cross(Vector3.up, forest);

            while (m_River != null && m_NextSection <= m_FlowAt)
            {
                AddSection(m_River, m_NextSection);
                m_NextSection += 1f;
            }

            // La vague : un dos de fonte qui roule en tête, plus haut au milieu.
            if (m_Crest != null)
            {
                var swell = 1f + 0.15f * Mathf.Sin(Time.time * 9f);
                m_Crest.position = front + Vector3.up * 0.5f;
                m_Crest.rotation = Quaternion.LookRotation(forest);
                m_Crest.localScale = new Vector3(m_FlowWidth * 0.9f, 1.8f * swell, 3f);
                SetMolten(m_Crest.GetComponent<Renderer>(), 0f, 1.8f, 0f);
            }

            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(front, m_FlowWidth * 0.5f + 0.5f));
            foreach (var enemy in m_Scratch)
            {
                if (!m_FlowHit.Add(enemy))
                    continue;
                if (!Hurt(enemy, m_FlowDamage, front, forest))
                    continue;

                Burning.Apply(enemy, m_FlowBurn * Strength, 3f, ShotGrade.None, 0f, BurnFlames.Enemy);
                FloatingText.Spawn(enemy.Center + Vector3.up, "Fondu !", k_Molten, 1.1f, 1f);
                if (!enemy.IsBoss && enemy.CanBeLaunched && enemy.Launch(forest * m_FlowPush + Vector3.up * 6f, this))
                    enemy.Tumble(new Vector3(Random.Range(-360f, 360f), Random.Range(-180f, 180f), Random.Range(-360f, 360f)));
            }

            if (Time.time >= m_NextFlowFx)
            {
                m_NextFlowFx = Time.time + 0.06f;
                var edge = front + side * Random.Range(-0.5f, 0.5f) * m_FlowWidth;
                Burst(edge + Vector3.up * 0.4f, k_Molten, 20, 7f, 0.5f, 0.8f, 1f, false);
                Burst(front + Vector3.up * 1.2f, k_Smoke, 5, 2.5f, 1.4f, 1.8f, -0.12f);
            }

            if (Time.time >= m_NextFrontFire)
            {
                m_NextFrontFire = Time.time + 0.18f;
                Explosion(front + side * Random.Range(-0.4f, 0.4f) * m_FlowWidth + Vector3.up * 0.5f, 2f);
            }

            FlowLight(front + Vector3.up * 2f, 7f);
            if (m_FlowAt < m_FlowLength && along > 0f)
                return;

            // Au bout : la vague retombe, la rivière refroidit.
            Burst(front + Vector3.up * 0.5f, k_Molten, 60, 9f, 0.6f, 1f, 1f, false);
            if (m_Crest != null)
                Destroy(m_Crest.gameObject);
            m_Crest = null;
            if (m_River != null)
                m_River.CoolStart = Time.time;
            m_River = null;
            m_FlowAt = -1f;
            m_FlowLane = null;
        }

        River NewRiver(MapScan.Lane lane, float start)
        {
            var river = new River { Lane = lane, Start = start };
            river.Go = new GameObject("Rivière de fonte");
            river.Mesh = new Mesh { name = "Rivière de fonte" };
            river.Mesh.MarkDynamic();
            river.Go.AddComponent<MeshFilter>().sharedMesh = river.Mesh;
            var renderer = river.Go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_MoltenMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            river.Renderer = renderer;
            m_Rivers.Add(river);
            return river;
        }

        // Une section de plus, à ce point de la voie : quatre points en travers, posés sur le sol, plus hauts au milieu.
        void AddSection(River river, float traveled)
        {
            if (river.Mesh == null || m_MoltenMaterial == null)
                return;

            var along = Mathf.Clamp(river.Start - traveled, 0f, river.Lane.Length);
            var center = river.Lane.PointAt(along, out var direction);
            direction.y = 0f;
            var side = direction.sqrMagnitude > 1e-4f ? Vector3.Cross(Vector3.up, direction.normalized) : Vector3.right;
            var first = river.Vertices.Count;
            for (var k = 0; k < k_Across.Length; k++)
            {
                var point = center + side * (k_Across[k] * m_FlowWidth * 0.5f);
                point.y = GroundY(point, 1.5f) + (k == 0 || k == k_Across.Length - 1 ? 0.05f : 0.18f);
                river.Vertices.Add(point);
                river.Uvs.Add(new Vector2(k / (float)(k_Across.Length - 1), traveled / 4f));
            }

            if (first > 0)
            {
                for (var k = 0; k < k_Across.Length - 1; k++)
                {
                    var a = first - k_Across.Length + k;
                    var b = first + k;
                    river.Triangles.Add(a);
                    river.Triangles.Add(b);
                    river.Triangles.Add(a + 1);
                    river.Triangles.Add(a + 1);
                    river.Triangles.Add(b);
                    river.Triangles.Add(b + 1);
                }
            }

            river.Length = traveled;
            river.Mesh.SetVertices(river.Vertices);
            river.Mesh.SetUVs(0, river.Uvs);
            river.Mesh.SetTriangles(river.Triangles, 0);
            river.Mesh.RecalculateBounds();
            river.Mesh.RecalculateNormals();
        }

        // Les rivières : chaudes tant que la vague avance, puis elles refroidissent (la croûte gagne), s'enfoncent et disparaissent ;
        // ceux qui y marchent encore chaudes brûlent.
        void UpdateRivers()
        {
            m_Burning.Clear();
            for (var i = m_Rivers.Count - 1; i >= 0; i--)
            {
                var river = m_Rivers[i];
                if (river.Go == null)
                {
                    m_Rivers.RemoveAt(i);
                    continue;
                }

                var cool = river.CoolStart < 0f ? 0f : (Time.time - river.CoolStart) / Mathf.Max(0.5f, m_FlowStay);
                SetMolten(river.Renderer, Mathf.Clamp01(cool), Mathf.Lerp(1.5f, 0.5f, Mathf.Clamp01(cool)), 1f);
                if (cool > 1f)
                    river.Go.transform.position += Vector3.down * (0.4f * Time.deltaTime);
                if (cool >= 1.6f)
                {
                    Destroy(river.Mesh);
                    Destroy(river.Go);
                    m_Rivers.RemoveAt(i);
                    continue;
                }

                if (cool < 0.6f && Time.time >= river.NextBurn && river.Lane != null)
                {
                    river.NextBurn = Time.time + 0.25f;
                    foreach (var enemy in GroundEnemies())
                    {
                        var gap = river.Lane.DistanceTo(enemy.transform.position, out var at);
                        if (gap <= m_FlowWidth * 0.5f && at <= river.Start + 0.5f && at >= river.Start - river.Length - 0.5f &&
                            !m_Burning.Contains(enemy))
                            m_Burning.Add(enemy);
                    }
                }

                // Des braises montent de la rivière encore chaude.
                if (cool < 0.8f && river.Vertices.Count >= 8 && Random.value < Time.deltaTime * 12f)
                {
                    var index = Random.Range(0, river.Vertices.Count);
                    Burst(river.Vertices[index] + Vector3.up * 0.3f, k_Sparks, 5, 2f, 0.12f, 1.2f, -0.3f);
                }
            }
        }

        // Le son de la coulée, en boucle : il suit le jet puis la vague, monte vite, et s'éteint doucement quand elle finit.
        void UpdateFlowSound(bool on)
        {
            if (m_FlowClip == null)
                return;

            if (m_FlowAudio == null)
            {
                if (!on)
                    return;
                var go = new GameObject("Son de la coulée");
                m_FlowAudio = go.AddComponent<AudioSource>();
                m_FlowAudio.clip = m_FlowClip;
                m_FlowAudio.loop = true;
                m_FlowAudio.playOnAwake = false;
                m_FlowAudio.spatialBlend = 1f;
                m_FlowAudio.rolloffMode = AudioRolloffMode.Logarithmic;
                m_FlowAudio.minDistance = 4f;
                m_FlowAudio.maxDistance = 60f;
                m_FlowAudio.dopplerLevel = 0f;
                m_FlowAudio.outputAudioMixerGroup = Sfx.Output;
                m_FlowAudio.volume = 0f;
                m_FlowAudio.time = Random.Range(0f, m_FlowClip.length * 0.9f);
                m_FlowAudio.Play();
            }

            m_FlowAudio.transform.position = m_FlowSoundAt;
            m_FlowAudio.volume = Mathf.MoveTowards(m_FlowAudio.volume, on ? 1f : 0f, Time.deltaTime * (on ? 3f : 1.2f));
            if (on || m_FlowAudio.volume > 0f)
                return;
            Destroy(m_FlowAudio.gameObject);
            m_FlowAudio = null;
        }

        // La lumière orange de la coulée (la fournaise, le torrent, puis la vague), qui s'éteint seule.
        void FlowLight(Vector3 point, float intensity)
        {
            if (m_FlowLight == null)
            {
                var go = new GameObject("Lueur de la fonte");
                m_FlowLight = go.AddComponent<Light>();
                m_FlowLight.type = LightType.Point;
                m_FlowLight.color = new Color(1f, 0.45f, 0.15f);
                m_FlowLight.range = 26f;
                m_FlowLight.shadows = LightShadows.None;
                m_FlowLight.intensity = 0f;
            }

            m_FlowLight.transform.position = point;
            m_FlowLight.intensity = Mathf.Max(intensity, m_FlowLight.intensity);
        }

        // ---------------------------------------------------------------- les effets

        void UpdateEffects()
        {
            // Ceux que les rivières chaudes ont trouvés brûlent (un quart de seconde de brûlure).
            UpdateRivers();
            foreach (var enemy in m_Burning)
            {
                if (enemy != null && enemy.IsAlive)
                    Hurt(enemy, m_RiverBurn * 0.25f, enemy.transform.position, Vector3.up);
            }

            m_Burning.Clear();
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

            for (var i = m_Cracks.Count - 1; i >= 0; i--)
            {
                var crack = m_Cracks[i];
                var t = (Time.time - crack.Born) / crack.Life;
                if (crack.Renderer == null || t >= 1f)
                {
                    if (crack.Renderer != null)
                        Destroy(crack.Renderer.gameObject);
                    m_Cracks.RemoveAt(i);
                    continue;
                }

                SetMolten(crack.Renderer, Mathf.Clamp01((t - 0.2f) / 0.7f), Mathf.Lerp(2f, 0.6f, t), 0f);
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

            // La lueur de la coulée baisse dès qu'on ne la ravive plus, puis disparaît.
            if (m_FlowLight != null && m_PourUntil < 0f && m_FlowAt < 0f)
            {
                m_FlowLight.intensity -= dt * 6f;
                if (m_FlowLight.intensity <= 0f)
                {
                    Destroy(m_FlowLight.gameObject);
                    m_FlowLight = null;
                }
            }
        }

        // La foudre tombe du ciel sur ce point : l'éclair, ses branches, l'éclat blanc (sans lui, flash = false : une gerbe
        // d'étincelles, pour les marteaux qui tombent entre deux coups complets).
        void Lightning(Vector3 to, float width, int branches, bool flash = true)
        {
            var scale = Mathf.Max(0.3f, transform.lossyScale.y);
            var from = to + new Vector3(Random.Range(-8f, 8f), m_BoltHeight * scale, Random.Range(-8f, 8f));
            LightningBolt.Spawn(from, to, m_BoltColor, null, width * scale, 0.45f);
            LightningBolt.Spawn(from, to, Color.Lerp(m_BoltColor, Color.white, 0.6f), null, width * 0.45f * scale, 0.3f);
            for (var i = 0; i < branches; i++)
            {
                var start = Vector3.Lerp(from, to, Random.Range(0.25f, 0.8f));
                var end = start + new Vector3(Random.Range(-10f, 10f), -Random.Range(6f, 14f), Random.Range(-10f, 10f)) * scale;
                LightningBolt.Spawn(start, end, m_BoltColor, null, width * 0.35f * scale, 0.3f);
            }

            if (!flash)
            {
                Sparks(to + Vector3.up * 0.4f, 30);
                return;
            }

            Burst(to + Vector3.up * 0.4f, Color.Lerp(m_BoltColor, Color.white, 0.5f), 40, 10f, 0.15f, 0.5f, 0.4f);
            FlashAt(to + Vector3.up * 3f, m_BoltColor, 12f, 40f, 0.3f);
        }

        // L'onde de choc : un anneau de lumière qui s'élargit au ras du sol, et s'efface.
        void Shockwave(Vector3 point, float radius, float duration)
        {
            if (m_RingMaterial == null)
                return;

            var go = new GameObject("Onde de choc");
            go.transform.position = point + Vector3.up * 0.2f;
            go.AddComponent<MeshFilter>().sharedMesh = RingMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var material = new Material(m_RingMaterial) { name = "Onde de choc (en jeu)" };
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            go.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
            m_Rings.Add(new Ring { Body = go.transform, Material = material, Color = k_Shock, Born = Time.time, Duration = duration,
                                   Radius = radius });
        }

        // Le sol fendu de fonte : des fissures qui partent du point, brûlantes, qui refroidissent puis disparaissent.
        void Cracks(Vector3 point, int count, float minLength, float maxLength, float life)
        {
            if (m_MoltenMaterial == null)
                return;

            var turn = Random.Range(0f, 360f);
            for (var i = 0; i < count; i++)
            {
                var angle = turn + i * 360f / count + Random.Range(-12f, 12f);
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var length = Random.Range(minLength, maxLength) * Mathf.Max(0.3f, transform.lossyScale.y);
                var center = point + direction * (length * 0.5f + 0.6f);
                center.y = GroundY(center, 1.5f) + 0.03f;
                var go = Primitive(PrimitiveType.Cube, "Fissure de fonte", m_MoltenMaterial);
                go.transform.SetPositionAndRotation(center, Quaternion.LookRotation(direction));
                go.transform.localScale = new Vector3(Random.Range(0.22f, 0.45f), 0.06f, length);
                m_Cracks.Add(new Crack { Renderer = go.GetComponent<Renderer>(), Born = Time.time, Life = life * Random.Range(0.8f, 1.1f) });
            }
        }

        // La roche qui vole : des éclats lancés en l'air, en tournant ; quelques-uns de fonte.
        void ThrowDebris(Vector3 point, int count, float strength)
        {
            for (var i = 0; i < count; i++)
            {
                var molten = Random.value < 0.3f && m_MoltenMaterial != null;
                var go = Primitive(PrimitiveType.Cube, "Éclat", molten ? m_MoltenMaterial : m_DebrisMaterial);
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

        // Un éclat de lumière qui s'éteint vite.
        void FlashAt(Vector3 point, Color color, float intensity, float range, float duration)
        {
            var go = new GameObject("Éclat de lumière");
            go.transform.position = point;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range * Mathf.Max(0.3f, transform.lossyScale.y);
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

        // La fonte d'un morceau : refroidie (0 à 1), son éclat, ses bords plus froids.
        void SetMolten(Renderer renderer, float cool, float glow, float edges)
        {
            if (renderer == null)
                return;
            renderer.GetPropertyBlock(m_Block);
            m_Block.SetFloat(k_CoolId, cool);
            m_Block.SetFloat(k_GlowId, glow);
            m_Block.SetFloat(k_EdgesId, edges);
            renderer.SetPropertyBlock(m_Block);
        }

        // Tout s'arrête : ce qui vole disparaît, les flèches en attente sont oubliées, les bras au repos, un lingot sur l'enclume ;
        // la fonte et sa lueur disparaissent.
        void StopAll()
        {
            foreach (var shot in m_Shots)
            {
                shot.Flying = false;
                shot.LandedAt = -1f;
                if (shot.Go != null)
                    shot.Go.SetActive(false);
            }

            m_Orders.Clear();
            m_SnapAt = -1f;
            m_Raise = 0f;
            m_ArmAngle = 0f;
            m_ArmYaw = 0f;
            m_ArmYawGoal = 0f;
            m_ForgeAt = -1f;
            m_ForgeAngle = 0f;
            m_Lean = 0f;
            m_IngotBack = -1f;
            if (m_Ingot != null)
                m_Ingot.gameObject.SetActive(true);
            m_PourUntil = -1f;
            m_FlowAt = -1f;
            if (m_River != null)
                m_River.CoolStart = Time.time;
            m_River = null;
            if (m_Jet != null)
                Destroy(m_Jet.gameObject);
            m_Jet = null;
            if (m_Crest != null)
                Destroy(m_Crest.gameObject);
            m_Crest = null;
            if (m_FlowLight != null)
                Destroy(m_FlowLight.gameObject);
            m_FlowLight = null;
            if (m_FlowAudio != null)
                Destroy(m_FlowAudio.gameObject);
            m_FlowAudio = null;
        }

        // Les effets laissés dans le monde (ce qu'il lance, éclats, ondes, fissures, lumières, rivières), quand le Colosse disparaît.
        void ClearEffects()
        {
            DestroyShots();

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

            foreach (var crack in m_Cracks)
            {
                if (crack.Renderer != null)
                    Destroy(crack.Renderer.gameObject);
            }

            foreach (var flash in m_Flashes)
            {
                if (flash.Light != null)
                    Destroy(flash.Light.gameObject);
            }

            foreach (var river in m_Rivers)
            {
                if (river.Mesh != null)
                    Destroy(river.Mesh);
                if (river.Go != null)
                    Destroy(river.Go);
            }

            m_Debris.Clear();
            m_Rings.Clear();
            m_Cracks.Clear();
            m_Flashes.Clear();
            m_Rivers.Clear();
        }

        static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var deeper = FindChild(child, name);
                if (deeper != null)
                    return deeper;
            }

            return null;
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

                s_RingMesh = new Mesh { name = "Onde de choc" };
                s_RingMesh.SetVertices(vertices);
                s_RingMesh.SetTriangles(triangles, 0);
                s_RingMesh.RecalculateNormals();
                s_RingMesh.RecalculateBounds();
                return s_RingMesh;
            }
        }

        // Le matériau des traînées : couleurs par sommet, sans lumière (celui des éclairs).
        static Material TrailMaterial
        {
            get
            {
                if (s_TrailMaterial != null)
                    return s_TrailMaterial;
                var shader = Shader.Find("Archery/UnlitVertexColor");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");
                s_TrailMaterial = new Material(shader) { name = "Traînée du marteau (en jeu)" };
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

        static float Sq(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t;
        }

        /// <summary>La portée dessinée pendant la pose (3 novembre, le placement lisible).</summary>
        public override float ReachAt(int level) => m_Range;
    }
}
