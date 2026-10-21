using System.Collections.Generic;
using System.Linq;
using Archery.Cyber;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace Archery.EditorTools
{
    /// <summary>
    /// Fabrique la scène du toit cyberpunk (<c>Scenes/Dimension_Cyberpunk</c>, le 2ᵉ easter egg) : le haut d'un immeuble,
    /// la nuit, sous la pluie, au milieu d'une ville fausse et déchue (les immeubles du City Kit de Kenney, assombris, aux
    /// fenêtres néon cyan, roses ou chaudes, posés sur des fûts sombres ; enseignes ; quelques immeubles « à moitié
    /// chargés » en fil de fer), le toit et ses objets (climatiseurs, antennes, réservoir, écrans, lampadaires du
    /// Cyberpunk Game Kit de Quaternius), la zone de 3 m × 3 m du joueur et sa rambarde néon, les postes des tireurs sur
    /// les toits voisins, la musique, le ciel, le brouillard, et la <see cref="CyberDimension"/> avec tous ses réglages.
    /// Les matériaux vont dans <c>Art/Cyber</c>.
    /// </summary>
    /// <remarks>
    /// Se fait tout seul (tant que la scène n'existe pas ou date d'une version plus ancienne du décor, et dès que les
    /// modèles sont importés), sans toucher aux scènes ouvertes : la scène est créée à part, enregistrée puis refermée.
    /// Ensuite, menu <c>Archery > Toit cyberpunk > Reconstruire le décor</c> (la scène est refaite de zéro). La disposition
    /// est tirée au sort, mais toujours la même. Les modèles sont mis à la taille voulue d'après leurs dimensions.
    /// </remarks>
    [InitializeOnLoad]
    static class CyberDimensionBaker
    {
        const string k_ScenePath = "Assets/_Project/Scenes/Dimension_Cyberpunk.unity";
        const string k_ArtFolder = "Assets/_Project/Art/Cyber";
        const string k_CrossbowModel = k_ArtFolder + "/Arbalete.obj";
        const string k_Mixer = "Assets/_Project/Audio/MainMixer.mixer";
        const int k_Seed = 2026;

        // La version du décor, notée dans le .meta de la scène : une scène plus ancienne est refaite toute seule.
        const string k_Version = "decor 2 : Kenney City Kit, Quaternius Cyberpunk Game Kit";

        const string k_CityFolder = "Assets/kenney_city-kit-commercial/Models/FBX format";
        const string k_CityTexture = k_CityFolder + "/Textures/colormap.png";
        const string k_PropsFolder = "Assets/Quaternius_Cyberpunk-Game-Kit/Models";

        // Les fenêtres de la palette du City Kit (vitres bleu pâle, fenêtres gris foncé) : la carte d'émission des immeubles.
        const string k_WindowMask = k_ArtFolder + "/Ville fenetres.png";

        // Les immeubles proches (sur leurs toits, les tireurs) et ceux de l'horizon.
        static readonly string[] k_NearModels =
        {
            "building-a", "building-b", "building-c", "building-d", "building-e", "building-f", "building-g", "building-h",
            "building-i", "building-j", "building-k", "building-l", "building-m", "building-n",
            "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c", "building-skyscraper-d", "building-skyscraper-e",
        };

        static readonly string[] k_FarModels =
        {
            "building-skyscraper-a", "building-skyscraper-b", "building-skyscraper-c", "building-skyscraper-d", "building-skyscraper-e",
            "low-detail-building-a", "low-detail-building-b", "low-detail-building-c", "low-detail-building-d",
            "low-detail-building-e", "low-detail-building-f", "low-detail-building-g", "low-detail-building-h",
            "low-detail-building-i", "low-detail-building-j", "low-detail-building-l", "low-detail-building-m",
            "low-detail-building-wide-a", "low-detail-building-wide-b",
        };

        // Les objets du toit (Quaternius) : modèle, hauteur (m), place, et s'il arrête les flèches (pas les antennes fines).
        static readonly (string Model, float Height, Vector3 Spot, bool Solid)[] k_RoofProps =
        {
            ("Air Conditioner/AC", 1.1f, new Vector3(-6f, 0f, 5f), true),
            ("Tank/Tank", 3f, new Vector3(5.5f, 0f, 6f), true),
            ("Antenna/Antenna_1", 5.5f, new Vector3(6.5f, 0f, -5f), false),
            ("Computer Large/Computer_Large", 2f, new Vector3(-5f, 0f, -6.5f), true),
            ("Ac Stacked/AC_Stacked", 2.2f, new Vector3(-7f, 0f, -1f), true),
            ("TV/TV_1", 1.8f, new Vector3(7f, 0f, 1.5f), true),
            ("Antenna/Antenna_2", 3.5f, new Vector3(0.5f, 0f, 7f), false),
            ("TV/TV_2", 1.8f, new Vector3(-1f, 0f, -7.2f), true),
            ("Streetlight/Light_Street_1", 4.2f, new Vector3(7.7f, 0f, 7.7f), false),
            ("Streetlight/Light_Street_2", 4.2f, new Vector3(-7.7f, 0f, -7.7f), false),
        };

        // Les tours d'écrans (Quaternius) sur quelques toits voisins.
        const string k_ScreenTower = "TV Tower/TV_3";

        // Dimensions des modèles (à leur taille d'origine), mesurées une fois par construction.
        static readonly Dictionary<GameObject, Bounds> s_Sizes = new Dictionary<GameObject, Bounds>();

        // Loin de la carte (et de la dimension des étoiles, à z = 3000) et haut : rien de la carte ne se voit ni ne s'entend.
        static readonly Vector3 k_Origin = new Vector3(0f, 600f, -3000f);

        // Le toit (m) et la zone du joueur.
        const float k_RoofHalf = 9f;
        const float k_ZoneHalf = 1.5f;

        // L'arbalète et les modèles de la ville peuvent s'importer après le chargement de cet outil : on réessaie quand ils
        // arrivent.
        sealed class ModelWatcher : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (!imported.Any(path => path == k_CrossbowModel || path.StartsWith(k_CityFolder) || path.StartsWith(k_PropsFolder)))
                    return;
                EditorApplication.delayCall -= AutoBuild;
                EditorApplication.delayCall += AutoBuild;
            }
        }

        static CyberDimensionBaker()
        {
            EditorApplication.delayCall -= AutoBuild;
            EditorApplication.delayCall += AutoBuild;
            // Au retour du mode Play aussi : compilé en entrant en Play, l'outil n'avait pas pu construire la scène.
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode)
                    return;
                EditorApplication.delayCall -= AutoBuild;
                EditorApplication.delayCall += AutoBuild;
            };
        }

        static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;
            var importer = AssetImporter.GetAtPath(k_ScenePath);
            if (importer != null && importer.userData == k_Version)
            {
                AddToBuild();
                return;
            }

            // Les modèles pas encore importés : on attend qu'ils arrivent (ModelWatcher).
            if (AssetDatabase.LoadAssetAtPath<GameObject>(k_CrossbowModel) == null || CityModel(k_NearModels[0]) == null ||
                PropModel(k_RoofProps[0].Model) == null)
                return;

            var open = SceneManager.GetSceneByPath(k_ScenePath);
            if (open.IsValid() && open.isLoaded)
                return;
            Build();
        }

        [MenuItem("Archery/Toit cyberpunk/Reconstruire le décor")]
        static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Toit cyberpunk : quitte le mode Play pour reconstruire le décor.");
                return;
            }

            var open = SceneManager.GetSceneByPath(k_ScenePath);
            if (open.IsValid() && open.isLoaded)
            {
                Debug.LogWarning("Toit cyberpunk : ferme la scène Dimension_Cyberpunk avant de la reconstruire.");
                return;
            }

            EnsureFolder();
            s_Sizes.Clear();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                BuildScene();
                EditorSceneManager.SaveScene(scene, k_ScenePath);
            }
            finally
            {
                if (previous.IsValid())
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }

            AssetDatabase.SaveAssets();
            var importer = AssetImporter.GetAtPath(k_ScenePath);
            if (importer != null && importer.userData != k_Version)
            {
                importer.userData = k_Version;
                importer.SaveAndReimport();
            }

            AddToBuild();
            Debug.Log($"Toit cyberpunk : scène {k_ScenePath} construite (ville du City Kit de Kenney, objets du toit de Quaternius).");
        }

        static void AddToBuild()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(k_ScenePath) == null)
                return;
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == k_ScenePath))
                return;
            scenes.Add(new EditorBuildSettingsScene(k_ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Toit cyberpunk : la scène Dimension_Cyberpunk est ajoutée à la liste des scènes du build.");
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(k_ArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Cyber");
        }

        // ---------------------------------------------------------------- la scène

        static void BuildScene()
        {
            var random = new Random(k_Seed);
            var materials = new Materials();

            var root = new GameObject("Toit cyberpunk").transform;
            root.position = k_Origin;
            var dimension = root.gameObject.AddComponent<CyberDimension>();

            var spawn = Child(root, "Arrivée du joueur", Vector3.zero);

            // Le toit, ses murets, et la zone du joueur entourée de sa rambarde (les flèches traversent la rambarde).
            var rails = Child(root, "Rambarde", Vector3.zero);
            Box(root, "Toit", new Vector3(0f, -0.5f, 0f), new Vector3(k_RoofHalf * 2f, 1f, k_RoofHalf * 2f), materials.Roof, true);
            Box(root, "Immeuble", new Vector3(0f, -31f, 0f), new Vector3(k_RoofHalf * 2f - 0.4f, 60f, k_RoofHalf * 2f - 0.4f), materials.Building, false);
            BuildParapet(rails, materials);
            BuildZone(rails, materials);
            BuildRoofProps(root, materials, random);

            var posts = Child(root, "Postes des tireurs", Vector3.zero);
            BuildCity(root, posts, materials, random);
            BuildRain(root, materials);
            BuildLights(root);

            // La musique de la dimension des étoiles (pour le moment) : elle part à l'arrivée du joueur.
            var musicObject = Child(root, "Musique", Vector3.zero);
            var music = musicObject.gameObject.AddComponent<AudioSource>();
            music.clip = Load<AudioClip>("Assets/_Project/Audio/Music/Sad_Nostalgie_40_Tempo_KLICKAUD.mp3");
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.volume = 0f;
            music.outputAudioMixerGroup = AssetDatabase.LoadAllAssetsAtPath(k_Mixer).OfType<AudioMixerGroup>()
                .FirstOrDefault(group => group.name == "Music");
            musicObject.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 22000f;

            ApplyEnvironment(materials);

            var settings = new SerializedObject(dimension);
            settings.FindProperty("m_Spawn").objectReferenceValue = spawn;
            settings.FindProperty("m_Posts").objectReferenceValue = posts;
            settings.FindProperty("m_Rails").objectReferenceValue = rails;
            settings.FindProperty("m_MusicSource").objectReferenceValue = music;
            settings.FindProperty("m_Music").objectReferenceValue = music.clip;
            settings.FindProperty("m_GlowMaterial").objectReferenceValue = Load<Material>("Assets/_Project/Materials/Effects.mat");
            settings.FindProperty("m_GlitchShader").objectReferenceValue = Load<Shader>("Assets/_Project/Shaders/Glitch.shader");
            settings.FindProperty("m_ShooterModel").objectReferenceValue = Load<GameObject>("Assets/_Project/Prefabs/Training Dummy.prefab");
            settings.FindProperty("m_CrossbowModel").objectReferenceValue = Load<GameObject>(k_CrossbowModel);
            settings.FindProperty("m_CrossbowMaterial").objectReferenceValue = materials.Crossbow;
            SetClips(settings.FindProperty("m_GlitchClips"), "Assets/kenney_interfaceSounds/Audio/glitch_001.ogg",
                     "Assets/kenney_interfaceSounds/Audio/glitch_002.ogg", "Assets/kenney_interfaceSounds/Audio/glitch_003.ogg",
                     "Assets/kenney_interfaceSounds/Audio/glitch_004.ogg");
            Clip(settings, "m_PanelOpenClip", "Assets/kenney_interfaceSounds/Audio/maximize_003.ogg");
            Clip(settings, "m_PanelCloseClip", "Assets/kenney_interfaceSounds/Audio/minimize_003.ogg");
            Clip(settings, "m_ShotClip", "Assets/_Project/Audio/Placeholder/enemy_cast.wav");
            Clip(settings, "m_PassClip", "Assets/_Project/Audio/Placeholder/arrow_flight_loop.wav");
            Clip(settings, "m_HitClip", "Assets/kenney_impact-sounds/Audio/impactGlass_heavy_000.ogg");
            Clip(settings, "m_KillClip", "Assets/_Project/Audio/Placeholder/projectile_break.wav");
            Clip(settings, "m_CrossbowClip", "Assets/_Project/Audio/Placeholder/bow_release.wav");
            Clip(settings, "m_SuccessClip", "Assets/kenney_interfaceSounds/Audio/confirmation_004.ogg");
            Clip(settings, "m_FailClip", "Assets/kenney_interfaceSounds/Audio/error_008.ogg");
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // Les murets au bord du toit, avec une bande néon sur le dessus.
        static void BuildParapet(Transform rails, Materials materials)
        {
            const float height = 0.7f;
            for (var side = 0; side < 4; side++)
            {
                var rotation = Quaternion.Euler(0f, side * 90f, 0f);
                var position = rotation * new Vector3(0f, height * 0.5f, k_RoofHalf - 0.15f);
                var wall = Box(rails, "Muret", position, new Vector3(k_RoofHalf * 2f, height, 0.3f), materials.Roof, true);
                wall.localRotation = rotation;
                var neon = Box(wall, "Néon", new Vector3(0f, 0.52f, 0f), new Vector3(1f, 0.04f, 0.4f), materials.NeonPink, false);
                neon.localScale = new Vector3(1f, 0.05f, 1.1f);
            }
        }

        // La zone du joueur : 3 m × 3 m, une ligne néon au sol et une rambarde à hauteur de hanche (poteaux et barres).
        static void BuildZone(Transform rails, Materials materials)
        {
            const float railHeight = 1f;
            var zone = Child(rails, "Zone du joueur", Vector3.zero);
            for (var side = 0; side < 4; side++)
            {
                var rotation = Quaternion.Euler(0f, side * 90f, 0f);
                var floorLine = Box(zone, "Ligne au sol", rotation * new Vector3(0f, 0.01f, k_ZoneHalf), new Vector3(k_ZoneHalf * 2f, 0.02f, 0.05f),
                                    materials.NeonCyan, false);
                floorLine.localRotation = rotation;
                var bar = Box(zone, "Barre", rotation * new Vector3(0f, railHeight, k_ZoneHalf + 0.05f), new Vector3(k_ZoneHalf * 2f + 0.1f, 0.05f, 0.05f),
                              materials.NeonCyan, true);
                bar.localRotation = rotation;
                var middle = Box(zone, "Barre basse", rotation * new Vector3(0f, railHeight * 0.5f, k_ZoneHalf + 0.05f),
                                 new Vector3(k_ZoneHalf * 2f + 0.1f, 0.03f, 0.03f), materials.Metal, true);
                middle.localRotation = rotation;
                for (var post = -1; post <= 1; post++)
                {
                    var position = rotation * new Vector3(post * k_ZoneHalf, railHeight * 0.5f, k_ZoneHalf + 0.05f);
                    Box(zone, "Poteau", position, new Vector3(0.06f, railHeight, 0.06f), materials.Metal, true);
                }
            }
        }

        // Climatiseurs, réservoir, antennes (et leur lumière rouge), armoire, écrans et lampadaires de Quaternius, hors de la
        // zone du joueur, tournés vers elle.
        static void BuildRoofProps(Transform root, Materials materials, Random random)
        {
            var props = Child(root, "Objets du toit", Vector3.zero);
            foreach (var (path, height, spot, solid) in k_RoofProps)
            {
                var model = PropModel(path);
                if (model == null)
                    continue;

                var size = Measure(model).size;
                var scale = height / Mathf.Max(0.01f, size.y);
                var facing = Quaternion.LookRotation(new Vector3(-spot.x, 0f, -spot.z));
                var prop = PlaceModel(model, props, spot, facing, scale, 0f, null);
                if (solid)
                {
                    var box = prop.gameObject.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, height * 0.5f, 0f);
                    box.size = size * scale;
                }

                if (path.StartsWith("Antenna/"))
                    Box(props, "Lumière", spot + Vector3.up * (height + 0.08f), Vector3.one * 0.16f, materials.NeonRed, false);
            }
        }

        // La ville : un anneau d'immeubles proches (sur leurs toits, les postes des tireurs), puis l'horizon.
        static void BuildCity(Transform root, Transform posts, Materials materials, Random random)
        {
            var city = Child(root, "Ville", Vector3.zero);
            var words = new[] { "THUNK", "ERREUR", "404", "NÉANT", "COPIE", "OUBLI", "VIDE", "ARCADE", "REFLET", "MIROIR" };
            var word = 0;

            // L'anneau proche : 14 immeubles, toits entre 4 m sous le nôtre et 12 m au-dessus.
            const int near = 14;
            for (var i = 0; i < near; i++)
            {
                var angle = (i + Range(random, -0.25f, 0.25f)) * 360f / near;
                var distance = Range(random, 15f, 24f);
                var width = Range(random, 6f, 9f);
                var top = Range(random, -4f, 12f);
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var center = direction * distance;
                var facing = Quaternion.LookRotation(-direction);
                var model = CityModel(k_NearModels[random.Next(k_NearModels.Length)]);
                Vector3 size;
                Transform building;
                if (model != null)
                {
                    building = CityBuilding(city, model, center, width, top, facing, materials.City[random.Next(materials.City.Length)],
                                            materials, random, out size);
                }
                else
                {
                    size = new Vector3(width, 0f, width);
                    building = Building(city, center, width, width, top, materials, random, true);
                    building.localRotation = facing;
                }

                // Le toit, pour y poser les tireurs : des colliders le temps de le trouver.
                var roof = new RoofFinder(building);

                // Un ou deux postes près du bord qui fait face au toit du joueur, tournés vers lui.
                var count = random.NextDouble() < 0.6 ? 2 : 1;
                for (var p = 0; p < count; p++)
                {
                    var along = count == 1 ? 0f : (p == 0 ? -1f : 1f) * size.x * 0.25f;
                    var local = new Vector3(along, 0f, size.z * 0.5f - 1.1f);
                    var position = center + facing * local;
                    position.y = roof.HeightAt(root, position, top);
                    var post = Child(posts, "Poste", position);
                    post.localRotation = facing;
                }

                // Sur un toit sur trois, une tour d'écrans au fond.
                var tower = i % 3 == 1 ? PropModel(k_ScreenTower) : null;
                if (tower != null)
                {
                    var spot = center + facing * new Vector3(0f, 0f, -size.z * 0.2f);
                    spot.y = roof.HeightAt(root, spot, top);
                    var towerScale = Range(random, 5f, 7f) / Mathf.Max(0.01f, Measure(tower).size.y);
                    PlaceModel(tower, city, spot, facing, towerScale, 0f, null);
                }

                roof.Dispose();
                if (random.NextDouble() < 0.5)
                {
                    var y = top - Range(random, 2.5f, Mathf.Clamp(size.y - 1f, 3f, 8f));
                    Sign(building, words[word++ % words.Length], y, size.x, size.z * 0.5f, materials, random);
                }
            }

            // L'horizon : de grands immeubles sombres aux fenêtres allumées, et quelques-uns à moitié chargés (en fil de fer).
            const int far = 46;
            for (var i = 0; i < far; i++)
            {
                var angle = Range(random, 0f, 360f);
                var distance = Range(random, 32f, 120f);
                var center = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
                var width = Range(random, 8f, 18f);
                var top = Range(random, -15f, 55f);
                if (random.NextDouble() < 0.12)
                {
                    Wireframe(city, center, width, top, materials);
                    continue;
                }

                var facing = Quaternion.Euler(0f, Range(random, 0f, 90f), 0f);
                var name = k_FarModels[random.Next(k_FarModels.Length)];
                var model = CityModel(name);
                Transform building;
                Vector3 size;
                if (model != null)
                {
                    building = CityBuilding(city, model, center, width, top, facing, materials.City[random.Next(materials.City.Length)],
                                            materials, random, out size);

                    // Les immeubles simples (des blocs blancs, sans fenêtres) : des bandes de fenêtres néon sur leurs faces.
                    if (name.StartsWith("low-detail"))
                        Bands(building, size.x, size.z, top - 1f, top - size.y, 4, materials, random);
                }
                else
                {
                    size = new Vector3(width, 0f, width);
                    building = Building(city, center, width, width, top, materials, random, false);
                    building.localRotation = facing;
                }

                if (random.NextDouble() < 0.25)
                    Sign(building, words[word++ % words.Length], top - Range(random, 3f, 9f), size.x, size.z * 0.5f, materials, random);
            }
        }

        // Un immeuble du City Kit, assombri, aux fenêtres néon (« skin »), mis à la largeur voulue, son toit à « top » ; dessous,
        // un fût sombre jusqu'au fond de la ville, avec des bandes de fenêtres. « size » : ses dimensions (largeur, hauteur du
        // modèle, profondeur), dans le repère de l'immeuble (z vers « facing »).
        static Transform CityBuilding(Transform parent, GameObject model, Vector3 center, float width, float top, Quaternion facing,
                                      Material skin, Materials materials, Random random, out Vector3 size)
        {
            const float bottom = -60f;
            var bounds = Measure(model);
            var scale = width / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.z));
            size = bounds.size * scale;
            var building = Child(parent, "Immeuble", new Vector3(center.x, 0f, center.z));
            building.localRotation = facing;
            var baseY = top - size.y;
            PlaceModel(model, building, new Vector3(0f, baseY, 0f), Quaternion.identity, scale, random.NextDouble() < 0.5 ? 0f : 180f, skin);

            if (baseY > bottom + 0.5f)
            {
                var trunk = baseY - bottom;
                Box(building, "Fût", new Vector3(0f, bottom + trunk * 0.5f + 0.02f, 0f), new Vector3(size.x * 0.96f, trunk, size.z * 0.96f),
                    materials.Building, false);
                Bands(building, size.x * 0.96f, size.z * 0.96f, baseY - 1f, Mathf.Max(bottom + 2f, baseY - 30f), 3, materials, random);
            }

            return building;
        }

        // Des bandes de fenêtres allumées sur les quatre faces d'un bloc (largeur × profondeur), entre « high » et « low ».
        static void Bands(Transform building, float width, float depth, float high, float low, int bands, Materials materials, Random random)
        {
            if (high - low < 1f)
                return;

            for (var face = 0; face < 4; face++)
            {
                var rotation = Quaternion.Euler(0f, face * 90f, 0f);
                var half = face % 2 == 0 ? depth * 0.5f : width * 0.5f;
                var span = face % 2 == 0 ? width : depth;
                for (var b = 0; b < bands; b++)
                {
                    if (random.NextDouble() < 0.35)
                        continue;
                    var y = Range(random, low, high);
                    var length = span * Range(random, 0.3f, 0.9f);
                    var shift = Range(random, -(span - length) * 0.5f, (span - length) * 0.5f);
                    var band = Box(building, "Fenêtres", rotation * new Vector3(shift, y, half + 0.03f), new Vector3(length, Range(random, 0.25f, 0.7f), 0.05f),
                                   materials.Windows[random.Next(materials.Windows.Length)], false);
                    band.localRotation = rotation;
                }
            }
        }

        // Trouve la hauteur du toit d'un immeuble (le dessus de ce qui s'y trouve) en un point, avec des colliders posés le
        // temps de la recherche (les immeubles n'en gardent pas).
        sealed class RoofFinder : System.IDisposable
        {
            readonly List<Collider> m_Colliders = new List<Collider>();

            public RoofFinder(Transform building)
            {
                foreach (var filter in building.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                        continue;
                    var collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                    m_Colliders.Add(collider);
                }

                Physics.SyncTransforms();
            }

            // « local » : dans le repère du toit cyberpunk ; à défaut de toit trouvé, « fallback ».
            public float HeightAt(Transform root, Vector3 local, float fallback)
            {
                var origin = root.TransformPoint(new Vector3(local.x, fallback + 6f, local.z));
                var best = float.MaxValue;
                var height = fallback;
                foreach (var hit in Physics.RaycastAll(origin, Vector3.down, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.distance >= best || !m_Colliders.Contains(hit.collider))
                        continue;
                    best = hit.distance;
                    height = root.InverseTransformPoint(hit.point).y;
                }

                return height;
            }

            public void Dispose()
            {
                foreach (var collider in m_Colliders)
                {
                    if (collider != null)
                        Object.DestroyImmediate(collider);
                }

                m_Colliders.Clear();
            }
        }

        // Un immeuble sombre, du fond (60 m sous le toit) jusqu'à « top », avec des bandes de fenêtres néon sur ses faces.
        static Transform Building(Transform parent, Vector3 center, float width, float depth, float top, Materials materials, Random random, bool near)
        {
            const float bottom = -60f;
            var building = Child(parent, "Immeuble", new Vector3(center.x, 0f, center.z));
            var height = top - bottom;
            Box(building, "Corps", new Vector3(0f, bottom + height * 0.5f, 0f), new Vector3(width, height, depth), materials.Building, false);
            Box(building, "Toit", new Vector3(0f, top + 0.15f, 0f), new Vector3(width + 0.2f, 0.3f, depth + 0.2f), materials.Roof, false);

            // Des bandes de fenêtres allumées, sur les quatre faces, de couleurs variées.
            var bands = near ? 6 : 4;
            for (var face = 0; face < 4; face++)
            {
                var rotation = Quaternion.Euler(0f, face * 90f, 0f);
                var half = face % 2 == 0 ? depth * 0.5f : width * 0.5f;
                var span = face % 2 == 0 ? width : depth;
                for (var b = 0; b < bands; b++)
                {
                    if (random.NextDouble() < 0.35)
                        continue;
                    var y = top - Range(random, 1.5f, Mathf.Min(40f, height - 2f));
                    var length = span * Range(random, 0.3f, 0.9f);
                    var shift = Range(random, -(span - length) * 0.5f, (span - length) * 0.5f);
                    var band = Box(building, "Fenêtres", rotation * new Vector3(shift, y, half + 0.03f), new Vector3(length, Range(random, 0.25f, 0.7f), 0.05f),
                                   materials.Windows[random.Next(materials.Windows.Length)], false);
                    band.localRotation = rotation;
                }
            }

            return building;
        }

        // Un immeuble « pas encore chargé » : seulement ses arêtes, en néon cyan.
        static void Wireframe(Transform parent, Vector3 center, float width, float top, Materials materials)
        {
            var frame = Child(parent, "Immeuble non chargé", new Vector3(center.x, 0f, center.z));
            const float bottom = -40f;
            var height = top - bottom;
            var half = width * 0.5f;
            const float bar = 0.12f;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                    Box(frame, "Arête", new Vector3(x * half, bottom + height * 0.5f, z * half), new Vector3(bar, height, bar), materials.NeonCyan, false);
            }

            foreach (var y in new[] { bottom, top, bottom + height * 0.5f })
            {
                for (var side = 0; side < 4; side++)
                {
                    var rotation = Quaternion.Euler(0f, side * 90f, 0f);
                    var edge = Box(frame, "Arête", rotation * new Vector3(0f, y, half), new Vector3(width, bar, bar), materials.NeonCyan, false);
                    edge.localRotation = rotation;
                }
            }
        }

        // Une enseigne : un panneau lumineux et un mot sombre dessus, à la hauteur « y », sur la face qui regarde le toit du
        // joueur (à « front » du centre de l'immeuble).
        static void Sign(Transform building, string text, float y, float width, float front, Materials materials, Random random)
        {
            var size = new Vector2(Mathf.Min(width * 0.8f, 1.1f * text.Length), 2f);
            var board = Box(building, "Enseigne", new Vector3(0f, y, front + 0.3f), new Vector3(size.x, size.y, 0.15f),
                            random.NextDouble() < 0.5 ? materials.NeonPink : materials.NeonCyan, false);
            var label = new GameObject("Texte").AddComponent<TextMeshPro>();
            label.transform.SetParent(board, false);
            label.transform.localPosition = new Vector3(0f, 0f, 0.6f);
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            label.transform.localScale = new Vector3(1f / board.localScale.x, 1f / board.localScale.y, 1f);
            label.rectTransform.sizeDelta = size;
            label.text = text;
            label.fontSize = 14f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 4f;
            label.fontSizeMax = 20f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.04f, 0.02f, 0.06f);
        }

        // La pluie : des traits pâles qui tombent sur toute la zone, et quelques gouttes rouges qui montent (le monde bugue).
        static void BuildRain(Transform root, Materials materials)
        {
            Rain(root, "Pluie", 700f, -14f, new Color(0.55f, 0.7f, 1f, 0.35f), materials.Rain, 14f);
            Rain(root, "Pluie à l'envers", 18f, 6f, new Color(1f, 0.15f, 0.2f, 0.6f), materials.Rain, -1f);
        }

        static void Rain(Transform root, string name, float rate, float speed, Color color, Material material, float height)
        {
            var go = Child(root, name, new Vector3(0f, height, 0f));
            go.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var particles = go.gameObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 1.8f;
            main.startSpeed = 0f;
            main.startSize = 0.025f;
            main.startColor = color;
            main.maxParticles = 2000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = rate;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 30f, 0.5f);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.6f, -0.3f);
            velocity.y = new ParticleSystem.MinMaxCurve(speed * 1.1f, speed * 0.9f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.04f;
            renderer.lengthScale = 1f;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // La lune (bleutée, faible) et deux néons près de la zone, qui éclairent le toit mouillé et les arbalètes.
        static void BuildLights(Transform root)
        {
            var moon = Child(root, "Lune", new Vector3(0f, 30f, 0f)).gameObject.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
            moon.color = new Color(0.55f, 0.65f, 1f);
            moon.intensity = 0.25f;
            moon.shadows = LightShadows.None;
            RenderSettings.sun = moon;

            foreach (var (position, color) in new[] { (new Vector3(-4f, 3f, 3f), new Color(0.2f, 0.9f, 1f)), (new Vector3(4f, 3f, -3f), new Color(1f, 0.2f, 0.75f)) })
            {
                var light = Child(root, "Néon", position).gameObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = color;
                light.range = 10f;
                light.intensity = 2.5f;
                light.shadows = LightShadows.None;
            }
        }

        static void ApplyEnvironment(Materials materials)
        {
            RenderSettings.skybox = materials.Sky;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.06f, 0.11f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.013f;
            RenderSettings.fogColor = new Color(0.06f, 0.03f, 0.09f);
            RenderSettings.reflectionIntensity = 0.5f;
        }

        // ---------------------------------------------------------------- les matériaux

        sealed class Materials
        {
            public readonly Material Roof = Lit("Toit", new Color(0.11f, 0.11f, 0.13f), Color.black, 0.65f);
            public readonly Material Building = Lit("Immeuble", new Color(0.045f, 0.045f, 0.06f), Color.black, 0.3f);
            public readonly Material Metal = Lit("Metal", new Color(0.2f, 0.21f, 0.24f), Color.black, 0.5f);
            public readonly Material Dark = Lit("Sombre", new Color(0.03f, 0.03f, 0.04f), Color.black, 0.2f);
            public readonly Material NeonCyan = Lit("Neon cyan", Color.black, new Color(0.2f, 1.6f, 2.2f), 0.5f);
            public readonly Material NeonPink = Lit("Neon rose", Color.black, new Color(2.2f, 0.25f, 1.5f), 0.5f);
            public readonly Material NeonRed = Lit("Neon rouge", Color.black, new Color(3f, 0.1f, 0.1f), 0.5f);
            public readonly Material[] Windows =
            {
                Lit("Fenetres cyan", Color.black, new Color(0.15f, 0.9f, 1.3f), 0.4f),
                Lit("Fenetres roses", Color.black, new Color(1.3f, 0.2f, 0.9f), 0.4f),
                Lit("Fenetres chaudes", Color.black, new Color(1.4f, 0.8f, 0.3f), 0.4f),
            };
            // Les immeubles du City Kit la nuit : murs sombres, fenêtres allumées.
            public readonly Material[] City =
            {
                CityMaterial("Ville nuit cyan", new Color(0.25f, 1.2f, 1.8f)),
                CityMaterial("Ville nuit rose", new Color(1.6f, 0.25f, 1.2f)),
                CityMaterial("Ville nuit chaude", new Color(1.5f, 0.85f, 0.35f)),
            };
            public readonly Material Rain = RainMaterial();
            public readonly Material Sky = SkyMaterial();
            public readonly Material Crossbow = CrossbowMaterial();
        }

        static Material Lit(string name, Color baseColor, Color emission, float smoothness)
        {
            var material = LoadOrCreate(name, Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_Color", baseColor);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        // La palette du City Kit, très assombrie, et ses fenêtres (le masque) qui brillent de la couleur « windows ».
        static Material CityMaterial(string name, Color windows)
        {
            var material = Lit(name, new Color(0.2f, 0.2f, 0.26f), windows, 0.35f);
            var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(k_CityTexture);
            material.SetTexture("_BaseMap", palette);
            material.SetTexture("_MainTex", palette);
            material.SetTexture("_EmissionMap", WindowMask());
            EditorUtility.SetDirty(material);
            return material;
        }

        // Le masque des fenêtres, sans mipmaps : de loin, les cases voisines de la palette ne s'allument pas.
        static Texture2D WindowMask()
        {
            if (AssetImporter.GetAtPath(k_WindowMask) is TextureImporter importer &&
                (importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Clamp))
            {
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            return Load<Texture2D>(k_WindowMask);
        }

        static Material RainMaterial()
        {
            var glow = Load<Material>("Assets/_Project/Resources/LaserGlow.mat");
            var material = LoadOrCreate("Pluie", glow != null ? glow.shader : Shader.Find("Sprites/Default"));
            material.SetColor("_BaseColor", new Color(0.6f, 0.75f, 1f, 1f));
            material.SetFloat("_Round", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material SkyMaterial()
        {
            var material = LoadOrCreate("Ciel nuit", Shader.Find("Skybox/Procedural"));
            material.SetFloat("_SunDisk", 0f);
            material.EnableKeyword("_SUNDISK_NONE");
            material.DisableKeyword("_SUNDISK_SIMPLE");
            material.DisableKeyword("_SUNDISK_HIGH_QUALITY");
            material.SetFloat("_SunSize", 0f);
            material.SetFloat("_AtmosphereThickness", 0.35f);
            material.SetColor("_SkyTint", new Color(0.28f, 0.12f, 0.38f));
            material.SetColor("_GroundColor", new Color(0.03f, 0.02f, 0.04f));
            material.SetFloat("_Exposure", 0.3f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Les arbalètes : une silhouette sombre au contour néon (cyan ou magenta par tranches), à peine glitchée.
        static Material CrossbowMaterial()
        {
            var material = LoadOrCreate("Arbalete neon", Load<Shader>("Assets/_Project/Shaders/Glitch.shader"));
            material.SetFloat("_Silhouette", 1f);
            material.SetFloat("_Glitch", 0.05f);
            material.SetColor("_ShadowColor", new Color(0.03f, 0.03f, 0.045f, 1f));
            material.SetColor("_RimColor", new Color(0.2f, 1.6f, 2.2f, 1f));
            material.SetColor("_RimColorAlt", new Color(2.1f, 0.25f, 1.6f, 1f));
            material.SetFloat("_RimPower", 2.2f);
            material.SetColor("_Tint", new Color(0.9f, 0.1f, 0.7f, 1f));
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material LoadOrCreate(string name, Shader shader)
        {
            var path = $"{k_ArtFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                if (shader != null && material.shader != shader)
                    material.shader = shader;
                return material;
            }

            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ---------------------------------------------------------------- les briques

        static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        static Transform Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material, bool collider)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            if (!collider)
                Object.DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = size;
            var renderer = box.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            box.isStatic = true;
            return box.transform;
        }

        static GameObject CityModel(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{k_CityFolder}/{name}.fbx");

        static GameObject PropModel(string path) => AssetDatabase.LoadAssetAtPath<GameObject>($"{k_PropsFolder}/{path}.fbx");

        // Les dimensions d'un modèle à sa taille d'origine (avec la rotation et l'échelle de son import), posé à l'origine.
        static Bounds Measure(GameObject model)
        {
            if (s_Sizes.TryGetValue(model, out var known))
                return known;

            var copy = (GameObject)Object.Instantiate(model);
            copy.transform.position = Vector3.zero;
            var renderers = copy.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.one);
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            Object.DestroyImmediate(copy);
            s_Sizes[model] = bounds;
            return bounds;
        }

        // Une copie du modèle, agrandie de « scale », tournée de « yaw » sur elle-même, posée par le milieu de sa base sur
        // « bottomCenter » (dans le repère de « parent »), dans un nœud tourné de « rotation ». « skin » : remplace tous ses
        // matériaux (rien : ceux du modèle). Statique, sans ombres ni colliders.
        static Transform PlaceModel(GameObject model, Transform parent, Vector3 bottomCenter, Quaternion rotation, float scale, float yaw, Material skin)
        {
            var bounds = Measure(model);
            var holder = Child(parent, model.name, bottomCenter);
            holder.localRotation = rotation;

            var copy = (GameObject)Object.Instantiate(model);
            copy.name = model.name;
            var transform = copy.transform;
            var importRotation = transform.localRotation;
            var importScale = transform.localScale;
            transform.SetParent(holder, false);
            var turn = Quaternion.Euler(0f, yaw, 0f);
            transform.localRotation = turn * importRotation;
            transform.localScale = importScale * scale;
            transform.localPosition = turn * (-new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale);

            foreach (var collider in copy.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            foreach (var animator in copy.GetComponentsInChildren<Animator>(true))
                Object.DestroyImmediate(animator);
            foreach (var renderer in copy.GetComponentsInChildren<MeshRenderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (skin != null)
                    renderer.sharedMaterials = Enumerable.Repeat(skin, Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray();
            }

            foreach (var child in copy.GetComponentsInChildren<Transform>(true))
                child.gameObject.isStatic = true;
            holder.gameObject.isStatic = true;
            return holder;
        }

        static void SetClips(SerializedProperty property, params string[] paths)
        {
            var clips = paths.Select(Load<AudioClip>).Where(clip => clip != null).ToArray();
            property.arraySize = clips.Length;
            for (var i = 0; i < clips.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }

        static void Clip(SerializedObject settings, string field, string path) =>
            settings.FindProperty(field).objectReferenceValue = Load<AudioClip>(path);

        static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogWarning($"Toit cyberpunk : {path} introuvable.");
            return asset;
        }

        static float Range(Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
