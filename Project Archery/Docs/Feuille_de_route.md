# Feuille de route : les guides à monter

Tout le code des étapes ci-dessous est écrit et compile. Il reste à les monter dans Unity, dans cet ordre. Chaque guide se teste seul ; commite après chacun.

| # | Guide | Ce qu'il ajoute | Dépend de | Durée estimée |
|---|---|---|---|---|
| 1 | `Guide_Coffres.md` | Coffres animés (modèle du pack), ralenti, orbes, bonus temporaires | — | 2 h |
| 2 | `Guide_Volants_Tireurs.md` | Volant (Beholder) et tireur (mage), projectiles | — | 2 h |
| 3 | `Guide_Barricades_Brasero.md` | Barricades, brasero, flèches enflammées, 1 carte de boutique | — | 1 h 30 |
| 4 | `Guide_CorpsACorps.md` | Coup de flèche au corps à corps | — (rien à monter) | 10 min |
| 5 | `Guide_Effets.md` | Particules d'impact et de mort | — | 45 min |
| 6 | `Guide_Ambiance.md` | Ambiance jour et nuit, musiques | — | 20 min |
| 7 | `Guide_Grenade.md` | Grenade de flèches en bas du dos | — | 45 min |
| 8 | `Guide_ArcLegendaire.md` | Arc légendaire en 3 morceaux | coffres (1) | 30 min |
| 9 | `Guide_Classes.md` | Arcs choisis au menu, expérience entre les parties | — (optionnel) | 45 min |
| 10 | `Guide_Carte.md` | Carte cubique à trois voies (blocs sur une grille, jungle en terrasses, rampes, rivière qui ralentit, montagnes, antre fermé), NavMesh, emplacements | barricades (3), coffres (1) | deux à trois journées |
| 11 | (à écrire) matériaux de défense | Deuxième monnaie ramassée sur la carte, améliorations de défense en boutique (GDD, section 9) | carte (10) | à estimer |
| 12 | `Checklist_Demo.md` | Tests complets, build, démo | tout | une demi-journée |

**Fait le 6 octobre** : les coffres (1) et le volant (partie de 2), par toi ; le tireur (partie de 2), les barricades et le brasero (3), et le corps à corps (4, rien à monter), par Claude. Il reste à tester 2 à 4, puis les guides 5 à 11.

**Fait le 7 octobre** : les effets (5), par toi, avec la couleur et la taille des particules réglées par Claude ; le téléporteur en bande autour de la tour, le bouton « Retour à la tour » de la boutique et la suppression de la flèche de téléportation du joystick droit, par Claude (`Guide_Teleporteur.md`).

**Pourquoi cet ordre** : les ennemis et les défenses d'abord, parce qu'ils changent le plus le jeu et méritent d'être testés longtemps. Les étapes courtes (4 à 9) peuvent se glisser n'importe quand. La carte vient à la fin, quand on sait où placer les barricades, les coffres et les points d'apparition. L'équilibrage se fait en dernier, sur la carte finale.

**Si le temps manque** : la carte (10) et les tests (12) passent avant les idées de fin de projet (7, 8, 9), qui sont des bonus.

**Gardé pour la fin, si le temps le permet** (GDD, sections 12, 17 bis, 21 et 23), dans l'ordre conseillé : le boss final dans son antre, la lumière et l'éclairage, les finitions (menus, icônes, arcs), le tutoriel, le saut aux bras, les totems, l'arc légendaire gardé par un boss, puis les cinématiques.
