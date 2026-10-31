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
    /// on le touche avec l'autre main et on reste un court instant dessus, comme le bouton « Tour », et le menu des plans s'ouvre
    /// (28 octobre : un carrousel de cartes, <see cref="BuildMenu"/>) ; le bâtiment de la carte du milieu suit aussitôt le rayon
    /// de la main qui porte la montre (<see cref="BuildPlacement"/>). Retouché, il ferme le menu et range le plan. Visible
    /// seulement quand le joueur a des plans.
    /// </summary>
    /// <remarks>
    /// Fabriqué au lancement par <see cref="BuildPlacement"/>, en copiant le bouton « Tour » (son fond, son remplissage, son
    /// texte) : rien à poser dans la scène. Depuis le 29 octobre (demandé par le joueur), il est au-dessus du cadran, en reflet
    /// du bouton « Tour » qui est dessous ; avant, à sa gauche, en bas à gauche de la montre. Son rayon de toucher est plus petit
    /// que celui du bouton « Tour », et les deux boutons sont assez écartés pour qu'on ne déclenche pas l'un en touchant l'autre.
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
        bool m_LastMenu;

        /// <summary>
        /// Copie le bouton « Tour », de l'autre côté du cadran (au-dessus), à <paramref name="worldGap"/> m de lui au moins (de
        /// centre à centre), et en fait le bouton « Plans ».
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
            var scale = Mathf.Max(1e-5f, Mathf.Abs(source.lossyScale.y));

            // Le reflet du bouton « Tour » par rapport au milieu du cadran : il est 42 points sous le bord du bas, celui-ci 42 points
            // au-dessus du bord du haut.
            rect.anchorMin = new Vector2(source.anchorMin.x, 1f - source.anchorMax.y);
            rect.anchorMax = new Vector2(source.anchorMax.x, 1f - source.anchorMin.y);
            rect.pivot = new Vector2(source.pivot.x, 1f - source.pivot.y);
            rect.anchoredPosition = new Vector2(source.anchoredPosition.x, -source.anchoredPosition.y);
            var gap = Vector3.Distance(rect.TransformPoint(rect.rect.center), source.TransformPoint(source.rect.center));
            if (gap < worldGap)
                rect.anchoredPosition += new Vector2(0f, (worldGap - gap) / scale);
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

            UpdateLook(plans, active, placement.IsMenuOpen);

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

        // Le menu des plans s'ouvre, l'hologramme du bâtiment du milieu déjà sorti (28 octobre au soir) ; retouché, le bouton
        // ferme le menu et range le plan, ou abandonne un déplacement.
        void Press(XRBaseInputInteractor hand, BuildPlacement placement)
        {
            var done = placement.IsActive ? placement.Cancel(placement.IsMoving ? "Déplacement annulé" : null) : placement.OpenMenu(PointerHand());
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

        void UpdateLook(int plans, bool active, bool menu)
        {
            if (m_Background != null)
                m_Background.color = active ? m_ActiveColor : m_ReadyColor;

            if (m_Label == null || (plans == m_LastPlans && active == m_LastActive && menu == m_LastMenu))
                return;

            m_LastPlans = plans;
            m_LastActive = active;
            m_LastMenu = menu;
            m_Label.text = menu ? "Fermer" : active ? "Annuler" : $"Plans {plans}";
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
