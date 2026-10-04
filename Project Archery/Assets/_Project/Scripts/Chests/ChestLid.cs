using System;
using Archery.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Chests
{
    /// <summary>
    /// Couvercle d'un coffre, à soulever à la main (GDD, section 13) : on l'attrape avec la poignée, et il suit la main
    /// en tournant autour de sa charnière. Passé un certain angle, le coffre s'ouvre et le couvercle finit de s'ouvrir seul.
    /// Lâché avant, il retombe.
    /// </summary>
    /// <remarks>
    /// À placer sur le couvercle, avec son collider. Le couvercle doit être l'enfant d'un objet « charnière » placé sur
    /// l'arête arrière du coffre : la charnière tourne autour de son axe X (rouge), et le couvercle fermé part vers son axe Z (bleu).
    /// </remarks>
    public class ChestLid : XRBaseInteractable
    {
        [Tooltip("Charnière du couvercle. Vide : le parent du couvercle.")]
        [SerializeField]
        Transform m_Hinge;

        [Tooltip("Angle d'ouverture maximal (°).")]
        [SerializeField]
        float m_MaxAngle = 110f;

        [Tooltip("Angle (°) à partir duquel le coffre s'ouvre.")]
        [SerializeField]
        float m_OpenAngle = 45f;

        [Tooltip("Distance maximale (m) entre la main et le couvercle : il faut le prendre à la main, pas au rayon.")]
        [SerializeField]
        float m_MaxGrabDistance = 0.45f;

        [Tooltip("Vitesse (°/s) à laquelle le couvercle finit de s'ouvrir, ou retombe.")]
        [SerializeField]
        float m_SwingSpeed = 240f;

        Quaternion m_ClosedRotation;
        IXRSelectInteractor m_Hand;
        float m_GrabOffset;

        /// <summary>Le coffre vient de s'ouvrir (une seule fois).</summary>
        public event Action Opened;

        /// <summary>Angle actuel du couvercle (0 = fermé).</summary>
        public float Angle { get; private set; }

        public bool IsOpen { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (m_Hinge == null)
                m_Hinge = transform.parent;
            if (m_Hinge != null)
                m_ClosedRotation = m_Hinge.localRotation;
        }

        // Une fois le coffre ouvert, XRI lâche tout seul le couvercle : la main est libre pour attraper une orbe.
        public override bool IsSelectableBy(IXRSelectInteractor interactor) =>
            base.IsSelectableBy(interactor) && !IsOpen && IsNear(interactor, interactor == m_Hand ? 2f : 1f);

        // La main (son point d'attache) doit presque toucher le couvercle ; une fois qu'elle le tient, on lui laisse plus de marge.
        bool IsNear(IXRInteractor interactor, float slack)
        {
            var attach = interactor.GetAttachTransform(this);
            if (attach == null)
                return false;

            var hand = attach.position;
            var maxDistance = m_MaxGrabDistance * slack;
            foreach (var collider in colliders)
            {
                if (collider != null && collider.enabled && (collider.ClosestPoint(hand) - hand).sqrMagnitude <= maxDistance * maxDistance)
                    return true;
            }

            return false;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            m_Hand = args.interactorObject;
            m_GrabOffset = Angle - HandAngle(m_Hand);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            if (args.interactorObject == m_Hand)
                m_Hand = null;
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic || m_Hinge == null)
                return;

            if (m_Hand != null && !IsOpen)
            {
                Angle = Mathf.Clamp(HandAngle(m_Hand) + m_GrabOffset, 0f, m_MaxAngle);
                if (Angle >= m_OpenAngle)
                    Open();
            }
            else
            {
                // Ouvert : il finit de s'ouvrir. Lâché avant : il retombe.
                var target = IsOpen ? m_MaxAngle : 0f;
                Angle = Mathf.MoveTowards(Angle, target, m_SwingSpeed * Time.deltaTime);
            }

            m_Hinge.localRotation = m_ClosedRotation * Quaternion.Euler(-Angle, 0f, 0f);
        }

        void Open()
        {
            IsOpen = true;
            Haptics.Pulse(m_Hand, 0.6f, 0.12f);
            m_Hand = null;
            Opened?.Invoke();
        }

        // Angle de la main autour de la charnière : 0° devant (couvercle fermé), 90° au-dessus.
        float HandAngle(IXRSelectInteractor interactor)
        {
            var attach = interactor.GetAttachTransform(this);
            if (attach == null)
                return Angle;

            var parent = m_Hinge.parent;
            var local = (parent != null ? parent.InverseTransformPoint(attach.position) : attach.position) - m_Hinge.localPosition;
            local = Quaternion.Inverse(m_ClosedRotation) * local;
            return Mathf.Atan2(local.y, local.z) * Mathf.Rad2Deg;
        }
    }
}
