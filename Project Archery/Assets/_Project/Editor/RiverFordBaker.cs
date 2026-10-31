using Archery.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le gué de droite (28 octobre au soir, trouvé en rendant la rivière constructible) : la zone qui ralentit les ennemis
    /// (<see cref="SlowWater"/> « Ford Right ») était restée en x = 25,5 quand la rivière a été retracée ; elle traverse
    /// maintenant la voie de droite en x = 19,5 (les pièces <c>ground_pathStraight_lane</c> de <c>Map &gt; River</c>). Les ennemis
    /// étaient ralentis à sec, 6 m après l'eau. Dans la scène ouverte, la zone est recentrée sur le milieu des pièces de passage
    /// voisines (sa hauteur et sa taille ne changent pas), une fois, seulement si elle est encore à son ancienne place : un
    /// réglage à la main n'est pas touché. La scène est marquée comme modifiée : il suffit de l'enregistrer (Ctrl+S).
    /// </summary>
    [InitializeOnLoad]
    static class RiverFordBaker
    {
        const string k_FordName = "Ford Right";
        const string k_CrossingPrefix = "ground_pathStraight_lane";

        // L'ancienne place de la zone, et la distance (m) où chercher les pièces de passage autour d'elle.
        static readonly Vector3 k_OldPosition = new Vector3(25.5f, -0.2f, 0f);
        const float k_SearchRadius = 9f;

        static RiverFordBaker()
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
                foreach (var water in root.GetComponentsInChildren<SlowWater>(true))
                {
                    var transform = water.transform;
                    if (water.name != k_FordName || (transform.position - k_OldPosition).sqrMagnitude > 0.01f)
                        continue;

                    if (!TryCrossing(scene, transform.position, out var center))
                    {
                        Debug.LogWarning($"Gué de droite : pas de pièce « {k_CrossingPrefix} » de la rivière à moins de {k_SearchRadius} m " +
                                         "de la zone qui ralentit : elle reste où elle est.", water);
                        continue;
                    }

                    var target = new Vector3(center.x, transform.position.y, center.z);
                    var local = transform.parent != null ? transform.parent.InverseTransformPoint(target) : target;
                    var settings = new SerializedObject(transform);
                    settings.FindProperty("m_LocalPosition").vector3Value = local;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"Gué de droite : la zone qui ralentit les ennemis passe de x = {k_OldPosition.x} à x = {target.x:0.##}, " +
                              $"sur la rivière (scène « {scene.name} »). Enregistre-la (Ctrl+S).", water);
                }
            }
        }

        // Le milieu des pièces de passage de la rivière (sous « River ») autour de ce point.
        static bool TryCrossing(Scene scene, Vector3 from, out Vector3 center)
        {
            var sum = Vector3.zero;
            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var piece in root.GetComponentsInChildren<Transform>(true))
                {
                    if (piece.parent == null || piece.parent.name != "River" ||
                        !piece.name.StartsWith(k_CrossingPrefix, System.StringComparison.Ordinal))
                        continue;

                    var offset = piece.position - from;
                    offset.y = 0f;
                    if (offset.magnitude > k_SearchRadius)
                        continue;

                    sum += piece.position;
                    count++;
                }
            }

            center = count > 0 ? sum / count : from;
            return count > 0;
        }
    }
}
