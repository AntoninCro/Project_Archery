using System;
using System.Collections.Generic;
using Archery.Difficulty;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Building
{
    /// <summary>
    /// Un bâtiment du tower defense (GDD, section 9), construit sur un <see cref="BuildSocket"/>. À placer à la racine
    /// du prefab du bâtiment. Pendant la construction (et pendant une amélioration), il s'affiche en plan bleu
    /// transparent, qui se remplit de bas en haut à chaque coup de marteau.
    /// </summary>
    /// <remarks>
    /// Les bâtiments eux-mêmes (baliste, tour de givre…) en héritent et réagissent à <see cref="OnLevelChanged"/>.
    /// Pendant la première construction, ses colliders, ses lumières et ses particules sont coupés ; pendant une
    /// amélioration, il continue de fonctionner à son niveau actuel.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Structure : MonoBehaviour
    {
        static readonly int k_FillHeightId = Shader.PropertyToID("_FillHeight");
        static readonly int k_FlashId = Shader.PropertyToID("_Flash");

        [Tooltip("Optionnel : objets ajoutés au niveau 2 (élément 0) puis au niveau 3 (élément 1). Cachés avant.")]
        [SerializeField]
        GameObject[] m_LevelParts = Array.Empty<GameObject>();

        [Tooltip("Les dégâts du bâtiment suivent la force des ennemis (difficulté et vague) : il garde le même effet toute la partie. " +
                 "Ils suivent aussi la puissance des bâtiments de la difficulté (Structure Power). Ses dégâts de base sont ceux de la vague 1 en Normal.")]
        [SerializeField]
        bool m_FollowEnemyStrength = true;

        struct RendererState
        {
            public Renderer Renderer;
            public Material[] Materials;
            public ShadowCastingMode Shadows;
        }

        static readonly List<Enemy> s_Enemies = new List<Enemy>();

        readonly List<RendererState> m_Swapped = new List<RendererState>();
        readonly List<Renderer> m_HiddenRenderers = new List<Renderer>();
        readonly List<Light> m_HiddenLights = new List<Light>();
        readonly List<Collider> m_DisabledColliders = new List<Collider>();
        Material m_Blueprint;
        Bounds m_Bounds;
        int m_TargetLevel;
        float m_Fill;
        float m_FillTarget;
        float m_Flash;

        /// <summary>Un bâtiment vient d'être construit ou amélioré.</summary>
        public static event Action<Structure> Built;

        public StructureDefinition Definition { get; private set; }
        public BuildSocket Socket { get; private set; }

        /// <summary>Niveau actuel : 0 tant que la première construction n'est pas finie, puis 1 à 3.</summary>
        public int Level { get; private set; }

        public bool IsBuilt => Level > 0;
        public bool IsUnderConstruction => m_TargetLevel > 0;

        /// <summary>Niveau en construction (0 si aucune construction en cours).</summary>
        public int TargetLevel => m_TargetLevel;

        public int MaxLevel => Definition != null ? Definition.MaxLevel : 1;
        public string DisplayName => Definition != null ? Definition.displayName : name;

        /// <summary>
        /// Force des ennemis qui apparaissent en ce moment : PV de la difficulté × renforcement de la vague
        /// (1 à la vague 1 en Normal).
        /// </summary>
        public static float EnemyStrength => DifficultyManager.Current.enemyHealth * EnemyScaling.Health;

        /// <summary>
        /// Multiplie les dégâts du bâtiment, s'il suit la force des ennemis : cette force × la puissance des bâtiments
        /// de la difficulté (plus d'ennemis en Impossible, donc des bâtiments plus forts pour garder la même utilité).
        /// </summary>
        protected float Strength => m_FollowEnemyStrength ? EnemyStrength * DifficultyManager.Current.structurePower : 1f;

        /// <summary>Volume du plan pendant la construction : c'est là qu'on frappe avec le marteau.</summary>
        public Bounds ConstructionBounds => m_Bounds;

        protected virtual void Awake() => ShowLevelParts(0);

        // Posé à la main dans la scène, sans socle : il est construit au niveau 1 dès le départ.
        protected virtual void Start()
        {
            if (Socket != null || IsBuilt || IsUnderConstruction)
                return;

            Level = 1;
            ShowLevelParts(Level);
            OnLevelChanged(Level);
        }

        protected virtual void Update()
        {
            if (!IsUnderConstruction || m_Blueprint == null)
                return;

            var deltaTime = Time.unscaledDeltaTime;
            m_Fill = Mathf.MoveTowards(m_Fill, m_FillTarget, deltaTime * 2.5f);
            m_Flash = Mathf.MoveTowards(m_Flash, 0f, deltaTime * 3f);
            ApplyBlueprint();
        }

        protected virtual void OnDestroy()
        {
            if (m_Blueprint != null)
                Destroy(m_Blueprint);
        }

        /// <summary>Appelé quand le bâtiment atteint un nouveau niveau (1 à la construction, puis 2 et 3).</summary>
        protected virtual void OnLevelChanged(int level)
        {
        }

        /// <summary>
        /// Faux pour un rendu qui ne fait pas partie du bâtiment lui-même (disque de portée…) : il ne passe pas en plan bleu
        /// et ne compte pas dans le volume qu'on frappe au marteau.
        /// </summary>
        protected virtual bool IsBlueprintPart(Renderer renderer) => true;

        /// <summary>Valeur du niveau donné (1 à 3) dans un tableau « par niveau » ; le dernier élément vaut pour les niveaux au-delà.</summary>
        protected static float ValueFor(float[] values, int level, float fallback) =>
            values == null || values.Length == 0 ? fallback : values[Mathf.Clamp(level - 1, 0, values.Length - 1)];

        protected static int ValueFor(int[] values, int level, int fallback) =>
            values == null || values.Length == 0 ? fallback : values[Mathf.Clamp(level - 1, 0, values.Length - 1)];

        /// <summary>Ennemis vivants, copiés dans une liste réutilisée : on peut les blesser (et les tuer) en la parcourant.</summary>
        protected static List<Enemy> AliveEnemies()
        {
            s_Enemies.Clear();
            s_Enemies.AddRange(Enemy.Alive);
            return s_Enemies;
        }

        internal void Setup(StructureDefinition definition, BuildSocket socket)
        {
            Definition = definition;
            Socket = socket;
        }

        /// <summary>Le bâtiment passe en plan bleu, vide, pour atteindre le niveau donné.</summary>
        internal void BeginConstruction(int targetLevel, Material blueprint)
        {
            if (IsUnderConstruction)
                FinishConstruction();

            m_TargetLevel = Mathf.Max(1, targetLevel);
            ShowLevelParts(m_TargetLevel);
            m_Bounds = ComputeBounds();
            m_Blueprint = blueprint != null ? new Material(blueprint) { name = "Blueprint (" + DisplayName + ")" } : null;

            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !IsBlueprintPart(renderer))
                    continue;

                if (m_Blueprint != null && (renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                {
                    var originals = renderer.sharedMaterials;
                    m_Swapped.Add(new RendererState { Renderer = renderer, Materials = originals, Shadows = renderer.shadowCastingMode });
                    var materials = new Material[originals.Length];
                    for (var i = 0; i < materials.Length; i++)
                        materials[i] = m_Blueprint;
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                else if (!IsBuilt && !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                {
                    // Particules, lignes, traînées : rien ne s'anime sur un plan.
                    renderer.enabled = false;
                    m_HiddenRenderers.Add(renderer);
                }
            }

            if (!IsBuilt)
            {
                foreach (var light in GetComponentsInChildren<Light>())
                {
                    if (!light.enabled)
                        continue;
                    light.enabled = false;
                    m_HiddenLights.Add(light);
                }

                // Le plan ne bloque rien : ni le joueur, ni les ennemis, ni les flèches.
                foreach (var collider in GetComponentsInChildren<Collider>())
                {
                    if (!collider.enabled)
                        continue;
                    collider.enabled = false;
                    m_DisabledColliders.Add(collider);
                }
            }

            m_Fill = 0f;
            m_FillTarget = 0f;
            m_Flash = 0f;
            ApplyBlueprint();
        }

        /// <summary>Part du plan remplie (0 à 1), atteinte en un court instant.</summary>
        internal void SetProgress(float progress)
        {
            m_FillTarget = Mathf.Clamp01(progress);
            m_Flash = 1f;
        }

        /// <summary>Fin de la construction : le bâtiment reprend ses matériaux et passe au niveau visé.</summary>
        internal void FinishConstruction()
        {
            if (!IsUnderConstruction)
                return;

            foreach (var state in m_Swapped)
            {
                if (state.Renderer == null)
                    continue;
                state.Renderer.sharedMaterials = state.Materials;
                state.Renderer.shadowCastingMode = state.Shadows;
            }

            foreach (var renderer in m_HiddenRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            foreach (var light in m_HiddenLights)
            {
                if (light != null)
                    light.enabled = true;
            }

            foreach (var collider in m_DisabledColliders)
            {
                if (collider != null)
                    collider.enabled = true;
            }

            m_Swapped.Clear();
            m_HiddenRenderers.Clear();
            m_HiddenLights.Clear();
            m_DisabledColliders.Clear();
            if (m_Blueprint != null)
                Destroy(m_Blueprint);
            m_Blueprint = null;

            Level = m_TargetLevel;
            m_TargetLevel = 0;
            ShowLevelParts(Level);
            OnLevelChanged(Level);
            Built?.Invoke(this);
        }

        void ApplyBlueprint()
        {
            if (m_Blueprint == null)
                return;

            var bottom = m_Bounds.min.y - 0.02f;
            var top = m_Bounds.max.y + 0.02f;
            m_Blueprint.SetFloat(k_FillHeightId, Mathf.Lerp(bottom, top, m_Fill));
            m_Blueprint.SetFloat(k_FlashId, m_Flash);
        }

        void ShowLevelParts(int level)
        {
            for (var i = 0; i < m_LevelParts.Length; i++)
            {
                if (m_LevelParts[i] != null)
                    m_LevelParts[i].SetActive(level >= i + 2);
            }
        }

        // Le volume visible du bâtiment, assez grand pour qu'on le frappe sans viser : au moins 1 m de haut
        // (pièges à piques au ras du sol) et 0,8 m de large.
        Bounds ComputeBounds()
        {
            var bounds = new Bounds(transform.position + Vector3.up * 0.5f, Vector3.zero);
            var first = true;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || !IsBlueprintPart(renderer))
                    continue;

                if (first)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                first = false;
            }

            var min = bounds.min;
            var max = bounds.max;
            max.y = Mathf.Max(max.y, min.y + 1f);
            for (var axis = 0; axis < 3; axis += 2)
            {
                var missing = 0.8f - (max[axis] - min[axis]);
                if (missing <= 0f)
                    continue;
                min[axis] -= missing * 0.5f;
                max[axis] += missing * 0.5f;
            }

            bounds.SetMinMax(min, max);
            return bounds;
        }

        void OnDrawGizmosSelected()
        {
            if (!IsUnderConstruction)
                return;

            Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.6f);
            Gizmos.DrawWireCube(m_Bounds.center, m_Bounds.size);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Built = null;
    }
}
