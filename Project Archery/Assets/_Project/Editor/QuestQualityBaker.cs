using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le niveau de qualité « Quest », pour le jeu dans le casque seul (build Android), d'après l'audit de performance du
    /// 25 octobre (<c>Docs/Audit_Performance_Quest.md</c>). Le PC garde « Ultra », inchangé.
    /// <list type="bullet">
    /// <item><c>Quest Renderer</c> : une copie du renderer mobile « Android Preset » (rendu Forward, sans occlusion ambiante,
    /// native render pass), avec le post-traitement allumé : le bloom des néons fait le look du jeu.</item>
    /// <item><c>Quest URP Config</c> : HDR en 32 bits, MSAA ×4, sans texture de profondeur, ombres dures du soleil (1024,
    /// une cascade, 20 m), 2 lumières en plus par objet.</item>
    /// <item>Le niveau « Quest », copie d'« Ultra » qui s'en sert, par défaut sur Android. Il reste proposé sur PC : le
    /// choisir dans <i>Project Settings > Quality</i> montre au casque, par Quest Link, l'image du Quest.</item>
    /// <item>Le rendu fovéal de l'OpenXR pour Android, avec l'API de l'URP ; son niveau est réglé au lancement du jeu par
    /// <c>QuestFoveation</c>.</item>
    /// </list>
    /// Une fois, tant que le niveau « Quest » n'existe pas. Le menu <i>Archery > Qualité Quest > Refaire les réglages</i>
    /// remet ces valeurs : ce qu'on y a changé à la main est alors perdu.
    /// </summary>
    [InitializeOnLoad]
    static class QuestQualityBaker
    {
        const string k_LevelName = "Quest";
        const string k_Folder = "Assets/Settings/Project Configuration";
        const string k_MobileRenderer = k_Folder + "/Android Preset.asset";
        const string k_PcRenderer = k_Folder + "/Standalone Preset.asset";
        const string k_MobilePipeline = k_Folder + "/Performance URP Config.asset";
        const string k_Renderer = k_Folder + "/Quest Renderer.asset";
        const string k_Pipeline = k_Folder + "/Quest URP Config.asset";
        const string k_QualitySettings = "ProjectSettings/QualitySettings.asset";

        static QuestQualityBaker()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                    return;
                if (System.Array.IndexOf(QualitySettings.names, k_LevelName) < 0)
                    Apply();
            };
        }

        [MenuItem("Archery/Qualité Quest/Refaire les réglages")]
        static void Apply()
        {
            var renderer = MakeRenderer();
            var pipeline = MakePipeline(renderer);
            if (pipeline == null)
                return;

            var level = MakeLevel(pipeline);
            var foveation = EnableFoveation();
            AssetDatabase.SaveAssets();
            Debug.Log($"QuestQualityBaker : niveau de qualité « {k_LevelName} » (n° {level}), par défaut sur Android, avec " +
                      $"{k_Pipeline} et {k_Renderer} ; rendu fovéal {(foveation ? "activé pour Android" : "introuvable")}. " +
                      "Le PC garde « Ultra ».");
        }

        static ScriptableObject MakeRenderer()
        {
            if (AssetDatabase.LoadMainAssetAtPath(k_Renderer) == null && !AssetDatabase.CopyAsset(k_MobileRenderer, k_Renderer))
            {
                Debug.LogError($"QuestQualityBaker : impossible de copier {k_MobileRenderer}.");
                return null;
            }

            var renderer = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Renderer);
            var so = new SerializedObject(renderer);

            // Le post-traitement (bloom, couleurs, vignette) : les données du renderer du PC. Pour l'éteindre sur le Quest
            // (le plus rapide, mais les néons ne bavent plus), décocher « Post-processing » dans Quest Renderer.
            var pc = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_PcRenderer);
            var postProcess = so.FindProperty("postProcessData");
            if (pc != null && postProcess != null)
                postProcess.objectReferenceValue = new SerializedObject(pc).FindProperty("postProcessData").objectReferenceValue;

            // Forward (Forward+ sur PC), sans pré-passe de profondeur, en native render pass (la puce du Quest dessine par
            // carreaux, en mémoire rapide).
            Set(so, "m_RenderingMode", 0);
            Set(so, "m_DepthPrimingMode", 0);
            Set(so, "m_UseNativeRenderPass", 1);
            so.ApplyModifiedPropertiesWithoutUndo();
            return renderer;
        }

        static RenderPipelineAsset MakePipeline(ScriptableObject renderer)
        {
            if (renderer == null)
                return null;

            if (AssetDatabase.LoadMainAssetAtPath(k_Pipeline) == null && !AssetDatabase.CopyAsset(k_MobilePipeline, k_Pipeline))
            {
                Debug.LogError($"QuestQualityBaker : impossible de copier {k_MobilePipeline}.");
                return null;
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(k_Pipeline);
            var so = new SerializedObject(pipeline);
            var renderers = so.FindProperty("m_RendererDataList");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            Set(so, "m_DefaultRendererIndex", 0);

            // Le HDR en 32 bits (R11G11B10) pèse autant qu'une image sans HDR, et garde le bloom des néons comme sur PC.
            Set(so, "m_SupportsHDR", 1);
            Set(so, "m_HDRColorBufferPrecision", 0);
            // Le MSAA ×4 est presque gratuit sur la puce du Quest (×2 sur PC).
            Set(so, "m_MSAA", 4);
            Set(so, "m_RenderScale", 1f);
            // Sans texture de profondeur (sa copie coûte cher sur le Quest) : l'eau et les rayons de lumière ont des bords
            // nets au lieu de fondus.
            Set(so, "m_RequireDepthTexture", 0);
            Set(so, "m_RequireOpaqueTexture", 0);
            // Les ombres du soleil : dures, 1024, une cascade, jusqu'à 20 m (douces, 2048, deux cascades et 50 m sur PC).
            // Elles redessinent la carte une deuxième fois.
            Set(so, "m_MainLightRenderingMode", 1);
            Set(so, "m_MainLightShadowsSupported", 1);
            Set(so, "m_MainLightShadowmapResolution", 1024);
            Set(so, "m_ShadowDistance", 20f);
            Set(so, "m_ShadowCascadeCount", 1);
            Set(so, "m_SoftShadowsSupported", 0);
            // Les lumières en plus (torches, néons, cristaux) : par pixel, mais 2 par objet au plus (4 sur PC), sans ombre.
            Set(so, "m_AdditionalLightsRenderingMode", 1);
            Set(so, "m_AdditionalLightsPerObjectLimit", 2);
            Set(so, "m_AdditionalLightShadowsSupported", 0);
            Set(so, "m_UseFastSRGBLinearConversion", 1);
            so.ApplyModifiedPropertiesWithoutUndo();
            return pipeline;
        }

        static int MakeLevel(RenderPipelineAsset pipeline)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(k_QualitySettings);
            if (assets.Length == 0)
            {
                Debug.LogError($"QuestQualityBaker : {k_QualitySettings} illisible.");
                return -1;
            }

            var so = new SerializedObject(assets[0]);
            var levels = so.FindProperty("m_QualitySettings");
            var index = -1;
            for (var i = 0; i < levels.arraySize; i++)
            {
                if (levels.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == k_LevelName)
                    index = i;
            }

            // Ajouté à la fin, copie du dernier niveau (« Ultra ») : les numéros des autres niveaux ne bougent pas, le PC
            // garde le sien.
            if (index < 0)
            {
                levels.arraySize++;
                index = levels.arraySize - 1;
            }

            var level = levels.GetArrayElementAtIndex(index);
            level.FindPropertyRelative("name").stringValue = k_LevelName;
            level.FindPropertyRelative("customRenderPipeline").objectReferenceValue = pipeline;
            // Le casque impose son rythme ; l'URP prend son MSAA dans Quest URP Config.
            SetRelative(level, "vSyncCount", 0);
            SetRelative(level, "antiAliasing", 0);
            // Les particules et la végétation « douces » demandent la texture de profondeur.
            SetRelative(level, "softParticles", 0);
            SetRelative(level, "softVegetation", 0);
            // Le filtrage anisotrope seulement pour les textures qui le demandent.
            SetRelative(level, "anisotropicTextures", 1);
            // La sonde de reflets du toit cyberpunk est rendue une fois, à l'entrée.
            SetRelative(level, "realtimeReflectionProbes", 1);
            var excluded = level.FindPropertyRelative("excludedTargetPlatforms");
            if (excluded != null)
                excluded.arraySize = 0;

            // Le niveau par défaut d'Android.
            var defaults = so.FindProperty("m_PerPlatformDefaultQuality");
            var found = false;
            for (var i = 0; defaults != null && i < defaults.arraySize; i++)
            {
                var pair = defaults.GetArrayElementAtIndex(i);
                var platform = pair.FindPropertyRelative("first");
                if (platform == null || platform.stringValue != "Android")
                    continue;
                pair.FindPropertyRelative("second").intValue = index;
                found = true;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(assets[0]);
            if (!found)
                Debug.LogWarning($"QuestQualityBaker : niveau par défaut d'Android introuvable. À choisir à la main : Project Settings > " +
                                 $"Quality, la flèche sous la colonne Android, sur la ligne « {k_LevelName} ».");
            return index;
        }

        // Le rendu fovéal de l'OpenXR pour Android : les bords de l'image, que l'œil voit flous à travers les lentilles,
        // sont dessinés en moins de détails. L'API de l'URP (« SRP Foveation ») : Unity la conseille avec l'URP.
        static bool EnableFoveation()
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var feature = settings != null ? settings.GetFeature<FoveatedRenderingFeature>() : null;
            if (feature == null)
                return false;

            feature.enabled = true;
            settings.foveatedRenderingApi = OpenXRSettings.BackendFovationApi.SRPFoveation;
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(settings);
            return true;
        }

        static void Set(SerializedObject so, string name, float value)
        {
            var property = so.FindProperty(name);
            if (property == null)
            {
                Debug.LogWarning($"QuestQualityBaker : réglage « {name} » introuvable dans {so.targetObject.name}.");
                return;
            }

            Assign(property, value);
        }

        static void SetRelative(SerializedProperty parent, string name, float value)
        {
            var property = parent.FindPropertyRelative(name);
            if (property == null)
            {
                Debug.LogWarning($"QuestQualityBaker : réglage de qualité « {name} » introuvable.");
                return;
            }

            Assign(property, value);
        }

        static void Assign(SerializedProperty property, float value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    property.boolValue = value != 0f;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = value;
                    break;
                default:
                    property.intValue = Mathf.RoundToInt(value);
                    break;
            }
        }
    }
}
