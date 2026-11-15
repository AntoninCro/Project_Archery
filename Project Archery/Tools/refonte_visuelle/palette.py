# La palette humaine des couleurs du jeu (14 novembre ; Docs/Refonte_Visuelle.md, « Les couleurs ») : les couleurs d'un kit de
# Kenney (CC0, faites par un humain), lues dans sa texture de couleurs, numérotées sur une planche, pour que le joueur choisisse
# une couleur par rôle (Pierre, Bois, Glace…). Puis, avec ses choix, les matériaux de la palette dans la bibliothèque de Blender
# (Modeles_Blender/Bibliotheque/Palette.blend, catalogue « Palette ») : on glisse un matériau sur une pièce.
# Depuis « Project Archery » :
#   py -I Tools/refonte_visuelle/palette.py planche [td]           la planche Docs/Planches/Palette_<kit>.png
#   py -I Tools/refonte_visuelle/palette.py matieres [td]          Palette.blend, d'après Modeles_Blender/Palette.txt
# Palette.txt : une ligne par rôle, « Nom = numéro » (« Transparent glace = 4 », « Lueur feu = 22 » ; un nom qui commence par
# Transparent est transparent, un nom qui contient Lueur luit : les règles du branchement dans Unity).
import os, sys, json, re, subprocess, glob
sys.stdout.reconfigure(encoding='utf-8')
import numpy as np
from PIL import Image, ImageDraw

PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
KITS = {
    'td': ('Kenney Tower Defense Kit', 'Assets/kenney_tower-defense-kit/Models/FBX format/Textures/colormap.png'),
    'chateau': ('Kenney Castle Kit', 'Assets/kenney_castle-kit/Models/FBX format/Textures/colormap.png'),
}


def swatches(kit):
    """Les couleurs du kit : par colonne de 32 px et bande de 128 px, la couleur du haut et celle du bas (une seule si la
    bande est unie)."""
    im = np.asarray(Image.open(os.path.join(PROJECT, KITS[kit][1])).convert('RGB')).astype(int)
    h, w, _ = im.shape
    out = []
    for y0 in range(0, h, 128):
        for x0 in range(0, w, 32):
            top = im[y0 + 6, x0 + 16]
            bottom = im[y0 + 121, x0 + 16]
            if top.max() < 8 and bottom.max() < 8:
                continue          # une zone vide (noire) de la texture
            for c in ([top] if np.abs(top - bottom).max() < 6 else [top, bottom]):
                c = tuple(int(v) for v in c)
                if c not in out:
                    out.append(c)
    return out


def planche(kit):
    cols = swatches(kit)
    per_row, s = 8, 120
    rows = (len(cols) + per_row - 1) // per_row
    img = Image.new('RGB', (per_row * s, rows * s + 40), (40, 42, 48))
    dr = ImageDraw.Draw(img)
    try:   # une police avec les accents (celle de PIL n'en a pas)
        from PIL import ImageFont
        font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 15)
    except Exception:
        font = None
    dr.text((10, 12), '%s : %d couleurs (CC0, Kenney). Choisis un numéro par rôle.' % (KITS[kit][0], len(cols)),
            fill=(255, 255, 255), font=font)
    for i, c in enumerate(cols):
        x, y = (i % per_row) * s, 40 + (i // per_row) * s
        dr.rectangle([x + 6, y + 6, x + s - 6, y + s - 6], fill=c)
        light = sum(c) > 380
        ink = (20, 20, 20) if light else (240, 240, 240)
        dr.text((x + 14, y + 12), str(i + 1), fill=ink, font=font)
        dr.text((x + 14, y + s - 26), '#%02x%02x%02x' % c, fill=ink, font=font)
    out = os.path.join(PROJECT, 'Docs', 'Planches', 'Palette_%s.png' % kit)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    img.save(out)
    json.dump({'kit': KITS[kit][0], 'couleurs': ['#%02x%02x%02x' % c for c in cols]},
              open(out[:-4] + '.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('Planche : %s (%d couleurs)' % (out, len(cols)))


def matieres(kit):
    cols = json.load(open(os.path.join(PROJECT, 'Docs', 'Planches', 'Palette_%s.json' % kit), encoding='utf-8'))['couleurs']
    roles = []
    for line in open(os.path.join(PROJECT, 'Modeles_Blender', 'Palette.txt'), encoding='utf-8'):
        line = line.split('#')[0].strip()
        m = re.match(r'(.+?)\s*=\s*(\d+)(?:\s*,\s*([\d.]+))?$', line)
        if not m:
            continue
        n = int(m.group(2))
        if not 1 <= n <= len(cols):
            print('« %s » : pas de couleur %d (1 à %d)' % (m.group(1), n, len(cols)))
            continue
        roles.append({'nom': m.group(1), 'couleur': cols[n - 1], 'alpha': float(m.group(3)) if m.group(3) else None})
    data = os.path.join(PROJECT, 'Modeles_Blender', 'Bibliotheque', 'palette_roles.json')
    json.dump({'kit': kit, 'roles': roles}, open(data, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    found = glob.glob('C:/Program Files/Blender Foundation/Blender */blender.exe')
    exe = os.environ.get('THUNK_BLENDER') or (sorted(found)[-1] if found else None)
    r = subprocess.run([exe, '--background', '--factory-startup', '--python',
                        os.path.join(PROJECT, 'Tools', 'blender', 'palette_blender.py'), '--', data],
                       capture_output=True, text=True, encoding='utf-8', errors='replace')
    for l in (r.stdout + r.stderr).splitlines():
        if l.startswith('THUNK') or 'Error' in l:
            print(l)


if __name__ == '__main__':
    what = sys.argv[1]
    kit = sys.argv[2] if len(sys.argv) > 2 else 'td'
    planche(kit) if what == 'planche' else matieres(kit)
