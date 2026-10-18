using System.Collections.Generic;
using System.IO;
using Archery.Portal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Construit le décor de la dimension du portail des étoiles (GDD, section 23) en vrais objets de la scène
    /// <c>Dimension_Etoiles</c>, modifiables à la main : le ciel (étoiles, nébuleuses, galaxies), le sol miroir, les torii
    /// (préfab <c>Torii</c>, plantés en désordre), les murs invisibles, la lumière, la musique et le reflet. Les maillages,
    /// les matériaux et le préfab sont rangés dans <c>Art/Dimension</c>.
    /// </summary>
    /// <remarks>
    /// Se fait tout seul une fois (tant que la scène n'a pas de reflet, <see cref="DimensionMirror"/>), et la scène est
    /// enregistrée. Ensuite, menu <c>Archery > Portail des étoiles > Reconstruire le décor de la dimension</c> pour tout
    /// refaire (les retouches faites à la main sur le décor sont alors perdues ; les matériaux et le préfab existants sont
    /// gardés).
    /// </remarks>
    [InitializeOnLoad]
    static class StarDimensionBaker
    {
        const string k_ScenePath = "Assets/_Project/Scenes/Dimension_Etoiles.unity";
        const string k_Folder = "Assets/_Project/Art/Dimension";
        const string k_MirrorGuid = "ef437f4ac7214f9fba08ad150881280e";

        // Un torii à l'échelle 1 : demi-écart des piliers, hauteur, rayon d'un pilier.
        const float k_HalfSpan = 2.8f;
        const float k_Height = 10f;
        const float k_PillarRadius = 0.55f;

        const float k_ArenaRadius = 23f;
        const float k_SkyRadius = 420f;
        const float k_NebulaRadius = 380f;
        const float k_GalaxyDistance = 330f;

        // L'axe de la Voie lactée (étoiles serrées et lueur).
        static readonly Vector3 k_MilkyWayAxis = new Vector3(0.35f, 0.75f, 0.55f).normalized;

        static StarDimensionBaker()
        {
            EditorApplication.delayCall -= AutoBuild;
            EditorApplication.delayCall += AutoBuild;
        }

        static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer || !File.Exists(k_ScenePath))
                return;
            if (File.ReadAllText(k_ScenePath).Contains(k_MirrorGuid))
                return;

            Build();
        }

        [MenuItem("Archery/Portail des étoiles/Reconstruire le décor de la dimension")]
        static void Rebuild()
        {
            if (!EditorUtility.DisplayDialog("Reconstruire le décor de la dimension",
                    "Le ciel, le sol, les torii, les murs, la lumière, la musique et le reflet de Dimension_Etoiles sont refaits : " +
                    "les retouches faites à la main sur ces objets seront perdues. Les réglages de l'objet « Dimension », " +
                    "les matériaux et le préfab Torii sont gardés.", "Reconstruire", "Annuler"))
                return;

            Build();
        }

        static void Build()
        {
            var starShader = FindShader("Archery/StarSprite", "Assets/_Project/Shaders/StarSprite.shader");
            var unlitShader = FindShader("Archery/UnlitVertexColor", "Assets/_Project/Shaders/UnlitVertexColor.shader");
            var skyShader = FindShader("Archery/StylizedSky", "Assets/_Project/Shaders/StylizedSky.shader");
            var litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (starShader == null || unlitShader == null || skyShader == null || litShader == null)
            {
                Debug.LogWarning("Dimension des étoiles : shaders pas encore prêts, le décor sera construit au prochain rechargement.");
                return;
            }

            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(k_ScenePath);
            var opened = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Additive);
                opened = true;
            }

            try
            {
                StarDimension dimension = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    dimension = root.GetComponentInChildren<StarDimension>(true);
                    if (dimension != null)
                        break;
                }

                if (dimension == null)
                {
                    Debug.LogError($"Dimension des étoiles : pas de Star Dimension dans {k_ScenePath}.");
                    return;
                }

                // Les objets temporaires (le préfab en construction) naissent dans la dimension, pas dans la carte ouverte.
                SceneManager.SetActiveScene(scene);
                EnsureFolder(k_Folder);
                var materials = BuildMaterials(starShader, unlitShader, skyShader, litShader);
                var torii = BuildToriiPrefab(materials);
                var light = BuildScene(dimension, materials, torii);
                ApplyEnvironment(materials.Sky, light);

                AssetDatabase.SaveAssets();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("Dimension des étoiles : décor construit et enregistré dans Dimension_Etoiles (ciel, sol miroir, torii, " +
                          "murs, lumière, musique, reflet). Tout se modifie à la main dans la scène ; maillages, matériaux et préfab " +
                          "dans Art/Dimension.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded && previous != scene)
                    SceneManager.SetActiveScene(previous);
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        static Shader FindShader(string name, string path)
        {
            var shader = Shader.Find(name);
            return shader != null ? shader : AssetDatabase.LoadAssetAtPath<Shader>(path);
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        // ---------------------------------------------------------------- matériaux

        class Materials
        {
            public Material Red;
            public Material Black;
            public Material Glow;
            public Material Sky;
            public Material Floor;
            public Material Grid;
        }

        static Materials BuildMaterials(Shader star, Shader unlit, Shader sky, Shader lit)
        {
            return new Materials
            {
                // Vermillon un peu lumineux (il brille aussi dans le reflet) ; noir pour le dessus des torii.
                Red = SaveMaterial("Torii_Vermillon", lit, m =>
                {
                    m.SetColor("_BaseColor", new Color(0.85f, 0.16f, 0.06f));
                    m.SetFloat("_Smoothness", 0.45f);
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", new Color(0.55f, 0.07f, 0.02f));
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }),
                Black = SaveMaterial("Torii_Noir", lit, m =>
                {
                    m.SetColor("_BaseColor", new Color(0.04f, 0.03f, 0.035f));
                    m.SetFloat("_Smoothness", 0.6f);
                }),
                Glow = SaveMaterial("Ciel_Lueurs", star, m => m.SetFloat("_Intensity", 1f)),
                Sky = SaveMaterial("Ciel_Dimension", sky, m =>
                {
                    // Noir violacé, sans aurore ni soleil ; sous l'horizon, la couleur du zénith (le sol miroir y montre le ciel).
                    m.SetColor("_ZenithColor", new Color(0.004f, 0.002f, 0.018f));
                    m.SetColor("_HorizonColor", new Color(0.06f, 0.02f, 0.12f));
                    m.SetColor("_GroundColor", new Color(0.006f, 0.003f, 0.02f));
                    m.SetFloat("_HorizonSharpness", 1.4f);
                    m.SetColor("_DiscColor", Color.black);
                    m.SetFloat("_DiscSize", 0.005f);
                    m.SetColor("_GlowColor", Color.black);
                    m.SetFloat("_GlowSize", 0.01f);
                    m.SetFloat("_MoonSurface", 0f);
                    m.SetFloat("_Stars", 1f);
                    m.SetFloat("_Aurora", 0f);
                    m.SetFloat("_ShootingStars", 1f);
                }),
                // Le sol : sombre et translucide (l'alpha règle la netteté du reflet : 0 = miroir parfait).
                Floor = SaveMaterial("Sol_Miroir", unlit, m => m.SetColor("_BaseColor", new Color(0.01f, 0.004f, 0.025f, 0.32f))),
                Grid = SaveMaterial("Sol_Grille", unlit, m => m.SetColor("_BaseColor", new Color(0.55f, 0.3f, 1.2f, 0.1f))),
            };
        }

        static Material SaveMaterial(string name, Shader shader, System.Action<Material> setUp)
        {
            var path = $"{k_Folder}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var material = new Material(shader) { name = name };
            setUp(material);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Mesh SaveMesh(Mesh mesh, string name)
        {
            var path = $"{k_Folder}/{name}.asset";
            mesh.name = name;
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            existing.name = name;
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        // ---------------------------------------------------------------- le torii

        // Au-dessus du sol, seulement les deux piliers ronds ; le haut (traverse, linteau, toit) n'est que dans le reflet :
        // ses rendus sont désactivés, DimensionMirror les montre quand même sous le sol.
        static GameObject BuildToriiPrefab(Materials materials)
        {
            var pillars = SaveMesh(ToriiPillars(), "Torii_Piliers");
            var beam = SaveMesh(ToriiBeam(), "Torii_Traverse");
            var lintel = SaveMesh(ToriiLintel(), "Torii_Linteau");
            var roof = SaveMesh(ToriiRoof(), "Torii_Toit");

            var path = $"{k_Folder}/Torii.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            var root = new GameObject("Torii");
            AddMesh(Child(root.transform, "Piliers"), pillars, materials.Red, 0);
            foreach (var side in new[] { -1f, 1f })
            {
                var post = Child(root.transform, side < 0f ? "Pilier gauche" : "Pilier droit", new Vector3(side * k_HalfSpan, 0f, 0f));
                var capsule = post.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0f, k_Height * 0.5f, 0f);
                capsule.height = k_Height;
                capsule.radius = k_PillarRadius;
            }

            var top = Child(root.transform, "Haut (seulement dans le reflet)");
            AddMesh(Child(top.transform, "Traverse"), beam, materials.Red, 0).enabled = false;
            AddMesh(Child(top.transform, "Linteau"), lintel, materials.Red, 0).enabled = false;
            AddMesh(Child(top.transform, "Toit"), roof, materials.Black, 0).enabled = false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static Mesh ToriiPillars()
        {
            var builder = new LitBuilder();
            foreach (var side in new[] { -k_HalfSpan, k_HalfSpan })
                builder.Cylinder(new Vector3(side, 0f, 0f), k_PillarRadius, k_Height, 24);
            return builder.ToMesh();
        }

        // La traverse du dessous (nuki), qui dépasse un peu des piliers.
        static Mesh ToriiBeam()
        {
            var builder = new LitBuilder();
            builder.Box(new Vector3(0f, k_Height - 2.2f, 0f), new Vector3(k_HalfSpan + 0.9f, 0.3f, 0.27f), Quaternion.identity);
            return builder.ToMesh();
        }

        // Le linteau vermillon (shimaki), relevé aux bouts, et le petit montant du milieu.
        static Mesh ToriiLintel()
        {
            var builder = new LitBuilder();
            foreach (var (center, halfLength, rotation) in KasagiSegments())
                builder.Box(center, new Vector3(halfLength, 0.28f, 0.42f), rotation);
            builder.Box(new Vector3(0f, k_Height - 0.94f, 0f), new Vector3(0.3f, 0.97f, 0.24f), Quaternion.identity);
            return builder.ToMesh();
        }

        // Le toit noir (kasagi), posé sur le linteau.
        static Mesh ToriiRoof()
        {
            var builder = new LitBuilder();
            foreach (var (center, halfLength, rotation) in KasagiSegments())
                builder.Box(center + rotation * (Vector3.up * 0.46f), new Vector3(halfLength, 0.18f, 0.58f), rotation);
            return builder.ToMesh();
        }

        static IEnumerable<(Vector3 center, float halfLength, Quaternion rotation)> KasagiSegments()
        {
            var length = k_HalfSpan + 2.1f;
            const int segments = 12;
            for (var i = 0; i < segments; i++)
            {
                var x0 = -length + 2f * length * i / segments;
                var x1 = x0 + 2f * length / segments;
                var y0 = 0.75f * Mathf.Pow(Mathf.Abs(x0) / length, 2.4f);
                var y1 = 0.75f * Mathf.Pow(Mathf.Abs(x1) / length, 2.4f);
                var rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(y1 - y0, x1 - x0) * Mathf.Rad2Deg);
                var halfLength = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)) * 0.5f + 0.03f;
                yield return (new Vector3((x0 + x1) * 0.5f, k_Height + 0.26f + (y0 + y1) * 0.5f, 0f), halfLength, rotation);
            }
        }

        // ---------------------------------------------------------------- la scène

        static Light BuildScene(StarDimension dimension, Materials materials, GameObject torii)
        {
            var root = dimension.transform;
            for (var i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            var spawn = Child(root, "Arrivée du joueur");

            // Le ciel : étoiles tout autour (le dessous compris), nébuleuses et galaxies (reflétées par le sol).
            var sky = Child(root, "Ciel");
            AddMesh(Child(sky.transform, "Étoiles"), SaveMesh(Stars(), "Ciel_Etoiles"), materials.Glow, -10);
            var nebulae = Child(sky.transform, "Nébuleuses");
            var nebulaPalettes = new[]
            {
                new[] { new Color(1.6f, 0.3f, 1.2f), new Color(0.8f, 0.3f, 1.8f), new Color(0.3f, 0.5f, 1.8f) },
                new[] { new Color(0.3f, 1.4f, 1.6f), new Color(0.2f, 0.9f, 1.3f), new Color(0.6f, 0.5f, 1.8f) },
                new[] { new Color(1.8f, 0.7f, 0.3f), new Color(1.6f, 0.4f, 0.8f), new Color(0.9f, 0.3f, 1.4f) },
            };
            var nebulaAxes = new[] { new Vector3(0.9f, 0.25f, 0.2f), new Vector3(-0.3f, 0.35f, 0.9f), new Vector3(0.6f, 0.45f, -0.65f) };
            for (var i = 0; i < nebulaPalettes.Length; i++)
            {
                var mesh = SaveMesh(Nebula(nebulaAxes[i].normalized, nebulaPalettes[i], 0.045f, 100 + i), $"Ciel_Nebuleuse_{i + 1}");
                AddMesh(Child(nebulae.transform, $"Nébuleuse {i + 1}"), mesh, materials.Glow, -9);
            }

            var milkyWay = new[] { new Color(0.8f, 0.8f, 1.1f), new Color(1f, 0.9f, 0.8f), new Color(0.7f, 0.75f, 1.2f) };
            AddMesh(Child(nebulae.transform, "Voie lactée"), SaveMesh(Nebula(k_MilkyWayAxis, milkyWay, 0.03f, 200), "Ciel_Voie_Lactee"),
                    materials.Glow, -9);

            var galaxies = Child(sky.transform, "Galaxies");
            var galaxyMeshes = new[]
            {
                SaveMesh(Galaxy(new Color(2.6f, 2.2f, 1.8f), new Color(0.6f, 0.9f, 2.4f), 2, 11), "Ciel_Galaxie_A"),
                SaveMesh(Galaxy(new Color(2.6f, 1.6f, 2.2f), new Color(2.2f, 0.5f, 1.8f), 3, 12), "Ciel_Galaxie_B"),
                SaveMesh(Galaxy(new Color(2.6f, 2f, 1.3f), new Color(2.3f, 1.1f, 0.6f), 2, 13), "Ciel_Galaxie_C"),
                SaveMesh(Galaxy(new Color(2.3f, 2.1f, 2.6f), new Color(1.1f, 0.6f, 2.4f), 4, 14), "Ciel_Galaxie_D"),
            };
            var random = new System.Random(23);
            const int galaxyCount = 6;
            for (var i = 0; i < galaxyCount; i++)
            {
                var yaw = i * 360f / galaxyCount + Range(random, -15f, 15f);
                var pitch = Range(random, 22f, 66f);
                var direction = Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;
                var size = Mathf.Min(Range(random, 75f, 140f), k_GalaxyDistance * Mathf.Sin(pitch * Mathf.Deg2Rad) * 0.85f);
                var galaxy = Child(galaxies.transform, $"Galaxie {i + 1}", direction * k_GalaxyDistance);
                galaxy.transform.localRotation = Quaternion.LookRotation(direction) *
                                                 Quaternion.Euler(Range(random, -35f, 35f), Range(random, -35f, 35f), Range(random, 0f, 360f));
                galaxy.transform.localScale = Vector3.one * size;
                AddMesh(galaxy, galaxyMeshes[i % galaxyMeshes.Length], materials.Glow, -8);
            }

            // Le sol miroir : un disque sombre et translucide, une grille discrète, et le sol où l'on marche.
            var floor = Child(root, "Sol miroir");
            var walk = floor.AddComponent<BoxCollider>();
            walk.center = new Vector3(0f, -0.5f, 0f);
            walk.size = new Vector3(80f, 1f, 80f);
            AddMesh(Child(floor.transform, "Disque"), SaveMesh(Disc(130f), "Sol_Disque"), materials.Floor, -2);
            AddMesh(Child(floor.transform, "Grille"), SaveMesh(Grid(30f, 3f), "Sol_Grille"), materials.Grid, -1);

            // Les torii, en désordre : ceux de l'épreuve (le monstre se cache derrière leurs piliers), puis jusqu'à l'horizon.
            var toriiRoot = Child(root, "Torii");
            var arena = Child(toriiRoot.transform, "Torii de l'épreuve");
            var far = Child(toriiRoot.transform, "Torii lointains");
            var placed = new List<Vector2>();
            var arenaCount = Scatter(torii, arena.transform, placed, new System.Random(5), 5f, 20.5f, 4000, _ => 8.5f,
                                     _ => 1.1f);
            var farCount = Scatter(torii, far.transform, placed, new System.Random(6), 27f, 100f, 8000, r => 9f + 0.15f * r,
                                   r => Mathf.Lerp(1.1f, 2.4f, (r - 27f) / 73f));

            // Les murs invisibles, en cercle : on ne sort pas de l'épreuve en marchant (les flèches les traversent).
            var walls = Child(root, "Murs invisibles");
            const int segments = 24;
            for (var i = 0; i < segments; i++)
            {
                var rotation = Quaternion.Euler(0f, i * 360f / segments, 0f);
                var wall = Child(walls.transform, $"Mur {i + 1}", rotation * new Vector3(0f, 3f, k_ArenaRadius));
                wall.transform.localRotation = rotation;
                wall.AddComponent<BoxCollider>().size = new Vector3(2f * Mathf.PI * k_ArenaRadius / segments + 0.5f, 6f, 1f);
            }

            var lightObject = Child(root, "Lumière des étoiles");
            lightObject.transform.localRotation = Quaternion.Euler(38f, 25f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.7f, 0.6f, 1f);
            light.intensity = 0.9f;
            light.shadows = LightShadows.None;

            var settings = new SerializedObject(dimension);
            var music = Child(root, "Musique");
            var source = music.AddComponent<AudioSource>();
            source.clip = settings.FindProperty("m_Music").objectReferenceValue as AudioClip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.pitch = 0.9f;
            source.volume = 0f;
            music.AddComponent<AudioLowPassFilter>().cutoffFrequency = 500f;

            // Le reflet : nébuleuses, galaxies et torii, retournés sous le sol.
            var mirror = Child(root, "Reflet").AddComponent<DimensionMirror>();
            var mirrorSettings = new SerializedObject(mirror);
            var sources = mirrorSettings.FindProperty("m_Sources");
            sources.arraySize = 3;
            sources.GetArrayElementAtIndex(0).objectReferenceValue = nebulae.transform;
            sources.GetArrayElementAtIndex(1).objectReferenceValue = galaxies.transform;
            sources.GetArrayElementAtIndex(2).objectReferenceValue = toriiRoot.transform;
            mirrorSettings.ApplyModifiedPropertiesWithoutUndo();

            settings.FindProperty("m_Spawn").objectReferenceValue = spawn.transform;
            settings.FindProperty("m_Torii").objectReferenceValue = toriiRoot.transform;
            settings.FindProperty("m_Walls").objectReferenceValue = walls.transform;
            settings.FindProperty("m_MusicSource").objectReferenceValue = source;
            settings.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"Dimension des étoiles : {arenaCount} torii dans la zone de l'épreuve, {farCount} jusqu'à l'horizon.");
            return light;
        }

        // Des torii semés au hasard dans un anneau (tirages successifs, écartés d'au moins minimumGap(r) les uns des
        // autres), tournés n'importe comment, de tailles variées, certains penchés.
        static int Scatter(GameObject prefab, Transform parent, List<Vector2> placed, System.Random random, float inner, float outer,
                           int attempts, System.Func<float, float> minimumGap, System.Func<float, float> scaleAt)
        {
            var count = 0;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var angle = Range(random, 0f, Mathf.PI * 2f);
                var radius = Mathf.Sqrt(Range(random, inner * inner, outer * outer));
                var point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var gap = minimumGap(radius);
                var free = true;
                foreach (var other in placed)
                {
                    if ((other - point).sqrMagnitude < gap * gap)
                    {
                        free = false;
                        break;
                    }
                }

                if (!free)
                    continue;

                placed.Add(point);
                count++;
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
                instance.transform.SetParent(parent, false);
                instance.name = $"Torii {count}";
                instance.transform.localPosition = new Vector3(point.x, 0f, point.y);
                var tilt = random.NextDouble() < 0.3
                    ? Quaternion.Euler(Range(random, -9f, 9f), 0f, Range(random, -9f, 9f))
                    : Quaternion.identity;
                instance.transform.localRotation = tilt * Quaternion.Euler(0f, Range(random, 0f, 360f), 0f);
                instance.transform.localScale = Vector3.one * (scaleAt(radius) * Range(random, 0.8f, 1.2f));
            }

            return count;
        }

        static void ApplyEnvironment(Material sky, Light sun)
        {
            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.3f, 0.2f, 0.48f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.1f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.05f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0035f;
            RenderSettings.fogColor = new Color(0.03f, 0.01f, 0.07f);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }

        static GameObject Child(Transform parent, string name, Vector3 localPosition = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go;
        }

        static MeshRenderer AddMesh(GameObject go, Mesh mesh, Material material, int sortingOrder)
        {
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        static float Gaussian(System.Random random)
        {
            var u1 = 1.0 - random.NextDouble();
            var u2 = random.NextDouble();
            return (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2));
        }

        static Vector3 RandomDirection(System.Random random)
        {
            var z = Range(random, -1f, 1f);
            var angle = Range(random, 0f, Mathf.PI * 2f);
            var r = Mathf.Sqrt(1f - z * z);
            return new Vector3(r * Mathf.Cos(angle), z, r * Mathf.Sin(angle));
        }

        // ---------------------------------------------------------------- le ciel

        // Des étoiles rondes tout autour (dessous compris), plus serrées le long de la Voie lactée.
        static Mesh Stars()
        {
            var random = new System.Random(41);
            var bandU = Vector3.Cross(k_MilkyWayAxis, Vector3.up).normalized;
            var bandV = Vector3.Cross(k_MilkyWayAxis, bandU);
            var builder = new SpriteBuilder();
            const int field = 6000;
            const int band = 3000;
            for (var i = 0; i < field + band; i++)
            {
                Vector3 direction;
                if (i < field)
                {
                    direction = RandomDirection(random);
                }
                else
                {
                    var angle = Range(random, 0f, Mathf.PI * 2f);
                    direction = (Mathf.Cos(angle) * bandU + Mathf.Sin(angle) * bandV + k_MilkyWayAxis * (Gaussian(random) * 0.08f)).normalized;
                }

                var brightness = Mathf.Pow((float)random.NextDouble(), 3f);
                var size = i < field ? Mathf.Lerp(1.6f, 4.5f, brightness) : Mathf.Lerp(1.3f, 3f, brightness);
                var tint = random.NextDouble();
                var color = tint < 0.6 ? new Color(1f, 1f, 1f) : tint < 0.82 ? new Color(0.72f, 0.84f, 1f) : new Color(1f, 0.85f, 0.65f);
                color *= (i < field ? 0.7f : 0.45f) + 5f * brightness;
                color.a = 1f;
                builder.Billboard(direction * k_SkyRadius, size, color, 1f);
            }

            return builder.ToMesh();
        }

        // Une nébuleuse qui traverse le ciel d'un horizon à l'autre : des nuages ronds et flous le long d'un grand arc qui
        // ondule, aux couleurs changeantes, avec quelques nœuds plus brillants.
        static Mesh Nebula(Vector3 axis, Color[] palette, float alpha, int seed)
        {
            var random = new System.Random(seed);
            var horizontal = Vector3.Cross(axis, Vector3.up);
            if (horizontal.sqrMagnitude < 1e-4f)
                horizontal = Vector3.right;
            horizontal.Normalize();
            var over = Vector3.Cross(axis, horizontal).normalized;
            if (over.y < 0f)
                over = -over;

            var phase = Range(random, 0f, 10f);
            var builder = new SpriteBuilder();
            const int steps = 150;
            for (var step = 0; step <= steps; step++)
            {
                var t = Mathf.Lerp(-0.04f, Mathf.PI + 0.04f, step / (float)steps);
                var wander = 0.06f * Mathf.Sin(t * 3.1f + phase) + 0.035f * Mathf.Sin(t * 7.3f + phase * 2f);
                var colorT = 0.5f + 0.5f * Mathf.Sin(t * 2.2f + phase);
                var color = colorT < 0.5f ? Color.Lerp(palette[0], palette[1], colorT * 2f) : Color.Lerp(palette[1], palette[2], colorT * 2f - 1f);
                var width = 0.07f + 0.04f * Mathf.Sin(t * 4.7f + phase * 3f);
                for (var k = 0; k < 4; k++)
                {
                    var spread = Gaussian(random) * width;
                    var along = Range(random, -0.01f, 0.01f);
                    var direction = (horizontal * Mathf.Cos(t + along) + over * Mathf.Sin(t + along) + axis * (wander + spread)).normalized;
                    if (direction.y < -0.02f)
                        continue;
                    var tint = color * Range(random, 0.7f, 1.1f);
                    tint.a = alpha * Range(random, 0.7f, 1.3f);
                    builder.Billboard(direction * k_NebulaRadius, Range(random, 10f, 32f), tint, 0f);
                }

                // Un nœud plus brillant, de temps en temps.
                if (random.NextDouble() < 0.15)
                {
                    var direction = (horizontal * Mathf.Cos(t) + over * Mathf.Sin(t) + axis * (wander + Gaussian(random) * width * 0.5f)).normalized;
                    if (direction.y >= 0f)
                    {
                        var knot = Color.Lerp(color, Color.white, 0.3f) * 1.3f;
                        knot.a = alpha * 2.5f;
                        builder.Billboard(direction * (k_NebulaRadius - 2f), Range(random, 5f, 11f), knot, 0f);
                    }
                }
            }

            return builder.ToMesh();
        }

        // Une galaxie spirale de rayon 1, à plat (plan XY) : un halo, un cœur éclatant, des bras de nuages ronds et
        // d'étoiles, de plus en plus colorés vers l'extérieur.
        static Mesh Galaxy(Color core, Color arm, int arms, int seed)
        {
            var random = new System.Random(seed);
            var builder = new SpriteBuilder();
            var right = Vector3.right;
            var up = Vector3.up;

            var halo = arm * 0.35f;
            halo.a = 0.3f;
            builder.Add(Vector3.zero, right * 1.1f, up * 1.1f, halo, 0f);

            for (var i = 0; i < 450; i++)
            {
                var t = Mathf.Sqrt((float)random.NextDouble());
                var angle = t * Mathf.PI * 3.2f + (i % arms) * Mathf.PI * 2f / arms;
                var jitter = new Vector2(Gaussian(random), Gaussian(random)) * (0.07f * (1f - 0.4f * t));
                var position = new Vector3(Mathf.Cos(angle) * t + jitter.x, Mathf.Sin(angle) * t + jitter.y, 0f);
                var color = Color.Lerp(core, arm, t) * Range(random, 0.6f, 1f);
                color.a = 0.16f;
                var size = Mathf.Lerp(0.12f, 0.06f, t) * Range(random, 0.7f, 1.3f);
                builder.Add(position, right * size, up * size, color, 0f);
            }

            for (var i = 0; i < 800; i++)
            {
                var t = Mathf.Sqrt((float)random.NextDouble());
                var angle = t * Mathf.PI * 3.2f + (i % arms) * Mathf.PI * 2f / arms;
                var jitter = new Vector2(Gaussian(random), Gaussian(random)) * (0.06f * (1f - 0.4f * t));
                var position = new Vector3(Mathf.Cos(angle) * t + jitter.x, Mathf.Sin(angle) * t + jitter.y, -0.002f);
                var color = Color.Lerp(Color.white, arm, t * 0.6f) * Range(random, 0.8f, 2.2f);
                color.a = 1f;
                var size = Range(random, 0.012f, 0.028f);
                builder.Add(position, right * size, up * size, color, 1f);
            }

            var glow = core;
            glow.a = 0.9f;
            builder.Add(new Vector3(0f, 0f, -0.004f), right * 0.42f, up * 0.42f, glow, 0f);
            var heart = core * 1.4f;
            heart.a = 1f;
            builder.Add(new Vector3(0f, 0f, -0.006f), right * 0.14f, up * 0.14f, heart, 0f);
            return builder.ToMesh();
        }

        // ---------------------------------------------------------------- le sol

        static Mesh Disc(float radius)
        {
            var mesh = new Mesh();
            const int sides = 96;
            var vertices = new List<Vector3> { Vector3.zero };
            var triangles = new List<int>();
            for (var i = 0; i < sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
                triangles.Add(0);
                triangles.Add(1 + (i + 1) % sides);
                triangles.Add(1 + i);
            }

            var colors = new List<Color>();
            foreach (var _ in vertices)
                colors.Add(Color.white);
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh Grid(float extent, float spacing)
        {
            var builder = new Archery.FinalBoss.MeshBuilder();
            const float half = 0.015f;
            for (var x = -extent; x <= extent + 0.01f; x += spacing)
            {
                builder.Quad(new Vector3(x - half, 0.01f, -extent), new Vector3(x - half, 0.01f, extent),
                             new Vector3(x + half, 0.01f, extent), new Vector3(x + half, 0.01f, -extent), Color.white);
                builder.Quad(new Vector3(-extent, 0.01f, x - half), new Vector3(extent, 0.01f, x - half),
                             new Vector3(extent, 0.01f, x + half), new Vector3(-extent, 0.01f, x + half), Color.white);
            }

            return builder.ToMesh("Grille");
        }

        // ---------------------------------------------------------------- fabriques de maillages

        // Des carrés de lumière (Archery/StarSprite) : uv = coin du carré, uv2.x = netteté (0 flou, 1 étoile).
        sealed class SpriteBuilder
        {
            readonly List<Vector3> m_Vertices = new List<Vector3>();
            readonly List<Color> m_Colors = new List<Color>();
            readonly List<Vector2> m_Uvs = new List<Vector2>();
            readonly List<Vector2> m_Shapes = new List<Vector2>();
            readonly List<int> m_Triangles = new List<int>();

            public void Add(Vector3 center, Vector3 right, Vector3 up, Color color, float sharpness)
            {
                var start = m_Vertices.Count;
                m_Vertices.Add(center - right - up);
                m_Vertices.Add(center - right + up);
                m_Vertices.Add(center + right + up);
                m_Vertices.Add(center + right - up);
                m_Uvs.Add(new Vector2(0f, 0f));
                m_Uvs.Add(new Vector2(0f, 1f));
                m_Uvs.Add(new Vector2(1f, 1f));
                m_Uvs.Add(new Vector2(1f, 0f));
                for (var i = 0; i < 4; i++)
                {
                    m_Colors.Add(color);
                    m_Shapes.Add(new Vector2(sharpness, 0f));
                }

                m_Triangles.Add(start);
                m_Triangles.Add(start + 1);
                m_Triangles.Add(start + 2);
                m_Triangles.Add(start);
                m_Triangles.Add(start + 2);
                m_Triangles.Add(start + 3);
            }

            // Un carré tourné vers le centre du ciel.
            public void Billboard(Vector3 center, float size, Color color, float sharpness)
            {
                var direction = center.normalized;
                var reference = Mathf.Abs(direction.y) > 0.9f ? Vector3.right : Vector3.up;
                var right = Vector3.Cross(direction, reference).normalized * size;
                var up = Vector3.Cross(direction, right).normalized * size;
                Add(center, right, up, color, sharpness);
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh();
                if (m_Vertices.Count > 65000)
                    mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(m_Vertices);
                mesh.SetColors(m_Colors);
                mesh.SetUVs(0, m_Uvs);
                mesh.SetUVs(1, m_Shapes);
                mesh.SetTriangles(m_Triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        // Des volumes éclairés : cylindres lisses et boîtes à faces plates.
        sealed class LitBuilder
        {
            readonly List<Vector3> m_Vertices = new List<Vector3>();
            readonly List<Vector3> m_Normals = new List<Vector3>();
            readonly List<Vector2> m_Uvs = new List<Vector2>();
            readonly List<int> m_Triangles = new List<int>();

            public void Cylinder(Vector3 bottom, float radius, float height, int sides)
            {
                var start = m_Vertices.Count;
                for (var i = 0; i <= sides; i++)
                {
                    var angle = i * Mathf.PI * 2f / sides;
                    var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Vertex(bottom + direction * radius, direction, new Vector2(i / (float)sides, 0f));
                    Vertex(bottom + direction * radius + Vector3.up * height, direction, new Vector2(i / (float)sides, 1f));
                }

                for (var i = 0; i < sides; i++)
                {
                    var b0 = start + i * 2;
                    Triangle(b0, b0 + 1, b0 + 3);
                    Triangle(b0, b0 + 3, b0 + 2);
                }

                var top = bottom + Vector3.up * height;
                var center = m_Vertices.Count;
                Vertex(top, Vector3.up, new Vector2(0.5f, 0.5f));
                for (var i = 0; i <= sides; i++)
                {
                    var angle = i * Mathf.PI * 2f / sides;
                    Vertex(top + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius, Vector3.up, new Vector2(0.5f, 0.5f));
                }

                for (var i = 0; i < sides; i++)
                    Triangle(center, center + 2 + i, center + 1 + i);
            }

            public void Box(Vector3 center, Vector3 half, Quaternion rotation)
            {
                Vector3 P(float x, float y, float z) => center + rotation * Vector3.Scale(half, new Vector3(x, y, z));
                Quad(P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1));
                Quad(P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), P(-1, -1, 1));
                Quad(P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), P(-1, -1, -1));
                Quad(P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1));
                Quad(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1));
                Quad(P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1));
            }

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                var normal = Vector3.Cross(b - a, c - a).normalized;
                var start = m_Vertices.Count;
                Vertex(a, normal, new Vector2(0f, 0f));
                Vertex(b, normal, new Vector2(0f, 1f));
                Vertex(c, normal, new Vector2(1f, 1f));
                Vertex(d, normal, new Vector2(1f, 0f));
                Triangle(start, start + 1, start + 2);
                Triangle(start, start + 2, start + 3);
            }

            void Vertex(Vector3 position, Vector3 normal, Vector2 uv)
            {
                m_Vertices.Add(position);
                m_Normals.Add(normal);
                m_Uvs.Add(uv);
            }

            void Triangle(int a, int b, int c)
            {
                m_Triangles.Add(a);
                m_Triangles.Add(b);
                m_Triangles.Add(c);
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh();
                mesh.SetVertices(m_Vertices);
                mesh.SetNormals(m_Normals);
                mesh.SetUVs(0, m_Uvs);
                mesh.SetTriangles(m_Triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
