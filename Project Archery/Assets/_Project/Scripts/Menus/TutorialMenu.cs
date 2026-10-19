using Archery.Player;
using Archery.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Archery.Menus
{
    /// <summary>
    /// La page « Tutoriel » du menu principal (GDD, section 14), fabriquée en jeu dans le menu : la liste des
    /// catégories (Tir, Déplacement, Vagues, Construction). Une catégorie s'ouvre sur un <b>grand écran</b>, à droite
    /// du menu, un peu plus loin que lui : un titre, une grande vidéo qui tourne en boucle, quelques lignes, les flèches
    /// pour passer d'une fenêtre à l'autre et « Fermer ». La liste reste dans le menu, pour passer à une autre catégorie.
    /// </summary>
    /// <remarks>
    /// Le contenu vient de <see cref="TutorialContent"/> (<c>Resources/TutorialContent</c>). Les vidéos se branchent
    /// toutes seules : <c>Resources/Tutoriel/&lt;préfixe&gt;_&lt;numéro&gt;</c>, par exemple <c>Tir_1.mp4</c> pour la
    /// première fenêtre du tir ; sans vidéo, le cadre affiche « Vidéo à venir ». Comme le reste du menu, tout se clique
    /// au rayon de la main ou se déclenche d'une flèche (<see cref="MenuArrowTarget"/>). La place et la taille du grand
    /// écran se règlent sur le <see cref="MainMenuPanel"/>. Il se ferme quand on quitte la page ou que la partie commence.
    /// </remarks>
    public class TutorialMenu : MonoBehaviour
    {
        const string k_VideoFolder = "Tutoriel/";

        // Le grand écran est dessiné après les autres interfaces (rang 0 dans la scène : panneaux de difficulté, menu…),
        // pour qu'aucune ne passe par-dessus ; les messages devant les yeux (rang 100) restent devant lui.
        const int k_ScreenSortingBoost = 50;

        // Mise en page du grand écran (unités du Canvas ; la largeur, 1600, fait la largeur réglée en mètres).
        static readonly Vector2 k_ScreenSize = new Vector2(1600f, 1100f);
        static readonly Vector2 k_VideoSize = new Vector2(1120f, 630f);
        const float k_VideoBorder = 18f;

        MainMenuPanel m_Menu;
        TutorialContent m_Content;
        GameObject m_Sections;
        GameObject m_Screen;
        TMP_Text m_Title;
        TMP_Text m_Text;
        TMP_Text m_Counter;
        TMP_Text m_Placeholder;
        RawImage m_Video;
        Button m_Previous;
        Button m_Next;
        VideoPlayer m_Player;
        RenderTexture m_Texture;
        float m_ScreenAngle;
        float m_ScreenDistance;
        float m_ScreenHeight;
        int m_Section = -1;
        int m_Index;

        /// <summary>
        /// Fabrique la page sous <paramref name="parent"/> (le contenu du menu), cachée, et le grand écran, caché, large
        /// de <paramref name="width"/> m : vu du joueur, <paramref name="angle"/> degrés à droite du menu, à
        /// <paramref name="distance"/> m de sa tête. Rend son composant.
        /// </summary>
        public static TutorialMenu Create(Transform parent, MainMenuPanel menu, float angle, float distance, float width)
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
            tutorial.m_ScreenAngle = angle;
            tutorial.m_ScreenDistance = distance;
            tutorial.m_ScreenHeight = Mathf.Max(0.5f, width) * k_ScreenSize.y / k_ScreenSize.x;
            tutorial.BuildSections(root);
            tutorial.BuildScreen(parent.GetComponentInParent<Canvas>(), width);

            var theme = UiTheme.Load();
            if (theme != null)
                UiThemer.Apply(root.gameObject, theme);

            root.gameObject.SetActive(false);
            return tutorial;
        }

        void OnEnable()
        {
            if (m_Sections != null)
                ShowSections();
        }

        // La page quittée (« Retour », début de la partie) : le grand écran se ferme aussi.
        void OnDisable() => CloseScreen();

        void OnDestroy()
        {
            if (m_Screen != null)
                Destroy(m_Screen);
            if (m_Texture != null)
            {
                m_Texture.Release();
                Destroy(m_Texture);
            }
        }

        // ---------------------------------------------------------------- navigation

        /// <summary>La liste des catégories, sans grand écran.</summary>
        public void ShowSections()
        {
            CloseScreen();
            m_Sections.SetActive(true);
        }

        /// <summary>Ouvre une catégorie sur le grand écran, à sa première fenêtre.</summary>
        public void OpenSection(int section)
        {
            if (m_Content == null || section < 0 || section >= m_Content.sections.Count || m_Content.sections[section].windows.Count == 0)
                return;

            m_Section = section;
            m_Index = 0;
            PlaceScreen();
            m_Screen.SetActive(true);
            ShowWindow();
        }

        public void Previous() => Step(-1);

        public void Next() => Step(1);

        void CloseScreen()
        {
            StopVideo();
            m_Section = -1;
            if (m_Screen != null)
                m_Screen.SetActive(false);
        }

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

        // À droite du menu, vu du joueur : sur un cercle autour de sa tête, tourné de m_ScreenAngle vers la droite depuis
        // la direction du menu, à m_ScreenDistance de lui ; le bas de l'écran au niveau du bas du menu. Droit, face à lui.
        void PlaceScreen()
        {
            var menu = m_Menu != null ? m_Menu.transform : transform;
            var rig = PlayerRig.Instance;
            var head = rig != null && rig.Head != null ? rig.Head.position : menu.position - menu.forward * 2f;
            var toMenu = Vector3.ProjectOnPlane(menu.position - head, Vector3.up);
            if (toMenu.sqrMagnitude < 1e-4f)
                toMenu = Vector3.ProjectOnPlane(menu.forward, Vector3.up);
            if (toMenu.sqrMagnitude < 1e-4f)
                toMenu = Vector3.forward;

            var direction = Quaternion.Euler(0f, m_ScreenAngle, 0f) * toMenu.normalized;
            var center = head + direction * m_ScreenDistance;
            center.y = MenuBottom(menu) + m_ScreenHeight * 0.5f;
            m_Screen.transform.SetPositionAndRotation(center, Quaternion.LookRotation(direction, Vector3.up));
        }

        static readonly Vector3[] s_Corners = new Vector3[4];

        static float MenuBottom(Transform menu)
        {
            if (!(menu is RectTransform rect))
                return menu.position.y - 0.4f;

            rect.GetWorldCorners(s_Corners);
            return Mathf.Min(Mathf.Min(s_Corners[0].y, s_Corners[1].y), Mathf.Min(s_Corners[2].y, s_Corners[3].y));
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
                var button = NewButton(list, section.title, section.title, 34f, () => OpenSection(index));
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

            var back = NewButton(list, "Back", "Retour", 34f, () =>
            {
                if (m_Menu != null)
                    m_Menu.ShowPage(0);
            });
            back.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;
            m_Sections = list.gameObject;
        }

        // Le grand écran : un Canvas à part dans le monde (comme le menu : clic au rayon, flèches), avec son fond, le titre,
        // le cadre de la vidéo, le texte, et en bas « Fermer », « < », « 1 / 4 », « > ».
        void BuildScreen(Canvas menuCanvas, float width)
        {
            var screen = new GameObject("Tutoriel (grand écran)", typeof(RectTransform));
            screen.layer = gameObject.layer;
            m_Screen = screen;
            var rect = (RectTransform)screen.transform;
            rect.sizeDelta = k_ScreenSize;
            rect.localScale = Vector3.one * (Mathf.Max(0.5f, width) / k_ScreenSize.x);

            var canvas = screen.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = (menuCanvas != null ? menuCanvas.sortingOrder : 0) + k_ScreenSortingBoost;
            if (menuCanvas != null)
            {
                canvas.worldCamera = menuCanvas.worldCamera;
                canvas.additionalShaderChannels = menuCanvas.additionalShaderChannels;
            }

            screen.AddComponent<CanvasScaler>();
            screen.AddComponent<GraphicRaycaster>();
            screen.AddComponent<TrackedDeviceGraphicRaycaster>();

            // Un fond plein, opaque, sous le panneau du thème (peint à 94 %) : on ne voit plus rien à travers.
            var backing = NewRect("Fond opaque", rect);
            Stretch(backing, 12f);
            var backingImage = backing.gameObject.AddComponent<Image>();
            backingImage.color = new Color(0.07f, 0.05f, 0.15f, 1f);
            backingImage.raycastTarget = false;
            AddPanelFx(backing.gameObject);

            var content = NewRect("Content", rect);
            Stretch(content, 0f);
            var contentImage = content.gameObject.AddComponent<Image>();
            contentImage.color = new Color(0.08f, 0.07f, 0.12f, 1f);

            m_Title = NewText(content, "Title", "", 60f, TextAlignmentOptions.Center);
            Place(m_Title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -65f), new Vector2(1300f, 90f));

            var frameSize = k_VideoSize + Vector2.one * (2f * k_VideoBorder);
            var frame = NewRect("Video Frame", content);
            Place(frame, new Vector2(0.5f, 1f), new Vector2(0f, -125f - frameSize.y * 0.5f), frameSize);
            var frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.color = new Color(0.06f, 0.05f, 0.12f, 1f);
            frameImage.raycastTarget = false;

            var video = NewRect("Video", frame);
            Stretch(video, k_VideoBorder);
            m_Video = video.gameObject.AddComponent<RawImage>();
            m_Video.raycastTarget = false;
            m_Placeholder = NewText(frame, "Label", "Vidéo à venir", 48f, TextAlignmentOptions.Center);
            Stretch(m_Placeholder.rectTransform, k_VideoBorder);

            m_Text = NewText(content, "Text", "", 40f, TextAlignmentOptions.Center);
            m_Text.enableAutoSizing = true;
            m_Text.fontSizeMin = 28f;
            m_Text.fontSizeMax = 40f;
            Place(m_Text.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -125f - frameSize.y - 75f), new Vector2(1400f, 130f));

            var close = NewButton(content, "Close", "Fermer", 44f, ShowSections);
            Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(-520f, 70f), new Vector2(320f, 96f));
            m_Previous = NewButton(content, "Previous", "<", 44f, Previous);
            Place((RectTransform)m_Previous.transform, new Vector2(0.5f, 0f), new Vector2(180f, 70f), new Vector2(120f, 96f));
            m_Counter = NewText(content, "Value", "1 / 1", 44f, TextAlignmentOptions.Center);
            Place(m_Counter.rectTransform, new Vector2(0.5f, 0f), new Vector2(330f, 70f), new Vector2(180f, 96f));
            m_Next = NewButton(content, "Next", ">", 44f, Next);
            Place((RectTransform)m_Next.transform, new Vector2(0.5f, 0f), new Vector2(480f, 70f), new Vector2(120f, 96f));

            var theme = UiTheme.Load();
            if (theme != null)
            {
                UiThemer.Apply(screen, theme);
                UiThemer.ApplyPanel(frameImage, theme, 14f, 1f);
                var color = contentImage.color;
                color.a = 1f;
                contentImage.color = color;
                backingImage.color = new Color(theme.panelColor.r, theme.panelColor.g, theme.panelColor.b, 1f);
            }

            AddPanelFx(content.gameObject);
            MenuArrowTarget.AttachAll(screen);
            screen.SetActive(false);

            // La vidéo : en boucle, sans le son, dans une texture affichée par le cadre.
            m_Texture = new RenderTexture(1280, 720, 0) { name = "Vidéo du tutoriel" };
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

        // Un bouton des menus : le thème le colore selon son rôle (« Retour », « Fermer », « < », « > »…), et lui donne
        // ses effets.
        static Button NewButton(Transform parent, string name, string label, float fontSize, UnityAction onClick)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.25f, 0.45f, 0.95f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(onClick);
            var text = NewText(rect, "Text (TMP)", label, fontSize, TextAlignmentOptions.Center);
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
