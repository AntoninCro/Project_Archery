using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le 8 novembre au soir, le joueur, après sa partie (<c>Docs/Refonte_TowerDefense.md</c>, 12.70) : les quatre raretés partout
    /// (commune, rare, épique, légendaire ; le code : <c>ShopCatalog</c>, <c>ShopManager</c>, <c>ChestReward</c>). Ici, ce qui est
    /// réglé dans le catalogue, chaque réglage seulement s'il a encore sa valeur d'avant (une seule fois, jamais une valeur
    /// retouchée à la main) :
    /// <list type="bullet">
    /// <item>les parchemins ont une rareté, qui multiplie leur effet (×1, ×1,5, ×2, ×3) : « il faudra diviser par 2 toutes les valeurs
    /// actuelles » ; Force +30 % → +15 %, Adresse 20 % → 10 %, Vigueur +35 → +17,5 PV, et le texte de leur carte (la commune) ;</item>
    /// <item>les chances des raretés : 50 / 35 / 15 → 45 / 30 / 17 / 8 (commune, rare, épique, légendaire) ;</item>
    /// <item>« Le style, ça compte aussi » ne rend plus les tirs parfaits (« slow + parfait me semble très puissant ») : son texte.</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class RaretesBaker
    {
        const string k_Catalog = "Assets/_Project/Data/Shop/ShopCatalog.asset";

        // (nom, valeurs d'avant (celle du 8 novembre au soir, puis celle d'avant le choix de la pause), nouvelle valeur, textes)
        static readonly (string Name, float[] Old, float New, string Description, string LevelUp)[] k_Scrolls =
        {
            ("Force", new[] { 0.3f, 0.12f }, 0.15f, "+15 % ➶, anneau 7,5 % plus rapide", "+15 % ➶ de plus, anneau 7,5 % plus rapide"),
            ("Adresse", new[] { 0.2f, 0.1f }, 0.1f, "Bande verte 10 % plus large, tirs parfaits +10 % ➶",
             "Bande verte 10 % plus large, tirs parfaits +10 % ➶ de plus"),
            ("Vigueur", new[] { 35f, 12f }, 17.5f, "+17,5 PV max, flèches 17,5 % plus loin", "+17,5 PV max de plus, flèches 17,5 % plus loin"),
        };

        const string k_StyleOldEnd = ", et chaque tir est parfait";

        static RaretesBaker()
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
            var log = new List<string>();
            UpdateScrolls(serialized, log);
            UpdateOdds(serialized, log);
            LowerOdds(serialized, log);
            UpdateStyle(serialized, log);
            if (log.Count == 0)
                return;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Les quatre raretés (8 novembre au soir) :\n" + string.Join("\n", log));
        }

        static void UpdateScrolls(SerializedObject serialized, List<string> log)
        {
            var upgrades = serialized.FindProperty("upgrades");
            for (var i = 0; upgrades != null && i < upgrades.arraySize; i++)
            {
                var entry = upgrades.GetArrayElementAtIndex(i);
                var name = entry.FindPropertyRelative("displayName");
                var value = entry.FindPropertyRelative("value");
                var kind = entry.FindPropertyRelative("kind");
                if (name == null || value == null || kind == null || kind.intValue != (int)UpgradeKind.Scroll)
                    continue;

                foreach (var scroll in k_Scrolls)
                {
                    // Déjà fait (même valeur, même texte) : rien. L'Adresse d'avant le choix de la pause valait déjà 0,1.
                    var description = entry.FindPropertyRelative("description");
                    if (name.stringValue != scroll.Name || !IsOneOf(value.floatValue, scroll.Old) ||
                        (Mathf.Approximately(value.floatValue, scroll.New) && description.stringValue == scroll.Description))
                        continue;

                    var old = value.floatValue;
                    value.floatValue = scroll.New;
                    description.stringValue = scroll.Description;
                    entry.FindPropertyRelative("levelUpText").stringValue = scroll.LevelUp;
                    log.Add($"{scroll.Name} : {old:0.###} → {scroll.New:0.###} (la commune ; ×1,5 rare, ×2 épique, ×3 légendaire)");
                }
            }
        }

        static void UpdateOdds(SerializedObject serialized, List<string> log)
        {
            var odds = serialized.FindProperty("rarityOdds");
            if (odds == null || odds.arraySize != 1)
                return;

            // L'ancienne ligne (50 / 35 / 15) : Unity a rempli l'épique toute seule avec la valeur par défaut du code (17) en
            // ajoutant le champ, sans toucher aux trois autres ; le premier jet de ce baker la croyait donc déjà réglée (une partie
            // s'est jouée à 50 / 35 / 17 / 15). Elle passe tout de suite aux chances d'après (LowerOdds).
            var row = odds.GetArrayElementAtIndex(0);
            var common = row.FindPropertyRelative("common");
            var rare = row.FindPropertyRelative("rare");
            var epic = row.FindPropertyRelative("epic");
            var legendary = row.FindPropertyRelative("legendary");
            if (common == null || rare == null || epic == null || legendary == null ||
                !Mathf.Approximately(common.floatValue, 50f) || !Mathf.Approximately(rare.floatValue, 35f) ||
                !Mathf.Approximately(legendary.floatValue, 15f) || !(epic.floatValue <= 0f || Mathf.Approximately(epic.floatValue, 17f)))
                return;

            common.floatValue = 45f;
            rare.floatValue = 30f;
            epic.floatValue = 17f;
            legendary.floatValue = 8f;
            log.Add("Chances des raretés : 50 / 35 / 15 → 45 / 30 / 17 / 8 (commune, rare, épique, légendaire)");
        }

        // Le même soir, après la partie de 15:40 (le joueur : « il faudrait réduire le drop de toutes les raretés sauf commune ») :
        // 45 / 30 / 17 / 8 → 60 / 24 / 11 / 5.
        static void LowerOdds(SerializedObject serialized, List<string> log)
        {
            var odds = serialized.FindProperty("rarityOdds");
            if (odds == null || odds.arraySize != 1)
                return;

            var row = odds.GetArrayElementAtIndex(0);
            var common = row.FindPropertyRelative("common");
            var rare = row.FindPropertyRelative("rare");
            var epic = row.FindPropertyRelative("epic");
            var legendary = row.FindPropertyRelative("legendary");
            if (common == null || rare == null || epic == null || legendary == null ||
                !Mathf.Approximately(common.floatValue, 45f) || !Mathf.Approximately(rare.floatValue, 30f) ||
                !Mathf.Approximately(epic.floatValue, 17f) || !Mathf.Approximately(legendary.floatValue, 8f))
                return;

            common.floatValue = 60f;
            rare.floatValue = 24f;
            epic.floatValue = 11f;
            legendary.floatValue = 5f;
            log.Add("Chances des raretés : 45 / 30 / 17 / 8 → 60 / 24 / 11 / 5 (moins de rares, d'épiques et de légendaires)");
        }

        static void UpdateStyle(SerializedObject serialized, List<string> log)
        {
            var upgrades = serialized.FindProperty("upgrades");
            for (var i = 0; upgrades != null && i < upgrades.arraySize; i++)
            {
                var entry = upgrades.GetArrayElementAtIndex(i);
                var effect = entry.FindPropertyRelative("effect");
                var description = entry.FindPropertyRelative("description");
                if (effect == null || description == null || effect.intValue != (int)UpgradeEffect.Acrobat)
                    continue;

                var text = description.stringValue.Replace("\n", " ");
                var cut = text.IndexOf(k_StyleOldEnd, System.StringComparison.Ordinal);
                if (cut < 0)
                    continue;

                description.stringValue = text.Substring(0, cut).TrimEnd() + text.Substring(cut + k_StyleOldEnd.Length);
                log.Add($"« Le style, ça compte aussi » : plus de tir parfait (« {description.stringValue} »)");
            }
        }

        static bool IsOneOf(float value, float[] candidates)
        {
            foreach (var candidate in candidates)
            {
                if (Mathf.Approximately(value, candidate))
                    return true;
            }

            return false;
        }
    }
}
