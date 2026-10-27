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
    /// Le deuxième tour de réglages du 27 octobre (le patch note <c>Docs/Patch_Note_Pieces.xlsx</c> rempli par le joueur), pour ce
    /// qui est rangé dans le catalogue et la scène ; le reste est dans le code des pièces. Chaque valeur n'est changée que si elle a
    /// encore l'ancienne : une seule fois, et jamais une valeur réglée à la main ensuite.
    /// <list type="bullet">
    /// <item>le catalogue : Tir écho 1 / 2 / 3 échos sûrs (0,5 par niveau avant : une chance), Vampirisme 3 / 6 / 9 PV (2 par niveau
    /// avant), Vigueur +25 PV par parchemin (+15 avant) ;</item>
    /// <item>la scène ouverte (<c>Special Arrows</c>) : la chaîne d'éclairs à 30 / 60 / 90 % (25 / 50 / 75 % avant), le ricochet à
    /// 100 % de la vitesse (85 % avant). La scène est alors à enregistrer (Ctrl+S).</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikePatchBaker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";

        static readonly (UpgradeEffect effect, float oldValue, float newValue, string what)[] k_Values =
        {
            (UpgradeEffect.Echo, 0.5f, 1f, "Tir écho : 1 / 2 / 3 échos"),
            (UpgradeEffect.Vampirism, 2f, 3f, "Vampirisme : 3 / 6 / 9 PV"),
            (UpgradeEffect.Vigor, 15f, 25f, "Vigueur : +25 PV"),
        };

        static RoguelikePatchBaker()
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

            var changed = new List<string>();
            foreach (var (effect, oldValue, newValue, what) in k_Values)
            {
                foreach (var upgrade in catalog.upgrades)
                {
                    if (upgrade == null || upgrade.kind == UpgradeKind.Retired || upgrade.effect != effect ||
                        !Mathf.Approximately(upgrade.value, oldValue))
                        continue;

                    upgrade.value = newValue;
                    changed.Add(what);
                }
            }

            if (changed.Count == 0)
                return;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log($"Réglages roguelike du 27 octobre, deuxième tour (catalogue) : {string.Join(", ", changed)}.", catalog);
        }

        static void UpdateScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var arrows in root.GetComponentsInChildren<SpecialArrows>(true))
                {
                    var serialized = new SerializedObject(arrows);
                    var log = new List<string>();
                    var chain = serialized.FindProperty("m_ChainDamageByLevel");
                    if (chain != null && chain.vector3Value == new Vector3(0.25f, 0.5f, 0.75f))
                    {
                        chain.vector3Value = new Vector3(0.3f, 0.6f, 0.9f);
                        log.Add("la chaîne d'éclairs à 30 / 60 / 90 %");
                    }

                    var ricochet = serialized.FindProperty("m_RicochetSpeed");
                    if (ricochet != null && Mathf.Approximately(ricochet.floatValue, 0.85f))
                    {
                        ricochet.floatValue = 1f;
                        log.Add("le ricochet à 100 % de la vitesse");
                    }

                    if (log.Count == 0)
                        continue;

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"Réglages roguelike du 27 octobre, deuxième tour (scène {scene.name}) : {string.Join(", ", log)}. " +
                              "Enregistre la scène (Ctrl+S).", arrows);
                }
            }
        }
    }
}
