"""Les textures du bois peint du Grand Manège (l'ultime de la fête foraine) : Assets/_Project/Art/TowerDefense/Manege_Bois.png (la
couleur) et Manege_Bois_Relief.png (le relief, une carte de normales).

Faites le 2 novembre, tard, avec le Manège (Docs/Ultimes_Cahier.md : « un modèle fini, une texture à lui », ce que le joueur a
demandé pour le Colosse). Un bois de carrousel peint et verni : le fil du bois sous la peinture, les coups de pinceau, le vernis
craquelé, quelques éclats où le bois apparaît. La couleur est presque blanche : le matériau la teinte (le rouge et le crème du
chapiteau, la robe des chevaux). Les deux textures se raccordent sur leurs bords (bruits faits par transformée de Fourier, cellules
du craquelé repliées sur le carré). Le baker (Editor/ManegeFinitionBaker) les met sur les matériaux du Manège.

Usage (depuis Project Archery/) : py Tools/texture_bois_peint.py
"""
import os

import numpy as np
from PIL import Image

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDER = os.path.join(PROJECT, "Assets", "_Project", "Art", "TowerDefense")
SIZE = 512
RNG = np.random.default_rng(2111)


def smooth_noise(scale_x, scale_y=None):
    # Un bruit qui se raccorde : du blanc filtré dans les fréquences (scale : la taille des taches, en part du carré ; plus grand en x
    # qu'en y, il s'étire en travers : le fil du bois).
    scale_y = scale_x if scale_y is None else scale_y
    white = RNG.normal(size=(SIZE, SIZE))
    fx = np.fft.fftfreq(SIZE)[None, :]
    fy = np.fft.fftfreq(SIZE)[:, None]
    falloff = np.exp(-(fx * scale_x * SIZE) ** 2 - (fy * scale_y * SIZE) ** 2)
    noise = np.real(np.fft.ifft2(np.fft.fft2(white) * falloff))
    return (noise - noise.min()) / (noise.max() - noise.min())


def grain():
    # Le fil du bois : des veines en travers, ondulées par un bruit large, plus serrées par endroits.
    y = (np.arange(SIZE) + 0.5)[:, None] / SIZE
    warp = smooth_noise(0.12, 0.05) - 0.5
    rings = np.sin((y * 34.0 + warp * 3.0) * 2.0 * np.pi)
    fine = smooth_noise(0.25, 0.004)
    return np.clip(0.5 + 0.35 * rings + 0.3 * (fine - 0.5), 0.0, 1.0)


def craquelure(count):
    # Le vernis craquelé : les bords des cellules (la distance au deuxième point le plus proche moins celle au premier, petite),
    # repliés sur le carré.
    grid = (np.arange(SIZE) + 0.5) / SIZE
    x, y = np.meshgrid(grid, grid)
    first = np.full((SIZE, SIZE), 9.0)
    second = np.full((SIZE, SIZE), 9.0)
    for cx, cy in RNG.random((count, 2)):
        dx = (x - cx + 0.5) % 1.0 - 0.5
        dy = (y - cy + 0.5) % 1.0 - 0.5
        d = np.sqrt(dx * dx + dy * dy * 1.6)
        closer = d < first
        second = np.where(closer, first, np.minimum(second, d))
        first = np.where(closer, d, first)
    edge = second - first
    return np.clip(1.0 - edge / 0.004, 0.0, 1.0)


def main():
    wood = grain()
    strokes = smooth_noise(0.35, 0.006)
    blotch = smooth_noise(0.05)
    chips = smooth_noise(0.03)
    cracks = craquelure(45) * np.clip((blotch - 0.58) * 4.0, 0.0, 1.0)
    worn = np.clip((chips - 0.78) * 7.0, 0.0, 1.0)

    # Le relief : le fil du bois sous la peinture (doux), les coups de pinceau, le craquelé creusé, les éclats un peu plus bas.
    height = wood * 0.35 + strokes * 0.3 - cracks * 0.3 - worn * 0.35
    gx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
    gy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5
    strength = 7.0
    # Unity lit les normales à la manière d'OpenGL (vert vers le haut de la texture, les lignes de l'image vont vers le bas).
    normal = np.stack([-gx * strength, gy * strength, np.ones_like(height)], axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    relief = ((normal * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(relief, "RGB").save(os.path.join(FOLDER, "Manege_Bois_Relief.png"))

    # La couleur : presque blanche, le fil du bois à peine visible sous la peinture, les coups de pinceau, le craquelé sombre, un
    # vernis un peu jauni par endroits, le bois brun dans les éclats.
    shade = 0.9 + 0.08 * (wood - 0.5) + 0.04 * (strokes - 0.5) - 0.09 * cracks
    rgb = np.stack([shade, shade * 0.985, shade * 0.95], axis=-1)
    yellow = np.clip((blotch - 0.6) * 2.5, 0.0, 1.0)[..., None]
    rgb = rgb * (1.0 - 0.08 * yellow) + np.array([0.95, 0.85, 0.6]) * 0.08 * yellow
    bare = np.stack([0.55 + 0.15 * wood, 0.38 + 0.1 * wood, 0.24 + 0.06 * wood], axis=-1)
    rgb = rgb * (1.0 - worn[..., None]) + bare * worn[..., None]
    Image.fromarray((np.clip(rgb, 0.0, 1.0) * 255 + 0.5).astype(np.uint8), "RGB").save(os.path.join(FOLDER, "Manege_Bois.png"))
    print("Écrit : Manege_Bois.png, Manege_Bois_Relief.png (" + FOLDER + ")")


if __name__ == "__main__":
    main()
