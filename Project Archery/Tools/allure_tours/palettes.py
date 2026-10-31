# Écrit les textures des palettes des tours (Art/TowerDefense/Palettes) d'après le fichier d'allure. Depuis
# « Project Archery/Assets » : py -I ../Tools/allure_tours/palettes.py . _Project/Editor/TowerDefenseLook.json
import os, sys, json, uuid
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
import prefab as pr
import scene_tour as ts
import palette as pal_mod

pr.ASSETS = sys.argv[1]
pr.GUIDS = pr.guid_map(pr.ASSETS)
look = json.load(open(sys.argv[2], encoding='utf-8'))
pals = look['palettes']
out_dir = os.path.join(pr.ASSETS, '_Project/Art/TowerDefense/Palettes')
os.makedirs(out_dir, exist_ok=True)

needed = set()
def walk(n, tour):
    for (V, T, C, meta) in n.meshes:
        if meta.get('kit'):
            needed.add((meta.get('palette', tour), meta['kit']))
    for c in n.children:
        walk(c, tour)

for tour in dict.fromkeys(t['prefab'] for t in look['towers']):
    parts = []
    root = ts.from_pr(pr.build(os.path.join(pr.ASSETS, '_Project/Prefabs/Structures', tour + '.prefab'), parts))
    ts.apply_passes(root, look, tour, pals)
    walk(root, tour)

# Les pièces des duos (31 octobre) : celles qu'ajoutent leurs opérations, à la palette du duo.
for d in look.get('duos', []):
    root, halves = ts.build_duo(look, d['name'], pals, lambda name: np.array([0.5, 0.5, 0.5]))
    def walk_new(n):
        for (V, T, C, meta) in n.meshes:
            if meta.get('kit') and not meta.get('fixed'):
                needed.add((meta.get('palette', d.get('palette', 'Duo')), meta['kit']))
        for c in n.children:
            walk_new(c)
    walk_new(root)

meta_src = open(os.path.join(pr.ASSETS, ts.KIT_TEX['td'] + '.meta'), 'rb').read().decode('utf-8')
for palette, kit in sorted(needed):
    name = '%s_%s' % (palette, ts.KIT_NAMES[kit])
    img = np.asarray(Image.open(os.path.join(pr.ASSETS, ts.KIT_TEX[kit])).convert('RGB'), dtype=np.float64) / 255.0
    rec = pal_mod.recolor(img, pals[palette])
    png = os.path.join(out_dir, name + '.png')
    Image.fromarray(np.clip(rec * 255 + 0.5, 0, 255).astype(np.uint8)).save(png, optimize=True)
    meta = png + '.meta'
    if not os.path.exists(meta):
        guid = uuid.uuid4().hex
        lines = meta_src.split('\r\n')
        lines = ['guid: ' + guid if l.startswith('guid:') else l for l in lines]
        open(meta, 'wb').write('\r\n'.join(lines).encode('utf-8'))
    print(name, os.path.getsize(png), 'octets')
