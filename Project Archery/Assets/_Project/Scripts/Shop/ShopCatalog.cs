using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Shop
{
    /// <summary>Chances (en %) de chaque rareté à partir d'une vague (GDD, section 6.1).</summary>
    [Serializable]
    public struct RarityOdds
    {
        [Tooltip("Ces chances valent pour la boutique qui suit cette vague, et les suivantes jusqu'à la ligne d'après.")]
        public int fromWave;
        public float common;
        public float rare;
        public float legendary;

        public RarityOdds(int fromWave, float common, float rare, float legendary)
        {
            this.fromWave = fromWave;
            this.common = common;
            this.rare = rare;
            this.legendary = legendary;
        }
    }

    /// <summary>
    /// Contenu et prix de la boutique (GDD, sections 5, 6 et 7) : améliorations, raretés, arcs, services de la tour.
    /// </summary>
    /// <remarks>
    /// Le menu ⋮ de l'asset propose « Remplir avec le GDD » : les 19 améliorations, les chances et les prix.
    /// Les arcs se glissent à la main dans la liste « Bows ».
    /// </remarks>
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "Archery/Shop Catalog")]
    public class ShopCatalog : ScriptableObject
    {
        [Header("Améliorations")]
        public List<Upgrade> upgrades = new List<Upgrade>();

        [Tooltip("Chances des raretés selon la vague qui vient de se terminer.")]
        public List<RarityOdds> rarityOdds = new List<RarityOdds>
        {
            new RarityOdds(1, 80f, 18f, 2f),
            new RarityOdds(4, 65f, 30f, 5f),
            new RarityOdds(7, 50f, 40f, 10f),
            new RarityOdds(11, 40f, 45f, 15f),
        };

        [Tooltip("Nombre d'améliorations proposées à chaque pause.")]
        [Min(1)]
        public int upgradeOffers = 4;

        [Header("Prix des améliorations")]
        public int commonPrice = 25;
        public int rarePrice = 60;
        public int legendaryPrice = 140;

        [Tooltip("Hausse des prix à chaque vague (0,08 = +8 %).")]
        public float priceIncreasePerWave = 0.08f;

        [Tooltip("En mode infini, tous les prix de la boutique sont multipliés par cette valeur à chaque vague, en se cumulant (1,2 = +20 %).")]
        [Min(1f)]
        public float endlessPriceGrowth = 1.2f;

        [Header("Relance des offres")]
        public int rerollCost = 10;

        [Tooltip("Ajouté au prix de la relance à chaque relance ; le prix revient au départ à chaque pause.")]
        public int rerollCostIncrease = 5;

        [Header("Arcs")]
        [Tooltip("Les arcs dans l'ordre d'achat. Le premier est l'arc de départ ; la boutique propose toujours le suivant.")]
        public List<BowDefinition> bows = new List<BowDefinition>();

        [Header("Tour")]
        [Tooltip("Part des PV max rendue par une réparation.")]
        [Range(0.05f, 1f)]
        public float repairFraction = 0.25f;

        public int repairPrice = 40;
        public int rebuildPrice = 250;

        [Header("Couleurs")]
        public Color commonColor = new Color(0.9f, 0.9f, 0.9f);
        public Color rareColor = new Color(0.35f, 0.6f, 1f);
        public Color legendaryColor = new Color(1f, 0.78f, 0.2f);
        public Color bowColor = new Color(0.6f, 0.9f, 0.5f);
        public Color towerColor = new Color(0.9f, 0.68f, 0.45f);

        public Color ColorOf(UpgradeRarity rarity) => rarity switch
        {
            UpgradeRarity.Rare => rareColor,
            UpgradeRarity.Legendary => legendaryColor,
            _ => commonColor,
        };

        public static string NameOf(UpgradeRarity rarity) => rarity switch
        {
            UpgradeRarity.Rare => "Rare",
            UpgradeRarity.Legendary => "Légendaire",
            _ => "Commune",
        };

        /// <summary>Prix d'une amélioration dans la boutique qui suit cette vague, arrondi à 5.</summary>
        public int PriceOf(UpgradeRarity rarity, int wave, int wavesToWin)
        {
            var basePrice = rarity switch
            {
                UpgradeRarity.Rare => rarePrice,
                UpgradeRarity.Legendary => legendaryPrice,
                _ => commonPrice,
            };

            var price = basePrice * (1f + priceIncreasePerWave * Mathf.Max(0, wave - 1)) * EndlessFactor(wave, wavesToWin);
            return RoundPrice(price);
        }

        /// <summary>Hausse des prix du mode infini : ×1,2 par vague au-delà de la dernière, en se cumulant.</summary>
        public float EndlessFactor(int wave, int wavesToWin) =>
            wavesToWin > 0 && wave > wavesToWin ? Mathf.Pow(endlessPriceGrowth, wave - wavesToWin) : 1f;

        /// <summary>Arrondit un prix à 5 près (5 au minimum).</summary>
        public static int RoundPrice(float price) => Mathf.Max(5, Mathf.RoundToInt(price / 5f) * 5);

        /// <summary>
        /// Tire une rareté au hasard selon les chances de cette vague.
        /// <paramref name="luck"/> multiplie les chances des rares et des légendaires, au détriment des communes.
        /// </summary>
        public UpgradeRarity RollRarity(int wave, float luck = 1f)
        {
            var odds = OddsFor(wave);
            var total = odds.common + odds.rare + odds.legendary;
            if (total <= 0f)
                return UpgradeRarity.Common;

            var rare = odds.rare * Mathf.Max(0f, luck);
            var legendary = odds.legendary * Mathf.Max(0f, luck);
            if (rare + legendary > total)
            {
                var scale = total / (rare + legendary);
                rare *= scale;
                legendary *= scale;
            }

            var roll = UnityEngine.Random.value * total;
            if (roll < legendary)
                return UpgradeRarity.Legendary;
            if (roll < legendary + rare)
                return UpgradeRarity.Rare;
            return UpgradeRarity.Common;
        }

        // La ligne dont la vague de départ est la plus grande sans dépasser la vague jouée.
        RarityOdds OddsFor(int wave)
        {
            var found = false;
            var best = new RarityOdds(0, 100f, 0f, 0f);
            foreach (var odds in rarityOdds)
            {
                if (odds.fromWave <= wave && (!found || odds.fromWave >= best.fromWave))
                {
                    best = odds;
                    found = true;
                }
            }

            return found || rarityOdds.Count == 0 ? best : rarityOdds[0];
        }

        [ContextMenu("Remplir avec le GDD")]
        void FillFromGdd()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Remplir avec le GDD");
#endif
            // Aucune limite d'achat : les bonus se cumulent, et au-delà de 100 % de chance un effet devient plus fort.
            upgrades = new List<Upgrade>
            {
                new Upgrade("Dégâts", "+10 % de dégâts.", UpgradeRarity.Common, UpgradeEffect.Damage, 0.1f),
                new Upgrade("Flèches rapides", "+8 % de vitesse : les flèches vont plus loin et tombent moins.", UpgradeRarity.Common, UpgradeEffect.ArrowSpeed, 0.08f),
                new Upgrade("Charge rapide", "L'anneau va 15 % plus vite, et toutes ses bandes s'élargissent un peu.", UpgradeRarity.Common, UpgradeEffect.QuickCharge, 0.15f),
                new Upgrade("Précision", "Bande verte (tir parfait) 15 % plus large.", UpgradeRarity.Common, UpgradeEffect.Precision, 0.15f),
                new Upgrade("Vitalité", "+15 PV max.", UpgradeRarity.Common, UpgradeEffect.Vitality, 15f),
                new Upgrade("Butin", "Plus de points changés en or : +5 points (25 % → 30 %).", UpgradeRarity.Common, UpgradeEffect.Loot, 0.05f),
                new Upgrade("Chasseur de têtes", "+25 % de dégâts à la tête.", UpgradeRarity.Common, UpgradeEffect.HeadHunter, 0.25f),
                new Upgrade("Chance", "Les cartes rares et légendaires sortent 25 % plus souvent.", UpgradeRarity.Common, UpgradeEffect.Luck, 0.25f),
                new Upgrade("Multitir", "+50 % de chance de tirer une flèche en plus. Avec 2 exemplaires, une flèche en plus à chaque tir ; avec 3, 50 % de chance d'une deuxième, etc.", UpgradeRarity.Rare, UpgradeEffect.Multishot, 0.5f),
                new Upgrade("Flèche de foudre", "Chaque flèche a 20 % de chance d'appeler un éclair qui blesse la cible et la ralentit. Au-delà de 100 % : éclairs plus forts.", UpgradeRarity.Rare, UpgradeEffect.Lightning, 0.2f),
                new Upgrade("Perçage", "Chaque flèche a 25 % de chance de traverser un ennemi. Au-delà de 100 % : plusieurs ennemis traversés.", UpgradeRarity.Rare, UpgradeEffect.Piercing, 0.25f),
                new Upgrade("Vampirisme", "Chaque tir à la tête rend 2 PV.", UpgradeRarity.Rare, UpgradeEffect.Vampirism, 2f),
                new Upgrade("Tir écho", "25 % de chance que la volée se répète un instant après, à la même puissance. Au-delà de 100 % : plusieurs échos.", UpgradeRarity.Rare, UpgradeEffect.Echo, 0.25f),
                new Upgrade("Flèche de glace", "Chaque flèche a 25 % de chance de laisser au sol une zone de glace qui ralentit les ennemis. Au-delà de 100 % : glace plus forte et plus grande.", UpgradeRarity.Rare, UpgradeEffect.Frost, 0.25f),
                new Upgrade("Déluge", "En vol, chaque flèche a 50 % de chance de se diviser en deux. Avec 2 exemplaires, toujours ; avec 3, 50 % de chance d'une troisième flèche, etc.", UpgradeRarity.Legendary, UpgradeEffect.Deluge, 0.5f),
                new Upgrade("Chaîne d'éclairs", "La foudre rebondit sur 3 ennemis proches de plus. Sans Flèche de foudre, 20 % des flèches l'appellent.", UpgradeRarity.Legendary, UpgradeEffect.ChainLightning, 3f),
                new Upgrade("Flèche explosive", "Chaque flèche a 25 % de chance d'exploser et de toucher les ennemis autour. Au-delà de 100 % : explosions plus fortes et plus larges.", UpgradeRarity.Legendary, UpgradeEffect.Explosive, 0.25f),
                new Upgrade("Auto-visée", "Les flèches dévient vers l'ennemi le plus proche, s'il est à moins de 12 m devant elles. Encore : elles tournent plus vite.", UpgradeRarity.Legendary, UpgradeEffect.Homing, 30f),
                new Upgrade("Tir ricochet", "Après avoir touché un ennemi, la flèche rebondit vers un autre ennemi proche. Encore : un rebond de plus.", UpgradeRarity.Legendary, UpgradeEffect.Ricochet, 1f),
            };

            rarityOdds = new List<RarityOdds>
            {
                new RarityOdds(1, 80f, 18f, 2f),
                new RarityOdds(4, 65f, 30f, 5f),
                new RarityOdds(7, 50f, 40f, 10f),
                new RarityOdds(11, 40f, 45f, 15f),
            };

            upgradeOffers = 4;
            commonPrice = 25;
            rarePrice = 60;
            legendaryPrice = 140;
            priceIncreasePerWave = 0.08f;
            endlessPriceGrowth = 1.2f;
            rerollCost = 10;
            rerollCostIncrease = 5;
            repairFraction = 0.25f;
            repairPrice = 40;
            rebuildPrice = 250;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
