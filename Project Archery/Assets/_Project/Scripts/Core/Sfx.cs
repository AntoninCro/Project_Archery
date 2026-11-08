using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Archery.Core
{
    /// <summary>
    /// Joue des sons ponctuels spatialisés avec un petit pool d'AudioSource,
    /// pour éviter les allocations de <see cref="AudioSource.PlayClipAtPoint"/>.
    /// </summary>
    public static class Sfx
    {
        const int k_MaxSources = 24;

        // Le budget d'effets (27 octobre au soir, jusqu'à 1000 flèches en vol) : au plus tant de sons dans l'espace qui commencent
        // à la même image ; les suivants se tairaient sous les premiers de toute façon. Les sons à plat (interface) passent toujours.
        const int k_MaxSpatialPerFrame = 6;

        // Le silence de Minuit : les sons du joueur, ceux qui passent, sont à moins de tant de sa tête (son arc, sa montre, ses pas).
        const float k_HushRadius = 2.5f;
        const float k_HushScanInterval = 0.25f;

        static readonly List<AudioSource> s_Sources = new List<AudioSource>();
        static readonly HashSet<AudioSource> s_ThroughSources = new HashSet<AudioSource>();
        static readonly List<AudioSource> s_Muted = new List<AudioSource>();
        static Transform s_Root;
        static int s_Next;
        static int s_Frame = -1;
        static int s_PlayedThisFrame;
        static bool s_Hushed;
        static bool s_Through;
        static float s_NextHushScan;

        /// <summary>Groupe du mixer par lequel passent ces sons (volume des effets). Réglé par les paramètres du jeu.</summary>
        public static AudioMixerGroup Output { get; set; }

        /// <summary>Le silence de Minuit est en cours (<see cref="Hush"/>).</summary>
        public static bool Hushed => s_Hushed;

        public static void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            if (clip == null)
                return;

            if (s_Hushed && !s_Through && !NearListener(position, ListenerPosition()))
                return;

            if (spatialBlend > 0.5f)
            {
                if (Time.frameCount != s_Frame)
                {
                    s_Frame = Time.frameCount;
                    s_PlayedThisFrame = 0;
                }

                if (s_PlayedThisFrame >= k_MaxSpatialPerFrame)
                    return;
                s_PlayedThisFrame++;
            }

            var source = NextSource();
            source.outputAudioMixerGroup = Output;
            source.transform.position = position;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.spatialBlend = spatialBlend;
            source.Play();
            if (s_Through)
                s_ThroughSources.Add(source);
            else
                s_ThroughSources.Remove(source);
        }

        /// <summary>Comme <see cref="Play"/>, mais le son passe le silence de Minuit (les sons de l'horloge qui l'a fait).</summary>
        public static void PlayThroughHush(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            s_Through = true;
            Play(clip, position, volume, pitch, spatialBlend);
            s_Through = false;
        }

        /// <summary>
        /// Le silence de Minuit (3 novembre, le joueur : « pendant l'arrêt du temps de l'horloge, enlève les autres sons et musique,
        /// laisse juste les bruits de l'horloge et du joueur ») : à appeler à chaque image tant qu'il dure, puis <see cref="Unhush"/>.
        /// La musique, l'ambiance et toutes les sources du monde se taisent (<c>mute</c> : leurs scripts gardent la main sur leur
        /// volume, et elles continuent sans qu'on les entende), y compris celles qui commencent pendant ; les sons de <see cref="Play"/>
        /// déjà partis s'arrêtent. Passent seulement les sons du joueur (à moins de 2,5 m de sa tête : son arc, sa montre, ses pas) et
        /// ceux de <see cref="PlayThroughHush"/>.
        /// </summary>
        public static void Hush()
        {
            var listener = ListenerPosition();
            if (!s_Hushed)
            {
                s_Hushed = true;
                s_NextHushScan = 0f;
                foreach (var source in s_Sources)
                {
                    if (source != null && source.isPlaying && !s_ThroughSources.Contains(source) &&
                        !NearListener(source.transform.position, listener))
                        source.Stop();
                }
            }

            if (Time.unscaledTime < s_NextHushScan)
                return;
            s_NextHushScan = Time.unscaledTime + k_HushScanInterval;

            // Une source à plat (la musique, l'ambiance) se tait toujours ; une source dans l'espace, sauf près du joueur (l'arc).
            foreach (var source in Object.FindObjectsByType<AudioSource>())
            {
                if (source == null || source.mute || (s_Root != null && source.transform.parent == s_Root))
                    continue;
                if (source.spatialBlend > 0f && NearListener(source.transform.position, listener))
                    continue;

                source.mute = true;
                s_Muted.Add(source);
            }
        }

        /// <summary>La fin du silence de Minuit : ce qu'il avait fait taire reprend.</summary>
        public static void Unhush()
        {
            if (!s_Hushed)
                return;

            s_Hushed = false;
            foreach (var source in s_Muted)
            {
                if (source != null)
                    source.mute = false;
            }

            s_Muted.Clear();
        }

        static Vector3 ListenerPosition()
        {
            var camera = Camera.main;
            return camera != null ? camera.transform.position : new Vector3(float.NaN, 0f, 0f);
        }

        // Sans caméra (NaN), tout passe.
        static bool NearListener(Vector3 position, Vector3 listener) =>
            float.IsNaN(listener.x) || (position - listener).sqrMagnitude <= k_HushRadius * k_HushRadius;

        static AudioSource NextSource()
        {
            if (s_Root == null)
            {
                var root = new GameObject("[Sfx]");
                Object.DontDestroyOnLoad(root);
                s_Root = root.transform;
                s_Sources.Clear();
                s_Next = 0;
            }

            // Réutilise d'abord une source libre, sinon la plus ancienne.
            foreach (var source in s_Sources)
            {
                if (source != null && !source.isPlaying)
                    return source;
            }

            if (s_Sources.Count < k_MaxSources)
            {
                var go = new GameObject("Sfx Source");
                go.transform.SetParent(s_Root, false);
                var source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.minDistance = 1f;
                source.maxDistance = 60f;
                source.dopplerLevel = 0f;
                s_Sources.Add(source);
                return source;
            }

            s_Next = (s_Next + 1) % s_Sources.Count;
            return s_Sources[s_Next];
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Sources.Clear();
            s_ThroughSources.Clear();
            s_Muted.Clear();
            s_Root = null;
            s_Next = 0;
            s_Frame = -1;
            s_PlayedThisFrame = 0;
            s_Hushed = false;
            s_Through = false;
            s_NextHushScan = 0f;
            Output = null;
        }
    }
}
