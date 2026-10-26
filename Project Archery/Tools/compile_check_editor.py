"""Vérifie que les outils d'éditeur (Assets/_Project/Editor : les bakers) compilent, sans ouvrir Unity.

Ils dépendent du code du jeu dans sa version éditeur (Archery.Runtime avec UNITY_EDITOR) : ce script réutilise la DLL
que compile_check.py vient de compiler. Lancer donc d'abord compile_check.py, puis celui-ci (26 octobre : il servait
depuis le 25 octobre dans le dossier temporaire de Claude, rangé ici pour reprendre le projet sur un autre compte).

Usage (depuis « Project Archery ») :
    py Tools/compile_check.py
    py Tools/compile_check_editor.py
"""
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import compile_check as cc  # noqa: E402


def runtime_dll():
    """La DLL d'Archery.Runtime version éditeur, laissée par compile_check.py (None si absente)."""
    found = None
    for root, _, files in os.walk(os.path.join(cc.WORK, "bin")):
        if "Archery.Runtime.InEditor.dll" in files:
            found = os.path.join(root, "Archery.Runtime.InEditor.dll")
        elif "Archery.Runtime.dll" in files and "InEditor" in root:
            found = os.path.join(root, "Archery.Runtime.dll")
    return found


def main():
    dll = runtime_dll()
    if dll is None:
        print("Archery.Runtime (version éditeur) introuvable : lance d'abord py Tools/compile_check.py")
        return 1

    source = open(os.path.join(cc.PROJECT, "Assembly-CSharp.csproj"), encoding="utf-8-sig").read()
    defines = re.search(r"<DefineConstants>(.*?)</DefineConstants>", source, re.S).group(1).strip()
    defines = ";".join([defines] + cc.version_defines())
    refs = [(name, cc.absolute(path))
            for name, path in re.findall(r'<Reference Include="([^"]+)">\s*<HintPath>(.*?)</HintPath>', source, re.S)]
    refs += cc.asmdef_references({name for name, _ in refs})

    print("== Outils d'éditeur (Assets/_Project/Editor), contre " + os.path.basename(dll) + " ==")
    editor_dir = os.path.join(os.path.dirname(cc.SCRIPTS), "Editor")
    project = cc.write_project("Archery.ProjectEditor", refs + [("Archery.Runtime", dll)], defines,
                               os.path.join(editor_dir, "**", "*.cs"))
    return 0 if cc.build(project) else 1


if __name__ == "__main__":
    sys.exit(main())
