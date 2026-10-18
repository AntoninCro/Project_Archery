# Guide : le portail des étoiles (easter egg)

L'easter egg du GDD (section 23), codé le 18 octobre, **pas encore testé dans le casque**.

## Ce que fait le joueur

1. Sur la terrasse de droite de la carte, l'anneau de pierre (`statue_ring`) est entouré de 5 obélisques (`statue_obelisk`). Chacun porte une **rune violette**, à peine visible, sur sa face qui ne regarde pas l'anneau : une sur deux à hauteur de poitrine, les autres au pied.
2. Il les **touche de la main** (sans casque : il les regarde de près et appuie sur **E**). La rune s'éveille quand la main approche, et s'allume quand on la touche. Hors d'une vague, elle reste froide (un message l'explique, au plus toutes les 8 s).
3. Les 5 runes allumées **pendant une même vague** ouvrent le portail : un voile sombre bordé de violet, plein d'étoiles, avec de petits bugs rouges. En fin de vague, s'il n'est pas entré, tout s'éteint.
4. Il **traverse l'anneau** : fondu au noir, la carte se fige (vague, chrono, ennemis, projectiles, tourelles, renforts, coffres, cristaux), et il arrive dans la dimension avec son **seul arc de départ, sans aucune amélioration** (il les retrouve au retour). Une seule tentative par partie.
5. **Dans la dimension** : ciel noir criblé d'étoiles rondes, 4 nébuleuses qui traversent le ciel d'un horizon à l'autre (dont la Voie lactée), 6 grandes galaxies spirales, ni nuages ni aurore. Un sol miroir, et environ 125 piliers de 7 m en laque rouge unie, légèrement métallique, plantés en désordre, de tailles variées, tous droits : au-dessus du sol, seulement les deux piliers ; **le haut des torii n'existe que dans le reflet**. 10 s pour découvrir le décor pendant que la musique monte, puis **1 minute à tenir** contre le monstre corrompu. Pendant la découverte, les panneaux d'erreur rouges montent : le premier à 5 s, puis de plus en plus vite, jusqu'à une quarantaine quand le monstre apparaît ; un très grand « SURVIVEZ » s'ouvre alors et les autres se ferment presque d'un coup. Ensuite, 2 à 3 panneaux s'ouvrent par seconde et restent 2,5 à 4,5 s (une dizaine à la fois) ; le temps restant n'est affiché que sur eux. Un panneau qui se retrouve devant le monstre se ferme aussitôt. (Réglages dans `ErrorPanels` : `k_SurgeStart` 5 s, `k_SurgeCount` 40.)
6. **Les monstres** (3 en Normal, 2 en Facile, plus en Difficile et en Impossible) : des silhouettes sombres et étirées d'ennemis du jeu, au contour qui grésille en rouge et cyan, avec deux yeux rouges qui clignent. **Ils ne bougent que quand on ne les regarde pas** : dans l'axe du regard (25°) et sans pilier entre eux et le joueur, ils se figent comme des statues. Le regard détourné, ils avancent par saccades, comme une vidéo qui saute des images, figés dans une pose de course qui change à chaque saut, toujours tournés vers le joueur : on les voit bouger du coin de l'œil. Ils apparaissent loin, **dans le champ de vision** du joueur, écartés les uns des autres, cachés derrière des piliers (on les entend grésiller) ; cachés, ils penchent parfois la tête hors du pilier. Puis ils avancent de pilier en pilier, de plus en plus près, en passant **hors de la vue du joueur** (à plus de 70° de son regard : sur les côtés, dans son dos) : ils le contournent, et il doit se retourner pour les surveiller. Faute de pilier hors de vue pendant 1,2 s, ils en prennent un qu'il voit du coin de l'œil. Tout près, ils foncent sur le joueur. Une flèche, tirée ou au corps à corps, en tue un : il éclate en pixels, et un autre le remplace 1,2 s plus tard, loin, devant le joueur (jamais dans son dos), avec l'apparence de l'ennemi suivant de la liste.
7. **Fin** : touché, le joueur est expulsé (retour devant le portail, tous ses PV, portail fermé jusqu'à la fin de la partie). S'il tient, il gagne **2 500 points** (× la difficulté) et une **étoile à côté de son nom** dans le classement. L'arc cosmique viendra plus tard.

## Mise en place

Presque rien à faire à la main :
- à l'ouverture de `Prototype_Tir`, l'outil `StarPortalBaker` crée l'objet **« Portail des étoiles »** (relié à l'anneau, aux 5 obélisques, au matériau `Effects` et aux sons), mesure l'anneau et pose les runes sur les obélisques, puis ajoute `Dimension_Etoiles` à la liste des scènes du build. **Enregistre la scène (Ctrl+S).**
- au premier rechargement des scripts, l'outil `StarDimensionBaker` construit le décor de la dimension dans `Dimension_Etoiles` et **enregistre la scène tout seul**.

## Modifier à la main

### Le portail (Prototype_Tir)

Sous **« Portail des étoiles »** :
- **« Ouverture du portail »** : sa position est le centre du voile, son axe bleu (Z) le sens du passage, son échelle X le rayon du voile. Sélectionne « Portail des étoiles » pour voir le cercle violet dans la vue Scene.
- **« Rune 1 » à « Rune 5 »** : déplace-les ou tourne-les librement (leur face avant regarde vers l'extérieur de la pierre).
- Pour tout remesurer : clic droit sur le composant *Star Portal* > **Replacer l'ouverture et les runes**, ou menu **Archery > Portail des étoiles > Replacer l'ouverture et les runes**.
- Pour les runes seules (l'ouverture est gardée) : **Replacer les runes**, au même endroit. Chaque rune est posée au milieu d'une face de l'obélisque, face à son axe : vues depuis l'anneau, certaines tombaient près d'une arête et débordaient dans le vide (corrigé le 18 octobre).

Pourquoi dans l'éditeur : en jeu, Unity fusionne les statues statiques en un seul maillage (*static batching*) et le script ne peut plus lire leur forme. C'est ce qui décalait le voile de 90° et décollait les runes.

### La dimension (Dimension_Etoiles)

Ouvre la scène : tout le décor est fait d'objets ordinaires, sous **« Dimension »** (à 3 km de la carte et 600 m plus haut : plus bas, les flèches disparaîtraient ou les effets de la carte suivraient le joueur).

| Objet | Contenu |
|---|---|
| `Arrivée du joueur` | Où le joueur arrive ; son axe bleu donne la direction du regard |
| `Ciel/Étoiles` | Les étoiles, tout autour (le dessous compris) |
| `Ciel/Nébuleuses`, `Ciel/Galaxies` | Les nébuleuses et les galaxies : déplace-les, tourne-les, change leur échelle |
| `Sol miroir` | Le sol où l'on marche (Box Collider), le disque sombre et la grille |
| `Torii/Torii de l'épreuve` | Les torii proches : le monstre se cache derrière leurs piliers |
| `Torii/Torii lointains` | Les torii jusqu'à l'horizon |
| `Murs invisibles` | Le cercle qu'on ne peut pas franchir (rayon 23 m) ; les flèches les traversent |
| `Lumière des étoiles`, `Musique` | La lumière directionnelle, la musique (et son filtre) |
| `Reflet` | Le miroir : ce qui est sous ses *Sources* est recopié à l'envers sous le sol, dans l'éditeur comme en jeu |

- **Les torii** sont des instances du préfab `Art/Dimension/Torii` : duplique-les (Ctrl+D), déplace-les, tourne-les, change leur taille. Le reflet suit. Le préfab contient les deux piliers (avec leurs Capsule Collider), et **« Haut (seulement dans le reflet) »** : la traverse, le linteau et le toit, dont le *Mesh Renderer* est décoché. Un rendu décoché n'apparaît que dans le reflet ; coche-le pour le voir aussi au-dessus du sol.
- **Le monstre** se cache derrière tous les piliers (Capsule Collider) à moins de *Post Range* (45 m) du centre, lointains compris.
- **Les couleurs** sont dans les matériaux de `Art/Dimension` : `Torii_Vermillon`, `Torii_Noir`, `Sol_Miroir` (l'alpha de sa couleur règle la netteté du reflet : 0 = miroir parfait), `Sol_Grille`, `Ciel_Lueurs` (l'éclat des étoiles, nébuleuses et galaxies), `Ciel_Dimension` (le fond du ciel).
- **Le ciel, la lumière ambiante et le brouillard** sont ceux de la scène (fenêtre *Lighting > Environment*, avec `Dimension_Etoiles` comme scène active).
- Un torii penché par erreur : menu **Archery > Portail des étoiles > Redresser les torii** (garde leur orientation autour de la verticale).
- Plus de torii : menu **Archery > Portail des étoiles > Ajouter des torii dans les trous** (ne touche pas à ceux qui existent ; écarts de 7 m dans la zone de l'épreuve, de plus en plus grands au loin, et 2,4 m au moins entre deux piliers). À l'inverse, **Éclaircir les torii** retire ceux qui sont plus serrés que ces écarts (les plus anciens sont gardés).
- L'aspect des torii : une laque rouge unie, légèrement métallique (`Torii_Vermillon` : *Metallic* 0,35, *Smoothness* 0,6, faible lueur), et noire pour le dessus. Règle la couleur, le métal et le brillant directement dans les matériaux ; menu **Archery > Portail des étoiles > Refaire l'aspect des torii** pour revenir aux valeurs de départ.
- Pour tout refaire depuis zéro : menu **Archery > Portail des étoiles > Reconstruire le décor de la dimension** (les retouches à la main sur le décor sont perdues ; les matériaux et le préfab sont gardés).

## Tester

- Dans l'éditeur, en jeu, **P** allume les 5 runes (le portail s'ouvre), puis **P** encore fait entrer dans la dimension.

## Régler

| Où | Réglage | Valeur |
|---|---|---|
| `Portail des étoiles` (Prototype_Tir) | *Touch Distance*, *Rune Size*, *Rune Color* | 0,25 m, 0,24 m, violet |
| | *Score Bonus* | 2 500 points |
| | *Fade Time* | 1 s |
| `Dimension` (Dimension_Etoiles) | *Intro Time*, *Survive Time* | 10 s, 60 s (30 s jusqu'au 18 octobre) |
| | *Post Range* | 45 m (piliers qui servent de cachettes) |
| | *Monster Models* | l'ordre des apparences des monstres |
| | *Monster Count* | 3 monstres en même temps en Normal (selon le nombre d'ennemis de la difficulté) |
| | *Respawn Delay* | 1,2 s |
| | *Fake Errors* | les fausses erreurs des panneaux |
| | sons et musique | sons Kenney *Interface Sounds*, `music_boss` |

Les monstres suivent la vitesse des ennemis de la difficulté : ils se cachent moins longtemps (0,8 s en Normal) et avancent plus vite (environ 12 m/s par saccades en Normal, 9,5 m/s quand ils foncent sur le joueur) en Difficile et en Impossible ; mais seulement quand on ne les regarde pas. S'il n'y a aucun pilier plus proche dans le champ de vision, il attend que le joueur se tourne ; au bout de 1,2 s, il prend un pilier au bord du champ de vision ; à moins de 8 m, il fonce.

## Les scripts

| Script | Rôle |
|---|---|
| `Portal/StarPortal` | Les runes, l'ouverture, le passage, le retour, la récompense (carte) |
| `Portal/MapFreeze` | Fige la carte pendant l'épreuve, et la remet en route |
| `Portal/StarDimension` | Le déroulement de l'épreuve, les cachettes du monstre, les torii qui bugent |
| `Portal/DimensionMirror` | Le reflet sous le sol miroir |
| `Portal/ErrorPanels` | Les panneaux d'erreur (police Share Tech Mono, dans `Resources/Fonts`) |
| `Portal/CorruptedMonster` | Le monstre corrompu et sa course buguée |
| Shader `Archery/Glitch` | L'apparence buguée du monstre |
| Shader `Archery/StarSprite` | Les points de lumière ronds des étoiles, nébuleuses et galaxies |
| `Editor/StarPortalBaker` | Met le portail en place, pose l'ouverture et les runes |
| `Editor/StarDimensionBaker` | Construit le décor de la dimension |

## Confort

Les bugs restent sur les objets (panneaux, monstre, piliers), jamais sur toute la vue ; rien ne clignote plus de 2 à 3 fois par seconde, et les panneaux s'ouvrent en s'étirant. Ils restent entre 2,2 et 6 m du joueur, et jamais devant le monstre.
