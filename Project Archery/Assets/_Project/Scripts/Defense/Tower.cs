using System;
using Archery.Combat;
using Archery.Core;
using UnityEngine;

namespace Archery.Defense
{
    /// <summary>
    /// La tour à défendre (GDD, section 9) : la cible principale des ennemis.
    /// À placer sur l'objet de la tour, avec un <see cref="Health"/> et au moins un collider.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class Tower : MonoBehaviour
    {
        [Tooltip("Optionnel : objet affiché tant que la tour est debout.")]
        [SerializeField]
        GameObject m_IntactVisual;

        [Tooltip("Optionnel : ruines affichées quand la tour est détruite.")]
        [SerializeField]
        GameObject m_RuinedVisual;

        [SerializeField]
        AudioClip m_HitClip;

        [SerializeField]
        AudioClip m_DestroyedClip;

        Health m_Health;
        Collider[] m_Colliders = Array.Empty<Collider>();

        public static Tower Instance { get; private set; }

        public Health Health => m_Health;
        public bool IsStanding => m_Health != null && m_Health.IsAlive;

        /// <summary>La tour vient d'être détruite (malus, effondrement…).</summary>
        public event Action Destroyed;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs tours dans la scène, seule la dernière est visée par les ennemis.", this);

            Instance = this;
            m_Health = GetComponent<Health>();
            m_Colliders = GetComponentsInChildren<Collider>();
            if (m_Colliders.Length == 0)
                Debug.LogWarning("Tower : aucun collider. Les ennemis viseront le centre de la tour.", this);

            if (m_RuinedVisual != null)
                m_RuinedVisual.SetActive(false);
        }

        void OnEnable()
        {
            m_Health.Damaged += OnDamaged;
            m_Health.Died += OnDied;
        }

        void OnDisable()
        {
            m_Health.Damaged -= OnDamaged;
            m_Health.Died -= OnDied;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Point de la tour le plus proche, là où un ennemi vient frapper.</summary>
        public Vector3 ClosestPoint(Vector3 from)
        {
            var best = transform.position;
            var bestSqrDistance = float.MaxValue;
            foreach (var collider in m_Colliders)
            {
                if (collider == null || !collider.enabled)
                    continue;

                // ClosestPoint ne marche que sur les colliders simples (boîte, sphère, capsule, mesh convexe).
                var point = collider is MeshCollider { convex: false } ? collider.bounds.ClosestPoint(from) : collider.ClosestPoint(from);
                var sqrDistance = (point - from).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = point;
                }
            }

            return best;
        }

        void OnDamaged(Health health, DamageInfo info) => Sfx.Play(m_HitClip, info.Point, 0.8f);

        void OnDied(Health health, DamageInfo info)
        {
            if (m_IntactVisual != null)
                m_IntactVisual.SetActive(false);
            if (m_RuinedVisual != null)
                m_RuinedVisual.SetActive(true);

            Sfx.Play(m_DestroyedClip, transform.position);
            Destroyed?.Invoke();
        }
    }
}
