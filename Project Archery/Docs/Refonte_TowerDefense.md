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

**31 octobre (nuit) : neuf duos de plus et les flèches « DUO »** (12.17). Après son essai du Boulet fou, le joueur fixe la règle
de tous les duos : impressionnants (« il doit avoir de la gueule »), et un **niveau 3 dans la démesure**. Il demande les
10 premiers de la liste pendant la nuit : Marteau-pilon, Toboggan, Yo-yo, Avalanche, Feu d'artifice, Chant des sirènes, Loupe,
Carquois géant, Chaîne de forçats (le Boulet fou était fait). Et une grande flèche dorée « DUO » au-dessus de deux tours qui
peuvent fusionner, seulement entre les vagues. Codés et compilés, modèles vérifiés hors de Unity (rendus, faces superposées) ;
**rien n'est vu dans Unity ni testé au casque**, rien de commité. Restent : Boussole folle, Lanceur de baudruches, Boule à neige.

**31 octobre : la vitrine, F4** (demandée par le joueur, pour voir chaque tour à chaque niveau sans plans ni marteau ;
`Scripts/Building/BuildShowcase.cs`, éditeur et builds de développement). Un appui pose une tour : sur la voie, sur les trois
voies vers 16 m de la tour (niveaux 3, 4, 5 de gauche à droite) ; hors de la voie, au centre des clairières de droite (3) et de
gauche (4), à mi-chemin entre la voie du milieu et sa voisine, vers 20 m de la tour ; une tour de la tour, sur deux emplacements
de la tour. L'appui suivant retire ce que la vitrine a posé (jamais les tours du joueur) et pose la suivante, dans l'ordre du menu ;
après les 10 tours, les duos du catalogue aux niveaux 1, 2, 3 (1 et 2 dans les clairières) ; après le dernier, tout est retiré ;
Maj + F4 revient en arrière. Chaque tour prend la place libre qui lui convient la plus proche du point visé. Compilé ; essayée
par le joueur le soir même.

**31 octobre au soir : les trois derniers duos, le Toboggan revu, les premiers bugs** (12.18). Le joueur a essayé les duos
(vitrine) : le Toboggan est à revoir en entier, la boule de l'Avalanche bugue (ennemis qui sautent, le pont du milieu), la Chaîne
de forçats fait planter le jeu (deux chaînes se renvoyaient les dégâts). Corrigés ; le Toboggan devient un grand toboggan qui
enjambe la voie (l'échelle, la piste en l'air, le saut) ; Boussole folle, Lanceur de baudruches et Boule à neige codés : les
13 duos sont faits. Compilés, modèles vérifiés hors de Unity ; **pas encore vus dans Unity ni testés au casque**. Ensuite : les
trios et les ultimes.

**1er novembre : les trios, triés** (12.19). Un trio devient un duo plus une tour (même formule que les duos) ; sur 9
propositions de Claude, le joueur garde **7 trios** : Bowling, Beffroi, Montagnes russes, Gonfleur, Volcan, Enclume, Colisée.
La règle de fusion et de prix (9, 13 ou 17 plans) est proposée, pas encore validée. Les essais des trois derniers duos sont
remis à plus tard. Puis, le même jour, **le système des trios et l'Enclume codés** (12.20) : compilés, modèle vérifié hors de
Unity, **pas encore vus dans Unity ni testés au casque**, rien de commité. Puis le **Gonfleur** (12.21), demandé par le
joueur avant d'essayer l'Enclume : compilé, modèle vérifié hors de Unity, pas encore vu dans Unity. Puis le **Bowling** (12.22)
et le **Volcan** (12.23), de même : les quatre trios « simples » et « moyens » sont faits. Puis le **Beffroi** (12.24), le
premier des lourds, le **Colisée** (12.25) et les **Montagnes russes** (12.26) : **les 7 trios sont codés**, compilés, leurs
modèles vérifiés hors de Unity ; **aucun n'est encore vu dans Unity ni testé au casque**. Ensuite : les essais du joueur (F4,
`Checklist_Demo.md`, « Les trios »), puis les ultimes (à revoir avec la formule des duos et des trios).

**1er novembre au soir : le premier essai des trios** (12.27). Corrigés : les baudruches attirent aussi les volants et les tireurs ;
un tireur charmé ne s'arrête plus à 17 m de son but (il tire maintenant sur la baudruche, ses rivaux de l'arène, ses proies) ; les
champions de deux Colisées ne se battent plus entre eux ; le grand champion est en armure dorée ; F4 pose une défense aérienne et
donne 20 000 PV au joueur et à la tour. Compilés, **pas encore revus dans Unity**. **Le Volcan et les Montagnes russes sont à
refaire** : trois propositions pour chacun, à trier par le joueur (12.27). Puis le joueur choisit le Réveil, sans volcan : la
**Grande bombarde** remplace le Volcan (12.28 ; un mortier géant qui se remplit de poudre, ta flèche dans la mèche déclenche la
salve). Compilée, **pas encore vue dans Unity**. Puis les **Montagnes russes refaites en Grappin** (12.29 ; un ovale en l'air
au-dessus de la voie, des grappins qui balaient toute sa largeur et jettent les accrochés vers la forêt). Compilées, **pas encore
vues dans Unity**. Le joueur les essaie : **« les nouvelles tours sont validées »** ; deux bugs du Colisée corrigés (12.30 : les
tourelles visaient les champions ; un champion restait figé quand son Colisée disparaissait). Ensuite : les ultimes.

**1er novembre au soir : l'équilibrage des tours.** Le joueur atteint la vague 45 avec les seules tourelles de F4 : les bâtiments
suivaient tout à fait la force des ennemis. Ils n'en suivent plus que la racine carrée, et leurs dégâts baissent de 20 % (12.31 ;
compilé, pas encore essayé). Puis : « les tours sont déséquilibrées les unes par rapport aux autres ». **La prochaine tâche, pour
l'autre compte Claude : le tableau d'équilibrage des 10 tours** (un fichier Excel, DPS par niveau et par vague, DPS de zone,
utilité de contrôle, changements proposés), décrite en entier en **12.32**. Les ultimes viennent après.

**2 novembre : le tableau d'équilibrage est fait** (12.33) : `Docs/Equilibrage_Tours.xlsx`, écrit par `Tools/equilibrage_tours.py`
d'après les prefabs, pour les 10 tours et aussi, à la demande du joueur, les 13 duos et les 7 trios ; les changements proposés par
Claude y sont, ligne « Équilibrage » de chaque bâtiment. Le même jour, le plafond d'ennemis à la fois passe à 150 (12.34), puis le
joueur remplit sa colonne et **tout est appliqué** (12.35 : `Editor/BalanceBaker` et le code ; ses idées en plus : la boule du
Bowling qui se met en place, Minuit du Beffroi en bulle noir et blanc sur toute la carte). Compilé, **pas encore vu dans Unity ni
testé au casque**. Ensuite : les essais (`Checklist_Demo.md`, « L'équilibrage du 2 novembre »), puis les ultimes.

**2 novembre au soir : le premier essai du joueur** (12.40, 12.41). Une partie « tout aux tours » : trop de course aux coffres,
des plans éparpillés sur les 10 tours, l'arc assez fort sans aucune pièce, MK trop facile, des places de construction
bloquées. **Appliqué** (12.41) : 4 coffres au gong, d'un seul côté de la forêt ; le chantier de la partie (3 bâtiments, un de
plus après chaque boss) ; le coffre : un parchemin, une tourelle, un objet ; le coffre de boss : 2 objets ; un plan par lot
et par niveau de tour, 2 par niveau de duo, 3 de trio ; la grille (cailloux et mannequins sous les bâtiments, le gué, la
raison affichée) ; la régénération du joueur et MK deux fois plus fort. Compilé, **pas encore vu dans Unity ni testé au
casque**. Ensuite : un nouvel essai (`Checklist_Demo.md`, « Le premier essai du 2 novembre »).

**2 novembre : le deuxième essai** (12.42). Les places refusées que le joueur signale (captures), puis une partie de sa sœur :
pas un duo en ouvrant tous les coffres, des vagues et des boss trop faciles avec un arc ordinaire, MK bien réglé, des vagues trop
longues maintenant que les coffres sont d'un seul côté. **Appliqué** (12.42) : 2 plans par coffre ; la courbe des PV relevée
(+6 à +70 % aux vagues 1 à 5, pas MK) ; les tours ×1,2 ; des vagues plus courtes (accalmie d'un tiers, montée d'un cinquième) ;
la grille (plus de limite de distance, l'habillage des falaises ignoré, la voie réservée à la terre des
tuiles des voies, les demi-cases de la voie du milieu en clairière, le nom de ce qui bloque en test, l'image de la grille dans
`Logs/BuildGrid.png`). Compilé, **pas encore vu dans Unity ni testé
au casque**. Ensuite : un nouvel essai (`Checklist_Demo.md`, « Le deuxième essai du 2 novembre »), relancer le tableau
d'équilibrage une fois Unity passé.

**2 novembre au soir : les ultimes** (12.44 : les règles, les trois choisies ; 12.45 : le Beffroi au niveau 3 devient le Phare du
passé). Puis **le système des ultimes et la première, le Colosse de forge** (12.46) : le niveau 4 de l'Enclume ou de la Grande
bombarde, 10 plans posés entre les vagues, une seule par partie ; un géant de fer à genoux qui lance son marteau où tombe ta
flèche, verse une coulée de fonte sur toute la voie et, dans l'arène, plaque MK au sol. Compilé, modèle dessiné hors de Unity
sans faces superposées ; **pas encore vu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`, « Le Colosse de forge »).
Ensuite : les essais du joueur (F4), puis le Grand Manège et la Grande Horloge. Le même soir, les sons de la fonte (freesound, CC0),
puis **l'essai du joueur** : l'attaque et la coulée manquent de puissance, le modèle doit être plus fini ; **refait** (12.47) :
l'éveil (il sort de terre, la foudre, le marteau abattu), le ciel de forge, un éclair à chaque coup, l'impact (onde de choc, roche,
sol fendu de fonte), une vraie rivière de fonte, le fer martelé et les veines de fonte qui luisent, des particules partout.
Puis **le deuxième essai** : mieux à voir et à entendre, mais un coup de temps en temps ne fait sentir ni sa puissance ni le nombre,
et le ciel de forge doit être passager ; **le Colosse à deux mains** (12.48, le choix du joueur) : de la main gauche, il bat le
lingot sur son enclume (un éclat de fonte à chaque coup, le lingot chauffé à blanc et la foudre au 5e), de la droite il lance un
marteau sur chaque flèche qui touche (5 par seconde au plus) ; le ciel de forge seulement pendant l'éveil et la coulée. Puis **le
troisième essai** : les marteaux trop petits et trop faibles ; **des marteaux imposants et la force au tableau** (12.49 : aussi grands
que le sien, la foudre et l'onde à chaque coup, 160 dans 6 m ; la forge renforcée ; le Colosse dans le tableau d'équilibrage, avec
les trios ; `Editor/ColosseForceBaker`). Compilé, **pas encore vu dans Unity ni testé au casque** ; relancer le tableau après Unity.
Puis les dégâts sur les boss des vagues (12.50) ; **le joueur valide le Colosse** (« ok, la tour me paraît bien ») ; ce qu'il a
appris est dans `Docs/Ultimes_Cahier.md`.
À faire plus tard (idée du joueur) : une lumière sous chaque tourelle pendant le boss final.

**2 novembre, tard : la deuxième ultime, le Grand Manège** (12.51 ; le Gonfleur, le Colisée ou les Montagnes russes au niveau 4). Le
choix du joueur : la ronde, la grande éjection, le feu d'artifice, MK étourdi. Un carrousel géant perché sur son mât : toutes les
1,2 s, son ruban doré attrape un ennemi et le fait tourner parmi les chevaux ; au 5e coup, tous ceux qui tournent sont éjectés d'un
coup sur le groupe le plus serré ; un cheval de bois saute sur chaque ennemi que touche ta flèche (5 par seconde au plus) ; toutes les
25 s, ses chevaux chargent sur toute la voie ; le feu d'artifice à chaque coup, le ciel de fête passager, l'éveil en boîte à musique ;
dans l'arène, sa charge étourdit MK (`FinalBoss/DragonBoss.Daze.cs`). Au tableau (relancé) : seul 87 / 484, 247 / 1 218 avec une flèche
qui touche par seconde, 407 / 1 951 avec deux. Compilé, modèle dessiné hors de Unity sans faces superposées ; **pas encore vu dans
Unity ni testé au casque** (tests : `Checklist_Demo.md`, « Le Grand Manège ») ; ses sons (l'orgue, la fanfare, le galop, le
hennissement, la fusée, le bouquet), choisis sur freesound et téléchargés par le joueur le même soir, sont découpés (à écouter en jeu).
Le joueur l'a lancé dans Unity le même soir (le journal : « Grand Manège : 4938 dégâts, 15 éliminations », aucune erreur) : « parfait ».

**2 novembre, tard : la troisième ultime, la Grande Horloge** (12.52 ; le Bowling ou le Beffroi au niveau 4). Les choix du joueur : les
chiffres, Minuit après le XII (sa version du Beffroi, 9 s sur toute la carte) et le dégel qui brise, l'écho du temps, l'aurore,
l'horloge astronomique, Minuit dans l'arène. Une horloge astronomique de glace et d'or qui flotte : à chaque tic, l'un de ses douze
chiffres romains de glace part sur un ennemi ; après le XII, « Za Warudo ! » : le temps s'arrête 9 s sur toute la carte, puis le givre
de tous les figés éclate ; chaque flèche qui touche est rejouée par une flèche de glace géante (5 par seconde au plus) ; dans l'arène,
Minuit arrête MK (`FinalBoss/DragonBoss.TimeStop.cs`). Au tableau : seule 134 / 413, 294 / 1 146 avec une flèche par seconde,
454 / 1 880 avec deux. Compilé, modèle dessiné hors de Unity sans faces superposées ; **pas encore vu dans Unity ni testé au casque**
(tests : `Checklist_Demo.md`, « La Grande Horloge ») ; ses sons, téléchargés par le joueur, sont découpés (le 3 novembre). Les
trois ultimes sont faites.

**3 novembre : les retouches du joueur.** L'Horloge (12.52, fin) : ses particules de glace et les aurores des chiffres plus petites,
les boss des vagues figés les 9 s de Minuit, les trois cristaux du socle retirés. **Le chantier du début** (12.53) : les 3 tours d'un
trio tiré au hasard, plus la Barricade (ou un autre bâtiment si le trio l'a déjà) : 4 bâtiments. Le silence de Minuit et son bruit d'arrêt du temps (12.52, fin ; puis 12.54, plus progressif et moins fort). L'action
par flèche des trois ultimes suit la force du tir (12.54 : parfait ×2, moyen ×0,8, la pluie moins). Compilé ; **pas encore vu dans
Unity**.

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
   couverte, les renforts reçus. (*La pose lisible codée le 3 novembre* : 12.55.)
5. **Les menaces par voie** (piste C), annoncées pendant la pause. (*Codé le 3 novembre, mis en sommeil le même jour* : 12.56.)
6. **Tenir le front** (piste B) : l'alerte de la montre, l'état des voies, l'équilibrage des gardiens. (*L'alerte de la montre est remplacée le 3 novembre par le cor et le rayon de la tour*, au choix du joueur : 12.59.)
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

### 12.17 Neuf duos de plus, les flèches « DUO » (31 octobre, nuit)

Le joueur, après le Boulet fou : un duo doit être **impressionnant**, et son **niveau 3 dans la démesure**. Ses
10 premiers duos de la liste sont faits (le Boulet fou, et neuf de plus cette nuit-là) : codés, compilés (le jeu et les outils
d'éditeur), leurs modèles dessinés hors de Unity et sans faces superposées ; **pas encore vus dans Unity ni testés au casque**.
À l'ouverture de Unity, le baker (`Editor/TowerDefenseLookBaker`) fait pour chacun le prefab (`Prefabs/Structures/Duos`), la
définition (`Data/Duos` : 3 niveaux à 4 plans, les textes) et l'entrée du catalogue (`Resources/TowerDuos`) ; la console l'écrit.
Les réglages sont sur le composant de chaque prefab. Toutes les valeurs sont des propositions, à régler aux tests.

**Les flèches « DUO »** (`Building/DuoHints.cs`, créé par le BuildManager) : entre deux tours qui peuvent fusionner (les deux au
niveau 3, finies, d'une paire du catalogue), une grande flèche dorée en arc, à double pointe, à 3,5 m au-dessus de la plus haute
(plus haut si elles sont loin), avec « DUO » et le nom du duo à son sommet, face au joueur. Seulement entre les vagues
(le joueur : pendant une vague, elles prendraient de la vue). Le matériau est celui des hologrammes de pose, en or qui respire.

**Ce que les duos partagent** (`Building/TowerDuo.cs`) : les ennemis à portée (`EnemiesNear`, `GroundEnemies`), blesser
(`Hurt`, aux dégâts qui suivent la force des ennemis), renverser (`KnockDown`), la voie la plus proche et le sens de la forêt
(`LaneNearest`, `ForestwardAt`), une gerbe de particules colorées (`Burst`, le matériau d'effet des flèches), l'explosion des
flèches (`Explosion`), le sol sous un point (`GroundY`). Côté ennemis : `Enemy.KeepHeld` (tenu plus de 8 s : la boule de
l'Avalanche), `Enemy.Release(vitesse, mortel, pesanteur)` (le saut à ski), et le **charme** (`Enemies/Enemy.Charm.cs` :
l'ennemi court où le bâtiment l'envoie, sans attaquer la tour ; le Chant des sirènes). Le baker sait maintenant donner à un
réglage le composant d'un prefab (le carreau de la baliste pour la Loupe et le Carquois).

| Duo | Niveau 1 | Niveau 2 | Niveau 3 : la démesure | Le modèle |
|---|---|---|---|---|
| **Marteau-pilon** (Barricade + Piques, voie, 4 × 3 cases ; `DuoMarteauPilon`) | toutes les 3 s, le marteau glisse au-dessus de l'ennemi et l'enfonce dans le sol jusqu'au cou : planté 4 s, seule sa tête dépasse (« Planté ! », l'ennemi cloué) ; il en sort comme un bouchon, en l'air | l'onde du coup renverse et blesse tout à 5 m | **la forge** : trois marteaux en cadence (gauche, milieu, droite), puis le **grand coup**, les trois ensemble : tout ce qui est à 11 m part en l'air | un portique de forge de 9 m sur deux piliers de pierre cerclés de fer, une enclume dorée ; la chaudière au niveau 2 |
| **Toboggan** (Tremplin + Tour de givre, voie, 4 × 7 ; `DuoToboggan`) | une piste de glace en pente vers la forêt : l'ennemi qui y monte glisse sur le ventre en tournoyant, renverse ceux qu'il croise et décolle du tremplin en vrille, au ralenti, sous trois notes du jury ; il retombe sonné. **Une glissade par ennemi** | ceux qu'il percute sur la piste glissent aussi ; un saut plus haut, plus long | **le saut à ski** : un grand tremplin les envoie à plus de 20 m de haut, loin vers la forêt, sous un « 10 ! 10 ! 10 ! » ; l'atterrissage les tue | la piste bleue sur sa butte de neige, ses liserés dorés, les bottes de paille, l'estrade du jury ; au niveau 3, le grand tremplin sur pilotis et la tour du jury. Posée à l'envers, la piste se retourne (la voie décide) |
| **Yo-yo** (Tremplin + Tour-aimant, voie, 4 × 3 ; `DuoYoyo`) | une grue de 13 m : son yo-yo aimanté attrape un ennemi, le hisse à 6 m, le fait claquer au sol trois fois (des dégâts à chaque fois), puis le lâche de tout en haut | il le lance sur le groupe le plus serré, qu'il renverse | **le lustre** : un anneau doré à six fils de plus, qui tourne ; sept ennemis montent et descendent, puis le lustre s'emballe et les jette au loin, où ils s'écrasent (45 % de leurs PV) | la grue au bord de la voie, sa cabine rouge, sa flèche au-dessus de la voie |
| **Avalanche** (Mortier + Tour de givre, bord ; `DuoAvalanche`) | le mortier de glace tire une boule de neige devant le premier ennemi : elle roule vers la forêt (22 m), grossit et avale ceux qu'elle touche (pris dedans, ils tournent avec elle) ; au bout, ou **sous ta flèche** (moitié plus de dégâts), elle éclate et ils volent en l'air | elle roule plus loin, plus grosse, avale aussi les gros | **l'avalanche** : une boule géante (jusqu'à 9 m) dévale toute la voie jusqu'à la forêt, avale tout et éclate en coûtant la moitié de leurs PV à tous | un gros mortier de glace cerclé d'or sur un socle enneigé, une boule chargée dans la gueule ; au niveau 3, un fût plus large, des cristaux de 4 m |
| **Feu d'artifice** (Mortier + Phare, bord ; `DuoFeuArtifice`) | toutes les 3 s, une fusée éclate en bouquet au-dessus du groupe le plus serré : éblouis 2 s (« Sonné ! »), sous une pluie d'étincelles qui brûle ; **ta flèche qui traverse le bouquet s'enflamme** | des fusées à deux étages : deux bouquets de plus à côté | **le bouquet final** : vingt fusées d'un coup sur tous les ennemis à portée, le ciel s'allume (un éclair de lumière par bouquet), tous éblouis 3 s et brûlés ; toutes les 14 s | un phare rayé rouge et blanc de 9 m, sa couronne de fusées ; un balcon de fusées à deux étages, puis la batterie et l'étoile dorée |
| **Chant des sirènes** (Phare + Bulle, bord ; `DuoSirenes`) | le faisceau charme un ennemi 6 s : dans sa bulle rose, il change de camp, se jette sur les autres ennemis, les renverse et les blesse ; sans personne, il repart vers la forêt | deux à la fois ; à la fin, la bulle éclate en deux bulles qui charment deux voisins | **la mêlée** : tout ce qui est à 20 m change de camp 5 s, chacun frappe son voisin ; toutes les 15 s | un phare de nacre rayé de rose, sa lanterne et son faisceau, une sirène de bronze au sommet, des bulles au pied ; la grande conque dorée au niveau 3 |
| **Loupe** (Phare + Baliste, bord ; `DuoLoupe`) | une loupe de 3 m sur une tour de bois se tourne vers les ennemis : dans son champ (30 m, 76°), leur tête est trois fois plus grosse (sa zone de tir aussi ; le point faible des boss deux fois) | une baliste sur la fourche plante ses carreaux dans les têtes géantes | **la fourmi** : un rayon de soleil sur le plus fort du champ ; il fume, puis prend feu (un tiers de ses PV, un boss 15 %) ; le rayon passe au suivant | la tour de bois, la fourche et l'anneau de laiton, le verre ; des rayons dorés au niveau 3 |
| **Carquois géant** (Baliste + Lance-filet, bord ; `DuoCarquoisGeant`) | tes flèches ratées (plantées dans le décor à 20 m) sont rattrapées et se rangent dans le filet ; à dix, l'arbalète à six arcs les renvoie en carreaux qui pleuvent sur le groupe le plus serré | chaque carreau explose à l'impact, et brûle si une flèche enflammée a été rattrapée (à la place de « garder leurs pièces », trop lourd à refaire) | **la nuée** : toutes tes flèches ratées, partout sur la carte ; à trente, elles repartent d'un coup, trois carreaux chacune, sur tous les ennemis à portée | un mât de 9,5 m, le filet tendu comme une voile, dix flèches plantées dedans (une par flèche rattrapée), l'arbalète pointée vers le ciel ; au niveau 3, une voile de plus jusqu'à 13 m |
| **Chaîne de forçats** (Tour-aimant + Lance-filet, bord ; `DuoChaineForcats`) | des chaînes aimantées lient jusqu'à 5 ennemis 8 s : ralentis, ils avancent ensemble ; **chaque dégât reçu par l'un, les autres en reçoivent 60 %** (ta flèche à la tête d'un seul : toute la chaîne) | jusqu'à 8 ; un lié qui meurt fait tomber les autres | **l'écrasement** : jusqu'à 15 ; la chaîne se resserre d'un coup, tous volent les uns contre les autres et s'écrasent au milieu (40 % de leurs PV), sonnés | une tour de fer, deux bobines de chaîne, un gros aimant rouge en fer à cheval ; au niveau 3, une couronne, quatre grosses chaînes jusqu'au sol, un boulet |

Les boss ne sont jamais tenus, plantés, avalés ni charmés (poussés, ralentis ou blessés à la place) ; les volants ne sont
touchés que par le Feu d'artifice, la Loupe et le Carquois.

**À surveiller aux tests** :
- **Marteau-pilon** : la profondeur où l'ennemi est planté (sous le bas de sa zone de tête) ; que les flèches touchent bien la
  tête qui dépasse ; le bouchon.
- **Toboggan** : le sens de la piste (elle doit descendre vers la forêt) ; l'ennemi couché sur le ventre (il est tourné de 90°
  autour de ses pieds) ; que la voie ne se bloque jamais.
- **Yo-yo** : le yo-yo et l'ennemi qui pend dessous ; le lustre (sept fils, l'emballement) ; les ennemis jetés au loin.
- **Avalanche** : la boule qui suit bien la voie et le sol ; les ennemis pris dans la neige ; la flèche qui la crève.
- **Feu d'artifice** : les flèches enflammées (la flamme passe par le brasier de la tour, `Brazier.IgniteArrow`, quand une
  flèche en vol traverse le bouquet : il faut qu'elle ait un collider en vol) ; le bouquet final (le nombre de particules).
- **Chant des sirènes** : les ennemis charmés qui courent sur les autres (le NavMesh) ; la bulle rose (le matériau des
  hologrammes) ; « bonjour les bugs », disait le joueur.
- **Loupe** : la tête qui grossit vraiment (l'os de la tête humanoïde, sinon l'os qui porte la zone de tête, sinon la zone
  seule) ; qu'elle dégonfle en sortant du champ et à la mort.
- **Carquois géant** : les flèches ratées comptées (`Arrow.Stuck`, hors ennemis) ; la pluie de carreaux.
- **Chaîne de forçats** : le partage des dégâts (pas de boucle : ce que la chaîne partage n'est pas repartagé) ; les chaînes
  (des LineRenderer au fer du modèle).

### 12.18 Les trois derniers duos, le Toboggan revu, les premiers bugs (31 octobre au soir)

Le joueur a essayé les duos dans Unity (avec la vitrine, F4) et noté : le Toboggan est à revoir en entier, la boule de
l'Avalanche est très buguée, et la Chaîne de forçats fait planter le jeu. Puis : « occupe-toi des dernières tours ». Tout est
codé et compilé (le jeu et les outils d'éditeur), les modèles dessinés hors de Unity sans faces superposées ; **pas encore vu
dans Unity ni testé au casque**. Les 13 duos de la partie 12.15 sont maintenant codés.

**Les bugs du premier essai** :
- **Chaîne de forçats, plantage** (`StackOverflowException`, la vingtième tour de la vitrine) : la vitrine pose deux Chaînes
  de forçats, et un ennemi lié par les deux se renvoyait les dégâts de l'une à l'autre, sans fin (chacune ne refusait que ses
  propres dégâts). Maintenant, les dégâts partagés par n'importe quelle chaîne ne se partagent plus, et un partage ne peut pas
  en lancer un autre.
- **Avalanche, les ennemis qui sautent partout dans la boule** : la boule cherchait le sol sous elle par un rayon tiré de 6 m
  plus haut, qui touchait **son propre collider** ; d'une image à l'autre, elle se posait sur le sol puis sur elle-même, et les
  ennemis avalés sautaient avec elle (selon sa taille : « de temps en temps »). Le sol d'un duo ignore maintenant ce que les
  duos lancent (`TowerDuo.GroundY`).
- **Avalanche, sous le pont du milieu** : ce rayon de 6 m touchait aussi le pont, et la boule montait dessus. Pour un point de
  la voie, il part maintenant de 1,5 m au-dessus (la boule, et les baudruches et la boule à neige).

**Le Toboggan revu** (le joueur : « on ne comprend pas comment les ennemis glissent dessus », et un ennemi qui revenait
traversait la piste, posée au sol : les bâtiments n'ont pas de collider). C'est maintenant un grand **toboggan de jardin
d'enfants qui enjambe la voie**, tout au-dessus de la tête des ennemis : un portique de bois, sa plate-forme à 5,6 m, une
échelle sur son flanc ; de la plate-forme, une piste de glace en l'air descend vers la forêt jusqu'au tremplin, porté par un
second portique (passage de 2,75 m sous sa traverse). L'ennemi qui passe sous la plate-forme saute au pied de l'échelle, la
grimpe (4 m/s, le bois craque), fait trois pas sur la plate-forme, glisse sur le ventre en tournoyant et décolle du tremplin en
vrille, au ralenti, sous les notes du jury ; il retombe sonné, plus loin vers la forêt. Trois à la fois au plus (les autres
passent dessous). Une seule glissade par ennemi : au deuxième passage, il passe sous le toboggan. Niveau 2 : il saute plus haut
et retombe sur les autres, qu'il renverse (« Strike ! », les quilles) ; niveau 3 : le saut à ski, inchangé. Le modèle est refait
par le baker (`rebuild`), les textes des niveaux et du catalogue aussi, **s'ils n'ont pas été changés à la main** (le baker sait
maintenant revoir le texte du catalogue d'un duo : `descriptionBefore`). Les réglages nouveaux prennent leurs valeurs par défaut
sur le prefab.

**Les trois derniers duos** :

| Duo | Niveau 1 | Niveau 2 | Niveau 3 : la démesure | Le modèle |
|---|---|---|---|---|
| **Boussole folle** (Piques + Tour-aimant, voie, 5 × 5 cases ; `DuoBoussole`) | une rose des vents de laiton de 4,8 m, posée à plat sur la voie et hérissée de pointes : l'ennemi qui y entre **perd le nord** (« Perdu le nord ! ») et tourne en rond 3 s sur les pointes, autour de l'aiguille, blessé ; il ne le reperd pas avant 7 s | il tourne plus longtemps, puis **repart dans le mauvais sens**, vers la forêt, 3 s (« Demi-tour ! ») | **la rose des vents** : toutes les 16 s, l'aiguille s'emballe et **toute la voie perd le nord**, à 45 m de part et d'autre : chacun tourne en rond là où il est 5 s, puis toute la voie repart vers la forêt 5 s | la rose de laiton, ses branches blanches (le nord rouge), ses diagonales noires, ses pointes de fer ; l'aiguille rouge et blanche sur son pilier, qui cherche le nord en tremblant et s'affole ; quatre aimants rouges au niveau 2 ; au niveau 3, deux grands anneaux dorés croisés sur l'aiguille, qui tournent avec elle |
| **Lanceur de baudruches** (Mortier + Bulle, bord ; `DuoBaudruches`) | la pompe et le canon à air lancent une **baudruche à ton image** (l'archer à la capuche verte, 2 m) devant le premier ennemi : ceux qui sont à 7 m s'arrêtent pour la frapper ; au sixième coup, elle éclate en **confettis piquants** (blessés, renversés) ; **ta flèche la crève avant**, quand tout le monde est autour (moitié plus de dégâts) | tous les trois coups, elle **rebondit** 4 m vers la forêt et sur le côté (« Boing ! ») : ceux qui la frappaient sont entraînés hors de leur chemin | **le char de carnaval** : toutes les 20 s, une baudruche de 7 m que **tous les ennemis à 40 m** viennent frapper ; 40 coups, puis une pluie de confettis à 9 m qui leur coûte 35 % de leurs PV | une estrade de foire à jupe rayée rouge et blanc, la grosse pompe de cuivre à poignée en T, le canon à pavillon de trompette sur sa tourelle, un mât de ballons, la baudruche attachée au-dessus ; au niveau 3, une étoile dorée, des fanions, trois gros ballons |
| **Boule à neige** (Bulle + Tour de givre, bord ; `DuoBouleNeige`) | une **boule de verre** de 5 m tombe du ciel sur le groupe le plus serré et l'enferme : il y neige, et les ennemis ralentissent jusqu'à **se figer en figurines** (2 s, « Figé ! ») ; **ta flèche brise la boule** : toutes les figurines éclatent (90 de dégâts) ; sinon, au bout de 6 s, elle se fend et les relâche, un peu blessés. Ceux du dehors ne peuvent pas y entrer | à mi-temps, **la boule se secoue** : les figurines dégèlent, tourbillonnent dans la neige et se heurtent (des dégâts à chaque choc), puis se refigent | **la boule immense** : toutes les 18 s, une boule de **18 m** tombe sur toute une voie (au moins 3 ennemis dessous), secouée elle aussi ; **brisée par ta flèche, chaque figurine éclate et meurt** ; fendue d'elle-même, elle leur coûte 40 % de leurs PV | un socle de bois verni à trois étages cerclés d'or, la boule de verre (le matériau de la bulle) où il neige, et dedans la tour du joueur en miniature, des sapins et un bonhomme de neige ; la clé de remontoir au niveau 2 ; au niveau 3, un anneau doré autour de la boule, des cristaux de glace, une étoile de glace |

Ce qu'ils partagent : `Building/DuoShotTarget.cs`, ce qu'un duo lance et que ta flèche peut crever (la baudruche, la boule de
verre), comme la boule de l'Avalanche. La Boussole et les baudruches se servent du charme (`Enemy.Charm`), la Boule à neige du
gel (`Enemy.Freeze`) et de la tenue (`Enemy.Hold`). Les boss ne sont ni charmés ni enfermés (ralentis à la place) ; les volants
ne sont pas touchés.

**À surveiller aux tests** :
- **Toboggan** : que les ennemis passent bien sous le portique et la traverse (2,75 m) sans la traverser, même les grands ;
  l'échelle du bon côté ; la glissade sur la piste en l'air ; le saut.
- **Boussole folle** : les ennemis qui tournent en rond (le charme poursuit un point qui tourne : le NavMesh) ; le demi-tour ;
  la rose des vents sur toute la voie ; l'aiguille.
- **Lanceur de baudruches** : les ennemis qui s'arrêtent pour frapper (leur animation d'attaque) ; le rebond ; la flèche qui la
  crève ; le char de carnaval.
- **Boule à neige** : le verre transparent (le matériau `Bulle`) ; les figurines figées, le tourbillon ; la flèche qui brise la
  boule ; la boule immense (18 m : ce qu'elle cache de la vue).
- **Avalanche** et **Chaîne de forçats** : les bugs ci-dessus, corrigés.

### 12.19 Les trios (proposés par Claude, triés par le joueur le 1er novembre)

Les 5 trios de 12.9 étaient faits pour les fusions empilées ; avec les duos devenus des tours nouvelles (12.14), **un trio
devient un duo plus une tour**. Même formule que les duos de 12.15 : chaque trio garde **un seul** élément de ses tours, le
pousse à l'extrême et apporte une mécanique qu'aucune autre tour ni aucun duo n'a (Claude a évité celles des 13 duos :
changer de voie, planter dans le sol, glisser, hypnotiser, grossir la tête, recycler les flèches, enchaîner, désorienter,
leurrer, figer en figurines). Plus grand qu'un duo, des accents dorés, un niveau 3 dans la démesure. Comme partout, les boss
ne sont ni projetés, ni aplatis, ni gonflés, ni recrutés : une grosse part de leurs PV à la place.

**La règle proposée** (pas encore validée par le joueur) :
- un trio = **un duo + une troisième tour** (pas une des deux du duo) au moins au niveau 3 ; on déplace l'un sur l'autre,
  comme pour les duos (hologramme doré ; une flèche « TRIO » entre les vagues) ; il reste sur la voie si l'une de ses tours y est ;
- il naît **au niveau du duo**, un de plus si la tour est au niveau 5 (3 au plus) ;
- **4 plans par niveau**, de l'une de ses trois tours : 9 plans au niveau 1, 13 au niveau 2, 17 au niveau 3, par n'importe quel
  chemin (comme les duos : 6 + 3 ; 10 + 3 ou 6 + 7 ; 14 + 3 ou 10 + 7) ;
- un trio ne refusionne pas (les ultimes restent à revoir à part).

**Le tri du joueur (1er novembre)** : sur 9 propositions, gardés les 5 conseillés moins la **Lanterne des phalènes**
(Carquois géant + Phare : les volants attirés comme des papillons de nuit), plus **Volcan**, **Enclume** et **Colisée** ;
retiré aussi le **Grand chapiteau** (Yo-yo + Bulle : un automate qui jongle avec les ennemis). **7 trios** donc, au lieu de 5.
Textes et chiffres : des propositions, à revoir aux essais.

| Trio | Duo + tour | Où | Niveau 1 : l'idée | Niveau 2 | Niveau 3 : la démesure | Allure | À coder |
|---|---|---|---|---|---|---|---|
| **Bowling** | Avalanche + Barricade | voie | des rails de bois guident les ennemis et les rangent en **triangle de quilles** au bout de la piste (« Rangés ! », 3 s) ; toutes les 8 s, une boule de glace roule dessus : « **STRIKE !** » en grand, ils volent dans tous les sens (« Au vol ! ») ; ta flèche dans la boule l'accélère et la fait éclater en glace au milieu des quilles | un strike rend la boule suivante deux fois plus grosse ; une deuxième boule fait le « spare » sur les survivants | **Partie parfaite** : douze boules d'affilée, toute la voie rangée en triangles, une boule de 4 m pour finir ; le tableau affiche 300 | une machine de retour de boules géante, des quilles dorées au sommet, un tableau de score lumineux | moyen |
| **Beffroi** | Boussole folle + Phare | bord | toutes les 10 s, la cloche sonne : les ennemis du rayon **rembobinent** leurs 4 dernières secondes, à reculons sur leur chemin (image sépia, bruit de cassette, « Rembobiné ! ») | à l'arrivée, ils restent figés 2 s (préparés) | **Minuit** : douze coups ; le temps s'arrête 6 s dans tout le rayon, **sauf pour toi** : tes flèches s'arrêtent en l'air devant leurs cibles, et au douzième coup partent toutes ensemble (un boss : figé moins longtemps) | un beffroi de pierre, un grand cadran doré aux aiguilles qui tournent à l'envers, la rose des vents au sol | lourd |
| **Montagnes russes** | Chaîne de forçats + Tremplin | voie | un circuit de bois avec un looping enjambe la voie ; toutes les 6 s, un wagon aimanté passe au ras du sol, ramasse jusqu'à 4 ennemis, monte ; en haut du looping, ils tombent (« Au vol ! ») | trois wagons attachés ; ceux qui tombent retombent sur un groupe (les quilles) | **Grand huit infernal** : un train de dix wagons, le circuit au-dessus de toute la voie, et à la fin un saut dans le vide : le train s'écrase sur le groupe le plus serré, l'atterrissage tue | une charpente rouge et or, des lampions, des wagons de fer enchaînés | lourd |
| **Gonfleur** | Lanceur de baudruches + Baliste | bord | la baliste plante un tuyau-harpon dans un ennemi, la pompe le **gonfle comme une baudruche** : il s'envole doucement (« Au vol ! ») ; ta flèche le crève (confettis, il blesse ceux du dessous) ; sinon il crève seul au bout de 6 s et retombe | deux tuyaux ; un ennemi crevé souffle son air sur ses voisins, qui gonflent à leur tour | **Défilé** : toute une voie gonflée d'un coup, le ciel plein d'ennemis-ballons, un stand de tir géant (chaque ballon crevé : deux fois plus de points) ; les gros deviennent des chars de parade | une pompe de foire géante en cuivre sur un char, des tuyaux rayés, la baliste-harpon dorée | moyen |
| **Volcan** | Feu d'artifice + Tremplin | voie | un petit volcan sur la voie ; toutes les 6 s, il entre en éruption : les ennemis sur son cône (4 m) partent en **bombes de lave** qui retombent sur les groupes autour (« Au vol ! » ; blessés, brûlés) ; ta flèche qui traverse le panache s'enflamme | la lave coule autour du cône, et les bombes en laissent là où elles tombent : le sol brûle | **Éruption** : la montagne grandit sous tes yeux (15 m), le ciel rougit, et une **coulée de lave descend la voie à contre-sens** sur 30 m : elle repousse vers la forêt tout ce qu'elle touche et le brûle | un cône de roche noire fendu de lave orange, une couronne de fusées dorées autour du cratère | moyen |
| **Enclume** | Marteau-pilon + Mortier | voie | toutes les 4 s, le mortier tire une enclume en cloche sur l'ennemi visé : **aplati comme une crêpe** (« Aplati ! ») 6 s, il avance tout plat, deux fois moins vite (préparé) | l'enclume rebondit et aplatit un deuxième ennemi ; l'onde du choc fait tomber autour | **Pluie d'enclumes** : vingt enclumes sur toute la voie, puis une enclume de 10 m au milieu : les petits tués net, tous les autres aplatis ; elle reste plantée 6 s, puis s'enfonce dans le sol | une forge sur pilotis, un mortier en cloche de bronze, une pile d'enclumes cerclées d'or, une cheminée qui crache des étincelles | simple |
| **Colisée** | Chant des sirènes + Barricade | voie | toutes les 15 s, une **arène** de pierre (8 m) s'élève autour de 3 à 5 ennemis : hypnotisés (« Gladiateurs ! »), ils se battent entre eux ; le dernier debout **se range de ton côté** pour la vague (couronne de laurier dorée) et frappe les ennemis qui passent ; ta flèche élimine le gladiateur qu'elle touche | deux champions à la fois ; un champion qui meurt éclate en bulles qui hypnotisent autour | **Les jeux du cirque** : l'arène s'élargit sur toute la voie (20 m) et avale un groupe entier ; le vainqueur devient **géant** (×3), en armure dorée, et marche à contre-sens sur la voie jusqu'à la fin de la vague (un boss n'est jamais gladiateur : l'arène le ralentit) | un petit amphithéâtre de pierre blanche, des gradins, une sirène de bronze sur une colonne, des oriflammes dorées | lourd |

**Les tours dans les trios** :

| Tour | Trios | Nombre |
|---|---|---|
| Mortier | Bowling, Gonfleur, Volcan, Enclume | 4 |
| Barricade | Bowling, Enclume, Colisée | 3 |
| Phare | Beffroi, Volcan, Colisée | 3 |
| Piques | Beffroi, Enclume | 2 |
| Tremplin | Montagnes russes, Volcan | 2 |
| Tour-aimant | Beffroi, Montagnes russes | 2 |
| Bulle | Gonfleur, Colisée | 2 |
| Tour de givre | Bowling | 1 |
| Lance-filet | Montagnes russes | 1 |
| Baliste | Gonfleur | 1 |

Les duos sans trio : Boulet fou, Toboggan, Yo-yo, Loupe, Carquois géant, Boule à neige. Sans la Lanterne, **aucun trio n'est
fait contre les volants**, et le mortier entre dans 4 trios sur 7 (le lance-filet, la baliste et la tour de givre dans un seul).

**L'ordre de code conseillé** : le système (la fusion d'un duo et d'une tour, la flèche « TRIO », la vitrine F4 étendue aux
trios), avec l'**Enclume** (la plus simple, pour éprouver le système) ; puis le Gonfleur, le Bowling et le Volcan (moyens) ;
enfin le Beffroi, le Colisée et les Montagnes russes (lourds).

### 12.20 Les trios dans le code : le système et l'Enclume (1er novembre)

Le joueur : « oui, commence » (le système avec l'Enclume). Codé, compilé (le jeu et les outils d'éditeur, sans erreur ni
avertissement), le modèle dessiné hors de Unity sans faces superposées ; **pas encore vu dans Unity ni testé au casque** (tests :
`Checklist_Demo.md`, « Les trios »). La règle de 12.19 est codée telle quelle, à valider aux essais.

**Le système** :
- Un trio est un duo (`Building/TowerTrio.cs` hérite de `TowerDuo`) : sa troisième tour (`Third`), et les plans de ses trois
  tours le font monter (`PlanSources`, `AcceptsPlansOf`). Sa définition va dans `Data/Duos` (sorte « Duo », comme les duos), son
  prefab dans `Prefabs/Structures/Duos` ; le catalogue `Resources/TowerDuos` a une liste à part, **Trios** (le duo dont il naît,
  sa troisième tour en `First`, sa définition).
- **Fusionner** : comme un duo, déplacer le duo sur la tour (ou la tour sur le duo) entre les vagues. `BuildManager.DuoOf` rend un
  duo ou un trio (« Déjà un trio », « Mortier au niveau 3 au moins ») ; `DuoLevelFrom` donne le niveau du duo, un de plus si la
  tour est au niveau maximal ; `Fuse` garde les deux tours du duo et la troisième. Les messages disent « Trio : Enclume ! ».
- Les flèches dorées relient aussi un duo et une tour qui font un trio, avec le mot « TRIO » (`DuoHints`) ; la vitrine **F4**
  montre les trios après les duos (`BuildShowcase`).
- Le baker (`Editor/TowerDefenseLookBaker`) lit la partie « trios » du fichier d'allure, écrite par
  `Tools/allure_tours/trios.py` (via `conception.py`) : prefab, modèle, définition, inscription au catalogue, une fois.
- **Une préparation nouvelle, « Aplati »** (`Preparation.Flattened`, ajoutée à la fin) : +50 % de dégâts pour un coup du joueur
  (proposé). Le mot « Aplati ! » s'affiche sans son tant que la scène n'a pas sa ligne dans les *Prepared Cues* du Score
  Manager (à ajouter à la main pour lui donner un son).

**L'Enclume** (`Building/TrioEnclume.cs`, Marteau-pilon + Mortier, sur la voie, 4 × 4 cases) : une forge sur pilotis (plate-forme à
6,5 m, les ennemis passent dessous), un foyer et sa cheminée, un mortier en cloche de bronze, un tas d'enclumes cerclées d'or
(celle du haut sert de modèle aux tirs) ; niveau 2 : le soufflet et deux fanions ; niveau 3 : un portique d'où pend une grande
enclume dorée. Les réglages sont en tête du script :
- niveau 1 : toutes les 4 s (3,2 ; 2,4 aux niveaux 2 et 3), une enclume en cloche (1,1 s de vol) sur l'ennemi au sol le plus
  avancé à 16 m ; dégâts 40 / 55 / 70 dans 1,4 m ; les survivants sont **aplatis** 6 s (7 au niveau 3) : écrasés à 18 % de leur
  hauteur et 1,45 fois plus larges (leurs zones de touche avec), deux fois moins vite, puis ils reprennent leur forme d'un coup ;
- niveau 2 : l'onde (3,5 m, 15 de dégâts, renversés) et un rebond vers l'ennemi le plus avancé à 6 m ;
- niveau 3 : toutes les 16 s, avec au moins trois ennemis à portée, vingt enclumes du ciel en 2,5 s (30 de dégâts), puis la
  grande enclume (×8) tout droit sur le groupe le plus serré : 350 de dégâts dans 6 m, les survivants aplatis ; un boss y perd
  8 % de ses PV max, sans être aplati. Elle reste plantée 6 s, puis s'enfonce (les ennemis passent au travers : les bâtiments n'ont
  pas de collider).
- Les boss ne sont jamais aplatis, les volants jamais visés.

**À surveiller aux tests** : l'ennemi aplati (sa racine écrasée, et donc son NavMesh Agent : qu'il ne s'enfonce pas et ne flotte
pas) ; la place de la forge sur la voie (4 × 4 cases, peut-être trop peu pour les pilotis à 3,9 m de part et d'autre) ; le nombre
d'enclumes en même temps ; la grande enclume qui cache la vue.

### 12.21 Le Gonfleur dans le code (1er novembre)

Le joueur : « fais le Gonfleur » (avant d'essayer l'Enclume). Codé, compilé (le jeu et les outils d'éditeur, sans erreur ni
avertissement), modèle dessiné hors de Unity sans faces superposées ; **pas encore vu dans Unity ni testé au casque** (tests :
`Checklist_Demo.md`, « Les trios »).

**Le Gonfleur** (`Building/TrioGonfleur.cs`, Lanceur de baudruches + Baliste, au bord, 4 × 4 cases) : un char de foire (roues,
jupe rayée rouge et blanc, cadre doré), une grosse pompe de cuivre cerclée d'or et son piston, un tuyau rayé jusqu'à la
baliste-harpon dorée, trois ballons à un mât ; niveau 2 : l'enrouleur du deuxième tuyau, deux ballons de plus ; niveau 3 : un
ballon de parade géant (un gros ennemi rose aux oreilles, couronné d'or) retenu par quatre cordes. Les réglages sont en tête du
script :
- niveau 1 : toutes les 3,5 s (3 ; 2,5), la baliste se tourne vers l'ennemi au sol le plus avancé à 18 m ; le tuyau file jusqu'à
  lui (le harpon : 15 / 20 / 25 de dégâts), la pompe bat et il **enfle** en 1,5 s, immobile (×1,7 : sa racine, ses zones de touche
  avec), puis il s'envole doucement à 7 m (« Gonflé ! » ; en l'air, il est préparé : « Au vol ! ») ; blessé (ta flèche, un coup),
  il crève : « Pan ! », des confettis qui blessent ceux du dessous (3 m, 30 / 40 / 50), et il retombe (la règle de la chute) ;
  sinon il crève tout seul au bout de 6 s, sans blesser personne, et retombe ;
- niveau 2 : deux tuyaux à la fois ; un ballon crevé avant la fin souffle son air sur deux voisins (4 m), qui gonflent en 0,6 s
  sans tuyau, un peu moins gros et moins haut (eux ne soufflent plus) ;
- niveau 3, **le défilé** : toutes les 18 s, avec au moins quatre ennemis à portée, toute la portée gonfle d'un coup (16 au
  plus, un tuyau chacun, en 0,8 s), monte à 10 m et flotte 8 s (« Le défilé ! ») ; un ennemi de plus de 2,4 m devient un char
  de parade (×2,6) ; un ballon du défilé crevé par ta flèche rapporte ses points une deuxième fois (« Défilé ×2 »).
- Les boss et les volants ne sont jamais gonflés. Un ennemi aplati par l'Enclume n'est pas gonflé, et un ennemi gonflé n'est pas
  aplati (les fusions qui changent la taille d'un ennemi se le réservent : `TowerDuo.s_Reshaped`).

**À surveiller aux tests** : l'ennemi gonflé (sa racine agrandie : son NavMesh Agent, ses pieds dans le sol pendant qu'il enfle) ;
les tuyaux (le matériau rouge du modèle sur un LineRenderer) ; le défilé, seize ennemis en l'air (la vue, le Quest) ; le son de
gonflage (provisoire : `teleport_charge`).

### 12.22 Le Bowling dans le code (1er novembre)

Le joueur : « oui, le Bowling ». Codé, compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement), modèle dessiné hors
de Unity sans faces superposées ; **pas encore vu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`, « Les trios »).

**Le Bowling** (`Building/TrioBowling.cs`, Avalanche + Barricade, sur la voie, 4 × 3 cases) : une machine de retour de boules
géante en arche au-dessus de la voie (les ennemis passent dessous) : deux tours laquées rouge cerclées d'or et leurs étagères de
boules de glace, la poutre, le tableau de score (son texte flotte devant, face au joueur), deux quilles dorées, et sous la poutre
la boule suivante dans sa pince (elle sert de modèle aux boules lancées) ; niveau 2 : deux boules de plus, deux fanions ; niveau
3 : une boule d'or sur le tableau, deux grandes quilles dorées. Tout suit la voie la plus proche de l'arche (pas l'orientation du
modèle) : les places des quilles et le chemin de la boule. Les réglages sont en tête du script :
- **les quilles** : 9 m plus loin vers la forêt, le long de la voie, les ennemis au sol à moins de 7 m sont **charmés** vers une
  place du triangle (1, 2, 3, 4 quilles par rang, sa pointe vers l'arche ; « Rangés ! ») et y attendent, 4 s au plus ;
- **la boule** (niveau 1) : toutes les 8 s s'il y a une quille rangée (dès 4 s s'il y en a trois), une boule de glace de 1,4 m part
  de l'arche et roule vers la forêt le long de la voie (13 m/s) ; ceux qu'elle touche (45 / 60 / 75 de dégâts) volent vers la
  forêt, sur les côtés et en l'air, en tournoyant ; toutes les quilles tombées (trois au moins) : « STRIKE ! » (en grand, sur le
  tableau) ; **ta flèche dans la boule** la rend givrée : elle va 1,8 fois plus vite et éclate au premier ennemi (4 m, 40 de
  dégâts, gelés 2 s : « Brisé ! » sous tes flèches) ;
- niveau 2 : après un strike, la boule suivante est deux fois plus grosse (et 1,5 fois plus forte) ; s'il reste des quilles, une
  deuxième boule part 0,6 s plus tard (« SPARE ! » si elle les fait toutes tomber) ;
- niveau 3, **la partie parfaite** : toutes les 24 s, avec cinq ennemis près des quilles, trois triangles à la suite (30 places),
  puis douze boules d'affilée (0,4 s entre deux), la dernière de 4 m (250 de dégâts ; un boss : 6 % de ses PV max) ; le tableau
  affiche 300.
- Le tableau compte les ennemis renversés. Les boss ne sont ni rangés ni projetés (ils prennent les dégâts) ; les volants ni l'un
  ni l'autre ; un ennemi déjà charmé (sirènes, boussole, baudruches) n'est pas rangé.

**À surveiller aux tests** : les quilles qui vont à leur place et y restent (le charme, le NavMesh sur les bords de la voie) ; la
boule qui suit bien la voie et le sol (les ponts, la rivière) ; les ennemis projetés en dehors des voies ; l'arche (4 × 3 cases,
les tours à 3,6 m de part et d'autre) ; le texte du tableau.

### 12.23 Le Volcan dans le code (1er novembre)

Le joueur : « oui, le Volcan ». Codé, compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement), modèle dessiné hors
de Unity sans faces superposées ; **pas encore vu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`, « Les trios »).

**Le Volcan** (`Building/TrioVolcan.cs`, Feu d'artifice + Tremplin, sur la voie, 4 × 4 cases) : un petit volcan sur la voie, six
gradins de roche noire fendue de lave, une ceinture dorée, le cratère et sa lave, et une couronne de huit fusées dorées (le feu
d'artifice) ; niveau 2 : des coulées de lave sur ses flancs, des fumerolles ; niveau 3 : dix grandes fusées à deux étages autour
de sa base, et quatre obélisques dorés coiffés de flammes. Les réglages sont en tête du script :
- niveau 1 : toutes les 6 s (5 ; 4,5), s'il y a quelqu'un sur le cône (4 m), **l'éruption** : ceux qui sont dessus partent en
  **bombes de lave** (« Bombe de lave ! », 14 m/s vers le haut, en tournoyant, une traînée de braises), **guidées** vers le
  groupe le plus serré à 14 m (comme la rampe de lancement) ; à l'impact, une explosion (2,5 m, 35 / 45 / 55 de dégâts, renversés)
  et une flaque de lave en feu 2 s ; le panache monte 3 s au-dessus du cratère : **ta flèche qui le traverse s'enflamme** (comme au
  brasero de la tour) ; une lueur rouge à chaque éruption ;
- niveau 2 : un anneau de lave en feu autour du cône (5 m), refait toutes les 5,5 s ; les bombes laissent leur lave 5 s ;
- niveau 3, **la grande éruption** : toutes les 20 s, avec quatre ennemis sur la voie vers la forêt, une éruption, puis la
  montagne grandit (×2,2), la lueur rougit, et **une coulée de lave descend la voie à contre-sens** (30 m à 6 m/s, 6 m de large) :
  ceux qu'elle touche prennent 60 de dégâts et sont repoussés vers la forêt, en l'air ; elle laisse de la lave en feu derrière elle
  (6 s) ; la montagne redescend ensuite.
- Les boss ne sont jamais projetés ni repoussés (ils prennent les dégâts) ; les volants pas touchés.

**À surveiller aux tests** : les ennemis qui traversent la montagne (les bâtiments n'ont pas de collider : ils marchent dans le
cône jusqu'à l'éruption) ; les bombes qui retombent bien sur un groupe ; les flaques (le matériau de la lave du modèle) ; la
coulée qui suit la voie ; la lueur (une lumière en plus : le Quest).

### 12.24 Le Beffroi dans le code (1er novembre)

Le joueur : « fais le Beffroi » (avant d'essayer les quatre premiers). Codé, compilé (le jeu et les outils d'éditeur, sans erreur
ni avertissement), modèle dessiné hors de Unity sans faces superposées ; **pas encore vu dans Unity ni testé au casque** (tests :
`Checklist_Demo.md`, « Les trios »).

**Le Beffroi** (`Building/TrioBeffroi.cs`, Boussole folle + Phare, au bord, 3 × 3 cases) : une tour de pierre de 11 m, deux grands
cadrans dorés (devant et derrière) aux aiguilles qui tournent à l'envers, un clocher ouvert et sa cloche de bronze qui se balance,
un toit d'ardoise en gradins et sa flèche dorée, la rose des vents de laiton à son pied ; niveau 2 : quatre pinacles dorés et une
lanterne (le phare) au sommet ; niveau 3 : un croissant de lune doré et une étoile au bout de la flèche, les douze heures dorées
autour des cadrans. Les réglages sont en tête du script :
- **le chemin** : le beffroi note la position des ennemis au sol à moins de 23 m, tous les dixièmes de seconde ;
- niveau 1 : toutes les 10 s (8,5 ; 7), s'il y a quelqu'un à 15 m, la cloche sonne : jusqu'à 12 ennemis **rembobinent** leurs
  4 dernières secondes (« << Rembobiné ! ») : tenus, ils repartent à reculons le long du chemin qu'ils viennent de faire, sans se
  retourner, en 1,2 s (une traînée sépia), puis sont lâchés là ; les aiguilles s'emballent à l'envers ;
- niveau 2 : à l'arrivée, ils restent figés 2 s (étourdis : « Figé ! », préparés pour tes flèches) ;
- niveau 3, **Minuit** : toutes les 30 s, avec cinq ennemis à portée, douze coups en 6 s (« Le temps s'arrête… », « Dong ! 1 » à
  « 12 ») ; dans un rayon de 20 m, les ennemis sont figés (gelés, sans glace ; un boss 2 s seulement) et **tes flèches s'arrêtent
  en l'air** (comme celles du fût suspendu) ; au douzième coup (« Minuit ! »), elles repartent toutes ensemble, à leur vitesse
  d'avant, et les ennemis reprennent.
- Les boss et les volants ne sont jamais rembobinés.

**À surveiller aux tests** : le rembobinage (l'ennemi tenu glisse sur son chemin, puis retombe sur le NavMesh) ; les aiguilles et la
cloche (leur sens) ; Minuit : les flèches arrêtées (cherchées deux fois par dixième de seconde parmi toutes les flèches de la
scène : le Quest), qu'elles repartent bien, et rien d'autre de figé par erreur.

### 12.25 Le Colisée dans le code (1er novembre)

Le joueur : « oui, le Colisée ». Codé, compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement), modèle dessiné hors
de Unity sans faces superposées ; **pas encore vu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`, « Les trios »).

**Le Colisée** (`Building/TrioColisee.cs`, Chant des sirènes + Barricade, sur la voie, 4 × 4 cases) : un petit amphithéâtre de
marbre ouvert sur la voie, deux gradins en arc de cercle de part et d'autre (trois rangs, une colonnade aux chapiteaux dorés), la
sirène de bronze sur sa colonne, couronnée de laurier doré (chaque champion en porte une copie), et un pan de mur à l'arche rouge
(les murs de l'arène en sont des copies) ; niveau 2 : quatre oriflammes rouges ; niveau 3 : deux mâts dorés et une grande
couronne de laurier suspendue au-dessus de la voie. Les réglages sont en tête du script :
- niveau 1 : toutes les 15 s (13 ; 12), s'il y a moins de champions que permis, **une arène** s'élève (16 pans de mur qui sortent du
  sol, 4 m de rayon) autour du groupe le plus serré à 16 m : jusqu'à 5 ennemis, charmés, se battent (« Gladiateurs ! ») ;
  chacun court sur son voisin et le frappe (20 % de ses PV max par coup, toutes les 0,7 s), sans sortir de l'arène ; **ta flèche
  élimine le gladiateur qu'elle touche** (« Éliminé ! », l'élimination compte pour toi) ; le dernier debout (ou le plus en forme au
  bout de 10 s) devient **champion** (« Champion ! », la couronne de laurier au-dessus de sa tête) : il ne prend plus aucun dégât,
  court sur l'ennemi le plus proche et le frappe (30 / 40 / 50) ; sans personne, il marche vers la forêt ; il tombe au bout de
  30 s, à la fin de la vague, ou quand il ne reste plus que des champions (sinon la vague ne finirait pas) ;
- niveau 2 : deux champions à la fois ; un champion qui tombe éclate en bulles roses qui hypnotisent 3 voisins (6 m), qui se
  battent 5 s sans arène (« Hypnotisés ! ») ;
- niveau 3, **les jeux du cirque** : toutes les 30 s, avec six ennemis à portée, une arène de 10 m (28 pans, plus hauts) avale
  jusqu'à 14 ennemis ; le vainqueur devient **le grand champion** (×3, ses coups ×3 et ils renversent).
- Les boss ne sont jamais gladiateurs (l'arène les ralentit de moitié) ; les volants pas touchés ; un ennemi déjà charmé non plus.

**À surveiller aux tests** : l'arène (les pans qui montent et redescendent, posés sur le sol) ; les gladiateurs qui se battent
vraiment (le charme, le NavMesh dans l'arène) ; le champion (qu'il ne prenne aucun dégât, qu'il ne bloque pas la fin de la vague) ;
le grand champion (×3 : son NavMesh Agent, sa couronne).

### 12.26 Les Montagnes russes dans le code (1er novembre)

Le joueur : « oui, le dernier ». Codé, compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement), modèle dessiné hors
de Unity sans faces superposées ; **pas encore vu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`, « Les trios »).

**Les Montagnes russes** (`Building/TrioMontagnesRusses.cs`, Chaîne de forçats + Tremplin, sur la voie, 4 × 8 cases) : un circuit
de bois rouge et or le long de la voie : la gare de départ, les rails au ras de la voie, un looping de 5,2 m (ses piliers, ses
rayons dorés), la gare d'arrivée, et un wagon de fer à l'aimant rouge garé au départ (il sert de modèle) ; niveau 2 : une guirlande
de lampions d'une gare à l'autre par le haut du looping, une étoile dorée ; niveau 3 : le tremplin au-delà de la gare d'arrivée
(jusqu'à 5 m de haut, sur ses étais) et son arche dorée. **Le chemin des wagons est calculé dans le code avec les mêmes cotes que le
modèle** (constantes `k_…` en tête du script, `MR_…` dans `Tools/allure_tours/trios.py`) : changer l'un, c'est changer l'autre.
Les réglages sont en tête du script :
- niveau 1 : toutes les 6 s (5 ; 4,5), s'il y a des ennemis près du circuit, un wagon sort de la gare (11 m/s) ; au ras de la
  voie, il **ramasse** jusqu'à 4 ennemis à 1,6 m (« Embarqué ! », 15 / 20 / 25 de dégâts : ils sont tenus sur ses places) ; dans le
  looping, ils font le tour avec lui et, **en haut, la tête en bas, ils tombent** (« Au vol ! » : la chute les blesse) ; le wagon
  rentre en gare d'arrivée et disparaît ;
- niveau 2 : un train de trois wagons (12 places) ; ceux qui tombent sont guidés vers le groupe le plus serré à 15 m et, en
  retombant, blessent (25) et renversent ceux qui sont autour ;
- niveau 3, **le grand huit infernal** : toutes les 25 s, avec six ennemis près du circuit, un train de dix wagons (plus vite)
  garde ses passagers dans le looping, file sur le tremplin et **saute dans le vide** (« Dans le vide ! ») vers le groupe le plus
  serré à 30 m : les wagons s'y écrasent (« CRASH ! », une explosion ; chacun blesse et renverse à 4 m), leurs passagers avec eux,
  et **l'atterrissage les tue**.
- Les boss et les volants ne montent jamais.

**À surveiller aux tests** : que le circuit soit bien posé le long de la voie (le chemin suit l'orientation du bâtiment : z le long
de la voie, comme le Toboggan) ; les passagers sur leurs places (tenus 8 s au plus, relancés à chaque image) ; la chute du haut du
looping ; le saut du grand huit (la cloche des wagons, les passagers qui retombent au même endroit) ; la place sur la voie (4 × 8
cases, le tremplin dépasse au niveau 3).

### 12.27 Le premier essai des trios (1er novembre au soir)

Le joueur a essayé les trios (F4). Ses retours, et ce qui en est fait (compilé, le jeu et les outils d'éditeur sans erreur ni
avertissement ; **pas encore revu dans Unity ni testé au casque**) :

- **Lanceur de baudruches** : « les baudruches devraient aussi attirer les volants et les tireurs ». Fait : les **volants** viennent
  tourner en rond serré au-dessus de la tête de la baudruche et la picorent en passant (`FlyingEnemy.Lure`, `Enemy.LureFlyer` : ils
  ne piquent plus sur le joueur tant qu'elle tient) ; les **tireurs** s'arrêtent à 7 m d'elle et lui tirent dessus (un projectile sans
  dégâts : le coup compte quand il arrive ; `Enemy.ShootAt`, `EnemyProjectile.LaunchAt`). Les deux sont attirés de plus loin que les
  autres (×2,5 pour les volants, ×2 pour les tireurs : réglages *Flyer Lure Factor*, *Ranged Lure Factor* ; pas le char de carnaval,
  qui attire déjà à 40 m) ; les confettis blessent aussi les volants qui tournent autour.
- **La cause commune, trouvée en passant** : un tireur charmé gardait son arrêt à 80 % de sa portée de tir (17 m). Il courait vers
  la baudruche, l'arène ou sa proie, s'arrêtait à 17 m et restait là, à regarder. Charmé, il s'arrête maintenant tout près, ou à la
  distance que le bâtiment lui donne (`Enemy.Charm`, nouveau paramètre *stopAt*), et retrouve son arrêt à la fin du charme. Cela
  vaut pour tous les duos et trios qui charment (Chant des sirènes, Boussole folle, Bowling).
- **Colisée** :
  - « les vainqueurs se battent entre eux sans pouvoir se tuer après être sortis chacun de leur arène » : chaque Colisée ne
    reconnaissait que ses propres champions ; ceux d'un autre Colisée (la vitrine en pose trois) étaient des proies, et des proies
    qui ne prennent aucun dégât. Les champions de tous les Colisées sont maintenant du même camp (une liste commune) : ils ne se
    chassent plus, et la vague finit quand il ne reste qu'eux, d'où qu'ils viennent.
  - « les tireurs ne tirent pas sur les ennemis et restent à regarder le joueur de loin » : la cause ci-dessus. Un **gladiateur
    tireur** se tient au milieu de l'arène et tire sur son voisin le plus proche ; un **champion tireur** s'arrête à 8 m de sa proie
    et lui tire dessus (10 m de portée, 15 pour le grand champion ; réglage *Ranged Reach*). Le coup porte quand le projectile arrive.
  - « il n'y a pas de guerrier en armure dorée » : le grand champion n'était que grandi (×3) et couronné. Il est maintenant **doré
    de la tête aux pieds** : chacun de ses matériaux est copié, teint d'or, rendu métallique et lumineux (rendu à la fin) ; « Le
    guerrier d'or ! » s'affiche devant le joueur, avec une plus grande gerbe d'or.
- **La vitrine (F4)** : au premier appui, elle pose aussi une **défense aérienne qui reste** (un lance-filet et une baliste au
  niveau 5 dans chacune des deux clairières, à 11 m de la tour, que F4 ne retire jamais) et donne **20 000 PV au joueur et à la
  tour**, remontés si une amélioration de Vitalité les change.
  Depuis le 2 novembre au soir (le joueur : « laisse juste les défenses uniquement anti-aériennes ») : plus de baliste, qui tirait
  aussi sur les ennemis au sol, et le lance-filet au niveau 3 (le barillet) au lieu de 5 : au niveau 4, son filet électrique vise
  aussi les ennemis au sol. Les bâtiments montrés ont les ennemis au sol pour eux seuls.
- **Volcan** : « tout le concept est à revoir ». Propositions ci-dessous, **à trier** ; rien n'est recodé.
- **Montagnes russes** (le « manège » du joueur) : « il ne semble prendre qu'un ennemi sur 30 », le modèle et les ennemis dans les
  wagons sont bugués, l'aspect est à revoir, et « ce n'est pas logique que les ennemis reviennent vers le joueur » ; le concept doit
  rester un peu différent du Toboggan. **Pourquoi un sur 30** : le wagon ne ramasse que sur 8 m de rails (de la gare au looping),
  dans un rayon de 1,6 m autour d'une seule ligne au milieu du bâtiment, en passant à 11 m/s (0,7 s) ; sur une voie de 4 à 8 m de
  large, presque tous passent à côté. Et le circuit va de la gare (côté forêt) vers l'arrivée (côté tour) : les passagers sont
  avancés vers le joueur. Propositions ci-dessous, **à trier** ; rien n'est recodé.

**Le Volcan, trois propositions (Claude, 1er novembre)** — Feu d'artifice + Tremplin, toujours :

| Volcan | Niveau 1 | Niveau 2 | Niveau 3 : la démesure | Allure | À coder |
|---|---|---|---|---|---|
| **Le Réveil** *(conseillé)* | un volcan endormi au bord de la voie, qui se **remplit** : chaque ennemi tué à 15 m et chaque flèche tirée dans son cratère le font gronder un peu plus (le cône rougeoie, la fumée monte, une jauge de lave dans ses fissures). **C'est toi qui choisis le moment** : ta flèche dans le cratère le fait exploser, et plus il est plein, plus l'éruption est grosse (jusqu'à 12 bombes de lave sur les groupes à 25 m, qui explosent et laissent de la lave en feu) | il se remplit deux fois plus vite ; l'éruption secoue le sol : tous les ennemis à 10 m sont renversés | **Le cataclysme** : plein à ras bord, il n'attend plus : le ciel rougit, la montagne grandit sous tes yeux, 40 bombes tombent sur toutes les voies, et une coulée descend chaque voie vers la forêt | un cône noir fendu de lave, la jauge qui monte dans ses fissures, des fusées dorées plantées au bord du cratère comme des mèches | moyen |
| **L'Obsidienne** | la lave coule en travers de la voie et **durcit en murs d'obsidienne** : les ennemis doivent faire le tour (le seul bâtiment qui change leur chemin sur la voie même) ; ceux qui sont dans la lave quand elle durcit restent prisonniers d'obsidienne 4 s (préparés : ta flèche les fait éclater) | les murs sont brûlants : les longer brûle | **La muraille** : une muraille de 20 m ferme toute la voie 10 s ; ses ennemis rebroussent chemin vers une autre voie | un volcan bas au bord de la voie, des orgues de basalte, des coulées figées et luisantes | lourd (le NavMesh change pendant la partie) |
| **Le Soulèvement** | le sol de la voie se **soulève** sous un groupe : un plateau de roche de 6 m monte à 5 m avec les ennemis dessus, qui ne peuvent plus en descendre (des cibles en hauteur, au vol), puis il s'effondre dans la lave (gros dégâts) | le plateau en jette au bord : ils tombent de 5 m | **La montagne** : 30 m de voie se soulèvent en montagne ; tout glisse vers la forêt en dévalant la pente, et elle redescend | une faille fumante au bord de la voie, des blocs de roche hérissés de fusées | lourd |

**Les Montagnes russes, trois propositions (Claude, 1er novembre)** — Chaîne de forçats + Tremplin, toujours ; elles emmènent
les ennemis **vers la forêt**, jamais vers le joueur :

| Montagnes russes | Niveau 1 | Niveau 2 | Niveau 3 : la démesure | Allure | À coder |
|---|---|---|---|---|---|
| **Le Grappin** *(conseillé)* | un circuit fermé, un grand ovale sur pylônes à 5 m au-dessus de la voie, dans son axe ; le train y **tourne sans arrêt**. Sous chaque wagon pend une chaîne et un **grappin aimanté qui se balance d'un bord à l'autre de la voie** : il accroche tout ce qu'il frôle (3,5 m : on ne passe plus à côté), et l'ennemi pend sous le wagon, ballotté dans les virages (« Accroché ! »). Les wagons ne ramassent que sur la branche qui va vers la forêt ; au virage du fond, la force centrifuge les décroche : ils volent 15 m plus loin vers la forêt (« Au vol ! ») | les accrochés d'un wagon sont enchaînés entre eux : une grappe qui fouette, au passage, les ennemis sous le circuit (dégâts, renversés) | **Le grand huit infernal** : le train accélère, fait un looping géant au bout du circuit et s'en détache avec tous ses accrochés, droit sur le groupe le plus serré : crash, l'atterrissage tue | une charpente rouge et or, des lampions, des wagons de fer, les grappins dorés au bout de leurs chaînes | moyen |
| **La Grande roue** | une grande roue de 14 m qui enjambe la voie ; ses nacelles **ramassent les ennemis en bas** et les **hissent** (des cibles lentes, en l'air : « Au vol ! ») ; en haut, la nacelle bascule vers la forêt et les vide 15 m plus loin | les nacelles sont enchaînées : en tombant, ceux d'une nacelle entraînent ceux de la suivante, ils retombent en grappe sur un groupe | **La roue folle** : elle tourne à toute vitesse et vide toutes ses nacelles d'un coup sur toute la voie, très loin vers la forêt | une roue de fer peinte, des nacelles dorées, des ampoules | moyen |
| **Le Train fantôme** | un tunnel hanté posé sur la voie ; les ennemis qui y entrent sont pris par les wagons et ressortent du **côté de la forêt, terrifiés** : ils s'enfuient vers la forêt 5 s en hurlant, sans attaquer (« Terrifié ! » ; une mécanique nouvelle, la peur) | un terrifié fait fuir ceux qu'il croise | **Le grand frisson** : un fantôme géant sort du tunnel et toute la voie s'enfuit 6 s | une maison de foire noire et violette, une gueule de monstre en entrée, des chaînes et des squelettes dorés | simple |

**À trier par le joueur** : un Volcan et une version des Montagnes russes (ou ses propres idées). Puis : les recoder, et revoir
les retours ci-dessus dans Unity (F4).

### 12.28 La Grande bombarde, à la place du Volcan (1er novembre au soir)

Le joueur : « code le Réveil, mais oublie complètement le volcan ». Il garde la mécanique du Réveil (une jauge qui se remplit, ta
flèche qui déclenche) avec une autre allure, choisie parmi deux (la Grande bombarde, ou un géant de pierre endormi). Codé, compilé
(le jeu et les outils d'éditeur, sans erreur ni avertissement), le modèle dessiné hors de Unity sans faces superposées ; **pas encore
vu dans Unity ni testé au casque**.

**Le Volcan est retiré** : le baker l'enlève de la liste des trios du catalogue (`retiredTrios` du fichier d'allure ; il ne se
fusionne plus et la vitrine ne le montre plus). Ses fichiers restent (`TrioVolcan.cs`, `Prefabs/Structures/Duos/Trio_Volcan`,
`Data/Duos/Trio_Volcan`), à supprimer quand le joueur le voudra.

**La Grande bombarde** (`Building/TrioBombarde.cs`, Feu d'artifice + Tremplin, **au bord**, 4 × 4 cases ; textes et chiffres
proposés) : un mortier de feu d'artifice en bronze de 7 m, posé sur quatre gros ressorts de cuivre (le tremplin : tout le corps
rebondit au tir). Sur son fût, cinq cercles de fer, la jauge ; à côté, sur un poteau, une cible ronde, la mèche, qui se tourne
vers le joueur, et son cordon jusqu'à la culasse ; trois bombes de feu d'artifice (bleue, rouge, verte) sur l'affût. Niveau 2 : le
baril de poudre et deux fanions dorés ; niveau 3 : la couronne dorée de la gueule et deux grandes fusées à deux étages.
- **La poudre** : chaque ennemi tué à 18 m (+8 %, pas ceux de ses propres bombes : elle se rechargerait toute seule) et chaque
  flèche tirée dans sa gueule (+12 %, « Poudre 40 % ») la remplissent. Les cercles rougeoient un à un (un grondement, de la
  fumée) ; de plus en plus de fumée et de braises montent de la gueule. Pleine : « Pleine ! Tire dans la mèche », le dernier
  cercle clignote.
- **La mèche** : ta flèche dans la cible l'allume (il faut 15 % de poudre au moins, et un ennemi à 30 m ; sinon « Pas assez de
  poudre », « Personne à portée », et la poudre reste). L'étincelle court le long du cordon (0,9 s), puis **la salve** : d'autant
  plus de bombes qu'elle est pleine (12 au plus au niveau 1, 14 au niveau 2, 16 au niveau 3), une toutes les 0,09 s, en cloche,
  sur le groupe le plus serré, puis le suivant… ; chacune explose en gerbe de couleur (3 m, 45 / 55 / 65 de dégâts) et projette
  ceux qu'elle touche (pas les boss). Le corps s'enfonce sur ses ressorts et rebondit.
- **Niveau 2** : elle se remplit deux fois plus vite ; au tir, la **secousse** renverse les ennemis à 10 m.
- **Niveau 3, le bouquet final** : pleine, elle n'attend plus ; « La bombarde déborde ! » 3 s, puis 40 bombes en 2,5 s, sur les
  groupes et au hasard sur les voies, à 60 m.

Trouvé en passant : un duo ou un trio **du bord** né d'une tour du bord et d'une tour de la voie se posait à la place de celle de la
voie, où la grille le refuse. Il prend maintenant la place de celle du bord (`BuildManager.DuoPlace`, la place du résultat). Et la
vitrine (F4) pose les duos et trios du bord aussi au niveau 3, à côté du niveau 1, dans la clairière de droite (avant : niveaux 1
et 2 seulement, le spectacle ne se voyait pas).

**À surveiller aux tests** : viser la gueule et la mèche depuis la tour (la gueule à 7 m de haut, une sphère de 1,2 m ; la cible
de 1,6 m) ; la jauge (les cercles qui s'allument, le matériau de braise) ; les bombes qui retombent sur les groupes ; le rebond sur
les ressorts ; le bouquet final (40 bombes et leurs explosions : le Quest).

### 12.29 Les Montagnes russes refaites : le Grappin (1er novembre au soir)

Le joueur choisit **le Grappin** parmi les trois propositions de 12.27. Codé, compilé (le jeu et les outils d'éditeur, sans erreur
ni avertissement), le modèle dessiné hors de Unity sans faces superposées ; **pas encore vu dans Unity ni testé au casque**. Le nom,
le prefab, la définition et le script restent ceux des Montagnes russes (`Building/TrioMontagnesRusses.cs`, réécrit) ; le baker
refait le modèle (une nouvelle passe, « Grappin du 1er novembre », `rebuild`) et les textes, s'ils n'ont pas été changés à la main.

**Le modèle** : un ovale de rails à 5 m au-dessus de la voie (deux lignes droites de 8 m, à 1,2 m de part et d'autre de l'axe,
reliées par deux virages), porté par trois portiques de bois aux poutres rouges et aux équerres dorées, et une enseigne de foire
rouge cerclée d'or, à ampoules, au-dessus du portique du milieu. Le wagon rouge et son treuil ; dessous, la chaîne et le grappin
aimanté à trois griffes dorées. Niveau 2 : des mâts à fanions dorés et des guirlandes de lampions le long du circuit ; niveau 3 :
un tremplin au bout de chaque ligne droite, ses étais, son arche dorée et son crâne.

**Ce qu'il fait** (réglages en tête du script, chiffres proposés) :
- **Le circuit tourne sans arrêt** : deux wagons (trois aux niveaux 2 et 3) à 6,5 m/s.
- **Le grappin** : sur la ligne droite qui va vers la forêt (le circuit peut être posé dans un sens ou dans l'autre : c'est revu
  toutes les 2 s), le treuil descend le grappin à 1,6 m du sol et il se **balance d'un bord à l'autre de la voie** (3,2 m de
  chaque côté de l'axe, un aller-retour en 1,3 s) ; il accroche les ennemis au sol à 1,8 m de lui (« Accroché ! », 15 / 20 / 25
  de dégâts ; 3 au plus, 4 aux niveaux 2 et 3). Plein, ou sur le reste du circuit, il remonte ; les accrochés **pendent** sous lui,
  en grappe, et se balancent (des cibles en l'air : le tir au vol).
- **Le virage du côté de la forêt** les jette vers la forêt (9 m/s, et vers le haut : ils retombent à une douzaine de mètres, en
  tournoyant ; « Au vol ! », la chute les blesse). Ils doivent refaire le chemin, et repasser sous le grappin.
- **Niveau 2, la grappe** : dès deux accrochés, ils fouettent au passage les ennemis à 2 m du grappin (« Fouetté ! », 20 de
  dégâts, renversés ; une fois par seconde chacun).
- **Niveau 3, le grand huit infernal** : toutes les 25 s, avec six ennemis près du circuit, un train de six wagons file à 13 m/s
  sur la ligne droite qui va vers la forêt, ses grappins balancés deux fois plus vite (et un peu plus larges), accroche tout, monte
  le tremplin et saute dans le vide vers le groupe le plus serré à 30 m : « CRASH ! », les wagons écrasent ce qui est dessous, les
  accrochés meurent à l'atterrissage.
- Les boss et les volants ne sont jamais accrochés.

**À surveiller aux tests** : le sens du circuit (la ligne droite qui ramasse doit aller vers la forêt) ; combien d'ennemis le
grappin prend (le joueur : « un sur 30 » avant) ; les accrochés qui pendent (leur position, pas sous le sol) ; le virage qui les
jette (qu'ils retombent bien sur la voie, vers la forêt) ; la chaîne (un LineRenderer au matériau de fer) ; la place sur la voie
(4 × 8 cases ; le circuit fait 12,4 m de long, 17 m avec les tremplins, et les portiques sont à 2,6 m de l'axe).

### 12.30 Les nouvelles tours validées, deux bugs du Colisée (1er novembre au soir)

Le joueur a essayé la Grande bombarde, le Grappin et les corrections de 12.27 : **« les nouvelles tours sont validées »**. Restaient
deux bugs du Colisée, corrigés (compilé, **pas encore revu dans Unity**) :
- **Les tourelles tiraient sur les champions** (sans leur faire de dégâts : leur protection annulait les coups). Un champion restait
  dans la liste des ennemis que visent les bâtiments. Il est maintenant **de ton côté** (`Enemy.IsAlly`, mis par le Colisée, retiré
  quand il tombe) : `Structure.AliveEnemies` et la baliste (`Ballista.FindTarget`) le laissent de côté, comme les mannequins. Tous
  les bâtiments passent par là : plus aucun ne le vise, ne le projette ni ne le marque.
- **Des ennemis figés après l'arène** (le joueur pensait à un ennemi mort en même temps que couronné). La cause : quand un Colisée
  disparaît (la vitrine F4 passe au suivant, une fusion), ses champions perdaient leur couronne et leur protection, mais **gardaient
  leur charme** (jusqu'à 31 s) : ils couraient vers leur dernière proie, puis restaient plantés là. Ils redeviennent maintenant des
  ennemis tout de suite (`Dismiss` les libère).

### 12.31 Le scaling des bâtiments (1er novembre au soir)

Le joueur : avec les seules tourelles de la vitrine (F4 : deux balistes et deux lance-filets au niveau 5), il est allé jusqu'à la
vague 45 ; « on ne peut juste pas perdre ». La cause : les dégâts de chaque bâtiment étaient multipliés par la force des ennemis
(leurs PV : difficulté et vague, mode infini compris, ×1,15 par vague puis ×1,44 en infini) ; un bâtiment gardait la même part de
leurs PV toute la partie, et seul leur nombre montait, plafonné à 40 à la fois. Maintenant, ils ne suivent plus que la **racine
carrée** de cette force (`Structure.Strength`, réglage *Structure Strength Exponent* des Wave Settings, 0,5 ; 1 : comme avant) :

| Vague (Normal) | PV des ennemis | Dégâts des bâtiments | Part des PV d'un ennemi |
|---|---|---|---|
| 1 | ×1 | ×1 | 100 % |
| 6 (fin de partie) | ×2,0 | ×1,4 | 71 % |
| 10 | ×8,6 | ×2,9 | 34 % |
| 15 | ×53 | ×7,3 | 14 % |
| 20 | ×323 | ×18 | 6 % |

Les dégâts « en part des PV max » de certaines fusions (les coups des gladiateurs, le char de carnaval, le cataclysme de la
Grande bombarde…) ne changent pas. Compilé, **pas encore essayé**. À noter : les essais avec F4 donnent 20 000 PV au joueur et à
la tour ; pour juger l'équilibrage, regarder quand les tours sont débordées plutôt que quand la partie est perdue. Les dégâts de
base des tours ne sont pas touchés (les valeurs du joueur font foi) : la *Structure Power* de chaque difficulté les multiplie
tous (1 en Normal).

Puis le joueur : « oui, baisse-les ». **Tous les dégâts des bâtiments baissent de 20 %**, duos et trios compris, par la *Structure
Power* : 0,75 → 0,6 (Facile), 1 → 0,8 (Normal), 1,25 → 1 (Difficile), 1,6 → 1,28 (Impossible). Fait par
`Editor/StructurePowerBaker` à l'ouverture de Unity (une fois, seulement si la valeur est encore l'ancienne ; la console l'écrit).
Avec la racine carrée, à la vague 6 en Normal, un bâtiment fait ainsi 57 % de la part des PV d'un ennemi qu'il faisait avant ;
à la vague 10, 27 %. Les dégâts par niveau de chaque tour (leurs prefabs) ne changent pas : si les niveaux 4 et 5 restent trop
forts par rapport aux premiers, c'est eux qu'il faudra revoir.

### 12.32 Le tableau d'équilibrage des tours : la demande (1er novembre au soir ; fait le 2 novembre, 12.33)

Le joueur : « les tours sont déséquilibrées les unes par rapport aux autres, il faudrait toutes les équilibrer ». Rien n'est
commencé : la session du 1er novembre n'avait plus assez d'utilisation pour le faire. **La demande, telle quelle** :

**Un fichier Excel** (`Project Archery/Docs/Equilibrage_Tours.xlsx` ; pas d'accent dans le nom), une ligne par **niveau** de chaque
tour, et des colonnes :
1. la tour, le niveau ;
2. **ce que fait la tour** à ce niveau (en une phrase : sa cible, son effet, ce que le niveau ajoute) ;
3. **le DPS sur une cible** (dégâts par seconde sur un seul ennemi), **une colonne par vague** : vagues 2, 3, 4, 5, 8, 10, 12 et 15 ;
4. **le DPS de zone** (sur un groupe : les dégâts par seconde sur tous les ennemis touchés, en supposant un groupe type, à dire
   dans l'en-tête, par exemple 5 ennemis serrés), une colonne par vague aussi ;
5. **l'utilité de contrôle** (CC : ralentir, figer, étourdir, projeter, renvoyer en arrière, attirer, prendre au filet), pour les
   tours qui en font : une estimation chiffrée (par exemple des secondes de chemin perdues par ennemi et par seconde, ou des
   « ennemis-secondes » de contrôle par seconde), avec l'unité dans l'en-tête ;
6. **à la fin de chaque tour, une ligne de plus**, avec deux colonnes : **« Changements proposés (Claude) »** (ce qu'il faudrait
   changer pour équilibrer cette tour par rapport aux autres : les chiffres précis) et **« Changements du joueur »** (vide : le joueur
   y écrit les siens s'il veut modifier ceux de Claude).

Une feuille de plus, conseillée : la méthode et les hypothèses (la formule des vagues, la taille du groupe, ce qui n'est pas compté).

**Les 10 tours** (les seules que donnent les caisses : `retired` décoché dans `Data/Structures`) : Barricade, Piques, Tremplin,
Baliste, Mortier, Tour de givre, Lance-filet, Tour-aimant, Bulle, Phare ; **5 niveaux** chacune (la catapulte, le pendule : leurs
niveaux 3). Leurs réglages par niveau sont dans leurs prefabs (`Prefabs/Structures/Barricade, Pieges, Tremplin, Baliste, Mortier,
Givre, Filet, Aimant, Bulle, Phare.prefab` : `m_DamagePerLevel`, `m_IntervalPerLevel`, portée, rayon…) et leurs scripts
(`Scripts/Building/`) ; les évolutions des niveaux 4 et 5 sont décrites en 12.10. **Les valeurs des prefabs font foi**, pas les
chiffres de ce document. Les duos et les trios ne sont pas demandés (ils pourront suivre).

**La formule des dégâts à une vague** (Normal) : dégâts de base × `Strength`, avec `Strength = (enemyHealth × EnemyScaling.Health)
^ 0,5 × structurePower` (`Structure.Strength`, 12.31 ; *Structure Power* 0,8 en Normal depuis le 1er novembre au soir, *Structure
Strength Exponent* 0,5 dans les Wave Settings). `EnemyScaling.Health` est réglé au début de chaque vague par `WaveManager.StartNextWave`
: `Growth(vague, healthGrowthPerWave, endlessHealthGrowth) × EarlyStrength(vague) × lerp(1, HealthCurve(vague), waveCurve)`
(`Data/Difficulties/Difficulty_Normal.asset`, `Data/Waves/WaveSettings.asset`, 6 vagues puis le mode infini). Pour comparer les
tours entre elles, donner aussi le DPS **en part des PV d'un ennemi type** de la vague (ses PV : `Data/Enemies`, × la même force
sans racine carrée) : c'est ce qui dit si une tour suit ou décroche. Ne pas oublier : le soutien du phare (+20 % de dégâts aux
bâtiments de son rayon, la cadence au niveau 2), la moitié des bonus de l'arc pour la baliste, les dégâts de chute des ennemis
projetés (la règle de la chute, `Enemy.Launch.cs`), le bonus des ennemis préparés (12.12 : il ne compte que pour les coups du joueur).

**La méthode conseillée** (pour économiser l'utilisation) : un script Python (`Tools/equilibrage_tours.py`, à créer) qui lit les
valeurs dans les prefabs (YAML), calcule la force de chaque vague avec les assets de difficulté et de vagues, et écrit le fichier
Excel ; puis seulement, à la main, les colonnes « ce que fait la tour », l'utilité de contrôle et les changements proposés. Python
3.13 est installé (`py`) avec numpy, Pillow et python-pptx ; **openpyxl n'y est peut-être pas** : vérifier, et demander au joueur
avant de l'installer (ou passer par la compétence Excel de Claude si elle est là). Rien n'est à changer dans le jeu avant que le
joueur ait rempli sa colonne : les changements se feront ensuite par un baker (les prefabs, une fois, seulement les valeurs encore
à l'ancienne).

### 12.33 Le tableau d'équilibrage, fait (2 novembre)

Fait par Claude (l'autre compte) le 2 novembre, d'après 12.32 ; le joueur a demandé en cours de route d'y ajouter **les duos et les
trios**, dans le même fichier. **Rien n'est changé dans le jeu** : ce sont des chiffres et des propositions.

**Les fichiers** :
- `Docs/Equilibrage_Tours.xlsx` : la feuille **Méthode** (les formules, les hypothèses, ce qui n'est pas compté, le constat), la
  **Synthèse** (une ligne par niveau des 30 bâtiments, les chiffres qui comparent, triables), **Tours**, **Duos**, **Trios** (une
  ligne par niveau, puis la ligne « Équilibrage » : les changements proposés par Claude, en jaune pâle, et « Changements du
  joueur », en jaune, vide), **Vagues** (la force des bâtiments et les PV de l'ennemi type, vague par vague, en formules : changer
  un réglage en bleu change tout le tableau), **Ennemis**.
- `Tools/equilibrage_tours.py` : lit les réglages par niveau dans les prefabs (YAML), la difficulté Normal, les Wave Settings et les
  ennemis, calcule chaque niveau et écrit le fichier. **Les valeurs des prefabs font foi** : après un changement, relancer
  `py Tools/equilibrage_tours.py` (depuis `Project Archery/`) ; la colonne du joueur est relue et gardée. `--print` affiche les
  chiffres sans écrire le fichier. Il faut openpyxl (`py -m pip install openpyxl`, installé sur ce PC le 2 novembre).

**Les colonnes** : par vague (2, 3, 4, 5, 8, 10, 12, 15), le DPS sur une cible, la part des PV d'un ennemi type par seconde, le DPS sur
un groupe de 5 ennemis serrés, les ennemis types tués par seconde ; puis le contrôle (secondes de chemin perdues par seconde), le
soutien (phare, Chaîne de forçats), un ennemi seul et un groupe qui passent (vague 5), et les données de la ligne : la portée,
l'exposition, les dégâts de base et la part des PV max par seconde (sur une cible, sur le groupe), le calcul. **Le DPS à la vague v =
dégâts de base × force des bâtiments (v) + part des PV max × PV de l'ennemi type (v)** : la première partie suit la racine carrée de
la force des ennemis (12.31), la seconde (chutes, éclats de glace, le choc du Boulet fou, les morts de la rampe) suit leurs PV.
Les hypothèses (combien d'ennemis un coup touche, la durée d'un cycle, un débit de 2 ennemis par seconde au plus pour les tours qui
éjectent…) sont dans la feuille Méthode et dans la colonne « Calcul » de chaque ligne.

**Ce que dit le tableau** (vague 5, Normal) :
1. **Les fusions font perdre de la force** : la plupart des duos et des trios au niveau 1 font moins de dégâts que ce qu'ils
   remplacent (le Marteau-pilon tue 0,23 ennemi par seconde, la barricade et les piques au niveau 3 en tuaient 4 ; l'Enclume 0,19, à
   la place d'un mortier au niveau 3 qui en tuait 1,7). Seuls le Boulet fou, le Toboggan, l'Avalanche et la Boussole folle s'en
   sortent, par leur contrôle. Règle proposée : un duo au niveau 1 vaut au moins ses deux tours au niveau 3, un trio au niveau 1 au
   moins son duo et sa tour.
2. **La rampe de lancement** (Tremplin niveau 5) tue chaque lancé, quelle que soit la vague : 3,9 tués par seconde à la vague 5, 2,3
   à la vague 15 (le mortier au niveau 5 : 0,6). Elle ne décroche jamais.
3. Entre les tours : la baliste est trop faible aux niveaux 1 et 2 (moins qu'un mortier, même sur une cible), puis le titan triple
   son niveau 5 ; le mortier fait un bond au niveau 3 ; les niveaux 4 de l'aimant, de la bulle et du phare n'aident que les flèches du
   joueur ; le lance-filet ne fait presque rien au sol.
4. **Un défaut** : le verglas de la tour de givre (niveau 4) fait glisser l'ennemi en avant, dans le sens de sa marche
   (`FrostTower.Slip`, 4 m/s pendant 0,7 s) : il gagne du chemin.
5. **Une erreur de 12.31** (et du GDD, corrigé) : la courbe vague par vague (×1,8) vaut aussi au-delà de la vague 6 ; les ennemis ont
   ×15,5 PV à la vague 10 (pas ×8,6), ×32 à la vague 12.

**Les changements proposés** (le détail, chiffré, est dans la ligne « Équilibrage » de chaque bâtiment) : la baliste plus forte aux
niveaux 1 à 3 et un titan moins fort ; le mortier moins fort aux niveaux 3 à 5 ; la rampe toutes les 4 s et 4 ennemis au plus ; le
verglas vers la forêt ; l'aimant, le lance-filet au sol, le phare au niveau 4 un peu plus forts ; rien pour la barricade, les piques,
la bulle (sauf son niveau 1). Duos : Marteau-pilon, Feu d'artifice, Chant des sirènes, Lanceur de baudruches, Boule à neige,
Carquois géant nettement plus forts ; la Loupe tire dès le niveau 1 ; les niveaux 3 de l'Avalanche, du Yo-yo, du Toboggan revus ; la
Chaîne de forçats partage moins (×3,4 des dégâts sur 5 liés : le soutien le plus fort du jeu) ; rien pour le Boulet fou. Trios :
tous plus forts, de deux à dix fois (l'Enclume, le Gonfleur et le Beffroi le plus) ; le Colisée n'est plus bloqué par ses champions
(une arène toutes les 30 s au niveau 1). Certains changements sont à coder (le verglas, le ralentissement de l'aimant, le nombre de
passagers du Toboggan par niveau, la baliste de la Loupe au niveau 1, le tir seul du Carquois, la cloche du Beffroi qui blesse).

**La suite** : le joueur lit le tableau et remplit sa colonne « Changements du joueur » (ou écrit « OK » sous ceux de Claude). Puis :
un baker (`Editor/`, comme `StructurePowerBaker`) qui change les prefabs une fois, seulement les valeurs encore à l'ancienne ; le
code des changements « à coder » ; relancer le script pour vérifier les nouveaux chiffres ; et l'essai au casque (F4). Ensuite :
les ultimes.

### 12.34 Le plafond d'ennemis à la fois : 150 (2 novembre)

Le joueur : « passe la limite d'ennemis à 150 en même temps ». Le plafond des Wave Settings (*Max Alive Cap*) passe de 40 à 150
(`Editor/EnemyCapBaker`, une fois, seulement si la valeur est encore 40 ; la valeur par défaut du script aussi). Le maximum de chaque
vague ne change pas (`WaveManager.MaxAlive` : 12 × le nombre d'ennemis de la difficulté × la racine carrée de la hausse de leur
nombre) : en Normal, 17 à la vague 5, 29 à la vague 10, puis 43 à la vague 13 (40 avant), 56 à la vague 15, 108 à la vague 20, 150
à partir de la vague 23. Rien ne change donc avant la vague 13. Le nombre total d'ennemis d'une vague ne change pas non plus : il
suit le budget de la vague, le plafond ne fait que les laisser venir plus nombreux à la fois. Les boss gardent leur propre limite
pour les ennemis qu'ils invoquent autour d'eux (pas s'il y en a déjà 10 en vie, `Boss.m_MaxEnemiesAlive`).

Le tableau d'équilibrage (12.33) montre le maximum de chaque vague (feuille « Vagues », dernière colonne) ; ses chiffres de tours
n'en dépendent pas (ils comptent un groupe de 5), mais des groupes plus gros avantagent les tours de zone, les pièges et la rampe.
**À mesurer** : les performances au-delà de 40 ennemis (tous animés en permanence : `Audit_Performance_Quest.md`, partie 4).

### 12.35 L'équilibrage appliqué (2 novembre)

Le joueur a rempli sa colonne du tableau : « si je laisse une case vide, je laisse tes corrections se mettre en place ». Tout est
fait, compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement), **pas encore vu dans Unity ni testé au casque** (tests :
`Checklist_Demo.md`, « L'équilibrage du 2 novembre »).

**Ses changements à lui** (les cases remplies) :
- **Piques** : en plus de l'intervalle proposé (0,8 / 0,7 / 0,6 s), **−30 % sur tous ses dégâts** (pointes 8,4 à 23,8, saignement 2,8,
  mine 42, mâchoire 84) : « il peut très bien synergiser avec une barricade pour faire des dégâts assez monstrueux ».
- **Tour-aimant** : la singularité toutes les **10 s** (proposé : 12), et « un poil plus stylée avec une vraie texture » : le trou noir
  a une texture (`Art/TowerDefense/Trou_noir.png`, dessinée par `Tools/texture_trou_noir.py` : des pôles noirs, un disque d'accrétion
  violet, magenta puis presque blanc à l'équateur, des bras en spirale qui tournent avec la sphère), en couleur et en émission.
- **Phare** : les changements proposés, et **l'anneau de feu** (niveau 4) plus grand (rayon 1,3 → 2,2 m) et **centré sur la voie** :
  le tracé des voies n'en suit pas toujours le milieu ; l'anneau se met maintenant au milieu, entre les bords du NavMesh de part et
  d'autre (`StructureLighthouse.CenterOnLane` ; s'il manque un bord à 12 m, il garde le tracé).
- **Feu d'artifice** : « ne buff pas les dégâts mais augmente légèrement les buffs que mettent ébloui » : les dégâts ne changent pas ;
  l'éblouissement (étourdi et préparé pour les flèches) dure 2,5 / 2,8 / 3,6 s (2 / 2,2 / 3 avant) et touche à 6 m (5). Lu ainsi par
  Claude : le bonus des ennemis préparés « Sonné ! » est commun à plusieurs tours, il n'est pas touché.
- **Bowling** : « la boule se met en place mais ne tire pas avant 2 secondes ; si le joueur tire dans la boule pendant ces
  2 secondes, le tir est plus gros et plus fort ». Fait : la boule apparaît sous l'arche, tourne sur elle-même et respire 2 s
  (« Tire dans la boule ! » les trois premières fois), puis roule ; ta flèche pendant ce temps : « Boule chargée ! », elle part
  aussitôt, 1,6 fois plus grosse et 1,75 fois plus forte (réglages *Windup Time*, *Charged Scale*, *Charged Damage*). Les quilles
  l'attendent (leur charme est prolongé). Pas pour le spare ni la partie parfaite ; une flèche dans une boule déjà lancée la givre
  toujours.
- **Beffroi** : sans les dégâts de la cloche ; pour le niveau 3, « une grande bulle s'étend très rapidement sur toute la map, et le
  monde devient noir et blanc à l'intérieur de la bulle qui arrête le temps ; l'arrêt du temps doit durer 9 secondes ». Fait : à
  Minuit, une bulle (le verre de la bulle, vue des deux côtés) part du beffroi et atteint 300 m en 0,8 s ; le temps s'arrête à son
  passage (ennemis, flèches) ; dès qu'elle a passé le joueur, l'image passe en noir et blanc (un volume global à part, saturation
  −100, au-dessus de celui de la carte, qui n'est pas touché) ; douze coups en 9 s, puis les couleurs reviennent. Les autres
  propositions du Beffroi sont faites (rembobinage de 8 s : la mémoire du chemin passe à 12,8 s ; toutes les 6 / 5 / 4 s ; figés
  3 s ; Minuit toutes les 20 s).
  Puis, à la demande du joueur, les messages de Minuit : « Za Warudo ! Temps, arrête-toi ! » quand le temps s'arrête (« Le temps
  s'arrête… » avant), « Le temps reprend son cours » quand il repart (« Minuit ! » avant).

**Les propositions de Claude, telles quelles** (les cases vides) : toutes celles des lignes « Équilibrage », sauf les deux qui ne
changeaient rien (Barricade, Boulet fou ; l'onde de la barricade toutes les 3 s n'était qu'une option, pas faite : elle aurait ajouté
des dégâts, à l'inverse de la remarque du joueur sur les piques et la barricade).

**Comment** :
- `Editor/BalanceBaker` : tous les réglages des prefabs (69), une fois, chacun seulement s'il a encore sa valeur d'avant ; aussi le
  matériau de la bulle de Minuit et la texture du trou noir. La console donne la liste.
- Le code : le verglas glisse vers la forêt à 2 m/s (`FrostTower`, *Slip Speed*) ; l'aimant ralentit de 25 % à partir du niveau 4
  (`StructureMagnet`, *Slow From Level*, *Slow Multiplier*) ; le Toboggan prend 3 / 3 / 5 passagers (`DuoToboggan`, *Max Riders Per
  Level*) ; la Loupe tire dès le niveau 1, 30 toutes les 2 s, puis 45 toutes les 1,6 s (`DuoLoupe`, *Bolt Damage / Interval Per
  Level* ; au niveau 1 le carreau part du haut de la tour, la baliste du modèle n'apparaissant qu'au niveau 2) ; le Carquois géant
  tire seul un carreau toutes les 2 s (`DuoCarquoisGeant`, *Auto Interval*) ; le Bowling et le Beffroi ci-dessus.
- Le tableau (`Tools/equilibrage_tours.py`) suit ces changements ; sa ligne « Équilibrage » dit ce qui a été fait, puis la
  proposition de départ. **Ses chiffres ne seront à jour qu'après le passage dans Unity** (les bakers) : relancer le script.

**À surveiller aux tests** : la bulle de Minuit (300 m : le plan de coupe de la caméra ; le noir et blanc au casque) ; la boule du
Bowling sous l'arche (les ennemis qui passent à travers pendant les 2 s) ; l'anneau du phare sur chaque voie ; l'équilibre lui-même :
le tableau est une estimation, les essais décident.

### 12.36 Les deux roguelikes liés : les parchemins valent pour les tours (2 novembre)

Le joueur, après l'équilibrage : les tourelles lui semblent plus fortes que le tir de base ; « on joue deux roguelikes en un, une
run sur notre arc et une run sur nos tourelles », sans lien entre eux. Deux pistes : les lier, ou gagner de deux façons (50 % du
temps chacune). Claude conseille de les lier (un jeu VR de tir à l'arc : une run « tourelles » qui gagne seule ferait du joueur un
spectateur ; deux voies à 50 / 50 sont très difficiles à tenir) ; le joueur choisit de **les lier**, puis donne son idée : **les
parchemins marchent pour le joueur et les tours**, et **on enlève le scale des tourelles par vagues**. Fait, compilé (le jeu et les
outils d'éditeur, sans erreur ni avertissement), **pas encore essayé** :
- **Force** (+12 % de dégâts au joueur par parchemin) : +12 % aux dégâts de tous les bâtiments, duos et trios compris
  (`Structure.ScrollStrength`, dans `Structure.Strength`).
- **Adresse** (+10 % de vitesse d'anneau) : +10 % de cadence à tous les bâtiments (`Structure.ScrollRate`, dans `OverchargeRate`, que
  suivent leurs minuteries).
- **Vigueur** (+25 PV au joueur, 12,5 % de ses 200 PV) : +12,5 % de PV à la tour (3 000 PV ; `PlayerUpgrades.Apply`) et aux
  barricades (`StructureBarricade`, aussi en cours de partie).
- Réglage commun : Wave Settings, *Structure Scroll Share* (1 : autant que le joueur ; 0 : rien).
- **Plus de suivi des vagues** : *Structure Strength Exponent* 0,5 → 0 (`Editor/BalanceBaker`, une fois). Un bâtiment ne grandit
  plus qu'avec ses niveaux, ses fusions et les parchemins du joueur. Ce qui compte en part des PV des ennemis (les chutes, le pic de
  glace, les éjections mortelles : la rampe, le saut à ski, l'orbe céleste…) les suit toujours, et devient l'essentiel en mode
  infini.
- Un premier essai de Claude, remplacé par l'idée du joueur avant d'être essayé : les bâtiments suivaient la moitié de tous les
  bonus de dégâts de l'arc. Ce qui en reste : la baliste n'a plus sa part à elle des bonus de l'arc (`m_BowBonusShare`, retiré) ;
  elle suit les parchemins comme les autres.
- Le bilan de fin de vague (la console) donne aussi les multiplicateurs des parchemins pour les bâtiments.

**Ce que ça change** (le tableau, `Docs/Equilibrage_Tours.xlsx`, avec l'hypothèse d'un demi-parchemin de force et d'un demi
d'adresse par vague) : les dégâts des bâtiments, cadence comprise, valent ×1,2 à la vague 5 (×1,4 avant, avec le suivi des vagues),
×1,8 à la vague 10 (×3,2), ×2,5 à la vague 15 (×7,8), pendant que les PV des ennemis font ×3, ×15 et ×95. Les tours seules
décrochent vite en mode infini ; investir dans les parchemins les fait tenir, et la feuille « Vagues » montre l'effet d'une run
riche ou pauvre en parchemins (ses colonnes en bleu). **À surveiller aux essais** : le message « Défenses : X % des dégâts » de fin
de vague (le but proposé : 30 à 70 % pour les bâtiments) ; si les tours décrochent trop tôt, remettre un peu de suivi des vagues
(*Structure Strength Exponent* 0,25) ou monter *Structure Scroll Share*.

**La suite proposée** (Claude, à trier) : les deux autres leviers de la discussion. Les tours préparent, le joueur achève (moins de
dégâts directs aux niveaux 1 à 3, plus de bonus sur les ennemis préparés, moins d'or pour une élimination d'un bâtiment) ; et des
récompenses à choisir entre l'arc et les tours (un coffre de boss : une pièce de flèche ou 3 plans).

**La puissance des bâtiments par difficulté** (le même jour) : le joueur demande si les vagues 1 à 5 restent bonnes par rapport à
l'ancienne méthode. Claude compare la force des tours (dégâts × cadence, un demi-parchemin de force et d'adresse par vague), nouvelle
sur ancienne : en Normal, 1,12 / 1,02 / 0,90 / 0,86 / 0,84 (vagues 1 à 5) ; en Facile, 1,20 à 1,37 ; en Difficile, 0,94 à 0,58 ; en
Impossible, 1,12 à 0,44. L'ancienne méthode suivait les PV des ennemis de la difficulté, qui grimpent bien plus vite en Difficile et en
Impossible (×6,6 et ×11,6 à la vague 5, ×3,2 en Normal). Le joueur valide la correction : *Structure Power* 0,6 → **0,5** (Facile),
0,8 (Normal, inchangé), 1 → **1,3** (Difficile), 1,28 → **1,9** (Impossible), par `Editor/BalanceBaker` (une fois, seulement les
valeurs encore à l'ancienne). Après : à la vague 5, 1,14 (Facile), 0,84 (Normal), 0,75 (Difficile ; 1,05 avec un parchemin de chaque
par vague, ces difficultés donnant plus d'or), 0,65 (Impossible ; 0,90). En Impossible, les tours sont plus fortes qu'avant au tout
début (×1,66 à la vague 1, où les ennemis sont encore faibles).

### 12.37 Lier encore l'arc et les tours (proposé par Claude, 2 novembre, à trier)

Le joueur : « même si les parchemins sont une bonne idée, je pense qu'il faut continuer de corréler ». Propositions de Claude,
dans les deux sens, en tenant compte des décisions passées : **les plans en boutique** ont été écartés le 29 octobre (12.2, 12.5),
et **tirer sur un bâtiment ne le déclenche pas pour tous** (réservé au talisman de l'ingénieur, 27 et 29 octobre). Aujourd'hui, une
élimination d'un bâtiment rapporte autant de points (donc d'or) que la tienne (`ScoreManager.OnEnemyKilled`).

**Les tours nourrissent l'arc** :
1. **L'assistance** : un ennemi tué par un bâtiment ne rapporte ses points (et l'or) en entier que si tu l'as touché dans les 5
   dernières secondes (« Assisté ! ») ; sinon, la moitié. Les tours n'enrichissent la run que si tu tires. *Simple.*
2. **Les emblèmes** : un bâtiment au niveau 5 (et chaque duo, chaque trio) donne à tes flèches un petit pouvoir de sa famille tant
   qu'il est debout, pris parmi les effets qui existent déjà : baliste, +1 ennemi traversé (*Piercing*) ; mortier, une petite
   explosion (*Explosive*) ; givre, ralentit (*Frost*) ; aimant, la flèche guidée (*Homing*) ; phare, la foudre en chaîne
   (*ChainLightning*)… On choisit ses tours aussi pour son arc. *Moyen* (un bonus de niveau sur des effets existants).

**L'arc nourrit les tours** (en plus des parchemins, 12.36) :
3. **Ton combo accélère les tours** : 10 touches d'affilée, +10 % de cadence aux bâtiments ; 25, +25 % ; un raté, retour à zéro
   (`ScoreManager.Combo`). Bien jouer à l'arc fait mieux tirer les tours. *Très simple.*
4. **Tes tirs parfaits surchargent** : un tir parfait (l'anneau vert) qui touche un ennemi surcharge 1 s les bâtiments à 12 m de
   lui (comme le talisman de l'ingénieur, en plus court). Tu tires sur les ennemis, pas sur les bâtiments : la règle du 29 octobre
   tient. *Simple.*
5. **Les pièces de flèche ont un écho sur les tours** : quelques pièces choisies changent aussi une tour (la corde triple : la
   baliste tire trois carreaux en éventail ; le détonateur : les obus du mortier rebondissent une fois…). *Lourd* (un écho par
   pièce) : à faire pour quelques pièces seulement.

**Une seule économie** :
6. **Le coffre de boss au choix** : une pièce de flèche ou 3 plans. *Simple.*
7. **Les caisses de l'ingénieur** donnent parfois un parchemin, **les coffres** parfois un plan. *Simple.*

**Les tours préparent, tu achèves** (la piste A du 29 octobre, poussée plus loin) :
8. **Le coup de grâce** : un ennemi que les bâtiments descendent sous 20 % de ses PV ne meurt pas d'eux ; il tombe à genoux 3 s
   (« À achever ! », préparé) ; ta flèche l'achève (points ×2), sinon il se relève. Les tours ne finissent plus seules : le lien le
   plus fort, et le plus risqué (les vagues peuvent s'entasser si le joueur ne suit pas). *Moyen.* À essayer après les autres.

**Conseil de Claude** : commencer par 1, 3 et 2 (l'assistance, le combo, les emblèmes) : les deux sens, peu de code, sans toucher
aux décisions passées ; puis 6 ; garder 8 pour un essai à part.

**Le tri du joueur** (le même jour) : il aime **6 et 7**, et va plus loin : **supprimer les deux sortes de caisses** (les coffres dorés
et les caisses de l'ingénieur) pour **un seul coffre**. **1, 2 et 3 non** : de bonnes idées, mais des règles de plus, alors que le jeu
est déjà complexe ; il cherche à le simplifier (sa règle de la complexité : GDD, section 1, « Le fil rouge » ; `CLAUDE.md`). 4, 5 et
8 tombent pour la même raison. Il a pensé à un arbre de talents qui débloquerait au fil des parties ce qui existe (commencer sans les
tourelles, puis la grenade, la glissade…), mais le trouve plus frustrant qu'autre chose. À décider ensuite : le coffre unique.

### 12.38 Le coffre unique (2 novembre)

Le joueur : « supprimer carrément les deux types de caisses différentes pour ne faire qu'un seul coffre » ; puis le contenu : **dans le
premier emplacement, un parchemin ; dans le deuxième, une tourelle ; dans le troisième, une chance sur deux de tourelle, une chance sur
deux d'amélioration** ; **le coffre de boss : deux améliorations au choix, ou une dernière orbe avec 3 plans aléatoires**. Fait, compilé
(le jeu et les outils d'éditeur, sans erreur ni avertissement), **pas encore essayé** :
- **Le coffre** (`ChestRewards.Roll`) : un parchemin ; une tourelle (un lot de plans, tiré comme dans l'ancienne caisse : la rareté
  d'abord) ; puis, avec la chance *Tower Chance* du prefab `Chest` (0,5), une autre tourelle (un autre bâtiment), sinon un objet (comme
  en boutique). Le fragment de l'arc légendaire peut remplacer le parchemin. Sans objet à donner : une tourelle ; sans tower defense :
  un objet. **Le bonus temporaire n'est plus proposé** (son tirage et ses couleurs sont retirés ; les bonus eux-mêmes restent dans le
  code, `PlayerUpgrades.AddBuff`).
- **Le coffre de boss** (`ChestRewards.RollBoss`) : deux objets, puis une orbe violette « 3 plans, de tourelles au hasard »
  (*Boss Plans*, 3) : trois plans, chacun d'un bâtiment tiré au hasard (la rareté d'abord) ; le message les nomme.
- **Une seule sorte de coffre** : le générateur des caisses (`CrystalSpawner`, ses emplacements et son nombre : 3 au début de la
  vague, un de plus toutes les 2 vagues, 6 au plus) pose maintenant le même coffre doré (« 3 coffres sont apparus ! ») ; le Chest
  Spawner garde les siens (2 ou 3 pendant la vague) et le coffre de boss. La caisse violette (`Chest.MakeEngineerCrate`) est retirée.
  Il y a donc autant de coffres qu'avant de coffres et de caisses ensemble (5 à 9 par vague) ; chacun est un choix entre l'arc et les
  tours.
- Les textes : le tutoriel (« Ouvrir les coffres »), « Pas de plan : ouvre les coffres », le raccourci **V** (un coffre).

**À surveiller aux essais** : le nombre de coffres (5 à 9 par vague : peut-être trop ; les réglages du Crystal Spawner et du Chest
Spawner dans la scène) ; l'équilibre entre les parchemins, les tourelles et les objets pris (les objets sont plus rares qu'avant : une
orbe sur deux au troisième emplacement, et le coffre de boss) ; les plans (un lot par coffre au lieu d'un par caisse).

### 12.39 Trois façons de jouer, simulées ; la puissance des tours baissée (2 novembre au soir)

Le joueur demande ce que donnerait une partie pour trois joueurs : tout pour l'arc, tout pour les tourelles, moitié-moitié, et la
comparaison avec l'ancienne version. Simulation grossière de Claude (400 parties par cas, Normal ; un script hors du dépôt, d'après
`Tools/equilibrage_tours.py`) : le joueur ouvre tous les coffres ; ses dégâts : 14 par seconde, 60 % du temps, +12 % par parchemin
de force, +5 % d'adresse, +20 % par objet ; les tours : la moitié de leur DPS sur un groupe, sans fusions ; le besoin : les PV de
toute la vague sur sa durée ; ni la boutique, ni les boss. Part du besoin couverte (et part des dégâts des tours) :

| Vague | Ancienne version (tout le monde a tout) | Tout l'arc | Toutes les tours | Moitié-moitié |
|---|---|---|---|---|
| 2 | 420 % (68 %) | 161 % | 590 % (87 %) | 286 % (55 %) |
| 5 | 367 % (89 %) | 78 % | 362 % (97 %) | 292 % (87 %) |
| 8 | 191 % (94 %) | 31 % | 140 % (99 %) | 118 % (89 %) |
| 10 | 110 % (97 %) | 14 % | 70 % | 60 % |

L'ancienne version donnait déjà 70 à 95 % des dégâts aux tours (l'impression du joueur était juste) ; dans la nouvelle, le chemin
des tours écrase celui de l'arc : un niveau de tour rapporte bien plus qu'un objet (et chaque coffre propose une tourelle : un joueur
« tout aux tours » a deux fois plus de niveaux qu'avant). Moins de coffres ne rééquilibre pas les deux chemins (tout devient plus
dur). Le modèle de l'arc est le plus incertain (les pièces qui frappent des groupes valent sûrement plus que +20 %).

Le joueur : **la puissance des tours en Normal passe de 0,8 à 0,5**, et **Difficile et Impossible dans la même proportion**
(×0,625) : 1,3 → **0,81**, 1,9 → **1,19** ; le Facile ne change pas (0,5 : il vaut maintenant le Normal). Par `Editor/BalanceBaker`.
**À essayer le 3 novembre** (le joueur) : trois parties jusqu'à la vague 6, tout pour l'arc, tout pour les tours, moitié-moitié ; à
chaque fin de vague, « Défenses : X % des dégâts » et si la vague a tenu.

### 12.40 Le premier essai : « tout aux tours » ; le brainstorming qui suit (2 novembre)

La partie du joueur : **que des tourelles**, aucune amélioration d'arc, l'argent seulement pour des parchemins, avec l'arc puissant.
Ses remarques :

- **Un simulateur de randonnée** : on passe son temps à courir dans la forêt chercher les coffres ; on voit très peu ce que font les
  tours, qui deviennent oubliables, et le tir à l'arc aussi. (Les deux générateurs donnent 5 à 8 coffres par vague, une trentaine
  avant MK : le Crystal Spawner, 3 puis 5 au début de la vague ; le Chest Spawner, 2 ou 3, dont deux pendant la montée, à 45 et 70 %
  du chrono.)
- **L'arc n'est pas faible** : sans aucune pièce (mais avec les parchemins de force, qui valent aussi pour lui), il faisait beaucoup de
  dégâts à chaque vague et a tué MK sans les meilleurs soutiens.
- **Avec tous les plans, un seul trio et un ou deux duos, pas améliorés** : les plans tombent au hasard sur 10 bâtiments, et un duo
  demande deux tours précises au niveau 3.
- **MK** : il doit faire plus de dégâts, avec en échange une régénération des PV du joueur quand il n'est pas touché pendant un moment,
  et des soins qui apparaissent sur la carte.

Ses premières idées : les coffres n'apparaissent que dans une des deux moitiés de la forêt, et aucun pendant la vague ; une partie sans
amélioration d'arc ne devrait pas être possible : le coffre de boss perd peut-être sa dernière orbe (3 plans au hasard).

Les propositions de Claude (à décider ; chacune dit ce qu'elle retire, règle du fil rouge) :

1. **Les coffres** : un seul générateur (les deux font la même chose depuis le coffre unique) ; tous les coffres au gong, dans une
   moitié de la forêt (l'autre à la vague suivante, annoncée), aucun pendant la montée (le combat, où l'on voit ses tours) ; 4 par
   vague au lieu de 5 à 8.
2. **Le chantier de la partie** : au départ, 3 bâtiments tirés au hasard ; les coffres ne proposent qu'eux ; un de plus après chaque
   boss. Les plans se concentrent (les duos et les trios arrivent), on n'apprend que 3 tours au début (la complexité dosée), et chaque
   partie a sa couleur. Plus discret : l'orbe de tourelle propose 2 fois sur 3 un bâtiment déjà posé.
3. **L'arc obligatoire** : le coffre de boss, 2 objets seulement (l'orbe « 3 plans » retirée) ; la troisième orbe du coffre, toujours
   un objet (plus de pile ou face) : un coffre = un parchemin (les deux), une tourelle (les tours), un objet (l'arc).
4. **MK** : la régénération du joueur (déjà dans `Combat/Health` : `m_RegenPerSecond` après `m_RegenDelay`, les mannequins s'en
   servent), partout ; les dégâts de MK ×1,5 à ×2 (8,33 depuis le 15 octobre, 25 avant) ; des soins seulement dans l'arène, au pied
   des piliers à chaque changement de phase, et seulement si la régénération ne suffit pas.

### 12.41 Ce que le joueur retient du premier essai, appliqué (2 novembre au soir)

Le joueur garde les quatre propositions de 12.40 (le chantier dans sa version principale) et ajoute : **un seul plan par lot**
(la barricade et les piques, qui venaient par 2, valaient bien plus que les autres), **un seul plan par niveau de tour** (plus
2 aux niveaux 4 et 5), **2 plans par niveau de duo et 3 de trio** (4 avant), et **les places bloquées** (des endroits de la
rivière, des cailloux, les mannequins et les cibles d'entraînement sur les voies). Compilé (`compile_check` et
`compile_check_editor` sans erreur ni avertissement), **pas encore vu dans Unity ni testé au casque**.

- **Les coffres** (`Building/CrystalSpawner`, `Chests/ChestSpawner`) : un seul générateur. **4 coffres** par vague (réglage
  *Count* ; avant 3, puis 4, 5…), tous 3 s après le gong, **d'un seul côté de la forêt** vu de la tour face aux ennemis (l'axe :
  de la tour vers le milieu des points d'apparition des ennemis), l'autre côté à la vague suivante, le premier au hasard ;
  « 4 coffres sont apparus, côté gauche ! ». Plus rien pendant la montée. Le Crystal Spawner prend aussi les 6 emplacements du
  Chest Spawner (18 en tout, 9 de chaque côté), qui ne garde que le coffre du boss et les touches C et X.
- **Le coffre** (`Chests/ChestReward`, `Chest`) : un parchemin, une tourelle (un plan d'un bâtiment du chantier), **un objet**
  (toujours ; une deuxième tourelle seulement s'il n'y a plus d'objet à donner). Le coffre de boss : **2 objets**, l'orbe des
  3 plans au hasard est retirée (*Tower Chance* et *Boss Plans* n'existent plus). Au moins 5 objets d'arc par partie.
- **Le chantier de la partie** (`Building/BuildManager`, *Starting Structures* 3, *Structures Per Boss* 1) : au lancement, 3
  bâtiments tirés au hasard (la rareté d'abord : 60 % commune, 30 % rare, parmi celles qui restent) ; les coffres ne proposent
  que leurs plans (au hasard, sans rareté). Au premier gong : « Ton chantier : Baliste, Givre et Mortier » ; à la fin de chaque
  vague (pas à la victoire contre MK), un bâtiment de plus : « Nouveau au chantier : Phare ! ». Le raccourci **B** met aussi
  toutes les tours au chantier. *À surveiller* : un chantier de départ sans tour qui fait des dégâts (barricade, bulle, filet)
  est possible ; à revoir si l'essai le montre. **Changé le 3 novembre** (12.53) : les 3 tours d'un trio, plus la Barricade.
- **Les plans** (`StructureDefinition.LotSize` et `PlansFor`) : un plan par lot et par niveau de tour ; les duos et les trios
  par `Editor/FirstTestBaker` (le réglage *Plans* de leurs niveaux 2 et 3 : 4 → 2 et 4 → 3). Une tour du niveau 1 au niveau 5 :
  5 plans (7 avant) ; un duo : 6, 8, 10 plans (6, 10, 14) ; un trio : 9, 12, 15 (9, 13, 17), ou 11 et 13 en montant d'abord la
  tour à son niveau maximal (un trio naît un niveau plus haut quand sa tour est au niveau 5). Le tableau d'équilibrage est relancé
  avec ces coûts.
- **La grille** (`Building/BuildGrid`) : les **cailloux du kit** (`rock_small…`, `stone_small…` : au ×3 du kit, beaucoup étaient
  plus gros que les petits cailloux déjà ignorés) et les **mannequins et cibles d'entraînement** (qui comptaient comme des
  ennemis, donc comme des obstacles, au lancement de la grille) ne bloquent plus : un bâtiment posé dessus les fait disparaître,
  et ils reviennent s'il est déplacé. Les grands rochers (`stone_tall…`) bloquent toujours. **Le gué** : le lit de la rivière à
  moins de 3 m du chemin des ennemis, à son niveau et sur leur NavMesh, compte comme la voie (barricade, piques). **La raison
  affichée** : l'étiquette de l'hologramme dit ce qui bloque (« un arbre ou un rocher », « le sol n'est pas plat », « trop près
  de la tour », « les ennemis arrivent là », « la place d'un coffre », « ça déborde sur la voie », « trop loin d'une voie ») :
  si une place gêne encore, le joueur peut dire laquelle.
- **La régénération** (`Player/PlayerHealth`, *Regen Delay* 5 s, *Regen Percent Per Second* 4) : après 5 s sans être touché,
  les PV remontent de 4 % des PV max par seconde, partout. **MK** (`Data/Enemies/Boss_Dragon`, par `Editor/FirstTestBaker`) :
  dégâts de base 8,33 → 16,67. Les soins dans l'arène (au pied des piliers, à chaque phase) ne sont pas faits : seulement si la
  régénération ne suffit pas.
- Le tutoriel (« Ouvrir les coffres ») dit le nouveau coffre et le chantier.

### 12.42 Le deuxième essai : les places refusées, la partie de sa sœur (2 novembre)

**Les places où l'on devrait pouvoir construire** (le joueur, avec cinq captures) : (1) des endroits au bord des voies, sous « un
caillou qui est en l'air pour le décor » ; (2) les cases au bord de la voie du milieu, mi-voie mi-clairière, à compter comme la
voie ; (3) la voie de droite qui s'élargit, des piques posables au milieu, alors qu'elle devrait rester droite ; (4) une grande
zone à gauche, près de la montée du milieu ; (5) la même, symétrique, à droite (« trop loin d'une voie »).

Unity ouvert, Claude ne voit pas la carte : il en a tiré un plan vu de dessus du fichier de la scène (la position de chaque objet,
calculée de parent en parent ; scripts hors du dépôt). Ce qu'il a trouvé et fait (`Building/BuildGrid`) :
1. **L'habillage des falaises** (`Map > Dressing > Cliffs`, 726 pièces, et une rangée de 9 `cliff_top_rock` au sol le long de la
   voie de gauche, de z = 48 à 75) : des faces et des rebords de 80 cm d'épaisseur, posés devant les blocs du terrain ; plusieurs,
   à 3 m et plus au bord des voies, débordent au-dessus de leur dernière rangée de cases. La grille prenait leur dessus pour le sol :
   la case n'était « pas plate ». Désormais, une pièce `cliff_…` qui n'est ni un bloc (`cliff_block…`) ni un escalier
   (`cliff_steps…`) n'est ni sol ni obstacle : la grille prend le terrain dessous. Un bâtiment posé au pied d'une falaise peut la
   mordre de quelques dizaines de centimètres.
2. **Les demi-cases** : la voie du milieu suit la diagonale, ses cases de bord sont coupées en deux (un triangle de terre, un
   d'herbe). Une case qui touche la terre de la voie, près du chemin, à son niveau et sur le NavMesh, est maintenant sur la voie
   (avant : « ça déborde sur la voie »). Les voies droites ont leurs bords sur les lignes de la grille : rien ne change pour elles.
3. **La voie de droite au gué** : la rivière y tourne juste après le passage (`ground_pathBend` en (19,5 ; 4,5) et (22,5 ; 4,5)), et
   le chemin des ennemis n'est pas au milieu de la voie. La règle du premier essai (le lit de la rivière à moins de 3 m du chemin compte
   comme la voie) y ajoutait une ou deux rangées. Retirée : seuls les passages (`…_lane`, de la terre) sont la voie ; le lit à côté
   se construit comme le bord de la voie.
4. et 5. **Les clairières** au pied de la voie du milieu (à gauche, celle des mannequins ; à droite, celle de la rivière et de la
   cible mobile) font une dizaine de mètres de profondeur ; la bande du bord de la voie n'en faisait que 4,5. Elle passe à **12 m**
   (comptés depuis les cases de la voie, à peu près au même niveau). Les terrasses : **en hauteur de 1,5 à 7,5 m** au-dessus de la
   voie (2 m et sans limite avant : entre 1,5 et 2 m, rien n'allait ; les montagnes ne comptent plus), jusqu'à **25 m** du chemin
   des ennemis (15 avant) : presque toute la jungle.
- **En test** (éditeur et build de développement), l'étiquette de l'hologramme ajoute le nom de ce qui bloque et celui de son
  parent : « Pas ici : le sol n'est pas plat (cliff_stone · Cliffs) ». Si une place gêne encore, le joueur peut dire l'objet.

**La partie de sa sœur** (Normal) : en ouvrant tous les coffres, pas un duo (pas assez de plans) ; les vagues et les boss trop
faciles, avec un arc « pas si incroyable » ; MK bien réglé ; des vagues trop longues, maintenant que tout est d'un seul côté.
Appliqué :
- **2 plans par coffre** (`BuildManager`, *Plans Per Chest* 2, multiplié par le lot du bâtiment, 1 partout) : l'orbe de tourelle
  donne 2 plans de son bâtiment (1 avant).
- **La courbe des PV relevée** (`Editor/SecondTestBaker`, *Wave Settings*, *Health Per Wave* ; toutes les difficultés sauf le
  Facile) : ×0,8 / 1,03 / 1,42 / 1,62 / 1,8 → **×0,85 / 1,29 / 1,99 / 2,51 / 3,06** aux vagues 1 à 5 (+6 %, +25 %, +40 %, +55 %,
  +70 %), boss des vagues compris. En Normal, les PV d'un ennemi font ×0,85 / 1,48 / 2,63 / 3,82 / 5,35 : à peu près la courbe
  d'avant le 1er novembre (×0,85 / 1,31 / 2,34 / 3,71 / 5,72). **MK ne change pas** : la vague du boss final ne suit pas cette
  courbe, et la croissance par vague (×1,15) reste.
- **Les tours ×1,2** (*Structure Power*) : Normal 0,5 → **0,6**, Difficile 0,81 → **0,97**, Impossible 1,19 → **1,43** ; le Facile
  garde 0,5.
- **Des vagues plus courtes** (*Wave Settings*) : un chrono de **90 s** à la vague 1 (120), **+12 s** par vague (15), l'accalmie
  **35 %** du chrono (40 %). En Normal : vague 1, accalmie 48 → **32 s** et montée 36 → **29 s** ; vague 5, accalmie 72 → **48 s**
  et montée 54 → **45 s**. Les ennemis de la montée sont aussi nombreux, donc plus serrés.
- Le tutoriel (`Resources/TutorialContent`, par le même baker) : la fenêtre « Ouvrir les caisses de l'ingénieur », restée d'avant
  le coffre unique, devient « Ouvrir les coffres » (deux plans d'un bâtiment du chantier).
- Compilé (`compile_check` et `compile_check_editor` sans erreur ni avertissement), **pas encore vu dans Unity ni testé au
  casque**. *À surveiller* : avec deux fois plus de plans, les tours vont monter bien plus vite ; la courbe relevée doit suivre.
  Tout se règle dans *Wave Settings* (*Health Per Wave*, la durée) et dans chaque difficulté (*Structure Power*).

**Le même jour, le joueur revient** (une capture) : la voie de droite s'élargit toujours par endroits ; et les demi-cases de la
voie du milieu devaient être de la **clairière**, pas la voie (Claude avait compris l'inverse). Fait (`Building/BuildGrid`) :
- **La voie, seulement la terre des voies** : celle des tuiles rangées sous `Map > Lanes` et des passages de la rivière
  (`…_lane`). La terre d'autres modèles (les blocs rocheux et les marches du kit ont un matériau « dirt ») ne compte plus
  (`Surface.Earth`) ; la règle d'avant le 28 octobre (un sol inconnu à moins de 3 m du chemin, *Lane Half Width*) est retirée :
  les deux ponts de la carte passent au-dessus de la voie du milieu, en hauteur. Si les tuiles des voies quittent le groupe
  `Lanes`, plus rien ne se pose sur la voie (la console le dit).
- **Les demi-cases** : une case de la voie dont un coin (à 10 % du bord) est sur autre chose que sa terre, à sa hauteur, est au
  bord de la voie : on y pose les tours, pas les pièges. Elles débordent d'une demi-case au plus sur la terre.
- **Pas trouvé** : Claude ne voit, dans le fichier de la scène, aucune terre de voie hors de la bande droite de la voie de
  droite, sauf la place devant la tour (la tuile de (−3 ; 3) à (9 ; 9), où les trois voies se rejoignent ; la terre y est
  vraiment). Si l'élargissement reste ailleurs, l'image de la grille le montrera.
- **L'image de la grille** (éditeur seulement) : à chaque lancement, `Logs/BuildGrid.png` (hors de Git) montre la grille vue de
  dessus, une couleur par sorte de case et par raison, le chemin des ennemis en blanc. Claude la lit lui-même : plus besoin de
  décrire les places.
- **Le soir, l'image de la grille lue** (le joueur : tout va, sauf « les hauteurs, c'est un peu le bordel ») : sur les terrasses,
  une grande zone « trop loin d'une voie » près de l'antre (les terrasses à 6 m : le chemin des ennemis du milieu part de
  l'apparition du milieu, à mi-voie, et l'antre est à plus de 25 m de tout chemin), des pierres dressées (`stone_tall…`) prises
  pour du sol « pas plat », et les arbres. Fait : **plus de limite de distance** (*Roadside Width* et *Height Reach* retirés) :
  tout sol plat se construit, au niveau des voies (au bord) ou sur une terrasse de 1,5 à 7,5 m plus haute (en hauteur) ; au-delà,
  « trop haut » (les montagnes). Les pierres (`stone…`, sauf les petites qui disparaissent sous un bâtiment) sont des obstacles,
  comme les rochers. Les arbres restent des obstacles (leur feuillage couvre plusieurs cases).
  La nouvelle image le confirme : les terrasses à 3 et 6 m se construisent presque partout. Le haut de la voie du milieu, entre
  l'apparition du milieu et l'antre (de la terre de voie où aucun ennemi ne passe), reste non constructible : choix du joueur.

### 12.43 Le centre d'entraînement sur la voie du milieu, les torches au bord des voies (2 novembre au soir)

Le joueur : les torches et les mannequins sont mal placés. **Les torches** doivent être collées au bord des voies ; **les
cibles** pour viser sont retirées (moins utiles que les mannequins) ; **les mannequins** vont en plein milieu de la voie du
milieu : un près de la tour qui fait des droite-gauche, un groupe de 3 un peu plus loin, un dernier qui fait des droite-gauche
plus loin encore ; et sur leur visage, son dessin fait sur Paint (« en gardant les bonnes couleurs »). Fait par
`Editor/TrainingLaneBaker` (la scène, une fois, tant que le groupe « Training » a ses cibles ; puis Ctrl+S) :
- **Les cibles** (16, 24 et 33 m, la cible mobile) sont supprimées de la scène ; le script et le prefab restent.
- **Les mannequins**, tournés vers la tour, sur l'axe de la voie du milieu (de la tour vers l'apparition du milieu) : un à
  **14 m** qui va et vient en travers de la voie (±2,8 m, 1,2 m/s), un **groupe de 3** serré en triangle à **24 m**, un à
  **42 m**, juste après le gué, qui va et vient aussi (1,5 m/s). Le doublon du « Mannequin groupe 1 » et l'ancien mannequin
  seul sont retirés. Le va-et-vient : un `PingPongMover` sur le mannequin, que `TrainingDummy` reprend (sa place glisse, il revient
  toujours après une poussée, il s'arrête à terre et pendant les vagues ; sa vitesse compte pour l'auto-visée). La grille de
  construction prend tout son trajet pour le cacher sous un bâtiment.
- `Editor/RoguelikeTuningBaker` n'ajoute plus de mannequins : il ne comptait pas celui qui va et vient et en avait posé un de
  trop (le doublon).
- **Les torches sur pied des voies** (les torches murales sont déjà sur les parois) : posées à 3,3 m du chemin des ennemis, qui
  n'est pas au milieu de la voie, certaines étaient dans la voie (voie de gauche, voie de droite, voie du milieu, la place devant
  la tour), une autre à 2 m de son bord. Chacune va au bord de la terre de la voie le plus proche, en travers de la voie, 40 cm
  dehors, tournée vers la voie ; 30 cm dedans, contre le bord, si dehors n'est pas un sol plat au niveau de la voie (l'eau, une
  paroi). Celles de la forêt et des terrasses ne bougent pas.
- **Le visage** : `Art/Training/Visage_Mannequin.png` (le dessin du joueur recadré : traits noirs, fond transparent, 256 px),
  sur un masque courbe qui épouse l'avant de la tête (`Visage_Mannequin_Masque.asset`, enfant `Visage` de `Head` dans le prefab
  `Training Dummy`), matériau URP Unlit découpé (`Visage_Mannequin.mat`) : les traits restent noirs à toute lumière, la toile
  de jute de la tête garde sa couleur.
- Compilé (`compile_check` et `compile_check_editor` sans erreur ni avertissement), **pas encore vu dans Unity ni testé au
  casque**.

### 12.44 Les ultimes : les règles décidées, les idées à trier (2 novembre au soir)

Le joueur : pas de règle nouvelle (le cœur du boss proposé par Claude est refusé : une ressource de plus) ; l'ultime doit être
intuitive, **pas à chaque partie**, et **méritée** : on ne doit pas pouvoir l'avoir facilement avant la vague 5, ni pendant.
**Décidé** :
- **L'ultime est le niveau 4 d'un trio au niveau 3** : on pose des plans dessus, comme pour un niveau ordinaire (des plans de
  l'une de ses trois tours), et elle se transforme **entre les vagues**, comme les fusions. Pas de cadenas de vague.
- **Elle coûte 10 plans de plus** (3 pour un niveau de trio) : environ 25 plans dans la même lignée. Estimation de Claude : un
  joueur qui prend la tourelle à chaque coffre a au plus 32 plans avant la vague 5, répartis sur 3 à 6 bâtiments du chantier ;
  l'ultime arrive au mieux à la pause avant MK, sinon en mode infini, ou jamais (à vérifier en jouant).
- **Une seule par partie.**
- **Une par famille de trios, 3 en tout** : la forge et la poudre (Enclume, Grande bombarde) ; la fête foraine (Montagnes
  russes, Gonfleur, Colisée) ; la glace et le temps (Bowling, Beffroi).
- Proposé et gardé : elle grandit (environ deux fois un trio), a une action avec la flèche du joueur (comme les trios), suit le
  joueur dans l'arène de MK (comme les balistes et les tours de givre) ; les boss ne sont jamais tués d'un coup.

**Les idées de Claude**, par famille (le joueur en garde une par famille) :
- **Forge et poudre** : le **Colosse de forge** (un géant de fer agenouillé, une fournaise dans la poitrine ; son marteau tombe
  où tombe ta flèche ; la coulée de fonte sur toute la voie ; il plaque MK au sol) ; la **Bombarde de siège** (un canon de 15 m
  dont l'obus traverse la carte ; ta flèche marque la cible ; le tir de barrage ; un boulet enchaîné immobilise MK) ;
  l'**Enclume céleste** (une forge volante tient une enclume grande comme une maison au bout de ses chaînes ; ta flèche coupe
  la chaîne ; le ciel de fer ; l'enclume cloue MK au sol) ; le **Volcan éveillé** (le volcan retiré le 1er novembre, en géant :
  ta flèche dans le cratère le fait entrer en éruption ; toute la voie en coulée de lave ; des bombes de lave sur MK).
- **Fête foraine** : le **Grand Chapiteau** (le chapiteau avale l'ennemi visé par ta flèche et le recrache par son canon humain ;
  la grande parade, toute une voie figée à danser ; des ballons explosifs sur MK) ; la **Grande Roue** (ses nacelles ramassent
  les ennemis et les lâchent d'en haut ; ta flèche décroche une nacelle pleine sur un groupe ; la roue folle qui roule sur la
  voie ; un crochet attrape l'aile de MK) ; le **Grand Manège** (des chevaux de bois géants ; les ennemis tournent et sont
  éjectés ; ta flèche l'emballe ; la charge, tous les chevaux descendent la voie au galop ; ils chargent MK).
- **Glace et temps** : la **Grande Horloge** (de glace ; à chaque heure, les ennemis du cadran sont rembobinés et givrés ; ta
  flèche arrête le temps sur une zone, tes flèches attendent en l'air ; Minuit éternel, toute la carte gelée 6 s ; MK figé en
  vol) ; le **Sablier géant** (son ombre ralentit le temps ; ta flèche fait couler le sable sur un groupe, figé dedans ; le
  retournement, toute une voie renvoyée à son départ ; MK ralenti) ; le **Géant des glaces** (un yéti de 10 m ; son souffle
  givre la voie ; ta flèche lui fait lancer une boule de neige géante ; l'ère glaciaire, un glacier avance sur la voie et
  enferme tous les ennemis, ta flèche les brise d'un coup ; il gèle les ailes de MK, qui tombe).

**Le choix du joueur (2 novembre au soir)** : les trois conseillées par Claude, le **Colosse de forge** (forge et poudre), le
**Grand Manège** (fête foraine) et la **Grande Horloge** (glace et temps). Le joueur : la Grande Horloge reprend l'arrêt du
temps du Beffroi au niveau 3 (Minuit, qu'il a réglé sur toute la carte : 9 s toutes les 20 s) ; **Minuit passe donc à
l'ultime**, et le niveau 3 du Beffroi reçoit autre chose. Le code de Minuit (la bulle, le noir et blanc, les flèches arrêtées,
« Za Warudo ! Temps, arrête-toi ! » et « Le temps reprend son cours ») est gardé pour la Grande Horloge. Rien n'est codé.

**Le nouveau niveau 3 du Beffroi, les propositions de Claude** (aucune règle nouvelle : chacune pousse le rembobinage des niveaux
1 et 2 ; à choisir par le joueur) :
- **Le Phare du passé** (conseillé) : toutes les 20 s, avec cinq ennemis à portée, la lanterne du sommet (celle du niveau 2)
  s'allume et un grand faisceau sépia fait un tour complet autour du beffroi en 6 s, sur toute la carte, comme un phare ; chaque
  ennemi au sol qu'il balaie est rembobiné (8 s de son chemin) puis figé 3 s. Le phare du trio (Boussole folle + Phare) en
  démesure ; rien ne ressemble à Minuit (ni bulle, ni noir et blanc, ni flèches arrêtées) ; le joueur voit venir le faisceau.
- **Le Grand Rembobinage** : douze coups en 6 s ; à chaque coup, tous les ennemis au sol de la carte reculent d'une demi-seconde
  sur leur chemin, dans un monde sépia (un vieux film qu'on rembobine), puis figés 3 s. Plus simple à coder (la bulle et les
  douze coups de Minuit), mais il ressemble trop à Minuit.
- **Le Pendule** : un balancier doré de 15 m descend sous le beffroi et balaie la voie d'un bord à l'autre pendant 10 s, au
  rythme du tic-tac ; chaque ennemi touché est rembobiné. Très visible, mais seulement sur sa voie, et un balayage proche de
  l'Avalanche et du Boulet fou ; une pièce de modèle en plus.
- La progression : rembobiner autour de lui (1), figés à l'arrivée (2), toute la carte rembobinée (3), puis le temps arrêté (la
  Grande Horloge, l'ultime). Les boss et les volants ne sont toujours pas rembobinés.

### 12.45 Le Beffroi au niveau 3 : le Phare du passé (2 novembre au soir)

Le joueur choisit le **Phare du passé** parmi les trois propositions de 12.44. Codé (`Building/TrioBeffroi.cs`), compilé
(`compile_check` et `compile_check_editor` sans erreur ni avertissement) ; **pas encore vu dans Unity ni testé au casque**
(tests : `Checklist_Demo.md`, « Le Beffroi, son nouveau niveau 3 »).
- **Le Phare du passé** : toutes les 20 s, avec au moins cinq ennemis à 15 m de la cloche, « Le Phare du passé ! », la cloche
  sonne et un faisceau sépia part de la lanterne du sommet (celle du niveau 2) : une part de lumière de 6° qui descend jusqu'au
  sol et va à 110 m (toute la carte) ; il fait un tour complet en 6 s, en partant vers l'ennemi le plus proche. Chaque ennemi au
  sol qu'il balaie est rembobiné comme par la cloche (8 s de son chemin, à reculons en 1,2 s), puis figé 3 s (le niveau 2) ; une
  fois par tour. Les aiguilles tournent à l'envers à toute vitesse pendant le tour. Les boss et les volants ne sont pas
  rembobinés. La cloche continue de sonner à côté (toutes les 4 s au niveau 3).
- Pour rembobiner toute la carte, le beffroi note au niveau 3 le chemin de tous les ennemis au sol à 110 m (une position tous les
  dixièmes de seconde, comme avant à 23 m). Les ennemis rembobinés par le faisceau ont une traînée sépia plus clairsemée que ceux
  de la cloche (une gerbe toutes les 0,3 s au lieu de 0,08 s) et le bruit de cassette joue au plus toutes les 1,5 s : toute la
  carte recule en même temps (le Quest).
- Les réglages sont en tête du script (partie « Le Phare du passé ») : la lanterne, l'intervalle, la portée, la durée du tour, la
  largeur, le matériau. `Editor/BeffroiPhareBaker` (chaque étape une fois, avec sa garde) : le matériau
  `Art/TowerDefense/Faisceau_du_passe.mat` (la lumière transparente du Rayon du jugement, en sépia) ; le prefab (ce matériau et la
  lanterne) ; le texte du niveau 3 de la fiche, s'il est encore celui de Minuit (« Le Phare du passé : la lanterne s'allume et son
  faisceau fait le tour de toute la carte ; chaque ennemi qu'il balaie rembobine son chemin, puis reste figé. »).
- **Minuit** reste dans le code du Beffroi, en sommeil : il ne joue qu'au niveau 4, celui de l'ultime, que rien n'atteint tant que
  la Grande Horloge n'est pas codée ; ses réglages du prefab (ceux du joueur : toutes les 20 s, toute la carte, 9 s) sont gardés.
  Le modèle du niveau 3 ne change pas (la lune, l'étoile, les douze heures).
- Le tableau d'équilibrage (`Tools/equilibrage_tours.py`) compte le Phare du passé au niveau 3 à la place de Minuit (à relancer
  après Unity) ; `Tools/allure_tours/trios.py` a le nouveau texte.

### 12.46 Les ultimes dans le code : le système et le Colosse de forge (2 novembre au soir)

Le joueur : « partons sur les 3 ultimes, fais le premier ». Codé, compilé (`compile_check` et `compile_check_editor` sans erreur
ni avertissement), le modèle dessiné hors de Unity sans faces superposées (`Tools/allure_tours/apercu.py`, « Duo:Colosse de
forge ») ; **pas encore vu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`, « Le Colosse de forge »). Aucun asset
téléchargé : le géant est fait de formes simples, comme les trios ; les sons sont ceux du projet (à remplacer si le joueur le veut,
voir plus bas). Textes et chiffres : des propositions, à revoir aux essais.

**Le système** (la règle de 12.44, sans rien de plus) :
- Une ultime est une tour (`Building/TowerUltimate.cs`, hérite de `TowerTrio`) : elle garde les trois tours de son trio, et le trio
  dont elle est née (`FromTrio`). Sa définition (`Data/Duos/Ultime_Colosse`) n'a **qu'un niveau**, dont les plans (10) sont ceux de la
  transformation ; elle se pose partout (*Anywhere*) sur 4 × 4 cases (celles de l'Enclume et de la Grande bombarde) : elle reste à la
  place de son trio, sur la voie ou au bord. Le catalogue `Resources/TowerDuos` a une liste à part, **Ultimes** (son nom, sa
  définition, les trios de sa famille).
- **La transformer** : un plan de l'une des trois tours du trio, sorti de la montre et posé sur le trio au niveau 3 (l'hologramme
  dit « Ultime : Colosse de forge (10 plans) »), ou la carte « Ultime » du panneau du trio. Entre les vagues seulement (« L'ultime se
  bâtit entre les vagues »), **une seule par partie** (« Une seule ultime par partie »), 10 plans de ses trois tours
  (`BuildManager.UltimateOf`, `BuildUltimate`). Le trio disparaît, l'ultime apparaît en plan bleu à sa place, et le joueur la bâtit
  avec ses marteaux (3 coups), comme un niveau ordinaire ; finie : « Ultime : Colosse de forge ! » et ce qu'elle fait.
- La vitrine **F4** la montre après les trios (une seule, sur la voie du centre ; sinon dans la clairière de droite).
- **L'arène de MK** (`FinalBoss/ArenaTurrets`) : l'ultime y descend la première, avant les balistes et les tours de givre, réduite
  aux deux tiers (0,65 : la voûte est à 22,5 m près des parois, le plateau à 5 m), et retrouve sa taille et sa place à la fin.
- Pour MK, un état nouveau : **plaqué au sol** (`FinalBoss/DragonBoss.Pin.cs`, `DragonBoss.Pin(durée)`) : en vol, il tombe sous lui
  (hors des piliers), s'étend à terre (l'animation « Sleep »), puis se relève et reprend le combat (en vol s'il est dans sa phase en
  vol). Pas pendant l'intro, un rugissement (il change de phase) ni s'il l'est déjà.

**Le Colosse de forge** (`Building/UltimeColosse.cs`, l'Enclume ou la Grande bombarde au niveau 4) : un géant de fer de 18 m, à genoux,
les genoux écartés (sur la voie, les ennemis passent entre eux, sous son bassin) ; une fournaise dorée dans la poitrine, deux
cheminées qui fument dans le dos, des yeux de braise, une crête dorée, des épaulières de bronze ; son marteau à la main droite, la
tête posée au sol devant son genou ; une grande enclume cerclée d'or à côté de lui. Le buste se tourne vers sa cible (120° au plus
de chaque côté). Modèle : `Tools/allure_tours/ultimes.py` (partie « ultimes » du fichier d'allure, faite par le baker). Les
réglages sont en tête du script :
- **Son marteau tombe où tombe ta flèche** : une flèche tirée qui touche à 30 m de lui (le sol, un ennemi, un volant), et il lève
  son marteau par-dessus sa tête et le lance là ; le marteau tourne en l'air, frappe, reste planté un instant, puis revient à sa
  main. L'impact : 140 de dégâts au centre, 70 au bord, dans 5 m ; tout ce qui survit vole en l'air (« Au vol ! ») ; le sol rougit
  sous la tête du marteau (une flaque de fonte en feu, 3 s). Un ennemi touché par la flèche est suivi ; un volant, frappé en l'air.
  Si le marteau est en vol, ta flèche reste sa cible 3 s : il y va dès qu'il l'a rattrapé (un coup toutes les 2,5 s environ).
- **Sans flèche**, il le lance de lui-même au plus toutes les 5 s, sur l'ennemi au sol qui a le plus de voisins à sa portée.
- **Un boss** n'est jamais projeté ni tué d'un coup : 1,5 % de ses PV max par coup de marteau.
- **La coulée de fonte** : toutes les 25 s, avec au moins six ennemis au sol sur la voie la plus proche, entre lui et la forêt
  (« La coulée de fonte ! ») : la fonte coule de sa poitrine jusqu'au sol (1,2 s, un jet lumineux), puis descend la voie vers la
  forêt sur 70 m (14 m/s, 7 m de large) : 80 de dégâts et une brûlure (40 sur 3 s) à ceux qu'elle touche, repoussés vers la forêt
  en l'air (pas les boss) ; elle laisse la fonte au sol 4 s (des flaques sans flammes : il y en a une vingtaine, le Quest). Une
  lueur orange suit le front.
- **Dans l'arène de MK** : ta flèche dans MK, et le marteau le **plaque au sol** 3 s (« Plaqué au sol ! »), au plus toutes les
  12 s ; il y perd 3 % de ses PV max. Rien d'autre là-bas (pas de coulée ni de coup de lui-même). À l'arrivée : « Le Colosse de
  forge est là ! Touche MK : son marteau le plaque au sol ».
- Les cadences suivent les parchemins d'adresse et le phare (*Overcharge Rate*) ; les dégâts, la force des bâtiments (*Strength*).

**Les sons** : le lancer (le souffle rapide), l'impact (`impactPlate_heavy`, grave), le marteau rattrapé (`impactMetal_heavy`),
ceux du projet. Pour la fonte, le joueur a choisi le même soir quatre sons CC0 de freesound.org, qu'il télécharge lui-même (il faut
un compte) : la fournaise de fonderie d'iankath (173991), la boucle de lave de Fission9 (474850), le grésillement de magnuswaker
(540080), le souffle de feu de hnhnh (244926) ; les originaux sont dans `Audio/` (20 Mo). Découpés par Claude en trois fichiers
courts (mono, 16 bits, 48 kHz, 1,1 Mo en tout ; détail dans `Credits_Licences.md`) : `Audio/Colosse_Fournaise` (3,8 s : le souffle
sur 3,5 s du grondement de la fournaise, au début de la coulée), `Audio/Colosse_Gresillement` (1,3 s : sous la tête du marteau, et
là où la fonte touche le sol) et `Audio/Colosse_Coulee` (6 s bouclées par un fondu enchaîné : elle suit le jet puis le front de la
coulée, monte vite et s'éteint doucement avec elle) ; réglages *Pour Clip*, *Sizzle Clip*, *Flow Clip*. Unity a fait le prefab
pendant que le joueur téléchargeait : ces trois réglages y sont vides. `Editor/ColosseSonsBaker` les pose, une fois, s'ils sont
vides (ou encore `fire_loop`, le son provisoire) et que les fichiers existent : à l'ouverture de Unity, et quand l'un des sons
est importé. Écoutés seulement en forme d'onde (pas de claquement au raccord de la boucle) : **à écouter en jeu**.

**À surveiller aux tests** : la place sur la voie (4 × 4 cases, le géant en déborde : ses genoux à 3 m de part et d'autre, l'enclume
à 5,6 m) ; le marteau qui revient bien à la main (le poing bouge pendant le retour) ; la pose sur un trio (l'hologramme, le panneau)
et le refus pendant une vague ; le corps de MK quand il est plaqué (au sol, hors des piliers ; l'animation « Sleep ») ; la vue
depuis la tour (18 m de haut, le marteau levé à 24 m) ; la coulée sur les voies qui tournent ; le Quest (la lueur, les gerbes).

**Pas encore fait** : l'ultime dans le tableau d'équilibrage (`Tools/equilibrage_tours.py`) ; une flèche dorée « ULTIME » au-dessus
d'un trio prêt à se transformer, comme les flèches « DUO » et « TRIO » (proposé, à décider par le joueur) ; le Grand Manège et la
Grande Horloge.

### 12.47 Le Colosse refait : « on doit ressentir sa puissance » (2 novembre au soir)

L'essai du joueur (F4) : « son attaque est loin d'être impressionnante, de même pour sa coulée de lave qui manque de peps ; de
manière générale il faut que le total du modèle soit plus fini : plus texturé, plus lumineux, avec plus de particules, et une
animation de spawn, le ciel qui change, à chaque coup de marteau un éclair qui tombe ; on doit ressentir sa puissance ». Aucune
règle ne change (le marteau où tombe ta flèche, la coulée, MK plaqué) : tout est dans l'allure, le son et la force des coups.
Compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement), le modèle dessiné hors de Unity sans faces superposées ;
**pas encore revu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`, « Le Colosse de forge, refait »).

**L'éveil** (son apparition ; bâti au marteau, ou posé par F4) : « Le Colosse de forge s'éveille ! » ; le sol se fend de fonte en
étoile, une mare de fonte en feu, et il **sort de terre** (14 m, en 2,4 s) dans la roche qui vole, la poussière et les
éclaboussures de fonte, le sol qui tremble sous les mains (les manettes vibrent) ; un éclair au loin. Debout, sa fonte s'allume
(veines, yeux, fournaise), la fournaise crache le feu et rugit, ses braises et sa fumée partent. Il lève son marteau, **la foudre le
frappe**, et il l'**abat** devant lui : « LE COLOSSE DE FORGE ! », l'onde de choc renverse tous les ennemis à 22 m (100 de dégâts),
le sol se fend, la roche vole. Il se bat 1,5 s plus tard.

**Le ciel de forge** (`World/SkySettings.Forge`, posé par-dessus celui de la difficulté : `SkyController.SetOverlay`, nouveau) :
un orage de braise, noir et rouge, le soleil comme une fournaise, des éclairs au loin, les nuages et la lumière de la carte avec.
Plein pendant l'éveil, fort pendant la coulée (85 %), léger tant que le Colosse est là (35 %) ; rien dans l'arène de MK. Le ciel
sait aussi lancer un éclair à la demande (`SkyController.Strike`) : toute la carte s'éclaire.

**Le coup de marteau** : en haut du geste, **un éclair tombe sur le marteau levé** (le ciel s'éclaire, le tonnerre) ; le marteau
chargé vole avec une traînée de feu et crépite d'éclairs ; à l'impact, **un second éclair tombe avec lui**. Puis : une onde de choc
de lumière qui s'élargit au ras du sol (13 m), la poussière en anneau, la roche qui vole (des éclats, dont des éclats de fonte), le
feu, des fissures de fonte qui partent de l'impact et refroidissent, le sol en feu sous la tête, un éclat de lumière, les manettes
qui vibrent ; le grand impact, le coup, le tonnerre, le grésillement. Plus fort : 160 de dégâts dans 6 m, les survivants projetés
plus haut. Un peu plus lent, pour que chaque coup soit un événement : lever 0,7 s, de lui-même toutes les 6 s.

**La coulée de fonte** : la fournaise explose en flammes ; un **torrent** de fonte animée (1,9 m d'épaisseur) coule de sa poitrine
jusqu'au sol, qui éclabousse, fume et flambe ; puis une **rivière de fonte** (une bande de 7 m qui suit la voie et le sol, la fonte
animée qui coule vers la forêt, plus froide sur les bords) s'allonge derrière sa **vague** (un dos de fonte qui roule en tête, des
flammes, des éclaboussures, la lumière qui la suit). La vague : 90 de dégâts, une brûlure, « Fondu ! », projetés vers la forêt ; la
rivière encore chaude brûle ceux qui y marchent (30 par seconde) ; au bout, elle refroidit (la croûte noire gagne, 6 s), s'enfonce et
disparaît. Des braises montent de la rivière. La coulée est un peu plus lente (12 m/s) pour qu'on la voie descendre.

**Le modèle fini** (une nouvelle passe, « Finition du 2 novembre », qui refait le modèle ; `Tools/allure_tours/ultimes.py`) :
- **la fonte** : un shader nouveau, `Shaders/MoltenIron.shader` (« Archery/MoltenIron », la lave de l'arène sans ses réglages
  d'arène, qui refroidit morceau par morceau), et son matériau `Art/TowerDefense/Fonte_en_fusion.mat` : la fournaise, les yeux, des
  veines sur la poitrine, le ventre, les bras, les cuisses et les tibias, des fentes dans les genouillères, la bouche des cheminées,
  un évent dans le dos, une croix sur chaque face du marteau et une veine autour de sa tête, et les fissures d'un socle de basalte
  sous lui. Leur éclat palpite, s'emballe à la foudre et pendant la coulée ;
- **le fer martelé** : `Tools/texture_fer_martele.py` dessine `Art/TowerDefense/Colosse_Fer.png` (la couleur : creux, grain, suie,
  rouille, rayures) et `Colosse_Fer_Relief.png` (le relief) ; `Editor/ColosseFinitionBaker` les pose sur les matériaux du Colosse
  (`Palettes/Ultime_Colosse_*`), une fois : le fer métallique et un peu poli, le bronze et le cuivre brillants, l'or très poli qui
  luit un peu ;
- **des détails** : des cornes et des pointes d'épaulières dorées, des tuyaux de cuivre dans le dos, les plaques de tibia et leurs
  rivets, des rivets sur la ceinture, le socle de basalte fendu ;
- **des particules qui ne s'arrêtent pas** : les braises de la fournaise, la fumée et les étincelles des cheminées, les gouttes de
  fonte du marteau ; **une lumière** dans la fournaise, qui éclaire autour de lui.

**Les sons** : le tonnerre et le grand impact, choisis par le joueur le même soir (freesound, CC0, téléchargés par lui dans
`Audio/`) : *thunder crack.wav* de cognito perceptu (347853 ; découpé par Claude en `Audio/Colosse_Tonnerre.wav` : 4,5 s depuis le
claquement, en mono, 48 kHz) et *Cinematic Trailer Hit Explosion CC0.mp3* de Wakerone (513110, tel quel). Le fichier d'allure les
nomme ; `Editor/ColosseSonsBaker` les pose aussi s'ils arrivent après le prefab (à la place de l'explosion provisoire).

**À surveiller aux tests** : les éclairs fréquents (un toutes les 3 s environ, guidé par tes flèches : *Strike Flash* les rend plus
doux dans le ciel) ; le ciel de forge (trop rouge ou pas assez : *Sky Idle*, *Sky Flow*) ; la rivière sur les voies qui tournent et
les ponts (elle suit le sol, section par section) ; la sortie de terre (le géant traverse le sol, la vitrine F4 aussi) ; le Quest
(les lumières : la fournaise, la coulée, les éclats ; les explosions de feu de la vague).

### 12.48 Le Colosse à deux mains : la forge, et un marteau par flèche (2 novembre, tard)

Le deuxième essai du joueur (F4) : le son et le visuel vont mieux, « même s'il faudrait retirer le changement de ciel après la
construction, cela doit être temporaire » ; mais « son attaque est très décevante, on ne ressent pas sa puissance puisqu'il
n'attaque qu'une fois tous les certains temps, on ne ressent pas le nombre ». Son idée : un marteau sur chaque flèche tirée, qui irait
bien avec la pluie de flèches et le multishot, mais il craint les lags au casque. Claude propose quatre pistes (la forge qui bat sans
arrêt, un grand coup au 5e ; le marteau qui ricoche d'ennemi en ennemi ; son idée plafonnée : ses flèches donnent la cadence ; la
colère, qui remplacerait la coulée) et conseille la forge et le ricochet. **Le choix du joueur : la forge et son idée** : « d'une main
il frappe sa forge, de l'autre il envoie des marteaux à chaque flèche (cap à 5 par seconde) ». Compilé (le jeu et les outils
d'éditeur, sans erreur ni avertissement), le modèle dessiné hors de Unity sans faces superposées ; **pas encore vu dans Unity ni
testé au casque** (tests : `Checklist_Demo.md`, « Le Colosse de forge, à deux mains »).

**La forge, de la main gauche** : le bras gauche devient un bras articulé, comme celui du marteau, qui tient une masse de forge ;
devant le genou gauche, sur un billot de basalte, la grande enclume (2,4 fois celle de l'Enclume, sa bigorne vers lui) et **un
lingot de fonte** sur sa table (le modèle : « Les deux mains du 2 novembre », `Tools/allure_tours/ultimes.py`, qui calcule l'angle
du bras pour que la masse repose sur le lingot). Il le bat sans arrêt, **un coup toutes les 1,2 s** (les parchemins d'adresse et la
surcharge l'accélèrent ; 2,5 fois moins souvent sans ennemi à 30 m) : il lève la masse et l'abat, les étincelles jaillissent, le fer
sonne, plus aigu à mesure que le lingot chauffe (*impactBell_heavy_001*, du pack de Kenney déjà dans le projet, CC0). Chaque coup
lance **un éclat de fonte** en cloche sur un ennemi au sol, au hasard, à sa portée (30 de dégâts dans 2,5 m). Le lingot chauffe :
rouge sombre et croûté, puis blanc. **Au 5e coup, le grand coup** : il lève sa masse par-dessus la tête, **la foudre la frappe** (le
ciel s'éclaire), il l'abat ; une onde autour de l'enclume, le fer battu, et **le lingot chauffé à blanc part** en cloche sur le
groupe le plus serré, en crépitant d'éclairs. Il tombe avec la foudre : c'est le coup de 12.47 (160 de dégâts dans 6 m, les
survivants projetés, l'onde de choc, la roche, le sol fendu de fonte, le sol en feu). Un lingot neuf revient sur l'enclume. Sans
ennemi à portée, il garde son lingot chauffé à blanc pour le coup suivant. Le buste se penche en arrière quand la masse se lève ;
**il ne se tourne plus** (la masse doit retomber sur l'enclume) : seul le bras du marteau se tourne, à l'épaule.

**Les marteaux, de la main droite** : chaque flèche tirée qui touche un ennemi à 30 m, et il lance sur lui **un marteau de fonte**
(une copie du sien, à 55 %, qui tournoie dans une traînée de feu) : 40 de dégâts dans 3 m, les survivants sautent, un petit éclair
crépite, le coup sonne. Une flèche dans le sol : le marteau va sur l'ennemi au sol le plus proche (5 m au plus), sinon rien. Celles
de la pluie de flèches et du multishot comptent : **5 marteaux par seconde au plus** (le plafond du joueur) ; les flèches attendent
leur marteau (8 au plus, les plus anciennes s'effacent ; 2 s au plus). Le bras se lève par-dessus la tête tant que des flèches
attendent, donne un coup sec par marteau, tourné à l'épaule vers la cible (de 35° vers le dedans à 110° vers le dehors), et se
repose 0,9 s après le dernier. Il ne lance plus son marteau de lui-même : sans toi, la forge travaille seule ; avec toi, il se
déchaîne.

**Pour le casque** : ce qu'il lance (marteaux, éclats, lingot) est réutilisé d'un lancer à l'autre ; les étincelles et la poussière
des petits coups viennent de deux systèmes de particules à lui, déplacés là où ça frappe (pas d'objet neuf par coup), sans lumière ni
éclair du ciel. Les lumières, l'éclair du ciel, les explosions et la roche qui vole restent au grand coup (toutes les 6 s environ).

**Le ciel de forge, passager** : pendant l'éveil, puis le temps d'une coulée ; le reste du temps, le ciel de la partie (le réglage
*Sky Idle* est retiré). Après l'éveil, il revient en 4 s.

**Dans l'arène de MK** : tes flèches dans MK y envoient les marteaux (0,1 % de ses PV max chacun), les éclats de la forge le visent
aussi, et **le lingot le plaque au sol** (3 s, 3 % de ses PV max, au plus toutes les 12 s ; entre deux, le grand coup attend). À
l'arrivée : « Touche MK : ses marteaux suivent tes flèches, son lingot le plaque au sol ».

**Les textes** de la carte et du niveau décrivent les deux mains (les anciens sont remplacés s'ils n'ont pas été changés à la main).

**La puissance** (à régler aux essais, et à mettre au tableau d'équilibrage) : seule, la forge fait environ 25 dégâts par seconde
(les éclats) et 27 (le grand coup), comme le marteau d'avant ; les marteaux s'ajoutent selon tes tirs, 40 par flèche qui touche,
jusqu'à 200 par seconde au plafond. Les réglages sont sur le prefab : *Forge Interval*, *Chunk Damage*, *Strikes Per Ingot*,
*Throw Interval*, *Queue Size*, *Hammer Damage*, *Hammer Radius*, *Hammer Scale*…

**À surveiller aux tests** : la masse qui retombe juste sur le lingot ; le bras du marteau à 5 coups par seconde (trop frénétique ?) ;
le fer qui sonne à chaque coup (lassant ?) ; les lags sous la pluie de flèches ; le grand coup qui attend quand il n'y a personne.

**Une idée du joueur, pour plus tard** (le même soir) : une lumière sous chaque tourelle pendant le boss final, pour mieux les voir
(`FinalBoss/ArenaTurrets.cs`).

### 12.49 Des marteaux imposants, et la force au tableau (2 novembre, tard)

Le troisième essai du joueur : « les marteaux doivent être bien plus imposants, comme la précédente version, et garder les mêmes
dégâts (les dégâts actuels sont vraiment faibles, j'ai l'impression) ; compare avec les autres trios du Excel ». Compilé (le jeu et
les outils d'éditeur, sans erreur ni avertissement) ; **pas encore vu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`,
« Le Colosse de forge, la force »).

**Ce que Claude a trouvé** : le Colosse essayé avait encore les chiffres de 12.46 (le marteau : 140 dans 5 m ; la coulée : 80) ; les
hausses de 12.47 (160 dans 6 m, la coulée plus forte) n'avaient jamais atteint le prefab : le baker de l'allure refait le modèle à
chaque marque, mais garde les réglages déjà posés. Au tableau (la méthode de `Tools/equilibrage_tours.py` : Normal, sans bonus, le
groupe de 5 ennemis serrés), le Colosse de 12.48 faisait **79 dégâts par seconde sur une cible et 307 sur le groupe** (une flèche qui
touche par seconde ; seule, la forge : 39 et 140), quand l'Enclume au niveau 3 fait 169 et 884 et la Grande bombarde 246 et 737 :
l'ultime valait moins que ses propres trios, à peu près une Enclume au niveau 1.

**La force** (`Editor/ColosseForceBaker`, une fois, seulement les réglages qui ont encore leur valeur d'avant ; le code a les mêmes) :
- **les marteaux**, ceux du marteau d'avant : **160 dans 6 m**, les survivants projetés en vrille (8 et 12 m/s) ;
- **la forge**, pour que seule elle vaille un trio moyen au niveau 3 : les éclats **30 → 70 dans 3 m** (2,5 avant), le lingot **140 →
  450 dans 7 m** (5 avant ; la grande enclume de l'Enclume : 350 dans 6 m), projetés plus loin ; le sol en feu un peu plus large et
  plus brûlant ;
- **la coulée de 12.47**, enfin posée : 12 m/s, 90 de dégâts, 50 de brûlure, repoussés à 12 m/s, 1,4 s de torrent, 6 s pour refroidir.

Au tableau, après : **270 sur une cible et 1 158 sur le groupe** avec une flèche qui touche par seconde (1,3 fois l'Enclume au niveau 3),
**430 et 1 891** avec deux (le double de l'Enclume : la règle des ultimes, « environ deux fois un trio »), **910 et 4 091** au plafond de
5 marteaux par seconde (la pluie de flèches, le multishot) ; seule, la forge : **110 et 424** (un trio moyen au niveau 3 : Bowling 484,
Colisée 390). Le Colosse est dans le tableau, avec les trios (`colosse()`, une flèche qui touche par seconde : `HITS_PER_SECOND`) :
relancer `py Tools/equilibrage_tours.py` une fois Unity passé (le prefab a alors ses nouveaux chiffres).

**Des marteaux imposants** (comme le marteau d'avant, 12.47) : chaque marteau lancé est **aussi grand que le sien** (*Hammer Scale* 0,55
→ 1), part de son poing, tournoie haut dans sa traînée de feu en crépitant d'éclairs, se remet tête en bas et frappe : **la foudre tombe
avec lui**, l'onde de choc, l'explosion de feu, la poussière en anneau, les étincelles, le coup et le grand impact ; il **reste planté**
0,3 s, puis s'enfonce dans le sol. Au début d'une salve, **la foudre frappe son marteau levé** (au plus toutes les 4 s).

**Pour le casque** (jusqu'à 5 coups par seconde) : les éclairs et les explosions sont réutilisés (`LightningBolt`, `ExplosionFx`), les
étincelles et la poussière viennent de ses deux systèmes de particules ; le **coup complet** (l'éclat de lumière, le tonnerre, la roche
qui vole, le sol fendu de fonte et en feu, les manettes qui vibrent) au plus toutes les 0,5 s (*Heavy Interval*) ; **l'éclair dans le
ciel** au plus toutes les 1,2 s (*Sky Flash Interval* ; ceux de la masse levée et du grand coup passent toujours) : pas de ciel qui
clignote. Un boss perd 0,2 % de ses PV max par marteau (*Hammer Boss Share* : 1 % par seconde au plus).

**À surveiller aux tests** : la puissance (trop forte avec la pluie de flèches ? *Hammer Damage*, *Throw Interval*) ; la fluidité sous
la pluie de flèches ; les marteaux plantés qui gênent la vue ; le bruit (le coup, le grand impact, le tonnerre).

### 12.50 Les dégâts du Colosse sur les boss des vagues (2 novembre, tard)

Le joueur : « les dégâts de la tour devraient aussi marcher sur les boss » ; puis « pas MK, on garde son effet actuel ». Jusqu'ici, un
boss sous un coup du Colosse ne perdait qu'une part de ses PV max (12.44 : 1,5 % sous le lingot, 0,2 % sous un marteau, 0,1 % sous un
éclat, 1,5 % à l'éveil). Désormais **un boss des vagues prend les mêmes dégâts que les autres** (le marteau, le lingot, l'éclat, l'onde
de l'éveil ; la coulée le blessait déjà), **et en plus** cette part de ses PV max ; il n'est toujours jamais projeté. Comme la Grande
bombarde, qui blesse les boss sans les projeter. **MK, dans l'arène, ne change pas** : ses parts seulement (0,2 % par marteau, 0,1 % par
éclat, 3 % quand le lingot le plaque au sol). Compilé (sans erreur ni avertissement) ; **pas encore vu dans Unity ni testé au casque**.

### 12.51 Le Grand Manège, la deuxième ultime (2 novembre, tard)

Le joueur : « cette conversation aura pour but la création de la deuxième tourelle ultime ». Fait d'emblée avec tout ce que le
Colosse a appris (`Docs/Ultimes_Cahier.md`). Ce qui n'était pas décidé, le joueur l'a choisi parmi les propositions de Claude :
- **l'action seule et continue** : **la ronde** (conseillée ; les autres : les fers à cheval, le tourbillon) ;
- **le grand coup régulier** : **la grande éjection** (pas le cheval d'or conseillé) ;
- **le signe de chaque coup** : **le feu d'artifice** (conseillé ; l'autre : confettis et fanfare) ;
- **MK** : **étourdi** (conseillé ; l'autre : renversé et repoussé).

Codé, compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement), le modèle dessiné hors de Unity sans faces superposées
(`Tools/allure_tours/apercu.py`, « Duo:Grand Manège ») ; **pas encore vu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`,
« Le Grand Manège »). Textes et chiffres : des propositions, à revoir aux essais.

**Le modèle** (`Tools/allure_tours/ultimes.py`, « Manège du 2 novembre » ; le Gonfleur, le Colisée ou les Montagnes russes au niveau 4,
4 × 4 cases, partout comme le Colosse) : un carrousel de 19 m de haut et 15 m de large, **perché sur son mât** (le plateau à 3,6 m :
sur la voie, les ennemis passent dessous, autour du socle). Le mât rouge et crème cerclé d'or, ses huit miroirs ; huit jambes de force
sous le plateau ; l'orgue (dix tuyaux dorés qui fument) et son tambour ; le plateau de planches, son bord doré, sa frise rouge et crème ;
**huit grands chevaux de bois** au galop (3,5 m, chacun sa robe, son tapis de selle et son plumet ; la crinière, la queue et les sabots
dorés) et quatre petits au milieu, sur leurs barres dorées ; le plafond et sa rosace ; la frise du chapiteau (24 panneaux, un miroir
ovale cerclé d'or sur chacun, des pampilles dorées) ; le toit rayé rouge et crème (16 pans, des nervures dorées) ; la lanterne, la
boule dorée, le fanion ; cent quatre ampoules. Le manège tourne, les chevaux montent et descendent sur leurs barres.
- **Ce qui luit** : les ampoules, en trois groupes (« Ampoule A », « B », « C » : trois copies du matériau
  `Art/TowerDefense/Ampoule_de_manege.mat`, animées), qui s'allument **en chenille** (plus vite quand il s'emballe), clignotent toutes
  ensemble avant la grande éjection et s'emballent aux grands moments ; une lumière sous le chapiteau, qui passe doucement de l'or au rose.
- **La texture** : le bois peint et verni d'un carrousel (`Tools/texture_bois_peint.py` : `Art/TowerDefense/Manege_Bois.png`, le fil du
  bois sous la peinture, les coups de pinceau, quelques éclats ; `Manege_Bois_Relief.png`, le relief). `Editor/ManegeFinitionBaker` la
  pose sur ses matériaux (`Palettes/Ultime_Manege_*`), une fois : la peinture brillante, l'or très poli qui luit un peu, les miroirs.
- **Les particules qui ne s'arrêtent pas** : des confettis qui tombent du bord du chapiteau, des paillettes dorées du sommet, la vapeur
  de l'orgue.
- **Pour le casque** : ses 940 pièces sont fondues au lancement (avant le plan bleu) en un maillage par matériau ; chaque cheval en un
  maillage (il bouge seul) ; les ampoules en trois maillages (la chenille).

**L'éveil** (bâti au marteau, ou posé par F4) : « Le Grand Manège s'éveille ! » ; il **sort de terre en tournant**, comme une boîte à
musique, de moins en moins vite (17 m, 2,6 s), dans la terre qui vole, la poussière et les confettis, le sol qui tremble ; une fusée
monte. Debout, ses ampoules s'allument, l'orgue sonne la fanfare, ses confettis partent ; **six fusées** partent du toit en éventail et
éclatent en bouquets ; les chevaux se cabrent et hennissent ; puis **l'onde de confettis** sous un anneau de six bouquets : « LE GRAND
MANÈGE ! », elle renverse tous les ennemis à 22 m (100 de dégâts). Il se bat 0,6 s plus tard.

**Le ciel de fête** (`World/SkySettings.Fete`, posé par-dessus celui de la partie, comme le ciel de forge) : une nuit violette et rose,
une grosse lune dorée, des aurores roses, des étoiles filantes. **Passager** : plein pendant l'éveil, fort pendant la charge (85 %),
rien le reste du temps ni dans l'arène.

**Le feu d'artifice, le signe de chaque coup** (ce qu'est la foudre pour le Colosse) : un bouquet de couleur éclate à 11 m au-dessus de
chaque coup (une sphère d'étoiles, un cœur blanc, des paillettes qui retombent ; un seul système de particules, déplacé là où ça
éclate) ; aux grands moments, une fusée monte en sifflant avec sa traînée dorée et éclate, et le ciel s'éclaire.

**Les trois couches du rythme** (`Building/UltimeManege.cs`) :
- **La ronde, seul et sans arrêt** : toutes les 1,2 s, un **ruban doré** part du bord du chapiteau, attrape l'ennemi au sol le plus
  avancé vers ta tour, à 30 m (60 de dégâts, « Dans la ronde ! »), et l'amène sur le manège en cloche ; il y tourne parmi les chevaux, à
  sa place (8 places, entre les grands chevaux), pris de **tournis** (il tourne sur lui-même, 70 de dégâts par seconde). Un boss ou un
  volant n'est jamais attrapé : le ruban le fouette (ses dégâts ; un boss, plus 0,1 % de ses PV max).
- **La grande éjection, au 5e coup de la ronde** (toutes les 6,9 s), qu'on voit venir : le manège **s'emballe**, les ampoules clignotent
  ensemble, l'orgue sonne la fanfare, une fusée monte du toit (0,9 s) ; puis **tous ceux qui tournent sont éjectés d'un coup**, en vrille,
  chacun vers sa place autour du groupe le plus serré à sa portée (sans groupe : sur la voie, vers la forêt), guidés en vol. Chacun y
  retombe sous un bouquet : 200 de dégâts pour lui (plus sa chute, 18 % de ses PV), 100 dans 5 m autour, les survivants projetés,
  l'onde dorée, les confettis. Sans personne sur le manège, elle attend le premier.
- **Un cheval par flèche qui touche** (l'action du joueur) : chaque flèche tirée qui touche un ennemi à 30 m (ou le sol à 5 m de lui),
  et **le grand cheval le plus proche saute du manège** (une copie, aussi grande que lui) et galope dans les airs jusqu'à la cible en se
  balançant, dans une traînée de paillettes ; il la **piétine** debout : 160 de dégâts dans 6 m (ceux des marteaux du Colosse, que le
  joueur a validés), le bouquet au-dessus, l'onde dorée, les confettis, les survivants projetés en vrille ; il reste cabré un instant,
  puis s'efface dans une gerbe de paillettes. Ceux de la pluie de flèches et du multishot comptent : **5 par seconde au plus** (le plafond
  du joueur) ; les flèches attendent (8 au plus, 2 s au plus). Le manège tourne plus vite tant que des flèches attendent (il s'emballe),
  et son orgue monte avec lui. Pour le casque, comme le Colosse : le coup complet (la lumière, le grand impact, les manettes) au plus
  toutes les 0,5 s, l'éclair du ciel toutes les 1,2 s.

**La charge** (« tous les chevaux descendent la voie au galop », 12.44) : toutes les 25 s, avec au moins six ennemis au sol sur la voie la
plus proche, entre lui et la forêt : « La charge ! », la fanfare, le hennissement, les chevaux se cabrent, trois fusées ; le ciel de fête.
**Les huit grands chevaux sautent du manège sur la voie** (deux rangs, en travers), touchent le sol (l'onde, le grand impact), puis la
**descendent au galop** vers la forêt sur 70 m (12 m/s) : chaque ennemi croisé, 90 de dégâts, « Piétiné ! », projeté vers la forêt en
vrille ; la poussière, les confettis, un bouquet au-dessus de la troupe toutes les 0,55 s, le galop qui la suit. Au bout, ils
s'effacent dans une gerbe de paillettes.

**Les boss** : un boss des vagues prend tous ses dégâts comme les autres, plus une part de ses PV max (1 % sous un éjecté ou la charge,
0,2 % sous un cheval, 0,1 % sous le ruban), sans jamais être attrapé ni projeté (la règle de 12.50).

**Dans l'arène de MK** (il y descend comme le Colosse, à 0,65) : « Le Grand Manège est là ! Touche MK : ses chevaux suivent tes flèches ;
au 5e tour, ils le chargent et l'étourdissent ». Tes flèches dans MK y envoient les chevaux (0,2 % de ses PV max chacun) ; à chaque coup
de la ronde, le ruban le fouette (0,1 %) ; au 5e, **tous ses chevaux le chargent dans les airs**, l'un après l'autre, et le premier
l'**étourdit** 3 s (3 % de ses PV max ; au plus toutes les 12 s). **MK étourdi** (`FinalBoss/DragonBoss.Daze.cs`, un état nouveau,
`DragonBoss.Daze`) : il n'attaque plus ; en vol, il flotte sur place en tournant sur lui-même et dodeline ; au sol, il reste planté et
tourne en titubant ; des étoiles dorées et **quatre petits chevaux tournent autour de sa tête** ; puis il reprend le combat (en vol, son
cercle). Seulement en plein vol en cercle, ou au sol pendant le combat (pas pendant une ruée, un perchoir, un salto, l'intro) : sinon,
la charge attend la suivante.

**La force, au tableau d'équilibrage** (`Tools/equilibrage_tours.py`, `manege()`, dans la feuille « Trios » sous le Colosse ; Normal, sans
bonus, le groupe de 5 ennemis serrés ; le tableau relancé le même soir, les 6 changements du joueur repris) :
- **seul, sans le joueur** : **87 sur une cible et 484 sur le groupe** (et la chute des éjectés : 2,6 % des PV d'une cible par seconde) ;
  la cible du cahier : un trio moyen au niveau 3 (environ 110 et 400 à 500 ; le Colosse seul : 110 et 424). Plus fort sur les groupes,
  un peu moins sur une cible : la ronde est faite pour les foules ;
- **avec une flèche qui touche par seconde** : **247 et 1 218** (le meilleur trio au niveau 3 : la Grande bombarde, 246 sur une cible ;
  l'Enclume, 884 sur le groupe ; le Colosse : 270 et 1 158) ;
- **avec deux** : **407 et 1 951** (environ deux fois le meilleur trio ; le Colosse : 430 et 1 891) ; au plafond de 5 : 887 et 4 151.
- La charge (toutes les 25 s) n'est pas comptée, comme la coulée du Colosse. Le tournis est passé de 60 à 70 par seconde pour que la
  ronde seule approche un trio moyen (avant le premier passage dans Unity : la valeur du code suffit, le prefab n'existe pas encore).

**Les sons** : demandés au joueur dès le début, comme le veut le cahier ; Claude a proposé des sons CC0 de freesound, le joueur les a
téléchargés dans `Audio/` le même soir (licences vérifiées : CC0) : *Carousel Organ.aif* d'Ohrwurm (64562), *Trumpet Fanfare* de
bevibeldesign (350428), *a_herd_of_horses.wav* de gurek (547967), *Horse_neighing.wav* de Lydmakeren (510903), *Whistle and Explosion
Single_Firework* de Rudmer_Rotteveel (336008). Découpés par Claude en Python (mono, 16 bits ; détail dans `Credits_Licences.md` ; les
outils dans `Tools/sons/`, réutilisables : `decoupe_manege.py` refait les sept fichiers), lus seulement en forme d'onde et en spectre
(**à écouter en jeu**) :
- `Audio/Manege_Orgue.wav` (*Organ Clip*, en boucle, doux, plus aigu quand il s'emballe) : 16,4 s de musique, bouclées par un fondu
  enchaîné de 0,6 s là où le rythme de la fin ressemble le plus à celui du début (cherché sur l'enveloppe des attaques) ;
- `Manege_Fanfare.wav` (*Fanfare Clip* : l'éveil, la charge ; 7 s) et `Manege_Fanfare_Courte.wav` (*Flourish Clip*, un réglage
  nouveau : sa première phrase, 1,6 s, pour la grande éjection, qui revient toutes les 7 s ; elle ne joue pas par-dessus la fanfare
  entière) ;
- `Manege_Galop.wav` (*Gallop Clip*, en boucle, la charge) : 2,66 s de la troupe au plus fort, au niveau égalisé (la troupe passe et
  enfle), bouclées de même ;
- `Manege_Hennissement.wav` (*Neigh Clip*) : un hennissement, 1,5 s ;
- `Manege_Fusee.wav` (*Rocket Clip*) : le sifflement (un son pur qui descend de 6 000 à 4 400 Hz), sans ses graves, remonté de 35 dB
  (il était très bas) ; `Manege_Bouquet.wav` (*Burst Clip*, le signe de chaque coup) : l'explosion, 0,95 s.
Le fichier d'allure les nomme (le prefab n'était pas encore fait) ; `Editor/ManegeSonsBaker` les pose aussi s'ils arrivent après le
prefab (la leçon de 12.46). Ceux du projet restent : le sabot (`impactWood_heavy`), le ruban (`impactSoft_medium`), le souffle, le grand
impact de Wakerone.

**Le système** ne change pas : le catalogue a une deuxième ultime (ses trios : Gonfleur, Colisée, Montagnes russes) ; la vitrine **F4**
montre le Colosse, puis le Manège, sur la voie du centre. Une seule ultime par partie, toujours.

**À surveiller aux tests** : la place (15 m de large : il déborde de la voie des deux côtés) et la vue depuis la tour (19 m) ; les
ennemis qui tournent (bien à leur place sur le plateau, pas dans les chevaux) ; la grande éjection (ils retombent bien sur le groupe) ;
les chevaux lancés (assez imposants ? trop nombreux sous la pluie de flèches ?) ; le feu d'artifice (lisible de loin, pas trop chargé à
5 par seconde) ; la chenille des ampoules (assez lumineuse ? la lumière dorée et rose) ; le toit (de fines fentes entre les pans près de
la pointe) ; la charge sur les voies qui tournent et les ponts (elle suit la voie et le sol) ; MK étourdi (en vol, au sol ; les petits
chevaux autour de sa tête) ; le Quest (la fonte des pièces au lancement, les lumières des grands coups, les bouquets).

### 12.52 La Grande Horloge, la troisième ultime (2 novembre, tard)

Le joueur : « parfait, partons sur l'horloge, fais-moi des propositions avec choix comme le carrousel ». Faite d'emblée d'après
`Docs/Ultimes_Cahier.md` (et sa partie 7, ce que le Manège a appris). Les choix du joueur, en deux séries de questions :
- **l'action seule** : **les chiffres** (conseillés ; les autres : les heures au sol, le rembobinage) ;
- **le grand coup** : sa réponse à lui, **Minuit après les douze chiffres** : « on reprend l'ancien Beffroi avec son Za Warudo qui dure
  9 secondes, une fois que les 12 sont passés » (les propositions : le coup de l'heure, le rembobinage de l'heure, la chute des heures) ;
- **sa flèche** : **l'écho du temps** (pas l'aiguille conseillée ; l'autre : la bulle du temps) ;
- **le signe de chaque coup** : **l'aurore** (pas le cadran fantôme conseillé ; l'autre : les pics de glace) ;
- **le modèle** : **l'horloge astronomique** (pas la tour-horloge conseillée ; l'autre : l'horloge comtoise) ;
- **la fin de Minuit** : **le dégel qui brise** (conseillé ; l'autre : comme le Beffroi) ;
- **MK** : **Minuit dans l'arène** (pas « figé en plein vol » conseillé ; l'autre : rembobiné).

Codé, compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement), le modèle dessiné hors de Unity sans faces superposées
(`Tools/allure_tours/apercu.py`, « Duo:Grande Horloge ») ; **pas encore vu dans Unity ni testé au casque** (tests : `Checklist_Demo.md`,
« La Grande Horloge »). Textes et chiffres : des propositions, à revoir aux essais.

**Le modèle** (`Tools/allure_tours/ultimes.py`, « Horloge du 2 novembre » ; le Bowling ou le Beffroi au niveau 4, 4 × 4 cases, partout) :
une horloge astronomique de 18 m de haut et 16,6 m de large, **qui flotte** au-dessus de son socle de glace cerclé d'or (elle monte et
descend doucement, reliée au socle par un rayon de glace qui luit ; trois grands cristaux autour). **Le cadran** (8 m) : la glace bleue
cerclée d'or, le ciel de nuit étoilé au milieu, un soleil d'or et une lune d'argent, les marques dorées des heures, **les douze chiffres
romains en glace qui luit** (des barres de glace, lisibles de face, dans le bon ordre), les trois aiguilles, le capuchon doré ; derrière,
trois engrenages qui tournent ; au sommet, la cloche sous son arche dorée ; dessous, des stalactites. **Trois grands anneaux** tournent
autour (7,8 m de glace, 6,9 m d'argent penché à 70°, 6,2 m d'or penché à 25°, des gemmes de glace qui luit, le soleil et la lune sur le
premier). **Le cadran se tourne vers le joueur** (on lit toujours ses chiffres) ; l'arrière est d'or, avec ses engrenages.
- **Ce qui luit** : les chiffres, les étoiles, les gemmes, les cristaux, le rayon : le matériau `Art/TowerDefense/Givre_lumineux.mat`,
  copié en jeu et animé (il palpite, monte à mesure que le XII approche, s'emballe aux grands moments) ; une lumière froide au cadran.
- **La texture** : une glace taillée (`Tools/texture_glace.py` : `Art/TowerDefense/Horloge_Glace.png`, des fêlures, des bulles, du
  givre ; `Horloge_Glace_Relief.png`) ; `Editor/HorlogeFinitionBaker` la pose (`Palettes/Ultime_Horloge_*`), une fois : la glace très lisse
  qui luit à peine, la nuit du cadran polie, l'argent et l'or très polis.
- **Les particules** : des flocons autour d'elle, des éclats de givre autour des anneaux, la brume froide au pied.
- **Pour le casque** : fondue au lancement, une pièce par partie qui bouge (le cadran, chaque aiguille, chaque engrenage, chaque anneau,
  chaque chiffre, la cloche) ; ce qu'elle lance est une copie fondue, réutilisée ; dix aurores au plus.

**L'éveil** : « La Grande Horloge s'éveille ! » ; le sol se couvre de givre, elle sort de terre (14 m, 2,4 s), ses anneaux à toute vitesse,
dans la glace qui vole et la brume ; une aurore tombe sur elle. Debout, sa glace s'allume, ses **douze chiffres apparaissent un à un**
(un tic chacun, de plus en plus aigu) ; ses aiguilles tournent à l'envers à toute vitesse et s'arrêtent sur midi ; la cloche sonne :
« LA GRANDE HORLOGE ! », l'onde de givre fige tous les ennemis à 22 m (100 de dégâts, figés 2 s), sous un anneau de six aurores.

**Le ciel de Minuit** (`World/SkySettings.Minuit`) : une nuit d'hiver bleu profond, une grande lune pâle presque au zénith, de grandes
aurores, des étoiles filantes. Passager : plein pendant l'éveil, fort pendant Minuit (et quand les aiguilles se rejoignent), rien le reste
du temps ni dans l'arène.

**L'aurore, le signe de chaque coup** : trois rubans de lumière verte, bleue et violette tombent du ciel sur le coup (26 m), ondulent et
s'effacent (1,1 s) ; sur chaque chiffre, chaque écho, chaque figé qui éclate.

**Le rythme** (`Building/UltimeHorloge.cs`) :
- **Les chiffres, seule et sans arrêt** : un tic toutes les 1,2 s (la trotteuse saute, le tic sonne) ; s'il y a un ennemi à 30 m (au sol
  ou en vol), **le chiffre suivant se détache du cadran** (I, puis II, puis III…) et part en cloche sur un ennemi au hasard, en tournant :
  220 de dégâts dans 4,5 m, ceux qui survivent figés de givre 1 s, l'aurore, son nom en lettres de glace (« IV »). L'aiguille des heures
  avance avec les chiffres partis, celle des minutes fait un tour à chaque tic : **on voit venir le XII**. Sans ennemi, l'horloge attend.
- **Minuit, après le XII** (le Beffroi au niveau 3 d'avant, avec les réglages du joueur : la bulle sur toute la carte, 300 m, 9 s, les
  boss figés 2 s) : les aiguilles se rejoignent sur midi (1 s ; le ciel de Minuit) ; « Za Warudo ! Temps, arrête-toi ! » : la bulle part
  de l'horloge, le monde en noir et blanc, tout s'arrête sauf toi (tes flèches attendent en l'air ; les boss des vagues aussi) ; l'horloge aussi s'arrête (ses
  anneaux, ses aiguilles). Douze coups de cloche (« Dong ! 1 »…), **un chiffre repousse à chacun**. Au douzième, **le dégel qui brise** :
  « Le temps reprend son cours », tes flèches repartent toutes ensemble, et **le givre de tous les ennemis figés éclate d'un coup** (500
  de dégâts à chacun ; les huit premiers ont leur aurore et « Brisé ! »). Puis le I au tic suivant. Un tour : 23,4 s.
- **L'écho du temps, à chaque flèche qui touche** : chaque flèche tirée qui touche un ennemi à 30 m (ou le sol à 5 m de lui) est **rejouée
  1 s plus tard** par une **flèche de glace géante** (4,3 m) qui part du cadran, file droit sur la cible et la frappe : 160 de dégâts dans
  6 m (les marteaux du Colosse, les chevaux du Manège), les survivants projetés et figés 1 s, l'aurore, l'onde ; elle reste plantée
  0,5 s, puis éclate en givre. **5 par seconde au plus** ; 8 en attente au plus. Pendant Minuit, tes flèches attendent en l'air : leurs
  échos viennent après le dégel.
- Le coup complet (la lumière, l'éclair du ciel, les éclats de glace, le givre au sol, les manettes) au plus toutes les 0,5 s.

**Les boss** : un boss des vagues prend tous ses dégâts, plus une part de ses PV max (0,2 % sous un chiffre ou un écho, 2 % au dégel), figé
par Minuit toute la durée comme les autres (3 novembre, le joueur ; le Beffroi ne les figeait que 2 s), jamais projeté.

**Dans l'arène de MK** (à 0,65) : « La Grande Horloge est là ! Touche MK : tes flèches sont rejouées ; après le XII, Minuit arrête toute
l'arène ». Ses chiffres partent sur MK (0,2 % de ses PV max chacun), ses échos aussi ; **après le XII, Minuit arrête toute l'arène 6 s**,
MK compris (`FinalBoss/DragonBoss.TimeStop.cs`, `DragonBoss.StopTime` : il s'arrête là où il est, en vol ou au sol, son animation figée,
ses boules de feu en vol aussi, ce qu'il préparait s'éteint ; puis il reprend exactement où il en était) ; le noir et blanc, tes flèches
qui attendent et le frappent toutes ensemble à la fin ; au dégel, MK perd 3 % de ses PV max (sa part seulement, pas les 500).

**La force, au tableau** (`manege()` et `horloge()`, feuille « Trios » ; le tableau relancé, les 6 changements du joueur repris) :
- **seule** : **134 sur une cible et 413 sur le groupe** (la cible : un trio moyen ; le Colosse seul : 110 et 424) ;
- **avec une flèche qui touche par seconde** : **294 et 1 146** (le Colosse : 270 et 1 158) ;
- **avec deux** : **454 et 1 880** (le Colosse : 430 et 1 891) ; au plafond de 5 : 934 et 4 080.
- Le tableau ne compte que le groupe de 5 : le dégel brise **tous** les figés de la carte (300 m), et Minuit fige tout 8 s sur 23 :
  c'est son vrai poids. À surveiller aux essais (*Shatter Damage*, *Midnight Time*).

**Les sons** : demandés au joueur dès le début (freesound, CC0 ; licences vérifiées), téléchargés par lui le 3 novembre, découpés par
Claude (`Tools/sons/decoupe_horloge.py` ; détail dans `Credits_Licences.md`), lus seulement en forme d'onde et en spectre (**à écouter
en jeu**) :
- `Audio/Horloge_Tic.wav` (*Tick Clip*) : le tic le plus net de *Clock ticking* (Mortifreshman, 237210), très bas dans l'enregistrement
  (à peine au-dessus de son souffle, grave) : coupé court (87 ms), sans ses graves, qui s'éteint vite ;
- `Horloge_Cloche.wav` (*Bell Clip*, les douze coups) : un coup de *church bells strike 12 midday* (Astounded, 481969), jusqu'au
  suivant (2,7 s), ramené de 96 à 48 kHz ; à Minuit, un coup toutes les 0,75 s : ils se chevauchent ;
- `Horloge_Arret.wav` (*Stop Clip*) : la dernière seconde de *Reverse Crash Cymbal Projection* (383903), qui enfle et se coupe net ; il
  part maintenant quand les aiguilles se rejoignent (1 s avant Minuit), pour que son sommet tombe quand le temps s'arrête ;
  `Horloge_Reprise.wav` (*Resume Clip*, le dégel) : la même cymbale remise à l'endroit, le coup qui s'éteint ;
- `Horloge_Bris.wav` (*Shatter Clip*, le dégel) : la série de craquements de *Ice break* (humanoide9000, 329744) ; `Horloge_Eclat.wav`
  (*Impact Clip* : chaque chiffre, chaque écho, chaque figé qui éclate) : trois craquements serrés ; un peu écrêtés tous les deux (très
  brefs, ils sonnaient maigres) ;
- `Horloge_Carillon.wav` (*Chime Clip*, les échos) : *GLEAM-GLOW-SFX-CHIME* (newagesoup, 351408), depuis son attaque.
Unity avait déjà fait le prefab avec les sons provisoires : `Editor/HorlogeSonsBaker` les remplace (il passe quand les sons sont
importés) ; le fichier d'allure nomme les nouveaux. Restent ceux du projet : le souffle, le grand impact de Wakerone.

**Le Beffroi** garde son Minuit en sommeil (rien ne l'atteint) : l'Horloge a le sien, repris de lui.

**À surveiller aux tests** : la place (16,6 m de large ; les anneaux passent à 4,5 m du sol au plus bas) et la vue depuis la tour ; le
cadran qui se tourne vers toi ; les chiffres lisibles de loin ; le tic toutes les 1,2 s (lassant ?) ; Minuit toutes les 23 s environ
(trop souvent ? le noir et blanc 8 s sur 23) ; le dégel sur une grande vague (beaucoup de figés d'un coup : le casque) ; les échos sous la
pluie de flèches ; les aurores (lisibles, pas trop chargées) ; MK arrêté (en vol, au sol, pendant une ruée) et ses boules de feu figées.

**Le 3 novembre, la première retouche du joueur** (après un essai dans Unity : « les particules de glace au pied de la tour et à
l'impact des flèches sont trop grosses, réduis-les, de même pour les aurores des chiffres ; arrête le temps pour les boss de vagues pour
la même durée que le reste (9 secondes) ») : la brume froide au pied (0,4 à 0,8 m, au lieu de 1,2 à 2,2, et elle grossit moins), la
poussière de glace des coups (0,25 à 0,55 m, au lieu de 0,8 à 1,6), les éclats de givre (0,05 à 0,14 m) et les éclats de glace qui volent
(deux fois plus petits) ; l'aurore d'un chiffre à 0,5 de sa taille (13 m de haut ; celle d'un écho reste à 1,1) ; Minuit fige les boss
des vagues toute la durée (le réglage *Boss Stop Time* du Beffroi, 2 s, est retiré de l'Horloge ; MK, dans l'arène, a son temps arrêté à
lui). Ces tailles sont dans le code (les particules sont faites en jeu) : rien à poser sur le prefab. Compilé ; **à revoir dans Unity**.
Le même jour (le joueur : « retirer les 3 petits cristaux à la base de l'horloge ») : le socle n'a plus ses trois cristaux (leur
cœur qui luisait compris). Le modèle a une nouvelle marque (« Horloge du 3 novembre ») et il est refait (*rebuild*) : à l'ouverture de
Unity, le baker de l'allure retire toutes ses pièces et le reconstruit ; les réglages du script restent.

Puis (le joueur : « pendant l'arrêt du temps de l'horloge, enlève les autres sons et musique, laisse juste les bruits de l'horloge et du
joueur ») : **le silence de Minuit** (`Core/Sfx.Hush`, appelé à chaque image de Minuit ; `Sfx.Unhush` au douzième coup, et dans tous les
cas où Minuit s'arrête). La musique, l'ambiance et toutes les sources du monde (les tours, les ennemis, MK et la musique de l'arène, les
boucles des autres ultimes) sont coupées (*mute* : elles continuent sans qu'on les entende, et leurs scripts gardent la main sur leur
volume), y compris celles qui commencent pendant (on les cherche 4 fois par seconde) ; les sons ponctuels déjà partis s'arrêtent, et les
nouveaux ne passent que s'ils sont joués à moins de 2,5 m de la tête du joueur (son arc, sa montre, ses pas, ses coups de katana). Les sons
de l'Horloge passent tous (`Sfx.PlayThroughHush`) : le tic, la cymbale qui monte avant Minuit, les douze coups, la reprise et le bris. Au
douzième coup, tout revient d'un coup, avec la cymbale de la reprise. Compilé ; **à entendre dans Unity**.

Puis (le joueur, avec un extrait du « Za Warudo » de JoJo : « j'aimerais que l'arrêt du temps se fasse vraiment ressentir ; juste après
[la phrase], il y a un bruit assez saturé annonçant l'arrêt du temps, reproduis-le ») : **le bruit de l'arrêt du temps**. L'extrait, un
MP3, est converti par le transcodeur de Windows (`Tools/sons/mp3_en_wav.ps1` : pas de décodeur MP3 en Python sur ce PC) et lu en forme
d'onde et en spectre : après la voix, environ 1,9 s en deux temps, un bourdon grave saturé qui descend (134 → 116 Hz, comme une bande
qui ralentit), puis un grave qui remonte (65 → 80 Hz, son 3e harmonique fort) avec un sifflement fixe (1,67 kHz, puis 3 kHz), et une
coupure nette sur un claquement aigu. **Synthétisé** sur ce dessin (`Tools/sons/arret_du_temps.py` → `Audio/Horloge_Temps_Arrete.wav`,
1,8 s ; rien de l'extrait n'est repris), plus court et plus lourd : un impact (une grosse caisse saturée qui tombe de 120 à 40 Hz), le
bourdon qui descend (134 → 112 Hz, une dent de scie saturée qui palpite), le grave qui remonte (64 → 82 Hz) avec le sifflement, la
coupure à 1,4 s (l'écho compris) sur un claquement, puis presque rien. Calculé à 192 kHz (la saturation ne se replie pas dans l'aigu),
la présence relevée de 0,6 à 5 kHz, saturé deux fois ; son profil suit celui du modèle. **En jeu** (*Time Stop Clip*, *Time Stop Cut*
1,4 s) : il part au début de Minuit, à plat (on l'entend partout, fort), avec une vibration des deux manettes ; il passe le silence ; le
premier coup de cloche tombe sur sa coupure, les onze autres se serrent derrière (le douzième tombe au même moment qu'avant : 0,62 s
entre deux coups au lieu de 0,75). La cymbale qui monte pendant que les aiguilles se rejoignent reste avant lui. Le son est posé sur le
prefab par `Editor/HorlogeSonsBaker` (un réglage vide) ; le fichier d'allure le nomme. **L'extrait lui-même n'est pas utilisé** (une
série animée, sous droits ; `Credits_Licences.md`) : rangé hors du dépôt, dans `Desktop/dev/Sons_modeles/`. Compilé ; **à entendre dans Unity**.

### 12.53 Le chantier du début : un trio (3 novembre)

Le joueur : « il faudrait que les 3 premiers bâtiments au début de chaque game soient forcément des bâtiments d'un trio du jeu, et
augmenter la limite à 4 bâtiments : le 4e étant les barricades (ou un autre aléatoire si les barricades font partie du trio) ».

**Codé** (`Building/BuildManager`, `PickStartingStructures`) : au lancement, un trio est tiré au hasard parmi les 7 ; ses 3 tours (les
deux de son duo et la sienne) sont au chantier. Puis la **Barricade** ; si le trio l'a déjà (l'Enclume, le Bowling, le Colisée), un
autre bâtiment, tiré comme ceux qui arrivent après chaque boss (la rareté d'abord). Un trio, et l'ultime de sa famille, est donc
possible dans chaque partie, et la partie commence avec de quoi faire deux duos au moins (celui du trio, et souvent un autre avec la
Barricade : le Boulet fou ou le Marteau-pilon). Au premier gong : « Ton chantier : Mortier, Bulle, Baliste et Barricade », puis « De
quoi bâtir un trio : Gonfleur ». Un bâtiment de plus après chaque boss, comme avant.

Les 7 trios et leur chantier : Enclume (Barricade, Pièges à piques, Mortier, + un au hasard), Gonfleur (Mortier, Bulle, Baliste,
Barricade), Bowling (Mortier, Tour de givre, Barricade, + un), Beffroi (Pièges à piques, Tour-aimant, Phare, Barricade), Colisée
(Phare, Bulle, Barricade, + un), Montagnes russes (Tour-aimant, Lance-filet, Tremplin, Barricade), Grande bombarde (Mortier, Phare,
Tremplin, Barricade).

Le réglage *Starting Structures* (3, dans la scène) est remplacé par *Extra Starting Structures* (1 : les bâtiments après ceux du
trio) ; un réglage nouveau prend sa valeur du code : rien à enregistrer dans la scène. Sans catalogue de trios, le tirage d'avant
(au hasard). Le tutoriel (« Ouvrir les coffres ») dit « 4 bâtiments au début (de quoi bâtir un trio) » : `Editor/StartingTrioBaker`,
une fois, si la fenêtre a encore l'ancien texte mot pour mot. Compilé ; **pas encore vu dans Unity**.

### 12.54 L'action par flèche des ultimes suit la force du tir ; le bruit de l'arrêt du temps, progressif (3 novembre)

Le joueur : « fais en sorte que chaque action qui se passe à chaque flèche lancée par le joueur avec les ultimes scale en fonction de
si le joueur fait un parfait, moyen, etc. ; de même, cela scale aussi avec la force de la flèche (la pluie réduit les dégâts, etc.) ».

**Codé** (`Building/TowerUltimate.ShotPower`) : la force d'une flèche est ses dégâts à l'impact, rapportés à ceux d'un tir « Bon » du
même arc avec les mêmes bonus (`Bow.MeleeDamage`), de 0,25 à 3. Les dégâts d'une flèche comptent déjà tout : la qualité du tir (les
dégâts de `Data/ShotTuning` : raté 0,75, moyen 1, bon 1,25, très bon 1,6, parfait 2,5), l'arc plus ou moins bandé, la corde de siège,
les flèches de la pluie et du multishot (moins fortes), les pièces qui changent les dégâts en vol. D'où : **parfait ×2, très bon ×1,28,
bon ×1, moyen ×0,8, raté ×0,6**, une flèche de la pluie bien moins. Les parchemins n'y comptent pas (ils renforcent déjà les tours,
*Structure Scroll Share*, 12.36), ni l'arc lui-même (les deux s'annulent dans le rapport). Le marteau du Colosse, le cheval du Manège
et l'écho de l'Horloge frappent d'autant : leurs dégâts et leur part des PV d'un boss (MK compris) ; le rayon, le recul et les effets
ne changent pas. La charge du Manège (sans flèche) reste à 1. File pleine (la pluie de flèches : 8 flèches en attente au plus), la plus
faible laisse sa place à une plus forte (à force égale, la plus ancienne, comme avant) : un tir parfait n'est plus perdu sous la pluie.
Au tableau, une flèche qui touche compte pour un tir « Bon » (le texte des trois ultimes le dit ; relancé, les changements du joueur
gardés) : les chiffres ne changent pas.

**Le bruit de l'arrêt du temps, retouché** (le joueur, après l'avoir entendu : « le bruit de JoJo d'arrêt du temps est plus progressif,
et fais-le moins fort aussi ») : plus d'impact au départ (la grosse caisse et le souffle) ; à leur place, un très grave (42 Hz) qui
enfle ; le bourdon monte en 0,4 s ; tout enfle jusqu'à la coupure (de −45 dB au départ à −9 dB juste avant) ; la crête à 0,8 (0,95), et
joué à 0,6 en jeu (1) : environ 7 dB de moins en moyenne. La vibration des manettes passe du début de Minuit au premier coup de cloche,
sur la coupure (0,6 au lieu de 0,8). `Tools/sons/arret_du_temps.py`, relancé.

Compilé ; **pas encore vu ni entendu dans Unity**.

**Le même jour, quatre erreurs dans la console de Unity** (« The same field name is serialized multiple times in the class or its
parent class ») : `m_ArenaScale`, privé dans `TowerUltimate` (la taille hors de l'arène) et réglable dans chaque ultime (0,65), et
`m_BoostUntil`, privé dans `Structure` et dans le Manège. Dans l'éditeur, Unity sérialise aussi les champs privés au rechargement
des scripts, d'où le conflit ; le compilateur, lui, l'accepte. Renommés ceux qui ne portent pas de réglage (`m_ScaleOutsideArena`,
`m_SpinBoostUntil`) : les réglages des prefabs restent. Un outil le cherche désormais : `Tools/champs_en_double.py` (aucun ailleurs).

### 12.55 La flèche « ULTIME », la pose lisible, la lumière de l'arène, la fluidité (3 novembre)

Le joueur : « fais tous les points 2 » (le point de Claude sur la suite de la refonte : ce qui aide à jouer sans rien ajouter à
apprendre). Codé, compilé (`compile_check`, `compile_check_editor`, `champs_en_double` : rien), **pas encore vu dans Unity ni testé
au casque** (tests : `Checklist_Demo.md`, « Le 3 novembre : la flèche ULTIME, la pose lisible, les menaces par voie »).
- **La flèche « ULTIME »** (`Building/DuoHints`, avec les flèches « DUO » et « TRIO ») : entre les vagues, une flèche dorée droite
  plonge sur chaque trio au niveau 3 dont la famille a son ultime, avec le mot « ULTIME », le nom de l'ultime et ses plans
  (« 4 / 10 plans », de l'une de ses trois tours) ; même sans assez de plans : on voit ce qu'on vise. Plus aucune dès qu'une
  ultime est bâtie (une par partie).
- **La pose lisible** (le lot 4 de 12.7, sa deuxième moitié ; le phare de soutien de la première moitié était déjà fait à sa
  façon) : pendant la pose (et le déplacement), la portée du bâtiment est dessinée au sol, dans la grille : un cercle de cases
  dorées, et les cases de la voie à sa portée, dorées elles aussi ; et le texte de la pose dit « Portée 24 m · couvre 31 m de la
  voie de gauche, 12 m de la voie du milieu » (la longueur du chemin des ennemis à portée, par pas d'un demi-mètre ; « aucune
  voie à portée » sinon). Les pièges de la voie et la barricade, qui agissent sur leurs cases, n'ont pas de cercle. La portée :
  `Structure.ReachAt(niveau)` (au niveau 1, ou au niveau du bâtiment déplacé), celle des réglages de chaque bâtiment : baliste
  et lance-filet, tour de givre, mortier, tour-aimant, bulle, phare ; les duos, trios et ultimes qui ont une portée (*Range*),
  la Boussole folle (sa rose). Les voies sont nommées de gauche à droite vues de la tour (`BuildGrid.SortedLanes`, `LaneName`).
- **La lumière sous les tourelles de l'arène** (l'idée du joueur ; `FinalBoss/ArenaLights`) : à côté de la rune qui s'allume
  sous chaque tourelle posée sur le plateau, une lumière ponctuelle à 50 cm du sol (8 m, sans ombre) qui l'éclaire d'en bas et
  respire avec la rune ; elle ne baisse pas quand l'arène s'assombrit. Le Quest éclaire chaque objet par 2 lumières au plus.
- **La fluidité au Quest** (sans casque : une relecture du code, pas une mesure) : l'arrêt du temps (Minuit de la Grande Horloge)
  cherchait les flèches dans toute la scène vingt fois par seconde (`FindObjectsByType`, un tableau à chaque fois) : il lit
  maintenant la liste des flèches actives (`Arrow.Active`, tenue par les flèches elles-mêmes). Les gerbes de particules des duos,
  trios et ultimes (`TowerDuo.Burst`, des dizaines par seconde pour les ultimes) étaient un objet créé puis détruit à chaque
  fois : elles sont en réserve (48 au plus ; une gerbe finie sert à la suivante). Le reste des recherches lourdes ne sert qu'à la
  mise en place (fondre les modèles, au lancement). **À mesurer au casque** : une vague chargée avec les trois ultimes, la pluie de
  flèches (`Docs/Audit_Performance_Quest.md`, partie 8 : les mesures du Quest).

### 12.56 Les menaces par voie (le lot 5 de 12.7, 3 novembre)

Le joueur : « fais […] le lot 5 » (la piste C du 29 octobre, 12.2 et 12.3 : chaque voie a une tendance, annoncée pendant la
pause). Codé (`Waves/WaveManager.Lanes.cs`, `Waves/LaneThreatSigns.cs`, `EnemySpawner.Spawn(ennemi, point)`), compilé, **pas
encore vu dans Unity ni testé au casque**.
- **La règle** : à partir de la **vague 2** (la vague 1 reste comme avant : on apprend à jouer), chaque voie a sa **famille
  d'ennemis**, tirée au hasard parmi celles de la vague : **volants**, **costauds**, **essaim**, **tireurs** ; une voie de trop
  (à la vague 2, il n'y a que l'essaim et les tireurs) : **mélange**. Et une voie est **la plus chargée**. Rien au boss final.
- **Ce qui change dans la vague** : seulement par où sortent les ennemis. Avant, chacun sortait d'un point d'apparition tiré au
  hasard ; maintenant, **3 fois sur 4 par la voie de sa famille**, sinon (ou si sa famille n'a pas de voie) par une voie au
  hasard, **la plus chargée deux fois plus souvent**. Combien et lesquels ne changent pas (le budget, les poids : rien de
  l'équilibrage du joueur). Les boss des vagues sortent toujours au hasard ; leurs renforts suivent la règle.
- **La famille d'un ennemi**, sans réglage : volant (`FlyingEnemy` : le volant, la guêpe, la gargouille), tireur
  (`EnemyRangedAttack` : le tireur, la liche), sinon **costaud** s'il coûte au moins 1,8 dans le budget des vagues (le cactus, le
  fantassin, la tortue, l'orc, la mimique), **essaim** sinon (les gluants, les champignons, l'étoile de mer, le rampant).
- **L'annonce, pendant la pause qui précède** (les menaces sont tirées à la fin de la vague d'avant) :
  - un message, sous celui de la fin de vague : « Vague 3 : par où ils viendront · Gauche : volants · Milieu : costauds ·
    Droite : essaim (la plus chargée) » ;
  - **au-dessus de chaque voie**, à 20 m de la tour et 5 m de haut, un grand mot de la couleur de sa famille (« VOLANTS » bleu
    ciel, « COSTAUDS » rouge orangé, « ESSAIM » vert, « TIREURS » violet, « MÉLANGE » blanc), « la plus chargée » dessous, face
    au joueur, toute la pause (comme les flèches « DUO ») ;
  - puis la ligne dans le titre du début de la vague.
- On s'adapte : les bâtiments se déplacent gratuitement entre les vagues (le lance-filet sur la voie des volants…).
- **Ce que le joueur a à apprendre** (la règle de la complexité) : lire le mot de la voie. Ce que ça remplace : le point
  d'apparition tiré au hasard.

**Mis en sommeil le même jour** (le joueur, après l'essai : « les écritures pour chaque vague sont dans les murs et on ne les voit
pas, il faudrait changer ce système pour éviter de complexifier le jeu » ; puis, devant les idées de Claude, « retirons cette
mécanique pour le moment »). Les mots au-dessus des voies étaient posés à 20 m de la tour et 5 m au-dessus du chemin : là, les voies
passent entre les terrasses de 3 et 6 m et dans des virages. Fait : `Waves/LaneThreatSigns.cs` (les mots) est supprimé ; le reste
dort dans `Waves/WaveManager.Lanes.cs` (`s_LaneThreats` à faux) : rien n'est tiré ni annoncé, chaque ennemi sort de nouveau d'un
point d'apparition au hasard. `EnemySpawner.Spawn(ennemi, point)` reste (sans point : au hasard). Compilé.

**Les idées pour une autre annonce, si la mécanique revient** (Claude, le même jour) :
1. *(conseillée)* **les fantômes de la vague** : pendant la pause, sur chaque voie, au milieu de la terre, à 12–15 m de la tour, des
   silhouettes bleutées (la matière des hologrammes des plans) des ennemis qui vont venir ; trois sur la voie la plus chargée, une ou
   deux ailleurs : ni mot ni couleur à apprendre, et plus besoin de « la plus chargée » ;
2. **des panneaux sur la tour** : sur son rebord, face à chaque voie, un petit panneau de bois avec le mot, à un mètre du joueur ;
3. **sur la montre** : une ligne pendant la pause ;
4. **les torches de la voie colorées** : un code de couleurs à apprendre (déconseillé) ;
5. **ne rien annoncer** : on découvre pendant la vague par où ils viennent (les torches) ; mais on ne s'y prépare plus.

### 12.57 Le talisman de l'ingénieur surcharge tous les bâtiments (3 novembre)

Le joueur demande comment marchent le talisman de l'ingénieur et son duo, et quelles tourelles rejoignent MK. Ce que Claude a
relevé dans le code :
- **Le talisman** (objet, talisman, 3 niveaux) : une flèche tirée dont le trajet traverse un bâtiment construit le **surcharge**,
  une fois par flèche (« Surchargée ! », des éclairs bleus) : il tire 2 / 4 / 6 fois plus vite et sa portée double, pendant 5 / 7 /
  9 s ; une flèche qui traverse une barricade debout lui rend 10 / 15 / 20 % de ses PV max. **Son duo, « Réseau »** (avec l'Arc
  conducteur) : une flèche plantée se relie aussi, par un arc électrique, au bâtiment le plus proche à moins de 8 m, qui reste
  surchargé tant que le lien dure (6 à 9 s).
- **Le trou** : il ne surchargeait que les balistes (les lance-filets compris : dans le code, c'en est une) et les tours de
  givre, les tourelles d'avant la refonte ; les 7 autres tours, les duos, les trios et l'ultime l'ignoraient, alors que tous savent
  être surchargés (leur cadence et leur portée suivent `OverchargeRate` et `OverchargeRange`).
- **Les tourelles de l'arène de MK** (`FinalBoss/ArenaTurrets`) : les balistes, les lance-filets, les tours de givre et l'ultime,
  6 emplacements (l'ultime d'abord, puis les plus hauts niveaux), la portée doublée. La baliste : des dégâts seulement (son harpon
  ne cloue pas MK, son pieu titan ne l'emporte pas). La tour de givre : des boules de glace à tour de rôle, qui ralentissent MK
  sans le blesser (25 % au plus, son plafond) ; ses niveaux 3 à 5 ne jouent pas là. L'ultime : son effet sur MK (le Colosse le
  plaque au sol, le Manège l'étourdit, l'Horloge le fige). **Le lance-filet n'y fait rien** : il ne vise que les volants, MK n'en
  est pas un pour le jeu, et son filet électrique (niveau 4) ignore les boss. À décider par le joueur (proposé : il ne vient plus,
  sa place va à une autre tourelle ; ou un filet qui ralentit MK).

**Fait** (le joueur : « fais en sorte que le talisman puisse surcharger toutes les tourelles ») : le talisman et le Réseau
prennent **tous les bâtiments construits** (`ArrowPieces.Lot3`, `RefreshBuildings` : `Structure.All` au lieu des balistes et des
tours de givre cherchées dans la scène). Compilé ; **pas encore vu dans Unity ni testé au casque**. **Les textes ne sont pas
changés** : la carte du talisman est celle que le joueur a refaite le 27 octobre (`Cartes_A_Refaire.xlsx` : « Une flèche dans ta
baliste la fait tirer {2|4|6} fois plus vite pendant {5|7|9} s ; un tir dans la barricade la répare de {10 %|15 %|20 %} »), et le
texte du duo Réseau (« Les arcs électriques relient tes flèches à tes balistes, qu'ils surchargent ») ; proposés par Claude, à
changer par le joueur s'il le veut : « Une flèche dans une de tes tourelles la fait tirer {2|4|6} fois plus vite pendant {5|7|9} s ;
un tir dans la barricade la répare de {10 %|15 %|20 %} » ; « Les arcs électriques relient tes flèches à tes tourelles, qu'ils
surchargent ».

**Puis le joueur : « oui, remplace »**. La carte : « dans ta baliste » devient « dans une de tes tourelles », le reste de son texte
ne bouge pas (`Editor/EngineerTextBaker`, une fois, seulement si la carte dit encore « dans ta baliste » ; le texte de montée de
niveau, « Cadence {×2|×4|×6} pendant {5 s|7 s|9 s} ; réparation {10 %|15 %|20 %} », reste juste). Le duo Réseau, dans le code
(`Upgrades/ArrowDuos`) : « Les arcs électriques relient tes flèches à tes tourelles, qu'ils surchargent ». Compilé.

### 12.58 Tous les bâtiments qui tirent rejoignent l'arène de MK (3 novembre)

Le joueur : « rajoute toutes les tourelles qui tirent dans la liste des tourelles qui peuvent aller dans l'arène, et adapte-les pour
qu'elles fonctionnent correctement ». Jusqu'ici : les balistes (les lance-filets compris), les tours de givre et l'ultime.

**Ceux qui viennent** (`Structure.FightsInArena`) : la baliste, le lance-filet, la tour de givre et le **mortier** ; les duos
**Avalanche**, **Feu d'artifice**, **Carquois géant**, **Loupe** et **Lanceur de baudruches** ; les trios **Enclume**, **Gonfleur** et
**Grande bombarde** ; l'ultime. **Ceux qui ne viennent pas** (ils ne tirent pas : ils agissent sur la voie, autour d'eux, ou font tomber
quelque chose du ciel) : la barricade, les piques, le tremplin, la tour-aimant, la bulle, le phare ; les duos Boulet fou,
Marteau-pilon, Toboggan, Yo-yo, Chant des sirènes, Chaîne de forçats, Boussole folle, Boule à neige ; les trios Bowling, Beffroi,
Colisée, Montagnes russes. Toujours 6 emplacements : **les plus forts d'abord**, l'ultime, puis les trios, les duos, les tours, et à
égalité le plus haut niveau (`ArenaTurrets.Rank`).

**Ce qui est commun** :
- l'entrée et la sortie de l'arène sont celles de tous les bâtiments (`Structure.EnterArena`, `LeaveArena`, `InArena` : avant, les
  ultimes seules) ; **les duos et les trios y sont réduits** pour tenir sur leur emplacement, sous la voûte (trois cases de large,
  12 m de haut au plus, `TowerDuo.ArenaScale`, d'après leur taille ; les ultimes, 0,65 comme avant) ;
- leur portée est doublée, comme celle des balistes (`RangeScale` : les duos et les trios ne s'en servaient pas) ;
- **ce qu'ils lancent suit MK** : sur la carte, un obus, une boule, une enclume, une bombe tombe au sol, là où sera l'ennemi ; MK vole,
  et tout tombait au sol sous lui. Dans l'arène, la fin de la cloche suit son corps à chaque image (`Structure.Arc`) : ça le
  rattrape en vol comme au sol ;
- **un coup « en pourcentage » ne lui coûte que 2 % de ses PV max au plus** (`Structure.ShareFor`, `k_ArenaBossShare`) : le char de
  carnaval (35 %), le rayon de la Loupe (15 % à chaque passage), la grande enclume (8 %) l'auraient tué à eux seuls (le Colosse lui
  en prend 3 % quand il le plaque au sol). Leurs dégâts ordinaires, eux, sont les mêmes.

**Chacun** :
- **le lance-filet** : MK se prend au filet à tous les niveaux (avant, il ne visait que les volants) ; il ne tombe pas (le Colosse
  seul le plaque au sol) : un grand filet l'enveloppe (« Pris au filet ! ») et le ralentit pendant le temps d'un volant au sol (3 / 4
  / 5 s ; 25 % au plus, son plafond) ; le filet électrique le blesse aussi ; la toile géante (niveau 5) le ralentit tant qu'il est
  dessous. C'est la proposition « un filet qui ralentit MK » ;
- **le mortier** : ses obus le rattrapent et éclatent sur lui (l'explosion compte depuis son corps, moins sa carrure de 2,5 m,
  `Structure.BlastDistance`) ; le bombardement (niveau 5) pleut le long de son corps ; ni flaque de goudron ni sol en feu dans l'arène
  (les obus y éclatent souvent en l'air) ;
- **l'Avalanche** : pas de voie où rouler, et un boss n'est jamais avalé : le mortier de glace tire sa boule (plus grosse) droit
  sur MK ; elle éclate sur lui en neige (« Boule de neige ! ») : les dégâts de l'éclatement, et il est ralenti 3 s ; au niveau 3,
  l'avalanche, plus grosse, et 2 % de ses PV max ;
- **le Feu d'artifice** : ses fusées le suivent et éclatent sur lui (et non 7 m au-dessus du sol) ; ébloui, il est ralenti, et la
  pluie d'étincelles le brûle ; le bouquet final : vingt fusées tout autour de lui ;
- **le Carquois géant** : il rattrape toutes tes flèches ratées dans l'arène ; ses volées partent de l'arbalète droit sur MK, là où il
  sera (tombées du ciel, la voûte les arrêtait, et elles tombaient sous lui quand il vole) ;
- **la Loupe** : la tête de MK n'y grossit que ×1,5 (×3, elle traverserait la voûte ; `m_ArenaBossHead`), son point faible ×2 comme
  les autres boss ; sa baliste vise sa tête ; le rayon le brûle (2 % de ses PV max à chaque passage, au lieu de 15 %) ;
- **le Lanceur de baudruches** : pas de voie où les poser, et un boss ne vient pas les frapper : le canon les lance sur MK, où elles
  éclatent en confettis piquants ; au niveau 3, le char de carnaval aussi, toutes les 20 s (2 % de ses PV max) ;
- **l'Enclume** : ses enclumes le rattrapent, en vol comme au sol, puis retombent au sol (elles restaient posées en l'air) ; il n'est
  jamais aplati ; le rebond (niveau 2) va vers un autre ennemi que celui qu'elle vient de frapper (MK est seul : elle retombe) ; la
  pluie d'enclumes (niveau 3) part sur lui seul (il en fallait trois à portée), de moins haut (la voûte), et la grande enclume lui
  coûte 2 % de ses PV max ;
- **le Gonfleur** : le harpon plante quand même le tuyau dans MK ; il ne gonfle pas (un boss ne change jamais de taille) ; la pompe le
  ralentit pendant qu'elle souffle, puis le tuyau éclate (« Pan ! ») : deux fois les dégâts d'un ballon crevé. Le défilé (niveau 3) ne
  joue pas dans l'arène (il faut quatre ennemis) ;
- **la Grande bombarde** : comme sur la carte, ta flèche dans sa gueule la remplit et ta flèche dans la mèche l'allume (elle ne se
  remplit plus aux ennemis tués : il n'y a que MK) ; ses bombes suivent MK et éclatent sur lui ; le bouquet final aussi.

Compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement ; aucun champ en double) ; **pas encore vu dans Unity ni testé
au casque** (tests : `Checklist_Demo.md`, « Le 3 novembre »). Les réglages nouveaux (le ralentissement du filet, de la neige et de la
pompe, la tête de MK sous la Loupe) sont des propositions, sur les prefabs dès qu'Unity les enregistre.

### 12.59 Le cor et le rayon : la tour attaquée (3 novembre)

Le joueur : « j'aime pas trop l'alerte sur la montre, mais il faut bien que le joueur soit averti des dégâts de la tour, des
propositions ? ». Claude en a proposé cinq (le cor de la tour, le rayon plus fort et la fumée, la flèche au bord de la vue, une
vibration, un message devant les yeux) ; **le joueur a choisi le cor et le rayon**. Ils **remplacent l'alerte de la montre et l'état
des trois voies** du lot 6 (piste B : « La montre prévient ») : rien sur la montre ni à l'écran, tout est dans le monde.

Ce qui existait : l'anneau, la lumière et le rayon de la tour, vert, orange puis rouge selon ses PV, qui clignotent quand elle est
touchée (`World/Lighting/MapGameplayLights`) ; on ne le voyait qu'en regardant la tour.

**Une attaque** : des coups reçus à moins de 8 s d'écart (*Tower Attack Gap*, dans `Resources/MapLighting`, les réglages des
lumières de la carte).
- **Le cor** : au premier coup d'une attaque, un cor de guerre sonne depuis le sommet de la tour, en son 3D : où qu'on soit, on
  l'entend venir de sa direction (une source à lui, qui porte jusqu'à 260 m en baissant doucement : celles des autres sons s'éteignent à
  60 m). Une fois au-dessus de la moitié des PV, deux fois en dessous, trois sous le quart ; et de nouveau pendant l'attaque quand
  les PV passent ces seuils. Pas si le joueur est sur la tour ou à moins de 15 m (il voit l'attaque), ni pendant le silence de Minuit.
  Le son : synthétisé par Claude (`Tools/sons/cor_de_la_tour.py`, `Audio/Tour_Cor.wav`, 2,15 s : un do grave, la lèvre qui attaque un
  peu bas, le son qui s'éclaircit en enflant, l'octave dessous, l'écho d'une vallée) ; posé par `Editor/TowerHornBaker` (une fois,
  si le réglage est vide). À remplacer par un son de freesound si le joueur en trouve un meilleur.
- **Le rayon** : pendant l'attaque (et 8 s après le dernier coup), il est deux fois et demie plus long et plus large (100 m au lieu
  de 40, *Alarm Beam Scale*) et bat en rouge, bien visible même le jour ; il redescend doucement à la fin. Toujours caché quand le
  joueur est sur la tour.
- **La fumée** : sous la moitié de ses PV, une colonne de fumée noire monte de la tour, d'autant plus épaisse qu'elle est abîmée (60
  particules au plus) : on la voit de toute la carte, jusqu'à ce que la tour remonte au-dessus de la moitié (les 15 % de fin de vague).

Compilé (le jeu et les outils d'éditeur) ; **pas encore vu dans Unity ni testé au casque**. Le reste du lot 6 (l'état des voies,
l'équilibrage des gardiens) n'est pas fait.

### 12.60 Le troisième essai : volants, boss, PV du joueur, le combo, le Manège ; le rythme à revoir (3 novembre, tard)

Le joueur a fait une partie entière (Normal, au casque ; le journal disait à tort « sans casque », corrigé le 4 novembre, 12.64), lue par Claude dans le premier **journal
de partie** (`Logs/Parties/`, `Scripts/Journal/`) : victoire à la vague 8 en 21 min, MK tué en 54 s, la tour attaquée 3 fois
(77 PV perdus sur 3 000 à 4 875), le joueur jamais sous 25 % de ses PV ; le chantier du début (Mortier, Bulle, Baliste, Barricade)
fusionné en Gonfleur à la vague 3, le Grand Manège à la vague 7 (63 % des dégâts à la vague 8) ; Force ×14 à la fin (presque tout
l'or en parchemins). Son ressenti : la partie était simple, mais il connaît le jeu par cœur et avait une bonne combinaison ; MK tué
vite, « juste de la chance » (le ralenti tout le temps).

**Fait** (le joueur : « fais tous ces changements ») ; les réglages par `Editor/ThirdTestBaker` (une fois, seulement s'ils ont
encore leur valeur d'avant), le reste dans le code :
- **les volants** (« vraiment casse-pieds » : avec un jeu de rebonds et de réactions en chaîne, ils étaient presque invincibles, et
  les tours anti-aériennes n'arrivent parfois qu'à la fin) : **PV −50 %** (Volant 20 → 10, Guêpe 12 → 6, Gargouille 35 → 18) ;
- **leur piqué** (« parfois ils ne nous attaquent pas : ils évitent trop de foncer dans le sol ») : dans ses 4 derniers mètres, un
  volant ne s'écarte plus de rien (`FlyingEnemy`, `k_FinalApproach`), et il touche un peu plus loin (+0,3 m : Volant 1 → 1,3,
  Guêpe 0,8 → 1,1, Gargouille 1,2 → 1,5) ;
- **les boss des vagues** (« trop de PV : le seul moyen de les tuer était de leur foncer dessus et de tirer à bout portant ; ils
  devraient pouvoir nous taper ») : ils **frappent le joueur à moins de 7 m** (*Player Aggro Range* 0 → 7, au même niveau que lui ;
  ils ne frappaient que la tour et les barricades), et **PV −30 %** (Thomas 272 → 190, Jean 383 → 268, Gontrand 592 → 414, Francine
  787 → 551, Nolan 868 → 608) ; MK ne change pas ;
- **les PV du joueur ÷3** (« on ne ressent pas assez les coups ») **et une grande régénération** (« on aurait une solution pour éviter
  de mourir ») : 200 → 67 PV ; les PV remontent après 3 s sans être touché (5 avant), de 10 % des PV max par seconde (4 avant) : de
  presque rien au plein en 10 s (la scène : enregistrer, Ctrl+S). La **Vigueur** suit : +8 PV au lieu de +25 (la même part des PV de
  base, que la tour et les barricades suivent aussi ; le nombre de sa carte avec, le reste de son texte ne change pas). Le Vampirisme
  (+3 / 6 / 9 PV par tir à la tête) ne change pas : il pèse trois fois plus ;
- **le combo « Le style, ça compte aussi » + corde triple** (le duo « 360 no scope headshot » : « tir parfait, ×9 et le temps
  ralenti : en une seconde, 30 tirs parfaits ; on passe à 5 ») : la couronne tire **deux fois** les flèches de la corde triple au lieu
  de trois : 5 / 9 / 13 flèches avec celle de l'arc (7 / 13 / 19 avant ; `SpecialArrows.k_CrownMultiplier`) ;
- **le Grand Manège** (« même avec un bon build, mes dégâts étaient faibles par rapport aux carrousels » : il faisait deux fois les
  dégâts du joueur, 280 par seconde contre 150) : **tous ses dégâts −35 %** (l'éveil 100 → 65, le ruban 60 → 39, le tournis 70 → 46,
  l'éjection 100 → 65 et 200 → 130, le cheval 160 → 104, la charge 90 → 59 ; ses parts sur les boss ne changent pas). À relancer au
  tableau (`Tools/equilibrage_tours.py`) après Unity ;
- **le trio** (« quand j'ai construit le trio, j'ai eu l'impression de perdre de la puissance ») : rien pour l'instant. Au journal,
  le Gonfleur faisait deux fois les dégâts des trois tours d'avant (39 par seconde contre 19), mais la Force du joueur est passée de 4
  à 10 au même moment, et il n'y avait plus qu'un emplacement défendu au lieu de trois ; à revoir après un essai au casque ;
- et deux corrections : une flèche tenue en main qui frappe ne change plus la vitesse d'un corps figé (`Arrow.StickInto`, 10
  avertissements au journal) ; le journal compte mieux (les flèches qui touchent ne dépassent plus 100 %, une ultime qui sort de terre
  n'est plus « déplacée », l'or dépensé à la pause va au bilan de la vague suivante, une partie arrêtée avant sa première vague tient
  en une ligne).

Compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement ; aucun champ en double) ; **pas encore vu dans Unity ni testé
au casque**.

**Le rythme, à revoir** (le joueur : « un grand problème de rythme ») :
- on prend les coffres au début de la vague, puis on fonce frapper le gong : rien ne pousse à ne pas appeler le boss plus tôt (son
  bonus d'or grandit avec le temps qui reste) ;
- le chrono s'allonge de vague en vague (+12 s), mais aller chercher les coffres ne prend que quelques secondes de plus : les
  premières vagues sont bien rythmées, puis on s'ennuie de plus en plus et on frappe le gong vite.

Propositions de Claude pour le brainstorm (chacune dit ce qu'elle remplace) :
1. **Les coffres à la pause** : ils apparaissent quand le boss meurt, et la pause (qui dure tant que le joueur ne frappe pas le gong)
   sert à les ouvrir, construire et acheter ; la vague n'est plus que du combat. Remplace les coffres au gong et l'accalmie.
2. **Une vague courte et fixe** : la même durée à chaque vague (environ 75 s), la difficulté passe par le nombre et la force des
   ennemis. Remplace l'allongement de +12 s par vague.
3. **Le boss quand la jauge est pleine** : chaque ennemi tué la remplit ; un joueur fort va plus vite, sans attendre. Remplace le
   chrono, l'appel du boss au gong et son bonus.
4. **Le gong sans bonus** : appeler le boss plus tôt fait gagner du temps, plus de l'or (le bonus le rendait toujours gagnant).
5. **Des raisons de rester dehors** (la piste B, 12.2 : un totem au bout de chaque voie, protégé tant que le joueur est près) :
   remplit le temps, mais c'est un système de plus (à éviter, la règle de la complexité).
Conseil de Claude : 1 et 2 (le moins de règles : la pause pour préparer, la vague pour se battre) ; 3 si le joueur veut que le rythme
suive sa force. À décider par le joueur.

**Le joueur, ensuite** : la 2 (la vague courte et fixe) est bien. La 1 (les coffres à la pause) non : les coffres pendant la vague
sont là pour varier la boucle (« pas juste faire la tourelle humaine pendant toute une vague »), mais tels qu'ils sont, c'est une
randonnée : pas d'ennemis, pas de risque. Sa proposition : **des ennemis qui gardent les coffres**, qui vadrouillent dans la forêt et
l'attaquent s'il s'approche ; il faut les tuer à l'arc pour prendre les coffres. Les pistes de Claude autour :
- **les gardiens** (l'idée du joueur, précisée) : 2 à 4 par coffre, pris parmi les ennemis de la vague (ils grandissent avec elle),
  qui restent autour de leur coffre et ne vont pas à la tour ; à 15 m ils attaquent, au-delà de 30 m ils y retournent ; le coffre
  est enchaîné tant qu'un gardien vit. Une seule règle de plus (« un coffre s'ouvre quand ses gardiens sont morts »), qui remplace
  les coffres gratuits. À trancher : visibles de la tour (on les abat de loin, puis on marche) ou **réveillés à l'approche**
  (conseillé : l'action se passe au coffre) ;
- **la mimique** : l'un des coffres est une Mimique (l'ennemi existe) qui se jette sur le joueur à l'ouverture ; une surprise ;
- **les coffres sur le chemin des ennemis** (au bord des voies, dans le flot de la vague) : aucun comportement nouveau, mais on
  reste près des voies ;
- **le coffre à rapporter** à deux mains jusqu'à la tour, poursuivi : beaucoup de tension, mais lourd, et sans arc.
Avec les gardiens, la vague fixe se remplit d'elle-même (les coffres prennent du temps et du risque qui grandissent avec la
vague) ; le gong garde son bonus, devenu un vrai choix (appeler le boss, c'est renoncer aux coffres pas encore pris) ; pendant
l'expédition, ce sont les tours qui tiennent, et le cor rappelle le joueur. À décider par le joueur.

### 12.61 Les gardiens des coffres, la vague de durée fixe (4 novembre)

Le joueur : « code les gardiens ; cependant le coffre n'est pas verrouillé, cela fait une règle de plus : le joueur peut courir vers
les coffres s'il le souhaite, mais cela sera au détriment de ses PV ». Et la vague courte et fixe (12.60, la 2) : « bien ».

**Les gardiens** (`Enemies/Enemy.Guard.cs`, `Building/CrystalSpawner`) :
- chaque coffre de la vague a **ses gardiens** : 2 aux vagues 1 et 2, 3 aux vagues 3 et 4, 4 ensuite (*Guards Per Wave*), tirés au
  sort parmi les ennemis de la vague, comme ceux des voies (au sol seulement : pas de volant, pas de boss) ; ils ont la force de la
  vague ;
- d'abord (le matin du 4 novembre), ils sortaient des fourrés quand le joueur approchait à 20 m du coffre ; **le joueur, ensuite :
  « ils sont là dès le début et vadrouillent un peu au hasard en prenant des pauses de temps en temps »**. Ils apparaissent donc avec
  le coffre, au hasard autour de lui (de 2 à 5 m) ;
- un gardien **vadrouille au pas** autour de son coffre (à 5 m au plus), d'un point au hasard à l'autre ; arrivé, il s'arrête 6 fois
  sur 10, de 1,5 à 4,5 s (`Enemy.Guard.cs`, `k_GuardPauseChance`, `k_GuardPause`) ; il attaque le joueur à 15 m de lui (ou s'il le
  blesse), le poursuit tant que le joueur est à moins de 30 m du coffre, puis y retourne (*Guard Radius*, *Guard Aggro*, *Guard
  Leash*) ; il ne va jamais à la tour ;
- **le coffre n'est pas verrouillé** : on peut foncer dessus, au prix de ses PV (67 depuis le troisième essai) ;
- ils ne comptent pas dans le maximum d'ennemis en vie de la vague (sinon une expédition aux coffres arrêtait les voies), ni dans
  celui des renforts des boss ; à la fin de la vague, ils s'enfuient avec les autres ;
- le journal de partie note chaque coffre gardé qui apparaît, chaque coffre ouvert avec les gardiens encore en vie (pour voir si l'on fonce ou si
  l'on se bat), et au bilan de vague « Coffres : N ouverts ; gardiens : N sortis, N tués ».

**La vague de durée fixe** (`Editor/ThirdTestBaker`) : toutes les vagues ont le chrono de la première (*Duration Increase* 12 → 0 s :
en Normal, 32 s d'accalmie et 29 s de montée à chaque vague) ; la difficulté passe par le nombre et la force des ennemis. Avec les
gardiens, l'expédition aux coffres prend d'elle-même plus de temps et de risque au fil des vagues ; le gong garde son bonus, devenu
un vrai choix (appeler le boss, c'est renoncer aux coffres pas encore pris).

Compilé (le jeu et les outils d'éditeur, sans erreur ni avertissement ; aucun champ en double) ; **pas encore vu dans Unity ni testé
au casque**. À vérifier d'abord : que le NavMesh couvre les abords des coffres (sans NavMesh à moins de 3 à 4 m, un gardien
n'apparaît pas : le journal n'écrit alors jamais « Un coffre apparaît, gardé par N ennemis »).

### 12.62 Les ultimes −30 %, la tour à 1 500 PV, le contrôle plutôt que les dégâts (4 novembre)

Après l'avis général de Claude sur le jeu (tirés du journal de la partie du 3 novembre : la tour avait perdu 77 PV sur 4 875 ; les
bâtiments faisaient l'essentiel des dégâts aux vagues 1 à 3, puis un seul faisait le travail). Le joueur retient deux points :

- **« La tour devrait avoir 2 fois moins de PV »** : 3 000 → **1 500 PV de base** (la scène, `Editor/UltimesTowerBaker`). La Vigueur
  s'y ajoute toujours en part, et la réparation de fin de vague (15 % des PV max) suit. Le but : qu'une expédition aux coffres laisse
  une trace sur la tour, que le cor serve, que « je reste ou je sors » soit un vrai choix.
- **« Les tourelles qui contrôlent les adversaires sont plus fun que les tourelles de dégâts »** : c'est la direction pour choisir et
  régler les bâtiments (ceux qui projettent, immobilisent, regroupent, ralentissent : ils préparent les tirs du joueur). Rien n'est
  retiré ni ajouté pour autant. Mais **les tourelles ne font pas trop de dégâts, sauf les ultimes** (le joueur, contre l'avis de
  Claude, qui proposait d'en couper) : **« nerf les dégâts de toutes les ultimes de 30 %, pas que le carrousel »**.

**Les ultimes** (`Editor/UltimesTowerBaker`, les prefabs) : tous les dégâts −30 % (arrondis) ; le Grand Manège garde son −35 % du
troisième essai (12.60) ; les parts des PV des boss ne changent pas (comme pour le Manège ; MK ne change pas).
- **Le Colosse de forge** : l'éveil 100 → 70, les éclats 70 → 49, le lingot 450 → 315, le sol brûlant 15 → 11 par seconde, les marteaux
  160 → 112, la coulée 90 → 63 et sa brûlure 50 → 35, la rivière de fonte 30 → 21 ;
- **La Grande Horloge** : l'éveil 100 → 70, les chiffres 220 → 154, le dégel qui brise 500 → 350, l'écho du temps 160 → 112.

Le tableau d'équilibrage (`Tools/equilibrage_tours.py`) est à relancer après Unity. Compilé ; **pas encore vu dans Unity ni testé au
casque** (enregistrer la scène après le baker, Ctrl+S).

### 12.63 Les plans par coffre : de 1 à 4, au hasard (4 novembre)

Le joueur : « 2 plans par coffre n'est pas la meilleure solution : il faudrait entre 1 et 4 plans par coffre, aléatoirement ; les
chances d'en avoir plus augmentent pendant la partie ; au total de la partie (manche 6), en moyenne, on doit en avoir légèrement plus
qu'actuellement (10 % de plus) ». Ce n'est pas une règle de plus : le nombre fixe (2, 12.42) devient un tirage.

L'orbe de tourelle d'un coffre donne **de 1 à 4 plans** de son bâtiment, tirés au sort à l'ouverture du coffre (l'orbe dit
combien avant qu'on la prenne : « 3 plans » ; le choix entre les trois orbes en devient plus intéressant). Les chances, vague par
vague (`BuildManager`, *Plan Chances* : une ligne par vague, la dernière vaut pour les suivantes ; des poids) :

| Vague | 1 plan | 2 plans | 3 plans | 4 plans | En moyenne |
|---|---|---|---|---|---|
| 1 | 40 % | 45 % | 10 % | 5 % | 1,8 |
| 2 | 30 % | 45 % | 20 % | 5 % | 2,0 |
| 3 | 25 % | 40 % | 25 % | 10 % | 2,2 |
| 4 | 20 % | 35 % | 30 % | 15 % | 2,4 |
| 5 et après | 15 % | 30 % | 35 % | 20 % | 2,6 |

**Sur la partie** : 4 coffres par vague, aux vagues 1 à 5 (aucun à la vague 6, celle de MK), soit 20 coffres ; **2,2 plans en moyenne
par orbe, 44 plans si l'on prend toutes les orbes de tourelle, au lieu de 40 : +10 %**. Le début est un peu plus pauvre (1,8 à la
vague 1), la fin plus riche (2,6 à la vague 5), au moment des duos, des trios et de l'ultime.

Le journal de partie écrit chaque lot pris (« Plans : +3 Baliste »). Le réglage n'est pas encore dans la scène : les valeurs du code
s'y posent à l'ouverture (l'ancien *Plans Per Chest*, 2, n'est plus lu). Compilé ; **pas encore vu dans Unity ni testé au casque**.

### 12.64 Le quatrième essai : le Carquois géant limité, le journal corrigé (4 novembre)

Le joueur a fait une partie entière **au casque** (Difficile, l'Arc légendaire ; celle du 3 novembre aussi était au casque : le
journal disait à tort « sans casque », voir plus bas), lue dans le journal : victoire en 15 min 17, aucune mort ; vagues de 1:09 à
1:32 ; les 20 coffres ouverts, 56 gardiens, 54 tués (le joueur tue les gardiens d'un coffre avant de l'ouvrir) ; jamais de gong ;
30 plans pour 14 orbes de tourelle (2,1 en moyenne), tous dépensés ; la tour attaquée une fois (vague 1, avant tout bâtiment) ; les
bâtiments à 65 % des dégâts hors MK (le Mortier au niveau 5, puis l'Avalanche) ; les boss tués en 10 à 23 s ; **MK tué en 24 s,
88 % par le Carquois géant** (de 23 158 PV à 0 en 2 s : une seule nuée).

Son ressenti : « les changements sur les coffres sont bons, on ne s'ennuie plus pareil niveau tempo » ; la partie était beaucoup trop
simple, mais avec un build monstrueux (multishot explosif à détonation, l'Arc légendaire), « multiplié au Carquois qui ramassait
l'entièreté des flèches loupées » : un cas extrême de très bonne synergie, des tourelles comme de l'arc. **« Le carquois devrait être
limité dans ce qu'il peut prendre à la seconde. »**

**Le Carquois géant** (`Building/DuoCarquoisGeant`, *Catches Per Second*) : **2 flèches rattrapées par seconde au plus** (une réserve
de deux, qui se remplit à ce rythme ; au ralenti, avec le temps du jeu). Un tir à la fois n'est pas gêné ; un multishot dont les
flèches explosent au sol (elles comptent comme ratées) ne remplit plus la nuée en quelques tirs : il faut au moins 15 s pour ses 30
flèches. Pourquoi une nuée tuait MK : dans l'arène, ses 90 carreaux partent tous droit sur lui, et chacun compte la zone touchée (la
tête, les points faibles) comme une flèche ; le plafond de 2 % de l'arène (12.58) ne vaut que pour les coups en pourcentage. À
revoir au prochain essai : si une nuée reste trop forte sur MK, la limiter dans l'arène.

**Le journal de partie** (`Scripts/Journal/`) :
- **au casque ou au clavier** est noté au début de chaque vague (« Vague 1 : elle commence (… ; au casque) »), d'après l'affichage du
  casque qui tourne et le jeu au clavier (`DesktopControls`) ; avant, l'en-tête le lisait au lancement du mode Jeu, avant que le
  casque ne démarre, et écrivait « sans casque » (les parties du 3 et du 4 novembre, jouées au casque) ;
- **les gardiens ne comptent plus parmi les ennemis tués** (ils ont leur ligne : « gardiens : N, N tués ») : il y avait plus de tués
  que d'apparus (268 pour 261) ;
- **les PV du joueur au plus bas**, à chaque bilan (« au plus bas : 31/99 PV ») : à 67 PV, est-on vraiment en danger ?

Compilé ; **pas encore vu dans Unity ni testé au casque**.

### 12.65 L'Avalanche n'avale qu'une fois ; la zone contre la monocible (4 novembre)

**L'Avalanche** (le duo Mortier + Tour de givre, au niveau 3 à la vague 5 du quatrième essai : 67 % des dégâts) : le joueur, « elle
me semblait surpuissante, aucune des trois voies ne pouvait avancer ; il faudrait soit augmenter son temps de recharge, soit limiter
le fait qu'elle puisse faire rouler les ennemis à une fois ». Au niveau 3, toutes les 11 s (5,5 s avec le Talisman de l'ingénieur,
porté à cette vague), sa boule dévale toute la voie jusqu'à la forêt et ramène tous ceux qu'elle avale : les mêmes ennemis
repartaient sans cesse du début. Choisi par Claude : **un ennemi n'est avalé qu'une fois dans sa vie**, par toutes les Avalanches
(`Building/DuoAvalanche`, *Swallow Once*) ; la fois suivante, la boule le renverse seulement, comme un gros ou un boss. Le spectacle
reste entier sur les ennemis frais (le contrôle, ce que le joueur préfère : 12.62), et les voies avancent ; le temps de recharge ne
change pas.

**La zone contre la monocible** : le joueur sentait les builds de dégâts de zone bien plus forts que les monocibles, sans en être sûr.
L'avis de Claude (le catalogue, le code, les deux journaux) : oui, pour quatre raisons : l'explosion du Tir explosif touche aussi
l'ennemi frappé (sur une cible seule, +75 à +125 %, autant que le Chasseur de têtes, sans viser la tête) ; les cibles seules sont rares
(5 boss et MK, tués en 10 à 25 s) ; la zone pardonne la visée (une explosion au sol près d'un groupe) ; et ce qui multiplie (Tir triple,
Multitir, Déluge, la Force) multiplie aussi les explosions. Et les volants, dispersés, étaient le contre naturel de la zone (leurs PV
divisés par deux le 3 novembre). **La décision du joueur : ne pas affaiblir la zone** (« nerf le multicible est une mauvaise idée ; à
la limite, il faudrait renforcer le monocible et renforcer les ennemis ») ; il fera d'abord une partie avec des pièces monocibles
(Pointe du chasseur, Transe, Surcharge, Bourreau…) pour comparer au journal. Rien n'est changé de ce côté.

Compilé ; **pas encore vu dans Unity ni testé au casque**.

### 12.66 Les ennemis au sol, 30 % plus rapides (4 novembre)

Le joueur : « de manière générale, les ennemis au sol ne sont pas une vraie menace, puisqu'ils se font détruire par tout ; il ne
faudrait pas augmenter leurs PV, ils deviendraient juste des sacs à PV ; à la place, on peut augmenter leur vitesse : +30 % pour
toutes les difficultés, à part en Facile ». (Le journal va dans ce sens : au quatrième essai, 81 des 275 PV perdus par le joueur
venaient d'ennemis au sol, surtout des gardiens, le reste des volants ; et la tour n'a plus été touchée une fois le premier bâtiment
posé.)

Un réglage de plus par difficulté, **Ground Speed** (`Difficulty/DifficultyDefinition`, posé par `Editor/GroundSpeedBaker`, une fois) :
**×1,3 en Normal, Difficile et Impossible, ×1 en Facile**, en plus d'*Enemy Speed* et de la hausse par vague. Il vaut pour **les ennemis
qui marchent** : ceux des voies, les gardiens des coffres et les boss des vagues ; pas les volants, ni MK, ni les mannequins (ceux qui ont
un comportement à part, `Enemy.Awake`). Leurs jambes suivent (l'animation de course aussi vite). Ce qui change : ils traversent la
portée des bâtiments 30 % plus vite (moins de coups chacun), arrivent plus tôt à la tour, et les gardiens rattrapent le joueur.

Compilé ; **pas encore vu dans Unity ni testé au casque**.

### 12.67 Une partie en Impossible : la corde des Akimbow, le voile rouge, 100 PV (4 novembre)

Le joueur, après une partie en Impossible : « le mode Impossible est bien difficile, mais c'est ce qui était convenu » (rien à changer).

- **Une corde apparaissait à côté du joueur** avec les Akimbow et le Multitir (ou toute pièce de corde) : la lueur de la corde
  (`PieceLook`, la couleur de la pièce de la corde) se rallumait sur l'arc caché derrière les Akimbow, à sa place près du joueur.
  Corrigé (`Bows/Bow`, `RefreshStringGlow`) : la lueur ne s'allume que sur l'arc visible (ni remplacé par les Akimbow ou le katana,
  ni rangé pour une construction), et revient quand il réapparaît.
- **« On ne voit pas assez le rouge sur le côté de l'écran quand on est sur le point de mourir »** (`Player/HurtVignette`) : sous
  30 % des PV (25 % avant), le danger, le voile est presque à son plus fort (90 % de son opacité au moins) et bat nettement (de 55 à
  100 % ; 80 à 100 % avant) ; son opacité la plus forte passe de 0,75 à 0,95 ; il suit les PV qui baissent en 0,12 s (0,8 s avant :
  un gros coup n'était montré qu'après coup), ceux qui remontent toujours lentement ; il ne s'approche pas plus du centre (on vise).
- **Les PV du joueur à 100** (67 depuis le troisième essai, 200 avant ; la régénération ne change pas : après 3 s, 10 % par seconde) :
  `Editor/PlayerHealth100Baker`, la scène, à enregistrer (Ctrl+S). **La Vigueur passe à +12 PV** (le choix du joueur, « passer à
  +12 » : la même part des PV de base qu'avant, 12 %, que la tour prend aussi ; le même baker, une fois, le nombre de sa carte avec).
- **Pour plus tard** (le joueur) : un menu de toutes les constructions, duos, trios et ultimes, et comment les obtenir ; ceux qu'on
  n'a pas encore obtenus restent grisés, sans dire comment les obtenir (GDD, section 21, « À faire ensuite »).

Compilé ; **pas encore vu dans Unity ni testé au casque**.
