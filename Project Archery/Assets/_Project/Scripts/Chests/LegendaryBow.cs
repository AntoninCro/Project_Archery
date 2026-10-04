using Archery.Bows;
using Archery.Core;
using Archery.Player;
using Archery.UI;
using UnityEngine;

namespace Archery.Chests
{
    /// <summary>
    /// Arc légendaire en 3 morceaux (GDD, section 23). Tant qu'il n'est pas complet, un coffre peut proposer un morceau
    /// (à la place de sa troisième orbe). Avec tous les morceaux, l'arc légendaire remplace celui du joueur
    /// jusqu'à la fin de la partie.
    /// </summary>
    [DisallowMultipleComponent]
    public class LegendaryBow : MonoBehaviour
    {
        [Tooltip("Caractéristiques de l'arc légendaire.")]
        [SerializeField]
        BowDefinition m_Definition;

        [Tooltip("Nombre de morceaux à trouver.")]
        [Min(1)]
        [SerializeField]
        int m_PartsNeeded = 3;

        [Tooltip("Chance (0 à 1) qu'un coffre propose un morceau, tant que l'arc n'est pas complet.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_PartChance = 0.35f;

        [Tooltip("Couleur de l'orbe d'un morceau et des messages.")]
        [SerializeField]
        Color m_Color = new Color(0.75f, 0.45f, 1f);

        [Tooltip("Optionnel : son d'un morceau trouvé.")]
        [SerializeField]
        AudioClip m_PartClip;

        [Tooltip("Optionnel : son de l'arc assemblé.")]
        [SerializeField]
        AudioClip m_AssembledClip;

        public static LegendaryBow Instance { get; private set; }

        public int Parts { get; private set; }
        public int PartsNeeded => m_PartsNeeded;
        public bool IsAssembled => Parts >= m_PartsNeeded;
        public BowDefinition Definition => m_Definition;
        public Color Color => m_Color;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Legendary Bow dans la scène.", this);
            Instance = this;
            if (m_Definition == null)
                Debug.LogWarning("LegendaryBow : aucune définition d'arc, les coffres ne proposeront pas de morceau.", this);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Tire au sort si ce coffre propose un morceau.</summary>
        public bool RollPartDrop() => m_Definition != null && !IsAssembled && Random.value < m_PartChance;

        /// <summary>Un morceau de plus ; au dernier, l'arc est assemblé et remplace celui du joueur.</summary>
        public void AddPart()
        {
            if (IsAssembled)
                return;

            Parts++;
            if (!IsAssembled)
            {
                Sfx.Play(m_PartClip, HeadPosition(), 0.9f, 1f, 0f);
                return;
            }

            var bow = FindAnyObjectByType<Bow>();
            if (bow != null && m_Definition != null)
                bow.SetDefinition(m_Definition);

            Sfx.Play(m_AssembledClip, HeadPosition(), 1f, 1f, 0f);
            var rig = PlayerRig.Instance;
            if (rig == null)
                return;

            foreach (var hand in rig.Hands)
                Haptics.Pulse(hand, 0.8f, 0.3f);
            if (rig.Head != null)
                FloatingText.Spawn(rig.Head.position + rig.HeadYaw * new Vector3(0f, 0.3f, 2f), "Arc légendaire assemblé !", m_Color, 1.2f, 3f);
        }

        static Vector3 HeadPosition()
        {
            var rig = PlayerRig.Instance;
            return rig != null && rig.Head != null ? rig.Head.position : Vector3.zero;
        }
    }
}
