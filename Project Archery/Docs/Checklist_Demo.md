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

**Refonte du tower defense, étape 3 : construire n'importe où (27 octobre ; `Docs/Refonte_TowerDefense.md`)**
- [ ] Au lancement : la console écrit « BuildGrid : N emplacements de construction (… sur la voie, … au bord, … en hauteur) » ; aucune erreur rouge, pas d'à-coup.
- [ ] Les 12 anciens socles et les 3 barricades de départ ont disparu (la scène, elle, n'a pas changé).
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
