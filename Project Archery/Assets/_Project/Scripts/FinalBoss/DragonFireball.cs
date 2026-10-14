using Archery.Bows;
using Archery.Core;
using Archery.Economy;
using Archery.UI;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Boule de feu du dragon en vol (phase 2) : elle file vers l'endroit où sera le joueur, explose contre le décor
    /// (un pilier arrête le tir) ou près du joueur, et blesse tout ce qui est dans le rayon de l'explosion.
    /// On peut l'abattre d'une flèche : elle explose alors en l'air, sans danger.
    /// </summary>
    public class DragonFireball : MonoBehaviour, IArrowHitHandler
    {
        Vector3 m_Velocity;
        float m_Damage;
        float m_Radius;
        float m_Age;
        bool m_Done;
        Object m_Source;
        Material m_FireMaterial;
        AudioClip m_ExplosionClip;
        Collider m_Collider;
        DragonBoss m_Owner;

        const float k_Lifetime = 6f;
        const int k_ShotDownPoints = 10;

        /// <summary>Crée et lance une boule de feu.</summary>
        public static DragonFireball Launch(Vector3 position, Vector3 velocity, float damage, float radius, float size,
                                            Material coreMaterial, Material fireMaterial, AudioClip explosionClip, DragonBoss owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Boule de feu";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * size;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = coreMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var fireball = go.AddComponent<DragonFireball>();
            fireball.m_Velocity = velocity;
            fireball.m_Damage = damage;
            fireball.m_Radius = radius;
            fireball.m_Source = owner;
            fireball.m_Owner = owner;
            fireball.m_FireMaterial = fireMaterial;
            fireball.m_ExplosionClip = explosionClip;
            fireball.m_Collider = go.GetComponent<Collider>();

            FireEffects.CreateTrail(go.transform, fireMaterial, size * 1.2f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.5f, 0.15f);
            light.range = 6f;
            light.intensity = 2f;
            light.shadows = LightShadows.None;
            return fireball;
        }

        void Update()
        {
            if (m_Done)
                return;

            var deltaTime = Time.deltaTime;
            m_Age += deltaTime;
            if (m_Age > k_Lifetime)
            {
                Explode(transform.position, harmless: false);
                return;
            }

            var start = transform.position;
            var step = m_Velocity * deltaTime;

            // Elle explose au contact du joueur (ou tout près de lui).
            if ((ArenaPlayer.Chest - (start + step)).sqrMagnitude < 0.6f * 0.6f)
            {
                Explode(start + step, harmless: false);
                return;
            }

            if (step.sqrMagnitude > 1e-6f &&
                Physics.SphereCast(start, transform.localScale.x * 0.3f, step.normalized, out var hit, step.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                IsObstacle(hit.collider))
            {
                Explode(hit.point, harmless: false);
                return;
            }

            transform.position = start + step;
        }

        // Le décor l'arrête ; pas le dragon, ni elle-même, ni ce que les flèches ignorent (corps du joueur, arc).
        bool IsObstacle(Collider other)
        {
            if (other == null || other == m_Collider || ArrowIgnore.IsIgnored(other))
                return false;
            if (m_Owner != null && other.transform.IsChildOf(m_Owner.transform))
                return false;
            return other.GetComponentInParent<DragonFireball>() == null && other.GetComponentInParent<Player.PlayerRig>() == null;
        }

        /// <summary>Abattue d'une flèche : elle explose en l'air, sans danger, et rapporte quelques points.</summary>
        public bool OnArrowHit(in ArrowHit hit)
        {
            if (m_Done)
                return false;

            var score = ScoreManager.Instance;
            var gained = score != null ? score.AddPoints(k_ShotDownPoints * score.DifficultyMultiplier) : 0;
            FloatingText.Spawn(transform.position + Vector3.up * 0.5f, gained > 0 ? $"Abattue ! +{gained}" : "Abattue !", new Color(1f, 0.7f, 0.3f), 0.9f, 1.2f);
            Explode(transform.position, harmless: true);
            return true;
        }

        void Explode(Vector3 point, bool harmless)
        {
            m_Done = true;
            if (m_Collider != null)
                m_Collider.enabled = false;

            FireEffects.Burst(point, m_FireMaterial, m_Radius, 50);
            Sfx.Play(m_ExplosionClip, point, 1f, Random.Range(0.8f, 0.95f));

            if (!harmless && ArenaPlayer.IsAlive)
            {
                var distance = Vector3.Distance(ArenaPlayer.Chest, point);
                if (distance <= m_Radius)
                {
                    // Pleins dégâts au centre, moitié au bord.
                    var falloff = Mathf.Lerp(1f, 0.5f, distance / Mathf.Max(0.01f, m_Radius));
                    ArenaPlayer.Damage(m_Damage * falloff, point, m_Source);
                    ArenaPlayer.Rumble(0.6f, 0.2f);
                }
            }

            Destroy(gameObject);
        }
    }
}
