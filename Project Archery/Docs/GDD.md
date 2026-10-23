# THUNK VR (Project Archery) — Document de conception (GDD)

Version 0.1 · 2 octobre 2026 · document vivant, mis à jour au fil du projet (dernière mise à jour : 23 octobre 2026).

Les réglages font foi dans la scène et les assets `Data/` : ils sont ajustés à la main après les tests, et ce document les reprend.

## 1. Le jeu en bref

**Nom du jeu : THUNK VR** (depuis le 16 octobre au soir : le bruit d'une flèche qui se plante, court et drôle à dire, dans l'esprit de *Muck* ; avant, *VR Archery Challenge*, puis *SLACK VR* l'après-midi du 16). Il est gravé dans un cartouche de pierre au-dessus de la porte de l'antre (section 10 bis). Le nom est à un seul endroit dans le code (`GameInfo.Name`). Le *Product Name* du projet est lui aussi « THUNK VR » : les sauvegardes sont dans `AppData\LocalLow\DefaultCompany\THUNK VR` (celles de « Project Archery » n'ont pas suivi).

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
| Graphismes | low-poly : décor des kits *Nature Kit* et *Castle Kit* de Kenney (licence CC0), monstres de l'Asset Store |
| Consignes | adaptables : par exemple, les points du headshot remplacent ceux du centre de la cible |

## 3. Déroulement d'une partie

1. **Menu** : le joueur est en haut de la tour. Des cibles d'entraînement permettent de s'échauffer. Depuis le 17 octobre, elles sont dans la zone dégagée entre la voie du milieu et celle de droite (vues de la tour), de 16 à 36 m, loin des voies, de la rivière et des socles. Il choisit la difficulté (le ciel change en direct) et lance la partie.
2. **Vague** : un chrono tourne, en deux temps : l'accalmie, avec peu d'ennemis, puis la montée, où ils sortent de la forêt de plus en plus vite. À la fin du chrono, le boss arrive ; la vague se termine à sa mort, et les ennemis restants s'enfuient (section 3 bis).
3. **Pause** : la boutique s'ouvre en haut de la tour et les cibles d'entraînement réapparaissent. Les PV du joueur sont restaurés, pas ceux de la tour. On tire dans un gong pour lancer la vague suivante.
4. **Boss** à la fin de chaque vague : un boss différent à chaque vague (section 10), de plus en plus fort. Deux coups de gong pendant le chrono le font venir plus tôt, contre une récompense plus grosse. En mode infini, ils sont plusieurs.
5. **Victoire** à la mort du boss final, à la vague 6 (MK, le dragon rouge, dans son antre : section 10 bis). Le joueur termine alors la partie (bouton de la boutique), ou continue en mode infini pour le score (gong).
6. **Défaite** quand les PV du joueur tombent à 0. La destruction de la tour ne termine pas la partie, mais pénalise fortement (voir section 9).
7. **Fin de partie** : résumé, saisie du nom au clavier virtuel, classement, retour au menu.

Durée des vagues : 2 min de chrono à la vague 1, puis 15 s de plus à chaque vague (3 min à la vague 5, 3 min 15 s à la vague 6), plus le combat contre le boss. Une partie de 6 vagues dure environ 25 minutes, pauses comprises. Le détail est dans la section 3 bis.

## 3 bis. Refonte des vagues (décidée et codée le 12 octobre, testée)

**Pourquoi** : aux tests, les vagues étaient trop courtes pour tout faire (récolter les cristaux, construire, ouvrir les coffres, défendre) : on se sentait submergé. Cette section décrit les règles actuelles ; les sections 3, 7, 8, 10, 11 et 13 renvoient ici. Les chiffres ont été calés par simulation, puis ajustés à la main après les tests.

**6 vagues, plus longues, avec un boss à la fin de chacune.** La partie est passée de 10 à 5 vagues le 12 octobre, puis à 6 le 13 octobre : la vague 6 est celle du boss final (section 10 bis).

- **Chrono** : 2 min à la vague 1, puis 15 s de plus à chaque vague (3 min à la vague 5, 3 min 15 s à la vague 6). Une partie dure environ 25 minutes, boss et pauses compris.
- **Chaque vague en 3 temps** (affichés sur la montre) :
  1. **Accalmie** (les premiers 40 % du chrono) : 15 % des ennemis de la vague seulement (7 % à la vague 1, pour oser sortir récolter), en filet. La première série de gisements de cristaux et le premier coffre apparaissent : c'est le moment de sortir, de récolter et de construire.
  2. **Montée** (le reste du chrono, deux fois plus court qu'avant le 17 octobre : *Rise Duration Scale* 0,5) : le reste des ennemis arrive, de plus en plus vite. Le deuxième coffre apparaît au début de la montée ; plus de gisements à ce moment-là depuis le 17 octobre.
  3. **Le boss** : à la fin du chrono, il arrive (cor et tambours), et les ennemis prévus qui restaient sortent vite. Ensuite, un renfort arrive toutes les 6 s environ (plus souvent avec la difficulté et les vagues), tant qu'il est en vie. La vague se termine à sa mort ; les survivants s'enfuient.
- **Appeler le boss plus tôt** : pendant le chrono, le gong reste visible. Un premier coup demande confirmation et affiche le bonus, un deuxième dans les 4 s (et au moins 0,5 s après le premier) fait venir le boss : une flèche perdue ne suffit pas. Seule la flèche tirée par l'arc compte. Les flèches en plus du multitir, de l'écho ou du déluge font sonner le gong sans rien déclencher, et les flèches de la volée qui vient de lancer la vague ne comptent pas (1,5 s). Les flèches disparaissent en touchant le gong au lieu de s'y planter. La récompense de la vague augmente de la part du chrono qui restait (gong à mi-chrono : +50 %).
- **Un boss différent à chaque vague** (depuis le 12 octobre au soir ; le chevalier géant est retiré) : Thomas (un champignon géant, qui remplace le Roi Gluant depuis le 15 octobre), Jean (la mimique), Gontrand (la tortue), Francine (le chef orc) et Nolan (la liche) (section 10). Aucun ne vole : un boss volant serait injouable. En mode infini, ils reviennent tour à tour, à plusieurs. Le **boss final**, MK, le dragon rouge (pack *Four Evil Dragons PBR*), attend à la vague 6, dans son antre (section 10 bis).
- Leurs PV suivent la courbe de la difficulté (en Normal, de 320 PV à la vague 1 à environ 1 500 à la vague 5). Après les tests du 12 octobre, la fin de la première vague était trop dure : l'ancien boss avait 600 PV, et ses renforts submergeaient le joueur. Les boss appellent maintenant 2 ou 3 renforts toutes les 18 à 20 s, plus un ennemi de la vague toutes les 6 s environ.
- **Victoire** à la mort du boss final, à la vague 6. Le joueur choisit ensuite :
  - **continuer en mode infini** en frappant le gong ;
  - ou **terminer la partie** avec le bouton « Terminer la partie » de la boutique. L'écran de fin affiche alors « Victoire ! », avec le nom à saisir et le classement.

  Le bouton reste dans la boutique à chaque pause du mode infini.
- **Mode infini** : la courbe continue de monter, et les boss sont plusieurs : deux aux vagues 7 et 8, trois aux vagues 9 et 10, puis un de plus toutes les 2 vagues.

**L'or vient surtout de la fin des vagues.**

- **Éliminations** : 5 % des points deviennent de l'or (au lieu de 25 %). L'or ne baisse pas pendant les 6 vagues ; la baisse de 9 % par vague ne reste qu'en mode infini, à partir de la vague 7.
- **Récompense de fin de vague** : 80 + 110 × le numéro de la vague, soit 190 or à la vague 1, 630 à la vague 5 et 740 à la vague 6. Elle est multipliée par le multiplicateur de score de la difficulté, et divisée par deux si la tour est détruite.
- **Bonus de performance** : chacun ajoute +20 % de cette récompense.
  - **Tour intacte** : la tour n'a perdu aucun PV pendant la vague.
  - **Boss express** : le boss est mort moins de 30 s après son arrivée.
  - **Combo** : un combo d'au moins 10 touches pendant la vague.

  Le message de fin de vague liste les bonus obtenus, avec celui du gong.
- **Résultat de la simulation du 12 octobre** (5 vagues, anciens prix de la boutique ; un bon joueur, la moitié des bonus) : environ 4 000 or sur la partie en Normal, à peu près comme avant sur 10 vagues, dont un tiers par les éliminations. En Impossible, environ 9 000.
- **Le score ne change pas** : les points restent ceux de la section 8. Les points de fin de vague passent à 100 × le numéro de la vague.

**Les courbes « par vague » ont été recalées le 12 octobre**, quand la partie est passée de 10 à 5 vagues : la vague 5 est aussi dure que l'ancienne vague 10. La vague 6 va un cran plus loin.

| | Facile | Normal | Difficile | Impossible |
|---|---|---|---|---|
| PV des ennemis, à chaque vague | ×1,14 | ×1,19 | ×1,27 | ×1,34 |
| Dégâts subis, à chaque vague | ×1,07 | ×1,09 | ×1,14 | ×1,19 |
| Nombre d'ennemis, à chaque vague | ×1,09 | ×1,12 | ×1,16 | ×1,21 |
| Vitesse des ennemis, à chaque vague | ×1,02 | ×1,05 | ×1,06 | ×1,07 |

- **Début plus doux en Impossible** : ×0,5 à la vague 1, et ×1 dès la vague 3.
- **Ennemis par vague** : budget de 20 à la vague 1, +10 par vague. Cela fait environ 20 ennemis à la vague 1 et 80 à la vague 5 en Normal, sans compter les renforts du boss. Les vagues sont plus longues, donc moins denses qu'avant.
- **Prix de la boutique** : +24 % par vague au lieu de +10 % (même hausse totale sur la partie), et +10 % par achat. En mode infini : ×1,3 par vague (section 7).
- **Mode infini** (à partir de la vague 7) : par vague, PV des ennemis ×1,25, dégâts ×1,14, nombre ×1,19 et vitesse ×1,07, en plus de la courbe de la difficulté.
- **Cristaux** : deux séries de gisements par vague. Chaque série en compte 3, avec un de plus toutes les 2 vagues (6 au plus). Un gisement donne 20 matériaux, +6 par vague. En récoltant 70 % des gisements, on a environ 900 matériaux à la fin de la vague 5, comme avant à la vague 10.
- **Coffres** : 2 par vague (à 10 % du chrono, puis à 45 %), et 50 % de chance d'un troisième à 70 %. Un coffre ouvert disparaît 15 s plus tard, avec ses orbes s'il n'en a pas pris : on ne le confond plus avec un coffre neuf.

**Montre : retour à la tour.**

- Un bouton « Tour », sous la montre, téléporte le joueur en haut de la tour, avec un fondu, comme le téléporteur.
- **On le touche, on ne le clique pas.** On approche la manette de l'autre main et on la garde 0,3 s dessus ; un remplissage montre l'appui.
  - La montre est au poignet de la main libre, et l'autre main tient souvent l'arc, ce qui l'empêche de cliquer au rayon.
  - Le temps d'appui évite les appuis par accident.
- **Temps de recharge** : 25 s pendant les vagues, affiché sur le bouton (« Tour 12 s »). Rien entre les vagues, comme le bouton « Retour à la tour » de la boutique.
- Le bouton est grisé tant que la tour est détruite (« Tour détruite »), et caché quand le joueur est déjà en haut.
- **Pourquoi** : revenir défendre plus vite rend les sorties sur la carte (cristaux, coffres, constructions) moins risquées.

## 4. Le tir à l'arc

### 4.1 Le geste

1. Prendre l'arc avec le bouton de poignée, dans n'importe quelle main.
2. Attraper une flèche au-dessus de l'épaule, dans une zone invisible derrière la tête qui suit la rotation horizontale du casque.
3. Encocher : approcher l'arrière de la flèche de la corde (à 20 cm près), ou poser la flèche sur le repose-flèche ; elle s'accroche à la corde.
4. Tendre : reculer la main. La puissance dépend de la distance de tirage (main à environ 40 cm de l'arc à tension maximale, selon l'arc : un quart de tension en moins depuis les tests du 12 octobre, l'arc était trop dur à bander ; tension maximale 26 cm, 30 cm pour l'arc long). La corde suit encore la main jusqu'à 15 cm plus loin (décidé le 18 octobre, elle s'arrêtait net à la tension maximale), sans plus de puissance : l'anneau de timing apparaît toujours à la tension maximale (réglage *Overdraw Distance* de chaque arc). La vibration et le grincement augmentent avec la tension.
5. Lâcher le bouton : la flèche part, propulsée par le moteur physique.

On voit des mains d'archer gantées de cuir à la place des manettes : le gant low-poly de Quaternius. Elles se ferment avec la poignée et la gâchette, serrent la poignée de l'arc sous le repose-flèche, tiennent la flèche dans le poing et crochètent la corde à l'encoche (prise méditerranéenne : index au-dessus de la flèche). Elles tendent l'index vers les menus. La main droite porte un gant à trois doigts : pouce et auriculaire nus. Le composant `GlovedHands` du XR Origin les règle ; le décocher rend les manettes.

### 4.2 L'anneau de timing

- Il apparaît quand la corde est tendue au maximum, sur l'arc, près de la main.
- C'est un anneau épais, vide au centre. Ses bandes, de l'extérieur vers l'intérieur : rouge, orange, vert, puis vert pastel. Les trois premières suivent un feu tricolore : rouge pour un tir raté, orange pour un bon tir, vert pour un tir parfait. Aux premiers tests, l'ancien doré du tir parfait se confondait avec du jaune.
- Un cercle d'approche rétrécit du bord vers l'intérieur (en 1,2 s du bord au centre avec l'arc de départ). La bande où il se trouve au moment du lâcher donne la qualité du tir.
- Une vibration et un « ding » marquent l'entrée dans le vert, pour le sentir sans regarder l'anneau.
- Après le vert, le cercle finit sa course dans le vert pastel et s'y arrête jusqu'au tir : l'anneau ne recommence plus. Un tir « très bon » va aussi loin qu'un tir parfait, mais fait moins de dégâts et rapporte moins de points. Aux tests, la boucle était frustrante pour les débutants : après avoir raté le vert, il fallait attendre un tour complet, en passant par le rouge.
- Un tir lâché avant la tension maximale est faible et sans bonus.

| Qualité | Bande | Vitesse | Portée | Dégâts | Points |
|---|---|---|---|---|---|
| Parfait | vert | ×1,5 | ×2,25 | ×2 | ×2 |
| Très bon | vert pastel (le cercle y attend) | ×1,5 | ×2,25 | ×1,6 | ×1,6 |
| Bon | orange | ×1,15 | ×1,3 | ×1,25 | ×1,25 |
| Raté | rouge | ×0,6 | ×0,36 | ×0,75 | ×0,5 |
| Sans anneau (tension incomplète) | — | ×0,6 | ×0,36 | ×0,75 | ×0,5 |

La portée varie comme le carré de la vitesse : un tir parfait va environ 6 fois plus loin qu'un tir raté. Ces valeurs se règlent dans `Data/ShotTuning`, et la largeur des bandes dans chaque arc (`Data/Bows`). Un tir parfait déclenche aussi un effet visuel et sonore.

### 4.3 L'aide à la visée

Pendant la tension, un trait part de la pointe de la flèche. Il devient plus visible à mesure que la corde se tend, et il prend la couleur de l'anneau à cet instant (blanc avant la tension maximale, puis rouge, orange, vert ou vert pastel). Sa forme dépend de la difficulté (section 11) :

- **Facile et Normal : la trajectoire complète.** Le trait suit la courbe qu'aurait la flèche lâchée maintenant, chute comprise, jusqu'à son point d'arrivée, marqué d'un petit cercle. La courbe s'allonge avec la tension et avec la bande de l'anneau : un tir rouge tombe vite, un tir vert ou vert pastel va loin.
- **Difficile : une ligne droite** dans l'axe de la flèche, sans la chute. Elle aide à viser sans tout faire à la place du joueur, et s'arrête au premier obstacle. Aux premiers tests, il n'y avait aucune aide en Difficile.
- **Impossible : aucune aide.**

La trajectoire est calculée comme le vol de la flèche (gravité, sans frottement), avec la vitesse qu'elle aurait au lâcher : arc, tension, bande de l'anneau et améliorations de vitesse comprises. Réglage *Aim Guide Mode* de chaque difficulté (`Data/Difficulties`).

### 4.4 La flèche

- `Rigidbody` soumis à la gravité, avec collisions continues ; la flèche s'oriente dans le sens de son vol.
- L'impact est détecté par un rayon entre deux positions successives, ce qui reste fiable à grande vitesse.
- Elle se plante dans ce qu'elle touche, puis disparaît au bout de quelques secondes.
- Les flèches sont illimitées. Elles sont réutilisées (pool d'objets) au lieu d'être recréées à chaque tir.

### 4.5 Coup de flèche au corps à corps

- On frappe un ennemi avec la pointe de la flèche tenue en main (pas encochée), d'un geste franc (pointe à plus de 2 m/s) : il subit les dégâts d'un tir orange de l'arc, bonus de dégâts compris. Un coup à la tête compte double.
- La flèche reste plantée dans l'ennemi ; il faut en reprendre une dans le dos.
- Le coup compte comme une touche (points, combo, vampirisme). (Le brasero, qui enflammait les flèches, a été retiré le 12 octobre.)
- C'est surtout utile en début de partie. L'attaque ne profite pas des flèches spéciales (multitir…), elle devient donc moins intéressante ensuite.

### 4.6 La grenade de flèches

Confiée à un coéquipier le 12 octobre, qui la code et la monte sur sa propre branche. La première version, codée le 5 octobre et jamais montée, a été retirée pour qu'il parte de zéro.

- On la prend en bas du dos, de la main droite ou de la main gauche (pour les gauchers), et on la lance. (Depuis le 22 octobre : à la hanche, comme dans une poche.)
- Elle explose à l'impact. L'explosion ne fait pas de dégâts, mais projette plein de flèches dans toutes les directions.
- Recharge : 15 s, affichée sur la montre.
- Deux nouvelles améliorations rares : la grenade envoie plus de flèches ; la grenade se recharge plus vite.
- **Faite par le coéquipier (dovinvilson), fusionnée le 22 octobre** (branche `feature/grenade-fleches` de son fork, pas encore testée dans le casque) : une ceinture invisible dans le bas du dos, juste sous la zone du carquois (scripts `Grenade/GrenadeBelt`, `ArrowGrenade`, `GrenadeWatchUI`, prefab `Prefabs/Grenade/Arrow Grenade`). Une main vide qui serre la poignée en sort une grenade ; lâchée, elle part avec la vitesse du bras et éclate au premier impact (ou au bout de 6 s) en 14 flèches à 16 m/s, réparties en sphère et poussées vers le haut pour retomber en pluie, de 6 dégâts chacune (améliorations de dégâts comprises). Une flèche de grenade perdue ne casse pas le combo. Recharge 15 s (4 s au moins avec les améliorations), affichée sur la montre (« Grenade prête », « Grenade 12 s », « Grenade en main »). Boutique : « Grenade sauvage » (6 flèches de plus) et « Grenade rapide » (recharge 30 % plus courte), rares. Pendant la pause du menu, la ceinture ne donne pas de grenade, et une grenade lâchée retourne à la ceinture sans recharge (22 octobre).
- **Après les premiers tests (22 octobre, codé, pas encore testé)** : elle ne semblait pas faire de dégâts. Ses flèches naissaient à plus d'un mètre du centre de l'explosion (25 cm de décalage plus la longueur de la flèche, détectée depuis sa pointe), donc au-delà de l'ennemi touché, et un tiers partait dans le sol. Désormais : les pointes partent du centre ; jusqu'à 6 flèches visent les 3 ennemis les plus proches dans un rayon de 7 m (2 chacun, un peu au-dessus de leur centre pour compenser la chute), les autres partent tout autour, entre 8° sous l'horizontale et 30° au-dessus (à hauteur d'ennemi). Elle éclate au contact d'un ennemi ou en le frôlant (35 cm), ainsi qu'au sol ; pas sur l'arc ni sur le joueur. **Tenue** : prise instantanée, collée à la main même en se déplaçant (elle était tirée vers la main par la physique et décrochait), sans collision avec le corps du joueur. **Vol** : vitesse du bras ×2 au lâcher (×1,5 avant), gravité ×0,7, mouvement lissé. **Prise** : la zone passe aux **hanches, comme une grenade qu'on sort de sa poche** (devant, sur le côté ou derrière) : de 0,95 à 0,45 m sous les yeux, de 35 cm derrière les yeux à 12 cm devant, sur toute la largeur du corps (elle allait de 1,05 à 0,62 m sous les yeux et uniquement derrière : au niveau des cuisses, loin dans le dos ; un premier essai au bas du dos, le même jour, obligeait encore à aller la chercher loin derrière). Elle s'arrête juste derrière le katana et le bouclier rangés à la hanche, et une main qui vise ou touche déjà ce qu'elle peut prendre (un emplacement de construction au bout du rayon, une arme rangée, un cristal, un coffre…) le prend, pas une grenade : sinon, viser un emplacement au sol la main basse sortait une grenade (corrigé le 22 octobre). Le carquois s'arrête au-dessus (0,42 m sous les yeux au lieu de 0,6 : il prenait le bas du dos). La petite vibration d'entrée dans la zone revient au plus toutes les 2 s (en courant, les bras balancés y passent sans arrêt) ; la ceinture reprend le son du carquois. **Montre** : la ligne de la recharge (et celle des bonus des coffres) étaient des textes 3D dans l'interface de la montre, aux lettres d'1 mm : ce sont maintenant des textes d'interface, comme les autres lignes (`GrenadeTuningBaker`, à enregistrer avec Ctrl+S). **Tutoriel** : une fenêtre « La grenade à flèches » dans la catégorie Tir (vidéo `Tir_3` à tourner). **Visuel** (choisi le 22 octobre) : la bombe de Quaternius (*Bomb*, CC0, via Poly Pizza), réduite à 11 cm, en fonte sombre avec une mèche de corde. La mèche du modèle est éteinte : des étincelles qui jaillissent (2,5 fois plus une fois lancée, elles traînent derrière elle), une lueur qui vacille et un filet de fumée la font brûler.
- **Renforcée le 22 octobre (codé, pas encore testé)** : **28 flèches** au lieu de 14 (12 visées sur les ennemis proches au lieu de 6) ; chaque flèche fait **80 % d'une flèche vert clair** (« très bon ») de l'arc tendu à fond, améliorations et bonus des coffres compris (12,8 avec l'arc de chasse ; avec les Akimbow ou le katana en main, l'arc du début de partie) ; « Grenade sauvage » donne **12 flèches de plus** au lieu de 6. **Le moindre contact la fait éclater**, sans délai (sol, mur, barricade, ennemi…), sauf le joueur, son arc, ses flèches et ses armes. **Mega-bomba** : pendant son vol, une zone de 60 cm de rayon l'entoure ; une flèche tirée par le joueur qui y passe (arc, Akimbow, déchirure du katana, ou le laser de l'arc cosmique) la fait éclater en **trois fois plus de flèches** (84, améliorations comprises), avec une explosion plus grosse et plus grave et « MEGA-BOMBA ! » en grand au-dessus de la bombe.

## 5. Les arcs

**Depuis le 19 octobre : les classes d'arc** (branche de Cider-Jude). On choisit son arc **avant la première vague**, dans le menu à droite du menu principal, parmi les arcs débloqués par les **succès** (gardés d'une partie à l'autre). La boutique ne vend plus d'arcs.

| Arc | Vitesse de la flèche | Dégâts | Anneau | Pouvoir | Débloqué par |
|---|---|---|---|---|---|
| Arc de chasse | 35 m/s | 10 | 1,2 s | — (arc par défaut) | dès le départ |
| Arc court | 30 m/s | 7 | 0,8 s | — (léger, rapide à tirer) | *Chasseur* : tuer 100 ennemis |
| Arc puissant | 45 m/s | 17 | 1,5 s | — (lent à tendre, frappe fort et loin) | *Œil de faucon* : 10 tirs parfaits à la tête |
| Arc runique | 40 m/s | 13,5 | 1,2 s | ses flèches traversent 1 ennemi | *D'une pierre deux coups* : 2 ennemis d'une flèche |
| Akimbow (deux arbalètes) | 45 m/s (gravité ×0,85) | 4 (0,4 × l'arc de chasse) | 0,6 s, repart seul après chaque tir | une arbalète dans chaque main, la gâchette tire | *Intrus* (secret) : réussir l'épreuve du toit cyberpunk |
| Katana et gantelet | faille lancée : 34 m/s (×1 à ×1,5 selon la qualité), elle suit sa cible | coup : 30 (×1 à ×1,6 selon la vitesse) ; faille : 15 à chaque ennemi traversé | — (fraîcheur de la faille) | une taille de katana ouvre une faille, le gantelet la frappe et l'envoie ; coups au corps à corps | *Rōnin* : vaincre 2 boss de vague |

**L'arc cosmique** (fait le 19 octobre) : il se gagne en réussissant l'épreuve des étoiles (le portail des monolithes) : en main tout de suite pour le reste de la partie, et débloqué pour toujours dans le menu des arcs (succès secret « Voyageur des étoiles » ; sa carte, verrouillée, ne dit que « Un mystère irrésolu se trouve autour des monolithes. »). Caractéristiques de l'arc runique, sans flèches qui traversent ; pouvoir **OVERCHARGE** : le vert de l'anneau devient doré, et quand le cercle a fini sa course, un deuxième cercle doré part du bord (0,9 s). Lâché dans son doré : un **laser** instantané en ligne droite, qui traverse tout (ennemis, murs, relief) sur 120 m, 3 fois les dégâts d'un tir parfait, points faibles compris, sans limite. Lâché après, dans le vert clair où le cercle finit sa course (décidé le 20 octobre) : le même laser, un peu moins fort (3 fois un tir « très bon », soit 80 % du laser doré). Lâché avant le doré : un tir « très bon ». Tant que lâcher tirerait le laser, la ligne de visée devient droite, sur toute sa portée et à travers les obstacles, comme lui (20 octobre). **Le rayon** (revu le 20 octobre, plus gros et plus lumineux) : un cœur blanc, un rayon bleu autour, un large halo violet, et deux spirales, une bleue et une violette, enroulées l'une dans l'autre, qui tournent et s'écartent en s'éteignant ; plus fin près de l'arc pour ne pas éblouir. Il laisse une traînée de particules bleues et violettes le long de son passage. Ses flèches normales laissent une traînée d'étoiles. Avec lui en main, les morceaux de l'arc légendaire ne tombent plus. Le laser traverse les menus et les panneaux de difficulté sans les déclencher. **Son apparence** : le modèle *Enchanted Bow* de Zsky (CC-BY 3.0, voir les crédits), sans sa corde ; son corps est une « fenêtre sur l'espace » (shader `CosmicSurface` : une nébuleuse violette vue à travers l'arc, très loin, comme un ciel, qui glisse quand on tourne l'arc, avec des étoiles qui scintillent et un liseré violet), ses ailettes brillent en violet et sa poignée est presque noire. Le préfab est assemblé par l'éditeur (`CosmicBowBaker`, menu *Archery > Arc cosmique > Refaire le modèle*). **Ses sons** (freesound, CC0, recoupés) : une charge qui monte pendant le deuxième cercle et s'arrête net quand on lâche, et une décharge de canon à ions, saturée (graves renforcés, distorsion à lampe ; un peu moins saturée depuis le 20 octobre), pour le laser. Ses dégâts : ceux de l'arc de chasse (10, depuis le 20 octobre au soir ; 13,5 avant), le laser compris (3 fois un tir parfait de 10).

**L'arc légendaire** (fait le 20 octobre) : il se gagne en trouvant ses **3 fragments** dans les coffres. Tant qu'il n'est pas débloqué, un coffre a **10 %** de chance de proposer un fragment (une orbe orange, à la place de sa deuxième amélioration permanente ; jamais avec l'arc cosmique en main). Les fragments se **gardent d'une partie à l'autre** : sa carte, verrouillée, affiche le succès « Trois fragments » et « Fragment 1 / 3 ». Au troisième, l'arc arrive tout de suite en main et reste débloqué pour toujours. Caractéristiques : 42 m/s, 8,4 dégâts (15 jusqu'au 20 octobre au soir, puis 10,5 : -30 %, puis -20 % ; la pluie et la zone au sol, en part du tir, baissent d'autant), anneau de 1,2 s aux bandes un peu larges, sans perforation ; ses flèches sont deux fois plus lourdes (gravité ×2, 20 octobre) : elles tombent plus vite et vont deux fois moins loin, l'aide à la visée suit. Pouvoir **PLUIE DE FLÈCHES** : au sommet de sa course, la flèche se divise ; 10 flèches retombent en pluie dans un cercle de 3,5 m autour de son point d'arrivée (chacune la moitié des dégâts du tir, qualité comprise), et une **zone brûlante** reste au sol 3 s (30 % des dégâts du tir par seconde à chaque ennemi au sol dedans ; braises, crépitement). Plantée avant son sommet (tir tendu), la flèche fait tomber la pluie du ciel sur ce point. L'aide à la visée montre la courbe et le cercle de la pluie, même en Difficile (rien en Impossible). Son modèle : *Bowblade* de Tijmen Matthys (CC-BY 4.0, voir les crédits), un arc aux branches orange et or et aux lames argentées, peint à la main, sans sa corde, 1,15 m de haut, un peu lumineux ; le préfab est assemblé par l'éditeur (`LegendaryBowBaker`, menu *Archery > Arc légendaire > Refaire le modèle*) ; sons provisoires (le tir d'arc pour la division, le feu de camp pour la zone).

**Les Akimbow** (faits le 20 octobre) : deux arbalètes, une dans chaque main, à la place de l'arc. Elles se gagnent en réussissant l'épreuve du toit cyberpunk (section 23) : en main tout de suite pour le reste de la partie, et débloquées pour toujours (succès secret « Intrus » ; sur la carte verrouillée, l'énigme du joueur : « Il te voit clair comme de l'eau de roche. »). Elles **flottent à côté du joueur, une de chaque côté** (à hauteur de hanche, un peu devant, comme l'arc rangé) : on en prend une dans chaque main avec la poignée de la manette (sa poignée tombe dans la paume, canon dans l'axe de visée, et la main gantée se referme sur la poignée, comme sur celle de l'arc ; la montre de ce poignet monte de 6 cm pour passer au-dessus du fût), et lâchée, elle retourne à sa place (depuis le 20 octobre au soir ; avant, elles étaient collées aux mains). La gâchette de la main qui en tient une tire une flèche tout droit ; pas de corde ni de carquois. Les mains qui les tiennent ne prennent rien d'autre : on lâche une arbalète pour ramasser un matériau ou ouvrir un coffre. **Chaque arbalète a son anneau de timing**, à côté d'elle, qui repart tout seul (et vite : 0,6 s) après chaque tir : tirer pendant le rouge, l'orange, le vert ou le vert clair donne la qualité de tir habituelle (vitesse, dégâts, points). **Chaque arbalète a son aide à la visée**, selon la difficulté, dans la couleur de son anneau. Caractéristiques : **0,4 fois les dégâts de l'arc de départ** (4 ; 0,66 puis 0,5 avant, le 20 octobre), une **bonne portée** (45 m/s, gravité ×0,85). Les améliorations et les coffres s'appliquent comme à l'arc (multitir, écho, feu, perçage, anneau…). La gâchette ne tire pas quand la main vise un menu. Rangées avec l'arc (construction, dimensions). Leur modèle : le *Sci fi crossbow* d'oarfish (CC-BY 4.0), réduit (×0,7, 38 cm), un look cyberpunk **stable** (shader `Archery/Akimbow`, le modèle n'a pas d'UV : un motif calculé dans son repère) : métal sombre en plaques de deux teintes, joints dont certains s'allument, une bande néon sur les flancs, une ligne sur le dessus, quelques LED, un léger contour néon, et des néons en relief sur le rail, les branches, l'avant et les flancs. Le métal n'est pas uni (20 octobre au soir) : des taches plus claires ou plus sombres, et des touches de couleur par-ci par-là (des plaques peintes en partie, de petites étiquettes sur les flancs, des rayures de danger sur certaines branches). **Les deux armes sont différentes** : à gauche, métal bleu acier, néons cyan et bleus, touches ambre et gris clair, plaques serrées ; à droite, métal prune, néons roses et orange, touches sarcelle et jaunes, plaques plus larges (20 octobre au soir ; avant, le matériau du toit clignotait bleu et violet). De temps en temps (toutes les 2,5 à 6 s, pendant un instant), elles buguent comme la porte glitchée : quelques morceaux sombres, l'image brouillée, un sursaut de quelques millimètres. Sans casque : devant la caméra, le clic gauche tire une main puis l'autre. Dans le menu des armes, leur carte (cyan) montre les deux arbalètes debout ; le menu s'élargit pour tenir 7 cartes. Fiche, aperçu et carte faits par l'éditeur (`AkimbowBaker`, menu *Archery > Akimbow*).

**Refait le 23 octobre, après les tests : les failles** (codé, pas encore testé dans le casque ; choix du joueur). Les symboles kanji sont retirés (`KanjiGlyphs`, `KanjiGuide`, shader `KanjiBrush`). **Une vraie taille de katana ouvre une faille** : un coup rapide (la pointe à 3,5 m/s au moins) d'au moins 40 cm, avec 30 cm entre son début et sa fin (un va-et-vient ou une épée agitée sur place ne compte pas), devant le joueur (à 75° au plus de son regard : pas en balançant les bras pour courir), et 0,25 s au moins après la faille précédente. **Tout se mesure par rapport à la poitrine du joueur** : avancer au joystick, courir aux bras ou marcher dans la pièce ne coupe plus rien (avant, en se déplaçant, on coupait dans le vide, et en agitant l'épée devant le gantelet on lançait des flèches à la chaîne). La faille suit le point de la lame qui passe à 62 cm de la poitrine : elle s'ouvre à portée du gantelet, là où la lame a tranché (ouverte plus loin, lame tendue, elle glisse à 60 cm). **Elle s'ouvre comme un œil** (demandé le 23 octobre) : d'abord une fine fissure qui brille et grésille (0,14 s, un petit craquement aigu), puis elle s'ouvre d'un coup (0,1 s, avec un léger dépassement, un craquement franc et une vibration), le milieu avant les coins, plus large au milieu qu'aux bouts (jusqu'à 12 cm pour une grande taille). **Elle vit** (shader `Archery/MatrixTear` refait : le maillage ne donne que le tracé, le shader le déforme à chaque image) : ses bords ondulent et restent déchiquetés, son tracé frémit (ancré aux deux bouts), elle respire, et ses fissures secondaires rampent, en s'allongeant et en se rétractant chacune à son rythme ; dedans, toujours le vide et les 0 et 1 verts de Matrix. **Frappée avec le gantelet** (un coup de poing à 1,3 m/s au moins par rapport au joueur, la paume à moins de 40 cm de la faille), elle part vers l'ennemi le plus proche du centre de la vue (plusieurs failles frappées d'un coup : une par ennemi, dans l'ordre), le suit, et toute la faille touche (15 × la qualité à chaque ennemi traversé, par sa zone de touche). **Sa fraîcheur donne la qualité**, comptée depuis son ouverture : moins de 0,4 s, parfaite (vert) ; moins de 1 s, bonne (orange) ; après, ratée (rouge) ; les améliorations de l'anneau élargissent ces fenêtres. Ses bords prennent la couleur de la qualité qu'aurait le tir à cet instant, et les lueurs du gantelet palpitent tant qu'une faille attend (vite pendant la fenêtre parfaite). Elle se referme au bout de 2,2 s ; 3 failles ouvertes au plus (une 4e referme la plus ancienne). **La faille et la lame déclenchent aussi ce qui réagit aux flèches** : le gong (au katana, on ne pouvait pas lancer de vague), les cibles d'entraînement, les cibles « Continuer » et « Terminer » après MK, les boules de feu et les projectiles ; pas les boutons des menus ni les panneaux de difficulté, qui se visent au rayon de la main. La lame les déclenche une fois par seconde au plus chacun (on ne fait pas venir le boss en frappant le gong deux fois d'un même geste). Sans casque : clic gauche, un coup de katana qui balaie la vue (il frappe au corps à corps et ouvre une faille) ; clic droit, le gantelet frappe toutes les failles ouvertes. Scripts `KatanaGauntlet`, `MatrixRift` (la faille), `MatrixRiftMesh` (son tracé) ; texte de la carte et fiche mis à jour par `KatanaBaker`.

**Refait le 22 octobre : le katana et le gantelet** (idée du joueur ; remplacé le 23 octobre par les failles ci-dessus). Le bouclier est retiré, remplacé par **un gantelet** porté sur la main de l'arc (l'*Energy Gauntlet* d'ENOMIC, chrome et lueurs cyan, CC-BY) : il ne sert qu'à envoyer les signes. **Le katana** (celui de Pichuliru, CC0) a une **lame luisante** dans le style des Akimbow et de Katana Zero (shader `Archery/KatanaBlade` : acier sombre, fil néon cyan, un balayage de lumière qui monte vers la pointe, des lignes numériques ; de temps en temps, et à chaque trait réussi ou signe lancé, la lame **bugue** : des tranches se décalent, passent au magenta ou disparaissent un instant). On ne déchire plus l'air en balançant le katana dans tous les sens : **un symbole style kanji** (2 ou 3 traits, tous droits depuis le 23 octobre : 十 人 八 乂 二 卜 工 土 士 干 大 川 三 千 丫 ; les symboles à crochets ou à angles, 力 刀 又 七 et deux inventés, étaient trop durs à tracer d'un coup de sabre, et 入 ressemblait à 人 une fois redressé) apparaît devant le joueur (à 95 cm depuis le 23 octobre), en coups de pinceau lumineux, une lueur montrant le sens de chaque trait. Le joueur trace chaque trait **d'un geste rapide de la lame** à travers le symbole (dans l'ordre et le sens qu'il veut) : le geste compte pour le trait qu'il suit le mieux, s'il le suit d'assez près. Là où la lame a coupé, **la matrice se fend** : une vraie fissure, le long du geste (donc en travers de la lame, plus parallèle à elle), au tracé en zigzag, aux bords déchiquetés et lumineux, avec des fissures plus fines sur les côtés ; dedans, le vide, où tombent des **colonnes de 0 et de 1 verts, comme dans Matrix** (shader `Archery/MatrixTear` refait). Le symbole tracé, le signe prend **la qualité du tracé** (écart moyen entre les gestes et les traits : vert, vert clair, orange ou rouge, élargis par les améliorations de l'anneau comme pour l'arc), qui s'affiche, et le gantelet palpite. **Touché avec le gantelet**, le signe part **vers l'ennemi le plus proche du centre de la vue** (à 70° au plus, sinon 120°, à 90 m au plus ; sans ennemi, droit devant le regard), le suit en virant (540°/s), et **tout le signe est la flèche** : chaque ennemi qu'il traverse prend ses dégâts une fois (15 × la qualité, améliorations comprises, par sa zone de touche : tête ×2, gemme ×3), puis il se brise sur sa cible (ou au bout de 3 s). Il compte comme un tir pour le score, le combo et les succès (pas pour les effets de flèches des améliorations, comme le feu ou la foudre). Puis un autre symbole apparaît. Un signe prêt attend 12 s ; un signe commencé s'abandonne après 7 s sans nouveau trait. Le katana garde ses **coups au corps à corps** et coupe toujours les projectiles. Sans casque : clic gauche maintenu en balayant un trait avec le viseur (le symbole se fige), relâché à la fin du trait ; clic droit pour lancer le signe. Sons : un « swoosh » pour chaque geste (Danjocross, CC0), du verre qui se fend pour chaque trait réussi, un glitch pour le signe complet, un sabre laser pour le lancer (h2p34, CC0). Modèles, matériaux et carte refaits par l'éditeur (`KatanaBaker`, menu *Archery > Katana et gantelet*). **Corrigé le 23 octobre, après le premier test** (aucun trait ne passait, même sur 十, sauf en avançant au joystick, en rafale) : un vrai coup de sabre dépasse des deux bouts du trait, et le geste était comparé point par point au trait, avec une pénalité de longueur : presque tout était refusé. Désormais, c'est **la pointe de la lame**, ramenée dans le plan du symbole, qui trace ; un geste compte s'il **passe près de chaque point du trait** (dépasser des bouts ou aller dans l'autre sens ne gêne plus) **et va dans sa direction** (un coup en travers est refusé) ; un geste refusé fait clignoter le symbole en rouge. Le symbole et le signe en cours **suivent le XR Origin** : marcher ou tourner au joystick ne les fait plus traverser par la lame (c'est ce qui donnait la rafale). Le symbole est **plus grand et plus loin** : 76 cm de côté (52 avant), à 95 cm devant les yeux et 12 cm plus bas (70 et 18 avant). Qualité : écart moyen sous 5 cm (parfait), 8 (très bon), 12 (bon) ; refusé au-delà d'environ 17 cm.
Le même jour : à 95 cm, le signe terminé était hors de portée du gantelet. **Prêt, il vient maintenant au joueur** : il glisse à 50 cm devant les yeux, 22 cm plus bas et 12 cm du côté de la main de l'arc, réduit à 60 % (il reprend sa taille en partant), et suit le regard ; le gantelet le lance à moins de 18 cm.
**Décision du 23 octobre, après un nouveau test : le système de symboles (kanji) est retiré** (fait le même jour : voir « les failles », plus haut). Prochaine version du katana, à faire : **garder le gantelet** (il lance la faille) **et la faille dans la matrice** (les 0 et 1 qui tombent) ; **retirer les symboles** (`KanjiGlyphs`, `KanjiGuide`, la reconnaissance des traits, le shader `KanjiBrush`) ; sans doute revenir aux traces laissées librement par les coups rapides, lancées en touchant la faille avec le gantelet (à confirmer). **Améliorer la faille** : on doit vraiment avoir l'impression d'ouvrir le jeu, avec une faille presque vivante, qui **s'ouvre comme une plaie** (plus large au milieu qu'aux extrémités, avec une animation d'ouverture), et dont **la fissure et la forme bougent** sans cesse (bords et tracé animés, pas un maillage figé). Le détail est dans `Docs/Reprise_Projet.md`.
L'ancien fonctionnement, ci-dessous, est gardé pour mémoire.

**Le katana et le bouclier** (idée du joueur, faits le 20 octobre au soir, prototype puis arme complète ; remplacés le 22 octobre) : à la place de l'arc, un katana dans une main (la main libre) et un bouclier dans l'autre (la main de l'arc). Ils flottent à côté du joueur, un de chaque côté (le katana poignée en haut, le bouclier de chant), et se prennent avec la poignée de la manette ; la main gantée se ferme sur leur poignée ; lâchés, ils retournent à leur place. **Le katana** fait de lourds dégâts au corps à corps quand on le balance vite (dès 2,5 m/s à la pointe : 30, jusqu'à ×1,6 à 7 m/s, tête ×2 comme une flèche, un même ennemi touché au plus toutes les 0,4 s ; les coups comptent pour le combo et les points de touche) et coupe les projectiles qu'il croise. Balancé vite (dès 3,5 m/s), il **déchire la matrice du jeu** : la déchirure (shader `Archery/MatrixTear` : une fente aux bords déchiquetés qui tressaute par à-coups, bordée de cyan et de magenta, avec du code vert qui tombe dans le vide) reste 1,2 s sur place en s'effaçant. **Frappée avec le bouclier** (qui va à au moins 1,2 m/s), la déchirure fonce vers l'avant du bouclier et agit comme une flèche : une flèche part de son milieu (15 dégâts, 34 m/s, gravité normale) et la déchirure la suit en s'effaçant. Sa **fraîcheur** donne la qualité du tir, comme l'anneau d'un arc : frappée moins de 0,3 s après le coup, parfaite (vert) ; moins de 0,8 s, bonne (orange) ; après, ratée (rouge) ; la qualité s'affiche à l'endroit. **Le bouclier tenu** arrête les coups (une attaque qui passe par lui, ou qui vient du côté qu'il couvre, face à elle) et brise les projectiles qui le traversent (« Bloqué ! », un choc métallique). Les améliorations s'appliquent : dégâts (coup et déchirure), vitesse, perçage, largeur de l'anneau (fenêtres de fraîcheur), tirs parfaits des coffres ; la déchirure lancée compte comme un tir de l'arc (multitir, écho, feu…). Pas de carquois. Rangés avec l'arc (construction, dimensions). Sans casque : clic gauche, un coup de katana devant soi (d'un côté puis de l'autre) ; clic droit maintenu, le bouclier levé (il bloque) ; un clic droit lance la dernière déchirure vers le viseur. Modèles low-poly faits par l'éditeur (`KatanaBaker`, menu *Archery > Katana et bouclier*) : un katana à la lame courbe et au fil néon cyan, une garde et un pommeau de métal sombre ; un bouclier rond bleu nuit, cerclé, à l'umbo de métal et à l'anneau néon, poignée au dos. Sons : un « chop » pour les coups, un glitch pour la déchirure lancée, un choc métallique pour le blocage. Carte rouge dans le menu des armes, débloquée par « Rōnin » (vaincre 2 boss de vague).

Raccourcis de test : 5 à 9 et 0 équipent un arc (9 : l'arc cosmique, 0 : l'arc légendaire), la touche à droite du 0 (« ) » en AZERTY) les Akimbow, la suivante (« = ») le katana et le gantelet, O les équipe tout de suite (même verrouillés, même pendant une vague ; O encore : l'arme d'avant), U débloque tout, J efface les succès, L donne un fragment d'arc légendaire.

*Avant le 19 octobre*, un arc de départ, puis des arcs plus puissants à acheter en boutique :

| Arc | Vitesse de la flèche | Dégâts | Anneau | Particularité | Prix | En boutique dès |
|---|---|---|---|---|---|---|
| Arc de chasse | 35 m/s | 10 | 1,2 s | équilibré, arc de départ | — | — |
| Arc composite | 40 m/s | 13 | 1,0 s | anneau rapide, bandes larges | 120 | vague 2 |
| Arc long | 50 m/s | 20 | 1,5 s | puissant mais lent à charger | 200 | vague 4 |
| Arc runique | 55 m/s | 24 | 1,2 s | flèches qui traversent un ennemi | 350 | vague 7 (mode infini) |

## 6. Améliorations et flèches spéciales

### 6.1 Raretés

| Rareté | Couleur | Rôle | Chances |
|---|---|---|---|
| Commune | blanc | statistiques | 70 % |
| Rare | bleu | effets qui se déclenchent parfois, bonus de dégâts plus gros | 25 % |
| Légendaire | doré | effets puissants ou uniques, très gros bonus de dégâts | 5 % |

Les chances sont les mêmes toute la partie. Aux premiers tests, elles montaient avec les vagues, et les boutiques de fin de partie n'avaient plus aucune carte commune. L'amélioration Chance augmente le poids des rares et des légendaires, mais les communes gardent toujours au moins 40 % des tirages.

### 6.2 Liste de départ

**Aucune limite d'achat** : chaque amélioration peut être prise autant de fois qu'on veut, et tout se cumule.

- **Les bonus** s'additionnent : dégâts, vitesse, PV, etc.
- **Les chances** (foudre, explosion, glace) s'additionnent jusqu'à 100 %. Au-delà, chaque exemplaire rend l'effet plus fort (tableau ci-dessous).
- **Les nombres de flèches** (multitir, tir écho, déluge) et le perçage sont des moyennes. Chaque exemplaire en ajoute une part : avec 150 %, on a 1 flèche en plus à coup sûr, et 50 % de chance d'en avoir une deuxième. Il n'y a pas de limite. Seule une sécurité, pour que le jeu reste fluide, arrête d'ajouter des flèches au-delà de 250 en vol en même temps.
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
- Butin : le taux de conversion des points en or passe de 5 % à 10 % (+5 points par exemplaire).
- Chasseur de têtes : +25 % de dégâts à la tête.
- Chance : les cartes rares et légendaires sortent plus souvent en boutique (+25 % de poids face aux communes). Les communes gardent toujours au moins 40 % des tirages.

**Des dégâts dans chaque rareté.** Aux tests, seules les cartes communes augmentaient les dégâts. Chaque rareté a donc la sienne, un peu plus rentable pour son prix à mesure qu'elle est rare : Dégâts (+10 % pour 10), Puissance (+25 % pour 25), Force du géant (+60 % pour 45). Elles s'additionnent entre elles.

**Rares** (surtout des effets qui se déclenchent au hasard)

- Puissance : +25 % de dégâts.
- Multitir : +50 % de chance de tirer une flèche en plus. Avec 2 exemplaires, une flèche en plus à chaque tir ; avec 3, une flèche en plus et 50 % de chance d'une deuxième ; etc. Avec 2 flèches, elles partent côte à côte ; à partir de 3, l'éventail s'ouvre de chaque côté.
- Flèche de foudre : chaque flèche a 20 % de chance d'appeler un éclair qui blesse la cible (75 % des dégâts de la flèche) et la ralentit de 40 % pendant 2 s.
- Perçage : chaque flèche a 25 % de chance de traverser un ennemi ; au-delà de 100 %, elle en traverse plusieurs. Chaque ennemi traversé enlève 20 % des dégâts.
- Vampirisme : chaque headshot rend 2 PV.
- Tir écho : 25 % de chance que la volée se répète 0,25 s après, à la même puissance, multitir compris ; au-delà de 100 %, plusieurs échos, à 0,25 s d'intervalle.
- Flèche de glace : chaque flèche a 25 % de chance de laisser au sol une zone de glace (3 m, 5 s) qui ralentit de 50 % les ennemis au sol.

**Légendaires**

- Force du géant : +60 % de dégâts.
- Déluge : en vol, environ 0,2 s après son départ, chaque flèche a 50 % de chance de se diviser en deux. Avec 2 exemplaires, toujours ; avec 3, en deux, et 50 % de chance en trois ; etc. La flèche continue tout droit, la nouvelle part juste à côté d'elle, et les suivantes s'écartent par paires.
- Chaîne d'éclairs : la foudre rebondit sur 3 ennemis proches de plus par exemplaire. Sans flèche de foudre, 20 % des flèches appellent l'éclair.
- Flèche explosive : chaque flèche a 25 % de chance d'exploser et de toucher tous les ennemis autour (3,5 m).
- Auto-visée : les flèches dévient légèrement (30° par seconde et par exemplaire) vers l'ennemi le plus proche, s'il est à moins de 12 m devant elles. Elles visent son point faible s'il se voit depuis la flèche, sinon sa tête, sinon son centre (17 octobre : un bon tireur n'est plus dévié de la tête vers le ventre).
- Tir ricochet : après avoir touché un ennemi, la flèche rebondit vers un autre ennemi proche (un rebond par exemplaire, 20 % de dégâts en moins par rebond).

**Flèches enflammées** (retirées avec le brasero, le 12 octobre ; le code reste, sans brasero dans la scène) : avec le brasero de la tour, on trempe une flèche dans le feu ; la cible brûle pendant 3 s et perd 30 % des dégâts de la flèche en plus, étalés sur la brûlure. Chaque flèche enflammée ajoute sa propre brûlure. Les flèches en plus d'un tir enflammé brûlent aussi.

## 7. La boutique

- Elle s'ouvre entre les vagues, en haut de la tour : un panneau à gauche du joueur. On vise une carte avec le rayon de la main libre et on achète avec la gâchette.
- Elle propose 4 améliorations tirées au sort selon les raretés, le prochain arc et les services de la tour.
- Relancer les offres coûte 10, puis 5 de plus à chaque relance (le coût revient au départ à chaque pause). Il suit la hausse des vagues et du mode infini.
- Services de la tour : réparation (+25 % des PV, 40), reconstruction après destruction (250), réparation de toutes les barricades (60). Chaque service a sa carte. Le brasero a été retiré le 12 octobre.

**Prix des améliorations.** Aux premiers tests, en Impossible, on gagnait jusqu'à 5 000 pièces d'or vers la vague 10 et l'on achetait des dizaines d'améliorations par pause, au point de faire ramer le jeu. Les prix montent donc de trois façons, qui se cumulent :

- prix de base : commune 10, rare 25, légendaire 45 ;
- **+24 % à chaque vague**, en se cumulant (×2,4 après la vague 5, ×2,9 après la vague 6) ;
- **+10 % à chaque amélioration achetée** en boutique (celles des coffres ne comptent pas), en se cumulant (×2,6 après 10, ×6,7 après 20, ×45 après 40) : plus on en achète, plus la suivante coûte cher ;
- **en mode infini, ×1,3 à chaque vague**, en se cumulant, pour tous les prix de la boutique (améliorations, arc, tour, relance).

Les prix sont arrondis à 5 près.

| Prix d'une rare (commune, légendaire) | 0 achat | 10 achats | 20 achats | 40 achats |
|---|---|---|---|---|
| Après la vague 1 | 25 (10, 45) | 65 (25, 115) | 170 (65, 305) | 1 130 (455, 2 035) |
| Après la vague 3 | 40 (15, 70) | 100 (40, 180) | 260 (105, 465) | 1 740 (695, 3 130) |
| Après la vague 5 | 60 (25, 105) | 155 (60, 275) | 400 (160, 715) | 2 675 (1 070, 4 815) |
| Après la vague 6 | 75 (30, 130) | 190 (75, 340) | 495 (195, 890) | 3 315 (1 325, 5 970) |
| Après la vague 8 (mode infini) | 190 (75, 345) | 495 (200, 890) | 1 280 (510, 2 305) | 8 620 (3 450, 15 515) |

Avec la courbe des ennemis (section 11) et la baisse de l'or en mode infini (section 8), cela compense les achats sans limite : les premières améliorations sont bon marché, mais chaque achat rend la suivante plus chère. Le mode infini finit toujours par déborder le joueur.

## 8. Score et argent

**Points**

- Chaque touche de la flèche tirée par l'arc : 5 points au corps, 10 à la tête. Les flèches en plus (multitir, écho, déluge) ne rapportent pas de points de touche : sinon, les points et l'or grimpaient avec le nombre de flèches.
- Chaque ennemi tué, par n'importe quelle flèche ou effet : sa valeur de base (section 10), doublée s'il meurt d'un tir à la tête.
- Ces points sont multipliés par la qualité du tir (section 4.2), la distance, le combo et la difficulté (section 11).
- Fin de vague : 100 points × numéro de la vague.

| Multiplicateur | Règle |
|---|---|
| Distance | +1 % par mètre au-delà de 10 m, jusqu'à +50 % |
| Combo | +10 % par touche consécutive de la flèche tirée par l'arc, jusqu'à ×2 ; retombe à zéro quand cette flèche ne touche aucun ennemi |

**Argent** : l'essentiel de l'or vient de la récompense de fin de vague, avec ses bonus de performance (section 3 bis). En plus, 5 % des points gagnés deviennent de l'or (l'amélioration Butin augmente ce taux), divisé par deux tant que la tour est détruite. En mode infini, à partir de la vague 7, ce taux baisse de 9 % à chaque vague, en se cumulant.


Le score, lui, ne baisse pas.

## 9. La tour

- 3 000 PV, qui ne remontent pas tout seuls : il faut la réparer en boutique.
- Un téléporteur au pied de la tour mène au sommet, avec un fondu au noir : une bande lumineuse fait le tour de la tour, et on reste debout 1 s dessus, de n'importe quel côté ; on arrive en haut tourné vers les ennemis. Pour redescendre, on marche simplement dans le vide (ou on prend l'escalier) : en arrivant sur la bande, on ne remonte pas tout de suite, il faut d'abord s'en éloigner d'au moins 1 m (depuis le 19 octobre ; avant, il suffisait d'en sortir, et l'escalier le long du mur, qui passe au-dessus de la bande, la réarmait : on était renvoyé en haut au pied des marches). Un cercle au sommet reste possible.
- Pendant la pause, le bouton « Retour à la tour » de la boutique ramène directement en haut. Il est caché quand le joueur y est déjà, et grisé tant que la tour est détruite.
- À la fin d'une vague, le panneau de la boutique s'ouvre devant le joueur, à sa gauche, où qu'il soit. Pendant la pause, il le suit quand il s'éloigne ou se retourne, et ne rentre jamais dans un mur : si sa place est prise, il en cherche une autre autour du joueur.
- Le sommet est une plateforme à environ 6 m, avec une rambarde et le gong. Quand la tour est détruite, des ruines la remplacent (socle, couronne tombée, gravats), et le gong descend à son pied, derrière elle (objet `Gong Ground Spot`), pour qu'on puisse encore lancer les vagues ; il remonte à la reconstruction.
- À 0 PV, la tour s'effondre. Le joueur qui était en haut est téléporté au sol, juste à côté de la bande, de son côté. Le téléporteur ne fonctionne plus et l'argent gagné est divisé par deux jusqu'à la reconstruction.
- **Barricades** : des murs de planches sur les 3 voies, vers 13 m de la tour (400 PV chacun, debout au début de la partie). Les flèches et les carreaux des balistes traversent leurs planches (17 octobre) : un ennemi caché derrière reste touchable. Un ennemi au sol qui arrive devant une barricade, du côté de son apparition (zone de 5 m), s'arrête pour la frapper ; il ne passe qu'une fois qu'elle est détruite. Les tireurs la cassent avec leurs projectiles ; les volants passent au-dessus. En boutique, une réparation remet toutes les barricades debout avec tous leurs PV.
- **Brasero** (retiré de la scène le 12 octobre) : il était allumé en haut de la tour dès le début de la partie. On y trempe la pointe d'une flèche tenue en main : elle s'enflamme. Sa cible brûle 3 s et perd 30 % des dégâts de la flèche en plus, étalés sur ces 3 s. Les flèches en plus d'un tir enflammé (multitir, écho, déluge) brûlent aussi.
- **Matériaux et tower defense** (décidé le 7 octobre, conçu le 11 octobre, équilibré le 12 octobre sur simulation, testé) : aux tests, rester en haut de la tour rapportait plus que d'aller chercher les coffres. Une deuxième monnaie, les **matériaux**, se gagne seulement sur la carte et sert à construire des défenses sur des emplacements fixes.
  - **Cristaux** : pendant chaque vague, des gisements de cristaux lumineux apparaissent dans la jungle et sur les terrasses, chacun surmonté d'un rayon de lumière violet de 30 m qui se voit de loin (17 octobre, les joueurs ne les trouvaient pas), sur des emplacements choisis (`Map > Crystal Spots`, une douzaine), jamais deux au même endroit, à plus de 15 m de la tour. Une série de gisements par vague, au début de l'accalmie (la deuxième, au milieu de la vague, est retirée depuis le 17 octobre) : 3 gisements, un de plus toutes les 2 vagues (6 au plus). On récolte un gisement en le visant et en appuyant sur la gâchette de prise ou sur la grosse gâchette, à 5 m au plus, comme un coffre : il éclate et donne **40 matériaux** (+12 par vague ; 20 % de moins le 20 octobre, c'était 50 et +15 ; 20 et +6 avant le 17 octobre). Ce qui n'est pas récolté disparaît à la fin de la vague. Un message et une cloche annoncent les gisements, une lueur violette les signale de loin, et la montre affiche les matériaux à côté de l'or (« Or 120 · Mat. 35 ») ; à chaque récolte, un message donne le nombre de gisements restants. Modèle : `detail-crystal-large` du Tower Defense Kit de Kenney (montage : `Docs/Guide_TowerDefense.md`).
  - **Emplacements de construction** : 12 socles fixes, 4 par voie. Deux **sur la voie**, pour une barricade ou des pièges à piques, et deux **au bord de la voie**, pour une baliste ou une tour de givre. On vise un socle et on appuie sur la gâchette de prise (5 m au plus) : un petit panneau propose les bâtiments possibles et leur prix, ou l'amélioration du bâtiment déjà construit (3 niveaux). On construit pendant les pauses comme pendant les vagues.
  - **Construire** : à l'achat, le **plan** du bâtiment apparaît en bleu transparent sur le socle. L'arc se range et un **marteau apparaît dans chaque main**, sans rien attraper (depuis le 16 octobre : la main se ferme dessus toute seule) ; on frappe le plan **3 fois**, avec l'un ou l'autre (un son, des étincelles, une vibration, le plan se remplit d'un tiers à chaque coup). Au troisième coup, le bâtiment est construit, le marteau disparaît et l'arc revient. Une amélioration se fait de la même façon. C'est un effet visuel : aucune difficulté. Une seule construction à la fois ; lâché, le marteau revient flotter dans la main ; si le joueur s'éloigne à plus de 15 m (téléporteur) ou si la partie se termine, la construction se termine d'elle-même. Pendant une amélioration, le bâtiment continue de fonctionner à son niveau actuel.
  - **Bâtiments** (prix en matériaux : construction / niveau 2 / niveau 3) :
    - **Barricade** (sur la voie, 20 / 30 / 45) : comme les barricades de départ, que les ennemis au sol frappent avant de passer. 400 / 700 / 1 000 PV ; au niveau 3, des pointes blessent ceux qui la frappent (10 dégâts par coup reçu au corps à corps). Seul bâtiment que les ennemis peuvent détruire ; la réparation de la boutique la remet debout avec les autres. Une planche et une traverse s'ajoutent au niveau 2, des pieux tournés vers la forêt au niveau 3.
    - **Pièges à piques** (sur la voie, 25 / 35 / 50) : les ennemis au sol qui passent dessus perdent 12 / 18 / 27 PV toutes les 0,8 / 0,65 / 0,5 s ; au niveau 3, ils sont aussi ralentis de 20 %. Les volants passent au-dessus. Les piques jaillissent du plateau à chaque coup ; elles sont plus hautes à chaque niveau.
    - **Tour de givre** (au bord, 40 / 50 / 70) : ralentit de 30 / 40 / 50 % les ennemis dans un rayon de 9 / 13 / 17 m (8 / 9 / 10 m avant le 20 octobre), volants compris (la hauteur ne compte pas). Un disque bleuté montre sa portée au sol ; ses cristaux grandissent et brillent plus à chaque niveau.
    - **Baliste** (au bord, 50 / 60 / 80) : tire seule sur l'ennemi le plus proche à 20 m, volants compris : 10 / 14,7 / 21,3 dégâts (15 / 22 / 32 avant le 16 octobre, baissés d'un tiers), un carreau toutes les 3 / 2 / 1,25 s (2,4 / 1,6 / 1 s avant le 18 octobre : cadence baissée de 20 %) (lente au début : aux tests du 12 octobre, la première baliste faisait plus de victimes qu'un joueur débutant ; le niveau 3 garde toute sa cadence) ; au niveau 3, le carreau traverse 2 ennemis. Elle profite de la moitié des bonus de dégâts de l'arc. Elle pivote vers sa cible, ne tire que sur un ennemi qu'elle voit (pas à travers une falaise) et vise là où il sera ; le carreau vole en ligne droite (45 m/s) et se plante dans le décor. Un fanion s'ajoute au niveau 2, un drapeau au niveau 3.
  - Les ennemis ne visent pas les bâtiments, sauf les barricades. Les bâtiments et les matériaux repartent de zéro à chaque partie.
  - **Les dégâts des bâtiments suivent la force des ennemis** : les valeurs ci-dessus sont celles de la vague 1 en Normal, multipliées ensuite par le renforcement des PV des ennemis (difficulté et vague, mode infini compris). Un bâtiment garde donc le même effet toute la partie et dans chaque difficulté ; ce sont ses niveaux qui le rendent plus fort. Les cartes du panneau l'expriment en multiplicateurs (« Dégâts ×1,5 »). Depuis le 12 octobre, ils suivent aussi la **puissance des bâtiments** de la difficulté (*Structure Power* : 0,75 en Facile, 1 en Normal, 1,25 en Difficile, 1,6 en Impossible, comme le nombre d'ennemis) : avec plus d'ennemis, une baliste en tue autant en proportion, et reste aussi utile au joueur d'Impossible qu'à celui de Facile.
  - **Équilibrage (12 octobre)** : le tower defense doit être très présent à la fin de la partie. En récoltant 70 % des gisements, on a environ 900 matériaux à la fin de la vague 5 (section 3 bis), soit une vingtaine d'achats : presque tous les socles construits et plusieurs niveaux 3. Avec des dégâts fixes, les bâtiments écrasaient les premières vagues en Normal et ne servaient presque plus en Impossible tardif ; avec des dégâts qui suivent la force des ennemis et des valeurs de départ réduites de 40 %, leur part reste la même d'une difficulté à l'autre. À la fin de chaque vague, un message donne la part des dégâts faite par les bâtiments (la console détaille chaque type) pour vérifier ces estimations en jouant.
  - Les ennemis tués par un bâtiment rapportent leurs points et leur or, mais les touches des bâtiments ne comptent ni pour les points de touche ni pour le combo (comme les flèches en plus).
  - **Montage, en 4 étapes** : 1) les cristaux et les matériaux (compteur, montre) ; 2) les socles, le panneau, le plan bleu et le marteau ; 3) les quatre bâtiments et leurs niveaux ; 4) l'équilibrage et le guide de montage.

## 10. Les ennemis

| Ennemi | Déplacement | Cible | PV | Vitesse | Attaque | Valeur | Dès la vague |
|---|---|---|---|---|---|---|---|
| Rampant (chevalier) | au sol | la tour, ou le joueur s'il est à moins de 8 m | 30 | 2,5 m/s | corps à corps | 10 | 1 |
| Gluant (slime vert) | au sol | idem, joueur à moins de 6 m | 18 | 2,2 m/s | corps à corps, faible | 6 | 1 |
| Champignon | au sol | idem | 24 | 2 m/s | corps à corps | 8 | 1 |
| Tireur (mage) | au sol, s'arrête vers 25 m | le joueur | 40 | 2,6 m/s | projectile lent, qu'on peut esquiver ou abattre | 20 | 2 |
| Gluant bleu | au sol | la tour, ou le joueur à moins de 6 m | 32 | 2,4 m/s | corps à corps | 10 | 2 |
| Étoile de mer | au sol | idem | 22 | 3,4 m/s | corps à corps, très rapide | 12 | 2 |
| Volant (Beholder) | dans les airs | le joueur | 20 | 5 m/s | attaque en piqué | 15 | 3 |
| Guêpe | dans les airs, basse | le joueur | 12 | 6 m/s | piqués rapides | 12 | 3 |
| Champignon furieux | au sol | la tour, ou le joueur à moins de 7 m | 45 | 2,7 m/s | corps à corps | 15 | 3 |
| Cactus | au sol | idem, 6 m | 60 | 1,8 m/s | corps à corps, solide | 16 | 3 |
| Fantassin (soldat) | au sol | idem, 8 m | 55 | 2,4 m/s | corps à corps, épée | 18 | 3 |
| Orc | au sol | idem, 8 m | 110 | 2,1 m/s | gros coups | 30 | 4 |
| Tortue | au sol | idem, 6 m | 120 | 1,3 m/s | lente, très solide | 28 | 4 |
| Liche | au sol, s'arrête vers 28 m | le joueur | 70 | 1,8 m/s | charge une grosse boule au bout de son bâton, puis la lance (plus forte, un peu plus lente) | 35 | 3 |
| Gargouille | dans les airs, haute | le joueur | 35 | 4,5 m/s | piqués puissants | 22 | 4 |
| Mimique (coffre) | au sol | la tour, ou le joueur à moins de 8 m | 85 | 3 m/s | corps à corps, rapide | 30 | 5 |

| Boss (fin de vague) | Vague | PV | Vitesse | Attaque | Renforts | Point faible | Valeur |
|---|---|---|---|---|---|---|---|
| Thomas (champignon géant) | 1 | 320 | 1,5 m/s | écrase la tour | 2 Champignons toutes les 20 s | le haut du chapeau | 200 |
| Jean (mimique vorace) | 2 | 510 (420 avant le 17 octobre) | 2,3 m/s | mord la tour, rapide | 2 Gluants bleus toutes les 20 s | la serrure | 250 |
| Gontrand (tortue colossale) | 3 | 910 (650 avant le 17 octobre) | 1 m/s | frappe très fort | 2 Étoiles de mer toutes les 18 s | le haut de la carapace | 300 |
| Francine (chef de guerre orc) | 4 | 1 210 (700 avant le 17 octobre) | 1,7 m/s | frappe très fort | 2 Fantassins toutes les 20 s | le dos | 400 |
| Nolan (liche suprême) | 5 | 1 335 (750 avant le 17 octobre) | 1,2 m/s | bombarde la tour de loin (30 m) de grosses boules chargées | 3 Rampants toutes les 18 s | la poitrine | 500 |
| Dragon rouge (boss final) | 6 | dans son antre, voir section 10 bis | | | | | |

- Zones de touche : la tête et le corps. Le boss a en plus des points faibles lumineux (dégâts ×3).
- **Volant** : un Beholder, gros œil volant à tentacules ; son œil est sa tête (dégâts ×2). Il tourne en l'air autour du joueur, à 6–8 m de haut. Il ne traverse plus les murs ni le relief (17 octobre) : devant un obstacle, il glisse le long et remonte par-dessus. En piqué, seul ce qui est avant la tête du joueur compte (19 octobre : le sol juste derrière lui le faisait remonter au dernier moment). De temps en temps, il fait du sur-place en se cabrant et en criant (0,6 s, le moment de le viser), puis pique sur la tête du joueur, en corrigeant lentement sa trajectoire : un pas de côté suffit à l'esquiver. Touché ou raté, il remonte et recommence (un piqué toutes les 5 s environ). Tué, il tombe et s'effondre au sol. Il ne passe pas par le NavMesh ; la glace, posée au sol, ne le ralentit pas.
- **Tireur** : un mage. Il marche vers le joueur et s'arrête dès qu'il est à portée (25 m) ; il ne repart qu'une fois à plus de 28 m (depuis le 16 octobre : à la limite de sa portée, il s'arrêtait et repartait plusieurs fois par seconde, au moindre mouvement du joueur, et avançait par à-coups). Plus rapide depuis le 16 octobre (2,6 m/s au lieu de 2), et plus fréquent dans les vagues, comme la liche, qui arrive dès la vague 3 (poids 1 et 0,9 au lieu de 0,4 et 0,35 : environ deux fois et demie plus de tireurs et de liches). Toutes les 3 s, il abat son bâton vers le joueur, et un projectile lent (9 m/s) part du bout du bâton vers sa tête. On peut l'esquiver, ou l'abattre d'une flèche (5 points). Le projectile se brise sur le décor.
- **Liche** (15 octobre) : elle n'a plus l'attaque du tireur. Une boule se forme au bout de son bâton et grossit pendant 1,4 s (plus lentement si elle est ralentie par la glace), puis elle abat son bâton et la lance, visée au dernier moment : la boule est 4,4 fois plus grosse qu'un projectile de tireur (2,2 avant le 17 octobre ; 6 fois pour Nolan ; sa zone de touche aussi) et un peu plus lente (7,5 m/s, contre 9). On peut l'esquiver, ou l'abattre d'une flèche, même pendant la charge : l'attaque est alors perdue. Si la liche meurt pendant la charge, la boule se dissipe. Nolan, la liche boss, fait de même en plus gros (1,6 s, 3 fois la taille, 7 m/s).
- Le volant et le tireur visent toujours le joueur, jamais la tour.
- **Bestiaire du 12 octobre** : les créatures des packs PBR de Dungeon Mason (Mini Legion, RPG Monster Duo, Buddies, Couple, Partners, Monster Minion Survivor), chacune avec des stats qui collent à son allure (le slime faible et nombreux, la tortue lente et solide, l'étoile de mer rapide, l'orc qui frappe fort…). Les plus forts arrivent plus tard dans la partie. Prefabs `Enemy_*` et `Boss_*`, données dans `Data/Enemies`, réglages des vagues dans `WaveSettings`.
- Chaque boss des vagues 1 à 5 est une version géante (×1,9 à ×3,2) d'un ennemi, avec une barre de PV et son nom au-dessus de lui, et un point faible (dégâts ×3) : une gemme taillée sertie dans une monture dorée à griffes, avec un sceau doré qui tourne devant elle face au joueur. Elle bat comme un cœur, plus vite quand le boss a peu de PV ; touchée, elle jette un éclat, une onde dorée part du sceau et il s'emballe. Chaque boss a sa couleur de gemme, qui ressort sur son modèle : vert vif pour Thomas, rose-magenta pour Jean, turquoise pour Gontrand, saphir pour Francine, vert nécromancien pour Nolan, ambre pour le Roi Gluant (16 octobre, codé, pas encore testé). **Des géants plus lisibles** (16 octobre, après les tests, codé, pas encore testé) : leur animation de course allait à la vitesse de celle des petits ennemis, et ils s'agitaient tellement (le champignon surtout) que leur point faible était presque impossible à viser. Elle est maintenant 3 fois plus lente (×0,35 : de grandes enjambées, à leur taille), et le point faible suit le corps du boss sans garder les secousses de l'animation (il en garde environ un quart, lissées). Réglages *Run Animation Speed* et *Weak Point Steadiness* du composant `Boss` de chaque prefab `Boss_*`. L'ancien boss, un chevalier géant, est retiré (le chevalier reste en ennemi normal). Le boss arrive à la fin du chrono de chaque vague, avec un cor et des tambours. Il appelle ses renforts (tableau ci-dessus) une première fois 12 à 15 s après son arrivée, puis toutes les 18 à 20 s, seulement s'il y a moins de 10 ennemis ; en plus, un ennemi de la vague arrive toutes les 6 s tant qu'il est en vie. Comme les autres ennemis, il suit la courbe de difficulté (section 11) : en Normal, de 320 PV à la vague 1 à environ 1 500 à la vague 5.
- Les ennemis arrivent par 3 voies : à gauche et à droite depuis les coins de la carte voisins de la base, au milieu depuis le centre de la carte (section 17).
- Chaque vague dispose d'un budget qui augmente de vague en vague. Chaque ennemi a un coût, et le budget est dépensé tout au long du chrono.
- Les PV, les dégâts, le nombre et la vitesse des ennemis suivent une courbe exponentielle (section 11). Le tableau ci-dessus donne leurs valeurs à la vague 1, en Normal. Quand ils vont plus vite, leur animation de course accélère aussi.
- Pour que le jeu reste fluide dans le casque : 500 ennemis au plus par vague, et 40 en vie en même temps (12 à la vague 1 en Normal ; ce maximum grandit avec la difficulté et les vagues). À la fin du chrono, ceux qui ne sont pas encore sortis ne viennent plus.

## 10 bis. Le boss final : MK, le dragon rouge (vague 6)

Décidé et codé le 13 octobre (la première version, faite sur l'ancien PC, a été perdue). Montage : `Docs/Guide_BossFinal.md`.

**L'antre.**

- La vague 6 commence au gong, comme les autres, mais sans chrono ni ennemis : la carte est vide et figée, la tour ne risque rien. Pas de coffres ni de cristaux.
- La grande porte au bout de la voie du milieu s'ouvre lentement : une double porte arrondie en bois cerclé de fer (le battant du pack *Modular Dungeon* de Quaternius, coupé en deux au milieu, depuis le 16 octobre), posée sur un seuil de pierre, presque au ras de la façade d'un porche de pierre collé à la face du fond, et deux braseros qui s'allument. Depuis le 16 octobre au soir, la porte remplit l'ouverture : elle va presque d'un pilier à l'autre, et l'arche à claveaux épouse sa forme (12 cm au-dessus du sommet, un peu plus aux épaules, juste ce qu'il faut pour que les battants passent sous les claveaux en s'ouvrant vers la voie ; elle est calculée toute seule). Derrière, un couloir tout noir, voûté comme la porte. Le porche est plus bas qu'avant (environ 8,3 m avec la corniche, au lieu de 11,8 m) : le nom du jeu n'est plus sur son linteau.
- **Le nom du jeu, gravé dans la roche** (16 octobre, codé, pas encore testé ; refait le soir pour mieux l'incruster) : « THUNK VR » sur la face de la montagne, au-dessus du porche, assez haut pour qu'on le voie par-dessus en arrivant par la voie. Il est dans un cartouche de pierre taillée encastré dans la falaise, de la pierre du porche : une dalle en retrait dans un cadre de blocs qui avancent davantage (un peu de travers, de profondeurs différentes), une clé au sommet et une oreille en losange de chaque côté. Les lettres (police des titres des menus, 2,1 m de haut au plus, environ 15 m de large) sont creusées dans la dalle : un creux sombre, sa lèvre du bas éclairée par la lave, et au fond la lave, plus claire en bas des lettres, où elle s'accumule. Quelques lumières chaudes éclairent la pierre, et des braises montent (les fissures de feu autour du titre ont été retirées le 16 octobre au soir). Le titre bat au rythme des veines de l'antre, de plus en plus fort au fil des vagues, et s'embrase quand la porte s'ouvre. Réglages : section *Titre gravé dans la roche* de `Lair Gate`.
- Pendant la marche vers l'antre, la musique et l'ambiance de la carte baissent à mesure que le joueur s'éloigne de la tour (pleines jusqu'à 12 m de la tour), jusqu'au silence à la porte. Elles reviennent en douceur s'il fait demi-tour, et au retour à la tour après la victoire.
- Quand le joueur passe le seuil, les portes claquent derrière lui et tout devient noir. On en profite pour charger l'arène (scène `Arena_Dragon`, par-dessus la carte) et y déplacer le joueur. Le bouton « Tour » de la montre est caché : on ne ressort qu'en battant le dragon.

**L'arène : « la fourche ».** Une grotte en cubes, comme la carte (30 × 30 cases de 2,5 m, soit 75 m ; agrandie le 15 octobre, elle en faisait 60), dessinée case par case dans `Data/Arena/Arena_Fourche.txt`. Sa bordure est un plateau en hauteur, inaccessible, où se placeront les tourelles ; le joueur arrive par un couloir, au bas du plan. Une coulée de lave tombe du plateau au fond (une seule cascade, d'une texture continue sur toute sa largeur, avec de la roche derrière), et se divise en deux bras qui traversent la grotte ; sans ponts : on les franchit (5 m) en courant et en sautant (saut aux bras, section 12). Le nid du dragon est une estrade au fond à gauche, au milieu de colonnes brisées et d'armes d'aventuriers. Parois, plateau et piliers en roche à facettes ; une voûte en cubes, de 22,5 m près des parois à 32,5 m au centre, avec des stalactites ; six piliers pour s'abriter, sur lesquels le dragon se perche (comme sur le bord du plateau) ; celui du milieu est décalé vers la gauche, pour qu'on voie l'arène en arrivant ; de fines craquelures rouges qui brillent dans le sol. Toute la lumière vient d'une myriade de petits cristaux au plafond (rouges, orange, roses, violets), en quatre quarts, avec un peu de lueur de la lave. Toucher la lave brûle (4,67 PV par seconde avant la difficulté et la vague ; 14 avant le 15 octobre).

**Les tourelles du joueur rejoignent l'arène** (15 octobre). Les balistes et les tours de givre construites sur la carte se téléportent pendant l'intro sur les emplacements du plateau (cases T du plan, 6 au plus : les plus améliorées d'abord). Sur chaque quart de cristaux qui s'allume, une tourelle (deux s'il y en a plus de quatre) descend du ciel dans un rayon de lumière bleutée, avec une traînée d'étincelles et une gerbe d'étincelles à l'arrivée, dans le quart qui s'allume si possible. Elles ne tirent qu'au début du combat, avec une portée doublée (du haut du plateau, elles atteignent le dragon), et retrouvent leur place sur la carte quand l'arène se ferme (victoire ou mort). Les pièges et les barricades restent sur la carte. Dans l'arène, les **tours de givre** ne ralentissent plus autour d'elles : elles tirent des **boules de glace** (comme les carreaux des balistes, en visant un peu devant le dragon) qui le ralentissent au coup (30 / 40 / 50 % selon le niveau, 25 % au plus sur le dragon), un ralentissement qui s'estompe vite (au carré, en 2 / 2,5 / 3 s). Une tour tire toutes les 3 / 2,5 / 2 s ; à plusieurs, elles ne tirent pas ensemble mais se relaient à intervalles réguliers (deux tours : une boule toutes les 1,5 s environ). Une boule de glace ne fait pas de dégâts ; le décor l'arrête. Touché, le dragon se couvre d'un léger voile bleuté (plus vif sur ses contours), qui s'efface au même rythme que le ralentissement.

**L'intro, au rythme de la musique** (morceau composé par un ami ; tempo mesuré : 140,26 BPM, 98 mesures). Après les tests du 13 et du 14 octobre, elle suit le **quart du tempo** (un temps de l'intro = une mesure du morceau, 1,7 s) : elle dure environ 17 s.

1. Le joueur arrive dans le noir complet ; la musique commence, un peu moins fort (60 %). Il reste **figé sur place** (il peut tourner la tête et regarder autour de lui, pas marcher, courir, glisser ni sauter) jusqu'à ce que tous les cristaux soient allumés (22 octobre).
2. Après les 4 premiers temps de l'intro, sur la grosse attaque du morceau (6,9 s), les cristaux s'allument, un quart sur chaque temps de l'intro (jusqu'à 12 s), en tournant autour de la grotte en partant du dragon. Avant, le dragon est totalement invisible : il apparaît avec le premier quart de cristaux, debout en haut de la coulée de lave du fond, face au joueur. La musique remonte à son volume normal à mesure qu'ils s'allument.
3. Avec le dernier quart (temps 7 de l'intro, 12 s), il s'envole du haut de la coulée en battant des ailes, monte à une douzaine de mètres au-dessus de l'arène, puis pique sur le centre et s'y pose dans une gerbe de flammes, juste avant que sa barre apparaisse (1,6 s en tout).
4. À 13,7 s, la barre de PV du dragon apparaît en haut du champ de vision, avec son nom (« MK » ; il s'appelait « Ignarok, le Dragon Usurpateur » avant le 15 octobre), et se remplit en 2 temps de l'intro (3,4 s). Pendant ce temps, il lève la tête et crache un long jet de feu vers la voûte (sans danger). En même temps, la coulée de lave du fond descend du plateau jusqu'à la lave du sol.
5. À 17,1 s, dès que la barre est pleine (au début d'une mesure du morceau), il cesse de cracher : le combat commence. Il rugit ensuite pendant le combat (envol, rage). Ensuite, seule la lumière suit la musique : les cristaux pulsent sur chaque temps du morceau. À la fin, le morceau reprend après sa propre intro (6,9 s).

**Le combat, en trois phases (par tiers de PV).** Revu entièrement le 14 octobre (codé, à tester dans le casque) : le combat repose sur le skill du joueur. Presque toutes les attaques s'esquivent en se déplaçant bien ; le dragon frappe moins fort, mais il a plus de PV.

| Phase | PV | Ce qu'il fait |
|---|---|---|
| 1. Au sol | 100 à 66 % | Il garde une dizaine de mètres avec le joueur (parfois il fonce au contact). **Boule de feu**, son attaque la plus courante : une énorme boule à la texture de feu se forme dans sa gueule, puis file tout droit vers l'endroit où était le joueur ; on l'esquive en se déplaçant (une flèche l'abat, même dans sa gueule). **Souffle** : il lève la tête vers la voûte, gueule ouverte, une lumière de plus en plus vive y brille (3,2 s ; 1,6 s avant le 15 octobre), puis il crache un cône de feu (16 m) qui suit lentement le joueur ; on s'en sort en passant sur son flanc, de près, ou derrière un pilier. Proche : **coup de patte** : il lève la patte droite ou gauche, puis rafle le sol devant lui ; on l'esquive en sautant au bon moment. Tout contre sa gueule : il **croque**, sans élan (presque inévitable). Toutes les 13 s environ, plus loin : il **se dresse** sur ses pattes arrière en rugissant (la queue posée derrière lui), puis **frappe le sol** de ses pattes avant : un **anneau de feu** bas (60 cm) part de l'impact et s'élargit à travers toute l'arène en suivant le sol (10 m/s) ; on l'esquive en sautant par-dessus quand il passe (un pilier ne l'arrête pas). |
| 2. En vol | 66 à 33 % | Rugissement, il décolle et tourne au-dessus de l'arène. Mêmes **boules de feu**, depuis les airs. **Triple ruée** : une bande rouge apparaît au sol, en travers du joueur, puis il fonce le long de la bande en crachant le feu sous lui ; il enchaîne trois fois, vite. Deux rideaux de lumière rouge montent des bords de la bande et deux projecteurs rougissent la voûte au-dessus (16 octobre ; plus bas et plus discrets depuis le 17, ils étaient trop visibles) : on la voit même la tête levée vers le dragon. **Perchoir** : il se pose sur un pilier ou sur le bord du plateau et lance le même souffle qu'au sol (même charge, tête levée), puis redécolle. |
| 3. Enragé | 33 à 0 % | Il se pose sur un perchoir, rugit, et revient au sol par son **salto**. Ensuite il alterne vite le sol (11 s) et le vol (9 s), avec toutes les attaques des deux formes sauf le perchoir, et tout va 30 % plus vite (déplacements, animations, attaques). **Salto** : il saute sur un rebord ou un pilier, s'élance en faisant un triple salto raté, raide comme une T-pose (pour le côté comique), marque un temps d'arrêt en l'air (un cercle rouge apparaît), puis pique, net, sur l'endroit où était le joueur au début de l'attaque (4 m, gros dégâts). Il le fait à chaque retour au sol, et de temps en temps depuis le sol. La lave déborde : des flaques s'étendent sur le sol. Les cristaux virent au rouge. |

- Ses yeux sont rouges et brillent. L'animation de marche suit sa vitesse : ses pattes ne patinent plus.
- La glace (et tout autre ralentissement) ne lui retire jamais plus de 25 % de sa vitesse, toutes flèches confondues ; elle ralentit tout : déplacements, animations et attaques.
- Zones de touche : la tête (×2), le corps, les pattes, et un point faible sous la poitrine (×3), une gemme cyan glacé sertie d'or, comme chez les autres boss.
- PV de base 7 760 (+15 % le 18 octobre ; 6 750 le 16 octobre, 4 500 avant, 3 000 avant le 14 octobre ; environ 18 500 à la vague 6 en Normal), dégâts de base 8,33 (divisés par 3 le 15 octobre ; 25 avant, 40 au départ) ; toutes ses attaques en sont une part : boule de feu 55 % (un quart à la moitié si elle s'écrase à côté), souffle 40 % par seconde, coup de patte 70 %, morsure 90 %, anneau de feu 60 %, ruée 60 % (et 25 % par seconde dans le feu qui reste au sol), salto 130 %. Enragé : ×1,1. Il suit la difficulté et la courbe des vagues comme les autres ennemis. Il rapporte 1 500 points.
- Les améliorations marchent sur lui (foudre, explosion, glace, auto-visée…).
- Quand il sort du champ de vision (en vol surtout), une double flèche rouge au bord de la vue montre où il est (17 octobre) ; elle disparaît dès qu'il revient devant les yeux.
- Pendant son salto et le temps qu'il se relève, son corps ne bouscule pas le joueur (il pouvait l'enfoncer dans le sol et le faire tomber à travers la carte) ; et si le joueur passe malgré tout sous le sol de l'arène, il est remis dessus (16 octobre).

**Les lumières de l'arène** (15 octobre, codé, pas encore testé). Comme sur la carte (section 17 bis), ce sont des objets de la scène `Arena_Dragon`, sous un objet **« Effet »** (groupes « Veines », « Braises de la grotte », « Coulées », « Soleil », « Piliers » ; en jeu, « En jeu » pour ce qui ne sert que pendant le combat), fabriqué par l'éditeur à l'ouverture de la scène (`Scripts/FinalBoss/ArenaLights.cs`), réglé dans `Resources/MapLighting` (partie « Arène du boss final ») :
- des **veines de lave** dans les parois et la voûte, qui battent sur chaque temps de la musique (plus fort sur le premier temps de la mesure) ; il y en a plus à chaque phase (moins de la moitié au sol, les trois quarts en vol, toutes dans la rage) ;
- la **lave vivante** : des bulles qui gonflent puis éclatent (étincelles, petit éclat de lumière), plus souvent quand le dragon s'énerve, et une vapeur de chaleur qui monte ;
- des **braises** qui montent dans toute la grotte, plus nombreuses à chaque phase ;
- des **gerbes d'étincelles et un halo** au pied des coulées de lave, une fois qu'elles sont descendues ;
- le **sol brûlant** : là où frappent une boule de feu, le souffle ou le slam, le sol reste orange vif, puis rouge sombre, et refroidit en 5 à 7 s ;
- des **annonces lumineuses** : pendant que le dragon inspire, le sol chauffe en rouge, de plus en plus vif, dans la direction où il va cracher (la portée et l'angle du souffle) ; pendant qu'il se dresse pour le slam, les craquelures du sol flashent de plus en plus fort ;
- à **l'envol et à la rage**, une onde de lumière part du dragon et parcourt tout le sol, les craquelures flashent, les braseros et la lave s'embrasent un instant ;
- une **rune bleue** s'allume au sol sous chaque tourelle téléportée ;
- les **flèches** laissent des traînées lumineuses (comme la nuit sur la carte), et une petite lumière douce entoure le joueur ;
- les **piliers** (16 octobre), pour qu'on les voie bien et qu'on s'en serve comme abri : ils sont en obsidienne (noire et brillante, matériau `Arena_Obsidian` ; les rochers du décor gardent la pierre rouge), une lumière froide les éclaire par en dessous depuis quelques cristaux posés à leur pied, leur contour brille légèrement en bleu (on les voit de partout, même au fond du brouillard), et une couronne de cristaux bleus brille à leur sommet, plus fort quand le dragon s'y pose. Pendant l'intro, chaque pilier (cristaux et lumière) s'allume en même temps que le quart des cristaux du plafond au-dessus de lui ; avant, il reste dans le noir. Pendant que le dragon inspire avant son souffle, l'**abri** derrière chaque pilier à sa portée s'éclaire en bleu au sol : la zone où la poitrine du joueur serait protégée du feu.

**Victoire.** À sa mort, la lave s'éteint peu à peu et la grotte s'assombrit, les cristaux blanchissent, puis des rayons de soleil percent la voûte (au centre et autour), avec de la poussière qui danse dedans. Deux cibles apparaissent devant le joueur : **Continuer** (mode infini : retour en haut de la tour, boutique, puis le gong lance la vague 7) ou **Terminer la partie** (écran « Victoire ! », nom, classement). On choisit en tirant dedans. Mourir dans l'arène termine la partie, comme ailleurs.

## 11. Les difficultés

| | Facile | Normal | Difficile | Impossible |
|---|---|---|---|---|
| Ciel | soleil au zénith | coucher de soleil | nuit, lune claire | ciel et lune rouges |
| PV des ennemis | ×0,7 | ×1 | ×1,4 | ×2 |
| Vitesse des ennemis | ×0,8 | ×1 | ×1,15 | ×1,3 |
| Taille de la tête (zone de touche) | ×1,3 | ×1 | ×0,85 | ×0,75 |
| Dégâts subis | ×0,6 | ×1 | ×1,3 | ×1,8 |
| Nombre d'ennemis | ×0,75 | ×1 | ×1,25 | ×1,6 |
| Ennemis après l'accalmie (montée, renforts du boss) | ×0,5 | ×1 | ×1 | ×1 |
| PV des boss (boss de vague et dragon), en plus de ceux des ennemis | ×0,5 | ×1 | ×1 | ×1 |
| Durée des vagues (chrono) | ×0,5 | ×1 | ×1 | ×1 |
| Cadence de tir des balistes | ×0,5 | ×1 | ×1 | ×1 |
| PV des ennemis, à chaque vague | ×1,14 | ×1,19 | ×1,27 | ×1,34 |
| Dégâts subis, à chaque vague | ×1,07 | ×1,09 | ×1,14 | ×1,19 |
| Nombre d'ennemis, à chaque vague | ×1,09 | ×1,12 | ×1,16 | ×1,21 |
| Vitesse des ennemis, à chaque vague | ×1,02 | ×1,05 | ×1,06 | ×1,07 |
| Début plus doux (PV et dégâts) | non | non | non | ×0,5 à la vague 1, en remontant jusqu'à ×1 à la vague 3 |
| Largeur de la bande verte (parfait) | ×1,3 | ×1 | ×0,85 | ×0,7 |
| Aide à la visée (section 4.3) | trajectoire complète | trajectoire complète | ligne droite | aucune |
| Score | ×0,75 | ×1 | ×1,5 | ×2 |

La nuit, les yeux des ennemis brillent et une lanterne éclaire la tour, pour que le jeu reste lisible.

**Facile, très simple (16 octobre).** Le Facile est fait pour quelqu'un qui découvre la réalité virtuelle : vagues deux fois plus courtes, deux fois moins d'ennemis après l'accalmie (ceux du début de la vague ne changent pas), boss deux fois moins résistants, dragon compris. Les balistes tirent deux fois moins souvent, pour que le joueur garde les éliminations à l'arc. Réglages *Boss Health*, *Wave Duration*, *Late Enemy Count* et *Ballista Fire Rate* de la difficulté.

**Courbe de difficulté exponentielle.** Les améliorations n'ont pas de limite (section 6.2) : pour compenser, chaque vague multiplie encore les PV, les dégâts, le nombre et la vitesse des ennemis (lignes « à chaque vague »). La courbe monte plus vite en Difficile et en Impossible. En mode infini (à partir de la vague 7), elle s'accélère encore, quelle que soit la difficulté : chaque vague multiplie en plus les PV par 1,25, les dégâts par 1,14, le nombre d'ennemis par 1,19 et leur vitesse par 1,07 (section 3 bis). Les prix de la boutique montent aussi, et l'or gagné baisse (sections 7 et 8).

**Courbe vague par vague (17 octobre, après les tests de la nuit des assos).** Le joueur devenait bien plus fort que les ennemis à chaque boutique : la vague 1 était un peu dure, la 2 équilibrée, puis les vagues 3 à 5 de plus en plus faciles (boss des vagues 4 et 5 tués en deux flèches). Par-dessus la croissance de la difficulté, les *Wave Settings* multiplient donc les PV (boss compris) et le nombre d'ennemis de chaque vague : PV ×0,85 / ×1,1 / ×1,65 / ×2,2 / ×2,85 et nombre ×0,85 / ×1 / ×1,2 / ×1,4 / ×1,6 aux vagues 1 à 5 (la dernière valeur continue en mode infini). À la vague 1, l'accalmie n'a plus que 7 % des ennemis (15 % ensuite), pour que le joueur ose sortir récolter. La vague du boss final n'en tient pas compte (le dragon est équilibré). Le Facile n'a pas cette courbe (*Wave Curve* 0). Boss de base des vagues 2 à 5 relevés (Jean 510, Gontrand 910, Francine 1 210, Nolan 1 335 : un tiers de plus encore le 17 au soir, avec 10 % de PV en plus pour les ennemis des vagues 2 à 5) : en Normal, environ 270 / 670 / 2 130 / 4 490 / 7 630 PV aux vagues 1 à 5 (320 / 500 / 920 / 1 180 / 1 500 avant).

La vitesse ne fait au plus que doubler (réglage *Max Speed Scale* des Wave Settings), pour que les ennemis restent lisibles et que leurs déplacements restent propres :

| Vitesse d'un Rampant | Vague 1 | Vague 3 | Vague 6 | Vague 8 | Vague 10 | Vague 15 |
|---|---|---|---|---|---|---|
| Facile | 2,0 m/s | 2,1 m/s | 2,2 m/s | 2,6 m/s | 3,1 m/s | 4,0 m/s (plafond) |
| Normal | 2,5 m/s | 2,8 m/s | 3,2 m/s | 4,0 m/s | 5,0 m/s (plafond) | 5,0 m/s (plafond) |
| Difficile | 2,9 m/s | 3,2 m/s | 3,8 m/s | 4,9 m/s | 5,8 m/s (plafond) | 5,8 m/s (plafond) |
| Impossible | 3,2 m/s | 3,7 m/s | 4,6 m/s | 6,0 m/s | 6,5 m/s (plafond) | 6,5 m/s (plafond) |

| PV d'un Rampant (dégâts d'un coup) | Vague 1 | Vague 3 | Vague 6 | Vague 8 | Vague 10 | Vague 15 |
|---|---|---|---|---|---|---|
| Facile | 21 (6) | 27 (7) | 40 (8) | 82 (13) | 167 (19) | 980 (50) |
| Normal | 30 (10) | 42 (12) | 72 (15) | 158 (24) | 350 (37) | 2 553 (109) |
| Difficile | 42 (13) | 68 (17) | 139 (25) | 350 (42) | 881 (71) | 8 886 (265) |
| Impossible | 30 (9) | 108 (25) | 259 (43) | 727 (79) | 2 040 (145) | 26 903 (668) |

Les vagues 8 à 15 sont celles du mode infini.

**Début de partie plus doux en Impossible.** Aux tests, les premières vagues étaient très dures en Impossible. Les PV et les dégâts des ennemis y sont donc réduits au début, puis remontent : la vague 1 est à 50 %, la vague 2 à 75 %, et l'on retrouve la courbe normale dès la vague 3. Le nombre d'ennemis ne change pas : Impossible garde ses vagues en masse, et l'on gagne autant d'or (même un peu plus, puisqu'on en tue davantage). Réglages *Early Strength* et *Full Strength Wave* de chaque difficulté.

| Impossible | Vague 1 | Vague 2 | Vague 3 |
|---|---|---|---|
| Force des ennemis | 50 % | 75 % | 100 % |
| PV d'un Rampant | 30 (sans réduction : 60) | 60 (80) | 108 |
| Dégâts d'un coup | 9 (18) | 16 (21) | 25 |

Le nombre d'ennemis par vague est donné dans la section 3 bis (budget de 20 à la vague 1, +10 par vague, dans la limite de 40 en vie en même temps, section 10). Ces réglages se trouvent dans les assets `Data/Difficulties` (courbe de chaque difficulté) et `Data/Waves/WaveSettings` (accélération du mode infini).

On choisit la difficulté avant la première vague, en tirant dans l'un des 4 panneaux devant la tour ; le ciel passe au nouveau ciel en quelques secondes. Les panneaux sont à 8 m, au-dessus du regard (montés de 1,6 m le 22 octobre : ils étaient à hauteur des yeux et barraient la vue du champ de bataille), et disparaissent au gong de la première vague. Elle est ensuite verrouillée, et elle est gardée quand la partie recommence.

## 12. Les déplacements

- Le joueur se déplace physiquement dans sa pièce.
- **Marche** : le joystick gauche donne la direction, par rapport au regard (lissé, pour que la course ne tangue pas). Joystick en arrière, on recule. 2,5 m/s, joystick à fond.
- **Course aux bras** : en marchant, balancer les deux bras multiplie la vitesse, jusqu'à ×3 (7,5 m/s). La vitesse suit celle de la main la plus lente par rapport à la tête : tendre la corde ou prendre une flèche, d'une seule main, ne fait pas courir. Pas besoin de gâchette (option possible).
  - On accélère et on freine progressivement : en pleine course, un demi-tour freine d'abord. En l'air, on garde son élan.
- **Slide** : A ou X pendant la course, à partir de 3,5 m/s. On repart 40 % plus vite (10 m/s au plus), puis on glisse 2,5 s dans la même direction, avec peu de frottement, les mains libres pour tirer. Un son, une vibration et la vignette de confort l'accompagnent.
- Ces déplacements passent par le système de locomotion d'XRI : on ne traverse pas les murs, et l'on tombe si l'on court dans le vide. Le script remplace le déplacement au joystick d'XRI, et le saut d'XRI (bouton A) est désactivé, puisque A sert au slide.
- Rotation par crans de 45° au joystick droit, désactivable : pratique avec le câble Link. Pas de demi-tour en tirant le joystick vers soi (retiré le 16 octobre). Depuis le 17 octobre, le joueur choisit dans les paramètres : par crans, continue (à la vitesse de son choix, de 30 à 180 °/s, 60 par défaut), ou coupée. Les crans restent le réglage par défaut, plus doux pour le mal des transports. Le joystick droit ne fait rien d'autre : la flèche de téléportation d'XRI est désactivée. Le 20 octobre, la rotation se coupait parfois : un objet attrapé au rayon, de loin (l'arc, le marteau…), réservait le joystick de la main pour l'avancer ou le reculer tant qu'on le tenait (réglage d'XRI). C'est retiré, ainsi que le défilement des menus au joystick (aucun ne défile) ; un filet de sécurité rallume la rotation si elle reste coupée (`GameSettings`).
- **L'arc rangé suit le joueur** (20 octobre) : lâché, il revient à sa place près du joueur (à côté de la taille, devant), dans le repère du joueur : il suit tous ses déplacements (course, glissade, rotation, téléporteur) sans traîner derrière, et ne fait que le chemin qui reste ; rangé, il est recalé juste avant l'affichage de chaque image (avant, en courant, il pouvait traîner à plus d'un mètre).
- Vignette de confort pendant le slide (et, en option, pendant la course), réglable dans les paramètres.
- Le téléporteur de la tour (une bande tout autour de son pied) et le bouton « Retour à la tour » de la boutique.
- **Sans casque** (tests, démonstration) : `Desktop Controls` prend le relais dès qu'aucun casque n'affiche l'image, et le jeu se joue entièrement au clavier et à la souris. L'arc est tenu sur la droite de l'écran (levé à 24 cm à droite du viseur et penché vers la droite, baissé en bas à droite ; à gauche et au milieu jusqu'au 20 octobre, il cachait la moitié de la visée) ; la flèche part toujours vers ce que vise le centre de l'écran. Les déplacements d'XRI sont mis en pause et la caméra est tenue à hauteur d'yeux ; tout revient dès qu'un casque se branche.
  - **Se déplacer** : ZQSD (WASD en QWERTY) ou les flèches, la souris pour regarder (curseur bloqué au centre de l'écran ; Échap le libère, un clic gauche le reprend sans tirer), Maj pour courir, Espace pour sauter.
  - **Tirer** : clic gauche maintenu. Une flèche s'encoche, l'arc se lève et la corde se tend en 0,75 s ; à pleine tension, l'anneau de timing démarre autour du viseur, avec l'aide à la visée de la difficulté. Relâcher tire vers le centre de l'écran (la flèche retombe comme en VR). Lâché trop tôt, le tir est annulé.
  - **Interagir** : E, en visant l'objet au centre de l'écran, fait ce que fait la gâchette de prise : récolter un gisement, ouvrir un coffre, prendre une orbe, ouvrir le panneau d'un socle, avec les mêmes portées qu'en VR. E clique aussi les boutons des panneaux (boutique, construction, menus), règle les curseurs (maintenu) et active le champ du nom. Pendant une construction, E abat le marteau sur le plan visé (à 3,5 m au plus). L'action possible s'affiche sous le viseur.
  - Pas encore au clavier : le bouton « Tour » de la montre (la montre n'est pas visible sans casque), et enflammer une flèche au brasero.
- **Prévu** : le slide accélère dans les descentes et ralentit dans les montées (les rampes de la carte, section 17).
- **Saut aux bras** (14 octobre, à tester dans le casque). Lever les deux bras d'un coup fait sauter, en se déplaçant comme à l'arrêt (depuis le 17 octobre ; à l'arrêt, pas avec une flèche encochée), d'environ 1,3 m (au clavier, Espace saute d'environ 1 m). On garde son élan en l'air : en courant aux bras, on franchit environ 5 à 6 m, assez pour un bras de lave de l'arène du boss. Il faut les deux mains, vite et vers le haut : prendre une flèche dans le carquois ou lever l'arc ne fait pas sauter (depuis le 19 octobre, une main qui part vers l'arrière ou passe derrière la tête ne déclenche plus de saut : lever l'arc en allant chercher une flèche faisait sauter à l'arrêt). Pratique pour franchir la rivière ou monter sur un rocher. Pas de saut pendant les 2 premières secondes, ni juste après un « saut » du suivi des manettes (une main qui bouge de plus de 25 cm en une image) : au lancement en VR, le suivi qui démarre faisait sauter le joueur tout seul (corrigé le 18 octobre).

## 13. Les coffres

- **Apparition** : 2 coffres par vague, après 10 % du chrono (accalmie) puis 45 % (début de la montée), et 50 % de chance d'un troisième à 70 %. Ils se posent à 15–30 m de la tour, sur le sol des ennemis, ou sur des emplacements choisis (dans la forêt de la carte finale). Un message les annonce, et un rayon de lumière doré, qui pulse, les signale depuis la tour.
- **Risque** : ils sont gratuits, mais il faut quitter la tour pendant la vague et courir les chercher. Ils disparaissent à la fin de la vague, ouverts ou non.
- **Ouverture** : on vise le coffre et on appuie sur la gâchette de prise ou sur la grosse gâchette (les deux marchent depuis le 20 octobre, pour le coffre, ses orbes et les gisements de cristaux), à 5 m au plus (ou on l'attrape de près) : il s'ouvre d'un coup. Le temps ralentit (×0,3), et 3 orbes montent du coffre, chacune avec le nom de sa récompense dans sa couleur. Depuis le 16 octobre, une orbe n'est plus une boule mais l'icône de sa récompense, celle de la boutique (un bonus prend celle de l'amélioration qui lui ressemble : l'épée pour les dégâts doublés, la cible pour les tirs parfaits, l'anneau de Charge rapide pour l'anneau rapide) ; le morceau d'arc légendaire garde sa boule. Simplifié après les tests du 11 octobre : soulever le couvercle à la main était trop laborieux.
- **Modèle** : le coffre animé du pack *Animated PBR Chest Demo*. Il tombe du ciel et rebondit en apparaissant, puis sautille sur place en attendant. À l'ouverture, son animation ouvre le couvercle d'un coup, avec un rebond, et une lueur dorée sort du coffre.
- **Choix** : on prend une orbe en la visant (6 m au plus) ou à la main, les autres disparaissent. Le ralenti s'arrête au choix, ou au bout de 6 s ; les orbes restent alors jusqu'à la fin de la vague.
- **Récompenses** : toujours les mêmes, dans le même ordre (de gauche à droite pour le joueur face au coffre) : **deux améliorations permanentes** différentes, tirées comme en boutique (raretés et Chance comprises) et dans la couleur de leur rareté, puis **un bonus temporaire**. Au-dessus de chaque orbe, « PERMANENT » ou « TEMPORAIRE », puis le nom de la récompense et sa rareté ou sa durée. Bonus possibles, au hasard :
  - 30 s de dégâts doublés ;
  - 30 s de tirs parfaits : l'anneau est presque entièrement vert, et tout tir lâché à pleine tension compte comme parfait ;
  - 30 s d'anneau accéléré (×1,75, bandes un peu plus larges).
- Les bonus en cours s'affichent sur la montre avec leur temps restant ; un message annonce leur fin. Une amélioration trouvée dans un coffre ne fait pas monter les prix de la boutique (section 7) : c'est un vrai cadeau.

## 14. L'interface

- **Point de départ** (22 octobre) : au lancement, le joueur apparaissait là où se trouvait le casque dans la pièce, par rapport au centre de la zone de jeu, parfois dans la tour ou à côté. Il est maintenant remis en haut de la tour, tourné vers le menu, dès que le casque suit la tête ; puis, avant la première vague, chaque fois qu'on remet le casque (capteur de présence), qu'il se recentre ou que son suivi saute (`PlayerTeleport`, case *Place At Spawn*).
- **Menu principal**, dans le décor en haut de la tour, avant la première vague : Paramètres, Tutoriel, Classement, Quitter. Depuis le 16 octobre, *Jouer* et le choix de la difficulté sont retirés du menu : on tire sur un panneau de difficulté (ou on le clique), puis on frappe le gong, comme le rappelle une phrase en haut de l'accueil. Tout se clique au rayon de la main libre (gâchette), ou se déclenche d'une flèche : un bouton est cliqué, un curseur prend la valeur de l'endroit touché ; « Quitter » demande une confirmation, pour qu'une flèche perdue ou un clic de trop ne ferme pas le jeu : au premier appui (une flèche ou un clic de la main), le bouton affiche « Vraiment ? » et un message dit d'appuyer encore ; un deuxième appui dans les 3 s ferme le jeu (23 octobre ; avant, seule la flèche le demandait). **En cours de partie** (21 octobre) : le bouton menu de la manette gauche (Tab sans casque) ouvre le même menu devant le joueur (à 2,1 m, un peu sous les yeux), le jeu en pause (le temps s'arrête net ; on ne peut ni encocher, ni tirer, ni lancer une déchirure, le carquois ne donne plus de flèche ni la ceinture de grenade, et une flèche lâchée retourne au carquois au lieu de rester figée en l'air) ; il suit le joueur sans gêner la visée de ses boutons (immobile tant qu'il reste à moins de 30° du regard et à sa distance, puis il revient en douceur devant lui) ; un rappel en bas : « Jeu en pause · bouton menu de la manette gauche : reprendre la partie ». Un nouvel appui le referme : il retourne en haut de la tour et le temps repart en douceur. Fermé tout seul à la fin de la partie.
- **Tutoriel** (16 octobre, codé, pas encore testé) : depuis le menu, 4 catégories et 14 fenêtres (13 jusqu'au 22 octobre). **Tir** : tirer à l'arc ; l'anneau et les points faibles (tête ×2, gemme des boss ×3) ; la grenade à flèches (22 octobre, sans vidéo pour l'instant). **Déplacement** : marcher et tourner ; courir et sauter ; glisser ; monter sur la tour. **Vagues** : lancer une vague ; ta vie et la tour ; les boss ; la montre. **Construction** : récolter les cristaux ; choisir un bâtiment ; construire. Chaque fenêtre a un titre, une vidéo en boucle (`Resources/Tutoriel/Tir_1.mp4`… : les 13 tournées le 19 octobre, 76 s en tout, allégées à 49 Mo par `Tools/alleger_videos_tutoriel.ps1` ; sans vidéo, « Vidéo à venir »), deux ou trois lignes, et les flèches pour passer à la suivante. Depuis le 18 octobre, une catégorie s'ouvre sur un **grand écran** (2,4 m de large), toujours à la même place : à gauche du menu, à l'opposé du choix des arcs (45° depuis le menu autour de la place du joueur, à 2,5 m, le bas au niveau du bas du menu ; d'abord caché derrière le menu, puis placé à droite d'après le regard, il chevauchait le choix des arcs), avec « Fermer » ; la liste des catégories reste dans le menu (réglages sur le *Main Menu Panel*). Le menu lui-même est baissé de 25 cm (il était trop haut). Ni l'arc légendaire ni le dragon n'y sont montrés. **Tournage des vidéos** (18 octobre) : dans le casque, avec Unity Recorder (`Archery > Tutoriel > Tournage des vidéos`) ; une caméra de tournage suit la tête en lissant ses mouvements, les deux joysticks enfoncés (ou F8) démarrent et arrêtent l'enregistrement, et la vidéo est écrite directement sous son nom dans `Resources/Tutoriel` (mode d'emploi dans `LISEZMOI.txt`).
- **Paramètres** : volumes général, musique et effets (Audio Mixer) ; rotation au joystick (par crans, continue avec sa vitesse, ou coupée ; coupée, pousser le joystick droit sur le côté affiche « Rotation au joystick coupée », avec où la rallumer) ; vignette de confort pendant le slide, de « Aucune » à 100 %.
- **Grand panneau du score**, dans le décor au-dessus des panneaux de difficulté : numéro de vague, chrono et score. Caché avant la première vague (22 octobre : il n'a rien à montrer avant la partie), il apparaît au gong en se dépliant, à la place des panneaux de difficulté.
- **Montre au poignet** : score, chrono, numéro de vague, PV du joueur, PV de la tour, argent, combo, difficulté, bonus des coffres en cours. Sous la montre, un bouton « Tour » ramène en haut de la tour : on le touche avec l'autre main un court instant (25 s de recharge pendant les vagues, section 3 bis).
- **Voile rouge des PV** : aux tests, il était difficile de surveiller ses PV sur la montre en plein combat. Un voile rouge borde donc la vision, comme la vignette du slide : il apparaît après 10 % de PV perdus, puis rougit et se resserre vers le centre à mesure que les PV baissent. Il suit les PV lentement (environ 1 s), bat comme un cœur sous 25 % des PV, et s'efface quand les PV remontent ou à la mort. Ajouté tout seul par le composant `Player Health` (case *Hurt Vignette*).
- **Messages importants** (début de vague, boss, coffres, cristaux, constructions, mort) : depuis les tests du 12 octobre, ils s'affichent sur un bandeau sombre qui reste dans le champ de vision, un peu au-dessus du regard, 4 à 5 s. Avant, c'étaient des textes posés dans le monde, trop petits, qu'on ratait en tournant la tête. Depuis le 17 octobre, ils sont plus visibles et restent plus longtemps : texte plus grand avec un contour sombre, bandeau plus opaque, durée ×1,6 (4 s au moins). Le message de la vague 6 (« la porte de l'antre s'ouvre ») reste affiché jusqu'à l'entrée dans l'arène.
- **Retours dans le monde** : chiffres de dégâts, « Headshot ! », « Parfait ! » ; particules à l'impact (giclée au corps, étincelles dorées à la tête, poussière dans le décor) et fumée à la mort d'un ennemi.
- **Fin de partie** : un écran apparaît devant le joueur après sa mort, et les ennemis s'enfuient. Il affiche le résumé (score, vague atteinte, difficulté, ennemis tués, headshots, tirs parfaits) et fait saisir le nom au clavier virtuel de XRI (exemple *Spatial Keyboard*). Le classement s'affiche ensuite avec la nouvelle ligne en doré ; *Rejouer* ramène au menu avec un fondu.
- **Classement** : les 10 meilleurs scores, avec le nom, le score, la difficulté et la vague atteinte, du meilleur au moins bon.
- **Le style des menus** (16 octobre, codé, pas encore testé) : inspiré des cadres « Fantasy UI Borders » de Kenney, en plus coloré. Panneaux violet nuit à bordure dorée ornée ; titres en Cinzel Decorative dorée, soulignés d'un trait orné ; boutons ornés de la couleur de leur rôle (émeraude pour valider, saphir par défaut, améthyste pour les flèches, prune pour revenir, rubis pour quitter), qui grossissent et s'éclairent quand on les vise, avec un petit son ; curseurs colorés (or, magenta, cyan, vert) à poignée en losange doré ; cartes de la boutique et des constructions au cadre orné de la couleur de leur offre, avec une petite icône de ce qu'elles font et un texte court (« +50 % de chance de tirer une flèche supplémentaire » ; les règles complètes restent dans la section 6) ; textes en Cinzel (boutons, étiquettes) et Inter (textes longs) ; les panneaux apparaissent en se dépliant. Le même style habille la montre (son bouton « Tour » en saphir, son remplissage d'appui doré), le panneau des vagues, du score et du chrono (numéro de vague doré), et les panneaux de difficulté : un fond sombre devant la planche, une bordure ornée et deux traits de la couleur de la difficulté (vifs si elle est choisie, atténués sinon), le nom en Cinzel Decorative dégradé vers cette couleur (validé le 16 octobre). Enfin, le bandeau des messages (panneau violet à bordure dorée ornée, texte en Cinzel de la couleur du message, deux traits ornés sous les grands messages), la grande barre de PV du dragon (cadre orné, jauge aux coins nets, nom doré en Cinzel Decorative posé sur la barre, repères de phase dorés) et la barre de PV au-dessus des boss des vagues (jauge sombre à bordure dorée, nom doré posé dessus). Dans le monde aussi : les orbes des coffres (l'icône de la récompense à la place de la boule, étiquette en Cinzel sur une plaque ornée de la couleur de la récompense, sceau orné qui tourne derrière l'icône, plus vite et plus clair quand on la vise), les cibles de fin du combat contre le dragon (texte en Cinzel Decorative dégradé vers la couleur du choix sur une plaque ornée, grand sceau doré qui tourne derrière le disque) et les textes flottants (Cinzel, dégradé du clair vers leur couleur, contour violet très sombre). Packs gratuits utilisés (CC0 ou licence libre) : Kenney Fantasy UI Borders, Kenney Interface Sounds, police Cinzel.

## 15. Sauvegarde

Fichiers JSON dans `Application.persistentDataPath` :

- `leaderboard.json` : nom, score, difficulté, vague atteinte, ennemis tués et date de chaque partie, triés du meilleur score au moins bon (100 parties gardées, 10 affichées) ;
- `settings.json` : volumes, rotation au joystick et vignette de confort ;
- plus tard, les succès et les arcs débloqués (arcs comme classes, section 23), dans un fichier choisi par le coéquipier qui s'en occupe.

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

Refaite le 7 octobre sur le modèle de *League of Legends* : défendre une tour attaquée à 360° tout en allant chercher des coffres était trop difficile. Avec un front d'environ 90°, on peut quitter la tour sans être pris à revers.

- Une seule scène : un carré d'environ 75 m de côté, aligné sur la grille. La **base** (la tour) est dans le coin sud-ouest, les montagnes dans son dos ; en haut de la tour, le joueur regarde vers le nord-est, le centre de la carte.
- **3 voies** de 5 m de large, qui zigzaguent un peu :
  - à gauche et à droite, le long des bords, depuis les coins voisins de la base, où apparaissent leurs ennemis (environ 70 m de voie) ;
  - au milieu, en diagonale (en escalier sur la grille), depuis le centre de la carte (53 m).

  Une barricade sur chaque voie, vers 13 m de la tour.
- De la **jungle** en terrasses entre les voies : des arbres, des clairières pour les coffres (et plus tard les matériaux et les emplacements de construction). Les ennemis au sol n'y montent pas et restent sur les voies : la jungle protège des monstres au sol, pas des volants ni des mages.
- **Plan final** (croquis du 9 octobre) : près de la base, de chaque côté de la voie du milieu, une **clairière** au niveau du sol et sans arbres, pour bien voir venir les ennemis. Un escalier de deux étages la relie à la jungle : à droite, le long d'une lisière perpendiculaire à la voie ; à gauche, le long d'une lisière en biais (escalier en dents de scie). Plus loin, entre la voie de gauche et celle du milieu, une forêt traversée par la rivière : elle descend du nord et se sépare en deux bras, l'un vers la voie de gauche, l'autre vers la voie du milieu puis celle de droite. Entre la voie du milieu et celle de droite, une forêt plus montagneuse, avec des rochers.
- Une **rivière** coupe les trois voies juste devant les barricades : les ennemis au sol y avancent 40 % moins vite (`Slow Water`), là où le joueur les vise. Les volants ne sont pas ralentis. Elle est creusée dans le terrain, avec quatre cascades (dont une de 9 m à la source, depuis la montagne). Son eau est animée : facettes qui ondulent, courant qui défile, écume le long des berges et au pied des cascades (`RiverWater`, shader `Archery/Water`, `Docs/Guide_Carte.md` sections 7 et 7 bis).
- Des **montagnes** tout autour, trop raides pour être gravies : la limite de la carte. Elles sont habillées de falaises taillées (roche et pierre, lèvres d'herbe, un relief irrégulier).
- L'**antre du boss** au fond de la voie du milieu : le coin nord-est est coupé par une grande face de pierre de 21 m de large et 18 m de haut, perpendiculaire à la voie. La porte du boss final y est, et le nom du jeu est gravé au-dessus (section 10 bis).
- Des **petits décors** partout sur le terrain (herbes, fleurs, champignons, cailloux, plantes), par petits groupes.
- **Le vent** (15 octobre, testé ; un peu renforcé après le test, force 1,5) : les arbres, les herbes, les fleurs et les plantes ondulent. Des vagues traversent la carte dans le sens du vent, avec des rafales lentes et un frémissement des feuilles ; le pied ne bouge pas, une herbe se couche d'une dizaine de centimètres, la cime d'un grand arbre d'une trentaine. Les ombres suivent. Le sol herbu, les rochers, les champignons, les souches et les falaises restent immobiles. Technique : leurs modèles sont préparés à l'import (`Editor/FoliageWindImporter` : hauteur de chaque sommet dans sa couleur, matériaux passés au shader `Archery/Foliage`, le Lit d'URP avec du vent) ; le vent se règle dans `Scripts/World/Wind.cs`. Rien n'a changé dans la scène : ça marche même sur les objets statiques.
- **Les nuages** (15 octobre, à voir dans Unity) : des nuages en blocs, à la Minecraft, à 80 m de haut : des pavés plats de 10 m de côté et 4 m d'épaisseur, groupés en masses irrégulières qui couvrent environ 40 % du ciel. Ils dérivent lentement avec le vent (2 m/s) et prennent la couleur du ciel de la difficulté : blancs à midi, rosés au coucher du soleil, gris-bleu la nuit, rougeâtres sous le ciel rouge. Leurs faces sont ombrées (dessus clair, dessous plus sombre) ; ils sont légèrement transparents et s'effacent au loin, sans bord visible. Créés au lancement par le `SkyController` (`Scripts/World/CloudLayer.cs`, shader `Archery/Clouds`, matériau `Resources/Clouds`) : rien à poser dans la scène.
- **Sol et voie du milieu** : on garde les aplats de couleur « low-poly » du kit. La voie du milieu et la forêt qui la borde suivent deux droites en diagonale : demi-cases de terre coupées en diagonale, blocs de forêt et pièces d'escalier coupés eux aussi (`Docs/Guide_Carte.md`, section 3). Les côtés d'escalier à découvert sont habillés de bordures d'escalier : de la roche à lèvre d'herbe, dont le dessus suit les marches (section 5). Un sol peint a été essayé le 9 octobre, puis abandonné : son vert ne correspondait pas au reste de la carte.
- **Style cubique**, comme l'image d'exemple du Nature Kit : pas de collines lisses, mais des blocs posés sur une grille de 3 m. Les voies sont au niveau du sol, la jungle en terrasses de 3 m (des falaises le long des voies), avec quelques terrasses à 6 m, des cascades et des ponts. On monte par des rampes et des escaliers à 27° (une pente invisible sous les marches), où l'on peut glisser en slide ; on descend en sautant. Les voies montent et descendent aussi : les ennemis les suivent, mais ne montent pas dans la jungle (ses accès sont interdits dans leur NavMesh).
- Modèles : *Nature Kit* (arbres, rochers, décor) et *Castle Kit* (tour, porte de l'antre ; balistes et catapultes pour plus tard) de Kenney, en licence CC0.
- Montage : `Docs/Guide_Carte.md`.

## 17 bis. Lumière et éclairage

Codé le 15 octobre, pas encore testé dans Unity. Depuis le 16 octobre, toutes les lumières ajoutées sont de vrais objets de la scène, rangés sous un seul objet **« Effet »** (groupes « Torches », « Nature », « Jeu », « Antre ») : on peut les déplacer, les modifier, les dupliquer ou les supprimer, et cacher « Effet » (ou un groupe) les éteint en jeu. L'éditeur fabrique « Effet » à l'ouverture de la scène s'il n'y est pas (il faut ensuite enregistrer la scène) ; menu *Archery > Effets* pour le refaire. Sans lui, tout est fabriqué au lancement. Les réglages sont dans `Assets/_Project/Resources/MapLighting` ; les scripts dans `Scripts/World/Lighting`.

- **L'heure règle tout** : chaque ciel de difficulté a une force des lumières (*Lamps* : 0,15 à midi en Facile, 0,7 au coucher du soleil en Normal, 1 la nuit en Difficile et Impossible). Le jour, tout est discret ; la nuit, tout est vif.
- **Rendu** : HDR et bloom (profil `Data/Map Volume Profile`), Forward+ (beaucoup de petites lumières sans ralentir). Une seule lumière projette des ombres.
- **Torches** : le long des voies, sur les parois (torches murales) ou sur pied, et une quinzaine dans la forêt. Avant la première vague, celles des voies sont éteintes ; au début de chaque vague, elles s'allument en chaîne, du point d'apparition des ennemis jusqu'à la tour ; entre deux vagues, elles couvent. Quand un ennemi apparaît, une vague rouge parcourt les torches de sa voie : on sait d'où il vient.
- **Nature** : lucioles dans la forêt (la nuit), champignons et une partie des fleurs qui luisent la nuit, grappes de cristaux bleu-vert dans les parois de la jungle, pollen doré autour du joueur en plein jour, rayons du soleil dans la forêt quand il est bas, brume au pied des cascades, arc-en-ciel devant la plus haute, parallèle à sa face (le jour ; il s'efface quand on le voit de profil).
- **Rivière bioluminescente** : la nuit, des points lumineux dérivent avec le courant et les berges luisent ; les ennemis (et le joueur) qui marchent dans l'eau laissent des anneaux, lumineux la nuit. Le reflet de la lune est plus fort.
- **Ciel** : aurores (vertes la nuit, rouges sous la lune de sang), étoiles filantes, éclairs suivis du tonnerre sous la lune de sang. L'étalonnage des couleurs change avec la difficulté (plus froid la nuit, plus rouge sous la lune de sang).
- **Jeu** : un anneau au pied de la tour, une lumière et un rayon au-dessus d'elle, verts, orange puis rouges selon ses PV, qui battent plus vite quand elle est abîmée et clignotent quand elle est touchée (le rayon se cache quand le joueur est sur la tour) ; des runes qui tournent au sol des emplacements de construction libres, et grossissent quand on les vise (elles remplacent les anciens coins carrés, retirés du prefab `Build Socket`) ; les âmes des ennemis vaincus qui s'envolent ; la nuit, les projectiles ennemis éclairent le sol, les flèches laissent des traînées lumineuses et une petite lumière douce suit le joueur.
- **L'antre** : des veines de lave dans la montagne autour de la porte ; une au début, d'autres à chaque vague, et elles battent comme un cœur, de plus en plus vite jusqu'au boss final. Elles contournent le nom du jeu gravé au-dessus du porche, qui bat avec elles (section 10 bis). Avec la nouvelle porte (16 octobre), l'éditeur refait une fois ce seul groupe « Antre » à l'ouverture de la scène (il faut ensuite l'enregistrer).

## 18. Architecture technique

```
Assets/_Project/
├── Scripts/                       assemblage Archery.Runtime
│   ├── Core/        vibrations des manettes, sons ponctuels, pulsation, ralenti du temps
│   ├── Bows/        arc, modèle d'arc importé (BowVisual), flèche, réserve de flèches, carquois, anneau de timing, réglages de tir
│   ├── Combat/      PV, zones de touche, dégâts
│   ├── Enemies/     ennemi au sol, volant, tireur et ses projectiles, boss, données des ennemis, apparition
│   ├── Defense/     la tour, son téléporteur, les barricades, le brasero
│   ├── Building/    tower defense : cristaux, socles, panneau de construction, marteau, bâtiments (barricade, piques, givre, baliste)
│   ├── Player/      accès au joueur (tête, mains, arc rangé), mains gantées, PV du joueur, téléportation, fondu au noir
│   ├── Training/    cibles d'entraînement, cibles mobiles
│   ├── UI/          textes flottants (points, dégâts), affichage de la partie (montre, panneau), barres de PV
│   ├── Waves/       gestion des vagues, réglages des vagues, gong
│   ├── Economy/     score, combo, argent
│   ├── Difficulty/  difficultés, choix de la difficulté (panneaux)
│   ├── World/       ciel (skybox, soleil ou lune, ambiance, brouillard), objets de nuit, ambiance sonore et musiques, rivière (zones qui ralentissent, eau animée et cascades)
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

- Les scènes et les prefabs sont montés à la main dans Unity, en suivant les guides de `Docs/` (`Guide_Ennemis.md`, `Guide_Modeles.md`, `Guide_Vagues.md`, `Guide_Difficultes.md`, `Guide_Boutique.md`, `Guide_Teleporteur.md`, `Guide_Menus.md`, `Guide_Boss.md`, `Guide_Deplacements.md`, `Guide_Coffres.md`, `Guide_Volants_Tireurs.md`, `Guide_Barricades_Brasero.md`, `Guide_CorpsACorps.md`, `Guide_Ambiance.md`, `Guide_Effets.md`, `Guide_Carte.md`, `Guide_ArcLegendaire.md`, `Guide_TowerDefense.md`). La grenade de flèches et les arcs comme classes auront les guides de leurs coéquipiers.
- Modèles : arc et flèche de base du pack gratuit Low Poly Style Bows & Quivers de Dimasjk Studio (`Bow Visual Base`, flèche du prefab `Arrow` ; matériau URP `Art/Bows/Bow Pack Matte`), arcs et flèches du pack Easy Weapons gardés pour les arcs des classes (`Bow Visual Classic`, riggé avec Animation Rigging, et `Arrow Classic`), chevalier de Toon RTS Units – Demo pour l'ennemi au sol, coffre d'Animated PBR Chest Demo (son couvercle est un os que `ChestLid` fait tourner à la main, puis son Animator prend le relais), Beholder de RPG Monster Partners PBR Polyart pour le volant (modèle descendu sous le pivot, qui est le centre du corps), mage de Wizard PolyArt pour le tireur (projectiles depuis la tête de son bâton), gant low-poly de Quaternius (CC0, sans os) pour les mains : `Art/Hands/Glove Rig.json` lui donne un squelette et des poids, et `GlovedHand` construit la main au lancement.
- Outils : `Tools/compile_check.py` vérifie que les scripts compilent sans ouvrir Unity, `Tools/shader_check.py` vérifie la syntaxe HLSL des shaders.
- Les réglages (arcs, améliorations, ennemis, vagues, difficultés) sont des ScriptableObjects : on équilibre le jeu sans toucher au code.
- `GameManager` enchaîne les états Menu → Vague → Pause → Fin de partie.
- Pools d'objets (`UnityEngine.Pool`) pour les flèches, les ennemis et les effets.
- Paquet AI Navigation, pour les déplacements des ennemis au sol (NavMesh).
- Tests rapides sans casque avec `Desktop Controls` (section 12) ; tests réels sur Quest 3 via Link.

## 19. Git et GitHub

- Le dépôt (`AntoninCro/Project_Archery`) contient le dossier du projet Unity, avec un `.gitignore` adapté à Unity. Pas de Git LFS : le dépôt fait environ 1 Go.
- Le 13 octobre, l'ancien PC est tombé en panne : le projet a été récupéré depuis GitHub sur un nouveau PC. Ce qui n'avait pas été poussé est perdu, dont le boss final (section 23) : pousser souvent.
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
- [x] Boutique, raretés, améliorations (21), flèches spéciales, arcs à acheter, réparation et reconstruction de la tour.
- [x] Menu principal, paramètres, fin de partie, clavier virtuel, sauvegarde JSON, classement (montage corrigé après les premiers tests).
- [x] Anneau de timing à 3 couleurs (rouge, orange, vert pour le tir parfait), après les tests.
- [x] Après les tests : améliorations sans limite d'achat (chances, puis effets plus forts), flèches en plus avec leurs propres effets, déluge qui divise les flèches en vol, courbe de difficulté exponentielle, prix du mode infini (+20 % par vague).
- [x] Après les tests suivants : prix qui montent à chaque vague et à chaque achat, or en baisse à partir de la vague 5, flèches en plus sans points de touche, raretés fixes, ennemis de plus en plus rapides, volées centrées sur la visée, début de partie plus doux en Impossible (vagues 1 à 5).
- [x] Après les tests du 9 octobre (codé, testé en casque) : anneau vide au centre, où le cercle s'arrête dans le vert pastel au lieu de recommencer (tir « très bon ») ; aide à la visée selon la difficulté (trajectoire complète en Facile et Normal, ligne droite en Difficile) et à la couleur de l'anneau ; voile rouge des PV autour de la vision ; améliorations de dégâts rares et légendaires ; nom des récompenses des coffres à nouveau visible.

Objectif : toutes les consignes du cours sont couvertes.

### Semaine 3 (16 au 22 octobre) : la vision

- [x] Course aux bras et slide. Après les tests : direction donnée par le joystick (on peut reculer), les bras accélèrent la marche, slide plus long et plus rapide.
- [x] Coffres (montés et testés : `Docs/Guide_Coffres.md`).
- [x] Ennemis volants et tireurs (montés, dans les vagues, testés : `Docs/Guide_Volants_Tireurs.md`).
- [x] Barricades et brasero (montés et testés : `Docs/Guide_Barricades_Brasero.md`). Après réflexion, le brasero est sur la tour dès le début et ne s'achète plus ; sa brûlure ajoute 30 % des dégâts de la flèche en 3 s. (Le brasero a ensuite été retiré de la scène le 12 octobre.)
- [x] Ambiance sonore et musiques (montées et testées, boucles provisoires générées : `Docs/Guide_Ambiance.md`).
- [x] Effets visuels des coups (montés ; couleur et taille des particules réglées : `Docs/Guide_Effets.md`).
- [x] Carte finale à trois voies, avec relief, jungle, rivière, montagnes habillées, montées du 2e étage, face du boss au bout de la voie du milieu et petits décors (11 octobre, guide : `Docs/Guide_Carte.md`).
- [x] Rendre la carte jouable : NavMesh refait, ennemis sur les trois voies jusqu'à la tour, gués et barricades à leur place, coffres accessibles ; puis l'équilibrage (réglé à la main après les tests).
- [x] Matériaux et tower defense (section 9, conçu le 11 octobre, monté du 11 au 12 octobre, testé le 12) : cristaux, socles, plan bleu et marteau, barricades, pièges à piques, tour de givre et baliste, équilibrage sur simulation (`Docs/Guide_TowerDefense.md`). Après le test : un plancher sous la baliste, qui flottait au-dessus de son échafaudage ouvert.
- [x] Coup de flèche au corps à corps (section 4.5) : rien à monter, testé (`Docs/Guide_CorpsACorps.md`).
- [ ] Arc légendaire en 3 fragments, avec la pluie de flèches (refait le 20 octobre, section 5 : à tester en casque ; modèle à choisir).
- [x] Grenade de flèches (section 4.6) et arcs comme classes avec leurs succès (section 23) : confiés à deux coéquipiers le 12 octobre, chacun sur sa branche ; arcs fusionnés le 19 octobre, grenade le 22 octobre.
- [ ] README (mis à jour le 23 octobre, avec les noms de l'équipe), tests complets, préparation de la démo (`Docs/Checklist_Demo.md`).

### Derniers jours (19 au 23 octobre)

- [x] Menu des armes au début de la partie et succès (branche `classes-arc` de Cider-Jude, fusionnée le 19 octobre) ; la boutique ne vend plus d'arcs.
- [x] Portail des étoiles et arc cosmique (section 23) ; arc légendaire refait (modèle de Tijmen Matthys, section 5).
- [x] Toit cyberpunk (deuxième secret, derrière les cascades creusées) et les Akimbow, jouables dans la partie (section 23). La ville du toit est à refaire (le joueur s'en charge).
- [x] Menu en cours de partie (bouton menu de la manette gauche, Tab au clavier) : pause, on ne peut plus tirer pendant la pause (21 octobre).
- [x] Avant la première vague : panneau du score caché, panneaux de difficulté montés (22 octobre).
- [x] Grenade à flèches de dovinvilson fusionnée (22 octobre, fork `dovinvilson/Project_Archery`, branche `feature/grenade-fleches`), puis reprise après les tests : vrais dégâts, explosion au contact, 28 flèches, prise à la hanche, recharge lisible sur la montre, modèle de bombe à la mèche allumée, mega-bomba, fenêtre du tutoriel (section 4.6). Testée en partie.
- [ ] Raccourci **F** de la démo (2 de chaque amélioration, tourelles niveau 2) ; joueur figé dans le noir de l'intro du dragon ; départ toujours en haut de la tour (22 octobre) : à tester.
- [ ] Katana : remplacé par le katana et le gantelet le 22 octobre, avec des symboles à tracer ; les symboles ne convainquaient pas : refait le 23 octobre avec des failles vivantes, ouvertes d'une taille et frappées au gantelet (section 5) : à tester.
- [ ] Le katana déclenche le gong (et les cibles), et « Quitter » demande une confirmation à la main aussi (23 octobre) : à tester.

### À faire ensuite (après le 23 octobre)

- [x] **Refonte du katana** (codée le 23 octobre, à tester au casque) : symboles retirés, gantelet et faille gardés, faille vivante qui s'ouvre comme un œil (section 5).
- [ ] **Présentation (PowerPoint)** pour l'oral, et répétitions (`Docs/Demo_Vendredi.md`).
- [ ] Tests au casque des nouveautés (`Docs/Checklist_Demo.md`, partie « Nouveautés du 19 au 23 octobre »).
- [ ] Vidéo `Tir_3` du tutoriel (la grenade) à tourner.
- [ ] Build Windows en *Development Build* (les raccourcis de démo y marchent).
- [ ] Crédits des musiques de l'ami (nom et accord).
- [ ] Ménage : anciens modèles du bouclier et du katana (`Art/Katana` : `Bouclier`, maillage `Katana.asset`), anciennes cascades creusées (`Art/Cyber/Cascade creusee 1/2`), `Assets/sci_fi_crossbow.glb`.

### Après les tests du 12 octobre

- [x] Refonte des vagues (codée le 12 octobre, testée) : 5 vagues plus longues (accalmie, montée, boss à la fin de chaque vague), gong pour appeler le boss plus tôt, or surtout en fin de vague avec des bonus de performance, courbes recalées (section 3 bis).
- [x] Bouton « Tour » sur la montre (codé le 12 octobre, testé), avec 25 s de recharge pendant les vagues (section 3 bis).
- [x] Un boss unique par vague (12 octobre au soir, testé) : Roi Gluant, Mimique vorace, Tortue colossale, Chef de guerre orc, Liche suprême ; plus 13 nouveaux ennemis (section 10).
- [x] Corrections après les tests du soir (12 octobre, testées) : arc plus facile à bander (un quart de tension en moins) ; premier boss à 320 PV et beaucoup moins de renforts ; baliste et pièges plus lents aux niveaux 1 et 2, bâtiments plus forts dans les difficultés à beaucoup d'ennemis ; rampes invisibles aux bouts des deux ponts de la voie du milieu (marches de 60 cm, le joueur n'en monte que 50) ; ruines à la place de la tour détruite (socle, couronne tombée, gravats, fumée) ; messages sur un bandeau qui reste devant le regard ; tableau des scores réorganisé (le texte de la vague passait sous le chrono et sur le score) ; écran de fin qui suit le joueur, « Tu es mort » en grand ; coffres ouverts qui disparaissent après 15 s ; cailloux au pied des cascades.

### Après la panne de l'ancien PC (13 octobre)

- [x] Projet récupéré depuis GitHub (dernier commit du 12 octobre) ; réglages de la scène reportés dans ce document.
- [x] **6 vagues** : la vague 6 devient celle du boss final, le dragon rouge, dans son antre (section 10 bis). Le travail fait sur l'ancien PC n'avait pas été poussé : refait le 13 octobre (codé, à tester dans le casque).

### Finitions

- [x] Embellissement de la base (fait le 9 octobre, testé au casque, images par seconde comprises) : ombres jusqu'à 80 m, occlusion ambiante, couleurs (post-traitement), falaises habillées, décors en touffes, bosquets, rebords et camp d'archers derrière la tour (`Docs/Guide_Carte.md`, section 10).
- [ ] Lumière et éclairage (section 17 bis).
- [ ] De beaux menus : panneaux et boutons avec de vrais contours (par exemple le *UI Pack* de Kenney), et des icônes pour les améliorations de la boutique (*Game Icons* de Kenney).
- [x] Des arcs et des flèches dans la direction artistique low-poly : arc et flèche de base du pack de Dimasjk Studio (11 octobre). Les modèles *Easy Weapons* restent pour les arcs des classes.
- [ ] Les vraies musiques, la lanterne et les yeux qui brillent la nuit.

### En cas de retard

Tout ce qui est prévu en semaines 1 et 2 est indispensable. En semaine 3, on abandonne dans cet ordre : les coffres, les barricades et le brasero, les ennemis tireurs, le slide, puis les ennemis volants.

### Après le rendu

- Tester un système d'XP avec choix d'amélioration en pleine vague.
- D'autres ennemis (brute, élites) et d'autres boss.
- Des légendaires qui attaquent tout seuls (baliste sur la tour, esprit gardien).
- Un équilibrage plus poussé, de nouvelles cartes.

## 22. Choix par défaut, à confirmer

- À la fin du chrono, le boss arrive ; à sa mort, les ennemis restants s'enfuient.
- Une vague ne se termine qu'à la mort de son boss.
- Les PV du joueur sont restaurés entre les vagues ; ceux de la tour ne remontent qu'en boutique.
- On lance la vague suivante en tirant dans un gong.
- Il faut vaincre le boss final de la vague 6 pour gagner.
- Le slide est sur le bouton A ou X.
- La rotation par crans au joystick est activée par défaut (désactivable).
- Le menu se trouve en haut de la tour, dans la même scène que le jeu.

## 23. Idées pour la fin du projet

Notées pendant les tests, à faire une fois le reste en place et seulement si le temps le permet.

- **Les arcs comme des classes**, débloqués par des **succès** : confiés à un coéquipier le 12 octobre, sur sa propre branche. La première version (5 octobre, débloquée par l'expérience, jamais montée) a été retirée pour qu'il parte de zéro.
  - La boutique ne vend plus d'arcs : on choisit son arc dans un menu, au début de la partie.
  - Chaque arc se débloque par un succès (par exemple : tuer 100 ennemis, réussir 10 tirs à la tête parfaits, tuer plusieurs ennemis d'une seule flèche). Le système de succès fait partie de la tâche.
  - Les arcs : l'arc de base ; l'arc court (plus rapide, moins de dégâts et de portée, modèle plus petit) ; l'arc puissant (modèle actuel, bandelettes rouges lumineuses) ; l'arc runique (un peu moins puissant que l'arc actuel, ses flèches traversent un ennemi de plus, bandelettes bleues lumineuses, débloqué en tuant plusieurs ennemis d'une flèche).
  - Deux arcs légendaires viendront plus tard, dont celui de l'easter egg du portail des étoiles.
  - **Arcs légendaires cachés qui changent la façon de jouer** (idées du 15 octobre, pas encore faites ; comment on les trouve reste à décider) :
    - **L'arc à pluie** : on tire en cloche ; au sommet de sa course, la flèche se divise en plusieurs flèches qui retombent en pluie et laissent au sol une zone de dégâts pendant quelques secondes (comme le E de Varus dans *League of Legends*). Bon contre les groupes. À prévoir : montrer la courbe du tir et le cercle d'impact, sinon viser en cloche est trop difficile.
    - **L'arc à surcharge** : quand la flèche est chargée au maximum, un deuxième cercle apparaît et permet de la surcharger. Un tir parfait sur ce deuxième cercle tire un rayon laser en ligne droite. À décider : ce que donne un tir raté sur le deuxième cercle (un tir normal ?), et si le rayon traverse tous les ennemis.
    - **Les flèches volantes** (un « arc » sans corde) : on contrôle deux flèches volantes, une par main ; les mouvements des mains les font avancer, reculer, tourner… Elles blessent ce qu'elles touchent. À décider : comment les mains les dirigent (direction de la manette, distance de la main au corps pour la vitesse), et leur limite (durée, énergie).
    - **L'arc à pluie d'étoiles** (idée du 18 octobre, gardée pour plus tard) : à l'impact, quelques petites étoiles retombent autour et blessent les ennemis proches. Bon contre les groupes, et très visible.
- **Un arc légendaire en 3 morceaux** : refait le 20 octobre avec les classes d'arc (section 5) : 3 fragments trouvés dans les coffres (10 % de chance par coffre), gardés d'une partie à l'autre, et le pouvoir de l'arc à pluie ci-dessus. La première version (19 octobre et avant : 35 % de chance, morceaux perdus à la fin de la partie, flèches qui traversent 2 ennemis) n'a jamais été montée dans la scène.
- **Une grenade de flèches** : confiée à un coéquipier le 12 octobre (section 4.6).
- **Les totems** (gardé pour la fin, 7 octobre) : un totem au bout de chaque voie, protégé par un bouclier tant que le joueur est à plus de 15 m. Chaque totem détruit fait venir moins d'ennemis par sa voie et donne une récompense ; tous détruits, ils font venir le boss final plus tôt, avec un gros bonus de score. Les 6 vagues restent.
- **L'arc légendaire gardé par un boss** (gardé pour la fin, 7 octobre) : ses morceaux ne sont plus dans les coffres, mais dans la forêt, gardés par un ennemi plus fort qu'il faut aller battre.
- **Le boss final dans son antre** : fait le 13 octobre, voir la section 10 bis. Les tourelles construites se téléportent dans l'arène pendant l'intro pour aider, puis retournent à leur place (fait le 15 octobre).
- **Un tutoriel** (en fin de projet) : le jeu s'est beaucoup enrichi. Un mode d'apprentissage, choisi comme une difficulté, présente les gestes un par un : l'arc et l'anneau, la course aux bras et le slide, le téléporteur, la boutique, les coffres, les constructions.
- **Des cinématiques** (tout à la fin, si le temps le permet) : une courte introduction du jeu, et une scène quand on bat le boss final.
- **Des événements de vague** (idée du 12 octobre, à garder pour la fin) : de temps en temps, une vague tire au hasard un événement qui change la façon de jouer. Pistes : brouillard (vue réduite, l'aide à la visée devient précieuse), ennemis invisibles qu'on ne voit qu'au contact de l'eau, de la lumière ou d'une flèche, nuit noire où seuls les braseros éclairent, pluie de cristaux, ennemis plus rapides mais plus fragiles, boss en double… Annoncés au début de la vague sur le bandeau des messages, avec un bonus d'or pour compenser.
- **Du vent** (gardé pour la fin, 11 octobre) : un vent léger, avec des rafales, fait bouger les arbres et les herbes. Piste : un shader qui balance les sommets des feuillages selon leur hauteur, réglé par une direction et une force de vent communes à toute la carte.
- **Easter egg : le portail des étoiles** (12 octobre, le premier d'une série possible) : un secret sur la carte, qui mène à une autre dimension et débloque un arc légendaire pour toujours. **Codé le 18 octobre, pas encore testé dans le casque** : voir `Docs/Guide_Portail.md` (dans l'éditeur, P ouvre le portail, puis fait entrer).
  - **Le portail** : sur le côté droit de la carte (la forêt rocheuse), un totem en forme de portail, entouré de 5 piliers. Chaque pilier (un obélisque) porte une rune un peu cachée : certaines sur la face extérieure, d'autres à son pied (décidé le 18 octobre). Activer les 5 pendant **une même vague** ouvre le portail ; ils s'éteignent à la fin de la vague. Chaque bouton activé s'allume avec un son, pour que le joueur sache qu'il avance. On active chaque rune **à la main**, en la touchant (décidé le 18 octobre).
  - **Une seule tentative par partie.** On entre en traversant le portail, avec un fondu. Pendant ce temps, la carte de base est figée (confirmé le 18 octobre) : la vague, son chrono et les ennemis s'arrêtent, comme le reste de la partie. Le temps du jeu, lui, ne s'arrête pas, sinon la dimension serait figée aussi.
  - **L'autre dimension** : un thème cosmique, avec un ciel plein d'étoiles et de galaxies. Le sol est plat et reflète légèrement le ciel, comme un miroir. Une forêt de grands torii vermillon, aux piliers ronds, est répartie partout. **Fait le 18 octobre, revu le même jour** : un ciel noir criblé d'étoiles rondes, des nébuleuses qui le traversent d'un horizon à l'autre (dont la Voie lactée) et de grandes galaxies spirales lumineuses, sans nuages ni aurore ; un sol miroir très net ; des torii plantés en désordre et bien espacés (environ 125, de 7 m de haut, tailles variées, tous droits : penchés, leur reflet faisait un V peu joli ; laque rouge unie, légèrement métallique, moins vive, pour qu'ils se fondent dans le décor), dont on ne voit au-dessus du sol que les deux piliers : leur haut n'existe que dans le reflet. Tout le décor est fait d'objets de la scène, modifiables à la main (voir `Docs/Guide_Portail.md`). Dans le joueur, les améliorations ne comptent plus : il n'a que son arc de départ (sa classe), sans bonus. Il les retrouve en revenant.
  - **Style bugué** (décidé le 18 octobre) : par-dessus le décor cosmique, le niveau a l'air cassé. Des **panneaux d'erreur** rouges, au style futuriste volontairement hors de la DA du jeu (rouge vif sur noir, coins coupés, police à chasse fixe, lignes de balayage), s'ouvrent et se ferment vite un peu partout, presque chaque seconde : certains affichent « ERREUR » ou une fausse erreur (« THUNK.exe ne répond pas »…), d'autres le **temps restant**, qu'on ne voit nulle part ailleurs. Ce ne sont que des décors : les flèches les traversent et ils ne cachent rien. Ils restent dans le monde (2 à 6 m du joueur, jamais collés au visage) ; les bugs touchent les objets, jamais toute la vue, et ne clignotent pas plus de 2 à 3 fois par seconde (confort, épilepsie photosensible).
  - **Le déroulement** : le joueur découvre le décor jusqu'à la montée de la musique, à 12 s (10 s jusqu'au 18 octobre ; la musique, « Sad Nostalgie », est composée par un ami, décidé le 18 octobre : l'épreuve part sur sa montée). Puis le compte à rebours commence, sur les panneaux : **survivre 1 minute** (30 s jusqu'au 18 octobre ; le monstre est aussi plus rapide et se cache moins longtemps depuis). Les panneaux montent pendant la découverte (décidé le 18 octobre) : le premier s'ouvre à 5 s, puis un autre, de plus en plus vite, jusqu'à 150 au moment où les monstres apparaissent (une quarantaine jusqu'au 20 octobre ; ils s'étalent jusqu'à 8,5 m et 3 m de haut) (sur la montée de la musique, 12 s) ; un très grand panneau « SURVIVEZ » s'ouvre alors (environ 3 m de large à 2,6 m, 60° de la vue ; 50 % plus grand depuis le 20 octobre), les autres se ferment presque d'un coup, puis le rythme de l'épreuve reprend : 2 à 3 panneaux par seconde, qui restent 2,5 à 4,5 s, une dizaine à la fois (plus nombreux et plus longs depuis le 18 octobre) ; un panneau qui se retrouve devant le monstre se ferme aussitôt.
  - **Le monstre corrompu** (il remplace le fantôme le 18 octobre), seul ennemi : un ennemi du jeu « cassé » (morceaux décalés, couleurs séparées en rouge, vert et bleu, saccades). À chaque mort, le suivant prend l'apparence d'un autre ennemi du jeu, et il ne réapparaît jamais dans le dos du joueur. **Revu le 18 octobre** : il apparaît loin, dans le champ de vision du joueur, puis court très vite de pilier en pilier, toujours vers un pilier dans le champ de vision du joueur et de plus en plus près de lui. **Revu encore le 18 octobre** (il faisait plus rire que peur) : ils sont plusieurs (3 en Normal), en silhouettes sombres et étirées aux yeux rouges, et **ne bougent que quand on ne les regarde pas** (comme les anges pleureurs de *Doctor Who*) : regardés, ils se figent ; le regard détourné, ils avancent par saccades, figés dans des poses qui changent à chaque saut. On les voit bouger du coin de l'œil ; seul, on le figerait en le regardant, d'où leur nombre. Ils avancent vers des piliers hors de la vue du joueur (sur les côtés, dans son dos) : sinon, ils venaient d'eux-mêmes se figer sous ses yeux. **On les entend approcher** (décidé le 18 octobre) : un grésillement continu propre à chacun, qui monte quand il avance et se tait presque quand on le regarde, un pas lourd et grave à chaque saut, un grondement quand il fonce ; les sons montent fort à mesure qu'ils approchent, et la musique baisse quand l'un d'eux est tout près. Le reste de son comportement suit l'ancien fantôme, en son 3D : Il se déplace par dashs extrêmement rapides, d'un pilier à l'autre : à chaque fois, il réapparaît derrière un pilier, du côté opposé au joueur, et un peu plus près de lui. Il ne marque une pause que caché derrière un pilier : c'est voulu, l'épreuve doit être difficile. Pour le toucher, il faut anticiper sa sortie, ou contourner le pilier pour le voir. On le tue d'une flèche ou d'un coup de flèche au corps à corps ; un autre fantôme apparaît alors plus loin. Le 20 octobre : beaucoup plus sombre (un noir presque pur, un contour rouge ou cyan trois fois plus faible et plus fin, des bandes de bug sourdes ; les yeux rouges restent) et un peu moins glitché (force du bug baissée d'environ 40 %).
  - **L'échec** : si le fantôme touche le joueur, celui-ci est expulsé. Il revient sur la carte, devant le portail, avec tous ses PV, et le portail se referme jusqu'à la fin de la partie.
  - **La victoire** : s'il tient 1 minute, le joueur revient avec l'**arc cosmique**, un arc légendaire propre à cet easter egg (décidé le 18 octobre) : apparence cosmique, flèches qui laissent une traînée d'étoiles, et l'effet de **l'arc à surcharge** (voir les arcs légendaires cachés : un deuxième cercle après la charge complète, et un tir parfait dessus lance un rayon laser). Fait le 19 octobre, une fois les classes d'arc arrivées (section 5) ; réussir l'épreuve donne aussi un gros bonus de score et une étoile à côté du nom dans le classement (décidé le 18 octobre). Il le garde pour le reste de la partie, et l'arc est débloqué pour toujours dans les classes, au début des parties suivantes.
  - **Avant de commencer** : les arcs choisis comme des classes au début de la partie (confiés à un coéquipier, section 23) doivent être en place.
  - **Conseils techniques** :
    - la dimension est une scène à part, chargée au passage du portail puis déchargée au retour, comme l'arène de MK (décidé le 18 octobre) ;
    - un vrai reflet en temps réel coûte cher en VR : on fait le miroir avec un sol sombre un peu transparent, au-dessus d'une copie à l'envers des piliers et du ciel ;
    - un murmure en son 3D, venant du pilier où le fantôme se cache, aide à le localiser. C'est aussi une belle démonstration des sons spatialisés ;
    - un raccourci clavier de test ouvre le portail, pour le montrer pendant une démo.
  - **Pistes** : un bonus de score et une mention au classement pour ceux qui réussissent ; un fantôme plus rapide selon la difficulté ; d'autres easter eggs du même genre, plus tard. Cette idée recoupe celle de l'arc légendaire gardé par un boss : à décider s'il faut garder les deux.
  - **Temps estimé** : environ 2 jours. Portail, boutons et zone : ½ jour ; fantôme, chrono, victoire et échec : 1 jour ; sol miroir, ciel et réglages : ½ jour.
- **Easter egg : le toit cyberpunk** (le 2ᵉ ; décidé et codé le 20 octobre, pas encore testé dans le casque ; lore : `Docs/Lore.md`, le joueur est un intrus que THUNK essaie de supprimer).
  - **L'entrée** : deux **éclats de miroir glitchés** sont cachés derrière les deux cascades de la rivière du haut (la grande, avec l'arc-en-ciel, et celle juste en aval), chacun dans une **petite grotte creusée** derrière le rideau d'eau (2,4 m de large, 2 m de haut, 1,6 m de profondeur : la pièce de Kenney n'avait qu'un renfoncement de 40 cm ; `Archery > Toit cyberpunk > Recreuser derrière les cascades`). L'eau de ces deux cascades est translucide (on devine la grotte sombre et la lueur cyan de l'éclat) et ne bloque plus : on traverse le rideau d'eau pour entrer dans la grotte, et le rayon des mains passe à travers. On les prend à la main ou en les visant (l'une ou l'autre gâchette, 4 m au plus) ; la montre affiche « Éclat de miroir : 1 / 2 ». Les deux pris, **la porte de l'antre du boss final glitche** (revu le 20 octobre : elle garde son aspect, mais par salves, quelques morceaux de ses battants se corrompent en rectangles sombres, cyan, rouges ou presque noirs ; elle tressaute à peine et grésille) et **se traverse même fermée**. Pas pendant la vague du boss final. Une seule tentative par partie ; les éclats se perdent à la fin de la partie.
  - **Le lieu** (scène `Dimension_Cyberpunk`, construite par `Archery > Toit cyberpunk > Reconstruire le décor`, refaite toute seule quand le décor change de version) : le haut d'un immeuble, la nuit, sous la pluie (quelques gouttes rouges montent), au milieu d'une ville fausse et déchue : les immeubles du *City Kit (Commercial)* de Kenney (depuis le 20 octobre), très assombris, aux fenêtres allumées en néon cyan, rose ou chaud (un masque des fenêtres de leur palette, `Art/Cyber/Ville fenetres.png`), posés sur des fûts sombres à bandes de fenêtres ; sur le toit du joueur, les objets du *Cyberpunk Game Kit* de Quaternius (climatiseurs, réservoir, antennes à lumière rouge, armoire, écrans, lampadaires) ; des tours d'écrans sur quelques toits voisins ; enseignes (« THUNK », « ERREUR », « NÉANT », « COPIE »…), quelques immeubles à moitié chargés, en fil de fer. Le joueur reste sur une zone de 3 m × 3 m entourée d'une rambarde néon : les déplacements sont coupés (pas la rotation), on esquive avec le corps. La carte est figée pendant ce temps, comme pour le portail.
  - **L'arme** : une **arbalète dans chaque main** (*Sci fi crossbow* d'oarfish, CC-BY 4.0, en silhouette sombre au contour néon), le canon dans l'axe du rayon de la main ; la gâchette tire une flèche, rechargement automatique (0,35 s). L'arc et le carquois sont rangés. Sans casque : les deux arbalètes devant la caméra, le clic gauche tire vers le centre de l'écran, une main puis l'autre.
  - **Le déroulement** : la découverte jusqu'à la montée de la musique (12 s ; pour le moment la même que la dimension des étoiles, « Sad Nostalgie ») : les panneaux « INTRUS DÉTECTÉ » montent, sans aucun tireur. Puis un très grand « SUPPRESSION » : les panneaux se ferment et les **tireurs** apparaissent tous d'un coup (en moins d'une seconde) sur les toits voisins ; il faut **tenir 1 minute**. Les tireurs sont des mannequins d'entraînement (des costumes ratés de THUNK) en silhouettes sombres aux yeux rouges, qui tiennent la même arbalète que le joueur ; ils ne bougent pas, se tournent vers lui, s'allument avant de tirer. Leurs balles sont des **éclats d'erreur rouges**, lents (6 à 8 m/s), qui ne viennent que de devant le joueur (70° de chaque côté du regard) : une balle, une rafale de trois, une ligne à hauteur de tête (s'accroupir), une colonne (se pencher), de plus en plus serrées (une attaque toutes les 1,6 s, puis 0,55 s). Quand le joueur tire, un tireur lui répond parfois (ils copient ses gestes). Une flèche efface un tireur en pixels ; un autre le remplace (12 tireurs, puis jusqu'à 20). Les balles ne touchent que la tête et le haut du corps (pas les mains) ; une balle qui frôle la tête siffle.
  - **L'échec** : 3 touches (« TOUCHÉ, encore 2 touches »), puis « INTRUS SUPPRIMÉ » : retour devant la porte, avec tous ses PV ; la porte redevient normale jusqu'à la fin de la partie.
  - **La victoire** : « ÉCHEC DE LA SUPPRESSION », 2 500 points (× la difficulté), un **losange ◆** à côté du nom au classement, et le succès secret « Intrus » (son énigme : « Il te voit clair comme de l'eau de roche. », du joueur, le 20 octobre ; « Derrière l'eau qui tombe, le miroir s'est brisé. » avant). **La récompense : les Akimbow** (les arbalètes jumelles, section 5), en main tout de suite et débloquées pour toujours dans le menu des armes (fait le 20 octobre). Sur le toit, ce sont déjà elles (anneaux, aide à la visée, néons), mais chaque flèche efface un tireur.
  - **Test** : **G** donne les deux éclats (la porte glitche), un deuxième **G** fait entrer.

## 24. Pistes pour plus tard (après le rendu)

Idées du 18 octobre, pour approfondir le jeu une fois la démo passée. Rien n'est commencé.

- **Un lore méta, révélé d'easter egg en easter egg.** Au premier abord, THUNK VR a l'air d'un simple jeu d'arcade coloré. Mais une série de secrets, du plus discret au plus dérangeant, dévoile peu à peu une histoire qui brise le quatrième mur : le jeu finit par parler au joueur. Le contraste fait la force de l'idée (comme dans *Doki Doki Literature Club*, *Inscryption* ou *Pony Island*), et la réalité virtuelle la renforce : le joueur est *dans* le jeu qui lui parle.
  - **D'abord une « bible » du lore** (une ou deux pages) : le secret central, la fin, puis 4 à 6 easter eggs qui en révèlent chacun un morceau. Sans elle, une histoire ajoutée au fil de l'eau ne tient pas. Boîte à idées commencée le 18 octobre : `Docs/Lore.md` (pistes de fond, bizarreries, secrets, quatrième mur, personnages, motifs, actes, fins, garde-fous, et une partie « Canon » pour ce qui est décidé).
  - **Le portail des étoiles est le chapitre 1** : son style bugué et ses fausses erreurs portent les premiers indices.
  - **À coder après la démo** (lore décidé le 20 octobre, voir le « Canon » de `Lore.md`) : les runes du portail n'apparaissent qu'après un certain nombre de parties (nombre à fixer) ; la ligne du reflet au classement (ton nom à l'envers, avec à peu près le score de ta dernière partie, quelques points en dessous, mise à jour à chaque fin de partie).
  - **Des graines déjà là** : les boss portent des prénoms de vraies personnes (Thomas, Jean, Gontrand, Francine, Nolan, MK) ; le jeu connaît le nom que le joueur tape au classement ; le titre gravé dans la roche, la montre, le menu et le classement peuvent changer au fil des découvertes ; un fichier peut « apparaître » dans le dossier de sauvegarde.
  - **Trois pistes d'idée centrale** : les développeurs disparus (les boss sont l'équipe, piégée dans le jeu, et MK celui qui les y a enfermés) ; le jeu qui s'éveille (THUNK comprend qu'il tourne en boucle, et ses erreurs sont ses tentatives de parler au joueur) ; la fausse simulation (ce tir à l'arc n'est qu'un entraînement, et les bugs laissent entrevoir ce pour quoi on entraîne le joueur).
  - **À prévoir** : une sauvegarde de la progression entre les parties (le jeu « se souvient » du joueur), qui recoupe le choix d'arc au début des parties.
- **Un mode démo pour un tutoriel interactif** (idée du 22 octobre, à préciser) : apprendre en jouant, plutôt qu'avec les vidéos du menu. Peut rejoindre l'idée du 21 octobre où le jeu prend possession des mains du joueur pour lui montrer comment mieux tirer (`Lore.md`).
- **Un portage sur PC, sans casque.** Le lore pourrait rendre le jeu assez riche pour une version PC.
  - **Déjà là** : un mode clavier et souris complet (`DesktopControls` : ZQSD, Maj, Espace, clic gauche pour tirer, E pour interagir).
  - **À retravailler** : le tir à la souris, qui doit devenir aussi satisfaisant que l'arc en VR ; un équivalent clavier pour la course aux bras, le slide, le saut et les objets pris à la main ; une interface à l'écran à la place de la montre et des menus à viser.
  - **Un méta encore plus fort sur PC** : le jeu peut changer le titre de sa fenêtre, afficher de fausses fenêtres de plantage, poser un fichier sur le bureau, appeler le joueur par son nom de session…
