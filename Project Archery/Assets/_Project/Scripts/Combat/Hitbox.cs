using Archery.Bows;
using Archery.Core;
using UnityEngine;

namespace Archery.Combat
{
    /// <summary>
    /// Zone de touche (tête, corps…) d'un objet qui a des <see cref="Health"/>.
    /// À placer sur chaque collider concerné.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour, IArrowHitHandler
    {
        [SerializeField]
        HitZone m_Zone = HitZone.Body;

        [Tooltip("Multiplie les dégâts reçus dans cette zone (tête ×2 par défaut).")]
        [SerializeField]
        float m_DamageMultiplier = 1f;

        [Tooltip("Laisser vide pour prendre le Health d'un parent.")]
        [SerializeField]
        Health m_Owner;

        [Tooltip("Son joué quand cette zone est touchée (ex. « ding » du headshot).")]
        [SerializeField]
        AudioClip m_HitClip;

        public HitZone Zone => m_Zone;
        public Health Owner => m_Owner;

        public float DamageMultiplier
        {
            get => m_DamageMultiplier;
            set => m_DamageMultiplier = value;
        }

        void Awake()
        {
            if (m_Owner == null)
                m_Owner = GetComponentInParent<Health>();
        }

        public bool OnArrowHit(in ArrowHit hit)
        {
            if (m_Owner == null || !m_Owner.IsAlive)
                return false;

            if (hit.IsShot)
            {
                m_Owner.TakeDamage(new DamageInfo
                {
                    Amount = hit.Damage * m_DamageMultiplier,
                    Zone = m_Zone,
                    Grade = hit.Grade,
                    Distance = hit.TravelDistance,
                    Point = hit.Point,
                    Direction = hit.Direction,
                    Source = hit.Arrow,
                });
                Sfx.Play(m_HitClip, hit.Point, 0.9f);
            }

            return true;
        }
    }
}
