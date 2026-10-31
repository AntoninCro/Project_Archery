# Tests complets et préparation de la démo

À faire dans les derniers jours avant le rendu (vendredi 23 octobre). Coche au fur et à mesure.

## 1. Tests complets (dans le casque)

Une partie en **Facile**, une en **Normal**, puis une en **Impossible**, la console ouverte : aucune ligne rouge, et le moins possible de lignes « Image lente ».

**La boutique à gauche, l'atelier à droite, le 27 octobre au soir (GDD, sections 6 et 7)**
- [ ] À l'ouverture de Unity : « Confort du 27 octobre (scène Prototype_Tir) : la boutique s'ouvre plus à gauche, l'atelier à droite du joueur. Enregistre la scène » (**Ctrl+S**).
- [ ] À la fin d'une vague : la boutique devant à gauche, l'atelier devant à droite, rien juste devant ; on vise et on clique les deux sans gêne.
- [ ] Tourner la tête vers l'atelier, puis vers la boutique : aucun des deux ne bouge ; se retourner complètement ou s'éloigner : les deux reviennent, chacun de son côté.
- [ ] Près d'un mur de la tour : chaque panneau cherche une place libre de son côté, sans se chevaucher.
- [ ] Le coffre de boss (raccourci X) tombe juste devant, entre les deux panneaux, et ses orbes ne sont cachées par aucun.
- [ ] **Un vrai boss tué** : son coffre tombe là où il est mort, sur le sol (même s'il volait), avec son rayon de lumière ; « Le boss a laissé un coffre là où il est tombé ! » ; il reste jusqu'à la fin de la vague suivante.

**Les coffres de la carte, le 27 octobre au soir (GDD, section 13)**
- [ ] Chaque coffre de vague propose toujours un objet (à gauche, avec son emplacement : « POINTE », « CORDE »…), un parchemin et un bonus temporaire ; le fragment de l'arc légendaire prend parfois la place du parchemin.
- [ ] Une pièce déjà prise apparaît au niveau suivant (« 50 % → 100 % ➶ » sous son nom) ; toutes les pièces au niveau 3 : deux parchemins.

**Le nouveau coffre de Quaternius, le 27 octobre (GDD, section 13)**
- [ ] À l'ouverture de Unity, la console : « Nouveau coffre (27 octobre) : le prefab Chest prend le coffre de Quaternius… » (rien à enregistrer : c'est le prefab).
- [ ] Un coffre de vague et un coffre de boss (X) : bois brun et ferrures sombres, serrure tournée vers le joueur, posé sur le sol (ni enfoncé ni flottant) ; il tombe du ciel, s'écrase, puis sautille en attendant.
- [ ] À l'ouverture : le couvercle bascule vers l'arrière autour de son arête arrière (sans traverser la caisse), la lueur dorée sort, les 3 orbes montent au-dessus.
- [ ] On l'ouvre en visant la caisse comme le couvercle ; ses colliders suivent le modèle (rien de touché dans le vide autour).

**Le compteur de dégâts des mannequins, le 27 octobre au soir (GDD, section 6, les mannequins)**
- [ ] Entre les vagues, tirer sur un mannequin : au-dessus de lui, « … /s » et « Total … en … s », lisibles de loin, toujours tournés vers soi.
- [ ] Les dégâts de la foudre, des explosions, des pieux et des sentinelles s'ajoutent au total.
- [ ] 3 s sans coup : le résultat pâlit et reste affiché, le mannequin reprend tous ses PV ; le coup suivant ouvre une nouvelle série.
- [ ] Les mannequins tiennent une longue série (PV ×100) ; pendant une vague, le compteur disparaît.
- [ ] Mesurer le build moyen (10 améliorations) en Normal : environ 200 /s sur un mannequin seul d'après les estimations du GDD.

**Les cartes refaites par le joueur, le 27 octobre au soir (GDD, section 6, même titre)**
- [ ] À l'ouverture de Unity : « Cartes du 27 octobre : le sprite des symboles… est créé », puis « … (catalogue) : 46 cartes refaites… ». Aucune ligne rouge de TextMeshPro (« sprite asset »).
- [ ] **Boutique** : les noms nouveaux (Foudroiement, ZA WARUDO, Le style, ça compte aussi…), sans faute ; le symbole flèche à la place de « dégâts », de la couleur du texte, à la bonne taille ; l'icône de la catégorie sur chaque carte (pointe, empennage, fût, corde, talisman ; parchemin pour les 3 parchemins).
- [ ] **Reprendre une carte** (Tab pour monter tout d'un niveau, puis la boutique) : la carte de niveau 2 dit « 50 % → 100 % ➶ » pour le foudroiement, avec une flèche « → » lisible ; au niveau 3, « 100 % → 150 % ➶ ». Pareil dans l'orbe du coffre de boss (X).
- [ ] **Atelier** : une pièce de niveau 2 ou 3 affiche les chiffres de son niveau (le foudroiement niveau 3 : 150 % ➶).
- [ ] **Raretés** : le foudroiement et la transe sortent en blanc (commune), ZA WARUDO et la pluie de flèches en doré.
- [ ] **Chances** (console à l'ouverture : « … les objets sortent à 50 % communs, 35 % rares, 15 % légendaires ») : sur une dizaine de boutiques, surtout des cartes blanches, une carte dorée environ une boutique sur trois.
- [ ] **Duos** : l'annonce dit « Duo : Donut ! », « Duo : Focus ! »… ; dans l'atelier, la ligne du duo a le nouveau nom.
- [ ] **Tir explosif** : un ennemi au bord de l'explosion prend autant qu'au centre.

**Équilibrage du 27 octobre au soir (GDD, section 6, même titre)**
- [ ] À l'ouverture de Unity : « Équilibrage du 27 octobre (catalogue) : le Tir écho devient légendaire », « … (ShotTuning) : le tir parfait, dégâts ×2,5, points et or ×3 », « … (scène Prototype_Tir) : 1000 flèches en vol au plus. Enregistre la scène » (**Ctrl+S**).
- [ ] **Tir parfait** : le chiffre de dégâts d'un parfait au corps vaut 2,5 fois la base de l'arme (×2 avant) ; un ennemi tué d'un parfait rapporte plus d'or qu'avant.
- [ ] **Budget d'effets** (F, puis corde triple, déluge et pluie, ou les Akimbow) : sous une marée de flèches, celles en plus perdent leur traînée et leurs plumes au-delà de 120 en vol ; les sons ne saturent pas ; les chiffres de dégâts s'espacent ; le jeu reste fluide (console : peu de « Image lente »). Les dégâts ne changent pas.
- [ ] **Akimbow** : le tir et le sifflement des flèches sont deux fois moins forts ; le son du parfait reste.
- [ ] **Pluie** : les flèches de pluie font 35 % (chiffres plus petits qu'avant). **Chasse à l'homme** : une tête marquée ne meurt plus d'un coup au-dessus de 75 % de PV (bourreau niveau 3).
- [ ] La boutique propose le **Tir écho** en légendaire (couleur dorée).

**Deuxième tour du 27 octobre : le patch note rempli (GDD, section 6, même titre)**
- [ ] À l'ouverture de Unity : « … deuxième tour (catalogue) : Tir écho…, Vampirisme…, Vigueur… », « … (scène Prototype_Tir) : la chaîne d'éclairs à 30 / 60 / 90 %, le ricochet à 100 % de la vitesse. Enregistre la scène » (**Ctrl+S**), et « textes revus : Corde du faucon…, Vigueur… ».
- [ ] **Détonateur** planté dans un mannequin : tirer juste à côté de la flèche la fait éclater. **Billard** : la chaîne part à chaque fois.
- [ ] **Mèche** : plus de 100 mèches (avec la pluie) : les plus anciennes explosent pour faire de la place.
- [ ] **Boomerang** : au demi-tour, elle revient droit sur toi, sans arc de cercle. **Télékinésie** avec la pluie ou le multitir : seule la flèche de l'arc tourne.
- [ ] **Transe** : à la 5e flèche parfaite d'affilée, la zone du parfait de l'anneau devient dorée ; les tirs parfaits suivants traversent tout (et explosent fort avec la supernova) ; un tir raté : elle redevient verte.
- [ ] **Siège avec les Akimbow** : une arbalète gardée au bout de son anneau fait des étincelles, « Chargée ! » au bout de 2 s, et frappe plus fort.
- [ ] **Siège avec le katana** (O pour l'équiper) : une faille ouverte se dore en 2 s (« Chargée ! ») ; lancée tout de suite elle frappe normalement (sa qualité suit la longueur du coup), lancée chargée bien plus fort. La boutique propose la corde de siège avec le katana.
- [ ] **Œil de faucon** : pendant le ralenti, des cadres dorés brillants sur les points faibles ; un point faible touché prend ×1,5.
- [ ] **Rebond + pluie** : les flèches de la pluie rebondissent. **Perçage** : les ennemis traversés prennent tous les mêmes dégâts.

**Réglages du joueur du 27 octobre, après la liste des tests (GDD, section 6, même titre)**
- [ ] À l'ouverture de Unity : « Réglages roguelike du 27 octobre, textes revus : Empennage boomerang… ; Corde de pluie… ».
- [ ] **Boomerang** : la flèche revient et attend ; sans rien presser, elle n'est pas prise. Presser la poignée de la main des flèches près d'elle : elle arrive dans la main ; encochée, l'anneau est tout vert, et le tir est parfait ; le tir d'après ne l'est pas. Deux flèches devant soi : une pression n'en prend qu'une. La main de l'arc ne prend rien.
- [ ] **Corde de pluie** : un tir à plat sème des flèches le long de sa traînée, plus serrées au niveau 3 ; rien les 4 premiers mètres. Avec l'empennage piqué : la pluie suit le piqué jusque sur la cible. Le nuage (plume) et l'averse (fût suspendu) marchent comme avant.
- [ ] **Rappel** : plus de 24 flèches plantées reviennent toutes (jusqu'à 100). **Conducteur** : beaucoup de flèches reliées sans que les premières s'effacent ; pas de saccade au Quest ?
- [ ] À l'ouverture de Unity : « … 100 flèches plantées au plus dans la réserve (40 avant). Enregistre la scène » ; **Ctrl+S**.
- [ ] **Faux** (boomerang + perçage) : revenue, la flèche attend une demi-seconde devant toi, puis repart.
- [ ] **Pluie + boomerang** : les flèches de pluie reviennent et disparaissent devant toi, sans s'arrêter ; la poignée ne les prend pas.
- [ ] **Pluie + sentinelle** : les flèches de pluie plantées au sol tirent toutes (jusqu'à 100 sentinelles) ; plusieurs tirs de suite : les premières tirent encore. Pas de saccade au Quest ?
- [ ] **Fil électrique** : l'arc part à 1 m devant le joueur, plus sous ses yeux. **Fût suspendu** : les flèches s'arrêtent plus près. **Corde fantôme** : la flèche fantôme frappe plus fort que le tir (120 / 150 / 200 %).

**Corrections du 27 octobre (après les tests du joueur ; GDD, section 6, « Corrections du 27 octobre »)**
- [ ] À l'ouverture de Unity : « Réglages roguelike du 27 octobre : plus d'effet réservé aux tirs parfaits… ». Les cartes Flèche de foudre, Chaîne d'éclairs, Flèche de glace et Déluge n'affichent plus « · tir parfait ».
- [ ] Un tir **raté** avec la Flèche de foudre, la Flèche de glace, la Pointe de givre : la foudre, la zone de glace, le gel (puis l'éclat au tir suivant). Au Quest, avec le multitir et la corde triple : pas de saccade ?
- [ ] **Anneau vert** : un coup au corps à corps avec le duelliste, puis l'anneau suivant entièrement vert ; un boomerang rattrapé ; une glissade avec l'acrobate (vert en l'air, des bandes après) ; le coffre « Tirs parfaits » (plus de liseré rouge, la corde de siège charge encore) ; les Akimbow ; l'arc cosmique (le deuxième cercle garde ses bandes).
- [ ] **Rappel** : des flèches au sol derrière les mannequins, et une plantée dans un mannequin ; poing fermé (ou Ctrl gauche) : des dégâts sur les mannequins frôlés, et sur celui qui avait la flèche.
- [ ] **Piqué** : un tir un peu vers le haut (plus de 15°) en direction d'un mannequin : la flèche pique dessus, traînée orange.
- [ ] **Boomerang** : la main de l'arc ne le rattrape pas, l'autre oui.
- [ ] **Ricochet** sur un groupe de mannequins (et sur des ennemis qui marchent) : la flèche déviée touche à chaque fois. **Billard** (détonateur + ricochet) : 3 ou 4 détonateurs plantés à moins de 15 m, en faire éclater un : les autres suivent.
- [ ] **Corde de pluie** avec une pointe (foudre, mèche, givre…) : ses flèches portent l'effet.
- [ ] **Ingénieur** : tirer à travers une baliste ou une tour de givre : « Surchargée ! », elle rougit un peu en battant, puis reprend ses couleurs.
- [ ] **Fût suspendu** : le rayon de la main libre et son cercle tant que des flèches attendent ; elles partent bien vers le cercle.
- [ ] **Corde fantôme** : la trajectoire du tir fantôme pendant qu'on tire la corde à vide.
- [ ] **Étoile filante** (acrobate + corde triple, à équiper dans l'atelier : Tab et F ne le font pas) : en glissade, une couronne de 6 / 12 / 18 flèches autour du point visé à 20-30 m. Sans casque : Espace, puis tirer en l'air.
- [ ] **Glacier** (Flèche de glace + Pointe de givre, à la main dans l'atelier) : des mannequins sur la glace ; en faire éclater un : « Glacier ! ×… », les autres gèlent.
- [ ] **Les duos du rebond** (rebond + sentinelle, pieu, conducteur, aimant, détonateur, Flèche de glace) : l'effet à chaque rebond ; l'annonce « Duo : … ! ».
- [ ] **Nuage** (plume + corde de pluie) : un tir vers le ciel s'arrête, devient un nuage qui avance et pleut 2,5 s, puis se dissipe. **Averse** (fût suspendu + corde de pluie) : un tir vers le ciel reste suspendu sans éclater ; à la gâchette, une pluie tombe sur le cercle du rayon. **Retour explosif** (rappel + mèche ou détonateur) : rappeler fait exploser les mèches et éclater les détonateurs, puis les flèches reviennent.
- [ ] **Akimbow ou katana** choisi au menu des armes : sur plusieurs boutiques et coffres, jamais la corde de siège ni la corde fantôme ; avec un arc, elles reviennent.
- [ ] **Le rappel laisse les effets** : une sentinelle ou un pieu planté, puis rappeler : la sentinelle tire encore, le pieu blesse encore ; un détonateur rappelé crépite encore 6 s et éclate si une flèche passe.
- [ ] **Les duos du boomerang** (boomerang + mèche, aimant, sentinelle, conducteur, pieu, détonateur) : l'effet au demi-tour, en l'air au bout de la course et contre un mur ; avec le perçage (la faux), à chacun des deux demi-tours ; le fil électrique suit bien la ligne du tir. Avec la corde triple : pas de saccade ?

**Réglages du 26 octobre au soir (après les tests du joueur ; GDD, section 6, le tableau « Avant / Depuis »)**
- [ ] À l'ouverture de Unity : la console écrit « Réglages roguelike du 26 octobre : catalogue de la boutique modifié… », « … le mannequin d'entraînement compte comme un ennemi… » et « … 4 mannequin(s) ajouté(s) au centre d'entraînement… Enregistre la scène » ; **Ctrl+S**. Pas de ligne rouge. Dans `Data/Shop/ShopCatalog` : Spectral, Grenade sauvage et Grenade rapide en *Retired*, « Super grenade » à la fin.
- [ ] Le centre d'entraînement : 6 mannequins, dont 3 serrés et un loin (45 m) ; aucun dans une voie, un mur ou la rivière (sinon, les déplacer à la main).
- [ ] Avant la première vague : un mannequin touché montre ses dégâts ; tué, il tombe en arrière puis se relève ; il ne rapporte ni points ni or ; les balistes ne tirent pas dessus. La foudre, l'explosion, l'aimant (il est attiré puis revient à sa place), le gel, la marque, le bourreau marchent dessus. Pendant une vague, il redevient invulnérable et l'auto-visée l'ignore.
- [ ] Foudre, chaîne d'éclairs, glace, déluge : seulement aux tirs parfaits, à chaque fois ; explosion, perçage, multitir (2 flèches au niveau 1) : à chaque tir ; écho : la copie part de là où est la flèche, pas de l'arc.
- [ ] Boomerang : il fait demi-tour sur le côté, sans piquer vers le sol, même tiré à plat ; tiré dans le sol ou un mur, il revient aussi ; il ralentit en approchant et s'arrête devant toi 1,5 s ; facile à rattraper ?
- [ ] Télékinésie : à l'appui, la flèche ralentit de moitié et tourne comme la main (160 °/s).
- [ ] Fût suspendu : la gâchette de l'autre main envoie les flèches là où elle vise.
- [ ] Faucon : le ralenti à chaque tension, une fois par tension (pas tout le temps quand on reste tendu).
- [ ] Acrobate : temps ×0,5 ; la glissade s'arrête plus tôt (à mi-vitesse).
- [ ] Ingénieur : la baliste surchargée tire 2 fois plus vite (4, 6 aux niveaux 2 et 3) ; un tir parfait à travers une barricade abîmée : « Réparée +… PV ».
- [ ] Bourreau : une tête qui laisse l'ennemi sous 25 % meurt net ; un boss sous 10 %.
- [ ] Super grenade (talisman) : plus de flèches, recharge plus courte.
- [ ] Trou noir (fût aimant + pointe à mèche) : une sphère noire avec son anneau et ses disques, qui aspire 1,5 s, se referme et explose large. Lisible ? Pas d'image lente ?
- [ ] Une carte de pièce qui formerait un duo dit avec quelle pièce ; rangée dans l'atelier, le message d'achat le rappelle.
- [ ] ~~Parchemins : même prix après la vague 3 qu'après la vague 1~~ : la hausse par vague est remise le soir même.

**Troisième lot, le 26 octobre au soir, suite (GDD, section 6 ; `Docs/Refonte_Roguelike.md`, parties « Avancement » et 14 ; R pour les essayer)**
- [ ] À l'ouverture de Unity : « Refonte roguelike, troisième lot : pièces déplacées… » et « … (scène Prototype_Tir) : la vraie explosion… Enregistre la scène » ; **Ctrl+S**. Dans `Data/Shop/ShopCatalog` : Perçage et Flèche de glace au fût, Vampirisme au talisman, Chasseur de têtes à la corde, Déluge rare, les 7 pièces nouvelles à la fin.
- [ ] La boutique : 3 objets de 3 emplacements différents, à chaque pause ; les pointes ne reviennent plus une fois sur deux. Le coffre de boss : 2 emplacements différents.
- [ ] La vraie explosion (flèche explosive, mèche, trou noir) : un flash, une boule de feu, une fumée épaisse qui monte et s'étale, de la poussière au sol, des étincelles. Belle ? Pas trop lourde au Quest avec beaucoup d'explosions ?
- [ ] Multitir : les flèches serrées ; corde triple : l'éventail plus large qu'avant.
- [ ] Corde de siège : laisser l'anneau aller au bout (vert clair) et garder la tension : des étincelles dorées grossissent à la pointe, la manette vibre de plus en plus, « Chargée ! » au bout de 2 s ; le tir fait bien plus mal et traverse. Avec l'arc cosmique : le laser, chargé au bout du deuxième cercle, fait bien plus mal.
- [ ] Corde de pluie : un tir vers le ciel éclate à son sommet en pluie de flèches ; avec l'arc légendaire, sa pluie a deux fois plus de flèches.
- [ ] Corde fantôme : juste après un tir, attraper la corde à vide (la poignée, près de l'encoche), la tirer, la lâcher : un fil pâle pendant qu'on tire, puis « Fantôme ! » et une flèche part là où vise l'arc. Le geste se fait-il naturellement ? Sans casque : clic du milieu.
- [ ] Empennage piqué : tirer en cloche vers un groupe : au sommet, la flèche vire orange et fond sur un ennemi.
- [ ] Empennage de rappel : planter plusieurs flèches, puis fermer le poing de la main libre devant soi : elles reviennent en traversant les ennemis, puis disparaissent près de toi. Rien quand on prend une flèche au carquois, la grenade à la hanche ou le marteau. Sans casque : Ctrl gauche.
- [ ] Fût-sentinelle : une flèche plantée au sol tire de petites flèches sur l'ennemi le plus proche pendant 5 s.
- [ ] Talisman de l'avare : avec beaucoup d'or, les dégâts montent (+1 % par 20 or) ; ils baissent après un achat.
- [ ] Les duos nouveaux (GDD, section 6) : orage magnétique, lance-grenades (une explosion de la taille de la mèche à chaque rebond), supernova, chasse à l'homme, pluie de fer, billard (des détonateurs en chaîne), clôture de givre, réseau (un arc électrique jusqu'à la baliste, qui tire plus vite), étoile filante (en l'air, une couronne de flèches), rail (un trait de lumière qui laisse une traînée), glacier.
- [ ] Bourreau sur MK : une tête qui le laisse sous 5 % le tue (7,5 et 10 % aux niveaux 2 et 3).

**Refonte roguelike, étape 1 (25 octobre ; GDD, sections 6, 7 et 13 ; `Docs/Refonte_Roguelike.md`)**
- [ ] À l'ouverture de Unity : la console écrit « Refonte roguelike, étape 1 : catalogue de la boutique converti », avec la liste des objets, des cartes retirées et des parchemins ajoutés ; aucune ligne rouge. Dans `Data/Shop/ShopCatalog`, chaque amélioration a son genre (*Kind*), les objets ont *Max Stacks* à 1, et les trois parchemins (Force, Adresse, Vigueur) sont à la fin, avec leur icône.
- [ ] Retouche du soir, à l'ouverture de Unity : la console écrit « Boutique : 3 objets par pause… » et « Boutique (scène Prototype_Tir) : 6 cartes sur 3 colonnes… Enregistre la scène » ; **Ctrl+S**. Dans la scène, le panneau de la boutique n'a plus de bouton « Relancer » ni de carte des arcs ; « Ligne parchemins » et « Ligne objets » sont à gauche des deux lignes de cartes.
- [ ] Boutique : en haut, les 3 parchemins (« Parchemin », 25 or) ; en bas, 3 objets différents (leur rareté, de 120 à 220 or à la première pause) ; « Parchemins » et « Objets » lisibles à gauche, sans chevaucher les cartes ni le cadre ; plus de Dégâts, Puissance, Vitalité, Butin, Chance, ni de réparation de la tour ou des barricades.
- [ ] Acheter un parchemin : il reste en vente, au niveau suivant (« Parchemin · niveau 2 »), et les trois coûtent un peu plus cher ; le bonus se sent (Force : les dégâts ; Adresse : l'anneau ; Vigueur : les PV de la montre).
- [ ] Acheter un objet : « Acheté », et les deux autres coûtent 15 % de plus ; on peut acheter les trois ; un objet déjà pris n'est plus proposé.
- [ ] Fin de vague avec la tour abîmée (ou détruite) et des barricades cassées : le message de fin dit « Tour réparée » (ou « Tour reconstruite ») et « barricades réparées » ; la tour retrouve tous ses PV (le gong remonte si elle était tombée) ; la récompense reste divisée par deux si la tour était détruite.
- [ ] La ligne du bas du panneau : « Objets : … — Parchemins : Force ×2 … ».
- [ ] Coffre de vague : un objet de temps en temps (« OBJET », une fois sur 4), sinon un parchemin (« PARCHEMIN »), un autre parchemin, et un bonus (« TEMPORAIRE ») ; un parchemin pris dans un coffre ne fait pas monter les prix.
- [ ] Coffre de boss (à la fin de chaque vague, ou **X**) : 1,5 s après le message de fin, un grand coffre tombe près de toi, devant à droite, sur le même sol (en haut de la tour : sur la plateforme, pas en bas), là où tu le vois ; « Le boss a laissé un coffre ! » ; 2 objets au choix ; il est encore là pendant la vague suivante si tu ne l'as pas ouvert ; pas de coffre après la victoire contre MK.
- [ ] Une partie complète en Normal : combien d'objets à la fin (une dizaine ?), et la difficulté : les ennemis des vagues 3 à 5 sont-ils devenus trop forts avec moins d'améliorations ? (courbe des *Wave Settings* à reprendre au besoin).
- [ ] Katana : les objets marchent toujours sur les failles (multitir, écho, foudre…).

**Refonte roguelike, étape 2 : les emplacements de la flèche (26 octobre ; GDD, section 6)**
- [ ] À l'ouverture de Unity : la console écrit « Refonte roguelike, étape 2 : les objets du catalogue rangés dans les emplacements de la flèche », avec la liste par emplacement ; dans `Data/Shop/ShopCatalog`, les objets ont leur *Slot* et *Max Stacks* à 3 (sauf ceux de la grenade).
- [ ] Les cartes d'objets disent l'emplacement et la rareté (« Pointe · Rare ») ; une pièce qu'on a déjà revient comme « Pointe · niveau 2 ».
- [ ] ~~Une autre pointe remplace la première~~ : depuis l'étape 5, elle va dans l'atelier (tests plus bas).
- [ ] Acheter deux fois la même pièce : « Multitir niveau 2 ! », et l'effet est plus fort (une flèche en plus à chaque tir) ; au niveau 3, elle n'est plus proposée.
- [ ] Coffres : au-dessus de l'orbe, « POINTE », « EMPENNAGE », « FÛT » ou « CORDE » (« OBJET » pour la grenade) ; sous le nom, le niveau (et depuis l'étape 5, le duo possible et « Rangée dans l'atelier ») ; la prendre fait pareil qu'en boutique.
- [ ] La ligne du bas de la boutique : « Pointe : … · Corde : … — Parchemins : … », mise à jour après chaque achat et après le coffre du boss.
- [ ] Raccourci **F** : toutes les pièces au niveau 3 (depuis l'étape 5 : la première de chaque emplacement équipée, les autres dans l'atelier), les objets de la grenade et 5 de chaque parchemin.
- [ ] Raccourci **Tab** (26 octobre au soir) : « Essai : les … pièces au niveau 1 » ; encore : niveau 2, puis 3 ; un quatrième appui : « déjà au niveau maximal ». L'atelier les montre toutes ; aucun parchemin ni tourelle donné.

**Refonte roguelike, étape 3 : les pièces nouvelles (26 octobre ; GDD, section 6 ; R pour les essayer une à une)**
- [ ] À l'ouverture de Unity : « Refonte roguelike, étape 3 : les pièces nouvelles ajoutées au catalogue » ; les 8 cartes sont à la fin de `Data/Shop/ShopCatalog`, avec leur emplacement et une icône.
- [ ] Corde triple : 3 flèches ; l'arc droit, l'éventail est à plat ; l'arc couché sur le côté, il est vertical (sinon, l'axe des branches est faux : le dire) ; avec les Akimbow, l'éventail suit l'inclinaison de chaque arbalète ; niveau 2 : 5 flèches.
- [ ] Empennage boomerang : la flèche fait demi-tour vers 22 m et revient vers la poitrine ; tendre la main sur son chemin la rattrape (« Rattrapée ! Tir parfait », une vibration), et le tir suivant est parfait ; manquée, elle passe et retombe derrière ; elle traverse un ennemi à l'aller et au retour. Le retour est-il rattrapable sans danger ni mal des transports ?
- [ ] Empennage télékinésie (guidage revu le 26 octobre) : tirer, puis garder la gâchette de la main de l'arc : la flèche file droit sans tomber (légère vibration) ; tourner la main à gauche, à droite, vers le haut ou le bas : la flèche tourne pareil, deux fois plus ; elle ne monte plus toute seule. Relâcher : elle reprend sa chute ; rappuyer (dans les 3 s) : elle repart de la main où elle est. Avec les Akimbow, la gâchette de l'arbalète qui a tiré. Sans casque : le clic droit maintenu, en tournant la vue. Assez sensible (sinon *k_SteerGain* en tête de `ArrowPieces.cs`) ?
- [ ] Pointe à mèche : la flèche plantée crépite (étincelles orange) puis explose 1 s après, plantée au sol comme dans un ennemi ; pas d'image lente avec beaucoup d'explosions (les effets sont réutilisés).
- [ ] Pointe de givre : un tir parfait gèle l'ennemi (figé, bleu, il n'attaque plus) 1,5 s, puis il repart et reprend ses couleurs ; un ennemi gelé touché éclate (« Éclat ! ») et blesse ses voisins ; un boss est seulement ralenti.
- [ ] Fût aimant : la flèche plantée attire les ennemis proches (un anneau violet se resserre), pas les boss ; avec la pointe à mèche : le groupe aspiré puis l'explosion.
- [ ] Fût conducteur : deux flèches plantées au sol à moins de 8 m sont reliées par un arc électrique qui scintille et blesse les ennemis qui le traversent ; avec la corde triple, un seul tir fait plusieurs arcs.
- [ ] Talisman d'acrobate : un saut aux bras ou une glissade ralentit le temps, et les tirs lâchés pendant ce temps sont parfaits ; le temps revient à l'atterrissage. Confortable ?
- [ ] Les pièces ne cassent rien au katana (ses failles gardent leurs effets ; la corde triple ajoute des failles).

**Refonte roguelike, étape 4 : l'arc qui raconte la partie (26 octobre ; GDD, section 6 ; R ou F pour avoir des pièces)**
- [ ] Prendre une flèche dans le carquois : la pointe brille de la couleur de la pièce de la pointe (un losange lumineux), l'empennage de celle de l'empennage (trois plumes), le fût porte trois runes de la couleur du fût ; rien sur la flèche sans pièce. Les mêmes en vol et plantées ; plus grands au niveau 2 et 3. Les runes se voient-elles (sinon, le fût est plus épais que prévu) ?
- [ ] La corde de l'arc brille de la couleur de la pièce de la corde.
- [ ] Avec le talisman d'acrobate : une breloque rose pend sous la poignée de l'arc, au bout d'un fil, se balance quand on bouge l'arc ; cachée avec les Akimbow ou le katana en main.
- [ ] Prendre une pièce (boutique, coffre, R) : un motif sonore par emplacement (pointe : tintements ; empennage : souffle ; fût : gong ; corde : corde pincée ; talisman : clochette), plus aigu au niveau suivant.
- [ ] Les sons d'action : le souffle du demi-tour du boomerang, le bourdonnement de la télékinésie pendant qu'on guide (il s'éteint en douceur), la mèche qui crépite, le ralenti de l'acrobate qui commence et qui finit.
- [ ] Lisible d'un coup d'œil ? Les couleurs se distinguent-elles (sinon, les changer en tête de `PieceLook.cs`) ? Pas d'image lente avec beaucoup de flèches décorées.
- [ ] En entrant dans la dimension des étoiles ou sur le toit cyberpunk (améliorations mises de côté), les flèches redeviennent nues ; elles reprennent leurs couleurs au retour.

**Refonte roguelike, étape 5 : deuxième lot, duos, atelier de la flèche, prix des parchemins (26 octobre ; GDD, sections 6 et 7 ; R ou F)**
- [ ] À l'ouverture de Unity : « Refonte roguelike, étape 5 : le deuxième lot de pièces ajouté au catalogue » ; les 16 cartes sont à la fin de `Data/Shop/ShopCatalog`. Pas d'erreur rouge dans la console.
- [ ] **L'atelier** : à la fin d'une vague, un panneau « L'atelier » à droite de la boutique, tourné vers toi. À gauche, la flèche du jeu en 3D, **debout**, la pointe en haut, qui tourne lentement sur elle-même avec ses pièces allumées ; la corde passe par l'encoche, la breloque du talisman pend à gauche du fût (grisées sans pièce). Un **sceau doré animé** (comme ceux des points faibles des boss) autour de la pointe, du fût, de l'empennage, de la corde et de la breloque, avec sa légende à gauche (icône, « POINTE », la pièce ou « Vide »). À droite de la flèche, les pièces de la partie choisie (3 colonnes).
- [ ] Les sceaux : ils tournent doucement ; survolé, un sceau s'éclaire et accélère ; celui de la partie choisie est plus grand, bat comme un cœur et lance une onde de temps en temps. Cliquer un sceau, puis une légende : la partie se choisit, son sceau s'emballe et lance une onde. Les sceaux tombent-ils bien sur les parties de la flèche (sinon, `Along` dans `ArrowWorkshop.cs`) ? Ceux de l'empennage et de la corde se touchent-ils ? La flèche est-elle bien devant le panneau, et éclairée ? Lisible à bout de bras ? Cliquable au rayon (et sans casque) ? Rentre-t-il dans un mur de la tour ? Pas d'image lente pendant que tout bouge ?
- [ ] Acheter une pièce pour un emplacement vide : elle s'équipe toute seule. Pour un emplacement pris : la carte disait « Rangée dans l'atelier (équipée : « … ») », le message dit « (rangée dans l'atelier) », l'ancienne pièce marche toujours, et l'atelier s'ouvre sur cet emplacement.
- [ ] Dans l'atelier, cliquer une autre pièce : « Sur la flèche : … », le motif de l'emplacement sonne, les couleurs de la flèche (celle qui tourne et celles du carquois) changent, et l'effet change (la pièce retirée ne marche plus, elle garde son niveau si on la remet).
- [ ] Les duos : avec le fût aimant, une carte de pointe à mèche dit « Duo possible : Trou noir » en doré ; l'équiper : « Duo : Trou noir ! », un accord, et la corde de l'arc bat d'or. Dans l'atelier, une pièce rangée qui formerait un duo dit « Duo : … ». Essayer les 6 (GDD, section 6).
- [ ] **Parchemins** : acheter 3 Force de suite : la Force monte bien plus vite que l'Adresse et la Vigueur. Seule la Vigueur fait monter les PV max (plus de « +5 PV max » sous la Force et l'Adresse).
- [ ] Les 16 pièces, une à une avec **R** : éclats derrière l'ennemi ; anneau doré de la marque (double dégâts, elle saute à sa mort) ; rebonds ; plume (plus de chute, l'aide à la visée droite) ; plomb (tir très courbe, onde de choc au sol) ; spectral (à travers un mur) ; pieu (les ennemis ralentis dessus) ; détonateur (tirer sur une flèche plantée : 6 flèches) ; suspendu (la flèche s'arrête ; la gâchette de l'autre main ou le clic droit la relâche) ; transe (« Transe 1… 5 », la flèche dorée) ; faucon (pleine tension : ralenti) ; sniper (dégâts plus forts de loin) ; pacte de verre (dégâts ×2, un tir rouge coûte 5 PV) ; ingénieur (une flèche dans sa baliste : elle tire deux fois plus vite) ; duelliste (un coup de flèche à la main : « Touché ! Tir parfait ») ; bourreau (une tête sous 25 % de PV : mort nette).
- [ ] Katana : la pointe de givre, de chasseur, le bourreau, la mèche et l'aimant marchent sur les failles.

**Refonte du tower defense, étape 2 : les caisses de l'ingénieur et les plans (27 octobre ; `Docs/Refonte_TowerDefense.md`)**
- [ ] À l'ouverture de Unity : la console écrit « Refonte du tower defense, étape 2 : 3 fenêtre(s) du tutoriel… » ; le tutoriel parle des caisses et des plans.
- [ ] Au début de l'accalmie : « 3 caisses de l'ingénieur sont apparues ! », là où poussaient les cristaux, sous un rayon **violet** ; la caisse (le coffre, teinté de violet en attendant son modèle) se distingue des coffres dorés.
- [ ] On l'ouvre comme un coffre (gâchette ou poignée) : ralenti, 3 orbes « CONSTRUCTION », 3 bâtiments différents : « 2 plans » pour la barricade et les piques, « 1 plan » pour la tour de givre et la baliste (« tu en as N » si on en a déjà).
- [ ] Prendre une orbe : « Pièges à piques : 2 plans » ; la montre affiche « Or … · Plans 2 ».
- [ ] Un socle : le panneau dit « Plans : N », chaque carte « Construction · 2 plans » et son prix « 1 plan » ; sans plan de ce bâtiment, le prix en rouge et « Il te faut un plan : … » au clic.
- [ ] Construire consomme un plan ; sur un bâtiment construit, le niveau suivant demande un autre plan du même bâtiment.
- [ ] Plus aucun gisement de cristaux, plus de « Mat. » sur la montre.
- [ ] Les caisses pas ouvertes disparaissent à la fin de la vague ; aucune pendant la vague de MK.
- [ ] Raccourcis : **B** donne 2 plans de chaque bâtiment ; **V** pose une caisse devant toi.

**Refonte du tower defense, lot 7 : les duos, le Boulet fou (31 octobre ; `Docs/Refonte_TowerDefense.md`, partie 12.16 ; B, H)**
- [ ] À l'ouverture de Unity, la console écrit « Duos : Boulet fou, prefab créé… », « …, duo du 31 octobre », « …, définition créée… » et « …, inscrit au catalogue » ; `Prefabs/Structures/Duos/Duo_Boulet_fou`, `Data/Duos/Duo_Boulet_fou` et `Resources/TowerDuos` existent. Les 4 anciens duos (Canon, Mur de glace, Rempart à ressort, Aurore boréale) ne sont plus là.
- [ ] **Fusionner** : une barricade et un tremplin au niveau 3 (B plusieurs fois pour les plans, H pour finir une construction), entre les vagues ; « Déplacer » sur l'un, viser l'autre : l'hologramme devient doré et montre le Boulet fou, « Duo : Boulet fou » ; à un niveau trop bas, rouge, « Les deux tours au niveau 3 au moins ». La gâchette : « Duo : Boulet fou ! » ; les deux tours d'avant disparaissent, le duo est sur la voie. Avec une tour au niveau 5, il naît au niveau 2 ; les deux au niveau 5, au niveau 3.
- [ ] Le panneau du duo : « Duo · Boulet fou · niveau 1 », sa carte d'amélioration (4 plans, de barricade ou de tremplin) et « Déplacer » ; un plan de barricade ou de tremplin sorti de la montre et visé sur le duo le fait monter.
- [ ] **Le Boulet fou** : la roue tourne en travers de la voie, du côté de la voie voisine ; un ennemi frappé part en cloche et retombe sur la voie voisine, puis repart vers la tour par elle ; sans voie voisine à portée, il part sur le côté. Le souffle du boulet à chaque tour, un choc à chaque coup, le boulet s'écrase un instant. Niveau 2 : plus vite, l'ennemi lancé renverse ceux sur qui il retombe.
- [ ] **Boulet fou, après le premier essai** : la console écrit « Duos : Boulet fou, duo 2 du 31 octobre » et « …, textes des niveaux revus » ; la tour est bien plus grande (10 m au niveau 3), fanions dorés ; les ennemis partent plus loin (et retombent en arrière sur la voie voisine) ; au niveau 3, l'ennemi file à plat jusqu'au mur d'en face, s'y écrase (une grosse part de ses PV, petite explosion) et renverse ceux qui sont autour.
- [ ] Déplacer le duo entre les vagues : son hologramme est le Boulet fou ; le viser avec une autre tour : « Déjà un duo ».

**Refonte du tower defense, lot 3 : l'ennemi préparé (30 octobre ; `Docs/Refonte_TowerDefense.md`, partie 12.12 ; B, F3)**
- [ ] À l'ouverture de Unity, la console écrit « Ennemi préparé : 8 sons donnés au Score Manager, dans la scène « Prototype_Tir » » : enregistrer la scène (Ctrl+S).
- [ ] **F3** projette les ennemis proches : une flèche sur l'un d'eux en l'air écrit « Au vol ! » avec un son, et fait deux fois plus de dégâts (un ennemi qui survivait à une flèche au sol en meurt-il plus vite ?).
- [ ] Chaque sorte a son mot et son son : « Brisé ! » (givre N3, le pic de glace), « Cloué ! » (baliste N4, le harpon), « Au filet ! » (un volant tombé du lance-filet), « Sonné ! » (la herse de la barricade N4, le filet électrique N4, un ennemi qui se relève d'une chute), « Englué ! » (le goudron du mortier N4), « Attiré ! » (la tour-aimant), « Marqué ! » (un ennemi dans le rayon du phare).
- [ ] Le katana et ses failles aussi ; les tirs des bâtiments n'écrivent rien ; la flèche de glace du joueur ne prépare pas.
- [ ] Les dégâts suivent la difficulté à préparer (31 octobre) : ×2 en l'air et gelé, ×1,75 cloué, ×1,5 au filet et sonné, ×1,3 englué, ×1,25 attiré, ×1,15 marqué. Le phare (tout ennemi de son rayon, +15 %) : est-ce encore trop facile ?

**Refonte du tower defense, deuxième brainstorm, lot 2 : les 5 niveaux, le tremplin et les 9 autres tours (29 octobre ; `Docs/Refonte_TowerDefense.md`, partie 12.10)**
- [ ] À l'ouverture de Unity : « Évolutions des tours : Tremplin passe à 5 niveaux… » (pas de Ctrl+S : prefab et réglage). Le prefab `Structures/Tremplin` a les pièces « Niveau 4 » (sous « Bras ») et « Niveau 5 » ; le réglage, 5 niveaux avec leurs textes.
- [ ] Les autres tours restent à 3 niveaux pour l'instant (« Niveau maximal atteint » au niveau 3).
- [ ] **B** quatre fois (8 plans de tremplin), pose un tremplin sur la voie (**H** finit la construction) ; viser le tremplin avec un plan : « Améliorer : niveau 2 », puis 3 ; au niveau 4, « Améliorer : niveau 4 (2 plans) » ; avec un seul plan, en rouge « il faut 2 plans ». Le panneau du tremplin dit « 2 plans » pour les niveaux 4 et 5.
- [ ] N2 : les ennemis montent plus haut. N3 : le bras de bois bascule, l'ennemi s'envole vers un groupe plus loin sur la voie et le renverse (explosion, fumée). N4 : trois ennemis d'un coup (trois godets). La grille : le tremplin occupe environ 3 × 5 cases (la console le dit).
- [x] N3 et N4 (la catapulte) : validés au casque le 29 octobre.
- [x] **N5, la rampe de lancement** (revue après le premier essai, 29 octobre ; validée : « c'est ce genre d'effet que je veux » pour les niveaux 5) : les ennemis sur la rampe partent dans le ciel **dix fois plus haut** (370 m : une colonne de feu de plus de 200 m), et retombent **5 s** plus tard, en météores, **sur le groupe le plus serré** (à 30 m au plus ; plusieurs ennemis lancés se partagent les groupes) ; un sifflement juste avant l'impact ; ils **meurent à l'atterrissage** et blessent et renversent ceux qu'ils écrasent (explosion, fumée). Sans ennemi à viser, ils retombent vers la forêt. La console à l'ouverture : « texte du niveau 5 de Tremplin revu ». C'est drôle ? Un boss qui passe dessus recule et perd 10 % de ses PV.
- [ ] Un ennemi touché par une flèche en plein vol meurt en l'air, mais retombe quand même avec sa traînée, et son impact blesse ceux d'en dessous.
- [ ] **Le tremplin ne scintille plus** (retour du 29 octobre) : à l'ouverture de Unity, la console écrit « Évolutions des tours : le tremplin ne scintille plus (la flèche au-dessus de la plaque, 8 rayures autour du liseré…) » (pas de Ctrl+S : c'est un prefab). Dès le niveau 2, la flèche de bois dépasse de la plaque de métal sans clignoter ; au niveau 5, les rayures jaunes et noires ne clignotent plus avec le liseré vert. S'il reste deux pièces qui scintillent, dire lesquelles.
- [ ] **Le bouton « Plans » au-dessus du cadran de la montre** (retour du 29 octobre ; avant, en bas à gauche), le bouton « Tour » dessous ; toucher l'un ne déclenche pas l'autre.
- [ ] **Les 9 autres tours à 5 niveaux** (29 octobre au soir) : à l'ouverture de Unity, la console écrit « Évolutions des tours : … passe à 5 niveaux » pour Barricade, Pieges, Baliste, Mortier, Givre, Filet, Aimant, Bulle et Phare (pas de Ctrl+S : prefabs et réglages), sans erreur rouge. Pour chacune : **B** quatre fois, la poser, **H** à chaque niveau ; vérifier qu'elle tient toujours sur la grille (la console donne ses cases).
- [ ] Barricade : N3 le boulet se balance devant le mur et renverse ; N4 la herse tombe sur ceux qui frappent, et une flèche tirée à travers le mur prend une traînée dorée et traverse plusieurs ennemis ; N5 le mur de pierre et les tours de guet qui tirent, et l'onde qui renvoie les assaillants en vol.
- [ ] Piques : N2 les ennemis continuent de perdre des PV après le piège ; N3 deux fois plus large ; N4 la mine saute au premier passage (ou sous une flèche), revient 20 s plus tard ; N5 la mâchoire : des pieux de 4 m sur 10 m de voie, les petits meurent, les autres volent.
- [ ] Baliste : N3 deux carreaux, deux cibles ; N4 la cible s'arrête 1 s, un volant tombe ; N5 le pieu géant toutes les 6 s, qui emporte les ennemis. Le pieu et le deuxième arc tournent bien avec la tourelle.
- [ ] Mortier : N2 les petites explosions autour ; N4 la flaque noire qui ralentit, qu'une flèche enflammée embrase ; N5 la pluie de 12 obus le long de la voie, le sol en feu, les ennemis qui volent. Pas d'à-coup sur le Quest ?
- [ ] Tour de givre : N3 un ennemi pris dans un bloc de glace (figé) ; une flèche le brise ; N4 des ennemis qui glissent et tombent ; N5 la sphère de glace et tous les ennemis en statues : une flèche les fait voler en éclats.
- [ ] Lance-filet : N4 filet bleu électrique, et sans volant il prend un ennemi au sol ; N5 la toile géante au-dessus de la voie, les volants tombent.
- [ ] Tour-aimant : N2 les volants tombent ; N3 l'attraction continue ; N4 une flèche tirée près des ennemis attirés se courbe vers eux ; N5 le trou noir, la boule d'ennemis qui tournoie puis qui explose en l'air.
- [ ] Bulle : N3 une grosse bulle autour de 2 ou 3 ennemis ; N4 une flèche dans une bulle : un éclair blanc crève les autres, de bulle en bulle ; N5 l'orbe qui emporte tout un groupe à 30 m : crevée, ils meurent en retombant.
- [ ] Phare : les bâtiments de son rayon font plus de dégâts dès le N1 ; N4 une flèche qui traverse le faisceau s'enflamme ; N5 la colonne de lumière du ciel toutes les 10 s, et les bâtiments autour qui tirent deux fois plus vite.
- [ ] **Retours du premier essai** (29 octobre au soir) : à l'ouverture de Unity, la console écrit « … retouché après les essais du 29 octobre au soir » pour Baliste, Mortier, Filet, Barricade, Phare, Givre et Pieges, et « le phare se construit aussi au bord de la voie ».
- [ ] Barricade de niveau 2 et plus : les ennemis qui la frappent meurent normalement de ses pointes ; plus aucun ennemi figé, intouchable devant elle ou après sa chute.
- [ ] Les piques (et tout bâtiment) se posent avec l'emprise de leur niveau 1 ; plus de cases rouges sur les petits cailloux et les bosses de la prairie ; la grille se voit loin (36 m) ; le menu des plans reste collé devant toi quand tu tournes la tête.
- [ ] Bulle : une flèche dans la bulle (sans toucher l'ennemi) la crève et le blesse ; N5 l'orbe ne fait plus planter le jeu.
- [ ] Piques N5 : des pieux fins sur 20 m de voie. Givre N4 : les ennemis tournoient en glissant ; N5 : 10 s de gel, un boss gèle aussi.
- [ ] Mortier : sur sa tour, plus grand ; N4 la flaque noire (pas bleue) s'embrase sous n'importe quelle flèche ; N5 cinq tubes côte à côte qui tournent ensemble.
- [ ] Baliste : N3 deux balistes l'une sur l'autre ; N4 harpons et treuil ; N5 la baliste géante en haut, le pieu part de sa bouche. Tout tourne avec la tourelle.
- [ ] Lance-filet : les filets ressemblent à des filets (en vol, sur un volant, la toile géante) ; N5 le canon à filet.
- [ ] **Modèles des kits** (deuxième retouche) : la console écrit « …, retouche 2 du 29 octobre » pour Barricade, Baliste, Mortier, Filet, Aimant, Bulle et Phare. Regarder l'orientation de chaque modèle (herse face à la forêt, baliste de siège qui tire vers l'avant, bannières, charrette, fontaine autour du chaudron).
- [ ] **L'allure des tours (30 octobre)** : la console écrit « Allure des tours : <tour>, nouvelle allure et palette » pour les 10 tours (rien à enregistrer dans la scène). Chaque tour a sa couleur ; aucune pièce ne scintille ni n'en traverse une autre, à aucun niveau (F2, monter chaque tour au niveau 5).
- [ ] Barricade : la herse de face, ajourée ; au niveau 5, deux tours fines à bannière rouge, leur petite baliste se tourne vers sa cible et tire.
- [ ] Baliste : au niveau 3, la charpente reste droite quand la tourelle vise en haut ou en bas ; au niveau 4, harpons et treuil ; au niveau 5, la baliste de siège tire vers l'avant, sa plate-forme ne tourne pas.
- [ ] Mortier : environ 3 m de haut ; les tubes ne traversent ni les créneaux ni les petits mortiers des coins en tournant.
- [ ] Phare : les deux rayons partent de la lanterne ; plus de lampadaires ; il se pose sur 2 × 2 cases.
- [ ] Lance-filet : nid sur pilotis, échelle côté joueur, la tourelle dépasse des créneaux ; niveau 4, paratonnerre de cuivre (plus de groupe électrogène). Aimant : socle carré, condensateurs au niveau 3.
- [ ] Givre : les cristaux grossissent moins, le pic flotte au-dessus. Pièges : cadre de poutres, crocs de fer au niveau 5. Bulle : eau bleu-vert dans la fontaine.
- [ ] **La rivière** : un ennemi retombé d'une bulle dans la rivière repart ; on peut le toucher.
- [ ] **Plus de plantage du goudron** (30 octobre au soir) : mortier niveau 4, enflammer une flaque de goudron avec plusieurs ennemis dessus jusqu'à ce qu'elle en tue ; pas d'erreur « Collection was modified » dans la console. **Phare** : des ennemis marqués (lueurs sur leurs points faibles) qui meurent, puis l'arrêt de la partie : pas d'erreur « Cannot set the parent of the GameObject 'Lueur du phare' » ; les lueurs suivent bien les têtes.
- [ ] La console écrit « Allure des tours : Baliste, allure 2 du 30 octobre » : le plancher sous chaque arc ne scintille plus.
- [ ] Forteresse : de grandes tours de guet. Phare : posé au bord de la voie ; les deux rayons dans le bon sens ; N4 l'anneau de feu au-dessus de la voie près de ta tour, une flèche qui le traverse s'enflamme.

**Refonte du tower defense, deuxième brainstorm, lot 1 : la tour et les 10 tours (29 octobre ; `Docs/Refonte_TowerDefense.md`, partie 12.7)**
- [ ] À l'ouverture de Unity : « Tower defense : 17 bâtiment(s) retiré(s) du jeu (…) ; restent 10 tours » (pas de Ctrl+S : ce sont des réglages). Dans `Data/Structures`, la case *Retired* est cochée sur les 17 (arbalète, foudre, lance-flammes, miroir, catapulte, soufflerie, pendule, geyser, trappe, les trois anneaux, mine, goudron, baliste sur la tour, esprit gardien, seau d'huile), pas sur les 10.
- [ ] Caisses de l'ingénieur : seulement barricade, piques, baliste, tour de givre, lance-filet (communes, 2 fois sur 3) et tremplin, mortier, tour-aimant, bulle, phare (rares) ; plus de légendaire. **B** : 2 plans de chacune des 10 seulement ; le menu des plans n'a que celles-là.
- [ ] Pendant la pose, plus de grands carrés d'emplacements sur la tour (ils servaient au seau d'huile et à la baliste sur la tour) ; la console dit « 0 emplacement(s) sur la tour ».
- [ ] **La tour abîmée** : à la fin de la vague, elle ne regagne que 15 % de ses PV max (le message dit « Tour : +450 PV » pour 3 000 PV) ; les barricades, elles, reviennent à tous leurs PV.
- [ ] **La tour détruite** : à la fin de la vague, elle reste en ruines (« La tour reste en ruines ») et ne se relève plus de la partie ; l'or de fin de vague reste divisé par deux, le téléporteur ne marche plus, le gong reste au sol (on lance encore les vagues). Réglage : *Tower Repair Share* du `Wave Manager` (0,15).

**Refonte du tower defense : la pose revue après les premiers tests (28 octobre ; `Docs/Refonte_TowerDefense.md`, étape 3)**
- [ ] À l'ouverture de Unity : « Refonte du tower defense, étape 5 : prefab Arbalete (petite baliste à la place de l'arbalète), prefab Filet (…) » ; l'arbalète à répétition et le lance-filet ont une petite baliste du Tower Defense Kit (pointée vers l'avant, l'axe bleu de « Tourelle »), avec un chargeur de carreaux pour l'arbalète ; les carreaux partent du bout. Pas de Ctrl+S nécessaire (ce sont des prefabs).
- [ ] Au lancement : la console écrit « BuildGrid : … × … cases de 1 m ; constructibles : … sur la voie (dont … sur sa terre), … au bord, … en hauteur ; … emplacement(s) sur la tour » ; aucune erreur rouge, pas d'à-coup (la grille se fait en quelques secondes). « dont 0 sur sa terre », ou l'avertissement « aucune case sur de la terre » : la terre des voies n'est pas reconnue, le dire à Claude.
- [ ] **La grille calée sur les voies** (deuxième retour du 28 octobre au soir) : avec une baliste, toute la terre des voies est rouge, jusqu'à son bord, **voie du milieu comprise** (8,5 m de large, en diagonale : la grille y fait des marches d'une case), et l'herbe verte commence juste après, sur 4,5 m. Plus de terre verte au bord de la voie, plus d'herbe rouge dans les virages. Avec une barricade : l'inverse (verte sur toute la terre). Les cases à cheval sur le bord de la terre sont rouges pour les deux.
- [ ] **On construit sur la rivière** (troisième retour du 28 octobre au soir) : dans la clairière, le lit de la rivière est vert pour une baliste comme l'herbe autour (plus de bande rouge) ; le bâtiment s'y pose à la hauteur de l'eau, même à cheval sur une berge. Sur les terrasses, le lit compte « en hauteur ». Là où la rivière traverse une voie (les gués), c'est la voie : rouge pour une baliste, vert pour une barricade ou des piques.
- [ ] **L'ancien système sort de la scène** : à l'ouverture de Unity, la console écrit « Ancien système de construction : 12 socle(s), 12 cercle(s) bleu(s) au sol, 3 barricade(s) de départ et le groupe « Build Sockets » supprimés de la scène… » ; enregistrer la scène (Ctrl+S). En jeu : plus aucun cercle bleu au sol aux anciennes places des tourelles, plus de barricade en travers des voies au départ.
- [ ] **La barricade, plus étroite** (retour du 29 octobre) : à l'ouverture de Unity, la console écrit « Tower defense : la barricade passe de 6 à 4 m de large (4 cases au lieu de 6)… » (pas de Ctrl+S : c'est un prefab). À la pose, « BuildGrid : Barricade occupe 4 × 2 cases ». Elle se pose sur les voies de gauche et de droite comme sur celle du milieu, et arrête toujours les ennemis de toute la largeur de la voie (ils s'arrêtent à 5 m devant elle, même sur les côtés).
- [ ] **Le Phare, à sa vraie taille** (29 octobre) : à la pose, « BuildGrid : Phare occupe 3 × 3 cases » (6 × 3 avant, à cause de son faisceau) ; dans le menu des plans, son aperçu montre la tour à une taille normale. Pendant la construction, pas de faisceau (seule la tour est en plan bleu) ; une fois construit, le faisceau s'allume et tourne (deux rayons au niveau 3). Le marteau ne frappe plus dans le vide à 6 m de la tour.
- [ ] **Le gué de droite** : à l'ouverture de Unity, la console écrit « Gué de droite : la zone qui ralentit les ennemis passe de x = 25.5 à x = 19.5, sur la rivière… » ; enregistrer la scène (Ctrl+S). En jeu, les ennemis de la voie de droite ralentissent dans l'eau, plus 6 m après.
- [ ] Bouton « Plans » de la montre : le **menu des plans** s'ouvre devant toi, un peu au-dessus du regard, **et l'hologramme du bâtiment du milieu suit déjà le rayon** (le premier la première fois, puis le dernier choisi) ; une carte par bâtiment dont tu as des plans : aperçu 3D qui tourne, nom, « ×N », zone (Voie, Bord, Tour, Partout), bordure grise (commune), bleue (rare) ou dorée (légendaire). Dessous : la carte du milieu décrite (rareté, où, cases, plans, effet du niveau 1). Le bouton de la montre dit « Fermer ». Le menu ne cache pas le sol que tu vises.
- [ ] Joystick de la main qui porte la montre, **gauche/droite** : les cartes défilent sur la largeur (un petit son au bout de la rangée) et l'hologramme change de bâtiment aussitôt ; le joueur ne tourne pas. **Une seule gâchette : posé.** Poignée : tout se ferme. Toucher une carte avec une manette, 0,3 s (une barre se remplit) : l'hologramme prend ce bâtiment. La main qui vient de toucher la montre ne change rien par accident.
- [ ] **Le menu flotte** : il reste où il est quand tu bouges un peu la tête ; tourne-toi franchement (plus de 30°) ou éloigne-toi : une demi-seconde après, il glisse en douceur jusque devant toi (plus de menu collé au bord de la vue).
- [ ] Pendant le menu et la pose, **la grille se voit au sol** autour de toi : cases de 1 m bordées de lignes nettes, **vertes** là où le bâtiment se pose, **rouges** ailleurs (pas la bonne zone, pente, eau, sous un arbre, au pied de la tour, place prise) ; elle épouse le terrain et s'efface vers le bord. Elle change quand la carte du milieu change.
- [ ] Le bâtiment choisi : son hologramme se cale sur la grille, ses cases brillent (vertes s'il se pose, rouges sinon) ; il ne déborde plus sur la voie depuis le bord, ni l'inverse. Les emplacements de la tour se dessinent en grands carrés (verts pour le seau d'huile et la baliste sur la tour).
- [ ] Joystick **haut/bas** : tourne par crans de 45° (les cases suivent), en gardant le sens quand on change de bâtiment. Gâchette : posé, puis les marteaux, et le menu se ferme. Poignée ou bouton de la montre retouché : tout se ferme. Pour un **déplacement** (panneau d'un bâtiment) : pas de menu, tout le joystick tourne, poignée : « Déplacement annulé ».
- [ ] La console donne une fois, par bâtiment posé, « BuildGrid : … occupe N × M cases » : à juger ; le réglage *Footprint* de `Data/Structures/…` le change.
- [ ] Deux bâtiments ne se chevauchent pas ; un bâtiment déplacé rend ses cases (elles redeviennent vertes) ; on peut le reposer sur sa propre place, tourné.
- [ ] Améliorer : viser un bâtiment du même type (ou une de ses cases) : « Améliorer : niveau 2 ».
- [ ] Sans casque : **F2** ouvre le menu, le bâtiment du milieu suit déjà le viseur ; molette : changer de bâtiment, **Maj** + molette : tourner, **E** : poser ; **F2** ferme.
- [ ] **F** : 6 balistes et tours de givre niveau 2 au bord de la voie, les plus proches de la tour, espacées, sans chevauchement.
- [ ] **Baliste sur la tour** : elle tire sur les ennemis collés au pied de la tour (elle pique vers le bas, jusqu'à 80°), à travers le bord de la plate-forme.
- [ ] À l'ouverture de Unity : « Refonte du tower defense, étape 2 : 1 fenêtre(s) du tutoriel… » ; Tutoriel, Construction, « Choisir un bâtiment » parle du menu des plans et de la grille verte et rouge (plus des runes), et du joystick qui change de bâtiment (plus de gâchette pour choisir). Les vidéos de la section montrent encore l'ancienne pose.

**Refonte du tower defense, étape 3 : construire n'importe où (27 octobre ; `Docs/Refonte_TowerDefense.md`)** (la pose a changé le 28 octobre, bloc précédent : les lignes sur les runes, le choix au joystick seul et F2 ne valent plus)
- [ ] Au lancement : la console écrit « BuildGrid : N emplacements de construction (… sur la voie, … au bord, … en hauteur) » ; aucune erreur rouge, pas d'à-coup.
- [ ] Les 12 anciens socles et les 3 barricades de départ ont disparu (depuis le 28 octobre au soir, ils sont supprimés de la scène, avec les cercles bleus des socles).
- [ ] Rien au sol hors de la pose ; le rayon de la main ne s'accroche pas au sol, et le viseur sans casque ne propose pas « Construire » partout.
- [ ] Avec des plans (**B**) : un bouton « Plans N » sur la montre, à côté du bouton « Tour » (assez loin pour ne pas toucher les deux à la fois ; le toucher ne doit pas téléporter en haut de la tour).
- [ ] Le toucher avec l'autre main (0,3 s) : l'hologramme bleu d'un bâtiment suit le rayon de la main qui porte la montre, calé sur les cases ; une étiquette au-dessus (nom, plans, « Pas ici : … » en rouge) ; il rougit là où il ne se pose pas. Le bouton dit « Annuler ».
- [ ] Pendant la pose, autour de toi : runes **orange** sur les voies (barricade, piques), **bleues** au bord, **vertes** sur les terrasses (baliste, tour de givre) ; aucune dans l'eau, sous un arbre, au pied de la tour, sur un mannequin, ni sur l'emplacement d'une caisse ou d'un coffre. Celle visée brille plus.
- [ ] Joystick de la main qui vise : gauche/droite tourne l'hologramme par crans de 45° (et **ne fait pas tourner le joueur**) ; haut/bas passe à un autre plan. Après la pose, la rotation du joueur au joystick revient.
- [ ] Gâchette : le bâtiment est posé, tourné comme l'hologramme, puis les marteaux comme avant ; un plan de moins. Poignée : « Pose annulée ».
- [ ] L'hologramme sur un bâtiment du même type : « Améliorer : niveau 2 », la gâchette lance l'amélioration.
- [ ] Une barricade posée sans la tourner a l'avant vers la forêt : les ennemis s'arrêtent devant pour la frapper. Tournée de travers, elle les arrête mal (attendu). Les tourelles regardent la voie.
- [ ] **Déplacer** : entre les vagues, viser un bâtiment, carte « Déplacer » (gratuit) : son hologramme suit le rayon ; gâchette sur un emplacement libre de la même sorte : il s'y pose ; sur sa propre place : il y reste, tourné ; une autre sorte : rouge, « Pas ici… ». Pendant une vague, la carte dit « Entre les vagues seulement ».
- [ ] Sans casque : **F2** sort un plan (puis le suivant, puis le range), la molette tourne, **E** pose ; le viseur dit ce qui va.
- [ ] **F** : 6 tourelles niveau 2 sur les emplacements au bord de la voie les plus proches de la tour, espacées.
- [ ] Tutoriel, Construction, « Choisir un bâtiment » : parle des runes, de la hauteur et du déplacement.
- [ ] À juger : la carte a-t-elle assez d'emplacements, aux bons endroits ? (Seuils réglables sur le composant *Build Grid*, ajouté à l'objet `Game` en jeu.)

**Refonte du tower defense, étape 5, lot 6 : emplacements sur la tour, seau d'huile, baliste sur la tour, esprit gardien (28 octobre ; `Docs/Refonte_TowerDefense.md`)**
- [ ] À l'ouverture de Unity : la console cite les prefabs `SeauHuile`, `BalisteTour`, `Esprit` ; enregistrer la scène (Ctrl+S).
- [ ] Au lancement, la console dit « … N sur la tour » (2 ou 3 attendus) ; avec un plan du seau ou de la baliste, des runes dorées apparaissent sur la plate-forme, à côté de toi, pas devant.
- [ ] **Seau d'huile** : la cible est dorée ; la toucher (flèche tirée ou tenue en main) fait basculer le seau, un filet d'huile coule au pied de la tour, les ennemis autour perdent des PV et ralentissent ; la cible devient grise, puis redevient dorée.
- [ ] **Baliste sur la tour** : elle tire seule, de haut, et ses carreaux traversent ; elle ne bouche pas la vue.
- [ ] **Esprit gardien** : le fantôme quitte son autel, fond sur un ennemi seul et le frappe (éclairs verts), puis sur le suivant ; il revient quand il n'y a plus personne.
- [ ] Caisses : de temps en temps une légendaire, toujours seule.
- [ ] À juger : la place des emplacements sur la tour (réglages *Tower Slot…* du *Build Grid*), les chiffres, la rareté du seau.

**Refonte du tower defense, étape 5, lot 5 : lance-flammes, phare, miroir, anneaux élémentaires (28 octobre ; `Docs/Refonte_TowerDefense.md`)**
- [ ] À l'ouverture de Unity : la console cite les prefabs `LanceFlammes`, `Phare`, `Miroir`, `AnneauFeu`, `AnneauFoudre`, `AnneauDouble` et le maillage `Anneau` ; enregistrer la scène (Ctrl+S).
- [ ] Ouvrir chaque prefab : tailles, « Niveau 2 » et « Niveau 3 » ; les anneaux sont visibles des deux côtés ; la glace du miroir a sa boîte de collision (verte) autour du miroir.
- [ ] **Lance-flammes** : un jet de feu vers la voie quand des ennemis passent devant ; ils brûlent (flammes sur eux, PV qui baissent sur 3 s).
- [ ] **Phare** (en hauteur) : la tête des ennemis proches porte une petite lueur jaune ; le faisceau tourne ; un tir à la tête fait plus de dégâts.
- [ ] **Miroir** : il se tourne vers la tour ; une flèche tirée dessus repart vers un ennemi proche (traînée blanche) ; sans ennemi, elle rebondit.
- [ ] **Anneaux** (posables partout) : ils se tournent vers toi ; une flèche qui passe dedans prend sa couleur, et : feu, la cible brûle ; foudre, un éclair saute vers d'autres ennemis à l'impact ; double, une deuxième flèche part à côté.
- [ ] À juger : trois anneaux (un par effet) ou un seul ; les chiffres.

**Refonte du tower defense, étape 5, lot 4 : trappe, pendule, bulle, lance-filet (28 octobre ; `Docs/Refonte_TowerDefense.md`)**
- [ ] À l'ouverture de Unity : la console cite les prefabs `Trappe`, `Pendule`, `Bulle`, `Filet` et le matériau `Bulle` (transparent) ; enregistrer la scène (Ctrl+S).
- [ ] Ouvrir chaque prefab : tailles, objets « Niveau 2 » et « Niveau 3 » ; le boulet du pendule pend sous la poutre, à hauteur de taille ; l'arbalète du lance-filet pointe le long de l'axe bleu (Z) de « Tourelle ».
- [ ] **Trappe** : les battants s'ouvrent sous les ennemis, qui réapparaissent plus loin en arrière sur la voie et repartent après 1 s.
- [ ] **Pendule** : le boulet se balance en travers de la voie ; un ennemi touché perd des PV et vole sur le côté.
- [ ] **Bulle** : l'ennemi le plus proche monte dans une bulle et flotte ; une flèche la crève, il tombe (dégâts de chute, « Au vol ! » si on le touche en l'air) ; sinon elle crève seule.
- [ ] **Lance-filet** : il se tourne vers un volant, le filet part, le volant tombe au sol et y reste quelques secondes, puis il redécolle. Aucun tir sans volant.
- [ ] À juger : les chiffres et les raretés proposées (lance-filet commun ; trappe, pendule et bulle rares).

**Refonte du tower defense, étape 5, lot 3 : geyser, soufflerie, tour-aimant, catapulte (28 octobre ; `Docs/Refonte_TowerDefense.md`)**
- [ ] À l'ouverture de Unity : la console cite les prefabs `Geyser`, `Soufflerie`, `Aimant`, `Catapulte` ; enregistrer la scène (Ctrl+S).
- [ ] Ouvrir chaque prefab : tailles, objets « Niveau 2 » et « Niveau 3 » ; la soufflerie a ses pales tournées vers l'avant (axe Z), la colonne d'eau du geyser est un cylindre bleu (cachée en jeu, sauf pendant un jet).
- [ ] **Geyser** : un groupe passe dessus → tous sautent à la verticale avec une colonne d'eau, retombent sur place ; recharge, puis le suivant.
- [ ] **Soufflerie** : les pales s'emballent ; les ennemis devant elle s'arrêtent ou reculent vers la forêt, les volants sont déviés ; puis une pause.
- [ ] **Tour-aimant** : des éclairs violets vers les ennemis proches, qui glissent vers elle ; un groupe se resserre.
- [ ] **Catapulte** : elle bascule, l'ennemi le plus proche s'envole et retombe sur un groupe (dégâts autour) ; seul, il est renvoyé vers la forêt.
- [ ] À juger : les chiffres, et les raretés proposées (geyser et soufflerie communs, aimant et catapulte rares) ; la catapulte qui lance sans « prendre » l'ennemi.

**Refonte du tower defense, étape 5, lot 2 : arbalète à répétition et mortier (28 octobre ; `Docs/Refonte_TowerDefense.md`)**
- [ ] À l'ouverture de Unity : « Refonte du tower defense, étape 5 : prefab Arbalete, … réglage Mortier ; … bâtiment(s) ajouté(s) au Build Manager » ; enregistrer la scène (Ctrl+S).
- [ ] Prefab `Arbalete` : l'arbalète pointe le long de l'axe bleu (Z) de « Tourelle » (sinon tourner l'objet « Arbalète » et replacer *Muzzle Offset* au bout) ; « Niveau 2 » ajoute une arbalète, « Niveau 3 » une troisième et un fanion.
- [ ] Prefab `Mortier` : le canon levé vers le ciel sur son socle ; « Bouche » au bout du tube ; boulets au niveau 2, deuxième tube au niveau 3.
- [ ] **Arbalète à répétition** : des rafales de 3 carreaux ; elle se tourne d'abord vers un volant s'il y en a un à portée.
- [ ] **Mortier** : l'obus monte en cloche et tombe sur un groupe (là où il sera) ; explosion, dégâts autour ; rien sur un ennemi à moins de 6 m ; au niveau 3, deux obus par salve.
- [ ] À juger : les chiffres, et s'il faut un son de tir au mortier.

**Refonte du tower defense, étape 5, lot 1 : raretés, mine, goudron, tremplin, tour de foudre (28 octobre ; `Docs/Refonte_TowerDefense.md`)**
- [ ] À l'ouverture de Unity : « Refonte du tower defense, étape 5 : … bâtiment(s) ajouté(s) au Build Manager » ; enregistrer la scène (Ctrl+S). Les prefabs `Mine`, `Goudron`, `Tremplin`, `Foudre` dans `Prefabs/Structures`, leurs réglages dans `Data/Structures`.
- [ ] Ouvrir chaque prefab : modèles à la bonne taille, posés au sol, centrés ; les objets « Niveau 2 » et « Niveau 3 » montrent la progression (à retoucher à la main si besoin).
- [ ] **B** puis la montre : les 27 bâtiments (lots 1 à 6 compris) défilent au joystick (haut, bas) ; l'hologramme de chacun.
- [ ] Caisses : des lots de 3 mines, 2 goudrons, et plus rarement un tremplin ou une tour de foudre.
- [ ] **Mine** : explose sous le premier ennemi au sol (explosion de la flèche explosive), blesse ceux autour, disparaît ; revient au début de la vague suivante ; niveau 3 : les ennemis volent.
- [ ] **Goudron** : la flaque noire ralentit les ennemis qui la traversent ; plus grande à chaque niveau.
- [ ] **Tremplin** : un ennemi qui passe dessus est projeté vers la forêt, la planche rebondit ; recharge, puis le suivant.
- [ ] **Tour de foudre** : un éclair part du sommet et saute entre 3 ennemis (volants compris) ; plus haute à chaque niveau.
- [ ] À juger : les chiffres (dégâts, rayons, recharges) et les aspects des niveaux.

**Refonte du tower defense, étape 4 : les ennemis projetés (28 octobre ; `Docs/Refonte_TowerDefense.md`, partie 6.2 ; F3 pour essayer)**
- [ ] Pendant une vague, **F3** près d'ennemis au sol : ils sautent en cloche, poussés loin de toi, puis retombent et repartent sur la voie après un court arrêt (0,5 s). Aucune erreur rouge dans la console.
- [ ] Un ennemi qui en percute un autre en l'air : les deux perdent des PV, l'autre fait un petit saut.
- [ ] Projeté contre une falaise : il rebondit un peu, tombe, reste étourdi plus longtemps.
- [ ] Retombé dans la rivière : pas de dégâts de chute, il y avance plus lentement.
- [ ] Retombé sur une terrasse de la jungle : il revient sur la voie la plus proche (il ne reste pas bloqué).
- [ ] Tué en l'air : le corps finit sa chute au sol (il ne reste pas suspendu).
- [ ] Un boss : F3 le pousse un peu, sans le soulever ; les volants ne bougent pas.
- [ ] **Tir au vol** : une flèche sur un ennemi en l'air affiche « Au vol ! » et rapporte plus de points.
- [ ] Fin de vague pendant qu'un ennemi vole : il retombe puis s'enfuit comme les autres.

**Nouveautés du 19 au 23 octobre : à vérifier en premier**
- [ ] Départ : quelle que soit ta place dans la pièce, tu apparais en haut de la tour, tourné vers le menu ; remettre le casque ou le recentrer avant la première vague t'y ramène.
- [ ] Avant la première vague : pas de panneau du score ; les panneaux de difficulté au-dessus du regard ; le panneau du score apparaît au gong.
- [ ] Menu en cours de partie (bouton menu de la manette gauche) : il s'ouvre devant soi, le jeu se fige, on ne peut ni prendre de flèche, ni tirer, ni lancer de grenade ; il suit le regard ; un nouvel appui le referme.
- [ ] Grenade : main vide à la hanche, poignée, une bombe à la mèche qui crépite ; elle reste en main en marchant ; lancée, elle éclate au premier contact (sol, mur, ennemi) en 28 flèches qui font mal ; recharge lisible sur la montre ; une flèche dans la grenade en vol : « MEGA-BOMBA » ; viser un socle de construction main basse ouvre bien le panneau (pas de grenade).
- [ ] Tutoriel, rubrique Tir : la fenêtre « La grenade à flèches » (« Vidéo à venir »).
- [ ] Raccourci **F** : chaque objet et 5 de chaque parchemin (2 de chaque amélioration avant le 25 octobre), tourelles niveau 2 sur tous les emplacements au bord de la voie, une seule fois par partie.
- [ ] Vague 6 : dans le noir de l'intro, on ne peut pas bouger (on peut regarder) ; on peut bouger dès que tous les cristaux sont allumés.
- [ ] Menu des armes : les Akimbow (arbalètes flottantes, une dans chaque main, la main gantée sur la poignée), l'arc cosmique, l'arc légendaire.
- [ ] Katana (refait le 24 octobre, **O** pour l'équiper) : plus de gantelet ; la main de l'arc porte le gant de la matrice (noir, des chiffres verts qui tombent, un liseré vert ; il bat quand une faille attend).
- [ ] Katana : une taille devant soi ouvre la faille au fil de la lame (un fil de lumière juste derrière la lame, aussitôt écarté), puis elle s'élargit encore ; plus large au centre, pointue aux bouts ; ses bords ondulent, ses fissures rampent ; elle se referme seule au bout de 3 s.
- [ ] Katana : des coups enchaînés vite donnent chacun leur faille (5 au plus) ; un petit coup ou un aller-retour laisse une fissure rouge qui grésille et se referme ; courir aux bras, avancer au joystick ou trancher hors du champ de vision n'ouvre rien.
- [ ] Katana : un long coup donne une faille verte (parfaite), un coup moyen une orange ; deux failles qui se croisent fusionnent de façon visible (la faille passe au cyan et bondit, une onde de lumière part du croisement, un nœud lumineux y reste, des 0 et des 1 en jaillissent, « Fusion ! ») ; 5 failles au plus en une ; elles partent ensemble et explosent sur l'ennemi.
- [ ] Katana en main : le tranchant est vers l'avant (le modèle a été retourné le 24 octobre).
- [ ] Katana rangé : debout à côté du joueur, comme l'arc, du côté de la main libre (plus à la hanche) ; lâché, il y retourne ; la lame ne gêne pas la vue.
- [ ] Katana : la faille s'ouvre au bout de la lame et reste où elle s'est ouverte ; jamais plus de 5 failles en tout : avec une fusion de 5, un nouveau coup referme la plus ancienne (elle quitte la fusion).
- [ ] Katana : un coup de poing (en tendant le bras) lance une onde de choc visible (deux anneaux verts qui filent en s'élargissant) ; elle propulse les failles qu'elle atteint ; balancer les bras pour courir n'en lance pas.
- [ ] Katana : un coup de poing du gant dans une faille la lance vers l'ennemi regardé ; un coup de poing dans un ennemi le frappe et le repousse ; la lame laisse une traînée quand elle va vite ; un lancer parfait fait un très bref ralenti ; un ennemi tué par une faille se défait en 0 et en 1.
- [ ] Katana avec les améliorations (raccourci **F**) : multitir (plusieurs failles partent), écho (elle repart un instant après), déluge (elle se brise en petites failles), foudre, explosion et glace à l'impact, ricochet ; avec le vampirisme, une tête touchée rend des PV.
- [ ] Katana : frapper le gong avec la lame, ou y envoyer une faille, lance la vague ; idem pour les cibles d'entraînement et les cibles « Continuer » / « Terminer » après MK. La lame ne clique pas les boutons des menus.
- [ ] Toit cyberpunk (refait le 24 octobre, **G** deux fois) : bien plus de néons et de couleurs, mais l'ensemble reste sombre ; le ciel est étoilé (étoiles cyan et roses, aurore néon, lune à bandes, quelques bandes qui buguent sans éblouir) ; aucun tireur n'est caché par le muret ou les objets du toit (on voit chaque tireur qui tire, et ses balles) ; le mannequin holographique géant se tourne vers toi ; le toit et les flaques reflètent les néons ; les enseignes se lisent ; pas d'image lente.
- [ ] Toit cyberpunk, l'épreuve : moins de tirs, un seul tireur vise à la fois ; chaque attaque s'entend d'où elle vient (un bip, trois bips, une note qui descend : s'accroupir, deux notes qui alternent : se pencher), puis sa charge monte jusqu'au tir ; les balles grésillent en approchant ; une rafale ne coûte qu'une touche ; tics des 10 dernières secondes, cœur qui bat à la dernière touche ; tenir la minute est faisable.
- [ ] « Quitter » cliqué à la main : le bouton affiche « Vraiment ? » et un message ; un deuxième clic dans les 3 s ferme le jeu, sinon il redevient normal. Pareil à la flèche (deux flèches).

**Lore, acte 1 (25 octobre) : à vérifier**
- [ ] Pour revoir l'arrivée des traces depuis le début : effacer `memory.json`, `leaderboard.json` et `choses_a_faire_dehors.txt` dans `%USERPROFILE%\AppData\LocalLow\DefaultCompany\THUNK VR` (garder une copie du classement si besoin). Sinon, le jeu compte les parties déjà enregistrées au classement : tout apparaît d'un coup.
- [ ] 1re partie enregistrée (**K** pour mourir) : **THINK** en tête du classement (999 999, Impossible, vague 999) ; « Nouveau record ! » si aucune vraie partie n'est au-dessus de la tienne.
- [ ] 2e partie : `choses_a_faire_dehors.txt` apparaît dans le dossier de la sauvegarde (titre, 4 lignes) ; une ligne de plus à chaque partie ; effacé, il revient à la partie suivante.
- [ ] 3e partie : le reflet (ton nom à l'envers) juste sous ta partie, de 1 à 9 points en dessous.
- [ ] 4e partie et suivantes : une copie de plus à chaque partie, sous le reflet, le nom de plus en plus abîmé, le score de plus en plus bas ; les anciennes copies gardent leur nom.
- [ ] Au casque, dès la 2e partie : retirer le casque au moins 8 s pendant une partie, le remettre : « Je t'attendais. » sur la montre, à la place de la ligne de la vague, 6 s ; une seule fois par partie ; rien pour une absence courte. Raccourci **Y** pour le voir sans attendre (sans casque : un message devant les yeux).

**Ménage des fichiers inutilisés (25 octobre) : à vérifier dans Unity**
- [ ] À l'ouverture (l'import peut être long) : la console sans erreur de référence manquante (*Missing*).
- [ ] Jouer chaque scène du build : `Prototype_Tir` (arcs, katana, coffres, boss des vagues), `Arena_Dragon`,
  `Dimension_Etoiles` (les torii), `Dimension_Cyberpunk` (les cascades, les Akimbow).
- [ ] Le joueur en VR (mains, manettes, téléportation, menus) : les démos des exemples XR et presque tout le modèle VR de
  Unity sont partis.
- [ ] Les monstres, le mage, le Beholder et le coffre ont leur bon matériau (les versions HP et Polyart des packs sont
  parties).
- [ ] Si la fenêtre *Tutorials* de Unity s'ouvre ou signale une erreur : son tutoriel (celui du modèle VR) est supprimé ;
  on peut retirer le paquet *Tutorial Framework* dans *Window > Package Manager*.
- [ ] Un fichier qui manque revient par Git : `git checkout HEAD -- "<chemin>"` (et son `.meta`).

**Niveau de qualité « Quest » (25 octobre) : à vérifier dans Unity**
- [ ] À l'ouverture : la console affiche « QuestQualityBaker : niveau de qualité « Quest »… » ; *Project Settings > Quality* a une ligne « Quest », cochée par défaut pour Android ; « Ultra » reste celui du PC.
- [ ] *Project Settings > XR Plug-in Management > OpenXR*, onglet Android : « Foveated Rendering » cochée, et la validation du projet sans erreur.
- [ ] L'image du Quest par Quest Link : choisir « Quest » dans *Quality*, jouer (néons, bloom, ombres, eau) ; puis revenir sur « Ultra ».
- [x] Un build Android de développement au casque seul (25 octobre au soir) : une partie jusqu'à la vague 10 ; ça rame un
  peu à la fin (les Akimbow, beaucoup d'améliorations, des particules partout).
- [ ] La prochaine partie au casque seul, pour mesurer : build de développement (*Development Build*) ; si le casque a
  redémarré, le rebrancher et refaire `adb logcat -G 16M` ; jouer sans câble jusqu'à ce que ça rame, puis rebrancher et
  faire lire le journal et les mesures (`Audit_Performance_Quest.md`, partie 8).

**Nouveau la semaine du 13 au 17 octobre, jamais testé en casque**
- [ ] Menu d'accueil : plus de boutons « Jouer » ni « Difficulté » ; le titre THUNK VR, le bouton **Tutoriel** et une ligne d'aide.
- [ ] Les menus se visent à l'arc : une flèche dans un bouton le presse, dans un curseur le règle ; « Quitter » demande deux flèches en 3 s (ou deux clics, depuis le 23 octobre). Les paramètres se règlent aussi à l'arc.
- [ ] Les panneaux de difficulté se choisissent au rayon de la manette (ils grossissent au survol).
- [ ] Tutoriel : 4 rubriques (Tir, Déplacement, Vagues, Construction) avec leur icône ; dans chaque fenêtre, le titre, le cadre de la vidéo, le texte, « < » et « > », le compteur et « Retour ».
- [x] Les 13 vidéos du tutoriel enregistrées et déposées dans `Assets/_Project/Resources/Tutoriel` (noms dans `LISEZMOI.txt`). Faites le 19 octobre, dans le casque, avec l'outil `Archery > Tutoriel > Tournage des vidéos`. Vérifier en jeu que chacune passe sur le grand écran du tutoriel.
- [ ] Facile très simple : vagues deux fois plus courtes, deux fois moins d'ennemis après l'accalmie, boss (et MK) deux fois moins résistants, balistes deux fois plus lentes.
- [ ] Construction : un marteau dans chaque main, sans rien saisir ; la main se ferme dessus.
- [ ] Boss : barre de PV plus haute, avec le nom dedans ; point faible en gemme sertie d'or, de la couleur du boss, qui bat comme un cœur ; la gemme de Nolan et celle de Francine se voient, et on ne les traverse plus.
- [ ] Mage (Tireur) : il avance normalement ; plus de Tireurs, et des Liches dès la vague 3.
- [ ] Boutique : cartes au texte court, avec une icône ; le montant d'or ne touche plus le cadre.
- [ ] Coffres : les 3 orbes montrent l'icône de leur récompense.
- [ ] Rayons de lumière de la carte : plus de bavure là où ils touchent le sol.
- [ ] Porte de l'antre : elle remplit l'arche et touche le sol ; « THUNK VR » creusé dans la roche, sans fissures jaunes.
- [ ] MK (le dragon) : 6 750 PV de base ; après son triple salto, il ne t'enfonce plus dans le sol ; pendant la triple ruée, la bande rouge envoie de la lumière vers le haut, visible la tête levée.
- [ ] Balistes : un tiers de dégâts en moins.
- [ ] Rotation au joystick : plus de demi-tour en tirant le joystick vers soi.
- [ ] Au lancement avec le casque, pas de message « pas de casque, jeu au clavier ».

- [ ] Portail des étoiles (`Guide_Portail.md`) : les 5 runes à toucher pendant une vague, le portail qui s'ouvre, la carte figée, la dimension (panneaux d'erreur, monstre corrompu qui change d'apparence), le retour (étoile au classement en cas de réussite). Dans l'éditeur, P ouvre le portail puis fait entrer.
**Tir**
- [ ] Prendre l'arc dans chaque main ; prendre une flèche dans le dos, l'encocher, tendre, tirer.
- [ ] Anneau vide au centre : rouge, orange, vert, vert pastel ; « ding » et vibration en entrant dans le vert ; tir parfait plus rapide et plus fort ; le cercle s'arrête dans le vert pastel sans recommencer.
- [ ] Aide à la visée de la couleur de l'anneau : trajectoire complète en Facile et Normal, ligne droite en Difficile, rien en Impossible.
- [ ] Coup au corps à corps : la flèche reste plantée dans l'ennemi.

**Partie**
- [ ] Menu : difficulté sur les panneaux (le ciel change), Tutoriel, Paramètres (volumes, rotation, confort), Classement, Quitter.
- [ ] Gong, chrono, montre (score, chrono, vague, PV, tour, or, combo, difficulté, bonus).
- [ ] Voile rouge autour de la vision quand les PV baissent, qui bat sous 25 % des PV.
- [ ] Ennemis : vague 1 Rampants, Gluants, Champignons ; vague 2 Tireurs, Gluants bleus, Étoiles de mer ; vague 3 Volants, Guêpes, Champignons furieux, Cactus, Fantassins, Liches ; vague 4 Orcs, Tortues, Gargouilles ; vague 5 Mimiques. Chacun s'anime (course, attaque, touché, mort) et ses zones de touche tombent juste (tête ×2).
- [ ] Un boss différent à chaque vague (Thomas le champignon, Jean la mimique, Gontrand la tortue, Francine le chef orc, Nolan la liche), avec son nom, sa barre de PV, sa gemme et ses renforts.
- [ ] Les boss courent à grandes enjambées (animation lente) et leur point faible se vise sans peine.
- [ ] Vague 6 : MK, le dragon rouge, dans son antre (GDD, section 23).
- [ ] Tour détruite : le gong descend au pied de la tour, et remonte à la reconstruction.
- [ ] Barricades : les ennemis s'y arrêtent, les cassent, la réparation en boutique les relève.
- [ ] Fin de vague : les survivants fuient, la boutique s'ouvre, les PV du joueur reviennent.
- [ ] Boutique : les parchemins et les objets (plus de relance, de réparations ni d'arcs depuis le 25 octobre : voir « Refonte roguelike, étape 1 », en tête de cette partie) ; les prix montent après chaque achat.
- [ ] Choix de l'arc avant la vague 1 : cartes avec jauges et pouvoir, arcs verrouillés par leur succès (U / J pour tester), l'arc choisi dans la main et dans la dimension du portail.
- [ ] Coffre : chute et rebond à l'apparition, rayon de lumière, couvercle, ouverture animée, ralenti, 3 orbes avec leur nom, récompense ; il disparaît en fin de vague.
- [ ] Brasero : flèche enflammée, l'ennemi brûle.
- [ ] Tower defense : gisements de cristaux pendant la vague, récolte, matériaux sur la montre ; socle visé à la poignée, panneau, plan bleu, un marteau dans chaque main, 3 coups, l'arc revient ; amélioration d'un bâtiment.
- [ ] Bâtiments : la baliste pivote et tire (volants compris), la tour de givre ralentit, les piques jaillissent, les ennemis s'arrêtent devant une barricade construite ; le message « Défenses : … % des dégâts » en fin de vague.
- [ ] Déplacements : marche, course aux bras, slide, descente de la tour à pied, téléporteur (bande autour de la tour, bouton « Retour à la tour » de la boutique), pas de flèche au joystick droit.
- [ ] Vagues : accalmie (peu d'ennemis), montée, boss à la fin du chrono ; deux coups de gong font venir le boss plus tôt (bonus affiché) ; or de fin de vague avec ses bonus.
- [ ] Bouton « Tour » sous la montre : on le touche avec l'autre main, téléportation, 25 s de recharge pendant les vagues.
- [ ] Victoire après MK, à la vague 6 : « Terminer la partie » dans la boutique (écran « Victoire ! »), ou le gong pour le mode infini (plusieurs boss).
- [ ] Tour détruite : les ruines remplacent la tour (fumée, son), le joueur est évacué ; reconstruite en boutique, elle revient.
- [ ] Les messages (début de vague, boss, coffre, cristaux) restent devant le regard, lisibles.
- [ ] Les deux ponts de la voie du milieu se traversent sans sauter.
- [ ] Un coffre ouvert disparaît 15 s après.
- [ ] Mort : « Tu es mort » en grand, écran de fin qui suit le joueur, nom au clavier virtuel, classement avec la nouvelle ligne, *Rejouer*.
- [ ] Les paramètres et le classement sont gardés après avoir relancé le jeu.

**Confort et fluidité**
- [ ] Au moins 72 images par seconde dans les moments chargés (fin de vague Impossible, multitir, déluge, phases 2 et 3 de MK). Vue *Game* > *Stats*.
- [ ] Aucune erreur rouge dans la console pendant toute une partie.
- [ ] La vignette de confort pendant le slide ; aucun malaise sur une partie de 15 minutes.
- [ ] Pendant le jeu dans l'éditeur, la fenêtre *Scene* fermée (elle alourdit la carte graphique).

## 2. Le build

- [ ] *File > Build Profiles* > *Windows* : `Prototype_Tir` et `Arena_Dragon` cochées dans la liste (c'est le cas au 17 octobre ; `SampleScene`, la scène d'exemple de Unity, a été supprimée et retirée de la liste le
  25 octobre).
- [x] *Player* : *Product Name* `THUNK VR` (17 octobre). Les sauvegardes sont dans `%USERPROFILE%\AppData\LocalLow\DefaultCompany\THUNK VR` ; le classement et les paramètres y ont été recopiés depuis l'ancien dossier `Project Archery`.
- [ ] *Player* : *Company Name* encore `DefaultCompany`. Le changer (le nom de l'équipe, par exemple) déplace aussi le dossier des sauvegardes.
- [ ] Le build se lance, le casque l'affiche via Quest Link, et une partie entière se joue sans l'éditeur.
- [ ] Les raccourcis clavier de test sont absents du build final (ils n'existent que dans l'éditeur et les builds *Development*). Pour la démo, faire aussi un build *Development* : les raccourcis permettent de tout montrer vite (`Demo_Vendredi.md`, partie 4).

## 3. Le jour de la démo

**Matériel**
- [ ] Pilote NVIDIA **591.86 ou plus ancien** sur le PC de la démo : à partir de la 595.71, Meta Horizon Link fige l'image du casque en pleine partie. Aucun bandeau rouge dans l'application Meta Horizon Link, et refuser les mises à jour de pilote.
- [ ] Casque chargé, manettes avec des piles neuves (et des piles de rechange).
- [ ] Câble Link testé sur le PC de la démo, ou Air Link sur un réseau 5 GHz fiable.
- [ ] Zone de jeu (*Guardian*) tracée, assez grande pour se tourner et balancer les bras.
- [ ] Le build sur le PC (et une copie sur une clé USB).

**Si l'image du casque se fige** (le jeu continue sur le PC) : PowerShell en administrateur, `Restart-Service OVRService`, puis relancer Link dans le casque. Si le casque ne se reconnecte plus en USB, redémarrer le PC.

**Avant de commencer**
- [ ] Vider le classement de test : supprimer `leaderboard.json` dans `%USERPROFILE%\AppData\LocalLow\DefaultCompany\THUNK VR`, ou garder quelques bons scores pour l'exemple.
- [ ] Volumes réglés pour la salle.
- [ ] Décider de la difficulté : Facile pour quelqu'un qui découvre la réalité virtuelle, Normal pour une démo fluide, Impossible pour impressionner.

**Déroulé conseillé (5 à 10 minutes)** : en résumé ci-dessous ; le déroulé minute par minute, le pitch, les questions du jury et les plans B sont dans `Demo_Vendredi.md`.
1. Le menu : choisir la difficulté sur les panneaux et montrer le ciel qui change ; ouvrir le Tutoriel.
2. Les cibles d'entraînement : le tir, l'anneau, un tir parfait.
3. Le gong : vague 1, une flèche enflammée au brasero, puis la boutique (une amélioration).
4. Une vague avec un coffre : descendre, courir, glisser, ouvrir le coffre.
5. Montrer un boss, puis MK, le boss final (dans l'éditeur, **N** appelle le boss ou le tue, jusqu'à la vague 6).
6. Mourir, saisir son nom, montrer le classement.

**Solution de secours** : si le build pose problème, jouer depuis l'éditeur, où les raccourcis (**N**, **M**, **C**) permettent de montrer chaque partie rapidement.

## 4. Le rendu

- [ ] README à jour : crédits complets (packs de la carte, sons et musiques définitifs), équipe. Inventaire et licences : `Credits_Licences.md` (18 octobre), dont les 3 points « À régler avant le rendu » (dépôt public et packs de l'Asset Store, crédit de la musique de MK, auteurs à confirmer).
- [ ] GDD à jour (planning coché, choix finaux).
- [ ] Dépôt GitHub propre : `main` jouable, dernier commit poussé.
- [ ] Rapport : ce qui a été fait, les choix, l'usage de l'IA.
