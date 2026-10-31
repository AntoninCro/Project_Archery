using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Building
{
    /// <summary>
    /// Une flaque au sol, pour un temps (deuxième brainstorm du tower defense, 29 octobre) : le goudron des obus du mortier
    /// (niveau 4), qui ralentit, et que ta flèche enflammée embrase ; le sol en feu du bombardement (niveau 5). En feu, elle
    /// blesse ceux qui sont dessus.
    /// </summary>
    /// <remarks>
    /// Faite par le code (un disque au bord irrégulier, comme la flaque de goudron), sans collider. Une flèche enflammée qui touche
    /// à moins de son rayon (<see cref="Arrow.AnyHit"/>) l'embrase.
    /// </remarks>
    public class GroundPatch : MonoBehaviour
    {
        const float k_Tick = 0.25f;

        static Mesh s_Disc;
        static readonly List<GroundPatch> s_All = new List<GroundPatch>();
        static readonly List<Enemy> s_Targets = new List<Enemy>();

        float m_Radius;
        float m_Life;
        float m_Age;
        float m_Speed = 1f;
        float m_BurnDamage;
        float m_BurnLeft;
        float m_Timer;
        Object m_Source;
        Transform m_Disc;
        readonly List<GameObject> m_Flames = new List<GameObject>();

        /// <summary>Le son quand elle s'embrase.</summary>
        public AudioClip IgniteClip { get; set; }

        /// <summary>
        /// Pose une flaque de <paramref name="radius"/> m pour <paramref name="life"/> s. <paramref name="speed"/> : vitesse gardée
        /// par les ennemis dessus (1 = pas ralentis).
        /// </summary>
        public static GroundPatch Spawn(Vector3 point, float radius, float life, float speed, Material material, Object source)
        {
            var go = new GameObject("Flaque");
            go.transform.position = point + Vector3.up * 0.04f;
            var patch = go.AddComponent<GroundPatch>();
            patch.m_Radius = Mathf.Max(0.3f, radius);
            patch.m_Life = Mathf.Max(0.5f, life);
            patch.m_Speed = Mathf.Clamp(speed, 0.05f, 1f);
            patch.m_Source = source;

            var disc = new GameObject("Disque");
            disc.transform.SetParent(go.transform, false);
            disc.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            disc.AddComponent<MeshFilter>().sharedMesh = Disc;
            var renderer = disc.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            patch.m_Disc = disc.transform;
            patch.m_Disc.localScale = new Vector3(0.1f, 1f, 0.1f);
            return patch;
        }

        /// <summary>
        /// L'embrase : <paramref name="damage"/> de dégâts par seconde à ceux qui sont dessus, pendant <paramref name="duration"/> s
        /// (la flaque dure au moins jusque-là).
        /// </summary>
        public void Ignite(float damage, float duration, AudioClip clip = null)
        {
            var first = m_BurnLeft <= 0f;
            m_BurnDamage = Mathf.Max(m_BurnDamage, damage);
            m_BurnLeft = Mathf.Max(m_BurnLeft, duration);
            m_Life = Mathf.Max(m_Life, m_Age + duration + 0.5f);
            if (!first)
                return;

            Sfx.Play(clip != null ? clip : IgniteClip, transform.position + Vector3.up * 0.3f, 0.8f, Random.Range(0.9f, 1.1f));
            var model = BurnFlames.Enemy;
            if (model == null)
                return;

            // Quelques foyers sur la flaque, plus nombreux sur une grande.
            var count = Mathf.Clamp(Mathf.RoundToInt(m_Radius * 1.5f), 2, 6);
            for (var i = 0; i < count; i++)
            {
                var offset = i == 0 ? Vector3.zero : Quaternion.Euler(0f, i * 360f / (count - 1), 0f) * Vector3.forward * m_Radius * 0.6f;
                var flames = Instantiate(model, transform.position + offset + Vector3.up * 0.2f, Quaternion.identity, transform);
                flames.SetActive(true);
                flames.transform.localScale = Vector3.one * Mathf.Clamp(m_Radius * 0.8f, 1f, 2.5f);
                m_Flames.Add(flames);
            }
        }

        public bool IsBurning => m_BurnLeft > 0f;

        void OnEnable()
        {
            s_All.Add(this);
            Arrow.AnyHit += OnArrowHit;
        }

        void OnDisable()
        {
            s_All.Remove(this);
            Arrow.AnyHit -= OnArrowHit;
        }

        // Une flèche enflammée qui touche sur la flaque (ou un ennemi dessus) l'embrase.
        void OnArrowHit(ArrowHit hit)
        {
            if (hit.Arrow == null || !hit.IsShot || IsBurning)
                return;

            var offset = hit.Point - transform.position;
            if (offset.y > 2.5f || offset.y < -1f)
                return;
            offset.y = 0f;
            if (offset.sqrMagnitude <= (m_Radius + 0.5f) * (m_Radius + 0.5f))
                Ignite(Mathf.Max(8f, hit.Damage * 0.4f), 4f);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_Age += deltaTime;
            if (m_BurnLeft > 0f)
            {
                m_BurnLeft -= deltaTime;
                if (m_BurnLeft <= 0f)
                    StopFlames();
            }

            // Elle s'étale vite, puis se résorbe à la fin.
            var grow = Mathf.Min(1f, m_Age / 0.25f) * Mathf.Clamp01((m_Life - m_Age) / 0.6f);
            if (m_Disc != null)
                m_Disc.localScale = new Vector3(m_Radius * grow, 1f, m_Radius * grow);
            if (m_Age >= m_Life)
            {
                Destroy(gameObject);
                return;
            }

            m_Timer -= deltaTime;
            if (m_Timer > 0f)
                return;

            m_Timer = k_Tick;
            // Une copie : un ennemi que le feu tue quitte la liste des vivants pendant qu'on la parcourt (le jeu plantait, 30
            // octobre).
            s_Targets.Clear();
            s_Targets.AddRange(Enemy.Alive);
            foreach (var enemy in s_Targets)
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsAirborne || enemy.IsFlyer || enemy.IsTrainingDummy)
                    continue;

                var offset = enemy.transform.position - transform.position;
                if (offset.y < -1f || offset.y > 1.5f)
                    continue;
                offset.y = 0f;
                if (offset.sqrMagnitude > m_Radius * m_Radius)
                    continue;

                // Le goudron l'englue : un ennemi préparé pour ta flèche (30 octobre).
                if (m_Speed < 1f)
                {
                    enemy.Slow(m_Speed, k_Tick * 2f);
                    enemy.Prepare(Preparation.Glued, k_Tick * 2f);
                }
                if (m_BurnLeft > 0f)
                {
                    enemy.Health.TakeDamage(new DamageInfo
                    {
                        Amount = m_BurnDamage * k_Tick,
                        Zone = HitZone.Body,
                        Point = enemy.Center,
                        Direction = Vector3.up,
                        Source = m_Source,
                    });
                }
            }
        }

        void StopFlames()
        {
            foreach (var flames in m_Flames)
            {
                if (flames != null)
                    Destroy(flames);
            }

            m_Flames.Clear();
        }

        /// <summary>La flaque la plus proche de ce point, à moins de son rayon (null s'il n'y en a pas).</summary>
        public static GroundPatch At(Vector3 point)
        {
            foreach (var patch in s_All)
            {
                var offset = point - patch.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= patch.m_Radius * patch.m_Radius)
                    return patch;
            }

            return null;
        }

        // Un disque de rayon 1 au bord irrégulier, à plat.
        static Mesh Disc
        {
            get
            {
                if (s_Disc != null)
                    return s_Disc;

                const int segments = 28;
                var vertices = new List<Vector3> { Vector3.zero };
                var triangles = new List<int>();
                var random = new System.Random(11);
                for (var i = 0; i < segments; i++)
                {
                    var angle = i * Mathf.PI * 2f / segments;
                    var radius = 0.85f + 0.15f * (float)random.NextDouble();
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
                }

                for (var i = 0; i < segments; i++)
                {
                    triangles.Add(0);
                    triangles.Add(1 + (i + 1) % segments);
                    triangles.Add(1 + i);
                }

                s_Disc = new Mesh { name = "Flaque" };
                s_Disc.SetVertices(vertices);
                s_Disc.SetTriangles(triangles, 0);
                s_Disc.RecalculateNormals();
                s_Disc.RecalculateBounds();
                return s_Disc;
            }
        }
    }
}
