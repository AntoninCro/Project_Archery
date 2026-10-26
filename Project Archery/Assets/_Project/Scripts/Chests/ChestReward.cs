using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using Archery.Waves;
using UnityEngine;

namespace Archery.Chests
{
    public enum ChestRewardKind
    {
        /// <summary>Une amélioration pour toute la partie, tirée comme en boutique.</summary>
        Upgrade,

        /// <summary>Un bonus temporaire (dégâts doublés, tirs parfaits, anneau rapide).</summary>
        Buff,

        /// <summary>Un morceau de l'arc légendaire.</summary>
        BowPart,
    }

    /// <summary>Ce que donne une orbe de coffre (GDD, section 13).</summary>
    public struct ChestReward
    {
        public ChestRewardKind Kind;
        public Upgrade Upgrade;
        public TemporaryBuff Buff;
        public string Title;
        public string Subtitle;
        public Color Color;

        /// <summary>Icône de la boutique qui dit la même chose (vide pour le morceau d'arc : son orbe garde sa boule).</summary>
        public Sprite Icon;

        /// <summary>Gardée toute la partie (amélioration, morceau d'arc), et non temporaire (bonus).</summary>
        public bool IsPermanent => Kind != ChestRewardKind.Buff;

        /// <summary>
        /// Au-dessus de l'orbe : l'emplacement d'une pièce (« POINTE »), « OBJET » hors emplacement, « PARCHEMIN »,
        /// « TEMPORAIRE », ou « PERMANENT » pour le fragment d'arc.
        /// </summary>
        public string KindLabel => Kind switch
        {
            ChestRewardKind.Buff => "TEMPORAIRE",
            ChestRewardKind.Upgrade when Upgrade != null && Upgrade.kind == UpgradeKind.Scroll => "PARCHEMIN",
            ChestRewardKind.Upgrade when Upgrade != null && Upgrade.slot != ArrowSlot.None => ShopCatalog.NameOf(Upgrade.slot).ToUpperInvariant(),
            ChestRewardKind.Upgrade => "OBJET",
            _ => "PERMANENT",
        };

        /// <summary>Applique la récompense au joueur. Renvoie le message à afficher.</summary>
        public string Apply(float buffDuration)
        {
            switch (Kind)
            {
                case ChestRewardKind.Upgrade:
                    var upgrades = PlayerUpgrades.Instance;
                    if (Upgrade == null || upgrades == null)
                        return Title + " !";

                    // Une pièce prise s'équipe si son emplacement est libre, sinon va dans l'atelier ; ou elle monte de niveau.
                    var occupant = upgrades.Occupant(Upgrade);
                    upgrades.Add(Upgrade);
                    return ShopManager.PurchaseMessage(Upgrade, occupant, upgrades.Level(Upgrade.effect));

                case ChestRewardKind.Buff:
                    if (PlayerUpgrades.Instance != null)
                        PlayerUpgrades.Instance.AddBuff(Buff, buffDuration);
                    return $"{Title} pendant {Mathf.RoundToInt(buffDuration)} s !";

                default:
                    var legendary = LegendaryBow.Instance;
                    if (legendary == null)
                        return Title;
                    legendary.AddPart();
                    return legendary.IsAssembled
                        ? "Le dernier fragment !"
                        : $"Fragment d'arc légendaire : {legendary.Parts} / {legendary.PartsNeeded}";
            }
        }
    }

    /// <summary>Tirage des récompenses d'un coffre.</summary>
    public static class ChestRewards
    {
        static readonly List<Upgrade> s_Candidates = new List<Upgrade>();
        static readonly TemporaryBuff[] s_Buffs = { TemporaryBuff.DoubleDamage, TemporaryBuff.PerfectShots, TemporaryBuff.FastRing };

        /// <summary>
        /// Les récompenses d'un coffre de vague, toujours dans cet ordre (refonte du 25 octobre) : un objet (avec la chance
        /// <paramref name="objectChance"/>, tiré comme en boutique) ou un parchemin, un autre parchemin, puis un bonus
        /// temporaire. Un fragment de l'arc légendaire peut remplacer la deuxième orbe.
        /// </summary>
        public static List<ChestReward> Roll(float buffDuration, ChestColors colors, float objectChance)
        {
            var rewards = new List<ChestReward>();
            var wantsObject = Random.value < objectChance;
            if (TryRollUpgrade(wantsObject ? UpgradeKind.Object : UpgradeKind.Scroll, rewards, out var first) ||
                (wantsObject && TryRollUpgrade(UpgradeKind.Scroll, rewards, out first)))
                rewards.Add(first);
            if (TryRollUpgrade(UpgradeKind.Scroll, rewards, out var second))
                rewards.Add(second);

            var legendaryBow = LegendaryBow.Instance;
            if (legendaryBow != null && legendaryBow.RollPartDrop())
            {
                var part = new ChestReward
                {
                    Kind = ChestRewardKind.BowPart,
                    Title = "Fragment d'arc légendaire",
                    Subtitle = $"{legendaryBow.Parts + 1} / {legendaryBow.PartsNeeded}",
                    Color = legendaryBow.Color,
                };
                if (rewards.Count >= 2)
                    rewards[1] = part;
                else
                    rewards.Add(part);
            }

            rewards.Add(RollBuff(buffDuration, colors));
            return rewards;
        }

        /// <summary>
        /// Le coffre de boss (refonte du 25 octobre) : <paramref name="count"/> objets différents, un seul à prendre ; des
        /// parchemins quand il ne reste plus d'objet à donner.
        /// </summary>
        public static List<ChestReward> RollBoss(int count)
        {
            var rewards = new List<ChestReward>();
            for (var i = 0; i < count; i++)
            {
                if (TryRollUpgrade(UpgradeKind.Object, rewards, out var reward) || TryRollUpgrade(UpgradeKind.Scroll, rewards, out reward))
                    rewards.Add(reward);
            }

            return rewards;
        }

        // Un objet : comme en boutique, la rareté d'abord (chance comprise), puis un objet de cette rareté. Un parchemin : au hasard.
        static bool TryRollUpgrade(UpgradeKind kind, List<ChestReward> taken, out ChestReward reward)
        {
            reward = default;
            var catalog = ShopManager.Instance != null ? ShopManager.Instance.Catalog : null;
            var owned = PlayerUpgrades.Instance;
            if (catalog == null || catalog.upgrades.Count == 0)
                return false;

            Upgrade upgrade;
            if (kind == UpgradeKind.Object)
            {
                var waves = WaveManager.Instance;
                var wave = waves != null ? Mathf.Max(1, waves.WaveNumber) : 1;
                var rarity = catalog.RollRarity(wave, owned != null ? owned.LuckMultiplier : 1f);
                if (!PickUpgrade(catalog, kind, rarity, owned, taken, true, out upgrade) &&
                    !PickUpgrade(catalog, kind, rarity, owned, taken, false, out upgrade))
                    return false;
            }
            else if (!PickUpgrade(catalog, kind, UpgradeRarity.Common, owned, taken, false, out upgrade))
            {
                return false;
            }

            // Le niveau qu'elle atteindrait, et dessous, le duo qu'elle pourrait former et si elle irait dans l'atelier.
            var level = (owned != null ? owned.Level(upgrade.effect) : 0) + 1;
            var duo = ShopCatalog.DuoLine(ArrowDuos.Completes(upgrade, owned));
            var storage = ShopCatalog.StorageLine(owned != null ? owned.Occupant(upgrade) : null);
            reward = new ChestReward
            {
                Kind = ChestRewardKind.Upgrade,
                Upgrade = upgrade,
                Title = upgrade.displayName,
                Subtitle = ShopCatalog.Lines(ShopCatalog.SubtitleOf(upgrade, level), duo, storage),
                Color = catalog.ColorOf(upgrade),
                Icon = upgrade.icon,
            };
            return true;
        }

        // L'icône d'une amélioration de la boutique qui a cet effet (les bonus reprennent celle qui leur ressemble).
        static Sprite ShopIcon(UpgradeEffect effect)
        {
            var catalog = ShopManager.Instance != null ? ShopManager.Instance.Catalog : null;
            if (catalog == null)
                return null;

            foreach (var upgrade in catalog.upgrades)
            {
                if (upgrade != null && upgrade.effect == effect && upgrade.icon != null)
                    return upgrade.icon;
            }

            return null;
        }

        static bool PickUpgrade(ShopCatalog catalog, UpgradeKind kind, UpgradeRarity rarity, PlayerUpgrades owned, List<ChestReward> taken,
                                bool sameRarity, out Upgrade upgrade)
        {
            s_Candidates.Clear();
            foreach (var candidate in catalog.upgrades)
            {
                if (candidate == null || candidate.kind != kind || (sameRarity && candidate.rarity != rarity) ||
                    (owned != null && !owned.CanTake(candidate)))
                    continue;
                if (!IsTaken(taken, candidate))
                    s_Candidates.Add(candidate);
            }

            upgrade = s_Candidates.Count > 0 ? s_Candidates[Random.Range(0, s_Candidates.Count)] : null;
            return upgrade != null;
        }

        static bool IsTaken(List<ChestReward> taken, Upgrade upgrade)
        {
            foreach (var reward in taken)
            {
                if (reward.Kind == ChestRewardKind.Upgrade && reward.Upgrade == upgrade)
                    return true;
            }

            return false;
        }

        // Un des trois bonus, au hasard, dans sa couleur.
        static ChestReward RollBuff(float buffDuration, ChestColors colors)
        {
            var buff = s_Buffs[Random.Range(0, s_Buffs.Length)];
            return new ChestReward
            {
                Kind = ChestRewardKind.Buff,
                Buff = buff,
                Title = PlayerUpgrades.NameOf(buff),
                Subtitle = Mathf.RoundToInt(buffDuration) + " s",
                Color = buff switch
                {
                    TemporaryBuff.DoubleDamage => colors.damage,
                    TemporaryBuff.PerfectShots => colors.perfect,
                    _ => colors.fastRing,
                },
                // Dégâts doublés : l'épée ; tirs parfaits : la cible ; anneau rapide : l'anneau de Charge rapide.
                Icon = ShopIcon(buff switch
                {
                    TemporaryBuff.DoubleDamage => UpgradeEffect.Damage,
                    TemporaryBuff.PerfectShots => UpgradeEffect.Precision,
                    _ => UpgradeEffect.QuickCharge,
                }),
            };
        }
    }

    /// <summary>Couleurs des orbes de bonus. Les améliorations prennent la couleur de leur rareté, comme en boutique.</summary>
    [System.Serializable]
    public struct ChestColors
    {
        public Color damage;
        public Color perfect;
        public Color fastRing;

        public static ChestColors Default => new ChestColors
        {
            damage = new Color(1f, 0.35f, 0.3f),
            perfect = new Color(0.3f, 0.92f, 0.35f),
            fastRing = new Color(0.35f, 0.85f, 1f),
        };
    }
}
