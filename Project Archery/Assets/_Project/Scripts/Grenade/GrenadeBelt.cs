using Archery.Core;
using Archery.Player;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Grenades
{
    /// <summary>
    /// Ceinture invisible dans le bas du dos : une main vide qui serre la poignée sous le carquois
    /// en ressort une grenade à flèches. Elle tient aussi le temps de recharge, lu par la montre.
    /// À placer sur le XR Origin, avec <see cref="PlayerRig"/> et <see cref="Archery.Bows.Quiver"/>.
    /// </summary>
    /// <remarks>
    /// Même principe que le carquois, mais plus bas : la zone commence sous celle du carquois
    /// pour qu'un même geste ne puisse pas donner une flèche et une grenade.
    /// </remarks>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_InteractionManager - 100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerRig))]
    public class GrenadeBelt : MonoBehaviour
    {
        [Tooltip("La grenade sortie du dos. Son prefab porte un XR Grab Interactable et un Arrow Grenade.")]
        [SerializeField]
        ArrowGrenade m_GrenadePrefab;

        [Header("Zone de prise")]
        [Tooltip("Coin minimal de la zone, dans le repère horizontal de la tête (x droite, y haut, z avant). " +
                 "Le y reste sous la zone du carquois, qui descend à -0,6.")]
        [SerializeField]
        Vector3 m_GrabZoneMin = new Vector3(-0.45f, -1.05f, -0.5f);

        [Tooltip("Coin maximal de la zone. z = -0,02 : il suffit que la main passe derrière le plan des yeux.")]
        [SerializeField]
        Vector3 m_GrabZoneMax = new Vector3(0.45f, -0.62f, -0.02f);

        [Tooltip("Délai (s) pendant lequel une pression faite juste avant d'entrer dans la zone compte encore.")]
        [SerializeField]
        float m_PressBuffer = 0.5f;

        [Header("Recharge")]
        [Tooltip("Temps de recharge de base, en secondes (GDD : 15 s).")]
        [Min(1f)]
        [SerializeField]
        float m_BaseCooldown = 15f;

        [Tooltip("Recharge minimale, quelles que soient les améliorations.")]
        [Min(0.5f)]
        [SerializeField]
        float m_MinCooldown = 4f;

        [Header("Retours")]
        [Tooltip("Petite vibration quand une main vide entre dans la zone.")]
        [SerializeField]
        float m_EnterHapticAmplitude = 0.12f;

        [SerializeField]
        AudioClip m_DrawClip;

        PlayerRig m_Rig;
        ArrowGrenade m_Held;
        float m_Remaining;
        float m_LastCooldown;

        // Dernière pression de la poignée, par main.
        XRBaseInputInteractor m_PressedHand;
        float m_PressTime = float.NegativeInfinity;
        XRBaseInputInteractor m_HandInZone;

        public static GrenadeBelt Instance { get; private set; }

        /// <summary>Secondes restantes avant la prochaine grenade (0 = prête).</summary>
        public float Remaining => m_Remaining;

        public bool IsReady => m_Remaining <= 0f && m_Held == null;

        /// <summary>Recharge actuelle, amélioration « Grenade rapide » comprise.</summary>
        public float Cooldown
        {
            get
            {
                var upgrades = PlayerUpgrades.Instance;
                var reduction = upgrades != null ? Mathf.Clamp01(upgrades.Total(UpgradeEffect.GrenadeCooldown)) : 0f;
                return Mathf.Max(m_MinCooldown, m_BaseCooldown * (1f - reduction));
            }
        }

        /// <summary>Progression de la recharge, de 0 (vient de partir) à 1 (prête).</summary>
        public float Progress
        {
            get
            {
                if (m_Remaining <= 0f)
                    return 1f;

                var total = m_LastCooldown > 0f ? m_LastCooldown : Cooldown;
                return Mathf.Clamp01(1f - m_Remaining / total);
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Grenade Belt dans la scène.", this);
            Instance = this;
            m_Rig = GetComponent<PlayerRig>();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (m_Remaining > 0f)
                m_Remaining = Mathf.Max(0f, m_Remaining - Time.deltaTime);

            var head = m_Rig.Head;
            if (head == null || m_GrenadePrefab == null)
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
                {
                    m_PressedHand = hand;
                    m_PressTime = Time.time;
                }

                var local = toHeadSpace * (hand.transform.position - head.position);
                if (!IsInZone(local))
                {
                    if (m_HandInZone == hand)
                        m_HandInZone = null;
                    continue;
                }

                if (m_HandInZone != hand)
                {
                    m_HandInZone = hand;
                    if (!hand.hasSelection && IsReady)
                        Haptics.Pulse(hand, m_EnterHapticAmplitude, 0.03f);
                }

                if (IsReady && !hand.hasSelection && hand.selectInput.ReadIsPerformed() && PressedRecently(hand))
                    GiveGrenade(hand);
            }
        }

        bool IsInZone(Vector3 point) =>
            point.x >= m_GrabZoneMin.x && point.x <= m_GrabZoneMax.x &&
            point.y >= m_GrabZoneMin.y && point.y <= m_GrabZoneMax.y &&
            point.z >= m_GrabZoneMin.z && point.z <= m_GrabZoneMax.z;

        bool PressedRecently(XRBaseInputInteractor hand) =>
            m_PressedHand == hand && Time.time - m_PressTime <= m_PressBuffer;

        void GiveGrenade(XRBaseInputInteractor hand)
        {
            // Une pression ne donne qu'une seule grenade.
            m_PressTime = float.NegativeInfinity;

            var grenade = Instantiate(m_GrenadePrefab);
            grenade.name = m_GrenadePrefab.name;
            if (!grenade.TryPutInHand(hand))
            {
                Destroy(grenade.gameObject);
                return;
            }

            m_Held = grenade;
            Haptics.Pulse(hand, 0.3f, 0.05f);
            Sfx.Play(m_DrawClip, hand.transform.position, 0.8f);
        }

        /// <summary>Appelé par la grenade quand la main la lâche : la recharge démarre.</summary>
        internal void OnGrenadeThrown(ArrowGrenade grenade)
        {
            if (m_Held != grenade)
                return;

            m_Held = null;
            m_LastCooldown = Cooldown;
            m_Remaining = m_LastCooldown;
        }

        /// <summary>Rend la grenade disponible tout de suite (nouvelle partie, test).</summary>
        public void ResetCooldown() => m_Remaining = 0f;

        void OnDrawGizmosSelected()
        {
            var rig = m_Rig != null ? m_Rig : GetComponent<PlayerRig>();
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(head.position, rig.HeadYaw, Vector3.one);
            Gizmos.DrawWireCube((m_GrabZoneMin + m_GrabZoneMax) * 0.5f, m_GrabZoneMax - m_GrabZoneMin);
        }
    }
}
