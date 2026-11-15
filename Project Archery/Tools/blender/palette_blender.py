# Les matériaux de la palette du joueur (14 novembre ; Docs/Refonte_Visuelle.md, « Les couleurs ») dans la bibliothèque de
# Blender : Modeles_Blender/Bibliotheque/Palette.blend, catalogue « Palette ». Lancé par Tools/refonte_visuelle/palette.py
# (matieres), d'après palette_roles.json (les choix du joueur dans Modeles_Blender/Palette.txt). Un matériau par rôle, à la
# couleur choisie ; « Transparent… » transparent (Alpha donné, 0,55 sinon), « …Lueur… » lumineux. L'extension THUNK remet
# ces couleurs à tout matériau du même nom dans un fichier de bâtiment (le nom suffit).
import bpy, json, os, sys, uuid

data = json.load(open(sys.argv[sys.argv.index('--') + 1], encoding='utf-8'))
lib = os.path.dirname(os.path.abspath(sys.argv[sys.argv.index('--') + 1]))
out = os.path.join(lib, 'Palette.blend')
CATALOG = str(uuid.uuid5(uuid.NAMESPACE_URL, 'thunk-vr/bibliotheque/Palette'))


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def catalogs():
    path = os.path.join(lib, 'blender_assets.cats.txt')
    lines = open(path, encoding='utf-8').read().splitlines() if os.path.exists(path) else ['VERSION 1', '']
    if not any(l.startswith(CATALOG) for l in lines):
        lines.append('%s:Palette:Palette' % CATALOG)
        open(path, 'w', encoding='utf-8').write('\n'.join(lines) + '\n')


bpy.ops.wm.read_factory_settings(use_empty=True)
catalogs()
for i, r in enumerate(data['roles']):
    h = r['couleur'].lstrip('#')
    rgb = [srgb_to_linear(int(h[k:k + 2], 16) / 255.0) for k in (0, 2, 4)]
    m = bpy.data.materials.new(r['nom'])
    nt = m.node_tree
    bsdf = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    bsdf.inputs['Base Color'].default_value = rgb + [1.0]
    bsdf.inputs['Roughness'].default_value = 0.7
    m.diffuse_color = rgb + [1.0]
    name = r['nom'].lower()
    if name.startswith('transparent'):
        alpha = r['alpha'] if r.get('alpha') is not None else 0.55
        bsdf.inputs['Alpha'].default_value = alpha
        m.surface_render_method = 'BLENDED'
    if 'lueur' in name:
        bsdf.inputs['Emission Color'].default_value = rgb + [1.0]
        bsdf.inputs['Emission Strength'].default_value = 2.0
    m['thunk_palette'] = r['couleur']
    m.use_fake_user = True
    m.asset_mark()
    m.asset_data.catalog_id = CATALOG
    m.asset_data.description = 'Palette du joueur : %s (%s, couleur du %s)' % (r['nom'], r['couleur'], data['kit'])
    m.asset_generate_preview()
bpy.ops.wm.save_as_mainfile(filepath=out, compress=True)
print('THUNK palette : %d couleurs dans %s' % (len(data['roles']), out))
