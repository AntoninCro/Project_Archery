# Les duos du tower defense (lot 7 de la refonte, 31 octobre ; Docs/Refonte_TowerDefense.md, 12.14 et 12.15) : chacun est une
# tour nouvelle, à l'attaque à elle (une TowerDuo), née de la fusion de deux tours. conception.py écrit ici leur partie du fichier
# d'allure (« duos ») ; le baker (Editor/TowerDefenseLookBaker) en fait le prefab (Prefabs/Structures/Duos), la définition
# (Data/Duos : ses 3 niveaux, 4 plans chacun) et l'entrée du catalogue (Resources/TowerDuos).
# Le modèle est fait d'opérations, comme l'allure des tours ; ses pièces des niveaux 2 et 3 sont les repères « Niveau 2 » et
# « Niveau 3 » (réglage m_LevelParts). Règle des faces : une pièce posée sur une autre y est enfoncée de 6 mm.
import math

def r(v):
    return [round(float(x), 3) for x in v]

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

def field(name, paths=None, asset=None):
    op = {'op': 'field', 'field': name}
    if paths is not None:
        op['paths'] = paths
    if asset:
        op['asset'] = asset
    return op

GOLD_LIGHT = '#E8B84A'

# Une palette par duo : l'accent doré est le signe d'un duo.
PALETTES = {
    'Duo_Boulet': {'pierre': '#9C978E', 'sable': '#CDBB9E', 'bois': '#7A4E33', 'metal': '#3E4047', 'blanc': '#C9CCD2', 'accent': GOLD_LIGHT},
    'Duo_Marteau': {'pierre': '#8E8A84', 'bois': '#6E4A30', 'metal': '#3A3C42', 'accent': GOLD_LIGHT},
    'Duo_Toboggan': {'glace': '#A8DCF0', 'neige': '#EEF3F6', 'bois': '#7A5236', 'paille': '#D9B35A', 'blanc': '#E8E4DA',
                     'rouge': '#C8463A', 'accent': GOLD_LIGHT},
    'Duo_Yoyo': {'pierre': '#9A948A', 'bois': '#7B5236', 'metal': '#45474E', 'rouge': '#B8402F', 'blanc': '#E6E2D6', 'accent': GOLD_LIGHT},
    'Duo_Avalanche': {'pierre': '#8F9196', 'glace': '#9FD6EE', 'neige': '#F0F5F8', 'metal': '#3F454E', 'accent': GOLD_LIGHT},
    'Duo_Feu': {'pierre': '#8E8A84', 'rouge': '#C8392E', 'blanc': '#EEE9DE', 'metal': '#3E4148', 'bois': '#7A5236',
                'bleu': '#3A6FD0', 'vert': '#3FA65A', 'lumiere': '#FFE6A0', 'accent': GOLD_LIGHT},
    'Duo_Sirenes': {'pierre': '#8E8A8C', 'nacre': '#EDE0E8', 'rose': '#E07AAE', 'bronze': '#9A6A3A', 'accent': GOLD_LIGHT},
    'Duo_Loupe': {'pierre': '#8E8A84', 'bois': '#7A5236', 'laiton': '#B8923A', 'metal': '#3E4148', 'verre': '#BFE6F2',
                  'lumiere': '#FFF0B0', 'accent': GOLD_LIGHT},
    'Duo_Carquois': {'pierre': '#8E8A84', 'bois': '#7A5236', 'metal': '#3E4148', 'rouge': '#C23B30', 'accent': GOLD_LIGHT},
    'Duo_Chaine': {'pierre': '#8E8A84', 'metal': '#4A4D55', 'fer': '#2E3036', 'bois': '#7A5236', 'rouge': '#C0352B', 'blanc': '#D8DCE2',
                   'accent': GOLD_LIGHT},
}

MARK = 'Duo du 31 octobre'

# ---------------------------------------------------------------- Boulet fou : Barricade + Tremplin (l'idée du joueur)
# Après le premier essai (31 octobre au soir, le joueur : « il doit avoir de la gueule ») : une fois et demie plus grand.
AXLE = 5.2        # hauteur de l'essieu
ARM = 3.9         # du centre de l'essieu au centre du boulet : au plus bas, il passe à 1,3 m du sol (les petits ennemis aussi)
BALL = 1.6        # diamètre du boulet
FRAME_Z = 1.15    # les deux chevalets, de part et d'autre du plan de la roue
FOOT_X = 3.0      # l'écart des pieds de chaque chevalet
FOOT_H = 0.4      # la hauteur des pieds de pierre

def boulet(parent, name, y):
    # Un boulet hérissé, cerclé d'or : sa sphère, deux cercles, six pointes.
    ops = [empty(parent, name, [0, y, 0])]
    b = parent + '/' + name
    ops.append(sph(b, 'Sphère', [0, 0, 0], [BALL, BALL, BALL], role='metal', shade=0.55))
    ops.append(cyl(b, 'Cercle', [0, 0, 0], [BALL * 1.04, 0.1, BALL * 1.04], role='accent'))
    ops.append(cyl(b, 'Cercle', [0, 0, 0], [BALL * 1.04, 0.1, BALL * 1.04], role='accent', rot=[90, 0, 0]))
    d = BALL * 0.5
    for at, rot in (([d, 0, 0], [0, 0, 90]), ([-d, 0, 0], [0, 0, 90]), ([0, d, 0], [0, 0, 0]), ([0, -d, 0], [0, 0, 0]),
                    ([0, 0, d], [90, 0, 0]), ([0, 0, -d], [90, 0, 0])):
        ops.append(sph(b, 'Pointe', at, [0.24, 0.64, 0.24], role='metal', shade=0.8, rot=rot))
    return ops

def boulet_fou():
    ops = []
    # Deux chevalets en A, de part et d'autre du plan de la roue (comme une grande roue), sur des pieds de pierre plaqués d'or.
    leg = math.hypot(FOOT_X, AXLE)
    tilt = math.degrees(math.atan2(FOOT_X, AXLE))
    for z in (-FRAME_Z, FRAME_Z):
        for s in (-1, 1):
            ops.append(box('', 'Pied', [s * FOOT_X, FOOT_H / 2, z], [1.0, FOOT_H, 1.0], role='pierre'))
            ops.append(box('', 'Plaque', [s * FOOT_X, FOOT_H + 0.03 - 0.006, z], [1.04, 0.06, 1.04], role='accent'))
            ops.append(box('', 'Jambe', [s * FOOT_X / 2, AXLE / 2, z], [0.38, leg + 0.15, 0.38], role='bois', rot=[0, 0, s * tilt]))
        span = 2 * FOOT_X * (1 - 2.2 / AXLE)
        ops.append(box('', 'Traverse', [0, 2.2, z], [span + 0.3, 0.3, 0.3], role='bois', shade=0.85))
        # Un fanion doré au-dessus de chaque chevalet, sur un mât qui part de l'embout de l'essieu.
        side = 1 if z > 0 else -1
        ops.append(cyl('', 'Mât', [0, AXLE + 1.0, z + side * 0.3], [0.12, 2.0, 0.12], role='bois'))
        ops.append(box('', 'Fanion', [0, AXLE + 1.7, z + side * 0.92], [0.05, 0.6, 1.1], role='accent'))
    # L'essieu, ses embouts dorés.
    ops.append(cyl('', 'Essieu', [0, AXLE, 0], [0.36, 2 * FRAME_Z + 0.6, 0.36], role='metal', rot=[90, 0, 0]))
    for z in (-1, 1):
        ops.append(cyl('', 'Embout', [0, AXLE, z * (FRAME_Z + 0.3)], [0.6, 0.12, 0.6], role='accent', rot=[90, 0, 0]))
    # Le bras qui tourne (en travers de la voie : autour de son axe Z), son moyeu doré, la tige, le boulet.
    ops.append(empty('', 'Bras', [0, AXLE, 0]))
    ops.append(cyl('Bras', 'Moyeu', [0, 0, 0], [0.9, 0.6, 0.9], role='accent', rot=[90, 0, 0]))
    ops.append(box('Bras', 'Tige', [0, -ARM / 2, 0], [0.26, ARM, 0.26], role='metal', shade=0.8))
    ops += boulet('Bras', 'Boulet', -ARM)
    # Niveau 2 : rebondissant pour de bon, un ressort de cuivre sur la tige et un collier de pointes dorées.
    ops.append(empty('Bras', 'Niveau 2', [0, 0, 0]))
    for k in range(8):
        ops.append(cyl('Bras/Niveau 2', 'Spire', [0, -0.9 - k * 0.28, 0], [0.52, 0.07, 0.52], role='cuivre'))
    for k in range(6):
        a = math.radians(k * 60)
        ops.append(sph('Bras/Niveau 2', 'Pointe dorée', [math.cos(a) * 0.99, -ARM, math.sin(a) * 0.99], [0.2, 0.2, 0.5],
                       role='accent', rot=[0, -k * 60 + 90, 0]))
    # Niveau 3, le boulet de canon : un second bras et un second boulet, à l'opposé.
    ops.append(empty('Bras', 'Niveau 3', [0, 0, 0]))
    ops.append(box('Bras/Niveau 3', 'Tige', [0, ARM / 2, 0], [0.26, ARM, 0.26], role='metal', shade=0.8))
    ops += boulet('Bras/Niveau 3', 'Boulet 2', ARM)
    ops.append(field('m_Arm', ['Bras']))
    ops.append(field('m_Balls', ['Bras/Boulet', 'Bras/Niveau 3/Boulet 2']))
    ops.append(field('m_LevelParts', ['Bras/Niveau 2', 'Bras/Niveau 3']))
    ops.append({'op': 'field', 'field': 'm_BallRadius', 'value': BALL / 2})
    ops.append(field('m_SwingClip', asset='Assets/650564__h2p34__lightsaber-whoosh.wav'))
    ops.append(field('m_HitClip', asset='Assets/kenney_impact-sounds/Audio/impactPunch_heavy_002.ogg'))
    return ops

# ---------------------------------------------------------------- Marteau-pilon : Barricade + Piques
# Un portique de forge en travers de la voie (x), 9 m de haut ; le marteau de pierre cerclé de fer glisse sous la poutre et tombe.
MP_PILLAR_X = 3.7   # les deux piliers, de part et d'autre de la voie
MP_TOP = 8.0        # le haut des piliers : la poutre y est posée
MP_REST = 5.0       # le centre d'un marteau au repos
MP_RAIL = 7.88      # le chariot, sous la poutre

def mp_head(parent, name, x, width):
    # Une tête de marteau : un bloc de pierre (1,4 m de haut : sa demi-hauteur, m_HeadHalf), deux cercles de fer, une plaque
    # dorée, quatre piques dessous (l'héritage des piques : elles s'enfoncent dans le sol au coup).
    ops = [empty(parent, name, [x, MP_REST, 0])]
    h = parent + '/' + name if parent else name
    ops.append(box(h, 'Bloc', [0, 0, 0], [width, 1.4, width], role='pierre', shade=0.8))
    for y in (-0.38, 0.38):
        ops.append(box(h, 'Cercle', [0, y, 0], [width + 0.08, 0.22, width + 0.08], role='metal'))
    ops.append(box(h, 'Plaque', [0, 0.7 + 0.04 - 0.006, 0], [width * 0.6, 0.08, width * 0.6], role='accent'))
    d = width * 0.3
    for sx in (-1, 1):
        for sz in (-1, 1):
            ops.append(sph(h, 'Pique', [sx * d, -0.7 - 0.12, sz * d], [0.22, 0.5, 0.22], role='metal', shade=0.7))
    return ops

def mp_chariot(parent, name, x):
    # Le chariot qui roule sous la poutre, et le câble (repère en haut, 1 m de câble à l'échelle 1) qui tient le marteau.
    ops = [empty(parent, name, [x, MP_RAIL, 0])]
    c = parent + '/' + name if parent else name
    ops.append(box(c, 'Caisse', [0, 0, 0], [0.9, 0.3, 0.8], role='metal', shade=0.85))
    for z in (-0.3, 0.3):
        ops.append(cyl(c, 'Roue', [0, 0.18, z], [0.26, 0.08, 0.26], role='accent', rot=[90, 0, 0]))
    ops.append(empty(c, 'Câble', [0, -0.15 + 0.006, 0]))
    ops.append(cyl(c + '/Câble', 'Fil', [0, -0.5, 0], [0.14, 1.0, 0.14], role='metal', shade=0.6))
    return ops

def marteau_pilon():
    ops = []
    # Les piliers de pierre sur leurs socles, cerclés de fer ; les jambes de force ; la poutre ; le rail.
    for s in (-1, 1):
        x = s * MP_PILLAR_X
        ops.append(box('', 'Socle', [x, 0.3, 0], [1.6, 0.6, 2.6], role='pierre', shade=0.9))
        ops.append(box('', 'Plaque', [x, 0.6 + 0.03 - 0.006, 0], [1.64, 0.06, 2.64], role='accent'))
        ops.append(box('', 'Pilier', [x, (0.594 + MP_TOP + 0.006) / 2, 0], [1.0, MP_TOP + 0.006 - 0.594, 1.0], role='pierre'))
        for y in (2.2, 4.4, 6.6):
            ops.append(box('', 'Cercle', [x, y, 0], [1.08, 0.2, 1.08], role='metal'))
        for z in (-1, 1):
            # Une jambe de force, du socle au pilier, de chaque côté (le long de la voie).
            ops.append(box('', 'Jambe de force', [x, 2.2, z * 0.75], [0.3, 3.4, 0.3], role='bois', rot=[-z * 18, 0, 0]))
    span = 2 * MP_PILLAR_X + 1.0
    ops.append(box('', 'Poutre', [0, MP_TOP + 0.45 - 0.006, 0], [span, 0.9, 1.3], role='bois', shade=0.9))
    for x in (-MP_PILLAR_X, MP_PILLAR_X):
        ops.append(box('', 'Ferrure', [x, MP_TOP + 0.45 - 0.006, 0], [1.1, 1.0, 1.36], role='metal'))
    ops.append(box('', 'Rail', [0, MP_TOP - 0.06, 0], [2 * MP_PILLAR_X - 1.1, 0.12, 0.5], role='metal', shade=0.7))
    # Une enclume dorée au sommet, entre deux fanions.
    top = MP_TOP + 0.9 - 0.012
    ops.append(box('', 'Enclume', [0, top + 0.3 - 0.006, 0], [1.2, 0.6, 0.7], role='metal'))
    ops.append(box('', 'Table', [0, top + 0.6 + 0.12 - 0.012, 0], [2.0, 0.24, 0.8], role='accent'))
    for s in (-1, 1):
        ops.append(cyl('', 'Mât', [s * MP_PILLAR_X, top + 1.0 - 0.006, 0], [0.12, 2.0, 0.12], role='bois'))
        ops.append(box('', 'Fanion', [s * MP_PILLAR_X, top + 1.6, s * 0.6], [0.05, 0.6, 1.1], role='accent'))
    # Le marteau du milieu (niveau 1).
    ops += mp_chariot('', 'Chariot', 0)
    ops += mp_head('', 'Marteau', 0, 2.2)
    # Niveau 2 : la chaudière de la forge contre un pilier, sa cheminée, ses cercles dorés.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    bx = -MP_PILLAR_X - 0.8 - 0.6 + 0.006
    ops.append(cyl('Niveau 2', 'Chaudière', [bx, 1.3 - 0.006, 0], [1.2, 2.6, 1.2], role='cuivre'))
    for y in (0.9, 1.9):
        ops.append(cyl('Niveau 2', 'Cercle', [bx, y, 0], [1.26, 0.12, 1.26], role='accent'))
    ops.append(cyl('Niveau 2', 'Cheminée', [bx, 2.594 - 0.006 + 1.0, 0], [0.4, 2.0, 0.4], role='metal'))
    # Niveau 3, la forge : deux marteaux de plus, à gauche et à droite.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    for name, x in (('gauche', -2.27), ('droit', 2.27)):
        ops += mp_chariot('Niveau 3', 'Chariot ' + name, x)
        ops += mp_head('Niveau 3', 'Marteau ' + name, x, 1.5)
    ops.append(field('m_Hammers', ['Marteau', 'Niveau 3/Marteau gauche', 'Niveau 3/Marteau droit']))
    ops.append(field('m_Chariots', ['Chariot', 'Niveau 3/Chariot gauche', 'Niveau 3/Chariot droit']))
    ops.append(field('m_Cables', ['Chariot/Câble', 'Niveau 3/Chariot gauche/Câble', 'Niveau 3/Chariot droit/Câble']))
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_SlamClip', asset='Assets/kenney_impact-sounds/Audio/impactMetal_heavy_002.ogg'))
    ops.append(field('m_PopClip', asset='Assets/kenney_interfaceSounds/Audio/pluck_002.ogg'))
    return ops

# ---------------------------------------------------------------- Toboggan : Tremplin + Tour de givre
# Une piste de glace de 7,5 m en pente vers la forêt (-z), un tremplin de glace au bout, des bottes de paille, le jury. Le
# code (DuoToboggan) suit cette glace : la pente de 0,15 m (z = -2,4) à 1 m (z = 5) ; le tremplin jusqu'à z = -3,8, à 0,66 m
# (0,9 m au niveau 2, 1,3 m au niveau 3).
TB_LOW = (-2.4, 0.15)   # le bas de la pente (z, hauteur de la glace)
TB_TOP = (5.0, 1.0)     # le haut
TB_HALF = 2.7           # la demi-largeur de la glace

def tb_segment(parent, name, z0, y0, angle, length, width, role, thick=0.3):
    # Un morceau qui part de (z0, y0) vers -z en montant de angle degrés : sa face du dessus suit la courbe.
    a = math.radians(angle)
    z1, y1 = z0 - math.cos(a) * length, y0 + math.sin(a) * length
    nz, ny = math.sin(a), math.cos(a)
    mz, my = (z0 + z1) / 2, (y0 + y1) / 2
    return box(parent, name, [0, my - ny * thick / 2, mz - nz * thick / 2], [width, thick, length], role=role, rot=[angle, 0, 0])

def tb_ramp(parent, z0, y0, angles, lengths, width, name):
    # Des morceaux de plus en plus raides, qui se chevauchent de 6 cm ; renvoie le bout de la courbe.
    ops = []
    z, y = z0, y0
    for angle, length in zip(angles, lengths):
        ops.append(tb_segment(parent, name, z, y, angle, length + 0.06, width, 'glace'))
        a = math.radians(angle)
        z, y = z - math.cos(a) * length, y + math.sin(a) * length
    return ops, (z, y)

def toboggan():
    ops = [empty('', 'Piste', [0, 0, 0])]
    P = 'Piste'
    # La pente : la glace, sur une butte de neige ; ses liserés dorés.
    (z0, y0), (z1, y1) = TB_LOW, TB_TOP
    length = math.hypot(z1 - z0, y1 - y0)
    slope = math.degrees(math.atan2(y1 - y0, z1 - z0))
    mz, my = (z0 + z1) / 2, (y0 + y1) / 2
    n = (-math.sin(math.radians(slope)), math.cos(math.radians(slope)))   # (z, y) : la normale du dessus
    ops.append(box(P, 'Glace', [0, my - n[1] * 0.15, mz - n[0] * 0.15], [2 * TB_HALF, 0.3, length], role='glace', rot=[-slope, 0, 0]))
    ops.append(box(P, 'Butte', [0, my - n[1] * 0.894, mz - n[0] * 0.894], [2 * TB_HALF + 0.2, 1.2, length - 0.2], role='neige',
                   rot=[-slope, 0, 0]))
    for s in (-1, 1):
        ops.append(box(P, 'Liseré', [s * (TB_HALF + 0.02), my + n[1] * 0.03, mz + n[0] * 0.03], [0.1, 0.12, length], role='accent',
                       rot=[-slope, 0, 0]))
    # Le tremplin de glace (trois morceaux de plus en plus raides), des ressorts de cuivre sous son bec.
    kick, (kz, ky) = tb_ramp(P, z0, y0, [8, 20, 32], [0.5, 0.5, 0.5], 2 * TB_HALF, 'Tremplin')
    ops += kick
    for x in (-1.8, -0.6, 0.6, 1.8):
        for k in range(3):
            ops.append(cyl(P, 'Ressort', [x, 0.08 + k * 0.1, -3.7], [0.36, 0.05, 0.36], role='cuivre'))
    # Les bottes de paille, de chaque côté.
    for s in (-1, 1):
        for z in (-1.6, -0.2, 1.2, 2.6, 4.0):
            ops.append(box(P, 'Botte', [s * (TB_HALF + 0.42), 0.27, z], [0.7, 0.55, 1.2], role='paille'))
            ops.append(box(P, 'Lien', [s * (TB_HALF + 0.42), 0.27, z], [0.74, 0.59, 0.08], role='bois', shade=0.7))
    # Les cristaux de givre en haut de la piste, deux fanions dorés sur les bottes du haut.
    for s in (-1, 1):
        for k, (dx, h) in enumerate(((0.0, 2.0), (0.45, 1.3), (-0.4, 1.1))):
            ops.append(sph(P, 'Cristal', [s * (TB_HALF - 0.4) + dx, y1 + h / 2 - 0.1, z1 - 0.3 - k * 0.25], [0.45, h, 0.45],
                           role='glace', shade=1.15, rot=[0, 0, s * (k - 1) * 12]))
        ops.append(cyl(P, 'Mât', [s * (TB_HALF + 0.42), 0.55 + 1.5 - 0.006, 4.0], [0.1, 3.0, 0.1], role='bois'))
        ops.append(box(P, 'Fanion', [s * (TB_HALF + 0.42), 3.3, 4.0 - 0.62], [0.05, 0.6, 1.2], role='accent'))
    # Le jury, à côté du tremplin : une estrade, trois panneaux blancs cerclés d'or.
    jx = TB_HALF + 1.5
    ops.append(box(P, 'Estrade', [jx, 0.5, -3.0], [1.3, 0.16, 2.2], role='bois'))
    for z in (-3.9, -2.1):
        ops.append(box(P, 'Pied', [jx, 0.21, z], [0.14, 0.432, 0.14], role='bois', shade=0.8))
    for z in (-3.7, -3.0, -2.3):
        ops.append(cyl(P, 'Manche', [jx, 0.58 + 0.5 - 0.006, z], [0.06, 1.0, 0.06], role='bois'))
        ops.append(box(P, 'Panneau', [jx, 1.08 + 0.3 - 0.006, z], [0.06, 0.5, 0.5], role='blanc'))
        ops.append(box(P, 'Cadre', [jx, 1.08 + 0.3 - 0.006, z], [0.05, 0.56, 0.56], role='accent'))
    # Niveau 2 : un bec au tremplin (0,9 m), des fanions rouges le long de la piste.
    ops.append(empty(P, 'Niveau 2', [0, 0, 0]))
    ops.append(tb_segment(P + '/Niveau 2', 'Bec', kz + math.cos(math.radians(32)) * 0.06, ky - math.sin(math.radians(32)) * 0.06,
                          45, 0.41, 2 * TB_HALF, 'glace'))
    for s in (-1, 1):
        for z in (-0.2, 2.6):
            ops.append(cyl(P + '/Niveau 2', 'Piquet', [s * (TB_HALF + 0.42), 0.55 + 0.7 - 0.006, z], [0.06, 1.4, 0.06], role='bois'))
            ops.append(box(P + '/Niveau 2', 'Fanion rouge', [s * (TB_HALF + 0.42), 1.65, z + 0.32], [0.04, 0.4, 0.6], role='rouge'))
    # Niveau 3, le saut à ski : un grand tremplin sur pilotis (1,3 m au bout), la tour du jury coiffée d'or.
    ops.append(empty(P, 'Niveau 3', [0, 0, 0]))
    big, _ = tb_ramp(P + '/Niveau 3', z0, y0, [14, 38, 58], [0.55, 0.6, 0.75], 2 * TB_HALF - 0.4, 'Grand tremplin')
    ops += big
    for x in (-2.0, 2.0):
        for z, h in ((-3.25, 0.45), (-3.95, 1.1)):
            ops.append(box(P + '/Niveau 3', 'Pilotis', [x, h / 2 - 0.006, z], [0.22, h, 0.22], role='bois'))
    tx, tz = -(TB_HALF + 1.7), -3.4
    for dx in (-0.6, 0.6):
        for dz in (-0.6, 0.6):
            ops.append(box(P + '/Niveau 3', 'Poteau', [tx + dx, 2.5 - 0.006, tz + dz], [0.2, 5.0, 0.2], role='bois'))
    ops.append(box(P + '/Niveau 3', 'Loge', [tx, 5.0 + 0.6 - 0.012, tz], [1.6, 1.2, 1.6], role='blanc'))
    ops.append(box(P + '/Niveau 3', 'Toit', [tx, 6.2 + 0.2 - 0.018, tz], [2.0, 0.4, 2.0], role='accent'))
    ops.append(sph(P + '/Niveau 3', 'Pommeau', [tx, 6.4 + 0.3 - 0.03, tz], [0.5, 0.6, 0.5], role='accent', shade=1.2))
    ops.append(field('m_Track', ['Piste']))
    ops.append(field('m_LevelParts', ['Piste/Niveau 2', 'Piste/Niveau 3']))
    ops.append(field('m_SlideClip', asset='Assets/_Project/Audio/Placeholder/slide.wav'))
    ops.append(field('m_JumpClip', asset='Assets/kenney_impact-sounds/Audio/impactSoft_heavy_001.ogg'))
    ops.append(field('m_JuryClip', asset='Assets/_Project/Audio/Placeholder/perfect_chime.wav'))
    return ops

# ---------------------------------------------------------------- Yo-yo : Tremplin + Tour-aimant
# Une grue de 13 m au bord de la voie, sa flèche au-dessus d'elle ; le yo-yo de cuivre aimanté au bout de son fil. Au niveau 3,
# le lustre : un anneau doré qui tourne sous la flèche, six fils de plus.
YO_MAST_X = -3.7
YO_JIB_Y = 10.1

def yo_tie(parent, name, a, b, role='metal', radius=0.05):
    # Un hauban (un cylindre) de a à b, dans le plan x-y.
    dx, dy = b[0] - a[0], b[1] - a[1]
    length = math.hypot(dx, dy)
    angle = math.degrees(math.atan2(-dx, dy))
    return cyl(parent, name, [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, a[2]], [radius * 2, length, radius * 2], role=role, rot=[0, 0, angle])

def yo_hook(parent, suffix, at, drop, disc):
    # Une ancre (repère du haut), le fil (repère en haut, 1 m de fil à l'échelle 1), le yo-yo : deux disques de cuivre sur un
    # axe doré (axe X), l'aimant rouge dessous.
    p = parent + '/' if parent else ''
    ops = [empty(parent, 'Ancre' + suffix, at), empty(parent, 'Fil' + suffix, at)]
    ops.append(cyl(p + 'Fil' + suffix, 'Corde', [0, -0.5, 0], [0.07, 1.0, 0.07], role='corde'))
    y = [at[0], at[1] - drop, at[2]]
    ops.append(empty(parent, 'Yo-yo' + suffix, y))
    h = p + 'Yo-yo' + suffix
    t = disc * 0.17
    for s in (-1, 1):
        ops.append(cyl(h, 'Disque', [s * (t / 2 + 0.06), 0, 0], [disc, t, disc], role='cuivre', rot=[0, 0, 90]))
        ops.append(cyl(h, 'Cercle', [s * (t + 0.06 + 0.006 - 0.012), 0, 0], [disc * 0.55, 0.024, disc * 0.55], role='accent', rot=[0, 0, 90]))
    ops.append(cyl(h, 'Axe', [0, 0, 0], [disc * 0.26, 0.14, disc * 0.26], role='accent', rot=[0, 0, 90]))
    ops.append(box(h, 'Aimant', [0, -disc / 2 - 0.12 + 0.03, 0], [disc * 0.3, 0.24, disc * 0.4], role='rouge'))
    return ops

def yoyo():
    ops = []
    x = YO_MAST_X
    # Le socle de pierre et sa plaque dorée ; le mât de bois aux montants de fer et ses barreaux.
    ops.append(box('', 'Socle', [x, 0.4 - 0.006, 0], [2.2, 0.8, 2.2], role='pierre'))
    ops.append(box('', 'Plaque', [x, 0.818, 0], [2.24, 0.06, 2.24], role='accent'))
    bottom, top = 0.842, 9.842
    ops.append(box('', 'Mât', [x, (bottom + top) / 2, 0], [0.8, top - bottom, 0.8], role='bois'))
    for dx in (-0.42, 0.42):
        for dz in (-0.42, 0.42):
            ops.append(box('', 'Montant', [x + dx, (bottom + top) / 2, dz], [0.14, top - bottom, 0.14], role='metal'))
    for k in range(6):
        y = 1.6 + k * 1.4
        for dz in (-0.42, 0.42):
            ops.append(box('', 'Barreau', [x, y, dz], [0.96, 0.08, 0.08], role='metal', shade=0.8))
    # La cabine rouge au toit doré, la flèche, la contre-flèche et son contrepoids, la pointe et les haubans.
    ops.append(box('', 'Cabine', [x, top - 0.006 + 0.6, 0], [1.5, 1.2, 1.5], role='rouge'))
    roof = top - 0.006 + 1.2
    ops.append(box('', 'Toit', [x, roof + 0.06 - 0.006, 0], [1.7, 0.12, 1.7], role='accent'))
    ops.append(box('', 'Flèche', [-1.5, YO_JIB_Y, 0], [5.6, 0.5, 0.6], role='metal'))
    ops.append(box('', 'Contre-flèche', [-5.7, YO_JIB_Y, 0], [2.6, 0.4, 0.6], role='metal', shade=0.85))
    ops.append(box('', 'Contrepoids', [-6.4, YO_JIB_Y - 0.2 - 0.7 + 0.006, 0], [1.0, 1.4, 1.0], role='pierre', shade=0.85))
    peak = roof + 0.12 - 0.012
    ops.append(box('', 'Pointe', [x, peak + 1.2, 0], [0.3, 2.4, 0.3], role='metal'))
    tip = (x, peak + 2.3, 0)
    ops.append(yo_tie('', 'Hauban', tip, (1.15, YO_JIB_Y + 0.2, 0)))
    ops.append(yo_tie('', 'Hauban', tip, (-6.9, YO_JIB_Y + 0.15, 0)))
    ops.append(box('', 'Fanion', [x, peak + 2.1, 0.62], [0.05, 0.5, 1.0], role='accent'))
    # La poulie dorée sous le bout de la flèche, et le yo-yo du milieu.
    ops.append(cyl('', 'Poulie', [0, YO_JIB_Y - 0.25 - 0.3 + 0.006, 0], [0.6, 0.25, 0.6], role='accent', rot=[90, 0, 0]))
    ops += yo_hook('', '', [0, YO_JIB_Y - 0.8, 0], 2.0, 1.4)
    # Niveau 2 : la flèche rayée de rouge, des ressorts de cuivre sous la poulie, un second contrepoids.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    for k in range(5):
        ops.append(box('Niveau 2', 'Rayure', [-3.6 + k * 1.1, YO_JIB_Y, 0], [0.5, 0.52, 0.62], role='rouge'))
    ops.append(box('Niveau 2', 'Contrepoids', [-5.3, YO_JIB_Y - 0.2 - 0.5 + 0.006, 0], [0.8, 1.0, 0.8], role='pierre', shade=0.7))
    for k in range(4):
        ops.append(cyl('Niveau 2', 'Ressort', [0, YO_JIB_Y - 0.62 - k * 0.09, 0], [0.32, 0.04, 0.32], role='cuivre'))
    # Niveau 3, le lustre : un anneau doré à six fils, qui tourne sous la flèche.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    ops.append(empty('Niveau 3', 'Lustre', [0, YO_JIB_Y - 1.2, 0]))
    L = 'Niveau 3/Lustre'
    ops.append(cyl(L, 'Tige', [0, 0.5, 0], [0.16, 1.0, 0.16], role='metal'))
    ops.append(cyl(L, 'Moyeu', [0, 0, 0], [0.8, 0.4, 0.8], role='accent'))
    radius = 2.6
    for k in range(12):
        a = math.radians(k * 30 + 15)
        ops.append(box(L, 'Anneau', [math.cos(a) * radius, 0, math.sin(a) * radius], [0.18, 0.18, 1.42], role='accent',
                       rot=[0, -k * 30 - 15, 0]))
    for k in range(6):
        a = math.radians(k * 60)
        ops.append(box(L, 'Rayon', [math.cos(a) * radius / 2, 0, math.sin(a) * radius / 2], [radius, 0.1, 0.1], role='metal',
                       rot=[0, -k * 60, 0]))
        ops += yo_hook(L, ' ' + str(k + 1), [math.cos(a) * (radius - 0.15), -0.1, math.sin(a) * (radius - 0.15)], 2.0, 0.95)
    hooks = [str(k + 1) for k in range(6)]
    ops.append(field('m_Anchors', ['Ancre'] + [L + '/Ancre ' + k for k in hooks]))
    ops.append(field('m_Cables', ['Fil'] + [L + '/Fil ' + k for k in hooks]))
    ops.append(field('m_YoYos', ['Yo-yo'] + [L + '/Yo-yo ' + k for k in hooks]))
    ops.append(field('m_Hub', [L]))
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_SlamClip', asset='Assets/kenney_impact-sounds/Audio/impactPunch_heavy_004.ogg'))
    ops.append(field('m_GrabClip', asset='Assets/kenney_impact-sounds/Audio/impactMetal_light_002.ogg'))
    ops.append(field('m_FlingClip', asset='Assets/507466__danjocross__clean-fast-swoosh.aiff'))
    return ops

# ---------------------------------------------------------------- Avalanche : Mortier + Tour de givre
# Un gros mortier de glace sur un socle de pierre enneigé ; sa tourelle tourne vers la cible ; une boule de neige chargée dans la
# gueule (le code la copie à chaque tir). Au niveau 3, un fût plus large et des cristaux de 4 m.
AV_ELEV = 55          # l'élévation du fût (degrés)
AV_PIVOT = 1.7        # la hauteur des tourillons, dans la tourelle
AV_BALL = 1.3         # le diamètre de la boule chargée (m_TemplateDiameter)

def av_along(d, t, base):
    return [base[0] + d[0] * t, base[1] + d[1] * t, base[2] + d[2] * t]

def avalanche():
    ops = []
    # Le socle rond, son liseré doré, des congères autour.
    ops.append(cyl('', 'Socle', [0, 0.294, 0], [4.4, 0.6, 4.4], role='pierre'))
    ops.append(cyl('', 'Liseré', [0, 0.628, 0], [4.5, 0.08, 4.5], role='accent'))
    for k in range(6):
        a = math.radians(k * 60 + 20)
        ops.append(sph('', 'Congère', [math.cos(a) * 2.25, 0.25, math.sin(a) * 2.25], [1.5, 0.8, 1.1], role='neige',
                       rot=[0, -k * 60 - 20, 0]))
    # La tourelle : deux joues de glace, les tourillons dorés, le fût de glace cerclé d'or, la gueule enneigée, la boule.
    T = 'Tourelle'
    ops.append(empty('', T, [0, 0.6, 0]))
    ops.append(cyl(T, 'Plateau', [0, 0.1 + 0.022, 0], [4.2, 0.2, 4.2], role='metal'))
    for s in (-1, 1):
        ops.append(box(T, 'Joue', [s * 1.05, 0.216 + 0.9 - 0.006, 0], [0.45, 1.8, 1.7], role='glace'))
        ops.append(cyl(T, 'Tourillon', [s * 0.92, AV_PIVOT, 0], [0.5, 0.5, 0.5], role='accent', rot=[0, 0, 90]))
        for k, h in enumerate((1.3, 0.9)):
            ops.append(sph(T, 'Cristal', [s * 1.05, 0.216 + 1.8 + h / 2 - 0.15, -0.4 + k * 0.6], [0.3, h, 0.3], role='glace',
                           shade=1.15, rot=[(k - 0.5) * 20, 0, s * 15]))
    e = math.radians(AV_ELEV)
    d = (0, math.sin(e), math.cos(e))
    rot = [90 - AV_ELEV, 0, 0]
    pivot = (0, AV_PIVOT, 0)
    ops.append(cyl(T, 'Fût', av_along(d, 0.5, pivot), [1.6, 3.0, 1.6], role='glace', rot=rot))
    for t in (-0.6, 0.5, 1.6):
        ops.append(cyl(T, 'Cercle', av_along(d, t, pivot), [1.68, 0.16, 1.68], role='accent', rot=rot))
    ops.append(cyl(T, 'Lèvre', av_along(d, 2.0 - 0.006, pivot), [1.95, 0.25, 1.95], role='neige', rot=rot))
    ops.append(cyl(T, 'Culot', av_along(d, -1.0 + 0.006, pivot), [1.3, 0.3, 1.3], role='metal', rot=rot))
    ops.append(empty(T, 'Gueule', av_along(d, 2.4, pivot)))
    ops.append(sph(T, 'Boule', av_along(d, 2.15, pivot), [AV_BALL, AV_BALL, AV_BALL], role='neige'))
    # Les pièces des niveaux sont sur le plateau qui tourne (une seule paire « Niveau 2 » / « Niveau 3 » par tour).
    # Niveau 2 : une couronne de pics de glace sur le plateau, un manchon de neige sur le fût.
    top = 0.222
    N2, N3 = T + '/Niveau 2', T + '/Niveau 3'
    ops.append(empty(T, 'Niveau 2', [0, 0, 0]))
    for k in range(8):
        a = math.radians(k * 45 + 22.5)
        ops.append(sph(N2, 'Pic', [math.cos(a) * 1.85, top + 0.5 - 0.1, math.sin(a) * 1.85], [0.3, 1.0, 0.3], role='glace',
                       shade=1.1, rot=[math.sin(a) * 18, 0, -math.cos(a) * 18]))
    ops.append(cyl(N2, 'Manchon', av_along(d, -0.05, pivot), [1.76, 0.5, 1.76], role='neige', rot=rot))
    # Niveau 3, l'avalanche : un fût plus large par-dessus, trois boules en réserve derrière, de grands cristaux.
    ops.append(empty(T, 'Niveau 3', [0, 0, 0]))
    ops.append(cyl(N3, 'Gros fût', av_along(d, 1.2, pivot), [2.3, 1.4, 2.3], role='glace', shade=0.9, rot=rot))
    ops.append(cyl(N3, 'Gros cercle', av_along(d, 1.95, pivot), [2.4, 0.2, 2.4], role='accent', rot=rot))
    for x, y, z, size in ((-0.6, top + 0.5, -1.65, 1.1), (0.6, top + 0.5, -1.65, 1.1), (0.0, top + 1.3, -1.65, 1.0)):
        ops.append(sph(N3, 'Réserve', [x, y, z], [size, size, size], role='neige'))
    for a, h in ((140, 4.2), (215, 3.4), (325, 3.8), (35, 3.0)):
        r = math.radians(a)
        ops.append(sph(N3, 'Grand cristal', [math.cos(r) * 1.75, top + h / 2 - 0.3, math.sin(r) * 1.75], [0.6, h, 0.6], role='glace',
                       shade=1.2, rot=[math.sin(r) * 10, 0, -math.cos(r) * 10]))
    ops.append(field('m_Turret', [T]))
    ops.append(field('m_Muzzle', [T + '/Gueule']))
    ops.append(field('m_BallTemplate', [T + '/Boule']))
    ops.append({'op': 'field', 'field': 'm_TemplateDiameter', 'value': AV_BALL})
    ops.append(field('m_LevelParts', [N2, N3]))
    ops.append(field('m_FireClip', asset='Assets/kenney_impact-sounds/Audio/impactPlate_heavy_001.ogg'))
    ops.append(field('m_BurstClip', asset='Assets/_Project/Audio/Placeholder/projectile_break.wav'))
    return ops

# ---------------------------------------------------------------- Feu d'artifice : Mortier + Phare
# Un phare rayé rouge et blanc de 9 m, sa lanterne qui tourne ; une couronne de fusées autour de la galerie (le code en copie
# une à chaque tir et cache l'original le temps de la recharge). Niveau 2 : un balcon de fusées à deux étages ; niveau 3 : la
# batterie du bouquet final et une étoile dorée.
FA_BASE = 0.494
FA_SECTION = 1.4
FA_DIAMS = [2.6, 2.4, 2.2, 2.0, 1.8]
FA_COLORS = ['rouge', 'bleu', 'vert', 'accent']

def fa_rocket(parent, name, at, tilt_dir, tilt, color, length=0.9, stages=1):
    # Une fusée : sa baguette, son corps (un ou deux étages), son nez doré ; son repère est à la base du corps, l'axe Y vers le nez.
    ops = [empty(parent, name, at, rot=[tilt_dir[1] * tilt, 0, -tilt_dir[0] * tilt])]
    f = parent + '/' + name if parent else name
    ops.append(cyl(f, 'Baguette', [0, -0.5 + 0.03, 0], [0.04, 1.0, 0.04], role='bois'))
    seg = length / stages
    for k in range(stages):
        ops.append(cyl(f, 'Corps', [0, seg * (k + 0.5) - k * 0.006, 0], [0.24 - k * 0.03, seg, 0.24 - k * 0.03],
                       role=color if k == 0 else 'blanc'))
    ops.append(sph(f, 'Nez', [0, length - 0.006 * (stages - 1) + 0.14, 0], [0.22, 0.36, 0.22], role='accent'))
    return ops

def feu_artifice():
    ops = []
    ops.append(cyl('', 'Socle', [0, 0.244, 0], [3.8, 0.5, 3.8], role='pierre'))
    # La tour rayée, de plus en plus fine.
    y = FA_BASE - 0.006
    for k, diam in enumerate(FA_DIAMS):
        ops.append(cyl('', 'Bande', [0, y + FA_SECTION / 2, 0], [diam, FA_SECTION, diam], role='rouge' if k % 2 == 0 else 'blanc'))
        y += FA_SECTION - 0.006
    top = y + 0.006
    # La galerie, son liseré doré, ses pavillons rayés ; la lanterne qui tourne sous un dôme doré.
    ops.append(cyl('', 'Galerie', [0, top + 0.1 - 0.006, 0], [3.6, 0.2, 3.6], role='metal'))
    ops.append(cyl('', 'Liseré', [0, top + 0.1 - 0.006, 0], [3.7, 0.12, 3.7], role='accent'))
    g = top + 0.2 - 0.012
    for k in range(4):
        a = math.radians(k * 90 + 22.5)
        x, z = math.cos(a) * 1.7, math.sin(a) * 1.7
        ops.append(cyl('', 'Hampe', [x, g + 0.75 - 0.006, z], [0.06, 1.5, 0.06], role='metal'))
        for s in range(3):
            ops.append(box('', 'Pavillon', [x, g + 1.2 - s * 0.16, z + 0.33], [0.03, 0.16, 0.6], role='rouge' if s % 2 == 0 else 'blanc'))
    ops.append(cyl('', 'Lanterne', [0, g + 0.7 - 0.006, 0], [1.3, 1.4, 1.3], role='lumiere'))
    ops.append(empty('', 'Phare', [0, g + 0.7, 0]))
    for s in (-1, 1):
        ops.append(box('Phare', 'Réflecteur', [s * 0.5, 0, 0], [0.34, 1.0, 0.7], role='accent', rot=[0, 0, 0]))
    ops.append(sph('', 'Dôme', [0, g + 1.4 - 0.012 + 0.3, 0], [1.5, 0.7, 1.5], role='accent'))
    # La couronne de fusées autour de la galerie, penchées vers l'extérieur.
    rockets = []
    for k in range(8):
        a = math.radians(k * 45)
        d = (math.cos(a), math.sin(a))
        name = 'Fusée ' + str(k + 1)
        ops.append(cyl('', 'Tube', [d[0] * 1.45, g + 0.25 - 0.006, d[1] * 1.45], [0.3, 0.5, 0.3], role='metal', shade=0.8))
        ops += fa_rocket('', name, [d[0] * 1.45, g + 0.5, d[1] * 1.45], d, 14, FA_COLORS[k % 4])
        rockets.append(name)
    # Niveau 2 : un balcon à mi-hauteur, six fusées à deux étages.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    by = FA_BASE + 2 * FA_SECTION + 0.3
    ops.append(cyl('Niveau 2', 'Balcon', [0, by, 0], [3.4, 0.16, 3.4], role='metal'))
    ops.append(cyl('Niveau 2', 'Liseré', [0, by, 0], [3.5, 0.1, 3.5], role='accent'))
    for k in range(6):
        a = math.radians(k * 60 + 30)
        d = (math.cos(a), math.sin(a))
        name = 'Fusée ' + str(k + 9)
        ops += fa_rocket('Niveau 2', name, [d[0] * 1.45, by + 0.6, d[1] * 1.45], d, 22, FA_COLORS[(k + 1) % 4], length=1.1, stages=2)
        rockets.append('Niveau 2/' + name)
    # Niveau 3, le bouquet final : une batterie de douze fusées dans sa caisse dorée, une grande étoile au sommet.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    bx, bz = 0.0, -2.45
    ops.append(box('Niveau 3', 'Caisse', [bx, 0.4 - 0.006, bz], [2.6, 0.8, 1.2], role='bois'))
    ops.append(box('Niveau 3', 'Cerclage', [bx, 0.4 - 0.006, bz], [2.64, 0.12, 1.24], role='accent'))
    for i in range(6):
        for j in range(2):
            name = 'Fusée ' + str(15 + i * 2 + j)
            x = bx - 1.05 + i * 0.42
            z = bz - 0.25 + j * 0.5
            ops += fa_rocket('Niveau 3', name, [x, 0.8 + 0.5 - 0.03 - 0.006, z], (0, -1), 8 + j * 8, FA_COLORS[(i + j) % 4])
            rockets.append('Niveau 3/' + name)
    star_y = g + 1.4 + 0.7 + 0.6
    ops.append(cyl('Niveau 3', 'Tige', [0, star_y - 0.5, 0], [0.08, 0.8, 0.08], role='accent'))
    for k in range(5):
        ops.append(box('Niveau 3', 'Branche', [0, star_y + 0.006, 0], [0.18, 1.2, 0.12], role='accent', shade=1.2, rot=[0, 0, k * 36]))
    ops.append(field('m_Rockets', rockets))
    ops.append(field('m_Lantern', ['Phare']))
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_LaunchClip', asset='Assets/507466__danjocross__clean-fast-swoosh.aiff'))
    ops.append(field('m_BurstClip', asset='Assets/_Project/Audio/Placeholder/explosion.wav'))
    return ops

# ---------------------------------------------------------------- Chant des sirènes : Phare + Bulle
# Un phare de nacre rayé de rose, 10 m ; sa lanterne qui tourne porte le faisceau (allongé par le code jusqu'à l'ennemi) ; une
# sirène de bronze au sommet ; des bulles roses au pied. Niveau 2 : un balcon de coquillages ; niveau 3 : la grande conque
# dorée derrière la sirène et sa perle.
SI_DIAMS = [2.7, 2.5, 2.3, 2.1, 1.9]

def sirenes():
    ops = []
    ops.append(cyl('', 'Socle', [0, 0.244, 0], [3.8, 0.5, 3.8], role='pierre'))
    ops.append(cyl('', 'Liseré', [0, 0.488, 0], [3.9, 0.06, 3.9], role='accent'))
    y = 0.494 - 0.006 + 0.012
    for k, diam in enumerate(SI_DIAMS):
        ops.append(cyl('', 'Bande', [0, y + 0.7, 0], [diam, 1.4, diam], role='nacre' if k % 2 == 0 else 'rose'))
        y += 1.4 - 0.006
    top = y + 0.006
    ops.append(cyl('', 'Galerie', [0, top + 0.1 - 0.006, 0], [3.2, 0.2, 3.2], role='bronze'))
    ops.append(cyl('', 'Liseré', [0, top + 0.1 - 0.006, 0], [3.3, 0.12, 3.3], role='accent'))
    g = top + 0.2 - 0.012
    for k in range(10):
        a = math.radians(k * 36)
        ops.append(cyl('', 'Balustre', [math.cos(a) * 1.5, g + 0.4 - 0.006, math.sin(a) * 1.5], [0.08, 0.8, 0.08], role='bronze'))
    # La lanterne (repère qui tourne) et son faisceau : un repère à son centre, le cylindre rose le long de son axe Z.
    ops.append(cyl('', 'Verrière', [0, g + 0.7 - 0.006, 0], [1.4, 1.4, 1.4], role='rose', shade=1.3))
    ops.append(empty('', 'Lanterne', [0, g + 0.7, 0]))
    for s in (-1, 1):
        ops.append(box('Lanterne', 'Miroir', [s * 0.75, 0, 0], [0.3, 1.0, 0.8], role='accent'))
    ops.append(empty('Lanterne', 'Faisceau', [0, 0, 0.75]))
    ops.append(cyl('Lanterne/Faisceau', 'Lumière', [0, 0, 0.5], [0.3, 1.0, 0.3], role='rose', shade=1.4, rot=[90, 0, 0]))
    roof = g + 1.4 - 0.012
    ops.append(sph('', 'Dôme', [0, roof + 0.3, 0], [1.6, 0.7, 1.6], role='bronze'))
    # La sirène de bronze sur le dôme : sa queue enroulée, son buste, sa tête, ses cheveux, un bras levé.
    s0 = roof + 0.6
    ops.append(sph('', 'Nageoire', [0.0, s0 + 0.05, -0.35], [0.7, 0.18, 0.4], role='bronze', rot=[20, 0, 0]))
    ops.append(sph('', 'Queue', [0.0, s0 + 0.35, -0.12], [0.42, 0.9, 0.38], role='bronze', rot=[-25, 0, 0]))
    ops.append(sph('', 'Buste', [0.0, s0 + 0.95, 0.05], [0.45, 0.75, 0.36], role='bronze', shade=1.1))
    ops.append(sph('', 'Tête', [0.0, s0 + 1.45, 0.08], [0.3, 0.34, 0.3], role='bronze', shade=1.1))
    ops.append(sph('', 'Cheveux', [0.0, s0 + 1.35, -0.06], [0.36, 0.6, 0.28], role='accent', rot=[-15, 0, 0]))
    ops.append(sph('', 'Bras', [0.24, s0 + 1.4, 0.1], [0.12, 0.62, 0.12], role='bronze', rot=[0, 0, -25]))
    # Les bulles roses au pied du phare (l'héritage de la bulle).
    for k, (a, size, h) in enumerate(((20, 0.7, 0.35), (75, 0.45, 0.25), (140, 0.8, 0.4), (215, 0.5, 0.25), (290, 0.65, 0.3))):
        r = math.radians(a)
        ops.append(sph('', 'Bulle', [math.cos(r) * 1.75, 0.494 + h - 0.05, math.sin(r) * 1.75], [size, size, size], role='rose', shade=1.25))
    # Niveau 2 : un balcon de coquillages à mi-hauteur, des bulles de plus.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    by = 0.5 + 2 * 1.394 + 0.2
    ops.append(cyl('Niveau 2', 'Balcon', [0, by, 0], [3.2, 0.16, 3.2], role='bronze'))
    for k in range(8):
        a = math.radians(k * 45 + 22.5)
        ops.append(sph('Niveau 2', 'Coquillage', [math.cos(a) * 1.5, by + 0.2, math.sin(a) * 1.5], [0.45, 0.3, 0.2], role='nacre',
                       rot=[0, -k * 45 - 22.5 + 90, 0]))
    for k, (a, size) in enumerate(((50, 0.9), (180, 1.0), (250, 0.7))):
        r = math.radians(a)
        ops.append(sph('Niveau 2', 'Grosse bulle', [math.cos(r) * 2.0, 0.494 + size / 2 - 0.06, math.sin(r) * 2.0], [size, size, size],
                       role='rose', shade=1.35))
    # Niveau 3, la mêlée : la grande conque dorée derrière la sirène, et la perle qu'elle brandit.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    for k in range(7):
        ops.append(box('Niveau 3', 'Conque', [0, s0 + 0.9, -0.55], [0.32, 2.4, 0.08], role='accent', rot=[0, 0, (k - 3) * 22]))
    ops.append(sph('Niveau 3', 'Perle', [0.42, s0 + 1.85, 0.12], [0.5, 0.5, 0.5], role='nacre', shade=1.3))
    ops.append(field('m_Lantern', ['Lanterne']))
    ops.append(field('m_Beam', ['Lanterne/Faisceau']))
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_SongClip', asset='Assets/_Project/Audio/Placeholder/enemy_cast.wav'))
    ops.append(field('m_PopClip', asset='Assets/kenney_interfaceSounds/Audio/pluck_001.ogg'))
    return ops

# ---------------------------------------------------------------- Loupe : Phare + Baliste
# Une tour de bois de 6 m ; au sommet, la fourche de laiton (elle tourne autour de Y) tient la loupe (elle s'incline), 3,2 m de
# diamètre. Niveau 2 : une petite baliste sur la fourche ; niveau 3 : le rayon (allongé par le code jusqu'à l'ennemi).
LO_TOP = 6.0          # le plancher du haut de la tour
LO_R = 1.6            # le rayon de l'anneau de la loupe
LO_AXIS = 2.5         # la hauteur de l'axe de la loupe au-dessus de la fourche

def loupe():
    ops = []
    # La tour : quatre poteaux sur des dés de pierre, des traverses, deux planchers, une échelle.
    for sx in (-1, 1):
        for sz in (-1, 1):
            x, z = sx * 1.2, sz * 1.2
            ops.append(box('', 'Dé', [x, 0.2 - 0.006, z], [0.6, 0.4, 0.6], role='pierre'))
            ops.append(box('', 'Poteau', [x, 0.394 + LO_TOP / 2 - 0.006, z], [0.3, LO_TOP, 0.3], role='bois'))
    for y in (2.2, 4.2):
        for sx in (-1, 1):
            ops.append(box('', 'Traverse', [sx * 1.2, y, 0], [0.18, 0.2, 2.6], role='bois', shade=0.85))
            ops.append(box('', 'Traverse', [0, y + 0.25, sx * 1.2], [2.6, 0.2, 0.18], role='bois', shade=0.85))
    ops.append(box('', 'Plancher', [0, 3.2, 0], [2.9, 0.14, 2.9], role='bois', shade=0.75))
    floor = 0.394 + LO_TOP - 0.006
    ops.append(box('', 'Plateforme', [0, floor + 0.1 - 0.012, 0], [3.2, 0.2, 3.2], role='bois', shade=0.9))
    ops.append(box('', 'Bordure', [0, floor + 0.1 - 0.012, 0], [3.26, 0.12, 3.26], role='accent'))
    # La fourche de laiton (repère qui tourne) : un pivot, un socle, deux bras ; la loupe entre eux.
    base = floor + 0.2 - 0.024
    F = 'Fourche'
    ops.append(empty('', F, [0, base, 0]))
    ops.append(cyl(F, 'Pivot', [0, 0.15 - 0.006, 0], [1.2, 0.3, 1.2], role='laiton'))
    ops.append(box(F, 'Socle', [0, 0.3 + 0.15 - 0.012, 0], [3.9, 0.3, 0.5], role='laiton', shade=0.85))
    for s in (-1, 1):
        ops.append(box(F, 'Bras', [s * (LO_R + 0.3), 0.6 + (LO_AXIS - 0.6) / 2 + 0.1, 0], [0.24, LO_AXIS - 0.3, 0.36], role='laiton'))
        ops.append(cyl(F, 'Moyeu', [s * (LO_R + 0.18), LO_AXIS, 0], [0.42, 0.3, 0.42], role='accent', rot=[0, 0, 90]))
    # La loupe (repère qui s'incline ; son axe Z regarde les ennemis) : l'anneau de laiton, le verre.
    L = F + '/Loupe'
    ops.append(empty(F, 'Loupe', [0, LO_AXIS, 0]))
    ops.append(cyl(L, 'Verre', [0, 0, 0], [2 * LO_R - 0.1, 0.1, 2 * LO_R - 0.1], role='verre', rot=[90, 0, 0]))
    for k in range(16):
        a = math.radians(k * 22.5)
        ops.append(box(L, 'Anneau', [math.cos(a) * LO_R, math.sin(a) * LO_R, 0], [0.26, 0.7, 0.3], role='laiton',
                       rot=[0, 0, k * 22.5]))
    # Le phare : une petite lanterne dorée au-dessus de l'anneau.
    ops.append(sph(L, 'Lanterne', [0, LO_R + 0.3, 0], [0.5, 0.5, 0.5], role='lumiere'))
    # Niveau 2 : une baliste sur la fourche, devant la loupe ; son carreau part de la Pointe.
    N2 = F + '/Niveau 2'
    ops.append(empty(F, 'Niveau 2', [0, 0, 0]))
    ops.append(box(N2, 'Fût', [0, 0.6 + 0.12 - 0.012, 1.3], [0.3, 0.24, 1.6], role='bois'))
    for s in (-1, 1):
        ops.append(box(N2, 'Arc', [s * 0.55, 0.72, 1.85], [1.0, 0.12, 0.12], role='metal', rot=[0, s * 22, 0]))
    ops.append(cyl(N2, 'Carreau', [0, 0.84, 1.4], [0.06, 1.4, 0.06], role='metal', rot=[90, 0, 0]))
    ops.append(empty(N2, 'Pointe', [0, 0.84, 2.2]))
    # Niveau 3, la fourmi : le rayon de soleil (un repère au centre du verre, le cylindre le long de son axe Z), des rayons dorés.
    N3 = L + '/Niveau 3'
    ops.append(empty(L, 'Niveau 3', [0, 0, 0]))
    ops.append(empty(N3, 'Rayon', [0, 0, 0.1]))
    ops.append(cyl(N3 + '/Rayon', 'Lumière', [0, 0, 0.5], [0.18, 1.0, 0.18], role='lumiere', shade=1.3, rot=[90, 0, 0]))
    for k in range(8):
        a = math.radians(k * 45 + 11.25)
        ops.append(box(N3, 'Rayon doré', [math.cos(a) * (LO_R + 0.45), math.sin(a) * (LO_R + 0.45), -0.02], [0.14, 0.6, 0.1],
                       role='accent', shade=1.2, rot=[0, 0, k * 45 + 11.25 - 90]))
    ops.append(field('m_Yoke', [F]))
    ops.append(field('m_Lens', [L]))
    ops.append(field('m_Bow', [N2 + '/Pointe']))
    ops.append(field('m_Ray', [N3 + '/Rayon']))
    ops.append(field('m_BoltPrefab', asset='Assets/_Project/Prefabs/Structures/Ballista Bolt.prefab'))
    ops.append(field('m_LevelParts', [N2, N3]))
    ops.append(field('m_BoltClip', asset='Assets/kenney_impact-sounds/Audio/impactWood_heavy_002.ogg'))
    ops.append(field('m_IgniteClip', asset='Assets/_Project/Audio/Placeholder/fire_loop.wav'))
    return ops

# ---------------------------------------------------------------- Carquois géant : Baliste + Lance-filet
# Un mât de 9,5 m tend un grand filet comme une voile (4,2 × 6,6 m) ; dix flèches y sont plantées (le code en montre une par
# flèche rattrapée) ; devant, l'arbalète à six arcs, pointée vers le ciel. Niveau 2 : un tonneau de carreaux rouges ; niveau 3,
# la nuée : une hune dorée et une voile de filet de plus, jusqu'à 13 m.
CQ_NET_Z = -1.3
CQ_LOW = 2.2
CQ_HIGH = 8.8

def cq_net(parent, low, high, half, z):
    ops = []
    n = int(round(2 * half / 0.5))
    for i in range(n + 1):
        x = -half + i * 2 * half / n
        ops.append(box(parent, 'Maille', [x, (low + high) / 2, z], [0.05, high - low, 0.05], role='corde'))
    rows = int(round((high - low) / 0.55))
    for j in range(1, rows):
        y = low + j * (high - low) / rows
        ops.append(box(parent, 'Maille', [0, y, z], [2 * half, 0.04, 0.04], role='corde', shade=0.9))
    return ops

def cq_arrow(parent, name, at, tilt):
    # Une flèche plantée dans le filet, la pointe dedans, l'empennage côté joueur (-z), d'où viennent les flèches ratées.
    ops = [empty(parent, name, at, rot=[90 + tilt[0], tilt[1], 0])]
    f = parent + '/' + name if parent else name
    ops.append(cyl(f, 'Fût', [0, -0.6, 0], [0.06, 1.2, 0.06], role='bois'))
    ops.append(sph(f, 'Pointe', [0, 0.02, 0], [0.1, 0.2, 0.1], role='metal'))
    for k in range(3):
        ops.append(box(f, 'Plume', [0, -1.08, 0], [0.016, 0.24, 0.14], role='rouge', rot=[0, k * 60, 0]))
    return ops

def carquois():
    ops = []
    ops.append(box('', 'Socle', [0, 0.194, 0], [4.2, 0.4, 4.2], role='pierre'))
    ops.append(box('', 'Plaque', [0, 0.418, 0], [4.26, 0.06, 4.26], role='accent'))
    floor = 0.442
    # Le mât, ses cercles dorés, les deux vergues ; le filet tendu entre elles.
    ops.append(cyl('', 'Mât', [0, floor + 4.75 - 0.006, CQ_NET_Z - 0.2], [0.34, 9.5, 0.34], role='bois'))
    for y in (2.0, 5.0, 8.0):
        ops.append(cyl('', 'Cercle', [0, y, CQ_NET_Z - 0.2], [0.4, 0.12, 0.4], role='accent'))
    for y in (CQ_LOW, CQ_HIGH):
        ops.append(box('', 'Vergue', [0, y, CQ_NET_Z - 0.06], [4.6, 0.2, 0.2], role='bois', shade=0.85))
    ops += cq_net('', CQ_LOW, CQ_HIGH, 2.1, CQ_NET_Z)
    ops.append(cyl('', 'Pomme', [0, floor + 9.5 + 0.1 - 0.012, CQ_NET_Z - 0.2], [0.44, 0.2, 0.44], role='accent'))
    ops.append(box('', 'Fanion', [0, floor + 9.1, CQ_NET_Z - 0.2 + 0.62], [0.05, 0.6, 1.1], role='accent'))
    # Les dix flèches plantées dans le filet.
    spots = [(-1.5, 3.1), (0.6, 3.6), (1.6, 4.4), (-0.7, 4.9), (0.2, 5.8), (-1.6, 6.1), (1.2, 6.6), (-0.4, 7.3), (1.7, 7.9), (-1.2, 8.1)]
    names = []
    for k, (x, y) in enumerate(spots):
        name = 'Flèche ' + str(k + 1)
        ops += cq_arrow('', name, [x, y, CQ_NET_Z - 0.06], ((k % 3 - 1) * 8, (k % 2 * 2 - 1) * 10))
        names.append(name)
    # L'arbalète à six arcs sur son fût de pierre, pointée vers le ciel.
    ops.append(cyl('', 'Fût de pierre', [0, floor + 0.6 - 0.006, 0.7], [1.1, 1.2, 1.1], role='pierre', shade=0.9))
    ops.append(cyl('', 'Pivot', [0, floor + 1.2 + 0.1 - 0.012, 0.7], [1.2, 0.2, 1.2], role='accent'))
    A = 'Arbalète'
    ops.append(empty('', A, [0, floor + 1.6, 0.7], rot=[-35, 0, 0]))
    ops.append(box(A, 'Arbrier', [0, 0, 0.6], [0.45, 0.4, 2.6], role='bois'))
    for k in range(3):
        y = -0.1 + k * 0.32
        for s in (-1, 1):
            ops.append(box(A, 'Arc', [s * 0.75, y, 1.55 - k * 0.25], [1.25, 0.1, 0.14], role='metal', rot=[0, s * 24, 0]))
        ops.append(box(A, 'Corde', [0, y, 1.05 - k * 0.25], [2.1, 0.03, 0.03], role='corde'))
    ops.append(empty(A, 'Bout', [0, 0.2, 1.95]))
    ops.append(box(A, 'Garde', [0, 0, 1.92], [0.6, 0.6, 0.12], role='accent'))
    # Niveau 2 : un tonneau de carreaux à pointe rouge à côté de l'arbalète.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    ops.append(cyl('Niveau 2', 'Tonneau', [1.45, floor + 0.55 - 0.006, 0.9], [0.8, 1.1, 0.8], role='bois'))
    for y in (0.25, 0.85):
        ops.append(cyl('Niveau 2', 'Cercle', [1.45, floor + y, 0.9], [0.84, 0.08, 0.84], role='accent'))
    for k in range(5):
        a = math.radians(k * 72)
        x, z = 1.45 + math.cos(a) * 0.2, 0.9 + math.sin(a) * 0.2
        ops.append(cyl('Niveau 2', 'Carreau', [x, floor + 1.1 + 0.3 - 0.03, z], [0.05, 0.9, 0.05], role='bois', rot=[math.sin(a) * 8, 0, -math.cos(a) * 8]))
        ops.append(sph('Niveau 2', 'Pointe rouge', [x + math.cos(a) * 0.06, floor + 1.84, z + math.sin(a) * 0.06], [0.1, 0.2, 0.1], role='rouge'))
    # Niveau 3, la nuée : une voile de filet de plus jusqu'à 13 m, une hune dorée.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    ops.append(cyl('Niveau 3', 'Mât de hune', [0, floor + 9.5 + 1.7 - 0.012, CQ_NET_Z - 0.2], [0.26, 3.6, 0.26], role='bois'))
    ops.append(box('Niveau 3', 'Vergue', [0, 12.6, CQ_NET_Z - 0.06], [3.6, 0.18, 0.18], role='bois', shade=0.85))
    ops += cq_net('Niveau 3', CQ_HIGH, 12.6, 1.6, CQ_NET_Z)
    ops.append(cyl('Niveau 3', 'Hune', [0, floor + 9.5 + 0.2, CQ_NET_Z - 0.2], [1.3, 0.3, 1.3], role='accent'))
    ops.append(field('m_NetArrows', names))
    ops.append(field('m_Crossbow', [A + '/Bout']))
    ops.append(field('m_BoltPrefab', asset='Assets/_Project/Prefabs/Structures/Ballista Bolt.prefab'))
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_CatchClip', asset='Assets/kenney_impact-sounds/Audio/impactSoft_medium_002.ogg'))
    ops.append(field('m_VolleyClip', asset='Assets/_Project/Audio/Placeholder/bow_release.wav'))
    return ops

# ---------------------------------------------------------------- Chaîne de forçats : Tour-aimant + Lance-filet
# Une tour de fer de 8 m sur un socle de pierre, deux bobines de chaîne sur ses flancs, un gros aimant rouge en fer à cheval au
# sommet ; des chaînes qui pendent. Niveau 2 : des fers de forçat accrochés, une troisième bobine ; niveau 3 : une couronne
# dorée sur l'aimant, quatre grosses chaînes jusqu'au sol, un boulet de forçat.
CH_TOP = 7.4

def ch_links(parent, x, z, top, count, size=0.22):
    # Une chaîne qui pend : des maillons alternés (à plat, puis de champ).
    ops = []
    for k in range(count):
        y = top - size * 0.5 - k * size * 0.8
        flat = k % 2 == 0
        ops.append(box(parent, 'Maillon', [x, y, z], [size * 0.7 if flat else 0.06, size, 0.06 if flat else size * 0.7],
                       role='fer'))
    return ops

def chaine():
    ops = []
    ops.append(box('', 'Socle', [0, 0.194, 0], [4.0, 0.4, 4.0], role='pierre'))
    ops.append(box('', 'Plaque', [0, 0.418, 0], [4.06, 0.06, 4.06], role='accent'))
    floor = 0.442
    # La tour de fer : une colonne, quatre contreforts, des bandes rivetées.
    ops.append(box('', 'Colonne', [0, floor + CH_TOP / 2 - 0.006, 0], [1.4, CH_TOP, 1.4], role='metal'))
    for sx in (-1, 1):
        for sz in (-1, 1):
            ops.append(box('', 'Contrefort', [sx * 0.85, floor + 1.4 - 0.006, sz * 0.85], [0.5, 2.8, 0.5], role='metal', shade=0.8,
                           rot=[sz * -8, 0, sx * 8]))
    for y in (1.6, 3.4, 5.2, 7.0):
        ops.append(box('', 'Bande', [0, y, 0], [1.48, 0.2, 1.48], role='fer'))
    # Les deux bobines de chaîne sur les flancs (axe X), leurs tours de chaîne, leurs flasques dorés.
    for s in (-1, 1):
        x = s * (0.7 + 0.35 - 0.006)
        ops.append(cyl('', 'Bobine', [x, 3.0, 0], [1.3, 0.7, 1.3], role='bois', rot=[0, 0, 90]))
        for k in range(4):
            ops.append(cyl('', 'Tour de chaîne', [x + s * (k * 0.13 - 0.2), 3.0, 0], [1.42, 0.08, 1.42], role='fer', rot=[0, 0, 90]))
        ops.append(cyl('', 'Flasque', [x + s * 0.36, 3.0, 0], [1.6, 0.06, 1.6], role='accent', rot=[0, 0, 90]))
        ops += ch_links('', x + s * 0.2, 0.5, 2.4, 6)
    # Le gros aimant rouge en fer à cheval, les pôles d'acier en haut, sur un plateau.
    top = floor + CH_TOP - 0.006
    ops.append(box('', 'Plateau', [0, top + 0.1 - 0.006, 0], [2.4, 0.2, 2.0], role='fer'))
    A = 'Aimant'
    ops.append(empty('', A, [0, top + 0.2, 0]))
    ops.append(box(A, 'Fond', [0, 0.35 - 0.012, 0], [2.2, 0.7, 0.8], role='rouge'))
    for s in (-1, 1):
        ops.append(box(A, 'Branche', [s * 0.75, 0.7 + 0.8 - 0.012, 0], [0.7, 1.6, 0.8], role='rouge'))
        ops.append(box(A, 'Pôle', [s * 0.75, 2.3 + 0.15 - 0.018, 0], [0.74, 0.3, 0.84], role='blanc'))
    ops.append(box(A, 'Cercle', [0, 0.35 - 0.012, 0], [2.26, 0.14, 0.86], role='accent'))
    # Deux chaînes qui pendent du plateau, devant.
    for x in (-0.9, 0.9):
        ops += ch_links('', x, 1.0, top + 0.01, 9)
    # Niveau 2 : des fers de forçat accrochés à la colonne, une troisième bobine derrière.
    ops.append(empty('', 'Niveau 2', [0, 0, 0]))
    for x in (-0.4, 0.4):
        ops.append(cyl('Niveau 2', 'Fer', [x, 4.4, 0.7 + 0.05], [0.4, 0.12, 0.4], role='fer', rot=[90, 0, 0]))
    ops.append(cyl('Niveau 2', 'Bobine', [0, 5.6, -(0.7 + 0.35 - 0.006)], [1.1, 0.7, 1.1], role='bois', rot=[90, 0, 0]))
    ops.append(cyl('Niveau 2', 'Flasque', [0, 5.6, -(0.7 + 0.72)], [1.3, 0.06, 1.3], role='accent', rot=[90, 0, 0]))
    # Niveau 3, l'écrasement : une couronne dorée sur l'aimant, quatre grosses chaînes jusqu'au sol, un boulet de forçat.
    ops.append(empty('', 'Niveau 3', [0, 0, 0]))
    crown = top + 0.2 + 2.6
    for k in range(6):
        a = math.radians(k * 60)
        ops.append(box('Niveau 3', 'Fleuron', [math.cos(a) * 0.5, crown + 0.3, math.sin(a) * 0.32], [0.18, 0.6, 0.12], role='accent',
                       shade=1.2, rot=[0, -k * 60, 0]))
    ops.append(cyl('Niveau 3', 'Bandeau', [0, crown, 0], [1.2, 0.16, 0.76], role='accent'))
    for sx in (-1, 1):
        for sz in (-1, 1):
            a = (sx * 0.6, top - 0.5, sz * 0.6)
            b = (sx * 1.9, floor, sz * 1.9)
            d = (b[0] - a[0], b[1] - a[1], b[2] - a[2])
            length = math.sqrt(d[0] ** 2 + d[1] ** 2 + d[2] ** 2)
            yaw = math.degrees(math.atan2(d[0], d[2]))
            tilt = math.degrees(math.acos(-d[1] / length))
            ops.append(cyl('Niveau 3', 'Grosse chaîne', [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2], [0.16, length, 0.16],
                           role='fer', rot=[-tilt, yaw, 0]))
    ops.append(sph('Niveau 3', 'Boulet', [1.5, floor + 0.45 - 0.05, 1.5], [0.9, 0.9, 0.9], role='fer'))
    ops.append(field('m_Magnet', [A]))
    ops.append({'op': 'field', 'field': 'm_ChainMaterial', 'role': 'fer'})
    ops.append(field('m_LevelParts', ['Niveau 2', 'Niveau 3']))
    ops.append(field('m_BindClip', asset='Assets/kenney_impact-sounds/Audio/impactMetal_heavy_000.ogg'))
    ops.append(field('m_CrushClip', asset='Assets/kenney_impact-sounds/Audio/impactBell_heavy_002.ogg'))
    return ops

def duos():
    return [
        {'name': 'Boulet fou', 'prefab': 'Duos/Duo_Boulet_fou', 'definition': 'Duo_Boulet_fou', 'kinds': ['Barricade', 'Trampoline'],
         'component': 'DuoBouletFou', 'slot': 'Lane', 'footprint': [4, 2], 'palette': 'Duo_Boulet', 'color': GOLD_LIGHT,
         # Deuxième passe (après le premier essai) : le modèle refait en plus grand, les textes des niveaux revus.
         'mark': 'Duo 2 du 31 octobre', 'rebuild': True,
         'plans': [0, 4, 4],
         'description': 'Le boulet fait des tours complets et envoie les ennemis qu\'il frappe sur la voie voisine.',
         'levels': ['Le boulet hérissé fait des tours complets en travers de la voie : chaque ennemi frappé part loin sur le côté, '
                    'jusque sur la voie voisine, et loin en arrière sur elle.',
                    'Plus vite, plus loin : l\'ennemi lancé renverse ceux sur qui il retombe.',
                    'Le boulet de canon : deux boulets ; l\'ennemi frappé part à toute vitesse jusqu\'au mur d\'en face, s\'y '
                    'écrase et blesse ceux qui sont autour.'],
         'levelsBefore': ['Le boulet hérissé fait des tours complets en travers de la voie : chaque ennemi frappé part loin sur le '
                          'côté, jusque sur la voie voisine.',
                          'Plus vite, plus fort : l\'ennemi lancé renverse ceux sur qui il retombe.',
                          'Les météores : deux boulets ; les ennemis lancés retombent en météores, qui blessent et renversent tout '
                          'autour.'],
         'ops': boulet_fou()},
        {'name': 'Marteau-pilon', 'prefab': 'Duos/Duo_Marteau_pilon', 'definition': 'Duo_Marteau_pilon', 'kinds': ['Barricade', 'Spikes'],
         'component': 'DuoMarteauPilon', 'slot': 'Lane', 'footprint': [4, 3], 'palette': 'Duo_Marteau', 'color': GOLD_LIGHT,
         'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Un marteau géant enfonce l\'ennemi dans le sol jusqu\'au cou : seule sa tête dépasse.',
         'levels': ['Le marteau de forge glisse au-dessus de l\'ennemi et l\'enfonce dans le sol jusqu\'au cou : planté, seule sa '
                    'tête dépasse, un tir à la tête facile.',
                    'L\'onde du coup renverse et blesse tout autour.',
                    'La forge : trois marteaux en cadence sur toute la largeur de la voie, puis le grand coup, les trois ensemble, '
                    'qui envoie en l\'air tout ce qui est à 11 m.'],
         'ops': marteau_pilon()},
        {'name': 'Toboggan', 'prefab': 'Duos/Duo_Toboggan', 'definition': 'Duo_Toboggan', 'kinds': ['Trampoline', 'FrostTower'],
         'component': 'DuoToboggan', 'slot': 'Lane', 'footprint': [4, 7], 'palette': 'Duo_Toboggan', 'color': GOLD_LIGHT,
         'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Une piste de glace : les ennemis glissent vers la forêt et décollent en vrille, sous les notes du jury.',
         'levels': ['Une piste de glace en pente vers la forêt : l\'ennemi qui y monte glisse sur le ventre en tournoyant, renverse '
                    'ceux qu\'il croise et décolle du tremplin en vrille, au ralenti, sous les notes du jury.',
                    'Ceux qu\'il percute glissent aussi ; le saut est plus haut et plus long.',
                    'Le saut à ski : un grand tremplin les envoie à plus de 20 m de haut, loin vers la forêt, sous un « 10 ! 10 ! '
                    '10 ! » ; l\'atterrissage les tue.'],
         'ops': toboggan()},
        {'name': 'Yo-yo', 'prefab': 'Duos/Duo_Yoyo', 'definition': 'Duo_Yoyo', 'kinds': ['Trampoline', 'Magnet'],
         'component': 'DuoYoyo', 'slot': 'Lane', 'footprint': [4, 3], 'palette': 'Duo_Yoyo', 'color': GOLD_LIGHT,
         'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Une grue au yo-yo aimanté : elle hisse un ennemi et le fait claquer au sol trois fois.',
         'levels': ['Une grue au-dessus de la voie : son yo-yo aimanté attrape un ennemi, le hisse à 6 m et le fait claquer au sol '
                    'trois fois, puis le lâche de tout en haut.',
                    'Après les claques, il le lance sur le groupe le plus serré, qu\'il renverse.',
                    'Le lustre : six fils de plus sur un anneau qui tourne ; sept ennemis montent et descendent en cadence, puis le '
                    'lustre s\'emballe et les jette tous au loin, où ils s\'écrasent.'],
         'ops': yoyo()},
        {'name': 'Avalanche', 'prefab': 'Duos/Duo_Avalanche', 'definition': 'Duo_Avalanche', 'kinds': ['Mortar', 'FrostTower'],
         'component': 'DuoAvalanche', 'slot': 'Roadside', 'footprint': [3, 3], 'palette': 'Duo_Avalanche', 'color': GOLD_LIGHT,
         'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Une boule de neige roule vers la forêt, grossit et avale les ennemis ; elle éclate au bout, ou sous ta flèche.',
         'levels': ['Le mortier de glace tire une boule de neige devant le premier ennemi : elle roule vers la forêt, grossit et '
                    'avale ceux qu\'elle touche ; au bout, ou sous ta flèche, elle éclate et ils volent en l\'air.',
                    'Elle roule plus loin, plus grosse, et avale aussi les gros.',
                    'L\'avalanche : une boule géante dévale toute la voie jusqu\'à la forêt, avale tout ce qu\'elle croise et éclate '
                    'en une explosion de neige qui coûte la moitié de leurs PV à tous.'],
         'ops': avalanche()},
        {'name': 'Feu d\'artifice', 'prefab': 'Duos/Duo_Feu_d_artifice', 'definition': 'Duo_Feu_d_artifice', 'kinds': ['Mortar', 'Lighthouse'],
         'component': 'DuoFeuArtifice', 'slot': 'Roadside', 'footprint': [3, 3], 'palette': 'Duo_Feu', 'color': GOLD_LIGHT,
         'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Des fusées éclatent en bouquets : les ennemis dessous sont éblouis et brûlés ; ta flèche s\'y enflamme.',
         'levels': ['Une fusée éclate en bouquet au-dessus du groupe le plus serré : éblouis, les ennemis s\'arrêtent sous une pluie '
                    'd\'étincelles qui brûle ; ta flèche qui traverse le bouquet s\'enflamme.',
                    'Des fusées à deux étages : chaque bouquet en fait éclater deux autres, à côté.',
                    'Le bouquet final : vingt fusées d\'un coup sur tous les ennemis à portée ; le ciel s\'allume, tous sont '
                    'éblouis et brûlés.'],
         'ops': feu_artifice()},
        {'name': 'Chant des sirènes', 'prefab': 'Duos/Duo_Chant_des_sirenes', 'definition': 'Duo_Chant_des_sirenes',
         'kinds': ['Lighthouse', 'Bubble'],
         'component': 'DuoSirenes', 'slot': 'Roadside', 'footprint': [3, 3], 'palette': 'Duo_Sirenes', 'color': GOLD_LIGHT,
         'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Le faisceau charme un ennemi : dans sa bulle rose, il change de camp et se jette sur les autres.',
         'levels': ['Le faisceau du phare charme un ennemi : enfermé dans une bulle rose, il change de camp 6 s, se jette sur les '
                    'autres ennemis, les renverse et les blesse.',
                    'Deux à la fois ; à la fin, la bulle éclate en deux bulles qui charment ses voisins.',
                    'La mêlée : tout ce qui est à 20 m change de camp ; chacun se jette sur son voisin.'],
         'ops': sirenes()},
        {'name': 'Loupe', 'prefab': 'Duos/Duo_Loupe', 'definition': 'Duo_Loupe', 'kinds': ['Lighthouse', 'Ballista'],
         'component': 'DuoLoupe', 'slot': 'Roadside', 'footprint': [3, 3], 'palette': 'Duo_Loupe', 'color': GOLD_LIGHT,
         'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Dans le champ de la loupe, les ennemis ont la tête trois fois plus grosse.',
         'levels': ['Une loupe grande comme une porte se tourne vers les ennemis : dans son champ, ils ont la tête trois fois plus '
                    'grosse ; les tirs à la tête deviennent faciles.',
                    'Une baliste plante ses carreaux dans les têtes géantes.',
                    'La fourmi : la loupe concentre le soleil en un rayon sur l\'ennemi le plus fort ; il fume, puis prend feu '
                    '(un tiers de ses PV ; un boss, 15 %).'],
         'ops': loupe()},
        {'name': 'Carquois géant', 'prefab': 'Duos/Duo_Carquois_geant', 'definition': 'Duo_Carquois_geant',
         'kinds': ['Ballista', 'NetLauncher'],
         'component': 'DuoCarquoisGeant', 'slot': 'Roadside', 'footprint': [3, 3], 'palette': 'Duo_Carquois', 'color': GOLD_LIGHT,
         'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Un grand filet rattrape tes flèches ratées ; à dix, l\'arbalète te les renvoie en une volée.',
         'levels': ['Un grand filet rattrape tes flèches ratées ; à dix, l\'arbalète à six arcs les renvoie en une volée de carreaux '
                    'qui pleut sur le groupe le plus serré.',
                    'Chaque carreau renvoyé explose à l\'impact, et brûle si ta flèche était enflammée.',
                    'La nuée : toutes tes flèches ratées, partout sur la carte, sont gardées ; à trente, elles repartent d\'un coup, '
                    'trois carreaux chacune, et pleuvent sur tous les ennemis à portée.'],
         'ops': carquois()},
        {'name': 'Chaîne de forçats', 'prefab': 'Duos/Duo_Chaine_de_forcats', 'definition': 'Duo_Chaine_de_forcats',
         'kinds': ['Magnet', 'NetLauncher'],
         'component': 'DuoChaineForcats', 'slot': 'Roadside', 'footprint': [3, 3], 'palette': 'Duo_Chaine', 'color': GOLD_LIGHT,
         'mark': MARK, 'plans': [0, 4, 4],
         'description': 'Des chaînes aimantées lient un groupe : chaque coup sur l\'un blesse tous les autres.',
         'levels': ['Des chaînes aimantées lient jusqu\'à 5 ennemis : ils avancent ensemble, plus lentement, et chaque coup sur l\'un '
                    'blesse tous les autres (ta flèche à la tête d\'un seul : toute la chaîne).',
                    'Jusqu\'à 8 ; un lié qui meurt fait tomber les autres.',
                    'L\'écrasement : jusqu\'à 15 ; la chaîne se resserre d\'un coup, tous volent les uns contre les autres et '
                    's\'écrasent au milieu.'],
         'ops': chaine()},
    ]
