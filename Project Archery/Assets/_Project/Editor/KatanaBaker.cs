using System.Collections.Generic;
using System.Linq;
using Archery.BowClasses;
using Archery.Bows;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le katana et le bouclier dans le jeu : leurs modèles (low-poly à facettes, faits ici : un katana à la lame courbe et
    /// au fil néon, un bouclier rond cerclé, à l'umbo et à l'anneau néon), leurs matériaux et celui de la déchirure
    /// (<c>Art/Katana</c>), leur fiche (<c>Data/BowClasses/Bow_Katana</c>), le succès qui les débloque (« Rōnin » : vaincre
    /// 2 boss de vague, <c>Data/Achievements/Succes_Ronin</c>), l'aperçu du menu des armes (<c>Prefabs/Bow Visual Katana</c>)
    /// et leur carte dans la liste des armes (<c>Prefabs/UI/Bow Classes</c>, avec le succès dans la liste des succès).
    /// </summary>
    /// <remarks>
    /// Se fait tout seul une fois. La fiche et le succès ne sont écrits qu'à leur création : ensuite, ce sont eux qui font
    /// foi. Menu <c>Archery > Katana et bouclier > Refaire les modèles et la carte</c> : refait les modèles, les matériaux,
    /// l'aperçu, et remet la carte si elle manque.
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
        const string k_HitClip = "Assets/kenney_rpg-audio/Audio/chop.ogg";
        const string k_LaunchClip = "Assets/kenney_interfaceSounds/Audio/glitch_004.ogg";
        const string k_BlockClip = "Assets/kenney_impact-sounds/Audio/impactMetal_heavy_000.ogg";

        static readonly Color k_CardColor = new Color(0.95f, 0.35f, 0.4f, 1f);

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
            if (definition != null && definition.katanaModel != null && definition.shieldModel != null && definition.visualPrefab != null &&
                InLoadout(definition))
                return;
            Bake();
        }

        [MenuItem("Archery/Katana et bouclier/Refaire les modèles et la carte")]
        static void Bake()
        {
            if (!AssetDatabase.IsValidFolder(k_Folder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Katana");

            var materials = new Materials();
            var katana = SavePrefab("Katana", BuildKatanaMesh(), materials.Wrap, materials.Metal, materials.Blade, materials.Neon);
            var shield = SavePrefab("Bouclier", BuildShieldMesh(), materials.ShieldFace, materials.Metal, materials.Neon, materials.Leather);

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
            definition.shieldModel = shield;
            if (definition.katanaTearMaterial == null)
                definition.katanaTearMaterial = materials.Tear;
            definition.visualPrefab = BuildVisual(katana, shield);
            EditorUtility.SetDirty(definition);

            AddToLoadout(definition, achievement);
            AssetDatabase.SaveAssets();
            Debug.Log($"Katana et bouclier : modèles dans {k_Folder}, fiche {k_Definition}, carte dans {k_Loadout} (débloquée par « Rōnin »). " +
                      "Raccourci de test : O les équipe (même verrouillés), O encore rend l'arme d'avant.");
        }

        // Les réglages de départ. La déchirure lancée fait les dégâts d'une flèche d'arc puissant, le coup de katana trois
        // fois ceux de l'arc de chasse.
        static void Fill(BowDefinition definition)
        {
            definition.displayName = "Katana et bouclier";
            definition.description = "Le katana déchire la matrice ; le bouclier renvoie la déchirure.";
            definition.specialPower = "Frappe la déchirure du katana avec le bouclier pour la lancer ; le bouclier arrête coups et projectiles";
            definition.damage = 15f;
            definition.arrowSpeed = 34f;
            definition.arrowGravity = 1f;
            definition.ringDuration = 1f;
            definition.katanaDamage = 30f;
            definition.katanaHitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(k_HitClip);
            definition.katanaLaunchClip = AssetDatabase.LoadAssetAtPath<AudioClip>(k_LaunchClip);
            definition.shieldBlockClip = AssetDatabase.LoadAssetAtPath<AudioClip>(k_BlockClip);
        }

        // ---------------------------------------------------------------- matériaux

        sealed class Materials
        {
            public readonly Material Wrap = Lit("Katana tsuka", new Color(0.07f, 0.05f, 0.07f), 0f, 0.25f);
            public readonly Material Metal = Lit("Katana metal", new Color(0.3f, 0.27f, 0.24f), 0.85f, 0.55f);
            public readonly Material Blade = Lit("Katana lame", new Color(0.78f, 0.8f, 0.85f), 0.9f, 0.82f);
            public readonly Material Neon = Glow("Katana neon", new Color(0.3f, 1.6f, 2.4f));
            public readonly Material ShieldFace = Lit("Bouclier face", new Color(0.11f, 0.13f, 0.19f), 0.5f, 0.45f);
            public readonly Material Leather = Lit("Bouclier cuir", new Color(0.24f, 0.14f, 0.08f), 0f, 0.3f);
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

        static Material Glow(string name, Color emission)
        {
            var material = Lit(name, Color.black, 0f, 0.5f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
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

        // ---------------------------------------------------------------- modèles

        // Un maillage à facettes (une normale par face), en sous-maillages. Chaque face est tournée vers l'extérieur d'après
        // un point « dedans » donné avec elle.
        sealed class MeshBuilder
        {
            readonly List<Vector3> m_Vertices = new List<Vector3>();
            readonly List<Vector3> m_Normals = new List<Vector3>();
            readonly List<int>[] m_Triangles;

            public MeshBuilder(int submeshes)
            {
                m_Triangles = new List<int>[submeshes];
                for (var i = 0; i < submeshes; i++)
                    m_Triangles[i] = new List<int>();
            }

            public void Triangle(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 inside)
            {
                var normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-14f)
                    return;
                if (Vector3.Dot(normal, (a + b + c) / 3f - inside) < 0f)
                {
                    (b, c) = (c, b);
                    normal = -normal;
                }

                normal.Normalize();
                var start = m_Vertices.Count;
                m_Vertices.Add(a);
                m_Vertices.Add(b);
                m_Vertices.Add(c);
                m_Normals.Add(normal);
                m_Normals.Add(normal);
                m_Normals.Add(normal);
                m_Triangles[sub].Add(start);
                m_Triangles[sub].Add(start + 1);
                m_Triangles[sub].Add(start + 2);
            }

            public void Quad(int sub, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 inside)
            {
                Triangle(sub, a, b, c, inside);
                Triangle(sub, a, c, d, inside);
            }

            public void Box(int sub, Vector3 center, Vector3 size)
            {
                var h = size * 0.5f;
                var p = new Vector3[8];
                for (var i = 0; i < 8; i++)
                    p[i] = center + new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
                Quad(sub, p[0], p[1], p[3], p[2], center);
                Quad(sub, p[4], p[5], p[7], p[6], center);
                Quad(sub, p[0], p[1], p[5], p[4], center);
                Quad(sub, p[2], p[3], p[7], p[6], center);
                Quad(sub, p[0], p[2], p[6], p[4], center);
                Quad(sub, p[1], p[3], p[7], p[5], center);
            }

            // Un prisme vertical (de y0 à y1), de section elliptique à « sides » côtés, fermé aux deux bouts.
            public void Prism(int sub, float y0, float y1, float rx, float rz, int sides, float turn = 0f)
            {
                var middle = new Vector3(0f, (y0 + y1) * 0.5f, 0f);
                for (var i = 0; i < sides; i++)
                {
                    var a0 = (i + turn) / sides * Mathf.PI * 2f;
                    var a1 = (i + 1 + turn) / sides * Mathf.PI * 2f;
                    var p0 = new Vector3(Mathf.Cos(a0) * rx, 0f, Mathf.Sin(a0) * rz);
                    var p1 = new Vector3(Mathf.Cos(a1) * rx, 0f, Mathf.Sin(a1) * rz);
                    Quad(sub, p0 + Vector3.up * y0, p1 + Vector3.up * y0, p1 + Vector3.up * y1, p0 + Vector3.up * y1, middle);
                    Triangle(sub, Vector3.up * y0, p0 + Vector3.up * y0, p1 + Vector3.up * y0, middle);
                    Triangle(sub, Vector3.up * y1, p0 + Vector3.up * y1, p1 + Vector3.up * y1, middle);
                }
            }

            public Mesh Build(string name)
            {
                var mesh = new Mesh { name = name };
                mesh.SetVertices(m_Vertices);
                mesh.SetNormals(m_Normals);
                mesh.subMeshCount = m_Triangles.Length;
                for (var i = 0; i < m_Triangles.Length; i++)
                    mesh.SetTriangles(m_Triangles[i], i);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        // Le katana (sous-maillages : poignée, métal, lame, fil néon) : la poignée centrée sur l'origine, la lame vers +Y,
        // courbée vers le dos (-Z), le tranchant vers +Z, un fil néon le long du tranchant, des deux côtés.
        static Mesh BuildKatanaMesh()
        {
            var builder = new MeshBuilder(4);
            builder.Prism(0, -0.118f, 0.115f, 0.0145f, 0.0175f, 8, 0.5f);
            builder.Prism(1, -0.13f, -0.118f, 0.0165f, 0.0195f, 8, 0.5f);
            builder.Prism(1, 0.115f, 0.124f, 0.04f, 0.046f, 12);
            builder.Box(1, new Vector3(0f, 0.137f, 0f), new Vector3(0.011f, 0.026f, 0.034f));

            const int stations = 20;
            const float start = 0.13f;
            const float length = 0.74f;
            var sections = new Vector3[stations + 1][];
            var centers = new Vector3[stations + 1];
            for (var i = 0; i <= stations; i++)
            {
                var s = i / (float)stations;
                var y = start + length * s;
                var curve = -0.035f * s * s;
                var taper = s > 0.93f ? Mathf.InverseLerp(1f, 0.93f, s) : 1f;
                var width = Mathf.Lerp(0.031f, 0.025f, s) * taper;
                var thickness = Mathf.Lerp(0.0068f, 0.0045f, s) * taper;
                var center = new Vector3(0f, y, curve - (1f - taper) * 0.008f);
                centers[i] = center;
                sections[i] = new[]
                {
                    center + new Vector3(-thickness * 0.5f, 0f, -width * 0.5f),
                    center + new Vector3(thickness * 0.5f, 0f, -width * 0.5f),
                    center + new Vector3(thickness * 0.45f, 0f, width * 0.15f),
                    center + new Vector3(0f, 0f, width * 0.5f),
                    center + new Vector3(-thickness * 0.45f, 0f, width * 0.15f),
                };
            }

            for (var i = 0; i < stations; i++)
            {
                var inside = (centers[i] + centers[i + 1]) * 0.5f;
                for (var k = 0; k < 5; k++)
                {
                    var next = (k + 1) % 5;
                    builder.Quad(2, sections[i][k], sections[i][next], sections[i + 1][next], sections[i + 1][k], inside);
                }

                // Le fil néon, entre l'arête et le tranchant, sur les deux faces (jusqu'à la pointe, sans elle).
                if (i >= stations - 1)
                    continue;
                foreach (var ridge in new[] { 2, 4 })
                {
                    var outward = ridge == 2 ? Vector3.right : Vector3.left;
                    var a0 = Vector3.Lerp(sections[i][ridge], sections[i][3], 0.55f) + outward * 0.0004f;
                    var b0 = Vector3.Lerp(sections[i][ridge], sections[i][3], 0.85f) + outward * 0.0004f;
                    var a1 = Vector3.Lerp(sections[i + 1][ridge], sections[i + 1][3], 0.55f) + outward * 0.0004f;
                    var b1 = Vector3.Lerp(sections[i + 1][ridge], sections[i + 1][3], 0.85f) + outward * 0.0004f;
                    builder.Quad(3, a0, b0, b1, a1, inside);
                }
            }

            return builder.Build("Katana");
        }

        // Le bouclier (sous-maillages : face, métal, anneau néon, poignée) : la poignée au dos, centrée sur l'origine,
        // la face bombée vers +Z, cerclée de métal, un umbo au centre, un anneau néon sur la face.
        static Mesh BuildShieldMesh()
        {
            var builder = new MeshBuilder(4);
            const int sides = 20;
            var front = new Vector3(0f, 0f, 0.078f);
            var behind = new Vector3(0f, 0f, -0.5f);
            var ahead = new Vector3(0f, 0f, 0.5f);
            for (var i = 0; i < sides; i++)
            {
                var a0 = i / (float)sides * Mathf.PI * 2f;
                var a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 Ring(float angle, float radius, float z) => new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z);

                // La face, bombée : du centre à un premier cercle, puis jusqu'au bord ; le dos, plat.
                builder.Triangle(0, front, Ring(a0, 0.1f, 0.07f), Ring(a1, 0.1f, 0.07f), behind);
                builder.Quad(0, Ring(a0, 0.1f, 0.07f), Ring(a1, 0.1f, 0.07f), Ring(a1, 0.185f, 0.055f), Ring(a0, 0.185f, 0.055f), behind);
                builder.Triangle(0, new Vector3(0f, 0f, 0.035f), Ring(a0, 0.185f, 0.035f), Ring(a1, 0.185f, 0.035f), ahead);

                // Le cerclage : un anneau de section carrée autour du bord.
                var rimCenter = Ring((a0 + a1) * 0.5f, 0.194f, 0.046f);
                builder.Quad(1, Ring(a0, 0.205f, 0.03f), Ring(a1, 0.205f, 0.03f), Ring(a1, 0.205f, 0.062f), Ring(a0, 0.205f, 0.062f), rimCenter);
                builder.Quad(1, Ring(a0, 0.183f, 0.062f), Ring(a1, 0.183f, 0.062f), Ring(a1, 0.205f, 0.062f), Ring(a0, 0.205f, 0.062f), rimCenter);
                builder.Quad(1, Ring(a0, 0.183f, 0.03f), Ring(a1, 0.183f, 0.03f), Ring(a1, 0.205f, 0.03f), Ring(a0, 0.205f, 0.03f), rimCenter);

                // L'umbo : un dôme bas à facettes.
                var axis = new Vector3(0f, 0f, 0.075f);
                builder.Quad(1, Ring(a0, 0.055f, 0.074f), Ring(a1, 0.055f, 0.074f), Ring(a1, 0.035f, 0.1f), Ring(a0, 0.035f, 0.1f), axis);
                builder.Triangle(1, new Vector3(0f, 0f, 0.112f), Ring(a0, 0.035f, 0.1f), Ring(a1, 0.035f, 0.1f), axis);

                // L'anneau néon, posé sur la face.
                builder.Quad(2, Ring(a0, 0.125f, 0.0685f), Ring(a1, 0.125f, 0.0685f), Ring(a1, 0.137f, 0.0665f), Ring(a0, 0.137f, 0.0665f), behind);
            }

            // La poignée au dos (verticale), et ses deux attaches.
            builder.Box(3, Vector3.zero, new Vector3(0.024f, 0.12f, 0.024f));
            builder.Box(1, new Vector3(0f, 0.055f, 0.0235f), new Vector3(0.026f, 0.018f, 0.023f));
            builder.Box(1, new Vector3(0f, -0.055f, 0.0235f), new Vector3(0.026f, 0.018f, 0.023f));
            return builder.Build("Bouclier");
        }

        static GameObject SavePrefab(string name, Mesh mesh, params Material[] materials)
        {
            var meshPath = $"{k_Folder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                mesh = existing;
                EditorUtility.SetDirty(mesh);
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            var go = new GameObject(name);
            try
            {
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                return PrefabUtility.SaveAsPrefabAsset(go, $"{k_Folder}/{name}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // L'aperçu du menu des armes : le katana debout, un peu penché, et le bouclier à côté, face à la carte.
        static BowVisual BuildVisual(GameObject katana, GameObject shield)
        {
            var root = new GameObject("Bow Visual Katana");
            try
            {
                root.AddComponent<BowVisual>();
                var blade = (GameObject)PrefabUtility.InstantiatePrefab(katana, root.transform);
                blade.transform.localPosition = new Vector3(-0.06f, -0.38f, 0f);
                blade.transform.localRotation = Quaternion.Euler(0f, 90f, -12f);
                var guard = (GameObject)PrefabUtility.InstantiatePrefab(shield, root.transform);
                guard.transform.localPosition = new Vector3(0.14f, -0.1f, -0.06f);
                guard.transform.localRotation = Quaternion.Euler(0f, 180f, 8f);
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
