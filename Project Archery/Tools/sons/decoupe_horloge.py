"""Les sons de la Grande Horloge (3 novembre), découpés dans ceux que le joueur a téléchargés (freesound, CC0) : en mono, 16 bits.
Usage (depuis Project Archery/) : py -I Tools/sons/decoupe_horloge.py Assets/_Project/Audio"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lecture import read, write_wav

AUDIO = sys.argv[1]


def load(name):
    sig, rate = read(os.path.join(AUDIO, name))
    return sig.mean(axis=1), rate


def cut(mono, rate, a, b):
    return mono[int(a * rate):int(b * rate)].copy()


def fade(x, rate, fin, fout):
    n_in, n_out = int(fin * rate), int(fout * rate)
    if n_in:
        x[:n_in] *= 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, n_in))
    if n_out:
        x[-n_out:] *= 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, n_out))
    return x


def peak(x, level):
    return x * (level / max(1e-9, np.abs(x).max()))


def highpass(x, rate, hz):
    spec = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1 / rate)
    spec *= np.clip((freqs - hz * 0.7) / (hz * 0.3), 0.0, 1.0)
    return np.fft.irfft(spec, len(x))


def resample(x, rate, to):
    # Par la transformée de Fourier (un son court) : sans repliement.
    n = int(round(len(x) * to / rate))
    spec = np.fft.rfft(x)
    out = np.zeros(n // 2 + 1, dtype=complex)
    k = min(len(spec), len(out))
    out[:k] = spec[:k]
    return np.fft.irfft(out, n) * (n / len(x))


def decay(x, rate, tau):
    # Une décroissance rapide (le souffle de l'enregistrement après le tic s'éteint).
    return x * np.exp(-np.arange(len(x)) / (tau * rate))


def drive(x, k):
    # Un écrêtage doux : les craquements, très brefs, sonnent plus pleins.
    x = peak(x, 1.0)
    return np.tanh(k * x) / np.tanh(k)


def save(name, x, rate):
    write_wav(os.path.join(AUDIO, name), x, rate)
    rms = 20 * np.log10(np.sqrt((x ** 2).mean()) + 1e-12)
    print('%-24s %6.2f s, %d Hz, crête %.2f, %d dB' % (name, len(x) / rate, rate, np.abs(x).max(), round(rms)))


# Le tic : le plus net de l'enregistrement (11,74 s), sans ses graves (le souffle y est), court, qui s'éteint vite.
tick, r = load('237210__mortifreshman__clock-ticking.wav')
x = highpass(cut(tick, r, 11.733, 11.82), r, 800)
save('Horloge_Tic.wav', peak(fade(decay(x, r, 0.02), r, 0.001, 0.01), 0.8), r)

# Un coup de cloche (42,4 s), jusqu'au suivant, ramené de 96 à 48 kHz.
bells, r = load('481969__astounded__church-bells-strike-12-midday.wav')
x = resample(cut(bells, r, 42.37, 45.05), r, 48000)
save('Horloge_Cloche.wav', peak(fade(x, 48000, 0.003, 0.45), 0.9), 48000)

# Le temps qui s'arrête : la dernière seconde de la cymbale à l'envers (son sommet, puis la coupure nette) ; et qui reprend : la même,
# remise à l'endroit (le coup de cymbale, qui s'éteint).
cymbal, r = load('383903__deleted_user_7146007__reverse-crash-cymbal-projection.wav')
save('Horloge_Arret.wav', peak(fade(cut(cymbal, r, 7.6, 8.61), r, 0.15, 0.006), 0.9), r)
save('Horloge_Reprise.wav', peak(fade(cut(cymbal, r, 6.1, 8.61)[::-1].copy(), r, 0.004, 0.5), 0.9), r)

# La glace qui éclate : la série de craquements (5 à 7,2 s) ; un craquement, trois coups serrés (6,25 à 6,7 s).
ice, r = load('329744__humanoide9000__ice-break.wav')
save('Horloge_Bris.wav', peak(fade(drive(cut(ice, r, 4.97, 7.25), 4.0), r, 0.005, 0.3), 0.9), r)
save('Horloge_Eclat.wav', peak(fade(drive(cut(ice, r, 6.235, 6.78), 4.0), r, 0.003, 0.12), 0.9), r)

# Le carillon : depuis son attaque (1,23 s), jusqu'à ce qu'il s'éteigne.
chime, r = load('351408__newagesoup__gleam-glow-sfx-chime.wav')
save('Horloge_Carillon.wav', peak(fade(cut(chime, r, 1.22, 4.6), r, 0.004, 0.8), 0.85), r)
