using Archery.Bows;
using Archery.Core;
using Archery.Economy;
using Archery.UI;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Boule de feu du dragon, son attaque la plus courante (au sol comme en vol) : énorme, elle se forme dans sa gueule
    /// puis file tout droit vers l'endroit où était le joueur au tir. On l'esquive en se déplaçant. Elle explose contre
    /// le décor (un pilier l'arrête) ou sur le joueur ; si elle s'écrase à côté de lui, l'explosion le blesse un peu.
    /// On peut l'abattre d'une flèche, même dans la gueule du dragon : elle explose alors sans danger.
    /// </summary>
    public class DragonFireball : MonoBehaviour, IArrowHitHandler
    {
        Transform m_Core;
        ParticleSystem m_Flames;
        ParticleSystem m_Trail;
        Light m_Light;
        Collider m_Collider;
        Vector3 m_Velocity;
        float m_Size;
        float m_Radius;
        float m_Damage;
        float m_SplashRadius;
        float m_Age;
        bool m_Launched;
        bool m_Done;
        Object m_Source;
        Material m_FireMaterial;
        AudioClip m_ExplosionClip;
        DragonBoss m_Owner;

        const float k_Lifetime = 7f;
        const int k_ShotDownPoints = 10;

        /// <summary>Encore dans la gueule du dragon.</summary>
        public bool IsHeld => !m_Launched && !m_Done;

        /// <summary>Crée une boule de feu, toute petite, qui se forme dans la gueule (voir <see cref="Hold"/>, puis <see cref="Launch"/>).</summary>
        public static DragonFireball Create(Vector3 position, float size, Material coreMaterial, Material fireMaterial, AudioClip explosionClip, DragonBoss owner)
        {
            var go = new GameObject("Boule de feu");
            go.transform.position = position;
            var fireball = go.AddComponent<DragonFireball>();
            fireball.m_Size = size;
            fireball.m_Source = owner;
            fireball.m_Owner = owner;
            fireball.m_FireMaterial = fireMaterial;
            fireball.m_ExplosionClip = explosionClip;

            // Le cœur : une sphère à la texture de feu qui tourne sur elle-même ; c'est elle que les flèches touchent.
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Cœur";
            core.transform.SetParent(go.transform, false);
            var renderer = core.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = coreMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fireball.m_Core = core.transform;
            fireball.m_Collider = core.GetComponent<Collider>();

            if (fireMaterial != null)
            {
                fireball.m_Flames = FireEffects.CreateFireballFlames(go.transform, fireMaterial, size);
                fireball.m_Trail = FireEffects.CreateTrail(go.transform, fireMaterial, size * 0.8f);
                fireball.m_Trail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.5f, 0.15f);
            light.shadows = LightShadows.None;
            fireball.m_Light = light;
            fireball.SetCharge(0f);
            return fireball;
        }

        /// <summary>Pendant qu'elle se forme dans la gueule : sa position, et sa taille (0 à 1).</summary>
        public void Hold(Vector3 position, float charge)
        {
            if (!IsHeld)
                return;

            transform.position = position;
            SetCharge(charge);
        }

        /// <summary>Elle part : vitesse, dégâts d'un coup direct, rayon de l'explosion à côté du joueur.</summary>
        public void Launch(Vector3 velocity, float damage, float splashRadius)
        {
            if (!IsHeld)
                return;

            SetCharge(1f);
            m_Velocity = velocity;
            m_Damage = damage;
            m_SplashRadius = splashRadius;
            m_Launched = true;
            m_Age = 0f;
            if (m_Trail != null)
                m_Trail.Play();
        }

        /// <summary>Le dragon est interrompu avant de la lancer : elle se dissipe.</summary>
        public void Fizzle()
        {
            if (m_Done)
                return;

            m_Done = true;
            FireEffects.Burst(transform.position, m_FireMaterial, m_Radius * 1.2f, 20);
            Destroy(gameObject);
        }

        void SetCharge(float charge)
        {
            var size = m_Size * Mathf.Lerp(0.15f, 1f, Mathf.Clamp01(charge));
            m_Radius = size * 0.5f;
            m_Core.localScale = Vector3.one * size;
            if (m_Flames != null)
                FireEffects.SetFireballFlamesSize(m_Flames, size);
            m_Light.range = 3f + size * 4f;
            m_Light.intensity = 1.5f + 2.5f * charge;
        }

        void Update()
        {
            if (m_Done)
                return;

            var deltaTime = Time.deltaTime;
            m_Core.Rotate(new Vector3(37f, 91f, 23f) * deltaTime, Space.Self);
            if (!m_Launched)
                return;

            m_Age += deltaTime;
            if (m_Age > k_Lifetime)
            {
                Explode(transform.position, direct: false);
                return;
            }

            var start = transform.position;
            var step = m_Velocity * deltaTime;

            // Le joueur est dans son chemin : de ses pieds à sa tête, son corps touche la boule.
            if (TouchesPlayer(start + step, m_Radius))
            {
                Explode(start + step, direct: true);
                return;
            }

            if (step.sqrMagnitude > 1e-6f &&
                Physics.SphereCast(start, m_Radius * 0.6f, step.normalized, out var hit, step.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                IsObstacle(hit.collider))
            {
                Explode(hit.point, direct: false);
                return;
            }

            transform.position = start + step;
        }

        static bool TouchesPlayer(Vector3 center, float radius)
        {
            if (!ArenaPlayer.IsAlive)
                return false;
            return DistanceToPlayer(center) <= radius + 0.25f;
        }

        // Distance de ce point au corps du joueur (un segment des pieds au-dessus de la tête).
        static float DistanceToPlayer(Vector3 point)
        {
            var feet = ArenaPlayer.Feet + Vector3.up * 0.3f;
            var head = ArenaPlayer.Chest + Vector3.up * 0.3f;
            var segment = head - feet;
            var t = Mathf.Clamp01(Vector3.Dot(point - feet, segment) / Mathf.Max(1e-4f, segment.sqrMagnitude));
            return Vector3.Distance(point, feet + segment * t);
        }

        // Le décor l'arrête ; pas le dragon, ni elle-même, ni les flèches, ni ce qu'elles ignorent (corps du joueur, arc).
        bool IsObstacle(Collider other)
        {
            if (other == null || other == m_Collider || ArrowIgnore.IsIgnored(other))
                return false;
            if (m_Owner != null && other.transform.IsChildOf(m_Owner.transform))
                return false;
            return other.GetComponentInParent<DragonFireball>() == null && other.GetComponentInParent<Player.PlayerRig>() == null &&
                   other.GetComponentInParent<Arrow>() == null;
        }

        /// <summary>Abattue d'une flèche : elle explose en l'air, sans danger, et rapporte quelques points.</summary>
        public bool OnArrowHit(in ArrowHit hit)
        {
            if (m_Done)
                return false;

            var score = ScoreManager.Instance;
            var gained = score != null ? score.AddPoints(k_ShotDownPoints * score.DifficultyMultiplier) : 0;
            FloatingText.Spawn(transform.position + Vector3.up * 0.8f, gained > 0 ? $"Abattue ! +{gained}" : "Abattue !", new Color(1f, 0.7f, 0.3f), 0.9f, 1.2f);
            Explode(transform.position, direct: false, harmless: true);
            return true;
        }

        void Explode(Vector3 point, bool direct, bool harmless = false)
        {
            m_Done = true;
            if (m_Collider != null)
                m_Collider.enabled = false;

            FireEffects.Burst(point, m_FireMaterial, Mathf.Max(m_SplashRadius, m_Radius * 2f), 70);
            Sfx.Play(m_ExplosionClip, point, 1f, Random.Range(0.7f, 0.85f));

            if (!harmless && ArenaPlayer.IsAlive)
            {
                if (direct)
                {
                    ArenaPlayer.Damage(m_Damage, point, m_Source);
                    ArenaPlayer.Rumble(0.7f, 0.25f);
                }
                else
                {
                    // Écrasée à côté de lui : moitié des dégâts au plus près, un quart au bord de l'explosion.
                    var distance = DistanceToPlayer(point);
                    if (distance <= m_SplashRadius)
                    {
                        ArenaPlayer.Damage(m_Damage * Mathf.Lerp(0.5f, 0.25f, distance / Mathf.Max(0.01f, m_SplashRadius)), point, m_Source);
                        ArenaPlayer.Rumble(0.45f, 0.2f);
                    }
                }
            }

            Destroy(gameObject);
        }
    }
}
