using System.Collections.Generic;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Un pilier posé à la main dans l'arène du boss final (pour remplacer ou compléter ceux qui sont générés) :
    /// le dragon le contourne et ne pose pas de rocher ni de flaque de lave dessus. Il faut aussi lui donner un collider
    /// pour qu'il arrête les flèches, le souffle et les boules de feu.
    /// </summary>
    [DisallowMultipleComponent]
    public class ArenaPillar : MonoBehaviour
    {
        static readonly List<ArenaPillar> s_All = new List<ArenaPillar>();

        [Tooltip("Rayon (m) du pilier au sol, à l'échelle 1.")]
        [SerializeField]
        float m_Radius = 1.9f;

        [Tooltip("Hauteur (m) du pilier, à l'échelle 1.")]
        [SerializeField]
        float m_Height = 8f;

        /// <summary>Les piliers posés à la main, actifs dans les scènes chargées.</summary>
        public static IReadOnlyList<ArenaPillar> All => s_All;

        public float Radius
        {
            get => m_Radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            set => m_Radius = Mathf.Max(0.1f, value);
        }

        public float Height
        {
            get => m_Height * transform.lossyScale.y;
            set => m_Height = Mathf.Max(0.1f, value);
        }

        void OnEnable() => s_All.Add(this);

        void OnDisable() => s_All.Remove(this);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
            var center = transform.position;
            const int sides = 24;
            for (var i = 0; i < sides; i++)
            {
                var a0 = i * Mathf.PI * 2f / sides;
                var a1 = (i + 1) * Mathf.PI * 2f / sides;
                Gizmos.DrawLine(center + new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * Radius,
                                center + new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)) * Radius);
            }
        }
    }
}
