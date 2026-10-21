using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Archery.Portal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
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

        // La musique de la dimension (composée par un ami, sa montée est à 12 s) et l'ancienne, générée pour le prototype.
        const string k_Music = "Assets/_Project/Audio/Music/Sad_Nostalgie_40_Tempo_KLICKAUD.mp3";
        const string k_OldMusicGuid = "0f73399bad1bfdb48a3a2ba0fd0c5bbd";
        const float k_MusicRise = 12f;
        const string k_Mixer = "Assets/_Project/Audio/MainMixer.mixer";

        // Les pas des monstres : les pas sur béton de Kenney (000 à 004).
        const string k_StepClips = "Assets/kenney_impact-sounds/Audio/footstep_concrete_";

        // Un torii à l'échelle 1 : demi-écart des piliers, hauteur (10 m jusqu'au 18 octobre), rayon d'un pilier.
        const float k_HalfSpan = 2.8f;
        const float k_Height = 7f;
        const float k_PillarRadius = 0.55f;

        const float k_ArenaRadius = 23f;
        const string k_ArenaGroup = "Torii de l'épreuve";
        const string k_FarGroup = "Torii lointains";

        // Au-dessus de ce nombre de torii dans la scène (la version trop dense du 18 octobre, 230), ils sont éclaircis.
        const int k_ThinAbove = 180;

        // Version de la forme des torii (hauteur), notée dans le .meta du préfab : maillages et collisions refaits une fois.
        const string k_ToriiShape = "torii-forme-7m";

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

            var text = File.ReadAllText(k_ScenePath);
            if (!text.Contains(k_MirrorGuid))
            {
                Build();
                return;
            }

            // Les retouches des versions suivantes du décor, une fois chacune.
            UpdateToriiLook(false);
            UpdateToriiShape(false);
            var loaded = SceneManager.GetSceneByPath(k_ScenePath).isLoaded;
            if (loaded || Regex.IsMatch(text, @"m_SurviveTime: 30\s"))
                LengthenTrial();
            if (loaded || text.Contains(k_OldMusicGuid))
                UseNewMusic();
            if (loaded || !text.Contains("m_StepClips:"))
                AddMonsterSteps();
            if (loaded || HasTiltedTorii(text))
                StraightenTorii(false);
            if (ToriiCount(text) > k_ThinAbove)
                ThinTorii(false);
        }

        // L'épreuve dure 1 minute (30 s jusqu'au 18 octobre) : l'ancienne valeur de la scène est remplacée. Une autre valeur,
        // choisie à la main, est gardée.
        static void LengthenTrial()
        {
            EditDimension(scene =>
            {
                var dimension = FindDimension(scene);
                if (dimension == null)
                    return null;

                var settings = new SerializedObject(dimension);
                var survive = settings.FindProperty("m_SurviveTime");
                if (survive == null || !Mathf.Approximately(survive.floatValue, 30f))
                    return null;

                survive.floatValue = 60f;
                settings.ApplyModifiedProperties();
                return "l'épreuve dure maintenant 1 minute (Survive Time : 60 s)";
            });
        }

        // La musique de l'ami remplace celle du prototype (jusqu'au 18 octobre), une fois : à sa hauteur (pitch 1, l'ancienne
        // était ralentie), par le groupe Music du mixer, et la découverte dure jusqu'à sa montée (12 s ; 10 s avant). Une
        // autre musique, choisie à la main, est gardée.
        static void UseNewMusic()
        {
            var music = AssetDatabase.LoadAssetAtPath<AudioClip>(k_Music);
            if (music == null)
                return;

            EditDimension(scene =>
            {
                var dimension = FindDimension(scene);
                if (dimension == null)
                    return null;

                var settings = new SerializedObject(dimension);
                var clip = settings.FindProperty("m_Music");
                var old = clip.objectReferenceValue;
                if (old != null && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(old)) != k_OldMusicGuid)
                    return null;

                clip.objectReferenceValue = music;
                var intro = settings.FindProperty("m_IntroTime");
                if (Mathf.Approximately(intro.floatValue, 10f))
                    intro.floatValue = k_MusicRise;
                var source = settings.FindProperty("m_MusicSource").objectReferenceValue as AudioSource;
                settings.ApplyModifiedProperties();
                if (source != null)
                    SetUpMusicSource(source, music);
                return $"musique « Sad Nostalgie » de ton ami, l'épreuve part sur sa montée (Intro Time : {intro.floatValue:0.#} s)";
            });
        }

        // Les pas des monstres (depuis le 18 octobre, pour bien les entendre approcher), si la liste est vide.
        static void AddMonsterSteps()
        {
            var clips = Enumerable.Range(0, 5).Select(i => AssetDatabase.LoadAssetAtPath<AudioClip>($"{k_StepClips}{i:000}.ogg"))
                .Where(clip => clip != null).ToArray();
            if (clips.Length == 0)
                return;

            EditDimension(scene =>
            {
                var dimension = FindDimension(scene);
                if (dimension == null)
                    return null;

                var settings = new SerializedObject(dimension);
                var list = settings.FindProperty("m_StepClips");
                if (list == null || list.arraySize > 0)
                    return null;

                list.arraySize = clips.Length;
                for (var i = 0; i < clips.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                settings.ApplyModifiedProperties();
                return $"les monstres ont des pas (Step Clips : {clips.Length} pas sur béton de Kenney)";
            });
        }

        static void SetUpMusicSource(AudioSource source, AudioClip music)
        {
            source.clip = music;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.pitch = 1f;
            source.volume = 0f;
            source.outputAudioMixerGroup = AssetDatabase.LoadAllAssetsAtPath(k_Mixer).OfType<AudioMixerGroup>()
                .FirstOrDefault(group => group.name == "Music");
            var lowPass = source.GetComponent<AudioLowPassFilter>();
            if (lowPass == null)
                lowPass = source.gameObject.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = 22000f;
            EditorUtility.SetDirty(source);
            EditorUtility.SetDirty(lowPass);
        }

        [MenuItem("Archery/Portail des étoiles/Éclaircir les torii")]
        static void ThinFromMenu() => ThinTorii(true);

        // Retire les torii trop serrés pour les écarts actuels (ceux de ScatterArena et ScatterFar). Les plus anciens sont
        // gardés en priorité (les premiers numéros de chaque groupe, ceux de l'épreuve d'abord).
        static void ThinTorii(bool fromMenu)
        {
            EditDimension(scene =>
            {
                var dimension = FindDimension(scene);
                if (dimension == null)
                    return null;

                var root = dimension.transform;
                var gates = new List<Transform>();
                foreach (var post in root.GetComponentsInChildren<CapsuleCollider>(true))
                {
                    var gate = post.transform.parent;
                    if (gate != null && !gates.Contains(gate))
                        gates.Add(gate);
                }

                int Order(Transform gate) => (gate.parent != null && gate.parent.name == k_ArenaGroup ? 0 : 100000) + Number(gate.name);
                gates.Sort((a, b) => Order(a).CompareTo(Order(b)));

                var kept = new List<Vector2>();
                var removed = 0;
                foreach (var gate in gates)
                {
                    var local = root.InverseTransformPoint(gate.position);
                    var point = new Vector2(local.x, local.z);
                    var radius = point.magnitude;
                    if (Free(kept, point, radius < k_ArenaRadius ? k_ArenaGap : FarGap(radius)))
                    {
                        kept.Add(point);
                        continue;
                    }

                    Undo.DestroyObjectImmediate(gate.gameObject);
                    removed++;
                }

                if (removed == 0)
                    return fromMenu ? "" : null;
                return $"{removed} torii retirés, {kept.Count} gardés";
            });
        }

        static int Number(string name)
        {
            var match = Regex.Match(name, @"\d+");
            return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : 0;
        }

        // Les maillages et les collisions des piliers, refaits à la hauteur actuelle (k_Height), une fois par version.
        static void UpdateToriiShape(bool force)
        {
            var path = $"{k_Folder}/Torii.prefab";
            var importer = AssetImporter.GetAtPath(path);
            if (importer == null || (!force && importer.userData == k_ToriiShape))
                return;

            SaveMesh(ToriiPillars(), "Torii_Piliers");
            SaveMesh(ToriiBeam(), "Torii_Traverse");
            SaveMesh(ToriiLintel(), "Torii_Linteau");
            SaveMesh(ToriiRoof(), "Torii_Toit");
            AssetDatabase.SaveAssets();

            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var capsule in contents.GetComponentsInChildren<CapsuleCollider>(true))
                {
                    capsule.center = new Vector3(0f, k_Height * 0.5f, 0f);
                    capsule.height = k_Height;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            importer = AssetImporter.GetAtPath(path);
            importer.userData = k_ToriiShape;
            importer.SaveAndReimport();
            Debug.Log($"Dimension des étoiles : torii de {k_Height} m de haut (maillages et collisions du préfab Torii refaits).");
        }

        // Le nombre de torii : dans la scène ouverte, ses piliers ; sinon, les instances du préfab dans le fichier.
        static int ToriiCount(string sceneText)
        {
            var scene = SceneManager.GetSceneByPath(k_ScenePath);
            if (scene.isLoaded)
            {
                var posts = 0;
                foreach (var root in scene.GetRootGameObjects())
                    posts += root.GetComponentsInChildren<CapsuleCollider>(true).Length;
                return posts / 2;
            }

            var guid = AssetDatabase.AssetPathToGUID($"{k_Folder}/Torii.prefab");
            return string.IsNullOrEmpty(guid) ? 0 : Regex.Matches(sceneText, "m_SourcePrefab: \\{fileID: \\d+, guid: " + guid).Count;
        }

        [MenuItem("Archery/Portail des étoiles/Ajouter des torii dans les trous")]
        static void AddToriiFromMenu() => AddTorii(true);

        // Plante des torii là où il reste de la place, sans toucher à ceux qui existent (même déplacés à la main).
        static void AddTorii(bool fromMenu)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{k_Folder}/Torii.prefab");
            if (prefab == null)
                return;

            EditDimension(scene =>
            {
                var dimension = FindDimension(scene);
                if (dimension == null)
                    return null;

                var root = dimension.transform;
                var toriiRoot = new SerializedObject(dimension).FindProperty("m_Torii").objectReferenceValue as Transform;
                if (toriiRoot == null)
                    toriiRoot = root.Find("Torii");
                if (toriiRoot == null)
                    return null;

                var arena = toriiRoot.Find(k_ArenaGroup);
                if (arena == null)
                    arena = Child(toriiRoot, k_ArenaGroup).transform;
                var far = toriiRoot.Find(k_FarGroup);
                if (far == null)
                    far = Child(toriiRoot, k_FarGroup).transform;

                // Les places prises : les centres des torii et leurs piliers.
                var centers = new List<Vector2>();
                var pillars = new List<Vector2>();
                var gates = new HashSet<Transform>();
                foreach (var post in toriiRoot.GetComponentsInChildren<CapsuleCollider>(true))
                {
                    var local = root.InverseTransformPoint(post.transform.position);
                    pillars.Add(new Vector2(local.x, local.z));
                    var gate = post.transform.parent;
                    if (gate != null && gates.Add(gate))
                    {
                        var center = root.InverseTransformPoint(gate.position);
                        centers.Add(new Vector2(center.x, center.z));
                    }
                }

                var addedArena = ScatterArena(prefab, arena, centers, pillars, new System.Random(15), arena.childCount + 1);
                var addedFar = ScatterFar(prefab, far, centers, pillars, new System.Random(16), far.childCount + 1);
                if (addedArena + addedFar == 0)
                    return fromMenu ? "" : null;
                return $"{addedArena + addedFar} torii ajoutés ({addedArena} dans la zone de l'épreuve, {addedFar} au loin), " +
                       $"{gates.Count + addedArena + addedFar} en tout";
            });
        }

        static StarDimension FindDimension(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var dimension = root.GetComponentInChildren<StarDimension>(true);
                if (dimension != null)
                    return dimension;
            }

            return null;
        }

        // Une retouche de la scène de la dimension. Ouverte dans l'éditeur, la retouche s'y fait (à enregistrer) ; sinon,
        // la scène est ouverte, retouchée, enregistrée et refermée. La retouche rend ce qu'elle a fait (null : rien changé ;
        // vide : rien changé, mais le dire).
        static void EditDimension(System.Func<Scene, string> edit)
        {
            var scene = SceneManager.GetSceneByPath(k_ScenePath);
            var opened = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Additive);
                opened = true;
            }

            try
            {
                var done = edit(scene);
                if (done == "")
                    Debug.Log("Dimension des étoiles : rien à changer.");
                if (string.IsNullOrEmpty(done))
                    return;

                EditorSceneManager.MarkSceneDirty(scene);
                if (opened)
                {
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log($"Dimension des étoiles : {done}. Scène enregistrée.");
                }
                else
                {
                    Debug.Log($"Dimension des étoiles : {done}, dans la scène ouverte. Enregistre-la (Ctrl+S).");
                }
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        // Un torii penché dans le fichier de la scène : une rotation de préfab qui n'est pas qu'autour de la verticale
        // (les torii sont les seules instances de préfab de la dimension).
        static bool HasTiltedTorii(string sceneText)
        {
            foreach (Match match in Regex.Matches(sceneText, @"propertyPath: m_LocalRotation\.[xz]\s*\r?\n\s*value: (\S+)"))
            {
                if (float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) &&
                    Mathf.Abs(value) > 1e-4f)
                    return true;
            }

            return false;
        }

        [MenuItem("Archery/Portail des étoiles/Redresser les torii")]
        static void StraightenFromMenu() => StraightenTorii(true);

        // Remet tous les torii droits, en gardant leur orientation autour de la verticale.
        static void StraightenTorii(bool fromMenu)
        {
            EditDimension(scene =>
            {
                var count = 0;
                var gates = new HashSet<Transform>();
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var post in root.GetComponentsInChildren<CapsuleCollider>(true))
                    {
                        var gate = post.transform.parent;
                        if (gate == null || !gates.Add(gate) || Vector3.Angle(gate.up, Vector3.up) < 0.01f)
                            continue;

                        var forward = Vector3.ProjectOnPlane(gate.forward, Vector3.up);
                        if (forward.sqrMagnitude < 1e-6f)
                            forward = Vector3.ProjectOnPlane(gate.up, Vector3.up);
                        Undo.RecordObject(gate, "Redresser les torii");
                        gate.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(gate);
                        count++;
                    }
                }

                if (count == 0)
                    return fromMenu ? "" : null;
                return $"{count} torii penchés redressés";
            });
        }

        // ---------------------------------------------------------------- l'aspect des torii

        // Version de l'aspect des torii, notée dans le .meta du matériau rouge : l'aspect n'est appliqué tout seul qu'une
        // fois par version (les retouches faites ensuite à la main sur le matériau sont gardées).
        const string k_ToriiLook = "torii-laque-unie-metal-1";

        [MenuItem("Archery/Portail des étoiles/Refaire l'aspect des torii")]
        static void LookFromMenu() => UpdateToriiLook(true);

        // Une laque rouge unie, un peu métallique : elle accroche la lumière et reflète légèrement le ciel, sans briller en
        // rouge vif. Le dessus (visible seulement dans le reflet) en laque noire, un peu plus métallique.
        static void UpdateToriiLook(bool force)
        {
            var redPath = $"{k_Folder}/Torii_Vermillon.mat";
            var red = AssetDatabase.LoadAssetAtPath<Material>(redPath);
            var importer = AssetImporter.GetAtPath(redPath);
            if (red == null || importer == null || (!force && importer.userData == k_ToriiLook))
                return;

            ApplyLacquer(red, new Color(0.55f, 0.14f, 0.1f), 0.35f, 0.6f, new Color(0.08f, 0.02f, 0.015f));
            var black = AssetDatabase.LoadAssetAtPath<Material>($"{k_Folder}/Torii_Noir.mat");
            if (black != null)
                ApplyLacquer(black, new Color(0.05f, 0.045f, 0.05f), 0.5f, 0.7f, Color.black);

            AssetDatabase.SaveAssets();
            importer.userData = k_ToriiLook;
            importer.SaveAndReimport();
            Debug.Log("Dimension des étoiles : torii en laque rouge unie, légèrement métallique (Torii_Vermillon, Torii_Noir).");
        }

        // Une couleur unie (sans texture ni relief), un peu de métal et de brillant, et une faible lueur pour qu'on devine
        // les torii dans l'obscurité.
        static void ApplyLacquer(Material material, Color color, float metallic, float smoothness, Color emission)
        {
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", null);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTexture("_BumpMap", null);
            material.DisableKeyword("_NORMALMAP");
            material.SetTexture("_EmissionMap", null);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(material);
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
                UpdateToriiLook(false);
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
                // Laque rouge pour les torii, noire pour leur dessus : l'aspect est posé juste après (UpdateToriiLook).
                Red = SaveMaterial("Torii_Vermillon", lit, _ => { }),
                Black = SaveMaterial("Torii_Noir", lit, _ => { }),
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
            var arena = Child(toriiRoot.transform, k_ArenaGroup);
            var far = Child(toriiRoot.transform, k_FarGroup);
            var centers = new List<Vector2>();
            var pillars = new List<Vector2>();
            var arenaCount = ScatterArena(torii, arena.transform, centers, pillars, new System.Random(5), 1);
            var farCount = ScatterFar(torii, far.transform, centers, pillars, new System.Random(6), 1);

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
            var clip = settings.FindProperty("m_Music");
            if (clip.objectReferenceValue == null)
                clip.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(k_Music);
            SetUpMusicSource(source, clip.objectReferenceValue as AudioClip);

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

        // Les écarts entre les centres des torii : 7 m dans la zone de l'épreuve, de plus en plus au loin (environ 125 torii
        // en tout ; 6 m et 6 + 0,08 r donnaient 230 torii, trop serrés).
        const float k_ArenaGap = 7f;
        static float FarGap(float radius) => 7f + 0.09f * radius;

        // Les torii de l'épreuve : entre 5 m (la place du joueur à l'arrivée) et 20,5 m (les murs sont à 23 m).
        static int ScatterArena(GameObject prefab, Transform parent, List<Vector2> centers, List<Vector2> pillars, System.Random random,
                                int firstNumber) =>
            Scatter(prefab, parent, centers, pillars, random, 5f, 20.5f, 8000, _ => k_ArenaGap, _ => 1.1f, firstNumber);

        // Les torii lointains, jusqu'à 120 m : de plus en plus grands et espacés.
        static int ScatterFar(GameObject prefab, Transform parent, List<Vector2> centers, List<Vector2> pillars, System.Random random,
                              int firstNumber) =>
            Scatter(prefab, parent, centers, pillars, random, 27f, 120f, 30000, FarGap,
                    r => Mathf.Lerp(1.1f, 2.4f, (r - 27f) / 93f), firstNumber);

        // Des torii semés au hasard dans un anneau (tirages successifs) : leurs centres écartés d'au moins minimumGap(r),
        // leurs piliers d'au moins 2,4 m (à l'échelle du plus grand), tournés n'importe comment autour de la verticale, de
        // tailles variées. Tous droits : penchés, leur reflet dans le sol miroir faisait un V peu joli. Les positions
        // (plan XZ, par rapport à la dimension) de ceux qui existent déjà sont dans centers et pillars.
        static int Scatter(GameObject prefab, Transform parent, List<Vector2> centers, List<Vector2> pillars, System.Random random,
                           float inner, float outer, int attempts, System.Func<float, float> minimumGap,
                           System.Func<float, float> scaleAt, int firstNumber)
        {
            var count = 0;
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                var angle = Range(random, 0f, Mathf.PI * 2f);
                var radius = Mathf.Sqrt(Range(random, inner * inner, outer * outer));
                var point = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                var yaw = Range(random, 0f, 360f);
                var scale = scaleAt(radius) * Range(random, 0.8f, 1.2f);
                var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right * (k_HalfSpan * scale);
                var left = point - new Vector2(right.x, right.z);
                var rightPillar = point + new Vector2(right.x, right.z);
                if (!Free(centers, point, minimumGap(radius)) || !Free(pillars, left, 2.4f * Mathf.Max(1f, scale)) ||
                    !Free(pillars, rightPillar, 2.4f * Mathf.Max(1f, scale)) || left.magnitude < inner - 1.5f ||
                    rightPillar.magnitude < inner - 1.5f)
                    continue;

                centers.Add(point);
                pillars.Add(left);
                pillars.Add(rightPillar);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
                instance.transform.SetParent(parent, false);
                instance.name = $"Torii {firstNumber + count}";
                instance.transform.localPosition = new Vector3(point.x, 0f, point.y);
                instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                instance.transform.localScale = Vector3.one * scale;
                count++;
            }

            return count;
        }

        static bool Free(List<Vector2> placed, Vector2 point, float gap)
        {
            foreach (var other in placed)
            {
                if ((other - point).sqrMagnitude < gap * gap)
                    return false;
            }

            return true;
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
                mesh.RecalculateTangents();
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
