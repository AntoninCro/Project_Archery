using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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

        /// <summary>Gravité de la flèche, en multiple de la normale (arc légendaire : elle tombe plus vite). 0 : normale.</summary>
        public float GravityScale;
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

        /// <summary>
        /// L'arme qui a tiré (26 octobre, pièces de la flèche) : son avant est la direction où elle vise, son haut l'axe de ses
        /// branches pour un arc, le dessus de l'arbalète. Vide pour les failles du katana.
        /// </summary>
        public Transform Aim;

        /// <summary>La main qui tient l'arme (sa gâchette guide la flèche de l'empennage télékinésie). Vide sans casque.</summary>
        public IXRSelectInteractor Hand;
    }
}
