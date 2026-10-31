using System.Collections.Generic;
using Archery.UI;
using TMPro;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Les flèches « DUO » (le joueur, 31 octobre) : entre deux tours qui peuvent fusionner, une grande flèche dorée en arc, haut
    /// au-dessus d'elles, les relie, avec le mot « DUO » (et le nom du duo) à son sommet. Seulement entre les vagues : pendant une
    /// vague, elles prendraient de la vue au joueur (et on ne fusionne pas).
    /// </summary>
    /// <remarks>
    /// Créé par <see cref="BuildManager"/> (rien à poser dans la scène). Les paires sont revues deux fois par seconde par
    /// <see cref="BuildManager.DuoOf"/> : une flèche par paire qui fusionne sans problème (les deux tours au niveau 3, finies).
    /// Le matériau est celui des hologrammes de pose (<see cref="BuildManager.BlueprintMaterial"/>), en or.
    /// </remarks>
    public class DuoHints : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_FilledColorId = Shader.PropertyToID("_FilledColor");
        static readonly int k_EdgeColorId = Shader.PropertyToID("_EdgeColor");

        const int k_Segments = 28;
        const int k_Sides = 10;

        [Tooltip("Temps (s) entre deux revues des paires de tours.")]
        [SerializeField]
        float m_RefreshInterval = 0.5f;

        [Tooltip("Hauteur (m) de l'arc au-dessus de la plus haute des deux tours (plus la part de leur écart ci-dessous).")]
        [SerializeField]
        float m_Height = 3.5f;

        [SerializeField]
        float m_HeightPerMeter = 0.25f;

        [Tooltip("Rayon (m) du corps de la flèche, et de ses deux pointes.")]
        [SerializeField]
        float m_Radius = 0.22f;

        [SerializeField]
        float m_HeadRadius = 0.75f;

        [SerializeField]
        float m_HeadLength = 1.6f;

        [SerializeField]
        Color m_Color = new Color(1f, 0.76f, 0.22f, 0.45f);

        [SerializeField]
        Color m_EdgeColor = new Color(1f, 0.9f, 0.5f, 1f);

        class Hint
        {
            public Structure A;
            public Structure B;
            public GameObject Root;
            public MeshFilter Filter;
            public Transform Label;
            public Vector3 From;
            public Vector3 To;
            public bool Seen;
        }

        readonly List<Hint> m_Hints = new List<Hint>();
        readonly List<Structure> m_Towers = new List<Structure>();
        Material m_Material;
        UiTheme m_Theme;
        float m_NextRefresh;
        bool m_Shown;

        void OnDisable() => Clear();

        void OnDestroy()
        {
            Clear();
            if (m_Material != null)
                Destroy(m_Material);
        }

        void Update()
        {
            var builds = BuildManager.Instance;
            var show = builds != null && builds.CanMove;
            if (!show)
            {
                if (m_Shown)
                    Clear();
                m_Shown = false;
                return;
            }

            m_Shown = true;
            if (Time.time >= m_NextRefresh)
            {
                m_NextRefresh = Time.time + m_RefreshInterval;
                Refresh(builds);
            }

            Animate();
        }

        // Les paires qui fusionnent : une flèche chacune ; celles qui ne fusionnent plus perdent la leur.
        void Refresh(BuildManager builds)
        {
            foreach (var hint in m_Hints)
                hint.Seen = false;

            m_Towers.Clear();
            foreach (var structure in Structure.All)
            {
                if (structure != null && structure.IsBuilt && !(structure is TowerDuo))
                    m_Towers.Add(structure);
            }

            for (var i = 0; i < m_Towers.Count; i++)
            {
                for (var j = i + 1; j < m_Towers.Count; j++)
                {
                    var entry = builds.DuoOf(m_Towers[i], m_Towers[j], out var problem);
                    if (entry == null || problem != null)
                        continue;

                    var hint = Find(m_Towers[i], m_Towers[j]) ?? Create(m_Towers[i], m_Towers[j], entry);
                    if (hint == null)
                        continue;
                    hint.Seen = true;
                    Place(hint);
                }
            }

            for (var i = m_Hints.Count - 1; i >= 0; i--)
            {
                if (m_Hints[i].Seen)
                    continue;
                Kill(m_Hints[i]);
                m_Hints.RemoveAt(i);
            }
        }

        Hint Find(Structure a, Structure b)
        {
            foreach (var hint in m_Hints)
            {
                if ((hint.A == a && hint.B == b) || (hint.A == b && hint.B == a))
                    return hint;
            }

            return null;
        }

        Hint Create(Structure a, Structure b, TowerDuoCatalog.Entry entry)
        {
            if (m_Material == null)
            {
                var builds = BuildManager.Instance;
                if (builds == null || builds.BlueprintMaterial == null)
                    return null;
                m_Material = new Material(builds.BlueprintMaterial) { name = "Flèche de duo" };
                m_Theme = UiTheme.Load();
            }

            var root = new GameObject("Flèche de duo (" + a.DisplayName + " + " + b.DisplayName + ")");
            root.transform.SetParent(transform, false);
            var filter = root.AddComponent<MeshFilter>();
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_Material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            filter.sharedMesh = new Mesh { name = "Flèche de duo" };

            var label = new GameObject("DUO").transform;
            label.SetParent(root.transform, false);
            AddText(label, "DUO", 26f, Vector3.zero, true);
            var title = entry != null && !string.IsNullOrEmpty(entry.displayName) ? entry.displayName : null;
            if (title != null)
                AddText(label, title, 10f, new Vector3(0f, -1.9f, 0f), false);

            var hint = new Hint { A = a, B = b, Root = root, Filter = filter, Label = label, From = Vector3.positiveInfinity };
            m_Hints.Add(hint);
            return hint;
        }

        void AddText(Transform parent, string content, float size, Vector3 at, bool title)
        {
            var go = new GameObject(content);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at;
            var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontStyle = FontStyles.Bold;
            if (m_Theme != null)
            {
                if (title)
                    UiThemer.ApplyTitle(text, m_Theme, false);
                else
                    UiThemer.ApplyLabel(text, m_Theme);
            }

            text.fontSize = size;
            var top = Color.Lerp(m_EdgeColor, Color.white, 0.5f);
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(top, top, m_EdgeColor, m_EdgeColor);
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(40, 22, 4, 230);
            text.sortingOrder = 1;
        }

        // L'arc d'une tour à l'autre, au-dessus de leurs sommets ; refait seulement si l'une a bougé.
        void Place(Hint hint)
        {
            var from = Top(hint.A);
            var to = Top(hint.B);
            if ((from - hint.From).sqrMagnitude < 0.01f && (to - hint.To).sqrMagnitude < 0.01f)
                return;

            hint.From = from;
            hint.To = to;
            var flat = to - from;
            flat.y = 0f;
            var apex = Mathf.Max(from.y, to.y) + m_Height + flat.magnitude * m_HeightPerMeter;
            var control = (from + to) * 0.5f;
            control.y = 2f * apex - 0.5f * (from.y + to.y);
            BuildArc(hint.Filter.sharedMesh, from, control, to);
            hint.Label.position = new Vector3((from.x + to.x) * 0.5f, apex + 2.4f, (from.z + to.z) * 0.5f);
        }

        // Le haut d'une tour : le sommet de ses pièces visibles, au-dessus de son centre.
        static Vector3 Top(Structure structure)
        {
            var bounds = new Bounds(structure.transform.position, Vector3.zero);
            foreach (var renderer in structure.GetComponentsInChildren<Renderer>())
            {
                if (renderer.enabled && renderer.gameObject.activeInHierarchy && !(renderer is ParticleSystemRenderer))
                    bounds.Encapsulate(renderer.bounds);
            }

            return new Vector3(structure.transform.position.x, bounds.max.y + 0.6f, structure.transform.position.z);
        }

        // Le texte face au joueur, qui flotte doucement ; l'or qui respire.
        void Animate()
        {
            var camera = Camera.main;
            var bob = Mathf.Sin(Time.time * 2f) * 0.25f;
            foreach (var hint in m_Hints)
            {
                if (hint.Label == null)
                    continue;
                if (camera != null)
                {
                    var look = hint.Label.position - camera.transform.position;
                    look.y = 0f;
                    if (look.sqrMagnitude > 1e-4f)
                        hint.Label.rotation = Quaternion.LookRotation(look);
                }

                hint.Label.GetChild(0).localPosition = new Vector3(0f, bob, 0f);
            }

            if (m_Material == null)
                return;
            var pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 3f);
            var color = m_Color;
            color.a *= pulse;
            m_Material.SetColor(k_BaseColorId, color);
            m_Material.SetColor(k_FilledColorId, color);
            m_Material.SetColor(k_EdgeColorId, m_EdgeColor * (0.8f + 0.4f * pulse));
        }

        // Un tube le long de la courbe (Bézier quadratique), une pointe à chaque bout, tournée vers sa tour.
        void BuildArc(Mesh mesh, Vector3 a, Vector3 control, Vector3 b)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<int>();

            // Le corps, entre les deux pointes.
            var start = m_HeadLength / Mathf.Max(1f, Vector3.Distance(a, b) * 1.3f);
            for (var s = 0; s <= k_Segments; s++)
            {
                var t = Mathf.Lerp(start, 1f - start, s / (float)k_Segments);
                Frame(a, control, b, t, out var point, out var tangent, out var side, out var up);
                for (var k = 0; k <= k_Sides; k++)
                {
                    var angle = k * Mathf.PI * 2f / k_Sides;
                    var normal = side * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                    vertices.Add(transform.InverseTransformPoint(point + normal * m_Radius));
                    normals.Add(transform.InverseTransformDirection(normal));
                }
            }

            for (var s = 0; s < k_Segments; s++)
            {
                for (var k = 0; k < k_Sides; k++)
                {
                    var i = s * (k_Sides + 1) + k;
                    var n = i + k_Sides + 1;
                    triangles.Add(i);
                    triangles.Add(i + 1);
                    triangles.Add(n);
                    triangles.Add(i + 1);
                    triangles.Add(n + 1);
                    triangles.Add(n);
                }
            }

            Head(vertices, normals, triangles, a, control, b, start, false);
            Head(vertices, normals, triangles, a, control, b, 1f - start, true);

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        // Une pointe en cône, de la fin du corps jusqu'au bout de la courbe.
        void Head(List<Vector3> vertices, List<Vector3> normals, List<int> triangles, Vector3 a, Vector3 control, Vector3 b,
                  float t, bool atEnd)
        {
            Frame(a, control, b, t, out var basePoint, out _, out var side, out var up);
            var tip = atEnd ? b : a;
            var axis = (tip - basePoint).normalized;
            var first = vertices.Count;
            for (var k = 0; k <= k_Sides; k++)
            {
                var angle = k * Mathf.PI * 2f / k_Sides;
                var radial = side * Mathf.Cos(angle) + up * Mathf.Sin(angle);
                vertices.Add(transform.InverseTransformPoint(basePoint + radial * m_HeadRadius));
                normals.Add(transform.InverseTransformDirection((radial + axis * 0.45f).normalized));
            }

            var tipIndex = vertices.Count;
            vertices.Add(transform.InverseTransformPoint(tip));
            normals.Add(transform.InverseTransformDirection(axis));
            var center = vertices.Count;
            vertices.Add(transform.InverseTransformPoint(basePoint));
            normals.Add(transform.InverseTransformDirection(-axis));
            for (var k = 0; k < k_Sides; k++)
            {
                // Le cône, et son fond (vu de dos, le matériau d'hologramme n'a qu'une face).
                triangles.Add(first + k);
                triangles.Add(atEnd ? first + k + 1 : tipIndex);
                triangles.Add(atEnd ? tipIndex : first + k + 1);
                triangles.Add(first + k);
                triangles.Add(atEnd ? center : first + k + 1);
                triangles.Add(atEnd ? first + k + 1 : center);
            }
        }

        static void Frame(Vector3 a, Vector3 control, Vector3 b, float t, out Vector3 point, out Vector3 tangent, out Vector3 side,
                          out Vector3 up)
        {
            var u = 1f - t;
            point = u * u * a + 2f * u * t * control + t * t * b;
            tangent = (2f * u * (control - a) + 2f * t * (b - control)).normalized;
            side = Vector3.Cross(Vector3.up, tangent);
            if (side.sqrMagnitude < 1e-4f)
                side = Vector3.right;
            side.Normalize();
            up = Vector3.Cross(tangent, side).normalized;
        }

        void Clear()
        {
            foreach (var hint in m_Hints)
                Kill(hint);
            m_Hints.Clear();
        }

        static void Kill(Hint hint)
        {
            if (hint.Filter != null && hint.Filter.sharedMesh != null)
                Destroy(hint.Filter.sharedMesh);
            if (hint.Root != null)
                Destroy(hint.Root);
        }
    }
}
