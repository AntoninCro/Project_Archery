using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Archery.Building;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Archery.EditorTools
{
    /// <summary>
    /// L'allure des tours (30 octobre, troisième retour du joueur sur les 10 tours ; <c>Docs/Refonte_TowerDefense.md</c>, partie
    /// 12.11) : chaque tour a sa propre palette (pierre, bois, métal, une couleur d'accent à elle) et ses pièces revues, sans
    /// faces confondues d'un niveau à l'autre. Une fois par prefab (marque « Allure du 30 octobre »), après les retouches de
    /// <see cref="TowerDefenseEvolutionsBaker"/>.
    /// </summary>
    /// <remarks>
    /// Les opérations sont dans <c>Editor/TowerDefenseLook.json</c> (une par ligne), pas dans le code : le même fichier sert à
    /// dessiner les tours hors de Unity (rendu de chaque niveau, recherche des faces superposées), et ce qu'on y voit est ce que
    /// ce baker construit. Opérations :
    /// <list type="bullet">
    /// <item><c>destroy</c> : les enfants de ce nom (le dernier nom du chemin) ;</item>
    /// <item><c>set</c> : place (<c>at</c>, ou <c>y</c>, <c>dy</c>), rotation (<c>rot</c>, degrés), échelle (<c>scale</c>, <c>sy</c>) ;</item>
    /// <item><c>model</c> : un modèle (« td/… », « castle/… », « town/… » pour les kits de Kenney, sinon un chemin sous Assets),
    /// à la largeur au sol (<c>width</c>) ou à la hauteur (<c>height</c>) voulue, posé au sol en <c>at</c> ; sans elles, à
    /// l'échelle <c>scale</c> avec son pivot en <c>at</c>. Sa rotation s'ajoute à celle de sa racine ;</item>
    /// <item><c>box</c>, <c>cylinder</c>, <c>sphere</c> : une forme simple de <c>size</c> mètres, au matériau d'un rôle de la
    /// palette (<c>role</c>, nuance <c>shade</c>) ou à un matériau du projet (<c>mat</c>) ;</item>
    /// <item><c>empty</c> : un repère (dans celui du prefab avec <c>root</c>, à l'échelle 1) ;</item>
    /// <item><c>follow</c> : le repère suit une partie qui tourne (<see cref="FollowPart"/>) ;</item>
    /// <item><c>field</c> : un réglage du bâtiment (des repères, un vecteur, le matériau d'un rôle, des valeurs par niveau si
    /// elles sont encore celles d'avant) ;</item>
    /// <item><c>paint</c> : les modèles des kits d'un repère prennent une autre palette.</item>
    /// </list>
    /// Pour finir, tous les modèles des kits de la tour prennent sa palette (une copie de leur texture de couleurs, recolorée,
    /// dans <c>Art/TowerDefense/Palettes</c>), et ses formes simples aux matériaux partagés (bois, fonte, métal…) le matériau du
    /// rôle correspondant.
    /// <para>
    /// Les <b>duos</b> (31 octobre, lot 7 de la refonte ; partie « duos » du fichier) : chacun est une tour nouvelle. Son prefab
    /// est fait une fois, dans <c>Prefabs/Structures/Duos</c> : une racine qui porte sa tour (<c>component</c>, une
    /// <see cref="TowerDuo"/>) ; puis, une fois (sa marque), son modèle (ses opérations, à sa palette). Sa définition
    /// (<c>Data/Duos</c> : ses 3 niveaux, leurs plans et leurs textes) est faite une fois, et il s'inscrit au catalogue
    /// <c>Resources/TowerDuos</c> s'il n'y est pas déjà : ce que le joueur y change reste. Dans une opération <c>field</c>, un
    /// matériau du projet se donne par <c>mat</c>, un asset (un son) par son chemin, <c>asset</c>.
    /// </para>
    /// </remarks>
    [InitializeOnLoad]
    static class TowerDefenseLookBaker
    {
        const string k_Json = "Assets/_Project/Editor/TowerDefenseLook.json";
        const string k_PrefabFolder = "Assets/_Project/Prefabs/Structures/";
        const string k_PaletteFolder = "Assets/_Project/Art/TowerDefense/Palettes";
        const string k_Mark = "Allure du 30 octobre";

        static readonly Dictionary<string, string> k_Kits = new Dictionary<string, string>
        {
            { "td", "Assets/kenney_tower-defense-kit/Models/FBX format/" },
            { "castle", "Assets/kenney_castle-kit/Models/FBX format/" },
            { "town", "Assets/kenney_fantasy-town-kit/Models/FBX format/" },
        };

        static readonly Dictionary<string, string> k_KitFiles = new Dictionary<string, string>
        {
            { "td", "TD" }, { "castle", "Chateau" }, { "town", "Village" },
        };

        // Un modèle de chaque kit, pour copier son matériau (ses réglages exacts) dans les palettes.
        static readonly Dictionary<string, string> k_KitProbe = new Dictionary<string, string>
        {
            { "td", "weapon-ballista" }, { "castle", "wall" }, { "town", "cart" },
        };

        // Les matériaux partagés des formes simples : leur rôle dans la palette, et leur nuance.
        static readonly Dictionary<string, (string Role, float Shade)> k_RoleOfMaterial = new Dictionary<string, (string, float)>
        {
            { "Bois", ("bois", 1f) }, { "Barricade_Wood", ("bois", 0.9f) }, { "Bois_sombre", ("bois", 0.68f) },
            { "Fonte", ("metal", 0.8f) }, { "Metal", ("metal", 1f) }, { "Noir_mat", ("metal", 0.55f) }, { "Pierre", ("pierre", 1f) },
            { "Fanion", ("accent", 1f) }, { "Aimant", ("accent", 1f) }, { "Corde", ("corde", 1f) }, { "Cuivre", ("cuivre", 1f) },
        };

        // Les rôles qui ne changent pas d'une tour à l'autre.
        static readonly Dictionary<string, string> k_FixedRoles = new Dictionary<string, string>
        {
            { "corde", "#BFA77A" }, { "cuivre", "#B86B3A" },
        };

        static TowerDefenseLookBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer || !File.Exists(k_Json))
                return;

            Dictionary<string, object> look;
            try
            {
                look = (Dictionary<string, object>)Json.Parse(File.ReadAllText(k_Json, Encoding.UTF8));
            }
            catch (Exception e)
            {
                Debug.LogError("Allure des tours : " + k_Json + " illisible (" + e.Message + ").");
                return;
            }

            var palettes = new Dictionary<string, Dictionary<string, string>>();
            foreach (var pair in (Dictionary<string, object>)look["palettes"])
            {
                var roles = new Dictionary<string, string>();
                foreach (var role in (Dictionary<string, object>)pair.Value)
                    roles[role.Key] = (string)role.Value;
                palettes[pair.Key] = roles;
            }

            ImportPalettes();
            foreach (Dictionary<string, object> tower in (List<object>)look["towers"])
            {
                var name = (string)tower["prefab"];
                try
                {
                    Apply(name, Text(tower, "mark") ?? k_Mark, Text(tower, "requires"), (List<object>)tower["ops"], palettes);
                }
                catch (Exception e)
                {
                    Debug.LogError("Allure des tours : " + name + ", " + e);
                }
            }

            // Les duos, après les tours : leur prefab part de celui des deux tours, à leur allure.
            if (look.TryGetValue("duos", out var duos) && duos is List<object> list)
            {
                foreach (Dictionary<string, object> duo in list)
                {
                    try
                    {
                        ApplyDuo(duo, palettes);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Duos : " + Text(duo, "name") + ", " + e);
                    }
                }
            }
        }

        // Les textures des palettes, écrites hors de Unity : importées tout de suite si elles ne le sont pas encore.
        static void ImportPalettes()
        {
            if (!Directory.Exists(k_PaletteFolder))
                return;
            foreach (var file in Directory.GetFiles(k_PaletteFolder, "*.png"))
            {
                var path = file.Replace('\\', '/');
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        // Une passe du fichier : une fois par prefab (sa marque), après celle qu'elle demande. Les passes suivantes (« Allure 2 du
        // 30 octobre »…) retouchent un prefab déjà passé.
        static void Apply(string name, string markName, string requires, List<object> ops, Dictionary<string, Dictionary<string, string>> palettes)
        {
            var path = k_PrefabFolder + name + ".prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.transform.Find(markName) != null ||
                (!string.IsNullOrEmpty(requires) && asset.transform.Find(requires) == null) || !palettes.ContainsKey(name))
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var context = new Context { Root = root.transform, Tower = name, Palettes = palettes };
                foreach (Dictionary<string, object> op in ops)
                    RunOp(context, op);
                if (!Paint(context))
                {
                    Debug.LogWarning("Allure des tours : " + name + ", les textures des palettes ne sont pas encore là ; au prochain lancement.");
                    return;
                }

                var mark = new GameObject(markName);
                mark.transform.SetParent(root.transform, false);
                mark.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("Allure des tours : " + name + ", " + markName.ToLowerInvariant() + " (" + path + ").");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        sealed class Context
        {
            public Transform Root;
            public string Tower;
            public Dictionary<string, Dictionary<string, string>> Palettes;
            public readonly HashSet<Renderer> Painted = new HashSet<Renderer>();
        }

        // ---------------------------------------------------------------- les opérations

        static void RunOp(Context context, Dictionary<string, object> op)
        {
            var kind = Text(op, "op");
            var root = context.Root;
            switch (kind)
            {
                case "destroy":
                    foreach (var t in FindAll(root, Text(op, "path")))
                        Object.DestroyImmediate(t.gameObject);
                    break;

                case "set":
                    foreach (var t in FindAll(root, Text(op, "path")))
                        Set(t, op);
                    break;

                case "model":
                case "box":
                case "cylinder":
                case "sphere":
                case "empty":
                {
                    var parent = Find(root, Text(op, "parent"));
                    if (parent == null)
                    {
                        Debug.LogWarning("Allure des tours : " + context.Tower + ", parent introuvable, " + Text(op, "parent"));
                        return;
                    }

                    if (kind == "model")
                        AddModel(context, parent, op);
                    else if (kind == "empty")
                        AddEmpty(root, parent, op);
                    else
                        AddShape(context, parent, kind, op);
                    break;
                }

                case "follow":
                {
                    var node = Find(root, Text(op, "path"));
                    var target = Find(root, Text(op, "target"));
                    if (node == null || target == null)
                        return;
                    var follow = node.GetComponent<FollowPart>();
                    if (follow == null)
                        follow = node.gameObject.AddComponent<FollowPart>();
                    var serialized = new SerializedObject(follow);
                    serialized.FindProperty("m_Target").objectReferenceValue = target;
                    serialized.FindProperty("m_YawOnly").boolValue = Flag(op, "yawOnly");
                    serialized.FindProperty("m_Offset").vector3Value = Vector(op, "offset", Vector3.zero);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    break;
                }

                case "field":
                    SetField(context, op);
                    break;

                case "paint":
                    foreach (var t in FindAll(root, Text(op, "path")))
                    {
                        foreach (var renderer in t.GetComponentsInChildren<Renderer>(true))
                            PaintKit(context, renderer, Text(op, "palette"));
                    }

                    break;

                default:
                    Debug.LogWarning("Allure des tours : opération inconnue, " + kind);
                    break;
            }
        }

        static void Set(Transform t, Dictionary<string, object> op)
        {
            if (op.ContainsKey("at"))
                t.localPosition = Vector(op, "at", t.localPosition);
            if (op.ContainsKey("rot"))
                t.localRotation = Quaternion.Euler(Vector(op, "rot", Vector3.zero));
            if (op.ContainsKey("scale"))
                t.localScale = Vector(op, "scale", t.localScale);
            if (op.ContainsKey("y"))
                t.localPosition = new Vector3(t.localPosition.x, Number(op, "y", 0f), t.localPosition.z);
            if (op.ContainsKey("dy"))
                t.localPosition += Vector3.up * Number(op, "dy", 0f);
            if (op.ContainsKey("sy"))
                t.localScale = new Vector3(t.localScale.x, Number(op, "sy", 1f), t.localScale.z);
            if (op.ContainsKey("active"))
                t.gameObject.SetActive(Flag(op, "active"));
        }

        static void AddModel(Context context, Transform parent, Dictionary<string, object> op)
        {
            var spec = Text(op, "model");
            var slash = spec.IndexOf('/');
            var kit = slash > 0 ? spec.Substring(0, slash) : null;
            var path = kit != null && k_Kits.ContainsKey(kit) ? k_Kits[kit] + spec.Substring(slash + 1) + ".fbx" : "Assets/" + spec;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("Allure des tours : modèle introuvable, " + path);
                return;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.gameObject.scene);
            go.name = Text(op, "name");
            go.transform.SetParent(parent, false);
            // Les bâtiments n'ont pas de collider : on vise leur emplacement.
            foreach (var collider in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);

            var t = go.transform;
            var baseScale = asset.transform.localScale;
            t.localRotation = Quaternion.Euler(Vector(op, "rot", Vector3.zero)) * asset.transform.localRotation;
            t.localScale = baseScale;
            t.localPosition = Vector3.zero;
            var at = Vector(op, "at", Vector3.zero);
            if (op.ContainsKey("width") || op.ContainsKey("height"))
            {
                // Posé au sol en « at » : le bas de son volume, au milieu.
                var bounds = BoundsIn(parent, t);
                var scale = op.ContainsKey("width") ? Number(op, "width", 1f) / Mathf.Max(1e-4f, Mathf.Max(bounds.size.x, bounds.size.z))
                    : Number(op, "height", 1f) / Mathf.Max(1e-4f, bounds.size.y);
                t.localScale = baseScale * scale;
                t.localPosition = at - scale * new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
            else
            {
                t.localScale = baseScale * Number(op, "scale", 1f);
                t.localPosition = at;
            }

            var palette = Text(op, "palette");
            if (!string.IsNullOrEmpty(palette))
            {
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                    PaintKit(context, renderer, palette);
            }
        }

        static void AddEmpty(Transform root, Transform parent, Dictionary<string, object> op)
        {
            var go = new GameObject(Text(op, "name"));
            SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            var t = go.transform;
            t.SetParent(parent, false);
            var at = Vector(op, "at", Vector3.zero);
            var rotation = Quaternion.Euler(Vector(op, "rot", Vector3.zero));
            if (Flag(op, "root"))
            {
                // Dans le repère du prefab, à l'échelle 1, même sous un parent mis à l'échelle (le drapeau de la baliste).
                t.SetPositionAndRotation(root.TransformPoint(at), root.rotation * rotation);
                var lossy = parent.lossyScale;
                t.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);
            }
            else
            {
                t.localPosition = at;
                t.localRotation = rotation;
            }
        }

        static void AddShape(Context context, Transform parent, string kind, Dictionary<string, object> op)
        {
            var type = kind == "box" ? PrimitiveType.Cube : kind == "cylinder" ? PrimitiveType.Cylinder : PrimitiveType.Sphere;
            var go = GameObject.CreatePrimitive(type);
            go.name = Text(op, "name");
            Object.DestroyImmediate(go.GetComponent<Collider>());
            SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = Vector(op, "at", Vector3.zero);
            t.localRotation = Quaternion.Euler(Vector(op, "rot", Vector3.zero));
            var size = Vector(op, "size", Vector3.one);
            // Le cylindre de Unity fait 2 m de haut.
            t.localScale = kind == "cylinder" ? new Vector3(size.x, size.y * 0.5f, size.z) : size;

            var renderer = go.GetComponent<MeshRenderer>();
            var material = op.ContainsKey("mat") ? NamedMaterial(Text(op, "mat"))
                : RoleMaterial(context, Text(op, "role"), Number(op, "shade", 1f));
            if (material != null)
                renderer.sharedMaterial = material;
            context.Painted.Add(renderer);
        }

        static void SetField(Context context, Dictionary<string, object> op)
        {
            // Le réglage d'un bâtiment, ou celui de l'effet d'un duo (component).
            var componentName = Text(op, "component");
            Component target = string.IsNullOrEmpty(componentName) ? context.Root.GetComponent<Structure>()
                : DuoType(componentName) is Type type ? context.Root.GetComponent(type) : null;
            if (target == null)
            {
                Debug.LogWarning("Allure des tours : " + context.Tower + ", composant introuvable, " + (componentName ?? "Structure"));
                return;
            }

            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(Text(op, "field"));
            if (property == null)
            {
                Debug.LogWarning("Allure des tours : " + context.Tower + ", réglage introuvable, " + Text(op, "field"));
                return;
            }

            if (op.TryGetValue("paths", out var pathsValue))
            {
                var paths = (List<object>)pathsValue;
                if (property.isArray)
                {
                    property.arraySize = paths.Count;
                    for (var i = 0; i < paths.Count; i++)
                    {
                        var element = property.GetArrayElementAtIndex(i);
                        element.objectReferenceValue = ReferenceTo(element, Find(context.Root, (string)paths[i]));
                    }
                }
                else if (paths.Count > 0)
                {
                    property.objectReferenceValue = ReferenceTo(property, Find(context.Root, (string)paths[0]));
                }
            }
            else if (op.ContainsKey("mat"))
            {
                property.objectReferenceValue = NamedMaterial(Text(op, "mat"));
            }
            else if (op.ContainsKey("value"))
            {
                property.floatValue = Number(op, "value", property.floatValue);
            }
            else if (op.ContainsKey("asset"))
            {
                // Un asset du projet par son chemin (les sons d'un duo).
                property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Object>(Text(op, "asset"));
            }
            else if (op.ContainsKey("vector"))
            {
                property.vector3Value = Vector(op, "vector", property.vector3Value);
            }
            else if (op.ContainsKey("role"))
            {
                property.objectReferenceValue = RoleMaterial(context, Text(op, "role"), Number(op, "shade", 1f));
            }
            else if (op.ContainsKey("floats"))
            {
                SetFloatsIfStill(property, Floats(op, "ifStill"), Floats(op, "floats"));
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Les valeurs par niveau, seulement si elles sont encore celles d'avant (le joueur a pu les régler).
        static void SetFloatsIfStill(SerializedProperty array, float[] old, float[] values)
        {
            if (array == null || !array.isArray || (old.Length > 0 && array.arraySize != old.Length))
                return;
            for (var i = 0; i < old.Length; i++)
            {
                if (!Mathf.Approximately(array.GetArrayElementAtIndex(i).floatValue, old[i]))
                    return;
            }

            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).floatValue = values[i];
        }

        // ---------------------------------------------------------------- les palettes

        // Tous les modèles des kits prennent la palette de la tour, et les formes simples aux matériaux partagés, le matériau du
        // rôle correspondant. Faux si une texture de palette manque encore (rien n'est enregistré).
        static bool Paint(Context context)
        {
            foreach (var renderer in context.Root.GetComponentsInChildren<Renderer>(true))
            {
                if (context.Painted.Contains(renderer))
                    continue;

                var materials = renderer.sharedMaterials;
                var changed = false;
                for (var i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null)
                        continue;

                    Material replacement = null;
                    var kit = KitOf(material);
                    if (kit != null)
                    {
                        replacement = PaletteMaterial(context.Tower, kit);
                        if (replacement == null)
                            return false;
                    }
                    else if (k_RoleOfMaterial.TryGetValue(material.name, out var role))
                    {
                        replacement = RoleMaterial(context, role.Role, role.Shade);
                    }

                    if (replacement != null && replacement != material)
                    {
                        materials[i] = replacement;
                        changed = true;
                    }
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }

            return true;
        }

        static void PaintKit(Context context, Renderer renderer, string palette)
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                var kit = materials[i] != null ? KitOf(materials[i]) : null;
                var replacement = kit != null ? PaletteMaterial(palette, kit) : null;
                if (replacement != null)
                    materials[i] = replacement;
            }

            renderer.sharedMaterials = materials;
            context.Painted.Add(renderer);
        }

        // Le kit d'un matériau de modèle de Kenney (sa texture est la « colormap » du kit), ou null.
        static string KitOf(Material material)
        {
            var texture = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : material.mainTexture;
            if (texture == null)
                return null;
            var path = AssetDatabase.GetAssetPath(texture);
            foreach (var kit in k_Kits)
            {
                if (path == kit.Value + "Textures/colormap.png")
                    return kit.Key;
            }

            return null;
        }

        static Material KitMaterial(string kit)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(k_Kits[kit] + k_KitProbe[kit] + ".fbx"))
            {
                if (asset is Material material)
                    return material;
            }

            return null;
        }

        // La copie du matériau du kit, avec la texture recolorée de cette palette.
        static Material PaletteMaterial(string palette, string kit)
        {
            var file = k_PaletteFolder + "/" + palette + "_" + k_KitFiles[kit];
            var material = AssetDatabase.LoadAssetAtPath<Material>(file + ".mat");
            if (material != null)
                return material;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(file + ".png");
            var source = KitMaterial(kit);
            if (texture == null || source == null)
                return null;

            material = new Material(source) { name = palette + "_" + k_KitFiles[kit] };
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            AssetDatabase.CreateAsset(material, file + ".mat");
            return material;
        }

        // Le matériau plat d'un rôle de la palette de la tour (même réglages que les modèles du kit), à cette nuance.
        static Material RoleMaterial(Context context, string role, float shade)
        {
            if (string.IsNullOrEmpty(role))
                return null;

            var tower = context.Tower;
            var file = k_PaletteFolder + "/" + tower + "_" + role + (Mathf.Approximately(shade, 1f) ? "" : "_" + Mathf.RoundToInt(shade * 100f));
            var material = AssetDatabase.LoadAssetAtPath<Material>(file + ".mat");
            if (material != null)
                return material;

            var hex = k_FixedRoles.TryGetValue(role, out var fixedHex) ? fixedHex
                : context.Palettes[tower].TryGetValue(role, out var paletteHex) ? paletteHex : null;
            var source = KitMaterial("td");
            if (hex == null || source == null || !ColorUtility.TryParseHtmlString(hex, out var color))
                return null;

            color = new Color(Mathf.Clamp01(color.r * shade), Mathf.Clamp01(color.g * shade), Mathf.Clamp01(color.b * shade), 1f);
            material = new Material(source) { name = Path.GetFileName(file) };
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", null);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", null);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            AssetDatabase.CreateAsset(material, file + ".mat");
            return material;
        }

        static readonly Dictionary<string, Material> s_Named = new Dictionary<string, Material>();

        static Material NamedMaterial(string name)
        {
            if (s_Named.TryGetValue(name, out var cached) && cached != null)
                return cached;

            foreach (var guid in AssetDatabase.FindAssets(name + " t:Material", new[] { "Assets/_Project" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name)
                {
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    s_Named[name] = material;
                    return material;
                }
            }

            Debug.LogWarning("Allure des tours : matériau introuvable, " + name);
            return null;
        }

        // Le volume des maillages de ce modèle, dans le repère de son parent (d'après les volumes de ses maillages).
        static Bounds BoundsIn(Transform parent, Transform model)
        {
            var toParent = parent.worldToLocalMatrix;
            var bounds = new Bounds();
            var any = false;
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                var matrix = toParent * filter.transform.localToWorldMatrix;
                var local = filter.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = local.center + Vector3.Scale(local.extents, new Vector3(i % 2 == 0 ? -1f : 1f, (i / 2) % 2 == 0 ? -1f : 1f, i < 4 ? -1f : 1f));
                    var point = matrix.MultiplyPoint3x4(corner);
                    if (any)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        any = true;
                    }
                }
            }

            return bounds;
        }

        // ---------------------------------------------------------------- les duos (31 octobre)

        const string k_DuoMark = "Duo du 31 octobre";
        const string k_DuoDataFolder = "Assets/_Project/Data/Duos/";
        const string k_CatalogPath = "Assets/_Project/Resources/TowerDuos.asset";

        // Un duo : son prefab (fait une fois), son modèle et sa palette (une fois, sa marque), sa définition, le catalogue.
        static void ApplyDuo(Dictionary<string, object> duo, Dictionary<string, Dictionary<string, string>> palettes)
        {
            var name = Text(duo, "name");
            var palette = Text(duo, "palette");
            var path = k_PrefabFolder + Text(duo, "prefab") + ".prefab";
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(palette) || !palettes.ContainsKey(palette))
                return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null && !CreateDuoPrefab(duo, path))
                return;

            var markName = Text(duo, "mark") ?? k_DuoMark;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path).transform.Find(markName) == null &&
                !BuildDuo(duo, path, markName, palette, palettes))
                return;

            var definition = DuoDefinition(duo, AssetDatabase.LoadAssetAtPath<GameObject>(path));
            if (definition != null)
                RegisterDuo(duo, name, definition);
        }

        // Le prefab, vide : sa racine et son effet (une TowerDuo, qui est la tour elle-même).
        static bool CreateDuoPrefab(Dictionary<string, object> duo, string path)
        {
            var type = DuoType(Text(duo, "component"));
            if (type == null || !typeof(TowerDuo).IsAssignableFrom(type))
            {
                Debug.LogWarning("Duos : " + Text(duo, "name") + ", tour introuvable, " + Text(duo, "component"));
                return false;
            }

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject(Path.GetFileNameWithoutExtension(path));
                SceneManager.MoveGameObjectToScene(root, scene);
                root.AddComponent(type);
                PrefabUtility.SaveAsPrefabAsset(root, path, out var saved);
                if (saved)
                    Debug.Log("Duos : " + Text(duo, "name") + ", prefab créé (" + path + ").");
                return saved;
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // Son modèle (les opérations), à sa palette, et sa marque.
        static bool BuildDuo(Dictionary<string, object> duo, string path, string markName, string palette,
                             Dictionary<string, Dictionary<string, string>> palettes)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Une nouvelle passe qui refait tout le modèle (rebuild) : ses pièces d'avant partent, la tour reste.
                if (Flag(duo, "rebuild"))
                {
                    for (var i = root.transform.childCount - 1; i >= 0; i--)
                        Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }

                var context = new Context { Root = root.transform, Tower = palette, Palettes = palettes };
                foreach (Dictionary<string, object> op in (List<object>)duo["ops"])
                    RunOp(context, op);
                if (!Paint(context))
                    return false;

                var mark = new GameObject(markName);
                mark.transform.SetParent(root.transform, false);
                mark.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("Duos : " + Text(duo, "name") + ", " + markName.ToLowerInvariant() + " (" + path + ").");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Sa définition (Data/Duos) : ses 3 niveaux (leurs plans, leurs textes), sa place, ses cases. Faite une fois : ce que le
        // joueur y change reste.
        static StructureDefinition DuoDefinition(Dictionary<string, object> duo, GameObject prefab)
        {
            var path = k_DuoDataFolder + Text(duo, "definition") + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<StructureDefinition>(path);
            if (existing != null)
            {
                UpdateDuoTexts(duo, existing);
                return existing;
            }

            var structure = prefab != null ? prefab.GetComponent<Structure>() : null;
            if (structure == null)
                return null;

            EnsureFolder(k_DuoDataFolder.TrimEnd('/'));
            var definition = ScriptableObject.CreateInstance<StructureDefinition>();
            definition.kind = StructureKind.Duo;
            definition.displayName = Text(duo, "name");
            definition.slot = Enum.TryParse<BuildSlot>(Text(duo, "slot") ?? "", out var slot) ? slot : BuildSlot.Roadside;
            definition.prefab = structure;
            if (ColorUtility.TryParseHtmlString(Text(duo, "color") ?? "", out var color))
                definition.color = color;
            var plans = Floats(duo, "plans");
            var texts = duo.TryGetValue("levels", out var levelsValue) ? levelsValue as List<object> : null;
            var count = texts != null ? texts.Count : 3;
            definition.levels = new StructureDefinition.Level[count];
            for (var i = 0; i < count; i++)
            {
                definition.levels[i] = new StructureDefinition.Level
                {
                    plans = i < plans.Length ? Mathf.RoundToInt(plans[i]) : 4,
                    description = texts != null ? texts[i] as string : "",
                };
            }

            var footprint = Floats(duo, "footprint");
            if (footprint.Length == 2)
                definition.footprint = new Vector2Int(Mathf.RoundToInt(footprint[0]), Mathf.RoundToInt(footprint[1]));
            AssetDatabase.CreateAsset(definition, path);
            Debug.Log("Duos : " + Text(duo, "name") + ", définition créée (" + path + ").");
            return definition;
        }

        // Les textes des niveaux d'un duo revu (levels), à la place de ceux d'avant (levelsBefore) : seulement ceux que le joueur
        // n'a pas changés.
        static void UpdateDuoTexts(Dictionary<string, object> duo, StructureDefinition definition)
        {
            var texts = duo.TryGetValue("levels", out var a) ? a as List<object> : null;
            var before = duo.TryGetValue("levelsBefore", out var b) ? b as List<object> : null;
            if (texts == null || before == null || definition.levels == null)
                return;

            var changed = false;
            for (var i = 0; i < definition.levels.Length && i < texts.Count && i < before.Count; i++)
            {
                if (definition.levels[i].description != before[i] as string || before[i] as string == texts[i] as string)
                    continue;
                definition.levels[i].description = texts[i] as string;
                changed = true;
            }

            if (!changed)
                return;
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssetIfDirty(definition);
            Debug.Log("Duos : " + Text(duo, "name") + ", textes des niveaux revus (" + AssetDatabase.GetAssetPath(definition) + ").");
        }

        // Inscrit le duo au catalogue s'il n'y est pas (par son nom) ; sinon lui rend seulement sa définition s'il l'a perdue.
        static void RegisterDuo(Dictionary<string, object> duo, string name, StructureDefinition definition)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<TowerDuoCatalog>(k_CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<TowerDuoCatalog>();
                AssetDatabase.CreateAsset(catalog, k_CatalogPath);
            }

            var serialized = new SerializedObject(catalog);
            var list = serialized.FindProperty("m_Duos");
            for (var i = 0; i < list.arraySize; i++)
            {
                var existing = list.GetArrayElementAtIndex(i);
                if (existing.FindPropertyRelative("displayName").stringValue != name)
                    continue;
                var definitionProperty = existing.FindPropertyRelative("definition");
                if (definitionProperty.objectReferenceValue != null)
                    return;
                definitionProperty.objectReferenceValue = definition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(catalog);
                return;
            }

            var kinds = duo.TryGetValue("kinds", out var kindsValue) ? kindsValue as List<object> : null;
            list.arraySize++;
            var entry = list.GetArrayElementAtIndex(list.arraySize - 1);
            entry.FindPropertyRelative("displayName").stringValue = name;
            SetEnum(entry.FindPropertyRelative("first"), kinds != null && kinds.Count > 0 ? kinds[0] as string : null);
            SetEnum(entry.FindPropertyRelative("second"), kinds != null && kinds.Count > 1 ? kinds[1] as string : null);
            entry.FindPropertyRelative("definition").objectReferenceValue = definition;
            if (ColorUtility.TryParseHtmlString(Text(duo, "color") ?? "", out var color))
                entry.FindPropertyRelative("color").colorValue = color;
            entry.FindPropertyRelative("description").stringValue = Text(duo, "description") ?? "";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("Duos : " + name + ", inscrit au catalogue (" + k_CatalogPath + ").");
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        static void SetEnum(SerializedProperty property, string value)
        {
            var index = value != null ? Array.IndexOf(property.enumNames, value) : -1;
            if (index >= 0)
                property.enumValueIndex = index;
            else
                Debug.LogWarning("Duos : sorte de tour inconnue, " + value);
        }

        // L'effet d'un duo par son nom de classe (DuoCannon…), dans le code du jeu.
        static Type DuoType(string name) =>
            string.IsNullOrEmpty(name) ? null : typeof(TowerDuo).Assembly.GetType("Archery.Building." + name);

        // Ce qu'attend un réglage : le repère lui-même, son objet, ou le composant du bon type qu'il porte (une tour, pour l'autre
        // tour d'un duo).
        static Object ReferenceTo(SerializedProperty property, Transform t)
        {
            if (t == null)
                return null;

            var type = property.type;
            var name = type.StartsWith("PPtr<") ? type.Substring(5, type.Length - 6).TrimStart('$') : "";
            if (name == "" || name == "Transform")
                return t;
            if (name == "GameObject")
                return t.gameObject;
            foreach (var component in t.GetComponents<Component>())
            {
                for (var k = component != null ? component.GetType() : null; k != null; k = k.BaseType)
                {
                    if (k.Name == name)
                        return component;
                }
            }

            return t;
        }

        // ---------------------------------------------------------------- chemins

        // Un repère par son chemin de noms (le premier enfant de chaque nom) ; vide : la racine.
        static Transform Find(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path))
                return root;
            var t = root;
            foreach (var part in path.Split('/'))
            {
                t = Child(t, part);
                if (t == null)
                    return null;
            }

            return t;
        }

        // Tous les enfants qui portent le dernier nom du chemin (plusieurs pièces ont souvent le même nom).
        static List<Transform> FindAll(Transform root, string path)
        {
            var found = new List<Transform>();
            var slash = path.LastIndexOf('/');
            var parent = slash < 0 ? root : Find(root, path.Substring(0, slash));
            var last = slash < 0 ? path : path.Substring(slash + 1);
            if (parent == null)
                return found;
            foreach (Transform child in parent)
            {
                if (child.name == last)
                    found.Add(child);
            }

            return found;
        }

        static Transform Child(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
            }

            return null;
        }

        // ---------------------------------------------------------------- les valeurs d'une opération

        static string Text(Dictionary<string, object> op, string key) => op.TryGetValue(key, out var value) ? value as string : null;

        static bool Flag(Dictionary<string, object> op, string key) => op.TryGetValue(key, out var value) && value is bool flag && flag;

        static float Number(Dictionary<string, object> op, string key, float fallback) =>
            op.TryGetValue(key, out var value) && value is double number ? (float)number : fallback;

        static float[] Floats(Dictionary<string, object> op, string key)
        {
            if (!op.TryGetValue(key, out var value) || !(value is List<object> list))
                return Array.Empty<float>();
            var result = new float[list.Count];
            for (var i = 0; i < list.Count; i++)
                result[i] = (float)(double)list[i];
            return result;
        }

        static Vector3 Vector(Dictionary<string, object> op, string key, Vector3 fallback)
        {
            if (!op.TryGetValue(key, out var value))
                return fallback;
            if (value is double number)
                return Vector3.one * (float)number;
            var list = value as List<object>;
            return list != null && list.Count == 3 ? new Vector3((float)(double)list[0], (float)(double)list[1], (float)(double)list[2]) : fallback;
        }

        // ---------------------------------------------------------------- un lecteur JSON minimal (objets, listes, nombres,
        // textes, vrai/faux, null), JsonUtility ne lisant ni les dictionnaires ni les champs absents

        static class Json
        {
            public static object Parse(string text)
            {
                var index = 0;
                var value = Value(text, ref index);
                Skip(text, ref index);
                if (index < text.Length)
                    throw new FormatException("texte en trop au caractère " + index);
                return value;
            }

            static object Value(string s, ref int i)
            {
                Skip(s, ref i);
                if (i >= s.Length)
                    throw new FormatException("fin inattendue");
                var c = s[i];
                if (c == '{')
                {
                    var result = new Dictionary<string, object>();
                    i++;
                    Skip(s, ref i);
                    if (s[i] == '}')
                    {
                        i++;
                        return result;
                    }

                    while (true)
                    {
                        Skip(s, ref i);
                        var key = Str(s, ref i);
                        Skip(s, ref i);
                        Expect(s, ref i, ':');
                        result[key] = Value(s, ref i);
                        Skip(s, ref i);
                        if (s[i] == ',')
                        {
                            i++;
                            continue;
                        }

                        Expect(s, ref i, '}');
                        return result;
                    }
                }

                if (c == '[')
                {
                    var result = new List<object>();
                    i++;
                    Skip(s, ref i);
                    if (s[i] == ']')
                    {
                        i++;
                        return result;
                    }

                    while (true)
                    {
                        result.Add(Value(s, ref i));
                        Skip(s, ref i);
                        if (s[i] == ',')
                        {
                            i++;
                            continue;
                        }

                        Expect(s, ref i, ']');
                        return result;
                    }
                }

                if (c == '"')
                    return Str(s, ref i);
                if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0)
                {
                    i += 4;
                    return true;
                }

                if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0)
                {
                    i += 5;
                    return false;
                }

                if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0)
                {
                    i += 4;
                    return null;
                }

                var start = i;
                while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0)
                    i++;
                if (start == i)
                    throw new FormatException("caractère inattendu « " + c + " » au caractère " + i);
                return double.Parse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            static string Str(string s, ref int i)
            {
                Expect(s, ref i, '"');
                var builder = new StringBuilder();
                while (s[i] != '"')
                {
                    if (s[i] == '\\')
                    {
                        i++;
                        switch (s[i])
                        {
                            case 'n': builder.Append('\n'); break;
                            case 't': builder.Append('\t'); break;
                            case 'u':
                                builder.Append((char)int.Parse(s.Substring(i + 1, 4), NumberStyles.HexNumber));
                                i += 4;
                                break;
                            default: builder.Append(s[i]); break;
                        }
                    }
                    else
                    {
                        builder.Append(s[i]);
                    }

                    i++;
                }

                i++;
                return builder.ToString();
            }

            static void Skip(string s, ref int i)
            {
                while (i < s.Length && char.IsWhiteSpace(s[i]))
                    i++;
            }

            static void Expect(string s, ref int i, char c)
            {
                if (i >= s.Length || s[i] != c)
                    throw new FormatException("« " + c + " » attendu au caractère " + i);
                i++;
            }
        }
    }
}
