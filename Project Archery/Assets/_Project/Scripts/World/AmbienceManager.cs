using Archery.Core;
using Archery.Difficulty;
using Archery.Enemies;
using Archery.Waves;
using UnityEngine;
using UnityEngine.Audio;

namespace Archery.World
{
    /// <summary>
    /// Ambiance sonore et musiques (GDD, section 16) :
    /// <list type="bullet">
    /// <item>une ambiance de jour (oiseaux, vent) ou de nuit (grillons, chouettes), selon le ciel de la difficulté ;</item>
    /// <item>une musique selon le moment : menu, pause (boutique), vague, boss ; rien après la mort.</item>
    /// </list>
    /// On passe d'une boucle à l'autre en fondu.
    /// </summary>
    /// <remarks>
    /// Les sons ne sont pas spatialisés (ils viennent de partout). La musique passe par le groupe « Music » du mixer,
    /// l'ambiance par celui des effets : les curseurs de volume des paramètres les règlent.
    /// </remarks>
    [DisallowMultipleComponent]
    public class AmbienceManager : MonoBehaviour
    {
        [Header("Ambiance")]
        [Tooltip("Boucle de jour : oiseaux, vent.")]
        [SerializeField]
        AudioClip m_DayAmbience;

        [Tooltip("Boucle de nuit : grillons, chouettes, vent.")]
        [SerializeField]
        AudioClip m_NightAmbience;

        [Range(0f, 1f)]
        [SerializeField]
        float m_AmbienceVolume = 0.5f;

        [Tooltip("Groupe du mixer pour l'ambiance. Vide : celui des effets.")]
        [SerializeField]
        AudioMixerGroup m_AmbienceGroup;

        [Header("Musiques")]
        [Tooltip("Avant la première vague. Vide : la musique de la boutique.")]
        [SerializeField]
        AudioClip m_MenuMusic;

        [Tooltip("Pendant les pauses, boutique ouverte.")]
        [SerializeField]
        AudioClip m_ShopMusic;

        [Tooltip("Pendant une vague.")]
        [SerializeField]
        AudioClip m_WaveMusic;

        [Tooltip("Tant qu'un boss est en vie. Vide : la musique de vague.")]
        [SerializeField]
        AudioClip m_BossMusic;

        [Range(0f, 1f)]
        [SerializeField]
        float m_MusicVolume = 0.6f;

        [Tooltip("Groupe « Music » du mixer.")]
        [SerializeField]
        AudioMixerGroup m_MusicGroup;

        [Tooltip("Durée (s) des fondus entre deux boucles.")]
        [SerializeField]
        float m_FadeTime = 1.5f;

        CrossFader m_Ambience;
        CrossFader m_Music;

        void Awake()
        {
            m_Ambience = new CrossFader(transform, "Ambience", m_AmbienceGroup, 200);
            m_Music = new CrossFader(transform, "Music", m_MusicGroup, 100);
        }

        void Start()
        {
            // Sans groupe précis, l'ambiance suit le volume des effets (réglé par Game Settings).
            if (m_AmbienceGroup == null && Sfx.Output != null)
                m_Ambience.SetOutput(Sfx.Output);
        }

        void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            var night = DifficultyManager.Current.sky.night;
            m_Ambience.Play(night ? m_NightAmbience : m_DayAmbience, m_AmbienceVolume, m_FadeTime);
            m_Music.Play(CurrentMusic(), m_MusicVolume, m_FadeTime);
            m_Ambience.Update(deltaTime);
            m_Music.Update(deltaTime);
        }

        AudioClip CurrentMusic()
        {
            var waves = WaveManager.Instance;
            var phase = waves != null ? waves.Phase : WavePhase.Ready;
            switch (phase)
            {
                case WavePhase.GameOver:
                    return null;
                case WavePhase.Wave:
                    return Boss.Current != null && m_BossMusic != null ? m_BossMusic : m_WaveMusic;
                case WavePhase.Intermission:
                    return m_ShopMusic;
                default:
                    return m_MenuMusic != null ? m_MenuMusic : m_ShopMusic;
            }
        }

        /// <summary>Deux sources en boucle qui se relaient : l'une monte pendant que l'autre descend.</summary>
        sealed class CrossFader
        {
            readonly AudioSource[] m_Sources = new AudioSource[2];
            int m_Current;
            float m_TargetVolume;
            float m_FadeTime = 1f;

            public CrossFader(Transform parent, string name, AudioMixerGroup group, int priority)
            {
                for (var i = 0; i < m_Sources.Length; i++)
                {
                    var go = new GameObject($"{name} {i + 1}");
                    go.transform.SetParent(parent, false);
                    var source = go.AddComponent<AudioSource>();
                    source.loop = true;
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    source.priority = priority;
                    source.volume = 0f;
                    source.outputAudioMixerGroup = group;
                    m_Sources[i] = source;
                }
            }

            public void SetOutput(AudioMixerGroup group)
            {
                foreach (var source in m_Sources)
                    source.outputAudioMixerGroup = group;
            }

            // Si la boucle demandée change, l'autre source la prend et monte ; l'ancienne descend.
            public void Play(AudioClip clip, float volume, float fadeTime)
            {
                m_TargetVolume = volume;
                m_FadeTime = Mathf.Max(0.05f, fadeTime);

                var current = m_Sources[m_Current];
                if (current.clip == clip && (clip == null || current.isPlaying))
                    return;

                m_Current = 1 - m_Current;
                var next = m_Sources[m_Current];
                next.clip = clip;
                next.volume = 0f;
                if (clip != null)
                    next.Play();
                else
                    next.Stop();
            }

            public void Update(float deltaTime)
            {
                for (var i = 0; i < m_Sources.Length; i++)
                {
                    var source = m_Sources[i];
                    var target = i == m_Current && source.clip != null ? m_TargetVolume : 0f;
                    source.volume = Mathf.MoveTowards(source.volume, target, deltaTime / m_FadeTime * Mathf.Max(0.01f, m_TargetVolume));
                    if (i != m_Current && source.isPlaying && source.volume <= 0f)
                        source.Stop();
                }
            }
        }
    }
}
