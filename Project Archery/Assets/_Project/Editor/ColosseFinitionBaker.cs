using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// La finition du Colosse de forge (2 novembre au soir, après l'essai du joueur : « plus texturé, plus lumineux » ;
    /// <c>Docs/Refonte_TowerDefense.md</c>, 12.47) : ses matériaux (<c>Art/TowerDefense/Palettes/Ultime_Colosse_*</c>, faits par
    /// <see cref="TowerDefenseLookBaker"/>, couleurs unies) deviennent du métal. Le fer est martelé (la couleur et le relief de
    /// <c>Tools/texture_fer_martele.py</c> : <c>Art/TowerDefense/Colosse_Fer.png</c> et <c>Colosse_Fer_Relief.png</c>, importé en carte
    /// de normales), métallique et un peu poli ; le bronze et l'or brillent, l'or luit un peu dans le noir.
    /// </summary>
    /// <remarks>
    /// Chaque matériau une seule fois : s'il n'a pas encore de relief (ou, sans relief, s'il est encore mat). Un tour d'éditeur après
    /// le baker de l'allure, qui peut créer des matériaux au même tour.
    /// </remarks>
    [InitializeOnLoad]
    static class ColosseFinitionBaker
    {
        const string k_Palettes = "Assets/_Project/Art/TowerDefense/Palettes";
        const string k_Prefix = "Ultime_Colosse_";
        const string k_Albedo = "Assets/_Project/Art/TowerDefense/Colosse_Fer.png";
        const string k_Relief = "Assets/_Project/Art/TowerDefense/Colosse_Fer_Relief.png";

        // Par rôle de la palette : la texture du fer (sa couleur), la force du relief, le métal, le poli, la lueur.
        static readonly Dictionary<string, (bool Albedo, float Bump, float Metallic, float Smoothness, float Glow)> k_Roles =
            new Dictionary<string, (bool, float, float, float, float)>
            {
                { "fer", (true, 1f, 0.7f, 0.42f, 0f) },
                { "fer_clair", (true, 1f, 0.75f, 0.48f, 0f) },
                { "metal", (true, 1f, 0.7f, 0.42f, 0f) },
                { "bronze", (true, 0.5f, 0.85f, 0.58f, 0f) },
                { "accent", (false, 0.3f, 1f, 0.75f, 0.12f) },
                { "cuir", (true, 0.6f, 0f, 0.3f, 0f) },
                { "bois", (false, 0f, 0f, 0.22f, 0f) },
                { "pierre", (true, 0.8f, 0f, 0.15f, 0f) },
                { "cuivre", (true, 0.5f, 0.9f, 0.55f, 0f) },
            };

        static ColosseFinitionBaker()
        {
            EditorApplication.delayCall -= Later;
            EditorApplication.delayCall += Later;
        }

        static void Later()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer || !Directory.Exists(k_Palettes))
                return;

            var log = new List<string>();
            var albedo = Texture(k_Albedo, false, log);
            var relief = Texture(k_Relief, true, log);
            if (albedo == null || relief == null)
                return;

            foreach (var file in Directory.GetFiles(k_Palettes, k_Prefix + "*.mat"))
            {
                var path = file.Replace('\\', '/');
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null || !material.HasProperty("_Smoothness"))
                    continue;

                // Le rôle : le nom sans le préfixe, ni la nuance (« fer_85 » : le fer, à 85 %).
                var role = Path.GetFileNameWithoutExtension(path).Substring(k_Prefix.Length);
                var cut = role.LastIndexOf('_');
                if (cut > 0 && int.TryParse(role.Substring(cut + 1), out _))
                    role = role.Substring(0, cut);
                if (!k_Roles.TryGetValue(role, out var look))
                    continue;

                var done = look.Bump > 0f ? material.GetTexture("_BumpMap") != null : material.GetFloat("_Smoothness") > 0f;
                if (done)
                    continue;

                if (look.Albedo)
                {
                    material.SetTexture("_BaseMap", albedo);
                    if (material.HasProperty("_MainTex"))
                        material.SetTexture("_MainTex", albedo);
                }

                if (look.Bump > 0f)
                {
                    material.SetTexture("_BumpMap", relief);
                    material.SetFloat("_BumpScale", look.Bump);
                    material.EnableKeyword("_NORMALMAP");
                }

                material.SetFloat("_Metallic", look.Metallic);
                material.SetFloat("_Smoothness", look.Smoothness);
                if (look.Glow > 0f)
                {
                    material.SetColor("_EmissionColor", material.GetColor("_BaseColor") * look.Glow);
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }

                EditorUtility.SetDirty(material);
                log.Add(Path.GetFileNameWithoutExtension(path));
            }

            if (log.Count == 0)
                return;
            AssetDatabase.SaveAssets();
            Debug.Log("La finition du Colosse de forge (2 novembre au soir) : " + string.Join(", ", log) + ".");
        }

        // La texture, importée tout de suite si elle ne l'est pas encore ; le relief, en carte de normales.
        static Texture2D Texture(string path, bool normalMap, List<string> log)
        {
            if (!File.Exists(path))
                return null;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            if (normalMap && AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
                log.Add(Path.GetFileName(path) + " en carte de normales");
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
