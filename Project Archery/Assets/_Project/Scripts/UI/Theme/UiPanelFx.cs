using UnityEngine;

namespace Archery.UI
{
    /// <summary>Un panneau du thème qui apparaît en douceur : il grandit un peu et se dévoile quand il s'affiche.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public class UiPanelFx : MonoBehaviour
    {
        [SerializeField]
        float m_Duration = 0.25f;

        [SerializeField]
        float m_StartScale = 0.94f;

        CanvasGroup m_Group;
        Vector3 m_BaseScale = Vector3.one;
        float m_Time = 1f;

        void Awake()
        {
            m_Group = GetComponent<CanvasGroup>();
            m_BaseScale = transform.localScale;
        }

        void OnEnable()
        {
            m_Time = 0f;
            Apply(0f);
        }

        void OnDisable()
        {
            m_Time = 1f;
            Apply(1f);
        }

        void Update()
        {
            if (m_Time >= 1f)
                return;

            m_Time = Mathf.Min(1f, m_Time + Time.unscaledDeltaTime / Mathf.Max(0.01f, m_Duration));
            Apply(m_Time);
        }

        void Apply(float t)
        {
            // Un léger dépassement à la fin, comme un parchemin qui se déplie.
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            var overshoot = Mathf.Sin(t * Mathf.PI) * 0.015f;
            transform.localScale = m_BaseScale * (Mathf.Lerp(m_StartScale, 1f, eased) + overshoot);
            if (m_Group != null)
                m_Group.alpha = Mathf.Clamp01(t * 1.6f);
        }
    }
}
