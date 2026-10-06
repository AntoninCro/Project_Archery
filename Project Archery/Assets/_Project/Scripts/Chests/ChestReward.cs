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

        /// <summary>Gardée toute la partie (amélioration, morceau d'arc), et non temporaire (bonus).</summary>
        public bool IsPermanent => Kind != ChestRewardKind.Buff;

        /// <summary>Applique la récompense au joueur. Renvoie le message à afficher.</summary>
        public string Apply(float buffDuration)
        {
            switch (Kind)
            {
                case ChestRewardKind.Upgrade:
                    if (Upgrade != null && PlayerUpgrades.Instance != null)
                        PlayerUpgrades.Instance.Add(Upgrade);
                    return Title + " !";

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
                        ? "Le dernier morceau !"
                        : $"Morceau d'arc légendaire : {legendary.Parts} / {legendary.PartsNeeded}";
            }
        }
    }

    /// <summary>Tirage des récompenses d'un coffre.</summary>
    public static class ChestRewards
    {
        static readonly List<Upgrade> s_Candidates = new List<Upgrade>();
        static readonly TemporaryBuff[] s_Buffs = { TemporaryBuff.DoubleDamage, TemporaryBuff.PerfectShots, TemporaryBuff.FastRing };

        /// <summary>
        /// Les récompenses d'un coffre, toujours dans cet ordre : deux améliorations permanentes différentes, tirées
        /// comme en boutique (raretés et Chance comprises), puis un bonus temporaire. Un morceau de l'arc légendaire
        /// peut remplacer la deuxième amélioration.
        /// </summary>
        public static List<ChestReward> Roll(float buffDuration, ChestColors colors)
        {
            var rewards = new List<ChestReward>();
            for (var i = 0; i < 2; i++)
            {
                if (TryRollUpgrade(rewards, out var upgrade))
                    rewards.Add(upgrade);
            }

            var legendaryBow = LegendaryBow.Instance;
            if (legendaryBow != null && legendaryBow.RollPartDrop())
            {
                var part = new ChestReward
                {
                    Kind = ChestRewardKind.BowPart,
                    Title = "Morceau d'arc légendaire",
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

        // Comme en boutique : la rareté d'abord (chance comprise), puis une amélioration de cette rareté.
        static bool TryRollUpgrade(List<ChestReward> taken, out ChestReward reward)
        {
            reward = default;
            var catalog = ShopManager.Instance != null ? ShopManager.Instance.Catalog : null;
            var owned = PlayerUpgrades.Instance;
            if (catalog == null || catalog.upgrades.Count == 0)
                return false;

            var waves = WaveManager.Instance;
            var wave = waves != null ? Mathf.Max(1, waves.WaveNumber) : 1;
            var rarity = catalog.RollRarity(wave, owned != null ? owned.LuckMultiplier : 1f);
            if (!PickUpgrade(catalog, rarity, owned, taken, true, out var upgrade) &&
                !PickUpgrade(catalog, rarity, owned, taken, false, out upgrade))
                return false;

            reward = new ChestReward
            {
                Kind = ChestRewardKind.Upgrade,
                Upgrade = upgrade,
                Title = upgrade.displayName,
                Subtitle = ShopCatalog.NameOf(upgrade.rarity),
                Color = catalog.ColorOf(upgrade.rarity),
            };
            return true;
        }

        static bool PickUpgrade(ShopCatalog catalog, UpgradeRarity rarity, PlayerUpgrades owned, List<ChestReward> taken,
                                bool sameRarity, out Upgrade upgrade)
        {
            s_Candidates.Clear();
            foreach (var candidate in catalog.upgrades)
            {
                if (candidate == null || (sameRarity && candidate.rarity != rarity) || (owned != null && !owned.CanTake(candidate)))
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
