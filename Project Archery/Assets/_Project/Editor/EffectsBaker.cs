using Archery.FinalBoss;
using Archery.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// L'objet « Effet » (toutes les lumières ajoutées) de la carte et de l'arène du dragon : à l'ouverture de la scène,
    /// s'il n'y est pas encore, il est fabriqué et la scène est marquée comme modifiée ; il suffit de l'enregistrer.
    /// Ensuite, on peut déplacer, modifier ou supprimer chaque lumière, ou tout cacher en cachant « Effet ».
    /// </summary>
    /// <remarks>
    /// Menu <b>Archery > Effets > Refaire les effets des scènes ouvertes</b> : supprime l'objet « Effet » des scènes ouvertes
    /// et le refait (après un changement de la carte, de l'arène ou des réglages « Resources/MapLighting »). Les
    /// modifications faites à la main sont alors perdues.
    /// </remarks>
    [InitializeOnLoad]
    static class EffectsBaker
    {
        static EffectsBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
        }

        // Après le chargement : les arènes et la carte ont fini de se construire (leurs colliders servent à placer les lumières).
        static void Schedule()
        {
            EditorApplication.delayCall -= BakeMissing;
            EditorApplication.delayCall += BakeMissing;
        }

        static void BakeMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    Bake(scene, false);
            }
        }

        [MenuItem("Archery/Effets/Refaire les effets des scènes ouvertes")]
        static void RebuildOpenScenes()
        {
            if (!EditorUtility.DisplayDialog("Refaire les effets",
                    "L'objet « Effet » des scènes ouvertes va être supprimé puis refait : les lumières déplacées ou modifiées " +
                    "à la main reviendront à leur place et à leurs réglages d'origine.", "Refaire", "Annuler"))
                return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    Bake(scene, true);
            }
        }

        static void Bake(Scene scene, bool rebuild)
        {
            if (Has<SkyController>(scene))
            {
                var existing = MapLighting.FindIn(scene);
                if (existing == null || rebuild)
                {
                    if (existing != null)
                        Undo.DestroyObjectImmediate(existing.gameObject);
                    Report(scene, MapLighting.Bake(scene) is { } created ? created.gameObject : null);
                }
            }

            if (Has<ArenaDirector>(scene))
            {
                var existing = ArenaLights.FindIn(scene);
                if (existing == null || rebuild)
                {
                    if (existing != null)
                        Undo.DestroyObjectImmediate(existing.gameObject);
                    Report(scene, ArenaLights.Bake(scene) is { } created ? created.gameObject : null);
                }
            }
        }

        static void Report(Scene scene, GameObject created)
        {
            if (created == null)
                return;

            Undo.RegisterCreatedObjectUndo(created, "Effets");
            EditorSceneManager.MarkSceneDirty(scene);
            var lights = created.GetComponentsInChildren<Light>(true).Length;
            var objects = created.GetComponentsInChildren<Transform>(true).Length - 1;
            Debug.Log($"Effets : objet « {created.name} » créé dans la scène {scene.name} ({objects} objets, dont {lights} lumières). " +
                      "Enregistre la scène (Ctrl+S) pour le garder.", created);
        }

        static bool Has<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<T>(true) != null)
                    return true;
            }

            return false;
        }
    }
}
