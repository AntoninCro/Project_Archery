# Guide : les barricades et le brasero

Ce guide ajoute les deux défenses de la tour du GDD (sections 6.2, 7 et 9) à la scène `Prototype_Tir`.

**Les barricades** sont des murs de planches aux entrées de la clairière, au bout des chemins :
- un ennemi au sol qui arrive devant une barricade, côté forêt, **s'arrête pour la frapper** ;
- il ne passe qu'une fois qu'elle est **détruite** (400 PV chacune) ;
- les tireurs la cassent avec leurs projectiles ; les volants passent au-dessus ;
- en boutique, la carte **« Réparer les barricades »** (60 or) les remet toutes debout, avec tous leurs PV ;
- elles sont debout au début de la partie.

**Le brasero** est un feu en haut de la tour :
- il s'achète **une seule fois** en boutique (100 or) ;
- on y trempe la **pointe d'une flèche** tenue en main : elle s'enflamme, avec un « fff » et une petite vibration ;
- l'ennemi touché par une flèche enflammée **brûle 3 s**. Il perd chaque seconde 30 % des dégâts de la flèche, soit 90 % en plus au total ;
- avec le multitir, l'écho ou le déluge, **les flèches en plus d'un tir enflammé brûlent aussi** ;
- un ennemi tué par la brûlure rapporte ses points comme s'il avait été tué par la flèche.

En mode infini, ces deux prix montent de 20 % par vague, comme le reste de la boutique.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Barricade` | Un mur qui arrête les ennemis au sol jusqu'à sa destruction | Racine de chaque barricade |
| `Brazier` | Le feu : achat, flèches trempées, brûlure des ennemis | Racine du brasero |
| `Burning` | La brûlure d'un ennemi | Ajouté en jeu : rien à faire |
| `Shop Panel` (déjà là) | Deux nouvelles cartes optionnelles : *Barricade Card* et *Brazier Card* | Canvas de la boutique |

Les sons sont dans `Audio/Placeholder` : `barricade_build`, `barricade_break`, `fire_loop` (boucle du feu), `brazier_build` et `arrow_ignite`.

## 1. Les matériaux

Dans `Materials` :

| Matériau | Shader | Réglages |
|---|---|---|
| `Barricade_Wood` | *Universal Render Pipeline/Lit* | brun clair `(140, 95, 55)` |
| `Brazier_Stone` | *Universal Render Pipeline/Lit* | gris `(110, 110, 115)` |
| `Brazier_Coal` | *Universal Render Pipeline/Lit* | presque noir `(30, 25, 25)` |
| `Fire_Particles` | *Universal Render Pipeline/Particles/Unlit* | *Surface Type* `Transparent`, *Blending Mode* `Additive`, *Base Map* : la texture `Default-Particle` (petit rond flou) |

## 2. Une barricade

### Le prefab

1. *Create Empty*, nomme-le `Barricade`, en `(0, 0, 0)`.
2. *Add Component > Health* : *Max Health* `400`.
3. Enfant `Planks` (*Create Empty*), en `(0, 0, 0)`. Dedans, des cubes au matériau `Barricade_Wood`, qui gardent tous leur *Box Collider* :

| Nom | Position | Scale |
|---|---|---|
| `Post L` | `(-1.8, 0.8, 0)` | `(0.2, 1.6, 0.2)` |
| `Post M` | `(0, 0.8, 0)` | `(0.2, 1.6, 0.2)` |
| `Post R` | `(1.8, 0.8, 0)` | `(0.2, 1.6, 0.2)` |
| `Plank Low` | `(0, 0.45, 0.12)` | `(4, 0.25, 0.08)` |
| `Plank Mid` | `(0, 0.9, 0.12)` | `(4, 0.25, 0.08)` |
| `Plank High` | `(0, 1.35, 0.12)` | `(4, 0.25, 0.08)` |

   Le mur fait 4 m de large. Les planches sont côté forêt : `z` positif.
4. Optionnel : un enfant `Debris`, quelques planches posées au sol en vrac (cubes tournés, **sans** collider). Il s'affiche quand la barricade est détruite.
5. Sur `Barricade` : *Add Component > Barricade* :
   - *Intact* : `Planks` ;
   - *Broken* : `Debris` (si tu l'as fait) ;
   - *Break Clip* `barricade_break`, *Build Clip* `barricade_build`.
6. Optionnel, une barre de PV comme celle du boss (voir `Guide_Boss.md`, « La barre de PV ») : Canvas `Health Bar` en `(0, 2.2, 0)`, Scale `(0.004, 0.004, 0.004)`, avec `Background`, `Fill` et le composant `Health Bar`. Il trouve tout seul le *Health* de la barricade.
7. Glisse `Barricade` dans `Prefabs`.

### Les placer

1. Place une barricade à **chacune des 4 entrées** de la clairière, là où les chemins débouchent.
2. Tourne chacune pour que sa **flèche bleue (axe Z) pointe vers la forêt**, d'où viennent les ennemis.
3. Le mur doit couvrir **toute la largeur du chemin**. Si un chemin est plus large, agrandis le *Scale X* de la racine.

Sélectionne une barricade : un cercle orange montre sa zone (5 m, réglage *Block Radius*), avec un trait vers la forêt. Un ennemi au sol qui entre dans ce cercle, du côté de la forêt, s'arrête pour frapper le mur. Ceux qui sont déjà dans la clairière ne sont pas concernés.

## 3. Le brasero

### Les effets de feu

Trois *Particle Systems* (*GameObject > Effects > Particle System*), tous avec le matériau `Fire_Particles` (module *Renderer*) et *Simulation Space* `World` :

| Effet | *Shape* | *Start Lifetime* | *Start Speed* | *Start Size* | *Emission* | Usage |
|---|---|---|---|---|---|---|
| `Flames` | *Cone*, angle 15, rayon 0,25 | 0,6 à 1 | 0,8 à 1,5 | 0,25 à 0,5 | 40 / s | le feu du brasero |
| `Arrow Fire` | *Cone*, angle 10, rayon 0,02 | 0,3 à 0,5 | 0,2 à 0,4 | 0,05 à 0,12 | 30 / s | la pointe d'une flèche enflammée |
| `Burn Fire` | *Sphere*, rayon 0,4 | 0,4 à 0,8 | 0,5 à 1 | 0,2 à 0,4 | 25 / s | un ennemi qui brûle |

Pour les trois :
- *Start Color* orange `(255, 150, 50)` ;
- *Color over Lifetime* : de jaune-orange à rouge, avec l'opacité qui tombe à 0 ;
- *Size over Lifetime* : de 1 vers 0,3 ;
- *Velocity over Lifetime* : *Y* `0.5`, *Space* `World`, pour que les flammes montent toujours, même sur une flèche penchée ;
- la valeur « 0,6 à 1 » se règle avec la petite flèche à droite du champ > *Random Between Two Constants*.

Fais de `Arrow Fire` et de `Burn Fire` des **prefabs** (glisse-les dans `Prefabs`), puis supprime-les de la scène. `Flames` reste dans le brasero.

En *World*, les particules d'une flèche enflammée restent derrière elle en vol : elles font une traînée de feu.

### Le brasero

1. *Create Empty*, nomme-le `Brazier`. Place-le en haut de la tour, à un coin de la plateforme, à portée de main (par exemple `(1.8, 4, 1.8)`).
2. Ses enfants :

| Nom | Création | Position | Scale | Réglages |
|---|---|---|---|---|
| `Base` | *3D Object > Cylinder* | `(0, 0.4, 0)` | `(0.5, 0.4, 0.5)` | `Brazier_Stone` ; garde son collider |
| `Bowl` | *3D Object > Cylinder* | `(0, 0.82, 0)` | `(0.75, 0.05, 0.75)` | `Brazier_Coal` |
| `Fire` | *Create Empty* | `(0, 0.9, 0)` | `(1, 1, 1)` | le feu, caché tant que le brasero n'est pas acheté |
| `Dip Point` | *Create Empty* | `(0, 1.05, 0)` | `(1, 1, 1)` | le cœur des flammes, où tremper la flèche |

3. Dans `Fire` :
   - glisse `Flames` (le *Particle System*), en `(0, 0, 0)`, tourné vers le haut (Rotation `(-90, 0, 0)`) ;
   - *Light > Point Light* : orange `(255, 140, 50)`, *Range* `6`, *Intensity* `3` ;
   - *Add Component > Audio Source* sur `Fire` : *AudioClip* `fire_loop`, coche *Loop* et *Play On Awake*, *Spatial Blend* `1`, *Volume* `0.6`, *Max Distance* `15`.
4. Sur `Brazier` : *Add Component > Brazier* :
   - *Fire* : `Fire` ;
   - *Dip Point* : `Dip Point` ;
   - *Arrow Fire Effect* : le prefab `Arrow Fire` ;
   - *Burn Effect* : le prefab `Burn Fire` ;
   - *Build Clip* `brazier_build`, *Ignite Clip* `arrow_ignite`.

`Fire` est caché au lancement : il s'allume à l'achat.

## 4. Les cartes de la boutique

La boutique a besoin de deux cartes de plus, donc d'une troisième rangée.

1. Sélectionne le Canvas `Shop` : passe *Height* de `1000` à `1330`.
2. Sélectionne `Cards` : passe *Height* de `640` à `960`.
3. Dans `Cards`, duplique `Tower Card` deux fois (Ctrl+D). Renomme les copies `Barricade Card` et `Brazier Card`, et place-les juste après `Tower Card`, avant `Reroll`.
4. Sur `Shop` (le *Shop Panel*) : *Barricade Card* `Barricade Card`, *Brazier Card* `Brazier Card`.

La grille place les 4 améliorations sur la première rangée ; l'arc, la tour, les barricades et le brasero sur la deuxième ; la relance seule sur la troisième.

## 5. Tester

**Brasero** :
1. Pour tester tout de suite, coche *Start Built* sur `Brazier`. Sinon : gong, **N** pour finir la vague, **M** pour l'or, et achète le brasero.
2. Prends une flèche dans ton dos et plonge sa pointe dans les flammes : elle s'enflamme.
3. Tire sur un chevalier :
   - il brûle 3 s, avec de petits chiffres de dégâts ;
   - avec un Multitir, toutes les flèches de l'éventail brûlent.

Pense à décocher *Start Built*.

**Barricades** :
1. Lance une vague et regarde une entrée : les chevaliers s'arrêtent devant le mur et le frappent, puis passent quand il casse, avec un bruit de bois.
2. À la pause, la carte « Réparer les barricades » affiche leurs PV : achète-la, elles se relèvent.

## En cas de problème

- **Les ennemis passent à travers la barricade** :
  - sa flèche bleue doit pointer vers la forêt ;
  - le mur doit couvrir toute la largeur du chemin ;
  - un ennemi qui vient d'un côté, hors du cercle orange, n'est pas arrêté : agrandis *Block Radius*.
- **Les ennemis frappent dans le vide** : *Intact* doit être `Planks`, avec les colliders des planches.
- **La barricade ne se relève pas** : *Intact* doit être renseigné ; c'est lui qui s'affiche et se cache.
- **La flèche ne s'enflamme pas** :
  - le brasero doit être acheté (ou *Start Built* coché) ;
  - c'est la **pointe** qu'il faut tremper, à moins de 30 cm de `Dip Point` (sphère orange quand le brasero est sélectionné).
- **Les flammes sont des carrés roses** : le matériau `Fire_Particles` doit utiliser un shader *Universal Render Pipeline/Particles*.
- **Les cartes débordent du panneau** : vérifie les nouvelles hauteurs du Canvas `Shop` et de `Cards` (section 4).
