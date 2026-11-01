using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// L'équilibrage des ennemis du 1er novembre (demandé par le joueur : mort à la vague 4 en Normal avec une bonne partie ; les
    /// vagues 1 et 2 vont bien, la 3 est un peu trop dure, puis c'est de pire en pire ; trop de PV aux boss, trop de volants, des
    /// tireurs qui tirent de trop loin). Chaque valeur n'est changée que si elle a encore l'ancienne : une seule fois, et jamais une
    /// valeur réglée à la main ensuite.
    /// <list type="bullet">
    /// <item>Normal (<c>Data/Difficulties/Difficulty_Normal</c>) : PV ×1,15 par vague (×1,19 avant), nombre ×1,09 (×1,12),
    /// dégâts ×1,07 (×1,09) ;</item>
    /// <item>la courbe vague par vague (<c>Data/Waves/WaveSettings</c>, toutes les difficultés sauf Facile) : PV 0,8 ; 1,03 ;
    /// 1,42 ; 1,62 ; 1,8 (0,85 ; 1,1 ; 1,65 ; 2,2 ; 2,85 avant), nombre 0,85 ; 1 ; 1,12 ; 1,25 ; 1,38 (0,85 ; 1 ; 1,2 ; 1,4 ;
    /// 1,6) ; les volants moins souvent (Volant 0,35, Guêpe 0,3, Gargouille 0,25 ; 0,6, 0,5 et 0,4 avant) ;</item>
    /// <item>les boss des vagues (<c>Data/Enemies/Boss_*</c>) : PV de base −15 % (Champignon), −25 % (Mimique), −35 % (Tortue,
    /// Chef orc, Liche) ; le dragon ne change pas ;</item>
    /// <item>les tireurs : portée 21 m (Tireur, 25 avant), 24 m (Liche, 28), 26 m (Boss Liche, 30).</item>
    /// </list>
    /// En Normal, les PV d'un ennemi passent ainsi, de la vague 1 à 5, de ×0,85 ; 1,31 ; 2,34 ; 3,71 ; 5,72 à ×0,8 ; 1,18 ;
    /// 1,88 ; 2,46 ; 3,15, et le nombre d'ennemis de ×0,85 ; 1,12 ; 1,51 ; 1,97 ; 2,52 à ×0,85 ; 1,09 ; 1,33 ; 1,62 ; 1,95.
    /// </summary>
    [InitializeOnLoad]
    static class EnemyBalanceBaker
    {
        const string k_Normal = "Assets/_Project/Data/Difficulties/Difficulty_Normal.asset";
        const string k_Waves = "Assets/_Project/Data/Waves/WaveSettings.asset";
        const string k_Enemies = "Assets/_Project/Data/Enemies/";

        static readonly float[] k_OldHealthCurve = { 0.85f, 1.1f, 1.65f, 2.2f, 2.85f };
        static readonly float[] k_NewHealthCurve = { 0.8f, 1.03f, 1.42f, 1.62f, 1.8f };
        static readonly float[] k_OldCountCurve = { 0.85f, 1f, 1.2f, 1.4f, 1.6f };
        static readonly float[] k_NewCountCurve = { 0.85f, 1f, 1.12f, 1.25f, 1.38f };

        // Les volants : le nom de leur prefab, leur poids avant et après.
        static readonly (string Prefab, float Old, float New)[] k_Flyers =
        {
            ("Enemy_Volant", 0.6f, 0.35f), ("Enemy_Guepe", 0.5f, 0.3f), ("Enemy_Gargouille", 0.4f, 0.25f),
        };

        // Les définitions : un réglage, sa valeur avant et après.
        static readonly (string Asset, string Field, float Old, float New)[] k_Definitions =
        {
            ("Boss_Champignon", "maxHealth", 320f, 272f),
            ("Boss_Mimique", "maxHealth", 510f, 383f),
            ("Boss_Tortue", "maxHealth", 910f, 592f),
            ("Boss_ChefOrc", "maxHealth", 1210f, 787f),
            ("Boss_Liche", "maxHealth", 1335f, 868f),
            ("Enemy_Tireur", "attackRange", 25f, 21f),
            ("Enemy_Liche", "attackRange", 28f, 24f),
            ("Boss_Liche", "attackRange", 30f, 26f),
        };

        static EnemyBalanceBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var log = new List<string>();
            UpdateNormal(log);
            UpdateWaves(log);
            UpdateDefinitions(log);
            if (log.Count > 0)
                Debug.Log("Équilibrage des ennemis (1er novembre) : " + string.Join(" ; ", log) + ".");
        }

        static void UpdateNormal(List<string> log)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Normal);
            if (asset == null)
                return;

            var serialized = new SerializedObject(asset);
            var changed = Replace(serialized, "healthGrowthPerWave", 1.19f, 1.15f, "Normal : PV ×1,15 par vague", log);
            changed |= Replace(serialized, "countGrowthPerWave", 1.12f, 1.09f, "nombre ×1,09 par vague", log);
            changed |= Replace(serialized, "damageGrowthPerWave", 1.09f, 1.07f, "dégâts ×1,07 par vague", log);
            Save(serialized, asset, changed);
        }

        static void UpdateWaves(List<string> log)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Waves);
            if (asset == null)
                return;

            var serialized = new SerializedObject(asset);
            var changed = ReplaceList(serialized, "healthPerWave", k_OldHealthCurve, k_NewHealthCurve, "courbe des PV adoucie", log);
            changed |= ReplaceList(serialized, "countPerWave", k_OldCountCurve, k_NewCountCurve, "courbe du nombre adoucie", log);

            var enemies = serialized.FindProperty("enemies");
            for (var i = 0; enemies != null && i < enemies.arraySize; i++)
            {
                var entry = enemies.GetArrayElementAtIndex(i);
                var prefab = entry.FindPropertyRelative("prefab").objectReferenceValue;
                var name = prefab != null ? System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(prefab)) : "";
                foreach (var flyer in k_Flyers)
                {
                    if (flyer.Prefab == name)
                        changed |= Replace(entry, "weight", flyer.Old, flyer.New, name.Replace("Enemy_", "") + " moins souvent", log);
                }
            }

            Save(serialized, asset, changed);
        }

        static void UpdateDefinitions(List<string> log)
        {
            foreach (var definition in k_Definitions)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Enemies + definition.Asset + ".asset");
                if (asset == null)
                    continue;

                var serialized = new SerializedObject(asset);
                var label = definition.Asset.Replace("Enemy_", "").Replace("Boss_", "boss ") + " : " +
                            (definition.Field == "maxHealth" ? definition.New + " PV" : "portée " + definition.New + " m");
                Save(serialized, asset, Replace(serialized, definition.Field, definition.Old, definition.New, label, log));
            }
        }

        // Une valeur, si elle a encore l'ancienne.
        static bool Replace(SerializedObject serialized, string field, float old, float value, string label, List<string> log)
        {
            var property = serialized.FindProperty(field);
            return property != null && Replace(property, old, value, label, log);
        }

        static bool Replace(SerializedProperty parent, string field, float old, float value, string label, List<string> log)
        {
            var property = parent.FindPropertyRelative(field);
            return property != null && Replace(property, old, value, label, log);
        }

        static bool Replace(SerializedProperty property, float old, float value, string label, List<string> log)
        {
            if (!Mathf.Approximately(property.floatValue, old))
                return false;
            property.floatValue = value;
            log.Add(label);
            return true;
        }

        // Une liste de valeurs, si elle a encore toutes les anciennes.
        static bool ReplaceList(SerializedObject serialized, string field, float[] old, float[] values, string label, List<string> log)
        {
            var property = serialized.FindProperty(field);
            if (property == null || !property.isArray || property.arraySize != old.Length)
                return false;
            for (var i = 0; i < old.Length; i++)
            {
                if (!Mathf.Approximately(property.GetArrayElementAtIndex(i).floatValue, old[i]))
                    return false;
            }

            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).floatValue = values[i];
            log.Add(label);
            return true;
        }

        static void Save(SerializedObject serialized, Object asset, bool changed)
        {
            if (!changed)
                return;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
