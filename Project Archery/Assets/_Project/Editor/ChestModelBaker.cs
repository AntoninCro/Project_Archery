using Archery.Chests;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le nouveau coffre (27 octobre, choisi par le joueur) : le coffre low-poly de Quaternius (pack LowPoly Modular Dungeon,
    /// CC0, <c>Art/Chests/Chest_Quaternius.fbx</c>) remplace celui d'Animated PBR Chest Demo dans le prefab <c>Chest</c>
    /// (coffres de vague et coffre de boss, qui n'en est qu'une version plus grande). Une seule fois, tant que le prefab n'a pas
    /// de <see cref="ChestMotion"/>.
    /// </summary>
    /// <remarks>
    /// Le modèle est tourné pour que sa serrure regarde vers l'avant du coffre (+Z, vers le joueur : <see cref="ChestSpawner"/>),
    /// mis à la largeur de l'ancien (0,95 m) et posé au sol. Son couvercle passe sous une charnière posée sur son arête arrière :
    /// sans Animator, <see cref="ChestLid"/> le fait tourner lui-même autour de l'axe X de la charnière. Le Chest Lid de l'ancien
    /// coffre (ses réglages) et sa lueur d'ouverture sont gardés ; les colliders suivent le nouveau modèle. Le modèle et la charnière
    /// vont sous « Visual », que <see cref="ChestMotion"/> fait tomber, sautiller et s'écraser, comme les animations de l'ancien
    /// coffre. Les matériaux reprennent les couleurs du modèle, dans <c>Art/Chests</c>, pour les retoucher à la main.
    /// </remarks>
    [InitializeOnLoad]
    static class ChestModelBaker
    {
        const string k_PrefabPath = "Assets/_Project/Prefabs/Chest.prefab";
        const string k_ModelPath = "Assets/_Project/Art/Chests/Chest_Quaternius.fbx";
        const string k_ArtFolder = "Assets/_Project/Art/Chests";
        const string k_HingeName = "Lid Hinge";

        // La largeur de l'ancien coffre (m), pour que le nouveau prenne la même place au sol.
        const float k_Width = 0.95f;

        // Les couleurs du modèle (fichier MTL du pack, linéaires), passées en sRGB pour le matériau.
        static readonly Color k_Wood = new Color(0.44f, 0.32f, 0.28f);
        static readonly Color k_DarkWood = new Color(0.34f, 0.25f, 0.22f);
        static readonly Color k_Metal = new Color(0.28f, 0.26f, 0.34f);

        static ChestModelBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabPath);
            if (prefab == null || prefab.GetComponentInChildren<ChestMotion>(true) != null)
                return;

            // Pas encore importé (Unity n'a pas repris le focus) : ce sera au prochain rechargement.
            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(k_ModelPath);
            if (modelAsset == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(k_PrefabPath);
            try
            {
                var report = Rebuild(root, modelAsset);
                if (report == null)
                    return;

                PrefabUtility.SaveAsPrefabAsset(root, k_PrefabPath);
                Debug.Log("Nouveau coffre (27 octobre) : le prefab Chest prend le coffre de Quaternius (CC0), " + report +
                          ". L'ancien modèle (Animated PBR Chest Demo) est retiré ; de ce pack, le coffre ne garde que la lueur " +
                          "d'ouverture.", prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Rend ce qui a été fait, ou null si rien ne doit être enregistré.
        static string Rebuild(GameObject root, GameObject modelAsset)
        {
            var chest = root.GetComponent<Chest>();
            var lid = root.GetComponentInChildren<ChestLid>(true);
            if (chest == null || lid == null)
            {
                Debug.LogWarning("Nouveau coffre : le prefab Chest n'a pas de Chest ou de Chest Lid, il n'est pas changé.");
                return null;
            }

            var openEffect = new SerializedObject(chest).FindProperty("m_OpenEffect").objectReferenceValue as GameObject;

            // On sort de l'ancien modèle ce qu'on garde : le Chest Lid (le coffre le référence) et la lueur d'ouverture.
            var oldModel = root.transform.Find("Model");
            lid.transform.SetParent(root.transform, false);
            if (openEffect != null)
                openEffect.transform.SetParent(root.transform, true);
            if (oldModel != null)
                Object.DestroyImmediate(oldModel.gameObject);

            // Dépaqueté : son couvercle doit passer sous la charnière.
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Model";
            model.transform.SetSiblingIndex(0);
            var animator = model.GetComponent<Animator>();
            if (animator != null)
                Object.DestroyImmediate(animator);

            MeshRenderer caseRenderer = null, lidRenderer = null;
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.name.Contains("Top"))
                    lidRenderer = renderer;
                else if (renderer.name.Contains("Base"))
                    caseRenderer = renderer;
            }

            if (caseRenderer == null || lidRenderer == null)
            {
                Debug.LogWarning("Nouveau coffre : le modèle n'a pas de Chest_Base ou de Chest_Top, le prefab Chest n'est pas changé.");
                return null;
            }

            // La serrure vers l'avant (+Z).
            var front = LockDirection(root.transform, caseRenderer);
            if (front == Vector3.zero)
                Debug.LogWarning("Nouveau coffre : serrure introuvable (matériau « Metal »), le modèle garde son orientation.");
            else
                model.transform.localRotation =
                    Quaternion.AngleAxis(Vector3.SignedAngle(front, Vector3.forward, Vector3.up), Vector3.up) * model.transform.localRotation;

            // À la largeur de l'ancien coffre, centré, posé au sol.
            var all = ModelBounds(root.transform, caseRenderer, lidRenderer);
            var scale = k_Width / all.size.x;
            model.transform.localScale *= scale;
            all = ModelBounds(root.transform, caseRenderer, lidRenderer);
            model.transform.localPosition -= new Vector3(all.center.x, all.min.y, all.center.z);

            // La charnière sur l'arête arrière du couvercle, en bas ; le Chest Lid et le couvercle dessous.
            var lidBounds = LocalBounds(root.transform, lidRenderer);
            var hinge = new GameObject(k_HingeName).transform;
            hinge.SetParent(root.transform, false);
            hinge.SetSiblingIndex(1);
            hinge.localPosition = new Vector3(lidBounds.center.x, lidBounds.min.y, lidBounds.min.z);

            lid.name = "Lid";
            lid.transform.SetParent(hinge, false);
            lid.transform.localPosition = Vector3.zero;
            lid.transform.localRotation = Quaternion.identity;
            lid.transform.localScale = Vector3.one;
            lidRenderer.transform.SetParent(lid.transform, true);

            var lidCollider = lid.GetComponent<BoxCollider>();
            if (lidCollider == null)
                lidCollider = lid.gameObject.AddComponent<BoxCollider>();
            lidCollider.center = lidBounds.center - hinge.localPosition;
            lidCollider.size = lidBounds.size;

            var lidData = new SerializedObject(lid);
            lidData.FindProperty("m_Animator").objectReferenceValue = null;
            lidData.FindProperty("m_Hinge").objectReferenceValue = null;
            lidData.ApplyModifiedPropertiesWithoutUndo();

            // La caisse.
            var caseBounds = LocalBounds(root.transform, caseRenderer);
            var caseCollider = root.GetComponent<BoxCollider>();
            if (caseCollider == null)
                caseCollider = root.AddComponent<BoxCollider>();
            caseCollider.center = caseBounds.center;
            caseCollider.size = caseBounds.size;

            // La lueur d'ouverture dans la caisse, près du bord, comme avant.
            if (openEffect != null)
                openEffect.transform.localPosition =
                    new Vector3(caseBounds.center.x, caseBounds.min.y + 0.75f * caseBounds.size.y, caseBounds.center.z);

            // Le dessin (modèle et couvercle) sous un même objet, à l'origine du coffre : ChestMotion le fait bouger, pas les
            // colliders.
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            visual.SetSiblingIndex(0);
            model.transform.SetParent(visual, false);
            hinge.SetParent(visual, false);
            visual.gameObject.AddComponent<ChestMotion>();

            foreach (var renderer in new[] { caseRenderer, lidRenderer })
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                    materials[i] = MaterialFor(materials[i] != null ? materials[i].name : "");
                renderer.sharedMaterials = materials;
            }

            all = ModelBounds(root.transform, caseRenderer, lidRenderer);
            return $"{all.size.x:0.00} × {all.size.y:0.00} × {all.size.z:0.00} m (échelle {scale:0.###}), couvercle ouvert par Chest Lid " +
                   $"autour d'une charnière à {hinge.localPosition.y:0.00} m de haut";
        }

        // Direction (horizontale, dans le repère du coffre) du centre de la caisse vers la serrure : la partie « Metal » de la caisse.
        static Vector3 LockDirection(Transform space, MeshRenderer caseRenderer)
        {
            var mesh = caseRenderer.GetComponent<MeshFilter>().sharedMesh;
            var materials = caseRenderer.sharedMaterials;
            for (var i = 0; i < materials.Length && i < mesh.subMeshCount; i++)
            {
                if (materials[i] == null || !materials[i].name.Contains("Metal"))
                    continue;

                var lockCenter = space.InverseTransformPoint(caseRenderer.transform.TransformPoint(mesh.GetSubMesh(i).bounds.center));
                var direction = lockCenter - LocalBounds(space, caseRenderer).center;
                direction.y = 0f;
                return direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.zero;
            }

            return Vector3.zero;
        }

        static Bounds ModelBounds(Transform space, MeshRenderer caseRenderer, MeshRenderer lidRenderer)
        {
            var bounds = LocalBounds(space, caseRenderer);
            bounds.Encapsulate(LocalBounds(space, lidRenderer));
            return bounds;
        }

        // Boîte du maillage dans le repère « space » (les 8 coins de sa boîte à lui).
        static Bounds LocalBounds(Transform space, MeshRenderer renderer)
        {
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh.bounds;
            var toSpace = space.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            var bounds = new Bounds(toSpace.MultiplyPoint3x4(mesh.center), Vector3.zero);
            for (var i = 0; i < 8; i++)
            {
                var corner = mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                bounds.Encapsulate(toSpace.MultiplyPoint3x4(corner));
            }

            return bounds;
        }

        // Les trois matériaux du modèle : « DarkWood », « Wood » et « Metal ».
        static Material MaterialFor(string modelMaterial)
        {
            if (modelMaterial.Contains("Metal"))
                return LitMaterial("Coffre metal", k_Metal, 0.6f, 0.45f);
            if (modelMaterial.Contains("Dark"))
                return LitMaterial("Coffre bois fonce", k_DarkWood, 0f, 0.15f);
            return LitMaterial("Coffre bois", k_Wood, 0f, 0.15f);
        }

        static Material LitMaterial(string name, Color color, float metallic, float smoothness)
        {
            var path = $"{k_ArtFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
