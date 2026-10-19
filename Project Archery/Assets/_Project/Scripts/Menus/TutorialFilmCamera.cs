using System;
using Archery.Core;
using Archery.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Archery.Menus
{
    /// <summary>
    /// La caméra de tournage des vidéos du tutoriel (dans l'éditeur, créée en mode Play par la fenêtre
    /// <c>Archery > Tutoriel > Tournage des vidéos</c>) : elle suit la tête du joueur en lissant ses petits mouvements
    /// (pas de tremblement, l'horizon reste droit) et filme en 16:9, dans une texture que Unity Recorder enregistre.
    /// Elle n'apparaît pas dans le casque.
    /// </summary>
    /// <remarks>
    /// Les deux joysticks enfoncés en même temps (ou F8 au clavier) démarrent et arrêtent l'enregistrement
    /// (<see cref="ToggleRequested"/>). Elle ne filme que pendant l'enregistrement, une image toutes les
    /// <see cref="FrameStep"/> images du casque (environ 30 par seconde), comme Recorder : chaque image enregistrée est
    /// une nouvelle image. Le casque garde sa fluidité.
    /// </remarks>
    public class TutorialFilmCamera : MonoBehaviour
    {
        public const int Width = 1280;
        public const int Height = 720;

        // Lissage (s) : la position suit vite, l'orientation plus doucement (les petits mouvements de tête disparaissent).
        const float k_PositionSmoothing = 0.08f;
        const float k_RotationSmoothing = 0.3f;

        // Au-delà (m) d'un coup, c'est une téléportation : la caméra saute avec le joueur au lieu de glisser.
        const float k_Teleport = 2f;

        // Champ de vision vertical (°) : environ 95° en largeur, proche de ce qu'on voit dans le casque.
        const float k_FieldOfView = 62f;

        Camera m_Camera;
        RenderTexture m_Texture;
        InputAction m_LeftStick;
        InputAction m_RightStick;
        bool m_BothPressed;
        bool m_Placed;
        bool m_Filming;
        int m_FilmFrame;

        public static TutorialFilmCamera Instance { get; private set; }

        /// <summary>Demande de démarrer ou d'arrêter l'enregistrement (les deux joysticks enfoncés, ou F8).</summary>
        public static event Action ToggleRequested;

        /// <summary>L'image filmée (1280 × 720).</summary>
        public RenderTexture Texture => m_Texture;

        /// <summary>Filme en ce moment (réglé par l'outil d'enregistrement) ; sinon, la caméra ne rend rien.</summary>
        public bool Filming
        {
            get => m_Filming;
            set
            {
                if (value && !m_Filming)
                    m_FilmFrame = 0;
                m_Filming = value;
            }
        }

        /// <summary>
        /// Une image filmée toutes les <c>FrameStep</c> images du casque : la même cadence que Recorder (réglée par l'outil),
        /// pour qu'il n'enregistre jamais deux fois la même image (des copies gâchaient le débit de la vidéo).
        /// </summary>
        public int FrameStep { get; set; } = 1;

        /// <summary>Crée la caméra de tournage (une seule).</summary>
        public static TutorialFilmCamera Create()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("Caméra de tournage (tutoriel)");
            return go.AddComponent<TutorialFilmCamera>();
        }

        void Awake()
        {
            Instance = this;
            m_Texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            {
                name = "Caméra de tournage",
            };
            m_Texture.Create();

            m_Camera = gameObject.AddComponent<Camera>();
            m_Camera.stereoTargetEye = StereoTargetEyeMask.None;
            m_Camera.targetTexture = m_Texture;
            m_Camera.fieldOfView = k_FieldOfView;
            m_Camera.nearClipPlane = 0.03f;
            m_Camera.enabled = false;

            // Comme la caméra du casque : ce qu'elle voit, son fond, ses effets.
            var rig = PlayerRig.Instance;
            var head = rig != null && rig.Head != null ? rig.Head.GetComponent<Camera>() : null;
            var cameraData = m_Camera.GetUniversalAdditionalCameraData();
            if (head != null)
            {
                m_Camera.cullingMask = head.cullingMask;
                m_Camera.clearFlags = head.clearFlags;
                m_Camera.backgroundColor = head.backgroundColor;
                m_Camera.farClipPlane = head.farClipPlane;
                cameraData.renderPostProcessing = head.GetUniversalAdditionalCameraData().renderPostProcessing;
            }

            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            m_LeftStick = new InputAction("Tournage gauche", InputActionType.Button, "<XRController>{LeftHand}/{Primary2DAxisClick}");
            m_RightStick = new InputAction("Tournage droite", InputActionType.Button, "<XRController>{RightHand}/{Primary2DAxisClick}");
        }

        void OnEnable()
        {
            m_LeftStick?.Enable();
            m_RightStick?.Enable();
        }

        void OnDisable()
        {
            m_LeftStick?.Disable();
            m_RightStick?.Disable();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            m_LeftStick?.Dispose();
            m_RightStick?.Dispose();
            if (m_Texture != null)
            {
                m_Texture.Release();
                Destroy(m_Texture);
            }
        }

        void LateUpdate()
        {
            Follow();
            UpdateToggle();

            // Une image toutes les FrameStep images, seulement pendant l'enregistrement.
            var render = m_Filming && m_FilmFrame++ % Mathf.Max(1, FrameStep) == 0;
            m_Camera.enabled = render;
        }

        /// <summary>Signal dans les deux manettes : un coup au début de l'enregistrement, deux à la fin.</summary>
        public void Signal(bool start)
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
                return;
            foreach (var hand in rig.Hands)
                Haptics.Pulse(hand, start ? 0.6f : 0.9f, start ? 0.12f : 0.3f);
        }

        // La tête, lissée ; l'horizon droit (pas de roulis).
        void Follow()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            var forward = head.forward;
            var rotation = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) < 0.98f
                ? Quaternion.LookRotation(forward, Vector3.up)
                : transform.rotation;

            if (!m_Placed || (transform.position - head.position).sqrMagnitude > k_Teleport * k_Teleport)
            {
                transform.SetPositionAndRotation(head.position, rotation);
                m_Placed = true;
                return;
            }

            var deltaTime = Time.unscaledDeltaTime;
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, head.position, 1f - Mathf.Exp(-deltaTime / k_PositionSmoothing)),
                Quaternion.Slerp(transform.rotation, rotation, 1f - Mathf.Exp(-deltaTime / k_RotationSmoothing)));
        }

        void UpdateToggle()
        {
            var keyboard = Keyboard.current;
            var pressed = keyboard != null && keyboard.f8Key.wasPressedThisFrame;

            var both = m_LeftStick.IsPressed() && m_RightStick.IsPressed();
            if (both && !m_BothPressed)
                pressed = true;
            m_BothPressed = both;

            if (pressed)
                ToggleRequested?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            ToggleRequested = null;
        }
    }
}
