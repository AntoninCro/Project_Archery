# Vues et dimensions de modèles FBX (orientation, taille) :
#   py -I ../Tools/allure_tours/modeles.py sortie.png iso,face,dessus 200 kenney_castle-kit/Models/FBX\ format/wall.fbx
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from lecture_fbx import load, bounds
from rendu import render, sheet

out, views, size = sys.argv[1], sys.argv[2].split(','), int(sys.argv[3])
images = []
for f in sys.argv[4:]:
    meshes = load(f)
    if not meshes:
        print('vide :', f)
        continue
    lo, hi = bounds(meshes)
    name = os.path.splitext(os.path.basename(f))[0]
    print('%-34s taille x %.2f  y %.2f  z %.2f   min (%.2f, %.2f, %.2f)  max (%.2f, %.2f, %.2f)  parts: %s' % (
        name, *(hi - lo), *lo, *hi, ', '.join(sorted(set(m[3] for m in meshes)))[:120]))
    for v in views:
        images.append(render([(m[0], m[1], m[2]) for m in meshes], v, size, grid=True, title='%s  %s' % (name, v)))
print(sheet(images, len(views), out))
