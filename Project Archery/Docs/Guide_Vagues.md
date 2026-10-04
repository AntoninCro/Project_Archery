# Guide : la première vague, le chrono et le score

Ce guide monte la boucle de jeu dans la scène `Prototype_Tir` :

1. tu tires dans un **gong** ;
2. une **vague** démarre, avec un chrono de 30 s pour la première vague, puis 5 s de plus à chaque vague ;
3. des chevaliers apparaissent au fil du chrono ;
4. à la fin du chrono, les survivants s'enfuient, tes PV reviennent et tu gagnes un bonus ;
5. tu retires dans le gong pour la vague suivante. Après la vague 10, c'est la victoire, puis le mode infini.

Le score suit le GDD (section 8) :
- chaque touche rapporte 5 points au corps et 10 à la tête ;
- chaque élimination rapporte la valeur de l'ennemi, ×2 s'il meurt d'un headshot ;
- tout est multiplié par la qualité du tir, la distance (au-delà de 10 m) et le combo ;
- 25 % des points deviennent de l'argent.

Tout s'affiche sur une **montre au poignet** et sur un **grand panneau** dans le décor.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Wave Settings` | Asset de données : nombre de vagues, durées, budget d'ennemis, bonus | `Data/Waves` |
| `Wave Manager` | Enchaîne les vagues, gère le chrono et les apparitions | Objet `Game` |
| `Score Manager` | Score, combo, argent, statistiques | Objet `Game` |
| `Wave Start Gong` | Lance la vague suivante quand une flèche le touche | Le gong |
| `Hud Display` | Remplit des textes TextMeshPro avec l'état de la partie | Chaque Canvas d'affichage |
| `Wrist Mount` | Accroche un objet au poignet de la main libre | Canvas de la montre |

Les sons sont déjà dans `Audio/Placeholder` : `gong`, `wave_start_horn`, `wave_clear`, `countdown_tick`, `coin`, `victory`, `game_over`.

## 1. Les réglages des vagues

1. Dans `Assets/_Project/Data`, crée un dossier `Waves`.
2. Dedans : clic droit > *Create > Archery > Wave Settings*, nomme-le `WaveSettings`.
3. Dans la liste *Enemies*, ajoute un élément :
   - *Prefab* : `Enemy_Rampant` ;
   - *Cost* `1`, *First Wave* `1`, *Weight* `1`.

Les autres valeurs suivent le GDD :
- 10 vagues pour gagner ;
- 30 s pour la vague 1, puis +5 s par vague ;
- un budget de 6 ennemis, puis +3 par vague, qui apparaissent pendant 80 % du chrono. La difficulté et sa courbe exponentielle le multiplient ensuite (GDD, section 11) ;
- 12 ennemis au maximum en même temps à la vague 1 ; ce maximum grandit avec la difficulté et les vagues, jusqu'à 40 (*Max Alive Cap*) ;
- en mode infini, la courbe s'accélère : chaque vague multiplie en plus les PV des ennemis par 1,12, leurs dégâts par 1,06 et leur nombre par 1,08 (partie *Mode infini*) ;
- un bonus de 50 × le numéro de la vague.

## 2. Le spawner

Sélectionne `Enemy Spawner` et **décoche *Spawn On Start*** : c'est maintenant le Wave Manager qui décide des apparitions.

## 3. Les gestionnaires

1. *Create Empty*, nomme-le `Game`.
2. *Add Component > Wave Manager* :
   - *Settings* : `WaveSettings` ;
   - *Spawner* : laisse vide, il est trouvé tout seul ;
   - sons : *Wave Start Clip* `wave_start_horn`, *Wave Clear Clip* `wave_clear`, *Countdown Clip* `countdown_tick`, *Victory Clip* `victory`, *Game Over Clip* `game_over` ;
   - *Auto Start Delay* : laisse `0`, la vague démarre au gong.
3. *Add Component > Score Manager* : *Shot Tuning* `Data/ShotTuning`, *Kill Clip* `coin`.
4. Sur le XR Origin, dans `Player Health`, passe *Reload Delay* à `5`. Tu auras ainsi le temps de lire le résumé de fin de partie avant que la scène recommence.

## 4. Le gong

On le pose sur la tour, à ta droite.

1. *Create Empty*, nomme-le `Gong`, Position `(2, 4, 0)`.
2. Enfant `Post` : *3D Object > Cube*, Position `(0, 0.8, 0)`, Scale `(0.08, 1.6, 0.08)`.
3. Enfant `Disc` : *3D Object > Cylinder*, Position `(0, 1.4, 0)`, Rotation `(0, 0, 90)`, Scale `(0.9, 0.04, 0.9)`.
   - Supprime son *Capsule Collider* (mauvaise forme pour un disque) et ajoute un *Box Collider* : il s'ajuste tout seul.
4. Crée un matériau `Gong_Bronze` (*Universal Render Pipeline/Lit*, couleur bronze, *Metallic* 0.8, *Smoothness* 0.6) et mets-le sur le disque.
5. Sur `Gong` : *Add Component > Wave Start Gong*, avec *Gong Clip* `gong` et *Swing Part* `Disc`.

Le gong disparaît pendant les vagues et réapparaît à la pause.

## 5. Les cibles d'entraînement seulement pendant les pauses

1. *Create Empty*, nomme-le `Training`, et glisse dedans les cibles d'entraînement et les mannequins.
2. Sur le `Wave Manager` :
   - *On Wave Started* : clique sur `+`, glisse `Training`, choisis *GameObject > SetActive (bool)* et laisse la case **décochée** ;
   - *On Wave Ended* : pareil, avec la case **cochée**.

## 6. La montre au poignet

Elle s'accroche toute seule au poignet de la main qui ne tient pas l'arc.

1. *GameObject > UI > Canvas*, nomme-le `Wrist HUD`.
   - *Render Mode* `World Space`. Dans le *Rect Transform* : Width `300`, Height `200`, Scale `(0.0006, 0.0006, 0.0006)`.
   - Si Unity a aussi créé un `EventSystem` et que la console affiche des erreurs d'Input, clique dans son Inspector sur *Replace with InputSystemUIInputModule*. Garde-le : il servira pour les menus.
2. Clic droit sur le Canvas > *UI > Image*, nomme-la `Background` :
   - étire-la sur tout le Canvas (*Anchor Presets*, Alt + clic sur l'étirement complet) ;
   - couleur noire, alpha 150.
   - *Add Component > Vertical Layout Group* : *Padding* 12, *Spacing* 2, *Child Alignment* `Middle Center`, coche *Control Child Size* (Width et Height) et décoche *Child Force Expand* (Height).
3. Dans `Background`, crée 7 textes avec clic droit > *UI > Text - TextMeshPro* : `Timer`, `Wave`, `Score`, `Combo`, `Player HP`, `Tower HP` et `Money`.
   - *Font Size* : 44 pour `Timer`, 26 pour les autres ;
   - alignement centré ;
   - le texte de départ n'a pas d'importance, il est remplacé en jeu.
4. Sur `Wrist HUD` :
   - *Add Component > Hud Display* : glisse chaque texte dans le champ du même nom ;
   - *Add Component > Wrist Mount*.

En jeu, si la montre est mal placée ou mal inclinée, sélectionne `Wrist HUD`. Règle *Local Position* et *Local Euler Angles* de `Wrist Mount`, puis recopie les valeurs après avoir arrêté Play.

## 7. Le grand panneau

Un tableau visible depuis la tour, pour le chrono, la vague et le score.

1. Nouveau Canvas `Scoreboard`, *Render Mode* `World Space` :
   - Position `(0, 9, 14)`, Rotation `(0, 0, 0)` ;
   - Width `800`, Height `300`, Scale `(0.01, 0.01, 0.01)`.
2. Ajoute un fond (Image noire, alpha 150) et trois textes : `Timer` (taille 140), `Wave` (60) et `Score` (60).
3. *Add Component > Hud Display*, et branche seulement ces trois textes.

## 8. Tester

1. Lance Play et tire dans le gong : « Vague 1 / 10 » s'affiche et les chevaliers arrivent.
2. Tue-les :
   - chaque élimination affiche « +points » en doré, avec un son de pièce ;
   - le combo monte à chaque touche et retombe à zéro si une flèche ne touche aucun ennemi.
3. À 0:00 :
   - les survivants s'enfuient ;
   - « Vague 1 terminée +50 » s'affiche ;
   - tes PV remontent ;
   - les cibles d'entraînement et le gong réapparaissent.
4. Retire dans le gong pour la vague 2.

**Raccourci de test** : dans l'éditeur, la touche **N** lance la vague suivante, ou termine tout de suite la vague en cours.

## En cas de problème

- **Rien ne se passe quand je touche le gong** : la console dit-elle « aucun Wave Manager » ? Le gong est-il bien un enfant de `Gong`, avec un collider ?
- **« aucun ennemi disponible pour la vague 1 »** : la liste *Enemies* de `WaveSettings` est vide, ou son *Prefab* n'est pas rempli.
- **Les ennemis apparaissent dès le lancement** : *Spawn On Start* est encore coché sur le spawner.
- **Les textes de la montre ne changent pas** : vérifie les champs de `Hud Display`, et qu'il y a un `Score Manager` et un `Wave Manager` dans la scène.
- **La montre est invisible** : en Play, sélectionne `Wrist HUD`. Il doit être devenu un enfant d'une manette. Sinon, vérifie que le XR Origin a bien le composant `Player Rig`.
