using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Une pièce d'un modèle fait main qui tourne sur elle-même (14 novembre, la refonte visuelle : les modèles du joueur sont
    /// immobiles, le code les anime ; <c>Docs/Refonte_Visuelle.md</c>). Posée par <c>Editor/ModelesFaitMain</c> d'après
    /// <c>Art/Modeles/Mouvements/&lt;Nom&gt;.json</c>, sur un objet vide au centre des pièces qui tournent ensemble (le grand
    /// cristal du niveau 5 du Givre, ses deux cônes). Elle suit le temps du jeu (les ralentis, la pause).
    /// </summary>
    public class ModelSpin : MonoBehaviour
    {
        [SerializeField, Tooltip("Degrés par seconde (négatif : dans l'autre sens).")]
        float m_DegreesPerSecond = 25f;

        [SerializeField, Tooltip("L'axe de rotation, dans le repère de l'objet (Y : vertical).")]
        Vector3 m_Axis = Vector3.up;

        public float DegreesPerSecond
        {
            get => m_DegreesPerSecond;
            set => m_DegreesPerSecond = value;
        }

        public Vector3 Axis
        {
            get => m_Axis;
            set => m_Axis = value;
        }

        void Update()
        {
            transform.Rotate(m_Axis, m_DegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
