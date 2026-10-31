# Écrit les opérations de chaque tour dans le fichier d'allure (les palettes restent), et les duos (duos.py, 31 octobre : leurs
# palettes et leur composition). Depuis « Project Archery/Assets » :
#   py -I ../Tools/allure_tours/conception.py _Project/Editor/TowerDefenseLook.json [Tour,Tour]
import json, os, sys, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import duos as duo_design

JSON = sys.argv[1]

def r(v):
    return [round(float(x), 3) for x in v]

def model(parent, name, model, at, width=None, height=None, rot=(0, 0, 0), palette=None):
    op = {'op': 'model', 'parent': parent, 'name': name, 'model': model, 'at': r(at), 'rot': r(rot)}
    if width is not None:
        op['width'] = round(width, 3)
    if height is not None:
        op['height'] = round(height, 3)
    if palette:
        op['palette'] = palette
    return op

def prim(kind, parent, name, at, size, role=None, rot=(0, 0, 0), shade=1.0, mat=None):
    op = {'op': kind, 'parent': parent, 'name': name, 'at': r(at), 'size': r(size), 'rot': r(rot)}
    if role:
        op['role'] = role
    if shade != 1.0:
        op['shade'] = shade
    if mat:
        op['mat'] = mat
    return op

def box(*a, **k):
    return prim('box', *a, **k)

def cyl(*a, **k):
    return prim('cylinder', *a, **k)

def sph(*a, **k):
    return prim('sphere', *a, **k)

def empty(parent, name, at, rot=(0, 0, 0)):
    return {'op': 'empty', 'parent': parent, 'name': name, 'at': r(at), 'rot': r(rot)}

def destroy(path):
    return {'op': 'destroy', 'path': path}

def setp(path, **k):
    op = {'op': 'set', 'path': path}
    for key, v in k.items():
        op[key] = r(v) if isinstance(v, (list, tuple)) else v
    return op

def field(name, paths):
    return {'op': 'field', 'field': name, 'paths': paths}

# ---------------------------------------------------------------- la barricade
def barricade():
    ops = []
    # Niveau 4 : la herse du château était de profil (le modèle de Kenney s'étend le long de son axe Z) ; les glissières montent
    # jusqu'en haut de la herse, un linteau les relie.
    ops.append(setp('Niveau 4/Herse/Herse du château', rot=[0, 90, 0]))
    ops.append(setp('Niveau 4/Glissière', y=3.0, sy=6.0))
    ops.append(box('Niveau 4', 'Linteau', [0, 6.1, 0.55], [4.4, 0.24, 0.26], role='bois'))
    # Niveau 5 : la forteresse avalait tout ; à la place, deux tours fines de chaque côté de la herse, une baliste en haut.
    for name in ('Archère', 'Tour de guet', 'Rempart', 'Bannière'):
        ops.append(destroy('Niveau 5/' + name))
    for side, x in (('gauche', -2.6), ('droite', 2.6)):
        t = 'Niveau 5/Tour ' + side
        w = 0.9
        ops.append(empty('Niveau 5', 'Tour ' + side, [x, 0, 0.6]))
        ops.append(model(t, 'Socle', 'castle/tower-square-base', [0, 0, 0], width=w))
        y = w * 1.01
        for i, piece in enumerate(('tower-square-mid', 'tower-square-mid-windows', 'tower-square-mid', 'tower-square-mid-windows', 'tower-square-mid')):
            ops.append(model(t, 'Étage', 'castle/' + piece, [0, y, 0], width=w * 0.93))
            y += w * 1.01
        ops.append(model(t, 'Créneaux', 'castle/tower-square-top', [0, y, 0], width=w))
        # La bannière pend du côté du joueur (derrière la barricade, -Z).
        ops.append(model(t, 'Bannière', 'castle/flag-banner-long', [0, y - 2.3, -w * 0.5 - 0.03], height=1.8, rot=[0, 90, 0]))
        ops.append(empty(t, 'Tourelle', [0, y + 0.08, 0]))
        ops.append(model(t + '/Tourelle', 'Petite baliste', 'td/weapon-ballista', [0, 0, 0], width=0.7))
    ops.append(field('m_WatchTowers', ['Niveau 5/Tour gauche/Tourelle', 'Niveau 5/Tour droite/Tourelle']))
    return ops

def cord(parent, name, a, b, d=0.035, role='corde'):
    """Une corde (cylindre fin) de a à b, dans le plan YZ ou quelconque."""
    import numpy as np
    a = np.array(a, float); b = np.array(b, float)
    v = b - a
    L = np.linalg.norm(v)
    u = v / L
    # Rotation qui amène +Y sur u : d'abord X (inclinaison vers +Z), puis Y (cap).
    pitch = math.degrees(math.acos(max(-1, min(1, u[1]))))
    yaw = math.degrees(math.atan2(u[0], u[2])) if abs(u[1]) < 0.9999 else 0
    return cyl(parent, name, (a + b) / 2, [d, L, d], role=role, rot=[pitch, yaw, 0])

def follow(path, target, yaw_only=False, offset=(0, 0, 0)):
    return {'op': 'follow', 'path': path, 'target': target, 'yawOnly': yaw_only, 'offset': r(offset)}

def root_empty(parent, name, at, rot=(0, 0, 0)):
    op = empty(parent, name, at, rot)
    op['root'] = True
    return op

# ---------------------------------------------------------------- la baliste
PIVOT = 1.26     # hauteur du pivot de la tourelle (weapon-ballista, échelle 2,4)
SECOND = 1.62    # le deuxième arc, au-dessus du premier

def harpoon(parent, y_off):
    """Un harpon sur la pointe du carreau, la corde et le treuil derrière (repère de la tourelle)."""
    ops = []
    tip = [0, 0.98 + 0.0, 1.15]
    ops.append(box(parent, 'Hampe', [0, 0.98, 1.08], [0.07, 0.07, 0.5], role='metal', shade=0.6))
    ops.append(box(parent, 'Pointe de harpon', [0, 0.98, 1.42], [0.22, 0.22, 0.4], role='metal', shade=0.5, rot=[0, 0, 45]))
    for side in (-1, 1):
        ops.append(box(parent, 'Crochet', [0.14 * side, 0.98, 1.26], [0.05, 0.05, 0.34], role='metal', shade=0.5, rot=[0, -32 * side, 0]))
    ops.append(cyl(parent, 'Treuil', [0, 0.6, -0.86], [0.4, 0.86, 0.4], role='bois', shade=0.8, rot=[0, 0, 90]))
    ops.append(cyl(parent, 'Corde enroulée', [0, 0.6, -0.86], [0.47, 0.56, 0.47], role='corde', rot=[0, 0, 90]))
    ops.append(box(parent, 'Manivelle', [0.5, 0.6, -0.86], [0.06, 0.4, 0.06], role='metal', shade=0.6))
    ops.append(cord(parent, 'Corde', [0, 0.82, -0.86], [0, 1.0, 1.2]))
    return ops

def baliste_planchers():
    """Le plancher sous chaque arc, 6 mm plus bas que son pied (le dessus du plancher et le pied de l'arc étaient dans le
    même plan : ils scintillaient). Des places absolues : rejouées, elles ne décalent rien."""
    return [setp('Plancher', at=[0, 1.194, 0]),
            setp('Niveau 3 (drapeau)/Charpente/Plancher', at=[0, SECOND - 0.046, 0])]

def baliste():
    ops = []
    t = 'weapon-ballista'
    ops += baliste_planchers()
    # Niveau 3 : la charpente du deuxième arc ne suit que le cap (elle ne bascule plus quand la tourelle vise en haut ou en bas),
    # ses poteaux hors des bras du premier ; le deuxième arc suit la tourelle en entier, plus haut.
    ops.append(destroy('Niveau 3 (drapeau)/Scorpion'))
    ops.append(root_empty('Niveau 3 (drapeau)', 'Charpente', [0, PIVOT, 0]))
    ops.append(follow('Niveau 3 (drapeau)/Charpente', t, yaw_only=True))
    c = 'Niveau 3 (drapeau)/Charpente'
    for x in (-0.93, 0.93):
        for z in (-0.62, 0.62):
            ops.append(box(c, 'Montant', [x, SECOND / 2 - 0.04, z], [0.1, SECOND - 0.08, 0.1], role='bois'))
        ops.append(box(c, 'Traverse', [x, SECOND * 0.55, 0], [0.08, 0.08, 1.3], role='bois', shade=0.85))
    ops.append(box(c, 'Plancher', [0, SECOND - 0.046, 0], [2.0, 0.08, 1.4], role='bois', shade=0.85))
    ops.append(root_empty('Niveau 3 (drapeau)', 'Deuxième arc', [0, PIVOT + SECOND, 0]))
    ops.append(follow('Niveau 3 (drapeau)/Deuxième arc', t, offset=[0, SECOND, 0]))
    ops.append({'op': 'model', 'parent': 'Niveau 3 (drapeau)/Deuxième arc', 'name': 'Deuxième baliste', 'model': 'td/weapon-ballista',
                'at': [0, 0, 0], 'rot': [0, 0, 0], 'scale': 2.4})
    # Niveau 4 : de vrais harpons, sur les deux arcs.
    ops.append(destroy('Niveau 4/Harpons'))
    for name, off in (('Harpon', 0.0), ('Harpon du haut', SECOND)):
        ops.append(root_empty('Niveau 4', name, [0, PIVOT + off, 0]))
        ops.append(follow('Niveau 4/' + name, t, offset=[0, off, 0]))
        ops += harpoon('Niveau 4/' + name, off)
    # Niveau 5 : la plate-forme du titan ne tourne plus (piliers au sol, hors du bâti), la baliste de siège tourne seulement sur
    # elle-même, face à l'avant (le modèle de Kenney tire vers -X).
    ops.append(destroy('Niveau 5/Titan'))
    top = 4.2
    for x in (-1.34, 1.34):
        for z in (-1.34, 1.34):
            ops.append(box('Niveau 5', 'Pilier', [x, top / 2, z], [0.26, top, 0.26], role='pierre', shade=0.9))
    ops.append(box('Niveau 5', 'Plate-forme', [0, top + 0.11, 0], [3.0, 0.22, 3.0], role='pierre'))
    for z in (-1.5, 1.5):
        ops.append(box('Niveau 5', 'Liseré', [0, top + 0.12, z], [3.04, 0.05, 0.05], mat='Lueur_bleue'))
    for x in (-1.5, 1.5):
        ops.append(box('Niveau 5', 'Liseré', [x, top + 0.12, 0], [0.05, 0.05, 3.04], mat='Lueur_bleue'))
    ops.append(root_empty('Niveau 5', 'Titan', [0, top + 0.22, 0]))
    ops.append(follow('Niveau 5/Titan', t, yaw_only=True, offset=[0, top + 0.22 - PIVOT, 0]))
    ops.append(model('Niveau 5/Titan', 'Baliste titan', 'castle/siege-ballista', [0, 0, 0], width=3.4, rot=[0, 90, 0]))
    ops.append(empty('Niveau 5/Titan', 'Bouche du titan', [0, 1.02, 1.85]))
    ops.append(field('m_TitanMuzzle', ['Niveau 5/Titan/Bouche du titan']))
    return ops

# ---------------------------------------------------------------- le mortier
BARREL = 'construction/Ultimate Fantasy RTS by Quaternius/FBX/Barrel.fbx'

def pile(parent, name, at, ball=0.2):
    """Une pyramide de boulets (3 au sol, 1 dessus)."""
    ops = []
    x, y, z = at
    for i in range(3):
        a = math.radians(90 + 120 * i)
        ops.append(model(parent, name, 'td/weapon-ammo-cannonball', [x + 0.58 * ball * math.cos(a), y, z + 0.58 * ball * math.sin(a)], width=ball))
    ops.append(model(parent, name, 'td/weapon-ammo-cannonball', [x, y + 0.78 * ball, z], width=ball))
    return ops

def mortier():
    ops = []
    W = 2.4
    base = 0.45 * W
    floor = base + 0.25 * W
    rim = base + 0.38 * W
    # Le socle : un bastion bas à créneaux (il faisait plus de 5 m : « beaucoup trop grand et imposant »).
    ops.append(destroy('Socle'))
    ops.append(model('', 'Socle', 'castle/tower-square-base-border', [0, 0, 0], width=W))
    ops.append(model('', 'Créneaux', 'castle/tower-square-top-color', [0, base, 0], width=W))
    # La tourelle, plus petite, sur le plancher du bastion ; le deuxième tube (niveau 3) serré contre le premier.
    ops.append(setp('Affût', at=[0, floor + 0.36, 0], scale=[1.1, 1.1, 1.1]))
    ops.append(setp('Affût/Niveau 3/Tube', at=[0.45, 0, -0.1]))
    # Niveau 2 : deux piles de boulets au pied, côté joueur.
    ops.append(destroy('Niveau 2/Boulet'))
    ops.append(setp('Niveau 2', at=[0, 0, 0], scale=[1, 1, 1]))
    for x in (-0.7, 0.7):
        ops += pile('Niveau 2', 'Boulet', [x, 0, -W / 2 - 0.2])
    # Niveau 4 : les tonneaux de goudron au pied, sur les côtés, une flaque noire.
    for name in ('Flaque', 'Charrette de goudron', 'Tonneau de goudron'):
        ops.append(destroy('Niveau 4/' + name))
    for x in (-1, 1):
        for z in (-0.45, 0.45):
            ops.append(model('Niveau 4', 'Tonneau de goudron', BARREL, [x * (W / 2 + 0.17), 0, z], height=0.62))
        ops.append(cyl('Niveau 4', 'Flaque', [x * (W / 2 + 0.12), 0.012, 0], [0.55, 0.02, 1.4], mat='Goudron_mat'))
    # Niveau 5 : la batterie, quatre petits mortiers sur les coins des créneaux (hors de la course des tubes qui tournent),
    # deux bannières sur les faces.
    for name in ('Batterie', 'Obus', 'Bannière'):
        ops.append(destroy('Niveau 5/' + name))
    for x in (-1, 1):
        for z in (-1, 1):
            yaw = math.degrees(math.atan2(x, z))
            ops.append(model('Niveau 5', 'Petit mortier', 'td/weapon-cannon', [x * (W / 2 - 0.22), rim - 0.02, z * (W / 2 - 0.22)],
                             width=0.5, rot=[-35, yaw, 0]))
    for z in (-1, 1):
        ops.append(model('Niveau 5', 'Bannière', 'castle/flag-banner-short', [0, rim - 0.95, z * (W / 2 + 0.03)], height=0.9, rot=[0, 90, 0]))
    return ops

# ---------------------------------------------------------------- le phare
def phare():
    ops = []
    # Un vrai phare (Kenney, comme le reste de la carte) à la place de la tour de guet de Quaternius : socle, fût rond à bandes
    # blanches et rouges, galerie, lanterne sous un toit conique rouge.
    ops.append(destroy('Tour de guet'))
    y = 0.0
    ops.append(model('', 'Socle', 'castle/tower-square-base-border', [0, 0, 0], width=1.8))
    y = 0.45 * 1.8
    for w, pal in ((1.3, 'Phare_rouge'), (1.16, None), (1.02, 'Phare_rouge')):
        # Chaque pièce posée sur la précédente, enfoncée de 6 mm : leurs faces ne sont jamais dans le même plan (scintillement).
        ops.append(model('', 'Fût', 'castle/tower-base', [0, y - 0.006, 0], width=w, palette=pal))
        y += 1.31 * w - 0.006
    gallery = y
    ops.append(model('', 'Galerie', 'castle/tower-top', [0, y - 0.006, 0], width=1.4))
    lantern = gallery + 0.45
    for i in range(4):
        a = math.radians(45 + 90 * i)
        ops.append(box('', 'Montant de la lanterne', [0.4 * math.cos(a), gallery + 0.355, 0.4 * math.sin(a)], [0.06, 0.61, 0.06], role='metal'))
    roof = gallery + 0.67
    ops.append(model('', 'Toit', 'castle/tower-hexagon-roof', [0, roof, 0], width=1.1))
    top = roof + 0.83 * 1.1 / 0.98
    ops.append(setp('Lanterne', at=[0, lantern, 0]))
    ops.append(setp('Faisceau', at=[0, lantern, 0]))
    # Les deux rayons s'effacent vers le haut de leur cylindre (shader LightBeam) : le pied, lumineux, doit être à la lanterne.
    ops.append(setp('Faisceau/Rayon', rot=[0, 0, -90]))
    ops.append(setp('Faisceau/Niveau 3/Rayon', rot=[0, 0, 90]))
    ops.append(destroy('Faisceau/Niveau 3/Lanternes'))
    # Niveau 2 : le fanion au sommet du toit.
    ops.append(setp('Niveau 2/Mât', at=[0, top + 0.5, 0]))
    ops.append(setp('Niveau 2/Fanion', at=[0.22, top + 0.86, 0]))
    # Niveau 4 : le brasier autour de la lanterne ; niveau 5 : la couronne sur la galerie, le soleil au-dessus du toit.
    ops.append(setp('Niveau 4/Flamme', dy=lantern - 3.88))
    ops.append(setp('Niveau 4/Brasier', dy=lantern - 3.88))
    ops.append(setp('Niveau 5/Pointe de couronne', dy=gallery + 0.32 - 4.43))
    ops.append(setp('Niveau 5/Soleil', at=[0, top + 0.6, 0]))
    ops.append(setp('Niveau 5/Halo', at=[0, top + 0.6, 0]))
    return ops

# ---------------------------------------------------------------- le lance-filet
def roll(parent, name, at, length=0.42, d=0.2, rot=(90, 0, 0)):
    return cyl(parent, name, at, [d, length, d], mat='Filet', rot=rot)

def rod(parent, name, at, height):
    """Un paratonnerre de cuivre (le filet électrique) : la tige, trois bobines, l'éclateur."""
    x, y, z = at
    ops = [cyl(parent, name, [x, y + height / 2, z], [0.07, height, 0.07], role='cuivre')]
    for i in range(3):
        ops.append(cyl(parent, 'Bobine', [x, y + height - 0.55 + i * 0.13, z], [0.22, 0.06, 0.22], role='cuivre', shade=0.85))
    ops.append(sph(parent, 'Éclateur', [x, y + height + 0.08, z], [0.2, 0.2, 0.2], mat='Lueur_cyan'))
    return ops

def filet():
    ops = []
    # Un nid de guetteur sur pilotis (« plus en hauteur, un style à elle ») : il ne ressemble plus à la baliste.
    ops.append(destroy('Base'))
    W = 1.5
    ops.append(model('', 'Pilotis', 'td/wood-structure-high', [0, 0, 0], width=W))
    ops.append(model('', 'Pilotis', 'td/wood-structure-high', [0, W - 0.006, 0], width=W))
    deck = 2 * W
    ops.append(model('', 'Nid', 'castle/tower-hexagon-top-wood', [0, deck, 0], width=1.75))
    floor = deck + 0.06
    # L'échelle, côté joueur (-Z).
    for x in (-0.2, 0.2):
        ops.append(box('', "Montant de l'échelle", [x, deck / 2, -W / 2 - 0.06], [0.06, deck, 0.06], role='bois', shade=0.8))
    for i in range(int(deck / 0.34)):
        ops.append(box('', 'Barreau', [0, 0.3 + i * 0.34, -W / 2 - 0.06], [0.44, 0.04, 0.05], role='bois', shade=0.8))
    ops.append(cyl('', 'Pivot', [0, floor + 0.355, 0], [0.42, 0.71, 0.42], role='bois', shade=0.7))
    # La tourelle : un lance-filet à deux bouches (Kenney) à la place de la petite baliste ; les filets roulés sur ses flancs.
    ops.append(setp('Tourelle', at=[0, floor + 0.72, 0]))
    ops.append(destroy('Tourelle/Petite baliste'))
    ops.append({'op': 'model', 'parent': 'Tourelle', 'name': 'Lance-filet', 'model': 'td/weapon-turret', 'at': [0, 0, 0], 'rot': [0, 0, 0], 'scale': 1.6})
    ops.append({'op': 'field', 'field': 'm_MuzzleOffset', 'vector': [0, 0.61, 0.56]})
    ops.append(destroy('Tourelle/Filet roulé'))
    ops.append(roll('Tourelle', 'Filet roulé', [0.5, 0.52, -0.08]))
    ops.append(destroy('Tourelle/Niveau 2/Filet roulé'))
    ops.append(roll('Tourelle/Niveau 2', 'Filet roulé', [-0.5, 0.52, -0.08]))
    # Niveau 3, le barillet : six filets roulés autour d'un axe, sur la tourelle.
    for name in ('Mât', 'Fanion', 'Petite baliste'):
        ops.append(destroy('Tourelle/Niveau 3/' + name))
    ops.append(cyl('Tourelle/Niveau 3', 'Axe du barillet', [0, 1.04, -0.05], [0.08, 0.62, 0.08], role='metal', rot=[90, 0, 0]))
    for i in range(6):
        a = math.radians(60 * i)
        ops.append(roll('Tourelle/Niveau 3', 'Filet du barillet', [0.17 * math.cos(a), 1.04 + 0.17 * math.sin(a), -0.05], length=0.5, d=0.15))
    for z in (-0.3, 0.2):
        ops.append(cyl('Tourelle/Niveau 3', 'Flasque', [0, 1.04, z], [0.5, 0.04, 0.5], role='bois', shade=0.75, rot=[90, 0, 0]))
    # Niveau 4, le filet électrique : un paratonnerre de cuivre le long d'un pilier (plus de groupe électrogène).
    for name in ('Bobine', 'Éclateur', 'Générateur'):
        ops.append(destroy('Niveau 4/' + name))
    ops += rod('Niveau 4', 'Paratonnerre', [W / 2 + 0.06, 1.2, W / 2 + 0.06], deck + 1.4 - 1.2)
    # Niveau 5, la toile géante : deux antennes aux autres coins, et le grand canon à filet au-dessus de la tourelle.
    for name in ('Antenne', 'Éclateur', 'Canon à filet'):
        ops.append(destroy('Niveau 5/' + name))
    for x, z in ((-1, 1), (1, -1)):
        ops += rod('Niveau 5', 'Antenne', [x * (W / 2 + 0.06), 1.6, z * (W / 2 + 0.06)], deck + 1.8 - 1.6)
    ops.append(root_empty('Niveau 5', 'Canon à filet', [0, floor + 0.72 + 1.32, 0]))
    ops.append(follow('Niveau 5/Canon à filet', 'Tourelle', offset=[0, 1.32, 0]))
    ops.append({'op': 'model', 'parent': 'Niveau 5/Canon à filet', 'name': 'Canon à filet', 'model': 'td/weapon-cannon', 'at': [0, 0, 0], 'rot': [0, 0, 0], 'scale': 1.25})
    ops.append(roll('Niveau 5/Canon à filet', 'Filet enroulé', [0, 0.45, -0.42], length=0.62, d=0.3, rot=[0, 0, 90]))
    return ops

# ---------------------------------------------------------------- la tour-aimant
def aimant():
    ops = []
    # Un socle carré (il était rond, comme la tour de givre) ; l'aimant posé dessus, un peu plus haut.
    ops.append(destroy('Base'))
    W = 1.9
    ops.append(model('', 'Socle', 'td/tower-square-bottom-c', [0, 0, 0], width=W))
    top = 0.5 * W / 1.0
    ops.append(cyl('', 'Colonne', [0, (top + 1.02) / 2, 0], [0.34, 1.02 - top + 0.02, 0.34], role='metal', shade=0.55))
    # Niveau 3, l'électro-aimant : deux condensateurs de cuivre à lueur violette, reliés aux branches (plus de groupe électrogène
    # ni de couronne de cristaux, celle de la tour de givre).
    for name in ('Générateur', 'Cristaux'):
        ops.append(destroy('Niveau 3/' + name))
    for x in (-0.62, 0.62):
        ops.append(cyl('Niveau 3', 'Condensateur', [x, top + 0.426, -0.55], [0.26, 0.84, 0.26], role='cuivre'))
        for i in range(3):
            ops.append(cyl('Niveau 3', 'Bague', [x, top + 0.2 + i * 0.25, -0.55], [0.3, 0.05, 0.3], role='metal', shade=0.55))
        ops.append(sph('Niveau 3', 'Lueur', [x, top + 0.94, -0.55], [0.24, 0.24, 0.24], mat='Lueur_violette'))
        ops.append(cord('Niveau 3', 'Câble', [x, top + 0.9, -0.55], [0.31 * (1 if x > 0 else -1), 1.5, -0.12], d=0.04, role='metal'))
    return ops

# ---------------------------------------------------------------- les pièges à piques
def frame(parent, cx, w, d, y=0.0, h=0.16, t=0.12, inner=None):
    """Un cadre de poutres sombres ferré aux coins, autour d'un plateau de w x d centré en x = cx. inner : le côté (x) déjà
    fermé par le cadre voisin (les rallonges du niveau 3), sans poutre ni ferrure, pour ne pas doubler celles du plateau."""
    ops = []
    x0, x1 = cx - w / 2, cx + w / 2
    a0 = x0 + t / 2 if inner is not None and abs(inner - x0) < 1e-6 else x0 - t / 2
    a1 = x1 - t / 2 if inner is not None and abs(inner - x1) < 1e-6 else x1 + t / 2
    for z in (-d / 2, d / 2):
        ops.append(box(parent, 'Poutre', [(a0 + a1) / 2, y + h / 2, z], [a1 - a0, h, t], role='bois', shade=0.7))
    for x in (x0, x1):
        if inner is not None and abs(inner - x) < 1e-6:
            continue
        ops.append(box(parent, 'Poutre', [x, y + h / 2, 0], [t, h, d - t], role='bois', shade=0.7))
        for z in (-d / 2, d / 2):
            ops.append(box(parent, 'Ferrure', [x, y + h / 2 + 0.01, z], [t + 0.05, h + 0.04, t + 0.05], role='metal'))
    return ops

def pieges():
    ops = []
    S = 2.6
    # Niveau 1 : le plateau dans un cadre de poutres ferrées ; les piques prennent le métal de la tour.
    ops += frame('', 0, S, S)
    ops.append({'op': 'field', 'field': 'm_SpikeMaterial', 'role': 'metal'})
    # Niveau 3 : les deux rallonges du champ de pieux, encadrées de même.
    for cx in (-S * 0.75, S * 0.75):
        ops += frame('Niveau 3/Champ de pieux', cx, S / 2, S, inner=(S / 2 if cx > 0 else -S / 2))
    # Niveau 5 : la mâchoire, deux rangées de crocs de fer sur les bords (à la place des blocs de pierre et des runes).
    ops.append(destroy('Niveau 5/Croc'))
    ops.append(destroy('Niveau 5/Rune'))
    for side in (-1, 1):
        x = side * (S + 0.08)
        ops.append(cyl('Niveau 5', 'Charnière', [x, 0.14, 0], [0.16, S + 0.2, 0.16], role='metal', rot=[90, 0, 0]))
        for i in range(6):
            z = -S / 2 + 0.22 + i * (S - 0.44) / 5
            ops.append(box('Niveau 5', 'Croc', [x - side * 0.08, 0.42, z], [0.16, 0.6, 0.16], role='metal', shade=0.75,
                           rot=[0, 45, -side * 22]))
            ops.append(box('Niveau 5', 'Pointe rouillée', [x - side * 0.2, 0.72, z], [0.09, 0.14, 0.09], role='accent',
                           rot=[0, 45, -side * 22]))
    return ops

# ---------------------------------------------------------------- la tour de givre, la bulle
CRYSTALS = [1, 1.08, 1.16, 1.24, 1.35]

def givre():
    ops = []
    # Les pièces empilées de Kenney, chacune enfoncée de 6 mm dans la précédente (faces confondues : scintillement).
    ops.append(setp('tower-round-top-a', dy=-0.006))
    ops.append(setp('tower-round-crystals', dy=-0.012))
    # Les cristaux grossissaient jusqu'à x1,7 et avalaient le pic de glace (niveau 3) et la couronne (niveau 5) : x1,35 au plus ;
    # le pic flotte au-dessus d'eux, la couronne au-dessus du pic.
    ops.append({'op': 'field', 'field': 'm_CrystalScalePerLevel', 'floats': CRYSTALS, 'ifStill': [1, 1.15, 1.3, 1.45, 1.7]})
    top = 2.64 - 0.012 + 0.817 * 2.4 * CRYSTALS[4]
    ops.append(setp('Niveau 3/Pic de glace', dy=top + 0.25 - 4.6))
    ops.append(setp('Niveau 5/Éclat', dy=0.95))
    ops.append(setp('Niveau 5/Gangue', dy=0.95))
    ops.append(setp("Niveau 5/Cœur de l'hiver", dy=0.95))
    return ops

def bulle():
    ops = []
    # La fontaine du niveau 4 (eau bleu-vert, pas la couleur de la tour) posée 6 mm plus haut : les pieds des rochers et du
    # cristal sont sous son fond.
    ops.append(setp('Niveau 4/Fontaine', dy=0.006))
    ops.append({'op': 'paint', 'path': 'Niveau 4/Fontaine', 'palette': 'Bulle_fontaine'})
    return ops

TOWERS = {'Givre': givre, 'Bulle': bulle, 'Barricade': barricade, 'Baliste': baliste, 'Mortier': mortier, 'Phare': phare, 'Filet': filet, 'Aimant': aimant,
          'Pieges': pieges}

MARK = 'Allure du 30 octobre'

# La marque que le prefab doit déjà porter (les retouches précédentes de TowerDefenseEvolutionsBaker).
REQUIRES = {'Barricade': 'Retouche 2 du 29 octobre', 'Baliste': 'Retouche 2 du 29 octobre', 'Mortier': 'Retouche 2 du 29 octobre',
            'Filet': 'Retouche 2 du 29 octobre', 'Aimant': 'Retouche 2 du 29 octobre', 'Bulle': 'Retouche 2 du 29 octobre',
            'Phare': 'Retouche 2 du 29 octobre', 'Pieges': 'Retouche du 29 octobre', 'Givre': 'Retouche du 29 octobre',
            'Tremplin': 'Niveau 5'}

# Les passes suivantes, pour les prefabs déjà passés (leur marque, celle qu'elles demandent, leurs opérations).
PASSES = [('Baliste', 'Allure 2 du 30 octobre', 'Allure du 30 octobre', baliste_planchers)]

look = json.load(open(JSON, encoding='utf-8'))
only = sys.argv[2].split(',') if len(sys.argv) > 2 else list(TOWERS)
old = {(t['prefab'], t.get('mark', MARK)): t for t in look['towers']}
towers = []
for name in ('Barricade', 'Pieges', 'Baliste', 'Mortier', 'Givre', 'Filet', 'Aimant', 'Bulle', 'Phare', 'Tremplin'):
    t = old.get((name, MARK), {'prefab': name, 'ops': []})
    if name in TOWERS and name in only:
        t['ops'] = TOWERS[name]()
    t['mark'] = MARK
    t['requires'] = REQUIRES[name]
    towers.append(t)
for name, mark, requires, fn in PASSES:
    towers.append({'prefab': name, 'mark': mark, 'requires': requires, 'ops': fn()})
look['towers'] = towers
for key in [k for k in look['palettes'] if k.startswith('Duo_')]:
    del look['palettes'][key]
look['palettes'].update(duo_design.PALETTES)
look['duos'] = duo_design.duos()
def dump(look):
    # Une opération par ligne : le fichier se lit (et se compare) facilement.
    lines = ['{', ' "palettes": {']
    names = list(look['palettes'])
    for i, k in enumerate(names):
        lines.append('  ' + json.dumps(k, ensure_ascii=False) + ': ' + json.dumps(look['palettes'][k], ensure_ascii=False) + (',' if i < len(names) - 1 else ''))
    lines.append(' },')
    lines.append(' "towers": [')
    for i, t in enumerate(look['towers']):
        lines.append('  { "prefab": ' + json.dumps(t['prefab']) + ', "mark": ' + json.dumps(t['mark'], ensure_ascii=False) + ', "requires": ' +
                     json.dumps(t['requires'], ensure_ascii=False) + ', "ops": [')
        for j, op in enumerate(t.get('ops', [])):
            lines.append('   ' + json.dumps(op, ensure_ascii=False) + (',' if j < len(t['ops']) - 1 else ''))
        lines.append('  ] }' + (',' if i < len(look['towers']) - 1 else ''))
    lines.append(' ],')
    # Les duos : leurs réglages sur une ligne, puis une opération par ligne.
    lines.append(' "duos": [')
    for i, d in enumerate(look.get('duos', [])):
        head = {k: v for k, v in d.items() if k != 'ops'}
        lines.append('  { ' + json.dumps(head, ensure_ascii=False)[1:-1] + ', "ops": [')
        for j, op in enumerate(d.get('ops', [])):
            lines.append('   ' + json.dumps(op, ensure_ascii=False) + (',' if j < len(d['ops']) - 1 else ''))
        lines.append('  ] }' + (',' if i < len(look['duos']) - 1 else ''))
    lines.append(' ]')
    lines.append('}')
    return '\n'.join(lines) + '\n'

with open(JSON, 'w', encoding='utf-8', newline='\n') as f:
    f.write(dump(look))
print('ok', only)
