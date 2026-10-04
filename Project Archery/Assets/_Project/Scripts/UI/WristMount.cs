using Archery.Player;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.UI
{
    /// <summary>
    /// Accroche cet objet (par exemple un Canvas en World Space) au poignet de la main
    /// qui ne tient pas l'arc, comme une montre. Il change de poignet si l'arc change de main.
    /// </summary>
    public class WristMount : MonoBehaviour
    {
        [Tooltip("Position par rapport à la manette droite (X est inversé pour la main gauche).")]
        [SerializeField]
        Vector3 m_LocalPosition = new Vector3(0f, 0.05f, -0.12f);

        [Tooltip("Rotation par rapport à la manette droite (Y et Z sont inversés pour la main gauche).")]
        [SerializeField]
        Vector3 m_LocalEulerAngles = new Vector3(75f, 0f, 0f);

        [Tooltip("Coché : sur la main qui ne tient pas l'arc. Décoché : sur la main de l'arc.")]
        [SerializeField]
        bool m_UseFreeHand = true;

        Transform m_CurrentHand;

        void LateUpdate()
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
                return;

            var bowHand = rig.BowHand;
            var handedness = m_UseFreeHand
                ? (bowHand == InteractorHandedness.Left ? InteractorHandedness.Right : InteractorHandedness.Left)
                : bowHand;

            if (!rig.TryGetHand(handedness, out var hand) || hand.transform == m_CurrentHand)
                return;

            // On devient enfant de la manette : on la suit sans aucun retard.
            m_CurrentHand = hand.transform;
            transform.SetParent(m_CurrentHand, false);

            var position = m_LocalPosition;
            var euler = m_LocalEulerAngles;
            if (handedness == InteractorHandedness.Left)
            {
                position.x = -position.x;
                euler.y = -euler.y;
                euler.z = -euler.z;
            }

            transform.localPosition = position;
            transform.localRotation = Quaternion.Euler(euler);
        }
    }
}
