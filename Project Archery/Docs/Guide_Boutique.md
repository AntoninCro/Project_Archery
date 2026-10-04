# Guide : la boutique, les améliorations et les arcs

Ce guide ajoute la boutique du GDD (sections 5, 6 et 7) à la scène `Prototype_Tir`.

À la fin de chaque vague, un panneau s'ouvre en haut de la tour, à ta gauche. On vise une carte avec le rayon de la main libre et on appuie sur la **gâchette** pour l'acheter.

Le panneau propose :
- **4 améliorations** tirées au sort selon leur rareté : **communes** (blanc, 70 %), **rares** (bleu, 25 %) et **légendaires** (doré, 5 %), avec les mêmes chances toute la partie. Elles sont listées juste en dessous ;
- **le prochain arc** : composite, puis long, puis runique ;
- **la tour** : la réparer (+25 % de PV) ou la reconstruire si elle est détruite ;
- **relancer** les 4 améliorations : 10 or, puis 5 de plus à chaque relance. Le prix revient au départ à chaque pause.

**Aucune limite d'achat** : chaque amélioration se prend autant de fois qu'on veut, et tout se cumule jusqu'à la fin de la partie.
- Les bonus s'additionnent.
- Les chances (foudre, explosion, glace) s'additionnent jusqu'à 100 %. Au-delà, chaque exemplaire rend l'effet plus fort : plus de dégâts, un ralentissement plus fort et plus long, une zone plus grande (détails dans le GDD, section 6.2).
- Le multitir, le tir écho, le déluge et le perçage sont des moyennes : 150 %, c'est 1 flèche (ou 1 écho, 1 division, 1 ennemi traversé) à coup sûr, et 50 % de chance d'une deuxième.

Les prix montent de trois façons, qui se cumulent (tableau dans le GDD, section 7) :
- **+10 % à chaque vague** (la relance aussi) ;
- **+5 % à chaque amélioration obtenue** (achetée, ou trouvée dans un coffre) : plus tu en as, plus la suivante coûte cher. Après un achat, les prix des autres cartes montent tout de suite ;
- **en mode infini, ×1,2 à chaque vague**, pour tout (relance, arc et tour compris).

L'or gagné baisse aussi à partir de la vague 5 (GDD, section 8), et les flèches en plus ne rapportent pas de points de touche.

### Les 19 améliorations

| Amélioration | Rareté | Effet de chaque exemplaire |
|---|---|---|
| Dégâts | commune | +10 % de dégâts |
| Flèches rapides | commune | +8 % de vitesse des flèches (plus de portée) |
| Charge rapide | commune | anneau 15 % plus rapide, bandes un peu plus larges |
| Précision | commune | bande verte (tir parfait) 15 % plus large |
| Vitalité | commune | +15 PV max |
| Butin | commune | +20 % d'or : 30 % des points deviennent de l'or au lieu de 25 % |
| Chasseur de têtes | commune | +25 % de dégâts à la tête |
| Chance | commune | les cartes rares et légendaires sortent plus souvent (+25 % de poids) ; il reste toujours au moins 40 % de communes |
| Multitir | rare | +50 % de chance de tirer une flèche en plus : à côté de la flèche de l'arc, puis en éventail de chaque côté |
| Flèche de foudre | rare | +20 % de chance par flèche : éclair, cible ralentie de 40 % pendant 2 s |
| Perçage | rare | +25 % de chance par flèche de traverser un ennemi |
| Vampirisme | rare | chaque tir à la tête rend 2 PV |
| Tir écho | rare | +25 % de chance que la volée se répète 0,25 s après, à la même puissance |
| Flèche de glace | rare | +25 % de chance par flèche : zone de glace au sol (3 m, 5 s) qui ralentit les ennemis de 50 % |
| Déluge | légendaire | +50 % de chance que chaque flèche se divise en deux, en vol ; elle continue tout droit |
| Chaîne d'éclairs | légendaire | la foudre rebondit sur 3 ennemis proches de plus |
| Flèche explosive | légendaire | +25 % de chance par flèche d'exploser et de toucher les ennemis autour |
| Auto-visée | légendaire | les flèches dévient vers l'ennemi le plus proche s'il est à moins de 12 m devant elles (+30° par seconde) |
| Tir ricochet | légendaire | après un ennemi, la flèche rebondit vers un autre ennemi proche (+1 rebond) |

Quelques règles :
- **Les flèches spéciales se reconnaissent à leur traînée** : bleue pour la foudre, orange pour l'explosion, bleu pâle pour la glace.
- **Les flèches en plus sont de vraies flèches** : celles du multitir, du tir écho et du déluge tirent au sort leurs propres effets (foudre, explosion, glace, perçage). Elles profitent de l'auto-visée et du ricochet, et ne cassent pas le combo quand elles ratent. Elles ne rapportent pas de points de touche (seulement leurs éliminations) et ne sifflent pas en vol.
- **La volée reste centrée sur la visée** : la flèche de l'arc part toujours tout droit, et les autres se placent par paires, à gauche et à droite. Avec un nombre pair de flèches, celle qui n'a pas de paire part 20 cm à côté de la flèche de l'arc, dans la même direction. Avec 2 flèches, les deux vont donc là où tu vises. Le déluge suit la même règle autour de la flèche qui se divise.
- **L'ordre** : le multitir ajoute des flèches à la volée, l'écho répète toute la volée, puis le déluge divise en vol chaque flèche, environ 0,2 s après son départ. Les flèches nées d'une division ne se divisent pas à leur tour.
- **Une sécurité** garde le jeu fluide : au-delà de 150 flèches en vol en même temps, on n'en ajoute plus (*Max Arrows In Flight* de `Special Arrows`).
- **L'auto-visée** reste légère : la flèche tourne de 30° par seconde et par exemplaire, et jamais vers un ennemi derrière elle.
- **Le ricochet** vise un peu au-dessus de l'ennemi suivant pour compenser la chute de la flèche. Comme le perçage, chaque rebond enlève 20 % des dégâts.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Shop Catalog` | Asset de données : améliorations, raretés, prix, arcs, services de la tour | `Data/Shop` |
| `Shop Manager` | Ouvre la boutique entre les vagues, tire les offres, gère les achats | Objet `Game` |
| `Player Upgrades` | Garde les améliorations achetées et applique leurs bonus | Objet `Game` |
| `Special Arrows` | Flèches spéciales : division, écho, perçage, foudre, explosion, glace, auto-visée, ricochet | Objet `Game` |
| `Shop Panel` | Affiche la boutique et transmet les clics | Canvas de la boutique |
| `Shop Card` | Une carte : nom, rareté, description, prix | Chaque carte |

Les sons sont dans `Audio/Placeholder` : `shop_buy`, `shop_error`, `shop_reroll`, `tower_repair`, `lightning`, `explosion`, `ice_zone` et `ricochet`. Le tir écho réutilise `bow_release`.

## 1. Le catalogue

1. Dans `Assets/_Project/Data`, crée un dossier `Shop`.
2. Dedans : clic droit > *Create > Archery > Shop Catalog*, nomme-le `ShopCatalog`.
3. Menu **⋮** de l'Inspector > *Remplir avec le GDD*. Cela remplit les 19 améliorations du tableau, les chances des raretés et les prix.
4. Dans la liste *Bows*, glisse les 4 arcs de `Data/Bows`, dans l'ordre : `Bow_Chasse`, `Bow_Composite`, `Bow_Long`, `Bow_Runique`.
   - Le premier est l'arc de départ.
   - La boutique propose toujours le suivant, à partir de la vague indiquée dans son asset : composite avant la vague 2, long avant la vague 4, runique avant la vague 7.

Chaque amélioration a un nom, une description, une rareté, un effet, une valeur et un niveau maximal (*Max Stacks*). Laisse *Max Stacks* à `0` : il n'y a pas de limite d'achat. L'infobulle de *Value* explique ce que représente la valeur pour chaque effet.

Par exemple, pour que **chaque** tir ait son écho, mets la *Value* du *Tir écho* à `1` (100 %). Pense alors à changer sa description.

> **Ta boutique est déjà montée ?** Il n'y a rien à refaire : l'asset `ShopCatalog` a été mis à jour avec les nouvelles valeurs (19 améliorations sans limite, raretés fixes, prix progressifs). Si tu l'as modifié à la main depuis, refais *Remplir avec le GDD* : la liste *Bows* n'est pas touchée.

## 2. Les gestionnaires

1. Sur l'objet `Game` :
   - *Add Component > Shop Manager* : *Catalog* `ShopCatalog`. Laisse *Bow* vide, il trouve l'arc de la scène.
   - *Add Component > Player Upgrades*.
   - *Add Component > Special Arrows*, avec ses sons :
     - *Echo Clip* `bow_release` ;
     - *Lightning Clip* `lightning` ;
     - *Explosion Clip* `explosion` ;
     - *Frost Clip* `ice_zone` ;
     - *Ricochet Clip* `ricochet` ;
     - *Deluge Clip* : laisse-le vide. Avec beaucoup de flèches, un son à chaque division devient vite trop chargé.
2. Crée un matériau `Effects` avec le shader *Archery > UnlitVertexColor*. Mets-le dans *Effect Material* de `Special Arrows` : c'est le matériau des éclairs et des zones de glace.
3. Sur `Tower` : *Repair Clip* `tower_repair`.

Les autres réglages de `Special Arrows` (angle du multitir, délai de l'écho, moment et écart de la division du déluge, dégâts de la foudre, rayon de l'explosion et de la glace, portée de l'auto-visée et du ricochet…) ont des valeurs par défaut prêtes à l'emploi. Chacun a son infobulle.

## 3. Cliquer au rayon de la manette

L'interface d'Unity a besoin d'un module d'entrée XR pour réagir au rayon des manettes.

1. Sélectionne `EventSystem`.
2. Supprime son composant *Input System UI Input Module*.
3. *Add Component > XR UI Input Module*.

Les manettes du XR Origin ont déjà l'interaction avec l'interface activée. Mais une main qui tient quelque chose (l'arc, une flèche) ne clique pas : on clique avec la main libre et vide.

## 4. Le panneau de la boutique

### Le Canvas

1. *GameObject > UI > Canvas*, nomme-le `Shop` :
   - *Render Mode* `World Space` ;
   - Position `(-1.4, 5.2, 1.6)`, Rotation `(15, -41, 0)` : à ta gauche, un peu sous les yeux, penché vers toi ;
   - Width `1400`, Height `1000`, Scale `(0.001, 0.001, 0.001)` : 1,4 m de large.
2. *Add Component > Tracked Device Graphic Raycaster* : c'est lui qui laisse le rayon des manettes toucher les boutons. Tu peux garder le *Graphic Raycaster*.
3. Dans `Shop`, *UI > Image*, nomme-la `Content` :
   - étire-la sur tout le Canvas (*Anchor Presets*, Alt + clic sur l'étirement complet) ;
   - couleur noire, alpha 170.
4. Dans `Content`, trois textes (*UI > Text - TextMeshPro*) :

| Nom | Ancre | Position | Width × Height | Police |
|---|---|---|---|---|
| `Title` | en haut au centre | `(0, -55)` | 700 × 80 | 56, gras, centré, texte « Boutique » |
| `Money` | en haut à droite | `(-200, -55)` | 360 × 70 | 44, aligné à droite, doré |
| `Message` | en bas au centre | `(0, 120)` | 1300 × 50 | 34, centré |

5. Encore un texte, `Owned` : ancre en bas au centre, position `(0, 55)`, 1300 × 60, taille 24, italique, gris clair. Il listera les améliorations possédées.
6. Dans `Content`, *Create Empty*, nomme-le `Cards` :
   - ancre au centre, position `(0, 10)`, Width `1300`, Height `640` ;
   - *Add Component > Grid Layout Group* : *Cell Size* `(305, 300)`, *Spacing* `(20, 20)`, *Child Alignment* `Upper Center`, *Constraint* `Fixed Column Count`, *Constraint Count* `4`.

### Une carte, puis le prefab

1. Dans `Cards` : *UI > Button - TextMeshPro*, nomme-le `Shop Card`.
2. *Add Component > Vertical Layout Group* :
   - *Padding* 16 partout, *Spacing* 6, *Child Alignment* `Upper Center` ;
   - coche *Control Child Size* (Width et Height) et *Child Force Expand* Width, décoche *Child Force Expand* Height.
3. Son enfant `Text (TMP)` : renomme-le `Title`, taille 30, gras, centré.
4. Duplique `Title` 3 fois (Ctrl+D) :
   - `Subtitle` : taille 22, italique ;
   - `Description` : taille 24, normal. *Add Component > Layout Element*, *Flexible Height* `1` : la description prend la place libre ;
   - `Price` : taille 32, gras.

   Garde cet ordre de haut en bas.
5. Sur `Shop Card` : *Add Component > Shop Card*. Glisse `Title`, `Subtitle`, `Description` et `Price` dans les champs du même nom. Le Button et l'Image de fond sont trouvés tout seuls.
6. Glisse `Shop Card` dans un dossier `Prefabs/UI` pour en faire un prefab.
7. Dans `Cards`, duplique la carte jusqu'à en avoir 6, et renomme-les : `Card 1`, `Card 2`, `Card 3`, `Card 4`, `Bow Card` et `Tower Card`.
8. Toujours dans `Cards`, ajoute un dernier *UI > Button - TextMeshPro*, `Reroll`, avec un texte de taille 34.

La grille place les 4 améliorations sur la première ligne, puis l'arc, la tour et la relance sur la seconde.

### Brancher le panneau

Sur `Shop` : *Add Component > Shop Panel* :
- *Content* : `Content` ;
- *Upgrade Cards* : 4 éléments, `Card 1` à `Card 4` ;
- *Bow Card* : `Bow Card`, *Tower Card* : `Tower Card` ;
- *Reroll Button* : `Reroll`, *Reroll Text* : son enfant `Text (TMP)` ;
- *Money Text*, *Message Text*, *Owned Text* : les textes du même nom ;
- sons : *Buy Clip* `shop_buy`, *Error Clip* `shop_error`, *Reroll Clip* `shop_reroll`.

Le panneau est caché au lancement. Il apparaît à la fin de chaque vague et disparaît au début de la suivante.

## 5. Les modèles des arcs (optionnel)

Sans modèle, un nouvel arc change de caractéristiques mais garde son apparence. Pour lui donner son propre modèle, fais ceci pour chacun des arcs composite, long et runique :

1. Ouvre le prefab `Bow`. Glisse un autre arc d'Easy Weapons (par exemple `Bow.RegColor.noEnch.1.1`) dans `Model`, en position et rotation `(0, 0, 0)`.
2. Prépare-le comme le premier (voir `Guide_Modeles.md`) :
   - supprime `bowWeaponControllerAA` ;
   - ajoute un `Bow Visual` avec `top.end`, `bottom.end` et la contrainte `StringDraw`.
3. Glisse ce modèle de la Hierarchy vers un dossier `Prefabs/Bows`. Choisis *Prefab Variant* et nomme-le `BowVisual_Composite`.
4. Supprime-le du prefab `Bow`, qui ne doit garder que l'arc de départ, puis enregistre.
5. Dans `Data/Bows/Bow_Composite`, glisse `BowVisual_Composite` dans *Visual Prefab*.

À l'achat, l'ancien modèle est remplacé par le nouveau, même pendant que tu tiens l'arc.

## 6. L'effet d'explosion (optionnel)

Sans effet, l'explosion fait une gerbe d'éclairs orange et un « boum ». Pour ajouter des étincelles :

1. *GameObject > Effects > Particle System*, nomme-le `Explosion`.
2. Module principal :
   - *Duration* `0.5`, décoche *Looping* ;
   - *Start Lifetime* `0.3` à `0.7`, *Start Speed* `3` à `8`, *Start Size* `0.2` à `0.6` (petite flèche à droite de chaque champ > *Random Between Two Constants*) ;
   - *Start Color* orange, *Gravity Modifier* `0.5` ;
   - *Simulation Space* `World`, *Stop Action* `Destroy`.
3. *Emission* :
   - *Rate over Time* `0` ;
   - dans *Bursts*, clique sur `+` : *Time* 0, *Count* 60.
4. *Shape* : `Sphere`, *Radius* `0.3`.
5. Coche *Color over Lifetime* et *Size over Lifetime* :
   - la couleur passe d'orange à rouge foncé, avec une opacité qui tombe à 0 ;
   - la taille descend vers 0.
6. Glisse `Explosion` dans `Prefabs`, supprime-le de la scène, puis mets le prefab dans *Explosion Effect* de `Special Arrows`.

## 7. Tester

1. Lance Play, tire dans le gong, puis appuie sur **N** pour finir la vague tout de suite. La boutique s'ouvre à ta gauche.
2. Appuie sur **M** pour recevoir 500 pièces d'or (raccourci de test, dans l'éditeur).
3. Vise une carte avec la main libre et appuie sur la gâchette :
   - un son et un message confirment l'achat ;
   - l'or baisse, sur le panneau comme sur la montre ;
   - la carte affiche « Acheté ».
4. *Relancer* change les 4 améliorations ; son prix monte de 5 à chaque relance.
5. Achète l'*Arc composite* : ses flèches vont plus vite et son anneau aussi. Il change d'apparence dans ta main si tu as fait la section 5.
6. Laisse les chevaliers abîmer la tour pendant une vague, puis répare-la.

**Pour tester les légendaires** :
- achète quelques *Chance* : elles augmentent leurs chances ;
- l'or de **M** paie les relances.

**Pour tester les prix** : achète une carte, les prix des autres cartes montent de 5 %. Enchaîne **N** : à chaque pause, les prix sont 10 % plus élevés.

Les effets à vérifier :
- *Multitir* : un tir sur deux, une deuxième flèche part 20 cm à côté de la première, parallèle à elle. Avec 2 exemplaires, à chaque tir ; à partir de 4 exemplaires (3 flèches), l'éventail s'ouvre de chaque côté ;
- *Déluge* : environ 0,2 s après le départ, une flèche sur deux se dédouble en vol et continue tout droit ;
- *Tir écho* : un quart de seconde après le tir, une seconde volée part du même endroit, avec le même éventail ;
- *Les flèches en plus* : avec *Multitir* et *Flèche explosive*, les flèches de l'éventail peuvent exploser elles aussi (traînée orange) ;
- *Au-delà de 100 %* : avec 5 *Flèches de foudre*, chaque flèche appelle un éclair ; à partir de la 6e, les éclairs font plus de dégâts ;
- *Flèche de foudre* : traînée bleue, éclair qui tombe du ciel, ennemi ralenti ;
- *Chaîne d'éclairs* : l'éclair saute d'un ennemi à l'autre ;
- *Flèche explosive* : traînée orange, gerbe d'éclairs, « boum », dégâts autour ;
- *Flèche de glace* : traînée bleu pâle, disque de glace au sol ; les chevaliers qui marchent dessus avancent deux fois moins vite ;
- *Auto-visée* : une flèche qui passe un peu à côté d'un chevalier se courbe vers lui ;
- *Tir ricochet* : après un chevalier, la flèche repart vers un autre, avec un « ptiing » ;
- *Vampirisme* : « +2 PV » en vert sur les tirs à la tête ;
- *Vitalité* : les PV max de la montre augmentent ;
- *Chance* : plus de cartes bleues et dorées aux pauses suivantes ;
- *Butin* : plus d'or pour le même score.

## En cas de problème

- **Le rayon ne clique pas** :
  - l'EventSystem doit avoir le *XR UI Input Module* (section 3) ;
  - le Canvas doit avoir le *Tracked Device Graphic Raycaster* ;
  - la main qui vise doit être vide.
- **Le panneau n'apparaît jamais** : il faut un `Shop Manager` sur `Game` avec son catalogue, et *Content* doit être branché. La boutique ne s'ouvre qu'à la fin d'une vague.
- **« Il manque un Player Upgrades dans la scène »** : ajoute `Player Upgrades` sur `Game`.
- **Les cartes affichent « Rupture de stock »** : la liste *Upgrades* du catalogue est vide (menu ⋮ > *Remplir avec le GDD*).
- **Le prochain arc ne s'affiche pas** : la liste *Bows* du catalogue est vide, ou l'arc de la scène n'utilise pas un des assets de la liste.
- **L'arc ne change pas d'apparence** : son asset n'a pas de *Visual Prefab* (section 5).
- **Les éclairs ou la glace sont roses ou invisibles** : *Effect Material* doit utiliser le shader *Archery/UnlitVertexColor*.
- **La glace n'apparaît pas quand la flèche se plante loin des ennemis** : elle se pose sur le NavMesh, là où marchent les ennemis. Si la flèche se plante à plus de 3 m du NavMesh, il n'y a pas de glace.
- **L'auto-visée est trop forte ou trop faible** : change la *Value* d'*Auto-visée* dans le catalogue (degrés par seconde), ou *Homing Range* et *Homing Angle* dans `Special Arrows`.
- **Le jeu saccade avec beaucoup de flèches** : baisse *Max Arrows In Flight* dans `Special Arrows` (150 par défaut).
- **Une amélioration a encore un niveau maximal** : son *Max Stacks* n'est pas à `0` dans le catalogue.
