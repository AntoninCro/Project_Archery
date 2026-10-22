using Archery.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Avant la première vague (22 octobre) : le grand panneau du score reste caché jusqu'au gong, et les panneaux de
    /// difficulté montent de 1,6 m (ils étaient à hauteur des yeux, devant le champ de bataille). Ils sont dans la scène :
    /// à l'ouverture, si le tableau des difficultés est encore à son ancienne hauteur, les deux sont changés et la scène
    /// est marquée comme modifiée ; il suffit de l'enregistrer (Ctrl+S).
    /// </summary>
    [InitializeOnLoad]
    static class PreGameLayoutBaker
    {
        const string k_BoardName = "Difficulty Board";
        const string k_ScoreboardName = "Scoreboard";
        const float k_OldBoardHeight = 5.8f;
        const float k_BoardHeight = 7.4f;

        static PreGameLayoutBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= ApplyToOpenScenes;
            EditorApplication.delayCall += ApplyToOpenScenes;
        }

        static void ApplyToOpenScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    Apply(scene);
            }
        }

        static void Apply(Scene scene)
        {
            Transform board = null;
            HudDisplay scoreboard = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == k_BoardName)
                    board = root.transform;
                else if (root.name == k_ScoreboardName)
                    scoreboard = root.GetComponent<HudDisplay>();
            }

            // Une seule fois : ensuite, la hauteur et la case restent celles qu'on choisit dans l'Inspector.
            if (board == null || Mathf.Abs(board.localPosition.y - k_OldBoardHeight) > 0.01f)
                return;

            var position = board.localPosition;
            position.y = k_BoardHeight;
            board.localPosition = position;

            if (scoreboard != null)
            {
                var settings = new SerializedObject(scoreboard);
                var hidden = settings.FindProperty("m_HiddenBeforeFirstWave");
                if (hidden != null)
                {
                    hidden.boolValue = true;
                    settings.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Avant la première vague : panneaux de difficulté montés à {k_BoardHeight} m (au lieu de " +
                      $"{k_OldBoardHeight} m), panneau du score caché jusqu'au gong. Enregistre la scène (Ctrl+S).", board);
        }
    }
}
