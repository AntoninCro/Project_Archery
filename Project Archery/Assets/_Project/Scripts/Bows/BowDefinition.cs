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
