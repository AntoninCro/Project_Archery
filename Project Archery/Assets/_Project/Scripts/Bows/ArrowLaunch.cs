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
    }

    /// <summary>
    /// Résumé d'un tir, envoyé par <see cref="Bow.ShotFired"/> (score, statistiques, interface…).
    /// </summary>
    public struct ShotInfo
    {
        public ShotGrade Grade;
        public float DrawRatio;
        public float Speed;
        public float Damage;
        public Vector3 Origin;
        public Vector3 Direction;
    }
}
