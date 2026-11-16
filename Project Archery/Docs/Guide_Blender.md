        # Guide : faire un bâtiment dans Blender (premier exercice : le Givre)

Ce guide est pour quelqu'un qui n'a jamais ouvert Blender. Il sert à la refonte visuelle (`Refonte_Visuelle.md`) : tu
assembles toi-même chaque bâtiment avec des formes simples et des pièces de packs faits par des humains ; tout ce qui entoure
ce travail est automatique (le fichier de départ, la bibliothèque des pièces, la vérification, l'export, le branchement dans
Unity). **Ton Blender est en français** : les menus sont donnés avec leurs noms français (tels qu'ils s'affichent dans
Blender 5.3), et le nom anglais entre parenthèses quand il aide à chercher un tutoriel.

**Ton Givre n'a pas à ressembler au mien.** Les images de référence et mon modèle donnent la taille et l'idée ; la forme, les
pièces, les couleurs, c'est toi qui décides. Claude ne te dira pas où poser une pièce : sinon le modèle redeviendrait le sien.
Et mon modèle ne se retouche jamais pour être livré : il est verrouillé, et l'export ne le prend pas.

Compte une soirée pour les parties 1 et 2 (installer, apprendre les gestes), puis une ou deux pour le Givre.

## 1. Installer

1. **Blender** : sur blender.org, *Download*, la dernière version stable (Blender 5.3 est installé sur ce PC depuis le
   14 novembre).
2. **L'extension THUNK et la bibliothèque des pièces** : **installées le 14 novembre** par Claude (`Tools/blender/installer.py`,
   Blender fermé). Elles ajoutent, dans les préférences de Blender, le dossier `Tools/blender` (l'extension suit le dépôt : quand
   Claude la change, il suffit de redémarrer Blender), l'extension « THUNK VR » cochée, et la bibliothèque `THUNK Kits`. Pour
   les refaire (autre PC) : fermer Blender, puis depuis un terminal ouvert dans `Project Archery` :
   `"C:\Program Files\Blender Foundation\Blender 5.3\blender.exe" --background --python Tools/blender/installer.py`
   Pour les défaire : *Édition > Préférences*, *Add-ons* (décocher THUNK VR), *Chemins de fichiers* (retirer les deux chemins).
3. Pas de pavé numérique (portable) ? Ce n'est pas grave : ce guide passe par les menus pour changer de vue.
4. Pour aller plus loin, le manuel de Blender existe en français : docs.blender.org (choisir la langue en haut).

## 2. Les gestes de base

Ouvre un fichier vide (*Fichier > Nouveau > Général*). Le cube du milieu sert à t'entraîner.

### 2.1 Regarder

| Geste | Pour |
|---|---|
| Molette enfoncée + glisser | tourner autour |
| Maj + molette enfoncée + glisser | se déplacer |
| Molette | zoomer |
| *Vue > Voir la sélection* (pavé `.`) | recentrer sur ce qui est sélectionné |
| *Vue > Point de vue > Face* / *Droite* / *Dessus* (pavé 1, 3, 7) | les vues de face, de droite, de dessus (en orthographique : sans perspective, idéal pour poser les pièces) |

En haut à droite de la vue, les quatre petites sphères changent l'affichage : la troisième, **Prévisualisation du matériau**
(*Material Preview*), montre les couleurs et les textures des pièces (les fichiers préparés s'ouvrent déjà ainsi).

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

**Le panneau des valeurs** : `N` ouvre un panneau à droite de la vue 3D ; son onglet **Élément** donne la *Position*, la
*Rotation*, l'*Échelle* et les *Dimensions* (la taille réelle, en mètres) de la pièce sélectionnée. On peut y taper les valeurs.
L'onglet **THUNK**, dans le même panneau, est celui de l'extension (partie 4).

### 2.3 Ajouter, copier, renommer, supprimer

| Touche | Pour |
|---|---|
| `Maj+A` > *Maillage* | ajouter une forme : *Cube*, *Cylindre*, *Cône*, *Sphère UV*, *Icosphère*, *Tore* |
| `Maj+D` | dupliquer (la copie suit la souris ; clic pour la poser) |
| `F2` | renommer la pièce active |
| `X` > *Supprimer* | supprimer |

**Juste après avoir ajouté une forme**, un petit panneau s'ouvre en bas à gauche de la vue (*Ajouter un cylindre*…) : on y règle
ses côtés (*Sommets*), son rayon, sa hauteur. **La règle : 12 côtés partout** (choisie le 14 novembre : du low poly simple,
avec la même rondeur que les pièces rondes de Kenney, qui ont 12 côtés) : 12 *Sommets* pour un cylindre ou un cône (32 par
défaut), une *Sphère UV* de 12 *Segments* et 6 *Anneaux*, ou une *Icosphère* à 1 *Subdivision*. Le panneau disparaît à l'action
suivante. Un cylindre de 12 côtés fait 44 triangles : le casque n'y voit rien.

**Ce qui bouge** (une tourelle qui tourne, un yo-yo, un bras) : tu le fais immobile, et le code le fera bouger. Il suffit de le
rattacher au bon repère (partie 3).

### 2.4 Retoucher une pièce (le mode édition)

1. Sélectionne la pièce, `Tab` : le **Mode édition** (ses sommets apparaissent).
2. En haut à gauche de la vue, trois petites icônes : sommets, arêtes, faces. Choisis *faces* (la troisième).
3. Clic sur une face (Maj + clic pour plusieurs), puis `G`, `S` ou `R` comme avant ; `E` tire la face pour en faire une
   nouvelle partie (*Extruder*) ; `Ctrl+B` coupe une arête en biseau (*Biseau*, la molette règle le nombre de pans).
4. `Tab` pour revenir au **Mode objet**.

Ça marche aussi sur les pièces de Kenney : raccourcir un mur, élargir un toit, enlever un morceau (`X` > *Faces*).

### 2.5 Ranger

| Touche | Pour |
|---|---|
| `Ctrl+J` | *Joindre* les pièces sélectionnées en une seule (elle prend le nom et l'origine de l'active) |
| `Ctrl+P` > *Objet* | rattacher les pièces sélectionnées à l'active (*Parenter à*, son « enfant » : elles la suivent) |
| `M` | *Déplacer vers la collection* (les collections sont les dossiers de la liste en haut à droite, le **Synoptique** ; l'œil en cache une) |

**L'origine** : le petit point orange d'une pièce. C'est autour de lui qu'elle tourne et grandit. Pour le placer : le **curseur
3D** (le cercle rouge et blanc) se pose par `Maj` + clic droit, ou au bon endroit par le panneau `N`, onglet *Vue*, *Curseur 3D* ;
puis *Objet > Définir l'origine > Origine vers curseur 3D*.

**Enregistrer** : `Ctrl+S`.

**Pour s'entraîner** (20 minutes, à jeter ensuite) : une table et un tabouret, avec des cubes et des cylindres de 12 côtés, à la
bonne taille (une table fait 75 cm de haut), en utilisant `G`, `R`, `S`, le panneau `N`, `Maj+D` et `Ctrl+J`.

### 2.6 La bibliothèque des pièces (l'Explorateur d'assets)

Toutes les pièces des kits du projet (Kenney : Tower Defense, Château, Village, Nature ; Quaternius : RTS et quelques modèles
isolés ; 781 en tout, faites par des humains) sont rangées dans la bibliothèque `THUNK Kits`, avec leur aperçu. Pour l'afficher,
on transforme la fine bande tout en bas de l'écran (la *Timeline*, qui ne sert pas pour nos bâtiments) :
1. **L'agrandir** : approche la souris de la limite entre la vue 3D et cette bande, jusqu'à la double flèche ↕ ; clique et tire
   vers le haut (un tiers de l'écran, environ).
2. **La changer** : tout à gauche de la bande, l'icône avec une petite flèche ▾ (le **Type d'éditeur**) ; dans la colonne
   *Général*, choisis **Explorateur d'assets** (*Asset Browser*).
3. **Choisir la bibliothèque** : en haut à gauche de l'Explorateur d'assets, la liste qui affiche *Toutes les bibliothèques* ;
   choisis **THUNK Kits**. À gauche, les kits ; au milieu, les aperçus (`Ctrl` + molette pour les grossir).
4. **Glisse** une pièce dans la vue 3D : elle arrive sous la souris, dans la collection `Modele`, avec ses couleurs ; ensuite, `G`
   pour la déplacer.

Pour revenir comme avant : même menu, *Timeline*. Blender garde cette disposition dans le fichier : à la prochaine ouverture,
la bibliothèque est là.

**Une pièce glissée est une vraie copie, que tu peux modifier** (`Tab`) **et joindre** (`Ctrl+J`) : la bibliothèque est réglée en
*Méthode d'importation* **Ajouter** (l'extension le fait à l'ouverture d'un fichier préparé ; Blender 5 « empaquette » par
défaut, et une pièce empaquetée ne se modifie pas). Si une pièce refuse quand même (« est empaqueté et ne peut pas être
édité », « Impossible d'éditer les données de bibliothèque externe ») : onglet THUNK, bouton **Rendre les pièces modifiables**.
Chaque pièce glissée apporte une copie de la couleur de son kit (« Kenney TD.001 ») : le même bouton, et l'export, les rangent
dans l'originale.

Chaque pièce garde son auteur et sa licence : l'extension en fait la liste (les crédits). **Tes propres pièces** (un cristal que
tu réutilises, une poutre, tes couleurs) : enregistre-les dans `Modeles_Blender/Bibliotheque/Mes_pieces.blend`, puis clic droit
sur chacune dans le Synoptique > *Définir comme asset* ; elles apparaissent dans la même bibliothèque.

## 3. Le Givre

### 3.1 Le fichier, déjà prêt

Ouvre **`Project Archery/Modeles_Blender/Givre.blend`** (*Fichier > Ouvrir*, préparé par Claude). Dans le Synoptique :
- **`Reference`** :
  - `Images de reference` : mon Givre de face, de droite et de dessus, à l'échelle (un carreau = 1 m). Elles ne se voient que
    dans les vues *Face*, *Droite* et *Dessus* (partie 2.1).
  - `Modele de Claude` : mon Givre en 3D, **niveau par niveau** (`Claude - niveau 1`, puis ce qu'ajoutent les niveaux 3, 4,
    5 ; l'œil de chaque collection l'affiche ou le cache). Il est verrouillé (on ne peut pas le cliquer) : il sert à voir et à
    s'inspirer. L'onglet THUNK le pose **à côté** du tien pour comparer, ou le remet au centre.
- **`Modele`** : c'est **là que tu travailles**, et c'est la seule collection qui part dans le jeu. Elle contient déjà **les
  repères du code**, des objets vides (des flèches) nommés : `tower-round-crystals`, `Glow`, `Niveau 2` à `Niveau 5`.
- **Le cahier des charges** : change une zone en **Éditeur de texte** (même menu *Type d'éditeur* qu'en 2.6) et choisis le texte
  `Cahier des charges` : ce que fait chaque niveau (le texte du jeu), les repères et ce que le code en fait, la taille de mon
  Givre.

### 3.2 Ce que le jeu attend du Givre (le cahier des charges, pas la forme)

- **Une tour de glace** : elle ralentit les ennemis dans sa zone (9 m autour d'elle, volants compris), et ce qu'elle lance (ses
  rayons de glace, ses boules de glace dans l'arène de MK) part de ses cristaux.
- **La taille au sol** : au niveau 1, sa largeur décide du nombre de cases qu'elle prend sur la grille de pose (1 case = 1 m,
  arrondi au-dessus). Le mien fait 2,4 × 2,4 m au sol, donc 3 × 3 cases ; l'onglet THUNK compte les tiennes. Les niveaux 2 à 5
  peuvent déborder (mon verglas fait 3 m).
- **La hauteur** : la mienne fait 4,6 m au niveau 1, 8,5 m au niveau 5 (avec le cœur qui flotte au-dessus).
- **Les cristaux** : le repère `tower-round-crystals`. **Rattache-lui tes cristaux** (sélectionne tes pièces, puis le repère en
  dernier, `Ctrl+P` > *Objet*). Le code agrandit le repère à chaque niveau (×1, ×1,08, ×1,16, ×1,24, ×1,35) autour de lui, et la
  boule de glace part 60 cm au-dessus : **pose le repère à la base de tes cristaux** (`G`), au centre. Chez moi, il est à
  2,63 m de haut.
- **La lumière bleue** : le repère `Glow`. Ce n'est **pas un niveau** : c'est la place d'une lampe invisible qui éclaire la tour
  en bleu (le code règle sa force à chaque niveau). Rien à y rattacher ; déplace-le si ta tour est bien plus haute ou plus basse.
- **Les niveaux** (ce que chacun fait en jeu ; à toi de lui donner une forme qui le raconte) :
  - **Niveau 2** : le givre reste 2 s sur les ennemis qui sortent de la zone (le mien n'ajoute rien de visible : libre à toi) ;
  - **Niveau 3, le Pic de glace** : de temps en temps, elle gèle net un ennemi, et ta flèche brise la glace ;
  - **Niveau 4, le Verglas** : le sol gèle autour, les ennemis glissent et tombent ;
  - **Niveau 5, le Cœur de l'hiver** : de temps en temps, une tempête change les ennemis de la zone en statues de glace.

  **Mettre une pièce à un niveau** : sélectionne-la (ou plusieurs), puis dans l'onglet THUNK, **Mettre la sélection au
  niveau : 1 2 3 4 5**. Elle ne bouge pas, et n'apparaîtra qu'à partir de ce niveau ; l'onglet affiche le niveau de la pièce
  active. (`Niveau 3` n'est pas une collection, c'est un repère : la pièce y est « rattachée », elle apparaît en dessous de
  lui dans le Synoptique, avec une petite flèche. À la main : onglet *Objet* des *Propriétés* (le carré orange) > *Relations* >
  *Parent* : choisir `Niveau 3`.) **Pour voir la tour sans un niveau** : un clic sur l'œil de `Niveau 3` dans le Synoptique
  cache aussi toutes ses pièces (l'extension le fait ; marche pour tous les repères). **Il n'y a pas de repère « Niveau 1 »** : le niveau 1, c'est la tour de base, toujours là ; ses pièces ne sont rattachées à aucun repère
  `Niveau`, elles sont simplement dans la collection `Modele` (ou rattachées à une autre de tes pièces, ou aux cristaux). Chez
  moi, c'est `Claude - niveau 1`, dans `Reference > Modele de Claude`.
- **Ne renomme pas et ne supprime pas les repères** : c'est par leur nom que le jeu les retrouve. L'onglet THUNK le vérifie.

### 3.3 Assembler

Avec les pièces de la bibliothèque (2.6) et tes formes (2.3), construis le niveau 1, puis les pièces des niveaux 2 à 5 :
- **Le bas de la tour sur le sol** (Z = 0), **centré** sur l'origine de la scène.
- **L'avant** : ce que tu vois dans la vue *Face* sera l'avant du bâtiment dans le jeu.
- **Peu de pièces** : quelques dizaines suffisent ; une grosse pièce de pack porte plus de détail que dix cubes.
- **Les couleurs : la palette** (le choix du 14 novembre). Tu ne règles aucune couleur à la main : chaque couleur du jeu est
  un **rôle** (`Pierre`, `Bois`, `Or`, `Transparent glace`…) de **ta palette**, faite une fois pour toutes avec les couleurs du
  kit Kenney (`Modeles_Blender/Palette.txt`, la planche `Docs/Planches/Palette_td.png`). Pour colorer une pièce, deux façons :
  - **la glisser** : dans l'Explorateur d'assets, bibliothèque `THUNK Kits`, catalogue **Palette**, glisse le rôle sur la pièce ;
  - **la nommer** : l'onglet **Matériau** (la sphère rouge, en bas de la colonne d'icônes des *Propriétés*), le nom du matériau
    = le nom du rôle (`Pierre`…).

  La couleur vient de la palette, toute seule (à l'ouverture du fichier, à *Vérifier*, à l'export) ; **le même nom donne la
  même couleur dans tous les bâtiments du jeu**. Tu peux aussi dire à Claude « la base en pierre, le cristal en glace » : il
  nomme. Deux mots dans un nom de rôle changent le rendu :
  - **`Lueur`** (n'importe où dans le nom : `Lueur cristal`…) : la couleur luit ;
  - **`Transparent`** (au début du nom : `Transparent glace`…) : la couleur est transparente, comme la glace (sa transparence se
    règle dans la palette). À garder pour quelques pièces : dans un casque, la transparence coûte plus cher que l'opaque.

  Les pièces de la bibliothèque des kits gardent leur couleur (celle de Kenney). Une couleur hors de la palette est signalée
  par *Vérifier*.
- **Recolorer une pièce de Kenney** : une pièce de kit n'a pas une seule couleur (chaque face prend la sienne dans l'image de
  couleurs du kit). Sélectionne-la (ou plusieurs), onglet THUNK, case **Couleurs des pièces Kenney** :
  1. **Lire les couleurs Kenney de la sélection** : ses couleurs apparaissent, chacune avec son nombre de faces (la tour du Givre
     en a 3 : son rouge et deux gris ; coche **Séparer les nuances** pour en avoir plus, 10 sur cette tour, comme le
     rouge-orangé à part du rouge). Une face (un carré, fait de deux triangles ou plus) change toujours de couleur d'un bloc ;
  2. sur chaque ligne : la couleur d'origine, une flèche, puis **ta nouvelle couleur**, qui s'applique **tout de suite** (pas
     de bouton) :
     - **la pastille** : un clic ouvre une grille de carrés de couleur (le nom au survol) : la couleur d'origine (garder), tes
       pots de palette, les 57 couleurs du kit, et *Couleur libre* ;
     - **la roue** (le carré à droite de la pastille) : n'importe quelle couleur, avec la roue de Blender ;
     - les deux interrupteurs : **transparente** (le damier) et **lumineuse** (le soleil) ;
     - le nombre de faces de cette couleur.

  Un pot de ta palette suit la palette si tu la changes ; une couleur du kit ou de la roue reste celle que tu as choisie (le
  même code de couleur donne la même couleur dans tous les bâtiments). **Remettre les couleurs Kenney** rend aux faces leur
  couleur d'origine (et `Ctrl+Z` marche aussi). Relire les couleurs d'une pièce recolorée montre ses couleurs d'origine, et ce
  que tu leur as donné.
- **Le pinceau** (15 novembre) : peindre une face d'un clic, sur tes formes comme sur les pièces de kit. Onglet THUNK, case
  **Pinceau** :
  1. choisis la couleur : la **pastille** (la même grille : tes pots, les couleurs du kit, *Couleur libre* ; la première case,
     la **gomme**, rend à une face sa couleur d'avant le pinceau), la **roue**, et les interrupteurs **transparente** et
     **lumineuse** ;
  2. **Peindre : clic sur une face** : le curseur devient un pinceau ; chaque clic gauche sur une face de tes pièces la peint.
     Tu peux tourner la vue (clic molette) et changer de couleur dans le panneau sans arrêter. **Clic droit**, **Échap** ou le
     bouton (**Arrêter le pinceau**) : fini. `Ctrl+Z` défait un clic.

  Une forme sans couleur (un cube neuf) prend la couleur partout au premier clic ; ensuite, chaque clic peint une face. Une
  face d'une pièce de kit se peint d'un bloc (tous ses triangles). Le pinceau marche en *Mode objet* et ne touche que les
  pièces de `Modele` (il traverse le modèle de Claude).
- **Une pièce qui disparaît à un niveau** (15 novembre) : par défaut, une pièce reste jusqu'au dernier niveau. Pour qu'elle
  laisse la place à une autre (la base d'une couleur par niveau : des copies, `Maj+D`, une par niveau), sélectionne-la et
  clique, dans **La sélection disparaît au niveau**, sur le niveau où elle s'en va (**Jamais** : elle reste). Exemple : la base
  du niveau 1 disparaît au niveau 2 ; sa copie, mise au niveau 2 (**Mettre la sélection au niveau**), disparaît au 3, etc.
  **Voir comme dans le jeu au niveau 1, 2, 3…** ne montre que les pièces présentes à ce niveau (pour travailler sur une copie
  sans que l'autre la cache) ; **Tout** remontre tout. Tout part à l'export, caché ou non. Un repère du code rattaché à une
  pièce qui disparaît (`Ctrl+P`) disparaît avec elle : la bouche d'un canon remplacé à un niveau ne tire plus.
- Enregistre souvent (`Ctrl+S`).

## 4. Vérifier et exporter (l'onglet THUNK)

Dans la vue 3D, `N`, onglet **THUNK** :
- **Vérifier** : les repères (présents, pas renommés, une pièce rattachée à ceux qui en avaient une), le sol, les cases au sol
  et la hauteur comparées à mon Givre, le nombre de triangles, les couleurs, les pièces des kits utilisées (et celles à
  créditer). Une ligne rouge bloque l'export ; une ligne d'attention, non.
- **Exporter vers Unity** : vérifie, puis exporte la collection `Modele` (et elle seule) dans
  `Assets/_Project/Art/Modeles/Givre.fbx`, avec les bons réglages (vérifiés le 14 novembre par un aller-retour exact).
- **Exporter à chaque Ctrl+S** : à cocher quand tout marche ; chaque enregistrement exporte (si la vérification passe).
- **Mettre la sélection au niveau 1, 2, 3…** : le niveau des pièces sélectionnées (3.2), et celui de la pièce active.
- **La sélection disparaît au niveau** et **Voir comme dans le jeu au niveau** : les pièces qui s'en vont à un niveau (3.3).
- **Pinceau** : peindre une face d'un clic (3.3).
- **Rendre les pièces modifiables** (seulement s'il y en a) : les pièces glissées en mode *Empaqueter* (2.6).
- **Ranger les pièces dans Modele** (seulement s'il y en a) : les pièces posées hors de `Modele`, qui ne partiraient pas.
- **Modèle de Claude** : à côté / au centre, voir / cacher.
- **Temps de travail** : les minutes où tu as fait quelque chose dans ce fichier ; Claude s'en sert pour recalculer
  l'estimation de la refonte.

## 5. Dans Unity

1. **Clique dans la fenêtre d'Unity** : il importe le modèle et le **branche tout seul** sur le prefab du Givre (ses pièces à
   la place des miennes, les repères du code à la place des tiens, les couleurs, la marque « Fait main »). La console dit
   ce qu'il a fait (« Modèles faits main : Givre branché… »).
2. À chaque changement : dans Blender, *Exporter vers Unity* (ou `Ctrl+S` si l'export automatique est coché) ; dans Unity, un
   clic dans la fenêtre.
3. Teste : **F4** (la vitrine : le Givre à plusieurs niveaux), puis une partie ; demande à Claude de lire le journal.
4. Pour refaire le branchement à la main : *Tools > THUNK > Brancher les modèles faits main* (Unity est en anglais).

**Les autres bâtiments** : demande à Claude de préparer leur fichier (`Tools/refonte_visuelle/preparer.py` : les images, le
modèle, les repères, le cahier des charges). Il ne remplace jamais un fichier où tu as travaillé.

## 6. En cas de problème

- **Pas d'onglet THUNK** : l'extension n'est pas cochée (*Édition > Préférences > Add-ons*, « THUNK VR ») ; refaire la
  partie 1.2.
- **Pas de bibliothèque THUNK Kits** : *Édition > Préférences > Chemins de fichiers > Bibliothèques d'assets* : le dossier
  `Project Archery/Modeles_Blender/Bibliotheque` doit y être ; il s'ajoute tout seul à l'ouverture d'un fichier préparé.
- **Je ne vois pas les images de référence** : elles ne s'affichent que dans les vues *Face*, *Droite* et *Dessus*, pas en
  perspective ; et la collection `Reference` doit être visible (l'œil, dans le Synoptique).
- **Une pièce glissée n'est pas exportée** : elle est hors de la collection `Modele` (une pièce va dans la collection
  *active*, celle qu'on a cliquée en dernier dans le Synoptique ; l'extension remet `Modele` active à l'ouverture du fichier) ;
  onglet THUNK, **Ranger les pièces dans Modele** (le bouton n'apparaît que s'il y en a), ou `M` > `Modele`. Les boutons des
  niveaux les rangent aussi d'eux-mêmes.
- **Je n'arrive pas à mettre une pièce au niveau 2** : `Niveau 2` est un repère, pas une collection (`M` ne le propose pas) ;
  sélectionne la pièce, puis onglet THUNK, bouton **2** (3.2).
- **L'onglet THUNK n'a pas les nouveaux boutons** (après un changement de l'extension par Claude) : redémarre Blender, ou
  `F3` (la recherche) > *Recharger les scripts*.
- **« Le bloc de données … est empaqueté et ne peut pas être édité »** (`Tab`) ou **« Impossible d'éditer les données de
  bibliothèque externe »** (`Ctrl+J`) : la pièce a été glissée en mode *Empaqueter* ; onglet THUNK, **Rendre les pièces
  modifiables**. Pour que les suivantes arrivent modifiables : *Édition > Préférences > Chemins de fichiers > Bibliothèques
  d'assets*, `THUNK Kits`, *Méthode d'importation* : **Ajouter** (l'extension le règle à l'ouverture d'un fichier préparé).
- **Une pièce tourne ou grandit de travers dans le jeu** : elle n'est pas rattachée au bon repère, ou le repère n'est pas à la
  bonne place (le pivot).
- **Unity n'a rien fait** : regarde la console ; le `.json` doit être à côté du `.fbx` dans `Art/Modeles` (réexporter depuis
  l'onglet THUNK) ; puis *Tools > THUNK > Brancher les modèles faits main*.
- **J'ai tout cassé** : `Ctrl+Z`, ou *Fichier > Revenir* (revient au dernier enregistrement). Blender garde aussi la version
  d'avant de chaque enregistrement (`Givre.blend1` : la renommer en `.blend` pour l'ouvrir).
- **La barre d'espace lance une animation** : appuie de nouveau dessus (ou Échap).

## Annexe : l'export à la main (si l'extension ne marche pas)

*Fichier > Exporter > FBX (.fbx)*, dans `Assets/_Project/Art/Modeles/Givre.fbx`, seulement la collection `Modele`
sélectionnée : *Limiter à* **Objets sélectionnés** ; *Types d'objet* **Vide** et **Maillage** ; *Échelle* `1.00`, *Appliquer les
échelles* **FBX All**, *Vers l'avant* **-Z Forward**, *Haut* **Y Up**, *Appliquer l'unité* et *Utiliser transformations
d'espace* cochés, **Appliquer la transformation** coché ; *Lissage* **Face** ; *Bake Animation* décoché. Sans le `.json` de
l'extension, Unity ne le branchera pas tout seul : demander à Claude.
