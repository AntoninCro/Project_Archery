# Guide : matériaux et tower defense

Ce guide suit le montage du tower defense du GDD (section 9), étape par étape.

## Étape 1 : les cristaux et les matériaux (montée le 11 octobre)

Rien à monter dans Unity : tout est déjà dans la scène `Prototype_Tir`.

### Ce qui a été ajouté

| Élément | Où | Rôle |
|---|---|---|
| `Crystal Spawner` | composant de l'objet `Game` (à côté du `Chest Spawner`) | Fait apparaître les gisements de chaque vague et annonce les récoltes |
| `Map > Crystal Spots` | 12 objets vides | Emplacements possibles des gisements : 6 dans chaque jungle, dont 5 sur les terrasses de 6 m, tous à plus de 18 m de la tour, sur un sol plat, loin des voies, de la rivière, des coffres et des arbres |
| `Prefabs/Crystal Deposit` | prefab | Un gisement : le modèle `detail-crystal-large` du Tower Defense Kit (échelle 2,4), une lumière violette qui pulse, un collider, et le script `Crystal Deposit` |
| `Art/Crystals/Crystal` | matériau | URP Lit, avec la texture du kit en couleur et en émission : les cristaux brillent dans leur propre couleur |
| `Scripts/Building` | `CrystalDeposit.cs`, `CrystalSpawner.cs` | Le gisement à viser et le générateur |
| `ScoreManager` | `Materials`, `AddMaterials`, `TrySpendMaterials` | La deuxième monnaie, remise à zéro à chaque partie |
| `HudDisplay` | ligne de l'or | « Or 120 · Mat. 35 » sur la montre et le panneau |

Sons (Kenney Impact Sounds) : une cloche (`impactBell_heavy_000`) à l'apparition, du verre qui éclate (`impactGlass_heavy_000`) à la récolte, du verre léger (`impactGlass_light_000`) quand un gisement disparaît.

### En jeu

1. Trois secondes après le début d'une vague, 3 gisements apparaissent (un de plus toutes les 2 vagues, 6 au plus), puis une deuxième série au début de la montée (refonte des vagues du 12 octobre, GDD section 3 bis). Une cloche et le message « 3 gisements de cristaux sont apparus ! » les annoncent. Ils sortent du sol et leur lueur pulse.
2. Vise un gisement avec la main libre, à 5 m au plus, et appuie sur la gâchette de prise : il éclate en éclats violets et donne ses matériaux (20 à la vague 1, +6 par vague depuis la refonte des vagues). Le message indique combien il en reste.
3. À la fin de la vague, les gisements non récoltés rétrécissent et disparaissent.
4. Les matériaux s'affichent après l'or sur la montre. Ils ne servent pas encore : la construction arrive à l'étape 2.

### Tester vite

- **V** (dans l'éditeur ou un *Development Build*) pose un gisement 3 m devant toi, à tout moment.
- **N** lance la vague suivante : les gisements apparaissent 3 s plus tard.

### Régler

Sur le composant `Crystal Spawner` de `Game` :

| Champ | Par défaut | Effet |
|---|---|---|
| *Base Count* / *Waves Per Extra* / *Max Count* | 3 / 2 / 6 | Nombre de gisements par série (deux séries par vague, *Second Batch*) |
| *Min Tower Distance* | 15 | Distance minimale à la tour |
| *Spawn Delay* | 3 | Délai après le début de la vague (s) |
| *Base Amount* / *Amount Per Wave* | 20 / 6 | Matériaux d'un gisement |

Sur le prefab `Crystal Deposit` : *Max Harvest Distance* (5 m), la taille du modèle (Scale de `detail-crystal-large`, 2,4) et la couleur de la lumière (`Glow`).

Pour déplacer un emplacement, bouge simplement son objet dans `Map > Crystal Spots`. Pour en ajouter un, duplique-en un et ajoute-le à la liste *Spots* du `Crystal Spawner`.

### Si ça ne marche pas

- **Rien n'apparaît** : la console affiche un message du `Crystal Spawner` (pas de prefab, ou aucun emplacement à plus de 15 m de la tour).
- **Le gisement est rose** : le matériau `Crystal` n'a pas trouvé son shader. Il doit utiliser *Universal Render Pipeline/Lit*, avec `colormap` en *Base Map* et en *Emission*.
- **On ne peut pas le viser** : le rayon de la main doit toucher le collider (la capsule verte autour des cristaux), à 5 m au plus.

## Étape 2 : les socles, le panneau, le plan bleu et le marteau

Cette étape construit tout le chemin d'un achat, du socle jusqu'au bâtiment terminé. Les bâtiments ne font encore rien : ce sont des modèles provisoires, qui prendront vie à l'étape 3.

> **Où en est cette étape** : montée par Claude le 11 octobre au soir, directement dans les fichiers, en suivant la partie « Montage » ci-dessous. Testée depuis dans le casque (13 octobre). Ce qui a été fait :
>
> - les 4 définitions remplies avec les valeurs du GDD et reliées à leur prefab ;
> - les prefabs `Build Socket`, `Build Hammer` et `Structures/Baliste`, `Givre`, `Barricade` et `Pieges` ;
> - dans la scène : `Build Panel` (copie du panneau de la boutique, avec les cartes 1 et 2), le `Build Manager` sur `Game`, et `Build Sockets` avec les 12 socles du tableau ci-dessous ;
> - 42 petits décors retirés sous les socles (herbes, fleurs, buissons, cailloux), ainsi que le monticule d'herbe de la clairière de droite, en (16 ; 7). Il prenait la seule place libre entre la voie du milieu et la rivière.
>
> | Socle | Position (X ; Z) | Rotation Y | Type |
> |---|---|---|---|
> | `Socle Gauche Voie 1` | (0 ; 15,2) | 0 | sur la voie, juste devant la barricade de départ |
> | `Socle Gauche Voie 2` | (0 ; 27) | 0 | sur la voie |
> | `Socle Gauche Bord 1` | (5,5 ; 15,5) | 270 | au bord, clairière de gauche |
> | `Socle Gauche Bord 2` | (5,75 ; 24,5) | 270 | au bord, clairière de gauche |
> | `Socle Milieu Voie 1` | (11,6 ; 11,6) | 45 | sur la voie, juste devant la barricade de départ |
> | `Socle Milieu Voie 2` | (19,1 ; 19,1) | 45 | sur la voie |
> | `Socle Milieu Bord 1` | (10 ; 19) | 135 | au bord, clairière de gauche |
> | `Socle Milieu Bord 2` | (17,5 ; 8) | 315 | au bord, clairière de droite, avant la rivière |
> | `Socle Droite Voie 1` | (15,2 ; 0) | 90 | sur la voie, juste devant la barricade de départ |
> | `Socle Droite Voie 2` | (27 ; 0) | 90 | sur la voie, après la rivière |
> | `Socle Droite Bord 1` | (14 ; 5) | 180 | au bord, clairière de droite |
> | `Socle Droite Bord 2` | (28,5 ; 5,5) | 180 | au bord, après la rivière |
>
> Tous sont à Y = 0, sur un sol plat, à l'écart des cibles d'entraînement et du trajet des cibles mobiles. Les socles « sur la voie » regardent vers la forêt, et les socles « au bord » vers leur voie.

### En jeu

1. Sur la carte, des **socles** marquent les emplacements de construction : quatre coins orange au sol. Il y en a deux sortes. **Sur la voie**, on peut construire une barricade ou des pièges à piques. **Au bord de la voie**, on peut construire une baliste ou une tour de givre.
2. Vise un socle avec la main libre, à 5 m au plus, et appuie sur la **poignée**. Un petit panneau s'ouvre devant toi, du côté du socle. Il montre tes matériaux et les deux bâtiments possibles, avec leur prix. Sur un bâtiment déjà construit, il montre le niveau suivant et son prix.
3. Clique une carte avec la **gâchette**. Les matériaux sont dépensés, et le bâtiment apparaît en **plan bleu transparent**. L'arc disparaît, et un **marteau** flotte dans ta main libre.
4. Attrape le marteau avec la **poignée**. Frappe le plan **3 fois**. Chaque coup fait un bruit, des étincelles et une vibration, et le plan se remplit d'un tiers, de bas en haut.
5. Au troisième coup, le bâtiment prend ses vraies couleurs. Le marteau disparaît, et l'arc réapparaît à sa place près de toi : reprends-le comme d'habitude.
6. Une amélioration (niveaux 2 et 3) se fait de la même façon. Vise le socle ou le bâtiment : le panneau propose le niveau suivant. Pendant les coups de marteau, le bâtiment repasse en plan bleu, mais il continue de fonctionner à son niveau actuel.

Quelques règles :

- **Une seule construction à la fois** : le marteau doit avoir fini avant d'en acheter une autre.
- **Le marteau ne se perd pas.** Si tu le lâches, il revient flotter dans la main qui l'a tenu en dernier.
- **Pour frapper de nouveau**, ressors la tête du marteau du plan, ou marque un arrêt, comme après un vrai coup. Un coup ne compte que s'il va vers le plan, assez vite (1,2 m/s).
- **La construction ne bloque jamais le joueur.** Si tu t'éloignes à plus de 15 m (par exemple avec le téléporteur de la tour), ou si la partie se termine, elle se termine toute seule et l'arc revient.
- **Le panneau se ferme** après un achat, avec son bouton X, ou quand tu t'éloignes de plus de 2,5 m.
- **Pendant la première construction, le plan ne bloque rien** : ni le joueur, ni les ennemis, ni les flèches.

### Ce qui a été ajouté

| Élément | Rôle |
|---|---|
| `Scripts/Building/StructureDefinition.cs` | Un bâtiment : nom, type de socle, prefab, couleur, et le prix et la description de chaque niveau. Menu contextuel « Valeurs du GDD » |
| `Scripts/Building/Structure.cs` | À la racine du prefab d'un bâtiment : son niveau, le plan bleu qui se remplit, et les objets qui apparaissent aux niveaux 2 et 3 (*Level Parts*). Les bâtiments de l'étape 3 en hériteront |
| `Scripts/Building/BuildSocket.cs` | Le socle qu'on vise. Au lancement, il vérifie que le sol est plat dessous, et prévient dans la console sinon |
| `Scripts/Building/BuildPanel.cs` | Le panneau des socles. Il réutilise les cartes de la boutique (`Prefabs/UI/Shop Card`) |
| `Scripts/Building/BuildManager.cs` | Sur `Game` : la liste des bâtiments, l'achat, le marteau, les coups et la fin de la construction |
| `Scripts/Building/BuildHammer.cs` | Le marteau : il flotte dans la main et détecte les coups sur le plan |
| `Shaders/Blueprint.shader`, `Resources/Blueprint.mat` | Le plan bleu : un hologramme parcouru de lignes qui montent, plus plein sous la partie construite. Déjà prêt, rien à faire |
| `Bow.cs` | L'arc peut se ranger pendant la construction : il devient invisible et impossible à prendre |
| `ShopOffer` / `ShopCard` | Une carte peut afficher son prix en matériaux (« 40 mat. ») |

### Montage

Compte environ 1 h 30. Fais les parties dans l'ordre : chacune sert à la suivante.

#### 1. Les quatre bâtiments (données)

1. Crée le dossier `Assets/_Project/Data/Structures`.
2. Dedans, crée 4 assets avec **Create > Archery > Structure Definition** : `Barricade`, `Pieges`, `Givre` et `Baliste`.
3. Sur chacun, choisis le **Kind** (*Barricade*, *Spikes*, *Frost Tower*, *Ballista*). Clique ensuite sur les trois points en haut à droite de l'Inspector et choisis **Valeurs du GDD**. Le nom, le type de socle, la couleur, les prix (GDD, section 9) et les descriptions se remplissent.
4. Le champ **Prefab** se remplit à la partie suivante.

#### 2. Les bâtiments provisoires (prefabs)

Ils servent à voir le plan bleu et le résultat. L'étape 3 leur donnera leur comportement et leurs niveaux.

Crée le dossier `Assets/_Project/Prefabs/Structures`. Pour chaque bâtiment :

1. Crée un objet vide à l'origine de la scène, au nom du bâtiment.
2. Ajoute-lui le composant **Structure**.
3. Ajoute le modèle en enfant, comme dans le tableau ci-dessous. Garde le pied du bâtiment à Y = 0, et son centre en X = Z = 0 : c'est le centre du socle.
4. Glisse l'objet dans `Prefabs/Structures` pour en faire un prefab, puis supprime-le de la scène.
5. Mets ce prefab dans le champ **Prefab** de sa définition.

| Prefab | Modèle provisoire |
|---|---|
| `Baliste` | Tower Defense Kit : `wood-structure` (Scale 2,4) au pied, et `weapon-ballista` (Scale 2,4) posé dessus, vers Y = 1,2. L'avant (Z) de la baliste regarde vers l'avant du prefab |
| `Givre` | Tower Defense Kit : `tower-round-bottom-a` (Scale 2,4), `tower-round-top-a` (Scale 2,4) posé dessus vers Y = 1,44, et `tower-round-crystals` (Scale 2,4) au sommet. Ajuste les hauteurs à l'œil |
| `Barricade` | Ouvre `Prefabs/Barricade`, copie son enfant `Intact` (les planches), et colle-le dans le nouveau prefab. Ses colliders peuvent rester |
| `Pieges` | Un cube aplati (*3D Object > Cube*, Scale (2,6 ; 0,1 ; 2,6), Position Y 0,05), avec le matériau `Barricade_Wood`. Supprime son *Box Collider*. Les vraies piques arrivent à l'étape 3 |

Les modèles du Tower Defense Kit mesurent 1 m et n'ont pas de collider. Si une pièce est blanche ou rose, mets-lui un matériau *URP Lit*, avec `kenney_tower-defense-kit/Models/FBX format/Textures/colormap.png` en *Base Map*.

#### 3. Le marteau

1. Crée un objet vide `Build Hammer` à l'origine.
2. Ajoute-lui ces composants :
   - un **Rigidbody** : décoche *Use Gravity* et coche *Is Kinematic* ;
   - un **XR Grab Interactable** : *Movement Type* sur *Instantaneous*, et décoche *Throw On Detach* et *Use Dynamic Attach* ;
   - un **Build Hammer** ;
   - un **Capsule Collider** : *Center* (0 ; 0,37 ; 0), *Radius* 0,05, *Height* 0,75, *Direction* *Y-Axis*.
3. Glisse en enfant le modèle `AgeSoft/Stylized Mechanized Hammer/Mesh/Stylized Hammer` et donne-lui le matériau `MechanizedHammer` (le marteau stylisé que tu as ajouté le 12 octobre, à la place du warHammer d'*Easy Weapons*). Renomme-le `Model`, et règle *Rotation* (0 ; −90 ; 0) et *Scale* 0,4. Le marteau mesure alors 75 cm, manche vers le haut ; la rotation tourne vers l'avant la bonne face de la tête (90° faisait frapper avec l'autre face, corrigé après les tests du 12 octobre).
4. Le matériau du pack était sur le shader 2D d'URP (*Mesh2D-Lit-Default*) : il est passé en *Universal Render Pipeline/Lit*, avec ses textures (couleur, normales, métal et lissé, émission cyan). La texture *MetallicSmoothness* est importée en linéaire (*sRGB* décoché).
5. Ajoute un enfant vide `Grip` : *Position* (0 ; 0,2 ; 0), *Rotation* (−40 ; 0 ; 0). C'est là que la main tient le manche. Glisse-le dans *Attach Transform* du XR Grab Interactable.
6. Ajoute un enfant vide `Head` : *Position* (0 ; 0,59 ; 0), au centre de la tête. Glisse-le dans *Head* du Build Hammer, avec *Head Radius* à 0,17. La sphère jaune du gizmo doit entourer la tête du marteau.
7. Garde le calque **Default** sur tous les objets : la main n'attrape que les objets de ce calque.
8. Glisse `Build Hammer` dans `Assets/_Project/Prefabs` pour en faire un prefab, puis supprime-le de la scène.

Dans le casque, le manche doit pointer vers le haut et un peu vers l'avant, et la tête doit frapper vers l'avant. Si le marteau penche trop, change *Rotation X* de `Grip` (−40). Pour frapper avec l'autre face de la tête, mets *Rotation Y* de `Model` à 90.

#### 4. Le socle

1. Crée un objet vide `Build Socket` à l'origine.
2. Ajoute-lui ces composants :
   - un **Box Collider** : *Size* (3 ; 0,05 ; 3), *Center* (0 ; 0,025 ; 0). C'est la plaque que vise le rayon de la main. Les flèches la traversent ;
   - un **Build Socket**. Laisse sa liste *Colliders* vide : il trouve le Box Collider tout seul.
3. Glisse en enfant le modèle `selection-a` du Tower Defense Kit (les quatre coins). Renomme-le `Marker`, et règle *Position* (0 ; 0,03 ; 0) et *Scale* (3 ; 2 ; 3). Glisse-le dans *Marker* du Build Socket. Les coins respirent doucement et grossissent quand le rayon les vise. Ils se cachent une fois le bâtiment posé.
4. Fais-en le prefab `Prefabs/Build Socket`, puis supprime-le de la scène.

Garde **Scale 1** sur la racine du socle : le bâtiment s'y pose en enfant et prendrait son échelle. Pour un socle plus petit, change plutôt *Size* et l'échelle de `Marker`.

#### 5. Le panneau

Il est unique et se place tout seul devant le joueur : sa position dans la scène n'a pas d'importance.

1. Crée **UI > Canvas** à la racine de la scène, et renomme-le `Build Panel`.
2. Règle le Canvas :
   - *Render Mode* sur *World Space*, et *Event Camera* sur la *Main Camera* du XR Origin ;
   - *Width* 700, *Height* 560 et *Scale* (0,001 ; 0,001 ; 0,001), soit 70 × 56 cm.
3. Supprime le *Graphic Raycaster* et ajoute un **Tracked Device Graphic Raycaster**, comme sur la boutique. Ajoute aussi le composant **Build Panel**.
4. Crée ses enfants :
   - **`Content`** : *UI > Image*, ancrée sur tout le Canvas (Alt + clic sur le préréglage en bas à droite), *Left/Top/Right/Bottom* à 0, couleur sombre (20, 20, 26, 235). Tous les objets suivants vont dedans ;
   - **`Title`** : *UI > Text - TextMeshPro*, ancré en haut sur toute la largeur, *Pos Y* −45, *Height* 70, marges *Left/Right* 90, taille 44, centré, en gras ;
   - **`Materials`** : un texte, ancré en haut sur toute la largeur, *Pos Y* −105, *Height* 45, taille 32, centré, en violet (185, 140, 255) ;
   - **`Cards`** : un objet vide, ancré au centre, *Pos Y* −5, *Width* 630, *Height* 300. Ajoute-lui un **Grid Layout Group** : *Cell Size* 305 × 300, *Spacing* 20 × 20, *Child Alignment* *Middle Center*, *Constraint* *Fixed Column Count* à 2. Glisse dedans **deux** prefabs `Prefabs/UI/Shop Card` ;
   - **`Close Button`** : *UI > Button - TextMeshPro*, ancré en haut à droite, *Pos* (−45 ; −45), 70 × 70, texte « X » en taille 40 ;
   - **`Message`** : un texte, ancré en bas sur toute la largeur, *Pos Y* 35, *Height* 50, taille 30, centré.
5. Remplis le composant **Build Panel** : *Content*, *Title Text*, *Materials Text*, *Cards* (les deux cartes), *Close Button* et *Message Text*. Laisse les réglages de placement par défaut.
6. Sons conseillés : *Open Clip* `kenney_rpg-audio/Audio/bookOpen`, *Close Clip* `bookClose`, *Buy Clip* `handleCoins`, et pour *Error Clip* le même que celui de la boutique.

Le panneau cache `Content` au lancement.

#### 6. Le Build Manager

Sur l'objet **`Game`**, ajoute le composant **Build Manager** :

| Champ | Valeur |
|---|---|
| *Structures* | les 4 définitions de la partie 1 |
| *Hammer Prefab* | `Prefabs/Build Hammer` |
| *Blueprint Material* | vide : il prend `Resources/Blueprint` tout seul |
| *Strikes To Build* | 3 |
| *Auto Finish Distance* | 15 |
| *Strike Effect* | `Prefabs/Effects/Hit Surface` |
| *Hammer Clip* | `kenney_rpg-audio/Audio/drawKnife1` |
| *Strike Clips* | 5 éléments : `kenney_impact-sounds/Audio/impactMining_000` à `impactMining_004` |
| *Complete Clip* | `kenney_impact-sounds/Audio/impactBell_heavy_002` |

#### 7. Les 12 socles sur la carte

1. Crée un objet vide `Build Sockets` à la racine de la scène, en (0, 0, 0). Ne le mets pas sous `Map`, et ne le coche pas *Static*.
2. Pose dedans 12 instances de `Prefabs/Build Socket`, **4 par voie** :
   - **2 sur la voie** (*Slot* : *Lane*), au milieu de la voie. Tourne-les comme les barricades : l'axe Z (flèche bleue) **vers la forêt**, d'où viennent les ennemis. Une barricade construite s'y posera dans ce sens. Laisse au moins 6 m entre deux socles, et entre un socle et la barricade de départ (sa zone d'arrêt fait 5 m). Une bonne idée : un socle juste devant la barricade de départ, côté forêt. Des pièges posés là blessent les ennemis pendant qu'ils frappent la barricade ;
   - **2 au bord de la voie** (*Slot* : *Roadside*), à côté de la voie sans empiéter dessus, à 4 à 6 m de son milieu : la tour de givre ne ralentit qu'à 8 m, et la baliste tire à 20 m. Tourne leur axe Z vers la voie.
3. Chaque socle a besoin d'un sol plat de 3 × 3 m. La boîte du gizmo (orange sur la voie, bleue au bord) montre la place prise, et un trait montre son avant.
4. Donne-leur des noms clairs, par exemple `Socle Gauche Voie 1` ou `Socle Milieu Bord 2`. Pour un autre texte que « Sur la voie » dans le panneau, remplis *Display Name*.

Pas besoin de refaire le NavMesh : les socles et les bâtiments n'y entrent pas.

### Tester vite

- **B** (dans l'éditeur ou un *Development Build*) donne 50 matériaux.
- **H** termine tout de suite la construction en cours, sans marteau.
- **V** pose un gisement de cristaux devant toi, et **N** lance la vague suivante.

Au lancement, la console signale les socles posés sur un sol qui n'est pas plat.

### Régler

| Où | Champ | Par défaut | Effet |
|---|---|---|---|
| `Build Manager` | *Strikes To Build* | 3 | Coups de marteau par construction |
| | *Auto Finish Distance* | 15 | Distance (m) au-delà de laquelle la construction se termine toute seule |
| `Build Hammer` (prefab) | *Head Radius* | 0,17 | Taille (m) de la tête du marteau pour toucher le plan |
| | *Min Strike Speed* | 1,2 | Vitesse (m/s) minimale d'un coup |
| | *Min Strike Interval* | 0,25 | Temps (s) minimal entre deux coups |
| `Build Socket` | *Max Reach* | 5 | Distance (m) maximale entre la main et le socle |
| `Build Panel` | *Distance*, *Height From Head*, *Tilt* | 1,2 / −0,05 / 10 | Place du panneau devant les yeux, du côté du socle |
| | *Close Distance* | 2,5 | Le panneau se ferme quand on s'éloigne de plus de cette distance (m) |
| Définitions | *Levels* | GDD | Prix et description de chaque niveau |
| `Resources/Blueprint` | couleurs, *Line Spacing* | bleu | Couleurs du plan vide, de la partie construite et des bords, espacement des lignes |

### Si ça ne marche pas

- **Le panneau ne s'ouvre pas.** Le rayon de la main doit toucher la plaque du socle (son Box Collider), à 5 m au plus, et la main ne doit rien tenir. Une seule construction se fait à la fois : finis d'abord celle en cours.
- **On ne peut pas cliquer les cartes.** Le Canvas a besoin d'un *Tracked Device Graphic Raycaster*. Comme dans la boutique, une main qui tient quelque chose ne clique pas.
- **Les cartes sont vides.** Les définitions doivent être dans la liste *Structures* du Build Manager, avec un *Prefab*, et avoir le bon *Slot* (« Valeurs du GDD » le règle).
- **Le plan est rose.** `Resources/Blueprint` doit utiliser le shader *Archery/Blueprint*.
- **Le marteau n'apparaît pas.** La console affiche un message du Build Manager (pas de prefab de marteau).
- **On n'attrape pas le marteau.** Il doit avoir son collider, sur le calque *Default*, et un Rigidbody.
- **Les coups ne comptent pas.** Vérifie que la sphère jaune de `Head` entoure la tête du marteau. Frappe franchement, puis ressors du plan avant le coup suivant.
- **L'arc ne revient pas.** Il réapparaît à sa place près de toi : un peu devant et en bas, du côté de la main qui le tenait.

## Étape 3 : les quatre bâtiments et leurs niveaux

Les bâtiments construits à l'étape 2 prennent vie : la barricade arrête les ennemis, les piques les blessent, la tour de givre les ralentit et la baliste leur tire dessus. Chaque niveau les renforce et se voit.

> **Où en est cette étape** : montée par Claude le 12 octobre, directement dans les fichiers. Testée depuis dans le casque (13 octobre). Les 4 prefabs de `Prefabs/Structures` ont été refaits avec leur composant et leurs pièces de niveau. Ils gardent les mêmes identifiants, donc les définitions et les socles n'ont pas bougé. Il n'y a rien à monter dans la scène.

### En jeu

| Bâtiment | Ce qu'il fait | Niveau 2 | Niveau 3 |
|---|---|---|---|
| **Barricade** (sur la voie) | Comme celles de départ : les ennemis au sol qui arrivent devant elle, côté forêt, s'arrêtent pour la frapper. 400 PV | 700 PV ; une planche et une traverse en plus | 1 000 PV ; des pieux tournés vers la forêt. Chaque coup reçu au corps à corps blesse l'ennemi qui frappe (10 dégâts) |
| **Pièges à piques** (sur la voie) | Toutes les 0,8 s, les ennemis au sol sur le plateau perdent 12 PV. Les piques jaillissent à chaque coup | 18 PV toutes les 0,65 s, piques plus hautes | 27 PV toutes les 0,5 s, et les ennemis sur le piège sont ralentis de 20 % |
| **Tour de givre** (au bord) | Ralentit de 30 % les ennemis à 8 m, volants compris. Un disque bleuté montre sa portée au sol | 40 % à 9 m ; cristaux plus grands, lumière plus forte | 50 % à 10 m |
| **Baliste** (au bord) | Pivote vers l'ennemi le plus proche à 20 m qu'elle voit, volants compris, et tire un carreau de 15 dégâts toutes les 2,4 s. Elle vise là où l'ennemi sera | 22 dégâts toutes les 1,6 s ; un fanion | 32 dégâts chaque seconde ; le carreau traverse 2 ennemis ; un drapeau |

Quelques règles :

- **Les dégâts indiqués sont ceux de la vague 1 en Normal** (étape 4) : ils suivent ensuite la force des ennemis, difficulté et vague comprises.
- **La baliste profite de la moitié des bonus de dégâts de l'arc** (améliorations de dégâts, bonus « dégâts doublés » des coffres). Avec +60 % de dégâts sur l'arc, elle fait +30 %. Ses carreaux comptent la zone touchée (tête, points faibles du boss).
- **Les bâtiments rapportent leurs éliminations** (points et or), mais pas de points de touche, et ils ne font pas monter le combo.
- **Seules les barricades peuvent être détruites.** Une barricade construite est réparée avec les autres par la carte « Barricades » de la boutique. L'améliorer la remet aussi debout, avec tous ses PV.
- **Pendant une amélioration**, le bâtiment repasse en plan bleu mais continue de fonctionner à son niveau actuel. Le disque de la tour de givre reste visible.
- **Les ralentissements ne s'additionnent pas** : c'est le plus fort qui compte (tour de givre, piques, gué, flèches de glace et de foudre).

### Ce qui a été ajouté

| Élément | Rôle |
|---|---|
| `Scripts/Building/StructureBarricade.cs` | Sur la barricade construite : active sa `Barricade` une fois construite, règle ses PV à chaque niveau, et gère les pointes du niveau 3 |
| `Scripts/Building/SpikeTrap.cs` | Les pièges : coups réguliers sur les ennemis au sol dans la zone, ralentissement au niveau 3, piques générées au lancement (5 × 5) |
| `Scripts/Building/FrostTower.cs` | La tour de givre : ralentissement autour d'elle, disque de portée, cristaux et lumière qui grandissent |
| `Scripts/Building/Ballista.cs`, `BallistaBolt.cs` | La baliste (cible, rotation, tir, bonus de l'arc) et son carreau (vol en ligne droite, zones touchées, traversée, effets d'impact) |
| `Structure.cs` | Les bâtiments peuvent exclure un rendu du plan bleu (le disque de portée) et lire une valeur « par niveau » |
| `Barricade.cs`, `HitEffects.cs`, `IceZone.cs` | Réparation sans son, effets d'impact pour les carreaux, disque de la flèche de glace réutilisé par la tour de givre |
| `Prefabs/Structures/Ballista Bolt` | Le carreau : modèle `weapon-ammo-arrow` du Tower Defense Kit (échelle 1,5), traînée `Arrow_Trail` |
| `Art/Structures/Spikes_Metal` | Matériau métallique des piques |

Contenu des prefabs :

- **`Barricade`** : une copie complète du prefab `Barricade` (planches, débris, barre de vie, Health, NavMesh Modifier), élargie à 1,5 comme celles de la carte. Sa `Barricade` est décochée au départ, et le script l'active à la fin de la construction. Les enfants `Niveau 2 (renfort)` et `Niveau 3 (pieux)` sont ses *Level Parts*.
- **`Pieges`** : le plateau en bois de l'étape 2. Les piques sont créées par le script.
- **`Givre`** : les trois pièces du kit, plus une lumière `Glow` bleutée au sommet. Les cristaux (`tower-round-crystals`) sont l'objet agrandi à chaque niveau.
- **`Baliste`** : l'estrade en bois, avec un `Plancher` en planches (ajouté après les tests du 12 octobre : l'échafaudage du kit est ouvert et la baliste semblait flotter), la baliste du kit posée dessus (`weapon-ballista`, la *Turret* qui pivote), plus le fanion et le drapeau du Castle Kit, ses *Level Parts*.

### Tester vite

1. **B** plusieurs fois pour avoir des matériaux, puis construis un bâtiment de chaque type. **H** termine une construction sans marteau.
2. **N** lance une vague. Regarde la baliste pivoter et tirer, les ennemis ralentir près de la tour de givre, les piques jaillir et les ennemis s'arrêter devant une barricade construite.
3. Améliore un bâtiment (vise le socle ou le bâtiment) : le fanion, les cristaux, les piques ou les pieux doivent apparaître ou grandir.

Dans l'éditeur, sélectionne un bâtiment pendant le jeu : un gizmo montre sa portée (sphère jaune de la baliste, cercle bleu de la tour de givre, boîte rouge des piques).

### Régler

Tous les réglages sont sur le composant à la racine de chaque prefab de `Prefabs/Structures`. Les tableaux « Per Level » ont un élément par niveau.

| Prefab | Champ | Par défaut | Effet |
|---|---|---|---|
| `Barricade` | *Health Per Level* | 400 / 700 / 1 000 | PV |
| | *Thorns From Level*, *Thorns Damage* | 3, 10 | Pointes : niveau d'apparition, dégâts par coup reçu |
| `Pieges` | *Damage Per Level*, *Interval Per Level* | 12 / 18 / 27, 0,8 / 0,65 / 0,5 s | Dégâts et rythme des coups (plus lents aux premiers niveaux depuis les tests du 12 octobre) |
| | *Slow From Level*, *Slow Multiplier* | 3, 0,8 | Ralentissement (0,8 = 20 % plus lents) |
| | *Size*, *Spike Height Per Level* | 2,6 × 2,6 m, 0,3 / 0,38 / 0,46 m | Zone piégée, hauteur des piques |
| `Givre` | *Radius Per Level*, *Speed Per Level* | 8 / 9 / 10 m, 0,7 / 0,6 / 0,5 | Portée, vitesse des ennemis ralentis |
| | *Range Color*, *Crystal Scale Per Level*, *Light Intensity Per Level* | bleu, 1 / 1,15 / 1,3, 1,5 / 2,2 / 3 | Apparence |
| `Baliste` | *Range*, *Damage Per Level*, *Interval Per Level* | 20 m, 15 / 22 / 32, 2,4 / 1,6 / 1 s | Portée, dégâts, cadence (plus lente aux premiers niveaux depuis les tests du 12 octobre) |
| `Data/Difficulties` | *Structure Power* | 0,75 / 1 / 1,25 / 1,6 | Puissance des bâtiments selon la difficulté (Facile, Normal, Difficile, Impossible), en plus de la force des ennemis |
| | *Pierce Per Level*, *Bow Bonus Share* | 0 / 0 / 2, 0,5 | Ennemis traversés, part des bonus de l'arc |
| | *Bolt Speed*, *Turn Speed*, *Max Pitch* | 45 m/s, 220 °/s, 40° | Carreau et rotation |
| | *Muzzle Offset* | (0 ; 0,41 ; 0,45) | Départ du carreau, dans le repère du modèle de la baliste (avant son échelle 2,4) |

Les prix et les descriptions restent dans les définitions (`Data/Structures`). Pense à les mettre à jour si tu changes une valeur.

### Si ça ne marche pas

- **La baliste ne tire pas.** Elle ne vise que les ennemis à 20 m qu'elle voit : un relief entre elle et l'ennemi la bloque. Vérifie aussi *Bolt Prefab* et *Turret*.
- **Les carreaux partent de travers ou d'ailleurs.** Ajuste *Muzzle Offset*, dans le repère du modèle de la baliste.
- **Le disque de la tour de givre est rose ou invisible.** *Range Material* doit utiliser le shader *Archery/UnlitVertexColor* (`Materials/Effects`).
- **Les piques sont invisibles.** *Spike Material* doit être renseigné (`Art/Structures/Spikes_Metal`).
- **Les ennemis ne s'arrêtent pas devant une barricade construite.** Son avant (axe Z) doit pointer vers la forêt : c'est l'avant du socle.

## Étape 4 : l'équilibrage

> **Où en est cette étape** : faite par Claude le 12 octobre, sur simulation. Testée depuis en jeu (13 octobre), puis réglée à la main : les valeurs de la scène et des assets font foi. Il n'y a rien à monter : les valeurs sont déjà dans les prefabs, les définitions et la scène.

### Le choix de départ

Le tower defense doit être **très présent** à la fin d'une partie (10 vagues à l'époque, 5 depuis la refonte du 12 octobre : les matériaux ont été recalés pour arriver au même total à la vague 5) : presque tous les socles construits, et plusieurs bâtiments au niveau 3.

### Ce qui a changé

| Quoi | Avant | Après |
|---|---|---|
| Matériaux d'un gisement (`Crystal Spawner` sur `Game`) | 10, +1 par vague | **20, +2 par vague** |
| Dégâts des bâtiments | fixes | **suivent la force des ennemis** : PV de la difficulté × renforcement de la vague, mode infini compris |
| Baliste (dégâts à la vague 1 en Normal) | 25 / 35 / 50 | **15 / 22 / 32** |
| Pièges à piques | 20 / 30 / 45 | **12 / 18 / 27** |
| Pointes de la barricade | 15 | **10** |
| Textes des cartes | dégâts en PV | **multiplicateurs** (« Dégâts ×1,5 »), toujours justes puisque les dégâts suivent les ennemis |
| Fin de vague | — | message **« Défenses : … % des dégâts »** et détail dans la console |

Les prix, les PV des barricades, la tour de givre et les cadences de la baliste n'ont pas changé.

### Pourquoi

Une simulation (vagues, ennemis et difficultés lus dans les réglages du jeu, 70 % des gisements récoltés) a donné :

- **Matériaux** : environ 140 à la vague 3, 600 à la vague 8 et 900 à la vague 10. Avant, on en avait deux fois moins (445 à la vague 10, aucun niveau 3).
- **Avec des dégâts fixes**, la part des PV qu'une voie défendue peut retirer allait, à la vague 8, d'environ 20 % en Impossible à 460 % en Facile. Les bâtiments écrasaient les premières vagues des difficultés faciles, et ne servaient presque plus en Impossible.
- **Avec des dégâts qui suivent la force des ennemis**, cette part ne dépend plus de la difficulté : un bâtiment garde le même effet toute la partie, ce sont ses niveaux qui le renforcent. Les dégâts de départ sont réduits de 40 % pour que les bâtiments aident beaucoup sans tout faire à la place de l'arc.

Le modèle est grossier : il ne sait pas combien d'ennemis tu tues avant qu'ils arrivent aux bâtiments, ni où tournent les volants. Le message de fin de vague sert à vérifier en jouant.

### Régler en jouant

À la fin de chaque vague, un message près de toi donne la **part des dégâts faite par les bâtiments** et leurs éliminations. La console détaille chaque type de bâtiment et la force des ennemis de la vague.

Repères pour une partie en Normal où l'on construit dès que possible :

| Vagues | Part des bâtiments visée |
|---|---|
| 2 à 4 | 15 à 30 % |
| 5 à 8 | 30 à 50 % |
| 9 et 10 | 40 à 60 % |

- **Trop forts** (l'arc devient inutile) : baisse les dégâts de la baliste et des piques (*Damage Per Level*), ou les matériaux (*Base Amount* et *Amount Per Wave* du `Crystal Spawner`).
- **Trop faibles** : fais l'inverse, ou baisse les prix dans les définitions (`Data/Structures`).
- **Un bâtiment que personne n'achète** : baisse son prix, ou augmente l'effet de son niveau 1.
- **Pour revenir à des dégâts fixes**, décoche *Follow Enemy Strength* sur le bâtiment.

Si tu changes une valeur, pense à mettre à jour le texte de sa carte (définition) et le GDD (section 9).
