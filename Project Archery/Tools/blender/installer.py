# Installe l'extension THUNK et la bibliothèque des kits dans les préférences de Blender du joueur (14 novembre ;
# Docs/Guide_Blender.md) : le dossier Tools/blender dans les « Script Directories » (l'extension suit alors le dépôt : rien à
# réinstaller quand Claude la change, il suffit de redémarrer Blender), l'extension « THUNK VR » cochée, la bibliothèque
# Modeles_Blender/Bibliotheque dans les « Asset Libraries ». Blender doit être fermé (il réécrirait ses préférences en quittant).
# Depuis « Project Archery » :
#   blender --background --python Tools/blender/installer.py
# Pour défaire : Edit > Preferences > Add-ons (décocher THUNK VR), File Paths (retirer les deux chemins).
import bpy, os

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, '..', '..'))
LIB = os.path.join(PROJECT, 'Modeles_Blender', 'Bibliotheque')
prefs = bpy.context.preferences


def same(a, b):
    return os.path.normcase(os.path.abspath(bpy.path.abspath(a))) == os.path.normcase(os.path.abspath(b))


dirs = prefs.filepaths.script_directories
if not any(same(d.directory, HERE) for d in dirs):
    d = dirs.new()
    d.name = 'THUNK VR'
    d.directory = HERE
    print('THUNK : dossier des scripts ajouté (%s)' % HERE)
bpy.utils.refresh_script_paths()
r = bpy.ops.preferences.addon_enable(module='thunk_blender')
print('THUNK : extension THUNK VR %s' % ('cochée' if 'FINISHED' in r else 'non cochée (%s)' % r))

libs = prefs.filepaths.asset_libraries
lib = next((l for l in libs if same(l.path, LIB)), None)
if os.path.isdir(LIB) and lib is None:
    try:
        lib = libs.new(name='THUNK Kits', directory=LIB)
    except Exception:
        bpy.ops.preferences.asset_library_add(directory=LIB)
        lib = libs[-1]
        lib.name = 'THUNK Kits'
    print('THUNK : bibliothèque des kits ajoutée (%s)' % LIB)
# « Ajouter » (une vraie copie, modifiable) : Blender 5 « empaquette » par défaut, et une pièce empaquetée ne se modifie pas
if lib is not None and lib.import_method != 'APPEND':
    lib.import_method = 'APPEND'
    print('THUNK : bibliothèque des kits en « Ajouter »')

bpy.ops.wm.save_userpref()
print('THUNK : préférences enregistrées.')
