using System.Collections.Generic;
using UnityEngine;

namespace Archery.Combat
{
    /// <summary>
    /// Le maillage d'une fissure dans la matrice (22 octobre), à plat dans le plan XY : le long d'un geste, une fente au tracé
    /// un peu en zigzag, aux bords déchiquetés (chaque bord a ses propres échancrures), pointue aux deux bouts, avec des
    /// fissures plus fines qui partent sur les côtés. Pour le shader <c>Archery/MatrixTear</c> : UV0 = (distance le long
    /// de la fissure, -1 à 1 en travers), UV1 = la position dans le plan (la grille des chiffres), couleur des sommets.
    /// </summary>
    public sealed class MatrixCrackMesh
    {
        const float k_Spacing = 0.012f;

        readonly List<Vector3> m_Vertices = new List<Vector3>();
        readonly List<Vector2> m_Uv0 = new List<Vector2>();
        readonly List<Vector2> m_Uv1 = new List<Vector2>();
        readonly List<Color> m_Colors = new List<Color>();
        readonly List<int> m_Triangles = new List<int>();
        readonly List<Vector3> m_Points = new List<Vector3>();
        readonly List<float> m_Widths = new List<float>();

        public void Clear()
        {
            m_Vertices.Clear();
            m_Uv0.Clear();
            m_Uv1.Clear();
            m_Colors.Clear();
            m_Triangles.Clear();
        }

        /// <summary>
        /// Une fissure le long de <paramref name="path"/> (dans le plan XY), d'une demi-largeur de
        /// <paramref name="halfWidth"/> en son milieu ; ses fissures secondaires viennent de <paramref name="random"/>.
        /// </summary>
        public void AddCrack(IReadOnlyList<Vector3> path, float halfWidth, System.Random random, Color color)
        {
            if (path.Count < 2)
                return;

            Resample(path, k_Spacing, m_Points);
            if (m_Points.Count < 2)
                return;

            // Le tracé tremble un peu en zigzag ; la largeur varie, et s'affine en pointe aux deux bouts.
            var main = new List<Vector3>(m_Points.Count);
            var widths = new List<float>(m_Points.Count);
            var sign = 1f;
            for (var i = 0; i < m_Points.Count; i++)
            {
                var s = i / (m_Points.Count - 1f);
                var normal = Normal(m_Points, i);
                sign = Next(random) < 0.7f ? -sign : sign;
                var wobble = (i == 0 || i == m_Points.Count - 1) ? 0f : sign * Mathf.Lerp(0.0015f, 0.006f, Next(random));
                main.Add(m_Points[i] + normal * wobble);
                var taper = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Clamp01(s)), 0.55f);
                widths.Add(halfWidth * taper * Mathf.Lerp(0.5f, 1.35f, Next(random)));
            }

            AddStrip(main, widths, random, color);

            // Les fissures secondaires : environ une tous les 10 cm, de chaque côté au hasard, en biais vers l'avant.
            var length = PathLength(main);
            var branches = 1 + Mathf.RoundToInt(length / 0.1f) + random.Next(0, 2);
            for (var b = 0; b < branches; b++)
            {
                var index = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(0.12f, 0.88f, Next(random)) * (main.Count - 1)), 1, main.Count - 2);
                AddBranch(main, index, widths[index] * 0.6f, Mathf.Lerp(0.03f, 0.1f, Next(random)), random, color, 1);
            }
        }

        /// <summary>Remplit <paramref name="mesh"/> avec tout ce qui a été ajouté.</summary>
        public void Apply(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(m_Vertices);
            mesh.SetUVs(0, m_Uv0);
            mesh.SetUVs(1, m_Uv1);
            mesh.SetColors(m_Colors);
            mesh.SetTriangles(m_Triangles, 0);
            mesh.RecalculateBounds();
        }

        void AddBranch(List<Vector3> main, int index, float startWidth, float length, System.Random random, Color color, int depth)
        {
            var origin = main[index];
            var tangent = (main[index + 1] - main[index - 1]).normalized;
            var normal = new Vector3(-tangent.y, tangent.x, 0f) * (Next(random) < 0.5f ? -1f : 1f);
            var angle = Mathf.Lerp(20f, 65f, Next(random)) * Mathf.Deg2Rad;
            var direction = (normal * Mathf.Cos(angle) + tangent * Mathf.Sin(angle) * (Next(random) < 0.75f ? 1f : -1f)).normalized;
            var side = new Vector3(-direction.y, direction.x, 0f);

            var points = new List<Vector3>();
            var widths = new List<float>();
            const int steps = 6;
            var zig = 1f;
            for (var k = 0; k <= steps; k++)
            {
                var t = k / (float)steps;
                zig = -zig;
                var offset = k == 0 ? 0f : zig * Mathf.Lerp(0.001f, 0.004f, Next(random));
                points.Add(origin + direction * (length * t) + side * offset);
                widths.Add(startWidth * Mathf.Pow(1f - t, 0.8f) * Mathf.Lerp(0.7f, 1.2f, Next(random)));
            }

            AddStrip(points, widths, random, color);

            // Une fissure sur deux se ramifie encore une fois, plus fine.
            if (depth < 2 && Next(random) < 0.5f)
                AddBranch(points, steps / 2, widths[steps / 2] * 0.7f, length * 0.5f, random, color, depth + 1);
        }

        // Trois sommets par point (bord gauche, milieu, bord droit), chaque bord échancré à sa façon.
        void AddStrip(List<Vector3> points, List<float> widths, System.Random random, Color color)
        {
            var start = m_Vertices.Count;
            var along = 0f;
            for (var i = 0; i < points.Count; i++)
            {
                if (i > 0)
                    along += Vector3.Distance(points[i - 1], points[i]);
                var normal = Normal(points, i);
                var left = points[i] + normal * widths[i] * Mathf.Lerp(0.55f, 1.25f, Next(random));
                var right = points[i] - normal * widths[i] * Mathf.Lerp(0.55f, 1.25f, Next(random));
                AddVertex(left, along, -1f, color);
                AddVertex(points[i], along, 0f, color);
                AddVertex(right, along, 1f, color);
                if (i == 0)
                    continue;

                var a = start + (i - 1) * 3;
                var b = start + i * 3;
                m_Triangles.Add(a);
                m_Triangles.Add(a + 1);
                m_Triangles.Add(b);
                m_Triangles.Add(a + 1);
                m_Triangles.Add(b + 1);
                m_Triangles.Add(b);
                m_Triangles.Add(a + 1);
                m_Triangles.Add(a + 2);
                m_Triangles.Add(b + 1);
                m_Triangles.Add(a + 2);
                m_Triangles.Add(b + 2);
                m_Triangles.Add(b + 1);
            }
        }

        void AddVertex(Vector3 position, float along, float across, Color color)
        {
            m_Vertices.Add(position);
            m_Uv0.Add(new Vector2(along, across));
            m_Uv1.Add(new Vector2(position.x, position.y));
            m_Colors.Add(color);
        }

        static Vector3 Normal(List<Vector3> points, int i)
        {
            var a = points[Mathf.Max(0, i - 1)];
            var b = points[Mathf.Min(points.Count - 1, i + 1)];
            var tangent = b - a;
            tangent.z = 0f;
            if (tangent.sqrMagnitude < 1e-10f)
                return Vector3.up;
            tangent.Normalize();
            return new Vector3(-tangent.y, tangent.x, 0f);
        }

        static float PathLength(List<Vector3> points)
        {
            var total = 0f;
            for (var i = 1; i < points.Count; i++)
                total += Vector3.Distance(points[i - 1], points[i]);
            return total;
        }

        static void Resample(IReadOnlyList<Vector3> path, float spacing, List<Vector3> result)
        {
            result.Clear();
            result.Add(path[0]);
            var carried = 0f;
            for (var i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                var segment = Vector3.Distance(a, b);
                var position = spacing - carried;
                while (position <= segment)
                {
                    result.Add(Vector3.Lerp(a, b, position / segment));
                    position += spacing;
                }

                carried = segment - (position - spacing);
            }

            if ((result[result.Count - 1] - path[path.Count - 1]).sqrMagnitude > 1e-8f)
                result.Add(path[path.Count - 1]);
        }

        static float Next(System.Random random) => (float)random.NextDouble();
    }
}
