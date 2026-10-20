using System.Linq;
using Archery.Cyber;
using Archery.FinalBoss;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Pose le passage vers le toit cyberpunk dans la scène de la carte (celle qui a la porte de l'antre,
    /// <see cref="LairGate"/>) : l'objet « Passage cyberpunk » (<see cref="CyberPassage"/>, branché sur la porte) et les deux
    /// éclats de miroir glitchés (<see cref="GlitchShard"/>), derrière les cascades. La scène est marquée comme modifiée :
    /// il suffit de l'enregistrer (Ctrl+S).
    /// </summary>
    /// <remarks>
    /// Se fait tout seul une fois (tant que la scène n'a pas d'objet « Passage cyberpunk »). Les éclats se déplacent à la
    /// main : ce sont les objets « Éclat de miroir 1 » et « 2 », enfants du passage. Le modèle de l'éclat et son matériau
    /// vont dans <c>Art/Cyber</c>.
    /// </remarks>
    [InitializeOnLoad]
    static class CyberPassageBaker
    {
        const string k_ObjectName = "Passage cyberpunk";
        const string k_ArtFolder = "Assets/_Project/Art/Cyber";
        const string k_ShardMesh = k_ArtFolder + "/Eclat.asset";
        const string k_ShardMaterial = k_ArtFolder + "/Eclat glitche.mat";

        // Derrière les deux cascades de la rivière du haut (la grande, avec l'arc-en-ciel, puis celle en aval), un peu
        // au-dessus de leur pied, côté falaise. À ajuster à la main si besoin.
        static readonly Vector3[] k_ShardPositions = { new Vector3(45f, 6.9f, 75f), new Vector3(39f, 3.7f, 60f) };

        static CyberPassageBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            // Au retour du mode Play aussi : compilé en entrant en Play, l'outil n'avait pas pu poser le passage.
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    Schedule();
            };
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= SetUpOpenScenes;
            EditorApplication.delayCall += SetUpOpenScenes;
        }

        static void SetUpOpenScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                var roots = scene.GetRootGameObjects();
                if (roots.Any(root => root.GetComponentInChildren<CyberPassage>(true) != null))
                    continue;
                var gate = roots.Select(root => root.GetComponentInChildren<LairGate>(true)).FirstOrDefault(found => found != null);
                if (gate != null)
                    SetUp(scene, gate);
            }
        }

        static void SetUp(Scene scene, LairGate gate)
        {
            if (!AssetDatabase.IsValidFolder(k_ArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Cyber");

            var root = new GameObject(k_ObjectName);
            SceneManager.MoveGameObjectToScene(root, scene);
            var shards = new GlitchShard[k_ShardPositions.Length];
            for (var i = 0; i < shards.Length; i++)
                shards[i] = CreateShard(root.transform, i, k_ShardPositions[i]);

            var passage = root.AddComponent<CyberPassage>();
            var settings = new SerializedObject(passage);
            settings.FindProperty("m_Gate").objectReferenceValue = gate;
            var list = settings.FindProperty("m_Shards");
            list.arraySize = shards.Length;
            for (var i = 0; i < shards.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = shards[i];
            settings.FindProperty("m_CrackleClip").objectReferenceValue = Load<AudioClip>("Assets/kenney_interfaceSounds/Audio/glitch_002.ogg");
            settings.FindProperty("m_GateClip").objectReferenceValue = Load<AudioClip>("Assets/_Project/Audio/Placeholder/teleport_charge.wav");
            settings.FindProperty("m_EnterClip").objectReferenceValue = Load<AudioClip>("Assets/kenney_interfaceSounds/Audio/glitch_004.ogg");
            settings.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Toit cyberpunk : le passage (la porte de l'antre) et les deux éclats de miroir sont posés derrière les cascades. " +
                      "Enregistre la scène (Ctrl+S).", root);
        }

        static GlitchShard CreateShard(Transform parent, int index, Vector3 position)
        {
            var go = new GameObject($"Éclat de miroir {index + 1}");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var visual = new GameObject("Éclat");
            visual.transform.SetParent(go.transform, false);
            visual.AddComponent<MeshFilter>().sharedMesh = ShardMesh();
            var renderer = visual.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ShardMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            // Une petite lueur cyan : on le devine dans le creux.
            var light = new GameObject("Lueur").AddComponent<Light>();
            light.transform.SetParent(go.transform, false);
            light.type = LightType.Point;
            light.color = new Color(0.3f, 0.95f, 1f);
            light.range = 3f;
            light.intensity = 1.2f;
            light.shadows = LightShadows.None;

            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(0.5f, 0.7f, 0.5f);

            var shard = go.AddComponent<GlitchShard>();
            var settings = new SerializedObject(shard);
            settings.FindProperty("m_Visual").objectReferenceValue = visual.transform;
            settings.FindProperty("m_PixelMaterial").objectReferenceValue = Load<Material>("Assets/_Project/Materials/Effects.mat");
            var crackles = settings.FindProperty("m_CrackleClips");
            var clips = new[] { "glitch_001", "glitch_002", "glitch_003", "glitch_004" }
                .Select(name => Load<AudioClip>($"Assets/kenney_interfaceSounds/Audio/{name}.ogg")).Where(clip => clip != null).ToArray();
            crackles.arraySize = clips.Length;
            for (var i = 0; i < clips.Length; i++)
                crackles.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            settings.FindProperty("m_PickClip").objectReferenceValue = Load<AudioClip>("Assets/_Project/Audio/Placeholder/orb_pick.wav");
            settings.ApplyModifiedPropertiesWithoutUndo();
            return shard;
        }

        // Un éclat de miroir : une plaque mince et irrégulière, à cinq pointes, d'environ 40 cm.
        static Mesh ShardMesh()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(k_ShardMesh);
            if (mesh != null)
                return mesh;

            var outline = new[]
            {
                new Vector2(0f, 0.22f), new Vector2(0.11f, 0.05f), new Vector2(0.07f, -0.2f), new Vector2(-0.05f, -0.12f), new Vector2(-0.12f, 0.06f),
            };
            const float half = 0.012f;
            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            // Les deux faces (en éventail depuis le centre).
            foreach (var side in new[] { 1f, -1f })
            {
                var center = vertices.Count;
                vertices.Add(new Vector3(0f, 0f, side * half));
                for (var i = 0; i < outline.Length; i++)
                    vertices.Add(new Vector3(outline[i].x, outline[i].y, side * half));
                for (var i = 0; i < outline.Length; i++)
                {
                    var a = center + 1 + i;
                    var b = center + 1 + (i + 1) % outline.Length;
                    if (side > 0f)
                        triangles.AddRange(new[] { center, b, a });
                    else
                        triangles.AddRange(new[] { center, a, b });
                }
            }

            // La tranche.
            for (var i = 0; i < outline.Length; i++)
            {
                var p0 = outline[i];
                var p1 = outline[(i + 1) % outline.Length];
                var start = vertices.Count;
                vertices.Add(new Vector3(p0.x, p0.y, half));
                vertices.Add(new Vector3(p1.x, p1.y, half));
                vertices.Add(new Vector3(p1.x, p1.y, -half));
                vertices.Add(new Vector3(p0.x, p0.y, -half));
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            mesh = new Mesh { name = "Éclat de miroir" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, k_ShardMesh);
            return mesh;
        }

        // Un miroir qui bugue : argenté, au contour cyan ou rouge par tranches, qui tressaute fort.
        static Material ShardMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(k_ShardMaterial);
            if (material != null)
                return material;

            material = new Material(Load<Shader>("Assets/_Project/Shaders/Glitch.shader")) { name = "Éclat glitché" };
            material.SetColor("_BaseColor", new Color(0.75f, 0.8f, 0.88f, 1f));
            material.SetFloat("_Silhouette", 0.35f);
            material.SetFloat("_Glitch", 0.6f);
            material.SetColor("_ShadowColor", new Color(0.05f, 0.05f, 0.08f, 1f));
            material.SetColor("_RimColor", new Color(2.2f, 0.2f, 0.3f, 1f));
            material.SetColor("_RimColorAlt", new Color(0.3f, 1.8f, 2.4f, 1f));
            material.SetFloat("_RimPower", 2f);
            material.SetColor("_Tint", new Color(1f, 0.1f, 0.3f, 1f));
            AssetDatabase.CreateAsset(material, k_ShardMaterial);
            return material;
        }

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogWarning($"Toit cyberpunk : {path} introuvable.");
            return asset;
        }
    }
}
