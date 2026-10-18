using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Economy;
using Archery.Player;
using Archery.UI;
using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Projectile lent d'un tireur (GDD, section 10). Il vole tout droit : il blesse le joueur s'il le touche,
    /// se brise sur le décor (en abîmant la tour ou une barricade s'il les touche), et peut être abattu d'une flèche
    /// (quelques points en récompense).
    /// </summary>
    /// <remarks>
    /// Il lui faut un collider qui n'est pas un trigger, pour que les flèches puissent le toucher.
    /// </remarks>
    public class EnemyProjectile : MonoBehaviour, IArrowHitHandler
    {
        [Tooltip("Durée de vie maximale (s).")]
        [SerializeField]
        float m_Lifetime = 8f;

        [Tooltip("Rayon (m) autour du corps du joueur (des pieds à la tête) qui compte comme touché.")]
        [SerializeField]
        float m_HitRadius = 0.35f;

        [Tooltip("Points de base gagnés en abattant le projectile d'une flèche.")]
        [SerializeField]
        int m_ShotDownPoints = 5;

        [SerializeField]
        Color m_ShotDownColor = new Color(0.6f, 0.9f, 1f);

        [Tooltip("Optionnel : son quand il touche le joueur ou le décor.")]
        [SerializeField]
        AudioClip m_ImpactClip;

        [Tooltip("Optionnel : son quand une flèche l'abat.")]
        [SerializeField]
        AudioClip m_ShotDownClip;

        [Tooltip("Optionnel : effet créé là où il se brise (particules), détruit au bout de 2 s.")]
        [SerializeField]
        GameObject m_BreakEffect;

        Collider[] m_Colliders;
        TrailRenderer[] m_Trails;
        Light[] m_Lights;
        float[] m_TrailWidths;
        float[] m_LightRanges;
        Vector3 m_BaseScale;
        Vector3 m_Velocity;
        float m_Damage;
        Enemy m_Shooter;
        float m_Age;
        float m_Size = 1f;
        bool m_Held;
        bool m_Done;

        void Awake()
        {
            m_Colliders = GetComponentsInChildren<Collider>(true);
            m_Trails = GetComponentsInChildren<TrailRenderer>(true);
            m_Lights = GetComponentsInChildren<Light>(true);
            m_TrailWidths = System.Array.ConvertAll(m_Trails, trail => trail.widthMultiplier);
            m_LightRanges = System.Array.ConvertAll(m_Lights, light => light.range);
            m_BaseScale = transform.localScale;
        }

        /// <summary>Un projectile vient d'être lancé (la nuit, il éclaire le sol en passant).</summary>
        public static event System.Action<EnemyProjectile> Launched;

        /// <summary>Tenu au bout du bâton pendant sa charge (liche) : il ne vole pas encore.</summary>
        public bool IsHeld => m_Held && !m_Done;

        /// <summary>
        /// Charge du projectile, tenu à <paramref name="position"/> : il grossit de presque rien jusqu'à
        /// <paramref name="size"/> fois sa taille (<paramref name="progress"/> de 0 à 1). Une flèche peut déjà l'abattre.
        /// </summary>
        public void Charge(Vector3 position, float progress, float size)
        {
            if (m_Done)
                return;

            if (!m_Held)
            {
                m_Held = true;
                foreach (var trail in m_Trails)
                    trail.emitting = false;
            }

            transform.position = position;
            SetSize(size * Mathf.Lerp(0.12f, 1f, 1f - (1f - progress) * (1f - progress)));
        }

        // Plus gros : la boule, son collider, sa zone de touche, sa traînée et sa lumière.
        void SetSize(float size)
        {
            m_Size = Mathf.Max(0.01f, size);
            transform.localScale = m_BaseScale * m_Size;
            for (var i = 0; i < m_Trails.Length; i++)
                m_Trails[i].widthMultiplier = m_TrailWidths[i] * m_Size;
            for (var i = 0; i < m_Lights.Length; i++)
                m_Lights[i].range = m_LightRanges[i] * Mathf.Max(1f, m_Size);
        }

        /// <summary>La charge est interrompue (le lanceur est mort) : la boule se dissipe.</summary>
        public void Fizzle()
        {
            if (!m_Done)
                Break(transform.position, null);
        }

        /// <summary>Lance le projectile (appelé par <see cref="EnemyRangedAttack"/>).</summary>
        public void Launch(Vector3 velocity, float damage, Enemy shooter)
        {
            if (m_Held)
            {
                m_Held = false;
                foreach (var trail in m_Trails)
                {
                    trail.Clear();
                    trail.emitting = true;
                }
            }

            m_Velocity = velocity;
            m_Damage = damage;
            m_Shooter = shooter;
            if (velocity.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(velocity);
            Launched?.Invoke(this);
        }

        void Update()
        {
            if (m_Done || m_Held)
                return;

            var deltaTime = Time.deltaTime;
            m_Age += deltaTime;
            if (m_Age > m_Lifetime)
            {
                Destroy(gameObject);
                return;
            }

            var start = transform.position;
            var step = m_Velocity * deltaTime;
            var end = start + step;

            if (TouchesPlayer(end))
            {
                HitPlayer(end);
                return;
            }

            // Le décor : un rayon entre deux positions, fiable même vite.
            if (step.sqrMagnitude > 1e-6f &&
                Physics.Raycast(start, step.normalized, out var hit, step.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                IsObstacle(hit.collider))
            {
                DamageObstacle(hit.collider, hit.point);
                Break(hit.point, m_ImpactClip);
                return;
            }

            transform.position = end;
        }

        // Le corps du joueur : un segment de ses pieds (un peu au-dessus) à sa tête.
        bool TouchesPlayer(Vector3 point)
        {
            var player = PlayerHealth.Instance;
            var rig = PlayerRig.Instance;
            if (player == null || !player.IsAlive || rig == null || rig.Head == null)
                return false;

            var feet = player.BodyPosition + Vector3.up * 0.3f;
            var head = rig.Head.position;
            var segment = head - feet;
            var t = segment.sqrMagnitude > 1e-4f ? Mathf.Clamp01(Vector3.Dot(point - feet, segment) / segment.sqrMagnitude) : 0f;
            var radius = m_HitRadius * m_Size;
            return (point - (feet + segment * t)).sqrMagnitude <= radius * radius;
        }

        void HitPlayer(Vector3 point)
        {
            var player = PlayerHealth.Instance;
            player.Health.TakeDamage(new DamageInfo
            {
                Amount = m_Damage,
                Zone = HitZone.Body,
                Point = point,
                Direction = m_Velocity.sqrMagnitude > 1e-4f ? m_Velocity.normalized : transform.forward,
                Source = m_Shooter != null ? (Object)m_Shooter : this,
            });
            Break(point, m_ImpactClip);
        }

        // La tour ou une barricade touchée perd des PV (un tireur bloqué devant une barricade la casse ainsi).
        void DamageObstacle(Collider other, Vector3 point)
        {
            var health = other.GetComponentInParent<Health>();
            var player = PlayerHealth.Instance;
            if (health == null || !health.IsAlive || (player != null && health == player.Health))
                return;

            health.TakeDamage(new DamageInfo
            {
                Amount = m_Damage,
                Zone = HitZone.Body,
                Point = point,
                Direction = m_Velocity.sqrMagnitude > 1e-4f ? m_Velocity.normalized : transform.forward,
                Source = m_Shooter != null ? (Object)m_Shooter : this,
            });
        }

        // Ce qui arrête le projectile : le décor, la tour. Pas les ennemis, ni lui-même, ni le joueur (traité à part).
        bool IsObstacle(Collider other)
        {
            if (other == null || ArrowIgnore.IsIgnored(other))
                return false;

            foreach (var own in m_Colliders)
            {
                if (own == other)
                    return false;
            }

            return other.GetComponentInParent<Enemy>() == null && other.GetComponentInParent<EnemyProjectile>() == null;
        }

        /// <summary>Une flèche l'abat : petite récompense, et il se brise.</summary>
        public bool OnArrowHit(in ArrowHit hit)
        {
            if (m_Done || !hit.IsShot)
                return false;

            var score = ScoreManager.Instance;
            var gained = score != null ? score.AddPoints(m_ShotDownPoints * score.DifficultyMultiplier) : 0;
            FloatingText.Spawn(transform.position + Vector3.up * 0.4f, gained > 0 ? $"Abattu ! +{gained}" : "Abattu !", m_ShotDownColor, 0.8f, 1.2f);
            Break(transform.position, m_ShotDownClip);
            return true;
        }

        void Break(Vector3 point, AudioClip clip)
        {
            m_Done = true;
            foreach (var collider in m_Colliders)
            {
                if (collider != null)
                    collider.enabled = false;
            }

            Sfx.Play(clip, point, 0.8f, Random.Range(0.95f, 1.05f));
            if (m_BreakEffect != null)
                Destroy(Instantiate(m_BreakEffect, point, Quaternion.identity), 2f);
            Destroy(gameObject);
        }
    }
}
