# Les images de référence d'un bâtiment pour Blender (14 novembre ; Docs/Guide_Blender.md, Docs/Refonte_Visuelle.md) : les vues
# de face, de droite et de dessus de Blender, à l'échelle exacte (un carreau = 1 m, l'origine du bâtiment marquée), au niveau le
# plus haut ; et une planche des niveaux (en biais et de face, niveau par niveau). Écrit aussi les réglages à taper dans Blender
# pour poser chaque image, et la place des objets demandés (en coordonnées de Blender).
# Depuis « Project Archery/Assets » :
#   py -I ../Tools/refonte_visuelle/planche.py _Project/Prefabs/Structures/Givre.prefab ../Docs/Planches/Givre [objet,objet]
# L'export de Blender vers Unity se fait en « -Z Forward, Y Up » (Guide_Blender.md) : Unity x = -Blender x, Unity y = Blender z,
# Unity z = -Blender y. L'avant d'un bâtiment dans Unity (+Z, la flèche bleue) est donc ce qu'on voit dans la vue de face de Blender.
import os, sys, math
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'allure_tours'))
sys.stdout.reconfigure(encoding='utf-8')
import numpy as np
from PIL import Image, ImageDraw
import prefab as pf
import rendu

ASSETS = '.'
prefab_path, out_dir = sys.argv[1], sys.argv[2]
wanted = [s for s in (sys.argv[3].split(',') if len(sys.argv) > 3 else []) if s]
name = os.path.splitext(os.path.basename(prefab_path))[0]

# Les vues de Blender en coordonnées d'Unity : (direction de la caméra vers la scène, haut de l'image, droite de l'image).
VIEWS = {
    'avant': (np.array([0, 0, -1.0]), np.array([0, 1.0, 0]), np.array([-1.0, 0, 0])),   # Front (pavé 1) : caméra en Blender -Y
    'droite': (np.array([1.0, 0, 0]), np.array([0, 1.0, 0]), np.array([0, 0, -1.0])),   # Right (pavé 3) : caméra en Blender +X
    'dessus': (np.array([0, -1.0, 0]), np.array([0, 0, -1.0]), np.array([-1.0, 0, 0])), # Top (pavé 7)
}
TITLES = {'avant': 'vue de face (Front)', 'droite': 'vue de droite (Right)', 'dessus': 'vue de dessus (Top)'}
rendu.VIEWS.update(VIEWS)

pf.ASSETS = ASSETS
pf.GUIDS = pf.guid_map(ASSETS)
parts = []
root = pf.build(prefab_path, parts)
levels = list(range(1, len(parts) + 2))
scenes = [pf.level_scene(root, parts, L) for L in levels]
top = scenes[-1]
allp = np.vstack([t[0] for t in top])

# Le côté des images (en mètres, entier) : tout le bâtiment, l'origine au milieu, le sol en bas.
reach = max(np.abs(allp[:, 0]).max(), np.abs(allp[:, 2]).max())
height = max(allp[:, 1].max(), 0.5)
L = int(math.ceil(max(2 * reach, height) * 1.1))
L = max(L, 2)
ppm = int(min(200, 4000 // L))
size = L * ppm

def draw(tris, view):
    d, up, right = VIEWS[view]
    img = Image.new('RGB', (size, size), (70, 72, 80))
    dr = ImageDraw.Draw(img)
    vmin = 0.0 if view != 'dessus' else -L / 2.0

    def px(p):
        return ((p @ right + L / 2.0) * ppm, size - (p @ up - vmin) * ppm)

    # la grille (1 m) et les axes passant par l'origine du bâtiment
    for i in range(L + 1):
        c = i * ppm
        dr.line([(c, 0), (c, size)], fill=(92, 94, 104), width=1)
        dr.line([(0, c), (size, c)], fill=(92, 94, 104), width=1)
    # aux couleurs des axes de Blender : X rouge, Y vert, Z bleu (de face et de droite, l'axe vertical est Z ; le sol est le bas
    # de l'image)
    o = px(np.zeros(3))
    if view == 'dessus':
        dr.line([(o[0], 0), (o[0], size)], fill=(80, 160, 60), width=2)
        dr.line([(0, o[1]), (size, o[1])], fill=(190, 60, 60), width=2)
    else:
        dr.line([(o[0], 0), (o[0], size)], fill=(70, 110, 210), width=2)
    polys = []
    for P, T, C in tris:
        A, B, Cc = P[T[:, 0]], P[T[:, 1]], P[T[:, 2]]
        N = np.cross(B - A, Cc - A)
        ln = np.linalg.norm(N, axis=1)
        ok = ln > 1e-12
        N[ok] /= ln[ok][:, None]
        depth = ((A + B + Cc) / 3.0) @ d
        shade = 0.45 + 0.55 * np.clip(np.abs(N @ rendu.LIGHT), 0, 1)
        for i in range(len(T)):
            if ok[i]:
                col = tuple(int(255 * min(1.0, c * shade[i])) for c in C[i][:3])
                polys.append((depth[i], [px(A[i]), px(B[i]), px(Cc[i])], col))
    polys.sort(key=lambda t: -t[0])
    for _, pts, col in polys:
        dr.polygon(pts, fill=col)
    dr.text((6, 6), '%s, %s, niveau %d ; un carreau = 1 m ; image de %d m' % (name, TITLES[view], levels[-1], L), fill=(255, 255, 255))
    return img

os.makedirs(out_dir, exist_ok=True)
files = []
for v in ('avant', 'droite', 'dessus'):
    p = os.path.join(out_dir, '%s_%s.png' % (name, v))
    draw(top, v).save(p)
    files.append(os.path.basename(p))

# La planche des niveaux : en biais et de face (Front de Blender), niveau par niveau.
lo, hi = allp.min(axis=0), allp.max(axis=0)
lo[1] = min(lo[1], 0)
images = []
for v in ('iso', 'avant'):
    for Lv, s in zip(levels, scenes):
        images.append(rendu.render(s, v, 360, bounds=(lo, hi), grid=True, title='%s niveau %d  %s' % (name, Lv, v)))
p = os.path.join(out_dir, '%s_niveaux.png' % name)
rendu.sheet(images, len(levels), p)
files.append(os.path.basename(p))

print('Images dans %s : %s' % (out_dir, ', '.join(files)))
print('Taille du modèle de Claude : %.2f m de haut, %.2f m de large (Blender X), %.2f m de profondeur (Blender Y) ; %d niveaux.' % (
    hi[1], hi[0] - lo[0], hi[2] - lo[2], len(levels)))
print()
print('Dans Blender, chaque image en Add > Image > Background, puis dans ses réglages (Object : Location, Rotation ; Object Data : Size) :')
print('  %-24s Location 0, 0, %g    Rotation 90, 0, 0     Size %d' % (name + '_avant.png', L / 2.0, L))
print('  %-24s Location 0, 0, %g    Rotation 90, 0, 90    Size %d' % (name + '_droite.png', L / 2.0, L))
print('  %-24s Location 0, 0, 0      Rotation 0, 0, 0      Size %d' % (name + '_dessus.png', L))

# La place des objets demandés, dans le modèle de Claude (pour information : dans le tien, ils vont où tu les mets).
if wanted:
    print()
    def walk(n, M):
        W = M @ n.local
        if n.name in wanted:
            u = W[:3, 3]
            print('  %-24s Blender X %.2f  Y %.2f  Z %.2f' % (n.name, -u[0], -u[2], u[1]))
        for c in n.children:
            walk(c, W)
    walk(root, np.eye(4))
