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
    ]
