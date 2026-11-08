# -*- coding: utf-8 -*-
"""
La fiche des améliorations (8 novembre au soir) : Docs/Fiche_Ameliorations.xlsx, écrite d'après le catalogue de la boutique
(Assets/_Project/Data/Shop/ShopCatalog.asset), pour que le joueur choisisse la rareté de chaque pièce de l'arc parmi les quatre
(commune, rare, épique, légendaire ; l'épique est nouvelle). Une ligne par pièce, avec sa rareté actuelle et deux colonnes jaunes
pour lui : « Nouvelle rareté » (une liste) et « Tes modifications ». Ensuite, Claude lit la fiche et l'applique par un baker.

Relancer le script garde ce que le joueur a écrit dans ses colonnes (par le code de la pièce).

    py Tools/fiche_ameliorations.py
"""
import os
import re
import sys

import openpyxl
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.worksheet.datavalidation import DataValidation

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CATALOG = os.path.join(ROOT, "Assets", "_Project", "Data", "Shop", "ShopCatalog.asset")
UPGRADE_CS = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Upgrades", "Upgrade.cs")
OUT = os.path.join(ROOT, "Docs", "Fiche_Ameliorations.xlsx")

RARITIES = {0: "Commune", 1: "Rare", 3: "Épique", 2: "Légendaire"}   # le numéro enregistré dans l'asset
RARITY_ORDER = ["Commune", "Rare", "Épique", "Légendaire"]
RARITY_FILL = {"Commune": "EDEDED", "Rare": "CFE0FF", "Épique": "E6D4FF", "Légendaire": "FFE9B0"}
SLOTS = {0: "—", 1: "Pointe", 2: "Empennage", 3: "Fût", 4: "Corde", 5: "Talisman"}
KIND_OBJECT, KIND_SCROLL, KIND_RETIRED = 0, 1, 2

# Les parchemins après le baker du 8 novembre au soir (Editor/RaretesBaker) : la valeur de la commune.
SCROLLS = [
    ("Strength", "Force", "+{v:.3g} % de dégâts (pour toi et les tours), anneau {h:.3g} % plus rapide", 15.0),
    ("Dexterity", "Adresse", "Bande verte {v:.3g} % plus large, tirs parfaits +{v:.3g} % de dégâts ; les tours +{v:.3g} % de cadence", 10.0),
    ("Vigor", "Vigueur", "+{v:.3g} PV max, flèches {v:.3g} % plus loin ; la tour +{v:.3g} % de PV", 17.5),
]
SCALES = [("Commune", 1.0), ("Rare", 1.5), ("Épique", 2.0), ("Légendaire", 3.0)]

YELLOW = PatternFill("solid", fgColor="FFF59D")
HEADER = PatternFill("solid", fgColor="3E2F1C")
TITLE = Font(bold=True, size=14)
WHITE = Font(bold=True, color="FFFFFF")
WRAP = Alignment(wrap_text=True, vertical="top")


def effect_names():
    """Les noms des effets, dans l'ordre de l'énumération UpgradeEffect (enregistrée par son numéro)."""
    src = open(UPGRADE_CS, encoding="utf-8").read()
    body = src.split("enum UpgradeEffect")[1].split("\n    }")[0]
    body = re.sub(r"//.*", "", body)
    return re.findall(r"^\s*([A-Za-z_]\w*)\s*,", body, flags=re.M)


def decode(raw):
    """Une valeur YAML d'Unity : entre guillemets doubles (échappements \\xNN, \\uNNNN, lignes repliées), simples, ou nue."""
    raw = raw.strip()
    if raw.startswith('"'):
        inner = raw[1:raw.rindex('"')]
        inner = re.sub(r"\s*\n\s*", " ", inner)

        def unescape(m):
            code = m.group(0)
            if code.startswith("\\x"):
                return chr(int(code[2:], 16))
            if code.startswith("\\u"):
                return chr(int(code[2:], 16))
            return {"\\n": "\n", '\\"': '"', "\\\\": "\\", "\\t": "\t"}.get(code, code[1:])

        return re.sub(r"\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\.", unescape, inner)
    if raw.startswith("'"):
        inner = raw[1:raw.rindex("'")]
        return re.sub(r"\s*\n\s*", " ", inner).replace("''", "'")
    return re.sub(r"\s*\n\s*", " ", raw)


def read_catalog():
    text = open(CATALOG, encoding="utf-8").read().replace("\r\n", "\n")
    part = text.split("\n  upgrades:\n", 1)[1]
    part = re.split(r"\n  [A-Za-z]", part, maxsplit=1)[0]
    entries = []
    for chunk in part.split("\n  - ")[1:] if not part.startswith("  - ") else ("\n" + part).split("\n  - ")[1:]:
        fields = {}
        key = None
        for line in ("    " + chunk).split("\n"):
            m = re.match(r"^    ([A-Za-z]\w*): ?(.*)$", line)
            if m:
                key = m.group(1)
                fields[key] = m.group(2)
            elif key is not None:
                fields[key] += "\n" + line
        entries.append(fields)
    return entries


def readable(text):
    """« {50 %|100 %|150 %} » devient « 50 % / 100 % / 150 % » ; « ➶ » reste."""
    return re.sub(r"\{([^{}]*)\}", lambda m: " / ".join(m.group(1).split("|")), text or "")


def previous_player_columns():
    if not os.path.exists(OUT):
        return {}
    kept = {}
    wb = openpyxl.load_workbook(OUT)
    if "Pièces" not in wb.sheetnames:
        return {}
    ws = wb["Pièces"]
    header = None
    for row in ws.iter_rows(values_only=True):
        if row and row[0] == "Code":
            header = list(row)
            continue
        if header and row and row[0]:
            data = dict(zip(header, row))
            kept[row[0]] = (data.get("Nouvelle rareté"), data.get("Tes modifications"))
    return kept


def main():
    names = effect_names()
    entries = read_catalog()
    kept = previous_player_columns()

    wb = openpyxl.Workbook()
    guide = wb.active
    guide.title = "Mode d'emploi"
    lines = [
        ("THUNK VR — la fiche des améliorations (8 novembre au soir)", TITLE),
        ("", None),
        ("Les quatre raretés, partout : commune (blanc), rare (bleu), épique (violet, nouvelle), légendaire (or).", None),
        ("1. Onglet « Pièces » : pour chaque pièce de l'arc, choisis sa rareté dans la colonne jaune « Nouvelle rareté » (une liste).", None),
        ("   Laisse la case vide si la rareté actuelle te convient. Aucune pièce n'est encore épique : à toi de les répartir.", None),
        ("2. « Tes modifications » : tout autre changement (texte, valeurs…), en clair.", None),
        ("3. Onglets « Raretés » et « Parchemins » : les règles et les valeurs, pour mémoire (écris tes remarques à droite).", None),
        ("4. Enregistre, puis dis à Claude de lire la fiche : il l'applique au catalogue par un baker (dans Unity, une fois).", None),
        ("", None),
        ("Relancer « py Tools/fiche_ameliorations.py » refait la fiche d'après le catalogue en gardant tes colonnes jaunes.", None),
    ]
    for i, (line, font) in enumerate(lines, start=1):
        cell = guide.cell(row=i, column=1, value=line)
        if font:
            cell.font = font
    guide.column_dimensions["A"].width = 130

    # ---- les pièces
    ws = wb.create_sheet("Pièces")
    ws.cell(row=1, column=1, value="Les pièces de l'arc").font = TITLE
    ws.cell(row=2, column=1, value="Une ligne par pièce proposée (boutique, coffres). Colonnes jaunes : tes choix.")
    headers = ["Code", "Emplacement", "Pièce", "Texte de la carte (niveaux 1 / 2 / 3)", "Rareté actuelle", "Nouvelle rareté",
               "Tes modifications"]
    for c, h in enumerate(headers, start=1):
        cell = ws.cell(row=4, column=c, value=h)
        cell.fill = HEADER
        cell.font = WHITE
    rows = []
    for e in entries:
        kind = int(e.get("kind", "0").strip() or 0)
        if kind != KIND_OBJECT:
            continue
        effect = int(e.get("effect", "0").strip() or 0)
        code = names[effect] if effect < len(names) else str(effect)
        slot = int(e.get("slot", "0").strip() or 0)
        rarity = RARITIES.get(int(e.get("rarity", "0").strip() or 0), "Commune")
        rows.append((slot, RARITY_ORDER.index(rarity), code, SLOTS.get(slot, "?"), decode(e.get("displayName", "")),
                     readable(decode(e.get("description", ""))), rarity))
    rows.sort(key=lambda r: (r[0] if r[0] else 9, r[1], r[4]))
    validation = DataValidation(type="list", formula1='"' + ",".join(RARITY_ORDER) + '"', allow_blank=True)
    ws.add_data_validation(validation)
    r = 5
    for _, _, code, slot, name, text, rarity in rows:
        new_rarity, notes = kept.get(code, (None, None))
        values = [code, slot, name, text, rarity, new_rarity, notes]
        for c, v in enumerate(values, start=1):
            cell = ws.cell(row=r, column=c, value=v)
            cell.alignment = WRAP
        ws.cell(row=r, column=5).fill = PatternFill("solid", fgColor=RARITY_FILL[rarity])
        ws.cell(row=r, column=6).fill = YELLOW
        ws.cell(row=r, column=7).fill = YELLOW
        validation.add(ws.cell(row=r, column=6))
        r += 1
    for col, width in zip("ABCDEFG", (16, 12, 26, 70, 14, 16, 40)):
        ws.column_dimensions[col].width = width
    ws.freeze_panes = "A5"

    # Le compte par rareté et par emplacement.
    r += 1
    ws.cell(row=r, column=1, value="Combien par rareté (actuelle)").font = Font(bold=True)
    r += 1
    counts = {}
    for row in rows:
        counts.setdefault(row[3], {}).setdefault(row[6], 0)
        counts[row[3]][row[6]] += 1
    ws.cell(row=r, column=1, value="Emplacement").font = Font(bold=True)
    for c, rarity in enumerate(RARITY_ORDER, start=2):
        ws.cell(row=r, column=c, value=rarity).font = Font(bold=True)
    for slot in sorted(counts, key=lambda s: list(SLOTS.values()).index(s) if s in SLOTS.values() else 99):
        r += 1
        ws.cell(row=r, column=1, value=slot)
        for c, rarity in enumerate(RARITY_ORDER, start=2):
            ws.cell(row=r, column=c, value=counts[slot].get(rarity, 0))

    # ---- les raretés
    rs = wb.create_sheet("Raretés")
    rs.cell(row=1, column=1, value="Les quatre raretés (8 novembre au soir)").font = TITLE
    table = [
        ("Rareté", "Couleur", "Chance d'un objet (et d'un parchemin)", "Tourelle (plans)", "Parchemin (effet)", "Tes remarques"),
        ("Commune", "Blanc", "60 %", "1 plan", "×1", None),
        ("Rare", "Bleu", "24 %", "2 plans", "×1,5", None),
        ("Épique", "Violet", "11 %", "3 plans", "×2", None),
        ("Légendaire", "Or", "5 %", "4 plans", "×3", None),
    ]
    for i, row in enumerate(table, start=3):
        for c, v in enumerate(row, start=1):
            cell = rs.cell(row=i, column=c, value=v)
            if i == 3:
                cell.fill = HEADER
                cell.font = WHITE
            elif c == 1:
                cell.fill = PatternFill("solid", fgColor=RARITY_FILL[v])
            if c == 6 and i > 3:
                cell.fill = YELLOW
    notes = [
        "La chance (la pièce Chance) fait monter les rares, les épiques et les légendaires ; les communes gardent au moins 40 %.",
        "Le nombre de plans d'une tourelle (coffres et pause) dépend aussi de la vague (plus souvent beaucoup en fin de partie).",
        "Le gong : appeler le boss tôt rend la pause suivante plus rare d'un cran (les objets, les parchemins ; les tourelles, un plan de plus).",
        "Un objet tiré dans une rareté où son emplacement n'a rien prend une autre rareté.",
    ]
    for i, note in enumerate(notes, start=9):
        rs.cell(row=i, column=1, value=note)
    for col, width in zip("ABCDEF", (14, 10, 34, 16, 16, 40)):
        rs.column_dimensions[col].width = width

    # ---- les parchemins
    ps = wb.create_sheet("Parchemins")
    ps.cell(row=1, column=1, value="Les trois parchemins, selon leur rareté").font = TITLE
    ps.cell(row=2, column=1, value="Valeurs de base divisées par deux le 8 novembre au soir ; la rareté les multiplie. On en prend un à "
                                   "chaque pause et dans les coffres.")
    header = ["Code", "Parchemin"] + [name for name, _ in SCALES] + ["Tes remarques"]
    for c, h in enumerate(header, start=1):
        cell = ps.cell(row=4, column=c, value=h)
        cell.fill = HEADER
        cell.font = WHITE
    for i, (code, name, text, base) in enumerate(SCROLLS, start=5):
        ps.cell(row=i, column=1, value=code)
        ps.cell(row=i, column=2, value=name)
        for c, (_, scale) in enumerate(SCALES, start=3):
            v = base * scale
            cell = ps.cell(row=i, column=c, value=text.format(v=v, h=v / 2))
            cell.alignment = WRAP
        ps.cell(row=i, column=7).fill = YELLOW
    for col, width in zip("ABCDEFG", (12, 12, 30, 30, 30, 30, 36)):
        ps.column_dimensions[col].width = width

    wb.save(OUT)
    print(f"Écrit : {OUT} ({len(rows)} pièces ; {sum(1 for v in kept.values() if any(v))} ligne(s) du joueur reprise(s)).")


if __name__ == "__main__":
    sys.exit(main())
