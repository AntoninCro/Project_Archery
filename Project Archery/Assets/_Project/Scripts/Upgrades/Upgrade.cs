using System;
using UnityEngine;

namespace Archery.Upgrades
{
    /// <summary>Rareté d'une amélioration (GDD, section 6.1).</summary>
    public enum UpgradeRarity
    {
        Common,
        Rare,
        Legendary,
    }

    /// <summary>
    /// Effet d'une amélioration (GDD, section 6.2). Le sens de <see cref="Upgrade.value"/> dépend de l'effet.
    /// </summary>
    public enum UpgradeEffect
    {
        /// <summary>value = bonus de dégâts par exemplaire (0,1 = +10 %).</summary>
        Damage,

        /// <summary>value = bonus de vitesse des flèches par exemplaire.</summary>
        ArrowSpeed,

        /// <summary>value = bonus de vitesse de l'anneau ; ses bandes s'élargissent aussi de 10 %.</summary>
        QuickCharge,

        /// <summary>value = bonus de largeur de la bande verte (tir parfait).</summary>
        Precision,

        /// <summary>value = PV max en plus.</summary>
        Vitality,

        /// <summary>value = part des points changée en or, en plus des 25 % de base (0,05 = 30 %).</summary>
        Loot,

        /// <summary>value = bonus de dégâts à la tête.</summary>
        HeadHunter,

        /// <summary>value = flèches en plus par tir, en moyenne (0,5 = 50 % de chance d'une flèche en plus ; 1 = une à chaque fois).</summary>
        Multishot,

        /// <summary>value = chance qu'une flèche appelle la foudre (0,2 = 20 %) ; au-delà de 100 %, la foudre est plus forte.</summary>
        Lightning,

        /// <summary>value = ennemis traversés en plus, en moyenne (0,25 = 25 % de chance d'en traverser un).</summary>
        Piercing,

        /// <summary>value = PV rendus par tir à la tête.</summary>
        Vampirism,

        /// <summary>value = flèches en plus quand une flèche se divise en vol, en moyenne (0,5 = 50 % de chance de se diviser en deux).</summary>
        Deluge,

        /// <summary>value = nombre d'ennemis sur lesquels la foudre rebondit.</summary>
        ChainLightning,

        /// <summary>value = chance qu'une flèche explose (0,25 = 25 %) ; au-delà de 100 %, l'explosion est plus forte et plus large.</summary>
        Explosive,

        /// <summary>value = bonus de chance des cartes rares et légendaires en boutique (0,25 = 25 % plus souvent).</summary>
        Luck,

        /// <summary>value = échos du tir, en moyenne (0,25 = 25 % de chance que la volée se répète peu après).</summary>
        Echo,

        /// <summary>value = chance qu'une flèche laisse une zone de glace (0,25 = 25 %) ; au-delà de 100 %, la glace est plus forte.</summary>
        Frost,

        /// <summary>value = vitesse (°/s) à laquelle les flèches tournent vers l'ennemi proche.</summary>
        Homing,

        /// <summary>value = nombre de rebonds d'un ennemi à l'autre.</summary>
        Ricochet,

        /// <summary>value = flèches en plus projetées par la grenade (6 = six flèches de plus).</summary>
        GrenadeArrows,

        /// <summary>value = part du temps de recharge de la grenade enlevée (0,3 = −30 %), sans descendre sous son minimum.</summary>
        GrenadeCooldown,

        // Les parchemins (refonte du 25 octobre) : ajoutés à la fin, car l'asset enregistre l'effet par son numéro.

        /// <summary>Parchemin de force : value = bonus de dégâts (0,12 = +12 %) ; les flèches vont aussi un peu plus vite.</summary>
        Strength,

        /// <summary>Parchemin d'adresse : value = bonus de vitesse de l'anneau et de largeur de la bande verte ; les autres bandes s'élargissent un peu.</summary>
        Dexterity,

        /// <summary>Parchemin de vigueur : value = PV max en plus.</summary>
        Vigor,

        // Le premier lot de pièces nouvelles (étape 3, 26 octobre ; ArrowPieces). value = 1 par niveau, sauf la corde triple.

        /// <summary>Corde triple : value = flèches en plus (2 par niveau), en éventail dans le plan de l'arc.</summary>
        Triple,

        /// <summary>Empennage boomerang : la flèche revient vers le joueur ; rattrapée, le tir suivant est parfait.</summary>
        Boomerang,

        /// <summary>Empennage télékinésie : gâchette de la main de l'arc tenue, la flèche suit la direction de l'arc.</summary>
        Telekinesis,

        /// <summary>Fût aimant : la flèche plantée attire les ennemis autour d'elle.</summary>
        Magnet,

        /// <summary>Fût conducteur : deux flèches plantées proches sont reliées par un arc électrique.</summary>
        Conductor,

        /// <summary>Pointe à mèche : la flèche plantée explose un instant plus tard.</summary>
        Fuse,

        /// <summary>Pointe de givre : un tir parfait gèle l'ennemi ; un ennemi gelé touché éclate en éclats de glace.</summary>
        FrostTip,

        /// <summary>Talisman d'acrobate : en l'air et en glissade, le temps ralentit et chaque tir est parfait.</summary>
        Acrobat,

        // Le deuxième lot (étape 5, 26 octobre ; ArrowPieces.Lot2). value = 1 par niveau.

        /// <summary>Pointe éclatée : à l'impact sur un ennemi, la flèche se brise en éclats qui continuent derrière lui.</summary>
        Splinter,

        /// <summary>Pointe de chasseur : la flèche marque sa cible ; tout ce qui la touche fait double ; la marque passe au voisin.</summary>
        HunterMark,

        /// <summary>Talisman du bourreau : un tir à la tête sur un ennemi affaibli le tue net.</summary>
        Executioner,

        /// <summary>Empennage rebond : la flèche rebondit sur le sol et les murs au lieu de se planter.</summary>
        Bounce,

        /// <summary>Empennage plume : la flèche ne tombe plus, et va un peu plus vite.</summary>
        Feather,

        /// <summary>Empennage de plomb : flèches lourdes, en cloche ; au sol, une onde de choc.</summary>
        Lead,

        /// <summary>Empennage spectral : la flèche traverse le décor, pas les ennemis.</summary>
        Spectral,

        /// <summary>Fût-pieu : la flèche plantée au sol blesse les ennemis qui passent dessus.</summary>
        Spike,

        /// <summary>Fût détonateur : tirer sur une de ses flèches plantées la fait éclater en flèches.</summary>
        Detonator,

        /// <summary>Fût suspendu : la flèche s'arrête en l'air ; la gâchette de l'autre main les relâche toutes.</summary>
        Suspended,

        /// <summary>Corde de transe : chaque tir parfait d'affilée renforce le suivant ; au cinquième, la flèche dorée traverse tout.</summary>
        Trance,

        /// <summary>Corde du faucon : à pleine tension, le temps ralentit un instant.</summary>
        Falcon,

        /// <summary>Talisman du sniper : plus de dégâts de loin, moins de près.</summary>
        Sniper,

        /// <summary>Talisman du pacte de verre : dégâts doublés, mais un tir raté coûte des PV.</summary>
        GlassPact,

        /// <summary>Talisman de l'ingénieur : une flèche dans sa baliste ou sa tour de givre la surcharge.</summary>
        Engineer,

        /// <summary>Talisman du duelliste : les coups de flèche au corps à corps frappent fort, portent la pointe et rendent le tir suivant parfait.</summary>
        Duelist,
    }

    /// <summary>
    /// Ce qu'est une amélioration depuis la refonte du 25 octobre (<c>Docs/Refonte_Roguelike.md</c>, étape 1) : les statistiques
    /// à part, dans les parchemins, et les effets qui changent le tir devenus des objets rares, pris une seule fois.
    /// </summary>
    public enum UpgradeKind
    {
        /// <summary>Change la façon de tirer ; une seule fois (Max Stacks 1). Coffre de boss, une carte par pause, parfois un coffre de vague.</summary>
        Object,

        /// <summary>Une statistique (Force, Adresse, Vigueur) : toujours en vente, sans limite, de plus en plus chère.</summary>
        Scroll,

        /// <summary>Gardée dans le catalogue avec ses réglages, mais plus jamais proposée.</summary>
        Retired,
    }

    /// <summary>
    /// L'emplacement d'une pièce sur la flèche (refonte roguelike, étape 2, 26 octobre, « la flèche en pièces ») : une seule pièce
    /// par emplacement ; en prendre une autre remplace l'ancienne, reprendre la même la fait monter de niveau.
    /// </summary>
    public enum ArrowSlot
    {
        /// <summary>Hors emplacement : les parchemins, les cartes retirées, les objets de la grenade.</summary>
        None,

        /// <summary>La pointe : ce que fait la flèche à l'impact.</summary>
        Tip,

        /// <summary>L'empennage : sa trajectoire.</summary>
        Fletching,

        /// <summary>Le fût : ce qu'elle fait en vol, puis une fois plantée.</summary>
        Shaft,

        /// <summary>La corde : la façon de tirer (volée, rythme, temps).</summary>
        String,

        /// <summary>Le talisman, accroché à l'arc : une règle du jeu.</summary>
        Talisman,
    }

    /// <summary>Quand l'effet d'un objet joue (refonte du 25 octobre : le geste plutôt que le dé).</summary>
    public enum UpgradeTrigger
    {
        /// <summary>À chaque tir.</summary>
        Always,

        /// <summary>Seulement aux tirs parfaits (le vert de l'anneau, une faille parfaite du katana).</summary>
        PerfectShot,
    }

    /// <summary>Une amélioration proposée en boutique (GDD, section 6).</summary>
    [Serializable]
    public class Upgrade
    {
        public string displayName = "Amélioration";

        [TextArea(2, 4)]
        public string description = "";

        [Tooltip("Objet (change le tir), parchemin (statistique, sans limite) ou retirée (jamais proposée).")]
        public UpgradeKind kind;

        [Tooltip("Objets : la partie de la flèche qu'il occupe (une pièce par emplacement : une autre la remplace, la même " +
                 "monte de niveau, jusqu'à Max Stacks). Aucun : l'objet s'ajoute sans rien remplacer.")]
        public ArrowSlot slot;

        public UpgradeRarity rarity;

        public UpgradeEffect effect;

        [Tooltip("Objets : à chaque tir, ou seulement aux tirs parfaits (multitir, écho, déluge, perçage, foudre, explosion, glace, " +
                 "chaîne d'éclairs). La carte l'indique.")]
        public UpgradeTrigger trigger;

        [Tooltip("Valeur par exemplaire, cumulée sans limite. Bonus (0,1 = +10 %) : Damage, ArrowSpeed, QuickCharge, Precision, HeadHunter, Luck. " +
                 "Points de conversion en or (0,05 = +5 points) : Loot. PV : Vitality, Vampirism. " +
                 "Chance (0,25 = 25 % ; au-delà de 100 %, l'effet devient plus fort) : Lightning, Explosive, Frost. " +
                 "Nombre moyen (0,5 = 50 % de chance d'un de plus ; 1,5 = un sûr + 50 %) : Multishot, Deluge, Echo, Piercing. " +
                 "Nombre de rebonds : ChainLightning, Ricochet. Degrés par seconde : Homing. " +
                 "Parchemins : bonus de dégâts (Strength), d'anneau (Dexterity), PV (Vigor).")]
        public float value;

        [Tooltip("Nombre maximal d'exemplaires (0 = illimité, la règle par défaut).")]
        [Min(0)]
        public int maxStacks;

        [Tooltip("Optionnel : icône affichée sur la carte de la boutique.")]
        public Sprite icon;

        public Upgrade()
        {
        }

        public Upgrade(string displayName, string description, UpgradeRarity rarity, UpgradeEffect effect, float value, int maxStacks = 0)
        {
            this.displayName = displayName;
            this.description = description;
            this.rarity = rarity;
            this.effect = effect;
            this.value = value;
            this.maxStacks = maxStacks;
        }
    }
}
