using Archery.Building;
using Archery.Core;
using Archery.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.UI
{
    /// <summary>
    /// Bouton « Plans » de la montre (refonte du tower defense, étape 3, 27 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, partie 3) :
    /// on le touche avec l'autre main et on reste un court instant dessus, comme le bouton « Tour », et un plan sort de la
    /// montre : son hologramme suit le rayon de la main qui porte la montre (<see cref="BuildPlacement"/>). Retouché pendant la
    /// pose, il l'annule. Visible seulement quand le joueur a des plans.
    /// </summary>
    /// <remarks>
    /// Fabriqué au lancement par <see cref="BuildPlacement"/>, en copiant le bouton « Tour » (son fond, son remplissage, son
    /// texte) à côté de lui : rien à poser dans la scène. Son rayon de toucher est plus petit que celui du bouton « Tour », et
    /// les deux boutons sont assez écartés pour qu'on ne déclenche pas l'un en touchant l'autre.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class WatchPlansButton : MonoBehaviour
    {
        [Tooltip("Distance (m) entre la manette de l'autre main et le centre du bouton pour le toucher.")]
        [SerializeField]
        float m_TouchRadius = 0.045f;

        [Tooltip("Temps (s) à rester sur le bouton pour l'activer.")]
        [SerializeField]
        float m_HoldTime = 0.3f;

        [Tooltip("Couleur du fond pendant la pose (le bouton annule alors).")]
        [SerializeField]
        Color m_ActiveColor = new Color(1f, 0.72f, 0.38f, 1f);

        GameObject m_Visual;
        Image m_Background;
        Image m_Fill;
        TMP_Text m_Label;
        Color m_ReadyColor = new Color(0.6f, 0.82f, 1f, 1f);
        AudioClip m_PressClip;
        AudioClip m_DeniedClip;

        RectTransform m_Rect;
        float m_Hold;
        bool m_Armed = true;
        int m_LastPlans = -1;
        bool m_LastActive;

        /// <summary>
        /// Copie le bouton « Tour » à côté de lui, à <paramref name="worldGap"/> m (de centre à centre), et en fait le bouton « Plans ».
        /// </summary>
        public static WatchPlansButton Create(WatchTowerButton tower, float worldGap)
        {
            if (tower == null)
                return null;

            // Copié sous un parent désactivé : le bouton « Tour » de la copie est retiré avant de se réveiller.
            var holder = new GameObject("Bouton Plans (montage)");
            holder.SetActive(false);
            var copy = Instantiate(tower.gameObject, holder.transform);
            var towerCopy = copy.GetComponent<WatchTowerButton>();
            var button = copy.AddComponent<WatchPlansButton>();
            button.m_Visual = towerCopy.Visual != copy ? towerCopy.Visual : null;
            button.m_Background = towerCopy.Background;
            button.m_Fill = towerCopy.Fill;
            button.m_Label = towerCopy.Label;
            button.m_ReadyColor = towerCopy.ReadyColor;
            button.m_PressClip = towerCopy.PressClip;
            button.m_DeniedClip = towerCopy.DeniedClip;
            DestroyImmediate(towerCopy);

            copy.name = "Bouton Plans";
            copy.transform.SetParent(tower.transform.parent, false);
            var rect = (RectTransform)copy.transform;
            var source = (RectTransform)tower.transform;
            var scale = Mathf.Max(1e-5f, Mathf.Abs(source.lossyScale.x));
            rect.anchoredPosition = source.anchoredPosition + new Vector2(-worldGap / scale, 0f);
            Destroy(holder);
            return button;
        }

        void Awake() => m_Rect = (RectTransform)transform;

        void Update()
        {
            var placement = BuildPlacement.Instance;
            var builds = BuildManager.Instance;
            var plans = builds != null ? builds.TotalPlans : 0;
            var active = placement != null && placement.IsActive;

            // Caché sans plan, et comme le bouton « Tour », dans l'arène du dragon, la dimension du portail et sur le toit cyberpunk.
            var visible = placement != null && (plans > 0 || active) && !FinalBoss.FinalBossDirector.PlayerInArena &&
                          !Portal.StarPortal.PlayerInDimension && !Cyber.CyberPassage.PlayerInDimension;
            if (m_Visual != null && m_Visual.activeSelf != visible)
                m_Visual.SetActive(visible);
            if (!visible)
            {
                m_Hold = 0f;
                SetFill(0f);
                return;
            }

            UpdateLook(plans, active);

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

            m_Hold += Time.deltaTime;
            SetFill(m_Hold / Mathf.Max(0.01f, m_HoldTime));
            if (m_Hold < m_HoldTime)
                return;

            m_Armed = false;
            SetFill(0f);
            Press(hand, placement);
        }

        void Press(XRBaseInputInteractor hand, BuildPlacement placement)
        {
            var done = placement.IsActive ? placement.Cancel("Pose annulée") : placement.BeginFromWatch(PointerHand());
            Haptics.Pulse(hand, done ? 0.5f : 0.15f, done ? 0.08f : 0.05f);
            Sfx.Play(done ? m_PressClip : m_DeniedClip, transform.position, done ? 0.8f : 0.6f, 1f, 0f);
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

        // La main qui porte la montre : c'est elle qui vise pour poser (l'autre tient souvent l'arc). Sinon, la main libre.
        XRBaseInputInteractor PointerHand()
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
                return null;

            foreach (var hand in rig.Hands)
            {
                if (hand != null && transform.IsChildOf(hand.transform))
                    return hand;
            }

            var free = rig.BowHand == UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Left
                ? UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Right
                : UnityEngine.XR.Interaction.Toolkit.Interactors.InteractorHandedness.Left;
            return rig.TryGetHand(free, out var freeHand) ? freeHand : null;
        }

        void UpdateLook(int plans, bool active)
        {
            if (m_Background != null)
                m_Background.color = active ? m_ActiveColor : m_ReadyColor;

            if (m_Label == null || (plans == m_LastPlans && active == m_LastActive))
                return;

            m_LastPlans = plans;
            m_LastActive = active;
            m_Label.text = active ? "Annuler" : $"Plans {plans}";
        }

        void SetFill(float amount)
        {
            if (m_Fill != null)
                m_Fill.fillAmount = Mathf.Clamp01(amount);
        }

        void OnDrawGizmosSelected()
        {
            var rect = (RectTransform)transform;
            Gizmos.color = new Color(1f, 0.72f, 0.38f, 0.6f);
            Gizmos.DrawWireSphere(rect.TransformPoint(rect.rect.center), m_TouchRadius);
        }
    }
}
