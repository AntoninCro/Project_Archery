using UnityEngine;

namespace Archery.UI
{
    /// <summary>Marque un menu (ou un prefab) auquel le thème a été appliqué, et avec quelle version du thème.</summary>
    [DisallowMultipleComponent]
    public class UiThemed : MonoBehaviour
    {
        [Tooltip("Version du thème appliquée : l'éditeur la réapplique toute seule quand elle change.")]
        public int version;
    }
}
