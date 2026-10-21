using System.Collections.Generic;
using System.Linq;
using Archery.Cyber;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Creuse une petite grotte derrière les deux cascades du passage cyberpunk (la grande, avec l'arc-en-ciel, et celle
    /// juste en aval), où l'on entre en traversant le rideau d'eau. La pièce de Kenney (<c>cliff_waterfall_rock</c>) n'est
    /// qu'un mur fin, avec un renfoncement de 40 cm derrière l'eau, et la montagne est faite d'autres blocs juste derrière :
    /// on découpe la grotte (2,4 m de large, 2,6 m de haut au-dessus du sol devant la cascade, 1,6 m de profondeur) dans le
    /// mur, sans toucher à l'eau, et dans tout ce qui la traverse (blocs de falaise, sol, rochers) ; on pose ses parois, du
    /// matériau du mur ; l'eau de ces deux cascades devient translucide et ne bloque plus (on la traverse, le rayon des
    /// mains aussi) ; l'éclat de miroir flotte au milieu, à 1 m du sol.
    /// </summary>
    /// <remarks>
    /// Les maillages creusés sont de nouveaux assets (<c>Art/Cyber/Cascade creusee v2 …</c>), donnés à ces objets-là
    /// seulement : les autres gardent leur modèle. Se fait tout seul une fois, dans la scène qui a le passage cyberpunk
    /// (scène marquée comme modifiée : Ctrl+S) ; ensuite, menu <c>Archery > Toit cyberpunk > Recreuser derrière les cascades</c>.
    /// </remarks>
    [InitializeOnLoad]
    static class WaterfallCaveBaker
    {
        const string k_SourceModel = "Assets/kenney_nature-kit/Models/FBX format/cliff_waterfall_rock.fbx";
        const string k_Folder = "Assets/_Project/Art/Cyber";
        const string k_Prefix = "Cascade creusee v2";
        const string k_AnyCarved = "Cascade creusee";
        const string k_MeshName = "cliff_waterfall_rock";
        const string k_WaterMaterial = k_Folder + "/Cascade water translucide.mat";

        // Le pied des deux cascades (la grande, puis celle en aval) : le mur le plus proche est creusé.
        static readonly Vector3[] k_Waterfalls = { new Vector3(45f, 5.6f, 73.5f), new Vector3(39f, 2.6f, 58.5f) };

        // La grotte (m) : largeur, hauteur au-dessus du sol devant la cascade, profondeur derrière le mur, hauteur de l'éclat.
        const float k_Width = 2.4f;
        const float k_Height = 2.6f;
        const float k_Depth = 1.6f;
        const float k_ShardHeight = 1f;

        struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 Uv;

            public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex
            {
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Normal = Vector3.Lerp(a.Normal, b.Normal, t).normalized,
                Uv = Vector2.Lerp(a.Uv, b.Uv, t),
            };
        }

        // Une face de la grotte : le plan « Normal · p = Offset », la normale vers l'extérieur de la grotte.
        struct Face
        {
            public Vector3 Normal;
            public float Offset;

            public float Distance(Vector3 point) => Vector3.Dot(Normal, point) - Offset;

            // La même face, dans un autre repère (matrice du repère de la cascade vers celui de l'objet).
            public Face In(Matrix4x4 toLocal)
            {
                var point = toLocal.MultiplyPoint3x4(Normal * Offset);
                var normal = toLocal.inverse.transpose.MultiplyVector(Normal).normalized;
                return new Face { Normal = normal, Offset = Vector3.Dot(normal, point) };
            }
        }

        static WaterfallCaveBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            // Au retour du mode Play aussi : compilé en entrant en Play, l'outil n'avait pas pu creuser.
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    Schedule();
            };
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= AutoCarve;
            EditorApplication.delayCall += AutoCarve;
        }

        static void AutoCarve()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;
            Carve(false);
        }

        [MenuItem("Archery/Toit cyberpunk/Recreuser derrière les cascades")]
        static void Recarve() => Carve(true);

        static void Carve(bool force)
        {
            for (var s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded)
                    continue;
                var passage = scene.GetRootGameObjects().Select(root => root.GetComponentInChildren<CyberPassage>(true)).FirstOrDefault(found => found != null);
                if (passage == null)
                    continue;

                var all = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true)).ToList();
                var shards = new SerializedObject(passage).FindProperty("m_Shards");
                var changed = false;
                for (var i = 0; i < k_Waterfalls.Length; i++)
                {
                    var wall = all.Where(IsWall).OrderBy(f => Vector3.Distance(f.transform.position, k_Waterfalls[i])).FirstOrDefault();
                    if (wall == null || Vector3.Distance(wall.transform.position, k_Waterfalls[i]) > 3f)
                    {
                        Debug.LogWarning($"Toit cyberpunk : pas de cascade (cliff_waterfall_rock) près de {k_Waterfalls[i]} : rien à creuser.");
                        continue;
                    }

                    if (!force && wall.sharedMesh.name.StartsWith(k_Prefix))
                        continue;

                    var shard = shards != null && i < shards.arraySize ? shards.GetArrayElementAtIndex(i).objectReferenceValue as GlitchShard : null;
                    if (CarveWaterfall(wall, i + 1, shard, all))
                        changed = true;
                }

                if (!changed)
                    continue;
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log("Toit cyberpunk : une grotte est creusée derrière les deux cascades (dans le mur et dans les blocs de la " +
                          "montagne derrière), et les éclats de miroir y sont rangés. Enregistre la scène (Ctrl+S).");
            }
        }

        // Le mur d'une cascade : le modèle de Kenney, ou une version creusée (pas un autre objet creusé, « … - nom »).
        static bool IsWall(MeshFilter filter)
        {
            var mesh = filter.sharedMesh;
            if (mesh == null)
                return false;
            return mesh.name.StartsWith(k_MeshName) || (mesh.name.StartsWith(k_AnyCarved) && !mesh.name.Contains(" - ") && !mesh.name.EndsWith("collision"));
        }

        static bool CarveWaterfall(MeshFilter wall, int number, GlitchShard shard, List<MeshFilter> all)
        {
            var source = Readable(AssetDatabase.LoadAllAssetsAtPath(k_SourceModel).OfType<Mesh>().FirstOrDefault());
            if (source == null)
            {
                Debug.LogWarning($"Toit cyberpunk : {k_SourceModel} introuvable ou illisible.");
                return false;
            }

            var renderer = wall.GetComponent<MeshRenderer>();
            var materials = renderer != null ? renderer.sharedMaterials : new Material[0];
            var water = -1;
            var lining = 0;
            for (var i = 0; i < materials.Length; i++)
            {
                var name = materials[i] != null ? materials[i].name.ToLowerInvariant() : "";
                if (name.Contains("water"))
                    water = i;
                else if (name.Contains("dirt"))
                    lining = i;
            }

            var positions = source.vertices;
            if (water < 0 || water >= source.subMeshCount)
            {
                Debug.LogWarning("Toit cyberpunk : pas d'eau (matériau « water ») dans le modèle de la cascade.");
                return false;
            }

            // Les mesures du mur, dans le repère du maillage : le rideau d'eau, le fond du mur, la hauteur.
            var waterVertices = source.GetTriangles(water).Distinct().Select(index => positions[index]).ToList();
            var bounds = new Bounds(positions[0], Vector3.zero);
            foreach (var position in positions)
                bounds.Encapsulate(position);
            var waterZ = waterVertices.Average(p => p.z);
            var halfWidth = waterVertices.Max(p => Mathf.Abs(p.x));
            var back = Mathf.Abs(bounds.min.z - waterZ) > Mathf.Abs(bounds.max.z - waterZ) ? bounds.min.z : bounds.max.z;
            var forward = Mathf.Sign(waterZ - back);

            var transform = wall.transform;
            var scale = transform.lossyScale;
            var perMeter = new Vector3(1f / Mathf.Max(1e-4f, Mathf.Abs(scale.x)), 1f / Mathf.Max(1e-4f, Mathf.Abs(scale.y)),
                                       1f / Mathf.Max(1e-4f, Mathf.Abs(scale.z)));

            // Le sol devant la cascade (1 m devant le mur) : celui de la grotte, pour y entrer de plain-pied.
            var outward = transform.TransformDirection(new Vector3(0f, 0f, forward));
            outward.y = 0f;
            outward.Normalize();
            var frontZ = forward > 0f ? bounds.max.z : bounds.min.z;
            var probe = transform.TransformPoint(new Vector3(0f, bounds.min.y, frontZ)) + outward;
            var groundY = GroundBelow(probe, wall.transform) ?? transform.TransformPoint(bounds.min).y;

            var yLow = transform.InverseTransformPoint(new Vector3(probe.x, groundY - 0.05f, probe.z)).y;
            var yHigh = Mathf.Min(yLow + (k_Height + 0.05f) * perMeter.y, bounds.max.y - 0.1f * perMeter.y);
            var xHalf = Mathf.Min(k_Width * 0.5f * perMeter.x, halfWidth * 0.85f);
            var front = waterZ - forward * 0.02f * perMeter.z;
            var rear = back - forward * k_Depth * perMeter.z;
            var min = new Vector3(-xHalf, yLow, Mathf.Min(front, rear));
            var max = new Vector3(xHalf, yHigh, Mathf.Max(front, rear));
            var faces = BoxFaces(min, max);

            // Le mur : tout sauf la grotte ; l'eau reste telle quelle ; les parois de la grotte.
            var vertices = new List<Vertex>();
            var triangles = new List<int>[source.subMeshCount];
            Subtract(source, faces, water, vertices, triangles);
            var wallLining = triangles[Mathf.Clamp(lining, 0, triangles.Length - 1)];
            Quad(vertices, wallLining, new Vector3(min.x, min.y, rear), new Vector3(max.x, min.y, rear), new Vector3(max.x, max.y, rear), new Vector3(min.x, max.y, rear));
            Quad(vertices, wallLining, new Vector3(min.x, min.y, front), new Vector3(min.x, min.y, rear), new Vector3(min.x, max.y, rear), new Vector3(min.x, max.y, front));
            Quad(vertices, wallLining, new Vector3(max.x, min.y, rear), new Vector3(max.x, min.y, front), new Vector3(max.x, max.y, front), new Vector3(max.x, max.y, rear));
            Quad(vertices, wallLining, new Vector3(min.x, min.y, front), new Vector3(max.x, min.y, front), new Vector3(max.x, min.y, rear), new Vector3(min.x, min.y, rear));
            Quad(vertices, wallLining, new Vector3(min.x, max.y, rear), new Vector3(max.x, max.y, rear), new Vector3(max.x, max.y, front), new Vector3(min.x, max.y, front));

            var carved = SaveMesh($"{k_Folder}/{k_Prefix} {number}.asset", vertices, triangles);
            wall.sharedMesh = carved;
            Changed(wall);

            // La collision sans l'eau : on traverse le rideau d'eau, et le rayon des mains aussi.
            var collider = wall.GetComponent<MeshCollider>();
            if (collider != null)
            {
                var solid = new List<int>();
                for (var sub = 0; sub < triangles.Length; sub++)
                {
                    if (sub != water)
                        solid.AddRange(triangles[sub]);
                }

                collider.sharedMesh = SaveMesh($"{k_Folder}/{k_Prefix} {number} collision.asset", vertices, new[] { solid });
                Changed(collider);
            }

            // L'eau translucide : on devine la grotte sombre derrière, et la lueur de l'éclat.
            if (renderer != null)
            {
                materials[water] = WaterMaterial(materials[water]);
                renderer.sharedMaterials = materials;
                Changed(renderer);
            }

            // Tout ce que la grotte traverse derrière le mur (blocs de falaise, sol, rochers) est creusé aussi.
            var corners = new Bounds(transform.TransformPoint(min), Vector3.zero);
            foreach (var corner in Corners(min, max))
                corners.Encapsulate(transform.TransformPoint(corner));
            foreach (var other in all)
            {
                if (other == null || other == wall || other.sharedMesh == null || other.GetComponentInParent<CyberPassage>() != null)
                    continue;
                var otherRenderer = other.GetComponent<Renderer>();
                if (otherRenderer == null || !otherRenderer.bounds.Intersects(corners))
                    continue;

                var mesh = Readable(other.sharedMesh);
                if (mesh == null)
                    continue;
                var toLocal = other.transform.worldToLocalMatrix * transform.localToWorldMatrix;
                var localFaces = faces.Select(face => face.In(toLocal)).ToArray();
                var otherVertices = new List<Vertex>();
                var otherTriangles = new List<int>[mesh.subMeshCount];
                if (!Subtract(mesh, localFaces, -1, otherVertices, otherTriangles))
                    continue;

                // Un asset par objet : déjà creusé (relancé, ou traversé par les deux grottes), on réécrit le sien.
                var otherCollider = other.GetComponent<MeshCollider>();
                var original = other.sharedMesh;
                var path = original.name.StartsWith(k_Prefix) ? AssetDatabase.GetAssetPath(original)
                         : AssetDatabase.GenerateUniqueAssetPath($"{k_Folder}/{k_Prefix} {number} - {Safe(other.name)}.asset");
                var cut = SaveMesh(path, otherVertices, otherTriangles);
                other.sharedMesh = cut;
                Changed(other);
                if (otherCollider != null && otherCollider.sharedMesh == original)
                {
                    otherCollider.sharedMesh = cut;
                    Changed(otherCollider);
                }
            }

            // L'éclat flotte au milieu de la grotte, à 1 m du sol, un peu en retrait du rideau d'eau.
            if (shard != null)
            {
                var center = new Vector3(0f, Mathf.Min(yLow + (k_ShardHeight + 0.05f) * perMeter.y, Mathf.Lerp(min.y, max.y, 0.5f)), Mathf.Lerp(front, rear, 0.55f));
                shard.transform.position = transform.TransformPoint(center);
                Changed(shard.transform);
            }

            return true;
        }

        static Face[] BoxFaces(Vector3 min, Vector3 max) => new[]
        {
            new Face { Normal = Vector3.left, Offset = -min.x },
            new Face { Normal = Vector3.right, Offset = max.x },
            new Face { Normal = Vector3.down, Offset = -min.y },
            new Face { Normal = Vector3.up, Offset = max.y },
            new Face { Normal = Vector3.back, Offset = -min.z },
            new Face { Normal = Vector3.forward, Offset = max.z },
        };

        static IEnumerable<Vector3> Corners(Vector3 min, Vector3 max)
        {
            for (var i = 0; i < 8; i++)
                yield return new Vector3((i & 1) == 0 ? min.x : max.x, (i & 2) == 0 ? min.y : max.y, (i & 4) == 0 ? min.z : max.z);
        }

        // Le maillage sans la partie dans la grotte (sauf le sous-maillage « keep », gardé tel quel). Vrai si la grotte en
        // touchait au moins un triangle.
        static bool Subtract(Mesh mesh, Face[] faces, int keep, List<Vertex> vertices, List<int>[] triangles)
        {
            var positions = mesh.vertices;
            var normals = mesh.normals;
            var uvs = mesh.uv;
            var touched = false;
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
            {
                triangles[sub] = new List<int>();
                var source = mesh.GetTriangles(sub);
                for (var t = 0; t < source.Length; t += 3)
                {
                    var polygon = new List<Vertex>(3);
                    for (var k = 0; k < 3; k++)
                    {
                        var index = source[t + k];
                        polygon.Add(new Vertex
                        {
                            Position = positions[index],
                            Normal = normals.Length > index ? normals[index] : Vector3.up,
                            Uv = uvs.Length > index ? uvs[index] : Vector2.zero,
                        });
                    }

                    if (sub == keep || faces.Any(face => polygon.All(v => face.Distance(v.Position) >= -1e-5f)))
                    {
                        AddPolygon(polygon, vertices, triangles[sub]);
                        continue;
                    }

                    // À chaque face de la grotte, la partie dehors est gardée, la partie dedans passe à la face suivante ;
                    // ce qui reste dedans à la fin est retiré.
                    var inside = polygon;
                    foreach (var face in faces)
                    {
                        Split(inside, face, out var outside, out var remaining);
                        if (outside.Count >= 3)
                            AddPolygon(outside, vertices, triangles[sub]);
                        inside = remaining;
                        if (inside.Count < 3)
                            break;
                    }

                    if (inside.Count >= 3 && Area(inside) > 1e-8f)
                        touched = true;
                }
            }

            return touched;
        }

        static float Area(List<Vertex> polygon)
        {
            var sum = Vector3.zero;
            for (var i = 1; i < polygon.Count - 1; i++)
                sum += Vector3.Cross(polygon[i].Position - polygon[0].Position, polygon[i + 1].Position - polygon[0].Position);
            return sum.magnitude * 0.5f;
        }

        // Un objet de la scène modifié par l'outil (gardé aussi sur une instance de modèle ou de prefab).
        static void Changed(Object target)
        {
            EditorUtility.SetDirty(target);
            if (PrefabUtility.IsPartOfPrefabInstance(target))
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }

        static void Split(List<Vertex> polygon, Face face, out List<Vertex> outside, out List<Vertex> remaining)
        {
            outside = new List<Vertex>();
            remaining = new List<Vertex>();
            for (var i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i];
                var b = polygon[(i + 1) % polygon.Count];
                var da = face.Distance(a.Position);
                var db = face.Distance(b.Position);
                var aOut = da > 0f;
                var bOut = db > 0f;
                if (aOut)
                    outside.Add(a);
                else
                    remaining.Add(a);
                if (aOut != bOut && Mathf.Abs(da - db) > 1e-7f)
                {
                    var crossing = Vertex.Lerp(a, b, da / (da - db));
                    outside.Add(crossing);
                    remaining.Add(crossing);
                }
            }
        }

        static void AddPolygon(List<Vertex> polygon, List<Vertex> vertices, List<int> triangles)
        {
            var start = vertices.Count;
            vertices.AddRange(polygon);
            for (var i = 1; i < polygon.Count - 1; i++)
            {
                triangles.Add(start);
                triangles.Add(start + i);
                triangles.Add(start + i + 1);
            }
        }

        // Un rectangle visible des deux côtés (normales de chaque côté).
        static void Quad(List<Vertex> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var normal = Vector3.Cross(b - a, c - a).normalized;
            foreach (var side in new[] { 1f, -1f })
            {
                var start = vertices.Count;
                foreach (var position in new[] { a, b, c, d })
                    vertices.Add(new Vertex { Position = position, Normal = normal * side, Uv = new Vector2(position.x + position.z, position.y) });
                if (side > 0f)
                    triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
                else
                    triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
        }

        // Le sol sous ce point (cherché à partir de « rise » m au-dessus), sans compter la cascade elle-même ni les éclats.
        static float? GroundBelow(Vector3 point, Transform ignore, float rise = 4f)
        {
            Physics.SyncTransforms();
            var hits = Physics.RaycastAll(point + Vector3.up * rise, Vector3.down, rise + 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            float? ground = null;
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(ignore) || hit.collider.GetComponentInParent<CyberPassage>() != null || hit.distance >= best)
                    continue;
                best = hit.distance;
                ground = hit.point.y;
            }

            return ground;
        }

        // Le maillage, lisible : celui d'un modèle importé l'est rendu (case Read/Write de son import).
        static Mesh Readable(Mesh mesh)
        {
            if (mesh == null || mesh.isReadable)
                return mesh;
            var path = AssetDatabase.GetAssetPath(mesh);
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
                return null;
            var name = mesh.name;
            importer.isReadable = true;
            importer.SaveAndReimport();
            mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m => m.name == name);
            return mesh != null && mesh.isReadable ? mesh : null;
        }

        // Une eau bleue translucide (URP Lit, transparente), de la couleur de l'eau d'origine.
        static Material WaterMaterial(Material original)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(k_WaterMaterial);
            if (material != null)
                return material;

            var color = original != null && original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor")
                      : original != null ? original.color : new Color(0.35f, 0.65f, 0.9f);
            color.a = 0.5f;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Cascade water translucide" };
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Smoothness", 0.85f);
            material.SetFloat("_Cull", 0f);
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, k_WaterMaterial);
            return material;
        }

        static Mesh SaveMesh(string path, List<Vertex> vertices, List<int>[] triangles)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            var created = mesh == null;
            if (created)
                mesh = new Mesh();
            mesh.Clear();
            mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices.Select(v => v.Position).ToList());
            mesh.SetNormals(vertices.Select(v => v.Normal).ToList());
            mesh.SetUVs(0, vertices.Select(v => v.Uv).ToList());
            mesh.subMeshCount = triangles.Length;
            for (var i = 0; i < triangles.Length; i++)
                mesh.SetTriangles(triangles[i] ?? new List<int>(), i);
            mesh.RecalculateBounds();
            if (created)
                AssetDatabase.CreateAsset(mesh, path);
            else
                EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        static string Safe(string name) => new string(name.Select(c => char.IsLetterOrDigit(c) || c == ' ' || c == '_' ? c : '_').ToArray());
    }
}
