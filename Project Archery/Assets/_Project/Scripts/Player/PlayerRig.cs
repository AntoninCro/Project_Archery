using System.Collections.Generic;
using Archery.Bows;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Jump;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace Archery.Player
{
    /// <summary>
    /// Point d'accès au joueur : tête, mains, emplacement de l'arc rangé.
    /// À placer sur le XR Origin.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [DisallowMultipleComponent]
    public class PlayerRig : MonoBehaviour
    {
        [SerializeField]
        XROrigin m_Origin;

        [Tooltip("Position de l'arc rangé, dans le repère horizontal de la tête : x vers la main de l'arc, y vers le haut, z vers l'avant.")]
        [SerializeField]
        Vector3 m_BowHolsterOffset = new Vector3(0.2f, -0.5f, 0.3f);

        [Tooltip("Désactive le déplacement « grab move » du XR Origin, qui utilise aussi le bouton de poignée.")]
        [SerializeField]
        bool m_DisableGrabMove = true;

        [Tooltip("Désactive le saut du XR Origin (bouton A) : A et X servent au slide de la course aux bras.")]
        [SerializeField]
        bool m_DisableJump = true;

        [Tooltip("Ce que le joueur tient en main (arc, grenade…) ne compte pas comme sol et ne heurte pas son corps. " +
                 "Sinon, en courant, l'arc passé sous la tête faisait croire à la gravité d'XRI qu'on touchait le sol : on courait dans le vide.")]
        [SerializeField]
        bool m_HeldObjectsAreNotGround = true;

        // Calque « Ignore Raycast » : la gravité d'XRI cherche le sol sur tous les calques sauf celui-ci.
        const int k_HeldLayer = 2;

        static readonly List<Collider> s_DestroyedColliders = new List<Collider>();

        readonly List<XRBaseInputInteractor> m_Hands = new List<XRBaseInputInteractor>();

        // Colliders des objets tenus, avec leur calque d'origine.
        readonly Dictionary<Collider, int> m_HeldLayers = new Dictionary<Collider, int>();
        CharacterController m_Body;

        public static PlayerRig Instance { get; private set; }

        public XROrigin Origin => m_Origin;
        public Transform Head => m_Origin != null && m_Origin.Camera != null ? m_Origin.Camera.transform : null;
        public IReadOnlyList<XRBaseInputInteractor> Hands => m_Hands;

        /// <summary>Main qui tient (ou a tenu en dernier) l'arc.</summary>
        public InteractorHandedness BowHand { get; set; } = InteractorHandedness.Left;

        /// <summary>Position des pieds du joueur : sous la tête, au niveau du sol du XR Origin.</summary>
        public Vector3 BodyPosition
        {
            get
            {
                var head = Head;
                var floor = m_Origin != null ? m_Origin.transform.position.y : transform.position.y;
                return head != null ? new Vector3(head.position.x, floor, head.position.z) : transform.position;
            }
        }

        /// <summary>Orientation horizontale du regard (sans tangage ni roulis).</summary>
        public Quaternion HeadYaw
        {
            get
            {
                var head = Head;
                if (head == null)
                    return Quaternion.identity;

                var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.01f)
                    forward = Vector3.ProjectOnPlane(head.forward.y < 0f ? head.up : -head.up, Vector3.up);
                return Quaternion.LookRotation(forward.normalized, Vector3.up);
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs PlayerRig dans la scène.", this);
            Instance = this;

            if (m_Origin == null)
                m_Origin = GetComponentInParent<XROrigin>();
            if (m_Origin == null)
                m_Origin = GetComponentInChildren<XROrigin>();

            var root = m_Origin != null ? m_Origin.transform : transform;
            foreach (var interactor in root.GetComponentsInChildren<NearFarInteractor>(true))
            {
                if (interactor.handedness != InteractorHandedness.None)
                    m_Hands.Add(interactor);
            }

            if (m_Hands.Count == 0)
                Debug.LogWarning("PlayerRig : aucune main (Near-Far Interactor) trouvée sous le XR Origin.", this);

            // Les flèches traversent le corps du joueur.
            ArrowIgnore.Register(root.GetComponentsInChildren<Collider>(true));
            m_Body = root.GetComponentInChildren<CharacterController>(true);

            if (m_DisableGrabMove)
            {
                foreach (var provider in root.GetComponentsInChildren<GrabMoveProvider>(true))
                    provider.enabled = false;
                foreach (var provider in root.GetComponentsInChildren<TwoHandedGrabMoveProvider>(true))
                    provider.enabled = false;
            }

            if (m_DisableJump)
            {
                foreach (var provider in root.GetComponentsInChildren<JumpProvider>(true))
                    provider.enabled = false;
            }
        }

        void OnEnable()
        {
            if (!m_HeldObjectsAreNotGround)
                return;

            foreach (var hand in m_Hands)
            {
                hand.selectEntered.AddListener(OnSelectEntered);
                hand.selectExited.AddListener(OnSelectExited);
            }
        }

        void OnDisable()
        {
            foreach (var hand in m_Hands)
            {
                if (hand == null)
                    continue;

                hand.selectEntered.RemoveListener(OnSelectEntered);
                hand.selectExited.RemoveListener(OnSelectExited);
            }

            foreach (var pair in m_HeldLayers)
            {
                if (pair.Key != null)
                    Release(pair.Key, pair.Value);
            }

            m_HeldLayers.Clear();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void OnSelectEntered(SelectEnterEventArgs args) => SetHeld(args.interactableObject, true);

        void OnSelectExited(SelectExitEventArgs args)
        {
            // Encore tenu par l'autre main : il reste « tenu » jusqu'à ce qu'elle le lâche aussi.
            if (args.interactableObject != null && args.interactableObject.isSelected)
                return;

            SetHeld(args.interactableObject, false);
        }

        // Pendant qu'on le tient, l'objet passe sur le calque « Ignore Raycast » et ne heurte plus le corps du joueur.
        // Il retrouve son calque quand on le lâche.
        void SetHeld(IXRSelectInteractable interactable, bool held)
        {
            if (interactable is not Object unityObject || unityObject == null)
                return;

            if (held)
                ForgetDestroyedColliders();

            foreach (var collider in interactable.colliders)
            {
                if (collider == null)
                    continue;

                if (held)
                {
                    if (!m_HeldLayers.ContainsKey(collider))
                        m_HeldLayers.Add(collider, collider.gameObject.layer);
                    collider.gameObject.layer = k_HeldLayer;
                    IgnoreBody(collider, true);
                }
                else if (m_HeldLayers.TryGetValue(collider, out var layer))
                {
                    m_HeldLayers.Remove(collider);
                    Release(collider, layer);
                }
            }
        }

        void Release(Collider collider, int layer)
        {
            collider.gameObject.layer = layer;
            IgnoreBody(collider, false);
        }

        void IgnoreBody(Collider collider, bool ignore)
        {
            // Unity refuse d'ignorer une collision avec un collider désactivé.
            if (m_Body != null && m_Body.enabled && m_Body.gameObject.activeInHierarchy &&
                collider.enabled && collider.gameObject.activeInHierarchy)
                Physics.IgnoreCollision(m_Body, collider, ignore);
        }

        // Un objet détruit dans la main (orbe de coffre) ne passe pas toujours par « lâché ».
        void ForgetDestroyedColliders()
        {
            s_DestroyedColliders.Clear();
            foreach (var collider in m_HeldLayers.Keys)
            {
                if (collider == null)
                    s_DestroyedColliders.Add(collider);
            }

            foreach (var collider in s_DestroyedColliders)
                m_HeldLayers.Remove(collider);
        }

        public bool TryGetHand(InteractorHandedness handedness, out XRBaseInputInteractor hand)
        {
            foreach (var candidate in m_Hands)
            {
                if (candidate != null && candidate.handedness == handedness)
                {
                    hand = candidate;
                    return true;
                }
            }

            hand = null;
            return false;
        }

        public bool TryGetBowHolsterPose(out Pose pose)
        {
            var head = Head;
            if (head == null)
            {
                pose = default;
                return false;
            }

            var yaw = HeadYaw;
            var offset = m_BowHolsterOffset;
            offset.x = Mathf.Abs(offset.x) * (BowHand == InteractorHandedness.Right ? 1f : -1f);
            pose = new Pose(head.position + yaw * offset, yaw);
            return true;
        }
    }
}
