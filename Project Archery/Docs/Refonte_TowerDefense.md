# Refonte du tower defense (brainstorm du 27 octobre)

Discussion de conception, puis suivi du travail. Les chiffres donnés ici sont des points de départ : les valeurs de la scène
et de `Data/*.asset` restent celles du joueur, et les textes des nouvelles cartes sont des propositions qu'il valide ou
réécrit. Le système actuel (matériaux, socles, 4 bâtiments) est décrit dans le GDD, section 9.

## Avancement

**27 octobre : le brainstorm est fait** (parties 1 à 8 : les décisions du joueur). Reste à décider : partie 10.

**Les étapes du travail**, dans l'ordre :
1. [ ] **Les modèles** : trouver les assets de chaque construction, avec un aspect différent à chaque niveau (demandé par le
   joueur). La recherche est faite (partie 9, 27 octobre), 5 packs CC0 téléchargés le même jour (partie 9.8), et le
   joueur a ajouté le pack RTS de Quaternius et 14 modèles de Poly Pizza (`Assets/construction`) ; manquent 15 modèles
   de Poly Pizza, puis viennent le choix des modèles, leur import et les prefabs à 3 niveaux.
2. [x] Les caisses de l'ingénieur et les plans ; retirer les matériaux (cristaux, compteur de la montre, prix du panneau).
   **Codée le 27 octobre, pas encore testée au casque** (tests : `Checklist_Demo.md` ; GDD, section 9) :
   - `Building/CrystalSpawner` fait tomber des caisses au lieu des gisements, aux mêmes emplacements et en même nombre ;
     une caisse est le coffre de la carte transformé par `Chest.MakeEngineerCrate` (rayon et coffre teintés de violet, la
     couleur des anciens cristaux), en attendant son modèle ;
   - elle propose 3 lots de plans de bâtiments différents (`ChestRewards.RollPlans`, `ChestRewardKind.Plans`, orbes
     « CONSTRUCTION ») : 2 plans pour la barricade et les piques, 1 pour la tour de givre et la baliste
     (`StructureDefinition.lotSize`, 0 = ces valeurs) ; pour l'instant au hasard, les raretés viendront à l'étape 5 ;
   - le `BuildManager` garde les plans du joueur (`PlansOf`, `AddPlans`, `TotalPlans`) ; un plan construit, un autre plan
     du même bâtiment fait monter d'un niveau ; **en attendant la pose libre (étape 3), les plans se posent sur les
     socles** ;
   - la montre affiche « Plans » à la place de « Mat. », le panneau des socles « Plans : N » et « 1 plan » ;
   - le tutoriel parle des caisses et des plans (`Editor/TowerDefenseStep2Baker`, une fois, sur `Resources/TutorialContent`) ;
   - raccourcis : **B** donne 2 plans de chaque bâtiment (50 matériaux avant), **V** pose une caisse (un gisement avant).
   - **Restent dans le projet, plus utilisés** : `Building/CrystalDeposit.cs` et son prefab, les matériaux de
     `ScoreManager` (`Materials`, `AddMaterials`, `TrySpendMaterials`), le prix en matériaux des bâtiments
     (`StructureDefinition.Level.cost`), `Editor/CrystalTuningBaker`. À supprimer quand le joueur le voudra.
3. [x] Les zones constructibles, la grille, la pose, le déplacement ; retirer les 12 socles et les 3 barricades de départ.
   **Codée le 27 octobre, pas encore testée au casque** (tests : `Checklist_Demo.md` ; GDD, section 9) :
   - `Building/BuildGrid` (créée toute seule par le `BuildManager`, rien à poser dans la scène) découpe la carte en cases de
     3 m (celles de la carte) au lancement, sur quelques images, et classe chaque case de sol plat d'après le chemin des
     ennemis (`MapScan.RunLanes`) : **sur la voie** (à moins de 3 m du chemin, sur le NavMesh, l'avant tourné vers la forêt),
     **au bord** (de 3 à 7,5 m du chemin, à peu près au même niveau, tournée vers la voie), **en hauteur** (2 m au moins
     au-dessus de la voie, à 15 m au plus, tournée vers elle). Pas dans l'eau, à moins de 7 m de la tour, sous un arbre ou
     un rocher, sur un mannequin, ni près des points d'apparition (ennemis 6 m, caisses et coffres 2,5 m). La console donne
     le compte (« BuildGrid : N emplacements… »). Tous les seuils sont des réglages du composant.
   - Chaque case devient un emplacement (`BuildSocket`, `IsCandidate`), invisible et impossible à viser au rayon tant qu'il
     est vide ; construit, il se vise comme les anciens socles (panneau : améliorer, déplacer).
   - **La pose, à la montre** (`Building/BuildPlacement`, `UI/WatchPlansButton`) : un bouton « Plans N » apparaît sur la
     montre, à côté du bouton « Tour » (copié de lui au lancement, à 13 cm), dès que le joueur a des plans. On le touche
     avec l'autre main (0,3 s) : l'**hologramme** du bâtiment (ses maillages en plan bleu, sans rien qui tire ni se vise) suit
     le rayon de la main qui porte la montre (14 m au plus) et se cale sur l'emplacement visé, **rouge** là où il ne se pose
     pas, avec une étiquette au-dessus (le bâtiment, ses plans, ce qui ne va pas). Pendant la pose, les runes des emplacements
     où ce bâtiment peut aller s'allument autour du joueur (12 m), **orange sur la voie, bleues au bord, vertes en hauteur** ;
     celle que vise l'hologramme brille plus. **Joystick** de cette main : gauche et droite tournent le bâtiment (crans de
     45°, depuis le sens de l'emplacement), haut et bas passent à un autre plan ; la rotation du joueur au joystick est
     coupée pendant la pose. **Gâchette** : poser (les marteaux apparaissent, comme avant) ; **poignée**, ou le bouton
     « Annuler » de la montre : ranger le plan. Posé sur un bâtiment du même type, le plan le fait monter d'un niveau.
   - Sans casque : **F2** sort un plan, puis passe au suivant, puis le range ; la molette tourne, **E** pose (le viseur
     dit ce qui va et ce qui ne va pas).
   - En hauteur se posent les bâtiments du bord de la voie (`StructureDefinition.Fits`) ; `BuildSlot` gagne `Height` et
     `Tower` (la tour : pour le seau d'huile et la baliste sur la tour, étape 5).
   - **Déplacer** : sur un bâtiment construit, le panneau propose « Déplacer » (gratuit, entre les vagues) ; son hologramme
     suit alors le rayon comme un plan, et se pose sur un emplacement libre de la même sorte (sur sa propre place : il y
     reste, tourné). Le début d'une vague annule.
   - Les 12 socles posés à la main (sans bâtiment) et les 3 barricades de départ sont **désactivés au lancement** : la
     scène n'est pas modifiée (cases *Remove Authored Sockets* et *Remove Start Barricades* du composant pour les garder).
   - Le raccourci **F** pose ses tourelles niveau 2 sur les 6 emplacements au bord de la voie les plus proches de la tour,
     à 8 m au moins les uns des autres ; le tutoriel « Choisir un bâtiment » parle de la grille (le baker de l'étape 2).
   - **Choix du joueur, le 27 octobre au soir** : une première version visait l'emplacement puis choisissait le plan dans le
     panneau, et tournait les bâtiments toute seule. Le joueur a gardé les **zones calculées** au lancement (plutôt que
     peintes à la main) et voulu la version de la partie 3 pour le reste : **le plan sorti de la montre, son hologramme, et
     le joystick qui le tourne**. Le sens de l'emplacement reste le point de départ de la rotation (barricade face à la
     forêt, tourelles face à la voie) : **une barricade tournée de travers n'arrête plus bien les ennemis** (ils sont
     arrêtés du côté de son avant).
4. [ ] L'état « projeté » des ennemis (partie 6.2), la base de toutes les constructions physiques.
5. [ ] Les constructions nouvelles, par lots.
6. [ ] Les fusions.
7. [ ] Les modifications du décor.
8. [ ] Le talisman de l'ingénieur revu (et le duo Réseau).
9. [ ] Ce qui doit suivre : l'arène de MK, le raccourci **F**, le tutoriel « Construction », la montre, le clavier.

Les étapes 2 à 9 sont un ordre proposé, à revoir avec le joueur au fur et à mesure.

## 1. Le plan du joueur (26 octobre)

Ses mots, dans l'ordre de ses prochaines étapes (après la fin de la refonte roguelike) :
- on retire le système de matériaux ;
- on le remplace par des sortes de coffres contenant le choix entre plusieurs tourelles ;
- on augmente grandement le nombre de tourelles et de constructions de manière générale ;
- on permet de construire à n'importe quel endroit sur les zones qui permettent les constructions.

Pourquoi les matériaux existaient (GDD, section 9) : aux premiers tests, rester en haut de la tour rapportait plus que de
sortir. La refonte doit garder une raison de quitter la tour pendant la vague.

## 2. Les caisses de l'ingénieur (ossature A, choisie le 27 octobre)

- Elles **remplacent les gisements de cristaux** : aux mêmes emplacements (`Map > Crystal Spots`), pendant l'accalmie, avec
  leur propre rayon de lumière, d'une autre couleur que celui des coffres dorés.
- **Combien** : pour l'instant, les valeurs des gisements (3 par vague, une de plus toutes les 2 vagues, 6 au plus).
- **On l'ouvre comme un coffre** : 3 orbes, une seule à prendre. Chaque orbe est un **lot** : « 1 baliste », « 3 mines »,
  « 2 barricades ». Les communes viennent par lots, les légendaires seules (les rares : partie 10).
- Ce qu'on prend devient un **plan**, rangé sur la montre (« Plans : 3 »), à poser quand on veut, pendant la vague ou la
  pause. L'orbe dit la fusion possible (« Fusion possible avec ta baliste »).
- **Pas de caisse de départ** avant la vague 1 (décidé le 27 octobre), et **plus de barricades de départ** : la tour n'a
  que ce que le joueur construit.
- Proposé, pas encore tranché : le coffre de boss proposerait parfois une construction légendaire à la place d'un objet.

## 3. Construire n'importe où (décidé le 27 octobre)

- **Des zones peintes sur la carte**, de quatre sortes :
  - **voie** : barricades, pièges ;
  - **bord** de la voie : tourelles ;
  - **hauteur** (terrasses de la jungle, falaises) : tourelles, avec un bonus de portée (partie 8) ;
  - **la tour** (le sommet) : le seau d'huile, la baliste sur la tour.
- **Une grille** (par exemple les cases de 2,5 m de la carte) : en VR, poser au centimètre près est pénible ; la grille
  rend la pose rapide et évite les chevauchements. Une construction prend 1 case, une grosse 2 × 2.
- **La pose** : on sort un plan de la montre, son hologramme suit le rayon, les zones permises s'allument (seulement à ce
  moment), le joystick le tourne (barricades), la gâchette le pose, puis **le marteau**, comme aujourd'hui (3 coups).
  Au clavier : E pose, la molette tourne (proposé).
- **Déplacer une construction entre les vagues**, gratuitement : se tromper de place ne doit pas coûter la partie.
- **Pas de labyrinthe** : les ennemis restent sur les trois voies ; sur la voie, une construction est un obstacle à casser
  (barricade) ou un piège à traverser.
- **Plafond : 40 constructions** (pour le Quest) ; une fusion compte pour une seule.
- **Les ennemis ne cassent que les barricades**, pour l'instant. Piste pour plus tard : un ennemi saboteur qui s'en prend
  aux constructions.

## 4. Les niveaux (décidé le 27 octobre)

- **Poser un plan sur la même construction la fait monter de niveau**, jusqu'à 3 (comme les pièces de la flèche). La
  caisse pose donc un vrai choix : une deuxième baliste, ou la première au niveau 2 ?
- **Chaque construction a un aspect différent à chaque niveau** (demandé par le joueur : étape 1, partie 9).

## 5. Le talisman de l'ingénieur, revu (décidé le 27 octobre)

- Il **remplace la surcharge** actuelle (cadence ×2 / ×4 / ×6, portée ×1,5, GDD section 6) : avec le talisman, **tirer
  sur une construction déclenche sa réaction** (colonne « Avec l'ingénieur » de la partie 6). Sans le talisman, une
  construction ne fait que son effet de base.
- **Un délai de 3 / 2 / 1 s par construction**, selon le niveau du talisman : sans lui, une volée de multitir ou de déluge
  la déclencherait des centaines de fois.
- **La mine** explose et reste en place : on peut retirer dessus, et les ennemis peuvent encore la déclencher en marchant
  dessus. **La barricade** : n'importe quel tir la répare (aujourd'hui, seulement un tir parfait qui la traverse).
- **Une fusion** fait les réactions de ses deux constructions à la fois.
- Le texte de la carte est à réécrire par le joueur. Le duo **Réseau** (fût conducteur + talisman) : partie 10.

## 6. Les constructions

### 6.1 Le catalogue (27 octobre)

Les raretés sont celles proposées au brainstorm ; « à fixer » pour les autres.

| Construction | Où | Toute seule | Avec l'ingénieur | Rareté |
|---|---|---|---|---|
| Baliste *(existe)* | bord | un carreau sur l'ennemi le plus proche | une volée de 5 carreaux | commune |
| Tour de givre *(existe)* | bord | ralentit autour d'elle | gèle tout le monde 1,5 s | commune |
| Piques *(existent)* | voie | blessent ceux qui passent | jaillissent toutes en même temps | commune |
| Barricade *(existe)* | voie | bloque les ennemis au sol | n'importe quel tir la répare | commune |
| Mine | voie | explose au premier passage, se réarme à chaque vague | explose tout de suite et reste en place | commune |
| Goudron | voie | ralentit | ta flèche l'enflamme : la flaque brûle | commune |
| Arbalète à répétition | bord | rafales rapides et faibles, bonne contre les volants | cadence doublée 5 s | commune |
| Mortier | bord, hauteur | obus en cloche, dégâts de zone, portée minimale | tire là où ta prochaine flèche se plante | rare |
| Tour de foudre | bord | un éclair qui saute entre 3 ennemis | décharge sur tous les ennemis à portée | rare |
| Lance-flammes | bord | cône de feu, brûlure (le retour du brasero) | 5 s : tes flèches qui passent au-dessus s'enflamment | rare |
| Tremplin | voie | projette les ennemis en l'air | projette tout le groupe | rare |
| Phare | hauteur | dans son rayon, les points faibles des ennemis brillent | marque tous les ennemis à portée | rare |
| Anneau élémentaire | partout, debout | une flèche qui le traverse prend du feu, de la foudre, ou se dédouble (un anneau par effet) | — | rare |
| Miroir | bord | renvoie ta flèche qui le touche vers l'ennemi le plus proche | à proposer (partie 10) | rare |
| Catapulte | bord | attrape un ennemi et le lance sur un groupe : il devient un boulet, qui blesse les deux | en lance 3 d'un coup | à fixer |
| Soufflerie | bord | un cône de vent repousse les ennemis vers la forêt et dévie les volants | une rafale qui projette tout le cône 8 m en arrière | à fixer |
| Pendule | au-dessus de la voie (2 × 2) | un boulet se balance en travers de la voie et fauche les ennemis | un balancement deux fois plus fort | à fixer |
| Geyser | voie | jaillit à intervalles et projette à la verticale ce qui est dessus | jaillit tout de suite, deux fois plus haut | à fixer |
| Bulle | bord | enferme un ennemi dans une bulle qui monte doucement ; crevée, elle le laisse tomber | 3 bulles d'un coup | à fixer |
| Lance-filet | bord | attrape un volant et le fait tomber au sol, étourdi | un filet large : tous les volants à portée | à fixer |
| Trappe | voie | s'ouvre sous un groupe : les ennemis ressortent plus loin en arrière | s'ouvre tout de suite | à fixer |
| Tour-aimant | bord | attire les ennemis vers elle par impulsions (l'attraction du fût aimant) | une grosse impulsion qui attire tout à 10 m | à fixer |
| Seau d'huile *(idée du joueur)* | la tour | une cible à côté du seau : la toucher renverse l'huile, qui blesse tous les ennemis autour de la tour ; il se remplit ensuite | à proposer (partie 10) | à fixer |
| Baliste sur la tour | la tour | une grosse baliste à côté du joueur | tir perforant | légendaire |
| Esprit gardien | partout | un esprit qui chasse les ennemis isolés | il fonce sur ta cible | légendaire |

Les deux légendaires viennent de la liste « Après le rendu » du GDD (section 21). Retirés le 27 octobre : le bélier, le
rocher roulant et le tourniquet. Le miroir : au brainstorm, il ne renvoyait la flèche que si on lui tirait dessus ; sans
l'ingénieur, il ne servait à rien, d'où le renvoi devenu son effet de base.

### 6.2 La physique des ennemis

- **Aujourd'hui**, un ennemi ne peut être poussé qu'à plat, en glissant sur le NavMesh (`Enemy.Push`, le coup de poing de
  la main de la matrice). Pour le projeter, il faut un **état « projeté »** : une trajectoire en cloche calculée (pas de
  ragdoll : léger pour le Quest et prévisible), puis l'atterrissage sur le NavMesh. Toutes les constructions physiques
  s'en servent (tremplin, catapulte, soufflerie, pendule, geyser, bulle, lance-filet, trappe).
- **L'atterrissage** joue avec la carte :
  - une **chute** fait des dégâts selon la hauteur ;
  - **retomber sur un autre ennemi** blesse les deux et renverse l'autre (les quilles) ;
  - **contre une falaise ou un mur** : étourdi 1 s ;
  - **dans la rivière** : emporté quelques mètres par le courant, puis ralenti comme aujourd'hui.
- **Les boss** ne sont jamais projetés et sont poussés moitié moins. **Les volants** ne craignent que le vent et le filet.
- **Le tir au vol** : toucher un ennemi en l'air rapporte plus de points, comme au ball-trap.

## 7. Les fusions (décidées le 27 octobre)

**Le principe** (le joueur, le 27 octobre) : deux constructions voisines ne forment pas un duo ; on pose le plan d'une
construction **directement sur une autre** avec laquelle elle peut fusionner, et elles deviennent **une construction
nouvelle**.

- Un hologramme doré montre le résultat et son nom, puis viennent les coups de marteau.
- La fusion prend la place de la construction du dessous. Si elle doit être sur la voie (le Volcan), on pose le plan sur
  celle de la voie.
- **Son niveau** : le plus haut des deux ; poser l'un des deux ingrédients dessus la fait monter, jusqu'au niveau 3.
- On ne la défait pas (un choix de partie), mais on peut la déplacer entre les vagues.
- Pas de fusion à trois ; les légendaires ne fusionnent pas.
- **Toujours affichées** : pas de « ??? » à découvrir (décidé le 27 octobre).
- Dans l'arène de MK, les fusions qui tirent y vont.
- Pas de duo entre les pièces de la flèche et les constructions (le joueur : ça complexifie le jeu pour rien).

| Fusion | Où | Ce que ça donne |
|---|---|---|
| Baliste + Tour de foudre | bord | **Baliste foudroyante** : chaque carreau lance un éclair qui saute entre 3 ennemis |
| Baliste + Lance-filet | bord | **Harpon** : un carreau à chaîne qui cloue un volant au sol, ou tire un ennemi 5 m en arrière |
| Baliste + Miroir | bord | **Baliste à ricochet** : le carreau rebondit d'ennemi en ennemi, 3 fois |
| Baliste + Phare | bord, hauteur | **Baliste de précision** : elle vise les points faibles (mêmes multiplicateurs que le joueur) |
| Tremplin + Arbalète à répétition | voie | **Stand de tir au pigeon** : projette les ennemis et les abat en l'air (dégâts ×2) |
| Mortier + Tour de givre | bord | **Mortier de glace** : les obus laissent une zone de glace et gèlent 1 s |
| Mortier + Tour-aimant | bord | **Point de chute** : attire les ennemis en un point, puis le bombarde |
| Geyser + Lance-flammes | voie | **Volcan** : projette les ennemis, qui retombent dans de la lave |
| Geyser + Tour de givre | voie | **Fontaine gelée** : l'ennemi gèle en l'air et éclate en retombant, en blessant ceux d'en dessous |
| Catapulte + Mine | bord | **Bombe humaine** : l'ennemi lancé explose en retombant |
| Soufflerie + Lance-flammes | bord | **Chalumeau** : un long jet de feu qui repousse en brûlant |
| Soufflerie + Tour de givre | bord | **Souffle polaire** : un vent glacé ; les gelés glissent loin et renversent les autres |
| Soufflerie + Anneau élémentaire | partout | **Anneau du vent** : tes flèches qui le traversent vont plus vite, plus loin, et percent |
| Bulle + Tour de foudre | bord | **Orbe d'orage** : les bulles deviennent électriques, l'éclair saute de l'une à l'autre |
| Bulle + Mine | bord | **Bulle piégée** : elle éclate tout en haut, puis l'ennemi explose en touchant le sol |
| Piques + Lance-filet | voie | **Filet à pointes** : les volants pris sont tirés sur les piques et y restent cloués |
| Trappe + Piques | voie | **Oubliette** : ceux qui tombent prennent très cher ; les petits n'en ressortent pas |
| Pendule + Lance-flammes | voie | **Boulet ardent** : le boulet enflammé met le feu à la voie sur toute sa course |
| Goudron + Lance-flammes | voie | **Brasier** : un foyer qui brûle en continu |
| Goudron + Tour de givre | voie | **Verglas** : les ennemis glissent et tombent régulièrement (étourdis 1 s) |
| Barricade + Miroir | voie | **Mur-miroir** : renvoie les projectiles des mages et de la liche |
| Barricade + Tremplin | voie | **Rempart à ressort** : ceux qui la frappent sont projetés 5 m en arrière |
| Phare + Miroir | bord, hauteur | **Rayon** : le faisceau du phare, renvoyé par le miroir, brûle tout le long d'une voie |
| Tour de foudre + Tour-aimant | bord | **Bobine** : elle attire les ennemis et les électrocute |

Chaque construction (sauf les légendaires et le seau d'huile) a au moins une fusion. Les noms évitent ceux des duos de la
flèche (Tempête, Blizzard, Patinoire, Flipper…).

## 8. Les modifications du décor (décidées le 27 octobre)

L'hologramme les annonce quand on pose un plan au bon endroit.

| Lieu | Construction | Effet |
|---|---|---|
| Bord de la rivière | Tour de foudre | l'eau est électrifiée : elle blesse ceux qui la traversent |
| Lit de la rivière | Geyser | un geyser d'eau, deux fois plus haut |
| Bord de la rivière | Tour de givre | l'eau gèle par plaques : les ennemis y glissent et tombent, au lieu d'être seulement ralentis |
| Rivière | Goudron | la nappe flotte et s'étale le long du courant |
| Hauteur | les tourelles qui tirent | +25 % de portée |
| Hauteur | Mortier, Catapulte | portée ×1,5 |
| Hauteur | Bulle | les bulles montent deux fois plus haut (la chute fait plus mal) |
| Bord d'une terrasse ou d'une falaise | Soufflerie, Tremplin, Catapulte | les ennemis projetés tombent de plus haut (la règle de la chute, rien à coder en plus) |

## 9. Étape 1 : les modèles

Chaque construction a un aspect différent à chaque niveau (1, 2, 3), et chaque fusion a le sien. **Recherche du
27 octobre ; rien n'est encore téléchargé** (le joueur choisit d'abord). Tout nouveau pack va dans `Credits_Licences.md` et
le README, avec sa licence (créditer le CC-BY).

### 9.1 Les règles des niveaux

Le style du jeu : les kits Kenney (CC0), les monstres Polyart de Dungeon Mason, quelques modèles Quaternius. On garde des
modèles simples, en couleurs franches ; les modèles « Poly by Google » de Poly Pizza varient beaucoup de style, à choisir un
par un.

- **Niveau 1** : bois et pierre brute (`wood-structure`, bases `*-bottom-*` du Tower Defense Kit).
- **Niveau 2** : un étage de plus (`*-middle-*`), un fanion (`flag-pennant` du Castle Kit), des ferrures.
- **Niveau 3** : le sommet (`*-top-*`, `*-roof-*`), un drapeau (`flag-banner-long`), des cristaux lumineux
  (`tower-round-crystals`, `detail-crystal`), une légère lueur.
- Un peu plus grand à chaque niveau, et une couleur par niveau pour la lisibilité (par exemple bronze, argent, or).
- **Le chantier** : le Tower Defense Kit a 6 étapes de construction (`tower-round-build-a` à `-f`, `tower-square-build-a` à
  `-f`) ; elles pourraient remplacer le plan bleu qui se remplit pendant les coups de marteau.

### 9.2 Déjà dans le projet (rien à télécharger)

- **Tower Defense Kit (Kenney, CC0)** : tours rondes et carrées en pièces (bases, 3 étages a / b / c, sommets, toits, cristaux),
  armes `weapon-ballista`, `weapon-cannon`, `weapon-catapult`, `weapon-turret`, munitions `weapon-ammo-arrow`, `-boulder`,
  `-bullet`, `-cannonball`, échafaudages `wood-structure` (et `-high`), cristaux et rochers de détail.
- **Castle Kit (Kenney, CC0)** : engins de siège `siege-ballista`, `siege-catapult`, `siege-trebuchet`, `siege-ram`,
  `siege-tower` (chacun avec une version détruite), drapeaux (`flag`, `flag-pennant`, `flag-banner-short`, `-long`,
  `flag-wide`), murs, portes, herse (`metal-gate`), pont-levis.
- **Nature Kit (Kenney, CC0)** : rochers (dont des plats et des hauts), souches, rondins, palissades (`fence_*`), feux de camp
  (`campfire_*`), pots (`pot_small`, `pot_large`), tentes, ponts.
- **Bomb (Quaternius, CC0)**, déjà utilisée pour la grenade.

### 9.3 À télécharger : CC0 (rien à créditer)

| Pack ou modèle | Où | Pour |
|---|---|---|
| Modèles Quaternius de [Poly Pizza](https://poly.pizza/u/Quaternius) : *Bear Trap* (3 versions : [1](https://poly.pizza/m/UTau9hyAnm), [2](https://poly.pizza/m/rYCSWZlgdW), [3](https://poly.pizza/m/fqv7qtGQYH)), [*Hazard Spike Trap*](https://poly.pizza/m/7eNPB3UYWV), [*Trap Door*](https://poly.pizza/m/PALqVBff9b), [*Trapdoor*](https://poly.pizza/m/Ph6MpmwySS), [*Spring*](https://poly.pizza/m/vKySckBbyb), [*Landmine*](https://poly.pizza/m/PtqkseZo9O), [*Exploding Barrel*](https://poly.pizza/m/1orHe0kCc1), *Barrel* ([1](https://poly.pizza/m/zjCQP1TAci), [2](https://poly.pizza/m/Eko3cjAMW9)), *Bucket* ([1](https://poly.pizza/m/83obI9bNun), [2](https://poly.pizza/m/tKTttwROq7)), [*Cauldron*](https://poly.pizza/m/QaWJOPa6Gt), *Cannon* ([1](https://poly.pizza/m/J15vlPVvKK), [2](https://poly.pizza/m/qIAvoaawib)), [*Ghost*](https://poly.pizza/m/Iip30bDHmu), [*Ghost Skull*](https://poly.pizza/m/TX8r9WBXpe), [*Watch Tower*](https://poly.pizza/m/cMxuj2gt7D), [*Archery Towers*](https://poly.pizza/m/IA5qOjIAiT), [*Torch*](https://poly.pizza/m/Gq38E7hFZw), [*Lever*](https://poly.pizza/m/KfauRUSZer) | Poly Pizza (un par un) | pièges, trappe, tremplin, mine, goudron, seau d'huile, mortier, esprit gardien, phare |
| [Kay Lousberg sur Poly Pizza](https://poly.pizza/m/CtHBJ1ufeW) : *Lantern* ; [*Mirror*](https://poly.pizza/m/2WQIUVj5qr) (Isa Lousberg) | Poly Pizza | phare, miroir (licence à vérifier sur la page) |
| [KayKit Dungeon Remastered](https://kaylousberg.itch.io/kaykit-dungeon-remastered) (Kay Lousberg, plus de 200 modèles) : `floor_tile_big_spikes`, grilles au sol ouvertes et fermées (`floor_tile_grate_open`, `floor_tile_big_grate_open`…), barrières (`barrier`, `barrier_column`, `barrier_corner`…), `keg`, tonneaux (`barrel_small_stack`…), bannières en 6 couleurs, torches, murs à échafaudage | itch.io (prix libre, 0 possible) ou [GitHub](https://github.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0) | piques niveau 3, trappe, barricade niveau 3, goudron, couleurs de niveau |
| [Kenney Platformer Kit](https://kenney-assets.itch.io/platformer-kit) (plus de 140 modèles) : piques (*Trap Spikes*, *Trap Spikes Large*, *Spike Block*), scie (*Saw*) ; noms à vérifier dans le zip | kenney.nl, itch.io | piques, pendule |
| [Quaternius Ultimate Fantasy RTS](https://quaternius.com/packs/ultimatefantasyrts.html) (128 modèles) : des bâtiments **en plusieurs stades d'évolution** ; lesquels, à vérifier dans le zip | quaternius.com | niveaux des tourelles |
| [Quaternius Fantasy Props MegaKit](https://opengameart.org/content/fantasy-props-megakit), version gratuite (94 modèles) : armes, outils, coffres, potions, mobilier | OpenGameArt | habillage des niveaux |
| [Quaternius Medieval Village MegaKit](https://opengameart.org/content/medieval-village-megakit), version gratuite (176 pièces modulaires) | OpenGameArt | toits, murs et étages des niveaux 2 et 3 |
| [KayKit Medieval Hexagon](https://kaylousberg.itch.io/kaykit-medieval-hexagon) (plus de 200 modèles) : moulin à vent, moulin à eau, puits, mine ; bâtiments en 4 couleurs | itch.io (prix libre, 0 possible) | ailes de la soufflerie, puits du geyser et du seau |

### 9.4 À télécharger : CC-BY (créditer l'auteur)

Les modèles « Poly by Google » de Poly Pizza (l'ancienne bibliothèque Poly de Google) sont en CC-BY ; vérifier la licence sur
chaque page avant de télécharger.

| Modèle | Pour |
|---|---|
| *Magnet* (Poly by Google : [1](https://poly.pizza/m/dD2RIIea6WR), [2](https://poly.pizza/m/e6JzvzwGt7v), [3](https://poly.pizza/m/7dHLjQ8eZhu)) | tour-aimant |
| *Lighthouse* (Poly by Google : [1](https://poly.pizza/m/0t2ZYRBsqX-), [2](https://poly.pizza/m/7H8is9jrGeB), [3](https://poly.pizza/m/bk72ChfpGo-)) | phare niveau 3 |
| *Catapult* (Poly by Google : [1](https://poly.pizza/m/9ahVI0EijEi), [2](https://poly.pizza/m/7P_ZOjZbbMJ)) | catapulte (si les engins de Kenney ne suffisent pas) |
| [*Giant Crossbow*](https://poly.pizza/m/5XoZ_5sp13S) (Aimé Tribolet), [*Crossbow*](https://poly.pizza/m/kHb0kA11oD) (CreativeTrio) | arbalète à répétition, lance-filet |
| *Fan* ([J-Toastie](https://poly.pizza/m/dPExITRhhZ), [Zsky](https://poly.pizza/m/wYMa7vha9k)), [*Large fan*](https://poly.pizza/m/a2ManZQPrEn) (Poly by Google) | soufflerie |
| [*Spike Mine*](https://poly.pizza/m/4VRVeCH1sHm) (Aaron Clifford) | mine niveau 3, boulet hérissé |
| *Fire Pit* (CircuitZ : [1](https://poly.pizza/m/DsQslCOoz0), [3](https://poly.pizza/m/arYSisaHyX)), *Cauldron* ([Zsky](https://poly.pizza/m/ctLCiQVn7u), [Vaporworks](https://poly.pizza/m/hUBrqAm5IP)) | lance-flammes, brasier, seau d'huile niveau 3 |
| *Mirror* ([Zsky](https://poly.pizza/m/fQZYQYysyU), [MilkAndBanana](https://poly.pizza/m/HIeVDCNgbA)) | miroir |
| [*Shark Gun*](https://poly.pizza/m/TQMWsUDw8B) (theking1322) | harpon (à voir : rien ne le décrit) |
| [*Large Electric Generator*](https://poly.pizza/m/ZPlQHwiqTp) (miro_art_studio) | tour de foudre (style à vérifier) |

### 9.5 Écartés ou à vérifier

- [*Traps Pack*](https://assetstore.unity.com/packages/3d/props/traps-pack-257557) (Unity Asset Store) : piège à loup,
  piques, lame en pendule… mais payant, licence de l'Asset Store.
- [*PSX Traps*](https://wildenza.itch.io/psx-traps) : CC0 mais payant (2,64 $ au moins) et dans un style horreur.
- [*3D Spike pack*](https://matsoj.itch.io/spikes) (matsoj, gratuit, 16 piques) : licence non précisée.
- [*Turret Pack 01*](https://t-allen-studios.itch.io/turret-pack-01) (T Allen Studios, prix libre, usage commercial permis
  sans crédit) et [*Free Defence Tower 3D Low Poly*](https://free-game-assets.itch.io/free-defence-tower-3d-low-poly-models/purchase) :
  licences à lire ; [*Low Poly Tower Defense Pack*](https://harning.itch.io/low-poly-tower-defense-pack) (1 $, licence
  maison).
- Les packs de tourelles « avec niveaux » trouvés sont payants, en voxels ou de licence floue : les kits Kenney à étages font
  mieux l'affaire.

### 9.6 Construction par construction

| Construction | Niveau 1 | Niveau 2 | Niveau 3 |
|---|---|---|---|
| Baliste | `weapon-ballista` sur `tower-square-bottom` (l'actuelle) | + étage, fanion (l'actuel) | `siege-ballista` du Castle Kit, plus grande, drapeau |
| Arbalète à répétition | une arbalète (*Crossbow*) sur `wood-structure` | deux arbalètes côte à côte | trois, sur une tour ronde, fanion |
| Mortier | `weapon-cannon` pointé vers le ciel, sur la terre | sur `tower-round-bottom`, boulets empilés | deux tubes (*Cannon* de Quaternius), sommet de tour |
| Tour de foudre | `tower-round-bottom` + `tower-round-crystals` | + étage, cristaux plus grands | + sommet, couronne de cristaux, arcs électriques permanents |
| Lance-flammes | `campfire_stones` et un pot | un chaudron (*Cauldron*) sur une base de pierre | gueule de chaudron sur `tower-square-top`, flammes permanentes |
| Catapulte | `weapon-catapult` du Tower Defense Kit | `siege-catapult` du Castle Kit | `siege-trebuchet` du Castle Kit |
| Soufflerie | petite roue à vent sur `wood-structure` | grandes ailes (moulin du KayKit Hexagon ou *Fan*) | ailes doubles sur `wood-structure-high`, fanion |
| Pendule | `wood-structure-high` + `weapon-ammo-boulder` au bout d'une chaîne | le bélier `siege-ram` du Castle Kit (un tronc qui se balance) | boulet hérissé (*Spike Mine*), cadre de pierre |
| Geyser | un trou entre des rochers plats (Nature Kit) | un anneau de rochers | une cheminée de `rock_tall*`, cristaux (`detail-crystal`) |
| Bulle | `pot_small` (Nature Kit) qui souffle des bulles | `pot_large` | un chaudron (*Cauldron*) ; les bulles : une sphère et un shader |
| Lance-filet | arbalète (*Crossbow*) et un filet | sur une base de pierre | double, harpon (*Shark Gun* ?) |
| Trappe | *Trap Door* en bois (Quaternius) | grille de métal (`floor_tile_grate_open`, KayKit) | grande grille 2 × 2 (`floor_tile_big_grate_open`), piques au fond |
| Tremplin | une planche sur un ressort (*Spring*) | deux ressorts, plaque de métal | grande plate-forme de lancement, runes |
| Tour-aimant | un aimant (*Magnet*) sur `wood-structure` | sur `tower-round-bottom`, plus grand | + sommet, cristaux, champ visible |
| Phare | une lanterne (*Lantern*) sur un mât | *Watch Tower* (Quaternius) avec la lanterne | un vrai phare (*Lighthouse*) ou les étages du Tower Defense Kit |
| Anneau élémentaire | un anneau (fait par le jeu, comme les runes des socles) | deux anneaux qui tournent en sens contraire | + cristaux flottants (`detail-crystal`) |
| Miroir | un petit miroir (*Mirror*) sur un pied en bois | grand, encadré | cadre de cristaux (`detail-crystal`) |
| Mine | *Landmine* enterrée (Quaternius) | la *Bomb* de Quaternius à moitié enterrée | un tas de barils explosifs (*Exploding Barrel*) |
| Goudron | une flaque (faite par le jeu) et un tonneau renversé (*Barrel*) | flaque plus grande, deux tonneaux | grande mare, tonneaux empilés (`barrel_small_stack`, KayKit) |
| Piques *(existent)* | les actuelles | plus hautes (les actuelles) | `floor_tile_big_spikes` (KayKit) ou *Hazard Spike Trap* |
| Barricade *(existe)* | planches (l'actuelle) | + planche et traverse (l'actuelle) | + pieux (l'actuelle) ; ou barrières de métal (`barrier*`, KayKit) |
| Tour de givre *(existe)* | `tower-round-crystals` (l'actuelle) | cristaux plus grands (l'actuelle) | idem ; + sommet de tour, givre au sol |
| Seau d'huile | un seau (`bucket_water` du KayKit Hexagon, ou `Bucket_Wooden_1`) sur un crochet, la cible `target` du KayKit à côté | un chaudron (*Cauldron*) sur une chaîne | grand chaudron sur un feu (`campfire_*`) |
| Baliste sur la tour | `siege-ballista` | + drapeau | + ferrures, cristaux |
| Esprit gardien | une petite lueur | *Ghost* (Quaternius) | *Ghost Skull*, plus grand, couronne de lueur |

### 9.7 Les fusions

24 fusions × 3 niveaux : trop pour un modèle chacune. **Proposé** : une fusion garde la base de la construction du dessous,
prend le haut (ou l'arme) de l'autre, et reçoit sa propre couleur ou son propre matériau (lave pour le Volcan, glace pour la
Fontaine gelée, foudre bleue pour la Baliste foudroyante…). Les niveaux suivent les règles de la partie 9.1. Une poignée de
fusions mérite un modèle à elle : le Volcan (une cheminée de lave), le Harpon (une baliste à chaîne), le Rayon (un phare et
son miroir reliés par un faisceau).

### 9.8 Téléchargés le 27 octobre (à la demande du joueur)

Dans `projet archery/Assets_TowerDefense/` (256 Mo), **hors du dépôt Git et hors du projet Unity** : on n'importera dans
`Assets/` que les modèles choisis, pour ne pas refaire grossir le projet (le ménage du 25 octobre avait retiré 740 Mo). À
l'import, ajouter chaque pack à `Credits_Licences.md` et au README. Les cinq packs sont en **CC0** (vérifié dans leur
fichier de licence).

| Dossier | Pack | Formats | Ce qui sert |
|---|---|---|---|
| `Kenney_Platformer_Kit` | Platformer Kit 4.1 (Kenney) | FBX, OBJ, glTF | `trap-spikes`, `trap-spikes-large`, `spike-block`, `spike-block-wide`, `saw`, `spring` (tremplin), `bomb`, `conveyor-belt`, `lever`, `button-round`, `barrel`, `crate-strong`, `fence-*`, `flag` |
| `KayKit_Dungeon_Remastered` | Dungeon Remastered 1.0 (Kay Lousberg) | OBJ, glTF (pas de FBX) | `spikes`, grilles `floor_tile_grate_open`, `floor_tile_big_grate_open`, `floor_tile_extralarge_grates_open` (trappe), `barrier*` (barricade), `keg`, `barrel_*`, bannières en 6 couleurs (couleurs de niveau), `torch_lit`, `pillar` |
| `KayKit_Medieval_Hexagon` | Medieval Hexagon Pack 1.0 (Kay Lousberg) | OBJ, glTF (pas de FBX) | `building_tower_A`, `_B`, `_base` (tours, sommet en 4 couleurs), `building_tower_catapult` (tourelle et bras à part), `projectile_catapult`, `building_windmill` (ailes à part : la soufflerie), `building_watermill` (roue), `building_well` (geyser), `bucket_water`, `bucket_empty` et `target` (le **seau d'huile** et sa cible), `bucket_arrows`, `building_stage_A` à `_C` et `building_scaffolding` (le chantier), `flag`, `weaponrack` |
| `Quaternius_Fantasy_Props_MegaKit` | Fantasy Props MegaKit, version gratuite (Quaternius) | FBX, OBJ, glTF | `Bucket_Metal`, `Bucket_Wooden_1` (seau d'huile), `Cauldron` (lance-flammes, bulle, seau niveau 3), `Chain_Coil` (pendule), `Rope_1` à `_3` (filet), `Barrel`, `Barrel_Holder` (goudron), `Banner_1`, `_2`, `Lantern_Wall` (phare), `Torch_Metal`, `Cage_Small`, `Anvil`, `Crate_Metal`, `Dummy` |
| `Quaternius_Medieval_Village_MegaKit` | Medieval Village MegaKit, version gratuite (Quaternius) | FBX, OBJ, glTF | toits (`Roof_Tower_RoundTiles`…), murs, barrières (`Prop_MetalFence_*`, `Prop_WoodenFence_*`), `Prop_Support`, `Prop_Wagon` : les étages et sommets des niveaux 2 et 3 |

- Unity importe le FBX et l'OBJ ; le glTF demanderait le paquet *glTFast*, absent du projet : pour les KayKit, prendre l'OBJ
  (avec sa texture `.png`, à poser sur un matériau à la main).
- **Téléchargés à la main par le joueur, le 27 octobre au soir, dans `Assets/construction`** (dans le projet Unity ; 142 modèles,
  tous valides, puis 51 et 8 Mo après l'allègement du pack RTS ; crédités dans `Credits_Licences.md` et le README). Poly Pizza demande une vérification
  anti-robot à chaque téléchargement, et Google Drive avait refusé le téléchargement automatique (« quota dépassé ») :
  - **Quaternius Ultimate Fantasy RTS** (CC0), dossier `Ultimate Fantasy RTS by Quaternius` : 128 FBX, les bâtiments en
    niveaux 1, 2 et 3 et en deux « âges » (`Archery_FirstAge_Level1` à `3`, `WatchTower_*_Level1` à `3`, `Barracks_*`,
    `Storage_*`, `Wall_*`, `WallTowers_*`, `Windmill_*`…). **Allégé le même soir, avant l'import dans Unity** (à la
    demande du joueur) : 37 FBX gardés (tours d'archerie et de guet, casernes, entrepôts, murs, tours de rempart, moulins,
    tours-maisons, tonneau), 91 retirés (fermes, maisons, marchés, temples, ports, centres-villes, merveilles, arbres,
    rochers, montagnes, caisses) ; `Storage_FirstAge_Leve3` renommé `Level3` (faute du pack) ;
  - CC0 : *Landmine*, *Exploding Barrel*, *Ghost*, *Ghost Skull*, *Watch Tower* et *Archery Towers* (Quaternius ; les deux
    tours sont les mêmes que `WatchTower_SecondAge_Level3` et `Archery_SecondAge_Level3` du pack RTS), *Mirror* (Isa
    Lousberg, MilkAndBanana), *Crossbow* (CreativeTrio : CC0, et non CC-BY comme écrit plus haut) ;
  - CC-BY 3.0 : *Giant Crossbow* (Aimé Tribolet, en OBJ ; accent retiré du nom du dossier), *Spike Mine* (Aaron Clifford,
    en OBJ), *Mirror* (Zsky), *Shark Gun* (theking1322), *Large Electric Generator* (miro_art_studio).
- **Manquent encore** (Poly Pizza) : *Bear Trap* ([1](https://poly.pizza/m/UTau9hyAnm),
  [2](https://poly.pizza/m/rYCSWZlgdW), [3](https://poly.pizza/m/fqv7qtGQYH)), *Trap Door*
  ([1](https://poly.pizza/m/PALqVBff9b), [2](https://poly.pizza/m/Ph6MpmwySS)), *Cannon*
  ([1](https://poly.pizza/m/J15vlPVvKK), [2](https://poly.pizza/m/qIAvoaawib)) en CC0 ; *Magnet*
  ([1](https://poly.pizza/m/dD2RIIea6WR), [2](https://poly.pizza/m/e6JzvzwGt7v), [3](https://poly.pizza/m/7dHLjQ8eZhu)),
  *Lighthouse* ([1](https://poly.pizza/m/0t2ZYRBsqX-), [2](https://poly.pizza/m/7H8is9jrGeB),
  [3](https://poly.pizza/m/bk72ChfpGo-)), *Fire Pit* ([1](https://poly.pizza/m/DsQslCOoz0),
  [3](https://poly.pizza/m/arYSisaHyX)) en CC-BY. Plus besoin (les packs les ont) : seaux, chaudrons, ressort, piques,
  torche, levier, lanterne, ventilateurs, les catapultes de Poly by Google.

## 10. À décider

1. Les **rares** dans les caisses : par lots ou seules ?
2. Le **coffre de boss** propose-t-il parfois une construction légendaire ?
3. Le **seau d'huile** : son temps de remplissage, son rayon, sa réaction avec l'ingénieur ; fusionne-t-il ?
4. La réaction du **miroir** avec l'ingénieur.
5. Le duo **Réseau** (fût conducteur + talisman de l'ingénieur) : proposé, la flèche reliée déclencherait la réaction de la
   construction la plus proche à chaque fin de délai.
6. La taille de la grille, et les rayons, dégâts et délais de chaque construction (à caler aux tests).

## 11. Ce qui doit suivre

- **Les performances du Quest** : plafond de 40, cibles cherchées quelques fois par seconde seulement, projectiles en
  réserve (pas créés et détruits), particules sobres.
- **L'arène de MK** : seules les constructions qui tirent y vont (6 au plus), chacune avec un comportement pour l'arène ;
  les pièges restent sur la carte.
- La cadence ×0,5 du Facile et la puissance des bâtiments par difficulté (*Structure Power*, GDD section 9).
- Le raccourci **F** (il remplit aujourd'hui les socles de tourelles niveau 2), le tutoriel « Construction » (il parle de
  cercles bleus et de matériaux), la montre (« Mat. »).
- **Le lore** : le tower defense n'existe que dans l'acte 1 ; THINK y étudie les décisions des joueurs (`Docs/Lore.md`).
- Le guide de montage `Guide_TowerDefense.md` (chiffres du 12 octobre) sera à refaire.
