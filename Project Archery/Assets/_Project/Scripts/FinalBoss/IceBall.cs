using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Boule de glace d'une tour de givre, dans l'arène du boss final : elle file tout droit vers le dragon et le ralentit
    /// au coup, d'un ralentissement qui s'estompe vite (<see cref="Enemy.SlowFading"/>). Le décor l'arrête ; elle ne fait
    /// pas de dégâts.
    /// </summary>
    public class IceBall : MonoBehaviour
    {
        const float k_Lifetime = 4f;
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        Vector3 m_Velocity;
        float m_Radius;
        float m_Slow;
        float m_SlowTime;
        float m_Age;
        bool m_Done;
        Transform m_Core;
        Transform m_Shooter;
        ParticleSystem m_Trail;
        Material m_SparkMaterial;
        Color m_Color;
        AudioClip m_HitClip;

        /// <summary>
        /// Lance une boule de glace. <paramref name="slow"/> : vitesse de l'ennemi au coup (0,7 = 30 % plus lent),
        /// revenue à la normale en <paramref name="slowTime"/> s.
        /// </summary>
        public static void Launch(Vector3 position, Vector3 velocity, float size, float slow, float slowTime, Transform shooter,
                                  Material coreMaterial, Material sparkMaterial, Color color, AudioClip hitClip)
        {
            var go = new GameObject("Boule de glace");
            go.transform.position = position;
            var ball = go.AddComponent<IceBall>();
            ball.m_Velocity = velocity;
            ball.m_Radius = size * 0.5f;
            ball.m_Slow = slow;
            ball.m_SlowTime = slowTime;
            ball.m_Shooter = shooter;
            ball.m_SparkMaterial = sparkMaterial;
            ball.m_Color = color;
            ball.m_HitClip = hitClip;

            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Cœur";
            DestroyImmediate(core.GetComponent<Collider>());
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = Vector3.one * size;
            var renderer = core.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = coreMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ball.m_Core = core.transform;

            if (sparkMaterial != null)
                ball.m_Trail = FireEffects.CreateSparkTrail(go.transform, sparkMaterial, size * 0.4f, color);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 4f;
            light.intensity = 1.5f;
            light.shadows = LightShadows.None;
        }

        void Update()
        {
            if (m_Done)
                return;

            var deltaTime = Time.deltaTime;
            m_Age += deltaTime;
            m_Core.Rotate(new Vector3(53f, 97f, 31f) * deltaTime, Space.Self);
            if (m_Age > k_Lifetime)
            {
                Shatter(transform.position);
                return;
            }

            var step = m_Velocity * deltaTime;
            var length = step.magnitude;
            if (length < 1e-5f)
                return;

            var start = transform.position;
            var direction = step / length;
            var count = Physics.SphereCastNonAlloc(start, m_Radius, direction, s_Hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var found = false;
            var bestDistance = float.MaxValue;
            var bestPoint = Vector3.zero;
            Enemy bestEnemy = null;
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                var other = hit.collider;
                if (other == null || ArrowIgnore.IsIgnored(other) || (m_Shooter != null && other.transform.IsChildOf(m_Shooter)))
                    continue;
                if (other.GetComponentInParent<Player.PlayerRig>() != null || other.GetComponentInParent<Arrow>() != null || other.GetComponentInParent<DragonFireball>() != null)
                    continue;

                // Un corps d'ennemi sans zone de touche ne compte pas ; le décor l'arrête.
                var enemy = other.GetComponentInParent<Enemy>();
                if (enemy != null && other.GetComponentInParent<Hitbox>() == null)
                    continue;
                if (hit.distance >= bestDistance)
                    continue;

                found = true;
                bestDistance = hit.distance;
                bestPoint = hit.distance > 0f ? hit.point : start;
                bestEnemy = enemy;
            }

            if (found)
            {
                if (bestEnemy != null && bestEnemy.IsAlive)
                    bestEnemy.SlowFading(m_Slow, m_SlowTime);
                Shatter(bestPoint);
                return;
            }

            transform.position = start + step;
        }

        // Elle éclate en éclats de glace.
        void Shatter(Vector3 point)
        {
            m_Done = true;
            if (m_Trail != null)
            {
                m_Trail.transform.SetParent(null, true);
                m_Trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(m_Trail.gameObject, 1f);
            }

            if (m_SparkMaterial != null)
                FireEffects.SparkBurst(point, m_SparkMaterial, 1.5f, 40, m_Color);
            Sfx.Play(m_HitClip, point, 0.9f, Random.Range(0.95f, 1.1f));
            Destroy(gameObject);
        }
    }
}
