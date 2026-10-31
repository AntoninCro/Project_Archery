# Cherche, tour par tour et niveau par niveau, les faces de deux pièces dans le même plan et le même sens (elles
# scintillent en jeu). Depuis « Project Archery/Assets » :
#   py -I ../Tools/allure_tours/faces_superposees.py . _Project/Editor/TowerDefenseLook.json Barricade,Pieges
# Un duo (31 octobre) : « Duo:Boulet fou », niveaux 1 à 3.
import os, sys, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
import prefab as pr
import scene_tour as ts

pr.ASSETS = sys.argv[1]
pr.GUIDS = pr.guid_map(pr.ASSETS)
look = json.load(open(sys.argv[2], encoding='utf-8'))
tours = sys.argv[3].split(',')
levels = ts.parse_levels(sys.argv[4] if len(sys.argv) > 4 else '1,2,3,4,5')
before = len(sys.argv) > 5 and sys.argv[5] == 'avant'

def gather(n, M, hidden, out, path):
    if not n.active or (n.gos & hidden):
        return
    W = M @ n.local()
    here = path + '/' + n.name if path else n.name
    for k, (V, T, C, meta) in enumerate(n.meshes):
        P = (np.c_[V, np.ones(len(V))] @ W.T)[:, :3]
        # Les cubes (formes simples) servent aussi à savoir si une face est cachée dedans.
        cube = np.linalg.inv(W) if (C is not None and len(V) == 8 and len(T) == 12) else None
        # Les maillages des FBX (X inversé à l'import) ont leurs triangles dans l'autre sens que les formes simples.
        if 'uv' in meta:
            T = T[:, ::-1]
        out.append((here + ('#%d' % k if len(n.meshes) > 1 else ''), P, T, C, cube))
    for c in n.children:
        gather(c, W, hidden, out, here)

def inside(p, a, b, c, nrm):
    # p dans le triangle abc (même plan), marge 1 mm vers l'intérieur
    for u, v in ((a, b), (b, c), (c, a)):
        if np.dot(np.cross(v - u, p - u), nrm) < 1e-6:
            return False
    return True

CUBES = []
EXAMPLE = {}

def hidden_in_cube(p, a, b):
    for ri, inv in CUBES:
        if ri in (a, b):
            continue
        q = inv @ np.array([p[0], p[1], p[2], 1.0])
        if abs(q[0]) < 0.5 and abs(q[1]) < 0.5 and abs(q[2]) < 0.5:
            return True
    return False

def coplanar(items, tol_d=0.003, tol_n=0.9995, min_area=0.0004):
    tris = []
    for ri, (name, P, T, COL, cube) in enumerate(items):
        A, B, C = P[T[:, 0]], P[T[:, 1]], P[T[:, 2]]
        N = np.cross(B - A, C - A)
        ar = np.linalg.norm(N, axis=1)
        ok = ar > 2 * min_area
        for i in np.nonzero(ok)[0]:
            n = N[i] / ar[i]
            cen = (A[i] + B[i] + C[i]) / 3
            if n[1] < -0.9 and cen[1] < 0.02:
                continue  # le dessous, posé au sol : jamais vu
            col = COL[i] if COL is not None else np.zeros(3)
            tris.append((ri, A[i], B[i], C[i], n, float(np.dot(n, A[i])), ar[i] / 2, col))
    # seau par normale arrondie et distance
    buckets = {}
    for t in tris:
        key = (tuple(np.round(t[4] * 20).astype(int)), int(round(t[5] / 0.01)))
        buckets.setdefault(key, []).append(t)
    found = {}
    for key, group in buckets.items():
        near = []
        for dk in (-1, 0, 1):
            near += buckets.get((key[0], key[1] + dk), [])
        for t in group:
            for u in near:
                if u[0] <= t[0]:
                    continue
                if np.dot(t[4], u[4]) < tol_n or abs(t[5] - u[5]) > tol_d:
                    continue
                if np.abs(t[7] - u[7]).max() < 0.004:
                    continue  # même couleur : rien ne se voit
                pts_t = [t[1], t[2], t[3], (t[1] + t[2] + t[3]) / 3]
                pts_u = [u[1], u[2], u[3], (u[1] + u[2] + u[3]) / 3]
                cen_t = pts_t[3]; cen_u = pts_u[3]
                hit = inside(cen_t, u[1], u[2], u[3], u[4]) or inside(cen_u, t[1], t[2], t[3], t[4])
                if not hit:
                    for p in pts_t[:3]:
                        q = p + (cen_t - p) * 0.02
                        if inside(q, u[1], u[2], u[3], u[4]):
                            hit = True
                            break
                if hit and not hidden_in_cube((cen_t + cen_u) / 2 + t[4] * 0.004, t[0], u[0]):
                    k = (t[0], u[0])
                    found[k] = found.get(k, 0) + min(t[6], u[6])
                    if k not in EXAMPLE:
                        EXAMPLE[k] = (np.round(t[4], 2), round(t[5], 3), np.round(cen_t, 2), np.round(cen_u, 2))
    return found

def aabb_overlaps(items, groups):
    out = []
    boxes = [(name, P.min(axis=0), P.max(axis=0)) for (name, P, T) in items]
    for i in range(len(boxes)):
        for j in range(i + 1, len(boxes)):
            if groups[i] == groups[j]:
                continue
            lo = np.maximum(boxes[i][1], boxes[j][1]); hi = np.minimum(boxes[i][2], boxes[j][2])
            d = hi - lo
            if (d > 0.02).all():
                v = d.prod()
                vi = np.prod(np.maximum(boxes[i][2] - boxes[i][1], 0.02)); vj = np.prod(np.maximum(boxes[j][2] - boxes[j][1], 0.02))
                share = v / min(vi, vj)
                if share > 0.25:
                    out.append((share, boxes[i][0], boxes[j][0]))
    return sorted(out, reverse=True)

def iter_nodes(n):
    yield n
    for c in n.children:
        yield from iter_nodes(c)

def group_of(name, parts_names):
    for p in parts_names:
        if ('/' + p + '/') in ('/' + name + '/'):
            return p
    return 'base'

pals = look['palettes']
for tour in tours:
    matcol = lambda name: np.array([0.5, 0.5, 0.5]) + (hash(name) % 97) / 400.0
    if before and not tour.startswith('Duo:'):
        root, parts = ts.build_tower(tour, look, pals, matcol, passes=False)
        halves = [(root, tour, parts)]
        for n in iter_nodes(root):
            n.meshes = [(V, T, C if C is not None else np.zeros((len(T), 3)), m) for (V, T, C, m) in n.meshes]
    else:
        root, halves = ts.build_any(tour, look, pals, matcol)
    reported = set()
    for L in levels:
        items = []
        ts.halves_scene(root, halves, L, lambda r, hidden: gather(r, np.eye(4), hidden, items, ''))
        CUBES[:] = [(i, it[4]) for i, it in enumerate(items) if it[4] is not None]
        EXAMPLE.clear()
        found = coplanar(items)
        for (i, j), area in sorted(found.items(), key=lambda t: -t[1]):
            key = ('c', items[i][0], items[j][0])
            if key in reported or area < 0.002:
                continue
            reported.add(key)
            print('%-9s niv %s  FACES SUPERPOSÉES %.3f m²  %s  <>  %s' % (tour, '-'.join(map(str, L)), area, items[i][0], items[j][0]))
            if os.environ.get('DETAIL'):
                print('            normale %s  plan %s  centres %s %s' % EXAMPLE.get((i, j), ('?', '?', '?', '?')))
    if not reported:
        print('%-9s aucune face superposée' % tour)
