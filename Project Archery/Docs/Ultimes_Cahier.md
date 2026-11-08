# Le cahier des ultimes : ce qui rend une ultime bonne

Écrit le 2 novembre, tard, quand le joueur a validé le Colosse de forge (« ok, la tour me paraît bien »), après quatre essais et
cinq passes (`Refonte_TowerDefense.md`, 12.46 à 12.50). **À lire avant de concevoir ou de coder le Grand Manège et la Grande
Horloge** : tout ce que le joueur a demandé pour le Colosse vaut pour elles **dès le premier jet**. Il ne doit pas avoir à le
redire.

## 1. Les règles (12.44, rappel)

- L'ultime est le **niveau 4 d'un trio au niveau 3** : 10 plans de plus (environ 25 dans la même lignée), posés **entre les vagues** ;
  le trio se transforme et l'ultime se bâtit au marteau. **Une seule par partie**, une par famille :
  - forge et poudre : Enclume, Grande bombarde → **le Colosse de forge** (fait) ;
  - fête foraine : Montagnes russes, Gonfleur, Colisée → **le Grand Manège** (codé le 2 novembre, tard, d'après ce cahier : 12.51 et
    partie 7 ci-dessous ; pas encore essayé) ;
  - glace et temps : Bowling, Beffroi → **la Grande Horloge**, qui reprend **Minuit**, l'arrêt du temps du Beffroi (12.44, 12.45) ;
    codée le 2 novembre, tard, d'après ce cahier (12.52 et partie 8 ci-dessous ; pas encore essayée).
- Elle grandit (environ deux fois un trio), a **une action avec la flèche du joueur**, **suit le joueur dans l'arène de MK**.
- Le système existe : `Building/TowerUltimate.cs` (hérite de `TowerTrio`), le catalogue (`TowerDuoCatalog`, « ultimates » du
  fichier d'allure, `trios` : les trios dont elle naît), `BuildManager.BuildUltimate`, la vitrine **F4** (après les trios, sur la
  voie du centre), `FinalBoss/ArenaTurrets` (elle descend dans l'arène). Une nouvelle ultime : une entrée dans
  `Tools/allure_tours/ultimes.py` et une classe qui hérite de `TowerUltimate`.

## 2. Ce que le joueur a demandé, essai après essai (à faire dès le départ)

1. **Un modèle fini** (12.47 : « plus texturé, plus lumineux, avec plus de particules ») : grand et imposant, des accents dorés
   (« il doit avoir de la gueule », règle des fusions) ; une **texture** à lui (le fer martelé : `Tools/texture_fer_martele.py`,
   posé par `Editor/ColosseFinitionBaker`), des **parties qui luisent** (les veines de fonte : le shader `Archery/MoltenIron`, leur
   éclat qui palpite et s'emballe aux grands moments, par `MaterialPropertyBlock`), des **particules qui ne s'arrêtent pas** (braises,
   fumée, étincelles, gouttes), **une lumière** à lui (la fournaise). Il bouge : des pièces articulées (bras, buste qui se penche).
2. **Une apparition** (12.47 : « une animation de spawn ») : bâtie, elle ne surgit pas d'un coup. Le Colosse sort de terre (14 m,
   2,4 s) dans la roche qui vole, s'allume, appelle la foudre et abat son marteau : l'onde renverse tout à 22 m (« LE COLOSSE DE
   FORGE ! »). Le sol tremble (les manettes vibrent).
3. **Le ciel qui change, mais passager** (12.47, puis 12.48 : « cela doit être temporaire ») : un ciel à elle
   (`World/SkySettings`, un réglage comme `Forge()`, posé par-dessus celui de la partie : `SkyController.SetOverlay`) **seulement
   pendant l'apparition et son grand moment** (pour le Colosse : la coulée), puis retour au ciel de la partie en quelques secondes.
   Jamais tant qu'elle est là. Rien dans l'arène de MK.
4. **Chaque coup est un événement** (12.47 : « à chaque coup de marteau, un éclair qui tombe ; on doit ressentir sa puissance ») :
   pour le Colosse, la foudre (`SkyController.Strike` éclaire le ciel). Pour les suivantes, la même idée dans leur thème : un signe
   fort, visible de loin et sonore, à chaque coup.
5. **Le rythme et le nombre** (12.48 : « il n'attaque qu'une fois tous les certains temps, on ne ressent pas le nombre ») : **pas
   une attaque toutes les N secondes**. Ce qui a marché, trois couches :
   - **une action seule et continue** (la forge : un coup de masse toutes les 1,2 s, qui lance un éclat sur un ennemi) ;
   - **un grand coup régulier**, qu'on voit venir (au 5e coup, le lingot chauffé à blanc, la foudre) ;
   - **une action à chaque flèche du joueur qui touche** (un marteau par flèche, l'idée du joueur), qui va avec la pluie de flèches
     et le multishot, **plafonnée à 5 par seconde** pour le casque (le chiffre du joueur ; les flèches en trop attendent, 8 au plus,
     2 s au plus). **Elle frappe selon la force de sa flèche** (3 novembre, le joueur : « scale en fonction de si le joueur fait un
     parfait, moyen, etc. », et la pluie réduit) : `TowerUltimate.ShotPower(hit)`, ses dégâts et sa part des PV d'un boss ; file
     pleine, la plus faible laisse sa place.
6. **Des coups imposants** (12.49 : « bien plus imposants, comme la précédente version ») : ce qu'elle lance est **à l'échelle du
   modèle** (le joueur a refusé les marteaux réduits à 55 %), et chaque coup a tout : le signe du coup (la foudre), l'onde de choc,
   l'explosion, la poussière en anneau, les ennemis projetés en vrille ; le marteau reste planté un instant, puis s'enfonce.
7. **La force, vérifiée au tableau** (12.49 : « les dégâts actuels sont vraiment faibles ; compare avec les autres trios du
   Excel ») : la mettre dans `Tools/equilibrage_tours.py` **avant** de la donner au joueur (comme `colosse()`, dans la feuille
   « Trios »), et viser, avec la méthode du tableau (Normal, sans bonus, le groupe de 5 ennemis serrés, dégâts de base par
   seconde) :
   - **seule, sans le joueur** : un trio moyen au niveau 3 (environ 110 sur une cible, 400 à 500 sur le groupe) ;
   - **avec une flèche qui touche par seconde** : au-dessus du meilleur trio au niveau 3 (le Colosse : 270 et 1 158) ;
   - **avec deux** : environ **deux fois** le meilleur trio (la règle de 12.44 ; le Colosse : 430 et 1 891).

   Les repères au niveau 3 (2 novembre) : Enclume 169 / 884, Grande bombarde 246 / 737, Bowling 50 / 484, Colisée 0 / 390,
   Montagnes russes 30 / 209, Gonfleur 6 / 120 (sur une cible / sur le groupe).
8. **Les boss** (12.50 : « les dégâts de la tour devraient aussi marcher sur les boss » ; « pas MK, on garde son effet actuel ») :
   un **boss des vagues** prend les mêmes dégâts que les autres, **plus** une part de ses PV max (12.44), et n'est jamais projeté.
   **MK** (l'arène) : une part de ses PV seulement, et l'effet à elle (le Colosse le plaque au sol : `DragonBoss.Pin`, 3 s, au plus
   toutes les 12 s).
9. **Les sons** : demander au joueur, dès le début, les sons qu'il faut (freesound, CC0 ; il les télécharge dans `Audio/`, Claude
   les découpe en Python et les nomme) ; les créditer (`Credits_Licences.md`, le README). Les packs de Kenney sont déjà là
   (`Assets/kenney_impact-sounds` : le fer qui sonne de la forge, `impactBell_heavy_001`).

## 3. Les pièges techniques (Claude)

- **Le baker de l'allure garde les réglages d'un prefab déjà fait** : il refait le modèle à chaque nouvelle marque, mais une valeur
  par défaut changée dans le code n'atteint pas le prefab (les hausses de 12.47 ne sont jamais arrivées : le joueur a essayé les
  chiffres de 12.46). Écrire les bons chiffres **avant le premier passage dans Unity** ; après, passer par un baker de valeurs, une
  fois, avec une garde (modèle : `Editor/ColosseForceBaker`). Un champ **nouveau** prend, lui, sa valeur du code.
- **Les sons importés après le prefab** restent vides : un baker de sons (modèle : `Editor/ColosseSonsBaker`, avec son
  `AssetPostprocessor`).
- **Le casque** (jusqu'à 5 coups par seconde, la pluie de flèches) :
  - ce qu'elle lance est **réutilisé** (la liste `Shot` du Colosse : pris, lancé, rendu) ;
  - les étincelles et la poussière des coups viennent de **systèmes de particules à elle**, déplacés là où ça frappe (`Emit`) ;
    `TowerDuo.Burst` crée un objet à chaque appel : seulement pour les coups rares ;
  - `LightningBolt` et `ExplosionFx` sont déjà réutilisés (96 éclairs à la fois au plus) ; une lumière (`FlashAt`) est un objet
    neuf : seulement pour les grands coups ;
  - le **coup complet** (lumière, tonnerre, roche, sol fendu, manettes) au plus toutes les 0,5 s (*Heavy Interval*), **l'éclair dans
    le ciel** au plus toutes les 1,2 s (*Sky Flash Interval* : pas de ciel qui clignote) ; `Sfx` joue 6 sons au plus par image.
- **Le modèle** (`Tools/allure_tours/ultimes.py`) : calculer en Python la pose d'une pièce qui doit en toucher une autre (la masse
  sur le lingot : `co_forge`) ; une pièce qui tourne casse ce contact (le buste du Colosse ne se tourne plus, seul le bras du
  marteau se tourne, à l'épaule) ; des noms uniques entre frères (« Bras » et « Bras gauche ») ; vérifier avec
  `faces_superposees.py` et `apercu.py` (« Duo:<nom> ») avant Unity. Une nouvelle marque (`mark`, avec `rebuild`) refait le modèle.
- **Les textes** : `descriptionBefore` et `levelsBefore` dans l'entrée font remplacer les textes de la carte et du niveau, s'ils
  n'ont pas été changés à la main.
- **L'arène** : `ArenaScale` (le Colosse à 0,65 : la voûte est basse), `EnterArena` / `LeaveArena` (y détruire ce qui est réutilisé :
  sa taille change), le message d'arrivée (« Touche MK : … »).
- **Pas deux champs du même nom dans la lignée** (`Structure` → `TowerDuo` → `TowerTrio` → `TowerUltimate` → l'ultime), même privés :
  Unity les sérialise au rechargement des scripts et refuse (« The same field name is serialized multiple times », vu le 3 novembre :
  `m_ArenaScale`, `m_BoostUntil`) ; le C# l'accepte. Renommer celui qui ne porte pas de réglage ; `py Tools/champs_en_double.py`.
- Vérifier : `py Tools/compile_check.py`, `py Tools/compile_check_editor.py` et `py Tools/champs_en_double.py` ; rien n'est fini
  avant l'essai au casque.

## 4. Les fichiers du Colosse, à reprendre comme modèle

- `Scripts/Building/UltimeColosse.cs` : l'éveil, la forge, les marteaux sur les flèches, la coulée, le ciel, les effets, l'arène.
- `Tools/allure_tours/ultimes.py` : le modèle, ses champs, ses sons, ses textes.
- `Scripts/FinalBoss/DragonBoss.Pin.cs` : MK plaqué au sol (l'état `Pinned`).
- `Shaders/MoltenIron.shader` et `Art/TowerDefense/Fonte_en_fusion.mat` : la matière qui luit.
- `Tools/texture_fer_martele.py`, `Editor/ColosseFinitionBaker` : la texture et la finition des matériaux.
- `Editor/ColosseSonsBaker`, `Editor/ColosseForceBaker` : les sons et les chiffres posés après coup.
- `World/SkySettings.Forge`, `World/SkyController` (`SetOverlay`, `Strike`) : le ciel passager.
- `Tools/equilibrage_tours.py`, `colosse()` : la force au tableau.

## 5. Le Colosse, en cinq passes

- **12.46** : le système des ultimes et le Colosse (le marteau où tombe ta flèche, la coulée, MK plaqué au sol).
- **12.47**, premier essai (« son attaque est loin d'être impressionnante… on doit ressentir sa puissance ») : l'éveil, le ciel
  de forge, un éclair à chaque coup, l'impact, la rivière de fonte, le modèle fini, les sons.
- **12.48**, deuxième essai (« il n'attaque qu'une fois tous les certains temps, on ne ressent pas le nombre » ; le ciel doit être
  temporaire) : à deux mains, la forge à gauche, un marteau par flèche à droite (5 par seconde au plus) ; le ciel passager.
- **12.49**, troisième essai (« bien plus imposants, comme la précédente version, et garder les mêmes dégâts ; compare avec les
  trios ») : les marteaux à sa taille et complets, 160 dans 6 m ; la forge renforcée ; le Colosse au tableau.
- **12.50** : les dégâts sur les boss des vagues ; MK inchangé. Puis : « ok, la tour me paraît bien ».

## 6. Pour les deux suivantes

Les idées gardées en 12.44, à reprendre avec tout ce qui précède (et à faire choisir au joueur avant de coder ce qui n'est pas
décidé) :
- **Le Grand Manège** (fête foraine) : des chevaux de bois géants ; les ennemis tournent et sont éjectés ; ta flèche l'emballe ;
  la charge : tous les chevaux descendent la voie au galop ; ils chargent MK. **Fait** (partie 7).
- **La Grande Horloge** (glace et temps) : de glace ; à chaque heure, les ennemis du cadran rembobinés et givrés ; ta flèche arrête
  le temps sur une zone ; **Minuit** (en sommeil dans `Building/TrioBeffroi.cs`, partie « Minuit », avec les réglages du joueur
  sur le prefab du Beffroi : la bulle sur toute la carte, le noir et blanc, les flèches arrêtées, « Za Warudo ! Temps, arrête-toi ! »,
  9 s toutes les 20 s ; il ne joue qu'au niveau 4, que rien n'atteint encore : 12.45) ; MK figé en vol.

Pour chacune, avant de la donner au joueur : les trois couches du rythme (seule et continue ; un grand coup régulier ; une action
par flèche qui touche, plafonnée à 5 par seconde), des coups à l'échelle du modèle avec leur signe, l'apparition, le ciel passager,
le modèle fini (texture, ce qui luit, particules, lumière), la force au tableau (seule : un trio moyen ; deux flèches par seconde :
deux fois le meilleur trio), les boss des vagues blessés et MK avec son effet à elle, les sons demandés au joueur, le casque.

## 7. Le Grand Manège, fait d'après ce cahier (2 novembre, tard ; 12.51)

Le joueur a choisi, avant le code (quatre questions, deux à quatre propositions chacune) : la **ronde** pour l'action seule (le ruban
attrape un ennemi toutes les 1,2 s et le fait tourner sur le manège), la **grande éjection** pour le grand coup (au 5e coup, tous ceux
qui tournent sont éjectés sur le groupe le plus serré ; il n'a pas pris le cheval d'or conseillé), le **feu d'artifice** pour le signe
de chaque coup, **MK étourdi** (des étoiles et des petits chevaux autour de la tête). Le reste suit le cahier : un cheval par flèche qui
touche (la copie d'un des siens, aussi grande ; 160 dans 6 m, les marteaux validés du Colosse), la charge toutes les 25 s, l'éveil (il
sort de terre en tournant, les fusées, l'onde de confettis), le ciel de fête passager (`SkySettings.Fete`), le bois peint et verni
(`Tools/texture_bois_peint.py`, `Editor/ManegeFinitionBaker`), les ampoules en chenille, les confettis, les paillettes et la vapeur, la
force au tableau avant le premier essai (seul 87 / 484, avec deux flèches 407 / 1 951), les sons demandés dès le début
(`Editor/ManegeSonsBaker`, en attendant ceux du joueur).

Ce que Claude a appris en le faisant, pour la Grande Horloge :
- **Un modèle de centaines de pièces** (le Manège en a 940) : les fondre au lancement (`UltimeManege.FuseModel`, avant le plan bleu :
  un maillage par matériau ; ce qui bouge seul à part ; ce qui s'anime par groupe, comme les ampoules, dans son maillage à lui).
- **Ce qu'il lance, copié d'une pièce du modèle** : la copie fondue en un maillage, faite une fois par pièce, réutilisée.
- **Ce qui luit sans le matériau de la fonte** : quelques copies du matériau, animées en groupe (pas un `MaterialPropertyBlock` par
  pièce : il y en a des centaines).
- **Un cône rayé en formes simples** : un pan est un trapèze ; un rectangle trop large dépasse au-dessus du pan voisin près de la pointe ;
  le couper en morceaux à sa largeur, un sur deux un peu décollé, une nervure sur la jointure (`ultimes.py`, `ma_roof`).
- **Les fins de ligne** : `grep $'\r'` de Git Bash ne voit pas les CRLF ; vérifier en Python (`open(p, 'rb')`).

## 8. La Grande Horloge, faite d'après ce cahier (2 novembre, tard ; 12.52)

Le joueur a choisi en deux séries de questions (comme pour le Manège) : les **chiffres** pour l'action seule (un chiffre romain de glace
par tic, 1,2 s), **Minuit après le XII** pour le grand coup (sa réponse à lui : « on reprend l'ancien Beffroi avec son Za Warudo qui
dure 9 secondes, une fois que les 12 sont passés »), **l'écho du temps** pour sa flèche (rejouée 1 s plus tard par une flèche de glace
géante), **l'aurore** pour le signe de chaque coup, **l'horloge astronomique** pour le modèle, **le dégel qui brise** pour la fin de
Minuit, **Minuit dans l'arène** pour MK. Les douze chiffres font voir venir Minuit : le grand coup régulier, c'est le XII.

Ce que Claude a appris en la faisant :
- **Un grand moment pris à un bâtiment existant** (Minuit, du Beffroi) : le recopier dans l'ultime avec les réglages que le joueur y
  avait mis (le prefab du Beffroi), plutôt que de partager le code ; le Beffroi garde le sien en sommeil.
- **Un cadran qu'on doit lire** : il se tourne vers le joueur (`UltimeHorloge.FaceViewer`) ; les lettres d'un chiffre romain se
  posent de gauche à droite pour qui regarde de face (vers −x du cadran) ; vérifier avec `apercu.py`, vue « dos » (la face avant).
- **Le temps arrêté pour MK** : un état de plus n'est pas nécessaire ; `DragonBoss.StopTime` saute son `Tick` et fige son animation
  (et ses boules de feu, `DragonFireball.TimeStopped`), puis il reprend où il en était.
- **Un effet sur toute la carte** (le dégel) pèse plus que ce que le tableau compte (le groupe de 5) : le dire, et le surveiller.
- **Le temps arrêté s'entend aussi** (le joueur, 3 novembre, après son essai : « enlève les autres sons et musique, laisse juste les
  bruits de l'horloge et du joueur ») : `Sfx.Hush` à chaque image du grand moment, `Sfx.Unhush` à la fin ; les sons de l'ultime par
  `Sfx.PlayThroughHush`. À prévoir dès le premier jet pour un grand moment qui fige le monde.
- **Un son donné en modèle** (le « Za Warudo » de JoJo, 3 novembre) : un extrait sous droits ne va pas dans le jeu. Le lire
  (`Tools/sons/mp3_en_wav.ps1` pour un MP3, puis les outils du dossier), en tirer le dessin (les temps, les hauteurs, le grain, la
  coupure), et le synthétiser (`Tools/sons/arret_du_temps.py`, à reprendre : calculer à 4 fois le taux sous une saturation) ; vérifier
  que le profil du son fait suit celui du modèle.
