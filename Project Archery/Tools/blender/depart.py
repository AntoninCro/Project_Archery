# Le fichier Blender de départ d'un bâtiment (14 novembre ; Docs/Guide_Blender.md), lancé par
# Tools/refonte_visuelle/preparer.py, dans Blender en arrière-plan :
#   blender --background --factory-startup --python depart.py -- <données.json> <sortie.blend>
# Il contient :
# - la collection « Reference » : les trois images de référence posées et réglées, et le modèle de Claude niveau par niveau
#   (verrouillé, jamais exporté : il sert à voir et à s'inspirer, pas à être retouché) ;
# - la collection « Modele », où le joueur travaille : les repères du code, des objets vides nommés à la place qu'ils ont chez
#   Claude (les niveaux, les pièces qui bougent, les points de tir) ;
# - le cahier des charges (un texte, dans l'éditeur de texte de Blender) ;
# - les réglages pour l'extension THUNK (Tools/blender/thunk_blender.py), qui vérifie et exporte vers Unity.
import bpy, json, math, os, sys
import numpy as np
from mathutils import Matrix

args = sys.argv[sys.argv.index('--') + 1:]
meta = json.load(open(args[0], encoding='utf-8'))
out = os.path.abspath(args[1])
arrays = np.load(meta['npz'])
L = meta['L']

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.name = meta['batiment']
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0


def collection(name, parent=None):
    c = bpy.data.collections.new(name)
    (parent.children if parent else scene.collection.children).link(c)
    return c


model = collection('Modele')
ref = collection('Reference')
images_col = collection('Images de reference', ref)
claude_col = collection('Modele de Claude', ref)


def rel(path):
    return '//' + os.path.relpath(path, os.path.dirname(out)).replace('\\', '/')


# Les images de référence, posées comme « Add > Image > Background » : derrière tout, dans leur vue seulement.
for view, info in meta['images'].items():
    img = bpy.data.images.load(info['fichier'])
    img.filepath = rel(info['fichier'])
    o = bpy.data.objects.new('Image ' + view, None)
    o.empty_display_type = 'IMAGE'
    o.data = img
    o.empty_display_size = info['size']
    o.location = info['location']
    o.rotation_euler = [math.radians(a) for a in info['rotation']]
    o.empty_image_depth = 'BACK'
    o.show_empty_image_orthographic = True
    o.show_empty_image_perspective = False
    o.show_empty_image_only_axis_aligned = True
    o.hide_select = True
    images_col.objects.link(o)

# Le modèle de Claude : un objet par niveau (le niveau 1, puis ce que chaque niveau ajoute), aux couleurs de son modèle.
mat = bpy.data.materials.new('Modele de Claude (reference)')
mat.diffuse_color = (0.75, 0.78, 0.85, 1.0)
nt = mat.node_tree
bsdf = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
if bsdf is None:
    bsdf = nt.nodes.new('ShaderNodeBsdfPrincipled')
    output = next((n for n in nt.nodes if n.type == 'OUTPUT_MATERIAL'), None) or nt.nodes.new('ShaderNodeOutputMaterial')
    nt.links.new(bsdf.outputs[0], output.inputs[0])
attr = nt.nodes.new('ShaderNodeVertexColor')
attr.layer_name = 'Couleur'
nt.links.new(attr.outputs['Color'], bsdf.inputs['Base Color'])
bsdf.inputs['Roughness'].default_value = 1.0

for i, name in enumerate(meta['maillages']):
    V, F, C = arrays['V%d' % i], arrays['F%d' % i], arrays['C%d' % i]
    me = bpy.data.meshes.new(name)
    me.from_pydata(V.tolist(), [], F.tolist())
    me.update()
    colors = np.ones((len(F) * 3, 4), dtype=np.float32)
    colors[:, :3] = np.repeat(C, 3, axis=0)
    ca = me.color_attributes.new('Couleur', 'BYTE_COLOR', 'CORNER')
    ca.data.foreach_set('color_srgb', colors.ravel())
    me.color_attributes.active_color = ca
    me.materials.append(mat)
    o = bpy.data.objects.new(name, me)
    o.hide_select = True
    sub = collection(name, claude_col)
    sub.objects.link(o)

# Les repères du code : des objets vides nommés, dans la collection « Modele ». Rattacher ses pièces au bon repère ; le
# déplacer si besoin (pour une pièce qui tourne, le repère est le pivot).
size = max(0.3, L / 25.0)
objs = {}
for m in meta['reperes']:
    e = bpy.data.objects.new(m['cle'], None)
    e.empty_display_type = 'ARROWS'
    e.empty_display_size = size
    model.objects.link(e)
    if m['parent'] and m['parent'] in objs:
        e.parent = objs[m['parent']]
    e.matrix_world = Matrix(m['matrice'])
    e['thunk_chemin'] = json.dumps(m['chemin'], ensure_ascii=False)
    objs[m['cle']] = e

# Le cahier des charges, dans l'éditeur de texte de Blender.
lines = ['%s (%s)' % (meta['titre'], meta['batiment']), '']
lines.append('Chez Claude : %d x %d cases au sol au niveau 1 (une case = 1 m), %.1f m de haut au niveau 1, %.1f m au dernier.'
             % (meta['cases'][0], meta['cases'][1], meta['hauteur_niveau1'], meta['hauteur']))
lines.append('')
if meta['descriptions']:
    lines.append('Ce que fait chaque niveau (le texte du jeu) :')
    for i, d in enumerate(meta['descriptions']):
        lines.append('  Niveau %d : %s' % (i + 1, d))
    lines.append('')
lines.append('Les repères du code (des objets vides dans la collection Modele) : rattache tes pièces au bon repère (Ctrl+P),')
lines.append('déplace-le si besoin ; pour une pièce qui tourne, le repère est le pivot. Ne les renomme pas, ne les supprime pas.')
for m in meta['reperes']:
    what = []
    if m['champs']:
        what.append('le code : ' + ', '.join(m['champs']))
    if m['script']:
        what.append('porte un script')
    if m['piece']:
        what.append('une pièce chez Claude')
    lines.append('  - %s (%s)' % (m['cle'], ' ; '.join(what)))
lines += ['', 'Le modèle de Claude (collection Reference > Modele de Claude) ne sert qu\'à voir : il est verrouillé et jamais exporté.',
          'Vérifier et exporter : panneau N, onglet THUNK.']
txt = bpy.data.texts.new('Cahier des charges')
txt.write('\n'.join(lines) + '\n')

# Les réglages pour l'extension THUNK.
scene['thunk'] = json.dumps({k: meta[k] for k in ('version', 'batiment', 'titre', 'prefab', 'fbx', 'niveaux', 'cases',
                                                   'hauteur_niveau1', 'hauteur', 'L', 'prepare_le')} |
                            {'reperes': [{k: m[k] for k in ('cle', 'nom', 'chemin', 'piece', 'champs')} for m in meta['reperes']]},
                            ensure_ascii=False)

# La vue : l'aperçu des matériaux (les couleurs des kits et du modèle de Claude), le bâtiment cadré.
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type != 'VIEW_3D':
            continue
        for space in area.spaces:
            if space.type == 'VIEW_3D':
                space.shading.type = 'MATERIAL'
                space.clip_end = max(1000.0, L * 20.0)
                r3d = space.region_3d
                if r3d:
                    r3d.view_distance = L * 1.6
                    r3d.view_location = (0.0, 0.0, meta['hauteur'] / 2.0)

# Ce qu'on ajoute (une forme, une pièce glissée de la bibliothèque) va dans « Modele ».
for lc in bpy.context.view_layer.layer_collection.children:
    if lc.collection == model:
        bpy.context.view_layer.active_layer_collection = lc

bpy.ops.wm.save_as_mainfile(filepath=out, compress=True, relative_remap=True)
print('THUNK fichier de départ : %s (%d repères, %d morceaux du modèle de Claude)' % (out, len(objs), len(meta['maillages'])))
