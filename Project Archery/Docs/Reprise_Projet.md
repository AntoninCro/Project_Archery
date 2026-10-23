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

**Pas encore commité** au moment d'écrire (fais un commit avant de changer de PC) : la refonte du katana (scripts
`Scripts/Combat/KatanaGauntlet.cs`, `KanjiGlyphs.cs`, `KanjiGuide.cs`, `MatrixSign.cs`, `MatrixCrackMesh.cs`, shaders
`KatanaBlade`, `KanjiBrush`, `MatrixTear` refait, `Editor/KatanaBaker.cs`, `Player/GlovedHand.cs`, `Bows/BowDefinition.cs`,
`Bows/Bow.cs`), les modèles et sons téléchargés pour lui (`Assets/Katana by Pichuliru - 15A7InejC7`,
`Assets/energy-gauntlet-low-poly-free-download`, `Assets/507466__danjocross__clean-fast-swoosh.aiff`,
`Assets/650564__h2p34__lightsaber-whoosh.wav`), la suppression de `Scripts/Combat/KatanaShield.cs`, et la documentation
(GDD, README, crédits, checklist, feuille de route, ce document).

## 2. La refonte du katana (prochaine tâche)

### Ce qui existe aujourd'hui

Le katana et le gantelet remplacent le katana et le bouclier depuis le 22 octobre (GDD, section 5, paragraphe « Refait le
22 octobre » et suivants). Le joueur trace au katana un symbole style kanji (2 ou 3 traits droits) qui flotte devant lui ;
chaque trait ouvre une faille dans la matrice ; le signe complet vient à portée du gantelet, qui le lance vers l'ennemi le plus
proche du centre de la vue ; tout le signe touche.

Les fichiers :

| Fichier | Rôle | Dans la prochaine version |
|---|---|---|
| `Scripts/Combat/KatanaGauntlet.cs` | L'arme : le katana flottant (prise, coup au corps à corps, coupe des projectiles), le gantelet porté sur la main de l'arc, le symbole et la reconnaissance des traits, la présentation du signe au gantelet, le lancer (`LaunchSign`, `FindTarget`), le jeu au clavier, le raccourci **O** | À garder, sans tout ce qui touche aux symboles |
| `Scripts/Combat/KanjiGlyphs.cs`, `KanjiGuide.cs`, `Shaders/KanjiBrush.shader`, matériau `Art/Katana/Kanji pinceau` | Les symboles et leur affichage | **À retirer** |
| `Scripts/Combat/MatrixSign.cs` | La faille comme objet : ses fissures, son état (en cours, prête, en vol, brisée), le vol vers la cible (elle la suit) et la zone de touche (tout le signe) | À garder et adapter (une faille = une trace) |
| `Scripts/Combat/MatrixCrackMesh.cs` | Le maillage d'une fissure : tracé en zigzag, bords déchiquetés, fissures secondaires ; **figé** | À refaire pour une faille vivante |
| `Shaders/MatrixTear.shader` | L'intérieur et les bords de la faille : le vide, les 0 et 1 verts qui tombent, les bords qui grésillent | À garder et enrichir (ouverture, bords qui bougent) |
| `Shaders/KatanaBlade.shader` | La lame néon qui bugue (Akimbow, Katana Zero) | À garder |
| `Editor/KatanaBaker.cs` | Fabrique les prefabs (`Art/Katana/Katana.prefab`, `Gantelet.prefab`), les matériaux, l'aperçu du menu et la fiche `Data/BowClasses/Bow_Katana.asset` | Retirer le matériau des symboles |
| `Bows/BowDefinition.cs` | Champs `katana…`, `gauntletModel`, `katanaGlyphMaterial`, les sons | Retirer `katanaGlyphMaterial` |
| `Player/GlovedHand.cs` | `For`, `WristFrame`, `SetGloveHidden` : le gantelet se pose sur la main gantée | Inchangé |

L'ancienne version (katana et bouclier, traces libres) est dans Git, au commit `90d5f83` :
`git show 90d5f83:"Project Archery/Assets/_Project/Scripts/Combat/KatanaShield.cs"`. Sa mécanique de traces est réutilisable :
un coup rapide (pointe à 3,5 m/s ou plus) laisse une trace qui reste 1,2 s ; la frapper vite donne un tir parfait (moins de 0,3 s),
bon (moins de 0,8 s), raté ensuite.

### Ce que veut le joueur (décidé le 23 octobre)

- **Retirer le système de symboles** : il ne convainc pas (traits durs à faire reconnaître, signe trop loin pour le gantelet,
  courbes trop dures ; voir l'historique dans le GDD).
- **Garder le gantelet** (il lance la faille) **et la faille dans la matrice** (les 0 et 1 qui tombent dedans, comme dans Matrix).
- Sans doute **revenir aux traces libres** : un coup rapide ouvre une faille le long du geste de la lame ; la toucher avec le
  gantelet la lance vers l'ennemi le plus proche du centre de la vue. **À confirmer avec le joueur avant de coder.**
- **Améliorer la faille** : il faut vraiment avoir l'impression d'ouvrir le jeu, avec une faille **presque vivante** :
  - elle **s'ouvre comme une plaie** : plus large au milieu qu'aux extrémités, avec une animation d'ouverture ;
  - **la fissure et sa forme bougent** sans cesse : bords qui ondulent, tracé qui frémit, fissures secondaires qui rampent.

### Pistes techniques

- Ouverture : un paramètre `_Open` (0 à 1, animé par le script, avec un léger dépassement) qui multiplie la largeur ; un profil
  de plaie le long de la faille (par exemple `sin(π·t)^0,7`, t de 0 à 1 le long) au lieu d'une largeur bruitée.
- Bords vivants : dans le vertex shader, déplacer chaque sommet de bord le long de la normale dans le plan, d'un bruit qui dépend
  de sa position le long de la faille et du temps. Il faut pour cela donner au maillage la position normalisée le long de la faille
  et la normale dans le plan (UV2, tangentes ou couleur des sommets).
- Tracé qui frémit : soit le même bruit sur les sommets du milieu, soit reconstruire le maillage quelques fois par seconde depuis
  le tracé d'origine.
- Garder pour le shader les UV actuelles : UV0 = (le long de la faille en mètres, -1 à 1 en travers), UV1 = position dans le plan
  (grille des chiffres).

## 3. Les autres choses à faire

Dans l'ordre conseillé (aussi dans le GDD, section 21, « À faire ensuite ») :
1. **La présentation** (PowerPoint) pour l'oral, et les répétitions (`Demo_Vendredi.md` : pitch, déroulé, raccourcis).
2. Les tests au casque des nouveautés (`Checklist_Demo.md`).
3. La refonte du katana (partie 2).
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
