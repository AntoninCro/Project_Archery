using System;
using System.Collections.Generic;
using System.Reflection;
using Archery.Bows;
using Archery.Core;
using Archery.Player;
using Archery.Save;
using Archery.Shop;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.BowClasses
{
    /// <summary>
    /// Les classes d'arc : la liste des arcs qu'on peut choisir au début de la partie, le succès qui débloque chacun,
    /// et l'arc choisi (gardé dans <c>bow_loadout.json</c>, donc aussi après une défaite ou en relançant le jeu).
    /// L'arc ne se change qu'avant la première vague. Le menu <see cref="BowSelectionMenu"/> l'affiche.
    /// </summary>
    /// <remarks>
    /// Les arcs ne s'achètent plus : au lancement, la carte « Arc » de la boutique est retirée (sans toucher au script
    /// de la boutique ni à la scène). Raccourcis de test (éditeur et builds Development) : <b>5 à 9</b> choisissent
    /// le 1er au 5e arc de la liste, s'il est débloqué.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BowLoadout : MonoBehaviour
    {
        const string k_FileName = "bow_loadout.json";

        /// <summary>Un arc de la liste.</summary>
        [Serializable]
        public class Entry
        {
            [Tooltip("Caractéristiques et modèle de l'arc. Vide : une carte « Bientôt » (arc pas encore créé).")]
            public BowDefinition bow;

            [Tooltip("Succès qui le débloque. Vide : débloqué dès le départ.")]
            public AchievementDefinition unlockedBy;

            [Tooltip("Couleur de sa carte dans le menu (cadre, jauges).")]
            public Color color = new Color(1f, 0.82f, 0.35f);

            [Tooltip("Seulement sans arc : nom affiché sur la carte « Bientôt ».")]
            public string comingSoonName = "Arc légendaire";
        }

        [Serializable]
        class SaveData
        {
            public string bowName;
        }

        [Tooltip("Les arcs, dans l'ordre du menu. Le premier doit être débloqué dès le départ : c'est l'arc par défaut.")]
        [SerializeField]
        List<Entry> m_Entries = new List<Entry>();

        [Tooltip("Retire la carte « Arc » de la boutique au lancement : les arcs ne s'achètent plus.")]
        [SerializeField]
        bool m_RemoveShopBowCard = true;

        [Tooltip("Optionnel : son quand on choisit un arc.")]
        [SerializeField]
        AudioClip m_SelectClip;

        [Tooltip("Optionnel : son quand on vise un arc verrouillé.")]
        [SerializeField]
        AudioClip m_LockedClip;

        static BowDefinition s_Selected;
        Bow m_Bow;

        public static BowLoadout Instance { get; private set; }

        /// <summary>L'arc choisi a changé.</summary>
        public static event Action<BowDefinition> Changed;

        public IReadOnlyList<Entry> Entries => m_Entries;

        /// <summary>L'arc choisi (le premier de la liste tant que rien n'est choisi).</summary>
        public BowDefinition Selected => s_Selected != null ? s_Selected : DefaultBow;

        /// <summary>On ne change d'arc qu'avant la première vague.</summary>
        public bool CanChange
        {
            get
            {
                var waves = WaveManager.Instance;
                return waves == null || waves.Phase == WavePhase.Ready;
            }
        }

        BowDefinition DefaultBow => m_Entries.Count > 0 ? m_Entries[0].bow : null;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Bow Loadout dans la scène.", this);
            Instance = this;
            if (m_Entries.Count == 0 || m_Entries[0].bow == null)
                Debug.LogWarning("BowLoadout : le premier arc de la liste (l'arc par défaut) est vide.", this);
            else if (m_Entries[0].unlockedBy != null)
                Debug.LogWarning("BowLoadout : le premier arc de la liste devrait être débloqué dès le départ (Unlocked By vide).", this);
        }

        void OnEnable()
        {
            AchievementManager.Unlocked += OnAchievementUnlocked;
            AchievementManager.Changed += OnAchievementsChanged;
        }

        void OnDisable()
        {
            AchievementManager.Unlocked -= OnAchievementUnlocked;
            AchievementManager.Changed -= OnAchievementsChanged;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            m_Bow = FindAnyObjectByType<Bow>();
            if (m_Bow == null)
                Debug.LogWarning("BowLoadout : aucun arc (Bow) dans la scène.", this);

            if (m_RemoveShopBowCard)
                RemoveShopBowCard();

            // Premier lancement de la scène : l'arc gardé dans la sauvegarde. Après une défaite (la scène est rechargée),
            // celui de la partie d'avant.
            if (s_Selected == null && JsonStorage.TryLoad<SaveData>(k_FileName, out var data))
                s_Selected = FindBow(data.bowName);

            // Un arc reverrouillé (succès effacés) ou retiré de la liste : retour à l'arc par défaut.
            if (s_Selected != null && !IsUnlocked(s_Selected))
                s_Selected = null;

            ApplyToBow();
            Changed?.Invoke(Selected);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.digit5Key.wasPressedThisFrame)
                SelectIndex(0);
            else if (keyboard.digit6Key.wasPressedThisFrame)
                SelectIndex(1);
            else if (keyboard.digit7Key.wasPressedThisFrame)
                SelectIndex(2);
            else if (keyboard.digit8Key.wasPressedThisFrame)
                SelectIndex(3);
            else if (keyboard.digit9Key.wasPressedThisFrame)
                SelectIndex(4);
        }
#endif

        // ---------------------------------------------------------------- lecture

        /// <summary>Un arc est débloqué s'il n'a pas de succès à remplir, ou si son succès est fait.</summary>
        public bool IsUnlocked(Entry entry)
        {
            if (entry == null || entry.bow == null)
                return false;
            if (entry.unlockedBy == null)
                return true;
            var achievements = AchievementManager.Instance;
            return achievements != null && achievements.IsUnlocked(entry.unlockedBy);
        }

        public bool IsUnlocked(BowDefinition bow) => IsUnlocked(EntryOf(bow));

        public Entry EntryOf(BowDefinition bow)
        {
            if (bow == null)
                return null;
            foreach (var entry in m_Entries)
            {
                if (entry != null && entry.bow == bow)
                    return entry;
            }

            return null;
        }

        BowDefinition FindBow(string bowName)
        {
            if (string.IsNullOrEmpty(bowName))
                return null;
            foreach (var entry in m_Entries)
            {
                if (entry != null && entry.bow != null && entry.bow.name == bowName)
                    return entry.bow;
            }

            return null;
        }

        // ---------------------------------------------------------------- choix

        /// <summary>Pour un bouton ou un raccourci : 0 = le premier arc de la liste.</summary>
        public void SelectIndex(int index)
        {
            if (index >= 0 && index < m_Entries.Count)
                TrySelect(m_Entries[index]);
        }

        /// <summary>
        /// Équipe cet arc s'il est débloqué et que la partie n'a pas commencé. Sinon, un message dit pourquoi.
        /// Renvoie vrai si l'arc est (ou était déjà) équipé.
        /// </summary>
        public bool TrySelect(Entry entry)
        {
            if (entry == null || entry.bow == null)
            {
                Refuse("Cet arc arrive bientôt");
                return false;
            }

            if (!IsUnlocked(entry))
            {
                var hint = entry.unlockedBy != null ? entry.unlockedBy.description : "";
                Refuse("Verrouillé. " + hint);
                return false;
            }

            if (entry.bow == Selected)
                return true;

            if (!CanChange)
            {
                Refuse("On change d'arc avant la première vague");
                return false;
            }

            s_Selected = entry.bow;
            JsonStorage.Save(k_FileName, new SaveData { bowName = entry.bow.name });
            ApplyToBow();
            PlaySound(m_SelectClip);
            HeadMessages.Show(entry.bow.displayName + " équipé", entry.color, 2f);
            Changed?.Invoke(entry.bow);
            return true;
        }

        void Refuse(string message)
        {
            PlaySound(m_LockedClip);
            HeadMessages.Show(message, new Color(1f, 0.6f, 0.55f), 2.5f);
        }

        void ApplyToBow()
        {
            if (m_Bow != null && Selected != null)
                m_Bow.SetDefinition(Selected);
        }

        void PlaySound(AudioClip clip)
        {
            var rig = PlayerRig.Instance;
            var position = rig != null && rig.Head != null ? rig.Head.position : transform.position;
            Sfx.Play(clip, position, 0.8f, 1f, 0f);
        }

        // Succès effacés (touche J) : l'arc équipé, reverrouillé, est remplacé par l'arc par défaut (avant la vague 1).
        void OnAchievementsChanged()
        {
            if (s_Selected == null || IsUnlocked(s_Selected) || !CanChange)
                return;

            s_Selected = null;
            if (DefaultBow != null)
                JsonStorage.Save(k_FileName, new SaveData { bowName = DefaultBow.name });
            ApplyToBow();
            Changed?.Invoke(Selected);
        }

        void OnAchievementUnlocked(AchievementDefinition achievement)
        {
            foreach (var entry in m_Entries)
            {
                if (entry != null && entry.bow != null && entry.unlockedBy == achievement)
                    HeadMessages.Show("Nouvel arc : " + entry.bow.displayName, entry.color, 4f, 1.2f);
            }
        }

        // ---------------------------------------------------------------- boutique

        // La boutique garde sa carte « Arc » dans un champ privé (m_BowCard). On la cache et on vide ce champ : la
        // boutique saute alors cette carte, et ses autres cartes se recentrent. Sans ce champ (script de la boutique
        // modifié), on cherche la carte par son nom.
        void RemoveShopBowCard()
        {
            var panel = FindAnyObjectByType<ShopPanel>(FindObjectsInactive.Include);
            if (panel == null)
                return;

            var field = typeof(ShopPanel).GetField("m_BowCard", BindingFlags.Instance | BindingFlags.NonPublic);
            var card = field != null ? field.GetValue(panel) as ShopCard : null;
            if (card == null)
            {
                foreach (var candidate in panel.GetComponentsInChildren<ShopCard>(true))
                {
                    if (candidate.name == "Bow Card")
                        card = candidate;
                }
            }

            if (card == null)
                return;

            card.gameObject.SetActive(false);
            if (field != null)
                field.SetValue(panel, null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Selected = null;
            Instance = null;
            Changed = null;
        }
    }
}
