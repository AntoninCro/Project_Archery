using System;
using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Defense;
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

        /// <summary>Une vague est en cours.</summary>
        Wave,

        /// <summary>Pause entre deux vagues (boutique, entraînement) : on attend le gong.</summary>
        Intermission,

        /// <summary>La partie est finie (mort du joueur, ou fin choisie après la victoire).</summary>
        GameOver,
    }

    /// <summary>Les trois temps d'une vague (GDD, section 3 bis).</summary>
    public enum WaveStage
    {
        /// <summary>Début du chrono : peu d'ennemis, le temps de récolter, construire et ouvrir les coffres.</summary>
        Calm,

        /// <summary>Fin du chrono : les ennemis arrivent de plus en plus vite.</summary>
        Rise,

        /// <summary>Le chrono est fini : le boss est là, les renforts accélèrent jusqu'à sa mort.</summary>
        Boss,

        /// <summary>
        /// Vague du boss final (GDD, section 23) : pas de chrono ni d'ennemis sur la carte ; la porte de l'antre est ouverte
        /// et le combat a lieu dans l'arène (<see cref="FinalBoss.FinalBossDirector"/>).
        /// </summary>
        FinalBoss,
    }

    /// <summary>
    /// Enchaîne les vagues (GDD, sections 3 et 3 bis). Chaque vague a un chrono en deux temps, l'accalmie puis la montée,
    /// et se termine par un boss : la vague s'arrête à sa mort. Tirer de nouveau dans le gong pendant le chrono fait venir
    /// le boss plus tôt, contre une récompense plus grosse. En fin de vague : survivants en fuite, PV du joueur restaurés,
    /// points, et or de fin de vague avec ses bonus de performance. Victoire après le boss de la dernière vague ; le joueur
    /// peut alors finir la partie ou continuer en mode infini.
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

        [Tooltip("Joué à chacune des 5 dernières secondes du chrono, avant le boss.")]
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

        // Couleurs des messages.
        static readonly Color k_Gold = new Color(1f, 0.85f, 0.3f);
        static readonly Color k_Danger = new Color(1f, 0.4f, 0.3f);
        static readonly Color k_Calm = new Color(0.6f, 0.9f, 1f);

        readonly List<Enemy> m_CalmQueue = new List<Enemy>();
        readonly List<Enemy> m_RiseQueue = new List<Enemy>();
        readonly List<WaveEnemy> m_Candidates = new List<WaveEnemy>();
        readonly List<Enemy> m_Fleeing = new List<Enemy>();
        readonly List<Enemy> m_Bosses = new List<Enemy>();
        int m_CalmTotal;
        int m_CalmSpawned;
        int m_RiseTotal;
        int m_RiseSpawned;
        float m_SpawnTimer;
        int m_NextCountdownSecond;
        Health m_PlayerHealth;
        float m_CountScale = 1f;
        float m_BossArrivalTime;
        float m_BossKillTime;
        float m_EarlyCallShare;
        float m_TowerHealthAtStart;
        bool m_TowerHitThisWave;
        int m_BestCombo;
        float m_GongConfirmUntil = -1f;
        float m_GongFirstHitTime;
        float m_GongReadyTime;

        public static WaveManager Instance { get; private set; }

        /// <summary>Le message épinglé de la vague du boss final (« la porte de l'antre s'ouvre »).</summary>
        public const string FinalBossMessageId = "boss-final";

        public WavePhase Phase { get; private set; } = WavePhase.Ready;

        /// <summary>Temps de la vague en cours : accalmie, montée ou boss.</summary>
        public WaveStage Stage { get; private set; } = WaveStage.Calm;

        /// <summary>Numéro de la vague en cours, ou de la dernière jouée (0 avant la première).</summary>
        public int WaveNumber { get; private set; }

        /// <summary>Temps restant avant le boss (0 pendant le combat contre le boss).</summary>
        public float TimeRemaining { get; private set; }

        /// <summary>Durée totale du chrono de la vague en cours (accalmie et montée).</summary>
        public float WaveDuration { get; private set; }

        public int WavesToWin => m_Settings != null ? m_Settings.wavesToWin : 0;
        public bool IsEndless => WaveNumber > WavesToWin;
        public bool CanStartWave => Phase == WavePhase.Ready || Phase == WavePhase.Intermission;

        /// <summary>Le boss de la dernière vague est vaincu : la partie est gagnée (on peut continuer en mode infini).</summary>
        public bool HasWon { get; private set; }

        /// <summary>Pendant le chrono, le gong peut faire venir le boss plus tôt.</summary>
        public bool CanCallBoss => Phase == WavePhase.Wave && Stage != WaveStage.Boss && Stage != WaveStage.FinalBoss &&
                                   m_Settings != null && m_Settings.HasBosses;

        /// <summary>La vague du boss final est en cours : porte de l'antre ouverte, puis combat dans l'arène.</summary>
        public bool IsFinalBossWave => Phase == WavePhase.Wave && Stage == WaveStage.FinalBoss;

        /// <summary>Le combat contre le boss de la vague est en cours.</summary>
        public bool IsBossWave => Phase == WavePhase.Wave && Stage == WaveStage.Boss;

        /// <summary>Le chrono est fini et le boss est en vie (nom gardé pour l'interface).</summary>
        public bool IsWaitingForBoss => IsBossWave;

        /// <summary>Durée (s) de l'accalmie de la vague en cours.</summary>
        public float CalmDuration { get; private set; }

        // La difficulté et la courbe des vagues augmentent aussi le maximum d'ennemis en même temps (moins vite que leur nombre).
        int MaxAlive => Mathf.Clamp(Mathf.RoundToInt(m_Settings.maxAlive * DifficultyManager.Current.enemyCount * Mathf.Sqrt(m_CountScale)),
                                    1, Mathf.Max(1, m_Settings.maxAliveCap));

        public event Action<int> WaveStarted;
        public event Action<int> WaveEnded;
        public event Action GameOver;

        /// <summary>La vague passe de l'accalmie à la montée, ou de la montée au boss.</summary>
        public event Action<WaveStage> StageChanged;

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

            var difficulty = DifficultyManager.Current;
            WaveNumber++;
            Phase = WavePhase.Wave;
            Stage = WaveStage.Calm;
            // Le chrono : l'accalmie, puis la montée, raccourcie (Rise Duration Scale) ; ses ennemis arrivent tous, plus serrés.
            var plannedDuration = m_Settings.DurationFor(WaveNumber) * difficulty.waveDuration;
            CalmDuration = plannedDuration * m_Settings.calmFraction;
            WaveDuration = CalmDuration + plannedDuration * (1f - m_Settings.calmFraction) * m_Settings.riseDurationScale;
            TimeRemaining = WaveDuration;
            m_NextCountdownSecond = 5;

            // Courbe de difficulté exponentielle, plus raide en mode infini.
            // Selon la difficulté, PV et dégâts sont réduits au début de la partie (vague 1), puis rejoignent la courbe.
            // Par-dessus, la courbe vague par vague des Wave Settings (PV et nombre d'ennemis), selon la difficulté.
            var early = difficulty.EarlyStrength(WaveNumber);
            var waveHealth = Mathf.Lerp(1f, m_Settings.HealthCurve(WaveNumber), difficulty.waveCurve);
            var waveCount = Mathf.Lerp(1f, m_Settings.CountCurve(WaveNumber), difficulty.waveCurve);
            EnemyScaling.Health = m_Settings.Growth(WaveNumber, difficulty.healthGrowthPerWave, m_Settings.endlessHealthGrowth) * early * waveHealth;
            EnemyScaling.Damage = m_Settings.Growth(WaveNumber, difficulty.damageGrowthPerWave, m_Settings.endlessDamageGrowth) * early;
            EnemyScaling.Speed = Mathf.Min(Mathf.Max(1f, m_Settings.maxSpeedScale),
                                           m_Settings.Growth(WaveNumber, difficulty.speedGrowthPerWave, m_Settings.endlessSpeedGrowth));
            m_CountScale = m_Settings.Growth(WaveNumber, difficulty.countGrowthPerWave, m_Settings.endlessCountGrowth) * waveCount;

            m_Bosses.Clear();
            m_BossArrivalTime = -1f;
            m_BossKillTime = -1f;
            m_EarlyCallShare = 0f;
            m_GongConfirmUntil = -1f;
            m_GongReadyTime = Time.time + 1.5f;   // les flèches de la volée qui a lancé la vague ne comptent pas
            m_BestCombo = 0;
            var tower = Tower.Instance;
            m_TowerHealthAtStart = tower != null ? tower.Health.Current : 0f;
            m_TowerHitThisWave = false;

            // Boss final : ni chrono ni ennemis sur la carte, la porte de l'antre s'ouvre (Final Boss Director).
            if (m_Settings.IsFinalBossWave(WaveNumber))
            {
                Stage = WaveStage.FinalBoss;
                WaveDuration = 0f;
                CalmDuration = 0f;
                TimeRemaining = 0f;
                m_CalmQueue.Clear();
                m_RiseQueue.Clear();

                // Le message reste devant le joueur tant qu'il n'est pas entré dans l'arène (Final Boss Director).
                HeadMessages.Pin(FinalBossMessageId,
                                 $"Vague {WaveNumber} / {WavesToWin}\nBoss final\n<size=60%>La porte de l'antre s'ouvre,\nau bout de la voie du milieu</size>",
                                 new Color(1f, 0.35f, 0.25f), 1.15f);
                Sfx.Play(m_WaveStartClip, AnnouncePosition(), 1f, 1f, 0f);
            }
            else
            {
                BuildSpawnQueues();
                m_SpawnTimer = 1f;

                var title = IsEndless ? $"Vague {WaveNumber}\nmode infini" : $"Vague {WaveNumber} / {WavesToWin}";
                if (WaveNumber == 1)
                    title += "\n" + DifficultyManager.Current.displayName;
                title += "\n<size=60%>Accalmie : récolte, construis, ouvre les coffres</size>";
                Announce(title, k_Gold, m_WaveStartClip);
            }

            WaveStarted?.Invoke(WaveNumber);
            m_OnWaveStarted.Invoke();
            return true;
        }

        void Update()
        {
            UpdateDebugKeys();
            if (Phase != WavePhase.Wave)
                return;

            var deltaTime = Time.deltaTime;
            TrackPerformance();

            // Boss final : c'est l'arène qui décide de la fin de la vague (CompleteFinalBoss).
            if (Stage == WaveStage.FinalBoss)
                return;

            if (Stage != WaveStage.Boss)
            {
                TimeRemaining = Mathf.Max(0f, TimeRemaining - deltaTime);
                var elapsed = WaveDuration - TimeRemaining;
                if (Stage == WaveStage.Calm && elapsed >= CalmDuration)
                    BeginRise();

                if (Stage == WaveStage.Calm)
                    UpdateCalmSpawning(elapsed);
                else
                    UpdateRiseSpawning(elapsed - CalmDuration, WaveDuration - CalmDuration);

                UpdateCountdown();
                if (TimeRemaining <= 0f)
                    BeginBoss();
                return;
            }

            UpdateBossSpawning(deltaTime);
            if (AllBossesDefeated())
                EndWave();
        }

        // ---------------------------------------------------------------- apparitions

        void BuildSpawnQueues()
        {
            m_CalmQueue.Clear();
            m_RiseQueue.Clear();
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
                m_RiseQueue.Add(pick.prefab);
                budget -= pick.cost;
            }

            // Une petite part arrive pendant l'accalmie, le reste pendant la montée.
            var calmCount = Mathf.RoundToInt(m_RiseQueue.Count * m_Settings.CalmShareFor(WaveNumber));
            for (var i = 0; i < calmCount && m_RiseQueue.Count > 0; i++)
            {
                var last = m_RiseQueue.Count - 1;
                m_CalmQueue.Add(m_RiseQueue[last]);
                m_RiseQueue.RemoveAt(last);
            }

            // Selon la difficulté, moins d'ennemis après l'accalmie ; ceux du début de la vague ne changent pas.
            var lateCount = Mathf.RoundToInt(m_RiseQueue.Count * DifficultyManager.Current.lateEnemyCount);
            if (lateCount < m_RiseQueue.Count)
                m_RiseQueue.RemoveRange(lateCount, m_RiseQueue.Count - lateCount);

            m_CalmTotal = m_CalmQueue.Count;
            m_CalmSpawned = 0;
            m_RiseTotal = m_RiseQueue.Count;
            m_RiseSpawned = 0;
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

        // Accalmie : un filet d'ennemis, réparti régulièrement.
        void UpdateCalmSpawning(float elapsed)
        {
            if (m_CalmQueue.Count == 0 || CalmDuration <= 0f)
                return;

            var due = Mathf.FloorToInt(elapsed / CalmDuration * m_CalmTotal + 0.5f);
            if (m_CalmSpawned < due && TrySpawnFrom(m_CalmQueue))
                m_CalmSpawned++;
        }

        // Montée : de plus en plus d'arrivées (le k-ième ennemi sort à √(k/N) de la montée).
        void UpdateRiseSpawning(float elapsed, float duration)
        {
            if (m_RiseQueue.Count == 0 || duration <= 0f)
                return;

            var progress = Mathf.Clamp01(elapsed / duration);
            var due = Mathf.CeilToInt(progress * progress * m_RiseTotal);
            if (m_RiseSpawned < due && TrySpawnFrom(m_RiseQueue))
                m_RiseSpawned++;
        }

        // Boss : les ennemis prévus qui restent sortent vite, puis des renforts arrivent régulièrement jusqu'à sa mort.
        void UpdateBossSpawning(float deltaTime)
        {
            m_SpawnTimer -= deltaTime;
            if (m_SpawnTimer > 0f)
                return;

            if (m_CalmQueue.Count > 0 || m_RiseQueue.Count > 0)
            {
                if (!TrySpawnFrom(m_CalmQueue.Count > 0 ? m_CalmQueue : m_RiseQueue))
                    m_SpawnTimer = 0.5f;
                else
                    m_SpawnTimer = 0.6f;
                return;
            }

            var difficulty = DifficultyManager.Current;
            var interval = m_Settings.bossReinforcementInterval /
                           Mathf.Max(0.1f, difficulty.enemyCount * difficulty.lateEnemyCount * Mathf.Sqrt(m_CountScale));
            m_SpawnTimer = interval * UnityEngine.Random.Range(0.8f, 1.2f);
            var pick = PickAffordable(float.MaxValue);
            if (pick != null && Enemy.Alive.Count < MaxAlive && m_Spawner != null)
                m_Spawner.Spawn(pick.prefab);
        }

        bool TrySpawnFrom(List<Enemy> queue)
        {
            if (queue.Count == 0 || m_Spawner == null || Enemy.Alive.Count >= MaxAlive)
                return false;

            var last = queue.Count - 1;
            var prefab = queue[last];
            queue.RemoveAt(last);
            m_Spawner.Spawn(prefab);
            return true;
        }

        // ---------------------------------------------------------------- temps de la vague

        void BeginRise()
        {
            if (Stage != WaveStage.Calm)
                return;

            Stage = WaveStage.Rise;
            Announce("Les ennemis arrivent en nombre !", k_Danger, null, false);
            StageChanged?.Invoke(Stage);
        }

        /// <summary>
        /// Fait venir le boss tout de suite (deuxième coup de gong pendant le chrono). La récompense de la vague augmente
        /// de la part du chrono qui restait. Renvoie faux si ce n'est pas possible maintenant.
        /// </summary>
        public bool CallBossEarly()
        {
            if (!CanCallBoss)
                return false;

            m_EarlyCallShare = WaveDuration > 0f ? TimeRemaining / WaveDuration : 0f;
            BeginBoss();
            return true;
        }

        /// <summary>
        /// Coup de gong pendant le chrono : le premier demande confirmation, le second (dans les 4 s) appelle le boss.
        /// Évite qu'une flèche perdue fasse venir le boss.
        /// </summary>
        public void OnGongHitDuringWave()
        {
            if (!CanCallBoss || Time.time < m_GongReadyTime)
                return;

            if (Time.time <= m_GongConfirmUntil)
            {
                // Le deuxième coup doit être un vrai deuxième tir, pas une flèche de la même volée.
                if (Time.time - m_GongFirstHitTime >= 0.5f)
                    CallBossEarly();
                return;
            }

            m_GongFirstHitTime = Time.time;
            m_GongConfirmUntil = Time.time + 4f;
            var bonus = Mathf.RoundToInt(100f * (WaveDuration > 0f ? TimeRemaining / WaveDuration : 0f));
            Announce($"Frappe encore le gong\npour appeler le boss\n<size=70%>récompense +{bonus} %</size>", k_Gold, null, false);
        }

        void BeginBoss()
        {
            if (Stage == WaveStage.Boss)
                return;

            Stage = WaveStage.Boss;
            TimeRemaining = 0f;

            // Les ennemis de l'accalmie qui n'étaient pas encore sortis arrivent avec ceux de la montée.
            m_RiseQueue.AddRange(m_CalmQueue);
            m_CalmQueue.Clear();
            m_SpawnTimer = 1.5f;

            var count = m_Settings.BossCountFor(WaveNumber);
            var bossName = "";
            for (var i = 0; i < count && m_Spawner != null; i++)
            {
                var prefab = m_Settings.BossFor(WaveNumber, i);
                var enemy = prefab != null ? m_Spawner.Spawn(prefab) : null;
                if (enemy == null)
                    continue;

                m_Bosses.Add(enemy);
                if (enemy.TryGetComponent<Boss>(out var boss))
                {
                    boss.BossNumber = Mathf.Max(1, WaveNumber);
                    bossName = boss.DisplayName;
                }
            }

            StageChanged?.Invoke(Stage);
            if (m_Bosses.Count == 0)
            {
                // Sans boss, la vague se termine avec le chrono.
                if (m_Settings.HasBosses)
                    Debug.LogWarning("WaveManager : le boss n'a pas pu apparaître.", this);
                EndWave();
                return;
            }

            m_BossArrivalTime = Time.time;
            if (string.IsNullOrEmpty(bossName))
                bossName = m_Settings.BossFor(WaveNumber).name;
            var text = m_Bosses.Count > 1 ? $"{m_Bosses.Count} boss arrivent !" : $"{bossName}\narrive !";
            Announce(text + "\n<size=60%>abats-le pour finir la vague</size>", new Color(1f, 0.35f, 0.25f), null);
        }

        bool AllBossesDefeated()
        {
            foreach (var boss in m_Bosses)
            {
                if (boss != null && boss.IsAlive)
                    return false;
            }

            return true;
        }

        void UpdateCountdown()
        {
            if (m_NextCountdownSecond <= 0 || TimeRemaining > m_NextCountdownSecond)
                return;

            Sfx.Play(m_CountdownClip, AnnouncePosition(), 0.6f, 1f, 0f);
            m_NextCountdownSecond--;
        }

        // Bonus de performance : la tour touchée pendant la vague, et le meilleur combo.
        void TrackPerformance()
        {
            var tower = Tower.Instance;
            if (tower != null && tower.Health.Current < m_TowerHealthAtStart - 0.01f)
                m_TowerHitThisWave = true;

            var score = ScoreManager.Instance;
            if (score != null && score.Combo > m_BestCombo)
                m_BestCombo = score.Combo;

            if (Stage == WaveStage.Boss && m_BossKillTime < 0f && m_BossArrivalTime >= 0f && AllBossesDefeated())
                m_BossKillTime = Time.time - m_BossArrivalTime;
        }

        // ---------------------------------------------------------------- fin de vague, fin de partie

        /// <summary>
        /// Le boss final est vaincu (GDD, section 23). Le joueur a choisi de continuer en mode infini
        /// (<paramref name="finishGame"/> faux : fin de vague normale, boutique, puis le gong lance la vague suivante)
        /// ou de terminer la partie (écran « Victoire ! », nom, classement). Renvoie faux hors de la vague du boss final.
        /// </summary>
        public bool CompleteFinalBoss(bool finishGame)
        {
            if (!IsFinalBossWave)
                return false;

            EndWave(finishGame);
            return true;
        }

        void EndWave() => EndWave(false);

        void EndWave(bool finishGame)
        {
            HeadMessages.Unpin(FinalBossMessageId);
            TrackPerformance();
            Phase = finishGame ? WavePhase.GameOver : WavePhase.Intermission;
            m_CalmQueue.Clear();
            m_RiseQueue.Clear();
            MakeSurvivorsFlee();

            // Les PV du joueur reviennent entre les vagues, pas ceux de la tour.
            if (m_PlayerHealth != null && m_PlayerHealth.IsAlive)
                m_PlayerHealth.Heal(m_PlayerHealth.Max);

            var score = ScoreManager.Instance;
            var points = score != null ? score.AddPoints(m_Settings.clearBonusPerWave * WaveNumber * score.DifficultyMultiplier) : 0;
            var gold = GiveWaveReward(score, out var bonusLine);

            // Après la récompense (divisée par deux si la tour était détruite) : la tour et les barricades se réparent.
            var repairLine = RestoreDefenses();

            var won = WaveNumber == WavesToWin && !HasWon;
            if (won)
                HasWon = true;

            var lines = $"+{points} points · +{gold} or";
            if (!string.IsNullOrEmpty(bonusLine))
                lines += $"\n<size=60%>{bonusLine}</size>";
            if (!string.IsNullOrEmpty(repairLine))
                lines += $"\n<size=60%>{repairLine}</size>";

            if (finishGame)
            {
                HasWon = true;
                Announce($"Victoire !\n{lines}", k_Gold, m_VictoryClip);
                GameOver?.Invoke();
                m_OnGameOver.Invoke();
                return;
            }

            if (won)
                Announce($"Victoire !\n{lines}\n<size=60%>Frappe le gong pour le mode infini,\nou termine la partie depuis la boutique</size>",
                         k_Gold, m_VictoryClip);
            else
                Announce($"Vague {WaveNumber} terminée\n{lines}", new Color(0.55f, 1f, 0.55f), m_WaveClearClip);

            WaveEnded?.Invoke(WaveNumber);
            m_OnWaveEnded.Invoke();
        }

        // Depuis le 25 octobre, la tour (reconstruite si elle est tombée) et les barricades retrouvent tous leurs PV à la fin
        // de chaque vague : la boutique ne vend plus de réparations. Renvoie la ligne du message de fin de vague (vide si
        // rien n'était abîmé).
        static string RestoreDefenses()
        {
            var parts = new List<string>();
            var tower = Tower.Instance;
            if (tower != null && !tower.IsStanding)
            {
                tower.Rebuild();
                parts.Add("Tour reconstruite");
            }
            else if (tower != null && tower.Health.Current < tower.Health.Max - 0.5f)
            {
                tower.Repair(tower.Health.Max - tower.Health.Current);
                parts.Add("Tour réparée");
            }

            Barricade.TotalHealth(out var current, out var max);
            if (current < max - 0.5f)
            {
                Barricade.RebuildAll();
                parts.Add(parts.Count == 0 ? "Barricades réparées" : "barricades réparées");
            }

            return string.Join(" · ", parts);
        }

        // Or de fin de vague : base × difficulté × (1 + gong + bonus de performance), divisé par deux si la tour est détruite.
        int GiveWaveReward(ScoreManager score, out string bonusLine)
        {
            bonusLine = "";
            if (score == null)
                return 0;

            var bonus = m_EarlyCallShare;
            var parts = new List<string>();
            if (m_EarlyCallShare > 0.01f)
                parts.Add($"gong +{Mathf.RoundToInt(100f * m_EarlyCallShare)} %");

            var tower = Tower.Instance;
            if (tower != null && tower.IsStanding && !m_TowerHitThisWave)
            {
                bonus += m_Settings.performanceBonus;
                parts.Add("tour intacte");
            }

            if (m_BossKillTime >= 0f && m_BossKillTime <= m_Settings.fastBossTime)
            {
                bonus += m_Settings.performanceBonus;
                parts.Add("boss express");
            }

            if (m_BestCombo >= m_Settings.comboTarget)
            {
                bonus += m_Settings.performanceBonus;
                parts.Add($"combo ×{m_BestCombo}");
            }

            if (parts.Count > 0)
                bonusLine = "Bonus : " + string.Join(" · ", parts);

            var towerFactor = tower != null && !tower.IsStanding ? 0.5f : 1f;
            var gold = Mathf.RoundToInt(m_Settings.WaveRewardFor(WaveNumber) * DifficultyManager.Current.scoreMultiplier * (1f + bonus) * towerFactor);
            score.AddMoney(gold);
            return gold;
        }

        /// <summary>Après la victoire, entre deux vagues : le joueur choisit de terminer la partie (écran de fin, classement).</summary>
        public bool FinishGame()
        {
            if (!HasWon || Phase != WavePhase.Intermission)
                return false;

            Phase = WavePhase.GameOver;
            Announce("Partie terminée", k_Gold, m_VictoryClip);
            GameOver?.Invoke();
            m_OnGameOver.Invoke();
            return true;
        }

        void OnPlayerDied(Health health, DamageInfo info)
        {
            if (Phase == WavePhase.GameOver)
                return;

            Phase = WavePhase.GameOver;
            HeadMessages.Unpin(FinalBossMessageId);
            m_CalmQueue.Clear();
            m_RiseQueue.Clear();
            MakeSurvivorsFlee();
            var score = ScoreManager.Instance;
            var summary = score != null ? $"Score : {score.Score}\n" : "";
            Announce($"Fin de partie\n{summary}Vague {WaveNumber}", k_Danger, m_GameOverClip);

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
        // Titre (début de vague, boss, victoire) ou message plus discret, dans le bandeau qui reste devant le regard.
        void Announce(string text, Color color, AudioClip clip, bool title = true)
        {
            HeadMessages.Show(text, color, title ? 5f : 4f, title ? 1.3f : 1f);
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
            // Raccourcis de test au clavier : N lance la vague suivante ; pendant le chrono, fait venir le boss ;
            // pendant le boss, le tue. K termine la partie (le joueur meurt).
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.nKey.wasPressedThisFrame)
            {
                if (Phase != WavePhase.Wave)
                    StartNextWave();
                else if (Stage == WaveStage.FinalBoss)
                {
                    // Boss final : N est géré par le Final Boss Director (entrer dans l'arène, passer l'intro, tuer le dragon).
                }
                else if (Stage != WaveStage.Boss)
                    BeginBoss();
                else
                {
                    foreach (var boss in m_Bosses)
                    {
                        if (boss != null && boss.IsAlive)
                            boss.Health.TakeDamage(new DamageInfo { Amount = boss.Health.Max * 10f, Point = boss.transform.position, Source = this });
                    }
                }
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
