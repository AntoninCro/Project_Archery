# Guide : la carte finale (cubique, à trois voies)

Ce guide remplace le sol plat du prototype par la carte finale (GDD, section 17), dans le style de l'image d'exemple du Nature Kit (`kenney_nature-kit/Sample.png`) : des terrasses de blocs, des falaises nettes, des cascades. Pas de collines lisses : tout est posé sur une grille.

La disposition reprend *League of Legends* :
- la **base** (la tour) dans un coin, avec les montagnes dans le dos ;
- **3 voies** qui y mènent, à gauche, au milieu et à droite ;
- de la **jungle** en terrasses entre les voies, une **rivière** qui coupe les voies et ralentit les ennemis ;
- des **montagnes** tout autour, et l'**antre du boss** au fond de la voie du milieu.

Les ennemis n'arrivent que de devant, sur un angle d'environ 90° : on peut quitter la tour sans être pris à revers.

Compte plusieurs séances : c'est la partie la plus longue, mais c'est elle qui donne l'identité du jeu. Avance zone par zone et teste souvent dans le casque.

## Le plan

Vue de dessus : `Docs/Plan_Carte_Prototype.png`. La carte est un **carré** d'environ 75 m de côté, aligné sur la grille. La tour est dans le coin sud-ouest ; en haut, le joueur regarde vers le nord-est, le centre de la carte.

```
  nord
   |   apparition gauche (0, 69)                        antre du boss (73.5, 79.5)
   |   *                                              /
   |   |          jungle                           /
   |   |                                        /
   |   |                       apparition du milieu (37.5, 37.5)
   |   | voie                     *
   |   | gauche              /           jungle
   |   |                 /
   |   |            / voie du milieu
   |   |       /
   |  tour (0, 0) -------------------------------------* apparition droite (69, 0)
   |                         voie droite                                     est ->
```

| Élément | Position | Rotation Y |
|---|---|---|
| Tour (ne bouge pas) | `(0, 0, 0)` | |
| Apparition gauche (`Spawn A`) | `(0, 0, 69)` | |
| Apparition du milieu (`Spawn B`) | `(37.5, 0, 37.5)` | |
| Apparition droite (`Spawn C`) | `(69, 0, 0)` | |
| `Barricade Gauche` | `(0, 0, 33)` | `0` |
| `Barricade Milieu` | `(23.33, 0, 23.33)` | `45` |
| `Barricade Droite` | `(33, 0, 0)` | `90` |
| Gués (rivière) | `(0, 0, 42)`, `(28.5, 0, 28.5)` et `(42, 0, 0)` | |
| Rampes vers la jungle | `(6, 0, 15)` et `(15, 0, 6)` | |
| Antre du boss | `(73.5, 0, 79.5)`, ouverte vers la tour | `180` |

- **Les voies de gauche et de droite** longent les bords de la carte, en ligne droite : de -3 à 3 m en X vers le nord, de -3 à 3 m en Z vers l'est. Elles font 6 m de large et environ 70 m de long, comme les chemins actuels : les vagues gardent leur rythme.
- **La voie du milieu** suit la diagonale, en escalier sur la grille (3 cases par rangée) : c'est son zigzag.
- **La jungle** est au-dessus des voies, en terrasses de 3 m, et de 6 m vers le fond.

## 0. La carte prototype (déjà dans la scène)

Un premier jet en blocs, calé sur la grille, est dans l'objet `Map` :

| Enfant | Ce qu'il contient |
|---|---|
| `Floor` | un seul grand bloc, son herbe à `y = 0` : le sol de toute la carte |
| `Lanes` | la terre des voies (`ground_pathOpen`) |
| `River` | l'eau des 3 gués (`ground_riverOpen`) et leurs zones *Slow Water* (`Ford Left`, `Ford Mid`, `Ford Right`) |
| `Jungle` | les terrasses : niveau 1 en terre orangée (`cliff_block_rock`), niveau 2 en pierre |
| `Ramps` | 2 rampes de la base vers la jungle, interdites aux ennemis (*NavMesh Modifier*) |
| `Mountains` | des cubes de 9 m, de 9 à 18 m de haut |
| `Lair` | l'entrée de la grotte de l'antre |
| `Chest Spots` | 6 emplacements de coffres sur les terrasses, déjà branchés au `Chest Spawner` |

Les objets du jeu ont été tournés avec la carte : le joueur, les menus, la boutique, le tableau des scores, les panneaux de difficulté, le gong et le brasero. Les cibles d'entraînement sont sur la voie du milieu et sur les terrasses. Ton premier essai est désactivé (`Map (ancien essai)`) : supprime-le quand tu veux.

**À faire dans Unity** :
1. Si Unity propose de recharger la scène, accepte.
2. **Le NavMesh** : sélectionne `Ground`, puis **Bake** dans son *NavMesh Surface*. Sans ça, les ennemis suivent l'ancien NavMesh, tout plat, et traversent les blocs.
3. Sélectionne `Map` et coche **Static**, avec ses enfants.

La suite du guide sert à **raffiner** ce prototype : des pièces plus fines que les gros blocs, des rampes et des escaliers sur les voies, la rivière dans la jungle, les cascades, les arbres et le décor.

**Pour que les blocs s'emboîtent** : le pivot d'un bloc est au centre de sa base, et un bloc de N cases (*Scale* N) couvre N × 3 m autour de sa position. Les bords des cases tombent sur les multiples de 3 m. Le centre d'un bloc va donc :
- sur un multiple de 3 plus 1,5 si N est impair : 1 case en `(1.5, 0, 4.5)`, 3 cases en `(4.5, 0, 4.5)` ;
- sur un multiple de 3 si N est pair : 2 cases en `(3, 0, 6)`.

Un bloc en `z = 29.625`, par exemple, tombe entre deux cases.

## 1. Préparer les kits

### Le ménage

Le **Nature Kit** est fait. Pour le **Castle Kit**, dans la fenêtre *Project*, supprime `Previews` et, dans `Models`, `GLB format` et `OBJ format`. Garde `Models/FBX format` (avec son sous-dossier `Textures`) et `Models/Textures` (des variantes de couleurs). Ces dossiers sont de toute façon dans le `.gitignore`.

### L'échelle et les colliders

C'est fait : l'échelle ×3 du Nature Kit (une case de la grille fait 3 m) et les colliders créés à l'import (*Generate Colliders*) pour `cliff_`, `ground_`, `rock_`, `stone_`, `tree_`, `stump_`, `log`, `bridge_` et `fence_`. Pas pour les herbes, les fleurs et les champignons : on ne doit ni s'y cogner ni y planter de flèches.

### Les pièces

Avec l'échelle ×3 :

| Pièce | Modèles (`_stone` : pierre grise ; `_rock` : terre orangée) | Taille |
|---|---|---|
| Bloc plein, herbe dessus | `cliff_block_stone`, `cliff_block_rock` | 3 × 3 × 3 m |
| Demi-bloc, quart de bloc | `cliff_blockHalf_*`, `cliff_blockQuarter_*` | 1,5 m et 75 cm de haut |
| Rampe | `cliff_blockSlope_stone` | voir section 5 |
| Escalier | `cliff_steps_stone` | voir section 5 |
| Cascade (un bord de terrasse) | `cliff_waterfall_stone`, `cliff_waterfallTop_stone` | 3 m de haut |
| Entrée de grotte | `cliff_blockCave_stone` | 3 m |
| Sol | `ground_grass` (herbe), `ground_pathOpen` (terre), `ground_pathSide` et `ground_pathStraight` (terre bordée d'herbe) | 3 × 3 m |
| Rivière | `ground_riverStraight`, `ground_riverBend`, `ground_riverOpen`, `ground_riverSide`, `ground_riverEnd` | 3 × 3 m |
| Pont | `bridge_wood`, `bridge_stone` | 3 m |
| Arbres, rochers, décor | comme avant : `tree_*` (*Scale* 1,5 à 2), `stone_tall*`, `rock_large*`, souches, rondins, fleurs | |

Toutes les pièces ont leur pivot au centre de leur base : posée en `(4.5, 0, 7.5)`, une case couvre de 3 à 6 m en X et de 6 à 9 m en Z.

## 2. La grille

1. Dans la vue *Scene*, barre d'outils en haut : à côté de l'aimant (*Grid Snapping*), clique sur la petite flèche. Dans *Grid Size*, décoche le lien (cadenas) pour régler les axes séparément : `1.5` en X et Z, `0.75` en Y.
2. Active l'aimant, en mode *Global* (bouton à côté de *Pivot/Center*). Les pièces déplacées avec l'outil de déplacement se calent alors sur la grille.
3. Range tout dans `Map`, dans l'enfant de sa zone (`Lanes`, `Jungle`, `Mountains`…), et ajoute un enfant `Decor` pour les arbres et le décor.

**Pour aller vite** :
- **Les gros blocs** : un `cliff_block_stone` en *Scale* `(2, 1, 2)` couvre 4 cases (6 × 6 m), centre sur un multiple de 3, par exemple `(6, 0, 6)`. Dans les montagnes, va jusqu'à *Scale* `3` (9 m).
- **La duplication** : pose une rangée, sélectionne-la et duplique-la (*Ctrl+D*), puis décale-la d'une case.
- **Les modules** : des groupes tout faits (un bout de terrasse avec ses arbres, un gué), gardés en prefabs dans `Prefabs/Nature` et réutilisés.

## 3. Le niveau 0 : la base, les voies et les gués

1. **Le sol** : le grand bloc `Floor`, dont l'herbe est à `y = 0`. `Ground` ne sert plus qu'à porter le *NavMesh Surface* : laisse-le où tu l'as mis.
2. **La base** : l'herbe du sol, sur environ 12 m autour de la tour.
3. **Les voies**, 2 cases de large (3 cases par rangée pour celle du milieu), au niveau du sol près de la base. Plus loin, elles peuvent monter et descendre par des rampes ou des escaliers (section 5), jusqu'aux apparitions :
   - `ground_pathOpen` (de la terre partout) ou, au bord, `ground_pathSide` tourné pour que l'herbe touche la falaise ;
   - la surface de ces cases est 15 cm sous leur pivot : pose-les à `y = 0.16` pour qu'elles dépassent de 1 cm l'herbe du sol.
4. Là où la rivière coupe une voie (section 7), des cases de rivière remplacent la terre, à la même hauteur.

### La voie du milieu en diagonale droite

La voie du milieu suit la diagonale : sur la grille, ses bords et les falaises de la forêt faisaient un escalier. Ils suivent maintenant deux droites, z = x + 6 et z = x - 6 (3 cases par rangée, environ 8,5 m de large) :
- **Au sol** : le prefab `Prefabs/Path Diagonal` est un triangle de terre (la moitié d'une case coupée en diagonale, même couleur que les voies). Rotation : `0` = terre au sud-est, `90` = sud-ouest, `180` = nord-ouest, `270` = nord-est. Il est posé au centre de la case, à `y = 0`, dans chaque creux le long des clairières et au pied des falaises coupées (`Map > Lanes > Path Diagonals`).
- **La forêt** : les cases de forêt qui faisaient des marches le long de la voie sont des blocs coupés en diagonale du kit (`cliff_blockDiagonal_*`, un par couche de 3 m), dans `Map > Jungle > Mid Lane Diagonals`. À rotation 0, la moitié pleine est au sud-ouest : `90` la met au nord-ouest (côté haut de la voie), `270` au sud-est (côté bas). Leur face coupée est habillée par `cliff_topDiagonal_*` (couche du haut, avec la lèvre d'herbe) et `cliff_diagonal_*` (couches du dessous), à la même rotation, dans `Dressing > Cliffs`.
- **Les escaliers** : là où ils touchent la voie, leurs pièces sont coupées sur la même diagonale. Le kit n'en a pas : ces modèles sont générés à partir des FBX de Kenney (mêmes matériaux), dans `Art/Diagonal Cuts` (`cliff_steps_rock_halfSW`, `cliff_stepsCornerInner_rock_halfSE`, `ground_pathOpen_halfSE` ; le suffixe dit quelle moitié est gardée à rotation 0). Les demi-blocs sous l'étage du haut sont des `cliff_blockDiagonal_rock` en *Scale Y* `0.5`. Leur face coupée est habillée par des bordures d'escalier diagonales (section 5).
- Pour prolonger une diagonale ailleurs : demande-moi, je peux générer d'autres pièces coupées.

## 4. La jungle en terrasses

1. Le prototype remplit l'espace entre les voies de gros blocs pleins (niveau 1 : 3 m de haut, jusqu'à 12 m de côté). Les voies deviennent des couloirs entre des falaises. Pour affiner, remplace les blocs du bord des voies par des blocs de 3 m, en variant `_stone` et `_rock`.
2. Vers le fond de la carte, les terrasses montent au niveau 2 (des blocs posés sur des blocs, à `y = 3`). Ajoute des demi-blocs pour varier les hauteurs, et des rampes pour y monter.
3. Garde de petites clairières dégagées sur les terrasses : coffres, matériaux et emplacements de construction.
4. Plante les arbres en retrait d'une case du bord des voies : depuis la tour, on doit voir le fond des couloirs.

## 5. Monter et descendre

Les falaises sont verticales : ni le joueur ni les ennemis ne les gravissent. Pour descendre d'une terrasse, on saute (la chute ne fait pas mal). Pour monter, deux pièces, qui montent toutes les deux à 27° : on y marche, et on y glisse en slide.

### Les rampes

- **Un niveau** (3 m) : `cliff_blockSlope_stone` en *Scale* `(1, 1, 2)`, soit 3 m de montée sur 6 m. Telle quelle (45°), elle serait trop raide, pour le joueur comme pour les ennemis.
- **Un demi-niveau** (1,5 m) : *Scale* `(1, 0.5, 1)`, contre un demi-bloc.
- **Sur une voie** (2 cases de large) : *Scale X* `2`, par exemple `(2, 1, 2)`.

### Les escaliers

Les marches du kit (`cliff_steps_stone`) feraient 60 cm de haut, trop pour le joueur. En *Scale* `(1, 0.5, 1)`, elles font 30 cm : 5 marches pour 1,5 m, sur 3 m. On leur donne une pente invisible :
1. Sur l'escalier, dans le *Mesh Collider* créé à l'import, remplace *Mesh* par `cliff_blockSlope_stone` (le maillage de la rampe, qui suit la même diagonale). On marche alors sur une pente lisse, sans à-coups, et le NavMesh en fait une rampe.
2. Fais-en un prefab (`Prefabs/Nature/Stairs`) pour le réutiliser.

- **Un niveau entier** (3 m) : deux escaliers à la suite, le second posé sur un demi-bloc (`cliff_blockHalf_*`).
- **Sur une voie** : l'escalier en *Scale* `(2, 0.5, 1)`, et le demi-bloc du second en `(2, 1, 1)`.
- La pente invisible dépasse de 30 cm de chaque côté : place les escaliers entre deux blocs, comme sur l'image d'exemple.

### Le sens

Pour les rampes comme pour les escaliers, le bas est du côté de la flèche bleue (axe Z) : tourne la pièce pour que la flèche pointe vers l'endroit d'où l'on arrive. Une pièce de 6 m de long occupe 2 cases : place son centre entre les deux.

### Les escaliers en biais

Le long d'une lisière à 45° (comme celle de la clairière de gauche), un escalier droit ne suit pas la grille. On prend alors les coins du kit : `cliff_stepsCorner_*` (coin extérieur, qui descend sur deux côtés) et `cliff_stepsCornerInner_*` (coin intérieur). Toutes les pièces sont en *Scale* `(1, 0.5, 1)` et tournées de la même façon.

1. **Étage du bas** (`y = 0`) : un coin extérieur sur chaque case de la lisière qui touche la clairière sur deux côtés, et un coin intérieur dans chaque creux, juste derrière. Les marches font une ligne en dents de scie qui se raccorde d'une case à l'autre.
2. **Étage du haut** (`y = 1.5`) : le même motif une rangée plus loin, posé sur des blocs `cliff_block_*` en *Scale* `(1, 0.5, 1)`.
3. Derrière, la terrasse de 3 m.

Avec la rotation Y à 0, un coin extérieur descend vers le nord et l'est ; un coin intérieur aussi (son point bas est dans le coin nord-est) :

| Rotation Y | Les coins descendent vers |
|---|---|
| 0 | le nord et l'est |
| 90 | l'est et le sud |
| 180 | le sud et l'ouest |
| 270 | l'ouest et le nord |

Exemple dans la scène : `Map > Ramps > Stairs Left`, 4 rangées en biais toutes tournées de 180°, entre la clairière de gauche et la jungle. Comme tout accès à la jungle, le groupe porte un *NavMesh Modifier* `Not Walkable` (ci-dessous).

### Les bordures d'escalier

Là où le côté d'un escalier donne sur un endroit plus bas (une voie, un gué, la coupe le long de la voie du milieu), on voyait son profil en terre lisse, à côté des falaises sculptées. Les pièces de `Art/Stair Borders` habillent ce côté : la même roche et la même lèvre d'herbe que `cliff_top`, avec un dessus qui suit les 5 marches de 30 cm des escaliers à demi-hauteur. Le kit n'en a pas : elles sont générées à partir des FBX de Kenney (mêmes matériaux, même collider).

Le nom dit tout : `cliff_stairBorder_<étage>_<sens>_<matière>` pour un côté droit, `cliff_stairBorderDiagonal_...` le long d'une coupe à 45°.
- **Étage** : `lower` pour la case d'escalier du bas (dessus de 0,3 à 1,5 m) ; `upper` pour celle du haut, posée sur un demi-bloc (la bordure couvre de 0 à 3 m, dessus de 1,8 à 3 m).
- **Sens** : `left` monte vers la gauche quand on regarde la face rocheuse, `right` vers la droite.
- **Matière** : `rock` ou `stone`, comme la falaise voisine.

On les pose comme l'habillage des falaises (section 10), en *Scale* `(1, 1, 1)`, à la hauteur du pied de l'escalier (`y = 0` au sol, même pour une `upper`) :
- **Côté droit** : au centre de la case basse, à côté de l'escalier, le dos contre lui. La rotation dépend de l'endroit où est l'escalier : au sud `0`, à l'ouest `90`, au nord `180`, à l'est `270`.
- **Diagonale** : au centre de la case d'escalier coupée, à la même rotation que `cliff_topDiagonal` sur cette diagonale. La rotation dépend du côté de la moitié pleine : au sud-ouest `0`, au nord-ouest `90`, au nord-est `180`, au sud-est `270`.

> **Déjà posées le 10 octobre** (`Map > Dressing > Stair Borders`, 8 bordures) :
> - les deux escaliers contre la voie du milieu (`Diagonal`, `lower` puis `upper`) ;
> - l'escalier de gauche au gué de la voie de gauche ;
> - l'escalier de droite au gué de la voie de droite.
>
> Elles avancent de 0,8 m sur la case voisine, comme l'habillage. Le dessus de leurs marches est trop étroit pour le NavMesh : les ennemis ne montent pas dessus.

### Sur les voies et vers la jungle

- **Sur les voies**, les ennemis montent et descendent comme toi : n'ajoute rien à ces rampes et escaliers. Là où une voie monte d'un niveau, monte aussi les terrasses qui la bordent, ou borde-la d'une palissade (`fence_simple`, avec son collider). Sinon, les ennemis entreraient dans la jungle de plain-pied.
- **Vers la jungle** (1 ou 2 accès par jungle, depuis la base, plus un ou deux depuis les voies si tu veux) : les ennemis ne doivent pas y monter. Sur chacun, *Add Component > NavMesh Modifier*, coche *Override Area* et choisis `Not Walkable`. Le joueur n'utilise pas le NavMesh : il monte quand même. La jungle protège ainsi des monstres au sol, mais pas des volants ni des mages.
- Garde les gués et les barricades sur des parties plates des voies.

## 6. Les montagnes

- Tout autour de la carte et derrière la base : des `cliff_block_stone` et `cliff_block_rock` en *Scale* `3` (cubes de 9 m), de 9 à 18 m de haut (déjà posés dans le prototype).
- Dessus : des sapins (`tree_pine*`) et de grands rochers (`stone_tall*`, *Scale* 2 à 3).
- Elles ferment la carte : vérifie depuis le haut de la tour qu'elles ferment l'horizon.

## 7. La rivière

1. **Son trajet** (plan final, GDD section 17) : elle descend du nord en cascade depuis la montagne, traverse la forêt de gauche et s'y sépare en deux bras. L'un rejoint la voie de gauche, l'autre la voie du milieu ; il traverse ensuite la clairière de droite jusqu'à la voie de droite.

### Pourquoi l'eau disparaît dans le sol

Les cases `ground_river*` sont une simple peau, sans épaisseur : leur pivot est au niveau de leurs berges (l'herbe), et leur eau est 15 cm plus bas. Posée à `y = 0` sur le sol (`Floor`, dont l'herbe est aussi à `y = 0`), l'eau se retrouve 15 cm **dans** le bloc : l'herbe du sol la recouvre, et les berges clignotent avec cette herbe (même hauteur). Il faut donc toujours que ce qui est sous une case de rivière soit **plus bas que son eau**.

### Au niveau du sol (gués, clairières)

2. Pose les cases à **`y = 0.16`**, comme la terre des voies : l'eau est alors 1 cm au-dessus de l'herbe du sol.
3. À découvert, prends **`ground_riverOpen`** (de l'eau partout, sans berges) : l'eau est à fleur de l'herbe, comme les gués actuels. Les cases à berges (`Straight`, `Bend`...) ont leurs berges 16 cm au-dessus du sol, et l'on voit le vide sous leurs bords : garde-les pour les couloirs entre deux falaises, qui cachent ces bords.

### Sur une terrasse (la forêt) : une rivière creusée, avec ses berges

4. Sous le tracé, découpe la terrasse en blocs d'une case et passe ceux de la rivière en *Scale Y* **`0.933`** : leur dessus descend à 2,80 m.
5. Pose les cases de rivière par-dessus à **`y = 3`** : les berges sont au niveau de l'herbe de la terrasse, l'eau 15 cm plus bas, au-dessus du bloc abaissé. Les bords des blocs voisins sont cachés sous les berges.

### Les pièces

6. Toutes en *Scale* `(1, 1, 1)`, au centre d'une case (`3k + 1.5`). Le chenal fait la moitié de la case (1,5 m d'eau, une berge de chaque côté). À rotation Y `0` (vue de dessus, nord = +Z) :

| Pièce | L'eau va vers |
|---|---|
| `ground_riverStraight` (et `Rocks`, avec des rochers) | le nord et le sud |
| `ground_riverBend` | le sud et l'est (virage) |
| `ground_riverSplit` | l'ouest, l'est et le sud (pour le fork des deux bras) |
| `ground_riverCross` | les quatre côtés |
| `ground_riverEnd` | le sud seulement (une source, ou la fin d'un bras) |
| `ground_riverOpen` | partout (gués, rivière large) |
| `ground_riverSide`, `Corner`, `CornerSmall`, `SideOpen` | bords d'une rivière large de plusieurs cases (berge au nord pour `Side`) |

Chaque +90° en Y tourne la pièce d'un quart de tour vers la droite (nord → est → sud → ouest). Par exemple, `Bend` relie le sud et l'est à 0, l'ouest et le sud à 90, le nord et l'ouest à 180, l'est et le nord à 270.

### Les cascades et les ponts

7. **Une cascade** (au bord d'une terrasse de 3 m) : `cliff_waterfallTop_rock` est une nappe d'eau de 3 m de haut, posée contre la falaise, sur la case du bas, à `y = 0`. Sa crête est à la hauteur de l'eau d'une rivière creusée sur la terrasse. Tourne-la pour que son dos touche la falaise :

| Falaise de ce côté de la case | Rotation Y |
|---|---|
| sud | 0 |
| ouest | 90 |
| nord | 180 |
| est | 270 |

   Pour une chute de 6 m : `cliff_waterfall_rock` en bas (`y = 0`) et `cliff_waterfallTop_rock` au-dessus (`y = 3`). Sous la cascade, la rivière repart au niveau du sol (étapes 2 et 3).
8. **Un pont** : `bridge_wood` (3 m, tablier à 30-60 cm, rambardes au nord et au sud à rotation 0), à la hauteur des berges. À la même rotation qu'une case `Straight`, il l'enjambe.
9. **Le ralentissement**, à chaque gué (là où la rivière coupe une voie) : *Create Empty* `Ford Left`, `Ford Mid` et `Ford Right`, à la surface de l'eau, puis *Add Component > Slow Water* :
   - *Size* : la largeur de la voie plus 2 m (X), `3` (Y), la largeur de la rivière plus 2 m (Z). Tourne l'objet pour suivre la rivière : la boîte est dessinée en bleu quand il est sélectionné ;
   - *Speed Multiplier* `0.6` : les ennemis au sol y avancent 40 % moins vite. Les volants ne sont pas concernés.

## 8. La tour, la base et l'antre

### La tour (Castle Kit)

1. Sur `Tower`, décoche le *Mesh Renderer* : le cube devient invisible, mais garde son *Box Collider*. C'est lui que frappent les ennemis et qui porte le joueur.
2. *Create Empty* `Tower Model` en `(0, 0, 0)`, **à côté** de `Tower`, pas dedans (`Tower` est étiré). Dedans :
   - `tower-square-base`, Position `(0, 0, 0)`, Scale `(5, 4, 5)` : 5 m de côté, 4 m de haut ;
   - `tower-square-top`, Position `(0, 4, 0)`, Scale `(5, 3, 5)` : des créneaux de 90 cm, sans collider (on peut toujours sauter pour descendre).
3. Optionnel, les ruines : *Create Empty* `Tower Ruins` en `(0, 0, 0)`, avec un `tower-square-base` en Scale `(5, 1.5, 5)` et quelques rochers autour.
4. Sur le composant *Tower* : *Intact Visual* `Tower Model`, *Ruined Visual* `Tower Ruins`.

La tour garde 5 m de côté et 4 m de haut : le XR Origin, `Tower Top Arrival` et la bande du téléporteur n'ont pas à bouger.

### La base

- Les cibles d'entraînement sont sur la voie du milieu (10 et 20 m) et sur les terrasses (les autres) ; les panneaux de difficulté flottent au-dessus de la voie du milieu.
- Décor du Castle Kit : un drapeau (`flag`, Scale 3), une palissade (`wall-narrow-wood-fence`, Scale 3) au pied des montagnes.

### L'antre du boss (porte fermée pour l'instant)

- L'entrée, au fond de la voie du milieu : le `cliff_blockCave_stone` du prototype, en *Scale* `3`, à `(73.5, 0, 79.5)`, rotation Y `180` (son ouverture regarde la tour).
- La porte : `gate` du Castle Kit, Scale 6 (4 m de large, 5,5 m de haut), à `(73.5, 0, 75.5)`, rotation Y `90` pour qu'elle barre l'entrée.
- Derrière, garde un replat d'environ 30 m pour l'arène : le combat final viendra plus tard (GDD, section 23).

## 9. Les ennemis, les barricades et les coffres

1. **Points d'apparition** : `Spawn A`, `B` et `C` sont au bout des 3 voies (positions du plan). Si tu modifies une voie, garde-les dessus.
2. **Barricades** : `Barricade Gauche`, `Milieu` et `Droite` sont en travers de leur voie, flèche bleue vers l'apparition, avec un *Scale X* de `1.5` (6 m), et de `2` pour celle du milieu.
3. **La navigation** : sélectionne `Ground` et clique sur **Bake** dans son *NavMesh Surface* (il prend tous les colliders de la scène). Dans la vue *Scene*, la base, les voies (avec leurs rampes et escaliers) et les gués doivent être bleus et reliés jusqu'aux apparitions. Les terrasses peuvent être bleues aussi, mais sans lien avec les voies : vérifie les *NavMesh Modifier* des accès à la jungle.
4. **Coffres** : les 6 `Chest Spot` sont sur les terrasses, accessibles par les rampes, et déjà dans *Spawn Points* du `Chest Spawner` (le coffre apparaît exactement là, sans NavMesh). Déplace-les dans des clairières quand tu planteras les arbres.
5. **Pour plus tard** (prévois la place, rien à poser) : des emplacements de construction plats d'une case, au bord des voies, près des barricades ; des recoins pour les matériaux de défense ; l'arène de l'antre.

## 10. La lumière et la fluidité

- Sélectionne `Map` et coche **Static**, avec ses enfants : Unity regroupe le décor fixe, ce qui le rend bien plus léger.
- La lumière et le brouillard viennent du ciel de chaque difficulté (`Sky Controller`). Ne fais pas de *Bake* de la lumière : le ciel change. Garde une seule lumière qui projette des ombres.
- *Window > Rendering > Occlusion Culling*, onglet *Bake*, puis **Bake** : Unity n'affiche plus ce qui est caché derrière les falaises.
- **Mesure** : dans la vue *Game*, active *Stats*. Dans le casque, vise au moins 72 images par seconde (moins de 13,8 ms par image). Si ça rame, utilise plus de gros blocs et moins de petites pièces.

### Pour que ce ne soit plus « des gros blocs »

> **Déjà fait le 9 octobre** autour de la base (rayon d'environ 45 m), d'après la carte du moment :
> - les réglages des étapes 1 à 3 (ombres, occlusion ambiante, `Global Volume` avec le profil `Data/Map Volume Profile`, post-traitement de la caméra) ;
> - tout le reste est rangé dans `Map > Dressing` : `Relief` (rebords d'1,5 m au pied des montagnes), `Cliffs` (habillage des falaises), `Decor` (touffes, rochers, buttes d'herbe), `Trees` (bosquets sur les terrasses), `Base Camp` (tentes, feu de camp, bancs, bûches, panneaux et barrières aux sorties des voies).
>
> Tu peux déplacer ou supprimer n'importe quel objet. Si tu modifies les blocs de la carte, l'habillage près des blocs modifiés ne suit pas : demande-le, `Dressing` peut être recréé entièrement d'après la nouvelle carte. Après ces changements, refais le **Bake** du NavMesh : l'habillage rétrécit un peu les voies le long des falaises, et les rochers et les arbres sont des obstacles.

1. **Les ombres** : dans `Settings/Project Configuration/Quality URP Config`, partie *Shadows*, *Max Distance* était à `10` (aucune ombre au-delà de 10 m). Mets `80`, et *Cascade Count* à `3`.
2. **L'occlusion ambiante** (les coins et les pieds de falaise s'assombrissent) : sur le renderer `Settings/Project Configuration/Standalone Preset`, *Add Renderer Feature > Screen Space Ambient Occlusion*, avec *Downsample* coché. Vérifie ensuite les images par seconde.
3. **Un peu de couleur** : *GameObject > Volume > Global Volume*, *New* profil, *Add Override > Post-processing > Color Adjustments* (*Contrast* `10`, *Saturation* `15`). Sur la caméra du XR Origin, coche *Rendering > Post Processing*.
4. **Les falaises habillées**, comme sur l'image d'exemple du kit : `cliff_block` est un cube nu. Devant chaque face visible, sur la case du bas, pose `cliff_top_rock` (roche sculptée et lèvre d'herbe, 3 m de haut), le dos contre le bloc. La règle de rotation est la même que pour les cascades (falaise au sud de la case `0`, à l'ouest `90`, au nord `180`, à l'est `270`). La pièce avance d'environ 0,8 m sur la case du bas.
   - Pour 6 m : `cliff_rock` en bas, `cliff_top_rock` au-dessus. Pour une marche de 1,5 m : `cliff_half_rock`.
   - Aux coins : `cliff_cornerTop_rock` (coin saillant) et `cliff_cornerInnerTop_rock` (coin rentrant).
   - Sur le côté d'un escalier : les bordures d'escalier (section 5).
5. **Les décors en touffes** (3 à 7 objets groupés, jamais en grille), surtout au pied des falaises et le long des voies pour casser leurs bords carrés. Fais varier la rotation Y et l'échelle (de 0,8 à 1,2).

| Décor | Échelle conseillée |
|---|---|
| `grass`, `grass_large` (1 m de large, 0,8 m de haut) | 0,6 |
| `flower_*`, `mushroom_*`, `plant_bush*` | 0,7 à 1 |
| `rock_small*`, `rock_large*`, `rock_tall*` | 1 |
| arbres (3,6 à 5 m de haut) | 1,3 à 1,8 |
| `tent_detailedOpen` (1,7 m de haut) | 1,4 |

## 11. Vérifier

1. Une vague dans chaque difficulté : les ennemis sortent des 3 voies, sont ralentis dans la rivière, s'arrêtent aux barricades, puis avancent vers la tour.
2. Les ennemis montent et descendent les rampes et les escaliers des voies, mais aucun ne monte dans la jungle ni ne reste coincé. Sinon, vérifie les rampes et rebake.
3. Les coffres apparaissent dans les clairières et s'ouvrent.
4. Depuis la tour, tu vois les trois voies sans tourner la tête de plus d'un quart de tour.
5. Tu descends de la tour, montes dans la jungle par une rampe ou un escalier (sans à-coups), sautes d'une terrasse et glisses dans une rampe. La bande du téléporteur te ramène en haut.
6. Les flèches se plantent dans les falaises, les arbres et le sol.
7. La nuit (Difficile, Impossible), la carte reste lisible.
