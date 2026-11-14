# Crédits et licences

Inventaire de tout ce qui vient d'ailleurs dans le projet (packs, polices, sons, musiques), avec sa licence et ce qu'elle demande. Fait le 18 octobre 2026 à partir des fichiers de licence présents dans `Assets/` ; les lignes « à confirmer » n'ont pas de fichier de licence dans le projet. Le 25 octobre, le ménage des fichiers inutilisés (`Fichiers_Inutilises.md`) a retiré du projet les packs *UI Pack* et *UI Pack: RPG Extension* de Kenney, l'ancien gantelet, et les parties inutilisées des autres packs (scènes de démo, versions de style non retenues).

## ⚠️ À régler avant le rendu

1. **Le dépôt GitHub est public, et il contient les packs de l'Asset Store** (Dungeon Mason, Dimasjk Studio, AgeSoft, coffre animé, Toon RTS ; Easy Weapons, inutilisé, a été supprimé du projet le
25 octobre, mais reste dans l'historique de Git). Le contrat de l'Asset Store (EULA standard) permet de les mettre dans un jeu, mais pas de redistribuer leurs fichiers sources, gratuits compris. Or un dépôt public les met à disposition de n'importe qui. Le plus simple : **rendre le dépôt privé** (GitHub > *Settings* > *General* > *Danger Zone* > *Change visibility*) et inviter le professeur et les coéquipiers comme collaborateurs. Ou demander au professeur ce qu'il attend.
2. **Musiques de l'ami** (boss final et dimension des étoiles) : son nom exact (ou son pseudo), et son accord pour les utiliser dans le jeu.
3. **Les packs « à confirmer »** : vérifier l'auteur dans Unity, *Window > Package Manager > My Assets*.

## Les licences, en bref

| Licence | Ce qu'elle permet | Ce qu'elle demande |
|---|---|---|
| **CC0** (domaine public) | Tout : utiliser, modifier, redistribuer | Rien (créditer est apprécié) |
| **CC-BY 3.0 et 4.0** (Creative Commons Attribution) | Tout : utiliser, modifier, redistribuer, même dans un jeu vendu | **Créditer l'auteur** (son nom, le titre, le lien, la licence) et dire si on l'a modifié |
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
| Fantasy Town Kit (2.0) | Kenney | `kenney_fantasy-town-kit` | Détails des bâtiments du tower defense (ajouté le 29 octobre 2026 ; licence CC0, `License.txt` dans le dossier) |
| Fantasy UI Borders (1.0) | Kenney | `kenney_fantasy-ui-borders` | Cadres des menus, barres de vie, plaques |
| Game Icons | Kenney | `kenney_game-icons` | Icônes |
| Impact Sounds (1.0) | Kenney | `kenney_impact-sounds` | Sons d'impact, pas des monstres de la dimension |
| Interface Sounds (1.0) | Kenney | `kenney_interfaceSounds` | Sons des menus, du portail des étoiles, des tirs préparés et de quatre duos (le bouchon du Marteau-pilon, la bulle des sirènes, « perdu le nord » de la Boussole folle, le couinement des baudruches) |
| RPG Audio | Kenney | `kenney_rpg-audio` | Portes de l'antre, sons divers |
| City Kit (Commercial) (2.1) | Kenney | `kenney_city-kit-commercial` | Toit cyberpunk : les immeubles de la ville |
| Glove (gant low-poly) | Quaternius | `Glove by Quaternius - l1zv4LaA4I` | Les mains gantées du joueur (squelette ajouté par le jeu) |
| Modular Dungeon (porte en arche) | Quaternius | `_Project/Resources/Lair` | La porte de l'antre (coupée en double porte par le jeu) |
| LowPoly Modular Dungeon Pack, version de mai 2019 (*Chest*, https://opengameart.org/content/lowpoly-modular-dungeon-pack, aussi sur https://poly.pizza/m/O72u4Drp8k) | Quaternius | `_Project/Art/Chests/Chest_Quaternius.fbx` | Les coffres, depuis le 27 octobre (matériaux refaits aux couleurs du modèle, couvercle ouvert et coffre animé par le jeu) |
| Cyberpunk Game Kit (via Poly Pizza, version FBX) | Quaternius | `Quaternius_Cyberpunk-Game-Kit` | Toit cyberpunk : objets des toits (climatiseurs, antennes, câbles, enseignes, écrans) |
| *Bomb* (https://poly.pizza/m/qGUAks6IHQ) | Quaternius | `Bomb by Quaternius - qGUAks6IHQ` | La grenade à flèches (réduite à 11 cm, matériaux refaits, mèche allumée par des particules) |
| *Katana* (https://poly.pizza/m/15A7InejC7) | Pichuliru | `Katana by Pichuliru - 15A7InejC7` | Le katana (remis dans le repère du jeu, 85 cm jusqu'à la pointe, lame néon refaite) |
| Seamless Space Backgrounds (*Purple Nebula 1*) | Screaming Brain Studios (screamingbrainstudios.itch.io/seamless-space-backgrounds, aussi sur OpenGameArt) | `_Project/Art/Bows/Cosmique/Nebuleuse_Violette.png` | La nébuleuse vue à travers le corps de l'arc cosmique |
| *Ultimate Fantasy RTS* (https://quaternius.com/packs/ultimatefantasyrts.html ; 37 FBX gardés sur 128 : tours d'archerie et de guet, casernes, entrepôts, murs, tours de rempart, moulins, tours-maisons, tonneau ; le fichier de licence du dossier porte par erreur l'en-tête « Ultimate Platformer Pack », CC0) | Quaternius | `construction/Ultimate Fantasy RTS by Quaternius` (renommé, le téléchargement s'appelait `FBX-20261027T…`) | Refonte du tower defense (27 octobre) : les tonneaux du goudron (*Barrel*, 28 octobre) ; les autres bâtiments à venir |
| *Landmine* (https://poly.pizza/m/PtqkseZo9O), *Exploding Barrel* (https://poly.pizza/m/1orHe0kCc1), *Ghost* (https://poly.pizza/m/Iip30bDHmu), *Ghost Skull* (https://poly.pizza/m/TX8r9WBXpe), *Watch Tower* (https://poly.pizza/m/cMxuj2gt7D), *Archery Towers* (https://poly.pizza/m/IA5qOjIAiT) | Quaternius | `construction/… by Quaternius - …` | Refonte du tower defense : la mine (*Landmine*, *Exploding Barrel*, 28 octobre), le phare (*Watch Tower*) et l'esprit gardien (*Ghost*), 28 octobre (les deux tours sont aussi dans *Ultimate Fantasy RTS*) |
| *Mirror* (https://poly.pizza/m/2WQIUVj5qr) | Isa Lousberg | `construction/Mirror by Isa Lousberg - 2WQIUVj5qr` | Refonte du tower defense : le miroir ; pas encore utilisé |
| *Mirror* (https://poly.pizza/m/HIeVDCNgbA) | MilkAndBanana | `construction/Mirror by MilkAndBanana - HIeVDCNgbA` | Refonte du tower defense : le miroir (28 octobre) |
| *Crossbow* (https://poly.pizza/m/kHb0kA11oD) | CreativeTrio | `construction/Crossbow by CreativeTrio - kHb0kA11oD` | Refonte du tower defense : l'arbalète à répétition et le lance-filet le 28 octobre, **plus utilisé depuis le soir même** (retour du joueur : il n'allait pas avec le style des autres tourelles ; remplacé par la baliste du Tower Defense Kit, en petit). Encore dans le projet |

### Licences CC-BY 3.0 et 4.0 (créditer l'auteur : c'est fait dans le README et ici)

| Asset | Auteur | Dossier | Utilisé pour |
|---|---|---|---|
| *Energy Gauntlet - Low Poly (Free Download)* (https://sketchfab.com/3d-models/energy-gauntlet-low-poly-free-download-708a936c5bdc4537842f976e51cbb1a9), **CC-BY 4.0** | **ENOMIC** (Sketchfab, https://sketchfab.com/ENOMIC) | `energy-gauntlet-low-poly-free-download` | **Plus utilisé depuis le 24 octobre** (c'était le gantelet du katana, remplacé par le gant de la matrice) ; **supprimé du projet le 25 octobre**, il reste dans l'historique de Git, d'où ce crédit. **Modifié** : remis dans le repère du jeu (poignet à l'origine, main gauche, 19 cm), matériaux refaits (plaques chromées, lueurs cyan) |
| *Sci fi crossbow* (https://sketchfab.com/3d-models/sci-fi-crossbow-b2890154cfdf4c1caf2780c50280b26b), **CC-BY 4.0** | **oarfish** (Sketchfab, https://sketchfab.com/oarfish54) | `_Project/Art/Cyber/Arbalete.obj` | Les arbalètes jumelles du toit cyberpunk. **Modifié** : converti de glTF en OBJ, recentré sur la poignée, tourné et réduit (55 cm) ; matériau refait (silhouette sombre au contour néon) |
| *Bowblade – DAE WeaponCraft – WoW Styled Bow* (https://sketchfab.com/3d-models/bowblade-dae-weaponcraft-wow-styled-bow-a57fb07e6b254947b1e036f5aa7b7cec), **CC-BY 4.0** | **Tijmen Matthys** (Sketchfab, https://sketchfab.com/tijmenmatthys) | `_Project/Art/Bows/Legendaire/` | Le modèle de l'arc légendaire, avec sa texture peinte à la main. **Modifié** : converti de FBX en OBJ, corde retirée (le jeu dessine la sienne), recentré sur la poignée et mis à l'échelle ; la texture sert aussi de lueur |
| *Enchanted Bow* (https://poly.pizza/m/9j2Tcybxue), **CC-BY 3.0** | **Zsky** (Poly Pizza) | `_Project/Art/Bows/Cosmique/Arc_Enchante.obj` | Le modèle de l'arc cosmique. **Modifié** : converti de glTF en OBJ, corde retirée (le jeu dessine la sienne), matériaux refaits (corps « fenêtre sur l'espace », ailettes violettes lumineuses) |
| *Giant Crossbow* (https://poly.pizza/m/5XoZ_5sp13S), **CC-BY 3.0** | **Aimé Tribolet** (Poly Pizza) | `construction/Giant Crossbow by Aime Tribolet - 5XoZ_5sp13S` (accent retiré du nom du dossier) | Refonte du tower defense : arbalète à répétition, lance-filet ; pas encore utilisé |
| *Spike Mine* (https://poly.pizza/m/4VRVeCH1sHm), **CC-BY 3.0** | **Aaron Clifford** (Poly Pizza) | `construction/Spike Mine by Aaron Clifford - 4VRVeCH1sHm` | Refonte du tower defense : le boulet hérissé du pendule, niveau 3 (28 octobre) ; mine à venir |
| *Mirror* (https://poly.pizza/m/fQZYQYysyU), **CC-BY 3.0** | **Zsky** (Poly Pizza) | `construction/Mirror by Zsky - fQZYQYysyU` | Refonte du tower defense : le miroir ; pas encore utilisé |
| *Shark Gun* (https://poly.pizza/m/TQMWsUDw8B), **CC-BY 3.0** | **theking1322** (Poly Pizza) | `construction/Shark Gun by theking1322 - TQMWsUDw8B` | Refonte du tower defense : le harpon ; pas encore utilisé |
| *Large Electric Generator* (https://poly.pizza/m/ZPlQHwiqTp), **CC-BY 3.0** | **miro_art_studio** (Poly Pizza) | `construction/Large Electric Generator by miro_art_studio - ZPlQHwiqTp` | Refonte du tower defense : la tour de foudre ; pas encore utilisé |

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
| Four Evil Dragons PBR | Dungeon Mason | `FourEvilDragonsPBR` | MK, le boss final (le dragon rouge ; les trois autres couleurs, inutilisées, supprimées le 25 octobre) |
| Low Poly Style Bows & Quivers, Shields, 1H Swords, Staffs, Warglaives (Part 1) | Dimasjk Studio | `Dimasjk Studio` | L'arc et la flèche de base ; décor de l'antre |
| Stylized Mechanized Hammer | AgeSoft | `AgeSoft` | Les marteaux de construction |
| Animated PBR Chest Demo | à confirmer | `Animated PBR Chest Demo` | La lueur à l'ouverture des coffres (son coffre, le modèle des coffres jusqu'au 27 octobre, n'est plus utilisé) |
| Toon RTS Units – Demo | à confirmer | `_Project/Art/Monsters`, animations `WK_…` dans `_Project/Animations` | Le chevalier, ennemi au sol |

### Paquets de Unity (Unity Companion License)

| Asset | Dossier | Utilisé pour |
|---|---|---|
| Exemples de XR Interaction Toolkit 3.5.1 (*Starter Assets*, *Hands Interaction Demo*, *Spatial Keyboard*) et de XR Hands 1.8.1 (*HandVisualizer*) | `Samples`, `XRI` | Le joueur en VR (XR Origin, manettes) ; leurs scènes de démo et leurs décors ont été supprimés le 25 octobre |
| Modèle de projet VR | `VRTemplateAssets` | La police *Inter* et quelques scripts ; le reste (scène d'exemple, décors, tutoriel) a été supprimé le 25 octobre |
| TextMesh Pro | `TextMesh Pro` | Les textes |
| Unity Recorder 5.1.7 (paquet, éditeur seulement : rien dans le jeu) | `Packages` | Le tournage des vidéos du tutoriel |

### Musiques et sons

| Son | Origine | Licence |
|---|---|---|
| Musique du boss final, « Mama Mia Marchello » (`music_final_boss.mp3`) | Composée par un ami (fichier récupéré avec Klickaud, un site qui télécharge les morceaux de SoundCloud) | **À confirmer** : son nom et son accord |
| Musique de la dimension des étoiles, « Sad Nostalgie » (`Sad_Nostalgie_40_Tempo_KLICKAUD.mp3`) | Composée par le même ami (récupérée de la même façon) | **À confirmer** : son nom et son accord |
| Musiques de vague, de boss et de boutique, ambiances jour et nuit (`Audio/Music/*.wav`) | Générées par script pour le prototype (`music_boss` servait aussi à la dimension jusqu'au 18 octobre) | Faites pour le projet |
| Sons provisoires (`Audio/Placeholder`) | Générés par script pour le prototype | Faits pour le projet |
| Texture du trou noir de la singularité (`Art/TowerDefense/Trou_noir.png`) | Dessinée par script (`Tools/texture_trou_noir.py`, 2 novembre) | Faite pour le projet |
| Fer martelé du Colosse de forge (`Art/TowerDefense/Colosse_Fer.png`, `Colosse_Fer_Relief.png`) | Dessinées par script (`Tools/texture_fer_martele.py`, 2 novembre au soir) | Faites pour le projet |
| Bois peint du Grand Manège (`Art/TowerDefense/Manege_Bois.png`, `Manege_Bois_Relief.png`) | Dessinées par script (`Tools/texture_bois_peint.py`, 2 novembre, tard) | Faites pour le projet |
| Glace de la Grande Horloge (`Art/TowerDefense/Horloge_Glace.png`, `Horloge_Glace_Relief.png`) | Dessinées par script (`Tools/texture_glace.py`, 2 novembre, tard) | Faites pour le projet |
| Sons Kenney | Voir CC0 ci-dessus | CC0 |
| Charge de l'arc cosmique (`Audio/Arc_Cosmique_Charge.wav`) | *Sci-fi Weapon Charging 01*, Tim_Verberne (freesound.org/people/Tim_Verberne/sounds/514082) ; recoupé (2,5 à 5,2 s) et fondu. L'original est dans `Audio/` | CC0 |
| Tir laser de l'arc cosmique (`Audio/Arc_Cosmique_Laser.wav`) | *Ion Cannon*, qubodup (freesound.org/people/qubodup/sounds/814049) ; recoupé (0 à 3,4 s), saturé (graves renforcés, distorsion) et fondu. L'original est dans `Audio/` | CC0 |
| Geste du katana (`507466__danjocross__clean-fast-swoosh.aiff`) | *Clean fast Swoosh*, Danjocross (freesound.org/people/Danjocross/sounds/507466) | CC0 |
| Faille lancée par la main (`650564__h2p34__lightsaber-whoosh.wav`) | *Lightsaber Whoosh*, h2p34 (freesound.org/people/h2p34/sounds/650564) | CC0 |
| Fournaise du Colosse de forge, l'ultime (`Audio/Colosse_Fournaise.wav`, 2 novembre) | *fire-whoosh.wav*, hnhnh (freesound.org/people/hnhnh/sounds/244926), mêlé à *Furnace.WAV*, iankath (freesound.org/people/iankath/sounds/173991 ; 3,5 s de 39,2 à 42,7 s, fondu) ; en mono, 48 kHz. Les originaux sont dans `Audio/` | CC0 |
| Grésillement du Colosse (`Audio/Colosse_Gresillement.wav`) | *Sizzler*, magnuswaker (freesound.org/people/magnuswaker/sounds/540080) ; recoupé (1,3 s depuis l'attaque) et fondu, en mono, 48 kHz. L'original est dans `Audio/` | CC0 |
| Tonnerre de la foudre du Colosse (`Audio/Colosse_Tonnerre.wav`) | *thunder crack.wav*, cognito perceptu (freesound.org/people/cognito%20perceptu/sounds/347853) ; recoupé (4,5 s depuis le claquement, à 1 s) et fondu, en mono, 48 kHz. L'original est dans `Audio/` | CC0 |
| Grand impact du marteau du Colosse (`Audio/513110__wakerone__cinematic-trailer-hit-explosion-cc0.mp3`) | *Cinematic Trailer Hit Explosion CC0.mp3*, Wakerone (freesound.org/people/Wakerone/sounds/513110) ; utilisé tel quel | CC0 |
| Coulée de fonte du Colosse (`Audio/Colosse_Coulee.wav`) | *Lava Loop 2*, Fission9 (freesound.org/people/Fission9/sounds/474850) ; 6 s de 2 à 8,5 s, bouclées par un fondu enchaîné de 0,5 s, en mono, 48 kHz. L'original est dans `Audio/` | CC0 |
| Orgue du Grand Manège, l'ultime (`Audio/Manege_Orgue.wav`, 2 novembre) | *Carousel Organ.aif*, Ohrwurm (freesound.org/people/Ohrwurm/sounds/64562 ; un orgue de manège du musée des automates de musique de Bruchsal) ; 16,4 s de 16,8 à 33,2 s, bouclées par un fondu enchaîné de 0,6 s au même endroit du rythme, en mono, 44,1 kHz. L'original est dans `Audio/` | CC0 |
| Fanfare du Manège (`Audio/Manege_Fanfare.wav`) et sa première phrase (`Manege_Fanfare_Courte.wav`) | *Trumpet Fanfare*, bevibeldesign (freesound.org/people/bevibeldesign/sounds/350428) ; 7 s depuis le début et la première phrase (1,6 s), fondus, en mono, 44,1 kHz. L'original est dans `Audio/` | CC0 |
| Galop de la charge du Manège (`Audio/Manege_Galop.wav`) | *a_herd_of_horses.wav*, gurek (freesound.org/people/gurek/sounds/547967) ; 2,66 s de 10,6 à 13,3 s, au niveau égalisé, bouclées par un fondu enchaîné de 0,3 s, en mono, 44,1 kHz. L'original est dans `Audio/` | CC0 |
| Hennissement du Manège (`Audio/Manege_Hennissement.wav`) | *Horse_neighing.wav*, Lydmakeren (freesound.org/people/Lydmakeren/sounds/510903) ; un hennissement, 1,5 s de 5 à 6,5 s, fondu, en mono, 48 kHz. L'original est dans `Audio/` | CC0 |
| Fusée et bouquet du Manège (`Audio/Manege_Fusee.wav`, `Manege_Bouquet.wav`) | *Whistle and Explosion Single_Firework*, Rudmer_Rotteveel (freesound.org/people/Rudmer_Rotteveel/sounds/336008) ; coupé en deux : le sifflement (0 à 1,13 s, sans les graves, remonté de 35 dB) et l'explosion (de 1,15 à 2,1 s, fondue), en mono, 44,1 kHz. L'original est dans `Audio/` | CC0 |
| Tic de la Grande Horloge, l'ultime (`Audio/Horloge_Tic.wav`, 3 novembre) | *Clock ticking*, Mortifreshman (freesound.org/people/Mortifreshman/sounds/237210) ; le tic le plus net (à 11,74 s, 87 ms), sans ses graves, qui s'éteint vite, en mono, 44,1 kHz. L'original est dans `Audio/` | CC0 |
| Les douze coups de Minuit (`Audio/Horloge_Cloche.wav`) | *church bells strike 12 midday*, Astounded (freesound.org/people/Astounded/sounds/481969) ; un coup (de 42,4 à 45 s), ramené de 96 à 48 kHz, en mono, fondu. L'original est dans `Audio/` | CC0 |
| Le temps qui s'arrête et qui reprend (`Audio/Horloge_Arret.wav`, `Horloge_Reprise.wav`) | *Reverse Crash Cymbal Projection*, deleted_user_7146007 (freesound.org/people/deleted_user_7146007/sounds/383903) ; sa dernière seconde (l'arrêt) et ses 2,5 dernières secondes remises à l'endroit (la reprise), en mono, 48 kHz. L'original est dans `Audio/` | CC0 |
| La glace qui éclate (`Audio/Horloge_Bris.wav`, `Horloge_Eclat.wav`) | *Ice break*, humanoide9000 (freesound.org/people/humanoide9000/sounds/329744) ; la série de craquements (5 à 7,2 s) et trois coups serrés (6,25 à 6,8 s), un peu écrêtés, en mono, 48 kHz. L'original est dans `Audio/` | CC0 |
| Le carillon des échos (`Audio/Horloge_Carillon.wav`) | *GLEAM-GLOW-SFX-CHIME.wav*, newagesoup (freesound.org/people/newagesoup/sounds/351408) ; depuis son attaque (1,2 s), 3,4 s, en mono, 48 kHz. L'original est dans `Audio/` | CC0 |
| Coup de la batte de baseball (`Audio/Batte_Coup.wav`, 13 novembre) | *Bat_hit2*, hashtagsmcgee (freesound.org/people/hashtagsmcgee/sounds/851452 ; une batte d'aluminium sur une balle) ; depuis son attaque, 0,62 s, fondu, en mono, 48 kHz. L'aperçu MP3 de freesound est dans `Audio/` | CC0 |
| Coup parfait de la batte (`Audio/Batte_Parfait.wav`) | *Big impact.wav*, AudioPapkin (freesound.org/people/AudioPapkin/sounds/430977) ; 2,2 s depuis son attaque, saturé fort (écrêtage doux puis saturation asymétrique), sa retombée refaite, mêlé à *Bat_hit2* (ci-dessus), ramené au volume de l'explosion de la grenade, en mono, 48 kHz (`Tools/sons/batte.py`). L'aperçu MP3 de freesound est dans `Audio/` | CC0 |

### Fait pour le projet

Le code (`_Project/Scripts`, `_Project/Editor`), les 20 shaders (`_Project/Shaders`), les icônes de la boutique et les symboles du texte des cartes (`_Project/UI/Icones`, dessinés par script), les décors fabriqués par le code (arène de MK, dimension du portail, fissures de lave), les données et les scènes. Une partie du code et de la documentation a été écrite avec l'aide de l'IA (Claude, d'Anthropic), comme indiqué dans le README et le rapport.

Le visage des mannequins d'entraînement (`_Project/Art/Training/Visage_Mannequin.png`) : dessiné par le joueur (Antonin Croquette) sur Paint, le 2 novembre 2026 (recadré, fond rendu transparent).

Le cor de la tour (`_Project/Audio/Tour_Cor.wav`, 3 novembre 2026) : synthétisé par script (`Tools/sons/cor_de_la_tour.py`), sans aucun son extérieur ni modèle.

Le bruit de l'arrêt du temps de la Grande Horloge (`_Project/Audio/Horloge_Temps_Arrete.wav`, 3 novembre 2026) : synthétisé par script (`Tools/sons/arret_du_temps.py`), sans aucun son extérieur. Son dessin (un bourdon saturé qui descend, un grave qui remonte avec un sifflement, la coupure nette) vient de l'analyse du « Za Warudo » de *JoJo's Bizarre Adventure*, que le joueur a donné pour modèle ; rien de l'extrait n'est repris. L'extrait lui-même (*ZA WARUDO - Dios The World Sound Effect.mp3*) vient d'une série animée sous droits : le jeu ne l'utilise pas, et il est rangé hors du dépôt (`Desktop/dev/Sons_modeles/`, à côté de `Thunk VR`).

À confirmer : `_Project/Art/Textures/TargetFace.png` (la cible d'entraînement, plus utilisée dans la carte depuis le 2 novembre au soir), présente depuis le premier prototype.
