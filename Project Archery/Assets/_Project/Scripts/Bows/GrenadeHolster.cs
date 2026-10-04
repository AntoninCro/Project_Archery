using System.Collections.Generic;
using Archery.Core;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Bows
{
    /// <summary>
    /// Étui invisible en bas du dos (GDD, section 23) : une main vide qui serre la poignée derrière les hanches
    /// reçoit une grenade de flèches, si elle est rechargée. Le temps de recharge commence quand on la prend.
    /// À placer sur le XR Origin, à côté du <see cref="Quiver"/>.
    /// </summary>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_InteractionManager - 100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerRig))]
    public class GrenadeHolster : MonoBehaviour
    {
        [SerializeField]
        ArrowGrenade m_GrenadePrefab;

        [Tooltip("Temps de recharge (s) entre deux grenades.")]
        [SerializeField]
        float m_Cooldown = 30f;

        [Tooltip("Coin minimal de la zone de prise, dans le repère horizontal de la tête (x droite, y haut, z avant). Sous le carquois.")]
        [SerializeField]
        Vector3 m_GrabZoneMin = new Vector3(-0.4f, -1.2f, -0.6f);

        [Tooltip("Coin maximal de la zone de prise.")]
        [SerializeField]
        Vector3 m_GrabZoneMax = new Vector3(0.4f, -0.65f, 0.05f);

        [Tooltip("Délai (s) pendant lequel une pression de la poignée faite juste avant d'entrer dans la zone compte encore.")]
        [SerializeField]
        float m_PressBuffer = 0.5f;

        [SerializeField]
        AudioClip m_DrawClip;

        [Tooltip("Optionnel : son quand la grenade est de nouveau prête.")]
        [SerializeField]
        AudioClip m_ReadyClip;

        [SerializeField]
        Color m_ReadyColor = new Color(1f, 0.6f, 0.2f);

        PlayerRig m_Rig;
        readonly Dictionary<XRBaseInputInteractor, float> m_LastPressTime = new Dictionary<XRBaseInputInteractor, float>();

        public static GrenadeHolster Instance { get; private set; }

        /// <summary>Secondes avant la prochaine grenade (0 = prête).</summary>
        public float CooldownLeft { get; private set; }

        public bool IsReady => CooldownLeft <= 0f;

        void Awake()
        {
            Instance = this;
            m_Rig = GetComponent<PlayerRig>();
            if (m_GrenadePrefab == null)
                Debug.LogWarning("GrenadeHolster : aucun prefab de grenade.", this);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (CooldownLeft > 0f)
            {
                CooldownLeft = Mathf.Max(0f, CooldownLeft - Time.deltaTime);
                if (CooldownLeft <= 0f)
                    AnnounceReady();
            }

            var head = m_Rig.Head;
            if (head == null || m_GrenadePrefab == null)
                return;

            var toHeadSpace = Quaternion.Inverse(m_Rig.HeadYaw);
            foreach (var hand in m_Rig.Hands)
            {
                if (hand == null || !hand.isActiveAndEnabled)
                    continue;

                if (hand.selectInput.ReadWasPerformedThisFrame())
                    m_LastPressTime[hand] = Time.time;

                var local = toHeadSpace * (hand.transform.position - head.position);
                if (!IsInZone(local) || hand.hasSelection || !hand.selectInput.ReadIsPerformed() || !PressedRecently(hand))
                    continue;

                if (IsReady)
                    GiveGrenade(hand);
                else
                    Haptics.Pulse(hand, 0.1f, 0.05f);

                m_LastPressTime[hand] = float.NegativeInfinity;
            }
        }

        bool IsInZone(Vector3 point) =>
            point.x >= m_GrabZoneMin.x && point.x <= m_GrabZoneMax.x &&
            point.y >= m_GrabZoneMin.y && point.y <= m_GrabZoneMax.y &&
            point.z >= m_GrabZoneMin.z && point.z <= m_GrabZoneMax.z;

        bool PressedRecently(XRBaseInputInteractor hand) =>
            m_LastPressTime.TryGetValue(hand, out var pressTime) && Time.time - pressTime <= m_PressBuffer;

        void GiveGrenade(XRBaseInputInteractor hand)
        {
            var attach = hand.attachTransform != null ? hand.attachTransform : hand.transform;
            var grenade = Instantiate(m_GrenadePrefab, attach.position, attach.rotation);
            var grab = grenade.GetComponent<XRGrabInteractable>();
            var manager = grab.interactionManager != null ? grab.interactionManager : FindAnyObjectByType<XRInteractionManager>();
            if (manager == null)
            {
                Destroy(grenade.gameObject);
                return;
            }

            manager.SelectEnter(hand, (IXRSelectInteractable)grab);
            CooldownLeft = m_Cooldown;
            Haptics.Pulse(hand, 0.4f, 0.06f);
            Sfx.Play(m_DrawClip, hand.transform.position, 0.8f);
        }

        void AnnounceReady()
        {
            foreach (var hand in m_Rig.Hands)
                Haptics.Pulse(hand, 0.25f, 0.08f);

            var head = m_Rig.Head;
            if (head == null)
                return;

            Sfx.Play(m_ReadyClip, head.position, 0.7f, 1f, 0f);
            FloatingText.Spawn(head.position + m_Rig.HeadYaw * new Vector3(0f, -0.3f, 1.5f), "Grenade prête", m_ReadyColor, 0.6f, 1.5f);
        }

        void OnDrawGizmosSelected()
        {
            var rig = m_Rig != null ? m_Rig : GetComponent<PlayerRig>();
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(head.position, rig.HeadYaw, Vector3.one);
            Gizmos.DrawWireCube((m_GrabZoneMin + m_GrabZoneMax) * 0.5f, m_GrabZoneMax - m_GrabZoneMin);
        }
    }
}
