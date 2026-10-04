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

### 4.5 Coup de flèche au corps à corps

- On frappe un ennemi avec la pointe de la flèche tenue en main (pas encochée), d'un geste franc (pointe à plus de 2 m/s) : il subit les dégâts d'un tir orange de l'arc, bonus de dégâts compris. Un coup à la tête compte double.
- La flèche reste plantée dans l'ennemi ; il faut en reprendre une dans le dos.
- Le coup compte comme une touche (points, combo, vampirisme). Une flèche enflammée au brasero fait brûler l'ennemi.
- C'est surtout utile en début de partie. L'attaque ne profite pas des flèches spéciales (multitir…), elle devient donc moins intéressante ensuite.

### 4.6 La grenade de flèches

- On la prend d'une main vide en bas du dos, derrière les hanches (un étui invisible, comme le carquois), et on la lance.
- Elle éclate au premier choc, ou 1,5 s après le lancer, et projette 24 flèches tout autour d'elle, sur deux couronnes. Chaque flèche fait les dégâts d'un tir orange et ne casse pas le combo.
- Recharge : 30 s à partir du moment où on la prend, affichée sur la montre ; une vibration annonce la suivante.

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

| Rareté | Couleur | Rôle | Chances |
|---|---|---|---|
| Commune | blanc | statistiques | 70 % |
| Rare | bleu | effets qui se déclenchent parfois | 25 % |
| Légendaire | doré | effets puissants ou uniques | 5 % |

Les chances sont les mêmes toute la partie. Aux premiers tests, elles montaient avec les vagues, et les boutiques de fin de partie n'avaient plus aucune carte commune. L'amélioration Chance augmente le poids des rares et des légendaires, mais les communes gardent toujours au moins 40 % des tirages.

### 6.2 Liste de départ

**Aucune limite d'achat** : chaque amélioration peut être prise autant de fois qu'on veut, et tout se cumule.

- **Les bonus** s'additionnent : dégâts, vitesse, PV, etc.
- **Les chances** (foudre, explosion, glace) s'additionnent jusqu'à 100 %. Au-delà, chaque exemplaire rend l'effet plus fort (tableau ci-dessous).
- **Les nombres de flèches** (multitir, tir écho, déluge) et le perçage sont des moyennes. Chaque exemplaire en ajoute une part : avec 150 %, on a 1 flèche en plus à coup sûr, et 50 % de chance d'en avoir une deuxième. Il n'y a pas de limite. Seule une sécurité, pour que le jeu reste fluide, arrête d'ajouter des flèches au-delà de 150 en vol en même temps.
- **Ils s'enchaînent** : le multitir ajoute des flèches à la volée, le tir écho répète toute la volée, et le déluge divise en vol chaque flèche, celles du multitir et de l'écho comprises. Les flèches nées d'une division ne se divisent pas à leur tour.
- **La volée reste centrée sur la visée** : la flèche de l'arc part toujours tout droit. Les flèches en plus se placent par paires, à gauche et à droite, à égalité. Avec un nombre pair de flèches, celle qui n'a pas de paire part juste à côté de la flèche de l'arc (20 cm), dans la même direction : avec 2 flèches, les deux vont là où l'on vise. Le déluge suit la même règle autour de la flèche qui se divise, qui continue tout droit.
- **Chaque flèche en plus est une vraie flèche** : elle tire au sort ses propres effets (foudre, explosion, glace, perçage) et profite de l'auto-visée et du ricochet. Elle ne casse pas le combo quand elle rate. En revanche, elle ne rapporte pas de points de touche et ne fait pas monter le combo ; ses éliminations rapportent normalement (section 8).

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
- Chance : les cartes rares et légendaires sortent plus souvent en boutique (+25 % de poids face aux communes). Les communes gardent toujours au moins 40 % des tirages.

**Rares** (se déclenchent au hasard)

- Multitir : +50 % de chance de tirer une flèche en plus. Avec 2 exemplaires, une flèche en plus à chaque tir ; avec 3, une flèche en plus et 50 % de chance d'une deuxième ; etc. Avec 2 flèches, elles partent côte à côte ; à partir de 3, l'éventail s'ouvre de chaque côté.
- Flèche de foudre : chaque flèche a 20 % de chance d'appeler un éclair qui blesse la cible (75 % des dégâts de la flèche) et la ralentit de 40 % pendant 2 s.
- Perçage : chaque flèche a 25 % de chance de traverser un ennemi ; au-delà de 100 %, elle en traverse plusieurs. Chaque ennemi traversé enlève 20 % des dégâts.
- Vampirisme : chaque headshot rend 2 PV.
- Tir écho : 25 % de chance que la volée se répète 0,25 s après, à la même puissance, multitir compris ; au-delà de 100 %, plusieurs échos, à 0,25 s d'intervalle.
- Flèche de glace : chaque flèche a 25 % de chance de laisser au sol une zone de glace (3 m, 5 s) qui ralentit de 50 % les ennemis au sol.

**Légendaires**

- Déluge : en vol, environ 0,2 s après son départ, chaque flèche a 50 % de chance de se diviser en deux. Avec 2 exemplaires, toujours ; avec 3, en deux, et 50 % de chance en trois ; etc. La flèche continue tout droit, la nouvelle part juste à côté d'elle, et les suivantes s'écartent par paires.
- Chaîne d'éclairs : la foudre rebondit sur 3 ennemis proches de plus par exemplaire. Sans flèche de foudre, 20 % des flèches appellent l'éclair.
- Flèche explosive : chaque flèche a 25 % de chance d'exploser et de toucher tous les ennemis autour (3,5 m).
- Auto-visée : les flèches dévient légèrement (30° par seconde et par exemplaire) vers l'ennemi le plus proche, s'il est à moins de 12 m devant elles.
- Tir ricochet : après avoir touché un ennemi, la flèche rebondit vers un autre ennemi proche (un rebond par exemplaire, 20 % de dégâts en moins par rebond).

**Flèches enflammées** : avec le brasero de la tour (voir section 9), on trempe une flèche dans le feu ; la cible brûle pendant 3 s (30 % des dégâts de la flèche par seconde). Les flèches en plus d'un tir enflammé brûlent aussi.

## 7. La boutique

- Elle s'ouvre entre les vagues, en haut de la tour : un panneau à gauche du joueur. On vise une carte avec le rayon de la main libre et on achète avec la gâchette.
- Elle propose 4 améliorations tirées au sort selon les raretés, le prochain arc et les services de la tour.
- Relancer les offres coûte 10, puis 5 de plus à chaque relance (le coût revient au départ à chaque pause). Il suit la hausse des vagues et du mode infini.
- Services de la tour : réparation (+25 % des PV, 40), reconstruction après destruction (250), réparation de toutes les barricades (60), brasero (100, une seule fois). Chaque service a sa carte.

**Prix des améliorations.** Aux premiers tests, en Impossible, on gagnait jusqu'à 5 000 pièces d'or vers la vague 10 et l'on achetait des dizaines d'améliorations par pause, au point de faire ramer le jeu. Les prix montent donc de trois façons, qui se cumulent :

- prix de base : commune 25, rare 60, légendaire 140 ;
- **+10 % à chaque vague**, en se cumulant (×2,4 après la vague 10) ;
- **+5 % à chaque amélioration obtenue** (achetée, ou trouvée dans un coffre), en se cumulant (×1,6 après 10, ×2,7 après 20, ×7 après 40) : plus on en a, plus la suivante coûte cher ;
- **en mode infini, ×1,2 à chaque vague**, en se cumulant, pour tous les prix de la boutique (améliorations, arc, tour, relance).

| Prix d'une rare (commune, légendaire) | 0 achat | 10 achats | 20 achats | 40 achats |
|---|---|---|---|---|
| Après la vague 1 | 60 (25, 140) | 100 (40, 230) | 160 (65, 370) | 420 (175, 985) |
| Après la vague 5 | 90 (35, 205) | 145 (60, 335) | 235 (95, 545) | 620 (260, 1 445) |
| Après la vague 10 | 140 (60, 330) | 230 (95, 540) | 375 (155, 875) | 995 (415, 2 325) |
| Après la vague 15 | 565 (235, 1 325) | 925 (385, 2 155) | 1 505 (625, 3 510) | 3 990 (1 665, 9 315) |

Avec la courbe des ennemis (section 11) et la baisse de l'or (section 8), cela compense les achats sans limite : selon une simulation (un très bon joueur qui tue tous les ennemis et dépense tout), on possède une trentaine d'améliorations à la vague 10 en Normal et une cinquantaine en Impossible, au lieu de plus de 250. Le mode infini finit toujours par déborder le joueur.

## 8. Score et argent

**Points**

- Chaque touche de la flèche tirée par l'arc : 5 points au corps, 10 à la tête. Les flèches en plus (multitir, écho, déluge) ne rapportent pas de points de touche : sinon, les points et l'or grimpaient avec le nombre de flèches.
- Chaque ennemi tué, par n'importe quelle flèche ou effet : sa valeur de base (section 10), doublée s'il meurt d'un tir à la tête.
- Ces points sont multipliés par la qualité du tir (section 4.2), la distance, le combo et la difficulté (section 11).
- Fin de vague : 50 points × numéro de la vague.

| Multiplicateur | Règle |
|---|---|
| Distance | +1 % par mètre au-delà de 10 m, jusqu'à +50 % |
| Combo | +10 % par touche consécutive de la flèche tirée par l'arc, jusqu'à ×2 ; retombe à zéro quand cette flèche ne touche aucun ennemi |

**Argent** : 25 % des points gagnés (l'amélioration Butin augmente ce taux), divisé par deux tant que la tour est détruite. À partir de la vague 5, ce taux baisse de 9 % à chaque vague, en se cumulant :

| Vague | 1 à 4 | 5 | 10 | 15 | 20 |
|---|---|---|---|---|---|
| Points changés en or | 25 % | 23 % | 14 % | 9 % | 5,5 % |

Le score, lui, ne baisse pas.

## 9. La tour

- 1000 PV, qui ne remontent pas tout seuls : il faut la réparer en boutique.
- Un téléporteur au pied de la tour mène au sommet, avec un fondu au noir. On reste debout 1 s sur un cercle lumineux ; on arrive en haut tourné vers les ennemis. Au sol, on rejoint le cercle avec le joystick. Pour redescendre, on marche simplement dans le vide (un cercle au sommet reste possible).
- À la fin d'une vague, si le joueur n'est pas en haut, le panneau de la boutique vient à côté de lui.
- Le sommet est une plateforme à environ 6 m, avec une rambarde, la boutique, le gong et l'emplacement du brasero.
- À 0 PV, la tour s'effondre. Le joueur qui était en haut est téléporté au sol, devant le cercle du bas. Le téléporteur ne fonctionne plus et l'argent gagné est divisé par deux jusqu'à la reconstruction.
- **Barricades** : des murs de planches aux 4 entrées de la clairière (400 PV chacun, debout au début de la partie). Un ennemi au sol qui arrive devant une barricade, côté forêt (zone de 5 m), s'arrête pour la frapper ; il ne passe qu'une fois qu'elle est détruite. Les tireurs la cassent avec leurs projectiles ; les volants passent au-dessus. En boutique, une réparation remet toutes les barricades debout avec tous leurs PV.
- **Brasero** : acheté une fois, il s'allume en haut de la tour. On y trempe la pointe d'une flèche tenue en main : elle s'enflamme. Sa cible brûle 3 s et perd chaque seconde 30 % des dégâts de la flèche. Les flèches en plus d'un tir enflammé (multitir, écho, déluge) brûlent aussi.

## 10. Les ennemis

| Ennemi | Déplacement | Cible | PV | Vitesse | Attaque | Valeur | Dès la vague |
|---|---|---|---|---|---|---|---|
| Rampant | au sol | la tour, ou le joueur s'il est à moins de 8 m | 30 | 2,5 m/s | corps à corps | 10 | 1 |
| Volant | dans les airs | le joueur | 20 | 5 m/s | attaque en piqué | 15 | 2 |
| Tireur | au sol, s'arrête vers 25 m | le joueur | 40 | 2 m/s | projectile lent, qu'on peut esquiver ou abattre | 20 | 4 |
| Boss | au sol | la tour | 1500 | 1 m/s | frappe la tour, appelle des Rampants | 250 | 5, 10, 15… |

- Zones de touche : la tête et le corps. Le boss a en plus des points faibles lumineux (dégâts ×3).
- **Volant** : il tourne en l'air autour du joueur, à 6–8 m de haut. De temps en temps, il fait du sur-place en criant (0,6 s, le moment de le viser), puis pique sur la tête du joueur, en corrigeant lentement sa trajectoire : un pas de côté suffit à l'esquiver. Touché ou raté, il remonte et recommence (un piqué toutes les 6 s environ). Tué, il tombe au sol en tournoyant. Il ne passe pas par le NavMesh ; la glace, posée au sol, ne le ralentit pas.
- **Tireur** : il marche vers le joueur et s'arrête vers 20 m (80 % de sa portée de 25 m). Toutes les 3,5 s, il lance un projectile lent (9 m/s) vers la tête du joueur. On peut l'esquiver, ou l'abattre d'une flèche (5 points). Le projectile se brise sur le décor.
- Le volant et le tireur visent toujours le joueur, jamais la tour.
- Le boss est un chevalier géant (×2,2) à l'armure rouge sombre, avec 3 points faibles cyan qui pulsent : la poitrine et les deux épaules. Il arrive 4 s après le début de la vague, avec un cor et des tambours. Il appelle 2 Rampants toutes les 14 s, et une barre de PV flotte au-dessus de lui. Comme les autres ennemis, il suit la courbe de difficulté (section 11) : le boss de la vague 10 est plus coriace que celui de la vague 5. Une vague de boss compte 40 % de Rampants ordinaires en moins.
- Les ennemis sortent de la forêt par 4 chemins autour de la clairière.
- Chaque vague dispose d'un budget qui augmente de vague en vague. Chaque ennemi a un coût, et le budget est dépensé tout au long du chrono.
- Les PV, les dégâts, le nombre et la vitesse des ennemis suivent une courbe exponentielle (section 11). Le tableau ci-dessus donne leurs valeurs à la vague 1, en Normal. Quand ils vont plus vite, leur animation de course accélère aussi.
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
| Vitesse des ennemis, à chaque vague | ×1,01 | ×1,02 | ×1,025 | ×1,03 |
| Début plus doux (PV et dégâts) | non | non | non | ×0,5 à la vague 1, en remontant jusqu'à ×1 à la vague 6 |
| Largeur de la bande verte (parfait) | ×1,3 | ×1 | ×0,85 | ×0,7 |
| Aide à la visée | oui | oui | non | non |
| Score | ×0,75 | ×1 | ×1,5 | ×2 |

La nuit, les yeux des ennemis brillent et une lanterne éclaire la tour, pour que le jeu reste lisible.

**Courbe de difficulté exponentielle.** Les améliorations n'ont pas de limite (section 6.2) : pour compenser, chaque vague multiplie encore les PV, les dégâts, le nombre et la vitesse des ennemis (lignes « à chaque vague »). La courbe monte plus vite en Difficile et en Impossible. En mode infini, elle s'accélère encore, quelle que soit la difficulté : chaque vague après la 10e multiplie en plus les PV par 1,12, les dégâts par 1,06, le nombre d'ennemis par 1,08 et leur vitesse par 1,03. Les prix de la boutique montent aussi, et l'or gagné baisse (sections 7 et 8).

La vitesse ne fait au plus que doubler (réglage *Max Speed Scale* des Wave Settings), pour que les ennemis restent lisibles et que leurs déplacements restent propres :

| Vitesse d'un Rampant | Vague 1 | Vague 5 | Vague 10 | Vague 15 | Vague 20 |
|---|---|---|---|---|---|
| Facile | 2,0 m/s | 2,1 m/s | 2,2 m/s | 2,7 m/s | 3,2 m/s |
| Normal | 2,5 m/s | 2,7 m/s | 3,0 m/s | 3,8 m/s | 4,9 m/s |
| Difficile | 2,9 m/s | 3,2 m/s | 3,6 m/s | 4,7 m/s | 5,8 m/s (plafond) |
| Impossible | 3,3 m/s | 3,7 m/s | 4,2 m/s | 5,7 m/s | 6,5 m/s (plafond) |

| PV d'un Rampant (dégâts d'un coup) | Vague 1 | Vague 5 | Vague 10 | Vague 15 | Vague 20 |
|---|---|---|---|---|---|
| Facile | 21 (6) | 27 (7) | 35 (8) | 84 (12) | 197 (19) |
| Normal | 30 (10) | 41 (12) | 60 (14) | 155 (23) | 402 (38) |
| Difficile | 42 (13) | 64 (16) | 107 (22) | 319 (39) | 947 (70) |
| Impossible | 30 (9) | 91 (22) | 195 (36) | 662 (71) | 2 247 (139) |

**Début de partie plus doux en Impossible.** Aux tests, les vagues 1 à 5 étaient très dures en Impossible. Les PV et les dégâts des ennemis y sont donc réduits au début, puis remontent régulièrement : la vague 1 est bien plus simple, la vague 2 un peu moins, et ainsi de suite jusqu'à la vague 6, où l'on retrouve la courbe normale. Le nombre d'ennemis ne change pas : Impossible garde ses vagues en masse, et l'on gagne autant d'or (même un peu plus, puisqu'on en tue davantage). Réglages *Early Strength* et *Full Strength Wave* de chaque difficulté.

| Impossible | Vague 1 | Vague 2 | Vague 3 | Vague 4 | Vague 5 | Vague 6 |
|---|---|---|---|---|---|---|
| Force des ennemis | 50 % | 60 % | 70 % | 80 % | 90 % | 100 % |
| PV d'un Rampant | 30 (avant 60) | 41 (68) | 55 (78) | 71 (89) | 91 (101) | 116 |
| Dégâts d'un coup | 9 (avant 18) | 12 (19) | 15 (21) | 18 (23) | 22 (24) | 26 |

En Normal, une vague sans boss compte 6 ennemis à la vague 1, 51 à la vague 10 et 343 à la vague 20 (dans la limite de 40 en vie en même temps, section 10). Ces réglages se trouvent dans les assets `Data/Difficulties` (courbe de chaque difficulté) et `Data/Waves/WaveSettings` (accélération du mode infini).

On choisit la difficulté avant la première vague, en tirant dans l'un des 4 panneaux devant la tour ; le ciel passe au nouveau ciel en quelques secondes. Elle est ensuite verrouillée, et elle est gardée quand la partie recommence.

## 12. Les déplacements

- Le joueur se déplace physiquement dans sa pièce.
- **Marche** : le joystick gauche donne la direction, par rapport au regard (lissé, pour que la course ne tangue pas). Joystick en arrière, on recule. 2,5 m/s, joystick à fond.
- **Course aux bras** : en marchant, balancer les deux bras multiplie la vitesse, jusqu'à ×3 (7,5 m/s). La vitesse suit celle de la main la plus lente par rapport à la tête : tendre la corde ou prendre une flèche, d'une seule main, ne fait pas courir. Pas besoin de gâchette (option possible).
  - On accélère et on freine progressivement : en pleine course, un demi-tour freine d'abord. En l'air, on garde son élan.
- **Slide** : A ou X pendant la course, à partir de 3,5 m/s. On repart 40 % plus vite (10 m/s au plus), puis on glisse 2,5 s dans la même direction, avec peu de frottement, les mains libres pour tirer. Un son, une vibration et la vignette de confort l'accompagnent.
- Ces déplacements passent par le système de locomotion d'XRI : on ne traverse pas les murs, et l'on tombe si l'on court dans le vide. Le script remplace le déplacement au joystick d'XRI, et le saut d'XRI (bouton A) est désactivé, puisque A sert au slide.
- Rotation par crans de 45° au joystick, désactivable : pratique avec le câble Link.
- Vignette de confort pendant le slide (et, en option, pendant la course), réglable dans les paramètres.
- Le téléporteur de la tour.

## 13. Les coffres

- **Apparition** : 1 coffre par vague, et 50 % de chance d'un deuxième, après 15 % puis 55 % du chrono. Ils se posent à 15–30 m de la tour, sur le sol des ennemis, ou sur des emplacements choisis (dans la forêt de la carte finale). Un message les annonce, et un rayon de lumière doré, qui pulse, les signale depuis la tour.
- **Risque** : ils sont gratuits, mais il faut quitter la tour pendant la vague et courir les chercher. Ils disparaissent à la fin de la vague, ouverts ou non.
- **Ouverture** : on attrape le couvercle à la main (pas au rayon) et on le soulève ; il tourne autour de sa charnière en suivant la main. Passé 45°, le coffre s'ouvre : le temps ralentit (×0,3), et 3 orbes montent du coffre, chacune avec le nom de sa récompense dans sa couleur.
- **Choix** : on attrape une orbe, les autres disparaissent. Le ralenti s'arrête au choix, ou au bout de 6 s ; les orbes restent alors jusqu'à la fin de la vague.
- **Récompenses** : il y a toujours au moins une amélioration pour toute la partie, tirée comme en boutique (raretés et Chance comprises), et un bonus temporaire. La troisième orbe est l'un ou l'autre. Bonus possibles :
  - 30 s de dégâts doublés ;
  - 30 s de tirs parfaits : l'anneau est presque entièrement vert, et tout tir lâché à pleine tension compte comme parfait ;
  - 30 s d'anneau accéléré (×1,75, bandes un peu plus larges) ;
  - un soin complet, proposé seulement si le joueur a perdu des PV.
- Les bonus en cours s'affichent sur la montre avec leur temps restant ; un message annonce leur fin. Une amélioration trouvée dans un coffre compte pour la hausse des prix de la boutique (section 7).

## 14. L'interface

- **Menu principal**, dans le décor en haut de la tour, avant la première vague : Jouer (avec le choix de la difficulté), Paramètres, Classement, Quitter. Le gong et les panneaux de difficulté marchent aussi. On clique au rayon de la main libre, avec la gâchette.
- **Paramètres** : volumes général, musique et effets (Audio Mixer) ; rotation au joystick par crans ; vignette de confort pendant le slide, de « Aucune » à 100 %.
- **Montre au poignet** : score, chrono, numéro de vague, PV du joueur, PV de la tour, argent, combo, difficulté, bonus des coffres en cours.
- **Retours dans le monde** : chiffres de dégâts, « Headshot ! », « Parfait ! » ; particules à l'impact (giclée au corps, étincelles dorées à la tête, poussière dans le décor) et fumée à la mort d'un ennemi.
- **Fin de partie** : un écran apparaît devant le joueur après sa mort, et les ennemis s'enfuient. Il affiche le résumé (score, vague atteinte, difficulté, ennemis tués, headshots, tirs parfaits) et fait saisir le nom au clavier virtuel de XRI (exemple *Spatial Keyboard*). Le classement s'affiche ensuite avec la nouvelle ligne en doré ; *Rejouer* ramène au menu avec un fondu.
- **Classement** : les 10 meilleurs scores, avec le nom, le score, la difficulté et la vague atteinte, du meilleur au moins bon.

## 15. Sauvegarde

Fichiers JSON dans `Application.persistentDataPath` :

- `leaderboard.json` : nom, score, difficulté, vague atteinte, ennemis tués et date de chaque partie, triés du meilleur score au moins bon (100 parties gardées, 10 affichées) ;
- `settings.json` : volumes, rotation au joystick et vignette de confort ;
- `progress.json` : expérience totale, arc choisi et nombre de parties (arcs comme classes, section 23).

Sous Windows, le dossier est `%USERPROFILE%\AppData\LocalLow\<Company Name>\<Product Name>`.

## 16. Sons

Tous les sons du jeu sont spatialisés.

- **Arc** : grincement de la corde (plus aigu quand elle est tendue), claquement au lâcher, sifflement de la flèche en vol.
- **Impacts** : bois, chair, « ding » du headshot, pierre.
- **Ennemis et tour** : grognements, battements d'ailes, tirs, tour qui encaisse, gong.
- **Déplacements et coffres** : pas, slide, apparition et ouverture d'un coffre, orbe prise, disparition.
- **Ambiance** : oiseaux le jour, grillons et chouettes la nuit, vent, selon le ciel de la difficulté.
- **Musiques** : menu, pause (boutique), vague, boss ; rien après la mort. On passe d'une boucle à l'autre en fondu. La musique suit le curseur *Musique* des paramètres, l'ambiance celui des *Effets*. Les boucles actuelles sont générées pour tester, à remplacer en fin de projet.
- **Sources libres** : Kenney (CC0), freesound.org (filtre CC0), Sonniss GDC Bundle.

## 17. La carte

- Une seule scène : une clairière d'environ 35 m de rayon, la tour au centre, une forêt dense autour (de 37 à 75 m), percée de 4 chemins de 4 m de large (nord, est, sud, ouest), avec les points d'apparition à leur bout (65 m) et une barricade à chaque entrée (36 m).
- La forêt n'est pas praticable pour les ennemis au sol (NavMesh « Not Walkable ») : ils passent par les chemins. Les coffres apparaissent à des emplacements choisis le long des chemins.
- Décors et monstres : packs nature et monstres animés de Quaternius, Nature Kit de Kenney.
- Sol : un grand plan au matériau d'herbe, avec des bandes de terre pour les chemins. Pas de Terrain d'Unity, dont le rendu lisse jure avec le style low-poly.

## 18. Architecture technique

```
Assets/_Project/
├── Scripts/                       assemblage Archery.Runtime
│   ├── Core/        vibrations des manettes, sons ponctuels, pulsation, ralenti du temps
│   ├── Bows/        arc, modèle d'arc importé (BowVisual), flèche, réserve de flèches, carquois, anneau de timing, réglages de tir, grenade de flèches et son étui
│   ├── Combat/      PV, zones de touche, dégâts
│   ├── Enemies/     ennemi au sol, volant, tireur et ses projectiles, boss, données des ennemis, apparition
│   ├── Defense/     la tour, son téléporteur, les barricades, le brasero
│   ├── Player/      accès au joueur (tête, mains, arc rangé), PV du joueur, téléportation, fondu au noir
│   ├── Training/    cibles d'entraînement, cibles mobiles
│   ├── UI/          textes flottants (points, dégâts), affichage de la partie (montre, panneau), barres de PV
│   ├── Waves/       gestion des vagues, réglages des vagues, gong
│   ├── Economy/     score, combo, argent
│   ├── Difficulty/  difficultés, choix de la difficulté (panneaux)
│   ├── World/       ciel (skybox, soleil ou lune, ambiance, brouillard), objets de nuit, ambiance sonore et musiques
│   ├── Upgrades/    améliorations achetées, bonus temporaires, flèches spéciales (division, écho, foudre, explosion, glace, auto-visée, ricochet), éclairs, zones de glace
│   ├── Chests/      coffres, couvercle à soulever, orbes, récompenses, apparition pendant les vagues, arc légendaire
│   ├── Effects/     particules d'impact et de mort
│   ├── Shop/        catalogue, offres et achats, panneau et cartes de la boutique
│   ├── Menus/       menu principal, paramètres, classement, écran de fin de partie
│   ├── Save/        fichiers JSON, classement, paramètres du joueur, progression entre les parties
│   └── Locomotion/  course aux bras, slide, vignette de confort
├── Shaders/         anneau de timing, ciel stylisé, traînées des flèches
├── Data/            ScriptableObjects : arcs, réglages de tir, ennemis, vagues, difficultés, catalogue de la boutique
├── Prefabs/
├── Scenes/
├── Materials/
├── Art/
└── Audio/           sons provisoires générés, à remplacer
```

- Les scènes et les prefabs sont montés à la main dans Unity, en suivant les guides de `Docs/` (`Guide_Ennemis.md`, `Guide_Modeles.md`, `Guide_Vagues.md`, `Guide_Difficultes.md`, `Guide_Boutique.md`, `Guide_Teleporteur.md`, `Guide_Menus.md`, `Guide_Boss.md`, `Guide_Deplacements.md`, `Guide_Coffres.md`, `Guide_Volants_Tireurs.md`, `Guide_Barricades_Brasero.md`, `Guide_CorpsACorps.md`, `Guide_Ambiance.md`, `Guide_Effets.md`, `Guide_Carte.md`, `Guide_Grenade.md`, `Guide_ArcLegendaire.md`, `Guide_Classes.md`).
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

- [x] Gestion des vagues : budget, durée croissante, 10 vagues puis mode infini, boss toutes les 5 vagues.
- [x] Les 4 difficultés et leurs ciels (la lanterne et les yeux qui brillent attendent les décors définitifs, en fin de projet).
- [x] Score complet (timing, distance, combo) et argent.
- [x] Boutique, raretés, améliorations (19), flèches spéciales, arcs à acheter, réparation et reconstruction de la tour.
- [x] Menu principal, paramètres, fin de partie, clavier virtuel, sauvegarde JSON, classement (montage corrigé après les premiers tests).
- [x] Anneau de timing à 3 couleurs (rouge, orange, vert pour le tir parfait), après les tests.
- [x] Après les tests : améliorations sans limite d'achat (chances, puis effets plus forts), flèches en plus avec leurs propres effets, déluge qui divise les flèches en vol, courbe de difficulté exponentielle, prix du mode infini (+20 % par vague).
- [x] Après les tests suivants : prix qui montent à chaque vague et à chaque achat, or en baisse à partir de la vague 5, flèches en plus sans points de touche, raretés fixes, ennemis de plus en plus rapides, volées centrées sur la visée, début de partie plus doux en Impossible (vagues 1 à 5).

Objectif : toutes les consignes du cours sont couvertes.

### Semaine 3 (16 au 22 octobre) : la vision

- [x] Course aux bras et slide. Après les tests : direction donnée par le joystick (on peut reculer), les bras accélèrent la marche, slide plus long et plus rapide.
- [ ] Coffres (codé, à monter et tester : `Docs/Guide_Coffres.md`).
- [ ] Ennemis volants et tireurs (codé, à monter et tester : `Docs/Guide_Volants_Tireurs.md`).
- [ ] Barricades et brasero (codé, à monter et tester : `Docs/Guide_Barricades_Brasero.md`).
- [ ] Ambiance sonore et musiques (codé, boucles provisoires générées, à monter : `Docs/Guide_Ambiance.md`).
- [ ] Effets visuels des coups (codé, à monter : `Docs/Guide_Effets.md`).
- [ ] Carte finale (guide : `Docs/Guide_Carte.md`), équilibrage ; finitions avec des assets de l'Asset Store (lanterne et yeux qui brillent la nuit, décor, vraies musiques).
- [x] Coup de flèche au corps à corps (section 4.5) : rien à monter, à tester (`Docs/Guide_CorpsACorps.md`).
- [ ] Les idées de la section 23 (codées, à monter et tester) : grenade de flèches (`Docs/Guide_Grenade.md`), arc légendaire en 3 morceaux (`Docs/Guide_ArcLegendaire.md`), arcs comme classes et progression entre les parties (`Docs/Guide_Classes.md`).
- [ ] README (rédigé, crédits et équipe à compléter), tests complets, préparation de la démo (`Docs/Checklist_Demo.md`).

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

- **Les arcs comme des classes** et la **progression entre les parties** : faits, en mode optionnel (`Docs/Guide_Classes.md`). Avec le composant `Bow Classes`, on choisit son arc au menu et la boutique n'en vend plus. Chaque partie rapporte de l'expérience (1 XP pour 10 points), gardée dans `progress.json`, qui débloque les arcs : composite à 300 XP, long à 1 000, runique à 2 500. Sans le composant, les arcs s'achètent en boutique comme avant.
- **Un arc légendaire en 3 morceaux** : fait (`Docs/Guide_ArcLegendaire.md`). Tant qu'il n'est pas complet, un coffre a 35 % de chance de proposer un morceau à la place de sa troisième orbe. Au troisième, l'arc légendaire (62 m/s, 32 dégâts, anneau de 1 s aux bandes larges, flèches qui traversent 2 ennemis) remplace celui du joueur jusqu'à la fin de la partie.
- **Une grenade de flèches** : faite (section 4.6, `Docs/Guide_Grenade.md`).
