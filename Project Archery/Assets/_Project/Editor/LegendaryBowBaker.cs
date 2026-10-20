using System.Linq;
using Archery.Bows;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Assemble le modèle de l'arc légendaire : « Bowblade – DAE WeaponCraft – WoW Styled Bow » de Tijmen Matthys
    /// (Sketchfab, CC BY 4.0), converti en <c>Art/Bows/Legendaire/Arc_Bowblade.obj</c> sans sa corde (le jeu dessine la
    /// sienne), déjà debout, centré sur la poignée, la corde vers l'archer (-Z), 1,15 m de haut. Le préfab
    /// <c>Prefabs/Bow Visual Legendaire</c> est fait : le modèle avec sa texture peinte (matériau <c>Arc Legendaire</c>, un peu
    /// lumineux), et les points du <see cref="BowVisual"/> (attaches de la corde aux boucles des pointes, repose-flèche juste
    /// au-dessus de la poignée, poignée). L'arc légendaire (<c>Data/BowClasses/Bow_Legendaire</c>) le prend.
    /// </summary>
    /// <remarks>
    /// Se fait tout seul une fois, quand le modèle est importé (tant que le préfab de l'arc n'a pas l'objet « Arc Bowblade ») ;
    /// ensuite, menu <c>Archery > Arc légendaire > Refaire le modèle</c>.
    /// </remarks>
    [InitializeOnLoad]
    static class LegendaryBowBaker
    {
        const string k_Model = "Assets/_Project/Art/Bows/Legendaire/Arc_Bowblade.obj";
        const string k_Prefab = "Assets/_Project/Prefabs/Bow Visual Legendaire.prefab";
        const string k_Bow = "Assets/_Project/Data/BowClasses/Bow_Legendaire.asset";
        const string k_Material = "Assets/_Project/Art/Bows/Arc Legendaire.mat";
        const string k_ModelName = "Arc Bowblade";

        // Mesurés à la conversion, dans le repère d'Unity (qui retourne l'axe X des OBJ) : les bouts de la corde retirée,
        // et la flèche juste au-dessus de la poignée (son haut est à 7,4 cm de son milieu).
        static readonly Vector3 k_StringTop = new Vector3(0f, 0.43f, -0.2236f);
        static readonly Vector3 k_StringBottom = new Vector3(0f, -0.4264f, -0.2244f);
        static readonly Vector3 k_ArrowRest = new Vector3(0f, 0.075f, 0f);

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

        static LegendaryBowBaker()
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

        [MenuItem("Archery/Arc légendaire/Refaire le modèle")]
        static void Rebuild()
        {
            var bow = AssetDatabase.LoadAssetAtPath<BowDefinition>(k_Bow);
            if (bow == null)
                Debug.LogWarning($"Arc légendaire : {k_Bow} introuvable.");
            else
                Build(bow);
        }

        static void Build(BowDefinition bow)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(k_Model);
            var source = model != null ? model.GetComponentInChildren<MeshFilter>() : null;
            if (source == null || source.sharedMesh == null)
            {
                Debug.LogWarning($"Arc légendaire : pas de maillage dans {k_Model}.");
                return;
            }

            var root = new GameObject("Bow Visual Legendaire");
            var visual = root.AddComponent<BowVisual>();

            // Le modèle est déjà en place : on garde seulement les transformations de l'import.
            var toModel = model.transform.worldToLocalMatrix * source.transform.localToWorldMatrix;
            var shape = new GameObject(k_ModelName);
            shape.transform.SetParent(root.transform, false);
            shape.transform.localPosition = toModel.GetPosition();
            shape.transform.localRotation = toModel.rotation;
            shape.transform.localScale = toModel.lossyScale;
            shape.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
            var material = AssetDatabase.LoadAssetAtPath<Material>(k_Material);
            shape.AddComponent<MeshRenderer>().sharedMaterials =
                Enumerable.Repeat(material, Mathf.Max(1, source.sharedMesh.subMeshCount)).ToArray();

            var stringTop = Point(root, "String Top", k_StringTop);
            var stringBottom = Point(root, "String Bottom", k_StringBottom);
            var rest = Point(root, "Arrow Rest", k_ArrowRest);
            var grip = Point(root, "Grip", Vector3.zero);

            var settings = new SerializedObject(visual);
            settings.FindProperty("m_StringTop").objectReferenceValue = stringTop;
            settings.FindProperty("m_StringBottom").objectReferenceValue = stringBottom;
            settings.FindProperty("m_ArrowRest").objectReferenceValue = rest;
            settings.FindProperty("m_Grip").objectReferenceValue = grip;
            settings.FindProperty("m_StringEndOffset").vector3Value = Vector3.zero;
            settings.FindProperty("m_ArrowSideOffset").floatValue = 0.03f;
            settings.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, k_Prefab);
            Object.DestroyImmediate(root);
            if (prefab == null)
            {
                Debug.LogWarning($"Arc légendaire : impossible d'enregistrer {k_Prefab}.");
                return;
            }

            bow.visualPrefab = prefab.GetComponent<BowVisual>();
            EditorUtility.SetDirty(bow);
            AssetDatabase.SaveAssets();
            Debug.Log($"Arc légendaire : modèle « Bowblade » (Tijmen Matthys, CC BY 4.0) assemblé dans {k_Prefab}.", prefab);
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
