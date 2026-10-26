# Fichiers inutilisés (25 octobre 2026)

La liste des assets que rien n'utilise dans le projet, pour faire le ménage.

**Ménage fait le 25 octobre**, à la demande du joueur, en deux passes : 3 726 fichiers supprimés avec leurs `.meta`
(740 Mo), marqués *supprimé* ci-dessous (parties 2 et 3), puis les restes des packs (partie 4). Ils restent dans
l'historique de Git. Après lui, il reste 1 894 fichiers inutilisés (43 Mo), gardés exprès (partie 5). **Pas encore vérifié
dans Unity** (partie 6, étape 2).

**Comment elle est faite** : `Tools/unused_assets.py` suit les liens entre les assets, sans ouvrir Unity. Chaque asset a un
identifiant (le GUID de son `.meta`) : une scène cite ses prefabs, un prefab ses matériaux, un matériau ses textures et son
shader. Le script part de ce que le jeu utilise à coup sûr et liste ce qu'il n'atteint jamais. Il part des scènes du build,
des réglages du projet, des dossiers `Resources`, des chemins écrits dans le code (les bakers) et des scripts qui se lancent
seuls. Pour le relancer après un ménage, depuis `Project Archery/` :

```
python Tools/unused_assets.py
```

Avec `--liste fichier.txt`, il écrit aussi la liste complète.

**Ce qu'on y gagne** : un asset inutilisé **n'est pas dans le build**. Unity n'y met que ce que les scènes et `Resources`
utilisent. Le supprimer ne rend donc pas le jeu plus léger ni plus rapide. Il allège le projet : le dépôt à cloner,
l'import dans Unity, le dossier `Library` (3,8 Go aujourd'hui) et les recherches dans la fenêtre *Project*.

**Limite** : un fichier chargé par un nom que le code fabrique (« Hologramme » + une couleur), ou par un outil hors de Unity,
paraît inutilisé. Les cas trouvés sont rangés dans la partie 5. Tout le reste a été vérifié à la main dans le code.

## 1. En bref

Avant le ménage, 3 523 fichiers sur 5 154 ne servaient à rien, soit **737 Mo**. Presque tout le poids était dans quatre
groupes :

| Quoi | Taille | Pourquoi rien ne s'en sert |
|---|---|---|
| Le pack **Easy Weapons** (`Assets/Kendall H`), en entier | **488 Mo** | Les arcs des classes ont pris d'autres modèles ; plus aucun prefab ne s'en servait. *Supprimé.* |
| Les dragons albinos, bleu et noir (`FourEvilDragonsPBR`) | 84 Mo | MK n'utilise que le dragon rouge. *Supprimés* (l'albinos, puis le bleu et le noir) |
| `Assets/_Recovery/0.unity` | 18 Mo | Une copie de secours de Unity après un plantage (18 octobre), commitée par erreur. *Supprimé.* |
| La texture `Grunt_Red.png` (Mini Legion Grunt) | 17 Mo | Le grunt utilise ses textures PBR. *Supprimée.* |

Le reste, ce sont des petits fichiers : les restes du prototype, l'ancien katana, le brasero retiré… (partie 3), et les
morceaux inutilisés des packs (des modèles, des scènes de démo : partie 4).

**Les packs de l'Asset Store** : le dépôt GitHub est public, et `Credits_Licences.md` rappelle que leur contrat interdit d'en
redistribuer les fichiers. Easy Weapons en était un : c'est réglé pour la suite, mais ses fichiers restent dans
l'historique de Git (partie 5).

## 2. Les gros morceaux

| Supprimer | Taille | Après, mettre à jour |
|---|---|---|
| *Supprimé* : `Assets/Kendall H` (le pack Easy Weapons) | 488 Mo | Fait : `Credits_Licences.md` (points 1 et ligne Easy Weapons), le README (crédits), le GDD (sections 18 et 21), `Guide_Modeles.md` et `Guide_Boutique.md` (ils montrent comment poser un arc d'Easy Weapons), un commentaire de `Bows/BowVisual.cs` |
| *Supprimé* : l'albinos (`Texture/DragonUsurper/Albino`, `Albino.prefab`, `AlbinoPBR.mat`) | 28 Mo | Rien |
| *Supprimé* : `Assets/FourEvilDragonsPBR/Texture/DragonUsurper/Blue` et `Dark` ; `Blue.prefab`, `Black.prefab`, `BluePBR.mat`, `DarkPBR.mat` | 56 Mo | Rien (le crédit du pack reste) |
| *Supprimé* : `Assets/_Recovery` | 18 Mo | Rien |
| *Supprimé* : `Assets/Mini Legion Grunt PBR HP Polyart/Textures/HP/Grunt_Red.png` | 17 Mo | Rien |
| *Supprimé* : `Assets/energy-gauntlet-low-poly-free-download` (l'ancien gantelet, CC-BY) | 0,9 Mo | Fait : son crédit reste (le modèle est encore dans l'historique de Git), marqué supprimé dans `Credits_Licences.md` et le README |
| *Supprimé* : `Assets/kenney_ui-pack` et `Assets/UIpack_RPG` (881 et 92 fichiers) | 1 Mo | Fait : leurs lignes retirées de `Credits_Licences.md` et du README |

## 3. Les fichiers du jeu (`Assets/_Project`)

C'est la liste du ménage du GDD (section 21), vérifiée et complétée. **Tout a été supprimé le 25 octobre**, et « À toi de voir » aussi,
sauf les bordures d'escalier (le prefab `Chest` n'avait aucun son réglé : `chest_open` et `chest_vanish` sont partis avec les autres). Tout est petit (2,6 Mo en tout, sans les sons de la
partie 5) : c'est surtout pour s'y retrouver.

**L'ancien katana** (avant la 3ᵉ version, le 24 octobre) :
- `Art/Katana/Bouclier.prefab`, `Bouclier.asset` (son maillage), `Bouclier cuir.mat`, `Bouclier face.mat` ;
- `Art/Katana/Gantelet.prefab` et ses 5 matériaux `Gantelet …` (avec le pack du gantelet, partie 2) ;
- `Art/Katana/Katana.asset` (l'ancien maillage), `Katana lame.mat`, `Katana metal.mat`, `Katana neon.mat` ; le katana
  utilise `Katana lame acier` et `Katana lame neon`.

**Le toit cyberpunk** : `Art/Cyber/Cascade creusee 1.asset`, `Cascade creusee 2.asset` et leurs deux `… collision.asset`.
Les cascades utilisent les « v2 ».

**Le brasero** (retiré de la scène le 12 octobre ; son code reste, comme le dit le GDD) : `Prefabs/Brazier.prefab`,
`Prefabs/Arrow Fire.prefab`, `Materials/Brazier_Stone.mat`, `Audio/Placeholder/arrow_ignite.wav` et `brazier_build.wav`.

**Le Roi Gluant** (remplacé par Thomas le 15 octobre) : `Prefabs/Boss_RoiGluant.prefab`, `Data/Enemies/Boss_RoiGluant.asset`.

**Les restes du prototype** :
- `Prefabs/Arrow Classic.prefab` et `Prefabs/Bow Visual Classic.prefab` ;
- 14 matériaux de `Materials` : `Arena_Floor`, `Arena_FloorDark`, `Arena_FloorScorched`, `Arrow_Fletching`, `Arrow_Head`,
  `Arrow_Nock`, `Arrow_Shaft`, `Boss_Knoght`, `Bow_Leather`, `Bow_Wood`, `DemoMat`, `Enemy_Skin`, `Shooting_Line` (et
  `Brazier_Stone`, plus haut) ;
- les sons provisoires `Audio/Placeholder/chest_open.wav`, `chest_vanish.wav`, `impact_target.wav` ;
- `Animations/WK_heavy_infantry_06_combat_walk.FBX` (le chevalier géant, retiré ; ses autres animations servent encore).

**Le reste** :
- `Scenes/Arena_Fourche.unity` : une scène d'essai de l'arène. L'arène est construite dans `Arena_Dragon` à partir de
  `Data/Arena/Arena_Fourche.txt`, qui, lui, sert.
- `Art/Dimension/Torii_Laque_Noire.png`, `Torii_Laque_Rouge.png`, `Torii_Relief.png` : les matériaux des torii ne s'en
  servent pas.
- `Assets/sci_fi_crossbow.glb`, à la racine d'`Assets`.

**À toi de voir** :
- `Art/Stair Borders` : 5 bordures d'escalier sur 16 ne sont pas posées sur la carte (les 4 diagonales et
  `cliff_stairBorder_lower_right_stone`). Elles peuvent resservir si la carte change. *Gardées.*
- *Supprimé* : **du code jamais utilisé** :
  - `Scripts/World/NightOnly.cs` (des objets visibles seulement la nuit) n'est posé nulle part ;
  - `Scripts/FinalBoss/DragonArenaBuilder.cs` (l'arène ronde du dragon) a été remplacé par la fourche le 15 octobre.
    `ArenaShape.cs` ne le cite que dans un commentaire.

  La compilation est vérifiée (`python Tools/compile_check.py`, et les outils d'éditeur) ; le commentaire d'`ArenaShape`
  est mis à jour.
- *Supprimé* : `Assets/Scenes` (`SampleScene`, `BasicScene` et leurs lumières précalculées, 6,5 Mo) : les scènes d'exemple du modèle VR de
  Unity. `SampleScene` était décochée dans *Build Settings* : elle en est retirée. Les deux modèles de scène
  `Settings/Project Configuration/*.scenetemplate` (1 Mo) vont avec.

## 4. La deuxième passe : les restes des packs

Le joueur a demandé ensuite de supprimer tout ce qui est utile à supprimer. La règle suivie : partent les fichiers qui ne
serviront jamais (les démos, les variantes que le jeu n'a pas retenues), restent ceux qu'on pourrait poser plus tard
(partie 5). Avant de supprimer, un fichier gardé qui cite encore un fichier visé le garde avec lui.

- **Les packs de monstres de Dungeon Mason** (Mini Legion, RPG Monster, Monster Minion, Wizard, dragons) : leurs scènes de
  démo, leurs socles (`Stage`), leurs ciels, leurs images promotionnelles et **les versions de style que le jeu n'utilise
  pas** (HP et Polyart : le jeu prend les versions PBR, sauf pour le mage, qui garde `PolyArtWizardStandardMat`). Pour le
  mage, aussi la variante `MaskTint` (un shader de l'ancien pipeline, rose en URP) et les archives `SRP/*.zip`. Dont
  `Grunt_Red.png` (17 Mo).
- **Le coffre animé** : sa scène de démo, sa caméra, sa timeline, son prefab de démo et sa texture *Unlit*. Le FBX, les
  matériaux et l'animation `_Press` restent.
- **AgeSoft** : la scène d'exemple et la texture `SpecularSmoothness` (le marteau utilise le flux métallique).
- **Dimasjk Studio** : les dossiers `Demo` (les scènes et les caméras de présentation). Les icônes restent : elles peuvent
  servir à la boutique.
- **Les packs entiers inutilisés** : `kenney_ui-pack`, `UIpack_RPG`, `energy-gauntlet-low-poly-free-download` (partie 2).
- **Les scènes d'exemple de Unity** et le code jamais utilisé (partie 3).
- **Les exemples des paquets XR** (`Samples`) : seulement les scènes de démo et leurs décors (`DemoAssets`,
  `HandsDemoScene`, `DemoScene`, `KeyboardDemo`, la scène de `HandVisualizer`). Les briques (prefabs d'interacteurs,
  mains, clavier), les scripts et les shaders restent.
- **Le modèle VR de Unity** (`VRTemplateAssets`) : **c'était `SampleScene` qui le gardait**. Le jeu ne s'en sert presque
  pas : son joueur en VR vient des exemples de XR Interaction Toolkit. Une fois `SampleScene` partie, il ne restait d'utile
  que la police *Inter* (`Inter-Regular SDF`), le modèle `Spatial Panel Manipulator Model` et quelques scripts. Ses décors,
  matériaux, sons, sa vidéo, ses prefabs et le tutoriel du modèle (`Tutorial`, lu par le paquet *Tutorial Framework*) sont
  partis ; ses scripts et ses shaders restent.

Total de la deuxième passe : 3 076 fichiers avec leurs `.meta` (environ 147 Mo). Un guide citait des fichiers supprimés :
`Guide_Volants_Tireurs.md` (les versions Polyart du Beholder, la variante du mage), corrigé.

## 5. Ce qui reste, gardé exprès (1 894 fichiers, 43 Mo)

- **Les modèles et les sons des kits, qu'on pourra poser plus tard** : Kenney (nature, château, tower defense, ville, sons),
  le *Cyberpunk Game Kit* de Quaternius (robots, tourelles, plateformes : 6,9 Mo), les armes et les boucliers de Dimasjk
  Studio et leurs icônes, les bâtons du mage, le prefab du marteau, l'animation `_Press` du coffre.
- **`kenney_game-icons`** et les PNG de `kenney_fantasy-ui-borders` : la présentation s'en sert
  (`Presentation/generer_presentation.py`).
- **Des matériaux qu'un baker refait** : `Art/Cyber/Accent violet.mat`, `Hologramme ambre.mat`, `Hologramme violet.mat`,
  `Texte neon violet.mat`. `CyberDimensionBaker` fabrique toujours les 4 couleurs : supprimés, ils reviendraient.
- **`Glove by Quaternius/Glove.fbx`** : la source de `Art/Hands/Glove Rig.json`, dont le code construit les mains gantées.
- **Les sons d'origine de l'arc cosmique** : `Audio/514082__tim_verberne__sci-fi-weapon-charging-01.wav` (2,8 Mo) et
  `Audio/814049__qubodup__ion-cannon.wav` (1,1 Mo). Le jeu utilise leurs versions recoupées (`Arc_Cosmique_Charge.wav`,
  `Arc_Cosmique_Laser.wav`) ; les originaux servent à les recouper autrement.
- **Les 5 bordures d'escalier** non posées (partie 3).
- **Les fichiers de Unity et des paquets** : TextMesh Pro, les réglages `XR`, `XRI` et `CompositionLayers`, les fichiers
  `.mtl` à côté des modèles `.obj` (lus à l'import), les `.asmdef`, les autres graisses des polices *Cinzel*.
- **Les briques des exemples XR et les scripts du modèle VR** (4 Mo environ) : les trier encore risque de casser une
  dépendance entre scripts pour presque rien.

Le paquet *Tutorial Framework* (`com.unity.learn.iet-framework`) ne servait qu'au tutoriel du modèle VR, supprimé. On peut le
retirer dans *Window > Package Manager* (pas fait : c'est un changement de paquets, à faire dans Unity).

## 6. Supprimer sans risque

1. **Un groupe à la fois.** Le plus simple est de supprimer dans la fenêtre *Project* de Unity, qui retire aussi le `.meta`.
   Unity fermé, il faut supprimer chaque fichier **et** son `.meta`.
2. **Ouvrir Unity et lire la console** : aucune erreur de référence manquante. Puis jouer chaque scène du build
   (`Prototype_Tir`, `Arena_Dragon`, `Dimension_Etoiles`, `Dimension_Cyberpunk`).
3. **Mettre à jour les crédits** pour un pack supprimé (partie 2), puis relancer `python Tools/unused_assets.py`.
4. **Git** : un fichier supprimé reste dans l'historique et peut revenir (`git checkout <commit> -- <chemin>`). Le dossier
   `.git` (412 Mo) ne rétrécit donc pas : seuls le projet et les prochains clones s'allègent.

Pour vérifier un asset dans Unity avant de le supprimer : clic droit sur l'asset, *Find References In Scene*, dans chaque
scène du build.
