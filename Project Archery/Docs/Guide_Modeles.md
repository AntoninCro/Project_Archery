# Guide : remplacer l'arc, les flèches et l'ennemi par de vrais modèles

Ce guide remplace les formes provisoires par les modèles de tes deux packs :

- **Easy Weapons** (dossier `Kendall H`) pour l'arc et les flèches ;
- **Toon RTS Units – Demo** pour le chevalier.

Les scripts sont prêts. Tout se règle dans les prefabs de `Assets/_Project/Prefabs`. Les menus sont donnés avec leurs noms anglais.

## Ce qu'il faut savoir sur les arcs d'Easy Weapons

Ils sont « riggés » : un squelette et des contraintes *Animation Rigging* font plier les branches. Le pack fournit un script pour ça, `bowWeaponControllerAA`, mais sa corde est recalculée trop tôt dans l'image : en VR, elle traînerait derrière l'arc quand tu le bouges.

On le remplace donc par notre composant **`Bow Visual`** :

- il indique à notre `Bow` où s'accroche la corde ;
- il fait plier les branches selon la tension ;
- notre corde, elle, reste toujours collée à l'arc.

Le même composant servira plus tard à changer d'arc en boutique.

## 1. L'arc

1. Ouvre le prefab `Bow` (double-clic dans `Assets/_Project/Prefabs`).
2. Sous `Model`, supprime les formes provisoires : `Riser`, `Grip`, `Shelf`, `Upper Limb` et `Lower Limb`. Garde `Arrow Rest`, `String` et `Timing Ring`.
3. Depuis `Kendall H/Easy Weapons/Prefabs/Regular_unEnchanted`, glisse `Bow.RegColor.noEnch.0.1` (ou un autre des 5 arcs) **en enfant de `Model`**.
   - *Transform* : Position `(0, 0, 0)`, Rotation `(0, 0, 0)`, Scale `(1, 1, 1)`. L'arc fait environ 95 cm ; tu peux monter la Scale à `1.2` pour un arc plus grand.
4. Sur cet arc importé (sa racine), clic droit sur le composant **`Bow Weapon Controller AA`** > *Remove Component*.
   - Ne te contente pas de le désactiver : son `Awake` créerait quand même une deuxième corde.
5. Toujours sur sa racine, *Add Component > Bow Visual* :
   - *String Top* et *String Bottom* : les deux bouts des branches, `top.end` et `bottom.end`. Ils se trouvent dans `BowRig > Bones > top.1 > top.2 > top.3 > top.3.ajust`, et pareil côté `bottom`. L'ordre n'a pas d'importance (dans ce pack, la branche « bottom » est d'ailleurs en haut).
   - *Draw Constraint* : l'objet `StringDraw`, dans `BowRig > Bones > Rig 1`.
   - *Arrow Rest* : laisse vide.
6. Sélectionne `Arrow Rest` (sous `Model`) et place-le là où la flèche doit reposer, juste au-dessus de la main : par exemple `(0, 0.02, 0)`. Un petit décalage en X fait passer la flèche à côté de l'arc plutôt qu'au milieu.
7. Enregistre le prefab.

**Pour tester sans casque** : en Play, sélectionne le `Bow` de la scène et regarde la vue Scene. En tirant la corde dans le casque, les branches plient.

## 2. Les flèches

Les flèches du pack mesurent 2 unités de long et pointent vers −Z (la pointe est près de leur origine). Notre flèche doit avoir son encoche à l'origine et sa pointe à +0,8 m. On la retourne donc et on la réduit.

1. Ouvre le prefab `Arrow`.
2. Sous `Visual`, supprime `Shaft`, `Head`, les trois `Fletching` et `Nock`.
3. Glisse `arrow.RegColor.noEnch.0.8` (ou une autre des 5 flèches) en enfant de `Visual`.
4. *Transform* : Position `(0, 0, 0.78)`, Rotation `(0, 180, 0)`, Scale `(0.4, 0.4, 0.4)`.
5. Sur cette flèche, supprime le composant `Weapon Controller AA` (*Remove Component*). Il ne sert à rien ici.
6. Vérifie dans la vue Scene : la pointe doit arriver sur l'objet `Tip` (z = 0,8) et l'encoche sur le point d'origine du prefab. Ajuste la Position en Z si besoin.
7. Enregistre le prefab.

Important : la flèche ne doit avoir **aucun collider**. Celle du pack n'en a pas : n'en ajoute pas.

## 3. Le chevalier

### 3.1 Sa texture

Seuls le FBX du chevalier et 4 animations sont dans le projet, pas sa texture : il apparaît sans doute blanc.

1. *Window > Package Manager > My Assets > Toon RTS Units - Demo > Import*.
2. Dans la fenêtre d'import, coche seulement les **textures** et **matériaux**, puis *Import*.
3. Si les matériaux importés sont **roses**, c'est qu'ils utilisent un shader de l'ancien pipeline. Convertis-les avec *Window > Rendering > Render Pipeline Converter* : choisis *Built-in to URP*, coche *Material Upgrade*, puis *Initialize And Convert*.
4. Autre solution : crée un matériau `Knight` (*Universal Render Pipeline/Lit*), mets la texture dans *Base Map*, et glisse-le sur le chevalier.

### 3.2 Les réglages d'import (pour que les animations restent sur place)

Le chevalier est en squelette *Generic* (os « Bip001 »). Les ennemis sont déplacés par le NavMesh, donc les animations ne doivent pas avancer toutes seules.

1. Sélectionne `ToonRTS_demo_Knight.FBX` (`_Project/Art/Monsters/Knight`). Onglet *Rig* : *Animation Type* `Generic`, *Avatar Definition* `Create From This Model`, *Root node* `Bip001`. Puis *Apply*.
2. Sélectionne les 4 FBX de `_Project/Animations`. Onglet *Rig* : *Avatar Definition* `Copy From Other Avatar`, *Source* l'avatar du chevalier (`ToonRTS_demo_KnightAvatar`). Puis *Apply*.
3. Pour chaque animation, onglet *Animation* : dans *Root Transform Rotation*, *Root Transform Position (Y)* et *Root Transform Position (XZ)*, coche **Bake Into Pose**. Vérifie que *Loop Time* est coché pour `charge`, `combat_idle` et `combat_walk`, mais pas pour `attack_B`. Puis *Apply*.

### 3.3 L'Animator Controller

1. Dans `_Project/Animations` : clic droit > *Create > Animator Controller*, nomme-le `Knight`, puis ouvre-le.
2. Onglet *Parameters* : ajoute `Speed` (Float) et `Attack` (Trigger).
   - N'ajoute pas `Hit` ni `Die` : tu n'as pas ces animations. Sans `Die`, le script fait basculer le chevalier en arrière à sa mort.
3. Glisse les animations dans le graphe :
   - `combat_idle` : clic droit > *Set as Layer Default State* ;
   - `charge` : la course (nos ennemis vont à 2,5 m/s) ;
   - `attack_B`.
4. Les transitions (clic droit sur un état > *Make Transition*) :

| De | Vers | Condition | Has Exit Time |
|---|---|---|---|
| combat_idle | charge | `Speed` Greater `0.1` | non |
| charge | combat_idle | `Speed` Less `0.1` | non |
| Any State | attack_B | `Attack` | non (et décoche *Can Transition To Self*) |
| attack_B | combat_idle | aucune | oui |

### 3.4 Le prefab de l'ennemi

1. Ouvre le prefab `Enemy_Rampant`.
2. Glisse `ToonRTS_demo_Knight.FBX` en enfant de la racine. Position `(0, 0, 0)`.
   - Le chevalier doit regarder vers la flèche bleue (axe Z) de la racine. Sinon, mets sa Rotation Y à `180`.
   - Il doit mesurer environ 1,8 à 2 m. Ajuste sa Scale si besoin.
3. Sur le chevalier, composant *Animator* : *Controller* `Knight`, décoche *Apply Root Motion*. Le script `Enemy` le trouve tout seul.
4. Les zones de touche suivent maintenant les os :
   - **Tête** : dans la hiérarchie du chevalier, trouve l'os `Bip001 Head`. Clic droit dessus > *Create Empty*, nomme-le `Head Hitbox`. Ajoute un *Sphere Collider* (ajuste *Radius* et *Center* pour couvrir le casque) et une *Hitbox* : *Zone* `Head`, *Damage Multiplier* `2`, *Hit Clip* `headshot_ding`.
   - **Corps** : sur l'os `Bip001 Spine`, crée de la même façon `Body Hitbox` avec un *Capsule Collider* (choisis la *Direction* le long du dos) et une *Hitbox* : *Zone* `Body`. La capsule ne doit pas englober la sphère de la tête, sinon elle « vole » les headshots.
5. Supprime les anciennes formes `Body` et `Head`, avec leurs colliders.
6. *Nav Mesh Agent* : adapte *Height* et *Radius* au chevalier.
7. Enregistre le prefab.

### 3.5 Synchroniser le coup avec l'animation

Le coup porte *Attack Windup* secondes après le début de l'attaque. Dans `Data/Enemies/Enemy_Rampant`, règle ce champ sur le moment où l'épée frappe dans `attack_B` (environ `0.5`).

Si le chevalier semble glisser en courant, baisse *Move Speed*. Tu peux aussi monter la *Speed* de l'état `charge` dans l'Animator.

## En cas de problème

- **L'arc ne plie pas** : vérifie le champ *Draw Constraint* de `Bow Visual` et que le composant `Bow Weapon Controller AA` a bien été supprimé.
- **Deux cordes, ou une corde qui traîne** : le `Bow Weapon Controller AA` est encore là.
- **La flèche pointe vers l'archer** : sa Rotation Y n'est pas à 180.
- **Le chevalier avance puis revient en arrière à chaque pas** : *Bake Into Pose* n'est pas coché (étape 3.2).
- **Le chevalier reste en T-pose** : le *Controller* de l'Animator est vide, ou l'avatar n'est pas copié (étape 3.2).
