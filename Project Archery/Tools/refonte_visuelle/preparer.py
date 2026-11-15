# Prépare un bâtiment pour Blender (14 novembre ; Docs/Guide_Blender.md, Docs/Refonte_Visuelle.md) : ses images de référence
# (Docs/Planches/<Nom>), puis son fichier de départ (Modeles_Blender/<Nom>.blend), fait par Blender en arrière-plan
# (Tools/blender/depart.py) : les images posées, le modèle de Claude niveau par niveau (une référence verrouillée, jamais
# exportée), les repères du code en objets vides nommés à leur place, le cahier des charges, et ce qu'il faut à l'extension
# THUNK pour vérifier et exporter. Ne remplace jamais un fichier .blend existant (le travail du joueur) : avec --refaire,
# l'ancien est d'abord renommé (<Nom>_ancien_<date>.blend).
# Depuis « Project Archery/Assets » :
#   py -I ../Tools/refonte_visuelle/preparer.py _Project/Prefabs/Structures/Givre.prefab [autre.prefab…] [--refaire]
# Blender : le plus récent de « C:/Program Files/Blender Foundation », ou le chemin de la variable THUNK_BLENDER.
import os, sys, re, json, glob, math, datetime, subprocess, tempfile
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'allure_tours'))
sys.path.insert(0, HERE)
sys.stdout.reconfigure(encoding='utf-8')
import numpy as np
import prefab as pf
import planche
import reperes

ASSETS = '.'
PROJECT = os.path.abspath('..')
BLENDS = os.path.join(PROJECT, 'Modeles_Blender')
DEPART = os.path.join(PROJECT, 'Tools', 'blender', 'depart.py')
# Unity -> Blender (l'export en « -Z Forward, Y Up » fait l'inverse) : x = -x, y = -z, z = y.
A = np.array([[-1.0, 0, 0], [0, 0, -1.0], [0, 1.0, 0]])


def blender_exe():
    exe = os.environ.get('THUNK_BLENDER')
    if exe and os.path.exists(exe):
        return exe
    found = glob.glob('C:/Program Files/Blender Foundation/Blender */blender.exe')
    def version(p):
        m = re.search(r'Blender (\d+)\.(\d+)', p)
        return (int(m.group(1)), int(m.group(2))) if m else (0, 0)
    return max(found, key=version) if found else None


def to_blender_matrix(W):
    R = W[:3, :3].copy()
    for i in range(3):
        n = np.linalg.norm(R[:, i])
        if n > 1e-9:
            R[:, i] /= n
    M = np.eye(4)
    M[:3, :3] = A @ R @ A.T
    M[:3, 3] = A @ W[:3, 3]
    return M


def to_blender_mesh(tris):
    """Les triangles d'une scène (repère d'Unity) en un seul maillage de Blender : sommets, faces (sens corrigé), couleurs."""
    V, F, C = [], [], []
    off = 0
    for P, T, col in tris:
        V.append(P @ A.T)
        F.append(T[:, [0, 2, 1]] + off)   # le repère change de sens : on retourne les triangles
        C.append(np.asarray(col)[:, :3])
        off += len(P)
    if not V:
        return None
    return (np.vstack(V).astype(np.float32), np.vstack(F).astype(np.int32), np.clip(np.vstack(C), 0, 1).astype(np.float32))


def descriptions(prefab_path, guids):
    """Les textes des niveaux, pris dans la définition du bâtiment (Data/…) qui désigne ce prefab."""
    meta = prefab_path + '.meta'
    g = re.search(r'guid: ([0-9a-f]+)', open(meta, encoding='utf-8').read()).group(1)
    for path in glob.glob('_Project/Data/**/*.asset', recursive=True):
        text = open(path, encoding='utf-8', errors='ignore').read()
        if g not in text or 'description:' not in text:
            continue
        name = re.search(r'displayName: (.*)', text)
        out = []
        lines = text.splitlines()
        for i, l in enumerate(lines):
            m = re.match(r'(\s*)description: (.*)', l)
            if not m:
                continue
            s = m.group(2)
            j = i + 1
            # la suite d'un texte long : les lignes plus en retrait que la clé
            while j < len(lines) and lines[j].strip() and len(lines[j]) - len(lines[j].lstrip()) > len(m.group(1)):
                s += ' ' + lines[j].strip()
                j += 1
            out.append(reperes._name(s.strip()))
        return (reperes._name(name.group(1).strip()) if name else None), out
    return None, []


def preparer(prefab_path, refaire=False):
    name = os.path.splitext(os.path.basename(prefab_path))[0]
    out = os.path.join(BLENDS, name + '.blend')
    if os.path.exists(out):
        if not refaire:
            print('%s : %s existe déjà (ton travail) ; rien n\'est touché. Pour le refaire : --refaire (l\'ancien est gardé).' % (name, out))
            return False
        backup = os.path.join(BLENDS, '%s_ancien_%s.blend' % (name, datetime.datetime.now().strftime('%Y%m%d_%H%M%S')))
        os.rename(out, backup)
        print('%s : l\'ancien fichier est gardé sous %s.' % (name, os.path.basename(backup)))

    pf.ASSETS = ASSETS
    pf.GUIDS = pf.guid_map(ASSETS)
    parts, nodes = [], {}
    root = pf.build(prefab_path, parts, nodes)
    images = planche.faire(prefab_path, os.path.join(PROJECT, 'Docs', 'Planches', name), built=(root, parts))
    fiche = reperes.lire(prefab_path, guids=pf.GUIDS)

    # La place de chaque nœud dans le prefab (repère d'Unity) et son parent.
    world, parent_of = {}, {}
    def walk(n, M, parent):
        W = M @ n.local
        world[id(n)] = W
        parent_of[id(n)] = parent
        for c in n.children:
            walk(c, W, n)
    walk(root, np.eye(4), None)

    # Les repères du code : un objet vide chacun dans Blender, à la place de l'objet dans le modèle de Claude.
    keep = set(fiche.keep())
    order = [k for k in fiche.depth_first() if k in keep]
    names = [fiche.objets[k].name for k in order]
    keys, seen = {}, {}
    for k, n in zip(order, names):
        if names.count(n) > 1:
            seen[n] = seen.get(n, 0) + 1
            keys[k] = '%s #%d' % (n, seen[n])
        else:
            keys[k] = n
    marks = []
    for k in order:
        o = fiche.objets[k]
        pfn = nodes.get(k)
        if pfn is None:
            for s in o.stripped:
                pfn = nodes.get(s) or pfn
        if pfn is None or id(pfn) not in world:
            print('  %s : repère introuvable dans le modèle, %s (ignoré)' % (name, o.name))
            continue
        p = o.parent
        while p in fiche.objets and p not in keep:
            p = fiche.objets[p].parent
        # « une pièce » : l'objet avait une forme à lui dans le modèle de Claude (le code peut lire son maillage)
        marks.append({'cle': keys[k], 'nom': o.name, 'chemin': [{'nom': s, 'rang': r} for s, r in fiche.path(k)],
                      'parent': keys.get(p), 'piece': bool(o.mesh), 'champs': sorted(o.used),
                      'script': bool(o.scripted), 'matrice': to_blender_matrix(world[id(pfn)]).tolist()})

    # Le modèle de Claude : le niveau 1, puis ce que chaque niveau ajoute.
    meshes = []
    base = to_blender_mesh(images['scenes'][0])
    if base:
        meshes.append(('Claude - niveau 1', base))
    for i, part in enumerate(parts):
        node = None
        for n_ in nodes.values():
            if n_ is not None and (n_.go == part or part in getattr(n_, 'gos', set())):
                node = n_
                break
        if node is None or id(node) not in world:
            continue
        par = parent_of[id(node)]
        out_tris = []
        pf.collect(node, world[id(par)] if par is not None else np.eye(4), set(), out_tris)
        m = to_blender_mesh(out_tris)
        if m:
            meshes.append(('Claude - niveau %d (ajouts)' % (i + 2), m))

    # Les cases au sol et les hauteurs du modèle de Claude.
    P1 = np.vstack([t[0] for t in images['scenes'][0]])
    size = P1.max(axis=0) - P1.min(axis=0)
    cases = [int(min(6, max(1, math.ceil(size[0] - 0.15)))), int(min(6, max(1, math.ceil(size[2] - 0.15))))]
    title, descs = descriptions(prefab_path, pf.GUIDS)

    tmp = tempfile.mkdtemp(prefix='thunk_')
    npz = os.path.join(tmp, name + '.npz')
    arrays = {}
    for i, (n, (V, F, C)) in enumerate(meshes):
        arrays['V%d' % i], arrays['F%d' % i], arrays['C%d' % i] = V, F, C
    np.savez_compressed(npz, **arrays)
    meta = {
        'version': 1, 'batiment': name, 'titre': title or name, 'prefab': 'Assets/' + prefab_path.replace('\\', '/'),
        'fbx': 'Assets/_Project/Art/Modeles/%s.fbx' % name, 'niveaux': images['levels'], 'descriptions': descs,
        'cases': cases, 'hauteur_niveau1': float(P1[:, 1].max()), 'hauteur': float(images['hi'][1]), 'L': images['L'],
        'images': images['images'], 'npz': npz, 'maillages': [n for n, _ in meshes], 'reperes': marks,
        'prepare_le': datetime.date.today().isoformat(),
    }
    js = os.path.join(tmp, name + '.json')
    json.dump(meta, open(js, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

    exe = blender_exe()
    if not exe:
        print('Blender introuvable (installe-le, ou donne son chemin dans la variable THUNK_BLENDER).')
        return False
    os.makedirs(BLENDS, exist_ok=True)
    r = subprocess.run([exe, '--background', '--factory-startup', '--python', DEPART, '--', js, out],
                       capture_output=True, text=True, encoding='utf-8', errors='replace')
    lines = [l for l in (r.stdout + r.stderr).splitlines() if l.startswith('THUNK') or 'Error' in l or 'Traceback' in l]
    for l in lines:
        print('  ' + l)
    if not os.path.exists(out):
        print('%s : le fichier n\'a pas été créé.' % name)
        print((r.stdout + r.stderr)[-3000:])
        return False
    print('%s : %s (%d repères du code, %d niveaux, %d × %d cases au sol chez Claude).' % (
        name, os.path.relpath(out, PROJECT), len(marks), images['levels'], cases[0], cases[1]))
    return True


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    refaire = '--refaire' in sys.argv
    ok = all([preparer(a, refaire) for a in args])
    sys.exit(0 if ok else 1)


if __name__ == '__main__':
    main()
