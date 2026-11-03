using System.Collections.Generic;
using System.Linq;
using Archery.Defense;
using Archery.Enemies;
using Archery.Training;
using Archery.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le centre d'entraînement et les torches revus par le joueur (2 novembre au soir ; <c>Docs/Refonte_TowerDefense.md</c>,
    /// 12.43) :
    /// <list type="bullet">
    /// <item>les cibles pour viser sont retirées (moins utiles que les mannequins, qui comptent comme des ennemis) ;</item>
    /// <item>5 mannequins en plein milieu de la voie du milieu : un près de la tour qui fait des droite-gauche, un groupe de 3 plus
    /// loin, et un dernier qui fait des droite-gauche plus loin encore (après le gué). Tournés vers la tour. Le doublon du
    /// « Mannequin groupe 1 » est retiré ;</item>
    /// <item>les torches sur pied des voies (posées à 3,3 m du chemin des ennemis, qui n'est pas au milieu de la voie : certaines
    /// étaient dans la voie, d'autres à 2 m de son bord) se collent au bord de la terre de la voie, juste dehors (dedans contre le
    /// bord si dehors n'est pas plat) ; les torches murales, déjà sur les parois, ne bougent pas ;</item>
    /// <item>le visage dessiné par le joueur (<c>Art/Training/Visage_Mannequin.png</c>, traits noirs) sur la tête du prefab
    /// <c>Training Dummy</c> : un masque courbe qui épouse l'avant de la sphère (<c>Visage</c>, enfant de <c>Head</c>).</item>
    /// </list>
    /// La scène ne change qu'une fois, tant que le groupe « Training » a encore ses cibles ; elle est alors à enregistrer (Ctrl+S).
    /// Le visage s'ajoute au prefab s'il n'y est pas.
    /// </summary>
    [InitializeOnLoad]
    static class TrainingLaneBaker
    {
        const string k_Folder = "Assets/_Project/Art/Training";
        const string k_FaceTexture = k_Folder + "/Visage_Mannequin.png";
        const string k_FaceMaterial = k_Folder + "/Visage_Mannequin.mat";
        const string k_FaceMesh = k_Folder + "/Visage_Mannequin_Masque.asset";
        const string k_DummyPrefab = "Assets/_Project/Prefabs/Training Dummy.prefab";
        const string k_FaceName = "Visage";
        const string k_GroupName = "Training";

        // Le masque, dans le repère de la tête (une sphère de rayon 0,5, à l'échelle 0,3 : 15 cm) : un carré de ±0,32 projeté sur
        // l'avant de la sphère (+Z), un peu au-dessus de sa surface.
        const float k_FaceHalfSize = 0.32f;
        const float k_FaceRadius = 0.507f;
        const int k_FaceSteps = 12;

        // Les mannequins le long de la voie du milieu : distance (m) depuis la tour, demi-largeur (m) des droite-gauche, vitesses.
        const float k_NearDistance = 14f;
        const float k_GroupDistance = 24f;
        const float k_FarDistance = 42f;
        const float k_PatrolHalfWidth = 2.8f;
        const float k_NearSpeed = 1.2f;
        const float k_FarSpeed = 1.5f;

        // Les torches : le pas de la recherche du bord (m), sa portée, l'écart au bord (dehors, dedans), le niveau admis dehors.
        const float k_Step = 0.05f;
        const float k_SearchReach = 12f;
        const float k_OutsideGap = 0.4f;
        const float k_InsideGap = 0.3f;
        const float k_LevelTolerance = 0.5f;
        const float k_LaneTorchReach = 9f;

        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        static TrainingLaneBaker()
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

            var log = new List<string>();
            var material = EnsureFaceAssets(log);
            if (material != null)
                AddFaceToPrefab(material, log);

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    LayOut(scene, log);
            }

            if (log.Count > 0)
                Debug.Log("Entraînement et torches (2 novembre au soir) :\n" + string.Join("\n", log));
        }

        // ---------------------------------------------------------------- le visage

        // Les réglages d'import de la texture (une fois), le masque et son matériau (s'ils manquent). Null tant que la texture
        // n'est pas importée.
        static Material EnsureFaceAssets(List<string> log)
        {
            var importer = AssetImporter.GetAtPath(k_FaceTexture) as TextureImporter;
            if (importer == null)
                return null;

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 256;
                importer.SaveAndReimport();
                log.Add("Visage_Mannequin.png : importée (transparence, bords bloqués, 256 px)");
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(k_FaceTexture);
            if (texture == null)
                return null;

            if (AssetDatabase.LoadAssetAtPath<Mesh>(k_FaceMesh) == null)
            {
                AssetDatabase.CreateAsset(BuildFaceMesh(), k_FaceMesh);
                log.Add("Visage_Mannequin_Masque.asset : le masque courbe, créé");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(k_FaceMaterial);
            if (material != null)
                return material;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                Debug.LogWarning("Visage des mannequins : shader « Universal Render Pipeline/Unlit » introuvable.");
                return null;
            }

            // Sans lumière, découpé (pas de transparence à trier) : des traits noirs nets, de la couleur du dessin.
            material = new Material(shader) { name = "Visage_Mannequin" };
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            AssetDatabase.CreateAsset(material, k_FaceMaterial);
            log.Add("Visage_Mannequin.mat : créé (URP Unlit, découpé)");
            return material;
        }

        // Une grille posée sur l'avant de la sphère de la tête. Vu de face (depuis +Z, en regardant vers -Z), la gauche du dessin
        // est du côté +X : u va de +X à -X, sinon le visage serait en miroir.
        static Mesh BuildFaceMesh()
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (var j = 0; j <= k_FaceSteps; j++)
            {
                for (var i = 0; i <= k_FaceSteps; i++)
                {
                    var u = (float)i / k_FaceSteps;
                    var v = (float)j / k_FaceSteps;
                    var x = (0.5f - u) * 2f * k_FaceHalfSize;
                    var y = (v - 0.5f) * 2f * k_FaceHalfSize;
                    var z = Mathf.Sqrt(Mathf.Max(0f, k_FaceRadius * k_FaceRadius - x * x - y * y));
                    var point = new Vector3(x, y, z);
                    vertices.Add(point);
                    normals.Add(point.normalized);
                    uvs.Add(new Vector2(u, v));
                }
            }

            var row = k_FaceSteps + 1;
            for (var j = 0; j < k_FaceSteps; j++)
            {
                for (var i = 0; i < k_FaceSteps; i++)
                {
                    var a = j * row + i;
                    var b = a + 1;
                    var c = a + row;
                    var d = c + 1;
                    // Faces tournées vers +Z (vers qui regarde le mannequin).
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
            }

            var mesh = new Mesh { name = "Visage_Mannequin_Masque" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddFaceToPrefab(Material material, List<string> log)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(k_FaceMesh);
            if (mesh == null || AssetDatabase.LoadAssetAtPath<GameObject>(k_DummyPrefab) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(k_DummyPrefab);
            try
            {
                var head = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Head");
                if (head == null || head.Find(k_FaceName) != null)
                    return;

                var face = new GameObject(k_FaceName);
                face.transform.SetParent(head, false);
                face.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = face.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                PrefabUtility.SaveAsPrefabAsset(root, k_DummyPrefab);
                log.Add("Training Dummy : le visage sur la tête (Head > Visage), tourné vers l'avant du mannequin (+Z)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- la scène

        static void LayOut(Scene scene, List<string> log)
        {
            var group = scene.GetRootGameObjects().Select(go => go.transform).FirstOrDefault(t => t.name == k_GroupName);
            if (group == null || group.GetComponentInChildren<TrainingTarget>(true) == null)
                return;

            var tower = Object.FindObjectsByType<Tower>(FindObjectsInactive.Include).FirstOrDefault(t => t.gameObject.scene == scene);
            var spawner = Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include).FirstOrDefault(s => s.gameObject.scene == scene);
            if (tower == null || spawner == null)
            {
                Debug.LogWarning("Entraînement : pas de tour ou de générateur d'ennemis dans la scène, rien n'est déplacé.");
                return;
            }

            // La voie du milieu : de la tour vers le point d'apparition du milieu (l'angle médian des trois, vu de la tour).
            var origin = Flat(tower.transform.position);
            var spawns = spawner.SpawnPoints.Where(point => point != null).Select(point => Flat(point.position)).ToList();
            if (spawns.Count < 3)
            {
                Debug.LogWarning("Entraînement : moins de 3 points d'apparition, la voie du milieu est introuvable.");
                return;
            }

            var middle = spawns.OrderBy(point => Mathf.Atan2(point.x - origin.x, point.z - origin.z)).ElementAt(spawns.Count / 2);
            var axis = (middle - origin).normalized;
            var right = new Vector3(axis.z, 0f, -axis.x);

            // Les chemins des ennemis (pour les torches) : sans NavMesh, rien ne change, on réessaiera au prochain passage.
            Physics.SyncTransforms();
            var lanes = MapScan.RunLanes().Lanes;
            if (lanes.Count == 0)
            {
                Debug.LogWarning("Entraînement et torches : aucun chemin d'ennemis trouvé (NavMesh ?), rien n'est changé pour l'instant.");
                return;
            }

            var ignored = new HashSet<Collider>(group.GetComponentsInChildren<Collider>(true));
            var removed = RemoveTargets(group);
            PlaceDummies(scene, group, origin, axis, right, ignored, log);
            log.Add($"Cibles d'entraînement retirées : {removed}");
            MoveTorches(scene, lanes, log);
            EditorSceneManager.MarkSceneDirty(scene);
            log.Add("Enregistre la scène (Ctrl+S).");
        }

        static int RemoveTargets(Transform group)
        {
            var targets = Enumerable.Range(0, group.childCount).Select(group.GetChild)
                .Where(child => child.GetComponentInChildren<TrainingTarget>(true) != null).ToList();
            foreach (var target in targets)
                Undo.DestroyObjectImmediate(target.gameObject);
            return targets.Count;
        }

        static void PlaceDummies(Scene scene, Transform group, Vector3 origin, Vector3 axis, Vector3 right, HashSet<Collider> ignored,
                                 List<string> log)
        {
            var pool = Enumerable.Range(0, group.childCount).Select(group.GetChild)
                .Where(child => child.GetComponent<TrainingDummy>() != null).ToList();

            Transform Take(System.Func<Transform, bool> wanted)
            {
                var found = pool.FirstOrDefault(wanted);
                if (found != null)
                    pool.Remove(found);
                return found;
            }

            var near = Take(t => t.GetComponent<PingPongMover>() != null) ?? Take(t => true);
            var far = Take(t => t.name.Contains("loin")) ?? Take(t => true);
            var trio = new[] { Take(t => t.name.Contains("groupe")) ?? Take(t => true), Take(t => t.name.Contains("groupe")) ?? Take(t => true),
                               Take(t => t.name.Contains("groupe")) ?? Take(t => true) };

            // Les mannequins en trop (le doublon du groupe, l'ancien mannequin seul) sont retirés ; il en manque : on en pose.
            foreach (var extra in pool)
                Undo.DestroyObjectImmediate(extra.gameObject);
            near = near != null ? near : NewDummy(scene, group);
            far = far != null ? far : NewDummy(scene, group);
            for (var i = 0; i < trio.Length; i++)
                trio[i] = trio[i] != null ? trio[i] : NewDummy(scene, group);

            var start = -right * k_PatrolHalfWidth;
            var patrol = right * (2f * k_PatrolHalfWidth);
            Put(near, "Mannequin proche (va-et-vient)", origin + axis * k_NearDistance + start, origin, ignored);
            SetPatrol(near, patrol, k_NearSpeed);
            Put(far, "Mannequin loin (va-et-vient)", origin + axis * k_FarDistance + start, origin, ignored);
            SetPatrol(far, patrol, k_FarSpeed);

            // Le groupe de 3, serré (1,5 m), en triangle pointé vers la tour.
            var center = origin + axis * k_GroupDistance;
            var slots = new[] { center - axis * 0.45f - right * 0.75f, center - axis * 0.45f + right * 0.75f, center + axis * 0.85f };
            for (var i = 0; i < trio.Length; i++)
            {
                Put(trio[i], $"Mannequin groupe {i + 1}", slots[i], origin, ignored);
                var mover = trio[i] != null ? trio[i].GetComponent<PingPongMover>() : null;
                if (mover != null)
                    Undo.DestroyObjectImmediate(mover);
            }

            log.Add($"Mannequins sur la voie du milieu : un à {k_NearDistance:0} m qui va et vient, un groupe de 3 à {k_GroupDistance:0} m, " +
                    $"un à {k_FarDistance:0} m qui va et vient ({pool.Count} en trop retiré(s))");
        }

        static Transform NewDummy(Scene scene, Transform group)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_DummyPrefab);
            if (prefab == null)
                return null;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(group, false);
            Undo.RegisterCreatedObjectUndo(instance, "Mannequins d'entraînement");
            return instance.transform;
        }

        // Posé au sol (la voie), tourné vers la tour, renommé.
        static void Put(Transform dummy, string name, Vector3 point, Vector3 origin, HashSet<Collider> ignored)
        {
            if (dummy == null)
                return;

            var ground = point;
            ground.y = GroundHeight(point, ignored, out var height) ? height : 0f;
            var toTower = origin - Flat(point);
            Undo.RecordObject(dummy, "Mannequins d'entraînement");
            dummy.SetPositionAndRotation(ground, Quaternion.LookRotation(toTower.sqrMagnitude > 1e-4f ? toTower.normalized : Vector3.back));
            PrefabUtility.RecordPrefabInstancePropertyModifications(dummy);
            if (dummy.name != name)
            {
                Undo.RecordObject(dummy.gameObject, "Mannequins d'entraînement");
                dummy.name = name;
                PrefabUtility.RecordPrefabInstancePropertyModifications(dummy.gameObject);
            }
        }

        static void SetPatrol(Transform dummy, Vector3 offset, float speed)
        {
            if (dummy == null)
                return;

            var mover = dummy.GetComponent<PingPongMover>();
            if (mover == null)
                mover = Undo.AddComponent<PingPongMover>(dummy.gameObject);
            var serialized = new SerializedObject(mover);
            serialized.FindProperty("m_Offset").vector3Value = offset;
            serialized.FindProperty("m_Speed").floatValue = speed;
            serialized.ApplyModifiedProperties();
        }

        static bool GroundHeight(Vector3 point, HashSet<Collider> ignored, out float height)
        {
            height = 0f;
            var count = Physics.RaycastNonAlloc(new Vector3(point.x, point.y + 10f, point.z), Vector3.down, s_Hits, 20f,
                                                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if (hit.distance >= best || ignored.Contains(hit.collider) || hit.normal.y < 0.5f || MapScan.IsPlant(hit.collider.transform))
                    continue;
                best = hit.distance;
                height = hit.point.y;
            }

            return best < float.MaxValue;
        }

        // ---------------------------------------------------------------- les torches

        // Les torches sur pied des voies (à moins de 9 m du chemin des ennemis, comme en jeu) : au bord de la terre de la voie,
        // juste dehors, tournées vers la voie.
        static void MoveTorches(Scene scene, List<MapScan.Lane> lanes, List<string> log)
        {
            var lighting = MapLighting.FindIn(scene);
            var torches = lighting != null ? lighting.transform.Find("Torches") : null;
            if (torches == null)
                return;

            var moved = 0;
            var inside = 0;
            for (var i = 0; i < torches.childCount; i++)
            {
                var torch = torches.GetChild(i);
                if (torch.name != "Torche")
                    continue;

                var position = torch.position;
                MapScan.Lane lane = null;
                var best = k_LaneTorchReach;
                var along = 0f;
                foreach (var candidate in lanes)
                {
                    var distance = candidate.DistanceTo(position, out var at);
                    if (distance >= best)
                        continue;
                    best = distance;
                    lane = candidate;
                    along = at;
                }

                // Seulement celles au niveau de la voie (pas une torche de terrasse au-dessus).
                if (lane == null || Mathf.Abs(position.y - lane.PointAt(along, out _).y) > 1.2f ||
                    !TryEdge(position, lane, along, out var edge, out var outward, out var laneHeight))
                    continue;

                var spot = edge + outward * k_OutsideGap;
                if (!IsOpenGround(spot, laneHeight, out var height))
                {
                    spot = edge - outward * k_InsideGap;
                    height = laneHeight;
                    inside++;
                }

                Undo.RecordObject(torch, "Torches au bord des voies");
                torch.SetPositionAndRotation(new Vector3(spot.x, height, spot.z), Quaternion.LookRotation(-outward));
                moved++;
            }

            log.Add($"Torches sur pied des voies collées au bord de la terre : {moved} (dont {inside} juste dedans, faute de sol plat dehors)");
        }

        // Le bord de la terre de la voie le plus proche de la torche, en travers de la voie : le point du bord, le sens vers
        // l'extérieur, la hauteur de la voie.
        static bool TryEdge(Vector3 torch, MapScan.Lane lane, float along, out Vector3 edge, out Vector3 outward, out float laneHeight)
        {
            var lanePoint = lane.PointAt(along, out var direction);
            direction.y = 0f;
            var across = Vector3.Cross(Vector3.up, direction.normalized);
            edge = torch;
            outward = across;
            laneHeight = lanePoint.y;
            if (across.sqrMagnitude < 1e-4f)
                return false;

            if (IsLaneDirt(torch, out var height))
            {
                // Dans la voie : le bord le plus proche, d'un côté ou de l'autre.
                laneHeight = height;
                var plus = March(torch, across, false);
                var minus = March(torch, -across, false);
                if (plus < 0f && minus < 0f)
                    return false;

                var useMinus = plus < 0f || (minus >= 0f && minus < plus);
                outward = useMinus ? -across : across;
                edge = torch + outward * ((useMinus ? minus : plus) - k_Step * 0.5f);
                return true;
            }

            // Hors de la voie : vers le chemin des ennemis, jusqu'à la terre.
            var toward = Vector3.Dot(lanePoint - torch, across) >= 0f ? across : -across;
            var reach = March(torch, toward, true);
            if (reach < 0f)
                return false;

            outward = -toward;
            edge = torch + toward * (reach - k_Step * 0.5f);
            IsLaneDirt(torch + toward * reach, out laneHeight);
            return true;
        }

        // La distance (m) du premier point où la terre de la voie commence (toDirt) ou cesse, sur cette ligne ; -1 sinon.
        static float March(Vector3 from, Vector3 direction, bool toDirt)
        {
            for (var distance = k_Step; distance <= k_SearchReach; distance += k_Step)
            {
                if (IsLaneDirt(from + direction * distance, out _) == toDirt)
                    return distance;
            }

            return -1f;
        }

        // De la terre d'une voie sous ce point (une tuile rangée sous « Lanes », matériau « dirt »), comme la grille de construction.
        static bool IsLaneDirt(Vector3 point, out float height)
        {
            if (!TopHit(point, out var hit))
            {
                height = 0f;
                return false;
            }

            height = hit.point.y;
            return HasAncestor(hit.collider.transform, "Lanes") &&
                   MaterialName(hit).StartsWith("dirt", System.StringComparison.OrdinalIgnoreCase);
        }

        // Dehors : un sol à peu près au niveau de la voie, ni sa terre, ni l'eau de la rivière.
        static bool IsOpenGround(Vector3 point, float laneHeight, out float height)
        {
            height = laneHeight;
            if (!TopHit(point, out var hit))
                return false;

            height = hit.point.y;
            return Mathf.Abs(height - laneHeight) <= k_LevelTolerance && !HasAncestor(hit.collider.transform, "River", true) &&
                   !(HasAncestor(hit.collider.transform, "Lanes") && MaterialName(hit).StartsWith("dirt", System.StringComparison.OrdinalIgnoreCase));
        }

        // Le sol le plus haut sous ce point (ni plante, ni mannequin, ni déclencheur).
        static bool TopHit(Vector3 point, out RaycastHit best)
        {
            best = default;
            var count = Physics.RaycastNonAlloc(new Vector3(point.x, point.y + 8f, point.z), Vector3.down, s_Hits, 16f,
                                                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var found = false;
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if ((found && hit.distance >= best.distance) || hit.normal.y < 0.5f || MapScan.IsPlant(hit.collider.transform) ||
                    hit.collider.GetComponentInParent<TrainingDummy>(true) != null)
                    continue;
                best = hit;
                found = true;
            }

            return found;
        }

        // prefix : le nom commence seulement par name (« River », « River Water »).
        static bool HasAncestor(Transform transform, string name, bool prefix = false)
        {
            for (var i = 0; i < 6 && transform != null; i++, transform = transform.parent)
            {
                if (prefix ? transform.name.StartsWith(name, System.StringComparison.Ordinal) : transform.name == name)
                    return true;
            }

            return false;
        }

        // Le matériau du triangle touché (une case coupée en diagonale a de la terre et de l'herbe).
        static string MaterialName(RaycastHit hit)
        {
            var renderer = hit.collider.GetComponent<MeshRenderer>();
            var materials = renderer != null ? renderer.sharedMaterials : null;
            if (materials == null || materials.Length == 0)
                return "";

            var mesh = hit.collider is MeshCollider meshCollider ? meshCollider.sharedMesh : null;
            if (materials.Length == 1 || mesh == null || hit.triangleIndex < 0)
                return materials[0] != null ? materials[0].name : "";

            var end = 0;
            for (var i = 0; i < mesh.subMeshCount && i < materials.Length; i++)
            {
                end += (int)(mesh.GetSubMesh(i).indexCount / 3);
                if (hit.triangleIndex < end)
                    return materials[i] != null ? materials[i].name : "";
            }

            return "";
        }

        static Vector3 Flat(Vector3 point) => new Vector3(point.x, 0f, point.z);
    }
}
