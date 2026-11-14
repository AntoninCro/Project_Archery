using System.Collections.Generic;
using System.Text;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Player;
using Archery.Shop;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Building
{
    /// <summary>
    /// Constructions du tower defense (GDD, section 9). Sur un <see cref="BuildSocket"/>, le bâtiment apparaît en plan bleu,
    /// l'arc se range et un marteau apparaît dans chaque main, sans rien attraper. Trois coups sur le plan, avec l'un ou
    /// l'autre, le construisent ; les marteaux disparaissent et l'arc revient. Une amélioration se fait de la même façon.
    /// </summary>
    /// <remarks>
    /// À placer sur l'objet <c>Game</c>. Une seule construction à la fois. Elle se termine d'elle-même si le joueur
    /// s'éloigne (téléporteur de la tour) ou si la partie se termine : la construction n'est jamais un obstacle.
    /// Depuis la refonte du 27 octobre (<c>Docs/Refonte_TowerDefense.md</c>, étape 2), plus de matériaux : le joueur garde ici
    /// ses <b>plans</b>, gagnés par lots dans les caisses de l'ingénieur. Un plan construit le bâtiment, un autre plan du même
    /// bâtiment le fait monter d'un niveau. Depuis l'étape 3, on construit sur les emplacements de la <see cref="BuildGrid"/>, en
    /// posant l'hologramme d'un plan sorti de la montre (<see cref="BuildPlacement"/>), et un bâtiment se déplace gratuitement
    /// entre les vagues (<see cref="MoveTo"/>). Les deux sont créés ici si la scène ne les a pas.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BuildManager : MonoBehaviour
    {
        const string k_BlueprintResource = "Blueprint";

        [Tooltip("Les bâtiments qu'on peut construire. Chaque socle propose ceux de son type (sur la voie, au bord).")]
        [SerializeField]
        List<StructureDefinition> m_Structures = new List<StructureDefinition>();

        [SerializeField]
        BuildHammer m_HammerPrefab;

        [Tooltip("Matériau du plan bleu (shader Archery/Blueprint). Vide : Resources/Blueprint.")]
        [SerializeField]
        Material m_BlueprintMaterial;

        [Tooltip("Coups de marteau pour construire ou améliorer un bâtiment.")]
        [Min(1)]
        [SerializeField]
        int m_StrikesToBuild = 3;

        [Tooltip("Si le joueur s'éloigne plus que ça (m) du plan, la construction se termine d'elle-même.")]
        [SerializeField]
        float m_AutoFinishDistance = 15f;

        [Header("Retours")]
        [Tooltip("Optionnel : effet créé à chaque coup (par exemple Prefabs/Effects/Hit Surface), détruit au bout de 2 s.")]
        [SerializeField]
        GameObject m_StrikeEffect;

        [Tooltip("Son quand les marteaux apparaissent dans les mains.")]
        [SerializeField]
        AudioClip m_HammerClip;

        [Tooltip("Sons des coups (un au hasard à chaque coup).")]
        [SerializeField]
        AudioClip[] m_StrikeClips = System.Array.Empty<AudioClip>();

        [Tooltip("Son du bâtiment terminé.")]
        [SerializeField]
        AudioClip m_CompleteClip;

        [SerializeField]
        Color m_MessageColor = new Color(0.55f, 0.8f, 1f);

        [Header("Duos (31 octobre)")]
        [Tooltip("Niveau que doivent avoir les deux tours pour fusionner en duo.")]
        [Min(1)]
        [SerializeField]
        int m_DuoFromLevel = 3;

        [Tooltip("Son de la fusion.")]
        [SerializeField]
        AudioClip m_DuoClip;

        [Header("Ultimes (2 novembre)")]
        [Tooltip("Son de la transformation d'un trio en ultime, et de l'ultime terminée. Vide : celui de la fusion.")]
        [SerializeField]
        AudioClip m_UltimateClip;

        // Au début de la partie, les coffres ne proposent que les plans du chantier (le joueur, après son premier essai : les plans
        // s'éparpillaient sur les 10 tours, et les duos n'arrivaient pas). Depuis le 3 novembre (le joueur), il commence par les 3 tours
        // d'un trio tiré au hasard ; depuis le 12 novembre, sans rien de plus (la Barricade, ajoutée le 3 novembre, est retirée : « pour
        // comprendre facilement l'ultime » ; Extra Starting Structures, retiré).
        const int k_StartingStructures = 3;

        [Header("Chantier de la partie (2 novembre)")]
        [Tooltip("Bâtiments qui rejoignent le chantier après chaque boss vaincu (à la fin de chaque vague).")]
        [Min(0)]
        [SerializeField]
        int m_StructuresPerBoss = 1;

        [Tooltip("Plans donnés par l'orbe de tourelle d'un coffre : de 1 à 4, tirés au sort (puis multipliés par le lot du bâtiment, " +
                 "Lot Size, 1 partout). Une ligne par vague (la dernière vaut pour les suivantes) : les chances de 1, 2, 3 et 4 plans, " +
                 "en poids (leur total n'a pas besoin de faire 100). Le nombre de plans fait la rareté de la carte (1 commune, 2 rare, " +
                 "3 épique, 4 légendaire). Depuis le 12 novembre au soir : 50 / 30 / 14 / 6 à la vague 1 (1,8 plan en moyenne), jusqu'à " +
                 "30 / 35 / 22 / 13 à la vague 5 (2,2) ; 2 plans en moyenne sur les 5 vagues à coffres. Le même jour, d'abord : les " +
                 "chances des objets (de 70 / 20 / 7 / 3 à 50 / 28 / 15 / 7, 1,6 plan en moyenne). Du 4 au 12 novembre : de " +
                 "40 / 45 / 10 / 5 à 15 / 30 / 35 / 20, 2,2 plans en moyenne. Avant : toujours 2 (2 novembre au soir) ; 1 avant.")]
        // Le 4 novembre, le joueur : « entre 1 et 4 plans par coffre, au hasard ; les chances d'en avoir plus augmentent pendant la
        // partie ». Le 12 novembre, après le neuvième essai (trop de tourelles épiques et légendaires) : « prends 70 / 20 / 7 / 3 pour
        // tout et augmente progressivement [...] ; si on applique cela partout, ça ne changera que les tours, qui seront légèrement
        // nerf » (Editor/NinthTestBaker). Le soir même, après le dixième essai (« la partie m'a paru très fade, pas de construction
        // gigantesque ; peut-être augmenter légèrement les drops, en particulier en début de partie ») : le début remonte, pas la fin
        // (Editor/TenthTestBaker).
        [SerializeField]
        PlanChances[] m_PlanChances =
        {
            new PlanChances(50f, 30f, 14f, 6f),
            new PlanChances(45f, 31f, 16f, 8f),
            new PlanChances(40f, 33f, 18f, 9f),
            new PlanChances(35f, 34f, 20f, 11f),
            new PlanChances(30f, 35f, 22f, 13f),
        };

        [Header("Bilan")]
        [Tooltip("À la fin de chaque vague, un message donne la part des dégâts faite par les bâtiments et leurs éliminations " +
                 "(la console détaille chaque type de bâtiment). Utile pour régler l'équilibre en jouant.")]
        [SerializeField]
        bool m_ShowWaveReport = true;

        readonly Dictionary<string, Vector2> m_ReportByName = new Dictionary<string, Vector2>();
        readonly StringBuilder m_Builder = new StringBuilder();
        float m_WaveDamage;
        float m_StructureDamage;
        int m_StructureKills;

        readonly Dictionary<StructureDefinition, int> m_Plans = new Dictionary<StructureDefinition, int>();
        readonly List<StructureDefinition> m_RunStructures = new List<StructureDefinition>();
        static readonly List<StructureDefinition> s_Pool = new List<StructureDefinition>();
        static readonly float[] k_RarityWeights = { 0.6f, 0.3f, 0.1f };
        TowerDuoCatalog.Entry m_StartingTrio;
        Structure m_Current;
        bool m_UltimateBuilt;
        readonly List<BuildHammer> m_Hammers = new List<BuildHammer>();
        Bow m_Bow;
        WaveManager m_Waves;
        int m_Strikes;
        float m_FarTime;

        public static BuildManager Instance { get; private set; }

        public IReadOnlyList<StructureDefinition> Structures => m_Structures;

        /// <summary>
        /// Le chantier de la partie (2 novembre) : les bâtiments dont les coffres proposent les plans. 3 au début (3 novembre : les
        /// 3 tours d'un trio tiré au hasard ; la Barricade en plus jusqu'au 12 novembre), un de plus après chaque boss. On n'apprend que quelques
        /// tours au début, les plans se concentrent (les duos et les trios arrivent), et chaque partie a sa couleur.
        /// </summary>
        public IReadOnlyList<StructureDefinition> RunStructures => m_RunStructures;

        /// <summary>
        /// Plans d'un bâtiment donnés par l'orbe de tourelle d'un coffre, avant son lot : de 1 à 4, tirés au sort selon la vague
        /// (4 novembre ; toujours 2 du 2 au 4 novembre, 1 avant).
        /// </summary>
        public int RollPlansPerChest()
        {
            if (m_PlanChances == null || m_PlanChances.Length == 0)
                return 2;

            var waves = m_Waves != null ? m_Waves : WaveManager.Instance;
            var wave = waves != null ? Mathf.Max(1, waves.WaveNumber) : 1;
            return m_PlanChances[Mathf.Min(wave, m_PlanChances.Length) - 1].Roll();
        }

        /// <summary>Les chances d'une orbe de tourelle de donner 1, 2, 3 ou 4 plans, à une vague (des poids, 4 novembre).</summary>
        [System.Serializable]
        public struct PlanChances
        {
            [Tooltip("Chance d'un plan.")]
            [Min(0f)]
            public float one;

            [Tooltip("Chance de deux plans.")]
            [Min(0f)]
            public float two;

            [Tooltip("Chance de trois plans.")]
            [Min(0f)]
            public float three;

            [Tooltip("Chance de quatre plans.")]
            [Min(0f)]
            public float four;

            public PlanChances(float one, float two, float three, float four)
            {
                this.one = one;
                this.two = two;
                this.three = three;
                this.four = four;
            }

            /// <summary>De 1 à 4 plans, selon les poids (2 s'ils sont tous nuls).</summary>
            public int Roll()
            {
                var total = Mathf.Max(0f, one) + Mathf.Max(0f, two) + Mathf.Max(0f, three) + Mathf.Max(0f, four);
                if (total <= 0f)
                    return 2;

                var pick = Random.value * total;
                if ((pick -= Mathf.Max(0f, one)) < 0f)
                    return 1;
                if ((pick -= Mathf.Max(0f, two)) < 0f)
                    return 2;
                if ((pick -= Mathf.Max(0f, three)) < 0f)
                    return 3;
                // Random.value peut valoir 1 : le dernier nombre qui a une chance.
                return four > 0f ? 4 : three > 0f ? 3 : two > 0f ? 2 : 1;
            }
        }

        /// <summary>Une construction est en cours (les marteaux sont sortis).</summary>
        public bool IsBuilding => m_Current != null;

        /// <summary>Un marteau de la construction en cours (le premier), ou null.</summary>
        public BuildHammer Hammer => m_Hammers.Count > 0 ? m_Hammers[0] : null;

        /// <summary>Nombre de coups déjà donnés sur le plan en cours.</summary>
        public int Strikes => m_Strikes;

        public int StrikesToBuild => m_StrikesToBuild;

        /// <summary>Le matériau du plan bleu (repris par l'hologramme de pose).</summary>
        public Material BlueprintMaterial => m_BlueprintMaterial;

        /// <summary>Les plans du joueur ont changé (caisse de l'ingénieur, construction).</summary>
        public event System.Action PlansChanged;

        /// <summary>Plans de ce bâtiment que le joueur a en poche.</summary>
        public int PlansOf(StructureDefinition definition) =>
            definition != null && m_Plans.TryGetValue(definition, out var count) ? count : 0;

        /// <summary>Tous les plans du joueur, tous bâtiments confondus (la montre).</summary>
        public int TotalPlans
        {
            get
            {
                var total = 0;
                foreach (var count in m_Plans.Values)
                    total += count;
                return total;
            }
        }

        /// <summary>Ajoute des plans d'un bâtiment (un lot de la caisse de l'ingénieur).</summary>
        public void AddPlans(StructureDefinition definition, int count)
        {
            if (definition == null || count <= 0)
                return;

            m_Plans[definition] = PlansOf(definition) + count;
            PlansChanged?.Invoke();
        }

        bool TrySpendPlans(StructureDefinition definition, int spent)
        {
            var count = PlansOf(definition);
            if (count < spent)
                return false;

            m_Plans[definition] = count - spent;
            PlansChanged?.Invoke();
            return true;
        }

        /// <summary>Un bâtiment que les coffres peuvent proposer : ni retiré, ni un duo.</summary>
        public static bool IsOffered(StructureDefinition definition) =>
            definition != null && definition.prefab != null && !definition.retired && definition.kind != StructureKind.Duo;

        // Un bâtiment de plus au chantier, parmi ceux qui n'y sont pas encore : la rareté d'abord (60 % commune, 30 % rare, 10 %
        // légendaire, parmi celles qui restent : la règle des coffres du 28 octobre), puis au hasard dans elle. Null : tous y sont.
        StructureDefinition AddRunStructure()
        {
            s_Pool.Clear();
            foreach (var definition in m_Structures)
            {
                if (IsOffered(definition) && !m_RunStructures.Contains(definition) && !s_Pool.Contains(definition))
                    s_Pool.Add(definition);
            }

            if (s_Pool.Count == 0)
                return null;

            var total = 0f;
            for (var rarity = 0; rarity < k_RarityWeights.Length; rarity++)
            {
                if (s_Pool.Exists(d => (int)d.rarity == rarity))
                    total += k_RarityWeights[rarity];
            }

            var roll = Random.value * total;
            var chosen = -1;
            for (var rarity = 0; rarity < k_RarityWeights.Length && chosen < 0; rarity++)
            {
                if (!s_Pool.Exists(d => (int)d.rarity == rarity))
                    continue;
                roll -= k_RarityWeights[rarity];
                if (roll <= 0f)
                    chosen = rarity;
            }

            var candidates = s_Pool.FindAll(d => chosen < 0 || (int)d.rarity == chosen);
            var picked = candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : s_Pool[Random.Range(0, s_Pool.Count)];
            m_RunStructures.Add(picked);
            return picked;
        }

        // Le chantier du début (3 novembre, le joueur) : les 3 tours d'un trio tiré au hasard, pour qu'un trio (et son ultime) soit
        // à portée dans chaque partie ; plus de Barricade en plus depuis le 12 novembre. Sans trio possible (pas de catalogue), au
        // hasard comme avant.
        void PickStartingStructures()
        {
            var catalog = TowerDuoCatalog.Instance;
            var trios = new List<(TowerDuoCatalog.Entry Trio, List<StructureDefinition> Towers)>();
            if (catalog != null)
            {
                foreach (var trio in catalog.Trios)
                {
                    var towers = TowersOf(catalog, trio);
                    if (towers != null)
                        trios.Add((trio, towers));
                }
            }

            if (trios.Count > 0)
            {
                var picked = trios[Random.Range(0, trios.Count)];
                m_StartingTrio = picked.Trio;
                m_RunStructures.AddRange(picked.Towers);
            }

            while (m_RunStructures.Count < k_StartingStructures)
            {
                if (AddRunStructure() == null)
                    break;
            }
        }

        // Les 3 tours d'un trio (les deux de son duo, et la sienne), toutes proposées par les coffres ; null sinon.
        List<StructureDefinition> TowersOf(TowerDuoCatalog catalog, TowerDuoCatalog.Entry trio)
        {
            if (trio == null || !trio.IsTrio || trio.definition == null || !(trio.definition.prefab is TowerTrio))
                return null;

            var duo = default(TowerDuoCatalog.Entry);
            foreach (var entry in catalog.Duos)
            {
                if (entry != null && entry.definition == trio.duo)
                    duo = entry;
            }

            if (duo == null)
                return null;

            var towers = new List<StructureDefinition>();
            foreach (var kind in new[] { duo.first, duo.second, trio.first })
            {
                var tower = m_Structures.Find(d => IsOffered(d) && d.kind == kind);
                if (tower == null || towers.Contains(tower))
                    return null;
                towers.Add(tower);
            }

            return towers;
        }

        static string NamesOf(List<StructureDefinition> definitions)
        {
            var names = definitions.ConvertAll(d => d.displayName);
            return names.Count > 1 ? string.Join(", ", names.GetRange(0, names.Count - 1)) + " et " + names[names.Count - 1]
                : names.Count == 1 ? names[0] : "";
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Build Manager dans la scène.", this);
            Instance = this;

            if (m_BlueprintMaterial == null)
                m_BlueprintMaterial = Resources.Load<Material>(k_BlueprintResource);
            if (m_BlueprintMaterial == null)
                Debug.LogWarning("BuildManager : aucun matériau de plan (Resources/Blueprint).", this);
            if (m_HammerPrefab == null)
                Debug.LogWarning("BuildManager : aucun prefab de marteau.", this);

            // Les zones constructibles et la pose à l'hologramme (refonte du 27 octobre, étape 3) : rien à poser dans la scène.
            if (GetComponent<BuildGrid>() == null && FindAnyObjectByType<BuildGrid>() == null)
                gameObject.AddComponent<BuildGrid>();
            if (GetComponent<BuildPlacement>() == null && FindAnyObjectByType<BuildPlacement>() == null)
                gameObject.AddComponent<BuildPlacement>();

            // Les flèches « DUO » entre deux tours qui peuvent fusionner, entre les vagues (31 octobre).
            if (GetComponent<DuoHints>() == null)
                gameObject.AddComponent<DuoHints>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // La vitrine (F4, 31 octobre) : chaque bâtiment, puis chaque duo, posé à plusieurs niveaux d'un coup.
            if (GetComponent<BuildShowcase>() == null)
                gameObject.AddComponent<BuildShowcase>();
#endif
        }

        void Start()
        {
            PickStartingStructures();

            m_Waves = WaveManager.Instance;
            if (m_Waves == null)
                return;

            m_Waves.GameOver += FinishNow;
            m_Waves.WaveStarted += OnWaveStarted;
            m_Waves.WaveEnded += OnWaveEnded;
        }

        void OnEnable()
        {
            Enemy.Damaged += OnEnemyDamaged;
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            Enemy.Damaged -= OnEnemyDamaged;
            Enemy.Killed -= OnEnemyKilled;
        }

        void OnDestroy()
        {
            if (m_Waves != null)
            {
                m_Waves.GameOver -= FinishNow;
                m_Waves.WaveStarted -= OnWaveStarted;
                m_Waves.WaveEnded -= OnWaveEnded;
            }

            if (Instance == this)
                Instance = null;
        }

        /// <summary>Les bâtiments qu'on peut construire sur ce type de socle (en hauteur : ceux du bord de la voie).</summary>
        public void GetChoices(BuildSlot slot, List<StructureDefinition> result)
        {
            result.Clear();
            foreach (var definition in m_Structures)
            {
                if (definition != null && definition.Fits(slot) && definition.prefab != null && !definition.retired)
                    result.Add(definition);
            }
        }

        // ---- Déplacer un bâtiment (refonte du 27 octobre, étape 3) : gratuit, entre les vagues, à l'hologramme (BuildPlacement).

        /// <summary>On peut déplacer un bâtiment en ce moment : entre les vagues, sans construction en cours.</summary>
        public bool CanMove => !IsBuilding && (m_Waves == null || m_Waves.Phase != WavePhase.Wave);

        /// <summary>
        /// Pose ce bâtiment sur cet emplacement libre, de la bonne sorte, tourné de <paramref name="localRotation"/> par rapport à
        /// l'emplacement.
        /// </summary>
        public ShopResult MoveTo(Structure structure, BuildSocket socket, Quaternion localRotation)
        {
            if (structure == null || socket == null)
                return ShopResult.Failed("Aucun emplacement");
            if (!CanMove)
                return ShopResult.Failed("On déplace les bâtiments entre les vagues");

            var definition = structure.Definition;
            var from = structure.Socket;
            if (socket.Structure != null && socket.Structure != structure)
                return ShopResult.Failed("Cet emplacement est déjà pris");
            if (definition != null && !definition.Fits(socket.Slot))
                return ShopResult.Failed($"Pas ici : {definition.displayName} se pose {BuildSocket.NameOf(definition.slot).ToLowerInvariant()}");

            if (from != null && from != socket)
                from.Detach();
            if (from != socket)
                socket.Attach(structure);
            structure.transform.localRotation = localRotation;
            structure.Setup(definition, socket);

            Sfx.Play(m_CompleteClip, socket.transform.position, 0.8f);
            FloatingText.Spawn(socket.transform.position + Vector3.up * 2f, $"{structure.DisplayName} déplacé", m_MessageColor, 1f, 1.5f);
            return ShopResult.Done($"{structure.DisplayName} déplacé", m_MessageColor);
        }

        // ---- Les duos (lot 7 de la refonte, 31 octobre ; Docs/Refonte_TowerDefense.md, 12.14 et 12.15) : deux tours partenaires,
        // chacune au moins au niveau 3, fusionnent quand on déplace l'une sur l'autre ; elles deviennent une tour nouvelle.

        /// <summary>
        /// La fusion que formeraient ces deux bâtiments (null s'ils ne sont pas partenaires) : un duo (deux tours), ou un trio (un
        /// duo et une tour, 1er novembre) ; et ce qui l'empêche en ce moment dans <paramref name="problem"/> (null si rien).
        /// </summary>
        public TowerDuoCatalog.Entry DuoOf(Structure a, Structure b, out string problem)
        {
            problem = null;
            if (a == null || b == null || a == b || a.Definition == null || b.Definition == null)
                return null;

            if (a is TowerDuo || b is TowerDuo)
                return TrioOf(a, b, out problem);

            var entry = TowerDuoCatalog.Find(a.Definition.kind, b.Definition.kind);
            if (entry == null)
                return null;

            if (!a.IsBuilt || !b.IsBuilt || a.IsUnderConstruction || b.IsUnderConstruction)
                problem = "Termine d'abord leur construction";
            else if (a.Level < m_DuoFromLevel || b.Level < m_DuoFromLevel)
                problem = $"Les deux tours au niveau {m_DuoFromLevel} au moins";
            else if (!CanMove)
                problem = "On fusionne entre les vagues";
            return entry;
        }

        // Un trio (1er novembre) : un duo (pas un trio) et une tour au moins au niveau 3 ; le duo, à n'importe quel niveau.
        TowerDuoCatalog.Entry TrioOf(Structure a, Structure b, out string problem)
        {
            problem = null;
            var duo = a as TowerDuo ?? b as TowerDuo;
            var tower = duo == a ? b : a;
            if (tower is TowerDuo)
                return null;

            var entry = TowerDuoCatalog.FindTrio(duo.Definition, tower.Definition.kind);
            if (entry == null)
                return null;

            if (duo is TowerTrio)
                problem = "Déjà un trio";
            else if (!duo.IsBuilt || !tower.IsBuilt || duo.IsUnderConstruction || tower.IsUnderConstruction)
                problem = "Termine d'abord leur construction";
            else if (tower.Level < m_DuoFromLevel)
                problem = $"{tower.DisplayName} au niveau {m_DuoFromLevel} au moins";
            else if (!CanMove)
                problem = "On fusionne entre les vagues";
            return entry;
        }

        /// <summary>
        /// Le niveau où naît le duo de ces deux tours : 1 (deux tours au niveau 3), 2 (l'une au niveau maximal), 3 (les deux). Un
        /// duo coûte ainsi autant de plans qu'on le fasse tôt puis le monte (2 plans par niveau depuis le 2 novembre, 4 avant), ou
        /// tard (31 octobre ; une tour monte du niveau 3 au niveau 5 pour 2 plans depuis le 2 novembre). Un trio
        /// (1er novembre) : le niveau du duo, un de plus si la tour est à son niveau maximal (3 au plus).
        /// </summary>
        public static int DuoLevelFrom(Structure a, Structure b)
        {
            var duo = a as TowerDuo ?? b as TowerDuo;
            if (duo != null)
            {
                var tower = duo == a ? b : a;
                return Mathf.Min(duo.MaxLevel, duo.Level + (tower != null && tower.Level >= tower.MaxLevel ? 1 : 0));
            }

            var full = (a != null && a.Level >= a.MaxLevel ? 1 : 0) + (b != null && b.Level >= b.MaxLevel ? 1 : 0);
            return 1 + full;
        }

        /// <summary>
        /// Fusionne la tour déplacée avec celle qu'elle vise : elles disparaissent, et le duo naît à la place de la tour visée (sur
        /// la voie, si l'une des deux y est), sur ses cases à lui.
        /// </summary>
        public ShopResult Fuse(Structure moving, Structure target)
        {
            var entry = DuoOf(moving, target, out var problem);
            if (entry == null)
                return ShopResult.Failed("Ces deux tours ne fusionnent pas");
            if (problem != null)
                return ShopResult.Failed(problem);

            var definition = entry.definition;
            var keep = DuoPlace(moving, target, definition);
            var socket = keep.Socket;
            var freed = (keep == moving ? target : moving).Socket;
            if (socket == null)
                return ShopResult.Failed("Aucun emplacement");

            var grid = BuildGrid.Instance;
            var cells = new List<int>();
            var spot = default(BuildGrid.Spot);
            var reclaim = grid != null && socket.IsCandidate;
            if (reclaim)
            {
                spot = grid.EvaluateInPlace(definition, socket, freed, cells);
                if (!spot.Valid)
                    return ShopResult.Failed($"Pas la place pour le {entry.Label.ToLowerInvariant()} ici : {spot.Reason}");
            }

            // Un trio garde les deux tours du duo, plus la troisième : leurs plans le font monter.
            var fromDuo = moving as TowerDuo ?? target as TowerDuo;
            var third = entry.IsTrio ? (fromDuo == moving ? target : moving).Definition : null;
            var first = entry.IsTrio ? fromDuo.First : moving.Definition;
            var second = entry.IsTrio ? fromDuo.Second : target.Definition;
            var level = DuoLevelFrom(moving, target);
            var localRotation = keep.transform.localRotation;

            // Les deux tours d'avant s'en vont ; leurs emplacements aussi, sauf celui que garde le duo.
            moving.Socket.Detach();
            if (target.Socket != null && target.Socket != moving.Socket)
                target.Socket.Detach();
            Destroy(moving.gameObject);
            Destroy(target.gameObject);
            if (grid != null && freed != null && freed != socket)
                grid.Release(freed);
            if (reclaim)
                grid.Reclaim(socket, spot, cells);

            var duo = (TowerDuo)socket.Place(definition);
            duo.transform.localRotation = localRotation;
            if (duo is TowerTrio trio)
                trio.SetupTrio(entry, first, second, third);
            else
                duo.SetupDuo(entry, first, second);
            duo.SetLevelImmediate(level);

            var top = socket.transform.position + Vector3.up * 3f;
            var title = $"{entry.Label} : {entry.displayName} !";
            Sfx.Play(m_DuoClip != null ? m_DuoClip : m_CompleteClip, top, 1f);
            FloatingText.Spawn(top + Vector3.up, title, entry.color, entry.IsTrio ? 1.8f : 1.4f, 2.5f);
            HeadMessages.Show(title + (string.IsNullOrEmpty(entry.description) ? "" : $"\n<size=65%>{entry.description}</size>"),
                              entry.color, 6f);
            return ShopResult.Done($"{entry.Label} : {entry.displayName}", entry.color);
        }

        /// <summary>« Duo », « Trio » ou « Ultime » : ce qu'est ce bâtiment fusionné, pour les messages.</summary>
        public static string FusionLabel(TowerDuo duo) => duo is TowerUltimate ? "Ultime" : duo is TowerTrio ? "Trio" : "Duo";

        // ---- Les ultimes (2 novembre ; Docs/Refonte_TowerDefense.md, 12.44) : le niveau 4 d'un trio au niveau 3. Des plans de
        // l'une de ses trois tours posés dessus (10), entre les vagues : le trio se transforme, puis on bâtit l'ultime au marteau.
        // Une par famille de trios, une seule par partie.

        /// <summary>Une ultime a déjà été bâtie dans cette partie (il n'y en a qu'une).</summary>
        public bool HasUltimate => m_UltimateBuilt;

        /// <summary>
        /// L'ultime que deviendrait ce bâtiment (un trio à son niveau maximal, dont la famille a son ultime ; null sinon), ses plans
        /// dans <paramref name="plans"/>, et ce qui l'empêche en ce moment dans <paramref name="problem"/> (null si rien).
        /// </summary>
        public TowerDuoCatalog.Entry UltimateOf(Structure structure, out string problem, out int plans)
        {
            problem = null;
            plans = 0;
            if (!(structure is TowerTrio trio) || structure is TowerUltimate || !structure.IsBuilt || structure.Level < structure.MaxLevel)
                return null;

            var entry = TowerDuoCatalog.FindUltimate(structure.Definition);
            if (entry == null)
                return null;

            plans = entry.definition.PlansFor(1);
            if (structure.IsUnderConstruction)
                problem = "Construction en cours";
            else if (m_UltimateBuilt)
                problem = "Une seule ultime par partie";
            else if (!CanMove)
                problem = "L'ultime se bâtit entre les vagues";
            else if (PlansOf(trio) < plans)
                problem = $"Il te faut {plans} plans : {SourceNames(trio, " ou ")}";
            return entry;
        }

        // Le trio devient son ultime, en plan bleu : ses plans payés, à sa place, sur ses cases ; le marteau la bâtit.
        ShopResult BuildUltimate(TowerTrio trio, StructureDefinition spentFrom, XRBaseInputInteractor hand)
        {
            var entry = UltimateOf(trio, out var problem, out var plans);
            if (entry == null)
                return ShopResult.Failed("Niveau maximal atteint");
            if (problem != null)
                return ShopResult.Failed(problem);

            var definition = entry.definition;
            var socket = trio.Socket;
            if (socket == null)
                return ShopResult.Failed("Aucun emplacement");

            var grid = BuildGrid.Instance;
            var cells = new List<int>();
            var spot = default(BuildGrid.Spot);
            var reclaim = grid != null && socket.IsCandidate;
            if (reclaim)
            {
                spot = grid.EvaluateInPlace(definition, socket, null, cells);
                if (!spot.Valid)
                    return ShopResult.Failed($"Pas la place pour l'ultime ici : {spot.Reason}");
            }

            if (!TrySpendDuoPlans(trio, spentFrom, plans))
                return ShopResult.Failed($"Il te faut {plans} plans : {SourceNames(trio, " ou ")}");

            var first = trio.First;
            var second = trio.Second;
            var third = trio.Third;
            var from = trio.Definition;
            var localRotation = trio.transform.localRotation;
            socket.Detach();
            Destroy(trio.gameObject);
            if (reclaim)
                grid.Reclaim(socket, spot, cells);

            var ultimate = (TowerUltimate)socket.Place(definition);
            ultimate.transform.localRotation = localRotation;
            ultimate.SetupUltimate(entry, first, second, third, from);
            m_UltimateBuilt = true;

            ultimate.BeginConstruction(1, m_BlueprintMaterial);
            m_Current = ultimate;
            m_Strikes = 0;
            m_FarTime = 0f;
            HandOverHammers(hand);

            var top = socket.transform.position + Vector3.up * 4f;
            Sfx.Play(m_UltimateClip != null ? m_UltimateClip : m_DuoClip != null ? m_DuoClip : m_CompleteClip, top, 1f);
            FloatingText.Spawn(top + Vector3.up, $"Ultime : {entry.displayName} !", entry.color, 2.2f, 2.5f);
            Announce($"Ultime : {entry.displayName}\n<size=70%>Frappe le plan {m_StrikesToBuild} fois avec tes marteaux</size>");
            return ShopResult.Done($"Ultime : {entry.displayName}", entry.color);
        }

        /// <summary>Les noms des tours dont les plans font monter ce duo (ou ce trio), séparés par des virgules.</summary>
        public static string SourceNames(TowerDuo duo, string separator = ", ")
        {
            var names = new List<string>();
            if (duo != null)
            {
                foreach (var source in duo.PlanSources)
                {
                    if (source != null)
                        names.Add(source.displayName);
                }
            }

            return string.Join(separator, names);
        }

        /// <summary>
        /// Où naît le duo de ces deux tours : à la place de la tour visée ; une tour de la voie et une du bord, sur la voie. Sauf si
        /// le duo (<paramref name="result"/>) va au bord : alors à la place de celle du bord (1er novembre au soir : la Grande
        /// bombarde, au bord, naît du Feu d'artifice, au bord, et du tremplin, sur la voie ; posée sur la voie, la grille la refusait).
        /// </summary>
        public static Structure DuoPlace(Structure moving, Structure target, StructureDefinition result = null)
        {
            if (moving.Definition == null || target.Definition == null)
                return target;

            var movingOnLane = moving.Definition.slot == BuildSlot.Lane;
            var targetOnLane = target.Definition.slot == BuildSlot.Lane;
            if (movingOnLane == targetOnLane)
                return target;
            var wantsLane = result == null || result.slot == BuildSlot.Lane;
            return movingOnLane == wantsLane ? moving : target;
        }

        /// <summary>Les plans qui font monter ce duo : ceux de ses deux tours (de ses trois tours, pour un trio).</summary>
        public int PlansOf(TowerDuo duo)
        {
            var total = 0;
            if (duo != null)
            {
                foreach (var source in duo.PlanSources)
                    total += PlansOf(source);
            }

            return total;
        }

        // Les plans d'un niveau de duo : d'abord ceux du bâtiment donné (le plan sorti de la montre), puis ceux des autres tours,
        // dans l'ordre (les deux du duo, puis la troisième d'un trio).
        bool TrySpendDuoPlans(TowerDuo duo, StructureDefinition preferred, int spent)
        {
            if (PlansOf(duo) < spent)
                return false;

            var order = new List<StructureDefinition>();
            if (preferred != null && duo.AcceptsPlansOf(preferred))
                order.Add(preferred);
            foreach (var source in duo.PlanSources)
            {
                if (source != null && !order.Contains(source))
                    order.Add(source);
            }

            foreach (var source in order)
            {
                var taken = Mathf.Min(spent, PlansOf(source));
                if (taken <= 0)
                    continue;
                m_Plans[source] = PlansOf(source) - taken;
                spent -= taken;
                if (spent <= 0)
                    break;
            }

            PlansChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Construit un bâtiment sur un socle vide, ou le niveau suivant de celui qui y est, avec un plan de ce bâtiment.
        /// Le marteau apparaît près de la main donnée (celle qui a ouvert le panneau). Un nouveau bâtiment peut être tourné de
        /// <paramref name="localRotation"/> par rapport au socle (l'hologramme tourné au joystick, 27 octobre).
        /// </summary>
        public ShopResult TryBuild(BuildSocket socket, StructureDefinition definition, XRBaseInputInteractor hand, Quaternion? localRotation = null,
                                   int levels = 1)
        {
            if (socket == null)
                return ShopResult.Failed("Aucun emplacement");
            if (IsBuilding)
                return ShopResult.Failed("Termine d'abord la construction en cours");

            var structure = socket.Structure;
            var spentFrom = definition;
            if (structure != null)
                definition = structure.Definition;
            if (definition == null || definition.prefab == null)
                return ShopResult.Failed("Bâtiment introuvable");

            var level = structure != null ? structure.Level + 1 : 1;
            if (level > definition.MaxLevel)
            {
                // Un trio au niveau 3 : son niveau 4, l'ultime de sa famille (2 novembre).
                if (structure is TowerTrio trio && UltimateOf(structure, out _, out _) != null)
                    return BuildUltimate(trio, spentFrom, hand);
                return ShopResult.Failed("Niveau maximal atteint");
            }

            // Plusieurs niveaux d'un coup (8 novembre, le joueur : un plan, une ouverture du panneau et trois coups de marteau
            // par niveau devenaient redondants, avec 19 plans en poche) : les plans de chaque niveau, un seul chantier.
            if (structure != null && levels > 1)
            {
                if (level + levels - 1 > definition.MaxLevel)
                    return ShopResult.Failed("Niveau maximal dépassé");
                level += levels - 1;
            }

            // Refonte du 27 octobre : un plan du bâtiment à la place des matériaux, pour le construire comme pour l'améliorer ;
            // un seul par niveau depuis le 2 novembre (deux aux niveaux 4 et 5 avant). Un duo (31 octobre), de l'une ou l'autre de
            // ses tours : 2 plans par niveau, un trio 3 (le joueur, 2 novembre ; 4 avant, réglage du niveau dans Data/Duos).
            var plans = structure != null ? PlansToReach(structure, level) : definition.PlansFor(level);
            if (structure is TowerDuo duo)
            {
                if (!TrySpendDuoPlans(duo, spentFrom, plans))
                    return ShopResult.Failed($"Il te faut {plans} plans : {SourceNames(duo, " ou ")}");
            }
            else if (!TrySpendPlans(definition, plans))
            {
                return ShopResult.Failed(plans > 1 ? $"Il te faut {plans} plans : {definition.displayName}"
                                             : $"Il te faut un plan : {definition.displayName}");
            }

            if (structure == null)
            {
                structure = socket.Place(definition);
                if (localRotation.HasValue)
                    structure.transform.localRotation = localRotation.Value;
            }

            structure.BeginConstruction(level, m_BlueprintMaterial);
            m_Current = structure;
            m_Strikes = 0;
            m_FarTime = 0f;
            HandOverHammers(hand);

            var title = level == 1 ? definition.displayName : $"{definition.displayName} niveau {level}";
            Announce($"{title}\n<size=70%>Frappe le plan {m_StrikesToBuild} fois avec tes marteaux</size>");
            return ShopResult.Done(level == 1 ? $"{definition.displayName} : à toi de construire !" : $"Amélioration : niveau {level}", definition.color);
        }

        /// <summary>
        /// Les plans qu'il faut pour monter ce bâtiment jusqu'au niveau donné, niveau après niveau (8 novembre : l'amélioration de
        /// plusieurs niveaux d'un coup). 0 s'il y est déjà, ou si ce niveau n'existe pas.
        /// </summary>
        public static int PlansToReach(Structure structure, int level)
        {
            var definition = structure != null ? structure.Definition : null;
            if (definition == null || level <= structure.Level || level > definition.MaxLevel)
                return 0;

            var plans = 0;
            for (var next = structure.Level + 1; next <= level; next++)
                plans += definition.PlansFor(next);
            return plans;
        }

        /// <summary>Les plans qu'on a pour améliorer ce bâtiment (ceux de ses tours, pour un duo ou un trio).</summary>
        public int PlansFor(Structure structure) =>
            structure is TowerDuo duo ? PlansOf(duo) : structure != null ? PlansOf(structure.Definition) : 0;

        // L'arc se range, et un marteau apparaît dans chaque main, sans rien attraper (sans casque : dans la main qui a
        // ouvert le panneau).
        void HandOverHammers(XRBaseInputInteractor opener)
        {
            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            if (m_Bow != null)
                m_Bow.SetStowed(true);

            if (m_HammerPrefab == null)
                return;

            var hands = new List<XRBaseInputInteractor>();
            var rig = PlayerRig.Instance;
            if (rig != null)
            {
                foreach (var side in new[] { InteractorHandedness.Left, InteractorHandedness.Right })
                {
                    if (rig.TryGetHand(side, out var hand) && hand != null && !hands.Contains(hand))
                        hands.Add(hand);
                }
            }

            if (hands.Count == 0 && opener != null)
                hands.Add(opener);

            foreach (var hand in hands)
            {
                // La main lâche ce qu'elle tient encore (une flèche).
                if (hand.hasSelection && hand.interactionManager != null)
                    hand.interactionManager.CancelInteractorSelection((IXRSelectInteractor)hand);

                var hammer = Instantiate(m_HammerPrefab);
                hammer.Begin(this, m_Current, hand);
                m_Hammers.Add(hammer);
                Haptics.Pulse(hand, 0.4f, 0.08f);
            }

            if (hands.Count > 0)
                Sfx.Play(m_HammerClip, hands[0].transform.position, 0.8f);
        }

        /// <summary>Un coup de marteau sur le plan en construction.</summary>
        internal void OnHammerStrike(BuildHammer hammer, Vector3 point, float speed, IXRSelectInteractor hand)
        {
            if (!m_Hammers.Contains(hammer) || m_Current == null)
                return;

            m_Strikes++;
            var progress = (float)m_Strikes / m_StrikesToBuild;
            m_Current.SetProgress(progress);

            Haptics.Pulse(hand, Mathf.Clamp01(0.5f + speed * 0.1f), 0.1f);
            if (m_StrikeClips.Length > 0)
                Sfx.Play(m_StrikeClips[Random.Range(0, m_StrikeClips.Length)], point, 1f, Random.Range(0.92f, 1.08f));
            if (m_StrikeEffect != null)
                Destroy(Instantiate(m_StrikeEffect, point, Quaternion.LookRotation(Vector3.up)), 2f);

            if (m_Strikes >= m_StrikesToBuild)
                Complete();
            else
                FloatingText.Spawn(point + Vector3.up * 0.3f, $"{m_Strikes}/{m_StrikesToBuild}", m_MessageColor, 0.8f, 0.9f);
        }

        void Update()
        {
            UpdateDebugKey();
            if (m_Current == null)
                return;

            // Le joueur est parti (téléporteur de la tour…) : la construction se termine sans lui.
            var rig = PlayerRig.Instance;
            if (rig == null)
                return;

            var offset = rig.BodyPosition - m_Current.ConstructionBounds.ClosestPoint(rig.BodyPosition);
            offset.y = 0f;
            m_FarTime = offset.magnitude > m_AutoFinishDistance ? m_FarTime + Time.unscaledDeltaTime : 0f;
            if (m_FarTime > 1f)
                Complete();
        }

        /// <summary>Termine tout de suite la construction en cours (fin de partie).</summary>
        public void FinishNow()
        {
            if (m_Current != null)
                Complete();
        }

        void Complete()
        {
            var structure = m_Current;
            m_Current = null;
            structure.FinishConstruction();

            foreach (var hammer in m_Hammers)
            {
                if (hammer != null)
                    Destroy(hammer.gameObject);
            }

            m_Hammers.Clear();

            if (m_Bow != null)
                m_Bow.SetStowed(false);

            var center = structure.ConstructionBounds.center;
            Sfx.Play(m_CompleteClip, center, 1f);
            var text = structure.Level == 1 ? $"{structure.DisplayName} : construction terminée !" : $"{structure.DisplayName} : niveau {structure.Level} !";
            FloatingText.Spawn(new Vector3(center.x, structure.ConstructionBounds.max.y + 0.4f, center.z), text,
                               structure.Definition != null ? structure.Definition.color : m_MessageColor, 1.1f, 2f);

            // L'ultime bâtie (2 novembre) : son nom et ce qu'elle fait, comme une fusion.
            if (structure is TowerUltimate ultimate && ultimate.Entry != null)
            {
                var entry = ultimate.Entry;
                Sfx.Play(m_UltimateClip != null ? m_UltimateClip : m_DuoClip, center, 1f);
                HeadMessages.Show($"Ultime : {entry.displayName} !" +
                                  (string.IsNullOrEmpty(entry.description) ? "" : $"\n<size=65%>{entry.description}</size>"), entry.color, 7f);
            }
        }

        void Announce(string text)
        {
            HeadMessages.Show(text, m_MessageColor, 4f);
        }

        // ---- Bilan de la vague : part des dégâts et éliminations des bâtiments.

        void OnWaveStarted(int wave)
        {
            m_WaveDamage = 0f;
            m_StructureDamage = 0f;
            m_StructureKills = 0;
            m_ReportByName.Clear();

            // Au premier gong, le chantier de la partie (2 novembre), et le trio qu'il permet (3 novembre).
            if (wave == 1 && m_RunStructures.Count > 0)
                HeadMessages.Show($"Ton chantier : {NamesOf(m_RunStructures)}\n<size=70%>" +
                                  (m_StartingTrio != null ? $"De quoi bâtir un trio : {m_StartingTrio.displayName}. " : "") +
                                  "Les coffres donnent leurs plans ; un bâtiment de plus après chaque boss</size>", m_MessageColor, 6f);
        }

        // Après chaque boss (la fin de la vague), des bâtiments rejoignent le chantier (2 novembre).
        void GrowRunStructures()
        {
            var added = new List<StructureDefinition>();
            for (var i = 0; i < m_StructuresPerBoss; i++)
            {
                var definition = AddRunStructure();
                if (definition != null)
                    added.Add(definition);
            }

            if (added.Count > 0)
                HeadMessages.Show($"Nouveau au chantier : {NamesOf(added)} !\n<size=70%>Les coffres proposent aussi ses plans</size>",
                                  added[0].color, 5f);
        }

        void OnEnemyDamaged(Enemy enemy, DamageInfo info)
        {
            // Les mannequins du centre d'entraînement (26 octobre) n'entrent pas dans le bilan de la vague.
            if (enemy != null && enemy.IsTrainingDummy)
                return;

            m_WaveDamage += info.Amount;
            if (!(info.Source is Structure structure))
                return;

            m_StructureDamage += info.Amount;
            m_ReportByName.TryGetValue(structure.DisplayName, out var entry);
            m_ReportByName[structure.DisplayName] = entry + new Vector2(info.Amount, 0f);
        }

        void OnEnemyKilled(Enemy enemy, DamageInfo info)
        {
            if (!(info.Source is Structure structure) || (enemy != null && enemy.IsTrainingDummy))
                return;

            m_StructureKills++;
            m_ReportByName.TryGetValue(structure.DisplayName, out var entry);
            m_ReportByName[structure.DisplayName] = entry + new Vector2(0f, 1f);
        }

        void OnWaveEnded(int wave)
        {
            // Pas à la victoire contre le boss final (le joueur est dans l'arène).
            if (!(m_Waves.HasWon && wave == m_Waves.WavesToWin))
                GrowRunStructures();
            if (!m_ShowWaveReport || m_StructureDamage <= 0f || m_WaveDamage <= 0f)
                return;

            var share = Mathf.RoundToInt(100f * m_StructureDamage / m_WaveDamage);
            var kills = m_StructureKills > 1 ? m_StructureKills + " éliminations" : m_StructureKills + " élimination";
            HeadMessages.Show($"Défenses : {share} % des dégâts\n<size=70%>{kills}</size>", m_MessageColor, 5f, 0.85f);

            m_Builder.Clear();
            m_Builder.Append("Bilan des bâtiments, vague ").Append(wave).Append(" : ").Append(share).Append(" % des dégâts infligés aux ennemis, ")
                     .Append(kills).Append(" (force des ennemis ×").Append(Structure.EnemyStrength.ToString("0.00"))
                     .Append(", parchemins : dégâts ×").Append(Structure.ScrollStrength.ToString("0.00"))
                     .Append(", cadence ×").Append(Structure.ScrollRate.ToString("0.00"))
                     .Append(", PV ×").Append(Structure.ScrollHealthMultiplier.ToString("0.00")).Append(')');
            foreach (var pair in m_ReportByName)
                m_Builder.Append("\n  ").Append(pair.Key).Append(" : ").Append(Mathf.RoundToInt(pair.Value.x)).Append(" dégâts, ")
                         .Append((int)pair.Value.y).Append(" éliminations");
            Debug.Log(m_Builder.ToString(), this);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // F3 : chaque ennemi au sol à moins de 15 m saute de 7 m/s, poussé loin du joueur (3 m/s) ; un boss est seulement poussé.
        void LaunchNearbyEnemies()
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
                return;

            var launched = 0;
            var player = rig.BodyPosition;
            foreach (var enemy in new List<Enemy>(Enemy.Alive))
            {
                if (enemy == null || enemy.IsTrainingDummy)
                    continue;

                var away = enemy.transform.position - player;
                away.y = 0f;
                if (away.sqrMagnitude > 15f * 15f)
                    continue;

                var flat = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
                if (enemy.Launch(Vector3.up * 7f + flat * 3f, this))
                    launched++;
            }

            Announce(launched > 0 ? $"Test : {launched} ennemi{(launched > 1 ? "s" : "")} projeté{(launched > 1 ? "s" : "")}" : "Test : aucun ennemi à projeter près de toi");
        }
#endif

        // Raccourcis de test au clavier (éditeur) : B donne 2 plans de chaque bâtiment non retiré (50 matériaux avant le 27 octobre)
        // et les met tous au chantier (2 novembre), H termine
        // la construction en cours, F3 projette en l'air les ennemis à moins de 15 m du joueur (l'état « projeté » des
        // constructions physiques, Enemy.Launch.cs, 27 octobre).
        void UpdateDebugKey()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.bKey.wasPressedThisFrame)
            {
                foreach (var definition in m_Structures)
                {
                    if (definition != null && definition.prefab != null && !definition.retired)
                        AddPlans(definition, 2);
                    if (IsOffered(definition) && !m_RunStructures.Contains(definition))
                        m_RunStructures.Add(definition);
                }

                Announce("Test : 2 plans de chaque tour, toutes au chantier");
            }
            if (keyboard.hKey.wasPressedThisFrame)
                FinishNow();
            if (keyboard.f3Key.wasPressedThisFrame)
                LaunchNearbyEnemies();
#endif
        }
    }
}
