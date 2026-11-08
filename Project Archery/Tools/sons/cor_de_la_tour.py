"""Le cor de la tour (3 novembre) : synthétisé, un son original. Il sonne quand la tour est attaquée, depuis la tour, en son 3D :
où qu'il soit sur la carte, le joueur l'entend venir de sa direction (le joueur a refusé l'alerte sur la montre ; Docs/
Refonte_TowerDefense.md, 12.59). Un cor de guerre grave (do 3, 131 Hz) : la lèvre qui attaque un peu bas et monte à la note, le
son sombre au départ qui s'éclaircit en enflant (les harmoniques hautes grandissent plus vite que la fondamentale, comme un cuivre),
une octave dessous pour le poids, un peu de souffle, un vibrato tardif, puis l'écho d'une vallée. Environ 1,7 s ; joué une, deux
ou trois fois selon les PV de la tour (World/Lighting/MapGameplayLights).
Usage (depuis Project Archery/) : py -I Tools/sons/cor_de_la_tour.py Assets/_Project/Audio/Tour_Cor.wav"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lecture import write_wav

RATE = 48000
OVER = 4                      # calculé à 4 fois le taux : la saturation ne se replie pas dans l'aigu
SR = RATE * OVER
NOTE = 1.25                   # la note tenue, attaque comprise
RELEASE = 0.35
ECHO = 0.55                   # ce que l'écho ajoute après
F0 = 130.8
rng = np.random.default_rng(1103)
t = np.arange(int((NOTE + RELEASE + ECHO) * SR)) / SR


def phase(freq):
    return 2 * np.pi * np.cumsum(freq) / SR


def drive(x, k):
    x = x / max(1e-9, np.abs(x).max())
    return np.tanh(k * x) / np.tanh(k)


# L'enveloppe : la lèvre attaque (0,09 s, un petit excès), la note enfle jusqu'au bout, puis s'éteint en 0,35 s.
attack = np.clip(t / 0.09, 0, 1)
env = np.sin(0.5 * np.pi * attack) ** 2 * (1 + 0.12 * np.exp(-((t - 0.1) / 0.05) ** 2))
env *= 0.78 + 0.22 * np.clip((t - 0.1) / (NOTE - 0.1), 0, 1)
release = np.clip((t - NOTE) / RELEASE, 0, 1)
env *= 0.5 + 0.5 * np.cos(np.pi * release)
env *= t < NOTE + RELEASE

# La hauteur : 2,5 demi-tons dessous à l'attaque, rejoints en 0,06 s ; un vibrato léger (4,6 par seconde) qui vient après 0,5 s ;
# elle retombe un peu à la fin, quand le souffle manque.
scoop = -2.5 * np.exp(-t / 0.06)
fall = -0.6 * release ** 2
vibrato = 0.25 * np.clip((t - 0.5) / 0.4, 0, 1) * np.sin(2 * np.pi * 4.6 * t)
freq = F0 * 2 ** ((scoop + fall + vibrato) / 12)
p = phase(freq)

# Le cuivre : l'harmonique n pèse n^-0,85 à pleine force, et suit l'enveloppe à la puissance 1 + 0,22 (n - 1) : sombre au départ,
# éclatant en enflant.
level = np.clip(env, 1e-4, None)
horn = sum(level ** (1 + 0.22 * (n - 1)) * n ** -0.85 * np.sin(n * p) for n in range(1, 34))
horn += 0.35 * level ** 1.3 * np.sin(p / 2)      # l'octave dessous

# Le souffle : un bruit autour de 1,2 kHz, plus fort à l'attaque.
noise = rng.standard_normal(len(t))
spec = np.fft.rfft(noise)
freqs = np.fft.rfftfreq(len(noise), 1 / SR)
spec *= np.exp(-0.5 * (np.log2(np.maximum(freqs, 1.0) / 1200.0) / 0.8) ** 2)
breath = np.fft.irfft(spec, len(noise))
breath /= np.abs(breath).max()
horn += 0.06 * breath * (0.4 * env + 1.2 * np.exp(-t / 0.08) * (t < NOTE))

# Un peu de grain (la saturation), puis moins d'aigu au-dessus de 4 kHz (un cor, pas une trompette) et un peu plus de corps vers 250 Hz.
dry = drive(horn, 1.6) * env.max()
spec = np.fft.rfft(dry)
freqs = np.fft.rfftfreq(len(dry), 1 / SR)
gain_db = -9.0 * np.clip(np.log2(np.maximum(freqs, 1.0) / 4000.0), 0, 2) + \
    3.0 * np.exp(-0.5 * (np.log2(np.maximum(freqs, 1.0) / 250.0) / 0.8) ** 2)
dry = np.fft.irfft(spec * 10 ** (gain_db / 20), len(dry))

# L'écho d'une vallée : deux retours lointains, plus sombres, et un peu d'espace (filtres en peigne).
sound = dry.copy()
for delay, gain in ((0.21, 0.28), (0.47, 0.14)):
    d = int(delay * SR)
    sound[d:] += gain * dry[:-d]
wet = np.zeros_like(dry)
for delay, gain in ((0.031, 0.55), (0.041, 0.5), (0.047, 0.48), (0.059, 0.45)):
    d = int(delay * SR)
    y = dry.copy()
    for i in range(d, len(y), d):
        y[i:i + d] += gain * y[i - d:i][:len(y[i:i + d])]
    wet += y
sound += 0.035 * wet

# Au taux du jeu (par la transformée de Fourier : sans repliement), sans le continu, la fin en fondu, la crête à 0,8.
spec = np.fft.rfft(sound)
freqs = np.fft.rfftfreq(len(sound), 1 / SR)
spec *= np.clip((freqs - 25) / 15, 0, 1)
n = len(sound) // OVER
out = np.fft.irfft(spec[:n // 2 + 1], n) / OVER
fade = int(0.3 * RATE)
out[-fade:] *= np.linspace(1, 0, fade) ** 2
out *= 0.8 / np.abs(out).max()
write_wav(sys.argv[1], out, RATE)
rms = 20 * np.log10(np.sqrt((out[:int(NOTE * RATE)] ** 2).mean()))
print('%s : %.2f s, %d Hz, note de %.2f s, %d dB pendant la note' % (os.path.basename(sys.argv[1]), len(out) / RATE, RATE, NOTE, round(rms)))
