# Guide : le boss des vagues 5, 10, 15…

Ce guide ajoute le boss du GDD (sections 3 et 10) à la scène `Prototype_Tir`.

Les vagues 5, 10, 15… sont des **vagues de boss** :
- au début de la vague, « Un boss approche ! » s'affiche ; 4 s plus tard, le boss arrive avec un cor et des tambours ;
- c'est un chevalier géant, lent, qui frappe fort la tour : 1 500 PV, multipliés par la difficulté et par la courbe des vagues, comme les autres ennemis (GDD, section 11). En Normal, il a environ 2 000 PV à la vague 5 et 3 000 à la vague 10 ;
- il a **3 points faibles lumineux** (poitrine et épaules) : chaque touche y fait **×3 dégâts** et rapporte 15 points ;
- toutes les 14 s, il **appelle 2 Rampants** en renfort ;
- une barre de PV flotte au-dessus de lui.

Le **chrono n'arrête que les renforts**. À 0:00, « Plus de renforts : abats le boss ! » s'affiche et la montre indique « Boss ». La vague se termine à la mort du boss, même avant la fin du chrono : « Boss vaincu ! ». Le boss de la vague 10 donne la victoire.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Boss` | Renforts, annonce de l'arrivée, nom et numéro du boss | Racine du prefab du boss |
| `Health Bar` | Barre de PV tournée vers le joueur | Canvas au-dessus du boss |
| `Pulse` | Fait « respirer » les points faibles | Chaque point faible |
| `Hitbox` (déjà là) | Zone *WeakPoint*, dégâts ×3 | Chaque point faible |

Le boss est un `Enemy` comme les autres : il garde toute la logique du chevalier (marche, attaque, difficulté…).

Les sons sont dans `Audio/Placeholder` : `boss_arrival` (arrivée) et `boss_summon` (renforts).

## 1. Les caractéristiques du boss

Dans `Data/Enemies` : clic droit > *Create > Archery > Enemy Definition*, nomme-le `Enemy_Boss`.

| Champ | Valeur |
|---|---|
| *Display Name* | `Seigneur de guerre` |
| *Max Health* | `1500` |
| *Move Speed* | `1` |
| *Attack Damage* | `60` |
| *Attack Range* | `2.8` |
| *Attack Interval* | `2.5` |
| *Attack Windup* | `0.9` |
| *Player Aggro Range* | `0`, pour qu'il vise toujours la tour |
| *Points* | `250` |

## 2. Le prefab du boss

On fait une **variante** du chevalier. Elle hérite de tout ce qui est réglé sur `Enemy_Rampant` (animations, zones de touche…), et on ne change que ce qui diffère.

1. Dans `Prefabs`, clic droit sur `Enemy_Rampant` > *Create > Prefab Variant*, nomme-la `Enemy_Boss`, puis ouvre-la.
2. Sur la racine :
   - Scale `(2.2, 2.2, 2.2)` ;
   - dans `Enemy`, *Definition* : `Enemy_Boss`.
3. Pour le reconnaître, donne-lui une autre couleur :
   - duplique le matériau du chevalier `DemoMat` (Ctrl+D), nomme-le `Boss_Knight` et donne-lui une couleur rouge sombre ;
   - mets-le sur le *Skinned Mesh Renderer* du modèle.
4. *Add Component > Boss* sur la racine :
   - *Summon Prefab* : `Enemy_Rampant` ;
   - *Summon Clip* : `boss_summon`, *Arrival Clip* : `boss_arrival` ;
   - *Health Growth Per Boss* : laisse `0`. La courbe des vagues rend déjà chaque boss plus coriace que le précédent.

### Les points faibles

1. Crée un matériau `Weak_Point` : *Universal Render Pipeline/Unlit*, cyan vif. Il ressort bien sur l'armure rouge.
2. Dans l'os `Bip001 Spine`, ajoute une *3D Object > Sphere*, nommée `Weak Point` :
   - garde son *Sphere Collider* : c'est lui que les flèches touchent ;
   - matériau `Weak_Point` ;
   - dans la vue Scene, place-la sur la poitrine, à peine sortie de l'armure, et règle sa taille à environ 30 cm. Les os ont leur propre échelle : ajuste à l'œil plutôt qu'en tapant des valeurs ;
   - *Add Component > Hitbox* : *Zone* `WeakPoint`, *Damage Multiplier* `3`, *Hit Clip* `headshot_ding` ;
   - *Add Component > Pulse*.
3. Duplique ce point faible dans `Bip001 L UpperArm` et `Bip001 R UpperArm` (les épaules).

Un point faible passe avant la tête et le corps quand une flèche les traverse à la suite.

### La barre de PV

1. Sur la racine de `Enemy_Boss` : *UI > Canvas*, nomme-le `Health Bar` :
   - *Render Mode* `World Space` ;
   - Position `(0, 2.3, 0)`, juste au-dessus de la tête ;
   - Width `300`, Height `36`, Scale `(0.004, 0.004, 0.004)` ;
   - supprime son *Graphic Raycaster* : on ne clique pas dessus.
2. Dans `Health Bar` :
   - `Background` : *UI > Image*, étirée sur tout le Canvas, noire, alpha 180 ;
   - `Fill` : *UI > Image*, étirée sur tout le Canvas, rouge. Le script raccourcit l'image par la droite quand le boss perd des PV ;
   - `Name` : *Text - TextMeshPro*, ancre en haut au centre, position `(0, 30)`, 300 × 40, taille 28, centré, texte « Seigneur de guerre ».
3. Sur `Health Bar` : *Add Component > Health Bar*, *Fill* : `Fill`.

## 3. Les réglages des vagues

Dans `Data/Waves/WaveSettings`, partie *Boss* :
- *Boss Prefab* : `Enemy_Boss` ;
- *Boss Every* `5`, *Boss Spawn Delay* `4` ;
- *Boss Wave Budget* `0.6` : il y a 40 % de Rampants ordinaires en moins pendant une vague de boss, car il appelle les siens.

## 4. La montre et le grand panneau (optionnel)

`Hud Display` a deux nouveaux champs optionnels :
- *Boss Text* : un texte qui affiche « Seigneur de guerre 1 234/1 500 » ;
- *Boss Health Bar* : une Image de type *Filled*.

Ils sont vides sans boss. Tu peux par exemple ajouter un texte `Boss` au grand panneau `Scoreboard`.

## 5. Tester

1. Lance Play, puis appuie 9 fois sur **N** : tu arrives au début de la vague 5. La montre affiche « Vague 5 / 10 · Boss ».
2. 4 s plus tard, le boss arrive, avec son cor et « Seigneur de guerre arrive ! ».
   - Tire dans les sphères cyan : gros chiffres de dégâts et « ding ».
   - Toutes les 14 s, « Renforts ! » s'affiche et 2 Rampants sortent du sol à côté de lui.
3. **N** une fois : fin des renforts, et la montre affiche « Boss ».
4. **N** une deuxième fois : le boss meurt. « Boss vaincu ! » s'affiche, puis la boutique s'ouvre.

## En cas de problème

- **Le boss n'arrive pas** : *Boss Prefab* est vide dans `WaveSettings`, ou la console affiche « le boss n'a pas pu apparaître » (pas de spawner, ou pas de NavMesh près des points d'apparition).
- **Le boss s'enfonce dans le sol ou flotte** : vérifie que la racine de la variante est bien à l'échelle 2,2 et pas le modèle seul. Sinon, le *NavMeshAgent* reste à la taille du chevalier.
- **Les points faibles ne font pas plus de dégâts** : la sphère doit avoir son *Sphere Collider* et une `Hitbox` en *WeakPoint* avec *Damage Multiplier* 3.
- **La console dit « le collider … n'a pas de Hitbox »** : un collider sans `Hitbox` traîne dans le boss (par exemple une sphère de décor). Supprime son collider ou ajoute-lui une `Hitbox`.
- **La barre de PV ne bouge pas** : *Fill* doit être branché, et l'image `Fill` étirée sur toute la barre (ancres à gauche et à droite).
