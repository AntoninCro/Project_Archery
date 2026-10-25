using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Combat
{
    /// <summary>
    /// L'onde de choc d'un coup de poing de la main de la matrice (24 octobre, après le test : la faille s'ouvre au bout de
    /// la lame, hors de portée de la main ; l'onde va la chercher) : deux anneaux de lumière, le second un peu en retrait,
    /// qui partent du poing, filent droit devant en s'élargissant et s'éteignent, en grésillant comme la matrice. Chaque
    /// faille qu'ils atteignent est propulsée (<see cref="TryHit"/> : c'est <see cref="Katana"/> qui la lance).
    /// </summary>
    /// <remarks>Shader <c>Archery/Glow</c> (le matériau de la traînée du katana) : <c>_Color</c> et <c>_Intensity</c> par bloc de propriétés.</remarks>
    [DisallowMultipleComponent]
    public sealed class ShockWave : MonoBehaviour
    {
        // L'anneau : 48 segments, son bord intérieur à 80 % de son rayon ; le second anneau suit à 18 cm, moitié moins vif.
        const int k_Segments = 48;
        const float k_Inner = 0.8f;
        const float k_Lag = 0.18f;
        const float k_SecondStrength = 0.55f;

        // Il touche aussi une faille un peu derrière le poing (le poing est déjà devant elle).
        const float k_BehindReach = 0.3f;

        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static Mesh s_Ring;

        readonly HashSet<MatrixRift> m_Hit = new HashSet<MatrixRift>();
        Vector3 m_Origin;
        Vector3 m_Direction;
        float m_Speed;
        float m_Range;
        float m_Radius;
        float m_Growth;
        float m_Front;
        float m_Intensity;
        Color m_Color;
        Transform m_First;
        Transform m_Second;
        MeshRenderer m_FirstRenderer;
        MeshRenderer m_SecondRenderer;
        MaterialPropertyBlock m_Block;

        /// <summary>Sa direction (le coup de poing).</summary>
        public Vector3 Direction => m_Direction;

        /// <summary>
        /// Une onde qui part de <paramref name="origin"/> vers <paramref name="direction"/> à <paramref name="speed"/> m/s, sur
        /// <paramref name="range"/> m, de rayon <paramref name="radius"/> m au départ et <paramref name="growth"/> m de plus par
        /// mètre parcouru. Sans matériau, elle propulse quand même les failles, sans se voir.
        /// </summary>
        public static ShockWave Spawn(Vector3 origin, Vector3 direction, Material material, Color color, float speed, float range,
                                      float radius, float growth)
        {
            if (direction.sqrMagnitude < 1e-6f)
                return null;

            var wave = new GameObject("Onde de choc").AddComponent<ShockWave>();
            wave.m_Origin = origin;
            wave.m_Direction = direction.normalized;
            wave.m_Speed = speed;
            wave.m_Range = Mathf.Max(0.1f, range);
            wave.m_Radius = radius;
            wave.m_Growth = growth;
            wave.m_Color = color;
            if (material == null)
                return wave;
            wave.m_Intensity = material.HasProperty(k_IntensityId) ? material.GetFloat(k_IntensityId) : 2f;
            wave.m_Block = new MaterialPropertyBlock();
            wave.m_First = wave.AddRing("Anneau", material, out wave.m_FirstRenderer);
            wave.m_Second = wave.AddRing("Anneau en retrait", material, out wave.m_SecondRenderer);
            wave.Place(wave.m_First, wave.m_FirstRenderer, 0f, 0f, 1f);
            wave.Place(wave.m_Second, wave.m_SecondRenderer, -k_Lag, 0f, k_SecondStrength);
            return wave;
        }

        /// <summary>Son rayon (m) à <paramref name="along"/> m du poing.</summary>
        public float RadiusAt(float along) => m_Radius + m_Growth * Mathf.Max(0f, along);

        /// <summary>
        /// L'onde atteint <paramref name="rift"/> en ce moment (son front l'a dépassée, et elle passe assez près) : true une
        /// seule fois par faille.
        /// </summary>
        public bool TryHit(MatrixRift rift)
        {
            if (rift == null || m_Hit.Contains(rift))
                return false;
            var along = Vector3.Dot(rift.transform.position - m_Origin, m_Direction);
            if (along > m_Front || along < -k_BehindReach)
                return false;
            var axis = m_Origin + m_Direction * Mathf.Clamp(along, 0f, m_Front);
            if (rift.DistanceTo(axis) > RadiusAt(along))
                return false;
            m_Hit.Add(rift);
            return true;
        }

        void Update()
        {
            m_Front += m_Speed * Time.deltaTime;
            var t = m_Front / m_Range;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            Place(m_First, m_FirstRenderer, m_Front, t, 1f);
            Place(m_Second, m_SecondRenderer, m_Front - k_Lag, t, k_SecondStrength);
        }

        // Un anneau à along m du poing : à son rayon, face à sa course, qui s'éteint en avançant ; il grésille (il tourne et
        // tremble un peu, et sa lueur vacille, à chaque image).
        void Place(Transform ring, MeshRenderer renderer, float along, float t, float strength)
        {
            if (renderer == null)
                return;
            var visible = along >= 0f;
            renderer.enabled = visible;
            if (!visible)
                return;
            var radius = RadiusAt(along) * Random.Range(0.97f, 1.03f);
            ring.SetPositionAndRotation(m_Origin + m_Direction * along,
                                        Quaternion.LookRotation(m_Direction) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            ring.localScale = Vector3.one * radius;
            var fade = Mathf.Pow(1f - t, 1.5f);
            renderer.GetPropertyBlock(m_Block);
            m_Block.SetColor(k_ColorId, m_Color);
            m_Block.SetFloat(k_IntensityId, m_Intensity * strength * fade * Random.Range(0.8f, 1.2f));
            renderer.SetPropertyBlock(m_Block);
        }

        Transform AddRing(string ringName, Material material, out MeshRenderer renderer)
        {
            var ring = new GameObject(ringName);
            ring.transform.SetParent(transform, false);
            ring.AddComponent<MeshFilter>().sharedMesh = RingMesh();
            renderer = ring.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ring.transform;
        }

        // Un anneau plat de rayon 1, face à +Z : son bord intérieur à 80 %, uv.x de 0 (dedans) à 1 (dehors) pour la douceur
        // des bords du shader Glow.
        static Mesh RingMesh()
        {
            if (s_Ring != null)
                return s_Ring;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            for (var i = 0; i <= k_Segments; i++)
            {
                var angle = i * Mathf.PI * 2f / k_Segments;
                var around = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices.Add(around * k_Inner);
                vertices.Add(around);
                uvs.Add(new Vector2(0f, i / (float)k_Segments));
                uvs.Add(new Vector2(1f, i / (float)k_Segments));
                colors.Add(Color.white);
                colors.Add(Color.white);
                if (i == 0)
                    continue;
                var a = (i - 1) * 2;
                var b = i * 2;
                triangles.Add(a);
                triangles.Add(a + 1);
                triangles.Add(b);
                triangles.Add(a + 1);
                triangles.Add(b + 1);
                triangles.Add(b);
            }

            s_Ring = new Mesh { name = "Anneau de l'onde de choc" };
            s_Ring.SetVertices(vertices);
            s_Ring.SetUVs(0, uvs);
            s_Ring.SetColors(colors);
            s_Ring.SetTriangles(triangles, 0);
            s_Ring.RecalculateBounds();
            return s_Ring;
        }
    }
}
