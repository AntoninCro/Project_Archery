# Guide : l'arc légendaire en 3 morceaux

Ce guide ajoute l'arc légendaire, une idée de fin de projet du GDD (section 23). Il demande d'avoir monté **les coffres** (`Guide_Coffres.md`).

- Tant que l'arc n'est pas complet, un coffre a **35 % de chance** de proposer un **morceau d'arc légendaire** (une orbe violette, « 1 / 3 »), à la place de sa troisième orbe.
- Prendre l'orbe donne le morceau, avec un son cristallin et le message « Morceau d'arc légendaire : 1 / 3 ».
- Au **troisième morceau**, l'arc est assemblé :
  - il **remplace ton arc** jusqu'à la fin de la partie, même pendant que tu le tiens ;
  - une fanfare et un message l'annoncent, et les manettes vibrent ;
  - la boutique ne propose plus d'arc.
- Les morceaux se perdent à la fin de la partie.

| Arc légendaire | Valeur |
|---|---|
| Vitesse des flèches | 62 m/s |
| Dégâts | 32 |
| Anneau | 1 s, bandes larges |
| Particularité | ses flèches traversent 2 ennemis |

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Legendary Bow` | Compte les morceaux et assemble l'arc | Objet `Game` |

Le reste est déjà dans les coffres et la boutique. Les sons sont dans `Audio/Placeholder` : `legendary_part` et `legendary_assembled`.

## 1. L'arc

Dans `Data/Bows` : clic droit > *Create > Archery > Bow Definition*, nomme-le `Bow_Legendaire` :

| Champ | Valeur |
|---|---|
| *Display Name* | `Arc légendaire` |
| *Description* | `Assemblé à partir de trois morceaux trouvés dans les coffres. Ses flèches traversent deux ennemis.` |
| *Price*, *Available From Wave* | `0` (il ne se vend pas) |
| *Arrow Speed* | `62` |
| *Damage* | `32` |
| *Max Draw Distance* | `0.35` |
| *Pierce Count* | `2` |
| *Ring Duration* | `1` |
| *Gold Half Width* | `0.08` |
| *Good Width* | `0.16` |

Ne l'ajoute **pas** à la liste *Bows* du catalogue : on ne l'achète pas.

**Son apparence** (optionnel) : comme pour les autres arcs (`Guide_Boutique.md`, section 5), prépare un des modèles d'Easy Weapons qui reste, en *Prefab Variant* `BowVisual_Legendaire`, et glisse-le dans *Visual Prefab*. Une couleur violette ou dorée le distingue bien.

## 2. Le gestionnaire

Sur l'objet `Game` : *Add Component > Legendary Bow* :
- *Definition* : `Bow_Legendaire` ;
- *Part Clip* `legendary_part`, *Assembled Clip* `legendary_assembled`.

Les autres réglages : *Parts Needed* (3), *Part Chance* (0,35), *Color* (violet).

## 3. Tester

1. Sur `Legendary Bow`, mets provisoirement *Part Chance* à `1` : chaque coffre proposera un morceau.
2. Lance Play. Appuie sur **C** pour poser un coffre, ouvre-le et prends l'orbe violette : « Morceau d'arc légendaire : 1 / 3 ».
3. Recommence deux fois (**C**) : au troisième, la fanfare joue et ton arc change, même dans ta main.
4. Tire : les flèches vont plus vite, font plus de dégâts et traversent deux ennemis.
5. Finis une vague (**N**) : la carte de l'arc dans la boutique indique que tu as l'arc légendaire.

Remets *Part Chance* à `0.35`.

## En cas de problème

- **Aucune orbe violette** : il faut un `Legendary Bow` sur `Game`, avec sa *Definition* (sinon la console l'indique).
- **L'arc ne change pas d'apparence** : `Bow_Legendaire` n'a pas de *Visual Prefab* (optionnel).
- **La boutique propose encore l'arc de chasse après l'assemblage** : vérifie que `Bow_Legendaire` n'est pas dans la liste *Bows* du catalogue.
