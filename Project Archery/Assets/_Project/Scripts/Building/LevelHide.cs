using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Une pièce d'un modèle fait main qui disparaît quand le bâtiment atteint un niveau (15 novembre, la refonte visuelle : le
    /// joueur veut « retirer une pièce de la tour quand elle monte de niveau », la base du Mortier d'une couleur par niveau, des
    /// copies dont chacune laisse la place à la suivante). Par défaut une pièce reste jusqu'au dernier niveau ; celle-ci est
    /// visible jusqu'au niveau d'avant <see cref="HideFromLevel"/>. Posée par <c>Editor/ModelesFaitMain</c> d'après le .json de
    /// l'export de Blender (le bouton « La sélection disparaît au niveau » de l'onglet THUNK) ; c'est <see cref="Structure"/> qui
    /// l'active ou la désactive, avec les pièces des niveaux.
    /// </summary>
    public class LevelHide : MonoBehaviour
    {
        [SerializeField, Range(2, 5), Tooltip("Le niveau du bâtiment à partir duquel la pièce disparaît.")]
        int m_HideFromLevel = 2;

        public int HideFromLevel
        {
            get => m_HideFromLevel;
            set => m_HideFromLevel = value;
        }

        /// <summary>Visible à ce niveau (le niveau 0 : le plan bleu, la pièce y est).</summary>
        public bool IsShownAt(int level) => level < m_HideFromLevel;
    }
}
