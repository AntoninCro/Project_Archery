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
  **V** (une caisse), **F2** (le menu des plans sans casque), **F3** (projeter les ennemis proches), **F4** (la vitrine : chaque
  tour, puis chaque duo, puis chaque trio, posée à plusieurs niveaux ; `Building/BuildShowcase.cs`). Les trios (1er novembre) :
  un duo plus une tour, `Building/TowerTrio.cs`, modèles dans `Tools/allure_tours/trios.py` (partie « trios » du fichier d'allure).
  **La tâche suivante (1er novembre au soir)** : le tableau Excel d'équilibrage des 10 tours, demandé par le joueur et décrit en
  entier dans `Refonte_TowerDefense.md`, partie 12.32 (rien n'est commencé ; le scaling des bâtiments vient de changer : 12.31).
- **Le chantier en cours** (26 octobre) : la refonte de la partie roguelike, « la flèche en pièces » ; son état et ses étapes
  sont dans `Project Archery/Docs/Refonte_Roguelike.md`, partie « Avancement ».

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
  **F** (tout, au niveau 3, une fois), **X** (coffre de boss), **M** (or).

## Pièges connus sur ce PC

- **Unity est-il ouvert ?** `Get-Process Unity` en PowerShell (`tasklist` dans Git Bash ne le voit pas). Ouvert, il recompile
  dès qu'un `.cs` change : modifier d'abord les fichiers dont un nouveau script dépend. Il ne réimporte qu'en reprenant le focus.
- **Fins de ligne** : la plupart des scripts et des docs sont en CRLF, quelques-uns en LF (vérifier avant). Jamais `sed -i` de
  Git Bash sur un fichier CRLF (il le passe en LF) ; l'outil Edit les garde ; en Python, lire et écrire en binaire. Un nouveau
  fichier prend la fin de ligne de ses voisins. Les assets YAML sont en CRLF : écrire le `.meta` avant l'asset (GUID choisi).
- **Git en PowerShell** : rafraîchir le PATH d'abord :
  `$env:Path = [Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [Environment]::GetEnvironmentVariable('Path','User')`.
- **Le casque** : `adb` est livré avec Unity ; journaux et mesures du Quest : `Docs/Audit_Performance_Quest.md`, partie 8.
- **Le PC actuel (installé le 1er novembre, vérifié)** : le dépôt est dans `C:\Users\anton\Desktop\dev\Thunk VR` (le projet
  Unity dans `Thunk VR\Project Archery`). Unity Hub y a **deux versions** : 6000.5.10f1 (celle du projet) et 6000.6.7f1 ;
  toujours ouvrir avec la 6000.5.10f1 (l'autre convertirait le projet). Python 3.13 (`py` et `python` marchent tous les deux,
  avec numpy, Pillow et python-pptx), SDK .NET 8 et 10, Git et Git LFS (le dépôt n'utilise pas LFS), installés par winget. Les
  deux `compile_check` passent sans erreur ni avertissement. Les packs d'assets téléchargés mais pas importés étaient hors du
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
  (les bakers) contre lui.
- Dans Unity : cliquer dans la fenêtre (recompilation), lire la console ; les bakers y écrivent ce qu'ils ont fait. Le journal
  est dans `Project Archery/Logs/Editor.log`.
- Rien n'est « fini » tant que le joueur ne l'a pas testé au casque : le dire clairement.
