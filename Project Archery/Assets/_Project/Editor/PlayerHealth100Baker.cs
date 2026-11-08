using Archery.Combat;
using Archery.Player;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les PV du joueur à 100 (4 novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.67) : le joueur, après une partie en Impossible,
    /// « augmentons les PV max à 100 » (67 depuis le troisième essai, 200 avant ; la grande régénération reste). Le joueur de la scène
    /// ouverte, une seule fois, seulement s'il a encore 67 PV ; puis enregistrer la scène (Ctrl+S). Et la Vigueur suit, à la demande
    /// du joueur (« passer à +12 ») : +12 PV au lieu de +8, la même part des PV de base (12 %, que la tour prend aussi), le nombre de sa
    /// carte avec (le reste de son texte ne change pas).
    /// </summary>
    /// <remarks>Un tour d'éditeur après les autres bakers (celui du troisième essai passe de 200 à 67 et la Vigueur de +25 à +8 au premier).</remarks>
    [InitializeOnLoad]
    static class PlayerHealth100Baker
    {
        const float k_Old = 67f;
        const float k_New = 100f;
        const float k_VigorOld = 8f;
        const float k_VigorNew = 12f;
        const string k_Catalog = "Assets/_Project/Data/Shop/ShopCatalog.asset";

        static PlayerHealth100Baker()
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

            UpdateVigor();
            UpdatePlayer();
        }

        static void UpdatePlayer()
        {
            var player = Object.FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);
            if (player == null || EditorUtility.IsPersistent(player) || !player.TryGetComponent<Health>(out var health))
                return;

            var serialized = new SerializedObject(health);
            var property = serialized.FindProperty("m_MaxHealth");
            if (property == null || !Mathf.Approximately(property.floatValue, k_Old))
                return;

            property.floatValue = k_New;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            Debug.Log($"Les PV du joueur (4 novembre) : {k_Old:0} → {k_New:0}.\n→ La scène a changé : enregistre-la (Ctrl+S).", player);
        }

        // La Vigueur : +12 PV au lieu de +8, seulement si elle a encore +8 (une fois, et jamais une valeur retouchée à la main).
        static void UpdateVigor()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_Catalog);
            if (catalog == null)
                return;
            var vigor = catalog.upgrades.Find(upgrade => upgrade != null && upgrade.effect == UpgradeEffect.Vigor);
            if (vigor == null || !Mathf.Approximately(vigor.value, k_VigorOld))
                return;

            vigor.value = k_VigorNew;
            if (vigor.description != null)
                vigor.description = vigor.description.Replace("+8 PV", "+12 PV");
            if (vigor.levelUpText != null)
                vigor.levelUpText = vigor.levelUpText.Replace("+8 PV", "+12 PV");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log($"La Vigueur (4 novembre) : +8 → +12 PV max (« {vigor.description} »).", catalog);
        }
    }
}
