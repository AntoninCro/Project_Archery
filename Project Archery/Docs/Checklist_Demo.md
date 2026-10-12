# Tests complets et préparation de la démo

À faire dans les derniers jours avant le rendu (vendredi 23 octobre). Coche au fur et à mesure.

## 1. Tests complets (dans le casque)

Une partie en **Normal**, puis une en **Impossible**, en vérifiant :

**Tir**
- [ ] Prendre l'arc dans chaque main ; prendre une flèche dans le dos, l'encocher, tendre, tirer.
- [ ] Anneau vide au centre : rouge, orange, vert, vert pastel ; « ding » et vibration en entrant dans le vert ; tir parfait plus rapide et plus fort ; le cercle s'arrête dans le vert pastel sans recommencer.
- [ ] Aide à la visée de la couleur de l'anneau : trajectoire complète en Facile et Normal, ligne droite en Difficile, rien en Impossible.
- [ ] Coup au corps à corps : la flèche reste plantée dans l'ennemi.

**Partie**
- [ ] Menu : Jouer, difficulté (le ciel change), Paramètres (volumes, rotation, confort), Classement, Quitter.
- [ ] Gong, chrono, montre (score, chrono, vague, PV, tour, or, combo, difficulté, bonus).
- [ ] Voile rouge autour de la vision quand les PV baissent, qui bat sous 25 % des PV.
- [ ] Ennemis : vague 1 Rampants, Gluants, Champignons ; vague 2 Tireurs, Gluants bleus, Étoiles de mer ; vague 3 Volants, Guêpes, Champignons furieux, Cactus, Fantassins ; vague 4 Orcs, Tortues, Liches, Gargouilles ; vague 5 Mimiques. Chacun s'anime (course, attaque, touché, mort) et ses zones de touche tombent juste (tête ×2).
- [ ] Un boss différent à chaque vague (Roi Gluant, Mimique vorace, Tortue colossale, Chef de guerre orc, Liche suprême), avec son nom, sa barre de PV, son point faible cyan et ses renforts.
- [ ] Tour détruite : le gong descend au pied de la tour, et remonte à la reconstruction.
- [ ] Barricades : les ennemis s'y arrêtent, les cassent, la réparation en boutique les relève.
- [ ] Fin de vague : les survivants fuient, la boutique s'ouvre, les PV du joueur reviennent.
- [ ] Boutique : améliorations, relance, arc suivant, tour, barricades ; les prix montent après chaque achat.
- [ ] Coffre : chute et rebond à l'apparition, rayon de lumière, couvercle, ouverture animée, ralenti, 3 orbes avec leur nom, récompense ; il disparaît en fin de vague.
- [ ] Brasero : flèche enflammée, l'ennemi brûle.
- [ ] Tower defense : gisements de cristaux pendant la vague, récolte, matériaux sur la montre ; socle visé à la poignée, panneau, plan bleu, marteau, 3 coups, l'arc revient ; amélioration d'un bâtiment.
- [ ] Bâtiments : la baliste pivote et tire (volants compris), la tour de givre ralentit, les piques jaillissent, les ennemis s'arrêtent devant une barricade construite ; le message « Défenses : … % des dégâts » en fin de vague.
- [ ] Déplacements : marche, course aux bras, slide, descente de la tour à pied, téléporteur (bande autour de la tour, bouton « Retour à la tour » de la boutique), pas de flèche au joystick droit.
- [ ] Vagues : accalmie (peu d'ennemis), montée, boss à la fin du chrono ; deux coups de gong font venir le boss plus tôt (bonus affiché) ; or de fin de vague avec ses bonus.
- [ ] Bouton « Tour » sous la montre : on le touche avec l'autre main, téléportation, 25 s de recharge pendant les vagues.
- [ ] Victoire après le boss de la vague 5 : « Terminer la partie » dans la boutique (écran « Victoire ! »), ou le gong pour le mode infini (plusieurs boss).
- [ ] Tour détruite : les ruines remplacent la tour (fumée, son), le joueur est évacué ; reconstruite en boutique, elle revient.
- [ ] Les messages (début de vague, boss, coffre, cristaux) restent devant le regard, lisibles.
- [ ] Les deux ponts de la voie du milieu se traversent sans sauter.
- [ ] Un coffre ouvert disparaît 15 s après.
- [ ] Mort : « Tu es mort » en grand, écran de fin qui suit le joueur, nom au clavier virtuel, classement avec la nouvelle ligne, *Rejouer*.
- [ ] Les paramètres et le classement sont gardés après avoir relancé le jeu.

**Confort et fluidité**
- [ ] Au moins 72 images par seconde dans les moments chargés (fin de vague Impossible, multitir, déluge). Vue *Game* > *Stats*.
- [ ] Aucune erreur rouge dans la console pendant toute une partie.
- [ ] La vignette de confort pendant le slide ; aucun malaise sur une partie de 15 minutes.

## 2. Le build

- [ ] *File > Build Profiles* > *Windows*, la scène `Prototype_Tir` dans la liste.
- [ ] *Player* : *Company Name* et *Product Name* corrects (ils donnent le dossier des sauvegardes).
- [ ] Le build se lance, le casque l'affiche via Quest Link, et une partie entière se joue sans l'éditeur.
- [ ] Les raccourcis clavier de test sont absents du build final (ils n'existent que dans l'éditeur et les builds *Development*).

## 3. Le jour de la démo

**Matériel**
- [ ] Casque chargé, manettes avec des piles neuves (et des piles de rechange).
- [ ] Câble Link testé sur le PC de la démo, ou Air Link sur un réseau 5 GHz fiable.
- [ ] Zone de jeu (*Guardian*) tracée, assez grande pour se tourner et balancer les bras.
- [ ] Le build sur le PC (et une copie sur une clé USB).

**Avant de commencer**
- [ ] Vider le classement de test : supprimer `leaderboard.json` dans `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Project Archery`, ou garder quelques bons scores pour l'exemple.
- [ ] Volumes réglés pour la salle.
- [ ] Décider de la difficulté : Normal pour une démo fluide, Impossible pour impressionner.

**Déroulé conseillé (5 à 10 minutes)**
1. Le menu : changer de difficulté et montrer le ciel qui change.
2. Les cibles d'entraînement : le tir, l'anneau, un tir parfait.
3. Le gong : vague 1, une flèche enflammée au brasero, puis la boutique (une amélioration).
4. Une vague avec un coffre : descendre, courir, glisser, ouvrir le coffre.
5. Montrer le boss (dans l'éditeur, **N** jusqu'à la vague 5).
6. Mourir, saisir son nom, montrer le classement.

**Solution de secours** : si le build pose problème, jouer depuis l'éditeur, où les raccourcis (**N**, **M**, **C**) permettent de montrer chaque partie rapidement.

## 4. Le rendu

- [ ] README à jour : crédits complets (packs de la carte, sons et musiques définitifs), équipe.
- [ ] GDD à jour (planning coché, choix finaux).
- [ ] Dépôt GitHub propre : `main` jouable, dernier commit poussé.
- [ ] Rapport : ce qui a été fait, les choix, l'usage de l'IA.
