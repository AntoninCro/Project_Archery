# L'extension THUNK pour Blender (14 novembre ; Docs/Guide_Blender.md, Docs/Refonte_Visuelle.md) : dans un fichier préparé par
# Tools/refonte_visuelle/preparer.py, un onglet « THUNK » dans le panneau N :
# - Vérifier : les repères du code (présents, pas renommés, une pièce rattachée à ceux qui en avaient une), le sol, les cases au
#   sol et la hauteur comparées au modèle de Claude, les faces, les couleurs, les pièces des kits utilisées (pour les crédits) ;
# - Exporter vers Unity : la collection « Modele » seule, avec les réglages vérifiés le 14 novembre (un aller-retour exact vers
#   le repère d'Unity), dans Assets/_Project/Art/Modeles/<Nom>.fbx, avec à côté <Nom>.json (les repères et leur chemin dans
#   le prefab) : l'outil d'Unity (Editor/ModelesFaitMain.cs) branche alors le modèle sur le prefab tout seul ;
# - Exporter à chaque Ctrl+S (si la vérification passe) ; le modèle de Claude à côté ou au centre, visible ou caché ;
# - le temps de travail sur le fichier (les minutes où quelque chose a été fait), pour l'estimation de la refonte.
# Il déclare aussi à Blender la bibliothèque des kits (Modeles_Blender/Bibliotheque), à l'ouverture d'un fichier préparé.
# Installation (une fois) : Tools/blender/installer.py, ou à la main : Edit > Preferences > File Paths > Script Directories,
# ajouter le dossier Tools/blender, redémarrer Blender, puis Add-ons : cocher « THUNK VR ».
bl_info = {
    'name': 'THUNK VR',
    'author': 'THUNK VR',
    'version': (1, 0, 0),
    'blender': (4, 2, 0),
    'location': 'Vue 3D > panneau N > THUNK',
    'description': 'Vérifier et exporter un bâtiment de THUNK VR vers Unity',
    'category': 'Import-Export',
}

import bpy, json, math, os, re, datetime
import bpy.utils.previews
from bpy.app.handlers import persistent
from mathutils import Vector

LIB_NAME = 'THUNK Kits'
MODEL = 'Modele'
CLAUDE = 'Modele de Claude'
# Les matériaux des kits (Tools/blender/bibliotheque.py) : leurs copies « Kenney TD.001 »… se rangent dans l'original.
KIT_PREFIXES = ('Kenney ', 'Quaternius ', 'Divers')


def linked_parts():
    """Les pièces de Modele qu'on ne peut pas modifier : glissées de la bibliothèque en mode « Empaqueter » (le mode par défaut
    de Blender 5, 14 novembre) ou « Lier »."""
    col = bpy.data.collections.get(MODEL)
    out = []
    for o in (col.all_objects if col else []):
        data = o.data
        if o.library or (data is not None and data.library) or \
                (o.type == 'MESH' and any(m is not None and m.library for m in data.materials)):
            out.append(o)
    return out


def local_object(name):
    """L'objet local de ce nom : une pièce empaquetée garde son nom d'origine, qui peut être celui d'un repère
    (« tower-round-crystals », 14 novembre) ; le repère, lui, est toujours local."""
    for o in bpy.data.objects:
        if o.name == name and o.library is None:
            return o
    return None


def localiser(objs=None):
    """Rend modifiables (locales) les pièces liées ou empaquetées (celles de Modele, ou celles données), leurs maillages et
    leurs matériaux. Rend {pointeur d'avant : objet local}."""
    mapping = {}
    for o in (linked_parts() if objs is None else objs):
        ptr = o.as_pointer()
        if o.library:
            local = o.make_local()
            if local != o:
                o.user_remap(local)
            o = local
        if o.data is not None and o.data.library:
            data = o.data.make_local()
            if data != o.data:
                o.data.user_remap(data)
        if o.type == 'MESH':
            for i, m in enumerate(o.data.materials):
                if m is not None and m.library:
                    lm = m.make_local()
                    if lm != m:
                        m.user_remap(lm)
        mapping[ptr] = o
    return mapping


def ranger_couleurs():
    """Range les copies des matériaux des kits (« Kenney TD.001 », faites à chaque pièce glissée) dans l'original."""
    count = 0
    for m in list(bpy.data.materials):
        base = re.sub(r'\.\d{3}$', '', m.name)
        target = bpy.data.materials.get(base)
        if base == m.name or not base.startswith(KIT_PREFIXES) or m.library or target is None or target.library:
            continue
        m.user_remap(target)
        bpy.data.materials.remove(m)
        count += 1
    return count


def palette_roles():
    """La palette du joueur (Modeles_Blender/Bibliotheque/palette_roles.json, faite par Tools/refonte_visuelle/palette.py
    d'après ses choix dans Palette.txt) : {nom du rôle : rôle}."""
    root = project_dir()
    path = os.path.join(root, 'Modeles_Blender', 'Bibliotheque', 'palette_roles.json') if root else None
    if not path or not os.path.exists(path):
        return {}
    try:
        return {r['nom']: r for r in json.load(open(path, encoding='utf-8'))['roles']}
    except Exception:
        return {}


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def appliquer_palette():
    """Les couleurs de la palette (le choix du joueur, 14 novembre) sur tout matériau du même nom (« Pierre », « Pierre.001 »…) :
    il suffit de nommer ; la palette colore, partout pareil. Rend le nombre de matériaux mis à la couleur de la palette."""
    roles = palette_roles()
    count = 0
    for m in bpy.data.materials:
        r = roles.get(re.sub(r'\.\d{3}$', '', m.name))
        if r is None or m.library or m.node_tree is None:
            continue
        bsdf = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        if bsdf is None:
            continue
        h = r['couleur'].lstrip('#')
        rgb = [srgb_to_linear(int(h[k:k + 2], 16) / 255.0) for k in (0, 2, 4)]
        bsdf.inputs['Base Color'].default_value = rgb + [1.0]
        m.diffuse_color = rgb + [1.0]
        name = r['nom'].lower()
        if name.startswith('transparent'):
            bsdf.inputs['Alpha'].default_value = r['alpha'] if r.get('alpha') is not None else 0.55
            m.surface_render_method = 'BLENDED'
        if 'lueur' in name:
            bsdf.inputs['Emission Color'].default_value = rgb + [1.0]
            bsdf.inputs['Emission Strength'].default_value = 2.0
        count += 1
    # les copies d'un matériau de la palette (« Pierre.001 ») ont maintenant la même couleur : rangées dans l'original
    for m in list(bpy.data.materials):
        base = re.sub(r'\.\d{3}$', '', m.name)
        target = bpy.data.materials.get(base)
        if base != m.name and base in roles and target is not None and not m.library and not target.library:
            m.user_remap(target)
            bpy.data.materials.remove(m)
    return count


def preparer_transparents():
    """Les couleurs « Transparent… » (transparentes dans le jeu, le joueur, 14 novembre) : Blender les montre transparentes
    (rendu Mélangé), à l'Alpha du matériau ; 0,55 s'il n'est pas réglé (celui que le jeu prend aussi)."""
    for m in bpy.data.materials:
        if m.library or not m.name.lower().startswith('transparent'):
            continue
        try:
            m.surface_render_method = 'BLENDED'
        except Exception:
            pass
        bsdf = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None) if m.node_tree else None
        if bsdf is not None and bsdf.inputs['Alpha'].default_value >= 0.99:
            bsdf.inputs['Alpha'].default_value = 0.55


def info(scene=None):
    scene = scene or bpy.context.scene
    try:
        return json.loads(scene['thunk'])
    except Exception:
        return None


def project_dir():
    if not bpy.data.filepath:
        return None
    return os.path.dirname(os.path.dirname(os.path.abspath(bpy.data.filepath)))


def ancestors(o):
    p = o.parent
    while p:
        yield p
        p = p.parent


def level_of(o):
    for p in [o] + list(ancestors(o)):
        if p.name.startswith('Niveau '):
            return p.name
    return None


def bounds(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    if not pts:
        return None
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return lo, hi


def cells(size):
    return int(min(6, max(1, math.ceil(size - 0.15))))


# ---------------------------------------------------------------- vérifier

def verifier(scene=None):
    """Une liste de (niveau, message) : ERREUR (l'export est refusé), ATTENTION, INFO."""
    scene = scene or bpy.context.scene
    data = info(scene)
    out = []
    if data is None:
        return [('ERREUR', 'Ce fichier n\'a pas été préparé pour THUNK (Tools/refonte_visuelle/preparer.py).')]
    col = bpy.data.collections.get(MODEL)
    if col is None:
        return [('ERREUR', 'La collection « Modele » a disparu : c\'est elle qui part dans le jeu.')]
    objs = list(col.all_objects)
    model_set = set(objs)
    for r in data['reperes']:
        k = r['cle']
        o = local_object(k)
        if o is None or o not in model_set:
            alt = [x.name for x in bpy.data.objects if x.library is None and x.type == 'EMPTY' and x.name.startswith(k + '.')]
            if o is not None:
                out.append(('ERREUR', 'Repère « %s » sorti de la collection Modele : l\'y remettre (M).' % k))
            elif alt:
                out.append(('ERREUR', 'Repère « %s » renommé en « %s » : lui rendre son nom (F2).' % (k, alt[0])))
            else:
                out.append(('ERREUR', 'Repère « %s » supprimé : Ctrl+Z, ou demander à Claude de le remettre.' % k))
            continue
        if r.get('piece') and o.type != 'MESH' and not any(c.type == 'MESH' for c in o.children_recursive):
            out.append(('ATTENTION', 'Rien n\'est rattaché à « %s » (une pièce chez Claude) : rattache-lui la tienne (Ctrl+P), '
                                     'sinon elle sera invisible.' % k))
        s = o.matrix_world.to_scale()
        if max(s) > 1e-6 and max(s) / max(min(s), 1e-6) > 1.01:
            out.append(('ATTENTION', 'Repère « %s » étiré (%.2f, %.2f, %.2f) : garde-le à la même échelle partout.' % ((k,) + tuple(s))))

    linked = linked_parts()
    if linked:
        out.append(('ATTENTION', '%d pièces de la bibliothèque ne sont pas modifiables (%s…) : bouton « Rendre les pièces '
                                 'modifiables ».' % (len(linked), linked[0].name)))
    # tout Modele part, caché ou non : l'œil sert à travailler à l'aise, pas à retirer une pièce du jeu (le joueur, 14 novembre :
    # le niveau 5 caché avait disparu de l'export)
    meshes = [o for o in objs if o.type == 'MESH']
    hidden = [o for o in objs if not o.visible_get()]
    if hidden:
        out.append(('INFO', '%d objets cachés dans Blender : ils partent quand même dans le jeu.' % len(hidden)))
    claude = [o for o in meshes if o.data.name.startswith('Claude - ') or o.name.startswith('Claude - ')]
    if claude:
        out.append(('ERREUR', 'Le modèle de Claude est dans Modele (%s) : il ne doit jamais partir dans le jeu.' % claude[0].name))
    if not meshes:
        out.append(('ERREUR', 'Aucune pièce visible dans la collection Modele.'))
        return out

    for o in meshes:
        b = bounds([o])
        if b and b[0].z < -0.05:
            out.append(('ATTENTION', '« %s » passe sous le sol (%.2f m).' % (o.name, b[0].z)))
        if not any(s.material for s in o.material_slots):
            out.append(('ATTENTION', '« %s » n\'a pas de couleur (matériau) : il sera gris dans le jeu.' % o.name))

    level1 = [o for o in meshes if level_of(o) is None]
    b1 = bounds(level1) if level1 else None
    ball = bounds(meshes)
    if b1:
        size = b1[1] - b1[0]
        c = [cells(size.x), cells(size.y)]
        theirs = data.get('cases', [0, 0])
        line = 'Au sol au niveau 1 : %d × %d cases (Claude : %d × %d) ; %.1f m de haut au niveau 1, %.1f m au dernier.' % (
            c[0], c[1], theirs[0], theirs[1], b1[1].z, ball[1].z)
        out.append(('ATTENTION' if c[0] * c[1] > theirs[0] * theirs[1] else 'INFO',
                    line + (' Plus de cases : il prendra plus de place sur la grille.' if c[0] * c[1] > theirs[0] * theirs[1] else '')))
    else:
        out.append(('ATTENTION', 'Rien au niveau 1 : toutes les pièces sont rattachées à un niveau.'))

    deps = bpy.context.evaluated_depsgraph_get()
    tris = 0
    for o in meshes:
        ev = o.evaluated_get(deps)
        me = ev.to_mesh()
        me.calc_loop_triangles()
        tris += len(me.loop_triangles)
        ev.to_mesh_clear()
    out.append(('ATTENTION' if tris > 30000 else 'INFO', '%d triangles, %d pièces.%s' % (
        tris, len(meshes), ' Beaucoup pour le casque : moins de côtés aux cylindres et aux sphères ?' if tris > 30000 else '')))
    mats = sorted({re.sub(r'\.\d{3}$', '', s.material.name) for o in meshes for s in o.material_slots if s.material})
    out.append(('INFO', 'Couleurs : %s.' % ', '.join(mats)))
    roles = palette_roles()
    if roles:
        off = [m for m in mats if m not in roles and not m.startswith(KIT_PREFIXES) and 'Couleur #' not in m
               and re.sub(r'^Transparent |^Lueur | lueur$', '', m) not in roles]
        if off:
            out.append(('ATTENTION', 'Couleurs hors de la palette : %s. Donne-leur un nom de la palette (%s…), ou dis à '
                                     'Claude quel rôle elles ont.' % (', '.join(off), ', '.join(list(roles)[:3]))))
    sources = sorted({o['thunk_source'] for o in meshes if 'thunk_source' in o})
    if sources:
        out.append(('INFO', 'Pièces de kits : %s.' % ' ; '.join(s.split(' · ')[0] + ' (' + s.split(' · ')[1] + ')' for s in sources)))
        for s in sources:
            if 'CC-BY' in s:
                out.append(('ATTENTION', 'À créditer (CC-BY) : %s.' % s))
    outside = [o for o in bpy.context.scene.objects if o.type == 'MESH' and o.visible_get() and o not in objs
               and not any(c.name in (CLAUDE, 'Reference') or c.name.startswith('Claude - ') for c in o.users_collection)]
    if outside:
        out.append(('ATTENTION', '%d pièces hors de la collection Modele ne partiront pas (%s…) : bouton « Ranger les pièces '
                                 'dans Modele ».' % (len(outside), outside[0].name)))
    if not any(m[0] in ('ERREUR', 'ATTENTION') for m in out):
        out.insert(0, ('OK', 'Tout est bon.'))
    return out


# ---------------------------------------------------------------- exporter

def exporter(scene=None):
    scene = scene or bpy.context.scene
    localiser()        # une pièce empaquetée peut porter le nom d'un repère : locale, elle prend un nom à elle
    ranger_couleurs()
    appliquer_palette()
    preparer_transparents()
    model = bpy.data.collections.get(MODEL)
    if model is not None:
        retirer_couleurs_vides(list(model.all_objects))
    msgs = verifier(scene)
    if any(m[0] == 'ERREUR' for m in msgs):
        return False, msgs
    data = info(scene)
    root = project_dir()
    if root is None:
        return False, msgs + [('ERREUR', 'Enregistre d\'abord le fichier (Ctrl+S).')]
    fbx = os.path.join(root, data['fbx'].replace('/', os.sep))
    os.makedirs(os.path.dirname(fbx), exist_ok=True)
    col = bpy.data.collections[MODEL]
    layer = bpy.context.view_layer
    before = [o for o in layer.objects if o.select_get()]
    active = layer.objects.active
    # tout Modele part, caché ou non : on montre le temps de l'export ce que l'œil cache (un objet caché ne se sélectionne
    # pas), puis on le recache
    lc = find_layer(layer.layer_collection, MODEL)
    col_hidden = lc.hide_viewport if lc else False
    if lc:
        lc.hide_viewport = False
    chosen = list(col.all_objects)
    hidden = [o for o in chosen if o.hide_get()]
    for o in hidden:
        o.hide_set(False)
    for o in layer.objects:
        o.select_set(False)
    for o in chosen:
        o.select_set(True)
    try:
        bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'EMPTY', 'MESH'}, apply_unit_scale=True,
                                 apply_scale_options='FBX_SCALE_ALL', use_space_transform=True, bake_space_transform=True,
                                 axis_forward='-Z', axis_up='Y', use_mesh_modifiers=True, mesh_smooth_type='FACE',
                                 use_custom_props=False, bake_anim=False, add_leaf_bones=False, path_mode='AUTO')
    finally:
        for o in layer.objects:
            o.select_set(o in before)
        layer.objects.active = active
        for o in hidden:
            o.hide_set(True)
        if lc:
            lc.hide_viewport = col_hidden
        # l'œil des repères (eye_tick) ne doit pas prendre ce retour pour un clic
        for o in hidden:
            _visibility[o.as_pointer()] = True
    meshes = [o for o in chosen if o.type == 'MESH']
    side = {
        'batiment': data['batiment'], 'prefab': data['prefab'], 'fbx': data['fbx'],
        'exporte_le': datetime.datetime.now().isoformat(timespec='seconds'),
        'reperes': [{'cle': r['cle'], 'chemin': r['chemin'], 'piece': bool(r.get('piece'))} for r in data['reperes']],
        'sources': sorted({o['thunk_source'] for o in meshes if 'thunk_source' in o}),
        'minutes': int(scene.get('thunk_minutes', 0)),
    }
    with open(os.path.splitext(fbx)[0] + '.json', 'w', encoding='utf-8') as f:
        json.dump(side, f, ensure_ascii=False, indent=1)
    msgs.append(('OK', 'Exporté : %s (%d objets). Clique dans Unity : il se branche tout seul.' % (data['fbx'], len(chosen))))
    return True, msgs


def store(msgs):
    bpy.context.window_manager.thunk_rapport = json.dumps(msgs, ensure_ascii=False)


# ---------------------------------------------------------------- opérateurs et panneau

class THUNK_OT_verifier(bpy.types.Operator):
    bl_idname = 'thunk.verifier'
    bl_label = 'Vérifier'
    bl_description = 'Vérifier le bâtiment avant l\'export'

    def execute(self, context):
        appliquer_palette()
        preparer_transparents()
        msgs = verifier(context.scene)
        store(msgs)
        bad = [m for m in msgs if m[0] == 'ERREUR']
        self.report({'ERROR'} if bad else {'INFO'}, bad[0][1] if bad else 'Vérifié.')
        return {'FINISHED'}


class THUNK_OT_exporter(bpy.types.Operator):
    bl_idname = 'thunk.exporter'
    bl_label = 'Exporter vers Unity'
    bl_description = 'Vérifier, puis exporter la collection Modele vers le jeu'

    def execute(self, context):
        ok, msgs = exporter(context.scene)
        store(msgs)
        if not ok:
            bad = [m for m in msgs if m[0] == 'ERREUR']
            self.report({'ERROR'}, 'Pas exporté : ' + (bad[0][1] if bad else '?'))
            return {'CANCELLED'}
        self.report({'INFO'}, msgs[-1][1])
        return {'FINISHED'}


def level_empties(data):
    """Les repères des niveaux : {numéro : objet} (« Niveau 2 », « Niveau 3 (drapeau) » pour la Baliste…)."""
    out = {}
    for r in data['reperes']:
        m = re.match(r'Niveau (\d+)\b', r['cle'])
        o = local_object(r['cle'])
        if m and o is not None:
            out.setdefault(int(m.group(1)), o)
    return out


def level_number(o):
    lvl = level_of(o) if o is not None else None
    m = re.match(r'Niveau (\d+)', lvl) if lvl else None
    return int(m.group(1)) if m else 1


class THUNK_OT_niveau(bpy.types.Operator):
    """Le joueur, 14 novembre : « Niveau 2 » est un repère, pas une collection, et le rattacher à la main (Ctrl+P, dans le bon
    ordre) n'allait pas de soi. Un bouton par niveau : les pièces sélectionnées y vont, sans bouger."""
    bl_idname = 'thunk.niveau'
    bl_label = 'Niveau'
    bl_description = ('Mettre les pièces sélectionnées à ce niveau : elles n\'apparaîtront qu\'à partir de lui (le niveau 1 : '
                      'toujours là). Elles ne bougent pas')
    bl_options = {'REGISTER', 'UNDO'}
    niveau: bpy.props.IntProperty(default=1, min=1)

    def execute(self, context):
        data = info(context.scene)
        col = bpy.data.collections.get(MODEL)
        if data is None or col is None:
            return {'CANCELLED'}
        # les repères par objet, pas par nom : une pièce empaquetée garde son nom d'origine, qui peut être celui d'un repère
        reperes = {local_object(r['cle']) for r in data['reperes']} - {None}
        sel = list(context.selected_objects)
        if not sel:
            self.report({'WARNING'}, 'Sélectionne d\'abord ta pièce (clic dessus), puis clique sur le niveau.')
            return {'CANCELLED'}
        # une pièce empaquetée ne se rattache pas : d'abord une vraie copie (elle reste sélectionnée)
        linked = [o for o in sel if o.library or (o.data is not None and o.data.library)]
        if linked:
            ptrs = [o.as_pointer() for o in sel]
            mapping = localiser(linked)
            sel = [mapping.get(p) or o for p, o in zip(ptrs, sel)]
        # une pièce hors de Modele (glissée quand une autre collection était active) : rangée dans Modele
        moved_in = ranger_dans_modele([o for o in sel if o not in reperes])
        model_set = set(col.all_objects)
        pieces = [o for o in sel if o not in reperes and o in model_set]
        if not pieces:
            first = sel[0]
            if first in reperes:
                why = '« %s » est un repère du code : sélectionne tes pièces, pas les repères.' % first.name
            elif any(c.name == CLAUDE or c.name.startswith('Claude - ') for c in first.users_collection):
                why = '« %s » est le modèle de Claude : il ne se modifie pas.' % first.name
            else:
                why = '« %s » n\'est pas dans la collection Modele : M > Modele, puis recommence.' % first.name
            self.report({'WARNING'}, why)
            return {'CANCELLED'}
        levels = level_empties(data)
        target = levels.get(self.niveau) if self.niveau > 1 else None
        if self.niveau > 1 and target is None:
            self.report({'WARNING'}, 'Ce bâtiment n\'a pas de niveau %d.' % self.niveau)
            return {'CANCELLED'}
        moved = 0
        for o in pieces:
            if level_number(o) == self.niveau and (self.niveau == 1 or o.parent == target):
                continue
            world = o.matrix_world.copy()
            o.parent = target          # le niveau 1 : rattachée à rien (détachée de son niveau d'avant)
            o.matrix_world = world     # elle ne bouge pas
            moved += 1
        self.report({'INFO'}, '%d pièce(s) au niveau %d%s.' % (
            moved, self.niveau, ' (%d rangée(s) dans Modele)' % moved_in if moved_in else ''))
        return {'FINISHED'}


def is_reference(o):
    return any(c.name in (CLAUDE, 'Reference', 'Images de reference') or c.name.startswith('Claude - ')
               for c in o.users_collection)


def outside_pieces():
    """Les pièces visibles hors de Modele (et hors de la référence) : elles ne partiraient pas."""
    col = bpy.data.collections.get(MODEL)
    inside = set(col.all_objects) if col else set()
    return [o for o in bpy.context.scene.objects
            if o.type == 'MESH' and o not in inside and not is_reference(o) and o.library is None]


def ranger_dans_modele(objs=None):
    """Range dans Modele des pièces posées ailleurs (à la racine de la scène : la collection active n'était pas Modele)."""
    col = bpy.data.collections.get(MODEL)
    if col is None:
        return 0
    inside = set(col.all_objects)
    count = 0
    for o in (outside_pieces() if objs is None else objs):
        if o in inside or is_reference(o) or o.library is not None:
            continue
        for c in list(o.users_collection):
            c.objects.unlink(o)
        col.objects.link(o)
        count += 1
    return count


class THUNK_OT_ranger(bpy.types.Operator):
    bl_idname = 'thunk.ranger'
    bl_label = 'Ranger les pièces dans Modele'
    bl_description = 'Les pièces posées hors de la collection Modele ne partent pas dans le jeu : les y ranger'
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        n = ranger_dans_modele()
        self.report({'INFO'}, '%d pièce(s) rangée(s) dans Modele.' % n)
        return {'FINISHED'}


class THUNK_OT_localiser(bpy.types.Operator):
    bl_idname = 'thunk.localiser'
    bl_label = 'Rendre les pièces modifiables'
    bl_description = ('Les pièces glissées en mode « Empaqueter » ne se modifient pas (Tab, Ctrl+J) : en faire de vraies '
                      'copies, et ranger les couleurs des kits en double')
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        n = len(localiser())
        m = ranger_couleurs()
        self.report({'INFO'}, '%d pièces rendues modifiables, %d couleurs en double rangées.' % (n, m))
        return {'FINISHED'}


def claude_objects():
    col = bpy.data.collections.get(CLAUDE)
    return list(col.all_objects) if col else []


class THUNK_OT_decaler(bpy.types.Operator):
    bl_idname = 'thunk.decaler_claude'
    bl_label = 'Modèle de Claude : à côté / au centre'
    bl_description = 'Poser le modèle de Claude à côté du tien pour comparer, ou le remettre au centre pour suivre ses formes'
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        objs = claude_objects()
        data = info(context.scene) or {}
        if not objs:
            return {'CANCELLED'}
        shift = 0.0 if abs(objs[0].location.x) > 1e-3 else float(data.get('L', 10))
        for o in objs:
            o.location.x = shift
        return {'FINISHED'}


def find_layer(layer, name):
    if layer.name == name:
        return layer
    for c in layer.children:
        r = find_layer(c, name)
        if r:
            return r
    return None


class THUNK_OT_voir(bpy.types.Operator):
    bl_idname = 'thunk.voir_claude'
    bl_label = 'Modèle de Claude : voir / cacher'
    bl_description = 'Afficher ou cacher le modèle de Claude'

    def execute(self, context):
        lc = find_layer(context.view_layer.layer_collection, CLAUDE)
        if lc:
            lc.hide_viewport = not lc.hide_viewport
        return {'FINISHED'}


ICONS = {'ERREUR': 'ERROR', 'ATTENTION': 'INFO', 'INFO': 'DOT', 'OK': 'CHECKMARK'}


# ---------------------------------------------------------------- recolorer les pièces des kits (le joueur, 14 novembre)
# Une pièce de Kenney prend ses couleurs dans la texture de couleurs du kit (chaque face, sa case). On lit les couleurs des pièces
# sélectionnées (regroupées quand elles se ressemblent : les dégradés du kit). Pour chacune, le joueur choisit, en voyant les
# couleurs (des pastilles, le nom au survol) : garder celle du kit, un pot de sa palette, une des couleurs du kit, ou une couleur
# libre (la roue de Blender) ; et s'il la veut transparente ou lumineuse. Le choix s'applique tout de suite (sans bouton). Les
# faces passent au matériau choisi (« Couleur #rrggbb », un rôle de la palette…) ; le matériau d'origine de chaque face est gardé
# dans un attribut, pour « Remettre les couleurs Kenney ».

_choix_cache = {}     # Blender garde les textes et les icônes des listes dynamiques seulement si on garde une référence
_pixel_cache = {}
_icons = None         # les pastilles (bpy.utils.previews)
_busy = [False]       # pendant la lecture : les réglages remplis ne doivent pas s'appliquer
MERGE = 0.12          # deux couleurs plus proches que ça (sur 0..1) comptent pour une (les dégradés du kit)
MERGE_FINE = 0.04     # « Séparer les nuances » : le rouge-orangé à part du rouge (10 groupes au lieu de 4 sur la tour du Givre)


def merge_threshold(context):
    return MERGE_FINE if getattr(context.scene, 'thunk_kit_nuances', False) else MERGE


def hexa(c):
    return '#%02x%02x%02x' % tuple(max(0, min(255, int(round(x * 255)))) for x in c[:3])


def from_hex(h):
    h = h.lstrip('#')
    return tuple(int(h[k:k + 2], 16) / 255.0 for k in (0, 2, 4))


def icon(h):
    """La pastille d'une couleur (#rrggbb) : son numéro d'icône."""
    global _icons
    if _icons is None:
        _icons = bpy.utils.previews.new()
    if h not in _icons:
        p = _icons.new(h)
        r, g, b = from_hex(h)
        for size, attr in (((32, 32), 'icon'), ((64, 64), 'image')):
            setattr(p, attr + '_size', size)
            setattr(p, attr + '_pixels_float', [r, g, b, 1.0] * (size[0] * size[1]))
    return _icons[h].icon_id


def kit_colors():
    """Les couleurs du kit Tower Defense (la planche Docs/Planches/Palette_td.json)."""
    root = project_dir()
    path = os.path.join(root, 'Docs', 'Planches', 'Palette_td.json') if root else None
    try:
        return json.load(open(path, encoding='utf-8'))['couleurs']
    except Exception:
        return []


def choix_items(self, context):
    items = [('GARDER', 'Couleur Kenney d\'origine', 'Garder la couleur du kit', icon(hexa(self.couleur)), 0)]
    for i, (name, r) in enumerate(palette_roles().items()):
        items.append(('R%d' % i, name, 'Ta palette : %s (%s)' % (name, r['couleur']), icon(r['couleur']), 1 + i))
    for i, h in enumerate(kit_colors()):
        items.append(('K%d' % i, h, 'Couleur %d du kit Kenney (%s)' % (i + 1, h), icon(h), 100 + i))
    items.append(('LIBRE', 'Couleur libre', 'La couleur choisie à la roue, à droite', 'COLOR', 999))
    _choix_cache[self.as_pointer()] = items
    return items


def on_choix(self, context):
    if not _busy[0]:
        appliquer_groupe(self, context)


def on_libre(self, context):
    if _busy[0]:
        return
    if self.choix != 'LIBRE':
        self.choix = 'LIBRE'          # on_choix applique
    else:
        appliquer_groupe(self, context)


class THUNK_KitColor(bpy.types.PropertyGroup):
    couleur: bpy.props.FloatVectorProperty(subtype='COLOR_GAMMA', size=3, min=0.0, max=1.0)
    faces: bpy.props.IntProperty()
    choix: bpy.props.EnumProperty(items=choix_items, name='Couleur', update=on_choix)
    libre: bpy.props.FloatVectorProperty(subtype='COLOR_GAMMA', size=3, min=0.0, max=1.0, name='Couleur libre',
                                         description='La roue de couleurs : une couleur à toi', update=on_libre)
    transparent: bpy.props.BoolProperty(name='Transparente', description='Transparente dans le jeu (comme la glace)',
                                        update=on_choix)
    lueur: bpy.props.BoolProperty(name='Lumineuse', description='Lumineuse dans le jeu', update=on_choix)


def kit_image(mat):
    if mat is None or mat.node_tree is None or not mat.name.startswith(KIT_PREFIXES):
        return None
    return next((n.image for n in mat.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image), None)


def image_pixels(img):
    key = img.name
    if key not in _pixel_cache:
        w, h = img.size
        if w == 0:
            return None
        import numpy as np
        _pixel_cache[key] = (np.array(img.pixels[:], dtype='float32').reshape(h, w, 4), w, h)
    return _pixel_cache[key]


def original_slot(o, p, stored):
    """L'emplacement du matériau Kenney d'origine d'une face (déjà recolorée : celui gardé dans thunk_kit_mat)."""
    if stored is not None and stored.data[p.index].value >= 0:
        return stored.data[p.index].value
    return p.material_index


def face_regions(o):
    """Les faces visibles d'une pièce de kit : {index d'un triangle : numéro de sa face visible}. Les pièces des kits sont en
    triangles, et la texture de Kenney est faite de bandes en dégradé (chaque face couvre un bout de bande, d'où l'ombré du
    kit) : le centre de chacun des deux triangles d'un carré tombe sur une nuance différente du dégradé. Lus un par un, ils se
    rangeaient dans deux nuances voisines, et un carré se recolorait à moitié (le joueur, 15 novembre, dans le jeu). Une face
    visible : des triangles voisins (une arête commune), dans le même plan, du même matériau d'origine, sur la même bande de la
    texture ; elle se lit et se recolore d'un bloc."""
    me = o.data
    uv = me.uv_layers.active
    stored = me.attributes.get('thunk_kit_mat')
    n = len(me.polygons)
    parent = list(range(n))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    box, slot = [], []
    for p in me.polygons:
        us = [uv.data[i].uv for i in p.loop_indices] if uv else []
        box.append((min(x.x for x in us), max(x.x for x in us), min(x.y for x in us), max(x.y for x in us)) if us
                   else (0.0, 0.0, 0.0, 0.0))
        slot.append(original_slot(o, p, stored))
    co = [tuple(round(c, 4) for c in v.co) for v in me.vertices]
    edges = {}
    for p in me.polygons:
        vs = list(p.vertices)
        for k in range(len(vs)):
            a, b = co[vs[k]], co[vs[(k + 1) % len(vs)]]
            edges.setdefault((a, b) if a <= b else (b, a), []).append(p.index)
    tol = 1.5 / 512      # un pixel et demi de la texture du kit
    for fs in edges.values():
        for i in range(len(fs)):
            for j in range(i + 1, len(fs)):
                a, b = fs[i], fs[j]
                if slot[a] != slot[b] or me.polygons[a].normal.dot(me.polygons[b].normal) < 0.999:
                    continue
                ba, bb = box[a], box[b]
                same_band = abs(ba[0] - bb[0]) < tol and abs(ba[1] - bb[1]) < tol
                overlap = max(ba[2], bb[2]) <= min(ba[3], bb[3]) + tol
                if same_band and overlap:
                    parent[find(a)] = find(b)
    return {i: find(i) for i in range(n)}, box, slot


def face_colors(o):
    """La couleur Kenney d'origine de chaque face d'une pièce de kit : {index de la face : (r, g, b)} ; les faces d'un autre
    matériau n'y sont pas. Lue par face visible (face_regions) : la moyenne de la texture du kit sur le bout de bande qu'elle
    couvre, la même pour tous ses triangles."""
    me = o.data
    out = {}
    if me.uv_layers.active is None:
        return out
    region, box, slot = face_regions(o)
    groups = {}
    for i, r in region.items():
        groups.setdefault(r, []).append(i)
    for faces in groups.values():
        idx = slot[faces[0]]
        mat = o.material_slots[idx].material if idx < len(o.material_slots) else None
        img = kit_image(mat)
        if img is None:
            continue
        px = image_pixels(img)
        if px is None:
            continue
        arr, w, h = px
        u0 = min(box[i][0] for i in faces); u1 = max(box[i][1] for i in faces)
        v0 = min(box[i][2] for i in faces); v1 = max(box[i][3] for i in faces)
        if 0.0 <= u0 and u1 <= 1.0 and 0.0 <= v0 and v1 <= 1.0:
            c0, c1 = min(int(u0 * w), w - 1), min(int(u1 * w), w - 1)
            r0, r1 = min(int(v0 * h), h - 1), min(int(v1 * h), h - 1)
            c = arr[r0:r1 + 1, c0:c1 + 1, :3].reshape(-1, 3).mean(axis=0)
        else:       # des coordonnées hors de la texture (elle se répète) : le centre
            c = arr[int((((v0 + v1) / 2) % 1.0) * h) % h, int((((u0 + u1) / 2) % 1.0) * w) % w]
        col = (float(c[0]), float(c[1]), float(c[2]))
        for i in faces:
            out[i] = col
    return out


def reunir_faces_coupees(objs, merge=None):
    """Les faces visibles recolorées à moitié avant le 15 novembre (face_regions) : toute la face prend la couleur que le joueur
    a donnée aux autres faces de la même teinte Kenney (lue comme le bouton « Lire », par face visible), celle qui en couvre le
    plus. Une moitié recolorée par erreur revient ainsi à Kenney si le reste de sa teinte est resté Kenney, et l'inverse.
    Rend le nombre de triangles changés."""
    merge = merge or merge_threshold(bpy.context)
    reps, regions = [], []      # regions : (objet, triangles, numéro de teinte, coupée ?)
    for o in objs:
        if o.type != 'MESH' or o.library is not None or o.data.attributes.get('thunk_kit_mat') is None:
            continue
        colors = face_colors(o)
        region, box, slot = face_regions(o)
        groups = {}
        for i, r in region.items():
            if i in colors:
                groups.setdefault(r, []).append(i)
        for faces in groups.values():
            k, d = nearest(colors[faces[0]], reps)
            if k is None or d >= merge:
                reps.append(colors[faces[0]])
                k = len(reps) - 1
            mixed = len({o.data.polygons[i].material_index for i in faces}) > 1
            regions.append((o, faces, k, mixed, slot))
    # ce que le joueur a donné à chaque teinte : le matériau qui en couvre le plus (hors faces coupées) ; None : Kenney
    def choice(o, i):
        return None if o.data.attributes['thunk_kit_mat'].data[i].value < 0 else o.material_slots[o.data.polygons[i].material_index].material
    area = {}
    for o, faces, k, mixed, slot in regions:
        if not mixed:
            for i in faces:
                key = (k, choice(o, i))
                area[key] = area.get(key, 0.0) + o.data.polygons[i].area
    n = 0
    for o, faces, k, mixed, slot in regions:
        if not mixed:
            continue
        cands = {m: a for (kk, m), a in area.items() if kk == k}
        if not cands:       # toute la teinte est coupée : la plus grande partie de la face
            for i in faces:
                cands[choice(o, i)] = cands.get(choice(o, i), 0.0) + o.data.polygons[i].area
        best = max(cands, key=cands.get)
        me = o.data
        stored = me.attributes['thunk_kit_mat']
        target = None
        if best is not None:
            names = [s.material for s in o.material_slots]
            if best not in names:
                me.materials.append(best)
                names.append(best)
            target = names.index(best)
        for i in faces:
            p = me.polygons[i]
            if best is None and stored.data[i].value >= 0:
                p.material_index = stored.data[i].value
                stored.data[i].value = -1
                n += 1
            elif best is not None and p.material_index != target:
                if stored.data[i].value < 0:
                    stored.data[i].value = slot[i]
                p.material_index = target
                n += 1
        me.update()
    return n


def nearest(c, colors):
    best, bd = None, 9.0
    for i, k in enumerate(colors):
        d = max(abs(c[0] - k[0]), abs(c[1] - k[1]), abs(c[2] - k[2]))
        if d < bd:
            best, bd = i, d
    return best, bd


def kit_objects(context):
    col = bpy.data.collections.get(MODEL)
    inside = set(col.all_objects) if col else set()
    return [o for o in context.selected_objects if o.type == 'MESH' and o in inside and o.library is None]


def read_objects(scene):
    """Les pièces dont on a lu les couleurs (le choix s'applique à elles, même si la sélection a changé depuis)."""
    try:
        names = json.loads(scene.thunk_kit_objets or '[]')
    except Exception:
        names = []
    return [o for o in (local_object(n) for n in names) if o is not None and o.type == 'MESH']


def material_for(item):
    """Le matériau du choix d'une couleur : None (garder), un rôle de la palette, ou « Couleur #rrggbb » ; « Transparent »
    devant, « lueur » derrière (les règles des noms du branchement dans Unity)."""
    c = item.choix
    if c == 'GARDER':
        return None
    roles = list(palette_roles())
    if c.startswith('R'):
        name, rgb = roles[int(c[1:])], None
    else:
        h = kit_colors()[int(c[1:])] if c.startswith('K') else hexa(item.libre)
        name, rgb = 'Couleur %s' % h, from_hex(h)
    if item.transparent and not name.lower().startswith('transparent'):
        name = 'Transparent ' + name
    if item.lueur and 'lueur' not in name.lower():
        name = name + ' lueur'
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        if rgb is None:      # un rôle avec Transparent ou lueur ajoutés : la couleur du rôle
            rgb = from_hex(palette_roles()[roles[int(c[1:])]]['couleur'])
        lin = [srgb_to_linear(x) for x in rgb]
        bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
        bsdf.inputs['Base Color'].default_value = lin + [1.0]
        bsdf.inputs['Roughness'].default_value = 0.7
        mat.diffuse_color = lin + [1.0]
        if 'lueur' in name.lower():
            bsdf.inputs['Emission Color'].default_value = lin + [1.0]
            bsdf.inputs['Emission Strength'].default_value = 2.0
    appliquer_palette()
    preparer_transparents()
    return mat


def appliquer_groupe(item, context):
    """Applique tout de suite le choix d'une couleur à ses faces (dans les pièces lues)."""
    scene = context.scene
    reps = [tuple(it.couleur) for it in scene.thunk_kit_colors]
    me_index = list(scene.thunk_kit_colors).index(item)
    mat = material_for(item)
    limit = merge_threshold(context) * 2
    for o in read_objects(scene):
        if o.data.users > 1:
            o.data = o.data.copy()       # une pièce à elle : les autres copies du maillage ne changent pas
        me = o.data
        stored = me.attributes.get('thunk_kit_mat')
        if stored is None:
            stored = me.attributes.new('thunk_kit_mat', 'INT', 'FACE')
            for p in me.polygons:
                stored.data[p.index].value = -1      # -1 : la face a sa couleur d'origine
            stored = me.attributes.get('thunk_kit_mat')
        slot = None
        if mat is not None:
            names = [s.material.name if s.material else '' for s in o.material_slots]
            if mat.name not in names:
                me.materials.append(mat)
                names.append(mat.name)
            slot = names.index(mat.name)
        for idx, c in face_colors(o).items():
            i, d = nearest(c, reps)
            if i != me_index or d >= limit:
                continue
            p = me.polygons[idx]
            original = stored.data[idx].value if stored.data[idx].value >= 0 else p.material_index
            if slot is None:
                p.material_index = original
                stored.data[idx].value = -1
            else:
                stored.data[idx].value = original
                p.material_index = slot
        me.update()
    retirer_couleurs_vides(read_objects(scene))


def retirer_couleurs_vides(objs):
    """Les emplacements de matériau qu'aucune face n'utilise plus (une couleur essayée puis changée : la recoloration est
    instantanée, 15 novembre) : retirés, pour qu'ils ne partent pas dans le FBX (des matériaux inutiles dans Unity). La couleur
    Kenney d'origine d'une face recolorée (thunk_kit_mat) reste. Rien ne change à l'écran."""
    done, n = set(), 0
    for o in objs:
        if o.type != 'MESH' or o.library is not None or o.data.library is not None or o.data in done:
            continue
        me = o.data
        done.add(me)
        if any(s.link == 'OBJECT' for s in o.material_slots):
            continue
        stored = me.attributes.get('thunk_kit_mat')
        used = {p.material_index for p in me.polygons}
        if stored is not None:
            used |= {v.value for v in stored.data if v.value >= 0}
        mats = list(me.materials)
        keep = [i for i in range(len(mats)) if i in used]
        if len(keep) == len(mats) or not keep:
            continue
        remap = {old: new for new, old in enumerate(keep)}
        idx = [remap.get(p.material_index, 0) for p in me.polygons]
        orig = [remap.get(v.value, -1) if v.value >= 0 else -1 for v in stored.data] if stored is not None else None
        n += len(mats) - len(keep)
        me.materials.clear()
        for i in keep:
            me.materials.append(mats[i])
        me.polygons.foreach_set('material_index', idx)
        if orig is not None:
            me.attributes.get('thunk_kit_mat').data.foreach_set('value', orig)
        me.update()
    return n


def current_choice(index, objs, reps, limit):
    """Ce qui est appliqué aujourd'hui aux faces d'une couleur (pour relire une pièce déjà recolorée) : le nom du matériau le
    plus fréquent, ou '' (la couleur Kenney d'origine)."""
    from collections import Counter
    seen = Counter()
    for o in objs:
        stored = o.data.attributes.get('thunk_kit_mat')
        for idx, c in face_colors(o).items():
            i, d = nearest(c, reps)
            if i == index and d < limit:
                p = o.data.polygons[idx]
                if stored is not None and stored.data[idx].value >= 0 and p.material_index < len(o.material_slots):
                    m = o.material_slots[p.material_index].material
                    seen[m.name if m else ''] += 1
                else:
                    seen[''] += 1
    return seen.most_common(1)[0][0] if seen else ''


class THUNK_OT_lire_couleurs(bpy.types.Operator):
    bl_idname = 'thunk.lire_couleurs'
    bl_label = 'Lire les couleurs Kenney de la sélection'
    bl_description = 'Montrer les couleurs du kit des pièces sélectionnées, pour en changer'

    def execute(self, context):
        scene = context.scene
        items = scene.thunk_kit_colors
        objs = kit_objects(context)
        reps, counts = [], []
        merge = merge_threshold(context)
        for o in objs:
            for c in face_colors(o).values():
                i, d = nearest(c, reps)
                if i is not None and d < merge:
                    counts[i] += 1
                else:
                    reps.append(c)
                    counts.append(1)
        if not reps:
            self.report({'WARNING'}, 'Sélectionne une pièce de Kenney (dans Modele), puis recommence.')
            return {'CANCELLED'}
        roles = list(palette_roles())
        kits = kit_colors()
        _busy[0] = True
        try:
            items.clear()
            scene.thunk_kit_objets = json.dumps([o.name for o in objs], ensure_ascii=False)
            ordered = sorted(zip(reps, counts), key=lambda x: -x[1])
            all_reps = [c for c, n in ordered]
            for k, (c, n) in enumerate(ordered):
                it = items.add()
                it.couleur = c
                it.faces = n
                it.libre = c
                # ce qui est déjà appliqué à ces faces, s'il y a
                name = re.sub(r'\.\d{3}$', '', current_choice(k, objs, all_reps, merge * 2))
                it.transparent = name.lower().startswith('transparent')
                it.lueur = name.lower().endswith(' lueur') or name.lower().startswith('lueur')
                core = re.sub(r'^transparent\s+', '', re.sub(r'\s+lueur$', '', name, flags=re.I), flags=re.I)
                if core in roles:
                    it.choix = 'R%d' % roles.index(core)
                elif name in roles:
                    it.choix = 'R%d' % roles.index(name)
                elif core.startswith('Couleur #'):
                    h = core[len('Couleur '):]
                    if h in kits:
                        it.choix = 'K%d' % kits.index(h)
                    else:
                        it.libre = from_hex(h)
                        it.choix = 'LIBRE'
        finally:
            _busy[0] = False
        self.report({'INFO'}, '%d couleurs.' % len(reps))
        return {'FINISHED'}


class THUNK_OT_couleurs_kenney(bpy.types.Operator):
    bl_idname = 'thunk.couleurs_kenney'
    bl_label = 'Remettre les couleurs Kenney'
    bl_description = 'Rendre aux faces recolorées des pièces lues leur couleur du kit'
    bl_options = {'REGISTER', 'UNDO'}

    def execute(self, context):
        n = 0
        for o in read_objects(context.scene) or kit_objects(context):
            stored = o.data.attributes.get('thunk_kit_mat')
            if stored is None:
                continue
            for p in o.data.polygons:
                v = stored.data[p.index].value
                if v >= 0:
                    p.material_index = v
                    stored.data[p.index].value = -1
                    n += 1
            o.data.update()
        _busy[0] = True
        try:
            for it in context.scene.thunk_kit_colors:
                it.choix = 'GARDER'
                it.transparent = False
                it.lueur = False
        finally:
            _busy[0] = False
        self.report({'INFO'}, '%d faces revenues aux couleurs Kenney.' % n)
        return {'FINISHED'}


class THUNK_PT_panel(bpy.types.Panel):
    bl_label = 'THUNK VR'
    bl_idname = 'THUNK_PT_panel'
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = 'THUNK'

    def draw(self, context):
        lay = self.layout
        data = info(context.scene)
        if data is None:
            lay.label(text='Fichier non préparé pour THUNK.', icon='ERROR')
            lay.label(text='Tools/refonte_visuelle/preparer.py')
            return
        lay.label(text='%s (%s)' % (data.get('titre', ''), data['batiment']))
        lay.label(text='%d niveaux ; chez Claude %d × %d cases' % (data['niveaux'], data['cases'][0], data['cases'][1]))
        minutes = int(context.scene.get('thunk_minutes', 0))
        lay.label(text='Temps de travail : %d h %02d' % (minutes // 60, minutes % 60), icon='TIME')
        if data['niveaux'] > 1:
            box = lay.box()
            active = context.active_object
            reperes = {local_object(r['cle']) for r in data['reperes']}
            if active is not None and active in reperes:
                box.label(text='« %s » : un repère du code' % active.name, icon='EMPTY_AXIS')
            elif active is not None:
                box.label(text='« %s » : niveau %d' % (active.name, level_number(active)), icon='OBJECT_DATA')
            box.label(text='Mettre la sélection au niveau :')
            row = box.row(align=True)
            for n in range(1, data['niveaux'] + 1):
                row.operator('thunk.niveau', text=str(n)).niveau = n
        row = lay.row(align=True)
        row.operator('thunk.verifier', icon='CHECKMARK')
        row.operator('thunk.exporter', icon='EXPORT')
        lay.prop(context.scene, 'thunk_auto')
        if linked_parts():
            lay.operator('thunk.localiser', icon='LIBRARY_DATA_DIRECT')
        if outside_pieces():
            lay.operator('thunk.ranger', icon='OUTLINER_COLLECTION')
        box = lay.box()
        box.label(text='Couleurs des pièces Kenney', icon='COLOR')
        box.prop(context.scene, 'thunk_kit_nuances')
        box.operator('thunk.lire_couleurs', icon='EYEDROPPER')
        items = context.scene.thunk_kit_colors
        if items:
            for it in items:
                row = box.row(align=True)
                orig = row.row(align=True)
                orig.enabled = False
                orig.prop(it, 'couleur', text='')
                row.label(text='', icon='FORWARD')
                row.template_icon_view(it, 'choix', show_labels=False, scale=1.0, scale_popup=3.0)
                row.prop(it, 'libre', text='')
                row.prop(it, 'transparent', text='', icon='IMAGE_ALPHA')
                row.prop(it, 'lueur', text='', icon='LIGHT_SUN')
                row.label(text='%d' % it.faces)
            box.label(text="La couleur choisie s'applique tout de suite.", icon='INFO')
            box.operator('thunk.couleurs_kenney', icon='LOOP_BACK')
        col = lay.column(align=True)
        col.operator('thunk.decaler_claude', icon='ARROW_LEFTRIGHT')
        col.operator('thunk.voir_claude', icon='HIDE_OFF')
        try:
            msgs = json.loads(context.window_manager.thunk_rapport or '[]')
        except Exception:
            msgs = []
        if msgs:
            box = lay.box()
            for level, text in msgs:
                words, line, first = text.split(' '), '', True
                for w in words:   # le panneau ne passe pas à la ligne tout seul
                    if len(line) + len(w) > 46:
                        box.label(text=line, icon=ICONS.get(level, 'DOT') if first else 'BLANK1')
                        line, first = w, False
                    else:
                        line = (line + ' ' + w).strip()
                if line:
                    box.label(text=line, icon=ICONS.get(level, 'DOT') if first else 'BLANK1')


# ---------------------------------------------------------------- l'export à chaque Ctrl+S, la bibliothèque, le temps

@persistent
def on_save(_):
    scene = bpy.context.scene
    if info(scene) is None or not scene.thunk_auto:
        return
    ok, msgs = exporter(scene)
    store(msgs)
    print('THUNK : %s' % (msgs[-1][1] if ok else 'pas exporté (voir l\'onglet THUNK)'))


def register_library():
    root = project_dir()
    if root is None:
        return
    path = os.path.join(root, 'Modeles_Blender', 'Bibliotheque')
    if not os.path.isdir(path):
        return
    libs = bpy.context.preferences.filepaths.asset_libraries
    lib = next((l for l in libs if os.path.normcase(os.path.abspath(bpy.path.abspath(l.path))) == os.path.normcase(path)), None)
    if lib is None:
        try:
            lib = libs.new(name=LIB_NAME, directory=path)
        except Exception:
            bpy.ops.preferences.asset_library_add(directory=path)
            lib = libs[-1]
            lib.name = LIB_NAME
        print('THUNK : bibliothèque des kits déclarée (%s)' % path)
    # « Ajouter » : une vraie copie, qu'on peut modifier (Tab) et joindre (Ctrl+J) ; Blender 5 « empaquette » par défaut, et
    # une pièce empaquetée ne se modifie pas (le joueur, 14 novembre)
    if lib.import_method != 'APPEND':
        lib.import_method = 'APPEND'
        print('THUNK : bibliothèque des kits en « Ajouter »')


@persistent
def on_load(_):
    if info() is None:
        return
    register_library()
    appliquer_palette()
    # ce qu'on ajoute (une forme, une pièce glissée de la bibliothèque) va dans la collection active : Modele
    for lc in bpy.context.view_layer.layer_collection.children:
        if lc.collection.name == MODEL:
            bpy.context.view_layer.active_layer_collection = lc


_visibility = {}


def eye_tick():
    """L'œil d'un repère (un niveau, les cristaux…) cache aussi les pièces qui lui sont rattachées (le joueur, 14 novembre ;
    Blender ne le fait qu'avec Maj + clic). Seulement quand on clique sur son œil : une pièce se cache encore seule."""
    try:
        if info() is None:
            return 0.5
        col = bpy.data.collections.get(MODEL)
        layer = bpy.context.view_layer
        for o in (col.all_objects if col else []):
            if o.type != 'EMPTY' or not o.children:
                continue
            key = o.as_pointer()
            hidden = o.hide_get(view_layer=layer)
            if key in _visibility and _visibility[key] != hidden:
                for c in o.children_recursive:
                    try:
                        c.hide_set(hidden, view_layer=layer)
                    except RuntimeError:
                        pass
            _visibility[key] = hidden
    except Exception:
        pass
    return 0.25


_last = [None]


def tick():
    try:
        wm = bpy.context.window_manager
        scene = bpy.context.scene
        ops = wm.operators
        sig = (len(ops), ops[-1].as_pointer() if len(ops) else 0)
        if info(scene) is not None and _last[0] is not None and sig != _last[0]:
            scene['thunk_minutes'] = int(scene.get('thunk_minutes', 0)) + 1
        _last[0] = sig
    except Exception:
        pass
    return 60.0


CLASSES = (THUNK_KitColor, THUNK_OT_verifier, THUNK_OT_exporter, THUNK_OT_niveau, THUNK_OT_localiser, THUNK_OT_ranger,
           THUNK_OT_lire_couleurs, THUNK_OT_couleurs_kenney, THUNK_OT_decaler, THUNK_OT_voir,
           THUNK_PT_panel)


def register():
    for c in CLASSES:
        bpy.utils.register_class(c)
    bpy.types.Scene.thunk_kit_colors = bpy.props.CollectionProperty(type=THUNK_KitColor)
    bpy.types.Scene.thunk_kit_objets = bpy.props.StringProperty(default='')
    bpy.types.Scene.thunk_kit_nuances = bpy.props.BoolProperty(
        name='Séparer les nuances', description='Plus de couleurs à choisir (les nuances proches d\'une couleur du kit à part)',
        default=False)
    bpy.types.Scene.thunk_auto = bpy.props.BoolProperty(
        name='Exporter à chaque Ctrl+S', description='Exporter vers Unity à chaque enregistrement, si la vérification passe',
        default=False)
    bpy.types.WindowManager.thunk_rapport = bpy.props.StringProperty(default='')
    bpy.app.handlers.save_post.append(on_save)
    bpy.app.handlers.load_post.append(on_load)
    if not bpy.app.timers.is_registered(tick):
        bpy.app.timers.register(tick, first_interval=60.0, persistent=True)
    if not bpy.app.timers.is_registered(eye_tick):
        bpy.app.timers.register(eye_tick, first_interval=1.0, persistent=True)


def unregister():
    if bpy.app.timers.is_registered(tick):
        bpy.app.timers.unregister(tick)
    if bpy.app.timers.is_registered(eye_tick):
        bpy.app.timers.unregister(eye_tick)
    for h, f in ((bpy.app.handlers.save_post, on_save), (bpy.app.handlers.load_post, on_load)):
        if f in h:
            h.remove(f)
    del bpy.types.Scene.thunk_auto
    del bpy.types.Scene.thunk_kit_colors
    del bpy.types.Scene.thunk_kit_nuances
    del bpy.types.Scene.thunk_kit_objets
    global _icons
    if _icons is not None:
        bpy.utils.previews.remove(_icons)
        _icons = None
    del bpy.types.WindowManager.thunk_rapport
    for c in reversed(CLASSES):
        bpy.utils.unregister_class(c)
