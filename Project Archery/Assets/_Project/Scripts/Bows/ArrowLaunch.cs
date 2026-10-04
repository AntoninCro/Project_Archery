using UnityEngine;

namespace Archery.Bows
{
    /// <summary>
    /// Paramètres de départ d'une flèche.
    /// </summary>
    public struct ArrowLaunch
    {
        public Vector3 Velocity;
        public float Damage;
        public ShotGrade Grade;
        public int Pierce;
        public Color TrailColor;
        public bool IsShot;

        /// <summary>Flèche ajoutée par le multitir ou le déluge : si elle rate, le combo n'est pas cassé.</summary>
        public bool IsExtra;
    }

    /// <summary>
    /// Résumé d'un tir, envoyé par <see cref="Bow.ShotFired"/> (score, statistiques, interface…).
    /// </summary>
    public struct ShotInfo
    {
        /// <summary>La flèche tirée, déjà lancée.</summary>
        public Arrow Arrow;

        public ShotGrade Grade;
        public float DrawRatio;
        public float Speed;
        public float Damage;
        public Vector3 Origin;
        public Vector3 Direction;
    }
}
