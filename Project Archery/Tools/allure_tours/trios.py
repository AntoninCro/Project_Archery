# Les trios du tower defense (1er novembre ; Docs/Refonte_TowerDefense.md, 12.19) : chacun est une tour nouvelle (une TowerTrio),
# née de la fusion d'un duo et d'une troisième tour. conception.py écrit ici leur partie du fichier d'allure (« trios ») ; le baker
# (Editor/TowerDefenseLookBaker) en fait le prefab (Prefabs/Structures/Duos), la définition (Data/Duos : ses 3 niveaux, 4 plans
# chacun) et l'entrée de la liste des trios du catalogue (Resources/TowerDuos). Comme les duos, en plus grand : un trio domine
# un duo, avec ses accents dorés. Règle des faces : une pièce posée sur une autre y est enfoncée de 6 mm.
import math
from duos import box, cyl, sph, empty, field, fa_rocket, GOLD_LIGHT

PALETTES = {
    'Trio_Enclume': {'pierre': '#8A857E', 'bois': '#6B4429', 'metal': '#3A3C42', 'bronze': '#A0703A', 'braise': '#FF7A2A',
                     'accent': GOLD_LIGHT},
    'Trio_Gonfleur': {'bois': '#7A5236', 'cuivre': '#B8733A', 'metal': '#3E4148', 'rouge': '#D8423A', 'blanc': '#F0EBE0',
                      'jaune': '#F2C53A', 'bleu': '#3A86E0', 'vert': '#3FB35A', 'rose': '#E86AAE', 'noir': '#22242A',
                      'accent': GOLD_LIGHT},
    'Trio_Bombarde': {'pierre': '#8E8A84', 'bois': '#6E4A30', 'bronze': '#A8763C', 'cuivre': '#C07A3E', 'fer': '#2E3036',
                      'metal': '#4A4D55', 'rouge': '#C8392E', 'blanc': '#EEE9DE', 'bleu': '#3A6FD0', 'vert': '#3FA65A',
                      'noir': '#1E2026', 'braise': '#FF8A2A', 'accent': GOLD_LIGHT},
    'Trio_Bowling': {'bois': '#8A5A34', 'rouge': '#B8322C', 'blanc': '#F2EEE4', 'glace': '#9FD6EE', 'metal': '#3E4148',
                     'noir': '#1E2026', 'accent': GOLD_LIGHT},
    'Trio_Beffroi': {'pierre': '#9A948A', 'ardoise': '#3E4452', 'laiton': '#B8923A', 'bronze': '#9A6A3A', 'blanc': '#EEE9DE',
                     'noir': '#22242A', 'rouge': '#C8392E', 'lumiere': '#FFF0B0', 'accent': GOLD_LIGHT},
    'Trio_Colisee': {'marbre': '#E9E4D8', 'pierre': '#BDB6A8', 'bronze': '#9A6A3A', 'rouge': '#B8322C', 'rose': '#E07AAE',
                     'accent': GOLD_LIGHT},
    'Trio_Montagnes': {'bois': '#8A5A34', 'rouge': '#B8322C', 'fer': '#2E3036', 'metal': '#4A4D55', 'blanc': '#F0EBE0',
                       'jaune': '#F2C53A', 'bleu': '#3A86E0', 'vert': '#3FB35A', 'pierre': '#8E8A84', 'accent': GOLD_LIGHT},
}

def tie(parent, name, a, b, role='metal', radius=0.03):
    # Un fil (un cylindre) de a à b, dans n'importe quel sens : Quaternion.Euler(x, y, 0) tourne l'axe du cylindre (y) vers
    # (sin x · sin y, cos x, sin x · cos y).
    dx, dy, dz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    length = math.sqrt(dx * dx + dy * dy + dz * dz)
    pitch = math.degrees(math.atan2(math.hypot(dx, dz), dy))
    yaw = math.degrees(math.atan2(dx, dz))
    mid = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2]
    return cyl(parent, name, mid, [radius * 2, length, radius * 2], role=role, rot=[pitch, yaw, 0])

MARK = 'Trio du 1er novembre'

# Les trios retirés : le baker les enlève de la liste des trios du catalogue (leurs fichiers restent). Le Volcan (1er novembre au
# soir, le joueur : « oublie complètement le volcan »), remplacé par la Grande bombarde.
RETIRED = ['Volcan']

# ---------------------------------------------------------------- Enclume : Marteau-pilon + Mortier
# Une forge sur pilotis au-dessus de la voie (x en travers, z le long) : quatre pilotis de part et d'autre de la voie (les ennemis
# passent dessous), la plate-forme à 6,5 m, le foyer et sa cheminée, le mortier en cloche de bronze au milieu, le tas d'enclumes
# cerclées d'or. L'enclume du haut du tas sert de modèle aux tirs (m_AnvilTemplate : son repère en bas, au milieu ; 1,1 m de haut).
EN_LEG_X = 3.9       # les pilotis, de part et d'autre de la voie
EN_LEG_Z = 1.9
EN_DECK = 6.5        # le dessus de la plate-forme
EN_DECK_T = 0.5      # son épaisseur

def en_anvil(parent, name, at, rot=(0, 0, 0), gold=True):
    # Une enclume : le pied, la taille, la table et sa bigorne, un cercle doré ; son repère en bas, au milieu (1,1 m de haut).
    ops = [empty(parent, name, at, rot)]
    a = parent + '/' + name if parent else name
    ops.append(box(a, 'Pied', [0, 0.17, 0], [0.8, 0.34, 0.6], role='metal', shade=0.75))
    ops.append(box(a, 'Taille', [0, 0.34 + 0.18 - 0.006, 0], [0.42, 0.36, 0.34], role='metal', shade=0.85))
    ops.append(box(a, 'Table', [0, 0.70 + 0.2 - 0.012, 0], [1.1, 0.4, 0.5], role='metal'))
    ops.append(sph(a, 'Bigorne', [0.55 + 0.2 - 0.006, 0.95, 0], [0.62, 0.26, 0.26], role='metal'))
    if gold:
        ops.append(box(a, 'Cercle', [0, 0.70 + 0.2 - 0.012, 0], [1.12, 0.1, 0.52], role='accent'))
    return ops

def en_leg(x, z):
    ops = []
    ops.append(box('', 'Socle', [x, 0.35, z], [1.2, 0.7, 1.2], role='pierre', shade=0.85))
    ops.append(box('', 'Plaque', [x, 0.7 + 0.03 - 0.006, z], [1.24, 0.06, 1.24], role='accent'))
    top = EN_DECK - EN_DECK_T + 0.006
    ops.append(box('', 'Pilotis', [x, (0.694 + top) / 2, z], [0.7, top - 0.694, 0.7], role='bois'))
    for y in (2.0, 4.0):
        ops.append(box('', 'Cercle', [x, y, z], [0.76, 0.18, 0.76], role='metal'))
    return ops

def enclume():
    ops = []
    for sx in (-1, 1):
        for sz in (-1, 1):
            ops += en_leg(sx * EN_LEG_X, sz * EN_LEG_Z)
        # Les croix de Saint-André entre deux pilotis d'un même côté (le long de la voie : rien en travers, les ennemis passent).
        for s in (-1, 1):
            ops.append(box('', 'Croisillon', [sx * EN_LEG_X, 3.2, 0], [0.22, 5.0, 0.22], role='bois', shade=0.8, rot=[s * 35, 0, 0]))
    # La plate-forme, bordée d'or ; les poutres dessous.
    ops.append(box('', 'Plancher', [0, EN_DECK - EN_DECK_T / 2, 0], [2 * EN_LEG_X + 1.4, EN_DECK_T, 2 * EN_LEG_Z + 1.4], role='bois', shade=0.9))
    # Les bordures dépassent du plancher de 2 cm (sinon leurs faces se confondent avec les siennes).
    for sz in (-1, 1):
        ops.append(box('', 'Bordure', [0, EN_DECK + 0.08 - 0.006, sz * (EN_LEG_Z + 0.67)], [2 * EN_LEG_X + 1.44, 0.16, 0.1], role='accent'))
    for sx in (-1, 1):
        ops.append(box('', 'Bordure', [sx * (EN_LEG_X + 0.67), EN_DECK + 0.08 - 0.006, 0], [0.1, 0.16, 2 * EN_LEG_Z + 1.2], role='accent'))
        ops.append(box('', 'Poutre', [sx * EN_LEG_X, EN_DECK - EN_DECK_T - 0.2 + 0.006, 0], [0.5, 0.4, 2 * EN_LEG_Z + 1.0], role='bois', shade=0.7))
    deck = EN_DECK - 0.006
    # Le foyer de pierre, sa braise, sa cheminée cerclée d'or.
    fx = -2.6
    ops.append(box('', 'Foyer', [fx, deck + 0.6, 0], [1.8, 1.2, 2.2], role='pierre'))
    ops.append(box('', 'Braise', [fx + 0.6, deck + 0.75, 0], [0.62, 0.5, 1.4], role='braise'))
    ops.append(cyl('', 'Cheminée', [fx - 0.3, deck + 1.2 + 2.2 - 0.012, 0], [0.8, 4.4, 0.8], role='pierre', shade=0.8))
    for y in (deck + 2.4, deck + 3.6):
        ops.append(cyl('', 'Cercle', [fx - 0.3, y, 0], [0.86, 0.14, 0.86], role='accent'))
    # Le mortier en cloche de bronze : un affût, et le fût (m_Barrel) qui recule à chaque tir ; sa bouche (m_Muzzle) en haut.
    mx = 0.6
    ops.append(box('', 'Affût', [mx, deck + 0.3, 0], [1.9, 0.6, 1.9], role='bois', shade=0.75))
    ops.append(box('', 'Ferrure', [mx, deck + 0.6 + 0.04 - 0.006, 0], [1.94, 0.08, 1.94], role='metal'))
    ops.append(empty('', 'Mortier', [mx, deck + 0.68, 0]))
    ops.append(empty('Mortier', 'Fût', [0, 0, 0], [-12, 0, 0]))
    ops.append(cyl('Mortier/Fût', 'Cloche', [0, 0.9 + 0.006, 0], [1.5, 1.8 - 0.012, 1.5], role='bronze'))
    ops.append(cyl('Mortier/Fût', 'Pied', [0, 0.25, 0], [1.8, 0.5, 1.8], role='bronze', shade=0.8))
    ops.append(cyl('Mortier/Fût', 'Gueule', [0, 1.8 + 0.1 - 0.006, 0], [1.8, 0.2, 1.8], role='accent'))
    ops.append(cyl('Mortier/Fût', 'Âme', [0, 2.0 + 0.02 - 0.006, 0], [1.25, 0.04, 1.25], role='metal', shade=0.3))
    ops.append(cyl('Mortier/Fût', 'Cercle', [0, 1.05, 0], [1.56, 0.16, 1.56], role='accent'))
    ops.append(empty('Mortier/Fût', 'Bouche', [0, 2.1, 0]))
    # Le tas d'enclumes, l'une sur l'autre ; celle du haut sert de modèle aux tirs.
    px = 3.0
    ops += en_anvil('', 'Enclume du bas', [px, deck, -0.6], [0, 20, 0])
    ops += en_anvil('', 'Enclume du bas 2', [px, deck, 0.7], [0, -10, 0])
    ops += en_anvil('', 'Enclume du haut', [px, deck + 1.1 - 0.012, 0.05], [0, 80, 0])
    # Niveau 2 : le soufflet de la forge, deux mâts à fanions dorés.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    ops.append(box('Niveau 2', 'Soufflet', [fx + 0.2, deck + 0.35, -1.6], [1.2, 0.5, 0.8], role='bois', shade=0.85))
    ops.append(box('Niveau 2', 'Cuir', [fx + 0.2, deck + 0.6 + 0.12 - 0.012, -1.6], [1.1, 0.24, 0.7], role='bronze', shade=0.6))
    for sx in (-1, 1):
        ops.append(cyl('Niveau 2', 'Mât', [sx * (EN_LEG_X + 0.3), deck + 1.5, EN_LEG_Z + 0.3], [0.14, 3.0, 0.14], role='bois'))
        ops.append(box('Niveau 2', 'Fanion', [sx * (EN_LEG_X + 0.3), deck + 2.5, EN_LEG_Z + 0.95], [0.05, 0.8, 1.2], role='accent'))
    # Niveau 3, la pluie d'enclumes : un portique au-dessus de la forge, d'où pend une grande enclume dorée à sa chaîne.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    gy = deck + 7.0
    for sx in (-1, 1):
        ops.append(box('Niveau 3', 'Montant', [sx * (EN_LEG_X - 0.2), deck + 3.5, 0], [0.5, 7.0, 0.5], role='bois', shade=0.8))
        ops.append(box('Niveau 3', 'Cercle', [sx * (EN_LEG_X - 0.2), deck + 5.0, 0], [0.56, 0.18, 0.56], role='accent'))
    ops.append(box('Niveau 3', 'Traverse', [0, gy + 0.3 - 0.006, 0], [2 * EN_LEG_X + 0.4, 0.6, 0.7], role='bois', shade=0.9))
    ops.append(cyl('Niveau 3', 'Chaîne', [1.6, gy - 0.6 + 0.006, 0], [0.14, 1.2, 0.14], role='metal', shade=0.6))
    ops += en_anvil('Niveau 3', 'Grande enclume', [1.6, gy - 1.2 - 2.2 + 0.012, 0], [0, 0, 0])
    ops.append({'op': 'set', 'path': 'Niveau 3/Grande enclume', 'scale': [2.0, 2.0, 2.0]})
    for sx in (-1, 1):
        for sz in (-1, 1):
            ops.append(cyl('Niveau 3', 'Pinacle', [sx * (EN_LEG_X + 0.3), deck + 0.6, sz * (EN_LEG_Z + 0.3)], [0.3, 1.2, 0.3], role='accent'))
    ops.append(field('m_Muzzle', ['Mortier/Fût/Bouche']))
    ops.append(field('m_Barrel', ['Mortier/Fût']))
    ops.append(field('m_AnvilTemplate', ['Enclume du haut']))
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_FireClip', asset='Assets/kenney_impact-sounds/Audio/impactPlate_heavy_002.ogg'))
    ops.append(field('m_ClangClip', asset='Assets/kenney_impact-sounds/Audio/impactBell_heavy_001.ogg'))
    ops.append(field('m_FlatClip', asset='Assets/kenney_impact-sounds/Audio/impactSoft_heavy_003.ogg'))
    return ops

# ---------------------------------------------------------------- Gonfleur : Lanceur de baudruches + Baliste
# Un char de foire (x sa longueur, z sa largeur) : quatre roues, une jupe rayée, la grosse pompe de cuivre et son piston
# (m_Piston, qui bat quand elle gonfle), un tuyau rayé jusqu'à la baliste-harpon dorée (m_Ballista, qui se tourne vers sa cible ;
# le bout du harpon, m_Nozzle, d'où partent les tuyaux), des ballons attachés à un mât. Niveau 2 : l'enrouleur du deuxième tuyau,
# deux ballons de plus ; niveau 3 : un ballon de parade géant, retenu par quatre cordes.
GF_DECK = 1.2        # le dessus du plateau
GF_HALF_X = 2.2
GF_HALF_Z = 1.4

def gf_balloon(parent, name, at, color, size=1.0):
    # Un ballon : sa sphère un peu allongée, son nœud dessous ; renvoie aussi le point du nœud (où attacher le fil).
    ops = [sph(parent, name, at, [size, size * 1.18, size], role=color)]
    knot = [at[0], at[1] - size * 0.59 - 0.06 + 0.012, at[2]]
    ops.append(sph(parent, name + ' (nœud)', knot, [0.14, 0.12, 0.14], role=color, shade=0.8))
    return ops, [knot[0], knot[1] - 0.05, knot[2]]

def gonfleur():
    ops = []
    deck = GF_DECK - 0.006
    # Le plateau et ses roues ; la jupe rayée, rouge et blanc, sur les deux grands côtés.
    ops.append(box('', 'Plateau', [0, GF_DECK - 0.2, 0], [2 * GF_HALF_X, 0.4, 2 * GF_HALF_Z], role='bois'))
    # Le cadre doré autour du plateau : deux longues baguettes, deux courtes entre elles.
    for sz in (-1, 1):
        ops.append(box('', 'Bordure', [0, GF_DECK + 0.05 - 0.006, sz * (GF_HALF_Z - 0.05)], [2 * GF_HALF_X + 0.04, 0.1, 0.14], role='accent'))
    for sx in (-1, 1):
        ops.append(box('', 'Bordure', [sx * (GF_HALF_X - 0.05), GF_DECK + 0.05 - 0.006, 0], [0.14, 0.1, 2 * GF_HALF_Z - 0.3], role='accent'))
    for sx in (-1, 1):
        for sz in (-1, 1):
            ops.append(cyl('', 'Roue', [sx * 1.5, 0.65, sz * (GF_HALF_Z + 0.16)], [1.3, 0.24, 1.3], role='bois', shade=0.7, rot=[90, 0, 0]))
            ops.append(cyl('', 'Moyeu', [sx * 1.5, 0.65, sz * (GF_HALF_Z + 0.3)], [0.42, 0.1, 0.42], role='accent', rot=[90, 0, 0]))
    for sz in (-1, 1):
        for i in range(8):
            x = -GF_HALF_X + 0.275 + i * 0.55
            ops.append(box('', 'Jupe', [x, GF_DECK - 0.45, sz * (GF_HALF_Z + 0.03)], [0.55, 0.5, 0.04],
                           role='rouge' if i % 2 == 0 else 'blanc'))
    # La pompe : un grand cylindre de cuivre cerclé d'or, son dôme, son manomètre ; le piston et sa poignée.
    px = -1.0
    ops.append(cyl('', 'Pompe', [px, deck + 1.7, 0], [1.7, 3.4, 1.7], role='cuivre'))
    for y in (deck + 0.5, deck + 1.7, deck + 2.9):
        ops.append(cyl('', 'Cercle', [px, y, 0], [1.78, 0.14, 1.78], role='accent'))
    ops.append(sph('', 'Dôme', [px, deck + 3.4, 0], [1.7, 0.9, 1.7], role='cuivre', shade=0.9))
    ops.append(cyl('', 'Cadran', [px, deck + 2.3, 0.86], [0.62, 0.06, 0.62], role='accent', rot=[90, 0, 0]))
    ops.append(cyl('', 'Manomètre', [px, deck + 2.3, 0.9], [0.52, 0.04, 0.52], role='blanc', rot=[90, 0, 0]))
    ops.append(box('', 'Aiguille', [px + 0.08, deck + 2.36, 0.925], [0.04, 0.22, 0.02], role='rouge', rot=[0, 0, -30]))
    ops.append(empty('', 'Piston', [px, deck + 3.8, 0]))
    ops.append(cyl('Piston', 'Tige', [0, 0.3, 0], [0.2, 1.6, 0.2], role='metal'))
    ops.append(box('Piston', 'Poignée', [0, 1.1, 0], [1.7, 0.18, 0.18], role='accent'))
    for s in (-1, 1):
        ops.append(sph('Piston', 'Pommeau', [s * 0.9, 1.1, 0], [0.3, 0.3, 0.3], role='rouge'))
    # Le tuyau rayé, de la pompe à la baliste.
    for i in range(6):
        x = px + 0.85 + 0.1 + i * 0.2
        ops.append(cyl('', 'Tuyau', [x, deck + 0.5, 0], [0.26, 0.2, 0.26], role='rouge' if i % 2 == 0 else 'blanc', rot=[0, 0, 90]))
    # La baliste-harpon, qui tourne (vers +z au repos) : son pied, son fût, son arc doré, le harpon et sa pointe.
    bx = 1.3
    ops.append(empty('', 'Baliste', [bx, deck, 0]))
    ops.append(cyl('Baliste', 'Pied', [0, 0.35, 0], [0.9, 0.7, 0.9], role='bois', shade=0.85))
    ops.append(box('Baliste', 'Fût', [0, 0.85, 0.2], [0.42, 0.32, 2.0], role='bois'))
    ops.append(box('Baliste', 'Arc', [0, 0.95, 0.85], [2.4, 0.16, 0.16], role='accent'))
    for s in (-1, 1):
        ops.append(tie('Baliste', 'Corde', [s * 1.18, 0.95, 0.85], [0, 1.02, -0.5], role='blanc', radius=0.02))
    ops.append(cyl('Baliste', 'Harpon', [0, 1.06, 0.45], [0.12, 2.1, 0.12], role='metal', rot=[90, 0, 0]))
    ops.append(sph('Baliste', 'Pointe', [0, 1.06, 1.55], [0.26, 0.26, 0.42], role='rouge'))
    ops.append(empty('Baliste', 'Bout', [0, 1.06, 1.75]))
    # Le mât des ballons, et trois ballons attachés.
    mast = [-1.9, deck + 4.6, -1.05]
    ops.append(cyl('', 'Mât', [mast[0], deck + 2.3, mast[2]], [0.14, 4.6, 0.14], role='bois'))
    ops.append(sph('', 'Boule', [mast[0], mast[1] + 0.1, mast[2]], [0.3, 0.3, 0.3], role='accent'))
    for name, at, color in (('Ballon rouge', [-2.4, 7.4, -0.6], 'rouge'), ('Ballon jaune', [-1.2, 8.0, -1.4], 'jaune'),
                            ('Ballon bleu', [-0.4, 7.1, -0.3], 'bleu')):
        balloon, knot = gf_balloon('', name, at, color)
        ops += balloon
        ops.append(tie('', 'Fil', mast, knot, role='blanc', radius=0.015))
    # Niveau 2 : l'enrouleur du deuxième tuyau, deux ballons de plus.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    ops.append(cyl('Niveau 2', 'Enrouleur', [0.1, deck + 0.55, -0.85], [1.0, 0.5, 1.0], role='rouge', rot=[90, 0, 0]))
    for s in (-1, 1):
        ops.append(cyl('Niveau 2', 'Joue', [0.1, deck + 0.55, -0.85 + s * 0.28], [1.2, 0.06, 1.2], role='accent', rot=[90, 0, 0]))
    for name, at, color in (('Ballon vert', [-2.9, 6.8, -1.7], 'vert'), ('Ballon rose', [-1.6, 8.7, -0.4], 'rose')):
        balloon, knot = gf_balloon('Niveau 2', name, at, color)
        ops += balloon
        ops.append(tie('Niveau 2', 'Fil', mast, knot, role='blanc', radius=0.015))
    # Niveau 3, le défilé : un ballon de parade géant (un gros ennemi gonflé, ses oreilles, ses yeux), sa ceinture et sa couronne
    # dorées, retenu par quatre cordes aux coins du plateau.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    gy = 11.5
    ops.append(sph('Niveau 3', 'Ballon géant', [0.3, gy, 0], [3.4, 3.2, 3.4], role='rose'))
    ops.append(cyl('Niveau 3', 'Ceinture', [0.3, gy - 0.4, 0], [3.3, 0.3, 3.3], role='accent'))
    for s in (-1, 1):
        ops.append(sph('Niveau 3', 'Oreille', [0.3 + s * 1.2, gy + 1.45, 0], [0.9, 1.1, 0.5], role='rose', shade=0.9))
        ops.append(sph('Niveau 3', 'Œil', [0.3 + s * 0.6, gy + 0.5, 1.45], [0.7, 0.7, 0.4], role='blanc'))
        ops.append(sph('Niveau 3', 'Pupille', [0.3 + s * 0.6, gy + 0.5, 1.64], [0.3, 0.3, 0.12], role='noir'))
    ops.append(cyl('Niveau 3', 'Couronne', [0.3, gy + 1.55, 0], [1.2, 0.5, 1.2], role='accent'))
    for sx in (-1, 1):
        for sz in (-1, 1):
            corner = [sx * (GF_HALF_X - 0.15), GF_DECK, sz * (GF_HALF_Z - 0.15)]
            ops.append(sph('Niveau 3', 'Anneau', corner, [0.2, 0.2, 0.2], role='accent'))
            ops.append(tie('Niveau 3', 'Corde', corner, [0.3 + sx * 1.1, gy - 1.2, sz * 1.1], role='blanc', radius=0.03))
    ops.append(field('m_Nozzle', ['Baliste/Bout']))
    ops.append(field('m_Ballista', ['Baliste']))
    ops.append(field('m_Piston', ['Piston']))
    ops.append({'op': 'field', 'field': 'm_HoseMaterial', 'role': 'rouge'})
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_HarpoonClip', asset='Assets/kenney_impact-sounds/Audio/impactWood_heavy_002.ogg'))
    ops.append(field('m_InflateClip', asset='Assets/_Project/Audio/Placeholder/teleport_charge.wav'))
    ops.append(field('m_PopClip', asset='Assets/kenney_impact-sounds/Audio/impactPunch_heavy_004.ogg'))
    return ops

# ---------------------------------------------------------------- Bowling : Avalanche + Barricade
# Une machine de retour de boules géante, en arche au-dessus de la voie (x en travers) : deux tours laquées rouge cerclées d'or,
# leurs étagères de boules de glace ; la poutre, le tableau de score (m_Board : son texte, devant) et deux quilles dorées dessus ;
# sous la poutre, au milieu, la boule suivante dans sa pince (m_BallTemplate : son repère au centre, 1,4 m de diamètre).
# Niveau 2 : deux boules de plus, deux fanions ; niveau 3 : une boule d'or sur le tableau, deux grandes quilles dorées.
BW_TOWER_X = 3.6
BW_TOP = 7.0         # le haut des tours : la poutre y est posée
BW_BALL = 1.4        # le diamètre de la boule du modèle (m_TemplateDiameter)

def bw_pin(parent, name, at, height, body='accent', stripe='rouge'):
    # Une quille : le ventre, le cou et la tête, deux bandes ; son repère en bas, au milieu.
    k = height / 2.0
    ops = [empty(parent, name, at)]
    p = parent + '/' + name if parent else name
    ops.append(sph(p, 'Ventre', [0, 0.45 * k, 0], [0.42 * k, 0.9 * k, 0.42 * k], role=body))
    ops.append(cyl(p, 'Cou', [0, 1.05 * k, 0], [0.2 * k, 0.5 * k, 0.2 * k], role=body))
    ops.append(sph(p, 'Tête', [0, 1.42 * k, 0], [0.3 * k, 0.4 * k, 0.3 * k], role=body))
    for y in (0.98, 1.12):
        ops.append(cyl(p, 'Bande', [0, y * k, 0], [0.215 * k, 0.05 * k, 0.215 * k], role=stripe))
    return ops

def bw_ball(parent, name, at, diameter, role='glace'):
    # Une boule : la sphère, sa ceinture dorée, trois trous noirs en haut, côté joueur (-z).
    ops = [empty(parent, name, at)]
    b = parent + '/' + name if parent else name
    r = diameter / 2
    ops.append(sph(b, 'Sphère', [0, 0, 0], [diameter, diameter, diameter], role=role))
    ops.append(cyl(b, 'Ceinture', [0, 0, 0], [diameter * 1.01, diameter * 0.08, diameter * 1.01], role='accent', rot=[90, 0, 0]))
    for dx, dy in ((-0.18, 0.62), (0.18, 0.62), (0.0, 0.36)):
        ops.append(sph(b, 'Trou', [dx * r, dy * r, -0.75 * r], [0.16 * r * 2, 0.16 * r * 2, 0.12 * r * 2], role='noir'))
    return ops

def bowling():
    ops = []
    for s in (-1, 1):
        x = s * BW_TOWER_X
        ops.append(box('', 'Socle', [x, 0.25, 0], [1.7, 0.5, 2.1], role='bois', shade=0.7))
        ops.append(box('', 'Tour', [x, 0.494 + (BW_TOP - 0.494) / 2, 0], [1.4, BW_TOP - 0.494, 1.8], role='rouge'))
        for y in (1.6, 3.6, 5.6):
            ops.append(box('', 'Cercle', [x, y, 0], [1.46, 0.16, 1.86], role='accent'))
        # L'étagère, côté voie, et ses boules de glace.
        sx = s * (BW_TOWER_X - 0.7 - 0.3 + 0.006)
        ops.append(box('', 'Étagère', [sx, 2.6, 0], [0.6, 0.1, 1.7], role='bois'))
        for i, z in enumerate((-0.45, 0.45)):
            ops += bw_ball('', 'Boule rangée %s%d' % ('g' if s < 0 else 'd', i + 1), [sx, 2.65 + 0.32 - 0.006, z], 0.64)
    # La poutre, bordée d'or, et le tableau de score dessus.
    beam_y = BW_TOP + 0.5 - 0.006
    ops.append(box('', 'Poutre', [0, beam_y, 0], [2 * BW_TOWER_X + 1.4, 1.0, 1.6], role='rouge', shade=0.9))
    for sz in (-1, 1):
        for y in (beam_y - 0.32, beam_y + 0.32):
            ops.append(box('', 'Baguette', [0, y, sz * 0.82], [2 * BW_TOWER_X + 1.44, 0.12, 0.06], role='accent'))
    board_y = BW_TOP + 1.0 - 0.012 + 0.9
    ops.append(box('', 'Cadre', [0, board_y, 0], [3.8, 1.8, 0.24], role='accent'))
    ops.append(box('', 'Tableau', [0, board_y, 0], [3.5, 1.5, 0.34], role='noir'))
    ops.append(empty('', 'Score', [0, board_y, -0.5]))
    for s in (-1, 1):
        ops += bw_pin('', 'Quille ' + ('gauche' if s < 0 else 'droite'), [s * BW_TOWER_X, BW_TOP + 1.0 - 0.012, 0], 2.2)
    # La boule suivante, dans sa pince, sous la poutre.
    ball_y = BW_TOP - 0.006 - 0.7 - BW_BALL / 2
    ops.append(cyl('', 'Pince', [0, BW_TOP - 0.35 + 0.006, 0], [0.24, 0.7, 0.24], role='metal'))
    ops.append(cyl('', 'Bague', [0, BW_TOP - 0.7 + 0.03, 0], [0.5, 0.08, 0.5], role='accent'))
    ops += bw_ball('', 'Boule', [0, ball_y, 0], BW_BALL)
    # Niveau 2 : deux boules de plus sur les étagères, deux fanions sur la poutre.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    for s in (-1, 1):
        sx = s * (BW_TOWER_X - 0.7 - 0.3 + 0.006)
        ops += bw_ball('Niveau 2', 'Boule rangée ' + ('gauche' if s < 0 else 'droite'), [sx, 2.65 + 0.32 - 0.006, 0], 0.64)
        ops.append(cyl('Niveau 2', 'Hampe', [s * 2.6, BW_TOP + 1.0 - 0.012 + 1.2, 0.5], [0.1, 2.4, 0.1], role='bois'))
        ops.append(box('Niveau 2', 'Fanion', [s * 2.6 + s * 0.5, BW_TOP + 1.0 + 1.9, 0.5], [0.9, 0.6, 0.04], role='rouge'))
        ops.append(box('Niveau 2', 'Liseré', [s * 2.6 + s * 0.5, BW_TOP + 1.0 + 1.57, 0.5], [0.94, 0.08, 0.06], role='accent'))
    # Niveau 3, la partie parfaite : une boule d'or sur le tableau, deux grandes quilles dorées à ses côtés.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    ops += bw_ball('Niveau 3', "Boule d'or", [0, board_y + 0.9 - 0.012 + 0.8, 0], 1.6, role='accent')
    for s in (-1, 1):
        ops += bw_pin('Niveau 3', 'Grande quille ' + ('gauche' if s < 0 else 'droite'), [s * 2.3, BW_TOP + 1.0 - 0.012, -0.35], 3.2, body='accent', stripe='rouge')
    ops.append(field('m_BallTemplate', ['Boule']))
    ops.append(field('m_Board', ['Score']))
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_RollClip', asset='Assets/_Project/Audio/Placeholder/slide.wav'))
    ops.append(field('m_StrikeClip', asset='Assets/kenney_impact-sounds/Audio/impactWood_heavy_003.ogg'))
    ops.append(field('m_ShatterClip', asset='Assets/kenney_impact-sounds/Audio/impactGlass_heavy_001.ogg'))
    return ops

# ---------------------------------------------------------------- Grande bombarde : Feu d'artifice + Tremplin
# Le joueur, le 1er novembre au soir : le Volcan est à revoir en entier ; il garde la mécanique du « Réveil » (une jauge qui se
# remplit, ta flèche qui déclenche) et oublie le volcan. Un énorme mortier de feu d'artifice en bronze au bord de la voie, posé
# sur quatre gros ressorts de cuivre (l'héritage du tremplin : tout le corps rebondit au tir ; m_Body, m_Springs). Sur son fût,
# cinq cercles de fer (m_GaugeRings) rougeoient un à un quand il se remplit de poudre ; sa gueule (m_Muzzle) avale tes flèches.
# À côté, sur un poteau, la cible de la mèche (m_FuseTarget : elle se tourne vers toi en jeu ; le disque face à son -Z), reliée
# par un cordon à la lumière de la culasse (m_Touchhole). Trois bombes de feu d'artifice sur l'affût servent de modèles aux tirs
# (m_Bombs : repère en bas, au milieu). Niveau 2 : le baril de poudre et deux fanions ; niveau 3 : la couronne dorée de la gueule
# et deux grandes fusées à deux étages.
BO_SOCLE = 0.494     # le dessus du socle (enfoncé de 6 mm)
BO_SPRING = 0.75     # la hauteur des ressorts
BO_POST_Z = -1.95    # le poteau de la mèche
BO_COLORS = ['rouge', 'bleu', 'vert', 'accent']

def bo_bomb(parent, name, at, role):
    # Une bombe de feu d'artifice : la boule de papier peint, son cercle doré, sa mèche ; repère en bas, au milieu (0,85 m de haut).
    b = parent + '/' + name
    return [empty(parent, name, at),
            sph(b, 'Boule', [0, 0.35, 0], [0.7, 0.7, 0.7], role=role),
            cyl(b, 'Cercle', [0, 0.35, 0], [0.72, 0.1, 0.72], role='accent'),
            cyl(b, 'Mèche', [0, 0.7 - 0.006 + 0.08, 0], [0.06, 0.16, 0.06], role='blanc')]

def bombarde():
    ops = []
    ops.append(cyl('', 'Socle', [0, 0.25 - 0.006, 0], [4.4, 0.5, 4.4], role='pierre'))
    ops.append(cyl('', 'Liseré', [0, 0.4, 0], [4.46, 0.12, 4.46], role='accent'))
    # Les quatre ressorts de cuivre (repère au pied : on les étire en jeu quand le corps rebondit).
    ops.append(empty('', 'Ressorts', [0, BO_SOCLE, 0]))
    for sx in (-1, 1):
        for sz in (-1, 1):
            x, z = sx * 1.1, sz * 1.1
            ops.append(cyl('Ressorts', 'Axe', [x, BO_SPRING / 2, z], [0.3, BO_SPRING + 0.012, 0.3], role='metal'))
            for k in range(5):
                ops.append(cyl('Ressorts', 'Spire', [x, 0.1 + k * 0.14, z], [0.7, 0.07, 0.7], role='cuivre'))
    # Le corps qui rebondit : l'affût, ses ferrures, ses flasques, le fût, les bombes.
    ops.append(empty('', 'Corps', [0, BO_SOCLE + BO_SPRING, 0]))
    ops.append(box('Corps', 'Affût', [0, 0.25 - 0.006, 0], [3.2, 0.5, 3.2], role='bois'))
    for sz in (-1, 1):
        ops.append(box('Corps', 'Ferrure', [0, 0.244, sz * 1.62], [3.24, 0.16, 0.06], role='metal'))
    for sx in (-1, 1):
        ops.append(box('Corps', 'Ferrure', [sx * 1.62, 0.244, 0], [0.06, 0.14, 3.24], role='metal'))
        ops.append(box('Corps', 'Flasque', [sx * 1.44, 0.488 + 0.9, 0], [0.3, 1.8, 1.2], role='bois', shade=0.85))
    # Le fût de bronze (son repère au pied de la culasse) : culasse, tube, la jauge (cinq cercles de fer), bourrelet doré, gueule.
    f = 'Corps/Fût'
    ops.append(empty('Corps', 'Fût', [0, 0.488, 0]))
    ops.append(sph(f, 'Culasse', [0, 1.0, 0], [2.4, 2.0, 2.4], role='bronze'))
    ops.append(cyl(f, 'Tube', [0, 2.6, 0], [2.0, 3.2, 2.0], role='bronze', shade=0.95))
    for k in range(5):
        ops.append(cyl(f, 'Jauge %d' % (k + 1), [0, 1.6 + 0.5 * k, 0], [2.08, 0.18, 2.08], role='fer'))
    ops.append(cyl(f, 'Bourrelet', [0, 4.2 - 0.006 + 0.15, 0], [2.5, 0.3, 2.5], role='accent'))
    ops.append(cyl(f, 'Gueule', [0, 4.494 - 0.006 + 0.25, 0], [2.3, 0.5, 2.3], role='bronze'))
    ops.append(cyl(f, 'Âme', [0, 4.988 - 0.006 + 0.02, 0], [1.8, 0.04, 1.8], role='noir'))
    ops.append(empty(f, 'Bouche', [0, 5.0, 0]))
    for sx in (-1, 1):
        ops.append(cyl(f, 'Tourillon', [sx * 1.15, 1.3, 0], [0.5, 0.4, 0.5], role='bronze', shade=0.8, rot=[0, 0, 90]))
    ops.append(sph(f, 'Bouton', [0, 1.3, -1.12], [0.3, 0.3, 0.3], role='accent'))
    ops.append(empty(f, 'Lumière', [0, 1.3, -1.2]))
    # Les bombes sur l'affût : les modèles des tirs.
    ops += bo_bomb('Corps', 'Bombe bleue', [1.15, 0.488, 1.15], 'bleu')
    ops += bo_bomb('Corps', 'Bombe rouge', [-1.15, 0.488, 1.15], 'rouge')
    ops += bo_bomb('Corps', 'Bombe verte', [1.15, 0.488, -1.15], 'vert')
    # La mèche : le poteau, la cible qui se tourne vers toi, le cordon jusqu'à la lumière de la culasse.
    top = BO_SOCLE + 3.2
    ops.append(cyl('', 'Poteau', [0, BO_SOCLE + 1.6, BO_POST_Z], [0.24, 3.2, 0.24], role='bois'))
    ops.append(cyl('', 'Collier', [0, top - 0.1, BO_POST_Z], [0.3, 0.12, 0.3], role='accent'))
    ops.append(empty('', 'Mèche', [0, top - 0.006, BO_POST_Z]))
    ops.append(empty('Mèche', 'Cible', [0, 0.75, 0]))
    ops.append(cyl('Mèche/Cible', 'Disque', [0, 0, 0], [1.4, 0.06, 1.4], role='blanc', rot=[90, 0, 0]))
    ops.append(cyl('Mèche/Cible', 'Cadre', [0, 0, 0.02], [1.5, 0.05, 1.5], role='accent', rot=[90, 0, 0]))
    ops.append(cyl('Mèche/Cible', 'Anneau', [0, 0, -0.04], [1.0, 0.04, 1.0], role='rouge', rot=[90, 0, 0]))
    ops.append(cyl('Mèche/Cible', 'Centre', [0, 0, -0.07], [0.45, 0.04, 0.45], role='accent', rot=[90, 0, 0]))
    touch = [0, BO_SOCLE + BO_SPRING + 0.488 + 1.3, -1.27]
    ops.append(tie('', 'Cordon', [0, top - 0.25, BO_POST_Z + 0.1], touch, role='noir', radius=0.035))
    # Des fusées plantées dans le socle, penchées vers l'extérieur.
    for k, (x, z) in enumerate(((1.95, 0), (-1.95, 0), (0, 1.95))):
        ops += fa_rocket('', 'Fusée %d' % (k + 1), [x, BO_SOCLE + 0.6, z], (x / 1.95, z / 1.95), 12, BO_COLORS[k % 4])
    # Niveau 2 : le baril de poudre cerclé de fer, deux fanions dorés sur les flasques.
    ops.append(empty('Corps', 'Niveau 2', [0, 0, 0]))
    bx = -1.18
    ops.append(cyl('Corps/Niveau 2', 'Baril', [bx, 0.488 + 0.5 - 0.006, bx], [0.8, 1.0, 0.8], role='bois'))
    for y in (0.75, 1.25):
        ops.append(cyl('Corps/Niveau 2', 'Cercle', [bx, y, bx], [0.84, 0.08, 0.84], role='metal'))
    ops.append(cyl('Corps/Niveau 2', 'Couvercle', [bx, 1.482 + 0.014, bx], [0.7, 0.04, 0.7], role='noir'))
    for sx in (-1, 1):
        ops.append(cyl('Corps/Niveau 2', 'Mât', [sx * 1.44, 2.282 + 0.6, 0.45], [0.08, 1.2, 0.08], role='bois'))
        ops.append(box('Corps/Niveau 2', 'Fanion', [sx * 1.44, 2.282 + 1.0, 0.77], [0.04, 0.4, 0.6], role='accent'))
    # Niveau 3 : la couronne dorée de la gueule, ses fleurons, deux grandes fusées à deux étages sur les flasques.
    ops.append(empty('Corps', 'Niveau 3', [0, 0, 0]))
    lip = 0.488 + 4.988
    # Enfoncée de 16 mm dans la gueule (pas 6 : son dessous serait au niveau du dessous de l'âme).
    ops.append(cyl('Corps/Niveau 3', 'Couronne', [0, lip - 0.016 + 0.06, 0], [2.36, 0.12, 2.36], role='accent'))
    for k in range(8):
        a = k * math.pi / 4
        ops.append(sph('Corps/Niveau 3', 'Fleuron', [math.cos(a) * 1.05, lip + 0.2, math.sin(a) * 1.05], [0.32, 0.6, 0.32], role='accent'))
    for sx in (-1, 1):
        ops += fa_rocket('Corps/Niveau 3', 'Grande fusée ' + ('gauche' if sx < 0 else 'droite'), [sx * 1.44, 2.282 + 0.5, -0.3], (sx, 0), 10,
                         'rouge' if sx < 0 else 'bleu', length=1.5, stages=2)
    ops.append(field('m_Body', ['Corps']))
    ops.append(field('m_Springs', ['Ressorts']))
    ops.append(field('m_Muzzle', ['Corps/Fût/Bouche']))
    ops.append(field('m_Touchhole', ['Corps/Fût/Lumière']))
    ops.append(field('m_FuseTarget', ['Mèche/Cible']))
    ops.append(field('m_GaugeRings', [f + '/Jauge %d' % (k + 1) for k in range(5)]))
    ops.append(field('m_Bombs', ['Corps/Bombe bleue', 'Corps/Bombe rouge', 'Corps/Bombe verte']))
    ops.append({'op': 'field', 'field': 'm_GlowMaterial', 'role': 'braise'})
    ops.append(field('m_LevelParts', ['Corps/Niveau 2', 'Corps/Niveau 3']))
    ops.append(field('m_FireClip', asset='Assets/_Project/Audio/Placeholder/explosion.wav'))
    ops.append(field('m_LaunchClip', asset='Assets/507466__danjocross__clean-fast-swoosh.aiff'))
    ops.append(field('m_BombClip', asset='Assets/_Project/Audio/Placeholder/projectile_break.wav'))
    ops.append(field('m_FeedClip', asset='Assets/kenney_impact-sounds/Audio/impactMetal_heavy_000.ogg'))
    ops.append(field('m_RumbleClip', asset='Assets/_Project/Audio/Placeholder/teleport_charge.wav'))
    ops.append(field('m_FuseClip', asset='Assets/_Project/Audio/Placeholder/fire_loop.wav'))
    return ops

# ---------------------------------------------------------------- Beffroi : Boussole folle + Phare
# Un beffroi de pierre au bord de la voie : sa tour, deux grands cadrans dorés (devant, -z, et derrière) dont les aiguilles
# (m_Hands : repère au centre, l'aiguille vers le haut) tournent à l'envers, le clocher ouvert et sa cloche de bronze (m_Bell :
# repère à l'attache), le toit d'ardoise en gradins et sa flèche dorée ; à son pied, la rose des vents de laiton (la boussole).
# Niveau 2 : quatre pinacles dorés et une lanterne (le phare) au sommet ; niveau 3, Minuit : un croissant de lune doré et une
# étoile au bout de la flèche, et les douze heures dorées autour des cadrans.
BF_HALF = 1.3        # la demi-largeur de la tour
BF_BODY = 8.4        # le haut de la tour, sous le clocher
BF_CLOCK = 6.6       # le centre des cadrans
BF_BELFRY = 3.0      # la hauteur du clocher ouvert

def bf_clock(side):
    # Un cadran (side -1 : devant, -z ; 1 : derrière) : le cercle doré, le disque blanc, ses deux aiguilles noires.
    z = side * (BF_HALF + 0.02)
    name = 'Horloge ' + ('avant' if side < 0 else 'arrière')
    ops = [empty('', name, [0, BF_CLOCK, z])]
    ops.append(cyl(name, 'Cercle', [0, 0, side * 0.01], [2.1, 0.06, 2.1], role='accent', rot=[90, 0, 0]))
    ops.append(cyl(name, 'Cadran', [0, 0, side * 0.05], [1.8, 0.04, 1.8], role='blanc', rot=[90, 0, 0]))
    ops.append(sph(name, 'Axe', [0, 0, side * 0.1], [0.16, 0.16, 0.1], role='accent'))
    for hand, length, width in (('Heure', 0.5, 0.1), ('Minute', 0.8, 0.06)):
        ops.append(empty(name, hand, [0, 0, side * (0.09 if hand == 'Heure' else 0.12)]))
        ops.append(box(name + '/' + hand, 'Aiguille', [0, length / 2, 0], [width, length, 0.02], role='noir'))
    return ops

def beffroi():
    ops = []
    # La rose des vents à son pied : un disque de laiton, les huit pointes, le nord rouge.
    ops.append(cyl('', 'Rose', [0, 0.04, 0], [5.2, 0.08, 5.2], role='laiton'))
    for k in range(8):
        a = k * 45
        long = k % 2 == 0
        ops.append(box('', 'Pointe', [math.sin(math.radians(a)) * 1.9, 0.09, math.cos(math.radians(a)) * 1.9],
                       [0.3 if long else 0.2, 0.04, 1.0 if long else 0.6],
                       role='rouge' if k == 0 else 'accent', rot=[0, a, 0]))
    # La tour, ses bandeaux, sa porte.
    ops.append(box('', 'Socle', [0, 0.45, 0], [2 * BF_HALF + 0.4, 0.8, 2 * BF_HALF + 0.4], role='pierre', shade=0.8))
    ops.append(box('', 'Tour', [0, 0.844 + (BF_BODY - 0.844) / 2, 0], [2 * BF_HALF, BF_BODY - 0.844, 2 * BF_HALF], role='pierre'))
    for y in (3.0, 5.2, BF_BODY - 0.15):
        ops.append(box('', 'Bandeau', [0, y, 0], [2 * BF_HALF + 0.12, 0.2, 2 * BF_HALF + 0.12], role='pierre', shade=0.85))
    ops.append(box('', 'Porte', [0, 0.844 + 0.9, -BF_HALF - 0.02], [0.9, 1.8, 0.06], role='bronze', shade=0.6))
    ops += bf_clock(-1)
    ops += bf_clock(1)
    # Le clocher ouvert : quatre piliers, la cloche de bronze au milieu.
    top = BF_BODY - 0.006
    for sx in (-1, 1):
        for sz in (-1, 1):
            ops.append(box('', 'Pilier', [sx * (BF_HALF - 0.2), top + BF_BELFRY / 2, sz * (BF_HALF - 0.2)], [0.4, BF_BELFRY, 0.4], role='pierre'))
    roof = top + BF_BELFRY - 0.006
    ops.append(box('', 'Linteau', [0, roof + 0.15, 0], [2 * BF_HALF + 0.3, 0.3, 2 * BF_HALF + 0.3], role='pierre', shade=0.85))
    ops.append(empty('', 'Cloche', [0, roof - 0.05, 0]))
    ops.append(cyl('Cloche', 'Attache', [0, -0.15, 0], [0.16, 0.3, 0.16], role='noir'))
    ops.append(sph('Cloche', 'Robe', [0, -0.95, 0], [1.3, 1.3, 1.3], role='bronze'))
    ops.append(cyl('Cloche', 'Bord', [0, -1.45, 0], [1.55, 0.22, 1.55], role='bronze', shade=0.85))
    ops.append(cyl('Cloche', 'Liseré', [0, -1.2, 0], [1.42, 0.08, 1.42], role='accent'))
    ops.append(sph('Cloche', 'Battant', [0, -1.62, 0], [0.3, 0.3, 0.3], role='noir'))
    # Le toit d'ardoise en gradins, la flèche dorée.
    y = roof + 0.3 - 0.006
    for w, h in ((3.0, 0.5), (2.4, 0.5), (1.8, 0.5), (1.2, 0.5), (0.6, 0.5)):
        ops.append(box('', 'Toit', [0, y + h / 2, 0], [w, h, w], role='ardoise'))
        y += h - 0.006
    spire = y
    ops.append(cyl('', 'Flèche', [0, spire + 0.9, 0], [0.12, 1.8, 0.12], role='accent'))
    ops.append(sph('', 'Pommeau', [0, spire + 0.4, 0], [0.34, 0.34, 0.34], role='accent'))
    # Niveau 2 : quatre pinacles dorés aux coins du toit, et une lanterne (le phare) au-dessus.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    for sx in (-1, 1):
        for sz in (-1, 1):
            ops.append(cyl('Niveau 2', 'Pinacle', [sx * (BF_HALF + 0.05), roof + 0.3 + 0.5 - 0.006, sz * (BF_HALF + 0.05)], [0.22, 1.0, 0.22], role='accent'))
            ops.append(sph('Niveau 2', 'Boule', [sx * (BF_HALF + 0.05), roof + 1.3, sz * (BF_HALF + 0.05)], [0.3, 0.3, 0.3], role='accent'))
    ops.append(cyl('Niveau 2', 'Lanterne', [0, spire + 1.2, 0], [0.5, 0.5, 0.5], role='lumiere'))
    ops.append(cyl('Niveau 2', 'Chapeau', [0, spire + 1.5, 0], [0.62, 0.1, 0.62], role='accent'))
    # Niveau 3, Minuit : un croissant de lune et une étoile au bout de la flèche, les douze heures autour des cadrans.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    moon = spire + 2.35
    for k in range(7):
        a = math.radians(-90 + k * 30)
        ops.append(sph('Niveau 3', 'Lune %d' % (k + 1), [math.cos(a) * 0.55 - 0.1, moon + math.sin(a) * 0.55, 0], [0.28, 0.28, 0.2], role='accent'))
    ops.append(sph('Niveau 3', 'Étoile', [0.55, moon + 0.25, 0], [0.32, 0.32, 0.2], role='lumiere'))
    for side in (-1, 1):
        for h in range(12):
            a = math.radians(h * 30)
            ops.append(box('Niveau 3', 'Heure', [math.sin(a) * 1.18, BF_CLOCK + math.cos(a) * 1.18, side * (BF_HALF + 0.04)],
                           [0.12, 0.26, 0.06], role='accent', rot=[0, 0, -h * 30]))
    ops.append(field('m_Bell', ['Cloche']))
    ops.append(field('m_Hands', ['Horloge avant/Heure', 'Horloge avant/Minute', 'Horloge arrière/Heure', 'Horloge arrière/Minute']))
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_BellClip', asset='Assets/_Project/Audio/Placeholder/gong.wav'))
    ops.append(field('m_RewindClip', asset='Assets/_Project/Audio/Placeholder/teleport.wav'))
    return ops

# ---------------------------------------------------------------- Colisée : Chant des sirènes + Barricade
# Un petit amphithéâtre de marbre ouvert sur la voie (z le long de la voie, x en travers) : deux gradins en arc de cercle de part
# et d'autre, trois rangs chacun, une colonnade derrière ; la sirène de bronze sur sa colonne (+x), couronnée de laurier doré
# (m_CrownTemplate : chaque champion en porte une copie) ; un pan de mur (-x, m_WallTemplate : les murs de l'arène en sont des
# copies). Niveau 2 : quatre oriflammes ; niveau 3, les jeux du cirque : deux mâts dorés et une grande couronne de laurier
# suspendue au-dessus de la voie.
CO_SIDES = ((50.0, 130.0), (230.0, 310.0))    # les deux arcs des gradins (degrés depuis +z, vers +x)
CO_SEGMENTS = 7
CO_RANKS = 3
CO_INNER = 4.2       # le milieu du premier rang
CO_DEPTH = 0.9
CO_COLONNADE = 6.4

def co_point(a, r, y=0.0):
    return [math.sin(math.radians(a)) * r, y, math.cos(math.radians(a)) * r]

def co_laurel(parent, name, at, radius, count, size, plane='xz'):
    # Une couronne de laurier : des feuilles dorées (de petites sphères) en cercle ; son repère au centre.
    ops = [empty(parent, name, at)]
    c = parent + '/' + name if parent else name
    for k in range(count):
        a = k * 2 * math.pi / count
        p = [math.cos(a) * radius, 0, math.sin(a) * radius] if plane == 'xz' else [math.cos(a) * radius, math.sin(a) * radius, 0]
        ops.append(sph(c, 'Feuille', p, [size, size * 0.7, size * 1.3], role='accent'))
    return ops

def colisee():
    ops = []
    step = (CO_SIDES[0][1] - CO_SIDES[0][0]) / CO_SEGMENTS
    for a0, a1 in CO_SIDES:
        for s in range(CO_SEGMENTS):
            a = a0 + (s + 0.5) * step
            # Un segment sur deux 6 mm plus haut : leurs faces du dessus et du dessous ne se confondent pas.
            lift = 0.006 if s % 2 else 0.0
            for rank in range(CO_RANKS):
                r = CO_INNER + rank * CO_DEPTH
                h = 0.7 * (rank + 1)
                width = 2 * (r + CO_DEPTH / 2) * math.sin(math.radians(step / 2))
                ops.append(box('', 'Gradin', co_point(a, r, h / 2 + lift), [width, h, CO_DEPTH], role='marbre',
                               shade=1.0 - rank * 0.05, rot=[0, a, 0]))
            # L'entablement de la colonnade, au-dessus.
            width = 2 * (CO_COLONNADE + 0.25) * math.sin(math.radians(step / 2))
            ops.append(box('', 'Entablement', co_point(a, CO_COLONNADE, 3.35 + lift), [width, 0.3, 0.5], role='pierre', rot=[0, a, 0]))
        for s in range(CO_SEGMENTS + 1):
            a = a0 + s * step
            ops.append(cyl('', 'Colonne', co_point(a, CO_COLONNADE, 1.6), [0.4, 3.2, 0.4], role='marbre'))
            ops.append(box('', 'Chapiteau', co_point(a, CO_COLONNADE, 3.2 - 0.1 + 0.006), [0.55, 0.2, 0.55], role='accent', rot=[0, a, 0]))
    # La sirène de bronze sur sa colonne, côté +x, couronnée de laurier.
    sx = CO_COLONNADE + 1.1
    ops.append(box('', 'Piédestal', [sx, 0.3, 0], [1.1, 0.6, 1.1], role='pierre'))
    ops.append(cyl('', 'Fût', [sx, 0.594 + 2.2, 0], [0.6, 4.4, 0.6], role='marbre'))
    ops.append(box('', 'Chapiteau de la sirène', [sx, 0.594 + 4.4 + 0.15 - 0.012, 0], [0.9, 0.3, 0.9], role='accent'))
    top = 0.594 + 4.4 + 0.3 - 0.018
    ops.append(cyl('', 'Queue', [sx, top + 0.5, 0.15], [0.45, 1.0, 0.45], role='bronze', rot=[20, 0, 0]))
    ops.append(sph('', 'Nageoire', [sx, top + 0.1, 0.45], [0.7, 0.18, 0.4], role='bronze', shade=0.85))
    ops.append(sph('', 'Buste', [sx, top + 1.3, 0], [0.62, 0.85, 0.5], role='bronze'))
    ops.append(sph('', 'Tête', [sx, top + 1.95, 0], [0.4, 0.44, 0.4], role='bronze'))
    ops.append(sph('', 'Chevelure', [sx, top + 1.9, 0.12], [0.46, 0.5, 0.36], role='rose'))
    for s in (-1, 1):
        ops.append(tie('', 'Bras', [sx, top + 1.5, s * 0.25], [sx - 0.25, top + 2.0, s * 0.6], role='bronze', radius=0.08))
    ops += co_laurel('', 'Couronne', [sx, top + 2.2, 0], 0.26, 10, 0.12)
    # Le pan de mur (le modèle des murs de l'arène), côté -x : son repère en bas, au milieu.
    ops.append(empty('', 'Pan de mur', [-CO_COLONNADE - 1.1, 0, 0], [0, 90, 0]))
    ops.append(box('Pan de mur', 'Mur', [0, 0.9, 0], [1.6, 1.8, 0.4], role='marbre'))
    ops.append(box('Pan de mur', 'Bandeau', [0, 1.8 + 0.06 - 0.006, 0], [1.64, 0.12, 0.44], role='accent'))
    ops.append(box('Pan de mur', 'Arche', [0, 0.8, -0.21], [0.7, 1.1, 0.04], role='rouge'))
    # Niveau 2 : quatre oriflammes sur l'entablement.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    for k, a in enumerate((70.0, 110.0, 250.0, 290.0)):
        p = co_point(a, CO_COLONNADE, 3.5 + 1.0 - 0.006)
        ops.append(cyl('Niveau 2', 'Hampe', p, [0.1, 2.0, 0.1], role='accent'))
        q = co_point(a, CO_COLONNADE - 0.05, 4.9)
        ops.append(box('Niveau 2', 'Oriflamme', q, [0.7, 1.1, 0.04], role='rouge', rot=[0, a, 0]))
        ops.append(box('Niveau 2', 'Frange', co_point(a, CO_COLONNADE - 0.05, 4.31), [0.74, 0.08, 0.06], role='accent', rot=[0, a, 0]))
    # Niveau 3, les jeux du cirque : deux mâts dorés, une corde, une grande couronne de laurier au-dessus de la voie.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    for s in (-1, 1):
        ops.append(cyl('Niveau 3', 'Mât', [s * (CO_COLONNADE + 0.2), 3.5 + 2.5 - 0.006, 0], [0.24, 5.0, 0.24], role='accent'))
        ops.append(sph('Niveau 3', 'Pommeau', [s * (CO_COLONNADE + 0.2), 8.6, 0], [0.4, 0.4, 0.4], role='accent'))
    ops.append(tie('Niveau 3', 'Corde', [-(CO_COLONNADE + 0.2), 8.2, 0], [CO_COLONNADE + 0.2, 8.2, 0], role='rouge', radius=0.04))
    ops.append(tie('Niveau 3', 'Suspente', [0, 8.2, 0], [0, 7.4, 0], role='rouge', radius=0.03))
    ops += co_laurel('Niveau 3', 'Grande couronne', [0, 6.2, 0], 1.2, 22, 0.42, plane='xy')
    ops.append(field('m_WallTemplate', ['Pan de mur']))
    ops.append(field('m_CrownTemplate', ['Couronne']))
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_RiseClip', asset='Assets/_Project/Audio/Placeholder/barricade_build.wav'))
    ops.append(field('m_HitClip', asset='Assets/kenney_impact-sounds/Audio/impactPunch_medium_001.ogg'))
    ops.append(field('m_ChampionClip', asset='Assets/_Project/Audio/Placeholder/wave_start_horn.wav'))
    return ops

# ---------------------------------------------------------------- Montagnes russes : Chaîne de forçats + Tremplin
# Le Grappin (1er novembre au soir, choisi par le joueur après le premier essai : le wagon au ras de la voie ne prenait qu'un ennemi
# sur 30, et les emmenait vers la tour). Un circuit fermé, un ovale de bois rouge et or à 5 m au-dessus de la voie (z le long de la
# voie, en principe +z vers la forêt) : deux lignes droites (x = ±MR_W) reliées par deux virages, portées par trois portiques. Le
# train y tourne sans arrêt ; sous chaque wagon pend une chaîne et un grappin aimanté. Les cotes sont celles du code
# (TrioMontagnesRusses : k_HalfWidth, k_Height, k_HalfLength, k_RampLength, k_RampRise, k_HookHigh) : les wagons y roulent. Le wagon
# du modèle (m_WagonTemplate : repère au ras des rails, au milieu ; son grappin « Grappin », sa chaîne « Chaîne ») sert de modèle.
# Niveau 2 : des mâts à fanions et des guirlandes de lampions ; niveau 3, le grand huit infernal : un tremplin au bout de chaque
# ligne droite, ses étais, son arche dorée.
MR_W = 1.2           # la demi-largeur de l'ovale (les deux lignes droites, et le rayon des virages)
MR_H = 5.0           # le dessus des rails
MR_L = 4.0           # la demi-longueur des lignes droites
MR_RAMP = (4.5, 2.5)  # le tremplin : sa longueur, sa montée
MR_HOOK_HIGH = 3.6   # le grappin remonté (au-dessus du sol)
MR_GAUGE = 0.35      # la demi-largeur entre les deux rails
MR_POST_X = 2.6      # les poteaux des portiques

def mr_oval(s):
    # Le point de l'ovale à la distance s (le même calcul que TrioMontagnesRusses.OvalPoint), et sa tangente.
    W, L = MR_W, MR_L
    p = 4 * L + 2 * math.pi * W
    s = s % p
    if s < 2 * L:
        return [W, MR_H, -L + s], [0, 0, 1]
    if s < 2 * L + math.pi * W:
        a = (s - 2 * L) / W
        return [W * math.cos(a), MR_H, L + W * math.sin(a)], [-math.sin(a), 0, math.cos(a)]
    if s < 4 * L + math.pi * W:
        u = s - (2 * L + math.pi * W)
        return [-W, MR_H, L - u], [0, 0, -1]
    a = (s - (4 * L + math.pi * W)) / W
    return [-W * math.cos(a), MR_H, -L - W * math.sin(a)], [math.sin(a), 0, -math.cos(a)]

def mr_rails(parent, name, points, every=2):
    # Deux rails (des fils de fer) le long des points (et leurs tangentes), et une traverse de bois tous les « every » points.
    ops = []
    def side(t):
        return [t[2], 0, -t[0]]
    for g in (-1, 1):
        for (a, ta), (b, tb) in zip(points, points[1:]):
            sa, sb = side(ta), side(tb)
            ops.append(tie(parent, name, [a[0] + g * MR_GAUGE * sa[0], a[1] - 0.05, a[2] + g * MR_GAUGE * sa[2]],
                           [b[0] + g * MR_GAUGE * sb[0], b[1] - 0.05, b[2] + g * MR_GAUGE * sb[2]], role='metal', radius=0.05))
    for k in range(0, len(points), every):
        p, t = points[k]
        s = side(t)
        ops.append(tie(parent, 'Traverse', [p[0] - 0.5 * s[0], p[1] - 0.15, p[2] - 0.5 * s[2]],
                       [p[0] + 0.5 * s[0], p[1] - 0.15, p[2] + 0.5 * s[2]], role='bois', radius=0.07))
    return ops

def mr_portal(z):
    # Un portique : deux poteaux sur leurs socles, une poutre rouge sous les rails, des équerres dorées.
    ops = []
    beam_bottom = MR_H - 0.514
    for sx in (-1, 1):
        x = sx * MR_POST_X
        ops.append(box('', 'Socle', [x, 0.2, z], [0.6, 0.4, 0.6], role='pierre', shade=0.85))
        top = beam_bottom + 0.006
        ops.append(box('', 'Poteau', [x, (0.394 + top) / 2, z], [0.35, top - 0.394, 0.35], role='bois'))
        ops.append(tie('', 'Équerre', [x, MR_H - 1.7, z], [sx * (MR_POST_X - 1.0), beam_bottom + 0.05, z], role='accent', radius=0.06))
    ops.append(box('', 'Poutre', [0, beam_bottom + 0.15, z], [2 * MR_POST_X + 0.4, 0.3, 0.4], role='rouge'))
    for sx in (-1, 1):
        ops.append(sph('', 'Pommeau', [sx * (MR_POST_X - 0.4), beam_bottom + 0.3 + 0.12, z], [0.3, 0.3, 0.3], role='accent'))
    return ops

def mr_sign(z):
    # L'enseigne de foire au-dessus du portique du milieu, au-dessus du passage des wagons : un panneau rouge cerclé d'or, ses
    # ampoules, sur deux pieds dorés posés sur la poutre.
    ops = []
    beam_top = MR_H - 0.214
    bottom = MR_H + 1.3
    for sx in (-1, 1):
        ops.append(cyl('', 'Pied', [sx * 2.0, (beam_top - 0.006 + bottom + 0.006) / 2, z], [0.1, bottom - beam_top + 0.012, 0.1], role='accent'))
    ops.append(box('', 'Enseigne', [0, bottom + 0.5, z], [4.0, 1.0, 0.12], role='rouge'))
    ops.append(box('', 'Cadre', [0, bottom + 0.5, z], [4.1, 1.1, 0.08], role='accent'))
    for k in range(7):
        for sz in (-1, 1):
            ops.append(sph('', 'Ampoule', [-1.65 + k * 0.55, bottom + 0.5, z + sz * 0.07], [0.16, 0.16, 0.08],
                           role=('jaune', 'blanc')[k % 2]))
    return ops

def mr_wagon():
    # Le wagon rouge, tourné vers +z, au début de la première ligne droite ; dessous, le treuil, la chaîne et le grappin aimanté
    # (remonté). Le grappin : son repère sous l'aimant, ses trois griffes.
    ops = [empty('', 'Wagon', [MR_W, MR_H, -MR_L + 1.5])]
    ops.append(box('Wagon', 'Caisse', [0, 0.36, 0], [0.9, 0.5, 1.3], role='rouge'))
    ops.append(box('Wagon', 'Liseré', [0, 0.61 + 0.04 - 0.006, 0], [0.94, 0.08, 1.34], role='accent'))
    ops.append(box('Wagon', 'Dossier', [0, 0.61 + 0.25 - 0.006, -0.45], [0.8, 0.5, 0.15], role='bois'))
    for sx in (-1, 1):
        for sz in (-1, 1):
            ops.append(cyl('Wagon', 'Roue', [sx * MR_GAUGE, 0.12, sz * 0.45], [0.24, 0.08, 0.24], role='metal', rot=[0, 0, 90]))
    ops.append(cyl('Wagon', 'Treuil', [0, -0.004, 0], [0.24, 0.5, 0.24], role='metal', rot=[0, 0, 90]))
    hook = MR_HOOK_HIGH - MR_H
    ops.append(tie('Wagon', 'Chaîne', [0, -0.1, 0], [0, hook + 0.39, 0], role='fer', radius=0.04))
    ops.append(empty('Wagon', 'Grappin', [0, hook, 0]))
    g = 'Wagon/Grappin'
    ops.append(cyl(g, 'Aimant', [0, 0.15, 0], [0.5, 0.3, 0.5], role='rouge'))
    ops.append(cyl(g, 'Pôle', [0, -0.024, 0], [0.52, 0.06, 0.52], role='blanc'))
    ops.append(cyl(g, 'Anneau', [0, 0.3 + 0.05 - 0.006, 0], [0.2, 0.1, 0.2], role='accent'))
    for k in range(3):
        a = k * 2 * math.pi / 3
        tip = [math.cos(a) * 0.5, -0.5, math.sin(a) * 0.5]
        ops.append(tie(g, 'Griffe', [0, -0.02, 0], tip, role='fer', radius=0.05))
        ops.append(sph(g, 'Pointe', tip, [0.14, 0.14, 0.14], role='accent'))
    return ops

def mr_ramp(sign):
    # Un tremplin (niveau 3) au bout d'une ligne droite : x = sign · MR_W, de z = sign · MR_L vers l'extérieur, en montant.
    ops = []
    x = sign * MR_W
    n = 6
    pts = []
    for k in range(n + 1):
        t = k / n
        pts.append(([x, MR_H + MR_RAMP[1] * t, sign * (MR_L + MR_RAMP[0] * t)], [0, 0, sign]))
    ops += mr_rails('Niveau 3', 'Rail du tremplin', pts, every=1)
    for k in (2, 4, 6):
        p = pts[k][0]
        ops.append(box('Niveau 3', 'Étai', [p[0], (p[1] - 0.22) / 2, p[2]], [0.3, p[1] - 0.22, 0.3], role='bois', shade=0.85))
    end = pts[-1][0]
    for s in (-1, 1):
        ops.append(cyl('Niveau 3', 'Montant', [end[0] + s * 0.8, end[1] + 1.0, end[2]], [0.18, 2.0, 0.18], role='accent'))
    ops.append(tie('Niveau 3', 'Arche', [end[0] - 0.8, end[1] + 2.0, end[2]], [end[0] + 0.8, end[1] + 2.0, end[2]], role='accent', radius=0.12))
    ops.append(sph('Niveau 3', 'Crâne', [end[0], end[1] + 2.4, end[2]], [0.5, 0.5, 0.4], role='blanc'))
    return ops

def montagnes():
    ops = []
    # Les rails de l'ovale, tous les 0,8 m environ.
    perimeter = 4 * MR_L + 2 * math.pi * MR_W
    count = int(math.ceil(perimeter / 0.8))
    pts = [mr_oval(perimeter * k / count) for k in range(count + 1)]
    ops += mr_rails('', 'Rail', pts)
    for z in (-MR_L, 0.0, MR_L):
        ops += mr_portal(z)
    ops += mr_sign(0.0)
    ops += mr_wagon()
    # Niveau 2 : des mâts à fanions aux bouts des poutres, et des guirlandes de lampions de l'un à l'autre, le long de la voie.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    beam_top = MR_H - 0.214
    for sx in (-1, 1):
        x = sx * (MR_POST_X + 0.1)
        for z in (-MR_L, 0.0, MR_L):
            ops.append(cyl('Niveau 2', 'Mât', [x, beam_top + 0.6 - 0.006, z], [0.08, 1.2, 0.08], role='bois'))
            ops.append(box('Niveau 2', 'Fanion', [x, beam_top + 1.0, z + 0.32], [0.04, 0.4, 0.6], role='accent'))
        for z0, z1 in ((-MR_L, 0.0), (0.0, MR_L)):
            a, b = [x, beam_top + 1.15, z0], [x, beam_top + 1.15, z1]
            ops.append(tie('Niveau 2', 'Guirlande', a, b, role='blanc', radius=0.02))
            for k, color in enumerate(('jaune', 'rouge', 'bleu')):
                t = (k + 1) / 4.0
                ops.append(sph('Niveau 2', 'Lampion', [x, beam_top + 1.0, z0 + (z1 - z0) * t], [0.26, 0.32, 0.26], role=color))
    # Niveau 3, le grand huit infernal : un tremplin au bout de chaque ligne droite (le train part vers la forêt, quel que soit
    # le sens dans lequel le circuit est posé).
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    ops += mr_ramp(1)
    ops += mr_ramp(-1)
    ops.append(field('m_WagonTemplate', ['Wagon']))
    ops.append({'op': 'field', 'field': 'm_ChainMaterial', 'role': 'fer'})
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_DepartClip', asset='Assets/_Project/Audio/Placeholder/wave_start_horn.wav'))
    ops.append(field('m_PickClip', asset='Assets/kenney_impact-sounds/Audio/impactMetal_heavy_000.ogg'))
    ops.append(field('m_CrashClip', asset='Assets/_Project/Audio/Placeholder/explosion.wav'))
    ops.append(field('m_FlingClip', asset='Assets/507466__danjocross__clean-fast-swoosh.aiff'))
    return ops

def trios():
    return [
        {'name': 'Enclume', 'prefab': 'Duos/Trio_Enclume', 'definition': 'Trio_Enclume', 'duo': 'Duo_Marteau_pilon',
         'kinds': ['Mortar'], 'component': 'TrioEnclume', 'slot': 'Lane', 'footprint': [4, 4], 'palette': 'Trio_Enclume',
         'color': GOLD_LIGHT, 'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Le mortier tire des enclumes : l\'ennemi touché est aplati comme une crêpe et avance tout plat, lentement.',
         'levels': ['Une forge sur pilotis au-dessus de la voie : toutes les 4 s, son mortier tire une enclume sur l\'ennemi le '
                    'plus avancé ; aplati comme une crêpe, il avance tout plat, deux fois moins vite, 6 s.',
                    'L\'enclume rebondit et aplatit un deuxième ennemi ; l\'onde du choc fait tomber ceux qui sont autour.',
                    'La pluie d\'enclumes : vingt enclumes tombent du ciel sur les ennemis à portée, puis une enclume de 10 m au '
                    'milieu du groupe le plus serré ; les petits meurent net, tous les autres sont aplatis.'],
         'ops': enclume()},
        {'name': 'Gonfleur', 'prefab': 'Duos/Trio_Gonfleur', 'definition': 'Trio_Gonfleur', 'duo': 'Duo_Lanceur_baudruches',
         'kinds': ['Ballista'], 'component': 'TrioGonfleur', 'slot': 'Roadside', 'footprint': [4, 4], 'palette': 'Trio_Gonfleur',
         'color': GOLD_LIGHT, 'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Le harpon plante un tuyau, la pompe gonfle l\'ennemi comme une baudruche : il s\'envole, ta flèche le crève.',
         'levels': ['Un char de foire et sa pompe géante : le harpon plante un tuyau dans l\'ennemi le plus avancé, la pompe le '
                    'gonfle comme une baudruche et il s\'envole doucement ; ta flèche le crève (confettis : ceux du dessous sont '
                    'blessés), sinon il crève tout seul au bout de 6 s et retombe.',
                    'Deux tuyaux à la fois ; un ennemi crevé souffle son air sur deux voisins, qui gonflent à leur tour.',
                    'Le défilé : toute la portée gonfle d\'un coup, le ciel se remplit d\'ennemis-ballons et les gros deviennent '
                    'des chars de parade ; chaque ballon crevé par ta flèche rapporte deux fois ses points.'],
         'ops': gonfleur()},
        {'name': 'Bowling', 'prefab': 'Duos/Trio_Bowling', 'definition': 'Trio_Bowling', 'duo': 'Duo_Avalanche',
         'kinds': ['Barricade'], 'component': 'TrioBowling', 'slot': 'Lane', 'footprint': [4, 3], 'palette': 'Trio_Bowling',
         'color': GOLD_LIGHT, 'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Les ennemis sont rangés en quilles sur la voie, une boule de glace roule dessus : STRIKE !',
         'levels': ['Une machine de bowling géante enjambe la voie : plus loin, les ennemis qui arrivent sont rangés en triangle '
                    'de quilles ; une boule de glace roule sur eux vers la forêt et ils volent dans tous les sens. Ta flèche dans '
                    'la boule l\'accélère : elle éclate en glace au milieu des quilles.',
                    'Un strike rend la boule suivante deux fois plus grosse ; s\'il en reste debout, une deuxième boule fait le '
                    'spare.',
                    'La partie parfaite : trois triangles de quilles, douze boules d\'affilée, la dernière de 4 m ; le tableau '
                    'affiche 300.'],
         'ops': bowling()},
        {'name': 'Grande bombarde', 'prefab': 'Duos/Trio_Bombarde', 'definition': 'Trio_Bombarde', 'duo': 'Duo_Feu_d_artifice',
         'kinds': ['Trampoline'], 'component': 'TrioBombarde', 'slot': 'Roadside', 'footprint': [4, 4], 'palette': 'Trio_Bombarde',
         'color': GOLD_LIGHT, 'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Un mortier géant qui se remplit de poudre ; c\'est toi qui allumes la mèche : plus il est plein, plus le '
                        'bouquet est gros.',
         'levels': ['Une bombarde de bronze sur ses ressorts : chaque ennemi tué autour d\'elle, et chaque flèche tirée dans sa '
                    'gueule, la remplit de poudre (ses cercles rougeoient). Ta flèche dans la cible de la mèche l\'allume : jusqu\'à '
                    '12 bombes de feu d\'artifice sur les groupes, d\'autant plus qu\'elle est pleine.',
                    'Elle se remplit deux fois plus vite ; au tir, elle rebondit si fort que les ennemis proches sont renversés.',
                    'Le bouquet final : pleine à ras bord, elle n\'attend plus ; elle déborde, puis tire 40 bombes sur toutes les '
                    'voies à la fois.'],
         'ops': bombarde()},
        {'name': 'Beffroi', 'prefab': 'Duos/Trio_Beffroi', 'definition': 'Trio_Beffroi', 'duo': 'Duo_Boussole_folle',
         'kinds': ['Lighthouse'], 'component': 'TrioBeffroi', 'slot': 'Roadside', 'footprint': [3, 3], 'palette': 'Trio_Beffroi',
         'color': GOLD_LIGHT, 'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Quand la cloche sonne, les ennemis rembobinent leurs 4 dernières secondes et perdent leur avance.',
         'levels': ['Un beffroi aux aiguilles qui tournent à l\'envers : toutes les 10 s, sa cloche sonne et les ennemis du rayon '
                    'rembobinent leurs 4 dernières secondes ; ils repartent à reculons sur leur chemin.',
                    'À l\'arrivée, ils restent figés 2 s.',
                    'Minuit : douze coups ; le temps s\'arrête 6 s dans tout le rayon, sauf pour toi. Tes flèches s\'arrêtent en '
                    'l\'air devant leurs cibles et, au douzième coup, repartent toutes ensemble.'],
         'ops': beffroi()},
        {'name': 'Colisée', 'prefab': 'Duos/Trio_Colisee', 'definition': 'Trio_Colisee', 'duo': 'Duo_Chant_des_sirenes',
         'kinds': ['Barricade'], 'component': 'TrioColisee', 'slot': 'Lane', 'footprint': [4, 4], 'palette': 'Trio_Colisee',
         'color': GOLD_LIGHT, 'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Une arène s\'élève autour des ennemis : ils se battent, et le vainqueur se range de ton côté.',
         'levels': ['Une arène de marbre s\'élève autour d\'un groupe d\'ennemis : hypnotisés, ils se battent entre eux ; le '
                    'dernier debout se range de ton côté, couronné de laurier, et frappe ceux qui passent. Ta flèche élimine le '
                    'gladiateur qu\'elle touche.',
                    'Deux champions à la fois ; un champion qui tombe éclate en bulles qui hypnotisent ses voisins.',
                    'Les jeux du cirque : l\'arène s\'élargit sur toute la voie et avale un groupe entier ; le vainqueur devient '
                    'géant, en armure dorée, et marche à contre-sens sur la voie.'],
         'ops': colisee()},
        {'name': 'Montagnes russes', 'prefab': 'Duos/Trio_Montagnes_russes', 'definition': 'Trio_Montagnes_russes',
         'duo': 'Duo_Chaine_de_forcats', 'kinds': ['Trampoline'], 'component': 'TrioMontagnesRusses', 'slot': 'Lane',
         'footprint': [4, 8], 'palette': 'Trio_Montagnes', 'color': GOLD_LIGHT,
         # Le Grappin (1er novembre au soir) : une nouvelle passe refait le modèle ; les textes changent s'ils n'ont pas été
         # changés à la main.
         'mark': 'Grappin du 1er novembre', 'rebuild': True, 'plans': [0, 4, 4],
         'descriptionBefore': 'Un wagon aimanté ramasse les ennemis au ras de la voie ; en haut du looping, ils tombent.',
         'description': 'Un circuit en l\'air au-dessus de la voie : les grappins des wagons accrochent les ennemis et les jettent '
                        'vers la forêt.',
         'levelsBefore': ['Un circuit de bois et son looping sur la voie : toutes les 6 s, un wagon aimanté file au ras de la voie '
                          'et ramasse jusqu\'à 4 ennemis ; en haut du looping, la tête en bas, ils tombent.',
                          'Un train de trois wagons ; ceux qui tombent retombent sur un groupe, qu\'ils renversent.',
                          'Le grand huit infernal : dix wagons, le tremplin au bout du circuit et un saut dans le vide ; le train '
                          's\'écrase sur le groupe le plus serré, l\'atterrissage tue.'],
         'levels': ['Un circuit en ovale à 5 m au-dessus de la voie, où deux wagons tournent sans arrêt. Sous chacun, un grappin '
                    'aimanté se balance d\'un bord à l\'autre de la voie et accroche les ennemis qu\'il frôle (3 au plus) ; ils '
                    'pendent sous le wagon jusqu\'au virage du côté de la forêt, qui les jette 13 m plus loin, vers la forêt.',
                    'Trois wagons, quatre ennemis par grappin ; la grappe qui pend fouette au passage ceux qui sont dessous.',
                    'Le grand huit infernal : un train de six wagons file à toute vitesse, accroche tout sur son passage, '
                    'monte le tremplin et saute dans le vide ; il s\'écrase sur le groupe le plus serré, l\'atterrissage tue.'],
         'ops': montagnes()},
    ]
