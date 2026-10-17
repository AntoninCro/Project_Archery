using System.Collections.Generic;
using System.Text;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Economy;
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
    /// Constructions du tower defense (GDD, section 9). À l'achat sur un <see cref="BuildSocket"/>, le bâtiment
    /// apparaît en plan bleu, l'arc se range et un marteau apparaît dans chaque main, sans rien attraper. Trois coups
    /// sur le plan, avec l'un ou l'autre, le construisent ; les marteaux disparaissent et l'arc revient. Une
    /// amélioration se fait de la même façon.
    /// </summary>
    /// <remarks>
    /// À placer sur l'objet <c>Game</c>. Une seule construction à la fois. Elle se termine d'elle-même si le joueur
    /// s'éloigne (téléporteur de la tour) ou si la partie se termine : la construction n'est jamais un obstacle.
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

        /// <summary>Les bâtiments qu'on peut construire sur ce type de socle.</summary>
        public void GetChoices(BuildSlot slot, List<StructureDefinition> result)
        {
            result.Clear();
            foreach (var definition in m_Structures)
            {
                if (definition != null && definition.slot == slot && definition.prefab != null)
                    result.Add(definition);
            }
        }

        /// <summary>
        /// Achète la construction d'un bâtiment sur un socle vide, ou le niveau suivant de celui qui y est.
        /// Le marteau apparaît près de la main donnée (celle qui a ouvert le panneau).
        /// </summary>
        public ShopResult TryBuild(BuildSocket socket, StructureDefinition definition, XRBaseInputInteractor hand)
        {
            if (socket == null)
                return ShopResult.Failed("Aucun emplacement");
            if (IsBuilding)
                return ShopResult.Failed("Termine d'abord la construction en cours");

            var structure = socket.Structure;
            if (structure != null)
                definition = structure.Definition;
            if (definition == null || definition.prefab == null)
                return ShopResult.Failed("Bâtiment introuvable");

            var level = structure != null ? structure.Level + 1 : 1;
            if (level > definition.MaxLevel)
                return ShopResult.Failed("Niveau maximal atteint");

            var cost = definition.CostFor(level);
            var score = ScoreManager.Instance;
            if (score == null || !score.TrySpendMaterials(cost))
                return ShopResult.Failed("Pas assez de matériaux");

            if (structure == null)
                structure = socket.Place(definition);

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
            m_WaveDamage += info.Amount;
            if (!(info.Source is Structure structure))
                return;

            m_StructureDamage += info.Amount;
            m_ReportByName.TryGetValue(structure.DisplayName, out var entry);
            m_ReportByName[structure.DisplayName] = entry + new Vector2(info.Amount, 0f);
        }

        void OnEnemyKilled(Enemy enemy, DamageInfo info)
        {
            if (!(info.Source is Structure structure))
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

        // Raccourcis de test au clavier (éditeur) : B donne 50 matériaux, H termine la construction en cours.
        void UpdateDebugKey()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.bKey.wasPressedThisFrame && ScoreManager.Instance != null)
                ScoreManager.Instance.AddMaterials(50);
            if (keyboard.hKey.wasPressedThisFrame)
                FinishNow();
#endif
        }
    }
}
