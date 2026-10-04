using System;
using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Difficulty;
using Archery.Economy;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Waves
{
    public enum WavePhase
    {
        /// <summary>Avant la première vague : on attend le gong.</summary>
        Ready,

        /// <summary>Une vague est en cours, le chrono tourne.</summary>
        Wave,

        /// <summary>Pause entre deux vagues (boutique, entraînement) : on attend le gong.</summary>
        Intermission,

        /// <summary>Le joueur est mort.</summary>
        GameOver,
    }

    /// <summary>
    /// Enchaîne les vagues (GDD, section 3) : chrono, apparition des ennemis selon un budget,
    /// fuite des survivants à la fin du chrono, PV du joueur restaurés, bonus de fin de vague,
    /// victoire après la dernière vague puis mode infini.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(10)]
    public class WaveManager : MonoBehaviour
    {
        [SerializeField]
        WaveSettings m_Settings;

        [Tooltip("Points d'apparition. Laisser vide pour prendre celui de la scène.")]
        [SerializeField]
        EnemySpawner m_Spawner;

        [Tooltip("Pour tester : lance la première vague toute seule après ce délai (s). 0 = attendre le gong.")]
        [SerializeField]
        float m_AutoStartDelay;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_WaveStartClip;

        [SerializeField]
        AudioClip m_WaveClearClip;

        [Tooltip("Joué à chacune des 5 dernières secondes.")]
        [SerializeField]
        AudioClip m_CountdownClip;

        [SerializeField]
        AudioClip m_VictoryClip;

        [SerializeField]
        AudioClip m_GameOverClip;

        [Header("Événements (ex. : afficher les cibles d'entraînement pendant les pauses)")]
        [SerializeField]
        UnityEvent m_OnWaveStarted = new UnityEvent();

        [SerializeField]
        UnityEvent m_OnWaveEnded = new UnityEvent();

        [SerializeField]
        UnityEvent m_OnGameOver = new UnityEvent();

        readonly List<Enemy> m_SpawnQueue = new List<Enemy>();
        readonly List<WaveEnemy> m_Candidates = new List<WaveEnemy>();
        readonly List<Enemy> m_Fleeing = new List<Enemy>();
        float m_SpawnInterval;
        float m_SpawnTimer;
        int m_NextCountdownSecond;
        Health m_PlayerHealth;
        float m_CountScale = 1f;
        bool m_IsBossWave;
        bool m_BossSpawned;
        bool m_ReinforcementsEndAnnounced;
        float m_BossTimer;
        Enemy m_Boss;

        public static WaveManager Instance { get; private set; }

        public WavePhase Phase { get; private set; } = WavePhase.Ready;

        /// <summary>Numéro de la vague en cours, ou de la dernière jouée (0 avant la première).</summary>
        public int WaveNumber { get; private set; }

        public float TimeRemaining { get; private set; }
        public float WaveDuration { get; private set; }
        public int WavesToWin => m_Settings != null ? m_Settings.wavesToWin : 0;
        public bool IsEndless => WaveNumber > WavesToWin;
        public bool CanStartWave => Phase == WavePhase.Ready || Phase == WavePhase.Intermission;

        /// <summary>La vague en cours est une vague de boss : elle ne se termine qu'à la mort du boss.</summary>
        public bool IsBossWave => Phase == WavePhase.Wave && m_IsBossWave;

        /// <summary>Le chrono est fini (plus de renforts), mais le boss est encore en vie.</summary>
        public bool IsWaitingForBoss => IsBossWave && TimeRemaining <= 0f;

        // La difficulté et la courbe des vagues augmentent aussi le maximum d'ennemis en même temps (moins vite que leur nombre).
        int MaxAlive => Mathf.Clamp(Mathf.RoundToInt(m_Settings.maxAlive * DifficultyManager.Current.enemyCount * Mathf.Sqrt(m_CountScale)),
                                    1, Mathf.Max(1, m_Settings.maxAliveCap));

        public event Action<int> WaveStarted;
        public event Action<int> WaveEnded;
        public event Action GameOver;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Wave Manager dans la scène.", this);
            Instance = this;
            EnemyScaling.Reset();

            if (m_Spawner == null)
                m_Spawner = FindAnyObjectByType<EnemySpawner>();
            if (m_Settings == null)
                Debug.LogError("WaveManager : aucun Wave Settings assigné.", this);
            if (m_Spawner == null)
                Debug.LogError("WaveManager : aucun Enemy Spawner dans la scène.", this);
        }

        void Start()
        {
            // C'est le gestionnaire de vagues qui décide des apparitions, pas le spawner.
            if (m_Spawner != null && m_Spawner.IsSpawning)
            {
                m_Spawner.IsSpawning = false;
                Debug.Log("WaveManager : le spawner n'apparaît plus tout seul (décoche « Spawn On Start »).", m_Spawner);
            }

            var player = PlayerHealth.Instance;
            if (player != null)
            {
                m_PlayerHealth = player.Health;
                m_PlayerHealth.Died += OnPlayerDied;
            }

            if (m_AutoStartDelay > 0f)
                Invoke(nameof(StartFirstWaveAutomatically), m_AutoStartDelay);
        }

        void OnDestroy()
        {
            if (m_PlayerHealth != null)
                m_PlayerHealth.Died -= OnPlayerDied;
            if (Instance == this)
                Instance = null;
        }

        void StartFirstWaveAutomatically() => StartNextWave();

        /// <summary>Lance la vague suivante (gong). Renvoie faux si ce n'est pas possible maintenant.</summary>
        public bool StartNextWave()
        {
            if (!CanStartWave || m_Settings == null)
                return false;

            WaveNumber++;
            Phase = WavePhase.Wave;
            WaveDuration = m_Settings.DurationFor(WaveNumber);
            TimeRemaining = WaveDuration;
            m_NextCountdownSecond = 5;

            // Courbe de difficulté exponentielle, plus raide en mode infini.
            // Selon la difficulté, PV et dégâts sont réduits au début de la partie (vague 1), puis rejoignent la courbe.
            var difficulty = DifficultyManager.Current;
            var early = difficulty.EarlyStrength(WaveNumber);
            EnemyScaling.Health = m_Settings.Growth(WaveNumber, difficulty.healthGrowthPerWave, m_Settings.endlessHealthGrowth) * early;
            EnemyScaling.Damage = m_Settings.Growth(WaveNumber, difficulty.damageGrowthPerWave, m_Settings.endlessDamageGrowth) * early;
            EnemyScaling.Speed = Mathf.Min(Mathf.Max(1f, m_Settings.maxSpeedScale),
                                           m_Settings.Growth(WaveNumber, difficulty.speedGrowthPerWave, m_Settings.endlessSpeedGrowth));
            m_CountScale = m_Settings.Growth(WaveNumber, difficulty.countGrowthPerWave, m_Settings.endlessCountGrowth);

            m_IsBossWave = m_Settings.IsBossWave(WaveNumber);
            m_BossSpawned = false;
            m_ReinforcementsEndAnnounced = false;
            m_BossTimer = m_Settings.bossSpawnDelay;
            m_Boss = null;

            BuildSpawnQueue();
            m_SpawnInterval = m_SpawnQueue.Count > 0 ? WaveDuration * m_Settings.spawnWindow / m_SpawnQueue.Count : 0f;
            m_SpawnTimer = 1f;

            var title = IsEndless ? $"Vague {WaveNumber}\nmode infini" : $"Vague {WaveNumber} / {WavesToWin}";
            if (m_IsBossWave)
                title += "\nUn boss approche !";
            else if (WaveNumber == 1)
                title += "\n" + DifficultyManager.Current.displayName;
            Announce(title, new Color(1f, 0.85f, 0.3f), m_WaveStartClip);

            WaveStarted?.Invoke(WaveNumber);
            m_OnWaveStarted.Invoke();
            return true;
        }

        void Update()
        {
            UpdateDebugKeys();
            if (Phase != WavePhase.Wave)
                return;

            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            if (TimeRemaining > 0f)
                UpdateSpawning(Time.deltaTime);

            if (!m_IsBossWave)
            {
                UpdateCountdown();
                if (TimeRemaining <= 0f)
                    EndWave();
                return;
            }

            // Vague de boss : le chrono n'arrête que les renforts, la vague se termine à la mort du boss.
            UpdateBoss(Time.deltaTime);
            if (m_BossSpawned && (m_Boss == null || !m_Boss.IsAlive))
                EndWave();
            else if (TimeRemaining <= 0f && !m_ReinforcementsEndAnnounced)
            {
                m_ReinforcementsEndAnnounced = true;
                Announce("Plus de renforts :\nabats le boss !", new Color(1f, 0.45f, 0.3f), null);
            }
        }

        void UpdateBoss(float deltaTime)
        {
            if (m_BossSpawned)
                return;

            m_BossTimer -= deltaTime;
            if (m_BossTimer > 0f)
                return;

            m_BossSpawned = true;
            m_Boss = m_Spawner != null ? m_Spawner.Spawn(m_Settings.bossPrefab) : null;
            if (m_Boss == null)
            {
                // Sans boss, la vague redevient une vague normale.
                Debug.LogWarning("WaveManager : le boss n'a pas pu apparaître.", this);
                m_IsBossWave = false;
                return;
            }

            var bossName = m_Settings.bossPrefab.name;
            if (m_Boss.TryGetComponent<Boss>(out var boss))
            {
                boss.BossNumber = Mathf.Max(1, WaveNumber / Mathf.Max(1, m_Settings.bossEvery));
                bossName = boss.DisplayName;
            }

            Announce($"{bossName}\narrive !", new Color(1f, 0.35f, 0.25f), null);
        }

        void BuildSpawnQueue()
        {
            m_SpawnQueue.Clear();
            m_Candidates.Clear();
            foreach (var enemy in m_Settings.enemies)
            {
                if (enemy != null && enemy.prefab != null && enemy.cost > 0f && enemy.weight > 0f && enemy.firstWave <= WaveNumber)
                    m_Candidates.Add(enemy);
            }

            if (m_Candidates.Count == 0)
            {
                Debug.LogWarning($"WaveManager : aucun ennemi disponible pour la vague {WaveNumber} (liste « Enemies » des Wave Settings).", this);
                return;
            }

            // On « achète » des ennemis au hasard tant que le budget le permet.
            var budget = m_Settings.BudgetFor(WaveNumber) * DifficultyManager.Current.enemyCount * m_CountScale;
            for (var guard = 0; guard < 500; guard++)
            {
                var pick = PickAffordable(budget);
                if (pick == null)
                    break;
                m_SpawnQueue.Add(pick.prefab);
                budget -= pick.cost;
            }
        }

        WaveEnemy PickAffordable(float budget)
        {
            var totalWeight = 0f;
            foreach (var candidate in m_Candidates)
            {
                if (candidate.cost <= budget)
                    totalWeight += candidate.weight;
            }

            if (totalWeight <= 0f)
                return null;

            var roll = UnityEngine.Random.value * totalWeight;
            foreach (var candidate in m_Candidates)
            {
                if (candidate.cost > budget)
                    continue;
                roll -= candidate.weight;
                if (roll <= 0f)
                    return candidate;
            }

            return null;
        }

        void UpdateSpawning(float deltaTime)
        {
            if (m_SpawnQueue.Count == 0 || m_Spawner == null)
                return;

            m_SpawnTimer -= deltaTime;
            if (m_SpawnTimer > 0f)
                return;

            if (Enemy.Alive.Count >= MaxAlive)
            {
                m_SpawnTimer = 0.5f;
                return;
            }

            var last = m_SpawnQueue.Count - 1;
            var prefab = m_SpawnQueue[last];
            m_SpawnQueue.RemoveAt(last);
            m_Spawner.Spawn(prefab);
            m_SpawnTimer = m_SpawnInterval * UnityEngine.Random.Range(0.7f, 1.3f);
        }

        void UpdateCountdown()
        {
            if (m_NextCountdownSecond <= 0 || TimeRemaining > m_NextCountdownSecond)
                return;

            Sfx.Play(m_CountdownClip, AnnouncePosition(), 0.6f, 1f, 0f);
            m_NextCountdownSecond--;
        }

        void EndWave()
        {
            Phase = WavePhase.Intermission;
            m_SpawnQueue.Clear();
            MakeSurvivorsFlee();

            // Les PV du joueur reviennent entre les vagues, pas ceux de la tour.
            if (m_PlayerHealth != null && m_PlayerHealth.IsAlive)
                m_PlayerHealth.Heal(m_PlayerHealth.Max);

            var score = ScoreManager.Instance;
            var bonus = score != null ? score.AddPoints(m_Settings.clearBonusPerWave * WaveNumber * score.DifficultyMultiplier) : 0;
            var bossLine = m_IsBossWave ? "Boss vaincu !\n" : "";
            m_IsBossWave = false;

            if (WaveNumber == WavesToWin)
                Announce($"{bossLine}Victoire !\n+{bonus}\nLe mode infini commence", new Color(1f, 0.85f, 0.3f), m_VictoryClip);
            else
                Announce($"{bossLine}Vague {WaveNumber} terminée\n+{bonus}", new Color(0.55f, 1f, 0.55f), m_WaveClearClip);

            WaveEnded?.Invoke(WaveNumber);
            m_OnWaveEnded.Invoke();
        }

        void OnPlayerDied(Health health, DamageInfo info)
        {
            if (Phase == WavePhase.GameOver)
                return;

            Phase = WavePhase.GameOver;
            m_SpawnQueue.Clear();
            MakeSurvivorsFlee();
            var score = ScoreManager.Instance;
            var summary = score != null ? $"Score : {score.Score}\n" : "";
            Announce($"Fin de partie\n{summary}Vague {WaveNumber}", new Color(1f, 0.4f, 0.3f), m_GameOverClip, 0.8f);

            GameOver?.Invoke();
            m_OnGameOver.Invoke();
        }

        // Les ennemis encore en vie s'enfuient vers le point d'apparition le plus proche.
        void MakeSurvivorsFlee()
        {
            m_Fleeing.Clear();
            m_Fleeing.AddRange(Enemy.Alive);
            foreach (var enemy in m_Fleeing)
            {
                if (enemy != null)
                    enemy.Flee(m_Spawner != null ? m_Spawner.NearestSpawnPoint(enemy.transform.position) : enemy.transform.position);
            }
        }

        // Message affiché devant le joueur.
        void Announce(string text, Color color, AudioClip clip, float heightOffset = 0.35f)
        {
            FloatingText.Spawn(AnnouncePosition() + Vector3.up * heightOffset, text, color, 2f, 2.8f);
            Sfx.Play(clip, AnnouncePosition(), 1f, 1f, 0f);
        }

        Vector3 AnnouncePosition()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            return head != null ? head.position + rig.HeadYaw * Vector3.forward * 3f : transform.position;
        }

        void UpdateDebugKeys()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Raccourcis de test au clavier : N lance la vague suivante, ou termine la vague en cours
            // (dans une vague de boss : d'abord la fin des renforts, puis la mort du boss) ;
            // K termine la partie (le joueur meurt).
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.nKey.wasPressedThisFrame)
            {
                if (Phase != WavePhase.Wave)
                    StartNextWave();
                else if (TimeRemaining > 0f)
                    TimeRemaining = 0f;
                else if (m_Boss != null && m_Boss.IsAlive)
                    m_Boss.Health.TakeDamage(new DamageInfo { Amount = m_Boss.Health.Max * 10f, Point = m_Boss.transform.position, Source = this });
            }

            if (keyboard.kKey.wasPressedThisFrame && m_PlayerHealth != null && m_PlayerHealth.IsAlive)
            {
                m_PlayerHealth.TakeDamage(new DamageInfo
                {
                    Amount = m_PlayerHealth.Max * 10f,
                    Point = transform.position,
                    Direction = Vector3.down,
                    Source = this,
                });
            }
#endif
        }
    }
}
