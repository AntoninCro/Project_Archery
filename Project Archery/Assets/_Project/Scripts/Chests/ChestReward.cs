using System.Collections.Generic;
using Archery.Building;
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

        /// <summary>Des plans de construction : un lot d'un bâtiment (le coffre, depuis le 2 novembre ; la caisse de l'ingénieur avant).</summary>
        Plans,
    }

    /// <summary>Ce que donne une orbe de coffre (GDD, section 13).</summary>
    public struct ChestReward
    {
        public ChestRewardKind Kind;
        public Upgrade Upgrade;
        public TemporaryBuff Buff;

        /// <summary>Le bâtiment d'un lot de plans, et le nombre de plans du lot.</summary>
        public StructureDefinition Structure;
        public int Count;

        public string Title;
        public string Subtitle;
        public Color Color;

        /// <summary>Icône de la boutique qui dit la même chose (vide pour le morceau d'arc : son orbe garde sa boule).</summary>
        public Sprite Icon;

        /// <summary>Gardée toute la partie (amélioration, morceau d'arc), et non temporaire (bonus).</summary>
        public bool IsPermanent => Kind != ChestRewardKind.Buff;

        /// <summary>
        /// Au-dessus de l'orbe : l'emplacement d'une pièce (« POINTE »), « OBJET » hors emplacement, « PARCHEMIN »,
        /// « TEMPORAIRE », « CONSTRUCTION » pour un lot de plans, ou « PERMANENT » pour le fragment d'arc.
        /// </summary>
        public string KindLabel => Kind switch
        {
            ChestRewardKind.Buff => "TEMPORAIRE",
            ChestRewardKind.Plans => "CONSTRUCTION",
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

                case ChestRewardKind.Plans:
                    var builds = BuildManager.Instance;
                    if (builds != null)
                        builds.AddPlans(Structure, Count);
                    var plans = Count > 1 ? Count + " plans" : "1 plan";
                    return $"{Title} : {plans}\n<size=70%>Vise un socle de construction pour le poser</size>";

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
        static readonly List<ArrowSlot> s_TakenSlots = new List<ArrowSlot>();
        static readonly List<StructureDefinition> s_Structures = new List<StructureDefinition>();

        /// <summary>
        /// Le coffre (2 novembre, le joueur : un seul coffre à la place du coffre doré et de la caisse de l'ingénieur ; les deux
        /// roguelikes, l'arc et les tourelles, se disputent chaque coffre), toujours dans cet ordre : un parchemin (pour l'arc et les
        /// tours) ; une tourelle (un lot de plans d'un bâtiment du chantier) ; un objet (pour l'arc), tiré comme en boutique. Un
        /// fragment de l'arc légendaire peut remplacer le parchemin. Sans plan à donner (pas de tower defense) : un objet à la place ;
        /// sans objet à donner (tous au niveau maximal) : une autre tourelle.
        /// (Avant : un objet, un parchemin et un bonus temporaire ; la caisse de l'ingénieur, trois lots de plans. Le matin du
        /// 2 novembre, la troisième orbe était une tourelle une fois sur deux ; le soir, après le premier essai, toujours un objet :
        /// une partie sans pièce d'arc ne doit pas être possible.)
        /// </summary>
        public static List<ChestReward> Roll()
        {
            var rewards = new List<ChestReward>();
            if (TryRollUpgrade(UpgradeKind.Scroll, rewards, out var scroll))
                rewards.Add(scroll);

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
                if (rewards.Count >= 1)
                    rewards[0] = part;
                else
                    rewards.Add(part);
            }

            // La deuxième orbe : une tourelle ; la troisième : un objet (deux tourelles différentes si plus d'objet à donner).
            var plans = RollPlans(2);
            if (plans.Count > 0)
                rewards.Add(plans[0]);
            else if (TryRollUpgrade(UpgradeKind.Object, rewards, out var instead))
                rewards.Add(instead);

            if (TryRollUpgrade(UpgradeKind.Object, rewards, out var item))
                rewards.Add(item);
            else if (plans.Count > 1)
                rewards.Add(plans[1]);
            else if (TryRollUpgrade(UpgradeKind.Scroll, rewards, out var other))
                rewards.Add(other);

            return rewards;
        }

        /// <summary>
        /// Le coffre de boss : <paramref name="objects"/> objets différents (des parchemins quand il ne reste plus d'objet à
        /// donner), un seul à prendre. (Le matin du 2 novembre, une dernière orbe de 3 plans au hasard ; retirée le soir par le
        /// joueur, après son premier essai : le coffre de boss est pour l'arc.)
        /// </summary>
        public static List<ChestReward> RollBoss(int objects)
        {
            var rewards = new List<ChestReward>();
            for (var i = 0; i < objects; i++)
            {
                if (TryRollUpgrade(UpgradeKind.Object, rewards, out var reward) || TryRollUpgrade(UpgradeKind.Scroll, rewards, out reward))
                    rewards.Add(reward);
            }

            return rewards;
        }

        /// <summary>
        /// <paramref name="count"/> lots de plans de bâtiments différents, tirés au hasard dans le chantier de la partie
        /// (<see cref="BuildManager.RunStructures"/>, 2 novembre ; tous les bâtiments proposés s'il est vide). Un lot donne
        /// <see cref="BuildManager.PlansPerChest"/> × <see cref="StructureDefinition.LotSize"/> plans : 2 depuis le 2 novembre au soir
        /// (1 avant ; avant, 2 pour la barricade et les piques, 1 pour les autres). (Du 28 octobre au 2 novembre, chaque orbe
        /// tirait d'abord une rareté parmi tous les bâtiments ; la rareté sert maintenant à tirer les bâtiments du chantier.)
        /// </summary>
        public static List<ChestReward> RollPlans(int count)
        {
            var rewards = new List<ChestReward>();
            var builds = BuildManager.Instance;
            if (builds == null)
                return rewards;

            s_Structures.Clear();
            foreach (var definition in builds.RunStructures)
            {
                if (BuildManager.IsOffered(definition) && !s_Structures.Contains(definition))
                    s_Structures.Add(definition);
            }

            if (s_Structures.Count == 0)
            {
                foreach (var definition in builds.Structures)
                {
                    if (BuildManager.IsOffered(definition) && !s_Structures.Contains(definition))
                        s_Structures.Add(definition);
                }
            }

            while (rewards.Count < count && s_Structures.Count > 0)
            {
                var index = Random.Range(0, s_Structures.Count);
                var definition = s_Structures[index];
                s_Structures.RemoveAt(index);

                var lot = builds.PlansPerChest * definition.LotSize;
                var owned = builds.PlansOf(definition);
                var subtitle = lot > 1 ? lot + " plans" : "1 plan";
                if (owned > 0)
                    subtitle = ShopCatalog.Lines(subtitle, $"tu en as {owned}");
                rewards.Add(new ChestReward
                {
                    Kind = ChestRewardKind.Plans,
                    Structure = definition,
                    Count = lot,
                    Title = definition.displayName,
                    Subtitle = subtitle,
                    Color = definition.color,
                    Icon = definition.icon,
                });
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
                // Comme en boutique (26 octobre au soir) : l'emplacement d'abord, de préférence un autre que ceux déjà dans le coffre.
                var waves = WaveManager.Instance;
                var wave = waves != null ? Mathf.Max(1, waves.WaveNumber) : 1;
                s_TakenSlots.Clear();
                foreach (var reward1 in taken)
                {
                    if (reward1.Kind == ChestRewardKind.Upgrade && reward1.Upgrade != null)
                        s_TakenSlots.Add(reward1.Upgrade.slot);
                }

                upgrade = catalog.PickObject(wave, owned, candidate => IsTaken(taken, candidate), s_TakenSlots);
                if (upgrade == null)
                    return false;
            }
            else if (!PickUpgrade(catalog, kind, UpgradeRarity.Common, owned, taken, false, out upgrade))
            {
                return false;
            }

            // Le niveau qu'elle atteindrait (au niveau 2 et plus, ce qui change : « 50 % → 100 % ➶ », 27 octobre au soir), et
            // dessous, le duo qu'elle pourrait former et si elle irait dans l'atelier.
            var level = (owned != null ? owned.Level(upgrade.effect) : 0) + 1;
            var duo = ShopCatalog.DuoLine(upgrade, owned);
            var storage = ShopCatalog.StorageLine(owned != null ? owned.Occupant(upgrade) : null);
            var subtitle = ShopCatalog.SubtitleOf(upgrade, level);
            if (level > 1)
                subtitle = ShopCatalog.Lines(subtitle, upgrade.LevelUpAt(level));
            reward = new ChestReward
            {
                Kind = ChestRewardKind.Upgrade,
                Upgrade = upgrade,
                Title = upgrade.displayName,
                Subtitle = ShopCatalog.Lines(subtitle, duo, storage),
                Color = catalog.ColorOf(upgrade),
                Icon = upgrade.icon,
            };
            return true;
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
    }
}
