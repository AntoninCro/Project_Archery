"""Vérifie que les scripts de Assets/_Project compilent, sans ouvrir Unity.

Compile les scripts avec le SDK .NET, contre les mêmes DLL qu'Unity
(références lues dans Assembly-CSharp.csproj, généré par Unity) :
  - Archery.Runtime : comme un build joueur (sans UnityEditor) ;
  - Archery.Editor : les outils d'éditeur, s'il y a un dossier Scripts/Editor.

Usage : python Tools/compile_check.py   (Unity doit avoir généré les .csproj au moins une fois)
"""
import json
import os
import re
import subprocess
import sys
import tempfile

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPTS = os.path.join(PROJECT, "Assets", "_Project", "Scripts")
WORK = os.path.join(tempfile.gettempdir(), "archery_compile_check")
IGNORED = ("NETSDK", "MSB3277", "MSB3539")


def absolute(path):
    return path if os.path.isabs(path) else os.path.join(PROJECT, path)


def is_editor_dll(path):
    name = os.path.basename(path)
    return name.startswith("UnityEditor") or ".Editor" in name


def write_project(name, references, defines, include, exclude=""):
    items = "\n".join(
        f'    <Reference Include="{n}"><HintPath>{p}</HintPath><Private>False</Private></Reference>'
        for n, p in references)
    exclude_attribute = f' Exclude="{exclude}"' if exclude else ""
    output = os.path.join(WORK, "bin", name) + os.sep
    intermediate = os.path.join(WORK, "obj", name) + os.sep
    xml = f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <NoStdLib>true</NoStdLib>
    <NoConfig>true</NoConfig>
    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <AssemblyName>{name}</AssemblyName>
    <DefineConstants>{defines}</DefineConstants>
    <OutputPath>{output}</OutputPath>
    <BaseIntermediateOutputPath>{intermediate}</BaseIntermediateOutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <NoWarn>0169;0649</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="{include}"{exclude_attribute} />
  </ItemGroup>
  <ItemGroup>
{items}
  </ItemGroup>
</Project>
"""
    path = os.path.join(WORK, name, name + ".csproj")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as file:
        file.write(xml)
    return path


def build(path):
    result = subprocess.run(["dotnet", "build", path, "-nologo", "-v:q", "-clp:NoSummary"],
                            capture_output=True, text=True)
    messages = []
    for line in (result.stdout + result.stderr).splitlines():
        if ("error" in line or "warning" in line) and not any(code in line for code in IGNORED):
            line = re.sub(r"\s*\[[^\]]*\.csproj\]$", "", line.replace(SCRIPTS + os.sep, ""))
            if line not in messages:
                messages.append(line)
    print("\n".join(messages) if messages else "(aucune erreur ni avertissement)")
    return result.returncode == 0


def version_defines():
    """Defines des « Version Defines » de l'asmdef, pour les paquets installés (manifest.json)."""
    with open(os.path.join(SCRIPTS, "Archery.Runtime.asmdef"), encoding="utf-8-sig") as file:
        asmdef = json.load(file)
    with open(os.path.join(PROJECT, "Packages", "manifest.json"), encoding="utf-8-sig") as file:
        installed = json.load(file).get("dependencies", {})
    return [entry["define"] for entry in asmdef.get("versionDefines", []) if entry["name"] in installed]


def main():
    csproj = os.path.join(PROJECT, "Assembly-CSharp.csproj")
    with open(csproj, encoding="utf-8-sig") as file:
        source = file.read()

    defines = re.search(r"<DefineConstants>(.*?)</DefineConstants>", source, re.S).group(1).strip()
    defines = ";".join([defines] + version_defines())
    references = [(n, absolute(p)) for n, p in re.findall(r'<Reference Include="([^"]+)">\s*<HintPath>(.*?)</HintPath>', source, re.S)]
    runtime_references = [(n, p) for n, p in references if not is_editor_dll(p)]
    runtime_defines = ";".join(d for d in defines.split(";") if not d.startswith("UNITY_EDITOR"))

    print("== Archery.Runtime (comme un build joueur) ==")
    runtime = write_project("Archery.Runtime", runtime_references, runtime_defines,
                            os.path.join(SCRIPTS, "**", "*.cs"), os.path.join(SCRIPTS, "Editor", "**"))
    if not build(runtime):
        return 1

    if not os.path.isdir(os.path.join(SCRIPTS, "Editor")):
        return 0

    print("== Archery.Editor ==")
    runtime_dll = os.path.join(WORK, "bin", "Archery.Runtime", "Archery.Runtime.dll")
    editor = write_project("Archery.Editor", references + [("Archery.Runtime", runtime_dll)], defines,
                           os.path.join(SCRIPTS, "Editor", "**", "*.cs"))
    return 0 if build(editor) else 1


if __name__ == "__main__":
    sys.exit(main())
