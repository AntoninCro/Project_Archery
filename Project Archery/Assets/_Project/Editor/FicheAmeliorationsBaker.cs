using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// La fiche des améliorations remplie par le joueur le 8 novembre au soir (<c>Docs/Fiche_Ameliorations.xlsx</c> ;
    /// <c>Docs/Refonte_TowerDefense.md</c>, 12.71) :
    /// <list type="bullet">
    /// <item>les pièces qui passent épiques (la quatrième rareté, nouvelle) : chacune seulement si elle a encore sa rareté d'avant ;</item>
    /// <item>la Pointe du chasseur : la marque reste sur un ennemi jusqu'à sa mort et ne saute plus, le tir qui marque compte déjà
    /// (le code : <c>ArrowPieces.Lot2</c>) ; le texte de sa carte, seulement s'il est encore celui d'avant.</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class FicheAmeliorationsBaker
    {
        const string k_Catalog = "Assets/_Project/Data/Shop/ShopCatalog.asset";

        // (effet, rareté d'avant, nouvelle rareté), d'après la colonne « Nouvelle rareté » de la fiche.
        static readonly (UpgradeEffect Effect, UpgradeRarity Old, UpgradeRarity New)[] k_Rarities =
        {
            (UpgradeEffect.Fuse, UpgradeRarity.Rare, UpgradeRarity.Epic),
            (UpgradeEffect.ChainLightning, UpgradeRarity.Legendary, UpgradeRarity.Epic),
            (UpgradeEffect.Boomerang, UpgradeRarity.Rare, UpgradeRarity.Epic),
            (UpgradeEffect.Recall, UpgradeRarity.Legendary, UpgradeRarity.Epic),
            (UpgradeEffect.Deluge, UpgradeRarity.Rare, UpgradeRarity.Epic),
            (UpgradeEffect.Sentinel, UpgradeRarity.Rare, UpgradeRarity.Epic),
            (UpgradeEffect.Conductor, UpgradeRarity.Legendary, UpgradeRarity.Epic),
            (UpgradeEffect.Multishot, UpgradeRarity.Rare, UpgradeRarity.Epic),
            (UpgradeEffect.Triple, UpgradeRarity.Rare, UpgradeRarity.Epic),
            (UpgradeEffect.Echo, UpgradeRarity.Legendary, UpgradeRarity.Epic),
            (UpgradeEffect.Engineer, UpgradeRarity.Rare, UpgradeRarity.Epic),
        };

        const string k_MarkOldStart = "Marque la cible";
        const string k_MarkOldJump = "la marque saute";
        const string k_MarkDescription = "Le premier ennemi touché est marqué jusqu'à sa mort : il prend {200 %|250 %|300 %} ➶, dès le tir " +
                                         "qui le marque";
        const string k_MarkLevelUp = "{200 %|250 %|300 %} ➶ sur l'ennemi marqué";

        static FicheAmeliorationsBaker()
        {
            EditorApplication.delayCall -= Later;
            EditorApplication.delayCall += Later;
        }

        static void Later()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_Catalog);
            if (catalog == null)
                return;

            var serialized = new SerializedObject(catalog);
            var upgrades = serialized.FindProperty("upgrades");
            if (upgrades == null || !upgrades.isArray)
                return;

            var log = new List<string>();
            for (var i = 0; i < upgrades.arraySize; i++)
            {
                var entry = upgrades.GetArrayElementAtIndex(i);
                var effect = entry.FindPropertyRelative("effect");
                var kind = entry.FindPropertyRelative("kind");
                var rarity = entry.FindPropertyRelative("rarity");
                var name = entry.FindPropertyRelative("displayName");
                if (effect == null || kind == null || rarity == null || kind.intValue != (int)UpgradeKind.Object)
                    continue;

                foreach (var change in k_Rarities)
                {
                    if (effect.intValue != (int)change.Effect || rarity.intValue != (int)change.Old)
                        continue;
                    rarity.intValue = (int)change.New;
                    log.Add($"{name.stringValue} : {ShopCatalog.NameOf(change.Old)} → {ShopCatalog.NameOf(change.New)}");
                }

                if (effect.intValue == (int)UpgradeEffect.HunterMark)
                {
                    var description = entry.FindPropertyRelative("description");
                    var text = description.stringValue.Replace("\n", " ");
                    if (text.StartsWith(k_MarkOldStart) && text.Contains(k_MarkOldJump))
                    {
                        description.stringValue = k_MarkDescription;
                        entry.FindPropertyRelative("levelUpText").stringValue = k_MarkLevelUp;
                        log.Add($"{name.stringValue} : la marque reste jusqu'à la mort, sans sauter (« {k_MarkDescription} »)");
                    }
                }
            }

            if (log.Count == 0)
                return;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"La fiche des améliorations (8 novembre au soir) : {log.Count} changement(s) :\n" + string.Join("\n", log));
        }
    }
}
