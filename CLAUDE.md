# THUNK VR — consignes pour Claude

Jeu de tir à l'arc en VR (Meta Quest 3, par Quest Link) pour le cours ST2OOS. Unity **6000.5.10f1**, URP, XR Interaction
Toolkit 3.5.1, OpenXR, Input System. Le projet Unity est dans `Project Archery/` ; le code dans `Assets/_Project/Scripts`
(un dossier par thème), les outils d'éditeur (« bakers ») dans `Assets/_Project/Editor`, les réglages de jeu dans
`Assets/_Project/Data`, la scène principale `Assets/_Project/Scenes/Prototype_Tir.unity`.

**À lire en premier** : `Project Archery/Docs/Reprise_Projet.md` (où en est le projet, le chantier en cours, la suite), puis
`Project Archery/Docs/GDD.md` (la référence : règles, chiffres, historique daté, planning).

## Reprendre une session

- Le joueur alterne entre **deux comptes Claude** sur ce dossier : une session n'a pas vu la conversation de la précédente.
  Avant d'agir : `Reprise_Projet.md` (partie 1, « Le chantier en cours »), `git status` et `git log --oneline -5`. Beaucoup
  de travail reste souvent **non commité** : ne rien annuler ni « nettoyer » sans demander (fichiers supprimés exprès,
  `Core/PerfProbe.cs`…).
- **Deuxième chantier** (27 et 28 octobre) : la refonte du tower defense (caisses de l'ingénieur, plans, grille, pose à la
  montre, ennemis projetés, puis constructions nouvelles et fusions) ; son état et l'étape suivante sont dans
  `Project Archery/Docs/Refonte_TowerDefense.md`, partie « Avancement ». Raccourcis : **B** (2 plans de chaque bâtiment),
  **V** (un coffre ; le coffre unique depuis le 2 novembre), **F2** (le menu des plans sans casque), **F3** (projeter les ennemis proches), **F4** (la vitrine : chaque
  tour, puis chaque duo, puis chaque trio, posée à plusieurs niveaux ; `Building/BuildShowcase.cs`). Les trios (1er novembre) :
  un duo plus une tour, `Building/TowerTrio.cs`, modèles dans `Tools/allure_tours/trios.py` (partie « trios » du fichier d'allure).
  **Le tableau d'équilibrage (2 novembre)** : `Docs/Equilibrage_Tours.xlsx`, écrit par `Tools/equilibrage_tours.py` d'après les
  prefabs (les 10 tours, les 13 duos, les 7 trios ; `Refonte_TowerDefense.md`, 12.32 et 12.33). Le joueur a rempli sa colonne : **tout
  est appliqué** (12.35, `Editor/BalanceBaker` et le code), pas encore vu dans Unity. Puis les parchemins du joueur valent aussi pour les tours, qui ne suivent plus les vagues
  (12.36). Puis le coffre unique (12.38) et la puissance des tours baissée (12.39). Puis le premier essai du joueur et ses suites
  (12.40, 12.41 : coffres au gong d'un seul côté, le chantier de la partie, un plan par niveau, la grille, la régénération, MK ×2).
  Puis le deuxième essai (12.42, 2 novembre : 2 plans par coffre, courbe des PV relevée, tours ×1,2, vagues plus courtes, la grille ;
  `Editor/SecondTestBaker`). **La tâche suivante** : un nouvel essai (`Checklist_Demo.md`, « Le sixième essai » : le Phare,
  MK, l'Adresse, plusieurs niveaux d'un coup, la sélection par le bâtiment ; `Refonte_TowerDefense.md`, 12.68 ; relancer le tableau
  d'équilibrage après Unity ; puis **plus d'or ni de combo, le choix de la pause** : une carte par ligne parmi trois parchemins,
  trois objets, trois tourelles ; les parchemins à deux effets ; plus de coffre de boss ; 12.69 ; puis **les quatre raretés**
  (épique en violet ; tourelles et parchemins par rareté), le Rappel, MK (paliers, ultimes toutes les 15 s), le Style ; 12.70 ;
  `Docs/Fiche_Ameliorations.xlsx` remplie par le joueur et appliquée : onze pièces épiques, la Pointe du chasseur refaite ; 12.71 ; puis le gong (une carte par ligne), les raretés à 60 / 24 / 11 / 5, le Déluge qui pleut ; 12.72 ; la vague 1 à 30 % d'ennemis en moins, après les parties d'un ami ; 12.73) ; puis les ultimes (choisies : Colosse de forge, Grand Manège, Grande Horloge, qui prend
  Minuit au Beffroi ; 12.44 ; le Beffroi au niveau 3 devient le Phare du passé, 12.45). **Le système des ultimes et le Colosse de
  forge sont codés** (12.46 : `Building/TowerUltimate.cs`, `UltimeColosse.cs`, `FinalBoss/DragonBoss.Pin.cs`, le modèle dans
  `Tools/allure_tours/ultimes.py`) ; le Colosse refait après l'essai du joueur (12.47 : « on doit ressentir sa puissance »),
  puis à deux mains après le deuxième (12.48 : la forge à gauche, un marteau par flèche à droite, 5 par seconde au plus ; le
  ciel de forge passager), puis des marteaux imposants et la force au tableau (12.49 : `Editor/ColosseForceBaker` ; le Colosse
  est dans `Tools/equilibrage_tours.py`, à relancer après Unity), puis les dégâts sur les boss des vagues (12.50, MK inchangé).
  **Le joueur a validé le Colosse** (2 novembre, tard : « ok, la tour me paraît bien »). **Piège** : le baker de l'allure
  refait un modèle à chaque marque mais garde les réglages déjà posés sur le prefab ; changer une valeur par défaut dans le code
  ne l'atteint pas : passer par un baker (la leçon de 12.47). **Le Grand Manège, la deuxième ultime, est codé** (2 novembre, tard ;
  12.51 : la ronde, la grande éjection, un cheval par flèche, la charge, le feu d'artifice, MK étourdi ; `Building/UltimeManege.cs`,
  `FinalBoss/DragonBoss.Daze.cs`, le modèle dans `Tools/allure_tours/ultimes.py`, `Editor/ManegeFinitionBaker`, `ManegeSonsBaker`) ;
  pas encore vu dans Unity ; ses sons (freesound, CC0, téléchargés par le joueur) sont découpés en `Audio/Manege_*.wav` et crédités
  (les outils pour les sons, réutilisables : `Tools/sons/` ; lire un WAV ou un AIFF, l'enveloppe, le spectre, chercher une boucle qui
  se raccorde au rythme, découper ; un MP3 se convertit par le transcodeur de Windows, `mp3_en_wav.ps1` ; synthétiser un son sur un
  modèle, `arret_du_temps.py`) ; le joueur l'a lancé : « parfait ». **La Grande Horloge, la troisième, est codée** (2 novembre,
  tard ; 12.52 : les chiffres, Minuit après le XII et le dégel qui brise, l'écho du temps, l'aurore, l'horloge astronomique, Minuit
  dans l'arène ; `Building/UltimeHorloge.cs`, `FinalBoss/DragonBoss.TimeStop.cs`, `Editor/HorlogeFinitionBaker`, `HorlogeSonsBaker`) ;
  pas encore vue dans Unity ; ses sons (freesound, CC0, téléchargés par le joueur) sont découpés (`Tools/sons/decoupe_horloge.py`) et
  crédités. **Les trois ultimes sont faites ; ensuite, leurs essais.** Le 3 novembre : l'Horloge retouchée (sans ses cristaux,
  modèle refait), et **le chantier du début** : les 3 tours d'un trio tiré au hasard, plus la Barricade (ou un autre si le trio
  l'a déjà), 4 bâtiments (12.53). Puis l'action par flèche des ultimes suit la force du tir (12.54 : `TowerUltimate.ShotPower`,
  parfait ×2, moyen ×0,8, la pluie moins), et Minuit a son silence et son bruit d'arrêt du temps (12.52, 12.54). Puis la flèche « ULTIME »,
  la pose lisible (la portée au sol et la voie couverte, `Structure.ReachAt`), la lumière sous les tourelles de l'arène, la
  fluidité (12.55). Les menaces par voie (le lot 5, 12.56 : `Waves/WaveManager.Lanes.cs`) sont en sommeil depuis le même jour. Le talisman de l'ingénieur surcharge tous les bâtiments (12.57). Tous les bâtiments qui tirent rejoignent l'arène de MK, adaptés à lui (12.58 : `Structure.FightsInArena`, `EnterArena`). La tour attaquée : le cor, le rayon, la fumée, à la place de l'alerte de la montre (12.59 ; `World/Lighting/MapGameplayLights`). Puis le troisième essai (12.60, `Editor/ThirdTestBaker` : volants, boss, PV du joueur ÷3, le combo, le Manège), et le rythme : les coffres gardés, la vague de durée fixe (12.61 ; `Enemies/Enemy.Guard.cs`). La tour à 1 500 PV et les ultimes −30 % (12.62, `Editor/UltimesTowerBaker`). Les plans par coffre de 1 à 4 au hasard (12.63, `BuildManager.RollPlansPerChest`). Le quatrième essai : le Carquois géant limité à 2 flèches par seconde, le journal corrigé (12.64 ; les parties du 3 et du 4 novembre étaient au casque, le journal disait à tort « sans casque »). L'Avalanche n'avale qu'une fois (12.65). Les ennemis au sol 30 % plus rapides, sauf en Facile (12.66, `Editor/GroundSpeedBaker`). La corde des Akimbow, le voile rouge, le joueur à 100 PV et la Vigueur à +12 (12.67, `Editor/PlayerHealth100Baker`). **Pour en refaire ou en changer une : lire d'abord
  `Project Archery/Docs/Ultimes_Cahier.md`**, tout ce que le joueur a demandé pour le Colosse, à faire dès le premier jet (le
  modèle fini, l'apparition, le ciel passager, un signe à chaque coup, le rythme en trois couches dont une action par flèche
  plafonnée à 5 par seconde, des coups à l'échelle du modèle, la force vérifiée au tableau, les boss, les sons, le casque), et ses parties
  7 (ce que le Manège a appris : fondre un modèle de centaines de pièces au lancement) et 8 (l'Horloge).
- **Le chantier en cours** (26 octobre) : la refonte de la partie roguelike, « la flèche en pièces » ; son état et ses étapes
  sont dans `Project Archery/Docs/Refonte_Roguelike.md`, partie « Avancement ».

## Le fil rouge du jeu : la complexité (règle du joueur, 2 novembre)

- **Simple à jouer, riche par les combinaisons.** Un fonctionnement simple qui laisse de grandes possibilités vaut mieux qu'un jeu
  compliqué. Le jeu a eu beaucoup d'ajouts : le but est maintenant de **réduire la complexité**. Ne pas proposer de règle ou de
  système de plus sans dire ce qu'il remplace ou retire ; préférer élargir ce qui existe (les parchemins qui valent aussi pour les
  tours) à une mécanique nouvelle.
- **La bonne complexité arrive au fil de la partie**, une fois que le joueur maîtrise ses outils, et reste dosée. Les modèles : les
  pièces de flèche (chacune simple, la profondeur par les combinaisons) ; les 10 tours de base, puis les duos et les trios.
- **Les bâtiments : contrôler plutôt que tuer** (le joueur, 4 novembre) : les tourelles qui contrôlent les ennemis (projeter,
  immobiliser, regrouper, ralentir) sont plus amusantes que celles qui font des dégâts ; elles préparent les tirs du joueur. Le
  joueur ne trouve pas que les tourelles font trop de dégâts (sauf les ultimes, −30 % le même jour).
- **Ne pas affaiblir les pièces de zone** (le joueur, 4 novembre) : si la monocible est en retard, la renforcer, et renforcer les
  ennemis (`Refonte_TowerDefense.md`, 12.65).
- GDD, section 1 (« Le fil rouge »).

## Façon de travailler

- **Répondre en français.** Textes du jeu, commentaires du code et documentation en français, dans le style du code existant
  (commentaires qui expliquent le pourquoi, avec la date des décisions).
- **Ne jamais modifier `Prototype_Tir.unity` pendant qu'il est ouvert dans Unity** (Unity écraserait la modification à
  l'enregistrement). Passer par un baker d'éditeur (`[InitializeOnLoad]` + `EditorApplication.delayCall`) qui modifie la scène
  ouverte, une seule fois, par `SerializedObject` ; puis l'utilisateur enregistre (Ctrl+S). Les prefabs et assets aussi par un
  baker (`PrefabUtility.LoadPrefabContents`, `SerializedObject`). Exemples : `Editor/GrenadeTuningBaker.cs`, `Editor/KatanaBaker.cs`.
- **Les valeurs de jeu de la scène et de `Data/*.asset` font foi** : le joueur les règle à la main ; le GDD suit, jamais l'inverse.
  Ne jamais lancer les menus « Remplir avec le GDD ». Ne pas réécrire les descriptions des améliorations de la boutique.
- **Le lore** (`Docs/Lore.md`) suit les idées du joueur (partie « Canon ») ; les propositions de Claude ne sont qu'une réserve.
- **Tenir à jour** le GDD (avec la date), `Checklist_Demo.md`, `Demo_Vendredi.md` (raccourcis), et `Credits_Licences.md` + le
  README pour tout nouvel asset (sa licence ; créditer le CC-BY).
- **Développement solo** depuis l'oral de groupe du 23 octobre : le code venu des anciens coéquipiers (`Scripts/BowClasses/*`,
  `Scripts/Grenade/*` et leurs prefabs) se modifie comme le reste, sans le signaler.
- **Git** : commiter ou pousser seulement quand le joueur le demande. Sur le PC actuel (depuis le 1er novembre), le dépôt est
  cloné en HTTPS (`https://github.com/AntoninCro/Project_Archery.git`, connexion par Git Credential Manager) et ça marche
  (pousser depuis un onglet du terminal du joueur, pas depuis l'outil PowerShell de Claude : il ne peut pas ouvrir la connexion
  dans le navigateur, « terminal prompts disabled ») ;
  sur le PC d'avant, Smart App Control bloquait le HTTPS et le dépôt était en SSH.
- Demander avant de supprimer un fichier du joueur et avant tout téléchargement ; ne jamais saisir de mot de passe.
- Vérifier qu'un nom de fichier est libre avant de le créer ; pas d'accents dans les noms de fichiers d'assets. Garder la fin de
  ligne d'un fichier (CRLF ou LF) quand on le modifie.
- **Raccourcis de test** : dans `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, et à ajouter au tableau de `Docs/Demo_Vendredi.md`.

## Les améliorations (refonte roguelike des 25 et 26 octobre)

- `UpgradeEffect` est enregistré **par son numéro** dans les assets : ajouter les nouveaux effets à la fin, sans en retirer ni
  en déplacer (sinon les cartes du catalogue changent d'effet).
- Le catalogue (`Data/Shop/ShopCatalog.asset`) ne change que par un baker qui **ajoute** sans toucher aux réglages du joueur,
  une seule fois, avec une condition de garde (exemples : `Editor/RoguelikeStep3Baker.cs`, `RoguelikeStep5Baker.cs`). Les
  textes des pièces nouvelles sont des propositions ; ceux des cartes existantes ne se réécrivent pas.
- Pour tester : **R** (la pièce nouvelle suivante, équipée), **Tab** (toutes les pièces un niveau de plus : 1, puis 2, puis 3),
  **F** (tout, au niveau 3, une fois), **X** (coffre de boss), **M** (or : retiré le 8 novembre, avec l'or).

## Pièges connus sur ce PC

- **Unity est-il ouvert ?** `Get-Process Unity` en PowerShell (`tasklist` dans Git Bash ne le voit pas). Ouvert, il recompile
  dès qu'un `.cs` change : modifier d'abord les fichiers dont un nouveau script dépend. Il ne réimporte qu'en reprenant le focus.
- **Fins de ligne** : la plupart des scripts et des docs sont en CRLF, quelques-uns en LF (vérifier avant, en Python avec
  `open(p, 'rb')` : `grep $'\r'` de Git Bash répond à tort qu'il n'y en a pas, vu le 2 novembre). Jamais `sed -i` de
  Git Bash sur un fichier CRLF (il le passe en LF) ; l'outil Edit les garde ; en Python, lire et écrire en binaire. Un nouveau
  fichier prend la fin de ligne de ses voisins. Les assets YAML sont en CRLF : écrire le `.meta` avant l'asset (GUID choisi).
- **Git en PowerShell** : rafraîchir le PATH d'abord :
  `$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')`.
- **Le casque** : `adb` est livré avec Unity ; journaux et mesures du Quest : `Docs/Audit_Performance_Quest.md`, partie 8.
- **Le PC actuel (installé le 1er novembre, vérifié)** : le dépôt est dans `C:\Users\anton\Desktop\dev\Thunk VR` (le projet
  Unity dans `Thunk VR\Project Archery`). Unity Hub y a **deux versions** : 6000.5.10f1 (celle du projet) et 6000.6.7f1 ;
  toujours ouvrir avec la 6000.5.10f1 (l'autre convertirait le projet). Python 3.13 (`py` et `python` marchent tous les deux,
  avec numpy, Pillow et python-pptx), SDK .NET 8 et 10, Git et Git LFS (le dépôt n'utilise pas LFS), installés par winget. Les
  deux `compile_check` passent sans erreur ni avertissement. Depuis le 2 novembre : **openpyxl** (pip) et **LibreOffice** (winget,
  `C:\Program Files\LibreOffice\program\soffice.exe`) ; pour recalculer un `.xlsx` et en lire les valeurs :
  `soffice --headless --convert-to xlsx --outdir <dossier> <fichier>` (le `recalc.py` de la compétence Excel ne marche pas sous
  Windows : `AF_UNIX`). Les packs d'assets téléchargés mais pas importés étaient hors du
  dépôt, dans `projet archery/Assets_TowerDefense/` sur l'ancien PC : ils ne sont pas dans Git, ni sur ce PC.

## Ce que Claude a retenu (la mémoire du PC d'avant, reprise ici le 31 octobre)

- **Le joueur** (AntoninCro, cours ST2OOS) écrit en français, souvent sans accents : toujours répondre en français. Il fait le
  montage dans l'éditeur en suivant les guides pas à pas que Claude écrit (`Docs/Guide_*.md`) ; à l'aise moyennement avec Git et
  le terminal : des commandes à copier, une par bloc.
- **Pas de multi-agent** (outil Workflow, sous-agents en éventail) : le 27 octobre, trois essais ont atteint la limite des 5 h
  sans finir. Travailler seul, en lisant seulement le code utile ; proposer la suite plutôt que de grosses fournées spéculatives.
- **« Branche de conversation »** = un embranchement de la conversation avec Claude (le joueur le fait dans l'appli), pas une
  branche Git. Ne créer une branche Git que s'il dit « git », ou après avoir demandé.
- **Les duos (et plus tard les trios, les ultimes)** : un modèle grand et imposant (« il doit avoir de la gueule », plus grand
  qu'une tour normale, des accents dorés), un effet impressionnant dès le niveau 1, et un **niveau 3 vraiment dans la démesure**
  (règle du joueur après l'essai du Boulet fou, 31 octobre).
- **Télécharger des assets** (toujours demander avant) : regarder d'abord dans `Assets/` (des kits Kenney y sont déjà). Poly
  Pizza bloque les téléchargements par un Turnstile : donner les liens au joueur (FBX ; pas de glTFast dans le projet). Quaternius :
  dossiers Google Drive, parfois « Quota exceeded ». KayKit : l'organisation GitHub `KayKit-Game-Assets`. Kenney : répond 403 à
  curl sans User-Agent de navigateur. Ranger les packs hors du dépôt, n'importer que les modèles choisis.

## Vérifier

- Sans Unity (depuis `Project Archery/`) : `py Tools/compile_check.py`
  compile le code du jeu comme un build, puis `py Tools/compile_check_editor.py` compile les outils d'`Assets/_Project/Editor`
  (les bakers) contre lui. Et `py Tools/champs_en_double.py` : un champ qui porte le nom d'un champ d'une classe parente, même privé,
  qu'Unity refuse (« The same field name is serialized multiple times ») et que le compilateur laisse passer (3 novembre).
- Dans Unity : cliquer dans la fenêtre (recompilation), lire la console ; les bakers y écrivent ce qu'ils ont fait. Le journal
  est dans `Project Archery/Logs/Editor.log`.
- **Après une partie de test** (3 novembre) : lire le **journal de partie**, `Project Archery/Logs/Parties/Partie_<date>.md` (le plus
  récent ; hors de Git), écrit tout seul par `Scripts/Journal/GameJournal*.cs` dans l'éditeur et les builds de développement : le
  déroulé daté (vagues, boss, objets, plans, bâtiments, attaques sur la tour, raccourcis de test, erreurs) et un bilan par vague et
  par partie (ennemis, dégâts et éliminations par source, tirs, dégâts reçus, or, fluidité, bâtiments, flèche).
- Rien n'est « fini » tant que le joueur ne l'a pas testé au casque : le dire clairement.
