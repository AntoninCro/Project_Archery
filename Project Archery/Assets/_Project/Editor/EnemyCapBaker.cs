using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le plafond d'ennemis vivants en même temps passe de 40 à 150 (2 novembre, demandé par le joueur). Le maximum de chaque vague
    /// ne change pas (<c>WaveManager.MaxAlive</c> : 12 × le nombre d'ennemis de la difficulté × la racine carrée de la hausse du
    /// nombre d'ennemis) : en Normal, il touchait les 40 à la vague 13, il monte maintenant jusqu'à 150 à la vague 23, en mode infini
    /// (56 à la vague 15, 108 à la vague 20). Changé seulement si la valeur est encore l'ancienne : une seule fois, et jamais une
    /// valeur réglée à la main ensuite.
    /// </summary>
    [InitializeOnLoad]
    static class EnemyCapBaker
    {
        const string k_Asset = "Assets/_Project/Data/Waves/WaveSettings.asset";
        const int k_Old = 40;
        const int k_New = 150;

        static EnemyCapBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Asset);
            if (asset == null)
                return;

            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty("maxAliveCap");
            if (property == null || property.intValue != k_Old)
                return;

            property.intValue = k_New;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log($"Plafond d'ennemis en même temps (Wave Settings, Max Alive Cap) : {k_Old} → {k_New} (2 novembre).");
        }
    }
}
