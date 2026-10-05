using System;
using Archery.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Chests
{
    /// <summary>
    /// Couvercle d'un coffre, à soulever à la main (GDD, section 13) : on l'attrape, et il suit la main
    /// en tournant autour de sa charnière. Passé un certain angle, le coffre s'ouvre et le couvercle finit de s'ouvrir seul.
    /// Lâché avant, il retombe.
    /// </summary>
    /// <remarks>
    /// À placer sur l'objet qui porte le collider du couvercle, enfant de la charnière : l'os du couvercle d'un modèle animé,
    /// ou un objet vide sur l'arête arrière du coffre. La charnière tourne autour de son axe X (rouge), qui pointe vers
    /// la droite du coffre ; le couvercle fermé part vers l'avant.
    /// Avec un modèle animé, son Animator s'arrête pendant qu'on tient le couvercle, puis son animation d'ouverture prend le relais.
    /// </remarks>
    public class ChestLid : XRBaseInteractable
    {
        [Tooltip("Charnière du couvercle, ou os du couvercle d'un modèle animé. Vide : le parent de cet objet.")]
        [SerializeField]
        Transform m_Hinge;

        [Tooltip("Angle d'ouverture maximal (°) quand ce script finit d'ouvrir le couvercle, sans animation d'ouverture.")]
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

        [Header("Modèle animé (optionnel)")]
        [Tooltip("Animator du modèle : il s'arrête pendant qu'on tient le couvercle, puis joue l'état d'ouverture. Vide : pas d'animation.")]
        [SerializeField]
        Animator m_Animator;

        [Tooltip("État de l'Animator qui ouvre le couvercle.")]
        [SerializeField]
        string m_OpenState = "Opening";

        [Tooltip("Moment de l'état d'ouverture (0 à 1) d'où il part : celui où son couvercle atteint à peu près l'angle d'ouverture.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_OpenStateStart = 0.62f;

        Quaternion m_ClosedRotation;
        IXRSelectInteractor m_Hand;
        float m_GrabOffset;
        bool m_Animated;

        /// <summary>Le coffre vient de s'ouvrir (une seule fois).</summary>
        public event Action Opened;

        /// <summary>Angle actuel du couvercle (0 = fermé), tant que l'animation d'ouverture n'a pas pris le relais.</summary>
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

            // Le modèle s'immobilise sous la main.
            if (m_Animator != null)
                m_Animator.speed = 0f;
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            if (args.interactorObject != m_Hand)
                return;

            m_Hand = null;
            if (m_Animator != null)
                m_Animator.speed = 1f;
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic || m_Hinge == null || m_Animated)
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
        }

        // Après l'Animator, qui écraserait sinon la rotation du couvercle.
        void LateUpdate()
        {
            if (m_Hinge == null || m_Animated)
                return;

            // Fermé et lâché, le couvercle d'un modèle animé est laissé à son animation d'attente.
            if (m_Animator != null && m_Hand == null && Angle <= 0f)
                return;

            m_Hinge.localRotation = m_ClosedRotation * Quaternion.Euler(-Angle, 0f, 0f);
        }

        void Open()
        {
            IsOpen = true;
            Haptics.Pulse(m_Hand, 0.6f, 0.12f);
            m_Hand = null;
            PlayOpenAnimation();
            Opened?.Invoke();
        }

        // L'animation d'ouverture du modèle reprend le couvercle à peu près où la main l'a laissé, et finit de l'ouvrir.
        void PlayOpenAnimation()
        {
            if (m_Animator == null)
                return;

            m_Animator.speed = 1f;
            var state = Animator.StringToHash(m_OpenState);
            if (!m_Animator.HasState(0, state))
            {
                Debug.LogWarning($"ChestLid : l'Animator n'a pas d'état « {m_OpenState} », le script finit d'ouvrir le couvercle.", this);
                return;
            }

            m_Animator.Play(state, 0, m_OpenStateStart);
            m_Animated = true;
        }

        // Angle de la main autour de la charnière, dans le plan où tourne le couvercle : 0° devant, 90° au-dessus.
        float HandAngle(IXRSelectInteractor interactor)
        {
            var attach = interactor.GetAttachTransform(this);
            if (attach == null)
                return Angle;

            var parent = m_Hinge.parent;
            var axis = (parent != null ? parent.rotation : Quaternion.identity) * m_ClosedRotation * Vector3.right;
            var forward = Vector3.Cross(axis, Vector3.up).normalized;
            var up = Vector3.Cross(forward, axis);
            var hand = attach.position - m_Hinge.position;
            return Mathf.Atan2(Vector3.Dot(hand, up), Vector3.Dot(hand, forward)) * Mathf.Rad2Deg;
        }
    }
}
