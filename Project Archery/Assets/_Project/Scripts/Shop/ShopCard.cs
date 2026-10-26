using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.Shop
{
    /// <summary>
    /// Une carte de la boutique : icône, nom, rareté ou type, description courte et prix.
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

        [Tooltip("Couleur du prix quand on n'a pas assez d'or (ou de matériaux).")]
        [SerializeField]
        Color m_TooExpensiveColor = new Color(1f, 0.45f, 0.4f);

        // Le cadre orné du thème des menus (enfant « Bordure »), teinté de la couleur de l'offre.
        Image m_Border;

        public ShopOffer Offer { get; private set; }

        public event Action<ShopCard> Clicked;

        /// <summary>Couleur du fond avant la teinte de l'offre (réglée par le thème des menus).</summary>
        public void SetBaseColor(Color color) => m_BaseColor = color;

        void Awake()
        {
            if (m_Button == null)
                m_Button = GetComponent<Button>();
            if (m_Background == null)
                m_Background = GetComponent<Image>();
            var border = transform.Find(UI.UiThemer.BorderName);
            if (border != null)
                m_Border = border.GetComponent<Image>();
            if (m_Button != null)
                m_Button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        /// <summary>Affiche une offre ; null cache la carte. <paramref name="money"/> : ce qu'on a dans la monnaie de l'offre.</summary>
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
                var free = offer.PriceText != null;
                m_PriceText.text = free ? offer.PriceText : offer.Sold ? "Acheté" : available ? offer.Price + " " + offer.PriceUnit : "";
                m_PriceText.color = !free && available && money < offer.Price ? m_TooExpensiveColor : Color.white;
            }

            // Mise en avant (l'emplacement choisi dans l'atelier) : deux fois plus de couleur, cadre blanc.
            if (m_Background != null)
            {
                var color = Color.Lerp(m_BaseColor, offer.Color, offer.Highlight ? Mathf.Min(1f, m_Tint * 2f) : m_Tint);
                color.a = m_BaseColor.a * (available || offer.Highlight ? 1f : 0.6f);
                m_Background.color = color;
            }

            if (m_Border != null)
            {
                var color = Color.Lerp(offer.Color, Color.white, offer.Highlight ? 0.6f : 0.15f);
                color.a = available || offer.Highlight ? 1f : 0.45f;
                m_Border.color = color;
            }

            // L'icône, de la couleur de l'offre un peu éclaircie ; sans icône, la place se libère pour le texte.
            if (m_Icon != null)
            {
                m_Icon.sprite = offer.Icon;
                if (m_Icon.gameObject.activeSelf != (offer.Icon != null))
                    m_Icon.gameObject.SetActive(offer.Icon != null);
                var color = Color.Lerp(offer.Color, Color.white, 0.2f);
                color.a = available ? 1f : 0.45f;
                m_Icon.color = color;
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
