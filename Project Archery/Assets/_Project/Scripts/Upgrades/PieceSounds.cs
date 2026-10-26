using UnityEngine;

namespace Archery.Upgrades
{
    /// <summary>
    /// Les sons des pièces de la flèche (refonte roguelike, étape 4, 26 octobre), fabriqués une fois au premier usage, comme
    /// ceux du toit cyberpunk (<c>Cyber/CyberCues</c>) : rien à importer.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><see cref="Equip"/> : prendre une pièce, un motif par emplacement (pointe : deux tintements aigus ; empennage :
    /// un souffle qui monte ; fût : un gong grave ; corde : une corde pincée ; talisman : une clochette). Plus aigu à chaque
    /// niveau (la hauteur se règle à la lecture).</item>
    /// <item><see cref="Whoosh"/> : le demi-tour de la flèche boomerang.</item>
    /// <item><see cref="SteerHum"/> : la flèche guidée par télékinésie (une boucle).</item>
    /// <item><see cref="Sizzle"/> : la mèche qui crépite, une seconde.</item>
    /// <item><see cref="SlowIn"/>, <see cref="SlowOut"/> : le ralenti du talisman d'acrobate qui commence et qui finit.</item>
    /// </list>
    /// </remarks>
    public static class PieceSounds
    {
        const int k_Rate = 44100;

        static readonly AudioClip[] s_Equip = new AudioClip[6];
        static AudioClip s_Whoosh;
        static AudioClip s_SteerHum;
        static AudioClip s_Sizzle;
        static AudioClip s_SlowIn;
        static AudioClip s_SlowOut;
        static AudioClip s_Duo;

        /// <summary>Le motif de l'emplacement d'une pièce qu'on vient de prendre.</summary>
        public static AudioClip Equip(ArrowSlot slot)
        {
            var index = Mathf.Clamp((int)slot, 0, s_Equip.Length - 1);
            if (s_Equip[index] != null)
                return s_Equip[index];

            float[] samples;
            switch (slot)
            {
                case ArrowSlot.Tip:
                    samples = Mix(Bell(0.45f, 1760f, 9f, 0f), Bell(0.45f, 2350f, 9f, 0.09f));
                    break;
                case ArrowSlot.Fletching:
                    samples = Wind(0.4f, 600f, 4200f, 0.5f);
                    break;
                case ArrowSlot.Shaft:
                    samples = Bell(0.9f, 196f, 4f, 0f);
                    break;
                case ArrowSlot.String:
                    samples = Pluck(0.6f, 196f);
                    break;
                default:
                    samples = Mix(Bell(1f, 880f, 3.5f, 0f), Bell(1f, 1320f, 4.5f, 0.12f));
                    break;
            }

            return s_Equip[index] = Create("Pièce " + slot, samples);
        }

        /// <summary>Un souffle qui passe, comme une lame qui fend l'air.</summary>
        public static AudioClip Whoosh => s_Whoosh != null ? s_Whoosh : s_Whoosh = Create("Demi-tour", Wind(0.45f, 3000f, 500f, 0.6f));

        /// <summary>Un bourdonnement doux qui ondule (une boucle d'une seconde : 110 et 165 périodes entières, sans claquement).</summary>
        public static AudioClip SteerHum
        {
            get
            {
                if (s_SteerHum != null)
                    return s_SteerHum;

                var samples = new float[k_Rate];
                for (var i = 0; i < samples.Length; i++)
                {
                    var t = i / (float)k_Rate;
                    var wobble = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 4f * t);
                    samples[i] = (Mathf.Sin(2f * Mathf.PI * 110f * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * 165f * t)) * wobble * 0.25f;
                }

                return s_SteerHum = Create("Télékinésie", samples);
            }
        }

        /// <summary>Une mèche qui crépite pendant une seconde, de plus en plus fort.</summary>
        public static AudioClip Sizzle
        {
            get
            {
                if (s_Sizzle != null)
                    return s_Sizzle;

                const float length = 1f;
                var samples = new float[Mathf.RoundToInt(length * k_Rate)];
                var random = new System.Random(17);
                var previous = 0f;
                var crackle = 0f;
                for (var i = 0; i < samples.Length; i++)
                {
                    var t = i / (float)k_Rate;
                    var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                    var hiss = noise - previous;
                    previous = noise;
                    if (random.NextDouble() < 0.004)
                        crackle = (float)(random.NextDouble() * 2.0 - 1.0);
                    crackle *= 0.985f;
                    samples[i] = (hiss * 0.18f + crackle * 0.6f) * (0.35f + 0.65f * t / length) * Envelope(t, length, 0.02f, 0.05f);
                }

                return s_Sizzle = Create("Mèche", samples);
            }
        }

        /// <summary>Un duo se forme : un accord qui monte (trois cloches, l'une après l'autre).</summary>
        public static AudioClip Duo =>
            s_Duo != null ? s_Duo : s_Duo = Create("Duo", Mix(Mix(Bell(1.4f, 523f, 2.5f, 0f), Bell(1.4f, 659f, 2.5f, 0.12f)), Bell(1.4f, 784f, 2.5f, 0.24f)));

        /// <summary>Le temps qui ralentit : une note qui descend dans un souffle.</summary>
        public static AudioClip SlowIn => s_SlowIn != null ? s_SlowIn : s_SlowIn = Create("Ralenti", Mix(Sweep(0.5f, 620f, 140f), Wind(0.5f, 2500f, 400f, 0.25f)));

        /// <summary>Le temps qui repart : une note qui remonte.</summary>
        public static AudioClip SlowOut => s_SlowOut != null ? s_SlowOut : s_SlowOut = Create("Fin du ralenti", Sweep(0.35f, 160f, 560f));

        // Une cloche de « frequency » Hz (et ses partiels inharmoniques), qui s'éteint à « decay » par seconde, après « delay » s.
        static float[] Bell(float length, float frequency, float decay, float delay)
        {
            var samples = new float[Mathf.RoundToInt(length * k_Rate)];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)k_Rate - delay;
                if (t < 0f)
                    continue;

                var wave = Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.5f * Mathf.Sin(2f * Mathf.PI * frequency * 2.76f * t) +
                           0.25f * Mathf.Sin(2f * Mathf.PI * frequency * 5.4f * t);
                samples[i] = wave * 0.3f * Mathf.Exp(-decay * t) * Mathf.Clamp01(t / 0.003f);
            }

            return samples;
        }

        // Une corde pincée (Karplus-Strong) : un bruit bref qui se change en note, de plus en plus douce.
        static float[] Pluck(float length, float frequency)
        {
            var samples = new float[Mathf.RoundToInt(length * k_Rate)];
            var period = Mathf.Max(2, Mathf.RoundToInt(k_Rate / frequency));
            var buffer = new float[period];
            var random = new System.Random(5);
            for (var i = 0; i < period; i++)
                buffer[i] = (float)(random.NextDouble() * 2.0 - 1.0);

            for (var i = 0; i < samples.Length; i++)
            {
                var index = i % period;
                var next = buffer[(index + 1) % period];
                samples[i] = buffer[index] * 0.45f;
                buffer[index] = 0.5f * (buffer[index] + next) * 0.996f;
            }

            return samples;
        }

        // Du vent : un bruit filtré dont la couleur glisse de « from » à « to » Hz, qui monte puis retombe.
        static float[] Wind(float length, float from, float to, float volume)
        {
            var samples = new float[Mathf.RoundToInt(length * k_Rate)];
            var random = new System.Random(31);
            var low = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)k_Rate;
                var progress = t / length;
                var cutoff = Mathf.Lerp(from, to, progress);
                var alpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / k_Rate);
                low += alpha * ((float)(random.NextDouble() * 2.0 - 1.0) - low);
                samples[i] = low * volume * Mathf.Sin(Mathf.PI * progress) * 2.2f;
            }

            return samples;
        }

        // Une note douce qui glisse de « from » à « to » Hz.
        static float[] Sweep(float length, float from, float to)
        {
            var samples = new float[Mathf.RoundToInt(length * k_Rate)];
            var phase = 0f;
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)k_Rate;
                phase += 2f * Mathf.PI * Mathf.Lerp(from, to, t / length) / k_Rate;
                samples[i] = (Mathf.Sin(phase) + 0.3f * Mathf.Sin(2f * phase)) * 0.3f * Envelope(t, length, 0.02f, 0.12f);
            }

            return samples;
        }

        static float[] Mix(float[] a, float[] b)
        {
            var samples = new float[Mathf.Max(a.Length, b.Length)];
            for (var i = 0; i < samples.Length; i++)
                samples[i] = (i < a.Length ? a[i] : 0f) + (i < b.Length ? b[i] : 0f);
            return samples;
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
            for (var i = 0; i < s_Equip.Length; i++)
                s_Equip[i] = null;
            s_Whoosh = null;
            s_SteerHum = null;
            s_Sizzle = null;
            s_SlowIn = null;
            s_SlowOut = null;
            s_Duo = null;
        }
    }
}
