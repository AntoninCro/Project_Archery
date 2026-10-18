using System.Collections.Generic;
using Archery.Player;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Building
{
    /// <summary>
    /// Un marteau de construction (GDD, section 9). Quand on achète un bâtiment, il en apparaît un dans chaque main,
    /// sans rien attraper : il suit la main, qui se ferme dessus, et on frappe le plan bleu avec l'un ou l'autre.
    /// Créé et détruit par le <see cref="BuildManager"/>.
    /// </summary>
    /// <remarks>
    /// Le prefab garde son <see cref="XRGrabInteractable"/> : sa poignée (Attach Transform) dit comment le marteau se
    /// cale dans la main. Il est désactivé en jeu : on ne l'attrape plus, et on ne peut pas le lâcher.
    /// Un coup compte quand la tête du marteau entre dans le volume du plan assez vite ; pour le coup suivant, il faut
    /// la ressortir ou marquer un arrêt.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class BuildHammer : MonoBehaviour
    {
        static readonly List<BuildHammer> s_Active = new List<BuildHammer>();

        [Tooltip("Centre de la tête du marteau (objet vide).")]
        [SerializeField]
        Transform m_Head;

        [Tooltip("Rayon (m) de la tête du marteau.")]
        [SerializeField]
        float m_HeadRadius = 0.12f;

        [Tooltip("Vitesse minimale (m/s) de la tête pour qu'un coup compte.")]
        [SerializeField]
        float m_MinStrikeSpeed = 1.2f;

        [Tooltip("Temps minimal (s) entre deux coups.")]
        [SerializeField]
        float m_MinStrikeInterval = 0.25f;

        XRGrabInteractable m_Grab;
        Rigidbody m_Body;
        BuildManager m_Manager;
        Structure m_Target;
        XRBaseInputInteractor m_Hand;
        Vector3 m_PreviousHead;
        bool m_HasPreviousHead;
        bool m_Armed = true;
        float m_LastStrikeTime = float.NegativeInfinity;

        /// <summary>La main qui porte ce marteau.</summary>
        public XRBaseInputInteractor Hand => m_Hand;

        /// <summary>Le bâtiment en plan que ce marteau construit.</summary>
        public Structure Target => m_Target;

        Vector3 HeadPosition => m_Head != null ? m_Head.position : transform.position;

        /// <summary>Cette main porte un marteau (son gant se ferme dessus).</summary>
        public static bool InHand(XRBaseInputInteractor hand)
        {
            if (hand == null)
                return false;

            foreach (var hammer in s_Active)
            {
                if (hammer != null && hammer.m_Hand == hand)
                    return true;
            }

            return false;
        }

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Body = GetComponent<Rigidbody>();
            if (m_Body != null)
            {
                m_Body.useGravity = false;
                m_Body.isKinematic = true;
            }

            if (m_Head == null)
                Debug.LogWarning("BuildHammer : renseigne « Head » (le centre de la tête du marteau).", this);
        }

        void OnEnable() => s_Active.Add(this);

        void OnDisable() => s_Active.Remove(this);

        internal void Begin(BuildManager manager, Structure target, XRBaseInputInteractor hand)
        {
            m_Manager = manager;
            m_Target = target;
            m_Hand = hand;

            // Il est déjà dans la main : on ne l'attrape pas.
            if (m_Grab != null)
                m_Grab.enabled = false;

            FollowHand();
            IgnorePlayerBody();
        }

        // Le marteau dans la main ne pousse pas le joueur.
        void IgnorePlayerBody()
        {
            var rig = PlayerRig.Instance;
            var body = rig != null ? rig.GetComponentInChildren<CharacterController>() : null;
            if (body == null || !body.enabled)
                return;

            foreach (var collider in GetComponentsInChildren<Collider>())
            {
                if (collider.enabled)
                    Physics.IgnoreCollision(body, collider, true);
            }
        }

        // Après le déplacement des manettes : le marteau colle à la main, puis on regarde s'il frappe le plan.
        void LateUpdate()
        {
            FollowHand();

            var head = HeadPosition;
            var deltaTime = Time.unscaledDeltaTime;
            var velocity = m_HasPreviousHead && deltaTime > 0f ? (head - m_PreviousHead) / deltaTime : Vector3.zero;
            m_PreviousHead = head;
            m_HasPreviousHead = true;
            DetectStrike(head, velocity);
        }

        void DetectStrike(Vector3 head, Vector3 velocity)
        {
            if (m_Target == null || !m_Target.IsUnderConstruction)
                return;

            var bounds = m_Target.ConstructionBounds;
            var closest = bounds.ClosestPoint(head);
            var distance = (closest - head).magnitude;
            var speed = velocity.magnitude;

            // Pour frapper de nouveau, on ressort du plan, ou on marque un arrêt comme après un vrai coup.
            if (!m_Armed && (distance > m_HeadRadius + 0.1f || speed < m_MinStrikeSpeed * 0.4f))
                m_Armed = true;

            if (!m_Armed || distance > m_HeadRadius || speed < m_MinStrikeSpeed ||
                Time.unscaledTime - m_LastStrikeTime < m_MinStrikeInterval)
                return;

            // On frappe vers le plan, pas en le quittant.
            var toward = distance > 0.001f ? closest - head : bounds.center - head;
            if (Vector3.Dot(velocity, toward) <= 0f)
                return;

            m_Armed = false;
            m_LastStrikeTime = Time.unscaledTime;
            if (m_Manager != null)
                m_Manager.OnHammerStrike(this, closest, speed, m_Hand);
        }

        /// <summary>
        /// Sans casque (touche E) : un coup sur le plan, au point visé. Faux si rien n'est en construction ou si le coup
        /// précédent est trop récent.
        /// </summary>
        public bool DesktopStrike(Vector3 point)
        {
            if (m_Target == null || !m_Target.IsUnderConstruction || Time.unscaledTime - m_LastStrikeTime < m_MinStrikeInterval)
                return false;

            m_LastStrikeTime = Time.unscaledTime;
            if (m_Manager != null)
                m_Manager.OnHammerStrike(this, point, m_MinStrikeSpeed * 2f, m_Hand);
            return true;
        }

        void FollowHand()
        {
            if (TryGetHandPose(out var pose))
                transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        // La pose qui met la poignée du marteau (Attach Transform) exactement dans la main.
        bool TryGetHandPose(out Pose pose)
        {
            pose = default;
            if (m_Hand == null || !m_Hand.isActiveAndEnabled)
                return false;

            var handAttach = m_Hand.attachTransform != null ? m_Hand.attachTransform : m_Hand.transform;
            var grip = m_Grab != null && m_Grab.attachTransform != null ? m_Grab.attachTransform : transform;
            var localRotation = Quaternion.Inverse(transform.rotation) * grip.rotation;
            var localOffset = Quaternion.Inverse(transform.rotation) * (grip.position - transform.position);
            var rotation = handAttach.rotation * Quaternion.Inverse(localRotation);
            pose = new Pose(handAttach.position - rotation * localOffset, rotation);
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(HeadPosition, m_HeadRadius);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Active.Clear();
    }
}
