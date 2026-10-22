using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.Grenades
{
    /// <summary>
    /// Affiche la recharge de la grenade sur la montre au poignet : les secondes restantes,
    /// et une jauge qui se remplit. Comme <see cref="Archery.UI.HudDisplay"/>, tous les champs
    /// sont optionnels : on ne remplit que ce que la montre contient.
    /// </summary>
    public class GrenadeWatchUI : MonoBehaviour
    {
        [Tooltip("Secondes restantes, ou le texte « prêt ».")]
        [SerializeField]
        TMP_Text m_Text;

        [Tooltip("Optionnel : Image de type « Filled » (Radial 360) qui se remplit pendant la recharge.")]
        [SerializeField]
        Image m_Bar;

        [Tooltip("Optionnel : icône de la grenade, grisée pendant la recharge.")]
        [SerializeField]
        Image m_Icon;

        [Header("Textes")]
        [Tooltip("Affiché quand la grenade est disponible.")]
        [SerializeField]
        string m_ReadyText = "Grenade prête";

        [Tooltip("Affiché pendant la recharge. {0} = secondes restantes.")]
        [SerializeField]
        string m_CooldownFormat = "Grenade {0} s";

        [Tooltip("Affiché pendant que la grenade est en main.")]
        [SerializeField]
        string m_HeldText = "Grenade en main";

        [Header("Couleurs")]
        [SerializeField]
        Color m_ReadyColor = new Color(0.45f, 1f, 0.5f);

        [SerializeField]
        Color m_CooldownColor = new Color(1f, 0.6f, 0.2f);

        // Dernière valeur affichée : on ne reconstruit le texte que quand une seconde change.
        int m_Seconds = -2;

        void Update()
        {
            var belt = GrenadeBelt.Instance;
            if (belt == null)
            {
                Apply(-1, 1f, true);
                return;
            }

            var seconds = belt.Remaining > 0f ? Mathf.CeilToInt(belt.Remaining) : (belt.IsReady ? -1 : 0);
            Apply(seconds, belt.Progress, belt.Remaining <= 0f);
        }

        void Apply(int seconds, float progress, bool ready)
        {
            if (m_Bar != null)
            {
                m_Bar.fillAmount = Mathf.Clamp01(progress);
                m_Bar.color = ready ? m_ReadyColor : m_CooldownColor;
            }

            if (m_Icon != null)
                m_Icon.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.35f);

            if (m_Text == null || seconds == m_Seconds)
                return;

            m_Seconds = seconds;
            m_Text.text = seconds switch
            {
                < 0 => m_ReadyText,
                0 => m_HeldText,
                _ => string.Format(m_CooldownFormat, seconds),
            };
            m_Text.color = ready ? m_ReadyColor : m_CooldownColor;
        }
    }
}
