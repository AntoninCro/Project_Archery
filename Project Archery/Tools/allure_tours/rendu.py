# Rendu logiciel très simple (peintre, ombrage plat) de triangles colorés, en vues orthographiques.
import numpy as np
from PIL import Image, ImageDraw

LIGHT = np.array([0.45, 0.8, -0.4])
LIGHT = LIGHT / np.linalg.norm(LIGHT)

VIEWS = {
    # nom : (axe de la caméra vers la scène, vecteur haut de l'image, vecteur droite de l'image)
    'face': (np.array([0, 0, 1.0]), np.array([0, 1.0, 0]), np.array([1.0, 0, 0])),      # caméra en -Z, regarde vers +Z
    'dos': (np.array([0, 0, -1.0]), np.array([0, 1.0, 0]), np.array([-1.0, 0, 0])),
    'cote': (np.array([-1.0, 0, 0]), np.array([0, 1.0, 0]), np.array([0, 0, 1.0])),     # caméra en +X, regarde vers -X ; +Z à droite
    'dessus': (np.array([0, -1.0, 0]), np.array([0, 0, 1.0]), np.array([1.0, 0, 0])),   # +Z en haut de l'image
    'iso': (None, None, None),
}

def _iso():
    # caméra devant-gauche-haut (en -Z, -X, +Y), regarde vers le centre : on voit l'avant (-Z) et la gauche (-X)
    d = np.array([0.6, -0.55, 1.0]); d /= np.linalg.norm(d)
    right = np.cross(np.array([0, 1.0, 0]), d); right /= np.linalg.norm(right)
    up = np.cross(d, right)
    return d, up, right

def render(tris_list, view='iso', size=360, bounds=None, bg=(70, 72, 80), grid=None, title=None):
    """tris_list : liste de (P Nx3, T Mx3, C Mx3). Renvoie une image PIL."""
    if view == 'iso':
        d, up, right = _iso()
    else:
        d, up, right = VIEWS[view]
    allP = []
    for P, T, C in tris_list:
        allP.append(P)
    allP = np.vstack(allP) if allP else np.zeros((1, 3))
    lo, hi = (allP.min(axis=0), allP.max(axis=0)) if bounds is None else bounds
    corners = np.array([[x, y, z] for x in (lo[0], hi[0]) for y in (lo[1], hi[1]) for z in (lo[2], hi[2])])
    sx = corners @ right
    sy = corners @ up
    span = max(sx.max() - sx.min(), sy.max() - sy.min(), 1e-3) * 1.08
    cx = (sx.max() + sx.min()) / 2
    cy = (sy.max() + sy.min()) / 2
    scale = size / span
    img = Image.new('RGB', (size, size), bg)
    dr = ImageDraw.Draw(img)
    polys = []
    for P, T, C in tris_list:
        A, B, Cc = P[T[:, 0]], P[T[:, 1]], P[T[:, 2]]
        N = np.cross(B - A, Cc - A)
        ln = np.linalg.norm(N, axis=1)
        ok = ln > 1e-12
        N[ok] /= ln[ok][:, None]
        # Unity : sens horaire = face avant ; avec X inversé à l'import, la normale (B-A)x(C-A) pointe vers l'extérieur
        facing = N @ d
        depth = ((A + B + Cc) / 3.0) @ d
        shade = 0.45 + 0.55 * np.clip(np.abs(N @ LIGHT), 0, 1)
        for i in range(len(T)):
            if not ok[i]:
                continue
            pts = [((p @ right - cx) * scale + size / 2, size / 2 - (p @ up - cy) * scale) for p in (A[i], B[i], Cc[i])]
            col = tuple(int(255 * min(1.0, c * shade[i])) for c in C[i][:3])
            polys.append((depth[i], pts, col))
    polys.sort(key=lambda t: -t[0])
    for _, pts, col in polys:
        dr.polygon(pts, fill=col)
    if grid:
        # repère : un trait de 1 m en bas à gauche
        dr.line([(8, size - 10), (8 + scale, size - 10)], fill=(255, 255, 0), width=2)
        dr.text((10, size - 24), '1 m', fill=(255, 255, 0))
    if title:
        dr.text((4, 4), title, fill=(255, 255, 255))
    return img

def sheet(images, cols, out):
    w, h = images[0].size
    rows = (len(images) + cols - 1) // cols
    s = Image.new('RGB', (cols * w, rows * h), (30, 30, 30))
    for i, im in enumerate(images):
        s.paste(im, ((i % cols) * w, (i // cols) * h))
    s.save(out)
    return s.size
