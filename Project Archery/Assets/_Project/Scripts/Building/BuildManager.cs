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
        Structure m_Current;
        readonly List<BuildHammer> m_Hammers = new List<BuildHammer>();
        Bow m_Bow;
        WaveManager m_Waves;
        int m_Strikes;
        float m_FarTime;

        public static BuildManager Instance { get; private set; }

        public IReadOnlyList<StructureDefinition> Structures => m_Structures;

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
        }

        void Start()
        {
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
        /// Le duo que formeraient ces deux tours (null si elles ne sont pas partenaires), et ce qui l'empêche en ce moment dans
        /// <paramref name="problem"/> (null si rien).
        /// </summary>
        public TowerDuoCatalog.Entry DuoOf(Structure a, Structure b, out string problem)
        {
            problem = null;
            if (a == null || b == null || a == b || a.Definition == null || b.Definition == null)
                return null;

            var entry = TowerDuoCatalog.Find(a.Definition.kind, b.Definition.kind);
            if (entry == null)
                return null;

            if (a is TowerDuo || b is TowerDuo)
                problem = "Déjà un duo";
            else if (!a.IsBuilt || !b.IsBuilt || a.IsUnderConstruction || b.IsUnderConstruction)
                problem = "Termine d'abord leur construction";
            else if (a.Level < m_DuoFromLevel || b.Level < m_DuoFromLevel)
                problem = $"Les deux tours au niveau {m_DuoFromLevel} au moins";
            else if (!CanMove)
                problem = "On fusionne entre les vagues";
            return entry;
        }

        /// <summary>
        /// Le niveau où naît le duo de ces deux tours : 1 (deux tours au niveau 3), 2 (l'une au niveau maximal), 3 (les deux). Un
        /// duo coûte ainsi autant de plans qu'on le fasse tôt puis le monte (4 plans par niveau), ou tard (31 octobre).
        /// </summary>
        public static int DuoLevelFrom(Structure a, Structure b)
        {
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
            var keep = DuoPlace(moving, target);
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
                    return ShopResult.Failed($"Pas la place pour le duo ici : {spot.Reason}");
            }

            var first = moving.Definition;
            var second = target.Definition;
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
            duo.SetupDuo(entry, first, second);
            duo.SetLevelImmediate(level);

            var top = socket.transform.position + Vector3.up * 3f;
            Sfx.Play(m_DuoClip != null ? m_DuoClip : m_CompleteClip, top, 1f);
            FloatingText.Spawn(top + Vector3.up, $"Duo : {entry.displayName} !", entry.color, 1.4f, 2.5f);
            HeadMessages.Show($"Duo : {entry.displayName} !" + (string.IsNullOrEmpty(entry.description) ? "" : $"\n<size=65%>{entry.description}</size>"),
                              entry.color, 6f);
            return ShopResult.Done($"Duo : {entry.displayName}", entry.color);
        }

        /// <summary>Où naît le duo de ces deux tours : à la place de la tour visée ; une tour de la voie et une du bord, sur la voie.</summary>
        public static Structure DuoPlace(Structure moving, Structure target) =>
            moving.Definition != null && target.Definition != null && moving.Definition.slot == BuildSlot.Lane &&
            target.Definition.slot != BuildSlot.Lane ? moving : target;

        /// <summary>Les plans qui font monter ce duo : ceux de ses deux tours.</summary>
        public int PlansOf(TowerDuo duo) => duo != null ? PlansOf(duo.First) + PlansOf(duo.Second) : 0;

        // Les plans d'un niveau de duo : d'abord ceux du bâtiment donné (le plan sorti de la montre), puis ceux de l'autre tour.
        bool TrySpendDuoPlans(TowerDuo duo, StructureDefinition preferred, int spent)
        {
            if (PlansOf(duo) < spent)
                return false;

            var first = preferred == duo.Second ? duo.Second : duo.First;
            var second = first == duo.First ? duo.Second : duo.First;
            var fromFirst = Mathf.Min(spent, PlansOf(first));
            if (fromFirst > 0)
                m_Plans[first] = PlansOf(first) - fromFirst;
            if (spent - fromFirst > 0 && second != null)
                m_Plans[second] = PlansOf(second) - (spent - fromFirst);
            PlansChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Construit un bâtiment sur un socle vide, ou le niveau suivant de celui qui y est, avec un plan de ce bâtiment.
        /// Le marteau apparaît près de la main donnée (celle qui a ouvert le panneau). Un nouveau bâtiment peut être tourné de
        /// <paramref name="localRotation"/> par rapport au socle (l'hologramme tourné au joystick, 27 octobre).
        /// </summary>
        public ShopResult TryBuild(BuildSocket socket, StructureDefinition definition, XRBaseInputInteractor hand, Quaternion? localRotation = null)
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
                return ShopResult.Failed("Niveau maximal atteint");

            // Refonte du 27 octobre : un plan du bâtiment à la place des matériaux, pour le construire comme pour l'améliorer ;
            // deux aux niveaux 4 et 5 (29 octobre). Un duo (31 octobre) : 4 plans par niveau, de l'une ou l'autre de ses tours.
            var plans = definition.PlansFor(level);
            if (structure is TowerDuo duo)
            {
                if (!TrySpendDuoPlans(duo, spentFrom, plans))
                    return ShopResult.Failed($"Il te faut {plans} plans : {duo.First?.displayName} ou {duo.Second?.displayName}");
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
            if (!m_ShowWaveReport || m_StructureDamage <= 0f || m_WaveDamage <= 0f)
                return;

            var share = Mathf.RoundToInt(100f * m_StructureDamage / m_WaveDamage);
            var kills = m_StructureKills > 1 ? m_StructureKills + " éliminations" : m_StructureKills + " élimination";
            HeadMessages.Show($"Défenses : {share} % des dégâts\n<size=70%>{kills}</size>", m_MessageColor, 5f, 0.85f);

            m_Builder.Clear();
            m_Builder.Append("Bilan des bâtiments, vague ").Append(wave).Append(" : ").Append(share).Append(" % des dégâts infligés aux ennemis, ")
                     .Append(kills).Append(" (force des ennemis ×").Append(Structure.EnemyStrength.ToString("0.00")).Append(')');
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

        // Raccourcis de test au clavier (éditeur) : B donne 2 plans de chaque bâtiment non retiré (50 matériaux avant le 27 octobre), H termine
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
                }

                Announce("Test : 2 plans de chaque tour");
            }
            if (keyboard.hKey.wasPressedThisFrame)
                FinishNow();
            if (keyboard.f3Key.wasPressedThisFrame)
                LaunchNearbyEnemies();
#endif
        }
    }
}
