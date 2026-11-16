# Les corrections techniques d'une tour faite main (14 novembre ; Docs/Refonte_Visuelle.md, « Après chaque tour »), dans son
# fichier Blender, Blender fermé (ou le fichier enregistré, puis « Fichier > Revenir » dans Blender). Une copie du fichier est
# gardée avant (<Nom>_avant_corrections_<date>.blend). Seulement de la mise au point, jamais l'allure :
# - --jours : les jours horizontaux de 2 à 30 mm entre deux pièces posées l'une sur l'autre (une fente visible de côté) se
#   ferment : tout ce qui est au-dessus descend de l'écart ;
# - --echelle F : tout le modèle (la collection Modele, repères compris) agrandi de F autour de l'origine (pour retrouver la
#   taille du modèle de Claude) ;
# - --separer REPERE COULEUR ZMIN : les morceaux d'une pièce jointe de cette couleur de kit (« violet », « bleu ») au-dessus
#   de ZMIN deviennent une pièce à part, rattachée au repère du code, posé à leur base (le Givre : ses cristaux) ;
# - --faces : les faces des pièces de kit recolorées à moitié par la recoloration d'avant le 15 novembre (un triangle d'un
#   carré d'une couleur, l'autre d'une autre) : toute la face prend la couleur donnée à sa teinte Kenney (l'extension THUNK,
#   reunir_faces_coupees ; les teintes lues nuances séparées, comme le joueur les avait lues) ;
# - --rattacher PIECE REPERE : la pièce rattachée au repère du code, sans bouger (le canon du Mortier à « Affût », qui tourne
#   pour viser ; 16 novembre) ;
# - --bouche REPERE PIECE X Y Z : le repère posé au point (X, Y, Z) du repère local de la pièce (la bouche d'un canon de
#   Kenney, weapon-cannon : 0 -0.30 0.352 ; d'où partent les obus) ;
# - --ajouter-repere CLE CHEMIN CHAMP : un repère du code nouveau (un objet vide ajouté au prefab par un baker), CHEMIN son
#   chemin dans le prefab (« Affût/Niveau 3/Tube/Bouche 3 »), CHAMP le champ du code (m_Muzzles) ; à poser ensuite
#   (--bouche, --rattacher) ; 16 novembre, la troisième bouche du Mortier ;
# - --repeindre PIECE DE VERS : les faces de la couleur DE prennent la couleur VERS (un matériau du fichier), à la demande
#   du joueur (16 novembre : une face du Mortier peinte par erreur) ;
# - --retirer-faces PIECE COULEUR : les faces de cette couleur retirées, à la demande du joueur (des faces qui ne se voient
#   jamais : le dessus et le dessous de la base du Mortier, en transparent) ; la couleur sort aussi de la pièce ;
# - --tube PIECE ELEVATION : le tube d'un canon de Kenney (weapon-cannon : ses morceaux au-dessus de l'axe, au milieu)
#   détaché en une pièce « <PIECE> tube », rattachée au canon, son origine sur l'axe, levé de ELEVATION degrés (16 novembre,
#   le joueur : le Mortier, « plus comme un vrai mortier qu'un canon » ; le code le fait bouger, Building/BarrelPitch) ;
# - --vers-dehors PIECE : la pièce tournée sur elle-même pour regarder à l'opposé du centre de la tour (les petits canons
#   du niveau 5 du Mortier, chacun vers son coin ; le joueur, 16 novembre) ;
# - --faces-nulles : les faces d'aire nulle (des arêtes écrasées en modelant) retirées ; rien ne change à l'écran ;
# - --deplacer PIECE DX DY DZ : la pièce déplacée de quelques centimètres (dans le monde), pour séparer deux pièces qui
#   s'enfoncent l'une dans l'autre avec des faces superposées (deux tonneaux côte à côte, les deux canons du niveau 3) ;
# - --exporter : puis l'export vers Unity, comme le bouton de l'onglet THUNK.
#   blender --background --python Tools/blender/corriger.py -- Modeles_Blender/Givre.blend --jours --exporter
import bpy, bmesh, sys, os, shutil, datetime
import numpy as np
from mathutils import Vector, Matrix, Euler

args = sys.argv[sys.argv.index('--') + 1:]
path = os.path.abspath(args[0])
stamp = datetime.datetime.now().strftime('%Y%m%d_%H%M%S')
backup = path[:-6] + '_avant_corrections_%s.blend' % stamp
shutil.copy(path, backup)
bpy.ops.wm.open_mainfile(filepath=path)
model = bpy.data.collections['Modele']
log = []


def meshes():
    return [o for o in model.all_objects if o.type == 'MESH' and o.library is None]


def world_tris():
    """Les triangles de toutes les pièces, dans le monde : (objet, index de face, sommets, normale)."""
    out = []
    for o in meshes():
        me = o.data
        me.calc_loop_triangles()
        M = o.matrix_world
        N3 = M.to_3x3().inverted().transposed()
        for t in me.loop_triangles:
            vs = [M @ me.vertices[i].co for i in t.vertices]
            n = (N3 @ t.normal).normalized()
            out.append((o, t.polygon_index, vs, n))
    return out


def overlap_xy(a, b):
    """Les boîtes de deux triangles horizontaux se recouvrent-elles (sur le plan) d'au moins 1 cm ?"""
    ax = [v.x for v in a]; ay = [v.y for v in a]; bx = [v.x for v in b]; by = [v.y for v in b]
    return min(max(ax), max(bx)) - max(min(ax), min(bx)) > 0.01 and min(max(ay), max(by)) - max(min(ay), min(by)) > 0.01


def find_gap():
    """Le plus bas des jours horizontaux : (hauteur du haut de la pièce du dessous, écart)."""
    tris = world_tris()
    ups = [(t, t[2][0].z) for t in tris if t[3].z > 0.999]      # faces tournées vers le haut
    downs = [(t, t[2][0].z) for t in tris if t[3].z < -0.999]   # faces tournées vers le bas
    best = None
    for (tu, zu) in ups:
        for (td, zd) in downs:
            g = zd - zu
            if 0.002 < g < 0.03 and overlap_xy(tu[2], td[2]):
                if best is None or zu < best[0]:
                    best = (zu, g)
    return best


def islands(bm):
    seen, out = set(), []
    for f in bm.faces:
        if f.index in seen:
            continue
        stack, isl = [f], []
        seen.add(f.index)
        while stack:
            x = stack.pop()
            isl.append(x)
            for e in x.edges:
                for y in e.link_faces:
                    if y.index not in seen:
                        seen.add(y.index)
                        stack.append(y)
        out.append(isl)
    return out


def lower_above(z, g):
    """Descend de g tout ce qui commence au-dessus de z (par morceaux : un morceau d'une pièce jointe peut rester en bas)."""
    moved = 0
    for o in meshes():
        if o.data.users > 1:
            log.append('  %s : maillage partagé, pas touché' % o.name)
            continue
        M = o.matrix_world
        local = M.inverted().to_3x3() @ Vector((0, 0, -g))
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bm.faces.ensure_lookup_table()
        changed = False
        for isl in islands(bm):
            verts = {v for f in isl for v in f.verts}
            if min((M @ v.co).z for v in verts) >= z + g - 0.0005:
                for v in verts:
                    v.co += local
                changed = True
                moved += 1
        if changed:
            bm.to_mesh(o.data)
            o.data.update()
        bm.free()
    return moved


def jours():
    for _ in range(12):
        gap = find_gap()
        if gap is None:
            break
        z, g = gap
        n = lower_above(z, g)
        log.append('jour de %.1f mm à %.2f m fermé (%d morceaux descendus)' % (g * 1000, z, n))
    jonctions_transparentes()


def transparent(o):
    return any(s.material and s.material.name.lower().startswith('transparent') for s in o.material_slots)


def jonctions_transparentes():
    """Deux pièces transparentes posées face contre face (les deux cônes du grand cristal) : leurs faces collées se verraient
    au travers, comme une vitre à l'intérieur ; on les retire toutes les deux."""
    tris = [t for t in world_tris() if transparent(t[0]) and abs(t[3].z) > 0.999]
    kill = {}
    for a in tris:
        for b in tris:
            if a[0] is b[0] or a[3].z <= 0 or b[3].z >= 0:
                continue
            if abs(a[2][0].z - b[2][0].z) < 0.0015 and overlap_xy(a[2], b[2]):
                kill.setdefault(a[0], set()).add(a[1])
                kill.setdefault(b[0], set()).add(b[1])
    for o, faces in kill.items():
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.faces[i] for i in faces], context='FACES_ONLY')
        bm.to_mesh(o.data)
        o.data.update()
        bm.free()
        log.append('« %s » : %d face(s) collée(s) à une autre pièce transparente retirée(s) (on les verrait au travers)' % (
            o.name, len(faces)))


# les couleurs des kits (lues dans leur texture ; les cristaux du kit Tower Defense : 0,76 0,47 0,92)
COLORS = {'violet': lambda c: c[2] > 0.6 and c[0] > 0.45 and c[2] - c[1] > 0.25, 'bleu': lambda c: c[2] > 0.6 and c[0] < 0.5}
PROJECT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
KIT_TEXTURES = {  # pour lire les couleurs des pièces des kits (les mêmes que Tools/blender/bibliotheque.py)
    'Kenney TD': 'Assets/kenney_tower-defense-kit/Models/FBX format/Textures/colormap.png',
    'Kenney Chateau': 'Assets/kenney_castle-kit/Models/FBX format/Textures/colormap.png',
    'Kenney Village': 'Assets/kenney_fantasy-town-kit/Models/FBX format/Textures/colormap.png',
}


def kit_image(mats):
    import re
    for m in mats:
        if m is None:
            continue
        rel = KIT_TEXTURES.get(re.sub(r'\.\d{3}$', '', m.name))
        if rel:
            return bpy.data.images.load(os.path.join(PROJECT, rel), check_existing=True)
    return None


def level_of(o):
    p = o
    while p is not None:
        if p.name.startswith('Niveau '):
            return p.name
        p = p.parent
    return None


def separer(repere, couleur, zmin):
    rep = next(o for o in model.all_objects if o.name == repere and o.library is None)
    test = COLORS[couleur]
    made = []
    for o in meshes():
        # seulement les pièces du niveau 1 : celles d'un niveau y restent (le code n'agrandit que le repère)
        if o.parent == rep or level_of(o) is not None:
            continue
        img = kit_image([s.material for s in o.material_slots])
        if img is None or img.size[0] == 0:
            continue
        w, h = img.size
        px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
        bm = bmesh.new()
        bm.from_mesh(o.data)
        uv = bm.loops.layers.uv.active
        M = o.matrix_world
        take = []
        for isl in islands(bm):
            if min((M @ v.co).z for f in isl for v in f.verts) < zmin or uv is None:
                continue
            cols = []
            for f in isl:
                c = sum((l[uv].uv for l in f.loops), Vector((0, 0))) / len(f.loops)
                x, y = int((c.x % 1.0) * w) % w, int((c.y % 1.0) * h) % h
                cols.append(px[y, x, :3])
            if test(np.mean(cols, axis=0)):
                take += isl
        if not take:
            bm.free()
            continue
        # la nouvelle pièce : une copie de l'objet, réduite à ces morceaux ; l'objet d'origine les perd
        keep = {f.index for f in take}
        new = o.copy()
        new.data = o.data.copy()
        new.name = 'Cristaux'
        model.objects.link(new)
        bm2 = bmesh.new()
        bm2.from_mesh(new.data)
        bm2.faces.ensure_lookup_table()
        bmesh.ops.delete(bm2, geom=[f for f in bm2.faces if f.index not in keep], context='FACES')
        bm2.to_mesh(new.data)
        bm2.free()
        bm.faces.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.index in keep], context='FACES')
        bm.to_mesh(o.data)
        bm.free()
        made.append((new, o.name, len(take)))
    if not made:
        log.append('aucun morceau %s au-dessus de %.2f m au niveau 1 : rien de séparé' % (couleur, zmin))
        return
    # le repère une seule fois, à la base de tous les morceaux, au centre ; puis les pièces rattachées sans bouger
    pts = [n.matrix_world @ v.co for n, _, _ in made for v in n.data.vertices]
    base = Vector(((min(p.x for p in pts) + max(p.x for p in pts)) / 2, (min(p.y for p in pts) + max(p.y for p in pts)) / 2,
                   min(p.z for p in pts)))
    worlds = [(n, n.matrix_world.copy()) for n, _, _ in made]
    rep.matrix_world.translation = base
    bpy.context.view_layer.update()
    for n, w in worlds:
        n.parent = rep
        n.matrix_world = w
    for n, src, count in made:
        log.append('%d faces %s de « %s » séparées en « %s », rattachées à « %s », posé à leur base (%.2f, %.2f, %.2f)' % (
            count, couleur, src, n.name, repere, base.x, base.y, base.z))


def couleurs_en_double():
    """« Transparent glace.001 » identique à « Transparent glace » (une pièce dupliquée) : rangée dans l'original."""
    import re
    def key(m):
        b = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None) if m.node_tree else None
        if b is None:
            return None
        return tuple(round(x, 4) for x in b.inputs['Base Color'].default_value) + (round(b.inputs['Alpha'].default_value, 4),)
    for m in list(bpy.data.materials):
        base = re.sub(r'\.\d{3}$', '', m.name)
        target = bpy.data.materials.get(base)
        if base != m.name and target is not None and not m.library and key(m) is not None and key(m) == key(target):
            m.user_remap(target)
            bpy.data.materials.remove(m)
            log.append('couleur « %s » identique à « %s » : rangée dedans' % (m.name if False else base + '.…', base))


i = 1
export = False
while i < len(args):
    a = args[i]
    if a == '--jours':
        jours()
        couleurs_en_double()
    elif a == '--echelle':
        f = float(args[i + 1]); i += 1
        for o in model.all_objects:
            if o.parent is None:
                o.matrix_world = __import__('mathutils').Matrix.Scale(f, 4) @ o.matrix_world
        log.append('modèle agrandi de %.3f' % f)
    elif a == '--separer':
        separer(args[i + 1], args[i + 2], float(args[i + 3])); i += 3
    elif a == '--faces':
        bpy.ops.preferences.addon_enable(module='thunk_blender')
        import thunk_blender as tb
        n = tb.reunir_faces_coupees(meshes(), tb.MERGE_FINE)
        tb.retirer_couleurs_vides(meshes())
        log.append('%d triangles de faces coupées remis à la couleur de leur face' % n)
    elif a == '--rattacher':
        piece, rep = bpy.data.objects[args[i + 1]], bpy.data.objects[args[i + 2]]; i += 2
        world = piece.matrix_world.copy()
        piece.parent = rep
        piece.matrix_world = world
        log.append('« %s » rattachée à « %s », sans bouger' % (piece.name, rep.name))
    elif a == '--bouche':
        rep, piece = bpy.data.objects[args[i + 1]], bpy.data.objects[args[i + 2]]
        local = Vector(tuple(float(x) for x in args[i + 3:i + 6])); i += 5
        before = rep.matrix_world.translation.copy()
        m = rep.matrix_world.copy()
        m.translation = piece.matrix_world @ local
        rep.matrix_world = m
        log.append('repère « %s » posé à la bouche de « %s » : (%.2f %.2f %.2f), avant (%.2f %.2f %.2f)' % (
            rep.name, piece.name, *rep.matrix_world.translation, *before))
    elif a == '--deplacer':
        piece = bpy.data.objects[args[i + 1]]
        d = Vector(tuple(float(x) for x in args[i + 2:i + 5])); i += 4
        piece.matrix_world.translation += d
        log.append('« %s » déplacée de (%.1f %.1f %.1f) cm' % (piece.name, d.x * 100, d.y * 100, d.z * 100))
    elif a == '--ajouter-repere':
        import json
        cle, chemin, champ = args[i + 1], args[i + 2], args[i + 3]; i += 3
        data = json.loads(bpy.context.scene['thunk'])
        if not any(r['cle'] == cle for r in data['reperes']):
            segs = [{'nom': s, 'rang': 0} for s in chemin.split('/')]
            data['reperes'].append({'cle': cle, 'nom': segs[-1]['nom'], 'chemin': segs, 'piece': False, 'champs': [champ]})
            bpy.context.scene['thunk'] = json.dumps(data, ensure_ascii=False)
            e = bpy.data.objects.get(cle) or bpy.data.objects.new(cle, None)
            e.empty_display_type = 'ARROWS'
            e.empty_display_size = 0.3
            if e.name not in model.objects:
                model.objects.link(e)
            e['thunk_chemin'] = json.dumps(segs, ensure_ascii=False)
            txt = bpy.data.texts.get('Cahier des charges')
            if txt is not None:
                lines = txt.as_string().split('\n')
                at = max((k for k, l in enumerate(lines) if l.startswith('  - ')), default=len(lines) - 1) + 1
                lines.insert(at, '  - %s (le code : %s ; ajouté par Claude)' % (cle, champ))
                txt.clear()
                txt.write('\n'.join(lines))
            log.append('repère « %s » ajouté (%s, %s)' % (cle, chemin, champ))
    elif a in ('--repeindre', '--retirer-faces'):
        import re
        piece = bpy.data.objects[args[i + 1]]
        me = piece.data
        base = lambda n: re.sub(r'\.\d{3}$', '', n or '')
        names = [base(s.material.name if s.material else '') for s in piece.material_slots]
        src = [k for k, n in enumerate(names) if n == base(args[i + 2])]
        if a == '--repeindre':
            dst = bpy.data.materials[args[i + 3]]; i += 3
            mats = [s.material for s in piece.material_slots]
            if dst not in mats:
                me.materials.append(dst)
                mats.append(dst)
            slot = mats.index(dst)
            n = 0
            for p in me.polygons:
                if p.material_index in src:
                    p.material_index = slot
                    n += 1
            log.append('« %s » : %d face(s) %s repeinte(s) en %s' % (piece.name, n, args[i - 1], dst.name))
        else:
            i += 2
            bm = bmesh.new()
            bm.from_mesh(me)
            gone = [f for f in bm.faces if f.material_index in src]
            bmesh.ops.delete(bm, geom=gone, context='FACES_ONLY')
            bm.to_mesh(me)
            bm.free()
            # leur couleur n'est plus « la couleur d'avant » d'aucune face (la gomme, « Remettre les couleurs Kenney »)
            st = me.attributes.get('thunk_kit_mat')
            if st is not None:
                for d in st.data:
                    if d.value in src:
                        d.value = -1
            me.update()
            log.append('« %s » : %d face(s) %s retirée(s)' % (piece.name, len(gone), args[i]))
    elif a == '--vers-dehors':
        import math
        piece = bpy.data.objects[args[i + 1]]; i += 1
        c = piece.matrix_world.translation
        before = math.degrees(piece.matrix_world.to_euler().z)
        world = piece.matrix_world.copy()
        loc, rot, sca = world.decompose()
        # l'avant d'une pièce de Kenney : -Y ; tournée autour de la verticale pour que -Y regarde à l'opposé du centre
        ang = math.atan2(c.x, -c.y)
        piece.matrix_world = Matrix.LocRotScale(loc, Euler((0, 0, ang)).to_quaternion(), sca)
        log.append('« %s » tournée vers l\'extérieur (%.0f° -> %.0f°)' % (piece.name, before, math.degrees(ang)))
    elif a == '--tube':
        import math
        piece = bpy.data.objects[args[i + 1]]
        elev = float(args[i + 2]); i += 2
        me = piece.data
        bm = bmesh.new()
        bm.from_mesh(me)
        isl = islands(bm)
        tube_faces = set()
        lo = hi = None
        for faces in isl:
            vs = {v for f in faces for v in f.verts}
            xs = [v.co.x for v in vs]; zs = [v.co.z for v in vs]
            if min(xs) < -0.05 and max(xs) > 0.05 and (min(zs) + max(zs)) / 2 > 0.3:
                tube_faces |= {f.index for f in faces}
                zl, zh = min(zs), max(zs)
                lo = zl if lo is None else min(lo, zl)
                hi = zh if hi is None else max(hi, zh)
        bm.free()
        if not tube_faces:
            log.append('« %s » : pas de tube trouvé' % piece.name)
        else:
            pivot = Vector((0.0, 0.0, (lo + hi) / 2))
            if me.users > 1:
                piece.data = me = me.copy()
            tube_me = me.copy()
            tube_me.name = piece.name + ' tube'
            for data, keep in ((me, False), (tube_me, True)):
                b = bmesh.new()
                b.from_mesh(data)
                b.faces.ensure_lookup_table()
                gone = [f for f in b.faces if (f.index in tube_faces) != keep]
                bmesh.ops.delete(b, geom=gone, context='FACES')
                if keep:
                    for v in b.verts:
                        v.co -= pivot
                b.to_mesh(data)
                b.free()
                data.update()
            tube = bpy.data.objects.new(piece.name + ' tube', tube_me)
            for col in piece.users_collection:
                col.objects.link(tube)
            if 'thunk_source' in piece:
                tube['thunk_source'] = piece['thunk_source']
            tube.parent = piece
            tube.matrix_world = piece.matrix_world @ Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(-elev), 4, 'X')
            log.append('« %s » : tube détaché (« %s », %d faces), levé de %.0f°' % (piece.name, tube.name, len(tube_faces), elev))
    elif a == '--faces-nulles':
        n = 0
        for o in meshes():
            bm = bmesh.new()
            bm.from_mesh(o.data)
            before = len(bm.faces)
            bmesh.ops.dissolve_degenerate(bm, dist=1e-5, edges=bm.edges[:])
            n += before - len(bm.faces)
            if before != len(bm.faces):
                bm.to_mesh(o.data)
                o.data.update()
            bm.free()
        log.append('%d faces d\'aire nulle retirées' % n)
    elif a == '--exporter':
        export = True
    i += 1

bpy.ops.wm.save_mainfile()
for l in log:
    print('THUNK ' + l)
if export:
    try:
        bpy.ops.preferences.addon_enable(module='thunk_blender')
        import thunk_blender as tb
        ok, msgs = tb.exporter()
        bpy.ops.wm.save_mainfile()
        for m in msgs:
            print('THUNK export %s : %s' % m)
    except Exception as e:
        print('THUNK export impossible : %s' % e)
print('THUNK sauvegarde d\'avant : %s' % backup)
