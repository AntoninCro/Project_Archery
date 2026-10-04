using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Enemies;
using UnityEngine;

namespace Archery.Effects
{
    /// <summary>
    /// Effets visuels des coups (GDD, section 14) : étincelles quand une flèche touche un ennemi (dorées à la tête),
    /// poussière quand elle se plante dans le décor, fumée à la mort d'un ennemi.
    /// </summary>
    /// <remarks>
    /// Les effets sont des prefabs de <see cref="ParticleSystem"/>, réutilisés au lieu d'être recréés.
    /// Un plafond par image évite d'en lancer des centaines d'un coup avec beaucoup de flèches. Tous sont optionnels.
    /// </remarks>
    [DisallowMultipleComponent]
    public class HitEffects : MonoBehaviour
    {
        [Tooltip("Flèche qui touche un ennemi, au corps.")]
        [SerializeField]
        ParticleSystem m_BodyHitEffect;

        [Tooltip("Flèche qui touche un ennemi à la tête ou à un point faible. Vide : l'effet du corps.")]
        [SerializeField]
        ParticleSystem m_HeadHitEffect;

        [Tooltip("Flèche qui se plante dans le décor (sol, arbre, tour…).")]
        [SerializeField]
        ParticleSystem m_SurfaceHitEffect;

        [Tooltip("Mort d'un ennemi.")]
        [SerializeField]
        ParticleSystem m_DeathEffect;

        [Tooltip("Nombre maximal d'effets lancés par image.")]
        [SerializeField]
        int m_MaxPerFrame = 6;

        readonly Dictionary<ParticleSystem, Stack<ParticleSystem>> m_Free = new Dictionary<ParticleSystem, Stack<ParticleSystem>>();
        readonly List<(ParticleSystem effect, ParticleSystem prefab)> m_Playing = new List<(ParticleSystem, ParticleSystem)>();
        int m_SpawnedThisFrame;
        int m_Frame = -1;

        void OnEnable()
        {
            Arrow.AnyHit += OnArrowHit;
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            Arrow.AnyHit -= OnArrowHit;
            Enemy.Killed -= OnEnemyKilled;
        }

        // Les effets terminés retournent dans la réserve.
        void Update()
        {
            for (var i = m_Playing.Count - 1; i >= 0; i--)
            {
                var (effect, prefab) = m_Playing[i];
                if (effect != null && effect.IsAlive(true))
                    continue;

                m_Playing.RemoveAt(i);
                if (effect == null)
                    continue;

                effect.gameObject.SetActive(false);
                Pool(prefab).Push(effect);
            }
        }

        void OnArrowHit(ArrowHit hit)
        {
            if (!hit.IsShot || hit.Collider == null)
                return;

            var normal = hit.Normal.sqrMagnitude > 1e-4f ? hit.Normal : -hit.Direction;
            var hitbox = hit.Collider.GetComponentInParent<Hitbox>();
            if (hitbox != null)
            {
                var critical = hitbox.Zone != HitZone.Body;
                Play(critical && m_HeadHitEffect != null ? m_HeadHitEffect : m_BodyHitEffect, hit.Point, normal);
                return;
            }

            if (hit.Collider.GetComponentInParent<Health>() == null)
                Play(m_SurfaceHitEffect, hit.Point, normal);
        }

        void OnEnemyKilled(Enemy enemy, DamageInfo info)
        {
            if (enemy != null)
                Play(m_DeathEffect, enemy.Center, Vector3.up);
        }

        void Play(ParticleSystem prefab, Vector3 position, Vector3 normal)
        {
            if (prefab == null)
                return;

            if (m_Frame != Time.frameCount)
            {
                m_Frame = Time.frameCount;
                m_SpawnedThisFrame = 0;
            }

            if (m_SpawnedThisFrame >= m_MaxPerFrame)
                return;
            m_SpawnedThisFrame++;

            var pool = Pool(prefab);
            var effect = pool.Count > 0 ? pool.Pop() : Instantiate(prefab, transform);
            effect.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
            effect.gameObject.SetActive(true);
            effect.Clear(true);
            effect.Play(true);
            m_Playing.Add((effect, prefab));
        }

        Stack<ParticleSystem> Pool(ParticleSystem prefab)
        {
            if (!m_Free.TryGetValue(prefab, out var pool))
            {
                pool = new Stack<ParticleSystem>();
                m_Free.Add(prefab, pool);
            }

            return pool;
        }
    }
}
