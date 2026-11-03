"""Les textures de la glace de la Grande Horloge (l'ultime de la glace et du temps) : Assets/_Project/Art/TowerDefense/Horloge_Glace.png
(la couleur) et Horloge_Glace_Relief.png (le relief, une carte de normales).

Faites le 2 novembre, tard, avec l'Horloge (Docs/Ultimes_Cahier.md : « un modèle fini, une texture à lui »). Une glace taillée : des
fêlures blanches qui la traversent, de petites bulles prises dedans, des plaques de givre, un léger voile. La couleur est presque
blanche : le matériau la teinte (la glace claire, la glace sombre du cadran). Les deux textures se raccordent sur leurs bords (bruits
faits par transformée de Fourier, fêlures et bulles repliées sur le carré). Le baker (Editor/HorlogeFinitionBaker) les met sur les
matériaux de l'Horloge.

Usage (depuis Project Archery/) : py Tools/texture_glace.py
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDER = os.path.join(PROJECT, "Assets", "_Project", "Art", "TowerDefense")
SIZE = 512
RNG = np.random.default_rng(1212)


def smooth_noise(scale):
    # Un bruit qui se raccorde : du blanc filtré dans les fréquences (scale : la taille des taches, en part du carré).
    white = RNG.normal(size=(SIZE, SIZE))
    fx = np.fft.fftfreq(SIZE)[None, :]
    fy = np.fft.fftfreq(SIZE)[:, None]
    falloff = np.exp(-(fx ** 2 + fy ** 2) * (scale * SIZE) ** 2)
    noise = np.real(np.fft.ifft2(np.fft.fft2(white) * falloff))
    return (noise - noise.min()) / (noise.max() - noise.min())


def cracks(count):
    # Les fêlures : des lignes brisées qui partent d'un point et bifurquent, tracées trois fois de chaque côté pour se raccorder.
    image = Image.new("L", (SIZE * 3, SIZE * 3), 0)
    draw = ImageDraw.Draw(image)
    for _ in range(count):
        x, y = RNG.random(2) * SIZE
        angle = RNG.uniform(0, 2 * np.pi)
        points = [(x, y)]
        for _ in range(int(RNG.integers(4, 9))):
            angle += RNG.uniform(-0.6, 0.6)
            step = RNG.uniform(10, 40)
            x, y = x + np.cos(angle) * step, y + np.sin(angle) * step
            points.append((x, y))
        value = int(RNG.uniform(140, 255))
        for ox in range(3):
            for oy in range(3):
                draw.line([(px + ox * SIZE, py + oy * SIZE) for px, py in points], fill=value, width=1)
    image = image.filter(ImageFilter.GaussianBlur(0.7))
    return np.asarray(image, dtype=np.float64)[SIZE:2 * SIZE, SIZE:2 * SIZE] / 255.0


def bubbles(count):
    # De petites bulles prises dans la glace, repliées sur le carré.
    grid = (np.arange(SIZE) + 0.5) / SIZE
    x, y = np.meshgrid(grid, grid)
    out = np.zeros((SIZE, SIZE))
    for _ in range(count):
        cx, cy = RNG.random(2)
        radius = RNG.uniform(0.002, 0.008)
        dx = (x - cx + 0.5) % 1.0 - 0.5
        dy = (y - cy + 0.5) % 1.0 - 0.5
        d = np.sqrt(dx * dx + dy * dy) / radius
        out = np.maximum(out, np.where(d < 1.0, 1.0 - d * d, 0.0))
    return out


def main():
    veil = smooth_noise(0.05)
    grain = smooth_noise(0.006)
    frost = np.clip((smooth_noise(0.03) - 0.62) * 4.0, 0.0, 1.0)
    lines = cracks(55)
    dots = bubbles(260)

    # Le relief : les fêlures creusées, le givre un peu granuleux, les bulles à peine bombées ; la glace reste lisse.
    height = -lines * 0.6 + frost * grain * 0.5 + dots * 0.15 + veil * 0.1
    gx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
    gy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5
    strength = 6.0
    # Unity lit les normales à la manière d'OpenGL (vert vers le haut de la texture, les lignes de l'image vont vers le bas).
    normal = np.stack([-gx * strength, gy * strength, np.ones_like(height)], axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    relief = ((normal * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(relief, "RGB").save(os.path.join(FOLDER, "Horloge_Glace_Relief.png"))

    # La couleur : presque blanche, un voile bleuté, les fêlures et le givre plus blancs, les bulles claires.
    shade = 0.86 + 0.08 * (veil - 0.5) + 0.03 * (grain - 0.5)
    rgb = np.stack([shade * 0.96, shade * 0.99, shade], axis=-1)
    white = np.clip(lines * 0.9 + frost * 0.6 + dots * 0.5, 0.0, 1.0)[..., None]
    rgb = rgb * (1.0 - 0.6 * white) + 0.6 * white
    Image.fromarray((np.clip(rgb, 0.0, 1.0) * 255 + 0.5).astype(np.uint8), "RGB").save(os.path.join(FOLDER, "Horloge_Glace.png"))
    print("Écrit : Horloge_Glace.png, Horloge_Glace_Relief.png (" + FOLDER + ")")


if __name__ == "__main__":
    main()
