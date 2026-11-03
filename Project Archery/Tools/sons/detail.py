"""L'enveloppe d'un son entre deux temps : un point toutes les <pas> s, avec le temps (dB et crête)."""
import os
# Usage : py -I Tools/sons/detail.py <son> <pas s> <début s> <fin s>
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lecture import read

path, step, start, end = sys.argv[1], float(sys.argv[2]), float(sys.argv[3]), float(sys.argv[4])
a, rate = read(path)
mono = a.mean(axis=1)
n = int(rate * step)
out = []
t = start
while t + step <= min(end, len(mono) / rate):
    seg = mono[int(t * rate):int(t * rate) + n]
    out.append('%.2f:%d/%.2f' % (t, round(20 * np.log10(np.sqrt((seg ** 2).mean()) + 1e-12)), np.abs(seg).max()))
    t += step
print(os.path.basename(path))
for i in range(0, len(out), 10):
    print('  ' + '  '.join(out[i:i + 10]))
