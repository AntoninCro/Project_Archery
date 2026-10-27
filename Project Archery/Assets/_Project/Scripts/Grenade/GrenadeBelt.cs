using Archery.Core;
using Archery.Player;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Grenades
{
    /// <summary>
    /// Ceinture invisible aux hanches : une main vide qui serre la poignée à hauteur des poches (devant, sur le côté ou
    /// derrière) en sort une grenade à flèches. Elle tient aussi le temps de recharge, lu par la montre.
    /// À placer sur le XR Origin, avec <see cref="PlayerRig"/> et <see cref="Archery.Bows.Quiver"/>.
    /// </summary>
    /// <remarks>
    /// Même principe que le carquois, mais plus bas : la zone commence sous celle du carquois
    /// pour qu'un même geste ne puisse pas donner une flèche et une grenade.
    /// Le 22 octobre, elle est passée du bas du dos (on devait aller la chercher loin derrière) aux hanches, comme une
    /// grenade qu'on sort de sa poche. Elle s'arrête juste derrière le katana rangé à la hanche.
    /// Une main qui vise ou touche déjà ce qu'elle peut prendre (un emplacement de construction au bout du rayon, une
    /// arme rangée, un cristal…) le prend, pas une grenade : sinon, viser un emplacement au sol, la main basse, sortait
    /// une grenade.
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
                 "Les hanches et les poches : de 0,95 à 0,45 m sous les yeux (le carquois s'arrête à 0,42), " +
                 "jusqu'à 35 cm derrière les yeux (les poches arrière).")]
        [SerializeField]
        Vector3 m_GrabZoneMin = new Vector3(-0.45f, -0.95f, -0.35f);

        [Tooltip("Coin maximal de la zone. z = 0,12 : les poches avant, juste derrière le katana rangé à la hanche " +
                 "(20 cm devant les yeux).")]
        [SerializeField]
        Vector3 m_GrabZoneMax = new Vector3(0.45f, -0.45f, 0.12f);

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
        [Tooltip("Petite vibration quand une main vide entre dans la zone (une toutes les 2 s au plus : en courant, les " +
                 "bras balancés y passent sans arrêt).")]
        [SerializeField]
        float m_EnterHapticAmplitude = 0.12f;

        [SerializeField]
        AudioClip m_DrawClip;

        const float k_EnterPulseInterval = 2f;

        PlayerRig m_Rig;
        ArrowGrenade m_Held;
        float m_LastEnterPulse = float.NegativeInfinity;
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

        // Le talisman de la super grenade (26 octobre) : la recharge raccourcie de 50 % au niveau 1, et de 10 % de plus par niveau.
        const float k_SuperGrenadeCooldown = 0.5f;
        const float k_SuperGrenadeCooldownPerLevel = 0.1f;

        /// <summary>Recharge actuelle, améliorations « Grenade rapide » (retirée) et « Super grenade » comprises.</summary>
        public float Cooldown
        {
            get
            {
                var upgrades = PlayerUpgrades.Instance;
                var reduction = upgrades != null ? upgrades.Total(UpgradeEffect.GrenadeCooldown) : 0f;
                var super = upgrades != null ? upgrades.Stacks(UpgradeEffect.SuperGrenade) : 0;
                if (super > 0)
                    reduction = Mathf.Max(reduction, k_SuperGrenadeCooldown + k_SuperGrenadeCooldownPerLevel * (super - 1));
                return Mathf.Max(m_MinCooldown, m_BaseCooldown * (1f - Mathf.Clamp01(reduction)));
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

            // Jeu en pause (menu ouvert) : pas de grenade à prendre, comme le carquois.
            var head = m_Rig.Head;
            if (head == null || m_GrenadePrefab == null || SlowMotion.IsPaused)
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
                    if (!hand.hasSelection && IsReady && Time.time - m_LastEnterPulse >= k_EnterPulseInterval)
                    {
                        m_LastEnterPulse = Time.time;
                        Haptics.Pulse(hand, m_EnterHapticAmplitude, 0.03f);
                    }
                }

                if (IsReady && !hand.hasSelection && hand.selectInput.ReadIsPerformed() && PressedRecently(hand) && !HasOtherTarget(hand))
                    GiveGrenade(hand);
            }
        }

        bool IsInZone(Vector3 point) =>
            point.x >= m_GrabZoneMin.x && point.x <= m_GrabZoneMax.x &&
            point.y >= m_GrabZoneMin.y && point.y <= m_GrabZoneMax.y &&
            point.z >= m_GrabZoneMin.z && point.z <= m_GrabZoneMax.z;

        bool PressedRecently(XRBaseInputInteractor hand) =>
            m_PressedHand == hand && Time.time - m_PressTime <= m_PressBuffer;

        // La main survole (au bout du rayon ou de près) quelque chose qu'elle peut prendre : un emplacement de construction,
        // une arme rangée à la hanche (katana, bouclier, Akimbow), un cristal… La poignée le prend, pas une grenade.
        static bool HasOtherTarget(XRBaseInputInteractor hand)
        {
            var hovered = hand.interactablesHovered;
            for (var i = 0; i < hovered.Count; i++)
            {
                if (hovered[i] is not Object target || target == null)
                    continue;
                if (hovered[i] is IXRSelectInteractable selectable && selectable.IsSelectableBy(hand))
                    return true;
            }

            return false;
        }

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

        /// <summary>Appelé par la grenade lâchée pendant la pause : elle retourne à la ceinture, sans recharge.</summary>
        internal void OnGrenadeReturned(ArrowGrenade grenade)
        {
            if (m_Held == grenade)
                m_Held = null;
        }

        /// <summary>Rend la grenade disponible tout de suite (nouvelle partie, test).</summary>
        public void ResetCooldown() => m_Remaining = 0f;

        void OnDrawGizmosSelected()
        {
            var rig = m_Rig != null ? m_Rig : GetComponent<PlayerRig>();
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            // Quaternion.LookRotation renvoie un quaternion dont la longueur peut valoir 0,999981
            // a cause des arrondis. Matrix4x4.TRS le refuse et remplit la console d'erreurs :
            // on le renormalise avant de le lui donner.
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.5f);
            Gizmos.matrix = Matrix4x4.TRS(head.position, rig.HeadYaw.normalized, Vector3.one);
            Gizmos.DrawWireCube((m_GrabZoneMin + m_GrabZoneMax) * 0.5f, m_GrabZoneMax - m_GrabZoneMin);
        }
    }
}
