# Guide : menu, paramètres, fin de partie et classement

Ce guide ajoute les menus du GDD (sections 14 et 15) à la scène `Prototype_Tir` :

1. **Menu principal**, en haut de la tour avant la première vague :
   - *Jouer*, avec le choix de la difficulté ;
   - *Paramètres* ;
   - *Classement* ;
   - *Quitter*.

   Le gong et les panneaux de difficulté marchent toujours.
2. **Paramètres** : volumes général, musique et effets, et rotation au joystick (par crans). Ils sont enregistrés dans `settings.json`.
3. **Fin de partie** : après ta mort, un écran apparaît devant toi.
   - Il affiche le résumé de la partie : score, vague, difficulté, ennemis tués, tirs à la tête, tirs parfaits.
   - Tu tapes ton nom avec le **clavier virtuel d'XRI**, puis tu enregistres.
   - Le **classement** s'affiche, avec ta ligne en doré.
   - *Rejouer* ramène au menu, avec un fondu.
4. **Classement** : les 10 meilleures parties, du meilleur score au moins bon, enregistrées dans `leaderboard.json`.

Les fichiers JSON sont dans `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Project Archery`. Tu peux changer *Company Name* dans *Project Settings > Player*, par exemple en `ST2OOS`.

Ce guide suppose que le téléporteur est en place (`Guide_Teleporteur.md`) : l'écran de fin utilise son fondu au noir.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Game Settings` | Charge, applique et enregistre les paramètres | Objet `Game` |
| `Main Menu Panel` | Pages du menu, boutons Jouer / difficulté / Quitter ; visible avant la première vague | Canvas du menu |
| `Settings Panel` | Curseurs de volume et rotation au joystick | Page Paramètres |
| `Leaderboard View` | Tableau du classement dans un texte | Texte du classement (menu et fin de partie) |
| `Game Over Panel` | Écran de fin : résumé, nom, enregistrement, rejouer | Canvas de fin de partie |

Les sons sont dans `Audio/Placeholder` : `ui_click` pour les boutons ; pour l'enregistrement du score, tu peux reprendre `perfect_chime`.

**Raccourci de test** : dans l'éditeur, **K** termine la partie (le joueur meurt).

## 1. Le mixer audio

Les volumes passent par un *Audio Mixer*, l'outil d'Unity pour régler le son par groupes.

1. Dans `Assets/_Project/Audio`, clic droit > *Create > Audio Mixer*, nomme-le `MainMixer`.
2. Ouvre-le (double-clic). Dans *Groups*, sélectionne `Master`, clique sur `+` et ajoute deux groupes : `Music` et `Effects`.
3. Expose les trois volumes :
   - sélectionne le groupe `Master` ;
   - dans l'Inspector, fais un clic droit sur *Volume* > *Expose 'Volume (of Master)' to script* ;
   - fais de même pour `Music` et `Effects`.
4. En haut à droite de la fenêtre *Audio Mixer*, ouvre *Exposed Parameters* et renomme les trois paramètres (double-clic) : `MasterVolume`, `MusicVolume`, `EffectsVolume`.
5. Fais passer les sons des prefabs par le groupe des effets :
   - prefab `Bow` : l'*Audio Source* de `Model` (le grincement), *Output* `Effects` ;
   - prefab `Arrow` : son *Audio Source* (le sifflement), *Output* `Effects`.

Les sons joués par le code (impacts, gong, boutique…) passeront par `Effects` grâce à `Game Settings`. Le groupe `Music` attend les musiques de la fin du projet.

## 2. Les paramètres

Sur l'objet `Game` : *Add Component > Game Settings* :
- *Mixer* : `MainMixer` ;
- *Effects Group* : le groupe `Effects` (déplie `MainMixer` dans le Project pour le trouver).

Les noms des paramètres (`MasterVolume`…) sont déjà remplis.

## 3. Le menu principal

Il prend la place de la boutique, à ta gauche. Ils ne sont jamais visibles en même temps : le menu avant la première vague, la boutique pendant les pauses.

### Le Canvas

1. *GameObject > UI > Canvas*, nomme-le `Main Menu` :
   - *Render Mode* `World Space` ;
   - Position `(-1.4, 5.2, 1.6)`, Rotation `(15, -41, 0)` ;
   - Width `1000`, Height `800`, Scale `(0.001, 0.001, 0.001)`.
2. *Add Component > Tracked Device Graphic Raycaster*.
3. Dans `Main Menu`, *UI > Image*, nomme-la `Content` : étirée sur tout le Canvas, noire, alpha 170.
4. Dans `Content`, un texte `Title` (*UI > Text - TextMeshPro*) :
   - ancre en haut au centre, position `(0, -60)`, 900 × 90 ;
   - taille 56, gras, centré, texte « VR Archery Challenge ».

### Les pages

Chaque page est un objet vide dans `Content` :
- étiré sur tout `Content`, avec *Left* et *Right* `60`, *Top* `140`, *Bottom* `40` ;
- avec un *Vertical Layout Group* : *Spacing* 18, *Child Alignment* `Upper Center`, coche *Control Child Size* (Width et Height) et *Child Force Expand* Width, décoche *Child Force Expand* Height.

Pour fixer la hauteur d'un élément, ajoute-lui un *Layout Element* (*Add Component > Layout > Layout Element*) et remplis *Preferred Height*.

**Page `Home`** (accueil), de haut en bas :

| Élément | Création | Réglages |
|---|---|---|
| `Play` | *UI > Button - TextMeshPro* | texte « Jouer », taille 40 ; *Preferred Height* 90 |
| `Difficulty Row` | *Create Empty* | *Horizontal Layout Group* : *Spacing* 20, *Control Child Size* coché (W et H), *Child Force Expand* : coche Height, décoche Width ; *Preferred Height* 80 |
| ↳ `Previous` | *Button - TextMeshPro* | texte « < », taille 40 ; *Preferred Width* 100 |
| ↳ `Difficulty` | *Text - TextMeshPro* | taille 40, centré ; *Flexible Width* 1 |
| ↳ `Next` | *Button - TextMeshPro* | texte « > », taille 40 ; *Preferred Width* 100 |
| `Settings Button` | *Button - TextMeshPro* | « Paramètres », taille 34 ; *Preferred Height* 80 |
| `Leaderboard Button` | *Button - TextMeshPro* | « Classement », taille 34 ; *Preferred Height* 80 |
| `Quit` | *Button - TextMeshPro* | « Quitter », taille 34 ; *Preferred Height* 80 |

**Page `Settings`** : pour chaque volume (Général, Musique, Effets), une ligne `Row` :
- *Create Empty*, avec un *Horizontal Layout Group* : *Spacing* 20, *Child Alignment* `Middle Left`, *Control Child Size* coché (W et H), *Child Force Expand* décoché ;
- *Preferred Height* 70 ;
- trois enfants :
  - `Label` : texte « Général » (ou « Musique », « Effets »), taille 32 ; *Preferred Width* 260, *Preferred Height* 60 ;
  - `Slider` : *UI > Slider* ; *Flexible Width* 1, *Preferred Height* 40 ;
  - `Value` : texte, taille 32 ; *Preferred Width* 120, *Preferred Height* 60.

Ensuite, en dessous des lignes :
- `Snap Turn` : *Button - TextMeshPro*, taille 30 ; *Preferred Height* 80. Son texte est rempli en jeu ;
- `Back` : *Button - TextMeshPro*, « Retour » ; *Preferred Height* 80.

**Page `Leaderboard`** :
- `Table` : *Text - TextMeshPro*, taille 26, aligné en haut à gauche ; *Layout Element* avec *Flexible Height* 1 ;
- *Add Component > Leaderboard View* sur `Table` (son texte est trouvé tout seul) ;
- `Back` : bouton « Retour », *Preferred Height* 80.

### Brancher le menu

1. Sur `Main Menu` : *Add Component > Main Menu Panel* :
   - *Content* : `Content` ;
   - *Pages* : 3 éléments, dans l'ordre `Home`, `Settings`, `Leaderboard` ;
   - *Difficulty Text* : le texte `Difficulty` ;
   - *Click Clip* : `ui_click`.
2. Les *On Click* des boutons. Pour chacun : clique sur `+`, glisse `Main Menu`, puis choisis la méthode de *MainMenuPanel* :

| Bouton | Méthode |
|---|---|
| `Play` | `Play ()` |
| `Previous` | `PreviousDifficulty ()` |
| `Next` | `NextDifficulty ()` |
| `Settings Button` | `ShowPage (int)` avec `1` |
| `Leaderboard Button` | `ShowPage (int)` avec `2` |
| `Quit` | `Quit ()` |
| `Back` (des deux pages) | `ShowPage (int)` avec `0` |

3. Sur la page `Settings` : *Add Component > Settings Panel* :
   - les trois curseurs et les trois textes `Value` (*Master*, *Music*, *Effects*) ;
   - *Snap Turn Button* : `Snap Turn`, et *Snap Turn Text* : son texte.

Le menu disparaît dès que la vague 1 commence, que ce soit par le gong ou par *Jouer*.

## 4. Le clavier virtuel

1. *Window > Package Manager* > *XR Interaction Toolkit* > onglet *Samples* > **Spatial Keyboard** > *Import*.
2. Glisse le prefab `XRI Global Keyboard Manager` (dans `Samples/XR Interaction Toolkit/3.5.1/Spatial Keyboard/Prefabs`) dans la scène.
   - *Player Root* : `XR Origin (XR Rig)`, pour que le clavier suive le joueur.

Il fait apparaître le clavier quand on clique dans un champ de texte qui utilise ce clavier (prefab `Input Field Global Keyboard`).

## 5. L'écran de fin de partie

Il se place tout seul devant toi quand il apparaît : sa position dans la scène n'a pas d'importance.

1. Nouveau Canvas `Game Over`, *World Space*, Width `1000`, Height `900`, Scale `(0.001, 0.001, 0.001)`. *Add Component > Tracked Device Graphic Raycaster*.
2. `Content` : *UI > Image*, étirée sur tout le Canvas, noire, alpha 200. Ajoute-lui un *Vertical Layout Group* :
   - *Padding* 30, *Spacing* 12, *Child Alignment* `Upper Center` ;
   - coche *Control Child Size* (W et H) et *Child Force Expand* Width, décoche *Child Force Expand* Height.
3. Dans `Content`, de haut en bas :

| Élément | Création | Réglages |
|---|---|---|
| `Title` | texte | taille 56, gras, centré ; *Preferred Height* 80 |
| `Summary` | texte | taille 30, centré ; *Preferred Height* 140 |
| `Name Section` | *Create Empty* | *Horizontal Layout Group* : *Spacing* 20, *Control Child Size* coché (W et H), *Child Force Expand* décoché ; *Preferred Height* 80 |
| ↳ `Name Label` | texte | « Ton nom : », taille 32 ; *Preferred Width* 200 |
| ↳ `Input Field Global Keyboard` | prefab du sample (glisse-le depuis `Spatial Keyboard/Prefabs`) | *Flexible Width* 1, *Preferred Height* 70 |
| ↳ `Save` | *Button - TextMeshPro* | « Enregistrer », taille 30 ; *Preferred Width* 260 |
| `Message` | texte | taille 32, centré, doré ; *Preferred Height* 50 |
| `Table` | texte | taille 24, aligné en haut à gauche ; *Flexible Height* 1 ; *Add Component > Leaderboard View* |
| `Replay` | *Button - TextMeshPro* | « Rejouer », taille 36 ; *Preferred Height* 80 |

4. Sur `Game Over` : *Add Component > Game Over Panel* :
   - *Content* : `Content`, *Title Text* : `Title`, *Summary Text* : `Summary` ;
   - *Name Section* : `Name Section` ;
   - *Name Input* : le champ du prefab `Input Field Global Keyboard` (l'objet qui a le *TMP_InputField*) ;
   - *Message Text* : `Message`, *Leaderboard* : `Table` ;
   - *Save Clip* : `perfect_chime`.
5. *On Click* des boutons :
   - `Save` → `Game Over` > *GameOverPanel* > `SaveScore ()` ;
   - `Replay` → `Game Over` > *GameOverPanel* > `Replay ()`.
6. Pour enregistrer aussi avec la touche Entrée du clavier : sur le prefab du champ, composant *XR Keyboard Display*, événement *On Text Submitted (String)* → `+`, glisse `Game Over`, et choisis `SubmitName` **dans la partie *Dynamic string*** du menu.

`Game Over Panel` désactive tout seul le rechargement automatique de `Player Health` : c'est désormais lui qui relance la partie.

## 6. Tester

1. Lance Play : le menu est à ta gauche.
   - `<` et `>` changent la difficulté, et le ciel suit.
   - *Paramètres* : les curseurs changent le volume tout de suite. *Rotation au joystick : non* coupe la rotation au joystick droit.
   - *Classement* : « Aucune partie enregistrée pour l'instant ».
2. *Jouer* : la vague 1 commence et le menu disparaît.
3. Appuie sur **K** : la partie se termine et les chevaliers s'enfuient. 3 s plus tard, l'écran de fin apparaît devant toi.
4. Vise le champ du nom et appuie sur la gâchette : le clavier apparaît. Tape ton nom, puis *Enregistrer* (ou Entrée).
   - Le classement s'affiche avec ta ligne en doré.
   - Le message indique ta place.
5. *Rejouer* : fondu au noir, retour au menu. Ton nom sera proposé à la partie suivante, et les paramètres sont gardés.
6. Relance Unity : le classement et les paramètres sont toujours là (fichiers JSON).

## En cas de problème

- **Les boutons ne réagissent pas au rayon** : le Canvas doit avoir le *Tracked Device Graphic Raycaster*, et la main qui vise doit être vide.
- **Le volume ne change pas** :
  - les paramètres du mixer doivent être exposés et nommés exactement `MasterVolume`, `MusicVolume` et `EffectsVolume` (sinon, la console le signale) ;
  - les *Audio Source* des prefabs doivent avoir *Output* `Effects`.
- **Le clavier n'apparaît pas** : il faut le `XRI Global Keyboard Manager` dans la scène, et un champ qui vient du prefab `Input Field Global Keyboard`.
- **La scène se recharge pendant que je tape mon nom** : il manque le `Game Over Panel` dans la scène, ou il est désactivé.
- **Le menu reste affiché pendant la partie** : *Content* doit être rempli dans `Main Menu Panel`.
- **Je veux vider le classement** : supprime `leaderboard.json` dans le dossier indiqué en haut de ce guide.
