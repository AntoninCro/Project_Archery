# Guide : les ennemis volants et les tireurs

Ce guide ajoute les deux nouveaux ennemis du GDD (section 10) à la scène `Prototype_Tir`, avec les modèles de tes deux packs :

- **le volant** est le **Beholder** du pack *RPG Monster Partners PBR Polyart* : un gros œil volant à tentacules. **Son œil est sa tête** : dégâts ×2 et « Headshot ! » ;
- **le tireur** est le **mage** du pack *Wizard PolyArt* : il pointe son bâton vers toi, et le projectile part du bout du bâton.

| Ennemi | PV | Vitesse | Attaque | Points | Dès la vague |
|---|---|---|---|---|---|
| **Volant** | 20 | 5 m/s | piqué sur la tête du joueur | 15 | 2 |
| **Tireur** | 40 | 2 m/s | projectile lent, à environ 20 m | 20 | 4 |

**Le volant** tourne en l'air autour de toi, à 6–8 m de haut :
- de temps en temps, il fait du **sur-place** en se cabrant, tourné vers toi : c'est le moment de viser son œil ;
- puis il **pique** sur ta tête. Un pas de côté suffit à l'esquiver : il ne corrige sa trajectoire que lentement ;
- touché ou raté, il remonte et recommence ;
- tué, il **tombe** au sol et s'y écrase (son animation de mort).

Il ne passe pas par le NavMesh et ne se cogne à rien.

**Le tireur** marche vers toi, s'arrête vers 20 m et, d'un geste de son bâton, **lance des projectiles lents** (9 m/s) vers ta tête. Tu peux :
- les **esquiver** en te déplaçant ;
- les **abattre d'une flèche** : « Abattu ! », 5 points (multipliés par la difficulté).

Les deux te visent **toi**, jamais la tour.

Le monstre coffre du pack des monstres ne sert pas dans ce guide.

> **Où en est ce guide** (6 octobre) : le **volant** est monté par toi (section 4), le **tireur** par Claude (sections 1, 5 et sa ligne de la section 6), et les matériaux des deux packs sont corrigés (section 2.1). Les deux sont dans `WaveSettings` (section 6). Il reste les tests (section 7).

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Flying Enemy` | Vol en cercle, sur-place, piqué, chute à la mort | Racine du volant |
| `Enemy Ranged Attack` | Tire des projectiles au lieu de frapper | Racine du tireur |
| `Enemy Projectile` | Le projectile : blesse, se brise, peut être abattu | Racine du prefab du projectile |
| `Enemy` (déjà là) | Nouveau réglage *Center Height* : hauteur du centre du corps | Chaque ennemi |

`Enemy Definition` a aussi une nouvelle case, *Targets Player* : l'ennemi vise toujours le joueur.

`Flying Enemy` a deux réglages pour un modèle animé, dans *Mort* : *Spin While Falling* (tournoyer en tombant) et *Corpse Height* (voir section 4.2).

Les sons sont dans `Audio/Placeholder` : `flyer_screech` (avant le piqué), `enemy_cast` (tir), `projectile_impact` et `projectile_break` (projectile abattu).

> Le Rampant et le boss ne changent pas. Leur *Center Height* vaut 1,2 m par défaut : c'est la hauteur que visaient déjà l'auto-visée, les éclairs et les explosions.

## 1. Les caractéristiques

Tu as déjà créé `Enemy_Volant` et `Enemy_Tireur` dans `Data/Enemies`. Une seule valeur change, pour que le tir parte au bon moment du geste du mage :

- `Enemy_Tireur` : *Attack Windup* `0.55` (au lieu de `0.8`). C'est le moment où son bâton pointe vers toi.

Pour rappel, leurs autres réglages :

| Champ | `Enemy_Volant` | `Enemy_Tireur` |
|---|---|---|
| *Max Health* | `20` | `40` |
| *Move Speed* | `5` | `2` |
| *Attack Damage* | `8` | `12` |
| *Attack Range* | `1` (inutilisé) | `25` |
| *Attack Interval* | temps entre deux piqués | temps entre deux tirs |
| *Attack Windup* | `0.6` (sur-place avant le piqué) | `0.55` |
| *Player Aggro Range* | `0` | `0` |
| *Targets Player* | coché | coché |
| *Points* | `15` | `20` |

## 2. Préparer les deux packs

### 2.1 Les matériaux

Les deux packs utilisent des shaders de l'ancien pipeline : leurs modèles sont **roses**.

1. Ouvre *Window > Rendering > Render Pipeline Converter*, choisis *Built-in to URP*, coche *Material Upgrade*, puis *Initialize And Convert*.
2. Vérifie que ces deux matériaux utilisent maintenant *Universal Render Pipeline/Lit* :
   - `RPGMonsterPartnersPBRPolyart/Materials/PolyartDefault` (le Beholder) ;
   - `WizardPolyArt/Materials/PolyArtStandardMaterial` (le mage).

> **Attention** : choisis bien *Built-in to URP*, pas *Built-in to 2D (URP)*. Les matériaux des deux packs avaient été convertis en *Mesh2D-Lit-Default*, un shader 2D qui éclaire mal les modèles en 3D. Ils sont maintenant en *Universal Render Pipeline/Lit* (corrigés le 6 octobre) : il n'y a plus rien à convertir.

Le matériau `PolyArtMaskTint` du mage utilise un shader spécial que le convertisseur ne sait pas traiter : il reste rose. On ne s'en sert pas, on prend le prefab `PolyArtWizardStandardMat`.

### 2.2 Les animations

Il n'y a rien à régler : les animations des deux packs restent sur place, et celles qui doivent tourner en boucle le font déjà.

Chaque animation est dans son propre FBX. Pour la glisser dans un Animator, déplie le FBX (petite flèche) et prends l'animation qu'il contient, l'icône en forme de triangle.

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

## 4. Le volant : le Beholder

### 4.1 Son Animator Controller

1. Dans `_Project/Animations` : clic droit > *Create* > *Animator Controller*, nomme-le `Volant`, puis ouvre-le.
2. Onglet *Parameters* : ajoute `Attack`, `Hit` et `Die`, tous en *Trigger*. Pas de `Speed` : il vole tout le temps.
3. Glisse ces 4 animations de `RPGMonsterPartnersPBRPolyart/Animations/Beholder` dans le graphe, `Run` en premier : c'est l'état par défaut (orange). Les états gardent le nom de leur animation.

| Animation | Ce qu'elle montre |
|---|---|
| `Run` | il fonce en ondulant |
| `Attack01` | il se cabre puis frappe : son sur-place avant le piqué |
| `GetHit` | il recule sous le coup |
| `Die` | il s'effondre au sol |

4. Les transitions :

| De | Vers | Condition | Has Exit Time |
|---|---|---|---|
| Any State | Attack01 | `Attack` | non (et décoche *Can Transition To Self*) |
| Attack01 | Run | aucune | oui |
| Any State | GetHit | `Hit` | non (et décoche *Can Transition To Self*) |
| GetHit | Run | aucune | oui |
| Any State | Die | `Die` | non (et décoche *Can Transition To Self*) |

### 4.2 Le prefab

Le pivot du volant est le **centre de son corps** : c'est le point qui vole, et qui pique sur ta tête. Le Beholder du pack, lui, a son pivot au sol et flotte 1,45 m au-dessus : on le descend donc de 1,45 m.

1. *Create Empty*, nomme-le `Enemy_Volant`, en `(0, 0, 0)`.
2. Glisse le prefab `RPGMonsterPartnersPBRPolyart/Prefabs/Character/BeholderPolyartDefault` en enfant de `Enemy_Volant`, et renomme-le `Model` :
   - Position `(0, -1.45, 0)`, Rotation `(0, 0, 0)`, Scale `(1, 1, 1)`. Son corps est alors centré sur le pivot, et son œil regarde vers l'axe Z (bleu) ;
   - composant *Animator* : *Controller* `Volant`, *Apply Root Motion* décoché.
3. Les zones de touche. Les os sont dans `Model > Root > Body` :
   - **L'œil** : clic droit sur `EyeBallCTRL` (dans `Body > EyeCTRL`) > *Create Empty*, nomme-le `Eye Hitbox`, Position `(0, 0, 0)`. Ajoute un *Sphere Collider* : *Center* `(0, -0.03, 0)`, *Radius* `0.18`. Puis *Add Component > Hitbox* : *Zone* `Head`, *Damage Multiplier* `2`, *Hit Clip* `headshot_ding` ;
   - **Le corps** : clic droit sur `Body` > *Create Empty*, nomme-le `Body Hitbox`, Position `(0, 0, 0)`. Ajoute un *Sphere Collider* : *Radius* `0.4`. Puis une *Hitbox* : *Zone* `Body`.

   Dans la vue Scene, la petite sphère doit couvrir l'œil et dépasser devant la grande : une flèche dans l'œil touche ainsi la tête. Les tentacules n'ont pas de collider : les flèches les traversent.
4. Sur `Enemy_Volant`, ajoute dans l'ordre :
   - *Rigidbody* : coche *Is Kinematic*, décoche *Use Gravity*. Comme pour le Rampant, il regroupe les colliders en un seul corps ;
   - *Health* ;
   - *Enemy* : *Definition* `Enemy_Volant`, *Center Height* `0` ;
   - *Damage Popups* : *Shot Tuning* `ShotTuning` (dans `Data`), pour les chiffres de dégâts ;
   - *Flying Enemy* :
     - *Screech Clip* `flyer_screech` ;
     - laisse *Wings* vide ;
     - **décoche** *Spin While Falling* : il tombe bien droit, et son animation de mort fait le reste ;
     - *Corpse Height* `1.45` : en tombant, son pivot s'arrête 1,45 m au-dessus du sol. Le modèle touche alors le sol, et son animation de mort le couche.
5. Glisse `Enemy_Volant` dans `Prefabs`, puis supprime-le de la scène.

Pas de NavMeshAgent : le volant n'en a pas besoin.

## 5. Le tireur : le mage (déjà monté)

L'Animator Controller `Mage` et le prefab `Enemy_Tireur` existent déjà. Cette section décrit comment ils sont faits, pour les vérifier ou les retoucher.

### 5.1 Son Animator Controller

`_Project/Animations/Mage` a 4 paramètres : `Speed` (*Float*), puis `Attack`, `Hit` et `Die` (*Trigger*). Ses états reprennent 5 animations de `WizardPolyArt/Animations` ; `Idle01` est l'état par défaut.

| Animation | Ce qu'elle montre |
|---|---|
| `Idle01` | il attend, bâton levé |
| `BattleRunForward` | il court ; la *Speed* de cet état est à `0.6` |
| `Attack01` | il abat son bâton vers l'avant |
| `GetHit` | il recule sous le coup |
| `Die` | il tombe en arrière |

- `BattleRunForward` est une course faite pour environ 3,5 m/s ; le mage avance à 2 m/s. À `0.6`, ses pieds glissent moins. Le script accélère ensuite l'animation avec les vagues, quand les ennemis vont plus vite.
- Le projectile part au moment où le bâton pointe vers toi, pendant `Attack01` (*Attack Windup* `0.55`, section 1).

Les transitions :

| De | Vers | Condition | Has Exit Time |
|---|---|---|---|
| Idle01 | BattleRunForward | `Speed` Greater `0.1` | non |
| BattleRunForward | Idle01 | `Speed` Less `0.1` | non |
| Any State | Attack01 | `Attack` | non (*Can Transition To Self* décoché) |
| Attack01 | Idle01 | aucune | oui |
| Any State | GetHit | `Hit` | non (*Can Transition To Self* décoché) |
| GetHit | Idle01 | aucune | oui |
| Any State | Die | `Die` | non (*Can Transition To Self* décoché) |

### 5.2 Le prefab

La racine `Enemy_Tireur` a les mêmes composants que le Rampant (NavMeshAgent, Rigidbody, Health, Enemy, Damage Popups), avec :
- dans `Enemy` : *Definition* `Enemy_Tireur`, *Center Height* `1.1` ;
- un `Enemy Ranged Attack` : *Projectile Prefab* `Enemy Projectile`, *Muzzle* `Muzzle`, *Fire Clip* `enemy_cast`.

Son enfant `Model` est le prefab `WizardPolyArt/Prefabs/PolyArtWizardStandardMat` :
- en `(0, 0, 0)`, sans rotation. Le mage mesure 2 m, chapeau compris, et regarde vers l'axe Z (bleu) ;
- sans son *Capsule Collider* d'origine : une flèche s'y planterait sans faire de dégâts ;
- *Animator* : *Controller* `Mage`, *Apply Root Motion* décoché.

Les zones de touche sont sous les os du mage. Ces os sont **en centimètres** (leur échelle est 0,01) : un *Radius* de `20` fait 20 cm.

| Objet | Parent | Collider | Hitbox |
|---|---|---|---|
| `Head Hitbox` | `head` (`Model > root > pelvis > spine_01 > spine_02 > spine_03 > neck_01 > head`) | *Sphere* : *Center* `(-10, -4, 0)`, *Radius* `20` | *Zone* `Head`, ×2, `headshot_ding` |
| `Body Hitbox` | `spine_01` | *Capsule* : *Center* `(-8, 1, 0)`, *Radius* `27`, *Height* `110`, *Direction* `X-Axis` | *Zone* `Body` |

Les projectiles partent de `Muzzle`, sous le bâton (`Model > root > pelvis > Weapon > Staff01PolyArt`), en `(0, -1.05, 0)` : la tête du bâton, le gros bout décoré.

**À vérifier dans la vue Scene** (ouvre le prefab) :
- la sphère couvre le visage et le bas du chapeau ;
- la capsule couvre le buste et la robe, sans englober la sphère de la tête. Sinon elle « vole » les headshots : réduis sa *Height* ;
- `Muzzle` est sur la tête du bâton ; sinon, déplace-le dessus.

Le NavMeshAgent s'arrête tout seul à 20 m de sa cible (80 % de l'*Attack Range*).

## 6. Dans les vagues

Dans `Data/Waves/WaveSettings`, liste *Enemies*, les deux lignes sont déjà là :

| *Prefab* | *Cost* | *First Wave* | *Weight* |
|---|---|---|---|
| `Enemy_Volant` | `1.5` | `2` | `0.6` |
| `Enemy_Tireur` | `2` | `4` | `0.4` |

Le *Cost* se paie sur le budget de la vague ; le *Weight* règle leur fréquence face au Rampant (*Weight* 1).

## 7. Tester

**Sans casque d'abord** : ouvre le prefab `Enemy_Volant`, puis `Enemy_Tireur`, et regarde-les dans la vue Scene. Le corps du Beholder doit être centré sur le pivot de la racine, et les zones de touche à leur place.

Pour les voir tout de suite en jeu, mets provisoirement leur *First Wave* à `1`.

1. Lance Play et tire dans le gong.
2. **Volants** : ils sortent des points d'apparition en l'air et viennent tourner autour de toi.
   - Avant un piqué, l'un d'eux s'arrête et se cabre : vise son œil, « Headshot ! ».
   - Laisse-le piquer, puis fais un pas de côté : il te rate et remonte. Reste immobile : il te touche.
   - Tue-le : il tombe et s'effondre au sol.
3. **Tireurs** : ils marchent vers toi, s'arrêtent loin et abattent leur bâton : une boule violette part du bout du bâton.
   - Déplace-toi : elles passent à côté.
   - Tire dedans : « Abattu ! » et quelques points.
   - Tire dans la tête du mage : « Headshot ! ».
4. Les flèches à auto-visée, les éclairs et les explosions touchent aussi les volants. La glace, posée au sol, ne les ralentit pas.
5. À la fin de la vague, les volants s'envolent au loin et les tireurs s'enfuient en courant, comme les Rampants.

Pense à remettre les *First Wave* à 2 et 4.

## En cas de problème

- **Le Beholder ou le mage est rose** : leurs matériaux ne sont pas convertis (section 2.1). Pour le mage, vérifie que c'est bien `PolyArtWizardStandardMat` et pas `PolyArtWizardMaskTintMat`.
- **Le Beholder ou le mage est sombre, plat ou mal éclairé** : son matériau doit utiliser *Universal Render Pipeline/Lit*, pas *Mesh2D-Lit-Default* (section 2.1).
- **« Enemy : il faut un NavMeshAgent, ou un comportement particulier »** : le volant n'a pas de `Flying Enemy`.
- **Les volants n'apparaissent pas** : vérifie leur ligne dans *Enemies* (prefab, *First Wave*). Ils n'ont pas besoin du NavMesh.
- **Le volant vole sur le côté ou à reculons** : `Model` doit avoir la Rotation `(0, 0, 0)` ; son œil regarde alors vers l'axe Z (bleu).
- **Le volant pique au-dessus ou en dessous de ta tête** : le corps du Beholder n'est pas centré sur le pivot. Vérifie la Position de `Model` : `(0, -1.45, 0)`.
- **Les flèches traversent un ennemi** :
  - `Eye Hitbox`, `Body Hitbox` et `Head Hitbox` doivent avoir leur collider et leur `Hitbox` ;
  - la racine doit avoir le *Rigidbody* (*Is Kinematic*).
- **La console dit « le collider … n'a pas de Hitbox »** : le *Capsule Collider* du mage est encore sur `Model` (section 5.2, étape 3).
- **Le mort reste en l'air, ou s'enfonce dans le sol** : *Corpse Height* de `Flying Enemy` doit valoir la descente de `Model`, `1.45`.
- **Le mort tournoie en tombant** : décoche *Spin While Falling*.
- **Les ennemis restent figés en pose de départ (T)** : le *Controller* de leur Animator est vide.
- **Le mage glisse en marchant** : règle la *Speed* de l'état `BattleRunForward` (plus haut si ses jambes sont trop lentes, plus bas si elles sont trop rapides).
- **Les projectiles partent de ses pieds ou de son dos** : `Muzzle` doit être l'enfant de `Staff01PolyArt`, sur la tête du bâton.
- **Le projectile part avant ou après le geste** : règle *Attack Windup* dans `Enemy_Tireur`.
- **On ne peut pas abattre les projectiles** : leur *Sphere Collider* ne doit pas être un trigger.
- **Les tireurs viennent frapper au corps à corps** : il manque `Enemy Ranged Attack`, ou l'*Attack Range* n'est pas à 25.
- **Trop dur ou trop facile** :
  - pour les volants : *Attack Interval* (temps entre deux piqués) et *Attack Windup* (sur-place) dans `Enemy_Volant` ; *Dive Turn Rate* et *Hit Radius* dans `Flying Enemy`. Pour une cible plus petite, baisse la Scale de `Model` et sa descente d'autant (Scale `0.8` : Position `(0, -1.16, 0)`, *Corpse Height* `1.16`) ;
  - pour les tireurs : *Projectile Speed* dans `Enemy Ranged Attack`.
