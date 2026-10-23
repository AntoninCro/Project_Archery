using System.Collections.Generic;
using System.Linq;
using Archery.BowClasses;
using Archery.Bows;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le katana et le gantelet dans le jeu (refaits le 22 octobre ; avant, un katana et un bouclier faits ici ; le 23
    /// octobre, le symbole à tracer est retiré) : le katana de Pichuliru (CC0) et l'Energy Gauntlet d'ENOMIC (CC-BY), remis
    /// dans le repère attendu par le jeu, avec leurs matériaux (<c>Art/Katana</c> : lame néon qui bugue, gantelet chrome aux
    /// lueurs cyan) et celui des failles, leur fiche (<c>Data/BowClasses/Bow_Katana</c>), le succès qui les débloque (« Rōnin » : vaincre
    /// 2 boss de vague, <c>Data/Achievements/Succes_Ronin</c>), l'aperçu du menu des armes (<c>Prefabs/Bow Visual Katana</c>)
    /// et leur carte dans la liste des armes (<c>Prefabs/UI/Bow Classes</c>, avec le succès dans la liste des succès).
    /// </summary>
    /// <remarks>
    /// Se fait tout seul (une fois, et de nouveau tant que la fiche n'a pas de gantelet). La fiche et le succès ne sont écrits
    /// qu'à leur création : ensuite, ce sont eux qui font foi ; seuls les textes et les sons laissés à leur valeur d'origine
    /// (le bouclier) sont mis à jour. Menu <c>Archery > Katana et gantelet > Refaire les modèles et la carte</c>.
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
        const string k_KatanaModel = "Assets/Katana by Pichuliru - 15A7InejC7/Flat Melee Bladed/Katana.fbx";
        const string k_GauntletModel = "Assets/energy-gauntlet-low-poly-free-download/source/HAND FBX LOW Poly.fbx";
        const string k_HitClip = "Assets/kenney_rpg-audio/Audio/chop.ogg";
        const string k_LaunchClip = "Assets/650564__h2p34__lightsaber-whoosh.wav";
        const string k_SwooshClip = "Assets/507466__danjocross__clean-fast-swoosh.aiff";
        const string k_CrackClip = "Assets/kenney_impact-sounds/Audio/impactGlass_medium_001.ogg";
        const string k_SignClip = "Assets/kenney_interfaceSounds/Audio/glitch_004.ogg";

        // Le katana : de la poignée (son milieu) à la pointe ; le gantelet : du poignet au bout du majeur.
        const float k_TipDistance = 0.85f;
        const float k_GauntletLength = 0.19f;
        static readonly Vector3 k_Palm = new Vector3(0f, -0.022f, 0.075f);

        static readonly Color k_CardColor = new Color(0.95f, 0.35f, 0.4f, 1f);

        // Les textes de la fiche du katana et du bouclier, remplacés s'ils n'ont pas été changés à la main.
        const string k_OldName = "Katana et bouclier";
        const string k_OldDescription = "Le katana déchire la matrice ; le bouclier renvoie la déchirure.";
        const string k_OldPower = "Frappe la déchirure du katana avec le bouclier pour la lancer ; le bouclier arrête coups et projectiles";
        const string k_Name = "Katana et gantelet";

        // Les textes du katana aux symboles (22 octobre), remplacés eux aussi s'ils n'ont pas été changés à la main.
        const string k_SignDescription = "Le katana fend la matrice ; le gantelet envoie le signe.";
        const string k_SignPower = "Trace le symbole au katana, puis touche le signe avec le gantelet : il file vers l'ennemi que tu regardes";
        const string k_Description = "Le katana fend la matrice ; le gantelet lance la faille.";
        const string k_Power = "Une taille rapide ouvre une faille ; frappe-la vite avec le gantelet : elle file vers l'ennemi que tu regardes";

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
            if (AssetDatabase.LoadAssetAtPath<Shader>(k_TearShader) == null || AssetDatabase.LoadAssetAtPath<GameObject>(k_Loadout) == null)
                return;
            var definition = AssetDatabase.LoadAssetAtPath<BowDefinition>(k_Definition);
            if (definition != null && definition.katanaModel != null && definition.gauntletModel != null && definition.katanaTearMaterial != null &&
                definition.visualPrefab != null && InLoadout(definition) && definition.specialPower != k_SignPower)
                return;
            Bake();
        }

        [MenuItem("Archery/Katana et gantelet/Refaire les modèles et la carte")]
        static void Bake()
        {
            if (!AssetDatabase.IsValidFolder(k_Folder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Katana");

            var katanaModel = FindModel(k_KatanaModel, "Katana", "Pichuliru");
            var gauntletModel = FindModel(k_GauntletModel, "HAND FBX LOW Poly", "gauntlet");
            if (katanaModel == null || gauntletModel == null)
            {
                Debug.LogWarning("Katana et gantelet : il manque le modèle du katana (Katana by Pichuliru) ou du gantelet " +
                                 "(energy-gauntlet-low-poly-free-download) dans Assets.");
                return;
            }

            var materials = new Materials();
            var katana = BuildKatana(katanaModel, materials);
            var gauntlet = BuildGauntlet(gauntletModel, materials);
            if (katana == null || gauntlet == null)
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
            definition.gauntletModel = gauntlet;
            if (definition.katanaTearMaterial == null)
                definition.katanaTearMaterial = materials.Tear;
            Migrate(definition);
            definition.visualPrefab = BuildVisual(katana, gauntlet);
            EditorUtility.SetDirty(definition);

            AddToLoadout(definition, achievement);
            AssetDatabase.SaveAssets();
            Debug.Log($"Katana et gantelet : modèles dans {k_Folder}, fiche {k_Definition}, carte dans {k_Loadout} (débloquée par « Rōnin »). " +
                      "Raccourci de test : O les équipe (même verrouillés), O encore rend l'arme d'avant.");
        }

        // Les réglages de départ. Le signe lancé fait les dégâts d'une flèche d'arc puissant, le coup de katana trois fois
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

        // La fiche du katana et du bouclier devient celle du katana et du gantelet : les textes d'origine changent, les sons
        // manquants arrivent (le lancer prend le sabre laser au lieu du bug). Les valeurs de jeu restent.
        static void Migrate(BowDefinition definition)
        {
            if (definition.displayName == k_OldName)
                definition.displayName = k_Name;
            if (definition.description == k_OldDescription || definition.description == k_SignDescription)
                definition.description = k_Description;
            if (definition.specialPower == k_OldPower || definition.specialPower == k_SignPower)
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
            public readonly Material Plates = Lit("Gantelet plaques", new Color(0.86f, 0.89f, 0.92f), 0.8f, 0.78f);
            public readonly Material Dark = Lit("Gantelet sombre", new Color(0.08f, 0.1f, 0.12f), 0.75f, 0.55f);
            public readonly Material Wires = Lit("Gantelet fils", new Color(0.14f, 0.15f, 0.16f), 0.4f, 0.4f);
            public readonly Material Glove = Lit("Gantelet gant", new Color(0.03f, 0.03f, 0.035f), 0f, 0.35f);
            public readonly Material Glow = Glowing("Gantelet lueur", new Color(0.3f, 1.8f, 2.4f));
            public readonly Material Tear = TearMaterial();
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

        static Material Glowing(string name, Color emission)
        {
            var material = Lit(name, new Color(0.1f, 0.6f, 0.8f), 0f, 0.6f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
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

        static Material TearMaterial()
        {
            var material = LoadOrCreate("Dechirure", AssetDatabase.LoadAssetAtPath<Shader>(k_TearShader));
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
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

                // La lame se courbe vers son dos : le tranchant est de l'autre côté.
                var spine = Vector3.zero;
                foreach (var point in bladePoints)
                {
                    var t = Vector3.Dot(point - handleCenter, axis) / tipT;
                    if (t > 0.35f && t < 0.65f)
                        spine += Vector3.ProjectOnPlane(point - handleCenter, axis);
                }

                var edgeDirection = spine.sqrMagnitude > 1e-10f ? -spine.normalized : Vector3.Cross(axis, Vector3.right).normalized;
                var inverse = Quaternion.Inverse(Quaternion.LookRotation(edgeDirection, axis));
                var bladeStartT = bladePoints.Min(p => Vector3.Dot(p - handleCenter, axis));
                var scale = k_TipDistance / tipT;
                instance.transform.localRotation = inverse * instance.transform.localRotation;
                instance.transform.localScale *= scale;
                instance.transform.localPosition = -(inverse * handleCenter) * scale;

                AddMarker(root.transform, "Garde", new Vector3(0f, bladeStartT * scale, 0f));
                AddMarker(root.transform, "Pointe", new Vector3(0f, k_TipDistance, 0f));

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

        // ---------------------------------------------------------------- le gantelet

        // L'Energy Gauntlet : le poignet à l'origine, les doigts vers +Z, le dos de la main vers +Y, une main gauche (pouce
        // vers +X ; mis en miroir s'il le faut), 19 cm du poignet au bout du majeur ; « Paume » au creux de la main.
        static GameObject BuildGauntlet(GameObject model, Materials materials)
        {
            var root = new GameObject("Gantelet");
            try
            {
                var holder = new GameObject("Miroir").transform;
                holder.SetParent(root.transform, false);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, holder);
                instance.name = "Modele";
                instance.transform.localPosition = Vector3.zero;

                var hand = PartPoints(instance, root.transform, "hand_basemesh6", null);
                var thumb = PartPoints(instance, root.transform, "polySurface25", null);
                var backPlate = PartPoints(instance, root.transform, "polySurface1", null);
                var handPlate = PartPoints(instance, root.transform, "polySurface30", null);
                var fingers = PartPoints(instance, root.transform, null, "fingerplate");
                if (hand.Count == 0 || thumb.Count == 0 || backPlate.Count == 0 || handPlate.Count == 0 || fingers.Count == 0)
                {
                    Debug.LogWarning("Gantelet : pièces introuvables dans le modèle (main, pouce, plaques, doigts).", model);
                    return null;
                }

                var handCenter = Average(hand);
                var forward = (Average(fingers) - Average(handPlate)).normalized;
                var up = Vector3.ProjectOnPlane(Average(backPlate) - handCenter, forward).normalized;
                var right = Vector3.Cross(up, forward);
                var wristT = handPlate.Min(p => Vector3.Dot(p - handCenter, forward));
                var tipT = hand.Max(p => Vector3.Dot(p - handCenter, forward));
                var wrist = handCenter + forward * wristT;
                var scale = k_GauntletLength / Mathf.Max(1e-5f, tipT - wristT);
                var inverse = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
                instance.transform.localRotation = inverse * instance.transform.localRotation;
                instance.transform.localScale *= scale;
                instance.transform.localPosition = -(inverse * wrist) * scale;

                // Le jeu attend une main gauche (pouce vers +X) : sinon, en miroir.
                if (Vector3.Dot(Average(thumb) - handCenter, right) < 0f)
                    holder.localScale = new Vector3(-1f, 1f, 1f);

                AddMarker(root.transform, "Paume", k_Palm);

                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    var source = renderer.sharedMaterials;
                    var replaced = new Material[source.Length];
                    for (var i = 0; i < source.Length; i++)
                        replaced[i] = GauntletMaterial(source[i], materials);
                    renderer.sharedMaterials = replaced;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                }

                return PrefabUtility.SaveAsPrefabAsset(root, $"{k_Folder}/Gantelet.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // Les matériaux du modèle, par leur nom : plaques chromées, tubes et attaches sombres, fils, lueurs cyan, gant noir.
        static Material GauntletMaterial(Material source, Materials materials)
        {
            var name = source != null ? source.name.ToLowerInvariant() : "";
            if (name.Contains("glow"))
                return materials.Glow;
            if (name.Contains("glove"))
                return materials.Glove;
            if (name.Contains("wire"))
                return materials.Wires;
            if (name.Contains("plate"))
                return materials.Plates;
            return materials.Dark;
        }

        // Les sommets d'une pièce (par son nom, ou par le nom de son matériau), dans le repère de la racine.
        static List<Vector3> PartPoints(GameObject instance, Transform root, string partName, string materialName)
        {
            var points = new List<Vector3>();
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;
                var renderer = filter.GetComponent<MeshRenderer>();
                var matches = partName != null
                    ? filter.name == partName
                    : renderer != null && renderer.sharedMaterials.Any(m => m != null && m.name.ToLowerInvariant().Contains(materialName));
                if (!matches)
                    continue;
                var toRoot = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                foreach (var vertex in mesh.vertices)
                    points.Add(toRoot.MultiplyPoint3x4(vertex));
            }

            return points;
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

        // L'aperçu du menu des armes : le katana debout, un peu penché, et le gantelet à côté, doigts en haut, dos de la main
        // face à la carte.
        static BowVisual BuildVisual(GameObject katana, GameObject gauntlet)
        {
            var root = new GameObject("Bow Visual Katana");
            try
            {
                root.AddComponent<BowVisual>();
                var blade = (GameObject)PrefabUtility.InstantiatePrefab(katana, root.transform);
                blade.transform.localPosition = new Vector3(-0.08f, -0.38f, 0f);
                blade.transform.localRotation = Quaternion.Euler(0f, 90f, -12f);
                var glove = (GameObject)PrefabUtility.InstantiatePrefab(gauntlet, root.transform);
                glove.transform.localPosition = new Vector3(0.14f, -0.2f, -0.04f);
                glove.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
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
