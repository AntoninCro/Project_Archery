"""Pour chaque son : format, durée, et l'enveloppe du volume (dB, toutes les <pas> s)."""
import os
# Usage (depuis Project Archery/) : py -I Tools/sons/analyse.py 0.25 Assets/_Project/Audio/<son>.wav [...]
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lecture import read

step = float(sys.argv[1])
for path in sys.argv[2:]:
    a, rate = read(path)
    mono = a.mean(axis=1)
    n = int(rate * step)
    blocks = len(mono) // n
    rms = np.sqrt((mono[:blocks * n].reshape(blocks, n) ** 2).mean(axis=1) + 1e-12)
    db = 20 * np.log10(rms)
    print('%s : %d canaux, %d Hz, %.2f s, crête %.2f' % (os.path.basename(path), a.shape[1], rate, len(mono) / rate, np.abs(mono).max()))
    line = ' '.join('%d' % round(v) for v in db)
    print('  dB/%.2fs : %s' % (step, line))
