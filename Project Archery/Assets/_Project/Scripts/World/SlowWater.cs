using Archery.Enemies;
using UnityEngine;

namespace Archery.World
{
    /// <summary>
    /// Eau peu profonde (gué d'une rivière) : les ennemis au sol qui la traversent sont ralentis, comme sur la glace.
    /// La zone est une boîte dans les axes de cet objet (dessinée en bleu quand il est sélectionné) ; elle n'a pas de collider.
    /// Les volants, bien plus haut, ne sont pas concernés.
    /// </summary>
    [AddComponentMenu("Archery/Slow Water")]
    public class SlowWater : MonoBehaviour
    {
        const float k_TickInterval = 0.2f;

        [Tooltip("Taille de la zone (m) dans les axes de cet objet : largeur (X), hauteur (Y), longueur (Z). " +
                 "Elle est centrée sur l'objet : place-le à la surface de l'eau.")]
        [SerializeField]
        Vector3 m_Size = new Vector3(8f, 3f, 6f);

        [Tooltip("Vitesse des ennemis dans l'eau (0,6 = 40 % plus lents).")]
        [Range(0.1f, 1f)]
        [SerializeField]
        float m_SpeedMultiplier = 0.6f;

        float m_TickTimer;

        void Update()
        {
            m_TickTimer -= Time.deltaTime;
            if (m_TickTimer > 0f)
                return;

            // Le ralentissement dure un peu plus qu'un intervalle : il continue tant que l'ennemi reste dans l'eau.
            m_TickTimer = k_TickInterval;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy != null && enemy.IsAlive && Contains(enemy.transform.position))
                    enemy.Slow(m_SpeedMultiplier, k_TickInterval * 1.5f);
            }
        }

        bool Contains(Vector3 position)
        {
            var local = Quaternion.Inverse(transform.rotation) * (position - transform.position);
            return Mathf.Abs(local.x) <= m_Size.x * 0.5f && Mathf.Abs(local.y) <= m_Size.y * 0.5f &&
                   Mathf.Abs(local.z) <= m_Size.z * 0.5f;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.75f, 1f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, m_Size);
        }
    }
}
