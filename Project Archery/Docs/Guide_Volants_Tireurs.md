# Guide : les ennemis volants et les tireurs

Ce guide ajoute les deux nouveaux ennemis du GDD (section 10) à la scène `Prototype_Tir`.

| Ennemi | PV | Vitesse | Attaque | Points | Dès la vague |
|---|---|---|---|---|---|
| **Volant** | 20 | 5 m/s | piqué sur la tête du joueur | 15 | 2 |
| **Tireur** | 40 | 2 m/s | projectile lent, à environ 20 m | 20 | 4 |

**Le volant** tourne en l'air autour de toi, à 6–8 m de haut :
- de temps en temps, il fait du **sur-place** en criant, tourné vers toi : c'est le moment de le viser ;
- puis il **pique** sur ta tête. Un pas de côté suffit à l'esquiver : il ne corrige sa trajectoire que lentement ;
- touché ou raté, il remonte et recommence ;
- tué, il **tombe** au sol en tournoyant.

Il ne passe pas par le NavMesh et ne se cogne à rien.

**Le tireur** marche vers toi, s'arrête vers 20 m et **lance des projectiles lents** (9 m/s) vers ta tête. Tu peux :
- les **esquiver** en te déplaçant ;
- les **abattre d'une flèche** : « Abattu ! », 5 points (multipliés par la difficulté).

Les deux te visent **toi**, jamais la tour.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Flying Enemy` | Vol en cercle, sur-place, piqué, chute à la mort | Racine du volant |
| `Wing Flap` | Fait battre les deux ailes | Racine du volant |
| `Enemy Ranged Attack` | Tire des projectiles au lieu de frapper | Racine du tireur |
| `Enemy Projectile` | Le projectile : blesse, se brise, peut être abattu | Racine du prefab du projectile |
| `Enemy` (déjà là) | Nouveau réglage *Center Height* : hauteur du centre du corps | Chaque ennemi |

`Enemy Definition` a aussi une nouvelle case, *Targets Player* : l'ennemi vise toujours le joueur.

Les sons sont dans `Audio/Placeholder` : `flyer_screech` (avant le piqué), `enemy_cast` (tir), `projectile_impact` et `projectile_break` (projectile abattu).

> Le Rampant et le boss ne changent pas. Leur *Center Height* vaut 1,2 m par défaut : c'est la hauteur que visaient déjà l'auto-visée, les éclairs et les explosions.

## 1. Les caractéristiques

Dans `Data/Enemies` : clic droit > *Create > Archery > Enemy Definition*, deux fois.

| Champ | `Enemy_Volant` | `Enemy_Tireur` |
|---|---|---|
| *Display Name* | `Volant` | `Tireur` |
| *Max Health* | `20` | `40` |
| *Move Speed* | `5` | `2` |
| *Attack Damage* | `8` | `12` |
| *Attack Range* | `1` (inutilisé) | `25` |
| *Attack Interval* | `6` (temps entre deux piqués) | `3.5` |
| *Attack Windup* | `0.6` (sur-place avant le piqué) | `0.8` |
| *Player Aggro Range* | `0` | `0` |
| *Targets Player* | coché | coché |
| *Points* | `15` | `20` |

## 2. Le volant

En attendant un modèle de l'Asset Store, c'est une chauve-souris en formes simples. Son avant est vers l'axe Z (bleu).

### Les matériaux

Dans `Materials` :
- `Flyer_Skin` : *Universal Render Pipeline/Lit*, violet sombre `(70, 40, 90)` ;
- `Flyer_Wing` : *Universal Render Pipeline/Lit*, violet plus sombre `(45, 25, 60)` ;
- `Flyer_Eyes` : *Universal Render Pipeline/Unlit*, jaune vif `(255, 220, 60)`.

### Le prefab

1. *Create Empty*, nomme-le `Enemy_Volant`, en `(0, 0, 0)`.
2. Ses enfants :

| Nom | Création | Position | Scale | Réglages |
|---|---|---|---|---|
| `Body` | *3D Object > Sphere* | `(0, 0, 0)` | `(0.5, 0.4, 0.7)` | `Flyer_Skin` ; garde son collider ; *Add Component > Hitbox*, *Zone* `Body` |
| `Head` | *3D Object > Sphere* | `(0, 0.1, 0.42)` | `(0.3, 0.3, 0.3)` | `Flyer_Skin` ; garde son collider ; *Hitbox* : *Zone* `Head`, *Damage Multiplier* `2`, *Hit Clip* `headshot_ding` |
| `Eye L` | *3D Object > Sphere* | `(-0.07, 0.17, 0.55)` | `(0.06, 0.06, 0.06)` | `Flyer_Eyes` ; supprime son collider |
| `Eye R` | *3D Object > Sphere* | `(0.07, 0.17, 0.55)` | `(0.06, 0.06, 0.06)` | `Flyer_Eyes` ; supprime son collider |
| `Left Wing Pivot` | *Create Empty* | `(-0.2, 0.05, 0)` | `(1, 1, 1)` | l'épaule gauche |
| `Right Wing Pivot` | *Create Empty* | `(0.2, 0.05, 0)` | `(1, 1, 1)` | l'épaule droite |

3. Les ailes, une dans chaque pivot :

| Nom | Parent | Création | Position | Scale | Réglages |
|---|---|---|---|---|---|
| `Left Wing` | `Left Wing Pivot` | *3D Object > Cube* | `(-0.45, 0, 0)` | `(0.9, 0.04, 0.45)` | `Flyer_Wing` ; supprime son collider |
| `Right Wing` | `Right Wing Pivot` | *3D Object > Cube* | `(0.45, 0, 0)` | `(0.9, 0.04, 0.45)` | `Flyer_Wing` ; supprime son collider |

   Sans collider, les ailes ne comptent pas : il faut toucher le corps ou la tête.
4. Sur `Enemy_Volant`, ajoute dans l'ordre :
   - *Rigidbody* : coche *Is Kinematic*, décoche *Use Gravity*. Comme pour le Rampant, il regroupe les colliders en un seul corps ;
   - *Health* ;
   - *Enemy* : *Definition* `Enemy_Volant`, *Center Height* `0` ;
   - *Wing Flap* : *Left Wing* `Left Wing Pivot`, *Right Wing* `Right Wing Pivot` ;
   - *Flying Enemy* : *Wings* `Enemy_Volant` (son *Wing Flap*), *Screech Clip* `flyer_screech`.
5. Glisse `Enemy_Volant` dans `Prefabs`, puis supprime-le de la scène.

Pas de NavMeshAgent : le volant n'en a pas besoin.

## 3. Le projectile

1. *Create Empty*, nomme-le `Enemy Projectile`, en `(0, 0, 0)`.
2. Sur `Enemy Projectile` :
   - *Add Component > Sphere Collider* : *Radius* `0.2`, **sans** cocher *Is Trigger* : sinon les flèches le traversent ;
   - *Add Component > Enemy Projectile* : *Impact Clip* `projectile_impact`, *Shot Down Clip* `projectile_break` ;
   - *Add Component > Trail Renderer* : *Time* `0.3`, largeur de `0.15` à `0`, matériau `Effects` (celui des éclairs), couleur violette qui devient transparente.
3. Enfant `Orb` : *3D Object > Sphere*, Scale `(0.25, 0.25, 0.25)` :
   - supprime son collider ;
   - nouveau matériau `Projectile_Glow` : *Universal Render Pipeline/Unlit*, violet vif `(200, 80, 255)`.
4. Optionnel : un enfant *Light > Point Light*, violet, *Range* `3`, *Intensity* `2` : le projectile éclaire autour de lui, bien visible la nuit.
5. Glisse `Enemy Projectile` dans `Prefabs`, puis supprime-le de la scène.

## 4. Le tireur

On fait une **variante** du chevalier, comme pour le boss.

1. Dans `Prefabs`, clic droit sur `Enemy_Rampant` > *Create > Prefab Variant*, nomme-la `Enemy_Tireur`, puis ouvre-la.
2. Sur la racine :
   - dans `Enemy`, *Definition* : `Enemy_Tireur` ;
   - *Add Component > Enemy Ranged Attack* : *Projectile Prefab* `Enemy Projectile`, *Fire Clip* `enemy_cast`. Laisse *Muzzle* vide : les projectiles partent à 1,6 m de haut.
3. Pour le reconnaître : duplique le matériau `DemoMat` (Ctrl+D), nomme-le `Archer_Knight`, donne-lui une couleur vert sombre, et mets-le sur le *Skinned Mesh Renderer*.

Le NavMeshAgent s'arrête tout seul à 20 m de sa cible (80 % de l'*Attack Range*). L'animation d'attaque du chevalier sert de geste de tir, en attendant un modèle d'archer ou de mage.

## 5. Dans les vagues

Dans `Data/Waves/WaveSettings`, liste *Enemies*, ajoute deux éléments :

| *Prefab* | *Cost* | *First Wave* | *Weight* |
|---|---|---|---|
| `Enemy_Volant` | `1.5` | `2` | `0.6` |
| `Enemy_Tireur` | `2` | `4` | `0.4` |

Le *Cost* se paie sur le budget de la vague ; le *Weight* règle leur fréquence face au Rampant (*Weight* 1).

## 6. Tester

Pour les voir tout de suite, mets provisoirement leur *First Wave* à `1`.

1. Lance Play et tire dans le gong.
2. **Volants** : ils sortent des points d'apparition en l'air et viennent tourner autour de toi.
   - Avant un piqué, l'un d'eux s'arrête et crie : vise-le.
   - Laisse-le piquer, puis fais un pas de côté : il te rate et remonte. Reste immobile : il te touche.
   - Tue-le : il tombe en tournoyant.
3. **Tireurs** : ils s'arrêtent loin de toi et lancent des boules violettes.
   - Déplace-toi : elles passent à côté.
   - Tire dedans : « Abattu ! » et quelques points.
4. Les flèches à auto-visée, les éclairs et les explosions touchent aussi les volants. La glace, posée au sol, ne les ralentit pas.
5. À la fin de la vague, les volants s'envolent au loin et les tireurs s'enfuient comme les Rampants.

Pense à remettre les *First Wave* à 2 et 4.

## En cas de problème

- **« Enemy : il faut un NavMeshAgent, ou un comportement particulier »** : le volant n'a pas de `Flying Enemy`.
- **Les volants n'apparaissent pas** : vérifie leur ligne dans *Enemies* (prefab, *First Wave*). Ils n'ont pas besoin du NavMesh.
- **Les flèches traversent le volant** :
  - `Body` et `Head` doivent garder leur collider, avec une `Hitbox` ;
  - la racine doit avoir le *Rigidbody* (*Is Kinematic*).
- **La console dit « le collider … n'a pas de Hitbox »** : une aile ou un œil a gardé son collider. Supprime-le.
- **Le volant vole en marche arrière ou sur le côté** : la tête doit être vers l'avant, sur l'axe Z (`z = 0.42`).
- **Les ailes battent dans le mauvais sens** : chaque aile doit être l'enfant de son pivot, décalée vers l'extérieur (`x = -0.45` à gauche, `0.45` à droite).
- **On ne peut pas abattre les projectiles** : leur *Sphere Collider* ne doit pas être un trigger.
- **Les tireurs viennent frapper au corps à corps** : il manque `Enemy Ranged Attack`, ou l'*Attack Range* n'est pas à 25.
- **Trop dur ou trop facile** :
  - pour les volants : *Attack Interval* (temps entre deux piqués) et *Attack Windup* (sur-place) dans `Enemy_Volant` ; *Dive Turn Rate* et *Hit Radius* dans `Flying Enemy` ;
  - pour les tireurs : *Projectile Speed* dans `Enemy Ranged Attack`.
