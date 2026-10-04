# Project Archery — Document de conception (GDD)

Version 0.1 · 2 octobre 2026 · document vivant, mis à jour au fil du projet.

## 1. Le jeu en bref

Un jeu de tir à l'arc en réalité virtuelle qui mélange trois genres :

- **Survivor** : survivre à des vagues d'ennemis de plus en plus difficiles.
- **Roguelike** : chaque partie repart de zéro, avec des améliorations tirées au sort, de différentes raretés.
- **Tower defense** : protéger une tour, du haut de laquelle on tire mieux.

Inspirations : *Megabonk* (survivor, style low-poly) et *Longbow* dans *The Lab* (défendre une position à l'arc, en VR).

## 2. Cadre du projet

| | |
|---|---|
| Rendu | vendredi 23 octobre 2026 |
| Équipe | développement en solo (groupe de 4 sur le papier) |
| Temps disponible | 10 à 20 h par semaine, soit 30 à 60 h jusqu'au rendu |
| Casque | Meta Quest 3 / 3S, démo branchée à un PC (Link ou Air Link) |
| Moteur | Unity 6000.5.10f1, URP, XR Interaction Toolkit 3.5.1, OpenXR |
| Graphismes | low-poly, packs gratuits (Quaternius, Kenney, licence CC0) |
| Consignes | adaptables : par exemple, les points du headshot remplacent ceux du centre de la cible |

## 3. Déroulement d'une partie

1. **Menu** : le joueur est en haut de la tour. Des cibles d'entraînement permettent de s'échauffer. Il choisit la difficulté (le ciel change en direct) et lance la partie.
2. **Vague** : un chrono tourne et les ennemis sortent de la forêt. Quand le chrono atteint zéro, la vague est finie et les ennemis restants s'enfuient.
3. **Pause** : la boutique s'ouvre en haut de la tour et les cibles d'entraînement réapparaissent. Les PV du joueur sont restaurés, pas ceux de la tour. On tire dans un gong pour lancer la vague suivante.
4. **Boss** aux vagues 5 et 10, puis toutes les 5 vagues. Une vague de boss ne se termine qu'à la mort du boss : le chrono arrête seulement les renforts.
5. **Victoire** après la vague 10. Le joueur peut ensuite continuer en mode infini pour le score.
6. **Défaite** quand les PV du joueur tombent à 0. La destruction de la tour ne termine pas la partie, mais pénalise fortement (voir section 9).
7. **Fin de partie** : résumé, saisie du nom au clavier virtuel, classement, retour au menu.

Durée des vagues : 30 s à la vague 1, puis 5 s de plus à chaque vague (75 s à la vague 10). Une partie de 10 vagues dure environ 9 minutes de combat et 5 minutes de pauses, soit environ 15 minutes.

## 4. Le tir à l'arc

### 4.1 Le geste

1. Prendre l'arc avec le bouton de poignée, dans n'importe quelle main.
2. Attraper une flèche au-dessus de l'épaule, dans une zone invisible derrière la tête qui suit la rotation horizontale du casque.
3. Encocher : approcher l'arrière de la flèche de la corde (à 20 cm près), ou poser la flèche sur le repose-flèche ; elle s'accroche à la corde.
4. Tendre : reculer la main. La puissance dépend de la distance de tirage (main à environ 50 cm de l'arc à tension maximale, selon l'arc). La vibration et le grincement augmentent avec la tension.
5. Lâcher le bouton : la flèche part, propulsée par le moteur physique.

### 4.2 L'anneau de timing

- Il apparaît quand la corde est tendue au maximum, sur l'arc, près de la main.
- Ses bandes, de l'extérieur vers le centre : rouge, orange, vert, orange, rouge. Trois couleurs, comme un feu tricolore : rouge pour un tir raté, orange pour un bon tir, vert pour un tir parfait. Aux premiers tests, l'ancien doré du tir parfait se confondait avec du jaune.
- Un cercle d'approche rétrécit du bord vers le centre (en 1,2 s avec l'arc de départ). La bande où il se trouve au moment du lâcher donne la qualité du tir.
- Une vibration et un « ding » marquent l'entrée dans le vert, pour le sentir sans regarder l'anneau.
- Si le cercle atteint le centre sans tir, l'anneau recommence.
- Un tir lâché avant la tension maximale est faible et sans bonus.

| Qualité | Bande | Vitesse | Portée | Dégâts | Points |
|---|---|---|---|---|---|
| Parfait | vert | ×1,5 | ×2,25 | ×2 | ×2 |
| Bon | orange | ×1,15 | ×1,3 | ×1,25 | ×1,25 |
| Raté | rouge | ×0,6 | ×0,36 | ×0,75 | ×0,5 |
| Sans anneau (tension incomplète) | — | ×0,6 | ×0,36 | ×0,75 | ×0,5 |

La portée varie comme le carré de la vitesse : un tir parfait va environ 6 fois plus loin qu'un tir raté. Ces valeurs se règlent dans `Data/ShotTuning`, et la largeur des bandes dans chaque arc (`Data/Bows`). Un tir parfait déclenche aussi un effet visuel et sonore.

### 4.3 L'aide à la visée

Pendant la tension, une ligne droite part de la pointe de la flèche, dans son axe. Elle ne montre pas la chute de la flèche : elle aide à viser sans tout faire à la place du joueur. Elle s'arrête au premier obstacle et devient plus visible à mesure que la corde se tend. Elle est disponible en Facile et Normal, et retirée en Difficile et Impossible (section 11).

### 4.4 La flèche

- `Rigidbody` soumis à la gravité, avec collisions continues ; la flèche s'oriente dans le sens de son vol.
- L'impact est détecté par un rayon entre deux positions successives, ce qui reste fiable à grande vitesse.
- Elle se plante dans ce qu'elle touche, puis disparaît au bout de quelques secondes.
- Les flèches sont illimitées. Elles sont réutilisées (pool d'objets) au lieu d'être recréées à chaque tir.

### 4.5 Coup de flèche au corps à corps (fin de projet)

Idée à ajouter quand tout le reste est en place.

- On frappe un ennemi avec la flèche tenue en main : il subit des dégâts équivalents à un tir orange.
- La flèche reste plantée dans l'ennemi ; il faut en reprendre une dans le dos.
- C'est surtout utile en début de partie. L'attaque ne profite pas des flèches spéciales (multitir…), elle devient donc moins intéressante ensuite.

## 5. Les arcs

Un arc de départ, puis des arcs plus puissants à acheter en boutique.

| Arc | Vitesse de la flèche | Dégâts | Anneau | Particularité | Prix | En boutique dès |
|---|---|---|---|---|---|---|
| Arc de chasse | 35 m/s | 10 | 1,2 s | équilibré, arc de départ | — | — |
| Arc composite | 40 m/s | 13 | 1,0 s | anneau rapide, bandes larges | 120 | vague 2 |
| Arc long | 50 m/s | 20 | 1,5 s | puissant mais lent à charger | 200 | vague 4 |
| Arc runique | 55 m/s | 24 | 1,2 s | flèches qui traversent un ennemi | 350 | vague 7 |

## 6. Améliorations et flèches spéciales

### 6.1 Raretés

| Rareté | Couleur | Rôle | Vagues 1–3 | Vagues 4–6 | Vagues 7–10 | Mode infini |
|---|---|---|---|---|---|---|
| Commune | blanc | statistiques | 80 % | 65 % | 50 % | 40 % |
| Rare | bleu | effets qui se déclenchent parfois | 18 % | 30 % | 40 % | 45 % |
| Légendaire | doré | effets puissants ou uniques | 2 % | 5 % | 10 % | 15 % |

### 6.2 Liste de départ

**Aucune limite d'achat** : chaque amélioration peut être prise autant de fois qu'on veut, et tout se cumule.

- **Les bonus** s'additionnent : dégâts, vitesse, PV, etc.
- **Les chances** (foudre, explosion, glace) s'additionnent jusqu'à 100 %. Au-delà, chaque exemplaire rend l'effet plus fort (tableau ci-dessous).
- **Les nombres de flèches** (multitir, tir écho, déluge) et le perçage sont des moyennes. Chaque exemplaire en ajoute une part : avec 150 %, on a 1 flèche en plus à coup sûr, et 50 % de chance d'en avoir une deuxième. Il n'y a pas de limite. Seule une sécurité, pour que le jeu reste fluide, arrête d'ajouter des flèches au-delà de 250 en vol en même temps.
- **Ils s'enchaînent** : le multitir ajoute des flèches à la volée, le tir écho répète toute la volée, et le déluge divise en vol chaque flèche, celles du multitir et de l'écho comprises. Les flèches nées d'une division ne se divisent pas à leur tour.
- **Chaque flèche en plus est une vraie flèche** : elle tire au sort ses propres effets (foudre, explosion, glace, perçage) et profite de l'auto-visée et du ricochet. Elle ne casse pas le combo quand elle rate.

Au-delà de 100 % de chance (« surplus » : 20 % de surplus avec 120 % de chance) :

| Effet | Ce qui augmente |
|---|---|
| Foudre | dégâts de l'éclair ×(1 + surplus) ; ralentissement plus fort, et plus long : ×(1 + surplus/2) |
| Explosion | dégâts ×(1 + surplus), rayon ×(1 + surplus/4) |
| Glace | ralentissement plus fort, durée ×(1 + surplus/2), rayon ×(1 + surplus/4) |

**Communes**

- Dégâts : +10 %.
- Vitesse des flèches : +8 %.
- Charge rapide : l'anneau va 15 % plus vite et toutes ses bandes s'épaississent un peu, pour que le parfait reste faisable.
- Précision : bande verte (tir parfait) +15 %.
- Vitalité : +15 PV max.
- Butin : le taux de conversion des points en or passe de 25 % à 30 % (+5 points par exemplaire).
- Chasseur de têtes : +25 % de dégâts à la tête.
- Chance : les cartes rares et légendaires sortent 25 % plus souvent en boutique. Avec beaucoup d'exemplaires, les cartes communes disparaissent.

**Rares** (se déclenchent au hasard)

- Multitir : +50 % de chance de tirer une flèche en plus, en éventail. Avec 2 exemplaires, une flèche en plus à chaque tir ; avec 3, une flèche en plus et 50 % de chance d'une deuxième ; etc.
- Flèche de foudre : chaque flèche a 20 % de chance d'appeler un éclair qui blesse la cible (75 % des dégâts de la flèche) et la ralentit de 40 % pendant 2 s.
- Perçage : chaque flèche a 25 % de chance de traverser un ennemi ; au-delà de 100 %, elle en traverse plusieurs. Chaque ennemi traversé enlève 20 % des dégâts.
- Vampirisme : chaque headshot rend 2 PV.
- Tir écho : 25 % de chance que la volée se répète 0,25 s après, à la même puissance, multitir compris ; au-delà de 100 %, plusieurs échos, à 0,25 s d'intervalle.
- Flèche de glace : chaque flèche a 25 % de chance de laisser au sol une zone de glace (3 m, 5 s) qui ralentit de 50 % les ennemis au sol.

**Légendaires**

- Déluge : en vol, environ 0,2 s après son départ, chaque flèche a 50 % de chance de se diviser en deux. Avec 2 exemplaires, toujours ; avec 3, en deux, et 50 % de chance en trois ; etc.
- Chaîne d'éclairs : la foudre rebondit sur 3 ennemis proches de plus par exemplaire. Sans flèche de foudre, 20 % des flèches appellent l'éclair.
- Flèche explosive : chaque flèche a 25 % de chance d'exploser et de toucher tous les ennemis autour (3,5 m).
- Auto-visée : les flèches dévient légèrement (30° par seconde et par exemplaire) vers l'ennemi le plus proche, s'il est à moins de 12 m devant elles.
- Tir ricochet : après avoir touché un ennemi, la flèche rebondit vers un autre ennemi proche (un rebond par exemplaire, 20 % de dégâts en moins par rebond).

**Flèches enflammées** : avec le brasero de la tour (voir section 9), on trempe une flèche dans le feu ; la cible brûle pendant 3 s.

## 7. La boutique

- Elle s'ouvre entre les vagues, en haut de la tour : un panneau à gauche du joueur. On vise une carte avec le rayon de la main libre et on achète avec la gâchette.
- Elle propose 4 améliorations tirées au sort selon les raretés, le prochain arc et les services de la tour.
- Relancer les offres coûte 10, puis 5 de plus à chaque relance (le coût revient à 10 à chaque pause).
- Prix de base : commune 25, rare 60, légendaire 140 ; +8 % par vague.
- Services de la tour : réparation (+25 % des PV, 40), reconstruction après destruction (250), barricades (60), brasero (100, une seule fois).
- **Mode infini** : tous les prix de la boutique (améliorations, arc, tour, relance) sont multipliés par 1,2 à chaque vague, en se cumulant. Avec la courbe des ennemis (section 11), cela compense les achats sans limite : le mode infini finit toujours par déborder le joueur.

| Boutique après la vague | 1 | 5 | 10 | 11 | 15 | 20 |
|---|---|---|---|---|---|---|
| Commune | 25 | 35 | 45 | 55 | 130 | 390 |
| Rare | 60 | 80 | 105 | 130 | 315 | 935 |
| Légendaire | 140 | 185 | 240 | 300 | 740 | 2 185 |

## 8. Score et argent

**Points**

- Chaque touche : 5 points au corps, 10 à la tête.
- Chaque ennemi tué : sa valeur de base (section 10), doublée s'il meurt d'un tir à la tête.
- Ces points sont multipliés par la qualité du tir (section 4.2), la distance, le combo et la difficulté (section 11).
- Fin de vague : 50 points × numéro de la vague.

| Multiplicateur | Règle |
|---|---|
| Distance | +1 % par mètre au-delà de 10 m, jusqu'à +50 % |
| Combo | +10 % par touche consécutive, jusqu'à ×2 ; retombe à zéro quand une flèche ne touche aucun ennemi |

**Argent** : 25 % des points gagnés (l'amélioration Butin augmente ce taux), divisé par deux tant que la tour est détruite.

## 9. La tour

- 1000 PV, qui ne remontent pas tout seuls : il faut la réparer en boutique.
- Un téléporteur au pied de la tour mène au sommet, avec un fondu au noir. On reste debout 1 s sur un cercle lumineux ; on arrive en haut tourné vers les ennemis. Au sol, on rejoint le cercle avec le joystick. Pour redescendre, on marche simplement dans le vide (un cercle au sommet reste possible).
- À la fin d'une vague, si le joueur n'est pas en haut, le panneau de la boutique vient à côté de lui.
- Le sommet est une plateforme à environ 6 m, avec une rambarde, la boutique, le gong et l'emplacement du brasero.
- À 0 PV, la tour s'effondre. Le joueur qui était en haut est téléporté au sol, devant le cercle du bas. Le téléporteur ne fonctionne plus et l'argent gagné est divisé par deux jusqu'à la reconstruction.
- Barricades : des murs aux entrées de la clairière, qui bloquent les ennemis au sol jusqu'à ce qu'ils les détruisent.
- Brasero : on y trempe une flèche pour l'enflammer.

## 10. Les ennemis

| Ennemi | Déplacement | Cible | PV | Vitesse | Attaque | Valeur | Dès la vague |
|---|---|---|---|---|---|---|---|
| Rampant | au sol | la tour, ou le joueur s'il est à moins de 8 m | 30 | 2,5 m/s | corps à corps | 10 | 1 |
| Volant | dans les airs | le joueur | 20 | 5 m/s | attaque en piqué | 15 | 2 |
| Tireur | au sol, s'arrête vers 25 m | le joueur | 40 | 2 m/s | projectile lent, qu'on peut esquiver ou abattre | 20 | 4 |
| Boss | au sol | la tour | 1500 | 1 m/s | frappe la tour, appelle des Rampants | 250 | 5, 10, 15… |

- Zones de touche : la tête et le corps. Le boss a en plus des points faibles lumineux (dégâts ×3).
- Le boss est un chevalier géant (×2,2) à l'armure rouge sombre, avec 3 points faibles cyan qui pulsent : la poitrine et les deux épaules. Il arrive 4 s après le début de la vague, avec un cor et des tambours. Il appelle 2 Rampants toutes les 14 s, et une barre de PV flotte au-dessus de lui. Comme les autres ennemis, il suit la courbe de difficulté (section 11) : le boss de la vague 10 est plus coriace que celui de la vague 5. Une vague de boss compte 40 % de Rampants ordinaires en moins.
- Les ennemis sortent de la forêt par 4 chemins autour de la clairière.
- Chaque vague dispose d'un budget qui augmente de vague en vague. Chaque ennemi a un coût, et le budget est dépensé tout au long du chrono.
- Les PV, les dégâts et le nombre des ennemis suivent une courbe exponentielle (section 11). Le tableau ci-dessus donne leurs valeurs à la vague 1, en Normal.
- Pour que le jeu reste fluide dans le casque : 500 ennemis au plus par vague, et 40 en vie en même temps (12 à la vague 1 en Normal ; ce maximum grandit avec la difficulté et les vagues). À la fin du chrono, ceux qui ne sont pas encore sortis ne viennent plus.

## 11. Les difficultés

| | Facile | Normal | Difficile | Impossible |
|---|---|---|---|---|
| Ciel | soleil au zénith | coucher de soleil | nuit, lune claire | ciel et lune rouges |
| PV des ennemis | ×0,7 | ×1 | ×1,4 | ×2 |
| Vitesse des ennemis | ×0,8 | ×1 | ×1,15 | ×1,3 |
| Taille de la tête (zone de touche) | ×1,3 | ×1 | ×0,85 | ×0,75 |
| Dégâts subis | ×0,6 | ×1 | ×1,3 | ×1,8 |
| Nombre d'ennemis | ×0,75 | ×1 | ×1,25 | ×1,6 |
| PV des ennemis, à chaque vague | ×1,06 | ×1,08 | ×1,11 | ×1,14 |
| Dégâts subis, à chaque vague | ×1,03 | ×1,04 | ×1,06 | ×1,08 |
| Nombre d'ennemis, à chaque vague | ×1,04 | ×1,05 | ×1,07 | ×1,09 |
| Largeur de la bande verte (parfait) | ×1,3 | ×1 | ×0,85 | ×0,7 |
| Aide à la visée | oui | oui | non | non |
| Score | ×0,75 | ×1 | ×1,5 | ×2 |

La nuit, les yeux des ennemis brillent et une lanterne éclaire la tour, pour que le jeu reste lisible.

**Courbe de difficulté exponentielle.** Les améliorations n'ont pas de limite (section 6.2) : pour compenser, chaque vague multiplie encore les PV, les dégâts et le nombre des ennemis (lignes « à chaque vague »). La courbe monte plus vite en Difficile et en Impossible. En mode infini, elle s'accélère encore, quelle que soit la difficulté : chaque vague après la 10e multiplie en plus les PV par 1,12, les dégâts par 1,06 et le nombre d'ennemis par 1,08. Les prix de la boutique montent aussi (section 7).

| PV d'un Rampant (dégâts d'un coup) | Vague 1 | Vague 5 | Vague 10 | Vague 15 | Vague 20 |
|---|---|---|---|---|---|
| Facile | 21 (6) | 27 (7) | 35 (8) | 84 (12) | 197 (19) |
| Normal | 30 (10) | 41 (12) | 60 (14) | 155 (23) | 402 (38) |
| Difficile | 42 (13) | 64 (16) | 107 (22) | 319 (39) | 947 (70) |
| Impossible | 60 (18) | 101 (24) | 195 (36) | 662 (71) | 2 247 (139) |

En Normal, une vague sans boss compte 6 ennemis à la vague 1, 51 à la vague 10 et 343 à la vague 20 (dans la limite de 40 en vie en même temps, section 10). Ces réglages se trouvent dans les assets `Data/Difficulties` (courbe de chaque difficulté) et `Data/Waves/WaveSettings` (accélération du mode infini).

On choisit la difficulté avant la première vague, en tirant dans l'un des 4 panneaux devant la tour ; le ciel passe au nouveau ciel en quelques secondes. Elle est ensuite verrouillée, et elle est gardée quand la partie recommence.

## 12. Les déplacements

- Le joueur se déplace physiquement dans sa pièce.
- Course aux bras : maintenir une gâchette et balancer les bras. La vitesse suit celle des mains (5 m/s au maximum). La direction est la moyenne de l'orientation des deux manettes.
- Slide : un bouton dédié (A ou X) pendant la course. On garde l'élan, avec peu de frottement, pendant environ 1,5 s, les mains libres pour tirer.
- Rotation par crans de 45° au joystick, désactivable : pratique avec le câble Link.
- Vignette de confort pendant le slide, réglable dans les paramètres.
- Le téléporteur de la tour.

## 13. Les coffres

- 1 ou 2 coffres par vague, à des emplacements aléatoires dans la forêt, signalés par un rayon de lumière visible depuis la tour.
- Ils sont gratuits. On soulève le couvercle à la main : le temps ralentit, 3 orbes flottent, et on en attrape une.
- Une orbe donne soit un bonus temporaire, soit une amélioration pour toute la partie, tirée comme en boutique. Bonus temporaires possibles : 30 s de dégâts doublés, de tirs parfaits garantis ou d'anneau accéléré, ou un soin.

## 14. L'interface

- **Menu principal**, dans le décor en haut de la tour, avant la première vague : Jouer (avec le choix de la difficulté), Paramètres, Classement, Quitter. Le gong et les panneaux de difficulté marchent aussi. On clique au rayon de la main libre, avec la gâchette.
- **Paramètres** : volumes général, musique et effets (Audio Mixer) ; rotation au joystick par crans ; vignette de confort (avec le slide, semaine 3).
- **Montre au poignet** : score, chrono, numéro de vague, PV du joueur, PV de la tour, argent, combo, difficulté.
- **Retours dans le monde** : chiffres de dégâts, « Headshot ! », « Parfait ! ».
- **Fin de partie** : un écran apparaît devant le joueur après sa mort, et les ennemis s'enfuient. Il affiche le résumé (score, vague atteinte, difficulté, ennemis tués, headshots, tirs parfaits) et fait saisir le nom au clavier virtuel de XRI (exemple *Spatial Keyboard*). Le classement s'affiche ensuite avec la nouvelle ligne en doré ; *Rejouer* ramène au menu avec un fondu.
- **Classement** : les 10 meilleurs scores, avec le nom, le score, la difficulté et la vague atteinte, du meilleur au moins bon.

## 15. Sauvegarde

Fichiers JSON dans `Application.persistentDataPath` :

- `leaderboard.json` : nom, score, difficulté, vague atteinte, ennemis tués et date de chaque partie, triés du meilleur score au moins bon (100 parties gardées, 10 affichées) ;
- `settings.json` : volumes et rotation au joystick.

Sous Windows, le dossier est `%USERPROFILE%\AppData\LocalLow\<Company Name>\<Product Name>`.

## 16. Sons

Tous les sons du jeu sont spatialisés.

- **Arc** : grincement de la corde (plus aigu quand elle est tendue), claquement au lâcher, sifflement de la flèche en vol.
- **Impacts** : bois, chair, « ding » du headshot, pierre.
- **Ennemis et tour** : grognements, battements d'ailes, tirs, tour qui encaisse, gong.
- **Ambiance** : oiseaux le jour, grillons et chouettes la nuit, vent ; musiques de vague et de boutique.
- **Sources libres** : Kenney (CC0), freesound.org (filtre CC0), Sonniss GDC Bundle.

## 17. La carte

- Une seule scène : une clairière d'environ 35 m de rayon, la tour au centre, une forêt dense autour, percée de 4 chemins.
- Décors et monstres : packs nature et monstres animés de Quaternius, Nature Kit de Kenney.
- Sol : le Terrain d'Unity donne un rendu lisse. Il faudra un shader à facettes ou un sol modélisé en low-poly (à trancher au moment du prototype).

## 18. Architecture technique

```
Assets/_Project/
├── Scripts/                       assemblage Archery.Runtime
│   ├── Core/        vibrations des manettes, sons ponctuels, pulsation
│   ├── Bows/        arc, modèle d'arc importé (BowVisual), flèche, réserve de flèches, carquois, anneau de timing, réglages de tir
│   ├── Combat/      PV, zones de touche, dégâts
│   ├── Enemies/     ennemi au sol, boss, données des ennemis, apparition
│   ├── Defense/     la tour, son téléporteur
│   ├── Player/      accès au joueur (tête, mains, arc rangé), PV du joueur, téléportation, fondu au noir
│   ├── Training/    cibles d'entraînement, cibles mobiles
│   ├── UI/          textes flottants (points, dégâts), affichage de la partie (montre, panneau), barres de PV
│   ├── Waves/       gestion des vagues, réglages des vagues, gong
│   ├── Economy/     score, combo, argent
│   ├── Difficulty/  difficultés, choix de la difficulté (panneaux)
│   ├── World/       ciel (skybox, soleil ou lune, ambiance, brouillard), objets de nuit
│   ├── Upgrades/    améliorations achetées, flèches spéciales (division, écho, foudre, explosion, glace, auto-visée, ricochet), éclairs, zones de glace
│   ├── Shop/        catalogue, offres et achats, panneau et cartes de la boutique
│   ├── Menus/       menu principal, paramètres, classement, écran de fin de partie
│   ├── Save/        fichiers JSON, classement, paramètres du joueur
│   └── (à venir)    Locomotion
├── Shaders/         anneau de timing, ciel stylisé, traînées des flèches
├── Data/            ScriptableObjects : arcs, réglages de tir, ennemis, vagues, difficultés, catalogue de la boutique
├── Prefabs/
├── Scenes/
├── Materials/
├── Art/
└── Audio/           sons provisoires générés, à remplacer
```

- Les scènes et les prefabs sont montés à la main dans Unity, en suivant les guides de `Docs/` (`Guide_Ennemis.md`, `Guide_Modeles.md`, `Guide_Vagues.md`, `Guide_Difficultes.md`, `Guide_Boutique.md`, `Guide_Teleporteur.md`, `Guide_Menus.md`, `Guide_Boss.md`).
- Modèles : arcs et flèches du pack Easy Weapons (arcs riggés avec Animation Rigging, pilotés par `BowVisual`), chevalier de Toon RTS Units – Demo pour l'ennemi au sol.
- Outils : `Tools/compile_check.py` vérifie que les scripts compilent sans ouvrir Unity, `Tools/shader_check.py` vérifie la syntaxe HLSL des shaders.
- Les réglages (arcs, améliorations, ennemis, vagues, difficultés) sont des ScriptableObjects : on équilibre le jeu sans toucher au code.
- `GameManager` enchaîne les états Menu → Vague → Pause → Fin de partie.
- Pools d'objets (`UnityEngine.Pool`) pour les flèches, les ennemis et les effets.
- Paquet à ajouter : AI Navigation, pour les déplacements des ennemis au sol.
- Tests rapides sans casque avec le *XR Interaction Simulator* ; tests réels sur Quest 3 via Link.

## 19. Git et GitHub

- Le dépôt est à la racine du projet Unity, avec un `.gitignore` adapté à Unity et Git LFS pour les modèles, textures et sons.
- `main` reste toujours jouable. Chaque fonctionnalité a sa branche (`feature/arc`, `feature/anneau-timing`, `feature/ennemis`…), fusionnée par pull request.
- Des commits petits et réguliers, avec des messages clairs.

## 20. Correspondance avec les consignes

| Consigne | Où elle est couverte |
|---|---|
| Interactions XR, geste naturel, trajectoire physique | sections 4 et 12 |
| Plusieurs arcs et flèches, améliorations entre les niveaux | sections 5, 6 et 7 (une vague = un niveau) |
| Zones de score, cibles mobiles, score en temps réel | sections 8 et 10 (tête et corps des ennemis mobiles), cibles d'entraînement à anneaux |
| Niveaux de difficulté | section 11 |
| Mode entraînement | cibles du menu et des pauses (section 3) |
| Menu, paramètres, affichage en jeu | section 14 |
| Nom au clavier virtuel, sauvegarde JSON, classement | sections 14 et 15 |
| Sons 3D | section 16 |
| GitHub | section 19 |

## 21. Planning

### Semaine 1 (2 au 8 octobre) : le cœur du jeu

- [x] Étape 0 : Git, dépôt GitHub, structure des dossiers, scène de test (le projet est resté dans OneDrive).
- [x] L'arc : prise en main, flèche dans le dos, encoche, tension, tir physique, flèche qui se plante, vibrations, sons.
- [x] L'anneau de timing et les cibles d'entraînement.
- [x] Un ennemi au sol (déplacement, tête et corps, PV, attaque), la tour (PV, téléporteur au pied de la tour) et les PV du joueur.
- [x] Une première vague avec chrono et score.

Objectif : tirer sur des ennemis qui attaquent la tour.

### Semaine 2 (9 au 15 octobre) : le jeu complet

- [ ] Gestion des vagues : budget, durée croissante, 10 vagues puis mode infini (fait), boss toutes les 5 vagues (codé, à tester : `Docs/Guide_Boss.md`).
- [x] Les 4 difficultés et leurs ciels (la lanterne et les yeux qui brillent attendent les décors définitifs, en fin de projet).
- [x] Score complet (timing, distance, combo) et argent.
- [x] Boutique, raretés, améliorations (19), flèches spéciales, arcs à acheter, réparation et reconstruction de la tour.
- [x] Menu principal, paramètres, fin de partie, clavier virtuel, sauvegarde JSON, classement (montage corrigé après les premiers tests).
- [x] Anneau de timing à 3 couleurs (rouge, orange, vert pour le tir parfait), après les tests.
- [x] Après les tests : améliorations sans limite d'achat (chances, puis effets plus forts), flèches en plus avec leurs propres effets, déluge qui divise les flèches en vol, courbe de difficulté exponentielle, prix du mode infini (+20 % par vague).

Objectif : toutes les consignes du cours sont couvertes.

### Semaine 3 (16 au 22 octobre) : la vision

- [ ] Course aux bras et slide.
- [ ] Coffres.
- [ ] Ennemis volants et tireurs.
- [ ] Barricades et brasero.
- [ ] Carte finale, ambiance sonore, effets visuels, équilibrage ; finitions avec des assets de l'Asset Store (lanterne et yeux qui brillent la nuit, décor).
- [ ] Coup de flèche au corps à corps (section 4.5), une fois tout le reste en place.
- [ ] Si le temps le permet, à la toute fin : les idées de la section 23 (arcs comme classes, progression entre les parties, arc légendaire en 3 morceaux, grenade de flèches).
- [ ] README, tests complets, préparation de la démo.

### En cas de retard

Tout ce qui est prévu en semaines 1 et 2 est indispensable. En semaine 3, on abandonne dans cet ordre : les coffres, les barricades et le brasero, les ennemis tireurs, le slide, puis les ennemis volants.

### Après le rendu

- Tester un système d'XP avec choix d'amélioration en pleine vague.
- D'autres ennemis (brute, élites) et d'autres boss.
- Des légendaires qui attaquent tout seuls (baliste sur la tour, esprit gardien).
- Un équilibrage plus poussé, de nouvelles cartes.

## 22. Choix par défaut, à confirmer

- À la fin du chrono, les ennemis restants s'enfuient.
- Une vague de boss ne se termine qu'à la mort du boss.
- L'anneau recommence si le cercle atteint le centre.
- Les PV du joueur sont restaurés entre les vagues ; ceux de la tour ne remontent qu'en boutique.
- On lance la vague suivante en tirant dans un gong.
- Il faut survivre à 10 vagues pour gagner.
- Le slide est sur le bouton A ou X.
- La rotation par crans au joystick est activée par défaut (désactivable).
- Le menu se trouve en haut de la tour, dans la même scène que le jeu.

## 23. Idées pour la fin du projet

Notées pendant les tests, à faire une fois le reste en place et seulement si le temps le permet.

- **Les arcs comme des classes** : on ne les achète plus en boutique. On en choisit un au début de la partie, chacun avec ses statistiques et sa particularité (anneau rapide, puissance, perçage…). La boutique garde les améliorations et les services de la tour.
- **Progression entre les parties (roguelite)** : on garde quelque chose d'une partie à l'autre, par exemple de l'expérience ou une monnaie, pour débloquer les arcs peu à peu. Elle s'ajoute à la sauvegarde JSON (section 15).
- **Un arc légendaire en 3 morceaux** : chaque morceau se trouve dans un coffre (section 13). Avec les trois, on obtient l'arc.
- **Une grenade de flèches** : on l'attrape en bas du dos, on la lance, et elle projette des flèches dans toutes les directions. Elle a un temps de recharge, affiché sur la montre.
- **Le coup de flèche au corps à corps** (section 4.5).
