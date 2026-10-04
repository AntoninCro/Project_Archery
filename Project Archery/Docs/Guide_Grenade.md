# Guide : la grenade de flèches

Ce guide ajoute la grenade de flèches, une idée de fin de projet du GDD (section 23).

- **La prendre** : passe une main vide **en bas de ton dos**, derrière les hanches, et serre la poignée, comme pour une flèche dans le dos. Une grenade arrive dans ta main.
- **La lancer** : lâche la poignée en faisant le geste de lancer.
- **L'explosion** : elle éclate au premier choc, ou 1,5 s après le lancer, et projette **24 flèches** tout autour d'elle, sur deux couronnes. Chaque flèche fait les dégâts d'un tir orange de ton arc, et ne casse pas le combo.
- **La recharge** : 30 s, à partir du moment où tu la prends. Une vibration et « Grenade prête » annoncent la suivante. La montre peut afficher le temps restant.

Avant la fin de la recharge, la poignée en bas du dos ne donne rien : juste une petite vibration.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Arrow Grenade` | La grenade : explosion au choc ou au bout du délai, couronnes de flèches | Racine du prefab de la grenade |
| `Grenade Holster` | L'étui invisible en bas du dos, et la recharge | XR Origin, à côté du `Quiver` |
| `Hud Display` (déjà là) | Nouveau champ optionnel : *Grenade Text* | Montre |

Les sons sont dans `Audio/Placeholder` : `explosion` (déjà là), `quiver_draw` (déjà là) et `grenade_ready`.

## 1. Le prefab de la grenade

Une sphère hérissée de pointes de flèches, en formes simples.

1. *Create Empty*, nomme-le `Arrow Grenade`, en `(0, 0, 0)`.
2. Sur `Arrow Grenade`, ajoute dans l'ordre :
   - *Sphere Collider* : *Radius* `0.08` ;
   - *Rigidbody* : *Mass* `0.5`, *Collision Detection* `Continuous Dynamic` (elle va vite) ;
   - *XR Grab Interactable* : vérifie que *Throw On Detach* est coché ;
   - *Arrow Grenade* : *Explosion Clip* `explosion`. Pour *Explosion Effect*, tu peux reprendre le prefab `Explosion` du guide de la boutique.
3. Ses enfants, **sans collider** :

| Nom | Création | Position | Rotation | Scale | Matériau |
|---|---|---|---|---|---|
| `Core` | *Sphere* | `(0, 0, 0)` | `(0, 0, 0)` | `(0.14, 0.14, 0.14)` | `Brazier_Coal` (ou un gris sombre) |
| `Spike X+` | *Cylinder* | `(0.09, 0, 0)` | `(0, 0, 90)` | `(0.015, 0.04, 0.015)` | `Chest_Metal` (ou un doré) |
| `Spike X-` | *Cylinder* | `(-0.09, 0, 0)` | `(0, 0, 90)` | `(0.015, 0.04, 0.015)` | idem |
| `Spike Y+` | *Cylinder* | `(0, 0.09, 0)` | `(0, 0, 0)` | `(0.015, 0.04, 0.015)` | idem |
| `Spike Y-` | *Cylinder* | `(0, -0.09, 0)` | `(0, 0, 0)` | `(0.015, 0.04, 0.015)` | idem |
| `Spike Z+` | *Cylinder* | `(0, 0, 0.09)` | `(90, 0, 0)` | `(0.015, 0.04, 0.015)` | idem |
| `Spike Z-` | *Cylinder* | `(0, 0, -0.09)` | `(90, 0, 0)` | `(0.015, 0.04, 0.015)` | idem |

4. Glisse `Arrow Grenade` dans `Prefabs`, puis supprime-le de la scène.

## 2. L'étui en bas du dos

Sur le `XR Origin (XR Rig)` : *Add Component > Grenade Holster* :
- *Grenade Prefab* : `Arrow Grenade` ;
- *Draw Clip* : `quiver_draw` ;
- *Ready Clip* : `grenade_ready`.

Sélectionne le XR Origin pendant le *Play* : une boîte orange montre la zone de prise, sous celle du carquois.

## 3. La recharge sur la montre (optionnel)

1. Dans `Wrist HUD` > `Background`, ajoute un texte `Grenade` (taille 22).
2. Glisse-le dans le champ *Grenade Text* du `Hud Display`. Il affiche « Grenade prête » ou « Grenade · 12 s ».

## 4. Tester

1. Lance Play et descends de la tour, devant quelques chevaliers.
2. Main vide derrière les hanches, serre la poignée : une grenade arrive dans ta main, avec un petit son.
3. Lance-la vers les chevaliers : elle éclate au sol et projette une couronne de flèches.
4. Réessaie tout de suite : rien, juste une petite vibration. La montre affiche le temps restant.
5. Au bout de 30 s : vibration et « Grenade prête ».

## En cas de problème

- **Rien n'arrive dans la main** :
  - la main doit être vide (pas d'arc, pas de flèche) ;
  - elle doit être dans la boîte orange (section 2). Agrandis *Grab Zone Min* et *Max* si besoin ;
  - la grenade doit être rechargée.
- **Elle explose dans la main** : son collider touche l'arc ou un objet de la scène au moment de la prise. Elle n'explose qu'après avoir été lâchée, au premier choc ou au bout de 1,5 s : vérifie qu'elle n'a pas été lâchée par erreur.
- **Elle tombe au lieu d'être lancée** : *Throw On Detach* doit être coché sur son *XR Grab Interactable*.
- **Elle traverse le sol** : *Collision Detection* `Continuous Dynamic` sur son *Rigidbody*.
- **Trop forte ou trop faible** : *Arrow Count*, *Arrow Speed* et *Grade* sur `Arrow Grenade` ; *Cooldown* sur `Grenade Holster`.
