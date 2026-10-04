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
    public class Hitbox : MonoBehaviour, IArrowHitHandler, IArrowHitPriority
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

        // Taille d'origine du collider, pour SetSizeMultiplier : rayon et hauteur, ou taille de la boîte.
        Vector3 m_BaseSize;
        bool m_HasBaseSize;

        /// <summary>Bonus de dégâts à la tête, pour toutes les zones « Head » (amélioration Chasseur de têtes).</summary>
        public static float HeadDamageMultiplier { get; set; } = 1f;

        public HitZone Zone => m_Zone;
        public Health Owner => m_Owner;

        // Un point faible ou la tête passent avant le corps quand la flèche traverse les deux.
        public int HitPriority => m_Zone switch
        {
            HitZone.WeakPoint => 3,
            HitZone.Head => 2,
            _ => 1,
        };

        public Object HitGroup => m_Owner;

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

        /// <summary>
        /// Agrandit ou réduit la zone par rapport à sa taille d'origine (difficulté : taille de la tête).
        /// Fonctionne avec un Sphere, Capsule ou Box Collider.
        /// </summary>
        public void SetSizeMultiplier(float multiplier)
        {
            var zoneCollider = GetComponent<Collider>();
            if (!m_HasBaseSize)
            {
                m_BaseSize = zoneCollider switch
                {
                    SphereCollider sphere => new Vector3(sphere.radius, 0f, 0f),
                    CapsuleCollider capsule => new Vector3(capsule.radius, capsule.height, 0f),
                    BoxCollider box => box.size,
                    _ => Vector3.zero,
                };
                m_HasBaseSize = true;
            }

            switch (zoneCollider)
            {
                case SphereCollider sphere:
                    sphere.radius = m_BaseSize.x * multiplier;
                    break;
                case CapsuleCollider capsule:
                    capsule.radius = m_BaseSize.x * multiplier;
                    capsule.height = m_BaseSize.y * multiplier;
                    break;
                case BoxCollider box:
                    box.size = m_BaseSize * multiplier;
                    break;
                default:
                    Debug.LogWarning($"Hitbox : impossible de changer la taille de « {name} » (collider non géré).", this);
                    break;
            }
        }

        public bool OnArrowHit(in ArrowHit hit)
        {
            if (m_Owner == null || !m_Owner.IsAlive)
                return false;

            if (hit.IsShot)
            {
                var zoneMultiplier = m_DamageMultiplier * (m_Zone == HitZone.Head ? HeadDamageMultiplier : 1f);
                m_Owner.TakeDamage(new DamageInfo
                {
                    Amount = hit.Damage * zoneMultiplier,
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => HeadDamageMultiplier = 1f;
    }
}
