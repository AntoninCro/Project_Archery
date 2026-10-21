using Archery.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Réglages des cristaux demandés après les tests. Le 17 octobre : plus de deuxième série de gisements au milieu de la
    /// vague, mais chaque gisement donne 2,5 fois plus de matériaux (en tout, un quart de plus par vague) : 50 + 15 par vague
    /// au lieu de 20 + 6. Le 20 octobre : 20 % de matériaux en moins, 40 + 12 par vague. Le Crystal Spawner est dans la
    /// scène : à l'ouverture, s'il a encore d'anciennes valeurs, elles sont changées et la scène est marquée comme
    /// modifiée ; il suffit de l'enregistrer (Ctrl+S).
    /// </summary>
    [InitializeOnLoad]
    static class CrystalTuningBaker
    {
        const int k_OldBaseAmount = 20;
        const int k_OldAmountPerWave = 6;
        const int k_SeventeenthBaseAmount = 50;
        const int k_SeventeenthAmountPerWave = 15;
        const int k_BaseAmount = 40;
        const int k_AmountPerWave = 12;

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
            if (baseAmount == null || amountPerWave == null || secondBatch == null)
                return;

            var oldBase = baseAmount.intValue;
            var oldPerWave = amountPerWave.intValue;
            if (oldBase == k_OldBaseAmount && oldPerWave == k_OldAmountPerWave)
                secondBatch.boolValue = false;
            else if (oldBase != k_SeventeenthBaseAmount || oldPerWave != k_SeventeenthAmountPerWave)
                return;

            baseAmount.intValue = k_BaseAmount;
            amountPerWave.intValue = k_AmountPerWave;
            settings.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Cristaux : {k_BaseAmount} matériaux par gisement à la vague 1 (+{k_AmountPerWave} par vague) au lieu de " +
                      $"{oldBase} (+{oldPerWave}). Enregistre la scène (Ctrl+S).", spawner);
        }
    }
}
