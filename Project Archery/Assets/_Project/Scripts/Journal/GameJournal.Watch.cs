#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using Archery.Building;
using Archery.Shop;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Journal
{
    // Ce que le journal surveille (3 novembre) : les bâtiments (comparés toutes les 0,5 s à ceux d'avant : posés, construits, améliorés,
    // fusionnés, retirés, déplacés, partis dans l'arène de MK), les objets et les pièces de la flèche, les plans.
    public partial class GameJournal
    {
        // Au-delà, des bâtiments posés d'un coup (la vitrine F4) tiennent en une ligne.
        const int k_ManyAtOnce = 6;

        sealed class Seen
        {
            public string Name;
            public int Level;
            public bool Building;
            public Vector3 Position;
            public bool InArena;
            public bool Fusion;
        }

        readonly Dictionary<Structure, Seen> m_Structures = new Dictionary<Structure, Seen>();
        readonly List<Structure> m_Appeared = new List<Structure>();
        readonly List<Seen> m_Gone = new List<Seen>();
        readonly List<Structure> m_GoneKeys = new List<Structure>();
        readonly HashSet<Structure> m_Present = new HashSet<Structure>();
        readonly Dictionary<string, (int Count, int Level)> m_Owned = new Dictionary<string, (int, int)>();
        readonly Dictionary<ArrowSlot, string> m_Equipped = new Dictionary<ArrowSlot, string>();
        readonly Dictionary<StructureDefinition, int> m_Plans = new Dictionary<StructureDefinition, int>();
        bool m_StructuresSeen;

        void ResetWatch()
        {
            m_Structures.Clear();
            m_Owned.Clear();
            m_Equipped.Clear();
            m_Plans.Clear();
            m_StructuresSeen = false;
        }

        void Watch()
        {
            WatchStructures();
            WatchChests();
        }

        // ---------------------------------------------------------------- les bâtiments

        void WatchStructures()
        {
            m_Appeared.Clear();
            m_Gone.Clear();
            m_Present.Clear();
            foreach (var structure in Structure.All)
            {
                if (structure == null)
                    continue;
                m_Present.Add(structure);
                if (!m_Structures.TryGetValue(structure, out var seen))
                {
                    m_Structures[structure] = Look(structure);
                    m_Appeared.Add(structure);
                    continue;
                }

                Compare(structure, seen);
            }

            m_GoneKeys.Clear();
            foreach (var pair in m_Structures)
            {
                if (m_Present.Contains(pair.Key))
                    continue;
                m_Gone.Add(pair.Value);
                m_GoneKeys.Add(pair.Key);
            }

            foreach (var key in m_GoneKeys)
                m_Structures.Remove(key);

            // Le premier regard : ce qui est déjà là (le chantier du début) tient en une ligne.
            if (!m_StructuresSeen)
            {
                m_StructuresSeen = true;
                if (m_Appeared.Count > 0)
                    Line("Bâtiments au départ : " + Group(m_Appeared.Select(Describe)) + ".");
                return;
            }

            Announce();
        }

        static Seen Look(Structure structure) => new Seen
        {
            Name = structure.DisplayName,
            Level = structure.Level,
            Building = structure.IsUnderConstruction,
            Position = structure.transform.position,
            InArena = structure.InArena,
            Fusion = structure is TowerDuo,
        };

        // Un bâtiment déjà vu : son niveau, son chantier, sa place, l'arène.
        void Compare(Structure structure, Seen seen)
        {
            if (structure.InArena != seen.InArena)
            {
                Line(structure.InArena ? $"Dans l'arène de MK : {Describe(structure)}." : $"Revenu sur la carte : {structure.DisplayName}.");
                seen.InArena = structure.InArena;
                seen.Position = structure.transform.position;
            }

            if (structure.IsUnderConstruction && !seen.Building)
                Line($"Chantier : {structure.DisplayName} vers le niveau {structure.TargetLevel}.");
            seen.Building = structure.IsUnderConstruction;

            if (structure.Level != seen.Level && structure.Level > 0)
            {
                Line(seen.Level <= 0 ? $"Construit : {Describe(structure)}." : $"Amélioré : {structure.DisplayName}, niveau {seen.Level} → {structure.Level}.");
                seen.Level = structure.Level;
            }

            // À plat seulement : une ultime qui sort de terre (le Grand Manège) n'est pas déplacée (le premier journal, 3 novembre).
            var moved = structure.transform.position - seen.Position;
            moved.y = 0f;
            if (!seen.InArena && !structure.InArena && moved.sqrMagnitude > 2f * 2f)
            {
                Line($"Déplacé : {structure.DisplayName}.");
                seen.Position = structure.transform.position;
            }
        }

        // Ce qui est apparu ou parti depuis le dernier regard : une fusion (des bâtiments partis, un duo, un trio ou une ultime
        // apparu), une vitrine (beaucoup d'un coup), ou un par un.
        void Announce()
        {
            var fusion = m_Appeared.FirstOrDefault(structure => structure is TowerDuo);
            if (fusion != null && m_Gone.Count > 0)
            {
                Line($"**Fusion** : {string.Join(" + ", m_Gone.Select(gone => $"{gone.Name} niv. {gone.Level}"))} → " +
                     $"{BuildManager.FusionLabel((TowerDuo)fusion)} **{fusion.DisplayName}** niv. {Mathf.Max(fusion.Level, fusion.TargetLevel)}.");
                m_Appeared.Remove(fusion);
                m_Gone.Clear();
            }

            if (m_Appeared.Count > k_ManyAtOnce)
            {
                Line($"{m_Appeared.Count} bâtiments posés d'un coup (la vitrine F4 ?) : {Group(m_Appeared.Select(Describe))}.");
            }
            else
            {
                foreach (var structure in m_Appeared)
                    Line(structure.IsUnderConstruction
                        ? $"Chantier : {structure.DisplayName} vers le niveau {structure.TargetLevel}."
                        : $"Posé : {Describe(structure)}.");
            }

            if (m_Gone.Count > k_ManyAtOnce)
                Line($"{m_Gone.Count} bâtiments retirés d'un coup : {Group(m_Gone.Select(gone => $"{gone.Name} niv. {gone.Level}"))}.");
            else
                foreach (var gone in m_Gone)
                    Line($"Retiré : {gone.Name} niv. {gone.Level}.");
        }

        static string Describe(Structure structure)
        {
            var kind = structure is TowerDuo duo ? BuildManager.FusionLabel(duo).ToLowerInvariant() + " " : "";
            var level = structure.IsUnderConstruction ? $"chantier vers le niveau {structure.TargetLevel}" : $"niv. {structure.Level}";
            return $"{kind}{structure.DisplayName} {level}";
        }

        // « Baliste niv. 2 ×3, Mortier niv. 1 » : les mêmes regroupés.
        static string Group(IEnumerable<string> names) =>
            string.Join(", ", names.GroupBy(name => name).Select(group => group.Count() > 1 ? $"{group.Key} ×{group.Count()}" : group.Key));

        string BuildingsText()
        {
            var built = Structure.All.Where(structure => structure != null && (structure.IsBuilt || structure.IsUnderConstruction)).ToList();
            return built.Count > 0 ? Group(built.Select(Describe)) : "aucun";
        }

        int BuiltCount() => Structure.All.Count(structure => structure != null && structure.IsBuilt);

        // ---------------------------------------------------------------- les objets et la flèche

        void SnapshotUpgrades()
        {
            ReadOwned(m_Owned);
            ReadEquipped(m_Equipped);
        }

        void ReadOwned(Dictionary<string, (int Count, int Level)> owned)
        {
            owned.Clear();
            if (m_Upgrades == null)
                return;
            foreach (var upgrade in m_Upgrades.Owned)
            {
                if (upgrade == null)
                    continue;
                var count = owned.TryGetValue(upgrade.displayName, out var old) ? old.Count + 1 : 1;
                owned[upgrade.displayName] = (count, m_Upgrades.Level(upgrade.effect));
            }
        }

        void ReadEquipped(Dictionary<ArrowSlot, string> equipped)
        {
            equipped.Clear();
            if (m_Upgrades == null)
                return;
            foreach (ArrowSlot slot in System.Enum.GetValues(typeof(ArrowSlot)))
            {
                if (slot == ArrowSlot.None)
                    continue;
                var piece = m_Upgrades.InSlot(slot);
                if (piece != null)
                    equipped[slot] = piece.displayName;
            }
        }

        // Un objet gagné (un coffre, la boutique, un raccourci), un niveau de plus, une pièce équipée.
        void OnUpgradesChanged()
        {
            if (m_Upgrades == null)
                return;
            var owned = new Dictionary<string, (int Count, int Level)>();
            var equipped = new Dictionary<ArrowSlot, string>();
            ReadOwned(owned);
            ReadEquipped(equipped);
            if (Active)
            {
                var gained = new List<string>();
                foreach (var pair in owned)
                {
                    if (!m_Owned.TryGetValue(pair.Key, out var before))
                        gained.Add(UpgradeText(pair.Key, pair.Value.Count, pair.Value.Level, true));
                    else if (pair.Value.Count > before.Count || pair.Value.Level > before.Level)
                        gained.Add(UpgradeText(pair.Key, pair.Value.Count, pair.Value.Level, false));
                }

                if (gained.Count > k_ManyAtOnce)
                    Line($"{gained.Count} objets d'un coup : {string.Join(", ", gained)}.");
                else
                    foreach (var text in gained)
                        Line(text + ".");

                var lost = m_Owned.Keys.Where(name => !owned.ContainsKey(name)).ToList();
                if (lost.Count > 0)
                    Line("Objets perdus : " + string.Join(", ", lost) + ".");

                foreach (var pair in equipped)
                {
                    if (!m_Equipped.TryGetValue(pair.Key, out var before) || before != pair.Value)
                        Line($"Équipé ({ShopCatalog.NameOf(pair.Key)}) : {pair.Value}.");
                }
            }

            m_Owned.Clear();
            foreach (var pair in owned)
                m_Owned[pair.Key] = pair.Value;
            m_Equipped.Clear();
            foreach (var pair in equipped)
                m_Equipped[pair.Key] = pair.Value;
        }

        // « Objet : Pointe de feu (rare, pointe), niveau 1 » ; « Parchemin de force ×3 ».
        string UpgradeText(string name, int count, int level, bool fresh)
        {
            var upgrade = m_Upgrades.Owned.FirstOrDefault(owned => owned != null && owned.displayName == name);
            var about = upgrade != null
                ? $" ({ShopCatalog.NameOf(upgrade.rarity).ToLowerInvariant()}{(upgrade.slot != ArrowSlot.None ? ", " + ShopCatalog.NameOf(upgrade.slot).ToLowerInvariant() : "")})"
                : "";
            var amount = count > 1 ? $" ×{count}" : level > 1 ? $", niveau {level}" : "";
            return fresh ? $"Objet : **{name}**{about}{amount}" : $"Objet : {name}{amount}";
        }

        string OwnedText()
        {
            var owned = new Dictionary<string, (int Count, int Level)>();
            ReadOwned(owned);
            return string.Join(", ", owned.Select(pair => pair.Value.Count > 1 ? $"{pair.Key} ×{pair.Value.Count}"
                                                             : pair.Value.Level > 1 ? $"{pair.Key} niv. {pair.Value.Level}" : pair.Key));
        }

        string ArrowText()
        {
            if (m_Upgrades == null)
                return "?";
            var parts = new List<string>();
            foreach (ArrowSlot slot in System.Enum.GetValues(typeof(ArrowSlot)))
            {
                if (slot == ArrowSlot.None)
                    continue;
                var piece = m_Upgrades.InSlot(slot);
                if (piece != null)
                    parts.Add($"{ShopCatalog.NameOf(slot).ToLowerInvariant()} : {piece.displayName} (niv. {m_Upgrades.Level(piece.effect)})");
            }

            return parts.Count > 0 ? string.Join(" ; ", parts) : "aucune pièce";
        }

        // ---------------------------------------------------------------- les plans

        void SnapshotPlans()
        {
            m_Plans.Clear();
            if (m_Build == null)
                return;
            foreach (var definition in m_Build.Structures)
            {
                if (definition != null)
                    m_Plans[definition] = m_Build.PlansOf(definition);
            }
        }

        // Des plans gagnés (un coffre, B) ou dépensés (une construction, un niveau).
        void OnPlansChanged()
        {
            if (m_Build == null)
                return;
            var changes = new List<string>();
            foreach (var definition in m_Build.Structures)
            {
                if (definition == null)
                    continue;
                var now = m_Build.PlansOf(definition);
                var before = m_Plans.TryGetValue(definition, out var old) ? old : 0;
                if (now != before)
                    changes.Add($"{(now > before ? "+" : "−")}{Mathf.Abs(now - before)} {definition.displayName}");
                m_Plans[definition] = now;
            }

            if (Active && changes.Count > 0)
                Line($"Plans : {string.Join(", ", changes)} (en réserve : {m_Build.TotalPlans}).");
        }

        string PlansText()
        {
            var parts = new List<string>();
            foreach (var definition in m_Build.Structures)
            {
                var count = definition != null ? m_Build.PlansOf(definition) : 0;
                if (count > 0)
                    parts.Add($"{definition.displayName} {count}");
            }

            return parts.Count > 0 ? $"{m_Build.TotalPlans} ({string.Join(", ", parts)})" : "aucun";
        }
    }
}
#endif
