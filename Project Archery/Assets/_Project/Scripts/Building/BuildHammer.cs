using Archery.Player;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Building
{
    /// <summary>
    /// Le marteau de construction (GDD, section 9). Il apparaît dans la main libre quand on achète un bâtiment :
    /// on l'attrape avec la poignée et on frappe le plan bleu. Lâché, il revient flotter dans la main.
    /// Créé et détruit par le <see cref="BuildManager"/>.
    /// </summary>
    /// <remarks>
    /// À placer sur un objet attrapable : <see cref="XRGrabInteractable"/> (Movement Type <c>Instantaneous</c>,
    /// sans Throw On Detach), un <see cref="Rigidbody"/> sans gravité et un collider sur le manche.
    /// Un coup compte quand la tête du marteau entre dans le volume du plan assez vite ; pour le coup suivant,
    /// il faut la ressortir ou marquer un arrêt.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class BuildHammer : MonoBehaviour
    {
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

        [Tooltip("Vitesse à laquelle le marteau lâché revient dans la main.")]
        [SerializeField]
        float m_ReturnSpeed = 14f;

        XRGrabInteractable m_Grab;
        Rigidbody m_Body;
        BuildManager m_Manager;
        Structure m_Target;
        XRBaseInputInteractor m_Hand;
        Vector3 m_PreviousHead;
        bool m_HasPreviousHead;
        bool m_Armed = true;
        bool m_IgnoreBodyPending;
        float m_LastStrikeTime = float.NegativeInfinity;

        /// <summary>Main près de laquelle le marteau flotte quand personne ne le tient.</summary>
        public XRBaseInputInteractor Hand => m_Hand;

        public bool IsHeld => m_Grab != null && m_Grab.isSelected;

        Vector3 HeadPosition => m_Head != null ? m_Head.position : transform.position;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Body = GetComponent<Rigidbody>();
            m_Grab.selectEntered.AddListener(OnGrabbed);
            m_Grab.selectExited.AddListener(OnReleased);
            if (m_Body != null)
            {
                m_Body.useGravity = false;
                m_Body.isKinematic = true;
            }

            if (m_Head == null)
                Debug.LogWarning("BuildHammer : renseigne « Head » (le centre de la tête du marteau).", this);
        }

        void OnDestroy()
        {
            if (m_Grab == null)
                return;

            m_Grab.selectEntered.RemoveListener(OnGrabbed);
            m_Grab.selectExited.RemoveListener(OnReleased);
        }

        internal void Begin(BuildManager manager, Structure target, XRBaseInputInteractor hand)
        {
            m_Manager = manager;
            m_Target = target;
            m_Hand = hand;
            if (TryGetFloatPose(out var pose))
                transform.SetPositionAndRotation(pose.position, pose.rotation);
            IgnorePlayerBody();
        }

        // Le marteau qui flotte dans la main ne pousse pas le joueur.
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

        // Le marteau flotte près de la dernière main qui l'a tenu.
        void OnGrabbed(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRBaseInputInteractor hand)
                m_Hand = hand;
            m_HasPreviousHead = false;
        }

        // Lâché, le PlayerRig lui rend ses collisions avec le corps du joueur : on les retire de nouveau.
        void OnReleased(SelectExitEventArgs args) => m_IgnoreBodyPending = true;

        // Après le déplacement des objets tenus par XRI (dans son Update).
        void LateUpdate()
        {
            if (!IsHeld)
            {
                if (m_IgnoreBodyPending)
                {
                    m_IgnoreBodyPending = false;
                    IgnorePlayerBody();
                }

                Float();
                m_HasPreviousHead = false;
                return;
            }

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
                m_Manager.OnHammerStrike(this, closest, speed, m_Grab.firstInteractorSelecting);
        }

        void Float()
        {
            if (!TryGetFloatPose(out var pose))
                return;

            if (m_Body != null && !m_Body.isKinematic)
                m_Body.isKinematic = true;

            var blend = 1f - Mathf.Exp(-m_ReturnSpeed * Time.unscaledDeltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, pose.position, blend),
                                             Quaternion.Slerp(transform.rotation, pose.rotation, blend));
        }

        // Pose du marteau quand sa poignée (Attach Transform) est exactement dans la main : on l'attrape sans à-coup.
        bool TryGetFloatPose(out Pose pose)
        {
            pose = default;
            if (m_Hand == null || !m_Hand.isActiveAndEnabled)
                return false;

            var handAttach = m_Hand.attachTransform != null ? m_Hand.attachTransform : m_Hand.transform;
            var grip = m_Grab.attachTransform != null ? m_Grab.attachTransform : transform;
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
    }
}
