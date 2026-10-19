# Crédits et licences

Inventaire de tout ce qui vient d'ailleurs dans le projet (packs, polices, sons, musiques), avec sa licence et ce qu'elle demande. Fait le 18 octobre 2026 à partir des fichiers de licence présents dans `Assets/` ; les lignes « à confirmer » n'ont pas de fichier de licence dans le projet.

## ⚠️ À régler avant le rendu

1. **Le dépôt GitHub est public, et il contient les packs de l'Asset Store** (Dungeon Mason, Dimasjk Studio, Easy Weapons, AgeSoft, coffre animé, Toon RTS). Le contrat de l'Asset Store (EULA standard) permet de les mettre dans un jeu, mais pas de redistribuer leurs fichiers sources, gratuits compris. Or un dépôt public les met à disposition de n'importe qui. Le plus simple : **rendre le dépôt privé** (GitHub > *Settings* > *General* > *Danger Zone* > *Change visibility*) et inviter le professeur et les coéquipiers comme collaborateurs. Ou demander au professeur ce qu'il attend.
2. **Musiques de l'ami** (boss final et dimension des étoiles) : son nom exact (ou son pseudo), et son accord pour les utiliser dans le jeu.
3. **Les packs « à confirmer »** : vérifier l'auteur dans Unity, *Window > Package Manager > My Assets*.

## Les licences, en bref

| Licence | Ce qu'elle permet | Ce qu'elle demande |
|---|---|---|
| **CC0** (domaine public) | Tout : utiliser, modifier, redistribuer | Rien (créditer est apprécié) |
| **SIL Open Font License 1.1** (polices) | Utiliser la police dans le jeu, la redistribuer | Garder le fichier de licence avec la police ; ne pas la vendre seule ; ne pas réutiliser son nom pour une version modifiée |
| **EULA standard de l'Asset Store** | Utiliser les assets dans ses jeux | Ne pas redistribuer les fichiers eux-mêmes (dépôt public, partage du pack) |
| **Unity Companion License** (paquets de Unity) | Utiliser ces fichiers avec Unity | Rien de plus pour un projet Unity |

## L'inventaire

### Licence CC0 (domaine public)

| Asset | Auteur | Dossier | Utilisé pour |
|---|---|---|---|
| Nature Kit (2.1) | Kenney (kenney.nl) | `kenney_nature-kit` | Toute la carte (falaises, arbres, rochers, rivière, statues du portail) ; pièces retouchées dans `_Project/Art` (*Diagonal Cuts*, *River*, *Stair Borders*) |
| Castle Kit (2.0) | Kenney | `kenney_castle-kit` | Tour, remparts, décor |
| Tower Defense Kit (2.1) | Kenney | `kenney_tower-defense-kit` | Bâtiments, socles |
| UI Pack (2.0), avec les polices *Kenney Future* | Kenney | `kenney_ui-pack` | Interface |
| UI Pack: RPG Extension | Kenney | `UIpack_RPG` | Interface |
| Fantasy UI Borders (1.0) | Kenney | `kenney_fantasy-ui-borders` | Cadres des menus, barres de vie, plaques |
| Game Icons | Kenney | `kenney_game-icons` | Icônes |
| Impact Sounds (1.0) | Kenney | `kenney_impact-sounds` | Sons d'impact, pas des monstres de la dimension |
| Interface Sounds (1.0) | Kenney | `kenney_interfaceSounds` | Sons des menus et du portail des étoiles |
| RPG Audio | Kenney | `kenney_rpg-audio` | Portes de l'antre, sons divers |
| Glove (gant low-poly) | Quaternius | `Glove by Quaternius - l1zv4LaA4I` | Les mains gantées du joueur (squelette ajouté par le jeu) |
| Modular Dungeon (porte en arche) | Quaternius | `_Project/Resources/Lair` | La porte de l'antre (coupée en double porte par le jeu) |

### Polices (SIL Open Font License 1.1)

| Police | Auteur | Dossier | Utilisée pour |
|---|---|---|---|
| Cinzel et Cinzel Decorative | Natanael Gama | `cinzel` (licence : `SIL Open Font License.txt`) | Titres et textes des menus |
| Share Tech Mono | Carrois Type Design (Ralph du Carrois) | `_Project/Resources/Fonts` (licence : `ShareTechMono-OFL.txt`, ajoutée le 18 octobre) | Panneaux d'erreur du portail des étoiles |
| Liberation Sans | Red Hat (fournie avec TextMesh Pro) | `TextMesh Pro/Fonts` | Police de secours de TextMesh Pro |

### Asset Store (EULA standard de Unity)

| Asset | Auteur | Dossier | Utilisé pour |
|---|---|---|---|
| Mini Legion Footman, Grunt et Lich PBR HP Polyart | Dungeon Mason | `Mini Legion …` | Fantassin, Orc, Liche ; boss Francine et Nolan |
| RPG Monster DUO, Buddies, Couple et Partners PBR Polyart | Dungeon Mason | `RPG Monster DUO PBR Polyart`, `RPGMonster…` | Bestiaire, volant (Beholder), mimique (Jean) |
| Monster Minion Survivor PBR Polyart | Dungeon Mason | `Monster Minion Survivor PBR Polyart` | Bestiaire |
| Wizard PolyArt | Dungeon Mason (à confirmer) | `WizardPolyArt` | Le mage (Tireur) |
| Four Evil Dragons PBR | Dungeon Mason | `FourEvilDragonsPBR` | MK, le boss final |
| Low Poly Style Bows & Quivers, Shields, 1H Swords, Staffs, Warglaives (Part 1) | Dimasjk Studio | `Dimasjk Studio` | L'arc et la flèche de base ; décor de l'antre |
| Easy Weapons | Kendall H. (à confirmer) | `Kendall H` | Arcs et flèches gardés pour les arcs des classes |
| Stylized Mechanized Hammer | AgeSoft | `AgeSoft` | Les marteaux de construction |
| Animated PBR Chest Demo | à confirmer | `Animated PBR Chest Demo` | Les coffres |
| Toon RTS Units – Demo | à confirmer | `_Project/Art/Monsters`, animations `WK_…` dans `_Project/Animations` | Le chevalier, ennemi au sol |

### Paquets de Unity (Unity Companion License)

| Asset | Dossier | Utilisé pour |
|---|---|---|
| Exemples de XR Interaction Toolkit 3.5.1 (*Starter Assets*, *Hands Interaction Demo*, *Spatial Keyboard*) et de XR Hands 1.8.1 (*HandVisualizer*) | `Samples`, `XRI` | Le joueur en VR (XR Origin, manettes) |
| Modèle de projet VR | `VRTemplateAssets` | Éléments repris du modèle VR de Unity |
| TextMesh Pro | `TextMesh Pro` | Les textes |
| Unity Recorder 5.1.7 (paquet, éditeur seulement : rien dans le jeu) | `Packages` | Le tournage des vidéos du tutoriel |

### Musiques et sons

| Son | Origine | Licence |
|---|---|---|
| Musique du boss final, « Mama Mia Marchello » (`music_final_boss.mp3`) | Composée par un ami (fichier récupéré avec Klickaud, un site qui télécharge les morceaux de SoundCloud) | **À confirmer** : son nom et son accord |
| Musique de la dimension des étoiles, « Sad Nostalgie » (`Sad_Nostalgie_40_Tempo_KLICKAUD.mp3`) | Composée par le même ami (récupérée de la même façon) | **À confirmer** : son nom et son accord |
| Musiques de vague, de boss et de boutique, ambiances jour et nuit (`Audio/Music/*.wav`) | Générées par script pour le prototype (`music_boss` servait aussi à la dimension jusqu'au 18 octobre) | Faites pour le projet |
| Sons provisoires (`Audio/Placeholder`) | Générés par script pour le prototype | Faits pour le projet |
| Sons Kenney | Voir CC0 ci-dessus | CC0 |

### Fait pour le projet

Le code (`_Project/Scripts`, `_Project/Editor`), les 17 shaders (`_Project/Shaders`), les icônes de la boutique (`_Project/UI/Icones`, dessinées par script), les décors fabriqués par le code (arène de MK, dimension du portail, fissures de lave), les données et les scènes. Une partie du code et de la documentation a été écrite avec l'aide de l'IA (Claude, d'Anthropic), comme indiqué dans le README et le rapport.

À confirmer : `_Project/Art/Textures/TargetFace.png` (la cible d'entraînement), présente depuis le premier prototype.
