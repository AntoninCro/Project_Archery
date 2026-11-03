using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Après le premier essai du tower defense par le joueur (2 novembre au soir, <c>Docs/Refonte_TowerDefense.md</c>, 12.40 et
    /// 12.41) : un niveau de duo coûte 2 plans et un niveau de trio 3 (4 avant), et MK frappe deux fois plus fort (8,33 → 16,67 ;
    /// 25 avant le 15 octobre), maintenant que le joueur se soigne tout seul quand il n'est pas touché (<c>Player/PlayerHealth</c>).
    /// Chaque réglage n'est changé que s'il a encore sa valeur d'avant : une seule fois, et jamais un réglage retouché à la main.
    /// Le reste de l'essai est dans le code (les lots d'un plan, un plan par niveau de tour, le chantier, le coffre, la grille).
    /// </summary>
    [InitializeOnLoad]
    static class FirstTestBaker
    {
        const string k_Duos = "Assets/_Project/Data/Duos";
        const string k_Dragon = "Assets/_Project/Data/Enemies/Boss_Dragon.asset";

        static FirstTestBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var log = new List<string>();
            DuoPlans(log);
            DragonDamage(log);
            if (log.Count > 0)
                Debug.Log($"Premier essai du tower defense (2 novembre) : {log.Count} réglage(s) changé(s) :\n" + string.Join("\n", log));
        }

        // Les niveaux 2 et 3 des duos (2 plans) et des trios (3 plans), s'ils coûtent encore 4 plans.
        static void DuoPlans(List<string> log)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { k_Duos }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                var plans = name.StartsWith("Trio_") ? 3 : name.StartsWith("Duo_") ? 2 : 0;
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (plans == 0 || asset == null)
                    continue;

                var serialized = new SerializedObject(asset);
                var levels = serialized.FindProperty("levels");
                if (levels == null || !levels.isArray)
                    continue;

                var changed = false;
                for (var i = 1; i < levels.arraySize; i++)
                {
                    var property = levels.GetArrayElementAtIndex(i).FindPropertyRelative("plans");
                    if (property == null || property.intValue != 4)
                        continue;

                    property.intValue = plans;
                    changed = true;
                }

                if (!changed)
                    continue;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
                log.Add($"{name} : 4 → {plans} plans par niveau");
            }
        }

        static void DragonDamage(List<string> log)
        {
            var dragon = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Dragon);
            if (dragon == null)
                return;

            var serialized = new SerializedObject(dragon);
            var property = serialized.FindProperty("attackDamage");
            if (property == null || Mathf.Abs(property.floatValue - 8.33f) > 0.006f)
                return;

            property.floatValue = 16.67f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dragon);
            AssetDatabase.SaveAssetIfDirty(dragon);
            log.Add("Boss_Dragon.attackDamage : 8,33 → 16,67 (MK frappe deux fois plus fort)");
        }
    }
}
