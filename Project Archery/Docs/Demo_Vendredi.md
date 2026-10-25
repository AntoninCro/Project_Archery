# Préparer la démo du vendredi 23 octobre

Le déroulé est prévu pour **10 minutes**, avec une version courte de 5 minutes. Adapte les durées au format réel (temps de passage, jury, public).

Complète `Checklist_Demo.md` : la checklist dit **quoi tester**, ce document dit **comment présenter**.

## 1. D'ici vendredi

| Jour | À faire |
|---|---|
| **Dim. 18** | Ouvrir Unity, **Ctrl+S** sur `Prototype_Tir` (le portail replacé). |
| **Lun. 19** | Une partie complète en casque (checklist, partie 1 : d'abord les nouveautés, le portail et la dimension). Noter chaque bug avec la vague et ce que tu faisais. |
| **Mar. 20** | Corrections. ~~Enregistrer les 13 vidéos du tutoriel~~ (faites le 19 octobre). |
| **Mer. 21** | **Gel des nouveautés le soir** : après, seulement des corrections. Faire le build Windows, en *Development Build* (les raccourcis de démo y marchent, voir partie 4), et y jouer une partie entière. |
| **Jeu. 22** | Deux répétitions chronométrées, dont une devant quelqu'un qui ne connaît pas le jeu. Enregistrer une **vidéo de secours** (2 à 3 minutes de partie). Préparer et charger le matériel (partie 8). Commit et push. |
| **Ven. 23** | Arriver tôt : installation **30 minutes avant** (partie 9). |

## 2. Le pitch (30 secondes, avant de mettre le casque)

> « THUNK VR, c'est un jeu de tir à l'arc en réalité virtuelle qui mélange survie, roguelike et tower defense. On défend une tour contre six vagues d'ennemis. Entre les vagues, on achète des améliorations. Pendant les vagues, on récolte des cristaux pour construire des balistes et des pièges, au marteau, avec ses mains. Chaque vague se termine par un boss, et la dernière par MK, un dragon, dans son antre. On l'a fait tester à la Nuit des assos et on a rééquilibré le jeu avec leurs retours. »

Garde ensuite **trois idées** en tête, à répéter si le jury décroche :
1. **Le tir à l'arc est physique** : on prend la flèche dans son dos, on tend, on vise, avec un anneau de timing pour le tir parfait.
2. **On bouge avec son corps** : courir en balançant les bras, glisser, sauter en levant les bras.
3. **Trois jeux en un** : la survie, le roguelike (boutique, coffres) et le tower defense (construire ses défenses).

## 3. Le déroulé (10 minutes)

L'idéal, c'est **deux personnes** : toi dans le casque, et un **copilote au clavier** qui tape les raccourcis et surveille l'écran (dans le casque, tu ne vois pas le clavier). Seul, garde le clavier tout près et préviens avant de retirer le casque.

| Temps | Tu montres | Tu dis | Raccourci |
|---|---|---|---|
| 0:00 | Le pitch, casque en main ; l'écran affiche le menu | Le pitch (partie 2) | |
| 0:45 | Mettre le casque. Le menu : choisir la difficulté sur les panneaux (le ciel change), ouvrir le Tutoriel une seconde | « 4 difficultés ; même les menus se visent à l'arc » | 1 à 4 au besoin |
| 1:45 | Les cibles d'entraînement : prendre une flèche, tendre, l'anneau, **un tir parfait** | « L'anneau passe du rouge au vert : un tir dans le vert part plus vite et plus fort. L'aide à la visée dépend de la difficulté. » | |
| 3:00 | Le gong : vague 1. Pendant l'accalmie, aller récolter un gisement, puis construire une baliste (deux marteaux, 3 coups) | « Pendant le calme, on sort chercher des cristaux ; la construction se fait vraiment au marteau. » | B (matériaux), V (gisement) |
| 4:30 | La montée de la vague : tirer, une flèche enflammée au brasero, courir aux bras, glisser | « On se déplace avec les bras, sans joystick : moins de mal des transports. » | |
| 5:30 | Le boss de la vague (Thomas) : viser sa gemme, son point faible | « Chaque boss a un point faible qui bat comme un cœur. » | N (faire venir le boss, puis le finir si ça traîne) |
| 6:15 | La boutique : acheter une amélioration. Un coffre : ouverture au ralenti, 3 orbes | « Chaque partie est différente selon ses achats. » | M (or), C (coffre) |
| 7:00 | **L'easter egg** : les runes, le portail, la dimension (panneaux d'erreur, monstre corrompu), 20 s, puis sortir | « Un secret caché sur la carte. Le monde bugue exprès : c'est le début d'une histoire qu'on veut développer. » | P (runes), P (entrer) |
| 8:00 | **MK** : entrer dans l'arène, l'intro (les cristaux, la lave), un peu de combat | « Le boss final : l'arène est construite par le code, et le dragon a trois phases. » | N, N… (voir la partie 4) |
| 9:15 | Mourir : saisir son nom, le classement | « Score, classement, et une étoile pour ceux qui réussissent l'épreuve du portail. » | K |
| 9:45 | Retirer le casque, conclure (partie 7, « Et après ? ») | | |

**Version 5 minutes** : le pitch, le tir parfait, une vague avec le boss, MK, puis la conclusion. Garde le portail si le jury aime les surprises.

**Faire essayer le jury** (si on te le propose) : en **Facile**, aux cibles d'entraînement puis le début de la vague 1, 2 à 3 minutes maximum. Règle le casque sur la personne et explique le geste avant, pas pendant.

**Pour aller vite à MK** : **F** d'abord (améliorations et tourelles niveau 2, qui suivent le joueur dans l'arène), puis chaque vague, c'est N (lancer la vague), N (faire venir le boss), N (le tuer). Répète-le jeudi pour connaître le nombre exact d'appuis. Plus simple : **prépare une partie déjà avancée** avant ton passage, pour ne pas tout faire en direct.

## 4. Les raccourcis de démo

Ils marchent dans l'éditeur et dans un build *Development Build* (*File > Build Profiles*, case *Development Build*). Ils n'existent pas dans un build normal.

| Touche | Effet |
|---|---|
| **N** | Hors vague : lance la vague suivante. Pendant le chrono : fait venir le boss. Pendant le boss : le tue. À la vague 6 : entre dans l'arène, passe l'intro, puis tue MK. |
| **K** | Le joueur meurt (fin de partie) |
| **M** | +500 or |
| **B** | +50 matériaux |
| **H** | Termine la construction en cours |
| **C** | Un coffre devant le joueur |
| **V** | Un gisement de cristaux devant le joueur |
| **P** | Allume les 5 runes (pendant une vague), puis fait entrer dans la dimension |
| **G** | Donne les deux éclats de miroir (la porte de l'antre glitche), puis fait entrer sur le toit cyberpunk |
| **1 à 4** | Choisit la difficulté (avant la première vague) |
| **5 à 9, 0, ), =** | Équipe le 1er au 8e arc de la liste, s'il est débloqué (avant la première vague) ; **9** : l'arc cosmique, **0** : l'arc légendaire, **)** (à droite du 0) : les Akimbow, **=** : le katana (après U) |
| **F** | **Pour faire affronter MK au prof** : 2 exemplaires de chaque amélioration de la boutique, et tous les emplacements de tourelle (au bord de la voie) remplis de tourelles **niveau 2** (balistes et tours de givre en alternance ; une tourelle déjà là monte au niveau 2). Une fois par partie ; les barricades et pièges ne changent pas |
| **O** | Équipe tout de suite le katana (même verrouillé, même pendant une vague) ; **O** encore : l'arme d'avant. Au casque : une taille de katana ouvre une faille au bout de la lame, un coup de poing du gant de la matrice lance une onde de choc qui la propulse ; deux failles qui se croisent fusionnent. Au clavier : clic gauche, un coup de katana qui ouvre une faille (deux coups de suite se croisent et fusionnent) ; clic droit, une onde de choc droit devant |
| **L** | Un fragment d'arc légendaire (au 3e, l'arc est débloqué et arrive en main) |
| **U** | Débloque tous les succès, donc tous les arcs |
| **J** | Efface tous les succès : les arcs se reverrouillent (l'arc équipé redevient l'arc par défaut avant la première vague) |
| **I / T** | Après la victoire contre MK : mode infini / terminer la partie |

## 5. Ce que voit le public

- La fenêtre du jeu sur le PC montre ce que voit le joueur : mets-la sur le vidéoprojecteur (**Windows + P**, *Dupliquer*). **À vérifier jeudi**, avec le build.
- **Bouge la tête lentement** et commente ce que tu fais : une vue qui tourne vite donne mal au cœur au public et on ne comprend rien.
- Un build *Development* affiche « Development Build » dans un coin de la fenêtre du PC (pas dans le casque). Ce n'est pas grave ; si ça te gêne, garde un build normal en plus.

## 6. Plans B

| Problème | Réaction |
|---|---|
| L'image du casque se fige (le jeu continue sur le PC) | PowerShell en administrateur, `Restart-Service OVRService`, puis relancer Link dans le casque (environ 1 minute). Pendant ce temps, parler du GDD ou du projet. |
| Le casque ou Link ne marche plus du tout | **Mode clavier-souris** : lancer le jeu sans casque. ZQSD pour avancer, la souris pour regarder, **clic gauche maintenu pour tirer**, E pour interagir, Maj pour courir, Espace pour sauter. Les raccourcis marchent aussi. |
| Le build plante | Jouer depuis l'éditeur (Play sur `Prototype_Tir`, fenêtre *Scene* fermée). |
| Le PC de la démo ne marche pas | La **vidéo de secours**, sur une clé USB et sur ton téléphone. |
| Une manette s'éteint | Piles de rechange. |
| Un bug en direct (boss coincé, ennemi bloqué) | Rester calme (« c'est du direct ! ») et passer à la suite avec **N**. |
| Un testeur du jury a mal au cœur | On arrête tout de suite. Ensuite : Facile, pas de slide, rotation par crans, vignette de confort au maximum. |

## 7. Les questions probables du jury

Des pistes de réponse, à dire avec tes mots.

- **Pourquoi la VR et ce concept ?** Le tir à l'arc est le geste le plus satisfaisant en réalité virtuelle : on tend vraiment la corde. Le tower defense et le roguelike donnent une raison de rejouer.
- **Qu'est-ce qui a été le plus dur ?**
  - Le confort en VR (se déplacer sans donner le mal des transports).
  - Garder 72 images par seconde avec beaucoup d'ennemis.
  - MK, avec son arène fabriquée par le code.
  - Un bug de pilote NVIDIA qui figeait le casque en pleine partie : on l'a trouvé et réglé en revenant à un ancien pilote.
- **Comment avez-vous équilibré la difficulté ?** Des tests avec de vrais joueurs (la Nuit des assos). Résultat :
  - les vagues 3 à 5 étaient trop faciles, d'où une courbe de difficulté vague par vague ;
  - un mode Facile très simple pour ceux qui découvrent ;
  - des messages plus visibles ;
  - le dragon renforcé.
- **Et le mal des transports ?** Vignette de confort pendant le slide, rotation par crans ou continue avec une vitesse au choix, téléporteur. Le déplacement aux bras dérange moins que le joystick.
- **Comment s'est réparti le travail ?** Sois factuel et neutre : ce qui était prévu pour chacun (la flèche grenade, les arcs comme classes), ce qui est livré, et comment tu as adapté le reste.
- **Avez-vous utilisé l'IA ?** Sois transparent, c'est aussi dans le rapport :
  - **ce que l'IA a fait** : écrire une grande partie du code et des guides, avec Claude Code ;
  - **ce que tu as fait** : le jeu lui-même (le GDD, les choix de design et de direction artistique), les tests en casque, l'équilibrage à partir des tests avec des joueurs, le montage des scènes dans Unity, la recherche des bugs (dont celui du pilote) ;
  - **ton rôle** : décider, vérifier et corriger ce que l'IA proposait.
- **Et après ?**
  - Les arcs comme classes au début de la partie, et l'arc cosmique à gagner dans le portail.
  - Une nouvelle arène pour MK.
  - Une histoire « méta » révélée d'easter egg en easter egg.
  - Peut-être une version PC sans casque, puisque le mode clavier-souris existe déjà.
- **Quelques chiffres** (si on te les demande) :
  - environ 170 scripts C# et 48 000 lignes ;
  - 17 shaders faits pour le jeu ;
  - 6 vagues, 5 boss et le dragon, 4 difficultés, un mode infini.
  - Les modèles viennent de packs (Kenney et d'autres, crédités dans le README).

## 8. Le matériel à emporter

- [ ] Casque chargé, son chargeur, le **câble Link** (et une rallonge USB si possible).
- [ ] Les deux manettes, avec **des piles AA de rechange**.
- [ ] Le PC portable et **son chargeur** (sur batterie, la carte graphique ralentit).
- [ ] **Une souris** (pour le plan B clavier-souris).
- [ ] Un câble HDMI et un adaptateur pour le vidéoprojecteur ; une multiprise.
- [ ] Une clé USB avec le build et la vidéo de secours.
- [ ] Des lingettes pour le casque (si le jury essaie).
- [ ] Ce document, imprimé ou sur le téléphone.

## 9. Le matin même (30 minutes avant)

1. Brancher le PC sur secteur. Fermer Discord, le navigateur et les mises à jour (Windows et pilotes) : rien ne doit s'ouvrir par-dessus le jeu.
2. Ouvrir Meta Horizon Link : **pas de bandeau rouge** sur le pilote NVIDIA (591.86 ou plus ancien).
3. Brancher le casque, lancer Link, tracer la zone de jeu (assez grande pour balancer les bras).
4. Brancher le vidéoprojecteur (Windows + P, *Dupliquer*).
5. Lancer le build. Vérifier le son de la salle, choisir la difficulté, tirer une flèche.
6. Classement : vider `leaderboard.json` (dans `AppData\LocalLow\DefaultCompany\THUNK VR`), ou garder quelques scores pour l'exemple.
7. Retirer le casque, le poser lentilles à l'abri de la lumière, et attendre ton tour.
