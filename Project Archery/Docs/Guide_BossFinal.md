# Guide : le boss final (vague 6)

> **Où en est ce guide** (13 octobre) : tout a été monté par Claude, directement dans les fichiers. Il n'y a rien à monter à la main. **Rien n'a encore été testé dans Unity ni dans le casque** : la section « Tester » dit quoi vérifier, et la section « Régler » quoi changer si quelque chose cloche.

Le déroulé et les chiffres sont dans le GDD, section 10 bis. Ce guide dit où sont les choses, comment les tester et comment les régler.

## 1. Ce qui a été fait

**Dans la scène `Prototype_Tir`** (sous `Map > Lair`) :

- `Lair Gate` : la grande porte, à 4 m devant la face du fond de la voie du milieu, tournée vers la tour. Le script `Lair Gate` fabrique tout lui-même : le porche de pierre, les deux battants, l'intérieur noir, les braseros, l'inscription. Ces objets générés apparaissent sous la porte dans la hiérarchie (« Porte (générée) »), mais ils ne sont pas enregistrés : on ne les modifie pas à la main, on règle le composant.
- `Final Boss` : le script `Final Boss Director`. Il ouvre la porte au début de la vague 6, charge l'arène quand le joueur passe le seuil, puis ramène le joueur à la tour ou termine la partie.

**Une nouvelle scène, `Scenes/Arena_Dragon`**, chargée par-dessus la carte (à 300 m au-dessus d'elle : sous 100 m de profondeur, les flèches disparaissent) :

- `Arena` : le script `Arena Grid Builder` fabrique la grotte « la fourche » à partir de son plan en texte (section 6). L'ancienne grotte ronde (`Dragon Arena Builder`) n'est plus utilisée, mais son script reste. Le script `Arena Lava` gère la brûlure de la lave et les flaques de la phase 3.
- `Arena > Ceiling Crystals` : les cristaux du plafond, en quatre quarts, avec une lumière par quart.
- `Arena > Player Spawn` : là où arrive le joueur.
- `Arena > Dragon` : `Health`, `Enemy` (définition `Data/Enemies/Boss_Dragon`), `Boss` (son nom) et `Dragon Boss` (son comportement). Le modèle est le prefab `Red` du pack, en enfant. Le script `Dragon Boss` est réparti en cinq fichiers (14 et 15 octobre) : `DragonBoss.cs` (phases, réglages), `DragonBoss.Intro.cs` (intro : cascade, ruée, feu vers la voûte), `DragonBoss.Ground.cs` (attaques au sol, dont l'anneau de feu, script `FireRing`), `DragonBoss.Air.cs` (vol, ruées, perchoirs, salto) et `DragonBoss.Body.cs` (os, zones de touche, yeux, poses ajoutées). Matériaux : `Dragon_Fireball` (boules de feu, lueur de la gueule) et `Dragon_Eyes`, sur le shader `Archery/Fireball`.
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
5. **L'intro** (au quart du tempo, environ 17 s) : noir complet, puis la musique, un peu moins forte. De 6,9 s (la grosse attaque) à 12 s, les quarts de cristaux s'allument un par un et la musique remonte. Le dragon est invisible jusqu'au premier quart, puis apparaît debout en haut de la coulée de lave du fond. Vers 12 s, il s'envole, monte au-dessus de l'arène, puis pique sur le centre et s'y pose dans une gerbe de flammes. À 13,7 s, la barre apparaît et se remplit en 3,4 s, pendant qu'il crache un jet de feu vers la voûte et que les coulées de lave descendent les parois. À 17,1 s, barre pleine, il cesse de cracher et le combat commence.
6. **Phase 1** : ses yeux brillent en rouge ; en marchant, ses pattes ne patinent pas. Il reste à une dizaine de mètres et crache d'énormes boules de feu : déplace-toi, elles doivent te rater (essaie d'en abattre une, même dans sa gueule). Le souffle : il lève la tête, une lumière grandit dans sa gueule, puis il crache ; tourne autour de lui de près ou cache-toi derrière un pilier. Approche-toi : il lève une patte et rafle le sol, saute au bon moment (« Esquivé ! »). Colle-toi à sa gueule : il croque. Éloigne-toi : il se dresse sur ses pattes arrière, frappe le sol, et un anneau de feu traverse toute l'arène ; saute quand il arrive sur toi (« Esquivé ! »). Avant que la cascade de lave coule (intro), on doit voir de la roche derrière elle, pas du ciel.
7. **Phase 2** (sous 66 % des PV) : il s'envole, crache ses boules de feu d'en haut, fait trois ruées de suite (sors de chaque bande rouge avant qu'il passe), et se pose sur un pilier ou le bord du plateau pour lancer son souffle.
8. **Phase 3** (sous 33 %) : il se pose, rugit, les cristaux rougissent, puis le salto : triple salto tout raide, arrêt, cercle rouge, piqué là où tu étais ; bouge. Ensuite il alterne vite sol et vol, plus rapide, et des flaques de lave s'étendent. Une flèche de glace ne doit pas le ralentir de plus d'un quart.
9. **La victoire** : deux cibles. *Continuer* te ramène en haut de la tour avec la boutique (bouton « Terminer la partie » compris), et le gong lance la vague 7. *Terminer* affiche « Victoire ! » et le classement.
10. **La mort dans l'arène** : écran de fin, puis *Rejouer* doit revenir à une carte normale (musique, ciel).

Ouvrir `Arena_Dragon` seule permet de voir la grotte, éclairée, avec le dragon endormi. Mais le combat ne se lance que depuis la partie.

## 3. Régler

| Si… | Où | Quoi |
|---|---|---|
| le dragon est trop grand ou trop petit | `Arena > Dragon > Model (Red Dragon)` | *Scale* (et `Dragon Boss` > *Body Radius*, la place qu'il prend au sol) |
| le dragon avance à reculons | même objet | *Rotation Y* à 180 |
| les flèches le traversent ou le touchent dans le vide | `Dragon Boss` | *Hitbox Scale* (pendant le jeu, sélectionne le dragon pour voir ses zones de touche en vert dans la vue *Scene*) |
| il est trop solide ou trop fragile | `Data/Enemies/Boss_Dragon` | *Max Health* (4 500 de base, environ 10 800 en Normal à la vague 6) |
| il fait trop mal | `Boss_Dragon` | *Attack Damage* (25 ; toutes ses attaques en dépendent) |
| les phases changent trop tôt ou trop tard | `Dragon Boss` | *Air Phase At*, *Enraged Phase At* |
| ses pattes patinent quand il marche | `Dragon Boss` | *Walk Stride Speed*, *Run Stride Speed* (0 = mesurée sur l'animation ; sinon la vitesse en m/s à laquelle l'animation avance : plus grand = pattes plus lentes) |
| les yeux sont cachés dans la tête, trop gros ou trop petits | `Dragon Boss` | *Eye Offset*, *Eye Radius* ; leur couleur dans `Materials/Dragon_Eyes` |
| les boules de feu sont trop dures à esquiver | `Dragon Boss` | *Fireball Speed*, *Air Fireball Speed*, *Fireball Size*, *Fireball Splash* ; leur rythme : *Spit Cooldown*, *Air Fireball Interval* |
| le souffle est trop dur à éviter | `Dragon Boss` | *Breath Tracking* (°/s), *Breath Angle*, *Breath Damage* ; la durée de la charge : *Breath Charge* (3,2 s) |
| l'arène est trop petite ou trop grande | `Arena` (composant `Arena Grid Builder`) | *Cell Size* (2,5 m) ; voir la section 6, « Les tailles » |
| son envol de l'intro part trop tôt ou trop tard | `Arena Director` | *Dragon Dash Beat* (7 : avec le dernier quart de cristaux) ; la hauteur d'où il pique est fixée à 12 m dans `DragonBoss.Intro.cs` |
| on n'arrive pas à sauter par-dessus la patte | `Dragon Boss` | *Sweep Windup* (temps pour la voir venir), *Sweep Time*, *Jump Clearance* (hauteur des pieds à atteindre, 0,3 m) |
| l'anneau de feu est trop dur à sauter, trop fréquent ou se voit mal | `Dragon Boss` | *Slam Ring Speed* (10 m/s), *Slam Clearance* (0,4 m), *Slam Ring Height* (0,6 m), *Slam Cooldown* (13 s), *Slam Rear Time* (temps pour le voir venir), *Slam Damage* |
| il se dresse trop ou pas assez | `Dragon Boss` | *Slam Rear Angle* (40°) |
| les ruées sont trop dures | `Dragon Boss` | *Dash Warning* (temps de la bande rouge), *Dash Width*, *Dash Speed*, *Dash Count* |
| le salto est trop dur ou trop facile | `Dragon Boss` | *Salto Pause* (arrêt avant le piqué), *Salto Radius*, *Salto Damage*, *Salto Flip Time* |
| la phase 3 est trop rapide | `Dragon Boss` | *Rage Tempo* (1,3), *Rage Ground Time*, *Rage Air Time* |
| la glace le ralentit trop ou pas assez | `Dragon Boss` | *Max Slow* (0,25 : 25 % au plus, toutes flèches confondues) |
| l'intro est décalée par rapport à la musique | `Arena Director` | *First Beat* (0,04 s), *Bpm* (140,26) |
| l'intro est trop lente ou trop rapide | `Arena Director` | *Music Beats Per Intro Beat* (4 = quart du tempo, 2 = moitié, 1 = tempo du morceau) ; les autres réglages de l'intro se comptent en temps de l'intro |
| la musique est trop forte ou trop faible dans le noir de l'intro | `Arena Director` | *Dark Music Volume* (0,6) |
| la musique du boss est trop faible ou trop forte | `Arena Director` | *Music Boost Db* (+6 dB au-dessus du réglage des paramètres, pendant le combat) |
| la musique de la carte baisse trop tôt en partant vers l'antre | `Map > Lair > Final Boss` | *Full Volume Radius* (12 m autour de la tour) |
| la grotte est trop sombre ou trop claire | `Arena Director` / `Ceiling Crystals` | *Ambient Lit* ; *Lit Intensity*, *Light Intensity* |
| la porte dépasse dans la falaise | `Lair Gate` | *Opening Width*, *Pillar Width* |

## 4. Retravailler la grotte à la main (ancienne grotte ronde)

> Cette section concerne l'ancienne grotte ronde. La fourche se retravaille par son plan et son composant : voir la section 6.

La grotte est fabriquée par le script `Dragon Arena Builder` (objet `Arena`), donc on ne peut pas déplacer ses morceaux tels quels. Deux façons de la reprendre :

- **La figer** : clic droit sur le composant `Dragon Arena Builder` (ou ses trois points ⋮), puis *Figer la grotte en objets modifiables*. Les morceaux deviennent des objets normaux sous `Arena > Grotte (modifiable)`, avec leurs maillages dans `Art/Arena`. Tu peux alors tout déplacer, supprimer ou remplacer, puis enregistrer la scène. Ctrl+Z annule.
- **Décocher des parties** : dans *Parties générées*, décoche celles que tu remplaces par tes propres modèles (sol, parois, anneau de lave, coulées, piliers, rochers, stalactites). Les autres restent générées.

Ce que le jeu attend de ta grotte :

- **Un sol avec collider** pour le joueur, et des **parois avec collider** pour qu'il ne sorte pas.
- **Les piliers** : ajoute le composant `Arena Pillar` à chacun (rayon au sol) et un collider. Le dragon les contourne, la flamme et les boules de feu s'y arrêtent, il se perche sur leur sommet, et les flaques de lave n'y tombent pas. Les piliers figés l'ont déjà.
- **La lave** : le matériau `Arena_Lava` (anneau, flaques) s'éteint pendant l'intro et s'allume avec les cristaux. Le matériau `Arena_LavaFall` (coulées) descend du haut vers le bas pendant le remplissage de la barre : mets-le sur tes propres coulées, et règle *Lava Fall Top Height* du builder sur le haut de ta plus haute coulée. La brûlure de la lave suit toujours l'anneau défini par *Lava Ring Start* (22 m du centre).
- **La voûte** : les cristaux s'accrochent selon *Floor Radius* et *Ceiling Height*. Si tu changes beaucoup la forme de la grotte, garde ces deux réglages proches de ta voûte.
- **Les cristaux** restent générés (`Ceiling Crystals`) : leurs couleurs (*Palette*), leur nombre et leur taille se règlent dans l'Inspector.

## 5. En cas de problème

- **On reste dans le noir à l'entrée** : la console dit sans doute que la scène `Arena_Dragon` est introuvable. Ajoute-la dans *File > Build Profiles > Scene List*.
- **Le dragon reste en T (pose de référence)** : `Dragon Boss` > *Controller* doit être `Animations/DragonBoss`.
- **Tout est rose** : un shader n'a pas compilé (`Archery/Lava`, `Archery/Crystal` ou `Archery/Fireball` pour les boules de feu et les yeux). Envoie-moi l'erreur de la console.
- **Les cristaux ou la lave ne brillent pas** : la caméra doit avoir le post-traitement activé (*Rendering > Post Processing*) pour le bloom. Ils restent visibles sans.

## 6. L'arène en cubes : la fourche

> **Où en est cette partie** (14 octobre) : c'est l'arène du combat, dans `Scenes/Arena_Dragon` (objet `Arena`). La scène `Scenes/Arena_Fourche` garde une copie pour la visiter seule, avec un joueur VR et les commandes clavier ; le plan est le même fichier, mais les réglages du composant sont propres à chaque scène. Pas de ponts : on franchit les bras de lave en courant aux bras et en sautant (lever les deux bras d'un coup). **Pas encore testée en combat.**

- **Le plan** : `Data/Arena/Arena_Fourche.txt`, une lettre par case (30 × 30). La légende est en tête du fichier : `#` roche, `^` plateau en hauteur, `.` sol, `~` lave, `!` coulée sur le plateau, `o` pilier, `D` estrade du dragon, `T` tourelle, `S` arrivée, `P` porte. Change une lettre et enregistre : le sol se refait tout seul dans la scène ouverte.
- **Les tailles** : sur `Arena (sol)`, composant `Arena Grid Builder`. *Cell Size* est le côté d'un cube : 2,5 m depuis le 15 octobre (75 m pour la grotte ; c'était 2 m, 60 m). 3 m serait la grille de la carte (90 m), mais les bras de lave feraient 6 m, trop larges pour les sauter. Tout suit ce réglage (sol, hauteurs, voûte, piliers, perchoirs, repères, arrivée du joueur), sauf le dragon et le décor (rochers, armes), qui gardent leur taille. Si tu le changes encore, règle aussi en proportion `Ceiling Crystals` (*Clusters*, *Light Range*, *Light Intensity*) et, sur `Dragon Boss`, *Fly Altitude*, *Orbit Radius*, *Dash Length*, *Dash Altitude*. Hauteurs en cubes : plateau 2, parois provisoires 6, piliers 4. Estrade 1 m, lave 0,4 m sous le sol.
- **Les repères** : sous « Sol (généré) > Repères » : l'arrivée du joueur, la porte, le dragon (sur l'estrade, tourné vers la porte) et les 6 tourelles (sur le plateau, tournées vers le centre). Dans la vue *Scene*, ils sont dessinés en vert, rouge et bleu ; sélectionne `Arena (sol)` pour voir le quadrillage des cases.
- **Les perchoirs du dragon** : le dessus des piliers, et des places au bord du plateau, choisies d'après le plan (là où le plateau domine l'arène, avec de la place derrière, sous une voûte assez haute, loin des tourelles, à 9 m les unes des autres). Sélectionne `Arena (sol)` : celles du plateau sont dessinées en orange, avec la direction du regard. Le décor n'y pose ni rocher ni stalagmite.
- **Le dragon pendant l'intro** : il attend en haut de la coulée de lave (les cases `!` qui tombent dans la lave du sol), une case en retrait du bord, tourné vers l'arène ; c'est dessiné en jaune quand `Arena (sol)` est sélectionné. Sa place dans la scène ne sert plus que pour une arène sans coulée (il y dort). Il s'envole ensuite et pique sur le centre de l'arène : l'instant se règle sur `Arena Director` > *Dragon Dash Beat* (7 : avec le dernier quart de cristaux).
- **L'arrivée du joueur** : `Arena > Player Spawn` suit tout seul la case `S` du plan.
- **Le décor** (section *Décor* du composant) : il est tiré du plan et se refait avec lui. Dalles sombres et sol brûlé au bord de la lave et autour du nid ; gros rochers au pied des parois, du plateau et des piliers ; petits rochers au bord de la lave ; stalagmites dans les recoins et sur le plateau ; parois, bords du plateau, piliers, estrade et berges de la lave habillés de roche à facettes avec des strates, comme les falaises de la carte (*Rock Skin*, *Skin Depth*, *Skin Facet*, *Skin Strata*) ; haut des parois irrégulier. Au nid : colonnes brisées, statue renversée, armes et boucliers d'aventuriers tombés, lueur rouge. À la porte : deux colonnes, un linteau, un battant sombre et quatre braseros dans le couloir. Un socle de pierre sous chaque tourelle. Au-dessus de la lave : lumières orangées et braises. *Decor Seed* change la disposition, *Decor Density* la quantité, *Dark Tiles* la part de dalles sombres ; les listes de modèles se modifient dans l'Inspector (kit Nature de Kenney, armes de Dimasjk). Décoche *Decorate* pour revoir le sol nu.
- **Les craquelures du sol** : de fines fissures rouges qui brillent sur tout le sol (texture `Art/Arena/Fourche_Cracks`, sans raccord, 8 m de côté), plus vives au bord de la lave. Elles sont dans l'émission des matériaux `Fourche_Floor`, `Fourche_FloorDark` et `Fourche_FloorScorched` (propres à cette arène) : *Emission* règle leur couleur et leur éclat. En jeu, leur lueur respire lentement (*Crack Pulse*, *Crack Pulse Period*) ; *Floor Texture Size* règle leur taille. La lumière ambiante de la scène vient surtout du sol, en rouge, comme si les fissures éclairaient la grotte par en dessous.
- **La voûte** (section *Plafond*) : un plafond en cubes ferme la grotte, bas près des parois (*Ceiling Low Levels*, 9 cubes = 18 m) et de plus en plus haut vers le centre (*Ceiling High Levels*, 13 cubes = 26 m), d'un cube toutes les *Ceiling Rise* cases, avec des marches irrégulières. Il est en roche à facettes, avec des stalactites (assez courtes pour laisser voler le dragon, à 10 m). Les parois montent jusqu'à lui. Vue d'en haut dans la vue *Scene*, la voûte est invisible : on voit toujours l'intérieur. La lumière directionnelle de la scène la traverse encore (éclairage provisoire, pour bien voir) ; l'éclairage de grotte viendra avec le combat.
- **Retoucher à la main** : clic droit sur le composant (ou ⋮), *Figer le sol en cubes modifiables*. Chaque case devient un cube sous « Sol (modifiable) », qu'on peut monter, baisser ou remplacer, et le décor devient des objets normaux (les modèles restent liés à leurs fichiers) ; la génération se coupe. Ctrl+Z annule. Après ça, modifier le fichier texte ne change plus rien : à faire une fois le plan bien calé.
