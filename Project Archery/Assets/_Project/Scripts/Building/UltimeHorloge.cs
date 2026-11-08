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
using UnityEngine.Rendering.Universal;

namespace Archery.Building
{
    /// <summary>
    /// L'ultime <b>Grande Horloge</b> (2 novembre, tard ; <c>Docs/Refonte_TowerDefense.md</c>, 12.44 et 12.52) : le niveau 4 du Bowling ou
    /// du Beffroi, la famille de la glace et du temps. Une horloge astronomique de glace et d'or qui flotte au-dessus de son socle : son
    /// grand cadran céleste et ses douze chiffres romains de glace, ses aiguilles, ses engrenages, trois grands anneaux qui tournent
    /// autour d'elle, sa cloche.
    /// <list type="bullet">
    /// <item><b>L'éveil</b> : bâtie, elle sort de terre dans le givre, ses anneaux tournent à toute vitesse ; ses douze chiffres
    /// s'allument un à un, ses aiguilles reviennent à midi, la cloche sonne : l'onde de givre fige tout autour.</item>
    /// <item><b>Les chiffres</b> (seule, sans arrêt) : à chaque tic (1,2 s), un chiffre de glace (I, II, III…) se détache du cadran et
    /// part en cloche sur un ennemi : il le blesse et givre ses voisins, sous une aurore. L'aiguille des heures avance avec lui.</item>
    /// <item><b>Minuit</b>, après le XII : les aiguilles se rejoignent, « Za Warudo ! Temps, arrête-toi ! » : la bulle part de
    /// l'horloge et couvre toute la carte, le monde en noir et blanc, le temps arrêté 9 s sauf pour toi (tes flèches attendent en
    /// l'air) ; douze coups, un chiffre repousse à chacun. Au douzième, <b>le dégel qui brise</b> : tes flèches repartent toutes
    /// ensemble, et le givre de tous les ennemis figés éclate d'un coup.</item>
    /// <item><b>L'écho du temps</b> (tes flèches) : chaque flèche tirée qui touche un ennemi à sa portée est rejouée 1 s plus tard par
    /// une flèche de glace géante qui part du cadran et frappe au même endroit, sous une aurore. 5 par seconde au plus.</item>
    /// <item><b>Dans l'arène de MK</b> : ses chiffres et ses échos frappent MK ; après le XII, Minuit arrête toute l'arène 6 s, MK
    /// compris, et tes flèches le frappent toutes ensemble à la fin.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Le joueur, le 2 novembre, tard, parmi les propositions de Claude : les chiffres pour l'action seule, Minuit après les douze
    /// chiffres pour le grand coup (« on reprend l'ancien Beffroi avec son Za Warudo qui dure 9 secondes, une fois que les 12 sont
    /// passés »), l'écho du temps pour sa flèche, l'aurore pour le signe de chaque coup, l'horloge astronomique pour le modèle, le dégel
    /// qui brise pour la fin de Minuit, Minuit dans l'arène pour MK. Fait d'emblée avec ce que le Colosse et le Manège ont appris
    /// (<c>Docs/Ultimes_Cahier.md</c>). Minuit vient du Beffroi (<see cref="TrioBeffroi"/>, où il dort au niveau 4), avec les réglages
    /// que le joueur y avait mis : la bulle sur toute la carte (300 m), le noir et blanc, 9 s ; les boss des vagues figés toute la
    /// durée eux aussi (le joueur, 3 novembre ; 2 s sur le Beffroi).
    /// <para>Pour le casque : le modèle est fondu au lancement, une pièce par partie qui bouge (le cadran, chaque aiguille, chaque
    /// engrenage, chaque anneau, chaque chiffre) ; ce qu'il lance (les chiffres, les flèches de glace) est une copie fondue,
    /// réutilisée ; les étincelles de givre, la poussière de glace et les flocons viennent de systèmes de particules à lui, déplacés là
    /// où ça frappe ; les aurores sont des rubans réutilisés (dix au plus) ; le coup complet au plus toutes les 0,5 s.</para>
    /// <para>Le cadran (m_Face) se tourne vers le joueur : on lit toujours ses chiffres.</para>
    /// </remarks>
    public class UltimeHorloge : TowerUltimate
    {
        // Le cadran : son rayon jusqu'aux chiffres (repère du cadran, avant sa taille) ; les aiguilles tournent dans le sens des
        // aiguilles d'une montre vues de face (autour de +z du cadran).
        const int k_Hours = 12;

        // Une flèche attend son écho au plus … s après l'heure de son écho (sa cible est loin, ou morte).
        const float k_OrderLife = 2f;

        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        static readonly string[] k_Roman = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };

        [Header("Le modèle")]
        [Tooltip("L'horloge qui flotte (repère au centre du cadran) : elle monte et descend doucement.")]
        [SerializeField]
        Transform m_Float;

        [Tooltip("Le cadran, ses chiffres, ses aiguilles, ses engrenages, sa cloche (repère au centre, +z vers l'avant) : il se tourne " +
                 "vers le joueur.")]
        [SerializeField]
        Transform m_Face;

        [Tooltip("Le centre du cadran, un peu devant : les flèches de glace en partent.")]
        [SerializeField]
        Transform m_Center;

        [Tooltip("Les douze chiffres de glace, de I à XII : ils partent un à un, et repoussent pendant Minuit.")]
        [SerializeField]
        Transform[] m_Numerals = System.Array.Empty<Transform>();

        [Tooltip("Les aiguilles (repère au centre, l'aiguille vers le haut) : les heures, les minutes, la trotteuse.")]
        [SerializeField]
        Transform m_HourHand;

        [SerializeField]
        Transform m_MinuteHand;

        [SerializeField]
        Transform m_SecondHand;

        [Tooltip("Les engrenages derrière le cadran (ils tournent autour de son axe).")]
        [SerializeField]
        Transform[] m_Gears = System.Array.Empty<Transform>();

        [Tooltip("Les trois anneaux qui tournent autour d'elle (chacun autour de son axe y, penché par son parent).")]
        [SerializeField]
        Transform[] m_Rings = System.Array.Empty<Transform>();

        [Tooltip("La cloche au sommet du cadran (repère à l'attache) : elle se balance quand elle sonne.")]
        [SerializeField]
        Transform m_Bell;

        [Tooltip("La flèche de glace du modèle (cachée en jeu) : les échos en sont des copies.")]
        [SerializeField]
        Transform m_ArrowTemplate;

        [Tooltip("La glace qui luit (Givre_lumineux) : les chiffres, les étoiles du cadran, les gemmes des anneaux ; copiée en jeu et " +
                 "animée.")]
        [SerializeField]
        Material m_GlowMaterial;

        [Tooltip("L'onde des coups : une lumière transparente (Rayon_du_jugement), copiée et teinte en jeu.")]
        [SerializeField]
        Material m_RingMaterial;

        [Tooltip("Le matériau de la bulle de Minuit (Bulle). Vide : la bulle ne se voit pas, le noir et blanc reste.")]
        [SerializeField]
        Material m_BubbleMaterial;

        [Tooltip("Les éclats de glace qui volent.")]
        [SerializeField]
        Material m_DebrisMaterial;

        [Tooltip("Le givre au sol sous les grands coups (Givre).")]
        [SerializeField]
        Material m_FrostMaterial;

        [Header("L'éveil")]
        [Tooltip("Elle sort de terre : de cette profondeur (m), en … s.")]
        [SerializeField]
        float m_WakeDepth = 14f;

        [SerializeField]
        float m_RiseTime = 2.4f;

        [Tooltip("L'onde de givre à la fin de l'éveil : à … m, … de dégâts, figés … s.")]
        [SerializeField]
        float m_WakeRadius = 22f;

        [SerializeField]
        float m_WakeDamage = 100f;

        [SerializeField]
        float m_WakeFreeze = 2f;

        [Header("La portée")]
        [Tooltip("Portée (m, à plat, autour de l'horloge) : ses chiffres y cherchent leur cible ; ta flèche doit toucher plus près.")]
        [SerializeField]
        float m_Range = 30f;

        [Header("Les chiffres (seule)")]
        [Tooltip("Un tic toutes les … s : un chiffre part sur un ennemi au hasard à sa portée (les parchemins d'adresse et la surcharge " +
                 "l'accélèrent). Sans ennemi, l'horloge attend : le chiffre suivant ne part pas.")]
        [SerializeField]
        float m_TickInterval = 1.2f;

        [Tooltip("Un chiffre qui tombe : dégâts au centre (vague 1, en Normal ; moitié au bord), rayon (m) ; ceux qui survivent sont " +
                 "figés de givre … s. 220 dans 4,5 m (au tableau d'équilibrage, 12.52 : seule, l'horloge vaut un trio moyen).")]
        [SerializeField]
        float m_NumeralDamage = 220f;

        [SerializeField]
        float m_NumeralRadius = 4.5f;

        [SerializeField]
        float m_NumeralFreeze = 1f;

        [Tooltip("Ce qu'un boss perd en plus sous un chiffre (part de ses PV max).")]
        [SerializeField]
        float m_NumeralBossShare = 0.002f;

        [Header("Minuit (après le XII)")]
        [Tooltip("Le temps que les aiguilles se rejoignent sur midi avant Minuit (s).")]
        [SerializeField]
        float m_MidnightWindup = 1f;

        [Tooltip("Rayon (m) où le temps s'arrête (300 : toute la carte, le réglage du joueur sur le Beffroi), et durée (s) des douze " +
                 "coups (9 : le joueur).")]
        [SerializeField]
        float m_MidnightRadius = 300f;

        [SerializeField]
        float m_MidnightTime = 9f;

        [Tooltip("La bulle part de l'horloge et atteint le rayon de Minuit en tant de secondes ; le temps ne s'arrête qu'à son passage.")]
        [SerializeField]
        float m_BubbleGrowTime = 0.8f;

        [Tooltip("La saturation dans la bulle (−100 : noir et blanc).")]
        [Range(-100f, 0f)]
        [SerializeField]
        float m_BubbleSaturation = -100f;

        [Tooltip("Le dégel qui brise (le choix du joueur) : au douzième coup, chaque ennemi figé par Minuit prend ces dégâts d'un coup " +
                 "(vague 1, en Normal) ; un boss, plus cette part de ses PV max.")]
        [SerializeField]
        float m_ShatterDamage = 500f;

        [SerializeField]
        float m_ShatterBossShare = 0.02f;

        [Header("L'écho du temps (tes flèches)")]
        [Tooltip("Ta flèche est rejouée … s après avoir touché ; au plus un écho toutes les … s (0,2 : 5 par seconde, le plafond du joueur).")]
        [SerializeField]
        float m_EchoDelay = 1f;

        [SerializeField]
        float m_EchoInterval = 0.2f;

        [Tooltip("Les flèches qui attendent leur écho, au plus : les plus anciennes s'effacent.")]
        [SerializeField]
        int m_QueueSize = 8;

        [Tooltip("Une flèche dans le sol : l'écho va sur l'ennemi au sol le plus proche, à … m au plus ; sinon, rien.")]
        [SerializeField]
        float m_SeekRadius = 5f;

        [Tooltip("L'écho frappe : dégâts au centre (vague 1, en Normal ; moitié au bord), rayon (m), ceux qui survivent projetés (loin, " +
                 "haut, m/s) et figés de givre … s. 160 dans 6 m : comme les marteaux du Colosse et les chevaux du Manège.")]
        [SerializeField]
        float m_EchoDamage = 160f;

        [SerializeField]
        float m_EchoRadius = 6f;

        [SerializeField]
        float m_EchoPush = 6f;

        [SerializeField]
        float m_EchoLift = 8f;

        [SerializeField]
        float m_EchoFreeze = 1f;

        [Tooltip("La taille d'une flèche de glace (fois celle du modèle : 1).")]
        [SerializeField]
        float m_EchoScale = 1f;

        [Tooltip("Ce qu'un boss perd en plus sous un écho (part de ses PV max). MK, dans l'arène : cette part seulement.")]
        [SerializeField]
        float m_EchoBossShare = 0.002f;

        [Tooltip("Le coup complet (la lumière, le grand impact, les éclats de glace, les manettes) au plus toutes les … s ; l'éclair dans " +
                 "le ciel au plus toutes les … s.")]
        [SerializeField]
        float m_HeavyInterval = 0.5f;

        [SerializeField]
        float m_SkyFlashInterval = 1.2f;

        [Header("L'aurore, le signe de chaque coup")]
        [Tooltip("Sa hauteur (m, à 1 : la taille du modèle), sa largeur, combien de temps elle reste (s).")]
        [SerializeField]
        float m_AuroraHeight = 26f;

        [SerializeField]
        float m_AuroraWidth = 2.6f;

        [SerializeField]
        float m_AuroraLife = 1.1f;

        [Tooltip("Ses couleurs, du bas vers le haut.")]
        [SerializeField]
        Color[] m_AuroraColors = { new Color(0.3f, 1f, 0.55f, 1f), new Color(0.2f, 0.85f, 1f, 1f), new Color(0.6f, 0.4f, 1f, 1f) };

        [Header("Le ciel de Minuit")]
        [Tooltip("Sa force (0 à 1) pendant Minuit (l'éveil : 1) ; le reste du temps, le ciel est celui de la partie.")]
        [SerializeField]
        float m_SkyMidnight = 0.85f;

        [Header("Dans l'arène de MK")]
        [Tooltip("Sa taille dans l'arène (fois la sienne) : la voûte de la grotte est basse près des parois.")]
        [SerializeField]
        float m_ArenaScale = 0.65f;

        [Tooltip("Minuit dans l'arène (le choix du joueur) : tout s'arrête … s, MK compris ; au dégel, MK perd cette part de ses PV max.")]
        [SerializeField]
        float m_ArenaMidnightTime = 6f;

        [SerializeField]
        float m_ArenaShatterShare = 0.03f;

        [Header("Sons")]
        [Tooltip("Le tic de l'horloge, à chaque tic.")]
        [SerializeField]
        AudioClip m_TickClip;

        [Tooltip("Un coup de la cloche : les douze coups de Minuit, la fin de l'éveil.")]
        [SerializeField]
        AudioClip m_BellClip;

        [Tooltip("Le temps qui s'arrête (il part quand les aiguilles se rejoignent : son sommet tombe au début de Minuit) ; et qui " +
                 "reprend (le dégel).")]
        [SerializeField]
        AudioClip m_StopClip;

        [SerializeField]
        AudioClip m_ResumeClip;

        [Tooltip("Le bruit de l'arrêt du temps, au début de Minuit (3 novembre ; le modèle du joueur : le « Za Warudo » de JoJo) : " +
                 "saturé, à plat (on l'entend partout), il se coupe net quand le temps s'est arrêté ; le premier coup de cloche tombe " +
                 "sur la coupure (Time Stop Cut, en s).")]
        [SerializeField]
        AudioClip m_TimeStopClip;

        [SerializeField]
        float m_TimeStopCut = 1.4f;

        [Tooltip("Le dégel qui brise : la glace qui éclate, en série.")]
        [SerializeField]
        AudioClip m_ShatterClip;

        [Tooltip("Un chiffre ou un écho qui frappe, un figé qui éclate : un craquement de glace.")]
        [SerializeField]
        AudioClip m_ImpactClip;

        [Tooltip("Un écho qui part, une aurore qui tombe : le carillon.")]
        [SerializeField]
        AudioClip m_ChimeClip;

        [Tooltip("Le souffle : un chiffre qui part.")]
        [SerializeField]
        AudioClip m_WhooshClip;

        [Tooltip("Le grand impact : la fin de l'éveil.")]
        [SerializeField]
        AudioClip m_BoomClip;

        // Ce qu'elle lance (un chiffre, une flèche de glace) : réutilisé d'un lancer à l'autre. Une flèche reste plantée un instant.
        class Shot
        {
            public GameObject Go;
            public TrailRenderer Trail;
            public Transform Source;
            public Vector3 Scale;
            public bool Echo;
            public int Numeral = -1;
            public float Power = 1f;
            public bool Flying;
            public float LandedAt = -1f;
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
        }

        struct Order
        {
            public Enemy Enemy;
            public DragonBoss Dragon;
            public Vector3 Point;
            public float At;
            public float Power;
        }

        class Stopped
        {
            public Arrow Arrow;
            public Vector3 Velocity;
        }

        // Une aurore : trois rubans de lumière qui tombent du ciel sur un coup, ondulent et s'effacent.
        class Curtain
        {
            public readonly LineRenderer[] Lines = new LineRenderer[3];
            public Vector3 Point;
            public float Born = -100f;
            public float Size;
            public float Phase;
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

        static readonly Color k_Ice = new Color(0.65f, 0.9f, 1f, 1f);
        static readonly Color k_Frost = new Color(0.85f, 0.95f, 1f, 1f);
        static readonly Color k_Gold = new Color(1f, 0.85f, 0.45f, 1f);
        static readonly Color k_Title = new Color(0.55f, 0.85f, 1f);
        static readonly Color k_Night = new Color(0.55f, 0.65f, 1f, 1f);
        static readonly Color k_Shock = new Color(0.6f, 0.9f, 1f, 0.85f);

        static Mesh s_RingMesh;
        static Material s_TrailMaterial;
        static Mesh s_TwoSidedSphere;

        readonly List<Enemy> m_Scratch = new List<Enemy>();
        readonly List<Shot> m_Shots = new List<Shot>();
        readonly List<Order> m_Orders = new List<Order>();
        readonly List<Stopped> m_Stopped = new List<Stopped>();
        readonly HashSet<Enemy> m_Frozen = new HashSet<Enemy>();
        readonly List<Curtain> m_Curtains = new List<Curtain>();
        readonly List<Debris> m_Debris = new List<Debris>();
        readonly List<Ring> m_Rings2 = new List<Ring>();
        readonly List<FlashLight> m_Flashes = new List<FlashLight>();
        readonly List<ParticleSystem> m_Loops = new List<ParticleSystem>();
        readonly Dictionary<Transform, (Mesh Mesh, Material[] Materials)> m_Copies = new Dictionary<Transform, (Mesh, Material[])>();
        readonly List<Mesh> m_Fused = new List<Mesh>();
        readonly List<Renderer> m_GlowRenderers = new List<Renderer>();
        readonly float[] m_RingSpeeds = { 12f, -18f, 7f };
        readonly float[] m_GearSpeeds = { 40f, -60f, 25f };

        SkySettings m_Sky;
        float m_SkyWeight;
        Material m_Glow;
        ParticleSystem m_FrostFx;
        ParticleSystem m_MistFx;
        Light m_CoreLight;
        Vector3 m_FloatRest;

        // L'éveil (−1 : fini, ou pas commencé) ; l'éclat de la glace (0 : éteinte), ses sursauts.
        float m_WakeAt = -1f;
        bool m_Awake;
        int m_WakeStep;
        float m_NextWakeFx;
        Vector3 m_Ground;
        float m_GlowLevel;
        float m_GlowFlare;

        // Les chiffres : le prochain tic, combien sont partis, les aiguilles (degrés, sens des aiguilles d'une montre), la vitesse des
        // anneaux (fois la leur).
        float m_NextTick;
        int m_Thrown;
        float m_HourAngle;
        float m_MinuteAngle;
        float m_MinuteSpin;
        float m_SecondAngle;
        float m_Spin = 1f;
        float m_Swing;

        // Minuit : il vient (les aiguilles se rejoignent), les coups qui restent, le prochain coup, sa fin.
        float m_MidnightAt = -1f;
        int m_Strokes;
        float m_NextStroke;
        float m_MidnightStart = -1f;
        float m_MidnightEnd;
        float m_NextArrowScan;
        float m_StrokeTime;
        Transform m_Bubble;
        Volume m_Grayscale;
        VolumeProfile m_GrayProfile;
        DragonBoss m_StoppedDragon;

        float m_LastEcho = -100f;
        float m_NextHeavy;
        float m_NextSkyFlash;
        bool m_ArenaHint;

        protected override float ArenaScale => m_ArenaScale;

        float Size => Mathf.Max(0.3f, transform.lossyScale.y);

        Vector3 CenterPoint => m_Center != null ? m_Center.position : transform.position + Vector3.up * 11f * Size;

        Vector3 BellPoint => m_Bell != null ? m_Bell.position : CenterPoint + Vector3.up * 5f * Size;

        Vector3 Hub => transform.position;

        bool InMidnight => m_Strokes > 0;

        protected override void Awake()
        {
            base.Awake();
            if (m_Float != null)
                m_FloatRest = m_Float.localPosition;
            m_Sky = SkySettings.Minuit();
            if (m_ArrowTemplate != null)
                m_ArrowTemplate.gameObject.SetActive(false);
            FuseModel();
        }

        // Pour le casque : ses centaines de pièces fondues au lancement, avant le plan bleu : une pièce fondue par partie qui bouge
        // (le cadran, chaque aiguille, chaque engrenage, chaque anneau, chaque chiffre, la cloche, l'horloge qui flotte, le socle), un
        // sous-maillage par matériau. La flèche de glace du modèle reste à part (cachée : on en fait des copies).
        void FuseModel()
        {
            var moving = new List<Transform> { transform };
            void Add(Transform t)
            {
                if (t != null && !moving.Contains(t))
                    moving.Add(t);
            }

            Add(m_Float);
            Add(m_Face);
            Add(m_HourHand);
            Add(m_MinuteHand);
            Add(m_SecondHand);
            Add(m_Bell);
            foreach (var t in m_Gears)
                Add(t);
            foreach (var t in m_Rings)
                Add(t);
            foreach (var t in m_Numerals)
                Add(t);

            var groups = new Dictionary<Transform, List<MeshFilter>>();
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.GetComponent<MeshRenderer>() == null || (m_ArrowTemplate != null && filter.transform.IsChildOf(m_ArrowTemplate)))
                    continue;
                var owner = Owner(filter.transform, moving);
                if (!groups.TryGetValue(owner, out var list))
                    groups[owner] = list = new List<MeshFilter>();
                list.Add(filter);
            }

            foreach (var group in groups)
            {
                var go = new GameObject("Fondu");
                go.transform.SetParent(group.Key, false);
                FuseInto(go.transform, group.Value, group.Key.name);
            }
        }

        static Transform Owner(Transform t, List<Transform> moving)
        {
            for (var p = t; p != null; p = p.parent)
            {
                if (moving.Contains(p))
                    return p;
            }

            return moving[0];
        }

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

        void OnEnable()
        {
            Arrow.AnyHit += OnArrowHit;

            if (InArena && IsBuilt && !m_ArenaHint)
            {
                m_ArenaHint = true;
                HeadMessages.Show("La Grande Horloge est là !\n<size=65%>Touche MK : tes flèches sont rejouées ; après le XII, Minuit " +
                                  "arrête toute l'arène</size>", k_Title, 5f);
            }
        }

        void OnDisable()
        {
            Arrow.AnyHit -= OnArrowHit;
            EndMidnight(false);
            ReleaseSky();
        }

        protected override void OnDestroy()
        {
            EndMidnight(false);
            StopAll();
            ClearEffects();
            ReleaseSky();
            if (m_Glow != null)
                Destroy(m_Glow);
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
            if (m_Bubble != null)
                Destroy(m_Bubble.gameObject);
            if (m_Grayscale != null)
                Destroy(m_Grayscale.gameObject);
            if (m_GrayProfile != null)
                Destroy(m_GrayProfile);
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
            EndMidnight(false);
            StopAll();
            DestroyShots();
            FinishWake();
            base.EnterArena();
            m_ArenaHint = false;
        }

        public override void LeaveArena()
        {
            EndMidnight(false);
            StopAll();
            DestroyShots();
            base.LeaveArena();
        }

        protected override void Update()
        {
            base.Update();
            UpdateEffects();
            UpdateShots();
            UpdateCurtains();
            UpdateBubble();
            if (!IsBuilt || IsUnderConstruction)
                return;

            UpdateSky();
            FaceViewer();
            if (m_WakeAt >= 0f)
            {
                UpdateWake();
                Animate();
                UpdateGlow();
                return;
            }

            if (InMidnight)
                UpdateMidnight();
            else
                UpdateTicks();
            UpdateEchoes();
            Animate();
            UpdateGlow();
        }

        // ---------------------------------------------------------------- l'éveil

        // Bâtie (au marteau, ou par la vitrine) : elle s'enfonce, éteinte, ses chiffres cachés ; l'éveil commence.
        void BeginWake()
        {
            m_WakeAt = Time.time;
            m_WakeStep = 0;
            m_Awake = false;
            m_GlowLevel = 0f;
            m_Spin = 12f;
            m_Ground = transform.parent != null ? transform.parent.position : transform.position;
            var local = transform.localPosition;
            transform.localPosition = new Vector3(local.x, -m_WakeDepth, local.z);
            SetupGlow();
            foreach (var numeral in m_Numerals)
            {
                if (numeral != null)
                    numeral.gameObject.SetActive(false);
            }

            m_HourAngle = 0f;
            m_MinuteAngle = 0f;
            HeadMessages.Show("La Grande Horloge s'éveille !", k_Title, 4f);
            Sfx.PlayThroughHush(m_StopClip, m_Ground, 0.8f, 0.8f);
            Shockwave(m_Ground, 12f, 0.9f, k_Shock);
            Frost(m_Ground, 7f, m_RiseTime + 5f);
            Rumble(m_Ground, 0.5f);
        }

        // Elle sort de terre, ses anneaux à toute vitesse ; sa glace s'allume ; ses chiffres apparaissent un à un ; ses aiguilles
        // reviennent à midi ; la cloche, l'onde de givre.
        void UpdateWake()
        {
            var t = Time.time - m_WakeAt;
            var rise = Mathf.Clamp01(t / Mathf.Max(0.1f, m_RiseTime));
            var local = transform.localPosition;
            transform.localPosition = new Vector3(local.x, -m_WakeDepth * (1f - EaseOut(rise)), local.z);
            m_Spin = Mathf.Lerp(12f, 1f, Mathf.Clamp01(t / (m_RiseTime + 2.4f)));

            if (rise < 1f && Time.time >= m_NextWakeFx)
            {
                // La glace qui vole, la poussière de givre ; le sol tremble.
                m_NextWakeFx = Time.time + 0.14f;
                var around = m_Ground + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(2f, 6f);
                ThrowDebris(around, 3, 0.4f);
                Mist(around + Vector3.up * 0.3f, 6);
                FrostBurst(around + Vector3.up * 0.4f, 20);
                Rumble(m_Ground, 0.3f);
            }

            if (m_WakeStep == 0 && t >= 0.5f)
            {
                m_WakeStep = 1;
                Aurora(m_Ground, 1.6f);
                SkyFlash(0.5f, true);
            }

            if (m_WakeStep == 1 && t >= m_RiseTime)
            {
                // Debout : sa glace s'allume, ses flocons, sa brume et sa lumière partent.
                m_WakeStep = 2;
                transform.localPosition = new Vector3(local.x, 0f, local.z);
                Mist(m_Ground + Vector3.up * 0.5f, 40);
                FrostBurst(CenterPoint, 120);
                StartLoops();
                m_GlowFlare = 1.5f;
            }

            m_GlowLevel = t < m_RiseTime ? 0f : Mathf.Clamp01((t - m_RiseTime) / 0.6f);

            // Ses douze chiffres apparaissent un à un, chacun son tic, plus aigu à chaque fois.
            if (m_WakeStep >= 2 && m_WakeStep < 2 + k_Hours && t >= m_RiseTime + 0.3f + (m_WakeStep - 2) * 0.1f)
            {
                var k = m_WakeStep - 2;
                ShowNumeral(k);
                Sfx.PlayThroughHush(m_TickClip, CenterPoint, 0.8f, 0.9f + 0.05f * k);
                m_WakeStep++;
            }

            // Les aiguilles tournent à l'envers à toute vitesse, et s'arrêtent sur midi.
            if (t >= m_RiseTime + 1.5f && t < m_RiseTime + 2.5f)
            {
                var s = Smooth((t - m_RiseTime - 1.5f) / 1f);
                m_HourAngle = -720f * (1f - s);
                m_MinuteAngle = -2880f * (1f - s);
            }

            if (m_WakeStep == 2 + k_Hours && t >= m_RiseTime + 2.5f)
            {
                m_WakeStep++;
                m_HourAngle = 0f;
                m_MinuteAngle = 0f;
                WakeBlast();
            }

            if (t >= m_RiseTime + 3.1f)
                FinishWake();
        }

        // La cloche sonne midi : l'onde de givre fige tout à la ronde, sous un anneau d'aurores.
        void WakeBlast()
        {
            var point = m_Ground;
            Toll(1f);
            Shockwave(point, m_WakeRadius, 0.8f, k_Shock);
            FrostBurst(point + Vector3.up * 2f, 200);
            MistRing(point, 7f, 8);
            for (var i = 0; i < 6; i++)
            {
                var angle = i * Mathf.PI / 3f;
                Aurora(point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (12f * Size), 1.2f);
            }

            Frost(point, 9f, 5f);
            FlashAt(point + Vector3.up * 6f, k_Ice, 10f, 50f, 0.8f);
            SkyFlash(1f, true);
            Sfx.PlayThroughHush(m_BoomClip, point, 1f, 0.7f);
            Sfx.PlayThroughHush(m_ShatterClip, point, 1f, 0.8f);
            Rumble(point, 1f);
            HeadMessages.Show("LA GRANDE HORLOGE !", k_Title, 2.5f);

            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(point, m_WakeRadius, false));
            foreach (var enemy in m_Scratch)
            {
                if (enemy.IsBoss)
                {
                    HurtBoss(enemy, m_WakeDamage, m_NumeralBossShare, point);
                    continue;
                }

                if (Hurt(enemy, m_WakeDamage, point, enemy.Center - point))
                    enemy.Freeze(m_WakeFreeze);
            }
        }

        void FinishWake()
        {
            if (m_WakeAt < 0f && m_Awake)
                return;

            m_WakeAt = -1f;
            m_Awake = true;
            m_GlowLevel = 1f;
            m_Spin = 1f;
            var local = transform.localPosition;
            transform.localPosition = new Vector3(local.x, 0f, local.z);
            for (var k = 0; k < m_Numerals.Length; k++)
                ShowNumeral(k);
            m_Thrown = 0;
            m_HourAngle = 0f;
            m_MinuteAngle = 0f;
            m_NextTick = Time.time + 0.6f;
            SetupGlow();
            StartLoops();
        }

        // ---------------------------------------------------------------- les chiffres

        // Un tic après l'autre : la trotteuse saute ; s'il y a un ennemi à sa portée, le chiffre suivant part sur lui (l'aiguille des
        // heures avance avec lui). Après le XII, Minuit vient.
        void UpdateTicks()
        {
            if (m_MidnightAt >= 0f)
            {
                // Les aiguilles se rejoignent sur midi : on le voit venir.
                var s = Mathf.Clamp01(1f - (m_MidnightAt - Time.time) / Mathf.Max(0.1f, m_MidnightWindup));
                m_MinuteAngle = Mathf.Lerp(m_MinuteAngle, 360f * Mathf.Ceil(m_MinuteAngle / 360f), s);
                m_Spin = Mathf.Lerp(1f, 4f, s);
                m_GlowFlare = Mathf.Max(m_GlowFlare, s);
                if (Time.time >= m_MidnightAt)
                {
                    m_MidnightAt = -1f;
                    BeginMidnight();
                }

                return;
            }

            if (Time.time < m_NextTick)
                return;

            m_NextTick = Time.time + m_TickInterval / Mathf.Max(0.1f, OverchargeRate);
            m_SecondAngle += 30f;
            Sfx.PlayThroughHush(m_TickClip, CenterPoint, 0.7f, Random.Range(0.97f, 1.03f));
            if (m_Thrown >= k_Hours)
                return;

            // Dans l'arène (8 novembre au soir), les chiffres ne partent plus sur MK : ils s'éteignent un à un, l'horloge compte les
            // heures, et Minuit arrive après le XII (environ 15 s), son seul effet sur MK.
            if (InArena)
            {
                if (m_Thrown < m_Numerals.Length && m_Numerals[m_Thrown] != null)
                {
                    FrostBurst(m_Numerals[m_Thrown].position, 14);
                    m_Numerals[m_Thrown].gameObject.SetActive(false);
                }
            }
            else
            {
                var target = NumeralTarget(out var dragon);
                if (target == null && dragon == null)
                    return;

                ThrowNumeral(m_Thrown, target, dragon);
            }

            m_Thrown++;
            m_MinuteSpin = 360f;
            if (m_Thrown < k_Hours)
                return;
            m_MidnightAt = Time.time + m_MidnightWindup;
            Sfx.PlayThroughHush(m_StopClip, CenterPoint, 1f, 1f);
        }

        // Sa cible : un ennemi au hasard à sa portée (au sol ou en vol) ; dans l'arène, MK.
        Enemy NumeralTarget(out DragonBoss dragon)
        {
            dragon = null;
            if (InArena)
            {
                dragon = ArenaDragon();
                return null;
            }

            var near = EnemiesNear(Hub, m_Range * RangeScale, false);
            m_Scratch.Clear();
            foreach (var enemy in near)
            {
                if (!enemy.IsTrainingDummy && !enemy.IsAlly)
                    m_Scratch.Add(enemy);
            }

            return m_Scratch.Count > 0 ? m_Scratch[Random.Range(0, m_Scratch.Count)] : null;
        }

        void ThrowNumeral(int k, Enemy enemy, DragonBoss dragon)
        {
            if (k < 0 || k >= m_Numerals.Length || m_Numerals[k] == null)
                return;

            var source = m_Numerals[k];
            var air = dragon != null || (enemy != null && enemy.IsFlyer);
            var point = dragon != null ? dragon.BodyPoint : air ? enemy.Center : enemy.transform.position;
            var shot = Throw(source, source.position, enemy, dragon, point, air, false);
            if (shot != null)
                shot.Numeral = k;
            source.gameObject.SetActive(false);
            FrostBurst(source.position, 20);
            Sfx.PlayThroughHush(m_WhooshClip, source.position, 0.6f, Random.Range(1.2f, 1.4f));
        }

        void ShowNumeral(int k)
        {
            if (k < 0 || k >= m_Numerals.Length || m_Numerals[k] == null)
                return;
            m_Numerals[k].gameObject.SetActive(true);
            FrostBurst(m_Numerals[k].position, 14);
        }

        // ---------------------------------------------------------------- Minuit (repris du Beffroi : la version du joueur)

        // Après le XII : les douze coups commencent, le temps s'arrête dans la bulle qui part de l'horloge.
        void BeginMidnight()
        {
            var time = InArena ? m_ArenaMidnightTime : m_MidnightTime;
            // Le bruit de l'arrêt du temps (3 novembre) : le premier coup tombe sur sa coupure, les onze autres se serrent derrière ;
            // le douzième tombe toujours au même moment (avant, le premier partait tout de suite, puis un tous les douzièmes).
            var cut = m_TimeStopClip != null ? Mathf.Clamp(m_TimeStopCut, 0f, time * 0.4f) : 0f;
            m_Strokes = k_Hours;
            m_StrokeTime = Mathf.Max(0.3f, (time * (k_Hours - 1) / k_Hours - cut) / (k_Hours - 1));
            m_NextStroke = Time.time + cut;
            m_MidnightStart = Time.time;
            m_MidnightEnd = Time.time + time + 0.2f;
            m_Spin = 0f;
            m_GlowFlare = 2f;
            Sfx.Hush();
            // Moins fort (le joueur, 3 novembre, après l'avoir entendu) : 0,6 ; il enfle jusqu'à sa coupure.
            Sfx.PlayThroughHush(m_TimeStopClip, CenterPoint, 0.6f, 1f, 0f);
            // Les mots du joueur (2 novembre).
            HeadMessages.Show("Za Warudo ! Temps, arrête-toi !", k_Night, 3f);
            Shockwave(Hub, 40f * Size, 0.8f, k_Shock);
            SkyFlash(0.6f, true);
            if (InArena)
            {
                var dragon = ArenaDragon();
                if (dragon != null && dragon.StopTime(time))
                    m_StoppedDragon = dragon;
            }
        }

        // Le temps arrêté ; un coup de cloche tous les douzièmes, un chiffre repousse à chacun. Au douzième, le dégel qui brise.
        void UpdateMidnight()
        {
            FreezeTime();
            // Le silence (3 novembre, le joueur) : plus que l'horloge et le joueur ; toutes ses sources passent par PlayThroughHush.
            Sfx.Hush();
            if (Time.time < m_NextStroke)
                return;

            var stroke = k_Hours + 1 - m_Strokes;
            m_Strokes--;
            m_NextStroke = Time.time + m_StrokeTime;
            // Le premier coup, sur la coupure du bruit de l'arrêt du temps : les manettes vibrent, le temps s'est arrêté.
            if (stroke == 1)
                ArenaPlayer.Rumble(0.6f, 0.3f);
            Toll(1.2f);
            ShowNumeral(stroke - 1);
            FloatingText.Spawn(BellPoint + Vector3.up * 1.5f, "Dong ! " + stroke, k_Night, 1.4f, 1f);
            if (m_Strokes > 0)
                return;

            EndMidnight(true);
            // Les mots du joueur (2 novembre).
            HeadMessages.Show("Le temps reprend son cours", k_Gold, 2.5f);
            m_Thrown = 0;
            m_HourAngle = 0f;
            m_NextTick = Time.time + 1f;
        }

        // Le rayon atteint par la bulle de Minuit : elle part vite, puis ralentit en arrivant au bord.
        float MidnightReach
        {
            get
            {
                if (m_MidnightStart < 0f)
                    return 0f;
                var t = Mathf.Clamp01((Time.time - m_MidnightStart) / Mathf.Max(0.05f, m_BubbleGrowTime));
                return m_MidnightRadius * (1f - (1f - t) * (1f - t));
            }
        }

        // Le temps arrêté : les ennemis dans la bulle figés (un boss un instant), tes flèches arrêtées en l'air.
        void FreezeTime()
        {
            var left = Mathf.Max(0.1f, m_MidnightEnd - Time.time);
            var reach = MidnightReach;
            foreach (var enemy in EnemiesNear(Hub, reach, false))
            {
                // Dans l'arène, MK a son temps arrêté à lui (DragonBoss.StopTime) et sa part de PV seulement.
                if (enemy.IsTrainingDummy || enemy.IsAlly || (InArena && enemy.IsBoss) || !m_Frozen.Add(enemy))
                    continue;
                // Les boss des vagues aussi, toute la durée (le joueur, 3 novembre : « la même durée que le reste ») ; le Beffroi ne
                // les figeait que 2 s.
                enemy.Freeze(left);
            }

            if (Time.time < m_NextArrowScan)
                return;
            m_NextArrowScan = Time.time + 0.05f;
            var radius = reach * reach;
            // La liste des flèches actives, à l'envers (3 novembre : plus de recherche dans toute la scène).
            var arrows = Arrow.Active;
            for (var i = arrows.Count - 1; i >= 0; i--)
            {
                var arrow = i < arrows.Count ? arrows[i] : null;
                if (arrow == null || arrow.IsHovering || Stops(arrow))
                    continue;
                var flat = arrow.transform.position - Hub;
                flat.y = 0f;
                if (flat.sqrMagnitude > radius)
                    continue;

                var velocity = arrow.Velocity;
                if (velocity.sqrMagnitude < 1f)
                    continue;
                arrow.Hover(true);
                if (arrow.IsHovering)
                    m_Stopped.Add(new Stopped { Arrow = arrow, Velocity = velocity });
            }
        }

        bool Stops(Arrow arrow)
        {
            foreach (var stopped in m_Stopped)
            {
                if (stopped.Arrow == arrow)
                    return true;
            }

            return false;
        }

        // Le douzième coup : les flèches repartent toutes ensemble, les ennemis reprennent ; et (shatter) le dégel qui brise : le givre
        // de tous les figés éclate d'un coup.
        void EndMidnight(bool shatter)
        {
            Sfx.Unhush();
            foreach (var stopped in m_Stopped)
            {
                if (stopped.Arrow == null || !stopped.Arrow.IsHovering)
                    continue;
                stopped.Arrow.Hover(false);
                stopped.Arrow.Velocity = stopped.Velocity;
            }

            if (m_StoppedDragon != null)
            {
                m_StoppedDragon.ResumeTime();
                if (shatter)
                    HurtShare(m_StoppedDragon.GetComponentInParent<Enemy>(), m_ArenaShatterShare, m_StoppedDragon.BodyPoint);
                if (shatter)
                    ShatterAt(m_StoppedDragon.BodyPoint, true);
                m_StoppedDragon = null;
            }

            var shown = 0;
            foreach (var enemy in m_Frozen)
            {
                if (enemy == null)
                    continue;
                enemy.Thaw();
                if (!shatter || !enemy.IsAlive)
                    continue;

                var point = enemy.Center;
                if (enemy.IsBoss)
                    HurtBoss(enemy, m_ShatterDamage, m_ShatterBossShare, point);
                else
                    Hurt(enemy, m_ShatterDamage, point, Vector3.up);
                // Les éclats sur tous ; l'aurore et le mot sur les huit premiers (le casque).
                FrostBurst(point, 24);
                if (shown++ < 8)
                    ShatterAt(point, false);
            }

            if (shatter)
            {
                Sfx.PlayThroughHush(m_ResumeClip, CenterPoint, 1f, 1f);
                Sfx.PlayThroughHush(m_ShatterClip, CenterPoint, 1f, 1f);
                FrostBurst(CenterPoint, 120);
                SkyFlash(0.8f, true);
                Rumble(Hub, 0.6f);
            }

            if (m_Stopped.Count > 0)
                Burst(CenterPoint, k_Gold, 60, 10f, 0.3f, 0.9f, 0.3f);
            m_Stopped.Clear();
            m_Frozen.Clear();
            m_Strokes = 0;
            m_MidnightStart = -1f;
            m_Spin = 1f;
        }

        // Un figé qui éclate : l'aurore, le mot, les éclats de glace.
        void ShatterAt(Vector3 point, bool big)
        {
            Aurora(point, big ? 1.4f : 1f);
            FloatingText.Spawn(point + Vector3.up, "Brisé !", k_Ice, 1.2f, 1f);
            ThrowDebris(point, big ? 10 : 4, 0.6f);
            Sfx.PlayThroughHush(m_ImpactClip, point, 0.9f, Random.Range(0.9f, 1.1f));
        }

        // La bulle qui grandit (sa paroi se voit pendant qu'elle passe, puis elle s'efface), et le noir et blanc pour qui est dedans :
        // un volume global à part, au-dessus de celui de la carte, dont le poids suit la bulle.
        void UpdateBubble()
        {
            var reach = MidnightReach;
            var growing = m_MidnightStart >= 0f && Time.time - m_MidnightStart < m_BubbleGrowTime + 0.4f;
            if (m_BubbleMaterial != null && (growing || (m_Bubble != null && m_Bubble.gameObject.activeSelf)))
            {
                EnsureBubble();
                m_Bubble.gameObject.SetActive(growing);
                m_Bubble.position = Hub;
                m_Bubble.localScale = Vector3.one * (2f * Mathf.Max(0.1f, reach));
            }

            var viewer = Camera.main != null ? Camera.main.transform.position : Hub;
            var inside = m_MidnightStart >= 0f && (viewer - Hub).sqrMagnitude <= reach * reach;
            if (!inside && m_Grayscale == null)
                return;

            EnsureGrayscale();
            m_Grayscale.weight = Mathf.MoveTowards(m_Grayscale.weight, inside ? 1f : 0f, Time.deltaTime * (inside ? 6f : 2.5f));
            m_Grayscale.enabled = m_Grayscale.weight > 0.001f;
        }

        void EnsureBubble()
        {
            if (m_Bubble != null)
                return;

            var go = new GameObject("Bulle de Minuit (effet)");
            go.AddComponent<MeshFilter>().sharedMesh = TwoSidedSphere();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_BubbleMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            m_Bubble = go.transform;
        }

        void EnsureGrayscale()
        {
            if (m_Grayscale != null)
                return;

            m_GrayProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var adjustments = m_GrayProfile.Add<ColorAdjustments>(true);
            adjustments.saturation.Override(m_BubbleSaturation);
            var go = new GameObject("Minuit, noir et blanc (effet)");
            m_Grayscale = go.AddComponent<Volume>();
            m_Grayscale.isGlobal = true;
            m_Grayscale.priority = 100f;
            m_Grayscale.sharedProfile = m_GrayProfile;
            m_Grayscale.weight = 0f;
        }

        // Une sphère vue des deux côtés : sa paroi se voit en arrivant, et encore de l'intérieur quand elle est passée.
        static Mesh TwoSidedSphere()
        {
            if (s_TwoSidedSphere != null)
                return s_TwoSidedSphere;

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var source = primitive.GetComponent<MeshFilter>().sharedMesh;
            var triangles = source.triangles;
            var both = new int[triangles.Length * 2];
            triangles.CopyTo(both, 0);
            for (var i = 0; i < triangles.Length; i += 3)
            {
                both[triangles.Length + i] = triangles[i];
                both[triangles.Length + i + 1] = triangles[i + 2];
                both[triangles.Length + i + 2] = triangles[i + 1];
            }

            s_TwoSidedSphere = new Mesh
            {
                name = "Sphère deux faces (Horloge)",
                vertices = source.vertices,
                normals = source.normals,
                uv = source.uv,
                triangles = both,
            };
            Destroy(primitive);
            return s_TwoSidedSphere;
        }

        // Un coup de la cloche : elle se balance, le son, les éclats de givre.
        void Toll(float volume)
        {
            m_Swing = 1f;
            Sfx.PlayThroughHush(m_BellClip, BellPoint, volume, Random.Range(0.97f, 1.02f));
            FrostBurst(BellPoint, 24);
        }

        // ---------------------------------------------------------------- l'écho du temps (tes flèches)

        // Une flèche tirée qui touche (celles de la pluie de flèches et du multishot comprises) : un écho de plus, dans 1 s. Sur la
        // carte, l'ennemi touché à sa portée, ou l'ennemi au sol le plus proche d'une flèche dans le sol ; dans l'arène, MK seulement.
        void OnArrowHit(ArrowHit hit)
        {
            if (!hit.IsShot || !IsBuilt || IsUnderConstruction || !m_Awake)
                return;

            // Dans l'arène, plus d'action à chaque flèche sur MK (8 novembre au soir, le joueur : il a tué MK d'une traite, cloué
            // au sol ; « les ultimes devraient uniquement faire leurs effets uniques sur MK ») : seulement l'effet unique, tous les
            // TowerUltimate.MkUniqueInterval s au moins.
            if (InArena)
                return;

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

        // La file pleine (la pluie de flèches), la plus faible laisse sa place, la plus ancienne à force égale (3 novembre : un écho
        // frappe selon la force de sa flèche, TowerUltimate.ShotPower).
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

            m_Orders.Add(new Order { Enemy = enemy, Dragon = dragon, Point = point, At = Time.time + m_EchoDelay, Power = power });
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
            if (order.Enemy != null && order.Enemy.IsAlive)
                return order.Enemy.IsFlyer ? order.Enemy.Center : order.Enemy.transform.position;
            return order.Point;
        }

        // Un écho par flèche, à son heure, au plus un toutes les Echo Interval s : une flèche de glace géante part du cadran.
        void UpdateEchoes()
        {
            while (m_Orders.Count > 0 && Time.time - m_Orders[0].At > k_OrderLife)
                m_Orders.RemoveAt(0);
            if (m_Orders.Count == 0 || Time.time < m_Orders[0].At || Time.time < m_LastEcho + Mathf.Max(0.05f, m_EchoInterval) ||
                m_ArrowTemplate == null)
                return;

            var order = m_Orders[0];
            m_Orders.RemoveAt(0);
            m_LastEcho = Time.time;
            var air = order.Dragon != null || (order.Enemy != null && order.Enemy.IsFlyer);
            var shot = Throw(m_ArrowTemplate, CenterPoint, order.Enemy, order.Dragon, OrderPoint(order), air, true);
            if (shot != null)
                shot.Power = order.Power;
            Sfx.PlayThroughHush(m_ChimeClip, CenterPoint, 0.6f, Random.Range(1.1f, 1.3f));
            FrostBurst(CenterPoint, 16);
            m_GlowFlare = Mathf.Max(m_GlowFlare, 0.6f);
        }

        // ---------------------------------------------------------------- ce qu'elle lance

        // Un chiffre (en cloche, en tournant) ou une flèche de glace (droite et vite, dans le sens du vol), vers ce point qui suit sa cible.
        Shot Throw(Transform source, Vector3 from, Enemy enemy, DragonBoss dragon, Vector3 point, bool air, bool echo)
        {
            var shot = TakeShot(source, echo);
            if (shot == null)
                return null;

            var flat = point - from;
            flat.y = 0f;
            var distance = flat.magnitude;
            shot.Enemy = enemy;
            shot.Dragon = dragon;
            shot.Air = air;
            shot.Echo = echo;
            shot.Numeral = -1;
            shot.Power = 1f;
            shot.From = from;
            shot.Point = point;
            shot.Born = Time.time;
            shot.LandedAt = -1f;
            shot.SpinAxis = distance > 0.01f ? Vector3.Cross(Vector3.up, flat / distance) : transform.right;
            if (echo)
            {
                shot.Duration = 0.3f + distance / 70f;
                shot.Arc = 1.5f * Size + distance * 0.06f;
            }
            else
            {
                shot.Duration = 0.55f + distance / 30f;
                shot.Arc = 4f * Size + distance * 0.3f;
            }

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

        Shot TakeShot(Transform source, bool echo)
        {
            foreach (var shot in m_Shots)
            {
                if (shot.Source == source && !shot.InUse && shot.Go != null)
                    return shot;
            }

            var go = MakeCopy(source, echo ? "Écho du temps (flèche de glace)" : "Chiffre lancé (" + source.name + ")");
            if (go == null)
                return null;
            go.SetActive(false);
            var scale = source.lossyScale * (echo ? m_EchoScale : 1f);
            var made = new Shot { Go = go, Source = source, Scale = scale, Trail = AddTrail(go.transform, (echo ? 1.2f : 0.9f) * Size, echo ? 0.4f : 0.35f) };
            m_Shots.Add(made);
            return made;
        }

        // Ce qui vole suit sa cible ; un chiffre tourne sur lui-même, une flèche se tourne dans le sens de son vol. Au bout : son coup.
        // Une flèche reste plantée 0,5 s, puis éclate en givre.
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
                    if (Time.time - shot.LandedAt < 0.5f)
                        continue;
                    FrostBurst(body.position, 30);
                    shot.LandedAt = -1f;
                    shot.Go.SetActive(false);
                    continue;
                }

                if (shot.Dragon != null)
                    shot.Point = shot.Dragon.BodyPoint;
                else if (shot.Enemy != null && shot.Enemy.IsAlive)
                    shot.Point = shot.Air ? shot.Enemy.Center : shot.Enemy.transform.position;

                var t = Mathf.Clamp01((Time.time - shot.Born) / Mathf.Max(0.05f, shot.Duration));
                var previous = body.position;
                var position = Vector3.Lerp(shot.From, shot.Point, t) + Vector3.up * (shot.Arc * 4f * t * (1f - t));
                body.position = position;
                if (shot.Echo)
                {
                    // La flèche de glace du modèle pointe le long de son axe +z.
                    var velocity = position - previous;
                    if (velocity.sqrMagnitude > 1e-6f)
                        body.rotation = Quaternion.LookRotation(velocity.normalized);
                }
                else
                {
                    body.Rotate(shot.SpinAxis, 520f * Time.deltaTime, Space.World);
                }

                if (t < 1f)
                    continue;

                shot.Flying = false;
                if (shot.Trail != null)
                    shot.Trail.emitting = false;
                if (shot.Echo && !shot.Air)
                    shot.LandedAt = Time.time;
                else
                    shot.Go.SetActive(false);

                if (shot.Echo)
                    EchoImpact(shot.Point, shot.Air, shot.Dragon, shot.Power);
                else
                    NumeralImpact(shot.Point, shot.Air, shot.Dragon, shot.Numeral);
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

        // Une copie d'une de ses pièces (un chiffre, la flèche de glace), fondue en un seul maillage par matériau (une fois par pièce).
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
                // La glace qui luit : la copie animée, pas le matériau de l'asset.
                if (m_Glow != null)
                {
                    for (var i = 0; i < copy.Materials.Length; i++)
                    {
                        if (copy.Materials[i] == m_GlowMaterial)
                            copy.Materials[i] = m_Glow;
                    }
                }

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

            var mesh = new Mesh { name = "Grande Horloge (" + name + ")", indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts, false, false);
            mesh.RecalculateBounds();
            foreach (var part in temporary)
                Destroy(part);
            return (mesh, order.ToArray());
        }

        // ---------------------------------------------------------------- les coups

        static bool Near(Enemy enemy, Vector3 point, float radius, bool air)
        {
            var offset = (air ? enemy.Center : enemy.transform.position) - point;
            var height = offset.y;
            offset.y = 0f;
            return air ? offset.sqrMagnitude + height * height <= radius * radius * 0.5f
                : offset.sqrMagnitude <= radius * radius && height > -1.5f && height < 4f;
        }

        // Les dégâts d'un coup : moitié au bord ; ceux qui survivent sont projetés (s'il le faut) et figés de givre. Un boss des vagues
        // prend les mêmes dégâts, plus une part de ses PV max (comme le Colosse, 12.50) ; il n'est jamais projeté.
        void Smash(Vector3 point, bool air, float damage, float radius, float push, float lift, float freeze, float bossShare)
        {
            m_Scratch.Clear();
            foreach (var enemy in AliveEnemies())
            {
                if (enemy != null && enemy.IsAlive && !enemy.IsAlly && !enemy.IsTrainingDummy && Near(enemy, point, radius, air))
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

                if (!Hurt(enemy, amount, point, enemy.Center - point))
                    continue;
                if (push + lift > 0f && !enemy.IsFlyer && enemy.CanBeLaunched && KnockDown(enemy, point, push, lift))
                    continue;
                if (freeze > 0f)
                    enemy.Freeze(freeze);
            }
        }

        // Un chiffre tombe : les dégâts, le givre, l'aurore (le signe), son nom en lettres de glace.
        void NumeralImpact(Vector3 point, bool air, DragonBoss dragon, int numeral)
        {
            if (dragon != null)
                HurtShare(dragon.GetComponentInParent<Enemy>(), m_NumeralBossShare, point);
            else
                Smash(point, air, m_NumeralDamage, m_NumeralRadius, 0f, 0f, m_NumeralFreeze, m_NumeralBossShare);

            // Plus petite que celle d'un écho (le joueur, 3 novembre : « trop grosses »).
            Aurora(point, 0.5f);
            FrostBurst(point + Vector3.up * (0.4f * Size), 40);
            if (!air)
                MistRing(point, m_NumeralRadius * 0.5f, 2);
            if (numeral >= 0 && numeral < k_Roman.Length)
                FloatingText.Spawn(point + Vector3.up * 1.5f, k_Roman[numeral], k_Ice, 1.6f, 1f);
            Sfx.PlayThroughHush(m_ImpactClip, point, 0.8f, Random.Range(0.9f, 1.1f));
            Heavy(point, air, false);
        }

        // Un écho frappe (comme les marteaux du Colosse et les chevaux du Manège) : l'aurore, l'onde de givre, les survivants projetés
        // et figés ; la flèche reste plantée un instant. Ses dégâts (et sa part des PV d'un boss) suivent la force de la flèche qu'il
        // rejoue (power, 3 novembre).
        void EchoImpact(Vector3 point, bool air, DragonBoss dragon, float power)
        {
            if (dragon != null)
                HurtShare(dragon.GetComponentInParent<Enemy>(), m_EchoBossShare * power, point);
            else
                Smash(point, air, m_EchoDamage * power, m_EchoRadius, m_EchoPush, m_EchoLift, m_EchoFreeze, m_EchoBossShare * power);

            Aurora(point, 1.1f);
            FrostBurst(point + Vector3.up * (0.6f * Size), 70);
            Sfx.PlayThroughHush(m_ImpactClip, point, 1f, Random.Range(0.6f, 0.75f));
            Sfx.PlayThroughHush(m_ChimeClip, point, 0.5f, Random.Range(0.9f, 1.05f));
            if (!air && dragon == null)
            {
                Shockwave(point, m_EchoRadius * 2f, 0.4f, k_Shock);
                MistRing(point, m_EchoRadius * 0.6f, 4);
            }

            Heavy(point, air || dragon != null, true);
        }

        // Le coup complet, au plus toutes les Heavy Interval s : la lumière, l'éclair de couleur dans le ciel, les éclats de glace, le
        // givre au sol, le grand impact, les manettes.
        void Heavy(Vector3 point, bool air, bool echo)
        {
            if (Time.time < m_NextHeavy)
                return;
            m_NextHeavy = Time.time + m_HeavyInterval;
            FlashAt(point + Vector3.up * 3f, k_Ice, echo ? 8f : 6f, 28f, 0.4f);
            SkyFlash(0.3f, false);
            Rumble(point, echo ? 0.6f : 0.4f);
            if (air)
                return;
            ThrowDebris(point, echo ? 8 : 5, 0.6f);
            Frost(point, echo ? 3f : 2.5f, 3f);
        }

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

        static DragonBoss ArenaDragon()
        {
            var dragon = DragonBoss.Instance;
            if (dragon == null || !dragon.isActiveAndEnabled || dragon.IsHidden)
                return null;
            var enemy = dragon.GetComponentInParent<Enemy>();
            return enemy != null && enemy.IsAlive ? dragon : null;
        }

        // ---------------------------------------------------------------- l'horloge qui vit

        // Le cadran se tourne vers le joueur (on lit ses chiffres).
        void FaceViewer()
        {
            if (m_Face == null)
                return;
            var rig = PlayerRig.Instance;
            var viewer = rig != null ? rig.BodyPosition : Camera.main != null ? Camera.main.transform.position : m_Face.position - transform.forward;
            var flat = viewer - m_Face.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1f)
                return;
            var goal = Quaternion.LookRotation(flat.normalized);
            m_Face.rotation = m_WakeAt >= 0f && m_WakeStep < 2 ? goal : Quaternion.RotateTowards(m_Face.rotation, goal, 40f * Time.deltaTime);
        }

        // Elle flotte ; ses anneaux et ses engrenages tournent (pas pendant Minuit : le temps est arrêté) ; ses aiguilles : les heures
        // avec les chiffres partis, les minutes font un tour à chaque tic, la trotteuse saute ; la cloche se balance.
        void Animate()
        {
            var dt = Time.deltaTime;
            var frozen = InMidnight;
            if (m_Float != null && !frozen)
                m_Float.localPosition = m_FloatRest + Vector3.up * (Mathf.Sin(Time.time * 0.9f) * 0.35f);

            for (var i = 0; i < m_Rings.Length; i++)
            {
                if (m_Rings[i] != null && !frozen)
                    m_Rings[i].Rotate(Vector3.up, m_RingSpeeds[i % m_RingSpeeds.Length] * m_Spin * dt, Space.Self);
            }

            for (var i = 0; i < m_Gears.Length; i++)
            {
                if (m_Gears[i] != null && !frozen)
                    m_Gears[i].Rotate(Vector3.forward, m_GearSpeeds[i % m_GearSpeeds.Length] * m_Spin * dt, Space.Self);
            }

            if (!frozen && m_WakeAt < 0f)
            {
                m_HourAngle = Mathf.MoveTowards(m_HourAngle, m_Thrown * 30f, 120f * dt);
                if (m_MinuteSpin > 0f)
                {
                    var step = Mathf.Min(m_MinuteSpin, 1400f * dt);
                    m_MinuteSpin -= step;
                    m_MinuteAngle += step;
                }
            }

            if (m_HourHand != null)
                m_HourHand.localRotation = Quaternion.Euler(0f, 0f, m_HourAngle);
            if (m_MinuteHand != null)
                m_MinuteHand.localRotation = Quaternion.Euler(0f, 0f, m_MinuteAngle);
            if (m_SecondHand != null)
                m_SecondHand.localRotation = Quaternion.Slerp(m_SecondHand.localRotation, Quaternion.Euler(0f, 0f, m_SecondAngle), dt * 25f);

            if (m_Bell != null)
            {
                m_Swing = Mathf.MoveTowards(m_Swing, 0f, dt * 0.5f);
                m_Bell.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 7f) * 28f * m_Swing, 0f, 0f);
            }
        }

        // La glace qui luit : une copie du matériau, posée sur ce qui est fait de lui (après le plan bleu), puis animée.
        void SetupGlow()
        {
            if (m_GlowMaterial == null)
                return;
            if (m_Glow == null)
            {
                m_Glow = new Material(m_GlowMaterial) { name = "Glace lumineuse de l'Horloge (en jeu)" };
                m_Glow.EnableKeyword("_EMISSION");
            }

            m_GlowRenderers.Clear();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                var changed = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == m_GlowMaterial)
                    {
                        materials[i] = m_Glow;
                        changed = true;
                    }
                }

                if (!changed)
                    continue;
                renderer.sharedMaterials = materials;
                m_GlowRenderers.Add(renderer);
            }
        }

        // L'éclat de la glace : il palpite, monte à mesure que le XII approche, s'emballe aux grands moments.
        void UpdateGlow()
        {
            m_GlowFlare = Mathf.MoveTowards(m_GlowFlare, 0f, Time.deltaTime * 1.2f);
            if (m_Glow == null)
                return;
            var near = Mathf.Clamp01(m_Thrown / (float)k_Hours);
            var pulse = 1f + 0.12f * Mathf.Sin(Time.time * 3.1f);
            var level = m_GlowLevel * pulse * (1f + 0.8f * near + m_GlowFlare);
            m_Glow.SetColor(k_EmissionColorId, k_Ice * (2.6f * level));
            if (m_CoreLight != null)
                m_CoreLight.intensity = 2.4f * level;
        }

        // Les particules qui ne s'arrêtent pas : les flocons autour d'elle, les éclats de givre autour des anneaux, la brume froide au
        // pied ; et sa lumière froide.
        void StartLoops()
        {
            if (m_Loops.Count > 0 || m_Float == null)
                return;

            Loop("Flocons", m_Float, Vector3.up * 6f, new Vector3(90f, 0f, 0f), 18f, new Vector2(0.1f, 0.5f), new Vector2(0.06f, 0.14f),
                 new Vector2(4f, 6f), 0.05f, ParticleSystemShapeType.Circle, 9f, 0f, Color.white, k_Frost);
            Loop("Givre des anneaux", m_Float, Vector3.zero, Vector3.zero, 14f, new Vector2(0.2f, 0.8f), new Vector2(0.05f, 0.12f),
                 new Vector2(1.2f, 2f), -0.05f, ParticleSystemShapeType.Sphere, 7f, 0f, k_Frost, k_Ice);
            Loop("Brume froide", transform, Vector3.up * 0.4f, new Vector3(-90f, 0f, 0f), 5f, new Vector2(0.4f, 1f), new Vector2(0.4f, 0.8f),
                 new Vector2(2.5f, 3.5f), -0.02f, ParticleSystemShapeType.Circle, 3f, 0f, new Color(0.8f, 0.9f, 1f, 1f), new Color(0.6f, 0.75f, 0.9f, 1f), 1.5f);
            if (m_CoreLight == null)
            {
                var go = new GameObject("Lumière de l'Horloge");
                go.transform.SetParent(m_Float, false);
                go.transform.localPosition = Vector3.forward * 1.5f;
                m_CoreLight = go.AddComponent<Light>();
                m_CoreLight.type = LightType.Point;
                m_CoreLight.color = new Color(0.55f, 0.8f, 1f);
                m_CoreLight.range = 22f;
                m_CoreLight.shadows = LightShadows.None;
            }
        }

        void Loop(string name, Transform parent, Vector3 at, Vector3 euler, float rate, Vector2 speed, Vector2 size, Vector2 life,
                  float gravity, ParticleSystemShapeType shapeType, float radius, float coneAngle, Color from, Color to, float grow = 1f)
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
            main.startColor = new ParticleSystem.MinMaxGradient(from, to);
            main.gravityModifier = gravity;
            main.maxParticles = 250;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = rate;
            var shape = particles.shape;
            shape.shapeType = shapeType;
            shape.angle = coneAngle;
            shape.radius = radius;
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
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

        // ---------------------------------------------------------------- le ciel de Minuit

        // Passager : pendant l'éveil, puis le temps de Minuit (et quand les aiguilles se rejoignent) ; le reste du temps, et dans
        // l'arène, le ciel est celui de la partie.
        void UpdateSky()
        {
            var sky = SkyController.Instance;
            if (sky == null)
                return;

            var target = InArena ? 0f : m_WakeAt >= 0f ? 1f : InMidnight || m_MidnightAt >= 0f ? m_SkyMidnight : 0f;
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

        void SkyFlash(float strength, bool force)
        {
            if (InArena || (!force && Time.time < m_NextSkyFlash))
                return;
            var sky = SkyController.Instance;
            if (sky != null)
                sky.Strike(strength);
            m_NextSkyFlash = Time.time + m_SkyFlashInterval;
        }

        // ---------------------------------------------------------------- l'aurore, le signe de chaque coup

        // Une aurore tombe du ciel sur ce point : trois rubans de lumière, verts, bleus et violets, qui ondulent et s'effacent. Dix
        // au plus à la fois (la plus ancienne est reprise).
        void Aurora(Vector3 point, float size)
        {
            Curtain curtain = null;
            foreach (var c in m_Curtains)
            {
                if (Time.time - c.Born >= m_AuroraLife)
                {
                    curtain = c;
                    break;
                }
            }

            if (curtain == null && m_Curtains.Count < 10)
            {
                curtain = new Curtain();
                for (var i = 0; i < curtain.Lines.Length; i++)
                {
                    var go = new GameObject("Aurore de l'Horloge");
                    var line = go.AddComponent<LineRenderer>();
                    line.positionCount = 14;
                    line.useWorldSpace = true;
                    line.sharedMaterial = TrailMaterial;
                    line.shadowCastingMode = ShadowCastingMode.Off;
                    line.receiveShadows = false;
                    line.enabled = false;
                    curtain.Lines[i] = line;
                }

                m_Curtains.Add(curtain);
            }

            if (curtain == null)
            {
                curtain = m_Curtains[0];
                foreach (var c in m_Curtains)
                {
                    if (c.Born < curtain.Born)
                        curtain = c;
                }
            }

            curtain.Point = point;
            curtain.Born = Time.time;
            curtain.Size = size * Size;
            curtain.Phase = Random.Range(0f, 10f);
            foreach (var line in curtain.Lines)
                line.enabled = true;
            DrawCurtain(curtain);
        }

        void UpdateCurtains()
        {
            foreach (var curtain in m_Curtains)
            {
                if (curtain.Lines[0] == null || !curtain.Lines[0].enabled)
                    continue;
                if (Time.time - curtain.Born >= m_AuroraLife)
                {
                    foreach (var line in curtain.Lines)
                        line.enabled = false;
                    continue;
                }

                DrawCurtain(curtain);
            }
        }

        // Elle tombe vite du ciel (0,15 s), ondule, puis s'efface ; plus claire au milieu de sa hauteur, transparente en haut et en bas.
        void DrawCurtain(Curtain curtain)
        {
            var age = Time.time - curtain.Born;
            var alpha = Mathf.Clamp01(age / 0.15f) * Mathf.Clamp01((m_AuroraLife - age) / 0.6f);
            var height = m_AuroraHeight * curtain.Size;
            var drop = Mathf.Clamp01(age / 0.15f);
            var view = Camera.main != null ? Camera.main.transform.right : Vector3.right;
            view.y = 0f;
            view = view.sqrMagnitude > 1e-4f ? view.normalized : Vector3.right;
            var colors = m_AuroraColors != null && m_AuroraColors.Length >= 3 ? m_AuroraColors
                : new[] { new Color(0.3f, 1f, 0.55f, 1f), new Color(0.2f, 0.85f, 1f, 1f), new Color(0.6f, 0.4f, 1f, 1f) };
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(colors[0], 0f), new GradientColorKey(colors[1], 0.5f), new GradientColorKey(colors[2], 1f) },
                             new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.75f * alpha, 0.2f), new GradientAlphaKey(0.6f * alpha, 0.6f),
                                     new GradientAlphaKey(0f, 1f) });
            for (var i = 0; i < curtain.Lines.Length; i++)
            {
                var line = curtain.Lines[i];
                line.colorGradient = gradient;
                line.widthMultiplier = m_AuroraWidth * curtain.Size * (i == 1 ? 1.3f : 0.8f);
                var count = line.positionCount;
                for (var j = 0; j < count; j++)
                {
                    var f = j / (float)(count - 1);
                    var y = curtain.Point.y + height * (1f - drop * (1f - f));
                    var wave = Mathf.Sin(f * 5f + Time.time * 2.2f + curtain.Phase + i * 1.7f) * 1.1f * curtain.Size;
                    line.SetPosition(j, new Vector3(curtain.Point.x, y, curtain.Point.z) + view * ((i - 1) * 1.1f * curtain.Size + wave));
                }
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

            for (var i = m_Rings2.Count - 1; i >= 0; i--)
            {
                var ring = m_Rings2[i];
                var t = (Time.time - ring.Born) / ring.Duration;
                if (ring.Body == null || t >= 1f)
                {
                    if (ring.Body != null)
                        Destroy(ring.Body.gameObject);
                    if (ring.Material != null)
                        Destroy(ring.Material);
                    m_Rings2.RemoveAt(i);
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

        // L'onde : un anneau de lumière froide qui s'élargit au ras du sol, et s'efface.
        void Shockwave(Vector3 point, float radius, float duration, Color color)
        {
            if (m_RingMaterial == null)
                return;

            var go = new GameObject("Onde de l'Horloge");
            go.transform.position = point + Vector3.up * 0.2f;
            go.AddComponent<MeshFilter>().sharedMesh = RingMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var material = new Material(m_RingMaterial) { name = "Onde de l'Horloge (en jeu)" };
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            go.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
            m_Rings2.Add(new Ring { Body = go.transform, Material = material, Color = color, Born = Time.time, Duration = duration, Radius = radius });
        }

        // Le givre au sol (le matériau du givre), qui s'efface seul.
        void Frost(Vector3 point, float radius, float time)
        {
            if (m_FrostMaterial != null)
                GroundPatch.Spawn(point, radius * Size, time, 1f, m_FrostMaterial, this);
        }

        // Les éclats de glace qui volent, en tournant.
        void ThrowDebris(Vector3 point, int count, float strength)
        {
            for (var i = 0; i < count; i++)
            {
                var go = Primitive(PrimitiveType.Cube, "Éclat de glace", m_DebrisMaterial);
                var size = Random.Range(0.1f, 0.28f);
                go.transform.localScale = new Vector3(size * 0.6f, size * Random.Range(1f, 1.8f), size * 0.6f);
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

        // Une traînée de givre derrière ce qu'elle lance.
        static TrailRenderer AddTrail(Transform at, float width, float time)
        {
            var trail = at.gameObject.AddComponent<TrailRenderer>();
            trail.time = time;
            trail.minVertexDistance = 0.2f;
            trail.widthMultiplier = width;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(k_Ice, 0.4f),
                                     new GradientColorKey(new Color(0.5f, 0.45f, 1f), 1f) },
                             new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.sharedMaterial = TrailMaterial;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            return trail;
        }

        // Les éclats de givre et la poussière de glace des coups : deux systèmes de particules à elle, déplacés là où ça frappe.
        void FrostBurst(Vector3 point, int count) => EmitAt(ref m_FrostFx, true, point, count);

        void Mist(Vector3 point, int count) => EmitAt(ref m_MistFx, false, point, count);

        void MistRing(Vector3 point, float radius, int count)
        {
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f;
                Mist(point + new Vector3(Mathf.Cos(angle) * radius, 0.3f, Mathf.Sin(angle) * radius), count);
            }
        }

        void EmitAt(ref ParticleSystem system, bool frost, Vector3 point, int count)
        {
            if (system == null)
                system = MakeEmitter(frost);
            if (system == null)
                return;
            system.transform.position = point;
            system.Emit(count);
        }

        ParticleSystem MakeEmitter(bool frost)
        {
            var arrows = SpecialArrows.Instance;
            var material = arrows != null ? arrows.EffectMaterial : null;
            if (material == null)
                return null;

            var go = new GameObject(frost ? "Givre des coups" : "Poussière de glace des coups");
            go.transform.SetParent(transform, false);
            if (!frost)
                go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = frost ? new ParticleSystem.MinMaxCurve(0.5f, 1.2f) : new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
            main.startSpeed = frost ? new ParticleSystem.MinMaxCurve(3f, 11f) : new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            // Plus petits (le joueur, 3 novembre : les particules de glace au pied et à l'impact des flèches, « trop grosses »).
            main.startSize = frost ? new ParticleSystem.MinMaxCurve(0.05f, 0.14f) : new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startColor = frost ? new ParticleSystem.MinMaxGradient(Color.white, k_Ice)
                : new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.93f, 1f, 1f), new Color(0.65f, 0.78f, 0.9f, 1f));
            main.gravityModifier = frost ? 0.6f : -0.04f;
            main.maxParticles = frost ? 900 : 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.shapeType = frost ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Hemisphere;
            shape.radius = frost ? 0.3f : 0.6f;
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colors.color = fade;
            if (!frost)
            {
                var sizes = particles.sizeOverLifetime;
                sizes.enabled = true;
                sizes.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 1.6f));
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

        // Tout s'arrête : ce qui vole disparaît, les échos en attente sont oubliés, les chiffres reviennent au cadran.
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
            m_MidnightAt = -1f;
            m_Thrown = 0;
            m_MinuteSpin = 0f;
            m_Spin = 1f;
            if (m_Awake)
            {
                for (var k = 0; k < m_Numerals.Length; k++)
                {
                    if (m_Numerals[k] != null)
                        m_Numerals[k].gameObject.SetActive(true);
                }
            }

            foreach (var curtain in m_Curtains)
            {
                foreach (var line in curtain.Lines)
                {
                    if (line != null)
                        line.enabled = false;
                }
            }
        }

        void ClearEffects()
        {
            DestroyShots();
            foreach (var curtain in m_Curtains)
            {
                foreach (var line in curtain.Lines)
                {
                    if (line != null)
                        Destroy(line.gameObject);
                }
            }

            m_Curtains.Clear();
            foreach (var debris in m_Debris)
            {
                if (debris.Body != null)
                    Destroy(debris.Body.gameObject);
            }

            foreach (var ring in m_Rings2)
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
            m_Rings2.Clear();
            m_Flashes.Clear();
        }

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

                s_RingMesh = new Mesh { name = "Onde de l'Horloge" };
                s_RingMesh.SetVertices(vertices);
                s_RingMesh.SetTriangles(triangles, 0);
                s_RingMesh.RecalculateNormals();
                s_RingMesh.RecalculateBounds();
                return s_RingMesh;
            }
        }

        // Le matériau des traînées et des aurores : couleurs par sommet, sans lumière (celui des éclairs).
        static Material TrailMaterial
        {
            get
            {
                if (s_TrailMaterial != null)
                    return s_TrailMaterial;
                var shader = Shader.Find("Archery/UnlitVertexColor");
                if (shader == null)
                    shader = Shader.Find("Sprites/Default");
                s_TrailMaterial = new Material(shader) { name = "Traînée de l'Horloge (en jeu)" };
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
