using System;
using Archery.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le nom du jeu (<see cref="GameInfo.Name"/>) dans les scènes : à l'ouverture d'une scène, les textes qui portent
    /// encore un ancien nom (le titre du menu) prennent le nouveau, et la scène est marquée comme modifiée ; il suffit
    /// de l'enregistrer. Le nom du produit (Player Settings) le suit aussi.
    /// </summary>
    [InitializeOnLoad]
    static class GameNameBaker
    {
        static readonly string[] k_OldNames = { "VR Archery Challenge", "SLACK VR" };

        static GameNameBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
            EditorApplication.delayCall += SyncProductName;
        }

        // Le nom du produit donne le titre de la fenêtre du build et le dossier des sauvegardes
        // (%USERPROFILE%\AppData\LocalLow\<Company Name>\<Product Name>).
        static void SyncProductName()
        {
            if (PlayerSettings.productName == GameInfo.Name)
                return;

            var previous = PlayerSettings.productName;
            PlayerSettings.productName = GameInfo.Name;
            AssetDatabase.SaveAssets();
            Debug.Log($"Nom du produit (Player Settings) : « {previous} » devient « {GameInfo.Name} ».");
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= RenameInOpenScenes;
            EditorApplication.delayCall += RenameInOpenScenes;
        }

        static void RenameInOpenScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    Rename(scene);
            }
        }

        static void Rename(Scene scene)
        {
            var renamed = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.text == null || Array.IndexOf(k_OldNames, text.text.Trim()) < 0)
                        continue;

                    Undo.RecordObject(text, "Nom du jeu");
                    text.text = GameInfo.Name;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                    EditorUtility.SetDirty(text);
                    renamed++;
                }
            }

            if (renamed == 0)
                return;

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Nom du jeu : « {GameInfo.Name} » mis dans {renamed} texte(s) de la scène « {scene.name} ». Enregistre-la (Ctrl+S).");
        }
    }
}
