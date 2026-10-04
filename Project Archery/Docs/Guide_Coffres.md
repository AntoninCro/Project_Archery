# Guide : les coffres

Ce guide ajoute les coffres du GDD (section 13) à la scène `Prototype_Tir` :

- pendant chaque vague, **1 ou 2 coffres** apparaissent loin de la tour, à 15–30 m, avec le message « Un coffre est apparu ! » ;
- un **rayon de lumière** doré les signale, visible depuis la tour ;
- ils sont **gratuits**, mais il faut descendre et courir pour aller les ouvrir. **Ils disparaissent à la fin de la vague**, ouverts ou non ;
- on **soulève le couvercle à la main** : on l'attrape avec la poignée et on lève la main. Le **temps ralentit** et **3 orbes** sortent du coffre ;
- on **attrape une orbe** : les deux autres disparaissent. Il y a toujours au moins :
  - une **amélioration** pour toute la partie, tirée comme en boutique ;
  - un **bonus** : 30 s de dégâts ×2, de tirs parfaits ou d'anneau rapide, ou un soin (seulement si tu as perdu des PV).

Le nom de chaque récompense flotte au-dessus de son orbe, dans sa couleur : blanc, bleu ou doré pour une amélioration, comme en boutique ; rouge (dégâts), vert (tirs parfaits), cyan (anneau rapide) ou rose (soin) pour un bonus.

Le ralenti dure jusqu'au choix d'une orbe, au plus 6 s ; les orbes restent ensuite jusqu'à la fin de la vague.

Pendant **Tirs parfaits**, l'anneau est presque entièrement vert, et tout tir lâché à pleine tension compte comme parfait.

Une amélioration trouvée dans un coffre compte comme une amélioration achetée : les prix de la boutique montent de 5 %.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Chest Spawner` | Fait apparaître les coffres pendant les vagues, les retire à la fin | Objet `Game` |
| `Chest` | Un coffre : ouverture, orbes, ralenti, récompenses | Racine du prefab `Chest` |
| `Chest Lid` | Couvercle à soulever à la main | Couvercle du coffre |
| `Chest Orb` | Une orbe à attraper | Racine du prefab `Chest Orb` |
| Shader `Archery/LightBeam` | Rayon de lumière qui pulse et s'efface vers le haut | Matériau du rayon |
| `Hud Display` (déjà là) | Nouveau champ optionnel : les bonus en cours | Montre |

Les bonus temporaires sont gérés par `Player Upgrades`, déjà sur `Game` : il n'y a rien à ajouter pour eux.

Les sons sont dans `Audio/Placeholder` : `chest_appear`, `chest_open`, `orb_pick` et `chest_vanish`.

## 1. Les matériaux

Dans `Materials` :

| Matériau | Shader | Réglages |
|---|---|---|
| `Chest_Wood` | *Universal Render Pipeline/Lit* | brun `(120, 75, 40)`, *Smoothness* 0,2 |
| `Chest_Metal` | *Universal Render Pipeline/Lit* | doré `(210, 165, 60)`, *Metallic* 0,7 |
| `Light_Beam` | *Archery/LightBeam* | laisse les valeurs par défaut |
| `Orb` | *Universal Render Pipeline/Unlit* | blanc : le script le colore selon la récompense |

## 2. Le prefab de l'orbe

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

## 3. Le prefab du coffre

Le coffre est fait de cubes, en attendant un modèle de l'Asset Store en fin de projet. Son avant est vers l'axe Z (bleu) : le spawner le tourne vers la tour, et le couvercle s'ouvre vers l'arrière.

1. *Create Empty*, nomme-le `Chest`, en `(0, 0, 0)`.
2. Ses enfants :

| Nom | Création | Position | Scale | Réglages |
|---|---|---|---|---|
| `Body` | *3D Object > Cube* | `(0, 0.25, 0)` | `(0.9, 0.5, 0.6)` | matériau `Chest_Wood` ; garde son *Box Collider* |
| `Hinge` | *Create Empty* | `(0, 0.5, -0.3)` | `(1, 1, 1)` | la charnière, sur l'arête arrière du dessus |
| `Orb Anchor` | *Create Empty* | `(0, 1.1, 0.15)` | `(1, 1, 1)` | là où flottent les orbes |
| `Beam` | *3D Object > Cylinder* | `(0, 20, 0)` | `(0.5, 20, 0.5)` | matériau `Light_Beam` ; supprime son *Capsule Collider* ; *Cast Shadows* `Off` |

3. Dans `Hinge` :

| Nom | Création | Position | Scale | Réglages |
|---|---|---|---|---|
| `Lid` | *3D Object > Cube* | `(0, 0.06, 0.3)` | `(0.92, 0.12, 0.62)` | matériau `Chest_Wood` ; garde son *Box Collider* |
| `Lock` | *3D Object > Cube* | `(0, 0.02, 0.61)` | `(0.12, 0.14, 0.04)` | matériau `Chest_Metal` ; supprime son *Box Collider* |

   Le couvercle et la serrure tournent avec la charnière.
4. Sur `Lid` : *Add Component > Chest Lid*. Laisse *Hinge* vide : c'est son parent, `Hinge`.
5. Sur `Chest` : *Add Component > Chest* :
   - *Lid* : `Lid` ;
   - *Orb Prefab* : `Chest Orb` (le prefab) ;
   - *Orb Anchor* : `Orb Anchor` ;
   - *Beam* : `Beam` ;
   - sons : *Open Clip* `chest_open`, *Pick Clip* `orb_pick`, *Vanish Clip* `chest_vanish`.
6. Optionnel, pour qu'il brille la nuit : un enfant *Light > Point Light* en `(0, 1, 0)`, doré, *Range* `4`, *Intensity* `2`.
7. Glisse `Chest` dans `Prefabs`, puis supprime-le de la scène.

## 4. Le spawner

Sur l'objet `Game` : *Add Component > Chest Spawner* :
- *Chest Prefab* : `Chest` (le prefab) ;
- *Appear Clip* : `chest_appear`.

Par défaut, les coffres se posent au hasard sur le sol des ennemis (NavMesh), entre 15 et 30 m de la tour. Pour choisir les endroits toi-même, par exemple dans la forêt de la carte finale, place des objets vides et glisse-les dans *Spawn Points*.

## 5. Les bonus sur la montre (optionnel)

1. Dans `Wrist HUD` > `Background`, ajoute un texte `Buffs` (taille 22, couleur claire).
2. Glisse-le dans le champ *Buff Text* du `Hud Display`. Il affiche les bonus en cours, par exemple « Dégâts ×2 · 23 s ».

## 6. Tester

**Raccourci de test** (éditeur) : la touche **C** pose un coffre 2,5 m devant toi, tourné vers toi, à tout moment.

1. Lance Play et appuie sur **C**. Un coffre apparaît avec son rayon de lumière et un son.
2. Avec la main libre, attrape le couvercle (poignée) près de son bord avant, puis lève la main :
   - le couvercle suit ta main ;
   - passé 45°, le temps ralentit, le couvercle s'ouvre seul et 3 orbes montent du coffre.
3. Approche la main d'une orbe : elle grossit. Attrape-la : un message annonce la récompense, et les deux autres disparaissent.
4. Si c'est un bonus, il s'affiche sur la montre (section 5) :
   - *Dégâts ×2* : les chiffres de dégâts doublent ;
   - *Tirs parfaits* : l'anneau est presque entièrement vert ;
   - *Anneau rapide* : l'anneau va plus vite.
5. En jeu normal, lance une vague : après environ 15 % du chrono, « Un coffre est apparu ! » s'affiche et un rayon s'élève dans la clairière. Appuie sur **N** pour finir la vague : le coffre disparaît.

## En cas de problème

- **Aucun coffre n'apparaît pendant les vagues** :
  - il faut un `Chest Spawner` sur `Game`, avec son prefab ;
  - sans *Spawn Points*, il faut un NavMesh entre 15 et 30 m de la tour : la console l'indique sinon.
- **Le couvercle ne s'attrape pas** :
  - attrape-le à la main, pas au rayon, avec la main libre ;
  - `Lid` doit avoir son *Box Collider* et le `Chest Lid`.
- **Le couvercle tourne dans le mauvais sens ou se décroche** :
  - `Hinge` doit être sur l'arête **arrière** du dessus (`z = -0.3`) ;
  - `Lid` doit être son enfant, décalé vers l'avant (`z = 0.3`).
- **Les orbes sont blanches** : le matériau `Orb` doit utiliser *Universal Render Pipeline/Unlit* (le script change sa *Base Color*).
- **Le rayon est rose ou invisible** : `Light_Beam` doit utiliser le shader *Archery/LightBeam*. Sélectionne le shader et lis l'erreur dans l'Inspector.
- **Le temps reste ralenti** : il revient tout seul à la normale au choix d'une orbe, après 6 s, ou à la disparition du coffre. Si ça arrive quand même, dis-le-moi avec le message de la console.
