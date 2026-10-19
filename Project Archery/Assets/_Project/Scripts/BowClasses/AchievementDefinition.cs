using UnityEngine;

namespace Archery.BowClasses
{
    /// <summary>Ce qu'un succès compte.</summary>
    public enum AchievementType
    {
        /// <summary>Ennemis tués, toutes parties confondues (flèches, pièges, balistes…).</summary>
        EnemiesKilled,

        /// <summary>Flèches tirées qui touchent une tête, quelle que soit la qualité du tir.</summary>
        Headshots,

        /// <summary>Flèches tirées dans le vert de l'anneau (tir parfait) qui touchent une tête.</summary>
        PerfectHeadshots,

        /// <summary>Tirs lâchés dans le vert de l'anneau.</summary>
        PerfectShots,

        /// <summary>Flèches qui ont tué au moins « Multi Kill Size » ennemis à elles seules.</summary>
        MultiKill,

        /// <summary>Boss de vague tués.</summary>
        BossesKilled,

        /// <summary>Meilleure vague terminée en une seule partie (ce n'est pas un cumul).</summary>
        WaveReached,

        /// <summary>Épreuves des étoiles réussies (le portail des monolithes).</summary>
        StarTrial,
    }

    /// <summary>
    /// Un succès (achèvement) : un objectif à remplir une fois, gardé d'une partie à l'autre.
    /// Un asset par succès ; l'<see cref="AchievementManager"/> suit leur progression et la sauvegarde.
    /// </summary>
    [CreateAssetMenu(fileName = "Succes", menuName = "Archery/Achievement")]
    public class AchievementDefinition : ScriptableObject
    {
        [Tooltip("Identifiant de sauvegarde. Vide : le nom de l'asset. À ne plus changer une fois le jeu lancé, " +
                 "sinon la progression déjà faite est perdue.")]
        [SerializeField]
        string m_Id;

        [Tooltip("Nom du succès, affiché dans le menu des arcs et quand il est débloqué.")]
        public string title = "Chasseur";

        [Tooltip("Ce qu'il faut faire, en une phrase.")]
        [TextArea]
        public string description = "Tuer 100 ennemis.";

        public AchievementType type = AchievementType.EnemiesKilled;

        [Tooltip("Nombre à atteindre (100 ennemis, 10 tirs parfaits à la tête, vague 5…).")]
        [Min(1)]
        public int target = 100;

        [Tooltip("Seulement pour Multi Kill : nombre d'ennemis qu'une même flèche doit tuer.")]
        [Min(2)]
        public int multiKillSize = 2;

        [Tooltip("Couleur du message quand le succès est débloqué.")]
        public Color color = new Color(1f, 0.82f, 0.35f);

        [Tooltip("Secret : sur la carte de l'arc verrouillé, seule la phrase s'affiche (une énigme), sans le nom du succès " +
                 "ni sa progression. Son nom apparaît quand il est débloqué.")]
        public bool secret;

        public string Id => string.IsNullOrEmpty(m_Id) ? name : m_Id;
    }
}
