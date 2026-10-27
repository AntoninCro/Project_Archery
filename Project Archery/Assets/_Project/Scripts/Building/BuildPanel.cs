using System;
using System.Collections.Generic;
using Archery.Core;
using Archery.Player;
using Archery.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Building
{
    /// <summary>
    /// Le petit panneau des socles (GDD, section 9) : un Canvas en World Space, unique dans la scène. Il s'ouvre devant
    /// le joueur, du côté du socle visé, et propose les bâtiments possibles, ou le niveau suivant du bâtiment déjà construit.
    /// Les cartes sont celles de la boutique (<see cref="ShopCard"/>). Depuis la refonte du 27 octobre, chacune coûte un plan
    /// de son bâtiment (trouvé dans les caisses de l'ingénieur) au lieu de matériaux, et dit combien on en a.
    /// </summary>
    /// <remarks>
    /// Comme la boutique, le Canvas a besoin d'un <c>Tracked Device Graphic Raycaster</c> pour être cliqué au rayon.
    /// Il se ferme après un achat, avec son bouton, ou quand le joueur s'éloigne.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BuildPanel : MonoBehaviour
    {
        [Tooltip("Tout ce qui s'affiche quand le panneau est ouvert.")]
        [SerializeField]
        GameObject m_Content;

        [Tooltip("« Construire » sur un socle vide, « Baliste · niveau 2 » sur un bâtiment.")]
        [SerializeField]
        TMP_Text m_TitleText;

        [Tooltip("Les plans du joueur, tous bâtiments confondus (ses matériaux avant le 27 octobre).")]
        [SerializeField]
        TMP_Text m_MaterialsText;

        [Tooltip("Une carte par bâtiment possible (2 par type de socle).")]
        [SerializeField]
        ShopCard[] m_Cards = Array.Empty<ShopCard>();

        [SerializeField]
        Button m_CloseButton;

        [Tooltip("Message après un clic (« Il te faut un plan »…).")]
        [SerializeField]
        TMP_Text m_MessageText;

        [SerializeField]
        float m_MessageDuration = 3f;

        [Header("Placement")]
        [Tooltip("Distance (m) entre la tête du joueur et le panneau, du côté du socle.")]
        [SerializeField]
        float m_Distance = 1.2f;

        [Tooltip("Hauteur (m) du panneau par rapport aux yeux : un peu au-dessus du socle visé, pour ne pas le cacher.")]
        [SerializeField]
        float m_HeightFromHead = -0.05f;

        [Tooltip("Inclinaison (°) du haut du panneau vers l'arrière.")]
        [SerializeField]
        float m_Tilt = 10f;

        [Tooltip("Le panneau se ferme quand le joueur s'éloigne de plus de cette distance (m) de l'endroit où il l'a ouvert.")]
        [SerializeField]
        float m_CloseDistance = 2.5f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_OpenClip;

        [SerializeField]
        AudioClip m_CloseClip;

        [SerializeField]
        AudioClip m_BuyClip;

        [SerializeField]
        AudioClip m_ErrorClip;

        readonly List<StructureDefinition> m_Choices = new List<StructureDefinition>();
        StructureDefinition[] m_CardChoices = Array.Empty<StructureDefinition>();
        int m_MoveCard = -1;
        BuildManager m_Builds;
        XRBaseInputInteractor m_Hand;
        Vector3 m_Anchor;
        float m_MessageTimer;

        public static BuildPanel Instance { get; private set; }

        /// <summary>Le socle dont le panneau est ouvert, ou null.</summary>
        public BuildSocket Socket { get; private set; }

        public bool IsOpen => Socket != null;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Build Panel dans la scène.", this);
            Instance = this;

            m_CardChoices = new StructureDefinition[m_Cards.Length];
            foreach (var card in m_Cards)
            {
                if (card != null)
                    card.Clicked += OnCardClicked;
            }

            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(OnCloseClicked);
            if (m_Content != null)
                m_Content.SetActive(false);
        }

        void Start()
        {
            m_Builds = BuildManager.Instance;
            if (m_Builds != null)
                m_Builds.PlansChanged += Refresh;
        }

        void OnDestroy()
        {
            if (m_Builds != null)
                m_Builds.PlansChanged -= Refresh;
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Ouvre le panneau pour ce socle, devant le joueur. La main donnée recevra le marteau.</summary>
        public void Open(BuildSocket socket, XRBaseInputInteractor hand)
        {
            if (socket == null)
                return;

            Socket = socket;
            m_Hand = hand;
            PlaceForPlayer(socket);
            if (m_Content != null)
                m_Content.SetActive(true);
            ShowMessage("", Color.white);
            Refresh();
            Sfx.Play(m_OpenClip, transform.position, 0.8f);
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            Socket = null;
            m_Hand = null;
            if (m_Content != null)
                m_Content.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen)
                return;

            if (m_MessageTimer > 0f)
            {
                m_MessageTimer -= Time.unscaledDeltaTime;
                if (m_MessageTimer <= 0f && m_MessageText != null)
                    m_MessageText.text = "";
            }

            // Le joueur s'en va, ou le socle a disparu : le panneau se ferme.
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            var moved = head != null ? head.position - m_Anchor : Vector3.zero;
            moved.y = 0f;
            if (Socket == null || moved.magnitude > m_CloseDistance)
                Close();
        }

        // Devant le joueur, dans la direction du socle (ou du regard, s'il est dessus), à hauteur des yeux.
        void PlaceForPlayer(BuildSocket socket)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            var toSocket = socket.transform.position - head.position;
            toSocket.y = 0f;
            var direction = toSocket.magnitude > 1.5f ? toSocket.normalized : rig.HeadYaw * Vector3.forward;
            m_Anchor = head.position;
            transform.SetPositionAndRotation(head.position + direction * m_Distance + Vector3.up * m_HeightFromHead,
                                             Quaternion.LookRotation(direction) * Quaternion.Euler(m_Tilt, 0f, 0f));
        }

        void Refresh()
        {
            if (!IsOpen)
                return;

            var builds = BuildManager.Instance;
            if (m_MaterialsText != null)
                m_MaterialsText.text = "Plans : " + (builds != null ? builds.TotalPlans : 0);

            var structure = Socket.Structure;
            if (m_TitleText != null)
                m_TitleText.text = structure == null ? $"Construire · {Socket.DisplayName}"
                    : $"{structure.DisplayName} · niveau {structure.Level}";

            for (var i = 0; i < m_CardChoices.Length; i++)
                m_CardChoices[i] = null;
            m_MoveCard = -1;

            // Un bâtiment : son niveau suivant, et à côté, le déplacer (refonte du 27 octobre, étape 3 : gratuit, entre les vagues).
            if (structure != null)
            {
                ShowCard(0, UpgradeOffer(structure, builds), structure.Definition, builds);
                var movable = builds != null && structure.IsBuilt && !structure.IsUnderConstruction && m_Cards.Length > 1;
                if (movable)
                    m_MoveCard = 1;
                ShowCard(1, movable ? MoveOffer(structure, builds.CanMove) : null, null, builds);
                for (var i = 2; i < m_Cards.Length; i++)
                    ShowCard(i, null, null, builds);
                return;
            }

            if (builds != null)
                builds.GetChoices(Socket.Slot, m_Choices);
            else
                m_Choices.Clear();

            for (var i = 0; i < m_Cards.Length; i++)
            {
                var definition = i < m_Choices.Count ? m_Choices[i] : null;
                ShowCard(i, definition != null ? BuildOffer(definition, 1, builds) : null, definition, builds);
            }

            if (m_Choices.Count > m_Cards.Length)
                Debug.LogWarning($"BuildPanel : {m_Choices.Count} bâtiments possibles pour {m_Cards.Length} cartes.", this);
        }

        // La carte compare le prix (un plan) aux plans qu'on a de ce bâtiment : sans plan, son prix passe dans la couleur « trop cher ».
        void ShowCard(int index, ShopOffer offer, StructureDefinition definition, BuildManager builds)
        {
            if (index >= m_Cards.Length || m_Cards[index] == null)
                return;

            m_CardChoices[index] = definition;
            m_Cards[index].Show(offer, builds != null ? builds.PlansOf(definition) : 0);
        }

        static ShopOffer UpgradeOffer(Structure structure, BuildManager builds)
        {
            var definition = structure.Definition;
            if (definition == null)
                return ShopOffer.Info(structure.DisplayName, "", Color.white);

            if (structure.Level >= definition.MaxLevel)
            {
                var info = ShopOffer.Info($"{definition.displayName} · niveau {structure.Level}", "Niveau maximal atteint.", definition.color);
                info.Subtitle = "Niveau " + structure.Level;
                info.Icon = definition.icon;
                return info;
            }

            return BuildOffer(definition, structure.Level + 1, builds);
        }

        // Un plan par construction ou par niveau (refonte du 27 octobre) ; le sous-titre dit combien on en a.
        static ShopOffer BuildOffer(StructureDefinition definition, int level, BuildManager builds)
        {
            var plans = builds != null ? builds.PlansOf(definition) : 0;
            var owned = plans == 0 ? "aucun plan" : plans == 1 ? "1 plan" : plans + " plans";
            return new ShopOffer
            {
                Kind = ShopOfferKind.Build,
                // La carte de la boutique n'a pas toujours de sous-titre : le niveau est aussi dans le titre.
                Title = level == 1 ? definition.displayName : $"{definition.displayName} · niveau {level}",
                Subtitle = (level == 1 ? "Construction" : "Amélioration") + " · " + owned,
                Description = definition.DescriptionFor(level),
                Color = definition.color,
                Icon = definition.icon,
                Price = 1,
                PriceUnit = "plan",
            };
        }

        static ShopOffer MoveOffer(Structure structure, bool now) => new ShopOffer
        {
            Kind = now ? ShopOfferKind.Build : ShopOfferKind.Unavailable,
            Title = "Déplacer",
            Subtitle = structure.DisplayName,
            Description = now ? "Son hologramme suit ton rayon : pose-le sur un emplacement libre de la même sorte." : "Entre les vagues seulement.",
            Color = new Color(0.55f, 0.8f, 1f),
            Icon = structure.Definition != null ? structure.Definition.icon : null,
            PriceText = now ? "Gratuit" : "",
            Verb = "Déplacer",
        };

        void OnCardClicked(ShopCard card)
        {
            var index = Array.IndexOf(m_Cards, card);
            var manager = BuildManager.Instance;
            if (!IsOpen || index < 0 || manager == null || card.Offer == null || !card.Offer.CanBuy)
                return;

            // Déplacer : l'hologramme du bâtiment suit ensuite le rayon de la main, comme un plan sorti de la montre.
            var placement = BuildPlacement.Instance;
            var result = index != m_MoveCard ? manager.TryBuild(Socket, m_CardChoices[index], m_Hand)
                : placement != null ? placement.BeginMove(Socket.Structure, m_Hand)
                : ShopResult.Failed("Déplacement impossible");
            if (!result.Success)
            {
                Sfx.Play(m_ErrorClip, transform.position, 0.9f);
                ShowMessage(result.Message, result.Color);
                return;
            }

            Sfx.Play(m_BuyClip, transform.position, 0.9f);
            Close();
        }

        void OnCloseClicked()
        {
            Sfx.Play(m_CloseClip, transform.position, 0.7f);
            Close();
        }

        void ShowMessage(string message, Color color)
        {
            if (m_MessageText == null)
                return;

            m_MessageText.text = message;
            m_MessageText.color = color;
            m_MessageTimer = string.IsNullOrEmpty(message) ? 0f : m_MessageDuration;
        }
    }
}
