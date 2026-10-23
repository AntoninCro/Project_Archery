# Reprise du projet (23 octobre)

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

**Pas encore commité** (23 octobre, après-midi ; fais un commit avant de changer de PC) : la refonte du katana (les failles,
partie 2), le gong et les cibles déclenchables au katana, la confirmation du bouton « Quitter » à la main, la correction de
`Tools/compile_check.py` (il ne trouvait pas les Starter Assets d'XRI), le retrait de `CLAUDE.md` du dépôt (il reste sur le PC,
et il est dans le `.gitignore`), et la documentation (GDD, README, checklist, démo, ce document).

## 2. La refonte du katana (faite le 23 octobre, à tester au casque)

Les symboles kanji sont retirés ; le gantelet et la faille dans la matrice restent (GDD, section 5, paragraphe « Refait le
23 octobre, après les tests : les failles »). Choix du joueur : on frappe la faille avec le gantelet, la qualité vient de sa
fraîcheur, 3 failles ouvertes au plus ; la coupe se mesure par rapport au joueur et doit être une vraie taille ; la faille
s'ouvre comme un œil (une fine fissure, puis d'un coup).

- **La coupe** : la pointe à 3,5 m/s, au moins 40 cm de geste et 30 cm entre son début et sa fin, devant le joueur, 0,25 s après
  la faille précédente ; tout est mesuré depuis la poitrine du joueur, dans le repère du XR Origin (se déplacer ne coupe rien).
  La faille suit le point de la lame qui passe à 62 cm de la poitrine : elle s'ouvre à portée du gantelet.
- **La faille** : fine fissure 0,14 s, puis ouverture en 0,1 s ; bords qui ondulent, tracé qui frémit, fissures secondaires qui
  rampent ; elle vit 2,2 s ; ses bords prennent la couleur de la qualité du moment (vert sous 0,4 s, orange sous 1 s, rouge).
- **Le lancer** : un coup de poing du gantelet (1,3 m/s, paume à moins de 40 cm) ; elle part vers l'ennemi le plus proche du
  centre de la vue et toute la faille touche.
- **Le gong** : la faille et la lame déclenchent aussi ce qui réagit aux flèches (gong, cibles d'entraînement, cibles de la
  victoire, boules de feu), sauf les menus et les panneaux de difficulté.

| Fichier | Rôle |
|---|---|
| `Scripts/Combat/KatanaGauntlet.cs` | L'arme : le katana flottant (prise, coup au corps à corps, coupe des projectiles, gong), le gantelet, la coupe, les failles ouvertes (fraîcheur, dérive), le coup de poing, le lancer, le jeu au clavier, le raccourci **O** |
| `Scripts/Combat/MatrixRift.cs` | Une faille : son plan face au joueur, son ouverture en œil, sa couleur, le vol vers la cible et la zone de touche, le gong et les cibles qu'elle traverse (ancien `MatrixSign.cs`) |
| `Scripts/Combat/MatrixRiftMesh.cs` | Le squelette de la faille (le tracé et les fissures secondaires) ; sa forme vient du shader (ancien `MatrixCrackMesh.cs`) |
| `Shaders/MatrixTear.shader` | La faille vivante : profil de plaie, ouverture (`_Open`), bords et tracé animés dans le vertex shader ; dedans, le vide et les 0 et 1 verts |
| `Shaders/KatanaBlade.shader` | La lame néon qui bugue |
| `Editor/KatanaBaker.cs` | Les prefabs, les matériaux, l'aperçu du menu, la fiche `Data/BowClasses/Bow_Katana.asset` et son texte |
| `Player/GlovedHand.cs` | `For`, `WristFrame`, `SetGloveHidden` : le gantelet se pose sur la main gantée |

**Si quelque chose ne va pas au test**, les réglages sont des constantes en tête de `KatanaGauntlet.cs` (seuils de la coupe,
portée et vitesse du coup de poing, fenêtres de fraîcheur, durée de vie) et de `MatrixRift.cs` (largeur, ouverture en œil),
et des propriétés du matériau `Art/Katana/Dechirure` (bords qui ondulent, déchiquetés, tracé qui frémit, couleurs).

L'ancienne version aux traces libres (katana et bouclier) est dans Git, au commit `90d5f83` ; celle aux symboles, au commit
`ccbe156`.

## 3. Les autres choses à faire

Dans l'ordre conseillé (aussi dans le GDD, section 21, « À faire ensuite ») :
1. **La présentation** (PowerPoint) pour l'oral, et les répétitions (`Demo_Vendredi.md` : pitch, déroulé, raccourcis).
2. Les tests au casque des nouveautés (`Checklist_Demo.md`).
3. Tester au casque la refonte du katana (partie 2) et la régler.
4. Le build Windows en *Development Build* (les raccourcis de démo y marchent).
5. La vidéo `Tir_3` du tutoriel (la grenade), les crédits des musiques de l'ami.
6. Le ménage des fichiers inutilisés (liste dans le GDD, section 21).
7. Plus tard : le mode démo en tutoriel interactif, les idées du lore (`Lore.md`, GDD section 24), refaire la ville du toit
   cyberpunk (le joueur s'en charge).

## 4. Pour la présentation

- **Le pitch** et les **trois idées à retenir** : `Demo_Vendredi.md`, parties 2 et 3 (déroulé de 10 minutes, version courte de 5).
- **Les consignes du cours et ce qui y répond** : le tableau « Fonctionnalités » du README, et le GDD, section 20.
- **Les chiffres** : 6 vagues (la 6e : le dragon MK) ; un boss par vague ; 4 difficultés, chacune avec son ciel ; 23 améliorations
  de 3 raretés ; 12 socles, 4 bâtiments à 3 niveaux ; 2 secrets ; armes : arc de chasse, arcs court, puissant et runique, Akimbow,
  katana et gantelet, arc cosmique, arc légendaire, grenade à flèches.
- **Ce qui est technique et montrable** : l'arène du dragon construite par le code depuis un plan texte ; les mains gantées
  animées par le code (squelette ajouté au gant) ; la course aux bras, le slide et le saut aux bras ; les shaders faits pour le jeu
  (lame néon qui bugue, faille Matrix, surface de l'arc cosmique, arbalètes cyberpunk) ; les bakers d'éditeur qui fabriquent
  modèles, matériaux et prefabs.
- **L'équipe et les crédits** : README (parties « Équipe » et « Crédits ») et `Credits_Licences.md` (tous les assets et leurs
  licences ; les modèles en CC-BY doivent être crédités).
- **Captures** : aucune capture d'écran n'est dans le projet ; en prendre dans Unity (vue Game) ou dans le casque.
