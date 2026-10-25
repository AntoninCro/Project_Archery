using System.Collections.Generic;
using System.Linq;
using Archery.Cyber;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace Archery.EditorTools
{
    /// <summary>
    /// Fabrique la scène du toit cyberpunk (<c>Scenes/Dimension_Cyberpunk</c>, le 2ᵉ easter egg) : le haut d'un immeuble,
    /// la nuit, sous la pluie, au milieu d'une ville fausse et déchue (les immeubles du City Kit de Kenney, assombris, aux
    /// fenêtres néon, posés sur des fûts sombres ; enseignes ; quelques immeubles « à moitié chargés » en fil de fer), le
    /// toit et ses objets (climatiseurs, antennes, réservoir, écrans, lampadaires du Cyberpunk Game Kit de Quaternius), la
    /// zone de 3 m × 3 m du joueur et sa rambarde néon, les postes des tireurs sur les toits voisins, la musique, le ciel,
    /// le brouillard, et la <see cref="CyberDimension"/> avec tous ses réglages. Les matériaux vont dans <c>Art/Cyber</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Décor 3 (24 octobre, la ville était trop sombre et pas assez cyberpunk) : des néons partout, de quatre couleurs (cyan,
    /// rose, violet, ambre), sur les arêtes et les bords des toits des immeubles proches, qui éclairent leurs façades ;
    /// des enseignes sur tous les immeubles proches, horizontales ou verticales aux lettres néon ; des écrans holographiques
    /// et un mannequin holographique géant qui se tourne vers le joueur ; un brouillard violet ; le toit mouillé qui reflète
    /// les néons (flaques, sonde de reflets), des bouches d'aération qui fument, des guirlandes de lampions, une enseigne
    /// holographique « THUNK » ; les objets de Quaternius rallumés (lumières, écrans, enseignes) ; un post-traitement à part
    /// (plus de halo, des couleurs plus franches). L'ensemble reste sombre : les murs sont presque noirs, la lumière vient
    /// des néons.
    /// </para>
    /// <para>
    /// Décor 4 (24 octobre, après le test) : les voitures volantes du décor 3 retirées ; un ciel étoilé à la façon du mode
    /// Difficile, aux couleurs du cyberpunk (shader <c>Archery/CyberSky</c>) ; les postes des tireurs seulement à découvert
    /// (<see cref="PostVisible"/> : des tireurs étaient cachés par le muret, sur des toits plus bas que le nôtre).
    /// </para>
    /// <para>
    /// Se fait tout seul (tant que la scène n'existe pas ou date d'une version plus ancienne du décor, et dès que les
    /// modèles sont importés), sans toucher aux scènes ouvertes : la scène est créée à part, enregistrée puis refermée.
    /// Ensuite, menu <c>Archery > Toit cyberpunk > Reconstruire le décor</c> (la scène est refaite de zéro). La disposition
    /// est tirée au sort, mais toujours la même. Les modèles sont mis à la taille voulue d'après leurs dimensions.
    /// </para>
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
        const string k_Version = "decor 4 : ciel etoile cyberpunk, tireurs visibles, sans voitures";

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

        // Les mots des enseignes et des écrans.
        static readonly string[] k_Words = { "THUNK", "ERREUR", "404", "NÉANT", "COPIE", "OUBLI", "VIDE", "ARCADE", "REFLET", "MIROIR" };

        // Les quatre couleurs des néons (décor 3) : cyan, rose, violet, ambre (l'ordre de Materials.Neons), et la lumière
        // qu'ils répandent.
        static readonly Color[] k_NeonLights =
        {
            new Color(0.2f, 0.9f, 1f), new Color(1f, 0.2f, 0.75f), new Color(0.6f, 0.25f, 1f), new Color(1f, 0.6f, 0.15f),
        };

        // Le brouillard, violet et sombre ; le bas du ciel a la même couleur, pour que les immeubles lointains s'y fondent.
        static readonly Color k_FogColor = new Color(0.1f, 0.035f, 0.13f);

        // Dimensions des modèles (à leur taille d'origine), mesurées une fois par construction.
        static readonly Dictionary<GameObject, Bounds> s_Sizes = new Dictionary<GameObject, Bounds>();

        // Loin de la carte (et de la dimension des étoiles, à z = 3000) et haut : rien de la carte ne se voit ni ne s'entend.
        static readonly Vector3 k_Origin = new Vector3(0f, 600f, -3000f);

        // Le toit (m), ses murets et la zone du joueur.
        const float k_RoofHalf = 9f;
        const float k_ParapetHeight = 0.7f;
        const float k_ZoneHalf = 1.5f;

        // L'anneau des immeubles proches s'arrête à 29 m du centre (coins compris) ; ceux de l'horizon commencent à 34 m.
        const float k_FarStart = 34f;

        // Les tireurs, vus du milieu de la zone, un peu accroupi : la hauteur des yeux, et celle de la poitrine d'un tireur
        // au-dessus de son poste ; il est assez à découvert si la ligne de vue passe 15 cm au-dessus du muret.
        const float k_SightEye = 1.3f;
        const float k_SightChest = 1.1f;
        const float k_SightClearance = 0.15f;

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
            {
                Debug.LogWarning("Toit cyberpunk : nouveau décor à construire ; ferme la scène Dimension_Cyberpunk, puis " +
                                 "Archery > Toit cyberpunk > Reconstruire le décor.");
                return;
            }

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
            Debug.Log($"Toit cyberpunk : scène {k_ScenePath} construite (décor 4 : néons, hologrammes, ciel étoilé, " +
                      "toit mouillé ; ville du City Kit de Kenney, objets du toit de Quaternius).");
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
            var zone = BuildZone(rails, materials);
            BuildRoofProps(root, materials);
            BuildRoofDetails(root, materials, random);

            var posts = Child(root, "Postes des tireurs", Vector3.zero);
            BuildCity(root, posts, materials, random);
            if (posts.childCount < 16)
                Debug.LogWarning($"Toit cyberpunk : seulement {posts.childCount} postes de tireurs à découvert (il en faut au moins 12).");
            BuildRain(root, materials);
            BuildLights(root);
            var reflections = BuildReflections(root);
            BuildPostProcess(root);

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
            settings.FindProperty("m_Zone").objectReferenceValue = zone;
            settings.FindProperty("m_MusicSource").objectReferenceValue = music;
            settings.FindProperty("m_Music").objectReferenceValue = music.clip;
            settings.FindProperty("m_Reflections").objectReferenceValue = reflections;
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
            Clip(settings, "m_TickClip", "Assets/_Project/Audio/Placeholder/countdown_tick.wav");
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // Les murets au bord du toit, avec une bande néon sur le dessus, d'une couleur par côté.
        static void BuildParapet(Transform rails, Materials materials)
        {
            const float height = k_ParapetHeight;
            for (var side = 0; side < 4; side++)
            {
                var rotation = Quaternion.Euler(0f, side * 90f, 0f);
                var position = rotation * new Vector3(0f, height * 0.5f, k_RoofHalf - 0.15f);
                var wall = Box(rails, "Muret", position, new Vector3(k_RoofHalf * 2f, height, 0.3f), materials.Roof, true);
                wall.localRotation = rotation;
                var neon = Box(wall, "Néon", new Vector3(0f, 0.52f, 0f), new Vector3(1f, 0.04f, 0.4f), materials.Neons[(side + 1) % 4], false);
                neon.localScale = new Vector3(1f, 0.05f, 1.1f);
            }
        }

        // La zone du joueur : 3 m × 3 m, une ligne néon au sol et une rambarde à hauteur de hanche (poteaux et barres).
        static Transform BuildZone(Transform rails, Materials materials)
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

            return zone;
        }

        // Climatiseurs, réservoir, antennes (et leur lumière rouge), armoire, écrans et lampadaires de Quaternius, hors de la
        // zone du joueur, tournés vers elle, rallumés (Materials.Night) ; les lampadaires éclairent autour d'eux.
        static void BuildRoofProps(Transform root, Materials materials)
        {
            var props = Child(root, "Objets du toit", Vector3.zero);
            for (var i = 0; i < k_RoofProps.Length; i++)
            {
                var (path, height, spot, solid) = k_RoofProps[i];
                var model = PropModel(path);
                if (model == null)
                    continue;

                var size = Measure(model).size;
                var scale = height / Mathf.Max(0.01f, size.y);
                var facing = Quaternion.LookRotation(new Vector3(-spot.x, 0f, -spot.z));
                var accent = i % 4;
                var prop = PlaceModel(model, props, spot, facing, scale, 0f, original => materials.Night(original, accent));
                if (solid)
                {
                    var box = prop.gameObject.AddComponent<BoxCollider>();
                    box.center = new Vector3(0f, height * 0.5f, 0f);
                    box.size = size * scale;
                }

                if (path.StartsWith("Antenna/"))
                    Box(props, "Lumière", spot + Vector3.up * (height + 0.08f), Vector3.one * 0.16f, materials.NeonRed, false);
                if (path.StartsWith("Streetlight/"))
                    PointLight(props, "Lumière du lampadaire", spot + Vector3.up * (height - 0.4f), new Color(1f, 0.85f, 0.65f), 7f, 3f);
            }
        }

        // Les détails du toit (décor 3) : des flaques qui reflètent les néons, un cercle de tirets néon autour de la zone,
        // deux bouches d'aération qui fument, deux guirlandes de lampions au-dessus du toit (entre quatre poteaux), et une
        // enseigne holographique « THUNK » au bord du toit, derrière le joueur à son arrivée. Rien de tout ça n'arrête les
        // flèches ni ne cache les tireurs (l'enseigne est transparente).
        static void BuildRoofDetails(Transform root, Materials materials, Random random)
        {
            var details = Child(root, "Détails du toit", Vector3.zero);

            var puddles = new[]
            {
                (new Vector3(-2.6f, 0f, 3.1f), new Vector2(1.6f, 1f)), (new Vector3(3.4f, 0f, 2.2f), new Vector2(1.1f, 0.8f)),
                (new Vector3(-3.8f, 0f, -2.8f), new Vector2(2f, 1.1f)), (new Vector3(2.2f, 0f, -3.6f), new Vector2(1.3f, 1.7f)),
                (new Vector3(0.4f, 0f, 4.6f), new Vector2(0.9f, 0.6f)), (new Vector3(5.6f, 0f, -1.6f), new Vector2(1.4f, 0.9f)),
                (new Vector3(-5.8f, 0f, 2.4f), new Vector2(1f, 1.3f)), (new Vector3(0.6f, 0f, -0.5f), new Vector2(1.2f, 0.8f)),
            };
            foreach (var (spot, size) in puddles)
            {
                var puddle = Shape(PrimitiveType.Cylinder, details, "Flaque", spot + Vector3.up * 0.004f, new Vector3(size.x, 0.002f, size.y),
                                   materials.Puddle);
                puddle.localRotation = Quaternion.Euler(0f, Range(random, 0f, 180f), 0f);
            }

            // Le cercle de tirets autour de la zone.
            const int dashes = 28;
            for (var i = 0; i < dashes; i++)
            {
                var rotation = Quaternion.Euler(0f, i * 360f / dashes, 0f);
                var dash = Box(details, "Tiret", rotation * new Vector3(0f, 0.008f, 2.3f), new Vector3(0.28f, 0.012f, 0.05f), materials.NeonPink, false);
                dash.localRotation = rotation;
            }

            // Les bouches d'aération, et leur vapeur teintée par les néons.
            foreach (var (spot, neon) in new[] { (new Vector3(-3.6f, 0f, 6.9f), 2), (new Vector3(3.8f, 0f, -6.9f), 0) })
            {
                Box(details, "Aération", spot + Vector3.up * 0.25f, new Vector3(0.7f, 0.5f, 0.7f), materials.Metal, true);
                Box(details, "Grille", spot + Vector3.up * 0.51f, new Vector3(0.6f, 0.03f, 0.6f), materials.Dark, false);
                Steam(details, spot + Vector3.up * 0.55f, materials.Steam, k_NeonLights[neon]);
            }

            // Les guirlandes : 4 poteaux près des murets, deux câbles qui se croisent au-dessus du toit (au plus bas, 3,3 m).
            var poles = new[] { new Vector3(-8.3f, 0f, 3.2f), new Vector3(8.3f, 0f, -3.2f), new Vector3(-3.2f, 0f, -8.3f), new Vector3(3.2f, 0f, 8.3f) };
            for (var i = 0; i < poles.Length; i++)
            {
                Box(details, "Poteau", poles[i] + Vector3.up * 2.1f, new Vector3(0.08f, 4.2f, 0.08f), materials.Metal, false);
                Box(details, "Lumière", poles[i] + Vector3.up * 4.25f, Vector3.one * 0.12f, materials.Neons[i % 4], false);
            }

            Garland(details, poles[0] + Vector3.up * 4.15f, poles[1] + Vector3.up * 4.15f, 0.85f, materials, random);
            Garland(details, poles[2] + Vector3.up * 4.15f, poles[3] + Vector3.up * 4.15f, 0.85f, materials, random);

            // L'enseigne « THUNK », un écran holographique sur deux supports, au bord du toit, tournée vers la zone.
            var sign = Child(details, "Enseigne holographique", new Vector3(0f, 2.35f, -8.4f));
            Box(details, "Support", new Vector3(-2.3f, 0.75f, -8.4f), new Vector3(0.06f, 1.5f, 0.06f), materials.Metal, false);
            Box(details, "Support", new Vector3(2.3f, 0.75f, -8.4f), new Vector3(0.06f, 1.5f, 0.06f), materials.Metal, false);
            HoloScreen(sign, "THUNK", new Vector2(5.2f, 1.7f), materials.Holograms[1], materials.GlowTexts[1]);
            sign.gameObject.AddComponent<CyberHologram>();
            Dynamic(sign);
        }

        // Un câble qui pend entre deux points (une parabole de « sag » m au milieu), en 18 tronçons, avec un lampion de
        // couleur tous les deux tronçons.
        static void Garland(Transform parent, Vector3 from, Vector3 to, float sag, Materials materials, Random random)
        {
            const int segments = 18;
            var previous = from;
            for (var i = 1; i <= segments; i++)
            {
                var t = i / (float)segments;
                var point = Vector3.Lerp(from, to, t) + Vector3.down * (sag * 4f * t * (1f - t));
                var wire = Box(parent, "Câble", (previous + point) * 0.5f, new Vector3(0.015f, 0.015f, Vector3.Distance(previous, point)),
                               materials.Dark, false);
                wire.localRotation = Quaternion.LookRotation(point - previous);
                if (i < segments && i % 2 == 0)
                    Box(parent, "Lampion", point + Vector3.down * 0.09f, new Vector3(0.11f, 0.15f, 0.11f), materials.Neons[random.Next(4)], false);
                previous = point;
            }
        }

        // De la vapeur qui monte doucement d'une bouche d'aération : de grosses bulles douces, ajoutées à l'image, teintées
        // par les néons voisins.
        static void Steam(Transform parent, Vector3 position, Material material, Color tint)
        {
            var go = Child(parent, "Vapeur", position);
            go.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var particles = go.gameObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
            main.startColor = new Color(tint.r * 0.7f + 0.3f, tint.g * 0.7f + 0.3f, tint.b * 0.7f + 0.3f, 0.1f);
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 8f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.22f;
            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.4f));
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
            colors.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // La ville : un anneau d'immeubles proches (sur leurs toits, les postes des tireurs), puis l'horizon.
        static void BuildCity(Transform root, Transform posts, Materials materials, Random random)
        {
            var city = Child(root, "Ville", Vector3.zero);
            var word = 0;

            // L'anneau proche : 14 immeubles, toits entre 2 m sous le nôtre et 13 m au-dessus (4 m sous le nôtre et 12 m
            // au-dessus jusqu'au 24 octobre : trop de tireurs étaient cachés par le muret). Chacun a sa couleur de néon
            // (arêtes, bord du toit, lumière sur sa façade) et une enseigne d'une autre couleur.
            const int near = 14;
            for (var i = 0; i < near; i++)
            {
                var angle = (i + Range(random, -0.25f, 0.25f)) * 360f / near;
                var distance = Range(random, 15f, 24f);
                var width = Range(random, 6f, 9f);
                var top = Range(random, -2f, 13f);
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var center = direction * distance;
                var facing = Quaternion.LookRotation(-direction);
                var neon = i % 4;
                var model = CityModel(k_NearModels[random.Next(k_NearModels.Length)]);
                Vector3 size;
                Transform building;
                if (model != null)
                {
                    building = CityBuilding(city, model, center, width, top, facing, materials.City[random.Next(materials.City.Length)],
                                            materials, random, materials.Neons[neon], true, out size);
                }
                else
                {
                    size = new Vector3(width, 0f, width);
                    building = Building(city, center, width, width, top, materials, random, true);
                    building.localRotation = facing;
                }

                // Le toit, pour y poser les tireurs : des colliders le temps de le trouver.
                var roof = new RoofFinder(building);

                // Un ou deux postes près du bord qui fait face au toit du joueur, tournés vers lui, seulement s'ils sont à
                // découvert (PostVisible).
                var count = random.NextDouble() < 0.75 ? 2 : 1;
                for (var p = 0; p < count; p++)
                {
                    var along = count == 1 ? 0f : (p == 0 ? -1f : 1f) * size.x * 0.25f;
                    var local = new Vector3(along, 0f, size.z * 0.5f - 1.1f);
                    var position = center + facing * local;
                    position.y = roof.HeightAt(root, position, top);
                    if (!PostVisible(root, roof, position))
                        continue;
                    var post = Child(posts, "Poste", position);
                    post.localRotation = facing;
                }

                // Un néon le long du bord avant du toit, sous les pieds des tireurs, s'il est plat.
                RoofEdge(root, building, roof, center, facing, size, top, materials.Neons[neon]);

                // Sur un toit sur trois, une tour d'écrans au fond.
                var tower = i % 3 == 1 ? PropModel(k_ScreenTower) : null;
                if (tower != null)
                {
                    var spot = center + facing * new Vector3(0f, 0f, -size.z * 0.2f);
                    spot.y = roof.HeightAt(root, spot, top);
                    var towerScale = Range(random, 5f, 7f) / Mathf.Max(0.01f, Measure(tower).size.y);
                    PlaceModel(tower, city, spot, facing, towerScale, 0f, original => materials.Night(original, neon));
                }

                roof.Dispose();

                // La façade éclairée par ses néons.
                PointLight(building, "Lumière des néons", new Vector3(0f, top - 4f, size.z * 0.5f + 2.5f), k_NeonLights[neon], 13f, 12f);

                // Une enseigne sur chaque immeuble proche : horizontale (un panneau lumineux), ou verticale, aux lettres néon.
                var signNeon = (neon + 1 + random.Next(3)) % 4;
                var text = k_Words[word++ % k_Words.Length];
                float signY;
                if (random.NextDouble() < 0.5)
                {
                    signY = top - Range(random, 2.5f, Mathf.Clamp(size.y - 1f, 3f, 8f));
                    Sign(building, text, signY, size.x, size.z * 0.5f, materials.Neons[signNeon]);
                }
                else
                {
                    var side = random.NextDouble() < 0.5 ? -1f : 1f;
                    signY = top - 1.5f - (1.05f * text.Length + 0.5f) * 0.5f;
                    VerticalSign(building, text, top - 1.5f, side * size.x * 0.3f, size.z * 0.5f, materials.Neons[signNeon], materials.GlowTexts[signNeon],
                                 materials);
                }

                PointLight(building, "Lumière de l'enseigne", new Vector3(0f, signY, size.z * 0.5f + 2f), k_NeonLights[signNeon], 9f, 6f);
            }

            // L'horizon : de grands immeubles sombres aux fenêtres allumées, quelques-uns à moitié chargés (en fil de fer),
            // des balises rouges sur les plus hauts, des écrans holographiques, et le mannequin holographique géant. Ils
            // commencent à 34 m du centre (coins compris).
            const int far = 46;
            var screens = 0;
            var giant = false;
            for (var i = 0; i < far; i++)
            {
                var angle = Range(random, 0f, 360f);
                var width = Range(random, 8f, 18f);
                var distance = Mathf.Max(Range(random, 36f, 120f), k_FarStart + width * 0.75f);
                var direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                var center = direction * distance;
                var top = Range(random, -15f, 55f);
                if (random.NextDouble() < 0.12)
                {
                    Wireframe(city, center, width, top, random.NextDouble() < 0.5 ? materials.NeonCyan : materials.NeonViolet);
                    continue;
                }

                var facing = Quaternion.Euler(0f, Range(random, 0f, 90f), 0f);
                var name = k_FarModels[random.Next(k_FarModels.Length)];
                var model = CityModel(name);
                var trim = random.NextDouble() < 0.35 ? materials.Neons[random.Next(4)] : null;
                Transform building;
                Vector3 size;
                if (model != null)
                {
                    building = CityBuilding(city, model, center, width, top, facing, materials.City[random.Next(materials.City.Length)],
                                            materials, random, trim, false, out size);

                    // Les immeubles simples (des blocs blancs, sans fenêtres) : des bandes de fenêtres néon sur leurs faces.
                    if (name.StartsWith("low-detail"))
                        Bands(building, size.x, size.z, top - 1f, top - size.y, 5, materials, random);
                }
                else
                {
                    size = new Vector3(width, 0f, width);
                    building = Building(city, center, width, width, top, materials, random, false);
                    building.localRotation = facing;
                }

                if (random.NextDouble() < 0.35)
                    Sign(building, k_Words[word++ % k_Words.Length], top - Range(random, 3f, 9f), size.x, size.z * 0.5f,
                         materials.Neons[random.Next(4)]);
                if (top > 22f)
                    Box(building, "Balise", new Vector3(0f, top + 0.3f, 0f), Vector3.one * 0.6f, materials.NeonRed, false);

                // Sur quelques toits pas trop hauts et pas trop loin, tournés vers le joueur : 4 écrans, puis le mannequin.
                var turned = Quaternion.LookRotation(-direction);
                if (!giant && distance > 45f && distance < 75f && top > 4f && top < 30f)
                {
                    giant = true;
                    GiantHologram(city, new Vector3(center.x, top + 0.2f, center.z), turned, materials);
                }
                else if (screens < 4 && distance < 85f && top > 0f && top < 40f && random.NextDouble() < 0.5)
                {
                    screens++;
                    var screen = Child(city, "Écran holographique", new Vector3(center.x, top + 3.2f, center.z));
                    screen.localRotation = turned;
                    var neon = random.Next(4);
                    HoloScreen(screen, k_Words[word++ % k_Words.Length], new Vector2(9f, 4.2f), materials.Holograms[neon], materials.GlowTexts[neon]);
                    screen.gameObject.AddComponent<CyberHologram>();
                    Dynamic(screen);
                    Box(city, "Pied", new Vector3(center.x, top + 0.55f, center.z) + turned * new Vector3(-3.6f, 0f, 0f), new Vector3(0.2f, 1.1f, 0.2f),
                        materials.Metal, false);
                    Box(city, "Pied", new Vector3(center.x, top + 0.55f, center.z) + turned * new Vector3(3.6f, 0f, 0f), new Vector3(0.2f, 1.1f, 0.2f),
                        materials.Metal, false);
                }
            }
        }

        // Un immeuble du City Kit, assombri, aux fenêtres néon (« skin »), mis à la largeur voulue, son toit à « top » ; dessous,
        // un fût sombre jusqu'au fond de la ville, avec des bandes de fenêtres et, si « trim », des néons sur ses arêtes (les
        // quatre et une ceinture sous l'immeuble si « allCorners », sinon les deux de devant). « size » : ses dimensions
        // (largeur, hauteur du modèle, profondeur), dans le repère de l'immeuble (z vers « facing »).
        static Transform CityBuilding(Transform parent, GameObject model, Vector3 center, float width, float top, Quaternion facing,
                                      Material skin, Materials materials, Random random, Material trim, bool allCorners, out Vector3 size)
        {
            const float bottom = -60f;
            var bounds = Measure(model);
            var scale = width / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.z));
            size = bounds.size * scale;
            var building = Child(parent, "Immeuble", new Vector3(center.x, 0f, center.z));
            building.localRotation = facing;
            var baseY = top - size.y;
            PlaceModel(model, building, new Vector3(0f, baseY, 0f), Quaternion.identity, scale, random.NextDouble() < 0.5 ? 0f : 180f, _ => skin);

            if (baseY > bottom + 0.5f)
            {
                var trunk = baseY - bottom;
                var trunkWidth = size.x * 0.96f;
                var trunkDepth = size.z * 0.96f;
                Box(building, "Fût", new Vector3(0f, bottom + trunk * 0.5f + 0.02f, 0f), new Vector3(trunkWidth, trunk, trunkDepth),
                    materials.Building, false);
                Bands(building, trunkWidth, trunkDepth, baseY - 1f, Mathf.Max(bottom + 2f, baseY - 30f), 3, materials, random);
                if (trim != null)
                    Trim(building, trunkWidth, trunkDepth, baseY, Mathf.Max(bottom, baseY - 40f), trim, allCorners);
            }

            return building;
        }

        // Des néons sur les arêtes d'un fût (largeur × profondeur), de « high » à « low » : les quatre et une ceinture juste
        // sous l'immeuble (« allCorners »), ou seulement les deux de devant (+z).
        static void Trim(Transform building, float width, float depth, float high, float low, Material neon, bool allCorners)
        {
            const float bar = 0.1f;
            var length = high - low;
            if (length < 1f)
                return;

            for (var x = -1; x <= 1; x += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                {
                    if (!allCorners && z < 0)
                        continue;
                    Box(building, "Néon", new Vector3(x * (width * 0.5f + 0.02f), low + length * 0.5f, z * (depth * 0.5f + 0.02f)),
                        new Vector3(bar, length, bar), neon, false);
                }
            }

            if (!allCorners)
                return;
            for (var side = 0; side < 4; side++)
            {
                var rotation = Quaternion.Euler(0f, side * 90f, 0f);
                var half = side % 2 == 0 ? depth * 0.5f : width * 0.5f;
                var span = side % 2 == 0 ? width : depth;
                var belt = Box(building, "Néon", rotation * new Vector3(0f, high - 0.5f, half + 0.04f), new Vector3(span, 0.12f, 0.06f), neon, false);
                belt.localRotation = rotation;
            }
        }

        // Un tireur sur ce poste est à découvert (24 octobre : des tireurs étaient sur des toits plus bas que le nôtre, cachés
        // par le muret : on ne voyait ni eux ni leurs balles, et nos flèches s'arrêtaient sur notre toit). Vue du milieu de
        // la zone, un peu accroupi, sa poitrine passe au-dessus du muret, et le toit de son immeuble ne la cache pas.
        static bool PostVisible(Transform root, RoofFinder roof, Vector3 post)
        {
            var eye = new Vector3(0f, k_SightEye, 0f);
            var chest = post + Vector3.up * k_SightChest;
            var flat = Mathf.Max(Mathf.Abs(chest.x), Mathf.Abs(chest.z));
            if (flat <= k_RoofHalf)
                return true;

            // La hauteur de la ligne de vue au bord du toit (le dehors du muret).
            var height = Mathf.Lerp(eye.y, chest.y, k_RoofHalf / flat);
            if (height < k_ParapetHeight + k_SightClearance)
                return false;
            return !roof.Blocks(root, eye, chest);
        }

        // Un néon le long du bord avant d'un toit proche (sous les pieds des tireurs) : seulement si le toit y est plat (trois
        // mesures à moins de 40 cm l'une de l'autre).
        static void RoofEdge(Transform root, Transform building, RoofFinder roof, Vector3 center, Quaternion facing, Vector3 size, float top,
                             Material neon)
        {
            var edge = size.z * 0.5f - 0.15f;
            var heights = new List<float>();
            foreach (var x in new[] { -0.35f, 0f, 0.35f })
            {
                if (!roof.TryHeightAt(root, center + facing * new Vector3(x * size.x, 0f, edge), top, out var height))
                    return;
                heights.Add(height);
            }

            if (heights.Max() - heights.Min() > 0.4f)
                return;
            Box(building, "Néon du toit", new Vector3(0f, heights.Average() + 0.05f, edge), new Vector3(size.x * 0.85f, 0.07f, 0.07f), neon, false);
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
            public float HeightAt(Transform root, Vector3 local, float fallback) =>
                TryHeightAt(root, local, fallback, out var height) ? height : fallback;

            // Cherche le toit sous « local », de 6 m au-dessus de « top » jusqu'à 24 m en dessous.
            public bool TryHeightAt(Transform root, Vector3 local, float top, out float height)
            {
                var origin = root.TransformPoint(new Vector3(local.x, top + 6f, local.z));
                var best = float.MaxValue;
                height = top;
                foreach (var hit in Physics.RaycastAll(origin, Vector3.down, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (hit.distance >= best || !m_Colliders.Contains(hit.collider))
                        continue;
                    best = hit.distance;
                    height = root.InverseTransformPoint(hit.point).y;
                }

                return best < float.MaxValue;
            }

            // La ligne de « from » à « to » (dans le repère du toit cyberpunk) traverse cet immeuble.
            public bool Blocks(Transform root, Vector3 from, Vector3 to)
            {
                var start = root.TransformPoint(from);
                var line = root.TransformPoint(to) - start;
                foreach (var hit in Physics.RaycastAll(start, line.normalized, line.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (m_Colliders.Contains(hit.collider))
                        return true;
                }

                return false;
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

        // Un immeuble « pas encore chargé » : seulement ses arêtes, en néon.
        static void Wireframe(Transform parent, Vector3 center, float width, float top, Material neon)
        {
            var frame = Child(parent, "Immeuble non chargé", new Vector3(center.x, 0f, center.z));
            const float bottom = -40f;
            var height = top - bottom;
            var half = width * 0.5f;
            const float bar = 0.12f;
            for (var x = -1; x <= 1; x += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                    Box(frame, "Arête", new Vector3(x * half, bottom + height * 0.5f, z * half), new Vector3(bar, height, bar), neon, false);
            }

            foreach (var y in new[] { bottom, top, bottom + height * 0.5f })
            {
                for (var side = 0; side < 4; side++)
                {
                    var rotation = Quaternion.Euler(0f, side * 90f, 0f);
                    var edge = Box(frame, "Arête", rotation * new Vector3(0f, y, half), new Vector3(width, bar, bar), neon, false);
                    edge.localRotation = rotation;
                }
            }
        }

        // Une enseigne : un panneau lumineux et un mot sombre dessus, à la hauteur « y », sur la face qui regarde le toit du
        // joueur (à « front » du centre de l'immeuble).
        static void Sign(Transform building, string text, float y, float width, float front, Material board)
        {
            var size = new Vector2(Mathf.Min(width * 0.8f, 1.1f * text.Length), 2f);
            var panel = Box(building, "Enseigne", new Vector3(0f, y, front + 0.3f), new Vector3(size.x, size.y, 0.15f), board, false);
            var label = Label(panel, text, new Vector3(0f, 0f, 0.6f), size, new Color(0.04f, 0.02f, 0.06f), null);
            label.transform.localScale = new Vector3(1f / panel.localScale.x, 1f / panel.localScale.y, 1f);
        }

        // Une enseigne verticale (décor 3) : un long panneau sombre posé à 40 cm de la façade, cerclé de néon, aux lettres
        // néon empilées, son haut à « top ».
        static void VerticalSign(Transform building, string text, float top, float x, float front, Material neon, Material glowText,
                                 Materials materials)
        {
            var height = 1.05f * text.Length + 0.5f;
            const float width = 1.15f;
            var center = new Vector3(x, top - height * 0.5f, front + 0.4f);
            Box(building, "Enseigne verticale", center, new Vector3(width, height, 0.12f), materials.Dark, false);
            const float bar = 0.06f;
            Box(building, "Néon", center + new Vector3(-width * 0.5f, 0f, 0.07f), new Vector3(bar, height, bar), neon, false);
            Box(building, "Néon", center + new Vector3(width * 0.5f, 0f, 0.07f), new Vector3(bar, height, bar), neon, false);
            Box(building, "Néon", center + new Vector3(0f, height * 0.5f, 0.07f), new Vector3(width, bar, bar), neon, false);
            Box(building, "Néon", center + new Vector3(0f, -height * 0.5f, 0.07f), new Vector3(width, bar, bar), neon, false);
            var label = Label(building, string.Join("\n", text.Select(letter => letter.ToString())), center + new Vector3(0f, 0f, 0.08f),
                              new Vector2(width - 0.25f, height - 0.3f), Color.white, glowText);
            label.lineSpacing = -15f;
        }

        // Un écran holographique (décor 3) : un quad de lumière cadré, traversé de lignes, et un mot qui brille devant lui,
        // tournés vers +z (le joueur) ; « parent » porte le CyberHologram qui le fait sauter.
        static void HoloScreen(Transform parent, string text, Vector2 size, Material hologram, Material glowText)
        {
            var quad = Shape(PrimitiveType.Quad, parent, "Écran", Vector3.zero, new Vector3(size.x, size.y, 1f), hologram);
            quad.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Label(parent, text, new Vector3(0f, 0f, 0.05f), size * 0.85f, Color.white, glowText);
        }

        // Le mannequin holographique géant (décor 3) : le mannequin d'entraînement des tireurs (un costume de THUNK), 19 m de
        // haut, en lumière cyan qui ne garde que ses contours, aux yeux rouges, sur un socle lumineux ; il se tourne lentement
        // vers le joueur.
        static void GiantHologram(Transform parent, Vector3 basePosition, Quaternion facing, Materials materials)
        {
            const float scale = 10f;
            var giant = Child(parent, "Mannequin holographique", basePosition);
            giant.localRotation = facing;
            var hologram = materials.Holograms[0];
            Shape(PrimitiveType.Cylinder, giant, "Socle", new Vector3(0f, 0.05f * scale, 0f), new Vector3(0.7f, 0.05f, 0.7f) * scale, hologram);
            Shape(PrimitiveType.Capsule, giant, "Corps", new Vector3(0f, 0.72f * scale, 0f), new Vector3(0.56f, 0.6f, 0.5f) * scale, hologram);
            Shape(PrimitiveType.Sphere, giant, "Tête", new Vector3(0f, 1.55f * scale, 0f), Vector3.one * 0.42f * scale, hologram);
            Box(giant, "Œil gauche", new Vector3(-0.07f, 1.6f, 0.2f) * scale, new Vector3(0.07f, 0.04f, 0.02f) * scale, materials.NeonRed, false);
            Box(giant, "Œil droit", new Vector3(0.07f, 1.6f, 0.2f) * scale, new Vector3(0.07f, 0.04f, 0.02f) * scale, materials.NeonRed, false);
            Shape(PrimitiveType.Cylinder, giant, "Projecteur", new Vector3(0f, 0.01f, 0f), new Vector3(0.9f, 0.01f, 0.9f) * scale, materials.NeonCyan);

            var turning = giant.gameObject.AddComponent<CyberHologram>();
            var settings = new SerializedObject(turning);
            settings.FindProperty("m_FacePlayer").boolValue = true;
            settings.FindProperty("m_TurnSpeed").floatValue = 4f;
            settings.FindProperty("m_Jump").floatValue = 0.8f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Dynamic(giant);
        }

        // La pluie : des traits pâles qui tombent sur toute la zone, et quelques gouttes rouges qui montent (le monde bugue).
        static void BuildRain(Transform root, Materials materials)
        {
            Rain(root, "Pluie", 700f, -14f, new Color(0.6f, 0.65f, 1f, 0.38f), materials.Rain, 14f);
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

        // La lune (violette, faible) et quatre néons autour de la zone, un de chaque couleur, qui éclairent le toit mouillé et
        // les arbalètes.
        static void BuildLights(Transform root)
        {
            var moon = Child(root, "Lune", new Vector3(0f, 30f, 0f)).gameObject.AddComponent<Light>();
            moon.type = LightType.Directional;
            // Elle éclaire depuis la lune du ciel (devant le joueur à son arrivée, un peu à gauche).
            moon.transform.rotation = Quaternion.Euler(19f, 158f, 0f);
            moon.color = new Color(0.6f, 0.5f, 1f);
            moon.intensity = 0.18f;
            moon.shadows = LightShadows.None;
            RenderSettings.sun = moon;

            var spots = new[] { new Vector3(-4.5f, 3f, 4.5f), new Vector3(4.5f, 3f, 4.5f), new Vector3(4.5f, 3f, -4.5f), new Vector3(-4.5f, 3f, -4.5f) };
            for (var i = 0; i < spots.Length; i++)
                PointLight(root, "Néon", spots[i], k_NeonLights[i], 11f, 8f);
        }

        // Les reflets du toit mouillé (décor 3) : une sonde au milieu du toit, rendue une fois à l'arrivée du joueur
        // (CyberDimension.Begin), qui fait briller les néons dans les flaques et sur le toit.
        static ReflectionProbe BuildReflections(Transform root)
        {
            var probe = Child(root, "Reflets", new Vector3(0f, 1.6f, 0f)).gameObject.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.resolution = 256;
            probe.hdr = true;
            probe.size = new Vector3(60f, 30f, 60f);
            probe.farClipPlane = 300f;
            probe.importance = 1;
            return probe;
        }

        // Le post-traitement du toit (décor 3), par-dessus celui de la carte (priorité plus haute), le temps de l'épreuve :
        // plus de halo autour des néons, des couleurs plus franches, les ombres bleutées et les lumières roses.
        static void BuildPostProcess(Transform root)
        {
            var path = k_ArtFolder + "/Post-traitement toit.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var bloom = Effect<Bloom>(profile);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.35f);
            bloom.scatter.Override(0.75f);
            var colors = Effect<ColorAdjustments>(profile);
            colors.contrast.Override(16f);
            colors.saturation.Override(25f);
            var toning = Effect<SplitToning>(profile);
            toning.shadows.Override(new Color(0.4f, 0.5f, 0.65f));
            toning.highlights.Override(new Color(0.65f, 0.45f, 0.58f));
            toning.balance.Override(-10f);
            EditorUtility.SetDirty(profile);

            var volume = Child(root, "Post-traitement", Vector3.zero).gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 50f;
            volume.sharedProfile = profile;
        }

        static T Effect<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var effect))
                return effect;
            effect = profile.Add<T>();
            effect.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(effect, profile);
            return effect;
        }

        static void ApplyEnvironment(Materials materials)
        {
            RenderSettings.skybox = materials.Sky;
            // Une ambiance violette et sombre, un peu plus claire par en dessous (la ville éclaire de la rue).
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.1f, 0.07f, 0.17f);
            RenderSettings.ambientEquatorColor = new Color(0.1f, 0.05f, 0.14f);
            RenderSettings.ambientGroundColor = new Color(0.13f, 0.04f, 0.1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = k_FogColor;
            RenderSettings.reflectionIntensity = 0.6f;
        }

        // ---------------------------------------------------------------- les matériaux

        sealed class Materials
        {
            // Le toit mouillé : il reflète les néons (sonde de reflets, rendue à l'arrivée du joueur).
            public readonly Material Roof = Lit("Toit", new Color(0.09f, 0.09f, 0.11f), Color.black, 0.82f);
            public readonly Material Puddle = Lit("Flaque", new Color(0.015f, 0.015f, 0.02f), Color.black, 0.97f);
            public readonly Material Building = Lit("Immeuble", new Color(0.045f, 0.045f, 0.06f), Color.black, 0.3f);
            public readonly Material Metal = Lit("Metal", new Color(0.13f, 0.13f, 0.16f), Color.black, 0.6f);
            public readonly Material MetalDark = Lit("Metal sombre", new Color(0.06f, 0.06f, 0.075f), Color.black, 0.55f);
            public readonly Material MetalLight = Lit("Metal clair", new Color(0.24f, 0.24f, 0.28f), Color.black, 0.7f);
            public readonly Material Dark = Lit("Sombre", new Color(0.03f, 0.03f, 0.04f), Color.black, 0.4f);
            public readonly Material NeonCyan = Lit("Neon cyan", Color.black, new Color(0.2f, 1.6f, 2.2f), 0.5f);
            public readonly Material NeonPink = Lit("Neon rose", Color.black, new Color(2.2f, 0.25f, 1.5f), 0.5f);
            public readonly Material NeonViolet = Lit("Neon violet", Color.black, new Color(1.1f, 0.3f, 2.6f), 0.5f);
            public readonly Material NeonAmber = Lit("Neon ambre", Color.black, new Color(2.4f, 1.05f, 0.2f), 0.5f);
            public readonly Material NeonRed = Lit("Neon rouge", Color.black, new Color(3f, 0.1f, 0.1f), 0.5f);
            public readonly Material Lamp = Lit("Lampe", Color.black, new Color(2.2f, 1.9f, 1.5f), 0.5f);
            public readonly Material[] Windows =
            {
                Lit("Fenetres cyan", Color.black, new Color(0.15f, 0.9f, 1.3f), 0.4f),
                Lit("Fenetres roses", Color.black, new Color(1.3f, 0.2f, 0.9f), 0.4f),
                Lit("Fenetres chaudes", Color.black, new Color(1.4f, 0.8f, 0.3f), 0.4f),
                Lit("Fenetres violettes", Color.black, new Color(0.8f, 0.25f, 1.6f), 0.4f),
                Lit("Fenetres blanches", Color.black, new Color(1.05f, 1.1f, 1.35f), 0.4f),
            };
            // Les immeubles du City Kit la nuit : murs sombres, fenêtres allumées (plus vives depuis le décor 3).
            public readonly Material[] City =
            {
                CityMaterial("Ville nuit cyan", new Color(0.3f, 1.45f, 2.1f)),
                CityMaterial("Ville nuit rose", new Color(1.9f, 0.3f, 1.4f)),
                CityMaterial("Ville nuit chaude", new Color(1.8f, 1f, 0.4f)),
                CityMaterial("Ville nuit violette", new Color(1.1f, 0.35f, 2f)),
            };
            public readonly Material Rain = RainMaterial();
            public readonly Material Steam = GlowMaterial("Vapeur", new Color(0.8f, 0.7f, 0.9f), 1f);
            public readonly Material Sky = SkyMaterial();
            public readonly Material Crossbow = CrossbowMaterial();

            /// <summary>Les néons, dans l'ordre de k_NeonLights : cyan, rose, violet, ambre.</summary>
            public readonly Material[] Neons;

            /// <summary>Les détails orange des objets de Quaternius, rallumés en néon plus discret, des mêmes couleurs.</summary>
            public readonly Material[] Accents;

            /// <summary>Les hologrammes, des mêmes couleurs.</summary>
            public readonly Material[] Holograms;

            /// <summary>Les textes qui brillent (enseignes verticales, écrans), des mêmes couleurs ; null sans police TextMeshPro.</summary>
            public readonly Material[] GlowTexts;

            Material m_Signs;

            public Materials()
            {
                Neons = new[] { NeonCyan, NeonPink, NeonViolet, NeonAmber };
                var names = new[] { "cyan", "rose", "violet", "ambre" };
                Accents = new Material[4];
                Holograms = new Material[4];
                GlowTexts = new Material[4];
                for (var i = 0; i < 4; i++)
                {
                    var light = k_NeonLights[i];
                    Accents[i] = Lit("Accent " + names[i], new Color(0.05f, 0.05f, 0.06f), light * 0.9f, 0.5f);
                    Holograms[i] = HologramMaterial("Hologramme " + names[i], light * 1.6f);
                    GlowTexts[i] = GlowText("Texte neon " + names[i], light * 2.6f);
                }
            }

            /// <summary>
            /// Les objets de Quaternius la nuit, d'après le nom de leurs matériaux : les gris assombris et mouillés, les
            /// lumières et les écrans allumés, les enseignes (leur texture) qui brillent, l'orange des détails en néon
            /// (« accent » : la couleur, de 0 à 3).
            /// </summary>
            public Material Night(Material original, int accent)
            {
                if (original == null)
                    return Dark;
                switch (original.name)
                {
                    case "Light":
                        return Lamp;
                    case "Screen":
                    case "Orange":
                        return Accents[accent % 4];
                    case "Texture_Signs":
                        return Signs(original);
                    case "Black":
                        return Dark;
                    case "DarkGrey":
                        return MetalDark;
                    case "Grey":
                        return Metal;
                    case "LightGrey":
                    case "MetalLight":
                        return MetalLight;
                    default:
                        return original;
                }
            }

            // La texture des enseignes de Quaternius, assombrie, et qui brille d'elle-même.
            Material Signs(Material original)
            {
                if (m_Signs != null)
                    return m_Signs;
                var texture = original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : original.mainTexture;
                if (texture == null)
                    return original;
                m_Signs = Lit("Enseignes Quaternius", new Color(0.35f, 0.35f, 0.4f), new Color(1.6f, 1.6f, 1.7f), 0.5f);
                m_Signs.SetTexture("_BaseMap", texture);
                m_Signs.SetTexture("_MainTex", texture);
                m_Signs.SetTexture("_EmissionMap", texture);
                EditorUtility.SetDirty(m_Signs);
                return m_Signs;
            }
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
            var material = Lit(name, new Color(0.17f, 0.16f, 0.24f), windows, 0.35f);
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

        // Une lumière ajoutée à l'image (shader Archery/LaserGlow) : ronde (« round » à 1 : la vapeur) ou en trait (les traînées).
        static Material GlowMaterial(string name, Color color, float round)
        {
            var glow = Load<Material>("Assets/_Project/Resources/LaserGlow.mat");
            var material = LoadOrCreate(name, glow != null ? glow.shader : Shader.Find("Sprites/Default"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Round", round);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Un hologramme (shader Archery/Hologram) : un écran cadré, ou une forme dont on ne voit que les contours, selon
        // l'objet (le cadre ne se voit que sur un quad, le contour que sur une forme ronde).
        static Material HologramMaterial(string name, Color color)
        {
            var shader = Load<Shader>("Assets/_Project/Shaders/Hologram.shader");
            if (shader == null)
                shader = Shader.Find("Archery/Glow");
            var material = LoadOrCreate(name, shader);
            material.SetColor("_Color", color);
            material.SetFloat("_Intensity", 0.45f);
            material.SetFloat("_ScanDensity", 5f);
            material.SetFloat("_ScanSpeed", 0.5f);
            material.SetFloat("_ScanDepth", 0.55f);
            material.SetFloat("_Flicker", 0.35f);
            material.SetFloat("_Frame", 3f);
            material.SetFloat("_Fresnel", 0.7f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Un texte qui brille : la police TextMeshPro par défaut, la face d'une couleur HDR (le halo du post-traitement la
        // fait briller comme un néon).
        static Material GlowText(string name, Color color)
        {
            var font = TMP_Settings.defaultFontAsset;
            if (font == null || font.material == null)
                return null;
            var path = $"{k_ArtFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(font.material) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = font.material.shader;
                material.CopyPropertiesFromMaterial(font.material);
            }

            material.SetColor("_FaceColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Le ciel (décor 4, shader Archery/CyberSky ; décor 3 : un dégradé sans étoiles, jugé trop vide) : celui du mode
        // Difficile aux couleurs du cyberpunk. Un violet presque noir en haut, un halo rose à l'horizon (la ville éclaire le
        // ciel) ; beaucoup d'étoiles blanches, cyan et roses ; une aurore néon, rose puis cyan ; une nébuleuse violette ; des
        // étoiles filantes cyan ; un smog bas éclairé en rose ; une grosse lune rose à bandes devant le joueur à son arrivée ;
        // des lignes de balayage et quelques bandes qui buguent. Sous l'horizon, la couleur du brouillard, pour que les
        // immeubles lointains s'y fondent.
        static Material SkyMaterial()
        {
            var shader = Load<Shader>("Assets/_Project/Shaders/CyberSky.shader");
            if (shader == null)
                shader = Load<Shader>("Assets/_Project/Shaders/StylizedSky.shader");
            var material = LoadOrCreate("Ciel nuit", shader != null ? shader : Shader.Find("Skybox/Procedural"));
            material.SetColor("_ZenithColor", new Color(0.02f, 0.01f, 0.06f));
            material.SetColor("_HorizonColor", new Color(0.42f, 0.09f, 0.36f));
            material.SetColor("_GroundColor", k_FogColor);
            material.SetFloat("_HorizonSharpness", 2.6f);
            material.SetFloat("_Stars", 1f);
            material.SetColor("_StarColorA", new Color(0.55f, 1.4f, 1.8f));
            material.SetColor("_StarColorB", new Color(1.8f, 0.55f, 1.4f));
            material.SetFloat("_Nebula", 0.6f);
            material.SetColor("_NebulaColor", new Color(0.35f, 0.08f, 0.5f));
            material.SetColor("_NebulaCore", new Color(0.6f, 0.15f, 0.55f));
            material.SetVector("_NebulaAxis", new Vector4(0.3f, 0.55f, -0.78f, 0f));
            material.SetFloat("_Aurora", 0.7f);
            material.SetColor("_AuroraLow", new Color(1.5f, 0.12f, 1f));
            material.SetColor("_AuroraHigh", new Color(0.12f, 1.1f, 1.5f));
            material.SetFloat("_Smog", 0.8f);
            material.SetColor("_SmogColor", new Color(0.05f, 0.015f, 0.07f));
            material.SetColor("_SmogGlow", new Color(0.75f, 0.12f, 0.48f));
            material.SetFloat("_ShootingStars", 1f);
            material.SetColor("_ShootingColor", new Color(0.4f, 1.6f, 2f));
            material.SetVector("_DiscDirection", new Vector4(-0.35f, 0.32f, 0.88f, 0f));
            material.SetColor("_DiscColor", new Color(1.3f, 1f, 1.25f));
            material.SetColor("_DiscColorLow", new Color(1.6f, 0.3f, 0.85f));
            material.SetFloat("_DiscSize", 0.09f);
            material.SetColor("_GlowColor", new Color(0.45f, 0.1f, 0.4f));
            material.SetFloat("_GlowSize", 0.3f);
            material.SetFloat("_MoonSurface", 0.6f);
            material.SetFloat("_MoonStripes", 1f);
            material.SetFloat("_Glitch", 0.5f);
            material.SetFloat("_Scanlines", 0.35f);
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

        static Transform Box(Transform parent, string name, Vector3 localPosition, Vector3 size, Material material, bool collider) =>
            Shape(PrimitiveType.Cube, parent, name, localPosition, size, material, collider);

        // Une forme simple (cube, cylindre, sphère, capsule, quad), statique, sans ombres ; un collider seulement si demandé.
        static Transform Shape(PrimitiveType type, Transform parent, string name, Vector3 localPosition, Vector3 size, Material material,
                               bool collider = false)
        {
            var shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            if (!collider)
                Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localScale = size;
            var renderer = shape.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            shape.isStatic = true;
            return shape.transform;
        }

        // Ce qui bouge (les hologrammes qui sautent, le mannequin qui se tourne) ne doit pas être statique : regroupé avec le décor au
        // chargement, il resterait sur place.
        static void Dynamic(Transform root)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(child.gameObject, 0);
        }

        // Un texte TextMeshPro au centre de « parent », lisible depuis +z (le joueur) ; « material » : sa matière (un texte
        // qui brille), sinon celle de la police.
        static TextMeshPro Label(Transform parent, string text, Vector3 localPosition, Vector2 size, Color color, Material material)
        {
            var label = new GameObject("Texte").AddComponent<TextMeshPro>();
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            label.rectTransform.sizeDelta = size;
            label.text = text;
            label.fontSize = 14f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 2f;
            label.fontSizeMax = 30f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = color;
            if (material != null)
                label.fontSharedMaterial = material;
            return label;
        }

        static Light PointLight(Transform parent, string name, Vector3 localPosition, Color color, float range, float intensity)
        {
            var light = Child(parent, name, localPosition).gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
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
        // « bottomCenter » (dans le repère de « parent »), dans un nœud tourné de « rotation ». « skin » : donne la matière de
        // chacun de ses matériaux (rien : ceux du modèle). Statique, sans ombres ni colliders.
        static Transform PlaceModel(GameObject model, Transform parent, Vector3 bottomCenter, Quaternion rotation, float scale, float yaw,
                                    System.Func<Material, Material> skin)
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
                if (skin == null)
                    continue;
                var materials = renderer.sharedMaterials;
                if (materials.Length == 0)
                    materials = new Material[1];
                for (var i = 0; i < materials.Length; i++)
                    materials[i] = skin(materials[i]);
                renderer.sharedMaterials = materials;
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
