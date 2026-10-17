using Archery.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Archery.Menus
{
    /// <summary>
    /// La page « Tutoriel » du menu principal (GDD, section 14), fabriquée en jeu dans le menu : la liste des
    /// catégories (Tir, Déplacement, Vagues, Construction), puis, pour chacune, ses fenêtres : un titre, une petite
    /// vidéo qui tourne en boucle, quelques lignes, les flèches pour passer de l'une à l'autre et « Retour ».
    /// </summary>
    /// <remarks>
    /// Le contenu vient de <see cref="TutorialContent"/> (<c>Resources/TutorialContent</c>). Les vidéos se branchent
    /// toutes seules : <c>Resources/Tutoriel/&lt;préfixe&gt;_&lt;numéro&gt;</c>, par exemple <c>Tir_1.mp4</c> pour la
    /// première fenêtre du tir ; sans vidéo, le cadre affiche « Vidéo à venir ». Comme le reste du menu, tout se clique
    /// au rayon de la main ou se déclenche d'une flèche (<see cref="MenuArrowTarget"/>).
    /// </remarks>
    public class TutorialMenu : MonoBehaviour
    {
        const string k_VideoFolder = "Tutoriel/";

        // Mise en page dans la page du menu (880 × 620 unités du Canvas).
        static readonly Vector2 k_VideoSize = new Vector2(480f, 270f);
        const float k_VideoBorder = 16f;

        MainMenuPanel m_Menu;
        TutorialContent m_Content;
        GameObject m_Sections;
        GameObject m_Window;
        TMP_Text m_Title;
        TMP_Text m_Text;
        TMP_Text m_Counter;
        TMP_Text m_Placeholder;
        RawImage m_Video;
        Button m_Previous;
        Button m_Next;
        VideoPlayer m_Player;
        RenderTexture m_Texture;
        int m_Section = -1;
        int m_Index;

        /// <summary>Fabrique la page sous <paramref name="parent"/> (le contenu du menu), cachée. Rend son composant.</summary>
        public static TutorialMenu Create(Transform parent, MainMenuPanel menu)
        {
            // Même place que les autres pages du menu.
            var root = NewRect("Tutorial", parent);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.sizeDelta = new Vector2(-120f, -180f);
            root.anchoredPosition = new Vector2(0f, -50f);

            var tutorial = root.gameObject.AddComponent<TutorialMenu>();
            tutorial.m_Menu = menu;
            tutorial.m_Content = TutorialContent.Load();
            tutorial.BuildSections(root);
            tutorial.BuildWindow(root);

            var theme = UiTheme.Load();
            if (theme != null)
            {
                UiThemer.Apply(root.gameObject, theme);
                UiThemer.ApplyPanel(tutorial.m_Video.transform.parent.GetComponent<Image>(), theme, 14f);
            }

            tutorial.m_Window.SetActive(false);
            root.gameObject.SetActive(false);
            return tutorial;
        }

        void OnEnable()
        {
            if (m_Sections != null)
                ShowSections();
        }

        void OnDisable() => StopVideo();

        void OnDestroy()
        {
            if (m_Texture != null)
            {
                m_Texture.Release();
                Destroy(m_Texture);
            }
        }

        // ---------------------------------------------------------------- navigation

        /// <summary>La liste des catégories.</summary>
        public void ShowSections()
        {
            StopVideo();
            m_Section = -1;
            m_Window.SetActive(false);
            m_Sections.SetActive(true);
        }

        public void OpenSection(int section)
        {
            if (m_Content == null || section < 0 || section >= m_Content.sections.Count || m_Content.sections[section].windows.Count == 0)
                return;

            m_Section = section;
            m_Index = 0;
            m_Sections.SetActive(false);
            m_Window.SetActive(true);
            ShowWindow();
        }

        public void Previous() => Step(-1);

        public void Next() => Step(1);

        void Step(int step)
        {
            if (m_Section < 0)
                return;

            var count = m_Content.sections[m_Section].windows.Count;
            var index = Mathf.Clamp(m_Index + step, 0, count - 1);
            if (index == m_Index)
                return;

            m_Index = index;
            ShowWindow();
        }

        void ShowWindow()
        {
            var section = m_Content.sections[m_Section];
            var window = section.windows[m_Index];
            m_Title.text = window.title;
            m_Text.text = window.text;
            m_Counter.text = $"{m_Index + 1} / {section.windows.Count}";
            m_Previous.interactable = m_Index > 0;
            m_Next.interactable = m_Index < section.windows.Count - 1;

            // La vidéo de cette fenêtre, si elle a été ajoutée ; sinon, « Vidéo à venir ».
            ClearVideo();
            var clip = string.IsNullOrEmpty(section.videoPrefix) ? null
                : Resources.Load<VideoClip>(k_VideoFolder + section.videoPrefix + "_" + (m_Index + 1));
            if (clip != null)
            {
                m_Player.clip = clip;
                m_Player.Play();
            }
            else
            {
                m_Player.Stop();
                m_Player.clip = null;
            }

            m_Video.enabled = clip != null;
            m_Placeholder.enabled = clip == null;
        }

        void StopVideo()
        {
            if (m_Player != null && m_Player.isPlaying)
                m_Player.Stop();
        }

        // La vidéo d'avant ne reste pas affichée le temps que la suivante démarre.
        void ClearVideo()
        {
            if (m_Texture == null)
                return;
            var active = RenderTexture.active;
            RenderTexture.active = m_Texture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = active;
        }

        // ---------------------------------------------------------------- fabrication

        // La liste : un intitulé, un bouton par catégorie (avec son icône), « Retour » à l'accueil du menu.
        void BuildSections(RectTransform root)
        {
            var list = NewRect("Sections", root);
            Stretch(list, 0f);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 18f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            AddPanelFx(list.gameObject);

            var heading = NewText(list, "Label", "Tutoriel", 34f, TextAlignmentOptions.Center);
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            for (var i = 0; i < m_Content.sections.Count; i++)
            {
                var index = i;
                var section = m_Content.sections[i];
                var button = NewButton(list, section.title, section.title, () => OpenSection(index));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;
                if (section.icon != null)
                {
                    var icon = NewRect("Icône", button.transform);
                    icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
                    icon.anchoredPosition = new Vector2(52f, 0f);
                    icon.sizeDelta = new Vector2(54f, 54f);
                    var image = icon.gameObject.AddComponent<Image>();
                    image.sprite = section.icon;
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    icon.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                }
            }

            var back = NewButton(list, "Back", "Retour", () =>
            {
                if (m_Menu != null)
                    m_Menu.ShowPage(0);
            });
            back.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;
            m_Sections = list.gameObject;
        }

        // Une fenêtre : le titre, le cadre de la vidéo, le texte, et en bas « Retour », « < », « 1 / 4 », « > ».
        void BuildWindow(RectTransform root)
        {
            var window = NewRect("Window", root);
            Stretch(window, 0f);
            AddPanelFx(window.gameObject);
            m_Window = window.gameObject;

            m_Title = NewText(window, "Title", "", 40f, TextAlignmentOptions.Center);
            Place(m_Title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(780f, 56f));

            var frame = NewRect("Video Frame", window);
            Place(frame, new Vector2(0.5f, 1f), new Vector2(0f, -76f - (k_VideoSize.y * 0.5f + k_VideoBorder)), k_VideoSize + Vector2.one * (2f * k_VideoBorder));
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.color = new Color(0.06f, 0.05f, 0.12f, 0.95f);
            frameImage.raycastTarget = false;

            var video = NewRect("Video", frame);
            Stretch(video, k_VideoBorder);
            m_Video = video.gameObject.AddComponent<RawImage>();
            m_Video.raycastTarget = false;
            m_Placeholder = NewText(frame, "Label", "Vidéo à venir", 30f, TextAlignmentOptions.Center);
            Stretch(m_Placeholder.rectTransform, k_VideoBorder);

            m_Text = NewText(window, "Text", "", 28f, TextAlignmentOptions.Center);
            m_Text.enableAutoSizing = true;
            m_Text.fontSizeMin = 20f;
            m_Text.fontSizeMax = 28f;
            Place(m_Text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -76f - k_VideoSize.y - 2f * k_VideoBorder - 80f), new Vector2(820f, 130f));

            var back = NewButton(window, "Back", "Retour", ShowSections);
            Place((RectTransform)back.transform, new Vector2(0.5f, 0f), new Vector2(-290f, 42f), new Vector2(250f, 72f));
            m_Previous = NewButton(window, "Previous", "<", Previous);
            Place((RectTransform)m_Previous.transform, new Vector2(0.5f, 0f), new Vector2(70f, 42f), new Vector2(96f, 72f));
            m_Counter = NewText(window, "Value", "1 / 1", 30f, TextAlignmentOptions.Center);
            Place(m_Counter.rectTransform, new Vector2(0.5f, 0f), new Vector2(190f, 42f), new Vector2(130f, 72f));
            m_Next = NewButton(window, "Next", ">", Next);
            Place((RectTransform)m_Next.transform, new Vector2(0.5f, 0f), new Vector2(310f, 42f), new Vector2(96f, 72f));

            // La vidéo : en boucle, sans le son, dans une texture affichée par le cadre.
            m_Texture = new RenderTexture(640, 360, 0) { name = "Vidéo du tutoriel" };
            m_Video.texture = m_Texture;
            m_Player = gameObject.AddComponent<VideoPlayer>();
            m_Player.playOnAwake = false;
            m_Player.isLooping = true;
            m_Player.skipOnDrop = true;
            m_Player.waitForFirstFrame = true;
            m_Player.renderMode = VideoRenderMode.RenderTexture;
            m_Player.targetTexture = m_Texture;
            m_Player.audioOutputMode = VideoAudioOutputMode.None;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static TMP_Text NewText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment)
        {
            var rect = NewRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        // Un bouton des menus : le thème le colore selon son rôle (« Retour », « < », « > »…), et lui donne ses effets.
        static Button NewButton(Transform parent, string name, string label, UnityAction onClick)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.25f, 0.45f, 0.95f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(onClick);
            var text = NewText(rect, "Text (TMP)", label, 34f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 8f);
            return button;
        }

        // Comme les autres pages : elle apparaît en se dépliant.
        static void AddPanelFx(GameObject target)
        {
            if (target.GetComponent<CanvasGroup>() == null)
                target.AddComponent<CanvasGroup>();
            if (target.GetComponent<UiPanelFx>() == null)
                target.AddComponent<UiPanelFx>();
        }
    }
}
