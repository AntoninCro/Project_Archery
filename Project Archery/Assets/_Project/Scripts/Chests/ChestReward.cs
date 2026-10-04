using System.Collections.Generic;
using Archery.Player;
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

        /// <summary>Tous les PV du joueur.</summary>
        Heal,

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

                case ChestRewardKind.BowPart:
                    var legendary = LegendaryBow.Instance;
                    if (legendary == null)
                        return Title;
                    legendary.AddPart();
                    return legendary.IsAssembled
                        ? "Le dernier morceau !"
                        : $"Morceau d'arc légendaire : {legendary.Parts} / {legendary.PartsNeeded}";

                default:
                    var player = PlayerHealth.Instance;
                    if (player != null && player.IsAlive)
                        player.Health.Heal(player.Health.Max);
                    return "PV au maximum !";
            }
        }
    }

    /// <summary>Tirage des récompenses d'un coffre.</summary>
    public static class ChestRewards
    {
        static readonly List<Upgrade> s_Candidates = new List<Upgrade>();
        static readonly List<ChestReward> s_Bonuses = new List<ChestReward>();

        /// <summary>
        /// Tire <paramref name="count"/> récompenses différentes : au moins une amélioration et un bonus,
        /// les autres au hasard, dans le désordre.
        /// </summary>
        public static List<ChestReward> Roll(int count, float buffDuration, ChestColors colors)
        {
            var rewards = new List<ChestReward>();
            if (TryRollUpgrade(rewards, out var upgrade))
                rewards.Add(upgrade);
            if (TryRollBonus(rewards, buffDuration, colors, out var bonus))
                rewards.Add(bonus);

            for (var guard = 0; rewards.Count < count && guard < 20; guard++)
            {
                var wantsUpgrade = Random.value < 0.5f;
                if (wantsUpgrade && TryRollUpgrade(rewards, out var extraUpgrade))
                    rewards.Add(extraUpgrade);
                else if (TryRollBonus(rewards, buffDuration, colors, out var extraBonus))
                    rewards.Add(extraBonus);
                else if (TryRollUpgrade(rewards, out extraUpgrade))
                    rewards.Add(extraUpgrade);
            }

            // Arc légendaire : parfois, un morceau remplace la dernière orbe.
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
                if (rewards.Count >= count && rewards.Count > 0)
                    rewards[rewards.Count - 1] = part;
                else
                    rewards.Add(part);
            }

            // Mélange : l'amélioration n'est pas toujours à gauche.
            for (var i = rewards.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (rewards[i], rewards[j]) = (rewards[j], rewards[i]);
            }

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

        // Un bonus pas encore proposé. Le soin n'est proposé que si le joueur a perdu des PV.
        static bool TryRollBonus(List<ChestReward> taken, float buffDuration, ChestColors colors, out ChestReward reward)
        {
            var subtitle = Mathf.RoundToInt(buffDuration) + " s";
            s_Bonuses.Clear();
            AddBonus(taken, new ChestReward { Kind = ChestRewardKind.Buff, Buff = TemporaryBuff.DoubleDamage, Subtitle = subtitle, Color = colors.damage });
            AddBonus(taken, new ChestReward { Kind = ChestRewardKind.Buff, Buff = TemporaryBuff.PerfectShots, Subtitle = subtitle, Color = colors.perfect });
            AddBonus(taken, new ChestReward { Kind = ChestRewardKind.Buff, Buff = TemporaryBuff.FastRing, Subtitle = subtitle, Color = colors.fastRing });

            var player = PlayerHealth.Instance;
            if (player != null && player.Health.Current < player.Health.Max - 0.5f)
                AddBonus(taken, new ChestReward { Kind = ChestRewardKind.Heal, Title = "Soin", Subtitle = "PV au maximum", Color = colors.heal });

            reward = s_Bonuses.Count > 0 ? s_Bonuses[Random.Range(0, s_Bonuses.Count)] : default;
            return s_Bonuses.Count > 0;
        }

        static void AddBonus(List<ChestReward> taken, ChestReward bonus)
        {
            foreach (var reward in taken)
            {
                if (reward.Kind == bonus.Kind && (bonus.Kind != ChestRewardKind.Buff || reward.Buff == bonus.Buff))
                    return;
            }

            if (bonus.Kind == ChestRewardKind.Buff)
                bonus.Title = PlayerUpgrades.NameOf(bonus.Buff);
            s_Bonuses.Add(bonus);
        }
    }

    /// <summary>Couleurs des orbes de bonus.</summary>
    [System.Serializable]
    public struct ChestColors
    {
        public Color damage;
        public Color perfect;
        public Color fastRing;
        public Color heal;

        public static ChestColors Default => new ChestColors
        {
            damage = new Color(1f, 0.35f, 0.3f),
            perfect = new Color(0.3f, 0.92f, 0.35f),
            fastRing = new Color(0.35f, 0.85f, 1f),
            heal = new Color(1f, 0.5f, 0.78f),
        };
    }
}
