using System;
using System.Collections.Generic;
using Archery.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Chests
{
    /// <summary>
    /// Couvercle d'un coffre (GDD, section 13). On vise le coffre (couvercle ou caisse) et on appuie sur la gâchette
    /// de prise, à quelques mètres au plus, ou on l'attrape de près : il s'ouvre d'un coup.
    /// </summary>
    /// <remarks>
    /// À placer sur l'objet qui porte le collider du couvercle, enfant de la charnière : l'os du couvercle d'un modèle animé,
    /// ou un objet vide sur l'arête arrière du coffre. Les colliders de la caisse (sur l'objet du <see cref="Chest"/>)
    /// comptent aussi : viser n'importe quelle partie du coffre suffit.
    /// Avec un modèle animé, son animation d'ouverture joue ; sinon, le couvercle tourne autour de l'axe X (rouge)
    /// de la charnière.
    /// </remarks>
    public class ChestLid : XRBaseInteractable
    {
        [Tooltip("Charnière du couvercle, ou os du couvercle d'un modèle animé. Vide : le parent de cet objet.")]
        [SerializeField]
        Transform m_Hinge;

        [Tooltip("Angle d'ouverture (°) quand ce script ouvre le couvercle, sans animation d'ouverture.")]
        [SerializeField]
        float m_MaxAngle = 110f;

        [Tooltip("Distance maximale (m) entre la main et le coffre pour l'ouvrir en le visant.")]
        [SerializeField]
        float m_MaxOpenDistance = 5f;

        [Tooltip("Vitesse (°/s) à laquelle le couvercle s'ouvre, sans animation d'ouverture.")]
        [SerializeField]
        float m_SwingSpeed = 240f;

        [Header("Modèle animé (optionnel)")]
        [Tooltip("Animator du modèle : il joue l'état d'ouverture. Vide : ce script ouvre le couvercle.")]
        [SerializeField]
        Animator m_Animator;

        [Tooltip("État de l'Animator qui ouvre le couvercle.")]
        [SerializeField]
        string m_OpenState = "Opening";

        Quaternion m_ClosedRotation;
        bool m_Animated;

        /// <summary>Le coffre vient de s'ouvrir (une seule fois).</summary>
        public event Action Opened;

        /// <summary>Angle actuel du couvercle (0 = fermé), sans animation d'ouverture.</summary>
        public float Angle { get; private set; }

        public bool IsOpen { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            if (m_Hinge == null)
                m_Hinge = transform.parent;
            if (m_Hinge != null)
                m_ClosedRotation = m_Hinge.localRotation;

            // La caisse compte aussi : on vise le coffre, pas seulement son couvercle.
            var chest = GetComponentInParent<Chest>();
            if (chest != null)
            {
                foreach (var collider in chest.GetComponents<Collider>())
                {
                    if (!collider.isTrigger && !colliders.Contains(collider))
                        colliders.Add(collider);
                }
            }
        }

        // Une fois le coffre ouvert, XRI lâche tout seul le couvercle : la main est libre pour prendre une orbe.
        public override bool IsSelectableBy(IXRSelectInteractor interactor) =>
            base.IsSelectableBy(interactor) && !IsOpen && IsInReach(interactor);

        // La main (la manette, pas le point visé) doit être à moins de quelques mètres du coffre.
        bool IsInReach(IXRInteractor interactor)
        {
            var hand = interactor.transform.position;
            foreach (var collider in colliders)
            {
                if (collider != null && collider.enabled &&
                    (collider.ClosestPoint(hand) - hand).sqrMagnitude <= m_MaxOpenDistance * m_MaxOpenDistance)
                    return true;
            }

            return false;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            if (IsOpen)
                return;

            IsOpen = true;
            Haptics.Pulse(args.interactorObject, 0.6f, 0.12f);
            PlayOpenAnimation();
            Opened?.Invoke();
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic || m_Hinge == null || m_Animated || !IsOpen)
                return;

            Angle = Mathf.MoveTowards(Angle, m_MaxAngle, m_SwingSpeed * Time.deltaTime);
        }

        // Après l'Animator, qui écraserait sinon la rotation du couvercle.
        void LateUpdate()
        {
            if (m_Hinge == null || m_Animated || Angle <= 0f)
                return;

            m_Hinge.localRotation = m_ClosedRotation * Quaternion.Euler(-Angle, 0f, 0f);
        }

        void PlayOpenAnimation()
        {
            if (m_Animator == null)
                return;

            var state = Animator.StringToHash(m_OpenState);
            if (!m_Animator.HasState(0, state))
            {
                Debug.LogWarning($"ChestLid : l'Animator n'a pas d'état « {m_OpenState} », le script ouvre le couvercle.", this);
                return;
            }

            m_Animator.speed = 1f;
            m_Animator.Play(state, 0, 0f);
            m_Animated = true;
        }
    }
}
