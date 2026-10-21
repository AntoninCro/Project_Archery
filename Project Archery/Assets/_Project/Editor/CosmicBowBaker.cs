using System.Collections.Generic;
using System.Linq;
using Archery.Bows;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Assemble le modèle de l'arc cosmique : « Enchanted Bow » de Zsky (Poly Pizza, CC-BY 3.0), converti en
    /// <c>Art/Bows/Cosmique/Arc_Enchante.obj</c> sans sa corde (le jeu dessine la sienne). Le préfab
    /// <c>Prefabs/Bow Visual Cosmique</c> est refait : le modèle debout, la corde vers l'archer, la poignée au centre,
    /// 1,1 m de haut, avec ses matériaux (corps « fenêtre sur l'espace », ailettes violettes lumineuses, poignée sombre),
    /// et les points du <see cref="BowVisual"/> (attaches de la corde là où était celle du modèle, repose-flèche juste
    /// au-dessus de la main, poignée). L'arc cosmique (<c>Data/BowClasses/Bow_Cosmique</c>) le prend.
    /// </summary>
    /// <remarks>
    /// Se fait tout seul une fois, quand le modèle est importé (tant que le préfab n'a pas l'objet « Arc enchanté ») ;
    /// ensuite, menu <c>Archery > Arc cosmique > Refaire le modèle</c>.
    /// </remarks>
    [InitializeOnLoad]
    static class CosmicBowBaker
    {
        // Le modèle peut s'importer après le chargement de cet outil : on réessaie quand il arrive.
        sealed class ModelWatcher : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (System.Array.IndexOf(imported, k_Model) < 0)
                    return;
                EditorApplication.delayCall -= AutoBuild;
                EditorApplication.delayCall += AutoBuild;
            }
        }

        const string k_Model = "Assets/_Project/Art/Bows/Cosmique/Arc_Enchante.obj";
        const string k_Prefab = "Assets/_Project/Prefabs/Bow Visual Cosmique.prefab";
        const string k_Bow = "Assets/_Project/Data/BowClasses/Bow_Cosmique.asset";
        const string k_BodyMaterial = "Assets/_Project/Art/Bows/Arc Cosmique Corps.mat";
        const string k_FinMaterial = "Assets/_Project/Art/Bows/Bow Pack Glow Violet.mat";
        const string k_GripMaterial = "Assets/_Project/Art/Bows/Arc Cosmique Poignee.mat";
        const string k_ModelName = "Arc enchanté";

        // Hauteur (m) de l'arc, d'une pointe à l'autre, et hauteur (m) du repose-flèche au-dessus du milieu de la poignée :
        // comme les autres arcs, la flèche passe juste au-dessus de la main.
        const float k_Height = 1.1f;
        const float k_RestHeight = 0.065f;

        // Mesures du modèle d'origine (unités du glTF : 5,27 de haut, poignée centrée en hauteur) : la corde retirée allait
        // de 2,604 en haut à -2,607 en bas, à 0,6915 de la poignée, du côté où l'arc est le plus profond.
        const float k_SourceHeight = 5.27f;
        const float k_SourceStringTop = 2.604f;
        const float k_SourceStringBottom = -2.607f;
        const float k_SourceBrace = 0.6915f;

        static CosmicBowBaker()
        {
            EditorApplication.delayCall -= AutoBuild;
            EditorApplication.delayCall += AutoBuild;
        }

        static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var bow = AssetDatabase.LoadAssetAtPath<BowDefinition>(k_Bow);
            if (bow == null || AssetDatabase.LoadAssetAtPath<GameObject>(k_Model) == null)
                return;
            if (bow.visualPrefab != null && bow.visualPrefab.transform.Find(k_ModelName) != null)
                return;

            Build(bow);
        }

        [MenuItem("Archery/Arc cosmique/Refaire le modèle")]
        static void Rebuild()
        {
            var bow = AssetDatabase.LoadAssetAtPath<BowDefinition>(k_Bow);
            if (bow == null)
                Debug.LogWarning($"Arc cosmique : {k_Bow} introuvable.");
            else
                Build(bow);
        }

        static void Build(BowDefinition bow)
        {
            // Les sommets doivent rester lisibles pour trouver la poignée.
            if (AssetImporter.GetAtPath(k_Model) is ModelImporter importer && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(k_Model);
            var source = model != null ? model.GetComponentInChildren<MeshFilter>() : null;
            var renderer = source != null ? source.GetComponent<MeshRenderer>() : null;
            if (source == null || source.sharedMesh == null || renderer == null)
            {
                Debug.LogWarning($"Arc cosmique : pas de maillage dans {k_Model}.");
                return;
            }

            var mesh = source.sharedMesh;
            var names = renderer.sharedMaterials.Select(m => m != null ? m.name : "").ToArray();
            var body = SubMesh(names, "Corps");
            var fins = SubMesh(names, "Ailettes");
            var grip = SubMesh(names, "Poignee");
            if (body < 0 || grip < 0 || body >= mesh.subMeshCount || grip >= mesh.subMeshCount)
            {
                Debug.LogWarning("Arc cosmique : les parties du modèle (Corps, Poignee) sont introuvables : " + string.Join(", ", names));
                return;
            }

            // Les sommets dans le repère du modèle importé.
            var toModel = model.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
            var vertices = mesh.vertices.Select(v => toModel.MultiplyPoint3x4(v)).ToArray();
            var all = BoundsOf(vertices, Enumerable.Range(0, vertices.Length));
            var gripCenter = BoundsOf(vertices, mesh.GetTriangles(grip)).center;

            // La corde est du côté où l'arc est le plus profond (les pointes) ; elle va vers l'archer, l'arrière de l'arc
            // (-Z). L'import des OBJ par Unity retourne l'axe X : le côté se lit sur le maillage importé.
            var stringOnPlusX = all.max.x - gripCenter.x >= gripCenter.x - all.min.x;
            var rotation = Quaternion.Euler(0f, stringOnPlusX ? 90f : -90f, 0f);
            var scale = k_Height / Mathf.Max(0.01f, all.size.y);
            var unit = k_Height / k_SourceHeight;

            var root = new GameObject("Bow Visual Cosmique");
            var visual = root.AddComponent<BowVisual>();

            var shape = new GameObject(k_ModelName);
            shape.transform.SetParent(root.transform, false);
            shape.transform.localPosition = rotation * ((toModel.GetPosition() - gripCenter) * scale);
            shape.transform.localRotation = rotation * toModel.rotation;
            shape.transform.localScale = toModel.lossyScale * scale;
            shape.AddComponent<MeshFilter>().sharedMesh = mesh;
            var shapeRenderer = shape.AddComponent<MeshRenderer>();
            var materials = new Material[mesh.subMeshCount];
            for (var i = 0; i < materials.Length; i++)
                materials[i] = renderer.sharedMaterials.Length > i ? renderer.sharedMaterials[i] : null;
            materials[body] = AssetDatabase.LoadAssetAtPath<Material>(k_BodyMaterial);
            if (fins >= 0 && fins < materials.Length)
                materials[fins] = AssetDatabase.LoadAssetAtPath<Material>(k_FinMaterial);
            materials[grip] = AssetDatabase.LoadAssetAtPath<Material>(k_GripMaterial);
            shapeRenderer.sharedMaterials = materials;
            shapeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var stringTop = Point(root, "String Top", new Vector3(0f, k_SourceStringTop * unit, -k_SourceBrace * unit));
            var stringBottom = Point(root, "String Bottom", new Vector3(0f, k_SourceStringBottom * unit, -k_SourceBrace * unit));
            var rest = Point(root, "Arrow Rest", new Vector3(0f, k_RestHeight, 0f));
            var gripPoint = Point(root, "Grip", Vector3.zero);

            var settings = new SerializedObject(visual);
            settings.FindProperty("m_StringTop").objectReferenceValue = stringTop;
            settings.FindProperty("m_StringBottom").objectReferenceValue = stringBottom;
            settings.FindProperty("m_ArrowRest").objectReferenceValue = rest;
            settings.FindProperty("m_Grip").objectReferenceValue = gripPoint;
            settings.FindProperty("m_StringEndOffset").vector3Value = Vector3.zero;
            settings.FindProperty("m_ArrowSideOffset").floatValue = 0.03f;
            settings.ApplyModifiedPropertiesWithoutUndo();

            var brace = Vector3.Distance(rest.localPosition, (stringTop.localPosition + stringBottom.localPosition) * 0.5f);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, k_Prefab);
            Object.DestroyImmediate(root);
            if (prefab == null)
            {
                Debug.LogWarning($"Arc cosmique : impossible d'enregistrer {k_Prefab}.");
                return;
            }

            bow.visualPrefab = prefab.GetComponent<BowVisual>();
            EditorUtility.SetDirty(bow);
            AssetDatabase.SaveAssets();
            Debug.Log($"Arc cosmique : modèle « Enchanted Bow » (Zsky, CC-BY 3.0) assemblé dans {k_Prefab} " +
                      $"({k_Height:0.##} m de haut, corde à {brace * 100f:0} cm de la poignée).", prefab);
        }

        static int SubMesh(string[] names, string part)
        {
            for (var i = 0; i < names.Length; i++)
            {
                if (names[i].StartsWith(part))
                    return i;
            }

            return -1;
        }

        static Bounds BoundsOf(Vector3[] vertices, IEnumerable<int> indices)
        {
            var first = true;
            var bounds = new Bounds();
            foreach (var index in indices)
            {
                if (first)
                {
                    bounds = new Bounds(vertices[index], Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(vertices[index]);
                }
            }

            return bounds;
        }

        static Transform Point(GameObject root, string name, Vector3 position)
        {
            var point = new GameObject(name).transform;
            point.SetParent(root.transform, false);
            point.localPosition = position;
            return point;
        }
    }
}
