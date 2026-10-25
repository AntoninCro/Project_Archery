# Reprise du projet (24 octobre)

Ce document fait le point pour reprendre le projet ailleurs (autre compte Claude, autre PC, coéquipier). Il complète le GDD
(`GDD.md`, la référence, à tenir à jour) et les règles de travail (`../../CLAUDE.md`).

## 1. Où en est le jeu

THUNK VR est complet et jouable de la première vague au dragon : tir à l'arc physique avec anneau de timing, 6 vagues (accalmie,
montée, boss), tower defense (cristaux, 12 socles, 4 bâtiments à 3 niveaux, marteau), boutique roguelike (23 améliorations),
coffres, 4 difficultés avec leur ciel, menu des armes et succès, deux secrets (portail des étoiles, toit cyberpunk), le dragon MK
dans son antre, mode infini, classement, tutoriel en vidéos, menu en cours de partie avec pause, jeu au clavier sans casque.

**Testé au casque** : le cœur du jeu (les guides montés et testés jusqu'au 13 octobre, voir `Feuille_de_route.md`), puis au fil
des retours du joueur une partie des nouveautés (la grenade, les Akimbow, le portail, le menu en cours de partie).
**Pas encore vérifié** : les cases non cochées de `Checklist_Demo.md` (la passe de tests complète n'a pas été faite), à commencer
par la liste « Nouveautés du 19 au 23 octobre », en tête de la partie 1.

**Pas encore commité** (24 octobre ; fais un commit avant de changer de PC) : la 3e version du katana (partie 2), le toit
cyberpunk refait (décor 3, épreuve rendue faisable), le dossier `Presentation/` (le PowerPoint de l'oral, son PDF et son
script), et la documentation (GDD, README, checklist, démo, crédits, ce document). La 2e version du katana (les failles qui
s'ouvrent comme un œil) est commitée (`f506918`, 23 octobre).

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

Dans l'ordre conseillé (aussi dans le GDD, section 21, « À faire ensuite ») :
1. **La présentation** : faite le 23 octobre (partie 4) ; il reste à y mettre les captures, puis les répétitions
   (`Demo_Vendredi.md` : pitch, déroulé, raccourcis).
2. Les tests au casque des nouveautés (`Checklist_Demo.md`).
3. Tester au casque le katana, 3e version (partie 2), et le régler.
4. Le build Windows en *Development Build* (les raccourcis de démo y marchent).
5. La vidéo `Tir_3` du tutoriel (la grenade), les crédits des musiques de l'ami.
6. Le ménage des fichiers inutilisés (liste dans le GDD, section 21).
7. Tester au casque le toit cyberpunk refait le 24 octobre (décor 4 et épreuve rendue faisable : GDD section 23). Les
   réglages de l'épreuve sont sur l'objet « Toit cyberpunk » de la scène `Dimension_Cyberpunk` ; le décor est fait par
   `Editor/CyberDimensionBaker.cs` (la scène est refaite toute seule quand sa version change : la refaire efface ce qu'on y
   a changé à la main).
8. Plus tard : le mode démo en tutoriel interactif, les idées du lore (`Lore.md`, GDD section 24).

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
