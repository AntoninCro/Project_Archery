"""Cherche une boucle : le départ (entre a et b) et la longueur (entre lmin et lmax) pour lesquels le rythme (l'enveloppe des
attaques, par 20 ms) de la fin ressemble le plus à celui du début, sur <fenetre> s.
Usage : py -I Tools/sons/boucle.py <son> <départ min> <départ max> <longueur min> <longueur max> <fenêtre s> <fin s>"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lecture import read

path = sys.argv[1]
a, b, lmin, lmax, window, end = (float(x) for x in sys.argv[2:8])
sig, rate = read(path)
mono = sig.mean(axis=1)
hop = int(rate * 0.02)
frames = len(mono) // hop
env = np.sqrt((mono[:frames * hop].reshape(frames, hop) ** 2).mean(axis=1))
onset = np.maximum(0.0, np.diff(np.log(env + 1e-6), prepend=0.0))
w = int(window / 0.02)
best = []
for s in np.arange(a, b, 0.02):
    i = int(s / 0.02)
    for length in np.arange(lmin, lmax, 0.02):
        j = i + int(length / 0.02)
        if (j + w) * 0.02 > end:
            break
        x, y = onset[i:i + w], onset[j:j + w]
        if x.std() == 0 or y.std() == 0:
            continue
        c = np.corrcoef(x, y)[0, 1]
        level = abs(20 * np.log10((env[i:i + w].mean() + 1e-9) / (env[j:j + w].mean() + 1e-9)))
        best.append((c - 0.02 * level, c, level, s, length))
best.sort(reverse=True)
for score, c, level, s, length in best[:6]:
    print('départ %.2f s, longueur %.2f s : corrélation %.2f, écart de niveau %.1f dB' % (s, length, c, level))
