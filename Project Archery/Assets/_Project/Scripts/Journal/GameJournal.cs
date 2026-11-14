#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Archery.BowClasses;
using Archery.Building;
using Archery.Combat;
using Archery.Defense;
using Archery.Difficulty;
using Archery.Economy;
using Archery.Enemies;
using Archery.FinalBoss;
using Archery.Player;
using Archery.Upgrades;
using Archery.Waves;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Archery.Journal
{
    /// <summary>
    /// Le journal de partie (3 novembre ; le joueur, avant une partie de test : « implémente tous les scripts qui te permettraient de
    /// suivre ma progression, objets, dégâts, etc., pour prendre note de la partie quelque part »). Pendant la partie, il écrit au fil
    /// de l'eau un fichier Markdown que Claude lit ensuite : <c>Project Archery/Logs/Parties/Partie_&lt;date&gt;.md</c> (hors de Git ;
    /// dans un build de développement, <c>Application.persistentDataPath/Parties</c>).
    /// <list type="bullet">
    /// <item>le <b>déroulé</b>, daté depuis le début de la partie : les vagues et leurs temps, les boss, les objets et les pièces de la
    /// flèche gagnés ou équipés, les plans, les bâtiments (posés, construits, améliorés, fusionnés, déplacés, partis dans l'arène), les
    /// attaques sur la tour, la mort du joueur, les phases de MK, les succès, les raccourcis de test, les erreurs du jeu ;</item>
    /// <item>à chaque fin de vague, son <b>bilan</b> : les ennemis (apparus, tués, par sorte), les dégâts et les éliminations par source
    /// (le joueur, chaque bâtiment), les tirs (par qualité, les flèches qui touchent, à la tête), les dégâts reçus par le joueur et la
    /// tour (par ennemi), l'or, le score, la fluidité (images par seconde), les bâtiments et la flèche ;</item>
    /// <item>à la fin, le <b>bilan de la partie</b> (les mêmes comptes sur toute la partie, les erreurs et avertissements).</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Seulement dans l'éditeur et les builds de développement, comme les raccourcis de test ; il se crée tout seul au lancement (rien
    /// à poser dans la scène) et ne fait que regarder : il écoute les événements du jeu, et compare toutes les 0,5 s les bâtiments à
    /// ceux d'avant. Un fichier par lancement du mode Jeu ; une partie recommencée (la scène rechargée) y ajoute « Partie 2 ». Il
    /// n'écrit rien tant qu'il n'y a pas de WaveManager. Le fichier est écrit ligne à ligne : il reste lisible si Unity plante.
    /// </remarks>
    [DisallowMultipleComponent]
    public partial class GameJournal : MonoBehaviour
    {
        const float k_BindInterval = 0.5f;

        // Une attaque sur la tour : des coups à moins de tant de secondes d'écart (comme le cor de la tour, Tower Attack Gap).
        const float k_AttackGap = 8f;

        static readonly CultureInfo k_French = CultureInfo.GetCultureInfo("fr-FR");

        // Les raccourcis de test (Docs/Demo_Vendredi.md) : notés dans le déroulé, pour savoir ce qui vient d'eux.
        static readonly (Key Key, string Text)[] k_Shortcuts =
        {
            (Key.N, "N (vague suivante, boss, ou le tuer)"), (Key.K, "K (le joueur meurt)"),
            (Key.B, "B (2 plans de chaque)"), (Key.H, "H (construction finie)"), (Key.C, "C (un coffre)"), (Key.X, "X (un coffre de boss)"),
            (Key.Tab, "Tab (toutes les pièces un niveau de plus)"), (Key.R, "R (la pièce nouvelle suivante)"), (Key.V, "V (un coffre)"),
            (Key.F, "F (tout au niveau 3)"), (Key.F2, "F2 (le menu des plans)"), (Key.F3, "F3 (les ennemis projetés)"),
            (Key.F4, "F4 (la vitrine des bâtiments)"), (Key.P, "P (les runes, la dimension)"), (Key.G, "G (les éclats, le toit cyberpunk)"),
        };

        // Le fichier de ce lancement du mode Jeu, et le nombre de parties qu'il raconte.
        static string s_Path;
        static int s_Games;
        static readonly List<UnityEngine.XR.XRDisplaySubsystem> s_Displays = new List<UnityEngine.XR.XRDisplaySubsystem>();

        StreamWriter m_Writer;
        bool m_WriteFailed;
        readonly List<string> m_Pending = new List<string>();

        // La partie en cours : sa section est ouverte (dès qu'il y a un WaveManager), son bilan écrit.
        bool m_GameOpen;
        bool m_GameClosed;
        float m_GameStart;
        float m_NextBind;

        WaveManager m_Waves;
        PlayerHealth m_Player;
        Health m_PlayerHealth;
        Tower m_Tower;
        Health m_TowerHealth;
        BuildManager m_Build;
        PlayerUpgrades m_Upgrades;
        ScoreManager m_Score;
        Archery.Shop.ShopManager m_Shop;

        // Les cartes de la pause, quand elle s'ouvre avant que le journal ait fini la vague (l'ordre des abonnés à la fin de vague).
        string m_PendingPause;
        DragonBoss m_Dragon;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Path = null;
            s_Games = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("[Journal de partie]");
            DontDestroyOnLoad(go);
            go.AddComponent<GameJournal>();
        }

        void Awake() => StartGame();

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Application.logMessageReceived += OnLog;
            Enemy.Damaged += OnEnemyDamaged;
            Enemy.Killed += OnEnemyKilled;
            EnemySpawner.Spawned += OnEnemySpawned;
            Archery.Bows.Bow.ShotFired += OnShotFired;
            Archery.Bows.Arrow.AnyHit += OnArrowHit;
            Archery.Bows.Arrow.ShotEnded += OnShotEnded;
            CrystalSpawner.GuardsCameOut += OnGuardsCameOut;
            Boss.Appeared += OnBossAppeared;
            Boss.Defeated += OnBossDefeated;
            AchievementManager.Unlocked += OnAchievement;
            DifficultyManager.Changed += OnDifficultyChanged;
            BowLoadout.Changed += OnBowChanged;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Application.logMessageReceived -= OnLog;
            Enemy.Damaged -= OnEnemyDamaged;
            Enemy.Killed -= OnEnemyKilled;
            EnemySpawner.Spawned -= OnEnemySpawned;
            Archery.Bows.Bow.ShotFired -= OnShotFired;
            Archery.Bows.Arrow.AnyHit -= OnArrowHit;
            Archery.Bows.Arrow.ShotEnded -= OnShotEnded;
            CrystalSpawner.GuardsCameOut -= OnGuardsCameOut;
            Boss.Appeared -= OnBossAppeared;
            Boss.Defeated -= OnBossDefeated;
            AchievementManager.Unlocked -= OnAchievement;
            DifficultyManager.Changed -= OnDifficultyChanged;
            BowLoadout.Changed -= OnBowChanged;
        }

        // Le mode Jeu s'arrête (ou le build se ferme) : le bilan de la partie en cours, s'il n'est pas écrit.
        void OnDestroy()
        {
            CloseGame("partie interrompue (fin du mode Jeu)");
            m_Writer?.Dispose();
            m_Writer = null;
        }

        // La scène recommence (une nouvelle partie, après une mort ou depuis l'écran de fin) : le bilan de l'ancienne, puis tout se
        // rebranche sur la nouvelle.
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single)
                return;
            CloseGame("la scène a été rechargée (nouvelle partie)");
            StartGame();
        }

        void StartGame()
        {
            m_GameOpen = false;
            m_GameClosed = false;
            m_GameStart = Time.unscaledTime;
            m_Waves = null;
            m_Player = null;
            m_PlayerHealth = null;
            m_Tower = null;
            m_TowerHealth = null;
            m_Build = null;
            m_Upgrades = null;
            m_Score = null;
            m_Shop = null;
            m_PendingPause = null;
            m_Dragon = null;
            m_Pending.Clear();
            ResetCounts();
            ResetWatch();
        }

        void Update()
        {
            if (Time.unscaledTime >= m_NextBind)
            {
                m_NextBind = Time.unscaledTime + k_BindInterval;
                Bind();
                if (Active)
                    Watch();
            }

            if (!Active)
                return;
            CountFrame(Time.unscaledDeltaTime);
            ReadShortcuts();
        }

        /// <summary>La partie en cours est racontée (elle a sa section, et son bilan n'est pas encore écrit).</summary>
        bool Active => m_GameOpen && !m_GameClosed;

        // ---------------------------------------------------------------- se brancher sur le jeu

        // Les objets du jeu, au fur et à mesure qu'ils apparaissent (et ceux de la nouvelle scène, après un rechargement).
        void Bind()
        {
            if (m_GameClosed)
                return;

            if (WaveManager.Instance != null && WaveManager.Instance != m_Waves)
            {
                m_Waves = WaveManager.Instance;
                m_Waves.WaveStarted += OnWaveStarted;
                m_Waves.WaveEnded += OnWaveEnded;
                m_Waves.GameOver += OnGameOver;
                m_Waves.StageChanged += OnStageChanged;
            }

            if (m_Waves == null)
                return;

            if (ScoreManager.Instance != null && ScoreManager.Instance != m_Score)
                m_Score = ScoreManager.Instance;

            // Le choix de la pause (8 novembre) : ce qui est proposé, et ce que le joueur prend.
            if (Archery.Shop.ShopManager.Instance != null && Archery.Shop.ShopManager.Instance != m_Shop)
            {
                m_Shop = Archery.Shop.ShopManager.Instance;
                m_Shop.Opened += OnShopOpened;
                m_Shop.Taken += OnShopTaken;
            }

            if (PlayerHealth.Instance != null && PlayerHealth.Instance != m_Player && PlayerHealth.Instance.Health != null)
            {
                m_Player = PlayerHealth.Instance;
                m_PlayerHealth = m_Player.Health;
                m_PlayerHealth.Damaged += OnPlayerDamaged;
                m_PlayerHealth.Died += OnPlayerDied;
            }

            if (Tower.Instance != null && Tower.Instance != m_Tower && Tower.Instance.Health != null)
            {
                m_Tower = Tower.Instance;
                m_TowerHealth = m_Tower.Health;
                m_TowerHealth.Damaged += OnTowerDamaged;
                m_Tower.Destroyed += OnTowerDestroyed;
            }

            if (BuildManager.Instance != null && BuildManager.Instance != m_Build)
            {
                m_Build = BuildManager.Instance;
                m_Build.PlansChanged += OnPlansChanged;
                SnapshotPlans();
            }

            if (PlayerUpgrades.Instance != null && PlayerUpgrades.Instance != m_Upgrades)
            {
                m_Upgrades = PlayerUpgrades.Instance;
                m_Upgrades.Changed += OnUpgradesChanged;
                SnapshotUpgrades();
            }

            if (DragonBoss.Instance != null && DragonBoss.Instance != m_Dragon)
            {
                m_Dragon = DragonBoss.Instance;
                m_Dragon.PhaseChanged += OnDragonPhase;
                Line("MK est là (l'arène est chargée) : PV " + HealthText(DragonHealth));
            }

            if (!m_GameOpen)
                OpenGame();
        }

        // La section de la partie, avec ce qu'on a au départ.
        void OpenGame()
        {
            m_GameOpen = true;
            s_Games++;
            Write("");
            Write($"## Partie {s_Games}, commencée à {DateTime.Now:HH:mm:ss}");
            Write("");
            Write("### Déroulé");
            Write("");
            Line($"Début : difficulté {DifficultyName}, arc {BowName} ; {Status()}.");
            var items = OwnedText();
            if (items.Length > 0)
                Line("Objets au départ : " + items);
            if (m_Build != null && m_Build.TotalPlans > 0)
                Line($"Plans au départ : {PlansText()}");
            foreach (var pending in m_Pending)
                Write(pending);
            m_Pending.Clear();
        }

        // ---------------------------------------------------------------- les vagues et la fin

        void OnWaveStarted(int wave)
        {
            if (!Active)
                return;
            if (m_Wave != null)
                WriteWaveSummary(m_Wave, "interrompue");
            m_Wave = NewCounts(wave);
            m_LastChests = -1;
            Line($"**Vague {wave}** : elle commence ({Status()}, {BuiltCount()} bâtiments ; {PlayMode()}).");
        }

        void OnWaveEnded(int wave)
        {
            if (!Active)
                return;
            Line($"**Vague {wave} terminée** ({Status()}).");
            m_LastChests = -1;
            if (m_Wave != null)
                WriteWaveSummary(m_Wave, "terminée");
            m_Wave = null;
            if (m_PendingPause != null)
            {
                Line(m_PendingPause);
                m_PendingPause = null;
            }
        }

        // La pause s'ouvre (8 novembre, le choix de la pause) : les cartes de chaque ligne.
        void OnShopOpened(int wave)
        {
            if (!Active || m_Shop == null)
                return;

            var rows = new List<string>[Archery.Shop.ShopManager.Rows];
            for (var i = 0; i < rows.Length; i++)
                rows[i] = new List<string>();
            foreach (var offer in m_Shop.UpgradeOffers)
            {
                // La carte que le gong a rendue plus rare le dit (12 novembre, ShopManager.MarkBoosted).
                if (offer != null && offer.Row >= 0 && offer.Row < rows.Length && offer.Kind != Archery.Shop.ShopOfferKind.Unavailable)
                    rows[offer.Row].Add(OfferName(offer) + (offer.PriceText != null && offer.PriceText.EndsWith("gong") ? " [gong]" : ""));
            }

            string Row(int i, string name) => $"{name} : {(rows[i].Count > 0 ? string.Join(", ", rows[i]) : "rien")}";
            var text = $"Pause après la vague {wave}, proposés : {Row(0, "parchemins")} ; {Row(1, "objets")} ; {Row(2, "tourelles")}.";
            if (m_Wave != null)
                m_PendingPause = text;
            else
                Line(text);
        }

        void OnShopTaken(Archery.Shop.ShopOffer offer)
        {
            if (!Active || offer == null)
                return;
            var kind = offer.Plans.HasValue ? "tourelle" : offer.Upgrade != null && offer.Upgrade.kind == UpgradeKind.Scroll ? "parchemin" : "objet";
            Line($"Pause : pris **{OfferName(offer)}** ({kind}).");
        }

        // « Force », « Multitir (rare) », « Phare, 3 plans ».
        static string OfferName(Archery.Shop.ShopOffer offer)
        {
            if (offer.Plans.HasValue)
                return $"{offer.Plans.Value.Title}, {offer.Plans.Value.Count} plan{(offer.Plans.Value.Count > 1 ? "s" : "")}";
            if (offer.Upgrade != null && offer.Upgrade.kind != UpgradeKind.Scroll)
                return $"{offer.Title} ({Archery.Shop.ShopCatalog.NameOf(offer.Upgrade.rarity).ToLowerInvariant()})";
            // Un parchemin : sa rareté (8 novembre au soir), lue sur son échelle (×1 commune … ×3 légendaire).
            var scale = offer.ScrollScale;
            var rarity = scale >= 3f ? UpgradeRarity.Legendary : scale >= 2f ? UpgradeRarity.Epic : scale >= 1.5f ? UpgradeRarity.Rare : UpgradeRarity.Common;
            return $"{offer.Title} ({Archery.Shop.ShopCatalog.NameOf(rarity).ToLowerInvariant()})";
        }

        void OnStageChanged(WaveStage stage)
        {
            if (!Active || m_Waves == null)
                return;
            switch (stage)
            {
                case WaveStage.Rise:
                    Line($"Vague {m_Waves.WaveNumber} : la montée commence.");
                    break;
                case WaveStage.Boss:
                    // Le boss appelé au gong (12 novembre : le journal écrivait toujours « le chrono est fini », et le joueur croyait
                    // que ses deux coups de gong n'avaient pas compté).
                    Line(m_Waves.BossCalledEarly
                        ? $"Vague {m_Waves.WaveNumber} : **le boss est appelé au gong** (la pause qui suit : une carte plus rare par ligne)."
                        : $"Vague {m_Waves.WaveNumber} : le chrono est fini, le boss arrive.");
                    break;
                case WaveStage.FinalBoss:
                    Line("La vague du boss final : l'antre de MK est ouvert.");
                    break;
            }
        }

        void OnGameOver()
        {
            if (!Active)
                return;
            var won = m_Waves != null && m_Waves.HasWon;
            Line(won ? "**Fin de la partie : victoire.**" : "**Fin de la partie : défaite.**");
            if (m_Wave != null)
                WriteWaveSummary(m_Wave, won ? "la partie est gagnée" : "la partie est perdue");
            m_Wave = null;
            CloseGame(won ? "victoire" : "défaite");
        }

        void OnDragonPhase(DragonPhase phase)
        {
            if (Active)
                Line($"MK : phase « {phase} » (PV {HealthText(DragonHealth)}).");
        }

        Health DragonHealth => m_Dragon != null && m_Dragon.TryGetComponent<Enemy>(out var enemy) ? enemy.Health : null;

        void OnBossAppeared(Boss boss)
        {
            if (!Active || boss == null)
                return;
            m_BossSince[boss] = Time.unscaledTime;
            Line($"Boss : **{boss.DisplayName}** apparaît (PV {HealthText(boss.Health)}).");
        }

        void OnBossDefeated(Boss boss)
        {
            if (!Active || boss == null)
                return;
            var since = m_BossSince.TryGetValue(boss, out var start) ? start : Time.unscaledTime;
            m_BossSince.Remove(boss);
            Line($"Boss vaincu : **{boss.DisplayName}** en {Duration(Time.unscaledTime - since)}.");
        }

        readonly Dictionary<Boss, float> m_BossSince = new Dictionary<Boss, float>();

        void OnAchievement(AchievementDefinition achievement)
        {
            if (Active && achievement != null)
                Line($"Succès : {achievement.title}.");
        }

        void OnDifficultyChanged(DifficultyDefinition difficulty)
        {
            if (Active)
                Line($"Difficulté : {DifficultyName}.");
        }

        void OnBowChanged(Archery.Bows.BowDefinition bow)
        {
            if (Active)
                Line($"Arc : {BowName}.");
        }

        void ReadShortcuts()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || DesktopHand.IsTypingText)
                return;
            foreach (var shortcut in k_Shortcuts)
            {
                if (keyboard[shortcut.Key].wasPressedThisFrame)
                    Line($"Raccourci {shortcut.Text}.");
            }
        }

        // ---------------------------------------------------------------- les erreurs du jeu

        readonly Dictionary<string, int> m_Errors = new Dictionary<string, int>();
        readonly Dictionary<string, int> m_Warnings = new Dictionary<string, int>();
        bool m_InLog;

        // Une erreur ou une exception : notée au déroulé la première fois (avec le haut de sa pile), comptée ensuite. Les avertissements
        // sont seulement comptés, pour le bilan.
        void OnLog(string message, string stack, LogType type)
        {
            if (m_InLog || !m_GameOpen)
                return;
            m_InLog = true;
            try
            {
                var key = message.Length > 300 ? message.Substring(0, 300) : message;
                if (type == LogType.Warning)
                {
                    m_Warnings[key] = m_Warnings.TryGetValue(key, out var seen) ? seen + 1 : 1;
                    return;
                }

                if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                    return;
                if (m_Errors.TryGetValue(key, out var count))
                {
                    m_Errors[key] = count + 1;
                    return;
                }

                m_Errors[key] = 1;
                var where = string.IsNullOrEmpty(stack) ? "" : stack.Split('\n')[0].Trim();
                Line($"⚠ {(type == LogType.Exception ? "Exception" : "Erreur")} : {OneLine(key)}{(where.Length > 0 ? $" (`{where}`)" : "")}");
            }
            finally
            {
                m_InLog = false;
            }
        }

        // ---------------------------------------------------------------- écrire

        /// <summary>Une ligne du déroulé, datée depuis le début de la partie.</summary>
        void Line(string text)
        {
            var line = $"- {Clock()} {text}";
            if (!m_GameOpen)
            {
                if (m_Pending.Count < 50)
                    m_Pending.Add(line);
                return;
            }

            Write(line);
        }

        void Write(string text)
        {
            if (m_WriteFailed)
                return;
            try
            {
                if (m_Writer == null)
                    OpenFile();
                m_Writer.WriteLine(text);
            }
            catch (Exception)
            {
                // Le journal ne doit jamais gêner la partie : sans fichier, il se tait.
                m_WriteFailed = true;
            }
        }

        void OpenFile()
        {
            var appending = s_Path != null;
            if (!appending)
            {
#if UNITY_EDITOR
                var folder = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Logs", "Parties");
#else
                var folder = Path.Combine(Application.persistentDataPath, "Parties");
#endif
                Directory.CreateDirectory(folder);
                s_Path = Path.Combine(folder, $"Partie_{DateTime.Now:yyyy-MM-dd_HH'h'mm'm'ss}.md");
            }

            m_Writer = new StreamWriter(s_Path, true, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\r\n" };
            if (appending)
                return;

            m_Writer.WriteLine($"# Journal de partie, {DateTime.Now.ToString("dddd d MMMM yyyy, HH:mm", k_French)}");
            m_Writer.WriteLine("");
            m_Writer.WriteLine("Écrit par `Scripts/Journal/GameJournal.cs` (le journal des parties de test, 3 novembre), lu par Claude après la " +
                               "partie. Les heures du déroulé partent du début de la partie (minutes:secondes).");
            m_Writer.WriteLine("");
            m_Writer.WriteLine($"Unity {Application.unityVersion}, {(Application.isEditor ? "éditeur" : "build de développement")}, " +
                               $"scène {SceneManager.GetActiveScene().name}. Au casque ou au clavier : au début de chaque vague.");
        }

        // Au casque ou au clavier, au moment où on le demande : un affichage XR qui tourne (comme DesktopControls), et le jeu au
        // clavier. (Avant le 4 novembre, l'en-tête le lisait au lancement du mode Jeu, avant que le casque ne démarre : il a écrit
        // « sans casque » pour deux parties jouées au casque.)
        static string PlayMode()
        {
            SubsystemManager.GetSubsystems(s_Displays);
            var headset = false;
            foreach (var display in s_Displays)
                headset |= display.running;

            var desktop = DesktopControls.Instance != null && DesktopControls.Instance.IsActive;
            if (desktop)
                return headset ? "au clavier, le casque branché" : "au clavier et à la souris";
            return headset ? "au casque" : "ni casque ni clavier";
        }

        // ---------------------------------------------------------------- textes

        string Clock()
        {
            var seconds = Mathf.Max(0, Mathf.FloorToInt(Time.unscaledTime - m_GameStart));
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }

        static string Duration(float seconds)
        {
            var total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        static string OneLine(string text) => text.Replace("\r", " ").Replace("\n", " ").Trim();

        static string HealthText(Health health) =>
            health != null ? $"{Mathf.CeilToInt(health.Current)}/{Mathf.CeilToInt(health.Max)}" : "?";

        static string DifficultyName => DifficultyManager.Current != null ? DifficultyManager.Current.displayName : "?";

        static string BowName =>
            BowLoadout.Instance != null && BowLoadout.Instance.Selected != null ? BowLoadout.Instance.Selected.displayName : "?";

        // L'état en une ligne : les PV du joueur et de la tour, le score (plus l'or depuis le 8 novembre : il est retiré du jeu).
        string Status()
        {
            var parts = new List<string>
            {
                "joueur " + HealthText(m_PlayerHealth),
                "tour " + (m_Tower != null && !m_Tower.IsStanding ? "en ruines" : HealthText(m_TowerHealth)),
            };
            if (m_Score != null)
                parts.Add($"score {m_Score.Score}");

            return string.Join(", ", parts);
        }
    }
}
#endif
