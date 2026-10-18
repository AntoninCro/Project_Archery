using Archery.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Réglage des cristaux demandé après les tests (17 octobre) : plus de deuxième série de gisements au milieu de la vague,
    /// mais chaque gisement donne 2,5 fois plus de matériaux (en tout, un quart de plus par vague). Le Crystal Spawner est
    /// dans la scène : à l'ouverture, s'il a encore les anciennes valeurs (20 + 6 par vague), elles sont changées et la
    /// scène est marquée comme modifiée ; il suffit de l'enregistrer (Ctrl+S).
    /// </summary>
    [InitializeOnLoad]
    static class CrystalTuningBaker
    {
        const int k_OldBaseAmount = 20;
        const int k_OldAmountPerWave = 6;
        const int k_BaseAmount = 50;
        const int k_AmountPerWave = 15;

        static CrystalTuningBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= TuneOpenScenes;
            EditorApplication.delayCall += TuneOpenScenes;
        }

        static void TuneOpenScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var spawner in root.GetComponentsInChildren<CrystalSpawner>(true))
                        Tune(scene, spawner);
                }
            }
        }

        static void Tune(Scene scene, CrystalSpawner spawner)
        {
            var settings = new SerializedObject(spawner);
            var baseAmount = settings.FindProperty("m_BaseAmount");
            var amountPerWave = settings.FindProperty("m_AmountPerWave");
            var secondBatch = settings.FindProperty("m_SecondBatch");
            if (baseAmount == null || amountPerWave == null || secondBatch == null ||
                baseAmount.intValue != k_OldBaseAmount || amountPerWave.intValue != k_OldAmountPerWave)
                return;

            baseAmount.intValue = k_BaseAmount;
            amountPerWave.intValue = k_AmountPerWave;
            secondBatch.boolValue = false;
            settings.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Cristaux : plus de deuxième série au milieu de la vague, et {k_BaseAmount} matériaux par gisement à la vague 1 " +
                      $"(+{k_AmountPerWave} par vague) au lieu de {k_OldBaseAmount} (+{k_OldAmountPerWave}). Enregistre la scène (Ctrl+S).",
                      spawner);
        }
    }
}
