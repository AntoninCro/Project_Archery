using System.Collections.Generic;
using Archery.Combat;
using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// La zone du corps qui suit les sauts (12 novembre, le soir ; le joueur : « les champignons qui sautillent sont intenables
    /// puisque leurs hitbox ne suivent pas leur saut »). La zone du corps est posée sur la racine de l'ennemi, au sol, alors que
    /// l'animation fait bondir le modèle (ses os) : seule la tête, accrochée à un os, suivait, et les flèches passaient au travers
    /// du corps en l'air. Coché (<i>Body Follows Hops</i>), la zone du corps monte d'autant que la tête au-dessus de sa hauteur au
    /// repos (la pose du modèle à l'apparition, avant que l'animation ne le bouge) ; jamais plus bas que sa place.
    /// </summary>
    public partial class Enemy
    {
        [Tooltip("La zone du corps (posée sur la racine, au sol) monte quand l'animation fait sauter le modèle : elle suit la tête " +
                 "au-dessus de sa hauteur au repos (12 novembre : le champignon et le gluant, qui sautillent).")]
        [SerializeField]
        bool m_BodyFollowsHops;

        // Le plus haut saut suivi (m) : au-delà, la tête est partie ailleurs (une animation de mort, un modèle qui s'envole).
        const float k_MaxHop = 2.5f;

        Transform m_HopHead;
        float m_HopRestHeight;
        readonly List<Transform> m_HopBodies = new List<Transform>();
        readonly List<Vector3> m_HopBodyRest = new List<Vector3>();

        // La tête (une zone « Head » portée par un os) et les zones du corps posées sur la racine, à l'apparition.
        void SetupHopFollow()
        {
            if (!m_BodyFollowsHops)
                return;

            foreach (var hitbox in GetComponentsInChildren<Hitbox>(true))
            {
                var zone = hitbox.transform;
                if (hitbox.Zone == HitZone.Head && zone.parent != transform && m_HopHead == null)
                {
                    m_HopHead = zone;
                }
                else if (hitbox.Zone == HitZone.Body && zone.parent == transform)
                {
                    m_HopBodies.Add(zone);
                    m_HopBodyRest.Add(zone.localPosition);
                }
            }

            if (m_HopHead == null || m_HopBodies.Count == 0)
            {
                m_HopHead = null;
                Debug.LogWarning($"Enemy : « {name} » suit ses sauts (Body Follows Hops), mais il lui faut une zone « Head » sur un os " +
                                 "et une zone « Body » posée sur sa racine.", this);
                return;
            }

            m_HopRestHeight = transform.InverseTransformPoint(m_HopHead.position).y;
        }

        // Après l'animation (les os sont posés) : la zone du corps monte avec le saut.
        void LateUpdate()
        {
            if (m_HopHead == null)
                return;

            var rise = Mathf.Clamp(transform.InverseTransformPoint(m_HopHead.position).y - m_HopRestHeight, 0f, k_MaxHop);
            for (var i = 0; i < m_HopBodies.Count; i++)
            {
                if (m_HopBodies[i] != null)
                    m_HopBodies[i].localPosition = m_HopBodyRest[i] + Vector3.up * rise;
            }
        }
    }
}
