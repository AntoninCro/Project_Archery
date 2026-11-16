using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le branchement des modèles faits main (14 novembre, la refonte visuelle ; <c>Docs/Refonte_Visuelle.md</c>, partie 4, et
    /// <c>Docs/Guide_Blender.md</c>). Le joueur fait un bâtiment dans Blender ; l'extension THUNK l'exporte dans
    /// <c>Art/Modeles/&lt;Nom&gt;.fbx</c>, avec <c>&lt;Nom&gt;.json</c> : les repères du code (leur nom dans Blender, leur chemin dans le
    /// prefab). Dès que les deux arrivent (ou changent), le prefab du bâtiment est mis à jour, sans rien d'autre à faire :
    /// <list type="bullet">
    /// <item>les objets que le code désigne (ou qui portent un script) restent, avec leurs réglages : chacun prend la place de son
    /// repère dans Blender, et la pièce du joueur rattachée à ce repère ; les niveaux (« Niveau 2 »…) prennent les pièces de leur
    /// niveau ;</item>
    /// <item>les pièces de Claude sont retirées (les formes simples, les modèles des kits qu'il avait posés) ; ce qui n'est pas une
    /// forme (une lumière, des particules, les marques cachées des bakers) reste ;</item>
    /// <item>le reste du modèle va sous « Modele fait main » ; une pièce que le code lit (elle avait une forme à elle chez Claude) est
    /// fondue en un maillage posé sur l'objet du code (<c>Art/Modeles/Fusions</c>) ;</item>
    /// <item>les couleurs : un matériau par nom de matériau de Blender, partagé par tous les bâtiments (<c>Art/Modeles/Materiaux</c> ;
    /// « Kenney TD »… prend la texture du kit ; un nom qui contient « Lueur » luit ; un nom qui commence par « Transparent » est
    /// transparent, à l'Alpha de Blender) ;</item>
    /// <item>un objet caché « Fait main » : plus aucun baker ne touche à ce bâtiment (règle de <c>CLAUDE.md</c>).</item>
    /// </list>
    /// Pour le refaire à la main : Tools > THUNK > Brancher les modèles faits main. La console dit ce qui a été fait.
    /// </summary>
    class ModelesFaitMain : AssetPostprocessor
    {
        const string k_Folder = "Assets/_Project/Art/Modeles/";
        const string k_Materials = "Assets/_Project/Art/Modeles/Materiaux";
        const string k_Fusions = "Assets/_Project/Art/Modeles/Fusions";
        const string k_Template = "Assets/_Project/Art/TowerDefense/Bois.mat";
        const string k_TransparentTemplate = "Assets/_Project/Art/TowerDefense/Glace.mat";
        const string k_Movements = "Assets/_Project/Art/Modeles/Mouvements/";
        const string k_Container = "Modele fait main";
        const string k_Mark = "Fait main";

        // Les matériaux des kits qui peignent avec une texture de couleurs (Tools/blender/bibliotheque.py les nomme ainsi).
        static readonly Dictionary<string, string> k_KitTextures = new Dictionary<string, string>
        {
            { "Kenney TD", "Assets/kenney_tower-defense-kit/Models/FBX format/Textures/colormap.png" },
            { "Kenney Chateau", "Assets/kenney_castle-kit/Models/FBX format/Textures/colormap.png" },
            { "Kenney Village", "Assets/kenney_fantasy-town-kit/Models/FBX format/Textures/colormap.png" },
        };

        static readonly HashSet<string> s_Pending = new HashSet<string>();

        [Serializable]
        class Segment
        {
            public string nom;
            public int rang;
        }

        [Serializable]
        class Repere
        {
            public string cle;
            public Segment[] chemin;
            public bool piece;
        }

        // Les mouvements d'un bâtiment (Art/Modeles/Mouvements/<Nom>.json, écrit par Claude à la demande du joueur) : des pièces
        // du joueur, par leur nom dans Blender, qui tournent ensemble autour de leur centre (14 novembre : le grand cristal du
        // niveau 5 du Givre).
        // « vise » (16 novembre, le tube du Mortier qui se lève selon la distance de l'ennemi, Building/BarrelPitch) : [élévation
        // du modèle, élévation pour un ennemi proche, pour un ennemi loin] ; la pièce tourne autour de son origine (l'axe) ;
        // « suiveurs » : les repères du code qui suivent son bout (les bouches).
        [Serializable]
        class Mouvement
        {
            public string nom;
            public string[] pieces;
            public float tourne;
            public float[] axe;
            public float[] vise;
            public string[] suiveurs;
        }

        // La place de chaque objet dans Blender (16 novembre) : l'export FBX de Blender (« Appliquer la transformation »)
        // donne un quart de tour de trop à une pièce rattachée à un repère lui-même rattaché à un repère (les canons du niveau 3
        // du Mortier, sous « Niveau 3 », sous « Affût », pointaient vers le sol) ; le branchement replace tout d'après Blender.
        [Serializable]
        class ObjetBlender
        {
            public string nom;
            public float[] matrice;
        }

        [Serializable]
        class Mouvements
        {
            public Mouvement[] mouvements;
        }

        // Le niveau où une pièce du joueur apparaît, et celui où elle disparaît (0 : jamais ; 15 novembre, le bouton « La
        // sélection disparaît au niveau » de l'onglet THUNK : la base du Mortier d'une couleur par niveau).
        [Serializable]
        class Niveau
        {
            public string piece;
            public int de;
            public int retire;
        }

        [Serializable]
        class Sidecar
        {
            public string batiment;
            public string prefab;
            public string fbx;
            public string exporte_le;
            public Repere[] reperes;
            public string[] sources;
            public int minutes;
            public Niveau[] niveaux;
            public ObjetBlender[] objets;
        }

        // ---------------------------------------------------------------- l'import

        void OnPreprocessModel()
        {
            if (!InFolder(assetPath))
                return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importBlendShapes = false;
            importer.isReadable = true;            // pour fondre les pièces que le code lit, et les ultimes au lancement
            importer.addCollider = false;          // les bâtiments n'ont pas de colliders
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        static bool InFolder(string path) =>
            path.StartsWith(k_Folder, StringComparison.Ordinal) && path.IndexOf('/', k_Folder.Length) < 0;

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (path.StartsWith(k_Movements, StringComparison.Ordinal) && path.EndsWith(".json", StringComparison.Ordinal))
                {
                    s_Pending.Add(Path.GetFileNameWithoutExtension(path));
                    continue;
                }
                if (!InFolder(path))
                    continue;
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".fbx" || ext == ".json")
                    s_Pending.Add(Path.GetFileNameWithoutExtension(path));
            }
            if (s_Pending.Count == 0)
                return;
            EditorApplication.delayCall -= Flush;
            EditorApplication.delayCall += Flush;
        }

        static void Flush()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // en jeu : à la sortie du mode Play
                EditorApplication.playModeStateChanged -= AfterPlay;
                EditorApplication.playModeStateChanged += AfterPlay;
                return;
            }
            var names = s_Pending.ToList();
            s_Pending.Clear();
            foreach (var name in names)
                Brancher(name, false);
        }

        static void AfterPlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;
            EditorApplication.playModeStateChanged -= AfterPlay;
            Flush();
        }

        [MenuItem("Tools/THUNK/Brancher les modèles faits main")]
        static void BrancherTout()
        {
            if (!Directory.Exists(k_Folder))
            {
                Debug.Log("Modèles faits main : aucun pour l'instant (" + k_Folder + ").");
                return;
            }
            foreach (var json in Directory.GetFiles(k_Folder, "*.json"))
                Brancher(Path.GetFileNameWithoutExtension(json), true);
        }

        // ---------------------------------------------------------------- le branchement

        static void Brancher(string name, bool verbose)
        {
            var jsonPath = k_Folder + name + ".json";
            var fbxPath = k_Folder + name + ".fbx";
            if (!File.Exists(jsonPath) || !File.Exists(fbxPath))
            {
                if (verbose)
                    Debug.LogWarning("Modèles faits main : " + name + ", il manque le .fbx ou le .json (réexporter depuis Blender).");
                return;
            }
            Sidecar side;
            try
            {
                side = JsonUtility.FromJson<Sidecar>(File.ReadAllText(jsonPath, Encoding.UTF8));
            }
            catch (Exception e)
            {
                Debug.LogError("Modèles faits main : " + name + ", le .json est illisible (" + e.Message + ").");
                return;
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null || side == null || side.reperes == null || string.IsNullOrEmpty(side.prefab) ||
                AssetDatabase.LoadAssetAtPath<GameObject>(side.prefab) == null)
            {
                Debug.LogWarning("Modèles faits main : " + name + ", modèle ou prefab introuvable (" + side?.prefab + ").");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(side.prefab);
            var warnings = new List<string>();
            try
            {
                var report = Branch(root, model, side, fbxPath, name, warnings);
                PrefabUtility.SaveAsPrefabAsset(root, side.prefab);
                Debug.Log("Modèles faits main : " + name + " branché sur " + side.prefab + " : " + report);
                foreach (var w in warnings)
                    Debug.LogWarning("Modèles faits main : " + name + ", " + w);
            }
            catch (Exception e)
            {
                Debug.LogError("Modèles faits main : " + name + ", le branchement a échoué, le prefab n'est pas changé (" + e + ").");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
        }

        static string Branch(GameObject root, GameObject model, Sidecar side, string fbxPath, string name, List<string> warnings)
        {
            var rt = root.transform;

            // 0. Les mouvements du branchement d'avant (refaits à la fin, avec les pièces du nouvel export).
            foreach (var spin in root.GetComponentsInChildren<Archery.Building.ModelSpin>(true))
                if (spin != null)
                    Object.DestroyImmediate(spin.gameObject);
            // les pièces qui disparaissaient à un niveau, les tubes qui se levaient : redevenus de simples formes, le nouvel export
            // les remplace
            foreach (var hide in root.GetComponentsInChildren<Archery.Building.LevelHide>(true))
            {
                var f = hide.GetComponent<MeshFilter>();
                if (f != null && IsMine(f.sharedMesh, fbxPath, name))
                {
                    hide.gameObject.SetActive(true);
                    Object.DestroyImmediate(hide);
                }
            }
            foreach (var pitch in root.GetComponentsInChildren<Archery.Building.BarrelPitch>(true))
            {
                var f = pitch.GetComponent<MeshFilter>();
                if (f != null && IsMine(f.sharedMesh, fbxPath, name))
                    Object.DestroyImmediate(pitch);
            }

            // 1. Ce que le code désigne (les réglages des scripts), et qui porte un script : comme Tools/refonte_visuelle/reperes.py.
            var raw = new HashSet<Transform>();
            var canon = new HashSet<Transform>();
            CollectKept(root, raw, canon);
            var keptPath = new HashSet<Transform>();
            foreach (var t in canon)
                AddWithParents(t, rt, keptPath);

            // 2. Les repères : leur chemin dans le prefab (le rang compte les frères du même nom qui restent).
            var resolved = new Dictionary<string, Transform>();
            foreach (var r in side.reperes)
            {
                var t = Resolve(rt, r.chemin, keptPath);
                if (t != null)
                    resolved[r.cle] = t;
                else
                    warnings.Add("repère introuvable dans le prefab : « " + r.cle + " » (il garde la place de Claude).");
            }

            // 3. Ce qui reste : le code, ses parents, et ce qui n'est pas une forme (lumières, particules, marques cachées…).
            var stay = new HashSet<Transform>(keptPath);
            foreach (var t in raw)
                AddWithParents(t, rt, stay);
            foreach (var t in rt.GetComponentsInChildren<Transform>(true))
            {
                if (t == rt || PrefabUtility.IsPartOfPrefabInstance(t))
                    continue;
                if (!OnlyGeometry(t) || (t.GetComponent<MeshFilter>() == null && t.childCount == 0))
                    AddWithParents(t, rt, stay);
            }
            var hadChildren = rt.GetComponentsInChildren<Transform>(true).ToDictionary(t => t, t => t.childCount > 0);

            // 4. Les modèles des kits que Claude avait posés : défaits s'ils gardent un objet du code, sinon retirés.
            var removed = 0;
            foreach (var t in rt.GetComponentsInChildren<Transform>(true).ToArray())
            {
                if (t == null || !PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) ||
                    PrefabUtility.GetPrefabAssetType(t.gameObject) != PrefabAssetType.Model)
                    continue;
                if (t.GetComponentsInChildren<Transform>(true).Any(stay.Contains))
                    PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                else
                {
                    Object.DestroyImmediate(t.gameObject);
                    removed++;
                }
            }

            // 5. L'ancien branchement (un nouvel export remplace le précédent).
            var old = rt.Find(k_Container);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            foreach (var f in rt.GetComponentsInChildren<MeshFilter>(true).ToArray())
            {
                if (f == null || !IsMine(f.sharedMesh, fbxPath, name))
                    continue;
                if (stay.Contains(f.transform) || f.GetComponentsInChildren<Transform>(true).Any(stay.Contains))
                    StripGeometry(f.gameObject);
                else
                    Object.DestroyImmediate(f.gameObject);
            }

            // 6. Les pièces de Claude : retirées ; les objets du code perdent seulement leur forme.
            var stripped = 0;
            foreach (var t in rt.GetComponentsInChildren<Transform>(true).OrderByDescending(Depth).ToArray())
            {
                if (t == null || t == rt)
                    continue;
                var filter = t.GetComponent<MeshFilter>();
                if (stay.Contains(t))
                {
                    if (filter != null && !IsMine(filter.sharedMesh, fbxPath, name))
                    {
                        StripGeometry(t.gameObject);
                        stripped++;
                    }
                    continue;
                }
                if (!OnlyGeometry(t) || t.childCount > 0)
                    continue;
                // un objet vide qui n'avait pas d'enfants (un repère, une marque) reste ; une forme ou un groupe vidé part
                if (filter == null && !(hadChildren.TryGetValue(t, out var had) && had))
                    continue;
                Object.DestroyImmediate(t.gameObject);
                removed++;
            }

            // 7. Le modèle fait main : chaque pièce à sa place.
            var container = new GameObject(k_Container).transform;
            container.SetParent(rt, false);
            var clone = (GameObject)Object.Instantiate(model, rt, false);
            var keyed = new Dictionary<string, Repere>();
            foreach (var r in side.reperes)
                keyed[r.cle] = r;
            var materials = new Dictionary<string, Material>();
            var toDestroy = new List<GameObject>();
            var placed = 0;
            var aligned = 0;
            var byBlenderName = new Dictionary<string, Transform>();

            void Place(Transform c, Transform parent)
            {
                if (keyed.TryGetValue(c.name, out var rep) && resolved.TryGetValue(rep.cle, out var u))
                {
                    Align(u, c);
                    byBlenderName[c.name] = u;
                    aligned++;
                    var f = c.GetComponent<MeshFilter>();
                    var r = c.GetComponent<MeshRenderer>();
                    if (f != null && r != null)
                    {
                        // pas de « ?? » : dans l'éditeur, un composant absent est un faux null
                        var uf = u.GetComponent<MeshFilter>();
                        if (uf == null)
                            uf = u.gameObject.AddComponent<MeshFilter>();
                        uf.sharedMesh = f.sharedMesh;
                        var ur = u.GetComponent<MeshRenderer>();
                        if (ur == null)
                            ur = u.gameObject.AddComponent<MeshRenderer>();
                        ur.sharedMaterials = Map(r.sharedMaterials, materials);
                        placed++;
                    }
                    foreach (var g in Children(c))
                        Place(g, u);
                    toDestroy.Add(c.gameObject);
                }
                else
                {
                    c.SetParent(parent, true);
                    byBlenderName[c.name] = c;
                    var r = c.GetComponent<MeshRenderer>();
                    if (r != null)
                    {
                        r.sharedMaterials = Map(r.sharedMaterials, materials);
                        placed++;
                    }
                    foreach (var g in Children(c))
                        Place(g, c);
                }
            }

            foreach (var c in Children(clone.transform))
                Place(c, container);
            foreach (var g in toDestroy)
                Object.DestroyImmediate(g);
            Object.DestroyImmediate(clone);

            // 7 ter. Chaque objet à sa place de Blender (les parents d'abord), pas à celle du FBX (ObjetBlender).
            if (side.objets != null)
            {
                var toPrefab = rt.localToWorldMatrix;
                foreach (var o in side.objets.Where(x => x != null && x.matrice != null && x.matrice.Length == 16 &&
                                                         byBlenderName.ContainsKey(x.nom ?? ""))
                             .OrderBy(x => Depth(byBlenderName[x.nom])))
                    SetFromBlender(byBlenderName[o.nom], toPrefab * BlenderToUnity(o.matrice));
            }

            // 7 bis. Les pièces qui disparaissent à un niveau (Building/LevelHide ; Structure les allume selon le niveau), et les
            // objets du code rattachés dans Blender à une telle pièce (16 novembre : une bouche du Mortier ; cachée, elle ne tire
            // pas). Ceux du branchement d'avant sont d'abord rallumés : le nouvel export dit lesquels disparaissent.
            var hidden = 0;
            foreach (var u in resolved.Values)
            {
                var previous = u != null ? u.GetComponent<Archery.Building.LevelHide>() : null;
                if (previous != null)
                {
                    u.gameObject.SetActive(true);
                    Object.DestroyImmediate(previous);
                }
            }
            if (side.niveaux != null)
            {
                var mine = rt.GetComponentsInChildren<MeshFilter>(true)
                    .Where(f => IsMine(f.sharedMesh, fbxPath, name)).Select(f => f.transform).ToList();
                foreach (var n in side.niveaux)
                {
                    if (n == null || n.retire < 2)
                        continue;
                    var t = resolved.TryGetValue(n.piece ?? "", out var code) && code != null
                        ? code
                        : mine.FirstOrDefault(x => x != null && x.name == n.piece);
                    if (t == null)
                    {
                        warnings.Add("la pièce « " + n.piece + " » (elle disparaît au niveau " + n.retire + ") est introuvable.");
                        continue;
                    }
                    var hide = t.GetComponent<Archery.Building.LevelHide>();
                    if (hide == null)
                        hide = t.gameObject.AddComponent<Archery.Building.LevelHide>();
                    hide.HideFromLevel = Mathf.Clamp(n.retire, 2, 5);
                    hidden++;
                }
            }

            // 8. Les pièces que le code lit (une forme à elles chez Claude) : fondues en un maillage sur l'objet du code ; pas celles
            // qui disparaissent à un niveau (elles restent à part, pour s'éteindre seules).
            var fused = 0;
            foreach (var r in side.reperes)
            {
                if (!r.piece || !resolved.TryGetValue(r.cle, out var u) || u.GetComponent<MeshFilter>() != null)
                    continue;
                var parts = u.GetComponentsInChildren<MeshRenderer>(true)
                    .Where(x => x.transform != u && x.GetComponent<MeshFilter>() != null &&
                                IsMine(x.GetComponent<MeshFilter>().sharedMesh, fbxPath, name) && OwnedBy(x.transform, u, stay) &&
                                x.GetComponentInParent<Archery.Building.LevelHide>(true) == null)
                    .ToList();
                if (parts.Count == 0)
                {
                    warnings.Add("rien n'est rattaché à « " + r.cle + " » : l'objet du code reste sans forme.");
                    continue;
                }
                var mesh = Combine(parts, u, out var mats);
                mesh.name = name + " - " + r.cle;
                mesh = SaveMesh(mesh, k_Fusions + "/" + name + "_" + FileName(r.cle) + ".asset");
                u.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                u.gameObject.AddComponent<MeshRenderer>().sharedMaterials = mats;
                foreach (var p in parts.OrderByDescending(x => Depth(x.transform)))
                {
                    if (p == null)
                        continue;
                    if (p.GetComponentsInChildren<Transform>(true).Any(stay.Contains))
                        StripGeometry(p.gameObject);
                    else if (p.transform.childCount == 0)
                        Object.DestroyImmediate(p.gameObject);
                    else
                        StripGeometry(p.gameObject);
                }
                fused++;
            }

            // 8 bis. Les mouvements demandés par le joueur (des pièces qui tournent).
            var moving = ApplyMovements(rt, name, fbxPath, resolved, warnings);

            // 9. La marque : plus aucun baker ne touche à ce bâtiment.
            if (rt.Find(k_Mark) == null)
            {
                var mark = new GameObject(k_Mark);
                mark.transform.SetParent(rt, false);
                mark.SetActive(false);
            }

            var triangles = 0;
            foreach (var f in rt.GetComponentsInChildren<MeshFilter>(true))
                if (f.sharedMesh != null && IsMine(f.sharedMesh, fbxPath, name))
                    triangles += (int)Enumerable.Range(0, f.sharedMesh.subMeshCount).Sum(i => (long)f.sharedMesh.GetIndexCount(i)) / 3;
            return aligned + " repères placés, " + placed + " pièces du joueur, " + fused + " fusions, " + moving + " mouvements, " +
                   (hidden > 0 ? hidden + " pièces qui disparaissent à un niveau, " : "") + triangles +
                   " triangles ; " + removed + " pièces de Claude retirées, " + stripped + " formes enlevées aux objets du code" +
                   (side.minutes > 0 ? " ; " + side.minutes / 60 + " h " + (side.minutes % 60).ToString("00") + " de travail dans Blender" : "") + ".";
        }

        // ---------------------------------------------------------------- les mouvements

        // Pour chaque mouvement : un objet vide au centre des pièces (leurs boîtes), qui les porte et tourne (ModelSpin) ; ou,
        // « vise », chaque pièce qui se lève autour de son origine selon la distance de la cible (BarrelPitch), avec ses
        // suiveurs (des objets du code, par leur repère).
        static int ApplyMovements(Transform rt, string name, string fbxPath, Dictionary<string, Transform> resolved, List<string> warnings)
        {
            var path = k_Movements + name + ".json";
            if (!File.Exists(path))
                return 0;
            Mouvements data;
            try
            {
                data = JsonUtility.FromJson<Mouvements>(File.ReadAllText(path, Encoding.UTF8));
            }
            catch (Exception e)
            {
                warnings.Add("les mouvements (" + path + ") sont illisibles : " + e.Message);
                return 0;
            }
            if (data?.mouvements == null)
                return 0;
            var all = rt.GetComponentsInChildren<Transform>(true);
            var count = 0;
            foreach (var m in data.mouvements)
            {
                var pieces = new List<Transform>();
                foreach (var n in m.pieces ?? Array.Empty<string>())
                {
                    var t = all.FirstOrDefault(x => x != null && x.name == n && x.GetComponent<MeshFilter>() != null &&
                                                    IsMine(x.GetComponent<MeshFilter>().sharedMesh, fbxPath, name));
                    if (t == null)
                        warnings.Add("mouvement « " + m.nom + " » : la pièce « " + n + " » est introuvable (renommée dans Blender ?).");
                    else
                        pieces.Add(t);
                }
                if (pieces.Count == 0)
                    continue;
                if (m.vise != null && m.vise.Length == 3)
                {
                    var followers = new List<Transform>();
                    foreach (var s in m.suiveurs ?? Array.Empty<string>())
                    {
                        if (resolved.TryGetValue(s, out var f) && f != null)
                            followers.Add(f);
                        else
                            warnings.Add("mouvement « " + m.nom + " » : le repère « " + s + " » est introuvable.");
                    }
                    foreach (var p in pieces)
                    {
                        var pitch = p.gameObject.AddComponent<Archery.Building.BarrelPitch>();
                        // chaque tube emmène les bouches les plus proches de lui (une par tube, en général)
                        var mine = followers.Where(f => pieces.OrderBy(q => (q.position - f.position).sqrMagnitude).First() == p).ToArray();
                        pitch.Configure(m.vise[0], m.vise[1], m.vise[2], mine);
                    }
                    count++;
                    continue;
                }
                var bounds = new Bounds();
                var first = true;
                foreach (var p in pieces)
                {
                    foreach (var r in p.GetComponentsInChildren<Renderer>(true))
                    {
                        if (first)
                            bounds = r.bounds;
                        else
                            bounds.Encapsulate(r.bounds);
                        first = false;
                    }
                }
                var parent = pieces[0].parent;
                var pivot = new GameObject(string.IsNullOrEmpty(m.nom) ? "Mouvement" : m.nom).transform;
                pivot.SetParent(parent, false);
                pivot.position = bounds.center;
                foreach (var p in pieces)
                    p.SetParent(pivot, true);
                var spin = pivot.gameObject.AddComponent<Archery.Building.ModelSpin>();
                spin.DegreesPerSecond = m.tourne;
                if (m.axe != null && m.axe.Length == 3)
                    spin.Axis = new Vector3(m.axe[0], m.axe[1], m.axe[2]);
                count++;
            }
            return count;
        }

        // ---------------------------------------------------------------- les objets du code

        static void CollectKept(GameObject root, HashSet<Transform> raw, HashSet<Transform> canon)
        {
            var rt = root.transform;
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                    continue;
                if (behaviour.transform != rt)
                {
                    raw.Add(behaviour.transform);
                    canon.Add(Canon(behaviour.transform));
                }
                var so = new SerializedObject(behaviour);
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType != SerializedPropertyType.ObjectReference)
                        continue;
                    switch (it.name)
                    {
                        case "m_Script":
                        case "m_GameObject":
                        case "m_CorrespondingSourceObject":
                        case "m_PrefabInstance":
                        case "m_PrefabAsset":
                            continue;
                    }
                    var o = it.objectReferenceValue;
                    var t = o is GameObject g ? g.transform : o is Component c ? c.transform : null;
                    if (t == null || t == rt || !t.IsChildOf(rt))
                        continue;
                    raw.Add(t);
                    canon.Add(Canon(t));
                }
            }
        }

        // Un objet dans un modèle imbriqué compte pour le modèle entier (comme reperes.py).
        static Transform Canon(Transform t)
        {
            var outer = PrefabUtility.IsPartOfPrefabInstance(t) ? PrefabUtility.GetOutermostPrefabInstanceRoot(t.gameObject) : null;
            return outer != null ? outer.transform : t;
        }

        static void AddWithParents(Transform t, Transform root, HashSet<Transform> set)
        {
            for (var p = t; p != null && p != root; p = p.parent)
                set.Add(p);
        }

        static Transform Resolve(Transform root, Segment[] path, HashSet<Transform> keptPath)
        {
            var t = root;
            if (path == null)
                return null;
            foreach (var seg in path)
            {
                var same = new List<Transform>();
                foreach (Transform c in t)
                    if (c.name == seg.nom && keptPath.Contains(c))
                        same.Add(c);
                if (seg.rang < 0 || seg.rang >= same.Count)
                    return null;
                t = same[seg.rang];
            }
            return t;
        }

        // L'objet appartient-il à u, sans autre objet du code entre les deux ?
        static bool OwnedBy(Transform t, Transform u, HashSet<Transform> stay)
        {
            for (var p = t; p != null; p = p.parent)
            {
                if (p == u)
                    return true;
                if (stay.Contains(p))
                    return false;
            }
            return false;
        }

        static bool OnlyGeometry(Transform t)
        {
            foreach (var c in t.GetComponents<Component>())
                if (c != null && !(c is Transform) && !(c is MeshFilter) && !(c is MeshRenderer))
                    return false;
            return true;
        }

        static void StripGeometry(GameObject go)
        {
            var r = go.GetComponent<MeshRenderer>();
            if (r != null)
                Object.DestroyImmediate(r);
            var f = go.GetComponent<MeshFilter>();
            if (f != null)
                Object.DestroyImmediate(f);
        }

        static bool IsMine(Mesh mesh, string fbxPath, string name)
        {
            if (mesh == null)
                return false;
            var path = AssetDatabase.GetAssetPath(mesh);
            return path == fbxPath || path.StartsWith(k_Fusions + "/" + name + "_", StringComparison.Ordinal);
        }

        static int Depth(Transform t)
        {
            var d = 0;
            for (var p = t; p != null; p = p.parent)
                d++;
            return d;
        }

        static List<Transform> Children(Transform t)
        {
            var list = new List<Transform>();
            foreach (Transform c in t)
                list.Add(c);
            return list;
        }

        static void Align(Transform u, Transform c)
        {
            u.SetPositionAndRotation(c.position, c.rotation);
            var want = c.lossyScale;
            var p = u.parent != null ? u.parent.lossyScale : Vector3.one;
            u.localScale = new Vector3(Div(want.x, p.x), Div(want.y, p.y), Div(want.z, p.z));
        }

        static float Div(float a, float b) => Mathf.Abs(b) > 1e-6f ? a / b : a;

        // Blender (Z en haut, droitier) -> Unity (Y en haut, gaucher) : x = -x, y = z, z = -y (l'aller-retour vérifié le 14
        // novembre) ; une matrice du monde de Blender (16 nombres, ligne par ligne) devient C · M · C⁻¹.
        static readonly Matrix4x4 k_BlenderToUnity = new Matrix4x4(
            new Vector4(-1f, 0f, 0f, 0f), new Vector4(0f, 0f, -1f, 0f), new Vector4(0f, 1f, 0f, 0f), new Vector4(0f, 0f, 0f, 1f));

        static Matrix4x4 BlenderToUnity(float[] m)
        {
            var b = new Matrix4x4();
            for (var r = 0; r < 4; r++)
                for (var c = 0; c < 4; c++)
                    b[r, c] = m[r * 4 + c];
            return k_BlenderToUnity * b * k_BlenderToUnity.inverse;
        }

        static void SetFromBlender(Transform t, Matrix4x4 world)
        {
            Vector3 x = world.GetColumn(0), y = world.GetColumn(1), z = world.GetColumn(2);
            var scale = new Vector3(x.magnitude, y.magnitude, z.magnitude);
            if (scale.x < 1e-6f || scale.y < 1e-6f || scale.z < 1e-6f)
                return;
            t.SetPositionAndRotation(world.GetColumn(3), Quaternion.LookRotation(z / scale.z, y / scale.y));
            var p = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(Div(scale.x, p.x), Div(scale.y, p.y), Div(scale.z, p.z));
        }

        // ---------------------------------------------------------------- les maillages fondus

        static Mesh Combine(List<MeshRenderer> parts, Transform u, out Material[] materials)
        {
            var byMaterial = new Dictionary<Material, List<CombineInstance>>();
            var order = new List<Material>();
            var toLocal = u.worldToLocalMatrix;
            foreach (var r in parts)
            {
                var mesh = r.GetComponent<MeshFilter>().sharedMesh;
                var mats = r.sharedMaterials;
                for (var i = 0; i < mesh.subMeshCount; i++)
                {
                    var m = mats.Length > 0 ? mats[Mathf.Min(i, mats.Length - 1)] : null;
                    if (m == null)
                        continue;
                    if (!byMaterial.TryGetValue(m, out var list))
                    {
                        byMaterial[m] = list = new List<CombineInstance>();
                        order.Add(m);
                    }
                    list.Add(new CombineInstance { mesh = mesh, subMeshIndex = i, transform = toLocal * r.transform.localToWorldMatrix });
                }
            }
            var subs = new List<CombineInstance>();
            var temps = new List<Mesh>();
            foreach (var m in order)
            {
                var part = new Mesh { indexFormat = IndexFormat.UInt32 };
                part.CombineMeshes(byMaterial[m].ToArray(), true, true);
                subs.Add(new CombineInstance { mesh = part, transform = Matrix4x4.identity });
                temps.Add(part);
            }
            var result = new Mesh { indexFormat = IndexFormat.UInt32 };
            result.CombineMeshes(subs.ToArray(), false, false);
            result.RecalculateBounds();
            foreach (var t in temps)
                Object.DestroyImmediate(t);
            materials = order.ToArray();
            return result;
        }

        static Mesh SaveMesh(Mesh mesh, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);   // le même asset (son GUID) : les références suivent
            Object.DestroyImmediate(mesh);
            return existing;
        }

        // ---------------------------------------------------------------- les couleurs

        static Material[] Map(Material[] source, Dictionary<string, Material> cache)
        {
            var result = new Material[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                var m = source[i];
                if (m == null)
                    continue;
                // « Kenney TD.001 » : la copie que Blender fait à chaque pièce glissée de la bibliothèque, le même matériau
                var name = System.Text.RegularExpressions.Regex.Replace(m.name, @"\.\d{3}$", "");
                if (!cache.TryGetValue(name, out var shared))
                    cache[name] = shared = Shared(m, name);
                result[i] = shared;
            }
            return result;
        }

        // Un matériau par nom de Blender, partagé : la couleur suit Blender à chaque export ; le reste (brillance…) se règle dans
        // Unity et reste. Les règles des noms : « Kenney TD »… prend la texture du kit ; un nom qui contient « Lueur » luit ; un
        // nom qui commence par « Transparent » est transparent (le joueur, 14 novembre, pour la glace ; sur le modèle de
        // Art/TowerDefense/Glace.mat), à l'Alpha réglé dans Blender (0,55 s'il ne l'a pas réglé) ; « Transparent lueur glace »
        // fait les deux.
        static Material Shared(Material source, string name)
        {
            var path = k_Materials + "/" + FileName(name) + ".mat";
            EnsureFolder(k_Materials);
            var transparent = name.StartsWith("Transparent", StringComparison.OrdinalIgnoreCase);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var created = mat == null;
            if (created)
            {
                var template = AssetDatabase.LoadAssetAtPath<Material>(transparent ? k_TransparentTemplate : k_Template);
                mat = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.name = FileName(name);
            }
            else if (transparent != IsTransparent(mat))
                CopySurface(mat, AssetDatabase.LoadAssetAtPath<Material>(transparent ? k_TransparentTemplate : k_Template));
            var color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.HasProperty("_Color") ? source.color : Color.gray;
            // l'Alpha de Blender arrive dans l'alpha de la couleur (l'import des FBX par URP le lit)
            color.a = transparent ? (color.a < 0.99f ? color.a : 0.55f) : 1f;
            Texture texture = null;
            if (k_KitTextures.TryGetValue(name, out var kit))
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(kit);
                color = new Color(1f, 1f, 1f, color.a);
            }
            else if (source.HasProperty("_BaseMap"))
                texture = source.GetTexture("_BaseMap");
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", texture);
            if (name.IndexOf("Lueur", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 2f);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (created)
                AssetDatabase.CreateAsset(mat, path);
            else
                EditorUtility.SetDirty(mat);
            return mat;
        }

        static bool IsTransparent(Material m) => m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f;

        // Passer un matériau d'opaque à transparent (ou l'inverse) : les réglages de surface du modèle (blend, file d'attente,
        // mots-clés, passes), la couleur et la texture du matériau sont remises ensuite par Shared.
        static void CopySurface(Material mat, Material template)
        {
            if (template == null)
                return;
            mat.CopyPropertiesFromMaterial(template);
            mat.shaderKeywords = template.shaderKeywords;
            mat.renderQueue = template.renderQueue;
            mat.SetOverrideTag("RenderType", template.GetTag("RenderType", false, ""));
            foreach (var pass in new[] { "DepthOnly", "ShadowCaster", "DepthNormals", "MotionVectors" })
                mat.SetShaderPassEnabled(pass, template.GetShaderPassEnabled(pass));
        }

        // Un nom de fichier sans accents ni caractères interdits (CLAUDE.md : pas d'accents dans les noms d'assets).
        static string FileName(string name)
        {
            var d = name.Replace("œ", "oe").Replace("Œ", "Oe").Replace("æ", "ae").Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in d)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;
                sb.Append(ch < 128 && (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-' || ch == '_') ? ch : '_');
            }
            var s = sb.ToString().Trim();
            return s.Length > 0 ? s : "Sans nom";
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
