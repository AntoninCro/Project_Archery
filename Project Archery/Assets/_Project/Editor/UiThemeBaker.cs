using System.IO;
using Archery.Building;
using Archery.Menus;
using Archery.Shop;
using Archery.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le thème des menus (<see cref="UiTheme"/>), préparé et appliqué par l'éditeur, sans rien à faire à la main :
    /// <list type="number">
    /// <item>les sprites des packs Kenney utilisés passent en « Sprite » avec leurs bords (9-slice) ;</item>
    /// <item>les polices Cinzel reçoivent leur version TextMeshPro (« UI/Fonts ») ;</item>
    /// <item>l'asset « Resources/UiTheme » est créé (ses sprites, polices et sons vides sont remplis) ;</item>
    /// <item>le thème est appliqué aux prefabs de la carte de boutique et du panneau de difficulté, et aux menus, à la
    /// montre et au panneau des vagues des scènes ouvertes qui ne l'ont pas
    /// encore (ou dans une version plus ancienne) : la scène est alors marquée comme modifiée, il suffit de l'enregistrer.</item>
    /// </list>
    /// Menu <b>Archery > Interface > Réappliquer le thème aux menus</b> : après un changement de l'asset du thème.
    /// </summary>
    [InitializeOnLoad]
    static class UiThemeBaker
    {
        const string k_ThemePath = "Assets/_Project/Resources/UiTheme.asset";
        const string k_FontFolder = "Assets/_Project/UI/Fonts";
        static readonly string[] k_Prefabs = { "Assets/_Project/Prefabs/UI/Shop Card.prefab", "Assets/_Project/Prefabs/Difficulty Panel.prefab" };
        const string k_Borders = "Assets/kenney_fantasy-ui-borders/PNG/Double/";
        const string k_Sounds = "Assets/kenney_interfaceSounds/Audio/";

        // Les sprites du thème et leurs bords (gauche, bas, droite, haut), mesurés sur les images.
        static readonly (string Path, Vector4 Border)[] k_Sprites =
        {
            (k_Borders + "Panel/panel-010.png", new Vector4(24, 24, 24, 24)),
            (k_Borders + "Border/panel-border-010.png", new Vector4(20, 20, 20, 20)),
            (k_Borders + "Panel/panel-008.png", new Vector4(32, 32, 32, 32)),
            (k_Borders + "Border/panel-border-008.png", new Vector4(16, 16, 16, 16)),
            (k_Borders + "Panel/panel-002.png", new Vector4(28, 28, 28, 28)),
            (k_Borders + "Border/panel-border-002.png", new Vector4(24, 24, 24, 24)),
            (k_Borders + "Panel/panel-015.png", new Vector4(8, 8, 8, 8)),
            (k_Borders + "Border/panel-border-015.png", new Vector4(4, 4, 4, 4)),
            (k_Borders + "Divider Fade/divider-fade-000.png", new Vector4(0, 0, 32, 0)),
        };

        // Les lettres préparées dans les polices (les autres s'ajoutent toutes seules au besoin).
        const string k_Characters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,;:!?'\"«»()[]<>/+-–—%&#*@=_…" +
            "éèêëàâäîïôöùûüçÉÈÊËÀÂÎÏÔÙÛÇœŒ";

        static UiThemeBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= ApplyMissing;
            EditorApplication.delayCall += ApplyMissing;
        }

        static void ApplyMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;
            Run(false);
        }

        [MenuItem("Archery/Interface/Réappliquer le thème aux menus")]
        static void Reapply() => Run(true);

        static void Run(bool force)
        {
            var theme = EnsureTheme();
            if (theme == null)
                return;

            ApplyToPrefab(theme, force);
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    ApplyToScene(scene, theme, force);
            }
        }

        // ---------------------------------------------------------------- préparation

        static UiTheme EnsureTheme()
        {
            // Sans les packs, pas de thème (ils ont pu être déplacés ou supprimés).
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(k_Sprites[0].Path) == null)
                return null;

            foreach (var (path, border) in k_Sprites)
                EnsureSprite(path, border);

            var theme = AssetDatabase.LoadAssetAtPath<UiTheme>(k_ThemePath);
            var created = theme == null;
            if (created)
                theme = ScriptableObject.CreateInstance<UiTheme>();

            var changed = created;
            changed |= Fill(ref theme.titleFont, () => EnsureFont("Assets/cinzel/CinzelDecorative-Bold.otf", "Cinzel Decorative SDF", true));
            changed |= Fill(ref theme.labelFont, () => EnsureFont("Assets/cinzel/Cinzel-Bold.otf", "Cinzel SDF", false));
            changed |= Fill(ref theme.bodyFont, () => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular SDF.asset"));
            changed |= Fill(ref theme.panelFill, () => LoadSprite(0));
            changed |= Fill(ref theme.panelBorder, () => LoadSprite(1));
            changed |= Fill(ref theme.frameFill, () => LoadSprite(2));
            changed |= Fill(ref theme.frameBorder, () => LoadSprite(3));
            changed |= Fill(ref theme.buttonFill, () => LoadSprite(4));
            changed |= Fill(ref theme.buttonBorder, () => LoadSprite(5));
            changed |= Fill(ref theme.plainFill, () => LoadSprite(6));
            changed |= Fill(ref theme.plainBorder, () => LoadSprite(7));
            changed |= Fill(ref theme.divider, () => LoadSprite(8));
            changed |= Fill(ref theme.hoverClip, () => AssetDatabase.LoadAssetAtPath<AudioClip>(k_Sounds + "select_001.ogg"));
            changed |= Fill(ref theme.clickClip, () => AssetDatabase.LoadAssetAtPath<AudioClip>(k_Sounds + "click_001.ogg"));
            changed |= Fill(ref theme.confirmClip, () => AssetDatabase.LoadAssetAtPath<AudioClip>(k_Sounds + "confirmation_001.ogg"));
            changed |= Fill(ref theme.backClip, () => AssetDatabase.LoadAssetAtPath<AudioClip>(k_Sounds + "back_001.ogg"));

            if (created)
            {
                AssetDatabase.CreateAsset(theme, k_ThemePath);
                Debug.Log("Thème des menus : asset « Resources/UiTheme » créé (couleurs, sprites, polices, sons).", theme);
            }
            else if (changed)
            {
                EditorUtility.SetDirty(theme);
            }

            if (changed)
                AssetDatabase.SaveAssets();
            return theme;
        }

        static bool Fill<T>(ref T field, System.Func<T> load) where T : Object
        {
            if (field != null)
                return false;
            field = load();
            return field != null;
        }

        static Sprite LoadSprite(int index) => AssetDatabase.LoadAssetAtPath<Sprite>(k_Sprites[index].Path);

        // En « Sprite » simple, avec ses bords, sans compression (traits nets), avec mipmaps (lisible de loin dans le casque).
        static void EnsureSprite(string path, Vector4 border)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                return;
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
                importer.spriteBorder == border && importer.textureCompression == TextureImporterCompression.Uncompressed)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // Une police TextMeshPro dynamique (les lettres manquantes s'ajoutent au besoin), avec un contour et une ombre
        // légers pour rester lisible sur les panneaux sombres et les boutons colorés.
        static TMP_FontAsset EnsureFont(string sourcePath, string assetName, bool title)
        {
            var assetPath = $"{k_FontFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
                return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null)
                return null;

            EnsureFolder(k_FontFolder);
            var asset = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null)
                return null;

            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.atlasTextures[0].name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            var material = asset.material;
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, title ? 0.12f : 0.08f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, title ? new Color(0.28f, 0.12f, 0.02f) : new Color(0.06f, 0.03f, 0.12f));
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.6f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.4f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.3f);

            asset.TryAddCharacters(k_Characters, out _);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"Thème des menus : police « {assetName} » créée dans {k_FontFolder}.", asset);
            return asset;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        // ---------------------------------------------------------------- application

        static bool UpToDate(GameObject root) => root.TryGetComponent<UiThemed>(out var marker) && marker.version >= UiThemer.Version;

        // La carte de la boutique (et du panneau de construction), le panneau de difficulté : on habille les prefabs, leurs
        // copies dans la scène suivent.
        static void ApplyToPrefab(UiTheme theme, bool force)
        {
            foreach (var path in k_Prefabs)
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    continue;

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (!force && UpToDate(root))
                        continue;
                    UiThemer.Apply(root, theme);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"Thème des menus : appliqué au prefab « {Path.GetFileNameWithoutExtension(path)} ».");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            // La barre de PV au-dessus de la tête des boss des vagues (prefabs « Boss_… »).
            foreach (var guid in AssetDatabase.FindAssets("Boss_ t:Prefab", new[] { "Assets/_Project/Prefabs" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileName(path).StartsWith("Boss_"))
                    continue;

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var applied = false;
                    foreach (var bar in root.GetComponentsInChildren<HealthBar>(true))
                    {
                        if (!force && UpToDate(bar.gameObject))
                            continue;
                        UiThemer.Apply(bar.gameObject, theme);
                        applied = true;
                    }

                    if (!applied)
                        continue;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"Thème des menus : appliqué à la barre de PV du prefab « {Path.GetFileNameWithoutExtension(path)} ».");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        static void ApplyToScene(Scene scene, UiTheme theme, bool force)
        {
            var applied = 0;
            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                foreach (var panel in sceneRoot.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    // Les menus, la montre et le panneau des vagues (tous deux avec un HudDisplay).
                    if (!(panel is MainMenuPanel || panel is GameOverPanel || panel is ShopPanel || panel is BuildPanel || panel is HudDisplay))
                        continue;

                    var root = panel.gameObject;
                    if (!force && UpToDate(root))
                        continue;

                    // Les morceaux de prefabs placés dans le menu (cartes, champ du clavier) : on ne les touche pas ici.
                    UiThemer.Apply(root, theme, go => PrefabUtility.IsPartOfPrefabInstance(go));

                    // Le menu principal jouait son propre clic : les boutons du thème ont désormais le leur.
                    if (panel is MainMenuPanel)
                    {
                        var serialized = new SerializedObject(panel);
                        var click = serialized.FindProperty("m_ClickClip");
                        if (click != null)
                        {
                            click.objectReferenceValue = null;
                            serialized.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }

                    EditorUtility.SetDirty(root);
                    applied++;
                }
            }

            if (applied == 0)
                return;

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Thème des menus : appliqué à {applied} menu(s) de la scène {scene.name}. Enregistre la scène (Ctrl+S) pour le garder ; " +
                      "si le résultat ne te plaît pas, ferme-la sans l'enregistrer.");
        }
    }
}
