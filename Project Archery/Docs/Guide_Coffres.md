# Guide : les coffres

Ce guide ajoute les coffres du GDD (section 13) à la scène `Prototype_Tir`, avec le modèle animé du pack **Animated PBR Chest Demo** :

- pendant chaque vague, **1 ou 2 coffres** apparaissent loin de la tour, à 15–30 m, avec le message « Un coffre est apparu ! ». Ils **tombent du ciel**, rebondissent, puis sautillent sur place ;
- un **rayon de lumière** doré les signale, visible depuis la tour ;
- ils sont **gratuits**, mais il faut descendre et courir pour aller les ouvrir. **Ils disparaissent à la fin de la vague**, ouverts ou non ;
- on **soulève le couvercle à la main** : on l'attrape par l'avant et on lève la main. Le coffre s'immobilise et le couvercle suit la main ;
- passé 45°, le **temps ralentit**, le couvercle s'ouvre d'un coup avec un rebond, une lueur dorée sort du coffre et **3 orbes** montent ;
- on **attrape une orbe** : les deux autres disparaissent. Les trois orbes sont toujours, de gauche à droite :
  - deux **améliorations permanentes**, différentes, tirées comme en boutique ;
  - un **bonus temporaire** : 30 s de dégâts ×2, de tirs parfaits ou d'anneau rapide.

Au-dessus de chaque orbe, « PERMANENT » ou « TEMPORAIRE », puis le nom de la récompense dans sa couleur : blanc, bleu ou doré pour une amélioration, comme en boutique (avec sa rareté dessous) ; rouge (dégâts), vert (tirs parfaits) ou cyan (anneau rapide) pour un bonus (avec sa durée dessous).

Le ralenti dure jusqu'au choix d'une orbe, au plus 6 s ; les orbes restent ensuite jusqu'à la fin de la vague.

Pendant **Tirs parfaits**, l'anneau est presque entièrement vert, et tout tir lâché à pleine tension compte comme parfait.

Une amélioration trouvée dans un coffre compte comme une amélioration achetée : les prix de la boutique montent de 5 %.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Chest Spawner` | Fait apparaître les coffres pendant les vagues, les retire à la fin | Objet `Game` |
| `Chest` | Un coffre : ouverture, orbes, ralenti, récompenses, lueur | Racine du prefab `Chest` |
| `Chest Lid` | Couvercle à soulever à la main ; arrête l'animation du coffre pendant qu'on le tient, puis lance l'ouverture | Collider du couvercle, enfant de l'os `Cap` du modèle |
| `Chest Orb` | Une orbe à attraper | Racine du prefab `Chest Orb` |
| Shader `Archery/LightBeam` | Rayon de lumière qui pulse et s'efface vers le haut | Matériau du rayon |
| `Hud Display` (déjà là) | Nouveau champ optionnel : les bonus en cours | Montre |

Les bonus temporaires sont gérés par `Player Upgrades`, déjà sur `Game` : il n'y a rien à ajouter pour eux.

Les sons sont dans `Audio/Placeholder` : `chest_appear`, `chest_open`, `orb_pick` et `chest_vanish`.

## 1. Préparer le modèle du pack

Tout est dans le dossier `Animated PBR Chest Demo`.

### 1.1 Les réglages d'import

Le couvercle du coffre est un os du squelette, `Cap`, placé sur la charnière, à l'arrière. Le script doit pouvoir le tourner à la main. Par défaut, le pack cache les os dans l'Animator : il faut les faire apparaître.

1. Sélectionne `Fbx/Animated PBR Chest _Wood_Demo`.
2. Onglet *Rig* : décoche **Optimize Game Objects**, puis *Apply*.

Le prefab et la scène de démo du pack ont été faits avec ce réglage coché : ils risquent de ne plus s'afficher correctement. Ce n'est pas grave, on ne s'en sert pas.

### 1.2 Le matériau du coffre

Le matériau `Materials/WoodChest` utilise un shader de l'ancien pipeline : le coffre est **rose**.

1. Ouvre *Window > Rendering > Render Pipeline Converter*, choisis *Built-in to URP*, coche *Material Upgrade*, puis *Initialize And Convert*.
2. Sélectionne `WoodChest` : son shader doit être *Universal Render Pipeline/Lit*, avec ses 4 textures.

Si la conversion n'a pas marché, règle-le à la main : shader *Universal Render Pipeline/Lit*, puis, dans `Textures` :

| Champ | Texture |
|---|---|
| *Base Map* | `WoodChest_Wood_Chest_AlbedoTransparency` |
| *Metallic Map* | `WoodChest_Wood_Chest_MetallicSmoothness` (*Source* : `Metallic Alpha`) |
| *Normal Map* | `WoodChest_Wood_Chest_Normal` |
| *Emission* (coché), couleur blanche | `WoodChest_Wood_Chest_Emission` |

### 1.3 Les matériaux de la lueur (optionnel)

La lueur dorée qui sort du coffre ouvert utilise deux matériaux de particules que le convertisseur ne sait pas traiter. Règle-les à la main, dans `Materials` :

| Matériau | Shader | Réglages |
|---|---|---|
| `Light_D` | *Universal Render Pipeline/Particles/Unlit* | *Surface Type* `Transparent`, *Blending Mode* `Additive`, *Base Map* : `Light_2` |
| `LightOrb_D` | *Universal Render Pipeline/Particles/Unlit* | *Surface Type* `Transparent`, *Blending Mode* `Additive`, *Base Map* : `orblight_D` |

### 1.4 Les animations

Le pack fournit 4 animations dans `Animation`. On en utilise 3 :

| Animation | Ce qu'elle montre |
|---|---|
| `Animated PBR Chest _Start` | Le coffre tombe du ciel et rebondit. |
| `Animated PBR Chest _Idle` | Il se tortille et sautille sur place. |
| `Animated PBR Chest _Opening_UnCommon` | Il s'écrase, puis le couvercle s'ouvre d'un coup avec un rebond. Le script la fait partir de 62 %, quand le couvercle se lève. |

1. Sélectionne `Animated PBR Chest _Idle` et coche **Loop Time** dans l'Inspector : l'attente tourne en boucle. Les deux autres ne se jouent qu'une fois.
2. Dans `_Project/Animations` : clic droit > *Create* > *Animator Controller*, comme pour le chevalier. Nomme-le `Chest` et ouvre-le.
3. Glisse les 3 animations dans le graphe, `_Start` en premier : c'est l'état par défaut (orange). Sinon, clic droit dessus > *Set as Layer Default State*.
4. Renomme les états (champ du nom, en haut de l'Inspector) : `Start`, `Idle` et `Opening`. Seul `Opening` doit avoir exactement ce nom : le script le cherche.
5. Une seule transition : clic droit sur `Start` > *Make Transition* > clic sur `Idle`. Garde *Has Exit Time* coché, sans condition.

`Opening` n'a aucune transition : c'est le script qui le lance, et le coffre reste ouvert à la fin.

## 2. Les matériaux du rayon et des orbes

Dans `Materials` :

| Matériau | Shader | Réglages |
|---|---|---|
| `Light_Beam` | *Archery/LightBeam* | laisse les valeurs par défaut |
| `Orb` | *Universal Render Pipeline/Unlit* | blanc : le script le colore selon la récompense |

## 3. Le prefab de l'orbe

1. *Create Empty*, nomme-le `Chest Orb`, en `(0, 0, 0)`.
2. Sur `Chest Orb` : *Add Component > Sphere Collider*, *Radius* `0.12`. C'est lui que la main attrape.
3. Enfant `Sphere` : *3D Object > Sphere*, Scale `(0.16, 0.16, 0.16)` :
   - supprime son *Sphere Collider* ;
   - matériau `Orb`.
4. Enfant `Label` : *3D Object > Text - TextMeshPro* :
   - Position `(0, 0.2, 0)`, Width `0.8`, Height `0.25` ;
   - *Font Size* `0.45`, centré au milieu. Le texte et la couleur sont remplis en jeu.
5. Sur `Chest Orb` : *Add Component > Chest Orb* :
   - *Renderer* : le *Mesh Renderer* de `Sphere` ;
   - *Label* : `Label`.
6. Glisse `Chest Orb` dans `Prefabs`, puis supprime-le de la scène.

## 4. Le prefab du coffre

L'avant du coffre (sa serrure) doit regarder vers l'axe Z (bleu) de la racine : le spawner le tourne vers la tour, et le couvercle s'ouvre vers l'arrière. Le modèle du pack est déjà dans ce sens.

1. *Create Empty*, nomme-le `Chest`, en `(0, 0, 0)`.
2. Glisse le FBX `Fbx/Animated PBR Chest _Wood_Demo` (le FBX, pas le prefab du pack) en enfant de `Chest`, et renomme-le `Model` :
   - Position `(0, 0, 0)`, Rotation `(0, 0, 0)`, Scale `(0.35, 0.35, 0.35)`. Le coffre fait alors 1 m de large et 70 cm de haut ;
   - composant *Animator* : *Controller* `Chest`, décoche *Apply Root Motion*.
3. Le couvercle. Dans la hiérarchie de `Model`, déplie `Armature > Root > Base.001 > Base.002 > Cap`. Clic droit sur `Cap` > *Create Empty*, nomme-le `Lid` :
   - Position `(0, 0, 0)`, Rotation `(-90, 0, 0)`, Scale `(1, 1, 1)`. Ses axes sont alors ceux du coffre ;
   - *Add Component > Box Collider* : *Center* `(0, 0.28, 0.79)`, *Size* `(2.7, 1.25, 1.75)`. La boîte verte doit entourer le couvercle ; ajuste-la avec *Edit Collider* si besoin. Ces valeurs sont à l'échelle du modèle, avant le `0.35` ;
   - *Add Component > Chest Lid* : laisse *Hinge* vide (c'est son parent, `Cap`), *Animator* : `Model`. Garde *Open State* `Opening` et *Open State Start* `0.62`.
4. Le corps : sur `Chest`, *Add Component > Box Collider*, *Center* `(0, 0.2, 0)`, *Size* `(0.95, 0.4, 0.57)`. Les flèches s'y plantent et on ne traverse pas le coffre.
5. Les autres enfants de `Chest` :

| Nom | Création | Position | Scale | Réglages |
|---|---|---|---|---|
| `Orb Anchor` | *Create Empty* | `(0, 1.1, 0.15)` | `(1, 1, 1)` | là où flottent les orbes |
| `Beam` | *3D Object > Cylinder* | `(0, 20, 0)` | `(0.5, 20, 0.5)` | matériau `Light_Beam` ; supprime son *Capsule Collider* ; *Cast Shadows* `Off` |

6. La lueur (optionnel, avec la section 1.3) : glisse le prefab `Fx/Animated PBR Chest _Fx_D` en enfant de `Model` :
   - Position `(0, 0.83, 0.06)`, Rotation `(-90, 0, 0)`, Scale `(1, 1, 1)`. Elle est au fond du coffre et monte vers le haut ;
   - sur lui **et** sur son enfant `Light`, module principal du *Particle System* : *Scaling Mode* `Hierarchy`. Sinon, les particules gardent la taille d'un coffre de 3 m.
7. Sur `Chest` : *Add Component > Chest* :
   - *Lid* : `Lid` ;
   - *Orb Prefab* : `Chest Orb` (le prefab) ;
   - *Orb Anchor* : `Orb Anchor` ;
   - *Beam* : `Beam` ;
   - *Open Effect* : `Animated PBR Chest _Fx_D` (si tu as mis la lueur). Le script l'éteint au départ et l'allume à l'ouverture ;
   - sons : *Open Clip* `chest_open`, *Pick Clip* `orb_pick`, *Vanish Clip* `chest_vanish`.
8. Optionnel, pour qu'il brille la nuit : un enfant *Light > Point Light* en `(0, 1, 0)`, doré, *Range* `4`, *Intensity* `2`.
9. Glisse `Chest` dans `Prefabs`, puis supprime-le de la scène.

**Pour vérifier sans casque** : ouvre le prefab, sélectionne `Cap` et tourne sa rotation X de `90` à `0` : le couvercle s'ouvre vers l'arrière, et la boîte verte de `Lid` le suit. Remets `90` (ou Ctrl+Z).

## 5. Le spawner

Sur l'objet `Game` : *Add Component > Chest Spawner* :
- *Chest Prefab* : `Chest` (le prefab) ;
- *Appear Clip* : `chest_appear`.

Par défaut, les coffres se posent au hasard sur le sol des ennemis (NavMesh), entre 15 et 30 m de la tour. Pour choisir les endroits toi-même, par exemple dans la forêt de la carte finale, place des objets vides et glisse-les dans *Spawn Points*.

## 6. Les bonus sur la montre (optionnel)

1. Dans `Wrist HUD` > `Background`, ajoute un texte `Buffs` (taille 22, couleur claire).
2. Glisse-le dans le champ *Buff Text* du `Hud Display`. Il affiche les bonus en cours, par exemple « Dégâts ×2 · 23 s ».

## 7. Tester

**Raccourci de test** (éditeur) : la touche **C** pose un coffre 2,5 m devant toi, tourné vers toi, à tout moment.

1. Lance Play et appuie sur **C**. Le coffre tombe devant toi et rebondit, avec son rayon de lumière et un son. Il sautille ensuite de temps en temps.
2. Avec la main libre, attrape le couvercle par l'avant, près de la serrure, puis lève la main :
   - le coffre s'immobilise et le couvercle suit ta main ;
   - passé 45°, le temps ralentit, le couvercle s'ouvre d'un coup (au ralenti), la lueur sort du coffre et 3 orbes montent.
3. Lâche le couvercle avant 45° : il retombe, et le coffre se remet à sautiller.
4. Les trois orbes montrent, de gauche à droite : « PERMANENT », « PERMANENT » et « TEMPORAIRE ». Approche la main d'une orbe : elle grossit. Attrape-la : un message annonce la récompense, et les deux autres disparaissent.
5. Si c'est un bonus, il s'affiche sur la montre (section 6) :
   - *Dégâts ×2* : les chiffres de dégâts doublent ;
   - *Tirs parfaits* : l'anneau est presque entièrement vert ;
   - *Anneau rapide* : l'anneau va plus vite.
6. En jeu normal, lance une vague : après environ 15 % du chrono, « Un coffre est apparu ! » s'affiche et un rayon s'élève dans la clairière. Appuie sur **N** pour finir la vague : le coffre disparaît.

## En cas de problème

- **Aucun coffre n'apparaît pendant les vagues** :
  - il faut un `Chest Spawner` sur `Game`, avec son prefab ;
  - sans *Spawn Points*, il faut un NavMesh entre 15 et 30 m de la tour : la console l'indique sinon.
- **Le coffre est rose** : son matériau n'est pas converti (section 1.2).
- **La lueur est faite de carrés roses** : ses deux matériaux (section 1.3).
- **La lueur est énorme** : *Scaling Mode* `Hierarchy` sur les deux systèmes de particules (section 4, étape 6).
- **Il n'y a pas d'os `Cap` sous `Model`** : *Optimize Game Objects* est encore coché (section 1.1). Après *Apply*, supprime `Model` et glisse de nouveau le FBX.
- **Le coffre ne tombe pas et ne bouge pas** : le *Controller* de l'Animator de `Model` est vide, ou `Start` n'est pas l'état par défaut.
- **Il ne sautille qu'une fois** : *Loop Time* n'est pas coché sur `Animated PBR Chest _Idle`.
- **Le couvercle ne s'attrape pas** :
  - attrape-le à la main, pas au rayon, avec la main libre ;
  - `Lid` doit avoir son *Box Collider* et le `Chest Lid`, et la boîte verte doit entourer le couvercle.
- **Le couvercle tourne dans le mauvais sens ou se décroche** :
  - `Lid` doit être l'enfant de `Cap`, avec *Hinge* vide ;
  - `Model` doit avoir la Rotation `(0, 0, 0)`.
- **À l'ouverture, le couvercle se referme puis se rouvre** : *Open State Start* est trop bas. Monte-le par pas de `0.02`.
- **Le couvercle s'ouvre moins haut que dans l'animation** : la console indique que l'état `Opening` n'existe pas. Le script finit alors d'ouvrir le couvercle seul, jusqu'à *Max Angle*. Vérifie le nom de l'état (section 1.4).
- **Les orbes sont blanches** : le matériau `Orb` doit utiliser *Universal Render Pipeline/Unlit* (le script change sa *Base Color*).
- **Le rayon est rose ou invisible** : `Light_Beam` doit utiliser le shader *Archery/LightBeam*. Sélectionne le shader et lis l'erreur dans l'Inspector.
- **Le temps reste ralenti** : il revient tout seul à la normale au choix d'une orbe, après 6 s, ou à la disparition du coffre. Si ça arrive quand même, dis-le-moi avec le message de la console.

**Sans les animations** : si tu préfères un coffre immobile, laisse vides le *Controller* de l'Animator et le champ *Animator* de `Chest Lid`, et mets *Max Angle* à `130`. Le script tourne alors le couvercle seul, jusqu'au bout.
