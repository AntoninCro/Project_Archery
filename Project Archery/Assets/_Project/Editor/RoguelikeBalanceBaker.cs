using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// L'équilibrage du 27 octobre au soir (décidé par le joueur après l'avis de Claude), pour ce qui est rangé dans les assets et la
    /// scène ; le reste est dans le code des pièces. Chaque valeur n'est changée que si elle a encore l'ancienne : une seule fois, et
    /// jamais une valeur réglée à la main ensuite.
    /// <list type="bullet">
    /// <item>le catalogue : le Tir écho devient légendaire (rare avant) ; les objets sortent à 50 % communs, 35 % rares, 15 %
    /// légendaires (20 / 60 / 20 avant), dans la boutique et les coffres ;</item>
    /// <item><c>Data/ShotTuning.asset</c> : le tir parfait fait ×2,5 de dégâts (×2 avant, soit ×1,25) et rapporte ×3 de points et
    /// d'or (×2 avant, soit +50 %) ;</item>
    /// <item>la scène ouverte (<c>Special Arrows</c>) : 1000 flèches en vol au plus (250 avant ; le budget d'effets de
    /// <c>Bows/Arrow</c>, <c>Core/Sfx</c> et <c>Upgrades/LightningBolt</c> garde le jeu fluide) ; la boutique s'ouvre plus à gauche
    /// (Offset From Head à -1,75 ; -0,4 ; 1,25, soit 55° au lieu de 41°), l'atelier passant à droite du joueur. La scène est alors
    /// à enregistrer (Ctrl+S).</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikeBalanceBaker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const string k_ShotTuningPath = "Assets/_Project/Data/ShotTuning.asset";

        static RoguelikeBalanceBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            UpdateCatalog();
            UpdateShotTuning();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    UpdateScene(scene);
            }
        }

        static void UpdateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_CatalogPath);
            if (catalog == null)
                return;

            var changed = false;
            var log = new List<string>();
            foreach (var upgrade in catalog.upgrades)
            {
                if (upgrade == null || upgrade.kind == UpgradeKind.Retired || upgrade.effect != UpgradeEffect.Echo ||
                    upgrade.rarity != UpgradeRarity.Rare)
                    continue;

                upgrade.rarity = UpgradeRarity.Legendary;
                changed = true;
                log.Add("le Tir écho devient légendaire");
            }

            // Les chances des raretés des objets (boutique, coffres), choisies par le joueur le même soir.
            for (var i = 0; i < catalog.rarityOdds.Count; i++)
            {
                var odds = catalog.rarityOdds[i];
                if (!Mathf.Approximately(odds.common, 20f) || !Mathf.Approximately(odds.rare, 60f) ||
                    !Mathf.Approximately(odds.legendary, 20f))
                    continue;

                odds.common = 50f;
                odds.rare = 35f;
                odds.legendary = 15f;
                catalog.rarityOdds[i] = odds;
                changed = true;
                log.Add("les objets sortent à 50 % communs, 35 % rares, 15 % légendaires");
            }

            if (!changed)
                return;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log($"Équilibrage du 27 octobre (catalogue) : {string.Join(", ", log)}.", catalog);
        }

        static void UpdateShotTuning()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<Archery.Bows.ShotTuning>(k_ShotTuningPath);
            if (tuning == null)
                return;

            var serialized = new SerializedObject(tuning);
            var log = new List<string>();
            var damage = serialized.FindProperty("m_Perfect.damage");
            if (damage != null && Mathf.Approximately(damage.floatValue, 2f))
            {
                damage.floatValue = 2.5f;
                log.Add("dégâts ×2,5");
            }

            var points = serialized.FindProperty("m_Perfect.points");
            if (points != null && Mathf.Approximately(points.floatValue, 2f))
            {
                points.floatValue = 3f;
                log.Add("points et or ×3");
            }

            if (log.Count == 0)
                return;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(tuning);
            Debug.Log($"Équilibrage du 27 octobre (ShotTuning) : le tir parfait, {string.Join(", ", log)}.", tuning);
        }

        static void UpdateScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var arrows in root.GetComponentsInChildren<SpecialArrows>(true))
                {
                    var serialized = new SerializedObject(arrows);
                    var max = serialized.FindProperty("m_MaxArrowsInFlight");
                    if (max == null || max.intValue != 250)
                        continue;

                    max.intValue = 1000;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"Équilibrage du 27 octobre (scène {scene.name}) : 1000 flèches en vol au plus. Enregistre la scène (Ctrl+S).",
                              arrows);
                }

                // La boutique plus à gauche (55° au lieu de 41°), l'atelier passant à droite du joueur (ShopPanel).
                foreach (var panel in root.GetComponentsInChildren<ShopPanel>(true))
                {
                    var serialized = new SerializedObject(panel);
                    var offset = serialized.FindProperty("m_OffsetFromHead");
                    if (offset == null || offset.vector3Value != new Vector3(-1.4f, -0.4f, 1.6f))
                        continue;

                    offset.vector3Value = new Vector3(-1.75f, -0.4f, 1.25f);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"Confort du 27 octobre (scène {scene.name}) : la boutique s'ouvre plus à gauche, l'atelier à droite du " +
                              "joueur. Enregistre la scène (Ctrl+S).", panel);
                }
            }
        }
    }
}
