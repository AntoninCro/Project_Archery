# Guide : le boss final (vague 6)

> **Où en est ce guide** (13 octobre) : tout a été monté par Claude, directement dans les fichiers. Il n'y a rien à monter à la main. **Rien n'a encore été testé dans Unity ni dans le casque** : la section « Tester » dit quoi vérifier, et la section « Régler » quoi changer si quelque chose cloche.

Le déroulé et les chiffres sont dans le GDD, section 10 bis. Ce guide dit où sont les choses, comment les tester et comment les régler.

## 1. Ce qui a été fait

**Dans la scène `Prototype_Tir`** (sous `Map > Lair`) :

- `Lair Gate` : la grande porte, à 4 m devant la face du fond de la voie du milieu, tournée vers la tour. Le script `Lair Gate` fabrique tout lui-même : le porche de pierre, les deux battants, l'intérieur noir, les braseros, l'inscription. Ces objets générés apparaissent sous la porte dans la hiérarchie (« Porte (générée) »), mais ils ne sont pas enregistrés : on ne les modifie pas à la main, on règle le composant.
- `Final Boss` : le script `Final Boss Director`. Il ouvre la porte au début de la vague 6, charge l'arène quand le joueur passe le seuil, puis ramène le joueur à la tour ou termine la partie.

**Une nouvelle scène, `Scenes/Arena_Dragon`**, chargée par-dessus la carte (à 300 m au-dessus d'elle : sous 100 m de profondeur, les flèches disparaissent) :

- `Arena` : le script `Dragon Arena Builder` fabrique la grotte (sol, parois, voûte, lave, piliers, rochers, stalactites). Le script `Arena Lava` gère la brûlure de la lave et les flaques de la phase 3.
- `Arena > Ceiling Crystals` : les cristaux du plafond, en quatre quarts, avec une lumière par quart.
- `Arena > Player Spawn` : là où arrive le joueur.
- `Arena > Dragon` : `Health`, `Enemy` (définition `Data/Enemies/Boss_Dragon`), `Boss` (son nom) et `Dragon Boss` (son comportement). Le modèle est le prefab `Red` du pack, en enfant.
- `Arena Director` : la musique, l'intro au rythme, la lumière, la barre de PV et les deux cibles de la victoire.
- `Arena Volume` : bloom et vignette rouge (`Data/Arena Volume Profile`).

**Fichiers ajoutés :**

- les scripts `Scripts/FinalBoss/*` ;
- les shaders `Archery/Lava` et `Archery/Crystal` ;
- les matériaux `Arena_*` et `Lair_Darkness` ;
- l'Animator `Animations/DragonBoss`, avec les animations du pack mais sans enchaînement automatique : celui du pack passait d'une animation à l'autre toutes les quelques secondes ;
- la musique `Audio/Music/music_final_boss.mp3`.

**Réglages modifiés :**

- `WaveSettings` : *Waves To Win* passe à 6, *Final Boss On Last Wave* est coché ;
- `Score Manager` : la baisse de l'or commence à la vague 7 ;
- la scène `Arena_Dragon` est ajoutée dans *File > Build Profiles > Scene List*.

## 2. Tester

Ouvre `Prototype_Tir` et lance *Play*. Raccourcis clavier :

| Touche | Quand | Effet |
|---|---|---|
| **N** (×6) | avant | enchaîne les vagues jusqu'à la vague 6 (N lance la vague, N appelle le boss, N le tue) |
| **N** | vague 6, sur la carte | entre tout de suite dans l'arène, sans marcher jusqu'à la porte |
| **N** | pendant l'intro | passe l'intro (cristaux allumés, barre pleine, réveil) |
| **N** | pendant le combat | tue le dragon |
| **I** / **T** | après la victoire | choisit « Continuer » ou « Terminer » sans tirer |

À vérifier, dans l'ordre :

1. **La porte**, sans lancer le jeu : au bout de la voie du milieu, fermée, avec l'inscription au-dessus. Coche *Preview Open* sur `Lair Gate` pour la voir ouverte, puis décoche-le.
2. **Vague 6** : le message « Boss final », pas d'ennemis ni de coffres. La montre affiche « Antre » et « Vague 6 / 6 · Boss final ». La porte s'ouvre en grinçant et les braseros s'allument.
3. **La marche vers l'antre** : la musique et l'ambiance baissent à mesure que tu t'éloignes de la tour, jusqu'au silence devant la porte.
4. **Le seuil** : en marchant dans le porche, la porte claque derrière toi et l'image devient noire.
5. **L'intro** (au quart du tempo, environ 17 s) : noir complet, puis la musique, un peu moins forte. De 6,9 s (la grosse attaque) à 12 s, les quarts de cristaux s'allument un par un et la musique remonte. À 13,7 s, la barre apparaît et se remplit en 3,4 s, pendant que les coulées de lave descendent les parois. À 17,1 s, barre pleine, le dragon se réveille en rugissant.
6. **Phase 1** : il marche vers toi, frappe de près et crache du feu de loin. Cache-toi derrière un pilier : la flamme ne doit plus te toucher.
7. **Phase 2** (sous 66 % des PV) : il s'envole, lance des boules de feu (essaie d'en abattre une) et fait des piqués annoncés par une bande rouge.
8. **Phase 3** (sous 33 %) : il se pose, les cristaux rougissent, des rochers tombent (cercles rouges) et des flaques de lave s'étendent.
9. **La victoire** : deux cibles. *Continuer* te ramène en haut de la tour avec la boutique (bouton « Terminer la partie » compris), et le gong lance la vague 7. *Terminer* affiche « Victoire ! » et le classement.
10. **La mort dans l'arène** : écran de fin, puis *Rejouer* doit revenir à une carte normale (musique, ciel).

Ouvrir `Arena_Dragon` seule permet de voir la grotte, éclairée, avec le dragon endormi. Mais le combat ne se lance que depuis la partie.

## 3. Régler

| Si… | Où | Quoi |
|---|---|---|
| le dragon est trop grand ou trop petit | `Arena > Dragon > Model (Red Dragon)` | *Scale* (et `Dragon Boss` > *Body Radius*, la place qu'il prend au sol) |
| le dragon avance à reculons | même objet | *Rotation Y* à 180 |
| les flèches le traversent ou le touchent dans le vide | `Dragon Boss` | *Hitbox Scale* (pendant le jeu, sélectionne le dragon pour voir ses zones de touche en vert dans la vue *Scene*) |
| il est trop solide ou trop fragile | `Data/Enemies/Boss_Dragon` | *Max Health* (3 000 de base, environ 7 200 en Normal à la vague 6) |
| il fait trop mal | `Boss_Dragon` | *Attack Damage* (toutes ses attaques en dépendent) |
| les phases changent trop tôt ou trop tard | `Dragon Boss` | *Air Phase At*, *Enraged Phase At* |
| le souffle est trop dur à éviter | `Dragon Boss` | *Breath Tracking* (°/s), *Breath Angle*, *Breath Damage* |
| l'intro est décalée par rapport à la musique | `Arena Director` | *First Beat* (0,04 s), *Bpm* (140,26) |
| l'intro est trop lente ou trop rapide | `Arena Director` | *Music Beats Per Intro Beat* (4 = quart du tempo, 2 = moitié, 1 = tempo du morceau) ; les autres réglages de l'intro se comptent en temps de l'intro |
| la musique est trop forte ou trop faible dans le noir de l'intro | `Arena Director` | *Dark Music Volume* (0,6) |
| la musique de la carte baisse trop tôt en partant vers l'antre | `Map > Lair > Final Boss` | *Full Volume Radius* (12 m autour de la tour) |
| la grotte est trop sombre ou trop claire | `Arena Director` / `Ceiling Crystals` | *Ambient Lit* ; *Lit Intensity*, *Light Intensity* |
| la porte dépasse dans la falaise | `Lair Gate` | *Opening Width*, *Pillar Width* |

## 4. Retravailler la grotte à la main

La grotte est fabriquée par le script `Dragon Arena Builder` (objet `Arena`), donc on ne peut pas déplacer ses morceaux tels quels. Deux façons de la reprendre :

- **La figer** : clic droit sur le composant `Dragon Arena Builder` (ou ses trois points ⋮), puis *Figer la grotte en objets modifiables*. Les morceaux deviennent des objets normaux sous `Arena > Grotte (modifiable)`, avec leurs maillages dans `Art/Arena`. Tu peux alors tout déplacer, supprimer ou remplacer, puis enregistrer la scène. Ctrl+Z annule.
- **Décocher des parties** : dans *Parties générées*, décoche celles que tu remplaces par tes propres modèles (sol, parois, anneau de lave, coulées, piliers, rochers, stalactites). Les autres restent générées.

Ce que le jeu attend de ta grotte :

- **Un sol avec collider** pour le joueur, et des **parois avec collider** pour qu'il ne sorte pas.
- **Les piliers** : ajoute le composant `Arena Pillar` à chacun (rayon au sol) et un collider. Le dragon les contourne, la flamme et les boules de feu s'y arrêtent, et ni les rochers ni les flaques de lave n'y tombent. Les piliers figés l'ont déjà.
- **La lave** : le matériau `Arena_Lava` (anneau, flaques) s'éteint pendant l'intro et s'allume avec les cristaux. Le matériau `Arena_LavaFall` (coulées) descend du haut vers le bas pendant le remplissage de la barre : mets-le sur tes propres coulées, et règle *Lava Fall Top Height* du builder sur le haut de ta plus haute coulée. La brûlure de la lave suit toujours l'anneau défini par *Lava Ring Start* (22 m du centre).
- **La voûte** : les rochers tombent et les cristaux s'accrochent selon *Floor Radius* et *Ceiling Height*. Si tu changes beaucoup la forme de la grotte, garde ces deux réglages proches de ta voûte.
- **Les cristaux** restent générés (`Ceiling Crystals`) : leurs couleurs (*Palette*), leur nombre et leur taille se règlent dans l'Inspector.

## 5. En cas de problème

- **On reste dans le noir à l'entrée** : la console dit sans doute que la scène `Arena_Dragon` est introuvable. Ajoute-la dans *File > Build Profiles > Scene List*.
- **Le dragon reste en T (pose de référence)** : `Dragon Boss` > *Controller* doit être `Animations/DragonBoss`.
- **Tout est rose** : un shader n'a pas compilé (`Archery/Lava` ou `Archery/Crystal`). Envoie-moi l'erreur de la console.
- **Les cristaux ou la lave ne brillent pas** : la caméra doit avoir le post-traitement activé (*Rendering > Post Processing*) pour le bloom. Ils restent visibles sans.
