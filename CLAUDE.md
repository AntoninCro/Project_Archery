# THUNK VR — consignes pour Claude

Jeu de tir à l'arc en VR (Meta Quest 3, par Quest Link) pour le cours ST2OOS. Unity **6000.5.10f1**, URP, XR Interaction
Toolkit 3.5.1, OpenXR, Input System. Le projet Unity est dans `Project Archery/` ; le code dans `Assets/_Project/Scripts`
(un dossier par thème), les outils d'éditeur (« bakers ») dans `Assets/_Project/Editor`, les réglages de jeu dans
`Assets/_Project/Data`, la scène principale `Assets/_Project/Scenes/Prototype_Tir.unity`.

**À lire en premier** : `Project Archery/Docs/Reprise_Projet.md` (où en est le projet, la suite, le plan du katana), puis
`Project Archery/Docs/GDD.md` (la référence : règles, chiffres, historique daté, planning).

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
- **Fichiers des coéquipiers** : `Scripts/BowClasses/*` et le prefab `Prefabs/UI/Bow Classes` (Cider-Jude), `Scripts/Grenade/*` et
  `Prefabs/Grenade/*` (dovinvilson) : dire au joueur chaque fois qu'on les modifie.
- **Git** : commiter ou pousser seulement quand le joueur le demande. Le dépôt est en SSH (`git@github.com:AntoninCro/Project_Archery.git`) :
  sur ce PC, Smart App Control bloque Git en HTTPS.
- Demander avant de supprimer un fichier du joueur et avant tout téléchargement ; ne jamais saisir de mot de passe.
- Vérifier qu'un nom de fichier est libre avant de le créer ; pas d'accents dans les noms de fichiers d'assets. Garder la fin de
  ligne d'un fichier (CRLF ou LF) quand on le modifie.
- **Raccourcis de test** : dans `#if UNITY_EDITOR || DEVELOPMENT_BUILD`, et à ajouter au tableau de `Docs/Demo_Vendredi.md`.

## Vérifier

- Sans Unity : `python Tools/compile_check.py` (depuis `Project Archery/`) compile le code du jeu comme un build ; les scripts
  d'`Assets/_Project/Editor` ne se vérifient que dans Unity.
- Dans Unity : cliquer dans la fenêtre (recompilation), lire la console ; les bakers y écrivent ce qu'ils ont fait. Le journal
  est dans `Project Archery/Logs/Editor.log`.
- Rien n'est « fini » tant que le joueur ne l'a pas testé au casque : le dire clairement.
