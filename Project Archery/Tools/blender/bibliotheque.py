# La bibliothèque des kits pour Blender (14 novembre ; Docs/Guide_Blender.md) : chaque pièce des kits du projet, faits par des
# humains (Kenney, Quaternius…), devient un « asset » de Blender, avec son aperçu, ses couleurs, son auteur et sa licence : on la
# glisse depuis l'Asset Browser au lieu de passer par File > Import. Un fichier par kit dans Modeles_Blender/Bibliotheque (que
# l'extension THUNK déclare à Blender). Ces fichiers se refont avec ce script (ils ne sont pas dans Git) ; les pièces et les
# couleurs du joueur vont dans un fichier à lui du même dossier (Mes_pieces.blend), jamais touché ici.
# Depuis « Project Archery » :
#   blender --background --factory-startup --python Tools/blender/bibliotheque.py -- [kit…]
# (sans kit : tous ; les noms : TD, Chateau, Village, Nature, RTS, Divers)
# Chaque pièce garde dans ses propriétés (thunk_source) son kit, son auteur et sa licence : l'extension THUNK en fait la liste
# des crédits à l'export. Les matériaux : un par kit pour ceux qui peignent avec une texture de couleurs (« Kenney TD »…), sinon
# « <kit> <couleur> » ; l'outil de branchement d'Unity les reconnaît à ces noms.
import bpy, os, re, sys, json, uuid

PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ASSETS = os.path.join(PROJECT, 'Assets')
LIB = os.path.join(PROJECT, 'Modeles_Blender', 'Bibliotheque')

KITS = {
    # nom : (fichier, dossier des FBX, nom du matériau à texture, auteur, licence, titre)
    'TD': ('Kenney_TowerDefense', 'kenney_tower-defense-kit/Models/FBX format', 'Kenney TD', 'Kenney', 'CC0', 'Kenney Tower Defense Kit'),
    'Chateau': ('Kenney_Chateau', 'kenney_castle-kit/Models/FBX format', 'Kenney Chateau', 'Kenney', 'CC0', 'Kenney Castle Kit'),
    'Village': ('Kenney_Village', 'kenney_fantasy-town-kit/Models/FBX format', 'Kenney Village', 'Kenney', 'CC0', 'Kenney Fantasy Town Kit'),
    'Nature': ('Kenney_Nature', 'kenney_nature-kit/Models/FBX format', 'Kenney Nature', 'Kenney', 'CC0', 'Kenney Nature Kit'),
    'RTS': ('Quaternius_RTS', 'construction/Ultimate Fantasy RTS by Quaternius/FBX', 'Quaternius RTS', 'Quaternius', 'CC0', 'Quaternius Ultimate Fantasy RTS'),
    'Divers': ('Divers_Construction', 'construction', 'Divers', None, None, 'Modèles isolés (construction)'),
}
# Les modèles isolés de « construction » : la licence de chaque auteur (Credits_Licences.md).
CC_BY = ('Zsky', 'theking1322', 'miro_art_studio', 'Aime Tribolet', 'Aaron Clifford')


def catalog(kit):
    return str(uuid.uuid5(uuid.NAMESPACE_URL, 'thunk-vr/bibliotheque/' + kit))


def write_catalogs():
    path = os.path.join(LIB, 'blender_assets.cats.txt')
    lines = open(path, encoding='utf-8').read().splitlines() if os.path.exists(path) else \
        ['# Catalogues de la bibliothèque THUNK (Tools/blender/bibliotheque.py ; le joueur peut en ajouter)', 'VERSION 1', '']
    for kit, info in KITS.items():
        u = catalog(kit)
        if not any(l.startswith(u) for l in lines):
            lines.append('%s:%s:%s' % (u, info[5].replace(':', ' '), info[5].replace(':', ' ')))
    open(path, 'w', encoding='utf-8').write('\n'.join(lines) + '\n')


def base_color(mat):
    nt = mat.node_tree
    if nt:
        for n in nt.nodes:
            if n.type == 'BSDF_PRINCIPLED':
                return tuple(n.inputs['Base Color'].default_value)
    return tuple(mat.diffuse_color)


def texture_of(mat):
    nt = mat.node_tree
    if nt:
        for n in nt.nodes:
            if n.type == 'TEX_IMAGE' and n.image:
                return n.image
    return None


def make_material(name, color=None, image=None):
    mat = bpy.data.materials.get(name)
    if mat:
        return mat
    mat = bpy.data.materials.new(name)
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is None:
        bsdf = nt.nodes.new('ShaderNodeBsdfPrincipled')
        out = next((n for n in nt.nodes if n.type == 'OUTPUT_MATERIAL'), None) or nt.nodes.new('ShaderNodeOutputMaterial')
        nt.links.new(bsdf.outputs[0], out.inputs[0])
    bsdf.inputs['Roughness'].default_value = 0.8
    if image is not None:
        tex = nt.nodes.new('ShaderNodeTexImage')
        tex.image = image
        tex.interpolation = 'Closest'   # une texture de couleurs : des aplats nets
        nt.links.new(tex.outputs['Color'], bsdf.inputs['Base Color'])
    elif color is not None:
        bsdf.inputs['Base Color'].default_value = color
        mat.diffuse_color = color
    return mat


def import_piece(path):
    before = set(bpy.data.objects)
    try:
        bpy.ops.import_scene.fbx(filepath=path)
    except Exception:
        # les FBX en texte (le Nature Kit) : l'importeur de Blender écrit en C++ les lit
        try:
            bpy.ops.wm.fbx_import(filepath=path)
        except Exception as e:
            print('THUNK import raté : %s (%s)' % (path, e))
            return None
    new = [o for o in bpy.data.objects if o not in before]
    meshes = [o for o in new if o.type == 'MESH']
    if not meshes:
        for o in new:
            bpy.data.objects.remove(o)
        return None
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in meshes:
        o.select_set(True)
    target = next((o for o in meshes if o.parent is None), meshes[0])
    leftovers = [o.name for o in new if o != target]   # par leur nom : la jonction en supprime une partie
    bpy.context.view_layer.objects.active = target
    if len(meshes) > 1:
        with bpy.context.temp_override(active_object=target, object=target, selected_objects=meshes,
                                       selected_editable_objects=meshes):
            bpy.ops.object.join()
    target.parent = None
    with bpy.context.temp_override(active_object=target, object=target, selected_objects=[target],
                                   selected_editable_objects=[target]):
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for n in leftovers:
        o = bpy.data.objects.get(n)
        if o is not None and o != target:
            bpy.data.objects.remove(o)
    return target


def build(kit):
    fname, folder, texname, author, licence, title = KITS[kit]
    out = os.path.join(LIB, fname + '.blend')
    bpy.ops.wm.read_factory_settings(use_empty=True)
    col = bpy.data.collections.new(title)
    bpy.context.scene.collection.children.link(col)
    root = os.path.join(ASSETS, folder)
    files = []
    if kit == 'Divers':
        for d in sorted(os.listdir(root)):
            if d.startswith('Ultimate Fantasy RTS'):
                continue
            for dp, dn, fn in os.walk(os.path.join(root, d)):
                files += [os.path.join(dp, f) for f in fn if f.lower().endswith('.fbx')]
    else:
        files = sorted(os.path.join(root, f) for f in os.listdir(root) if f.lower().endswith('.fbx'))
    count = 0
    for i, path in enumerate(files):
        stem = os.path.splitext(os.path.basename(path))[0]
        a, lic, src = author, licence, title
        if kit == 'Divers':
            m = re.match(r'(.+?) by (.+?) - ', os.path.relpath(path, root).split(os.sep)[0])
            src, a = (m.group(1), m.group(2)) if m else (stem, '?')
            lic = 'CC-BY 3.0 (à créditer)' if a in CC_BY else 'CC0'
            stem = src
        o = import_piece(path)
        if o is None:
            continue
        o.name = stem
        o.data.name = stem
        # les matériaux : un par kit pour la texture de couleurs, sinon « <kit> <couleur> »
        for slot in o.material_slots:
            m = slot.material
            if m is None:
                continue
            img = texture_of(m)
            if img is not None:
                # le chemin reste absolu jusqu'à l'enregistrement : les aperçus se font avant, et un chemin relatif à un
                # fichier pas encore enregistré ne mène nulle part (des aperçus violets, 14 novembre)
                img.name = texname + ' (texture)'
                slot.material = make_material(texname, image=img)
            else:
                slot.material = make_material('%s %s' % (texname, re.sub(r'\.\d{3}$', '', m.name)), color=base_color(m))
        for c in o.users_collection:
            c.objects.unlink(o)
        col.objects.link(o)
        o.location = ((count % 16) * 3.0, (count // 16) * 3.0, 0.0)
        o['thunk_source'] = '%s · %s · %s · %s' % (src, a, lic, os.path.relpath(path, ASSETS).replace('\\', '/'))
        o.asset_mark()
        o.asset_data.catalog_id = catalog(kit)
        o.asset_data.author = a or ''
        o.asset_data.description = '%s, par %s (%s)' % (src, a, lic)
        o.asset_data.tags.new(kit)
        o.asset_generate_preview()
        count += 1
    bpy.data.orphans_purge(do_recursive=True)
    os.makedirs(LIB, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=out, compress=True, relative_remap=True)
    # puis les chemins des textures relatifs au fichier (le dépôt peut changer de place), et on réenregistre
    bpy.ops.file.make_paths_relative()
    bpy.ops.wm.save_mainfile(compress=True)
    missing = [i.name for i in bpy.data.images if i.source == 'FILE' and not i.packed_file
               and not os.path.exists(bpy.path.abspath(i.filepath))]
    print('THUNK bibliothèque : %s, %d pièces (%s)%s' % (fname, count, out,
                                                         ' ; textures introuvables : ' + ', '.join(missing) if missing else ''))


bpy.context.preferences.filepaths.save_version = 0   # pas de copies .blend1 à côté des bibliothèques (préférences non enregistrées)
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
kits = [k for k in (args or list(KITS)) if k in KITS]
os.makedirs(LIB, exist_ok=True)
write_catalogs()
for k in kits:
    build(k)
