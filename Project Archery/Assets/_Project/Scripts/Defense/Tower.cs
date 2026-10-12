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
        [Tooltip("Optionnel : objet affiché tant que la tour est debout. Vide : c'est le modèle posé sur cet objet " +
                 "(ses rendus et ses colliders) qui disparaît quand la tour tombe.")]
        [SerializeField]
        GameObject m_IntactVisual;

        [Tooltip("Optionnel : ruines affichées quand la tour est détruite (objet à part, caché au départ).")]
        [SerializeField]
        GameObject m_RuinedVisual;

        [Tooltip("Optionnel : effet de particules joué à l'effondrement (la fumée de mort des ennemis, en plus grand).")]
        [SerializeField]
        GameObject m_DestroyedEffect;

        [Tooltip("Taille de l'effet d'effondrement.")]
        [SerializeField]
        float m_DestroyedEffectScale = 5f;

        [SerializeField]
        AudioClip m_HitClip;

        [SerializeField]
        AudioClip m_DestroyedClip;

        [Tooltip("Joué quand la tour est réparée ou reconstruite (boutique).")]
        [SerializeField]
        AudioClip m_RepairClip;

        Health m_Health;
        Collider[] m_Colliders = Array.Empty<Collider>();
        Renderer[] m_OwnRenderers = Array.Empty<Renderer>();
        Collider[] m_OwnColliders = Array.Empty<Collider>();
        float? m_TopHeight;

        public static Tower Instance { get; private set; }

        public Health Health => m_Health;
        public bool IsStanding => m_Health != null && m_Health.IsAlive;

        /// <summary>Hauteur (m) du sommet de la tour : le haut de ses colliders.</summary>
        public float TopHeight
        {
            get
            {
                if (m_TopHeight == null)
                {
                    var top = transform.position.y;
                    foreach (var collider in m_Colliders)
                    {
                        if (collider != null)
                            top = Mathf.Max(top, collider.bounds.max.y);
                    }

                    m_TopHeight = top;
                }

                return m_TopHeight.Value;
            }
        }

        /// <summary>La tour vient d'être détruite (malus, effondrement…).</summary>
        public event Action Destroyed;

        /// <summary>La tour vient d'être reconstruite (boutique).</summary>
        public event Action Rebuilt;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs tours dans la scène, seule la dernière est visée par les ennemis.", this);

            Instance = this;
            m_Health = GetComponent<Health>();
            m_Colliders = GetComponentsInChildren<Collider>();
            if (m_Colliders.Length == 0)
                Debug.LogWarning("Tower : aucun collider. Les ennemis viseront le centre de la tour.", this);

            // Sans objet « intact » à part, la tour est le modèle lui-même : on cachera ses rendus et ses colliders.
            if (m_IntactVisual == null)
            {
                m_OwnRenderers = GetComponents<Renderer>();
                m_OwnColliders = GetComponents<Collider>();
            }

            // Le sommet se mesure tant que les colliders de la tour debout sont actifs.
            _ = TopHeight;

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

        /// <summary>Rend des PV à la tour debout (réparation en boutique). Les PV ne remontent jamais tout seuls.</summary>
        public void Repair(float amount)
        {
            if (!IsStanding || amount <= 0f)
                return;

            m_Health.Heal(amount);
            Sfx.Play(m_RepairClip, transform.position + Vector3.up * 2f);
        }

        /// <summary>Relève la tour détruite avec tous ses PV (boutique).</summary>
        public void Rebuild()
        {
            if (IsStanding)
                return;

            m_Health.ResetHealth(m_Health.Max);
            SetStanding(true);

            Sfx.Play(m_RepairClip, transform.position + Vector3.up * 2f);
            Rebuilt?.Invoke();
        }

        void OnDamaged(Health health, DamageInfo info) => Sfx.Play(m_HitClip, info.Point, 0.8f);

        void OnDied(Health health, DamageInfo info)
        {
            SetStanding(false);
            PlayDestroyedEffect();
            Sfx.Play(m_DestroyedClip, transform.position + Vector3.up * 2f, 1f, 1f, 0.3f);
            Destroyed?.Invoke();
        }

        // Tour debout : le modèle intact ; détruite : les ruines (et plus de collider de la tour entière).
        void SetStanding(bool standing)
        {
            if (m_IntactVisual != null)
                m_IntactVisual.SetActive(standing);
            foreach (var renderer in m_OwnRenderers)
            {
                if (renderer != null)
                    renderer.enabled = standing;
            }

            foreach (var collider in m_OwnColliders)
            {
                if (collider != null)
                    collider.enabled = standing;
            }

            if (m_RuinedVisual != null)
                m_RuinedVisual.SetActive(!standing);
        }

        void PlayDestroyedEffect()
        {
            if (m_DestroyedEffect == null)
                return;

            var effect = Instantiate(m_DestroyedEffect, transform.position + Vector3.up * 1.5f, Quaternion.identity);
            effect.transform.localScale = Vector3.one * m_DestroyedEffectScale;
            foreach (var particles in effect.GetComponentsInChildren<ParticleSystem>())
            {
                var main = particles.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                particles.Play();
            }

            Destroy(effect, 6f);
        }
    }
}
