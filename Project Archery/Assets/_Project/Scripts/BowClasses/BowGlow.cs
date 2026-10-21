using UnityEngine;

namespace Archery.BowClasses
{
    /// <summary>
    /// Fait briller les bandelettes d'un arc (arc puissant en rouge, arc runique en bleu) : leur émission pulse
    /// doucement, et une petite lumière de la même couleur éclaire la main et la flèche.
    /// </summary>
    /// <remarks>
    /// À placer sur la racine du modèle de l'arc (le prefab avec le <see cref="Bows.BowVisual"/>). Le matériau des
    /// bandelettes doit avoir la case <i>Emission</i> cochée et la carte d'émission du pack (<c>T_Main_Emissive</c>) :
    /// seules les zones blanches de cette carte s'allument. Le bloom de la carte fait le halo autour.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BowGlow : MonoBehaviour
    {
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Couleur des bandelettes.")]
        [SerializeField]
        Color m_Color = new Color(1f, 0.12f, 0.05f);

        [Tooltip("Force de l'émission. Au-dessus de 1, le bloom ajoute un halo (3 à 6 : bien visible).")]
        [Min(0f)]
        [SerializeField]
        float m_Intensity = 4f;

        [Tooltip("Variation de la pulsation, en part de l'intensité (0 = lumière fixe).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_PulseAmount = 0.35f;

        [Tooltip("Pulsations par seconde.")]
        [Min(0f)]
        [SerializeField]
        float m_PulseSpeed = 0.8f;

        [Header("Lumière")]
        [Tooltip("Ajoute une petite lumière ponctuelle au milieu de l'arc.")]
        [SerializeField]
        bool m_AddLight = true;

        [Tooltip("Portée de la lumière (m).")]
        [Min(0.05f)]
        [SerializeField]
        float m_LightRange = 0.6f;

        [Min(0f)]
        [SerializeField]
        float m_LightIntensity = 1.2f;

        Renderer[] m_Renderers;
        MaterialPropertyBlock m_Block;
        Light m_Light;
        float m_Phase;

        public Color Color => m_Color;

        void Awake()
        {
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            m_Block = new MaterialPropertyBlock();
            // Deux arcs côte à côte (menu des arcs) ne pulsent pas en même temps.
            m_Phase = Random.value * Mathf.PI * 2f;

            if (m_AddLight)
            {
                var go = new GameObject("Lueur");
                go.transform.SetParent(transform, false);
                m_Light = go.AddComponent<Light>();
                m_Light.type = LightType.Point;
                m_Light.color = m_Color;
                m_Light.range = m_LightRange;
                m_Light.shadows = LightShadows.None;
                m_Light.renderMode = LightRenderMode.ForcePixel;
            }
        }

        void OnEnable()
        {
            if (m_Light != null)
                m_Light.enabled = true;
            Apply(Time.time);
        }

        void OnDisable()
        {
            if (m_Light != null)
                m_Light.enabled = false;
            Apply(0f, true);
        }

        void LateUpdate() => Apply(Time.time);

        void Apply(float time, bool off = false)
        {
            if (m_Renderers == null)
                return;

            var pulse = 1f - m_PulseAmount * 0.5f * (1f + Mathf.Sin(time * m_PulseSpeed * Mathf.PI * 2f + m_Phase));
            var strength = off ? 0f : m_Intensity * pulse;
            var emission = m_Color * strength;
            emission.a = 1f;

            var shown = false;
            foreach (var target in m_Renderers)
            {
                if (target == null || target is ParticleSystemRenderer || target is TrailRenderer || target is LineRenderer)
                    continue;
                target.GetPropertyBlock(m_Block);
                m_Block.SetColor(k_EmissionColorId, emission);
                target.SetPropertyBlock(m_Block);
                shown |= target.enabled && target.gameObject.activeInHierarchy;
            }

            // Arc rangé (construction au marteau) : ses rendus sont cachés, la lumière aussi.
            if (m_Light != null)
            {
                m_Light.enabled = shown && !off;
                m_Light.intensity = off ? 0f : m_LightIntensity * pulse;
            }
        }

        void OnValidate()
        {
            if (m_Light != null)
            {
                m_Light.color = m_Color;
                m_Light.range = m_LightRange;
            }
        }
    }
}
