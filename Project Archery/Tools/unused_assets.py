"""Liste les assets que rien n'utilise, sans ouvrir Unity (25 octobre 2026 ; rapport : Docs/Fichiers_Inutilises.md).

Unity relie les assets par leur GUID (écrit dans le .meta de chacun) : une scène cite ses prefabs, un prefab ses matériaux,
un matériau ses textures et son shader. Le script part de ce que le jeu utilise à coup sûr, suit ces liens, et liste ce
qu'il n'a jamais atteint. Points de départ :
  - les scènes cochées dans Build Settings ;
  - tout ce que citent les réglages du projet (ProjectSettings : URP, XR, Input System…) ;
  - les dossiers Resources et StreamingAssets (le code les charge par leur nom) ;
  - les chemins et les noms de fichiers écrits entre guillemets dans le code (les bakers), les shaders de Shader.Find ;
  - les outils d'éditeur du jeu, et les scripts du jeu qui se lancent tout seuls (InitializeOnLoad,
    RuntimeInitializeOnLoadMethod…). Un autre script compte s'il est posé sur un objet atteint ou cité par un script atteint.

Un asset inutilisé n'est pas dans le build (Unity n'y met que ce que les scènes et Resources utilisent) : le supprimer
allège le projet (clonage, import, dossier Library), pas le jeu.

Limites : ce qui est chargé par un nom construit dans le code (« Hologramme " + couleur ») ou par un outil hors de Unity
(Presentation/generer_presentation.py) paraît inutilisé. Vérifier avant de supprimer.

Usage (depuis « Project Archery ») :
  python Tools/unused_assets.py                 le total par dossier
  python Tools/unused_assets.py --liste x.txt   en plus, la liste complète des fichiers inutilisés dans x.txt
"""
import os
import re
import sys
from collections import defaultdict, deque

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HEX = re.compile(rb"(?<![0-9a-f])[0-9a-f]{32}(?![0-9a-f])")
GUID_META = re.compile(rb"^guid: ([0-9a-f]{32})", re.M)
# Fichiers lus par le compilateur ou un importeur, sans lien par GUID : jamais listés.
INFRA = (".asmdef", ".asmref", ".rsp", ".mtl")
IMPLICIT = re.compile(r"InitializeOnLoad|RuntimeInitializeOnLoadMethod|MenuItem|AssetPostprocessor|CustomEditor|"
                      r"ScriptedImporter|CustomPropertyDrawer|EditorWindow|DidReloadScripts")


def read(path):
    with open(os.path.join(PROJECT, path), "rb") as f:
        return f.read()


def strip_comments(code):
    code = re.sub(r"/\*.*?\*/", " ", code, flags=re.S)
    return re.sub(r"//[^\n]*", " ", code)


def scan():
    guid_to_path, path_to_guid = {}, {}
    for dirpath, _, names in os.walk(os.path.join(PROJECT, "Assets")):
        for name in names:
            if name.endswith(".meta"):
                continue
            full = os.path.join(dirpath, name)
            if not os.path.exists(full + ".meta"):
                continue
            m = GUID_META.search(open(full + ".meta", "rb").read())
            if m:
                rel = os.path.relpath(full, PROJECT).replace("\\", "/")
                guid_to_path[m.group(1).decode()] = rel
                path_to_guid[rel] = m.group(1).decode()
    return guid_to_path, path_to_guid


def main():
    guid_to_path, path_to_guid = scan()
    known = set(guid_to_path)

    def refs(data):
        return {h.decode() for h in HEX.findall(data)} & known

    # Les liens : chaque GUID connu cité par un fichier texte (scène, prefab, matériau…) ou par son .meta.
    edges = defaultdict(set)
    code = {}
    for rel, g in path_to_guid.items():
        edges[g] |= refs(read(rel + ".meta")) - {g}
        if rel.endswith(".cs"):
            code[rel] = read(rel).decode("utf-8", "replace")
            continue
        if os.path.getsize(os.path.join(PROJECT, rel)) < 80 * 1024 * 1024:
            data = read(rel)
            if b"\0" not in data[:8192]:
                edges[g] |= refs(data) - {g}

    reached = set()
    queue = deque()

    def reach(g):
        if g in known and g not in reached:
            reached.add(g)
            queue.append(g)

    build = read("ProjectSettings/EditorBuildSettings.asset").decode("utf-8")
    for m in re.finditer(r"- enabled: 1\n\s+path: .+\n\s+guid: ([0-9a-f]{32})", build):
        reach(m.group(1))
    settings = os.path.join(PROJECT, "ProjectSettings")
    for name in os.listdir(settings):
        if name.endswith(".asset"):
            for g in refs(open(os.path.join(settings, name), "rb").read()):
                reach(g)
    for rel, g in path_to_guid.items():
        folders = rel.split("/")[:-1]
        if "Resources" in folders or "StreamingAssets" in folders:
            reach(g)

    # Les chemins et les noms de fichiers cités dans le code.
    quoted = set()
    for text in code.values():
        quoted |= set(re.findall(r'"([^"\n]{3,160})"', text))
    shaders = {}
    for rel in path_to_guid:
        if rel.endswith(".shader"):
            m = re.search(r'Shader\s+"([^"]+)"', read(rel).decode("utf-8", "replace"))
            if m:
                shaders[m.group(1)] = rel
    by_name = defaultdict(list)
    for rel in path_to_guid:
        by_name[rel.rsplit("/", 1)[-1]].append(rel)
    for q in quoted:
        if q in path_to_guid:
            reach(path_to_guid[q])
        if q in shaders:
            reach(path_to_guid[shaders[q]])
        tail = q.lstrip("/").rsplit("/", 1)[-1]
        for rel in by_name.get(tail, ()):
            if rel.endswith(q.lstrip("/")):
                reach(path_to_guid[rel])

    # Les scripts : ceux qui se lancent tout seuls, puis, de proche en proche, ceux qu'un script atteint cite par le nom de
    # leurs types.
    declares = defaultdict(set)
    for rel, text in code.items():
        for t in re.findall(r"\b(?:class|struct|enum|interface)\s+(\w+)", text):
            declares[t].add(rel)
        if rel.startswith("Assets/_Project/") and (IMPLICIT.search(text) or "/Editor/" in rel):
            reach(path_to_guid[rel])
    done_code = set()
    while queue:
        while queue:
            g = queue.popleft()
            for h in edges.get(g, ()):
                reach(h)
        for rel, text in code.items():
            if rel in done_code or path_to_guid[rel] not in reached:
                continue
            done_code.add(rel)
            for word in set(re.findall(r"\b[A-Za-z_]\w+\b", strip_comments(text))) & declares.keys():
                for other in declares[word]:
                    reach(path_to_guid[other])

    # Les fichiers inclus par un shader (#include) : par leur nom.
    includes = set()
    for rel in path_to_guid:
        if rel.endswith((".shader", ".hlsl", ".cginc", ".compute", ".shadergraph", ".shadersubgraph")):
            includes |= {i.rsplit("/", 1)[-1] for i in re.findall(r'#include\s+"([^"]+)"', read(rel).decode("utf-8", "replace"))}

    unused = [rel for rel, g in path_to_guid.items()
              if g not in reached and not rel.endswith(INFRA) and rel.rsplit("/", 1)[-1] not in includes]

    def group(rel):
        parts = rel.split("/")
        depth = 3 if parts[1] in ("_Project", "Samples") and len(parts) > 3 else 2
        return "/".join(parts[1:depth]) if len(parts) > 2 else "(racine d'Assets)"

    totals = defaultdict(lambda: [0, 0, 0, 0])
    unused_set = set(unused)
    for rel in path_to_guid:
        size = os.path.getsize(os.path.join(PROJECT, rel))
        t = totals[group(rel)]
        if rel in unused_set:
            t[2] += 1
            t[3] += size
        else:
            t[0] += 1
            t[1] += size
    print(f"{'Dossier':45} {'utilisés':>18} {'inutilisés':>20}")
    for name, (n_used, s_used, n_un, s_un) in sorted(totals.items(), key=lambda x: -x[1][3]):
        if n_un:
            print(f"{name:45} {n_used:5d} ({s_used / 1e6:6.1f} Mo) {n_un:6d} ({s_un / 1e6:6.1f} Mo)"
                  + ("  entièrement" if n_used == 0 else ""))
    print(f"\n{len(unused)} fichiers inutilisés sur {len(path_to_guid)}, "
          f"{sum(t[3] for t in totals.values()) / 1e6:.1f} Mo.")

    if "--liste" in sys.argv:
        out = sys.argv[sys.argv.index("--liste") + 1]
        with open(out, "w", encoding="utf-8", newline="\n") as f:
            for rel in sorted(unused):
                f.write(f"{os.path.getsize(os.path.join(PROJECT, rel)) / 1e3:10.0f} Ko  {rel}\n")
        print("Liste complète :", out)


if __name__ == "__main__":
    main()
