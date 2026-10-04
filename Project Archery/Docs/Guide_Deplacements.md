# Guide : la marche, la course aux bras et le slide

Ce guide ajoute les déplacements du GDD (section 12) à la scène `Prototype_Tir` :

- **Marcher** : le **joystick gauche** donne la direction, par rapport à ton regard. Joystick en avant, tu avances ; en arrière, tu recules ; sur le côté, tu te décales. La marche va à 2,5 m/s.
- **Courir** : en marchant, **balance les deux bras**, comme en courant. Ta vitesse est multipliée, jusqu'à ×3 (7,5 m/s) en balançant à fond. Il faut bouger les deux bras : tendre la corde ou prendre une flèche, d'une seule main, ne fait pas courir.
- **Glisser (slide)** : pendant la course, appuie sur **A** ou **X**. Tu repars 40 % plus vite (10 m/s au plus) et tu glisses 2,5 s en gardant ta direction, les mains libres pour tirer. Il faut courir à au moins 3,5 m/s.
- Une **vignette de confort** assombrit les bords de la vue pendant le slide. Sa force se règle dans les paramètres.
- Le **saut** du XR Origin (bouton A) est désactivé, puisque A sert au slide.

La direction suit le regard, lissée : elle ne tangue pas quand on balance les bras ou la tête. En pleine course, un demi-tour freine d'abord avant de repartir.

Le déplacement passe par le système d'XRI : on ne traverse pas les murs, et on tombe si on court dans le vide. C'est pratique pour descendre de la tour. Le script **remplace le déplacement au joystick d'XRI** (`Move`), qu'il coupe tout seul tant qu'il est actif.

## Les scripts

| Script | Rôle | Où le mettre |
|---|---|---|
| `Arm Swing Locomotion` | Marche au joystick, course aux bras, slide, vignette de confort | Objet `Arm Swing` dans `XR Origin (XR Rig) > Locomotion` |
| `Player Rig` (déjà là) | Désactive le saut (*Disable Jump*, coché par défaut) | XR Origin |
| `Settings Panel` (déjà là) | Curseur « Confort » | Page `Settings` du menu |

Les sons sont dans `Audio/Placeholder` : `slide` et `footstep` (les pas).

> **Tu avais déjà monté la première version ?** Il n'y a rien à refaire : garde ton objet `Arm Swing`. Les nouveaux réglages (marche, course, slide plus long et plus rapide) s'appliquent tout seuls. La gâchette n'est plus nécessaire pour courir.

## 1. La course aux bras

1. Dans la Hierarchy, déplie `XR Origin (XR Rig)`, puis `Locomotion`. Tu y trouves `Move`, `Turn`, `Gravity`…
2. Clic droit sur `Locomotion` > *Create Empty*, nomme-le `Arm Swing`.
3. Sur `Arm Swing` : *Add Component > Arm Swing Locomotion* :
   - *Slide Clip* : `slide` ;
   - *Step Clip* : `footstep`.

Le reste est déjà réglé :
- *Mediator* se remplit tout seul en jeu, avec celui de `Locomotion` ;
- le joystick gauche, A et X sont déjà liés : ce sont les champs *Move Input*, *Left Slide Input* et *Right Slide Input*.

Ajouter un objet dans le XR Origin crée une modification de prefab dans la scène. C'est normal : n'applique rien au prefab de l'XRI. Ne désactive pas `Move` toi-même : `Arm Swing` le coupe en jeu, et le rallume si on le désactive.

## 2. La vignette de confort

1. Dans le Project, ouvre `Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/TunnelingVignette`.
2. Glisse le prefab `TunnelingVignette` sur `XR Origin (XR Rig) > Camera Offset > Main Camera` : il devient un enfant de la caméra. Position et rotation `(0, 0, 0)`.
3. Laisse sa liste *Locomotion Vignette Providers* vide : c'est `Arm Swing Locomotion` qui allume la vignette pendant le slide. Il la trouve tout seul sous le XR Origin.

Option : pour une vignette aussi pendant la course, coche *Vignette While Running* sur `Arm Swing`.

## 3. Le réglage dans les paramètres

Dans le menu principal, page `Settings` :

1. Sélectionne la ligne du volume des effets et duplique-la (Ctrl+D). Garde la copie juste en dessous et renomme-la `Vignette Row`.
2. Dans la copie, change le texte de `Label` en « Confort ».
3. Sélectionne la page `Settings`. Dans `Settings Panel` :
   - *Vignette Slider* : le `Slider` de `Vignette Row` ;
   - *Vignette Value* : son texte `Value`.

Le curseur va de « Aucune » à 100 % (50 % par défaut). Le réglage est enregistré dans `settings.json` avec les autres.

## 4. Tester

La course se teste **dans le casque** : il faut vraiment balancer les bras.

1. Lance Play et descends de la tour (le cercle du bas, ou marche dans le vide).
2. Pousse le joystick gauche : tu marches. Tire-le en arrière : tu recules, en regardant toujours devant toi.
3. Joystick poussé, balance les deux bras : tu accélères nettement, avec des pas plus espacés. Arrête de balancer : tu reviens à la marche. Lâche le joystick : tu t'arrêtes.
4. En courant, appuie sur A ou X : tu glisses 2,5 s, avec un « chhh », une petite vibration et les bords de la vue qui s'assombrissent. Tire pendant le slide.
5. En marchant, tends la corde de l'arc : tu ne dois pas accélérer.
6. Menu > *Paramètres* : mets « Confort » sur « Aucune », puis refais un slide : il n'y a plus de vignette.

## Les réglages d'Arm Swing Locomotion

| Réglage | Par défaut | Effet |
|---|---|---|
| *Walk Speed* | 2,5 m/s | vitesse de marche, joystick à fond |
| *Stick Dead Zone* | 0,15 | inclinaison du joystick ignorée |
| *Direction Smoothing* | 0,2 s | lissage de la direction du regard (0 = aucun) |
| *Max Run Multiplier* | 3 | vitesse de marche × 3 en balançant les bras à fond |
| *Hand Speed Dead Zone* | 0,35 m/s | en dessous, les bras ne comptent pas |
| *Full Swing Hand Speed* | 2,2 m/s | vitesse des mains qui donne la course la plus rapide |
| *Acceleration Rate* / *Braking Rate* | 12 / 16 m/s² | réactivité au départ et à l'arrêt |
| *Require Trigger* | non | coché : on ne court qu'en tenant une gâchette |
| *Slide Minimum Speed* | 3,5 m/s | vitesse minimale pour glisser |
| *Slide Time* | 2,5 s | durée du slide |
| *Slide Speed Boost* | 1,4 | vitesse au début du slide, par rapport à la course |
| *Max Slide Speed* | 10 m/s | vitesse maximale au début du slide |
| *Slide Cooldown* | 0,4 s | délai avant le slide suivant |
| *Step Length* | 1,4 m | distance entre deux pas ; les foulées s'allongent avec la vitesse |
| *Strongest Aperture* | 0,4 | ouverture de la vignette quand « Confort » est à 100 % (1 = pas de vignette) |

## En cas de problème

- **Rien ne bouge** :
  - `Arm Swing` doit être dans `Locomotion`, pour trouver le *Locomotion Mediator* ;
  - il faut un `Player Rig` sur le XR Origin (sinon, un avertissement s'affiche dans la console).
- **On va deux fois trop vite au joystick** : *Replace Joystick Move* doit être coché, pour couper le `Move` d'XRI.
- **Les bras ne font pas accélérer** : il faut pousser le joystick et balancer les deux bras franchement. Baisse *Full Swing Hand Speed* pour atteindre plus facilement la vitesse maximale.
- **A fait sauter au lieu de glisser** : *Disable Jump* doit être coché sur le `Player Rig`.
- **La vignette n'apparaît pas** :
  - le prefab `TunnelingVignette` doit être sous la `Main Camera` ;
  - le réglage « Confort » ne doit pas être sur « Aucune ».
- **Le slide ne part pas** : il faut courir à au moins 3,5 m/s (*Slide Minimum Speed*), et ne pas être en train de tomber.
