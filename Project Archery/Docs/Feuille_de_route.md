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
| 7 | — | Grenade de flèches en bas du dos : confiée à un coéquipier le 12 octobre, sur sa branche (GDD, section 4.6) | — | — |
| 8 | `Guide_ArcLegendaire.md` | Arc légendaire en 3 fragments, pluie de flèches (rien à monter depuis le 20 octobre) | coffres (1) | 10 min |
| 9 | — | Arcs choisis au menu, débloqués par des succès : confiés à un coéquipier le 12 octobre, sur sa branche (GDD, section 23) | — | — |
| 10 | `Guide_Carte.md` | Carte cubique à trois voies (blocs sur une grille, jungle en terrasses, rampes, rivière qui ralentit, montagnes, antre fermé), NavMesh, emplacements | barricades (3), coffres (1) | deux à trois journées |
| 11 | `Guide_TowerDefense.md` | Tower defense (GDD, section 9) : cristaux et matériaux (étape 1, montée le 11 octobre), socles, panneau, plan bleu et marteau (étape 2, montée par Claude le 11 octobre), les quatre bâtiments et leurs niveaux (étape 3) et l'équilibrage (étape 4), montés par Claude le 12 octobre ; testé | carte (10) | fait |
| 12 | `Guide_BossFinal.md` | Boss final, le dragon rouge, à la vague 6, dans son antre (GDD, section 10 bis) : refait et monté par Claude le 13 octobre ; reste à tester | carte (10) | tests : 1 h |
| 13 | `Checklist_Demo.md` | Tests complets, build, démo | tout | une demi-journée |

**Fait le 6 octobre** : les coffres (1) et le volant (partie de 2), par toi ; le tireur (partie de 2), les barricades et le brasero (3), et le corps à corps (4, rien à monter), par Claude. Il reste à tester 2 à 4, puis les guides 5 à 11.

**Fait le 7 octobre** : les effets (5), par toi, avec la couleur et la taille des particules réglées par Claude ; le téléporteur en bande autour de la tour, le bouton « Retour à la tour » de la boutique et la suppression de la flèche de téléportation du joystick droit, par Claude (`Guide_Teleporteur.md`).

**Point du 13 octobre** : tout ce qui est monté a été testé dans le casque (guides 1 à 6, la carte 10 et le tower defense 11). L'ancien PC est tombé en panne : le projet a été récupéré depuis GitHub (dernier commit du 12 octobre). La partie passe à **6 vagues** : le boss final de la vague 6 (12), perdu avec l'ancien PC, a été refait et monté par Claude le soir même ; il reste à le tester. Restent aussi l'arc légendaire (8), le travail des coéquipiers (7 et 9) et les tests de la démo (13).

**Pourquoi cet ordre** : les ennemis et les défenses d'abord, parce qu'ils changent le plus le jeu et méritent d'être testés longtemps. Les étapes courtes (4 à 9) peuvent se glisser n'importe quand. La carte vient à la fin, quand on sait où placer les barricades, les coffres et les points d'apparition. L'équilibrage se fait en dernier, sur la carte finale.

**Si le temps manque** : le boss final (12) et les tests (13) passent avant les idées de fin de projet (7, 8, 9), qui sont des bonus.

**Gardé pour la fin, si le temps le permet** (GDD, sections 12, 17 bis, 21 et 23), dans l'ordre conseillé : la lumière et l'éclairage, les finitions (menus, icônes, arcs), le tutoriel, le saut aux bras, les totems, l'arc légendaire gardé par un boss, l'easter egg du portail des étoiles (après les classes d'arcs au début de la partie), puis les cinématiques.
