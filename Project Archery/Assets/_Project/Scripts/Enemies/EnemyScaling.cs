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
        /// seules tourelles de la vitrine (F4) ; une baliste gardait le même effet toute la partie.
        /// </summary>
        public const float k_DefaultStructureExponent = 0.5f;

        public static void Reset()
        {
            Health = 1f;
            Damage = 1f;
            Speed = 1f;
            StructureExponent = k_DefaultStructureExponent;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();
    }
}
