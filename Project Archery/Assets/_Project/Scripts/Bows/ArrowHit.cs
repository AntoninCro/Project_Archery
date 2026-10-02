using UnityEngine;

namespace Archery.Bows
{
    /// <summary>
    /// Tout ce qu'il faut savoir sur l'impact d'une flèche.
    /// </summary>
    public struct ArrowHit
    {
        public Arrow Arrow;
        public Collider Collider;
        public Vector3 Point;
        public Vector3 Normal;
        public Vector3 Direction;
        public float Speed;
        public float Damage;
        public ShotGrade Grade;

        /// <summary>Distance parcourue depuis le tir (m), pour le bonus de distance.</summary>
        public float TravelDistance;

        /// <summary>Faux pour une flèche lâchée ou lancée à la main : elle ne rapporte rien.</summary>
        public bool IsShot;
    }

    /// <summary>
    /// Implémenté par ce qui réagit aux flèches : zones de touche des ennemis, cibles d'entraînement…
    /// </summary>
    public interface IArrowHitHandler
    {
        /// <summary>
        /// Appelé quand une flèche touche ce collider (ou un de ses enfants).
        /// </summary>
        /// <returns>Vrai si la flèche a touché quelque chose de vivant, qu'elle peut donc traverser si elle a du perçage.</returns>
        bool OnArrowHit(in ArrowHit hit);
    }
}
