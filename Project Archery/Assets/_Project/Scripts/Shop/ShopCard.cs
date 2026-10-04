using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.Shop
{
    /// <summary>
    /// Une carte de la boutique : nom, rareté ou type, description et prix.
    /// Un clic dessus (rayon de la manette + gâchette) l'achète.
    /// </summary>
    /// <remarks>À placer sur un Button d'un Canvas ; tous les textes sont optionnels.</remarks>
    [DisallowMultipleComponent]
    public class ShopCard : MonoBehaviour
    {
        [Tooltip("Vide : le Button de cet objet.")]
        [SerializeField]
        Button m_Button;

        [Tooltip("Fond teinté par la couleur de l'offre. Vide : l'Image de cet objet.")]
        [SerializeField]
        Image m_Background;

        [SerializeField]
        TMP_Text m_TitleText;

        [Tooltip("Rareté, ou type de l'offre (arc, tour).")]
        [SerializeField]
        TMP_Text m_SubtitleText;

        [SerializeField]
        TMP_Text m_DescriptionText;

        [SerializeField]
        TMP_Text m_PriceText;

        [Tooltip("Optionnel : icône de l'amélioration.")]
        [SerializeField]
        Image m_Icon;

        [Tooltip("Couleur du fond avant la teinte de l'offre.")]
        [SerializeField]
        Color m_BaseColor = new Color(0.12f, 0.12f, 0.14f, 0.95f);

        [Tooltip("Part de la couleur de l'offre dans le fond.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_Tint = 0.3f;

        [Tooltip("Couleur du prix quand on n'a pas assez d'or.")]
        [SerializeField]
        Color m_TooExpensiveColor = new Color(1f, 0.45f, 0.4f);

        public ShopOffer Offer { get; private set; }

        public event Action<ShopCard> Clicked;

        void Awake()
        {
            if (m_Button == null)
                m_Button = GetComponent<Button>();
            if (m_Background == null)
                m_Background = GetComponent<Image>();
            if (m_Button != null)
                m_Button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        /// <summary>Affiche une offre ; null cache la carte.</summary>
        public void Show(ShopOffer offer, int money)
        {
            Offer = offer;
            gameObject.SetActive(offer != null);
            if (offer == null)
                return;

            var available = offer.CanBuy;
            SetText(m_TitleText, offer.Title, offer.Color);
            SetText(m_SubtitleText, offer.Subtitle, offer.Color);
            SetText(m_DescriptionText, offer.Description, Color.white);

            if (m_PriceText != null)
            {
                m_PriceText.text = offer.Sold ? "Acheté" : available ? offer.Price + " or" : "";
                m_PriceText.color = available && money < offer.Price ? m_TooExpensiveColor : Color.white;
            }

            if (m_Background != null)
            {
                var color = Color.Lerp(m_BaseColor, offer.Color, m_Tint);
                color.a = m_BaseColor.a * (available ? 1f : 0.6f);
                m_Background.color = color;
            }

            if (m_Icon != null)
            {
                m_Icon.sprite = offer.Icon;
                m_Icon.enabled = offer.Icon != null;
            }

            if (m_Button != null)
                m_Button.interactable = available;
        }

        static void SetText(TMP_Text text, string value, Color color)
        {
            if (text == null)
                return;

            text.text = value;
            text.color = color;
        }
    }
}
