using Archery.Player;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Chests
{
    /// <summary>
    /// Une orbe qui flotte au-dessus d'un coffre ouvert (GDD, section 13). On l'attrape à la main pour prendre
    /// sa récompense ; les autres orbes du coffre disparaissent.
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

        [Tooltip("Distance maximale (m) entre la main et l'orbe : il faut la prendre à la main, pas au rayon.")]
        [SerializeField]
        float m_MaxGrabDistance = 0.35f;

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
            base.IsSelectableBy(interactor) && !m_Leaving && m_Age >= m_RiseTime * 0.5f && IsNear(interactor);

        bool IsNear(IXRInteractor interactor)
        {
            var attach = interactor.GetAttachTransform(this);
            if (attach == null)
                return false;

            var hand = attach.position;
            foreach (var collider in colliders)
            {
                if (collider != null && collider.enabled &&
                    (collider.ClosestPoint(hand) - hand).sqrMagnitude <= m_MaxGrabDistance * m_MaxGrabDistance)
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
