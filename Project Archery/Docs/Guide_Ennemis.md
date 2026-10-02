# Guide : la tour, les PV du joueur et le premier ennemi

Ce guide te fait monter dans Unity ce que les scripts gèrent déjà : la tour à défendre, les PV du joueur, un ennemi au sol qui marche vers la tour, et un point d'apparition. Tout se fait dans la scène `Assets/_Project/Scenes/Prototype_Tir`.

Les menus sont donnés avec leurs noms anglais, tels qu'ils apparaissent dans Unity.

## Les scripts utilisés

| Script | Rôle | Où le mettre |
|---|---|---|
| `Health` | Points de vie (visibles en jeu dans le champ *Current*) | Tour, joueur, ennemi |
| `Tower` | La tour visée par les ennemis | Tour |
| `Player Health` | Vibrations et voile rouge quand le joueur est touché ; recharge la scène à sa mort | XR Origin |
| `Enemy Definition` | Asset de données : PV, vitesse, dégâts, portée, points | Dossier `Data/Enemies` |
| `Enemy` | Intelligence de l'ennemi : déplacement, attaque, mort | Racine de l'ennemi |
| `Hitbox` | Zone de touche (tête ×2, corps ×1) | Chaque collider de l'ennemi |
| `Damage Popups` | Chiffres de dégâts au-dessus des impacts | Racine de l'ennemi |
| `Enemy Spawner` | Fait apparaître les ennemis | Un objet vide de la scène |

## 1. Installer AI Navigation

Les ennemis se déplacent avec un *NavMesh*, une carte des zones où l'on peut marcher.

1. *Window > Package Manager*.
2. À gauche, *Unity Registry*, puis cherche **AI Navigation**.
3. *Install*.

## 2. La tour (provisoire)

On la remplacera plus tard par un vrai modèle. Pour l'instant, un cube sur lequel tu te tiens suffit.

1. *GameObject > 3D Object > Cube*, renomme-le `Tower`.
2. *Transform* : Position `(0, 2, 0)`, Scale `(5, 4, 5)`.
3. Dans `Assets/_Project/Materials` : clic droit > *Create > Material*, nomme-le `Tower_Stone`, mets une couleur grise, puis glisse-le sur le cube.
4. Sur `Tower` : *Add Component > Health*, mets *Max Health* à `1000`.
5. *Add Component > Tower*. Tu peux mettre `impact_wood` (dans `Audio/Placeholder`) dans *Hit Clip*.
6. Sélectionne `XR Origin (XR Rig)` et mets sa Position à `(0, 4, 0)` : tu te retrouves sur la tour.

## 3. Les PV du joueur

1. Sélectionne `XR Origin (XR Rig)`.
2. *Add Component > Health*, avec *Max Health* à `100`.
3. *Add Component > Player Health*.

**Optionnel : le voile rouge quand tu es touché**

1. Dans la hiérarchie : `XR Origin (XR Rig) > Camera Offset > Main Camera`. Clic droit dessus > *3D Object > Quad*, nomme-le `Damage Overlay`.
2. *Transform* : Position `(0, 0, 0.15)`, Rotation `(0, 0, 0)`, Scale `(1, 1, 1)`. Supprime son composant *Mesh Collider*.
3. Crée un matériau `Damage_Overlay` : *Shader* `Universal Render Pipeline/Unlit`, *Surface Type* `Transparent`. Glisse-le sur le quad.
4. Glisse le quad dans le champ *Damage Overlay* de `Player Health`.

## 4. Le premier ennemi : le Rampant (en formes simples)

### Les données

1. Dans `Assets/_Project/Data` : clic droit > *Create > Folder*, nomme-le `Enemies`.
2. Dedans : clic droit > *Create > Archery > Enemy Definition*, nomme-le `Enemy_Rampant`.
3. Garde les valeurs par défaut du GDD : 30 PV, 2,5 m/s, 10 dégâts, portée 1,2 m, une attaque toutes les 1,5 s, attaque le joueur à moins de 8 m, 10 points.

### L'objet

1. Dans la hiérarchie : clic droit > *Create Empty*, nomme-le `Enemy_Rampant`, Position `(0, 0, 15)`.
2. Ajoute-lui ces composants :
   - *Nav Mesh Agent* : *Radius* `0.4`, *Height* `1.9`. La vitesse est réglée par le script.
   - *Rigidbody* : coche *Is Kinematic*, décoche *Use Gravity*. Il sert à ce que tous les colliders de l'ennemi forment un seul corps.
   - *Health* : la valeur sera remplacée par celle de `Enemy_Rampant`.
   - *Enemy* : glisse `Enemy_Rampant` (l'asset de données) dans *Definition*.
   - *Damage Popups* : glisse `Data/ShotTuning` dans *Shot Tuning*.

### Le corps et la tête

1. Clic droit sur `Enemy_Rampant` > *3D Object > Capsule*, nomme-la `Body`. Position `(0, 0.75, 0)`, Scale `(0.6, 0.75, 0.6)`. Garde son *Capsule Collider*.
   - *Add Component > Hitbox* : *Zone* `Body`, *Damage Multiplier* `1`.
   - Crée un matériau `Enemy_Skin` (vert, par exemple) et mets-le dessus.
2. Clic droit sur `Enemy_Rampant` > *3D Object > Sphere*, nomme-la `Head`. Position `(0, 1.72, 0)`, Scale `(0.42, 0.42, 0.42)`. Garde son *Sphere Collider*.
   - *Add Component > Hitbox* : *Zone* `Head`, *Damage Multiplier* `2`, *Hit Clip* `headshot_ding`.
3. Optionnel, des yeux : deux petites sphères *enfants de* `Head`, Scale `(0.2, 0.2, 0.2)`, Position locale `(±0.2, 0.1, 0.42)`. **Supprime leurs colliders**, sinon les flèches s'y planteraient sans faire de dégâts.

### Le prefab

1. Glisse `Enemy_Rampant` de la hiérarchie vers `Assets/_Project/Prefabs` : le prefab est créé.
2. Supprime l'ennemi de la scène, sinon il serait compté comme un obstacle au moment de calculer le NavMesh.

## 5. Le NavMesh

1. Sélectionne `Ground`, puis *Add Component > NavMesh Surface*.
2. *Agent Type* `Humanoid`, *Collect Objects* `All Game Objects`, *Use Geometry* `Physics Colliders`.
3. Clique sur *Bake*. Une surface bleue apparaît dans la vue Scene (si les gizmos sont activés), avec des trous autour de la tour, des cibles et des mannequins.

Si tu déplaces la tour ou des obstacles, refais *Bake*.

## 6. Le point d'apparition

1. *Create Empty*, nomme-le `Enemy Spawner`.
2. *Add Component > Enemy Spawner* :
   - *Enemy Prefab* : le prefab `Enemy_Rampant` (depuis le dossier `Prefabs`, pas depuis la scène) ;
   - *Spawn Interval* `3`, *Max Alive* `6`.
3. Crée trois objets vides enfants du spawner : `Spawn A` en `(-15, 0, 40)`, `Spawn B` en `(0, 0, 45)` et `Spawn C` en `(15, 0, 40)`.
4. Glisse-les dans la liste *Spawn Points*. Pour les ajouter d'un coup, sélectionne-les tous et glisse-les sur le titre de la liste.

## 7. Tester

Lance Play :

- toutes les 3 s, un ennemi apparaît et marche vers la tour ;
- arrivé au pied de la tour, il la frappe : le champ *Current* du `Health` de la tour baisse dans l'Inspector ;
- chaque flèche qui le touche affiche ses dégâts, doublés à la tête ;
- à 0 PV, il bascule, s'enfonce dans le sol et disparaît.

Pour tester les attaques contre toi, remets le XR Origin au sol, par exemple en `(0, 0, -8)` derrière la tour : un ennemi à moins de 8 m te vise à la place de la tour. À 0 PV, « Tu es mort ! » s'affiche et la scène recommence.

**Si les ennemis n'apparaissent pas ou restent immobiles :**

- regarde la console : « pas de NavMesh près de… » veut dire qu'il faut refaire *Bake* ;
- vérifie que l'*Enemy Prefab* du spawner est bien le prefab et que le champ *Definition* est rempli.

## 8. Plus tard : remplacer les formes par un vrai monstre

À faire quand le comportement te convient. Par exemple avec le pack gratuit *Ultimate Monsters* de Quaternius (licence CC0), au format FBX.

1. Range le modèle et ses textures dans `Assets/_Project/Art/Monsters/<Nom>/`.
2. Sélectionne le FBX :
   - onglet *Rig* : *Animation Type* `Generic`, puis *Apply* ;
   - onglet *Animation* : coche *Loop Time* pour la marche et l'attente, puis *Apply*.
3. Crée un *Animator Controller* `Rampant` (clic droit > *Create > Animator Controller*) et ouvre-le :
   - ajoute les paramètres `Speed` (Float), `Attack`, `Hit` et `Die` (Trigger) : le script `Enemy` les utilise s'ils existent ;
   - glisse les animations d'attente, de marche, d'attaque, de coup reçu et de mort dans le graphe, puis mets l'attente en état par défaut (clic droit > *Set as Layer Default State*) ;
   - attente → marche si `Speed` > 0.1, marche → attente si `Speed` < 0.1, sans *Has Exit Time* ;
   - *Any State* → attaque avec `Attack`, *Any State* → coup reçu avec `Hit`, *Any State* → mort avec `Die`. Décoche *Can Transition To Self* sur ces trois transitions ;
   - attaque → attente et coup reçu → attente avec *Has Exit Time*. La mort n'a pas de sortie.
4. Ouvre le prefab `Enemy_Rampant` (double-clic) :
   - glisse le modèle en enfant de la racine, Position `(0, 0, 0)` ; ajuste sa taille vers 1,8 m ;
   - sur son *Animator* : *Controller* `Rampant`, décoche *Apply Root Motion* ;
   - déplace le collider et la `Hitbox` de la tête sur l'os de la tête du modèle, et cache les formes simples (décoche leur *Mesh Renderer*).

Le script `Enemy` trouve l'Animator tout seul. Sans animation de mort, l'ennemi bascule simplement en arrière.
