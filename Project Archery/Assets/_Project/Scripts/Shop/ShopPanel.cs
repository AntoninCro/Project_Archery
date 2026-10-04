using System;
using System.Globalization;
using System.Text;
using Archery.Core;
using Archery.Economy;
using Archery.Player;
using Archery.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.Shop
{
    /// <summary>
    /// Le panneau de la boutique (Canvas en World Space). Il affiche les offres du <see cref="ShopManager"/>
    /// pendant les pauses et transmet les clics. Les sons partent du panneau.
    /// </summary>
    /// <remarks>
    /// Pour cliquer au rayon de la manette, le Canvas a besoin d'un <c>Tracked Device Graphic Raycaster</c>,
    /// et l'EventSystem d'un <c>XR UI Input Module</c>.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ShopPanel : MonoBehaviour
    {
        [Tooltip("Tout ce qui s'affiche quand la boutique est ouverte (caché pendant les vagues).")]
        [SerializeField]
        GameObject m_Content;

        [Tooltip("Une carte par amélioration proposée (4 par défaut).")]
        [SerializeField]
        ShopCard[] m_UpgradeCards = Array.Empty<ShopCard>();

        [SerializeField]
        ShopCard m_BowCard;

        [SerializeField]
        ShopCard m_TowerCard;

        [SerializeField]
        Button m_RerollButton;

        [Tooltip("Texte du bouton de relance (son prix).")]
        [SerializeField]
        TMP_Text m_RerollText;

        [SerializeField]
        TMP_Text m_MoneyText;

        [Tooltip("Message après un clic (« Pas assez d'or »…).")]
        [SerializeField]
        TMP_Text m_MessageText;

        [Tooltip("Optionnel : liste des améliorations déjà possédées.")]
        [SerializeField]
        TMP_Text m_OwnedText;

        [SerializeField]
        float m_MessageDuration = 3f;

        [Header("Placement")]
        [Tooltip("Si le joueur est loin du panneau quand la boutique s'ouvre (tombé de la tour, tour détruite), " +
                 "le panneau vient à côté de lui. Sinon, il reste à sa place dans la scène.")]
        [SerializeField]
        bool m_FollowPlayer = true;

        [Tooltip("Distance (m) entre la tête du joueur et le panneau au-delà de laquelle le panneau vient près de lui.")]
        [SerializeField]
        float m_FollowDistance = 3.5f;

        [Tooltip("Position du panneau par rapport à la tête du joueur, quand il vient près de lui (X à droite, Z devant).")]
        [SerializeField]
        Vector3 m_OffsetFromHead = new Vector3(-1.4f, -0.4f, 1.6f);

        [Tooltip("Rotation du panneau par rapport au regard du joueur, quand il vient près de lui.")]
        [SerializeField]
        Vector3 m_RotationFromHead = new Vector3(15f, -41f, 0f);

        [Header("Sons")]
        [SerializeField]
        AudioClip m_BuyClip;

        [SerializeField]
        AudioClip m_ErrorClip;

        [SerializeField]
        AudioClip m_RerollClip;

        [Tooltip("Optionnel : joué quand la boutique s'ouvre.")]
        [SerializeField]
        AudioClip m_OpenClip;

        readonly StringBuilder m_Builder = new StringBuilder();
        ShopManager m_Shop;
        ScoreManager m_Score;
        PlayerUpgrades m_Upgrades;
        float m_MessageTimer;
        bool m_WasOpen;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;

        void Awake()
        {
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;

            foreach (var card in m_UpgradeCards)
            {
                if (card != null)
                    card.Clicked += OnCardClicked;
            }

            if (m_BowCard != null)
                m_BowCard.Clicked += OnCardClicked;
            if (m_TowerCard != null)
                m_TowerCard.Clicked += OnCardClicked;
            if (m_RerollButton != null)
                m_RerollButton.onClick.AddListener(OnRerollClicked);
            if (m_Content != null)
                m_Content.SetActive(false);
        }

        void Start()
        {
            m_Shop = ShopManager.Instance;
            m_Score = ScoreManager.Instance;
            m_Upgrades = PlayerUpgrades.Instance;
            if (m_Shop == null)
            {
                Debug.LogWarning("ShopPanel : aucun Shop Manager dans la scène.", this);
                return;
            }

            m_Shop.Changed += Refresh;
            if (m_Score != null)
                m_Score.Changed += Refresh;
            if (m_Upgrades != null)
                m_Upgrades.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (m_Shop != null)
                m_Shop.Changed -= Refresh;
            if (m_Score != null)
                m_Score.Changed -= Refresh;
            if (m_Upgrades != null)
                m_Upgrades.Changed -= Refresh;
        }

        void Update()
        {
            if (m_MessageTimer <= 0f)
                return;

            m_MessageTimer -= Time.deltaTime;
            if (m_MessageTimer <= 0f && m_MessageText != null)
                m_MessageText.text = "";
        }

        void Refresh()
        {
            var open = m_Shop != null && m_Shop.IsOpen;
            if (m_Content != null)
                m_Content.SetActive(open);

            if (open && !m_WasOpen)
            {
                PlaceForPlayer();
                ShowMessage("", Color.white);
                Sfx.Play(m_OpenClip, transform.position, 0.8f);
            }

            m_WasOpen = open;
            if (!open)
                return;

            var money = m_Score != null ? m_Score.Money : 0;
            var offers = m_Shop.UpgradeOffers;
            for (var i = 0; i < m_UpgradeCards.Length; i++)
            {
                if (m_UpgradeCards[i] != null)
                    m_UpgradeCards[i].Show(i < offers.Count ? offers[i] : null, money);
            }

            if (m_BowCard != null)
                m_BowCard.Show(m_Shop.BowOffer, money);
            if (m_TowerCard != null)
                m_TowerCard.Show(m_Shop.TowerOffer, money);
            if (m_RerollText != null)
                m_RerollText.text = $"Relancer\n{m_Shop.RerollCost} or";
            if (m_MoneyText != null)
                m_MoneyText.text = "Or " + money.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');
            if (m_OwnedText != null)
                m_OwnedText.text = OwnedSummary();
        }

        // À sa place dans la scène si le joueur est en haut de la tour, sinon à côté de lui.
        void PlaceForPlayer()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (!m_FollowPlayer || head == null || Vector3.Distance(head.position, m_HomePosition) <= m_FollowDistance)
            {
                transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
                return;
            }

            var yaw = rig.HeadYaw;
            transform.SetPositionAndRotation(head.position + yaw * m_OffsetFromHead, yaw * Quaternion.Euler(m_RotationFromHead));
        }

        void OnCardClicked(ShopCard card)
        {
            if (m_Shop != null)
                Report(m_Shop.TryBuy(card.Offer), m_BuyClip);
        }

        void OnRerollClicked()
        {
            if (m_Shop != null)
                Report(m_Shop.TryReroll(), m_RerollClip);
        }

        void Report(ShopResult result, AudioClip successClip)
        {
            Sfx.Play(result.Success ? successClip : m_ErrorClip, transform.position, 0.9f);
            ShowMessage(result.Message, result.Color);
        }

        void ShowMessage(string message, Color color)
        {
            if (m_MessageText == null)
                return;

            m_MessageText.text = message;
            m_MessageText.color = color;
            m_MessageTimer = string.IsNullOrEmpty(message) ? 0f : m_MessageDuration;
        }

        // « Améliorations : Dégâts ×2 · Multitir »
        string OwnedSummary()
        {
            if (m_Upgrades == null || m_Upgrades.Owned.Count == 0)
                return "";

            m_Builder.Clear();
            m_Builder.Append("Améliorations : ");
            for (var i = 0; i < m_Upgrades.Owned.Count; i++)
            {
                var upgrade = m_Upgrades.Owned[i];
                if (i > 0)
                    m_Builder.Append(" · ");
                m_Builder.Append(upgrade.displayName);
                var stacks = m_Upgrades.Stacks(upgrade.effect);
                if (stacks > 1)
                    m_Builder.Append(" ×").Append(stacks);
            }

            return m_Builder.ToString();
        }
    }
}
