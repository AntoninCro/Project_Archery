"""Les attaques les plus nettes d'un son (un tic, un coup) : leur temps, leur crête, et combien elles dépassent le bruit autour (dB).
Usage (depuis Project Archery/) : py -I Tools/sons/attaques.py <son> <début s> <fin s> [combien]"""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lecture import read

path, start, end = sys.argv[1], float(sys.argv[2]), float(sys.argv[3])
count = int(sys.argv[4]) if len(sys.argv) > 4 else 8
sig, rate = read(path)
mono = sig.mean(axis=1)[int(start * rate):int(end * rate)]
hop = int(rate * 0.005)
frames = len(mono) // hop
env = np.abs(mono[:frames * hop]).reshape(frames, hop).max(axis=1)
found = []
for i in range(2, frames - 2):
    if env[i] < env[i - 2:i + 3].max():
        continue
    around = np.concatenate([env[max(0, i - 30):max(0, i - 6)], env[i + 20:i + 40]])
    if len(around) == 0:
        continue
    noise = np.median(around) + 1e-9
    found.append((20 * np.log10(max(env[i], 1e-9) / noise), start + i * 0.005, env[i]))
found.sort(reverse=True)
for ratio, t, peak in found[:count]:
    print('%.3f s : crête %.3f, %.0f dB au-dessus du bruit' % (t, peak, ratio))
