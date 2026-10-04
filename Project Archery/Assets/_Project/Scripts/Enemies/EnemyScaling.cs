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

        public static void Reset()
        {
            Health = 1f;
            Damage = 1f;
            Speed = 1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Reset();
    }
}
