using Archery.Bows;
using Archery.Grenades;
using Archery.Menus;
using Archery.UI;
using TMPro;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// La grenade à flèches, après les tests du 22 octobre. Dans la scène ouverte :
    /// la ceinture passe aux hanches, comme des poches (y de -0,95 à -0,45 sous les yeux, z de -0,35 à 0,12 ; elle allait
    /// de -1,05 à -0,62, au niveau des cuisses, loin derrière le dos) et le carquois s'arrête au-dessus (-0,42 au lieu
    /// de -0,6, sinon il prenait le bas du dos), une fois, s'ils ont encore ces anciennes valeurs ; la ceinture reprend
    /// le son du carquois, qu'elle n'avait pas.
    /// Sur la montre, les lignes « Grenade » et « Buffs » étaient des textes TextMeshPro 3D dans le Canvas : leur police
    /// y compte dix fois moins, et les lettres faisaient 1 mm. Elles deviennent des textes d'interface, réglés comme leurs
    /// voisines. La scène est marquée comme modifiée : il suffit de l'enregistrer (Ctrl+S).
    /// Ajoute aussi la fenêtre de la grenade à la catégorie Tir du tutoriel (<c>Resources/TutorialContent</c>).
    /// Enfin, son visuel (choisi le 22 octobre) : la bombe de Quaternius (CC0, <c>Bomb by Quaternius - qGUAks6IHQ</c>)
    /// remplace la boule noire du prefab, réduite à 11 cm, en fonte sombre avec une mèche de corde. La mèche du modèle
    /// est éteinte : des étincelles, une lueur qui vacille et un filet de fumée la font brûler (plus fort une fois
    /// lancée). Une fois, tant que le prefab n'a pas son enfant « Bombe ».
    /// </summary>
    [InitializeOnLoad]
    static class GrenadeTuningBaker
    {
        const string k_ContentPath = "Assets/_Project/Resources/TutorialContent.asset";
        const string k_OldGrenadeText =
            "Passe une main vide dans le bas de ton dos et serre la poignée : une grenade sort. Lance-la : elle éclate sur un " +
            "ennemi ou au sol en une gerbe de flèches. Ta montre affiche sa recharge.";
        const string k_PrefabPath = "Assets/_Project/Prefabs/Grenade/Arrow Grenade.prefab";
        const string k_ModelPath = "Assets/Bomb by Quaternius - qGUAks6IHQ/Bomb.fbx";
        const string k_ArtFolder = "Assets/_Project/Art/Grenade";
        const string k_SparkMaterialPath = "Assets/_Project/Materials/Fire_Particles.mat";
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const string k_ModelName = "Bombe";

        // Diamètre de la boule, mèche non comprise : une grenade tient dans la main.
        const float k_BodyDiameter = 0.11f;
        const float k_OldQuiverBottom = -0.6f;
        const float k_QuiverBottom = -0.42f;
        static readonly Vector2 k_OldBelt = new Vector2(-1.05f, -0.62f);

        // Le bas du dos, d'abord (22 octobre), avec z de -0,5 à -0,02 : on devait aller la chercher loin derrière.
        static readonly Vector2 k_BackBelt = new Vector2(-0.9f, -0.42f);

        // Puis les hanches, comme des poches, le même jour : y et z du coin minimal, puis du coin maximal.
        static readonly Vector2 k_HipMin = new Vector2(-0.95f, -0.35f);
        static readonly Vector2 k_HipMax = new Vector2(-0.45f, 0.12f);

        static GrenadeTuningBaker()
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

            AddTutorialWindow();
            ApplyLook();
            DoubleArrows();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    Apply(scene);
            }
        }

        static void AddTutorialWindow()
        {
            var content = AssetDatabase.LoadAssetAtPath<TutorialContent>(k_ContentPath);
            if (content == null)
                return;

            var section = content.sections.Find(s => s.videoPrefix == "Tir");
            if (section == null)
                return;

            // Le premier texte (22 octobre) parlait du bas du dos : la grenade se prend maintenant à la hanche.
            var existing = section.windows.Find(w => w.title == TutorialContent.GrenadeTitle);
            if (existing != null)
            {
                if (existing.text == k_OldGrenadeText)
                {
                    existing.text = TutorialContent.GrenadeText;
                    EditorUtility.SetDirty(content);
                    AssetDatabase.SaveAssetIfDirty(content);
                    Debug.Log("Tutoriel : la fenêtre de la grenade dit maintenant de la prendre à la hanche.", content);
                }

                return;
            }

            section.windows.Add(new TutorialContent.Window { title = TutorialContent.GrenadeTitle, text = TutorialContent.GrenadeText });
            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssetIfDirty(content);
            Debug.Log($"Tutoriel : fenêtre « {TutorialContent.GrenadeTitle} » ajoutée à la catégorie Tir " +
                      $"(vidéo Resources/Tutoriel/Tir_{section.windows.Count}, « Vidéo à venir » sans elle).", content);
        }

        static void Apply(Scene scene)
        {
            var changes = new System.Collections.Generic.List<string>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var belt in root.GetComponentsInChildren<GrenadeBelt>(true))
                {
                    if (RaiseBelt(belt))
                        changes.Add("ceinture aux hanches, carquois au-dessus");
                }

                foreach (var watch in root.GetComponentsInChildren<GrenadeWatchUI>(true))
                {
                    if (FixText(watch, "m_Text"))
                        changes.Add("ligne « Grenade » de la montre lisible");
                }

                foreach (var hud in root.GetComponentsInChildren<HudDisplay>(true))
                {
                    if (FixText(hud, "m_BuffText"))
                        changes.Add("ligne des bonus de la montre lisible");
                }
            }

            if (changes.Count == 0)
                return;

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Grenade : {string.Join(", ", changes)}, dans la scène « {scene.name} ». Enregistre-la (Ctrl+S).");
        }

        static bool RaiseBelt(GrenadeBelt belt)
        {
            var settings = new SerializedObject(belt);
            var min = settings.FindProperty("m_GrabZoneMin");
            var max = settings.FindProperty("m_GrabZoneMax");
            if (min == null || max == null)
                return false;

            var low = min.vector3Value;
            var high = max.vector3Value;
            var original = Near(low.y, k_OldBelt.x) && Near(high.y, k_OldBelt.y);
            var back = Near(low.y, k_BackBelt.x) && Near(high.y, k_BackBelt.y) && Near(high.z, -0.02f);
            if (!original && !back)
                return false;

            min.vector3Value = new Vector3(low.x, k_HipMin.x, k_HipMin.y);
            max.vector3Value = new Vector3(high.x, k_HipMax.x, k_HipMax.y);

            var quiver = belt.GetComponent<Quiver>();
            if (quiver == null)
                quiver = Object.FindAnyObjectByType<Quiver>(FindObjectsInactive.Include);
            if (quiver != null)
            {
                var quiverSettings = new SerializedObject(quiver);
                var bottom = quiverSettings.FindProperty("m_GrabZoneMin");
                if (bottom != null && Near(bottom.vector3Value.y, k_OldQuiverBottom))
                {
                    bottom.vector3Value = WithY(bottom.vector3Value, k_QuiverBottom);
                    quiverSettings.ApplyModifiedPropertiesWithoutUndo();
                }

                var clip = settings.FindProperty("m_DrawClip");
                var quiverClip = quiverSettings.FindProperty("m_DrawClip");
                if (clip != null && clip.objectReferenceValue == null && quiverClip != null)
                    clip.objectReferenceValue = quiverClip.objectReferenceValue;
            }

            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Un texte 3D dans un Canvas devient un texte d'interface, sur le même objet, réglé comme ses voisins.
        static bool FixText(Component owner, string property)
        {
            var settings = new SerializedObject(owner);
            var reference = settings.FindProperty(property);
            var old = reference != null ? reference.objectReferenceValue as TextMeshPro : null;
            if (old == null || old.GetComponentInParent<Canvas>(true) == null)
                return false;

            var go = old.gameObject;
            var template = Neighbour(go.transform);
            Object.DestroyImmediate(old);
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
                Object.DestroyImmediate(renderer);
            var filter = go.GetComponent<MeshFilter>();
            if (filter != null)
                Object.DestroyImmediate(filter);

            var text = go.AddComponent<TextMeshProUGUI>();
            if (template != null && ComponentUtility.CopyComponent(template))
                ComponentUtility.PasteComponentValues(text);
            text.text = string.Empty;
            text.raycastTarget = false;

            reference.objectReferenceValue = text;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // La ligne « Difficulty » de la montre, sinon un autre petit texte d'interface du même panneau (pas le chrono).
        static TextMeshProUGUI Neighbour(Transform line)
        {
            var parent = line.parent;
            if (parent == null)
                return null;

            TextMeshProUGUI fallback = null;
            foreach (Transform sibling in parent)
            {
                if (sibling == line || !sibling.TryGetComponent<TextMeshProUGUI>(out var text))
                    continue;
                if (sibling.name == "Difficulty")
                    return text;
                if (fallback == null && text.fontSize < 25f)
                    fallback = text;
            }

            return fallback;
        }

        // --- Deux fois plus de flèches (22 octobre) ---

        // Le prefab passe de 14 à 28 flèches (6 à 12 visées) et « Grenade sauvage » de 6 à 12 flèches de plus, une fois,
        // tant qu'ils ont ces anciennes valeurs.
        static void DoubleArrows()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabPath);
            var grenade = prefab != null ? prefab.GetComponent<ArrowGrenade>() : null;
            if (grenade != null)
            {
                var settings = new SerializedObject(grenade);
                var count = settings.FindProperty("m_ArrowCount");
                var aimed = settings.FindProperty("m_AimedArrows");
                if (count != null && count.intValue == 14)
                {
                    count.intValue = 28;
                    if (aimed != null && aimed.intValue == 6)
                        aimed.intValue = 12;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.SaveAssetIfDirty(prefab);
                    Debug.Log("Grenade : 28 flèches au lieu de 14 (12 visées au lieu de 6).", prefab);
                }
            }

            var catalog = AssetDatabase.LoadAssetAtPath<Archery.Shop.ShopCatalog>(k_CatalogPath);
            if (catalog == null)
                return;

            foreach (var upgrade in catalog.upgrades)
            {
                if (upgrade == null || upgrade.effect != Archery.Upgrades.UpgradeEffect.GrenadeArrows || !Mathf.Approximately(upgrade.value, 6f))
                    continue;

                upgrade.value = 12f;
                upgrade.description = upgrade.description.Replace("6 de plus", "12 de plus");
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
                Debug.Log($"Boutique : « {upgrade.displayName} » donne 12 flèches de plus à la grenade au lieu de 6.", catalog);
            }
        }

        // --- Le visuel ---

        static void ApplyLook()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(k_ModelPath);
            if (model == null)
            {
                // Déplacée ou renommée : on cherche une bombe de Quaternius.
                foreach (var guid in AssetDatabase.FindAssets("Bomb t:Model"))
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("Quaternius"))
                        model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            if (model == null || AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabPath) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(k_PrefabPath);
            try
            {
                if (root.transform.Find(k_ModelName) != null || !BuildLook(root, model))
                    return;

                PrefabUtility.SaveAsPrefabAsset(root, k_PrefabPath);
                Debug.Log($"Grenade : la bombe de Quaternius remplace la boule noire ({k_BodyDiameter * 100f:0} cm), " +
                          $"mèche allumée par des étincelles ({k_PrefabPath}).", AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabPath));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static bool BuildLook(GameObject root, GameObject model)
        {
            var bomb = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            bomb.name = k_ModelName;
            bomb.transform.localPosition = Vector3.zero;
            var filter = bomb.GetComponentInChildren<MeshFilter>();
            var renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
            var mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || renderer == null)
            {
                Debug.LogWarning("Grenade : le modèle de la bombe n'a pas de mesh, visuel inchangé.", model);
                return false;
            }

            // Tout se mesure dans le repère de la grenade, avant la mise à l'échelle. L'origine du modèle est le centre
            // de la boule ; son rayon, la distance médiane des sommets du corps (le col dépasse un peu).
            var toRoot = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var bodyIndex = SubMeshNamed(renderer, "Black", 0);
            var fuseIndex = mesh.subMeshCount > 1 ? (bodyIndex == 0 ? 1 : 0) : -1;
            var center = toRoot.MultiplyPoint3x4(Vector3.zero);
            var body = Points(mesh, bodyIndex, toRoot);
            if (body.Count == 0)
                return false;

            var distances = body.ConvertAll(p => (p - center).magnitude);
            distances.Sort();
            var radius = Mathf.Max(1e-4f, distances[distances.Count / 2]);
            var scale = k_BodyDiameter / (2f * radius);
            bomb.transform.localScale *= scale;
            bomb.transform.localPosition = -center * scale;

            // Le bout de la mèche : son point le plus loin de l'endroit où elle sort du col.
            var tip = center + Vector3.up * radius;
            var direction = Vector3.up;
            if (fuseIndex >= 0)
            {
                var fuse = Points(mesh, fuseIndex, toRoot);
                if (fuse.Count > 0)
                {
                    var attach = fuse[0];
                    foreach (var p in fuse)
                    {
                        if ((p - center).sqrMagnitude < (attach - center).sqrMagnitude)
                            attach = p;
                    }

                    var far = attach;
                    foreach (var p in fuse)
                    {
                        if ((p - attach).sqrMagnitude > (far - attach).sqrMagnitude)
                            far = p;
                    }

                    var length = (far - attach).magnitude;
                    tip = Average(fuse.FindAll(p => (p - far).magnitude < length * 0.2f), far);
                    var back = Average(fuse.FindAll(p => (p - far).magnitude > length * 0.3f && (p - far).magnitude < length * 0.6f), attach);
                    direction = (tip - back).sqrMagnitude > 1e-8f ? (tip - back).normalized : (tip - center).normalized;
                }
            }

            // L'ancienne boule : plus de rendu, et la grenade à l'échelle 1 ; le collider prend la boule, sans la mèche.
            var oldRenderer = root.GetComponent<MeshRenderer>();
            if (oldRenderer != null)
                Object.DestroyImmediate(oldRenderer);
            var oldFilter = root.GetComponent<MeshFilter>();
            if (oldFilter != null)
                Object.DestroyImmediate(oldFilter);
            root.transform.localScale = Vector3.one;
            var sphere = root.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                sphere.center = Vector3.zero;
                sphere.radius = k_BodyDiameter * 0.5f;
            }

            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i] = i == bodyIndex
                    ? LitMaterial("Grenade fonte", new Color(0.07f, 0.07f, 0.075f), 0.75f, 0.55f)
                    : LitMaterial("Grenade meche", new Color(0.45f, 0.36f, 0.24f), 0f, 0.15f);
            }

            renderer.sharedMaterials = materials;

            // La mèche allumée, au bout de la corde, tournée dans son sens.
            var fire = new GameObject("Mèche allumée").transform;
            fire.SetParent(root.transform, false);
            fire.localPosition = (tip - center) * scale + direction * 0.002f;
            fire.localRotation = Quaternion.LookRotation(direction, Mathf.Abs(direction.y) > 0.95f ? Vector3.forward : Vector3.up);

            var sparkMaterial = AssetDatabase.LoadAssetAtPath<Material>(k_SparkMaterialPath);
            var sparks = Sparks(fire, sparkMaterial);
            Glow(fire, sparkMaterial);
            Smoke(fire, SmokeMaterial(sparkMaterial));

            var grenade = root.GetComponent<ArrowGrenade>();
            if (grenade != null)
            {
                var settings = new SerializedObject(grenade);
                var property = settings.FindProperty("m_FuseSparks");
                if (property != null)
                {
                    property.objectReferenceValue = sparks;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            return true;
        }

        static int SubMeshNamed(Renderer renderer, string name, int fallback)
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].name == name)
                    return i;
            }

            return fallback;
        }

        static System.Collections.Generic.List<Vector3> Points(Mesh mesh, int subMesh, Matrix4x4 toRoot)
        {
            var points = new System.Collections.Generic.List<Vector3>();
            if (subMesh < 0 || subMesh >= mesh.subMeshCount)
                return points;

            var vertices = mesh.vertices;
            var used = new System.Collections.Generic.HashSet<int>(mesh.GetTriangles(subMesh));
            foreach (var index in used)
                points.Add(toRoot.MultiplyPoint3x4(vertices[index]));
            return points;
        }

        static Vector3 Average(System.Collections.Generic.List<Vector3> points, Vector3 fallback)
        {
            if (points.Count == 0)
                return fallback;

            var sum = Vector3.zero;
            foreach (var p in points)
                sum += p;
            return sum / points.Count;
        }

        static Material LitMaterial(string name, Color color, float metallic, float smoothness)
        {
            var path = $"{k_ArtFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            EnsureArtFolder();
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Les particules du feu de l'arène (additives) donnent étincelles et lueur ; la fumée en est une copie
        // en transparence normale, pour qu'elle assombrisse au lieu d'éclairer.
        static Material SmokeMaterial(Material spark)
        {
            var path = $"{k_ArtFolder}/Grenade fumee.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null || spark == null)
                return material != null ? material : spark;

            EnsureArtFolder();
            material = new Material(spark);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void EnsureArtFolder()
        {
            if (!AssetDatabase.IsValidFolder(k_ArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Grenade");
        }

        static ParticleSystem Sparks(Transform parent, Material material)
        {
            var sparks = NewSystem("Étincelles", parent, material, true);
            var main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.004f, 0.009f);
            main.gravityModifier = 0.35f;
            main.maxParticles = 120;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.7f), new Color(1f, 0.7f, 0.25f));
            var emission = sparks.emission;
            emission.rateOverTime = 45f;
            var shape = sparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.004f;
            Fade(sparks, new Color(1f, 0.95f, 0.7f), new Color(1f, 0.45f, 0.1f), new Color(0.7f, 0.1f, 0.03f));
            var size = sparks.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            // Des traits plutôt que des points : elles filent.
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.03f;
            renderer.lengthScale = 1.5f;
            return sparks;
        }

        // Une braise au bout de la corde : de grosses particules immobiles, brèves, qui vacillent.
        static void Glow(Transform parent, Material material)
        {
            var glow = NewSystem("Lueur", parent, material, false);
            var main = glow.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.022f, 0.04f);
            main.maxParticles = 20;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f, 0.8f), new Color(1f, 0.85f, 0.4f, 0.9f));
            var emission = glow.emission;
            emission.rateOverTime = 60f;
            Fade(glow, new Color(1f, 0.85f, 0.45f), new Color(1f, 0.6f, 0.2f), new Color(1f, 0.4f, 0.1f));
        }

        static void Smoke(Transform parent, Material material)
        {
            var smoke = NewSystem("Fumée", parent, material, true);
            var main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.02f);
            main.gravityModifier = -0.03f;
            main.maxParticles = 30;
            main.startColor = new Color(0.35f, 0.33f, 0.3f, 0.35f);
            var emission = smoke.emission;
            emission.rateOverTime = 14f;
            var shape = smoke.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.002f;
            Fade(smoke, new Color(0.4f, 0.38f, 0.35f), new Color(0.35f, 0.33f, 0.3f), new Color(0.3f, 0.3f, 0.3f));
            var size = smoke.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 3f));
        }

        static ParticleSystem NewSystem(string name, Transform parent, Material material, bool world)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var system = go.AddComponent<ParticleSystem>();
            var main = system.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = true;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            // En espace monde, les étincelles et la fumée traînent derrière la grenade lancée.
            main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            var shape = system.shape;
            shape.enabled = false;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        // Couleur sur la durée de vie : apparaît vite, passe par les trois teintes, s'efface.
        static void Fade(ParticleSystem system, Color start, Color middle, Color end)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(middle, 0.4f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            var color = system.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.005f;

        static Vector3 WithY(Vector3 value, float y) => new Vector3(value.x, y, value.z);
    }
}
