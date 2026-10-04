using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Upgrades
{
    /// <summary>
    /// Éclair en zigzag entre deux points, avec un cœur blanc, qui scintille puis s'efface
    /// (flèche de foudre ; en gerbe pour les explosions). Les éclairs sont réutilisés.
    /// </summary>
    public class LightningBolt : MonoBehaviour
    {
        const int k_Segments = 14;

        static readonly Stack<LightningBolt> s_Pool = new Stack<LightningBolt>();
        static readonly Vector3[] s_Points = new Vector3[k_Segments + 1];
        static Transform s_Root;
        static Material s_DefaultMaterial;

        LineRenderer m_Glow;
        LineRenderer m_Core;
        Vector3 m_From;
        Vector3 m_To;
        Color m_Color;
        float m_Jitter;
        float m_Age;
        float m_Duration;
        float m_NextFlicker;

        static Material DefaultMaterial
        {
            get
            {
                if (s_DefaultMaterial == null)
                {
                    var shader = Shader.Find("Archery/UnlitVertexColor");
                    if (shader == null)
                        shader = Shader.Find("Sprites/Default");
                    s_DefaultMaterial = new Material(shader) { name = "Lightning (runtime)" };
                }

                return s_DefaultMaterial;
            }
        }

        /// <param name="material">Matériau au shader « Archery/UnlitVertexColor » ; vide = créé en jeu.</param>
        public static void Spawn(Vector3 from, Vector3 to, Color color, Material material = null, float width = 0.07f, float duration = 0.3f)
        {
            LightningBolt bolt = null;
            while (bolt == null && s_Pool.Count > 0)
                bolt = s_Pool.Pop();
            if (bolt == null)
                bolt = Create();

            bolt.Show(from, to, color, material != null ? material : DefaultMaterial, width, duration);
        }

        /// <summary>Gerbe d'éclairs qui partent d'un point (explosion).</summary>
        public static void Burst(Vector3 center, float radius, Color color, Material material = null, int count = 10)
        {
            for (var i = 0; i < count; i++)
            {
                var direction = Random.onUnitSphere;
                direction.y = Mathf.Abs(direction.y) * 0.8f + 0.2f;
                Spawn(center, center + direction.normalized * (radius * Random.Range(0.6f, 1f)), color, material, 0.06f, 0.35f);
            }
        }

        static LightningBolt Create()
        {
            if (s_Root == null)
                s_Root = new GameObject("[Lightning Bolts]").transform;

            var go = new GameObject("Lightning Bolt");
            go.transform.SetParent(s_Root, false);
            var bolt = go.AddComponent<LightningBolt>();
            bolt.m_Glow = CreateLine(go);

            var core = new GameObject("Core");
            core.transform.SetParent(go.transform, false);
            bolt.m_Core = CreateLine(core);
            return bolt;
        }

        static LineRenderer CreateLine(GameObject go)
        {
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = k_Segments + 1;
            line.numCapVertices = 2;
            line.numCornerVertices = 1;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        void Show(Vector3 from, Vector3 to, Color color, Material material, float width, float duration)
        {
            m_From = from;
            m_To = to;
            m_Color = color;
            m_Jitter = Vector3.Distance(from, to) * 0.06f;
            m_Age = 0f;
            m_Duration = Mathf.Max(0.05f, duration);
            m_NextFlicker = 0f;

            m_Glow.sharedMaterial = material;
            m_Core.sharedMaterial = material;
            m_Glow.widthMultiplier = width;
            m_Core.widthMultiplier = width * 0.35f;

            gameObject.SetActive(true);
            Rebuild();
            UpdateColors();
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            if (m_Age >= m_Duration)
            {
                gameObject.SetActive(false);
                s_Pool.Push(this);
                return;
            }

            // Le zigzag change plusieurs fois par seconde : l'éclair scintille.
            m_NextFlicker -= Time.deltaTime;
            if (m_NextFlicker <= 0f)
            {
                Rebuild();
                m_NextFlicker = 0.04f;
            }

            UpdateColors();
        }

        void Rebuild()
        {
            var direction = m_To - m_From;
            var side = Vector3.Cross(direction, Vector3.up);
            if (side.sqrMagnitude < 1e-4f)
                side = Vector3.Cross(direction, Vector3.right);
            side.Normalize();
            var other = Vector3.Cross(direction.normalized, side);

            for (var i = 0; i <= k_Segments; i++)
            {
                var t = i / (float)k_Segments;
                var point = Vector3.Lerp(m_From, m_To, t);
                if (i > 0 && i < k_Segments)
                {
                    // Zigzag plus large au milieu, nul aux extrémités.
                    var amplitude = m_Jitter * Mathf.Sin(t * Mathf.PI);
                    point += (side * Random.Range(-1f, 1f) + other * Random.Range(-1f, 1f)) * amplitude;
                }

                s_Points[i] = point;
            }

            m_Glow.SetPositions(s_Points);
            m_Core.SetPositions(s_Points);
        }

        void UpdateColors()
        {
            var fade = 1f - m_Age / m_Duration;
            var glow = m_Color;
            glow.a *= 0.7f * fade;
            m_Glow.startColor = glow;
            m_Glow.endColor = glow;

            var core = Color.Lerp(m_Color, Color.white, 0.75f);
            core.a = fade;
            m_Core.startColor = core;
            m_Core.endColor = core;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Pool.Clear();
            s_Root = null;
        }
    }
}
