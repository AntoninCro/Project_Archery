using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Player
{
    /// <summary>
    /// Fondu au noir de la vue (téléportation, fin de partie…). Crée tout seul un voile devant la caméra,
    /// dessiné par-dessus tout le reste (shader « Archery/ScreenFade »).
    /// </summary>
    [DisallowMultipleComponent]
    public class ScreenFader : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Matériau au shader « Archery/ScreenFade ». Vide : créé en jeu (le shader risque alors de manquer dans les builds).")]
        [SerializeField]
        Material m_Material;

        [SerializeField]
        Color m_Color = Color.black;

        [Tooltip("Caméra devant laquelle placer le voile. Vide : la caméra du XR Origin, sinon la caméra principale.")]
        [SerializeField]
        Transform m_Camera;

        Renderer m_Veil;
        MaterialPropertyBlock m_PropertyBlock;
        float m_Alpha;
        float m_Target;
        float m_Speed = float.MaxValue;

        public static ScreenFader Instance { get; private set; }

        /// <summary>À mettre à vrai avant de recharger la scène : elle commencera noire puis s'éclaircira.</summary>
        public static bool FadeInOnNextLoad { get; set; }

        /// <summary>Opacité actuelle du voile (0 = transparent, 1 = noir).</summary>
        public float Alpha => m_Alpha;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Screen Fader dans la scène.", this);
            Instance = this;
            m_PropertyBlock = new MaterialPropertyBlock();
        }

        void Start()
        {
            if (m_Camera == null)
            {
                var rig = PlayerRig.Instance;
                m_Camera = rig != null && rig.Head != null ? rig.Head : Camera.main != null ? Camera.main.transform : null;
            }

            if (m_Camera == null)
            {
                Debug.LogWarning("ScreenFader : aucune caméra trouvée, pas de fondu.", this);
                return;
            }

            m_Veil = CreateVeil(m_Camera, m_Material);
            if (FadeInOnNextLoad)
            {
                FadeInOnNextLoad = false;
                m_Alpha = 1f;
                FadeTo(0f, 0.6f);
            }

            Apply();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Lance un fondu vers cette opacité (0 = transparent, 1 = noir) en <paramref name="duration"/> secondes.</summary>
        public void FadeTo(float alpha, float duration)
        {
            m_Target = Mathf.Clamp01(alpha);
            m_Speed = duration > 0f ? 1f / duration : float.MaxValue;
        }

        /// <summary>Comme <see cref="FadeTo"/>, à attendre dans une coroutine : se termine quand le fondu est fini.</summary>
        public IEnumerator Fade(float alpha, float duration)
        {
            FadeTo(alpha, duration);
            while (!Mathf.Approximately(m_Alpha, m_Target))
                yield return null;
        }

        void LateUpdate()
        {
            if (Mathf.Approximately(m_Alpha, m_Target))
                return;

            m_Alpha = Mathf.MoveTowards(m_Alpha, m_Target, m_Speed * Time.unscaledDeltaTime);
            Apply();
        }

        void Apply()
        {
            if (m_Veil == null)
                return;

            m_Veil.enabled = m_Alpha > 0.001f;
            var color = m_Color;
            color.a *= m_Alpha;
            m_Veil.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor(k_BaseColorId, color);
            m_Veil.SetPropertyBlock(m_PropertyBlock);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => FadeInOnNextLoad = false;

        // Un grand carré à 10 cm devant les yeux : il couvre tout le champ de vision.
        static Renderer CreateVeil(Transform camera, Material material)
        {
            if (material == null)
            {
                var shader = Shader.Find("Archery/ScreenFade");
                if (shader == null)
                {
                    Debug.LogWarning("ScreenFader : shader « Archery/ScreenFade » introuvable, pas de fondu.");
                    return null;
                }

                material = new Material(shader) { name = "Screen Fade (runtime)" };
            }

            var mesh = new Mesh
            {
                name = "Screen Fade Quad",
                vertices = new[] { new Vector3(-1f, -1f, 0f), new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(1f, -1f, 0f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            mesh.RecalculateBounds();

            var veil = new GameObject("Screen Fade");
            veil.transform.SetParent(camera, false);
            veil.transform.localPosition = new Vector3(0f, 0f, 0.1f);
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
