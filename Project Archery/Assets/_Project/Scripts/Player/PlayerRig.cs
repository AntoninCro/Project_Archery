using System.Collections.Generic;
using Archery.Bows;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
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

        readonly List<XRBaseInputInteractor> m_Hands = new List<XRBaseInputInteractor>();

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

            if (m_DisableGrabMove)
            {
                foreach (var provider in root.GetComponentsInChildren<GrabMoveProvider>(true))
                    provider.enabled = false;
                foreach (var provider in root.GetComponentsInChildren<TwoHandedGrabMoveProvider>(true))
                    provider.enabled = false;
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
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
