using System.Collections.Generic;
using System.Linq;
using Archery.Building;
using Archery.Defense;
using Archery.Training;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Replace les cibles d'entraînement (objet « Training ») dans la zone dégagée entre la voie du milieu et celle de
    /// droite, vues depuis la tour (17 octobre) : elles étaient sur la voie du milieu, contre une torche, au-dessus de
    /// la rivière ou dans le décor. Autour de chaque place prévue, il cherche un sol plat sans obstacle, loin des voies,
    /// de la rivière et des socles de construction.
    /// </summary>
    /// <remarks>
    /// À l'ouverture de la scène, une seule fois (tant que le groupe a son ancienne orientation, 45°) ; le menu
    /// Archery > Entraînement > Replacer les cibles recommence. La scène est alors marquée comme modifiée : il suffit
    /// de l'enregistrer (Ctrl+S).
    /// </remarks>
    [InitializeOnLoad]
    static class TrainingLayoutBaker
    {
        const string k_GroupName = "Training";
        const float k_OldYaw = 45f;

        // Entre la voie du milieu (45°) et celle de droite (90°), vues de la tour.
        const float k_MinYaw = 52f;
        const float k_MaxYaw = 83f;

        // Marges (m) à garder avec les voies, la rivière et les socles ; différence de hauteur de sol tolérée.
        const float k_LaneMargin = 2.5f;
        const float k_SocketMargin = 3f;
        const float k_GroundTolerance = 0.8f;
        const float k_SearchRadius = 5f;

        // Places prévues : distance à la tour (m) et décalage sur le côté (m, positif à droite en regardant depuis la tour).
        static readonly Vector2[] k_Targets = { new Vector2(16f, 0f), new Vector2(24f, -1.5f), new Vector2(33f, -4f) };
        static readonly Vector2 k_Mobile = new Vector2(28f, 3f);
        static readonly Vector2[] k_Dummies = { new Vector2(20f, -6.5f), new Vector2(36f, 5f) };

        static readonly Collider[] s_Overlaps = new Collider[32];
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        static TrainingLayoutBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= LayoutOpenScenes;
            EditorApplication.delayCall += LayoutOpenScenes;
        }

        static void LayoutOpenScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var group = scene.isLoaded ? FindGroup(scene) : null;
                if (group != null && Mathf.Abs(Mathf.DeltaAngle(group.eulerAngles.y, k_OldYaw)) < 0.5f)
                    Layout(scene, group);
            }
        }

        [MenuItem("Archery/Entraînement/Replacer les cibles")]
        static void LayoutFromMenu()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                var group = scene.isLoaded ? FindGroup(scene) : null;
                if (group != null)
                    Layout(scene, group);
            }
        }

        static Transform FindGroup(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == k_GroupName && root.GetComponentInChildren<TrainingTarget>(true) != null)
                    return root.transform;
            }

            return null;
        }

        static void Layout(Scene scene, Transform group)
        {
            Physics.SyncTransforms();
            var tower = Object.FindObjectsByType<Tower>(FindObjectsInactive.Include)
                .FirstOrDefault(t => t.gameObject.scene == scene);
            var origin = tower != null ? tower.transform.position : group.position;
            origin.y = group.position.y;

            var ignored = new HashSet<Collider>(group.GetComponentsInChildren<Collider>(true));
            var map = new Obstacles(scene, ignored);

            // L'axe le plus dégagé entre les deux voies.
            var bestYaw = (k_MinYaw + k_MaxYaw) * 0.5f;
            var bestScore = float.MinValue;
            for (var yaw = k_MinYaw; yaw <= k_MaxYaw + 0.01f; yaw += 1.5f)
            {
                var score = 0f;
                for (var distance = 16f; distance <= 36f; distance += 4f)
                    score += map.Score(At(origin, yaw, new Vector2(distance, 0f)), origin.y);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestYaw = yaw;
                }
            }

            // Les éléments du groupe : cibles fixes (de la plus proche à la plus loin), cible mobile, mannequins.
            var items = Enumerable.Range(0, group.childCount).Select(group.GetChild).ToList();
            var mobiles = items.Where(t => t.GetComponentInChildren<PingPongMover>(true) != null).ToList();
            var targets = items.Where(t => !mobiles.Contains(t) && t.GetComponentInChildren<TrainingTarget>(true) != null)
                .OrderBy(t => Vector3.Distance(t.position, origin)).ToList();
            var dummies = items.Where(t => !mobiles.Contains(t) && !targets.Contains(t))
                .OrderBy(t => Vector3.Distance(t.position, origin)).ToList();

            Undo.RecordObject(group, "Replacer les cibles");
            group.rotation = Quaternion.Euler(0f, bestYaw, 0f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(group);

            var log = new List<string>();
            for (var i = 0; i < targets.Count; i++)
            {
                var place = Place(targets[i], k_Targets[Mathf.Min(i, k_Targets.Length - 1)], origin, bestYaw, map, null);
                var distance = Mathf.RoundToInt(Vector3.Distance(Flat(place), Flat(origin)));
                Rename(targets[i], $"Target {distance} m");
                log.Add(targets[i].name);
            }

            foreach (var mobile in mobiles)
            {
                var mover = mobile.GetComponentInChildren<PingPongMover>(true);
                var offset = new SerializedObject(mover).FindProperty("m_Offset").vector3Value;
                Place(mobile, k_Mobile, origin, bestYaw, map, offset);
                log.Add($"{mobile.name} à {Vector3.Distance(Flat(mobile.position), Flat(origin)):0} m");
            }

            for (var i = 0; i < dummies.Count; i++)
            {
                Place(dummies[i], k_Dummies[Mathf.Min(i, k_Dummies.Length - 1)], origin, bestYaw, map, null);
                log.Add($"{dummies[i].name} à {Vector3.Distance(Flat(dummies[i].position), Flat(origin)):0} m");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Cibles d'entraînement replacées entre la voie du milieu et celle de droite (axe à {bestYaw:0}° depuis la tour) : " +
                      string.Join(", ", log) + ". Enregistre la scène (Ctrl+S).");
        }

        // Cherche, autour de la place prévue, le point valable le plus proche ; la cible regarde dans l'axe, comme avant.
        static Vector3 Place(Transform item, Vector2 slot, Vector3 origin, float yaw, Obstacles map, Vector3? moverOffset)
        {
            var wanted = At(origin, yaw, slot);
            var rotation = Quaternion.Euler(0f, yaw, 0f);
            var best = wanted;
            best.y = origin.y;
            var found = false;
            var candidates = new List<Vector3>();
            for (var x = -k_SearchRadius; x <= k_SearchRadius + 0.01f; x += 1f)
            {
                for (var z = -k_SearchRadius; z <= k_SearchRadius + 0.01f; z += 1f)
                {
                    if (x * x + z * z <= k_SearchRadius * k_SearchRadius)
                        candidates.Add(wanted + rotation * new Vector3(x, 0f, z));
                }
            }

            foreach (var candidate in candidates.OrderBy(c => (c - wanted).sqrMagnitude))
            {
                if (!map.IsFree(candidate, origin.y, out var ground))
                    continue;

                // La cible mobile va et vient (décalage en coordonnées du monde) : le bout de son trajet doit être libre aussi.
                if (moverOffset.HasValue && !map.IsFree(ground + moverOffset.Value, origin.y, out _))
                    continue;

                best = ground;
                found = true;
                break;
            }

            if (!found)
                Debug.LogWarning($"Cible d'entraînement « {item.name} » : pas de place dégagée trouvée près de la place prévue, " +
                                 "elle y est posée quand même.", item);

            Undo.RecordObject(item, "Replacer les cibles");
            item.SetPositionAndRotation(best, rotation);
            PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            return best;
        }

        static void Rename(Transform item, string name)
        {
            if (item.name == name)
                return;
            Undo.RecordObject(item.gameObject, "Replacer les cibles");
            item.name = name;
            PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject);
        }

        static Vector3 At(Vector3 origin, float yaw, Vector2 slot) =>
            origin + Quaternion.Euler(0f, yaw, 0f) * new Vector3(slot.y, 0f, slot.x);

        static Vector3 Flat(Vector3 point) => new Vector3(point.x, 0f, point.z);

        // Ce qu'il faut éviter : les voies et la rivière (leurs morceaux), les socles, et tout ce qui dépasse du sol.
        sealed class Obstacles
        {
            readonly List<Bounds> m_Lanes = new List<Bounds>();
            readonly List<Vector3> m_Sockets = new List<Vector3>();
            readonly HashSet<Collider> m_Ignored;

            public Obstacles(Scene scene, HashSet<Collider> ignored)
            {
                m_Ignored = ignored;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (transform.name != "Lanes" && transform.name != "River")
                            continue;

                        // Les morceaux de voie et de rivière ; un seul grand maillage (l'eau générée) ne dirait rien de leur forme.
                        foreach (var renderer in transform.GetComponentsInChildren<Renderer>(true))
                        {
                            var size = renderer.bounds.size;
                            if (size.x * size.z < 400f)
                                m_Lanes.Add(renderer.bounds);
                        }
                    }

                    foreach (var socket in root.GetComponentsInChildren<BuildSocket>(true))
                        m_Sockets.Add(socket.transform.position);
                }
            }

            /// <summary>Distance (m) au plus proche morceau de voie ou de rivière, ou socle (marges retirées).</summary>
            public float Clearance(Vector3 point)
            {
                var clearance = float.MaxValue;
                foreach (var bounds in m_Lanes)
                {
                    var closest = bounds.ClosestPoint(new Vector3(point.x, bounds.center.y, point.z));
                    clearance = Mathf.Min(clearance, Vector2.Distance(new Vector2(point.x, point.z), new Vector2(closest.x, closest.z)) - k_LaneMargin);
                }

                foreach (var socket in m_Sockets)
                    clearance = Mathf.Min(clearance, Vector2.Distance(new Vector2(point.x, point.z), new Vector2(socket.x, socket.z)) - k_SocketMargin);
                return clearance;
            }

            /// <summary>Un sol plat, à la hauteur de la tour, loin des voies et des socles, sans rien qui dépasse.</summary>
            public bool IsFree(Vector3 point, float baseHeight, out Vector3 ground)
            {
                ground = point;
                if (!TryGround(point, baseHeight, out var height) || Mathf.Abs(height - baseHeight) > k_GroundTolerance)
                    return false;
                ground.y = height;
                if (Clearance(ground) < 0f)
                    return false;

                var count = Physics.OverlapCapsuleNonAlloc(ground + Vector3.up * 0.5f, ground + Vector3.up * 2.2f, 0.9f, s_Overlaps,
                                                           Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                for (var i = 0; i < count; i++)
                {
                    if (!m_Ignored.Contains(s_Overlaps[i]))
                        return false;
                }

                return true;
            }

            /// <summary>Note d'un point de l'axe : plus il est dégagé, mieux c'est ; un point impossible compte en négatif.</summary>
            public float Score(Vector3 point, float baseHeight) =>
                IsFree(point, baseHeight, out var ground) ? Mathf.Min(Clearance(ground), 6f) : -6f;

            bool TryGround(Vector3 point, float baseHeight, out float height)
            {
                height = 0f;
                var top = new Vector3(point.x, baseHeight + 15f, point.z);
                var count = Physics.RaycastNonAlloc(top, Vector3.down, s_Hits, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                var best = float.MaxValue;
                for (var i = 0; i < count; i++)
                {
                    if (m_Ignored.Contains(s_Hits[i].collider) || s_Hits[i].distance >= best)
                        continue;
                    best = s_Hits[i].distance;
                    height = s_Hits[i].point.y;
                }

                return best < float.MaxValue;
            }
        }
    }
}
