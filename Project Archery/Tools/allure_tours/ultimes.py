# Les ultimes du tower defense (2 novembre ; Docs/Refonte_TowerDefense.md, 12.44 et 12.46) : chacune est le niveau 4 d'un trio
# de sa famille (une TowerUltimate). conception.py écrit ici leur partie du fichier d'allure (« ultimes ») ; le baker
# (Editor/TowerDefenseLookBaker) en fait le prefab (Prefabs/Structures/Duos), la définition (Data/Duos : un seul niveau, ses 10
# plans) et l'entrée de la liste des ultimes du catalogue (Resources/TowerDuos). Deux fois un trio, des accents dorés (la règle
# du joueur pour les fusions : « il doit avoir de la gueule »). Règle des faces : une pièce posée sur une autre y est enfoncée de
# 6 mm.
import math
from duos import box, cyl, sph, empty, field, GOLD_LIGHT
from trios import tie, en_anvil

PALETTES = {
    'Ultime_Colosse': {'fer': '#3B3E46', 'fer_clair': '#646873', 'bronze': '#A0703A', 'cuir': '#5A3A24', 'bois': '#5E3F28',
                       'metal': '#3A3C42', 'pierre': '#8A857E', 'accent': GOLD_LIGHT},
    # Le Grand Manège (2 novembre, tard) : le bois peint d'un carrousel (Editor/ManegeFinitionBaker y pose le bois peint de
    # Tools/texture_bois_peint.py), rouge et crème, des chevaux de toutes les robes, l'or partout, des miroirs.
    'Ultime_Manege': {'rouge': '#C42F3A', 'creme': '#F2E4C4', 'bleu': '#2E6CC4', 'vert': '#3A9A5A', 'violet': '#8A4FC0',
                      'rose': '#E87AAE', 'blanc': '#F4F0E6', 'noir': '#2A2628', 'gris': '#B4B0AE', 'bai': '#8A5230',
                      'alezan': '#B8683A', 'isabelle': '#D9B877', 'plancher': '#9A6B42', 'bois': '#6B4426', 'miroir': '#C9D6E0',
                      'pierre': '#8E8A84', 'accent': GOLD_LIGHT},
    # La Grande Horloge (2 novembre, tard) : la glace claire et sombre (Editor/HorlogeFinitionBaker y pose la glace de
    # Tools/texture_glace.py), la nuit du cadran céleste, l'argent, l'or.
    'Ultime_Horloge': {'glace': '#BFE6F5', 'glace_sombre': '#4F86B0', 'nuit': '#1C2A5A', 'argent': '#C8D0DA', 'pierre': '#8E949C',
                       'accent': GOLD_LIGHT},
}

MARK = 'Ultime du 2 novembre'

# La finition (2 novembre au soir, après l'essai du joueur : « plus texturé, plus lumineux ») : une nouvelle passe qui refait tout
# le modèle (rebuild) ; les veines de fonte luisent (le matériau animé Fonte_en_fusion, shader Archery/MoltenIron), le fer est
# martelé (Editor/ColosseFinitionBaker : textures de Tools/texture_fer_martele.py).
FINISH = 'Finition du 2 novembre'
MOLTEN = 'Fonte_en_fusion'

# Les deux mains (2 novembre, tard ; 12.48, après le deuxième essai du joueur : « d'une main il frappe sa forge, de l'autre il
# envoie des marteaux à chaque flèche ») : le bras gauche devient un bras articulé qui bat le lingot sur l'enclume, posée sur un
# billot devant lui. Une nouvelle marque : le modèle est refait.
FORGE = 'Les deux mains du 2 novembre'

def vein(parent, name, a, b, radius=0.06):
    # Une veine de fonte de a à b (un cylindre fin au matériau qui luit), comme tie.
    dx, dy, dz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    length = math.sqrt(dx * dx + dy * dy + dz * dz)
    pitch = math.degrees(math.atan2(math.hypot(dx, dz), dy))
    yaw = math.degrees(math.atan2(dx, dz))
    mid = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2]
    return cyl(parent, name, mid, [radius * 2, length, radius * 2], mat=MOLTEN, rot=[pitch, yaw, 0])

# ---------------------------------------------------------------- Colosse de forge : l'Enclume ou la Grande bombarde, niveau 4
# Un géant de fer à genoux (x en travers, z vers l'avant : la forêt sur la voie, la voie au bord), les genoux écartés : sur la
# voie, les ennemis passent entre eux, sous son bassin (un couloir de près de 4 m, à 5 m de haut). Le buste (m_Torso, repère sur
# la ceinture) se tourne vers sa cible : la fournaise dorée de la poitrine (m_Furnace, d'où coule la fonte), deux cheminées dans
# le dos (m_Chimneys, qui fument), la tête aux yeux de braise et sa crête dorée, les épaulières de bronze. Le bras droit (m_Arm,
# repère à l'épaule, penché de 15° vers l'avant au repos) tient le marteau (m_Hammer, repère au poing), sa tête posée au sol devant
# le genou droit. Le bras gauche (m_LeftArm, repère à l'épaule) tient la masse de forge, posée sur le lingot de fonte (m_Ingot) de
# la grande enclume cerclée d'or, sur son billot devant le genou gauche : il tourne autour de son axe X pour frapper. Le buste ne
# se tourne plus (12.48 : la masse doit retomber sur l'enclume).
CO_KNEE_X = 3.0      # les genoux, de part et d'autre
CO_HIP_X = 1.75      # les hanches
CO_HIP_Y = 6.2
CO_BELT = 7.844      # le haut de la ceinture : le repère du buste
CO_SHOULDER = (3.9, 5.4, 0.1)   # l'épaule droite, dans le buste (la gauche : son reflet)
CO_ARM_TILT = -15    # le bras du marteau, penché vers l'avant au repos
CO_ARM_LENGTH = 7.9  # de l'épaule au poing

# La forge : l'enclume (2,4 fois celle de l'Enclume) sur un billot de 0,5 m, sous l'épaule gauche ; le lingot sur sa table. Le bras
# gauche est penché vers l'avant de ce qu'il faut pour que le bas de la masse (3,9 m sous le poing, son manche droit au repos) y
# soit posé.
AN_SCALE = 2.4
AN_BILLOT = 0.5
AN_TABLE = 1.088             # le haut de la table de l'enclume (son repère)
AN_INGOT = (0.62, 0.14, 0.26)
FORGE_DROP = 3.9             # du poing au bas de la tête de la masse

def co_forge():
    # Où est la forge : la hauteur du lingot, l'angle du bras gauche au repos, et l'avancée du poing.
    base = AN_BILLOT - 0.006
    ingot_top = base + (AN_TABLE + AN_INGOT[1] - 0.006) * AN_SCALE
    fist_y = ingot_top - 0.006 + FORGE_DROP
    shoulder_y = CO_BELT + CO_SHOULDER[1]
    tilt = math.degrees(math.acos((shoulder_y - fist_y) / CO_ARM_LENGTH))
    fist_z = CO_SHOULDER[2] + 0.1 + CO_ARM_LENGTH * math.sin(math.radians(tilt))
    return base, tilt, fist_z

def co_leg(s):
    ops = []
    x = s * CO_KNEE_X
    knee = [x, 1.4, 0.4]
    hip = [s * CO_HIP_X, CO_HIP_Y, 0.1]
    # Le tibia à plat sur le sol, le pied derrière, orteils au sol.
    ops.append(box('', 'Tibia', [x, 0.75, -1.5], [1.6, 1.5, 4.6], role='fer', shade=0.85))
    ops.append(box('', 'Pied', [x, 0.9, -3.8 - 0.6 + 0.006], [1.5, 1.4, 1.2], role='fer', shade=0.75))
    ops.append(box('', 'Orteils', [x, 0.3, -5.0 + 0.012], [1.46, 0.6, 0.6], role='fer', shade=0.65))
    # Le genou, sa genouillère de bronze et son rivet doré.
    ops.append(sph('', 'Genou', knee, [2.1, 1.9, 2.1], role='fer_clair'))
    ops.append(box('', 'Genouillère', [x, 1.5, 1.35], [1.5, 1.6, 0.25], role='bronze'))
    ops.append(sph('', 'Rivet', [x, 1.5, 1.475 + 0.15 - 0.012], [0.32, 0.32, 0.32], role='accent'))
    # La cuisse, du genou à la hanche ; la hanche, une rotule de bronze.
    ops.append(tie('', 'Cuisse', knee, hip, role='fer', radius=0.95))
    ops.append(cyl('', 'Cuissard', [s * (CO_KNEE_X + CO_HIP_X) / 2 * 1.0, 3.9, 0.25], [2.06, 0.3, 2.06], role='accent',
                   rot=[0, 0, s * 14.6]))
    ops.append(sph('', 'Hanche', hip, [1.9, 1.9, 1.9], role='bronze'))
    # La finition : la plaque de bronze du tibia et ses rivets, une veine de fonte le long du tibia et de la cuisse, deux fentes
    # qui luisent dans la genouillère.
    ops.append(box('', 'Plaque de tibia', [x, 1.5 + 0.06 - 0.006, -1.2], [1.3, 0.12, 2.6], role='bronze'))
    for z in (-2.2, -1.2, -0.2):
        ops.append(sph('', 'Rivet', [x, 1.614 + 0.1 - 0.012, z], [0.2, 0.2, 0.2], role='accent'))
    ops.append(box('', 'Veine', [s * (3.8 + 0.03 - 0.006), 0.8, -1.5], [0.06, 0.12, 3.6], mat=MOLTEN))
    ops.append(vein('', 'Veine', [x, 1.4, 0.4 + 0.93], [s * CO_HIP_X, CO_HIP_Y, 0.1 + 0.93]))
    for y in (1.15, 1.85):
        ops.append(box('', 'Fente', [x, y, 1.475 + 0.015 - 0.006], [0.9, 0.1, 0.03], mat=MOLTEN))
    return ops

def co_hammer(parent):
    # Le marteau, repère au poing (le manche le long de -y, la tête en bas, son long côté vers l'avant).
    h = parent + '/Marteau'
    ops = [empty(parent, 'Marteau', [0, -7.9, 0])]
    ops.append(cyl(h, 'Manche', [0, -1.5, 0], [0.42, 5.2, 0.42], role='bois'))
    ops.append(sph(h, 'Pommeau', [0, 1.1 + 0.25 - 0.012, 0], [0.5, 0.5, 0.5], role='accent'))
    ops.append(box(h, 'Tête', [0, -4.6, 0], [1.6, 1.6, 3.0], role='fer_clair'))
    for sz in (-1, 1):
        ops.append(box(h, 'Cercle', [0, -4.6, sz * 1.0], [1.66, 1.66, 0.3], role='accent'))
        ops.append(box(h, 'Face', [0, -4.6, sz * (1.5 + 0.05 - 0.006)], [1.4, 1.4, 0.1], role='fer', shade=0.7))
        # La finition : une croix de fonte qui luit sur chaque face.
        ops.append(box(h, 'Rune', [0, -4.6, sz * (1.594 + 0.015 - 0.006)], [0.9, 0.12, 0.03], mat=MOLTEN))
        ops.append(box(h, 'Rune', [0, -4.6, sz * (1.594 + 0.015 - 0.006)], [0.12, 0.9, 0.03], mat=MOLTEN))
    # Une veine de fonte autour de la tête, sur ses côtés.
    ops.append(box(h, 'Veine', [0, -4.6, 0], [1.62, 0.1, 1.64], mat=MOLTEN))
    ops.append(empty(h, 'Tête du marteau', [0, -4.6, 0]))
    return ops

def co_arm_right():
    # Le bras du marteau, repère à l'épaule : il pend (le long de -y), penché vers l'avant.
    a = 'Buste/Bras'
    ops = [empty('Buste', 'Bras', CO_SHOULDER, [CO_ARM_TILT, 0, 0])]
    ops.append(cyl(a, 'Bras', [0, -1.9, 0], [1.3, 3.8, 1.3], role='fer'))
    ops.append(sph(a, 'Coude', [0, -3.9, 0], [1.45, 1.45, 1.45], role='bronze'))
    ops.append(cyl(a, 'Avant-bras', [0, -5.7, 0], [1.2, 3.4, 1.2], role='fer'))
    ops.append(cyl(a, 'Brassard', [0, -6.3, 0], [1.32, 0.35, 1.32], role='accent'))
    ops.append(box(a, 'Poing', [0, -7.9, 0], [1.4, 1.4, 1.4], role='fer', shade=0.9))
    # La finition : des veines de fonte sur le dehors et le devant du bras et de l'avant-bras.
    ops.append(box(a, 'Veine', [0.65 + 0.03 - 0.006, -1.9, 0], [0.06, 3.0, 0.14], mat=MOLTEN))
    ops.append(box(a, 'Veine', [0, -1.9, 0.65 + 0.03 - 0.006], [0.14, 2.6, 0.06], mat=MOLTEN))
    ops.append(box(a, 'Veine', [0.6 + 0.03 - 0.006, -5.5, 0], [0.06, 2.4, 0.14], mat=MOLTEN))
    ops += co_hammer(a)
    return ops

def co_sledge(parent, tilt):
    # La masse de forge, repère au poing : redressée de l'angle du bras, son manche est droit au repos, la tête à plat sur le
    # lingot (son long côté le long de l'enclume).
    m = parent + '/Masse'
    ops = [empty(parent, 'Masse', [0, -CO_ARM_LENGTH, 0], [tilt, 0, 0])]
    ops.append(cyl(m, 'Manche', [0, -1.6, 0], [0.36, 3.2, 0.36], role='bois'))
    ops.append(sph(m, 'Pommeau', [0, 1.05, 0], [0.44, 0.44, 0.44], role='accent'))
    ops.append(box(m, 'Tête', [0, -FORGE_DROP + 0.5, 0], [1.0, 1.0, 1.8], role='fer_clair'))
    for sz in (-1, 1):
        ops.append(box(m, 'Cercle', [0, -FORGE_DROP + 0.5, sz * 0.6], [1.06, 1.06, 0.2], role='accent'))
        ops.append(box(m, 'Face', [0, -FORGE_DROP + 0.5, sz * (0.9 + 0.05 - 0.006)], [0.86, 0.86, 0.1], role='fer', shade=0.7))
    # Une veine de fonte qui luit autour de la tête.
    ops.append(box(m, 'Veine', [0, -FORGE_DROP + 0.5, 0], [1.02, 0.08, 1.82], mat=MOLTEN))
    ops.append(empty(m, 'Tête de la masse', [0, -FORGE_DROP + 0.5, 0]))
    return ops

def co_arm_left(tilt):
    # Le bras de la forge, repère à l'épaule : le reflet du bras du marteau, penché vers l'avant au repos jusqu'au lingot.
    a = 'Buste/Bras gauche'
    ops = [empty('Buste', 'Bras gauche', [-CO_SHOULDER[0], CO_SHOULDER[1], CO_SHOULDER[2]], [-tilt, 0, 0])]
    ops.append(cyl(a, 'Bras', [0, -1.9, 0], [1.3, 3.8, 1.3], role='fer'))
    ops.append(sph(a, 'Coude', [0, -3.9, 0], [1.45, 1.45, 1.45], role='bronze'))
    ops.append(cyl(a, 'Avant-bras', [0, -5.7, 0], [1.2, 3.4, 1.2], role='fer'))
    ops.append(cyl(a, 'Brassard', [0, -6.3, 0], [1.32, 0.35, 1.32], role='accent'))
    ops.append(box(a, 'Poing', [0, -CO_ARM_LENGTH, 0], [1.4, 1.4, 1.4], role='fer', shade=0.9))
    # Des veines de fonte sur le dehors et le devant du bras et de l'avant-bras.
    ops.append(box(a, 'Veine', [-(0.65 + 0.03 - 0.006), -1.9, 0], [0.06, 3.0, 0.14], mat=MOLTEN))
    ops.append(box(a, 'Veine', [0, -1.9, 0.65 + 0.03 - 0.006], [0.14, 2.6, 0.06], mat=MOLTEN))
    ops.append(box(a, 'Veine', [-(0.6 + 0.03 - 0.006), -5.5, 0], [0.06, 2.4, 0.14], mat=MOLTEN))
    ops += co_sledge(a, tilt)
    return ops

def colosse():
    ops = []
    # La finition : un socle de basalte sous lui, fendu de fonte qui luit.
    ops.append(cyl('', 'Socle', [0, 0.04, -0.6], [9.0, 0.08, 9.0], role='pierre', shade=0.45))
    for k in range(7):
        a = math.radians(k * 360 / 7 + 12)
        length = 2.6 + (k % 3) * 0.5
        r = 0.9 + length / 2
        ops.append(box('', 'Fente', [math.sin(a) * r, 0.08 + 0.015 - 0.006, -0.6 + math.cos(a) * r], [0.16, 0.03, length], mat=MOLTEN,
                       rot=[0, math.degrees(a), 0]))
    for s in (-1, 1):
        ops += co_leg(s)
    # Le bassin, la ceinture de cuir et sa boucle dorée, la tassette de bronze bordée d'or qui pend devant.
    ops.append(box('', 'Bassin', [0, 6.5, 0.1], [4.6, 1.7, 2.8], role='fer'))
    ops.append(box('', 'Ceinture', [0, CO_BELT - 0.25, 0.1], [4.9, 0.5, 3.1], role='cuir'))
    ops.append(box('', 'Boucle', [0, CO_BELT - 0.25, 1.65 + 0.08 - 0.006], [1.0, 0.7, 0.16], role='accent'))
    for x in (-2.2, -1.6, -1.0, 1.0, 1.6, 2.2):
        ops.append(sph('', 'Rivet', [x, CO_BELT - 0.25, 1.65 + 0.1 - 0.012], [0.2, 0.2, 0.2], role='accent'))
    ops.append(box('', 'Tassette', [0, 6.0, 1.5 + 0.09 - 0.006], [2.2, 2.6, 0.18], role='bronze'))
    ops.append(box('', 'Bordure', [0, 4.7 + 0.08 - 0.006, 1.6], [2.24, 0.16, 0.24], role='accent'))
    # Le buste, qui tourne : ventre, poitrine, plastrons de bronze et leurs rivets dorés.
    ops.append(empty('', 'Buste', [0, CO_BELT, 0.1]))
    b = 'Buste'
    ops.append(box(b, 'Ventre', [0, 1.2 - 0.006, 0], [4.0, 2.412, 2.6], role='fer', shade=0.9))
    ops.append(box(b, 'Poitrine', [0, 4.4 - 0.006, 0.1], [6.6, 4.0, 3.8], role='fer'))
    front = 0.1 + 1.9   # la face avant de la poitrine
    for sx in (-1, 1):
        ops.append(box(b, 'Plastron', [sx * 1.85, 5.45, front + 0.1 - 0.006], [2.4, 1.3, 0.2], role='bronze'))
        for k in (-1, 1):
            ops.append(sph(b, 'Rivet', [sx * 1.85 + k * 0.85, 5.45, front + 0.2 - 0.012 + 0.1], [0.26, 0.26, 0.26], role='accent'))
    # La finition : des fissures de fonte sur la poitrine, de part et d'autre de la fournaise, et sur le ventre.
    for sx in (-1, 1):
        for x, y, length, angle in ((1.85, 4.45, 0.9, -30), (2.3, 3.7, 1.0, 20), (2.75, 2.85, 0.9, -35)):
            ops.append(box(b, 'Fissure', [sx * x, y, front + 0.03 - 0.006], [0.12, length, 0.06], mat=MOLTEN, rot=[0, 0, sx * angle]))
    for x, y, length, angle in ((0.4, 1.4, 1.2, 15), (-0.5, 0.8, 0.9, -25)):
        ops.append(box(b, 'Fissure', [x, y, 1.3 + 0.03 - 0.006], [0.12, length, 0.06], mat=MOLTEN, rot=[0, 0, angle]))
    # L'évent du dos : un foyer qui luit derrière sa grille, cadré d'or ; deux tuyaux de cuivre jusqu'aux cheminées.
    back = 0.1 - 1.9
    ops.append(box(b, 'Évent', [0, 4.0, back - 0.05 + 0.006], [2.2, 1.0, 0.1], mat=MOLTEN))
    for k in range(4):
        ops.append(box(b, 'Grille', [-0.75 + 0.5 * k, 4.0, back - 0.1 - 0.06 + 0.012], [0.12, 1.02, 0.12], role='fer', shade=0.6))
    for sy in (-1, 1):
        ops.append(box(b, 'Cadre', [0, 4.0 + sy * (0.5 + 0.12 - 0.006), back - 0.12 + 0.006], [2.44, 0.24, 0.24], role='accent'))
    for sx in (-1, 1):
        start = [sx * 1.3, 4.5, back - 0.16]
        ops.append(sph(b, 'Coude', start, [0.4, 0.4, 0.4], role='cuivre'))
        ops.append(tie(b, 'Tuyau', start, [sx * 1.6, 6.6, -1.75], role='cuivre', radius=0.16))
    # La fournaise : le foyer qui luit (la fonte animée), sa grille de fer, son cadre doré.
    fy = 3.4
    ops.append(box(b, 'Braise', [0, fy, front + 0.05 - 0.006], [2.4, 1.6, 0.1], mat=MOLTEN))
    for k in range(5):
        ops.append(box(b, 'Grille', [-0.8 + 0.4 * k, fy, front + 0.1 + 0.06 - 0.012], [0.12, 1.62, 0.12], role='fer', shade=0.6))
    for sy in (-1, 1):
        ops.append(box(b, 'Cadre', [0, fy + sy * (0.8 + 0.15 - 0.006), front + 0.15 - 0.006], [3.04, 0.3, 0.3], role='accent'))
    for sx in (-1, 1):
        ops.append(box(b, 'Cadre', [sx * (1.2 + 0.15 - 0.006), fy, front + 0.15 - 0.006], [0.3, 1.612, 0.3], role='accent'))
    ops.append(empty(b, 'Fournaise', [0, fy, front + 0.4]))
    # Les cheminées du dos, cerclées d'or ; leur fumée sort en haut.
    for sx, side in ((-1, 'gauche'), (1, 'droite')):
        ops.append(cyl(b, 'Cheminée', [sx * 1.6, 6.394 + 1.3 - 0.3, -1.3], [0.9, 2.6, 0.9], role='fer', shade=0.8))
        ops.append(cyl(b, 'Cercle', [sx * 1.6, 6.394 + 2.3 - 0.3, -1.3], [0.98, 0.18, 0.98], role='accent'))
        ops.append(cyl(b, 'Braise', [sx * 1.6, 8.694 + 0.02 - 0.006, -1.3], [0.72, 0.04, 0.72], mat=MOLTEN))
        ops.append(empty(b, 'Fumée ' + side, [sx * 1.6, 6.394 + 2.6 - 0.3 + 0.2, -1.3]))
    # Le cou, la tête : visière de bronze, yeux de braise, mentonnière, crête dorée.
    ops.append(cyl(b, 'Cou', [0, 6.394 + 0.4 - 0.006, 0.2], [1.5, 0.8, 1.5], role='fer', shade=0.8))
    head = 7.182 - 0.006 + 1.1   # le centre de la tête
    face = 0.3 + 1.1             # sa face avant
    ops.append(box(b, 'Tête', [0, head, 0.3], [2.2, 2.2, 2.2], role='fer'))
    ops.append(box(b, 'Visière', [0, head + 0.35, face + 0.1 - 0.006], [2.3, 0.4, 0.2], role='bronze'))
    for sx in (-1, 1):
        ops.append(box(b, 'Œil', [sx * 0.5, head - 0.05, face + 0.03 - 0.006], [0.55, 0.22, 0.06], mat=MOLTEN))
        ops.append(sph(b, 'Boulon', [sx * (1.1 + 0.15 - 0.012), head + 0.1, 0.3], [0.32, 0.32, 0.32], role='accent'))
        # La finition : deux cornes dorées, penchées vers l'extérieur.
        ops.append(sph(b, 'Corne', [sx * 1.35, head + 0.75, 0.3], [0.32, 1.4, 0.32], role='accent', rot=[0, 0, -sx * 35]))
    ops.append(box(b, 'Mentonnière', [0, head - 0.75, face + 0.1 - 0.006], [1.6, 0.6, 0.2], role='fer_clair'))
    ops.append(box(b, 'Crête', [0, head + 1.1 + 0.3 - 0.006, 0.3], [0.3, 0.6, 2.3], role='accent'))
    # Les épaulières de bronze, cerclées d'or, hérissées de deux pointes dorées.
    for sx in (-1, 1):
        ops.append(sph(b, 'Épaulière', [sx * 3.7, 5.9, 0.1], [2.8, 2.2, 2.8], role='bronze'))
        ops.append(cyl(b, 'Cercle', [sx * 3.7, 5.5, 0.1], [2.9, 0.2, 2.9], role='accent'))
        ops.append(sph(b, 'Pointe', [sx * 3.9, 6.9, 0.1], [0.28, 1.1, 0.28], role='accent', rot=[0, 0, -sx * 25]))
        ops.append(sph(b, 'Pointe', [sx * 4.4, 6.5, 0.1], [0.26, 0.9, 0.26], role='accent', rot=[0, 0, -sx * 55]))
    base, tilt, forge_z = co_forge()
    ops += co_arm_left(tilt)
    ops += co_arm_right()
    # La forge (12.48), devant le genou gauche, sous l'épaule : le billot de basalte, la grande enclume (sa bigorne vers le géant),
    # le lingot de fonte sur sa table, où retombe la masse.
    ops.append(box('', 'Billot', [-CO_SHOULDER[0], AN_BILLOT / 2, forge_z], [2.0, AN_BILLOT, 3.6], role='pierre', shade=0.55))
    ops += en_anvil('', 'Enclume', [-CO_SHOULDER[0], base, forge_z], [0, 90, 0])
    ops.append({'op': 'set', 'path': 'Enclume', 'scale': [AN_SCALE, AN_SCALE, AN_SCALE]})
    ops.append(box('Enclume', 'Lingot', [0, AN_TABLE + AN_INGOT[1] / 2 - 0.006, 0], list(AN_INGOT), mat=MOLTEN))
    ops.append(field('m_Torso', ['Buste']))
    ops.append(field('m_Arm', ['Buste/Bras']))
    ops.append(field('m_Hammer', ['Buste/Bras/Marteau']))
    ops.append(field('m_HammerHead', ['Buste/Bras/Marteau/Tête du marteau']))
    ops.append(field('m_LeftArm', ['Buste/Bras gauche']))
    ops.append(field('m_ForgeHead', ['Buste/Bras gauche/Masse/Tête de la masse']))
    ops.append(field('m_Ingot', ['Enclume/Lingot']))
    ops.append(field('m_Furnace', ['Buste/Fournaise']))
    ops.append(field('m_Chimneys', ['Buste/Fumée gauche', 'Buste/Fumée droite']))
    ops.append({'op': 'field', 'field': 'm_MoltenMaterial', 'mat': MOLTEN})
    # La finition : l'onde de choc (la lumière transparente du Rayon du jugement, teinte en jeu), les éclats de roche.
    ops.append({'op': 'field', 'field': 'm_RingMaterial', 'mat': 'Rayon_du_jugement'})
    ops.append({'op': 'field', 'field': 'm_DebrisMaterial', 'role': 'pierre', 'shade': 0.7})
    # Le tonnerre (découpé) et le grand impact (tel quel), de freesound.org, CC0 (2 novembre au soir, Docs/Credits_Licences.md).
    ops.append(field('m_ThunderClip', asset='Assets/_Project/Audio/Colosse_Tonnerre.wav'))
    ops.append(field('m_BoomClip', asset='Assets/_Project/Audio/513110__wakerone__cinematic-trailer-hit-explosion-cc0.mp3'))
    ops.append(field('m_ThrowClip', asset='Assets/507466__danjocross__clean-fast-swoosh.aiff'))
    ops.append(field('m_SlamClip', asset='Assets/kenney_impact-sounds/Audio/impactPlate_heavy_002.ogg'))
    ops.append(field('m_CatchClip', asset='Assets/kenney_impact-sounds/Audio/impactMetal_heavy_001.ogg'))
    # Le fer qui sonne à chaque coup de la masse (12.48 ; le pack de Kenney, CC0, déjà dans le projet).
    ops.append(field('m_ClangClip', asset='Assets/kenney_impact-sounds/Audio/impactBell_heavy_001.ogg'))
    # Les sons de la fonte (freesound.org, CC0 ; découpés le 2 novembre au soir, Docs/Credits_Licences.md) : posés aussi par
    # Editor/ColosseSonsBaker si le prefab est fait avant qu'ils soient dans le projet.
    ops.append(field('m_PourClip', asset='Assets/_Project/Audio/Colosse_Fournaise.wav'))
    ops.append(field('m_SizzleClip', asset='Assets/_Project/Audio/Colosse_Gresillement.wav'))
    ops.append(field('m_FlowClip', asset='Assets/_Project/Audio/Colosse_Coulee.wav'))
    return ops

# ---------------------------------------------------------------- Grand Manège : le Gonfleur, le Colisée ou les Montagnes russes, niveau 4
# Un carrousel géant de chevaux de bois (2 novembre, tard ; 12.51), perché sur son mât : les ennemis passent dessous, entre le socle
# et le bord du plateau. Tout ce qui tourne est sous « Manège » (m_Rotor, autour de +y) : le mât et ses miroirs, l'orgue et ses
# tuyaux qui fument (m_Pipes), le plateau et sa frise, les chevaux sur leurs barres dorées (m_Horses, m_Foals : repère au milieu de
# la selle, tournés dans le sens du manège), le plafond, la frise du chapiteau, le toit rayé, la lanterne, la boule dorée (m_Crown :
# les fusées en partent) et le fanion. Les ampoules s'appellent « Ampoule A », « B » ou « C » : les trois groupes de la chenille
# (UltimeManege). Les cotes suivent celles du script (k_DeckHeight, k_SeatRadius, k_RimRadius, k_RimHeight, k_HoofDrop).
MANEGE = 'Manège du 2 novembre'
BULB = 'Ampoule_de_manege'
MA_DECK = 3.6        # le dessous du plateau
MA_DECK_T = 0.4      # son épaisseur : le dessus à 4 m (k_DeckHeight)
MA_TOP_DECK = MA_DECK + MA_DECK_T
MA_DECK_R = 6.6      # le rayon du plateau
MA_HORSE_R = 5.2     # le cercle des grands chevaux (les places des ennemis, entre eux, à 6 m : k_SeatRadius)
MA_FOAL_R = 2.9      # celui des petits chevaux
MA_HORSES = 8
MA_FOALS = 4
MA_RIM_R = 7.2       # la frise du chapiteau (k_RimRadius)
MA_CEIL = 11.0       # le plafond, le bas de la frise (k_RimHeight)
MA_FRIEZE = 1.3      # la hauteur de la frise du chapiteau
MA_ROOF_R = 7.45     # le bord du toit
MA_APEX = 16.4       # la pointe du toit
MA_HOOF = 1.35       # les sabots, sous le repère d'un cheval (k_HoofDrop)
MA_HORSE_S = 0.85
MA_FOAL_S = 0.62
MA_BOB = 0.45        # l'aller-retour sur la barre (k_Bob ; les petits : 70 %)
# Le repère d'un cheval au repos : au plus bas, ses sabots restent au-dessus du plateau.
MA_HORSE_Y = MA_TOP_DECK + MA_HOOF * MA_HORSE_S + MA_BOB + 0.05
MA_FOAL_Y = MA_TOP_DECK + MA_HOOF * MA_FOAL_S + MA_BOB * 0.7 + 0.05

def blob(parent, name, a, b, width, depth, role, extra=0.0, shade=1.0):
    # Un ovale de a à b (une sphère étirée le long de son axe y, plus long de extra), comme tie.
    dx, dy, dz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    length = math.sqrt(dx * dx + dy * dy + dz * dz)
    pitch = math.degrees(math.atan2(math.hypot(dx, dz), dy))
    yaw = math.degrees(math.atan2(dx, dz))
    mid = [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, (a[2] + b[2]) / 2]
    return sph(parent, name, mid, [width, length + extra, depth], role=role, rot=[pitch, yaw, 0], shade=shade)

def bulb(parent, at, k):
    # Une ampoule, de l'un des trois groupes de la chenille.
    return sph(parent, 'Ampoule ' + 'ABC'[k % 3], at, [0.2, 0.2, 0.2], mat=BULB)

# Les jambes au galop (la pose des chevaux sauteurs) : l'épaule ou la hanche, le genou ou le jarret, le sabot. Les antérieurs
# repliés vers l'avant, les postérieurs tendus ; les sabots arrière à MA_HOOF sous le repère.
MA_LEGS = [
    ([0.26, -0.2, 0.85], [0.26, -0.42, 1.42], [0.26, -0.9, 1.55]),
    ([-0.26, -0.15, 0.85], [-0.26, -0.28, 1.5], [-0.26, -0.8, 1.7]),
    ([0.28, -0.22, -0.82], [0.28, -0.78, -1.38], [0.28, -1.28, -1.22]),
    ([-0.28, -0.22, -0.82], [-0.28, -0.82, -1.3], [-0.28, -1.29, -1.08]),
]

def ma_horse(parent, name, at, yaw, scale, robe, selle):
    # Un cheval de bois au galop, repère au milieu de la selle, z vers l'avant : sa robe, son tapis de selle et son plumet de la
    # couleur de la selle, la crinière, la queue, les sabots et les garnitures dorés.
    ops = [empty(parent, name, at, [0, yaw, 0])]
    h = parent + '/' + name
    ops.append(sph(h, 'Corps', [0, 0, 0], [0.95, 1.0, 2.2], role=robe))
    ops.append(sph(h, 'Poitrail', [0, 0.12, 0.8], [0.9, 1.0, 0.95], role=robe))
    ops.append(sph(h, 'Croupe', [0, 0.08, -0.78], [0.98, 1.0, 1.0], role=robe))
    # L'encolure, de la poitrine à la nuque, et son collier doré ; la tête penchée, son museau, ses oreilles, ses yeux.
    ops.append(blob(h, 'Encolure', [0, 0.3, 0.92], [0, 1.32, 1.46], 0.52, 0.62, robe, extra=0.3))
    ops.append(sph(h, 'Collier', [0, 0.62, 1.02], [0.6, 0.16, 0.7], role='accent', rot=[28, 0, 0]))
    ops.append(box(h, 'Tête', [0, 1.35, 1.75], [0.42, 0.46, 0.95], role=robe, rot=[35, 0, 0]))
    ops.append(box(h, 'Museau', [0, 1.06, 2.15], [0.36, 0.38, 0.4], role=robe, rot=[35, 0, 0], shade=0.85))
    for s in (-1, 1):
        ops.append(sph(h, 'Oreille', [s * 0.12, 1.74, 1.42], [0.11, 0.34, 0.13], role=robe, rot=[-10, 0, -s * 12]))
        ops.append(sph(h, 'Œil', [s * 0.205, 1.47, 1.8], [0.08, 0.12, 0.13], role='noir'))
    # La crinière, le toupet et le plumet sur sa base dorée.
    ops.append(blob(h, 'Crinière', [0, 0.62, 0.66], [0, 1.66, 1.24], 0.14, 0.34, 'accent', extra=0.1))
    ops.append(sph(h, 'Toupet', [0, 1.5, 1.92], [0.2, 0.14, 0.26], role='accent'))
    ops.append(sph(h, 'Base du plumet', [0, 1.76, 1.58], [0.18, 0.12, 0.18], role='accent'))
    ops.append(sph(h, 'Plumet', [0, 2.0, 1.55], [0.16, 0.55, 0.16], role=selle))
    # Le tapis de selle autour du ventre, son galon doré ; la selle de cuir, son pommeau, son troussequin.
    ops.append(sph(h, 'Tapis', [0, 0.04, -0.05], [1.0, 1.05, 1.0], role=selle))
    ops.append(sph(h, 'Galon', [0, 0.04, -0.05], [1.03, 1.08, 0.14], role='accent'))
    ops.append(box(h, 'Selle', [0, 0.52, -0.05], [0.62, 0.14, 0.8], role='bois'))
    ops.append(sph(h, 'Pommeau', [0, 0.68, 0.3], [0.16, 0.22, 0.16], role='accent'))
    ops.append(sph(h, 'Troussequin', [0, 0.64, -0.42], [0.5, 0.18, 0.14], role='accent'))
    # Les jambes : la cuisse, le genou, le canon, le sabot doré.
    for k, (a, b, c) in enumerate(MA_LEGS):
        ops.append(blob(h, 'Cuisse', a, b, 0.3, 0.34, robe, extra=0.16))
        ops.append(sph(h, 'Genou', b, [0.2, 0.2, 0.2], role=robe, shade=0.9))
        ops.append(tie(h, 'Canon', b, c, role=robe, radius=0.085))
        ops.append(blob(h, 'Sabot', [c[0], c[1] + 0.07, c[2]], [c[0], c[1] - 0.07, c[2]], 0.22, 0.26, 'accent'))
    ops.append(blob(h, 'Queue', [0, 0.38, -1.18], [0, -0.55, -1.62], 0.22, 0.34, 'accent', extra=0.15))
    ops.append({'op': 'set', 'path': h, 'scale': [scale, scale, scale]})
    return ops

def ma_pole(parent, name, x, z):
    # Une barre dorée, du plateau au plafond, et ses bagues.
    ops = [cyl(parent, name, [x, (MA_TOP_DECK + MA_CEIL) / 2, z], [0.16, MA_CEIL - MA_TOP_DECK + 0.012, 0.16], role='accent')]
    for y in (MA_TOP_DECK + 0.5, MA_CEIL - 1.4, MA_CEIL - 0.6):
        ops.append(cyl(parent, 'Bague', [x, y, z], [0.26, 0.1, 0.26], role='accent'))
    return ops

MA_ROOF_CUTS = 8     # les morceaux d'un pan du toit, le long de la pente

def ma_roof(m):
    # Le toit rayé : seize pans rouges et crème. Un pan est un trapèze, et une forme simple un rectangle : chaque pan est fait de huit
    # morceaux, chacun à la largeur du pan à sa hauteur (un rectangle plus large dépasserait au-dessus du pan voisin, près de la
    # pointe) ; un morceau sur deux un peu décollé (pas de faces dans le même plan là où ils se chevauchent). Une nervure dorée
    # couvre la jointure de deux pans ; ses ampoules.
    ops = []
    y0 = MA_CEIL + MA_FRIEZE - 0.06
    height = MA_APEX - y0
    slope = math.hypot(MA_ROOF_R, height)
    alpha = math.degrees(math.atan2(height, MA_ROOF_R))
    half = math.radians(360 / 32)
    for k in range(16):
        phi = k * 22.5 + 11.25
        a = math.radians(phi)
        normal = [math.sin(a) * math.sin(math.radians(alpha)), math.cos(math.radians(alpha)), math.cos(a) * math.sin(math.radians(alpha))]
        for s in range(MA_ROOF_CUTS):
            mid = (s + 0.5) / MA_ROOF_CUTS
            r = MA_ROOF_R * (1 - mid)
            width = 2 * r * math.tan(half) * 1.08
            lift = 0.05 + (s % 2) * 0.012
            at = [math.sin(a) * r + normal[0] * lift, y0 + height * mid + normal[1] * lift, math.cos(a) * r + normal[2] * lift]
            ops.append(box(m, 'Pan', at, [width, 0.1, slope / MA_ROOF_CUTS * 1.04], role='rouge' if k % 2 else 'creme', rot=[alpha, phi, 0]))
    for k in range(16):
        psi = k * 22.5
        a = math.radians(psi)
        normal = [math.sin(a) * math.sin(math.radians(alpha)), math.cos(math.radians(alpha)), math.cos(a) * math.sin(math.radians(alpha))]
        out = 0.12
        at = [math.sin(a) * MA_ROOF_R / 2 + normal[0] * out, y0 + height / 2 + normal[1] * out, math.cos(a) * MA_ROOF_R / 2 + normal[2] * out]
        ops.append(box(m, 'Nervure', at, [0.42, 0.14, slope * 0.98], role='accent', rot=[alpha, psi, 0]))
        for j, f in enumerate((0.25, 0.55)):
            r = MA_ROOF_R * (1 - f)
            lift = out + 0.07 + 0.1 - 0.012
            ops.append(bulb(m, [math.sin(a) * r + normal[0] * lift, y0 + height * f + normal[1] * lift, math.cos(a) * r + normal[2] * lift], k * 2 + j))
    return ops

def manege():
    ops = []
    # Le socle de pierre et sa marche, cerclé d'or : il ne tourne pas.
    ops.append(cyl('', 'Marche', [0, 0.06, 0], [5.0, 0.12, 5.0], role='pierre', shade=0.6))
    ops.append(cyl('', 'Socle', [0, 0.357, 0], [4.0, 0.486, 4.0], role='pierre', shade=0.8))
    ops.append(cyl('', 'Cercle', [0, 0.54, 0], [4.06, 0.1, 4.06], role='accent'))
    ops.append(empty('', 'Manège', [0, 0, 0]))
    m = 'Manège'
    # Le mât : en bas, sous le plateau, peint en rouge, et ses jambes de force ; en haut, crème, cerclé d'or, ses huit miroirs.
    low_bottom, low_top = 0.594, MA_DECK + 0.006
    ops.append(cyl(m, 'Pied du mât', [0, (low_bottom + low_top) / 2, 0], [1.2, low_top - low_bottom, 1.2], role='rouge'))
    for y in (1.1, 3.3):
        ops.append(cyl(m, 'Bague du pied', [0, y, 0], [1.32, 0.16, 1.32], role='accent'))
    for k in range(8):
        a = math.radians(k * 45 + 22.5)
        ops.append(tie(m, 'Jambe de force', [math.sin(a) * 0.5, 1.3, math.cos(a) * 0.5],
                       [math.sin(a) * 4.6, MA_DECK + 0.006, math.cos(a) * 4.6], role='bois', radius=0.13))
    top_bottom, top_top = MA_TOP_DECK - 0.006, MA_CEIL + 0.012
    ops.append(cyl(m, 'Mât', [0, (top_bottom + top_top) / 2, 0], [1.7, top_top - top_bottom, 1.7], role='creme'))
    for y in (4.25, 6.3, 8.9, 10.75):
        ops.append(cyl(m, 'Bague du mât', [0, y, 0], [1.82, 0.16, 1.82], role='accent'))
    for k in range(8):
        a = math.radians(k * 45)
        ops.append(box(m, 'Cadre', [math.sin(a) * 0.864, 7.6, math.cos(a) * 0.864], [0.62, 2.04, 0.04], role='accent', rot=[0, k * 45, 0]))
        ops.append(box(m, 'Miroir', [math.sin(a) * 0.886, 7.6, math.cos(a) * 0.886], [0.52, 1.9, 0.06], role='miroir', rot=[0, k * 45, 0]))
    # L'orgue, sur le plateau devant le mât : dix tuyaux dorés, les plus grands au milieu, qui fument ; son tambour derrière.
    vapeur = 0
    for k in range(10):
        a = math.radians(-54 + k * 12)
        tall = 1.0 + 1.0 * (1 - abs(k - 4.5) / 4.5)
        at = [math.sin(a) * 1.25, MA_TOP_DECK - 0.006 + tall / 2, math.cos(a) * 1.25]
        ops.append(cyl(m, 'Tuyau', at, [0.22, tall, 0.22], role='accent'))
        if 3 <= k <= 6:
            ops.append(empty(m, 'Vapeur %d' % vapeur, [at[0], MA_TOP_DECK + tall + 0.1, at[2]]))
            vapeur += 1
    ops.append(cyl(m, 'Tambour', [0, MA_TOP_DECK - 0.006 + 0.4, -1.45], [0.9, 0.8, 0.9], role='rouge'))
    for y in (MA_TOP_DECK + 0.06, MA_TOP_DECK + 0.74):
        ops.append(cyl(m, 'Cercle du tambour', [0, y, -1.45], [0.94, 0.08, 0.94], role='accent'))
    # Le plateau de planches, son bord doré, sa frise rouge et crème qui pend dessous, une ampoule sur chaque panneau. Deux panneaux
    # voisins se chevauchent aux coins : un sur deux un peu moins haut (pas de faces dans le même plan).
    ops.append(cyl(m, 'Plateau', [0, MA_DECK + MA_DECK_T / 2, 0], [2 * MA_DECK_R, MA_DECK_T, 2 * MA_DECK_R], role='plancher'))
    ops.append(cyl(m, 'Bord doré', [0, MA_DECK + 0.2, 0], [2 * (MA_DECK_R + 0.05), 0.16, 2 * (MA_DECK_R + 0.05)], role='accent'))
    for k in range(24):
        a = math.radians(k * 15 + 7.5)
        r = MA_DECK_R - 0.006 + 0.04
        ops.append(box(m, 'Frise du plateau', [math.sin(a) * r, 3.4, math.cos(a) * r], [1.76, 0.7 - 0.024 * (k % 2), 0.08],
                       role='rouge' if k % 2 else 'creme', rot=[0, k * 15 + 7.5, 0]))
        rb = r + 0.04 + 0.1 - 0.012
        ops.append(bulb(m, [math.sin(a) * rb, 3.28, math.cos(a) * rb], k))
    # Les barres et les chevaux : huit grands sur le cercle extérieur, quatre petits au milieu.
    robes = ['blanc', 'bai', 'gris', 'noir', 'isabelle', 'alezan', 'blanc', 'gris']
    selles = ['rouge', 'bleu', 'violet', 'vert', 'rose', 'bleu', 'rouge', 'vert']
    for k in range(MA_HORSES):
        a = math.radians(k * 360 / MA_HORSES)
        x, z = math.sin(a) * MA_HORSE_R, math.cos(a) * MA_HORSE_R
        ops += ma_pole(m, 'Barre %d' % k, x, z)
        ops += ma_horse(m, 'Cheval %d' % k, [x, MA_HORSE_Y, z], k * 360 / MA_HORSES + 90, MA_HORSE_S, robes[k], selles[k])
    foal_robes = ['isabelle', 'blanc', 'alezan', 'gris']
    foal_selles = ['bleu', 'rose', 'vert', 'violet']
    for k in range(MA_FOALS):
        angle = k * 360 / MA_FOALS + 22.5
        a = math.radians(angle)
        x, z = math.sin(a) * MA_FOAL_R, math.cos(a) * MA_FOAL_R
        ops += ma_pole(m, 'Barre du petit %d' % k, x, z)
        ops += ma_horse(m, 'Poulain %d' % k, [x, MA_FOAL_Y, z], angle + 90, MA_FOAL_S, foal_robes[k], foal_selles[k])
    # Le plafond et sa rosace dorée.
    ops.append(cyl(m, 'Plafond', [0, MA_CEIL + 0.08, 0], [2 * 7.22, 0.16, 2 * 7.22], role='creme', shade=0.92))
    ops.append(cyl(m, 'Rosace', [0, MA_CEIL - 0.03 + 0.006, 0], [2.6, 0.06, 2.6], role='accent'))
    # La frise du chapiteau : vingt-quatre panneaux rouges et crème, chacun son miroir ovale cerclé d'or, deux ampoules, sa corniche
    # dorée ; des pampilles dorées dessous, entre deux panneaux.
    for k in range(24):
        a = math.radians(k * 15)
        r = MA_RIM_R + 0.05
        rot = [0, k * 15, 0]
        ops.append(box(m, 'Frise', [math.sin(a) * r, MA_CEIL + MA_FRIEZE / 2, math.cos(a) * r], [1.9, MA_FRIEZE - 0.024 * (k % 2), 0.1],
                       role='rouge' if k % 2 else 'creme', rot=rot))
        ops.append(sph(m, 'Cadre du miroir', [math.sin(a) * 7.296, MA_CEIL + MA_FRIEZE / 2, math.cos(a) * 7.296], [1.04, 0.69, 0.05],
                       role='accent', rot=rot))
        ops.append(sph(m, 'Miroir', [math.sin(a) * 7.3, MA_CEIL + MA_FRIEZE / 2, math.cos(a) * 7.3], [0.9, 0.55, 0.06], role='miroir', rot=rot))
        rb = r + 0.05 + 0.1 - 0.012
        ops.append(bulb(m, [math.sin(a) * rb, MA_CEIL + 0.18, math.cos(a) * rb], k))
        ops.append(bulb(m, [math.sin(a) * rb, MA_CEIL + MA_FRIEZE - 0.2, math.cos(a) * rb], k + 1))
        ops.append(box(m, 'Corniche', [math.sin(a) * 7.26, MA_CEIL + MA_FRIEZE + 0.06 - 0.006 + 0.012 * (k % 2), math.cos(a) * 7.26],
                       [1.95, 0.12, 0.22], role='accent', rot=rot))
        b = math.radians(k * 15 + 7.5)
        ops.append(sph(m, 'Pampille', [math.sin(b) * 7.32, MA_CEIL - 0.12, math.cos(b) * 7.32], [0.16, 0.3, 0.16], role='accent'))
    ops += ma_roof(m)
    # La pointe : la couronne dorée, la lanterne qui luit, son chapeau, la boule dorée (les fusées en partent), la hampe et le fanion.
    ops.append(cyl(m, 'Couronne du toit', [0, MA_APEX - 0.25, 0], [1.6, 0.3, 1.6], role='accent'))
    ops.append(cyl(m, 'Lanterne', [0, MA_APEX + 0.294, 0], [0.9, 0.8, 0.9], mat=BULB))
    ops.append(cyl(m, 'Chapeau', [0, MA_APEX + 0.748, 0], [1.1, 0.12, 1.1], role='accent'))
    ops.append(sph(m, 'Boule', [0, MA_APEX + 1.146, 0], [0.7, 0.7, 0.7], role='accent'))
    ops.append(empty(m, 'Sommet', [0, MA_APEX + 1.146, 0]))
    ops.append(cyl(m, 'Hampe', [0, MA_APEX + 2.184, 0], [0.08, 1.4, 0.08], role='accent'))
    ops.append(box(m, 'Fanion', [0, MA_APEX + 2.6, 0.5], [0.04, 0.55, 0.95], role='rouge'))
    ops.append(field('m_Rotor', ['Manège']))
    ops.append(field('m_Horses', ['Manège/Cheval %d' % k for k in range(MA_HORSES)]))
    ops.append(field('m_Foals', ['Manège/Poulain %d' % k for k in range(MA_FOALS)]))
    ops.append(field('m_Crown', ['Manège/Sommet']))
    ops.append(field('m_Pipes', ['Manège/Vapeur %d' % k for k in range(4)]))
    ops.append({'op': 'field', 'field': 'm_BulbMaterial', 'mat': BULB})
    ops.append({'op': 'field', 'field': 'm_RingMaterial', 'mat': 'Rayon_du_jugement'})
    ops.append({'op': 'field', 'field': 'm_DebrisMaterial', 'role': 'pierre', 'shade': 0.7})
    # Les sons choisis par le joueur sur freesound.org (CC0), découpés le 2 novembre, tard (Docs/Credits_Licences.md) : l'orgue (en
    # boucle), la fanfare et sa première phrase, le galop (en boucle), le hennissement, la fusée, le bouquet ; Editor/ManegeSonsBaker
    # les pose aussi si le prefab est fait avant eux. Et ceux du projet : le sabot, le ruban, le souffle, le grand impact.
    ops.append(field('m_OrganClip', asset='Assets/_Project/Audio/Manege_Orgue.wav'))
    ops.append(field('m_FanfareClip', asset='Assets/_Project/Audio/Manege_Fanfare.wav'))
    ops.append(field('m_FlourishClip', asset='Assets/_Project/Audio/Manege_Fanfare_Courte.wav'))
    ops.append(field('m_GallopClip', asset='Assets/_Project/Audio/Manege_Galop.wav'))
    ops.append(field('m_NeighClip', asset='Assets/_Project/Audio/Manege_Hennissement.wav'))
    ops.append(field('m_RocketClip', asset='Assets/_Project/Audio/Manege_Fusee.wav'))
    ops.append(field('m_BurstClip', asset='Assets/_Project/Audio/Manege_Bouquet.wav'))
    ops.append(field('m_StompClip', asset='Assets/kenney_impact-sounds/Audio/impactWood_heavy_001.ogg'))
    ops.append(field('m_CatchClip', asset='Assets/kenney_impact-sounds/Audio/impactSoft_medium_001.ogg'))
    ops.append(field('m_WhooshClip', asset='Assets/507466__danjocross__clean-fast-swoosh.aiff'))
    ops.append(field('m_BoomClip', asset='Assets/_Project/Audio/513110__wakerone__cinematic-trailer-hit-explosion-cc0.mp3'))
    return ops

# ---------------------------------------------------------------- Grande Horloge : le Bowling ou le Beffroi, niveau 4
# Une horloge astronomique de glace et d'or (2 novembre, tard ; 12.52, le choix du joueur) : elle flotte au-dessus de son socle de
# glace (« Horloge », m_Float, au centre du cadran, à 11 m), reliée à lui par un rayon de glace. Le cadran (« Face », m_Face : il se
# tourne vers le joueur en jeu ; +z vers l'avant, les aiguilles tournent autour de +z) : le disque de glace cerclé d'or, son ciel de
# nuit étoilé, ses douze chiffres romains de glace qui luit (« Chiffre 1 » à « Chiffre 12 », m_Numerals : ils partent un à un), ses
# trois aiguilles, ses engrenages derrière, sa cloche sous une arche dorée, des stalactites dessous, et la flèche de glace du modèle
# (cachée en jeu : les échos en sont des copies). Trois grands anneaux tournent autour (« Anneau 0 » à « 2 », penchés ; leur enfant
# « Rotation », m_Rings, tourne autour de son axe y), plus grands que le cadran et sa cloche.
HORLOGE = 'Horloge du 2 novembre'
GLOW = 'Givre_lumineux'
HO_CENTER = 11.0     # le centre du cadran
HO_DIAL_R = 4.0      # le rayon du cadran
HO_NUMERAL_R = 3.15  # le cercle des chiffres
HO_FRONT = 0.15      # la face avant du cadran (il fait 0,3 m d'épaisseur)
HO_RINGS = [(7.8, [0, 0, 0], 0.28, 'glace'), (6.9, [70, 0, 0], 0.28, 'argent'), (6.2, [0, 0, 25], 0.7, 'accent')]

# Les lettres des chiffres romains (en barres de glace, 0,9 m de haut) : leur largeur.
HO_LETTER = {'I': 0.16, 'V': 0.5, 'X': 0.56}

def ho_letter(parent, letter, x, z):
    # Une lettre en barres de glace qui luit, centrée en x (repère du chiffre).
    h = 0.9
    if letter == 'I':
        return [box(parent, 'Barre', [x, 0, z], [0.16, h, 0.12], mat=GLOW)]
    if letter == 'V':
        tilt = math.degrees(math.atan2(0.17, h))
        return [box(parent, 'Barre', [x + s * 0.125, 0, z + 0.012 * (s > 0)], [0.15, math.hypot(h, 0.34), 0.12], mat=GLOW, rot=[0, 0, -s * tilt])
                for s in (-1, 1)]
    tilt = math.degrees(math.atan2(0.5, h))
    return [box(parent, 'Barre', [x, 0, z + 0.012 * (s > 0)], [0.15, math.hypot(h, 0.5), 0.12], mat=GLOW, rot=[0, 0, s * tilt]) for s in (-1, 1)]

def ho_numeral(parent, k):
    # Le chiffre k (1 à 12), à sa place sur le cadran (le XII en haut, dans le sens des aiguilles d'une montre vu de face) ; ses
    # lettres de gauche à droite pour qui le regarde de face (vers -x du cadran).
    text = ['I', 'II', 'III', 'IV', 'V', 'VI', 'VII', 'VIII', 'IX', 'X', 'XI', 'XII'][k - 1]
    a = math.radians(k * 30)
    name = 'Chiffre %d' % k
    ops = [empty(parent, name, [-math.sin(a) * HO_NUMERAL_R, math.cos(a) * HO_NUMERAL_R, HO_FRONT + 0.06 + 0.014])]
    n = parent + '/' + name
    gap = 0.08
    width = sum(HO_LETTER[c] for c in text) + gap * (len(text) - 1)
    left = width / 2
    for c in text:
        ops += ho_letter(n, c, left - HO_LETTER[c] / 2, 0)
        left -= HO_LETTER[c] + gap
    return ops

def ho_gear(parent, name, at, radius, teeth, role):
    # Un engrenage dans le plan du cadran : son disque, ses dents, son moyeu doré.
    ops = [empty(parent, name, at)]
    g = parent + '/' + name
    ops.append(cyl(g, 'Disque', [0, 0, 0], [2 * radius, 0.16, 2 * radius], role=role, rot=[90, 0, 0]))
    for j in range(teeth):
        a = j * 360 / teeth
        r = math.radians(a)
        ops.append(box(g, 'Dent', [math.cos(r) * (radius + 0.1), math.sin(r) * (radius + 0.1), 0], [0.26, 0.32, 0.14], role=role,
                       rot=[0, 0, a - 90]))
    ops.append(cyl(g, 'Moyeu', [0, 0, 0], [0.4, 0.22, 0.4], role='accent', rot=[90, 0, 0]))
    return ops

def ho_ring(parent, k, radius, tilt, thick, role):
    # Un grand anneau autour de l'horloge : 32 morceaux (un sur deux un peu moins épais : pas de faces dans le même plan là où ils se
    # chevauchent), huit gemmes de glace qui luit ; sur le premier, un soleil d'or et une lune d'argent.
    name = 'Anneau %d' % k
    ops = [empty(parent, name, [0, 0, 0], tilt)]
    r = parent + '/' + name + '/Rotation'
    ops.append(empty(parent + '/' + name, 'Rotation', [0, 0, 0]))
    count = 32
    length = 2 * math.pi * radius / count * 1.06
    width = 0.28 if thick < 0.5 else thick
    height = 0.28 if thick < 0.5 else 0.12
    for j in range(count):
        a = j * 2 * math.pi / count
        ops.append(box(r, 'Morceau', [math.cos(a) * radius, 0, math.sin(a) * radius],
                       [width, height - 0.024 * (j % 2), length], role=role, rot=[0, -math.degrees(a), 0]))
        if j % 4 == 0:
            out = radius + width / 2 + 0.18 - 0.012
            ops.append(sph(r, 'Gemme', [math.cos(a) * out, 0, math.sin(a) * out], [0.36, 0.36, 0.36], mat=GLOW))
    if k == 0:
        top = height / 2 + 0.5 - 0.012
        ops.append(sph(r, 'Soleil', [radius, top, 0], [1.0, 1.0, 1.0], role='accent'))
        ops.append(sph(r, 'Lune', [-radius, top - 0.15, 0], [0.7, 0.7, 0.7], role='argent'))
    return ops

def horloge():
    ops = []
    # Le socle de glace sur sa marche de pierre, cerclé d'or, sa rosace ; trois grands cristaux qui luisent de l'intérieur.
    ops.append(cyl('', 'Marche', [0, 0.06, 0], [7.0, 0.12, 7.0], role='pierre', shade=0.6))
    ops.append(cyl('', 'Socle', [0, 0.514, 0], [5.0, 0.8, 5.0], role='glace_sombre'))
    ops.append(cyl('', 'Cercle', [0, 0.85, 0], [5.08, 0.12, 5.08], role='accent'))
    ops.append(cyl('', 'Rosace', [0, 0.914 + 0.03 - 0.006, 0], [1.4, 0.06, 1.4], role='accent'))
    for k in range(3):
        a = math.radians(k * 120 + 30)
        at = [math.sin(a) * 2.0, 0.914 + 1.8 - 0.1, math.cos(a) * 2.0]
        rot = [math.cos(a) * 16, 0, -math.sin(a) * 16]
        ops.append(sph('', 'Cristal', at, [0.75, 4.0 - 0.5 * k, 0.75], role='glace', rot=rot))
        ops.append(sph('', 'Cœur du cristal', at, [0.3, 3.0 - 0.4 * k, 0.3], mat=GLOW, rot=rot))
    # L'horloge qui flotte, et le rayon de glace qui la relie au socle.
    ops.append(empty('', 'Horloge', [0, HO_CENTER, 0]))
    h = 'Horloge'
    ops.append(cyl(h, 'Rayon', [0, -HO_DIAL_R - (HO_CENTER - HO_DIAL_R - 0.9) / 2, -0.3], [0.24, HO_CENTER - HO_DIAL_R - 0.6, 0.24], mat=GLOW))
    # Le cadran : le disque de glace, son bord doré, son ciel de nuit, ses étoiles, son soleil et sa lune, les marques des heures.
    ops.append(empty(h, 'Face', [0, 0, 0]))
    f = h + '/Face'
    ops.append(cyl(f, 'Cadran', [0, 0, 0], [2 * HO_DIAL_R, 2 * HO_FRONT, 2 * HO_DIAL_R], role='glace_sombre', rot=[90, 0, 0]))
    ops.append(cyl(f, 'Bord', [0, 0, -0.06], [2 * HO_DIAL_R + 0.5, 0.24, 2 * HO_DIAL_R + 0.5], role='accent', rot=[90, 0, 0]))
    ops.append(cyl(f, 'Ciel', [0, 0, HO_FRONT + 0.02 - 0.006], [5.4, 0.04, 5.4], role='nuit', rot=[90, 0, 0]))
    sky = HO_FRONT + 0.034
    for x, y, s in ((1.3, 1.6, 0.16), (-1.6, 1.1, 0.12), (-0.6, 2.2, 0.14), (2.1, 0.2, 0.12), (-2.2, -0.4, 0.16), (0.9, -1.9, 0.14),
                    (-1.0, -1.7, 0.12), (1.8, -1.1, 0.12), (-0.2, 1.3, 0.1), (0.4, -0.9, 0.1)):
        ops.append(sph(f, 'Étoile', [x, y, sky + s / 2 - 0.012], [s, s, s], mat=GLOW))
    ops.append(sph(f, 'Soleil', [1.15, 0.9, sky + 0.2 - 0.012], [0.55, 0.55, 0.4], role='accent'))
    ops.append(sph(f, 'Lune', [-1.2, -0.85, sky + 0.16 - 0.012], [0.42, 0.42, 0.32], role='argent'))
    for k in range(12):
        a = math.radians(k * 30)
        ops.append(box(f, 'Marque', [-math.sin(a) * 3.85, math.cos(a) * 3.85, HO_FRONT + 0.03 - 0.006], [0.12, 0.34, 0.06], role='accent',
                       rot=[0, 0, k * 30]))
    for k in range(1, 13):
        ops += ho_numeral(f, k)
    # Les aiguilles (repère au centre, vers le haut), l'une devant l'autre ; le capuchon doré.
    ops.append(empty(f, 'Aiguille des heures', [0, 0, 0.33]))
    ops.append(box(f + '/Aiguille des heures', 'Tige', [0, 0.9, 0], [0.28, 2.3, 0.08], role='accent'))
    ops.append(box(f + '/Aiguille des heures', 'Pointe', [0, 2.05, 0], [0.42, 0.42, 0.08], role='accent', rot=[0, 0, 45]))
    ops.append(empty(f, 'Aiguille des minutes', [0, 0, 0.43]))
    ops.append(box(f + '/Aiguille des minutes', 'Tige', [0, 1.3, 0], [0.18, 3.1, 0.06], role='accent'))
    ops.append(box(f + '/Aiguille des minutes', 'Pointe', [0, 2.85, 0], [0.3, 0.3, 0.06], role='accent', rot=[0, 0, 45]))
    ops.append(empty(f, 'Trotteuse', [0, 0, 0.51]))
    ops.append(box(f + '/Trotteuse', 'Tige', [0, 1.35, 0], [0.07, 3.3, 0.04], role='argent'))
    ops.append(sph(f + '/Trotteuse', 'Contrepoids', [0, -0.55, 0], [0.26, 0.26, 0.06], role='argent'))
    ops.append(sph(f, 'Capuchon', [0, 0, 0.6], [0.5, 0.5, 0.3], role='accent'))
    ops.append(empty(f, 'Centre', [0, 0, 1.0]))
    # Les engrenages derrière le cadran.
    ops += ho_gear(f, 'Engrenage 0', [1.7, -1.3, -HO_FRONT - 0.3], 1.0, 10, 'accent')
    ops += ho_gear(f, 'Engrenage 1', [-1.5, 1.0, -HO_FRONT - 0.3], 0.8, 8, 'argent')
    ops += ho_gear(f, 'Engrenage 2', [0.3, 2.0, -HO_FRONT - 0.55], 0.7, 8, 'accent')
    # La cloche sous son arche dorée, au sommet du cadran.
    top = HO_DIAL_R + 0.25
    for s in (-1, 1):
        ops.append(box(f, 'Pilier de l\'arche', [s * 0.75, top + 0.7 - 0.006, 0], [0.18, 1.4, 0.18], role='accent'))
    ops.append(box(f, 'Arche', [0, top + 1.4 + 0.09 - 0.012, 0], [1.7, 0.18, 0.22], role='accent'))
    ops.append(empty(f, 'Cloche', [0, top + 1.38, 0]))
    ops.append(sph(f + '/Cloche', 'Cloche', [0, -0.5, 0], [0.9, 0.95, 0.9], role='accent'))
    ops.append(cyl(f + '/Cloche', 'Bord', [0, -0.88, 0], [1.0, 0.1, 1.0], role='accent'))
    ops.append(sph(f + '/Cloche', 'Battant', [0, -1.0, 0], [0.2, 0.2, 0.2], role='argent'))
    # Les stalactites sous le cadran.
    for x, length in ((-2.2, 1.2), (-1.3, 1.8), (-0.5, 1.4), (0.4, 2.1), (1.2, 1.5), (2.0, 1.1), (2.7, 0.8)):
        y = -math.sqrt(max(0.0, HO_DIAL_R ** 2 - x ** 2)) + 0.1
        ops.append(sph(f, 'Stalactite', [x, y - length / 2, -0.05], [0.32, length, 0.32], role='glace'))
    # La flèche de glace du modèle (cachée en jeu, rangée derrière le cadran), le long de +z : la hampe, la pointe, l'empennage qui luit.
    ops.append(empty(f, 'Flèche de glace', [0, 0, -3.2]))
    a = f + '/Flèche de glace'
    ops.append(cyl(a, 'Hampe', [0, 0, 0], [0.24, 3.2, 0.24], role='glace', rot=[90, 0, 0]))
    ops.append(sph(a, 'Pointe', [0, 0, 1.85], [0.5, 0.5, 1.1], mat=GLOW))
    for j in range(3):
        ops.append(box(a, 'Plume', [0, 0, -1.35], [0.03, 0.55, 0.75], mat=GLOW, rot=[0, 0, j * 120]))
    # Les trois grands anneaux.
    for k, (radius, tilt, thick, role) in enumerate(HO_RINGS):
        ops += ho_ring(h, k, radius, tilt, thick, role)
    ops.append(field('m_Float', ['Horloge']))
    ops.append(field('m_Face', ['Horloge/Face']))
    ops.append(field('m_Center', ['Horloge/Face/Centre']))
    ops.append(field('m_Numerals', ['Horloge/Face/Chiffre %d' % k for k in range(1, 13)]))
    ops.append(field('m_HourHand', ['Horloge/Face/Aiguille des heures']))
    ops.append(field('m_MinuteHand', ['Horloge/Face/Aiguille des minutes']))
    ops.append(field('m_SecondHand', ['Horloge/Face/Trotteuse']))
    ops.append(field('m_Gears', ['Horloge/Face/Engrenage %d' % k for k in range(3)]))
    ops.append(field('m_Rings', ['Horloge/Anneau %d/Rotation' % k for k in range(3)]))
    ops.append(field('m_Bell', ['Horloge/Face/Cloche']))
    ops.append(field('m_ArrowTemplate', ['Horloge/Face/Flèche de glace']))
    ops.append({'op': 'field', 'field': 'm_GlowMaterial', 'mat': GLOW})
    ops.append({'op': 'field', 'field': 'm_RingMaterial', 'mat': 'Rayon_du_jugement'})
    ops.append({'op': 'field', 'field': 'm_BubbleMaterial', 'mat': 'Bulle'})
    ops.append({'op': 'field', 'field': 'm_FrostMaterial', 'mat': 'Givre'})
    ops.append({'op': 'field', 'field': 'm_DebrisMaterial', 'role': 'glace'})
    # Les sons choisis sur freesound.org (CC0), téléchargés par le joueur, découpés le 3 novembre (Tools/sons/decoupe_horloge.py ;
    # Docs/Credits_Licences.md) : le tic, un coup de cloche, le temps qui s'arrête et qui reprend, la glace qui éclate (en série, et un
    # craquement seul), le carillon ; Editor/HorlogeSonsBaker les pose aussi sur un prefab fait avant eux. Et ceux du projet.
    ops.append(field('m_TickClip', asset='Assets/_Project/Audio/Horloge_Tic.wav'))
    ops.append(field('m_BellClip', asset='Assets/_Project/Audio/Horloge_Cloche.wav'))
    ops.append(field('m_StopClip', asset='Assets/_Project/Audio/Horloge_Arret.wav'))
    ops.append(field('m_ResumeClip', asset='Assets/_Project/Audio/Horloge_Reprise.wav'))
    ops.append(field('m_ShatterClip', asset='Assets/_Project/Audio/Horloge_Bris.wav'))
    ops.append(field('m_ImpactClip', asset='Assets/_Project/Audio/Horloge_Eclat.wav'))
    ops.append(field('m_ChimeClip', asset='Assets/_Project/Audio/Horloge_Carillon.wav'))
    ops.append(field('m_WhooshClip', asset='Assets/507466__danjocross__clean-fast-swoosh.aiff'))
    ops.append(field('m_BoomClip', asset='Assets/_Project/Audio/513110__wakerone__cinematic-trailer-hit-explosion-cc0.mp3'))
    return ops

def ultimes():
    return [
        {'name': 'Colosse de forge', 'prefab': 'Duos/Ultime_Colosse', 'definition': 'Ultime_Colosse',
         'trios': ['Trio_Enclume', 'Trio_Bombarde'], 'component': 'UltimeColosse', 'slot': 'Roadside', 'anywhere': True,
         'footprint': [4, 4], 'palette': 'Ultime_Colosse', 'color': '#FF9E40', 'mark': FORGE, 'rebuild': True, 'plans': [10],
         # Les textes d'avant (12.46) : remplacés s'ils n'ont pas été changés à la main (12.48).
         'descriptionBefore': 'Son marteau tombe où tombe ta flèche ; sa fournaise verse une coulée de fonte sur toute la voie.',
         'description': 'D\'une main il bat sa forge, de l\'autre il lance un marteau sur chaque flèche qui touche ; sa fournaise '
                        'verse une coulée de fonte sur toute la voie.',
         'levelsBefore': ['L\'ultime de la forge et de la poudre : un géant de fer à genoux, une fournaise dans la poitrine. Ta flèche '
                          'touche à 30 m, et il y lance son marteau : tout autour vole en l\'air (sans flèche, il vise le groupe le plus '
                          'serré, toutes les 5 s). Toutes les 25 s, la fonte coule de sa poitrine et descend toute la voie vers la forêt. '
                          'Dans l\'arène, son marteau plaque MK au sol.'],
         'levels': ['L\'ultime de la forge et de la poudre : un géant de fer à genoux, une fournaise dans la poitrine. De la main '
                    'gauche, il bat le lingot sur son enclume sans arrêt : chaque coup lance un éclat de fonte sur un ennemi à 30 m, '
                    'et au 5e, le lingot chauffé à blanc part sur le groupe le plus serré, avec la foudre. De la main droite, il lance '
                    'un marteau sur chaque ennemi que touche ta flèche (5 par seconde au plus). Toutes les 25 s, la fonte coule de sa '
                    'poitrine et descend toute la voie vers la forêt. Dans l\'arène, ses marteaux suivent tes flèches dans MK, et son '
                    'lingot le plaque au sol.'],
         'ops': colosse()},
        # Le Grand Manège (2 novembre, tard ; 12.51) : la famille de la fête foraine.
        {'name': 'Grand Manège', 'prefab': 'Duos/Ultime_Manege', 'definition': 'Ultime_Manege',
         'trios': ['Trio_Gonfleur', 'Trio_Colisee', 'Trio_Montagnes_russes'], 'component': 'UltimeManege', 'slot': 'Roadside',
         'anywhere': True, 'footprint': [4, 4], 'palette': 'Ultime_Manege', 'color': '#FF7AC8', 'mark': MANEGE, 'plans': [10],
         'description': 'Son ruban fait tourner les ennemis sur le manège, puis les éjecte sur leurs voisins ; un cheval de bois saute '
                        'sur chaque ennemi que touche ta flèche ; ses chevaux chargent sur toute la voie.',
         'levels': ['L\'ultime de la fête foraine : un carrousel géant de chevaux de bois, perché sur son mât. Toutes les 1,2 s, son '
                    'ruban doré attrape un ennemi à 30 m et le fait tourner parmi les chevaux ; au 5e tour, tous ceux qui tournent sont '
                    'éjectés d\'un coup sur le groupe le plus serré, sous le feu d\'artifice. Chaque ennemi que touche ta flèche, et un '
                    'de ses chevaux saute du manège et le piétine (5 par seconde au plus). Toutes les 25 s, ses chevaux descendent la '
                    'voie au galop jusqu\'à la forêt. Dans l\'arène, ses chevaux suivent tes flèches dans MK, et leur charge '
                    'l\'étourdit.'],
         'ops': manege()},
        # La Grande Horloge (2 novembre, tard ; 12.52) : la famille de la glace et du temps ; elle reprend Minuit du Beffroi.
        {'name': 'Grande Horloge', 'prefab': 'Duos/Ultime_Horloge', 'definition': 'Ultime_Horloge',
         'trios': ['Trio_Bowling', 'Trio_Beffroi'], 'component': 'UltimeHorloge', 'slot': 'Roadside', 'anywhere': True,
         'footprint': [4, 4], 'palette': 'Ultime_Horloge', 'color': '#8FD8FF', 'mark': HORLOGE, 'plans': [10],
         'description': 'Ses douze chiffres de glace tombent un à un sur les ennemis ; après le XII, Minuit arrête le temps sur toute la '
                        'carte, puis le givre brise tous les figés ; chaque flèche qui touche est rejouée par une flèche de glace géante.',
         'levels': ['L\'ultime de la glace et du temps : une horloge astronomique de glace et d\'or qui flotte au-dessus de son socle. À '
                    'chaque tic (1,2 s), l\'un de ses douze chiffres romains de glace part sur un ennemi à 30 m et givre ses voisins. '
                    'Après le XII, Minuit : le temps s\'arrête 9 s sur toute la carte, sauf pour toi (tes flèches attendent en l\'air) ; '
                    'au douzième coup, tes flèches repartent toutes ensemble et le givre de tous les figés éclate. Chaque flèche qui '
                    'touche est rejouée 1 s plus tard par une flèche de glace géante (5 par seconde au plus). Dans l\'arène, Minuit '
                    'arrête MK.'],
         'ops': horloge()},
    ]
