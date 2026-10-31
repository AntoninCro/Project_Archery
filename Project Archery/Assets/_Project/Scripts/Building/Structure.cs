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
        protected float Strength => (m_FollowEnemyStrength ? EnemyStrength * DifficultyManager.Current.structurePower : 1f) * SupportDamage;


        // Le soutien du phare (deuxième brainstorm du tower defense, 29 octobre) : dans son rayon, les dégâts (+20 % au niveau 1)
        // et la cadence (niveau 2) des bâtiments ; il le renouvelle tant qu'ils y sont. Et l'élan du phare du jugement (niveau 5) :
        // une cadence doublée pendant 5 s, à part, pour ne pas durer tant que le soutien se renouvelle.
        float m_SupportDamage = 1f;
        float m_SupportRate = 1f;
        float m_SupportUntil;
        float m_BoostRate = 1f;
        float m_BoostUntil;

        float SupportDamage => Time.time <= m_SupportUntil ? m_SupportDamage : 1f;

        float SupportRate => (Time.time <= m_SupportUntil ? m_SupportRate : 1f) * (Time.time <= m_BoostUntil ? m_BoostRate : 1f);

        /// <summary>
        /// Soutenu par un phare pendant <paramref name="duration"/> s : ses dégâts multipliés par <paramref name="damage"/>, sa
        /// cadence par <paramref name="rate"/> (le plus fort des phares s'il y en a plusieurs).
        /// </summary>
        public void Support(float damage, float rate, float duration)
        {
            if (Time.time > m_SupportUntil)
            {
                m_SupportDamage = 1f;
                m_SupportRate = 1f;
            }

            m_SupportDamage = Mathf.Max(m_SupportDamage, damage);
            m_SupportRate = Mathf.Max(m_SupportRate, rate);
            m_SupportUntil = Mathf.Max(m_SupportUntil, Time.time + duration);
        }

        /// <summary>Sa cadence multipliée par <paramref name="rate"/> pendant <paramref name="duration"/> s (le phare du jugement).</summary>
        public void Boost(float rate, float duration)
        {
            m_BoostRate = Time.time > m_BoostUntil ? rate : Mathf.Max(m_BoostRate, rate);
            m_BoostUntil = Mathf.Max(m_BoostUntil, Time.time + duration);
        }

        /// <summary>Volume du plan pendant la construction : c'est là qu'on frappe avec le marteau.</summary>
        public Bounds ConstructionBounds => m_Bounds;

        float m_RangeScale = 1f;
        float m_Elevation;

        /// <summary>
        /// Multiplie la portée (baliste, tour de givre) : 1 sur la carte ; plus dans l'arène du boss final, où les tourelles
        /// téléportées tirent du haut du plateau.
        /// </summary>
        public float RangeScale
        {
            get => m_RangeScale;
            set
            {
                m_RangeScale = Mathf.Max(0.1f, value);
                OnRangeChanged();
            }
        }

        /// <summary>
        /// Hauteur (m) du bâtiment au-dessus du sol où se battent les ennemis : 0 sur la carte ; la hauteur du plateau dans
        /// l'arène du boss final (il touche aussi les ennemis en contrebas).
        /// </summary>
        public float Elevation
        {
            get => m_Elevation;
            set
            {
                m_Elevation = Mathf.Max(0f, value);
                OnRangeChanged();
            }
        }

        /// <summary>Appelé quand <see cref="RangeScale"/> ou <see cref="Elevation"/> change.</summary>
        protected virtual void OnRangeChanged()
        {
        }

        static readonly List<Structure> s_All = new List<Structure>();

        /// <summary>Les bâtiments présents (le phare soutient ceux de son rayon, 29 octobre).</summary>
        public static IReadOnlyList<Structure> All => s_All;

        protected virtual void Awake()
        {
            s_All.Add(this);
            ShowLevelParts(0);
        }

        // Posé à la main dans la scène, sans socle : il est construit au niveau 1 dès le départ.
        protected virtual void Start()
        {
            if (Socket != null || IsBuilt || IsUnderConstruction)
                return;

            Level = 1;
            ShowLevelParts(Level);
            OnLevelChanged(Level);
        }

        float m_OverchargeTimer;

        /// <summary>
        /// Surchargé (26 octobre : une flèche du joueur avec le talisman de l'ingénieur) : la baliste tire deux fois plus vite,
        /// la baliste et la tour de givre portent deux fois plus loin (depuis le 27 octobre, réglage du joueur ; 50 % avant).
        /// </summary>
        public bool IsOvercharged => m_OverchargeTimer > 0f;

        /// <summary>Multiplie la cadence de tir pendant la surcharge (×2, ×4, ×6 selon le niveau du talisman : 26 octobre).</summary>
        public float OverchargeRate => (IsOvercharged ? m_OverchargeRate : 1f) * SupportRate;

        /// <summary>Multiplie la portée pendant la surcharge.</summary>
        public float OverchargeRange => IsOvercharged ? 2f : 1f;

        float m_OverchargeRate = 2f;

        // La surcharge se voit (27 octobre, demandé par le joueur) : le bâtiment rougit un peu, en battant. Une teinte par matériau
        // (MaterialPropertyBlock), retirée à la fin ; pas pendant une construction (le plan bleu remplace les matériaux).
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly Color k_OverchargeTint = new Color(1f, 0.15f, 0.1f);
        const float k_OverchargeTintShare = 0.35f;

        struct TintedRenderer
        {
            public Renderer Renderer;
            public Material[] Materials;
        }

        readonly List<TintedRenderer> m_Tinted = new List<TintedRenderer>();
        MaterialPropertyBlock m_TintBlock;
        bool m_TintOn;

        // Le volume du bâtiment pour les flèches (ArrowBounds) et quand le recalculer.
        Bounds m_ArrowBounds;
        float m_NextArrowBounds;

        /// <summary>
        /// Le volume visible du bâtiment, recalculé toutes les 2 s au plus (27 octobre : la baliste et la tour de givre n'ont pas de
        /// collider ; le talisman de l'ingénieur regarde si le trajet d'une flèche traverse ce volume).
        /// </summary>
        public Bounds ArrowBounds
        {
            get
            {
                if (Time.time >= m_NextArrowBounds)
                {
                    m_NextArrowBounds = Time.time + 2f;
                    m_ArrowBounds = ComputeBounds();
                }

                return m_ArrowBounds;
            }
        }

        /// <summary>
        /// Surcharge le bâtiment construit pendant <paramref name="duration"/> s (prolonge une surcharge en cours), sa cadence
        /// multipliée par <paramref name="rate"/> (la plus forte des deux si une surcharge est en cours).
        /// </summary>
        public void Overcharge(float duration, float rate = 2f)
        {
            if (!IsBuilt || duration <= 0f)
                return;

            var was = IsOvercharged;
            m_OverchargeRate = was ? Mathf.Max(m_OverchargeRate, rate) : Mathf.Max(1f, rate);
            m_OverchargeTimer = Mathf.Max(m_OverchargeTimer, duration);
            if (!was)
                OnRangeChanged();
        }

        protected virtual void Update()
        {
            if (m_OverchargeTimer > 0f)
            {
                m_OverchargeTimer -= Time.deltaTime;
                if (m_OverchargeTimer <= 0f)
                    OnRangeChanged();
            }

            UpdateOverchargeTint();

            if (!IsUnderConstruction || m_Blueprint == null)
                return;

            var deltaTime = Time.unscaledDeltaTime;
            m_Fill = Mathf.MoveTowards(m_Fill, m_FillTarget, deltaTime * 2.5f);
            m_Flash = Mathf.MoveTowards(m_Flash, 0f, deltaTime * 3f);
            ApplyBlueprint();
        }

        void UpdateOverchargeTint()
        {
            var on = IsOvercharged && !IsUnderConstruction;
            if (!on && !m_TintOn)
                return;

            if (m_TintBlock == null)
                m_TintBlock = new MaterialPropertyBlock();
            if (on && !m_TintOn)
            {
                m_Tinted.Clear();
                foreach (var renderer in GetComponentsInChildren<Renderer>())
                {
                    if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && IsBlueprintPart(renderer))
                        m_Tinted.Add(new TintedRenderer { Renderer = renderer, Materials = renderer.sharedMaterials });
                }
            }

            m_TintOn = on;
            var share = on ? k_OverchargeTintShare * (0.75f + 0.25f * Mathf.Sin(Time.time * 6f)) : 0f;
            foreach (var tinted in m_Tinted)
            {
                if (tinted.Renderer == null)
                    continue;

                for (var i = 0; i < tinted.Materials.Length; i++)
                {
                    var material = tinted.Materials[i];
                    m_TintBlock.Clear();
                    if (on && material != null)
                    {
                        if (material.HasProperty(k_BaseColorId))
                            m_TintBlock.SetColor(k_BaseColorId, Color.Lerp(material.GetColor(k_BaseColorId), k_OverchargeTint, share));
                        else if (material.HasProperty(k_ColorId))
                            m_TintBlock.SetColor(k_ColorId, Color.Lerp(material.GetColor(k_ColorId), k_OverchargeTint, share));
                    }

                    tinted.Renderer.SetPropertyBlock(m_TintBlock, i);
                }
            }

            if (!on)
                m_Tinted.Clear();
        }

        protected virtual void OnDestroy()
        {
            s_All.Remove(this);
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

        /// <summary>Valeur du niveau donné (1 à 5) dans un tableau « par niveau » ; le dernier élément vaut pour les niveaux au-delà.</summary>
        protected static float ValueFor(float[] values, int level, float fallback) =>
            values == null || values.Length == 0 ? fallback : values[Mathf.Clamp(level - 1, 0, values.Length - 1)];

        protected static int ValueFor(int[] values, int level, int fallback) =>
            values == null || values.Length == 0 ? fallback : values[Mathf.Clamp(level - 1, 0, values.Length - 1)];

        /// <summary>
        /// Ennemis vivants, copiés dans une liste réutilisée : on peut les blesser (et les tuer) en la parcourant. Sans les
        /// mannequins du centre d'entraînement (26 octobre) : les tourelles ne tirent pas dessus.
        /// </summary>
        /// <summary>
        /// Direction (à plat) vers la forêt, d'où vient cet ennemi (refonte du tower defense, 28 octobre : soufflerie,
        /// catapulte) : à l'inverse de sa marche ; arrêté, à l'opposé de la tour du joueur.
        /// </summary>
        protected static Vector3 TowardForest(Enemy enemy)
        {
            var velocity = enemy.Velocity;
            velocity.y = 0f;
            if (velocity.sqrMagnitude > 0.25f)
                return -velocity.normalized;

            var tower = Archery.Defense.Tower.Instance;
            var away = tower != null ? enemy.transform.position - tower.transform.position : -enemy.transform.forward;
            away.y = 0f;
            return away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
        }

        protected static List<Enemy> AliveEnemies()
        {
            s_Enemies.Clear();
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy != null && !enemy.IsTrainingDummy)
                    s_Enemies.Add(enemy);
            }

            return s_Enemies;
        }

        /// <summary>Les pièces des niveaux 2 et 3 (l'hologramme de pose les montre selon le niveau : BuildPlacement, 27 octobre).</summary>
        internal GameObject[] LevelParts => m_LevelParts;

        /// <summary>Ce rendu fait partie du bâtiment montré en plan bleu (l'hologramme de pose reprend les mêmes).</summary>
        internal bool IsPreviewPart(Renderer renderer) => IsBlueprintPart(renderer);

        /// <summary>
        /// Visible au niveau donné : les pièces des niveaux 2 et 3 seulement à partir de leur niveau, le reste s'il est actif (lu
        /// aussi sur le prefab, où les pièces des niveaux sont actives : l'hologramme de pose, l'aperçu du menu des plans).
        /// </summary>
        internal bool IsShownAtLevel(Transform part, int level)
        {
            for (var current = part; current != null; current = current.parent)
            {
                var index = m_LevelParts != null ? Array.IndexOf(m_LevelParts, current.gameObject) : -1;
                if (index >= 0)
                {
                    if (level < index + 2)
                        return false;
                }
                else if (!current.gameObject.activeSelf)
                {
                    return false;
                }

                if (current == transform)
                    break;
            }

            return true;
        }

        /// <summary>
        /// Largeur et profondeur (m) du modèle à son plus haut niveau (3, ou 5 depuis le 29 octobre), autour de son centre, dans
        /// son propre repère (28 octobre : les cases qu'il occupe sur la grille de construction, <see cref="BuildGrid.FootprintOf"/>).
        /// Se lit sur le prefab.
        /// </summary>
        public Vector2 ModelSize()
        {
            var toLocal = transform.worldToLocalMatrix;
            var half = Vector2.zero;
            // Depuis le 29 octobre au soir (le joueur) : l'emprise de pose est celle du niveau 1, les niveaux suivants peuvent déborder.
            var top = 1;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || !IsBlueprintPart(renderer) ||
                    !IsShownAtLevel(renderer.transform, top))
                    continue;

                var mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                    : renderer.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
                if (mesh == null)
                    continue;

                var matrix = toLocal * renderer.transform.localToWorldMatrix;
                var bounds = mesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3(i % 2 == 0 ? -1f : 1f, (i / 2) % 2 == 0 ? -1f : 1f,
                                                                                          i < 4 ? -1f : 1f));
                    var local = matrix.MultiplyPoint3x4(corner);
                    half.x = Mathf.Max(half.x, Mathf.Abs(local.x));
                    half.y = Mathf.Max(half.y, Mathf.Abs(local.z));
                }
            }

            var scale = transform.localScale;
            return new Vector2(Mathf.Max(0.3f, half.x * 2f * Mathf.Abs(scale.x)), Mathf.Max(0.3f, half.y * 2f * Mathf.Abs(scale.z)));
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

        /// <summary>
        /// Le bâtiment passe tout de suite à ce niveau, sans plan bleu ni marteau : une tour qui naît dans un duo garde le niveau
        /// qu'elle avait (31 octobre).
        /// </summary>
        internal void SetLevelImmediate(int level)
        {
            if (IsUnderConstruction)
                FinishConstruction();

            Level = Mathf.Clamp(level, 1, Mathf.Max(1, MaxLevel));
            ShowLevelParts(Level);
            OnLevelChanged(Level);
            Built?.Invoke(this);
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
