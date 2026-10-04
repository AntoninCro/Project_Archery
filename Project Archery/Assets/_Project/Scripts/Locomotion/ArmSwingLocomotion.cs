using System.Collections.Generic;
using Archery.Core;
using Archery.Player;
using Archery.Save;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace Archery.Locomotion
{
    /// <summary>
    /// Marche, course aux bras et slide (GDD, section 12).
    /// <list type="bullet">
    /// <item>Marche : le joystick gauche donne la direction, par rapport au regard (en arrière, on recule).</item>
    /// <item>Course : balancer les deux bras en marchant multiplie la vitesse, jusqu'à ×3.</item>
    /// <item>Slide : A ou X pendant la course. On repart plus vite, puis on glisse environ 2,5 s,
    /// les mains libres pour tirer, avec une vignette de confort réglable dans les paramètres.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// C'est un Locomotion Provider d'XRI : il passe par le Locomotion Mediator du XR Origin. Il profite donc du
    /// Character Controller (on ne traverse pas les murs) et de la gravité. Il remplace le déplacement au joystick
    /// d'XRI (« Move »), qu'il désactive tant qu'il est actif.
    /// </remarks>
    [AddComponentMenu("Archery/Arm Swing Locomotion")]
    public class ArmSwingLocomotion : LocomotionProvider, ITunnelingVignetteProvider
    {
        [Header("Marche")]
        [Tooltip("Joystick de déplacement. Lu directement sur la manette ; la liaison se change ici.")]
        [SerializeField]
        XRInputValueReader<Vector2> m_MoveInput = ControllerStick("Move", "<XRController>{LeftHand}/{Primary2DAxis}");

        [Tooltip("Vitesse de marche (m/s), joystick poussé à fond, sans balancer les bras.")]
        [SerializeField]
        float m_WalkSpeed = 2.5f;

        [Tooltip("En dessous de cette inclinaison (0 à 1), le joystick est ignoré.")]
        [Range(0f, 0.5f)]
        [SerializeField]
        float m_StickDeadZone = 0.15f;

        [Tooltip("Lissage (s) de la direction du regard : la course ne tangue pas avec la tête.")]
        [SerializeField]
        float m_DirectionSmoothing = 0.2f;

        [Tooltip("Remplace le déplacement au joystick d'XRI (« Move »), désactivé tant que ce script est actif.")]
        [SerializeField]
        bool m_ReplaceJoystickMove = true;

        [Header("Course aux bras")]
        [Tooltip("Vitesse de marche multipliée par cette valeur quand on balance les bras à fond (3 = trois fois plus vite).")]
        [SerializeField]
        float m_MaxRunMultiplier = 3f;

        [Tooltip("Vitesse des mains (m/s) en dessous de laquelle les bras ne comptent pas.")]
        [SerializeField]
        float m_HandSpeedDeadZone = 0.35f;

        [Tooltip("Vitesse des mains (m/s) qui donne la course la plus rapide.")]
        [SerializeField]
        float m_FullSwingHandSpeed = 2.2f;

        [Tooltip("Accélération (m/s²).")]
        [SerializeField]
        float m_AccelerationRate = 12f;

        [Tooltip("Freinage (m/s²) quand on ralentit, qu'on change de direction ou qu'on lâche le joystick.")]
        [SerializeField]
        float m_BrakingRate = 16f;

        [Tooltip("Ne courir qu'en gardant une gâchette enfoncée (sinon, il suffit de balancer les deux bras).")]
        [SerializeField]
        bool m_RequireTrigger;

        [Tooltip("Gâchette gauche, si « Require Trigger » est coché.")]
        [SerializeField]
        XRInputButtonReader m_LeftRunInput = ControllerButton("Left Run", "<XRController>{LeftHand}/{TriggerButton}");

        [Tooltip("Gâchette droite, si « Require Trigger » est coché.")]
        [SerializeField]
        XRInputButtonReader m_RightRunInput = ControllerButton("Right Run", "<XRController>{RightHand}/{TriggerButton}");

        [Header("Slide")]
        [Tooltip("Bouton X : glisser pendant la course.")]
        [SerializeField]
        XRInputButtonReader m_LeftSlideInput = ControllerButton("Left Slide", "<XRController>{LeftHand}/{PrimaryButton}");

        [Tooltip("Bouton A : glisser pendant la course.")]
        [SerializeField]
        XRInputButtonReader m_RightSlideInput = ControllerButton("Right Slide", "<XRController>{RightHand}/{PrimaryButton}");

        [Tooltip("Vitesse minimale (m/s) pour glisser : il faut courir, pas seulement marcher.")]
        [SerializeField]
        float m_SlideMinimumSpeed = 3.5f;

        [Tooltip("Durée du slide (s).")]
        [SerializeField]
        float m_SlideTime = 2.5f;

        [Tooltip("Vitesse au début du slide, par rapport à la course (1,4 = 40 % plus vite).")]
        [SerializeField]
        float m_SlideSpeedBoost = 1.4f;

        [Tooltip("Vitesse maximale (m/s) au début du slide.")]
        [SerializeField]
        float m_MaxSlideSpeed = 10f;

        [Tooltip("Temps (s) après un slide avant de pouvoir en refaire un.")]
        [SerializeField]
        float m_SlideCooldown = 0.4f;

        [Header("Retours")]
        [Tooltip("Optionnel : son du slide.")]
        [SerializeField]
        AudioClip m_SlideClip;

        [Tooltip("Optionnel : son d'un pas.")]
        [SerializeField]
        AudioClip m_StepClip;

        [Tooltip("Distance (m) entre deux pas à l'arrêt ; les foulées s'allongent avec la vitesse.")]
        [SerializeField]
        float m_StepLength = 1.4f;

        [Header("Confort")]
        [Tooltip("Vignette de confort (prefab TunnelingVignette d'XRI, sous la caméra). Vide : cherchée sous le XR Origin.")]
        [SerializeField]
        TunnelingVignetteController m_Vignette;

        [Tooltip("Vignette aussi pendant la course, pas seulement pendant le slide.")]
        [SerializeField]
        bool m_VignetteWhileRunning;

        [Tooltip("Ouverture de la vignette au réglage le plus fort des paramètres (1 = pas de vignette, 0 = vue fermée).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_StrongestAperture = 0.4f;

        readonly XROriginMovement m_Movement = new XROriginMovement();
        readonly VignetteParameters m_VignetteParameters = new VignetteParameters();
        readonly List<ContinuousMoveProvider> m_ReplacedProviders = new List<ContinuousMoveProvider>();
        PlayerRig m_Rig;
        GravityProvider m_Gravity;
        Vector3 m_LeftPrevious;
        Vector3 m_RightPrevious;
        bool m_HasPrevious;
        float m_HandSpeed;
        float m_SmoothedYaw;
        bool m_HasYaw;
        Vector3 m_Velocity;
        Vector3 m_SlideDirection;
        float m_SlideTimeLeft;
        float m_SlideStartSpeed;
        float m_SlideCooldownTimer;
        float m_StepDistance;
        bool m_VignetteOn;

        /// <summary>Les bras accélèrent la marche en ce moment.</summary>
        public bool IsRunning { get; private set; }

        public bool IsSliding => m_SlideTimeLeft > 0f;

        /// <summary>Vitesse horizontale actuelle (m/s).</summary>
        public float Speed => m_Velocity.magnitude;

        /// <summary>Réglages de la vignette demandés à la Tunneling Vignette (ITunnelingVignetteProvider).</summary>
        public VignetteParameters vignetteParameters => m_VignetteParameters;

        protected override void Awake()
        {
            base.Awake();
            m_VignetteParameters.easeInTime = 0.15f;
            m_VignetteParameters.easeOutTime = 0.4f;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_MoveInput.EnableDirectActionIfModeUsed();
            m_LeftRunInput.EnableDirectActionIfModeUsed();
            m_RightRunInput.EnableDirectActionIfModeUsed();
            m_LeftSlideInput.EnableDirectActionIfModeUsed();
            m_RightSlideInput.EnableDirectActionIfModeUsed();
            m_HasPrevious = false;
            m_HasYaw = false;
            ReplaceJoystickMove();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_MoveInput.DisableDirectActionIfModeUsed();
            m_LeftRunInput.DisableDirectActionIfModeUsed();
            m_RightRunInput.DisableDirectActionIfModeUsed();
            m_LeftSlideInput.DisableDirectActionIfModeUsed();
            m_RightSlideInput.DisableDirectActionIfModeUsed();

            m_Velocity = Vector3.zero;
            m_SlideTimeLeft = 0f;
            IsRunning = false;
            SetVignette(false);
            TryEndLocomotion();
            RestoreJoystickMove();
        }

        void Start()
        {
            m_Rig = PlayerRig.Instance;
            if (m_Rig == null)
                Debug.LogWarning("ArmSwingLocomotion : il faut un Player Rig sur le XR Origin pour lire la tête et les mains.", this);

            if (mediator != null)
                m_Gravity = mediator.GetComponentInChildren<GravityProvider>(true);
            if (m_Gravity == null)
                m_Gravity = FindAnyObjectByType<GravityProvider>();

            if (m_Vignette == null && m_Rig != null && m_Rig.Origin != null)
                m_Vignette = m_Rig.Origin.GetComponentInChildren<TunnelingVignetteController>(true);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            var origin = mediator != null ? mediator.xrOrigin : null;
            if (deltaTime <= 0f || origin == null || origin.Origin == null || m_Rig == null)
                return;

            UpdateHandSpeed(origin.Origin.transform, deltaTime);
            UpdateYaw(deltaTime);
            m_SlideCooldownTimer -= deltaTime;

            var grounded = IsGrounded();
            if (IsSliding)
            {
                IsRunning = false;
                UpdateSlide(deltaTime);
            }
            else
            {
                UpdateWalk(grounded, deltaTime);
                if (grounded && Speed >= m_SlideMinimumSpeed && m_SlideCooldownTimer <= 0f && SlidePressed())
                    StartSlide();
            }

            UpdateVignette();
            UpdateSteps(grounded, deltaTime);

            if (m_Velocity.sqrMagnitude > 1e-4f)
            {
                Move(origin.Origin.transform, deltaTime);
            }
            else
            {
                m_Velocity = Vector3.zero;
                TryEndLocomotion();
            }
        }

        // La vitesse visée : le joystick (direction et marche), multipliée par le balancement des bras.
        // On y va en accélérant ou en freinant : en pleine course, un demi-tour freine d'abord. En l'air, on garde son élan.
        void UpdateWalk(bool grounded, float deltaTime)
        {
            if (!grounded)
                return;

            var stick = ReadStick();
            var armFactor = ArmFactor();
            IsRunning = stick.sqrMagnitude > 0f && armFactor > 0.05f;

            var multiplier = 1f + (Mathf.Max(1f, m_MaxRunMultiplier) - 1f) * armFactor;
            var target = Quaternion.Euler(0f, m_SmoothedYaw, 0f) * new Vector3(stick.x, 0f, stick.y) * (m_WalkSpeed * multiplier);
            var rate = target.sqrMagnitude > m_Velocity.sqrMagnitude ? m_AccelerationRate : m_BrakingRate;
            m_Velocity = Vector3.MoveTowards(m_Velocity, target, rate * deltaTime);
        }

        Vector2 ReadStick()
        {
            var stick = Vector2.ClampMagnitude(m_MoveInput.ReadValue(), 1f);
            var magnitude = stick.magnitude;
            if (magnitude <= m_StickDeadZone)
                return Vector2.zero;

            // Au-delà de la zone morte, l'inclinaison repart de 0 pour garder une marche lente possible.
            return stick / magnitude * ((magnitude - m_StickDeadZone) / (1f - m_StickDeadZone));
        }

        // De 0 (bras immobiles) à 1 (balancement à fond).
        float ArmFactor()
        {
            if (m_RequireTrigger && !m_LeftRunInput.ReadIsPerformed() && !m_RightRunInput.ReadIsPerformed())
                return 0f;

            var range = Mathf.Max(0.1f, m_FullSwingHandSpeed - m_HandSpeedDeadZone);
            return Mathf.Clamp01((m_HandSpeed - m_HandSpeedDeadZone) / range);
        }

        // Vitesse de la main la plus lente, par rapport à la tête, dans le repère du XR Origin.
        // Il faut balancer les deux bras : tendre la corde ou prendre une flèche (une seule main) ne fait pas courir.
        // Ni les déplacements du joueur ni ceux du XR Origin ne comptent.
        void UpdateHandSpeed(Transform space, float deltaTime)
        {
            var head = m_Rig.Head;
            if (head == null || !TryGetHands(out var left, out var right))
            {
                m_HandSpeed = 0f;
                m_HasPrevious = false;
                return;
            }

            var headLocal = space.InverseTransformPoint(head.position);
            var leftLocal = space.InverseTransformPoint(left.position) - headLocal;
            var rightLocal = space.InverseTransformPoint(right.position) - headLocal;
            if (m_HasPrevious)
            {
                var speed = Mathf.Min((leftLocal - m_LeftPrevious).magnitude, (rightLocal - m_RightPrevious).magnitude) / deltaTime;

                // Filtre : la vitesse des mains varie beaucoup au fil d'un balancement.
                m_HandSpeed = Mathf.Lerp(m_HandSpeed, speed, 1f - Mathf.Exp(-deltaTime / 0.25f));
            }

            m_LeftPrevious = leftLocal;
            m_RightPrevious = rightLocal;
            m_HasPrevious = true;
        }

        // Direction du regard, lissée pour que la course ne tangue pas avec les mouvements de la tête.
        void UpdateYaw(float deltaTime)
        {
            var yaw = m_Rig.HeadYaw.eulerAngles.y;
            if (!m_HasYaw || m_DirectionSmoothing <= 0f)
            {
                m_SmoothedYaw = yaw;
                m_HasYaw = true;
                return;
            }

            m_SmoothedYaw = Mathf.LerpAngle(m_SmoothedYaw, yaw, 1f - Mathf.Exp(-deltaTime / m_DirectionSmoothing));
        }

        bool SlidePressed() => m_LeftSlideInput.ReadWasPerformedThisFrame() || m_RightSlideInput.ReadWasPerformedThisFrame();

        void StartSlide()
        {
            m_SlideTimeLeft = Mathf.Max(0.1f, m_SlideTime);
            m_SlideDirection = m_Velocity.normalized;
            m_SlideStartSpeed = Mathf.Min(Speed * Mathf.Max(1f, m_SlideSpeedBoost), Mathf.Max(Speed, m_MaxSlideSpeed));
            m_Velocity = m_SlideDirection * m_SlideStartSpeed;

            Sfx.Play(m_SlideClip, m_Rig.BodyPosition, 0.8f, Random.Range(0.95f, 1.05f), 0f);
            foreach (var hand in m_Rig.Hands)
                Haptics.Pulse(hand, 0.4f, 0.15f);
        }

        // On garde presque tout l'élan au début, puis on freine de plus en plus (peu de frottement).
        void UpdateSlide(float deltaTime)
        {
            m_SlideTimeLeft = Mathf.Max(0f, m_SlideTimeLeft - deltaTime);
            var progress = 1f - m_SlideTimeLeft / Mathf.Max(0.1f, m_SlideTime);
            m_Velocity = m_SlideDirection * (m_SlideStartSpeed * (1f - progress * progress));

            if (m_SlideTimeLeft <= 0f)
                m_SlideCooldownTimer = m_SlideCooldown;
        }

        void Move(Transform originTransform, float deltaTime)
        {
            TryStartLocomotionImmediately();
            if (locomotionState != LocomotionState.Moving)
                return;

            m_Movement.motion = m_Velocity * (deltaTime * originTransform.localScale.x);
            TryQueueTransformation(m_Movement);
        }

        // Pendant le slide (et la course si demandé), selon le réglage « Confort » des paramètres.
        void UpdateVignette()
        {
            var strength = GameSettings.Data.comfortVignette;
            var running = m_VignetteWhileRunning && Speed > m_WalkSpeed * 1.2f;
            var wanted = strength > 0.01f && (IsSliding || running);
            if (wanted)
                m_VignetteParameters.apertureSize = Mathf.Lerp(1f, m_StrongestAperture, Mathf.Clamp01(strength));
            SetVignette(wanted);
        }

        void SetVignette(bool on)
        {
            if (on == m_VignetteOn || m_Vignette == null)
                return;

            m_VignetteOn = on;
            if (on)
                m_Vignette.BeginTunnelingVignette(this);
            else
                m_Vignette.EndTunnelingVignette(this);
        }

        // Un pas tous les m_StepLength mètres, avec des foulées plus longues en courant.
        void UpdateSteps(bool grounded, float deltaTime)
        {
            var speed = Speed;
            if (m_StepClip == null || !grounded || IsSliding || speed < 0.5f)
            {
                m_StepDistance = 0f;
                return;
            }

            m_StepDistance += speed * deltaTime;
            if (m_StepDistance < m_StepLength * (1f + 0.15f * speed))
                return;

            m_StepDistance = 0f;
            Sfx.Play(m_StepClip, m_Rig.BodyPosition, 0.35f, Random.Range(0.85f, 1.15f), 0f);
        }

        // Le déplacement au joystick d'XRI ferait doublon : il est coupé tant que ce script est actif.
        void ReplaceJoystickMove()
        {
            if (!m_ReplaceJoystickMove || mediator == null)
                return;

            foreach (var provider in mediator.GetComponentsInChildren<ContinuousMoveProvider>(true))
            {
                if (provider.enabled)
                {
                    provider.enabled = false;
                    m_ReplacedProviders.Add(provider);
                }
            }
        }

        void RestoreJoystickMove()
        {
            foreach (var provider in m_ReplacedProviders)
            {
                if (provider != null)
                    provider.enabled = true;
            }

            m_ReplacedProviders.Clear();
        }

        bool IsGrounded() =>
            m_Gravity == null || !m_Gravity.isActiveAndEnabled || !m_Gravity.useGravity || m_Gravity.isGrounded;

        bool TryGetHands(out Transform left, out Transform right)
        {
            left = m_Rig.TryGetHand(InteractorHandedness.Left, out var leftHand) ? leftHand.transform : null;
            right = m_Rig.TryGetHand(InteractorHandedness.Right, out var rightHand) ? rightHand.transform : null;
            return left != null && right != null;
        }

        // Entrées lues directement sur les manettes, sans Input Action Asset. Les liaisons se changent dans l'Inspector.
        static XRInputButtonReader ControllerButton(string name, string bindingPath)
        {
            var reader = new XRInputButtonReader(name, null, false, XRInputButtonReader.InputSourceMode.InputAction);
            reader.inputActionPerformed.AddBinding(bindingPath);
            return reader;
        }

        static XRInputValueReader<Vector2> ControllerStick(string name, string bindingPath)
        {
            var reader = new XRInputValueReader<Vector2>(name, XRInputValueReader.InputSourceMode.InputAction);
            reader.inputAction.AddBinding(bindingPath);
            return reader;
        }
    }
}
