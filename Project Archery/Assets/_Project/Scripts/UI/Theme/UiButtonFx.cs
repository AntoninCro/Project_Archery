using Archery.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Archery.UI
{
    /// <summary>
    /// Un bouton du thème qui réagit : visé par le rayon de la main, il grossit un peu, sa bordure s'éclaire et son fond
    /// s'avive, avec un petit son ; cliqué, un son selon son rôle (valider, revenir, autre).
    /// </summary>
    /// <remarks>Ajouté par <see cref="UiThemer"/> ; les couleurs de repos sont celles du bouton quand on commence à le viser.</remarks>
    [DisallowMultipleComponent]
    public class UiButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField]
        Image m_Fill;

        [SerializeField]
        Image m_Border;

        [Tooltip("Le fond s'avive quand on vise le bouton (non pour les cartes : leur couleur change avec l'offre).")]
        [SerializeField]
        bool m_BrightenFill = true;

        [SerializeField]
        float m_HoverScale = 1.05f;

        [SerializeField]
        Color m_HoverBorder = new Color(1f, 1f, 0.92f);

        [SerializeField]
        AudioClip m_HoverClip;

        [SerializeField]
        AudioClip m_ClickClip;

        [SerializeField]
        float m_HoverVolume = 0.35f;

        [SerializeField]
        float m_ClickVolume = 0.7f;

        Selectable m_Selectable;
        Vector3 m_BaseScale = Vector3.one;
        Color m_RestFill;
        Color m_RestBorder;
        float m_Hover;
        bool m_Hovered;

        /// <summary>Réglé par le thème.</summary>
        public void Configure(Image fill, Image border, bool brightenFill, float hoverScale, Color hoverBorder,
                              AudioClip hoverClip, float hoverVolume, AudioClip clickClip, float clickVolume)
        {
            m_Fill = fill;
            m_Border = border;
            m_BrightenFill = brightenFill;
            m_HoverScale = hoverScale;
            m_HoverBorder = hoverBorder;
            m_HoverClip = hoverClip;
            m_HoverVolume = hoverVolume;
            m_ClickClip = clickClip;
            m_ClickVolume = clickVolume;
        }

        void Awake()
        {
            m_Selectable = GetComponent<Selectable>();
            m_BaseScale = transform.localScale;
        }

        void OnDisable()
        {
            if (m_Hovered)
                Rest();
            m_Hovered = false;
            m_Hover = 0f;
            transform.localScale = m_BaseScale;
        }

        bool Usable => m_Selectable == null || m_Selectable.IsInteractable();

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!Usable || m_Hovered)
                return;

            // Les couleurs de repos du moment (une carte change de couleur avec son offre).
            if (m_Hover <= 0f)
            {
                if (m_Fill != null)
                    m_RestFill = m_Fill.color;
                if (m_Border != null)
                    m_RestBorder = m_Border.color;
            }

            m_Hovered = true;
            Sfx.Play(m_HoverClip, transform.position, m_HoverVolume, Random.Range(0.97f, 1.05f));
        }

        public void OnPointerExit(PointerEventData eventData) => m_Hovered = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Usable)
                Sfx.Play(m_ClickClip, transform.position, m_ClickVolume, Random.Range(0.97f, 1.03f));
        }

        void Update()
        {
            var target = m_Hovered && Usable ? 1f : 0f;
            if (Mathf.Approximately(m_Hover, target))
                return;

            m_Hover = Mathf.MoveTowards(m_Hover, target, Time.unscaledDeltaTime * 8f);
            var eased = Mathf.SmoothStep(0f, 1f, m_Hover);
            transform.localScale = m_BaseScale * Mathf.Lerp(1f, m_HoverScale, eased);
            if (m_Border != null)
                m_Border.color = Color.Lerp(m_RestBorder, m_HoverBorder, eased);
            if (m_Fill != null && m_BrightenFill)
            {
                var bright = m_RestFill * 1.3f;
                bright.a = m_RestFill.a;
                m_Fill.color = Color.Lerp(m_RestFill, bright, eased);
            }

            if (m_Hover <= 0f)
                Rest();
        }

        void Rest()
        {
            if (m_Border != null && m_Hover > 0f)
                m_Border.color = m_RestBorder;
            if (m_Fill != null && m_BrightenFill && m_Hover > 0f)
                m_Fill.color = m_RestFill;
        }
    }
}
