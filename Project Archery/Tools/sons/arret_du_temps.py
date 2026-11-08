"""Le bruit de l'arrêt du temps de la Grande Horloge (3 novembre) : synthétisé, un son original (rien de l'extrait n'est repris).
Le joueur a donné pour modèle le « Za Warudo » de JoJo : après la phrase, un bruit saturé qui annonce l'arrêt du temps. Lu en forme
d'onde et en spectre (Claude), il fait environ 1,9 s, en deux temps : un bourdon grave saturé qui descend (134 → 116 Hz, une bande
qui ralentit), puis un grave qui remonte (65 → 80 Hz, son 3e harmonique fort) avec un sifflement fixe (1,67 kHz, puis 3 kHz) ; il se
coupe net sur un claquement aigu, et un souffle à peine audible reste. Ici, le même dessin, un peu plus court, coupé à CUT secondes :
le premier coup de cloche de Minuit tombe sur la coupure (UltimeHorloge). Le 3 novembre, après l'avoir entendu (le joueur : « plus
progressif, et moins fort ») : plus d'impact au départ ; le bourdon monte en 0,4 s, et tout enfle jusqu'à la coupure ; crête à 0,8.
Usage (depuis Project Archery/) : py -I Tools/sons/arret_du_temps.py Assets/_Project/Audio/Horloge_Temps_Arrete.wav"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lecture import write_wav

RATE = 48000
OVER = 4                      # tout est calculé à 4 fois le taux : la saturation ne se replie pas dans l'aigu
SR = RATE * OVER
CUT = 1.4                     # la coupure : le temps s'arrête
TAIL = 0.4                    # ce qui reste après
rng = np.random.default_rng(1989)
t = np.arange(int((CUT + TAIL) * SR)) / SR


def phase(freq):
    return 2 * np.pi * np.cumsum(freq) / SR


def window(a, b, fade_in, fade_out):
    # 1 entre a et b, avec des fondus en cosinus.
    w = np.clip((t - a) / max(fade_in, 1e-6), 0, 1) * np.clip((b - t) / max(fade_out, 1e-6), 0, 1)
    return 0.5 - 0.5 * np.cos(np.pi * w)


def lowpass(x, hz):
    a = np.exp(-2 * np.pi * hz / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i in range(len(x)):
        acc = (1 - a) * x[i] + a * acc
        y[i] = acc
    return y


def drive(x, k):
    x = x / max(1e-9, np.abs(x).max())
    return np.tanh(k * x) / np.tanh(k)


# La pression qui monte (à la place de l'impact du premier jet) : un très grave (42 Hz) qui enfle jusqu'à la coupure.
swell = np.clip(t / CUT, 0, 1)
pressure = drive(np.sin(phase(np.full(len(t), 42.0))), 1.5) * swell ** 1.5 * (t < CUT)

# Le bourdon qui descend : une dent de scie (ses harmoniques, assez sombre), son octave en dessous, un peu de grain ; saturé ; il
# palpite (9 par seconde) et la bande pleure un peu (5,5 par seconde).
fa = (134 - 22 * np.clip(t / 0.95, 0, 1) ** 0.8) * (1 + 0.012 * np.sin(2 * np.pi * 5.5 * t))
pa = phase(fa)
drone_a = sum(np.sin(n * pa) / n ** 0.8 for n in range(1, 41)) + 0.4 * np.sin(pa / 2) + 0.15 * rng.standard_normal(len(t))
drone_a = drive(drone_a, 7.0) * (0.75 + 0.25 * np.sin(2 * np.pi * 9 * t)) * window(0.0, 1.0, 0.4, 0.15)

# Le grave qui remonte (64 → 82 Hz), son 3e et son 5e harmoniques forts, saturé, de plus en plus fort jusqu'à la coupure.
rise = np.clip((t - 0.85) / (CUT - 0.85), 0, 1)
pb = phase(64 + 18 * rise ** 1.3)
weights = {1: 1.0, 2: 0.5, 3: 0.8, 4: 0.3, 5: 0.45, 6: 0.2, 7: 0.15, 8: 0.1}
drone_b = drive(sum(w * np.sin(n * pb) for n, w in weights.items()) + 0.1 * rng.standard_normal(len(t)), 6.0) * \
    window(0.85, CUT, 0.15, 0.0) * (0.8 + 0.4 * rise)

# Le sifflement, propre (après la saturation) : 1 669 Hz, puis 3 036 Hz jusqu'à la coupure, de plus en plus fort.
vibrato = 1 + 0.002 * np.sin(2 * np.pi * 6 * t)
whine = (0.3 * np.sin(phase(1669 * vibrato)) * window(0.95, 1.2, 0.12, 0.05) +
         (0.2 + 0.15 * rise) * np.sin(phase(3036 * vibrato)) * window(1.15, CUT, 0.05, 0.0))

# Le grain : la saturation mord dans l'aigu (moins de très grave, la présence de 0,6 à 5 kHz relevée), puis tout passe une
# deuxième fois dans la saturation.
dry = 0.5 * pressure + 0.9 * drone_a + 0.85 * drone_b
spec = np.fft.rfft(dry)
freqs = np.fft.rfftfreq(len(dry), 1 / SR)
octaves = np.log2(np.maximum(freqs, 1.0) / 1800.0)
gain_db = (12.0 * np.exp(-0.5 * (octaves / 1.1) ** 2) + 5.0 * np.exp(-0.5 * (np.log2(np.maximum(freqs, 1.0) / 320.0) / 0.7) ** 2) +
           5.0 * np.clip(np.log2(np.maximum(freqs, 1.0) / 2000.0), 0, 1) - 6.0 * np.clip((90 - freqs) / 50, 0, 1))
dry = np.fft.irfft(spec * 10 ** (gain_db / 20), len(dry))
dry = drive(dry, 2.0)

# Tout enfle jusqu'à la coupure (après la saturation, qui l'écraserait) : du tiers au plein.
dry *= 0.3 + 0.7 * swell ** 1.2

# Un peu d'espace (quatre filtres en peigne), puis tout s'arrête d'un coup à la coupure, l'écho compris : le temps s'arrête.
wet = np.zeros_like(dry)
for delay, gain in ((0.029, 0.6), (0.037, 0.58), (0.043, 0.55), (0.053, 0.5)):
    d = int(delay * SR)
    y = dry.copy()
    for i in range(d, len(y), d):
        y[i:i + d] += gain * y[i - d:i][:len(y[i:i + d])]
    wet += y
sound = dry + 0.06 * wet + whine
sound *= np.clip((CUT - t) / 0.003, 0, 1)

# La coupure : un claquement aigu (2,1 kHz, très bref), puis un coup sourd et un souffle à peine audibles.
after = np.clip(t - CUT, 0, None) * (t >= CUT)
sound += 0.5 * np.sin(2 * np.pi * 2100 * after) * np.exp(-after / 0.012) * (t >= CUT)
sound += 0.12 * np.sin(2 * np.pi * 60 * after) * np.exp(-after / 0.15) * (t >= CUT)
hiss = lowpass(rng.standard_normal(len(t)), 900)
sound += 0.03 * hiss / np.abs(hiss).max() * np.exp(-after / 0.25) * (t >= CUT)

# Au taux du jeu (par la transformée de Fourier : sans repliement), sans le continu, puis la crête à 0,8.
spec = np.fft.rfft(sound)
freqs = np.fft.rfftfreq(len(sound), 1 / SR)
spec *= np.clip((freqs - 20) / 10, 0, 1)
n = len(sound) // OVER
out = np.fft.irfft(spec[:n // 2 + 1], n) / OVER
fade = int(0.01 * RATE)
out[-fade:] *= np.linspace(1, 0, fade)
out *= 0.8 / np.abs(out).max()
write_wav(sys.argv[1], out, RATE)
rms = 20 * np.log10(np.sqrt((out[:int(CUT * RATE)] ** 2).mean()))
print('%s : %.2f s, %d Hz, coupure à %.2f s, %d dB avant la coupure' % (os.path.basename(sys.argv[1]), len(out) / RATE, RATE, CUT, round(rms)))
