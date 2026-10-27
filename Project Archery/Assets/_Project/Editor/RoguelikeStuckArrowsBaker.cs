using Archery.Bows;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// 100 flèches plantées au plus dans la réserve de flèches (27 octobre, réglage du joueur ; 40 avant : avec la corde de pluie,
    /// les flèches plantées, donc les sentinelles et les pieux, disparaissaient au bout d'un ou deux tirs). Sur la scène ouverte,
    /// une seule fois : tant que <c>Arrow Pool</c> a encore l'ancienne valeur, 40 ; une autre valeur, réglée à la main, ne change
    /// pas. La scène est alors à enregistrer (Ctrl+S).
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikeStuckArrowsBaker
    {
        const int k_OldMax = 40;
        const int k_NewMax = 100;

        static RoguelikeStuckArrowsBaker()
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

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    UpdateScene(scene);
            }
        }

        static void UpdateScene(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var pool in root.GetComponentsInChildren<ArrowPool>(true))
                {
                    var serialized = new SerializedObject(pool);
                    var max = serialized.FindProperty("m_MaxStuckArrows");
                    if (max == null || max.intValue != k_OldMax)
                        continue;

                    max.intValue = k_NewMax;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"Réglages roguelike du 27 octobre (scène {scene.name}) : {k_NewMax} flèches plantées au plus dans la réserve " +
                              $"({k_OldMax} avant). Enregistre la scène (Ctrl+S).", pool);
                }
            }
        }
    }
}
