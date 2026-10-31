# Rendu d'un prefab Unity (YAML) niveau par niveau : formes simples, modèles FBX imbriqués, matériaux (couleur, texture).
# Seul : py -I prefab.py <Assets> <prefab> <sortie.png> [vues=iso,face] [taille=300] [niveaux=1,2,3,4,5] (le prefab tel quel).
import os, re, sys, pickle, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import numpy as np
from lecture_fbx import load
from rendu import render, sheet

ASSETS = None
GUIDS = None
head = re.compile(r'^--- !u!(\d+) &(-?\d+)( stripped)?')

def guid_map(assets):
    import tempfile, time
    cache = os.path.join(tempfile.gettempdir(), 'archery_guids.pkl')
    if os.path.exists(cache) and time.time() - os.path.getmtime(cache) < 600:
        return pickle.load(open(cache, 'rb'))
    guids = {}
    for root, dirs, files in os.walk(assets):
        for f in files:
            if f.endswith('.meta'):
                p = os.path.join(root, f)
                with open(p, 'r', encoding='utf-8', errors='ignore') as fh:
                    for line in fh:
                        if line.startswith('guid:'):
                            guids[line.split()[1]] = os.path.relpath(p[:-5], assets).replace('\\', '/')
                            break
    pickle.dump(guids, open(cache, 'wb'))
    return guids

def parse(path):
    docs = {}
    cur = None
    with open(path, 'r', encoding='utf-8', errors='ignore') as fh:
        for line in fh:
            m = head.match(line)
            if m:
                cur = {'cls': int(m.group(1)), 'id': m.group(2), 'stripped': bool(m.group(3)), 'lines': []}
                docs[cur['id']] = cur
            elif cur is not None:
                cur['lines'].append(line.rstrip('\n'))
    return docs

def field(doc, key):
    for l in doc['lines']:
        s = l.strip()
        if s.startswith(key + ':'):
            return s[len(key) + 1:].strip()
    return None

def ystr(s):
    """Une chaîne YAML de Unity : entre guillemets, ses caractères non ASCII sont échappés (\xE2, \u0153)."""
    if s and len(s) >= 2 and s[0] == '"' and s[-1] == '"':
        import codecs
        return codecs.decode(s[1:-1], 'unicode_escape')
    return s

def ref(s):
    m = re.search(r'fileID: (-?\d+)', s or '')
    return m.group(1) if m else None

def gref(s):
    m = re.search(r'guid: ([0-9a-f]+)', s or '')
    return m.group(1) if m else None

def nums(s):
    return [float(n) for n in re.findall(r':\s*(-?[\d.]+(?:[eE][-+]?\d+)?)', s or '')]

def quat(q):
    x, y, z, w = q
    return np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                     [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                     [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])

def trs(p, q, s):
    M = np.eye(4)
    M[:3, :3] = quat(q) @ np.diag(s)
    M[:3, 3] = p
    return M

# ---------------------------------------------------------------- formes simples de Unity
def prim(kind):
    if kind == 'Cube':
        v = np.array([[x, y, z] for x in (-.5, .5) for y in (-.5, .5) for z in (-.5, .5)])
        f = [(0, 1, 3), (0, 3, 2), (4, 6, 7), (4, 7, 5), (0, 4, 5), (0, 5, 1), (2, 3, 7), (2, 7, 6), (0, 2, 6), (0, 6, 4), (1, 5, 7), (1, 7, 3)]
        return v, np.array(f)
    if kind in ('Cylindre', 'Capsule'):
        n = 20
        v = []
        for y in (-1, 1):
            for i in range(n):
                a = 2 * math.pi * i / n
                v.append((0.5 * math.cos(a), y, 0.5 * math.sin(a)))
        v += [(0, -1, 0), (0, 1, 0)]
        f = []
        for i in range(n):
            j = (i + 1) % n
            f += [(i, j, n + j), (i, n + j, n + i), (2 * n, j, i), (2 * n + 1, n + i, n + j)]
        return np.array(v), np.array(f)[:, ::-1]  # normales vers l'extérieur, comme le cube
    if kind == 'Sphere':
        la, lo = 10, 16
        v = []
        for i in range(la + 1):
            t = math.pi * i / la
            for j in range(lo):
                p = 2 * math.pi * j / lo
                v.append((0.5 * math.sin(t) * math.cos(p), 0.5 * math.cos(t), 0.5 * math.sin(t) * math.sin(p)))
        f = []
        for i in range(la):
            for j in range(lo):
                a, b, c, d = i * lo + j, i * lo + (j + 1) % lo, (i + 1) * lo + j, (i + 1) * lo + (j + 1) % lo
                f += [(a, b, d), (a, d, c)]
        return np.array(v), np.array(f)
    if kind == 'Quad':
        return np.array([(-.5, -.5, 0), (.5, -.5, 0), (.5, .5, 0), (-.5, .5, 0)]), np.array([(0, 1, 2), (0, 2, 3)])
    return None, None

def kit_of(path):
    for k, v in (('td', 'kenney_tower-defense-kit/'), ('castle', 'kenney_castle-kit/'), ('town', 'kenney_fantasy-town-kit/')):
        if path.replace(chr(92), '/').startswith(v):
            return k
    return None

def mat_name(guid):
    return os.path.splitext(os.path.basename(GUIDS.get(guid, '')))[0] if guid else None

PRIMS = {'10202': 'Cube', '10206': 'Cylindre', '10207': 'Sphere', '10208': 'Capsule', '10210': 'Quad'}

# ---------------------------------------------------------------- matériaux
_mats = {}
_tex = {}

def material(guid):
    """(couleur RGB, texture ou None, émissif) d'un .mat."""
    if guid in _mats:
        return _mats[guid]
    path = GUIDS.get(guid)
    out = (np.array([0.8, 0.8, 0.8]), None, False)
    if path and path.endswith('.mat'):
        txt = open(os.path.join(ASSETS, path), encoding='utf-8', errors='ignore').read()
        m = re.search(r'- _BaseColor: \{r: ([-\d.e]+), g: ([-\d.e]+), b: ([-\d.e]+)', txt) or re.search(r'- _Color: \{r: ([-\d.e]+), g: ([-\d.e]+), b: ([-\d.e]+)', txt)
        col = np.array([float(m.group(i)) for i in (1, 2, 3)]) if m else np.array([0.8, 0.8, 0.8])
        t = re.search(r'- _BaseMap:\s*\n\s*m_Texture: \{fileID: \d+, guid: ([0-9a-f]+)', txt) or re.search(r'- _MainTex:\s*\n\s*m_Texture: \{fileID: \d+, guid: ([0-9a-f]+)', txt)
        tex = texture(t.group(1)) if t else None
        em = re.search(r'- _EmissionColor: \{r: ([-\d.e]+), g: ([-\d.e]+), b: ([-\d.e]+)', txt)
        emissive = bool(em) and '_EMISSION' in txt and max(float(em.group(i)) for i in (1, 2, 3)) > 0.05
        if emissive:
            col = np.clip(col + np.array([float(em.group(i)) for i in (1, 2, 3)]) * 0.5, 0, 1)
        out = (col, tex, emissive)
    _mats[guid] = out
    return out

def texture(guid):
    if guid in _tex:
        return _tex[guid]
    path = GUIDS.get(guid)
    img = None
    if path and os.path.splitext(path)[1].lower() in ('.png', '.jpg', '.tga'):
        try:
            from PIL import Image
            img = np.asarray(Image.open(os.path.join(ASSETS, path)).convert('RGB'), dtype=np.float32) / 255.0
        except Exception:
            img = None
    _tex[guid] = img
    return img

def sample(tex, uv):
    h, w = tex.shape[:2]
    x = ((uv[:, 0] % 1.0) * w).astype(int) % w
    y = ((1.0 - (uv[:, 1] % 1.0)) * h).astype(int) % h
    return tex[y, x]

# ---------------------------------------------------------------- le prefab
class Node:
    def __init__(self, name):
        self.name = name
        self.local = np.eye(4)
        self.children = []
        self.meshes = []        # (V, T, couleurs)
        self.active = True
        self.go = None

def build(path, level_parts_out=None):
    docs = parse(path)
    comps = {}
    for d in docs.values():
        if d['cls'] == 1:
            comps[d['id']] = [ref(l) for l in d['lines'] if 'component:' in l]
    nodes = {}
    tr_of_go = {}
    for d in docs.values():
        if d['cls'] == 4 and not d['stripped']:
            go = ref(field(d, 'm_GameObject'))
            g = docs.get(go)
            n = Node(ystr(field(g, 'm_Name')) if g else '?')
            n.go = go
            n.active = not (g and field(g, 'm_IsActive') == '0')
            q = nums(field(d, 'm_LocalRotation'))
            p = nums(field(d, 'm_LocalPosition'))
            s = nums(field(d, 'm_LocalScale'))
            n.local = trs(p, q, s)
            nodes[d['id']] = n
            tr_of_go[go] = d['id']
            for c in comps.get(go, []):
                cd = docs.get(c)
                if cd is None:
                    continue
                if cd['cls'] == 114 and level_parts_out is not None:
                    take = False
                    for l in cd['lines']:
                        if l.strip().startswith('m_LevelParts:'):
                            take = True
                            continue
                        if take:
                            if l.strip().startswith('- {fileID'):
                                level_parts_out.append(ref(l))
                            else:
                                take = False
            mf = [docs[c] for c in comps.get(go, []) if c in docs and docs[c]['cls'] == 33]
            mr = [docs[c] for c in comps.get(go, []) if c in docs and docs[c]['cls'] == 23]
            if mf and mr and field(mr[0], 'm_Enabled') != '0':
                mesh = field(mf[0], 'm_Mesh')
                kind = PRIMS.get(ref(mesh)) if (gref(mesh) or '').startswith('0000000000000000e') else None
                matg = None
                take = False
                for l in mr[0]['lines']:
                    if l.strip().startswith('m_Materials:'):
                        take = True
                        continue
                    if take:
                        matg = gref(l)
                        break
                col, tex, em = material(matg) if matg else (np.array([.8, .8, .8]), None, False)
                if kind:
                    V, T = prim(kind)
                    n.meshes.append((V, T, np.tile(col, (len(T), 1)), {'srcmat': mat_name(matg)}))
                elif gref(mesh) and GUIDS.get(gref(mesh), '').lower().endswith('.fbx'):
                    # un maillage d'un FBX copié tel quel (Object.Instantiate) : on prend le nœud du même nom
                    fbx = os.path.join(ASSETS, GUIDS[gref(mesh)])
                    found = load(fbx, local='node')
                    base = os.path.splitext(os.path.basename(fbx))[0]
                    pick = [m for m in found if m[3] == n.name] or [m for m in found if m[3] == base] or found[:1]
                    for (P, Tm, C, nm, UV) in pick[:1]:
                        cols = sample(tex, UV) * col if (tex is not None and UV is not None) else C
                        n.meshes.append((P, Tm, cols, {'kit': kit_of(GUIDS[gref(mesh)]), 'uv': UV, 'srcmat': mat_name(matg)}))
    for d in docs.values():
        if d['cls'] == 4 and not d['stripped']:
            f = ref(field(d, 'm_Father'))
            if f in nodes:
                nodes[f].children.append(nodes[d['id']])
    # instances de prefab (modèles FBX, prefabs imbriqués)
    stripped = {}
    for d in docs.values():
        if d['cls'] == 4 and d['stripped']:
            stripped.setdefault(ref(field(d, 'm_PrefabInstance')), []).append(d['id'])
    stripped_go = {}
    for d in docs.values():
        if d['cls'] == 1 and d['stripped']:
            stripped_go.setdefault(ref(field(d, 'm_PrefabInstance')), []).append(d['id'])
    for d in docs.values():
        if d['cls'] != 1001:
            continue
        src = GUIDS.get(gref(field(d, 'm_SourcePrefab')), '')
        mods = {}
        prop = None
        matover = None
        active = True
        for l in d['lines']:
            s = l.strip()
            if s.startswith('- target:'):
                prop = None
            elif s.startswith('propertyPath:'):
                prop = s.split(':', 1)[1].strip()
            elif s.startswith('value:') and prop:
                mods[prop] = s.split(':', 1)[1].strip()
                if prop == 'm_IsActive' and mods[prop] == '0':
                    active = False
            elif s.startswith('objectReference:') and prop and prop.startswith('m_Materials'):
                g = gref(s)
                if g:
                    matover = g
        def f(k, dflt):
            try:
                return float(mods.get(k, dflt))
            except ValueError:
                return dflt
        n = Node(ystr(mods.get('m_Name', os.path.basename(src))))
        n.active = active
        gos = stripped_go.get(d['id'], [])
        n.go = gos[0] if gos else 'pi' + d['id']
        n.gos = set(gos)
        n.local = trs([f('m_LocalPosition.' + a, 0) for a in 'xyz'], [f('m_LocalRotation.' + a, 1 if a == 'w' else 0) for a in 'xyzw'],
                      [f('m_LocalScale.' + a, 1) for a in 'xyz'])
        if src.lower().endswith('.fbx'):
            col, tex, em = material(matover) if matover else (None, None, False)
            for (P, Tm, C, nm, UV) in load(os.path.join(ASSETS, src), local=True):
                cols = C
                if matover:
                    cols = sample(tex, UV) * col if (tex is not None and UV is not None) else np.tile(col, (len(Tm), 1))
                n.meshes.append((P, Tm, cols, {'kit': None if matover else kit_of(src), 'uv': UV, 'srcmat': mat_name(matover)}))
        elif src.endswith('.prefab'):
            sub = build(os.path.join(ASSETS, src))
            if sub:
                n.children.append(sub)
        parent = ref(field(d, 'm_TransformParent'))
        if parent in nodes:
            nodes[parent].children.append(n)
        for st in stripped.get(d['id'], []):
            nodes[st] = n
    # enfants ajoutés sous une instance
    for d in docs.values():
        if d['cls'] == 4 and not d['stripped']:
            f_ = ref(field(d, 'm_Father'))
            if f_ in nodes and nodes[f_] is not None and nodes[d['id']] not in nodes[f_].children:
                nodes[f_].children.append(nodes[d['id']])
    roots = [nodes[d['id']] for d in docs.values() if d['cls'] == 4 and not d['stripped'] and ref(field(d, 'm_Father')) in (None, '0')]
    return roots[0] if roots else None

def collect(node, M, hidden, out, skip_names=()):
    if not node.active or node.go in hidden or (getattr(node, 'gos', None) and node.gos & hidden) or node.name in skip_names:
        return
    W = M @ node.local
    for (V, T, C, *rest) in node.meshes:
        P = (np.c_[V, np.ones(len(V))] @ W.T)[:, :3]
        out.append((P, T, C))
    for c in node.children:
        collect(c, W, hidden, out, skip_names)

def level_scene(root, parts, level):
    hidden = set(p for i, p in enumerate(parts) if level < i + 2)
    out = []
    collect(root, np.eye(4), hidden, out)
    return out

if __name__ == '__main__':
    ASSETS = sys.argv[1]
    GUIDS = guid_map(ASSETS)
    prefab, outp = sys.argv[2], sys.argv[3]
    views = (sys.argv[4] if len(sys.argv) > 4 else 'iso,face').split(',')
    size = int(sys.argv[5]) if len(sys.argv) > 5 else 300
    levels = [int(x) for x in (sys.argv[6] if len(sys.argv) > 6 else '1,2,3,4,5').split(',')]
    parts = []
    root = build(prefab, parts)
    scenes = [level_scene(root, parts, L) for L in levels]
    allp = np.vstack([t[0] for s in scenes for t in s])
    lo, hi = allp.min(axis=0), allp.max(axis=0)
    lo[1] = min(lo[1], 0)
    print(os.path.basename(prefab), 'taille max (m) :', np.round(hi - lo, 2), 'min', np.round(lo, 2), 'max', np.round(hi, 2))
    images = []
    for v in views:
        for L, s in zip(levels, scenes):
            images.append(render(s, v, size, bounds=(lo, hi), grid=True, title='%s niveau %d  %s' % (os.path.splitext(os.path.basename(prefab))[0], L, v)))
    print(sheet(images, len(levels), outp))
