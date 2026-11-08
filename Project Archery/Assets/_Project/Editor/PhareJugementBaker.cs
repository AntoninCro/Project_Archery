using System.Collections.Generic;
using Archery.Building;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le 8 novembre, le joueur, après sa partie (le phare avait fait 32 % des dégâts de la partie et 49 % de ceux de MK : « il
    /// faudra effectivement nerf le phare ») ; <c>Docs/Refonte_TowerDefense.md</c>, 12.68. Le jugement (niveau 5) ne fait plus de
    /// dégâts, il étourdit et prépare sa cible (le code, <c>StructureLighthouse</c>) ; ici, ce qui est réglé sur le prefab :
    /// <list type="bullet">
    /// <item>le bonus des flèches sur les points faibles qui brillent : 20 / 35 / 50 / 50 / 60 % → 20 / 25 / 30 / 30 / 30 % ;</item>
    /// <item>le texte du niveau 5 (<c>Data/Structures/Phare.asset</c>), qui disait le rayon sans dire ce qu'il fait.</item>
    /// </list>
    /// Chaque réglage n'est changé que s'il a encore sa valeur d'avant : une seule fois, et jamais un réglage retouché à la main.
    /// Il passe un tour d'éditeur après les autres, comme <c>UltimesTowerBaker</c> (le baker de l'allure refait les prefabs au
    /// premier tour).
    /// </summary>
    [InitializeOnLoad]
    static class PhareJugementBaker
    {
        const string k_Prefab = "Assets/_Project/Prefabs/Structures/Phare.prefab";
        const string k_Definition = "Assets/_Project/Data/Structures/Phare.asset";
        static readonly float[] k_BonusOld = { 0.2f, 0.35f, 0.5f, 0.5f, 0.6f };
        static readonly float[] k_BonusNew = { 0.2f, 0.25f, 0.3f, 0.3f, 0.3f };
        const string k_JudgmentOld = "Phare du jugement : toutes les 10 s, un rayon géant tombe du ciel sur l'ennemi le plus fort, et tous " +
                                     "les bâtiments du rayon tirent deux fois plus vite pendant 5 s.";
        const string k_JudgmentNew = "Phare du jugement : toutes les 10 s, un rayon géant tombe du ciel sur l'ennemi le plus fort : il est " +
                                     "étourdi 3 s (un boss 1,5 s), et tes flèches lui font plus de dégâts ; les bâtiments du rayon tirent " +
                                     "plus vite pendant 5 s.";

        static PhareJugementBaker()
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

            var log = new List<string>();
            UpdatePrefab(log);
            UpdateDefinition(log);
            if (log.Count > 0)
                Debug.Log("Le phare du jugement (8 novembre) : " + string.Join(" ; ", log) + ".");
        }

        static void UpdatePrefab(List<string> log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(k_Prefab) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(k_Prefab);
            try
            {
                var phare = root.GetComponent<StructureLighthouse>();
                if (phare == null)
                    return;

                var serialized = new SerializedObject(phare);
                var bonus = serialized.FindProperty("m_BonusPerLevel");
                if (bonus == null || !bonus.isArray || bonus.arraySize != k_BonusOld.Length)
                    return;
                for (var i = 0; i < k_BonusOld.Length; i++)
                {
                    if (!Mathf.Approximately(bonus.GetArrayElementAtIndex(i).floatValue, k_BonusOld[i]))
                        return;
                }

                for (var i = 0; i < k_BonusNew.Length; i++)
                    bonus.GetArrayElementAtIndex(i).floatValue = k_BonusNew[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, k_Prefab);
                log.Add("bonus sur les points faibles 20/35/50/50/60 % → 20/25/30/30/30 %");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void UpdateDefinition(List<string> log)
        {
            var definition = AssetDatabase.LoadAssetAtPath<StructureDefinition>(k_Definition);
            if (definition == null)
                return;

            var serialized = new SerializedObject(definition);
            var levels = serialized.FindProperty("levels");
            if (levels == null || !levels.isArray || levels.arraySize < 5)
                return;

            var description = levels.GetArrayElementAtIndex(4).FindPropertyRelative("description");
            if (description == null || description.stringValue.Replace("\n", " ").Trim() != k_JudgmentOld)
                return;

            description.stringValue = k_JudgmentNew;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            log.Add("texte du niveau 5");
        }
    }
}
