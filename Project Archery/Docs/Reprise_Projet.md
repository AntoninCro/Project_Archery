# Reprise du projet (26 octobre)

Ce document fait le point pour reprendre le projet ailleurs (autre compte Claude, autre PC, coéquipier). Il complète le GDD
(`GDD.md`, la référence, à tenir à jour) et les règles de travail (`../../CLAUDE.md`).

## 1. Où en est le jeu

THUNK VR est complet et jouable de la première vague au dragon : tir à l'arc physique avec anneau de timing, 6 vagues (accalmie,
montée, boss), tower defense (cristaux, 12 socles, 4 bâtiments à 3 niveaux, marteau), boutique roguelike (en refonte depuis le 25 octobre : parchemins et pièces de la flèche, voir plus bas),
coffres, 4 difficultés avec leur ciel, menu des armes et succès, deux secrets (portail des étoiles, toit cyberpunk), le dragon MK
dans son antre, mode infini, classement, tutoriel en vidéos, menu en cours de partie avec pause, jeu au clavier sans casque.

**Testé au casque** : le cœur du jeu (les guides montés et testés jusqu'au 13 octobre, voir `Feuille_de_route.md`), puis au fil
des retours du joueur une partie des nouveautés (la grenade, les Akimbow, le portail, le menu en cours de partie).
**Pas encore vérifié** : les cases non cochées de `Checklist_Demo.md` (la passe de tests complète n'a pas été faite), à commencer
par la liste « Nouveautés du 19 au 23 octobre », en tête de la partie 1.

**Commité le 25 octobre** (`f986573`) : la 3e version du katana (partie 2), le toit cyberpunk refait (décor 4, épreuve rendue
faisable), la présentation et le rapport. **Commité le 26 octobre** (`5232b71`) : le lore, acte 1, première partie (GDD,
section 24 ; scripts dans `Scripts/Lore`), l'audit de performance pour le Quest en autonome, avec le niveau de qualité
« Quest » (`Docs/Audit_Performance_Quest.md`, `Editor/QuestQualityBaker.cs`), le ménage des fichiers inutilisés
(`Docs/Fichiers_Inutilises.md`, `Tools/unused_assets.py` ; 3 726 fichiers supprimés, 740 Mo), la mesure de performance des
builds de développement (`Core/PerfProbe.cs`), et la **refonte de la partie roguelike**, étapes 1 à 5 (partie suivante).
**Commité le soir du 26 octobre** (`d523f2b`, poussé avec `7a69a08`) : la troisième version de l'atelier (flèche debout,
sceaux animés), les PV des parchemins retirés, la télékinésie guidée par la main. **Commité par le joueur du 29 au
31 octobre** (`1366cff`, `282d5b8`, `7a7efd0`) : la refonte roguelike finie, la refonte du tower defense (les 10 tours de
base, le tourniquet), la vitrine F4 et les 10 premiers duos. **Commité et poussé le 31 octobre au soir**, avant le
**changement de PC** : les 3 derniers duos, le Toboggan revu, les bugs du premier essai des duos, et les consignes pour
Claude (`CLAUDE.md`, à la racine du dépôt) désormais dans le dépôt, avec ce que Claude avait retenu sur le PC d'avant.

**Sur le nouveau PC** (installé le 1er novembre) : le dépôt est cloné dans `C:\Users\anton\Desktop\dev\Thunk VR` (où est
`CLAUDE.md`), Unity 6000.5.10f1, Python et le SDK .NET sont installés, et les deux `compile_check` passent sans erreur ni
avertissement (détail : `CLAUDE.md`, « Pièges connus »). Les packs d'assets téléchargés mais pas importés
(`projet archery/Assets_TowerDefense/`) étaient hors du dépôt : à recopier à la main depuis l'ancien PC s'ils servent.

### Le deuxième chantier : la refonte du tower defense (27 et 28 octobre, dans une autre discussion)

Tout est dans `Refonte_TowerDefense.md` (partie « Avancement » en tête : ce qui est fait, ce qui reste, l'étape suivante).
En bref (au 28 octobre ; depuis, tout est commité, et le joueur a commencé à essayer les duos dans Unity le 31 octobre),
**codé et compilé, rien de testé dans Unity ni au casque** :
- étape 1, les modèles : 5 packs CC0 dans `projet archery/Assets_TowerDefense/` (hors du dépôt), et ce que le joueur a choisi
  dans `Assets/construction` (pack RTS allégé, modèles Poly Pizza ; crédités) ;
- étape 2 : les **caisses de l'ingénieur** (à la place des cristaux) donnent des **plans** (à la place des matériaux) ;
- étape 3 : la **grille** des zones constructibles (`Building/BuildGrid`), la pose à la **montre** (bouton « Plans »,
  hologramme, joystick : `Building/BuildPlacement`, `UI/WatchPlansButton`), le déplacement ; socles et barricades de départ
  désactivés au lancement ;
- étape 4 : les ennemis **projetés** (`Enemies/Enemy.Launch.cs`) et le tir au vol ;
- étape 5 (les constructions nouvelles), **codée en entier le 28 octobre, en 6 lots** : raretés, mine, goudron, tremplin,
  tour de foudre ; arbalète à répétition, mortier ; geyser, soufflerie, tour-aimant, catapulte ; trappe, pendule, bulle,
  lance-filet ; lance-flammes, phare, miroir, trois anneaux élémentaires ; seau d'huile, baliste sur la tour, esprit
  gardien, avec des emplacements sur la tour (`Editor/TowerDefenseStep5Baker` crée prefabs et réglages, à l'ouverture de
  Unity : enregistrer la scène ensuite). Raretés proposées par Claude et acceptées, sauf celle du seau (rare, proposée).
  La suite : tester les étapes 2 à 5, puis l'étape 6 (les fusions).
- **premiers tests du joueur, le soir du 28 octobre**, et ce qui a été refait (codé, compilé, pas encore testé) : la baliste sur
  la tour tire sur les ennemis collés à la tour ; une **grille de 1 m visible au sol** pendant la pose (verte où le bâtiment se
  pose, rouge ailleurs ; un bâtiment occupe plusieurs cases), à la place des runes ; un **menu des plans** à la montre
  (carrousel de cartes avec l'aperçu 3D de chaque bâtiment) ; une petite baliste du Tower Defense Kit à la place de l'arbalète
  de l'arbalète à répétition et du lance-filet. Puis, après un deuxième essai, la grille « mal cadrée » sur la voie du
  milieu : la voie est maintenant lue dans le sol (la terre des tuiles), plus à 3 m du chemin des ennemis. Puis : le menu
  flotte et rejoint le joueur, on construit sur la rivière, et la pose se fait en un geste (le menu reste ouvert, le
  joystick change de bâtiment, une seule gâchette). Détail : `Refonte_TowerDefense.md`, en tête de « Avancement ».
- **deuxième brainstorm, le 29 octobre** (fait, rien de codé) : le tower defense faisait « gadget ». Décidé : quatre pistes
  (les bâtiments préparent les tirs, tiennent le front, une menace par voie, moins de bâtiments), une tour qui ne se
  reconstruit plus (15 % de PV par vague), et, d'après Bloons TD6, 10 tours à 5 niveaux aux évolutions uniques (des
  niveaux 5 très forts et très visibles, comme la rampe de lancement qui tue à l'atterrissage), 13 duos, 5 trios et
  4 ultimes. **Lot 1 codé** (la tour à 15 %, jamais reconstruite ; les 10 tours), pas encore testé ; **lot 2 commencé** :
  le système des 5 niveaux et le tremplin : catapulte (N3, N4) validée au casque, rampe (N5) revue après l'essai
  (370 m, retombe sur un groupe) et validée, le modèle de tous les niveaux 5 ; les 9 autres tours codées le soir, à tester :
  `Refonte_TowerDefense.md`, parties 12.6 à 12.10.
- **l'allure des tours, le 30 octobre** (codée, compilée, pas encore vue dans Unity) : une palette par tour, les pièces revues
  d'après le troisième retour du joueur, plus de faces confondues, le bug des ennemis plantés dans la rivière ; partie 12.11.
  Pour voir une tour sans Unity ni casque : `Tools/allure_tours/apercu.py` (dessins de chaque niveau d'après le prefab et
  `Editor/TowerDefenseLook.json`) et `faces_superposees.py` ; pour changer une tour, modifier `conception.py`, le relancer,
  vérifier les dessins, puis retirer la marque « Allure du 30 octobre » du prefab ou en changer le nom.
- **l'ennemi préparé (lot 3), le 30 octobre** (codé, compilé, pas encore testé) : un coup du joueur sur un ennemi qu'un
  bâtiment a préparé (en l'air, gelé, cloué, pris au filet, étourdi, englué, attiré, marqué) fait plus de dégâts (de
  +15 % à +100 %, selon la difficulté à le préparer) et 1,5 fois plus de points, avec un mot et un son ; partie 12.12. Ensuite, au choix du joueur : les lots 4 à 6 ou les duos
  (lot 7).
- **les duos, le 31 octobre** (le joueur a refait la formule) : un duo est une tour nouvelle, à l'attaque à elle, à 3 niveaux
  (4 plans chacun) ; 13 duos décidés (partie 12.15). Codés : le système de fusion et le **Boulet fou** (compilé, pas encore vu
  dans Unity) ; partie 12.16. Leur modèle : `Tools/allure_tours/duos.py`, puis `conception.py`. La nuit du 31 octobre, neuf de
  plus (Marteau-pilon, Toboggan, Yo-yo, Avalanche, Feu d'artifice, Chant des sirènes, Loupe, Carquois géant, Chaîne de
  forçats) et les flèches « DUO » entre deux tours qui peuvent fusionner : partie 12.17, compilés, **pas encore vus dans
  Unity**. La règle du joueur : un duo impressionnant, un niveau 3 dans la démesure. Le 31 octobre au soir, après le premier
  essai du joueur (vitrine **F4**) : le Toboggan refait (un toboggan qui enjambe la voie), les bugs de l'Avalanche et de la
  Chaîne de forçats corrigés, et les trois derniers (Boussole folle, Lanceur de baudruches, Boule à neige) : partie 12.18, les
  13 duos codés, **pas encore vus dans Unity**. Le 1er novembre, **les trios triés** (un duo plus une tour) : 7 gardés
  (Bowling, Beffroi, Montagnes russes, Gonfleur, Volcan, Enclume, Colisée) (partie 12.19) ; le même jour, **le système des
  trios et **les 7 trios codés** (Enclume, Gonfleur, Bowling, Volcan, Beffroi, Colisée, Montagnes russes), compilés, modèles
  vérifiés hors de Unity, **pas encore vus dans Unity** (parties 12.20 à 12.26 ; F4 les montre après les duos ; tests :
  `Checklist_Demo.md`, « Les trios »). Le soir, après le premier essai (parties 12.27 à 12.30) : le Volcan est remplacé par la
  **Grande bombarde**, les Montagnes russes refaites en **Grappin**, les baudruches attirent aussi volants et tireurs, le Colisée
  corrigé ; F4 pose une défense aérienne et donne 20 000 PV. **Le joueur valide les nouvelles tours** (commit `d512b9f`, poussé).
  Puis, le même soir, l'équilibrage (partie 12.31, **pas commité, pas essayé**) : les bâtiments ne suivent plus que la racine
  carrée de la force des ennemis (le joueur tenait jusqu'à la vague 45 avec les seules tourelles de F4), et leurs dégâts baissent
  de 20 % (`Editor/StructurePowerBaker`). Le 2 novembre, **le tableau d'équilibrage est fait** (partie 12.33) :
  `Docs/Equilibrage_Tours.xlsx` (les 10 tours, les 13 duos, les 7 trios, niveau par niveau ; les changements proposés par Claude),
  écrit par `Tools/equilibrage_tours.py` d'après les prefabs ; **rien n'est changé dans le jeu**. Le même jour, le plafond
  d'ennemis à la fois passe de 40 à 150 (partie 12.34, `Editor/EnemyCapBaker` ; il ne compte qu'à partir de la vague 13). Le joueur a rempli sa colonne et **tout est
  appliqué** (partie 12.35 : `Editor/BalanceBaker`, le code ; ses idées : la boule du Bowling, Minuit du Beffroi en noir et blanc),
  compilé, **pas encore vu dans Unity ni testé**. Puis les deux roguelikes liés (partie 12.36) : les parchemins du joueur
  valent aussi pour les tours (force, adresse, vigueur), et les tourelles ne suivent plus les vagues. Puis **le coffre unique** (12.38 : un parchemin, une
  tourelle, une tourelle ou un objet ; le coffre de boss : deux objets ou 3 plans). Le soir, la puissance des tours baissée (12.39 : Normal 0,5, Difficile 0,81,
  Impossible 1,19). Puis **le premier essai du joueur** (« tout aux tours ») et ce qu'il en retient (12.40, 12.41) : 4 coffres au gong
  d'un seul côté de la forêt, le chantier de la partie (3 bâtiments, un de plus par boss), un coffre parchemin / tourelle / objet, le
  coffre de boss à 2 objets, un plan par lot et par niveau (duo 2, trio 3), la grille (cailloux, mannequins, gué, la raison affichée),
  la régénération du joueur, MK ×2. Le 2 novembre, **le deuxième essai** (12.42 : les places refusées signalées par le joueur, la
  partie de sa sœur) : 2 plans par coffre, la courbe des PV relevée (pas MK), les tours ×1,2, des vagues plus courtes, la grille
  (plus de limite de distance, habillage des falaises ignoré, la voie réservée à la terre des voies, demi-cases en clairière) ;
  compilé, pas encore vu dans Unity. **LA PROCHAINE TÂCHE** : ouvrir Unity (les bakers), un nouvel essai au casque
  (`Checklist_Demo.md`, « Le deuxième essai du 2 novembre », puis « Le premier essai du 2 novembre »), relancer le tableau
  d'équilibrage ; ensuite : les ultimes. Le même soir : le centre d'entraînement sur la voie du milieu (5 mannequins, dont deux
  qui vont et viennent, avec le visage dessiné par le joueur ; plus de cibles) et les torches au bord des voies (12.43,
  `Editor/TrainingLaneBaker` ; enregistrer la scène après Unity). Les ultimes : règles décidées (12.44 : le niveau 4 d'un trio
  au niveau 3, 10 plans, une par partie, une par famille, 3 en tout) ; choisies : le Colosse de forge, le Grand Manège, la Grande
  Horloge, qui reprend Minuit (l'arrêt du temps) ; le niveau 3 du Beffroi devient le Phare du passé (12.45 : codé,
  `Editor/BeffroiPhareBaker`, pas encore vu dans Unity). Puis **le système des ultimes et le Colosse de forge** (12.46 :
  `Building/TowerUltimate.cs`, `UltimeColosse.cs`, `FinalBoss/DragonBoss.Pin.cs`, le modèle dans `Tools/allure_tours/ultimes.py`,
  fait par `Editor/TowerDefenseLookBaker` ; compilé, pas encore vu dans Unity ; tests : `Checklist_Demo.md`, « Le Colosse de
  forge »). Puis, après l'essai du joueur, **le Colosse refait** (12.47 : l'éveil, le ciel de forge, un éclair à chaque coup,
  la rivière de fonte, le fer martelé et les veines qui luisent ; `Shaders/MoltenIron.shader`, `Editor/ColosseFinitionBaker`,
  `World/SkyController.SetOverlay` et `Strike`). Puis, après son deuxième essai, **le Colosse à deux mains** (12.48, le choix
  du joueur : la main gauche bat le lingot sur l'enclume, un éclat à chaque coup, le lingot et la foudre au 5e ; la droite lance
  un marteau sur chaque flèche qui touche, 5 par seconde au plus ; le ciel de forge seulement à l'éveil et pendant la coulée ;
  compilé, pas encore vu dans Unity ; tests : `Checklist_Demo.md`, « Le Colosse de forge, à deux mains »). Puis, au troisième
  essai, **des marteaux imposants et la force au tableau** (12.49 : aussi grands que le sien, la foudre à chaque coup, 160 dans
  6 m, la forge renforcée ; `Editor/ColosseForceBaker` ; le Colosse dans `Tools/equilibrage_tours.py`, à relancer après Unity),
  et les dégâts sur les boss des vagues (12.50, MK inchangé). **Validé par le joueur** (« ok, la tour me paraît bien »).
  Puis, le même soir, **la deuxième ultime, le Grand Manège** (12.51, faite d'après `Docs/Ultimes_Cahier.md` ; le choix du joueur :
  la ronde, la grande éjection, le feu d'artifice, MK étourdi) : un carrousel géant perché sur son mât ; son ruban fait tourner les
  ennemis sur le manège, au 5e coup il les éjecte tous sur le groupe le plus serré ; un cheval de bois saute sur chaque ennemi que
  touche ta flèche (5 par seconde au plus) ; ses chevaux chargent sur toute la voie ; dans l'arène, leur charge étourdit MK
  (`Building/UltimeManege.cs`, `FinalBoss/DragonBoss.Daze.cs`, `World/SkySettings.Fete`, le modèle dans `Tools/allure_tours/ultimes.py`,
  `Editor/ManegeFinitionBaker`, `Editor/ManegeSonsBaker`, `Tools/texture_bois_peint.py`) ; au tableau d'équilibrage (relancé) : seul,
  un trio moyen ; avec deux flèches par seconde, comme le Colosse. Compilé, **pas encore vu dans Unity ni testé au casque** (tests :
  `Checklist_Demo.md`, « Le Grand Manège ») ; ses sons (l'orgue, la fanfare, le galop, le hennissement, la fusée, le bouquet ;
  freesound, CC0, téléchargés par le joueur) sont découpés et crédités ; le joueur l'a lancé dans Unity : « parfait ». Puis, le
  même soir, **la troisième ultime, la Grande Horloge** (12.52, d'après le cahier ; les choix du joueur en deux séries : les
  chiffres, Minuit après le XII et le dégel qui brise, l'écho du temps, l'aurore, l'horloge astronomique, Minuit dans l'arène) : une
  horloge astronomique de glace et d'or qui flotte ; à chaque tic, un de ses douze chiffres romains tombe sur un ennemi ; après le
  XII, le Minuit du Beffroi (9 s sur toute la carte), puis le givre de tous les figés éclate ; chaque flèche qui touche est rejouée
  par une flèche de glace géante ; dans l'arène, Minuit arrête MK (`Building/UltimeHorloge.cs`, `FinalBoss/DragonBoss.TimeStop.cs`,
  `World/SkySettings.Minuit`, `Editor/HorlogeFinitionBaker`, `HorlogeSonsBaker`, `Tools/texture_glace.py`). Compilé, **pas encore vu
  dans Unity ni testé au casque** (tests : `Checklist_Demo.md`, « La Grande Horloge ») ; ses sons (le tic, la cloche, le temps qui
  s'arrête et qui reprend, la glace qui éclate, le carillon ; freesound, CC0, téléchargés par le joueur) sont découpés et crédités.
  **Les trois ultimes sont faites** ; ensuite, leurs essais. Le 3 novembre, les retouches du joueur : l'Horloge (particules et
  aurores plus petites, les boss des vagues figés 9 s, les trois cristaux du socle retirés : le modèle est refait à l'ouverture de
  Unity) ; **le chantier du début** devient les 3 tours d'un trio tiré au hasard, plus la Barricade (ou un autre si le trio l'a
  déjà) : 4 bâtiments (12.53 ; `Building/BuildManager`, `Editor/StartingTrioBaker` pour le tutoriel). Puis (3 novembre)
  la flèche « ULTIME », la pose lisible (la portée au sol, la voie couverte), la lumière sous les tourelles de l'arène, la
  fluidité (12.55) ; compilés, pas encore vus dans Unity. Le talisman de l'ingénieur (et son duo Réseau) surcharge maintenant
  tous les bâtiments, plus seulement les balistes et les tours de givre (12.57 ; la carte dit « dans une de tes tourelles »,
  `Editor/EngineerTextBaker`, et le duo « à tes tourelles »). Puis tous les bâtiments qui tirent rejoignent l'arène de MK, chacun
  adapté à lui (12.58 : le mortier, 5 duos, 3 trios en plus ; ce qu'ils lancent le suit en vol ; le lance-filet le ralentit) ;
  compilés, pas encore vus dans Unity. Puis la tour attaquée s'annonce par le cor (un son 3D synthétisé) et le
  rayon qui monte et bat en rouge, et elle fume sous la moitié de ses PV, à la place de l'alerte de la montre (12.59). Pour les parties
  de test, le **journal de partie** (`Scripts/Journal/`) : chaque partie s'écrit dans `Logs/Parties/` (le déroulé et un bilan par
  vague), que Claude lit après. Le troisième essai (12.60, `Editor/ThirdTestBaker`) : volants −50 % de PV et piqué jusqu'au bout,
  boss −30 % de PV qui frappent le joueur de près, le joueur à 67 PV avec une grande régénération, la couronne du combo à 5 flèches,
  le Manège −35 %. Le rythme (12.61, 4 novembre) : les coffres ont leurs gardiens (le joueur : pas de verrou, on peut foncer au prix
  de ses PV ; `Enemies/Enemy.Guard.cs`, `Building/CrystalSpawner`), et toutes les vagues ont le chrono de la première. Puis (12.62) la tour à 1 500 PV au lieu de 3 000, et les ultimes −30 % (`Editor/UltimesTowerBaker`) ; la direction du joueur pour
  les bâtiments : contrôler plutôt que tuer. Puis (12.63) les plans par coffre : de 1 à 4 au hasard, plus souvent beaucoup au fil de la partie, 10 % de plus
  sur la partie (`BuildManager`, *Plan Chances*). Le quatrième essai (12.64, au casque) : le Carquois géant limité à 2 flèches par seconde, le journal
  corrigé (au casque ou au clavier à chaque vague, les PV au plus bas). L'Avalanche n'avale qu'une fois (12.65) ; la zone contre la monocible : le joueur ne veut pas
  affaiblir la zone (plutôt renforcer la monocible et les ennemis) et fera une partie monocible. Les ennemis au sol 30 % plus rapides sauf en Facile
  (12.66, *Ground Speed* des difficultés, `Editor/GroundSpeedBaker`). Puis (12.67) la corde des Akimbow corrigée, le voile rouge plus
  visible, le joueur à 100 PV et la Vigueur à +12 (`Editor/PlayerHealth100Baker`) ; à faire plus tard : le menu des constructions (GDD, section 21). Les menaces par voie, le lot 5 (12.56 : chaque voie sa famille
  d'ennemis), codées puis mises en sommeil le même jour (les mots au-dessus des voies tombaient dans les murs ; le joueur :
  « retirons cette mécanique pour le moment » ; les idées pour une autre annonce sont dans 12.56). Plus tard (idée du joueur) : des sorts ou des compétences
  pour la deuxième partie du jeu (GDD, section 24, à préciser).
Raccourcis : **B** (2 plans de chaque), **V** (une caisse), **F2** (menu des plans et pose sans casque), **F3** (projeter les ennemis proches), **F4** (la vitrine : chaque tour, puis chaque duo, chaque trio, puis chaque ultime, posée à plusieurs niveaux).

### Le chantier en cours : la refonte roguelike (25 et 26 octobre)

**Pourquoi** : le joueur trouvait la partie roguelike ratée (des hausses de statistiques en boucle, une cinquantaine
d'améliorations par partie, presque toujours les mêmes). Il a choisi la proposition B, « la flèche en pièces »
(`Refonte_Roguelike.md`, avec le diagnostic, les propositions et la partie « Avancement » à jour).

**Codé, rien n'est encore testé au casque** (le jeu et les bakers compilent sans erreur ni avertissement) :
- étape 1 : les statistiques passent dans 3 parchemins (Force, Adresse, Vigueur) ; les objets se prennent une fois ; la
  boutique sur deux lignes (parchemins, 3 objets chers) sans relance ni réparations (la tour et les barricades se réparent
  seules à la fin de la vague) ; le coffre de boss (2 objets au choix) ;
- étape 2 : chaque objet est une pièce d'une partie de la flèche (pointe, empennage, fût, corde, talisman), niveaux 1 à 3 ;
- étapes 3 et 5 : 24 pièces nouvelles, 6 duos (la corde de l'arc bat d'or) ;
- étape 4 : les pièces se voient sur la flèche et l'arc, un son par pièce ;
- demandé par le joueur le 26 octobre : **l'atelier**, à droite de la boutique (plus de remplacement : on garde toutes les
  pièces et on équipe celle qu'on veut ; la flèche du jeu y tourne en 3D, debout, un sceau animé cliquable sur
  chaque partie, les pièces à sa droite), et **un prix par
  parchemin** (chacun monte quand on le prend souvent ; seule la Vigueur donne des PV max).

**Premiers retours du joueur** (26 octobre au soir, après quelques tests) : les +5 PV max de la Force et de l'Adresse sont
retirés ; la télékinésie faisait toujours monter la flèche (elle suivait l'avant du modèle de l'arc) : elle est maintenant
guidée par rapport à la main (à l'appui, la main sert de référence, puis la flèche tourne comme elle, deux fois plus,
*k_SteerGain* en tête de `ArrowPieces.cs`).

**Réglages du joueur, le 26 octobre au soir** (il a essayé chaque amélioration au casque ; codés, compilent, **pas encore
testés au casque**, rien n'est commité) : plus de dés (foudre, chaîne, glace, déluge aux tirs parfaits ; explosion, perçage,
multitir, écho à chaque tir), de nouvelles valeurs pour une vingtaine de pièces, le boomerang refait, l'empennage spectral
retiré, la super grenade (talisman), le visuel du trou noir, les duos qui nomment la pièce partenaire, et **les mannequins
du centre d'entraînement qui comptent comme des ennemis** entre les vagues (6, dont un groupe de 3). **Puis, le même soir,
le troisième lot** choisi par le joueur parmi les propositions de Claude : le tirage par emplacement, des pièces déplacées,
7 pièces nouvelles (siège, pluie, fantôme, piqué, rappel, sentinelle, avare), 11 duos, une vraie explosion avec de la fumée,
et l'équilibre (explosion affaiblie, multitir serré, triple large, déluge rare, bourreau sur MK). `Scripts/Upgrades/ArrowPieces.Lot3.cs`,
`ExplosionFx.cs`, `Editor/RoguelikePiecesBaker`. Détail : GDD, section 6 (tableau « Avant / Depuis ») ; `Refonte_Roguelike.md`, « Avancement », et partie 14
(les propositions de Claude : le tirage de la boutique, des déplacements, des pièces et des duos nouveaux, l'équilibre) ;
tests : `Checklist_Demo.md`, « Réglages du 26 octobre au soir » et « Troisième lot ». À l'ouverture de Unity,
`Editor/RoguelikeTuningBaker` et `Editor/RoguelikePiecesBaker` modifient le catalogue, le prefab du mannequin et la scène
(l'enregistrer).

**Corrections du 27 octobre** (après de nouveaux tests du joueur ; codées, compilent, **pas encore testées au casque**) : plus
d'effet réservé aux tirs parfaits (`Editor/RoguelikeAllShotsBaker`, une fois), l'anneau entièrement vert pour un tir parfait
d'office, et les bugs du rappel, du piqué, du boomerang, du ricochet, du billard, de la pluie, de l'ingénieur, du fût
suspendu, de la corde fantôme, de l'étoile filante et de Glacier ; six duos du rebond. Détail : GDD, section 6, « Corrections
du 27 octobre » ; tests : `Checklist_Demo.md`, même titre. Puis, à la demande du joueur, six duos du boomerang (un effet au
demi-tour avec la mèche et chaque fût qui agit planté), puis les trois derniers duos de compatibilité (nuage, retour explosif,
averse) ; le rappel laisse en place ce que faisaient les flèches plantées (`Refonte_Roguelike.md`, partie 15). Puis les
réglages du joueur (GDD, section 6, « Réglages du joueur du 27 octobre, après la liste des tests ») : le boomerang rattrapé à
la poignée arrive dans la main et c'est lui qui est parfait, la corde de pluie refaite (des flèches semées le long de la
traînée), rappel et conducteur à 100, fil électrique à 1 m du joueur, fût suspendu deux fois plus tôt, fantôme à
120 / 150 / 200 % (`Editor/RoguelikeTextsBaker` revoit deux textes de cartes) ; la faux attend 0,5 s devant le joueur, les
flèches de pluie ne s'arrêtent plus devant lui avec le boomerang, 100 sentinelles et 100 flèches plantées
(`Editor/RoguelikeStuckArrowsBaker` change la scène : l'enregistrer). Tout est à tester au casque. **Le patch note à remplir** :
`Docs/Patch_Note_Pieces.xlsx` (une ligne par pièce, duo, parchemin et règle, avec les chiffres du 27 octobre) ; le joueur écrit
ses modifications dans la colonne jaune, puis Claude les lit (colonne « Code » = le nom de l'effet ou du duo dans le code)
et les implémente. **Deuxième tour fait** (26 lignes du joueur, codées le 27 octobre : GDD, section 6, « Deuxième tour du 27
octobre »), et la corde de siège avec le katana (les failles mûrissent ; leur qualité suit toujours la longueur du coup).
**Équilibrage du 27 octobre au soir** (GDD, section 6, même titre, avec les estimations de DPS) : le tir parfait ×2,5 de dégâts
et ×3 de points, le tir écho légendaire, la pluie à 35 %, la chasse à l'homme ×1,5, 1000 flèches en vol avec un budget
d'effets (au-delà de 120, les flèches en plus perdent traînée, ombre, plumes et runes ; sons, éclairs et textes plafonnés),
le bruit des flèches de l'Akimbow à moitié. **Les cartes refaites par le joueur** (`Docs/Cartes_A_Refaire.xlsx`, codées le
soir même : GDD, section 6, même titre) : noms, textes qui suivent le niveau (« {50 %|100 %|150 %} »), texte de montée de
niveau (« 50 % → 100 % ➶ », champ `levelUpText`), le symbole ➶ pour les dégâts d'une flèche de base, les raretés, l'icône
de la catégorie, les duos renommés, et le tir explosif aux mêmes dégâts sur toute sa zone. Le joueur a **à peu près tout
testé au casque** le même soir. **Reste** : la courbe des ennemis (en Normal, le build moyen doit un peu galérer à la vague 5,
sans plus), à mesurer avec le **compteur de dégâts des mannequins**, dont les PV sont ×100 (`Training/TrainingDummy.cs`).
L'étape 6 de la refonte est mise de côté par le joueur. Les objets sortent à 50 % communs, 35 % rares, 15 % légendaires
(20 / 60 / 20 avant, `Editor/RoguelikeBalanceBaker`). Chaque coffre de la carte propose toujours un objet, un parchemin et un
bonus temporaire (un objet une fois sur 4 avant ; GDD, section 13). La boutique s'ouvre plus à gauche, l'atelier à l'opposé,
à droite du joueur (`Shop/ShopPanel.cs` ; la scène change par `Editor/RoguelikeBalanceBaker`, à enregistrer). Le coffre de boss
tombe là où le boss est mort (`Chests/ChestSpawner.cs` ; le raccourci X le pose encore juste devant le joueur).

**À l'ouverture de Unity** : cliquer dans la fenêtre ; la console écrit ce que chaque baker a fait (le catalogue de la
boutique est modifié une seule fois par étape). Si un baker a changé la scène, l'enregistrer (Ctrl+S). Puis les tests :
`Checklist_Demo.md`, parties « Refonte roguelike, étape 1 » à « étape 5 » ; raccourcis **R**, **F**, **X**, **M**
(`Demo_Vendredi.md`).

| Fichier | Rôle |
|---|---|
| `Scripts/Upgrades/Upgrade.cs` | les effets (nouveaux toujours à la fin), les genres (objet, parchemin, retiré), les emplacements, le déclencheur « tir parfait » |
| `Scripts/Upgrades/PlayerUpgrades.cs` | ce qu'on possède (`Level`) et ce qui joue (`Stacks` : les pièces équipées, les parchemins), `Equip` |
| `Scripts/Upgrades/SpecialArrows.cs` | les anciens effets (foudre, glace…), la corde triple ; crée `ArrowPieces` et `PieceLook` |
| `Scripts/Upgrades/ArrowPieces.cs`, `ArrowPieces.Lot2.cs` | les 24 pièces nouvelles et ce que font les duos (réglages en tête des fichiers) |
| `Scripts/Upgrades/ArrowDuos.cs` | les 6 duos, « Duo possible » |
| `Scripts/Upgrades/PieceLook.cs`, `PieceSounds.cs` | les pièces visibles (flèches, corde, breloque, flèche de l'atelier) et leurs couleurs ; les sons fabriqués |
| `Scripts/Shop/ShopCatalog.cs`, `ShopManager.cs`, `ShopPanel.cs`, `ShopCard.cs`, `ShopOffer.cs` | la boutique : prix, offres, panneau |
| `Scripts/Shop/ArrowWorkshop.cs` | l'atelier : la flèche 3D debout, les sceaux animés et leurs légendes, les pièces à équiper (place, tailles et vitesses en tête du fichier) |
| `Scripts/Chests/ChestReward.cs`, `Chest.cs`, `ChestSpawner.cs` | les coffres et le coffre de boss |
| `Editor/RoguelikeStep1Baker.cs` à `RoguelikeStep5Baker.cs` | les changements du catalogue et de la scène, faits une fois |
| `Editor/RoguelikeTuningBaker.cs` | les réglages du joueur du 26 octobre au soir (catalogue, mannequin, mannequins en plus), faits une fois |
| `Editor/RoguelikeAllShotsBaker.cs` | le 27 octobre : les cartes réservées aux tirs parfaits jouent à chaque flèche, fait une fois |
| `Editor/RoguelikeTextsBaker.cs` | le 27 octobre : les textes de l'empennage boomerang et de la corde de pluie, s'ils sont encore les anciens |
| `Editor/RoguelikeStuckArrowsBaker.cs` | le 27 octobre : 100 flèches plantées au plus dans la réserve de la scène (si elle a encore 40) |
| `Editor/RoguelikePatchBaker.cs` | le deuxième tour du 27 octobre : écho, vampirisme, vigueur (catalogue), chaîne et ricochet (scène), si les anciennes valeurs y sont encore |
| `Editor/RoguelikeCardsBaker.cs` | les cartes refaites par le joueur le 27 octobre au soir : le sprite des symboles (`Resources/Sprite Assets/Symboles`), puis nom, texte par niveau, montée de niveau, rareté et icône de chaque carte qui n'a pas encore de `levelUpText` |
| `Editor/RoguelikeBalanceBaker.cs` | l'équilibrage du 27 octobre au soir : tir écho légendaire et chances 50 / 35 / 15 (catalogue), tir parfait ×2,5 de dégâts et ×3 de points (`Data/ShotTuning.asset`), 1000 flèches en vol et la boutique plus à gauche (scène), si les anciennes valeurs y sont encore |
| `Scripts/Training/TrainingDummy.cs`, `Scripts/Upgrades/BlackHole.cs` | le mannequin qui compte comme un ennemi entre les vagues ; le visuel du duo Trou noir |
| `Scripts/Core/DemoBoost.cs` | les raccourcis F et R |

**En attente du joueur** : quels anciens objets ne jouent qu'aux tirs parfaits (tableau dans `Refonte_Roguelike.md`), la
courbe des ennemis avec moins d'améliorations, les textes et les icônes des pièces nouvelles (provisoires), la hausse
de prix de chaque parchemin (*Own Scroll Price Increase*, 10 %). **Ensuite** : l'étape 6
(des pièces débloquées par les succès, une réserve par source).

## 2. Le katana, 3e version (refait le 24 octobre, après l'oral ; testé au casque le même jour, satisfaisant)

Plus de gantelet : la main de l'arc devient **le gant de la matrice** (GDD, section 5, paragraphe « Refait le 24 octobre, après
l'oral »). Choix du joueur : la qualité vient de la longueur du coup ; une faille par coup ; toutes les failles qui se croisent
fusionnent, quelle que soit leur forme ; le poing frappe aussi les ennemis ; le coup raté se voit ; la traînée de la lame ; un
bref ralenti au lancer parfait et les ennemis tués qui se défont en 0 et 1. La fenêtre du tutoriel des armes spéciales, le
joueur l'ajoutera lui-même plus tard, pour toutes.

- **La coupe** : la pointe à 3 m/s dans le champ de vision (55°), jamais en courant aux bras ; la faille naît tout de suite et
  s'ouvre au fil de la lame ; un demi-tour de la lame (plus de 110°) finit la faille et en commence une autre, sans pause ; au
  moins 35 cm de geste et 25 cm entre son début et sa fin, sinon la fissure grésille en rouge et se referme. 5 failles au plus.
- **La faille** : un fil de lumière juste derrière la lame, écarté en 0,07 s, puis elle s'élargit pendant 1,4 s ; plus large au
  centre (jusqu'à 16 cm), pointue aux bouts, en 3D le long du vrai chemin de la lame ; elle vit 3 s.
- **La qualité** : la longueur du coup (moyen sous 55 cm, bon, très bon, parfait dès 95 cm) ; des failles fusionnées sont
  parfaites, +35 % de dégâts par faille en plus, et explosent au premier ennemi touché.
- **Après le test** (24 octobre, à revoir au casque) : 5 failles au plus en une fusion ; la fusion se voit (cyan, bond, onde
  depuis le croisement, nœud lumineux, chiffres qui jaillissent : `_Fuse0` à `_Fuse3` dans `MatrixTear`) ; le katana est
  retourné de 180° autour de sa lame (il était tenu le tranchant vers l'arrière ; repère « Tranchant » dans son prefab).
- **Après le deuxième test** (24 octobre, à revoir au casque) : 5 failles en tout, fusions comprises (une nouvelle referme
  la plus ancienne, même dans une fusion : `MatrixRift.DetachOldest`) ; la faille s'ouvre au bout de la lame (seuils de
  longueur ×1,6, à régler) ; le coup de poing lance une onde de choc (`Combat/ShockWave.cs`) qui propulse les failles ;
  rangé, le katana se tient debout à côté du joueur, comme l'arc (`k_KatanaHolster` dans `Katana.cs`).
- **Le lancer** : un coup de poing du gant de la matrice (1,3 m/s, à 40 cm) ; à 2,2 m/s, le poing frappe et repousse les
  ennemis. Les améliorations des flèches servent aux failles (multitir, écho, déluge, perçage, ricochet, auto-visée, foudre,
  explosion, glace, vampirisme).
- **Le gong** : la faille, la lame et le poing déclenchent ce qui réagit aux flèches, sauf les menus et les panneaux de difficulté.

| Fichier | Rôle |
|---|---|
| `Scripts/Combat/Katana.cs` | L'arme (ancien `KatanaGauntlet.cs`) : le katana flottant (prise, coup au corps à corps, coupe des projectiles, gong), le gant de la matrice (matériau, battement, poing), la coupe (champ de vision, demi-tours, coup raté), la fusion, la qualité, le lancer et les améliorations (multitir, écho), le ralenti, les ennemis défaits en 0 et 1, le jeu au clavier, le raccourci **O** |
| `Scripts/Combat/MatrixRift.cs` | Une faille : elle grandit au fil de la lame, s'ouvre ou grésille, fusionne (plusieurs brins), vole vers sa cible (perçage, ricochet, auto-visée, déluge, effets et explosion à l'impact) |
| `Scripts/Combat/MatrixRiftMesh.cs` | Le squelette de la faille (les brins et leurs fissures secondaires, en 3D) ; sa forme vient du shader |
| `Scripts/Combat/BladeTrail.cs` | La traînée de la lame |
| `Scripts/Combat/IPlayerShot.cs` | Une faille compte comme un tir pour le score, le combo et le vampirisme |
| `Shaders/MatrixTear.shader` | La faille vivante : ouverture au fil de la lame (`_Age`, moment de passage de la lame), profil plus large au centre (`_Bulge`), bords et tracé animés ; dedans, le vide et les 0 et 1 verts |
| `Shaders/MatrixGlove.shader`, `Shaders/MatrixDigits.shader` | Le gant de la matrice ; les chiffres des ennemis tués |
| `Shaders/KatanaBlade.shader` | La lame néon qui bugue |
| `Editor/KatanaBaker.cs` | Le prefab, les matériaux (`Art/Katana` : `Dechirure`, `Gant matrice`, `Trainee lame`, `Chiffres matrice`), l'aperçu du menu, la fiche `Data/BowClasses/Bow_Katana.asset` (« Katana ») et son texte |
| `Upgrades/SpecialArrows.cs` | Prête aux failles ses effets à l'impact (`RollEffects`, `Impact`), son explosion (`Blast`) et ses réglages (écho, déluge, ricochet, auto-visée) |
| `Player/GlovedHand.cs` | `SetGloveMaterial` : le gant de la matrice ; `WristFrame` : le poing |

**Si quelque chose ne va pas au test**, les réglages sont des constantes en tête de `Katana.cs` (seuils de la coupe, champ de
vision, demi-tour, seuils de qualité, fusion, poing, ralenti, durée de vie), de `MatrixRift.cs` (vol, déluge) et de
`MatrixRiftMesh.cs` (largeur), et des propriétés des matériaux `Art/Katana/Dechirure` (ouverture derrière la lame,
élargissement, plus large au centre, bords) et `Gant matrice`.

L'ancienne version aux traces libres (katana et bouclier) est dans Git, au commit `90d5f83` ; celle aux symboles, au commit
`ccbe156` ; celle aux failles qui s'ouvrent comme un œil, au commit `f506918`.

## 3. Les autres choses à faire

**La suite du développement, dans l'ordre** (choisie par le joueur le 26 octobre ; détail dans le GDD, section 21, « La
suite, dans l'ordre ») :
1. finir la refonte roguelike (partie 1 et `Refonte_Roguelike.md`) ;
2. la refonte des tourelles : plus de matériaux, des sortes de coffres qui proposent plusieurs tourelles au choix, beaucoup
   plus de tourelles et de constructions, construire n'importe où sur les zones constructibles ; brainstorm fait le
   27 octobre (dans une autre discussion que la refonte roguelike) : `Refonte_TowerDefense.md`, partie « Avancement » ;
3. le troisième easter egg : le katana en sera l'arme ; un monde fait presque entièrement de 0 et 1 ;
4. le boss d'après MK, qui fait la transition entre l'acte 1 et l'acte 2 ;
5. commencer l'acte 2.

Le reste, qui se glisse entre ces étapes, dans l'ordre conseillé (aussi dans le GDD, section 21, « À faire ensuite ») :
1. **La présentation** : faite le 23 octobre (partie 4) ; il reste à y mettre les captures, puis les répétitions
   (`Demo_Vendredi.md` : pitch, déroulé, raccourcis).
2. Les tests au casque des nouveautés (`Checklist_Demo.md`).
3. Tester au casque le katana, 3e version (partie 2), et le régler.
4. Le build Windows en *Development Build* (les raccourcis de démo y marchent).
5. La vidéo `Tir_3` du tutoriel (la grenade), les crédits des musiques de l'ami.
6. Le ménage des fichiers inutilisés, fait le 25 octobre (`Docs/Fichiers_Inutilises.md` : 3 726 fichiers, 740 Mo, dont
   le pack Easy Weapons et presque tout le modèle VR de Unity). À vérifier à l'ouverture de Unity (`Checklist_Demo.md`,
   « Ménage »). Ce qui reste (43 Mo) est gardé exprès : les modèles des kits, pour plus tard.
7. Tester au casque le toit cyberpunk refait le 24 octobre (décor 4 et épreuve rendue faisable : GDD section 23). Les
   réglages de l'épreuve sont sur l'objet « Toit cyberpunk » de la scène `Dimension_Cyberpunk` ; le décor est fait par
   `Editor/CyberDimensionBaker.cs` (la scène est refaite toute seule quand sa version change : la refaire efface ce qu'on y
   a changé à la main).
8. Tester le lore, acte 1, première partie (GDD section 24 ; `Checklist_Demo.md`, « Lore, acte 1 »), puis fixer ses nombres
   avec la condition d'apparition des runes.
9. Le Quest seul : premier build testé le 25 octobre au soir, qui rame un peu vers la vague 10. Mesurer une partie
   (`Docs/Audit_Performance_Quest.md`, partie 8), puis alléger ce qui pèse.
10. La refonte roguelike (partie 1, « Le chantier en cours », et `Docs/Refonte_Roguelike.md`) : tester au casque les étapes 1 à 5
    (codées les 25 et 26 octobre : `Checklist_Demo.md`, « Refonte roguelike » ; R pour essayer les pièces nouvelles,
    F pour les avoir toutes dans l'atelier), les régler, écrire leurs textes, décider quels anciens objets ne jouent
    qu'aux tirs parfaits, reprendre la courbe des ennemis.
11. Plus tard : le mode démo en tutoriel interactif, la suite du lore (`Lore.md`, GDD section 24).

## 4. Pour la présentation

**Le PowerPoint** (23 octobre) : `Presentation/THUNK_VR_Presentation.pptx` (à la racine du dépôt), avec un PDF de secours.
12 diapos dans la direction artistique du jeu (fond violet, cadres dorés de Kenney, titres en Cinzel), la parole partagée à
parts égales (Merleau : le concept ; Jude : ce qui est réalisé ; Antonin : la technique et les difficultés ; Dovin : l'équipe,
la collaboration, le bilan), ce que chacun dit dans les notes de l'orateur. Il est généré par
`py Presentation/generer_presentation.py` (PowerPoint et la police Cinzel installés) : déposer les captures dans
`Presentation/captures/` (les noms attendus sont en tête du script : titre, tir, vague, boutique, mk, construction, armes,
grenade, secret, unity, github), puis relancer ; une capture absente laisse un cadre « capture à venir ».

- **Le pitch** et les **trois idées à retenir** : `Demo_Vendredi.md`, parties 2 et 3 (déroulé de 10 minutes, version courte de 5).
- **Les consignes du cours et ce qui y répond** : le tableau « Fonctionnalités » du README, et le GDD, section 20.
- **Les chiffres** : 6 vagues (la 6e : le dragon MK) ; un boss par vague ; 4 difficultés, chacune avec son ciel ; 23 améliorations
  de 3 raretés ; 12 socles, 4 bâtiments à 3 niveaux ; 2 secrets ; armes : arc de chasse, arcs court, puissant et runique, Akimbow,
  katana, arc cosmique, arc légendaire, grenade à flèches.
- **Ce qui est technique et montrable** : l'arène du dragon construite par le code depuis un plan texte ; les mains gantées
  animées par le code (squelette ajouté au gant) ; la course aux bras, le slide et le saut aux bras ; les shaders faits pour le jeu
  (lame néon qui bugue, faille Matrix, surface de l'arc cosmique, arbalètes cyberpunk) ; les bakers d'éditeur qui fabriquent
  modèles, matériaux et prefabs.
- **L'équipe et les crédits** : README (parties « Équipe » et « Crédits ») et `Credits_Licences.md` (tous les assets et leurs
  licences ; les modèles en CC-BY doivent être crédités).
- **Captures** : à prendre dans Unity (vue Game) ou dans le casque, et à déposer dans `Presentation/captures/`.
