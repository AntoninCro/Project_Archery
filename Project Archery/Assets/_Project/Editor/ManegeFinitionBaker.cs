using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// La finition du Grand Manège (2 novembre, tard ; <c>Docs/Refonte_TowerDefense.md</c>, 12.51 ; <c>Docs/Ultimes_Cahier.md</c> : « un
    /// modèle fini », ce que le joueur a demandé pour le Colosse) : ses matériaux (<c>Art/TowerDefense/Palettes/Ultime_Manege_*</c>, faits
    /// par <see cref="TowerDefenseLookBaker"/>, couleurs unies) deviennent du bois peint et verni (la couleur et le relief de
    /// <c>Tools/texture_bois_peint.py</c> : <c>Art/TowerDefense/Manege_Bois.png</c> et <c>Manege_Bois_Relief.png</c>, importé en carte de
    /// normales), brillant ; l'or très poli, qui luit un peu dans le noir ; les miroirs, qui reflètent le ciel.
    /// </summary>
    /// <remarks>
    /// Comme <see cref="ColosseFinitionBaker"/> : chaque matériau une seule fois, s'il n'a pas encore de relief (ou, sans relief, s'il est
    /// encore mat) ; un tour d'éditeur après le baker de l'allure, qui peut créer des matériaux au même tour.
    /// </remarks>
    [InitializeOnLoad]
    static class ManegeFinitionBaker
    {
        const string k_Palettes = "Assets/_Project/Art/TowerDefense/Palettes";
        const string k_Prefix = "Ultime_Manege_";
        const string k_Albedo = "Assets/_Project/Art/TowerDefense/Manege_Bois.png";
        const string k_Relief = "Assets/_Project/Art/TowerDefense/Manege_Bois_Relief.png";

        // La peinture vernie des chevaux et du chapiteau : la texture du bois peint, un relief doux, brillante.
        static readonly (bool Albedo, float Bump, float Metallic, float Smoothness, float Glow) k_Paint = (true, 0.45f, 0f, 0.62f, 0f);

        // Par rôle de la palette : la texture du bois (sa couleur), la force du relief, le métal, le poli, la lueur.
        static readonly Dictionary<string, (bool Albedo, float Bump, float Metallic, float Smoothness, float Glow)> k_Roles =
            new Dictionary<string, (bool, float, float, float, float)>
            {
                { "rouge", k_Paint }, { "creme", k_Paint }, { "bleu", k_Paint }, { "vert", k_Paint }, { "violet", k_Paint },
                { "rose", k_Paint }, { "blanc", k_Paint }, { "noir", k_Paint }, { "gris", k_Paint }, { "bai", k_Paint },
                { "alezan", k_Paint }, { "isabelle", k_Paint },
                { "plancher", (true, 0.9f, 0f, 0.3f, 0f) },
                { "bois", (true, 0.8f, 0f, 0.38f, 0f) },
                { "pierre", (true, 0.6f, 0f, 0.15f, 0f) },
                { "accent", (false, 0.2f, 1f, 0.82f, 0.15f) },
                { "miroir", (false, 0f, 1f, 0.96f, 0.06f) },
            };

        static ManegeFinitionBaker()
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

                // Le rôle : le nom sans le préfixe, ni la nuance (« creme_92 » : le crème, à 92 %).
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
            Debug.Log("La finition du Grand Manège (2 novembre) : " + string.Join(", ", log) + ".");
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
