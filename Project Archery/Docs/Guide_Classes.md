# Guide : les arcs comme classes, et la progression entre les parties

Ce guide ajoute deux idées de fin de projet du GDD (section 23), qui vont ensemble :
- **les arcs comme des classes** : on choisit son arc au menu, avant la partie, avec ses caractéristiques. La boutique ne vend plus d'arcs ; elle garde les améliorations et les services de la tour ;
- **une progression d'une partie à l'autre** : chaque partie rapporte de l'**expérience** (1 XP pour 10 points de score). Elle est gardée dans `progress.json` et débloque les arcs peu à peu.

| Arc | Débloqué à |
|---|---|
| Arc de chasse | dès le début |
| Arc composite | 300 XP |
| Arc long | 1 000 XP |
| Arc runique | 2 500 XP |

Ces seuils sont déjà réglés dans les assets de `Data/Bows` (champ *Unlock Xp*).

**C'est un mode à part** : tant que tu n'ajoutes pas le composant `Bow Classes`, rien ne change, et les arcs s'achètent en boutique comme avant. Tu peux donc monter ce guide et revenir en arrière en désactivant le composant.

L'arc légendaire des coffres (`Guide_ArcLegendaire.md`) remplace toujours l'arc choisi, jusqu'à la fin de la partie.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Bow Classes` | Choix de l'arc, expérience, déblocages | Objet `Game` |
| `Main Menu Panel` (déjà là) | Nouveaux champs : *Bow Text*, *Progress Text* ; méthodes `PreviousBow` et `NextBow` | Menu principal |
| `Game Over Panel` (déjà là) | Nouveau champ : *Xp Text* | Écran de fin |

## 1. Le gestionnaire

Sur l'objet `Game` : *Add Component > Bow Classes* :
- *Bows* : 4 éléments, dans l'ordre `Bow_Chasse`, `Bow_Composite`, `Bow_Long`, `Bow_Runique` ;
- *Unlock Clip* : `legendary_part`.

Pour une démo où tout doit être disponible tout de suite, coche *Unlock All*.

## 2. Le choix de l'arc au menu

La page d'accueil du menu (`Home`) reçoit une ligne pour l'arc, sur le modèle de celle de la difficulté.

1. Sélectionne le Canvas `Main Menu` : passe *Height* de `800` à `1050`, pour faire de la place.
2. Dans `Home`, duplique `Difficulty Row` (Ctrl+D), renomme la copie `Bow Row` et place-la juste en dessous. Dans `Bow Row` :
   - renomme le texte du milieu `Bow` : taille 26, *Preferred Height* `110` (trois lignes : nom, caractéristiques, description) ;
   - garde les boutons `<` et `>`.
3. Sous `Bow Row`, ajoute un texte `Progress` (*UI > Text - TextMeshPro*) : taille 24, centré, *Preferred Height* `60`.
4. Les *On Click* des deux boutons de `Bow Row` : retire l'ancienne méthode (copiée de la difficulté), puis glisse `Main Menu` et choisis :
   - bouton `<` : `MainMenuPanel > PreviousBow ()` ;
   - bouton `>` : `MainMenuPanel > NextBow ()`.
5. Sur `Main Menu` : *Bow Text* `Bow`, *Progress Text* `Progress`.

Les flèches ne parcourent que les arcs débloqués. Le texte du bas indique l'expérience et le prochain arc à débloquer.

## 3. L'expérience sur l'écran de fin

1. Dans le `Content` de l'écran `Game Over`, ajoute un texte `Xp` sous le résumé : taille 28, centré, doré, *Preferred Height* `80`.
2. Sur `Game Over` : *Xp Text* `Xp`.

Il affiche par exemple « +120 XP (total 760) », puis « Nouvel arc : Arc long ! » si un arc vient d'être débloqué.

## 4. Tester

**Raccourci de test** (éditeur) : la touche **X** donne 500 XP.

1. Lance Play. Le menu affiche « Arc de chasse », ses caractéristiques, et « Expérience : 0 XP · prochain arc : Arc composite à 300 XP ».
2. Appuie deux fois sur **X** (1 000 XP) : le texte du bas indique maintenant l'arc runique.
3. Avec `>`, passe à l'arc composite, puis à l'arc long : l'arc change dans ta main.
4. Lance la partie, puis **N** pour finir la vague : la carte de l'arc indique que l'arc se choisit au menu.
5. **K** pour mourir : l'écran de fin affiche l'expérience gagnée.
6. *Rejouer* : le menu garde l'arc choisi et l'expérience.

**Remettre la progression à zéro** : supprime `progress.json` dans `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Project Archery`.

## En cas de problème

- **La boutique vend encore des arcs** : `Bow Classes` doit être actif sur `Game`.
- **Les flèches du menu ne font rien** :
  - vérifie leurs *On Click* (`PreviousBow`, `NextBow`) ;
  - les autres arcs ne sont peut-être pas encore débloqués : regarde le texte du bas, ou appuie sur **X** ;
  - on ne change d'arc qu'avant la première vague.
- **L'arc ne change pas d'apparence** : son asset n'a pas de *Visual Prefab* (`Guide_Boutique.md`, section 5) ; ses caractéristiques changent quand même.
- **Pas d'expérience à la fin** : le score était de 0, ou *Xp Text* n'est pas branché.
