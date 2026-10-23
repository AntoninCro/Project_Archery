using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Combat
{
    /// <summary>
    /// Le maillage d'une faille dans la matrice (refait le 23 octobre, pour une faille vivante), à plat dans le plan XY : le
    /// tracé du coup de katana (un peu en zigzag) et des fissures secondaires qui partent de ses bords. Il ne donne que le
    /// squelette : c'est le shader <c>Archery/MatrixTear</c> qui, à chaque image, lui donne sa forme de plaie (plus large
    /// au milieu, ouverte par <c>_Open</c>), fait onduler ses bords, frémir son tracé et ramper ses fissures secondaires.
    /// </summary>
    /// <remarks>
    /// Chaque point du tracé a trois sommets (bord gauche, milieu, bord droit), tous posés sur le tracé :
    /// <list type="bullet">
    /// <item>position : le point du tracé ; normale : la perpendiculaire au tracé, dans le plan ;</item>
    /// <item>tangente : pour une fissure secondaire, sa direction (xyz) et sa longueur (w, en mètres) ;</item>
    /// <item>UV0 : (distance le long du tracé en mètres, -1 / 0 / 1 en travers) ;</item>
    /// <item>UV1 : (avancée de 0 à 1 le long du tracé, demi-largeur de départ en mètres, 0 tracé ou 1 fissure secondaire,
    /// phase de 0 à 1) ;</item>
    /// <item>couleur : blanc, l'alpha atténue les fissures secondaires.</item>
    /// </list>
    /// </remarks>
    public static class MatrixRiftMesh
    {
        const float k_Spacing = 0.012f;

        // Marge des bornes du maillage : le shader écarte les bords et décale le tracé de quelques centimètres.
        const float k_BoundsMargin = 0.2f;

        sealed class Builder
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<Vector3> Normals = new List<Vector3>();
            public readonly List<Vector4> Tangents = new List<Vector4>();
            public readonly List<Vector2> Uv0 = new List<Vector2>();
            public readonly List<Vector4> Uv1 = new List<Vector4>();
            public readonly List<Color> Colors = new List<Color>();
            public readonly List<int> Triangles = new List<int>();
        }

        /// <summary>
        /// Le maillage d'une faille le long de <paramref name="path"/> (dans le plan XY), de demi-largeur
        /// <paramref name="halfWidth"/> en son milieu une fois grande ouverte. <paramref name="spine"/> reçoit le tracé
        /// (zones de touche, distance au gantelet).
        /// </summary>
        public static Mesh Build(IReadOnlyList<Vector3> path, float halfWidth, System.Random random, List<Vector3> spine)
        {
            spine.Clear();
            var mesh = new Mesh { name = "Faille du katana" };
            if (path.Count < 2)
                return mesh;

            var points = new List<Vector3>();
            Resample(path, k_Spacing, points);
            if (points.Count < 3)
                return mesh;

            // Le tracé tremble un peu en zigzag (les deux bouts restent en place).
            var flip = 1f;
            for (var i = 0; i < points.Count; i++)
            {
                flip = Next(random) < 0.7f ? -flip : flip;
                var wobble = i == 0 || i == points.Count - 1 ? 0f : flip * Mathf.Lerp(0.001f, 0.005f, Next(random));
                spine.Add(points[i] + Normal(points, i) * wobble);
            }

            var builder = new Builder();
            var length = PathLength(spine);
            var phase = Next(random);
            var along = new float[spine.Count];
            for (var i = 1; i < spine.Count; i++)
                along[i] = along[i - 1] + Vector3.Distance(spine[i - 1], spine[i]);

            // Le tracé : t de 0 à 1, la même demi-largeur de départ partout (le shader en fait une plaie).
            var start = builder.Vertices.Count;
            for (var i = 0; i < spine.Count; i++)
            {
                var normal = Normal(spine, i);
                var t = length > 0f ? along[i] / length : 0f;
                AddPoint(builder, spine[i], normal, Vector4.zero, along[i], new Vector4(t, halfWidth, 0f, phase), 1f);
                if (i > 0)
                    AddQuads(builder, start + (i - 1) * 3, start + i * 3);
            }

            // Les fissures secondaires : environ une tous les 8 cm, de chaque côté au hasard, en biais vers l'avant ou
            // l'arrière ; une sur deux se ramifie. Chacune a sa phase : elles rampent chacune à leur rythme.
            var branches = 2 + Mathf.RoundToInt(length / 0.08f);
            for (var b = 0; b < branches; b++)
            {
                var index = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(0.1f, 0.9f, Next(random)) * (spine.Count - 1)), 1, spine.Count - 2);
                var t = length > 0f ? along[index] / length : 0.5f;
                var widthHere = halfWidth * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 0.7f);
                var branchLength = Mathf.Lerp(0.04f, 0.12f, Next(random)) * Mathf.Lerp(0.6f, 1.2f, widthHere / Mathf.Max(1e-4f, halfWidth));
                AddBranch(builder, spine, index, along[index], widthHere * 0.45f, branchLength, random, 0);
            }

            if (builder.Vertices.Count > 65000)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(builder.Vertices);
            mesh.SetNormals(builder.Normals);
            mesh.SetTangents(builder.Tangents);
            mesh.SetUVs(0, builder.Uv0);
            mesh.SetUVs(1, builder.Uv1);
            mesh.SetColors(builder.Colors);
            mesh.SetTriangles(builder.Triangles, 0);
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            bounds.Expand(k_BoundsMargin);
            mesh.bounds = bounds;
            return mesh;
        }

        // Une fissure secondaire : un petit tracé en zigzag qui part du bord, en biais.
        static void AddBranch(Builder builder, List<Vector3> path, int index, float alongStart, float startWidth, float length,
                              System.Random random, int depth)
        {
            var origin = path[index];
            var tangent = (path[Mathf.Min(path.Count - 1, index + 1)] - path[Mathf.Max(0, index - 1)]).normalized;
            var normal = new Vector3(-tangent.y, tangent.x, 0f) * (Next(random) < 0.5f ? -1f : 1f);
            var angle = Mathf.Lerp(20f, 65f, Next(random)) * Mathf.Deg2Rad;
            var direction = (normal * Mathf.Cos(angle) + tangent * (Mathf.Sin(angle) * (Next(random) < 0.7f ? 1f : -1f))).normalized;
            var side = new Vector3(-direction.y, direction.x, 0f);
            var phase = Next(random);

            const int steps = 7;
            var points = new List<Vector3>(steps + 1);
            var zig = 1f;
            for (var k = 0; k <= steps; k++)
            {
                zig = -zig;
                var offset = k == 0 ? 0f : zig * Mathf.Lerp(0.001f, 0.0035f, Next(random));
                points.Add(origin + direction * (length * k / steps) + side * offset);
            }

            var start = builder.Vertices.Count;
            var travelled = 0f;
            for (var k = 0; k <= steps; k++)
            {
                if (k > 0)
                    travelled += Vector3.Distance(points[k - 1], points[k]);
                var t = k / (float)steps;
                AddPoint(builder, points[k], Normal(points, k), new Vector4(direction.x, direction.y, 0f, length), alongStart + travelled,
                         new Vector4(t, startWidth * Mathf.Lerp(0.8f, 1.2f, Next(random)), 1f, phase), Mathf.Lerp(0.95f, 0.7f, t));
                if (k > 0)
                    AddQuads(builder, start + (k - 1) * 3, start + k * 3);
            }

            if (depth < 1 && Next(random) < 0.5f)
                AddBranch(builder, points, steps / 2, alongStart + travelled * 0.5f, startWidth * 0.6f, length * 0.5f, random, depth + 1);
        }

        static void AddPoint(Builder builder, Vector3 point, Vector3 normal, Vector4 tangent, float along, Vector4 data, float alpha)
        {
            for (var across = -1; across <= 1; across++)
            {
                builder.Vertices.Add(point);
                builder.Normals.Add(normal);
                builder.Tangents.Add(tangent);
                builder.Uv0.Add(new Vector2(along, across));
                builder.Uv1.Add(data);
                builder.Colors.Add(new Color(1f, 1f, 1f, alpha));
            }
        }

        // Entre deux points du tracé : quatre triangles (gauche-milieu, milieu-droite).
        static void AddQuads(Builder builder, int a, int b)
        {
            var t = builder.Triangles;
            t.Add(a);
            t.Add(a + 1);
            t.Add(b);
            t.Add(a + 1);
            t.Add(b + 1);
            t.Add(b);
            t.Add(a + 1);
            t.Add(a + 2);
            t.Add(b + 1);
            t.Add(a + 2);
            t.Add(b + 2);
            t.Add(b + 1);
        }

        static Vector3 Normal(List<Vector3> points, int i)
        {
            var tangent = points[Mathf.Min(points.Count - 1, i + 1)] - points[Mathf.Max(0, i - 1)];
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
                if (segment < 1e-6f)
                    continue;
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
