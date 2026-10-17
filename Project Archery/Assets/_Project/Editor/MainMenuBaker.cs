using Archery.Menus;
using Archery.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Archery.EditorTools
{
    /// <summary>
    /// L'accueil du menu principal, à l'ouverture de la scène (16 octobre) : les boutons « Jouer » et la ligne
    /// « Difficulté » sont retirés (on choisit la difficulté en tirant sur un panneau, et on lance la partie avec le
    /// gong), une phrase le rappelle en haut, et un bouton « Tutoriel » est ajouté sous « Paramètres ». La scène est
    /// marquée comme modifiée : il suffit de l'enregistrer.
    /// Crée aussi l'asset <c>Resources/TutorialContent</c> (les textes du tutoriel, avec les icônes des catégories).
    /// </summary>
    [InitializeOnLoad]
    static class MainMenuBaker
    {
        const string k_ContentPath = "Assets/_Project/Resources/TutorialContent.asset";
        const string k_IconFolder = "Assets/_Project/UI/Icones/";
        const string k_Hint = "Tire sur un panneau pour choisir ta difficulté, puis frappe le gong pour commencer.";

        static MainMenuBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            EnsureTutorialContent();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    UpdateHome(scene);
            }
        }

        // Les textes validés, et les icônes des catégories : l'arc, les pas, le gong, le marteau.
        static void EnsureTutorialContent()
        {
            if (AssetDatabase.LoadAssetAtPath<TutorialContent>(k_ContentPath) != null)
                return;

            var content = TutorialContent.CreateDefault();
            content.name = "TutorialContent";
            string[] icons = { "Icone_Arc", "Icone_Deplacement", "Icone_Gong", "Icone_Reparer" };
            for (var i = 0; i < content.sections.Count && i < icons.Length; i++)
                content.sections[i].icon = AssetDatabase.LoadAssetAtPath<Sprite>(k_IconFolder + icons[i] + ".png");

            AssetDatabase.CreateAsset(content, k_ContentPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Tutoriel : textes créés dans {k_ContentPath} (modifiables dans l'inspecteur).");
        }

        static void UpdateHome(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var menu in root.GetComponentsInChildren<MainMenuPanel>(true))
                {
                    var home = menu.transform.Find("Content/Home");
                    if (home == null || !UpdateHome(menu, home))
                        continue;

                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"Menu principal : « Jouer » et « Difficulté » retirés, « Tutoriel » ajouté (scène « {scene.name} »). " +
                              "Enregistre-la (Ctrl+S).", menu);
                }
            }
        }

        static bool UpdateHome(MainMenuPanel menu, Transform home)
        {
            var changed = false;
            foreach (var name in new[] { "Play", "Difficulty Row" })
            {
                var child = home.Find(name);
                if (child == null)
                    continue;
                Undo.DestroyObjectImmediate(child.gameObject);
                changed = true;
            }

            // « Tutoriel » : une copie de « Paramètres », juste dessous, qui ouvre le tutoriel.
            var settings = home.Find("Settings Button");
            if (home.Find("Tutorial Button") == null && settings != null)
            {
                var copy = Object.Instantiate(settings.gameObject, home);
                copy.name = "Tutorial Button";
                copy.transform.SetSiblingIndex(settings.GetSiblingIndex() + 1);
                var label = copy.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = "Tutoriel";

                var button = copy.GetComponent<Button>();
                if (button != null)
                {
                    for (var i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                        UnityEventTools.RemovePersistentListener(button.onClick, i);
                    UnityEventTools.AddVoidPersistentListener(button.onClick, new UnityAction(menu.ShowTutorial));
                }

                Undo.RegisterCreatedObjectUndo(copy, "Bouton Tutoriel");
                changed = true;
            }

            // La phrase qui dit comment commencer, en haut de l'accueil.
            if (home.Find("Hint") == null)
            {
                var hint = new GameObject("Hint", typeof(RectTransform));
                hint.layer = home.gameObject.layer;
                hint.transform.SetParent(home, false);
                hint.transform.SetSiblingIndex(0);
                var text = hint.AddComponent<TextMeshProUGUI>();
                text.text = k_Hint;
                text.fontSize = 28f;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.raycastTarget = false;
                var theme = UiTheme.Load();
                if (theme != null)
                {
                    if (theme.bodyFont != null)
                        text.font = theme.bodyFont;
                    text.color = theme.labelColor;
                }

                hint.AddComponent<LayoutElement>().preferredHeight = 80f;
                Undo.RegisterCreatedObjectUndo(hint, "Phrase de l'accueil");
                changed = true;
            }

            return changed;
        }
    }
}
