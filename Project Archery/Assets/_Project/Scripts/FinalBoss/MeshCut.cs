using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Coupe un maillage en deux le long du plan x = 0 (porte d'un seul battant → deux battants) : les triangles à
    /// cheval sont coupés, et chaque morceau tranché (planche, bande de fer, cadre) est refermé par un couvercle plat.
    /// </summary>
    /// <remarks>
    /// Le maillage doit être lisible (« Read/Write » coché dans l'import du modèle). Les positions sont d'abord passées
    /// par <c>toLocal</c> : la coupe se fait dans ce repère.
    /// </remarks>
    public static class MeshCut
    {
        const float k_Weld = 1000f;     // sommets confondus à 1 mm près

        sealed class Half
        {
            public readonly List<Vector3> Positions = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int>[] Triangles;

            public Half(int subMeshes)
            {
                Triangles = new List<int>[subMeshes];
                for (var i = 0; i < subMeshes; i++)
                    Triangles[i] = new List<int>();
            }

            public void Add(int subMesh, Vertex a, Vertex b, Vertex c)
            {
                var start = Positions.Count;
                foreach (var v in new[] { a, b, c })
                {
                    Positions.Add(v.Position);
                    Normals.Add(v.Normal);
                    Uvs.Add(v.Uv);
                }

                Triangles[subMesh].Add(start);
                Triangles[subMesh].Add(start + 1);
                Triangles[subMesh].Add(start + 2);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                if (Positions.Count > 65000)
                    mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(Positions);
                mesh.SetNormals(Normals);
                mesh.SetUVs(0, Uvs);
                mesh.subMeshCount = Triangles.Length;
                for (var i = 0; i < Triangles.Length; i++)
                    mesh.SetTriangles(Triangles[i], i);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 Uv;
            public int Weld;        // sommet soudé (même position), pour reconnaître les arêtes partagées

            public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex
            {
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Normal = Vector3.Lerp(a.Normal, b.Normal, t).normalized,
                Uv = Vector2.Lerp(a.Uv, b.Uv, t),
                Weld = -1,
            };
        }

        // Un point de la coupe : le croisement d'une arête (deux sommets soudés) ou un sommet posé sur le plan.
        readonly struct CutKey
        {
            readonly int m_A;
            readonly int m_B;

            public CutKey(int a, int b)
            {
                m_A = Mathf.Min(a, b);
                m_B = Mathf.Max(a, b);
            }

            public override int GetHashCode() => m_A * 73856093 ^ m_B;
            public override bool Equals(object other) => other is CutKey key && key.m_A == m_A && key.m_B == m_B;
        }

        /// <summary>
        /// Coupe <paramref name="source"/> : <paramref name="left"/> garde ce qui est à x &lt; 0 (dans le repère
        /// <paramref name="toLocal"/>), <paramref name="right"/> ce qui est à x &gt; 0. Les sous-maillages (matériaux)
        /// sont gardés. Faux si le maillage n'est pas lisible.
        /// </summary>
        public static bool SplitAtX(Mesh source, Matrix4x4 toLocal, out Mesh left, out Mesh right)
        {
            left = null;
            right = null;
            if (source == null || !source.isReadable)
                return false;

            var positions = source.vertices;
            var normals = source.normals;
            var uvs = source.uv;
            var normalMatrix = toLocal.inverse.transpose;
            var flip = toLocal.determinant < 0f;

            // Sommets dans le repère de la coupe, soudés par position.
            var vertices = new Vertex[positions.Length];
            var welds = new Dictionary<Vector3Int, int>();
            for (var i = 0; i < positions.Length; i++)
            {
                var p = toLocal.MultiplyPoint3x4(positions[i]);
                var n = normals.Length == positions.Length ? normalMatrix.MultiplyVector(normals[i]).normalized : Vector3.up;
                var key = Vector3Int.RoundToInt(p * k_Weld);
                if (!welds.TryGetValue(key, out var weld))
                {
                    weld = welds.Count;
                    welds.Add(key, weld);
                }

                vertices[i] = new Vertex { Position = p, Normal = n, Uv = uvs.Length == positions.Length ? uvs[i] : Vector2.zero, Weld = weld };
            }

            var subMeshes = source.subMeshCount;
            var leftHalf = new Half(subMeshes);
            var rightHalf = new Half(subMeshes);

            // Les segments de la coupe, reliés en boucles : une boucle par morceau tranché (un couvercle chacune).
            var cutPoints = new Dictionary<CutKey, int>();
            var cutPositions = new List<Vector3>();
            var cutParent = new List<int>();
            var cutSubMesh = new List<int>();

            int CutPoint(CutKey key, Vector3 position, int subMesh)
            {
                if (cutPoints.TryGetValue(key, out var index))
                    return index;
                index = cutPositions.Count;
                cutPoints.Add(key, index);
                cutPositions.Add(new Vector3(0f, position.y, position.z));
                cutParent.Add(index);
                cutSubMesh.Add(subMesh);
                return index;
            }

            int Find(int i)
            {
                while (cutParent[i] != i)
                {
                    cutParent[i] = cutParent[cutParent[i]];
                    i = cutParent[i];
                }

                return i;
            }

            var polygon = new List<Vertex>(4);
            var corners = new Vertex[3];
            for (var subMesh = 0; subMesh < subMeshes; subMesh++)
            {
                var triangles = source.GetTriangles(subMesh);
                for (var t = 0; t + 2 < triangles.Length; t += 3)
                {
                    // Un repère miroir retourne les faces : on inverse alors l'ordre des sommets.
                    corners[0] = vertices[triangles[t]];
                    corners[1] = vertices[triangles[t + (flip ? 2 : 1)]];
                    corners[2] = vertices[triangles[t + (flip ? 1 : 2)]];

                    var anyLeft = false;
                    var anyRight = false;
                    foreach (var corner in corners)
                    {
                        if (corner.Position.x < -1e-5f)
                            anyLeft = true;
                        else if (corner.Position.x > 1e-5f)
                            anyRight = true;
                    }

                    if (!anyRight)
                    {
                        leftHalf.Add(subMesh, corners[0], corners[1], corners[2]);
                        continue;
                    }

                    if (!anyLeft)
                    {
                        rightHalf.Add(subMesh, corners[0], corners[1], corners[2]);
                        continue;
                    }

                    // À cheval : chaque moitié garde son côté du triangle (un triangle ou un quadrilatère).
                    Clip(corners, -1f, polygon);
                    for (var i = 1; i + 1 < polygon.Count; i++)
                        leftHalf.Add(subMesh, polygon[0], polygon[i], polygon[i + 1]);
                    Clip(corners, 1f, polygon);
                    for (var i = 1; i + 1 < polygon.Count; i++)
                        rightHalf.Add(subMesh, polygon[0], polygon[i], polygon[i + 1]);

                    // Le segment de coupe de ce triangle, entre ses deux points sur le plan.
                    var first = -1;
                    for (var i = 0; i < 3; i++)
                    {
                        var a = corners[i];
                        var b = corners[(i + 1) % 3];
                        int point;
                        if (Mathf.Abs(a.Position.x) <= 1e-5f)
                            point = CutPoint(new CutKey(a.Weld, a.Weld), a.Position, subMesh);
                        else if (a.Position.x * b.Position.x < 0f && Mathf.Abs(b.Position.x) > 1e-5f)
                            point = CutPoint(new CutKey(a.Weld, b.Weld), Vertex.Lerp(a, b, a.Position.x / (a.Position.x - b.Position.x)).Position, subMesh);
                        else
                            continue;

                        if (first < 0)
                            first = point;
                        else
                            cutParent[Find(point)] = Find(first);
                    }
                }
            }

            // Un couvercle plat par boucle de coupe : l'enveloppe convexe de ses points (planches, bandes et cadre sont
            // des formes pleines, à section simple).
            var loops = new Dictionary<int, List<Vector3>>();
            var loopSubMesh = new Dictionary<int, int>();
            for (var i = 0; i < cutPositions.Count; i++)
            {
                var root = Find(i);
                if (!loops.TryGetValue(root, out var points))
                {
                    points = new List<Vector3>();
                    loops.Add(root, points);
                    loopSubMesh.Add(root, cutSubMesh[i]);
                }

                points.Add(cutPositions[i]);
            }

            foreach (var loop in loops)
            {
                var hull = Hull(loop.Value);
                if (hull.Count < 3)
                    continue;

                var subMesh = loopSubMesh[loop.Key];
                Vertex V(Vector3 p, Vector3 n) => new Vertex { Position = p, Normal = n, Uv = Vector2.zero, Weld = -1 };
                for (var i = 1; i + 1 < hull.Count; i++)
                {
                    // L'enveloppe tourne dans le sens direct du plan (z, y) : c'est le sens inverse d'Unity vu depuis +x
                    // (couvercle de la moitié gauche, tourné vers +x), et son sens vu depuis -x (moitié droite).
                    leftHalf.Add(subMesh, V(hull[0], Vector3.right), V(hull[i + 1], Vector3.right), V(hull[i], Vector3.right));
                    rightHalf.Add(subMesh, V(hull[0], Vector3.left), V(hull[i], Vector3.left), V(hull[i + 1], Vector3.left));
                }
            }

            left = leftHalf.ToMesh(source.name + " (gauche)");
            right = rightHalf.ToMesh(source.name + " (droite)");
            return true;
        }

        // Le morceau du triangle du côté voulu (side = -1 : x ≤ 0 ; +1 : x ≥ 0), en polygone (Sutherland-Hodgman).
        static void Clip(Vertex[] corners, float side, List<Vertex> result)
        {
            result.Clear();
            for (var i = 0; i < 3; i++)
            {
                var a = corners[i];
                var b = corners[(i + 1) % 3];
                var da = a.Position.x * side;
                var db = b.Position.x * side;
                if (da >= 0f)
                    result.Add(a);
                if (da > 0f && db < 0f || da < 0f && db > 0f)
                    result.Add(Vertex.Lerp(a, b, da / (da - db)));
            }
        }

        // Enveloppe convexe des points (dans le plan x = 0, coordonnées z et y), sens direct (chaîne monotone).
        static List<Vector3> Hull(List<Vector3> points)
        {
            var sorted = new List<Vector3>(points);
            sorted.Sort((a, b) => a.z != b.z ? a.z.CompareTo(b.z) : a.y.CompareTo(b.y));
            var hull = new List<Vector3>();
            if (sorted.Count < 3)
                return hull;

            float Cross(Vector3 o, Vector3 a, Vector3 b) => (a.z - o.z) * (b.y - o.y) - (a.y - o.y) * (b.z - o.z);

            for (var pass = 0; pass < 2; pass++)
            {
                var start = hull.Count;
                for (var i = 0; i < sorted.Count; i++)
                {
                    var p = sorted[pass == 0 ? i : sorted.Count - 1 - i];
                    while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], p) <= 1e-7f)
                        hull.RemoveAt(hull.Count - 1);
                    hull.Add(p);
                }

                hull.RemoveAt(hull.Count - 1);
            }

            return hull;
        }
    }
}
