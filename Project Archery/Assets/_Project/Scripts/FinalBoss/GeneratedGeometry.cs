using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Formes « low-poly » fabriquées par le code (grotte, piliers, rochers, cristaux, porte) : chaque triangle a ses
    /// propres sommets, pour des facettes nettes comme les kits de Kenney, et sa couleur dans les couleurs des sommets.
    /// </summary>
    public sealed class MeshBuilder
    {
        readonly List<Vector3> m_Vertices = new List<Vector3>();
        readonly List<Vector3> m_Normals = new List<Vector3>();
        readonly List<Color> m_Colors = new List<Color>();
        readonly List<Vector2> m_Uvs = new List<Vector2>();
        readonly List<int> m_Triangles = new List<int>();

        public int VertexCount => m_Vertices.Count;

        /// <summary>Un triangle à facette plate (sens horaire vu de l'extérieur, comme Unity).</summary>
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color, Vector2 uvA = default, Vector2 uvB = default, Vector2 uvC = default)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-12f)
                return;

            normal.Normalize();
            var start = m_Vertices.Count;
            m_Vertices.Add(a);
            m_Vertices.Add(b);
            m_Vertices.Add(c);
            for (var i = 0; i < 3; i++)
            {
                m_Normals.Add(normal);
                m_Colors.Add(color);
            }

            m_Uvs.Add(uvA);
            m_Uvs.Add(uvB);
            m_Uvs.Add(uvC);
            m_Triangles.Add(start);
            m_Triangles.Add(start + 1);
            m_Triangles.Add(start + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            Triangle(a, b, c, color, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
            Triangle(a, c, d, color, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f));
        }

        /// <summary>Un quadrilatère avec ses coordonnées de texture (lave qui coule).</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            Triangle(a, b, c, color, uvA, uvB, uvC);
            Triangle(a, c, d, color, uvA, uvC, uvD);
        }

        /// <summary>Une boîte (centre, demi-tailles, rotation).</summary>
        public void Box(Vector3 center, Vector3 halfSize, Quaternion rotation, Color color)
        {
            Vector3 P(float x, float y, float z) => center + rotation * Vector3.Scale(halfSize, new Vector3(x, y, z));
            var c = color;
            var shade = new Color(c.r * 0.85f, c.g * 0.85f, c.b * 0.85f, c.a);
            Quad(P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1), shade);     // devant (-z)
            Quad(P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1), shade);         // derrière
            Quad(P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1), c);         // gauche
            Quad(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1), c);             // droite
            Quad(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), c);             // dessus
            Quad(P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), shade);     // dessous
        }

        /// <summary>Un rocher : un icosaèdre déformé.</summary>
        public void Rock(Vector3 center, Vector3 size, Quaternion rotation, Color color, System.Random random, float roughness = 0.25f)
        {
            var t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var points = new[]
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (var i = 0; i < points.Length; i++)
            {
                var jitter = 1f + ((float)random.NextDouble() * 2f - 1f) * roughness;
                points[i] = center + rotation * Vector3.Scale(points[i].normalized * jitter, size);
            }

            int[] faces =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            for (var i = 0; i < faces.Length; i += 3)
            {
                var light = 0.85f + 0.15f * (float)random.NextDouble();
                Triangle(points[faces[i]], points[faces[i + 1]], points[faces[i + 2]], color * light);
            }
        }

        /// <summary>Un cristal : une bipyramide allongée le long de <paramref name="axis"/>.</summary>
        public void Crystal(Vector3 basePoint, Vector3 axis, float length, float width, int sides, Color color, float spin, float sparkle)
        {
            axis.Normalize();
            var side = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            var side2 = Vector3.Cross(axis, side);
            var tip = basePoint + axis * length;
            var root = basePoint - axis * (length * 0.18f);
            var middle = basePoint + axis * (length * 0.25f);
            var ring = new Vector3[sides];
            for (var i = 0; i < sides; i++)
            {
                var angle = spin + i * Mathf.PI * 2f / sides;
                ring[i] = middle + (side * Mathf.Cos(angle) + side2 * Mathf.Sin(angle)) * width;
            }

            // uv.x : phase du scintillement de ce cristal (lu par le shader « Archery/Crystal »).
            var uv = new Vector2(sparkle, 0f);
            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                var light = 0.75f + 0.25f * Mathf.Abs(Mathf.Cos(spin + i * 1.7f));
                Triangle(ring[next], tip, ring[i], color * light, uv, uv + Vector2.up, uv);
                Triangle(ring[i], root, ring[next], color * (light * 0.6f), uv, uv, uv);
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (m_Vertices.Count > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(m_Vertices);
            mesh.SetNormals(m_Normals);
            mesh.SetColors(m_Colors);
            mesh.SetUVs(0, m_Uvs);
            mesh.SetTriangles(m_Triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// <summary>
    /// Objets fabriqués par le code sous un composant (porte de l'antre, arène) : visibles dans l'éditeur, jamais
    /// enregistrés dans la scène, refaits à chaque ouverture et au lancement du jeu.
    /// </summary>
    public sealed class GeneratedRoot
    {
        readonly Transform m_Owner;
        readonly string m_Name;
        readonly List<Object> m_Owned = new List<Object>();
        GameObject m_Root;

        public GeneratedRoot(Transform owner, string name)
        {
            m_Owner = owner;
            m_Name = name;
        }

        public Transform Root => m_Root != null ? m_Root.transform : null;

        /// <summary>Hors du jeu (éditeur), les objets fabriqués ne sont pas enregistrés dans la scène.</summary>
        static HideFlags Flags => Application.isPlaying ? HideFlags.None : HideFlags.DontSave;

        public Transform Begin()
        {
            Clear();

            // Un reste d'une génération précédente (rechargement des scripts) : on le retire.
            for (var i = m_Owner.childCount - 1; i >= 0; i--)
            {
                var child = m_Owner.GetChild(i);
                if (child.name != m_Name)
                    continue;

                EditorSelection.ReleaseInside(child, m_Owner.gameObject);
                Destroy(child.gameObject);
            }

            m_Root = new GameObject(m_Name) { hideFlags = Flags };
            m_Root.transform.SetParent(m_Owner, false);
            return m_Root.transform;
        }

        public GameObject Create(string name, Transform parent)
        {
            var go = new GameObject(name) { hideFlags = Flags };
            go.transform.SetParent(parent != null ? parent : Root, false);
            return go;
        }

        /// <summary>Un objet avec ce maillage et ce matériau ; un collider en option (maillage, ou rien).</summary>
        public MeshRenderer CreateMesh(string name, Transform parent, Mesh mesh, Material material, bool collider, bool castShadows = false)
        {
            mesh.hideFlags = Flags;
            m_Owned.Add(mesh);
            var go = Create(name, parent);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (collider)
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return renderer;
        }

        /// <summary>Un matériau propre à cet objet (couleur réglée en jeu) : détruit avec le reste.</summary>
        public Material CloneMaterial(Material source, string name)
        {
            if (source == null)
                return null;

            var material = new Material(source) { name = name, hideFlags = Flags };
            m_Owned.Add(material);
            return material;
        }

        public void Clear()
        {
            if (m_Root != null)
            {
                EditorSelection.ReleaseInside(m_Root.transform, m_Owner != null ? m_Owner.gameObject : null);
                Destroy(m_Root);
            }

            m_Root = null;

            foreach (var owned in m_Owned)
            {
                if (owned != null)
                    Destroy(owned);
            }

            m_Owned.Clear();
        }

        static void Destroy(Object target)
        {
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }

    /// <summary>
    /// Dans l'éditeur : avant de détruire des objets (morceaux refabriqués, arène déchargée), on retire de la sélection
    /// ceux qui vont disparaître. Sinon l'Inspector reste sur un objet détruit et remplit la console d'erreurs
    /// (« MissingReferenceException … GameObjectInspector », « SerializedObjectNotCreatableException »).
    /// </summary>
    public static class EditorSelection
    {
        /// <summary>Si un objet sous <paramref name="root"/> est sélectionné, sélectionne <paramref name="fallback"/> à la place.</summary>
        public static void ReleaseInside(Transform root, GameObject fallback)
        {
#if UNITY_EDITOR
            if (root == null)
                return;

            foreach (var selected in UnityEditor.Selection.gameObjects)
            {
                if (selected != null && selected.transform.IsChildOf(root))
                {
                    UnityEditor.Selection.activeGameObject = fallback;
                    return;
                }
            }
#endif
        }

        /// <summary>Retire de la sélection tout ce qui appartient à cette scène (avant de la décharger).</summary>
        public static void ReleaseScene(UnityEngine.SceneManagement.Scene scene)
        {
#if UNITY_EDITOR
            foreach (var selected in UnityEditor.Selection.gameObjects)
            {
                if (selected != null && selected.scene == scene)
                {
                    UnityEditor.Selection.activeObject = null;
                    return;
                }
            }
#endif
        }
    }
}
