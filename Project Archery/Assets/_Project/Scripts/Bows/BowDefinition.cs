using UnityEngine;
using UnityEngine.Serialization;

namespace Archery.Bows
{
    /// <summary>
    /// Caractéristiques d'un arc (GDD, section 5). Un asset par arc.
    /// </summary>
    [CreateAssetMenu(fileName = "Bow", menuName = "Archery/Bow Definition")]
    public class BowDefinition : ScriptableObject
    {
        [Header("Boutique")]
        public string displayName = "Arc de chasse";

        [TextArea]
        public string description = "Équilibré.";

        [Tooltip("Pouvoir spécial, affiché sous l'arc dans le menu de choix des arcs. Vide : déduit de ses réglages (flèches " +
                 "qui traversent des ennemis) ; rien si l'arc n'en a pas.")]
        [TextArea(1, 3)]
        public string specialPower;

        public int price;

        [Tooltip("Première vague où l'arc peut apparaître en boutique.")]
        public int availableFromWave;

        [Tooltip("Optionnel : modèle 3D de l'arc (prefab avec un Bow Visual), mis à la place de l'ancien quand on l'achète. " +
                 "Vide : l'arc change de caractéristiques mais garde son apparence.")]
        public BowVisual visualPrefab;

        [Header("Tir")]
        [Tooltip("Vitesse de la flèche (m/s) à tension maximale, avant le bonus de timing.")]
        public float arrowSpeed = 35f;

        public float damage = 10f;

        [Tooltip("Gravité des flèches, en multiple de la normale : au-dessus de 1, elles tombent plus vite et vont moins loin " +
                 "(à 2, deux fois moins loin). L'aide à la visée suit.")]
        public float arrowGravity = 1f;

        [Tooltip("Distance de tirage maximale (m), mesurée depuis la position de repos de la corde.")]
        public float maxDrawDistance = 0.2625f;

        [Tooltip("Distance (m) dont on peut encore tirer la corde au-delà de la tension maximale : la corde suit la main, " +
                 "sans plus de puissance, et l'anneau de timing apparaît toujours à la tension maximale.")]
        public float overdrawDistance = 0.15f;

        [Tooltip("Puissance selon la tension (0 = corde au repos, 1 = tension maximale).")]
        public AnimationCurve drawToPower = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 1f));

        [Tooltip("En dessous de cette tension, lâcher la corde fait juste tomber la flèche.")]
        [Range(0f, 1f)]
        public float minDrawToFire = 0.12f;

        [Tooltip("Nombre d'ennemis qu'une flèche peut traverser.")]
        public int pierceCount;

        /// <summary>Le pouvoir spécial de l'arc, en une phrase ; vide s'il n'en a pas.</summary>
        public string SpecialPowerText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(specialPower))
                    return specialPower.Trim();
                if (pierceCount > 0)
                    return $"Ses flèches traversent {pierceCount} ennemi{(pierceCount > 1 ? "s" : "")}";
                return "";
            }
        }

        [Header("Surcharge (arc cosmique)")]
        [Tooltip("Le vert de l'anneau devient doré, et un deuxième cercle doré suit le premier : lâché dans son doré (ou " +
                 "dans le vert clair, après, un peu moins fort), le tir devient un laser qui traverse tout (ennemis, murs, relief).")]
        public bool overcharge;

        [Tooltip("Durée (s) du deuxième cercle, du bord au centre (plus court que l'anneau).")]
        public float overchargeDuration = 0.9f;

        [Tooltip("Dégâts du laser, en multiple de ceux d'un tir parfait (lâché dans le doré) ou « très bon » (dans le vert " +
                 "clair) ; les points faibles comptent en plus.")]
        public float laserDamageMultiplier = 3f;

        [Tooltip("Portée (m) du laser.")]
        public float laserRange = 120f;

        [Tooltip("Première couleur du laser (violet) : le large halo et une des deux spirales ; aussi la traînée d'étoiles.")]
        [ColorUsage(false, true)]
        public Color laserColor = new Color(0.66f, 0.16f, 1f);

        [Tooltip("Deuxième couleur du laser (bleu) : le rayon autour du cœur blanc et l'autre spirale. Les particules " +
                 "laissées par le laser prennent les deux couleurs.")]
        [ColorUsage(false, true)]
        public Color laserSecondColor = new Color(0.12f, 0.45f, 1f);

        [Tooltip("Sons du tir laser, joués ensemble (on peut superposer une décharge et un grondement).")]
        public AudioClip[] laserClips = new AudioClip[0];

        [Tooltip("Son du deuxième cercle qui part (la charge qui monte).")]
        public AudioClip overchargeClip;

        [Tooltip("Les flèches tirées normalement laissent une traînée d'étoiles, de la couleur du laser.")]
        public bool starTrail;

        [Header("Pluie de flèches (arc légendaire)")]
        [Tooltip("Au sommet de sa course, la flèche se divise : des flèches retombent en pluie autour de son point " +
                 "d'arrivée, et laissent au sol une zone qui brûle les ennemis. Plantée avant (tir tendu), la pluie tombe là.")]
        public bool arrowRain;

        [Tooltip("Nombre de flèches de la pluie, en plus de la flèche tirée (qui tombe au centre).")]
        public int rainArrows = 10;

        [Tooltip("Rayon (m) de la pluie et de la zone au sol (aussi le cercle de l'aide à la visée).")]
        public float rainRadius = 3.5f;

        [Tooltip("Dégâts d'une flèche de la pluie, en part de ceux du tir (qualité comprise).")]
        public float rainDamage = 0.5f;

        [Tooltip("Durée (s) de la zone au sol. 0 : pas de zone.")]
        public float rainZoneDuration = 3f;

        [Tooltip("Dégâts par seconde de la zone, à chaque ennemi au sol dedans, en part de ceux du tir.")]
        public float rainZoneDamage = 0.3f;

        [Tooltip("Couleur des traînées de la pluie et de la zone au sol.")]
        public Color rainColor = new Color(1f, 0.6f, 0.2f);

        [Tooltip("Son de la division (la pluie part).")]
        public AudioClip rainSplitClip;

        [Tooltip("Son en boucle de la zone au sol.")]
        public AudioClip rainZoneClip;

        [Header("Akimbow (arbalètes jumelles)")]
        [Tooltip("Une arbalète dans chaque main à la place de l'arc (Akimbow) : la gâchette de chaque main tire, et l'anneau de " +
                 "chaque arbalète repart tout seul après chaque tir (Ring Duration, plus court qu'un arc). Dégâts, vitesse, " +
                 "gravité et perçage : ceux de cette définition, à pleine puissance. Visual Prefab ne sert qu'à l'aperçu du menu.")]
        public bool akimbo;

        [Tooltip("Modèle d'une arbalète : la poignée à l'origine, le canon vers +Z.")]
        public GameObject akimboModel;

        [Tooltip("Matériau du corps de l'arbalète de la main gauche (Archery/Akimbow : métal en plaques et néons, stable ; " +
                 "il se brouille pendant les bugs).")]
        public Material akimboMaterial;

        [Tooltip("Matériau du corps de l'arbalète de la main droite : une autre arme (couleurs, plaques). Vide : comme la gauche.")]
        public Material akimboMaterialRight;

        [Tooltip("Néons posés sur l'arbalète de gauche (rail, branches, avant, flancs) : deux couleurs.")]
        public Material[] akimboNeon = new Material[0];

        [Tooltip("Néons de l'arbalète de droite. Vide : comme la gauche.")]
        public Material[] akimboNeonRight = new Material[0];

        [Tooltip("Pointe d'une arbalète, d'où partent les flèches, dans le repère du modèle.")]
        public Vector3 akimboTip = new Vector3(0f, 0.096f, 0.285f);

        [Tooltip("Taille des arbalètes, en multiple du modèle.")]
        public float akimboScale = 0.7f;

        [Tooltip("Son d'un tir d'arbalète.")]
        public AudioClip akimboFireClip;

        [Header("Katana et gantelet")]
        [Tooltip("Un katana et un gantelet à la place de l'arc (refaits le 23 octobre, sans symbole à tracer) : une vraie taille " +
                 "de katana ouvre une faille dans la matrice ; frappée vite avec le gantelet, elle part vers l'ennemi le plus " +
                 "proche du centre de la vue et le suit (Damage, Arrow Speed ; qualité selon sa fraîcheur) ; toute la faille " +
                 "touche. Le katana frappe aussi au corps à corps (Katana Damage). Visual Prefab ne sert qu'à l'aperçu du menu.")]
        public bool katana;

        [Tooltip("Modèle du katana : le milieu de la poignée à l'origine, la lame vers +Y, le tranchant vers +Z ; enfants " +
                 "« Garde » (début de la lame) et « Pointe ».")]
        public GameObject katanaModel;

        [Tooltip("Modèle du gantelet, porté sur la main de l'arc : main gauche, le poignet à l'origine, les doigts vers +Z, le " +
                 "dos de la main vers +Y, 19 cm du poignet au bout du majeur ; enfant « Paume » (le point qui frappe les failles).")]
        public GameObject gauntletModel;

        [Tooltip("Dégâts d'un coup de katana (pointe à 2,5 m/s ; jusqu'à ×1,6 à 7 m/s ; tête ×2).")]
        public float katanaDamage = 30f;

        [Tooltip("Matériau des failles (shader Archery/MatrixTear : faille vivante, chiffres qui tombent).")]
        public Material katanaTearMaterial;

        [Tooltip("Son d'un coup de katana qui touche (et d'une faille qui touche).")]
        public AudioClip katanaHitClip;

        [Tooltip("Son d'une faille lancée par le gantelet.")]
        public AudioClip katanaLaunchClip;

        [Tooltip("Son d'une taille rapide de la lame.")]
        public AudioClip katanaSwooshClip;

        [Tooltip("Son de la matrice qui se fend (aigu et doux pour la fissure, franc quand elle s'ouvre d'un coup).")]
        public AudioClip katanaCrackClip;

        [Tooltip("Son d'une faille lancée parfaite (un bug).")]
        public AudioClip katanaSignClip;

        [Header("Anneau de timing")]
        [Tooltip("Temps (s) pour que le cercle d'approche aille du bord au centre.")]
        public float ringDuration = 1.2f;

        [Tooltip("Demi-largeur de la bande verte (tir parfait), en fraction du rayon de l'anneau.")]
        [Range(0f, 0.2f)]
        public float goldHalfWidth = 0.06f;

        [Tooltip("Largeur de la bande orange (bon tir) de chaque côté du vert. Au-delà, c'est le rouge (raté).")]
        [FormerlySerializedAs("greenWidth")]
        [Range(0f, 0.3f)]
        public float goodWidth = 0.14f;
    }
}
