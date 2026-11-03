"""Les textures du fer martelé du Colosse de forge (l'ultime) : Assets/_Project/Art/TowerDefense/Colosse_Fer.png (la couleur) et
Colosse_Fer_Relief.png (le relief, une carte de normales).

Demandées par le joueur le 2 novembre au soir, après son essai du Colosse (« plus texturé, plus lumineux ») : le géant était fait
de formes simples aux couleurs unies. Un fer battu au marteau : des creux ronds qui se chevauchent, un grain fin, des rayures
claires (le métal à nu), de la suie dans les creux et quelques taches de rouille. La couleur est presque blanche : le matériau la
teinte (fer sombre, bronze, or). Les deux textures se raccordent sur leurs bords (bruits faits par transformée de Fourier, creux
et rayures repliés sur le carré). Le baker (Editor/ColosseFinitionBaker) les met sur les matériaux du Colosse.

Usage (depuis Project Archery/) : py Tools/texture_fer_martele.py
"""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDER = os.path.join(PROJECT, "Assets", "_Project", "Art", "TowerDefense")
SIZE = 512
RNG = np.random.default_rng(1102)


def smooth_noise(scale):
    # Un bruit qui se raccorde : du blanc filtré dans les fréquences (scale : la taille des taches, en part du carré).
    white = RNG.normal(size=(SIZE, SIZE))
    fx = np.fft.fftfreq(SIZE)[None, :]
    fy = np.fft.fftfreq(SIZE)[:, None]
    falloff = np.exp(-(fx ** 2 + fy ** 2) * (scale * SIZE) ** 2)
    noise = np.real(np.fft.ifft2(np.fft.fft2(white) * falloff))
    return (noise - noise.min()) / (noise.max() - noise.min())


def dents(count):
    # Les coups de marteau : des creux ronds, repliés sur le carré (ils passent d'un bord à l'autre).
    grid = (np.arange(SIZE) + 0.5) / SIZE
    x, y = np.meshgrid(grid, grid)
    depth = np.zeros((SIZE, SIZE))
    for _ in range(count):
        cx, cy = RNG.random(2)
        radius = RNG.uniform(0.025, 0.075)
        dx = (x - cx + 0.5) % 1.0 - 0.5
        dy = (y - cy + 0.5) % 1.0 - 0.5
        d = np.sqrt(dx * dx + dy * dy) / radius
        depth += np.where(d < 1.0, (1.0 - d * d) ** 3 * RNG.uniform(0.3, 0.6), 0.0)
    return depth


def scratches(count):
    # Des rayures fines, tracées trois fois de chaque côté pour se raccorder, puis un peu floutées.
    image = Image.new("L", (SIZE * 3, SIZE * 3), 0)
    draw = ImageDraw.Draw(image)
    for _ in range(count):
        x, y = RNG.random(2) * SIZE
        angle = RNG.uniform(0, np.pi)
        length = RNG.uniform(0.05, 0.25) * SIZE
        x2, y2 = x + np.cos(angle) * length, y + np.sin(angle) * length
        value = int(RNG.uniform(120, 255))
        for ox in range(3):
            for oy in range(3):
                draw.line([(x + ox * SIZE, y + oy * SIZE), (x2 + ox * SIZE, y2 + oy * SIZE)], fill=value, width=1)
    image = image.filter(ImageFilter.GaussianBlur(0.6))
    return np.asarray(image, dtype=np.float64)[SIZE:2 * SIZE, SIZE:2 * SIZE] / 255.0


def main():
    hollow = dents(170)
    grain = smooth_noise(0.004)
    blotch = smooth_noise(0.06)
    rust = smooth_noise(0.035)
    lines = scratches(40)

    # Le relief : les creux, le grain, les rayures un peu creusées ; doux (un fer battu, pas des bosses).
    height = -hollow * 1.0 + grain * 0.12 - lines * 0.12
    gx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
    gy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5
    strength = 9.0
    # Unity lit les normales à la manière d'OpenGL (vert vers le haut de la texture, les lignes de l'image vont vers le bas).
    normal = np.stack([-gx * strength, gy * strength, np.ones_like(height)], axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    relief =((normal * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(relief, "RGB").save(os.path.join(FOLDER, "Colosse_Fer_Relief.png"))

    # La couleur : presque blanche, la suie dans les creux et par taches, la rouille, le métal à nu des rayures.
    shade = 0.84 + 0.12 * (grain - 0.5) - 0.2 * np.clip(hollow, 0.0, 1.0)
    shade *= 1.0 - 0.28 * np.clip((blotch - 0.55) * 4.0, 0.0, 1.0)
    shade = np.clip(shade + 0.14 * lines, 0.0, 1.0)
    rgb = np.stack([shade, shade, shade], axis=-1)
    rusty = np.clip((rust - 0.68) * 5.0, 0.0, 1.0)[..., None] * (1.0 - lines[..., None])
    rgb = rgb * (1.0 - 0.55 * rusty) + np.array([0.78, 0.42, 0.24]) * shade[..., None] * 0.55 * rusty
    Image.fromarray((np.clip(rgb, 0.0, 1.0) * 255 + 0.5).astype(np.uint8), "RGB").save(os.path.join(FOLDER, "Colosse_Fer.png"))
    print("Écrit : Colosse_Fer.png, Colosse_Fer_Relief.png (" + FOLDER + ")")


if __name__ == "__main__":
    main()
