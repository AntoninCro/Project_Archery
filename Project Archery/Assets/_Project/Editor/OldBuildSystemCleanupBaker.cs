using System.Collections.Generic;
using Archery.Building;
using Archery.Defense;
using Archery.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// L'ancien système de construction sort de la scène (28 octobre au soir, demandé par le joueur). Depuis la grille de
    /// construction (27 octobre), les 12 socles posés à la main (<c>Build Sockets</c>) et les 3 barricades de départ étaient
    /// seulement désactivés au lancement (<c>BuildGrid</c>) ; les cercles bleus des socles, eux (les runes de
    /// <see cref="MapGameplayLights"/>, enregistrées dans la scène sous <c>Jeu &gt; Runes</c>), restaient allumés au sol. Dans la
    /// scène ouverte, les socles sans bâtiment, leurs runes, le groupe <c>Build Sockets</c> s'il est vide et les barricades qui
    /// ne sont pas un bâtiment construit sont supprimés, avec l'annulation d'Unity (Ctrl+Z les rend tant que la scène n'est pas
    /// enregistrée). La scène est marquée comme modifiée : il suffit de l'enregistrer (Ctrl+S). Rien ne les référençait
    /// ailleurs dans la scène.
    /// </summary>
    [InitializeOnLoad]
    static class OldBuildSystemCleanupBaker
    {
        const string k_SocketGroup = "Build Sockets";
        const string k_Runes = "Runes";

        // Une rune est celle d'un socle si elle est à moins de cette distance (m) de lui (comme MapGameplayLights au lancement).
        const float k_RuneReach = 2.5f;

        static OldBuildSystemCleanupBaker()
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
            var sockets = new List<GameObject>();
            var socketPositions = new List<Vector3>();
            var barricades = new List<GameObject>();
            var runes = new List<GameObject>();
            var groups = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var socket in root.GetComponentsInChildren<BuildSocket>(true))
                {
                    if (socket.IsCandidate || socket.GetComponentInChildren<Structure>(true) != null)
                        continue;

                    var instance = Outermost(socket.gameObject);
                    if (!sockets.Contains(instance))
                    {
                        sockets.Add(instance);
                        socketPositions.Add(socket.transform.position);
                    }

                    var parent = instance.transform.parent;
                    if (parent != null && parent.name == k_SocketGroup && !groups.Contains(parent.gameObject))
                        groups.Add(parent.gameObject);
                }

                foreach (var barricade in root.GetComponentsInChildren<Barricade>(true))
                {
                    if (barricade.GetComponentInParent<Structure>(true) != null)
                        continue;

                    var instance = Outermost(barricade.gameObject);
                    if (!barricades.Contains(instance))
                        barricades.Add(instance);
                }
            }

            if (sockets.Count == 0 && barricades.Count == 0)
                return;

            // Les runes de ces socles : les cercles au sol, sous le groupe « Runes » des lumières de la carte.
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var lights in root.GetComponentsInChildren<MapGameplayLights>(true))
                {
                    var group = lights.transform.Find(k_Runes);
                    if (group == null)
                        continue;

                    foreach (Transform rune in group)
                    {
                        foreach (var position in socketPositions)
                        {
                            var offset = rune.position - position;
                            offset.y = 0f;
                            if (offset.magnitude > k_RuneReach)
                                continue;

                            runes.Add(rune.gameObject);
                            break;
                        }
                    }
                }
            }

            foreach (var go in runes)
                Undo.DestroyObjectImmediate(go);
            foreach (var go in sockets)
                Undo.DestroyObjectImmediate(go);
            foreach (var go in barricades)
                Undo.DestroyObjectImmediate(go);

            var removedGroups = 0;
            foreach (var group in groups)
            {
                if (group != null && group.transform.childCount == 0)
                {
                    Undo.DestroyObjectImmediate(group);
                    removedGroups++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Ancien système de construction : {sockets.Count} socle(s), {runes.Count} cercle(s) bleu(s) au sol, " +
                      $"{barricades.Count} barricade(s) de départ" + (removedGroups > 0 ? $" et le groupe « {k_SocketGroup} »" : "") +
                      $" supprimés de la scène « {scene.name} ». Enregistre-la (Ctrl+S) ; Ctrl+Z les rend avant.");
        }

        // L'objet à supprimer : la racine de l'instance de prefab (un socle, une barricade), ou l'objet lui-même.
        static GameObject Outermost(GameObject go)
        {
            var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            return root != null ? root : go;
        }
    }
}
