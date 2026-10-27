using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Player;
using Archery.Save;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.BowClasses
{
    /// <summary>
    /// Suit les succès (<see cref="AchievementDefinition"/>) pendant les parties et les sauvegarde dans
    /// <c>achievements.json</c> : ils restent acquis d'une partie à l'autre, et après avoir quitté le jeu.
    /// </summary>
    /// <remarks>
    /// Il écoute les événements du jeu (ennemi tué ou touché, tir, fin de vague, boss vaincu) sans rien changer aux
    /// autres scripts. Raccourcis de test (éditeur et builds Development) : <b>U</b> débloque tous les succès,
    /// <b>J</b> efface toute la progression.
    /// </remarks>
    [DisallowMultipleComponent]
    public class AchievementManager : MonoBehaviour
    {
        const string k_FileName = "achievements.json";
        const float k_AutoSaveDelay = 20f;

        [Tooltip("Tous les succès du jeu.")]
        [SerializeField]
        AchievementDefinition[] m_Achievements = Array.Empty<AchievementDefinition>();

        [Tooltip("Optionnel : son d'un succès débloqué.")]
        [SerializeField]
        AudioClip m_UnlockClip;

        [Serializable]
        class Entry
        {
            public string id;
            public int progress;
            public bool unlocked;
        }

        [Serializable]
        class SaveData
        {
            public List<Entry> entries = new List<Entry>();
        }

        readonly Dictionary<string, Entry> m_Entries = new Dictionary<string, Entry>();

        // Ennemis tués par chaque flèche en vol (succès Multi Kill).
        readonly Dictionary<Arrow, int> m_KillsByArrow = new Dictionary<Arrow, int>();

        WaveManager m_Waves;
        bool m_Dirty;
        float m_SaveTimer;

        public static AchievementManager Instance { get; private set; }

        /// <summary>Un succès vient d'être débloqué.</summary>
        public static event Action<AchievementDefinition> Unlocked;

        /// <summary>La progression d'un succès a changé (ou toute la progression a été effacée).</summary>
        public static event Action Changed;

        public IReadOnlyList<AchievementDefinition> Achievements => m_Achievements;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Achievement Manager dans la scène.", this);
            Instance = this;
            Load();
        }

        void OnEnable()
        {
            Enemy.Killed += OnEnemyKilled;
            Enemy.Damaged += OnEnemyDamaged;
            Bow.ShotFired += OnShotFired;
            Arrow.ShotEnded += OnArrowShotEnded;
            Boss.Defeated += OnBossDefeated;
            SubscribeWaves();
        }

        void OnDisable()
        {
            Enemy.Killed -= OnEnemyKilled;
            Enemy.Damaged -= OnEnemyDamaged;
            Bow.ShotFired -= OnShotFired;
            Arrow.ShotEnded -= OnArrowShotEnded;
            Boss.Defeated -= OnBossDefeated;
            if (m_Waves != null)
            {
                m_Waves.WaveEnded -= OnWaveEnded;
                m_Waves.GameOver -= SaveIfDirty;
                m_Waves = null;
            }

            SaveIfDirty();
        }

        // Le WaveManager se déclare dans son Awake : il est sûrement prêt au Start, peut-être pas au premier OnEnable.
        void Start() => SubscribeWaves();

        void SubscribeWaves()
        {
            if (m_Waves != null || WaveManager.Instance == null)
                return;
            m_Waves = WaveManager.Instance;
            m_Waves.WaveEnded += OnWaveEnded;
            m_Waves.GameOver += SaveIfDirty;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveIfDirty();
        }

        void OnApplicationQuit() => SaveIfDirty();

        void Update()
        {
            // Pas d'écriture du fichier à chaque ennemi tué : on regroupe.
            if (m_Dirty)
            {
                m_SaveTimer += Time.unscaledDeltaTime;
                if (m_SaveTimer >= k_AutoSaveDelay)
                    SaveIfDirty();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            if (keyboard.uKey.wasPressedThisFrame)
                UnlockAll();
            else if (keyboard.jKey.wasPressedThisFrame)
                ResetAll();
#endif
        }

        // ---------------------------------------------------------------- lecture

        /// <summary>
        /// Fait avancer les succès de ce type, pour ce qui n'est pas suivi par un événement (l'épreuve des étoiles, réussie :
        /// <see cref="Archery.Portal.StarPortal"/>).
        /// </summary>
        public void Report(AchievementType type, int amount = 1) => Add(type, amount);

        public bool IsUnlocked(AchievementDefinition achievement) =>
            achievement != null && m_Entries.TryGetValue(achievement.Id, out var entry) && entry.unlocked;

        /// <summary>Progression actuelle (plafonnée à l'objectif).</summary>
        public int ProgressOf(AchievementDefinition achievement)
        {
            if (achievement == null || !m_Entries.TryGetValue(achievement.Id, out var entry))
                return 0;
            return entry.unlocked ? achievement.target : Mathf.Min(entry.progress, achievement.target);
        }

        /// <summary>Progression entre 0 et 1.</summary>
        public float Progress01(AchievementDefinition achievement) =>
            achievement == null ? 0f : Mathf.Clamp01(ProgressOf(achievement) / (float)Mathf.Max(1, achievement.target));

        // ---------------------------------------------------------------- événements du jeu

        void OnEnemyKilled(Enemy enemy, DamageInfo info)
        {
            // Les mannequins du centre d'entraînement (26 octobre) ne comptent pas.
            if (enemy != null && enemy.IsTrainingDummy)
                return;

            Add(AchievementType.EnemiesKilled, 1);

            // Une flèche qui tue plusieurs ennemis (perçage, ricochet).
            if (info.Source is Arrow arrow && arrow != null)
            {
                m_KillsByArrow.TryGetValue(arrow, out var kills);
                kills++;
                m_KillsByArrow[arrow] = kills;
                foreach (var achievement in m_Achievements)
                {
                    // Compté une seule fois par flèche, au moment où elle atteint le nombre demandé.
                    if (achievement != null && achievement.type == AchievementType.MultiKill && kills == achievement.multiKillSize)
                        AddTo(achievement, 1);
                }
            }
        }

        void OnEnemyDamaged(Enemy enemy, DamageInfo info)
        {
            if (info.Zone != HitZone.Head || !(info.Source is Arrow) || (enemy != null && enemy.IsTrainingDummy))
                return;

            Add(AchievementType.Headshots, 1);
            if (info.Grade == ShotGrade.Perfect)
                Add(AchievementType.PerfectHeadshots, 1);
        }

        void OnShotFired(Bow bow, ShotInfo shot)
        {
            // Une flèche réutilisée (réserve de flèches) repart de zéro.
            if (shot.Arrow != null)
                m_KillsByArrow.Remove(shot.Arrow);
            if (shot.Grade == ShotGrade.Perfect)
                Add(AchievementType.PerfectShots, 1);
        }

        void OnArrowShotEnded(Arrow arrow) => m_KillsByArrow.Remove(arrow);

        void OnBossDefeated(Boss boss) => Add(AchievementType.BossesKilled, 1);

        void OnWaveEnded(int wave)
        {
            foreach (var achievement in m_Achievements)
            {
                if (achievement != null && achievement.type == AchievementType.WaveReached)
                    SetAtLeast(achievement, wave);
            }

            SaveIfDirty();
        }

        // ---------------------------------------------------------------- progression

        void Add(AchievementType type, int amount)
        {
            foreach (var achievement in m_Achievements)
            {
                if (achievement != null && achievement.type == type)
                    AddTo(achievement, amount);
            }
        }

        void AddTo(AchievementDefinition achievement, int amount)
        {
            var entry = EntryOf(achievement);
            if (entry.unlocked)
                return;
            entry.progress += amount;
            AfterProgress(achievement, entry);
        }

        void SetAtLeast(AchievementDefinition achievement, int value)
        {
            var entry = EntryOf(achievement);
            if (entry.unlocked || value <= entry.progress)
                return;
            entry.progress = value;
            AfterProgress(achievement, entry);
        }

        void AfterProgress(AchievementDefinition achievement, Entry entry)
        {
            m_Dirty = true;
            if (entry.progress >= achievement.target)
            {
                entry.unlocked = true;
                Announce(achievement);
                Unlocked?.Invoke(achievement);
                SaveIfDirty();
            }

            Changed?.Invoke();
        }

        Entry EntryOf(AchievementDefinition achievement)
        {
            if (!m_Entries.TryGetValue(achievement.Id, out var entry))
            {
                entry = new Entry { id = achievement.Id };
                m_Entries.Add(entry.id, entry);
            }

            return entry;
        }

        void Announce(AchievementDefinition achievement)
        {
            HeadMessages.Show("Succès débloqué : " + achievement.title, achievement.color, 4f, 1.2f);
            var rig = PlayerRig.Instance;
            var position = rig != null && rig.Head != null ? rig.Head.position : transform.position;
            Sfx.Play(m_UnlockClip, position, 1f, 1f, 0f);
            if (rig != null)
            {
                foreach (var hand in rig.Hands)
                    Haptics.Pulse(hand, 0.6f, 0.25f);
            }
        }

        // ---------------------------------------------------------------- tests

        /// <summary>Débloque tous les succès (test).</summary>
        [ContextMenu("Débloquer tous les succès")]
        public void UnlockAll()
        {
            foreach (var achievement in m_Achievements)
            {
                if (achievement == null)
                    continue;
                var entry = EntryOf(achievement);
                if (entry.unlocked)
                    continue;
                entry.progress = achievement.target;
                AfterProgress(achievement, entry);
            }
        }

        /// <summary>Efface toute la progression (test) : tous les arcs se reverrouillent.</summary>
        [ContextMenu("Effacer tous les succès")]
        public void ResetAll()
        {
            m_Entries.Clear();
            m_Dirty = true;
            SaveIfDirty();
            HeadMessages.Show("Succès effacés", new Color(1f, 0.55f, 0.5f), 2f);
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- sauvegarde

        void Load()
        {
            m_Entries.Clear();
            if (!JsonStorage.TryLoad<SaveData>(k_FileName, out var data) || data.entries == null)
                return;

            foreach (var entry in data.entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.id))
                    m_Entries[entry.id] = entry;
            }
        }

        void SaveIfDirty()
        {
            m_SaveTimer = 0f;
            if (!m_Dirty)
                return;

            m_Dirty = false;
            var data = new SaveData();
            data.entries.AddRange(m_Entries.Values);
            JsonStorage.Save(k_FileName, data);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            Unlocked = null;
            Changed = null;
        }
    }
}
