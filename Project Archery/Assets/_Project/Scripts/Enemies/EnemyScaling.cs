using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Renforcement des ennemis selon la vague en cours (courbe de difficulté exponentielle, GDD section 11).
    /// Le <see cref="Waves.WaveManager"/> le règle au début de chaque vague ; chaque ennemi le lit à son apparition.
    /// </summary>
    public static class EnemyScaling
    {
        /// <summary>Multiplie les PV des ennemis qui apparaissent maintenant (1 = vague 1).</summary>
        public static float Health { get; set; } = 1f;

        /// <summary>Multiplie les dégâts des ennemis qui apparaissent maintenant.</summary>
        public static float Damage { get; set; } = 1f;

        /// <summary>Multiplie la vitesse des ennemis qui apparaissent maintenant (plafonnée par les Wave Settings).</summary>
        public static float Speed { get; set; } = 1f;

        /// <summary>
        /// Les dégâts des bâtiments suivent la force des ennemis à cette puissance (Wave Settings, <c>Structure Strength
        /// Exponent</c>) : 1, tout à fait ; 0,5, sa racine carrée.
        /// </summary>
        public static float StructureExponent { get; set; } = k_DefaultStructureExponent;

        /// <summary>
        /// Le 1er novembre au soir : avec 1 (les bâtiments suivaient tout à fait), le joueur est allé jusqu'à la vague 45 avec les
        /// seules tourelles de la vitrine (F4) ; une baliste gardait le même effet toute la partie. 0,5 ce soir-là ; 0 depuis le
        /// 2 novembre (le joueur : « on enlève le scale des tourelles par vagues ») : ils grandissent avec les parchemins.
        /// </summary>
        public const float k_DefaultStructureExponent = 0f;

        /// <summary>
        /// Part des bonus des parchemins du joueur (force, adresse, vigueur) dont profitent les bâtiments, la tour et les
        /// barricades (Wave Settings, <c>Structure Scroll Share</c>) : 1, tout ; 0, rien.
        /// </summary>
        public static float StructureScrollShare { get; set; } = k_DefaultStructureScrollShare;

        /// <summary>
        /// Le 2 novembre (le joueur : « on joue deux roguelikes en un », celui de l'arc et celui des tourelles, sans lien entre
        /// eux ; puis son idée : « les parchemins marchent pour le joueur et les tours »).
        /// </summary>
        public const float k_DefaultStructureScrollShare = 1f;

        public static void Reset()
        {
            Health = 1f;
            Damage = 1f;
            Speed = 1f;
            StructureExponent = k_DefaultStructureExponent;
            StructureScrollShare = k_DefaultStructureScrollShare;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();
    }
}
