"""Les sons de la batte de baseball (13 novembre au soir), faits à partir de deux sons de freesound (CC0) choisis par le joueur :
- Audio/Batte_Coup.wav : « Bat_hit2 » de hashtagsmcgee (une batte d'aluminium sur une balle, un « ping » sec), recoupé, pour les
  coups qui ne sont pas parfaits ;
- Audio/Batte_Parfait.wav : le coup du parfait (le joueur : « le gros impact, mais il faudrait bien le saturer pour plus d'impact,
  en réduisant tout de même le son pour éviter de nous exploser les oreilles ») : « Big impact » d'AudioPapkin, saturé fort (le
  grave, qui fait presque tout ce son, devient un grondement granuleux qui s'entend aussi dans les petits haut-parleurs du
  casque), avec le coup de batte par-dessus pour l'attaque, puis ramené au volume de l'explosion de la grenade.
Mono, 16 bits, 48 kHz. Usage (depuis Project Archery/) :
py -I Tools/sons/batte.py <bat_hit2.wav> <big_impact.wav> Assets/_Project/Audio
(les MP3 de freesound passent d'abord en WAV par Tools/sons/mp3_en_wav.ps1)."""
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from lecture import read, write_wav

HIT, IMPACT, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
RATE = 48000

# Le parfait : la saturation (tanh, gain avant elle), la part du son saturé, la durée et le fondu de la fin, le volume visé (le
# RMS de la fenêtre de 300 ms la plus forte : celui de l'explosion de la grenade est à -11 dB, le tir parfait de l'arc à -10,5).
DRIVE = 9.0
TUBE_DRIVE = 2.5
TUBE_BIAS = 0.3
WET = 0.9
HOLD = 0.12
DECAY = 0.55
LENGTH = 2.2
FADE_FROM = 1.2
TARGET_RMS_DB = -12.5
PEAK_LIMIT = 0.85

# Le coup : sa crête.
HIT_PEAK = 0.8


def load(path):
    sig, rate = read(path)
    mono = sig.mean(axis=1)
    if rate != RATE:
        # Les deux sons de freesound sont convertis à 48 kHz par Windows ; sinon, un rééchantillonnage simple.
        t = np.arange(int(len(mono) * RATE / rate)) / RATE
        mono = np.interp(t, np.arange(len(mono)) / rate, mono)
    return mono


def onset(x, threshold=0.05):
    # Le début du son : le premier échantillon au-dessus du seuil (en part de la crête), 2 ms avant.
    i = int(np.argmax(np.abs(x) >= threshold * np.abs(x).max()))
    return max(0, i - int(0.002 * RATE))


def fade_out(x, start, end):
    a, b = int(start * RATE), min(len(x), int(end * RATE))
    x = x[:b].copy()
    if b > a:
        x[a:b] *= 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, b - a))
    return x


def loudest_rms_db(x, window=0.3):
    n = int(window * RATE)
    c = np.convolve(x ** 2, np.ones(n) / n, mode='valid')
    return 10 * np.log10(c.max() + 1e-12)


def one_pole_highpass(x, cutoff):
    # Retire le très grave (sous ~30 Hz) avant la saturation : il ne s'entend pas et mangerait la marge.
    rc = 1 / (2 * np.pi * cutoff)
    alpha = rc / (rc + 1 / RATE)
    y = np.zeros_like(x)
    for i in range(1, len(x)):
        y[i] = alpha * (y[i - 1] + x[i] - x[i - 1])
    return y


hit = load(HIT)
hit = hit[onset(hit):]
hit = fade_out(hit, 0.45, 0.62)
hit *= HIT_PEAK / np.abs(hit).max()
write_wav(os.path.join(OUT, 'Batte_Coup.wav'), hit, RATE)

impact = load(IMPACT)
impact = impact[onset(impact):][:int((LENGTH + 0.1) * RATE)]
impact = one_pole_highpass(impact, 30.0)
impact /= np.abs(impact).max()

# Deux étages : un écrêtage doux et fort (les harmoniques impaires : le grave devient granuleux), puis une saturation
# asymétrique, comme une lampe (les harmoniques paires : plus chaud, plus sale) ; le décalage qu'elle laisse est retiré.
saturated = np.tanh(DRIVE * impact) / np.tanh(DRIVE)
saturated = np.tanh(TUBE_DRIVE * (saturated + TUBE_BIAS * saturated ** 2))
saturated = one_pole_highpass(saturated, 20.0)
saturated /= np.abs(saturated).max()
impact = WET * saturated + (1 - WET) * impact

# La saturation écrase la retombée (le grondement resterait fort 1,2 s) : après le choc, le son redescend.
t = np.arange(len(impact)) / RATE
impact *= np.where(t < HOLD, 1.0, np.exp(-(t - HOLD) / DECAY))

# Le coup de batte d'abord (l'attaque), l'impact 5 ms après.
mix = np.zeros(int(LENGTH * RATE))
shift = int(0.005 * RATE)
mix[shift:shift + len(impact)] += impact[:len(mix) - shift]
crack = hit / np.abs(hit).max()
mix[:len(crack)] += 0.9 * crack[:len(mix)]
mix = fade_out(mix, FADE_FROM, LENGTH)

# Le volume : le RMS visé, sans dépasser la crête.
mix *= 10 ** ((TARGET_RMS_DB - loudest_rms_db(mix)) / 20)
peak = np.abs(mix).max()
if peak > PEAK_LIMIT:
    mix *= PEAK_LIMIT / peak
write_wav(os.path.join(OUT, 'Batte_Parfait.wav'), mix, RATE)

for name, x in (('Batte_Coup', hit), ('Batte_Parfait', mix)):
    print('%s : %.2f s, crête %.2f, RMS max 300 ms %.1f dB' % (name, len(x) / RATE, np.abs(x).max(), loudest_rms_db(x)))
