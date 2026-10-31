using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Une pièce de niveau qui suit une autre partie du bâtiment (deuxième brainstorm du tower defense, 29 octobre : le deuxième
    /// arc et le pieu géant de la baliste, le tube géant du mortier suivent leur tourelle, qui pivote). Elle reste sous sa pièce
    /// de niveau, montrée ou cachée avec elle, et garde sa propre échelle (ses formes sont en mètres) : seules sa place et son
    /// orientation suivent celles de la cible, à chaque image.
    /// </summary>
    /// <remarks>
    /// Depuis le 30 octobre (l'allure des tours) : une charpente ou une plate-forme ne prend que le cap de la cible (elle ne bascule
    /// plus quand la tourelle vise en haut ou en bas), et une pièce peut être décalée de la cible (le deuxième arc, posé plus haut).
    /// </remarks>
    public class FollowPart : MonoBehaviour
    {
        [Tooltip("La partie à suivre (la tourelle).")]
        [SerializeField]
        Transform m_Target;

        [Tooltip("Seulement le cap de la cible (sa rotation autour de la verticale) : une charpente, une plate-forme.")]
        [SerializeField]
        bool m_YawOnly;

        [Tooltip("Décalage (m) depuis la cible, dans son repère tourné de son seul cap : une deuxième arme posée plus haut.")]
        [SerializeField]
        Vector3 m_Offset;

        void LateUpdate()
        {
            if (m_Target == null)
                return;

            var yaw = Quaternion.Euler(0f, m_Target.eulerAngles.y, 0f);
            transform.SetPositionAndRotation(m_Target.position + yaw * m_Offset, m_YawOnly ? yaw : m_Target.rotation);
        }
    }
}
