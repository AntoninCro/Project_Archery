using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Combat
{
    /// <summary>
    /// Le symbole à tracer au katana (22 octobre), devant le joueur : chaque trait est un coup de pinceau lumineux, avec une
    /// lueur qui le parcourt dans le sens où le tracer ; un trait réussi disparaît (la fissure le remplace). Le symbole est
    /// dans le plan XY de cet objet (Z vers l'avant, à l'opposé du joueur), dans un carré de 2 × <see cref="HalfSize"/>.
    /// </summary>
    /// <remarks>Créé et placé par <see cref="KatanaGauntlet"/>.</remarks>
    [DisallowMultipleComponent]
    public class KanjiGuide : MonoBehaviour
    {
        /// <summary>Demi-côté (m) du carré du symbole (0,26 jusqu'au 23 octobre : trop petit).</summary>
        public const float HalfSize = 0.38f;

        static readonly int k_PulseId = Shader.PropertyToID("_Pulse");
        static readonly int k_ColorId = Shader.PropertyToID("_Color");

        // Largeur (m) du pinceau : appuyé au début du trait, plus fin au milieu, effilé à la fin.
        const float k_StartWidth = 0.046f;
        const float k_MiddleWidth = 0.035f;
        const float k_EndWidth = 0.011f;
        const float k_PulsePeriod = 1.3f;

        // Un geste à côté : le symbole clignote en rouge un instant.
        const float k_MissTime = 0.3f;
        static readonly Color k_MissColor = new Color(2.4f, 0.35f, 0.3f, 0.75f);

        readonly List<MeshRenderer> m_Renderers = new List<MeshRenderer>();
        readonly List<Mesh> m_Meshes = new List<Mesh>();
        readonly List<bool> m_Done = new List<bool>();
        MaterialPropertyBlock m_Block;
        Material m_Material;
        Color m_InkColor = Color.white;
        float m_MissTime = -1f;
        List<Vector2>[] m_Strokes = System.Array.Empty<List<Vector2>>();
        int m_Glyph = -1;
        bool m_Visible = true;

        public int Glyph => m_Glyph;
        public IReadOnlyList<Vector2>[] Strokes => m_Strokes;
        public int StrokeCount => m_Strokes.Length;

        public int Remaining
        {
            get
            {
                var count = 0;
                foreach (var done in m_Done)
                {
                    if (!done)
                        count++;
                }

                return count;
            }
        }

        public static KanjiGuide Create(Transform parent, Material material)
        {
            var guide = new GameObject("Symbole à tracer").AddComponent<KanjiGuide>();
            guide.transform.SetParent(parent, false);
            guide.m_Material = material;
            guide.m_Block = new MaterialPropertyBlock();
            if (material != null && material.HasProperty(k_ColorId))
                guide.m_InkColor = material.GetColor(k_ColorId);
            return guide;
        }

        void OnDestroy()
        {
            foreach (var mesh in m_Meshes)
            {
                if (mesh != null)
                    Destroy(mesh);
            }
        }

        public bool IsDone(int stroke) => stroke >= 0 && stroke < m_Done.Count && m_Done[stroke];

        /// <summary>Un nouveau symbole, tous ses traits à tracer.</summary>
        public void Show(int glyph)
        {
            m_Glyph = glyph;
            m_Strokes = KanjiGlyphs.Strokes(glyph);
            m_Done.Clear();
            for (var s = 0; s < m_Strokes.Length; s++)
            {
                m_Done.Add(false);
                if (s >= m_Renderers.Count)
                    AddRenderer();
                BuildStroke(m_Meshes[s], m_Strokes[s]);
            }

            for (var s = 0; s < m_Renderers.Count; s++)
                m_Renderers[s].enabled = m_Visible && s < m_Strokes.Length;
        }

        /// <summary>Ce trait est fait : il disparaît.</summary>
        public void MarkDone(int stroke)
        {
            if (stroke < 0 || stroke >= m_Done.Count)
                return;
            m_Done[stroke] = true;
            m_Renderers[stroke].enabled = false;
        }

        /// <summary>Un geste à côté du symbole : il clignote en rouge.</summary>
        public void Miss() => m_MissTime = Time.time;

        public void SetVisible(bool visible)
        {
            m_Visible = visible;
            for (var s = 0; s < m_Renderers.Count; s++)
                m_Renderers[s].enabled = visible && s < m_Strokes.Length && !m_Done[s];
        }

        /// <summary>Un point du monde, dans le carré du symbole (-1 à 1 sur ses bords).</summary>
        public Vector2 ToGlyph(Vector3 world)
        {
            var local = transform.InverseTransformPoint(world);
            return new Vector2(local.x, local.y) / HalfSize;
        }

        /// <summary>Distance signée (m) d'un point du monde au plan du symbole (positive devant, à l'opposé du joueur).</summary>
        public float PlaneDistance(Vector3 world) => Vector3.Dot(world - transform.position, transform.forward);

        void Update()
        {
            // La lueur parcourt chaque trait restant, du début à la fin, un trait après l'autre.
            if (!m_Visible)
                return;
            var phase = Time.time / k_PulsePeriod;
            var miss = m_MissTime >= 0f ? Mathf.Clamp01(1f - (Time.time - m_MissTime) / k_MissTime) : 0f;
            var ink = Color.Lerp(m_InkColor, k_MissColor, miss);
            for (var s = 0; s < m_Strokes.Length && s < m_Renderers.Count; s++)
            {
                if (m_Done[s])
                    continue;
                m_Block.SetFloat(k_PulseId, Mathf.Repeat(phase + s * 0.35f, 1.25f) - 0.1f);
                m_Block.SetColor(k_ColorId, ink);
                m_Renderers[s].SetPropertyBlock(m_Block);
            }
        }

        void AddRenderer()
        {
            var go = new GameObject("Trait " + (m_Renderers.Count + 1));
            go.transform.SetParent(transform, false);
            var mesh = new Mesh { name = "Trait du symbole" };
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            m_Meshes.Add(mesh);
            m_Renderers.Add(renderer);
        }

        // Un ruban le long du trait (dans le plan XY), sa largeur variant comme un coup de pinceau.
        static void BuildStroke(Mesh mesh, List<Vector2> stroke)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var total = KanjiGlyphs.Length(stroke);
            var along = 0f;
            for (var i = 0; i < stroke.Count; i++)
            {
                if (i > 0)
                    along += Vector2.Distance(stroke[i - 1], stroke[i]);
                var t = total > 1e-5f ? along / total : 0f;
                var a = stroke[Mathf.Max(0, i - 1)];
                var b = stroke[Mathf.Min(stroke.Count - 1, i + 1)];
                var tangent = (b - a).sqrMagnitude > 1e-10f ? (b - a).normalized : Vector2.right;
                var normal = new Vector2(-tangent.y, tangent.x);
                var width = t < 0.15f
                    ? Mathf.Lerp(k_StartWidth * 0.8f, k_StartWidth, t / 0.15f)
                    : Mathf.Lerp(k_MiddleWidth, k_EndWidth, Mathf.SmoothStep(0f, 1f, (t - 0.15f) / 0.85f));
                if (t < 0.15f)
                    width = Mathf.Max(width, k_MiddleWidth);
                var center = stroke[i] * HalfSize;
                vertices.Add(center + normal * width);
                vertices.Add(center - normal * width);
                uvs.Add(new Vector2(t, -1f));
                uvs.Add(new Vector2(t, 1f));
                colors.Add(Color.white);
                colors.Add(Color.white);
                if (i == 0)
                    continue;
                var k = (i - 1) * 2;
                triangles.Add(k);
                triangles.Add(k + 2);
                triangles.Add(k + 1);
                triangles.Add(k + 1);
                triangles.Add(k + 2);
                triangles.Add(k + 3);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}
