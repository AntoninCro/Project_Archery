# Audit de performance : THUNK VR sur Quest 3 en autonome (25 octobre 2026)

Aujourd'hui, le jeu tourne sur PC et s'affiche dans le casque par Quest Link. Cet audit regarde ce qu'il faudrait changer pour
un build Android qui tourne **dans le casque seul** (72 images par seconde au moins, soit 13,9 ms par image, pour les deux yeux).

**Limite de l'audit** : il a été fait en lisant le projet (réglages, scènes, assets, code), sans build Android ni mesure dans le
casque. Les gains annoncés sont des ordres de grandeur ; seule une mesure (partie 7) dira où le jeu perd vraiment du temps.

## 1. En bref

- **Le code est sain** : pas de LINQ, des requêtes physiques sans allocation, des textes de la montre refaits seulement quand ils
  changent, des flèches réutilisées (pool), le ramasse-miettes incrémental. Deux petites allocations par image ont été
  corrigées (partie 5).
- **Le rendu est réglé pour un PC** : sur Android, le jeu prend aujourd'hui le niveau de qualité « Ultra », donc les réglages
  du PC (Forward+, HDR, occlusion ambiante, ombres douces sur 50 m, post-traitement). Tel quel, il ne tiendrait très
  probablement pas 72 images par seconde dans le casque. **C'est là que se trouve presque tout le gain.**
- **La bonne nouvelle** : le projet contient déjà une configuration mobile (« Performance URP Config » et son renderer
  « Android Preset » : rendu Forward, sans post-traitement, *native render pass*), inutilisée sur Android. Un niveau de
  qualité « Quest » qui s'en sert changerait le casque sans toucher au PC.

## 2. Le rendu (priorité haute)

Ce que le Quest reçoit aujourd'hui (niveau « Ultra » → `Quality URP Config` → renderer `Standalone Preset`), et ce qui est
conseillé pour lui :

| Réglage | Aujourd'hui | Conseillé pour le Quest | Ce que ça change à l'image |
|---|---|---|---|
| Occlusion ambiante (SSAO, effet du renderer) | Active, depuis la profondeur, intensité 1,5 | **Coupée** | Coins et contacts un peu moins sombres |
| Chemin de rendu | Forward+ | **Forward** (celui de l'`Android Preset`) | Rien, tant qu'un objet reçoit peu de lumières |
| HDR | Oui | **Non** | Le bloom des néons s'appuie dessus : à vérifier à l'œil |
| Post-traitement (bloom, couleurs, vignette) | Oui, sur toutes les scènes | **À trancher** : sans, ou un bloom seul et léger | Le look néon du jeu en dépend : c'est le vrai choix artistique |
| Texture de profondeur | Demandée (l'eau et les rayons de lumière s'en servent) | **Non** | Les bords de l'eau et des rayons deviennent nets au lieu de fondus |
| Ombres du soleil | Douces, 2048, 2 cascades, 50 m | **Dures, 1024, 1 cascade, 20 m** (ou aucune) | Des ombres moins loin et plus dures |
| Lumières en plus | Par pixel, 4 par objet | **Par sommet, ou 2 par objet** | Les halos des torches et des néons moins précis |
| Anticrénelage (MSAA) | ×2 | **×4** (presque gratuit sur la puce du Quest) | Image plus propre |
| Rendu fovéal | Non | **Oui** (fonction Meta de l'OpenXR, réglage *Foveated Rendering*) | Bords de l'image un peu moins nets, peu visible |

Pourquoi tout cela compte autant sur le Quest : sa puce dessine l'image par petits carreaux en mémoire rapide. Chaque passe
plein écran (occlusion ambiante, copie de la profondeur, post-traitement, résolution du HDR) l'oblige à tout recopier en
mémoire lente, deux fois (un œil chacun). Les ombres, elles, redessinent la carte une deuxième fois (ses 2 500 objets).

**Fait le 25 octobre (codé, pas encore lancé dans Unity) : le niveau de qualité « Quest »**, par `Editor/QuestQualityBaker.cs`,
une fois, à la prochaine ouverture de Unity (console : « QuestQualityBaker : … ») :

- `Quest Renderer` (dans `Assets/Settings/Project Configuration`) : copie du renderer mobile `Android Preset` (Forward, sans
  occlusion ambiante, native render pass), **avec le post-traitement** (le look néon) ;
- `Quest URP Config` : HDR en 32 bits (il pèse autant qu'une image sans HDR et garde le bloom), MSAA ×4, sans texture de
  profondeur, ombres du soleil dures (1024, une cascade, 20 m), 2 lumières en plus par objet, par pixel ;
- le niveau « Quest » (copie d'« Ultra » qui s'en sert), **par défaut sur Android** ; le PC garde « Ultra » ;
- le **rendu fovéal** de l'OpenXR pour Android (API de l'URP), réglé à mi-force au lancement par `Core/QuestFoveation.cs`
  (rien sur PC).

**Voir l'image du Quest sans build** : dans *Project Settings > Quality*, cliquer sur la ligne « Quest » (le niveau de
l'éditeur), puis jouer avec Quest Link : c'est le look du Quest, pas sa vitesse (le PC dessine toujours). Revenir sur
« Ultra » ensuite. **Le jeu encore trop lent dans le casque** : décocher *Post-processing* dans `Quest Renderer` et le HDR
dans `Quest URP Config` (le plus gros gain restant, mais les néons ne bavent plus). Le menu *Archery > Qualité Quest >
Refaire les réglages* remet les valeurs du baker.

## 3. Les scènes (priorité moyenne)

- **La carte** (`Prototype_Tir`) : environ 2 500 objets posés, presque tous statiques (bien : ils sont regroupés au build),
  80 systèmes de particules, 28 lumières ponctuelles enregistrées dans la scène, plus celles que le code ajoute au lancement
  (une torche sur deux, les cristaux des parois, la tour, le joueur…), toutes sans ombre. Les particules transparentes (feu,
  brume des cascades, lucioles) sont le premier suspect côté carte : chaque couche transparente redessine les mêmes pixels.
  **L'occlusion culling** a été calculée le 13 octobre : à refaire, la carte a changé depuis.
- **Les ciels** (`StylizedSky`, et `CyberSky` sur le toit) sont calculés pixel par pixel (des boucles d'étoiles, de nuages,
  d'aurores) et couvrent souvent la moitié de l'image. Sur le Quest, les « cuire » en cubemaps (une image par ciel) serait un
  gros gain, sans différence visible sauf l'animation.
- **Le toit cyberpunk** : 1 355 objets (presque tous statiques), 34 lumières ponctuelles, et une sonde de reflets rendue
  **en temps réel** une fois à l'entrée (6 rendus de toute la scène d'un coup : un à-coup possible au casque, désagréable en
  VR). Pour le Quest : moins de lumières (les néons brillent déjà par leur matériau et le bloom), et une sonde précalculée.
- **L'arène du dragon** : 15 lumières dans la scène, plus celles du code (piliers, cristaux du plafond, boules de feu, souffle
  du dragon). Toutes sans ombre ; à mesurer pendant le combat, quand tout s'allume.

## 4. Le code (priorité basse)

Rien d'urgent. À surveiller au profileur une fois dans le casque, dont le processeur est bien plus lent que celui d'un PC :

- **Jusqu'à 40 ennemis en vie** (plafond des vagues), tous animés en permanence, même hors de vue (`AlwaysAnimate`, choisi
  parce que les bornes de certains modèles sont fausses : le mage agrandi ×100). Corriger les bornes des modèles concernés
  permettrait de ne plus animer les ennemis invisibles.
- **Physique** : `Auto Sync Transforms` est activé (chaque déplacement d'objet resynchronise la physique avant la requête
  suivante). Le couper est un gain connu, mais il faut alors vérifier les endroits qui déplacent un objet puis lancent un
  rayon dans la même image : à ne faire qu'après une mesure.
- **Créations et destructions** : les projectiles ennemis, les carreaux de baliste, les âmes et les effets sont créés puis
  détruits. Si le profileur montre des à-coups du ramasse-miettes, les réutiliser comme les flèches.
- `FrameWatchdog` écrit déjà les images lentes dans le journal : sur le Quest, il se lit avec `adb logcat`. Il ne note que
  les gels (plus de 80 ms) : pour un jeu qui rame, voir la partie 8 (`PerfProbe`).

## 5. Corrigé aujourd'hui (codé, pas encore vérifié dans Unity)

- `World/Lighting/MapTorches.cs` : la vague rouge des torches retirait ses impulsions finies avec une lambda qui capturait
  l'heure, allouée à chaque image ; c'est une boucle.
- `UI/WatchTowerButton.cs` : pendant la recharge du bouton « Tour » de la montre, son texte était reconstruit à chaque image
  pour être comparé ; il ne l'est plus qu'une fois par seconde.

## 6. Les assets (priorité basse)

- **Textures** : plafonnées à 2048, compressées en ASTC sur Android par défaut. Les modèles PBR (le dragon, les Mini Legion)
  ont 5 textures de 2048 chacun : un plafond à 1024 pour Android suffirait dans le casque et allégerait la mémoire.
- **Sons** : presque tous « décompressés au chargement », très bien pour des sons courts. La musique du boss final
  (`music_final_boss.mp3`, 2,6 Mo) le serait aussi : décompressée, elle prendrait quelques dizaines de Mo de mémoire ; la
  passer en « streaming ».
- **Resources** : 65 Mo, toujours embarqués dans le build (dont 49 Mo de vidéos du tutoriel). Acceptable sur le Quest.

## 7. Plan conseillé

1. **Créer le niveau de qualité « Quest »** (partie 2). C'est le plus gros gain, et le PC ne change pas. *Codé le
   25 octobre : à l'ouverture de Unity, vérifier la console, puis l'image du Quest par Quest Link.*
2. **Faire un build Android de développement** (*premier build testé le 25 octobre au soir : partie 8*), puis mesurer
   dans le casque avec *OVR Metrics Tool* (images par seconde,
   charge du GPU et du processeur) et le profileur de Unity branché au casque, à trois endroits : la carte à la vague 5 avec
   beaucoup d'ennemis, l'arène du dragon, le toit cyberpunk.
3. **Selon les mesures** : les ciels en cubemaps, la sonde du toit précalculée, moins de lumières, les animations des
   ennemis invisibles, puis la décision sur le post-traitement (la plus visible : à juger au casque).

## 8. Le premier test au casque seul, et comment mesurer (25 octobre au soir)

**Le test** : un build Android de développement (`builds/test_1_thunk.apk`, à côté du dépôt), une partie jusqu'à la
vague 10 avec beaucoup d'améliorations et les Akimbow. Ça rame un peu à la fin, avec des particules partout. Le jeu ne
plante pas : il s'est fermé normalement. Son journal était déjà perdu : le Quest ne garde que 256 Ko de journal, que le
système recouvre en quelques minutes. Et `FrameWatchdog` n'aurait rien noté : il ne voit que les gels (plus de 80 ms).

**Les suspects, dans le code** (à confirmer par la mesure) :
- les explosions (l'amélioration « explosion », les fusions du katana) : un effet complet créé puis détruit à chaque fois,
  sans réserve (`Upgrades/SpecialArrows.cs`, `Explode` et `Blast`). **Corrigé le 26 octobre** (avec la pointe à mèche,
  qui en fait beaucoup) : 6 effets réutilisés au plus, le plus ancien repart au-delà (`PlayExplosionEffect`) ;
- les effets d'impact (`Effects/HitEffects.cs`) sont réutilisés, mais rien ne limite le nombre d'effets visibles en même
  temps ;
- la foudre trace un éclair à chaque touche (`LightningBolt`).

Avec les Akimbow (deux arbalètes qui tirent vite), le multitir et l'écho, tout se multiplie.

**Mesurer** :
1. **Le journal du casque à 16 Mo**, casque branché : `adb logcat -G 16M` (fait le 25 octobre ; à refaire après chaque
   redémarrage du casque). `adb` est fourni avec Unity : `C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`.
2. **Un build de développement** (*Development Build* coché). `Core/PerfProbe.cs` y écrit toutes les 10 s une ligne
   « Mesure : … » :
   - les images par seconde, la part d'images lentes (plus de 1,25 fois la durée d'une image du casque), la pire image et
     le temps du thread principal ;
   - les particules (et les 3 effets qui en ont le plus), les lumières, les traînées et les éclairs ;
   - les ennemis, les flèches en vol et plantées, la mémoire.

   Au lancement, une ligne donne le casque et le niveau de qualité utilisé.
3. **Jouer sans câble**, puis rebrancher le casque et récupérer :
   - le journal : `adb logcat -d -v time -s Unity VrApi > journal.txt`. Les lignes `VrApi` du système, une par seconde,
     donnent les images par seconde vues par le casque et la charge du processeur et de la carte graphique ;
   - les mesures : `adb pull /sdcard/Android/data/com.DefaultCompany.VRTemplate/files/Mesures` (un fichier CSV par
     lancement, les 10 derniers ; il s'ouvre dans Excel).
