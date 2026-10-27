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
sceaux animés), les PV des parchemins retirés, la télékinésie guidée par la main.

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
