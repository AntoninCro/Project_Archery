using Archery.Economy;
using Archery.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// L'ennemi préparé (lot 3 de la refonte du tower defense, 30 octobre) : dans la scène ouverte, donne au Score Manager un
    /// son pour chaque sorte de préparation (« Au vol ! », « Brisé ! »…), pris dans les sons de Kenney déjà dans le projet
    /// (CC0). Une fois, tant qu'aucune sorte n'a de son : ceux que le joueur change ou retire ensuite restent comme il les a
    /// laissés. La scène est marquée comme modifiée : il suffit de l'enregistrer (Ctrl+S).
    /// </summary>
    [InitializeOnLoad]
    static class PreparedShotBaker
    {
        // Une famille de petits sons d'interface, un par sorte, pour qu'on apprenne vite à reconnaître un tir préparé.
        static readonly (Preparation Kind, string Path)[] k_Clips =
        {
            (Preparation.Airborne, "Assets/kenney_interfaceSounds/Audio/maximize_006.ogg"),
            (Preparation.Frozen, "Assets/kenney_interfaceSounds/Audio/glass_002.ogg"),
            (Preparation.Pinned, "Assets/kenney_rpg-audio/Audio/metalLatch.ogg"),
            (Preparation.Netted, "Assets/kenney_interfaceSounds/Audio/pluck_002.ogg"),
            (Preparation.Stunned, "Assets/kenney_interfaceSounds/Audio/bong_001.ogg"),
            (Preparation.Glued, "Assets/kenney_impact-sounds/Audio/impactSoft_heavy_001.ogg"),
            (Preparation.Pulled, "Assets/kenney_interfaceSounds/Audio/glitch_002.ogg"),
            (Preparation.Marked, "Assets/kenney_interfaceSounds/Audio/confirmation_002.ogg"),
        };

        static PreparedShotBaker()
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
                    Apply(scene);
            }
        }

        static void Apply(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var score in root.GetComponentsInChildren<ScoreManager>(true))
                {
                    var settings = new SerializedObject(score);
                    var cues = settings.FindProperty("m_PreparedCues");
                    if (cues == null || !cues.isArray || HasAnyClip(cues))
                        continue;

                    var given = 0;
                    for (var c = 0; c < cues.arraySize; c++)
                    {
                        var cue = cues.GetArrayElementAtIndex(c);
                        var path = PathFor((Preparation)cue.FindPropertyRelative("Kind").intValue);
                        if (path == null)
                            continue;

                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        if (clip == null)
                        {
                            Debug.LogWarning($"Ennemi préparé : son introuvable ({path}).");
                            continue;
                        }

                        cue.FindPropertyRelative("Clip").objectReferenceValue = clip;
                        given++;
                    }

                    if (given == 0)
                        continue;

                    settings.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"Ennemi préparé : {given} sons donnés au Score Manager, dans la scène « {scene.name} ». " +
                              "Enregistre-la (Ctrl+S).", score);
                }
            }
        }

        static bool HasAnyClip(SerializedProperty cues)
        {
            for (var c = 0; c < cues.arraySize; c++)
            {
                if (cues.GetArrayElementAtIndex(c).FindPropertyRelative("Clip").objectReferenceValue != null)
                    return true;
            }

            return false;
        }

        static string PathFor(Preparation kind)
        {
            foreach (var (clipKind, path) in k_Clips)
            {
                if (clipKind == kind)
                    return path;
            }

            return null;
        }
    }
}
