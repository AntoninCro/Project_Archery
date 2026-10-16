using Archery.Player;
using Archery.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Chests
{
    /// <summary>
    /// Une orbe qui flotte au-dessus d'un coffre ouvert (GDD, section 13). On la prend en la visant et en appuyant sur
    /// la gâchette de prise, à quelques mètres au plus, ou de près à la main : sa récompense est donnée, les autres
    /// orbes du coffre disparaissent.
    /// </summary>
    /// <remarks>
    /// Elle bouge en temps réel : elle reste vive pendant le ralenti. Son texte se tourne vers le joueur.
    /// </remarks>
    public class ChestOrb : XRBaseInteractable
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Sphère colorée selon la récompense.")]
        [SerializeField]
        Renderer m_Renderer;

        [Tooltip("Au-dessus de l'orbe : « Permanent » ou « Temporaire », puis le nom de la récompense.")]
        [SerializeField]
        TMP_Text m_Label;

        [Tooltip("Distance maximale (m) entre la main et l'orbe pour la prendre en la visant.")]
        [SerializeField]
        float m_MaxPickDistance = 6f;

        [Tooltip("Durée (s) de la montée hors du coffre.")]
        [SerializeField]
        float m_RiseTime = 0.5f;

        [Tooltip("Amplitude (m) du flottement.")]
        [SerializeField]
        float m_BobAmplitude = 0.03f;

        [Tooltip("Taille de l'orbe survolée par la main, par rapport à sa taille normale.")]
        [SerializeField]
        float m_HoverScale = 1.25f;

        Chest m_Chest;
        MaterialPropertyBlock m_Block;

        // Le thème des menus : une plaque ornée derrière l'étiquette, un sceau qui tourne derrière l'orbe.
        UiTheme m_Theme;
        Image m_Seal;
        Color m_SealColor;

        // L'icône de la récompense (celle de la boutique), à la place de la boule.
        Image m_Icon;
        Color m_IconColor;
        float m_Hover;
        Vector3 m_From;
        Vector3 m_To;
        Vector3 m_BaseScale;
        float m_Age;
        float m_Phase;
        bool m_Leaving;
        float m_LeaveTime;

        public ChestReward Reward { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            m_BaseScale = transform.localScale;
            m_Phase = Random.Range(0f, 6f);
        }

        /// <summary>Prépare l'orbe : sa récompense, sa couleur, et son trajet depuis le coffre.</summary>
        public void Setup(Chest chest, ChestReward reward, Vector3 from, Vector3 to)
        {
            m_Chest = chest;
            Reward = reward;
            m_From = from;
            m_To = to;
            transform.position = from;

            if (m_Renderer != null)
            {
                m_Block ??= new MaterialPropertyBlock();
                m_Renderer.GetPropertyBlock(m_Block);
                m_Block.SetColor(k_BaseColorId, reward.Color);
                m_Block.SetColor(k_EmissionColorId, reward.Color * 1.5f);
                m_Renderer.SetPropertyBlock(m_Block);
            }

            if (m_Label != null)
            {
                var kind = reward.IsPermanent ? "PERMANENT" : "TEMPORAIRE";
                m_Label.text = $"<size=55%><color=#FFFFFFC0>{kind}</color></size>\n<b>{reward.Title}</b>\n<size=70%>{reward.Subtitle}</size>";
                m_Label.color = reward.Color;
            }

            ApplyTheme(reward.Color);
            ShowIcon(reward);
        }

        // L'icône de la boutique remplace la boule : au même endroit, dans le sceau, tournée vers le joueur avec
        // l'étiquette. Sans icône (morceau d'arc légendaire), la boule reste.
        void ShowIcon(ChestReward reward)
        {
            if (reward.Icon == null || m_Label == null)
                return;

            var orb = m_Label.transform.InverseTransformPoint(transform.position);
            m_IconColor = Color.Lerp(reward.Color, Color.white, 0.2f);
            m_Icon = UiThemer.WorldImage(m_Label.transform, "Icône", reward.Icon, 0.2f, new Vector3(orb.x, orb.y, 0.04f), m_IconColor);
            if (m_Renderer != null)
                m_Renderer.enabled = false;
        }

        // L'étiquette en Cinzel sur une plaque ornée de la couleur de la récompense ; derrière l'orbe, un sceau orné.
        void ApplyTheme(Color color)
        {
            m_Theme ??= UiTheme.Load();
            if (m_Theme == null)
                return;

            if (m_Label != null)
            {
                UiThemer.ApplyLabel(m_Label, m_Theme);
                UiThemer.InFrontOfPlaques(m_Label);
                m_Label.ForceMeshUpdate();
                var bounds = m_Label.textBounds;
                var border = Color.Lerp(color, Color.white, 0.15f);
                UiThemer.Plaque(m_Label.transform, "Plaque", m_Theme, (Vector2)bounds.size + new Vector2(0.12f, 0.08f),
                                new Vector3(bounds.center.x, bounds.center.y, 0.01f), border);

                // Le sceau, au niveau de l'orbe (sous l'étiquette), un peu derrière elle : il tourne avec l'étiquette vers le joueur.
                var orb = m_Label.transform.InverseTransformPoint(transform.position);
                m_SealColor = Color.Lerp(color, m_Theme.panelBorderColor, 0.35f);
                m_Seal = UiThemer.Seal(m_Label.transform, "Sceau", m_Theme, 0.3f, new Vector3(orb.x, orb.y, 0.1f), m_SealColor);
            }
        }

        void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            m_Age += deltaTime;

            if (m_Leaving)
            {
                // Elle rétrécit puis disparaît.
                m_LeaveTime += deltaTime;
                transform.localScale = m_BaseScale * Mathf.Max(0f, 1f - m_LeaveTime / 0.3f);
                if (m_LeaveTime >= 0.3f)
                    Destroy(gameObject);
                return;
            }

            // Sortie du coffre, puis flottement.
            var t = Mathf.Clamp01(m_Age / Mathf.Max(0.01f, m_RiseTime));
            var eased = 1f - (1f - t) * (1f - t);
            var bob = Mathf.Sin((m_Age + m_Phase) * 2.5f) * m_BobAmplitude * t;
            transform.position = Vector3.Lerp(m_From, m_To, eased) + Vector3.up * bob;

            var targetScale = m_BaseScale * (isHovered ? m_HoverScale : 1f);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f - Mathf.Exp(-deltaTime * 12f));

            // Le sceau tourne doucement, plus vite et plus clair quand la main vise l'orbe ; l'icône s'éclaire et respire.
            m_Hover = Mathf.MoveTowards(m_Hover, isHovered ? 1f : 0f, deltaTime * 5f);
            if (m_Icon != null)
            {
                m_Icon.color = Color.Lerp(m_IconColor, Color.white, 0.45f * m_Hover);
                m_Icon.rectTransform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin((m_Age + m_Phase) * 3f));
            }

            if (m_Seal != null)
            {
                m_Seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f + (m_Age + m_Phase) * Mathf.Lerp(18f, 60f, m_Hover));
                var color = Color.Lerp(m_SealColor, Color.white, 0.5f * m_Hover);
                color.a = Mathf.Lerp(0.75f, 1f, m_Hover) * (0.85f + 0.15f * Mathf.Sin(m_Age * 3f));
                m_Seal.color = color;
            }

            FaceLabelToPlayer();
        }

        void FaceLabelToPlayer()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (m_Label == null || head == null)
                return;

            var away = m_Label.transform.position - head.position;
            away.y = 0f;
            if (away.sqrMagnitude > 1e-4f)
                m_Label.transform.rotation = Quaternion.LookRotation(away);
        }

        public override bool IsSelectableBy(IXRSelectInteractor interactor) =>
            base.IsSelectableBy(interactor) && !m_Leaving && m_Age >= m_RiseTime * 0.5f && IsInReach(interactor);

        // La main (la manette, pas le point visé) doit être à moins de quelques mètres de l'orbe.
        bool IsInReach(IXRInteractor interactor)
        {
            var hand = interactor.transform.position;
            foreach (var collider in colliders)
            {
                if (collider != null && collider.enabled &&
                    (collider.ClosestPoint(hand) - hand).sqrMagnitude <= m_MaxPickDistance * m_MaxPickDistance)
                    return true;
            }

            return false;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            if (m_Chest != null && !m_Leaving)
                m_Chest.Pick(this, args.interactorObject);
        }

        /// <summary>L'orbe s'en va (prise ou perdue) : elle ne peut plus être attrapée.</summary>
        public void Leave()
        {
            if (m_Leaving)
                return;

            m_Leaving = true;
            foreach (var collider in colliders)
            {
                if (collider != null)
                    collider.enabled = false;
            }
        }
    }
}
