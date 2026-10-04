using System;
using System.Collections.Generic;
using Archery.Core;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace Archery.Save
{
    /// <summary>Contenu du fichier settings.json.</summary>
    [Serializable]
    public class SettingsData
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float effectsVolume = 1f;

        /// <summary>Rotation par crans au joystick (GDD, section 12) : pratique à désactiver avec le câble Link.</summary>
        public bool snapTurn = true;
    }

    /// <summary>
    /// Paramètres du joueur (GDD, sections 14 et 15) : volumes général, musique et effets, rotation par crans.
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
        readonly HashSet<string> m_MissingParameters = new HashSet<string>();
        bool m_Dirty;
        float m_SaveTimer;

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
        }

        void Update()
        {
            if (!m_Dirty)
                return;

            m_SaveTimer -= Time.unscaledDeltaTime;
            if (m_SaveTimer <= 0f)
                SaveNow();
        }

        void OnDisable()
        {
            if (m_Dirty)
                SaveNow();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
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
                AddTurnProvider(provider);
            foreach (var provider in origin.GetComponentsInChildren<ContinuousTurnProvider>(true))
                AddTurnProvider(provider);
        }

        void AddTurnProvider(LocomotionProvider provider)
        {
            m_TurnProviders.Add(provider);
            m_TurnProvidersEnabled.Add(provider.enabled);
        }

        // Désactivée, la rotation au joystick est coupée ; activée, on retrouve les réglages d'origine du XR Origin.
        void ApplyTurn()
        {
            for (var i = 0; i < m_TurnProviders.Count; i++)
            {
                if (m_TurnProviders[i] != null)
                    m_TurnProviders[i].enabled = Data.snapTurn && m_TurnProvidersEnabled[i];
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Data = null;
    }
}
