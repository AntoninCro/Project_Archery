using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les dégâts des bâtiments baissés de 20 % (1er novembre au soir, demandé par le joueur : avec les seules tourelles de la
    /// vitrine, F4, il est allé jusqu'à la vague 45). Avec le scaling revu le même soir (les bâtiments ne suivent plus que la racine
    /// carrée de la force des ennemis : <c>Structure.Strength</c>), la <i>Structure Power</i> de chaque difficulté, qui multiplie les
    /// dégâts de tous les bâtiments, duos et trios compris, passe de 0,75 / 1 / 1,25 / 1,6 à 0,6 / 0,8 / 1 / 1,28 (Facile, Normal,
    /// Difficile, Impossible). Chaque valeur n'est changée que si elle a encore l'ancienne : une seule fois, et jamais une valeur
    /// réglée à la main ensuite.
    /// </summary>
    [InitializeOnLoad]
    static class StructurePowerBaker
    {
        const string k_Folder = "Assets/_Project/Data/Difficulties/";

        static readonly (string Asset, float Old, float New)[] k_Powers =
        {
            ("Difficulty_facile", 0.75f, 0.6f),
            ("Difficulty_Normal", 1f, 0.8f),
            ("Difficulty_Difficile", 1.25f, 1f),
            ("Difficulty_Impossible", 1.6f, 1.28f),
        };

        static StructurePowerBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var log = new List<string>();
            foreach (var power in k_Powers)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Folder + power.Asset + ".asset");
                if (asset == null)
                    continue;

                var serialized = new SerializedObject(asset);
                var property = serialized.FindProperty("structurePower");
                if (property == null || !Mathf.Approximately(property.floatValue, power.Old))
                    continue;

                property.floatValue = power.New;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
                log.Add(power.Asset.Replace("Difficulty_", "") + " " + power.New);
            }

            if (log.Count > 0)
                Debug.Log("Dégâts des bâtiments −20 % (1er novembre au soir), Structure Power : " + string.Join(", ", log) + ".");
        }
    }
}
