using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.World
{
    /// <summary>
    /// L'eau de la rivière, construite au lancement d'après ses pièces : une surface d'eau dans chaque case du lit
    /// (pièces « ground_path… » du groupe de la rivière), une nappe d'eau animée sur chaque cascade
    /// (pièces « cliff_waterfall… »), et de l'écume à leur pied.
    /// </summary>
    /// <remarks>
    /// Le sens du courant est calculé tout seul : l'eau part de la source (le bout du lit le plus haut, le plus loin des
    /// sorties) et s'en éloigne, en descendant les cascades et en se partageant aux embranchements.
    /// Modifier le tracé de la rivière suffit : l'eau suit au prochain lancement.
    /// </remarks>
    [DisallowMultipleComponent]
    public class RiverWater : MonoBehaviour
    {
        const float k_Cell = 3f;

        [Tooltip("Groupe qui contient les pièces du lit (ground_path…) et les cascades (cliff_waterfall…).")]
        [SerializeField]
        Transform m_RiverRoot;

        [Tooltip("Matériau de la rivière (shader Archery/Water).")]
        [SerializeField]
        Material m_WaterMaterial;

        [Tooltip("Matériau des cascades (shader Archery/Water, Cascade à 1).")]
        [SerializeField]
        Material m_FallMaterial;

        [Tooltip("Matériau de l'écume (shader Archery/UnlitVertexColor).")]
        [SerializeField]
        Material m_FoamMaterial;

        [Tooltip("Hauteur de l'eau sous le haut des berges (m). Le lit est 37,5 cm sous les berges.")]
        [SerializeField]
        float m_DepthBelowBank = 0.2f;

        [Tooltip("Vitesse du courant (multiplie celle du matériau).")]
        [SerializeField]
        float m_FlowSpeed = 1f;

        [Tooltip("Découpage de chaque case en carrés (pour les vagues).")]
        [Range(1, 6)]
        [SerializeField]
        int m_Subdivisions = 3;

        [Tooltip("Écume qui jaillit au pied des cascades.")]
        [SerializeField]
        bool m_FallFoam = true;

        [Tooltip("Optionnel : son en boucle au pied de chaque cascade.")]
        [SerializeField]
        AudioClip m_FallSound;

        [Range(0f, 1f)]
        [SerializeField]
        float m_FallVolume = 0.25f;

        sealed class Node
        {
            public Vector2Int cell;
            public float level;
            public string half;
            public readonly List<Node> sameLevel = new List<Node>();
            public readonly List<Node> below = new List<Node>();
            public readonly List<Node> above = new List<Node>();
            public int outlet = -1;
            public int distance = -1;
            public Vector2 flow;
        }

        readonly List<Transform> m_Falls = new List<Transform>();

        /// <summary>Les cascades de la rivière (pièces « cliff_waterfall… »), trouvées au lancement (hors du jeu : tout de suite).</summary>
        public IReadOnlyList<Transform> Falls
        {
            get
            {
                if (!Application.isPlaying && m_Falls.Count == 0 && m_RiverRoot != null)
                {
                    foreach (var t in m_RiverRoot.GetComponentsInChildren<Transform>())
                    {
                        if (t.name.StartsWith("cliff_waterfall"))
                            m_Falls.Add(t);
                    }
                }

                return m_Falls;
            }
        }

        void Start()
        {
            if (m_RiverRoot == null)
            {
                Debug.LogWarning("RiverWater : il manque le groupe de la rivière.", this);
                return;
            }

            var nodes = CollectPieces();
            if (nodes.Count == 0)
                return;

            Link(nodes);
            ComputeFlow(nodes);
            BuildSurface(nodes);
            foreach (var fall in m_Falls)
                BuildFall(fall, nodes);
        }

        static bool IsBed(Transform t) => t.name.StartsWith("ground_path") || t.name.StartsWith("ground_river");

        List<Node> CollectPieces()
        {
            // (La liste a pu être remplie dans l'éditeur, par Falls.)
            m_Falls.Clear();
            var byKey = new Dictionary<Vector3Int, Node>();
            foreach (var t in m_RiverRoot.GetComponentsInChildren<Transform>())
            {
                if (t.name.StartsWith("cliff_waterfall"))
                {
                    m_Falls.Add(t);
                    continue;
                }

                if (!IsBed(t))
                    continue;

                var p = t.position;
                var cell = new Vector2Int(Mathf.FloorToInt(p.x / k_Cell), Mathf.FloorToInt(p.z / k_Cell));
                var key = new Vector3Int(cell.x, cell.y, Mathf.RoundToInt(p.y * 10f));
                var halfAt = t.name.IndexOf("_half");
                var half = halfAt >= 0 && t.name.Length >= halfAt + 7 ? WorldHalf(t.name.Substring(halfAt + 5, 2), t.rotation) : null;
                if (byKey.TryGetValue(key, out var existing))
                {
                    // Une pièce entière l'emporte sur une demi-pièce dans la même case.
                    if (half == null)
                        existing.half = null;
                    continue;
                }

                byKey.Add(key, new Node { cell = cell, level = p.y, half = half });
            }

            return new List<Node>(byKey.Values);
        }

        // Moitié gardée d'une pièce coupée en diagonale, dans le repère du monde (« NW », « SE »…) :
        // le nom du modèle la donne dans le repère de la pièce, qui peut être tournée.
        static string WorldHalf(string half, Quaternion rotation)
        {
            var kept = new Vector3(half[1] == 'E' ? 1f : -1f, 0f, half[0] == 'N' ? 1f : -1f);
            var world = rotation * kept;
            return (world.z >= 0f ? "N" : "S") + (world.x >= 0f ? "E" : "W");
        }

        static bool Adjacent(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

        // Une demi-case ne touche que deux côtés de sa case (une moitié nord-ouest : le nord et l'ouest).
        static bool Touches(Node node, Vector2Int side)
        {
            if (node.half == null)
                return true;
            return (side.y > 0 && node.half[0] == 'N') || (side.y < 0 && node.half[0] == 'S') ||
                   (side.x > 0 && node.half[1] == 'E') || (side.x < 0 && node.half[1] == 'W');
        }

        static void Link(List<Node> nodes)
        {
            foreach (var a in nodes)
            {
                foreach (var b in nodes)
                {
                    if (a == b)
                        continue;

                    var close = Adjacent(a.cell, b.cell) || a.cell == b.cell;
                    if (!close)
                        continue;

                    if (a.cell != b.cell && (!Touches(a, b.cell - a.cell) || !Touches(b, a.cell - b.cell)))
                        continue;

                    if (Mathf.Abs(a.level - b.level) < 0.3f)
                    {
                        if (a.cell != b.cell)
                            a.sameLevel.Add(b);
                    }
                    else if (b.level < a.level)
                    {
                        // Une marche entre deux niveaux du lit : c'est une cascade.
                        a.below.Add(b);
                        b.above.Add(a);
                    }
                }
            }
        }

        void ComputeFlow(List<Node> nodes)
        {
            // 1. Distance aux sorties : bouts du lit au niveau le plus bas, sans cascade au-dessus.
            var lowest = float.MaxValue;
            var highest = float.MinValue;
            foreach (var n in nodes)
            {
                lowest = Mathf.Min(lowest, n.level);
                highest = Mathf.Max(highest, n.level);
            }

            var queue = new Queue<Node>();
            foreach (var n in nodes)
            {
                if (n.level < lowest + 0.3f && n.sameLevel.Count <= 1 && n.above.Count == 0)
                {
                    n.outlet = 0;
                    queue.Enqueue(n);
                }
            }

            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                foreach (var up in n.sameLevel)
                    VisitOutlet(up, n.outlet + 1, queue);
                foreach (var up in n.above)
                    VisitOutlet(up, n.outlet + 1, queue);
            }

            // 2. Sources : au niveau le plus haut, sans cascade au-dessus, et le plus loin possible des sorties.
            foreach (var n in nodes)
            {
                if (n.level < highest - 0.3f || n.above.Count > 0)
                    continue;

                var farthest = true;
                foreach (var other in n.sameLevel)
                    farthest &= other.outlet <= n.outlet;
                if (farthest)
                    Visit(n, 0, queue);
            }

            // 3. On descend le courant depuis les sources : le long du lit, puis en bas des cascades.
            // Un bras qu'aucune source n'atteint part de sa case la plus haute.
            while (true)
            {
                while (queue.Count > 0)
                {
                    var n = queue.Dequeue();
                    foreach (var next in n.sameLevel)
                        Visit(next, n.distance + 1, queue);
                    foreach (var next in n.below)
                        Visit(next, n.distance + 1, queue);
                }

                Node start = null;
                foreach (var n in nodes)
                {
                    if (n.distance < 0 && (start == null || n.level > start.level + 0.3f ||
                                           (Mathf.Abs(n.level - start.level) < 0.3f && n.outlet > start.outlet)))
                        start = n;
                }

                if (start == null)
                    break;
                Visit(start, 0, queue);
            }

            // 4. Chaque case coule vers ses voisines plus loin de la source (elle se partage aux embranchements).
            foreach (var n in nodes)
            {
                var sum = Vector2.zero;
                foreach (var next in n.sameLevel)
                {
                    if (next.distance > n.distance)
                        sum += (Vector2)(next.cell - n.cell);
                }

                foreach (var next in n.below)
                {
                    if (next.distance > n.distance)
                        sum += next.cell != n.cell ? (Vector2)(next.cell - n.cell) : FallDirection(n);
                }

                if (sum.sqrMagnitude < 0.0001f)
                {
                    // Bout du lit (sortie) : le courant continue dans le sens d'où il vient.
                    foreach (var previous in n.sameLevel)
                    {
                        if (previous.distance < n.distance)
                            sum += (Vector2)(n.cell - previous.cell);
                    }

                    foreach (var previous in n.above)
                        sum += previous.cell != n.cell ? (Vector2)(n.cell - previous.cell) : FallDirection(previous);
                }

                n.flow = sum.sqrMagnitude > 0.0001f ? sum.normalized : Vector2.zero;
            }
        }

        static void VisitOutlet(Node node, int distance, Queue<Node> queue)
        {
            if (node.outlet >= 0)
                return;
            node.outlet = distance;
            queue.Enqueue(node);
        }

        static void Visit(Node node, int distance, Queue<Node> queue)
        {
            if (node.distance >= 0)
                return;
            node.distance = distance;
            queue.Enqueue(node);
        }

        // Sens de la cascade la plus proche (vers le bas de la falaise).
        Vector2 FallDirection(Node node)
        {
            var center = new Vector3((node.cell.x + 0.5f) * k_Cell, node.level, (node.cell.y + 0.5f) * k_Cell);
            Transform best = null;
            var bestDistance = float.MaxValue;
            foreach (var fall in m_Falls)
            {
                var d = (fall.position - center).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = fall;
                }
            }

            if (best == null)
                return Vector2.zero;
            var forward = best.forward;
            return new Vector2(forward.x, forward.z).normalized;
        }

        // Une surface plane par case (découpée pour les vagues), sommets partagés entre cases voisines au même niveau :
        // le sens du courant y est la moyenne des cases qui se touchent.
        void BuildSurface(List<Node> nodes)
        {
            var n = Mathf.Max(1, m_Subdivisions);
            var step = k_Cell / n;
            var index = new Dictionary<Vector3Int, int>();
            var positions = new List<Vector3>();
            var flows = new List<Vector2>();
            var counts = new List<int>();
            var triangles = new List<int>();

            int Vertex(int gx, int gz, Node node)
            {
                var key = new Vector3Int(gx, gz, Mathf.RoundToInt(node.level * 10f));
                if (!index.TryGetValue(key, out var i))
                {
                    i = positions.Count;
                    index.Add(key, i);
                    positions.Add(new Vector3(gx * step, node.level - m_DepthBelowBank, gz * step));
                    flows.Add(Vector2.zero);
                    counts.Add(0);
                }

                flows[i] += node.flow;
                counts[i]++;
                return i;
            }

            foreach (var node in nodes)
            {
                var diagonalUp = node.half == "SW" || node.half == "NE";
                var center = new Vector3((node.cell.x + 0.5f) * k_Cell, 0f, (node.cell.y + 0.5f) * k_Cell);
                for (var a = 0; a < n; a++)
                {
                    for (var b = 0; b < n; b++)
                    {
                        var gx = node.cell.x * n + a;
                        var gz = node.cell.y * n + b;
                        // a=(x0,z0) b=(x0,z1) c=(x1,z1) d=(x1,z0), face vers le haut
                        var quad = diagonalUp
                            ? new[] { new Vector2Int(gx, gz), new Vector2Int(gx, gz + 1), new Vector2Int(gx + 1, gz),
                                      new Vector2Int(gx, gz + 1), new Vector2Int(gx + 1, gz + 1), new Vector2Int(gx + 1, gz) }
                            : new[] { new Vector2Int(gx, gz), new Vector2Int(gx, gz + 1), new Vector2Int(gx + 1, gz + 1),
                                      new Vector2Int(gx, gz), new Vector2Int(gx + 1, gz + 1), new Vector2Int(gx + 1, gz) };
                        for (var t = 0; t < 6; t += 3)
                        {
                            if (node.half != null)
                            {
                                var centroid = (Vector2)(quad[t] + quad[t + 1] + quad[t + 2]) * (step / 3f);
                                if (!KeepsHalf(node.half, centroid.x - center.x, centroid.y - center.z))
                                    continue;
                            }

                            for (var k = 0; k < 3; k++)
                                triangles.Add(Vertex(quad[t + k].x, quad[t + k].y, node));
                        }
                    }
                }
            }

            var local3 = new Vector3[positions.Count];
            var uv0 = new Vector2[positions.Count];
            var uv1 = new Vector2[positions.Count];
            for (var i = 0; i < positions.Count; i++)
            {
                local3[i] = transform.InverseTransformPoint(positions[i]);
                uv0[i] = new Vector2(positions[i].x, positions[i].z);
                var f = counts[i] > 0 ? flows[i] / counts[i] : Vector2.zero;
                uv1[i] = f * m_FlowSpeed;
            }

            var mesh = new Mesh { name = "River Water" };
            mesh.SetVertices(local3);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            bounds.Expand(new Vector3(0f, 0.5f, 0f));
            mesh.bounds = bounds;

            var surface = CreateRenderer("River Water Surface", mesh, m_WaterMaterial);
            surface.transform.SetParent(transform, false);
        }

        // Moitiés des cases coupées en diagonale (repère du monde, depuis le centre de la case).
        static bool KeepsHalf(string half, float x, float z)
        {
            switch (half)
            {
                case "NW": return z - x >= 0f;
                case "SE": return x - z >= 0f;
                case "SW": return x + z <= 0f;
                case "NE": return x + z >= 0f;
                default: return true;
            }
        }

        // Nappe d'eau devant celle de la pièce du kit : un rebord qui déborde en haut, puis la chute.
        // Le rebord part de la surface de l'eau du lit d'en haut, quelle que soit la hauteur de la pièce.
        void BuildFall(Transform fall, List<Node> nodes)
        {
            // Repère de la pièce, en mètres pour une échelle 1 (case de 3 m) ; la nappe du kit est en z = -1,1.
            var profile = new[]
            {
                new Vector2(3f, -1.41f), new Vector2(2.91f, -1.08f), new Vector2(2.7f, -1.02f),
                new Vector2(1.8f, -1.035f), new Vector2(0.9f, -1.035f), new Vector2(0f, -1.02f),
            };
            const int columns = 4;
            const float halfWidth = 0.81f;
            var bottom = fall.position.y;
            var top = UpperWaterLevel(fall, nodes, halfWidth) ?? fall.TransformPoint(new Vector3(0f, 3f, 0f)).y;
            Vector3 Point(float x, Vector2 p)
            {
                var world = fall.TransformPoint(new Vector3(x, 0f, p.y));
                world.y = Mathf.LerpUnclamped(bottom, top, p.x / 3f);
                return world;
            }

            var vertices = new List<Vector3>();
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector2>();
            var triangles = new List<int>();
            var along = 0f;
            for (var r = 0; r < profile.Length; r++)
            {
                if (r > 0)
                    along += Vector3.Distance(Point(0f, profile[r - 1]), Point(0f, profile[r]));

                for (var c = 0; c <= columns; c++)
                {
                    var x = Mathf.Lerp(-halfWidth, halfWidth, c / (float)columns);
                    var world = Point(x, profile[r]);
                    vertices.Add(transform.InverseTransformPoint(world));
                    uv0.Add(new Vector2(x * fall.lossyScale.x, along));
                    uv1.Add(new Vector2(0f, m_FlowSpeed));
                }
            }

            for (var r = 0; r < profile.Length - 1; r++)
            {
                for (var c = 0; c < columns; c++)
                {
                    var i = r * (columns + 1) + c;
                    triangles.Add(i);
                    triangles.Add(i + columns + 1);
                    triangles.Add(i + 1);
                    triangles.Add(i + 1);
                    triangles.Add(i + columns + 1);
                    triangles.Add(i + columns + 2);
                }
            }

            var mesh = new Mesh { name = "Waterfall " + fall.name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var sheet = CreateRenderer("Waterfall Sheet", mesh, m_FallMaterial);
            sheet.transform.SetParent(transform, false);

            var foot = fall.TransformPoint(new Vector3(0f, 0.1f, -0.85f));
            var width = halfWidth * 2f * fall.lossyScale.x;
            if (m_FallFoam && m_FoamMaterial != null)
            {
                var outward = fall.forward;
                CreateFoam("Waterfall Foam", foot, outward, width, 26f * width, new Vector2(0.06f, 0.16f), new Vector2(1f, 2.2f), 1f);
                CreateFoam("Waterfall Spray", foot + Vector3.up * 0.2f, outward, width, 6f * width, new Vector2(0.25f, 0.5f), new Vector2(0.3f, 0.8f), 0.35f);
            }

            if (m_FallSound != null)
            {
                var sound = new GameObject("Waterfall Sound");
                sound.transform.SetParent(transform, false);
                sound.transform.position = foot;
                var source = sound.AddComponent<AudioSource>();
                source.clip = m_FallSound;
                source.loop = true;
                source.spatialBlend = 1f;
                source.volume = m_FallVolume;
                source.minDistance = 2f;
                source.maxDistance = 30f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.time = Random.Range(0f, m_FallSound.length);
                source.Play();
            }
        }

        // Surface de l'eau du lit juste derrière le rebord de la cascade (null s'il n'y a pas de lit plus haut).
        float? UpperWaterLevel(Transform fall, List<Node> nodes, float halfWidth)
        {
            float? level = null;
            for (var side = -1; side <= 1; side++)
            {
                var behind = fall.TransformPoint(new Vector3(side * halfWidth, 3f, -1.8f));
                var cell = new Vector2Int(Mathf.FloorToInt(behind.x / k_Cell), Mathf.FloorToInt(behind.z / k_Cell));
                foreach (var node in nodes)
                {
                    if (node.cell == cell && node.level > fall.position.y + 0.5f && (level == null || node.level > level.Value))
                        level = node.level;
                }
            }

            return level - m_DepthBelowBank;
        }

        GameObject CreateRenderer(string objectName, Mesh mesh, Material material)
        {
            var go = new GameObject(objectName);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        // Éclats d'écume low-poly (petits octaèdres blancs) qui jaillissent du pied de la cascade et retombent.
        void CreateFoam(string objectName, Vector3 position, Vector3 outward, float width, float rate,
            Vector2 size, Vector2 speed, float alpha)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            var up = Vector3.ProjectOnPlane(outward, Vector3.up).normalized * 0.6f + Vector3.up;
            go.transform.rotation = Quaternion.LookRotation(up.normalized, -outward);

            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(1f, 1f, 1f, alpha);
            main.gravityModifier = 1.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;

            var emission = particles.emission;
            emission.rateOverTime = rate;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.1f;
            shape.scale = new Vector3(Mathf.Max(0.1f, width * 4f), 1f, 1f);

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var color = particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Octahedron();
            renderer.sharedMaterial = m_FoamMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            particles.Play();
        }

        static Mesh s_Octahedron;

        // Aussi utilisé pour les éclats des gisements de cristaux.
        internal static Mesh Octahedron()
        {
            if (s_Octahedron != null)
                return s_Octahedron;

            var v = new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            var faces = new[] { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
            var vertices = new Vector3[faces.Length];
            var triangles = new int[faces.Length];
            for (var i = 0; i < faces.Length; i++)
            {
                vertices[i] = v[faces[i]] * 0.5f;
                triangles[i] = i;
            }

            s_Octahedron = new Mesh { name = "Foam Octahedron", vertices = vertices, triangles = triangles };
            s_Octahedron.RecalculateNormals();
            return s_Octahedron;
        }
    }
}
