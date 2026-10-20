using Archery.BowClasses;
using Archery.Bows;
using Archery.Core;
using Archery.Player;
using Archery.UI;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Chests
{
    /// <summary>
    /// L'arc légendaire, en 3 fragments (GDD, section 23 ; refait le 20 octobre avec les classes d'arc). Tant qu'il n'est
    /// pas débloqué, un coffre a une petite chance de proposer un fragment, à la place de sa deuxième amélioration
    /// permanente. Les fragments se gardent d'une partie à l'autre : c'est la progression du succès de l'arc (type
    /// <see cref="AchievementType.LegendaryFragment"/>), affichée sur sa carte du menu des arcs (« Fragment 1 / 3 »). Au
    /// dernier, l'arc est débloqué pour toujours et arrive tout de suite en main.
    /// </summary>
    /// <remarks>
    /// L'arc est celui de la liste des arcs (<see cref="BowLoadout"/>) que débloque ce succès. Pas de fragment avec l'arc
    /// cosmique en main (décidé le 19 octobre : on le garde). Test (éditeur, Development Build) : L donne un fragment.
    /// </remarks>
    [DisallowMultipleComponent]
    public class LegendaryBow : MonoBehaviour
    {
        [Tooltip("Chance (0 à 1) qu'un coffre propose un fragment, tant que l'arc n'est pas débloqué.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_PartChance = 0.1f;

        [Tooltip("Couleur de l'orbe d'un fragment et des messages.")]
        [SerializeField]
        Color m_Color = new Color(1f, 0.6f, 0.2f);

        [Tooltip("Optionnel : son d'un fragment trouvé.")]
        [SerializeField]
        AudioClip m_PartClip;

        [Tooltip("Optionnel : son de l'arc assemblé.")]
        [SerializeField]
        AudioClip m_AssembledClip;

        public static LegendaryBow Instance { get; private set; }

        /// <summary>Le succès des fragments (le premier de type Legendary Fragment), ou null.</summary>
        static AchievementDefinition Achievement
        {
            get
            {
                var achievements = AchievementManager.Instance;
                if (achievements == null)
                    return null;
                foreach (var achievement in achievements.Achievements)
                {
                    if (achievement != null && achievement.type == AchievementType.LegendaryFragment)
                        return achievement;
                }

                return null;
            }
        }

        /// <summary>L'arc que débloquent les fragments, dans la liste des arcs, ou null.</summary>
        public BowDefinition Definition
        {
            get
            {
                var loadout = BowLoadout.Instance;
                var achievement = Achievement;
                if (loadout == null || achievement == null)
                    return null;
                foreach (var entry in loadout.Entries)
                {
                    if (entry != null && entry.bow != null && entry.unlockedBy == achievement)
                        return entry.bow;
                }

                return null;
            }
        }

        /// <summary>Fragments déjà trouvés, toutes parties confondues.</summary>
        public int Parts
        {
            get
            {
                var achievement = Achievement;
                return achievement != null ? AchievementManager.Instance.ProgressOf(achievement) : 0;
            }
        }

        public int PartsNeeded
        {
            get
            {
                var achievement = Achievement;
                return achievement != null ? achievement.target : 3;
            }
        }

        /// <summary>Tous les fragments sont trouvés : l'arc est débloqué pour toujours.</summary>
        public bool IsAssembled
        {
            get
            {
                var achievement = Achievement;
                return achievement != null && AchievementManager.Instance.IsUnlocked(achievement);
            }
        }

        public Color Color => m_Color;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Legendary Bow dans la scène.", this);
            Instance = this;
        }

        void Start()
        {
            if (Definition == null)
                Debug.LogWarning("LegendaryBow : aucun arc de la liste n'est débloqué par un succès « Legendary Fragment », " +
                                 "les coffres ne proposeront pas de fragment.", this);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.lKey.wasPressedThisFrame)
            {
                AddPart();
                if (!IsAssembled)
                    HeadMessages.Show($"Fragment d'arc légendaire : {Parts} / {PartsNeeded}", m_Color, 3f);
            }
        }
#endif

        /// <summary>Tire au sort si ce coffre propose un fragment. Jamais avec l'arc cosmique en main (arc à surcharge).</summary>
        public bool RollPartDrop() => Definition != null && !IsAssembled && !HoldsOverchargeBow() && Random.value < m_PartChance;

        static bool HoldsOverchargeBow()
        {
            var bow = FindAnyObjectByType<Bow>();
            return bow != null && bow.Definition != null && bow.Definition.overcharge;
        }

        /// <summary>Un fragment de plus ; au dernier, l'arc est débloqué pour toujours et arrive en main.</summary>
        public void AddPart()
        {
            var achievements = AchievementManager.Instance;
            if (achievements == null || Achievement == null || IsAssembled)
                return;

            achievements.Report(AchievementType.LegendaryFragment);
            if (!IsAssembled)
            {
                Sfx.Play(m_PartClip, HeadPosition(), 0.9f, 1f, 0f);
                return;
            }

            var bow = FindAnyObjectByType<Bow>();
            var definition = Definition;
            if (bow != null && definition != null)
                bow.SetDefinition(definition);

            Sfx.Play(m_AssembledClip, HeadPosition(), 1f, 1f, 0f);
            var rig = PlayerRig.Instance;
            if (rig == null)
                return;

            foreach (var hand in rig.Hands)
                Haptics.Pulse(hand, 0.8f, 0.3f);
            HeadMessages.Show("Arc légendaire assemblé !\n<size=60%>il est à toi pour toujours</size>", m_Color, 5f, 1.3f);
        }

        static Vector3 HeadPosition()
        {
            var rig = PlayerRig.Instance;
            return rig != null && rig.Head != null ? rig.Head.position : Vector3.zero;
        }
    }
}
