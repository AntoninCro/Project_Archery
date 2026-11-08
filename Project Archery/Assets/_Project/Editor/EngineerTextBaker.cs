using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le texte de la carte du talisman de l'ingénieur (3 novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.57) : il surcharge
    /// maintenant toutes les tourelles, plus seulement la baliste. Le joueur : « oui, remplace » (« ta baliste » par « une de tes
    /// tourelles », dans le texte qu'il avait refait le 27 octobre). Une seule fois : seulement si le texte dit encore « dans ta
    /// baliste » ; le reste du texte n'est pas touché.
    /// </summary>
    [InitializeOnLoad]
    static class EngineerTextBaker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const string k_Old = "dans ta baliste";
        const string k_New = "dans une de tes tourelles";

        static EngineerTextBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_CatalogPath);
            if (catalog == null)
                return;

            var upgrade = catalog.upgrades.Find(candidate => candidate != null && candidate.effect == UpgradeEffect.Engineer);
            if (upgrade == null || upgrade.description == null || !upgrade.description.Contains(k_Old))
                return;

            upgrade.description = upgrade.description.Replace(k_Old, k_New);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log($"Le talisman de l'ingénieur (3 novembre) : il surcharge toutes les tourelles ; le texte de sa carte dit « {k_New} » " +
                      $"au lieu de « {k_Old} » : « {upgrade.description} ».", catalog);
        }
    }
}
