using System;
using System.Collections.Generic;
using Archery.BowClasses;
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

        [Tooltip("Épique (8 novembre au soir), entre rare et légendaire.")]
        public float epic;

        // Sans épique (les anciens bakers de la boutique).
        public RarityOdds(int fromWave, float common, float rare, float legendary) : this(fromWave, common, rare, 0f, legendary)
        {
        }

        public RarityOdds(int fromWave, float common, float rare, float epic, float legendary)
        {
            this.fromWave = fromWave;
            this.common = common;
            this.rare = rare;
            this.epic = epic;
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
        // 50 / 35 / 15 depuis le 27 octobre au soir (choix du joueur) : une légendaire précise sortait aussi souvent qu'une commune
        // précise (20 / 60 / 20 avant).
        // 45 / 30 / 17 / 8 le 8 novembre au soir, avec l'épique ; puis 60 / 24 / 11 / 5 le même soir, après la partie de 15:40
        // (moins de rares, d'épiques et de légendaires ; Editor/RaretesBaker).
        public List<RarityOdds> rarityOdds = new List<RarityOdds>
        {
            new RarityOdds(1, 60f, 24f, 11f, 5f),
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

        // Les prix, les quantités et leurs hausses ne servent plus depuis le 8 novembre (le joueur : l'or est retiré, la pause
        // devient un choix, ShopManager). Gardés pour les anciens bakers de la boutique (Editor/RoguelikeStep1Baker), qui les lisent.
        [Header("Prix des objets (plus utilisés depuis le 8 novembre)")]
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

        [Header("Prix des parchemins (plus utilisés depuis le 8 novembre)")]
        [Tooltip("Prix d'un parchemin dans la boutique qui suit la vague 1, avant la hausse par achat.")]
        public int scrollPrice = 25;

        [Tooltip("Chaque parchemin acheté en boutique fait monter le prix de tous les suivants, en se cumulant " +
                 "(0,05 = +5 % par achat : ×1,6 après 10 achats, ×2,7 après 20). Ceux des coffres ne comptent pas.")]
        [Min(0f)]
        public float priceIncreasePerPurchase = 0.05f;

        [Tooltip("En plus, chaque parchemin acheté en boutique fait monter son propre prix, en se cumulant (0,1 = +10 % par " +
                 "achat du même parchemin). Ajouté le 26 octobre : chaque parchemin a son prix, qui monte quand on le prend " +
                 "souvent ; les autres restent abordables. 0 : un seul prix pour tous.")]
        [Min(0f)]
        public float ownScrollPriceIncrease = 0.1f;

        [Tooltip("En mode infini, tous les prix de la boutique sont multipliés par cette valeur à chaque vague, en se cumulant (1,3 = +30 %).")]
        [Min(1f)]
        public float endlessPriceGrowth = 1.3f;

        [Header("Couleurs")]
        public Color commonColor = new Color(0.9f, 0.9f, 0.9f);
        public Color rareColor = new Color(0.35f, 0.6f, 1f);
        public Color legendaryColor = new Color(1f, 0.78f, 0.2f);

        [Tooltip("Épique (8 novembre au soir) : violet.")]
        public Color epicColor = new Color(0.72f, 0.42f, 1f);

        [Tooltip("Couleur des parchemins (cartes et orbes des coffres).")]
        public Color scrollColor = new Color(0.95f, 0.82f, 0.58f);

        public Color ColorOf(UpgradeRarity rarity) => rarity switch
        {
            UpgradeRarity.Rare => rareColor,
            UpgradeRarity.Epic => epicColor,
            UpgradeRarity.Legendary => legendaryColor,
            _ => commonColor,
        };

        public static string NameOf(UpgradeRarity rarity) => rarity switch
        {
            UpgradeRarity.Rare => "Rare",
            UpgradeRarity.Epic => "Épique",
            UpgradeRarity.Legendary => "Légendaire",
            _ => "Commune",
        };

        // ---- Les quatre raretés, partout (8 novembre au soir, le joueur) : les objets, les tourelles (1 plan commun, 2 rare, 3
        // épique, 4 légendaire) et les parchemins (leur effet ×1, ×1,5, ×2, ×3 ; les valeurs de base divisées par deux).

        /// <summary>Le rang d'une rareté : 0 commune, 1 rare, 2 épique, 3 légendaire.</summary>
        public static int Rank(UpgradeRarity rarity) => rarity switch
        {
            UpgradeRarity.Rare => 1,
            UpgradeRarity.Epic => 2,
            UpgradeRarity.Legendary => 3,
            _ => 0,
        };

        public static UpgradeRarity FromRank(int rank) => Mathf.Clamp(rank, 0, 3) switch
        {
            1 => UpgradeRarity.Rare,
            2 => UpgradeRarity.Epic,
            3 => UpgradeRarity.Legendary,
            _ => UpgradeRarity.Common,
        };

        /// <summary>Plus rare de tant de crans (le gong appelé tôt : la pause suivante, d'un cran), légendaire au plus.</summary>
        public static UpgradeRarity Boosted(UpgradeRarity rarity, int steps) => FromRank(Rank(rarity) + Mathf.Max(0, steps));

        /// <summary>La rareté d'un lot de plans : 1 plan commun, 2 rare, 3 épique, 4 légendaire.</summary>
        public static UpgradeRarity RarityOfPlans(int plans) => FromRank(plans - 1);

        /// <summary>L'effet d'un parchemin selon sa rareté : ×1, ×1,5, ×2, ×3.</summary>
        public static float ScrollScale(UpgradeRarity rarity) => rarity switch
        {
            UpgradeRarity.Rare => 1.5f,
            UpgradeRarity.Epic => 2f,
            UpgradeRarity.Legendary => 3f,
            _ => 1f,
        };

        /// <summary>
        /// Le texte d'un parchemin à cette échelle (sa rareté), prêt pour un texte TMP (« ➶ » devient le sprite des dégâts : la police
        /// Cinzel n'a pas ce caractère, il s'affichait en carré ; 8 novembre au soir), d'après sa valeur : la Force, les dégâts et l'anneau (la moitié) ;
        /// l'Adresse, la bande verte et les tirs parfaits ; la Vigueur, les PV et la portée (la même part des PV de base).
        /// </summary>
        public static string ScrollText(Upgrade upgrade, float scale)
        {
            if (upgrade == null)
                return "";
            var value = upgrade.value * Mathf.Max(0f, scale);
            string Percent(float share) => (share * 100f).ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("fr-FR")) + " %";
            switch (upgrade.effect)
            {
                case UpgradeEffect.Strength:
                    return CardText.Symbols($"+{Percent(value)} \u27B6, anneau {Percent(value * 0.5f)} plus rapide");
                case UpgradeEffect.Dexterity:
                    return CardText.Symbols($"Bande verte {Percent(value)} plus large, tirs parfaits +{Percent(value)} \u27B6");
                case UpgradeEffect.Vigor:
                    var player = PlayerUpgrades.Instance;
                    var baseHealth = player != null && player.BaseMaxHealth > 0f ? player.BaseMaxHealth : 100f;
                    var points = value.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
                    return $"+{points} PV max, flèches {Percent(value / baseHealth)} plus loin";
                default:
                    return upgrade.DescriptionAt(1);
            }
        }

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

        /// <summary>
        /// « Duo possible avec « Fût aimant » : Trou noir » (vide sans duo), en couleur, pour un texte TMP. Depuis le 26 octobre
        /// (demandé par le joueur), la ligne nomme la pièce déjà possédée qui forme le duo avec celle-ci.
        /// </summary>
        public static string DuoLine(ArrowDuos.Duo duo, Upgrade partner)
        {
            if (duo == null)
                return "";
            return partner != null
                ? $"<color=#FFD54F>Duo possible avec « {partner.displayName} » : {duo.Name}</color>"
                : $"<color=#FFD54F>Duo possible : {duo.Name}</color>";
        }

        /// <summary>La ligne du duo que <paramref name="piece"/> formerait avec une pièce de <paramref name="owned"/> (vide sinon).</summary>
        public static string DuoLine(Upgrade piece, PlayerUpgrades owned)
        {
            var duo = ArrowDuos.Completes(piece, owned);
            return DuoLine(duo, ArrowDuos.PartnerOf(duo, piece, owned));
        }

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

        static readonly List<Upgrade> s_Candidates = new List<Upgrade>();
        static readonly List<ArrowSlot> s_Slots = new List<ArrowSlot>();

        /// <summary>
        /// Tire un objet (26 octobre au soir, demandé par le joueur : l'emplacement d'abord). Parmi les emplacements de la flèche
        /// qui ont encore une pièce à prendre, un au hasard, chacun avec la même chance, quel que soit son nombre de pièces (une
        /// pointe sortait une fois sur deux) ; de préférence un emplacement pas encore montré (<paramref name="shownSlots"/> : les
        /// 3 objets d'une boutique, les 2 du coffre de boss) ; puis la rareté (chance comprise), et une pièce de cette rareté dans
        /// cet emplacement (une autre rareté s'il n'en a pas). <paramref name="excluded"/> : les objets déjà proposés. Null s'il ne
        /// reste rien.
        /// </summary>
        public Upgrade PickObject(int wave, PlayerUpgrades owned, Func<Upgrade, bool> excluded, ICollection<ArrowSlot> shownSlots,
                                  int rarityBoost = 0)
        {
            s_Slots.Clear();
            var freshSlot = false;
            foreach (var upgrade in upgrades)
            {
                if (!IsCandidate(upgrade, owned, excluded) || s_Slots.Contains(upgrade.slot))
                    continue;
                s_Slots.Add(upgrade.slot);
                freshSlot |= shownSlots == null || !shownSlots.Contains(upgrade.slot);
            }

            if (freshSlot && shownSlots != null)
                s_Slots.RemoveAll(slot => shownSlots.Contains(slot));
            if (s_Slots.Count == 0)
                return null;

            var chosen = s_Slots[UnityEngine.Random.Range(0, s_Slots.Count)];
            var rarity = RollRarity(wave, owned != null ? owned.LuckMultiplier : 1f, rarityBoost);
            s_Candidates.Clear();
            foreach (var upgrade in upgrades)
            {
                if (IsCandidate(upgrade, owned, excluded) && upgrade.slot == chosen && upgrade.rarity == rarity)
                    s_Candidates.Add(upgrade);
            }

            if (s_Candidates.Count == 0)
            {
                foreach (var upgrade in upgrades)
                {
                    if (IsCandidate(upgrade, owned, excluded) && upgrade.slot == chosen)
                        s_Candidates.Add(upgrade);
                }
            }

            return s_Candidates.Count > 0 ? s_Candidates[UnityEngine.Random.Range(0, s_Candidates.Count)] : null;
        }

        static bool IsCandidate(Upgrade upgrade, PlayerUpgrades owned, Func<Upgrade, bool> excluded) =>
            upgrade != null && upgrade.kind == UpgradeKind.Object && (owned == null || owned.CanTake(upgrade)) &&
            (excluded == null || !excluded(upgrade)) && FitsWeapon(upgrade);

        /// <summary>
        /// La pièce agit avec l'arme choisie (27 octobre, demandé par le joueur) : la corde fantôme (la corde à vide) n'existe qu'avec
        /// un arc ; avec les Akimbow ou le katana, la boutique et les coffres ne la proposent pas. Les raccourcis de test (R, Tab, F)
        /// la donnent quand même. La corde de siège marche avec toutes les armes depuis le 27 octobre (les arbalètes se chargent au
        /// bout de leur anneau, les failles du katana mûrissent).
        /// </summary>
        public static bool FitsWeapon(Upgrade upgrade)
        {
            if (upgrade == null || upgrade.effect != UpgradeEffect.Ghost)
                return true;

            var loadout = BowLoadout.Instance;
            var weapon = loadout != null ? loadout.Selected : null;
            return weapon == null || (!weapon.akimbo && !weapon.katana);
        }

        /// <summary>
        /// Tire une rareté au hasard selon les chances de cette vague.
        /// <paramref name="luck"/> multiplie le poids des rares et des légendaires face aux communes,
        /// mais les communes gardent au moins <see cref="minCommonShare"/>.
        /// </summary>
        public UpgradeRarity RollRarity(int wave, float luck = 1f, int boost = 0)
        {
            var odds = OddsFor(wave);
            var common = Mathf.Max(0f, odds.common);
            var rare = Mathf.Max(0f, odds.rare) * Mathf.Max(0f, luck);
            var epic = Mathf.Max(0f, odds.epic) * Mathf.Max(0f, luck);
            var legendary = Mathf.Max(0f, odds.legendary) * Mathf.Max(0f, luck);

            // Plafond de la part des rares, des épiques et des légendaires (jamais sous leur part de départ).
            var baseTotal = common + Mathf.Max(0f, odds.rare) + Mathf.Max(0f, odds.epic) + Mathf.Max(0f, odds.legendary);
            var baseShare = baseTotal > 0f ? (baseTotal - common) / baseTotal : 0f;
            var maxShare = Mathf.Max(baseShare, 1f - minCommonShare);
            var lucky = rare + epic + legendary;
            if (common > 0f && lucky > 0f && maxShare < 0.999f && lucky / (common + lucky) > maxShare)
            {
                var scale = common * maxShare / (1f - maxShare) / lucky;
                rare *= scale;
                epic *= scale;
                legendary *= scale;
            }

            var total = common + rare + epic + legendary;
            if (total <= 0f)
                return Boosted(UpgradeRarity.Common, boost);

            var roll = UnityEngine.Random.value * total;
            var rarity = roll < legendary ? UpgradeRarity.Legendary
                : roll < legendary + epic ? UpgradeRarity.Epic
                : roll < legendary + epic + rare ? UpgradeRarity.Rare
                : UpgradeRarity.Common;
            return Boosted(rarity, boost);
        }

        // La ligne dont la vague de départ est la plus grande sans dépasser la vague jouée.
        RarityOdds OddsFor(int wave)
        {
            var found = false;
            var best = new RarityOdds(0, 100f, 0f, 0f, 0f);
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
