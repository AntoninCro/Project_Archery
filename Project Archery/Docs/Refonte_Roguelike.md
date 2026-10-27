# Refonte de la partie roguelike (propositions du 25 octobre)

Discussion de conception, puis suivi du travail. Les chiffres donnés ici sont des points de départ : les valeurs de la scène
et de `Data/*.asset` restent celles du joueur, et les textes des nouvelles cartes sont des propositions qu'il valide ou
réécrit.

## Avancement

**Le joueur a choisi la proposition B, « la flèche en pièces » (25 octobre).**

**Corrections du 27 octobre, codées, pas encore testées au casque** (après les tests du joueur ; le détail : GDD, section 6,
« Corrections du 27 octobre » ; tests : `Checklist_Demo.md`) : plus d'effet réservé aux tirs parfaits (`Editor/RoguelikeAllShotsBaker`),
l'anneau entièrement vert quand le tir suivant est parfait d'office, le rappel qui blesse ce qu'il frôle, le piqué qui pique
sur l'ennemi visé, le boomerang que la main de l'arc ne rattrape plus, le ricochet qui suit sa cible, le billard en chaîne,
la pluie qui porte les pièces, l'ingénieur qui surcharge en traversant le bâtiment (qui rougit), le rayon du fût suspendu,
l'aide à la visée de la corde fantôme, l'étoile filante en couronne de 6 / 12 / 18 flèches, Glacier revu, et six duos du
rebond (patrouille, chausse-trappes, haute tension, flipper, fragmentation, patinoire). Les duos qui rendraient compatibles
d'autres paires de pièces sont proposés au joueur (partie 15) ; il les a tous choisis, codés le même jour : six du boomerang
(retour de flamme, tourbillon, escorte, fil électrique, semeur, mitraille), nuage, retour explosif et averse ; et le rappel
laisse en place ce que faisaient les flèches plantées.

**Étape 1, codée le 25 octobre, pas encore testée au casque** (GDD, sections 6, 7 et 13 ; tests : `Checklist_Demo.md`,
« Refonte roguelike, étape 1 ») :
- les objets (14) ne se prennent qu'une fois ; les statistiques passent dans 3 parchemins (Force +12 % de dégâts, Adresse
  +10 % d'anneau, Vigueur +15 PV), sans limite ; Dégâts, Puissance, Force du géant, Flèches rapides, Charge rapide,
  Précision, Vitalité, Butin et Chance sont retirées (gardées dans le catalogue, jamais proposées) ;
- la boutique, retouchée le soir même à la demande du joueur : deux lignes de 3 cartes, les parchemins en haut (25 or,
  +5 % par achat), 3 objets en bas, tous achetables mais chers (120 / 160 / 220 or, +15 % par objet acheté) : améliorer
  ses parchemins doit rapporter plus que d'acheter les objets au hasard ; plus de relance ni de réparations (la tour et
  les barricades se réparent seules à la fin de chaque vague), plus de carte des arcs ; raretés des objets 20 / 60 / 20 %
  (première version : 1 objet par pause, relançable, à 70 / 100 / 140) ;
- les coffres de vague : un objet une fois sur 4, sinon un parchemin ; un autre parchemin ; un bonus temporaire ;
- le coffre de boss : à la fin de chaque vague, près du joueur, 2 objets au choix (raccourci **X**) ;
- le catalogue est converti une fois par `Editor/RoguelikeStep1Baker` ; `Scripts/Upgrades/Upgrade.cs` (genres, déclencheur,
  parchemins), `PlayerUpgrades`, `SpecialArrows`, `Shop/*`, `Chests/*`, le katana et ses failles, `Core/DemoBoost` (F).

**Les effets au tir parfait : décidés par le joueur le 26 octobre au soir** (partie « Réglages du joueur » plus bas, et GDD,
section 6) : la foudre, la chaîne d'éclairs, la glace et le déluge à chaque tir parfait ; l'explosion, le perçage, le multitir
et l'écho à chaque tir. Les descriptions des cartes restent à réécrire par le joueur. Première proposition, gardée pour
mémoire :

| Objet | Aujourd'hui | Proposé (valeur, déclencheur) |
|---|---|---|
| Multitir | 50 % de chance d'une flèche en plus | toujours une flèche en plus (1, à chaque tir) |
| Tir écho | 25 % de chance que le tir se répète | chaque tir parfait se répète (1, tir parfait) |
| Déluge | 50 % de chance que la flèche se divise | elle se divise toujours en deux (1, à chaque tir) |
| Perçage | 25 % de chance de traverser un ennemi | elle traverse toujours un ennemi (1, à chaque tir) |
| Flèche de foudre | 20 % de chance d'appeler la foudre | chaque tir parfait appelle la foudre (1, tir parfait) |
| Flèche de glace | 25 % de chance de geler le sol | chaque tir parfait gèle le sol (1, tir parfait) |
| Flèche explosive | 25 % de chance d'exploser | chaque tir parfait explose (1, tir parfait) |
| Chaîne d'éclairs | sans la foudre, une flèche sur cinq appelle l'éclair | sans la foudre, chaque tir parfait (3 rebonds, tir parfait) |

**Étape 2, codée le 26 octobre, pas encore testée au casque** (GDD, section 6 ; tests : `Checklist_Demo.md`, « Refonte
roguelike, étape 2 ») :
- chaque objet a son emplacement (réglage *Slot* du catalogue, rangé une fois par `Editor/RoguelikeStep2Baker`) : Pointe
  (Foudre, Chaîne d'éclairs, Glace, Explosive, Perçage, Vampirisme, Chasseur de têtes), Empennage (Auto-visée,
  Ricochet), Fût (Déluge), Corde (Multitir, Écho) ; le Talisman attend l'étape 3 ; les objets de la grenade restent hors
  emplacement ;
- une pièce prise pour un emplacement occupé remplace l'ancienne (perdue, avec ses niveaux : la réponse par défaut à la
  question 5 de la partie 13) ; la même pièce monte de niveau, jusqu'à 3 ;
- les cartes et les orbes disent l'emplacement, le niveau et la pièce remplacée ; la ligne du bas de la boutique liste la
  flèche emplacement par emplacement ; le raccourci **F** donne une pièce par emplacement ;
- **écarts avec le plan** : la montre ne liste pas les pièces (elle a déjà 10 lignes dans 200 px ; elles se verront sur la
  flèche à l'étape 4) ; les réserves par source (boss, coffre, boutique) attendent l'étape 6, quand il y aura assez de
  pièces.

**Étape 3, codée le 26 octobre, pas encore testée au casque** (GDD, section 6, avec le tableau des pièces ; tests :
`Checklist_Demo.md`, « Refonte roguelike, étape 3 » ; raccourci **R**) :
- les 8 pièces du premier lot : corde triple (dans `SpecialArrows`), empennages boomerang et télékinésie, fûts aimant et
  conducteur, pointes à mèche et de givre, talisman d'acrobate (`Scripts/Upgrades/ArrowPieces.cs`, créé tout seul à
  côté de `SpecialArrows`) ; ajoutées au catalogue par `Editor/RoguelikeStep3Baker`, niveau 3 au plus ;
- ce qu'il a fallu ajouter ailleurs : l'arme et la main de chaque tir (`ShotInfo.Aim`, `ShotInfo.Hand`), le tir parfait
  accordé (`Bow.ClaimPerfectShot`), l'événement d'une flèche plantée (`Arrow.Stuck`), le gel des ennemis
  (`Enemy.Freeze`), l'état « en l'air » du joueur (`ArmSwingLocomotion.IsAirborne`) ;
- les effets d'explosion sont réutilisés (6 au plus) : un suspect de l'audit de performance en moins ;
- **écarts avec le plan** : les pièces de vol, de fût et de pointe ne marchent qu'avec les flèches (arc, arbalètes) ; au
  katana, seule la corde triple compte (des failles en plus). Le boomerang est « rattrapé » quand il passe près d'une main,
  sans geste de prise. Les textes et les icônes des cartes sont provisoires (icônes reprises des cartes qui ressemblent).

**Étape 4, codée le 26 octobre, pas encore testée au casque** (GDD, section 6 ; tests : `Checklist_Demo.md`, « Refonte
roguelike, étape 4 ») :
- sur chaque flèche, de petits maillages lumineux de la couleur des pièces : un losange autour de la pointe, trois plumes à
  l'empennage, trois runes autour du fût, plus grands à chaque niveau (`Scripts/Upgrades/PieceLook.cs`, avec le tableau
  des couleurs) ; une lueur le long de la corde de l'arc (`Bow.SetStringGlow`) ; la breloque du talisman qui pend sous
  la poignée et se balance ;
- un son par pièce, fabriqué par le jeu (`Scripts/Upgrades/PieceSounds.cs`) : le motif de l'emplacement quand on prend
  une pièce, et les sons d'action qui manquaient (demi-tour du boomerang, télékinésie, mèche, ralenti de l'acrobate) ;
- **écarts avec le plan** : la flèche n'a qu'un maillage et un matériau, d'où les pièces ajoutées par-dessus plutôt que
  la pointe ou les plumes recolorées ; les Akimbow n'ont ni lueur de corde ni breloque ; l'épaisseur du fût est supposée
  (le maillage ne se lit pas en jeu).

**Étape 5, codée le 26 octobre, pas encore testée au casque** (GDD, sections 6 et 7 ; tests : `Checklist_Demo.md`,
« Refonte roguelike, étape 5 » ; raccourcis **R** et **F**) :
- les 16 pièces du deuxième lot (`Scripts/Upgrades/ArrowPieces.Lot2.cs`), ajoutées au catalogue par
  `Editor/RoguelikeStep5Baker`, niveau 3 au plus ; ajouts dans `Arrow` (dégâts en vol, traverser le décor, rebond, arrêt
  en l'air), `Bow` et `Akimbow` (gravité des flèches, suivie par l'aide à la visée), `Structure`, `Ballista`, `FrostTower`
  (surcharge), `PlayerUpgrades` (pacte de verre, vitesse de l'anneau de la transe), `SpecialArrows`, `PieceSounds` ;
- les 6 duos (`ArrowDuos.cs`) : annoncés, la corde qui bat d'or, « Duo possible » en doré sur les cartes et les orbes ;
- les failles du katana passent par `ArrowPieces.OnRiftHit` (givre, chasseur, bourreau ; mèche et aimant au premier ennemi) ;
- **demandé par le joueur le même jour** : l'**atelier** (`Scripts/Shop/ArrowWorkshop.cs`, construit par le
  `ShopPanel` à droite de la boutique) : plus de remplacement, toutes les pièces prises restent, une seule équipée par
  emplacement, celle d'un emplacement vide s'équipe toute seule ; dans sa troisième version, la flèche du jeu y tourne
  en 3D, debout, chaque partie entourée d'un sceau animé (comme les points faibles des boss), les pièces à sa droite ;
  on clique une partie pour la changer ; et **chaque parchemin a son prix** (+10 % par achat du
  même, *Own Scroll Price Increase*) ; la Force et l'Adresse ont donné aussi +5 PV max, comme dans *Dead Cells*, retiré le
  même jour à la demande du joueur ;
- **écarts avec le plan** : la question 5 de la partie 13 (que devient une pièce remplacée ?) a maintenant la réponse du
  joueur : rien n'est perdu, on choisit dans l'atelier. Textes, icônes et couleurs des pièces provisoires.

**Premiers retours du joueur** (26 octobre au soir) : les +5 PV max de la Force et de l'Adresse sont retirés ; la
télékinésie faisait toujours monter la flèche (elle suivait l'avant du modèle de l'arc) : elle est maintenant guidée par
rapport à la main (à l'appui, la main sert de référence, puis la flèche tourne comme elle, deux fois plus).

**Réglages du joueur, le 26 octobre au soir** (après avoir essayé chaque amélioration au casque ; codés, pas encore testés au
casque ; le détail, avant et après, est dans le tableau du GDD, section 6 ; tests : `Checklist_Demo.md`, « Réglages du
26 octobre au soir ») :
- **plus de dés** : la foudre (50/100/150 % des dégâts), la chaîne d'éclairs (25/50/75 %), la glace (40/50/60 % plus lents,
  3/4/5 m) et le déluge (1/2/3 flèches) jouent à chaque tir parfait ; l'explosion (100/150/200 %, 4/5/6 m), le perçage (2/4/99
  ennemis) et le multitir (2/3/4 flèches) à chaque tir ; l'écho garde un dé (50/100/150 %) mais part de la flèche, là où elle
  est rendue. Valeurs par niveau sur l'objet *Special Arrows* de la scène (champs « … By Level ») ;
- les pièces : chasseur de têtes et pointe de chasseur plus forts, boomerang refait (demi-tour à plat, retour sur le décor,
  plus lent, il attend devant le joueur), télékinésie (160 °/s, la flèche ralentit), rebond, plume, aimants (9), arcs (12),
  pieux (20, 2 m, deux fois moins forts), suspendu (vers la main), faucon sans recharge, acrobate (et sa glissade),
  ingénieur (×2/4/6, et la réparation des barricades), bourreau (37/50 %, et les boss) ; l'empennage spectral est retiré ;
  Grenade sauvage et Grenade rapide deviennent le talisman légendaire « Super grenade » ;
- les parchemins ne montaient plus avec les vagues (remis le soir même) ; les cartes nomment la pièce partenaire d'un duo ; le trou noir a son
  visuel ; les mannequins du centre d'entraînement comptent comme des ennemis entre les vagues (6 mannequins, dont un groupe
  de 3) ;
- le catalogue et les mannequins changent une fois par `Editor/RoguelikeTuningBaker` (la scène est à enregistrer).

**Remarque** : beaucoup de tests du joueur ont été faits sur les mannequins, qui n'étaient pas des ennemis avant ce soir :
la foudre, l'explosion, la marque, le bourreau, l'aimant… n'avaient rien à toucher. Les retours sur ces pièces sont à revoir
avec les nouveaux mannequins.

**Le troisième lot et les choix du joueur, le 26 octobre au soir, suite** (parmi les propositions de la partie 14 ; codés,
pas encore testés au casque ; détail : GDD, section 6, après le tableau « Avant / Depuis » ; tests : `Checklist_Demo.md`,
« Troisième lot ») :
- **gardé tel que proposé** : le tirage par emplacement (14.1), les déplacements (14.2), les 7 pièces de 14.3 (corde de siège,
  qui charge aussi le laser de l'arc cosmique ; corde de pluie, qui multiplie la pluie de l'arc légendaire ; corde fantôme ;
  empennage piqué ; empennage de rappel ; fût-sentinelle ; talisman de l'avare) et les 11 duos de 14.4 (le lance-grenades
  explose au rayon de la mèche, demandé par le joueur) ; `Scripts/Upgrades/ArrowPieces.Lot3.cs`, `Editor/RoguelikePiecesBaker` ;
- **une vraie explosion** (feu, fumée épaisse, poussière, étincelles : `Scripts/Upgrades/ExplosionFx.cs`), demandée par le
  joueur à la place des petits éclairs orange ;
- **l'équilibre (14.5)** : l'explosion affaiblie (75/100/125 %, 3,5/4/4,5 m) ; le multitir resserré (2°) et la corde triple
  élargie (9°) ; le déluge passe en rare ; le prix des parchemins remonte avec les vagues ; le bourreau sur MK à 5/7,5/10 % ;
  laissés tels quels, voulus par le joueur : l'ingénieur (« fun »), le faucon, la super grenade (« des lags satisfaisants, si
  c'est bien dosé »), le pacte de verre (pas de réponse).

**Ensuite** (mis à jour le 27 octobre au soir) :
- **Fait** : les textes, les raretés et les icônes des cartes sont refaits par le joueur (GDD, section 6, « Les cartes refaites
  par le joueur »). Le joueur a **à peu près tout testé au casque** ; le reste des combinaisons le sera en jouant, au fil des
  séances de test.
- **Plus de pièces par partie** (demandé par le joueur, pour plus de liberté dans le build) : chaque coffre de la carte propose
  toujours un objet, un parchemin et un bonus temporaire (GDD, section 13). Les objets sortent à 50 % communs, 35 % rares,
  15 % légendaires.
- **Reste** : la courbe des ennemis. En Normal, le build moyen doit un peu galérer à la vague 5, sans plus. La mesure se fait
  avec le compteur de dégâts des mannequins, dont les PV sont ×100 (GDD, section 6, les mannequins).
- **Mis de côté** : l'étape 6 (des pièces débloquées par les succès, une réserve par source, une pièce moins proposée si on l'a
  prise à la partie d'avant). Le joueur ne pense pas qu'elle soit bonne pour le jeu ; peut-être plus tard.

## 1. Le diagnostic du joueur

Il existe deux types de roguelike :
1. ceux où l'on augmente seulement les statistiques du personnage (par exemple *Elden Ring Nightreign*) ;
2. ceux qui changent la façon de jouer (*The Binding of Isaac*, *Dead Cells*, *Windblown*…).

THUNK a un peu des deux sur le papier, mais en jeu on se sent clairement dans le premier type, celui qu'on reproche aux
mauvais roguelikes et qui tue la rejouabilité. Trois causes :
- **les améliorations se ressemblent trop** : surtout des hausses de caractéristiques en boucle ; même les flèches à effets
  (foudre, explosion) ne font qu'augmenter les dégâts autrement ; l'auto-visée fait exception ;
- **il y a trop d'améliorations par partie** : on finit avec à peu près le même jeu à chaque partie, à deux ou trois
  améliorations près ;
- **il y a trop peu d'améliorations différentes**, ce qui, avec le point précédent, rend les parties semblables.

La référence : Isaac. Chaque partie est unique, on n'obtient pas les mêmes objets, et leur combinaison en fin de partie rend
le tir du personnage reconnaissable d'une partie à l'autre.

## 2. Le système actuel, en chiffres

### 2.1 Les 23 améliorations (`Data/Shop/ShopCatalog.asset`)

| Type | Améliorations | Nombre |
|---|---|---|
| Statistiques pures | Dégâts, Puissance, Force du géant (trois fois la même : +10, +25, +60 % de dégâts), Flèches rapides, Charge rapide, Précision, Vitalité, Butin, Chasseur de têtes, Chance, Vampirisme, Grenade sauvage, Grenade rapide | 13 |
| Effets au hasard (« X % de chance ») | Multitir, Tir écho, Déluge, Perçage, Flèche de foudre, Chaîne d'éclairs, Flèche de glace, Flèche explosive | 8 |
| Changent le comportement du tir | Auto-visée, Tir ricochet | 2 |

Raretés : 8 communes, 9 rares, 6 légendaires, tirées à 70 / 25 / 5 %. Aucune limite d'exemplaires (`maxStacks` à 0
partout, alors que le code sait déjà en gérer une).

Le diagnostic se lit dans les données : 13 cartes sur 23 sont des statistiques, et 8 sont des dés qui ajoutent des dégâts.
Un effet qui arrive une flèche sur quatre, au hasard, ne se sent pas comme une façon de tirer : le joueur ne le provoque
pas et ne le voit pas venir.

### 2.2 Combien par partie

- **Boutique** : 5 pauses (après les vagues 1 à 5), 4 cartes à chaque fois, relance possible. Prix 10 / 25 / 45, +24 % par
  vague, **+5 % par achat** (valeur de l'asset ; le GDD, section 7, dit encore +10 %).
- **Coffres** : 2 par vague et 50 % de chance d'un troisième, aux vagues 1 à 5, soit une douzaine par partie. Chacun propose
  2 améliorations permanentes et un bonus temporaire ; celles des coffres ne font pas monter les prix.
- **Estimation** (simulation de la boutique avec les prix de l'asset, pour un joueur qui dépense tout son or) : avec 2 500 à
  6 000 or sur la partie (environ 4 000 en Normal d'après le GDD), on achète **37 à 53 améliorations**, plus jusqu'à une
  douzaine dans les coffres. **Soit 45 à 65 améliorations pour 23 différentes** : on finit avec presque toutes, plusieurs
  fois chacune. Les causes 2 et 3 se renforcent : la fin de partie converge forcément vers « tout ». (Le raccourci **F** de
  la démo, 2 exemplaires de chaque, en donne 46 : à peu près une fin de partie ordinaire.)

### 2.3 Deux conséquences à garder en tête

- **La courbe des ennemis compense ce flot** : depuis le 17 octobre, les *Wave Settings* multiplient les PV par ×0,85 à
  ×2,85 des vagues 1 à 5, parce que le joueur devenait trop fort à chaque boutique. Avec beaucoup moins d'améliorations, il
  faudra adoucir cette courbe (réglage du joueur, au test).
- **La performance sur le Quest** : multitir, écho et déluge se multiplient sans limite (jusqu'à 250 flèches en vol, réglage
  de la scène), et chaque flèche tire ses effets (explosion, éclair) : c'est ce qui fait ramer la vague 10
  (`Audit_Performance_Quest.md`, partie 8). Des améliorations moins nombreuses et uniques, c'est aussi moins de flèches et
  de particules.

### 2.4 Le code

- `Upgrades/Upgrade.cs` : une amélioration = un effet (`UpgradeEffect`, 21 valeurs) et une valeur qui se cumule.
- `Upgrades/PlayerUpgrades.cs` : additionne les exemplaires par effet (rien ne se retire), pousse les statistiques vers
  l'arc, les PV et l'or ; gère les bonus temporaires des coffres.
- `Upgrades/SpecialArrows.cs` : au tir (`Bow.ShotFired` : multitir, écho), en vol (auto-visée, division du déluge), à
  l'impact (`Arrow.AnyHit` : foudre, explosion, glace, ricochet). Les failles du katana lui empruntent ses effets
  (`Combat/MatrixRift.cs`).
- `Shop/ShopManager.cs` (tirage par rareté), `Chests/ChestReward.cs` (2 améliorations et 1 bonus), `Core/DemoBoost.cs` (F).
- Déjà là pour la suite : `Boss.Defeated`, `Enemy.Killed`, la flèche plantée (`Arrow.StickInto`, 10 s de vie), le temps
  ralenti (coffres, katana), la gravité de chaque flèche (`GravityScale`), l'aide à la visée en ligne droite (Difficile),
  la flèche qui fait éclater la grenade en vol (Mega-bomba).

## 3. Ce qu'on prend à Isaac

Cinq principes, qui valent pour toutes les propositions :
1. **Un objet change une règle du tir, pas un chiffre.** Dans Isaac, *Spoon Bender* rend toutes les larmes chercheuses,
   *My Reflection* les fait revenir, *Ipecac* les rend explosives et en cloche. Ce sont des règles, toujours actives, qui
   se voient.
2. **Peu d'objets, chacun un événement.** Une dizaine par partie, pris une seule fois. On se souvient de « la partie avec
   le boomerang ».
3. **Les combinaisons naissent toutes seules.** Chaque objet touche une seule propriété du tir (sa trajectoire, son impact,
   ce qu'il fait une fois planté…). Comme ces propriétés sont indépendantes, deux objets se combinent sans qu'on ait prévu
   leur rencontre : la larme chercheuse qui revient, et qui explose.
4. **Le geste plutôt que le dé.** Remplacer « 25 % de chance » par une condition que le joueur provoque : un tir parfait, un
   tir à la tête, un tir en l'air, l'arc incliné, une flèche plantée. En VR, le plaisir vient du geste : l'objet doit le
   récompenser.
5. **On voit son build.** La pointe, l'empennage, la traînée, la corde, une breloque à l'arc : en prenant une flèche dans
   le carquois, on voit sa partie.

## 4. Le socle commun (quelle que soit la proposition)

- **Les statistiques à part, en 3 parchemins** (comme les parchemins de *Dead Cells*) : *Force* (dégâts et vitesse des
  flèches : remplace Dégâts, Puissance, Force du géant, Flèches rapides), *Adresse* (vitesse et bandes de l'anneau : Charge
  rapide, Précision), *Vigueur* (PV max : Vitalité). Ils suivent la montée des ennemis sans se mêler à l'identité du tir.
  En boutique, une seule carte « Parchemin » (on choisit lequel), présente à chaque pause, de plus en plus chère.
- **Les objets deviennent rares et uniques** :
  - **chaque boss de vague laisse un coffre de boss** : au début de la pause, il tombe en haut de la tour, avec 2 objets
    au choix. 5 par partie, au moment le plus fort de la vague ;
  - **la boutique** propose 1 objet, cher (de l'ordre de la moitié de la récompense de la vague), à côté du parchemin et
    des services de la tour ;
  - **les coffres de vague** : une orbe d'objet dans un coffre sur trois environ ; sinon un parchemin, un bonus temporaire,
    de l'or ou des matériaux ;
  - les épreuves secrètes donnent déjà une arme ; elles pourraient aussi donner un objet rare (à voir avec le lore) ;
  - **au total, 7 à 10 objets par partie** (au lieu de 45 à 65 améliorations), tirés dans une réserve de 20 à 30 : on en
    voit environ un tiers à chaque partie.
- **Les effets deviennent sûrs et liés au geste** (principe 4).
- **Butin et Chance quittent la boutique** : ils ne changent pas le tir, et avec moins de cartes la Chance perd son sens.
- **La courbe des ennemis** est à reprendre au test.

## 5. Proposition A : les reliques (Isaac pur)

- Tous les objets sont des **reliques** uniques, qui s'accumulent librement : 7 à 10 par partie.
- Chaque relique touche une propriété du tir (trajectoire, impact, planté, volée, règle du jeu) et toutes se combinent.
  Une règle de priorité tranche quand deux reliques touchent la même chose (par exemple, la trajectoire prise en dernier
  l'emporte).
- **Des transformations**, comme dans Isaac : 3 reliques d'une même famille (Tempête, Givre, Chasseur, Ingénieur…)
  transforment l'arc (apparence et pouvoir en plus).
- **Pour** : le plus proche d'Isaac, la plus grande surprise ; chaque relique ajoutée enrichit le jeu.
- **Contre** : beaucoup de conflits à régler ; avec 10 reliques à la fois, le tir devient difficile à lire dans le casque et
  lourd pour le Quest ; on retombe en partie dans « tout s'empile ».

## 6. Proposition B : la flèche en pièces (recommandée)

L'idée : **on fabrique sa flèche**. Chaque objet est une pièce d'une partie de la flèche ou de l'arc, et chaque partie n'a
qu'un emplacement.

| Emplacement | Ce qu'il change | Où on le voit |
|---|---|---|
| **Pointe** | ce que fait la flèche à l'impact | la pointe (forme, couleur, lueur) |
| **Empennage** | sa trajectoire | les plumes et la traînée |
| **Fût** | ce qu'elle fait en vol, puis une fois plantée | des runes qui brillent le long du fût |
| **Corde** | la façon de tirer (volée, rythme, temps) | la couleur et la vibration de la corde |
| **Talisman** | une règle du jeu, souvent à double tranchant | une breloque qui pend à l'arc |

- **Une pièce par emplacement.** Une nouvelle pièce pour un emplacement occupé : on la prend (l'ancienne est perdue) ou on
  la laisse. Chaque prise est une vraie décision, et deux parties ne finissent pas pareil.
- **La même pièce une deuxième ou une troisième fois la fait monter de niveau** (II, III : plus de flèches, un plus grand
  rayon…). Les doublons servent, et c'est un deuxième axe de puissance, avec les parchemins.
- **Quelques pièces rares cassent la règle** : *Double encoche* (un deuxième emplacement au choix)… Ce sont les moments
  « Isaac » de la partie.
- En fin de partie, la flèche a 5 pièces : on la décrit en une phrase (« pointe de givre, empennage boomerang, fût aimant,
  corde triple, talisman d'acrobate ») et on la reconnaît. Avec 5 emplacements de 4 à 7 pièces, il y a des milliers de
  flèches possibles.
- **Pour** : elle règle les trois causes d'un coup ; elle se lit d'un coup d'œil dans le casque (on regarde sa flèche) ;
  le nombre d'effets en même temps est plafonné (bon pour le Quest) ; elle se code sur les étapes qui existent déjà dans
  `SpecialArrows` (au tir, en vol, à l'impact). Elle peut s'ouvrir plus tard vers A, avec plus de pièces qui cassent la
  règle.
- **Contre** : moins de chaos qu'Isaac (5 effets à la fois, 6 avec une pièce rare).

## 7. Proposition C : la retouche (rapide)

Sans nouveau contenu : un seul exemplaire pour chaque effet (`maxStacks`, que le code gère déjà), les effets au tir parfait
au lieu du hasard, les 3 parchemins, le coffre de boss, 3 cartes en boutique dont 1 objet. Environ une journée, surtout des
données (par un baker). Elle ne règle pas la cause 3 (trop peu d'améliorations différentes), mais elle permet de sentir au
casque « plus rare, plus marquant » avant d'aller plus loin. **C'est la première étape de A comme de B.**

## 8. Le catalogue

Pensé pour un arc en VR. L'emplacement est celui de la proposition B (dans A, ce sont des reliques). Coût : **P** = petit
(quelques heures, le code existe presque), **M** = moyen (une demi-journée à une journée). 30 pièces : 12 tirées des
améliorations actuelles (souvent transformées), 18 nouvelles. Tout n'est pas à faire d'un coup (étape 3 : un premier lot
de 8).

### Pointe (à l'impact)

| Pièce | Effet | Vient de | Coût |
|---|---|---|---|
| Pointe d'orage | chaque **tir parfait** appelle la foudre, qui rebondit sur 3 ennemis proches | Flèche de foudre et Chaîne d'éclairs | P |
| Pointe à mèche | la flèche plantée **crépite, puis explose 1 s plus tard** (dans l'ennemi ou au sol) : on tire devant un groupe | Flèche explosive | P |
| Pointe de givre | un tir parfait **gèle** l'ennemi 1,5 s ; un ennemi gelé touché **éclate** en éclats de glace qui blessent autour | Flèche de glace | M |
| Pointe éclatée | à l'impact, la flèche se brise en 4 éclats qui continuent derrière la cible | nouvelle | P |
| Pointe perforante | traverse tous les ennemis alignés (−15 % de dégâts à chacun) | Perçage | P |
| Pointe de chasseur | la flèche **marque** sa cible ; tout ce qui touche un ennemi marqué fait ×2, et à sa mort la marque passe à l'ennemi le plus proche | Chasseur de têtes (en partie) | P |
| Pointe sangsue | un tir à la tête qui tue rend des PV, et une orbe de vie tombe | Vampirisme | P |

### Empennage (la trajectoire)

| Pièce | Effet | Vient de | Coût |
|---|---|---|---|
| Empennage chercheur | la flèche vire vers l'ennemi proche, vers sa tête ou son point faible | Auto-visée (gardée) | existe |
| Empennage boomerang | à 25 m, la flèche fait demi-tour et revient : elle touche à l'aller et au retour ; **rattrapée à la main, elle s'encoche aussitôt**, et le tir suivant est parfait | nouvelle | M |
| Empennage télékinésie | **tant qu'on garde la gâchette de la main de l'arc après le lâcher, la flèche suit la direction où pointe l'arc** (elle vire doucement) : on la pilote | idée des « flèches volantes » (GDD, section 23) | M |
| Empennage rebond | la flèche **rebondit** deux fois sur le sol et les murs au lieu de se planter : on tire au sol devant un groupe, on contourne un rocher | Tir ricochet (devient le rebond sur le décor) | M |
| Empennage plume | plus de chute : la flèche file droit, sans limite de portée, et l'aide à la visée devient une ligne droite | Flèches rapides | P |
| Empennage de plomb | flèches lourdes, en cloche ; au sol, l'impact fait une onde de choc ; l'aide à la visée montre le point de chute | nouvelle (cousine de l'arc légendaire) | P |
| Empennage spectral | la flèche traverse le décor (falaises, barricades), pas les ennemis | nouvelle | P |

### Fût (en vol, puis planté)

| Pièce | Effet | Vient de | Coût |
|---|---|---|---|
| Fût aimant | la flèche plantée **attire les ennemis** à 6 m pendant 2,5 s | nouvelle | M |
| Fût conducteur | deux flèches plantées à moins de 8 m l'une de l'autre sont **reliées par un arc électrique** pendant 6 s, qui blesse ce qui le traverse : deux tirs de part et d'autre d'une voie font une clôture (4 liens au plus) | nouvelle | M |
| Fût-pieu | une flèche plantée au sol **devient un pieu** qui blesse les ennemis qui passent dessus pendant 8 s (6 au plus) : on sème les voies, en lien avec le tower defense | nouvelle | M |
| Fût détonateur | **tirer sur une de ses flèches plantées la fait éclater** en 6 flèches, comme la Mega-bomba de la grenade : un tir de billard | nouvelle | P à M |
| Fût suspendu | la flèche **s'arrête en l'air** 0,3 s après le départ et attend (3 s au plus) ; **fermer le poing de la main libre les relâche toutes** : on prépare une salve | nouvelle | M |
| Fût fragmenté | en vol, la flèche se divise toujours en 3 | Déluge | existe |

### Corde (la façon de tirer)

| Pièce | Effet | Vient de | Coût |
|---|---|---|---|
| Corde triple | 3 flèches en éventail **dans le plan de l'arc** : l'arc droit, l'éventail est à plat (contre les groupes) ; l'arc couché, il est vertical (contre les volants, ou une grande cible) | Multitir | P |
| Corde d'écho | un tir parfait est **répété 0,4 s plus tard, dans la direction visée à ce moment-là** : on enchaîne deux cibles d'un seul tir | Tir écho | P |
| Corde de transe | chaque tir parfait d'affilée donne +15 % de dégâts et un anneau plus rapide (jusqu'à 5) ; au cinquième, la flèche est dorée et traverse tout ; un tir qui n'est pas parfait remet à zéro | nouvelle | P |
| Corde du faucon | à pleine tension, **le temps ralentit** (×0,4 pendant 1,5 s, une fois toutes les 8 s) | nouvelle (le ralenti existe) | P |

### Talisman (une règle du jeu, souvent à double tranchant)

| Pièce | Effet | Coût |
|---|---|---|
| Acrobate | en l'air (saut aux bras) et en glissade, le temps ralentit et chaque tir est parfait | P |
| Œil du sniper | +2 % de dégâts par mètre au-delà de 15 m, mais −50 % sous 10 m | P |
| Pacte de verre | dégâts ×2, mais chaque tir raté (rouge) coûte 5 PV | P |
| Ingénieur | une flèche qui touche ta baliste ou ta tour de givre la **surcharge** 5 s (cadence ×2, portée +50 %) ; un tir parfait dans une barricade la répare un peu | M |
| Duelliste | les coups de flèche au corps à corps font ×4 et portent la pointe ; chaque coup réussi rend le tir suivant parfait | P |
| Bourreau | un tir à la tête sur un ennemi sous 25 % de ses PV le tue net, et sa tête explose | P |

### Ce que deviennent les 23 améliorations actuelles

- **Dans les parchemins** : Dégâts, Puissance, Force du géant, Flèches rapides (Force) ; Charge rapide, Précision (Adresse) ;
  Vitalité (Vigueur).
- **Transformées en pièces** : Multitir, Tir écho, Déluge, Perçage, Flèche de foudre et Chaîne d'éclairs (une seule
  pièce), Flèche de glace, Flèche explosive, Auto-visée, Tir ricochet, Vampirisme, Chasseur de têtes.
- **Retirées de la boutique** : Butin, Chance.
- **La grenade** : ses flèches portent déjà la pointe ; Grenade sauvage et Grenade rapide peuvent rester des cartes à part,
  ou devenir plus tard des pièces de grenade (*Bombe collante*, *Bombe aimant*).

## 9. Les synergies

### Les combinaisons qui naissent toutes seules

Dans B, elles se font entre emplacements, donc elles sont toujours possibles ensemble.

| Combinaison | Ce que devient le tir |
|---|---|
| Fût aimant + Pointe à mèche | **le trou noir** : la flèche aspire le groupe, puis le fait sauter |
| Empennage boomerang + Pointe perforante | **la faux** : toute la file touchée à l'aller, puis au retour |
| Empennage télékinésie + Fût fragmenté | **la bombe à sous-munitions** : on la pilote au-dessus du groupe, elle s'y divise |
| Corde triple + Fût conducteur | **la clôture d'un seul tir** : trois flèches plantées, trois arcs électriques |
| Empennage plume + Pointe perforante + Corde de transe | **le rail** : une ligne droite dorée qui traverse toute une voie |
| Fût suspendu + Empennage chercheur + Corde triple | **l'essaim** : des dizaines de flèches en l'air, lâchées d'un poing fermé, qui partent seules sur les ennemis |
| Pointe de givre + Corde d'écho | geler puis briser d'un seul tir |
| Empennage rebond + Pointe à mèche | **le lance-grenades** : la flèche rebondit au sol et explose au milieu du groupe |
| Empennage de plomb + Fût-pieu | **le champ de pieux** : depuis la tour, une pluie de pieux sur la voie |
| Corde du faucon + Empennage télékinésie | piloter sa flèche au ralenti |
| Talisman acrobate + Corde triple | sauter au-dessus d'un groupe et l'arroser au ralenti |

### Les duos nommés

Quelques paires reconnues par le jeu, comme les duos de *Hades* ou les transformations d'Isaac : avec les deux pièces, l'arc
change d'apparence et le duo ajoute un petit pouvoir. La boutique et les orbes l'annoncent (« Duo possible : Trou noir »),
ce qui donne envie de construire sa flèche.
- **Trou noir** (Fût aimant + Pointe à mèche) : l'explosion aspire encore une seconde, et son rayon double.
- **Tempête** (Pointe d'orage + Fût conducteur) : les arcs électriques appellent la foudre sur ce qui les traverse.
- **Faux** (Empennage boomerang + Pointe perforante) : la flèche fait deux allers-retours.
- **Essaim** (Fût suspendu + Empennage chercheur) : les flèches suspendues tournent autour du joueur et partent seules sur
  l'ennemi qui approche.
- **Blizzard** (Pointe de givre + Corde triple) : un ennemi gelé gèle ses voisins.
- **Œil de faucon** (Corde du faucon + Œil du sniper) : pendant le ralenti, les points faibles brillent à travers le décor.

## 10. Les armes

- **Arc de chasse, arcs de classe, Akimbow, arc légendaire** : leurs tirs sont des flèches (`Arrow`), tout s'applique. Avec
  les Akimbow, la corde triple suit l'inclinaison de chaque arbalète ; la flèche boomerang qui revient se recharge seule
  (les mains tiennent les arbalètes) ; la corde du faucon est commune aux deux.
- **Arc légendaire** : la pointe s'applique à la flèche principale, pas aux 10 flèches de la pluie (sinon dix fois les
  effets : trop lourd pour le Quest).
- **Arc cosmique** : la pointe s'applique aux ennemis touchés par le laser ; l'empennage et le fût, à ses flèches normales.
- **Katana** : la faille emprunte déjà les effets des flèches. La pointe s'applique à son impact (`SpecialArrows.Impact`) ;
  l'empennage chercheur existe, le boomerang et la télékinésie s'adaptent (la faille vole déjà vers sa cible) ; le fût ne
  sert d'abord qu'aux flèches (la faille ne se plante pas ; plus tard, elle pourrait laisser un éclat planté en se
  refermant) ; la corde triple et la corde d'écho existent déjà pour les failles, la transe compterait les failles
  parfaites d'affilée.
- **Grenade** : ses flèches portent la pointe, sans les effets du fût (pour la performance).

## 11. La performance sur le Quest

- Moins d'effets à la fois : 5 pièces au plus dans B, et plus de multiplication sans limite (multitir × écho × déluge). Le
  plafond de 250 flèches en vol reste.
- Les nouveautés restent légères : arcs électriques en `LineRenderer` (4 au plus), pieux et aimants limités (6 et 3),
  explosions réutilisées (à faire de toute façon : c'est un suspect de l'audit) ; le ralenti ne coûte rien.
- Avant de valider, mesurer avec `PerfProbe` une partie avec les combinaisons les plus chargées (essaim, clôture).

## 12. Coût et ordre de travail

Pour B ; A suit les mêmes étapes, sans les emplacements. Coûts estimés en temps de code (avec Claude), sans les tests.

| Étape | Contenu | Coût | À tester au casque |
|---|---|---|---|
| 1. Moins, mais mieux (la proposition C) | les parchemins ; un exemplaire par effet ; les effets au tir parfait ; le coffre de boss (`Boss.Defeated`, un coffre à 2 orbes d'objet en haut de la tour) ; 3 cartes en boutique, dont 1 objet ; les coffres de vague à une orbe d'objet au plus. Par un baker, sans toucher aux valeurs réglées par le joueur | 1 jour | « plus rare, plus marquant » ? la courbe des ennemis |
| 2. Les emplacements | `Upgrade` : emplacement, niveau, réserve (boss, coffre, boutique) ; `PlayerUpgrades` : retirer, remplacer, monter de niveau ; cartes et orbes qui montrent l'emplacement et « remplace … » ; la montre liste les 5 pièces | 2 jours | choisir et remplacer se comprend-il ? |
| 3. Premier lot de 8 pièces | au moins une par emplacement, et qui se combinent : Corde triple, Empennage boomerang, Empennage télékinésie, Fût aimant, Fût conducteur, Pointe à mèche, Pointe de givre, Talisman acrobate | 3 à 4 jours | chaque pièce, puis les combinaisons |
| 4. L'arc qui raconte la partie | la pointe, l'empennage, les runes du fût, la corde, la breloque ; un son par pièce | 1 à 2 jours | lisible d'un coup d'œil ? |
| 5. Duos et deuxième lot | les 6 duos, le reste du catalogue, les failles du katana | 3 à 4 jours | |
| 6. Plus tard | des pièces débloquées par les succès (comme Isaac débloque ses objets ; `memory.json` existe), une réserve propre à chaque source, une pièce moins souvent proposée si on l'a prise à la partie d'avant | 1 à 2 jours | |

À chaque étape : le GDD (sections 6, 7 et 13, avec la date), `Checklist_Demo.md`, et les raccourcis (`DemoBoost` : F
donnerait une pièce par emplacement ; un raccourci ferait défiler les pièces d'un emplacement, pour essayer les
combinaisons ; à ajouter à `Demo_Vendredi.md`).

Points techniques :
- `UpgradeEffect` est enregistré par son numéro dans l'asset : ajouter les nouveaux effets **à la fin** de l'énumération,
  sans en retirer ni en déplacer (sinon les cartes actuelles changent d'effet).
- Le catalogue se remplit par un baker qui ajoute les nouvelles pièces sans toucher aux valeurs réglées par le joueur
  (jamais « Remplir avec le GDD »).
- Les descriptions actuelles des cartes ne sont pas réécrites sans l'accord du joueur ; celles des nouvelles pièces
  ci-dessus sont des propositions.

## 13. À décider

1. A, B, ou un mélange ? (B est recommandée.)
2. Les parchemins : trois (Force, Adresse, Vigueur) ? En boutique seulement, ou aussi dans les coffres ?
3. Le coffre de boss : 2 objets au choix, ou 3 ?
4. Le premier lot de pièces : la liste de l'étape 3 est une proposition.
5. Une pièce prise sur un emplacement occupé : l'ancienne est perdue, ou revendue (or, parchemin) ? **Réponse du joueur (26 octobre)** : ni l'un ni
   l'autre, elle attend dans l'atelier, où l'on choisit la pièce équipée.

## 14. Propositions du 26 octobre au soir (demandées par le joueur, après ses réglages)

Des propositions de Claude. **Le joueur les a presque toutes gardées le soir même, et elles sont codées** (partie
« Avancement ») : 14.1 à 14.4 telles quelles, 14.5 en partie (voir « Avancement »). Les nombres sont des points de départ.

### 14.1 Pourquoi on trouve une pointe une fois sur deux

La boutique tire d'abord une **rareté** (20 / 60 / 20 %), puis une pièce de cette rareté. Or la seule pièce commune est
**Chasseur de têtes, une pointe** : une carte sur cinq est donc toujours la même pointe. Avec les 8 pointes rares sur 23 rares,
une carte a environ **44 %** de chances d'être une pointe (11 pointes sur 36 pièces). Deux remèdes, qui s'additionnent :
- **tirer d'abord l'emplacement** (une chance sur cinq chacun), puis la rareté parmi les pièces de cet emplacement : chaque partie
  de la flèche revient aussi souvent, quel que soit son nombre de pièces (le plus efficace ; une demi-journée) ;
- **déplacer quelques pointes** (14.2) et donner des pièces communes aux autres emplacements.

### 14.2 Déplacer des pièces

| Pièce | De → vers | Pourquoi |
|---|---|---|
| Perçage | Pointe → **Fût** | traverser, c'est le fût qui passe ; le duo Faux (boomerang + perçage) reste possible |
| Flèche de glace | Pointe → **Fût** (« Fût givrant ») | la glace se pose au sol, comme un fût planté ; et elle se combine alors avec la pointe de givre |
| Vampirisme | Pointe → **Talisman** | une règle du jeu (se soigner), pas un impact ; dans la pointe, il n'est jamais pris |
| Chasseur de têtes | Pointe → **Corde** (« Corde tendue ») | la corde a le moins de pièces ; et elle n'aurait plus la seule commune |

Ensuite : pointe 7, empennage 7, fût 8, corde 6, talisman 8 (avec les pièces de 14.3 : 7, 9, 8, 9, 8).

### 14.3 Pièces nouvelles (surtout la corde et l'empennage)

| Pièce | Emplacement | Effet | Coût |
|---|---|---|---|
| Corde de siège | Corde | passé le vert, l'anneau ne baisse plus les dégâts, il les monte : garder la tension charge la flèche (jusqu'à ×2,5 et +3 ennemis traversés en 2 s), elle grossit et brille. L'inverse de la transe : la patience contre le rythme | P |
| Corde de pluie | Corde | un tir vers le ciel (plus de 45° vers le haut) éclate à son sommet en 8 flèches qui retombent en pluie autour du point visé (la pluie de l'arc légendaire, en petit) | P |
| Corde fantôme | Corde | dans la seconde après un tir, tirer la corde à vide et la lâcher décoche une flèche fantôme (60 % des dégâts, avec les pièces) : le geste rapide est récompensé | M |
| Empennage piqué | Empennage | tirée vers le haut, au sommet de sa course, la flèche repère l'ennemi le plus proche de son point de chute et pique dessus (dégâts ×1,5) : le tir en cloche devient une frappe | P |
| Empennage de rappel | Empennage | fermer le poing de la main libre rappelle toutes tes flèches plantées : elles reviennent vers toi en traversant les ennemis sur leur chemin (comme la hache de *God of War*) | M |
| Fût-sentinelle | Fût | plantée au sol, la flèche tire une petite flèche sur l'ennemi le plus proche chaque seconde pendant 5 s (4 au plus) : un pont avec la refonte des tourelles | M |
| Talisman de l'avare | Talisman | +1 % de dégâts par tranche de 20 or gardés (au plus +100 %) : dépenser, ou garder ? | P |

### 14.4 Duos nouveaux

Toujours entre deux emplacements différents (avec les déplacements de 14.2, ou sans quand c'est précisé).

| Duo | Pièces | Pouvoir |
|---|---|---|
| Orage magnétique | Fût aimant + Flèche de foudre | à la fin de l'aspiration, un éclair frappe chaque ennemi attiré |
| Lance-grenades | Empennage rebond + Pointe à mèche | une petite explosion à chaque rebond, puis la mèche |
| Supernova | Corde de transe + Flèche explosive | au 5ᵉ tir parfait d'affilée, la flèche dorée explose en supernova (rayon et dégâts ×2) |
| Chasse à l'homme | Pointe de chasseur + Talisman du bourreau | l'ennemi marqué s'exécute sous un seuil doublé, et la marque saute aussitôt |
| Pluie de fer | Empennage de plomb + Fût-pieu | chaque onde de choc du plomb plante 3 pieux autour d'elle |
| Billard | Fût détonateur + Tir ricochet | chaque flèche du détonateur ricoche une fois |
| Clôture de givre | Fût conducteur + Pointe de givre | les arcs électriques gèlent ce qui les traverse (1 s), prêt à éclater |
| Réseau | Fût conducteur + Talisman de l'ingénieur | un arc électrique peut relier une flèche plantée à ta baliste : elle reste surchargée tant que le lien dure |
| Étoile filante | Talisman d'acrobate + Corde triple | en l'air, la volée part en couronne vers le bas, tout autour du point visé |
| Rail | Empennage plume + Perçage | la flèche devient un trait de lumière, trois fois plus rapide, qui traverse tout et laisse une traînée qui blesse 1 s |
| Glacier (avec 14.2) | Fût givrant + Pointe de givre | un ennemi sur la glace gèle au premier tir, même pas parfait |

### 14.5 Ce qui semble déséquilibré

1. **L'explosion à chaque tir** (jusqu'à 200 % sur 6 m), surtout avec le multitir (4 flèches) ou l'écho : 4 à 8 explosions par tir
   sur un groupe. Elle écrase les autres pointes, et pèsera sur le Quest. Piste : 5 m au niveau 3, ou le bonus de niveau aux
   seuls tirs parfaits.
2. **Multitir et corde triple** sont maintenant presque la même pièce (deux cordes, à chaque tir), et la triple donne plus de
   flèches (3/5/7 contre 2/3/4). Piste : le multitir en **rafale** (les flèches partent l'une après l'autre, à 0,1 s, dans la
   direction visée à ce moment) pour qu'il ait son geste.
3. **Le déluge** (légendaire) ne joue plus qu'aux tirs parfaits, avec une flèche de plus au niveau 1 : moins fort que le multitir
   (rare, à chaque tir). Piste : 2/3/4 flèches, ou à chaque tir.
4. **Les dégâts se multiplient** : pacte de verre (×3), marque du chasseur (×3), chasseur de têtes (×2,5 à la tête), transe
   (+125 %) et la Force se cumulent en ×50 et plus. Et **les parchemins ne montent plus avec les vagues** alors que l'or, lui,
   monte : en fin de partie, la Force devient presque gratuite. Piste : *Own Scroll Price Increase* de 10 à 20 %.
5. **Le pacte de verre** ne coûte que sur un tir rouge : pour un bon tireur, ×2 à ×3 presque gratuits. Piste : le coût monte avec
   le niveau, ou il touche aussi les tirs orange.
6. **L'ingénieur à ×6 pendant 9 s**, relancé à chaque flèche : il suffit de tirer dans sa baliste toutes les 9 s pour qu'elle tire
   six fois plus vite tout le temps. Piste : une surcharge au plus toutes les 15 s par bâtiment, ou ×2/×3/×4.
7. **Le faucon sans recharge** ralentit le temps à chaque tension, et l'anneau ralentit avec lui : le tir parfait devient facile,
   donc tout ce qui joue au parfait (foudre, glace, givre, déluge, transe). C'est la pièce qui rend toutes les autres plus fortes.
   Piste : l'anneau garde sa vitesse pendant le ralenti.
8. **Le bourreau à 50 %** (et 20 % sur les boss), avec les dégâts de la flèche comptés : au niveau 3, toute tête à mi-vie meurt,
   et la fin des boss, MK compris, devient très courte. Piste : boss à 10/12/15 %.
9. **La super grenade au niveau 3** : 64 flèches par grenade, rechargée 70 % plus vite ; avec l'explosion à chaque tir, 64
   explosions. Lourd pour le Quest. Piste : les flèches de la grenade sans l'explosion de la pointe, ou 8/16/24.
10. **Le vampirisme** (2/4/6 PV par tir à la tête) ne pèse rien face aux autres pointes : il n'est jamais pris (14.2 : talisman).
11. **La courbe des ennemis** (PV ×0,85 à ×2,85 des vagues 1 à 5) avait été faite pour l'ancien flot d'améliorations : à revoir
    au casque, une fois ces réglages essayés.

### 14.6 Textes proposés pour les anciennes cartes

Leurs descriptions parlent encore de pourcentages de chance. Le joueur les réécrit ; propositions (la carte dit déjà « tir
parfait » dans son sous-titre quand il le faut) :

| Carte | Texte proposé |
|---|---|
| Flèche de foudre | La foudre frappe la cible (50 % des dégâts) |
| Chaîne d'éclairs | Un éclair qui rebondit sur 3 ennemis (25 % des dégâts chacun) |
| Flèche de glace | Le sol gèle sous la cible : 40 % plus lents |
| Flèche explosive | Chaque flèche explose (4 m) |
| Perçage | La flèche traverse 2 ennemis |
| Chasseur de têtes | +50 % de dégâts à la tête |
| Déluge | En vol, la flèche se divise en deux |
| Multitir | 2 flèches à chaque tir |
| Tir écho | 50 % de chance que la flèche se répète, là où elle est |

## 15. Les pièces qui ne marchent pas ensemble (27 octobre, demandé par le joueur)

Relevé en relisant le code (pas encore au casque). Les pièces d'un même emplacement ne se combinent jamais : les cas
ci-dessous sont entre emplacements différents. Pour chacun, un duo qui les ferait marcher ensemble ; **le joueur les a tous
choisis le 27 octobre, et ils sont codés** (GDD, section 6). Correction : la mèche, l'aimant et les arcs du conducteur
restaient déjà en place après un rappel ; seuls la sentinelle, le pieu et le détonateur s'effaçaient.

| Pièces | Ce qui coince | Duo proposé |
|---|---|---|
| Empennage boomerang + fûts qui agissent plantés (sentinelle, pieu, conducteur, aimant, détonateur) et Pointe à mèche | la flèche boomerang ne se plante presque jamais : le décor la renvoie, et elle ne se plante dans un ennemi qu'une fois son perçage épuisé (la sentinelle, le pieu et le conducteur exigent le décor : jamais) | **codés le 27 octobre**, comme pour le rebond, un effet **au demi-tour** : « Retour de flamme » (mèche : explosion), « Tourbillon » (aimant au sol sous la flèche), « Escorte » (petites flèches de sentinelle), « Fil électrique » (un arc du demi-tour au point de départ), « Semeur » (trois pieux au sol), « Mitraille » (une volée du détonateur) |
| Empennage plume + Corde de pluie | presque sans gravité, une flèche tirée vers le ciel n'atteint jamais son sommet : pas de pluie (elle se perd en l'air) | « Nuage » : la flèche plume s'arrête dans le ciel au bout d'1 s et y fait pleuvoir des flèches pendant 2 à 3 s |
| Empennage de rappel + pièces plantées (mèche, aimant, conducteur, pieu, détonateur, sentinelle) | rappeler les flèches plantées efface leur effet en cours (une mèche qui n'a pas explosé, un détonateur jamais déclenché, une sentinelle qui tirait encore) | « Retour explosif » (rappel + mèche ou détonateur : la flèche rappelée explose ou détone là où elle était, puis revient) ; pour les autres, un simple réglage : l'effet reste en place après le rappel |
| Fût suspendu + Corde de pluie | la pluie éclate quand la flèche ne monte plus : arrêtée en l'air, la flèche suspendue éclate tout de suite, au mauvais endroit | « Averse » : relâchées, les flèches suspendues éclatent chacune en une petite pluie sur le point visé |
| Corde de pluie + Empennage boomerang ou rebond | pas cassé, mais lourd : jusqu'à 16 flèches de pluie (64 avec l'arc légendaire) qui reviennent vers le joueur ou rebondissent 7 fois ; le plafond de flèches suivies (250) protège le Quest | à voir au casque : si c'est trop, les flèches de pluie ne reviennent pas et ne rebondissent pas |
| Corde de siège + Akimbow ou Katana | la charge n'existe que pour l'arc (et le laser de l'arc cosmique) | aucun duo : depuis le 27 octobre (demandé par le joueur), la boutique et les coffres ne la proposent pas avec ces armes |
| Corde fantôme + Akimbow ou Katana | la fenêtre ne s'ouvre qu'après un tir de l'arc | aucun duo : depuis le 27 octobre, la boutique et les coffres ne la proposent pas avec ces armes |

Ce qui a été **corrigé** plutôt que proposé (GDD, section 6, « Corrections du 27 octobre ») : les flèches de pluie sans les
pièces, l'ingénieur sans effet sur les bâtiments, le rappel sans dégâts, le piqué introuvable, le ricochet qui ratait les
ennemis en marche, le billard au hasard, l'étoile filante invisible, Glacier masqué par le tir parfait, la charge du siège
perdue quand un tir devient parfait d'office, le rebond limité à la mèche (six duos).
