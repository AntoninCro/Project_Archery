# Guide : le téléporteur de la tour

Ce guide ajoute le téléporteur du GDD (section 9) à la scène `Prototype_Tir` :

- **un cercle au pied de la tour** : reste debout dessus 1 seconde et tu arrives en haut, tourné vers les ennemis ;
- pour redescendre, il suffit de marcher dans le vide. Un **cercle au sommet** est possible mais optionnel.

Le voyage se fait avec un fondu au noir. Après une téléportation, il faut sortir du cercle avant de pouvoir repartir.

Tant que la tour est détruite, les cercles s'éteignent (gris). Si la tour s'effondre pendant que tu es en haut, le cercle du bas te ramène au sol, 1,6 m devant lui (option *Evacuate On Collapse*, cochée par défaut).

À la fin d'une vague, si tu n'es pas en haut de la tour, **le panneau de la boutique vient à côté de toi**. Il n'y a rien à régler pour ça.

Au sol, on se déplace avec le joystick gauche (le *Move* du XR Origin) jusqu'au cercle.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Screen Fader` | Fondu au noir de la vue (crée tout seul un voile devant la caméra) | XR Origin |
| `Player Teleport` | Téléporte le joueur avec le fondu, en passant par le *Teleportation Provider* d'XRI | XR Origin |
| `Tower Teleporter` | Un cercle de téléportation | Chaque cercle |

Les sons sont dans `Audio/Placeholder` : `teleport_charge` (en montant sur le cercle) et `teleport` (à l'arrivée).

## 1. Le fondu au noir

1. Dans `Materials`, crée un matériau `ScreenFade` avec le shader *Archery > ScreenFade*.
2. Sur le `XR Origin (XR Rig)` :
   - *Add Component > Screen Fader* : *Material* `ScreenFade` ;
   - *Add Component > Player Teleport* : *Teleport Clip* `teleport`.

Le matériau garantit que le shader du voile sera inclus dans la build.

## 2. Les points d'arrivée

Un point d'arrivée marque l'endroit où arrivent les pieds du joueur. Sa flèche bleue (axe Z) donne la direction de son regard.

1. *Create Empty* `Tower Top Arrival` : Position `(0, 4, 0)`, Rotation `(0, 0, 0)`. C'est le centre du haut de la tour, regard vers les ennemis.
2. Seulement pour un cercle au sommet : *Create Empty* `Ground Arrival`, Position `(-4, 0, 1.6)`, Rotation `(0, 0, 0)`. C'est au sol, à côté du cercle du bas mais pas dessus.

## 3. Les cercles

1. Crée un matériau `Teleport Pad` (*Universal Render Pipeline/Unlit*, bleu clair). Le script change sa couleur selon l'état : il pulse doucement, s'éclaircit pendant la charge et devient gris si la tour est détruite.
2. Le cercle du bas :
   - *Create Empty* `Teleporter Bottom`, Position `(-4, 0, 0)`, à gauche de la tour ;
   - enfant `Visual` : *3D Object > Cylinder*, Position `(0, 0.01, 0)`, Scale `(1.5, 0.01, 1.5)`, matériau `Teleport Pad`. Supprime son *Capsule Collider* : on marche dessus et les flèches ne doivent pas s'y planter ;
   - sur `Teleporter Bottom` : *Add Component > Tower Teleporter*, avec *Destination* `Tower Top Arrival`, *Visual* `Visual` et *Charge Clip* `teleport_charge`.
   - laisse *Evacuate On Collapse* coché : si la tour s'effondre pendant que tu es en haut, tu arrives au sol 1,6 m devant ce cercle. Tu peux choisir un autre endroit avec *Evacuation Point*.
3. Optionnel, un cercle au sommet pour redescendre :
   - duplique `Teleporter Bottom` (Ctrl+D), renomme-le `Teleporter Top`, Position `(-1.7, 4, -1.7)`, dans le coin arrière gauche du haut de la tour ;
   - dans son `Tower Teleporter` : *Destination* `Ground Arrival`, et **décoche** *Evacuate On Collapse* (c'est le rôle du cercle du bas).

*Radius* (0,75 m) correspond à un cylindre de *Scale* 1,5. Si tu agrandis le cercle, augmente aussi *Radius*.

## 4. Tester

1. Lance Play et descends de la tour en marchant dans le vide (joystick gauche).
2. Marche jusqu'au cercle du bas. Après 1 s, l'image passe au noir et tu reviens en haut, tourné vers les ennemis.
3. Si la tour est détruite pendant une vague, tu es renvoyé au sol et les cercles deviennent gris. Ils se rallument quand tu la reconstruis en boutique.

## En cas de problème

- **Rien ne se passe sur le cercle** :
  - le XR Origin doit avoir `Player Teleport` ;
  - *Destination* doit être rempli ;
  - tes pieds doivent être à moins de *Radius* du centre du cercle ;
  - après une téléportation, sors du cercle puis reviens.
- **Pas de fondu au noir** : le XR Origin doit avoir `Screen Fader` avec le matériau `ScreenFade`.
- **J'arrive à côté ou un peu en l'air** : déplace le point d'arrivée. La gravité du XR Origin te pose ensuite au sol.
- **J'arrive tourné dans la mauvaise direction** : tourne le point d'arrivée (flèche bleue vers où regarder), ou décoche *Face Destination Forward*.
