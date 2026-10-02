using System.Text;
using Archery.Bows;
using Archery.Combat;
using UnityEngine;

namespace Archery.UI
{
    /// <summary>
    /// Affiche les dégâts reçus au-dessus du point d'impact (« 20 », « Headshot ! », « Parfait ! »).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class DamagePopups : MonoBehaviour
    {
        [SerializeField]
        Color m_BodyColor = Color.white;

        [SerializeField]
        Color m_HeadColor = new Color(1f, 0.85f, 0.2f);

        [SerializeField]
        ShotTuning m_ShotTuning;

        Health m_Health;
        readonly StringBuilder m_Builder = new StringBuilder();

        void Awake() => m_Health = GetComponent<Health>();

        void OnEnable() => m_Health.Damaged += OnDamaged;

        void OnDisable() => m_Health.Damaged -= OnDamaged;

        void OnDamaged(Health health, DamageInfo info)
        {
            var tuning = m_ShotTuning != null ? m_ShotTuning : ShotTuning.Fallback;
            m_Builder.Clear();
            m_Builder.Append(Mathf.RoundToInt(info.Amount));
            if (info.Zone == HitZone.Head)
                m_Builder.Append("\nHeadshot !");
            if (info.Grade == ShotGrade.Perfect)
                m_Builder.Append('\n').Append(tuning.Get(ShotGrade.Perfect).label);

            var color = info.Zone == HitZone.Head ? m_HeadColor : m_BodyColor;
            FloatingText.Spawn(info.Point + Vector3.up * 0.2f, m_Builder.ToString(), color);
        }
    }
}
