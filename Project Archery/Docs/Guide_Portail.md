# Guide : le portail des étoiles (easter egg)

L'easter egg du GDD (section 23), codé le 18 octobre, **pas encore testé dans le casque**.

## Ce que fait le joueur

1. Sur la terrasse de droite de la carte, l'anneau de pierre (`statue_ring`) est entouré de 5 obélisques (`statue_obelisk`). Chacun porte une **rune violette**, à peine visible, sur sa face qui ne regarde pas l'anneau : une sur deux à hauteur de poitrine, les autres au pied.
2. Il les **touche de la main** (sans casque : il les regarde de près et appuie sur **E**). La rune s'éveille quand la main approche, et s'allume quand on la touche. Hors d'une vague, elle reste froide (un message l'explique, au plus toutes les 8 s).
3. Les 5 runes allumées **pendant une même vague** ouvrent le portail : un voile sombre bordé de violet, plein d'étoiles, avec de petits bugs rouges. En fin de vague, s'il n'est pas entré, tout s'éteint.
4. Il **traverse l'anneau** : fondu au noir, la carte se fige (vague, chrono, ennemis, projectiles, tourelles, renforts, coffres, cristaux), et il arrive dans la dimension avec son **seul arc de départ, sans aucune amélioration** (il les retrouve au retour). Une seule tentative par partie.
5. **Dans la dimension** : ciel noir criblé d'étoiles rondes, 4 nébuleuses qui traversent le ciel d'un horizon à l'autre (dont la Voie lactée), 6 grandes galaxies spirales, ni nuages ni aurore. Un sol miroir, et des piliers vermillon plantés en désordre, de tailles variées, certains penchés : au-dessus du sol, seulement les deux piliers ; **le haut des torii n'existe que dans le reflet**. 10 s pour découvrir le décor pendant que la musique monte, puis **30 s à tenir** contre le monstre corrompu. Des panneaux d'erreur rouges s'ouvrent presque chaque seconde ; le temps restant n'est affiché que sur eux.
6. **Le monstre** : un ennemi du jeu « cassé ». Il apparaît loin, **dans le champ de vision** du joueur, caché derrière un pilier (on l'entend grésiller). Puis il **court très vite** jusqu'à un autre pilier, toujours dans le champ de vision du joueur et de plus en plus près de lui. Sa course bugue : avec l'animation de course de l'ennemi, il court parfois à l'envers, de côté, penché, la tête en bas ou en tournant sur lui-même, change de pose par à-coups, se fige une fraction de seconde et saute quelques images. Tout près, il fonce sur le joueur. Une flèche, tirée ou au corps à corps, le tue : il éclate en pixels, et le suivant prend l'apparence de l'ennemi suivant de la liste, loin, devant le joueur (jamais dans son dos).
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
- Pour tout refaire depuis zéro : menu **Archery > Portail des étoiles > Reconstruire le décor de la dimension** (les retouches à la main sur le décor sont perdues ; les matériaux et le préfab sont gardés).

## Tester

- Dans l'éditeur, en jeu, **P** allume les 5 runes (le portail s'ouvre), puis **P** encore fait entrer dans la dimension.

## Régler

| Où | Réglage | Valeur |
|---|---|---|
| `Portail des étoiles` (Prototype_Tir) | *Touch Distance*, *Rune Size*, *Rune Color* | 0,25 m, 0,24 m, violet |
| | *Score Bonus* | 2 500 points |
| | *Fade Time* | 1 s |
| `Dimension` (Dimension_Etoiles) | *Intro Time*, *Survive Time* | 10 s, 30 s |
| | *Post Range* | 45 m (piliers qui servent de cachettes) |
| | *Monster Models* | l'ordre des apparences du monstre |
| | *Respawn Delay* | 1,2 s |
| | *Fake Errors* | les fausses erreurs des panneaux |
| | sons et musique | sons Kenney *Interface Sounds*, `music_boss` |

Le monstre suit la vitesse des ennemis de la difficulté : il se cache moins longtemps (1,5 s en Normal) et court plus vite (9 m/s en Normal) en Difficile et en Impossible. S'il n'y a aucun pilier plus proche dans le champ de vision, il attend que le joueur se tourne ; au bout de 2 s, il prend un pilier au bord du champ de vision ; à moins de 8 m, il fonce.

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
