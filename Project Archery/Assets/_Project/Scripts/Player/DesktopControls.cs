using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;

namespace Archery.Player
{
    /// <summary>
    /// Se promener sur la carte sans casque : ZQSD (WASD en QWERTY) pour avancer, la souris (clic droit maintenu)
    /// pour regarder, Maj pour courir, Espace pour sauter.
    /// </summary>
    /// <remarks>
    /// Actif seulement quand aucun casque n'affiche l'image (ou si « Toujours actif » est coché). La caméra est alors
    /// tenue à hauteur d'yeux, et les déplacements d'XRI (course aux bras, gravité, téléportation…) sont mis en pause :
    /// le corps (Character Controller du XR Origin) est déplacé directement, sans traverser les murs.
    /// Dès qu'un casque se branche, tout revient comme avant.
    /// </remarks>
    [AddComponentMenu("Archery/Desktop Controls")]
    [DisallowMultipleComponent]
    public class DesktopControls : MonoBehaviour
    {
        [Tooltip("Actif même avec un casque (pour tester au clavier).")]
        [SerializeField]
        bool m_AlwaysActive;

        [Tooltip("Hauteur des yeux au-dessus du sol (m).")]
        [SerializeField]
        float m_EyeHeight = 1.65f;

        [Tooltip("Vitesse de marche (m/s).")]
        [SerializeField]
        float m_WalkSpeed = 4f;

        [Tooltip("Vitesse en courant, Maj enfoncée (m/s).")]
        [SerializeField]
        float m_RunSpeed = 9f;

        [Tooltip("Vitesse verticale au départ d'un saut (m/s). 6 permet de monter sur un demi-bloc (1,5 m).")]
        [SerializeField]
        float m_JumpSpeed = 6f;

        [Tooltip("Degrés par point de déplacement de la souris.")]
        [SerializeField]
        float m_MouseSensitivity = 0.15f;

        [Tooltip("Ne tourner la tête qu'en gardant le clic droit enfoncé (sinon, la souris tourne tout le temps).")]
        [SerializeField]
        bool m_LookWithRightButton = true;

        const float k_CheckInterval = 1f;
        const float k_Gravity = -18f;

        static readonly List<XRDisplaySubsystem> s_Displays = new List<XRDisplaySubsystem>();

        readonly List<Behaviour> m_Paused = new List<Behaviour>();
        XROrigin m_Origin;
        CharacterController m_Body;
        Transform m_Camera;
        float m_Pitch;
        float m_VerticalSpeed;
        float m_NextCheck;

        /// <summary>Vrai quand on se déplace au clavier et à la souris.</summary>
        public bool IsActive { get; private set; }

        void Update()
        {
            if (Time.unscaledTime >= m_NextCheck)
            {
                m_NextCheck = Time.unscaledTime + k_CheckInterval;
                var wanted = m_AlwaysActive || !HeadsetActive();
                if (wanted != IsActive)
                    SetActive(wanted);
            }

            if (IsActive)
                Drive();
        }

        void OnDisable()
        {
            if (IsActive)
                SetActive(false);
        }

        static bool HeadsetActive()
        {
            SubsystemManager.GetSubsystems(s_Displays);
            foreach (var display in s_Displays)
            {
                if (display.running)
                    return true;
            }

            return false;
        }

        void SetActive(bool active)
        {
            if (active)
            {
                var rig = PlayerRig.Instance;
                m_Origin = rig != null ? rig.Origin : FindAnyObjectByType<XROrigin>();
                if (m_Origin == null || m_Origin.Camera == null)
                    return;

                m_Camera = m_Origin.Camera.transform;
                m_Body = m_Origin.GetComponentInChildren<CharacterController>(true);

                // La caméra ne suit plus le casque, et XRI ne déplace plus le corps.
                m_Paused.Clear();
                foreach (var driver in m_Camera.GetComponents<TrackedPoseDriver>())
                    Pause(driver);
                foreach (var provider in m_Origin.GetComponentsInChildren<LocomotionProvider>(true))
                    Pause(provider);

                m_Pitch = 0f;
                m_VerticalSpeed = 0f;
                PlaceCamera();
                IsActive = true;
                Debug.Log("DesktopControls : pas de casque, déplacement au clavier (ZQSD, clic droit pour regarder, Maj, Espace).", this);
            }
            else
            {
                foreach (var behaviour in m_Paused)
                {
                    if (behaviour != null)
                        behaviour.enabled = true;
                }

                m_Paused.Clear();
                IsActive = false;
            }
        }

        void Pause(Behaviour behaviour)
        {
            if (!behaviour.enabled)
                return;
            behaviour.enabled = false;
            m_Paused.Add(behaviour);
        }

        // Caméra au-dessus des pieds du XR Origin, à hauteur d'yeux, inclinée du tangage de la souris.
        void PlaceCamera()
        {
            var parent = m_Camera.parent;
            var parentHeight = parent != null ? m_Origin.transform.InverseTransformPoint(parent.position).y : 0f;
            m_Camera.localPosition = new Vector3(0f, m_EyeHeight - parentHeight, 0f);
            m_Camera.localRotation = Quaternion.Euler(m_Pitch, 0f, 0f);
        }

        void Drive()
        {
            if (m_Origin == null || m_Camera == null)
            {
                SetActive(false);
                return;
            }

            var dt = Time.deltaTime;
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            // Regarder : la souris tourne le XR Origin (gauche-droite) et incline la caméra (haut-bas).
            if (mouse != null && (!m_LookWithRightButton || mouse.rightButton.isPressed))
            {
                var delta = mouse.delta.ReadValue() * m_MouseSensitivity;
                m_Origin.RotateAroundCameraUsingOriginUp(delta.x);
                m_Pitch = Mathf.Clamp(m_Pitch - delta.y, -85f, 85f);
            }

            PlaceCamera();

            // Avancer : touches à l'emplacement de W, A, S, D (Z, Q, S, D sur un clavier AZERTY), ou les flèches.
            var input = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                    input.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                    input.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                    input.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                    input.x -= 1f;
            }

            var running = keyboard != null && keyboard.leftShiftKey.isPressed;
            var forward = Vector3.ProjectOnPlane(m_Origin.transform.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, forward);
            var move = (forward * input.y + right * input.x);
            if (move.sqrMagnitude > 1f)
                move.Normalize();
            move *= running ? m_RunSpeed : m_WalkSpeed;

            if (m_Body == null || !m_Body.enabled)
            {
                m_Origin.transform.position += move * dt;
                return;
            }

            if (m_Body.isGrounded)
            {
                m_VerticalSpeed = -1f;
                if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                    m_VerticalSpeed = m_JumpSpeed;
            }
            else
            {
                m_VerticalSpeed += k_Gravity * dt;
            }

            m_Body.Move((move + Vector3.up * m_VerticalSpeed) * dt);
        }
    }
}
