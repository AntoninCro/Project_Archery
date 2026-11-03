using System.Collections.Generic;
using Archery.Menus;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Après le deuxième essai du tower defense (2 novembre, la sœur du joueur ; <c>Docs/Refonte_TowerDefense.md</c>, 12.42) : les
    /// vagues trop faciles, boss compris, avec un arc ordinaire ; des tours un peu faibles ; des vagues trop longues maintenant que
    /// les coffres sont d'un seul côté. Chaque réglage n'est changé que s'il a encore sa valeur d'avant : une seule fois, et jamais
    /// un réglage retouché à la main.
    /// <list type="bullet">
    /// <item>la courbe des PV vague par vague (<c>Data/Waves/WaveSettings</c>, toutes les difficultés sauf le Facile ; pas MK, que
    /// la vague du boss final ne suit pas) : ×0,8 / 1,03 / 1,42 / 1,62 / 1,8 → ×0,85 / 1,29 / 1,99 / 2,51 / 3,06 aux vagues 1 à 5
    /// (+6 %, +25 %, +40 %, +55 %, +70 % ; à peu près la courbe d'avant le 1er novembre), boss des vagues compris ;</item>
    /// <item>le chrono : 90 s à la vague 1 (120), +12 s par vague (15), l'accalmie 35 % du chrono (40 %) : en Normal, l'accalmie de la
    /// vague 1 passe de 48 à 32 s et sa montée de 36 à 29 s (vague 5 : 72 → 48 s, 54 → 45 s) ;</item>
    /// <item>la puissance des tours ×1,2 : Normal 0,5 → 0,6, Difficile 0,81 → 0,97, Impossible 1,19 → 1,43 (le Facile garde 0,5) ;</item>
    /// <item>la fenêtre des coffres du tutoriel (« Ouvrir les caisses de l'ingénieur », d'avant le coffre unique).</item>
    /// </list>
    /// Le reste de l'essai est dans le code : 2 plans par coffre (<c>BuildManager</c>), la grille (<c>BuildGrid</c>).
    /// </summary>
    [InitializeOnLoad]
    static class SecondTestBaker
    {
        const string k_Waves = "Assets/_Project/Data/Waves/WaveSettings.asset";
        const string k_Difficulties = "Assets/_Project/Data/Difficulties/";
        const string k_Tutorial = "Assets/_Project/Resources/TutorialContent.asset";
        const string k_OldChestTitle = "Ouvrir les caisses de l'ingénieur";

        static readonly float[] k_OldHealthCurve = { 0.8f, 1.03f, 1.42f, 1.62f, 1.8f };
        static readonly float[] k_NewHealthCurve = { 0.85f, 1.29f, 1.99f, 2.51f, 3.06f };

        static readonly (string Asset, float Old, float New)[] k_Powers =
        {
            ("Difficulty_Normal", 0.5f, 0.6f),
            ("Difficulty_Difficile", 0.81f, 0.97f),
            ("Difficulty_Impossible", 1.19f, 1.43f),
        };

        static SecondTestBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var log = new List<string>();
            UpdateWaves(log);
            UpdatePowers(log);
            UpdateTutorial(log);
            if (log.Count > 0)
                Debug.Log($"Deuxième essai du tower defense (2 novembre) : {log.Count} réglage(s) changé(s) :\n" + string.Join("\n", log));
        }

        static void UpdateWaves(List<string> log)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Waves);
            if (asset == null)
                return;

            var serialized = new SerializedObject(asset);
            var changed = ReplaceList(serialized, "healthPerWave", k_OldHealthCurve, k_NewHealthCurve,
                                      "WaveSettings.healthPerWave : ×0,85 / 1,29 / 1,99 / 2,51 / 3,06 (vagues plus dures)", log);
            changed |= Replace(serialized, "firstWaveDuration", 120f, 90f, "WaveSettings.firstWaveDuration : 120 → 90 s", log);
            changed |= Replace(serialized, "durationIncrease", 15f, 12f, "WaveSettings.durationIncrease : 15 → 12 s", log);
            changed |= Replace(serialized, "calmFraction", 0.4f, 0.35f, "WaveSettings.calmFraction : 0,4 → 0,35 (accalmie plus courte)", log);
            Save(serialized, asset, changed);
        }

        static void UpdatePowers(List<string> log)
        {
            foreach (var power in k_Powers)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Difficulties + power.Asset + ".asset");
                if (asset == null)
                    continue;

                var serialized = new SerializedObject(asset);
                var label = $"{power.Asset.Replace("Difficulty_", "")}.structurePower : {power.Old:0.##} → {power.New:0.##}";
                Save(serialized, asset, Replace(serialized, "structurePower", power.Old, power.New, label, log));
            }
        }

        // La fenêtre des caisses de l'ingénieur (TowerDefenseStep2Baker, 27 octobre) devient celle du coffre unique.
        static void UpdateTutorial(List<string> log)
        {
            var content = AssetDatabase.LoadAssetAtPath<TutorialContent>(k_Tutorial);
            if (content == null)
                return;

            var changed = false;
            foreach (var section in content.sections)
            {
                if (section == null)
                    continue;

                foreach (var window in section.windows)
                {
                    if (window == null || window.title != k_OldChestTitle)
                        continue;

                    window.title = TutorialContent.ChestTitle;
                    window.text = TutorialContent.ChestText;
                    changed = true;
                }
            }

            if (!changed)
                return;

            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssetIfDirty(content);
            log.Add($"Tutoriel : « {k_OldChestTitle} » → « {TutorialContent.ChestTitle} »");
        }

        // Une valeur, si elle a encore l'ancienne.
        static bool Replace(SerializedObject serialized, string field, float old, float value, string label, List<string> log)
        {
            var property = serialized.FindProperty(field);
            if (property == null || !Mathf.Approximately(property.floatValue, old))
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
