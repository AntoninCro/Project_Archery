# Guide : les barricades et le brasero

Ce guide ajoute les deux défenses de la tour du GDD (sections 6.2, 7 et 9) à la scène `Prototype_Tir`.

> **Où en est ce guide** (6 octobre) : tout est monté par Claude. Il te reste à **tester** (section 5). Les sections 1 à 4 décrivent ce qui est en place, pour le vérifier ou le retoucher.

**Les barricades** sont des murs de planches aux entrées de la clairière, au bout des chemins :
- un ennemi au sol qui arrive devant une barricade, côté forêt, **s'arrête pour la frapper** ;
- il ne passe qu'une fois qu'elle est **détruite** (400 PV chacune) ;
- les tireurs la cassent avec leurs projectiles ; les volants passent au-dessus ;
- en boutique, la carte **« Réparer les barricades »** (60 or) les remet toutes debout, avec tous leurs PV ;
- elles sont debout au début de la partie.

**Le brasero** est un feu en haut de la tour, **allumé dès le début de la partie** : il ne s'achète pas.
- On y trempe la **pointe d'une flèche** tenue en main : elle s'enflamme, avec un « fff » et une petite vibration.
- L'ennemi touché par une flèche enflammée **brûle 3 s** et perd **30 % des dégâts de la flèche en plus**, étalés sur ces 3 s (un peu toutes les 0,5 s). Une flèche qui fait 50 de dégâts en ajoute donc 15 par la brûlure.
- Chaque flèche enflammée ajoute sa propre brûlure : deux flèches, deux brûlures.
- Avec le multitir, l'écho ou le déluge, **les flèches en plus d'un tir enflammé brûlent aussi**.
- Un ennemi tué par la brûlure rapporte ses points comme s'il avait été tué par la flèche.

En mode infini, le prix de la réparation des barricades monte de 20 % par vague, comme le reste de la boutique.

## Les scripts

| Script | Rôle | Où il est |
|---|---|---|
| `Barricade` | Un mur qui arrête les ennemis au sol jusqu'à sa destruction | Racine du prefab `Barricade` |
| `Brazier` | Le feu : flèches trempées, brûlure des ennemis | Racine du prefab `Brazier` |
| `Burning` | La brûlure d'un ennemi | Ajouté en jeu : rien à faire |
| `Health Bar` (déjà là) | Barre de PV au-dessus de chaque barricade. Elle se cache quand la barricade est détruite et revient quand on la répare | `Barricade > Health Bar` |
| `Shop Panel` (déjà là) | Une carte de plus : *Barricade Card* | Canvas `Shop` |

Les sons sont dans `Audio/Placeholder` : `barricade_build`, `barricade_break`, `fire_loop` (boucle du feu) et `arrow_ignite`.

## 1. Les matériaux

Dans `Materials` :

| Matériau | Shader | Réglages |
|---|---|---|
| `Barricade_Wood` | *Universal Render Pipeline/Lit* | brun clair `(140, 95, 55)` |
| `Brazier_Stone` | *Universal Render Pipeline/Lit* | gris `(110, 110, 115)` |
| `Brazier_Coal` | *Universal Render Pipeline/Lit* | presque noir `(30, 25, 25)` |
| `Fire_Particles` | *Universal Render Pipeline/Particles/Unlit* | *Surface Type* `Transparent`, *Blending Mode* `Additive`, *Base Map* : la texture `Default-Particle` (petit rond flou) |

## 2. Les barricades

### Le prefab `Barricade`

- Racine `Barricade` : *Health* (*Max Health* `400`) et *Barricade* (*Intact* `Planks`, *Broken* `Debris`, *Block Radius* `5`, *Break Clip* `barricade_break`, *Build Clip* `barricade_build`).
- `Planks` : 3 poteaux et 3 planches (cubes en `Barricade_Wood`, avec leur *Box Collider*). Le mur fait 4 m de large et 1,6 m de haut ; les planches sont côté forêt, vers `z` positif.
- `Debris` : 5 planches couchées au sol, sans collider. Caché au départ, il s'affiche quand la barricade est détruite.
- `Health Bar` : la barre de PV du boss, à 2,1 m de haut, brune quand la barricade est neuve et rouge quand elle va céder.

### Leur place dans la scène

Les 4 barricades sont déjà aux entrées de l'ancienne clairière. Leur flèche bleue (axe Z) pointe vers la forêt. **Avec la carte finale à trois voies**, il n'en reste que 3, déplacées sur les voies : voir `Guide_Carte.md`, section 7.

| Objet | Position | Rotation Y |
|---|---|---|
| `Barricade Nord` | `(0, 0, 36)` | `0` |
| `Barricade Est` | `(36, 0, 0)` | `90` |
| `Barricade Sud` | `(0, 0, -36)` | `180` |
| `Barricade Ouest` | `(-36, 0, 0)` | `270` |

**Dans le prototype**, les ennemis sortent des trois points d'apparition du nord et marchent tout droit vers la tour. Seuls ceux de `Spawn B`, au milieu, passent par `Barricade Nord`. Les autres passent 10 m à côté. Avec la carte finale, les 4 chemins les obligeront à passer par les barricades.

Sélectionne une barricade : un cercle orange montre sa zone (5 m, réglage *Block Radius*), avec un trait vers la forêt. Un ennemi au sol qui entre dans ce cercle, du côté de la forêt, s'arrête pour frapper le mur. Ceux qui sont déjà dans la clairière ne sont pas concernés.

Si un chemin est plus large que 4 m, agrandis le *Scale X* de sa barricade.

## 3. Le brasero

### Les effets de feu

Trois *Particle Systems* au matériau `Fire_Particles`, en *Simulation Space* `World` : en vol, les flammes d'une flèche font une traînée de feu derrière elle.

| Effet | Où | *Shape* | *Start Lifetime* | *Start Speed* | *Start Size* | *Emission* |
|---|---|---|---|---|---|---|
| `Flames` | dans le brasero, `Brazier > Fire` | *Cone*, angle 15, rayon 0,25 | 0,6 à 1 | 0,8 à 1,5 | 0,25 à 0,5 | 40 / s |
| `Arrow Fire` | prefab : la pointe d'une flèche enflammée | *Cone*, angle 10, rayon 0,02 | 0,3 à 0,5 | 0,2 à 0,4 | 0,05 à 0,12 | 30 / s, et 5 par mètre parcouru (*Rate over Distance*) |
| `Burn Fire` | prefab : un ennemi qui brûle | *Sphere*, rayon 0,4 | 0,4 à 0,8 | 0,5 à 1 | 0,2 à 0,4 | 25 / s |

Pour les trois :
- *Start Color* orange `(255, 150, 50)` ;
- *Color over Lifetime* : de jaune-orange à rouge, avec l'opacité qui tombe à 0 ;
- *Size over Lifetime* : de 1 vers 0,3 ;
- *Velocity over Lifetime* : *Y* `0.5`, *Space* `World`, pour que les flammes montent toujours, même sur une flèche penchée.

Les cônes pointent vers le haut grâce à la rotation de leur *Shape* (`-90` en X).

### Le prefab `Brazier`

Il est en haut de la tour, au coin nord-est de la plateforme : `Brazier` en `(1.8, 4, 1.8)`.

| Enfant | Ce que c'est |
|---|---|
| `Base` | cylindre en `Brazier_Stone`, avec son collider |
| `Bowl` | coupe aplatie en `Brazier_Coal` |
| `Fire` | le feu, toujours allumé : les flammes (`Flames`), une lumière orange (*Range* `6`, *Intensity* `3`) et le son `fire_loop` en boucle (*Audio Source* sur `Fire`) |
| `Dip Point` | le cœur des flammes, à 1,05 m : c'est là qu'on trempe la flèche |

Le composant *Brazier* : *Fire* `Fire`, *Dip Point* `Dip Point`, *Arrow Fire Effect* `Arrow Fire`, *Burn Effect* `Burn Fire`, *Burn Duration* `3`, *Burn Damage* `0.3` (30 % des dégâts de la flèche, en tout), *Ignite Clip* `arrow_ignite`.

## 4. La carte de la boutique

La boutique a une carte de plus, « Réparer les barricades » :
- dans `Cards`, `Barricade Card` est une copie de la carte de la tour, juste après `Tower Card` ;
- elle est branchée sur le *Shop Panel* de `Shop` : *Barricade Card*.

La grille place les 4 améliorations sur la première rangée ; l'arc, la tour, les barricades et la relance sur la deuxième.

## 5. Tester

**Brasero** :
1. Lance Play : le feu brûle déjà en haut de la tour, au coin nord-est.
2. Prends une flèche dans ton dos et plonge sa pointe dans les flammes : elle s'enflamme.
3. Tire sur un chevalier :
   - il brûle 3 s, avec de petits chiffres de dégâts toutes les 0,5 s : en tout, 30 % des dégâts de la flèche ;
   - avec un Multitir, toutes les flèches de l'éventail brûlent.

**Barricades** :
1. Lance une vague et regarde vers le nord, au bout de la clairière : les chevaliers qui passent par `Barricade Nord` s'arrêtent devant le mur et le frappent. Sa barre de PV baisse, puis ils passent quand elle casse, avec un bruit de bois.
2. À la pause, la carte « Réparer les barricades » affiche leurs PV : achète-la. La barricade se relève, avec sa barre de PV.

**La boutique** : la carte des barricades est sur la deuxième rangée, entre celle de la tour et la relance.

## En cas de problème

- **Les ennemis passent à travers la barricade** :
  - dans le prototype, seuls ceux qui viennent de `Spawn B` passent par une barricade (section 2) ;
  - sa flèche bleue doit pointer vers la forêt ;
  - le mur doit couvrir toute la largeur du chemin ;
  - un ennemi qui vient d'un côté, hors du cercle orange, n'est pas arrêté : agrandis *Block Radius*.
- **Les ennemis frappent dans le vide** : *Intact* doit être `Planks`, avec les colliders des planches.
- **La barricade ne se relève pas** : *Intact* doit être renseigné ; c'est lui qui s'affiche et se cache.
- **La flèche ne s'enflamme pas** :
  - c'est la **pointe** qu'il faut tremper, à moins de 30 cm de `Dip Point` (sphère orange quand le brasero est sélectionné).
- **Les flammes sont des carrés roses** : le matériau `Fire_Particles` doit utiliser un shader *Universal Render Pipeline/Particles*.
- **La brûlure est trop forte ou trop faible** : *Burn Damage* (part des dégâts de la flèche) et *Burn Duration* sur `Brazier`.
