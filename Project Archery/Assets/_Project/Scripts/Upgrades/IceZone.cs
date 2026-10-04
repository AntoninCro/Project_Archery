using Archery.Enemies;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Upgrades
{
    /// <summary>
    /// Zone de glace au sol (flèche de glace) : un disque translucide qui ralentit les ennemis au sol
    /// qui marchent dessus, puis fond au bout de quelques secondes.
    /// </summary>
    public class IceZone : MonoBehaviour
    {
        const int k_Segments = 40;
        const float k_TickInterval = 0.2f;

        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static Mesh s_Disc;
        static Material s_DefaultMaterial;

        MeshRenderer m_Renderer;
        MaterialPropertyBlock m_PropertyBlock;
        Color m_Color;
        float m_Radius;
        float m_Duration;
        float m_Slow;
        float m_Age;
        float m_TickTimer;

        // Disque de rayon 1 : centre un peu transparent, bord plus visible.
        static Mesh Disc
        {
            get
            {
                if (s_Disc != null)
                    return s_Disc;

                var vertices = new Vector3[1 + k_Segments * 2];
                var colors = new Color[vertices.Length];
                vertices[0] = Vector3.zero;
                colors[0] = new Color(1f, 1f, 1f, 0.3f);
                for (var i = 0; i < k_Segments; i++)
                {
                    var angle = i * Mathf.PI * 2f / k_Segments;
                    var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    vertices[1 + i] = direction * 0.8f;
                    colors[1 + i] = new Color(1f, 1f, 1f, 0.35f);
                    vertices[1 + k_Segments + i] = direction;
                    colors[1 + k_Segments + i] = new Color(1f, 1f, 1f, 0.75f);
                }

                var triangles = new int[k_Segments * 9];
                var t = 0;
                for (var i = 0; i < k_Segments; i++)
                {
                    var next = (i + 1) % k_Segments;
                    int inner = 1 + i, innerNext = 1 + next, outer = 1 + k_Segments + i, outerNext = 1 + k_Segments + next;
                    triangles[t++] = 0;
                    triangles[t++] = innerNext;
                    triangles[t++] = inner;
                    triangles[t++] = inner;
                    triangles[t++] = innerNext;
                    triangles[t++] = outerNext;
                    triangles[t++] = inner;
                    triangles[t++] = outerNext;
                    triangles[t++] = outer;
                }

                s_Disc = new Mesh
                {
                    name = "Ice Disc",
                    hideFlags = HideFlags.HideAndDontSave,
                    vertices = vertices,
                    colors = colors,
                    triangles = triangles,
                };
                s_Disc.RecalculateNormals();
                s_Disc.RecalculateBounds();
                return s_Disc;
            }
        }

        static Material DefaultMaterial
        {
            get
            {
                if (s_DefaultMaterial == null)
                {
                    var shader = Shader.Find("Archery/UnlitVertexColor");
                    if (shader == null)
                        shader = Shader.Find("Sprites/Default");
                    s_DefaultMaterial = new Material(shader) { name = "Ice (runtime)" };
                }

                return s_DefaultMaterial;
            }
        }

        /// <param name="material">Matériau au shader « Archery/UnlitVertexColor » ; vide = créé en jeu.</param>
        public static IceZone Spawn(Vector3 groundPoint, float radius, float duration, float slow, Color color, Material material = null)
        {
            var go = new GameObject("Ice Zone");
            go.transform.SetPositionAndRotation(groundPoint + Vector3.up * 0.03f, Quaternion.identity);
            go.transform.localScale = new Vector3(radius, 1f, radius);
            go.AddComponent<MeshFilter>().sharedMesh = Disc;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material != null ? material : DefaultMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var zone = go.AddComponent<IceZone>();
            zone.m_Renderer = renderer;
            zone.m_PropertyBlock = new MaterialPropertyBlock();
            zone.m_Color = color;
            zone.m_Radius = radius;
            zone.m_Duration = Mathf.Max(0.5f, duration);
            zone.m_Slow = slow;
            zone.ApplyAlpha(0f);
            return zone;
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            if (m_Age >= m_Duration)
            {
                Destroy(gameObject);
                return;
            }

            // Apparition rapide, puis la glace fond pendant la dernière seconde.
            ApplyAlpha(Mathf.Min(1f, m_Age / 0.15f) * Mathf.Clamp01(m_Duration - m_Age));

            m_TickTimer -= Time.deltaTime;
            if (m_TickTimer > 0f)
                return;

            m_TickTimer = k_TickInterval;
            SlowEnemiesInside();
        }

        // Le ralentissement dure un peu plus qu'un intervalle : il continue tant que l'ennemi reste sur la glace.
        void SlowEnemiesInside()
        {
            var center = transform.position;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null)
                    continue;

                var offset = enemy.transform.position - center;
                if (Mathf.Abs(offset.y) > 1f)
                    continue;

                offset.y = 0f;
                if (offset.sqrMagnitude <= m_Radius * m_Radius)
                    enemy.Slow(m_Slow, k_TickInterval * 1.5f);
            }
        }

        void ApplyAlpha(float alpha)
        {
            var color = m_Color;
            color.a *= alpha;
            m_Renderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor(k_BaseColorId, color);
            m_Renderer.SetPropertyBlock(m_PropertyBlock);
        }
    }
}
