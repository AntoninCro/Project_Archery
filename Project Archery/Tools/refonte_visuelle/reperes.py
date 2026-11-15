# La fiche d'un prefab pour la refonte visuelle (14 novembre ; Docs/Refonte_Visuelle.md) : sa hiérarchie, et pour chaque objet
# ce que le code en fait (le réglage d'un script qui le désigne : une pièce qui bouge, un repère, les pièces d'un niveau). Ce
# qui est désigné se garde (on change son maillage, pas l'objet) ; les objets cachés au nom daté sont les marques des bakers :
# les garder, sinon le baker refait le modèle de Claude au lancement suivant.
# Depuis « Project Archery/Assets » :
#   py -I ../Tools/refonte_visuelle/reperes.py _Project/Prefabs/Structures/Duos/Duo_Yoyo.prefab [profondeur=2]
# Comme module (preparer.py) : lire(chemin) donne les objets, leur ordre, ce que le code en fait et leur chemin dans le prefab.
import os, re, sys

HEAD = re.compile(r'^--- !u!(\d+) &(-?\d+)( stripped)?')
SKIP_FIELDS = ('m_GameObject', 'm_Script', 'm_EditorClassIdentifier', 'm_CorrespondingSourceObject', 'm_PrefabInstance')


class Objet:
    def __init__(self, key, name, kind):
        self.key = key            # l'identifiant du transform (ou de l'instance, pour un modèle imbriqué)
        self.name = name
        self.kind = kind          # '' (un objet) ou 'modèle' (un modèle d'un kit, un prefab imbriqué)
        self.parent = '0'
        self.children = []        # dans l'ordre du prefab
        self.hidden = False
        self.used = set()         # les réglages des scripts qui le désignent
        self.scripted = False     # il porte un script à lui
        self.mesh = False         # il a un maillage à lui (ou c'est un modèle)
        self.go = None            # son GameObject
        self.stripped = set()     # pour un modèle : ses transforms vus du prefab


class Fiche:
    def __init__(self):
        self.objets = {}
        self.roots = []

    def keep(self):
        """Ce qui se garde : désigné par le code, ou porteur d'un script (sauf la racine)."""
        return [k for k, o in self.objets.items() if o.parent != '0' and (o.used or o.scripted)]

    def kept_path(self):
        """Ce qui se garde et tous ses parents : ce qui reste dans le prefab quand le modèle fait main y est branché."""
        out = set()
        for k in self.keep():
            while k in self.objets and k not in out:
                out.add(k)
                k = self.objets[k].parent
        return out

    def path(self, key):
        """Le chemin dans le prefab depuis la racine (exclue) : des (nom, rang). Le rang se compte parmi les frères du même nom
        qui restent quand le modèle fait main est branché (kept_path) : il ne change pas d'un branchement à l'autre
        (Editor/ModelesFaitMain.cs compte de même)."""
        stay = self.kept_path()
        segs = []
        k = key
        while k in self.objets and self.objets[k].parent != '0':
            o = self.objets[k]
            siblings = self.objets[o.parent].children if o.parent in self.objets else []
            same = [s for s in siblings if self.objets[s].name == o.name and s in stay]
            segs.append((o.name, same.index(k) if k in same else 0))
            k = o.parent
        return list(reversed(segs))

    def depth_first(self):
        out = []
        def walk(k):
            out.append(k)
            for c in self.objets[k].children:
                walk(c)
        for r in self.roots:
            walk(r)
        return out


def _name(s):
    # Unity écrit les noms accentués entre guillemets, avec des \xE8 ou des ….
    if s is None:
        return '?'
    if s.startswith('"') and s.endswith('"'):
        s = s[1:-1]
        s = re.sub(r'\\x([0-9A-Fa-f]{2})', lambda m: chr(int(m.group(1), 16)), s)
        s = re.sub(r'\\u([0-9A-Fa-f]{4})', lambda m: chr(int(m.group(1), 16)), s)
        s = s.replace('\\"', '"')
    return s


def lire(path, guids=None):
    docs = {}
    cur = None
    for line in open(path, 'r', encoding='utf-8', errors='ignore'):
        m = HEAD.match(line)
        if m:
            cur = {'cls': int(m.group(1)), 'id': m.group(2), 'stripped': bool(m.group(3)), 'lines': []}
            docs[cur['id']] = cur
        elif cur is not None:
            cur['lines'].append(line.rstrip('\n'))

    def val(d, k):
        for l in d['lines']:
            s = l.strip()
            if s.startswith(k + ':'):
                return s[len(k) + 1:].strip()
        return None

    def ref(d, k):
        m = re.search(r'fileID: (-?\d+)', val(d, k) or '')
        return m.group(1) if m else None

    def children_of(d):
        out, take = [], False
        for l in d['lines']:
            s = l.strip()
            if s.startswith('m_Children:'):
                take = not s.endswith('[]')
                continue
            if take:
                if s.startswith('- {fileID'):
                    out.append(re.search(r'fileID: (-?\d+)', s).group(1))
                else:
                    break
        return out

    f = Fiche()
    alias = {}
    # les modèles imbriqués : un objet chacun
    for i, d in docs.items():
        if d['cls'] == 1001:
            name = None
            lines = d['lines']
            for j, l in enumerate(lines):
                if l.strip() == 'propertyPath: m_Name' and j + 1 < len(lines):
                    name = lines[j + 1].strip()[len('value:'):].strip()
                    break
            if not name and guids is not None:
                g = re.search(r'guid: ([0-9a-f]+)', val(d, 'm_SourcePrefab') or '')
                src = guids.get(g.group(1), '') if g else ''
                name = os.path.splitext(os.path.basename(src))[0] if src else None
            o = Objet(i, _name(name) if name else '(modèle imbriqué)', 'modèle')
            o.parent = ref(d, 'm_TransformParent') or '0'
            o.mesh = True
            f.objets[i] = o
    for i, d in docs.items():
        if d['stripped']:
            inst = ref(d, 'm_PrefabInstance')
            if inst in f.objets:
                alias[i] = inst
                if d['cls'] in (4, 224):
                    f.objets[inst].stripped.add(i)
    comps = {}
    for i, d in docs.items():
        if d['cls'] == 1 and not d['stripped']:
            comps[i] = [re.search(r'fileID: (-?\d+)', l).group(1) for l in d['lines'] if 'component:' in l]
    order = {}
    for i, d in docs.items():
        if d['cls'] in (4, 224) and not d['stripped']:
            go = ref(d, 'm_GameObject')
            g = docs.get(go)
            o = Objet(i, _name(val(g, 'm_Name')) if g else '?', '')
            o.go = go
            o.parent = ref(d, 'm_Father') or '0'
            o.hidden = g is not None and val(g, 'm_IsActive') == '0'
            o.mesh = any(docs.get(c, {}).get('cls') in (33, 137) for c in comps.get(go, []))
            f.objets[i] = o
            order[i] = children_of(d)
    go_to = {o.go: k for k, o in f.objets.items() if o.go}

    def node(fid):
        if fid in alias:
            return alias[fid]
        if fid in f.objets:
            return fid
        if fid in go_to:
            return go_to[fid]
        d = docs.get(fid)
        if d is not None and not d['stripped']:
            g = ref(d, 'm_GameObject')   # un composant : son objet
            if g in go_to:
                return go_to[g]
        return None

    for k, o in f.objets.items():
        o.parent = alias.get(o.parent, o.parent)
        if o.parent not in f.objets:
            o.parent = '0'
    # les enfants, dans l'ordre du prefab (les modèles imbriqués y sont par leur transform)
    for k, kids in order.items():
        seen = []
        for c in kids:
            n = alias.get(c, c)
            if n in f.objets and n not in seen:
                seen.append(n)
        f.objets[k].children = seen
    for k, o in f.objets.items():
        if o.parent != '0' and k not in f.objets[o.parent].children:
            f.objets[o.parent].children.append(k)
    f.roots = [k for k, o in f.objets.items() if o.parent == '0']

    for i, d in docs.items():
        if d['cls'] != 114:
            continue
        holder = node(ref(d, 'm_GameObject') or '')
        if holder:
            f.objets[holder].scripted = True
        field = None
        for l in d['lines']:
            m = re.match(r'\s{2}(\w+):', l)
            if m:
                field = m.group(1)
            if field in (None,) + SKIP_FIELDS:
                continue
            for fid in re.findall(r'fileID: (-?\d+)', l):
                n = node(fid)
                if n:
                    f.objets[n].used.add(field)
    return f


def main():
    sys.stdout.reconfigure(encoding='utf-8')
    path = sys.argv[1]
    depth_max = int(sys.argv[2]) if len(sys.argv) > 2 else 2
    f = lire(path)
    keep = set(f.keep())

    def label(k):
        o = f.objets[k]
        s = o.name
        if o.kind:
            s += '  (' + o.kind + ')'
        if o.hidden:
            s += '  (caché' + (' : marque d\'un baker, à garder' if re.search(r'\d', o.name) else '') + ')'
        if o.scripted and o.parent != '0':
            s += '  [porte un script]'
        if o.used and o.parent != '0':
            s += '   <- ' + ', '.join(sorted(o.used))
        return s

    def show(k, depth):
        kids = f.objets[k].children
        more = '  [%d enfants]' % len(kids) if kids and depth >= depth_max else ''
        print('  ' * depth + '- ' + label(k) + more)
        if depth < depth_max:
            for c in kids:
                show(c, depth + 1)
            return
        stack = list(kids)
        while stack:
            c = stack.pop(0)
            if c in keep:
                print('  ' * (depth + 1) + '… ' + label(c))
            stack += f.objets[c].children

    for r in f.roots:
        show(r, 0)
    print()
    print('%d objets désignés par le code (<-) ou qui portent un script : les garder, avec leur nom ; changer leur maillage.' % len(keep))


if __name__ == '__main__':
    main()
