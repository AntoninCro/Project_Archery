using Archery.Core;
using Archery.Defense;
using Archery.Player;
using Archery.Waves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.UI
{
    /// <summary>
    /// Bouton « Tour » de la montre (GDD, section 3 bis) : on le touche avec l'autre main et on reste un court instant
    /// dessus, et le joueur est téléporté en haut de la tour, avec un fondu. Pendant les vagues, il se recharge
    /// (25 s par défaut) ; entre les vagues, il est libre, comme le bouton de la boutique. Grisé tant que la tour est
    /// détruite, caché quand le joueur y est déjà.
    /// </summary>
    /// <remarks>
    /// On le touche plutôt qu'on ne le clique : la montre est au poignet de la main libre, et l'autre main tient
    /// souvent l'arc, ce qui l'empêche de cliquer au rayon. Le temps d'appui évite les appuis par accident.
    /// À placer sur un objet du Canvas de la montre ; l'apparence (fond, remplissage, texte) est dans « Visual ».
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class WatchTowerButton : MonoBehaviour
    {
        [Tooltip("Téléporteur du pied de la tour. Vide : cherché dans la scène.")]
        [SerializeField]
        TowerTeleporter m_Teleporter;

        [Tooltip("Temps de recharge (s) après une téléportation pendant une vague.")]
        [SerializeField]
        float m_Cooldown = 25f;

        [Tooltip("Distance (m) entre la manette de l'autre main et le centre du bouton pour le toucher.")]
        [SerializeField]
        float m_TouchRadius = 0.07f;

        [Tooltip("Temps (s) à rester sur le bouton pour l'activer.")]
        [SerializeField]
        float m_HoldTime = 0.3f;

        [Header("Apparence")]
        [Tooltip("Ce qui s'affiche (fond, remplissage, texte) : caché quand le joueur est déjà en haut de la tour.")]
        [SerializeField]
        GameObject m_Visual;

        [SerializeField]
        Image m_Background;

        [Tooltip("Optionnel : image remplie (Image Type « Filled ») pendant l'appui.")]
        [SerializeField]
        Image m_Fill;

        [SerializeField]
        TMP_Text m_Label;

        [SerializeField]
        Color m_ReadyColor = new Color(0.6f, 0.82f, 1f, 1f);

        [SerializeField]
        Color m_DisabledColor = new Color(0.45f, 0.45f, 0.5f, 0.8f);

        [Header("Sons")]
        [SerializeField]
        AudioClip m_PressClip;

        [SerializeField]
        AudioClip m_DeniedClip;

        RectTransform m_Rect;
        float m_CooldownLeft;
        float m_Hold;
        bool m_Armed = true;
        string m_LastLabel;

        void Awake() => m_Rect = (RectTransform)transform;

        void Start()
        {
            if (m_Teleporter == null)
                m_Teleporter = FindAnyObjectByType<TowerTeleporter>();
            if (m_Teleporter == null)
                Debug.LogWarning("WatchTowerButton : aucun téléporteur de tour dans la scène.", this);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            if (m_CooldownLeft > 0f)
                m_CooldownLeft = Mathf.Max(0f, m_CooldownLeft - deltaTime);

            // Caché aussi dans l'arène du boss final : on n'en ressort qu'en battant le dragon.
            var visible = m_Teleporter != null && !m_Teleporter.IsPlayerAtDestination && !FinalBoss.FinalBossDirector.PlayerInArena;
            if (m_Visual != null && m_Visual.activeSelf != visible)
                m_Visual.SetActive(visible);
            if (!visible)
            {
                m_Hold = 0f;
                return;
            }

            var usable = m_Teleporter.IsUsable && m_CooldownLeft <= 0f;
            UpdateLook(usable);

            var hand = TouchingHand();
            if (hand == null)
            {
                m_Hold = 0f;
                m_Armed = true;
                SetFill(0f);
                return;
            }

            if (!m_Armed)
                return;

            m_Hold += deltaTime;
            SetFill(m_Hold / Mathf.Max(0.01f, m_HoldTime));
            if (m_Hold < m_HoldTime)
                return;

            m_Armed = false;
            SetFill(0f);
            Press(hand, usable);
        }

        void Press(XRBaseInputInteractor hand, bool usable)
        {
            if (!usable || !m_Teleporter.SendPlayer())
            {
                Haptics.Pulse(hand, 0.15f, 0.05f);
                Sfx.Play(m_DeniedClip, transform.position, 0.6f, 1f, 0f);
                return;
            }

            var waves = WaveManager.Instance;
            m_CooldownLeft = waves != null && waves.Phase == WavePhase.Wave ? m_Cooldown : 0f;
            Haptics.Pulse(hand, 0.5f, 0.08f);
            Sfx.Play(m_PressClip, transform.position, 0.8f, 1f, 0f);
        }

        // La manette de l'autre main (pas celle qui porte la montre) touche le bouton.
        XRBaseInputInteractor TouchingHand()
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
                return null;

            var center = m_Rect.TransformPoint(m_Rect.rect.center);
            foreach (var hand in rig.Hands)
            {
                if (hand == null || !hand.isActiveAndEnabled || transform.IsChildOf(hand.transform))
                    continue;
                if ((hand.transform.position - center).sqrMagnitude <= m_TouchRadius * m_TouchRadius)
                    return hand;
            }

            return null;
        }

        void UpdateLook(bool usable)
        {
            if (m_Background != null)
                m_Background.color = usable ? m_ReadyColor : m_DisabledColor;

            if (m_Label == null)
                return;

            var text = !m_Teleporter.IsUsable ? "Tour détruite"
                : m_CooldownLeft > 0f ? $"Tour {Mathf.CeilToInt(m_CooldownLeft)} s"
                : "Tour";
            if (text == m_LastLabel)
                return;

            m_LastLabel = text;
            m_Label.text = text;
        }

        void SetFill(float amount)
        {
            if (m_Fill != null)
                m_Fill.fillAmount = Mathf.Clamp01(amount);
        }

        void OnDrawGizmosSelected()
        {
            var rect = (RectTransform)transform;
            Gizmos.color = new Color(0.6f, 0.82f, 1f, 0.6f);
            Gizmos.DrawWireSphere(rect.TransformPoint(rect.rect.center), m_TouchRadius);
        }
    }
}
