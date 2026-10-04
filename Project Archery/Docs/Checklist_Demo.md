# Tests complets et préparation de la démo

À faire dans les derniers jours avant le rendu (vendredi 23 octobre). Coche au fur et à mesure.

## 1. Tests complets (dans le casque)

Une partie en **Normal**, puis une en **Impossible**, en vérifiant :

**Tir**
- [ ] Prendre l'arc dans chaque main ; prendre une flèche dans le dos, l'encocher, tendre, tirer.
- [ ] Anneau : rouge, orange, vert ; « ding » et vibration en entrant dans le vert ; tir parfait plus rapide et plus fort.
- [ ] Aide à la visée en Facile et Normal, absente en Difficile et Impossible.
- [ ] Coup au corps à corps : la flèche reste plantée dans l'ennemi.

**Partie**
- [ ] Menu : Jouer, difficulté (le ciel change), Paramètres (volumes, rotation, confort), Classement, Quitter.
- [ ] Gong, chrono, montre (score, chrono, vague, PV, tour, or, combo, difficulté, bonus).
- [ ] Ennemis : Rampants (vague 1), Volants (vague 2), Tireurs (vague 4), boss (vague 5).
- [ ] Barricades : les ennemis s'y arrêtent, les cassent, la réparation en boutique les relève.
- [ ] Fin de vague : les survivants fuient, la boutique s'ouvre, les PV du joueur reviennent.
- [ ] Boutique : améliorations, relance, arc suivant, tour, barricades, brasero ; les prix montent après chaque achat.
- [ ] Coffre : rayon de lumière, couvercle, ralenti, 3 orbes, récompense ; il disparaît en fin de vague.
- [ ] Brasero : flèche enflammée, l'ennemi brûle.
- [ ] Déplacements : marche, course aux bras, slide, téléporteur, descente de la tour à pied.
- [ ] Victoire après la vague 10, puis mode infini.
- [ ] Mort : écran de fin, nom au clavier virtuel, classement avec la nouvelle ligne, *Rejouer*.
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
3. Le gong : vague 1, puis la boutique (une amélioration, le brasero).
4. Une vague avec un coffre : descendre, courir, glisser, ouvrir le coffre.
5. Montrer le boss (dans l'éditeur, **N** jusqu'à la vague 5).
6. Mourir, saisir son nom, montrer le classement.

**Solution de secours** : si le build pose problème, jouer depuis l'éditeur, où les raccourcis (**N**, **M**, **C**) permettent de montrer chaque partie rapidement.

## 4. Le rendu

- [ ] README à jour : crédits complets (packs de la carte, sons et musiques définitifs), équipe.
- [ ] GDD à jour (planning coché, choix finaux).
- [ ] Dépôt GitHub propre : `main` jouable, dernier commit poussé.
- [ ] Rapport : ce qui a été fait, les choix, l'usage de l'IA.
