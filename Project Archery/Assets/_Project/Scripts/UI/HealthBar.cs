using Archery.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.UI
{
    /// <summary>
    /// Barre de PV dans le monde, au-dessus d'un ennemi (le boss…) : une Image « Filled » dans un Canvas
    /// en World Space, toujours tournée vers le joueur. Elle se cache à la mort, et revient si les PV remontent
    /// (barricade réparée).
    /// </summary>
    [DisallowMultipleComponent]
    public class HealthBar : MonoBehaviour
    {
        [Tooltip("PV suivis. Vide : ceux d'un parent.")]
        [SerializeField]
        Health m_Health;

        [Tooltip("Image de la partie remplie, étirée sur toute la barre : sa largeur suit les PV. " +
                 "Une Image de type « Filled » marche aussi.")]
        [SerializeField]
        Image m_Fill;

        [Tooltip("Couleur de la barre pleine.")]
        [SerializeField]
        Color m_FullColor = new Color(0.9f, 0.18f, 0.12f);

        [Tooltip("Couleur de la barre presque vide.")]
        [SerializeField]
        Color m_LowColor = new Color(1f, 0.75f, 0.2f);

        [SerializeField]
        bool m_FaceCamera = true;

        Camera m_Camera;
        Canvas m_Canvas;

        void Awake()
        {
            if (m_Health == null)
                m_Health = GetComponentInParent<Health>();
            m_Canvas = GetComponent<Canvas>();
        }

        void LateUpdate()
        {
            if (m_Health == null)
                return;

            if (!m_Health.IsAlive)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            if (m_Fill != null)
            {
                var amount = m_Health.Normalized;
                if (m_Fill.type == Image.Type.Filled)
                {
                    m_Fill.fillAmount = amount;
                }
                else
                {
                    // Sans sprite, pas de type « Filled » : on raccourcit l'image par la droite.
                    var rect = m_Fill.rectTransform;
                    rect.anchorMax = new Vector2(Mathf.Lerp(rect.anchorMin.x, 1f, amount), rect.anchorMax.y);
                }

                m_Fill.color = Color.Lerp(m_LowColor, m_FullColor, amount);
            }

            if (!m_FaceCamera)
                return;

            if (m_Camera == null)
                m_Camera = Camera.main;
            if (m_Camera != null)
            {
                var away = transform.position - m_Camera.transform.position;
                if (away.sqrMagnitude > 1e-4f)
                    transform.rotation = Quaternion.LookRotation(away, Vector3.up);
            }
        }

        // Le Canvas se cache sans désactiver l'objet : LateUpdate continue, et peut la réafficher.
        void SetVisible(bool visible)
        {
            if (m_Canvas != null)
                m_Canvas.enabled = visible;
            else if (!visible)
                gameObject.SetActive(false);
        }
    }
}
