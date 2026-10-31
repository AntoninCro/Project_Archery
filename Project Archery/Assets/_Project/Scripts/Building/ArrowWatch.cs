using System.Collections.Generic;
using Archery.Bows;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Les flèches en vol, image par image (deuxième brainstorm du tower defense, 29 octobre : les évolutions « arc » des tours,
    /// la flèche qui traverse la barricade, le faisceau du phare, le champ de l'aimant), comme l'anneau élémentaire les suivait
    /// (<see cref="StructureRing"/>) : pour chaque flèche en vol, le trajet de sa pointe depuis l'image d'avant.
    /// </summary>
    /// <remarks>
    /// Une par bâtiment : <see cref="Begin"/> à son départ, <see cref="End"/> à sa fin, <see cref="Next"/> à chaque image.
    /// <see cref="Once"/> dit si une flèche passe pour la première fois pendant ce tir (elle est oubliée quand elle ne vole plus).
    /// </remarks>
    sealed class ArrowWatch
    {
        readonly List<Arrow> m_Arrows = new List<Arrow>();
        readonly Dictionary<Arrow, Vector3> m_LastTip = new Dictionary<Arrow, Vector3>();
        readonly HashSet<Arrow> m_Done = new HashSet<Arrow>();
        int m_Index;
        bool m_Started;

        public void Begin()
        {
            if (m_Started)
                return;

            m_Started = true;
            ArrowPool.Taken += Add;
            var pool = ArrowPool.Instance;
            if (pool != null)
            {
                foreach (var arrow in pool.AllArrows())
                    Add(arrow);
            }
        }

        public void End()
        {
            ArrowPool.Taken -= Add;
            m_Started = false;
        }

        void Add(Arrow arrow)
        {
            if (arrow != null && !m_Arrows.Contains(arrow))
                m_Arrows.Add(arrow);
        }

        /// <summary>Avant de parcourir les flèches avec <see cref="Next"/>.</summary>
        public void Reset() => m_Index = m_Arrows.Count;

        /// <summary>
        /// La flèche en vol suivante et le trajet de sa pointe depuis l'image d'avant (rien à sa première image en vol). Faux
        /// quand il n'y en a plus.
        /// </summary>
        public bool Next(out Arrow arrow, out Vector3 from, out Vector3 to)
        {
            while (--m_Index >= 0)
            {
                arrow = m_Arrows[m_Index];
                if (arrow == null)
                {
                    m_Arrows.RemoveAt(m_Index);
                    continue;
                }

                if (arrow.CurrentState != Arrow.State.Flying || arrow.IsHovering)
                {
                    m_LastTip.Remove(arrow);
                    m_Done.Remove(arrow);
                    continue;
                }

                to = arrow.TipPosition;
                var known = m_LastTip.TryGetValue(arrow, out from);
                m_LastTip[arrow] = to;
                if (known)
                    return true;
            }

            arrow = null;
            from = to = Vector3.zero;
            return false;
        }

        /// <summary>Vrai la première fois pour cette flèche pendant ce tir.</summary>
        public bool Once(Arrow arrow) => m_Done.Add(arrow);

        /// <summary>Distance la plus courte entre deux segments (le trajet d'une flèche et un rayon, une poutre…).</summary>
        public static float SegmentDistance(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
        {
            var u = a1 - a0;
            var v = b1 - b0;
            var w = a0 - b0;
            var a = Vector3.Dot(u, u);
            var b = Vector3.Dot(u, v);
            var c = Vector3.Dot(v, v);
            var d = Vector3.Dot(u, w);
            var e = Vector3.Dot(v, w);
            var denominator = a * c - b * b;
            var s = denominator > 1e-6f ? Mathf.Clamp01((b * e - c * d) / denominator) : 0f;
            var t = c > 1e-6f ? Mathf.Clamp01((b * s + e) / c) : 0f;
            s = a > 1e-6f ? Mathf.Clamp01((b * t - d) / a) : 0f;
            return Vector3.Distance(a0 + u * s, b0 + v * t);
        }
    }
}
