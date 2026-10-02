using System.Collections.Generic;
using Archery.Core;
using Archery.Player;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Bows
{
    /// <summary>
    /// Carquois invisible dans le dos : une main vide qui serre la poignée derrière la tête
    /// reçoit une flèche (GDD, section 4.1). À placer sur le XR Origin, avec <see cref="PlayerRig"/>.
    /// </summary>
    /// <remarks>
    /// S'exécute avant le XRInteractionManager pour que la flèche soit saisie avant
    /// tout autre objet visé par la main au même moment.
    /// </remarks>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_InteractionManager - 100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerRig))]
    public class Quiver : MonoBehaviour
    {
        [Tooltip("Coin minimal de la zone de prise, dans le repère horizontal de la tête (x droite, y haut, z avant).")]
        [SerializeField]
        Vector3 m_GrabZoneMin = new Vector3(-0.45f, -0.6f, -0.6f);

        [Tooltip("Coin maximal de la zone de prise. z = -0.02 : il suffit que la main passe derrière le plan des yeux.")]
        [SerializeField]
        Vector3 m_GrabZoneMax = new Vector3(0.45f, 0.4f, -0.02f);

        [Tooltip("Délai (s) pendant lequel une pression de la poignée faite juste avant d'entrer dans la zone compte encore.")]
        [SerializeField]
        float m_PressBuffer = 0.5f;

        [Tooltip("Petite vibration quand une main vide entre dans la zone.")]
        [SerializeField]
        float m_EnterHapticAmplitude = 0.12f;

        [SerializeField]
        AudioClip m_DrawClip;

        PlayerRig m_Rig;
        readonly HashSet<XRBaseInputInteractor> m_HandsInZone = new HashSet<XRBaseInputInteractor>();
        readonly Dictionary<XRBaseInputInteractor, float> m_LastPressTime = new Dictionary<XRBaseInputInteractor, float>();

        void Awake() => m_Rig = GetComponent<PlayerRig>();

        void Update()
        {
            var head = m_Rig.Head;
            var pool = ArrowPool.Instance;
            if (head == null || pool == null)
                return;

            var toHeadSpace = Quaternion.Inverse(m_Rig.HeadYaw);
            var hands = m_Rig.Hands;
            for (var i = 0; i < hands.Count; i++)
            {
                var hand = hands[i];
                if (hand == null || !hand.isActiveAndEnabled)
                    continue;

                // On retient le moment de la pression : souvent, on serre la poignée
                // un peu avant que la main arrive dans le dos.
                if (hand.selectInput.ReadWasPerformedThisFrame())
                    m_LastPressTime[hand] = Time.time;

                var local = toHeadSpace * (hand.transform.position - head.position);
                if (!IsInZone(local))
                {
                    m_HandsInZone.Remove(hand);
                    continue;
                }

                if (m_HandsInZone.Add(hand) && !hand.hasSelection)
                    Haptics.Pulse(hand, m_EnterHapticAmplitude, 0.03f);

                if (!hand.hasSelection && hand.selectInput.ReadIsPerformed() && PressedRecently(hand))
                    GiveArrow(hand, pool);
            }
        }

        bool IsInZone(Vector3 point) =>
            point.x >= m_GrabZoneMin.x && point.x <= m_GrabZoneMax.x &&
            point.y >= m_GrabZoneMin.y && point.y <= m_GrabZoneMax.y &&
            point.z >= m_GrabZoneMin.z && point.z <= m_GrabZoneMax.z;

        bool PressedRecently(XRBaseInputInteractor hand) =>
            m_LastPressTime.TryGetValue(hand, out var pressTime) && Time.time - pressTime <= m_PressBuffer;

        void GiveArrow(XRBaseInputInteractor hand, ArrowPool pool)
        {
            // Une pression ne donne qu'une seule flèche.
            m_LastPressTime[hand] = float.NegativeInfinity;

            var arrow = pool.Get();
            if (!arrow.TryPutInHand(hand))
            {
                arrow.Despawn();
                return;
            }

            Haptics.Pulse(hand, 0.3f, 0.04f);
            Sfx.Play(m_DrawClip, hand.transform.position, 0.8f);
        }

        void OnDrawGizmosSelected()
        {
            var rig = m_Rig != null ? m_Rig : GetComponent<PlayerRig>();
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(head.position, rig.HeadYaw, Vector3.one);
            Gizmos.DrawWireCube((m_GrabZoneMin + m_GrabZoneMax) * 0.5f, m_GrabZoneMax - m_GrabZoneMin);
        }
    }
}
