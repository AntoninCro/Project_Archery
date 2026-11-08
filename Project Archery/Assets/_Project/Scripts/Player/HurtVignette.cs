using Archery.Combat;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Player
{
    /// <summary>
    /// Voile rouge autour de la vision (GDD, section 14) : plus le joueur a perdu de PV, plus il est rouge et plus il
    /// s'approche du centre de la vue. Il suit vite les PV qui baissent, lentement ceux qui remontent ; sous 30 % des PV (le danger,
    /// 4 novembre), il est presque à son plus fort et bat comme un cœur.
    /// Il disparaît quand les PV remontent, et à la mort (l'écran de fin reste lisible).
    /// </summary>
    /// <remarks>
    /// Le <see cref="PlayerHealth"/> l'ajoute tout seul au XR Origin. Il dessine une sphère autour de la tête,
    /// avec le matériau <c>Resources/HurtVignette</c> (shader « Archery/HurtVignette »).
    /// </remarks>
    [DisallowMultipleComponent]
    public class HurtVignette : MonoBehaviour
    {
        const string k_MaterialResource = "HurtVignette";
        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_StartId = Shader.PropertyToID("_Start");

        [Tooltip("Matériau au shader « Archery/HurtVignette ». Vide : celui de Resources/HurtVignette.")]
        [SerializeField]
        Material m_Material;

        [Tooltip("Opacité maximale du rouge, au bord de la vue, quand il ne reste presque plus de PV (0,75 avant le 4 novembre).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_MaxOpacity = 0.95f;

        [Tooltip("Part de PV perdus en dessous de laquelle il n'y a pas de voile (0,1 : rien tant qu'il reste 90 % des PV).")]
        [Range(0f, 0.5f)]
        [SerializeField]
        float m_Threshold = 0.1f;

        [Tooltip("Temps (s) que met le voile à suivre les PV qui remontent.")]
        [SerializeField]
        float m_Smoothing = 0.8f;

        [Tooltip("Temps (s) que met le voile à suivre les PV qui baissent : vite, qu'il soit là au moment du danger (4 novembre ; avant, " +
                 "le même que pour la remontée, 0,8 s : un gros coup n'était montré qu'après coup).")]
        [SerializeField]
        float m_RiseSmoothing = 0.12f;

        [Tooltip("Sous cette part de PV restants, le danger : le voile est presque à son plus fort et bat comme un cœur.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_PulseBelow = 0.3f;

        [Tooltip("Dans le danger : la force du rouge au moins (part de l'opacité maximale), et le creux de chaque battement (part du " +
                 "rouge). Le joueur, 4 novembre : « on ne voit pas assez le rouge sur le côté de l'écran quand on est sur le point de " +
                 "mourir » (avant : 0,8 à 1 au battement, sans plancher).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_DangerIntensity = 0.9f;

        [Range(0f, 1f)]
        [SerializeField]
        float m_PulseLow = 0.55f;

        Health m_Health;
        Renderer m_Veil;
        MaterialPropertyBlock m_PropertyBlock;
        float m_Amount;

        void Start()
        {
            m_Health = GetComponent<Health>();
            if (m_Health == null && PlayerHealth.Instance != null)
                m_Health = PlayerHealth.Instance.Health;

            var rig = PlayerRig.Instance;
            var head = rig != null && rig.Head != null ? rig.Head : Camera.main != null ? Camera.main.transform : null;
            var material = m_Material != null ? m_Material : Resources.Load<Material>(k_MaterialResource);
            if (material == null)
            {
                var shader = Shader.Find("Archery/HurtVignette");
                if (shader != null)
                    material = new Material(shader) { name = "Hurt Vignette (runtime)" };
            }

            if (head == null || material == null || m_Health == null)
            {
                Debug.LogWarning("HurtVignette : il manque la caméra, les PV ou le matériau Resources/HurtVignette : pas de voile rouge.", this);
                enabled = false;
                return;
            }

            m_PropertyBlock = new MaterialPropertyBlock();
            m_Veil = CreateSphere(head, material);
            Apply(0f, 0f);
        }

        void LateUpdate()
        {
            var alive = m_Health.IsAlive && m_Health.Max > 0f;
            var remaining = alive ? m_Health.Current / m_Health.Max : 1f;
            var target = Mathf.Clamp01((1f - remaining - m_Threshold) / Mathf.Max(0.01f, 1f - m_Threshold));
            var smoothing = target > m_Amount ? m_RiseSmoothing : m_Smoothing;
            m_Amount = Mathf.Lerp(m_Amount, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.01f, smoothing)));

            // Le danger (4 novembre) : le rouge presque à son plus fort, qui bat nettement ; il ne s'approche pas plus du centre
            // pour autant (on doit pouvoir viser).
            var intensity = Mathf.Clamp01(m_Amount * 1.2f);
            if (alive && remaining < m_PulseBelow)
            {
                var beat = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f);
                intensity = Mathf.Max(intensity, m_DangerIntensity) * Mathf.Lerp(m_PulseLow, 1f, beat);
            }

            Apply(m_Amount, intensity);
        }

        void Apply(float spread, float intensity)
        {
            if (m_Veil == null)
                return;

            m_Veil.enabled = intensity > 0.005f;
            if (!m_Veil.enabled)
                return;

            // Le rouge commence au bord de la vue (environ 55° du centre) et s'approche jusqu'à environ 20° du centre.
            m_Veil.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetFloat(k_IntensityId, intensity * m_MaxOpacity);
            m_PropertyBlock.SetFloat(k_StartId, Mathf.Lerp(0.42f, 0.06f, spread));
            m_Veil.SetPropertyBlock(m_PropertyBlock);
        }

        // Une sphère de 40 cm de rayon autour des yeux : elle couvre tout le champ de vision, sous le fondu au noir.
        static Renderer CreateSphere(Transform head, Material material)
        {
            const int rings = 12;
            const int segments = 24;
            const float radius = 0.4f;
            var vertices = new Vector3[(rings + 1) * (segments + 1)];
            var triangles = new int[rings * segments * 6];
            for (var r = 0; r <= rings; r++)
            {
                var polar = Mathf.PI * r / rings;
                for (var s = 0; s <= segments; s++)
                {
                    var azimuth = 2f * Mathf.PI * s / segments;
                    vertices[r * (segments + 1) + s] = radius * new Vector3(
                        Mathf.Sin(polar) * Mathf.Cos(azimuth), Mathf.Sin(polar) * Mathf.Sin(azimuth), Mathf.Cos(polar));
                }
            }

            var t = 0;
            for (var r = 0; r < rings; r++)
            {
                for (var s = 0; s < segments; s++)
                {
                    var a = r * (segments + 1) + s;
                    var b = a + segments + 1;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = b + 1;
                }
            }

            var mesh = new Mesh { name = "Hurt Vignette Sphere", vertices = vertices, triangles = triangles };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);

            var veil = new GameObject("Hurt Vignette");
            veil.transform.SetParent(head, false);
            veil.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = veil.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }
    }
}
