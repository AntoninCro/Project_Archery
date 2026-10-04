using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Caractéristiques d'un type d'ennemi (GDD, section 10). Un asset par type.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "Archery/Enemy Definition")]
    public class EnemyDefinition : ScriptableObject
    {
        public string displayName = "Rampant";

        [Header("Vie et déplacement")]
        public float maxHealth = 30f;

        [Tooltip("Vitesse de marche (m/s).")]
        public float moveSpeed = 2.5f;

        [Header("Attaque")]
        public float attackDamage = 10f;

        [Tooltip("Distance (m) à partir de laquelle l'ennemi peut frapper sa cible (ou tirer, pour un tireur).")]
        public float attackRange = 1.2f;

        [Tooltip("Temps (s) entre deux attaques (entre deux piqués, pour un volant).")]
        public float attackInterval = 1.5f;

        [Tooltip("Délai (s) entre le début de l'attaque et le moment où le coup porte (le vol sur place avant un piqué).")]
        public float attackWindup = 0.4f;

        [Tooltip("Si le joueur est au sol à moins de cette distance (m), l'ennemi l'attaque à la place de la tour. 0 = jamais.")]
        public float playerAggroRange = 8f;

        [Tooltip("L'ennemi vise toujours le joueur, où qu'il soit, et jamais la tour (volant, tireur).")]
        public bool targetsPlayer;

        [Header("Score")]
        [Tooltip("Points de base gagnés en tuant cet ennemi.")]
        public int points = 10;
    }
}
