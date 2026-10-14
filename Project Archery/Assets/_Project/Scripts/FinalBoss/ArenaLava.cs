using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Player;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// La lave de l'arène : l'anneau au pied des parois brûle le joueur qui y marche, et en phase 3 (dragon enragé)
    /// des flaques de lave débordent et s'étendent sur le sol, ce qui rétrécit l'espace (GDD, section 23).
    /// </summary>
    [DisallowMultipleComponent]
    public class ArenaLava : MonoBehaviour
    {
        [SerializeField]
        DragonArenaBuilder m_Arena;

        [Tooltip("Matériau des flaques. Vide : celui de la lave de l'arène.")]
        [SerializeField]
        Material m_PoolMaterial;

        [SerializeField]
        Material m_FireMaterial;

        [Header("Brûlure")]
        [Tooltip("Dégâts par seconde dans la lave (avant la difficulté et la vague).")]
        [SerializeField]
        float m_DamagePerSecond = 14f;

        [SerializeField]
        float m_TickInterval = 0.5f;

        [Header("Débordement (phase 3)")]
        [Tooltip("Temps (s) entre deux nouvelles flaques.")]
        [SerializeField]
        float m_PoolInterval = 7f;

        [SerializeField]
        int m_MaxPools = 6;

        [Tooltip("Rayon final d'une flaque (m) : plus petit et plus grand.")]
        [SerializeField]
        Vector2 m_PoolRadius = new Vector2(2.2f, 3.4f);

        [Tooltip("Temps (s) pour qu'une flaque atteigne sa taille.")]
        [SerializeField]
        float m_PoolGrowTime = 5f;

        [Tooltip("Une nouvelle flaque n'apparaît jamais à moins de cette distance (m) du joueur.")]
        [SerializeField]
        float m_MinPlayerDistance = 4f;

        [SerializeField]
        AudioClip m_BubbleClip;

        sealed class Pool
        {
            public Transform Root;
            public Vector3 Center;
            public float Radius;
            public float Target;
            public float Age;
        }

        readonly List<Pool> m_Pools = new List<Pool>();
        float m_DamageScale = 1f;
        float m_TickTimer;
        float m_PoolTimer;
        bool m_Overflowing;
        Mesh m_DiscMesh;

        /// <summary>Multiplie les dégâts (difficulté et vague : ceux du dragon).</summary>
        public float DamageScale
        {
            get => m_DamageScale;
            set => m_DamageScale = Mathf.Max(0f, value);
        }

        void Awake()
        {
            if (m_Arena == null)
                m_Arena = GetComponentInParent<DragonArenaBuilder>();
            m_DiscMesh = BuildDisc();
        }

        void OnDestroy()
        {
            if (m_DiscMesh != null)
                Destroy(m_DiscMesh);
        }

        /// <summary>Phase 3 : la lave commence à déborder.</summary>
        public void BeginOverflow()
        {
            m_Overflowing = true;
            m_PoolTimer = 1.5f;
        }

        /// <summary>Le dragon est mort : les flaques se figent et refroidissent.</summary>
        public void Calm()
        {
            m_Overflowing = false;
            foreach (var pool in m_Pools)
                pool.Target = 0f;
        }

        void Update()
        {
            if (m_Arena == null)
                return;

            var deltaTime = Time.deltaTime;
            UpdatePools(deltaTime);

            m_TickTimer -= deltaTime;
            if (m_TickTimer > 0f)
                return;

            m_TickTimer = m_TickInterval;
            if (PlayerInLava())
                ArenaPlayer.Damage(m_DamagePerSecond * m_TickInterval * m_DamageScale, ArenaPlayer.Feet, this);
        }

        bool PlayerInLava()
        {
            if (!ArenaPlayer.IsAlive)
                return false;

            var feet = ArenaPlayer.Feet;
            var local = m_Arena.transform.InverseTransformPoint(feet);
            if (Mathf.Abs(local.y) < 1.5f && new Vector2(local.x, local.z).magnitude > m_Arena.LavaRingStart + 0.3f)
                return true;

            foreach (var pool in m_Pools)
            {
                if (pool.Radius > 0.5f && ArenaPlayer.FeetInside(pool.Center, pool.Radius * 0.9f))
                    return true;
            }

            return false;
        }

        void UpdatePools(float deltaTime)
        {
            if (m_Overflowing && m_Pools.Count < m_MaxPools)
            {
                m_PoolTimer -= deltaTime;
                if (m_PoolTimer <= 0f)
                {
                    m_PoolTimer = m_PoolInterval;
                    TrySpawnPool();
                }
            }

            for (var i = m_Pools.Count - 1; i >= 0; i--)
            {
                var pool = m_Pools[i];
                pool.Age += deltaTime;
                var speed = Mathf.Max(0.1f, m_PoolRadius.y) / Mathf.Max(0.1f, m_PoolGrowTime);
                pool.Radius = Mathf.MoveTowards(pool.Radius, pool.Target, speed * deltaTime);
                pool.Root.localScale = new Vector3(pool.Radius, 1f, pool.Radius);
                if (pool.Target <= 0f && pool.Radius <= 0.01f)
                {
                    Destroy(pool.Root.gameObject);
                    m_Pools.RemoveAt(i);
                }
            }
        }

        void TrySpawnPool()
        {
            var player = ArenaPlayer.Feet;
            for (var attempt = 0; attempt < 12; attempt++)
            {
                var angle = Random.value * Mathf.PI * 2f;
                var radius = Mathf.Lerp(5f, m_Arena.LavaRingStart - 2f, Random.value);
                var point = m_Arena.transform.TransformPoint(new Vector3(Mathf.Sin(angle) * radius, 0.08f, Mathf.Cos(angle) * radius));
                var flat = point - player;
                flat.y = 0f;
                if (flat.magnitude < m_MinPlayerDistance + m_PoolRadius.y || !m_Arena.IsFreeGround(point, m_PoolRadius.y))
                    continue;

                if (OverlapsPool(point))
                    continue;

                SpawnPool(point);
                return;
            }
        }

        bool OverlapsPool(Vector3 point)
        {
            foreach (var pool in m_Pools)
            {
                if (Vector3.Distance(pool.Center, point) < pool.Target + m_PoolRadius.y)
                    return true;
            }

            return false;
        }

        void SpawnPool(Vector3 point)
        {
            var go = new GameObject("Flaque de lave");
            go.transform.SetParent(transform, true);
            go.transform.position = point;
            go.transform.rotation = m_Arena.transform.rotation * Quaternion.Euler(0f, Random.value * 360f, 0f);
            go.transform.localScale = new Vector3(0.01f, 1f, 0.01f);
            go.AddComponent<MeshFilter>().sharedMesh = m_DiscMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_PoolMaterial != null ? m_PoolMaterial : m_Arena.LavaMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var flames = FireEffects.CreateGroundFlames(go.transform, m_FireMaterial, 0.8f, 10f);
            flames.transform.localPosition = Vector3.up * 0.1f;

            m_Pools.Add(new Pool { Root = go.transform, Center = point, Target = Mathf.Lerp(m_PoolRadius.x, m_PoolRadius.y, Random.value) });
            Sfx.Play(m_BubbleClip, point, 0.8f, 0.6f);
        }

        // Un disque irrégulier de rayon 1, à plat (mis à l'échelle de la flaque).
        static Mesh BuildDisc()
        {
            var builder = new MeshBuilder();
            const int sides = 14;
            var center = Vector3.zero;
            var points = new Vector3[sides];
            for (var i = 0; i < sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                var radius = 0.8f + 0.2f * Mathf.PerlinNoise(i * 0.7f, 3f);
                points[i] = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
            }

            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                builder.Triangle(center, points[i], points[next], Color.white,
                                 new Vector2(0.5f, 0.5f), new Vector2(points[i].x * 0.5f + 0.5f, points[i].z * 0.5f + 0.5f),
                                 new Vector2(points[next].x * 0.5f + 0.5f, points[next].z * 0.5f + 0.5f));
            }

            return builder.ToMesh("Flaque de lave");
        }
    }
}
