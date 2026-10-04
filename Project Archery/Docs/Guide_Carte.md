# Guide : la carte finale

Ce guide transforme le sol plat du prototype en vraie carte (GDD, section 17) : une **clairière** d'environ 35 m de rayon autour de la tour, une **forêt** dense tout autour, et **4 chemins** par lesquels arrivent les ennemis. Il n'y a pas de script : tout se fait dans l'éditeur, avec des modèles gratuits.

Prends ton temps sur cette étape : c'est elle qui donne l'impression de jeu fini. Avance par petites touches et teste souvent dans le casque.

## Le plan

Vue de dessus, le nord est vers l'axe Z (là où regarde le joueur en haut de la tour) :

```
                  N  (0, 65)
                  |  apparition
        forêt     |      forêt
                  |  barricade (0, 36)
   (-65, 0)  -----( clairière )-----  (65, 0)
  apparition      |  tour (0, 0)     apparition
        forêt     |      forêt
                  |
                  S  (0, -65)
```

| Élément | Nord | Est | Sud | Ouest |
|---|---|---|---|---|
| Chemin (4 m de large) | de `(0, 0, 35)` à `(0, 0, 70)` | de `(35, 0, 0)` à `(70, 0, 0)` | de `(0, 0, -35)` à `(0, 0, -70)` | de `(-35, 0, 0)` à `(-70, 0, 0)` |
| Point d'apparition | `(0, 0, 65)` | `(65, 0, 0)` | `(0, 0, -65)` | `(-65, 0, 0)` |
| Barricade : position, rotation Y | `(0, 0, 36)`, `0` | `(36, 0, 0)`, `90` | `(0, 0, -36)`, `180` | `(-36, 0, 0)`, `270` |

Les cibles d'entraînement et les panneaux de difficulté restent dans la clairière, au nord : ils ne gênent pas.

## 1. Les modèles

Des packs gratuits en licence CC0 (utilisables sans condition) :
- **Kenney Nature Kit** (kenney.nl, rubrique *Assets*) : arbres, rochers, buissons, chemins, clôtures ;
- **Quaternius** (quaternius.com) : packs nature (arbres, rochers, herbes) et village médiéval (tours, palissades) ;
- l'**Asset Store**, en cherchant « low poly nature » avec le filtre *Free*.

Pour chaque pack :
1. Télécharge-le et dézippe-le. Copie les modèles (`.fbx` ou `.glb`) dans un nouveau dossier `Assets/_Project/Art/Nature`.
2. Si des modèles sont **roses**, leurs matériaux viennent du rendu par défaut d'Unity. Ouvre *Window > Rendering > Render Pipeline Converter*, choisis *Built-in to URP*, coche *Material Upgrade*, puis *Initialize And Convert*.
3. Sur les matériaux de la nature, coche *Enable GPU Instancing* : les arbres identiques s'affichent d'un coup, c'est plus fluide.

Note les packs utilisés pour les crédits du README.

## 2. Le sol et les chemins

1. Sélectionne `Ground` : passe son *Scale* de `(20, 1, 20)` à `(16, 1, 16)` (160 m de côté, assez pour la forêt).
2. Donne-lui un matériau `Grass` (*Universal Render Pipeline/Lit*, vert `(90, 140, 60)`, *Smoothness* 0).
3. Pour chaque chemin, un cube aplati au matériau `Dirt` (brun `(120, 95, 65)`), **sans collider** :
   - nord : Position `(0, 0.01, 52)`, Scale `(4, 0.02, 36)` ;
   - les trois autres en tournant de 90°, ou avec les morceaux de chemin du Nature Kit.

## 3. La forêt

1. *Create Empty* `Forest`, en `(0, 0, 0)`. Toute la forêt va dedans.
2. Place des arbres en anneau, **de 37 à 75 m** du centre, serrés pour qu'on ne voie pas au travers. Laisse les 4 chemins libres.
3. Varie la rotation (Y au hasard) et la taille (de 0,8 à 1,2) : la forêt paraît naturelle avec peu de modèles différents.
4. Devant la lisière, des buissons et des petits arbres font un mur de verdure.
5. Dans la clairière, seulement 2 ou 3 rochers ou buissons comme couvert. Depuis la tour, la vue doit rester dégagée.
6. Colliders :
   - garde un collider simple (capsule ou boîte) sur les arbres proches des chemins et de la clairière : les flèches s'y plantent ;
   - enlève-les sur les arbres du fond, qu'on ne peut pas atteindre ;
   - évite les *Mesh Collider* sur les arbres détaillés.
7. Sélectionne `Forest` et coche **Static** (en haut à droite de l'Inspector), avec ses enfants : Unity regroupe le décor fixe, ce qui le rend bien plus léger.

## 4. La navigation des ennemis

Les ennemis au sol doivent passer par les chemins, pas à travers la forêt : c'est ce qui rend les barricades utiles.

1. Pour chaque quart de forêt, entre deux chemins, crée un objet vide avec *Add Component > NavMesh Modifier Volume* :
   - *Area Type* `Not Walkable` ;
   - une boîte qui couvre le quart de forêt, de la lisière (37 m) au bord de la carte, **sans mordre sur les chemins**.
2. Sélectionne `Ground` et, dans son *NavMesh Surface*, clique sur **Bake**.
3. Vérifie la zone bleue dans la vue Scene : seuls la clairière et les 4 chemins doivent être bleus.

Les volants ne sont pas concernés : ils passent au-dessus.

## 5. Les ennemis, les barricades et les coffres

1. **Points d'apparition** : place `Enemy Spawner` en `(0, 0, 0)`. Garde ses enfants `Spawn A`, `Spawn B` et `Spawn C`, ajoute-en un quatrième (Ctrl+D), et place les quatre aux bouts des chemins (tableau du plan). Mets les 4 dans la liste *Spawn Points*.
2. **Barricades** (`Guide_Barricades_Brasero.md`) : une à chaque entrée, avec la position et la rotation du tableau. Leur flèche bleue pointe vers la forêt. Le mur doit couvrir toute la largeur du chemin.
3. **Coffres** (`Guide_Coffres.md`) : place 6 à 8 objets vides le long des chemins, entre 25 et 50 m de la tour, par exemple dans de petites clairières à côté du chemin. Glisse-les dans *Spawn Points* du `Chest Spawner`. Chacun doit être accessible à pied.

## 6. La tour

Pour remplacer le cube par une vraie tour (palissade, tour de guet en bois…) :
1. Sur `Tower`, décoche le *Mesh Renderer* : le cube devient invisible, mais garde son *Box Collider*. C'est lui que frappent les ennemis et qui porte le joueur.
2. Glisse le modèle de tour **à côté** de `Tower`, pas dedans : `Tower` est étiré `(5, 4, 5)`, et le modèle le serait aussi. Place-le en `(0, 0, 0)` et règle son *Scale* pour que son sommet arrive à 4 m.
3. Si le haut du modèle a une rambarde, ajoute des *Box Collider* fins sur ses bords, sur des objets vides **dans** `Tower` : on ne tombe plus par mégarde. Garde-les à moins de 1,2 m au-dessus de la plateforme : le téléporteur se sert du haut des colliders de la tour pour savoir si tu es en haut.

Le sommet reste à 4 m : le XR Origin, `Tower Top Arrival` et le téléporteur n'ont pas à bouger.

## 7. La lumière et la fluidité

- La lumière et le brouillard viennent du ciel de chaque difficulté (`Sky Controller`). Ne fais pas de *Bake* de la lumière : le ciel change.
- Garde une seule lumière qui projette des ombres, le soleil ou la lune. Coupe *Cast Shadows* sur les petites pierres et l'herbe.
- Pour une forêt dense : *Window > Rendering > Occlusion Culling*, onglet *Bake*, puis **Bake**. Unity n'affiche plus ce qui est caché derrière les arbres.
- **Mesure** : dans la vue *Game*, active *Stats*. Dans le casque, vise au moins 72 images par seconde, soit moins de 13,8 ms par image. Si ça rame : moins d'arbres au fond, ombres moins loin (*Shadows > Max Distance* dans `Quality URP Config`).

## 8. Les finitions de nuit

Le moment de remplacer les objets de nuit par de vrais modèles (voir `Guide_Difficultes.md`, section 5) :
- un modèle de **lanterne** dans `Lantern`, à la place du poteau et de la sphère ;
- des **yeux qui brillent** sur le modèle des chevaliers (petites sphères au matériau *Unlit* orange), dans `Eyes`, toujours avec *Night Only*.

## 9. Vérifier

1. Une vague dans chaque difficulté : les ennemis sortent des 4 chemins, s'arrêtent aux barricades, puis avancent vers la tour.
2. Aucun ennemi ne reste coincé dans un arbre ou un rocher. Sinon, rebake le NavMesh (section 4).
3. Les coffres apparaissent à leurs emplacements et s'ouvrent.
4. Les volants tournent au-dessus de la clairière sans traverser le décor de trop près.
5. La nuit (Difficile, Impossible), la carte reste lisible.
