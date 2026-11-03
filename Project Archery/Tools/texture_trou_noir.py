"""La texture du trou noir de la singularité (tour-aimant, niveau 5) : Assets/_Project/Art/TowerDefense/Trou_noir.png.

Demandée par le joueur le 2 novembre (« un poil plus stylé avec une vraie texture ») : la sphère du trou noir était d'un violet
uni. Une texture équirectangulaire (la sphère de Unity : u autour, v du pôle sud au pôle nord) : des pôles noirs, un disque
d'accrétion qui brille à l'équateur, violet puis magenta puis presque blanc au plus chaud, et des bras en spirale qui tournent avec
la sphère. Elle se raccorde sur u (des fréquences entières). Le baker (Editor/BalanceBaker) la met en couleur et en émission du
matériau Trou_noir.

Usage (depuis Project Archery/) : py Tools/texture_trou_noir.py
"""
import os

import numpy as np
from PIL import Image

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUTPUT = os.path.join(PROJECT, "Assets", "_Project", "Art", "TowerDefense", "Trou_noir.png")
WIDTH, HEIGHT = 1024, 512


def main():
    u = np.linspace(0.0, 1.0, WIDTH, endpoint=False)[None, :]
    v = (np.arange(HEIGHT)[:, None] + 0.5) / HEIGHT
    lat = (v - 0.5) * np.pi                              # -π/2 au pôle sud, π/2 au pôle nord
    turn = 2 * np.pi * u
    # Un bruit qui se raccorde : des sinus de fréquences entières sur u.
    noise = (np.sin(3 * turn + 5.0 * lat) * 0.5 + np.sin(7 * turn - 11.0 * lat + 1.3) * 0.3 + np.sin(13 * turn + 17.0 * lat + 2.1) * 0.2)
    # Le disque d'accrétion : une bande à l'équateur, striée en spirale.
    band = np.exp(-(lat / 0.22) ** 2)
    streaks = 0.5 + 0.5 * np.sin(6 * turn + 26.0 * lat + 1.5 * noise)
    disk = band * (0.35 + 0.65 * streaks)
    # Les bras qui montent vers les pôles en s'enroulant, plus faibles.
    arms = np.exp(-(lat / 0.75) ** 2) * (0.5 + 0.5 * np.sin(3 * turn - 9.0 * lat + noise)) ** 3 * 0.45
    # Le cœur de l'équateur, presque blanc ; les pôles, noirs (l'horizon).
    core = np.exp(-(lat / 0.06) ** 2) * (0.6 + 0.4 * streaks)
    heat = np.clip(disk + arms + 0.6 * core, 0.0, 1.0)

    # Noir → violet profond → magenta → presque blanc.
    stops = np.array([0.0, 0.35, 0.7, 1.0])
    colors = np.array([[0.0, 0.0, 0.0], [0.22, 0.03, 0.42], [0.85, 0.2, 0.95], [1.0, 0.88, 1.0]])
    rgb = np.stack([np.interp(heat, stops, colors[:, channel]) for channel in range(3)], axis=-1)
    Image.fromarray((rgb * 255 + 0.5).astype(np.uint8), "RGB").save(OUTPUT)
    print(f"Écrit : {OUTPUT}")


if __name__ == "__main__":
    main()
