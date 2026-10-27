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

        static readonly List<AudioSource> s_Sources = new List<AudioSource>();
        static Transform s_Root;
        static int s_Next;
        static int s_Frame = -1;
        static int s_PlayedThisFrame;

        /// <summary>Groupe du mixer par lequel passent ces sons (volume des effets). Réglé par les paramètres du jeu.</summary>
        public static AudioMixerGroup Output { get; set; }

        public static void Play(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 1f)
        {
            if (clip == null)
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
        }

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
            s_Root = null;
            s_Next = 0;
            s_Frame = -1;
            s_PlayedThisFrame = 0;
            Output = null;
        }
    }
}
