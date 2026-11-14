# La fiche d'un prefab pour la refonte visuelle (14 novembre ; Docs/Refonte_Visuelle.md) : sa hiérarchie, et pour chaque objet
# ce que le code en fait (le réglage d'un script qui le désigne : une pièce qui bouge, un repère, les pièces d'un niveau). Ce
# qui est désigné se garde (on change son maillage, pas l'objet) ; les objets cachés au nom daté sont les marques des bakers :
# les garder, sinon le baker refait le modèle de Claude au lancement suivant.
# Depuis « Project Archery/Assets » :
#   py -I ../Tools/refonte_visuelle/reperes.py _Project/Prefabs/Structures/Duos/Duo_Yoyo.prefab [profondeur=2]
import re, sys

sys.stdout.reconfigure(encoding='utf-8')
path = sys.argv[1]
depth_max = int(sys.argv[2]) if len(sys.argv) > 2 else 2

head = re.compile(r'^--- !u!(\d+) &(-?\d+)( stripped)?')
docs = {}
cur = None
for line in open(path, 'r', encoding='utf-8', errors='ignore'):
    m = head.match(line)
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

# Unity écrit les noms accentués entre guillemets, avec des \xE8 ou des ….
def name_of(s):
    if s is None:
        return '?'
    if s.startswith('"') and s.endswith('"'):
        s = s[1:-1]
        s = re.sub(r'\\x([0-9A-Fa-f]{2})', lambda m: chr(int(m.group(1), 16)), s)
        s = re.sub(r'\\u([0-9A-Fa-f]{4})', lambda m: chr(int(m.group(1), 16)), s)
        s = s.replace('\\"', '"')
    return s

# Les nœuds : les transforms du prefab, et chaque modèle imbriqué (un kit, un autre prefab) comme un seul nœud.
node_name, node_parent, node_hidden, node_kind = {}, {}, {}, {}
alias = {}  # objet ou transform d'un modèle imbriqué -> le nœud du modèle
go_of = {}
for i, d in docs.items():
    if d['cls'] == 1001:
        name = None
        lines = d['lines']
        for j, l in enumerate(lines):
            if l.strip() == 'propertyPath: m_Name' and j + 1 < len(lines):
                name = lines[j + 1].strip()[len('value:'):].strip()
                break
        node_name[i] = name_of(name) if name else '(modèle imbriqué)'
        node_parent[i] = ref(d, 'm_TransformParent') or '0'
        node_kind[i] = 'modèle'
for i, d in docs.items():
    if d['stripped']:
        inst = ref(d, 'm_PrefabInstance')
        if inst in node_name:
            alias[i] = inst
for i, d in docs.items():
    if d['cls'] in (4, 224) and not d['stripped']:
        go = ref(d, 'm_GameObject')
        go_of[i] = go
        node_parent[i] = ref(d, 'm_Father') or '0'
        g = docs.get(go)
        node_name[i] = name_of(val(g, 'm_Name')) if g else '?'
        node_hidden[i] = g is not None and val(g, 'm_IsActive') == '0'
        node_kind[i] = ''
tr_of_go = {g: t for t, g in go_of.items()}

def node(fid):
    if fid in alias:
        return alias[fid]
    if fid in tr_of_go:
        return tr_of_go[fid]
    return fid if fid in node_name else None

children = {}
for n, p in node_parent.items():
    p = alias.get(p, p)
    node_parent[n] = p
    children.setdefault(p, []).append(n)

# Ce que les scripts désignent (le nom du réglage), et les objets qui portent un script.
used = {}
scripted = set()
for i, d in docs.items():
    if d['cls'] != 114:
        continue
    holder = node(ref(d, 'm_GameObject') or '')
    if holder:
        scripted.add(holder)
    field = None
    for l in d['lines']:
        m = re.match(r'\s{2}(\w+):', l)
        if m:
            field = m.group(1)
        if field in (None, 'm_GameObject', 'm_Script', 'm_EditorClassIdentifier', 'm_CorrespondingSourceObject', 'm_PrefabInstance'):
            continue
        for fid in re.findall(r'fileID: (-?\d+)', l):
            n = node(fid)
            if n:
                used.setdefault(n, set()).add(field)

def label(n):
    s = node_name.get(n, '?')
    if node_kind.get(n):
        s += '  (' + node_kind[n] + ')'
    if node_hidden.get(n):
        s += '  (caché' + (' : marque d\'un baker, à garder' if re.search(r'\d', s) else '') + ')'
    if n in scripted and node_parent.get(n) != '0':
        s += '  [porte un script]'
    if n in used:
        s += '   <- ' + ', '.join(sorted(used[n]))
    return s

def show(n, depth):
    kids = children.get(n, [])
    more = '  [%d enfants]' % len(kids) if kids and depth >= depth_max else ''
    print('  ' * depth + '- ' + label(n) + more)
    if depth < depth_max:
        for k in kids:
            show(k, depth + 1)
        return
    stack = list(kids)
    while stack:
        k = stack.pop(0)
        if k in used or k in scripted:
            print('  ' * (depth + 1) + '… ' + label(k))
        stack += children.get(k, [])

for r in children.get('0', []):
    show(r, 0)
print()
keep = set(used) | {n for n in scripted if node_parent.get(n) != '0'}
print('%d objets désignés par le code (<-) ou qui portent un script : les garder, avec leur nom ; changer leur maillage.' % len(keep))
