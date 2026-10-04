using Archery.Difficulty;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.World
{
    /// <summary>
    /// Applique le ciel de la difficulté (GDD, section 11) : skybox « Archery/StylizedSky », soleil ou lune,
    /// lumière ambiante et brouillard. Quand la difficulté change, le ciel passe de l'un à l'autre en direct.
    /// </summary>
    [DisallowMultipleComponent]
    public class SkyController : MonoBehaviour
    {
        static readonly int k_ZenithColorId = Shader.PropertyToID("_ZenithColor");
        static readonly int k_HorizonColorId = Shader.PropertyToID("_HorizonColor");
        static readonly int k_GroundColorId = Shader.PropertyToID("_GroundColor");
        static readonly int k_HorizonSharpnessId = Shader.PropertyToID("_HorizonSharpness");
        static readonly int k_DiscDirectionId = Shader.PropertyToID("_DiscDirection");
        static readonly int k_DiscColorId = Shader.PropertyToID("_DiscColor");
        static readonly int k_DiscSizeId = Shader.PropertyToID("_DiscSize");
        static readonly int k_GlowColorId = Shader.PropertyToID("_GlowColor");
        static readonly int k_GlowSizeId = Shader.PropertyToID("_GlowSize");
        static readonly int k_MoonSurfaceId = Shader.PropertyToID("_MoonSurface");
        static readonly int k_StarsId = Shader.PropertyToID("_Stars");

        [Tooltip("Matériau du ciel (shader « Archery/StylizedSky »). Vide : celui de Lighting > Environment.")]
        [SerializeField]
        Material m_SkyMaterial;

        [Tooltip("Lumière directionnelle du soleil ou de la lune. Vide : celle de cet objet, sinon la lumière « Sun » de Lighting, sinon la première de la scène.")]
        [SerializeField]
        Light m_Light;

        [Tooltip("Durée (s) du passage d'un ciel à l'autre.")]
        [SerializeField]
        float m_TransitionDuration = 3f;

        [Tooltip("Réapplique le ciel à chaque image. En Play, on peut ainsi régler les couleurs dans l'asset " +
                 "de la difficulté et voir le résultat tout de suite ; les réglages restent après Play.")]
        [SerializeField]
        bool m_LiveTuning = true;

        readonly SkySettings m_From = new SkySettings();
        readonly SkySettings m_Blend = new SkySettings();
        SkySettings m_Target;
        float m_Transition = 1f;
        Material m_RuntimeSky;

        void Start()
        {
            if (m_SkyMaterial == null)
                m_SkyMaterial = RenderSettings.skybox;

            // On travaille sur une copie : le matériau du projet n'est pas modifié en jeu.
            if (m_SkyMaterial != null && m_SkyMaterial.HasProperty(k_ZenithColorId))
            {
                m_RuntimeSky = new Material(m_SkyMaterial) { name = m_SkyMaterial.name + " (en jeu)" };
                RenderSettings.skybox = m_RuntimeSky;
            }
            else
            {
                Debug.LogWarning("SkyController : aucun matériau avec le shader « Archery/StylizedSky ». " +
                                 "Seuls la lumière, l'ambiance et le brouillard changeront.", this);
            }

            if (m_Light == null && TryGetComponent<Light>(out var ownLight) && ownLight.type == LightType.Directional)
                m_Light = ownLight;
            if (m_Light == null)
                m_Light = RenderSettings.sun != null ? RenderSettings.sun : FindDirectionalLight();
            if (m_Light != null)
                RenderSettings.sun = m_Light;

            m_Target = DifficultyManager.Current.sky;
            m_Blend.CopyFrom(m_Target);
            Apply(m_Blend);

            DifficultyManager.Changed += OnDifficultyChanged;
        }

        void OnDestroy()
        {
            DifficultyManager.Changed -= OnDifficultyChanged;
            if (m_RuntimeSky != null)
                Destroy(m_RuntimeSky);
        }

        void OnDifficultyChanged(DifficultyDefinition difficulty)
        {
            // La transition part du ciel affiché, même si la précédente n'était pas finie.
            m_From.CopyFrom(m_Blend);
            m_Target = difficulty.sky;
            m_Transition = 0f;
        }

        void Update()
        {
            if (m_Target == null)
                return;

            if (m_Transition < 1f)
            {
                m_Transition = Mathf.Min(1f, m_Transition + Time.deltaTime / Mathf.Max(0.01f, m_TransitionDuration));
                m_Blend.Lerp(m_From, m_Target, Mathf.SmoothStep(0f, 1f, m_Transition));
                Apply(m_Blend);
            }
            else if (m_LiveTuning)
            {
                m_Blend.CopyFrom(m_Target);
                Apply(m_Blend);
            }
        }

        void Apply(SkySettings sky)
        {
            if (m_RuntimeSky != null)
            {
                m_RuntimeSky.SetColor(k_ZenithColorId, sky.zenithColor);
                m_RuntimeSky.SetColor(k_HorizonColorId, sky.horizonColor);
                m_RuntimeSky.SetColor(k_GroundColorId, sky.groundColor);
                m_RuntimeSky.SetFloat(k_HorizonSharpnessId, sky.horizonSharpness);
                m_RuntimeSky.SetVector(k_DiscDirectionId, sky.DiscDirection);
                m_RuntimeSky.SetColor(k_DiscColorId, sky.discColor);
                m_RuntimeSky.SetFloat(k_DiscSizeId, sky.discSize * Mathf.Deg2Rad);
                m_RuntimeSky.SetColor(k_GlowColorId, sky.glowColor);
                m_RuntimeSky.SetFloat(k_GlowSizeId, sky.glowSize * Mathf.Deg2Rad);
                m_RuntimeSky.SetFloat(k_MoonSurfaceId, sky.moonSurface);
                m_RuntimeSky.SetFloat(k_StarsId, sky.stars);
            }

            if (m_Light != null)
            {
                m_Light.transform.rotation = sky.LightRotation;
                m_Light.color = sky.lightColor;
                m_Light.intensity = sky.lightIntensity;
                m_Light.shadowStrength = sky.shadowStrength;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky.ambientSky;
            RenderSettings.ambientEquatorColor = sky.ambientEquator;
            RenderSettings.ambientGroundColor = sky.ambientGround;
            RenderSettings.reflectionIntensity = sky.reflections;

            // Le brouillard prend la couleur de l'horizon : le décor lointain se fond dans le ciel.
            RenderSettings.fog = sky.fogDensity > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = sky.horizonColor;
            RenderSettings.fogDensity = sky.fogDensity;
        }

        static Light FindDirectionalLight()
        {
            foreach (var light in FindObjectsByType<Light>())
            {
                if (light.type == LightType.Directional)
                    return light;
            }

            return null;
        }
    }
}
