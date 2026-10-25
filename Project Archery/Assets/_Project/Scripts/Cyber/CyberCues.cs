using System.Collections.Generic;
using UnityEngine;

namespace Archery.Cyber
{
    /// <summary>
    /// Les sons d'annonce du toit cyberpunk (24 octobre : l'épreuve était impossible ; ils aident à la tenir), fabriqués
    /// une fois au lancement, comme des bips d'ordinateur : rien à importer, et ils vont avec le monde de THUNK.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><see cref="Tell"/> : au début de la visée, l'attaque qui vient. Un bip, une balle ; trois bips rapides, une
    /// rafale ; une note qui descend, une ligne (s'accroupir) ; une note qui oscille, une colonne (se pencher).</item>
    /// <item><see cref="Charge"/> : la charge du tireur qui vise, qui monte jusqu'au tir.</item>
    /// <item><see cref="Hum"/> : le grésillement d'une balle en vol, qu'on entend arriver.</item>
    /// <item><see cref="Heartbeat"/> : un cœur qui bat quand il ne reste qu'une touche.</item>
    /// </list>
    /// Joués en son 3D depuis le tireur ou la balle : on entend d'où ils viennent.
    /// </remarks>
    public static class CyberCues
    {
        const int k_Rate = 44100;

        static readonly Dictionary<int, AudioClip> s_Charges = new Dictionary<int, AudioClip>();
        static readonly AudioClip[] s_Tells = new AudioClip[4];
        static AudioClip s_Hum;
        static AudioClip s_Heartbeat;

        /// <summary>L'annonce d'une attaque (0 : une balle, 1 : une rafale, 2 : une ligne, 3 : une colonne).</summary>
        public static AudioClip Tell(int pattern)
        {
            pattern = Mathf.Clamp(pattern, 0, s_Tells.Length - 1);
            if (s_Tells[pattern] != null)
                return s_Tells[pattern];

            float[] samples;
            switch (pattern)
            {
                case 0:
                    samples = Beeps(1, 0.09f, 0f, 1320f);
                    break;
                case 1:
                    samples = Beeps(3, 0.05f, 0.045f, 1320f);
                    break;
                case 2:
                    // Une note qui tombe : baisse-toi.
                    samples = Tone(0.42f, t => Mathf.Lerp(1150f, 260f, Mathf.Sqrt(t)));
                    break;
                default:
                    // Deux notes qui alternent vite, comme une sirène : penche-toi.
                    samples = Tone(0.42f, t => Mathf.Repeat(t * 7f, 1f) < 0.5f ? 1050f : 640f);
                    break;
            }

            return s_Tells[pattern] = Create("Annonce " + pattern, samples);
        }

        /// <summary>La charge d'un tireur, qui monte pendant <paramref name="duration"/> s, jusqu'au tir.</summary>
        public static AudioClip Charge(float duration)
        {
            var key = Mathf.Clamp(Mathf.RoundToInt(duration * 20f), 2, 60);
            if (s_Charges.TryGetValue(key, out var clip) && clip != null)
                return clip;

            var length = key / 20f;
            var count = Mathf.RoundToInt(length * k_Rate);
            var samples = new float[count];
            var phase = 0f;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)k_Rate;
                var progress = t / length;
                var frequency = Mathf.Lerp(200f, 900f, progress * progress);
                phase += 2f * Mathf.PI * frequency / k_Rate;
                // Un trémolo de plus en plus rapide, et de plus en plus fort : le tir approche.
                var tremolo = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * (6f + 22f * progress) * t);
                var wave = Mathf.Sin(phase) + 0.35f * Mathf.Sin(2f * phase) + 0.15f * Mathf.Sin(3f * phase);
                // Plus discrète que l'annonce : elle dit quand, l'annonce dit quoi.
                samples[i] = wave * tremolo * (0.2f + 0.8f * progress) * 0.28f * Envelope(t, length, 0.01f, 0.01f);
            }

            clip = Create("Charge " + key, samples);
            s_Charges[key] = clip;
            return clip;
        }

        /// <summary>Le grésillement d'une balle (une boucle d'une seconde).</summary>
        public static AudioClip Hum
        {
            get
            {
                if (s_Hum != null)
                    return s_Hum;

                // 1 s : 90 et 135 périodes entières, pour boucler sans claquer ; des craquements par-dessus.
                var samples = new float[k_Rate];
                var random = new System.Random(90);
                var crackle = 0f;
                for (var i = 0; i < samples.Length; i++)
                {
                    var t = i / (float)k_Rate;
                    var saw = 2f * Mathf.Repeat(t * 90f, 1f) - 1f;
                    var buzz = 0.55f * saw + 0.3f * Mathf.Sin(2f * Mathf.PI * 135f * t);
                    if (random.NextDouble() < 0.0015)
                        crackle = (float)(random.NextDouble() * 2.0 - 1.0);
                    crackle *= 0.995f;
                    samples[i] = (buzz * 0.6f + crackle * 0.5f) * 0.6f;
                }

                return s_Hum = Create("Grésillement", samples);
            }
        }

        /// <summary>Un battement de cœur (une boucle : deux coups sourds).</summary>
        public static AudioClip Heartbeat
        {
            get
            {
                if (s_Heartbeat != null)
                    return s_Heartbeat;

                const float length = 0.85f;
                var samples = new float[Mathf.RoundToInt(length * k_Rate)];
                for (var i = 0; i < samples.Length; i++)
                {
                    var t = i / (float)k_Rate;
                    samples[i] = Thump(t, 0f, 0.9f) + Thump(t, 0.26f, 0.6f);
                }

                return s_Heartbeat = Create("Cœur", samples);
            }
        }

        // Un coup sourd à « start » s : une note grave qui s'éteint vite.
        static float Thump(float t, float start, float strength)
        {
            var local = t - start;
            if (local < 0f || local > 0.25f)
                return 0f;
            var frequency = Mathf.Lerp(70f, 45f, local / 0.25f);
            return Mathf.Sin(2f * Mathf.PI * frequency * local) * Mathf.Exp(-local * 22f) * strength * Mathf.Clamp01(local / 0.004f);
        }

        // « count » bips de « on » s, séparés de « off » s.
        static float[] Beeps(int count, float on, float off, float frequency)
        {
            var length = count * on + (count - 1) * off;
            var samples = new float[Mathf.RoundToInt(length * k_Rate)];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)k_Rate;
                var inBeep = Mathf.Repeat(t, on + off);
                if (inBeep > on)
                    continue;
                samples[i] = Square(2f * Mathf.PI * frequency * t) * 0.45f * Envelope(inBeep, on, 0.004f, 0.01f);
            }

            return samples;
        }

        // Une note de « length » s dont la fréquence suit « frequency » (de 0 à 1 sur sa durée).
        static float[] Tone(float length, System.Func<float, float> frequency)
        {
            var samples = new float[Mathf.RoundToInt(length * k_Rate)];
            var phase = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)k_Rate;
                phase += 2f * Mathf.PI * frequency(t / length) / k_Rate;
                samples[i] = Square(phase) * 0.45f * Envelope(t, length, 0.006f, 0.04f);
            }

            return samples;
        }

        // Un carré adouci : un son d'ordinateur, sans être agressif.
        static float Square(float phase)
        {
            var sine = Mathf.Sin(phase);
            return (float)System.Math.Tanh(sine * 3f) * 0.8f;
        }

        // Monte en « attack » s et s'éteint en « release » s : pas de claquement aux bords.
        static float Envelope(float t, float length, float attack, float release) =>
            Mathf.Clamp01(t / attack) * Mathf.Clamp01((length - t) / release);

        static AudioClip Create(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, Mathf.Max(1, samples.Length), 1, k_Rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Charges.Clear();
            for (var i = 0; i < s_Tells.Length; i++)
                s_Tells[i] = null;
            s_Hum = null;
            s_Heartbeat = null;
        }
    }
}
