using UnityEngine;

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

        public int price;

        [Tooltip("Première vague où l'arc peut apparaître en boutique.")]
        public int availableFromWave;

        [Header("Tir")]
        [Tooltip("Vitesse de la flèche (m/s) à tension maximale, avant le bonus de timing.")]
        public float arrowSpeed = 35f;

        public float damage = 10f;

        [Tooltip("Distance de tirage maximale (m), mesurée depuis la position de repos de la corde.")]
        public float maxDrawDistance = 0.35f;

        [Tooltip("Puissance selon la tension (0 = corde au repos, 1 = tension maximale).")]
        public AnimationCurve drawToPower = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 1f));

        [Tooltip("En dessous de cette tension, lâcher la corde fait juste tomber la flèche.")]
        [Range(0f, 1f)]
        public float minDrawToFire = 0.12f;

        [Tooltip("Nombre d'ennemis qu'une flèche peut traverser.")]
        public int pierceCount;

        [Header("Anneau de timing")]
        [Tooltip("Temps (s) pour que le cercle d'approche aille du bord au centre.")]
        public float ringDuration = 1.2f;

        [Tooltip("Demi-largeur de la bande dorée, en fraction du rayon de l'anneau.")]
        [Range(0f, 0.2f)]
        public float goldHalfWidth = 0.06f;

        [Range(0f, 0.2f)]
        public float greenWidth = 0.08f;

        [Range(0f, 0.2f)]
        public float orangeWidth = 0.1f;
    }
}
