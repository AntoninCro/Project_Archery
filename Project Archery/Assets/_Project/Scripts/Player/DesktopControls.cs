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
    /// Jouer sans casque : ZQSD (WASD en QWERTY) pour avancer, la souris pour regarder, Maj pour courir, Espace pour
    /// sauter. Clic gauche maintenu pour tendre l'arc (<see cref="DesktopArcher"/>), E pour interagir
    /// (<see cref="DesktopHand"/>).
    /// </summary>
    /// <remarks>
    /// Actif seulement quand aucun casque n'affiche l'image (ou si « Toujours actif » est coché). La caméra est alors
    /// tenue à hauteur d'yeux, et les déplacements d'XRI (course aux bras, gravité, téléportation…) sont mis en pause :
    /// le corps (Character Controller du XR Origin) est déplacé directement, sans traverser les murs.
    /// Le curseur est caché et bloqué au centre de l'écran : Échap le libère, un clic gauche dans le jeu le rebloque.
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

        [Tooltip("Cache le curseur et le bloque au centre de l'écran : la souris tourne la tête. Échap le libère, " +
                 "un clic gauche dans le jeu le rebloque. Décoché : la souris tourne la tête sans bloquer le curseur.")]
        [SerializeField]
        bool m_LockCursor = true;

        const float k_CheckInterval = 1f;
        const float k_StartupGrace = 6f;
        const float k_Gravity = -18f;

        static readonly List<XRDisplaySubsystem> s_Displays = new List<XRDisplaySubsystem>();

        readonly List<Behaviour> m_Paused = new List<Behaviour>();
        XROrigin m_Origin;
        CharacterController m_Body;
        Transform m_Camera;
        DesktopArcher m_Archer;
        DesktopHand m_Hand;
        float m_Pitch;
        float m_VerticalSpeed;
        float m_NextCheck;
        float m_StartTime = -1f;
        int m_RefocusFrame = -1;

        public static DesktopControls Instance { get; private set; }

        /// <summary>Vrai quand on joue au clavier et à la souris.</summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Sans casque, le joueur est en l'air (le saut à Espace) : le talisman d'acrobate le compte (27 octobre), pour essayer le
        /// ralenti et le duo « Étoile filante » dans l'éditeur.
        /// </summary>
        public bool IsAirborne => IsActive && m_Body != null && m_Body.enabled && !m_Body.isGrounded;

        /// <summary>Le jeu reçoit la souris (curseur bloqué au centre) : on peut regarder, tirer et interagir.</summary>
        public bool HasFocus => IsActive && (!m_LockCursor || Cursor.lockState == CursorLockMode.Locked);

        /// <summary>Le clic gauche de cette image a repris la souris : il ne tire pas.</summary>
        public bool RefocusedThisFrame => m_RefocusFrame == Time.frameCount;

        /// <summary>La caméra du joueur, tenue à hauteur d'yeux.</summary>
        public Transform CameraTransform => m_Camera;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Desktop Controls dans la scène.", this);
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            // Le délai du casque part de la première image, pas d'Awake : le chargement de la scène peut prendre plus
            // longtemps que le délai lui-même (11 s le 20 octobre), et le jeu passait au clavier, casque sur la tête.
            if (m_StartTime < 0f)
                m_StartTime = Time.unscaledTime;
            if (Time.unscaledTime >= m_NextCheck)
            {
                m_NextCheck = Time.unscaledTime + k_CheckInterval;
                var wanted = m_AlwaysActive || !HeadsetActive(Time.unscaledTime - m_StartTime < k_StartupGrace);
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

        // Au lancement, le casque met quelques secondes à démarrer son affichage : pendant ce délai, un affichage XR
        // créé (le casque a répondu) mais pas encore lancé compte déjà comme un casque.
        static bool HeadsetActive(bool starting)
        {
            SubsystemManager.GetSubsystems(s_Displays);
            foreach (var display in s_Displays)
            {
                if (display.running || starting)
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
                SetHelpersActive(true);
                SetCursorLocked(true);
                Debug.Log("DesktopControls : pas de casque, jeu au clavier (ZQSD, souris, Maj, Espace, clic gauche pour tirer, E pour interagir).", this);
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
                SetHelpersActive(false);
                SetCursorLocked(false);
            }
        }

        // L'arc à la souris et la main de la touche E (avec le viseur) n'existent que sans casque.
        void SetHelpersActive(bool active)
        {
            if (active)
            {
                if (m_Archer == null && !TryGetComponent(out m_Archer))
                    m_Archer = gameObject.AddComponent<DesktopArcher>();
                if (m_Hand == null && m_Camera != null)
                {
                    var handObject = new GameObject("Desktop Hand");
                    handObject.transform.SetParent(m_Camera, false);
                    m_Hand = handObject.AddComponent<DesktopHand>();
                }
            }

            if (m_Archer != null)
                m_Archer.enabled = active;
            if (m_Hand != null)
                m_Hand.gameObject.SetActive(active);
        }

        void SetCursorLocked(bool locked)
        {
            if (!m_LockCursor && locked)
                return;

            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // Échap libère la souris (pour l'éditeur, une autre fenêtre) ; un clic gauche dans le jeu la reprend.
        void UpdateCursor(Keyboard keyboard, Mouse mouse)
        {
            if (!m_LockCursor)
                return;

            var locked = Cursor.lockState == CursorLockMode.Locked;
            if (locked && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);
            else if (!locked && Application.isFocused && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                SetCursorLocked(true);
                m_RefocusFrame = Time.frameCount;
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
            UpdateCursor(keyboard, mouse);

            // Regarder : la souris tourne le XR Origin (gauche-droite) et incline la caméra (haut-bas).
            if (mouse != null && HasFocus)
            {
                var delta = mouse.delta.ReadValue() * m_MouseSensitivity;
                m_Origin.RotateAroundCameraUsingOriginUp(delta.x);
                m_Pitch = Mathf.Clamp(m_Pitch - delta.y, -85f, 85f);
            }

            PlaceCamera();

            // Pendant qu'on écrit son nom (classement), les touches écrivent au lieu de faire marcher.
            if (DesktopHand.IsTypingText)
                keyboard = null;

            // Avancer : touches à l'emplacement de W, A, S, D (Z, Q, S, D sur un clavier AZERTY), ou les flèches.
            // Figé sur place (l'intro du dragon, dans le noir) : on peut encore regarder autour de soi, pas bouger.
            var locked = PlayerRig.IsMovementLocked;
            var input = Vector2.zero;
            if (keyboard != null && !locked)
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
                if (keyboard != null && !locked && keyboard.spaceKey.wasPressedThisFrame)
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
