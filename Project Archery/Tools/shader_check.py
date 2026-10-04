"""Vérifie la syntaxe HLSL des shaders de Assets/_Project/Shaders, sans ouvrir Unity.

Compile le code entre HLSLPROGRAM et ENDHLSL avec le compilateur Direct3D fourni avec Unity
(D3DCompiler_47.dll), pour le vertex et le fragment shader. Les includes d'URP sont remplacés
par quelques définitions minimales : la vérification porte sur notre code, pas sur URP.

Usage : python Tools/shader_check.py   (Windows, avec la version d'Unity du projet installée par Unity Hub)
"""
import ctypes
import glob
import os
import re
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SHADERS = os.path.join(PROJECT, "Assets", "_Project", "Shaders")

STUBS = """
#define half float
#define half2 float2
#define half3 float3
#define half4 float4
#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };
#define UNITY_VERTEX_INPUT_INSTANCE_ID
#define UNITY_VERTEX_OUTPUT_STEREO
#define UNITY_SETUP_INSTANCE_ID(x)
#define UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(x)
#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(x)
float4 _Time;
float4x4 _StubViewProjection;
float4x4 _StubObjectToWorld;
float3 _WorldSpaceCameraPos;
float4 TransformObjectToHClip(float3 positionOS) { return mul(_StubViewProjection, float4(positionOS, 1.0)); }
float3 TransformObjectToWorld(float3 positionOS) { return mul(_StubObjectToWorld, float4(positionOS, 1.0)).xyz; }
float4 TransformWorldToHClip(float3 positionWS) { return mul(_StubViewProjection, float4(positionWS, 1.0)); }
float3 TransformObjectToWorldNormal(float3 normalOS) { return normalize(mul((float3x3)_StubObjectToWorld, normalOS)); }
float3 GetWorldSpaceViewDir(float3 positionWS) { return _WorldSpaceCameraPos - positionWS; }
"""


def compiler_path():
    with open(os.path.join(PROJECT, "ProjectSettings", "ProjectVersion.txt"), encoding="utf-8") as file:
        version = re.search(r"m_EditorVersion: (\S+)", file.read()).group(1)
    return os.path.join(os.environ.get("ProgramFiles", r"C:\Program Files"), "Unity", "Hub", "Editor",
                        version, "Editor", "Data", "Tools", "D3DCompiler_47.dll")


def blob_text(blob):
    # Interface ID3DBlob : QueryInterface, AddRef, Release, GetBufferPointer, GetBufferSize.
    vtable = ctypes.cast(ctypes.cast(blob, ctypes.POINTER(ctypes.c_void_p))[0], ctypes.POINTER(ctypes.c_void_p))
    get_pointer = ctypes.WINFUNCTYPE(ctypes.c_void_p, ctypes.c_void_p)(vtable[3])
    get_size = ctypes.WINFUNCTYPE(ctypes.c_size_t, ctypes.c_void_p)(vtable[4])
    return ctypes.string_at(get_pointer(blob), get_size(blob)).decode("utf-8", "replace").strip()


def compile_hlsl(compiler, source, entry, target):
    code = ctypes.c_void_p()
    errors = ctypes.c_void_p()
    data = source.encode("utf-8")
    result = compiler.D3DCompile(data, len(data), b"shader.hlsl", None, None, entry.encode(), target.encode(),
                                 0, 0, ctypes.byref(code), ctypes.byref(errors))
    return result == 0, blob_text(errors) if errors.value else ""


def main():
    path = compiler_path()
    if not os.path.isfile(path):
        print(f"Compilateur introuvable : {path}")
        return 1

    compiler = ctypes.WinDLL(path)
    success = True
    for shader in sorted(glob.glob(os.path.join(SHADERS, "*.shader"))):
        with open(shader, encoding="utf-8") as file:
            text = file.read()
        for block in re.findall(r"HLSLPROGRAM(.*?)ENDHLSL", text, re.S):
            body = "\n".join(line for line in block.splitlines()
                             if not line.strip().startswith(("#pragma", "#include")))
            for kind, target in (("vertex", "vs_5_0"), ("fragment", "ps_5_0")):
                entry = re.search(rf"#pragma {kind} (\w+)", block).group(1)
                ok, message = compile_hlsl(compiler, STUBS + body, entry, target)
                success &= ok
                print(f"{os.path.basename(shader)} : {entry} ({target}) {'ok' if ok else 'ERREUR'}")
                if message:
                    print("  " + message.replace("\n", "\n  "))
    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())
