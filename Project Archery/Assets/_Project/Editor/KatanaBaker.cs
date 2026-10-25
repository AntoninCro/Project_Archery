using System.Collections.Generic;
using System.Linq;
using Archery.BowClasses;
using Archery.Bows;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le katana dans le jeu (refait le 22 octobre ; avant, un katana et un bouclier faits ici ; le 23 octobre, le symbole à
    /// tracer est retiré ; le 24 octobre, le gantelet aussi : la main de l'arc devient le gant de la matrice) : le katana de
    /// Pichuliru (CC0), remis dans le repère attendu par le jeu, avec ses matériaux (<c>Art/Katana</c> : lame néon qui bugue),
    /// ceux des failles, du gant de la matrice, de la traînée de la lame et des chiffres des ennemis tués, sa fiche
    /// (<c>Data/BowClasses/Bow_Katana</c>), le succès qui le débloque (« Rōnin » : vaincre 2 boss de vague,
    /// <c>Data/Achievements/Succes_Ronin</c>), l'aperçu du menu des armes (<c>Prefabs/Bow Visual Katana</c>) et sa carte dans
    /// la liste des armes (<c>Prefabs/UI/Bow Classes</c>, avec le succès dans la liste des succès).
    /// </summary>
    /// <remarks>
    /// Se fait tout seul (une fois, et de nouveau tant que la fiche n'a pas tous ses matériaux ou garde un ancien texte). La
    /// fiche et le succès ne sont écrits qu'à leur création : ensuite, ce sont eux qui font foi ; seuls les textes et les sons
    /// laissés à leur valeur d'origine sont mis à jour. Menu <c>Archery > Katana > Refaire les modèles et la carte</c>.
    /// L'ancien gantelet (<c>Art/Katana/Gantelet.prefab</c> et ses matériaux) n'est plus utilisé.
    /// </remarks>
    [InitializeOnLoad]
    static class KatanaBaker
    {
        const string k_Folder = "Assets/_Project/Art/Katana";
        const string k_Definition = "Assets/_Project/Data/BowClasses/Bow_Katana.asset";
        const string k_Achievement = "Assets/_Project/Data/Achievements/Succes_Ronin.asset";
        const string k_Visual = "Assets/_Project/Prefabs/Bow Visual Katana.prefab";
        const string k_Loadout = "Assets/_Project/Prefabs/UI/Bow Classes.prefab";
        const string k_TearShader = "Assets/_Project/Shaders/MatrixTear.shader";
        const string k_BladeShader = "Assets/_Project/Shaders/KatanaBlade.shader";
        const string k_GloveShader = "Assets/_Project/Shaders/MatrixGlove.shader";
        const string k_GlowShader = "Assets/_Project/Shaders/Glow.shader";
        const string k_DigitsShader = "Assets/_Project/Shaders/MatrixDigits.shader";
        const string k_KatanaModel = "Assets/Katana by Pichuliru - 15A7InejC7/Flat Melee Bladed/Katana.fbx";
        const string k_HitClip = "Assets/kenney_rpg-audio/Audio/chop.ogg";
        const string k_LaunchClip = "Assets/650564__h2p34__lightsaber-whoosh.wav";
        const string k_SwooshClip = "Assets/507466__danjocross__clean-fast-swoosh.aiff";
        const string k_CrackClip = "Assets/kenney_impact-sounds/Audio/impactGlass_medium_001.ogg";
        const string k_SignClip = "Assets/kenney_interfaceSounds/Audio/glitch_004.ogg";

        // Le katana : de la poignée (son milieu) à la pointe. « Tranchant » : un repère du côté du tranchant (+Z), posé depuis
        // que le katana a été retourné (24 octobre) ; un katana sans lui est refait.
        const float k_TipDistance = 0.85f;
        const string k_EdgeMarker = "Tranchant";

        static readonly Color k_CardColor = new Color(0.95f, 0.35f, 0.4f, 1f);

        // Les textes d'avant (katana et bouclier, katana et gantelet aux symboles, puis aux failles), remplacés s'ils n'ont
        // pas été changés à la main.
        static readonly string[] k_OldNames = { "Katana et bouclier", "Katana et gantelet" };
        static readonly string[] k_OldDescriptions =
        {
            "Le katana déchire la matrice ; le bouclier renvoie la déchirure.",
            "Le katana fend la matrice ; le gantelet envoie le signe.",
            "Le katana fend la matrice ; le gantelet lance la faille.",
        };
        static readonly string[] k_OldPowers =
        {
            "Frappe la déchirure du katana avec le bouclier pour la lancer ; le bouclier arrête coups et projectiles",
            "Trace le symbole au katana, puis touche le signe avec le gantelet : il file vers l'ennemi que tu regardes",
            "Une taille rapide ouvre une faille ; frappe-la vite avec le gantelet : elle file vers l'ennemi que tu regardes",
        };

        const string k_Name = "Katana";
        const string k_Description = "Le katana fend la matrice, ta main lance la faille ; des failles croisées fusionnent.";
        const string k_Power = "Une taille ouvre une faille, plus forte si le coup est long ; frappe-la de la main : elle file vers l'ennemi que tu regardes";

        static KatanaBaker()
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
            if (AssetDatabase.LoadAssetAtPath<Shader>(k_TearShader) == null || AssetDatabase.LoadAssetAtPath<Shader>(k_GloveShader) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(k_Loadout) == null)
                return;
            var definition = AssetDatabase.LoadAssetAtPath<BowDefinition>(k_Definition);
            if (definition != null && definition.katanaModel != null && definition.katanaModel.transform.Find(k_EdgeMarker) != null &&
                definition.katanaTearMaterial != null &&
                definition.katanaGloveMaterial != null && definition.katanaTrailMaterial != null && definition.katanaDigitsMaterial != null &&
                definition.visualPrefab != null && InLoadout(definition) && !k_OldNames.Contains(definition.displayName) &&
                !k_OldPowers.Contains(definition.specialPower))
                return;
            Bake();
        }

        [MenuItem("Archery/Katana/Refaire les modèles et la carte")]
        static void Bake()
        {
            if (!AssetDatabase.IsValidFolder(k_Folder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Katana");

            var katanaModel = FindModel(k_KatanaModel, "Katana", "Pichuliru");
            if (katanaModel == null)
            {
                Debug.LogWarning("Katana : il manque le modèle du katana (Katana by Pichuliru) dans Assets.");
                return;
            }

            var materials = new Materials();
            var katana = BuildKatana(katanaModel, materials);
            if (katana == null)
                return;

            var achievement = AssetDatabase.LoadAssetAtPath<AchievementDefinition>(k_Achievement);
            if (achievement == null)
            {
                achievement = ScriptableObject.CreateInstance<AchievementDefinition>();
                achievement.title = "Rōnin";
                achievement.description = "Vaincre 2 boss de vague.";
                achievement.type = AchievementType.BossesKilled;
                achievement.target = 2;
                achievement.color = k_CardColor;
                AssetDatabase.CreateAsset(achievement, k_Achievement);
            }

            var definition = AssetDatabase.LoadAssetAtPath<BowDefinition>(k_Definition);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BowDefinition>();
                Fill(definition);
                AssetDatabase.CreateAsset(definition, k_Definition);
            }

            definition.katana = true;
            definition.katanaModel = katana;
            if (definition.katanaTearMaterial == null)
                definition.katanaTearMaterial = materials.Tear;
            if (definition.katanaGloveMaterial == null)
                definition.katanaGloveMaterial = materials.Glove;
            if (definition.katanaTrailMaterial == null)
                definition.katanaTrailMaterial = materials.Trail;
            if (definition.katanaDigitsMaterial == null)
                definition.katanaDigitsMaterial = materials.Digits;
            Migrate(definition);
            definition.visualPrefab = BuildVisual(katana);
            EditorUtility.SetDirty(definition);

            AddToLoadout(definition, achievement);
            AssetDatabase.SaveAssets();
            Debug.Log($"Katana : modèle et matériaux dans {k_Folder} (faille, gant de la matrice, traînée, chiffres), fiche {k_Definition}, " +
                      $"carte dans {k_Loadout} (débloquée par « Rōnin »). Raccourci de test : O l'équipe (même verrouillé), O encore rend " +
                      "l'arme d'avant.");
        }

        // Les réglages de départ. La faille lancée fait les dégâts d'une flèche d'arc puissant, le coup de katana trois fois
        // ceux de l'arc de chasse.
        static void Fill(BowDefinition definition)
        {
            definition.displayName = k_Name;
            definition.description = k_Description;
            definition.specialPower = k_Power;
            definition.damage = 15f;
            definition.arrowSpeed = 34f;
            definition.arrowGravity = 1f;
            definition.ringDuration = 1f;
            definition.katanaDamage = 30f;
            definition.katanaHitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(k_HitClip);
        }

        // Les textes d'origine des versions d'avant changent, les sons manquants arrivent (le lancer prend le sabre laser au
        // lieu du bug). Les valeurs de jeu restent.
        static void Migrate(BowDefinition definition)
        {
            if (k_OldNames.Contains(definition.displayName))
                definition.displayName = k_Name;
            if (k_OldDescriptions.Contains(definition.description))
                definition.description = k_Description;
            if (k_OldPowers.Contains(definition.specialPower))
                definition.specialPower = k_Power;

            var oldLaunch = AssetDatabase.LoadAssetAtPath<AudioClip>(k_SignClip);
            if (definition.katanaLaunchClip == null || definition.katanaLaunchClip == oldLaunch)
                definition.katanaLaunchClip = FindClip(k_LaunchClip, "lightsaber-whoosh");
            if (definition.katanaSwooshClip == null)
                definition.katanaSwooshClip = FindClip(k_SwooshClip, "clean-fast-swoosh");
            if (definition.katanaCrackClip == null)
                definition.katanaCrackClip = AssetDatabase.LoadAssetAtPath<AudioClip>(k_CrackClip);
            if (definition.katanaSignClip == null)
                definition.katanaSignClip = oldLaunch;
            if (definition.katanaHitClip == null)
                definition.katanaHitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(k_HitClip);
        }

        static GameObject FindModel(string path, string name, string folderHint)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null)
                return model;
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Model"))
            {
                var candidate = AssetDatabase.GUIDToAssetPath(guid);
                if (candidate.ToLowerInvariant().Contains(folderHint.ToLowerInvariant()))
                    return AssetDatabase.LoadAssetAtPath<GameObject>(candidate);
            }

            return null;
        }

        static AudioClip FindClip(string path, string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
                return clip;
            var guid = AssetDatabase.FindAssets(name + " t:AudioClip").FirstOrDefault();
            return guid != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid)) : null;
        }

        // ---------------------------------------------------------------- matériaux

        sealed class Materials
        {
            public readonly Material Wrap = Lit("Katana tsuka", new Color(0.05f, 0.04f, 0.05f), 0f, 0.3f);
            public readonly Material Brass = Lit("Katana laiton", new Color(0.62f, 0.42f, 0.12f), 0.9f, 0.6f);
            public readonly Material Edge = Blade("Katana lame neon", new Color(0.25f, 2.2f, 3f));
            public readonly Material Steel = Blade("Katana lame acier", new Color(0.06f, 0.55f, 0.8f));
            public readonly Material Tear = Transparent("Dechirure", k_TearShader);
            public readonly Material Glove = LoadOrCreate("Gant matrice", AssetDatabase.LoadAssetAtPath<Shader>(k_GloveShader));
            public readonly Material Trail = TrailMaterial();
            public readonly Material Digits = Transparent("Chiffres matrice", k_DigitsShader);
        }

        static Material Lit(string name, Color color, float metallic, float smoothness)
        {
            var material = LoadOrCreate(name, Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Blade(string name, Color glow)
        {
            var material = LoadOrCreate(name, AssetDatabase.LoadAssetAtPath<Shader>(k_BladeShader));
            material.SetColor("_GlowColor", glow);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Transparent(string name, string shaderPath)
        {
            var material = LoadOrCreate(name, AssetDatabase.LoadAssetAtPath<Shader>(shaderPath));
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        // La traînée de la lame : la lueur ajoutée à l'image, blanche (la couleur vient de ses sommets), assez vive pour le
        // bloom. Réglée seulement à sa création.
        static Material TrailMaterial()
        {
            var path = $"{k_Folder}/Trainee lame.mat";
            var created = AssetDatabase.LoadAssetAtPath<Material>(path) == null;
            var material = Transparent("Trainee lame", k_GlowShader);
            if (created)
            {
                material.SetColor("_Color", Color.white);
                material.SetFloat("_Intensity", 2.2f);
                material.SetFloat("_Softness", 0.3f);
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        static Material LoadOrCreate(string name, Shader shader)
        {
            var path = $"{k_Folder}/{name}.mat";
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

        // ---------------------------------------------------------------- le katana

        // Le katana de Pichuliru : le milieu de la poignée à l'origine, la lame vers +Y, le tranchant vers +Z (la lame se
        // courbe vers son dos), la pointe à 85 cm ; « Garde » au début de la lame, « Pointe » au bout. La lame (acier et fil)
        // prend le shader néon, repéré dans l'espace de son maillage.
        static GameObject BuildKatana(GameObject model, Materials materials)
        {
            var root = new GameObject("Katana");
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                instance.name = "Modele";
                instance.transform.localPosition = Vector3.zero;
                var filter = instance.GetComponentInChildren<MeshFilter>();
                var renderer = filter != null ? filter.GetComponent<MeshRenderer>() : null;
                var mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null || renderer == null)
                {
                    Debug.LogWarning("Katana : le modèle n'a pas de mesh.", model);
                    return null;
                }

                var source = renderer.sharedMaterials;
                var handle = IndexOf(source, "black");
                var steel = IndexOf(source, "grey");
                var edge = IndexOf(source, "white");
                var brass = IndexOf(source, "brass");
                var toRoot = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var handlePoints = Points(mesh, handle, toRoot);
                var bladePoints = Points(mesh, steel, toRoot);
                bladePoints.AddRange(Points(mesh, edge, toRoot));
                if (handlePoints.Count == 0 || bladePoints.Count == 0)
                {
                    Debug.LogWarning("Katana : poignée ou lame introuvable dans le modèle (matériaux Black, Grey, White).", model);
                    return null;
                }

                var handleCenter = Average(handlePoints);
                var tip = bladePoints.OrderByDescending(p => (p - handleCenter).sqrMagnitude).First();
                var axis = (tip - handleCenter).normalized;
                var tipT = Vector3.Dot(tip - handleCenter, axis);

                // Le milieu de la lame bombe du côté du tranchant (un katana a le tranchant sur le côté convexe de sa courbe).
                // 24 octobre : le baker le croyait du côté du dos, et le katana était à l'envers en main (le tranchant vers
                // l'arrière) ; il est retourné de 180° autour de sa lame.
                var bulge = Vector3.zero;
                foreach (var point in bladePoints)
                {
                    var t = Vector3.Dot(point - handleCenter, axis) / tipT;
                    if (t > 0.35f && t < 0.65f)
                        bulge += Vector3.ProjectOnPlane(point - handleCenter, axis);
                }

                var edgeDirection = bulge.sqrMagnitude > 1e-10f ? bulge.normalized : Vector3.Cross(axis, Vector3.right).normalized;
                var inverse = Quaternion.Inverse(Quaternion.LookRotation(edgeDirection, axis));
                var bladeStartT = bladePoints.Min(p => Vector3.Dot(p - handleCenter, axis));
                var scale = k_TipDistance / tipT;
                instance.transform.localRotation = inverse * instance.transform.localRotation;
                instance.transform.localScale *= scale;
                instance.transform.localPosition = -(inverse * handleCenter) * scale;

                AddMarker(root.transform, "Garde", new Vector3(0f, bladeStartT * scale, 0f));
                AddMarker(root.transform, "Pointe", new Vector3(0f, k_TipDistance, 0f));
                AddMarker(root.transform, k_EdgeMarker, new Vector3(0f, k_TipDistance * 0.5f, 0.03f));

                // La lame est repérée dans l'espace de son maillage, pour le néon qui la remonte.
                var toMesh = toRoot.inverse;
                var axisMesh = toMesh.MultiplyVector(axis).normalized;
                var range = new Vector2(float.MaxValue, float.MinValue);
                foreach (var vertex in BladeVertices(mesh, steel, edge))
                {
                    var along = Vector3.Dot(vertex, axisMesh);
                    range = new Vector2(Mathf.Min(range.x, along), Mathf.Max(range.y, along));
                }

                foreach (var blade in new[] { materials.Edge, materials.Steel })
                {
                    blade.SetVector("_Axis", axisMesh);
                    blade.SetVector("_Range", range);
                    EditorUtility.SetDirty(blade);
                }

                var replaced = new Material[source.Length];
                for (var i = 0; i < source.Length; i++)
                {
                    replaced[i] = i == handle ? materials.Wrap
                        : i == steel ? materials.Steel
                        : i == edge ? materials.Edge
                        : i == brass ? materials.Brass
                        : source[i];
                }

                renderer.sharedMaterials = replaced;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return PrefabUtility.SaveAsPrefabAsset(root, $"{k_Folder}/Katana.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static IEnumerable<Vector3> BladeVertices(Mesh mesh, int steel, int edge)
        {
            var vertices = mesh.vertices;
            foreach (var sub in new[] { steel, edge })
            {
                if (sub < 0 || sub >= mesh.subMeshCount)
                    continue;
                foreach (var index in new HashSet<int>(mesh.GetTriangles(sub)))
                    yield return vertices[index];
            }
        }

        // ---------------------------------------------------------------- outils

        static int IndexOf(Material[] materials, string key)
        {
            for (var i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && materials[i].name.ToLowerInvariant().Contains(key))
                    return i;
            }

            return -1;
        }

        static List<Vector3> Points(Mesh mesh, int subMesh, Matrix4x4 toRoot)
        {
            var points = new List<Vector3>();
            if (subMesh < 0 || subMesh >= mesh.subMeshCount)
                return points;
            var vertices = mesh.vertices;
            foreach (var index in new HashSet<int>(mesh.GetTriangles(subMesh)))
                points.Add(toRoot.MultiplyPoint3x4(vertices[index]));
            return points;
        }

        static Vector3 Average(List<Vector3> points)
        {
            var sum = Vector3.zero;
            foreach (var point in points)
                sum += point;
            return points.Count > 0 ? sum / points.Count : Vector3.zero;
        }

        static void AddMarker(Transform parent, string name, Vector3 position)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.localPosition = position;
        }

        // L'aperçu du menu des armes : le katana debout, un peu penché.
        static BowVisual BuildVisual(GameObject katana)
        {
            var root = new GameObject("Bow Visual Katana");
            try
            {
                root.AddComponent<BowVisual>();
                var blade = (GameObject)PrefabUtility.InstantiatePrefab(katana, root.transform);
                blade.transform.localPosition = new Vector3(0f, -0.38f, 0f);
                blade.transform.localRotation = Quaternion.Euler(0f, 90f, -12f);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, k_Visual);
                return prefab != null ? prefab.GetComponent<BowVisual>() : null;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------------------------------------------------------------- liste des armes

        static bool InLoadout(BowDefinition definition)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_Loadout);
            var loadout = prefab != null ? prefab.GetComponentInChildren<BowLoadout>(true) : null;
            return loadout != null && loadout.Entries.Any(entry => entry != null && entry.bow == definition);
        }

        // La carte du katana à la fin de la liste des armes, et son succès dans la liste des succès (fichier du prefab de
        // l'équipe : une ligne de plus dans chaque liste, rien d'autre).
        static void AddToLoadout(BowDefinition definition, AchievementDefinition achievement)
        {
            var root = PrefabUtility.LoadPrefabContents(k_Loadout);
            try
            {
                var changed = false;
                var loadout = root.GetComponentInChildren<BowLoadout>(true);
                if (loadout != null && !loadout.Entries.Any(entry => entry != null && entry.bow == definition))
                {
                    var settings = new SerializedObject(loadout);
                    var entries = settings.FindProperty("m_Entries");
                    entries.arraySize++;
                    var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                    entry.FindPropertyRelative("bow").objectReferenceValue = definition;
                    entry.FindPropertyRelative("unlockedBy").objectReferenceValue = achievement;
                    entry.FindPropertyRelative("color").colorValue = k_CardColor;
                    entry.FindPropertyRelative("comingSoonName").stringValue = "";
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                var manager = root.GetComponentInChildren<AchievementManager>(true);
                if (manager != null)
                {
                    var settings = new SerializedObject(manager);
                    var list = settings.FindProperty("m_Achievements");
                    var present = false;
                    for (var i = 0; list != null && i < list.arraySize; i++)
                        present |= list.GetArrayElementAtIndex(i).objectReferenceValue == achievement;
                    if (list != null && !present)
                    {
                        list.arraySize++;
                        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = achievement;
                        settings.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, k_Loadout);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
