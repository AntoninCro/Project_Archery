using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Combat
{
    /// <summary>
    /// Le maillage d'une faille dans la matrice (refait le 24 octobre : elle suit le vrai chemin de la lame, en 3D autour du
    /// joueur, grandit pendant le coup, et peut avoir plusieurs brins quand des failles qui se croisent fusionnent). Pour
    /// chaque brin : le tracé du coup de katana (un peu en zigzag) et des fissures secondaires qui partent de ses bords. Il
    /// ne donne que le squelette : c'est le shader <c>Archery/MatrixTear</c> qui, à chaque image, lui donne sa forme de plaie
    /// (plus large au centre, ouverte au fil de la lame), fait onduler ses bords, frémir son tracé et ramper ses fissures.
    /// </summary>
    /// <remarks>
    /// Chaque point du tracé a trois sommets (bord gauche, milieu, bord droit), tous posés sur le tracé :
    /// <list type="bullet">
    /// <item>position : le point du tracé ; normale : le travers (perpendiculaire au tracé et au regard du joueur) ;</item>
    /// <item>tangente : pour une fissure secondaire, sa direction (xyz) et sa longueur (w, en mètres) ;</item>
    /// <item>UV0 : (distance le long du tracé en mètres, -1 / 0 / 1 en travers) ;</item>
    /// <item>UV1 : (avancée de 0 à 1 le long du tracé, demi-largeur au centre en mètres, 0 tracé ou 1 fissure secondaire,
    /// phase de 0 à 1) ;</item>
    /// <item>UV2 : (moment où la lame est passée là, en secondes depuis la naissance de la faille, 0) ;</item>
    /// <item>couleur : blanc, l'alpha atténue les fissures secondaires.</item>
    /// </list>
    /// Le maillage est refait à chaque image pendant le coup : le zigzag et les fissures secondaires sont tirés au sort
    /// selon la distance le long du tracé, avec la graine du brin, et ne bougent donc pas quand le tracé s'allonge.
    /// </remarks>
    public static class MatrixRiftMesh
    {
        /// <summary>
        /// Un brin de faille : le chemin de la lame (dans le repère de la faille), le moment où elle est passée en chaque
        /// point, et d'où le joueur le regardait.
        /// </summary>
        public sealed class Strand
        {
            /// <summary>Le chemin de la lame, dans le repère de la faille.</summary>
            public readonly List<Vector3> Path = new List<Vector3>();

            /// <summary>Pour chaque point du chemin : le moment (s depuis la naissance de la faille) où la lame y est passée.</summary>
            public readonly List<float> Births = new List<float>();

            /// <summary>Le tracé du brin (rempli à la construction du maillage) : zones de touche, distance à la main.</summary>
            public readonly List<Vector3> Spine = new List<Vector3>();

            /// <summary>Les yeux du joueur, dans le repère de la faille : le travers du tracé est perpendiculaire au regard.</summary>
            public Vector3 Viewer;

            public int Seed;

            /// <summary>Longueur du chemin (m).</summary>
            public float Length
            {
                get
                {
                    var total = 0f;
                    for (var i = 1; i < Path.Count; i++)
                        total += Vector3.Distance(Path[i - 1], Path[i]);
                    return total;
                }
            }

            public Strand Copy()
            {
                var copy = new Strand { Viewer = Viewer, Seed = Seed };
                copy.Path.AddRange(Path);
                copy.Births.AddRange(Births);
                copy.Spine.AddRange(Spine);
                return copy;
            }

            /// <summary>Passe le brin dans un autre repère (<paramref name="matrix"/>), ses moments décalés de <paramref name="timeShift"/>.</summary>
            public void Transform(Matrix4x4 matrix, float timeShift)
            {
                for (var i = 0; i < Path.Count; i++)
                    Path[i] = matrix.MultiplyPoint3x4(Path[i]);
                for (var i = 0; i < Spine.Count; i++)
                    Spine[i] = matrix.MultiplyPoint3x4(Spine[i]);
                for (var i = 0; i < Births.Count; i++)
                    Births[i] += timeShift;
                Viewer = matrix.MultiplyPoint3x4(Viewer);
            }
        }

        const float k_Spacing = 0.012f;

        // Demi-largeur (m) du centre de la plaie grande ouverte, selon la longueur du brin : 2,5 cm au moins, 8 cm au plus
        // (24 octobre : plus large au centre ; le shader la gonfle encore au milieu et l'affine aux bouts).
        const float k_WidthPerMeter = 0.075f;
        const float k_MinHalfWidth = 0.025f;
        const float k_MaxHalfWidth = 0.08f;

        // Une fissure secondaire environ tous les 8 cm.
        const float k_BranchSpacing = 0.08f;

        // Marge des bornes du maillage : le shader écarte les bords et décale le tracé de quelques centimètres.
        const float k_BoundsMargin = 0.25f;

        // Les listes de construction, gardées d'une fois sur l'autre (le maillage est refait à chaque image pendant le coup).
        static readonly List<Vector3> s_Vertices = new List<Vector3>();
        static readonly List<Vector3> s_Normals = new List<Vector3>();
        static readonly List<Vector4> s_Tangents = new List<Vector4>();
        static readonly List<Vector2> s_Uv0 = new List<Vector2>();
        static readonly List<Vector4> s_Uv1 = new List<Vector4>();
        static readonly List<Vector2> s_Uv2 = new List<Vector2>();
        static readonly List<Color> s_Colors = new List<Color>();
        static readonly List<int> s_Triangles = new List<int>();
        static readonly List<Vector3> s_Points = new List<Vector3>();
        static readonly List<float> s_Births = new List<float>();
        static readonly List<Vector3> s_Sides = new List<Vector3>();
        static readonly List<float> s_Along = new List<float>();
        static readonly List<Vector3> s_Branch = new List<Vector3>();

        /// <summary>Demi-largeur (m) du centre d'un brin de cette longueur, grand ouvert.</summary>
        public static float HalfWidthFor(float length) => Mathf.Clamp(length * k_WidthPerMeter, k_MinHalfWidth, k_MaxHalfWidth);

        /// <summary>
        /// Refait <paramref name="mesh"/> avec tous les <paramref name="strands"/> (et remplit leur tracé). Les brins de
        /// moins de deux points ne donnent rien.
        /// </summary>
        public static void Build(Mesh mesh, IReadOnlyList<Strand> strands)
        {
            s_Vertices.Clear();
            s_Normals.Clear();
            s_Tangents.Clear();
            s_Uv0.Clear();
            s_Uv1.Clear();
            s_Uv2.Clear();
            s_Colors.Clear();
            s_Triangles.Clear();
            foreach (var strand in strands)
                AddStrand(strand);

            mesh.Clear();
            if (s_Vertices.Count == 0)
                return;
            mesh.indexFormat = s_Vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(s_Vertices);
            mesh.SetNormals(s_Normals);
            mesh.SetTangents(s_Tangents);
            mesh.SetUVs(0, s_Uv0);
            mesh.SetUVs(1, s_Uv1);
            mesh.SetUVs(2, s_Uv2);
            mesh.SetColors(s_Colors);
            mesh.SetTriangles(s_Triangles, 0);
            mesh.RecalculateBounds();
            var bounds = mesh.bounds;
            bounds.Expand(k_BoundsMargin);
            mesh.bounds = bounds;
        }

        static void AddStrand(Strand strand)
        {
            strand.Spine.Clear();
            if (strand.Path.Count < 2)
                return;

            Resample(strand, k_Spacing);
            if (s_Points.Count < 2)
                return;

            // Le travers : perpendiculaire au tracé et au regard, sans se retourner d'un point à l'autre.
            s_Sides.Clear();
            var previousSide = Vector3.zero;
            for (var i = 0; i < s_Points.Count; i++)
            {
                var side = SideAt(s_Points, i, strand.Viewer, previousSide);
                s_Sides.Add(side);
                previousSide = side;
            }

            // Le tracé tremble un peu en zigzag (les deux bouts restent en place) ; tiré au sort point par point, toujours
            // dans le même ordre : il ne change pas quand le coup s'allonge.
            var random = new System.Random(strand.Seed);
            var flip = 1f;
            for (var i = 0; i < s_Points.Count; i++)
            {
                var a = Next(random);
                var b = Next(random);
                flip = a < 0.7f ? -flip : flip;
                var wobble = i == 0 ? 0f : flip * Mathf.Lerp(0.001f, 0.005f, b);
                strand.Spine.Add(s_Points[i] + s_Sides[i] * wobble);
            }

            var spine = strand.Spine;
            s_Along.Clear();
            s_Along.Add(0f);
            for (var i = 1; i < spine.Count; i++)
                s_Along.Add(s_Along[i - 1] + Vector3.Distance(spine[i - 1], spine[i]));
            var length = s_Along[s_Along.Count - 1];
            var halfWidth = HalfWidthFor(length);
            var phase = (strand.Seed & 1023) / 1023f;

            // Le tracé : t de 0 à 1, la même demi-largeur de départ partout (le shader en fait une plaie).
            var start = s_Vertices.Count;
            for (var i = 0; i < spine.Count; i++)
            {
                var t = length > 0f ? s_Along[i] / length : 0f;
                AddPoint(spine[i], s_Sides[i], Vector4.zero, s_Along[i], new Vector4(t, halfWidth, 0f, phase), s_Births[i], 1f);
                if (i > 0)
                    AddQuads(start + (i - 1) * 3, start + i * 3);
            }

            // Les fissures secondaires : environ une tous les 8 cm, de chaque côté au hasard, en biais vers l'avant ou
            // l'arrière ; une sur deux se ramifie. Chacune a sa place (tirée au sort selon la distance le long du tracé) et
            // sa phase : elles rampent chacune à leur rythme.
            var branches = new System.Random(strand.Seed ^ 0x5bd1e995);
            for (var slot = 0; ; slot++)
            {
                var jitter = Next(branches);
                var side = Next(branches) < 0.5f ? -1f : 1f;
                var angle = Mathf.Lerp(20f, 65f, Next(branches)) * Mathf.Deg2Rad;
                var forward = Next(branches) < 0.7f ? 1f : -1f;
                var size = Next(branches);
                var nested = Next(branches);
                var branchPhase = Next(branches);
                var widthJitter = Next(branches);

                var along = (slot + 0.3f + 0.4f * jitter) * k_BranchSpacing;
                if (along > length - 0.03f)
                    break;
                if (along < 0.03f)
                    continue;

                var index = IndexAt(along);
                var t = along / length;
                var widthHere = halfWidth * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 1.4f);
                var branchLength = Mathf.Lerp(0.04f, 0.12f, size) * Mathf.Lerp(0.6f, 1.2f, widthHere / Mathf.Max(1e-4f, halfWidth));
                var tangent = (spine[Mathf.Min(spine.Count - 1, index + 1)] - spine[Mathf.Max(0, index - 1)]).normalized;
                var direction = (s_Sides[index] * (side * Mathf.Cos(angle)) + tangent * (Mathf.Sin(angle) * forward)).normalized;
                AddBranch(spine[index], direction, strand.Viewer, s_Along[index], widthHere * 0.45f * Mathf.Lerp(0.8f, 1.2f, widthJitter),
                          branchLength, branchPhase, s_Births[index], nested < 0.5f, strand.Seed + slot * 7919);
            }
        }

        // Une fissure secondaire : un petit tracé en zigzag qui part du bord, en biais ; elle peut se ramifier une fois.
        static void AddBranch(Vector3 origin, Vector3 direction, Vector3 viewer, float alongStart, float startWidth, float length,
                              float phase, float birth, bool nested, int seed)
        {
            var random = new System.Random(seed);
            var side = Vector3.Cross(direction, (origin - viewer).normalized);
            side = side.sqrMagnitude > 1e-8f ? side.normalized : Vector3.up;

            const int steps = 7;
            s_Branch.Clear();
            var zig = 1f;
            for (var k = 0; k <= steps; k++)
            {
                zig = -zig;
                var offset = k == 0 ? 0f : zig * Mathf.Lerp(0.001f, 0.0035f, Next(random));
                s_Branch.Add(origin + direction * (length * k / steps) + side * offset);
            }

            var start = s_Vertices.Count;
            var travelled = 0f;
            for (var k = 0; k <= steps; k++)
            {
                if (k > 0)
                    travelled += Vector3.Distance(s_Branch[k - 1], s_Branch[k]);
                var t = k / (float)steps;
                AddPoint(s_Branch[k], side, new Vector4(direction.x, direction.y, direction.z, length), alongStart + travelled,
                         new Vector4(t, startWidth, 1f, phase), birth, Mathf.Lerp(0.95f, 0.7f, t));
                if (k > 0)
                    AddQuads(start + (k - 1) * 3, start + k * 3);
            }

            if (!nested)
                return;

            // La ramification : du milieu de la fissure, plus petite, de l'autre côté.
            var middle = s_Branch[steps / 2];
            var turn = Quaternion.AngleAxis((Next(random) < 0.5f ? -1f : 1f) * Mathf.Lerp(25f, 50f, Next(random)), (middle - viewer).normalized);
            AddBranch(middle, turn * direction, viewer, alongStart + travelled * 0.5f, startWidth * 0.6f, length * 0.5f,
                      Next(random), birth + 0.03f, false, seed * 31 + 1);
        }

        static void AddPoint(Vector3 point, Vector3 normal, Vector4 tangent, float along, Vector4 data, float birth, float alpha)
        {
            for (var across = -1; across <= 1; across++)
            {
                s_Vertices.Add(point);
                s_Normals.Add(normal);
                s_Tangents.Add(tangent);
                s_Uv0.Add(new Vector2(along, across));
                s_Uv1.Add(data);
                s_Uv2.Add(new Vector2(birth, 0f));
                s_Colors.Add(new Color(1f, 1f, 1f, alpha));
            }
        }

        // Entre deux points du tracé : quatre triangles (gauche-milieu, milieu-droite).
        static void AddQuads(int a, int b)
        {
            var t = s_Triangles;
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

        // Le travers du tracé en ce point : perpendiculaire à sa direction et au regard (depuis viewer), du même côté que
        // le travers du point d'avant.
        static Vector3 SideAt(List<Vector3> points, int i, Vector3 viewer, Vector3 previous)
        {
            var tangent = points[Mathf.Min(points.Count - 1, i + 1)] - points[Mathf.Max(0, i - 1)];
            var side = Vector3.Cross(tangent, points[i] - viewer);
            if (side.sqrMagnitude < 1e-12f)
                return previous.sqrMagnitude > 0f ? previous : Vector3.up;
            side.Normalize();
            return Vector3.Dot(side, previous) < 0f ? -side : side;
        }

        // Le point du tracé à cette distance de son début.
        static int IndexAt(float along)
        {
            var index = s_Along.BinarySearch(along);
            if (index < 0)
                index = ~index;
            return Mathf.Clamp(index, 1, s_Along.Count - 2);
        }

        // Des points tous les « spacing » mètres le long du chemin, avec le moment où la lame y est passée.
        static void Resample(Strand strand, float spacing)
        {
            var path = strand.Path;
            var births = strand.Births;
            s_Points.Clear();
            s_Births.Clear();
            s_Points.Add(path[0]);
            s_Births.Add(births.Count > 0 ? births[0] : 0f);
            var carried = 0f;
            for (var i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                var segment = Vector3.Distance(a, b);
                if (segment < 1e-6f)
                    continue;
                var birthA = i - 1 < births.Count ? births[i - 1] : 0f;
                var birthB = i < births.Count ? births[i] : birthA;
                var position = spacing - carried;
                while (position <= segment)
                {
                    var f = position / segment;
                    s_Points.Add(Vector3.Lerp(a, b, f));
                    s_Births.Add(Mathf.Lerp(birthA, birthB, f));
                    position += spacing;
                }

                carried = segment - (position - spacing);
            }

            if ((s_Points[s_Points.Count - 1] - path[path.Count - 1]).sqrMagnitude > 1e-8f)
            {
                s_Points.Add(path[path.Count - 1]);
                s_Births.Add(births.Count > 0 ? births[births.Count - 1] : 0f);
            }
        }

        static float Next(System.Random random) => (float)random.NextDouble();
    }
}
