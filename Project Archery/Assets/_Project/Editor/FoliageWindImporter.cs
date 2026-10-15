using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le vent dans la végétation : à l'import des arbres, herbes, fleurs et plantes du kit Nature de Kenney, chaque sommet
    /// reçoit dans sa couleur sa hauteur dans le modèle (rouge : 0 au pied, 1 en haut ; vert : taille du modèle ÷ 16 m),
    /// et leurs matériaux passent au shader Archery/Foliage, qui les fait onduler au vent (réglé par Archery.World.Wind).
    /// </summary>
    /// <remarks>
    /// Rien à changer dans les scènes : les objets posés gardent leurs modèles, même statiques (fusionnés au lancement, ils
    /// gardent la couleur de leurs sommets). Le sol herbu, les rochers, les champignons et les souches ne bougent pas.
    /// </remarks>
    public class FoliageWindImporter : AssetPostprocessor
    {
        const string k_ShaderPath = "Assets/_Project/Shaders/Foliage.shader";
        const string k_ShaderName = "Archery/Foliage";
        const string k_ModelsFolder = "Assets/kenney_nature-kit";
        const string k_CheckedKey = "Archery.FoliageWindChecked";

        static readonly string[] k_Prefixes = { "tree_", "grass", "flower_", "plant_" };

        // Augmenter ce numéro refait l'import des modèles (après un changement de ce script).
        public override uint GetVersion() => 2;

        // Après celui d'URP (-980), qui fait de chaque matériau du modèle un Lit et y recopie ses couleurs.
        public override int GetPostprocessOrder() => 100;

        /// <summary>Un modèle de végétation du kit Nature : arbre, herbe, fleur ou plante (pas le sol herbu, « ground_grass »).</summary>
        public static bool IsFoliage(string path)
        {
            path = path.Replace('\\', '/');
            if (!path.StartsWith(k_ModelsFolder + "/", StringComparison.OrdinalIgnoreCase))
                return false;

            var name = Path.GetFileNameWithoutExtension(path);
            foreach (var prefix in k_Prefixes)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        void OnPostprocessModel(GameObject root)
        {
            if (IsFoliage(assetPath))
                WriteHeights(root);
        }

        // Les matériaux du modèle sont créés d'après sa description (comme le fait URP) : une fois URP passé, le matériau
        // garde ses couleurs et ses réglages de Lit, seul le shader change.
        void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            if (!IsFoliage(assetPath))
                return;

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(k_ShaderPath);
            if (shader != null)
                material.shader = shader;
        }

        // Hauteur de chaque sommet dans le modèle entier : le pied et le haut sont communs à toutes ses pièces (tronc et
        // feuillage bougent ensemble).
        static void WriteHeights(GameObject root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var bottom = float.MaxValue;
            var top = float.MinValue;
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null)
                    continue;

                var toWorld = filter.transform.localToWorldMatrix;
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    var y = toWorld.MultiplyPoint3x4(vertex).y;
                    bottom = Mathf.Min(bottom, y);
                    top = Mathf.Max(top, y);
                }
            }

            var height = top - bottom;
            if (height <= 1e-4f)
                return;

            var size = Mathf.Clamp01(height / 16f);
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;

                var toWorld = filter.transform.localToWorldMatrix;
                var vertices = mesh.vertices;
                var colors = new Color[vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                {
                    var y = toWorld.MultiplyPoint3x4(vertices[i]).y;
                    colors[i] = new Color(Mathf.Clamp01((y - bottom) / height), size, 0f, 1f);
                }

                mesh.colors = colors;
            }
        }

        // Si le shader est arrivé après les modèles (premier import), les modèles de végétation dont les matériaux ne
        // l'utilisent pas encore sont réimportés : une fois par session de l'éditeur, et à chaque import du shader.
        [InitializeOnLoadMethod]
        static void CheckOncePerSession()
        {
            if (SessionState.GetBool(k_CheckedKey, false))
                return;

            SessionState.SetBool(k_CheckedKey, true);
            EditorApplication.delayCall += ReimportMissing;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (Array.IndexOf(importedAssets, k_ShaderPath) >= 0)
                EditorApplication.delayCall += ReimportMissing;
        }

        static void ReimportMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<Shader>(k_ShaderPath) == null)
                return;

            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { k_ModelsFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!IsFoliage(path))
                    continue;

                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || material.shader == null || material.shader.name != k_ShaderName)
                    paths.Add(path);
            }

            if (paths.Count == 0)
                return;

            Debug.Log($"Vent dans la végétation : {paths.Count} modèle(s) du kit Nature réimporté(s) avec le shader {k_ShaderName}.");
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in paths)
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}
