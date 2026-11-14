# Guide : faire un bâtiment dans Blender (premier exercice : le Givre)

Ce guide est pour quelqu'un qui n'a jamais ouvert Blender. Il sert à la refonte visuelle (`Refonte_Visuelle.md`) : tu
assembles toi-même chaque bâtiment avec des formes simples et des pièces de packs faits par des humains, et Claude le branche
sur le jeu. Les menus sont donnés avec leurs noms anglais (laisse Blender en anglais, c'est plus simple pour suivre).

**Ton Givre n'a pas à ressembler au mien.** Les images de référence donnent la taille et l'idée ; la forme, les pièces, les
couleurs, c'est toi qui décides. Claude ne te dira pas où poser une pièce : sinon le modèle redeviendrait le sien.

Compte une soirée pour les parties 1 et 2 (installer, apprendre les gestes), puis une ou deux pour le Givre.

## 1. Installer Blender

1. Sur **blender.org**, *Download* : la dernière version stable (ou la version « LTS », suivie plus longtemps). Installe-la.
2. Au premier lancement, garde les réglages proposés (*Left* pour sélectionner), et ferme la fenêtre d'accueil (clic à côté).
3. Pas de pavé numérique (portable) ? Ce n'est pas grave : ce guide passe par les menus pour changer de vue.
4. Pour aller plus loin, le manuel de Blender existe en français : docs.blender.org (choisir la langue en haut).

## 2. Les gestes de base

Ouvre un fichier vide (*File > New > General*). Le cube du milieu sert à t'entraîner.

### 2.1 Regarder

| Geste | Pour |
|---|---|
| Molette enfoncée + glisser | tourner autour |
| Maj + molette enfoncée + glisser | se déplacer |
| Molette | zoomer |
| *View > Frame Selected* (pavé `.`) | recentrer sur ce qui est sélectionné |
| *View > Viewpoint > Front* / *Right* / *Top* (pavé 1, 3, 7) | les vues de face, de droite, de dessus (en orthographique : sans perspective, idéal pour poser les pièces) |

En haut à droite de la vue, les quatre petites sphères changent l'affichage : la troisième, **Material Preview**, montre les
couleurs et les textures des pièces.

### 2.2 Sélectionner, déplacer, tourner, agrandir

| Touche | Pour |
|---|---|
| Clic gauche ; Maj + clic | sélectionner une pièce ; en ajouter d'autres (la dernière cliquée est l'« active », plus claire) |
| `A` ; `Alt+A` | tout sélectionner ; plus rien |
| `G` (*grab*), `R` (*rotate*), `S` (*scale*) | déplacer, tourner, agrandir à la souris ; clic gauche ou Entrée pour valider, clic droit ou Échap pour annuler |
| puis `X`, `Y` ou `Z` | seulement le long de cet axe (rouge, vert, bleu) |
| puis un nombre | la valeur exacte : `G` `Z` `1` Entrée monte la pièce de 1 m ; `R` `Z` `45` la tourne de 45° ; `S` `2` la double |
| `Ctrl` en déplaçant | par crans de la grille |
| `Ctrl+Z` | annuler |

**Le panneau des valeurs** : `N` ouvre un panneau à droite ; son onglet *Item* donne la place (*Location*), la rotation, l'échelle
et la taille réelle (*Dimensions*, en mètres) de la pièce sélectionnée. On peut y taper les valeurs.

### 2.3 Ajouter, copier, renommer, supprimer

| Touche | Pour |
|---|---|
| `Maj+A` > *Mesh* | ajouter une forme : *Cube*, *Cylinder*, *Cone*, *UV Sphere*, *Ico Sphere*, *Torus* |
| `Maj+D` | dupliquer (la copie suit la souris ; clic pour la poser) |
| `F2` | renommer la pièce active |
| `X` > *Delete* | supprimer |

**Juste après avoir ajouté une forme**, un petit panneau s'ouvre en bas à gauche (*Add Cylinder*…) : on y règle ses côtés
(*Vertices*), son rayon, sa hauteur. **Mets très peu de côtés** (le style choisi le 14 novembre : du low poly très simple) :
6 à 8 pour un cylindre (32 par défaut), une *Ico Sphere* à 1 subdivision, ou une *UV Sphere* de 8 segments et 4 anneaux. Le
panneau disparaît à l'action suivante.

**Ce qui bouge** (une tourelle qui tourne, un yo-yo, un bras) : tu le fais immobile, en pièce à part, et le code le fera
bouger. Il lui faut seulement son nom (celui de la fiche) et son origine là où il tourne (2.5).

### 2.4 Retoucher une pièce (le mode édition)

1. Sélectionne la pièce, `Tab` : le mode édition (ses sommets apparaissent).
2. En haut à gauche de la vue, trois icônes : sommets, arêtes, faces. Choisis *faces*.
3. Clic sur une face (Maj + clic pour plusieurs), puis `G`, `S` ou `R` comme avant ; `E` tire la face pour en faire une
   nouvelle partie (*extrude*) ; `Ctrl+B` arrondit une arête (*bevel*, la molette règle le nombre de pans).
4. `Tab` pour sortir.

Ça marche aussi sur les pièces de Kenney : raccourcir un mur, élargir un toit, enlever un morceau (`X` > *Faces*).

### 2.5 Ranger

| Touche | Pour |
|---|---|
| `Ctrl+J` | joindre les pièces sélectionnées en une seule (elle prend le nom et l'origine de l'active) |
| `Ctrl+P` > *Object* | rattacher les pièces sélectionnées à l'active (son « enfant » : elles la suivent) |
| `M` > *+ New Collection* | ranger dans une collection (un dossier de la liste en haut à droite, l'*Outliner* ; l'œil la cache) |
| `Ctrl+A` > *Rotation & Scale* | « appliquer » la rotation et l'échelle (la pièce garde sa forme, ses réglages reviennent à zéro et à 1) |

**L'origine** : le petit point orange d'une pièce. C'est autour de lui qu'elle tourne et grandit dans le jeu. Pour le placer :
le curseur 3D (le cercle rouge et blanc) se pose par `Maj` + clic droit, ou au bon endroit par le panneau `N`, onglet *View*,
*3D Cursor* ; puis *Object > Set Origin > Origin to 3D Cursor*.

**Enregistrer** : `Ctrl+S`.

**Pour s'entraîner** (20 minutes, à jeter ensuite) : une table et un tabouret, avec des cubes et des cylindres de 8 côtés, à la
bonne taille (une table fait 75 cm de haut), en utilisant `G`, `R`, `S`, le panneau `N`, `Maj+D` et `Ctrl+J`.

## 3. Le Givre

### 3.1 Ce que le jeu attend du Givre (le cahier des charges, pas la forme)

- **Une tour de glace** : elle ralentit les ennemis dans sa zone (9 m autour d'elle, volants compris), et ce qu'elle lance (ses
  rayons de glace, ses boules de glace dans l'arène de MK) part de ses cristaux.
- **La taille au sol** : au niveau 1, sa largeur décide du nombre de cases qu'elle prend sur la grille de pose (1 case = 1 m,
  arrondi au-dessus). Le mien fait 2,4 × 2,4 m au sol, donc 3 × 3 cases. Plus large, elle prendra plus de place ; les niveaux 2
  à 5 peuvent déborder (mon verglas fait 3 m).
- **La hauteur** : la mienne fait 4,6 m au niveau 1, 8,5 m au niveau 5 (avec le cœur qui flotte au-dessus).
- **Les cristaux** : la seule pièce que le code utilise. Une pièce (ou plusieurs jointes, `Ctrl+J`) **nommée exactement
  `tower-round-crystals`** (le nom de la pièce de Kenney qu'elle remplace : c'est par ce nom que l'outil de Claude la
  retrouve). Le code l'agrandit à chaque niveau (×1, ×1,08, ×1,16, ×1,24, ×1,35) **autour de son origine**, et la boule de
  glace part 60 cm au-dessus de cette origine : mets l'origine **à la base des cristaux, au centre**. Chez moi, elle est à
  2,63 m de haut ; chez toi, là où tu poses tes cristaux.
- **La lumière bleue** (`Glow`) : ce n'est pas un modèle, rien à faire dans Blender (Claude la déplace si ta tour est bien plus
  haute ou plus basse que la mienne).
- **Les niveaux** (ce que chacun fait en jeu ; à toi de lui donner une forme qui le raconte) :
  - **Niveau 2** : le givre reste 2 s sur les ennemis qui sortent de la zone (le mien n'ajoute rien de visible : libre à toi) ;
  - **Niveau 3, le Pic de glace** : de temps en temps, elle gèle net un ennemi, et ta flèche brise la glace ;
  - **Niveau 4, le Verglas** : le sol gèle autour, les ennemis glissent et tombent ;
  - **Niveau 5, le Cœur de l'hiver** : de temps en temps, une tempête change les ennemis de la zone en statues de glace.

  Chaque niveau : un objet vide (`Maj+A` > *Empty* > *Plain Axes*) posé à l'origine (0, 0, 0), **nommé `Niveau 2`, `Niveau 3`,
  `Niveau 4`, `Niveau 5`**, et les pièces de ce niveau rattachées à lui (`Ctrl+P` > *Object*). Une pièce rattachée à `Niveau 3`
  n'apparaît qu'à partir du niveau 3. Tout le reste (la tour du niveau 1) n'est rattaché à rien.

### 3.2 Le fichier

1. *File > New > General*. Supprime le cube, la caméra et la lumière : `A`, puis `X` > *Delete*.
2. *File > Save As* : crée le dossier `Project Archery/Modeles_Blender` (à côté d'`Assets`, pas dedans) et enregistre
   `Givre.blend`.

### 3.3 Les images de référence

Elles sont dans `Project Archery/Docs/Planches/Givre/` : `Givre_avant.png`, `Givre_droite.png`, `Givre_dessus.png` (mon Givre au
niveau 5, à l'échelle exacte : un carreau = 1 m), et `Givre_niveaux.png` (chaque niveau, en biais et de face : à regarder, pas à
mettre dans Blender).

Pour chacune des trois :
1. Passe dans sa vue : *View > Viewpoint > Front* pour `Givre_avant.png`, *Right* pour `Givre_droite.png`, *Top* pour
   `Givre_dessus.png`.
2. *Add > Image > Background*, choisis l'image.
3. Dans ses réglages (le panneau des propriétés, en bas à droite) : onglet *Object* (le carré orange), *Location* et *Rotation* ;
   onglet *Object Data* (l'icône d'image), *Size* :

| Image | Vue | Location | Rotation | Size |
|---|---|---|---|---|
| `Givre_avant.png` | Front | 0, 0, 5 | 90, 0, 0 | 10 |
| `Givre_droite.png` | Right | 0, 0, 5 | 90, 0, 90 | 10 |
| `Givre_dessus.png` | Top | 0, 0, 0 | 0, 0, 0 | 10 |

4. **Vérifie** : les carreaux de l'image tombent sur ceux de la grille de Blender, et la ligne bleue (ou le croisement rouge et
   vert, vu de dessus) passe par le centre de la grille. Sinon, change *Size* jusqu'à ce que ce soit le cas, et dis-le à Claude.
5. Range les trois images dans une collection `Reference` (`M` > *+ New Collection*). Elles ne se voient que dans ces trois
   vues, pas en perspective : c'est normal.

### 3.4 Les pièces de Kenney

1. Pour choisir : ouvre `Project Archery/Assets/kenney_tower-defense-kit/Overview.html` dans ton navigateur (toutes les pièces
   du kit, avec leur nom). Les autres kits du projet : `kenney_castle-kit`, `kenney_fantasy-town-kit`, `kenney_nature-kit`
   (`Preview.png` dans chaque dossier).
2. *File > Import > FBX*, puis `Project Archery/Assets/kenney_tower-defense-kit/Models/FBX format/` et la pièce voulue. Une
   pièce de tour de Kenney arrive à 1 m de large (vérifie *Dimensions* dans le panneau `N`) ; dans mon Givre, elles sont agrandies
   trois fois.
3. En *Material Preview*, elle a ses couleurs. Si elle est rose ou blanche, sa texture n'a pas été trouvée : dis-le à Claude.
4. Après l'avoir mise à l'échelle : `Ctrl+A` > *Rotation & Scale*.

### 3.5 Assembler

Avec tes pièces de Kenney et tes formes, construis le niveau 1, puis les pièces des niveaux 2 à 5 (3.1). Quelques conseils :
- **Le bas de la tour sur le sol** (Z = 0), **centré** sur l'origine de la scène.
- **L'avant** : ce que tu vois dans la vue *Front* sera l'avant du bâtiment dans le jeu.
- **Peu de pièces** : quelques dizaines suffisent ; une grosse pièce de pack porte plus de détail que dix cubes.
- **Les couleurs** : un matériau par couleur, partagé entre les pièces. Pour en créer un : onglet *Material* (la sphère rouge
  en bas des propriétés) > *New*, nomme-le (`Glace`, `Pierre`…), règle sa *Base Color* ; pour le donner à une autre pièce, le
  choisir dans la liste du même onglet. Ce qui doit luire dans le jeu : un matériau dont le nom commence par `Lueur`
  (`Lueur glace`…). Les pièces de Kenney gardent le leur.
- Enregistre souvent (`Ctrl+S`).

### 3.6 Avant d'exporter

- [ ] Les cristaux s'appellent `tower-round-crystals`, leur origine à leur base.
- [ ] Les objets vides `Niveau 2` à `Niveau 5` sont à (0, 0, 0), avec leurs pièces rattachées.
- [ ] Tout sélectionné (`A`, sans les images de référence : cache la collection `Reference` avec l'œil d'abord) :
  `Ctrl+A` > *Rotation & Scale*.
- [ ] La collection `Reference` est cachée (l'œil).

## 4. Exporter vers Unity

1. *File > Export > FBX (.fbx)*. Le dossier : `Project Archery/Assets/_Project/Art/Modeles/` (à créer), le nom : `Givre.fbx`.
2. Les réglages, dans le panneau de droite :
   - *Include* : *Limit to* **Visible Objects** ; *Object Types* : **Empty** et **Mesh** seulement (Maj + clic pour en
     choisir deux).
   - *Transform* : *Scale* `1.00`, *Apply Scalings* **FBX All**, *Forward* **-Z Forward**, *Up* **Y Up**, *Apply Unit* coché,
     *Use Space Transform* coché, **Apply Transform coché**.
   - *Geometry* : *Smoothing* **Face**, *Apply Modifiers* coché.
   - *Bake Animation* : **décoché**.
3. Tout en haut du panneau, *Operator Presets* > `+` : enregistre ces réglages sous le nom `THUNK Unity` (la prochaine fois, un
   clic).
4. *Export FBX*.

Claude vérifie ces réglages à ton premier export (l'orientation surtout) et corrige ce guide s'il le faut.

## 5. Dans Unity

1. Dis à Claude que le Givre est exporté. La première fois, il écrit l'outil qui branche ton modèle sur le prefab (un menu
   d'Unity) et vérifie le résultat avec toi.
2. Ensuite, à chaque changement : dans Blender, réexporte (même nom, même dossier, le préréglage `THUNK Unity`) ; dans Unity,
   relance le menu.
3. Teste : **F4** (la vitrine : le Givre à plusieurs niveaux), puis une partie ; demande à Claude de lire le journal.

## 6. En cas de problème

- **Je ne vois pas mes images de référence** : elles ne s'affichent que dans les vues *Front*, *Right* et *Top*, pas en
  perspective ; et la collection `Reference` doit être visible (l'œil).
- **Les carreaux de l'image ne tombent pas sur la grille** : change *Size* (onglet *Object Data* de l'image).
- **Une pièce importée est minuscule ou énorme** : regarde *Dimensions* (panneau `N`) ; si elle est 100 fois trop petite ou trop
  grande, réimporte-la avec *Scale* `100` ou `0.01` dans les réglages de l'import, et dis-le à Claude.
- **Une pièce tourne ou grandit de travers dans le jeu** : son origine n'est pas au bon endroit (2.5), ou son échelle n'a pas
  été appliquée (`Ctrl+A` > *Rotation & Scale*).
- **J'ai tout cassé** : `Ctrl+Z`, ou *File > Revert* (revient au dernier enregistrement).
- **La barre d'espace lance une animation** : appuie de nouveau dessus (ou Échap).
