using System.Collections.Generic;
using UnityEngine;

namespace Archery.Combat
{
    /// <summary>
    /// Les symboles à tracer au katana (22 octobre) : des kanji simples de 2 ou 3 traits, tous droits. Chaque trait se trace
    /// d'un seul geste. Coordonnées dans un carré de -1 à 1 (Y vers le haut), dans l'ordre et le sens d'écriture ; un trait
    /// de plus de deux points serait lissé (Catmull-Rom).
    /// </summary>
    public static class KanjiGlyphs
    {
        // Seulement des traits droits depuis le 23 octobre : les symboles à crochets ou à angles (力 刀 又 七, l'éclair, la
        // clé) étaient trop durs à tracer d'un coup de sabre ; 人 八 大 川 sont redressés, 入 retiré (redressé, c'était 人).
        static readonly Vector2[][][] k_Glyphs =
        {
            // 十
            new[] { Stroke(-0.8f, 0.1f, 0.8f, 0.1f), Stroke(0f, 0.85f, 0f, -0.9f) },
            // 人
            new[] { Stroke(0.05f, 0.85f, -0.75f, -0.8f), Stroke(-0.05f, 0.25f, 0.8f, -0.8f) },
            // 八
            new[] { Stroke(-0.25f, 0.6f, -0.8f, -0.7f), Stroke(0.25f, 0.65f, 0.85f, -0.7f) },
            // 乂
            new[] { Stroke(-0.7f, 0.8f, 0.75f, -0.85f), Stroke(0.7f, 0.8f, -0.75f, -0.85f) },
            // 二
            new[] { Stroke(-0.55f, 0.45f, 0.55f, 0.45f), Stroke(-0.85f, -0.45f, 0.85f, -0.45f) },
            // 卜
            new[] { Stroke(-0.15f, 0.9f, -0.15f, -0.9f), Stroke(-0.05f, 0.3f, 0.65f, -0.15f) },
            // 工
            new[] { Stroke(-0.6f, 0.7f, 0.6f, 0.7f), Stroke(0f, 0.7f, 0f, -0.75f), Stroke(-0.85f, -0.75f, 0.85f, -0.75f) },
            // 土
            new[] { Stroke(-0.6f, 0.25f, 0.6f, 0.25f), Stroke(0f, 0.85f, 0f, -0.75f), Stroke(-0.85f, -0.75f, 0.85f, -0.75f) },
            // 士
            new[] { Stroke(-0.8f, 0.35f, 0.8f, 0.35f), Stroke(0f, 0.9f, 0f, -0.6f), Stroke(-0.5f, -0.6f, 0.5f, -0.6f) },
            // 干
            new[] { Stroke(-0.65f, 0.7f, 0.65f, 0.7f), Stroke(-0.85f, 0.05f, 0.85f, 0.05f), Stroke(0f, 0.7f, 0f, -0.9f) },
            // 大
            new[] { Stroke(-0.8f, 0.3f, 0.8f, 0.3f), Stroke(0.05f, 0.9f, -0.75f, -0.85f), Stroke(0.05f, 0.25f, 0.8f, -0.85f) },
            // 川
            new[] { Stroke(-0.6f, 0.8f, -0.7f, -0.75f), Stroke(0f, 0.6f, 0f, -0.45f), Stroke(0.6f, 0.9f, 0.6f, -0.9f) },
            // 三
            new[] { Stroke(-0.55f, 0.7f, 0.55f, 0.7f), Stroke(-0.4f, 0.05f, 0.4f, 0.05f), Stroke(-0.85f, -0.7f, 0.85f, -0.7f) },
            // 千
            new[] { Stroke(0.45f, 0.9f, -0.45f, 0.6f), Stroke(-0.85f, 0.15f, 0.85f, 0.15f), Stroke(0f, 0.6f, 0f, -0.9f) },
            // 丫
            new[] { Stroke(-0.65f, 0.8f, 0f, 0.05f), Stroke(0.65f, 0.8f, 0f, 0.05f), Stroke(0f, 0.05f, 0f, -0.9f) },
        };

        static readonly string[] k_Names = { "十", "人", "八", "乂", "二", "卜", "工", "土", "士", "干", "大", "川", "三", "千", "丫" };

        static readonly Dictionary<int, List<Vector2>[]> s_Smoothed = new Dictionary<int, List<Vector2>[]>();

        public static int Count => k_Glyphs.Length;

        /// <summary>Le nom du symbole (le kanji, ou celui d'un signe inventé), pour la console.</summary>
        public static string Name(int glyph) => k_Names[Mathf.Clamp(glyph, 0, k_Names.Length - 1)];

        /// <summary>Les traits du symbole, lissés (environ un point tous les 0,05).</summary>
        public static List<Vector2>[] Strokes(int glyph)
        {
            glyph = Mathf.Clamp(glyph, 0, k_Glyphs.Length - 1);
            if (s_Smoothed.TryGetValue(glyph, out var strokes))
                return strokes;

            var source = k_Glyphs[glyph];
            strokes = new List<Vector2>[source.Length];
            for (var s = 0; s < source.Length; s++)
                strokes[s] = Smooth(source[s], 0.05f);
            s_Smoothed[glyph] = strokes;
            return strokes;
        }

        /// <summary>Un autre symbole que <paramref name="previous"/>, au hasard.</summary>
        public static int Random(int previous)
        {
            if (k_Glyphs.Length < 2)
                return 0;
            var glyph = UnityEngine.Random.Range(0, k_Glyphs.Length - 1);
            return glyph >= previous && previous >= 0 ? glyph + 1 : glyph;
        }

        /// <summary><paramref name="count"/> points répartis régulièrement le long d'une ligne brisée.</summary>
        public static void Resample(IReadOnlyList<Vector2> points, int count, List<Vector2> result)
        {
            result.Clear();
            if (points.Count == 0)
                return;

            var total = 0f;
            for (var i = 1; i < points.Count; i++)
                total += Vector2.Distance(points[i - 1], points[i]);
            if (total < 1e-5f || points.Count == 1)
            {
                for (var i = 0; i < count; i++)
                    result.Add(points[0]);
                return;
            }

            var segment = 1;
            var walked = 0f;
            for (var i = 0; i < count; i++)
            {
                var target = total * i / (count - 1f);
                while (segment < points.Count - 1 && walked + Vector2.Distance(points[segment - 1], points[segment]) < target)
                {
                    walked += Vector2.Distance(points[segment - 1], points[segment]);
                    segment++;
                }

                var length = Vector2.Distance(points[segment - 1], points[segment]);
                var t = length > 1e-6f ? Mathf.Clamp01((target - walked) / length) : 0f;
                result.Add(Vector2.Lerp(points[segment - 1], points[segment], t));
            }
        }

        /// <summary>La longueur d'une ligne brisée.</summary>
        public static float Length(IReadOnlyList<Vector2> points)
        {
            var total = 0f;
            for (var i = 1; i < points.Count; i++)
                total += Vector2.Distance(points[i - 1], points[i]);
            return total;
        }

        static Vector2[] Stroke(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (var i = 0; i < points.Length; i++)
                points[i] = new Vector2(xy[2 * i], xy[2 * i + 1]);
            return points;
        }

        // Une ligne droite reste droite ; une ligne brisée devient une courbe qui passe par ses points.
        static List<Vector2> Smooth(Vector2[] points, float step)
        {
            var result = new List<Vector2>();
            if (points.Length <= 2)
            {
                var length = points.Length == 2 ? Vector2.Distance(points[0], points[1]) : 0f;
                var steps = Mathf.Max(1, Mathf.CeilToInt(length / step));
                for (var i = 0; i <= steps && points.Length == 2; i++)
                    result.Add(Vector2.Lerp(points[0], points[1], i / (float)steps));
                if (points.Length == 1)
                    result.Add(points[0]);
                return result;
            }

            for (var i = 0; i < points.Length - 1; i++)
            {
                var p0 = points[Mathf.Max(0, i - 1)];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = points[Mathf.Min(points.Length - 1, i + 2)];
                var steps = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2) / step));
                for (var k = 0; k < steps; k++)
                {
                    var t = k / (float)steps;
                    var t2 = t * t;
                    var t3 = t2 * t;
                    result.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                }
            }

            result.Add(points[points.Length - 1]);
            return result;
        }
    }
}
