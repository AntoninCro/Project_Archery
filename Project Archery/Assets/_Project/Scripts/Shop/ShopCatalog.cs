using System;
using System.Collections.Generic;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Shop
{
    /// <summary>Chances (en %) de chaque rareté, à partir d'une vague (GDD, section 6.1).</summary>
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
    /// Contenu et prix de la boutique (GDD, sections 6 et 7) : améliorations, raretés, prix.
    /// </summary>
    /// <remarks>
    /// Depuis la refonte du 25 octobre (<c>Docs/Refonte_Roguelike.md</c>, étape 1), chaque amélioration a un genre
    /// (<see cref="UpgradeKind"/>) : la boutique propose les parchemins, toujours en vente, et sur une autre ligne des objets,
    /// chers, jusqu'à trois par pause. Le catalogue a été converti par <c>Editor/RoguelikeStep1Baker</c> ; le menu
    /// « Remplir avec le GDD », qui remettait l'ancienne liste sans genres, est retiré. Plus de relance, d'arcs ni de
    /// réparations (25 octobre au soir) : leurs réglages ont disparu avec eux.
    /// </remarks>
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "Archery/Shop Catalog")]
    public class ShopCatalog : ScriptableObject
    {
        [Header("Améliorations")]
        public List<Upgrade> upgrades = new List<Upgrade>();

        [Tooltip("Chances des raretés des objets (boutique, coffres). Une seule ligne : les mêmes chances toute la partie. " +
                 "On peut ajouter des lignes pour les faire changer à partir d'une vague.")]
        public List<RarityOdds> rarityOdds = new List<RarityOdds>
        {
            new RarityOdds(1, 20f, 60f, 20f),
        };

        [Tooltip("Part minimale des cartes communes, même avec beaucoup d'améliorations Chance (0,4 = 40 %).")]
        [Range(0f, 1f)]
        public float minCommonShare = 0.4f;

        [Tooltip("Objets proposés à chaque pause, sur la ligne du bas (autant de cartes que le panneau en a). " +
                 "Les parchemins, eux, sont toujours tous proposés, sur la ligne du haut.")]
        [Min(0)]
        public int objectOffers = 3;

        [Tooltip("Objets qu'on peut acheter par pause.")]
        [Min(1)]
        public int objectsPerShop = 3;

        [Header("Prix des objets")]
        [Tooltip("Chers exprès (25 octobre au soir) : améliorer ses parchemins doit rapporter plus que d'acheter tous les objets au hasard.")]
        public int commonPrice = 120;
        public int rarePrice = 160;
        public int legendaryPrice = 220;

        [Tooltip("Chaque objet acheté en boutique fait monter le prix des suivants, en se cumulant (0,15 = +15 % par objet : " +
                 "×2 après 5). Ceux des coffres ne comptent pas.")]
        [Min(0f)]
        public float objectPriceIncreasePerPurchase = 0.15f;

        [Tooltip("Hausse des prix à chaque vague, en se cumulant (0,24 = +24 % par vague : ×2,4 après la vague 5). " +
                 "Elle touche les objets et les parchemins.")]
        [Min(0f)]
        public float priceGrowthPerWave = 0.24f;

        [Header("Prix des parchemins")]
        [Tooltip("Prix d'un parchemin dans la boutique qui suit la vague 1, avant la hausse par achat.")]
        public int scrollPrice = 25;

        [Tooltip("Chaque parchemin acheté en boutique fait monter le prix de tous les suivants, en se cumulant " +
                 "(0,05 = +5 % par achat : ×1,6 après 10 achats, ×2,7 après 20). Ceux des coffres ne comptent pas.")]
        [Min(0f)]
        public float priceIncreasePerPurchase = 0.05f;

        [Tooltip("En plus, chaque parchemin acheté en boutique fait monter son propre prix, en se cumulant (0,1 = +10 % par " +
                 "achat du même parchemin). Ajouté le 26 octobre : chaque parchemin a son prix, qui monte quand on le prend " +
                 "souvent ; les autres restent abordables (et donnent de la vie, comme dans Dead Cells). 0 : un seul prix pour tous.")]
        [Min(0f)]
        public float ownScrollPriceIncrease = 0.1f;

        [Tooltip("En mode infini, tous les prix de la boutique sont multipliés par cette valeur à chaque vague, en se cumulant (1,3 = +30 %).")]
        [Min(1f)]
        public float endlessPriceGrowth = 1.3f;

        [Header("Couleurs")]
        public Color commonColor = new Color(0.9f, 0.9f, 0.9f);
        public Color rareColor = new Color(0.35f, 0.6f, 1f);
        public Color legendaryColor = new Color(1f, 0.78f, 0.2f);

        [Tooltip("Couleur des parchemins (cartes et orbes des coffres).")]
        public Color scrollColor = new Color(0.95f, 0.82f, 0.58f);

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

        /// <summary>La couleur des parchemins, ou celle de la rareté d'un objet.</summary>
        public Color ColorOf(Upgrade upgrade) =>
            upgrade == null ? commonColor : upgrade.kind == UpgradeKind.Scroll ? scrollColor : ColorOf(upgrade.rarity);

        /// <summary>Le nom d'un emplacement de la flèche (« Pointe »), vide hors emplacement.</summary>
        public static string NameOf(ArrowSlot slot) => slot switch
        {
            ArrowSlot.Tip => "Pointe",
            ArrowSlot.Fletching => "Empennage",
            ArrowSlot.Shaft => "Fût",
            ArrowSlot.String => "Corde",
            ArrowSlot.Talisman => "Talisman",
            _ => "",
        };

        /// <summary>Un niveau en chiffres romains (« II »), pour la liste des pièces.</summary>
        public static string LevelName(int level) => level switch
        {
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => level.ToString(),
        };

        /// <summary>
        /// Sous-titre d'une carte ou d'une orbe : « Parchemin · niveau 3 », ou l'emplacement d'une pièce et sa rareté
        /// (« Pointe · Rare »), son niveau si elle en monte (« Pointe · niveau 2 »), et « tir parfait » si elle ne joue qu'aux
        /// tirs parfaits. <paramref name="level"/> : le niveau qu'elle atteindrait.
        /// </summary>
        public static string SubtitleOf(Upgrade upgrade, int level)
        {
            if (upgrade.kind == UpgradeKind.Scroll)
                return level > 1 ? $"Parchemin · niveau {level}" : "Parchemin";

            var text = upgrade.slot != ArrowSlot.None ? NameOf(upgrade.slot) + " · " : "";
            text += level > 1 ? $"niveau {level}" : NameOf(upgrade.rarity);
            return upgrade.trigger == UpgradeTrigger.PerfectShot ? text + " · tir parfait" : text;
        }

        /// <summary>
        /// La ligne qui prévient qu'une pièce irait dans l'atelier, son emplacement étant déjà pris (vide sinon),
        /// en couleur, pour un texte TMP. Depuis le 26 octobre, rien n'est remplacé : on choisit dans l'atelier.
        /// </summary>
        public static string StorageLine(Upgrade occupant) =>
            occupant == null ? "" : $"<color=#9FD3FF>Rangée dans l'atelier (équipée : « {occupant.displayName} »)</color>";

        /// <summary>« Duo possible : Trou noir » (vide sans duo), en couleur, pour un texte TMP.</summary>
        public static string DuoLine(ArrowDuos.Duo duo) => duo == null ? "" : $"<color=#FFD54F>Duo possible : {duo.Name}</color>";

        /// <summary>Les lignes non vides, l'une sous l'autre.</summary>
        public static string Lines(string first, string second, string third = "")
        {
            var text = first;
            foreach (var line in new[] { second, third })
            {
                if (!string.IsNullOrEmpty(line))
                    text = string.IsNullOrEmpty(text) ? line : text + "\n" + line;
            }

            return text;
        }

        /// <summary>
        /// Prix d'un objet dans la boutique qui suit cette vague, arrondi à 5.
        /// <paramref name="purchases"/> : nombre d'objets déjà achetés en boutique pendant la partie.
        /// </summary>
        public int ObjectPriceOf(UpgradeRarity rarity, int wave, int wavesToWin, int purchases)
        {
            var basePrice = rarity switch
            {
                UpgradeRarity.Rare => rarePrice,
                UpgradeRarity.Legendary => legendaryPrice,
                _ => commonPrice,
            };

            var purchaseFactor = Mathf.Pow(1f + objectPriceIncreasePerPurchase, Mathf.Max(0, purchases));
            return RoundPrice(basePrice * WaveFactor(wave) * purchaseFactor * EndlessFactor(wave, wavesToWin));
        }

        /// <summary>
        /// Prix d'un parchemin dans la boutique qui suit cette vague, arrondi à 5.
        /// <paramref name="purchases"/> : nombre de parchemins déjà achetés en boutique pendant la partie, tous confondus ;
        /// <paramref name="ownPurchases"/> : combien de celui-ci (26 octobre).
        /// </summary>
        public int ScrollPriceOf(int wave, int wavesToWin, int purchases, int ownPurchases)
        {
            var purchaseFactor = Mathf.Pow(1f + priceIncreasePerPurchase, Mathf.Max(0, purchases)) *
                                 Mathf.Pow(1f + ownScrollPriceIncrease, Mathf.Max(0, ownPurchases));
            return RoundPrice(scrollPrice * WaveFactor(wave) * purchaseFactor * EndlessFactor(wave, wavesToWin));
        }

        /// <summary>Hausse des prix due aux vagues : +10 % par vague, en se cumulant.</summary>
        public float WaveFactor(int wave) => Mathf.Pow(1f + priceGrowthPerWave, Mathf.Max(0, wave - 1));

        /// <summary>Hausse des prix du mode infini : ×1,2 par vague au-delà de la dernière, en se cumulant.</summary>
        public float EndlessFactor(int wave, int wavesToWin) =>
            wavesToWin > 0 && wave > wavesToWin ? Mathf.Pow(endlessPriceGrowth, wave - wavesToWin) : 1f;

        /// <summary>Arrondit un prix à 5 près (5 au minimum).</summary>
        public static int RoundPrice(float price) => Mathf.Max(5, Mathf.RoundToInt(price / 5f) * 5);

        /// <summary>
        /// Tire une rareté au hasard selon les chances de cette vague.
        /// <paramref name="luck"/> multiplie le poids des rares et des légendaires face aux communes,
        /// mais les communes gardent au moins <see cref="minCommonShare"/>.
        /// </summary>
        public UpgradeRarity RollRarity(int wave, float luck = 1f)
        {
            var odds = OddsFor(wave);
            var common = Mathf.Max(0f, odds.common);
            var rare = Mathf.Max(0f, odds.rare) * Mathf.Max(0f, luck);
            var legendary = Mathf.Max(0f, odds.legendary) * Mathf.Max(0f, luck);

            // Plafond de la part des rares et des légendaires (jamais sous leur part de départ).
            var baseTotal = common + Mathf.Max(0f, odds.rare) + Mathf.Max(0f, odds.legendary);
            var baseShare = baseTotal > 0f ? (baseTotal - common) / baseTotal : 0f;
            var maxShare = Mathf.Max(baseShare, 1f - minCommonShare);
            var lucky = rare + legendary;
            if (common > 0f && lucky > 0f && maxShare < 0.999f && lucky / (common + lucky) > maxShare)
            {
                var scale = common * maxShare / (1f - maxShare) / lucky;
                rare *= scale;
                legendary *= scale;
            }

            var total = common + rare + legendary;
            if (total <= 0f)
                return UpgradeRarity.Common;

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
    }
}
