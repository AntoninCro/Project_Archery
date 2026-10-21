using UnityEngine;

namespace Archery.Player
{
    /// <summary>
    /// Sur un objet qu'on tient en main (une arbalète des Akimbow) : où le poing ganté se ferme, et de combien la montre du
    /// poignet monte pour rester visible au-dessus de l'objet.
    /// </summary>
    /// <remarks>
    /// <see cref="GlovedHand"/> place la main sur <see cref="Point"/>, comme sur la poignée de l'arc : le centre du poing sur
    /// sa position, l'axe du poing (de l'auriculaire vers l'index) sur son axe Y, la paume vers son côté (-X pour la main
    /// droite, +X pour la gauche). <see cref="Archery.UI.WristMount"/> lève la montre de <see cref="WatchLift"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    public class HandGrip : MonoBehaviour
    {
        [Tooltip("Centre de la poignée. Son axe Y va du bas de la poignée vers le haut, son axe X vers la droite de l'objet.")]
        public Transform Point;

        [Tooltip("Hauteur (m) dont la montre monte, quand cette main tient l'objet, pour passer au-dessus de lui.")]
        public float WatchLift;
    }
}
