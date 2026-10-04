# Guide : les effets visuels des coups

Ce guide ajoute des particules aux coups (GDD, section 14) :
- **flèche dans un ennemi** : une giclée sombre au corps, des étincelles dorées à la tête ou sur un point faible ;
- **flèche dans le décor** (sol, arbre, tour) : un petit nuage de poussière ;
- **mort d'un ennemi** : une bouffée de fumée.

Les effets sont réutilisés au lieu d'être recréés. Avec beaucoup de flèches, au plus 6 effets partent par image, pour que le jeu reste fluide.

## Le script

| Script | Rôle | Où le mettre |
|---|---|---|
| `Hit Effects` | Lance le bon effet à chaque impact et à chaque mort | Nouvel objet `Effects` |

## 1. Les matériaux

Dans `Materials`, deux matériaux au shader *Universal Render Pipeline/Particles/Unlit*, avec comme *Base Map* la texture `Default-Particle` (petit rond flou) :

| Matériau | *Surface Type* | *Blending Mode* | Pour |
|---|---|---|---|
| `FX_Glow` | `Transparent` | `Additive` | ce qui brille (étincelles) |
| `FX_Smoke` | `Transparent` | `Alpha` | ce qui ne brille pas (sang, poussière, fumée) |

Si tu as déjà fait `Fire_Particles` (guide des barricades), c'est le même réglage que `FX_Glow` : tu peux le réutiliser.

## 2. Les quatre effets

Pour chacun : *GameObject > Effects > Particle System*. Dans le module principal :
- *Duration* `1`, décoche *Looping* et *Play On Awake* ;
- *Simulation Space* `World` ;
- *Stop Action* `None`, et **pas** `Destroy` : l'effet est réutilisé.

Dans *Emission* : *Rate over Time* `0`, puis un *Burst* au temps 0 (bouton `+`).

Les valeurs « 0,2 à 0,4 » se règlent avec la petite flèche à droite du champ > *Random Between Two Constants*.

| Effet | *Burst* | *Start Lifetime* | *Start Speed* | *Start Size* | *Start Color* | *Gravity Modifier* | *Shape* | Matériau |
|---|---|---|---|---|---|---|---|---|
| `Hit Body` | 12 | 0,2 à 0,4 | 2 à 4 | 0,03 à 0,06 | rouge sombre `(150, 20, 20)` | 1 | *Cone*, angle 30 | `FX_Smoke` |
| `Hit Head` | 18 | 0,3 à 0,5 | 2 à 5 | 0,04 à 0,08 | doré `(255, 210, 60)` | 0,5 | *Cone*, angle 40 | `FX_Glow` |
| `Hit Surface` | 8 | 0,4 à 0,7 | 0,5 à 1,5 | 0,08 à 0,15 | poussière `(150, 130, 100)`, alpha 150 | 0,2 | *Cone*, angle 50 | `FX_Smoke` |
| `Death Smoke` | 20 | 0,8 à 1,4 | 0,3 à 1 | 0,4 à 0,8 | gris `(120, 120, 120)`, alpha 180 | -0,05 (monte) | *Sphere*, rayon 0,5 | `FX_Smoke` |

Ajoute aussi à chacun :
- *Color over Lifetime* : l'opacité tombe à 0 à la fin ;
- *Size over Lifetime* : la taille descend vers 0,3, sauf pour `Death Smoke` où elle **grandit** jusqu'à 1,5.

Le cône de *Shape* part vers l'axe Z (bleu) de l'effet : le script le tourne vers l'extérieur de la surface touchée.

Glisse les quatre dans `Prefabs` (dans un dossier `Prefabs/Effects` par exemple), puis supprime-les de la scène.

## 3. L'objet Effects

1. *Create Empty*, nomme-le `Effects`.
2. *Add Component > Hit Effects* :
   - *Body Hit Effect* `Hit Body` ;
   - *Head Hit Effect* `Hit Head` ;
   - *Surface Hit Effect* `Hit Surface` ;
   - *Death Effect* `Death Smoke`.

Les effets sont créés en jeu, sous `Effects`, puis réutilisés.

## 4. Tester

1. Lance Play et tire dans le sol : un petit nuage de poussière.
2. Tire sur un chevalier : une giclée sombre au corps, des étincelles dorées à la tête.
3. Tue-le : une bouffée de fumée monte de son corps.
4. Avec beaucoup de flèches (multitir, déluge), le jeu doit rester fluide.

## En cas de problème

- **Les effets sont des carrés roses** : les matériaux doivent utiliser un shader *Universal Render Pipeline/Particles*.
- **Un effet ne se joue qu'une fois** : son *Stop Action* doit être `None`, pas `Destroy`.
- **L'effet part dans le mauvais sens** : son *Shape* doit être un cône, sans rotation sur l'objet de l'effet.
- **Rien ne se passe** : vérifie les champs du `Hit Effects`, et que *Play On Awake* est décoché (le script lance l'effet lui-même).
