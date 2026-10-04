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
    }

    /// <summary>Une amélioration proposée en boutique (GDD, section 6).</summary>
    [Serializable]
    public class Upgrade
    {
        public string displayName = "Amélioration";

        [TextArea(2, 4)]
        public string description = "";

        public UpgradeRarity rarity;

        public UpgradeEffect effect;

        [Tooltip("Valeur par exemplaire, cumulée sans limite. Bonus (0,1 = +10 %) : Damage, ArrowSpeed, QuickCharge, Precision, HeadHunter, Luck. " +
                 "Points de conversion en or (0,05 = +5 points) : Loot. PV : Vitality, Vampirism. " +
                 "Chance (0,25 = 25 % ; au-delà de 100 %, l'effet devient plus fort) : Lightning, Explosive, Frost. " +
                 "Nombre moyen (0,5 = 50 % de chance d'un de plus ; 1,5 = un sûr + 50 %) : Multishot, Deluge, Echo, Piercing. " +
                 "Nombre de rebonds : ChainLightning, Ricochet. Degrés par seconde : Homing.")]
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
