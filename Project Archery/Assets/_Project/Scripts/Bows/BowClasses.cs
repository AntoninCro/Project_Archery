using System;
using System.Collections.Generic;
using Archery.Core;
using Archery.Economy;
using Archery.Player;
using Archery.Save;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Bows
{
    /// <summary>
    /// Les arcs comme des classes, avec une progression d'une partie à l'autre (GDD, section 23) :
    /// <list type="bullet">
    /// <item>on choisit son arc au menu, avant la partie, et la boutique n'en vend plus ;</item>
    /// <item>chaque partie rapporte de l'expérience (une part du score), gardée dans <c>progress.json</c> ;</item>
    /// <item>l'expérience débloque les arcs peu à peu (réglage <c>Unlock Xp</c> de chaque arc).</item>
    /// </list>
    /// Sans ce composant, rien ne change : les arcs s'achètent en boutique.
    /// </summary>
    [DisallowMultipleComponent]
    public class BowClasses : MonoBehaviour
    {
        [Tooltip("Les arcs, dans l'ordre. Le premier est toujours débloqué ; les autres demandent l'expérience de leur asset (Unlock Xp).")]
        [SerializeField]
        List<BowDefinition> m_Bows = new List<BowDefinition>();

        [Tooltip("Expérience gagnée par point de score (0,1 = 1 XP pour 10 points).")]
        [SerializeField]
        float m_XpPerPoint = 0.1f;

        [Tooltip("Pour une démo : tous les arcs sont débloqués.")]
        [SerializeField]
        bool m_UnlockAll;

        [SerializeField]
        AudioClip m_UnlockClip;

        [SerializeField]
        Color m_UnlockColor = new Color(1f, 0.85f, 0.3f);

        readonly List<BowDefinition> m_NewlyUnlocked = new List<BowDefinition>();
        WaveManager m_Waves;
        bool m_Awarded;

        public static BowClasses Instance { get; private set; }

        /// <summary>Les arcs sont des classes dans cette scène.</summary>
        public static bool IsActive => Instance != null && Instance.isActiveAndEnabled;

        public IReadOnlyList<BowDefinition> Bows => m_Bows;

        /// <summary>L'arc choisi pour la partie.</summary>
        public BowDefinition Selected { get; private set; }

        /// <summary>Expérience totale.</summary>
        public int Xp => PlayerProgress.Data.xp;

        /// <summary>Expérience gagnée à la fin de cette partie (0 avant).</summary>
        public int LastXpGained { get; private set; }

        /// <summary>Arcs débloqués à la fin de cette partie.</summary>
        public IReadOnlyList<BowDefinition> NewlyUnlocked => m_NewlyUnlocked;

        /// <summary>L'arc choisi ou l'expérience ont changé.</summary>
        public event Action Changed;

        /// <summary>On ne change d'arc qu'avant la première vague.</summary>
        public static bool CanChoose => WaveManager.Instance == null || WaveManager.Instance.Phase == WavePhase.Ready;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Bow Classes dans la scène.", this);
            Instance = this;
        }

        void Start()
        {
            Selected = SavedBow();
            Apply();

            m_Waves = WaveManager.Instance;
            if (m_Waves != null)
                m_Waves.GameOver += OnGameOver;
        }

        void OnDestroy()
        {
            if (m_Waves != null)
                m_Waves.GameOver -= OnGameOver;
            if (Instance == this)
                Instance = null;
        }

        public bool IsUnlocked(BowDefinition bow) =>
            bow != null && (m_UnlockAll || m_Bows.IndexOf(bow) == 0 || bow.unlockXp <= Xp);

        /// <summary>Le premier arc, dans l'ordre, qui reste à débloquer (null s'ils le sont tous).</summary>
        public BowDefinition NextLocked()
        {
            foreach (var bow in m_Bows)
            {
                if (bow != null && !IsUnlocked(bow))
                    return bow;
            }

            return null;
        }

        /// <summary>Passe à l'arc débloqué suivant (+1) ou précédent (-1).</summary>
        public void Select(int step)
        {
            if (!CanChoose || m_Bows.Count == 0)
                return;

            var count = m_Bows.Count;
            var index = Mathf.Max(0, m_Bows.IndexOf(Selected));
            for (var i = 1; i <= count; i++)
            {
                var candidate = m_Bows[((index + step * i) % count + count) % count];
                if (!IsUnlocked(candidate))
                    continue;

                Selected = candidate;
                break;
            }

            PlayerProgress.Data.selectedBow = Selected != null ? Selected.name : "";
            PlayerProgress.Save();
            Apply();
            Changed?.Invoke();
        }

        /// <summary>
        /// Donne l'expérience de la partie, une seule fois (à la fin de la partie). Renvoie l'expérience gagnée.
        /// </summary>
        public int AwardGameXp()
        {
            if (m_Awarded)
                return LastXpGained;
            m_Awarded = true;

            var unlockedBefore = new List<BowDefinition>();
            foreach (var bow in m_Bows)
            {
                if (IsUnlocked(bow))
                    unlockedBefore.Add(bow);
            }

            var score = ScoreManager.Instance;
            LastXpGained = score != null ? Mathf.RoundToInt(score.Score * m_XpPerPoint) : 0;
            PlayerProgress.Data.xp += LastXpGained;
            PlayerProgress.Data.gamesPlayed++;
            PlayerProgress.Save();

            m_NewlyUnlocked.Clear();
            foreach (var bow in m_Bows)
            {
                if (bow != null && IsUnlocked(bow) && !unlockedBefore.Contains(bow))
                    m_NewlyUnlocked.Add(bow);
            }

            if (m_NewlyUnlocked.Count > 0)
                AnnounceUnlock(m_NewlyUnlocked[0]);
            Changed?.Invoke();
            return LastXpGained;
        }

        void OnGameOver() => AwardGameXp();

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Raccourci de test au clavier : X donne 500 XP.
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.xKey.wasPressedThisFrame)
                return;

            PlayerProgress.Data.xp += 500;
            PlayerProgress.Save();
            Debug.Log($"BowClasses : +500 XP (total {Xp}).", this);
            Changed?.Invoke();
#endif
        }

        BowDefinition SavedBow()
        {
            var saved = PlayerProgress.Data.selectedBow;
            foreach (var bow in m_Bows)
            {
                if (bow != null && bow.name == saved && IsUnlocked(bow))
                    return bow;
            }

            return m_Bows.Count > 0 ? m_Bows[0] : null;
        }

        void Apply()
        {
            var bow = FindAnyObjectByType<Bow>();
            if (bow != null && Selected != null)
                bow.SetDefinition(Selected);
        }

        void AnnounceUnlock(BowDefinition bow)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            Sfx.Play(m_UnlockClip, head.position, 0.9f, 1f, 0f);
            FloatingText.Spawn(head.position + rig.HeadYaw * new Vector3(0f, 0.5f, 2f), $"Nouvel arc débloqué : {bow.displayName} !", m_UnlockColor, 1f, 3f);
        }
    }
}
