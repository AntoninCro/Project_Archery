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
   - la surface de ces cases est 15 cm sous leur pivot : pose-les à `y = 0.15`, leur terre est alors au niveau exact de l'herbe. Le sol (`Floor`) est creusé sous les voies : sans cela, la terre et l'herbe se superposent et clignotent. Si tu ajoutes une voie, demande-moi de recreuser le sol dessous.
4. Là où la rivière coupe une voie (section 7), des cases de rivière remplacent la terre, à la même hauteur.

### La voie du milieu en diagonale droite

La voie du milieu suit la diagonale : sur la grille, ses bords et les falaises de la forêt faisaient un escalier. Ils suivent maintenant deux droites, z = x + 6 et z = x - 6 (3 cases par rangée, environ 8,5 m de large) :
- **Au sol** : le prefab `Prefabs/Path Diagonal` est une case coupée en diagonale : un triangle de terre (même couleur que les voies) et un triangle d'herbe, au niveau du sol. Rotation : `0` = terre au sud-est, `90` = sud-ouest, `180` = nord-ouest, `270` = nord-est. Il est posé au centre de la case, à `y = 0`, dans chaque creux le long des clairières et au pied des falaises coupées (`Map > Lanes > Path Diagonals`).
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
> Et le 11 octobre, celles des montées du 2e étage, à droite puis à gauche (ci-dessous).
>
> Elles avancent de 0,8 m sur la case voisine, comme l'habillage. Le dessus de leurs marches est trop étroit pour le NavMesh : les ennemis ne montent pas dessus.

### Les montées du 2e étage

Les deux terrasses de 6 m, dans la jungle de droite et dans celle de gauche, se font face de part et d'autre de la voie du milieu : celle de gauche est le miroir de celle de droite (on échange x et z), sauf la rivière qui la traverse. On y monte par des escaliers et des rampes mélangés, avec des paliers à mi-hauteur, sans marche de plus de 30 cm :
- **Jungle de droite** (`Map > Ramps > Upper Climb West` et `Upper Climb East`) :
  - montée ouest (colonne 18, entre les obélisques) : un escalier de 3 à 4,5 m, puis une rampe à murets jusqu'en (18, 14), avec un rebord à 4,5 m à droite de l'escalier ;
  - montée est (rangées 10 et 11), en Y : deux escaliers côte à côte, un palier de deux cases à 4,5 m, puis un escalier creusé dans la terrasse jusqu'en (23, 10). Le haut côté nord est laissé au grand rocher (`stone_tallB`).
- **Jungle de gauche** (`Upper Climb Northwest` et `Upper Climb Falls`) :
  - montée nord-ouest : le miroir de la montée est (escaliers en (10-11, 20), palier en (10-11, 21), escalier creusé en (10, 22), arrivée en (10, 23)) ;
  - montée de la cascade : le même plan que la montée ouest (escalier en (14, 17), rampe à murets en (14, 18), arrivée en (14, 19), à côté de la cascade) ; son miroir exact tombait dans la rivière.
- **L'habillage** : chaque face des montées et du bord de la terrasse qui donne sur le vide est en pierre. On utilise `cliff_top_stone` (en *Scale Y* `0.5` devant un demi-bloc), `cliff_cornerTop_stone` aux coins extérieurs, `cliff_halfCornerInner_stone` aux coins intérieurs des paliers, et une bordure d'escalier (section ci-dessus) sur chaque côté d'escalier à découvert.
- **Le décor** : des obélisques et un anneau au pied des montées, de grands rochers (`stone_tall*`), une statue et une souche sur la terrasse. À gauche, c'est le même décor en miroir ; l'anneau et la souche sont déplacés hors de la rivière.

Pour en faire une autre ailleurs : un étage de 1,5 m par case, un palier (`cliff_blockHalf_*`) pour changer de sens ou de type de pièce, et une bordure d'escalier sur chaque côté d'escalier qui donne sur le vide.

### Sur les voies et vers la jungle

- **Sur les voies**, les ennemis montent et descendent comme toi : n'ajoute rien à ces rampes et escaliers. Là où une voie monte d'un niveau, monte aussi les terrasses qui la bordent, ou borde-la d'une palissade (`fence_simple`, avec son collider). Sinon, les ennemis entreraient dans la jungle de plain-pied.
- **Vers la jungle** (1 ou 2 accès par jungle, depuis la base, plus un ou deux depuis les voies si tu veux) : les ennemis ne doivent pas y monter. Sur chacun, *Add Component > NavMesh Modifier*, coche *Override Area* et choisis `Not Walkable`. Le joueur n'utilise pas le NavMesh : il monte quand même. La jungle protège ainsi des monstres au sol, mais pas des volants ni des mages.
- Garde les gués et les barricades sur des parties plates des voies.

## 6. Les montagnes

- Tout autour de la carte et derrière la base : des `cliff_block_stone` et `cliff_block_rock` en *Scale* `3` (cubes de 9 m), de 9 à 18 m de haut (déjà posés dans le prototype).
- Dessus : des sapins (`tree_pine*`) et de grands rochers (`stone_tall*`, *Scale* 2 à 3).
- Elles ferment la carte : vérifie depuis le haut de la tour qu'elles ferment l'horizon.

> **Habillées le 11 octobre**, comme ton mur ouest (`Map > Dressing > Cliffs > Map Walls`, 357 pièces) : chaque face visible depuis la carte (y compris les marches entre deux blocs de hauteurs différentes) reçoit une colonne de `cliff_stone` / `cliff_rock` (selon le bloc) tous les 3 m, avec un `cliff_top` à lèvre d'herbe au sommet. Pour casser la régularité : des colonnes décalées de 1,5 m (`cliff_half` en bas et en haut), des `cliff_half` en saillie en haut de certaines colonnes, des colonnes un peu renfoncées, et des coins (`cliff_corner…` saillants, `cliff_cornerInner…` rentrants). Les faces tournées vers l'extérieur de la carte ne sont pas habillées (on ne les voit pas).
>
> **Le coin du fond est devenu une face** (`Map > Lair > End Wall`) : au bout de la voie du milieu, le coin nord-est est coupé en diagonale, perpendiculairement à la voie, de (60, 75) à (75, 60). La face fait 21 m de large et 18 m de haut (blocs pleins derrière, demi-blocs `cliff_blockDiagonal_stone` à rotation `180` sur la diagonale). Au-dessus des deux terrasses, elle est habillée de roche taillée (`cliff_diagonal_stone`) ; tout en haut, une lèvre d'herbe (`cliff_topDiagonal_stone`) court sur toute la largeur. Au-dessus de la voie, la pierre reste lisse sur 12 m de large et 15 m de haut : c'est la place de la porte du boss final et du nom du jeu.

## 7. La rivière

> **Fait le 11 octobre**, d'après ton tracé (`Map > River`, le groupe de 75 pièces) :
> - **Le cours** : la source sort de la montagne (cascade de 9 m), descend en chenal large sur la terrasse de 6 m, tombe en cascade sur la terrasse de 3 m et s'élargit en bassin. De là partent deux bras. Celui de l'ouest tombe en cascade sur la voie de gauche et quitte la carte. Celui du sud tombe en cascade dans la falaise qui borde la voie du milieu (à plat, dans l'alignement du bord coupé), traverse la voie du milieu tout droit (3 cases), fait un méandre en S dans la clairière, puis traverse la voie de droite et quitte la carte.
> - **L'eau** est construite au lancement par `Map > River Water` (section 7 bis).

### Une rivière creusée dans le terrain

1. **Les pièces du lit** sont les `ground_path*` du kit (terre et berges d'herbe). Le pivot est au niveau des berges : pose-les **au ras du terrain** (`y = 0` au sol et sur une voie, `3` ou `6` sur une terrasse), au centre d'une case, en *Scale* `(1, 2.5, 1)`. Le lit est alors 37,5 cm sous les berges.
2. **Le terrain dessous est découpé** : le bloc qui couvrait la case (forêt, ou `Floor` au sol) n'y passe plus, et un bloc plus bas le remplace sous le lit (dessus 45 cm sous les berges). Sans cela, le terrain cache le lit. Les voies sont découpées de la même façon sous les passages.
3. **Sur une voie**, les berges vertes juraient avec la terre : `Art/River/ground_pathStraight_lane` est la même pièce, avec des berges en terre. Elle sert aux trois passages (voie de gauche, voie du milieu, voie de droite).
4. **Au bord de la forêt coupée**, la case (8, 10) est coupée en diagonale comme ses voisines : rien ne dépasse dans la voie du milieu. Le lit y arrive par l'ouest dans un virage coupé en deux, qui touche la diagonale à angle droit :
   - en haut (`y = 3`), `Art/River/ground_pathBend_halfSW` à rotation `90` : la moitié nord-ouest du virage, sur le bloc diagonal ;
   - en bas (`y = 0`), `Art/River/ground_pathBend_lane_halfNE` à rotation `90` : la moitié sud-est du même virage, berges en terre, qui renvoie le lit vers le sud ;
   - entre les deux, `Art/River/cliff_waterfall_rock_narrow` (la cascade du kit réduite à sa nappe) à rotation `135`, le dos sur la diagonale, dans une encoche de l'habillage diagonal `Art/River/cliff_topDiagonal_rock_notch` (rotation `90`, comme ses voisines).
   Pour arriver par l'ouest, le lit d'en haut passe par la colonne 7 (cases (7, 12) à (7, 10)) ; les cases (8, 11) et (8, 12) sont redevenues de la forêt.

Pour changer le tracé : déplace ou ajoute des pièces dans `Map > River`, puis demande-moi de refaire le découpage du terrain. L'eau suit toute seule au lancement suivant.

### Les pièces

À rotation Y `0` (vue de dessus, nord = +Z) :

| Pièce | Le lit va vers |
|---|---|
| `ground_pathStraight` | le nord et le sud |
| `ground_pathBend` | le sud et l'est (virage) |
| `ground_pathSplit` | l'ouest, l'est et le sud (embranchement) |
| `ground_pathCross` | les quatre côtés |
| `ground_pathEndClosed` | le sud seulement (fin d'un bras) |
| `ground_pathOpen` | partout (bassin, rivière large) |
| `ground_pathSide`, `Corner`, `CornerSmall`, `SideOpen` | bords d'une rivière large de plusieurs cases (berge au nord pour `Side`) |

Chaque +90° en Y tourne la pièce d'un quart de tour vers la droite (nord → est → sud → ouest). Par exemple, `Bend` relie le sud et l'est à 0, l'ouest et le sud à 90, le nord et l'ouest à 180, l'est et le nord à 270.

### Les cascades

5. `cliff_waterfall_rock`, posée comme l'habillage des falaises (section 10), le dos contre la falaise, sur la case du bas :

| Falaise de ce côté de la case | Rotation Y |
|---|---|
| sud | 0 |
| ouest | 90 |
| nord | 180 |
| est | 270 |

   Elle descend jusqu'au lit : `y` = niveau du bas moins `0.375`. Avec *Scale Y* = hauteur de la chute / 3 (ton réglage), le haut de la pièce arrive au fond du lit d'en haut et l'eau passe par-dessus ; avec (hauteur + 0,375) / 3, la roche monte jusqu'aux berges. Dans les deux cas, la nappe d'eau part de la surface du lit d'en haut. Par exemple, une chute de 3 m au sol se pose à `y = -0.375` en *Scale Y* `1`, et *Scale X* `2` pour une rivière de deux cases.
6. **Le ralentissement** : à chaque passage sur une voie, un objet `Ford Left`, `Ford Mid` ou `Ford Right` (*Slow Water*, *Speed Multiplier* `0.6`) couvre la rivière sur la voie. La boîte est dessinée en bleu quand il est sélectionné. Les volants ne sont pas concernés.

## 7 bis. L'eau

`Map > River Water` (composant `River Water`) construit l'eau au lancement d'après les pièces de `Map > River` :
- **La surface** : une nappe dans chaque case du lit, 20 cm sous les berges, découpée en carrés de 1 m pour les vagues. Le sens du courant est calculé tout seul : l'eau part de la source (le bout du lit le plus haut), descend les cascades, se partage au bassin et file vers les deux sorties.
- **Les cascades** : une nappe animée devant celle de chaque pièce `cliff_waterfall…`, avec un rebord qui déborde en haut, au niveau de l'eau du lit d'en haut.
- **Au pied des cascades** : de l'écume (petits éclats blancs low-poly qui jaillissent et retombent), un peu d'embruns, et un son en boucle (`Audio/Placeholder/waterfall_loop`, provisoire).

Les matériaux sont dans `Art/River`, avec le shader `Archery/Water` :
- **`River Water`** :
  - *Eau peu profonde* et *Eau profonde* : couleurs près des berges et au milieu ;
  - *Largeur de l'écume des berges* ;
  - *Vitesse du courant* ;
  - *Quantité de traînées d'écume* ;
  - *Hauteur des vagues*.
- **`Waterfall Water`** : la même chose pour les cascades (*Cascade* à 1).
- **`Water Foam`** : la couleur de l'écume.

Le shader se sert de la profondeur de la scène pour la couleur et l'écume des berges : *Depth Texture* est cochée dans `Settings/Project Configuration/Quality URP Config` et `Performance URP Config`.

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

- L'entrée est maintenant la grande face du fond de la voie du milieu (section 6). La grotte du prototype (`cliff_blockCave_stone`, à `(73.5, 0, 79.5)`) est cachée derrière : tu peux la supprimer.
- La porte : au pied de la face, au milieu de la voie, en `(67.5, 0, 67.5)`, tournée vers la tour (vers le sud-ouest). Elle peut être très grande : la partie lisse fait 12 m de large et 15 m de haut. Le nom du jeu se gravera au-dessus.
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

> **Petits décors partout, le 11 octobre** (`Map > Dressing > Small Decor`, 828 pièces) : herbes, massifs de fleurs d'une même couleur, champignons près des arbres, cailloux plats, cailloux ronds au pied des falaises, petites plantes ; par groupes de 2 à 7, plus des touffes isolées. Ils sont posés seulement sur le dessus des blocs de terrain (jungle, terrasses, clairières), à plat : ni sur les voies, ni dans la rivière, ni sur les escaliers et les rampes, et à l'écart de la tour, des coffres, des arbres et du décor existant. Herbes, fleurs, champignons et plantes n'ont pas de collider (on marche au travers) ; les cailloux en ont un, d'où les cailloux plats sur les zones de passage. Le groupe peut être effacé et refait d'un coup si la carte change.

## 11. Vérifier

1. Une vague dans chaque difficulté : les ennemis sortent des 3 voies, sont ralentis dans la rivière, s'arrêtent aux barricades, puis avancent vers la tour.
2. Les ennemis montent et descendent les rampes et les escaliers des voies, mais aucun ne monte dans la jungle ni ne reste coincé. Sinon, vérifie les rampes et rebake.
3. Les coffres apparaissent dans les clairières et s'ouvrent.
4. Depuis la tour, tu vois les trois voies sans tourner la tête de plus d'un quart de tour.
5. Tu descends de la tour, montes dans la jungle par une rampe ou un escalier (sans à-coups), sautes d'une terrasse et glisses dans une rampe. La bande du téléporteur te ramène en haut.
6. Les flèches se plantent dans les falaises, les arbres et le sol.
7. La nuit (Difficile, Impossible), la carte reste lisible.
