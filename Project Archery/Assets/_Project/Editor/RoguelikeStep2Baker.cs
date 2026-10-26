using System.Collections.Generic;
using System.Text;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Refonte de la partie roguelike, étape 2 (26 octobre, <c>Docs/Refonte_Roguelike.md</c>) : les emplacements de la flèche.
    /// Range les objets du catalogue (<c>Data/Shop/ShopCatalog.asset</c>) dans leur emplacement, une seule fois : quand
    /// l'étape 1 est faite (il y a des parchemins) et qu'aucun objet n'a encore d'emplacement.
    /// <list type="bullet">
    /// <item>Pointe (l'impact) : Flèche de foudre, Chaîne d'éclairs, Flèche de glace, Flèche explosive, Perçage, Vampirisme,
    /// Chasseur de têtes ;</item>
    /// <item>Empennage (la trajectoire) : Auto-visée, Tir ricochet ;</item>
    /// <item>Fût (en vol) : Déluge ;</item>
    /// <item>Corde (la façon de tirer) : Multitir, Tir écho ;</item>
    /// <item>le Talisman attend ses pièces (étape 3) ; les objets de la grenade restent hors emplacement.</item>
    /// </list>
    /// Une pièce monte jusqu'au niveau 3 (Max Stacks 3) : reprendre la même ajoute sa valeur, comme avant. Les objets de la
    /// grenade restent à un exemplaire. L'asset est enregistré tout seul ; la console dit ce qui a changé.
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikeStep2Baker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const int k_MaxLevel = 3;

        static readonly Dictionary<UpgradeEffect, ArrowSlot> k_Slots = new Dictionary<UpgradeEffect, ArrowSlot>
        {
            { UpgradeEffect.Lightning, ArrowSlot.Tip },
            { UpgradeEffect.ChainLightning, ArrowSlot.Tip },
            { UpgradeEffect.Frost, ArrowSlot.Tip },
            { UpgradeEffect.Explosive, ArrowSlot.Tip },
            { UpgradeEffect.Piercing, ArrowSlot.Tip },
            { UpgradeEffect.Vampirism, ArrowSlot.Tip },
            { UpgradeEffect.HeadHunter, ArrowSlot.Tip },
            { UpgradeEffect.Homing, ArrowSlot.Fletching },
            { UpgradeEffect.Ricochet, ArrowSlot.Fletching },
            { UpgradeEffect.Deluge, ArrowSlot.Shaft },
            { UpgradeEffect.Multishot, ArrowSlot.String },
            { UpgradeEffect.Echo, ArrowSlot.String },
        };

        static RoguelikeStep2Baker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_CatalogPath);
            // Les pièces nouvelles de l'étape 3 ont déjà leur emplacement : seuls les anciens objets comptent.
            if (catalog == null || !catalog.upgrades.Exists(upgrade => upgrade != null && upgrade.kind == UpgradeKind.Scroll) ||
                catalog.upgrades.Exists(upgrade => upgrade != null && k_Slots.ContainsKey(upgrade.effect) && upgrade.slot != ArrowSlot.None))
                return;

            var bySlot = new SortedDictionary<ArrowSlot, List<string>>();
            foreach (var upgrade in catalog.upgrades)
            {
                if (upgrade == null || upgrade.kind != UpgradeKind.Object || !k_Slots.TryGetValue(upgrade.effect, out var slot))
                    continue;

                upgrade.slot = slot;
                upgrade.maxStacks = k_MaxLevel;
                if (!bySlot.TryGetValue(slot, out var names))
                    bySlot[slot] = names = new List<string>();
                names.Add(upgrade.displayName);
            }

            if (bySlot.Count == 0)
                return;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);

            var log = new StringBuilder("Refonte roguelike, étape 2 : les objets du catalogue rangés dans les emplacements de la flèche " +
                                        $"(niveau {k_MaxLevel} au plus).\n");
            foreach (var pair in bySlot)
                log.Append(ShopCatalog.NameOf(pair.Key)).Append(" : ").Append(string.Join(", ", pair.Value)).Append('\n');
            log.Append("Talisman : aucune pièce pour l'instant. Hors emplacement : les objets de la grenade.");
            Debug.Log(log.ToString(), catalog);
        }
    }
}
