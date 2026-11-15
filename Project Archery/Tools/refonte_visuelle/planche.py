# Les images de référence d'un bâtiment pour Blender (14 novembre ; Docs/Guide_Blender.md, Docs/Refonte_Visuelle.md) : les vues
# de face, de droite et de dessus de Blender, à l'échelle exacte (un carreau = 1 m, l'origine du bâtiment marquée), au niveau le
# plus haut ; et une planche des niveaux (en biais et de face, niveau par niveau). Écrit aussi les réglages à taper dans Blender
# pour poser chaque image, et la place des objets demandés (en coordonnées de Blender).
# Depuis « Project Archery/Assets » :
#   py -I ../Tools/refonte_visuelle/planche.py _Project/Prefabs/Structures/Givre.prefab ../Docs/Planches/Givre [objet,objet]
# Comme module (preparer.py) : faire(prefab, dossier) fait les images et rend leurs réglages.
# L'export de Blender vers Unity se fait en « -Z Forward, Y Up » (Guide_Blender.md) : Unity x = -Blender x, Unity y = Blender z,
# Unity z = -Blender y. L'avant d'un bâtiment dans Unity (+Z, la flèche bleue) est donc ce qu'on voit dans la vue de face de Blender.
import os, sys, math
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'allure_tours'))
import numpy as np
from PIL import Image, ImageDraw
import prefab as pf
import rendu

# Les vues de Blender en coordonnées d'Unity : (direction de la caméra vers la scène, haut de l'image, droite de l'image).
VIEWS = {
    'avant': (np.array([0, 0, -1.0]), np.array([0, 1.0, 0]), np.array([-1.0, 0, 0])),   # Front (pavé 1) : caméra en Blender -Y
    'droite': (np.array([1.0, 0, 0]), np.array([0, 1.0, 0]), np.array([0, 0, -1.0])),   # Right (pavé 3) : caméra en Blender +X
    'dessus': (np.array([0, -1.0, 0]), np.array([0, 0, -1.0]), np.array([-1.0, 0, 0])), # Top (pavé 7)
}
TITLES = {'avant': 'vue de face (Front)', 'droite': 'vue de droite (Right)', 'dessus': 'vue de dessus (Top)'}
# Pour poser chaque image dans Blender : sa rotation, et si son centre est à mi-hauteur (vues de côté) ou au sol (vue de dessus).
PLACE = {'avant': ((90, 0, 0), True), 'droite': ((90, 0, 90), True), 'dessus': ((0, 0, 0), False)}
rendu.VIEWS.update(VIEWS)


def unity_to_blender(p):
    return np.array([-p[0], -p[2], p[1]])


def _draw(tris, view, L, ppm, name, level):
    size = L * ppm
    d, up, right = VIEWS[view]
    img = Image.new('RGB', (size, size), (70, 72, 80))
    dr = ImageDraw.Draw(img)
    vmin = 0.0 if view != 'dessus' else -L / 2.0

    def px(p):
        return ((p @ right + L / 2.0) * ppm, size - (p @ up - vmin) * ppm)

    # la grille (1 m), et les axes passant par l'origine du bâtiment aux couleurs de Blender : X rouge, Y vert, Z bleu (de face
    # et de droite, l'axe vertical est Z ; le sol est le bas de l'image)
    for i in range(L + 1):
        c = i * ppm
        dr.line([(c, 0), (c, size)], fill=(92, 94, 104), width=1)
        dr.line([(0, c), (size, c)], fill=(92, 94, 104), width=1)
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
    dr.text((6, 6), '%s, %s, niveau %d ; un carreau = 1 m ; image de %d m' % (name, TITLES[view], level, L), fill=(255, 255, 255))
    return img


def faire(prefab_path, out_dir, assets='.', built=None):
    """Les images d'un prefab dans out_dir. built : (racine, parties) déjà construits par prefab.build. Rend un dict : le côté
    des images (L, en m), leurs fichiers et réglages, la taille du modèle, le nombre de niveaux."""
    name = os.path.splitext(os.path.basename(prefab_path))[0]
    if built is None:
        pf.ASSETS = assets
        pf.GUIDS = pf.guid_map(assets)
        parts = []
        root = pf.build(prefab_path, parts)
    else:
        root, parts = built
    levels = list(range(1, len(parts) + 2))
    scenes = [pf.level_scene(root, parts, L) for L in levels]
    allp = np.vstack([t[0] for t in scenes[-1]])

    # Le côté des images (en mètres, entier) : tout le bâtiment, l'origine au milieu, le sol en bas.
    reach = max(np.abs(allp[:, 0]).max(), np.abs(allp[:, 2]).max())
    height = max(allp[:, 1].max(), 0.5)
    L = max(int(math.ceil(max(2 * reach, height) * 1.1)), 2)
    ppm = int(min(200, 4000 // L))

    os.makedirs(out_dir, exist_ok=True)
    images = {}
    for v in ('avant', 'droite', 'dessus'):
        p = os.path.join(out_dir, '%s_%s.png' % (name, v))
        _draw(scenes[-1], v, L, ppm, name, levels[-1]).save(p)
        rot, mid = PLACE[v]
        images[v] = {'fichier': os.path.abspath(p), 'location': [0, 0, L / 2.0 if mid else 0], 'rotation': list(rot), 'size': L}

    # La planche des niveaux : en biais et de face (Front de Blender), niveau par niveau.
    lo, hi = allp.min(axis=0), allp.max(axis=0)
    lo[1] = min(lo[1], 0)
    sheet = []
    for v in ('iso', 'avant'):
        for Lv, s in zip(levels, scenes):
            sheet.append(rendu.render(s, v, 360, bounds=(lo, hi), grid=True, title='%s niveau %d  %s' % (name, Lv, v)))
    p = os.path.join(out_dir, '%s_niveaux.png' % name)
    rendu.sheet(sheet, len(levels), p)
    return {'L': L, 'images': images, 'niveaux': os.path.abspath(p), 'levels': len(levels), 'root': root, 'parts': parts,
            'scenes': scenes, 'lo': lo, 'hi': hi}


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    prefab_path, out_dir = sys.argv[1], sys.argv[2]
    wanted = [s for s in (sys.argv[3].split(',') if len(sys.argv) > 3 else []) if s]
    name = os.path.splitext(os.path.basename(prefab_path))[0]
    r = faire(prefab_path, out_dir)
    lo, hi, L = r['lo'], r['hi'], r['L']
    print('Images dans %s : %s' % (out_dir, ', '.join(os.path.basename(i['fichier']) for i in r['images'].values())
                                   + ', ' + os.path.basename(r['niveaux'])))
    print('Taille du modèle de Claude : %.2f m de haut, %.2f m de large (Blender X), %.2f m de profondeur (Blender Y) ; %d niveaux.' % (
        hi[1], hi[0] - lo[0], hi[2] - lo[2], r['levels']))
    print()
    print('Dans Blender, chaque image en Add > Image > Background, puis dans ses réglages (Object : Location, Rotation ; Object Data : Size) :')
    for v, i in r['images'].items():
        print('  %-24s Location %s    Rotation %s    Size %d' % (
            os.path.basename(i['fichier']), ', '.join('%g' % x for x in i['location']), ', '.join('%g' % x for x in i['rotation']), L))
    # La place des objets demandés, dans le modèle de Claude (pour information : dans le tien, ils vont où tu les mets).
    if wanted:
        print()
        def walk(n, M):
            W = M @ n.local
            if n.name in wanted:
                b = unity_to_blender(W[:3, 3])
                print('  %-24s Blender X %.2f  Y %.2f  Z %.2f' % (n.name, b[0], b[1], b[2]))
            for c in n.children:
                walk(c, W)
        walk(r['root'], np.eye(4))


if __name__ == '__main__':
    main()
