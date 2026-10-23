# THUNK VR

Jeu de tir à l'arc en réalité virtuelle pour Meta Quest 3, réalisé avec Unity pour le cours ST2OOS (UnityXR).

On défend une tour au milieu d'une clairière contre des vagues de monstres de plus en plus fortes. Le jeu mélange trois genres :
- **survivor** : tenir le plus longtemps possible ;
- **roguelike** : des améliorations tirées au sort entre les vagues, qui se cumulent sans limite ;
- **tower defense** : la tour, ses barricades et son brasero ; des cristaux à récolter sur la carte, et 12 socles où construire au marteau barricades, pièges à piques, tours de givre et balistes, à 3 niveaux chacun.

Inspirations : *Megabonk*, et *Longbow* dans *The Lab*.

## Fonctionnalités

| Consigne du projet | Dans le jeu |
|---|---|
| Interactions XR, geste naturel | Prendre l'arc, attraper une flèche dans son dos, l'encocher, tendre la corde, lâcher. Sortir une grenade de sa poche et la lancer. Soulever le couvercle d'un coffre, attraper une orbe, tremper une flèche dans le feu, frapper au corps à corps, construire au marteau, toucher des runes. |
| Trajectoire physique | Flèches soumises à la gravité, portée selon la tension et la qualité du tir. |
| Plusieurs arcs et flèches | Un menu des armes au début de la partie : arc de chasse, arcs court, puissant et runique, débloqués par des succès ; armes secrètes : les Akimbow (arbalètes jumelles), le katana et le gantelet, l'arc cosmique (laser), l'arc légendaire (en 3 fragments). Flèches spéciales (foudre, glace, explosion, multitir, déluge, ricochet…), flèches enflammées, grenade à flèches. |
| Améliorations entre les niveaux | Boutique entre les vagues : 23 améliorations de trois raretés, sans limite d'achat (dont 2 pour la grenade). |
| Zones de score, cibles mobiles, score en direct | Tête et corps des ennemis (et points faibles du boss) ; cibles d'entraînement, dont une mobile ; score, combo et argent sur la montre. |
| Niveaux de difficulté | Facile, Normal, Difficile, Impossible, chacun avec son ciel (jour, coucher de soleil, nuit, lune de sang). |
| Mode entraînement | Cibles et mannequins au menu et pendant les pauses. |
| Menu, paramètres, affichage | Menu dans le décor, tutoriel en vidéos, volumes, rotation au joystick, vignette de confort ; montre au poignet ; en cours de partie, le bouton menu de la manette gauche ouvre le menu et met le jeu en pause. |
| Clavier virtuel, sauvegarde, classement | Nom saisi au clavier virtuel d'XRI ; classement et paramètres en JSON. |
| Sons 3D | Sons spatialisés (arc, impacts, ennemis), ambiance jour et nuit, musiques. |

Aussi : un timing à l'anneau (rouge, orange, vert, vert pastel) qui récompense le tir parfait, une aide à la visée qui montre la trajectoire selon la difficulté, des ennemis volants et tireurs, un boss différent à chaque vague et un boss final à la vague 6 (MK, un dragon, dans son antre), des coffres dans la forêt, la course en balançant les bras, le slide et le saut, un mode infini. Deux secrets à trouver sur la carte : le **portail des étoiles** (5 runes, une dimension où survivre, l'arc cosmique en récompense) et le **toit cyberpunk** (des éclats de miroir derrière les cascades, la porte de l'antre qui bugue, les Akimbow en récompense).

## Commandes

| Action | Geste |
|---|---|
| Prendre l'arc | Bouton de poignée, n'importe quelle main |
| Prendre une flèche | Main derrière l'épaule, poignée |
| Tirer | Approcher la flèche de la corde, reculer la main, lâcher la poignée. Lâcher dans le vert de l'anneau donne un tir parfait. |
| Marcher | Joystick gauche (la direction suit le regard) |
| Courir | Balancer les deux bras en marchant |
| Glisser | A ou X en courant |
| Tourner | Joystick droit (par crans, désactivable) |
| Menus et boutique | Viser avec la main libre, gâchette |
| Lancer une vague | Tirer dans le gong |
| Ouvrir un coffre | Viser le coffre et appuyer sur la poignée (ou l'attraper de près), puis prendre une orbe |
| Récolter des cristaux | Viser le gisement et appuyer sur la poignée |
| Construire | Viser un socle et appuyer sur la poignée, choisir le bâtiment, puis frapper le plan 3 fois au marteau |
| Revenir en haut de la tour | Rester 1 s sur la bande lumineuse au pied de la tour, ou toucher le bouton « Tour » sous la montre avec l'autre main |
| Affronter le boss final | Vague 6 : marcher jusqu'à la grande porte au bout de la voie du milieu et passer le seuil |
| Après la victoire | Tirer dans la cible « Continuer » (mode infini) ou « Terminer » |
| Flèche enflammée | Tremper la pointe dans le brasero, en haut de la tour |
| Coup au corps à corps | Frapper un ennemi avec la flèche tenue en main |
| Grenade à flèches | Main vide à la hanche, comme pour fouiller sa poche, poignée : une grenade sort ; la lancer. Elle éclate au premier contact en une gerbe de flèches (recharge sur la montre). La toucher d'une flèche en vol : « MEGA-BOMBA », trois fois plus de flèches |
| Akimbow (arme secrète) | Prendre chaque arbalète qui flotte à côté de soi, gâchette pour tirer |
| Katana et gantelet (arme secrète) | Prendre le katana, le gantelet est porté sur la main de l'arc. Une vraie taille rapide devant soi ouvre une faille dans la matrice ; un coup de poing du gantelet dedans la lance vers l'ennemi regardé (plus on frappe vite, meilleur est le tir). La lame frappe aussi au corps à corps et fait sonner le gong |
| Menu en cours de partie | Bouton menu de la manette gauche (Tab au clavier) : menu devant soi, jeu en pause ; encore une fois pour reprendre |

**Raccourcis de test** (dans l'éditeur et les builds *Development*) : la liste complète est dans `Project Archery/Docs/Demo_Vendredi.md` (partie 4). Les principaux : **N** lance la vague, appelle le boss ou le tue (à la vague 6 : entre dans l'arène, passe l'intro, tue le dragon), **F** (pour la démo) donne 2 exemplaires de chaque amélioration et des tourelles niveau 2 sur tous les emplacements, **K** fait mourir le joueur, **M** donne 500 pièces d'or, **B** 50 matériaux, **C** pose un coffre, **V** un gisement de cristaux, **H** termine la construction en cours, **1 à 4** choisissent la difficulté, **5 à 9, 0, ), =** équipent une arme, **U** débloque tous les succès, **P** et **G** ouvrent les deux secrets, **O** équipe le katana. Après la victoire, **I** continue en mode infini et **T** termine la partie. Les touches suivent la position physique du clavier QWERTY : sur un clavier AZERTY, **M** se trouve sur la touche **,**.

## Installation

1. Installer **Unity 6000.5.10f1** avec Unity Hub, et le module *Windows Build Support*.
2. Ouvrir le dossier `Project Archery` avec Unity Hub. Les paquets (XR Interaction Toolkit 3.5.1, OpenXR, URP 17.5, AI Navigation, Animation Rigging) s'installent tout seuls.
3. Ouvrir la scène `Assets/_Project/Scenes/Prototype_Tir.unity`.

**Avec le casque** (Quest 3 ou 3S) :
1. Installer l'application **Meta Quest Link** sur le PC et brancher le casque (câble Link ou Air Link).
2. Dans l'application Meta Quest Link : *Paramètres > Général* > définir Meta Quest Link comme **runtime OpenXR actif**.
3. Dans le casque, activer Quest Link, puis lancer *Play* dans Unity.

**Construire le jeu** : *File > Build Profiles* > *Windows*, vérifier que la scène est dans la liste, puis *Build*.

Sans casque, `Desktop Controls` prend le relais et le jeu se joue au clavier et à la souris : ZQSD ou les flèches pour avancer, la souris pour regarder (Échap libère le curseur, un clic gauche le reprend), Maj pour courir, Espace pour sauter, **clic gauche maintenu pour tendre l'arc** (relâcher tire), **Tab** pour le menu en cours de partie, et **E pour interagir** avec ce que vise le centre de l'écran (récolter, ouvrir un coffre, prendre une orbe, construire, frapper au marteau, cliquer les boutons de la boutique et des menus). La grenade ne se lance qu'avec le casque.

**Récupérer le projet** (par exemple sur un nouveau PC) : `git clone git@github.com:AntoninCro/Project_Archery.git`, puis ouvrir le sous-dossier `Project Archery` avec Unity Hub. La première ouverture est longue : Unity recrée le dossier `Library/`.

## Organisation du projet

```
Project Archery/
├── Assets/_Project/
│   ├── Scripts/     code du jeu (arc, ennemis, vagues, boutique, coffres, déplacements…), un dossier par thème
│   ├── Data/        réglages en ScriptableObjects : arcs, ennemis, vagues, difficultés, boutique
│   ├── Prefabs/, Scenes/, Materials/, Shaders/, Art/, Audio/
├── Docs/
│   ├── GDD.md       document de conception : règles, chiffres, planning
│   ├── Reprise_Projet.md  état du projet au 23 octobre et suite (présentation, tests ; refonte du katana faite)
│   └── Guide_*.md   pas-à-pas de montage de chaque partie dans Unity
└── Tools/           vérifications sans ouvrir Unity : compilation des scripts, syntaxe des shaders
```

Les fichiers de sauvegarde (`leaderboard.json`, `settings.json`) sont dans `%USERPROFILE%\AppData\LocalLow\DefaultCompany\THUNK VR`.

## Crédits

Inventaire complet, avec les licences et ce qu'elles demandent : `Project Archery/Docs/Credits_Licences.md`.

- **Modèles 3D (Asset Store de Unity, EULA standard)** :
  - bestiaire et boss : *Mini Legion Footman / Grunt / Lich PBR HP Polyart*, *RPG Monster DUO / Buddies / Couple / Partners PBR Polyart*, *Monster Minion Survivor PBR Polyart*, *Wizard PolyArt* (le mage) ; MK, le dragon rouge : *Four Evil Dragons PBR* (Dungeon Mason) ;
  - arcs et flèches : *Low Poly Style Bows & Quivers* (Dimasjk Studio) et *Easy Weapons* ; décor de l'antre : autres packs *Low Poly Style* de Dimasjk Studio ;
  - marteau de construction : *Stylized Mechanized Hammer* (AgeSoft) ; coffre animé : *Animated PBR Chest Demo* ; chevalier : *Toon RTS Units – Demo*.
- **Kenney** (www.kenney.nl), licence CC0 : *Nature Kit*, *Castle Kit*, *Tower Defense Kit*, *UI Pack*, *UI Pack: RPG Extension*, *Fantasy UI Borders*, *Game Icons*, *City Kit (Commercial)*, sons *Impact Sounds*, *Interface Sounds* et *RPG Audio*.
- **Quaternius**, licence CC0 : le gant low-poly des mains, la porte de l'antre (*Modular Dungeon*, coupée en double porte par le jeu), les objets du toit cyberpunk (*Cyberpunk Game Kit*, via Poly Pizza) et la bombe de la grenade à flèches (*Bomb*, via Poly Pizza).
- **Pichuliru**, licence CC0 : le katana (*Katana*, via Poly Pizza).
- **Toit cyberpunk** : les arbalètes, modèle *Sci fi crossbow* de **oarfish** (https://sketchfab.com/3d-models/sci-fi-crossbow-b2890154cfdf4c1caf2780c50280b26b), licence **CC-BY 4.0**, modifié (converti en OBJ, recentré, réduit, nouveau matériau).
- **Arc légendaire** : le modèle *Bowblade – DAE WeaponCraft – WoW Styled Bow* de **Tijmen Matthys** (https://sketchfab.com/3d-models/bowblade-dae-weaponcraft-wow-styled-bow-a57fb07e6b254947b1e036f5aa7b7cec), licence **CC-BY 4.0**, modifié (converti en OBJ, corde retirée, recentré et mis à l'échelle).
- **Gantelet du katana** : le modèle *Energy Gauntlet - Low Poly (Free Download)* de **ENOMIC** (https://sketchfab.com/3d-models/energy-gauntlet-low-poly-free-download-708a936c5bdc4537842f976e51cbb1a9), licence **CC-BY 4.0**, modifié (remis à l'échelle et en main gauche, matériaux refaits).
- **Sons du katana** (Freesound, CC0) : *Clean fast Swoosh* de Danjocross, *Lightsaber Whoosh* de h2p34.
- **Arc cosmique** : le modèle *Enchanted Bow* de **Zsky** (https://poly.pizza/m/9j2Tcybxue), licence **CC-BY 3.0**, modifié (corde retirée, matériaux refaits) ; la nébuleuse *Purple Nebula 1* des *Seamless Space Backgrounds* de **Screaming Brain Studios**, CC0 ; les sons *Sci-fi Weapon Charging 01* de **Tim_Verberne** et *Ion Cannon* de **qubodup** (freesound.org), CC0, recoupés.
- **Polices**, licence SIL Open Font License 1.1 : *Cinzel* et *Cinzel Decorative* (Natanael Gama) ; *Share Tech Mono* (Carrois Type Design, Ralph du Carrois), pour les panneaux d'erreur du portail des étoiles.
- **Musiques du boss final et de la dimension des étoiles** : « Mama Mia Marchello » et « Sad Nostalgie », composées par un ami (crédit exact à confirmer).
- **Autres sons, ambiances et musiques** : provisoires, générés par script pour le prototype.
- **Unity** : XR Interaction Toolkit et ses exemples, XR Hands, TextMesh Pro.
- Développement assisté par IA (Claude, d'Anthropic) pour le code et la documentation ; les détails sont dans le rapport.

## Équipe

- Antonin Croquette : lead developer.
- Jude Aybalen : scope originel en partie + implémentation des classes.
- Tchoffo Manfouo Dovin Vilson : scope originel en partie + implémentation des grenades + test.
- Merleau Gezo : scope originel en partie + test.
