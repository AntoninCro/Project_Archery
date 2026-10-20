# Guide : l'arc légendaire

Refait le 20 octobre avec les classes d'arc. **Il n'y a plus rien à monter** : tout est déjà dans le préfab du menu des arcs (`Prefabs/UI/Bow Classes`) et dans les données. Ce guide dit comment il marche, où le régler et comment le tester.

## Comment on l'obtient

- Tant qu'il n'est pas débloqué, un coffre a **10 % de chance** de proposer un **fragment d'arc légendaire** (une orbe orange, « 1 / 3 »), à la place de sa deuxième amélioration permanente. Jamais avec l'arc cosmique en main.
- Les fragments se **gardent d'une partie à l'autre** : c'est la progression du succès « Trois fragments », affichée sur la carte de l'arc dans le menu des arcs (« Fragment 1 / 3 »).
- Au **troisième fragment**, l'arc est débloqué pour toujours et arrive tout de suite en main (fanfare, vibrations, message).

## Son pouvoir : la pluie de flèches

- Au sommet de sa course, la flèche se divise : **10 flèches** retombent en pluie dans un cercle de **3,5 m** autour de son point d'arrivée. Chacune fait la moitié des dégâts du tir (qualité de l'anneau comprise). La flèche tirée tombe au centre.
- À l'arrivée, une **zone brûlante** reste au sol **3 s** : chaque ennemi au sol dedans perd 30 % des dégâts du tir par seconde (pas de points de touche ni de combo, mais l'élimination compte).
- Tir tendu (la flèche se plante avant son sommet) : la pluie tombe du ciel sur ce point.
- L'aide à la visée montre la courbe et le **cercle de la pluie**, même en Difficile (rien en Impossible).

| Arc légendaire | Valeur |
|---|---|
| Vitesse des flèches | 42 m/s |
| Dégâts | 15 |
| Gravité des flèches | ×2 (*Arrow Gravity*) : elles tombent plus vite et vont deux fois moins loin |
| Anneau | 1,2 s, bandes un peu larges |
| Pouvoir | PLUIE DE FLÈCHES |

## Où le régler

| Quoi | Où | Champs |
|---|---|---|
| L'arc, la pluie, la zone | `Data/BowClasses/Bow_Legendaire` | *Arrow Speed*, *Damage*, *Arrow Gravity*… ; *Rain Arrows*, *Rain Radius*, *Rain Damage*, *Rain Zone Duration*, *Rain Zone Damage*, *Rain Color*, *Rain Split Clip*, *Rain Zone Clip* |
| La chance d'un fragment, les sons | `Prefabs/UI/Bow Classes`, composant `Legendary Bow` | *Part Chance* (0,1), *Color*, *Part Clip*, *Assembled Clip* |
| Le succès | `Data/Achievements/Succes_Legendaire` | *Title*, *Description*, *Target* (3), *Progress Label* (« Fragment ») |
| Son modèle | `Prefabs/Bow Visual Legendaire`, assemblé par l'outil `LegendaryBowBaker` (menu *Archery > Arc légendaire > Refaire le modèle*) | *Bowblade* de Tijmen Matthys (CC-BY 4.0), dans `Art/Bows/Legendaire` ; matériau `Art/Bows/Arc Legendaire` (*Emission Color* : la lueur) |

## Tester

1. Lance Play. Avant la première vague : **U** débloque tous les succès, puis **0** équipe l'arc légendaire.
2. Tire en cloche : la flèche se divise au sommet, la pluie tombe dans le cercle montré par l'aide à la visée, et la zone brûle au sol.
3. Pour tester les fragments : **J** efface les succès, puis **L** donne un fragment (« Fragment d'arc légendaire : 1 / 3 ») ; au troisième, l'arc arrive en main. Ou **C** pose un coffre (mets provisoirement *Part Chance* à 1 pour qu'il propose un fragment).

## En cas de problème

- **Aucun fragment dans les coffres** : la console dit si aucun arc de la liste n'est débloqué par un succès « Legendary Fragment ».
- **La pluie ne tombe pas** : *Arrow Rain* doit être coché dans `Bow_Legendaire`. Une flèche perdue en l'air (qui ne touche rien) ne fait pas de pluie.
