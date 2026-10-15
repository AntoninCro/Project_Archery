using System.Collections.Generic;
using Archery.Defense;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.World
{
    /// <summary>
    /// Ce que les lumières de la carte savent du terrain, trouvé une fois au lancement, sans rien poser dans la scène :
    /// les arbres, les champignons et les fleurs (par le nom de leur modèle), le chemin des ennemis sur chaque voie (sur
    /// le NavMesh, de leur point d'apparition à la tour), et des points au sol dans la forêt, loin des voies.
    /// </summary>
    public class MapScan
    {
        /// <summary>Le chemin des ennemis d'une voie : une ligne brisée, de leur point d'apparition à la tour.</summary>
        public class Lane
        {
            public Transform Spawn;
            public readonly List<Vector3> Points = new List<Vector3>();
            public readonly List<float> Distances = new List<float>();

            public float Length => Distances.Count > 0 ? Distances[Distances.Count - 1] : 0f;

            /// <summary>Le point à cette distance du départ, et le sens du chemin à cet endroit.</summary>
            public Vector3 PointAt(float distance, out Vector3 direction)
            {
                direction = Vector3.forward;
                if (Points.Count == 0)
                    return Vector3.zero;

                for (var i = 1; i < Points.Count; i++)
                {
                    if (distance > Distances[i] && i < Points.Count - 1)
                        continue;

                    var segment = Points[i] - Points[i - 1];
                    var length = Mathf.Max(1e-4f, Distances[i] - Distances[i - 1]);
                    direction = segment.sqrMagnitude > 1e-6f ? segment.normalized : Vector3.forward;
                    return Points[i - 1] + segment * Mathf.Clamp01((distance - Distances[i - 1]) / length);
                }

                return Points[0];
            }

            /// <summary>Distance (horizontale) de ce point au chemin, et distance le long du chemin du point le plus proche.</summary>
            public float DistanceTo(Vector3 point, out float along)
            {
                along = 0f;
                var best = float.MaxValue;
                for (var i = 1; i < Points.Count; i++)
                {
                    var a = Points[i - 1];
                    var b = Points[i];
                    var segment = new Vector3(b.x - a.x, 0f, b.z - a.z);
                    var offset = new Vector3(point.x - a.x, 0f, point.z - a.z);
                    var t = Mathf.Clamp01(Vector3.Dot(offset, segment) / Mathf.Max(1e-4f, segment.sqrMagnitude));
                    var distance = (offset - segment * t).magnitude;
                    if (distance >= best)
                        continue;

                    best = distance;
                    along = Mathf.Lerp(Distances[i - 1], Distances[i], t);
                }

                return best;
            }
        }

        public readonly List<Transform> Trees = new List<Transform>();
        public readonly List<Transform> Mushrooms = new List<Transform>();
        public readonly List<Transform> Flowers = new List<Transform>();
        public readonly List<Lane> Lanes = new List<Lane>();

        /// <summary>Des points au sol dans la forêt, au pied des arbres, loin des voies, de l'eau et de la tour.</summary>
        public readonly List<Vector3> ForestPoints = new List<Vector3>();

        // Au-dessus de cette hauteur, c'est l'arène du boss final (à 300 m) : on n'y touche pas.
        const float k_MapTop = 150f;

        public static MapScan Run()
        {
            var scan = new MapScan();
            scan.FindModels();
            scan.FindLanes();
            scan.FindForestPoints();
            return scan;
        }

        // Les modèles du kit Nature gardent le nom de leur fichier dans la scène (tree_oak, mushroom_red (3)…).
        void FindModels()
        {
            foreach (var transform in Object.FindObjectsByType<Transform>())
            {
                if (transform.position.y > k_MapTop)
                    continue;

                var name = transform.name;
                var parent = transform.parent;
                if (name.StartsWith("tree_") && !HasPrefix(parent, "tree_"))
                    Trees.Add(transform);
                else if (name.StartsWith("mushroom_") && !HasPrefix(parent, "mushroom_"))
                    Mushrooms.Add(transform);
                else if (name.StartsWith("flower_") && !HasPrefix(parent, "flower_"))
                    Flowers.Add(transform);
            }
        }

        static bool HasPrefix(Transform transform, string prefix) => transform != null && transform.name.StartsWith(prefix);

        // Le chemin des ennemis de chaque point d'apparition jusqu'à la tour, sur le NavMesh.
        void FindLanes()
        {
            var spawner = Object.FindAnyObjectByType<EnemySpawner>();
            var tower = Tower.Instance != null ? Tower.Instance : Object.FindAnyObjectByType<Tower>();
            if (spawner == null || tower == null)
                return;

            if (!NavMesh.SamplePosition(tower.transform.position, out var towerHit, 12f, NavMesh.AllAreas))
                return;

            var path = new NavMeshPath();
            foreach (var spawn in spawner.SpawnPoints)
            {
                if (spawn == null || !NavMesh.SamplePosition(spawn.position, out var spawnHit, 6f, NavMesh.AllAreas))
                    continue;
                if (!NavMesh.CalculatePath(spawnHit.position, towerHit.position, NavMesh.AllAreas, path) || path.corners.Length < 2)
                    continue;

                var lane = new Lane { Spawn = spawn };
                var total = 0f;
                for (var i = 0; i < path.corners.Length; i++)
                {
                    if (i > 0)
                        total += Vector3.Distance(path.corners[i - 1], path.corners[i]);
                    lane.Points.Add(path.corners[i]);
                    lane.Distances.Add(total);
                }

                Lanes.Add(lane);
            }
        }

        /// <summary>Distance horizontale de ce point à la voie la plus proche (l'infini s'il n'y a pas de voie).</summary>
        public float LaneDistance(Vector3 point)
        {
            var best = float.MaxValue;
            foreach (var lane in Lanes)
                best = Mathf.Min(best, lane.DistanceTo(point, out _));
            return best;
        }

        void FindForestPoints()
        {
            var random = new System.Random(11);
            var tower = Tower.Instance != null ? Tower.Instance.transform.position : Vector3.zero;
            foreach (var tree in Trees)
            {
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    var radius = 1.6f + (float)random.NextDouble() * 1.8f;
                    var around = tree.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
                    if (!TryGround(around, out var ground) || Mathf.Abs(ground.y - tree.position.y) > 1.5f)
                        continue;
                    if (LaneDistance(ground) < 5f || SlowWater.InAnyWater(ground + Vector3.up * 0.3f))
                        continue;
                    if (new Vector2(ground.x - tower.x, ground.z - tower.z).magnitude < 10f)
                        continue;

                    ForestPoints.Add(ground);
                }
            }
        }

        /// <summary>
        /// Le sol sous ce point : le dessus le plus haut d'un collider (pas un arbre ni une plante), assez plat. Faux s'il
        /// n'y a pas de sol à moins de 15 m.
        /// </summary>
        public static bool TryGround(Vector3 around, out Vector3 ground, float above = 6f)
        {
            ground = around;
            var hits = Physics.RaycastAll(around + Vector3.up * above, Vector3.down, above + 15f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.distance >= best || hit.normal.y < 0.6f || IsPlant(hit.collider.transform))
                    continue;
                best = hit.distance;
                ground = hit.point;
            }

            return best < float.MaxValue;
        }

        /// <summary>Un arbre, une herbe, une fleur, une plante ou un champignon du kit Nature (ou une de ses pièces).</summary>
        public static bool IsPlant(Transform transform)
        {
            for (var i = 0; i < 4 && transform != null; i++, transform = transform.parent)
            {
                var name = transform.name;
                if (name.StartsWith("tree_") || name.StartsWith("grass") || name.StartsWith("flower_") || name.StartsWith("plant_") ||
                    name.StartsWith("mushroom_"))
                    return true;
            }

            return false;
        }

        /// <summary>Des points pris dans une liste, espacés d'au moins <paramref name="spacing"/> m, au plus <paramref name="count"/>.</summary>
        public static List<Vector3> Spread(List<Vector3> points, int count, float spacing, int seed)
        {
            var picked = new List<Vector3>();
            if (count <= 0 || points.Count == 0)
                return picked;

            var random = new System.Random(seed);
            var order = new List<Vector3>(points);
            for (var i = order.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            foreach (var point in order)
            {
                var free = true;
                foreach (var other in picked)
                {
                    if ((other - point).sqrMagnitude < spacing * spacing)
                    {
                        free = false;
                        break;
                    }
                }

                if (!free)
                    continue;
                picked.Add(point);
                if (picked.Count >= count)
                    break;
            }

            return picked;
        }
    }
}
