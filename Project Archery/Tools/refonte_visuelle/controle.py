# Le contrôle d'une tour faite main (14 novembre ; Docs/Refonte_Visuelle.md, « Après chaque tour ») : sur ce que l'extension a
# exporté (Art/Modeles/<Nom>.fbx), comparé au modèle de Claude (le prefab tel qu'il était avant, dans Git, ou le prefab actuel) :
# - les tailles, niveau par niveau (largeur, profondeur, hauteur ; les cases au sol) ;
# - les faces superposées : deux faces dans le même plan, tournées du même côté, qui se recouvrent (elles scintillent en jeu) ;
# - les faces presque collées : deux faces parallèles qui se recouvrent, à moins de 3 cm l'une de l'autre sans se toucher (un
#   jour fin visible de côté), ou du même côté à quelques millimètres (elles scintillent de loin, dans le casque).
# Depuis « Project Archery/Assets » :
#   py -I ../Tools/refonte_visuelle/controle.py Givre [référence.prefab]
import os, sys, math, json
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, '..', 'allure_tours'))
sys.stdout.reconfigure(encoding='utf-8')
import numpy as np
import lecture_fbx as lf
import prefab as pf

A = np.array([[-1.0, 0, 0], [0, 0, -1.0], [0, 1.0, 0]])   # Unity -> Blender


def cells(s):
    return int(min(6, max(1, math.ceil(s - 0.15))))


def tris_of(meshes):
    """Tous les triangles (repère de Blender), avec le nom de leur pièce."""
    out = []
    for P, T, C, name, UV in meshes:
        Q = P @ A.T
        for t in T:
            out.append((name, Q[t[0]], Q[t[1]], Q[t[2]]))
    return out


def clip(poly, a, b):
    """Garde la partie du polygone (2D) à gauche de la droite a->b."""
    out = []
    n = len(poly)
    def side(p):
        return (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])
    for i in range(n):
        p, q = poly[i], poly[(i + 1) % n]
        sp, sq = side(p), side(q)
        if sp >= 0:
            out.append(p)
        if (sp >= 0) != (sq >= 0):
            t = sp / (sp - sq)
            out.append((p[0] + t * (q[0] - p[0]), p[1] + t * (q[1] - p[1])))
    return out


def area(poly):
    return 0.5 * abs(sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1]
                         for i in range(len(poly)))) if len(poly) >= 3 else 0.0


def overlap(t1, t2, n):
    """L'aire commune de deux triangles presque parallèles, projetés sur leur plan."""
    u = np.cross(n, [1, 0, 0] if abs(n[0]) < 0.9 else [0, 1, 0])
    u /= np.linalg.norm(u)
    v = np.cross(n, u)
    p1 = [(float(p @ u), float(p @ v)) for p in t1]
    p2 = [(float(p @ u), float(p @ v)) for p in t2]
    if area(p2) < 1e-12:
        return 0.0
    # sens trigonométrique pour le découpage
    if (p2[1][0] - p2[0][0]) * (p2[2][1] - p2[0][1]) - (p2[1][1] - p2[0][1]) * (p2[2][0] - p2[0][0]) < 0:
        p2 = [p2[0], p2[2], p2[1]]
    poly = p1
    for i in range(3):
        poly = clip(poly, p2[i], p2[(i + 1) % 3])
        if not poly:
            return 0.0
    return area(poly)


def ensemble(niveaux):
    """Deux pièces sont-elles là en même temps à un niveau ? (15 novembre : les copies d'une pièce, une par niveau, chacune
    disparaissant au suivant, se superposent exprès.) niveaux : le .json de l'export, {pièce : (de, retire)}."""
    def span(n):
        de, retire = niveaux.get(n, (1, 0))
        return de, (retire - 1 if retire else 99)
    def f(a, b):
        sa, sb = span(a), span(b)
        return max(sa[0], sb[0]) <= min(sa[1], sb[1])
    return f


def defauts(tris, together=lambda a, b: True):
    names = [t[0] for t in tris]
    V = np.array([[t[1], t[2], t[3]] for t in tris])          # (n, 3, 3)
    N = np.cross(V[:, 1] - V[:, 0], V[:, 2] - V[:, 0])
    ar = np.linalg.norm(N, axis=1)
    ok = ar > 1e-8
    N[ok] /= ar[ok][:, None]
    C = V.mean(axis=1)
    lo, hi = V.min(axis=1), V.max(axis=1)
    found = []
    idx = np.where(ok)[0]
    for a in idx:
        # les boîtes qui se touchent (à 3 cm près), puis les faces parallèles
        cand = idx[idx > a]
        m = np.all(lo[cand] <= hi[a] + 0.03, axis=1) & np.all(hi[cand] >= lo[a] - 0.03, axis=1)
        cand = cand[m]
        if len(cand) == 0:
            continue
        d = N[cand] @ N[a]
        par = np.abs(d) > 0.999
        cand, d = cand[par], d[par]
        for b, dd in zip(cand, d):
            dist = abs(float((C[b] - C[a]) @ N[a]))
            same = dd > 0
            if same and dist > 0.01:
                continue
            if not same and (dist < 0.002 or dist > 0.03):
                continue          # collées pour de vrai (dedans, invisibles), ou franchement écartées
            if set(map(tuple, np.round(V[a], 5))) & set(map(tuple, np.round(V[b], 5))) and names[a] == names[b]:
                continue          # deux triangles voisins d'une même face
            if names[a] != names[b] and not together(names[a], names[b]):
                continue          # jamais là ensemble (une pièce remplacée à un niveau par sa copie)
            ov = overlap(V[a], V[b], N[a])
            if ov < 1e-4:          # moins de 1 cm² en commun
                continue
            if same:
                kind = 'superposées' if dist < 0.002 else 'presque superposées'
            elif abs(N[a][2]) > 0.9:
                kind = 'jour horizontal'      # deux pièces posées l'une sur l'autre, avec une fente
            else:
                kind = 'parois proches'       # souvent voulu (le rebord d'une pièce de kit autour de la suivante)
            found.append((kind, names[a], names[b], dist, ov, C[a]))
    return found


def main():
    name = sys.argv[1]
    fbx = '_Project/Art/Modeles/%s.fbx' % name
    side = json.load(open('_Project/Art/Modeles/%s.json' % name, encoding='utf-8'))
    meshes = lf.load(fbx)
    # la place de chaque pièce d'après Blender (le .json, 16 novembre) : le FBX tourne d'un quart de tour de trop une pièce
    # rattachée à un repère lui-même rattaché (comme le branchement d'Unity, Editor/ModelesFaitMain)
    places = {o['nom']: np.array(o['matrice']).reshape(4, 4) for o in side.get('objets', [])}
    if places:
        C = np.linalg.inv(A)       # Blender -> Unity
        fixed = []
        for P, T, Cl, nm, UV in lf.load(fbx, local='node'):
            if nm in places:
                M = places[nm]
                Q = (np.c_[P @ A.T, np.ones(len(P))] @ M.T)[:, :3]      # données (Unity) -> Blender local -> Blender monde
                P = Q @ C.T                                             # -> Unity monde
            fixed.append((P, T, Cl, nm, UV))
        meshes = fixed
    tris = tris_of(meshes)

    # les tailles, comparées au modèle de Claude
    ref = sys.argv[2] if len(sys.argv) > 2 else side['prefab'][len('Assets/'):]
    pf.ASSETS = '.'
    pf.GUIDS = pf.guid_map('.')
    parts = []
    root = pf.build(ref, parts)
    print('== Tailles (largeur X, profondeur Y, hauteur Z ; en m)')
    for L in range(1, len(parts) + 2):
        s = pf.level_scene(root, parts, L)
        if not s:
            continue
        P = np.vstack([t[0] for t in s]) @ A.T
        print('  Claude niveau %d : %.2f × %.2f × %.2f (%d × %d cases)' % (L, *(P.max(0) - P.min(0))[:2], P.max(0)[2],
                                                                             cells(np.ptp(P[:, 0])), cells(np.ptp(P[:, 1]))))
    Q = np.vstack([np.array([t[1], t[2], t[3]]) for t in tris])
    print('  Joueur (tout)    : %.2f × %.2f × %.2f' % (*np.ptp(Q, axis=0)[:2], Q.max(0)[2]))
    for P, T, C, nm, UV in meshes:
        B = P @ A.T
        print('    %-26s %4d tri  de (%.2f %.2f %.2f) à (%.2f %.2f %.2f)' % (nm, len(T), *B.min(0), *B.max(0)))

    niveaux = {n['piece']: (n['de'], n['retire']) for n in side.get('niveaux', [])}
    for n, (de, retire) in sorted(niveaux.items()):
        if retire:
            print('    %-26s niveaux %d à %d (disparaît au %d)' % (n, de, retire - 1, retire))
    print('== Défauts')
    found = defauts(tris, ensemble(niveaux))
    if not found:
        print('  aucun')
    seen = set()
    for kind, a, b, dist, ov, c in sorted(found, key=lambda f: (f[0], f[1], f[2])):
        key = (kind, a, b)
        if key in seen:
            continue
        seen.add(key)
        n = sum(1 for f in found if (f[0], f[1], f[2]) == key)
        print('  %-20s %s / %s : %d faces, écart %.1f mm, vers (%.2f %.2f %.2f)' % (kind, a, b, n, dist * 1000, *c))


if __name__ == '__main__':
    main()
