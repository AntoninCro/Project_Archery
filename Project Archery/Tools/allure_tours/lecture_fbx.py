# Lecture minimale d'un FBX binaire : maillages (dans le repère de Unity, en mètres), couleurs des faces (texture ou matériau).
import os, struct, zlib, math
import numpy as np

def _parse(data):
    version = struct.unpack_from('<I', data, 23)[0]
    wide = version >= 7500
    head = 25 if wide else 13

    def node(pos):
        if wide:
            end, nprops, plen = struct.unpack_from('<QQQ', data, pos); pos += 24
        else:
            end, nprops, plen = struct.unpack_from('<III', data, pos); pos += 12
        nlen = data[pos]; pos += 1
        if end == 0:
            return None, pos
        name = data[pos:pos + nlen].decode('ascii', 'replace'); pos += nlen
        props = []
        for _ in range(nprops):
            t = chr(data[pos]); pos += 1
            if t == 'Y':
                v = struct.unpack_from('<h', data, pos)[0]; pos += 2
            elif t == 'C':
                v = data[pos] != 0; pos += 1
            elif t == 'I':
                v = struct.unpack_from('<i', data, pos)[0]; pos += 4
            elif t == 'F':
                v = struct.unpack_from('<f', data, pos)[0]; pos += 4
            elif t == 'D':
                v = struct.unpack_from('<d', data, pos)[0]; pos += 8
            elif t == 'L':
                v = struct.unpack_from('<q', data, pos)[0]; pos += 8
            elif t in 'fdlib':
                n, enc, clen = struct.unpack_from('<III', data, pos); pos += 12
                raw = data[pos:pos + clen]; pos += clen
                if enc == 1:
                    raw = zlib.decompress(raw)
                dt = {'f': '<f4', 'd': '<f8', 'l': '<i8', 'i': '<i4', 'b': 'u1'}[t]
                v = np.frombuffer(raw, dtype=dt, count=n)
            elif t in 'SR':
                n = struct.unpack_from('<I', data, pos)[0]; pos += 4
                v = data[pos:pos + n]; pos += n
                if t == 'S':
                    v = v.decode('utf-8', 'replace')
            else:
                raise ValueError('type ' + t)
            props.append(v)
        children = []
        while pos < end:
            if data[pos:pos + head] == b'\0' * head:
                pos += head
                break
            child, pos = node(pos)
            if child is None:
                break
            children.append(child)
        return (name, props, children), end

    pos = 27
    nodes = []
    while pos < len(data) - head:
        n, pos = node(pos)
        if n is None:
            break
        nodes.append(n)
    return nodes

def _find(children, name):
    for c in children:
        if c[0] == name:
            return c
    return None

def _props70(n):
    out = {}
    p = _find(n[2], 'Properties70')
    if p:
        for c in p[2]:
            if c[0] == 'P':
                out[c[1][0]] = c[1][4:]
    return out

def _euler(r, order=0):
    x, y, z = [math.radians(a) for a in r]
    cx, sx, cy, sy, cz, sz = math.cos(x), math.sin(x), math.cos(y), math.sin(y), math.cos(z), math.sin(z)
    Rx = np.array([[1, 0, 0], [0, cx, -sx], [0, sx, cx]])
    Ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]])
    Rz = np.array([[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]])
    return Rz @ Ry @ Rx

def _local(p):
    T = np.eye(4)
    t = p.get('Lcl Translation', (0, 0, 0))
    r = p.get('Lcl Rotation', (0, 0, 0))
    s = p.get('Lcl Scaling', (1, 1, 1))
    pre = p.get('PreRotation', (0, 0, 0))
    post = p.get('PostRotation', (0, 0, 0))
    R = _euler(pre) @ _euler(r) @ np.linalg.inv(_euler(post))
    M = np.eye(4)
    M[:3, :3] = R @ np.diag(s)
    M[:3, 3] = t
    return M

_cache = {}

def load(path, local=False):
    """Renvoie une liste de (sommets Nx3 en mètres dans le repère Unity, triangles Mx3, couleurs Mx3 0..1, nom, UV des faces Mx2
    ou None). local : dans le repère du nœud racine du fichier (celui que remplace la place d'une instance du prefab dans Unity)."""
    key = (path, local)
    if key in _cache:
        return _cache[key]
    data = open(path, 'rb').read()
    nodes = _parse(data)
    gs = _find(nodes, 'GlobalSettings')
    g = _props70(gs) if gs else {}
    unit = g.get('UnitScaleFactor', (1.0,))[0]
    up, upsign = g.get('UpAxis', (1,))[0], g.get('UpAxisSign', (1,))[0]
    front, frontsign = g.get('FrontAxis', (2,))[0], g.get('FrontAxisSign', (1,))[0]
    coord, coordsign = g.get('CoordAxis', (0,))[0], g.get('CoordAxisSign', (1,))[0]
    objs = _find(nodes, 'Objects')[2]
    conns = _find(nodes, 'Connections')[2]
    by_id = {}
    for o in objs:
        if o[1]:
            by_id[o[1][0]] = o
    parent = {}
    children = {}
    for c in conns:
        if c[0] != 'C':
            continue
        kind, a, b = c[1][0], c[1][1], c[1][2]
        children.setdefault(b, []).append(a)
        parent.setdefault(a, []).append(b)
    # textures : chemin du fichier, relié au matériau
    folder = os.path.dirname(path)
    def tex_of(mat_id):
        for cid in children.get(mat_id, []):
            o = by_id.get(cid)
            if o and o[0] == 'Texture':
                f = _find(o[2], 'RelativeFilename') or _find(o[2], 'FileName')
                if f:
                    name = os.path.basename(f[1][0].replace('\\', '/'))
                    for cand in [os.path.join(folder, name), os.path.join(folder, 'Textures', name), os.path.join(folder, '..', 'Textures', name)]:
                        if os.path.exists(cand):
                            return cand
                    return name
        return None
    textures = {}
    def tex_image(p):
        if p not in textures:
            try:
                from PIL import Image
                textures[p] = np.asarray(Image.open(p).convert('RGB'), dtype=np.float32) / 255.0
            except Exception:
                textures[p] = None
        return textures[p]

    roots = [o[1][0] for o in objs if o[0] == 'Model' and o[1] and all(by_id.get(p) is None or by_id[p][0] != 'Model' for p in parent.get(o[1][0], []))]
    root = roots[0] if local is True and len(roots) == 1 else None

    def world(mid):
        if mid == root:
            return np.eye(4)
        M = _local(_props70(by_id[mid]))
        for pid in parent.get(mid, []):
            po = by_id.get(pid)
            if po is not None and po[0] == 'Model':
                return world(pid) @ M
        return M

    # passage au repère de Unity : axes du fichier -> (droite, haut, avant) droitier, puis X inversé
    axes = np.zeros((3, 3))
    axes[0, coord] = coordsign
    axes[1, up] = upsign
    axes[2, front] = frontsign
    conv = np.diag([-1.0, 1.0, 1.0]) @ axes * (unit / 100.0)

    meshes = []
    for o in objs:
        if o[0] != 'Geometry' or len(o[1]) < 3 or o[1][2] != 'Mesh':
            continue
        gid = o[1][0]
        verts = _find(o[2], 'Vertices')
        idx = _find(o[2], 'PolygonVertexIndex')
        if verts is None or idx is None:
            continue
        V = np.array(verts[1][0], dtype=np.float64).reshape(-1, 3)
        I = np.array(idx[1][0], dtype=np.int64)
        # polygones -> triangles (éventail)
        polys = []
        cur = []
        for k, v in enumerate(I):
            if v < 0:
                cur.append((~v, k))
                polys.append(cur)
                cur = []
            else:
                cur.append((v, k))
        # UV
        uvl = _find(o[2], 'LayerElementUV')
        UV = UVI = None
        uvmap = uvref = None
        if uvl:
            UV = np.array(_find(uvl[2], 'UV')[1][0]).reshape(-1, 2)
            ui = _find(uvl[2], 'UVIndex')
            UVI = np.array(ui[1][0]) if ui else None
            uvmap = _find(uvl[2], 'MappingInformationType')[1][0]
        matl = _find(o[2], 'LayerElementMaterial')
        MI = None
        if matl:
            MI = np.array(_find(matl[2], 'Materials')[1][0])
        # modèles qui utilisent cette géométrie
        for mid in parent.get(gid, []):
            mo = by_id.get(mid)
            if mo is None or mo[0] != 'Model':
                continue
            mats = [c for c in children.get(mid, []) if by_id.get(c) is not None and by_id[c][0] == 'Material']
            matcols = []
            mattex = []
            for m in mats:
                p = _props70(by_id[m])
                col = p.get('DiffuseColor', p.get('Diffuse', (0.8, 0.8, 0.8)))
                matcols.append(np.array(col[:3], dtype=np.float32))
                tp = tex_of(m)
                mattex.append(tex_image(tp) if tp and os.path.exists(str(tp)) else None)
            W = conv @ (np.eye(4) if local == 'node' else world(mid))[:3, :]
            P = (np.c_[V, np.ones(len(V))] @ W.T)
            tris, cols, uvs = [], [], []
            for pi, poly in enumerate(polys):
                mi = 0
                if MI is not None and len(MI) > 0:
                    mi = MI[pi] if len(MI) > pi else MI[0]
                base = matcols[mi] if mi < len(matcols) else np.array([0.8, 0.8, 0.8])
                tex = mattex[mi] if mi < len(mattex) else None
                col = base
                uvc = None
                if UV is not None:
                    ks = [k for (_, k) in poly]
                    if uvmap == 'ByPolygonVertex':
                        uvc = (UV[UVI[ks]] if UVI is not None else UV[ks]).mean(axis=0)
                    else:
                        uvc = UV[[v for (v, _) in poly]].mean(axis=0)
                if tex is not None and UV is not None:
                    ks = [k for (_, k) in poly]
                    if uvmap == 'ByPolygonVertex':
                        uv = UV[UVI[ks]] if UVI is not None else UV[ks]
                    else:
                        uv = UV[[v for (v, _) in poly]]
                    u, vv = uv.mean(axis=0)
                    h, w = tex.shape[:2]
                    x = int((u % 1.0) * w) % w
                    y = int((1.0 - (vv % 1.0)) * h) % h
                    col = tex[y, x]
                for j in range(1, len(poly) - 1):
                    tris.append((poly[0][0], poly[j][0], poly[j + 1][0]))
                    cols.append(col)
                    uvs.append(uvc if uvc is not None else (0.0, 0.0))
            if tris:
                meshes.append((P, np.array(tris), np.array(cols), mo[1][1].split('\x00')[0], np.array(uvs) if UV is not None else None))
    _cache[key] = meshes
    return meshes

def bounds(meshes):
    allp = np.vstack([m[0] for m in meshes])
    return allp.min(axis=0), allp.max(axis=0)
