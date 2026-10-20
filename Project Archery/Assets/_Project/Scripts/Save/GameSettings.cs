using System;
using System.Collections.Generic;
using System.Reflection;
using Archery.Core;
using Archery.Player;
using Archery.UI;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace Archery.Save
{
    /// <summary>Contenu du fichier settings.json.</summary>
    [Serializable]
    public class SettingsData
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float effectsVolume = 1f;

        /// <summary>Rotation au joystick droit (GDD, section 12) : pratique à désactiver avec le câble Link.</summary>
        public bool snapTurn = true;

        /// <summary>Rotation continue (douce) au lieu des crans de 45° ; elle peut gêner les joueurs sensibles au mal des transports.</summary>
        public bool smoothTurn;

        /// <summary>Vitesse de la rotation continue (°/s).</summary>
        public float turnSpeed = 60f;

        /// <summary>Force de la vignette de confort pendant le slide (0 = désactivée, 1 = la plus forte).</summary>
        public float comfortVignette = 0.5f;
    }

    /// <summary>
    /// Paramètres du joueur (GDD, sections 14 et 15) : volumes général, musique et effets, rotation par crans,
    /// vignette de confort.
    /// Ils sont enregistrés dans <c>settings.json</c> et appliqués au lancement.
    /// </summary>
    /// <remarks>
    /// Les volumes passent par un Audio Mixer dont les paramètres sont exposés.
    /// Sans mixer, seul le volume général marche (AudioListener).
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-90)]
    public class GameSettings : MonoBehaviour
    {
        public const string FileName = "settings.json";

        [Tooltip("Mixer du jeu. Ses volumes doivent être exposés sous les noms ci-dessous.")]
        [SerializeField]
        AudioMixer m_Mixer;

        [SerializeField]
        string m_MasterParameter = "MasterVolume";

        [SerializeField]
        string m_MusicParameter = "MusicVolume";

        [SerializeField]
        string m_EffectsParameter = "EffectsVolume";

        [Tooltip("Groupe du mixer pour les sons joués par le code (impacts, coups, gong…).")]
        [SerializeField]
        AudioMixerGroup m_EffectsGroup;

        // Gardés entre deux parties (la scène est rechargée), lus une seule fois sur le disque.
        static SettingsData s_Data;

        readonly List<LocomotionProvider> m_TurnProviders = new List<LocomotionProvider>();
        readonly List<bool> m_TurnProvidersEnabled = new List<bool>();
        readonly List<ContinuousTurnProvider> m_ContinuousTurns = new List<ContinuousTurnProvider>();
        readonly List<ControllerInputActionManager> m_InputManagers = new List<ControllerInputActionManager>();
        readonly HashSet<string> m_MissingParameters = new HashSet<string>();
        bool m_Dirty;
        float m_SaveTimer;
        float m_NextTurnCheck;
        bool m_TurnRestoredLogged;

        // Le joystick droit, lu directement : la rotation coupée dans les paramètres, on le rappelle au joueur qui le pousse.
        InputAction m_TurnProbe;
        float m_NextTurnHint;

        // Les actions de rotation de chaque manette, gardées privées par l'exemple d'XRI (voir KeepTurnAlive).
        static readonly FieldInfo s_SnapTurnField =
            typeof(ControllerInputActionManager).GetField("m_SnapTurn", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo s_SmoothTurnField =
            typeof(ControllerInputActionManager).GetField("m_Turn", BindingFlags.Instance | BindingFlags.NonPublic);

        public static GameSettings Instance { get; private set; }

        /// <summary>Paramètres actuels (chargés depuis le disque à la première lecture).</summary>
        public static SettingsData Data
        {
            get
            {
                if (s_Data == null && (!JsonStorage.TryLoad(FileName, out s_Data) || s_Data == null))
                    s_Data = new SettingsData();
                return s_Data;
            }
        }

        public float MasterVolume
        {
            get => Data.masterVolume;
            set => SetVolume(ref Data.masterVolume, value);
        }

        public float MusicVolume
        {
            get => Data.musicVolume;
            set => SetVolume(ref Data.musicVolume, value);
        }

        public float EffectsVolume
        {
            get => Data.effectsVolume;
            set => SetVolume(ref Data.effectsVolume, value);
        }

        /// <summary>Force de la vignette de confort (0 à 1), lue par la course aux bras à chaque slide.</summary>
        public float ComfortVignette
        {
            get => Data.comfortVignette;
            set
            {
                Data.comfortVignette = Mathf.Clamp01(value);
                MarkDirty();
            }
        }

        /// <summary>Rotation au joystick activée (par crans ou continue).</summary>
        public bool SnapTurn
        {
            get => Data.snapTurn;
            set
            {
                Data.snapTurn = value;
                ApplyTurn();
                MarkDirty();
            }
        }

        /// <summary>Rotation continue au lieu des crans.</summary>
        public bool SmoothTurn
        {
            get => Data.smoothTurn;
            set
            {
                Data.smoothTurn = value;
                ApplyTurn();
                MarkDirty();
            }
        }

        /// <summary>Vitesse de la rotation continue (°/s), entre <see cref="MinTurnSpeed"/> et <see cref="MaxTurnSpeed"/>.</summary>
        public float TurnSpeed
        {
            get => Data.turnSpeed;
            set
            {
                Data.turnSpeed = Mathf.Clamp(value, MinTurnSpeed, MaxTurnSpeed);
                ApplyTurn();
                MarkDirty();
            }
        }

        public const float MinTurnSpeed = 30f;
        public const float MaxTurnSpeed = 180f;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Game Settings dans la scène.", this);
            Instance = this;

            if (m_EffectsGroup != null)
                Sfx.Output = m_EffectsGroup;
        }

        // Le mixer ignore les valeurs données pendant Awake : on applique tout au Start.
        void Start()
        {
            CollectTurnProviders();
            ApplyAudio();
            ApplyTurn();
            m_TurnProbe = new InputAction("Rotation coupée (rappel)", InputActionType.Value, "<XRController>{RightHand}/{Primary2DAxis}");
            m_TurnProbe.Enable();
        }

        void Update()
        {
            if (Time.unscaledTime >= m_NextTurnCheck)
            {
                m_NextTurnCheck = Time.unscaledTime + 0.5f;
                KeepTurnAlive();
            }

            HintTurnDisabled();
            if (!m_Dirty)
                return;

            m_SaveTimer -= Time.unscaledDeltaTime;
            if (m_SaveTimer <= 0f)
                SaveNow();
        }

        void OnEnable() => m_TurnProbe?.Enable();

        void OnDisable()
        {
            m_TurnProbe?.Disable();
            if (m_Dirty)
                SaveNow();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            m_TurnProbe?.Dispose();
        }

        // La rotation coupée dans les paramètres (« Rotation au joystick : non ») : le joystick droit poussé sur le côté
        // ne fait rien. On le dit au joueur, au plus une fois toutes les 30 s, avec l'endroit où la rallumer.
        void HintTurnDisabled()
        {
            if (Data.snapTurn || m_TurnProbe == null || Time.unscaledTime < m_NextTurnHint ||
                (DesktopControls.Instance != null && DesktopControls.Instance.IsActive))
                return;
            if (Mathf.Abs(m_TurnProbe.ReadValue<Vector2>().x) < 0.8f)
                return;

            m_NextTurnHint = Time.unscaledTime + 30f;
            HeadMessages.Show("Rotation au joystick coupée\n<size=60%>Paramètres > Rotation au joystick</size>",
                              new Color(1f, 0.85f, 0.35f), 3.5f, 0.9f);
        }

        void SetVolume(ref float field, float value)
        {
            field = Mathf.Clamp01(value);
            ApplyAudio();
            MarkDirty();
        }

        // On enregistre un peu après le dernier changement, pas à chaque mouvement du curseur.
        void MarkDirty()
        {
            m_Dirty = true;
            m_SaveTimer = 0.5f;
        }

        void SaveNow()
        {
            m_Dirty = false;
            JsonStorage.Save(FileName, Data);
        }

        void ApplyAudio()
        {
            if (m_Mixer == null)
            {
                AudioListener.volume = Data.masterVolume;
                return;
            }

            SetMixerVolume(m_MasterParameter, Data.masterVolume);
            SetMixerVolume(m_MusicParameter, Data.musicVolume);
            SetMixerVolume(m_EffectsParameter, Data.effectsVolume);
        }

        void SetMixerVolume(string parameter, float volume)
        {
            if (string.IsNullOrEmpty(parameter))
                return;

            // 0 → -80 dB (silence), 1 → 0 dB (volume normal).
            var decibels = volume > 0.0001f ? 20f * Mathf.Log10(volume) : -80f;
            if (!m_Mixer.SetFloat(parameter, decibels) && m_MissingParameters.Add(parameter))
                Debug.LogWarning($"GameSettings : le mixer n'a pas de paramètre exposé « {parameter} ».", this);
        }

        void CollectTurnProviders()
        {
            var origin = FindAnyObjectByType<XROrigin>();
            if (origin == null)
                return;

            foreach (var provider in origin.GetComponentsInChildren<SnapTurnProvider>(true))
            {
                // Pas de demi-tour en tirant le joystick vers soi : seulement les rotations à gauche et à droite.
                provider.enableTurnAround = false;
                AddTurnProvider(provider);
            }
            foreach (var provider in origin.GetComponentsInChildren<ContinuousTurnProvider>(true))
            {
                provider.enableTurnAround = false;
                m_ContinuousTurns.Add(provider);
                AddTurnProvider(provider);
            }

            // Ce sont les gestionnaires d'entrées des manettes qui choisissent entre les crans et la rotation continue.
            m_InputManagers.AddRange(origin.GetComponentsInChildren<ControllerInputActionManager>(true));

            // La rotation se coupait parfois : XRI réserve le joystick d'une main à d'autres usages, et y coupe la rotation.
            // - Un objet saisi au rayon, de loin (l'arc, le marteau…), s'avance et se recule au joystick tant qu'on le tient :
            //   on n'en a pas besoin, l'objet vient dans la main.
            // - Viser un menu qui défile : aucun de nos menus ne défile.
            foreach (var attach in origin.GetComponentsInChildren<InteractionAttachController>(true))
                attach.useManipulationInput = false;
            foreach (var manager in m_InputManagers)
                manager.uiScrollingEnabled = false;
        }

        // Filet de sécurité : si la rotation choisie est coupée sur une manette qui tourne (la droite ; la gauche sert à
        // marcher), on la rallume. Pas en jeu au clavier, qui met les déplacements d'XRI en pause.
        void KeepTurnAlive()
        {
            if (!Data.snapTurn || (DesktopControls.Instance != null && DesktopControls.Instance.IsActive))
                return;

            var field = Data.smoothTurn ? s_SmoothTurnField : s_SnapTurnField;
            if (field == null)
                return;

            foreach (var manager in m_InputManagers)
            {
                if (manager == null || !manager.isActiveAndEnabled || manager.smoothMotionEnabled)
                    continue;

                var action = (field.GetValue(manager) as InputActionReference)?.action;
                if (action == null || action.enabled)
                    continue;

                action.Enable();
                if (!m_TurnRestoredLogged)
                {
                    m_TurnRestoredLogged = true;
                    Debug.Log($"GameSettings : la rotation au joystick était coupée ({manager.name}), elle est rallumée.", manager);
                }
            }
        }

        void AddTurnProvider(LocomotionProvider provider)
        {
            m_TurnProviders.Add(provider);
            m_TurnProvidersEnabled.Add(provider.enabled);
        }

        // Désactivée, la rotation au joystick est coupée ; activée, on retrouve les réglages d'origine du XR Origin,
        // par crans ou en continu à la vitesse choisie.
        void ApplyTurn()
        {
            for (var i = 0; i < m_TurnProviders.Count; i++)
            {
                if (m_TurnProviders[i] != null)
                    m_TurnProviders[i].enabled = Data.snapTurn && m_TurnProvidersEnabled[i];
            }

            foreach (var manager in m_InputManagers)
            {
                if (manager != null)
                    manager.smoothTurnEnabled = Data.smoothTurn;
            }

            var speed = Mathf.Clamp(Data.turnSpeed, MinTurnSpeed, MaxTurnSpeed);
            foreach (var provider in m_ContinuousTurns)
            {
                if (provider != null)
                    provider.turnSpeed = speed;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Data = null;
    }
}
