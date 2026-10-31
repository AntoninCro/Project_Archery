# Recoloration des textures de couleurs de Kenney (colormap) : chaque case prend la couleur de son rôle dans la palette d'une
# tour (pierre, sable, bois, métal, blanc, accent), en gardant son dégradé.
import colorsys
import numpy as np

REF_V = {'pierre': 0.84, 'metal': 0.56, 'blanc': 0.93, 'bois': 0.80, 'sable': 0.90, 'accent': 0.88}

def hexrgb(h):
    h = h.lstrip('#')
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)])

def role(h, s, v):
    """Rôle d'une couleur de la palette de Kenney (h en degrés)."""
    if v < 0.33 and s < 0.3:
        return 'noir'
    if s < 0.28 and (200 <= h <= 260 or s < 0.12):
        if s < 0.12 and v > 0.84:
            return 'blanc'
        return 'pierre' if v >= 0.72 else 'metal'
    if h <= 20.5 and 0.42 <= s < 0.66:
        return 'bois'
    if 20.5 < h <= 36 and s < 0.6:
        return 'sable'
    return 'accent'

def recolor(img, pal):
    """img : HxWx3 (0..1). pal : rôle -> '#rrggbb'. Renvoie la texture recolorée."""
    out = img.copy()
    hsv = np.zeros_like(img)
    flat = img.reshape(-1, 3)
    res = out.reshape(-1, 3)
    targets = {k: colorsys.rgb_to_hsv(*hexrgb(v)) for k, v in pal.items()}
    cache = {}
    for i, (r, g, b) in enumerate(flat):
        key = (round(r, 3), round(g, 3), round(b, 3))
        if key in cache:
            res[i] = cache[key]
            continue
        h, s, v = colorsys.rgb_to_hsv(r, g, b)
        ro = role(h * 360, s, v)
        if ro == 'noir' or ro not in targets or (r + g + b) == 0:
            cache[key] = (r, g, b)
            res[i] = cache[key]
            continue
        th, ts, tv = targets[ro]
        nv = min(1.0, tv * v / REF_V[ro])
        ns = ts * (min(1.4, s / 0.62) if ro == 'accent' else 1.0)
        cache[key] = colorsys.hsv_to_rgb(th, min(1.0, ns), nv)
        res[i] = cache[key]
    return out

def flat(pal, ro, shade=1.0):
    """La couleur plate d'un rôle (pour les formes simples), éclaircie ou assombrie."""
    c = hexrgb(pal[ro])
    return np.clip(c * shade, 0, 1)
