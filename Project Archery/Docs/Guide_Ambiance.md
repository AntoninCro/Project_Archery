# Guide : l'ambiance sonore et les musiques

Ce guide ajoute l'ambiance et les musiques du GDD (section 16) à la scène `Prototype_Tir`.

- **Ambiance** : oiseaux et vent le jour, grillons, chouettes et vent léger la nuit. Elle suit le ciel de la difficulté : en choisissant Difficile dans le menu, les oiseaux laissent place aux grillons en fondu.
- **Musiques** :
  - au menu, avant la première vague (celle de la boutique si tu n'en mets pas d'autre) ;
  - pendant les pauses, boutique ouverte ;
  - pendant les vagues ;
  - tant qu'un boss est en vie ;
  - plus rien après la mort.
- On passe d'une boucle à l'autre **en fondu** (1,5 s).
- La musique suit le curseur **Musique** des paramètres ; l'ambiance, celui des **Effets**.

Les boucles fournies sont **provisoires** : générées par un script, elles servent à tester. Remplace-les en fin de projet (voir la dernière section).

## Le script

| Script | Rôle | Où le mettre |
|---|---|---|
| `Ambience Manager` | Choisit l'ambiance et la musique du moment, et passe de l'une à l'autre en fondu | Nouvel objet `Ambience` |

Les boucles sont dans `Audio/Music` : `ambience_day`, `ambience_night`, `music_shop`, `music_wave` et `music_boss`.

## 1. Les réglages d'import

1. Dans `Audio/Music`, sélectionne les 5 fichiers d'un coup.
2. Dans l'Inspector :
   - *Load Type* `Decompress On Load` : la boucle repart sans petit blanc ;
   - *Compression Format* `Vorbis`, *Quality* `70` ;
3. *Apply*.

Les fichiers sont déjà en mono, à 22 kHz : ils prennent peu de mémoire.

## 2. L'objet Ambience

1. *Create Empty*, nomme-le `Ambience`.
2. *Add Component > Ambience Manager* :
   - *Day Ambience* `ambience_day`, *Night Ambience* `ambience_night` ;
   - *Shop Music* `music_shop`, *Wave Music* `music_wave`, *Boss Music* `music_boss` ;
   - laisse *Menu Music* vide : le menu reprend la musique de la boutique ;
   - *Music Group* : déplie l'asset `Audio/MainMixer` dans le Project et glisse son groupe `Music` ;
   - laisse *Ambience Group* vide : l'ambiance passe par le groupe des effets, celui de `Game Settings`.

Les sources sont créées en jeu, sous `Ambience` : il n'y a pas d'*Audio Source* à ajouter.

## 3. Tester

1. Lance Play : la musique calme et les oiseaux montent doucement.
2. Dans le menu, passe la difficulté à *Difficile* (ou touche **3**) : le ciel passe à la nuit, et les grillons remplacent les oiseaux.
3. Tire dans le gong : la musique de vague arrive en fondu.
4. **N** pour finir la vague : retour à la musique calme pendant la pause.
5. Enchaîne **N** jusqu'à la vague 5 : à l'arrivée du boss, la musique devient plus lourde ; elle revient à la musique de vague à sa mort.
6. Menu > *Paramètres* : le curseur *Musique* change le volume de la musique ; *Effets*, celui de l'ambiance.
7. **K** pour mourir : la musique s'éteint.

## 4. Les remplacer en fin de projet

Il suffit de glisser d'autres boucles dans les mêmes champs. Quelques sources gratuites :
- **ambiances** : freesound.org, avec le filtre de licence *Creative Commons 0* (« forest birds loop », « night crickets ») ;
- **musiques** : opengameart.org (filtre CC0), ou les packs gratuits d'itch.io. Cherche des pistes « loop », qui bouclent sans coupure.

Vérifie la licence de chaque fichier, et note les auteurs dans les crédits du README si elle le demande (CC-BY).

Le *Volume* de chaque partie se règle sur `Ambience Manager` (*Ambience Volume*, *Music Volume*), en plus des curseurs des paramètres.

## En cas de problème

- **Pas de musique** :
  - les champs de musique doivent être remplis ;
  - le curseur *Musique* des paramètres ne doit pas être à 0 ;
  - le groupe `Music` du mixer ne doit pas être coupé.
- **La musique ne suit pas le curseur Musique** : *Music Group* doit être le groupe `Music` du mixer.
- **Un petit blanc à chaque tour de boucle** : *Load Type* doit être `Decompress On Load` (section 1).
- **Les oiseaux continuent la nuit** : la difficulté doit avoir *Night* coché dans son ciel (c'est le cas de Difficile et d'Impossible).
