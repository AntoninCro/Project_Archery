using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Combat
{
    /// <summary>
    /// La traînée de la lame du katana (24 octobre) : un ruban de lumière entre la garde et la pointe, sur les dernières
    /// 0,12 s, dans le monde. Sa force suit la vitesse de la lame (posée à chaque image par <see cref="Katana"/>) : éteinte au
    /// repos, elle s'allume quand la lame va assez vite pour trancher, et prend la couleur de la faille en cours.
    /// </summary>
    /// <remarks>Shader <c>Archery/Glow</c> : couleur des sommets, alpha = force, plus douce vers la garde et la pointe.</remarks>
    [DisallowMultipleComponent]
    public sealed class BladeTrail : MonoBehaviour
    {
        const float k_Duration = 0.12f;
        const int k_MaxSamples = 32;

        struct Sample
        {
            public Vector3 Base;
            public Vector3 Tip;
            public float Time;
            public float Strength;
            public Color Color;
        }

        readonly List<Sample> m_Samples = new List<Sample>();
        readonly List<Vector3> m_Vertices = new List<Vector3>();
        readonly List<Vector2> m_Uvs = new List<Vector2>();
        readonly List<Color> m_Colors = new List<Color>();
        readonly List<int> m_Triangles = new List<int>();
        Mesh m_Mesh;
        MeshRenderer m_Renderer;

        /// <summary>Une traînée (dans le monde), avec <paramref name="material"/> ; null si le matériau manque.</summary>
        public static BladeTrail Create(Material material)
        {
            if (material == null)
                return null;
            var go = new GameObject("Traînée du katana");
            var trail = go.AddComponent<BladeTrail>();
            trail.m_Mesh = new Mesh { name = "Traînée du katana" };
            trail.m_Mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = trail.m_Mesh;
            trail.m_Renderer = go.AddComponent<MeshRenderer>();
            trail.m_Renderer.sharedMaterial = material;
            trail.m_Renderer.shadowCastingMode = ShadowCastingMode.Off;
            trail.m_Renderer.receiveShadows = false;
            return trail;
        }

        /// <summary>La lame est là à cette image ; <paramref name="strength"/> de 0 (éteinte) à 1 (elle tranche).</summary>
        public void Add(Vector3 bladeBase, Vector3 tip, float strength, Color color)
        {
            m_Samples.Add(new Sample { Base = bladeBase, Tip = tip, Time = Time.time, Strength = Mathf.Clamp01(strength), Color = color });
            if (m_Samples.Count > k_MaxSamples)
                m_Samples.RemoveAt(0);
        }

        /// <summary>Efface la traînée (katana lâché, rangé).</summary>
        public void Clear() => m_Samples.Clear();

        void OnDestroy()
        {
            if (m_Mesh != null)
                Destroy(m_Mesh);
        }

        void LateUpdate()
        {
            while (m_Samples.Count > 0 && Time.time - m_Samples[0].Time > k_Duration)
                m_Samples.RemoveAt(0);

            m_Vertices.Clear();
            m_Uvs.Clear();
            m_Colors.Clear();
            m_Triangles.Clear();
            m_Mesh.Clear();
            if (m_Samples.Count < 2)
                return;

            // La traînée s'efface vers son bout le plus ancien.
            for (var i = 0; i < m_Samples.Count; i++)
            {
                var sample = m_Samples[i];
                var fade = 1f - (Time.time - sample.Time) / k_Duration;
                var color = sample.Color;
                color.a = sample.Strength * fade * fade;
                m_Vertices.Add(sample.Base);
                m_Vertices.Add(sample.Tip);
                m_Uvs.Add(new Vector2(0f, fade));
                m_Uvs.Add(new Vector2(1f, fade));
                m_Colors.Add(color);
                m_Colors.Add(color);
                if (i == 0)
                    continue;
                var a = (i - 1) * 2;
                var b = i * 2;
                m_Triangles.Add(a);
                m_Triangles.Add(a + 1);
                m_Triangles.Add(b);
                m_Triangles.Add(a + 1);
                m_Triangles.Add(b + 1);
                m_Triangles.Add(b);
            }

            m_Mesh.SetVertices(m_Vertices);
            m_Mesh.SetUVs(0, m_Uvs);
            m_Mesh.SetColors(m_Colors);
            m_Mesh.SetTriangles(m_Triangles, 0);
            m_Mesh.RecalculateBounds();
        }
    }
}
