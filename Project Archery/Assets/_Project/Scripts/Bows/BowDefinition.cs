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
