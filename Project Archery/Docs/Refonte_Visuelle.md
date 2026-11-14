# Refonte visuelle : tout refaire à la main (commencée le 14 novembre)

**Le but** (le joueur, 14 novembre) : que THUNK VR n'ait pas la mention d'IA sur Steam, par fierté personnelle. Tout élément
visuel fait par Claude est refait par le joueur, **depuis zéro**, en prenant les modèles de Claude comme référence. Ce
document dit ce que Steam demande (partie 1), ce qui est déjà fait par des humains (partie 2), l'inventaire de ce qui est à
refaire, avec son avancement (partie 3), et la méthode, bâtiment par bâtiment (partie 4).

## 1. Ce que Steam demande (page officielle, lue le 14 novembre 2026)

La page : <https://partner.steamgames.com/doc/gettingstarted/contentsurvey> (le questionnaire de contenu de Steamworks, réécrit
par Valve en janvier 2026). Valve demande de déclarer le contenu **pré-généré** : tout contenu
« that ships with your game and is consumed by players », fait avec l'aide d'outils d'IA pendant le développement. Ses exemples :
les graphismes, le son, la narration, la traduction. Le gain d'efficacité (les outils de développement) n'est pas visé, et **le
code n'est pas cité**.

Ce que ça veut dire pour THUNK VR :
- **Les visuels faits par Claude** : la partie 3.
- **Les sons faits par Claude** comptent aussi : les musiques des vagues, du boss et du choix de la pause et les deux ambiances
  (`Audio/Music/music_wave.wav`, `music_boss.wav`, `music_shop.wav`, `ambience_day.wav`, `ambience_night.wav`, « générées par
  script »), les 46 sons provisoires (`Audio/Placeholder/`), le cor de la tour (`Audio/Tour_Cor.wav`) et l'arrêt du temps
  (`Audio/Horloge_Temps_Arrete.wav`), synthétisés par script. Les sons de freesound sont des enregistrements faits par des
  humains, que Claude a seulement coupés, fondus et mis au même volume : un montage, le cas le moins net, à juger par le joueur.
- **Les textes** comptent aussi (« narrative, localization ») : les cartes, les pièces, le tutoriel, les menus, le lore ;
  beaucoup sont des propositions de Claude.
- **La page Steam** (capsules, captures, bande-annonce) : à faire soi-même aussi ; certains guides la comptent dans la
  déclaration.
- **Donc** : tant qu'il reste dans le jeu des sons ou des textes faits par Claude, la règle de Valve demande de cocher la case,
  même avec tous les visuels refaits. Le code (le jeu, les bakers, les outils) n'est pas concerné.
- **Les modèles de Claude comme base** : Valve ne parle que de ce qui est livré. Un modèle refait depuis zéro, en regardant la
  référence (les planches en fond, partie 4), est fait par le joueur. Importer un maillage de Claude pour le retoucher donnerait un
  modèle livré qui en dérive : **à éviter**. Valve ne dit rien de la conception qui a servi de modèle ; s'en éloigner librement
  rend le modèle encore plus à soi.

## 2. Ce qui n'est pas à refaire : fait par des humains

Tout ce qui vient d'un pack (détail et licences : `Credits_Licences.md`) :
- **Les ennemis et les boss** (Dungeon Mason, le chevalier de Toon RTS), **MK** (Four Evil Dragons), les mains (Quaternius),
  la porte de l'antre, les coffres, la grenade, le katana, le marteau de construction, l'arc et la flèche de base (Dimasjk),
  les modèles des arcs légendaire et cosmique, les arbalètes du toit cyberpunk.
- **Les kits de Kenney** : la carte, les remparts, les pièces de kit dans les tours, les cadres des menus. Les polices.
- **La carte elle-même** : posée à la main par le joueur (2 391 pièces du Nature Kit dans la scène), d'après le plan du guide
  (`Guide_Carte.md`).
- **Le visage des mannequins** (dessiné par le joueur sur Paint, le 2 novembre).
- **L'arène de MK** (réponse du joueur, 14 novembre) : dessinée d'après ses schémas (le plan en lettres,
  `Data/Arena/Arena_Fourche.txt`), faite de blocs que le code pose selon ce plan et de pièces du Nature Kit (la tête de statue,
  les colonnes, le feu de camp, les socles des tourelles). Sa conception est celle du joueur, les pièces sont faites par des
  humains : le code ne fait qu'exécuter le plan. **Sauf les fissures du sol**, dessinées par un script de Claude (confirmé par
  le joueur, 14 novembre) : à refaire (lot 5).
- **Les formes de base d'Unity seules** (les orbes des coffres et des tourelles : une sphère et une couleur ; les projectiles) :
  une sphère d'Unity n'est pas un dessin de l'IA.
- **Les effets** : gardés, choix du joueur du 14 novembre (lot 6).

**La règle de pouce** (l'avis de Claude, pas celui de Valve) : compte comme fait par l'IA une forme, une image ou un assemblage
**dont Claude a décidé l'allure** (une tour de 80 formes, une icône dessinée par script, un maillage construit par un baker).
Ne compte pas : une pièce faite par un humain, une forme de base d'Unity seule, du code qui exécute le plan du joueur.

À confirmer par le joueur : les pièces retouchées du Nature Kit (`Art/Diagonal Cuts`, `Art/River`, `Art/Stair Borders`, 11 et
12 octobre) ; les as-tu coupées toi-même, ou un script de Claude ?

## 3. L'inventaire : ce que Claude a fait (à refaire)

Compté le 14 novembre dans les prefabs et le code (« formes » : les cubes, cylindres et sphères d'Unity que Claude a assemblés ;
« kit » : les pièces de Kenney ou de Quaternius qu'il a placées). La colonne **État** se coche au fil de la refonte :
à faire, en cours, fait, vu au casque.

### Lot 1 : les 10 tours de base (vues tout le temps, 5 niveaux)

Des pièces de kit (faites par des humains) et des formes de Claude. L'assemblage des pièces de kit est aussi de Claude : le plus
simple est de refaire la tour entière (rien n'empêche de reprendre des pièces de kit dans ton modèle, placées par toi).

| Tour | Prefab (`Prefabs/Structures/`) | Formes | Kit | État |
|---|---|---|---|---|
| Barricade | `Barricade` | 33 | 19 | à faire |
| Pièges | `Pieges` | 55 | 1 | à faire |
| Baliste | `Baliste` | 33 | 6 | à faire |
| Mortier | `Mortier` | 2 | 22 | à faire |
| Givre | `Givre` | 12 | 3 | en cours (images de référence et guide prêts, 14 novembre) |
| Filet | `Filet` | 38 | 5 | à faire |
| Aimant | `Aimant` | 74 | 1 | à faire |
| Bulle | `Bulle` | 42 | 4 | à faire |
| Phare | `Phare` | 52 | 6 | à faire |
| Tremplin | `Tremplin` | 38 | 1 | à faire |

Avec elles : **les palettes** (`Art/TowerDefense/Palettes/`, 15 textures de couleurs des kits, recolorées par script pour
chaque tour) ; à repeindre soi-même, ou à remplacer par les couleurs de tes modèles.

Les anciennes structures (`Arbalete`, `BalisteTour`, `Catapulte`, `Esprit`, `Foudre`, `Geyser`, `Goudron`, `LanceFlammes`,
`Mine`, `Miroir`, `Pendule`, `SeauHuile`, `Soufflerie`, `Trappe`, les trois `Anneau…`) ne sont plus proposées par les caisses
depuis la refonte du tower defense : **ne pas les refaire**, les sortir du jeu avant la sortie (Claude vérifiera qu'aucun
niveau d'une tour ne s'en sert encore). De même pour `Prefabs/Barricade.prefab` et `Prefabs/Training Target.prefab`, plus
utilisés.

### Lot 2 : les duos, les trios et les ultimes (tout en formes)

| Bâtiment | Prefab (`Prefabs/Structures/Duos/`) | Formes | État |
|---|---|---|---|
| Boulet fou | `Duo_Boulet_fou` | 56 | à faire |
| Marteau-pilon | `Duo_Marteau_pilon` | 66 | à faire |
| Toboggan | `Duo_Toboggan` | 88 | à faire |
| Yo-yo | `Duo_Yoyo` | 108 | à faire |
| Avalanche | `Duo_Avalanche` | 42 | à faire |
| Feu d'artifice | `Duo_Feu_d_artifice` | 130 | à faire |
| Chant des sirènes | `Duo_Chant_des_sirenes` | 55 | à faire |
| Loupe | `Duo_Loupe` | 56 | à faire |
| Carquois géant | `Duo_Carquois_geant` | 122 | à faire |
| Chaîne de forçats | `Duo_Chaine_de_forcats` | 76 | à faire |
| Boussole folle | `Duo_Boussole_folle` | 121 | à faire |
| Lanceur de baudruches | `Duo_Lanceur_baudruches` | 93 | à faire |
| Boule à neige | `Duo_Boule_a_neige` | 84 | à faire |
| Enclume (trio) | `Trio_Enclume` | 79 | à faire |
| Gonfleur (trio) | `Trio_Gonfleur` | 91 | à faire |
| Bowling (trio) | `Trio_Bowling` | 87 | à faire |
| Grande bombarde (trio) | `Trio_Bombarde` | 96 | à faire |
| Beffroi (trio) | `Trio_Beffroi` | 84 | à faire |
| Colisée (trio) | `Trio_Colisee` | 151 | à faire |
| Montagnes russes (trio) | `Trio_Montagnes_russes` | 219 | à faire |
| Colosse de forge (ultime) | `Ultime_Colosse` | 158 | à faire |
| Grand Manège (ultime) | `Ultime_Manege` | 942 | à faire |
| Grande Horloge (ultime) | `Ultime_Horloge` | 246 | à faire |

Avec eux, **les textures dessinées par script** : `Art/TowerDefense/Colosse_Fer.png` et `Colosse_Fer_Relief.png`,
`Manege_Bois.png` et `Manege_Bois_Relief.png`, `Horloge_Glace.png` et `Horloge_Glace_Relief.png`, `Trou_noir.png` (les scripts
`Tools/texture_*.py`).

### Lot 3 : les icônes et l'interface

- **Les 34 icônes** (`UI/Icones/`, dessinées par script) : les cartes, les pièces de la flèche, les parchemins, les tourelles.
  Rapides à refaire et très visibles. Sans dessiner : les icônes de Kenney (`kenney_game-icons`, CC0, déjà dans le projet) ou
  celles de game-icons.net (des milliers, dessinées par des humains, CC-BY 3.0 : créditer chaque auteur), choisies et recolorées
  par toi.
- La disposition des menus et des cartes (les cadres de Kenney et la police Cinzel sont faits par des humains ; leur agencement
  est de Claude) : le cas le moins net du lot.
- Le panneau des difficultés (2 cubes), le corps des mannequins (4 formes, le visage est déjà de toi).

### Lot 4 : les armes et les objets

- **La batte : à refaire.** Son maillage (`Art/Batte/Batte_Maillage.asset`) a été construit en entier par `Editor/BatteBaker`.
  Le plus simple : une batte d'un pack, ou une batte assemblée par toi (un cylindre effilé, des bagues).
- **Facultatif** (le joueur, 14 novembre : la base est un pack fait par des humains) : les 10 bandes néon des Akimbow
  (`Prefabs/Bow Visual Akimbow.prefab`), quelques minutes pour les replacer toi-même ; les matériaux que Claude a refaits sur des
  modèles de packs (les arcs légendaire et cosmique, la lame néon du katana, la grenade, les coffres), où choisir tes couleurs
  suffit.
- Deux maillages faits par le code, à remplacer par une forme simple ou une pièce de pack : l'éclat du toit cyberpunk
  (`Art/Cyber/Eclat.asset`) et l'anneau des tours (`Art/TowerDefense/Anneau.asset`, s'il sert encore : il était aux anneaux,
  retirés des caisses).

### Lot 5 : les décors construits par le code

Ces décors sont fabriqués par des scripts au lancement ou par des bakers : pour les refaire, tu fais les modèles, et Claude
change le code pour qu'il les pose à la place des siens (une tâche par décor).
- **Les fissures du sol de l'arène** (`Art/Arena/Fourche_Cracks.png`, 2 048 × 2 048 px, des traits blancs sur du noir ; les
  matériaux `Materials/Fourche_Floor.mat`, `Fourche_FloorDark.mat` et `Fourche_FloorScorched.mat` s'en servent) : dessinées par
  script, **à refaire**. Quelques minutes dans un logiciel de dessin (Krita ou GIMP, gratuits) ; l'image se répète sur le sol,
  donc ses bords doivent se raccorder (dans Krita, le mode *Wrap Around*, touche W). Enregistrer par-dessus le même fichier :
  les matériaux suivent. Le reste de l'arène est la conception du joueur
  (partie 2). État : à faire.
- Pour les deux suivants, si le joueur en a fait les schémas comme pour l'arène, ils passent dans la partie 2 (à confirmer).
- **La dimension des étoiles** (`Portal/StarDimension.cs`, `Editor/StarDimensionBaker` : le torii, 4 maillages ; le ciel,
  9 maillages ; le sol, `Art/Dimension/`).
- **Le toit cyberpunk** (`Editor/CyberDimensionBaker` : l'agencement des kits de Kenney et de Quaternius, les fenêtres
  `Art/Cyber/Ville fenetres.png`, la cascade creusée, les hologrammes).
- Le ciel, les nuages, l'eau et le vent dans le feuillage de la carte (des shaders, lot 6), les lumières (`World/Lighting/`).

### Lot 6 : les effets, gardés (le joueur, 14 novembre)

Du code qui dessine : le cas le moins net. Le joueur les garde ; pour mémoire :

- **Les particules** : une vingtaine de scripts en créent au lancement (les ultimes, le katana, la batte, la grenade, les
  explosions, le feu de MK, la dimension, la rivière, les lumières de la carte…), 80 systèmes dans la scène, et les prefabs
  `Prefabs/Effects/`, `Explosion`, `Burn Fire`.
- **Les traînées et les rayons** (une quinzaine de scripts), le plan bleu, la grille de pose, l'anneau de timing.
- **Les formes ajoutées en jeu** par le code des bâtiments (les statues de glace, les boulets, les rayons du Phare, les ballons…).
- **Les 26 shaders** (`_Project/Shaders/`).

### Hors des visuels : les sons (classés le 14 novembre) et les textes

**Les sons**, selon ce que Claude en a fait (l'avis de Claude, partie 5) :
- **Faits par des humains, tels quels ou simplement montés** (coupés, fondus, mis au volume, en mono, rééchantillonnés, bouclés,
  un peu filtrés, remis à l'endroit) : **pas de l'IA** à notre avis. Les sons de Kenney, le geste du katana, la faille, le grand
  impact du Colosse (tels quels) ; `Arc_Cosmique_Charge`, `Colosse_Gresillement`, `Colosse_Tonnerre`, `Colosse_Coulee`,
  `Manege_Orgue`, `Manege_Fanfare` et `Manege_Fanfare_Courte`, `Manege_Galop`, `Manege_Hennissement`, `Manege_Fusee`,
  `Manege_Bouquet`, `Horloge_Tic`, `Horloge_Cloche`, `Horloge_Carillon`, `Horloge_Arret`, `Horloge_Reprise`, `Horloge_Bris`,
  `Horloge_Eclat`, `Batte_Coup`.
- **Transformés par Claude** (des sons d'humains, mais un son nouveau fabriqué par des choix de Claude : forte saturation,
  mélange de plusieurs sons) : **zone grise**, à refaire par le joueur dans Audacity (gratuit, une dizaine de minutes chacun)
  pour zéro doute. `Arc_Cosmique_Laser` (saturé, graves renforcés), `Colosse_Fournaise` (deux sons mêlés), `Batte_Parfait` (deux
  sons mêlés, fortement saturés à la demande du joueur).
- **Synthétisés par Claude** (de l'IA, à remplacer par des sons faits par des humains) : les musiques des vagues, du boss et du
  choix de la pause et les deux ambiances (`Audio/Music/music_wave.wav`, `music_boss.wav`, `music_shop.wav`,
  `ambience_day.wav`, `ambience_night.wav`) ; les 45 sons provisoires encore utilisés (`Audio/Placeholder/`, tous sauf
  `shop_reroll.wav`, plus utilisé) ; le cor de la tour (`Tour_Cor.wav`) ; l'arrêt du temps (`Horloge_Temps_Arrete.wav`).
  Les deux musiques de l'ami (le boss final, la dimension des étoiles) sont faites par un humain.

**Les textes** faits par Claude (les cartes, le tutoriel, les menus, le lore) : à reprendre plus tard. Le joueur a choisi de
commencer par les visuels.

## 4. La méthode (le joueur, 14 novembre) : assembler à la main, dans Blender

Le joueur n'a jamais modélisé : il **assemble lui-même** des **formes simples** et des **pièces de packs faits par des humains**
(Kenney, Quaternius, KayKit), qu'il peut aussi retoucher (couper, étirer, fondre), les tours étant un mélange des deux. C'est la
méthode courante des jeux faits seul (le *kitbashing*), et elle va avec le style du jeu (la carte en Nature Kit, les tours en
kits de Kenney, les ennemis *polyart*).

**L'outil : Blender** (gratuit, blender.org ; choix du joueur, approuvé par Claude le 14 novembre), plutôt que l'éditeur de
prefab d'Unity : on y retouche les pièces des packs, on y règle le nombre de côtés des formes (un cylindre de 8 à 16 côtés, une
sphère légère, au lieu des formes d'Unity, lourdes et sans biseau), et c'est un savoir qui sert pour la suite. Le prix : apprendre
une dizaine de gestes, et un aller-retour Blender > Unity à chaque changement ; Claude écrit le guide pas à pas et l'outil qui
branche le modèle sur le prefab (4.4), pour que l'aller-retour se fasse en un clic.

**Le style et les mouvements** (le joueur, 14 novembre, pour réduire la durée) :
- **Du low poly très simple** pour toutes les tours, des formes franches et peu de faces.
- **Les modèles sont immobiles** : tout ce qui bouge (les tourelles qui tournent, les yo-yos, les bras, les chevaux…) est animé
  par le code de Claude, comme c'est déjà le cas aujourd'hui (aucune tour n'a d'animation faite à la main). Du code, que Valve
  ne vise pas. Le joueur fournit seulement les pièces mobiles **à part**, nommées comme dans la fiche, avec leur origine au
  bon endroit (le pivot) ; Claude adapte le code si une pièce change de forme ou de taille.
- Ce qui donne « de la gueule » aux duos, aux trios et aux ultimes (la règle du 31 octobre : imposant, des accents dorés, un
  niveau 3 dans la démesure) vient alors de la **silhouette, de la taille et des couleurs** du joueur, et des **mouvements, des
  lumières et des effets** du code.

**Le partage des rôles**, pour que le modèle soit celui du joueur :
- **Claude** prépare la référence (la planche) et la fiche (les pièces que le code utilise), cherche les packs et dit quelles
  pièces existent et où (« la boîte de Lego »), vérifie le prefab et la performance, adapte le code si une pièce en a besoin.
- **Le joueur** choisit les pièces, les place, les met à l'échelle, les colore. **Claude ne donne pas de composition** (pas de
  positions, pas de « mets ce cylindre là ») : le bâtiment redeviendrait sa conception, exécutée par d'autres mains.

### 4.1 La référence (Claude la prépare)

- **Les images de référence** (`Tools/refonte_visuelle/planche.py`, dans `Docs/Planches/<Batiment>/`) : les vues de face, de
  droite et de dessus de Blender, à l'échelle exacte (un carreau = 1 m, l'origine marquée), au niveau le plus haut, avec les
  réglages à taper dans Blender pour les poser ; et une planche des niveaux. Depuis `Project Archery/Assets` :
  `py -I ../Tools/refonte_visuelle/planche.py _Project/Prefabs/Structures/Givre.prefab ../Docs/Planches/Givre tower-round-crystals`
  (le dernier argument, facultatif : des objets dont on veut la place). Comment s'en servir : `Guide_Blender.md`, 3.3.
- **La fiche des pièces à garder** (`Tools/refonte_visuelle/reperes.py`) : la hiérarchie du prefab, et ce que le code en fait.
  Depuis `Project Archery/Assets` : `py -I ../Tools/refonte_visuelle/reperes.py _Project/Prefabs/Structures/Baliste.prefab`.
  - `<- m_…` : un script désigne l'objet (une pièce qui bouge, un repère d'où part un tir, les pièces d'un niveau). **Le garder,
    avec son nom** ; on change son maillage, pas l'objet.
  - `[porte un script]` : l'objet a un script à lui. Le garder aussi.
  - `(caché : marque d'un baker, à garder)` : les objets cachés au nom daté (« Allure du 30 octobre », « Duo du 31 octobre »…).
    Sans eux, le baker refait le modèle de Claude au lancement suivant.
- **Le modèle de Claude en 3D**, si la planche ne suffit pas : Claude le sort en `.obj`, à mettre dans Blender dans une
  collection à part, verrouillée et en fil de fer, pour tourner autour ; jamais retouché, jamais exporté.

### 4.2 Les pièces

- **Les formes de Blender** (*Shift+A > Mesh*) : cube, cylindre, cône, sphère, tore. **Peu de côtés** : un cylindre de 8 à 16
  côtés (32 par défaut), une sphère UV de 12 × 6 ou une icosphère de niveau 1 ou 2 (une sphère d'Unity fait près de 800
  triangles : le Manège de Claude en a 537, environ 400 000 triangles, qu'il faut fondre au lancement).
- **Les packs** : d'abord ce qui est dans le projet (`kenney_tower-defense-kit`, `kenney_castle-kit`, `kenney_fantasy-town-kit`,
  `kenney_nature-kit`, dans `Models/FBX format/`, avec leur texture de couleurs `Textures/colormap.png` ;
  `construction/Ultimate Fantasy RTS by Quaternius/FBX`), à importer dans Blender (*File > Import > FBX*) ; puis d'autres kits
  de Kenney, de Quaternius ou de KayKit (CC0) que Claude propose bâtiment par bâtiment, avec ce qu'ils contiennent. Les retoucher
  est permis (CC0 ; pour un CC-BY, le dire dans les crédits). Toujours demander avant de télécharger, créditer au fur et à mesure
  (`Credits_Licences.md`).
- **Attention aux modèles faits par IA** : Sketchfab, Poly Pizza ou itch.io en ont de plus en plus (Meshy, Tripo…). S'en tenir
  aux auteurs connus (Kenney, Quaternius, KayKit) ; pour un autre auteur, regarder son profil et ses autres modèles avant.

### 4.3 Dans Blender, bâtiment par bâtiment

Le détail des gestes sera dans un guide pas à pas (à écrire avant le premier bâtiment) ; ici, les règles qui comptent pour le jeu.
1. **Un fichier par bâtiment**, hors d'`Assets` (Unity essaierait d'ouvrir un `.blend` avec Blender) :
   `Project Archery/Modeles_Blender/<Batiment>.blend`.
2. **La planche** en image de référence (*Add > Image > Reference*) dans les vues de face et de côté, à l'échelle : la barre de
   1 m sur un carreau de la grille.
3. **Le bâtiment au sol, centré sur l'origine du monde** ; 1 unité = 1 m.
4. **Les pièces que le code utilise** (la fiche) : un objet chacune, **nommé exactement comme dans la fiche**, son origine
   (*Object > Set Origin*) là où elle tourne ou d'où part le tir. Ce qui bouge ensemble se joint en un objet (*Ctrl+J*).
5. **Les pièces d'un niveau** : rattachées (*Ctrl+P*) à un objet vide nommé `Niveau 2`, `Niveau 3`… (les tours de base vont
   jusqu'au niveau 5, les duos et les trios jusqu'au 3).
6. **Le reste** : autant d'objets que tu veux (Claude les fond côté Unity s'il y en a beaucoup).
7. **Moins de pièces que Claude** : ses modèles sont surchargés (jusqu'à 942 pièces). Quelques dizaines de pièces bien choisies
   se lisent mieux de loin dans le casque, et une grosse pièce de pack porte plus de détail que dix cubes.
8. **Peu de matériaux** : un par couleur (`Bois`, `Pierre`, `Laiton`…), partagé ; les pièces de Kenney gardent le leur (leur
   texture de couleurs). Ce qui luit : un matériau dont le nom commence par `Lueur` (Claude le rend lumineux dans Unity).
9. **L'export** : *File > Export > FBX*, dans `Assets/_Project/Art/Modeles/<Batiment>.fbx` (pas d'accents) ; *Limit to* les
   objets visibles (la référence cachée), *Object Types* `Empty` et `Mesh`, *Scale* 1, *Apply Scalings* `FBX All`, *Forward*
   `-Z Forward`, *Up* `Y Up`, *Apply Transform* coché. Claude vérifie l'orientation au premier essai, et corrige ces réglages
   s'il le faut.

### 4.4 Dans Unity : le branchement

1. **Au premier bâtiment**, Claude écrit l'outil (un menu d'Unity) qui branche ton FBX sur le prefab : chaque pièce nommée
   comme une pièce de la fiche prend ta pièce (l'objet du jeu reste, avec ses scripts), les pièces sous `Niveau N` vont sous le
   `Niveau N` du prefab, le reste sous `Modele fait main` ; pas de colliders ; les pièces de Claude retirées (jamais la racine, ni
   les marques cachées des bakers) ; l'objet caché `Fait main` ajouté (la règle pour Claude : plus aucun baker ne touche à ce
   bâtiment).
2. **À chaque changement** : réexporter le FBX sous le même nom, relancer le menu.
3. **Tester** : **F4** (la vitrine : chaque bâtiment, à plusieurs niveaux), puis une partie ; demander à Claude de lire le journal
   et de compter les pièces et les triangles. Si un effet ne marche plus sur tes pièces (une lueur, une couleur qui change),
   Claude adapte le code. Puis cocher l'**État** ici, et Claude ajoute le bâtiment à `Credits_Licences.md` (« modélisé par le
   joueur », avec les packs utilisés).

### 4.5 L'ordre proposé

1. **Un bâtiment simple pour roder la chaîne** : le Givre (12 formes) ou les Pièges.
2. **Les 10 tours de base** (vues à chaque partie).
3. **Les 34 icônes** (rapides, très visibles ; des icônes de packs, lot 3).
4. **Les duos, puis les trios, puis les ultimes** (le Manège en dernier). Si un bâtiment coûte trop pour ce qu'il apporte, c'est
   aussi l'occasion de se demander s'il garde sa place (le fil rouge : moins de complexité).
5. **La batte**, les deux maillages du lot 4, puis **les décors** (avec Claude pour le code).
6. **Les sons et les textes**, si le but reste de ne pas cocher la case de Steam (partie 1).

## 5. La règle pour tout nouvel élément visuel (le joueur, 14 novembre)

Pour ne pas refaire de la modélisation en boucle, dès qu'il faut quelque chose de nouveau à voir dans le jeu :
1. **D'abord, un asset gratuit fait par des humains**, posé tel quel. Vérifier sa **licence** : CC0 (rien à faire), CC-BY
   (créditer, dans `Credits_Licences.md`), l'EULA de l'Asset Store (pas de redistribution : voir le dépôt public, en tête de
   `Credits_Licences.md`). **Écarter** : « gratuit » sans licence écrite, NC (pas d'usage commercial : interdit pour un jeu
   vendu), ND (pas de modification), SA (le jeu entier devrait suivre la même licence : à éviter). Vérifier aussi que l'auteur
   est un humain (de plus en plus de modèles gratuits sont faits par IA : regarder son profil). Toujours demander avant de
   télécharger.
2. **Sur demande du joueur, deux ou trois détails simples ajoutés par Claude** (comme les bandes néon des Akimbow : une bande,
   un anneau, une lueur), notés dans la liste ci-dessous. Peu de risque à notre avis (comme choisir une couleur), mais c'est
   fait par l'IA : la liste permet au joueur de les refaire en quelques minutes s'il veut zéro doute.
3. **Sinon, un assemblage de plusieurs assets gratuits, fait par le joueur** : Claude propose les pièces (lesquelles, où les
   trouver), le joueur les compose. **Un assemblage composé par Claude compterait comme fait par l'IA** (la règle de pouce de la
   partie 2 : c'est ainsi qu'ont été faites les tours actuelles). Les licences doivent permettre de modifier et de combiner (CC0,
   CC-BY, Asset Store : oui).
4. **Sinon, un provisoire dessiné par Claude**, noté dans la liste ci-dessous, et marqué dans son prefab par un objet vide caché
   `Provisoire Claude` (pour le retrouver). À refaire à la main avant la sortie, ou à déclarer à Steam.

### Les sons : la même règle (le joueur, 14 novembre)

1. **D'abord, un son gratuit fait par des humains** : freesound.org (CC0 d'abord ; CC-BY crédité), les sons de Kenney ; les
   mêmes licences à écarter (NC, ND, SA, « gratuit » sans licence) ; regarder la description et les étiquettes du son (pas de son
   généré par IA). Toujours demander avant de télécharger (le joueur télécharge lui-même sur freesound, qui demande un compte).
2. **Le montage simple par Claude** (`Tools/sons/`) : couper, fondre, régler le volume, passer en mono, rééchantillonner,
   boucler, filtrer un peu, remettre à l'endroit. À notre avis, ce n'est pas de l'IA : la matière du son reste celle de
   l'humain, et n'importe quel éditeur ferait la même chose. Noté dans `Credits_Licences.md`, comme aujourd'hui.
3. **Sinon, un son nouveau fait de plusieurs sons gratuits** (les superposer, les saturer fort, les déformer), **par le joueur**,
   à l'oreille, dans Audacity ; Claude propose les sons. Le même son transformé par Claude serait une zone grise (le mélange et
   la forte saturation sont des choix de création) : il va dans la liste ci-dessous.
4. **Sinon, un son provisoire synthétisé par Claude**, noté dans la liste ci-dessous, à remplacer avant la sortie.

**Avant toute sortie publique** (le jeu, mais aussi une démo sur Steam, les captures et la bande-annonce de la page) : la liste
doit être vide, sinon la case de Steam se coche. Les textes faits par Claude comptent aussi (partie 1).

### La liste des provisoires et des détails de Claude (visuels et sons)

L'inventaire de la partie 3 couvre ce qui existait le 14 novembre (les sons compris). Ici, ce que Claude ajoute depuis.

| Élément | Où | Sorte | Ajouté le | État |
|---|---|---|---|---|
| Les bandes néon des Akimbow (10 cubes) | `Prefabs/Bow Visual Akimbow.prefab` | détail (2) | avant le 14 novembre | gardé (facultatif à refaire) |

## 6. Avancement

- **14 novembre** : l'inventaire (partie 3), la méthode (partie 4), la fiche des pièces à garder (`Tools/refonte_visuelle/reperes.py`).
  Puis les réponses du joueur : l'arène de MK est sa conception (partie 2), sauf ses fissures, dessinées par Claude (lot 5) ; les
  effets sont gardés ; les Akimbow et les orbes restent (les néons des Akimbow, facultatif) ; **la méthode** : assembler à la
  main des formes simples et des pièces de packs faits par des humains, **dans Blender** (le choix du joueur, approuvé par Claude),
  Claude conseillant les pièces, jamais la composition. Puis **le guide de Blender** (`Guide_Blender.md` : installer, les gestes
  de base, le Givre en premier exercice, l'export) et **les images de référence du Givre** (`Docs/Planches/Givre/`, faites par
  `Tools/refonte_visuelle/planche.py`). Puis l'estimation (environ 270 h avec le premier style, de 200 à 400 h) et, pour la
  réduire, **le style très low poly et les mouvements laissés au code** (partie 4 ; à peu près 190 h, de 130 à 270 h, à
  recalculer avec le temps du Givre et des deux tours suivantes). Rien n'est encore refait. **La suite** : le joueur fait le Givre dans Blender ; à son
  premier export, Claude écrit l'outil de branchement (4.4) et vérifie l'orientation et les réglages d'export.
