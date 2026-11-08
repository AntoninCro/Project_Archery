"""Le tableau d'équilibrage des tours, des duos et des trios (Docs/Equilibrage_Tours.xlsx).

Demandé par le joueur le 1er novembre au soir (Docs/Refonte_TowerDefense.md, partie 12.32), fait le 2 novembre ; les duos et
les trios ajoutés à sa demande le même jour. Le script lit les réglages par niveau dans les prefabs (YAML) et les assets de
difficulté, de vagues et d'ennemis, calcule pour chaque niveau ses dégâts de base par seconde (sur une cible, sur un groupe),
sa part des PV max par seconde, son contrôle, puis écrit le fichier Excel : les colonnes par vague y sont des formules (la
force des bâtiments et les PV de l'ennemi type, feuille « Vagues »), qui suivent les réglages si on les change dans le fichier.

Les estimations (combien d'ennemis un coup touche, la durée d'un cycle…) sont des hypothèses de Claude, écrites dans la colonne
« Calcul » de chaque ligne et dans la feuille « Méthode ». Les valeurs des prefabs font foi : relancer le script après les avoir
changées. La colonne « Changements du joueur » est gardée d'une fois sur l'autre (relue dans le fichier existant).

Usage (depuis Project Archery/) : py Tools/equilibrage_tours.py      (il faut openpyxl : py -m pip install openpyxl)
"""
import math
import os
import re
import sys

from openpyxl import Workbook, load_workbook
from openpyxl.comments import Comment
from openpyxl.formatting.rule import ColorScaleRule
from openpyxl.styles import Alignment, Border, Font, PatternFill, Side
from openpyxl.utils import get_column_letter

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(PROJECT, "Assets", "_Project")
OUTPUT = os.path.join(PROJECT, "Docs", "Equilibrage_Tours.xlsx")

# ------------------------------------------------------------------------------------------------ lecture des assets

_script_guids = None


def script_guid(name):
    """GUID du script C# (lu dans son .meta)."""
    global _script_guids
    if _script_guids is None:
        _script_guids = {}
        for folder, _, files in os.walk(os.path.join(ASSETS, "Scripts")):
            for file in files:
                if file.endswith(".cs.meta"):
                    with open(os.path.join(folder, file), encoding="utf-8", errors="replace") as handle:
                        match = re.search(r"guid: ([0-9a-f]{32})", handle.read())
                    if match:
                        _script_guids[file[:-8]] = match.group(1)
    return _script_guids[name]


def _value(text):
    text = text.strip()
    try:
        return float(text)
    except ValueError:
        return text


def _fields(block):
    """Les champs d'un bloc MonoBehaviour : nombres, listes de nombres, tableaux d'entiers (hexadécimal, petit-boutiste)."""
    fields = {}
    key = None
    for line in block.split("\n"):
        item = re.match(r"^  - (.*)$", line)
        if item and key is not None:
            if not isinstance(fields.get(key), list):
                fields[key] = []
            fields[key].append(_value(item.group(1)))
            continue
        match = re.match(r"^  (\w+):\s*(.*)$", line)
        if not match:
            continue
        key, raw = match.group(1), match.group(2)
        if raw == "":
            fields[key] = []
        elif re.fullmatch(r"[0-9a-f]{8,}", raw) and len(raw) % 8 == 0 and (key.endswith("PerLevel") or len(raw) >= 16):
            fields[key] = [int.from_bytes(bytes.fromhex(raw[i:i + 8]), "little") for i in range(0, len(raw), 8)]
        else:
            fields[key] = _value(raw)
    return fields


def component(relative_path, script):
    """Les réglages du composant <script> dans un prefab ou un asset."""
    with open(os.path.join(ASSETS, relative_path), encoding="utf-8", errors="replace") as handle:
        text = handle.read()
    guid = script_guid(script)
    for block in re.split(r"\n--- ", text):
        if "\nMonoBehaviour:" in block and "guid: " + guid in block:
            return _fields(block)
    raise LookupError(f"{script} introuvable dans {relative_path}")


def asset(relative_path):
    """Les champs d'un ScriptableObject (difficulté, vagues, ennemi)."""
    with open(os.path.join(ASSETS, relative_path), encoding="utf-8", errors="replace") as handle:
        return _fields(handle.read())


def per_level(values, level):
    """Comme Structure.ValueFor : la valeur du niveau, la dernière valant pour les niveaux au-delà."""
    return values[min(level, len(values)) - 1]


def tower(name, script):
    return component(f"Prefabs/Structures/{name}.prefab", script)


def duo(name, script):
    return component(f"Prefabs/Structures/Duos/{name}.prefab", script)


# ------------------------------------------------------------------------------------------------ physique (Enemy.Launch)

G = 9.81
LANDING_STUN = 0.5   # Enemy.Launch : k_LandingStun
FLYER_HEIGHT = 7.0   # FlyingEnemy : m_CruiseHeight

# Les hypothèses (feuille « Méthode ») : un groupe type de 5 ennemis serrés (1,5 m de rayon) ; une vitesse de marche de
# référence pour changer une distance en temps ; le débit d'ennemis qui limite les tours qui éjectent ou tuent leur cible.
GROUP = 5
SPEED = 2.5          # m/s, pour le contrôle (la colonne « Exposition » suit la vitesse de la vague 5, feuille « Vagues »)
FLOW_ONE = 0.5       # « 1 cible » : un ennemi à la fois, un nouveau toutes les 2 s au plus
FLOW_GROUP = 2.0     # « groupe » : 2 ennemis par seconde au plus (un groupe de 5 qui passe en 2,5 s)
MISSED_EVERY = 3.0   # Carquois géant : une flèche ratée toutes les 3 s
KILLS_PER_SECOND = 0.4  # Grande bombarde : ennemis tués à 18 m par seconde (la poudre)
HITS_PER_SECOND = 1.0   # Colosse de forge : tes flèches qui touchent à 30 m de lui, par seconde (chacune, un marteau ; 12.48),
                        # des tirs « Bon » (l'action d'une flèche suit sa force depuis le 3 novembre : TowerUltimate.ShotPower)
SCROLLS_PER_WAVE = 0.5  # parchemins de force (et autant d'adresse) que le joueur gagne par vague, coffres et boutique


def fall(peak):
    """Dégâts de la chute (Enemy.Launch) en part des PV max : 5 % par mètre au-delà de 1,5 m, la moitié au plus."""
    return min(0.5, max(0.0, peak - 1.5) * 0.05)


def peak(up_speed, gravity=G):
    return up_speed * up_speed / (2 * gravity)


def flight(up_speed, gravity=G, start=0.0):
    """Durée d'un vol en cloche (vitesse vers le haut, en partant de <start> m au-dessus du sol)."""
    top = start + peak(up_speed, gravity)
    return up_speed / gravity + math.sqrt(2 * top / gravity)


def chord(radius, offset=5.0):
    """Longueur de voie dans un cercle de portée, la tour à <offset> m de la voie."""
    return 2 * math.sqrt(max(0.0, radius * radius - offset * offset))


# ------------------------------------------------------------------------------------------------ les lignes du tableau

class Row:
    """Un niveau d'un bâtiment. <dist> : la longueur de voie à portée (m) ; l'exposition (le temps qu'il agit sur un ennemi seul, sur un
    groupe, qui passent) en découle à 2,5 m/s, sauf si <expo1> ou <expoz> la donnent (un mur qui tient, une tour qui éjecte…)."""

    def __init__(self, level, plans, desc, reach, dist, d1=0.0, p1=0.0, dz=0.0, pz=0.0, cc=0.0, support=None, calc="", expo1=None,
                 expoz=None):
        self.level, self.plans, self.desc, self.reach = level, plans, desc, reach
        self.d1, self.p1, self.dz, self.pz, self.cc, self.support, self.calc = d1, p1, dz, pz, cc, support, calc
        self.expo1 = expo1 if expo1 is not None else dist / SPEED
        self.expoz = expoz if expoz is not None else dist / SPEED + (0.5 if dist < 10 else 0.0)


class Building:
    def __init__(self, name, family, origin, rows, proposal):
        self.name, self.family, self.origin, self.rows, self.proposal = name, family, origin, rows, proposal


TOWER_PLANS = [1, 2, 3, 4, 5]   # 1 plan par niveau (StructureDefinition, 2 novembre ; 2 aux niveaux 4 et 5 avant)
DUO_PLANS = [6, 8, 10]          # deux tours au niveau 3, puis 2 plans par niveau (2 novembre ; 4 avant)
TRIO_PLANS = [9, 12, 15]        # un duo au niveau 1 et une tour au niveau 3, puis 3 plans par niveau (2 novembre ; 4 avant)
ULTIMATE_PLANS = 25             # le trio au niveau 3, puis 10 plans (12.44)


def r1(x):
    return f"{x:.1f}".replace(".", ",")


def pct(x):
    return f"{x * 100:.1f} %".replace(".", ",")


# ------------------------------------------------------------------------------------------------ les 10 tours

def barricade():
    p = tower("Barricade", "StructureBarricade")
    hp = p["m_HealthPerLevel"]
    thorns = p["m_ThornsDamage"]
    pend = p["m_PendulumDamagePerLevel"]
    period = p["m_PendulumPeriod"]
    herse, herse_iv, herse_stun = p["m_PortcullisDamage"], p["m_PortcullisInterval"], p["m_PortcullisStun"]
    tdmg, tiv = p["m_TowerDamage"], p["m_TowerShotInterval"]
    attack = 1.7  # cadence moyenne d'attaque des ennemis (s)
    th1, thz = thorns / attack, GROUP * thorns / attack
    # Le pendule passe deux fois par période devant le mur ; un ennemi au plus une fois par passage ; 3 du groupe sur sa trajectoire.
    pass_time = period / 2
    wave_up = 7.0
    wave_fall = fall(peak(wave_up))
    hold1 = [min(60.0, h / WAVES_DATA.enemy_dps(5)) for h in hp]
    holdz = [min(60.0, h / (GROUP * WAVES_DATA.enemy_dps(5))) for h in hp]
    rows = [
        Row(1, 1, f"Un mur de {int(hp[0])} PV en travers de la voie : les ennemis s'arrêtent pour le frapper. Aucun dégât.",
            "Mur de 4 m", 0, cc=GROUP, expo1=hold1[0], expoz=holdz[0],
            calc=f"Contrôle : le groupe arrêté tant que le mur tient (5 ennemis × 1 s/s) : {r1(holdz[0])} s face au groupe à la vague 5."),
        Row(2, 2, f"Rempart de {int(hp[1])} PV ; les pointes : chaque coup reçu au corps à corps renvoie {r1(thorns)} de dégâts à l'attaquant.",
            "Mur de 4 m", 0, d1=th1, dz=thz, cc=GROUP, expo1=hold1[1], expoz=holdz[1],
            calc=f"Pointes : {r1(thorns)} par coup reçu, un coup toutes les {r1(attack)} s par attaquant (5 au contact)."),
        Row(3, 3, f"{int(hp[2])} PV ; le pendule devant le mur fauche les ennemis ({r1(pend[2])} de dégâts, projetés sur le côté).",
            "Mur + pendule", 0, d1=pend[2] / pass_time + th1, dz=3 * pend[2] / pass_time + thz, cc=GROUP, expo1=hold1[2], expoz=holdz[2],
            calc=f"Pendule : 2 passages par {r1(period)} s, 3 ennemis du groupe sur sa trajectoire ; + pointes."),
        Row(4, 5, f"{int(hp[3])} PV ; pendule à {r1(pend[3])} ; la herse s'abat toutes les {r1(herse_iv)} s ({r1(herse)} de dégâts, étourdis "
                  f"{r1(herse_stun)} s) ; ta flèche qui traverse le mur devient perforante.",
            "Mur + pendule", 0,
            d1=pend[3] / pass_time + th1 + herse / herse_iv, dz=3 * pend[3] / pass_time + thz + GROUP * herse / herse_iv, cc=GROUP,
            expo1=hold1[3], expoz=holdz[3],
            calc="Pendule + pointes + herse sur les 5 du contact. La flèche perforante (le joueur) n'est pas comptée."),
        Row(5, 7, f"Forteresse de {int(hp[4])} PV ; pendule à {r1(pend[4])} ; deux tours de guet tirent ({r1(tdmg)} toutes les {r1(tiv)} s "
                  f"chacune, {int(p['m_TowerRange'])} m) ; un coup reçu lâche une onde qui renvoie le groupe {int(p['m_WaveDistance'])} m vers la forêt.",
            "Mur + 26 m", chord(p["m_TowerRange"]),
            d1=0.5 * (pend[4] / pass_time + th1 + herse / herse_iv) + 2 * tdmg / tiv,
            p1=wave_fall / 6.0,
            dz=0.5 * (3 * pend[4] / pass_time + thz + GROUP * herse / herse_iv) + 2 * tdmg / tiv,
            pz=GROUP * wave_fall / 6.0, cc=GROUP, expo1=60.0, expoz=min(60.0, 2 * holdz[4]),
            calc=f"L'onde éloigne le groupe : pendule, pointes et herse ne servent que la moitié du temps ; elle revient toutes "
                 f"les 6 s (chute de {r1(peak(wave_up))} m : {pct(wave_fall)} des PV). Tours de guet : 2 × {r1(tdmg)} / {r1(tiv)} s. "
                 f"Exposition : le temps que le mur tient (doublé par l'onde)."),
    ]
    return Building("Barricade", "Tours", "", rows,
                    "")


def piques():
    p = tower("Pieges", "SpikeTrap")
    dmg, iv = p["m_DamagePerLevel"], p["m_IntervalPerLevel"]
    bleed = p["m_BleedDamage"] / 0.5
    mine = p["m_MineDamage"] / p["m_MineRearm"]
    mine_fall = fall(peak(6.0))
    jaw_fall = fall(peak(p["m_JawLaunch"]))
    jaw_iv = p["m_JawInterval"]
    size = p["m_Size"] if isinstance(p["m_Size"], float) else 2.6
    rows = []
    for level in range(1, 6):
        d, i = per_level(dmg, level), per_level(iv, level)
        on_plate = 4 if level < 3 else GROUP
        d1 = d / i + (bleed if level >= 2 else 0)
        dz = on_plate * d1
        p1 = pz = 0.0
        cc = GROUP * (1 - p["m_SlowMultiplier"]) if level >= 3 else 0.0
        extra = []
        if level >= 4:
            d1 += mine
            dz += GROUP * 0.75 * mine
            p1 += mine_fall / p["m_MineRearm"]
            pz += GROUP * mine_fall / p["m_MineRearm"]
            cc += GROUP * (flight(6.0) + LANDING_STUN) / p["m_MineRearm"]
            extra.append(f"mine {r1(p['m_MineDamage'])} toutes les {r1(p['m_MineRearm'])} s")
        if level >= 5:
            d1 += p["m_JawDamage"] / jaw_iv
            dz += GROUP * p["m_JawDamage"] / jaw_iv
            p1 += jaw_fall / jaw_iv
            pz += GROUP * jaw_fall / jaw_iv
            cc += GROUP * (flight(p["m_JawLaunch"]) + 1.5) / jaw_iv
            extra.append(f"mâchoire {r1(p['m_JawDamage'])} toutes les {r1(jaw_iv)} s (chute de {r1(peak(p['m_JawLaunch']))} m : {pct(jaw_fall)})")
        desc = {
            1: f"Une plaque de pointes sur la voie : {r1(d)} de dégâts toutes les {r1(i)} s à tous ceux qui sont dessus.",
            2: f"Pointes barbelées ({r1(d)} / {r1(i)} s) : les touchés saignent ({r1(p['m_BleedDamage'])} toutes les 0,5 s pendant {r1(p['m_BleedTime'])} s).",
            3: f"Champ de pieux de 5,2 m de large ({r1(d)} / {r1(i)} s, saignement) ; ceux qui sont dessus ralentissent de 20 %.",
            4: f"Pieux ({r1(d)} / {r1(i)} s) ; une mine au centre ({r1(p['m_MineDamage'])} dans {r1(p['m_MineRadius'])} m, projette ; se réarme en "
               f"{r1(p['m_MineRearm'])} s ; ta flèche la fait sauter).",
            5: f"Pieux ({r1(d)} / {r1(i)} s), mine ; la mâchoire : toutes les {r1(jaw_iv)} s, des pieux de 4 m sur {int(p['m_JawLength'])} m de voie "
               f"({r1(p['m_JawDamage'])} de dégâts, les survivants projetés et étourdis ; un boss perd 10 % de ses PV).",
        }[level]
        rows.append(Row(level, TOWER_PLANS[level - 1], desc, "Plaque 2,6 m" if level < 3 else "Plaque 5,2 × 2,6 m",
                        size, d1=d1, p1=p1, dz=dz, pz=pz, cc=cc,
                        expo1=size / SPEED / (p["m_SlowMultiplier"] if level >= 3 else 1),
                        expoz=size / SPEED / (p["m_SlowMultiplier"] if level >= 3 else 1) + 0.5,
                        calc=f"{r1(d)} / {r1(i)} s sur chacun ; {on_plate} du groupe sur la plaque"
                             + (f" ; saignement {r1(bleed)}/s" if level >= 2 else "")
                             + (" ; " + " ; ".join(extra) if extra else "") + ". Le saignement continue 3 s après la plaque."))
    return Building("Piques", "Tours", "", rows, "")


def tremplin():
    p = tower("Tremplin", "StructureTrampoline")
    up, cd = p["m_UpSpeedPerLevel"], p["m_CooldownPerLevel"]
    back = p["m_BackSpeed"]
    impact, throws = p["m_ImpactDamagePerLevel"], p["m_ThrowsPerLevel"]
    rows = []
    for level in (1, 2):
        u, c = per_level(up, level), per_level(cd, level)
        f = fall(peak(u))
        t = flight(u)
        lost = t + LANDING_STUN + back * t / SPEED
        rows.append(Row(level, TOWER_PLANS[level - 1],
                        f"L'ennemi qui marche sur la plaque est projeté en l'air ({r1(peak(u))} m) et un peu en arrière ; un toutes les {r1(c)} s."
                        if level == 1 else
                        f"Plus haut ({r1(peak(u))} m de chute : {pct(f)} de ses PV) ; un ennemi toutes les {r1(c)} s.",
                        "Plaque 2,4 m", 2.4, p1=f / c, pz=f / c, cc=lost / c, expo1=2 * max(c, lost), expoz=GROUP * c,
                        calc=f"Lancé à {r1(u)} m/s : chute de {r1(peak(u))} m ({pct(f)} des PV), vol {r1(t)} s, recule de {r1(back * t)} m ; "
                             f"un seul ennemi par saut, toutes les {r1(c)} s."))
    # La catapulte : vol de 1,3 s, le boulet monte de g t / 2 ; il retombe sur le groupe le plus serré à 16 m.
    t = p["m_ThrowFlightTime"]
    thrown_fall = fall(peak(0.5 * G * t))
    for level in (3, 4):
        c, dmg, n = per_level(cd, level), per_level(impact, level), per_level(throws, level)
        hits = 5 if n == 1 else 3
        self_dmg = dmg * p["m_SelfDamage"]
        rows.append(Row(level, TOWER_PLANS[level - 1],
                        f"La catapulte : le bras attrape l'ennemi le plus proche et le lance sur le groupe le plus serré à 16 m "
                        f"({r1(dmg)} de dégâts dans {r1(p['m_ImpactRadius'])} m, renversés ; le boulet en prend la moitié) ; toutes les {r1(c)} s."
                        if level == 3 else
                        f"Trois godets : trois ennemis lancés à la fois ({r1(dmg)} de dégâts à l'impact) ; toutes les {r1(c)} s.",
                        "3,5 m (prise), 16 m (jet)", 3.5, expo1=2 * c, expoz=GROUP / n * c,
                        d1=self_dmg / c, p1=thrown_fall / c,
                        dz=n * (self_dmg + hits * dmg) / c, pz=n * thrown_fall / c,
                        cc=n * (t + LANDING_STUN + 8 / SPEED + hits * (flight(3.0) + LANDING_STUN)) / c,
                        calc=f"1 cible : seul, il est jeté 8 m vers la forêt ({r1(self_dmg)} de dégâts, chute {pct(thrown_fall)}). Groupe : "
                             f"{n} jeté(s) sur le groupe, l'impact touche {hits} ennemis."))
    c5 = per_level(cd, 5)
    ramp_on = 4
    rows.append(Row(5, 7, f"La rampe de lancement : toutes les {r1(c5)} s, jusqu'à {int(p['m_RampMax'])} ennemis sur la rampe montent à "
                          f"{int(p['m_RampHeight'])} m et retombent sur un groupe : l'atterrissage les tue ({r1(impact[4])} de dégâts autour) ; "
                          f"un boss perd 10 % de ses PV.",
                    "Rampe 3,4 m", 3.4, expo1=1 / FLOW_ONE, expoz=GROUP / min(ramp_on / c5, FLOW_GROUP),
                    p1=min(1 / c5, FLOW_ONE), dz=min(ramp_on / c5, FLOW_GROUP) * 3 * impact[4], pz=min(ramp_on / c5, FLOW_GROUP),
                    cc=0.0,
                    calc=f"Chaque lancé meurt (100 % de ses PV). 1 cible : un toutes les {r1(c5)} s (débit 0,5/s au plus). Groupe : "
                         f"{ramp_on} sur la rampe, débit plafonné à {r1(FLOW_GROUP)}/s ; chacun retombe sur 3 ennemis ({r1(impact[4])})."))
    return Building("Tremplin", "Tours", "", rows, "")


def baliste():
    p = tower("Baliste", "Ballista")
    dmg, iv, pierce = p["m_DamagePerLevel"], p["m_IntervalPerLevel"], p["m_PiercePerLevel"]
    rng = p["m_Range"]
    titan = per_level(dmg, 5) * p["m_TitanDamageScale"]
    rows = []
    for level in range(1, 6):
        d, i = per_level(dmg, level), per_level(iv, level)
        hits = 2 if per_level(pierce, level) > 0 else 1   # un carreau perforant touche 2 ennemis du groupe
        bolts = 2 if level >= 3 else 1
        d1 = d / i
        dz = bolts * hits * d / i
        cc = 0.0
        calc = f"{r1(d)} / {r1(i)} s ; groupe : {bolts} carreau(x) × {hits} ennemi(s) touché(s)"
        if level >= 4:
            cc = min(GROUP, bolts * hits / i * p["m_HarpoonPin"])
            calc += f" ; harpon : cloué {r1(p['m_HarpoonPin'])} s par coup"
        if level >= 5:
            d1 += titan / p["m_TitanInterval"]
            dz += 3 * titan / p["m_TitanInterval"]
            calc += f" ; titan : {r1(titan)} toutes les {r1(p['m_TitanInterval'])} s, traverse 3 ennemis du groupe"
        desc = {
            1: f"Une baliste tire un carreau sur l'ennemi le plus proche ({r1(d)} de dégâts toutes les {r1(i)} s, {int(rng)} m) ; volants compris.",
            2: f"Carreaux perforants : chacun traverse jusqu'à 3 ennemis ({r1(d)} / {r1(i)} s).",
            3: f"Le scorpion : un deuxième carreau sur la deuxième cible la plus proche ({r1(d)} / {r1(i)} s).",
            4: f"Le harpon ({r1(d)} / {r1(i)} s) : chaque carreau cloue l'ennemi {r1(p['m_HarpoonPin'])} s au sol ; un volant tombe "
               f"{r1(p['m_HarpoonFlyerHold'])} s.",
            5: f"Le titan : en plus ({r1(d)} / {r1(i)} s), toutes les {r1(p['m_TitanInterval'])} s un pieu géant ({r1(titan)} de dégâts) "
               f"traverse tout et emporte ceux qu'il touche.",
        }[level]
        rows.append(Row(level, TOWER_PLANS[level - 1], desc, f"{int(rng)} m", chord(rng), d1=d1, dz=dz, cc=cc,
                        calc=calc + ". La moitié des bonus de l'arc n'est pas comptée."))
    return Building("Baliste", "Tours", "", rows, "")


def mortier():
    p = tower("Mortier", "StructureMortar")
    dmg, iv, radius, shells, rng = (p["m_DamagePerLevel"], p["m_IntervalPerLevel"], p["m_RadiusPerLevel"], p["m_ShellsPerLevel"],
                                    p["m_RangePerLevel"])
    edge = p["m_EdgeDamage"]
    rows = []
    for level in range(1, 6):
        d, i, rad, n = per_level(dmg, level), per_level(iv, level), per_level(radius, level), per_level(shells, level)
        falloff = 1 - (1 - edge) * 1.0 / rad   # un ennemi du groupe à 1 m du point d'impact en moyenne
        shell_group = GROUP * falloff * d
        frags = p["m_Fragments"] * p["m_FragmentShare"] * d * 0.3 if level >= 2 else 0.0
        d1 = d * (1 + (0.8 if n > 1 else 0)) / i
        dz = n * (shell_group + frags) / i
        p1 = pz = cc = 0.0
        calc = f"{n} obus de {r1(d)} / {r1(i)} s ; groupe : 5 ennemis à {r1(falloff * 100)} % (à 1 m du centre)"
        if level >= 2:
            calc += f" ; éclats : 4 × {pct(p['m_FragmentShare'])}, 0,3 ennemi chacun"
        if n > 1:
            calc += " ; 1 cible : le deuxième obus à 80 %"
        if level >= 4:
            cc = GROUP * (1 - p["m_TarSpeed"]) * 0.8
            calc += f" ; goudron : ralentis de {pct(1 - p['m_TarSpeed'])}, 80 % du temps"
        if level >= 5:
            barrage = 2 * d * 0.7 + p["m_BarrageBurn"] * 3
            bfall = fall(peak(7.5))
            d1 += barrage / p["m_BarrageInterval"]
            dz += GROUP * barrage / p["m_BarrageInterval"]
            p1 += bfall / p["m_BarrageInterval"]
            pz += GROUP * bfall / p["m_BarrageInterval"]
            cc += GROUP * (flight(7.5) + LANDING_STUN) / p["m_BarrageInterval"]
            calc += f" ; bombardement : 2 obus à 70 % et 3 s de feu par ennemi, toutes les {r1(p['m_BarrageInterval'])} s, chute {pct(bfall)}"
        desc = {
            1: f"Un obus en cloche sur le groupe le plus serré ({r1(d)} de dégâts dans {r1(rad)} m, moitié au bord ; toutes les {r1(i)} s, "
               f"{int(per_level(rng, level))} m) ; pas les volants.",
            2: f"Fragmentation : l'obus ({r1(d)}, {r1(rad)} m, toutes les {r1(i)} s) lâche 4 éclats autour ({pct(p['m_FragmentShare'])} des dégâts).",
            3: f"Deux tubes : deux obus à la fois ({r1(d)}, {r1(rad)} m, toutes les {r1(i)} s).",
            4: f"Goudron : deux obus ({r1(d)} / {r1(i)} s) qui laissent une flaque de {r1(p['m_TarLife'])} s : ralentis de moitié ; "
               f"une flèche l'embrase.",
            5: f"Bombardement : en plus ({r1(d)} / {r1(i)} s), toutes les {r1(p['m_BarrageInterval'])} s, {int(p['m_BarrageShells'])} obus sur "
               f"{int(p['m_BarrageLength'])} m de voie qui projettent les ennemis et laissent le sol en feu.",
        }[level]
        rows.append(Row(level, TOWER_PLANS[level - 1], desc, f"{int(per_level(rng, level))} m", chord(per_level(rng, level)),
                        d1=d1, p1=p1, dz=dz, pz=pz, cc=cc, calc=calc + "."))
    return Building("Mortier", "Tours", "", rows, "")


def givre():
    p = tower("Givre", "FrostTower")
    radius, speed = p["m_RadiusPerLevel"], p["m_SpeedPerLevel"]
    freeze_iv, freeze_t, brk = p["m_FreezeInterval"], p["m_FreezeTime"], p["m_FreezeBreakShare"]
    storm_iv, storm_t = p["m_StormInterval"], p["m_StormFreeze"]
    rows = []
    for level in range(1, 6):
        rad, s = per_level(radius, level), per_level(speed, level)
        slow = 1 - s
        cc = GROUP * slow
        p1 = pz = 0.0
        calc = f"Ralentis de {pct(slow)} dans {int(rad)} m : 5 × {r1(slow)}"
        if level >= 3:
            p1 += brk / freeze_iv
            pz += brk / freeze_iv
            cc += (1 - slow) * freeze_t / freeze_iv
            calc += f" ; pic de glace : {pct(brk)} des PV toutes les {r1(freeze_iv)} s si un autre coup brise la glace"
        if level >= 5:
            p1 += 1 / storm_iv
            pz += GROUP / storm_iv
            cc += GROUP * (1 - slow) * storm_t / storm_iv
            calc += f" ; cœur de l'hiver : tous gelés {r1(storm_t)} s toutes les {r1(storm_iv)} s, le moindre coup les tue (compté : 100 %)"
        if level >= 4:
            slip_speed = p.get("m_SlipSpeed", 2.0)
            slip_t = flight(3.5)
            slip_loss = (slip_t + LANDING_STUN) * s + slip_speed * slip_t / SPEED
            cc += GROUP * p["m_SlipChance"] / p["m_SlipInterval"] * slip_loss
            calc += (f" ; verglas : un sur quatre par seconde glisse de {r1(slip_speed * slip_t)} m vers la forêt et tombe "
                     f"({r1(slip_loss)} s perdues)")
        desc = {
            1: f"Une aura de froid : les ennemis à {int(rad)} m marchent {pct(slow)} moins vite. Aucun dégât.",
            2: f"{int(rad)} m, ralentis de {pct(slow)} ; le givre reste {r1(p['m_LingerTime'])} s après la sortie de l'aura.",
            3: f"{int(rad)} m, ralentis de moitié ; pic de glace : toutes les {r1(freeze_iv)} s, l'ennemi le plus solide est gelé {r1(freeze_t)} s ; "
               f"le premier coup qui le touche lui coûte {pct(brk)} de ses PV max.",
            4: f"Verglas : chaque seconde, un ennemi sur quatre glisse vers la forêt et tombe (en plus du pic de glace).",
            5: f"Cœur de l'hiver : toutes les {r1(storm_iv)} s, tous les ennemis au sol de l'aura gèlent {r1(storm_t)} s, boss compris, et le moindre "
               f"coup les tue (un boss perd 10 %).",
        }[level]
        rows.append(Row(level, TOWER_PLANS[level - 1], desc, f"{int(rad)} m", chord(rad), p1=p1, pz=pz, cc=cc, calc=calc + "."))
    return Building("Tour de givre", "Tours", "", rows, "")


def filet():
    p = tower("Filet", "StructureNetLauncher")
    iv, hold, nets = p["m_IntervalPerLevel"], p["m_HoldPerLevel"], p["m_NetsPerLevel"]
    rng = p["m_Range"]
    net_fall = fall(FLYER_HEIGHT)
    rows = []
    for level in range(1, 6):
        i, h, n = per_level(iv, level), per_level(hold, level), per_level(nets, level)
        if level <= 3:
            desc = {1: f"Un filet sur le volant le plus proche ({int(rng)} m), toutes les {r1(i)} s : il tombe et reste au sol {r1(h)} s. "
                       f"Contre les volants seulement.",
                    2: f"Le volant reste {r1(h)} s au sol ; un filet toutes les {r1(i)} s. Contre les volants seulement.",
                    3: f"Deux filets à la fois, toutes les {r1(i)} s. Contre les volants seulement."}[level]
            rows.append(Row(level, TOWER_PLANS[level - 1], desc, f"{int(rng)} m (volants)", chord(rng),
                            p1=net_fall / i, pz=n * net_fall / i, cc=n * h / i,
                            calc=f"CONTRE LES VOLANTS : chute de {r1(FLYER_HEIGHT)} m ({pct(net_fall)} des PV) par filet ; {n} filet(s) / {r1(i)} s. "
                                 f"Rien contre les ennemis au sol."))
            continue
        shock, stun = p["m_ShockDamage"], p["m_ShockStun"]
        desc = (f"Filet électrique : {n} filets toutes les {r1(i)} s ; sans volant, il vise un ennemi au sol : {r1(shock)} de dégâts, étourdi {r1(stun)} s."
                if level == 4 else
                f"Toile géante : en plus, toutes les {r1(p['m_WebInterval'])} s s'il vise un volant, une toile de {int(p['m_WebRadius'])} m au-dessus "
                f"de lui pendant {r1(p['m_WebDuration'])} s : tout volant dessous tombe.")
        rows.append(Row(level, TOWER_PLANS[level - 1], desc, f"{int(rng)} m", chord(rng),
                        d1=shock / i, dz=n * shock / i, cc=n * stun / i,
                        calc=f"Au sol : {n} filet(s) de {r1(shock)} / {r1(i)} s, étourdis {r1(stun)} s (un volant : en plus {pct(net_fall)} de chute)."))
    return Building("Lance-filet", "Tours", "", rows, "")


def aimant():
    p = tower("Aimant", "StructureMagnet")
    rng, pull, iv = p["m_RangePerLevel"], p["m_PullPerLevel"], p["m_IntervalPerLevel"]
    cont = p["m_ContinuousPull"]
    sing_iv = p["m_SingularityInterval"]
    thrown_peak = 3 + peak(p["m_SingularityThrow"])
    sing_fall = fall(thrown_peak)
    rows = []
    for level in range(1, 6):
        r = per_level(rng, level)
        if level <= 2:
            d, i = per_level(pull, level), per_level(iv, level)
            loss = 0.7 * d / SPEED / i      # tiré de côté : il refait 70 % de la distance
            cc = GROUP * loss
            calc = f"Tiré de {r1(d)} m toutes les {r1(i)} s, hors de son chemin : 70 % de la distance à refaire, à {r1(SPEED)} m/s"
            desc = (f"Toutes les {r1(i)} s, une impulsion attire les ennemis à {int(r)} m de {r1(d)} m vers l'aimant. Aucun dégât."
                    if level == 1 else
                    f"Impulsion de {r1(d)} m toutes les {r1(i)} s ({int(r)} m) ; les volants à portée tombent {r1(p['m_FlyerHold'])} s.")
        else:
            loss = 0.7 * cont / SPEED
            cc = GROUP * loss
            calc = f"Électro-aimant : tiré en continu à {r1(cont)} m/s, 70 % compté contre sa marche"
            desc = {3: f"Électro-aimant : il attire en continu ({r1(cont)} m/s, {int(r)} m) ; les volants tombent.",
                    4: f"Les ennemis attirés ralentissent de {pct(1 - p.get('m_SlowMultiplier', 0.75))} ; tes flèches qui passent "
                       f"dans sa zone ({int(r)} m) se courbent vers les ennemis devant elles.",
                    5: f"Singularité : toutes les {r1(sing_iv)} s, jusqu'à {int(p['m_SingularityMax'])} ennemis à {int(p['m_SingularityRange'])} m "
                       f"aspirés dans une boule qui tournoie {r1(p['m_SingularitySpin'])} s, puis recrachés en l'air dans tous les sens."}[level]
        p1 = pz = 0.0
        if level >= p.get("m_SlowFromLevel", 4):
            slow = 1 - p.get("m_SlowMultiplier", 0.75)
            cc += GROUP * slow
            calc += f" ; ralentis de {pct(slow)} dans sa zone"
        if level >= 5:
            lost = 0.8 + p["m_SingularitySpin"] + flight(p["m_SingularityThrow"], start=3) + LANDING_STUN
            p1 = sing_fall / sing_iv
            pz = GROUP * sing_fall / sing_iv
            cc += GROUP * lost / sing_iv
            calc += f" ; singularité : {r1(lost)} s perdues et une chute de {r1(thrown_peak)} m ({pct(sing_fall)}) toutes les {r1(sing_iv)} s"
        if level == 4:
            calc += " ; les flèches courbées (le joueur) ne sont pas comptées"
        rows.append(Row(level, TOWER_PLANS[level - 1], desc, f"{int(r)} m", chord(r), p1=p1, pz=pz, cc=cc, calc=calc + "."))
    return Building("Tour-aimant", "Tours", "", rows, "")


def bulle():
    p = tower("Bulle", "StructureBubble")
    rng, iv, height, dur, count = (p["m_RangePerLevel"], p["m_IntervalPerLevel"], p["m_HeightPerLevel"], p["m_DurationPerLevel"],
                                   p["m_BubblesPerLevel"])
    rows = []
    for level in range(1, 6):
        r, i, h, d, n = (per_level(rng, level), per_level(iv, level), per_level(height, level), per_level(dur, level),
                         per_level(count, level))
        f = fall(h)
        cycle = d + math.sqrt(2 * h / G) + LANDING_STUN       # un même ennemi ne peut pas être repris avant de retomber
        per_bubble = p["m_GroupMax"] if level >= 3 else 1
        caught_rate = min(n * per_bubble / i, GROUP / cycle)
        p1 = f / max(i, cycle)
        pz = caught_rate * f
        cc = caught_rate * cycle
        calc = (f"Enfermé {r1(d)} s, chute de {r1(h)} m ({pct(f)}) ; {n} bulle(s) de {per_bubble} / {r1(i)} s ; un ennemi repris au plus "
                f"toutes les {r1(cycle)} s")
        if level >= 5:
            p1 += 1 / p["m_OrbInterval"]
            pz += GROUP / p["m_OrbInterval"]
            calc += f" ; orbe céleste : le groupe meurt à l'atterrissage, toutes les {r1(p['m_OrbInterval'])} s"
        if level == 4:
            calc += " ; le rebond de ta flèche (le joueur) n'est pas compté"
        desc = {
            1: f"Toutes les {r1(i)} s, une bulle enferme un ennemi et l'emporte à {r1(h)} m ; crevée (ta flèche) ou au bout de {r1(d)} s, il retombe.",
            2: f"Deux bulles à la fois ({r1(h)} m, {r1(d)} s, toutes les {r1(i)} s).",
            3: f"Bulles géantes : chacune prend jusqu'à {int(p['m_GroupMax'])} ennemis à {r1(p['m_GroupRadius'])} m ({r1(h)} m, {r1(d)} s).",
            4: f"Rebond : ta flèche qui crève une bulle fait ses dégâts à la bulle la plus proche (20 m), de bulle en bulle.",
            5: f"Orbe céleste : toutes les {r1(p['m_OrbInterval'])} s, jusqu'à {int(p['m_OrbMax'])} ennemis à {r1(p['m_OrbRadius'])} m montent à "
               f"{int(p['m_OrbHeight'])} m ; crevée ou au bout de {r1(p['m_OrbDuration'])} s, ils meurent à l'atterrissage.",
        }[level]
        rows.append(Row(level, TOWER_PLANS[level - 1], desc, f"{int(r)} m", chord(r), p1=p1, pz=pz, cc=cc, calc=calc + "."))
    return Building("Bulle", "Tours", "", rows, "")


def phare():
    p = tower("Phare", "StructureLighthouse")
    radius, sdmg, srate = p["m_RadiusPerLevel"], p["m_SupportDamagePerLevel"], p["m_SupportRatePerLevel"]
    boost_rate, boost_time = p["m_JudgmentBoost"] if isinstance(p["m_JudgmentBoost"], list) else (2.0, 5.0)
    rows = []
    for level in range(1, 6):
        r = per_level(radius, level)
        support = per_level(sdmg, level) * per_level(srate, level)
        d1 = 0.0
        calc = f"Soutien : dégâts ×{r1(per_level(sdmg, level))}, cadence ×{r1(per_level(srate, level))}"
        if level >= 5:
            support *= 1 + (boost_rate - 1) * boost_time / p["m_JudgmentInterval"]
            d1 = p["m_JudgmentDamage"] / p["m_JudgmentInterval"]
            calc += (f", et le jugement double la cadence {r1(boost_time)} s sur {r1(p['m_JudgmentInterval'])} ; jugement : "
                     f"{r1(p['m_JudgmentDamage'])} toutes les {r1(p['m_JudgmentInterval'])} s (ou {pct(p['m_JudgmentShare'])} des PV max s'il fait "
                     f"plus : compté sans, c'est moins jusqu'à la vague 10 environ)")
        desc = {
            1: f"Le soutien : les bâtiments à {int(r)} m font {pct(sdmg[0] - 1)} de dégâts en plus ; ta flèche marque les points faibles.",
            2: f"Soutien : +{pct(per_level(sdmg, level) - 1)} de dégâts et +{pct(per_level(srate, level) - 1)} de cadence ({int(r)} m).",
            3: f"Grand phare : {int(r)} m, +{pct(per_level(sdmg, level) - 1)} de dégâts, +{pct(per_level(srate, level) - 1)} de cadence ; faisceau plus large.",
            4: f"Soutien : +{pct(per_level(sdmg, level) - 1)} de dégâts, +{pct(per_level(srate, level) - 1)} de cadence ; un anneau de feu "
               f"de {r1(2 * p['m_RingRadius'])} m au milieu de la voie : ta flèche qui le traverse s'enflamme (sa cible brûle : 40 % des "
               f"dégâts en 3 s).",
            5: f"Jugement : toutes les {r1(p['m_JudgmentInterval'])} s, une colonne de lumière sur l'ennemi qui a le plus de PV "
               f"({r1(p['m_JudgmentDamage'])} ou la moitié de ses PV max) ; les bâtiments du rayon tirent deux fois plus vite {r1(boost_time)} s.",
        }[level]
        rows.append(Row(level, TOWER_PLANS[level - 1], desc, f"{int(r)} m", chord(r), d1=d1, dz=d1, support=support,
                        calc=calc + f". Soutien total : ×{r1(support)} sur le DPS des tours du rayon (non compté dans leurs lignes)."))
    return Building("Phare", "Tours", "", rows, "")


# ------------------------------------------------------------------------------------------------ les 13 duos

def boulet_fou():
    p = duo("Duo_Boulet_fou", "DuoBouletFou")
    turn, hit = p["m_TurnTimePerLevel"], p["m_HitDamagePerLevel"]
    rows = []
    for level in (1, 2, 3):
        balls = 2 if level == 3 else 1
        passes = balls / per_level(turn, level)
        d = per_level(hit, level)
        if level < 3:
            flight_t = 2.3 if level == 1 else 2.8
            f = fall(peak(0.5 * G * flight_t))
            back = per_level(p["m_BackAlongPerLevel"], level)
            loss = flight_t + LANDING_STUN + back / SPEED + 1.5
            splash = 0.0
        else:
            f = p["m_WallSmashShare"]
            loss = 10.0
            splash = p["m_SmashDamage"]
        rate1, ratez = min(passes, FLOW_ONE), min(2 * passes, FLOW_GROUP)
        desc = {
            1: f"Une roue géante tourne en travers de la voie (un tour en {r1(turn[0])} s) : le boulet frappe ({r1(d)}) et jette l'ennemi sur une "
               f"autre voie, jusqu'à 50 m, 10 m en arrière.",
            2: f"Plus vite ({r1(turn[1])} s), {r1(d)} de dégâts, jeté jusqu'à 65 m et 18 m en arrière ; il renverse ceux sur qui il retombe.",
            3: f"Le boulet de canon : deux boulets ({r1(d)}) ; l'ennemi part à plat à 38 m/s jusqu'au décor d'en face : le choc lui coûte "
               f"{pct(f)} de ses PV et blesse ceux qui sont à 3 m.",
        }[level]
        rows.append(Row(level, DUO_PLANS[level - 1], desc, "Roue sur la voie", 2.0,
                        d1=rate1 * d, p1=rate1 * f, dz=ratez * (d + splash), pz=ratez * f, cc=ratez * loss,
                        expo1=1 / rate1, expoz=GROUP / ratez,
                        calc=f"{balls} passage(s) par tour, 2 ennemis du groupe par passage ; débit plafonné ({r1(FLOW_ONE)} et {r1(FLOW_GROUP)}/s) ; "
                             f"par ennemi frappé : {r1(d)} + {pct(f)} des PV ({'chute' if level < 3 else 'le choc'}), {r1(loss)} s de chemin perdues."))
    return Building("Boulet fou", "Duos", "Barricade + Tremplin", rows, "")


def marteau_pilon():
    p = duo("Duo_Marteau_pilon", "DuoMarteauPilon")
    iv, dmg, plant = p["m_IntervalPerLevel"], p["m_DamagePerLevel"], p["m_PlantTimePerLevel"]
    pop = fall(peak(p["m_PopSpeed"]) - 0.5)
    rows = []
    for level in (1, 2):
        i, d, t = per_level(iv, level), per_level(dmg, level), per_level(plant, level)
        cycle1 = max(i, t + 1.2)
        wave = 3 * p["m_WaveDamage"] if level >= 2 else 0.0
        rows.append(Row(level, DUO_PLANS[level - 1],
                        f"Toutes les {r1(i)} s, le marteau enfonce l'ennemi dans le sol jusqu'au cou ({r1(d)} de dégâts) : planté {r1(t)} s, "
                        f"il en ressort comme un bouchon." if level == 1 else
                        f"Toutes les {r1(i)} s ({r1(d)}, planté {r1(t)} s) ; l'onde du coup blesse ({r1(p['m_WaveDamage'])}) et renverse tout à "
                        f"{r1(p['m_WaveRadius'])} m.",
                        "Portique sur la voie", 4.8,
                        d1=d / cycle1, p1=pop / cycle1, dz=(d + d / 2 + wave) / i, pz=pop / i,
                        cc=(t + 1.2) / i + (3 * 1.5 / i if level >= 2 else 0),
                        calc=f"Seul, il est repris toutes les {r1(cycle1)} s (planté, il est intouchable) ; groupe : la cible, un voisin à moitié"
                             + (f", l'onde sur 3 autres" if level >= 2 else "") + f" ; sortie du sol : {pct(pop)} de chute."))
    i3, d3, t3 = per_level(iv, 3), per_level(dmg, 3), per_level(plant, 3)
    great = fall(peak(p["m_GreatLift"]))
    cycle = 4 * i3
    plants = GROUP / (t3 + 1.2)
    rows.append(Row(3, DUO_PLANS[2],
                    f"La forge : trois marteaux en cadence ({r1(d3)}, toutes les {r1(i3)} s), puis le grand coup : tout ce qui est à "
                    f"{int(p['m_GreatRadius'])} m part en l'air ({r1(p['m_GreatDamage'])} de dégâts).",
                    f"{int(p['m_GreatRadius'])} m", 2 * p["m_GreatRadius"],
                    d1=d3 / (t3 + 1.2) + 0.2 * p["m_GreatDamage"] / cycle, p1=pop / (t3 + 1.2) + 0.2 * great / cycle,
                    dz=plants * d3 + 6 / cycle * p["m_WaveDamage"] * 1.5 + 1.5 * p["m_GreatDamage"] / cycle,
                    pz=plants * pop + 1.5 * great / cycle, cc=GROUP + 1.5 * 4 / cycle,
                    calc=f"Un ennemi planté {r1(t3)} s ne peut pas être refrappé : au plus {r1(plants)} plantés par seconde sur le groupe ; "
                         f"6 coups par cycle de {r1(cycle)} s (ondes sur 1,5 ennemi libre) ; grand coup : chute de {r1(peak(p['m_GreatLift']))} m "
                         f"({pct(great)}) sur 1,5 ennemi libre."))
    return Building("Marteau-pilon", "Duos", "Barricade + Piques", rows, "")


def toboggan():
    p = duo("Duo_Toboggan", "DuoToboggan")
    lift, fwd, grav = p["m_LaunchLiftPerLevel"], p["m_LaunchForwardPerLevel"], p["m_LaunchGravityPerLevel"]
    deck_end = p["m_ChuteEndHeight"]
    ride = p["m_DeckHeight"] / p["m_ClimbSpeed"] + 1.0 + 0.8
    riders = p.get("m_MaxRidersPerLevel", [p.get("m_MaxRiders", 3)] * 2 + [5])
    rows = []
    for level in (1, 2, 3):
        throughput = min(per_level(riders, level) / (ride + 0.5), FLOW_GROUP)
        u = per_level(lift, level) + 1.0
        g = G * per_level(grav, level)
        top = deck_end + peak(u, g)
        t = u / g + math.sqrt(2 * top / g)
        back = per_level(fwd, level) * t + 4.2
        if level < 3:
            f = fall(top)
            loss = ride + t + p["m_DizzyTime"] + back / SPEED
            strike = 2 * p["m_StrikeDamage"] if level == 2 else 0.0
        else:
            f, loss, strike = 1.0, ride, 0.0
        desc = {
            1: f"Un grand toboggan qui enjambe la voie : l'ennemi grimpe, glisse sur une piste de glace et décolle du tremplin vers la forêt ; "
               f"il retombe sonné {r1(p['m_DizzyTime'])} s. Une glissade par ennemi, 3 à la fois.",
            2: f"Il saute plus haut ({r1(top)} m) et plus loin, et renverse ceux sur qui il retombe ({r1(p['m_StrikeDamage'])} de dégâts).",
            3: f"Le saut à ski : un grand tremplin les envoie à plus de 20 m de haut, loin vers la forêt ; l'atterrissage les tue. "
               f"{per_level(riders, 3)} à la fois sur le toboggan.",
        }[level]
        rows.append(Row(level, DUO_PLANS[level - 1], desc, "Sur la voie", 4.0,
                        p1=FLOW_ONE * f, dz=throughput * strike, pz=throughput * f, cc=throughput * loss,
                        expo1=1 / FLOW_ONE, expoz=GROUP / throughput,
                        calc=f"Montée et glissade : {r1(ride)} s, {per_level(riders, level)} à la fois : {r1(throughput)} ennemis/s ; saut de {r1(top)} m, {r1(t)} s de vol, "
                             f"{r1(back)} m vers la forêt ; " + (f"chute {pct(f)}" if level < 3 else "tué (100 %)")
                             + ". Une seule fois par ennemi : « 1 cible » suppose un ennemi neuf toutes les 2 s."))
    return Building("Toboggan", "Duos", "Tremplin + Tour de givre", rows, "")


def yoyo():
    p = duo("Duo_Yoyo", "DuoYoyo")
    slam = p["m_SlamDamagePerLevel"]
    splash = p["m_SlamSplash"]
    slams = int(p["m_Slams"])
    cycle = 0.9 + slams * 0.56 + p["m_Cooldown"]
    drop = fall(p["m_HoistHeight"] + 0.5)
    rows = []
    for level in (1, 2):
        d = per_level(slam, level)
        extra = 3 * splash if level == 2 else 0.0
        rows.append(Row(level, DUO_PLANS[level - 1],
                        f"Une grue : son yo-yo aimanté attrape un ennemi, le hisse à {r1(p['m_HoistHeight'])} m, le fait claquer au sol {slams} fois "
                        f"({r1(d)} chaque fois, {r1(splash)} autour) et le lâche de tout en haut." if level == 1 else
                        f"Il le lance sur le groupe le plus serré à {int(p['m_ThrowRange'])} m, qu'il renverse ({r1(d)} par claquement).",
                        f"{r1(per_level(p['m_GrabRadiusPerLevel'], level))} m (prise)", 2 * per_level(p["m_GrabRadiusPerLevel"], level),
                        d1=slams * d / cycle, p1=drop / cycle, dz=(slams * d + slams * 2 * splash + extra) / cycle, pz=drop / cycle,
                        cc=(2.5 + 1.6) / cycle + 6 * 1.25 / cycle + (3 * 1.25 / cycle if level == 2 else 0),
                        calc=f"Un cycle de {r1(cycle)} s : {slams} claquements, 2 voisins éclaboussés à chacun ; chute de {r1(p['m_HoistHeight'] + 0.5)} m "
                             f"({pct(drop)})."))
    d3 = per_level(slam, 3)
    cycle3 = cycle + p["m_FlingWindup"] + 0.6
    smash = p["m_FlingSmashShare"]
    rows.append(Row(3, DUO_PLANS[2],
                    f"Le lustre : sept yo-yo sur un anneau doré qui tourne ; sept ennemis montent et claquent ({r1(d3)}), puis le lustre s'emballe "
                    f"et les jette au loin, où ils s'écrasent ({pct(smash)} de leurs PV).",
                    f"{r1(per_level(p['m_GrabRadiusPerLevel'], 3))} m (prise)", 14.0,
                    d1=slams * d3 / cycle3, p1=smash / cycle3, dz=GROUP * slams * d3 / cycle3, pz=GROUP * smash / cycle3,
                    cc=GROUP * (4 + 1.5 + 20 / SPEED) / cycle3,
                    calc=f"Le groupe entier pris, cycle de {r1(cycle3)} s : {slams} claquements chacun, puis le choc ({pct(smash)}), jetés à 20 m."))
    return Building("Yo-yo", "Duos", "Tremplin + Tour-aimant", rows, "")


def avalanche():
    p = duo("Duo_Avalanche", "DuoAvalanche")
    iv, roll, speed, burst = p["m_IntervalPerLevel"], p["m_RollDistancePerLevel"], p["m_RollSpeedPerLevel"], p["m_BurstDamagePerLevel"]
    f = fall(peak(9.0))
    rows = []
    for level in (1, 2, 3):
        i, dist, sp, d = per_level(iv, level), per_level(roll, level), per_level(speed, level), per_level(burst, level)
        carried = min(dist / 2, 50.0)
        share = p["m_GiantShare"] if level == 3 else 0.0
        loss = carried / SPEED + carried / sp + flight(9.0) + LANDING_STUN
        desc = {
            1: f"Toutes les {r1(i)} s, une boule de neige roule vers la forêt ({int(dist)} m) et avale ceux qu'elle touche ; au bout, ou sous ta flèche "
               f"(+50 %), elle éclate ({r1(d)} de dégâts) et ils volent.",
            2: f"Elle roule plus loin ({int(dist)} m), plus grosse, avale jusqu'à 10 ennemis ({r1(d)} de dégâts), toutes les {r1(i)} s.",
            3: f"L'avalanche : toutes les {r1(i)} s, une boule géante dévale toute la voie jusqu'à la forêt, avale tout et éclate : "
               f"{r1(d)} de dégâts et {pct(share)} de leurs PV.",
        }[level]
        rows.append(Row(level, DUO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m (tir), voie", dist,
                        d1=d / i, p1=(f + share) / i, dz=GROUP * d / i, pz=GROUP * (f + share) / i, cc=GROUP * loss / i,
                        expo1=i, expoz=i,
                        calc=f"Tout le groupe avalé ; emporté {r1(carried)} m en arrière en moyenne ({r1(loss)} s perdues) ; éclat : {r1(d)} + chute "
                             f"{pct(f)}" + (f" + {pct(share)} des PV" if share else "") + ". La flèche qui crève (+50 %) n'est pas comptée."))
    return Building("Avalanche", "Duos", "Mortier + Tour de givre", rows, "")


def feu_artifice():
    p = duo("Duo_Feu_d_artifice", "DuoFeuArtifice")
    iv, dazzle, burn = p["m_IntervalPerLevel"], p["m_DazzleTimePerLevel"], p["m_BurnDamagePerLevel"]
    rows = []
    for level in (1, 2, 3):
        i, t, d = per_level(iv, level), per_level(dazzle, level), per_level(burn, level)
        sides = 2 * d * 1 if level >= 2 else 0.0
        finale1 = d / p["m_FinaleInterval"] if level >= 3 else 0.0
        finalez = GROUP * d / p["m_FinaleInterval"] if level >= 3 else 0.0
        desc = {
            1: f"Toutes les {r1(i)} s, une fusée éclate au-dessus du groupe le plus serré : éblouis {r1(t)} s (étourdis), sous une pluie "
               f"d'étincelles qui brûle ({r1(d)}) ; ta flèche qui traverse le bouquet s'enflamme.",
            2: f"Fusées à deux étages : deux bouquets de plus à côté ({r1(d)}, éblouis {r1(t)} s, toutes les {r1(i)} s).",
            3: f"Le bouquet final : toutes les {r1(p['m_FinaleInterval'])} s, {int(p['m_FinaleRockets'])} fusées sur tous les ennemis à portée "
               f"({r1(d)}, éblouis {r1(t)} s).",
        }[level]
        rows.append(Row(level, DUO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]),
                        d1=d / i + finale1, dz=(GROUP * d + sides) / i + finalez, cc=min(GROUP, GROUP * t / i),
                        calc=f"Le bouquet ({r1(p['m_DazzleRadius'])} m) couvre le groupe : {r1(d)} chacun / {r1(i)} s"
                             + (" ; les bouquets de côté : 1 ennemi chacun" if level >= 2 else "")
                             + (f" ; bouquet final : {r1(d)} chacun / {r1(p['m_FinaleInterval'])} s" if level >= 3 else "")
                             + f" ; éblouis {r1(t)} s sur {r1(i)} (contrôle plafonné à 5 : le groupe ne bouge plus)."))
    return Building("Feu d'artifice", "Duos", "Mortier + Phare", rows, "")


def sirenes():
    p = duo("Duo_Chant_des_sirenes", "DuoSirenes")
    iv, targets, charm, hit = p["m_IntervalPerLevel"], p["m_TargetsPerLevel"], p["m_CharmTimePerLevel"], p["m_HitDamagePerLevel"]
    cd = p["m_HitCooldown"]
    uptime = 0.6
    rows = []
    for level in (1, 2, 3):
        i, n, t, d = per_level(iv, level), per_level(targets, level), per_level(charm, level), per_level(hit, level)
        charmed = n * t / i
        spread = n * 2 * p["m_SpreadTime"] / i if level >= 2 else 0.0
        dz = (charmed + spread) * d / cd * uptime
        cc = min(GROUP, charmed + spread + 0.6 * (charmed + spread) / cd)
        if level >= 3:
            dz += GROUP * d / cd * uptime * p["m_MeleeTime"] / p["m_MeleeInterval"]
        desc = {
            1: f"Le faisceau charme un ennemi {r1(t)} s, toutes les {r1(i)} s : il change de camp, se jette sur les autres, les renverse et les "
               f"blesse ({r1(d)} par coup) ; seul, il repart vers la forêt.",
            2: f"Deux à la fois ({r1(d)} par coup) ; à la fin, la bulle éclate en deux bulles qui charment deux voisins {r1(p['m_SpreadTime'])} s.",
            3: f"La mêlée : toutes les {r1(p['m_MeleeInterval'])} s, tout ce qui est à {int(p['m_MeleeRadius'])} m change de camp "
               f"{r1(p['m_MeleeTime'])} s et frappe son voisin.",
        }[level]
        rows.append(Row(level, DUO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]),
                        dz=dz, cc=cc,
                        calc=f"{r1(charmed + spread)} charmé(s) en moyenne, un coup de {r1(d)} toutes les {r1(cd)} s, {pct(uptime)} du temps au contact ; "
                             f"seul, le charmé n'a personne à frapper (1 cible : 0)" + (" ; la mêlée : le groupe se frappe 5 s sur 15" if level >= 3 else "")
                             + "."))
    return Building("Chant des sirènes", "Duos", "Phare + Bulle", rows, "")


def loupe():
    p = duo("Duo_Loupe", "DuoLoupe")
    damage = p.get("m_BoltDamagePerLevel", [30.0, 45.0, 45.0])
    interval = p.get("m_BoltIntervalPerLevel", [2.0, 1.6, 1.6])
    ray_cycle = p["m_SmokeTime"] + p["m_BurnTime"] + 0.5
    rows = []
    for level in (1, 2, 3):
        d, i = per_level(damage, level), per_level(interval, level)
        bolt = d * 2 / i      # dans la tête (×2)
        desc = {
            1: f"Une loupe de 3 m : dans son champ ({int(p['m_Range'])} m, {int(2 * p['m_HalfAngle'])}°), la tête des ennemis est "
               f"{r1(p['m_HeadScalePerLevel'][0])} fois plus grosse (sa zone de tir aussi) ; elle y plante des carreaux ({r1(d)}, ×2 à la "
               f"tête, toutes les {r1(i)} s).",
            2: f"Une baliste sur la fourche : des carreaux plus forts ({r1(d)}, ×2 à la tête, toutes les {r1(i)} s).",
            3: f"La fourmi : un rayon de soleil sur le plus fort du champ ; il fume, puis prend feu ({pct(p['m_BurnShare'])} de ses PV, "
               f"un boss {pct(p['m_BossBurnShare'])}) ; le rayon passe au suivant.",
        }[level]
        ray_d = p["m_BurnDamage"] / ray_cycle if level >= 3 else 0.0
        ray_p = p["m_BurnShare"] / ray_cycle if level >= 3 else 0.0
        rows.append(Row(level, DUO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]),
                        d1=bolt + ray_d, p1=ray_p, dz=bolt + ray_d, pz=ray_p,
                        calc=f"{r1(d)} × 2 (tête) / {r1(i)} s, une cible" + (
                            f" ; le rayon : un ennemi toutes les {r1(ray_cycle)} s, {pct(p['m_BurnShare'])} des PV et {r1(p['m_BurnDamage'])} "
                            f"de feu" if level >= 3 else "") + ". Les têtes plus grosses pour tes flèches ne sont pas comptées."))
    return Building("Loupe", "Duos", "Phare + Baliste", rows, "")


def carquois():
    p = duo("Duo_Carquois_geant", "DuoCarquoisGeant")
    at, per_arrow, dmg = p["m_VolleyAtPerLevel"], p["m_BoltsPerArrowPerLevel"], p["m_BoltDamagePerLevel"]
    blast = p["m_BlastDamage"]
    rows = []
    for level in (1, 2, 3):
        n, k, d = per_level(at, level), per_level(per_arrow, level), per_level(dmg, level)
        period = n * MISSED_EVERY
        bolts = n * k
        b = blast if level >= 2 else 0.0
        dz = bolts * (d + 3 * b) / period
        d1 = bolts * (0.3 * d + 0.6 * b) / period
        auto = p.get("m_AutoInterval", 2.0)
        if auto > 0:
            d1 += (d + 0.6 * b) / auto
            dz += (d + 3 * b) / auto
        desc = {
            1: f"L'arbalète tire seule sur l'ennemi le plus proche ({r1(d)} toutes les {r1(p.get('m_AutoInterval', 2.0))} s) ; tes flèches "
               f"ratées (plantées à {int(p['m_CatchRadiusPerLevel'][0])} m) sont rattrapées ; à {n}, elle les renvoie en carreaux sur le "
               f"groupe le plus serré.",
            2: f"Chaque carreau explose à l'impact ({r1(b)} dans {r1(p['m_BlastRadius'])} m) ({r1(d)} par carreau).",
            3: f"La nuée : toutes tes flèches ratées, partout ; à {n}, elles repartent d'un coup, {k} carreaux chacune ({r1(d)}).",
        }[level]
        rows.append(Row(level, DUO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]), d1=d1, dz=dz,
                        calc=f"Seule, l'arbalète tire un carreau toutes les {r1(auto)} s sur le plus proche ; et selon toi : une flèche ratée "
                             f"toutes les {r1(MISSED_EVERY)} s, une salve de {bolts} carreaux toutes les {r1(period)} s ; "
                             f"groupe : chaque carreau touche" + (" et son explosion 3 ennemis" if b else "") + " ; 1 cible : 30 % des carreaux."))
    return Building("Carquois géant", "Duos", "Baliste + Lance-filet", rows, "")


def chaine():
    p = duo("Duo_Chaine_de_forcats", "DuoChaineForcats")
    iv, linked = p["m_IntervalPerLevel"], p["m_MaxLinkedPerLevel"]
    share, slow, dur = p["m_Share"], p["m_Slow"], p["m_Duration"]
    rows = []
    for level in (1, 2, 3):
        i = per_level(iv, level)
        uptime = min(1.0, dur / i)
        support = 1 + share * (GROUP - 1) * uptime
        cc = GROUP * (1 - slow) * uptime
        dz = pz = 0.0
        if level >= 3:
            dz = GROUP * p["m_CrushDamage"] / i
            pz = GROUP * p["m_CrushShare"] / i
            cc += GROUP * (p["m_CrushStun"] + 0.8) / i
        desc = {
            1: f"Des chaînes aimantées lient jusqu'à {per_level(linked, 1)} ennemis {r1(dur)} s (toutes les {r1(i)} s) : ralentis, et chaque dégât "
               f"reçu par l'un, les autres en reçoivent {pct(share)}.",
            2: f"Jusqu'à {per_level(linked, 2)} liés, toutes les {r1(i)} s ; un lié qui meurt fait tomber les autres.",
            3: f"L'écrasement : jusqu'à {per_level(linked, 3)} liés ; la chaîne se resserre d'un coup : ils s'écrasent au milieu "
               f"({r1(p['m_CrushDamage'])} et {pct(p['m_CrushShare'])} de leurs PV), sonnés {r1(p['m_CrushStun'])} s.",
        }[level]
        rows.append(Row(level, DUO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]), dz=dz, pz=pz, cc=cc, support=support,
                        calc=f"Soutien : sur le groupe lié ({pct(uptime)} du temps), chaque dégât compte 1 + 4 × {r1(share)} fois : ×{r1(support)} "
                             f"sur tous les dégâts reçus ; ralentis de {pct(1 - slow)}" + ("; l'écrasement toutes les " + r1(i) + " s" if level >= 3 else "")
                             + ". Seul : rien (il faut deux liés)."))
    return Building("Chaîne de forçats", "Duos", "Tour-aimant + Lance-filet", rows, "")


def boussole():
    p = duo("Duo_Boussole_folle", "DuoBoussole")
    lost, spike, back = p["m_LostTimePerLevel"], p["m_SpikeDamagePerLevel"], p["m_BackwardTimePerLevel"]
    tick = p["m_SpikeInterval"]
    rows = []
    for level in (1, 2, 3):
        t, d, b = per_level(lost, level), per_level(spike, level), per_level(back, level)
        loss = t + 2 * b
        cc = FLOW_GROUP * loss
        if level >= 3:
            rose = p["m_RoseLostTime"] + 2 * p["m_RoseBackwardTime"]
            cc += GROUP * rose / p["m_RoseInterval"]
        desc = {
            1: f"Une rose des vents hérissée de pointes sur la voie : l'ennemi qui y entre perd le nord et tourne en rond {r1(t)} s sur les "
               f"pointes ({r1(d)} toutes les {r1(tick)} s) ; pas avant {int(p['m_Immunity'])} s de nouveau.",
            2: f"Il tourne {r1(t)} s ({r1(d)} / {r1(tick)} s), puis repart dans le mauvais sens, vers la forêt, {r1(b)} s.",
            3: f"La rose des vents : toutes les {int(p['m_RoseInterval'])} s, toute la voie perd le nord à {int(p['m_RoseReach'])} m : chacun tourne "
               f"en rond {r1(p['m_RoseLostTime'])} s, puis repart vers la forêt {r1(p['m_RoseBackwardTime'])} s.",
        }[level]
        rows.append(Row(level, DUO_PLANS[level - 1], desc, f"Rose de {r1(2 * p['m_Radius'])} m", t * SPEED + 2 * p["m_Radius"],
                        d1=d / tick, dz=GROUP * d / tick, cc=cc,
                        calc=f"Pendant qu'il tourne : {r1(d)} / {r1(tick)} s ; chemin perdu par ennemi : {r1(t)} s" + (f" + 2 × {r1(b)} s à reculons" if b else "")
                             + f", débit {r1(FLOW_GROUP)}/s" + (" ; la rose : le groupe perd 15 s toutes les 16 s" if level >= 3 else "") + "."))
    return Building("Boussole folle", "Duos", "Piques + Tour-aimant", rows, "")


def baudruches():
    p = duo("Duo_Lanceur_baudruches", "DuoBaudruches")
    iv, hits, conf = p["m_IntervalPerLevel"], p["m_HitsPerLevel"], p["m_ConfettiDamagePerLevel"]
    hit_iv = p["m_HitInterval"]
    rows = []
    for level in (1, 2, 3):
        i, h, d = per_level(iv, level), per_level(hits, level), per_level(conf, level)
        life = h * hit_iv
        cc = min(GROUP, GROUP * life / i)
        if level >= 2:
            cc += GROUP * (p["m_BounceDistance"] / SPEED) / (p["m_HitsPerBounce"] * hit_iv)
        d1, dz, p1, pz = d / i, GROUP * d / i, 0.0, 0.0
        if level >= 3:
            fi = p["m_FloatInterval"]
            d1 += p["m_FloatDamage"] / fi
            dz += GROUP * p["m_FloatDamage"] / fi
            p1 += p["m_FloatShare"] / fi
            pz += GROUP * p["m_FloatShare"] / fi
        desc = {
            1: f"Une baudruche à ton image tombe devant le premier ennemi : ceux qui sont à {int(p['m_LureRadiusPerLevel'][0])} m s'arrêtent pour la "
               f"frapper ; au {h}e coup, elle éclate en confettis ({r1(d)}, renversés) ; ta flèche la crève avant (+50 %).",
            2: f"Tous les 3 coups, elle rebondit {int(p['m_BounceDistance'])} m vers la forêt et sur le côté : ils la suivent ({h} coups, {r1(d)}).",
            3: f"Le char de carnaval : toutes les {int(p['m_FloatInterval'])} s, une baudruche de 7 m que tous les ennemis à "
               f"{int(p['m_FloatReach'])} m viennent frapper ; 40 coups, puis des confettis qui leur coûtent {pct(p['m_FloatShare'])} de leurs PV.",
        }[level]
        rows.append(Row(level, DUO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m (tir)", chord(p["m_Range"]),
                        d1=d1, p1=p1, dz=dz, pz=pz, cc=cc,
                        calc=f"Le groupe arrêté {r1(life)} s par baudruche ({h} coups, un toutes les {r1(hit_iv)} s), une toutes les {r1(i)} s ; confettis sur "
                             f"le groupe" + (" ; le rebond les tire de 4 m vers la forêt" if level >= 2 else "")
                             + (f" ; le char : {r1(p['m_FloatDamage'])} et {pct(p['m_FloatShare'])} toutes les {int(p['m_FloatInterval'])} s" if level >= 3 else "")
                             + ". La flèche qui crève (+50 %) n'est pas comptée."))
    return Building("Lanceur de baudruches", "Duos", "Mortier + Bulle", rows, "")


def boule_neige():
    p = duo("Duo_Boule_a_neige", "DuoBouleNeige")
    iv, life, crack = p["m_IntervalPerLevel"], p["m_LifetimePerLevel"], p["m_CrackDamagePerLevel"]
    rows = []
    for level in (1, 2, 3):
        i, t, d = per_level(iv, level), per_level(life, level), per_level(crack, level)
        shake = 3 * p["m_ShakeHitDamage"] if level >= 2 else 0.0
        dz, pz = GROUP * (d + shake) / i, 0.0
        cc = min(GROUP, GROUP * t / i)
        if level >= 3:
            gi = p["m_GiantInterval"]
            pz = GROUP * p["m_GiantCrackShare"] / gi
            dz += GROUP * shake / gi
            cc = GROUP
        desc = {
            1: f"Toutes les {r1(i)} s, une boule de verre tombe sur le groupe le plus serré et l'enferme {r1(t)} s : ils se figent ; ta flèche brise "
               f"la boule ({r1(p['m_ShatterDamagePerLevel'][0])} chacun) ; sinon elle se fend ({r1(d)}).",
            2: f"À mi-temps, la boule se secoue : les figurines se heurtent ({r1(p['m_ShakeHitDamage'])} par choc) ; fendue : {r1(d)} ; "
               f"brisée : {r1(p['m_ShatterDamagePerLevel'][1])}.",
            3: f"La boule immense : toutes les {int(p['m_GiantInterval'])} s, une boule de 18 m sur toute une voie ; brisée par ta flèche, chaque "
               f"figurine meurt ; fendue d'elle-même : {pct(p['m_GiantCrackShare'])} de leurs PV.",
        }[level]
        rows.append(Row(level, DUO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]),
                        d1=d / i, dz=dz, pz=pz, cc=cc,
                        calc=f"Le groupe enfermé {r1(t)} s sur {r1(i)} ; fendue d'elle-même : {r1(d)} chacun" + (" ; secousse : 3 chocs chacun" if shake else "")
                             + (f" ; la boule immense : {pct(p['m_GiantCrackShare'])} des PV toutes les {int(p['m_GiantInterval'])} s" if level >= 3 else "")
                             + " ; seul : pas de chocs. Ta flèche qui brise (90 à 120, ou la mort au niveau 3) n'est pas comptée."))
    return Building("Boule à neige", "Duos", "Bulle + Tour de givre", rows, "")


# ------------------------------------------------------------------------------------------------ les 7 trios

def enclume():
    p = duo("Trio_Enclume", "TrioEnclume")
    iv, dmg, flat = p["m_IntervalPerLevel"], p["m_DamagePerLevel"], p["m_FlatTimePerLevel"]
    rows = []
    for level in (1, 2, 3):
        i, d, t = per_level(iv, level), per_level(dmg, level), per_level(flat, level)
        # Les dégâts sont les mêmes dans tout le rayon ; visée sur le plus avancé, l'enclume touche le devant du groupe : 2,5 ennemis
        # dans 1,4 m, 4 au plus dans un rayon plus grand (2,5 m depuis le 2 novembre).
        hit = min(4.0, 2.5 * (p["m_SplashRadius"] / 1.4) ** 2)
        dz = hit * d
        if level >= 2:
            dz += d + max(0.0, GROUP - hit - 1) * p["m_WaveDamage"]
        d1, dz = d / i, dz / i
        cc = min(GROUP * (1 - p["m_FlatSpeed"]), hit * t * (1 - p["m_FlatSpeed"]) / i) + (2.5 * 1.4 / i if level >= 2 else 0)
        if level >= 3:
            ri = p["m_RainInterval"]
            d1 += (4 * p["m_RainDamage"] + p["m_GiantDamage"]) / ri
            dz += (20 * 2 * p["m_RainDamage"] + GROUP * p["m_GiantDamage"]) / ri
        desc = {
            1: f"Toutes les {r1(i)} s, une enclume en cloche sur l'ennemi le plus avancé à {int(p['m_Range'])} m ({r1(d)} dans "
               f"{r1(p['m_SplashRadius'])} m) ; les survivants sont aplatis {r1(t)} s : deux fois moins vite.",
            2: f"L'onde ({r1(p['m_WaveDamage'])} dans {r1(p['m_WaveRadius'])} m, renversés) et un rebond sur le suivant ({r1(d)}, toutes les {r1(i)} s).",
            3: f"Toutes les {int(p['m_RainInterval'])} s, vingt enclumes du ciel ({r1(p['m_RainDamage'])}), puis la grande enclume : "
               f"{int(p['m_GiantDamage'])} de dégâts dans {int(p['m_GiantRadius'])} m.",
        }[level]
        rows.append(Row(level, TRIO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]), d1=d1, dz=dz, cc=cc,
                        calc=f"{r1(d)} / {r1(i)} s sur {r1(hit)} ennemis du groupe ({r1(p['m_SplashRadius'])} m)" + (" ; le rebond : 1 de plus ; l'onde sur les autres" if level >= 2 else "")
                             + (f" ; la pluie : 2 ennemis par enclume, 4 sur la cible ; la grande enclume sur le groupe ({int(p['m_GiantDamage'])})" if level >= 3 else "")
                             + "."))
    return Building("Enclume", "Trios", "Marteau-pilon + Mortier", rows, "")


def gonfleur():
    p = duo("Trio_Gonfleur", "TrioGonfleur")
    iv, hoses, harpoon = p["m_IntervalPerLevel"], p["m_HosesPerLevel"], p["m_HarpoonDamagePerLevel"]
    f = fall(p["m_FloatHeight"])
    cycle = p["m_InflateTime"] + p["m_FloatTime"] + math.sqrt(2 * p["m_FloatHeight"] / G) + LANDING_STUN
    rows = []
    for level in (1, 2, 3):
        i, n, d = per_level(iv, level), per_level(hoses, level), per_level(harpoon, level)
        rate = min(n / i, FLOW_GROUP)
        p1, pz = f / max(i, cycle), rate * f
        cc = rate * cycle
        if level >= 3:
            pf = fall(p["m_ParadeHeight"])
            pi = p["m_ParadeInterval"]
            p1 += pf / pi
            pz += GROUP * pf / pi
            cc += GROUP * (0.8 + p["m_ParadeTime"] + 1.4 + LANDING_STUN) / pi
        desc = {
            1: f"Toutes les {r1(i)} s, un harpon-tuyau ({r1(d)}) : l'ennemi enfle en {r1(p['m_InflateTime'])} s et s'envole à "
               f"{int(p['m_FloatHeight'])} m ; crevé (un coup), il blesse ceux du dessous ; sinon il crève tout seul au bout de "
               f"{int(p['m_FloatTime'])} s ; il retombe.",
            2: f"Deux tuyaux à la fois ({r1(d)}) ; un ballon crevé souffle son air sur deux voisins, qui gonflent aussi.",
            3: f"Le défilé : toutes les {int(p['m_ParadeInterval'])} s, toute la portée gonfle d'un coup ({int(p['m_ParadeMax'])} au plus), monte à "
               f"{int(p['m_ParadeHeight'])} m et flotte {int(p['m_ParadeTime'])} s.",
        }[level]
        rows.append(Row(level, TRIO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]),
                        d1=d / max(i, cycle), p1=p1, dz=rate * d, pz=pz, cc=cc,
                        calc=f"{n} ballon(s) / {r1(i)} s ; un ennemi en l'air {r1(cycle)} s, chute de {int(p['m_FloatHeight'])} m ({pct(f)})"
                             + (f" ; le défilé : chute de {int(p['m_ParadeHeight'])} m toutes les {int(p['m_ParadeInterval'])} s" if level >= 3 else "")
                             + " ; les confettis du ballon crevé (un coup du joueur) et le souffle ne sont pas comptés."))
    return Building("Gonfleur", "Trios", "Lanceur de baudruches + Baliste", rows, "")


def bowling():
    p = duo("Trio_Bowling", "TrioBowling")
    dmg = p["m_DamagePerLevel"]
    f = fall(peak(p["m_KnockUp"]))
    fast = p["m_BallInterval"] / 2      # dès 4 s s'il y a trois quilles rangées
    loss = p["m_PinDistance"] / SPEED + 2.0 + flight(p["m_KnockUp"]) + LANDING_STUN + p["m_KnockForward"] / SPEED
    rows = []
    for level in (1, 2, 3):
        d = per_level(dmg, level) * (1.25 if level >= 2 else 1)
        d1 = per_level(dmg, level) / p["m_BallInterval"]
        dz = GROUP * d / fast
        p1, pz = f / p["m_BallInterval"], GROUP * f / fast
        if level >= 3:
            perfect = 1.5 * per_level(dmg, 3) + 0.3 * p["m_GiantDamage"]
            d1 += perfect / p["m_PerfectInterval"]
            dz += GROUP * perfect / p["m_PerfectInterval"]
            pz += GROUP * 2 * f / p["m_PerfectInterval"]
        desc = {
            1: f"Les ennemis à {int(p['m_GatherRadius'])} m vont se ranger en quilles {int(p['m_PinDistance'])} m plus loin vers la forêt ; une boule "
               f"de glace se met en place sous l'arche, attend {r1(p.get('m_WindupTime', 2.0))} s (ta flèche dedans la lance plus grosse et "
               f"plus forte) et les fauche ({r1(per_level(dmg, 1))}, ils volent vers la forêt) ; toutes les 8 s, 4 avec trois quilles.",
            2: f"Après un strike, la boule suivante est deux fois plus grosse (×1,5) ; une deuxième boule pour les quilles restantes "
               f"({r1(per_level(dmg, 2))}).",
            3: f"La partie parfaite : toutes les {int(p['m_PerfectInterval'])} s, trois triangles de quilles et douze boules, la dernière de 4 m "
               f"({int(p['m_GiantDamage'])} de dégâts) ({r1(per_level(dmg, 3))} par boule).",
        }[level]
        rows.append(Row(level, TRIO_PLANS[level - 1], desc, f"{int(p['m_GatherRadius'])} m", 2 * p["m_GatherRadius"],
                        d1=d1, p1=p1, dz=dz, pz=pz, cc=GROUP * loss / fast,
                        calc=f"Groupe : une boule toutes les {r1(fast)} s, tout le groupe fauché ({r1(d)}" + (", strikes compris" if level >= 2 else "")
                             + f", chute {pct(f)}) ; chaque quille recule de {int(p['m_PinDistance'])} m, attend, vole : {r1(loss)} s perdues ; seul : une boule / 8 s"
                             + (" ; la partie parfaite : 1,5 boule et 30 % de la grande par ennemi" if level >= 3 else "") + "."))
    return Building("Bowling", "Trios", "Avalanche + Barricade", rows, "")


def beffroi():
    p = duo("Trio_Beffroi", "TrioBeffroi")
    iv = p["m_IntervalPerLevel"]
    rows = []
    for level in (1, 2, 3):
        i = per_level(iv, level)
        loss = p["m_RewindSeconds"] + p["m_RewindTime"] + (p["m_ArrivalStun"] if level >= 2 else 0)
        cc = GROUP * loss / i
        # Le Phare du passé (2 novembre au soir ; Minuit avant, passé à la Grande Horloge) : chaque ennemi de la carte
        # rembobiné et figé une fois par tour du faisceau. Ses réglages n'arrivent dans le prefab qu'après Unity.
        beam_iv = p.get("m_BeamInterval", 20.0)
        beam_r = p.get("m_BeamRadius", 110.0)
        if level >= 3:
            cc += GROUP * (p["m_RewindSeconds"] + p["m_RewindTime"] + p["m_ArrivalStun"]) / beam_iv
        desc = {
            1: f"Toutes les {r1(i)} s, la cloche sonne : jusqu'à {int(p['m_MaxRewound'])} ennemis à {int(p['m_Range'])} m rembobinent leurs "
               f"{int(p['m_RewindSeconds'])} dernières secondes de chemin, à reculons. Aucun dégât.",
            2: f"À l'arrivée, ils restent figés {int(p['m_ArrivalStun'])} s (étourdis) ; toutes les {r1(i)} s.",
            3: f"Le Phare du passé : toutes les {int(beam_iv)} s, la lanterne s'allume et un faisceau sépia fait le tour du beffroi "
               f"en {int(p.get('m_BeamTurnTime', 6.0))} s, sur toute la carte ({int(beam_r)} m) : chaque ennemi au sol qu'il balaie "
               f"rembobine ses {int(p['m_RewindSeconds'])} dernières secondes de chemin, puis reste figé {int(p['m_ArrivalStun'])} s.",
        }[level]
        rows.append(Row(level, TRIO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]), cc=cc,
                        calc=f"Par ennemi : {int(p['m_RewindSeconds'])} s de chemin + {r1(p['m_RewindTime'])} s à reculons"
                             + (f" + {int(p['m_ArrivalStun'])} s figé" if level >= 2 else "") + f", le groupe toutes les {r1(i)} s"
                             + (f" ; le Phare du passé : le groupe rembobiné et figé une fois de plus toutes les {int(beam_iv)} s (toute "
                                f"la carte, {int(beam_r)} m)" if level >= 3 else "")
                             + "."))
    return Building("Beffroi", "Trios", "Boussole folle + Phare", rows, "")


def colisee():
    p = duo("Trio_Colisee", "TrioColisee")
    iv, champs, cdmg = p["m_IntervalPerLevel"], p["m_ChampionsPerLevel"], p["m_ChampionDamagePerLevel"]
    rows = []
    for level in (1, 2, 3):
        i, n, d = per_level(iv, level), per_level(champs, level), per_level(cdmg, level)
        cycle = max(i, p["m_ChampionTime"] / n)
        champion_dps = n * d / p["m_ChampionCooldown"] * 0.6
        pz = GROUP / cycle
        if level >= 2:
            pz += 2 / cycle
        if level >= 3:
            pz += 0.5 * GROUP / p["m_CircusInterval"]
            champion_dps += 3 * d * 3 / p["m_ChampionCooldown"] * 0.6 * p["m_ChampionTime"] / p["m_CircusInterval"] / 3
        desc = {
            1: f"Toutes les {int(i)} s, une arène s'élève autour du groupe le plus serré à {int(p['m_Range'])} m : jusqu'à {int(p['m_MaxGladiators'])} "
               f"ennemis se battent ({pct(p['m_HitShare'])} de leurs PV par coup) ; le dernier debout devient ton champion "
               f"{int(p['m_ChampionTime'])} s ({r1(d)} par coup), puis tombe. Ta flèche élimine un gladiateur.",
            2: f"Deux champions à la fois ({r1(d)}) ; un champion qui tombe éclate en bulles qui font se battre 3 voisins 5 s.",
            3: f"Les jeux du cirque : toutes les {int(p['m_CircusInterval'])} s, une arène de {int(p['m_CircusRadius'])} m avale jusqu'à "
               f"{int(p['m_CircusGladiators'])} ennemis ; le vainqueur devient le grand champion (×3, ses coups ×3).",
        }[level]
        rows.append(Row(level, TRIO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]),
                        d1=0.0, p1=1 / cycle, dz=champion_dps, pz=pz, cc=GROUP, expo1=cycle, expoz=cycle,
                        calc=f"Un champion ne laisse pas d'arène tant qu'il vit : une arène toutes les {r1(cycle)} s ; tout le groupe y meurt (le champion "
                             f"tombe à la fin) ; le champion frappe {pct(0.6)} du temps" + (" ; les bulles : 2 morts de plus" if level >= 2 else "")
                             + (" ; le cirque : la moitié du groupe de plus" if level >= 3 else "") + ". 1 cible : il devient champion et tombe."))
    return Building("Colisée", "Trios", "Chant des sirènes + Barricade", rows, "")


def bombarde():
    p = duo("Trio_Bombarde", "TrioBombarde")
    rate, bombs, dmg = p["m_ChargeRatePerLevel"], p["m_MaxBombsPerLevel"], p["m_BombDamagePerLevel"]
    rows = []
    for level in (1, 2, 3):
        fill = KILLS_PER_SECOND * p["m_KillCharge"] * per_level(rate, level)
        period = 1 / fill
        n, d = per_level(bombs, level), per_level(dmg, level)
        if level >= 3:
            n = p["m_FinaleBombs"] / 2      # le bouquet final : la moitié des bombes sur le groupe
        hits_each = n * 3 * 0.8 / GROUP
        desc = {
            1: f"Un mortier géant qui se remplit de poudre (chaque ennemi tué à {int(p['m_FeedRadius'])} m, chaque flèche dans sa gueule) ; ta flèche "
               f"dans la mèche tire la salve : jusqu'à {per_level(bombs, 1)} bombes ({r1(d)} dans {int(p['m_BombRadius'])} m, projetés).",
            2: f"Deux fois plus vite rempli ; jusqu'à {per_level(bombs, 2)} bombes ({r1(d)}) ; la secousse renverse tout à {int(p['m_ShakeRadius'])} m.",
            3: f"Le bouquet final : pleine, elle n'attend plus : {int(p['m_FinaleBombs'])} bombes en {r1(p['m_FinaleTime'])} s sur toutes les voies "
               f"à {int(p['m_FinaleRange'])} m ({r1(d)}).",
        }[level]
        rows.append(Row(level, TRIO_PLANS[level - 1], desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]),
                        d1=n * d * 0.8 / period, dz=GROUP * hits_each * d / period, cc=GROUP * (flight(p["m_Lift"]) + LANDING_STUN) * hits_each / period,
                        calc=f"DÉPEND DE TOI : {r1(KILLS_PER_SECOND)} ennemi tué par seconde à portée, pleine en {r1(period)} s"
                             + (" et tirée aussitôt" if level < 3 else ", le bouquet final aussitôt") + f" ; {int(n)} bombes sur le groupe, 3 ennemis chacune "
                             f"à 80 %."))
    return Building("Grande bombarde", "Trios", "Feu d'artifice + Tremplin", rows, "")


def montagnes_russes():
    p = duo("Trio_Montagnes_russes", "TrioMontagnesRusses")
    wagons, seats, pick = p["m_WagonCount"], p["m_HookSeats"], p["m_PickDamagePerLevel"]
    lap = (2 * 8 + 2 * math.pi * 1.2) / p["m_TrackSpeed"]
    f = fall(2.2 + peak(p["m_FlingLift"]))
    loss = 2.0 + flight(p["m_FlingLift"], start=2.2) + LANDING_STUN + 12 / SPEED
    rows = []
    for level in (1, 2, 3):
        cap = per_level(wagons, level) * per_level(seats, level) / lap
        rate = min(cap * 0.6, FLOW_GROUP)
        d = per_level(pick, level)
        dz = rate * d + (2 * p["m_WhipDamage"] if level >= 2 else 0)
        d1, p1 = FLOW_ONE * d, FLOW_ONE * f
        pz = rate * f
        if level >= 3:
            gi = p["m_GreatInterval"]
            p1 += 1 / gi
            pz += GROUP / gi
            dz += 3 * p["m_CrashDamage"] / gi
        desc = {
            1: f"Un circuit en l'air au-dessus de la voie ; sous chaque wagon, un grappin qui se balance d'un bord à l'autre accroche les ennemis "
               f"({r1(d)}) ; au virage du côté de la forêt, ils sont jetés une douzaine de mètres plus loin.",
            2: f"Trois wagons ; les accrochés d'un wagon fouettent au passage ceux du dessous ({int(p['m_WhipDamage'])}, renversés) ({r1(d)}).",
            3: f"Le grand huit infernal : toutes les {int(p['m_GreatInterval'])} s, six wagons accrochent tout, sautent du tremplin sur le groupe "
               f"le plus serré à {int(p['m_JumpRange'])} m : les accrochés meurent ({int(p['m_CrashDamage'])} autour).",
        }[level]
        rows.append(Row(level, TRIO_PLANS[level - 1], desc, "Au-dessus de la voie", 12.4,
                        d1=d1, p1=p1, dz=dz, pz=pz, cc=rate * loss, expo1=1 / FLOW_ONE, expoz=GROUP / rate,
                        calc=f"Un tour en {r1(lap)} s, {r1(cap)} places par seconde, 60 % remplies : {r1(rate)} accrochés/s ; jeté : chute {pct(f)}, "
                             f"{r1(loss)} s perdues" + (" ; le fouet : 2 ennemis par seconde" if level >= 2 else "")
                             + (f" ; le grand huit : le groupe tué toutes les {int(p['m_GreatInterval'])} s" if level >= 3 else "") + "."))
    return Building("Montagnes russes (Grappin)", "Trios", "Chaîne de forçats + Tremplin", rows, "")


def colosse(p=None):
    """L'ultime Colosse de forge (2 novembre, 12.48), avec les trios : la forge de la main gauche (un éclat à chaque coup, le lingot au
    dernier), les marteaux de la main droite (un par flèche qui touche ; ils dépendent de toi). <p> : ses réglages (sinon, le prefab)."""
    p = p or duo("Ultime_Colosse", "UltimeColosse")
    interval, strikes = p["m_ForgeInterval"], int(p["m_StrikesPerIngot"])
    # Un tour de forge : les coups, dont le grand, dont le geste dure 0,63 s de plus (1,19 s au lieu de 0,56).
    cycle = strikes * interval + 0.63

    def falloff(radius):
        # Moitié des dégâts au bord ; le groupe à 1 m du centre en moyenne.
        return 1 - 0.5 * min(1.0, 1.0 / radius)

    chunk, ingot, hammer = p["m_ChunkDamage"], p["m_Damage"], p["m_HammerDamage"]
    chunks = (strikes - 1) / cycle
    # Les éclats vont sur un ennemi au hasard : sur le groupe une fois sur deux. Le lingot, sur le groupe le plus serré.
    forge1 = chunks * chunk + ingot / cycle
    forgez = chunks * chunk * (0.5 * GROUP * falloff(p["m_ChunkRadius"]) + 0.5) + GROUP * ingot * falloff(p["m_Radius"]) / cycle
    most = 1 / p["m_ThrowInterval"]

    def with_hits(hits):
        hits = min(hits, most)
        return forge1 + hits * hammer, forgez + hits * GROUP * hammer * falloff(p["m_HammerRadius"])

    d1, dz = with_hits(HITS_PER_SECOND)
    d1_2, dz_2 = with_hits(2.0)
    d1_max, dz_max = with_hits(most)
    hits = min(HITS_PER_SECOND, most)
    cc = hits * GROUP * (flight(p["m_HammerLift"]) + LANDING_STUN) + GROUP * (flight(p["m_Lift"]) + LANDING_STUN) / cycle
    desc = (f"L'ultime (l'Enclume ou la Grande bombarde au niveau 3, et 10 plans). La forge : un coup de masse toutes les {r1(interval)} s, "
            f"un éclat de fonte ({r1(chunk)} dans {r1(p['m_ChunkRadius'])} m) ; au {strikes}e, le lingot ({int(ingot)} dans "
            f"{r1(p['m_Radius'])} m, la foudre). Les marteaux : un par flèche qui touche à {int(p['m_Range'])} m ({int(hammer)} dans "
            f"{r1(p['m_HammerRadius'])} m, la foudre, projetés), {r1(most)} par seconde au plus.")
    calc = (f"DÉPEND DE TOI : {r1(HITS_PER_SECOND)} flèche qui touche par seconde à sa portée, un tir « Bon » (son action suit la force de la flèche depuis le 3 novembre : parfait ×2, moyen ×0,8, la pluie moins ; 2 par seconde : {r1(d1_2)} sur une cible, "
            f"{r1(dz_2)} sur le groupe ; au plafond de {r1(most)} : {r1(d1_max)} et {r1(dz_max)}). Seule, la forge : {r1(forge1)} et "
            f"{r1(forgez)}. Les éclats sur le groupe une fois sur deux, le lingot sur le groupe ; le grand geste dure 0,63 s de plus.")
    rows = [Row(4, ULTIMATE_PLANS, desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]), d1=d1, dz=dz, cc=cc, calc=calc)]
    return Building("Colosse de forge (ultime)", "Trios", "Enclume ou Grande bombarde, niveau 4", rows, "")


# Les réglages du Grand Manège dans son code (Building/UltimeManege.cs), tant que Unity n'a pas fait son prefab (2 novembre, tard).
MANEGE_CODE = {
    "m_Range": 30.0, "m_RoundInterval": 1.2, "m_CatchDamage": 60.0, "m_RideDamage": 70.0, "m_RoundsPerEjection": 5.0,
    "m_EjectWindup": 0.9, "m_EjectDamage": 100.0, "m_EjectRadius": 5.0, "m_EjectLift": 11.0, "m_EjectSelfDamage": 200.0,
    "m_GallopInterval": 0.2, "m_HorseDamage": 160.0, "m_HorseRadius": 6.0, "m_HorseLift": 12.0,
}
MANEGE_CATCH = 0.82   # UltimeManege : le ruban va jusqu'à sa cible (k_Reach) et l'amène sur le manège (k_Pull)
MANEGE_DECK = 4.0     # UltimeManege : le dessus du plateau (k_DeckHeight), d'où partent les éjectés
MANEGE_FLIGHT = 1.5   # UltimeManege.Eject : la durée du vol d'un éjecté (0,9 s + 1 s tous les 22 m, de 1 à 2,2 s)


def manege(p=None):
    """L'ultime Grand Manège (2 novembre, tard, 12.51), avec les trios : la ronde (le ruban attrape un ennemi et le fait tourner sur
    le manège, le tournis), la grande éjection au 5e coup (chacun retombe sur le groupe le plus serré), un cheval par flèche qui touche
    (ils dépendent de toi). <p> : ses réglages (sinon, le prefab ; tant que Unity ne l'a pas fait, ceux du code)."""
    if p is None:
        try:
            p = duo("Ultime_Manege", "UltimeManege")
        except (OSError, LookupError):
            p = dict(MANEGE_CODE)
    interval, rounds, windup = p["m_RoundInterval"], int(p["m_RoundsPerEjection"]), p["m_EjectWindup"]
    # Un tour de ronde : le ruban attrape aux coups 1 à 4, la grande éjection part au 5e (le temps qu'on la voie venir), et le coup
    # suivant attend un intervalle.
    cycle = rounds * interval + windup
    caught = rounds - 1
    # Le tournis jusqu'à la grande éjection : un ennemi seul, attrapé au 1er coup ; le groupe, en moyenne sur les quatre.
    ride_one = (rounds - 1) * interval + windup - MANEGE_CATCH
    ride_avg = (rounds - 1) * interval + windup - MANEGE_CATCH - (caught - 1) / 2 * interval

    def falloff(radius):
        # Moitié des dégâts au bord ; le groupe à 1 m du centre en moyenne.
        return 1 - 0.5 * min(1.0, 1.0 / radius)

    catch, ride, self_hit, eject = p["m_CatchDamage"], p["m_RideDamage"], p["m_EjectSelfDamage"], p["m_EjectDamage"]
    # L'éjecté retombe de la hauteur du plateau, plus sa cloche : sa chute (Enemy.Launch).
    g = G
    up = (-MANEGE_DECK + 0.5 * g * MANEGE_FLIGHT ** 2) / MANEGE_FLIGHT
    drop = fall(MANEGE_DECK + max(0.0, peak(up)))
    round1 = (catch + ride * ride_one + self_hit) / cycle
    roundz = (caught * (catch + ride * ride_avg + self_hit) + caught * eject * (GROUP - 1) * falloff(p["m_EjectRadius"])) / cycle
    most = 1 / p["m_GallopInterval"]
    horse = p["m_HorseDamage"]

    def with_hits(hits):
        hits = min(hits, most)
        return round1 + hits * horse, roundz + hits * GROUP * horse * falloff(p["m_HorseRadius"])

    d1, dz = with_hits(HITS_PER_SECOND)
    d1_2, dz_2 = with_hits(2.0)
    d1_max, dz_max = with_hits(most)
    hits = min(HITS_PER_SECOND, most)
    cc = (caught * (MANEGE_CATCH + ride_avg + MANEGE_FLIGHT + LANDING_STUN) + caught * (GROUP - 1) * (flight(p["m_EjectLift"]) + LANDING_STUN)) / cycle \
        + hits * GROUP * (flight(p["m_HorseLift"]) + LANDING_STUN)
    desc = (f"L'ultime (le Gonfleur, le Colisée ou les Montagnes russes au niveau 3, et 10 plans). La ronde : toutes les {r1(interval)} s, "
            f"le ruban attrape un ennemi à {int(p['m_Range'])} m ({int(catch)}) et le fait tourner sur le manège ({int(ride)} par seconde) ; au "
            f"{rounds}e coup, la grande éjection : chacun retombe sur le groupe le plus serré ({int(self_hit)} pour lui, {int(eject)} dans "
            f"{r1(p['m_EjectRadius'])} m autour, projetés). Les chevaux : un par flèche qui touche ({int(horse)} dans {r1(p['m_HorseRadius'])} m, "
            f"le feu d'artifice, projetés), {r1(most)} par seconde au plus.")
    calc = (f"DÉPEND DE TOI : {r1(HITS_PER_SECOND)} flèche qui touche par seconde à sa portée, un tir « Bon » (son action suit la force de la flèche depuis le 3 novembre : parfait ×2, moyen ×0,8, la pluie moins ; 2 par seconde : {r1(d1_2)} sur une cible, "
            f"{r1(dz_2)} sur le groupe ; au plafond de {r1(most)} : {r1(d1_max)} et {r1(dz_max)}). Seule, la ronde : {r1(round1)} et "
            f"{r1(roundz)}. Un tour de ronde en {r1(cycle)} s : {caught} attrapés, le tournis {r1(ride_one)} s pour un ennemi seul, "
            f"{r1(ride_avg)} s en moyenne dans le groupe ; chaque éjecté retombe sur les {GROUP - 1} autres. Sa chute : {pct(drop)} de ses PV. "
            f"La charge (toutes les 25 s) n'est pas comptée, comme la coulée du Colosse.")
    rows = [Row(4, ULTIMATE_PLANS, desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]), d1=d1, p1=drop / cycle, dz=dz, pz=caught * drop / cycle,
                cc=cc, calc=calc)]
    return Building("Grand Manège (ultime)", "Trios", "Gonfleur, Colisée ou Montagnes russes, niveau 4", rows, "")


# Les réglages de la Grande Horloge dans son code (Building/UltimeHorloge.cs), tant que Unity n'a pas fait son prefab (2 novembre, tard).
HORLOGE_CODE = {
    "m_Range": 30.0, "m_TickInterval": 1.2, "m_NumeralDamage": 220.0, "m_NumeralRadius": 4.5, "m_NumeralFreeze": 1.0,
    "m_MidnightWindup": 1.0, "m_MidnightTime": 9.0, "m_ShatterDamage": 500.0, "m_EchoInterval": 0.2, "m_EchoDamage": 160.0,
    "m_EchoRadius": 6.0, "m_EchoLift": 8.0, "m_EchoFreeze": 1.0,
}


def horloge(p=None):
    """L'ultime Grande Horloge (2 novembre, tard, 12.52), avec les trios : les douze chiffres (un par tic, sur un ennemi au hasard à sa
    portée), Minuit après le XII (le temps arrêté sur toute la carte, puis le dégel qui brise tous les figés), l'écho de chaque flèche qui
    touche (ils dépendent de toi). <p> : ses réglages (sinon, le prefab ; tant que Unity ne l'a pas fait, ceux du code)."""
    if p is None:
        try:
            p = duo("Ultime_Horloge", "UltimeHorloge")
        except (OSError, LookupError):
            p = dict(HORLOGE_CODE)
    tick, windup = p["m_TickInterval"], p["m_MidnightWindup"]
    # Un tour : le I au premier tic, le XII au douzième ; les aiguilles se rejoignent ; Minuit (douze coups, le dernier au bout des
    # onze douzièmes de sa durée) ; le tic suivant 1 s après.
    midnight = p["m_MidnightTime"] * 11 / 12
    cycle = 11 * tick + windup + midnight + 1.0

    def falloff(radius):
        # Moitié des dégâts au bord ; le groupe à 1 m du centre en moyenne.
        return 1 - 0.5 * min(1.0, 1.0 / radius)

    numeral, shatter = p["m_NumeralDamage"], p["m_ShatterDamage"]
    # Un ennemi seul : les douze chiffres sur lui, puis le dégel. Le groupe : un chiffre sur deux tombe sur lui (au hasard dans la
    # portée, comme les éclats du Colosse), le dégel sur les cinq.
    alone1 = (12 * numeral + shatter) / cycle
    on_group = 0.5 * GROUP * falloff(p["m_NumeralRadius"]) + 0.5
    alonez = (12 * numeral * on_group + GROUP * shatter) / cycle
    most = 1 / p["m_EchoInterval"]
    echo = p["m_EchoDamage"]

    def with_hits(hits):
        hits = min(hits, most)
        return alone1 + hits * echo, alonez + hits * GROUP * echo * falloff(p["m_EchoRadius"])

    d1, dz = with_hits(HITS_PER_SECOND)
    d1_2, dz_2 = with_hits(2.0)
    d1_max, dz_max = with_hits(most)
    hits = min(HITS_PER_SECOND, most)
    cc = (12 * on_group * p["m_NumeralFreeze"] + GROUP * midnight) / cycle + hits * GROUP * (flight(p["m_EchoLift"]) + LANDING_STUN)
    desc = (f"L'ultime (le Bowling ou le Beffroi au niveau 3, et 10 plans). Les chiffres : un tic toutes les {r1(tick)} s, l'un des douze "
            f"chiffres de glace part sur un ennemi à {int(p['m_Range'])} m ({int(numeral)} dans {r1(p['m_NumeralRadius'])} m, figés "
            f"{r1(p['m_NumeralFreeze'])} s). Après le XII, Minuit : le temps arrêté {r1(p['m_MidnightTime'])} s sur toute la carte, puis le "
            f"dégel qui brise ({int(shatter)} à chaque figé). L'écho : chaque flèche qui touche rejouée par une flèche de glace ({int(echo)} "
            f"dans {r1(p['m_EchoRadius'])} m), {r1(most)} par seconde au plus.")
    calc = (f"DÉPEND DE TOI : {r1(HITS_PER_SECOND)} flèche qui touche par seconde à sa portée, un tir « Bon » (son action suit la force de la flèche depuis le 3 novembre : parfait ×2, moyen ×0,8, la pluie moins ; 2 par seconde : {r1(d1_2)} sur une cible, "
            f"{r1(dz_2)} sur le groupe ; au plafond de {r1(most)} : {r1(d1_max)} et {r1(dz_max)}). Seule, l'horloge : {r1(alone1)} et "
            f"{r1(alonez)}. Un tour en {r1(cycle)} s (douze chiffres, Minuit) ; un chiffre sur deux sur le groupe ; le dégel sur tous les "
            f"figés de la carte (le groupe en compte cinq). Contrôle : les figés des chiffres, Minuit sur le groupe.")
    rows = [Row(4, ULTIMATE_PLANS, desc, f"{int(p['m_Range'])} m", chord(p["m_Range"]), d1=d1, dz=dz, cc=cc, calc=calc)]
    return Building("Grande Horloge (ultime)", "Trios", "Bowling ou Beffroi, niveau 4", rows, "")


# ------------------------------------------------------------------------------------------------ les changements proposés (Claude)

# Proposés par Claude le 2 novembre, d'après le tableau (vague 5, Normal) ; « après » : les chiffres attendus, à vérifier en relançant
# le script avec les nouvelles valeurs. Les changements se font ensuite par un baker, une fois que le joueur a rempli sa colonne.
PROPOSALS = {
    # Les tours
    "Barricade": "Rien de chiffré : c'est le mur (arrêter le groupe est son contrôle ; il tient 9 s face à un groupe de la vague 5 au "
                 "niveau 1, une minute au niveau 5). Seule remarque : au niveau 5, l'onde éloigne les ennemis du pendule et de la herse, "
                 "qui ne servent plus que la moitié du temps ; pour plus de dégâts : l'onde toutes les 3 s au lieu de 1,5 (m_WaveCooldown).",
    "Piques": "Rien : la plus forte sur le papier (3 ennemis tués par seconde au niveau 3), mais un ennemi n'y reste que 1 à 1,3 s ; sur un "
              "ennemi seul qui passe, elle est dans la moyenne (15 % de ses PV au niveau 1, 78 % au niveau 3). À surveiller : le saut du "
              "niveau 3 (0,5 s et une plaque deux fois plus large : ×2,2 d'un coup) ; si elle domine aux essais, intervalle "
              "0,8 / 0,65 / 0,5 → 0,8 / 0,7 / 0,6 s.",
    "Tremplin": "Rampe (niveau 5) : recharge 2 → 4 s et 6 → 4 ennemis au plus (m_CooldownPerLevel[4], m_RampMax). Pourquoi : elle tue "
                "chaque lancé quelle que soit la vague (100 % des PV) : 3,9 ennemis tués par seconde à la vague 5 et encore 2,3 à la vague "
                "15, quand le mortier au niveau 5 n'en tue plus que 0,6 ; en mode infini, elle fait la partie à elle seule. Après : environ "
                "1,2 tué par seconde, toujours la meilleure tour à partir de la vague 10. Niveaux 1 à 4 : rien.",
    "Baliste": "Dégâts 10 / 14,67 / 21,33 / 26 / 32 → 18 / 24 / 26 / 30 / 32 ; titan ×6 → ×3 (m_DamagePerLevel, m_TitanDamageScale). "
               "Pourquoi : c'est la tour d'une cible, mais au niveau 1 elle fait deux fois moins qu'un mortier sur une cible (3,2 % des PV "
               "par seconde contre 6,5 %) ; puis le titan triple d'un coup le niveau 5 (62 %). Après : 5,8 / 11,6 / 20 / 26 / 46 % des PV "
               "par seconde, une montée régulière.",
    "Mortier": "Dégâts 30 / 42 / 60 / 70 / 85 → 30 / 42 / 42 / 50 / 60 (m_DamagePerLevel). Pourquoi : le deuxième tube double déjà le "
               "niveau 3 ; avec des dégâts qui montent aussi, le niveau 3 fait 3,5 fois le niveau 2 (1,7 ennemi tué par seconde), plus que "
               "presque tous les duos au niveau 1. Après : 0,26 / 0,49 / 1,2 / 1,5 / 2,3 tués par seconde ; toujours la meilleure tour de "
               "zone.",
    "Tour de givre": "Le verglas (niveau 4) fait glisser l'ennemi EN AVANT (dans le sens de sa marche, 4 m/s pendant 0,7 s) : il gagne "
                     "plus de chemin qu'il n'en perd. À coder : le faire glisser vers la forêt, à 2 m/s (FrostTower.Slip). Ralentissement du "
                     "niveau 2 : 0,6 → 0,55 (m_SpeedPerLevel[1]). Le reste : rien (son contrôle est dans la moyenne, 1,5 à 4,5 s/s).",
    "Lance-filet": "Contre les volants : rien. Au sol (niveaux 4 et 5), il ne fait presque rien (0,08 tué par seconde, 1,1 s/s de "
                   "contrôle) pour 2 et 4 plans de plus : choc 15 → 35, étourdi 2 → 2,5 s (m_ShockDamage, m_ShockStun) ; 3 filets au "
                   "niveau 5 (m_NetsPerLevel : 1, 1, 2, 2, 3) ; cadence du niveau 5 : 3,5 → 3 s. Après : environ 0,3 tué par seconde et "
                   "2,1 s/s au niveau 4 ; 0,5 et 3,1 au niveau 5.",
    "Tour-aimant": "Impulsion 3 / 3,5 → 4 / 4,5 m et intervalle 3,5 / 3 → 3 / 2,5 s (niveaux 1 et 2) ; électro-aimant 1,6 → 2,2 m/s "
                   "(m_ContinuousPull) ; singularité toutes les 15 → 12 s. Le niveau 4 n'aide que tes flèches : lui donner aussi un "
                   "ralentissement de 25 % dans sa zone (à coder). Pourquoi : son contrôle (1,2 à 2,2 s/s) est le plus faible des tours de "
                   "contrôle, sans aucun dégât avant le niveau 5. Après : environ 1,9 / 2,5 / 3,1 / 4,3 / 5,9 s/s.",
    "Bulle": "Intervalle du niveau 1 : 7 → 6 s (m_IntervalPerLevel[0]). Le reste : rien (le contrôle monte bien, de 0,9 à 5 s/s ; l'orbe "
             "céleste tue un groupe toutes les 15 s sans décrocher avec les vagues). Le niveau 4 n'aide que tes flèches (le rebond).",
    "Phare": "Niveau 4 : il n'aide que tes flèches (l'anneau de feu) ; lui donner un peu de soutien : +30 % / +25 % → +35 % / +30 % "
             "(m_SupportDamagePerLevel[3], m_SupportRatePerLevel[3]). Niveau 5 : le jugement double la cadence 5 s sur 10 ; avec le "
             "soutien, ×2,8 sur les tours du rayon, de loin le plus fort : élan ×2 → ×1,5 (m_JudgmentBoost) : ×2,4.",
    # Les duos
    "Boulet fou": "Rien : c'est le meilleur duo (chaque ennemi frappé perd 7 à 10 s de chemin, avec une chute ou le choc de 60 %), au "
                  "niveau des deux tours qu'il remplace.",
    "Marteau-pilon": "Dégâts 45 / 60 / 70 → 120 / 150 / 180 ; intervalle 3 / 2,4 → 2 / 1,6 s (m_DamagePerLevel, m_IntervalPerLevel). "
                     "Pourquoi : il remplace une barricade et des piques au niveau 3 (environ 4 ennemis tués par seconde à elles deux) et "
                     "n'en tue que 0,23 : c'est le duo le plus faible. Un ennemi planté ne peut plus être frappé : ses dégâts doivent venir "
                     "du coup lui-même. Après : environ 0,9 / 2 / 3,5 tués par seconde.",
    "Toboggan": "Niveau 3 : 3 → 5 ennemis à la fois sur le toboggan (m_MaxRiders, par niveau : à coder). Pourquoi : le saut à ski tue, "
                "mais l'échelle limite le débit à moins d'un ennemi par seconde. Niveaux 1 et 2 : rien (beaucoup de contrôle : 10 et "
                "15 s/s).",
    "Yo-yo": "Le lustre (niveau 3) : claquement 45 → 70 et choc 45 → 60 % des PV (m_SlamDamagePerLevel[2], m_FlingSmashShare). "
             "Pourquoi : sur une cible, il fait moins que les niveaux 1 et 2 (31 % des PV par seconde contre 37 %). Niveaux 1 et 2 : rien.",
    "Avalanche": "Niveau 3 : toutes les 11 → 8 s, et 50 → 60 % des PV à l'éclat (m_IntervalPerLevel[2], m_GiantShare). Pourquoi : la "
                 "boule géante fait moins que le niveau 2 (0,64 tué par seconde contre 0,91). Niveaux 1 et 2 : rien (elle emporte le "
                 "groupe vers la forêt : 7 et 12 s/s de contrôle).",
    "Feu d'artifice": "Brûlure 30 / 36 / 60 → 70 / 85 / 130 (m_BurnDamagePerLevel). Pourquoi : il remplace un mortier au niveau 3 "
                      "(1,7 tué par seconde) et n'en tue que 0,48 au niveau 1. Après : environ 1,1 / 2,2 / 4,1 tués par seconde, "
                      "l'éblouissement en plus.",
    "Chant des sirènes": "Coup du charmé 20 / 26 / 24 → 45 / 55 / 65 (le niveau 3 était plus faible que le 2) ; deux charmés dès le "
                         "niveau 1 (m_HitDamagePerLevel, m_TargetsPerLevel : 2, 2, 2). Pourquoi : un seul charmé qui frappe 60 % du temps "
                         "fait 0,17 tué par seconde ; seul, il ne blesse personne. Après : environ 0,8 / 2,3 / 3,9.",
    "Loupe": "Avancer la baliste au niveau 1 : des carreaux de 30 toutes les 2 s dès le niveau 1, 45 toutes les 1,6 s au niveau 2 (à "
             "coder : le niveau qui l'allume). Pourquoi : au niveau 1, la Loupe ne fait rien d'elle-même (elle aide seulement tes "
             "flèches) alors qu'elle remplace une baliste au niveau 3 : contraire à la règle des duos, impressionnants dès le niveau 1. "
             "Après : 29 % des PV d'une cible par seconde au niveau 1.",
    "Carquois géant": "Volée à 10 / 10 / 30 → 6 / 6 / 20 flèches ; carreau 30 / 32 / 34 → 50 / 55 / 60 (m_VolleyAtPerLevel, "
                      "m_BoltDamagePerLevel) ; et l'arbalète tire seule un carreau toutes les 2 s sur l'ennemi le plus proche (à coder). "
                      "Pourquoi : tout dépend de tes flèches ratées ; à une toutes les 3 s, il tue 0,1 ennemi par seconde, moins que la "
                      "baliste qu'il remplace. Après : environ 0,5 / 0,8 / 1,5.",
    "Chaîne de forçats": "Partage 60 → 40 % (m_Share). Pourquoi : sur 5 liés, chaque dégât compte 3,4 fois (×3,1 en moyenne) : avec un "
                         "mortier au niveau 3 à côté, 5 tués par seconde. C'est le soutien le plus fort du jeu. Après : ×2,4 (×2,3 en "
                         "moyenne). Rien d'autre.",
    "Boussole folle": "Pointes 7 / 9 / 11 → 10 / 14 / 18 (m_SpikeDamagePerLevel). Le contrôle est très fort (6 à 29 s/s : la rose des "
                      "vents fait perdre 15 s à toute la voie toutes les 16 s), les dégâts faibles. Après : environ 1 / 1,4 / 1,7 tué par "
                      "seconde.",
    "Lanceur de baudruches": "Confettis 35 / 45 / 50 → 90 / 110 / 130 ; intervalle 7 / 6 / 6 → 6 / 5 / 5 s ; char de carnaval 60 → 100 "
                             "(m_ConfettiDamagePerLevel, m_IntervalPerLevel, m_FloatDamage). Pourquoi : il remplace un mortier au niveau "
                             "3 et une bulle, et ne tue que 0,24 ennemi par seconde. Après : environ 0,7 / 1,1 / 1,6.",
    "Boule à neige": "Fêlure 25 / 35 / 35 → 70 / 90 / 100 ; intervalle 9 / 8 / 8 → 7 / 6 / 6 s ; boule immense : 40 → 60 % des PV, "
                     "toutes les 18 → 12 s (m_CrackDamagePerLevel, m_IntervalPerLevel, m_GiantCrackShare, m_GiantInterval). Pourquoi : "
                     "sans ta flèche, presque aucun dégât (0,13 tué par seconde) ; le contrôle (le groupe enfermé) est bon. Après : "
                     "environ 0,5 / 1 / 1,6.",
    # Les trios
    "Enclume": "Dégâts 40 / 55 / 70 → 200 / 240 / 280 ; rayon de l'enclume 1,4 → 2,5 m ; intervalle 4 / 3,2 / 2,4 → 2,5 / 2,2 / 2 s "
               "(m_DamagePerLevel, m_SplashRadius, m_IntervalPerLevel). Pourquoi : elle remplace un Marteau-pilon et un mortier au niveau "
               "3 (environ 2 tués par seconde) et en tue 0,19 : c'est le trio le plus faible. Après : environ 3 / 4 / 6 tués par seconde.",
    "Gonfleur": "Tuyaux 1 / 2 / 2 → 3 / 4 / 5 ; intervalle 3,5 / 3 / 2,5 → 2,5 / 2 / 1,5 s ; harpon 15 / 20 / 25 → 40 / 50 / 60 ; "
                "hauteur 7 → 10 m (chute : 27,5 → 42,5 % des PV) ; défilé à 10 → 15 m (m_HosesPerLevel, m_IntervalPerLevel, "
                "m_HarpoonDamagePerLevel, m_FloatHeight, m_ParadeHeight). Pourquoi : 0,12 tué par seconde au niveau 1, la moitié du "
                "Lanceur de baudruches qu'il remplace. Après : environ 1 / 1,8 / 2,5 tués par seconde et 12 à 20 s/s de contrôle.",
    "Bowling": "Boule 45 / 60 / 75 → 150 / 200 / 250 (m_DamagePerLevel). Le contrôle est bon (11 s/s : les quilles reculent de 9 m), les "
               "dégâts très faibles pour un trio. Après : environ 1,9 / 3 / 4,5 tués par seconde.",
    "Beffroi": "Rembobinage 4 → 8 s ; intervalle 10 / 8,5 / 7 → 6 / 5 / 4 s ; figés 2 → 3 s ; Minuit toutes les 30 → 20 s "
               "(m_RewindSeconds, m_IntervalPerLevel, m_ArrivalStun, m_MidnightInterval) ; et la cloche blesse les rembobinés : "
               "60 / 90 / 120 (à coder). Pourquoi : aucun dégât et peu de contrôle (2,6 à 6 s/s, moins que la Boussole folle qu'il "
               "remplace). Après : environ 8 / 12 / 17 s/s.",
    "Colisée": "Le champion bloque les arènes : un seul à la fois au niveau 1, 30 s chacun, donc une arène toutes les 30 s. Champion "
               "30 → 12 s ; champions 1 / 2 / 2 → 2 / 3 / 4 ; intervalle 15 / 13 / 12 → 10 / 8 / 6 s ; coups du champion 30 / 40 / 50 → "
               "60 / 80 / 100 (m_ChampionTime, m_ChampionsPerLevel, m_IntervalPerLevel, m_ChampionDamagePerLevel). Après : environ "
               "1,4 / 2,6 / 4,3 tués par seconde.",
    "Grande bombarde": "Bombes 45 / 55 / 65 → 120 / 140 / 160 ; poudre par ennemi tué 8 → 12 % (m_BombDamagePerLevel, m_KillCharge). "
                       "Pourquoi : à 0,4 ennemi tué par seconde à portée, la salve part toutes les 31 s (16 s au niveau 2) : 0,4 tué par "
                       "seconde au niveau 1. Après : environ 1,6 / 4,5 / 6 (le bouquet final).",
    "Montagnes russes (Grappin)": "Grappin 15 / 20 / 25 → 40 / 50 / 60 ; fouet 20 → 40 ; jet plus haut (m_FlingLift 4,5 → 7 : une chute "
                                  "de 16 % des PV au lieu de 5 %) (m_PickDamagePerLevel, m_WhipDamage). Le contrôle est bon (8,5 à "
                                  "17 s/s), les dégâts faibles. Après : environ 0,6 / 1,6 / 2,1 tués par seconde.",
}


# Ce qui a été fait de chaque proposition, le 2 novembre, après la colonne du joueur (une case vide : celles de Claude, telles
# quelles) : Editor/BalanceBaker pour les réglages des prefabs, le code pour le reste (Docs/Refonte_TowerDefense.md, 12.35).
APPLIED = {
    "Barricade": "Rien n'a été changé (2 novembre).",
    "Boulet fou": "Rien n'a été changé (2 novembre).",
    "Piques": "Appliqué le 2 novembre, avec tes changements : l'intervalle 0,8 / 0,7 / 0,6 s, et −30 % sur tous ses dégâts (pointes, "
              "saignement, mine, mâchoire).",
    "Tour-aimant": "Appliqué le 2 novembre, avec tes changements : la singularité toutes les 10 s (pas 12), et une vraie texture pour le "
                   "trou noir (Tools/texture_trou_noir.py).",
    "Phare": "Appliqué le 2 novembre, avec tes changements : l'anneau de feu au milieu de la voie (entre les bords du NavMesh) et plus "
             "grand (2,6 → 4,4 m de diamètre).",
    "Feu d'artifice": "Appliqué le 2 novembre, à ta façon : ses dégâts ne changent pas ; l'éblouissement dure plus longtemps (2 / 2,2 / 3 → "
                      "2,5 / 2,8 / 3,6 s) et touche plus loin (5 → 6 m).",
    "Bowling": "Appliqué le 2 novembre, avec tes changements : la boule se met en place sous l'arche et attend 2 s ; ta flèche dedans "
               "pendant ce temps la lance tout de suite, 1,6 fois plus grosse et 1,75 fois plus forte.",
    "Colosse de forge (ultime)": "Ajouté le 2 novembre, tard (12.48) : les marteaux du Colosse d'avant (160 dans 6 m), la "
                                 "forge renforcée (les éclats 30 → 70 dans 3 m, le lingot 140 → 450 dans 7 m).",
    "Grand Manège (ultime)": "Ajouté le 2 novembre, tard (12.51), avant son premier essai : ses chiffres sont choisis pour qu'il vaille, "
                             "seul, un trio moyen au niveau 3, et avec deux flèches qui touchent par seconde, le Colosse (ses chevaux "
                             "frappent comme ses marteaux : 160 dans 6 m).",
    "Grande Horloge (ultime)": "Ajoutée le 2 novembre, tard (12.52), avant son premier essai : ses chiffres choisis pour qu'elle vaille, "
                               "seule, un trio moyen au niveau 3, et avec deux flèches qui touchent par seconde, le Colosse (ses échos "
                               "frappent comme ses marteaux : 160 dans 6 m).",
    "Beffroi": "Appliqué le 2 novembre, à ta façon : sans les dégâts de la cloche ; Minuit devient une grande bulle qui part du beffroi et "
               "couvre toute la carte (300 m), le monde en noir et blanc dedans, le temps arrêté 9 s.",
}


def build_all():
    buildings = [barricade(), piques(), tremplin(), baliste(), mortier(), givre(), filet(), aimant(), bulle(), phare(),
                 boulet_fou(), marteau_pilon(), toboggan(), yoyo(), avalanche(), feu_artifice(), sirenes(), loupe(), carquois(),
                 chaine(), boussole(), baudruches(), boule_neige(),
                 enclume(), gonfleur(), bowling(), beffroi(), colisee(), bombarde(), montagnes_russes(), colosse(), manege(), horloge()]
    for building in buildings:
        proposal = PROPOSALS.get(building.name, building.proposal)
        applied = APPLIED.get(building.name, "Appliqué le 2 novembre, tel quel.")
        building.proposal = applied + " La proposition : " + proposal if proposal else applied
    return buildings


# ------------------------------------------------------------------------------------------------ les vagues (comme le jeu)

WAVES = [2, 3, 4, 5, 8, 10, 12, 15]


def enemies():
    """Les ennemis des vagues (Data/Waves/WaveSettings.asset), avec leur définition."""
    settings_path = os.path.join(ASSETS, "Data", "Waves", "WaveSettings.asset")
    with open(settings_path, encoding="utf-8") as handle:
        text = handle.read()
    entries = re.findall(r"- prefab: \{fileID: -?\d+, guid: ([0-9a-f]{32}), type: 3\}\n\s+cost: (\S+)\n\s+firstWave: (\S+)\n\s+weight: (\S+)", text)
    prefabs = {}
    for folder, _, files in os.walk(os.path.join(ASSETS, "Prefabs")):
        for file in files:
            if file.endswith(".prefab.meta"):
                with open(os.path.join(folder, file), encoding="utf-8", errors="replace") as handle:
                    match = re.search(r"guid: ([0-9a-f]{32})", handle.read())
                if match:
                    prefabs[match.group(1)] = os.path.join(folder, file[:-5])
    definitions = {}
    for file in os.listdir(os.path.join(ASSETS, "Data", "Enemies")):
        if file.endswith(".asset.meta"):
            with open(os.path.join(ASSETS, "Data", "Enemies", file), encoding="utf-8") as handle:
                definitions[re.search(r"guid: ([0-9a-f]{32})", handle.read()).group(1)] = file[:-5]
    result = []
    for guid, cost, first, weight in entries:
        with open(prefabs[guid], encoding="utf-8", errors="replace") as handle:
            definition = re.search(r"m_Definition: \{fileID: \d+, guid: ([0-9a-f]{32})", handle.read()).group(1)
        name = definitions[definition]
        data = asset(os.path.join("Data", "Enemies", name))
        result.append(dict(name=data["displayName"], hp=data["maxHealth"], speed=data["moveSpeed"], damage=data["attackDamage"],
                           attack=data["attackInterval"], first=int(first), weight=float(weight), cost=float(cost)))
    return result


def scroll_values():
    """Les parchemins du catalogue (Data/Shop/ShopCatalog.asset) : force (21), adresse (22), vigueur (23), par parchemin."""
    with open(os.path.join(ASSETS, "Data", "Shop", "ShopCatalog.asset"), encoding="utf-8", errors="replace") as handle:
        text = handle.read()
    found = dict(re.findall(r"\n    effect: (2[123])\n    trigger: \d+\n    value: ([0-9.]+)", text))
    return {"force": float(found.get("21", 0.12)), "adresse": float(found.get("22", 0.1)), "vigueur": float(found.get("23", 25))}


class Waves:
    """La force des ennemis et des bâtiments à chaque vague (WaveManager.StartNextWave, Structure.Strength)."""

    def __init__(self):
        self.difficulty = asset(os.path.join("Data", "Difficulties", "Difficulty_Normal.asset"))
        self.settings = asset(os.path.join("Data", "Waves", "WaveSettings.asset"))
        self.enemies = enemies()
        self.exponent = float(self.settings.get("structureStrengthExponent", 0.0))
        self.scroll_share = float(self.settings.get("structureScrollShare", 1.0))
        self.scrolls = scroll_values()

    def health(self, wave):
        d, s = self.difficulty, self.settings
        growth = d["healthGrowthPerWave"] ** (wave - 1)
        if wave > s["wavesToWin"]:
            growth *= s["endlessHealthGrowth"] ** (wave - s["wavesToWin"])
        early = 1.0 if d["earlyStrength"] >= 1 or wave >= d["fullStrengthWave"] else \
            d["earlyStrength"] + (1 - d["earlyStrength"]) * min(1, max(0, (wave - 1) / max(1, d["fullStrengthWave"] - 1)))
        curve = s["healthPerWave"]
        value = 1.0 if (s["finalBossOnLastWave"] and wave == s["wavesToWin"]) else max(0.05, curve[min(wave, len(curve)) - 1])
        return growth * early * (1 + (value - 1) * d["waveCurve"])

    def strength(self, wave):
        scrolls = 1 + self.scrolls["force"] * self.scroll_share * SCROLLS_PER_WAVE * (wave - 1)
        return max(0.01, self.difficulty["enemyHealth"] * self.health(wave)) ** self.exponent * self.difficulty["structurePower"] * scrolls

    def rate(self, wave):
        return 1 + self.scrolls["adresse"] * self.scroll_share * SCROLLS_PER_WAVE * (wave - 1)

    def enemy_dps(self, wave):
        """Dégâts par seconde d'un ennemi type au corps à corps (moyenne pondérée, × la croissance des dégâts)."""
        pool = [e for e in self.enemies if e["first"] <= wave]
        base = sum(e["weight"] * e["damage"] / e["attack"] for e in pool) / sum(e["weight"] for e in pool)
        d, s = self.difficulty, self.settings
        growth = d["damageGrowthPerWave"] ** (wave - 1)
        if wave > s["wavesToWin"]:
            growth *= s["endlessDamageGrowth"] ** (wave - s["wavesToWin"])
        return base * growth

    def typical_hp(self, wave):
        pool = [e for e in self.enemies if e["first"] <= wave]
        base = sum(e["weight"] * e["hp"] for e in pool) / sum(e["weight"] for e in pool)
        return base * self.difficulty["enemyHealth"] * self.health(wave)


# ------------------------------------------------------------------------------------------------ le classeur

ARIAL = "Arial"
F_TITLE = Font(name=ARIAL, size=14, bold=True)
F_BOLD = Font(name=ARIAL, size=10, bold=True)
F_NORMAL = Font(name=ARIAL, size=10)
F_INPUT = Font(name=ARIAL, size=10, color="0000FF")
F_LINK = Font(name=ARIAL, size=10, color="008000")
F_SMALL = Font(name=ARIAL, size=9, italic=True, color="555555")
F_HEAD = Font(name=ARIAL, size=10, bold=True, color="FFFFFF")
FILL_HEAD = PatternFill("solid", fgColor="44546A")
FILL_GROUP = [PatternFill("solid", fgColor=c) for c in ("DDEBF7", "E2EFDA", "FCE4D6", "EDE7F6")]
FILL_DATA = PatternFill("solid", fgColor="F2F2F2")
FILL_PLAYER = PatternFill("solid", fgColor="FFFF00")
FILL_CLAUDE = PatternFill("solid", fgColor="FFF2CC")
FILL_NAME = PatternFill("solid", fgColor="D9E1F2")
THIN = Side(style="thin", color="BFBFBF")
BORDER = Border(left=THIN, right=THIN, top=THIN, bottom=THIN)
WRAP = Alignment(wrap_text=True, vertical="top")
CENTER = Alignment(horizontal="center", vertical="center", wrap_text=True)

VAGUES = "Vagues"
WAVE_ROW0 = 31          # la ligne de la vague w dans la feuille « Vagues » : WAVE_ROW0 + w


def wave_row(wave):
    return WAVE_ROW0 + wave


# Les colonnes des feuilles de bâtiments.
COL_NAME, COL_LEVEL, COL_PLANS, COL_DESC = 1, 2, 3, 4
COL_DPS1 = 5                       # 8 colonnes
COL_PCT1 = COL_DPS1 + 8            # 8 colonnes
COL_DPSZ = COL_PCT1 + 8            # 8 colonnes
COL_KILLZ = COL_DPSZ + 8           # 8 colonnes
COL_CC = COL_KILLZ + 8
COL_SUPPORT = COL_CC + 1
COL_PASS1 = COL_SUPPORT + 1        # un ennemi seul qui passe (vague 5)
COL_PASSZ = COL_PASS1 + 1          # un groupe de 5 qui passe (vague 5)
COL_REACH = COL_PASSZ + 1
COL_EXPO1 = COL_REACH + 1
COL_EXPOZ = COL_EXPO1 + 1
COL_D1, COL_P1, COL_DZ, COL_PZ = COL_EXPOZ + 1, COL_EXPOZ + 2, COL_EXPOZ + 3, COL_EXPOZ + 4
COL_CALC = COL_PZ + 1
LAST_COL = COL_CALC
PLAYER_LABEL_FROM, PLAYER_LABEL_TO = COL_DPS1, COL_DPS1 + 2
PLAYER_FROM, PLAYER_TO = COL_DPS1 + 3, COL_CC


def L(col):
    return get_column_letter(col)


def read_player_changes(path):
    """Les changements du joueur déjà écrits (relus pour ne pas les perdre en régénérant le fichier)."""
    changes = {}
    if not os.path.exists(path):
        return changes
    try:
        book = load_workbook(path)
    except Exception as error:  # fichier ouvert dans Excel, abîmé…
        print(f"Attention : impossible de relire {path} ({error}) ; les changements du joueur ne sont pas repris.")
        return changes
    for sheet in ("Tours", "Duos", "Trios"):
        if sheet not in book.sheetnames:
            continue
        ws = book[sheet]
        for row in range(1, ws.max_row + 1):
            if ws.cell(row, COL_NAME).value == "Équilibrage":
                name = ws.cell(row, COL_LEVEL).value
                value = ws.cell(row, PLAYER_FROM).value
                if name and value not in (None, ""):
                    changes[(sheet, name)] = value
    return changes


def write_vagues(wb, waves):
    ws = wb.create_sheet(VAGUES)
    ws["A1"] = "La force des ennemis et des bâtiments, vague par vague (Normal)"
    ws["A1"].font = F_TITLE
    ws["A2"] = ("Les réglages en bleu viennent des assets (relus par le script) ; on peut les changer ici pour voir l'effet d'un autre "
                "réglage : toutes les colonnes par vague des feuilles Tours, Duos et Trios suivent.")
    ws["A2"].font = F_SMALL
    d, s = waves.difficulty, waves.settings
    params = [
        ("enemyHealth (PV des ennemis de la difficulté)", d["enemyHealth"], "Difficulty_Normal"),
        ("healthGrowthPerWave (PV, par vague)", d["healthGrowthPerWave"], "Difficulty_Normal"),
        ("earlyStrength (force à la vague 1)", d["earlyStrength"], "Difficulty_Normal"),
        ("fullStrengthWave", d["fullStrengthWave"], "Difficulty_Normal"),
        ("waveCurve (part de la courbe vague par vague)", d["waveCurve"], "Difficulty_Normal"),
        ("structurePower (puissance des bâtiments)", d["structurePower"], "Difficulty_Normal (0,8 depuis le 1er novembre)"),
        ("structureStrengthExponent (suivi de la force)", waves.exponent, "WaveSettings (0,5 : la racine carrée, 12.31)"),
        ("wavesToWin", s["wavesToWin"], "WaveSettings"),
        ("finalBossOnLastWave (1 = oui)", s["finalBossOnLastWave"], "WaveSettings"),
        ("endlessHealthGrowth (PV, par vague du mode infini)", s["endlessHealthGrowth"], "WaveSettings"),
        ("speedGrowthPerWave", d["speedGrowthPerWave"], "Difficulty_Normal"),
        ("endlessSpeedGrowth", s["endlessSpeedGrowth"], "WaveSettings"),
        ("maxSpeedScale", s["maxSpeedScale"], "WaveSettings"),
        ("damageGrowthPerWave (dégâts des ennemis)", d["damageGrowthPerWave"], "Difficulty_Normal"),
        ("endlessDamageGrowth", s["endlessDamageGrowth"], "WaveSettings"),
        ("enemyCount (nombre d'ennemis de la difficulté)", d["enemyCount"], "Difficulty_Normal"),
        ("countGrowthPerWave (nombre d'ennemis, par vague)", d["countGrowthPerWave"], "Difficulty_Normal"),
        ("endlessCountGrowth", s["endlessCountGrowth"], "WaveSettings"),
        ("maxAlive (ennemis à la fois, à la vague 1)", s["maxAlive"], "WaveSettings"),
        ("maxAliveCap (plafond d'ennemis à la fois)", s["maxAliveCap"],
         "WaveSettings (150 depuis le 2 novembre, 40 avant : Editor/EnemyCapBaker)"),
        ("Parchemin de force : dégâts en plus, par parchemin", waves.scrolls["force"], "ShopCatalog (Force)"),
        ("Parchemin d'adresse : cadence en plus, par parchemin", waves.scrolls["adresse"], "ShopCatalog (Adresse : l'anneau du joueur)"),
        ("structureScrollShare (part des parchemins pour les bâtiments)", waves.scroll_share,
         "WaveSettings (1 : autant que le joueur ; 2 novembre)"),
    ]
    for i, (label, value, source) in enumerate(params):
        row = 4 + i
        ws.cell(row, 1, label).font = F_NORMAL
        cell = ws.cell(row, 2, value)
        cell.font = F_INPUT
        ws.cell(row, 3, source).font = F_SMALL
    # Lignes : 4 enemyHealth, 5 healthGrowth, 6 early, 7 fullWave, 8 waveCurve, 9 power, 10 exponent, 11 wavesToWin, 12 finalBoss,
    # 13 endlessHealth, 14 speedGrowth, 15 endlessSpeed, 16 maxSpeed, 17 damageGrowth, 18 endlessDamage, 19 enemyCount,
    # 20 countGrowth, 21 endlessCount, 22 maxAlive, 23 maxAliveCap, 24 force, 25 adresse, 26 part des parchemins ; les courbes
    # vague par vague : 28 (PV), 29 (nombre).
    ws.cell(28, 1, "healthPerWave (PV, vagues 1 à 5 ; la dernière vaut au-delà)").font = F_NORMAL
    for i, value in enumerate(s["healthPerWave"]):
        ws.cell(28, 2 + i, value).font = F_INPUT
    ws.cell(29, 1, "countPerWave (nombre, vagues 1 à 5 ; la dernière vaut au-delà)").font = F_NORMAL
    for i, value in enumerate(s["countPerWave"]):
        ws.cell(29, 2 + i, value).font = F_INPUT
    curve_last = L(1 + len(s["healthPerWave"]))
    count_last = L(1 + len(s["countPerWave"]))
    headers = ["Vague", "Croissance des PV", "Mode infini", "Début de partie", "Courbe vague par vague", "Santé (EnemyScaling.Health)",
               "Force des ennemis (× PV)", "Force des bâtiments (Strength)", "PV de l'ennemi type (base)", "PV de l'ennemi type (vague)",
               "Vitesse moyenne (m/s)", "Dégâts des ennemis (×)", "Ennemis à la fois (au plus)",
               "Parchemins de force du joueur (hypothèse)", "Parchemins d'adresse du joueur (hypothèse)", "Cadence des bâtiments (×)"]
    for i, header in enumerate(headers):
        cell = ws.cell(WAVE_ROW0, 1 + i, header)
        cell.font, cell.fill, cell.alignment, cell.border = F_HEAD, FILL_HEAD, CENTER, BORDER
    n = len(waves.enemies)
    e_first, e_weight, e_hp, e_speed = (f"Ennemis!$G$5:$G${4 + n}", f"Ennemis!$H$5:$H${4 + n}", f"Ennemis!$C$5:$C${4 + n}",
                                        f"Ennemis!$D$5:$D${4 + n}")
    for wave in range(1, 16):
        r = wave_row(wave)
        ws.cell(r, 1, wave).font = F_BOLD
        ws.cell(r, 2, f"=$B$5^(A{r}-1)")
        ws.cell(r, 3, f"=IF(A{r}>$B$11,$B$13^(A{r}-$B$11),1)")
        ws.cell(r, 4, f"=IF(OR($B$6>=1,A{r}>=$B$7),1,$B$6+(1-$B$6)*MIN(1,MAX(0,(A{r}-1)/MAX(1,$B$7-1))))")
        ws.cell(r, 5, f"=IF(AND($B$12=1,A{r}=$B$11),1,MAX(0.05,INDEX($B$28:${curve_last}$28,1,MIN(A{r},{len(s['healthPerWave'])}))))")
        ws.cell(r, 6, f"=B{r}*C{r}*D{r}*(1+(E{r}-1)*$B$8)")
        ws.cell(r, 7, f"=$B$4*F{r}")
        # Structure.Strength : la force des ennemis (exposant 0 depuis le 2 novembre), la puissance, les parchemins de force.
        ws.cell(r, 8, f"=MAX(0.01,G{r})^$B$10*$B$9*(1+$B$24*$B$26*N{r})")
        ws.cell(r, 9, f"=SUMPRODUCT(({e_first}<=A{r})*{e_weight}*{e_hp})/SUMPRODUCT(({e_first}<=A{r})*{e_weight})")
        ws.cell(r, 10, f"=I{r}*G{r}")
        ws.cell(r, 11, f"=SUMPRODUCT(({e_first}<=A{r})*{e_weight}*{e_speed})/SUMPRODUCT(({e_first}<=A{r})*{e_weight})"
                       f"*MIN($B$16,$B$14^(A{r}-1)*IF(A{r}>$B$11,$B$15^(A{r}-$B$11),1))")
        ws.cell(r, 12, f"=$B$17^(A{r}-1)*IF(A{r}>$B$11,$B$18^(A{r}-$B$11),1)")
        # WaveManager.MaxAlive : 12 × enemyCount × la racine carrée de la hausse du nombre d'ennemis, plafonné.
        ws.cell(r, 13, f"=MIN($B$23,MAX(1,ROUND($B$22*$B$19*SQRT($B$20^(A{r}-1)*IF(A{r}>$B$11,$B$21^(A{r}-$B$11),1)"
                       f"*(1+(MAX(0.05,INDEX($B$29:${count_last}$29,1,MIN(A{r},{len(s['countPerWave'])})))-1)*$B$8)),0)))")
        # Les parchemins du joueur avant cette vague (hypothèse : la moitié d'un de chaque par vague, coffres et boutique), et la
        # cadence des bâtiments qui en découle (Structure.ScrollRate).
        ws.cell(r, 14, SCROLLS_PER_WAVE * (wave - 1))
        ws.cell(r, 15, SCROLLS_PER_WAVE * (wave - 1))
        ws.cell(r, 16, f"=1+$B$25*$B$26*O{r}")
        for col in range(2, 17):
            cell = ws.cell(r, col)
            cell.font = F_INPUT if col in (14, 15) else F_NORMAL
            cell.border = BORDER
            cell.number_format = "0.00" if col not in (9, 10, 13, 14, 15) else ("0" if col == 13 else "0.0")
        ws.cell(r, 1).border = BORDER
        if wave in WAVES:
            for col in range(1, 17):
                ws.cell(r, col).fill = FILL_GROUP[0]
    ws.cell(WAVE_ROW0 + 17, 1, "En bleu clair : les vagues du tableau. La vague 6 est celle du boss final (ses ennemis ont la courbe à 1) ; "
                               "à partir de la vague 7, le mode infini (×1,25 par vague en plus). « Ennemis à la fois » : le maximum "
                               "d'ennemis vivants en même temps (WaveManager.MaxAlive), plafonné par maxAliveCap.").font = F_SMALL
    ws.column_dimensions["A"].width = 46
    for col in range(2, 17):
        ws.column_dimensions[L(col)].width = 14
    ws.column_dimensions["C"].width = 16
    ws.freeze_panes = ws.cell(WAVE_ROW0 + 1, 2)


def write_enemies(wb, waves):
    ws = wb.create_sheet("Ennemis")
    ws["A1"] = "Les ennemis des vagues (Data/Enemies, Data/Waves/WaveSettings)"
    ws["A1"].font = F_TITLE
    ws["A2"] = ("L'ennemi type d'une vague : la moyenne des PV de ceux qui peuvent y venir, pondérée par leur poids de tirage (WaveManager). "
                "Les boss n'y sont pas.")
    ws["A2"].font = F_SMALL
    headers = ["Ennemi", "", "PV max", "Vitesse (m/s)", "Dégâts", "Cadence (s)", "Première vague", "Poids", "Coût"]
    for i, header in enumerate(headers):
        cell = ws.cell(4, 1 + i, header)
        cell.font, cell.fill, cell.alignment, cell.border = F_HEAD, FILL_HEAD, CENTER, BORDER
    for i, e in enumerate(waves.enemies):
        r = 5 + i
        values = [e["name"], "", e["hp"], e["speed"], e["damage"], e["attack"], e["first"], e["weight"], e["cost"]]
        for j, value in enumerate(values):
            cell = ws.cell(r, 1 + j, value)
            cell.font = F_INPUT if j >= 2 else F_NORMAL
            cell.border = BORDER
    ws.column_dimensions["A"].width = 22
    ws.column_dimensions["B"].width = 2
    for col in range(3, 10):
        ws.column_dimensions[L(col)].width = 13


def wave_label(wave):
    return f"V{wave}"


def write_buildings(wb, sheet, title, buildings, waves, player_changes, fill):
    ws = wb.create_sheet(sheet)
    ws.cell(1, 1, title).font = F_TITLE
    ws.cell(2, 1, "Normal ; dégâts par seconde (DPS) à chaque vague, sans soutien du phare ni bonus du joueur. Groupe type : 5 ennemis "
                  "serrés (1,5 m de rayon). En bleu : les données de la ligne (calculées par le script depuis les prefabs) ; les colonnes "
                  "par vague sont des formules. Ligne « Équilibrage » : les changements proposés par Claude, puis les tiens (en jaune). "
                  "Méthode et hypothèses : feuille « Méthode ».").font = F_SMALL
    ws.merge_cells(start_row=2, start_column=1, end_row=2, end_column=COL_CC)
    ws.cell(2, 1).alignment = WRAP
    ws.row_dimensions[2].height = 40
    groups = [(COL_DPS1, "DPS sur une cible"), (COL_PCT1, "Part des PV d'un ennemi type, par seconde (1 cible)"),
              (COL_DPSZ, "DPS sur le groupe de 5 (total)"), (COL_KILLZ, "Ennemis types tués par seconde (groupe)")]
    for (col, label), group_fill in zip(groups, FILL_GROUP):
        ws.merge_cells(start_row=3, start_column=col, end_row=3, end_column=col + 7)
        cell = ws.cell(3, col, label)
        cell.font, cell.alignment, cell.fill = F_BOLD, CENTER, group_fill
        for i, wave in enumerate(WAVES):
            head = ws.cell(4, col + i, wave_label(wave))
            head.font, head.fill, head.alignment, head.border = F_HEAD, FILL_HEAD, CENTER, BORDER
    fixed = {COL_NAME: "Tour", COL_LEVEL: "Niv.", COL_PLANS: "Plans (cumulés)", COL_DESC: "Ce que fait la tour à ce niveau",
             COL_CC: "Contrôle (s de chemin perdues / s, groupe)", COL_SUPPORT: "Soutien (× dégâts des autres)",
             COL_PASS1: "Un ennemi seul qui passe : part de ses PV perdue (V5)",
             COL_PASSZ: "Un groupe de 5 qui passe : ennemis tués (V5)",
             COL_REACH: "Portée / zone", COL_EXPO1: "Exposition, 1 cible (s)", COL_EXPOZ: "Exposition, groupe (s)",
             COL_D1: "Dégâts de base / s (1 cible)",
             COL_P1: "PV max / s (1 cible)", COL_DZ: "Dégâts de base / s (groupe)", COL_PZ: "PV max / s (groupe)",
             COL_CALC: "Calcul (hypothèses de la ligne)"}
    if sheet != "Tours":
        fixed[COL_NAME] = "Duo" if sheet == "Duos" else "Trio"
        fixed[COL_DESC] = f"Ce que fait le {fixed[COL_NAME].lower()} à ce niveau"
    for col, label in fixed.items():
        ws.merge_cells(start_row=3, start_column=col, end_row=4, end_column=col)
        cell = ws.cell(3, col, label)
        cell.font, cell.fill, cell.alignment, cell.border = F_HEAD, FILL_HEAD, CENTER, BORDER
    ws.cell(3, COL_REACH).comment = Comment("Les colonnes « Portée » à « Calcul » sont les données de chaque ligne : les formules des "
                                            "colonnes par vague s'en servent.", "Claude")
    ws.row_dimensions[3].height = 30
    ws.row_dimensions[4].height = 42
    hp5 = f"{VAGUES}!$J${wave_row(5)}"
    row = 5
    rows_out = []
    for building in buildings:
        first = row
        for item in building.rows:
            values = {COL_NAME: building.name, COL_LEVEL: item.level, COL_PLANS: item.plans, COL_DESC: item.desc,
                      COL_REACH: item.reach, COL_EXPO1: round(item.expo1, 1), COL_EXPOZ: round(item.expoz, 1),
                      COL_D1: round(item.d1, 2), COL_P1: round(item.p1, 4),
                      COL_DZ: round(item.dz, 2), COL_PZ: round(item.pz, 4), COL_CALC: item.calc,
                      COL_CC: round(item.cc, 2), COL_SUPPORT: round(item.support, 2) if item.support else None}
            for col, value in values.items():
                cell = ws.cell(row, col, value)
                cell.font = F_INPUT if col in (COL_EXPO1, COL_EXPOZ, COL_D1, COL_P1, COL_DZ, COL_PZ, COL_CC, COL_SUPPORT) else F_NORMAL
            ws.cell(row, COL_NAME).font = F_BOLD if row == first else F_NORMAL
            for i, wave in enumerate(WAVES):
                strength = f"{VAGUES}!$H${wave_row(wave)}"
                hp = f"{VAGUES}!$J${wave_row(wave)}"
                rate = f"{VAGUES}!$P${wave_row(wave)}"
                ws.cell(row, COL_DPS1 + i, f"=(${L(COL_D1)}{row}*{strength}+${L(COL_P1)}{row}*{hp})*{rate}").number_format = "0.0"
                ws.cell(row, COL_PCT1 + i, f"={L(COL_DPS1 + i)}{row}/{hp}").number_format = "0.0%"
                ws.cell(row, COL_DPSZ + i, f"=(${L(COL_DZ)}{row}*{strength}+${L(COL_PZ)}{row}*{hp})*{rate}").number_format = "0.0"
                ws.cell(row, COL_KILLZ + i, f"={L(COL_DPSZ + i)}{row}/{hp}").number_format = "0.00"
            v5 = WAVES.index(5)
            ws.cell(row, COL_PASS1, f"=MIN(1,{L(COL_DPS1 + v5)}{row}*{L(COL_EXPO1)}{row}/{hp5})").number_format = "0%"
            ws.cell(row, COL_PASSZ, f"=MIN({GROUP},{L(COL_DPSZ + v5)}{row}*{L(COL_EXPOZ)}{row}/{hp5})").number_format = "0.0"
            ws.cell(row, COL_CC).number_format = "0.0"
            ws.cell(row, COL_SUPPORT).number_format = "0.00"
            ws.cell(row, COL_P1).number_format = "0.0%"
            ws.cell(row, COL_PZ).number_format = "0.0%"
            for col in range(1, LAST_COL + 1):
                cell = ws.cell(row, col)
                cell.border = BORDER
                if cell.font is None or cell.font == Font():
                    cell.font = F_NORMAL
                if col in (COL_DESC, COL_CALC, COL_REACH):
                    cell.alignment = WRAP
                elif col not in (COL_NAME,):
                    cell.alignment = Alignment(horizontal="center", vertical="top")
                else:
                    cell.alignment = Alignment(vertical="top")
                if col >= COL_REACH:
                    cell.fill = FILL_DATA
                if col == COL_NAME:
                    cell.fill = FILL_NAME
            for col in range(COL_DPS1, COL_PASSZ + 1):
                if ws.cell(row, col).font in (None, Font()):
                    ws.cell(row, col).font = F_NORMAL
            lines = max(len(item.desc) / 52, len(item.calc) / 60, 1)
            ws.row_dimensions[row].height = max(15, 13 * math.ceil(lines))
            rows_out.append((building, item, row))
            row += 1
        # La ligne « Équilibrage » : les changements proposés (Claude), puis ceux du joueur.
        ws.cell(row, COL_NAME, "Équilibrage").font = F_BOLD
        ws.cell(row, COL_LEVEL, building.name).font = F_SMALL
        ws.merge_cells(start_row=row, start_column=COL_LEVEL, end_row=row, end_column=COL_PLANS)
        claude = ws.cell(row, COL_DESC, ("Changements proposés (Claude) : " + building.proposal) if building.proposal else
                         "Changements proposés (Claude) : —")
        claude.font, claude.alignment, claude.fill = F_NORMAL, WRAP, FILL_CLAUDE
        ws.merge_cells(start_row=row, start_column=PLAYER_LABEL_FROM, end_row=row, end_column=PLAYER_LABEL_TO)
        label = ws.cell(row, PLAYER_LABEL_FROM, "Changements du joueur →")
        label.font, label.alignment = F_BOLD, CENTER
        ws.merge_cells(start_row=row, start_column=PLAYER_FROM, end_row=row, end_column=PLAYER_TO)
        player = ws.cell(row, PLAYER_FROM, player_changes.get((sheet, building.name)))
        player.font, player.alignment = F_NORMAL, WRAP
        for col in range(PLAYER_FROM, PLAYER_TO + 1):
            ws.cell(row, col).fill = FILL_PLAYER
        for col in range(1, PLAYER_TO + 1):
            ws.cell(row, col).border = BORDER
        ws.cell(row, COL_NAME).fill = FILL_NAME
        ws.cell(row, COL_NAME).alignment = Alignment(vertical="top")
        ws.cell(row, COL_LEVEL).alignment = Alignment(vertical="top", wrap_text=True)
        ws.row_dimensions[row].height = max(30, 13 * math.ceil(len(claude.value) / 52))
        row += 2
    widths = {COL_NAME: 20, COL_LEVEL: 5, COL_PLANS: 7, COL_DESC: 52, COL_CC: 11, COL_SUPPORT: 10, COL_PASS1: 11, COL_PASSZ: 11,
              COL_REACH: 14, COL_EXPO1: 9, COL_EXPOZ: 9, COL_D1: 9, COL_P1: 9, COL_DZ: 9, COL_PZ: 9, COL_CALC: 60}
    for col in range(1, LAST_COL + 1):
        ws.column_dimensions[L(col)].width = widths.get(col, 7.5)
    ws.freeze_panes = ws.cell(5, COL_DPS1)
    last = row - 1
    for col in (COL_PCT1, COL_KILLZ):
        ws.conditional_formatting.add(f"{L(col)}5:{L(col + 7)}{last}",
                                      ColorScaleRule(start_type="num", start_value=0, start_color="FFFFFF",
                                                     mid_type="percentile", mid_value=70, mid_color="FFE699",
                                                     end_type="percentile", end_value=97, end_color="F8696B"))
    ws.conditional_formatting.add(f"{L(COL_CC)}5:{L(COL_CC)}{last}",
                                  ColorScaleRule(start_type="num", start_value=0, start_color="FFFFFF",
                                                 end_type="percentile", end_value=95, end_color="9BC2E6"))
    return rows_out


def write_summary(wb, placed):
    ws = wb.create_sheet("Synthèse", 1)
    ws["A1"] = "Synthèse : toutes les tours, duos et trios, niveau par niveau"
    ws["A1"].font = F_TITLE
    ws["A2"] = ("Liens vers les feuilles Tours, Duos et Trios (en vert). Pour comparer : la part des PV d'un ennemi type par seconde (une "
                "cible), les ennemis types tués par seconde sur le groupe de 5, et ce même chiffre par plan dépensé. Trier ou filtrer "
                "avec les flèches de l'en-tête.")
    ws["A2"].font = F_SMALL
    ws.merge_cells("A2:N2")
    ws["A2"].alignment = WRAP
    ws.row_dimensions[2].height = 30
    headers = ["Famille", "Bâtiment", "Niv.", "Plans", "1 cible : % PV / s (V5)", "1 cible : % PV / s (V10)", "Groupe : tués / s (V5)",
               "Groupe : tués / s (V10)", "Un groupe de 5 qui passe : tués (V5)", "Le même, par plan", "Contrôle (s / s)",
               "Soutien (×)", "Un ennemi seul qui passe : % PV (V5)", "Né de"]
    for i, header in enumerate(headers):
        cell = ws.cell(4, 1 + i, header)
        cell.font, cell.fill, cell.alignment, cell.border = F_HEAD, FILL_HEAD, CENTER, BORDER
    ws.row_dimensions[4].height = 45
    r = 5
    i5, i10 = WAVES.index(5), WAVES.index(10)
    for sheet, building, item, src in placed:
        link = lambda col: f"='{sheet}'!{L(col)}{src}"
        values = [building.family, building.name, item.level, link(COL_PLANS), link(COL_PCT1 + i5), link(COL_PCT1 + i10),
                  link(COL_KILLZ + i5), link(COL_KILLZ + i10), link(COL_PASSZ), f"=I{r}/D{r}", link(COL_CC),
                  f"=IF('{sheet}'!{L(COL_SUPPORT)}{src}=\"\",\"\",'{sheet}'!{L(COL_SUPPORT)}{src})", link(COL_PASS1), building.origin]
        for j, value in enumerate(values):
            cell = ws.cell(r, 1 + j, value)
            cell.font = F_LINK if isinstance(value, str) and value.startswith("='") else F_NORMAL
            cell.border = BORDER
            cell.alignment = Alignment(horizontal="center") if 2 <= j <= 12 else Alignment()
        ws.cell(r, 5).number_format = ws.cell(r, 6).number_format = "0.0%"
        for col in (7, 8, 11):
            ws.cell(r, col).number_format = "0.00"
        ws.cell(r, 9).number_format = "0.00"
        ws.cell(r, 10).number_format = "0.000"
        ws.cell(r, 12).number_format = "0.00"
        ws.cell(r, 13).number_format = "0%"
        r += 1
    last = r - 1
    for col in ("E", "F", "G", "H", "I", "J"):
        ws.conditional_formatting.add(f"{col}5:{col}{last}",
                                      ColorScaleRule(start_type="num", start_value=0, start_color="FFFFFF",
                                                     mid_type="percentile", mid_value=60, mid_color="FFE699",
                                                     end_type="percentile", end_value=95, end_color="F8696B"))
    ws.conditional_formatting.add(f"K5:K{last}", ColorScaleRule(start_type="num", start_value=0, start_color="FFFFFF",
                                                                end_type="percentile", end_value=95, end_color="9BC2E6"))
    ws.auto_filter.ref = f"A4:N{last}"
    widths = [8, 26, 5, 6, 12, 12, 12, 12, 13, 13, 11, 9, 12, 30]
    for i, width in enumerate(widths):
        ws.column_dimensions[L(1 + i)].width = width
    ws.freeze_panes = "C5"


METHOD = [
    ("Le tableau d'équilibrage", None),
    ("Demandé par le joueur le 1er novembre au soir (Docs/Refonte_TowerDefense.md, 12.32), fait par Claude le 2 novembre : les 10 tours "
     "(5 niveaux), les 13 duos et les 7 trios (3 niveaux). Écrit par Tools/equilibrage_tours.py, qui lit les prefabs : relancer le script "
     "après avoir changé une valeur d'un prefab (py Tools/equilibrage_tours.py, depuis Project Archery/). Ta colonne « Changements du "
     "joueur » est gardée d'une fois sur l'autre.", None),
    ("Les feuilles", None),
    ("Synthèse : une ligne par niveau de chaque bâtiment, les chiffres qui comparent (vagues 5 et 10), triables. Tours, Duos, Trios : le "
     "détail, une ligne par niveau, puis la ligne « Équilibrage » (les changements proposés par Claude, en jaune pâle ; les tiens, en "
     "jaune). Vagues : la force des bâtiments, les PV de l'ennemi type et le nombre d'ennemis à la fois, à chaque vague, en formules. Ennemis : les PV, vitesses et poids "
     "de tirage.", None),
    ("Les formules", None),
    ("Force des bâtiments à la vague v (Structure.Strength) = (enemyHealth × Santé(v)) ^ e × structurePower (0,8 en Normal) × (1 + 0,12 "
     "× parchemins de force du joueur) ; e = 0 depuis le 2 novembre (0,5 avant) : les bâtiments ne suivent plus les vagues. La cadence "
     "des bâtiments = 1 + 0,1 × parchemins d'adresse ; elle multiplie tous les DPS. "
     "Santé(v) = 1,15^(v−1) × 1,25^(v−6 en mode infini) × la courbe vague par vague (0,8 / 1,03 / 1,42 / 1,62 / 1,8, puis 1,8 ; 1 à la "
     "vague 6, celle du boss final). Remarque : le tableau de 12.31 oubliait cette courbe au-delà de la vague 6 (×1,8) : à la vague 10, "
     "les ennemis ont ×15,5 PV, pas ×8,6.", None),
    ("DPS à la vague v = (dégâts de base par seconde × Force(v) + part des PV max par seconde × PV de l'ennemi type(v)) × Cadence(v). "
     "La première partie suit les parchemins de force, la seconde (chutes, éclats de glace, le choc du Boulet fou…) suit les PV des "
     "ennemis : c'est elle qui ne décroche pas.", None),
    ("Part des PV d'un ennemi type par seconde = DPS / PV de l'ennemi type (la moyenne des PV de ceux qui peuvent venir à cette vague, "
     "pondérée par leur poids de tirage). Ennemis types tués par seconde = DPS sur le groupe / PV de l'ennemi type.", None),
    ("Les hypothèses", None),
    ("Groupe type : 5 ennemis serrés, dans 1,5 m de rayon, qui restent à portée (renouvelés s'ils meurent ou s'en vont). Une explosion "
     "de 2,5 m et plus les touche tous (à 1 m du centre en moyenne : les dégâts au bord sont comptés) ; un carreau perforant en traverse 2.",
     None),
    ("Une cible : un seul ennemi à portée. Pour les tours qui éjectent ou tuent leur cible (tremplin, Boulet fou, Toboggan…), un nouvel "
     "ennemi arrive toutes les 2 s au plus ; sur le groupe, le débit est plafonné à 2 ennemis par seconde (un groupe de 5 qui passe en "
     "2,5 s).", None),
    ("Contrôle : les secondes de chemin que la tour fait perdre aux ennemis, par seconde (ralentir de 50 % un ennemi pendant 1 s = 0,5 ; "
     "l'étourdir 1 s = 1 ; le renvoyer de 10 m en arrière = 4 s à 2,5 m/s). Les auras (givre, aimant…) comptent les 5 du groupe ; les "
     "tours qui éjectent comptent chaque ennemi éjecté (le chiffre peut dépasser 5). Vitesse de référence : 2,5 m/s.", None),
    ("Exposition : le temps qu'un ennemi passe à portée en marchant (la voie à 5 m d'une tour à distance ; la longueur d'un piège), à la "
     "vitesse moyenne de la vague 5. « Un ennemi seul qui passe » : la part de ses PV qu'il perd pendant ce temps, à la vague 5 ; c'est ce "
     "qui compare les pièges (forts mais brefs) aux tours à distance (faibles mais longues).", None),
    ("Parchemins : le joueur en gagne en moyenne la moitié d'un de force et la moitié d'un d'adresse par vague (le coffre de vague et "
     "la boutique) : 2 de chaque avant la vague 5, 4,5 avant la vague 10. Une hypothèse, à changer dans la feuille « Vagues » (colonnes "
     "en bleu) pour voir une run riche ou pauvre en parchemins.", None),
    ("Plans cumulés (2 novembre au soir) : une tour, 1 plan par niveau (1 à 5) ; un duo naît de deux tours au niveau 3 (6 plans), "
     "puis 2 par niveau (6, 8, 10) ; un trio, d'un duo au niveau 1 et d'une tour au niveau 3, puis 3 par niveau (9, 12, 15).", None),
    ("Ce qui n'est pas compté", None),
    ("Le soutien du phare (+20 à +40 % de dégâts, la cadence : sa colonne « Soutien ») et celui de la Chaîne de forçats ; les "
     "parchemins de vigueur (les PV de la tour et des barricades) ; tout ce que fait le joueur : ses flèches, l'ennemi préparé (12.12 : il ne compte que pour ses "
     "coups), les flèches perforantes, enflammées, courbées, la mèche de la Grande bombarde (comptée seulement par l'hypothèse de 0,4 "
     "ennemi tué par seconde), les flèches ratées du Carquois géant (une toutes les 3 s) ; les chocs entre ennemis projetés (8 % et 4 % des "
     "PV) ; les boss (la plupart des tours ne leur font qu'une part de leurs PV) ; les volants (sauf pour le lance-filet).", None),
    ("Les chiffres de chaque ligne sont des estimations (les cycles, le nombre d'ennemis touchés) : la colonne « Calcul » dit lesquelles. "
     "Ils servent à comparer les tours entre elles ; rien ne remplace l'essai au casque.", None),
    ("Ce que dit le tableau (Claude, 2 novembre)", None),
    ("1. Les fusions font perdre de la force. Un duo naît de deux tours au niveau 3 et un trio d'un duo et d'une tour au niveau 3, mais la "
     "plupart font moins de dégâts que ce qu'ils remplacent : le Marteau-pilon au niveau 1 tue 0,23 ennemi par seconde, la barricade et "
     "les piques au niveau 3 en tuaient 4 ; l'Enclume, 0,19 à la place d'un mortier au niveau 3 (1,7). Seuls le Boulet fou, le Toboggan, "
     "l'Avalanche et la Boussole folle s'en sortent, par leur contrôle. Règle proposée : un duo au niveau 1 vaut au moins ses deux tours "
     "au niveau 3, un trio au niveau 1 au moins son duo et sa tour ; les propositions de chaque ligne « Équilibrage » visent cela.", None),
    ("2. La rampe de lancement (Tremplin, niveau 5) tue chaque ennemi lancé, quelle que soit la vague : elle ne décroche jamais, quand "
     "toutes les autres tours suivent la racine carrée de la force des ennemis (12.31). En mode infini, elle fait tout.", None),
    ("3. Entre les tours : la baliste est trop faible aux niveaux 1 et 2 (moins qu'un mortier, même sur une cible) puis triple au niveau 5 "
     "(le titan) ; le mortier fait un bond au niveau 3 (deux tubes et des dégâts en hausse) ; les niveaux 4 de l'aimant, de la bulle et "
     "du phare n'aident que les flèches du joueur ; le lance-filet ne fait presque rien au sol, même aux niveaux 4 et 5.", None),
    ("Le plafond d'ennemis à la fois passe de 40 à 150 (2 novembre, le joueur ; Editor/EnemyCapBaker). Les chiffres des tours n'en "
     "dépendent pas (ils comptent un groupe de 5) ; mais en Normal, à partir de la vague 13, il y a plus d'ennemis en même temps (56 à "
     "la vague 15 au lieu de 40, 108 à la vague 20, 150 à partir de la vague 23 : feuille « Vagues ») : des groupes plus gros et plus "
     "serrés, ce qui avantage les tours de zone, les pièges et la rampe, et fait baisser la part des tours d'une seule cible.", None),
    ("4. Un défaut : le verglas de la tour de givre (niveau 4) fait glisser l'ennemi en avant, dans le sens de sa marche : il gagne du "
     "chemin.", None),
    ("5. Une erreur dans le tableau de 12.31 (Refonte_TowerDefense.md) : la courbe vague par vague (×1,8) vaut aussi au-delà de la vague "
     "6. Les ennemis ont ×7,5 PV à la vague 8, ×15,5 à la vague 10, ×32 à la vague 12 (pas ×8,6 à la vague 10) ; à la vague 10, un "
     "bâtiment ne fait donc plus que 23 % de la part de leurs PV qu'il faisait à la vague 1 (où ils ont ×0,8), pas 34 %.", None),
    ("Ce qui a été appliqué (2 novembre)", None),
    ("Après la colonne du joueur (une case vide : les changements de Claude, tels quels), tout est appliqué : les réglages des prefabs "
     "par Editor/BalanceBaker (une fois, seulement les valeurs encore à l'ancienne), le reste dans le code : le verglas vers la forêt, "
     "le ralentissement de l'aimant au niveau 4, la texture du trou noir, l'anneau du phare centré et plus grand, la baliste de la Loupe "
     "dès le niveau 1, le tir seul du Carquois géant, 5 passagers au Toboggan niveau 3, la boule du Bowling qui se met en place, la bulle "
     "de Minuit du Beffroi. Les chiffres de ce tableau sont ceux des prefabs au moment où il a été écrit : après le passage dans Unity "
     "(le baker), relancer le script pour les voir. Chaque ligne « Équilibrage » dit ce qui a été fait, puis la proposition de départ.",
     None),
    ("Les deux roguelikes liés (2 novembre)", None),
    ("Le joueur : « on joue deux roguelikes en un », l'arc et les tourelles, sans lien ; il choisit de les lier, avec son idée : les "
     "parchemins valent aussi pour les bâtiments (force : +12 % de dégâts, adresse : +10 % de cadence, vigueur : +12,5 % de PV à la tour et "
     "aux barricades, comme les +25 PV sur 200 du joueur), et les tourelles ne suivent plus les vagues. Un bâtiment ne grandit plus "
     "qu'avec ses niveaux, ses fusions et les parchemins du joueur : il prend du retard sur les ennemis vague après vague (×15 PV à la "
     "vague 10), sauf ce qui compte en part de leurs PV (chutes, glace, éjections mortelles). Réglages : Wave Settings, Structure Strength "
     "Exponent (0) et Structure Scroll Share (1).", None),
    ("Légende", None),
    ("Texte bleu : une donnée (lue dans les assets, ou calculée par le script) ; texte noir : une formule ; texte vert : un lien vers une "
     "autre feuille ; fond jaune : à remplir par le joueur ; fond jaune pâle : les propositions de Claude.", None),
]


def write_method(wb):
    ws = wb.active
    ws.title = "Méthode"
    ws.column_dimensions["A"].width = 130
    row = 1
    for text, _ in METHOD:
        cell = ws.cell(row, 1, text)
        is_title = len(text) < 40
        cell.font = F_TITLE if row == 1 else (F_BOLD if is_title else F_NORMAL)
        cell.alignment = WRAP
        if not is_title:
            ws.row_dimensions[row].height = 13 * math.ceil(len(text) / 125) + 4
        row += 2 if is_title and row > 1 else 1
        if is_title and row == 2:
            row += 1


def summary_print(buildings, waves):
    """Pour l'équilibrage : les chiffres qui comparent, aux vagues 5 et 10."""
    print(f"{'Bâtiment':26} niv pl  1c%PV/s V5  V10  grp tués/s V5  V10  passe:1c%  grp  /plan  contrôle /plan  soutien")
    for b in buildings:
        for item in b.rows:
            out = []
            for wave in (5, 10):
                s, hp = waves.strength(wave), waves.typical_hp(wave)
                k = waves.rate(wave)
                out.append(((item.d1 * s + item.p1 * hp) * k / hp, (item.dz * s + item.pz * hp) * k / hp))
            pass1 = min(1.0, out[0][0] * item.expo1)
            passz = min(GROUP, out[0][1] * item.expoz)
            print(f"{b.name[:26]:26} {item.level:3} {item.plans:2}  {out[0][0] * 100:6.1f}% {out[1][0] * 100:6.1f}%  {out[0][1]:6.2f} "
                  f"{out[1][1]:5.2f}   {pass1 * 100:5.0f}% {passz:5.2f} {passz / item.plans:5.2f}  {item.cc:6.1f} {item.cc / item.plans:5.2f}"
                  f"  {item.support or 0:5.2f}")


WAVES_DATA = None


def main():
    global WAVES_DATA
    waves = WAVES_DATA = Waves()
    buildings = build_all()
    if "--print" in sys.argv:
        for wave in WAVES:
            print(f"Vague {wave:2} : force des bâtiments {waves.strength(wave):5.2f}, PV de l'ennemi type {waves.typical_hp(wave):7.1f}")
        summary_print(buildings, waves)
        return
    player_changes = read_player_changes(OUTPUT)
    wb = Workbook()
    write_method(wb)
    placed = []
    for sheet, title in (("Tours", "Les 10 tours, niveau par niveau"), ("Duos", "Les 13 duos, niveau par niveau"),
                         ("Trios", "Les 7 trios, niveau par niveau, et les ultimes (le Colosse de forge, le Grand Manège, la Grande Horloge)")):
        group = [b for b in buildings if b.family == sheet]
        for building, item, row in write_buildings(wb, sheet, title, group, waves, player_changes, None):
            placed.append((sheet, building, item, row))
    write_summary(wb, placed)
    write_vagues(wb, waves)
    write_enemies(wb, waves)
    # openpyxl n'écrit pas les valeurs des formules : Excel les calcule à l'ouverture.
    wb.calculation.fullCalcOnLoad = True
    wb.save(OUTPUT)
    print(f"Écrit : {OUTPUT} ({len(placed)} niveaux, {len(player_changes)} changement(s) du joueur repris).")


if __name__ == "__main__":
    main()
