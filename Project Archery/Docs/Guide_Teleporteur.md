# Guide : le téléporteur de la tour

Ce guide ajoute le téléporteur du GDD (section 9) à la scène `Prototype_Tir` :

- **une bande tout autour du pied de la tour** : reste debout dessus 1 seconde, de n'importe quel côté, et tu arrives en haut, tourné vers les ennemis ;
- pour redescendre, il suffit de marcher dans le vide. En retombant sur la bande, tu ne remontes pas tout de suite : il faut d'abord en sortir. Un **cercle au sommet** est possible mais optionnel ;
- pendant la pause entre deux vagues, le bouton **« Retour à la tour »** de la boutique te ramène en haut directement.

Le voyage se fait avec un fondu au noir. Après une téléportation, il faut sortir de la bande avant de pouvoir repartir.

Tant que la tour est détruite, la bande s'éteint (grise) et le bouton de la boutique se grise. Si la tour s'effondre pendant que tu es en haut, tu es renvoyé au sol, juste à côté de la bande, de ton côté (option *Evacuate On Collapse*, cochée par défaut).

À la fin d'une vague, si tu n'es pas en haut de la tour, **le panneau de la boutique vient à côté de toi**. Il n'y a rien à régler pour ça.

Au sol, on se déplace avec le joystick gauche jusqu'à la bande. Le joystick droit ne sert qu'à tourner : la flèche de téléportation d'XRI est désactivée (section 5).

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Screen Fader` | Fondu au noir de la vue (crée tout seul un voile devant la caméra) | XR Origin |
| `Player Teleport` | Téléporte le joueur avec le fondu, en passant par le *Teleportation Provider* d'XRI | XR Origin |
| `Tower Teleporter` | Une zone de téléportation : une bande autour de la tour, ou un cercle | `Teleporter Bottom` (et le cercle du sommet) |

Les sons sont dans `Audio/Placeholder` : `teleport_charge` (en montant sur la zone) et `teleport` (à l'arrivée).

## 1. Le fondu au noir

1. Dans `Materials`, crée un matériau `ScreenFade` avec le shader *Archery > ScreenFade*.
2. Sur le `XR Origin (XR Rig)` :
   - *Add Component > Screen Fader* : *Material* `ScreenFade` ;
   - *Add Component > Player Teleport* : *Teleport Clip* `teleport`.

Le matériau garantit que le shader du voile sera inclus dans la build.

## 2. Les points d'arrivée

Un point d'arrivée marque l'endroit où arrivent les pieds du joueur. Sa flèche bleue (axe Z) donne la direction de son regard.

1. *Create Empty* `Tower Top Arrival` : Position `(0, 4, 0)`, Rotation `(0, 0, 0)`. C'est le centre du haut de la tour, regard vers les ennemis.
2. Seulement pour un cercle au sommet : *Create Empty* `Ground Arrival`, Position `(-4, 0, 1.6)`, Rotation `(0, 0, 0)`. C'est au sol, au bord de la bande.

## 3. La bande autour de la tour

**C'est déjà fait dans la scène.** Pour le refaire :

1. Crée un matériau `Teleport Pad` (*Universal Render Pipeline/Unlit*, bleu clair). Le script change sa couleur selon l'état : il pulse doucement, s'éclaircit pendant la charge et devient gris si la tour est détruite.
2. *Create Empty* `Teleporter Bottom`, Position `(0, 0, 0)` : au pied de la tour, en son milieu.
3. Son enfant `Visual` : *Create Empty*, Position `(0, 0.02, 0)`, puis *Add Component > Mesh Filter* (laisse *Mesh* vide) et *Add Component > Mesh Renderer*, avec le matériau `Teleport Pad`. En jeu, le script y crée la bande.
4. Sur `Teleporter Bottom` : *Add Component > Tower Teleporter* :
   - *Destination* `Tower Top Arrival` ;
   - *Shape* `Around Tower` ;
   - *Tower Size* `(5, 5)` : la largeur et la profondeur de `Tower` (son *Scale* X et Z) ;
   - *Tower Corner Radius* `0` (tour carrée) ;
   - *Band Width* `1.5` : la largeur de la bande, à partir du mur ;
   - *Visual* `Visual`, *Charge Clip* `teleport_charge` ;
   - laisse *Evacuate On Collapse* coché.

Sélectionne `Teleporter Bottom` : la bande est dessinée en bleu dans la vue *Scene* (elle n'apparaît vraiment qu'en jeu).

**Pour une tour ronde** (carte finale) : *Tower Size* au diamètre de la tour, et *Tower Corner Radius* à la moitié. La bande devient un anneau.

**Optionnel, un cercle au sommet pour redescendre** :
- *Create Empty* `Teleporter Top`, Position `(-1.7, 4, -1.7)`, dans le coin arrière gauche du haut de la tour ;
- enfant `Visual` : *3D Object > Cylinder*, Position `(0, 0.01, 0)`, Scale `(1.5, 0.01, 1.5)`, matériau `Teleport Pad`. Supprime son *Capsule Collider* : on marche dessus et les flèches ne doivent pas s'y planter ;
- *Add Component > Tower Teleporter* : *Shape* `Disc`, *Radius* `0.75` (pour un cylindre de *Scale* 1,5), *Destination* `Ground Arrival`, *Visual* `Visual`, *Charge Clip* `teleport_charge`, et **décoche** *Evacuate On Collapse* (c'est le rôle de la bande).

## 4. Le bouton de la boutique

**C'est déjà fait dans la scène.** `Shop > Content > Tower Button` est un bouton bleu clair en haut à gauche du panneau, en face de l'or :
- copie du bouton `Reroll` : *Anchor* en haut à gauche, Pos `(200, -55)`, taille `360 × 70`, couleur `(153, 209, 255)` ;
- son texte : « Retour à la tour », taille 34.

Sur le *Shop Panel* de `Shop` : *Tower Button* `Tower Button`, *Tower Teleporter* `Teleporter Bottom`.

Le bouton se cache quand tu es déjà en haut de la tour (à moins de 4 m du point d'arrivée, réglage *Arrival Radius* du téléporteur), et se grise tant que la tour est détruite.

## 5. Plus de flèche de téléportation à droite

**C'est déjà fait dans la scène.** Le XR Origin d'XRI affichait une flèche de téléportation quand on poussait le joystick droit vers l'avant, mais elle ne menait nulle part.

Sur `XR Origin (XR Rig) > Camera Offset > Right Controller`, le composant *Controller Input Action Manager* a maintenant *Teleport Mode* et *Teleport Mode Cancel* vides (`None`, en gras : ce sont des modifications de la scène). La rotation au joystick droit marche toujours.

## 6. Tester

1. Lance Play et descends de la tour en marchant dans le vide. Tu retombes sur la bande : rien ne se passe.
2. Sors de la bande, puis reviens-y, de n'importe quel côté. Après 1 s, l'image passe au noir et tu reviens en haut, tourné vers les ennemis.
3. Pousse le joystick droit vers l'avant : plus de flèche.
4. Redescends, tire dans le gong, puis appuie sur **N** pour finir la vague. La boutique a le bouton « Retour à la tour » : clique dessus, tu remontes. En haut, le bouton disparaît.
5. Si la tour est détruite pendant une vague, tu es renvoyé au sol et la bande devient grise. Elle se rallume quand tu la reconstruis en boutique.

## En cas de problème

- **Rien ne se passe sur la bande** :
  - le XR Origin doit avoir `Player Teleport` ;
  - *Destination* doit être rempli ;
  - tes pieds doivent être sur la bande (moins de *Band Width* du mur) ;
  - après une téléportation, ou un saut depuis le haut de la tour, sors de la bande puis reviens.
- **La bande n'apparaît pas** : `Visual` doit avoir un *Mesh Filter* et un *Mesh Renderer*, et être dans le champ *Visual*.
- **La bande rentre dans la tour, ou en est loin** : `Teleporter Bottom` doit être au milieu du pied de la tour, et *Tower Size* égal à sa taille vue de dessus.
- **Pas de fondu au noir** : le XR Origin doit avoir `Screen Fader` avec le matériau `ScreenFade`.
- **J'arrive à côté ou un peu en l'air** : déplace le point d'arrivée. La gravité du XR Origin te pose ensuite au sol.
- **J'arrive tourné dans la mauvaise direction** : tourne le point d'arrivée (flèche bleue vers où regarder), ou décoche *Face Destination Forward*.
- **Le bouton de la boutique n'apparaît pas** : *Tower Button* et *Tower Teleporter* doivent être remplis sur le *Shop Panel*. En haut de la tour, il est caché exprès.
