using System.Linq;
using Archery.BowClasses;
using Archery.Bows;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les Akimbow dans le jeu de base : leur fiche (<c>Data/BowClasses/Bow_Akimbow</c>, une arbalète dans chaque main,
    /// 0,4 fois les dégâts de l'arc de départ, une bonne portée, un anneau qui repart vite), leur aperçu pour le menu des
    /// armes (<c>Prefabs/Bow Visual Akimbow</c> : les deux arbalètes debout), et leur carte dans la liste des armes
    /// (<c>Prefabs/UI/Bow Classes</c>), débloquée par le succès secret du toit cyberpunk (« Intrus »), dont l'énigme devient
    /// « Il te voit clair comme de l'eau de roche. ».
    /// </summary>
    /// <remarks>
    /// Se fait tout seul une fois (tant que la fiche n'existe pas, n'est pas dans la liste, ou n'a pas le look actuel : les
    /// matériaux <c>Art/Cyber/Akimbow gauche</c> et <c>Akimbow droite</c>, shader <c>Archery/Akimbow</c>, une arme différente
    /// dans chaque main). Les réglages de tir de la fiche ne sont écrits qu'à sa création : ensuite, c'est elle qui fait foi
    /// (on la règle dans l'inspecteur). Menu
    /// <c>Archery > Akimbow > Refaire l'aperçu et la carte</c> : refait l'aperçu et remet la carte si elle manque.
    /// </remarks>
    [InitializeOnLoad]
    static class AkimbowBaker
    {
        const string k_Definition = "Assets/_Project/Data/BowClasses/Bow_Akimbow.asset";
        const string k_Visual = "Assets/_Project/Prefabs/Bow Visual Akimbow.prefab";
        const string k_Loadout = "Assets/_Project/Prefabs/UI/Bow Classes.prefab";
        const string k_Achievement = "Assets/_Project/Data/Achievements/Succes_Cyber.asset";
        const string k_Model = "Assets/_Project/Art/Cyber/Arbalete.obj";
        const string k_FireClip = "Assets/_Project/Audio/Placeholder/bow_release.wav";
        const string k_Shader = "Assets/_Project/Shaders/Akimbow.shader";
        const string k_Folder = "Assets/_Project/Art/Cyber";

        // Le look (20 octobre au soir) : un métal en plaques stable aux néons, une arme différente dans chaque main. Avant :
        // le matériau du toit (Archery/Glitch), qui clignotait bleu et violet.
        const string k_LeftMaterial = k_Folder + "/Akimbow gauche.mat";
        const string k_RightMaterial = k_Folder + "/Akimbow droite.mat";
        static readonly string[] k_LeftNeons = { k_Folder + "/Neon cyan.mat", k_Folder + "/Akimbow neon bleu.mat" };
        static readonly string[] k_RightNeons = { k_Folder + "/Neon rose.mat", k_Folder + "/Akimbow neon orange.mat" };

        // Les touches de couleur (20 octobre au soir : le métal était trop uni) : à gauche, ambre et gris clair ; à droite, sarcelle
        // et jaune.
        static readonly Color k_LeftAccent = new Color(0.55f, 0.32f, 0.05f);
        static readonly Color k_LeftAccentAlt = new Color(0.4f, 0.42f, 0.45f);
        static readonly Color k_RightAccent = new Color(0.04f, 0.4f, 0.42f);
        static readonly Color k_RightAccentAlt = new Color(0.6f, 0.5f, 0.08f);

        const string k_Riddle = "Il te voit clair comme de l'eau de roche.";
        static readonly Color k_Color = new Color(0.25f, 0.95f, 1f, 1f);

        // Les dégâts, en part de ceux de l'arc de départ (le premier de la liste).
        const float k_DamageShare = 0.4f;

        // L'aperçu, agrandi : les arbalètes ont la taille d'un arc dans le menu (tous les aperçus y ont la même échelle).
        const float k_PreviewScale = 2.5f;

        static AkimbowBaker()
        {
            EditorApplication.delayCall -= AutoBake;
            EditorApplication.delayCall += AutoBake;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode)
                    return;
                EditorApplication.delayCall -= AutoBake;
                EditorApplication.delayCall += AutoBake;
            };
        }

        static void AutoBake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;
            var definition = AssetDatabase.LoadAssetAtPath<BowDefinition>(k_Definition);
            if (definition != null && definition.visualPrefab != null && InLoadout(definition) && HasLook(definition))
                return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(k_Model) == null || AssetDatabase.LoadAssetAtPath<GameObject>(k_Loadout) == null ||
                AssetDatabase.LoadAssetAtPath<Shader>(k_Shader) == null)
                return;
            Bake();
        }

        // Le look actuel : les deux matériaux du shader Archery/Akimbow (avec leurs touches de couleur), et les néons des deux mains.
        static bool HasLook(BowDefinition definition) =>
            definition.akimboMaterial != null && definition.akimboMaterialRight != null &&
            AssetDatabase.GetAssetPath(definition.akimboMaterial) == k_LeftMaterial &&
            AssetDatabase.GetAssetPath(definition.akimboMaterialRight) == k_RightMaterial &&
            definition.akimboMaterialRight.HasProperty("_AccentColor") &&
            Same(definition.akimboMaterialRight.GetColor("_AccentColor"), k_RightAccent) &&
            definition.akimboNeonRight != null && definition.akimboNeonRight.Length > 0;

        static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.01f;

        [MenuItem("Archery/Akimbow/Refaire l'aperçu et la carte")]
        static void Bake()
        {
            var definition = AssetDatabase.LoadAssetAtPath<BowDefinition>(k_Definition);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BowDefinition>();
                Fill(definition);
                AssetDatabase.CreateAsset(definition, k_Definition);
                SetRiddle();
            }

            if (!HasLook(definition))
                ApplyLook(definition);
            definition.visualPrefab = BuildVisual(definition);
            EditorUtility.SetDirty(definition);
            AddToLoadout(definition);
            AssetDatabase.SaveAssets();
            Debug.Log($"Akimbow : fiche {k_Definition}, aperçu {k_Visual} et carte dans {k_Loadout} (débloquée par « Intrus »). " +
                      "Raccourci de test : U débloque les succès, puis la touche à droite du 0 (« ) ») les équipe avant la vague 1.");
        }

        // Les réglages de départ : 0,4 fois les dégâts de l'arc de départ, des flèches rapides qui tombent moins (une bonne
        // portée), un anneau qui repart vite après chaque tir.
        static void Fill(BowDefinition definition)
        {
            var baseDamage = 10f;
            var loadout = LoadLoadout(out var root);
            if (loadout != null && loadout.Entries.Count > 0 && loadout.Entries[0].bow != null)
                baseDamage = loadout.Entries[0].bow.damage;
            if (root != null)
                PrefabUtility.UnloadPrefabContents(root);

            definition.displayName = "Akimbow";
            definition.description = k_Riddle;
            definition.specialPower = "Une arbalète dans chaque main, qui se recharge toute seule";
            definition.damage = Mathf.Round(baseDamage * k_DamageShare * 100f) / 100f;
            definition.arrowSpeed = 45f;
            definition.arrowGravity = 0.85f;
            definition.ringDuration = 0.6f;
            definition.goldHalfWidth = 0.07f;
            definition.goodWidth = 0.15f;
            definition.akimbo = true;
            definition.akimboModel = AssetDatabase.LoadAssetAtPath<GameObject>(k_Model);
            definition.akimboTip = new Vector3(0f, 0.096f, 0.285f);
            definition.akimboScale = 0.7f;
            definition.akimboFireClip = AssetDatabase.LoadAssetAtPath<AudioClip>(k_FireClip);
            ApplyLook(definition);
        }

        // Deux armes différentes : à gauche, un métal bleu acier aux néons cyan et bleus, plaques serrées ; à droite, un
        // métal prune aux néons roses et orange, plaques plus larges, autre motif.
        static void ApplyLook(BowDefinition definition)
        {
            definition.akimboMaterial = BodyMaterial(k_LeftMaterial, new Color(0.045f, 0.055f, 0.075f), new Color(0.12f, 0.14f, 0.18f),
                                                     new Color(0.2f, 1.4f, 2.2f), new Color(0.35f, 0.5f, 2.6f), 22f, 0f, 0.09f,
                                                     k_LeftAccent, k_LeftAccentAlt);
            definition.akimboMaterialRight = BodyMaterial(k_RightMaterial, new Color(0.07f, 0.035f, 0.065f), new Color(0.16f, 0.08f, 0.13f),
                                                          new Color(2.2f, 0.25f, 1.4f), new Color(2.6f, 0.9f, 0.15f), 15f, 7.3f, 0.075f,
                                                          k_RightAccent, k_RightAccentAlt);
            NeonMaterial(k_LeftNeons[1], new Color(0.25f, 0.45f, 2.6f));
            NeonMaterial(k_RightNeons[1], new Color(2.6f, 0.9f, 0.15f));
            definition.akimboNeon = k_LeftNeons.Select(AssetDatabase.LoadAssetAtPath<Material>).Where(material => material != null).ToArray();
            definition.akimboNeonRight = k_RightNeons.Select(AssetDatabase.LoadAssetAtPath<Material>).Where(material => material != null).ToArray();
            EditorUtility.SetDirty(definition);
        }

        static Material BodyMaterial(string path, Color body, Color panels, Color neon, Color neonAlt, float density, float seed, float stripe,
                                     Color accent, Color accentAlt)
        {
            var material = LoadOrCreate(path, AssetDatabase.LoadAssetAtPath<Shader>(k_Shader));
            material.SetColor("_BodyColor", body);
            material.SetColor("_PanelColor", panels);
            material.SetColor("_AccentColor", accent);
            material.SetColor("_AccentColorAlt", accentAlt);
            material.SetFloat("_Wear", 0.4f);
            material.SetColor("_NeonColor", neon);
            material.SetColor("_NeonColorAlt", neonAlt);
            material.SetFloat("_PanelDensity", density);
            material.SetFloat("_Seed", seed);
            material.SetFloat("_StripeHeight", stripe);
            material.SetFloat("_RimStrength", 0.35f);
            material.SetFloat("_Glitch", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        // Un néon (URP Lit, émissif), comme ceux du toit.
        static void NeonMaterial(string path, Color emission)
        {
            var material = LoadOrCreate(path, Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", Color.black);
            material.SetColor("_Color", Color.black);
            material.SetFloat("_Smoothness", 0.5f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
        }

        static Material LoadOrCreate(string path, Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                if (shader != null && material.shader != shader)
                    material.shader = shader;
                return material;
            }

            material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // L'énigme de la carte verrouillée (le succès secret du toit), donnée par le joueur le 20 octobre.
        static void SetRiddle()
        {
            var achievement = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(k_Achievement);
            if (achievement == null || achievement.description == k_Riddle)
                return;
            achievement.description = k_Riddle;
            EditorUtility.SetDirty(achievement);
        }

        // Les deux arbalètes debout, canon en haut, un peu écartées en V, pour l'aperçu qui tourne dans la carte.
        static BowVisual BuildVisual(BowDefinition definition)
        {
            if (definition.akimboModel == null)
                return null;

            var root = new GameObject("Bow Visual Akimbow");
            try
            {
                root.AddComponent<BowVisual>();
                var pair = new GameObject("Paire").transform;
                pair.SetParent(root.transform, false);
                pair.localScale = Vector3.one * k_PreviewScale;
                foreach (var side in new[] { -1f, 1f })
                {
                    var crossbow = Akimbow.CreateCrossbow(definition, pair, side > 0f, out _);
                    crossbow.localPosition = new Vector3(side * 0.07f, 0f, 0f);
                    crossbow.localRotation = Quaternion.AngleAxis(-side * 12f, Vector3.forward) * Quaternion.Euler(-90f, 0f, 0f);
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, k_Visual);
                return prefab != null ? prefab.GetComponent<BowVisual>() : null;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static BowLoadout LoadLoadout(out GameObject root)
        {
            root = AssetDatabase.LoadAssetAtPath<GameObject>(k_Loadout) != null ? PrefabUtility.LoadPrefabContents(k_Loadout) : null;
            return root != null ? root.GetComponentInChildren<BowLoadout>(true) : null;
        }

        static bool InLoadout(BowDefinition definition)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_Loadout);
            var loadout = prefab != null ? prefab.GetComponentInChildren<BowLoadout>(true) : null;
            return loadout != null && loadout.Entries.Any(entry => entry != null && entry.bow == definition);
        }

        // La carte des Akimbow, à la fin de la liste, débloquée par « Intrus » (fichier du prefab de l'équipe : une ligne
        // de plus dans sa liste, rien d'autre).
        static void AddToLoadout(BowDefinition definition)
        {
            if (InLoadout(definition))
                return;

            var loadout = LoadLoadout(out var root);
            if (loadout == null)
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
                Debug.LogWarning($"Akimbow : pas de Bow Loadout dans {k_Loadout}.");
                return;
            }

            try
            {
                var settings = new SerializedObject(loadout);
                var entries = settings.FindProperty("m_Entries");
                entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                entry.FindPropertyRelative("bow").objectReferenceValue = definition;
                entry.FindPropertyRelative("unlockedBy").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(k_Achievement);
                entry.FindPropertyRelative("color").colorValue = k_Color;
                entry.FindPropertyRelative("comingSoonName").stringValue = "";
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, k_Loadout);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
