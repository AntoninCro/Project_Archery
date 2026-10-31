using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Effects;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Carreau tiré par une <see cref="Ballista"/> : il vole en ligne droite, blesse les ennemis qu'il touche
    /// (la zone touchée compte : tête, point faible), en traverse quelques-uns selon le niveau, et se plante dans le décor.
    /// </summary>
    /// <remarks>À placer à la racine d'un prefab dont le modèle pointe vers l'avant (axe Z).</remarks>
    public class BallistaBolt : MonoBehaviour
    {
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];
        static readonly Comparison<RaycastHit> s_ByDistance = (a, b) => a.distance.CompareTo(b.distance);

        [Tooltip("Durée de vie maximale (s) d'un carreau qui ne touche rien.")]
        [SerializeField]
        float m_Lifetime = 2f;

        [Tooltip("Temps (s) pendant lequel un carreau planté dans le décor reste visible.")]
        [SerializeField]
        float m_StuckTime = 3f;

        [Tooltip("Optionnel : matériau de la traînée (comme celle des flèches).")]
        [SerializeField]
        Material m_TrailMaterial;

        [SerializeField]
        Color m_TrailColor = new Color(1f, 0.85f, 0.5f, 0.6f);

        readonly HashSet<Health> m_Hit = new HashSet<Health>();
        readonly List<RaycastHit> m_Sorted = new List<RaycastHit>();
        Vector3 m_Velocity;
        float m_Damage;
        int m_PierceLeft;
        UnityEngine.Object m_Source;
        Action<Enemies.Enemy, Vector3> m_OnHit;
        float m_Age;
        float m_Travel;
        bool m_Stuck;

        /// <summary>Lance le carreau. <paramref name="pierce"/> : ennemis qu'il traverse avant de s'arrêter.</summary>
        public void Launch(Vector3 velocity, float damage, int pierce, UnityEngine.Object source) => Launch(velocity, damage, pierce, source, null);

        /// <summary>
        /// Comme <see cref="Launch(Vector3, float, int, UnityEngine.Object)"/> ; <paramref name="onHit"/> est appelé pour chaque
        /// ennemi touché, avec la direction du carreau (le harpon et la baliste titan, 29 octobre).
        /// </summary>
        public void Launch(Vector3 velocity, float damage, int pierce, UnityEngine.Object source, Action<Enemies.Enemy, Vector3> onHit)
        {
            m_OnHit = onHit;
            m_Velocity = velocity;
            m_Damage = damage;
            m_PierceLeft = Mathf.Max(0, pierce);
            m_Source = source != null ? source : this;
            if (velocity.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(velocity);

            if (m_TrailMaterial != null)
            {
                var trail = gameObject.AddComponent<TrailRenderer>();
                trail.sharedMaterial = m_TrailMaterial;
                trail.time = 0.12f;
                trail.startWidth = 0.06f;
                trail.endWidth = 0f;
                trail.startColor = m_TrailColor;
                var end = m_TrailColor;
                end.a = 0f;
                trail.endColor = end;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_Age += deltaTime;
            if (m_Stuck)
            {
                if (m_Age >= m_StuckTime)
                    Destroy(gameObject);
                return;
            }

            if (m_Age >= m_Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            var step = m_Velocity * deltaTime;
            var length = step.magnitude;
            if (length < 1e-5f)
                return;

            var start = transform.position;
            var direction = step / length;
            var count = Physics.RaycastNonAlloc(start, direction, s_Hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            m_Sorted.Clear();
            for (var i = 0; i < count; i++)
                m_Sorted.Add(s_Hits[i]);
            m_Sorted.Sort(s_ByDistance);

            foreach (var hit in m_Sorted)
            {
                if (hit.collider == null || ArrowIgnore.IsIgnored(hit.collider))
                    continue;

                var hitbox = hit.collider.GetComponentInParent<Hitbox>();
                if (hitbox != null)
                {
                    if (!TryDamage(hitbox, hit, direction))
                        continue;
                    if (m_PierceLeft-- > 0)
                        continue;

                    Destroy(gameObject);
                    return;
                }

                // Un corps sans zone de touche : il ne compte pas. Le décor arrête le carreau.
                if (hit.collider.GetComponentInParent<Health>() != null)
                    continue;

                if (HitEffects.Instance != null)
                    HitEffects.Instance.PlayImpact(hit.collider, hit.point, hit.normal);
                transform.position = hit.point - direction * 0.3f;
                m_Stuck = true;
                m_Age = 0f;
                return;
            }

            transform.position = start + step;
            m_Travel += length;
        }

        // Une fois par ennemi : la zone la plus touchée en premier (tête, corps…) donne le multiplicateur.
        bool TryDamage(Hitbox hitbox, RaycastHit hit, Vector3 direction)
        {
            var owner = hitbox.Owner;
            if (owner == null || !owner.IsAlive || !m_Hit.Add(owner))
                return false;

            owner.TakeDamage(new DamageInfo
            {
                Amount = m_Damage * hitbox.DamageMultiplier,
                Zone = hitbox.Zone,
                Grade = ShotGrade.None,
                Distance = m_Travel + hit.distance,
                Point = hit.point,
                Direction = direction,
                Source = m_Source,
            });

            if (HitEffects.Instance != null)
                HitEffects.Instance.PlayImpact(hit.collider, hit.point, -direction);

            var enemy = m_OnHit != null ? owner.GetComponentInParent<Enemies.Enemy>() : null;
            if (enemy != null)
                m_OnHit(enemy, direction);
            return true;
        }
    }
}
