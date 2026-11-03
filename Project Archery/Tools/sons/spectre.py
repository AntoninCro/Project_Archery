"""La fréquence dominante d'un son, par tranches (et la part de l'énergie dans son pic : un sifflement est un pic net)."""
import os
# Usage : py -I Tools/sons/spectre.py <son> <pas s> <début s> <fin s>
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lecture import read

path, step, start, end = sys.argv[1], float(sys.argv[2]), float(sys.argv[3]), float(sys.argv[4])
a, rate = read(path)
mono = a.mean(axis=1)
n = int(rate * step)
t = start
while t + step <= end:
    seg = mono[int(t * rate):int(t * rate) + n] * np.hanning(n)
    spec = np.abs(np.fft.rfft(seg)) ** 2
    freqs = np.fft.rfftfreq(n, 1 / rate)
    k = spec[1:].argmax() + 1
    band = spec[max(1, k - 3):k + 4].sum() / spec[1:].sum()
    print('%.2f s : %5d Hz, %2d %% de l\'énergie dans le pic' % (t, freqs[k], band * 100))
    t += step
