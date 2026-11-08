using System.Collections.Generic;
using System.IO;
using Archery.Difficulty;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// La vitesse au sol (4 novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.66) : le joueur, « les ennemis au sol ne sont pas une
    /// vraie menace, ils se font détruire par tout ; plus de PV en ferait des sacs à PV : plus de vitesse à la place, +30 % dans toutes
    /// les difficultés sauf Facile ». Pose <c>groundSpeed</c> (nouveau réglage de <see cref="DifficultyDefinition"/>) : 1,3 en Normal,
    /// Difficile et Impossible, 1 en Facile.
    /// </summary>
    /// <remarks>
    /// Une seule fois par difficulté : seulement si le fichier de l'asset n'a pas encore ce réglage (ensuite, la valeur est celle du
    /// joueur, même s'il la remet à 1).
    /// </remarks>
    [InitializeOnLoad]
    static class GroundSpeedBaker
    {
        const string k_Folder = "Assets/_Project/Data/Difficulties";
        const float k_Faster = 1.3f;

        static GroundSpeedBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var log = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:DifficultyDefinition", new[] { k_Folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var file = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", path);
                if (!File.Exists(file) || File.ReadAllText(file).Contains("groundSpeed:"))
                    continue;

                var difficulty = AssetDatabase.LoadAssetAtPath<DifficultyDefinition>(path);
                if (difficulty == null)
                    continue;

                var easy = Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Contains("facile");
                var serialized = new SerializedObject(difficulty);
                var property = serialized.FindProperty("groundSpeed");
                if (property == null)
                    continue;

                property.floatValue = easy ? 1f : k_Faster;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(difficulty);
                AssetDatabase.SaveAssetIfDirty(difficulty);
                log.Add($"{difficulty.displayName} : {(easy ? "1 (inchangée)" : "×1,3")}");
            }

            if (log.Count > 0)
                Debug.Log("La vitesse au sol des ennemis (4 novembre ; Ground Speed des difficultés) : " + string.Join(" ; ", log) + ".");
        }
    }
}
