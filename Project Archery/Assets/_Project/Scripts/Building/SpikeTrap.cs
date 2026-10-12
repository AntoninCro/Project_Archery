using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Building
{
    /// <summary>
    /// Pièges à piques sur un socle de voie (GDD, section 9) : les ennemis au sol qui passent dessus perdent des PV
    /// à intervalles réguliers (12 / 18 / 27 toutes les 0,5 s à la vague 1 en Normal) ; au niveau 3, ils sont aussi ralentis.
    /// Les piques jaillissent du plateau à chaque coup. Les volants passent au-dessus.
    /// </summary>
    public class SpikeTrap : Structure
    {
        [Tooltip("Dégâts à chaque coup, par niveau, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 12f, 18f, 27f };

        [Tooltip("Temps (s) entre deux coups, par niveau : plus lents au début, ils gardent leur cadence au niveau 3.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 0.8f, 0.65f, 0.5f };

        [Tooltip("Niveau à partir duquel les ennemis sur le piège sont aussi ralentis.")]
        [SerializeField]
        int m_SlowFromLevel = 3;

        [Tooltip("Vitesse des ennemis ralentis (0,8 = 20 % plus lents).")]
        [Range(0.1f, 1f)]
        [SerializeField]
        float m_SlowMultiplier = 0.8f;

        [Tooltip("Taille (m) de la zone piégée, en X et en Z, centrée sur le bâtiment.")]
        [SerializeField]
        Vector2 m_Size = new Vector2(2.6f, 2.6f);

        [Tooltip("Hauteur (m) du dessus du plateau, d'où sortent les piques.")]
        [SerializeField]
        float m_PlateTop = 0.1f;

        [Tooltip("Matériau des piques (générées au lancement).")]
        [SerializeField]
        Material m_SpikeMaterial;

        [Tooltip("Nombre de piques par côté.")]
        [SerializeField]
        int m_SpikesPerSide = 5;

        [Tooltip("Hauteur (m) des piques, par niveau.")]
        [SerializeField]
        float[] m_SpikeHeightPerLevel = { 0.3f, 0.38f, 0.46f };

        [SerializeField]
        AudioClip m_StabClip;

        Transform m_Spikes;
        float m_Timer;
        float m_Pop;

        float Interval => ValueFor(m_IntervalPerLevel, Mathf.Max(1, Level), 0.5f);

        protected override void Awake()
        {
            BuildSpikes();
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            m_Timer -= Time.deltaTime;
            if (m_Timer <= 0f)
            {
                m_Timer = Interval;
                Strike();
            }

            // Les piques jaillissent d'un coup, puis rentrent doucement.
            m_Pop = Mathf.MoveTowards(m_Pop, 0f, Time.deltaTime * 2.5f);
            PlaceSpikes();
        }

        protected override void OnLevelChanged(int level)
        {
            base.OnLevelChanged(level);
            PlaceSpikes();
        }

        void Strike()
        {
            var damage = ValueFor(m_DamagePerLevel, Level, 12f) * Strength;
            var slow = Level >= m_SlowFromLevel;
            var any = false;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || !Contains(enemy.transform.position))
                    continue;

                if (slow)
                    enemy.Slow(m_SlowMultiplier, Interval * 1.5f);
                enemy.Health.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Zone = HitZone.Body,
                    Point = enemy.transform.position + Vector3.up * 0.3f,
                    Direction = Vector3.up,
                    Source = this,
                });
                any = true;
            }

            if (!any)
                return;

            m_Pop = 1f;
            Sfx.Play(m_StabClip, transform.position + Vector3.up * 0.3f, 0.8f, Random.Range(0.9f, 1.1f));
        }

        // Les ennemis au sol dans la zone (les volants sont bien plus haut).
        bool Contains(Vector3 position)
        {
            var local = Quaternion.Inverse(transform.rotation) * (position - transform.position);
            return Mathf.Abs(local.x) <= m_Size.x * 0.5f && Mathf.Abs(local.z) <= m_Size.y * 0.5f &&
                   local.y > -0.6f && local.y < 1.2f;
        }

        // Au repos, seules les pointes dépassent du plateau ; à chaque coup, les piques sortent entièrement.
        void PlaceSpikes()
        {
            if (m_Spikes == null)
                return;

            var height = ValueFor(m_SpikeHeightPerLevel, Mathf.Max(1, Level), 0.3f);
            var raised = Mathf.Lerp(0.3f, 1f, Mathf.SmoothStep(0f, 1f, m_Pop));
            m_Spikes.localScale = new Vector3(1f, height, 1f);
            m_Spikes.localPosition = new Vector3(0f, m_PlateTop - height * (1f - raised), 0f);
        }

        // Une grille de piques pyramidales, à facettes plates comme les modèles du kit.
        void BuildSpikes()
        {
            var go = new GameObject("Spikes");
            m_Spikes = go.transform;
            m_Spikes.SetParent(transform, false);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var count = Mathf.Max(1, m_SpikesPerSide);
            var step = new Vector2(m_Size.x / count, m_Size.y / count);
            var half = Mathf.Min(step.x, step.y) * 0.3f;
            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < count; j++)
                {
                    var center = new Vector3(-m_Size.x * 0.5f + step.x * (i + 0.5f), 0f, -m_Size.y * 0.5f + step.y * (j + 0.5f));
                    AddSpike(vertices, triangles, center, half);
                }
            }

            var mesh = new Mesh { name = "Spikes" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_SpikeMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            PlaceSpikes();
        }

        // Pyramide à base carrée, de hauteur 1 (mise à l'échelle par niveau).
        static void AddSpike(List<Vector3> vertices, List<int> triangles, Vector3 center, float half)
        {
            var tip = center + Vector3.up;
            var corners = new[]
            {
                center + new Vector3(-half, 0f, -half), center + new Vector3(-half, 0f, half),
                center + new Vector3(half, 0f, half), center + new Vector3(half, 0f, -half),
            };
            for (var k = 0; k < 4; k++)
            {
                var start = vertices.Count;
                // Ordre des sommets : la face regarde vers l'extérieur (sens horaire vu de dehors, comme Unity l'attend).
                vertices.Add(corners[(k + 1) % 4]);
                vertices.Add(tip);
                vertices.Add(corners[k]);
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.6f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(new Vector3(0f, 0.3f, 0f), new Vector3(m_Size.x, 0.6f, m_Size.y));
        }
    }
}
