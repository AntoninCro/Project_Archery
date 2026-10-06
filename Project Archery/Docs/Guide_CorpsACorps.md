# Guide : le coup de flèche au corps à corps

Ce guide présente le coup au corps à corps du GDD (section 4.5). **Il n'y a rien à monter** : tout est déjà dans le script de la flèche, et marche dès que tu ouvres le projet.

> **Où en est ce guide** (6 octobre) : rien à monter, c'est vérifié : le prefab `Arrow` garde les réglages par défaut ci-dessous. Il te reste à **tester** (section « Tester »).

- Tiens une flèche en main (pas encochée) et **frappe un ennemi** avec sa pointe, d'un geste franc (plus de 2 m/s).
- Il subit les **dégâts d'un tir orange** de ton arc, bonus de dégâts compris. Un coup à la tête compte double, comme une flèche.
- La flèche **reste plantée** dans l'ennemi et quitte ta main : il faut en reprendre une dans ton dos.
- Le coup compte comme une touche : points, combo, vampirisme.
- **Aucune flèche spéciale** ne s'y ajoute (multitir, écho, foudre…) : le coup est surtout utile en début de partie.
- Exception : une flèche **enflammée** au brasero fait brûler l'ennemi, comme en tir.

Un geste lent (encocher, ranger la flèche) ne frappe pas.

## Les réglages

Sur le prefab `Arrow`, partie *Coup au corps à corps* :

| Réglage | Par défaut | Effet |
|---|---|---|
| *Melee Enabled* | coché | décoche-le pour couper le coup au corps à corps |
| *Melee Min Speed* | 2 m/s | vitesse minimale de la pointe pour que le coup porte |
| *Melee Grade* | `Good` | le coup fait les dégâts d'un tir de cette qualité (`Perfect` : comme un tir parfait) |

## Tester

1. Lance Play et descends de la tour.
2. Prends une flèche dans ton dos. Laisse un chevalier approcher, et frappe-le d'un coup sec avec la pointe :
   - un chiffre de dégâts s'affiche, avec le bruit d'impact ;
   - la flèche reste plantée dans le chevalier, et ta main est vide.
3. Frappe-le à la tête : les dégâts doublent et le « ding » sonne.
4. Approche doucement une flèche d'un chevalier : rien ne se passe, il faut frapper.

## En cas de problème

- **Le coup ne porte jamais** : frappe plus vite, ou baisse *Melee Min Speed* à `1.5`.
- **Je frappe sans le vouloir** : monte *Melee Min Speed* à `3`.
