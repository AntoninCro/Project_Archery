"""Les sons du Grand Manège (2 novembre, tard), découpés dans ceux que le joueur a téléchargés (freesound, CC0) : en mono, 16 bits,
au taux de l'original. Usage (depuis Project Archery/) : py -I Tools/sons/decoupe_manege.py Assets/_Project/Audio"""
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


def loop(mono, rate, start, length, cross):
    # Une boucle : la fin (avec ce qui suit) fondue dans le début, sur <cross> s ; elle repart sans raccord.
    seg = cut(mono, rate, start, start + length + cross)
    n, x = int(length * rate), int(cross * rate)
    out = seg[:n].copy()
    t = np.linspace(0, np.pi / 2, x)
    out[:x] = seg[:x] * np.sin(t) + seg[n:n + x] * np.cos(t)
    return out


def flatten(x, rate, window=0.25, limit=2.5):
    # Un niveau égal : un gain qui suit doucement l'enveloppe, vers sa médiane (la troupe qui passe et enfle).
    hop = int(rate * window)
    frames = len(x) // hop + 1
    rms = np.array([np.sqrt((x[i * hop:(i + 1) * hop] ** 2).mean() + 1e-12) for i in range(frames)])
    target = np.median(rms)
    gain = np.clip(target / rms, 1 / limit, limit)
    kernel = np.hanning(5) / np.hanning(5).sum()
    gain = np.convolve(np.concatenate([gain[-2:], gain, gain[:2]]), kernel, mode='valid')
    centers = (np.arange(frames) + 0.5) * hop
    return x * np.interp(np.arange(len(x)), centers, gain, period=len(x))


def highpass(x, rate, hz):
    spec = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1 / rate)
    spec *= np.clip((freqs - hz * 0.7) / (hz * 0.3), 0.0, 1.0)
    return np.fft.irfft(spec, len(x))


def save(name, x, rate):
    write_wav(os.path.join(AUDIO, name), x, rate)
    rms = 20 * np.log10(np.sqrt((x ** 2).mean()) + 1e-12)
    seam = abs(x[-1] - x[0])
    print('%-28s %6.2f s, %d Hz, crête %.2f, %d dB, raccord %.3f' % (name, len(x) / rate, rate, np.abs(x).max(), round(rms), seam))


organ, r = load('64562__ohrwurm__carousel-organ.aiff')
save('Manege_Orgue.wav', peak(loop(organ, r, 16.82, 16.40, 0.6), 0.85), r)

fanfare, r = load('350428__bevibeldesign__trumpet-fanfare.aiff')
save('Manege_Fanfare.wav', peak(fade(cut(fanfare, r, 0.03, 7.0), r, 0.01, 0.6), 0.9), r)
save('Manege_Fanfare_Courte.wav', peak(fade(cut(fanfare, r, 0.03, 1.65), r, 0.01, 0.23), 0.9), r)

herd, r = load('547967__gurek__a_herd_of_horses.wav')
gallop = flatten(loop(herd, r, 10.60, 2.66, 0.3), r)
save('Manege_Galop.wav', peak(gallop, 0.9), r)

neigh, r = load('510903__lydmakeren__horse_neighing.wav')
save('Manege_Hennissement.wav', peak(fade(cut(neigh, r, 5.02, 6.5), r, 0.01, 0.35), 0.9), r)

firework, r = load('336008__rudmer_rotteveel__whistle-and-explosion-single_firework.wav')
whistle = highpass(cut(firework, r, 0.0, 1.13), r, 1500)
save('Manege_Fusee.wav', peak(fade(whistle, r, 0.03, 0.05), 0.6), r)
save('Manege_Bouquet.wav', peak(fade(cut(firework, r, 1.15, 2.1), r, 0.002, 0.4), 0.95), r)
