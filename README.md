# VR Archery Challenge

Jeu de tir à l'arc en réalité virtuelle pour Meta Quest 3, réalisé avec Unity pour le cours ST2OOS (UnityXR).

On défend une tour au milieu d'une clairière contre des vagues de monstres de plus en plus fortes. Le jeu mélange trois genres :
- **survivor** : tenir le plus longtemps possible ;
- **roguelike** : des améliorations tirées au sort entre les vagues, qui se cumulent sans limite ;
- **tower defense** : la tour, ses barricades et son brasero.

Inspirations : *Megabonk*, et *Longbow* dans *The Lab*.

## Fonctionnalités

| Consigne du projet | Dans le jeu |
|---|---|
| Interactions XR, geste naturel | Prendre l'arc, attraper une flèche dans son dos, l'encocher, tendre la corde, lâcher. Soulever le couvercle d'un coffre, attraper une orbe, tremper une flèche dans le feu, frapper au corps à corps. |
| Trajectoire physique | Flèches soumises à la gravité, portée selon la tension et la qualité du tir. |
| Plusieurs arcs et flèches | 4 arcs à acheter ; flèches spéciales (foudre, glace, explosion, multitir, déluge, ricochet…) ; flèches enflammées. |
| Améliorations entre les niveaux | Boutique entre les vagues : 19 améliorations de trois raretés, sans limite d'achat. |
| Zones de score, cibles mobiles, score en direct | Tête et corps des ennemis (et points faibles du boss) ; cibles d'entraînement, dont une mobile ; score, combo et argent sur la montre. |
| Niveaux de difficulté | Facile, Normal, Difficile, Impossible, chacun avec son ciel (jour, coucher de soleil, nuit, lune de sang). |
| Mode entraînement | Cibles et mannequins au menu et pendant les pauses. |
| Menu, paramètres, affichage | Menu dans le décor, volumes, rotation au joystick, vignette de confort ; montre au poignet. |
| Clavier virtuel, sauvegarde, classement | Nom saisi au clavier virtuel d'XRI ; classement et paramètres en JSON. |
| Sons 3D | Sons spatialisés (arc, impacts, ennemis), ambiance jour et nuit, musiques. |

Aussi : un timing à l'anneau (rouge, orange, vert) qui récompense le tir parfait, des ennemis volants et tireurs, un boss toutes les 5 vagues, des coffres dans la forêt, la course en balançant les bras et le slide, un mode infini.

## Commandes

| Action | Geste |
|---|---|
| Prendre l'arc | Bouton de poignée, n'importe quelle main |
| Prendre une flèche | Main derrière l'épaule, poignée |
| Tirer | Approcher la flèche de la corde, reculer la main, lâcher la poignée. Lâcher dans le vert de l'anneau donne un tir parfait. |
| Marcher | Joystick gauche (la direction suit le regard) |
| Courir | Balancer les deux bras en marchant |
| Glisser | A ou X en courant |
| Tourner | Joystick droit (par crans, désactivable) |
| Menus et boutique | Viser avec la main libre, gâchette |
| Lancer une vague | Tirer dans le gong |
| Ouvrir un coffre | Attraper le couvercle et le soulever, puis attraper une orbe |
| Flèche enflammée | Tremper la pointe dans le brasero, en haut de la tour |
| Coup au corps à corps | Frapper un ennemi avec la flèche tenue en main |

**Raccourcis de test** (dans l'éditeur) : **N** lance ou termine une vague, **K** fait mourir le joueur, **M** donne 500 pièces d'or, **C** pose un coffre devant soi, **1 à 4** choisissent la difficulté avant la première vague.

## Installation

1. Installer **Unity 6000.5.10f1** avec Unity Hub, et le module *Windows Build Support*.
2. Ouvrir le dossier `Project Archery` avec Unity Hub. Les paquets (XR Interaction Toolkit 3.5.1, OpenXR, URP 17.5, AI Navigation, Animation Rigging) s'installent tout seuls.
3. Ouvrir la scène `Assets/_Project/Scenes/Prototype_Tir.unity`.

**Avec le casque** (Quest 3 ou 3S) :
1. Installer l'application **Meta Quest Link** sur le PC et brancher le casque (câble Link ou Air Link).
2. Dans l'application Meta Quest Link : *Paramètres > Général* > définir Meta Quest Link comme **runtime OpenXR actif**.
3. Dans le casque, activer Quest Link, puis lancer *Play* dans Unity.

**Construire le jeu** : *File > Build Profiles* > *Windows*, vérifier que la scène est dans la liste, puis *Build*.

Sans casque, le *XR Interaction Simulator* (exemple d'XRI) permet de tester au clavier et à la souris.

## Organisation du projet

```
Project Archery/
├── Assets/_Project/
│   ├── Scripts/     code du jeu (arc, ennemis, vagues, boutique, coffres, déplacements…), un dossier par thème
│   ├── Data/        réglages en ScriptableObjects : arcs, ennemis, vagues, difficultés, boutique
│   ├── Prefabs/, Scenes/, Materials/, Shaders/, Art/, Audio/
├── Docs/
│   ├── GDD.md       document de conception : règles, chiffres, planning
│   └── Guide_*.md   pas-à-pas de montage de chaque partie dans Unity
└── Tools/           vérifications sans ouvrir Unity : compilation des scripts, syntaxe des shaders
```

Les fichiers de sauvegarde (`leaderboard.json`, `settings.json`) sont dans `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Project Archery`.

## Crédits

- Arcs et flèches : pack *Easy Weapons*.
- Chevaliers : *Toon RTS Units – Demo*.
- Coffre animé : *Animated PBR Chest Demo*.
- Volant (Beholder) : *RPG Monster Partners PBR Polyart* ; tireur (mage) : *Wizard PolyArt*.
- Décor : *à compléter avec les packs de la carte finale* (Kenney, Quaternius…).
- Sons, ambiances et musiques : provisoires, générés par script pour le prototype.
- Développement assisté par IA (Claude, d'Anthropic) pour le code et la documentation ; les détails sont dans le rapport.

## Équipe

*À compléter.*
