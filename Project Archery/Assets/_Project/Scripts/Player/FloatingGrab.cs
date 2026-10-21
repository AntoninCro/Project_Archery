using System;
using Archery.Bows;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Player
{
    /// <summary>
    /// Une arme qui flotte à côté du joueur et se prend avec la poignée de la manette (le katana, le bouclier) : lâchée, elle
    /// retourne à sa place (comme l'arc rangé) et suit ses déplacements sans traîner ; tenue, elle suit la main
    /// (XRGrabInteractable), et la main gantée se ferme sur sa poignée (<see cref="HandGrip"/>). Elle ne pousse ni ne bloque
    /// le joueur, et les flèches la traversent.
    /// </summary>
    /// <remarks>
    /// Créée par <see cref="Create"/>. <see cref="Suspend"/> : ni prise ni retour à sa place (sans casque, l'arme est placée
    /// par son propriétaire).
    /// </remarks>
    [DisallowMultipleComponent]
    public class FloatingGrab : MonoBehaviour
    {
        const float k_ReturnSpeed = 8f;

        XRGrabInteractable m_Grab;
        Collider[] m_Colliders = Array.Empty<Collider>();
        CharacterController m_PlayerBody;
        Vector3 m_HolsterOffset;
        Quaternion m_HolsterRotation = Quaternion.identity;
        Pose m_LastHolster;
        bool m_Following;
        bool m_Holstered = true;
        bool m_NeedsIgnore = true;
        bool m_Suspended;

        public XRGrabInteractable Grab => m_Grab;

        /// <summary>Tenue par une main.</summary>
        public bool Held => m_Grab != null && m_Grab.isSelected;

        /// <summary>La main qui la tient, ou null.</summary>
        public IXRSelectInteractor Holder => Held ? m_Grab.firstInteractorSelecting : null;

        /// <summary>Une main vient de la prendre.</summary>
        public event Action<IXRSelectInteractor> Grabbed;

        /// <summary>La main l'a lâchée.</summary>
        public event Action Released;

        /// <summary>La main qui la tient vient d'appuyer sur la gâchette.</summary>
        public event Action<IXRActivateInteractor> Activated;

        /// <summary>
        /// Rend <paramref name="root"/> prenable : un corps rigide, une boîte de prise (repère de la racine), l'interactable
        /// (la prise en <paramref name="gripPosition"/>, tournée de <paramref name="attachRotation"/> par rapport à la
        /// manette), la poignée pour la main gantée (au même endroit, sans rotation : son axe Y est celui du poing). Rangée :
        /// <paramref name="holsterOffset"/> dans le repère horizontal de la tête (x vers la droite), tournée de
        /// <paramref name="holsterRotation"/>.
        /// </summary>
        public static FloatingGrab Create(GameObject root, Vector3 gripPosition, Quaternion attachRotation, Vector3 boxCenter, Vector3 boxSize,
                                          Vector3 holsterOffset, Quaternion holsterRotation)
        {
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;

            // La boîte se met avant l'interactable : il prend les colliders présents à sa création.
            var box = root.AddComponent<BoxCollider>();
            box.center = boxCenter;
            box.size = boxSize;

            var attach = new GameObject("Prise").transform;
            attach.SetParent(root.transform, false);
            attach.localPosition = gripPosition;
            attach.localRotation = attachRotation;

            var point = new GameObject("Poignée").transform;
            point.SetParent(root.transform, false);
            point.localPosition = gripPosition;

            var floating = root.AddComponent<FloatingGrab>();
            floating.m_Colliders = new Collider[] { box };
            floating.m_HolsterOffset = holsterOffset;
            floating.m_HolsterRotation = holsterRotation;

            var grab = root.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = false;
            grab.farAttachMode = InteractableFarAttachMode.Near;
            grab.attachTransform = attach;
            grab.selectEntered.AddListener(args => floating.OnGrabbed(args.interactorObject));
            grab.selectExited.AddListener(_ => floating.OnReleased());
            grab.activated.AddListener(args => floating.Activated?.Invoke(args.interactorObject));
            floating.m_Grab = grab;

            root.AddComponent<HandGrip>().Point = point;
            ArrowIgnore.Register(floating.m_Colliders);
            return floating;
        }

        void OnEnable() => Application.onBeforeRender += OnBeforeRender;

        void OnDisable() => Application.onBeforeRender -= OnBeforeRender;

        void OnDestroy() => ArrowIgnore.Unregister(m_Colliders);

        void OnGrabbed(IXRSelectInteractor interactor)
        {
            m_Holstered = false;
            m_Following = false;
            Grabbed?.Invoke(interactor);
        }

        void OnReleased()
        {
            if (Held)
                return;
            m_NeedsIgnore = true;
            Released?.Invoke();
        }

        /// <summary>La main la lâche (rangement, changement d'arme).</summary>
        public void Release()
        {
            if (Held && m_Grab.interactionManager != null)
                m_Grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)m_Grab);
        }

        /// <summary>Suspendue : on ne peut plus la prendre et elle ne retourne plus à sa place (son propriétaire la place).</summary>
        public void Suspend(bool suspended)
        {
            if (suspended == m_Suspended)
                return;
            m_Suspended = suspended;
            if (suspended)
                Release();
            if (m_Grab != null)
                m_Grab.enabled = !suspended;
            foreach (var collider in m_Colliders)
            {
                if (collider != null)
                    collider.enabled = !suspended;
            }

            if (!suspended)
            {
                m_NeedsIgnore = true;
                SnapToHolster();
            }
        }

        /// <summary>À sa place, tout de suite.</summary>
        public void SnapToHolster()
        {
            if (!TryHolsterPose(out var target))
                return;
            transform.SetPositionAndRotation(target.position, target.rotation);
            m_LastHolster = target;
            m_Following = true;
            m_Holstered = true;
        }

        void Update()
        {
            if (m_Suspended || Held)
                return;

            if (m_NeedsIgnore)
                IgnoreBody();
            UpdateHolster(Time.deltaTime);
        }

        // Juste avant l'image, avec la tête à sa place la plus récente : rangée, elle ne traîne pas d'une image.
        void OnBeforeRender()
        {
            if (!m_Suspended && !Held && isActiveAndEnabled)
                UpdateHolster(0f);
        }

        // Le joueur rend ses collisions à ce qu'il lâche, et Unity les oublie quand un collider se désactive : on les recoupe.
        void IgnoreBody()
        {
            m_NeedsIgnore = false;
            if (m_PlayerBody == null)
            {
                var rig = PlayerRig.Instance;
                m_PlayerBody = rig != null && rig.Origin != null ? rig.Origin.GetComponentInChildren<CharacterController>(true) : null;
            }

            if (m_PlayerBody == null || !m_PlayerBody.enabled || !m_PlayerBody.gameObject.activeInHierarchy)
                return;
            foreach (var collider in m_Colliders)
            {
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy)
                    Physics.IgnoreCollision(m_PlayerBody, collider, true);
            }
        }

        bool TryHolsterPose(out Pose pose)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
            {
                pose = default;
                return false;
            }

            var yaw = rig.HeadYaw;
            pose = new Pose(head.position + yaw * m_HolsterOffset, yaw * m_HolsterRotation);
            return true;
        }

        // Comme l'arc : elle revient dans le repère du joueur (elle suit ses déplacements sans traîner), puis reste à sa place.
        void UpdateHolster(float deltaTime)
        {
            if (!TryHolsterPose(out var target))
                return;

            if (m_Following)
            {
                var turn = target.rotation * Quaternion.Inverse(m_LastHolster.rotation);
                transform.SetPositionAndRotation(target.position + turn * (transform.position - m_LastHolster.position), turn * transform.rotation);
            }

            m_LastHolster = target;
            m_Following = true;
            if (m_Holstered)
            {
                transform.SetPositionAndRotation(target.position, target.rotation);
                return;
            }

            if (deltaTime <= 0f)
                return;
            var blend = 1f - Mathf.Exp(-k_ReturnSpeed * deltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, target.position, blend), Quaternion.Slerp(transform.rotation, target.rotation, blend));
            if ((transform.position - target.position).sqrMagnitude < 0.0004f)
                m_Holstered = true;
        }
    }
}
