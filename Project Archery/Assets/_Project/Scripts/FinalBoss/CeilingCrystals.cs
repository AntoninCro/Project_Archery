using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// La myriade de petits cristaux du plafond de l'arène : la seule source de lumière de la grotte (GDD, section 23).
    /// Ils sont répartis en quatre quarts (par secteur, dans le sens des aiguilles d'une montre en partant du dragon)
    /// qui s'allument chacun sur un temps de la musique, puis pulsent au rythme pendant le combat.
    /// </summary>
    /// <remarks>
    /// Chaque quart a son maillage, son matériau (shader « Archery/Crystal ») et une lumière ponctuelle qui éclaire
    /// vraiment la grotte. L'<see cref="ArenaDirector"/> pilote leur éclat : <see cref="SetLevel"/>, <see cref="Pulse"/>,
    /// <see cref="SetTint"/>. Dans l'éditeur, ils sont allumés pour qu'on les voie ; en jeu, ils commencent éteints.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class CeilingCrystals : MonoBehaviour
    {
        public const int GroupCount = 4;

        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_TintId = Shader.PropertyToID("_Tint");

        [SerializeField]
        ArenaShape m_Arena;

        [SerializeField]
        Material m_CrystalMaterial;

        [Header("Répartition")]
        [Tooltip("Nombre de grappes de cristaux.")]
        [SerializeField]
        int m_Clusters = 420;

        [Tooltip("Cristaux par grappe (au plus).")]
        [SerializeField]
        int m_PerCluster = 6;

        [Tooltip("Taille d'un cristal (m) : plus petite et plus grande.")]
        [SerializeField]
        Vector2 m_Size = new Vector2(0.07f, 0.24f);

        [Tooltip("Les cristaux couvrent le plafond jusqu'à cette distance du centre (part du rayon du sol).")]
        [Range(0.3f, 1f)]
        [SerializeField]
        float m_Coverage = 0.86f;

        [SerializeField]
        int m_Seed = 11;

        [SerializeField]
        Color[] m_Palette =
        {
            new Color(1f, 0.2f, 0.25f), new Color(1f, 0.55f, 0.15f), new Color(0.95f, 0.25f, 0.65f),
            new Color(0.65f, 0.3f, 1f), new Color(1f, 0.75f, 0.8f),
        };

        [Header("Éclat")]
        [Tooltip("Éclat des cristaux allumés (au-dessus de 1, ils brillent avec le bloom).")]
        [SerializeField]
        float m_LitIntensity = 2.2f;

        [Tooltip("Lumière ponctuelle de chaque quart, allumé.")]
        [SerializeField]
        float m_LightIntensity = 3f;

        [SerializeField]
        float m_LightRange = 34f;

        [SerializeField]
        Color m_LightColor = new Color(1f, 0.42f, 0.35f);

        GeneratedRoot m_Generated;
        readonly Material[] m_Materials = new Material[GroupCount];
        readonly Light[] m_Lights = new Light[GroupCount];
        readonly float[] m_Levels = new float[GroupCount];
        float m_Pulse;
        Color m_Tint = Color.white;
        bool m_Dirty;

        void OnEnable()
        {
            m_Generated ??= new GeneratedRoot(transform, "Cristaux (générés)");
            Build();
        }

        void OnDisable() => m_Generated?.Clear();

        void OnValidate() => m_Dirty = true;

        void Update()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                Build();
            }
        }

        /// <summary>Éclat d'un quart, de 0 (éteint) à 1 (allumé).</summary>
        public void SetLevel(int group, float level)
        {
            if (group < 0 || group >= GroupCount)
                return;

            m_Levels[group] = Mathf.Max(0f, level);
            Apply(group);
        }

        /// <summary>Le même éclat pour tous les quarts.</summary>
        public void SetAllLevels(float level)
        {
            for (var i = 0; i < GroupCount; i++)
                SetLevel(i, level);
        }

        public float GetLevel(int group) => group >= 0 && group < GroupCount ? m_Levels[group] : 0f;

        /// <summary>Surcroît d'éclat passager (un temps de la musique), de 0 à 1 environ.</summary>
        public void Pulse(float amount)
        {
            m_Pulse = Mathf.Max(0f, amount);
            for (var i = 0; i < GroupCount; i++)
                Apply(i);
        }

        /// <summary>Couleur qui multiplie celle des cristaux (blanc : leurs couleurs ; rouge : dragon enragé).</summary>
        public void SetTint(Color tint)
        {
            m_Tint = tint;
            for (var i = 0; i < GroupCount; i++)
                Apply(i);
        }

        void Apply(int group)
        {
            var level = m_Levels[group] * (1f + m_Pulse);
            var material = m_Materials[group];
            if (material != null)
            {
                material.SetFloat(k_IntensityId, level * m_LitIntensity);
                material.SetColor(k_TintId, m_Tint);
            }

            var light = m_Lights[group];
            if (light != null)
            {
                light.intensity = level * m_LightIntensity;
                light.color = m_LightColor * m_Tint;
                light.enabled = level > 0.001f;
            }
        }

        // ---------------------------------------------------------------- fabrication

        void Build()
        {
            if (m_Generated == null)
                return;

            var arena = m_Arena != null ? m_Arena : GetComponentInParent<ArenaShape>();
            var root = m_Generated.Begin();
            if (arena == null || m_CrystalMaterial == null)
                return;

            var random = new System.Random(m_Seed);
            var builders = new MeshBuilder[GroupCount];
            for (var i = 0; i < GroupCount; i++)
                builders[i] = new MeshBuilder();

            var maxRadius = arena.CeilingRadius * m_Coverage;
            for (var c = 0; c < m_Clusters; c++)
            {
                // Plus de grappes vers le bord (même densité au mètre carré), mais au-dessus du sol de combat.
                var radius = Mathf.Sqrt((float)random.NextDouble()) * maxRadius;
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var floorPoint = arena.transform.TransformPoint(new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius));
                var ceiling = arena.CeilingPoint(floorPoint, out var inward);
                var group = GroupOf(angle);
                var color = m_Palette.Length > 0 ? m_Palette[random.Next(m_Palette.Length)] : Color.white;
                var count = 1 + random.Next(Mathf.Max(1, m_PerCluster));

                for (var k = 0; k < count; k++)
                {
                    var jitter = new Vector3((float)random.NextDouble() - 0.5f, 0f, (float)random.NextDouble() - 0.5f) * 0.6f;
                    var axis = (inward + new Vector3((float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f, (float)random.NextDouble() - 0.5f) * 0.9f).normalized;
                    var size = Mathf.Lerp(m_Size.x, m_Size.y, Mathf.Pow((float)random.NextDouble(), 2f));
                    var basePoint = transform.InverseTransformPoint(ceiling + arena.transform.TransformDirection(jitter) + inward * 0.05f);
                    var localAxis = transform.InverseTransformDirection(axis);
                    var shade = color * Mathf.Lerp(0.8f, 1.1f, (float)random.NextDouble());
                    shade.a = 1f;
                    builders[group].Crystal(basePoint, localAxis, size, size * 0.32f, 5, shade, (float)random.NextDouble() * 6f, (float)random.NextDouble());
                }
            }

            for (var i = 0; i < GroupCount; i++)
            {
                m_Materials[i] = m_Generated.CloneMaterial(m_CrystalMaterial, $"Cristaux {i + 1}");
                m_Generated.CreateMesh($"Cristaux {i + 1}", root, builders[i].ToMesh($"Cristaux du plafond {i + 1}"), m_Materials[i], collider: false);

                // Une lumière par quart, sous la voûte, au milieu de son secteur.
                var lightAngle = (i + 0.5f) * Mathf.PI * 0.5f - Mathf.PI * 0.25f;
                var lightFloor = arena.transform.TransformPoint(new Vector3(Mathf.Sin(lightAngle), 0f, Mathf.Cos(lightAngle)) * maxRadius * 0.5f);
                var lightPoint = arena.CeilingPoint(lightFloor, out _) - arena.transform.up * 3f;
                var lightObject = m_Generated.Create($"Lumière des cristaux {i + 1}", root);
                lightObject.transform.position = lightPoint;
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = m_LightRange;
                light.shadows = LightShadows.None;
                m_Lights[i] = light;

                // Dans l'éditeur, on les voit allumés ; en jeu, ils attendent la musique.
                m_Levels[i] = Application.isPlaying ? 0f : 1f;
                Apply(i);
            }
        }

        // Quart du secteur : 0 face au joueur (côté dragon, autour de +Z), puis dans le sens des aiguilles d'une montre.
        static int GroupOf(float angle)
        {
            var degrees = Mathf.Repeat(angle * Mathf.Rad2Deg + 45f, 360f);
            return Mathf.Clamp((int)(degrees / 90f), 0, GroupCount - 1);
        }
    }
}
