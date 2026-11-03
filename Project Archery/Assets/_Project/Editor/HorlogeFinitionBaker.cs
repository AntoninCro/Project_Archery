using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// La finition de la Grande Horloge (2 novembre, tard ; <c>Docs/Refonte_TowerDefense.md</c>, 12.52 ; <c>Docs/Ultimes_Cahier.md</c> :
    /// « un modèle fini ») : ses matériaux (<c>Art/TowerDefense/Palettes/Ultime_Horloge_*</c>, faits par <see cref="TowerDefenseLookBaker"/>,
    /// couleurs unies) deviennent de la glace taillée (la couleur et le relief de <c>Tools/texture_glace.py</c> :
    /// <c>Art/TowerDefense/Horloge_Glace.png</c> et <c>Horloge_Glace_Relief.png</c>, importé en carte de normales), très lisse et qui
    /// luit à peine ; la nuit du cadran, polie ; l'argent et l'or très polis.
    /// </summary>
    /// <remarks>
    /// Comme <see cref="ManegeFinitionBaker"/> : chaque matériau une seule fois, s'il n'a pas encore de relief (ou, sans relief, s'il est
    /// encore mat) ; un tour d'éditeur après le baker de l'allure, qui peut créer des matériaux au même tour.
    /// </remarks>
    [InitializeOnLoad]
    static class HorlogeFinitionBaker
    {
        const string k_Palettes = "Assets/_Project/Art/TowerDefense/Palettes";
        const string k_Prefix = "Ultime_Horloge_";
        const string k_Albedo = "Assets/_Project/Art/TowerDefense/Horloge_Glace.png";
        const string k_Relief = "Assets/_Project/Art/TowerDefense/Horloge_Glace_Relief.png";

        // Par rôle de la palette : la texture de la glace (sa couleur), la force du relief, le métal, le poli, la lueur.
        static readonly Dictionary<string, (bool Albedo, float Bump, float Metallic, float Smoothness, float Glow)> k_Roles =
            new Dictionary<string, (bool, float, float, float, float)>
            {
                { "glace", (true, 0.5f, 0f, 0.9f, 0.08f) },
                { "glace_sombre", (true, 0.5f, 0f, 0.88f, 0.05f) },
                { "nuit", (false, 0f, 0.2f, 0.8f, 0f) },
                { "argent", (false, 0.2f, 1f, 0.86f, 0.05f) },
                { "accent", (false, 0.2f, 1f, 0.82f, 0.15f) },
                { "pierre", (true, 0.6f, 0f, 0.2f, 0f) },
            };

        static HorlogeFinitionBaker()
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

                // Le rôle : le nom sans le préfixe, ni la nuance (« pierre_60 » : la pierre, à 60 %).
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
            Debug.Log("La finition de la Grande Horloge (2 novembre) : " + string.Join(", ", log) + ".");
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
