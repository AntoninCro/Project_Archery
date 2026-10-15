using Archery.Difficulty;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        static readonly int k_AuroraId = Shader.PropertyToID("_Aurora");
        static readonly int k_AuroraColorId = Shader.PropertyToID("_AuroraColor");
        static readonly int k_ShootingStarsId = Shader.PropertyToID("_ShootingStars");
        static readonly int k_NightId = Shader.PropertyToID("_ArcheryNight");
        static readonly int k_FlashId = Shader.PropertyToID("_ArcheryFlash");

        /// <summary>
        /// Éclat des lumières de la carte en ce moment (0 = plein jour, 1 = pleine nuit) : il suit les transitions du ciel.
        /// Les shaders le lisent aussi (_ArcheryNight).
        /// </summary>
        public static float Lamps { get; private set; } = 0.7f;

        /// <summary>Le ciel affiché en ce moment (null avant le lancement).</summary>
        public static SkySettings Current { get; private set; }

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
        CloudLayer m_Clouds;
        MapLighting m_Lighting;
        ColorAdjustments m_Grading;
        float m_BaseSaturation;
        float m_BaseContrast;
        float m_Flash;
        float m_FlashStrength = 1f;
        float m_FlashTime = -100f;
        float m_NextLightning = -1f;
        float m_ThunderTime = -1f;

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

            // Des nuages en blocs, de la couleur du ciel, et les lumières de la carte (torches, lucioles…).
            m_Clouds = CloudLayer.Create();
            m_Lighting = MapLighting.Create();
            FindGrading();

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
            if (m_Clouds != null)
                Destroy(m_Clouds.gameObject);
            if (m_Lighting != null)
                Destroy(m_Lighting.gameObject);
            Shader.SetGlobalFloat(k_FlashId, 0f);
        }

        // L'étalonnage de la carte (volume global avec un Color Adjustments) : la teinte du ciel s'ajoute à ses réglages.
        // On travaille sur une copie du profil : l'asset n'est pas modifié.
        void FindGrading()
        {
            foreach (var volume in FindObjectsByType<Volume>())
            {
                if (!volume.isGlobal || volume.sharedProfile == null || !volume.sharedProfile.Has<ColorAdjustments>())
                    continue;

                if (volume.profile.TryGet(out m_Grading))
                {
                    m_BaseSaturation = m_Grading.saturation.overrideState ? m_Grading.saturation.value : 0f;
                    m_BaseContrast = m_Grading.contrast.overrideState ? m_Grading.contrast.value : 0f;
                    return;
                }
            }
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

            // Un éclair éclaire le ciel et la carte un instant : on réapplique le ciel tant qu'il dure.
            var wasFlashing = m_Flash > 0.001f;
            UpdateLightning(m_Blend);
            if (wasFlashing || m_Flash > 0.001f)
                Apply(m_Blend);
        }

        // Éclairs au loin (ciel rouge) : un double flash, puis le tonnerre une ou deux secondes plus tard.
        void UpdateLightning(SkySettings sky)
        {
            if (sky.lightning <= 0.01f)
            {
                m_NextLightning = -1f;
                m_Flash = 0f;
                Shader.SetGlobalFloat(k_FlashId, 0f);
                return;
            }

            var now = Time.time;
            var meanInterval = 60f / sky.lightning;
            if (m_NextLightning < 0f)
                m_NextLightning = now + Random.Range(0.3f, 1.2f) * meanInterval;

            if (now >= m_NextLightning)
            {
                m_FlashTime = now;
                m_FlashStrength = Random.Range(0.55f, 1f);
                m_NextLightning = now + Random.Range(0.4f, 1.6f) * meanInterval;
                m_ThunderTime = now + Random.Range(0.8f, 2.5f);
            }

            var t = now - m_FlashTime;
            var flash = t < 0.07f ? 1f : t < 0.14f ? 0.2f : t < 0.24f ? 0.75f : 0.75f * Mathf.Exp(-(t - 0.24f) * 9f);
            m_Flash = flash < 0.002f ? 0f : flash * m_FlashStrength;
            Shader.SetGlobalFloat(k_FlashId, m_Flash);

            if (m_ThunderTime > 0f && now >= m_ThunderTime)
            {
                m_ThunderTime = -1f;
                if (m_Lighting != null)
                    m_Lighting.PlayThunder(m_FlashStrength);
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
                m_RuntimeSky.SetFloat(k_AuroraId, sky.aurora);
                m_RuntimeSky.SetColor(k_AuroraColorId, sky.auroraColor);
                m_RuntimeSky.SetFloat(k_ShootingStarsId, sky.shootingStars);
            }

            if (m_Light != null)
            {
                m_Light.transform.rotation = sky.LightRotation;
                m_Light.color = sky.lightColor;
                m_Light.intensity = sky.lightIntensity;
                m_Light.shadowStrength = sky.shadowStrength;
            }

            // Pendant un éclair, toute la carte s'éclaire d'une lumière blanc-bleu.
            var flash = new Color(0.55f, 0.58f, 0.7f) * m_Flash;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = sky.ambientSky + flash;
            RenderSettings.ambientEquatorColor = sky.ambientEquator + flash * 0.8f;
            RenderSettings.ambientGroundColor = sky.ambientGround + flash * 0.4f;
            RenderSettings.reflectionIntensity = sky.reflections;

            // Le brouillard prend la couleur de l'horizon : le décor lointain se fond dans le ciel.
            RenderSettings.fog = sky.fogDensity > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = sky.horizonColor;
            RenderSettings.fogDensity = sky.fogDensity;

            if (m_Clouds != null)
                m_Clouds.SetSky(sky);

            // Les lumières de la carte : plus vives la nuit.
            Lamps = sky.lamps;
            Current = sky;
            Shader.SetGlobalFloat(k_NightId, sky.lamps);

            // L'étalonnage de l'image : la teinte du ciel, ajoutée aux réglages du volume de la carte.
            if (m_Grading != null)
            {
                m_Grading.colorFilter.Override(sky.colorFilter);
                m_Grading.saturation.Override(Mathf.Clamp(m_BaseSaturation + sky.saturation, -100f, 100f));
                m_Grading.contrast.Override(Mathf.Clamp(m_BaseContrast + sky.contrast, -100f, 100f));
            }
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
