# Guide : les difficultés et leurs ciels

Ce guide ajoute les 4 difficultés du GDD (section 11) à la scène `Prototype_Tir` :

1. avant la première vague, tu tires dans l'un des **4 panneaux** devant la tour pour choisir la difficulté ;
2. le **ciel change en direct** : soleil au zénith, coucher de soleil, nuit étoilée ou lune de sang ;
3. la difficulté change :
   - les ennemis : PV, vitesse, dégâts, taille de la tête et nombre ;
   - la courbe de difficulté : de combien les PV, les dégâts, le nombre et la vitesse des ennemis augmentent à chaque vague ;
   - la largeur du vert (tir parfait) de l'anneau ;
   - l'aide à la visée : trajectoire complète, ligne droite ou rien ;
   - le score ;
4. la nuit, une **lanterne** s'allume sur la tour et les **yeux des chevaliers** brillent.

La difficulté est verrouillée dès que la vague 1 commence. Elle reste la même quand la partie recommence après une défaite.

| | Facile | Normal | Difficile | Impossible |
|---|---|---|---|---|
| Ciel | soleil au zénith | coucher de soleil | nuit, lune claire | lune de sang |
| PV des ennemis | ×0,7 | ×1 | ×1,4 | ×2 |
| Vitesse des ennemis | ×0,8 | ×1 | ×1,15 | ×1,3 |
| Taille de la tête | ×1,3 | ×1 | ×0,85 | ×0,75 |
| Dégâts subis (joueur et tour) | ×0,6 | ×1 | ×1,3 | ×1,8 |
| Nombre d'ennemis | ×0,75 | ×1 | ×1,25 | ×1,6 |
| PV des ennemis, à chaque vague | ×1,06 | ×1,08 | ×1,11 | ×1,14 |
| Dégâts subis, à chaque vague | ×1,03 | ×1,04 | ×1,06 | ×1,08 |
| Nombre d'ennemis, à chaque vague | ×1,04 | ×1,05 | ×1,07 | ×1,09 |
| Vitesse des ennemis, à chaque vague | ×1,01 | ×1,02 | ×1,025 | ×1,03 |
| Début plus doux (PV et dégâts) | non | non | non | ×0,5 à la vague 1, en remontant jusqu'à ×1 à la vague 6 |
| Largeur de la bande verte (tir parfait) | ×1,3 | ×1 | ×0,85 | ×0,7 |
| Aide à la visée | trajectoire complète | trajectoire complète | ligne droite | aucune |
| Score | ×0,75 | ×1 | ×1,5 | ×2 |

Les lignes « à chaque vague » font une **courbe exponentielle** : elles se multiplient d'une vague à l'autre. En Normal, un Rampant a 30 PV à la vague 1, 60 à la vague 10 et 402 à la vague 20 ; il court à 2,5 m/s, puis 3 m/s, puis 4,9 m/s. La vitesse ne fait au plus que doubler (*Max Speed Scale* de `WaveSettings`). En mode infini, la courbe s'accélère encore (partie *Mode infini* de `Data/Waves/WaveSettings`). Le GDD (section 11) donne le tableau complet.

En Impossible, le **début de partie est plus doux** : à la vague 1, les ennemis n'ont que la moitié de leurs PV et de leurs dégâts, puis ils regagnent 10 % par vague jusqu'à retrouver toute leur force à la vague 6 (*Early Strength* et *Full Strength Wave*, partie *Début de partie plus doux*). Leur nombre ne change pas.

> **Tes 4 difficultés sont déjà créées ?** Il n'y a rien à refaire : les nouvelles valeurs ont été ajoutées à leurs assets. Tu les trouves dans les parties *Progression d'une vague à l'autre* et *Début de partie plus doux* de chaque asset. L'ancienne case *Aim Guide* est devenue la liste *Aim Guide Mode* (`None`, `Line`, `Trajectory`), déjà réglée dans les 4 assets (GDD, section 4.3).

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Difficulty Definition` | Asset de données : les multiplicateurs et le ciel d'une difficulté | `Data/Difficulties` |
| `Difficulty Manager` | Garde la difficulté choisie | Objet `Game` |
| `Difficulty Button` | Panneau à viser pour choisir une difficulté | Chaque panneau |
| `Sky Controller` | Applique le ciel : skybox, soleil ou lune, lumière ambiante, brouillard | `Directional Light` |
| `Night Only` | Affiche des objets seulement la nuit | Lanterne, chevalier |
| Shader `Archery/StylizedSky` | Dessine le ciel : dégradé, soleil ou lune, étoiles | Matériau du ciel |

Les ennemis, les vagues, l'arc, l'aide à la visée, le score et la montre lisent la difficulté tout seuls : il n'y a rien à brancher pour eux. Les cibles et les mannequins d'entraînement ne changent pas.

Le son du choix est dans `Audio/Placeholder` : `difficulty_select`.

## 1. Les 4 difficultés

1. Dans `Assets/_Project/Data`, crée un dossier `Difficulties`.
2. Dedans : clic droit > *Create > Archery > Difficulty Definition*, nomme-le `Difficulty_Facile`.
3. Dans l'Inspector, ouvre le menu **⋮** en haut à droite, sur la ligne du nom de l'asset, et choisis *Valeurs du GDD : Facile*. Toutes les valeurs se remplissent, ciel compris.
4. Fais de même pour `Difficulty_Normal`, `Difficulty_Difficile` et `Difficulty_Impossible`, chacune avec ses propres valeurs.

Tu peux ensuite modifier chaque valeur à la main ; chaque champ a une infobulle. Pour le ciel, c'est plus simple de le régler en jeu (section 8).

## 2. Le gestionnaire

Sur l'objet `Game` : *Add Component > Difficulty Manager*.
- *Difficulties* : 4 éléments, dans l'ordre Facile, Normal, Difficile, Impossible ;
- *Default Index* : laisse `1`. C'est la position dans la liste (0 = Facile), donc Normal au premier lancement.

## 3. Le ciel

1. Dans `Materials`, crée un matériau `Sky_Stylized` et choisis le shader *Archery > StylizedSky*.
2. *Window > Rendering > Lighting*, onglet *Environment* :
   - *Skybox Material* : `Sky_Stylized` ;
   - dans *Other Settings*, coche **Fog** et choisis le mode *Exponential Squared*.

   Le script règle lui-même la couleur et la densité du brouillard. Mais sans cette case, Unity retire le brouillard des shaders au moment de la build, et il n'apparaîtrait que dans l'éditeur.
3. Sur `Directional Light` : *Add Component > Sky Controller*. Laisse ses champs vides : il prend la lumière de son objet et le ciel réglé dans Lighting.

## 4. Les panneaux de difficulté

On fabrique un panneau, on en fait un prefab, puis on le place 4 fois.

### Le panneau

1. *Create Empty*, nomme-le `Difficulty Panel`.
2. Enfant `Board` : *3D Object > Cube*, Scale `(1.8, 1.1, 0.08)`.
   - Garde son *Box Collider* : c'est lui que les flèches touchent.
   - Crée un matériau `Panel` (*Universal Render Pipeline/Lit*, blanc) et mets-le sur `Board`. Le script le teinte avec la couleur de la difficulté.
3. Enfant `Selected` : *3D Object > Cube*, Position `(0, 0, 0.03)`, Scale `(1.95, 1.25, 0.04)`.
   - Supprime son *Box Collider*.
   - Crée un matériau `Panel_Frame` (*Universal Render Pipeline/Unlit*, blanc) et mets-le sur `Selected`. C'est le cadre lumineux du panneau choisi.
4. Enfant `Name` : *3D Object > Text - TextMeshPro* :
   - Position `(0, 0.25, -0.05)`, Width `1.7`, Height `0.4` ;
   - *Font Size* `3.5`, en gras, centré.
5. Enfant `Description` : *3D Object > Text - TextMeshPro* :
   - Position `(0, -0.2, -0.05)`, Width `1.7`, Height `0.55` ;
   - *Font Size* `1.3`, centré.

   Le texte de départ de `Name` et `Description` n'a pas d'importance : il est remplacé en jeu.
6. Sur `Difficulty Panel` : *Add Component > Difficulty Button* :
   - *Name Text* : `Name`, *Description Text* : `Description` ;
   - *Tinted Renderers* : `Board` ;
   - *Selected Indicator* : `Selected` ;
   - *Select Clip* : `difficulty_select`.
7. Glisse `Difficulty Panel` dans `Prefabs` pour en faire un prefab, puis supprime-le de la scène.

### Les 4 panneaux

1. *Create Empty*, nomme-le `Difficulty Board`, Position `(0, 5.8, 8)`. Les panneaux seront à hauteur des yeux, 8 m devant la tour.
2. Glisse 4 fois le prefab `Difficulty Panel` dans `Difficulty Board`, puis règle chacun :

| Nom | *Difficulty* | Position | Rotation Y |
|---|---|---|---|
| `Panel Facile` | `Difficulty_Facile` | `(-3.3, 0, 0)` | `-22` |
| `Panel Normal` | `Difficulty_Normal` | `(-1.1, 0, 0)` | `-8` |
| `Panel Difficile` | `Difficulty_Difficile` | `(1.1, 0, 0)` | `8` |
| `Panel Impossible` | `Difficulty_Impossible` | `(3.3, 0, 0)` | `22` |

   Les rotations tournent un peu les panneaux des côtés vers toi.
3. Sur le `Wave Manager` de `Game`, dans *On Wave Started* :
   - clique sur `+` et glisse `Difficulty Board` ;
   - choisis *GameObject > SetActive (bool)* et laisse la case décochée.

   Les panneaux disparaissent au début de la vague 1 et ne reviennent qu'à la partie suivante.

## 5. La nuit

### La lanterne de la tour

1. *Create Empty*, nomme-le `Lantern`, Position `(-2.2, 4, 2.2)`, au coin avant gauche du haut de la tour.
   - Ne le range pas dans `Tower` : il hériterait de son échelle `(5, 4, 5)`.
2. Enfant `Pole` : *3D Object > Cube*, Position `(0, 1.2, 0)`, Scale `(0.08, 2.4, 0.08)`.
3. Enfant `Flame` : *3D Object > Sphere*, Position `(0, 2.5, 0)`, Scale `(0.25, 0.25, 0.25)`.
   - Supprime son *Sphere Collider*.
   - Crée un matériau `Lantern_Flame` (*Universal Render Pipeline/Unlit*, orange `(255, 180, 80)`) et mets-le sur `Flame`.
4. Enfant `Lamp Light` : *Light > Point Light* :
   - Position `(0, 2.5, 0)`, couleur `(255, 190, 120)` ;
   - *Range* `25`, *Intensity* `15`.
5. Sur `Lantern` : *Add Component > Night Only*, *Targets* : `Flame` et `Lamp Light`. Le poteau reste visible le jour.

### Les yeux des chevaliers

1. Ouvre le prefab `Enemy_Rampant` et trouve l'os de la tête, `Bip001 Head` : c'est lui qui contient `Head Hitbox`.
2. Dans `Bip001 Head`, crée un objet vide `Eyes`.
3. Dans `Eyes`, crée deux *3D Object > Sphere*, `Eye L` et `Eye R`.
   - Supprime leur *Sphere Collider*. Sinon, les flèches s'y planteraient sans faire de dégâts, et la console afficherait un avertissement.
   - Crée un matériau `Enemy_Eyes` (*Universal Render Pipeline/Unlit*, rouge orangé `(255, 90, 40)`) et mets-le sur les deux sphères.
   - Dans la vue Scene, réduis-les à environ 3 cm et place-les dans la fente du casque. Les axes de l'os de la tête sont tournés : il est plus simple de les déplacer avec les poignées de la vue Scene que de taper des valeurs.
4. Sur la racine `Enemy_Rampant` : *Add Component > Night Only*, *Targets* : `Eyes`.

## 6. La difficulté sur la montre

1. Dans `Wrist HUD` > `Background`, ajoute un 8e texte, `Difficulty` (taille 26).
2. Glisse-le dans le champ *Difficulty Text* du `Hud Display`. Il affiche le nom de la difficulté, dans sa couleur.

Tu peux faire de même sur le grand panneau `Scoreboard`.

## 7. La qualité graphique dans l'éditeur

Dans *Edit > Project Settings > Quality* :

- L'éditeur utilise en ce moment le niveau *Low*, c'est-à-dire `Performance URP Config`. Avec ce réglage, les lumières ponctuelles sont désactivées, donc la lanterne n'éclaire rien. Les ombres s'arrêtent aussi à 2,5 m.
- Clique sur le nom de la ligne *Ultra* pour l'utiliser dans l'éditeur. C'est déjà le niveau par défaut des builds Windows. Il utilise `Quality URP Config` : lumières ponctuelles activées, ombres jusqu'à 10 m.
- Optionnel : dans `Assets/Settings/Project Configuration/Quality URP Config`, passe *Shadows > Max Distance* à `40`, pour que les ennemis au loin aient une ombre.

## 8. Tester

1. Lance Play. Le ciel est un coucher de soleil, et le panneau Normal est le plus gros, avec son cadre.
2. Tire dans `Difficile`. Le son se joue et le ciel passe à la nuit en 3 s, avec les étoiles et la lune.
   - La lanterne s'allume.
   - La montre affiche « Difficile » en bleu.
   - En tendant l'arc, la ligne de visée n'apparaît plus.
3. Tire dans le gong. « Vague 1 / 10 » s'affiche avec « Difficile » en dessous, et les panneaux disparaissent.
   - Les chevaliers ont les yeux qui brillent.
   - Ils sont plus rapides et ont 42 PV au lieu de 30.
4. Laisse-toi battre : la scène recommence avec la même difficulté.

**Raccourcis de test** : dans l'éditeur, les touches **1 à 4** choisissent la difficulté (avant la vague 1), et **N** lance ou termine une vague.

### Régler les ciels en jeu

Pendant le Play :

1. Sélectionne un asset de `Data/Difficulties` et modifie sa partie *Sky* : le ciel change tout de suite.
2. Change de difficulté avec les touches 1 à 4 pour comparer.

**Les assets gardent les modifications faites en Play**, contrairement aux objets de la scène.

Les réglages du ciel :
- *Elevation* et *Azimuth* placent le soleil ou la lune ; la lumière vient de là. Un azimut de 0 pointe vers les ennemis.
- *Horizon Color* est aussi la couleur du brouillard.
- *Moon Surface* : 0 pour un soleil uni, 1 pour une lune tachée.
- Les trois *Ambient* éclairent les zones d'ombre : remonte-les si les ennemis sont trop sombres la nuit.
- *Night* allume la lanterne et les yeux.

## En cas de problème

- **Le ciel est gris, ou c'est le ciel par défaut** : *Skybox Material* n'est pas `Sky_Stylized`, ou son shader n'est pas *Archery/StylizedSky*. La console affiche alors un avertissement du `Sky Controller`.
- **Le ciel est rose** : le shader n'a pas compilé. Sélectionne `Shaders/StylizedSky` et lis l'erreur dans l'Inspector.
- **Rien ne se passe quand je tire dans un panneau** :
  - il faut un `Difficulty Manager` sur `Game` ;
  - `Board` doit avoir son *Box Collider* ;
  - après le début de la vague 1, la difficulté est verrouillée.
- **La lanterne n'éclaire rien** : l'éditeur est encore au niveau de qualité *Low* (section 7).
- **Il n'y a pas de brouillard dans la build** : la case *Fog* de Lighting n'est pas cochée (section 3).
- **« le collider Eye L n'a pas de Hitbox »** : supprime le *Sphere Collider* des yeux.
- **Les étoiles scintillent trop dans le casque** : baisse *Stars* dans le ciel de la difficulté.
