# Tests complets et préparation de la démo

À faire dans les derniers jours avant le rendu (vendredi 23 octobre). Coche au fur et à mesure.

## 1. Tests complets (dans le casque)

Une partie en **Facile**, une en **Normal**, puis une en **Impossible**, la console ouverte : aucune ligne rouge, et le moins possible de lignes « Image lente ».

**Le journal de partie** (3 novembre) : chaque partie lancée dans Unity s'écrit toute seule dans `Project Archery/Logs/Parties/Partie_<date>.md` (le déroulé daté et un bilan par vague : objets, plans, bâtiments, dégâts par source, tirs, dégâts reçus, or, fluidité, erreurs). Après la partie, dire à Claude de le lire. À vérifier la première fois : le fichier apparaît dès la première vague, et il se termine par « Bilan de la partie » (à la mort, à la victoire ou à l'arrêt du mode Jeu).

**Le neuvième essai : ce qui a changé le 12 novembre (`Docs/Refonte_TowerDefense.md`, 12.74)** : à l'ouverture de Unity, la console dit « Le neuvième essai (12 novembre) : … réglage(s) changé(s) » (les 4 difficultés, la Force, les chances des raretés, l'Horloge, le tutoriel, les plans des tourelles) puis « La scène a changé : enregistre-la (Ctrl+S) » : **enregistrer la scène**. Une partie en **Normal**, au casque, puis dire à Claude de lire le journal.
- [ ] **Après la courte partie du 12 au soir (`Docs/Refonte_TowerDefense.md`, 12.75)** : la console dit aussi « Les petits ennemis et les boss (12 novembre, le soir) : 10 réglage(s) changé(s) ». **Les petits ennemis** (Gluant, Gluant bleu, Champignon, Champignon furieux, Fantassin) : les flèches ne passent plus au travers, ni des champignons en plein saut. **Les gardiens** : jamais sur la voie, jamais introuvables, chaque coffre finit par s'ouvrir (le journal : « gardiens : N, N tués »), plus de coups venus de nulle part. **Les boss** vont plus vite que les autres (de 2,4 à 3,4 m/s avant la difficulté) : menaçants, sans prendre la tour trop vite ? **Les projectiles** (tireur, liche, Nolan) : la flèche qui en abat un le traverse et continue (elle peut toucher l'ennemi derrière) ; une boule de feu de MK l'arrête toujours.
- [ ] **Le dixième essai (`Docs/Refonte_TowerDefense.md`, 12.76)** : la console dit « Le saut aux bras (12 novembre) » puis « La scène a changé » (Ctrl+S), et « La corde fantôme refaite ». **La porte** ouverte : plus de taches qui scintillent sur la tranche des battants. **Le rayon scellé** : rouge vif, large, il respire ; visible de loin, même sur le ciel. **Le saut** : moins sensible ; la ligne « Sensibilité du saut » des paramètres le règle (« Coupé » au plus bas). **La Tour-aimant niveau 4** : une flèche bien visée sur la gemme d'un boss, sur un ennemi précis ou (avec le talisman de l'ingénieur) sur une de tes tours file droit ; une flèche qui allait manquer est courbée, vers la gemme d'un boss. **La grenade** : ses flèches gèlent, explosent, foudroient, percent comme les tiennes ; la fluidité tient-elle ? **La corde fantôme** : après un tir, une flèche bleue transparente sur la corde ; la prendre de la main libre, tendre, lâcher : elle part comme le tir d'avant (+20 %) ; 1, 2 ou sans fin selon le niveau ; une vraie flèche l'efface ; lâchée sans tir, elle revient. Trop forte au niveau 3 ?
- [ ] **La batte, le verrou et la nouvelle apparence** (14 novembre) : la console dit « la batte steampunk refaite ». Gâchette à moitié de la main libre : un anneau pâle sur l'ennemi visé ? À fond : la roue dorée, et les grenades frappées filent sur lui ? Dans le vide : le verrou se lève ? La vapeur coule-t-elle dans les tubes de verre, l'aiguille suit-elle la recharge, l'engrenage, le piston, le foyer bougent-ils ? La grenade fantôme, maintenant 35 cm sur le côté et 80 cm devant, se voit-elle bien, et n'apparaît-elle qu'après le coup ?
- [ ] **La batte, après les parties du 14 novembre** : la grenade lancée en l'air va-t-elle en cloche vers le côté de la batte (plus de saut d'un coup sur le côté) ? La visée prend-elle bien l'ennemi voulu (plus de cible trop à droite), jamais un ennemi caché derrière le décor ? Avec la corde triple (et le multitir), les copies partent-elles sans éclater sur la batte ? Le voile rouge bat-il dès la moitié des PV ?
- [ ] **La batte de baseball, après le premier essai** (13 novembre au soir) : la console dit « le coup en mêlée passe de 30 à 22 dégâts ». Une grenade lancée à peu près vers le haut monte droit et retombe **du côté de ta batte** (70 cm sur le côté, 50 cm devant), même en courant (la course ne fausse plus le lancer) ? La grenade fantôme flotte du même côté ? Avec la batte, la grenade tombe-t-elle assez vite (gravité 1,2 au lieu de 0,7) ? Une grenade lâchée près d'un menu (la pause, le menu des armes, les panneaux de difficulté) ne clique plus rien ? Se frappe-t-elle maintenant facilement (35 cm autour de la batte) ? Regarder les ennemis, lancer, suivre la grenade des yeux, frapper : part-elle bien vers les ennemis regardés avant le lancer ?
- [ ] **La batte de baseball** (13 novembre, GDD section 5) : la console dit « La batte de baseball (13 novembre) » ; **F6** l'équipe. Le style steampunk (tuyaux de cuivre, laiton, manomètre, foyer) se lit-il en main et dans le menu ? La vapeur sort-elle des cheminées à chaque coup, plus fort pour un parfait ? Se prend-elle bien à côté de toi ? Une grenade lancée en l'air se frappe-t-elle facilement (pas de coup dans le vide alors qu'on la touche) ? Le parfait arrive-t-il pour une grenade lancée à 2 m au-dessus de la main (sinon, régler les seuils) ? Elle file bien sur l'ennemi regardé, flammes et vitesse selon la qualité ? Recharge de 3 s, plus courte avec l'Adresse. Les améliorations (**R** ou **Tab**) : le multitir et la corde triple envoient des copies sur d'autres ennemis, l'écho fait éclater la grenade deux fois, la corde de siège donne plus de flèches à une grenade lancée haut, la corde fantôme fait flotter une grenade devant toi (se frappe-t-elle bien, à la bonne place ?) ; le journal : la part des dégâts de la grenade et la fluidité (beaucoup de flèches toutes les 3 s).
- [ ] **Les raretés qui montent** (revues après le dixième essai, 12.76) : objets et parchemins de 65 / 22 / 9 / 4 à la vague 1 à 45 / 30 / 17 / 8 à la vague 5 ; tourelles de 50 / 30 / 14 / 6 (1,8 plan par orbe) à 30 / 35 / 22 / 13 (2,2). La console dit « Les drops du début de partie » (deux lignes), puis Ctrl+S. La partie est-elle moins fade : un duo ou un trio monté, une pièce marquante avant la vague 3 ?
- [ ] **Les coffres scellés** : chaque coffre de la vague a son rayon rouge et l'étiquette « Scellé · N gardiens » ; le viser et appuyer ne l'ouvre pas (petite vibration, l'étiquette rappelle « Tue ses gardiens pour l'ouvrir ») ; chaque gardien tué (flèche ou bâtiment) fait baisser le compte ; au dernier, « Sceau brisé ! », le rayon reprend sa couleur, le coffre s'ouvre. L'annonce des coffres dit « scellés : tue leurs gardiens pour les ouvrir ».
- [ ] **Le chantier du début** : 3 bâtiments, ceux d'un trio, sans la Barricade en plus.
- [ ] **Les tours −25 %** et **la Force à +10 %** (la carte : « +10 % ➶, anneau 10 % plus rapide » en commune) : les bâtiments font-ils encore l'essentiel des dégâts (le journal) ?
- [ ] **Le gong** : après un boss appelé tôt, une carte par ligne dit « Prendre · gong » ; le journal écrit « le boss est appelé au gong ».
- [ ] **La Grande Horloge** : au douzième coup de Minuit, tes flèches arrêtées repartent et touchent les figés, puis le dégel les brise (plus faible : 117) ; dans l'arène, la musique de MK continue pendant Minuit.
- [ ] **Les ultimes dans l'arène** : rien avant le début du combat ; leur premier coup sur MK (plaqué, chargé, Minuit) une quinzaine de secondes après.
- [ ] **La porte de l'antre** : le message dit d'entrer dans la porte ; des étincelles et des flammes jaillissent pendant l'ouverture, un souffle de feu, puis des braises ; les battants ouverts ne scintillent plus ; plus de barres qui scintillent de chaque côté de la grotte ; collé au mur du fond quand la porte se referme, plus d'éclair blanc.

- [x] **La vague 1 plus douce** (12.73) : 30 % d'ennemis en moins (le journal : « Ennemis : N apparus »). Vérifié le 12 novembre : 18 (24 ou 25 le 8 novembre).

**Le huitième essai (`Docs/Refonte_TowerDefense.md`, 12.72)** : la console dit « Chances des raretés : … → 60 / 24 / 11 / 5 ».
- [ ] **Le gong** : après un boss appelé tôt, une seule carte par ligne est plus rare (pas toute la pause).
- [ ] **Moins de raretés hautes** : la plupart des cartes sont communes ; une légendaire reste un événement.
- [ ] **Le Déluge avec la Pluie de flèches** : chaque flèche du Déluge sème aussi une pluie derrière elle ; la fluidité tient-elle ?

**Le septième essai : ce qui a changé le 8 novembre au soir (`Docs/Refonte_TowerDefense.md`, 12.70)** : à l'ouverture de Unity, la console dit « Les quatre raretés (8 novembre au soir) ». Une partie en **Normal**, au casque, puis dire à Claude de lire le journal.
- [ ] **Les boutons « +N niveaux »** : plus grands, sur deux lignes, rien ne déborde.
- [ ] **Viser l'ultime** (et les grands bâtiments) par son modèle, pas seulement son socle.
- [ ] **« Le style, ça compte aussi »** : rien sur les escaliers ni la tour en marchant ; en l'air pour de bon, le ralenti, mais pas de tir parfait gratuit ; l'anneau tourne à sa vitesse normale pendant le ralenti (et pendant les autres ralentis).
- [ ] **Le Rappel avec la Pluie de flèches** : les flèches rappelées foncent sur les ennemis proches avant de revenir ; ça doit faire plus mal qu'un tir direct.
- [ ] **MK** : il passe par ses trois phases (au sol, en vol, enragé ; le journal) ; les ultimes ne le clouent, ne l'étourdissent ou n'arrêtent le temps que toutes les 15 s environ, sans coup à chaque flèche.
- [ ] **Plus de stun-lock** : un ennemi étourdi a 1 s de répit avant d'être de nouveau étourdi (la baliste).
- [ ] **Plus de chiffre doré** au-dessus des ennemis tués.
- [ ] **Le gong** : « la pause suivante sera plus rare » ; après un boss appelé tôt, la pause est plus rare d'un cran.
- [ ] **Les quatre raretés** : les cartes et les orbes en blanc, bleu, violet, or ; les parchemins ont une rareté (leur texte suit) ; une tourelle de 3 plans est épique ; l'orbe d'un lot de plans montre la tourelle en miniature.
- [x] **La fiche** `Docs/Fiche_Ameliorations.xlsx` : remplie par le joueur, appliquée (12.71) ; la console dit « La fiche des améliorations (8 novembre au soir) : 12 changement(s) ».
- [ ] **Les pièces épiques** (violet) : Tir détonant, Chaîne d'éclairs, Boomerang, Rappel, Déluge, Sentinelle, Arc conducteur, Multitir, Tir triple, Tir écho, Talisman de l'ingénieur.
- [ ] **La Pointe du chasseur** : la marque reste sur le premier ennemi touché jusqu'à sa mort (tirer sur un autre ne la déplace pas), ne saute plus à sa mort, et le tir qui marque fait déjà les dégâts en plus.

**Le sixième essai : ce qui a changé le 8 novembre (`Docs/Refonte_TowerDefense.md`, 12.68)** : à l'ouverture de Unity, la console dit « Le phare du jugement (8 novembre) : bonus sur les points faibles… ; texte du niveau 5 ». Puis une partie en **Normal**, au casque, et dire à Claude de lire le journal.
- [ ] **Le Phare du jugement** : au niveau 5, toutes les 10 s, le rayon tombe sur l'ennemi le plus fort, « Jugement ! » : il reste étourdi 3 s (un boss 1,5 s), sans perdre de PV ; tes flèches lui font plus de dégâts. Le journal : le Phare ne fait plus une grosse part des dégâts, et **aucun sur MK**.
- [x] **MK** (vérifié le 12 novembre : 20 291 PV, l'envol à 66 %, la rage à 33 %, mort en 1:36) : environ 20 300 PV en Normal (journal : « MK apparaît (PV …) ») ; il s'envole vers 66 % de ses PV et devient enragé vers 33 % (journal : les phases « Air » et « Enraged » et leurs PV), même au milieu d'une attaque ou sur un perchoir ; le combat dure entre 1 min 30 et 2 min.
- [ ] **L'Adresse** : avec quelques parchemins d'adresse, les tirs parfaits font nettement plus mal (+20 % par parchemin, voir plus bas).
- [x] **Plusieurs niveaux d'un coup** (vérifié le 12 novembre : Mortier 1 → 4, Baliste 3 → 5, Lance-filet 1 → 4) : sur un bâtiment, sous les deux cartes, les boutons « +2 niveaux · 2 plans », « +3 niveaux · 3 plans »… (seulement avec assez de plans) ; un clic, trois coups de marteau, et le bâtiment saute au niveau visé. Un duo paie avec les plans de ses tours.
- [ ] **Viser le bâtiment entier** : le rayon de la main ouvre le panneau en visant la tour elle-même (son modèle), plus seulement son socle ; les flèches, les ennemis et les boulets la traversent comme avant ; la pose d'un plan à côté marche toujours.
- [ ] **Le panneau tout près d'une tour** : il s'ouvre devant la tour ou sur le côté, jamais dedans ; tout se clique.
- [ ] **Le rappel au gong** : avec des plans en poche, l'annonce de la vague dit « N plans en réserve : pose-les ! ».
- [ ] **Le choix de la pause** (12.69 ; la console, à l'ouverture de Unity : « Les parchemins du choix de la pause ») : après chaque vague, trois lignes (Parchemins, Objets, Tourelles), « Prendre » sur chaque carte, une seule par ligne (la prise dit « Pris », les deux autres s'éteignent) ; la ligne des tourelles est bien sous les autres, avec son nom ; le panneau reste lisible (plus haut d'une ligne) ; la place de l'or dit « Une carte par ligne ».
- [ ] **Plus d'or** : ni « +N or » en fin de vague, ni or sur la montre (« Plans N ») ; plus de coffre de boss ; jamais le talisman de l'avare ni le butin.
- [ ] **Plus de combo** : plus rien sur la montre.
- [x] **Le journal** (vérifié le 12 novembre ; Claude le lit après la partie) : les cartes de chaque pause, proposées et prises ; « Flèches pour tuer » par sorte d'ennemi (les orcs, la liche) ; plus d'or ; la console, à l'ouverture de Unity, dit aussi « Talisman de l'avare : retiré ».
- [ ] **Les parchemins à deux effets** : Force (dégâts et anneau 15 % plus rapide), Adresse (bande verte plus large, tirs parfaits plus forts), Vigueur (+35 PV, les flèches vont plus loin) ; les cartes le disent ; un ou deux par pause et par coffre suffisent-ils, ou c'est trop fort ?

**Le cinquième essai : ce qui a changé le 4 novembre (8 novembre ; `Docs/Refonte_TowerDefense.md`, 12.60 à 12.67)** : une partie
entière en **Normal**, au casque, puis dire à Claude de lire le journal.
- [x] **Les gardiens** (12.61) : chaque coffre apparaît avec ses gardiens (2 aux vagues 1 et 2, 3 aux vagues 3 et 4, 4 ensuite ; le journal : « Un coffre apparaît, gardé par N ennemis ») ; ils vadrouillent au pas autour du coffre avec des pauses, attaquent à 15 m, lâchent la poursuite au-delà de 30 m du coffre, ne vont jamais à la tour ; on peut ouvrir le coffre sans les tuer.
- [x] **La vague de durée fixe** (12.61) : toutes les vagues durent autant que la première (environ 1 min avant le boss, en Normal).
- [x] **La tour à 1 500 PV** (12.62) : une expédition aux coffres laisse une trace sur la tour ; le cor et le rayon rouge quand elle est attaquée.
- [ ] **Les plans par coffre** (12.63) : l'orbe de tourelle dit combien de plans elle donne (« 3 plans »), de 1 à 4, plus souvent beaucoup en fin de partie ; le journal : « Plans : +N … ».
- [ ] **Le Carquois géant** (12.64) : il ne rattrape plus que 2 flèches par seconde (avec un multishot, la nuée met au moins 15 s à se remplir) ; sur MK, une nuée ne le tue plus d'un coup.
- [ ] **L'Avalanche** (12.65) : un ennemi déjà avalé une fois est seulement renversé par la boule ; les voies avancent.
- [ ] **Les ennemis au sol 30 % plus rapides** (12.66) : ceux des voies, les gardiens et les boss des vagues ; pas les volants ni MK ; leurs jambes suivent.
- [ ] **Le joueur à 100 PV, la Vigueur à +12** (12.67) : la carte de la Vigueur dit +12 ; sous 30 % des PV, le voile rouge est bien visible et bat ; avec les Akimbow et une pièce de corde, plus de corde qui flotte à côté de soi.
- [ ] **Les ultimes −30 %** (12.62) : le Colosse, le Manège et l'Horloge restent impressionnants, mais ne font plus tout le travail.
- [x] **La partie monocible** (12.65, ta proposition) : une autre partie avec des pièces monocibles (Pointe du chasseur, Transe, Surcharge, Bourreau…), pour comparer au journal avec les builds de zone.

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

**Lot 7 : neuf duos de plus et les flèches « DUO » (31 octobre, nuit ; `Docs/Refonte_TowerDefense.md`, partie 12.17 ; B, H)**
- [ ] **La vitrine, F4** (31 octobre) : la première tour du menu apparaît d'un coup ; une tour de la voie sur les trois voies, vers 16 m de la tour (niveaux 3, 4, 5 de gauche à droite), une tour hors de la voie au centre des clairières de droite (niveau 3) et de gauche (niveau 4). Le message dit « Vitrine 1/… » et où rien n'a trouvé place. **F4** encore : elles disparaissent, la suivante apparaît, aux bons endroits ; après les 10 tours, les duos (niveaux 1 à 3) ; après le dernier, tout est retiré. **Maj + F4** revient en arrière. Une tour construite à la main reste, et celle de la vitrine se pose à côté.
- [ ] À l'ouverture de Unity, la console écrit pour chacun « Duos : <nom>, prefab créé… », « …, définition créée… », « …, inscrit au catalogue » (Marteau-pilon, Toboggan, Yo-yo, Avalanche, Feu d'artifice, Chant des sirènes, Loupe, Carquois géant, Chaîne de forçats) ; aucune erreur.
- [ ] **Les flèches « DUO »** : entre les vagues, deux tours d'une paire au niveau 3 : une grande flèche dorée en arc les relie, « DUO » et le nom du duo à son sommet, face à toi ; elle disparaît pendant la vague, et quand elles ne peuvent plus fusionner.
- [ ] **Marteau-pilon** (barricade + piques) : le marteau glisse au-dessus d'un ennemi et l'enfonce jusqu'au cou (« Planté ! ») ; seule sa tête dépasse, une flèche à la tête le touche ; il ressort comme un bouchon. Niveau 2 : l'onde renverse autour. Niveau 3 : trois marteaux en cadence, puis le grand coup qui envoie tout en l'air.
- [ ] **Toboggan** (tremplin + givre) : la piste descend vers la forêt ; l'ennemi glisse sur le ventre en tournoyant, décolle en vrille sous trois notes du jury, retombe sonné ; jamais deux fois le même. Niveau 2 : ceux qu'il percute glissent aussi. Niveau 3 : le saut à ski, très haut, « 10 ! 10 ! 10 ! », l'atterrissage tue.
- [ ] **Yo-yo** (tremplin + aimant) : le yo-yo attrape un ennemi, le hisse et le fait claquer au sol trois fois, puis le lâche. Niveau 2 : lancé sur un groupe. Niveau 3 : le lustre à sept fils, puis il s'emballe et jette tout le monde au loin.
- [ ] **Avalanche** (mortier + givre) : la boule roule vers la forêt sur la voie, grossit, avale les ennemis ; une flèche la crève (« Avalanche ! »). Niveau 3 : la boule géante sur toute la voie.
- [ ] **Feu d'artifice** (mortier + phare) : bouquets colorés, ennemis « Sonné ! », étincelles qui brûlent ; une flèche tirée à travers un bouquet s'enflamme. Niveau 2 : deux bouquets de plus. Niveau 3 : le bouquet final, vingt fusées.
- [ ] **Chant des sirènes** (phare + bulle) : un ennemi dans une bulle rose (« Charmé ! ») court frapper les autres. Niveau 2 : deux à la fois, la bulle éclate et en charme deux autres. Niveau 3 : la mêlée.
- [ ] **Loupe** (phare + baliste) : dans son champ, les ennemis ont une tête énorme, qui dégonfle en sortant (et à la mort). Niveau 2 : des carreaux dans les têtes. Niveau 3 : le rayon de soleil, l'ennemi fume puis prend feu.
- [ ] **Carquois géant** (baliste + lance-filet) : rater des flèches près de lui : elles apparaissent dans le filet ; à dix, une volée de carreaux pleut sur un groupe. Niveau 2 : les carreaux explosent. Niveau 3 : trente flèches ratées n'importe où, la nuée.
- [ ] **Chaîne de forçats** (aimant + lance-filet) : « Enchaînés ! », des chaînes entre les ennemis d'un groupe ; en blesser un blesse les autres. Niveau 2 : un lié qui meurt fait tomber les autres. Niveau 3 : l'écrasement au milieu (« Écrasés ! »).

**Les trios : le système et l'Enclume (1er novembre ; `Docs/Refonte_TowerDefense.md`, partie 12.20 ; F4, B, H)**
- [ ] À l'ouverture de Unity, la console écrit « Duos : Enclume, prefab créé… », « Duos : Enclume, trio du 1er novembre… », « …, définition créée… », puis « Trios : Enclume, inscrit au catalogue ». Aucune erreur rouge.
- [ ] **F4** jusqu'au bout : après le dernier duo vient « Trio : Enclume », sur les trois voies (niveaux 1, 2, 3). La forge sur pilotis enjambe la voie ; les ennemis passent dessous sans s'y arrêter.
- [ ] **La fusion** : un Marteau-pilon (n'importe quel niveau) et un mortier au niveau 3, entre les vagues : la flèche dorée « TRIO » les relie ; déplacer l'un sur l'autre montre l'hologramme doré « Trio : Enclume », la gâchette les fusionne (« Trio : Enclume ! »). Un mortier au niveau 2 : « Mortier au niveau 3 au moins ». Le trio naît au niveau du duo, un de plus si le mortier était au niveau 5.
- [ ] **Monter le trio** : 4 plans par niveau, de barricade, de piques ou de mortier ; le panneau dit « Trio · Enclume · niveau 1 » et les trois tours.
- [ ] **Niveau 1** : toutes les 4 s, une enclume part du mortier (le fût recule), vole en tournant et tombe sur l'ennemi le plus avancé : « Aplati ! », il s'écrase comme une crêpe, avance tout plat et lentement 6 s, puis reprend sa forme d'un coup. Une flèche sur un aplati : « Aplati ! » et plus de dégâts. Les enclumes posées s'enfoncent dans le sol.
- [ ] **Niveau 2** : l'onde du choc fait tomber ceux qui sont autour ; l'enclume rebondit vers un deuxième ennemi et l'aplatit.
- [ ] **Niveau 3** : avec trois ennemis à portée, « Pluie d'enclumes ! » : vingt enclumes tombent du ciel en 2,5 s, puis la grande enclume (10 m) tombe tout droit sur le groupe le plus serré (« BONG ! ») : les petits meurent, les autres sont aplatis ; elle reste plantée 6 s puis s'enfonce. Un boss dessous perd une part de ses PV, sans être aplati.
- [ ] À surveiller : un ennemi aplati qui s'enfonce dans le sol ou flotte (son NavMesh Agent écrasé avec lui) ; un ennemi tué aplati.
- [ ] **Gonfleur** (lanceur de baudruches + baliste, au bord ; partie 12.21) : la console écrit « Duos : Gonfleur, prefab créé… » puis « Trios : Gonfleur, inscrit au catalogue ». Fusion : un Lanceur de baudruches et une baliste au niveau 3 (flèche « TRIO »).
- [ ] **Gonfleur, niveau 1** : la baliste se tourne vers l'ennemi, un tuyau rouge file jusqu'à lui ; la pompe bat, il enfle en tremblant, puis s'envole doucement (« Gonflé ! »). Une flèche le crève : « Pan ! », confettis, ceux du dessous blessés, il retombe. Sans flèche, il crève seul au bout de 6 s.
- [ ] **Gonfleur, niveau 2** : deux tuyaux à la fois ; un ballon crevé fait gonfler deux voisins (sans tuyau).
- [ ] **Gonfleur, niveau 3** : avec quatre ennemis à portée, « Le défilé ! » : tous gonflent d'un coup et montent à 10 m ; un gros devient un énorme ballon ; une flèche dans un ballon du défilé : « Défilé ×2 : +… ». Un boss ni un volant ne gonflent jamais ; un ennemi aplati par l'Enclume non plus.
- [ ] **Bowling** (avalanche + barricade, sur la voie ; partie 12.22) : « Trios : Bowling, inscrit au catalogue » ; l'arche rouge et or enjambe la voie. Fusion : une Avalanche et une barricade au niveau 3.
- [ ] **Bowling, niveau 1** : 9 m plus loin vers la forêt, les ennemis qui arrivent vont se ranger en triangle (« Rangés ! ») ; une boule de glace part de l'arche, roule vers la forêt le long de la voie et les envoie voler en tournoyant ; toutes les quilles tombées : « STRIKE ! » (le tableau aussi). Une flèche dans la boule : « Boule givrée ! », elle accélère et éclate au premier ennemi (« Éclats de glace ! », gelés).
- [ ] **Bowling, niveau 2** : après un strike, une boule deux fois plus grosse ; des quilles debout : une deuxième boule, « SPARE ! ».
- [ ] **Bowling, niveau 3** : avec cinq ennemis près des quilles, « La partie parfaite ! » : trois triangles, douze boules d'affilée, la dernière énorme ; le tableau affiche 300.
- [ ] **Volcan** (feu d'artifice + tremplin, sur la voie ; partie 12.23) : « Trios : Volcan, inscrit au catalogue » ; le volcan noir fendu de lave, couronné de fusées. Fusion : un Feu d'artifice et un tremplin au niveau 3.
- [ ] **Volcan, niveau 1** : un ennemi sur le cône : l'éruption (lueur rouge, braises, fumée), il part en « Bombe de lave ! » en tournoyant et retombe sur un groupe : explosion, blessés renversés, une flaque de lave en feu. Une flèche tirée à travers le panache, juste après l'éruption, s'enflamme.
- [ ] **Volcan, niveau 2** : un anneau de lave en feu autour du cône ; les flaques des bombes durent plus longtemps.
- [ ] **Volcan, niveau 3** : avec quatre ennemis sur la voie vers la forêt, « Éruption ! » : la montagne grandit, la lueur rougit, une coulée de lave descend la voie vers la forêt, repousse et brûle tout ce qu'elle touche, et laisse de la lave derrière elle ; puis la montagne redescend.
- [ ] **Beffroi** (boussole folle + phare, au bord ; partie 12.24) : « Trios : Beffroi, inscrit au catalogue » ; la tour de pierre, ses cadrans dorés, sa cloche. Fusion : une Boussole folle et un phare au niveau 3.
- [ ] **Beffroi, niveau 1** : la cloche sonne et se balance ; les ennemis proches repartent à reculons sur le chemin qu'ils venaient de faire (« << Rembobiné ! », traînée sépia), puis reprennent leur route ; les aiguilles tournent à l'envers, plus vite à ce moment.
- [ ] **Beffroi, niveau 2** : à l'arrivée, « Figé ! » 2 s ; une flèche sur eux à ce moment : « Sonné ! ».
- [ ] **Beffroi, niveau 3** : le Phare du passé depuis le 2 novembre au soir (Minuit, l'arrêt du temps, est passé à la Grande Horloge, l'ultime) : voir « Le Beffroi, son nouveau niveau 3 ».
- [ ] **Colisée** (chant des sirènes + barricade, sur la voie ; partie 12.25) : « Trios : Colisée, inscrit au catalogue » ; deux gradins de marbre de part et d'autre de la voie, la sirène de bronze. Fusion : un Chant des sirènes et une barricade au niveau 3.
- [ ] **Colisée, niveau 1** : une arène de pans de mur sort du sol autour d'un groupe (« Gladiateurs ! ») ; ils se battent entre eux ; une flèche sur l'un : « Éliminé ! » ; le dernier debout : « Champion ! », une couronne de laurier dorée au-dessus de lui ; il frappe les autres ennemis et ne prend plus de dégâts (même de tes flèches) ; les murs redescendent. La vague finit bien quand il ne reste que lui.
- [ ] **Colisée, niveau 2** : deux champions à la fois ; un champion qui tombe : des bulles roses, « Hypnotisés ! », ses voisins se battent.
- [ ] **Colisée, niveau 3** : avec six ennemis à portée, « Les jeux du cirque ! » : une grande arène, un grand combat ; le vainqueur devient géant (« Le grand champion ! ») et renverse ceux qu'il frappe.
- [ ] **Montagnes russes** (chaîne de forçats + tremplin, sur la voie ; partie 12.26) : « Trios : Montagnes russes, inscrit au catalogue » ; le circuit, ses deux gares et son looping le long de la voie (vérifier qu'il est bien dans le sens de la voie). Fusion : une Chaîne de forçats et un tremplin au niveau 3.
- [ ] **Montagnes russes, niveau 1** : un wagon sort de la gare, file au ras de la voie et ramasse des ennemis (« Embarqué ! ») assis dessus ; dans le looping, en haut, la tête en bas, ils tombent (« Au vol ! ») ; le wagon rentre en gare d'arrivée.
- [ ] **Montagnes russes, niveau 2** : un train de trois wagons ; ceux qui tombent retombent sur un groupe et le renversent.
- [ ] **Montagnes russes, niveau 3** : avec six ennemis près du circuit, « Le grand huit infernal ! » : dix wagons gardent leurs passagers, montent le tremplin et sautent dans le vide (« Dans le vide ! ») ; « CRASH ! » sur un groupe, les passagers meurent à l'atterrissage.

**Après le premier essai des trios (1er novembre au soir ; `Docs/Refonte_TowerDefense.md`, partie 12.27 ; F4, B)** — le Volcan et les Montagnes russes sont à refaire (propositions à trier) : ne pas les retester.
- [ ] **F4, premier appui** : la console écrit « Vitrine : 2 défenses aériennes posées, 20000 PV au joueur et à la tour » ; un lance-filet au niveau 3 dans chaque clairière, près de la tour, et plus de baliste (depuis le 2 novembre au soir : seulement de l'anti-aérien) ; ils ne visent que les volants, jamais les ennemis au sol, qui restent pour le bâtiment montré ; ils restent quand F4 passe au bâtiment suivant ou retire tout. La barre de vie du joueur et celle de la tour : 20 000.
- [ ] **Lanceur de baudruches, les volants** : une baudruche posée pendant qu'il y a des volants : ils viennent tourner au-dessus de sa tête et la picorent (elle s'écrase à chaque coup) ; ils ne piquent plus sur toi tant qu'elle tient ; quand elle éclate, ils repartent (et prennent les confettis s'ils sont tout près).
- [ ] **Lanceur de baudruches, les tireurs** : les tireurs (et les liches) viennent jusqu'à 7 m d'elle et lui tirent dessus ; leurs projectiles ne te blessent pas ; elle encaisse leurs coups.
- [ ] **Colisée, les tireurs** : un tireur pris dans l'arène reste au milieu et tire sur ses voisins ; un tireur champion poursuit les ennemis et leur tire dessus de 8 m (il ne reste plus planté à regarder de loin).
- [ ] **Colisée, plusieurs champions** : la vitrine en pose trois ; les champions sortis de deux arènes différentes ne se battent plus entre eux ; la vague finit quand il ne reste que des champions.
- [ ] **Colisée, niveau 3** : le grand champion est doré de la tête aux pieds, brillant ; « Le guerrier d'or ! » devant toi ; il reprend ses couleurs quand il tombe.
- [ ] **Colisée, les tourelles** (12.30) : avec une baliste, un mortier ou un lance-filet à portée, aucun ne tire sur un champion ; ils visent les autres ennemis.
- [ ] **Le scaling des bâtiments** (12.31) : avec les seules tourelles de F4, passer les vagues (gong) jusqu'au mode infini : vers la vague 10 à 15, elles sont débordées (les ennemis arrivent à la tour). Les Wave Settings ont un réglage *Structure Strength Exponent* (0,5). À l'ouverture de Unity, la console écrit « Dégâts des bâtiments −20 % … Structure Power : facile 0.6, Normal 0.8, Difficile 1, Impossible 1.28 ».
- [ ] **Colisée retiré** (12.30) : pendant qu'un champion se bat, F4 (le Colisée disparaît) : il perd sa couronne et repart aussitôt vers la tour comme un ennemi, sans rester planté.
- [ ] **Les autres charmes** (Chant des sirènes, Boussole folle, Bowling) : un tireur charmé va bien jusqu'à son point (la rose, la place de quille, sa cible) au lieu de s'arrêter à 17 m.

**La Grande bombarde, à la place du Volcan (1er novembre au soir ; `Docs/Refonte_TowerDefense.md`, partie 12.28 ; F4, B)**
- [ ] À l'ouverture de Unity, la console écrit « Duos : Grande bombarde, prefab créé… », « …, définition créée… », « Trios : Grande bombarde, inscrit au catalogue » et « Trios : Volcan, retiré du catalogue ». F4 ne montre plus le Volcan.
- [ ] **Le modèle** : une bombarde de bronze sur quatre ressorts de cuivre, cinq cercles de fer sur le fût, la cible de la mèche sur son poteau (elle se tourne vers toi), le cordon jusqu'à la culasse, trois bombes sur l'affût. F4 la pose dans les clairières : niveaux 1 et 3 à droite, 2 à gauche. Niveau 2 : le baril et deux fanions ; niveau 3 : la couronne dorée et deux grandes fusées.
- [ ] **La poudre** : une flèche dans la gueule : « Poudre 12 % », un cercle s'allume (orange) à chaque 20 % avec un grondement ; des ennemis tués près d'elle la remplissent aussi ; la fumée monte de la gueule. Pleine : « Pleine ! Tire dans la mèche », le cercle du haut clignote.
- [ ] **La mèche** : une flèche dans la cible avec peu de poudre : « Pas assez de poudre » ; avec de la poudre : « La mèche ! », l'étincelle court le long du cordon, puis « Le bouquet ! ×N » : les bombes partent en cloche sur les groupes, explosent en couleur et projettent les ennemis ; la bombarde s'enfonce sur ses ressorts et rebondit ; les cercles s'éteignent.
- [ ] **Niveau 2** : elle se remplit deux fois plus vite ; au tir, « Secousse ! », les ennemis à 10 m tombent.
- [ ] **Niveau 3** : pleine, « La bombarde déborde ! », puis « Le bouquet final ! » : 40 bombes sur toutes les voies (regarder le Quest).
- [ ] **Fusion** : un Feu d'artifice (au bord) et un tremplin (sur la voie) au niveau 3 : la flèche « TRIO » ; la Grande bombarde naît à la place du Feu d'artifice, au bord (pas sur la voie).

**Les Montagnes russes refaites : le Grappin (1er novembre au soir ; `Docs/Refonte_TowerDefense.md`, partie 12.29 ; F4, B)** — remplacent les essais « Montagnes russes » plus haut.
- [ ] À l'ouverture de Unity, la console écrit « Duos : Montagnes russes, grappin du 1er novembre », « …, textes des niveaux revus » et « …, texte du catalogue revu ».
- [ ] **Le modèle** : un ovale de rails à 5 m au-dessus de la voie, trois portiques, l'enseigne dorée à ampoules au milieu ; les wagons rouges et, dessous, la chaîne et le grappin à trois griffes. Niveau 2 : fanions et lampions ; niveau 3 : un tremplin à chaque bout.
- [ ] **Niveau 1** : deux wagons tournent sans arrêt. Sur la ligne droite qui va **vers la forêt**, le grappin descend et se balance d'un bord à l'autre de la voie ; il accroche les ennemis (« Accroché ! »), qui pendent sous lui en se balançant ; au virage du côté de la forêt, ils sont jetés vers la forêt (« Au vol ! ») et retombent une douzaine de mètres plus loin. Compter : il doit en prendre bien plus d'un sur 30.
- [ ] **Le sens** : tourner le circuit d'un demi-tour (Déplacer, joystick) : la ligne droite qui ramasse change, c'est toujours celle qui va vers la forêt.
- [ ] **Niveau 2** : trois wagons ; avec deux accrochés au moins, « Fouetté ! » : les ennemis sous la grappe tombent.
- [ ] **Niveau 3** : avec six ennemis près du circuit, « Le grand huit infernal ! » : six wagons filent, accrochent tout, montent le tremplin côté forêt et sautent (« Dans le vide ! ») ; « CRASH ! » sur un groupe, les accrochés meurent en retombant.

**Lot 7 : les trois derniers duos, le Toboggan revu, les premiers bugs (31 octobre au soir ; `Docs/Refonte_TowerDefense.md`, partie 12.18 ; F4, B, H)**
- [ ] À l'ouverture de Unity, la console écrit « Duos : Boussole folle / Lanceur de baudruches / Boule à neige, prefab créé… », « …, définition créée… », « …, inscrit au catalogue » ; pour le Toboggan, « Duos : Toboggan, toboggan revu, 31 octobre », « …, textes des niveaux revus » et « …, texte du catalogue revu ».
- [ ] **Chaîne de forçats** : la vitrine (F4) jusqu'à elle, des ennemis liés par ses deux chaînes ; plus de plantage (StackOverflowException).
- [ ] **Avalanche** : les ennemis avalés tournent avec la boule sans sauter ; sous le pont du milieu, la boule reste sur la voie.
- [ ] **Toboggan revu** : un portique enjambe la voie ; l'ennemi qui passe sous la plate-forme grimpe l'échelle (sur le côté), glisse sur la piste en l'air et décolle du tremplin vers la forêt, sous les notes du jury ; au deuxième passage, il passe dessous sans rien traverser (même un grand ennemi, sous la traverse du bas). Niveau 2 : « Strike ! », il renverse ceux sur qui il retombe. Niveau 3 : le saut à ski.
- [ ] **Boussole folle** (piques + aimant, sur la voie) : sur la rose, « Perdu le nord ! », l'ennemi tourne en rond sur les pointes, blessé ; l'aiguille s'affole. Niveau 2 : « Demi-tour ! », il repart vers la forêt. Niveau 3 : « La rose des vents ! », toute la voie tourne en rond, puis repart vers la forêt.
- [ ] **Lanceur de baudruches** (mortier + bulle) : la baudruche archer se gonfle en vol et se pose devant les ennemis, qui s'arrêtent pour la frapper ; elle éclate en confettis ; une flèche la crève (« Pop ! »). Niveau 2 : « Boing ! », elle rebondit et les entraîne. Niveau 3 : « Le char de carnaval ! », une baudruche de 7 m, « Confettis ! ».
- [ ] **Boule à neige** (bulle + givre) : la boule de verre transparente tombe sur un groupe (« Enfermés ! ») ; il y neige, ils se figent (« Figé ! ») ; une flèche la brise (« Éclatées ! »). Niveau 2 : « Secouée ! », les figurines tourbillonnent. Niveau 3 : « La boule immense ! » sur toute une voie ; brisée, les figurines meurent.

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

**Le Beffroi, son nouveau niveau 3 : le Phare du passé (2 novembre au soir ; `Docs/Refonte_TowerDefense.md`, 12.45)**
- [ ] À l'ouverture de Unity, la console écrit « Le Beffroi, son nouveau niveau 3, le Phare du passé (2 novembre au soir) : … » (le matériau du faisceau, le prefab, le texte du niveau 3).
- [ ] Un Beffroi au niveau 3 (F4, la vitrine) avec cinq ennemis à portée : « Le Phare du passé ! », la cloche ; un faisceau sépia part de la lanterne du sommet (pas du milieu de la tour), descend jusqu'au sol et fait un tour complet en 6 s, en partant vers l'ennemi le plus proche ; les aiguilles tournent à l'envers à toute vitesse.
- [ ] Chaque ennemi au sol que le faisceau balaie, sur toutes les voies, même loin : « << Rembobiné ! », il repart à reculons sur son chemin, puis « Figé ! » 3 s ; une seule fois par tour. Les boss et les volants ne bougent pas.
- [ ] Plus de Minuit : ni bulle, ni noir et blanc, ni flèches arrêtées en l'air. Le faisceau revient au plus toutes les 20 s ; la cloche sonne toujours à côté.
- [ ] Le faisceau se voit bien depuis la tour sans cacher la vue (transparent) ; la fluidité quand toute la carte recule en même temps (le Quest).

**Le Colosse de forge, la première ultime (2 novembre au soir ; `Docs/Refonte_TowerDefense.md`, 12.46)**
- [ ] À l'ouverture de Unity, la console écrit « Duos : Colosse de forge, prefab créé », « … ultime du 2 novembre », « définition créée » et « Ultimes : Colosse de forge, inscrit au catalogue » ; aucune erreur. Le catalogue `Resources/TowerDuos` a une liste **Ultimes** (le Colosse, ses trios : Enclume et Grande bombarde).
- [ ] **F4** jusqu'après les trios : « Vitrine : Ultime : Colosse de forge, sur la voie du centre ». Un géant de fer à genoux (18 m), les genoux de part et d'autre de la voie, la fournaise dorée qui luit, les cheminées qui fument, le marteau posé au sol devant son genou droit, l'enclume dorée à côté. Les ennemis passent entre ses genoux.
- [ ] Sans tirer : toutes les 5 s, il tourne le buste, lève le marteau par-dessus sa tête et le lance sur le groupe le plus serré ; le marteau tourne en l'air, frappe (poussière, étincelles, le sol rougit), les survivants volent en l'air, puis il revient à sa main.
- [ ] **Ta flèche** : tirer au sol ou dans un ennemi à 30 m de lui : le marteau y va (dès qu'il est revenu, si une flèche arrive pendant le vol) ; un ennemi touché est suivi ; un volant touché en l'air est frappé en l'air. Une flèche plus loin que 30 m ne fait rien.
- [ ] Un boss sous le marteau : ni projeté ni tué d'un coup, il perd un peu de PV (1,5 % de ses PV max par coup).
- [ ] **La coulée de fonte** (toutes les 25 s, six ennemis au moins sur la voie vers la forêt) : « La coulée de fonte ! », un jet de fonte de la poitrine au sol, puis un front orange descend la voie vers la forêt, brûle et repousse en l'air tout ce qu'il touche, et laisse la fonte au sol quelques secondes ; une lueur orange le suit.
- [ ] **La transformation, en jeu** : une Enclume (ou une Grande bombarde) au niveau 3, entre les vagues, avec 10 plans de ses tours (**B** plusieurs fois) : un plan d'une de ses tours sorti de la montre sur le trio : « Ultime : Colosse de forge (10 plans) » ; poser : le trio disparaît, le géant apparaît en plan bleu, 3 coups de marteau : « Ultime : Colosse de forge ! ». Pareil par le panneau du trio (la carte « Ultime »).
- [ ] Pendant une vague : « L'ultime se bâtit entre les vagues » ; avec moins de 10 plans : « Il te faut 10 plans : … » ; une deuxième ultime dans la même partie : « Une seule ultime par partie ».
- [ ] **L'arène de MK** : le Colosse y descend le premier, plus petit, sur le plateau ; « Le Colosse de forge est là ! » au début du combat. Une flèche dans MK : le marteau part, MK tombe au sol (en vol aussi), s'étend 3 s (« Plaqué au sol ! »), se relève et reprend (en vol dans sa phase en vol) ; pas plus d'une fois toutes les 12 s. À la fin de l'arène, le Colosse retrouve sa taille et sa place.
- [ ] La vue depuis la tour (le géant, le marteau levé), la fluidité (le Quest) pendant la coulée.
**Le Colosse de forge, refait (2 novembre au soir, après l'essai du joueur ; `Docs/Refonte_TowerDefense.md`, 12.47)**
- [ ] À l'ouverture de Unity, la console écrit « Duos : Colosse de forge, finition du 2 novembre » (le modèle refait) et « La finition du Colosse de forge (2 novembre au soir) : Colosse_Fer_Relief.png en carte de normales, Ultime_Colosse_fer, … » ; aucune erreur.
- [ ] **L'éveil** (F4 jusqu'au Colosse, ou bâti au marteau) : « Le Colosse de forge s'éveille ! », le sol se fend de fonte, il sort de terre dans la roche qui vole et la fonte qui jaillit, les manettes vibrent ; debout, ses yeux, ses veines et sa fournaise s'allument, la fournaise crache le feu ; il lève son marteau, un éclair le frappe, il l'abat : « LE COLOSSE DE FORGE ! », l'onde renverse tous les ennemis autour.
- [ ] **Le ciel de forge** : rouge et noir pendant l'éveil, encore teinté tant que le Colosse est là, plus fort pendant la coulée ; des éclairs au loin ; il revient au ciel normal quand la vitrine retire le Colosse.
- [ ] **Le géant** : le fer martelé (creux, rayures, rouille) qui brille un peu, le bronze et l'or polis ; les veines de fonte qui coulent et palpitent ; les braises de la fournaise, la fumée et les étincelles des cheminées, les gouttes de fonte du marteau ; la lumière orange de la fournaise autour de lui.
- [ ] **Chaque coup** : un éclair sur le marteau levé (le ciel s'éclaire), le marteau qui vole avec sa traînée de feu et crépite, un second éclair sur l'impact, l'onde de choc, la roche qui vole, les fissures de fonte qui refroidissent, le feu ; les ennemis volent haut ; les manettes vibrent. Les éclairs ne sont pas trop fréquents ni trop aveuglants (sinon : *Strike Flash*, *Interval*).
- [ ] **La coulée** : la fournaise explose, un gros torrent de fonte coule de la poitrine, puis une rivière de fonte animée descend la voie derrière une vague qui roule, flambe et éclabousse ; « Fondu ! » sur ceux qu'elle prend, projetés vers la forêt ; la rivière brûle ceux qui y marchent, puis refroidit (croûte noire), s'enfonce et disparaît ; elle suit bien la voie et le sol (virages, ponts).
- [ ] La fluidité (le Quest) pendant l'éveil et la coulée.
- [ ] **Les sons de la fonte** (freesound, CC0) : la console écrit « Les sons du Colosse de forge (2 novembre au soir) : m_PourClip : Colosse_Fournaise.wav ; m_SizzleClip : … ; m_FlowClip : … ». Au début de la coulée, le souffle de feu et le grondement de la fournaise ; le grésillement là où la fonte touche le sol et sous la tête du marteau ; la coulée qui bouillonne en suivant le front, sans claquement à chaque tour de la boucle, et qui s'éteint doucement à la fin.

**Le Colosse de forge, à deux mains (2 novembre, tard, après le deuxième essai ; `Docs/Refonte_TowerDefense.md`, 12.48)**
- [ ] À l'ouverture de Unity, la console écrit « Duos : Colosse de forge, les deux mains du 2 novembre » (le modèle refait), et que les textes du niveau et du catalogue sont revus ; aucune erreur.
- [ ] **Le modèle** : le bras gauche tient une masse de forge, posée à plat sur le lingot de fonte de la grande enclume, sur son billot devant le genou gauche ; rien ne traverse rien (le genou, le billot).
- [ ] **Le ciel** : il change pendant l'éveil, puis revient au ciel de la partie en quelques secondes ; il ne revient que pendant une coulée.
- [ ] **La forge** (F4 jusqu'au Colosse, des ennemis sur la voie) : un coup de masse sur l'enclume toutes les 1,2 s environ, la masse retombe juste sur le lingot ; les étincelles, le fer qui sonne (plus aigu à chaque coup) ; un éclat de fonte part sur un ennemi à chaque coup ; le lingot passe du rouge sombre au blanc. Sans ennemi, il frappe plus lentement.
- [ ] **Le grand coup** (au 5e) : il lève la masse par-dessus la tête, un éclair la frappe, il l'abat ; le lingot chauffé à blanc part en crépitant sur le groupe le plus serré, la foudre tombe avec lui, l'onde, la roche, le sol fendu ; un lingot neuf revient sur l'enclume.
- [ ] **Les marteaux** : tire sur des ennemis à moins de 30 m : il lève son marteau et lance un marteau de fonte sur chacun (le bras tourné vers la cible), le marteau tournoie, frappe, un petit éclair, les ennemis sautent. Une flèche dans le sol près d'un ennemi : le marteau va sur lui. Sans tirer : le bras se repose.
- [ ] **Avec la pluie de flèches et le multishot** (R ou Tab pour les pièces) : il se déchaîne, 5 marteaux par seconde au plus, sans lag au casque (sinon : *Throw Interval*, *Queue Size*).
- [ ] **L'arène de MK** : tes flèches dans MK y envoient les marteaux ; le lingot le plaque au sol (pas plus d'une fois toutes les 12 s).
- [ ] La sensation : on sent sa puissance et le nombre de coups ; le fer qui sonne et les coups répétés ne lassent pas (sinon : le volume, *Forge Interval*).

**Le Colosse de forge, la force (2 novembre, tard, après le troisième essai ; `Docs/Refonte_TowerDefense.md`, 12.49)**
- [ ] À l'ouverture de Unity, la console écrit « La force du Colosse de forge (2 novembre, tard ; 12.48) : m_HammerDamage 40 → 160 ; … ; m_FlowStay 4 → 6. » ; aucune erreur.
- [ ] **Les marteaux** : aussi grands que le sien, ils tournoient haut dans leur traînée de feu, crépitent, se remettent tête en bas et frappent ; la foudre tombe avec chacun, l'onde de choc, l'explosion, la poussière en anneau ; tout ce qui survit vole en vrille ; le marteau reste planté un instant puis s'enfonce dans le sol.
- [ ] Au début d'une salve, un éclair frappe son marteau levé.
- [ ] Le coup complet (l'éclat de lumière, le tonnerre, la roche qui vole, le sol fendu et en feu, les manettes) revient souvent sans être à chaque marteau ; le ciel s'éclaire au plus une fois par seconde environ, sans clignoter.
- [ ] **La force** : un marteau tue net les petits ennemis des premières vagues ; le lingot (au 5e coup) vide un groupe ; seul, sans tirer, le Colosse tient la voie comme un bon trio.
- [ ] **Sous la pluie de flèches** (R ou Tab) : 5 marteaux par seconde, sans lag au casque (sinon : *Heavy Interval*, *Throw Interval*).
- [ ] **Un boss des vagues** (à une vague de boss) sous les marteaux et le lingot : il perd nettement plus qu'avant (les dégâts des coups, plus sa part de PV), sans être projeté (12.50). **MK** : comme avant.
- [ ] Ensuite : relancer le tableau (`py Tools/equilibrage_tours.py`, depuis `Project Archery/`) ; le Colosse est dans la feuille « Trios », sous les trios. *(Fait le 2 novembre, tard, avec le Grand Manège : le prefab avait bien ses chiffres.)*

**Le Grand Manège, la deuxième ultime (2 novembre, tard ; `Docs/Refonte_TowerDefense.md`, 12.51)**
- [ ] À l'ouverture de Unity, la console écrit « Duos : Grand Manège, prefab créé », « … manège du 2 novembre », « définition créée », « Ultimes : Grand Manège, inscrit au catalogue », puis « La finition du Grand Manège (2 novembre) : Manege_Bois_Relief.png en carte de normales, Ultime_Manege_… » ; aucune erreur. Le catalogue `Resources/TowerDuos`, liste **Ultimes** : le Colosse et le Manège (ses trios : Gonfleur, Colisée, Montagnes russes).
- [ ] **F4** jusqu'après le Colosse : « Vitrine : Ultime : Grand Manège, sur la voie du centre ». **L'éveil** : « Le Grand Manège s'éveille ! », il sort de terre en tournant comme une boîte à musique, la terre et les confettis volent, les manettes vibrent ; debout, les ampoules s'allument, la fanfare, six fusées partent du toit et éclatent, les chevaux se cabrent ; « LE GRAND MANÈGE ! », l'onde de confettis renverse les ennemis autour. Le **ciel de fête** (violet et rose, la grosse lune dorée, les aurores) pendant l'éveil, puis il revient au ciel de la partie en quelques secondes.
- [ ] **Le modèle** : un carrousel de 19 m perché sur son mât, le toit rayé rouge et crème, les nervures et les miroirs dorés, huit grands chevaux et quatre petits qui montent et descendent en tournant ; les ampoules en chenille ; la lumière sous le chapiteau, de l'or au rose ; les confettis qui tombent du chapiteau, les paillettes du sommet, la vapeur de l'orgue ; le bois peint et verni (pas d'aplats unis). Les ennemis passent sous le plateau. Pas de faces qui scintillent ; de près, pas de fentes visibles entre les pans du toit.
- [ ] **La ronde** (des ennemis sur la voie, sans tirer) : toutes les 1,2 s environ, le ruban doré part du bord du chapiteau, attrape un ennemi (« Dans la ronde ! ») et l'amène sur le plateau ; il y tourne entre les chevaux (pas dedans), en tournant sur lui-même. Un boss, un volant : le ruban le fouette sans l'attraper.
- [ ] **La grande éjection** (au 5e coup) : le manège s'emballe, les ampoules clignotent ensemble, la fanfare, une fusée monte ; tous ceux qui tournent partent d'un coup en vrille et retombent sur le groupe le plus serré, chacun sous un bouquet de feu d'artifice, l'onde dorée, les confettis, les voisins projetés.
- [ ] **Les chevaux** (tire sur des ennemis à 30 m) : à chaque flèche qui touche, un grand cheval saute du manège, galope dans les airs en se balançant, piétine la cible ; un bouquet éclate au-dessus, l'onde, les ennemis projetés ; il reste cabré un instant, puis s'efface en paillettes. Le manège tourne plus vite tant que tu tires. **Sous la pluie de flèches** (R ou Tab) : 5 chevaux par seconde, sans lag au casque.
- [ ] **La charge** (au moins six ennemis sur la voie, entre le manège et la forêt) : « La charge ! », le ciel de fête, les huit chevaux sautent sur la voie et la descendent au galop vers la forêt ; les ennemis croisés sont piétinés et projetés (« Piétiné ! ») ; des bouquets au-dessus de la troupe ; au bout, ils s'effacent ; le ciel revient. Sur les voies qui tournent et les ponts, ils suivent la voie et le sol.
- [ ] **La transformation, en jeu** : un Gonfleur, un Colisée ou des Montagnes russes au niveau 3, entre les vagues, 10 plans de ses tours : « Ultime : Grand Manège (10 plans) » ; poser, bâtir au marteau : « Ultime : Grand Manège ! ». Une seule ultime par partie (avec un Colosse déjà là : refusé).
- [ ] **Un boss des vagues** sous les chevaux, les éjectés et la charge : il perd ses dégâts plus sa part de PV, sans être attrapé ni projeté.
- [ ] **L'arène de MK** : le Manège y descend, plus petit ; « Le Grand Manège est là ! ». Les flèches dans MK y envoient les chevaux ; au 5e tour, tous les chevaux le chargent dans les airs : « Étourdi ! », MK tourne sur lui-même 3 s (en vol, il flotte et dodeline ; au sol, il reste planté), des étoiles et quatre petits chevaux tournent autour de sa tête, puis il reprend le combat ; pas plus d'une fois toutes les 12 s. À la fin, le Manège retrouve sa taille et sa place.
- [ ] **Les sons** (freesound, CC0, découpés le 2 novembre, tard ; le prefab les a dès sa création) : l'orgue doux en boucle, plus aigu quand le manège s'emballe, sans saut audible à chaque tour de la boucle (16 s) ; la fanfare entière à l'éveil et à la charge, sa première phrase à chaque grande éjection (pas les deux en même temps) ; le galop qui suit la charge, sans « pompage » à chaque tour (2,7 s) ; le hennissement ; la fusée qui siffle (assez fort ? remontée de 35 dB) ; le bouquet qui éclate à chaque coup (lassant à 5 par seconde ?).
- [ ] **La force** : seul, il tient la voie comme un bon trio (surtout contre les groupes) ; avec tes flèches, il se déchaîne ; trop fort ou trop faible : *Ride Damage*, *Eject Damage*, *Horse Damage*, *Gallop Interval*, puis relancer le tableau.

**La Grande Horloge, la troisième ultime (2 novembre, tard ; `Docs/Refonte_TowerDefense.md`, 12.52)**
- [ ] À l'ouverture de Unity, la console écrit « Duos : Grande Horloge, prefab créé », « … horloge du 2 novembre », « définition créée », « Ultimes : Grande Horloge, inscrit au catalogue », puis « La finition de la Grande Horloge (2 novembre) : Horloge_Glace_Relief.png en carte de normales, … » ; aucune erreur. Le catalogue, liste **Ultimes** : le Colosse, le Manège, l'Horloge (ses trios : Bowling, Beffroi).
- [ ] **F4** jusqu'après le Manège : « Vitrine : Ultime : Grande Horloge, sur la voie du centre ». **L'éveil** : « La Grande Horloge s'éveille ! », le givre au sol, elle sort de terre, ses anneaux à toute vitesse ; une aurore tombe sur elle ; ses douze chiffres s'allument un à un (un tic chacun) ; ses aiguilles tournent à l'envers et s'arrêtent sur midi ; la cloche : « LA GRANDE HORLOGE ! », l'onde de givre fige les ennemis autour. Le **ciel de Minuit** (bleu nuit, la grande lune, les aurores) pendant l'éveil, puis le ciel de la partie.
- [ ] **Le modèle** : l'horloge astronomique de 18 m qui flotte au-dessus de son socle (elle monte et descend), le cadran bleu cerclé d'or, son ciel étoilé, les douze chiffres romains de glace qui luisent, lisibles et **tournés vers toi** où que tu sois ; les engrenages derrière, la cloche, les stalactites ; les trois anneaux qui tournent ; les flocons, le givre, la brume froide ; la glace taillée (des fêlures, des bulles). Pas de faces qui scintillent.
- [ ] **Les chiffres** (des ennemis à 30 m, sans tirer) : un tic toutes les 1,2 s ; à chacun, le chiffre suivant (I, II, III…) se détache et tombe sur un ennemi : l'aurore, « IV » en lettres de glace, les voisins figés de givre. L'aiguille des heures avance avec les chiffres partis. Sans ennemi, elle attend.
- [ ] **Minuit**, après le XII : les aiguilles se rejoignent sur midi, « Za Warudo ! Temps, arrête-toi ! », la bulle part et couvre toute la carte, noir et blanc, tout s'arrête (l'horloge aussi), tes flèches attendent en l'air ; douze coups (« Dong ! 1 »…), un chiffre repousse à chacun ; au douzième, « Le temps reprend son cours » : tes flèches repartent toutes ensemble et tous les figés éclatent (« Brisé ! », des aurores). Puis le I revient.
- [ ] **L'écho du temps** (tire sur des ennemis à 30 m) : 1 s après chaque flèche qui touche, une flèche de glace géante part du cadran et frappe au même endroit : l'aurore, l'onde, les ennemis projetés et figés ; elle reste plantée un instant, puis éclate en givre. **Sous la pluie de flèches** : 5 échos par seconde, sans lag au casque.
- [ ] **La transformation, en jeu** : un Bowling ou un Beffroi au niveau 3, entre les vagues, 10 plans : « Ultime : Grande Horloge (10 plans) » ; poser, bâtir : « Ultime : Grande Horloge ! ».
- [ ] **Un boss des vagues** : il prend les chiffres, les échos et le dégel, plus sa part de PV ; Minuit le fige toute la durée (9 s), comme les autres (3 novembre).
- [ ] **Les tailles revues** (3 novembre, après ton essai) : la brume au pied de l'Horloge, la poussière et les éclats de glace aux impacts des chiffres et des flèches, plus petits ; l'aurore d'un chiffre deux fois plus petite que celle d'un écho.
- [ ] **Sans les cristaux** (3 novembre) : à l'ouverture de Unity, la console écrit « Duos : Grande Horloge, horloge du 3 novembre » ; le socle n'a plus ses trois cristaux ; le reste de l'Horloge est là et marche comme avant (chiffres, aiguilles, anneaux, cloche, échos).
- [ ] **Le silence de Minuit** (3 novembre) : quand le temps s'arrête, la musique, l'ambiance, les tours et les ennemis se taisent d'un coup ; restent les douze coups de cloche et tes sons (l'arc, tes pas, la montre). Au douzième coup, tout revient avec la cymbale. Aussi dans l'arène (la musique de MK). Après Minuit, plus rien ne reste muet (la musique, les boucles des tours).
- [ ] **Le bruit de l'arrêt du temps** (3 novembre) : à l'ouverture de Unity, la console écrit « Les sons de la Grande Horloge (2 novembre) : m_TimeStopClip : Horloge_Temps_Arrete.wav » (ou l'Horloge est refaite avec lui). Au début de Minuit, « Za Warudo ! » : le bourdon saturé monte peu à peu et descend, le grave remonte avec un sifflement, tout enfle, puis la coupure nette, et le premier « Dong ! » tombe dessus, avec la vibration des manettes (retouché le même jour : plus d'impact au départ, plus progressif, moins fort). On l'entend partout sur la carte, même loin de l'Horloge. À dire : le volume ? assez saturé ?
- [ ] **La force des flèches** (3 novembre, les trois ultimes) : un tir parfait sur un ennemi, puis un tir moyen : le marteau du Colosse, le cheval du Manège ou l'écho de l'Horloge fait à peu près 2,5 fois plus de dégâts au parfait (×2 contre ×0,8 d'un tir « Bon ») ; sous la pluie de flèches, chaque flèche de la pluie frappe bien moins, et un tir parfait au milieu de la pluie a quand même son action (la plus faible laisse sa place). Le bilan de la vague (console) le montre.
- [ ] **L'arène de MK** : l'Horloge y descend ; « La Grande Horloge est là ! ». Ses chiffres et les échos de tes flèches frappent MK ; après le XII, Minuit arrête toute l'arène 6 s : MK s'arrête là où il est (en vol aussi), ses boules de feu figées en l'air, le noir et blanc ; tes flèches attendent et le frappent toutes ensemble à la fin ; il reprend exactement où il en était. À la fin, l'Horloge retrouve sa taille et sa place.
- [ ] **Les sons** (freesound, CC0, découpés le 3 novembre) : la console écrit « Les sons de la Grande Horloge (2 novembre) : m_TickClip : Horloge_Tic.wav ; … » (le prefab avait les sons provisoires) ; le tic net, sans souffle (lassant toutes les 1,2 s ?) ; la cymbale à l'envers qui culmine juste quand le temps s'arrête ; les douze coups qui se chevauchent ; au dégel, le coup de cymbale et la série de craquements ; un craquement à chaque chiffre, écho et figé qui éclate ; le carillon des échos.
- [ ] **La force** : seule, elle tient la voie comme un bon trio ; Minuit et le dégel (toute la carte, toutes les 23 s) pas trop forts sur une grande vague ? *Shatter Damage*, *Midnight Time*, *Numeral Damage*, *Echo Damage*, puis relancer le tableau.

**Le 3 novembre : la flèche ULTIME, la pose lisible, la lumière de l'arène (`Docs/Refonte_TowerDefense.md`, 12.55 ; les menaces par voie, 12.56, sont en sommeil)**
- [ ] **La flèche « ULTIME »** : entre les vagues, un trio au niveau 3 (F4, ou B puis les fusions) : une flèche dorée droite plonge sur lui, « ULTIME », le nom de l'ultime et « N / 10 plans » ; le nombre monte quand on prend des plans de ses tours ; elle disparaît pendant une vague, et pour de bon une fois l'ultime bâtie.
- [ ] **La pose lisible** : sortir le plan d'une baliste, d'un mortier, d'une tour de givre : un cercle de cases dorées au sol autour de l'hologramme et les cases de la voie à l'intérieur en doré ; le texte « Portée N m · couvre N m de la voie de … » change quand on bouge ; un piège de la voie ou la barricade : pas de cercle. Déplacer un bâtiment monté : la portée de son niveau.
- [ ] Les noms des voies sont justes : gauche, milieu, droite vus de la tour en regardant la forêt.
- [ ] **Les menaces par voie, en sommeil** : à la fin d'une vague, pas de message « par où ils viendront », pas de mot au-dessus des voies ; le titre de la vague est comme avant ; les ennemis sortent de partout.
- [ ] **Dans l'arène de MK** : sous chaque tourelle posée sur le plateau, une lumière qui l'éclaire d'en bas (avec la rune) ; on les voit mieux pendant le combat.
- [ ] À l'ouverture de Unity, la console écrit « Le talisman de l'ingénieur (3 novembre) : il surcharge toutes les tourelles ; le texte de sa carte dit « dans une de tes tourelles »… » ; la carte du talisman à la boutique et à l'atelier le dit ; le duo Réseau s'annonce avec « … à tes tourelles, qu'ils surchargent ».
- [ ] **Le talisman de l'ingénieur sur toutes les tours** (12.57 ; R ou F pour l'avoir) : une flèche qui traverse un mortier, une tour-aimant, un phare, un duo, un trio ou l'ultime : « Surchargée ! », des éclairs bleus, il tire plus vite pendant quelques secondes ; la barricade se répare toujours ; avec le Réseau (Arc conducteur + talisman), une flèche plantée près de n'importe quel bâtiment s'y relie.
- [ ] **Une partie en Impossible, ses suites** (12.67 ; à l'ouverture de Unity, « Les PV du joueur (4 novembre) : 67 → 100 », puis Ctrl+S) : les Akimbow avec le Multitir, plus de corde lumineuse à côté de toi ; sous 30 % des PV, le rouge bien visible au bord de la vue et qui bat ; le joueur à 100 PV ; la Vigueur « +12 PV max » (la console : « La Vigueur (4 novembre) : +8 → +12 PV max »).
- [ ] **Les ennemis au sol 30 % plus rapides** (12.66 ; à l'ouverture de Unity, la console : « La vitesse au sol des ennemis… ») : en Normal et au-dessus, ceux des voies, les gardiens et les boss des vagues courent plus vite, les jambes aussi (pas de glissade) ; les volants et MK ne changent pas ; en Facile, rien ne change. La tour est-elle de nouveau attaquée ?
- [ ] **L'Avalanche n'avale qu'une fois** (12.65) : au niveau 3, un ennemi déjà roulé jusqu'à la forêt est seulement renversé par la boule suivante ; les voies avancent-elles ? Et **une partie avec des pièces monocibles** (Pointe du chasseur, Transe, Surcharge, Bourreau) pour comparer au journal avec les builds de zone.
- [ ] **Le Carquois géant limité** (12.64) : avec un multishot, son filet se remplit au plus de 2 flèches par seconde (la nuée au niveau 3 : 15 s au moins) ; dans l'arène, MK tient-il plus qu'une nuée ? Au journal : « Vague N : elle commence (… ; au casque) », et « au plus bas : N/M PV » à chaque bilan.
- [ ] **Les plans par coffre** (12.63) : l'orbe de tourelle dit « 1 plan » à « 4 plans », et le donne ; à la vague 1, surtout 1 ou 2 ; à la vague 5, souvent 3 ou 4. Au journal, chaque « Plans : +N ».
- [ ] **La tour à 1 500 PV et les ultimes −30 %** (12.62) : au lancement, la tour affiche 1 500 PV (enregistrer la scène après le baker) ; une expédition aux coffres pendant la montée lui coûte-t-elle des PV visibles, le cor sonne-t-il ? Le Colosse et l'Horloge font-ils encore jeu égal avec le joueur sans l'écraser (le journal : la part des dégâts de l'ultime) ?
- [ ] **Les gardiens des coffres** (12.61) : chaque coffre apparaît avec ses gardiens (2 aux vagues 1 et 2), qui vadrouillent autour au hasard et s'arrêtent de temps en temps (pas tous en même temps). À 15 m d'eux, ils foncent sur toi ; tu peux les tuer à l'arc (de loin aussi), ou courir au coffre et l'ouvrir (il n'est pas verrouillé) en prenant des coups. Éloigne-toi à plus de 30 m du coffre : ils y retournent. Ils ne vont jamais à la tour ; les voies continuent d'envoyer leurs ennemis pendant ce temps. Au journal : « Un coffre apparaît, gardé par N ennemis » (sinon, le NavMesh ne couvre pas les abords des coffres).
- [ ] **La vague fixe** : chaque vague a le même chrono que la première (environ une minute avant le boss) ; plus d'ennui aux vagues 4 à 6 ? Le gong : appeler le boss tôt fait perdre les coffres pas encore pris.
- [ ] **Le troisième essai** (12.60) : à l'ouverture de Unity, la console écrit « Troisième essai (3 novembre) : N réglage(s) changé(s) » (les volants, les boss, le Manège, la Vigueur, le joueur) et « enregistre-la (Ctrl+S) » : **enregistrer la scène**.
- [ ] **Le joueur à 67 PV** : un coup se sent (la barre et le voile rouge) ; après 3 s sans être touché, les PV remontent vite (pleins en 10 s environ). Les coups de MK ne tuent pas d'un coup ?
- [ ] **Les volants** meurent d'une flèche ; en piqué, ils vont au bout et te touchent (au sol et sur la tour), sans remonter au dernier moment.
- [ ] **Les boss** : collé à un boss (moins de 7 m, au sol), il se tourne et frappe ; ses coups font mal. De loin, il meurt en un temps raisonnable (PV −30 %).
- [ ] **Le combo Le style + corde triple** : en l'air, la couronne fait 5 flèches au niveau 1 (9 au niveau 2, 13 au niveau 3).
- [ ] **Le Grand Manège** : toujours impressionnant, mais tes flèches comptent autant que lui (au journal : à peu près la même part des dégâts).
- [ ] **Le cor de la tour** (12.59) : à l'ouverture de Unity, la console écrit « Le cor de la tour (3 novembre) : Assets/_Project/Audio/Tour_Cor.wav posé sur … ». En partie, loin de la tour (plus de 15 m, pas dessus), quand les ennemis la frappent : un cor sonne une fois, et on l'entend venir de la tour (tourner la tête) ; sous la moitié de ses PV, deux fois ; sous le quart, trois. Sur la tour : pas de cor. Le son plaît-il ? Assez fort de loin, pas trop fort de près ?
- [ ] **Le rayon pendant l'attaque** : de loin, il monte très haut, s'élargit et bat en rouge, visible le jour comme la nuit ; quelques secondes après le dernier coup, il redescend doucement.
- [ ] **La fumée** : sous la moitié des PV de la tour, une colonne de fumée noire monte du sommet, plus épaisse quand elle est très abîmée ; elle cesse quand la tour remonte au-dessus de la moitié. La fluidité ne baisse pas.
- [ ] **Tous les bâtiments qui tirent dans l'arène de MK** (12.58 ; F4 puis l'arène, ou une partie) : avec un mortier, une Avalanche, un Feu d'artifice, un Carquois géant, une Loupe, un Lanceur de baudruches, une Enclume, un Gonfleur ou une Grande bombarde construits, ils descendent sur le plateau (6 au plus : l'ultime, puis les trios, les duos, les tours) ; les duos et les trios, réduits, ne traversent ni la paroi ni la voûte ; ils attendent le début du combat.
- [ ] Ce qu'ils lancent rattrape MK **en vol comme au sol** et éclate sur lui (obus, boule de neige, fusées, baudruches, enclumes, bombes) ; plus rien ne tombe au sol sous lui ; les enclumes retombent ensuite au sol (aucune ne reste en l'air).
- [ ] **Le lance-filet** : un grand filet enveloppe MK, « Pris au filet ! », il ralentit ; **le Gonfleur** : le tuyau se plante dans MK, il ne gonfle pas, puis « Pan ! » ; **la Loupe** : la tête de MK grossit un peu (pas au travers de la voûte), sa baliste vise sa tête ; **le Carquois** : tes flèches ratées dans l'arène remplissent son filet, et la volée part droit sur MK ; **la Bombarde** : ta flèche dans la gueule, puis dans la mèche, et la salve part sur MK.
- [ ] **La force** : le combat contre MK n'est pas trop court avec six bâtiments forts (aucun ne lui retire plus de 2 % d'un coup) ; sinon, le dire (les dégâts ordinaires sont ceux de la carte, la portée doublée).
- [ ] À la fin du combat (victoire ou mort), chacun retrouve sa place et sa taille sur la carte.
- [ ] **La fluidité** : une vague chargée avec une ultime et la pluie de flèches ; la Grande Horloge et son Minuit (tes flèches s'arrêtent toujours en l'air et repartent) ; les gerbes des tours s'affichent comme avant.

**Le centre d'entraînement et les torches (2 novembre au soir ; `Docs/Refonte_TowerDefense.md`, 12.43)**
- [ ] À l'ouverture de Unity, la console écrit « Entraînement et torches (2 novembre au soir) : … » (le visage, les mannequins, les cibles retirées, les torches) : **enregistrer la scène (Ctrl+S)**.
- [ ] Plus de cibles pour viser ; sur la voie du milieu, tournés vers la tour : un mannequin à 14 m qui fait des droite-gauche, un groupe de 3 à 24 m, un mannequin à 42 m (après le gué) qui fait des droite-gauche.
- [ ] Chaque mannequin porte le visage dessiné (traits noirs nets, la toile de la tête garde sa couleur), dans le bon sens (pas en miroir).
- [ ] Un mannequin qui va et vient : le pousser (aimant, coup de poing) : il revient sur son trajet ; tué, il tombe et se relève, puis reprend ses allers-retours. Pendant une vague, ils disparaissent ; ils reviennent à la pause.
- [ ] Les torches sur pied des voies sont collées au bord de la terre (plus aucune dans la voie) ; elles s'allument toujours en chaîne au début d'une vague.
- [ ] Des pièges posés sur un mannequin, ou sur le trajet d'un mannequin qui va et vient : il disparaît, et revient si on déplace les pièges.

**Le deuxième essai du 2 novembre, ce qui a changé (`Docs/Refonte_TowerDefense.md`, 12.42) : à vérifier en premier**
- [ ] À l'ouverture de Unity (cliquer dans la fenêtre), la console écrit « Deuxième essai du tower defense (2 novembre) : … » : la courbe des PV (*healthPerWave*), la durée (*firstWaveDuration* 90, *durationIncrease* 12, *calmFraction* 0,35), *structurePower* (Normal 0,6, Difficile 0,97, Impossible 1,43), le tutoriel « Ouvrir les coffres ». Le message du premier essai (2 novembre) doit aussi y être, s'il n'est pas déjà passé.
- [ ] **Les places signalées** : (1) les bords des voies sous les rochers « en l'air » ; (2) les demi-cases au bord de la voie du milieu (mi-terre mi-herbe) : une tour s'y pose, pas des piques ; (3) la voie de droite reste droite sur toute sa longueur (au gué aussi : une baliste dans le lit de la rivière à côté, pas de piques) ; seule la place devant la tour, où les trois voies se rejoignent, est plus large ; (4) et (5) les deux clairières au pied de la voie du milieu (mannequins à gauche, rivière et cible mobile à droite) : vertes en entier pour une baliste ; les terrasses de la jungle aussi, à 3 m comme à 6 m (près de l'antre compris), sauf sous les arbres, les grandes pierres et sur les pentes.
- [ ] Une place encore refusée : l'étiquette dit pourquoi, et entre parenthèses l'objet qui bloque et son parent : noter les deux.
- [ ] Après un lancement dans l'éditeur, `Project Archery/Logs/BuildGrid.png` existe (la grille vue de dessus) : le dire à Claude, il la lira.
- [ ] **Un coffre** : l'orbe de tourelle dit « 2 plans » ; la prendre donne 2 plans. Une partie en ouvrant tous les coffres : des duos, et un trio ?
- [ ] **Les vagues** : la vague 1 a environ 32 s d'accalmie, puis 29 s de montée (Normal) ; les vagues 3 à 5 et leurs boss résistent nettement plus ; MK comme avant.
- [ ] **Les tours** : un peu plus fortes (×1,2) ; noter « Défenses : X % des dégâts » à chaque fin de vague.
- [ ] Le tutoriel, catégorie Construction : « Ouvrir les coffres » (deux plans d'un bâtiment du chantier).

**Le premier essai du 2 novembre, ce qui a changé (`Docs/Refonte_TowerDefense.md`, 12.41) : à vérifier en premier**
- [ ] À l'ouverture de Unity (cliquer dans la fenêtre), la console écrit « Premier essai du tower defense (2 novembre) : … » : les 13 duos « 4 → 2 plans par niveau », les 7 trios « 4 → 3 », « Boss_Dragon.attackDamage : 8,33 → 16,67 ».
- [ ] **Au premier gong** : « Ton chantier : … » (4 bâtiments depuis le 3 novembre : les 3 tours d'un trio, puis la Barricade, ou un autre si le trio l'a déjà ; puis « De quoi bâtir un trio : … », le nom du trio qu'elles font ; relancer plusieurs parties : le trio change) ; 3 s après, « 4 coffres sont apparus, côté gauche ! » (ou droit) : les 4 rayons dorés sont tous du même côté de la forêt ; plus aucun coffre pendant la montée.
- [ ] **La vague suivante** : les coffres de l'autre côté ; à la fin de chaque vague, « Nouveau au chantier : … ».
- [ ] **Un coffre** : un parchemin, une tourelle (seulement des bâtiments du chantier, « 1 plan »), un objet. **Le coffre de boss** : deux objets, sans orbe de plans.
- [ ] **Les plans** : la barricade et les piques donnent 1 plan ; les niveaux 4 et 5 d'une tour coûtent 1 plan ; un niveau de duo 2 plans, de trio 3.
- [ ] **La grille** : poser un bâtiment sur un caillou ou sur un mannequin d'entraînement : il disparaît ; déplacer le bâtiment : il revient. Une barricade ou des piques dans le gué de la rivière, là où passent les ennemis. Viser une place refusée : l'étiquette dit pourquoi (noter celles qui gênent encore, et ce qu'elles disent).
- [ ] **La régénération** : touché, puis 5 s sans coup : les PV remontent doucement (le voile rouge s'efface).
- [ ] **MK** : il fait mal (environ deux fois plus) ; la régénération entre ses attaques suffit-elle ? Sinon, des soins dans l'arène (à décider).
- [ ] **Les trois façons de jouer** (12.39), à rejouer : tout pour l'arc, tout pour les tours, moitié-moitié ; noter « Défenses : X % des dégâts » à chaque vague.

**L'équilibrage du 2 novembre (`Docs/Equilibrage_Tours.xlsx`, `Docs/Refonte_TowerDefense.md`, parties 12.34 et 12.35 ; F4)**
- [ ] À l'ouverture de Unity (cliquer dans la fenêtre), la console écrit « Plafond d'ennemis en même temps (Wave Settings, Max Alive Cap) : 40 → 150 » et « Équilibrage du 2 novembre (Docs/Equilibrage_Tours.xlsx) : … réglage(s) changé(s) » avec la liste. Aucune erreur rouge. Puis relancer `py Tools/equilibrage_tours.py` (le tableau prend les nouvelles valeurs).
- [ ] **Les dégâts** : la baliste tue plus vite aux niveaux 1 à 3 ; le mortier au niveau 3 n'efface plus tout seul un groupe ; les piques avec une barricade devant ne font plus des dégâts « monstrueux » ; les duos et les trios se sentent plus forts que les tours qu'ils remplacent (le Marteau-pilon, l'Enclume, le Gonfleur surtout).
- [ ] **La rampe** (tremplin niveau 5) : un lancer toutes les 4 s, 4 ennemis au plus.
- [ ] **Le verglas** (givre niveau 4) : l'ennemi glisse vers la forêt (en arrière), plus en avant.
- [ ] **La tour-aimant** : au niveau 4, les ennemis attirés ralentissent ; au niveau 5, la singularité toutes les 10 s, et le trou noir a sa texture (un disque violet qui tourbillonne, qui brille).
- [ ] **Le phare, niveau 4** : l'anneau de feu est plus grand et au milieu de la voie, là où passe la vague ; une flèche qui le traverse s'enflamme toujours.
- [ ] **Le Feu d'artifice** : les ennemis restent éblouis plus longtemps (2,5 s au niveau 1), même dégâts qu'avant.
- [ ] **La Loupe, niveau 1** : elle tire déjà des carreaux dans les têtes (du haut de la tour : la baliste du modèle n'apparaît qu'au niveau 2).
- [ ] **Le Carquois géant** : l'arbalète tire seule un carreau toutes les 2 s sur l'ennemi le plus proche, même sans flèches ratées.
- [ ] **Le Toboggan, niveau 3** : jusqu'à 5 ennemis sur le toboggan à la fois.
- [ ] **Le Bowling** : la boule se met en place sous l'arche et attend 2 s en tournant (« Tire dans la boule ! » les trois premières fois) ; une flèche dedans pendant ce temps : « Boule chargée ! », elle part tout de suite, bien plus grosse ; sans flèche, elle part au bout des 2 s ; les quilles l'attendent. Une flèche dans une boule déjà lancée : toujours « Boule givrée ! ».
- [ ] **Le Beffroi** : rembobinage de 8 s de chemin. (Minuit, la bulle en noir et blanc : plus à tester sur le Beffroi ; il passe à la Grande Horloge, l'ultime, le 2 novembre au soir.)
- [ ] **Le Colisée** : une arène toutes les 10 s au niveau 1 (le champion ne dure plus que 12 s, deux à la fois).
- [ ] **Plus d'ennemis à la fois** : en mode infini, plus de 40 ennemis en même temps à partir de la vague 13 ; surveiller la fluidité (le Quest).
- [ ] **Les parchemins valent pour les tours** (12.36) : prendre des parchemins de force (B, la boutique, les coffres) : les tours font plus de dégâts ; d'adresse : elles tirent plus vite ; de vigueur : la tour et les barricades ont plus de PV (leurs barres de vie). La console, à la fin de chaque vague : « Bilan des bâtiments … parchemins : dégâts ×…, cadence ×…, PV ×… ». À la fin de chaque vague, noter « Défenses : X % des dégâts » : les tours ne suivent plus les vagues, elles doivent décrocher sans parchemins et tenir avec.
- [ ] **Le coffre unique** (12.38) : au début de chaque vague, « 3 coffres sont apparus ! » (dorés, plus de caisse violette) ; un coffre propose, de gauche à droite : un parchemin, une tourelle (« CONSTRUCTION »), puis une tourelle ou un objet ; plus jamais de bonus temporaire. Le coffre de boss : deux objets et une orbe violette « 3 plans » ; la prendre : « 3 plans : … » avec les trois bâtiments. V pose un coffre. Le tutoriel « Construction » parle des coffres. *Revu le soir : voir « Le premier essai du 2 novembre » plus haut.*
- [ ] **La puissance des bâtiments par difficulté** (12.36, 12.39) : la console écrit « Normal.structurePower : 0,8 → 0,5 », « Difficile… 1,3 → 0,81 », « Impossible… 1,9 → 1,19 ». Trois parties jusqu'à la vague 6 (tout pour l'arc, tout pour les tours, moitié-moitié) : noter « Défenses : X % des dégâts » à chaque vague et si elle a tenu. En Difficile et en Impossible, les tours comptent encore aux vagues 3 à 5 (« Défenses : X % des dégâts ») ; en Facile, elles ne font pas tout.

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
