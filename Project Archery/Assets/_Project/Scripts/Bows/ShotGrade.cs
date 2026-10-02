using System;
using UnityEngine;

namespace Archery.Bows
{
    /// <summary>
    /// Qualité d'un tir, donnée par la bande de l'anneau de timing au moment du lâcher.
    /// <see cref="None"/> : la corde a été lâchée avant la tension maximale (pas d'anneau).
    /// </summary>
    public enum ShotGrade
    {
        None,
        Miss,
        Ok,
        Good,
        Perfect,
    }

    /// <summary>
    /// Multiplicateurs appliqués à un tir selon sa qualité.
    /// </summary>
    [Serializable]
    public struct GradeModifiers
    {
        public string label;
        public Color color;
        [Tooltip("Multiplie la vitesse de la flèche.")]
        public float speed;
        [Tooltip("Multiplie les dégâts.")]
        public float damage;
        [Tooltip("Multiplie les points gagnés.")]
        public float points;

        public GradeModifiers(string label, Color color, float speed, float damage, float points)
        {
            this.label = label;
            this.color = color;
            this.speed = speed;
            this.damage = damage;
            this.points = points;
        }
    }
}
