# Une tour du tower defense hors de Unity : son prefab (YAML), les opérations du fichier d'allure (TowerDefenseLook.json),
# les palettes, et le rendu niveau par niveau. Même sens que Editor/TowerDefenseLookBaker.cs.
import os, re, sys, json, math, pickle
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from PIL import Image
from lecture_fbx import load
from rendu import render, sheet
import palette as pal_mod
import prefab as pr

KITS = {'td': 'kenney_tower-defense-kit/Models/FBX format', 'castle': 'kenney_castle-kit/Models/FBX format',
        'town': 'kenney_fantasy-town-kit/Models/FBX format'}
KIT_TEX = {k: v + '/Textures/colormap.png' for k, v in KITS.items()}
KIT_NAMES = {'td': 'TD', 'castle': 'Chateau', 'town': 'Village'}

# Matériaux partagés des formes simples -> rôle de la palette (et nuance)
ROLE_OF_MAT = {'Bois': ('bois', 1.0), 'Barricade_Wood': ('bois', 0.9), 'Bois_sombre': ('bois', 0.68), 'Fonte': ('metal', 0.8),
               'Metal': ('metal', 1.0), 'Noir_mat': ('metal', 0.55), 'Pierre': ('pierre', 1.0), 'Fanion': ('accent', 1.0),
               'Aimant': ('accent', 1.0), 'Corde': ('corde', 1.0), 'Cuivre': ('cuivre', 1.0)}

def quat_euler(e):
    """Quaternion.Euler(x, y, z) de Unity (Z, puis X, puis Y), en (x, y, z, w)."""
    x, y, z = [math.radians(a) / 2 for a in e]
    cx, sx, cy, sy, cz, sz = math.cos(x), math.sin(x), math.cos(y), math.sin(y), math.cos(z), math.sin(z)
    # q = qy * qx * qz
    qx = np.array([sx, 0, 0, cx]); qy = np.array([0, sy, 0, cy]); qz = np.array([0, 0, sz, cz])
    return qmul(qmul(qy, qx), qz)

def qmul(a, b):
    ax, ay, az, aw = a; bx, by, bz, bw = b
    return np.array([aw * bx + ax * bw + ay * bz - az * by, aw * by - ax * bz + ay * bw + az * bx,
                     aw * bz + ax * by - ay * bx + az * bw, aw * bw - ax * bx - ay * by - az * bz])

class Node:
    def __init__(self, name):
        self.name = name
        self.pos = np.zeros(3); self.rot = np.array([0, 0, 0, 1.0]); self.scale = np.ones(3)
        self.children = []
        self.meshes = []      # (V, T, C, meta)
        self.active = True
        self.gos = set()
        self.parent = None

    def local(self):
        return pr.trs(self.pos, self.rot, self.scale)

    def add(self, c):
        c.parent = self
        self.children.append(c)
        return c

def from_pr(n, parent=None):
    """Copie d'un nœud lu par prefab.py, en gardant sa place (décomposée) et ses maillages."""
    m = Node(n.name)
    M = n.local
    s = np.linalg.norm(M[:3, :3], axis=0)
    R = M[:3, :3] / np.where(s > 1e-9, s, 1)
    m.pos = M[:3, 3].copy(); m.scale = s; m.rot = mat_quat(R)
    m.active = n.active
    m.gos = set([n.go]) if n.go else set()
    m.gos |= getattr(n, 'gos', set())
    m.meshes = list(n.meshes)
    m.parent = parent
    for c in n.children:
        m.children.append(from_pr(c, m))
    return m

def mat_quat(R):
    t = np.trace(R)
    if t > 0:
        s = math.sqrt(t + 1.0) * 2
        return np.array([(R[2, 1] - R[1, 2]) / s, (R[0, 2] - R[2, 0]) / s, (R[1, 0] - R[0, 1]) / s, 0.25 * s])
    i = int(np.argmax([R[0, 0], R[1, 1], R[2, 2]]))
    if i == 0:
        s = math.sqrt(1.0 + R[0, 0] - R[1, 1] - R[2, 2]) * 2
        return np.array([0.25 * s, (R[0, 1] + R[1, 0]) / s, (R[0, 2] + R[2, 0]) / s, (R[2, 1] - R[1, 2]) / s])
    if i == 1:
        s = math.sqrt(1.0 + R[1, 1] - R[0, 0] - R[2, 2]) * 2
        return np.array([(R[0, 1] + R[1, 0]) / s, 0.25 * s, (R[1, 2] + R[2, 1]) / s, (R[0, 2] - R[2, 0]) / s])
    s = math.sqrt(1.0 + R[2, 2] - R[0, 0] - R[1, 1]) * 2
    return np.array([(R[0, 2] + R[2, 0]) / s, (R[1, 2] + R[2, 1]) / s, 0.25 * s, (R[1, 0] - R[0, 1]) / s])

def find(root, path):
    n = root
    for part in path.split('/'):
        nxt = [c for c in n.children if c.name == part]
        if not nxt:
            return None
        n = nxt[0]
    return n

def world_of(n):
    M = np.eye(4)
    chain = []
    while n is not None:
        chain.append(n)
        n = n.parent
    for c in reversed(chain[:-1]):
        M = M @ c.local()
    return M

def find_all(root, path):
    """Tous les nœuds de ce chemin (le dernier nom peut être porté par plusieurs enfants)."""
    head, _, last = path.rpartition('/')
    parent = find(root, head) if head else root
    return [c for c in parent.children if c.name == last] if parent is not None else []

# ---------------------------------------------------------------- les modèles et les formes
def kit_model(spec):
    kit, name = spec.split('/', 1)
    path = os.path.join(pr.ASSETS, KITS[kit], name + '.fbx')
    out = []
    # Le modèle tel que Unity l'instancie (avec l'orientation et l'échelle de sa racine) : la rotation de l'opération s'y ajoute.
    for (P, T, C, nm, UV) in load(path, local=False):
        out.append((P, T, C, {'kit': kit, 'uv': UV}))
    return out

def model_any(spec):
    if spec.split('/', 1)[0] in KITS:
        return kit_model(spec)
    path = os.path.join(pr.ASSETS, spec)
    return [(P, T, C, {'kit': None, 'uv': UV}) for (P, T, C, nm, UV) in load(path, local=False)]

def prim_mesh(kind):
    k = {'box': 'Cube', 'cylinder': 'Cylindre', 'sphere': 'Sphere'}[kind]
    return pr.prim(k)

def rotate_bounds(meshes, rot):
    R = pr.quat(rot)
    allp = np.vstack([(R @ m[0].T).T for m in meshes])
    return allp.min(axis=0), allp.max(axis=0)

def apply_ops(root, ops, pal):
    for op in ops:
        kind = op['op']
        if kind == 'destroy':
            found = find_all(root, op['path'])
            if not found:
                print('  (destroy : introuvable)', op['path'])
            for n in found:
                n.parent.children.remove(n)
        elif kind == 'set':
            found = find_all(root, op['path'])
            if not found:
                print('  (set : introuvable)', op['path'])
            for n in found:
                if 'at' in op:
                    n.pos = np.array(op['at'], float)
                if 'rot' in op:
                    n.rot = quat_euler(op['rot'])
                if 'scale' in op:
                    n.scale = np.array(op['scale'], float)
                if 'active' in op:
                    n.active = bool(op['active'])
                if 'y' in op:
                    n.pos = np.array([n.pos[0], op['y'], n.pos[2]])
                if 'dy' in op:
                    n.pos = n.pos + np.array([0, op['dy'], 0])
                if 'sy' in op:
                    n.scale = np.array([n.scale[0], op['sy'], n.scale[2]])
        elif kind in ('model', 'box', 'cylinder', 'sphere', 'empty'):
            parent = find(root, op['parent']) if op.get('parent') else root
            if parent is None:
                print('  (parent introuvable)', op.get('parent'))
                continue
            n = Node(op['name'])
            n.rot = quat_euler(op.get('rot', [0, 0, 0]))
            if kind == 'model':
                meshes = model_any(op['model'])
                n.meshes = meshes
                if 'width' in op or 'height' in op:
                    lo, hi = rotate_bounds(meshes, n.rot)
                    size = hi - lo
                    s = op['width'] / max(size[0], size[2]) if 'width' in op else op['height'] / size[1]
                    n.scale = np.array([s, s, s])
                    at = np.array(op.get('at', [0, 0, 0]), float)
                    c = (lo + hi) / 2
                    n.pos = at - s * np.array([c[0], lo[1], c[2]])
                else:
                    n.scale = np.array(op.get('scale', [1, 1, 1]), float) if isinstance(op.get('scale', 1), list) else np.ones(3) * op.get('scale', 1)
                    n.pos = np.array(op.get('at', [0, 0, 0]), float)
                if op.get('palette'):
                    for i, m in enumerate(n.meshes):
                        meta = dict(m[3]); meta['palette'] = op['palette']
                        n.meshes[i] = (m[0], m[1], m[2], meta)
            elif kind == 'empty':
                n.pos = np.array(op.get('at', [0, 0, 0]), float)
                if op.get('root'):
                    # Place et orientation données dans le repère du prefab, à l'échelle 1 (sous un parent mis à l'échelle).
                    W = world_of(parent)
                    L = np.linalg.inv(W) @ pr.trs(n.pos, n.rot, [1, 1, 1])
                    sc = np.linalg.norm(L[:3, :3], axis=0)
                    n.pos = L[:3, 3].copy(); n.scale = sc; n.rot = mat_quat(L[:3, :3] / sc)
            else:
                V, T = prim_mesh(kind)
                size = np.array(op['size'], float)
                n.scale = size * (np.array([1, 0.5, 1]) if kind == 'cylinder' else 1)
                n.pos = np.array(op.get('at', [0, 0, 0]), float)
                n.meshes = [(V, T, None, {'role': op.get('role'), 'shade': op.get('shade', 1.0), 'mat': op.get('mat')})]
            parent.add(n)
        elif kind == 'paint':
            for n in find_all(root, op['path']):
                def paint(m):
                    for i, mm in enumerate(m.meshes):
                        meta = dict(mm[3]); meta['palette'] = op['palette']
                        m.meshes[i] = (mm[0], mm[1], mm[2], meta)
                    for c in m.children:
                        paint(c)
                paint(n)
        elif kind in ('field', 'follow'):
            pass
        else:
            print('  op inconnue', kind)

def apply_passes(root, look, tour, pals):
    """Les passes du fichier d'allure pour ce prefab, dans l'ordre, comme le baker : chacune seulement si le prefab n'a pas
    encore sa marque et a celle qu'elle demande ; sa marque est posée ensuite."""
    for spec in look['towers']:
        if spec['prefab'] != tour:
            continue
        mark = spec.get('mark', 'Allure du 30 octobre')
        if find(root, mark) is not None or (spec.get('requires') and find(root, spec['requires']) is None):
            continue
        apply_ops(root, spec.get('ops', []), pals)
        flag = Node(mark)
        flag.active = False
        root.add(flag)

# ---------------------------------------------------------------- palettes
_paltex = {}

def palette_texture(tour, kit, pals):
    key = (tour, kit)
    if key not in _paltex:
        img = np.asarray(Image.open(os.path.join(pr.ASSETS, KIT_TEX[kit])).convert('RGB'), dtype=np.float64) / 255.0
        _paltex[key] = pal_mod.recolor(img, pals[tour]).astype(np.float32)
    return _paltex[key]

FIXED = {'corde': '#BFA77A', 'cuivre': '#B86B3A'}

def role_color(pal, ro, shade):
    if ro in FIXED:
        return np.clip(pal_mod.hexrgb(FIXED[ro]) * shade, 0, 1)
    return pal_mod.flat(pal, ro, shade)

def colorize(root, tour, pals, matcol):
    pal = pals[tour]
    def walk(n):
        for i, (V, T, C, meta) in enumerate(n.meshes):
            # Déjà aux couleurs de sa tour (une tour imbriquée dans un duo) : on n'y touche plus.
            if meta.get('fixed'):
                continue
            if meta.get('kit') and meta.get('uv') is not None:
                tex = palette_texture(meta.get('palette', tour), meta['kit'], pals)
                C = pr.sample(tex, meta['uv'])
            elif 'role' in meta and meta.get('role'):
                C = np.tile(role_color(pal, meta['role'], meta.get('shade', 1.0)), (len(T), 1))
            elif meta.get('mat'):
                C = np.tile(matcol(meta['mat']), (len(T), 1))
            elif meta.get('srcmat') in ROLE_OF_MAT:
                ro, sh = ROLE_OF_MAT[meta['srcmat']]
                C = np.tile(role_color(pal, ro, sh), (len(T), 1))
            n.meshes[i] = (V, T, C, meta)
        for c in n.children:
            walk(c)
    walk(root)

def freeze(root):
    """Fige les couleurs de cette tour : les opérations et la palette d'un duo ne les changent plus (comme le baker, qui ne
    repeint que les modèles des kits encore à la texture d'origine)."""
    def walk(n):
        n.meshes = [(V, T, C, dict(meta, fixed=True)) for (V, T, C, meta) in n.meshes]
        for c in n.children:
            walk(c)
    walk(root)

# ---------------------------------------------------------------- tours et duos
def build_tower(tour, look, pals, matcol, passes=True):
    """Une tour telle que le baker la laisse : son prefab, ses passes du fichier d'allure, ses couleurs. Renvoie sa racine et
    ses pièces de niveau."""
    parts = []
    root = from_pr(pr.build(os.path.join(pr.ASSETS, '_Project/Prefabs/Structures', tour + '.prefab'), parts))
    if passes:
        apply_passes(root, look, tour, pals)
        colorize(root, tour, pals, matcol)
    return root, parts

def duo_spec(look, name):
    return next(d for d in look.get('duos', []) if d['name'] == name)

def build_duo(look, name, pals, matcol):
    """Un duo (31 octobre ; « duos » du fichier d'allure) : une tour nouvelle, faite de ses seules opérations, à sa palette,
    comme le baker la construit. Ses pièces des niveaux 2 et 3 sont les repères « Niveau 2 » et « Niveau 3 ». Renvoie la racine
    et [(la racine, None, ses pièces de niveau)]."""
    d = duo_spec(look, name)
    root = Node('Duo')
    apply_ops(root, d.get('ops', []), pals)
    colorize(root, d.get('palette', 'Duo'), pals, matcol)
    parts = []
    for level in (2, 3):
        node = find_named(root, 'Niveau %d' % level)
        if node is not None:
            tag = 'duo:%s:%d' % (name, level)
            node.gos = node.gos | {tag}
            parts.append(tag)
    return root, [(root, None, parts)]

def find_named(n, name):
    """Le premier nœud de ce nom, à n'importe quelle profondeur."""
    if n.name == name:
        return n
    for c in n.children:
        found = find_named(c, name)
        if found is not None:
            return found
    return None

def build_any(name, look, pals, matcol):
    """« Duo:Canon » : un duo ; sinon une tour. Renvoie la racine et ses tours (nœud, nom, pièces de niveau)."""
    if name.startswith('Duo:'):
        return build_duo(look, name[4:], pals, matcol)
    root, parts = build_tower(name, look, pals, matcol)
    return root, [(root, name, parts)]

def parse_levels(text):
    """« 3,5 » : les niveaux à dessiner (1 à 5 pour une tour, 1 à 3 pour un duo)."""
    return [tuple(int(x) for x in token.split('-')) for token in text.split(',')]

# ---------------------------------------------------------------- scène, niveaux
def collect(n, M, hidden, out):
    if not n.active or (n.gos & hidden):
        return
    W = M @ n.local()
    for (V, T, C, meta) in n.meshes:
        if C is None:
            continue
        P = (np.c_[V, np.ones(len(V))] @ W.T)[:, :3]
        out.append((P, T, C))
    for c in n.children:
        collect(c, W, hidden, out)

# Les échelles que les scripts donnent en jeu, par niveau (FrostTower : les cristaux ; StructureLighthouse : le faisceau).
RUNTIME = {'Givre': [('tower-round-crystals', [1, 1.08, 1.16, 1.24, 1.35])], 'Phare': [('Faisceau', [1, 1, 1.6, 1.6, 2])]}

def level_scene(root, parts, level, tour=None):
    return halves_scene(root, [(root, tour, parts)], (level,))

def halves_scene(root, halves, levels, visit=None):
    """La scène d'une tour ou d'un duo, chaque tour à son niveau (levels, dans l'ordre de halves). visit(root, hidden) remplace
    la collecte des maillages si on veut autre chose (la recherche des faces superposées)."""
    hidden = set()
    saved = []
    for (node, tour, parts), level in zip(halves, levels):
        hidden |= set(p for i, p in enumerate(parts) if level < i + 2)
        for path, scales in RUNTIME.get(tour, []):
            n = find(node, path)
            if n is not None:
                saved.append((n, n.scale.copy()))
                n.scale = n.scale * scales[level - 1]
    try:
        if visit is not None:
            return visit(root, hidden)
        out = []
        collect(root, np.eye(4), hidden, out)
        return out
    finally:
        for n, sc in saved:
            n.scale = sc
