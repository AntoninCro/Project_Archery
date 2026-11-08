"""Cherche, dans les scripts de Assets/_Project/Scripts, un champ (m_...) qui porte le même nom qu'un champ d'une classe parente.

Unity le refuse, même pour des champs privés : dans l'éditeur, il les sérialise au rechargement des scripts (« The same field name is
serialized multiple times in the class or its parent class », vu le 3 novembre sur les ultimes : m_ArenaScale dans TowerUltimate et
dans chaque ultime, m_BoostUntil dans Structure et dans le Manège). compile_check.py ne le voit pas : le C# l'accepte. Le corriger en
renommant le champ qui ne porte pas de réglage (sinon, [FormerlySerializedAs] pour garder les valeurs des prefabs).

Lecture simple : les classes « class X : Y » et les champs d'instance au premier niveau du corps de la classe (8 espaces), hors
static et const ; les classes partielles mettent leurs champs ensemble.
Usage (depuis Project Archery/) : py Tools/champs_en_double.py
"""
import glob
import os
import re
import sys

ROOT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "_Project", "Scripts")
CLASS = re.compile(r"^\s*(?:public |internal |protected |private |sealed |abstract |partial |static )*class (\w+)(?:\s*:\s*([\w\.]+))?", re.M)
TOP = re.compile(r"^    (?:public |internal |sealed |abstract |partial )*class (\w+)", re.M)
FIELD = re.compile(r"^        (?!.*\bstatic\b)(?!.*\bconst\b)(?:\[[^\]]*\]\s*)*(?:(?:public|protected|private|internal|readonly)\s+)*"
                   r"[\w<>\[\],\.\? ]+?\s+(m_\w+)\s*(?:=[^;]*)?;", re.M)

fields, parent = {}, {}
for path in glob.glob(os.path.join(ROOT, "**", "*.cs"), recursive=True):
    text = open(path, encoding="utf-8-sig").read()
    for match in CLASS.finditer(text):
        fields.setdefault(match.group(1), set())
        if match.group(2):
            parent.setdefault(match.group(1), match.group(2).split(".")[-1])
    top = TOP.search(text)
    if top:
        fields[top.group(1)].update(f.group(1) for f in FIELD.finditer(text))


def ancestors(name):
    seen = []
    while name in parent and parent[name] not in seen:
        name = parent[name]
        seen.append(name)
    return seen


found = 0
for name in sorted(fields):
    for ancestor in ancestors(name):
        for field in sorted(fields[name] & fields.get(ancestor, set())):
            print("%s.%s : aussi dans %s" % (name, field, ancestor))
            found += 1

print("%d champ(s) en double" % found if found else "aucun champ en double")
sys.exit(1 if found else 0)
