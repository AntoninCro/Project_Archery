# Refonte du tower defense (brainstorm du 27 octobre)

Discussion de conception, puis suivi du travail. Les chiffres donnés ici sont des points de départ : les valeurs de la scène
et de `Data/*.asset` restent celles du joueur, et les textes des nouvelles cartes sont des propositions qu'il valide ou
réécrit. Le système actuel (matériaux, socles, 4 bâtiments) est décrit dans le GDD, section 9.

## Avancement

**27 octobre : le brainstorm est fait** (parties 1 à 8 : les décisions du joueur). Reste à décider : partie 10.

**29 octobre : deuxième brainstorm, fait** (partie 12) : après ses tests, le joueur trouvait que le tower defense
faisait « gadget » (le placement compte peu, on ne sent pas les bâtiments). Décidé : les quatre pistes A, B, C, D ;
10 tours à 5 niveaux aux évolutions uniques (des niveaux 5 très forts et très visibles, sur le modèle de la rampe de
lancement ; la catapulte et le pendule en niveaux 3), 13 duos, 5 trios et 4 ultimes (12.6, 12.9) ; une tour qui ne se
reconstruit plus (15 % de PV par vague), l'ennemi préparé, le phare en seul soutien, le placement lisible et les
menaces par voie. **Lot 1 codé** (la tour, les 10 tours), pas encore testé. **Lot 2 commencé** (12.10) : le système des
5 niveaux et le tremplin : la catapulte (N3, N4) validée au casque le 29 ; la rampe (N5) revue après l'essai (dix fois
plus haut, retombe sur un groupe) et validée : « c'est ce genre d'effet » pour tous les niveaux 5 ; les 9 autres tours
sont codées le soir même (12.10), pas encore testées. Le bouton « Plans » passe au-dessus du cadran de la montre (29
octobre, pas encore testé).

**30 octobre : l'allure des tours** (troisième retour du joueur, partie 12.11) : chaque tour a sa palette et ses pièces
revues, sans faces confondues d'un niveau à l'autre ; les ennemis ne restent plus plantés dans la rivière. Fait par
`Editor/TowerDefenseLookBaker`, d'après `Editor/TowerDefenseLook.json` ; les outils de `Tools/allure_tours` dessinent les tours
hors de Unity. Compilé, **pas encore vu dans Unity ni testé au casque**, rien de commité.

**30 octobre (suite) : le lot 3, l'ennemi préparé** (12.12) : un coup du joueur sur un ennemi qu'un bâtiment a mis en
l'air, gelé, cloué, pris au filet, étourdi, englué, attiré ou marqué fait plus de dégâts (de +15 % à +100 %
selon la difficulté à le préparer, 31 octobre) et rapporte 1,5 fois plus de points, avec un mot et un son pour chaque
sorte. Compilé, **pas encore testé**, rien de commité. Ensuite, au choix du
joueur : les lots 4 à 6 ou les duos (lot 7 ; 12.7).

**31 octobre : le lot 7 commencé, les duos** (12.13) : le joueur passe directement aux fusions. Le système (déplacer une tour
sur une tour partenaire, l'hologramme doré, chaque moitié qui monte avec ses plans) et 4 duos, chacun avec son modèle à lui
(le choix du joueur : un modèle par duo dès le début) : Canon, Mur de glace, Rempart à ressort, Aurore boréale. Compilé,
**pas encore vu dans Unity ni testé au casque**, rien de commité. Ensuite : les 9 autres duos, puis les trios et les ultimes.

**31 octobre (suite) : les duos changent de formule** (12.14). Le joueur : deux tours empilées qui gardent leurs deux
fonctions, plus un effet, « ce n'est pas plaisant à jouer ». **Décidé** : un duo devient **une tour nouvelle**, avec sa propre
attaque (elle peut reprendre l'idée des deux tours, ou non) et son modèle ; elle a **ses 3 niveaux à elle**, montés avec les
plans de l'une ou l'autre de ses deux tours ; elle naît au niveau 1 de deux tours au niveau 3, au niveau 2 si l'une est au
niveau 5, au niveau 3 si les deux le sont. Le joueur accepte aussi de changer les paires. Les 13 duos refaits sont proposés en
12.14, à trier ; le système de fusion de 12.13 (déplacer, hologramme doré, place, catalogue, baker) reste, les deux moitiés
partent. Le joueur les trouve « pas originaux du tout » et donne un exemple (barricade + tremplin : le boulet qui fait des tours
complets, rebondissant, et envoie les ennemis sur d'autres voies) : deuxième proposition en 12.15, chaque duo autour d'une idée
qu'on ne trouve dans aucune autre tour. Le joueur trie (12.15) et garde 13 duos ; le prix d'un niveau de duo est de 4 plans.
Codés le soir même : la fusion refaite pour une tour nouvelle, et le **Boulet fou** (12.16), pas encore vus dans Unity.

**Où on en est (28 octobre)** : le joueur a fait de premiers tests le soir ; ses retours (un bug, la pose à revoir, deux
modèles) sont traités juste en dessous, à tester. Étapes 2, 3 et 4 codées et compilées (`py Tools/compile_check.py` et
`compile_check_editor.py` sans erreur ni avertissement), **pas encore testées en entier dans Unity ni au casque** (tests :
`Checklist_Demo.md`, parties « Refonte du tower defense, étape 2 / 3 / 4 ») ; rien n'est commité. **Étape 5 : codée en entier** (6 lots, 23 bâtiments nouveaux : tout le catalogue, avec trois anneaux élémentaires). Claude avait conseillé de tester avant l'étape 5 ; le joueur a choisi d'enchaîner. Fichiers touchés
aussi par la discussion roguelike (petits ajouts seulement) : `Chests/Chest.cs`, `ChestReward.cs`, `ChestSpawner.cs`,
`Enemies/Enemy.cs`, `Economy/ScoreManager.cs`, `Core/DemoBoost.cs`, `Player/DesktopHand.cs`, `Upgrades/SpecialArrows.cs` ;
et `Building/Ballista.cs` (lot 2), que les pièces de la flèche (`ArrowPieces`) testent par `is Ballista` : l'arbalète à
répétition en hérite, elle compte donc comme une baliste pour elles et pour l'arène de MK.

**Premiers tests du joueur (28 octobre au soir) et ce qui a été refait** (codé et compilé, **pas encore testé** ; tests :
`Checklist_Demo.md`, « la pose revue après les premiers tests ») :
- **Bug : la baliste sur la tour ne tirait pas, de temps en temps, sur les ennemis collés à la tour.** Deux causes : elle ne
  pouvait pas viser à plus de 40° vers le bas, et sa vue était coupée par le bord de la plate-forme (que ses carreaux
  traversent pourtant : ce qui a des PV ne les arrête pas). `Ballista` vise maintenant jusqu'à 80° vers le bas sur la tour et
  en hauteur (réglage *Max Pitch Down Elevated*), et sa vue passe au travers de ce qui a des PV, comme ses carreaux.
- **La grille, visible et plus fine** (demandé par le joueur) : `BuildGrid` découpe la carte en cases de **1 m** (3 m avant :
  « certaines constructions dépassent sur les voies et inversement ») ; chaque bâtiment **occupe plusieurs cases**, d'après la
  taille de son modèle au niveau 3 (`Structure.ModelSize`, arrondi ; réglage *Footprint* de `StructureDefinition` pour le
  forcer ; la console donne le compte à la première pose) ; il se pose là où toutes ses cases lui conviennent (voie, bord,
  hauteur), sont libres et au même niveau (`BuildGrid.Evaluate`). Le sens : celui de la case arrondi au cran de 45°, puis les
  crans du joystick ; droit, le bâtiment est calé pour que ses cases tombent juste. **Les emplacements ne sont plus faits
  d'avance** (des milliers à 1 m) : `BuildGrid.Claim` crée celui d'un bâtiment à sa pose, sur ses cases, `Release` le rend
  quand il est déplacé. Seuls les 3 emplacements de la tour restent faits au lancement. La recherche se fait sur quelques
  millisecondes par image (coins des cases, puis cases ; réglage *Frame Budget*).
- **La grille se voit au sol** (`BuildGridOverlay`, shader `Archery/BuildGrid`, matériau `Resources/BuildGrid`) : pendant le
  menu et la pose, toutes les cases de sol à 16 m du joueur se teintent, bordées d'une ligne nette, **vertes là où le bâtiment
  se pose, rouges ailleurs** ; elles épousent le terrain et s'effacent vers le bord ; les cases du bâtiment visé brillent plus
  fort. Les emplacements de la tour s'y dessinent en grands carrés. **Les runes des emplacements sont retirées.**
- **Le menu des plans** (`BuildMenu`) : le bouton « Plans » de la montre ouvre devant le joueur un carrousel de cartes, une par
  bâtiment dont il a des plans (aperçu 3D qui tourne, nom, plans en poche, zone, bordure de la couleur de la rareté ; dessous,
  la carte du milieu décrite). Joystick de la main qui porte la montre : défiler sur la largeur ; gâchette : choisir ; poignée :
  fermer ; ou toucher une carte avec une manette (0,3 s). La grille montre déjà où va le bâtiment du milieu. Une fois choisi,
  l'hologramme comme avant (joystick : tourner, changer de plan ; gâchette : poser), mais **la poignée ramène au menu**. Sans
  casque : F2 ouvre le menu (molette, E), puis revient au menu, puis le ferme.
- **Modèles** : l'arbalète de CreativeTrio de l'arbalète à répétition et du lance-filet « ne va pas du tout dans la DA » :
  remplacée par la baliste du Tower Defense Kit, en petit (`TowerDefenseStep5Baker.SmallBallista`), avec un chargeur de
  carreaux sur le dos pour l'arbalète à répétition. Les deux prefabs existants sont repris une fois par le baker (il reste un
  objet « Arbalète » : il est remplacé ; la bouche est recalculée, les autres réglages ne bougent pas).
- Le raccourci **F** pose 6 balistes et tours de givre (pas les autres bâtiments du bord) sur la grille, avec leurs cases.
- Le **tutoriel** « Choisir un bâtiment » (section Construction) parle du menu des plans et de la grille au sol, plus des
  runes (`Editor/TowerDefenseStep2Baker`, une fois, seulement s'il a encore le texte de la veille ; texte proposé). Les
  vidéos de la section montrent encore l'ancienne pose : à refaire.
- **Deuxième retour (28 octobre au soir) : la grille « mal cadrée » sur la carte**, surtout sur la voie du milieu. Cause :
  une case était « sur la voie » si son centre était à moins de 3 m du chemin des ennemis (le chemin du NavMesh). Or la
  voie du milieu fait 8,5 m de large (entre les droites z = x + 6 et z = x − 6 : 13 cases de terre par rangée), et le
  chemin coupe les virages : il restait 1 à 2 cases de terre vertes « au bord » de chaque côté, et de l'herbe rouge à
  l'intérieur des virages. **La voie est maintenant lue dans le sol** : `BuildGrid` regarde le matériau du triangle touché
  sous le centre de chaque case (« dirt » : la terre des tuiles de chemin et des demi-cases en diagonale ; « grass » :
  l'herbe). Sur la terre, près du chemin (8 m, réglage *Lane Reach*) et à son niveau : sur la voie. Une case qui déborde
  sur la terre sans avoir son centre dessus (4 points près de ses coins) : ni voie ni bord, rouge pour tout. **Au bord** :
  à moins de 4,5 m d'une case de la voie (mesuré depuis les cases, plus depuis le chemin). Sur un sol sans terre ni herbe
  (un pont), l'ancienne règle des 3 m. Vérifié sur un plan de la carte reconstitué depuis la scène et les modèles (sans les
  arbres, l'eau ni les rochers) : la terre des trois voies est entièrement rouge, la bande verte la longe de chaque côté ;
  le long de la diagonale, la grille fait des marches d'une case (les bords de la voie passent par les coins des cases).
- **Troisième retour (28 octobre au soir)**, trois changements :
  - **Le menu flotte** : il restait collé au bord de la vue (il ne revenait que jusqu'à son angle de 40°). Il reste
    maintenant où il est ; si le joueur se tourne de plus de 30° ou s'éloigne, il attend 0,5 s puis glisse en douceur
    jusque devant lui (`BuildMenu`, réglages *Follow Angle*, *Follow Delay*, *Follow Time*). Il est un peu au-dessus du
    regard (6 cm ; 20 cm dessous avant), car il reste ouvert pendant la pose et ne doit pas cacher le sol visé.
  - **On construit sur la rivière** : son lit (les tuiles de chemin sous `Map > River`, 37,5 cm sous les berges) compte
    comme un sol plat à la hauteur de l'eau (20 cm sous les berges), constructible comme l'herbe : au bord de la voie, ou en
    hauteur sur les terrasses. Les passages sur les voies (`…_lane`, berges en terre) restent la voie. L'eau ne bloque
    plus rien. Trouvé en passant : la zone qui ralentit le gué de droite (`Ford Right`, x = 25,5) n'était plus sur la
    rivière, qui traverse la voie de droite en x = 19,5 (pièces `ground_pathStraight_lane`) : les ennemis étaient ralentis
    à sec, 6 m après l'eau. Recalée avec l'accord du joueur par `Editor/RiverFordBaker` (une fois, seulement si elle est
    encore à x = 25,5 ; centrée sur les pièces de passage, même taille et même hauteur ; enregistrer la scène).
  - **Plus de gâchette pour choisir** : le bouton « Plans » ouvre le menu **et** sort l'hologramme de la carte du milieu
    (le premier bâtiment, puis le dernier choisi). Le menu reste ouvert pendant la pose : joystick gauche et droite :
    changer de bâtiment (l'hologramme suit) ; haut et bas : tourner ; gâchette : poser (une seule fois) ; poignée :
    fermer. Toucher une carte la sélectionne aussi. Un déplacement n'a pas de menu : tout le joystick tourne. Sans
    casque : F2 ouvre (molette : changer, Maj + molette : tourner, E : poser), F2 ferme. Le tutoriel suit
    (`TowerDefenseStep2Baker`, texte proposé).
- **L'ancien système sort de la scène** (demandé par le joueur, 28 octobre au soir) : les 12 socles (`Build Sockets`) et
  les 3 barricades de départ n'étaient que désactivés au lancement ; leurs cercles bleus (les runes de `MapGameplayLights`,
  enregistrées dans la scène sous `Jeu > Runes`) restaient allumés : au lancement, une rune se rattache au socle le plus
  proche, et les socles étaient déjà désactivés. `Editor/OldBuildSystemCleanupBaker` supprime de la scène les socles sans
  bâtiment, leurs runes, le groupe `Build Sockets` et les barricades qui ne sont pas un bâtiment (avec l'annulation
  d'Unity ; enregistrer la scène). Rien d'autre ne les référençait. Une rune sans emplacement est maintenant cachée au
  lancement. Les emplacements de la grille gardent les réglages du premier socle, qui sont ceux du prefab. (Le baker a
  tourné le 28 au soir ; le 29 au matin, la scène n'était pas encore enregistrée.)
- **Quatrième retour (29 octobre) : la barricade ne se posait pas sur certaines voies.** Elle occupait 6 × 2 cases
  (modèle de 6,0 × 1,8 m : son prefab, `Prefabs/Structures/Barricade`, est un modèle de 4 m étiré ×1,5 en X), alors que les
  voies de gauche et de droite n'ont que 4 ou 5 cases de terre de large (à peu près moitié-moitié sur leur longueur ; la
  voie du milieu, en diagonale, en a plus). `Editor/TowerDefenseTuningBaker` la ramène à ×1 : 4,0 m, **4 × 2 cases**,
  qui tiennent sur toutes les voies (une fois, seulement si l'échelle est encore 1,5). Elle arrête toujours les ennemis de
  toute la voie : c'est une zone de 5 m de rayon devant elle, pas sa largeur (`Barricade.FindBlocking`). Vu dans le même
  journal : le **Phare** mesurait 12 m de large (son faisceau, deux rayons de 6 m au niveau 3, comptait dans sa taille),
  donc 6 × 3 cases. Corrigé avec l'accord du joueur : `StructureLighthouse.IsBlueprintPart` laisse le faisceau hors du
  bâtiment (comme le jet du lance-flammes ou la colonne du geyser) : il ne compte plus dans les cases (environ 3 × 3), ni
  dans le volume frappé au marteau, ni dans l'hologramme et l'aperçu du menu (où la tour devenait minuscule) ; il est
  caché tant que le phare n'est pas construit, puis s'allume et tourne.

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
4. [x] L'état « projeté » des ennemis (partie 6.2), la base de toutes les constructions physiques.
   **Codée le 28 octobre, pas encore testée** (aucune construction ne s'en sert encore : raccourci **F3** pour essayer ;
   tests : `Checklist_Demo.md`) : `Enemies/Enemy.Launch.cs` (la classe `Enemy` devient partielle, deux appels dans son
   `Update`). `Enemy.Launch(vitesse, source)` coupe le NavMeshAgent, l'ennemi suit une cloche calculée (gravité du jeu),
   ses pieds balayés par une sphère de 35 cm ; `Enemy.Landed` prévient quand il retombe (pour les fusions), `IsAirborne`,
   `IsStunned`, `CanBeLaunched`, `Stun(durée)`. Les chiffres de la partie 6.2 sont des constantes en tête du fichier. Le tir
   au vol : `ScoreManager`, *Air Shot Multiplier* ×1,5 et « Au vol ! » au-dessus de l'ennemi.
5. [x] Les constructions nouvelles, par lots (6 lots, codés le 28 octobre, pas encore testés).
   **Lot 1 codé le 28 octobre, pas encore testé** (tests : `Checklist_Demo.md`) :
   - **Raretés** : `StructureDefinition.rarity` (commune, rare, légendaire) ; la caisse tire pour chaque orbe une rareté
     (60 / 30 / 10 %, parmi celles qui ont encore un bâtiment), puis un bâtiment de cette rareté (`ChestRewards.RollPlans`) ;
     une légendaire vient toujours seule. Les 4 bâtiments d'avant sont communs.
   - **Mine** (`StructureMine`, voie, commune, lots de 3) : explose sous le premier ennemi au sol (40 / 60 / 90 de dégâts à
     3 / 3,5 / 4 m, l'explosion de la flèche explosive ; au niveau 3, les ennemis sont projetés), se réarme à chaque vague.
     Modèles : la *Landmine* de Quaternius (cachée une fois explosée), deux de plus au niveau 2, un baril explosif au niveau 3.
   - **Goudron** (`StructureTar`, voie, commune, lots de 2) : une flaque faite par le jeu qui ralentit de 40 / 50 / 60 % et
     grandit (1,4 / 1,7 / 2 m) ; des tonneaux du pack RTS (un, deux, trois).
   - **Tremplin** (`StructureTrampoline`, voie, rare) : projette en l'air, vers la forêt, un ennemi au sol qui passe dessus
     (8 / 9,5 / 11 m/s, recharge 3 / 2,2 / 1,5 s ; `Enemy.Launch`). En formes simples : cadre, ressorts, planche qui rebondit
     avec une flèche vers la forêt ; plaque de métal au niveau 2, liseré lumineux au niveau 3.
   - **Tour de foudre** (`StructureLightningTower`, bord ou hauteur, rare) : un éclair saute entre 3 ennemis à 14 / 16 / 18 m,
     volants compris (9 / 13 / 19 de dégâts, toutes les 2,6 / 2,1 / 1,6 s ; l'éclair de la flèche de foudre). Les tours rondes
     du Tower Defense Kit, empilées (base, étage, sommet), couronnées de cristaux.
   - **Le baker** `Editor/TowerDefenseStep5Baker` (à l'ouverture de Unity) crée ce qui manque : 4 matériaux
     (`Art/TowerDefense`), les 4 prefabs (`Prefabs/Structures` : Mine, Goudron, Tremplin, Foudre), leurs réglages
     (`Data/Structures`, textes des niveaux proposés) et les ajoute au Build Manager de la scène (**Ctrl+S** ensuite). Il ne
     refait ni ne retouche rien de ce qui existe ; un bâtiment retiré à la main de la liste du Build Manager y serait remis.
   - Le choix d'un bâtiment se fait à la montre (haut et bas du joystick) : plus besoin d'une carte par bâtiment au panneau.
   - Les chiffres sont des réglages des prefabs, à caler aux tests ; les réactions avec l'ingénieur viennent à l'étape 8.

   **Lot 2 codé le 28 octobre, pas encore testé** :
   - **Arbalète à répétition** (`StructureRepeater`, bord, commune) : une petite baliste (elle en hérite : visée, carreaux,
     cadence de la difficulté, moitié des bonus de l'arc) qui tire des rafales de 3 / 4 / 5 carreaux (0,12 s d'écart, 1,5°
     de dispersion) à 18 m, toutes les 2,2 / 1,9 / 1,6 s ; 3 / 4 / 5,5 de dégâts par carreau. Elle vise d'abord le volant
     le plus proche, et le blesse ×1,5. Modèle : une arbalète (*Crossbow* de CreativeTrio) sur `wood-structure` ; une
     deuxième au niveau 2, une troisième et un fanion au niveau 3. **À vérifier** : le sens du modèle n'était pas connu, le
     baker l'a couché le long de sa plus grande longueur ; l'arbalète doit pointer le long de l'axe bleu (Z) de
     « Tourelle », sinon tourner l'objet « Arbalète » (et la bouche, *Muzzle Offset*, est à replacer au bout).
   - **Mortier** (`StructureMortar`, bord ou hauteur, rare) : tire en cloche sur le groupe d'ennemis au sol le plus serré
     (puis le plus proche), entre 6 m et 24 / 28 / 32 m, toutes les 4,5 / 3,8 / 3,2 s, en visant là où la cible sera à
     l'arrivée (vol de 1,4 à 2,4 s selon la distance). L'explosion (celle de la flèche explosive) fait 30 / 42 / 60 de
     dégâts au centre, moitié au bord, à 2,5 / 3 / 3,5 m. Un obus qui heurte une falaise explose là. Les volants passent
     au-dessus. Niveau 3 : deux tubes, deux obus par salve (sur les deux meilleures cibles, ou la même un peu à côté).
     Modèle : le `weapon-cannon` du Tower Defense Kit levé de 40°, sur `tower-round-base` ; des boulets empilés au
     niveau 2 ; un deuxième tube au niveau 3 ; les obus sont des `weapon-ammo-cannonball`. Pas encore de son de tir
     (réglage *Fire Clip*).
   - Les deux constructions sont créées par le même baker de l'étape 5 (prefabs et réglages `Arbalete` et `Mortier`). Pour
     l'arbalète, `Ballista` s'ouvre (méthodes `protected virtual` : `FindTarget`, `Fire`, `ShotDamage`) sans que la baliste
     change ; `Enemy` gagne `IsFlyer` et `Velocity`.

   **Lot 3 codé le 28 octobre, pas encore testé** : quatre constructions physiques. Leurs raretés étaient « à fixer » :
   celles-ci ont été **proposées par Claude et acceptées par le joueur** le 28 octobre (réglage *Rarity* de chaque
   `Data/Structures`).
   - **Geyser** (`StructureGeyser`, voie, commune) : chargé, il jaillit dès qu'un ennemi au sol passe dessus et projette à
     la verticale **tous** ceux qui sont dessus (1,5 / 1,75 / 2 m ; 9 / 10,5 / 12 m/s vers le haut), puis se recharge
     (4 / 3,4 / 2,8 s). Le catalogue disait « à intervalles » : il attend un ennemi, sinon il jaillirait surtout dans le
     vide. Une colonne d'eau (cylindre au matériau `Light_Beam`, teinté en bleu) monte et retombe. Modèles : un trou sombre
     entre trois `detail-rocks` du Tower Defense Kit ; des `detail-crystal` au niveau 2 ; un gros rocher et un gros
     cristal au niveau 3.
   - **Soufflerie** (`StructureBlower`, bord, commune) : par bouffées de 2,5 / 3 / 3,5 s (2,5 s de pause), un cône de vent
     (30° de chaque côté, 9 / 10,5 / 12 m) repousse vers la forêt les ennemis qui s'y trouvent, volants compris, à
     3 / 3,5 / 4 m/s (moitié au bout du cône, moitié pour un boss) : assez pour les arrêter ou les faire reculer. Elle
     passe par `Enemy.Push`. Modèle : une charpente `wood-structure`, une nacelle et 4 pales (formes simples) qui tournent
     vite pendant une bouffée ; 4 pales de plus au niveau 2 ; un cadre de métal et un fanion au niveau 3.
   - **Tour-aimant** (`StructureMagnet`, bord, rare) : toutes les 3,5 / 3 / 2,5 s, attire vers elle de 3 / 3,5 / 4 m les
     ennemis à 8 / 9 / 10 m, volants compris (moitié pour un boss ; jamais à moins de 1,5 m d'elle) ; un éclair violet
     relie la tour à chacun (6 au plus). Modèle : `tower-round-bottom-b` couronnée d'un aimant en U (formes simples) ; des
     bobines de cuivre au niveau 2 ; des cristaux et une lueur entre les pôles au niveau 3.
   - **Catapulte** (`StructureCatapult`, bord, rare) : toutes les 5 / 4,2 / 3,5 s, attrape l'ennemi au sol le plus proche
     (6 / 6,5 / 7 m) et le lance (vol de 1,3 s, `Enemy.Launch`) sur le groupe le plus serré à 12 / 14 / 16 m (au moins
     2 ennemis à 2,5 m l'un de l'autre, là où ils seront) ; sans groupe, elle le renvoie de 6 / 7 / 8 m vers la forêt. À
     l'atterrissage, le boulet fait 15 / 22 / 32 de dégâts à ceux qui sont à 2,5 m, et la moitié à lui-même (en plus de sa
     chute et des quilles). **Pour l'instant, l'ennemi s'envole de sa place**, sans passer par la cuillère : à revoir aux
     tests. Modèle : `weapon-catapult` (qui bascule au lancer) ; des rochers `weapon-ammo-boulder` au niveau 2 ; plus de
     rochers et un fanion au niveau 3.
   - Nouveau : `Structure.TowardForest(enemy)` (vers la forêt : à l'inverse de sa marche, ou à l'opposé de la tour du
     joueur) ; `Enemy.IsBoss` devient public. Les quatre sont créées par le baker de l'étape 5 (`Geyser`, `Soufflerie`,
     `Aimant`, `Catapulte`).

   **Lot 4 codé le 28 octobre, pas encore testé** : les quatre autres constructions physiques (raretés proposées par
   Claude, à confirmer par le joueur).
   - **Trappe** (`StructureTrapdoor`, voie, rare) : chargée, elle s'ouvre dès qu'un ennemi au sol passe dessus ; tous ceux
     qui sont dessus (1,5 / 1,75 / 2 m) ressortent 8 / 10 / 12 m plus loin en arrière **sur la voie la plus proche**
     (`BuildGrid.TryPointBack`, vers l'apparition des ennemis), étourdis 1 s ; recharge 6 / 5 / 4 s. Ils sont renvoyés
     d'un coup (`Enemy.Teleport`), sans animation de chute. En formes simples : un cadre de bois sur un trou noir, deux
     battants qui s'ouvrent vers le bas ; des ferrures au niveau 2 ; un cadre de métal au niveau 3.
   - **Pendule** (`StructurePendulum`, voie, rare) : un portique de 4,4 m d'où pend un boulet qui se balance en travers de
     la voie (65° de chaque côté, un aller-retour en 3 / 2,6 / 2,2 s). Il fauche les ennemis au sol qu'il croise :
     20 / 30 / 45 de dégâts, et ils sont projetés sur le côté (5 / 6 / 7 m/s, plus 4 vers le haut) ; pas deux fois le même
     en 0,8 s. **Il tient sur un seul emplacement** (le catalogue disait 2 × 2) : le boulet balaie au-delà. Modèles :
     portique et chaîne en formes simples, `weapon-ammo-boulder` ; cerclages de métal au niveau 2 ; le *Spike Mine*
     (Aaron Clifford, CC-BY) en boulet hérissé au niveau 3.
   - **Bulle** (`StructureBubble`, bord, rare) : toutes les 7 / 6 / 5 s, enferme l'ennemi au sol le plus proche
     (10 / 11 / 12 m) dans une bulle qui monte à 1,5 m/s jusqu'à 4 / 5 / 6 m et flotte (`Enemy.Float` : il compte comme en
     l'air, le tir au vol rapporte plus). **N'importe quel coup la crève** (une flèche, une tourelle), sinon elle crève
     seule au bout de 5 / 6 / 7 s : il retombe, et la règle de la chute le blesse. Deux bulles à la fois au niveau 3. La
     bulle est une sphère sans collider (matériau transparent « Bulle ») : les flèches la traversent et touchent l'ennemi.
     Modèle : un chaudron de fonte au liquide bleu lumineux, une bulle au-dessus (formes simples) ; des rochers au niveau 2 ;
     une deuxième bulle et un gros cristal au niveau 3.
   - **Lance-filet** (`StructureNetLauncher`, bord, commune) : comme l'arbalète, il hérite de la baliste (visée, cadence ;
     `Ballista.Shoot` devient `virtual`), mais ne vise que les volants, à 16 m, toutes les 5 / 4,2 / 3,5 s. Le filet vole
     jusqu'au volant (toujours touché) : celui-ci tombe au sol, blessé par sa chute, et y reste étourdi 3 / 4 / 5 s
     (`Enemy.Net`), à portée de flèche et de katana, puis il repart. Deux filets par tir au niveau 3. Modèle : une
     arbalète (*Crossbow*) et un filet roulé sur `tower-square-bottom-a` ; un deuxième filet au niveau 2 ; une deuxième
     arbalète et un fanion au niveau 3. Même vérification que l'arbalète : le sens du modèle.
   - Nouveau sur l'ennemi (`Enemy.Launch.cs`) : `Float` / `Pop` / `IsFloating` (la bulle), `Net` / `IsNetted` (le filet),
     `Teleport` (la trappe). Les quatre sont créées par le baker de l'étape 5 (`Trappe`, `Pendule`, `Bulle`, `Filet`).
   - **Les physiques sont toutes faites.** Raretés du lot 4 acceptées par le joueur le 28 octobre.

   **Lot 5 codé le 28 octobre, pas encore testé** : le soutien (raretés du catalogue : toutes rares).
   - **Lance-flammes** (`StructureFlamethrower`, bord) : quand des ennemis sont dans son cône (25° de chaque côté,
     6 / 7 / 8 m, volants compris), il crache un jet de feu (le souffle du dragon, `FireEffects.CreateBreath`, 0,8 s) et
     chacun brûle : 12 / 18 / 27 de dégâts sur 3 s (`Burning`) ; toutes les 3 / 2,6 / 2,2 s. **Le brasero n'est plus dans
     la scène** : ses flammes non plus. Celles des ennemis qui brûlent (et des flèches enflammées) sont faites par le code,
     avec le matériau de feu des flèches explosives (`Building/BurnFlames`). Modèle : un tonneau du pack RTS en
     réservoir, tuyau et buse de métal ; un deuxième tonneau au niveau 2 ; une bague lumineuse et un fanion au niveau 3.
   - **Phare** (`StructureLighthouse`, hauteur) : dans son rayon (12 / 14 / 16 m, à plat), les points faibles des ennemis
     brillent (une petite sphère lumineuse). **Seuls les boss ont des points faibles** (zone `WeakPoint`) : pour les
     autres, c'est la tête. Une flèche tirée qui touche une zone qui brille fait 20 / 35 / 50 % de dégâts en plus (des
     dégâts du phare, par `Arrow.AnyHit`). Modèle : la *Watch Tower* de Quaternius, une lanterne et un faisceau qui tourne
     (le matériau `Light_Beam`) ; un fanion au niveau 2 ; un deuxième faisceau au niveau 3.
   - **Miroir** (`StructureMirror`, bord) : il se tourne vers la tour du joueur. Une flèche tirée qui touche sa glace
     repart vers l'ennemi le plus proche du miroir (25 / 30 / 35 m ; en visant devant lui, et plus haut pour la chute),
     avec ×1 / ×1,25 / ×1,5 de dégâts et, au niveau 3, un ennemi de plus traversé ; sans ennemi, elle rebondit comme sur
     un miroir (`Arrow.Deflect`). **Le seul bâtiment avec un collider** : la glace. Modèle : le *Mirror* de
     MilkAndBanana sur un pied de bois ; un cadre au niveau 2 ; des cristaux au niveau 3.
   - **Anneaux élémentaires** (`StructureRing`, partout : voie, bord ou hauteur ; nouveau réglage
     `StructureDefinition.anywhere`) : « un anneau par effet », donc **trois bâtiments** : l'**anneau de feu** (la flèche
     s'enflamme, sa cible brûle de 30 / 40 / 50 % de ses dégâts sur 3 s), l'**anneau de foudre** (à l'impact, un éclair
     saute vers 2 / 3 / 4 ennemis proches, la moitié des dégâts de la flèche chacun) et l'**anneau double** (une flèche en
     plus part à côté, 60 / 80 / 80 % des dégâts ; deux au niveau 3 ; par `SpecialArrows.LaunchExtraShot`, elles ne
     cassent pas le combo). Aux niveaux 2 et 3, la flèche qui passe fait aussi 15 / 30 % de dégâts en plus. L'anneau se
     tourne vers la tête du joueur ; il suit les flèches du pool d'une image à l'autre et regarde si leur pointe traverse
     son disque (0,8 m de rayon). Modèle : un anneau lumineux (un tore fait par le baker, `Art/TowerDefense/Anneau`) sur
     un pied ; un deuxième anneau qui tourne en sens contraire au niveau 2 ; des cristaux au niveau 3. **Trois bâtiments :
     choix du joueur, le 28 octobre.**

   **Lot 6 codé le 28 octobre, pas encore testé** : sur la tour, et les deux légendaires. **Tout le catalogue est codé.**
   - **Les emplacements sur la tour** (`BuildGrid.CreateTowerSockets`) : au lancement, la grille cherche le sol de la
     plate-forme sous la tête du joueur et y pose jusqu'à 3 petits emplacements (1,2 m) à 1,2 à 1,9 m de lui, sur les côtés
     et derrière (pas devant : il y vise), tournés vers l'extérieur, seulement sur un sol plat à la hauteur de ses pieds
     (réglages *Tower Slots*, *Tower Slot Size*, *Tower Slot Radii*). La pose les trouve avant la grille
     (`BuildGrid.SocketAt`) ; leurs runes sont dorées. Si le joueur n'est pas sur la tour au lancement, il n'y en a pas
     (un avertissement dans la console).
   - **Seau d'huile** (`StructureOilBucket`, sur la tour, rare proposée ; idée du joueur) : toucher sa cible (une flèche
     tirée, ou tenue en main : la cible est à portée de main) renverse le seau ; l'huile bouillante fait 40 / 60 / 90 de
     dégâts à tous les ennemis au sol à 8 / 9 / 10 m du pied de la tour et les ralentit (40 %, 3 s) ; il se remplit en
     20 / 16 / 12 s. La cible est dorée quand il est plein, grise sinon. En formes simples : un seau de bois sur un axe,
     qui bascule vers l'extérieur, un filet d'huile jusqu'au pied de la tour, la cible sur un poteau ; des cerclages au
     niveau 2 ; un fanion au niveau 3.
   - **Baliste sur la tour** (légendaire) : une `Ballista` réglée plus fort (pas de script à elle) : 35 m, 30 / 45 / 66 de
     dégâts, toutes les 2,5 / 2,1 / 1,7 s, le carreau traverse 1 / 2 / 3 ennemis. La baliste du Tower Defense Kit, plus
     grosse, sur un socle rond bas (pour ne pas boucher la vue) ; des plaques au niveau 2 ; un fanion au niveau 3.
   - **Esprit gardien** (`StructureSpirit`, partout, légendaire) : le fantôme de Quaternius flotte au-dessus d'un autel ;
     il chasse l'ennemi à 25 / 30 / 35 m qui a le moins de voisins (à 6 m), volants compris, fond sur lui (7 m/s) et le
     frappe (15 / 22 / 32 de dégâts, toutes les 1,2 / 1 / 0,8 s, un court éclair vert pâle), puis passe au suivant ; sans
     proie, il revient. Des cristaux au niveau 2 ; une couronne de cristaux et une lueur au niveau 3.
   - Avec les légendaires, le tirage des caisses peut enfin donner sa part de 10 % (une légendaire vient toujours seule).
   - **La suite** : l'étape 6 (les fusions). Avant, il faudrait tester les étapes 2 à 5 dans Unity et au casque.
     Les modèles de code : `SpikeTrap` et les nouveaux pour une zone, `Ballista` et `StructureLightningTower` pour une
     tourelle, `StructureTrampoline` et `Enemy.Launch` pour la physique ; le baker de l'étape 5 pour les prefabs.
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
| Catapulte | bord | attrape un ennemi et le lance sur un groupe : il devient un boulet, qui blesse les deux | en lance 3 d'un coup | rare |
| Soufflerie | bord | un cône de vent repousse les ennemis vers la forêt et dévie les volants | une rafale qui projette tout le cône 8 m en arrière | commune |
| Pendule | au-dessus de la voie (2 × 2) | un boulet se balance en travers de la voie et fauche les ennemis | un balancement deux fois plus fort | rare |
| Geyser | voie | jaillit à intervalles et projette à la verticale ce qui est dessus | jaillit tout de suite, deux fois plus haut | commune |
| Bulle | bord | enferme un ennemi dans une bulle qui monte doucement ; crevée, elle le laisse tomber | 3 bulles d'un coup | rare |
| Lance-filet | bord | attrape un volant et le fait tomber au sol, étourdi | un filet large : tous les volants à portée | commune |
| Trappe | voie | s'ouvre sous un groupe : les ennemis ressortent plus loin en arrière | s'ouvre tout de suite | rare |
| Tour-aimant | bord | attire les ennemis vers elle par impulsions (l'attraction du fût aimant) | une grosse impulsion qui attire tout à 10 m | rare |
| Seau d'huile *(idée du joueur)* | la tour | une cible à côté du seau : la toucher renverse l'huile, qui blesse tous les ennemis autour de la tour ; il se remplit ensuite | à proposer (partie 10) | rare (proposée) |
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
- **L'atterrissage** joue avec la carte (chiffres de l'étape 4, 28 octobre, à régler aux tests) :
  - une **chute** de plus de 1,5 m fait 5 % de ses PV max par mètre au-delà (la moitié au plus) ;
  - **percuter un autre ennemi**, en vol ou en retombant, lui fait 8 % de ses PV max (4 % au projeté) et le renverse
    (un petit saut) : les quilles ;
  - **contre une falaise ou un mur** : il rebondit un peu, tombe, et reste étourdi 1 s de plus ;
  - il se relève toujours **étourdi 0,5 s** (sans bouger ni attaquer) ;
  - **dans la rivière** : pas de dégâts de chute, ralenti de moitié 2 s. *Pas encore* emporté par le courant : la
    rivière ne donne pas facilement le sens de son courant en un point (`RiverWater`).
  - retombé hors des voies (la jungle, une terrasse), il revient au plus près du NavMesh (15 m au plus) ; au-delà, il est
    perdu (il meurt).
- **Les boss** ne sont jamais projetés et sont poussés moitié moins. **Les volants** (et les mannequins) ne sont pas
  projetés : ils ne craindront que le vent et le filet.
- **Le tir au vol** : un coup du joueur (flèche, katana, faille) sur un ennemi en l'air rapporte ×1,5 points, comme au
  ball-trap ; « Au vol ! » s'affiche au-dessus de lui.

## 7. Les fusions (décidées le 27 octobre)

> **Remplacée le 29 octobre** par la partie 12.9 (les fusions des 10 tours, avec des trios et des ultimes). Gardée pour
> l'historique.

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

## 12. Deuxième brainstorm : que le tower defense compte (29 octobre)

### 12.1 Le constat

Le joueur, après ses tests : poser partout lui plaît, mais le tower defense fait « gadget », pas partie intégrante du jeu ;
le placement des tourelles a peu d'utilité, peu d'entre elles servent vraiment, ou on ne le ressent pas. (Les modèles
encore moches et trop semblables : un autre problème, plus tard.) Les causes relevées par Claude :

1. **Les bâtiments pèsent peu face au joueur.** Une baliste niveau 1 fait 10 dégâts toutes les 3 s : environ un tir de
   joueur toutes les 5 s (une flèche « très bonne » de l'arc de chasse fait environ 16). Leurs dégâts suivent les PV des
   ennemis (vague, difficulté), mais pas la montée du joueur par la refonte roguelike : au milieu de partie, il fait tout.
2. **Le placement ne pose pas de vrai choix** : les trois voies mènent à la tour, avec à peu près autant d'ennemis, qui
   suivent toujours le même chemin. La meilleure place est presque toujours la même (au bord, près de la tour), là où le
   joueur tue déjà tout ; plus loin, un bâtiment travaille hors de sa vue.
3. **On ne voit pas ce qu'ils font** : ni éliminations comptées, ni bilan, des effets discrets.
4. **Laisser passer coûte peu** : la tour tombée divise la récompense de la vague par deux et coupe le téléporteur ; la
   partie se joue sur les PV du joueur.
5. **Trop de bâtiments aux rôles proches** (27 en comptant les trois anneaux), tirés au hasard dans les caisses : on ne
   construit pas une défense.

### 12.2 Décidé (29 octobre)

- **Les quatre pistes ensemble** :
  - **A. Les bâtiments préparent les tirs du joueur** : moins de dégâts directs, plus de contrôle (projeter, geler,
    regrouper, marquer, ralentir) ; toucher un ennemi « préparé » rapporte plus ;
  - **B. Ils tiennent le front quand le joueur part** sur la carte (coffres, caisses, objectifs) ;
  - **C. Chaque voie, une menace différente**, connue d'avance : le choix du bâtiment et de sa voie y répond ;
  - **D. Moins de bâtiments, plus marqués.**
- **Une quinzaine de constructions** (le joueur : plutôt 15 que la dizaine proposée). Dans la liste proposée (12.3),
  **la bulle reste** : 16 constructions.
- **L'enjeu de la tour** (le joueur, 29 octobre ; remplace la réparation complète du 25 octobre, GDD section 9) : une
  tour **détruite n'est plus reconstruite** à la fin de la vague ; une tour abîmée ne regagne que **15 % de ses PV max**.
  Conséquences des règles actuelles : tour détruite, l'or gagné reste divisé par deux, le téléporteur ne marche plus et le
  gong reste au sol. **Elle ne se reconstruit jamais** (le joueur, même jour) : détruite, elle l'est jusqu'à la fin de la
  partie. **Les barricades, elles, se réparent toujours en entier** à la fin de chaque vague (le joueur, même jour).
- **Continuer le brainstorm en s'inspirant de bons tower defense**, Bloons TD6 en tête (le joueur l'aime bien) : 12.5.
- **Retenu de 12.5** : les **bâtiments de soutien** (3) et le **placement lisible** (4). **Pas retenu** : les propriétés
  d'ennemis, les deux branches, le compteur et la priorité de cible, le joueur héros, les plans en boutique, les combos de
  pièges.
- **Tirer sur un bâtiment ne le déclenche pas pour tous** : la réaction reste réservée au talisman de l'ingénieur, comme
  décidé le 27 octobre (partie 5).
- **Les tendances des voies, annoncées pendant la pause** : oui (piste C, 12.3).
- **Les bâtiments de soutien : le phare et un atelier de l'ingénieur** (17 constructions) : le phare renforce les dégâts
  des bâtiments de son rayon, l'atelier leur cadence (détail en 12.3).
- **L'ennemi préparé est gardé** (+50 % de dégâts des flèches, ×1,5 points ; 12.3). Revu le 31 octobre : les dégâts selon la
  difficulté à le préparer, de +15 % à +100 % (12.12).

### 12.3 Proposé par Claude (à valider)

**Les 15 constructions** (devenues 17 avec la bulle et l'atelier, puis **remplacées par les 12 tours de 12.6**), chacune un rôle net. Les « gardiens » (piste B) font de vrais dégâts tout seuls ; les
« préparateurs » (piste A) font peu de dégâts mais préparent les tirs du joueur.

| Construction | Où | Rôle | Piste |
|---|---|---|---|
| Barricade | voie | bloque | B |
| Piques | voie | dégâts continus sur la voie | B |
| Mine | voie | piège ; une flèche la fait exploser tout de suite | A |
| Goudron | voie | ralentit ; une flèche l'enflamme | A |
| Tremplin | voie | projette : tir au vol | A |
| Baliste | bord, hauteur | la sentinelle : dégâts sûrs, tient le front | B |
| Mortier | bord, hauteur | zone contre les essaims, portée en hauteur | B, C |
| Lance-filet | bord | fait tomber les volants, étourdis | A, C |
| Tour de givre | bord | ralentit, gèle : un gelé touché éclate | A |
| Tour-aimant | bord | regroupe : une flèche de zone en touche plus | A |
| Bulle *(gardée par le joueur)* | bord | enferme un ennemi qui monte doucement : une cible offerte ; crevée, il retombe | A |
| Phare | hauteur | fait briller les points faibles ; **soutien** : les bâtiments de son rayon (12 / 14 / 16 m) font +20 / 30 / 40 % de dégâts (ils visent eux aussi les points faibles) | A, soutien |
| Atelier de l'ingénieur *(nouveau, 29 octobre)* | bord | **soutien**, rare : les bâtiments à moins de 8 m tirent 15 / 25 / 35 % plus vite | soutien |
| Anneau élémentaire | partout | **un seul anneau** : feu au niveau 1, + foudre au niveau 2, + flèche double au niveau 3 | A |
| Seau d'huile | la tour | la dernière défense de la tour (idée du joueur) | B |
| Baliste sur la tour | la tour | légendaire | B |
| Esprit gardien | partout | légendaire, chasse les isolés | B |

- **Écartées** (11) : arbalète à répétition (doublon de la baliste et du filet), tour de foudre (doublon du mortier),
  lance-flammes (le feu passe par le goudron), miroir, catapulte, soufflerie, pendule, geyser, trappe, et les anneaux de
  foudre et double (fondus dans un seul). La bulle, d'abord écartée, est gardée par le joueur. **Retirées des caisses seulement** : leur code et leurs prefabs restent,
  on peut en faire revenir une. Certaines reviendraient comme **fusions** : la table de la partie 7 est à refaire avec
  les 15 (l'étape 6 n'est pas codée).
- **Piste A** :
  - **L'ennemi préparé** : en l'air, gelé, englué, attiré, marqué ou pris au filet. Le toucher fait +50 % de dégâts et
    ×1,5 points (le tir au vol devient un cas parmi d'autres), avec un mot et un son à chaque sorte (« Au vol ! »,
    « Brisé ! », « Embrasé ! »…).
  - ~~Tirer sur un bâtiment le déclenche, pour tous~~ : refusé par le joueur (12.2), la réaction reste au talisman.
- **Piste B** :
  - **Un vrai enjeu pour la tour** : tranché par le joueur (12.2) : plus de reconstruction, 15 % de PV par vague.
  - **La montre prévient** : « Voie de gauche : la tour est attaquée », et montre l'état des trois voies.
  - **Des raisons de partir** : les caisses et les coffres, puis **les totems** (l'idée du 7 octobre, GDD section 23 : un
    totem au bout de chaque voie, protégé tant que le joueur est à plus de 15 m).
  - **Les gardiens plus forts** : viser qu'une baliste niveau 1 vaille environ la moitié d'un joueur sans amélioration (à
    caler aux tests).
- **Piste C** :
  - **À chaque vague, chaque voie a une tendance**, tirée au hasard : volants (Volant, Guêpe, Gargouille), costauds
    (Orc, Tortue, Cactus, Fantassin), essaim (Gluants, Champignons, Étoiles de mer, Rampants), tireurs (Mage, Liche) ; et
    une voie plus chargée que les autres.
  - **Annoncée pendant la pause** (montre et panneau de la boutique) : « Vague 3 · Gauche : volants · Milieu : costauds ·
    Droite : essaim (la plus chargée) ». Les bâtiments se déplacent gratuitement entre les vagues : on s'adapte.
  - Plus tard : un bonus d'**avant-poste** loin de la tour, et le **saboteur** qui s'en prend aux constructions.
- **Pour toutes les pistes** : la portée de chaque bâtiment dessinée pendant la pose ; ses éliminations comptées (un
  petit nom au-dessus de l'ennemi) ; un **bilan de fin de vague** (« Baliste : 12 éliminations · Tour de givre : 40
  ralentis ») ; des effets et des sons plus marqués.

### 12.4 Questions au joueur

1. ~~La liste des 15~~ : gardée, plus la bulle (16).
2. ~~Tirer sur un bâtiment le déclenche pour tous~~ : non, il reste au talisman.
3. ~~L'enjeu de la tour~~ : tranché (12.2), jamais reconstruite.
4. ~~Les tendances des voies annoncées pendant la pause~~ : oui.
5. ~~L'ennemi préparé, les bâtiments de soutien, les barricades~~ : gardé ; le phare et l'atelier ; réparées en entier.
6. Encore ouvert : le bilan de fin de vague par bâtiment (le compteur dans le panneau n'est pas retenu) ; deux renforts de
   la même sorte (deux phares) s'additionnent-ils ? Proposé : non, le meilleur compte ; un phare et un atelier, oui.

### 12.5 Ce que font les bons tower defense (proposé par Claude, 29 octobre)

Bloons TD6 en tête (le choix du joueur), et deux voisins : *Orcs Must Die* (un héros armé au milieu de ses pièges, le
plus proche de THUNK) et *Kingdom Rush* (des tours à deux branches). Ce qu'ils font bien, et ce que ça donnerait ici :

1. **Des ennemis qui exigent des réponses** (le cœur de BTD6 : camo, plomb, céramique, MOAB). Une seule tour ne suffit
   jamais : il faut composer. Ici, des **propriétés** posées sur n'importe quel ennemi, annoncées à l'avance (une vague
   les introduit, comme les rounds de BTD6) :
   - **blindé** : les flèches ordinaires lui font peu ; il faut l'explosion (mine, mortier), le feu ou les piques ;
   - **camouflé** : invisible, et intouchable, hors du rayon d'un **phare** (qui devient indispensable : la détection) ;
   - **volant** : le lance-filet le fait tomber (existe) ;
   - **régénérant** : seuls le gel et le feu arrêtent sa régénération ;
   - **sous bouclier** : un bouclier qui renvoie les flèches, que seuls les bâtiments cassent.
   L'arc ne peut plus tout faire seul : c'est ce qui rend les bâtiments nécessaires.
2. **Deux branches par bâtiment** (les chemins de BTD6, simplifiés comme dans *Kingdom Rush*) : au deuxième plan posé sur
   un bâtiment, on choisit une branche parmi deux (deux hologrammes), qui le transforme ; le troisième plan la monte au
   niveau 3. Par exemple, la baliste **perforante** (traverse une file d'ennemis) ou **lourde** (dégâts ×2 contre les
   costauds et les blindés). 16 bâtiments font 32 identités, sans en ajouter, et chaque branche a son aspect.
3. **Des bâtiments qui renforcent leurs voisins** (le village et l'alchimiste de BTD6) : le placement compte aussi par
   rapport aux autres bâtiments, pas seulement aux voies. Le **phare** deviendrait le soutien : dans son rayon, les
   bâtiments voient les camouflés et portent plus loin. On construit en grappes, à des endroits choisis.
4. **Un placement lisible** : BTD6 dessine la portée, et ses parcours font des boucles qu'une tour bien placée couvre
   plusieurs fois. Ici, pendant la pose : la portée au sol, et **la longueur de voie couverte** surlignée (« couvre 22 m de
   la voie de gauche »). Les bons coins se voient : les gués (ennemis ralentis 40 %), les virages, l'endroit où deux voies
   se rapprochent près de la tour (un bâtiment les couvre toutes les deux).
5. **Ce que chaque bâtiment a fait, et à qui il tire** : chaque singe de BTD6 compte ses ballons éclatés et a une priorité
   de cible (premier, dernier, fort, proche). Ici, dans le panneau du bâtiment : ses éliminations et ses dégâts, et sa
   priorité (premier, fort, volants, proche).
6. **Les capacités actives** (BTD6) : déjà prévu, **tirer sur un bâtiment le déclenche** (12.3) ; c'est la version VR.
7. **Le joueur est le héros** (les héros de BTD6, qui montent de niveau et renforcent les tours autour d'eux) : piste à
   discuter, les bâtiments à moins de 10 m du joueur tirent plus vite. Attention, ça tire en sens inverse de la piste B
   (les bâtiments qui tiennent seuls).
8. **Choisir ce qu'on construit** : dans BTD6, on achète la tour qu'on veut. Ici, les plans viennent au hasard des caisses.
   Piste : la boutique vend aussi des plans (prix qui monte), et **recycler** un bâtiment rend un plan.
9. **Les combos de pièges** (*Orcs Must Die*) : ralentir dans le goudron puis enflammer, projeter sur des piques, regrouper
   puis faire sauter une mine ; les éliminations par combo affichées et récompensées. Ça rejoint l'ennemi préparé (12.3).

### 12.6 Les évolutions (29 octobre, après Bloons TD6)

Le constat du joueur : ce qui rend Bloons TD6 intéressant, ce sont les **évolutions** de ses tours, chacune unique ; les
nôtres sont fades et manquent d'originalité. Ses pistes, et celles de Claude, triées par le joueur :

**Décidé** :
- **Une douzaine de tours de base** (au lieu de 17) : celles qu'on retire deviennent des évolutions des autres.
- **5 niveaux** (au lieu de 3). **Chaque niveau apporte une capacité nouvelle**, pas seulement des chiffres ; le
  niveau 3 transforme la tour (une autre silhouette), le niveau 5 est un spectacle.
- **Les niveaux 4 et 5 coûtent 2 plans chacun** : une tour au niveau 5 coûte 7 plans (une partie en donne une trentaine
  à une quarantaine, environ 24 caisses) ; monter haut devient un vrai choix.
- **Les duos, et quelques trios** choisis à la main (pas toutes les combinaisons : 10 tours font déjà 45 duos possibles).
- **Des tours ultimes**, de 4 ou 5 fusions : l'objectif de fin de partie (comme les Paragons de BTD6). Proposé : 3 ou 4 au
  total, **une seule par partie**.
- **Des évolutions pour l'arc** : les hauts niveaux préparent des ennemis pour les flèches du joueur, ou transforment ses
  flèches (marqué « arc » ci-dessous).
- **L'art d'abord en haut** : l'effort sur les modèles va d'abord aux niveaux 3 et 5, puis aux ultimes, **exagérés**
  (taille, pièces qui bougent, lueur, particules) ; une tour au niveau 5 se reconnaît à 50 m à sa silhouette.
- **Pas retenu** : une spécialisation au choix au niveau 3 (pas de branches : une seule échelle par tour) ; des
  évolutions qui changent selon l'endroit (les modifications du décor de la partie 8 restent comme décidées le 27).

**Décidé ensuite (29 octobre)** :
- **La rampe de lancement** (tremplin, niveau 5), « de la plus haute importance » pour le joueur : elle envoie les ennemis
  dans les airs à une vitesse incroyable ; ils retombent au bout de 5 à 6 s et **l'atterrissage les tue d'un coup**. « Ce
  serait extrêmement drôle. » Après le premier essai au casque (29 octobre) : **dix fois plus haut**, « dans la démesure
  totale », mais toujours **5 s** de vol ; et ils retombent **sur des ennemis** si possible, pour les blesser comme le
  boulet de la catapulte (le niveau 3). Revue ainsi, elle est **validée** : « c'est ce genre d'effet que je veux mettre en
  place pour les tours niveau 5 ».
- **C'est la direction de tous les niveaux 5** : un effet **très fort, et surtout très visible** par le joueur.
- **Le phare et l'atelier ne font plus qu'une tour** de soutien. Claude propose de garder le **phare** (son modèle et son
  faisceau existent, et la lumière se voit de loin) avec les deux renforts : dégâts et cadence.
- **Le seau d'huile est retiré.** Il n'y a donc plus de bâtiment sur la tour.
- **Le joueur veut revoir toutes les tours écartées depuis le début**, pour en remettre (12.8). Il aimait la **catapulte**
  et le **pendule** : à faire revenir en duo ou en évolution. **Décidé** (proposé par Claude, accepté) : la catapulte
  devient le **niveau 3 du tremplin**, le pendule le **niveau 3 de la barricade** (tableau ci-dessous) ; leur code existe
  déjà (`StructureCatapult`, `StructurePendulum`).

**Proposé par Claude : les 10 tours** (à valider). Celles qui ne sont plus des tours de base :
- **la mine** devient le niveau 4 des piques, **le goudron** celui du mortier, **l'anneau élémentaire** celui du phare ;
- **la baliste sur la tour** est remplacée par la baliste titan (niveau 5 de la baliste) ;
- **l'esprit gardien** deviendrait une ultime ;
- **l'atelier** est fondu dans le phare, **le seau d'huile** est retiré.

Les caisses n'ont plus que deux raretés (commune, rare).

| Où | Tours |
|---|---|
| Voie | Barricade, Piques, Tremplin |
| Bord, hauteur | Baliste, Mortier, Tour de givre, Lance-filet, Tour-aimant, Bulle |
| Soutien | Phare (dégâts et cadence) |

**Proposé par Claude : leurs 5 niveaux** (N1 : ce qu'elle fait aujourd'hui ; N3 : la transformation ; N5 : le spectacle,
revu dans l'esprit de la rampe de lancement). **Les boss** ne sont jamais projetés ni tués d'un coup (partie 6.2) : un
niveau 5 leur fait une grosse part de leurs PV à la place (par exemple 10 %).

| Tour | N2 | N3 (transformation) | N4 | N5 (spectacle) |
|---|---|---|---|---|
| Barricade | **Rempart** de pierre : PV ×2, et qui la frappe se blesse sur ses pointes | **Pendule** (l'ancien pendule, que le joueur aimait) : un portique au-dessus d'elle, d'où un boulet hérissé se balance en travers de la voie et fauche ceux qui l'assiègent (arrêtés devant elle, ils ne l'esquivent pas) | une herse s'abat toutes les 8 s et étourdit ceux qui frappent (préparés) ; ta flèche qui la traverse devient perforante (arc) | **Forteresse** : un mur géant, deux tours de guet qui criblent la voie de flèches ; chaque coup reçu lâche une onde qui renvoie les assaillants 10 m en arrière, en vol |
| Piques | barbelées : les blessés saignent | **Champ de pieux** : deux fois plus large | une **mine** cachée explose au premier passage, se réarme toutes les 20 s ; ta flèche la fait sauter (arc) | **Mâchoire de la terre** : toutes les 12 s, la voie s'ouvre et des pieux de 4 m jaillissent sur 10 m : les petits sont tués net, les autres embrochés en l'air |
| Tremplin | projette plus haut, plus longtemps en l'air (tir au vol) | **Catapulte** (l'ancienne catapulte, que le joueur aimait) : un bras attrape un ennemi et le lance sur un groupe ; il devient un boulet qui blesse et renverse ceux qu'il percute | trois godets : trois ennemis lancés d'un coup | **Rampe de lancement** (le joueur) : les ennemis partent dans le ciel à une vitesse incroyable, avec une traînée et un sifflement ; ils montent à 370 m, retombent 5 s plus tard en météores sur le groupe le plus serré (qu'ils blessent) et **l'atterrissage les tue** (cratère, poussière) |
| Baliste | carreaux perforants (3 ennemis) | **Scorpion** : double arc, deux cibles | **Harpon** : le carreau cloue sa cible au sol 1 s (préparé) et fait tomber les volants | **Baliste titan** : toutes les 6 s, un pieu géant traverse toute la voie, embroche les ennemis en brochette et les emporte, cloués au décor |
| Mortier | obus à fragmentation | **Mortier de siège** : deux tubes, plus de portée | obus de **goudron** : une flaque qui ralentit ; ta flèche enflammée l'embrase (arc) | **Bombardement** : toutes les 15 s, 12 obus pleuvent sur toute une voie, le sol reste en feu et les ennemis volent dans tous les sens |
| Tour de givre | le givre reste 2 s sur ceux qui sortent | **Pic de glace** : gèle net un ennemi toutes les 4 s ; ta flèche le brise (arc) | verglas : le sol gèle, les ennemis glissent et tombent | **Cœur de l'hiver** : toutes les 15 s, une tempête change toute la voie de son rayon en statues de glace pendant 3 s ; le moindre coup les fait voler en éclats |
| Lance-filet | le volant tombé reste au sol 3 s (préparé) | **Barillet** : deux filets à la fois | filet électrique : étourdit et blesse, au sol aussi | **Toile géante** : un filet immense couvre le ciel au-dessus de la voie 6 s ; tous les volants qui le touchent s'écrasent au sol |
| Tour-aimant | attire aussi les volants vers le sol | **Électro-aimant** : attire en continu dans une zone | tes flèches qui passent près sont courbées vers les ennemis attirés (arc) | **Singularité** : un trou noir aspire tous les ennemis à 15 m en une boule qui tournoie 3 s (préparés), puis les recrache en l'air |
| Bulle | deux bulles | **Bulle géante** : enferme un petit groupe | ta flèche qui crève une bulle rebondit vers la suivante (arc) | **Orbe céleste** : une bulle immense emporte tout un groupe très haut ; crevée par ta flèche (ou au bout de 6 s), ils s'écrasent et meurent, comme la rampe |
| Phare (soutien) | **cadence** : les bâtiments du rayon tirent 15 % plus vite (l'ancien atelier) | **Grand phare** : rayon et renforts plus grands, et de la portée | ta flèche qui traverse le faisceau s'enflamme (l'ancien **anneau** ; arc) | **Phare du jugement** : toutes les 10 s, un rayon géant tombe du ciel sur l'ennemi le plus fort, et tous les bâtiments du rayon tirent deux fois plus vite pendant 5 s |

Au niveau 1, le phare fait déjà briller les points faibles et donne +20 % de dégâts aux bâtiments de son rayon.

**Les fusions et les ultimes** : les règles et les listes, décidées, sont en 12.9.

### 12.7 L'ordre de travail (revu le 29 octobre)

Par lots, chacun codé puis testé au casque avant le suivant :

1. **Les règles et le catalogue** (*codé le 29 octobre, pas encore testé* : `WaveManager.RestoreDefenses`, réglage *Tower
   Repair Share* ; case *Retired* des réglages des bâtiments, cochée par `Editor/TowerDefenseTuningBaker` ; plus
   d'emplacements sur la tour sans bâtiment pour elle) : la tour ne regagne que 15 % de ses PV par vague et ne se reconstruit plus (les
   barricades, si) ; les caisses ne proposent plus que les 10 tours, en deux raretés (les autres sont retirées du Build
   Manager, leur code et leurs prefabs restent).
2. **Les 5 niveaux** (*en cours ; codé le 29 octobre, pas encore testé* : le système, et le **tremplin** en entier, validé au
   casque ; les 9 autres tours codées le soir même, pas encore testées) : le niveau maximal passe à 5, les niveaux 4 et 5 coûtent 2 plans ; puis les évolutions, tour par
   tour (10 tours, 4 capacités chacune : le plus gros lot, en plusieurs fois, avec des modèles provisoires exagérés).
3. **L'ennemi préparé** (piste A), dont beaucoup d'évolutions se servent (*codé le 30 octobre, pas encore testé* : 12.12).
4. **Le soutien et le placement lisible** : le phare (dégâts et cadence) ; pendant la pose, la portée au sol, la longueur de voie
   couverte, les renforts reçus.
5. **Les menaces par voie** (piste C), annoncées pendant la pause.
6. **Tenir le front** (piste B) : l'alerte de la montre, l'état des voies, l'équilibrage des gardiens.
7. **Les fusions** (12.9) : les duos, les trios, puis les ultimes (*commencé le 31 octobre : le système et 4 duos, pas encore
   testés* : 12.13).

Ensuite, l'équilibrage aux tests, puis les modèles, en commençant par les niveaux 3 et 5.

### 12.8 Les tours écartées depuis le début (29 octobre, pour que le joueur en remette)

« Codée » : son code et son prefab existent (étape 5, 28 octobre) ; la remettre ne demande que de la rajouter aux caisses
et de lui donner ses 5 niveaux.

| Tour | Où | Ce qu'elle faisait | Écartée | État |
|---|---|---|---|---|
| Bélier | voie | (idée seulement) | 27 octobre, au premier brainstorm | jamais codée |
| Rocher roulant | voie | (idée seulement) | 27 octobre | jamais codée |
| Tourniquet | voie | (idée seulement) | 27 octobre | jamais codée |
| Arbalète à répétition | bord | rafales rapides et faibles, bonne contre les volants | 29 octobre (doublon de la baliste et du filet) | codée |
| Tour de foudre | bord, hauteur | un éclair qui saute entre 3 ennemis, volants compris | 29 octobre (doublon du mortier) | codée |
| Lance-flammes | bord | un cône de feu, les ennemis brûlent | 29 octobre (le feu passait par le goudron) | codée |
| Miroir | bord | renvoie ta flèche qui le touche vers l'ennemi le plus proche | 29 octobre | codée |
| Catapulte | bord | attrape un ennemi et le lance sur un groupe : un boulet vivant | 29 octobre ; le joueur l'aimait : revient comme niveau 3 du tremplin (décidé) | codée |
| Soufflerie | bord | un cône de vent repousse les ennemis vers la forêt et dévie les volants | 29 octobre | codée |
| Pendule | au-dessus de la voie | un boulet se balance en travers de la voie et fauche les ennemis | 29 octobre ; le joueur l'aimait : revient comme niveau 3 de la barricade (décidé) | codé |
| Geyser | voie | jaillit à intervalles et projette à la verticale ce qui est dessus | 29 octobre | codée |
| Trappe | voie | s'ouvre sous un groupe : les ennemis ressortent plus loin en arrière | 29 octobre | codée |
| Anneaux élémentaires (feu, foudre, double) | partout | une flèche qui les traverse prend du feu, de la foudre, ou se dédouble | 29 octobre : fondus en un, puis devenus le niveau 4 du phare | codés |
| Mine | voie | explose au premier passage, se réarme à chaque vague | 29 octobre : devenue le niveau 4 des piques | codée |
| Goudron | voie | une flaque qui ralentit, que le feu embrase | 29 octobre : devenu le niveau 4 du mortier | codé |
| Baliste sur la tour | la tour | une grosse baliste à côté du joueur (légendaire) | 29 octobre : remplacée par la baliste titan | codée |
| Esprit gardien | partout | un esprit qui chasse les ennemis isolés (légendaire) | 29 octobre : deviendrait une ultime | codé |
| Seau d'huile | la tour | une cible : la toucher renverse l'huile autour de la tour (idée du joueur) | 29 octobre, par le joueur | codé |
| Atelier de l'ingénieur | bord | les bâtiments proches tirent plus vite | 29 octobre : fondu dans le phare | jamais codé |

Avant la refonte, le **brasero** (en haut de la tour, on y allumait ses flèches) a été retiré le 12 octobre.

### 12.9 Les fusions (proposées par Claude, décidées le 29 octobre)

Le joueur : « pas ball trap ; sinon tout me plaît ». Le duo Ball-trap (tremplin + baliste) est retiré ; le reste est
décidé. Puis il ajoute le **Canon** (baliste + mortier).

Elles remplacent la table de la partie 7 (faite pour les 27 constructions d'avant). Les noms évitent ceux des duos de la
flèche (Tempête, Blizzard, Patinoire, Flipper…).

**Les règles** :
- **Un duo** : deux tours bâties, chacune **au moins au niveau 3**. On **déplace l'une sur l'autre** (le panneau
  « Déplacer », entre les vagues) ; un hologramme doré montre le résultat et son nom. La fusion **garde tout ce que savent
  les deux tours**, à leur niveau, **plus un effet de fusion** qu'on ne trouve nulle part ailleurs. Un plan de l'une ou de
  l'autre posé dessus fait monter cette partie, jusqu'au niveau 5 ; quand les deux sont au niveau 5, l'effet de fusion
  devient un spectacle, comme les niveaux 5.
- **Un trio** : de même, trois tours au moins au niveau 3 (un duo plus une tour). Il n'y en a que cinq.
- **Une ultime** : une tour **au niveau 5**, plus 3 ou 4 autres au moins au niveau 3. Elle devient une tour unique, au
  comportement à elle (elle ne garde pas ceux des tours fondues). **Une seule par partie**, annoncée en grand.
- **Le prix** : un duo de deux tours au niveau 3 coûte 6 plans, un trio 9, une ultime 16 à 19 (une partie en donne une
  trentaine à une quarantaine) : une ultime et un duo, ou trois ou quatre duos.
- **Où** : à la place de la tour du dessous ; une fusion d'une tour de la voie et d'une tour du bord reste sur la voie.
- On ne défait pas une fusion ; on peut la déplacer entre les vagues.

**Les 13 duos** (chaque tour en a au moins deux) :

| Duo | Tours | Effet de fusion |
|---|---|---|
| Rempart à ressort | Barricade + Tremplin | ceux qui la frappent sont projetés 10 m en arrière, en vol (tir au vol) |
| Mur de glace | Barricade + Tour de givre | les assaillants gèlent contre le mur, en statues ; ta flèche les fait éclater |
| Filet à pointes | Piques + Lance-filet | les volants pris sont tirés sur les piques et y restent cloués |
| Broyeur | Piques + Tour-aimant | l'aimant tire les ennemis sur un tapis de pieux qui tourne |
| Chute fatale | Piques + Bulle | les bulles éclatent au-dessus des piques : les petits ennemis meurent sur le coup |
| Canon humain | Tremplin + Mortier | les ennemis sont tirés comme des obus sur les groupes et explosent à l'impact |
| Canon *(idée du joueur, 29 octobre)* | Baliste + Mortier | un tir explosif, tendu et rapide (pas en cloche comme le mortier) : le boulet explose à l'impact et fait des dégâts de zone |
| Baliste de précision | Baliste + Phare | elle vise les points faibles (les mêmes multiplicateurs que toi), le faisceau suit sa cible |
| Mortier de glace | Mortier + Tour de givre | les obus laissent une plaque de glace et gèlent 1 s ceux qu'ils touchent |
| Point de chute | Mortier + Tour-aimant | l'aimant rassemble les ennemis en un point, que le mortier pilonne |
| Filet aimanté | Lance-filet + Tour-aimant | les volants à 15 m sont aspirés dans un filet et écrasés au sol |
| Bulle piégée | Bulle + Mortier | l'ennemi lâché tout en haut explose en touchant le sol et blesse le groupe |
| Aurore boréale | Phare + Tour de givre | le faisceau ralentit et gèle ce qu'il éclaire ; ta flèche qui le traverse devient une flèche de glace (arc) |

**Les 5 trios** :

| Trio | Tours | Effet de fusion |
|---|---|---|
| Volcan | Tremplin + Mortier + Phare | un cratère de lave sur la voie : les ennemis jaillissent et retombent dans la lave en feu |
| Grand chapiteau | Tremplin + Bulle + Baliste | les ennemis sont jonglés entre le tremplin et les bulles sans jamais toucher le sol, pendant que la baliste les crible |
| Boule à neige | Tour de givre + Tour-aimant + Bulle | l'aimant les rassemble, une bulle de glace les emporte gelés, ils retombent en éclats |
| Chasseur de ciel | Lance-filet + Baliste + Phare | aucun volant ne passe : le faisceau le suit, le filet le prend, le carreau l'abat |
| Siège infernal | Barricade + Piques + Mortier | la barricade arrête, le sol se hérisse sous les assiégeants, le mortier pilonne l'attroupement |

**Les 4 ultimes** (une seule par partie) :

| Ultime | Tours | Ce qu'elle fait |
|---|---|---|
| Esprit gardien | Phare (niveau 5) + Bulle + Baliste + Tour-aimant | un esprit géant flotte au-dessus des voies ; il attrape les ennemis à pleines mains et les jette au loin |
| Colosse | Barricade (niveau 5) + Piques + Mortier + Baliste | la forteresse se lève et marche sur la voie : elle écrase les ennemis à chaque pas |
| Tornade | Tour-aimant (niveau 5) + Tour de givre + Lance-filet + Bulle | une tornade avale toute une voie et recrache les ennemis au loin ; l'atterrissage les tue, comme la rampe |
| Canon orbital | Tremplin (niveau 5) + Mortier + Baliste + Phare | un canon géant aspire les ennemis d'une voie et les tire dans le ciel, d'où ils retombent en météores sur les autres |

Comme pour les niveaux 5, **les boss** ne sont ni projetés ni tués d'un coup : une ultime leur fait une grosse part de
leurs PV.

### 12.10 Le lot 2, tour par tour (commencé le 29 octobre)

**Le système** (codé, pas encore testé) :
- `StructureDefinition.Level.plans` : les plans d'un niveau (0 : 1 jusqu'au niveau 3, 2 aux niveaux 4 et 5) ;
  `PlansFor(level)` ; `BuildManager.TryBuild` les dépense ; le panneau d'un bâtiment affiche « 2 plans » ; l'hologramme dit
  « Améliorer : niveau 4 (2 plans) », ou en rouge « il faut 2 plans ».
- Le niveau maximal est le nombre de niveaux du réglage : **une tour ne passe à 5 niveaux qu'avec ses évolutions** (aucun
  plan dépensé pour un niveau vide). Les pièces de modèle des niveaux 4 et 5 s'ajoutent à `m_LevelParts` (chacune
  s'allume à partir de son niveau). Les cases occupées se mesurent au plus haut niveau.
- `Enemy.Launch(velocity, source, lethal, gravityScale)` : l'atterrissage tue (la rampe) ; le vol a sa propre gravité
  (la rampe : 370 m en 5 s) ; `GuideFlight(point, vitesse)` règle sa vitesse à plat pour retomber sur ce point ; un vol
  peut durer 9 s (6 avant).
- `Editor/TowerDefenseEvolutionsBaker` : pour chaque tour, une fois, les pièces des niveaux 3 à 5 (modèles provisoires,
  exagérés) et son réglage à 5 niveaux, avec leurs textes (proposés ; ceux des niveaux 2 et 3 sont réécrits, puisque
  ces niveaux changent).

**Les tours faites** :
- **Tremplin** (`StructureTrampoline`) : N1 un ennemi projeté ; N2 plus haut ; N3 **catapulte** (le bras attrape l'ennemi
  le plus proche, à 3,5 m, et le lance sur le groupe le plus serré à 16 m, ou 8 m vers la forêt ; à l'impact, 22 de dégâts
  à 2,5 m et ils sont renversés, le boulet en prend la moitié) ; N4 trois godets (32 de dégâts) ; N5 **rampe de
  lancement** (jusqu'à 6 ennemis sur la rampe, zone ×1,4 ; depuis l'essai du 29 : ils montent à **370 m** et retombent
  en **5 s**, d'où 296 m/s au départ et une gravité de vol douze fois la vraie (`m_RampHeight`, `m_RampFlightTime`) ;
  chacun vise le groupe au sol le plus serré à 30 m (hors de la rampe ; un groupe déjà visé compte moins, la pluie se
  partage) et s'y guide tout le vol, 60 m/s à plat au plus ; sans groupe, il retombe vers la forêt ; traînée de feu de
  plus de 200 m, sifflement au départ et 0,8 s avant l'impact ; l'atterrissage le tue et blesse ceux qui sont autour
  comme le boulet (32 de dégâts à 2,5 m, renversés), avec explosion et fumée ; tué en l'air, il retombe quand même et son
  impact compte ; un boss y perd 10 % de ses PV max et recule ; validée au casque le 29) ; recharges 3 / 2,2 / 3,5 / 3 / 2 s. Modèle : le bras et l'axe, la catapulte du Tower Defense Kit, deux godets
  de plus, puis deux rails inclinés de 4 m qui luisent, deux pylônes à feu, un ressort et des rayures jaunes et noires.
- **Les 9 autres tours** (codées le 29 octobre au soir, « oui, enchaîne » ; pas encore testées). Valeurs proposées, à régler :
  - **Barricade** (`StructureBarricade`) : PV 400 / 800 / 1 000 / 1 300 / 2 500 ; N2 rempart (les pointes dès le niveau 2) ;
    N3 pendule (portique devant le mur, boulet hérissé qui se balance en travers, 30 / 36 / 45 de dégâts, renverse) ; N4
    herse (toutes les 8 s s'il y a quelqu'un devant : 20 de dégâts, étourdis 1,5 s) et ta flèche qui traverse le mur
    devient perforante (+2 ennemis, traînée dorée) ; N5 forteresse (mur de pierre de 3 m crénelé, deux tours de guet qui
    tirent un carreau chacune toutes les 0,45 s, 8 de dégâts à 26 m ; chaque coup reçu au corps à corps lâche une onde qui
    renvoie en vol, 10 m vers la forêt, ceux qui sont à 5 m devant, une fois par 1,5 s). Tout tient dans ses 4 m de large.
  - **Piques** (`SpikeTrap`) : N2 barbelées (saignement 4 toutes les 0,5 s pendant 3 s) ; N3 champ de pieux (5,2 m de large,
    deux plateaux de plus) ; N4 mine au centre (60 de dégâts à 3 m, projette ; se réarme en 20 s ; une flèche à 1,3 m la
    fait sauter) ; N5 mâchoire (toutes les 12 s s'il y a quelqu'un : pieux de 4 m sur 10 m de voie, 120 de dégâts, les
    survivants projetés à 9 m/s et étourdis ; un boss perd 10 % de ses PV ; quatre crocs de pierre et des runes rouges).
  - **Baliste** (`Ballista`, `BallistaBolt` : un rappel par ennemi touché) : N2 perforante (3 ennemis) ; N3 scorpion (un
    deuxième carreau sur la deuxième cible la plus proche) ; N4 harpon (cloue 1 s au sol, un volant tombe 1,5 s) ; N5
    titan (toutes les 6 s, un pieu ×4, 6 fois les dégâts, traverse tout, emporte ceux qu'il touche à 12 m/s) ; dégâts
    10 / 14,7 / 21,3 / 26 / 32, cadence 3 / 2 / 1,25 / 1,1 / 1 s. Le lance-filet, qui en hérite, n'a rien de tout ça.
  - **Mortier** (`StructureMortar`) : N2 fragmentation (4 éclats à 3,5 m au plus, 35 % des dégâts à 1,6 m) ; N3 deux tubes ;
    N4 goudron (une flaque de 7 s qui ralentit de moitié, `GroundPatch` ; une flèche enflammée l'embrase) ; N5 bombardement
    (toutes les 15 s, 12 obus l'un après l'autre sur 24 m de voie dans le sens de la marche du groupe visé, qui projettent
    les ennemis et laissent le sol en feu 5 s, 10 de dégâts par seconde ; pas d'éclats, pour le Quest) ; dégâts 30 / 42 / 60
    / 70 / 85.
  - **Tour de givre** (`FrostTower`, `IceStatue`) : N2 le givre reste 2 s ; N3 pic de glace (toutes les 4 s, l'ennemi le plus
    solide de la zone est gelé 2,5 s dans un bloc de glace, son animation figée ; le premier coup qui ne vient pas du givre
    brise la glace et lui coûte 30 % de ses PV max) ; N4 verglas (chaque seconde, un ennemi sur quatre glisse et tombe) ; N5
    cœur de l'hiver (toutes les 15 s, tous les ennemis au sol de la zone gèlent 3 s et le moindre coup les tue ; un boss
    perd 10 % ; une sphère de glace s'étend jusqu'au bord de la zone).
  - **Lance-filet** (`StructureNetLauncher`) : N2 5 s au sol ; N3 deux filets ; N4 filet électrique (15 de dégâts ; sans
    volant, il vise un ennemi au sol et l'étourdit 2 s) ; N5 toile géante (toutes les 15 s s'il vise un volant : un disque
    de 14 m de rayon au-dessus de lui pendant 6 s, tout volant dessous tombe).
  - **Tour-aimant** (`StructureMagnet`) : N2 les volants à portée tombent 1,5 s ; N3 électro-aimant (une petite impulsion
    toutes les 0,25 s, 1,6 m/s) ; N4 tes flèches dans sa zone se courbent vers les ennemis devant elles (240°/s) ; N5
    singularité (toutes les 15 s : jusqu'à 14 ennemis à 15 m aspirés en 0,8 s dans une boule à 3 m au-dessus d'eux, qui
    tournoie 3 s, puis recrachés à 11 m/s vers le haut, dans tous les sens ; `Enemy.Hold` et `Release`). Portée 8 / 9 / 10 /
    11 / 12 m.
  - **Bulle** (`StructureBubble`) : N2 deux bulles ; N3 bulle géante (jusqu'à 3 ennemis à 2,5 m) ; N4 rebond (ta flèche qui
    crève une bulle fait ses dégâts à la bulle suivante la plus proche, à 20 m, et la crève, de bulle en bulle) ; N5 orbe
    céleste (toutes les 15 s, jusqu'à 8 ennemis à 4 m montent à 30 m ; crevée, ou au bout de 6 s, ils meurent à
    l'atterrissage, `Enemy.MarkLethalLanding`).
  - **Phare** (`StructureLighthouse`, `Structure.Support` et `Boost`) : le soutien, dès le N1 (+20 % de dégâts aux bâtiments
    de son rayon ; ce qui était prévu au lot 4 est donc fait, sauf l'affichage pendant la pose) ; N2 cadence +15 % ; N3 grand
    phare (rayon 20 m, faisceau ×1,6, +30 % / +25 %) ; N4 ta flèche qui traverse un rayon du faisceau s'enflamme (sa cible
    brûle : 40 % des dégâts en 3 s) ; N5 jugement (toutes les 10 s, une colonne de lumière de 80 m sur l'ennemi qui a le
    plus de PV : 150 de dégâts ou la moitié de ses PV max, 10 % pour un boss ; les bâtiments du rayon tirent deux fois plus
    vite 5 s). Rayon 12 / 14 / 20 / 20 / 24 m.
  - Communs : `ArrowWatch` (les flèches en vol, image par image), `GroundPatch` (les flaques), `IceStatue` (la glace),
    `FollowPart` (une pièce qui suit sa tourelle). Les modèles des niveaux 3 à 5 (`Editor/TowerDefenseEvolutionsBaker`) sont
    provisoires, en formes simples et en modèles déjà dans le projet (tour de guet et mine de Quaternius, tonneau).
  - **Pas encore** : les fusions. (L'ennemi préparé, dont parlent la herse, le harpon et la singularité : fait le 30
    octobre, 12.12.)
- **Retours du premier essai des 9 tours** (29 octobre au soir ; corrigés, pas encore retestés) :
  - **Bug** : un ennemi tué par les pointes de la barricade pendant son propre coup restait « en attaque », mort mais debout
    (les flèches le traversaient, il ne bougeait plus, même la barricade tombée) : `Enemy.Update` s'arrête s'il est mort de
    son coup. Il existait déjà au niveau 3 ; les pointes dès le niveau 2 l'ont fait voir.
  - **Bug** : l'orbe céleste plantait le jeu (la liste des ennemis relue pendant qu'on la parcourait) : corrigé.
  - **Pose** : l'emprise d'un bâtiment sur la grille est celle de son **niveau 1** (`Structure.ModelSize`), les niveaux
    suivants peuvent déborder ; les petits cailloux (moins de 1,2 m de large et 70 cm de haut) ne bloquent plus une case, et
    l'herbe tolère ses bosses (45°, 60 cm d'écart) ; la grille se dessine sur **36 m** autour du joueur (16 avant) ; le menu
    des plans reste **collé** devant le joueur (avant, il flottait puis le rejoignait).
  - **Bulle** : une flèche qui traverse la bulle blesse ceux qu'elle enferme (avant, il fallait toucher l'ennemi).
  - **Piques** : la mâchoire couvre **20 m** de voie, en pieux fins et serrés.
  - **Givre** : la glissade du verglas se voit (l'ennemi part en avant, tournoie et retombe, `Enemy.Tumble`) ; le cœur de
    l'hiver gèle **10 s**, **boss compris** (leur glace brisée leur coûte 10 % de leurs PV, ils ne volent pas en éclats).
  - **Mortier** : posé sur une tour carrée du kit, une fois et demie plus grand ; au niveau 5, une batterie de quatre tubes
    de plus, une pile d'obus et des rayures ; le goudron en noir brun mat (il paraissait bleu de loin) ; n'importe quelle
    flèche l'embrase (pas seulement une flèche enflammée).
  - **Baliste** : au niveau 3, une deuxième baliste du kit sur un cadre au-dessus de la première ; au niveau 4, des harpons
    barbelés et un treuil ; au niveau 5, une baliste géante sur une plate-forme de pierre, d'où part le pieu.
  - **Lance-filet** : les filets sont de vrais filets (un dôme de cordes, fait par le code), la toile géante aussi ; au
    niveau 5, un canon à filet du kit, un filet enroulé, des bobines et deux antennes.
  - **Forteresse** : des tours de guet bien plus grandes, aux deux bouts du mur.
  - **Phare** : il se construit aussi au bord de la voie ; le deuxième rayon n'est plus à l'envers ; au niveau 4, un
    **anneau de feu** flotte sur la voie la plus proche du phare, à 14 m de ta tour et 2,6 m de haut, tourné vers toi : ta
    flèche qui le traverse s'enflamme (le faisceau, trop dur à viser, n'enflamme plus).
  - Tout passe par `Editor/TowerDefenseEvolutionsBaker` (partie « retouches », une fois par prefab, marque « Retouche du
    29 octobre »).
- **Les modèles refaits avec de vrais modèles** (29 octobre au soir, « oui, refais-les » ; pas encore vus) : le joueur
  voulait plus d'assets ; le *Fantasy Town Kit* de Kenney (CC0) est importé (`Assets/kenney_fantasy-town-kit`), le *Castle
  Kit* était déjà là en entier. Deuxième retouche du baker (marque « Retouche 2 du 29 octobre », après la première) : la
  herse en métal du château (barricade, niveau 4) ; la forteresse en rempart crénelé et deux tours hexagonales de quatre
  étages à bannière (niveau 5) ; la baliste titan en baliste de siège ; le mortier sur une tour carrée de château (base et
  étage), une charrette de goudron (niveau 4) et des bannières aux coins (niveau 5) ; le générateur électrique de
  miro_art_studio pour le filet électrique (niveau 4) et l'électro-aimant (niveau 3) ; une fontaine ronde autour du chaudron
  de la bulle (niveau 4) ; quatre lanternes autour de celle du grand phare (niveau 3). Encore en réserve, téléchargés le 27
  octobre et pas importés (`Assets_TowerDefense/`) : KayKit Dungeon et Medieval Hexagon, Quaternius Fantasy Props et Medieval
  Village (environ 1 500 modèles).

### 12.11 L'allure des tours (30 octobre)

Troisième retour du joueur, après avoir vu les modèles des kits : la herse tournée de 90°, la forteresse qui avalait la
barricade, des pièges peu cohérents, le niveau 4 de la baliste « sans sens », le mortier trop grand, des lampadaires qui
tournaient autour du phare et ses deux rayons à l'envers, le lance-filet trop bas et son groupe électrogène hors du style
du jeu ; « de façon générale », des pièces qui fusionnent d'un niveau à l'autre, et des tours qui se ressemblent parce
qu'elles partent des mêmes pièces. Il voulait que ce soit propre, quitte à y passer du temps. Pas besoin d'assets
nouveaux : les kits de Kenney du projet (Tower Defense, Castle, Fantasy Town) suffisent ; les tours de Quaternius
(sombres, réalistes) sont écartées, la carte est dans le style pastel de Kenney.

- **Une palette par tour** : pierre, sable (la pierre du Castle Kit), bois, métal, blanc et une couleur d'accent à elle
  (barricade rouge garance, piège rouille, baliste bleu roi, mortier orange feu, givre cyan glacier, lance-filet vert forêt,
  aimant violet, bulle rose et cuivre, phare blanc et rouge, tremplin jaune). Les modèles des kits prennent une copie de
  leur texture de couleurs, recolorée case par case selon son rôle, en gardant son dégradé
  (`Art/TowerDefense/Palettes/<Tour>_<Kit>.png`, écrite par `Tools/allure_tours/palettes.py`) ; les formes simples aux
  matériaux partagés (bois, fonte, métal, pierre, fanion…) le matériau plat du rôle (`Palettes/<Tour>_<rôle>.mat`).
- **Barricade** : la herse de face (le modèle de Kenney s'étend le long de son axe Z), ses glissières jusqu'en haut, un
  linteau ; niveau 5 : à la place du rempart et des grosses tours, deux **tours fines** de pierre à meurtrières de chaque côté
  de la herse, une bannière rouge côté joueur, et au sommet une **petite baliste** qui se tourne vers sa cible
  (`StructureBarricade`, réglage *Tower Muzzle*).
- **Pièges** : un cadre de poutres sombres ferrées autour du plateau et de ses rallonges (niveau 3, sans doubler les
  poutres communes) ; niveau 5 : deux rangées de **crocs de fer** à pointe rouillée sur les bords (plus de blocs de pierre
  ni de runes) ; les piques prennent le métal de la tour.
- **Baliste** : la charpente du deuxième arc (niveau 3) et la plate-forme du titan (niveau 5) ne basculent plus quand la
  tourelle vise en haut ou en bas (`FollowPart` : *Yaw Only*, *Offset*) ; les poteaux sont hors des bras du premier arc ;
  niveau 4 : de vrais **harpons** (pointe et crochets) sur les deux arcs, la corde jusqu'au **treuil** à l'arrière ;
  niveau 5 : plate-forme de pierre fixe sur quatre piliers au sol, la baliste de siège **face à l'avant** (le modèle tire
  vers -X), qui tourne sur elle-même.
- **Mortier** : un **bastion bas** à créneaux (2,4 m, 3,3 m de haut en tout ; il dépassait 7 m), brique sombre à bandeau
  orange ; tourelle plus petite, deuxième tube serré contre le premier ; piles de boulets au pied (niveau 2), tonneaux de
  goudron debout sur les côtés (niveau 4, plus de charrette) ; niveau 5 : quatre **petits mortiers** sur les coins des
  créneaux, hors de la course des tubes qui tournent, et deux bannières.
- **Phare** : un **vrai phare de Kenney** (socle, fût rond à bandes rouges et blanches, galerie, lanterne sous un toit
  conique) à la place de la tour de guet de Quaternius ; les deux **rayons dans le bon sens** (le shader s'efface vers le
  haut du cylindre : le pied, lumineux, à la lanterne) ; **plus de lampadaires** qui tournaient avec le faisceau ; posé sur
  2 × 2 cases (2 × 3 avant).
- **Lance-filet** : un **nid de guetteur sur pilotis** (plus de 5 m), échelle côté joueur ; un lance-filet à deux bouches à
  la place de la petite baliste (il ne ressemble plus à la baliste) ; niveau 3, le **barillet** de six filets roulés ;
  niveau 4 : un **paratonnerre de cuivre** le long d'un pilier (plus de groupe électrogène) ; niveau 5 : deux antennes et le
  grand canon à filet au-dessus de la tourelle.
- **Aimant** : un socle carré (il était rond, comme le givre) ; niveau 3 : deux **condensateurs** de cuivre à lueur violette
  reliés aux branches (plus de groupe électrogène ni de la couronne de cristaux, celle du givre).
- **Givre** : les cristaux grossissent moins (×1,35 au niveau 5 au lieu de ×1,7 : ils avalaient le pic et la couronne) ; le
  pic flotte au-dessus d'eux, la couronne au-dessus du pic.
- **Bulle** : la fontaine du niveau 4 à l'eau bleu-vert (palette à part).
- **Plus de faces confondues** : deux faces de pièces différentes dans le même plan et le même sens scintillent en jeu (le
  bug du tremplin). `Tools/allure_tours/faces_superposees.py` les cherche à chaque niveau, à chaque tour : il en restait
  entre les pièces empilées des kits (fûts du phare, pilotis, étages du givre), dans la charpente de la baliste, sous les
  condensateurs et la fontaine. Règle : une pièce posée sur une autre y est **enfoncée de 6 mm**.
- **Le bug de la rivière** : des ennemis retombés d'une bulle restaient plantés dans la rivière, intouchables. Le journal de
  la partie montrait `Physics.ClosestPoint` appelé sur un collider concave (une berge) au début d'une chute
  (`Enemy.TrySweep`). Depuis le 30 octobre, ce qu'il touche déjà au départ n'est plus pris pour le sol (il se pose sur le sol
  sous lui quand il l'atteint), et à l'atterrissage, s'il ne peut pas rejoindre à pied l'endroit d'où il est parti (un bout
  de NavMesh isolé), il est replacé au plus près d'où il le peut (`Enemy.Reachable`).
- **Comment c'est fait** : `Editor/TowerDefenseLookBaker`, une fois par prefab (marque « Allure du 30 octobre », après les
  retouches du 29), applique les opérations de `Editor/TowerDefenseLook.json` (une par ligne : détruire, déplacer, ajouter un
  modèle ou une forme, suivre une tourelle, régler un champ, peindre) puis les palettes. Le même fichier sert aux outils de
  `Tools/allure_tours` (Python, numpy et Pillow), qui lisent les prefabs et les FBX : `apercu.py` dessine chaque niveau
  (vues iso, face, côté, dessus), `faces_superposees.py`, `palettes.py`, `modeles.py` (vues et dimensions d'un modèle :
  c'est ainsi qu'on a vu la herse de profil et la baliste de siège tournée vers -X) ; `conception.py` écrit le fichier.
- **Après le premier essai (30 octobre au soir)** : le baker a bien construit les 10 tours, identiques aux dessins (même
  taille au centimètre). Le joueur a eu un plantage : la flaque de goudron en feu (`GroundPatch`) parcourait la liste des
  ennemis vivants en les brûlant, et celui qu'elle tuait en sortait pendant la boucle ; elle parcourt maintenant une copie.
  Et une erreur de Unity : les lueurs des points faibles du phare étaient rattachées aux ennemis, puis ramenées au phare
  pendant qu'un ennemi s'activait ou se désactivait ; elles restent maintenant hors des ennemis et les suivent à chaque
  image. Les prefabs enregistrés, relus avec leurs vrais matériaux, avaient encore deux faces confondues (le plancher sous
  chaque arc de la baliste, presque de la même couleur que le pied de l'arc : le contrôle les ignorait à tort) : une
  deuxième passe du fichier (« Allure 2 du 30 octobre ») baisse ces planchers de 6 mm.

### 12.12 L'ennemi préparé (lot 3, 30 octobre)

Codé le 30 octobre, compilé, **pas encore testé** (tests : `Checklist_Demo.md`). La règle décidée le 29 (12.2, 12.3) :

- **Un ennemi est préparé** quand un bâtiment l'a mis **en l'air** (projeté, dans une bulle, tenu par la singularité),
  **gelé** (tour de givre), **cloué** (le harpon de la baliste), **pris au filet** (lance-filet, harpon, aimant), **étourdi**
  (la herse, le filet électrique, il se relève d'une chute), **englué** (le goudron du mortier), **attiré** (tour-aimant :
  1,5 s après chaque impulsion, en continu pour l'électro-aimant) ou **marqué** (dans le rayon du phare).
- **Un coup du joueur** (flèche, katana, faille du katana, comme le tir au vol du 27 octobre) sur un ennemi préparé fait
  **plus de dégâts** et rapporte **1,5 fois plus de points** (la touche et l'élimination).
- **Les dégâts dépendent de la difficulté à préparer** (31 octobre, le joueur ; avant, +50 % pour toutes) : plus il est
  difficile d'obtenir la préparation et d'en profiter, plus ils montent (tableau ci-dessous).
- **Un mot et un son pour chaque sorte**, au-dessus de l'ennemi : « Au vol ! », « Brisé ! », « Cloué ! », « Au filet ! », « Sonné ! », « Englué ! », « Attiré ! », « Marqué ! ». Les sons sont de
  petits sons de Kenney déjà dans le projet (CC0), d'une même famille, pour qu'on reconnaisse vite un tir préparé.
- **Une seule sorte compte par coup**, sans cumul : la plus marquante (gelé, cloué, en l'air, pris au filet, étourdi,
  englué, attiré, marqué).
- **Seuls les bâtiments préparent** : la flèche de glace du joueur (le gel des pièces de la flèche) ne compte pas, sinon il
  se préparerait lui-même ses tirs.
- **Les tours qui le promettaient** le font maintenant (dégâts, mot et son) : la herse (barricade N4), le harpon
  (baliste N4), la singularité (aimant N5), le volant tombé du lance-filet (N1), le pic de glace et le cœur de l'hiver
  (givre N3 et N5).
- **À surveiller aux tests** : le phare marque tous les ennemis de son rayon (12 à 24 m), donc tout ce qui y passe est
  préparé : c'est pourquoi il ne donne que +15 %. Si c'est encore trop facile, on peut le retirer de la liste.
- **Comment c'est fait** : `Enemies/Enemy.Prepared.cs` (les sortes, `Enemy.Prepare`, et le filtre des dégâts du `Health`
  de l'ennemi, qui relève la préparation avant que le coup ne compte : la glace se brise sous la flèche) ;
  `Economy/ScoreManager.cs` (les points ; les réglages *Prepared Points Multiplier*, l'ancien *Air Shot Multiplier* dont la
  valeur est gardée, et *Prepared Cues* : les dégâts, le mot, sa couleur et le son de chaque sorte ; les dégâts proposés
  sont dans `Enemy.DefaultPreparedDamage`) ;
  les bâtiments signalent ce qu'ils font (`GroundPatch`, `StructureMagnet`, `StructureLighthouse`, `Ballista`,
  `IceStatue`). `Editor/PreparedShotBaker` donne les sons au Score Manager de la scène, une fois : enregistrer la scène
  (Ctrl+S).

**Les dégâts par sorte** (31 octobre ; proposés par Claude, réglables dans le Score Manager, *Prepared Cues*, champ *Damage*) :

| Sorte | Mot | Dégâts | Pourquoi |
|---|---|---|---|
| En l'air | « Au vol ! » | ×2 (+100 %) | une cible qui vole, vite et peu de temps ; il faut le tremplin, la bulle ou la singularité |
| Gelé | « Brisé ! » | ×2 | un seul coup en profite (il brise la glace) ; le pic de glace ne gèle qu'un ennemi toutes les 4 s |
| Cloué | « Cloué ! » | ×1,75 | le harpon : la baliste au niveau 4, et il ne cloue qu'1 s |
| Pris au filet | « Au filet ! » | ×1,5 | un volant au sol, immobile, 1,5 à 3 s |
| Étourdi | « Sonné ! » | ×1,5 | de courts moments : la herse toutes les 8 s, le filet électrique, le relevé d'une chute |
| Englué | « Englué ! » | ×1,3 | la flaque dure plusieurs secondes, et l'ennemi ralenti est plus facile à toucher |
| Attiré | « Attiré ! » | ×1,25 | l'aimant attire tout seul, en continu dès le niveau 3 |
| Marqué | « Marqué ! » | ×1,15 | le phare marque tout son rayon, sans rien faire |

Les points restent ×1,5 pour toutes les sortes.

### 12.13 Les duos, premier lot (31 octobre)

*Remplacé le jour même par la nouvelle formule (12.14) : le système de fusion reste ; les deux tours gardées dans le duo, leurs
niveaux et les 4 effets ci-dessous partent.*

Le joueur passe aux fusions avant les lots 4 à 6. Ses choix : **un modèle par duo dès le début** (plutôt qu'une allure
commune retouchée ensuite), et **le système avec 4 duos** qui couvrent tous les cas (bord + bord, voie + bord, voie + voie, le
phare et l'arc), à tester avant les 9 autres. Codé, compilé, **pas encore testé** (tests : `Checklist_Demo.md`).

**Le système** (les règles de 12.9) :
- **Fusionner** : entre les vagues, la carte « Déplacer » d'une tour, puis viser une tour partenaire. L'hologramme devient
  **doré** et montre le duo, chaque tour à son niveau, à la place qu'il prendra, avec son nom (« Duo : Canon ») ; s'il manque
  quelque chose, il est rouge et dit quoi (« Les deux tours au niveau 3 au moins », « Déjà dans un duo »…). La gâchette
  fusionne : « Duo : Canon ! », avec ce que fait la fusion.
- **Où** : à la place de la tour visée ; une tour de la voie et une du bord, sur la voie. Le duo prend les cases de la tour du
  dessous (celle dont son prefab part) ; s'il n'y a pas la place, la fusion est refusée.
- **Chaque moitié garde son niveau**, et monte avec ses propres plans jusqu'au niveau 5 : en visant le duo avec un plan de la
  montre, ou dans son panneau, qui a trois cartes (une par tour, et « Déplacer » ; la troisième carte est faite au démarrage).
  Pendant l'amélioration d'une moitié, seule elle passe en plan bleu.
- **Le spectacle** : quand les deux tours sont au niveau 5, l'effet de fusion grandit.
- On ne défait pas un duo ; il se déplace entre les vagues comme une tour. Une fusion ne coûte rien de plus : son prix, ce sont
  les plans des deux tours (6 pour deux tours au niveau 3).

**Les 4 duos** (les réglages sont sur l'effet de chaque prefab, dans `Prefabs/Structures/Duos`) :

| Duo | Le modèle | L'effet de fusion | Le spectacle (5 et 5) |
|---|---|---|---|
| **Canon** (Baliste + Mortier) | le bastion du mortier ; la baliste, à moitié de sa taille, sur une plate-forme de pierre au fond du toit, liseré doré ; un fût de bronze cerclé d'or sur l'affût du mortier | toutes les 4 s, le fût tire où regarde le mortier (35° de part et d'autre, 5 à 34 m) un boulet tendu (40 m/s) qui explose à l'impact : 70 de dégâts au centre (vague 1, Normal), 3 m, volants compris | une salve de trois boulets, ×1,5, 4,5 m, qui projettent en l'air les ennemis au sol |
| **Mur de glace** (Barricade + Tour de givre) | la tour de givre, à 55 %, derrière le mur (côté joueur) sur un disque doré ; de la glace et des glaçons sur la palissade ; du verglas devant | toutes les 2,5 s, les assaillants à 3 m devant le mur gèlent 4 s ; le coup qui brise la glace leur coûte la moitié de leurs PV max (ta flèche : « Brisé ! », ×2) | toutes les 1,5 s, 6 s, boss compris ; la glace brisée les fait voler en éclats (les boss : 10 % de leurs PV) |
| **Rempart à ressort** (Barricade + Tremplin) | le tremplin, à 65 %, devant le mur (côté forêt), là où arrivent les assaillants ; derrière le mur, deux gros ressorts de cuivre sur des butées cerclées d'or | chaque ennemi qui frappe le mur repart vers la forêt, 10 m en arrière, en vol (« Au vol ! », ×2) ; les ressorts se tassent | 20 m, bien plus haut, et tous les assaillants du mur avec lui |
| **Aurore boréale** (Phare + Tour de givre) | la tour de givre, à 55 %, au pied du phare sur un disque doré ; de la glace autour du phare ; deux rideaux d'aurore (vert, violet) qui ondulent autour de lui, faits par le code | le faisceau ralentit ce qu'il éclaire (de moitié) et gèle ce qu'il a éclairé 1 s en tout (2,5 s ; un quart des PV au bris) ; ta flèche qui traverse l'aurore devient une flèche de glace : sa cible gèle 3 s | l'aurore s'élargit (×1,6) ; le faisceau gèle d'un coup, plus fort |

Les ressorts, la glace sur la palissade et les glaçons sont accrochés au modèle intact de la barricade : ils disparaissent avec
lui quand elle tombe (ses débris restent).

**Comment c'est fait** :
- `Building/TowerDuo.cs` (un duo bâti : ses deux tours, le spectacle ; les effets en héritent : `DuoCannon`, `DuoIceWall`,
  `DuoSpringWall`, `DuoAurora`), `Building/TowerDuoCatalog.cs` (le catalogue, dans `Resources/TowerDuos`) ;
  `BuildManager.DuoOf` et `Fuse`, `BuildPlacement` (l'hologramme doré), `BuildPanel` (les trois cartes), `BuildGrid.EvaluateInPlace`
  et `Reclaim` (les cases du duo) ; `Structure.Owns` (le plan bleu, la surcharge et l'emprise d'une tour ne prennent pas l'autre
  tour imbriquée).
- **Le prefab d'un duo** est une variante de celui de la tour du dessous, l'autre tour imbriquée dedans : ce qui change sur une
  tour (son allure, ses réglages) passe dans ses duos. On peut retoucher un duo à la main dans son prefab.
- Sa composition est dans `Editor/TowerDefenseLook.json` (partie « duos »), écrite par `Tools/allure_tours/duos.py` (via
  `conception.py`) ; `Editor/TowerDefenseLookBaker` fait le prefab une fois, puis ses pièces, son effet et sa palette (marque
  « Duo du 31 octobre »), et l'inscrit au catalogue (ses textes changés à la main restent). Les outils dessinent un duo à
  chaque couple de niveaux : `apercu.py … "Duo:Canon" … "3-3,5-5"`, et `faces_superposees.py` (aucune face confondue, aux
  niveaux 3-3, 3-5, 5-3 et 5-5).
- **À surveiller aux tests** : la portée et la cadence du canon ; le Mur de glace contre une vague dense (6 statues au plus à
  chaque gel) ; le Rempart à ressort qui renvoie les ennemis au milieu d'un autre bâtiment ; la lisibilité de l'aurore de loin.

### 12.14 Les duos, nouvelle formule (proposés par Claude, 31 octobre, à trier)

*Les règles restent ; les 13 duos sont remplacés par la deuxième proposition (12.15), plus originale.*

**Les règles** (décidées par le joueur) : un duo est **une tour nouvelle**, plus forte que les deux tours au niveau 3 qu'elle
remplace (elle coûte leurs plans et ne prend qu'une place) ; une seule attaque, lisible de loin, et son modèle à elle. Ses
**3 niveaux** se montent avec les plans de l'une ou l'autre de ses deux tours (deux plans par niveau, proposé) ; elle naît au
niveau 1 (deux tours au niveau 3), 2 (l'une au niveau 5) ou 3 (les deux). Le niveau 3 est le **spectacle**, comme les niveaux 5.
Comme partout, les boss ne sont ni projetés ni tués d'un coup : une grosse part de leurs PV à la place.

**Les paires** : 10 des 13 paires de 12.9 restent (certaines redessinées), 3 changent. Chaque tour entre dans 2 ou 3 duos.
Retirées : Point de chute (mortier + aimant), Filet aimanté (filet + aimant), Bulle piégée (bulle + mortier) ; nouvelles :
Pêcheur de nuages (baliste + filet), Montgolfière (aimant + bulle), Manège (tremplin + aimant).

| Duo | Tours | Où | Niveau 1 : l'attaque | Niveau 2 | Niveau 3 : le spectacle | Allure |
|---|---|---|---|---|---|---|
| **Canon** | Baliste + Mortier | bord | toutes les 3 s, un boulet tendu qui traverse la file en renversant les ennemis (les quilles) et explose au bout, volants compris | le boulet ricoche au sol et explose deux fois | **Bordée** : quatre canons sortent des créneaux, une salve de huit boulets balaie toute la voie | un bastion trapu, un grand canon de bronze cerclé d'or sur un affût tournant |
| **Canon humain** | Tremplin + Mortier | voie | avale l'ennemi qui passe devant sa bouche et le tire en cloche sur le groupe le plus serré, où il explose ; touché en vol par ta flèche, il explose en l'air, deux fois plus fort (« Au vol ! ») | deux ennemis d'un coup | **Homme-obus géant** : il avale les gros (pas les boss), qui font un cratère et renversent tout autour | un canon de cirque rayé rouge et or, gueule vers le ciel |
| **Grêle** | Mortier + Tour de givre | bord | des obus de glace qui éclatent en grêlons sur 4 m : dégâts, et ils gèlent 2 s (« Brisé ! ») | là où ils tombent, le verglas : les ennemis glissent et tombent | **Orage de grêle** : 6 s de grêle sur toute une voie, tout gèle | un mortier de glace bleutée, tubes givrés, cristaux sur le bastion |
| **Mur de glace** | Barricade + Tour de givre | voie | qui frappe le mur gèle en statue (ta flèche la brise : « Brisé ! ») ; le mur se reforme seul | une statue brisée lance des éclats qui gèlent ses voisins | **Banquise** : toutes les 15 s, le mur éclate en pics de glace sur 15 m devant lui, toute la voie gèle, puis il se reforme | un mur de glace translucide entre deux piliers de pierre givrés |
| **Rempart à ressort** | Barricade + Tremplin | voie | qui frappe le mur est renvoyé 15 m en vol vers la forêt (« Au vol ! ») | renvoyé sur ceux qui suivent (les quilles) | **Détente** : toutes les 12 s, le mur se tasse puis se détend ; tout ce qui est à 6 m devant lui part à 40 m, l'atterrissage tue (comme la rampe) | une palissade montée sur d'énormes ressorts de cuivre, plaques dorées |
| **Broyeur** | Piques + Tour-aimant | voie | attire les ennemis à 8 m sur une meule de pieux qui tourne au ras du sol et les broie (gros dégâts continus, ils y restent ralentis) | 12 m, et les volants qui passent bas | **Hachoir** : la meule se dresse et aspire tout à 20 m pendant 4 s ; les petits sont broyés net | une meule hérissée entre deux aimants en fer à cheval |
| **Chute fatale** | Piques + Bulle | voie | une bulle enlève l'ennemi qui passe et le hisse à 8 m au-dessus d'une fosse à pieux ; elle crève au bout de 3 s, ou sous ta flèche (« Au vol ! ») : il s'empale (les petits meurent net, les autres sont cloués 2 s) | deux bulles à la fois | une grappe de bulles hisse tout un groupe à 20 m : crevée, c'est une pluie d'ennemis sur les pieux | une fosse hérissée sous une arche d'où pendent des bulles |
| **Pêcheur de nuages** | Baliste + Lance-filet | bord | harponne un volant et le ramène au sol au treuil, où il reste ficelé 4 s (« Au filet ! ») ; un ennemi au sol harponné est tiré 6 m en arrière | deux harpons | **Grand chalut** : un filet géant, tiré au harpon, ramasse tous les volants d'une zone et les écrase au sol | une arbalète à treuil sur un mât de navire, cordages et filets |
| **Aurore boréale** | Phare + Tour de givre | bord | le faisceau ralentit et gèle ce qu'il éclaire ; ta flèche qui traverse l'aurore devient une flèche de glace (sa cible gèle) ; garde le soutien du phare (+20 % de dégâts aux bâtiments du rayon) | l'aurore s'élargit ; une statue brisée par une flèche de glace éclate et blesse autour | **Nuit polaire** : la lumière s'éteint d'un coup, puis tout le rayon gèle | un phare de glace, lanterne verte, l'aurore qui ondule autour |
| **Mire** | Phare + Baliste | bord | le projecteur se pose sur l'ennemi le plus fort et le marque (« Marqué ! ») ; toutes les 3 s, un carreau lourd dans son point faible | tes flèches qui passent près de lui sont courbées vers son point faible | le projecteur suit trois cibles à la fois, et le carreau les traverse toutes | une tour fine coiffée d'une grande arbalète à lunette et d'un projecteur |
| **Montgolfière** | Tour-aimant + Bulle | bord | toutes les 8 s, l'aimant rassemble les ennemis proches dans une grosse bulle que le ballon remorque 15 m vers la forêt avant de la lâcher (ils perdent du chemin, et prennent la chute) ; ta flèche la crève plus tôt, plus haut | la bulle monte plus haut et retombe sur ceux qui suivent | **Dirigeable** : il emporte tout un groupe très haut et le lâche sur un autre ; l'atterrissage tue | un ballon rayé, retenu par des chaînes à un treuil aimanté |
| **Roncier** | Piques + Lance-filet | voie | les ennemis qui le traversent s'empêtrent : retenus 1,5 s (« Au filet ! »), ils saignent ; les volants qui passent bas y restent pris | plus large, et retenus plus longtemps à chaque passage | **Forêt de ronces** : toutes les 15 s, des ronces jaillissent sur 20 m de voie et ligotent tout 4 s | des ronces de fer et des filets barbelés tendus entre des piquets |
| **Manège** | Tremplin + Tour-aimant | voie | attire sur son plateau les ennemis à 6 m, les fait tourner et les éjecte sur les côtés, hors de la voie (ils retombent loin et doivent revenir) | les éjectés renversent les autres (les quilles) | **Grand huit** : 5 s à toute vitesse, il éjecte tout à 30 m ; l'atterrissage tue | un carrousel de fête foraine, chevaux de bois, mâts dorés |

**Ce qu'ils couvrent** : contre les volants (Canon, Pêcheur de nuages, Roncier, Broyeur au niveau 2) ; pour le joueur et ses
flèches (Aurore boréale, Mire, et les ennemis préparés de presque tous) ; tenir la voie (Mur de glace, Rempart à ressort) ;
pièges de voie (Broyeur, Roncier, Chute fatale, Manège, Canon humain) ; artillerie du bord (Canon, Grêle, Montgolfière).

**Les trios et les ultimes** (12.9) seront à revoir ensuite : un trio devient un duo plus une tour.

**À trier par le joueur** : garder, retirer ou changer chaque duo ; puis, pour le code, refaire le système (une tour nouvelle à
la place des deux) et les 4 premiers.

### 12.15 Les duos, deuxième proposition (Claude, 31 octobre, à trier)

Le joueur : les duos de 12.14 ne sont « pas originaux du tout ». Son exemple, **barricade + tremplin** : on ne garde que le
boulet qui tourne, beaucoup plus vite, en tours complets ; il devient rebondissant et envoie les ennemis qu'il touche loin sur
le côté, **jusque sur d'autres voies**. Claude en tire la règle de cette proposition : **chaque duo garde un seul élément de
ses tours, le pousse à l'extrême, et apporte une mécanique qu'aucune autre tour n'a** (changer un ennemi de voie, l'enfoncer
dans le sol, le faire reculer, le retourner contre les siens, lui grossir la tête, recycler tes flèches ratées…). Les règles de
12.14 restent (une tour nouvelle, 3 niveaux à elle, plus forte que les deux tours qu'elle remplace ; les boss ni projetés ni
tués d'un coup).

Chaque tour entre dans 2 ou 3 duos (26 places pour 13 duos). « À coder » : l'effort, pour choisir l'ordre.

| Duo | Tours | Où | Niveau 1 : l'idée | Niveau 2 | Niveau 3 : le spectacle | Allure | À coder |
|---|---|---|---|---|---|---|---|
| **Boulet fou** *(l'idée du joueur)* | Barricade + Tremplin | voie | le boulet hérissé, au bout d'un bras, fait des tours complets à toute vitesse ; rebondissant, il envoie chaque ennemi touché loin sur le côté, jusque sur une autre voie (vers la mieux défendue, ou la plus longue) | il rebondit de l'ennemi touché vers le suivant, trois fois | un bras double, deux boulets ; les ennemis lancés retombent en météores sur l'autre voie | un mât, un bras qui tourne, le boulet rayé d'or | moyen |
| **Marteau-pilon** | Barricade + Piques | voie | toutes les 4 s, un marteau géant enfonce l'ennemi visé dans le sol jusqu'au cou : planté 4 s, seule sa tête dépasse (« Planté ! » : un tir à la tête facile) | l'onde du coup fait tomber tout autour | trois marteaux en cadence, comme une forge, sur toute la largeur de la voie | un portique de forge, un marteau de pierre cerclé de fer | moyen |
| **Toboggan** | Tremplin + Tour de givre | voie | 15 m de voie deviennent une piste de glace en pente vers la forêt : les ennemis y glissent en arrière en tournoyant, ils avancent à peine | au bout de la piste, un petit tremplin de glace les fait décoller (« Au vol ! ») | **Saut à ski** : un grand tremplin les envoie à 40 m, l'atterrissage tue | une piste bleue bordée de bottes de paille, un tremplin de glace | moyen |
| **Tapis roulant** | Tremplin + Tour-aimant | voie | un tapis magnétique de 12 m qui recule : les ennemis marchent sur place, des cibles immobiles ; les volants qui passent bas y sont aspirés | il recule plus vite que les plus rapides | il s'emballe et jette tout le monde en tas au bout, dans la forêt (les quilles) | des lattes de bois entre deux rouleaux de cuivre, des aimants | simple |
| **Avalanche** | Mortier + Tour de givre | bord | le mortier tire une boule de neige qui roule sur la voie vers la forêt, grossit et avale les ennemis qu'elle touche (gelés dedans) ; au bout, ou sous ta flèche, elle éclate et tous volent en éclats | elle roule plus loin et avale aussi les gros | cinq boules à la fois sur toute une voie | un mortier en glace, la gueule pleine de neige | moyen |
| **Feu d'artifice** | Mortier + Phare | bord | des fusées éclatent en bouquets au-dessus des ennemis : éblouis, ils s'arrêtent 2 s (« Sonné ! ») sous une pluie d'étincelles qui brûle ; ta flèche qui traverse un bouquet s'enflamme | des fusées à deux étages, deux bouquets | **Bouquet final** : vingt fusées, tout le ciel s'allume, toute une voie éblouie et brûlée | une rampe de fusées aux pavillons rayés, la lanterne du phare en mèche | simple |
| **Chant des sirènes** | Phare + Bulle | bord | le faisceau hypnotise un ennemi : enfermé dans une bulle rose, il fait demi-tour 6 s et renverse et blesse ceux qu'il croise | deux à la fois ; à la fin il éclate en bulles qui en hypnotisent d'autres | tout le rayon change de camp 5 s : une mêlée géante | un phare de nacre, une sirène de bronze au sommet | lourd |
| **Loupe** | Phare + Baliste | bord | une loupe grande comme une porte se tourne vers les ennemis : dans son champ, ils ont la tête trois fois plus grosse (le point faible aussi) ; les tirs à la tête deviennent faciles | la baliste plante ses carreaux dans les têtes géantes | **Fourmi** : la loupe concentre le soleil en un rayon sur l'ennemi le plus fort, qui fume puis prend feu (un boss : une grosse part de ses PV) | une loupe de laiton sur une tour de bois | moyen |
| **Carquois géant** | Baliste + Lance-filet | bord | un grand filet rattrape tes flèches ratées (dans son rayon) ; à 10 flèches, la baliste te les renvoie en une volée sur le groupe le plus serré | les flèches renvoyées gardent leurs pièces (feu, glace, explosion…) | **Nuée** : tout ce que tu as raté pendant la vague repart d'un coup, une nuée qui obscurcit le ciel | une arbalète à six arcs devant un filet tendu comme une voile | moyen |
| **Chaîne de forçats** | Tour-aimant + Lance-filet | bord | des chaînes aimantées lient jusqu'à 5 ennemis proches : ils avancent ensemble, plus lentement, et chaque coup sur l'un blesse tous les autres (ta flèche à la tête d'un seul : toute la chaîne) | 8 ennemis ; un lié qui meurt fait tomber les autres | la chaîne se resserre d'un coup et les écrase les uns contre les autres | une tour de fer, des bobines de chaîne, un gros aimant rouge | moyen |
| **L'Araignée** | Piques + Lance-filet | voie | une toile en travers de la voie ; une araignée de fer aux pattes en pieux y court et enroule les ennemis pris dans des cocons qu'elle suspend (« Au filet ! ») ; ta flèche crève un cocon | elle mord : les cocons empoisonnent | trois araignées, la toile sur toute la largeur, volants compris | la toile entre deux arbres morts, l'araignée de fer aux yeux rouges | lourd |
| **Baudruche** | Mortier + Bulle | bord | le mortier tire des poupées gonflables à ton image au milieu des voies : les ennemis s'arrêtent pour les frapper ; crevées, elles éclatent en confettis piquants (ta flèche peut les crever avant) | frappée, elle rebondit plus loin et les entraîne hors de leur chemin | une baudruche géante que tous les ennemis de la voie viennent frapper | un mortier peint en couleurs de fête, des baudruches attachées comme des ballons de foire | simple |
| **Boule à neige** | Bulle + Tour de givre | bord | une boule de verre géante descend sur un groupe et l'enferme ; il y neige, les ennemis s'y figent peu à peu en figurines ; ta flèche brise la boule : toutes les figurines éclatent | la boule se secoue : ils tourbillonnent et se heurtent | une boule immense sur toute une voie | un socle de bois verni, une boule de verre et de la neige | moyen |

**À trier par le joueur**, comme les évolutions : garder, retirer, changer, ou remplacer par ses propres idées. Puis l'ordre du
code (le système de fusion refait pour une tour nouvelle, puis les duos un par un).

**Le tri du joueur (31 octobre)** :
- **Gardés** : Boulet fou, Marteau-pilon, Avalanche, Feu d'artifice, Chant des sirènes (« même si bonjour les bugs »), Loupe,
  Carquois géant, Chaîne de forçats, Boule à neige.
- **Toboggan** : gardé, mais l'équilibrage et l'effet « wow » sont à voir.
- **Tapis roulant** : fait écho au toboggan ; à remplacer.
- **L'Araignée** : le joueur a du mal à en voir le rendu ; à remplacer.
- **Baudruche** : bonne idée, mais ce doit être une tour qui lance les baudruches (un lanceur), pas un mortier qui les tire.

**Les nouvelles propositions de Claude (à trier)** :

| Pour | Duo | Tours | Niveau 1 | Niveau 2 | Niveau 3 : le spectacle | Allure |
|---|---|---|---|---|---|---|
| le Tapis roulant | **Yo-yo** *(conseillé)* | Tremplin + Tour-aimant | une grue au-dessus de la voie : son aimant, au bout d'un fil, attrape un ennemi, le hisse à 6 m et le fait claquer au sol trois fois, comme un yo-yo (des dégâts à chaque fois ; en haut, « Au vol ! »), puis le lâche | il le lâche en le lançant sur le groupe (les quilles) | **Lustre** : un plateau tournant à six fils, six ennemis qui montent et descendent en cadence | une grue de bois, un gros yo-yo de cuivre aimanté |
| le Tapis roulant | **Pogo** | Tremplin + Tour-aimant | des ressorts aimantés se collent aux pieds des ennemis : ils sautillent sur place, de plus en plus haut, sans avancer (« Au vol ! » à chaque bond) | le dernier bond les envoie très haut | toute la voie sautille | une tour à ressorts, des bottes à ressort accrochées comme des trophées |
| l'Araignée | **Épervier** *(conseillé)* | Piques + Lance-filet | un mât de pêcheur lance un épervier (un filet rond lesté de crochets) sur le groupe le plus serré : plaqués au sol dessous 3 s (« Au filet ! »), chaque mouvement les blesse | les crochets restent : le sol reste hérissé 6 s | **Grand épervier** : un filet de 15 m qui couvre toute la voie | un mât de pêcheur à bras tournant, le filet rond plié, des plombs dorés |
| l'Araignée | **Boussole folle** | Piques + Tour-aimant | une rose des vents géante au sol, hérissée de pointes ; son aiguille s'affole : les ennemis qui y entrent perdent le nord et tournent en rond 3 s sur les pointes | ils repartent dans le mauvais sens | toute la voie tourne en rond | une boussole de laiton grande comme une place, l'aiguille rouge qui tourne |

**Le Toboggan, revu pour l'effet et l'équilibrage** : les ennemis glissent sur le ventre en tournoyant et s'entrechoquent ; au
bout, un tremplin les fait décoller en vrille, au ralenti, et des notes de jury s'affichent au-dessus d'eux (« 9,5 ! ») : le
tir au vol, en spectacle. Pour l'équilibre : **une seule glissade par ennemi** (il en ressort sonné), environ 4 s de perdues,
pas plus ; la voie ne se bloque jamais. Le spectacle ne change pas (le saut à ski, l'atterrissage tue).

**Le lanceur de baudruches** (Mortier + Bulle, bord) : une grosse pompe de foire en cuivre, un canon à air, des baudruches
attachées comme des ballons. Il gonfle des poupées à ton image et les lance au milieu des voies ; les ennemis s'arrêtent pour
les frapper ; crevées, elles éclatent en confettis piquants (ta flèche peut les crever avant). Niveau 2 : frappée, elle rebondit
plus loin et les entraîne hors de leur chemin ; niveau 3 : un char de carnaval gonflable que tous les ennemis de la voie
viennent frapper.

**Le nombre de duos par tour**, avec les deux conseillés (Yo-yo, Épervier) :

| Tour | Duos | Nombre |
|---|---|---|
| Barricade | Boulet fou, Marteau-pilon | 2 |
| Piques | Marteau-pilon, Épervier | 2 |
| Tremplin | Boulet fou, Toboggan, Yo-yo | 3 |
| Baliste | Loupe, Carquois géant | 2 |
| Mortier | Avalanche, Feu d'artifice, Lanceur de baudruches | 3 |
| Tour de givre | Toboggan, Avalanche, Boule à neige | 3 |
| Lance-filet | Carquois géant, Chaîne de forçats, Épervier | 3 |
| Tour-aimant | Chaîne de forçats, Yo-yo | 2 |
| Bulle | Chant des sirènes, Boule à neige, Lanceur de baudruches | 3 |
| Phare | Feu d'artifice, Chant des sirènes, Loupe | 3 |

Avec la Boussole folle à la place de l'Épervier : Piques 2, Tour-aimant 3, Lance-filet 2. Avec le Pogo à la place du Yo-yo :
rien ne change.

**Décidé (31 octobre)** : le **Yo-yo** et la **Boussole folle** (pas le Pogo ni l'Épervier) ; le **Toboggan revu** (vrilles,
notes du jury, une glissade par ennemi) ; le **lanceur de baudruches**. Les 13 duos sont donc : Boulet fou, Marteau-pilon,
Toboggan, Yo-yo, Avalanche, Feu d'artifice, Chant des sirènes, Loupe, Carquois géant, Chaîne de forçats, Boussole folle,
Lanceur de baudruches, Boule à neige.

| Tour | Duos | Nombre |
|---|---|---|
| Barricade | Boulet fou, Marteau-pilon | 2 |
| Piques | Marteau-pilon, Boussole folle | 2 |
| Tremplin | Boulet fou, Toboggan, Yo-yo | 3 |
| Baliste | Loupe, Carquois géant | 2 |
| Mortier | Avalanche, Feu d'artifice, Lanceur de baudruches | 3 |
| Tour de givre | Toboggan, Avalanche, Boule à neige | 3 |
| Lance-filet | Carquois géant, Chaîne de forçats | 2 |
| Tour-aimant | Chaîne de forçats, Yo-yo, Boussole folle | 3 |
| Bulle | Chant des sirènes, Boule à neige, Lanceur de baudruches | 3 |
| Phare | Feu d'artifice, Chant des sirènes, Loupe | 3 |

**Le prix des niveaux d'un duo (proposé par Claude, accepté par le joueur le 31 octobre)** : 4 plans par niveau (de l'une ou l'autre de ses tours), au lieu de 2.
Ainsi un duo coûte autant par les deux chemins : au niveau 1, 6 plans (deux tours au niveau 3) ; au niveau 2, 10 (6 + 4, ou une
tour au niveau 5 et une au niveau 3 : 7 + 3) ; au niveau 3, 14 (10 + 4, ou deux tours au niveau 5 : 7 + 7). Avec 2 plans par
niveau, fusionner tôt puis monter le duo coûtait 10 plans au lieu de 14 : les niveaux 4 et 5 des tours ne servaient plus à rien
avant une fusion.

**Combien par partie** : une partie donne une trentaine à une quarantaine de plans (12.9). Un duo coûte 6 plans au niveau 1,
10 au niveau 2, 14 au niveau 3 : de quoi faire **2 ou 3 duos** en gardant des tours seules, ou 2 duos au niveau 3. Les plans
venant des caisses, par lots d'une tour au hasard, ce sont les caisses qui disent quels duos on peut viser.

### 12.16 Les duos dans le code (31 octobre au soir)

Codé, compilé, **pas encore vu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`).

**Le système** :
- Un duo est une **tour** à part entière : `Building/TowerDuo.cs` (une `Structure` : son entrée du catalogue, les deux tours dont
  il est né et dont les plans le font monter, son spectacle au niveau 3), et une classe par duo qui en hérite
  (`DuoBouletFou`…). Sa **définition** est dans `Data/Duos` (sorte « Duo », ses 3 niveaux : leurs plans, 4, et leurs textes ;
  sa place ; ses cases) ; le **catalogue** `Resources/TowerDuos` dit quelles deux tours le font.
- **Fusionner** : la carte « Déplacer » d'une tour, puis viser une tour partenaire ; l'hologramme doré montre le duo au niveau
  où il naîtra (`BuildManager.DuoLevelFrom` : 1, 2 ou 3), à la place qu'il prendra (`BuildManager.DuoPlace` : la tour visée ;
  sur la voie si l'une des deux y est) ; la gâchette appelle `BuildManager.Fuse`. Le duo prend ses cases à lui
  (`BuildGrid.EvaluateInPlace`, `Reclaim`) ; s'il n'y a pas la place, la fusion est refusée.
- **Monter un duo** : 4 plans par niveau, de l'une ou l'autre de ses deux tours (d'abord celle du plan sorti de la montre),
  dans son panneau ou en le visant avec un plan de la montre.
- Un duo ne fusionne plus (« Déjà un duo ») : les trios viendront ensuite.
- Le code des duos à deux moitiés (12.13 : la tour imbriquée, le panneau à trois cartes, les quatre effets) est retiré, avec
  leurs 4 prefabs, leurs matériaux et l'ancien catalogue (des fichiers faits par le baker le jour même).

**Le Boulet fou** (Barricade + Tremplin, l'idée du joueur ; `Building/DuoBouletFou.cs`) :
- Le modèle : deux chevalets en A de part et d'autre du plan de la roue (comme une grande roue), un essieu à 3,45 m aux
  embouts dorés, un bras qui tourne et le boulet hérissé, noir cerclé d'or, à 2,2 m de l'essieu : il passe à 1,25 m du sol.
  Niveau 2 : un ressort de cuivre sur le bras et un collier de pointes dorées ; niveau 3 : un second bras et un second boulet.
  Ses cases : 4 × 2 (celles de la barricade) ; les chevalets débordent un peu sur les bords de la voie.
- La roue tourne en travers de la voie, **du côté de la voie voisine la plus proche** (revu toutes les 2 s : la tour a pu être
  déplacée). Un tour en 1,6 s (1,25 s au niveau 2, 1,1 s au niveau 3).
- Chaque ennemi frappé (une fois par 1,2 s) prend 25 / 35 / 45 de dégâts (vague 1, Normal), et part en cloche
  (`Enemy.Launch`) vers **le point le plus proche d'une autre voie**, à 4 m au moins et 35 m au plus, de préférence du côté
  où tourne la roue ; sans autre voie à portée, 16 m sur le côté. Il repart vers la tour par cette voie. Boss poussés
  seulement ; volants et mannequins pas touchés.
- Niveau 2 : à l'atterrissage, l'ennemi lancé renverse ceux qui sont à 3 m. Niveau 3, les **météores** : ils sont aussi
  blessés (40 au centre, la moitié au bord).
- Les sons : le souffle du boulet au plus bas (le *Lightsaber Whoosh* de la faille lancée, plus grave), un choc de Kenney à
  chaque coup ; le boulet s'écrase un instant à chaque coup (rebondissant).
- **À surveiller aux tests** : le choix de la voie voisine sur la carte (et l'ennemi qui ne retrouverait pas son chemin) ; la
  cadence et la portée du lancer ; ce que font les ennemis lancés sur une voie déjà défendue.

**Après le premier essai du Boulet fou (31 octobre au soir)** — le joueur : la tour doit « avoir de la gueule », projeter plus
loin aux niveaux 1 et 2, et son niveau 3 doit être « du même niveau que le tremplin » : l'ennemi projeté jusqu'au mur
opposé, à toute vitesse, en prenant de gros dégâts. Fait (pas encore retesté) :
- **Plus grand**, une fois et demie : l'essieu à 5,2 m, le bras de 3,9 m, un boulet de 1,6 m (il passe toujours à 1,3 m du
  sol) ; des pieds de pierre plaqués d'or, une traverse par chevalet, un fanion doré au-dessus de chacun ; au niveau 3, le
  second boulet monte à 10 m. Ses cases restent 4 × 2 (la tour déborde sur les bords de la voie).
- **Plus loin** : l'ennemi retombe sur la voie voisine jusqu'à 50 m (65 au niveau 2), et 10 m (18) **en arrière** sur elle, vers
  la forêt : il y perd aussi du chemin ; sans voie à portée, 28 m (38) sur le côté. Des cloches plus hautes (1,4 à 3,2 s de vol).
- **Le boulet de canon** (niveau 3, à la place des météores) : l'ennemi frappé part à plat à 38 m/s, presque sans pesanteur,
  jusqu'au mur, à la falaise ou au décor d'en face ; le choc lui coûte **60 % de ses PV max** (le sol, s'il ne heurte rien), et
  blesse (40 au centre) et renverse ceux qui sont à 3 m, avec une petite explosion. C'est `Enemy.SetSmash` et l'événement
  `Enemy.Smashed` (le choc d'un vol, contre le décor ou au sol), que d'autres tours pourront reprendre.
- Le prefab déjà fait est refait par une deuxième passe du baker (marque « Duo 2 du 31 octobre », `rebuild` : son modèle
  repart de zéro) ; les textes des niveaux de `Data/Duos/Duo_Boulet_fou` sont revus s'ils n'ont pas été changés à la main.
