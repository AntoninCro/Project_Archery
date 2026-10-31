# Dessine des tours du tower defense niveau par niveau, hors de Unity, telles que les construit
# Editor/TowerDefenseLookBaker.cs (même fichier d'allure). Depuis « Project Archery/Assets » :
#   py -I ../Tools/allure_tours/apercu.py . _Project/Editor/TowerDefenseLook.json Baliste,Mortier sortie.png iso,face 240
# (vues : iso, face (côté joueur, -Z), dos, cote, dessus ; « avant » en 8e argument : le prefab sans le fichier d'allure).
# Un duo (31 octobre) : « Duo:Boulet fou », niveaux 1 à 3.
import os, sys, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
import prefab as pr
import scene_tour as ts
from rendu import render, sheet
from PIL import Image

pr.ASSETS = sys.argv[1]
pr.GUIDS = pr.guid_map(pr.ASSETS)
look = json.load(open(sys.argv[2], encoding='utf-8'))
tours = sys.argv[3].split(',')
out = sys.argv[4]
views = (sys.argv[5] if len(sys.argv) > 5 else 'iso,face').split(',')
size = int(sys.argv[6]) if len(sys.argv) > 6 else 240
levels = ts.parse_levels(sys.argv[7] if len(sys.argv) > 7 else '1,2,3,4,5')
before = len(sys.argv) > 8 and sys.argv[8] == 'avant'

pals = {k: v for k, v in look['palettes'].items()}

def matcol(name):
    for g, p in pr.GUIDS.items():
        if p.endswith('/' + name + '.mat'):
            return pr.material(g)[0]
    return np.array([1.0, 0.0, 1.0])

rows = []
for tour in tours:
    if before and not tour.startswith('Duo:'):
        root, parts = ts.build_tower(tour, look, pals, matcol, passes=False)
        halves = [(root, tour, parts)]
    else:
        root, halves = ts.build_any(tour, look, pals, matcol)
    scenes = [ts.halves_scene(root, halves, L) for L in levels]
    allp = np.vstack([t[0] for s in scenes for t in s])
    lo, hi = allp.min(axis=0), allp.max(axis=0)
    lo[1] = min(lo[1], 0)
    print('%-10s taille %s  min %s  max %s' % (tour, np.round(hi - lo, 2), np.round(lo, 2), np.round(hi, 2)))
    for v in views:
        for L, s in zip(levels, scenes):
            rows.append(render(s, v, size, bounds=(lo, hi), grid=True, title='%s %s %s' % (tour, '-'.join(map(str, L)), v)))
print(sheet(rows, len(levels), out))
