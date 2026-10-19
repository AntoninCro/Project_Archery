using System.Collections.Generic;
using Archery.Bows;
using Archery.UI;
using Archery.Waves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Archery.BowClasses
{
    /// <summary>
    /// Le menu des arcs, affiché avant la première vague : une carte par arc de la <see cref="BowLoadout"/>, avec son
    /// modèle 3D qui tourne, ses caractéristiques, et soit « Équipé » / « Choisir », soit le succès qui le débloque et
    /// sa progression. On choisit un arc en le visant au rayon de la main (gâchette), en tirant dessus, ou sans casque
    /// avec la touche E. Il disparaît dès que la partie commence.
    /// </summary>
    /// <remarks>
    /// Tout le menu est fabriqué en jeu, avec le thème des menus (<see cref="UiTheme"/>) : il suffit de placer cet objet
    /// dans la scène, tourné vers le joueur. Dans la vue Scene, un rectangle jaune montre sa taille et sa place.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BowSelectionMenu : MonoBehaviour
    {
        const string k_ContentName = "Content";

        [Tooltip("Optionnel : la liste des arcs. Vide : celle de la scène (Bow Loadout).")]
        [SerializeField]
        BowLoadout m_Loadout;

        [Header("Mise en page")]
        [SerializeField]
        string m_Title = "Choisis ton arc";

        [Tooltip("Taille du menu en unités du Canvas (1 unité = 1 mm avec la taille de pixel par défaut).")]
        [SerializeField]
        Vector2 m_Size = new Vector2(1700f, 960f);

        [Tooltip("Taille d'une unité du Canvas, en mètres.")]
        [SerializeField]
        float m_PixelSize = 0.001f;

        [Tooltip("Largeur d'une carte (unités du Canvas).")]
        [SerializeField]
        float m_CardWidth = 250f;

        [Tooltip("Hauteur d'une carte (unités du Canvas).")]
        [SerializeField]
        float m_CardHeight = 720f;

        [SerializeField]
        float m_CardSpacing = 20f;

        [Header("Aperçu 3D")]
        [Tooltip("Hauteur (m) du plus grand arc dans sa carte. Les autres gardent leur taille relative (l'arc court est plus petit).")]
        [SerializeField]
        float m_PreviewHeight = 0.2f;

        [Tooltip("Vitesse de rotation des arcs (degrés par seconde).")]
        [SerializeField]
        float m_PreviewSpin = 30f;

        [Tooltip("Couleur des arcs verrouillés (une silhouette sombre).")]
        [SerializeField]
        Color m_LockedTint = new Color(0.08f, 0.08f, 0.1f);

        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        class Card
        {
            public BowLoadout.Entry Entry;
            public RectTransform Root;
            public Image Background;
            public Image Selection;
            public TMP_Text State;
            public GameObject LockedGroup;
            public Image Progress;
            public TMP_Text ProgressText;
            public Transform Pivot;
            public Renderer[] PreviewRenderers = new Renderer[0];
            public BowGlow[] PreviewGlows = new BowGlow[0];
        }

        readonly List<Card> m_Cards = new List<Card>();
        GameObject m_Content;
        GameObject m_Backing;
        UiTheme m_Theme;
        MaterialPropertyBlock m_Block;
        int m_Layer;

        void Start()
        {
            if (m_Loadout == null)
                m_Loadout = BowLoadout.Instance;
            if (m_Loadout == null)
            {
                Debug.LogWarning("BowSelectionMenu : aucun Bow Loadout dans la scène, le menu des arcs n'est pas créé.", this);
                enabled = false;
                return;
            }

            m_Theme = UiTheme.Load();
            m_Block = new MaterialPropertyBlock();
            var uiLayer = LayerMask.NameToLayer("UI");
            m_Layer = uiLayer >= 0 ? uiLayer : gameObject.layer;

            Build();
            Refresh();
            BowLoadout.Changed += OnLoadoutChanged;
            AchievementManager.Changed += Refresh;
        }

        void OnDestroy()
        {
            BowLoadout.Changed -= OnLoadoutChanged;
            AchievementManager.Changed -= Refresh;
        }

        void Update()
        {
            if (m_Content == null)
                return;

            // Visible seulement avant la première vague, comme le menu principal.
            var waves = WaveManager.Instance;
            var visible = waves == null || waves.Phase == WavePhase.Ready;
            if (m_Content.activeSelf != visible)
            {
                m_Content.SetActive(visible);
                if (m_Backing != null)
                    m_Backing.SetActive(visible);
                if (visible)
                    Refresh();
            }

            if (!visible)
                return;

            var angle = Time.time * m_PreviewSpin;
            foreach (var card in m_Cards)
            {
                if (card.Pivot != null)
                    card.Pivot.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        void OnLoadoutChanged(BowDefinition bow) => Refresh();

        // ---------------------------------------------------------------- état des cartes

        void Refresh()
        {
            // Caché pendant les vagues : il se met à jour quand il réapparaît (Update).
            if (m_Loadout == null || (m_Content != null && !m_Content.activeSelf))
                return;

            var achievements = AchievementManager.Instance;
            foreach (var card in m_Cards)
            {
                var entry = card.Entry;
                var exists = entry.bow != null;
                var unlocked = m_Loadout.IsUnlocked(entry);
                var selected = exists && entry.bow == m_Loadout.Selected;

                card.Selection.gameObject.SetActive(selected);
                card.Background.color = Darken(PanelColor, unlocked ? 1f : 0.7f);

                if (card.LockedGroup != null)
                    card.LockedGroup.SetActive(exists && !unlocked);

                if (!exists)
                {
                    SetState(card, "Bientôt", new Color(0.7f, 0.65f, 0.85f));
                }
                else if (!unlocked)
                {
                    SetState(card, "", Color.white);
                    var progress = achievements != null ? achievements.ProgressOf(entry.unlockedBy) : 0;
                    var target = entry.unlockedBy != null ? entry.unlockedBy.target : 1;
                    SetFill(card.Progress, target > 0 ? progress / (float)target : 0f);
                    card.ProgressText.text = progress + " / " + target;
                }
                else if (selected)
                {
                    SetState(card, "Équipé", entry.color);
                }
                else
                {
                    SetState(card, "Choisir", Color.white);
                }

                // Un arc verrouillé n'est qu'une silhouette sombre, sans lueur.
                foreach (var glow in card.PreviewGlows)
                {
                    if (glow != null)
                        glow.enabled = unlocked;
                }

                foreach (var preview in card.PreviewRenderers)
                {
                    if (preview == null)
                        continue;
                    if (unlocked)
                    {
                        // Couleurs du matériau (la lueur des bandelettes est remise par BowGlow).
                        preview.SetPropertyBlock(null);
                    }
                    else
                    {
                        preview.GetPropertyBlock(m_Block);
                        m_Block.SetColor(k_BaseColorId, m_LockedTint);
                        m_Block.SetColor(k_ColorId, m_LockedTint);
                        m_Block.SetColor(k_EmissionColorId, Color.black);
                        preview.SetPropertyBlock(m_Block);
                    }
                }
            }
        }

        static void SetState(Card card, string text, Color color)
        {
            card.State.text = text;
            card.State.color = color;
        }

        Color PanelColor => m_Theme != null ? m_Theme.cardColor : new Color(0.12f, 0.09f, 0.25f, 0.95f);

        static Color Darken(Color color, float factor) => new Color(color.r * factor, color.g * factor, color.b * factor, color.a);

        void OnCardClicked(Card card)
        {
            if (m_Loadout != null)
                m_Loadout.TrySelect(card.Entry);
        }

        // ---------------------------------------------------------------- fabrication

        void Build()
        {
            // Le Canvas, en 3D dans le monde : le rayon des mains (et la touche E sans casque) clique ses boutons.
            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                              typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            canvasObject.layer = m_Layer;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.SetParent(transform, false);
            canvasRect.localPosition = Vector3.zero;
            canvasRect.localRotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one * m_PixelSize;
            canvasRect.sizeDelta = m_Size;
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            // Le fond : un panneau du thème (son nom, « Content », le désigne au thème).
            var content = NewRect(k_ContentName, canvasRect);
            Stretch(content, 0f);
            var background = content.gameObject.AddComponent<Image>();
            background.color = new Color(0.09f, 0.06f, 0.19f, 0.94f);
            m_Content = content.gameObject;

            var title = NewText(content, "Title", m_Title, 56f, TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 80f));

            var hint = NewText(content, "Hint", "Vise un arc et appuie sur la gâchette, ou tire dessus. Les arcs se débloquent avec leur succès.",
                               24f, TextAlignmentOptions.Center);
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(m_Size.x - 120f, 40f));

            var entries = m_Loadout.Entries;
            var count = entries.Count;
            var totalWidth = count * m_CardWidth + Mathf.Max(0, count - 1) * m_CardSpacing;
            var y = -10f;
            for (var i = 0; i < count; i++)
            {
                if (entries[i] == null)
                    continue;
                var x = -totalWidth * 0.5f + m_CardWidth * 0.5f + i * (m_CardWidth + m_CardSpacing);
                m_Cards.Add(BuildCard(content, entries[i], new Vector2(x, y)));
            }

            BuildStats();

            // Le thème : fond, titre et polices. Les cartes sont habillées à part (ci-dessous), après lui.
            if (m_Theme != null)
                UiThemer.Apply(canvasObject, m_Theme);
            MakeOpaque(background);
            foreach (var card in m_Cards)
            {
                StyleCard(card);
                AddButton(card);
            }

            BuildPreviews();

            // Les cartes se déclenchent aussi d'une flèche.
            MenuArrowTarget.AttachAll(content.gameObject);
        }

        // Opaque, comme le menu principal et le grand écran du tutoriel : le thème peint ses panneaux à 94 % ; le fond passe
        // à 100 %, devant un fond plein qui cache aussi ce qui serait dessiné après lui (les panneaux de difficulté).
        void MakeOpaque(Image background)
        {
            var color = background.color;
            color.a = 1f;
            background.color = color;
            m_Backing = OpaqueBacking.Create(background.rectTransform, color, 10f);
        }

        Card BuildCard(RectTransform parent, BowLoadout.Entry entry, Vector2 position)
        {
            var card = new Card { Entry = entry };
            var bow = entry.bow;
            var top = m_CardHeight * 0.5f;

            var root = NewRect("Card " + (bow != null ? bow.displayName : entry.comingSoonName), parent);
            Place(root, new Vector2(0.5f, 0.5f), position, new Vector2(m_CardWidth, m_CardHeight));
            card.Root = root;
            card.Background = root.gameObject.AddComponent<Image>();
            card.Background.color = PanelColor;

            // Cadre de l'arc équipé, un peu plus grand que la carte, de la couleur de l'arc.
            var selection = NewRect("Sélection", root);
            Stretch(selection, -10f);
            card.Selection = selection.gameObject.AddComponent<Image>();
            card.Selection.raycastTarget = false;
            // Sans le thème, un simple voile coloré (avec le thème : un cadre orné, voir StyleCard).
            card.Selection.color = new Color(entry.color.r, entry.color.g, entry.color.b, 0.25f);
            selection.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var name = NewText(root, "Name", bow != null ? bow.displayName : entry.comingSoonName, 34f, TextAlignmentOptions.Center);
            Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, top - 45f), new Vector2(m_CardWidth - 20f, 60f));

            // Zone de l'aperçu 3D (le modèle est ajouté plus tard, devant).
            var previewBack = NewRect("Fond aperçu", root);
            Place(previewBack, new Vector2(0.5f, 0.5f), new Vector2(0f, top - 190f), new Vector2(m_CardWidth - 30f, 220f));
            var previewImage = previewBack.gameObject.AddComponent<Image>();
            previewImage.color = new Color(0f, 0f, 0f, 0.35f);
            previewImage.raycastTarget = false;
            var pivot = new GameObject("Aperçu").transform;
            pivot.SetParent(previewBack, false);
            pivot.localPosition = new Vector3(0f, 0f, -70f);
            card.Pivot = pivot;

            if (bow == null)
            {
                var unknown = NewText(previewBack, "Inconnu", "?", 140f, TextAlignmentOptions.Center);
                Stretch(unknown.rectTransform, 0f);
                unknown.color = new Color(0.7f, 0.65f, 0.85f);
            }
            else
            {
                // Caractéristiques : des jauges remplies d'après le meilleur arc de la liste. Pas de texte de description
                // (retiré le 19 octobre) ; dessous, son pouvoir spécial s'il en a un.
                AddStat(root, "Dégâts", 0, entry.color, top);
                AddStat(root, "Portée", 1, entry.color, top);
                AddStat(root, "Cadence", 2, entry.color, top);

                var power = bow.SpecialPowerText;
                if (!string.IsNullOrEmpty(power))
                {
                    var powerText = NewText(root, "Subtitle", "Pouvoir : " + power, 22f, TextAlignmentOptions.Center);
                    powerText.enableAutoSizing = true;
                    powerText.fontSizeMin = 15f;
                    powerText.fontSizeMax = 22f;
                    powerText.color = entry.color;
                    Place(powerText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, top - 465f), new Vector2(m_CardWidth - 24f, 46f));
                }
            }

            // Bas de la carte : « Équipé », « Choisir » ou « Bientôt ».
            card.State = NewText(root, "Label", "", 32f, TextAlignmentOptions.Center);
            Place(card.State.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -top + 50f), new Vector2(m_CardWidth - 20f, 60f));

            // Verrouillé : le succès à faire et sa progression.
            if (bow != null && entry.unlockedBy != null)
            {
                var locked = NewRect("Verrouillé", root);
                Stretch(locked, 0f);
                card.LockedGroup = locked.gameObject;

                var label = NewText(locked, "Label", "Verrouillé", 24f, TextAlignmentOptions.Center);
                label.color = new Color(0.75f, 0.72f, 0.8f);
                Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -top + 210f), new Vector2(m_CardWidth - 20f, 34f));

                var achievementTitle = NewText(locked, "Value", entry.unlockedBy.title, 24f, TextAlignmentOptions.Center);
                achievementTitle.color = entry.unlockedBy.color;
                Place(achievementTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -top + 175f), new Vector2(m_CardWidth - 20f, 34f));

                var achievementText = NewText(locked, "Objectif", entry.unlockedBy.description, 18f, TextAlignmentOptions.Center);
                achievementText.enableAutoSizing = true;
                achievementText.fontSizeMin = 13f;
                achievementText.fontSizeMax = 18f;
                Place(achievementText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -top + 130f), new Vector2(m_CardWidth - 26f, 58f));

                var track = NewRect("Progression", locked);
                Place(track, new Vector2(0.5f, 0.5f), new Vector2(0f, -top + 80f), new Vector2(m_CardWidth - 50f, 18f));
                var trackImage = track.gameObject.AddComponent<Image>();
                trackImage.color = new Color(0.04f, 0.03f, 0.1f, 0.95f);
                trackImage.raycastTarget = false;
                if (m_Theme != null)
                    UiThemer.ApplyGaugeTrack(trackImage, m_Theme);

                var fill = NewRect("Remplissage", track);
                Stretch(fill, 2f);
                card.Progress = fill.gameObject.AddComponent<Image>();
                card.Progress.color = entry.unlockedBy.color;
                card.Progress.raycastTarget = false;
                if (m_Theme != null)
                    UiThemer.ApplyGaugeFill(card.Progress, m_Theme, entry.unlockedBy.color);

                card.ProgressText = NewText(locked, "Value", "0 / 0", 20f, TextAlignmentOptions.Center);
                Place(card.ProgressText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -top + 45f), new Vector2(m_CardWidth - 20f, 34f));
            }

            return card;
        }

        // Toute la carte est un bouton. Ajouté après le thème, qui habillerait sinon la carte comme un bouton ordinaire.
        void AddButton(Card card)
        {
            var button = card.Root.gameObject.AddComponent<Button>();
            button.targetGraphic = card.Background;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => OnCardClicked(card));
        }

        // Une ligne de caractéristique : le nom à gauche, la jauge à droite (remplie par BuildStats).
        void AddStat(RectTransform card, string label, int row, Color color, float top)
        {
            var y = top - 335f - row * 40f;
            var text = NewText(card, "Label", label, 20f, TextAlignmentOptions.Left);
            Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-m_CardWidth * 0.25f + 2f, y), new Vector2(m_CardWidth * 0.5f - 16f, 32f));

            var track = NewRect("Jauge " + label, card);
            Place(track, new Vector2(0.5f, 0.5f), new Vector2(m_CardWidth * 0.22f, y), new Vector2(m_CardWidth * 0.44f, 14f));
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(0.04f, 0.03f, 0.1f, 0.95f);
            trackImage.raycastTarget = false;

            var fill = NewRect("Remplissage", track);
            Stretch(fill, 2f);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = color;
            fillImage.raycastTarget = false;
        }

        // Les jauges : chaque valeur par rapport à la meilleure de la liste (dégâts, vitesse des flèches, anneau rapide).
        void BuildStats()
        {
            float maxDamage = 0f, maxSpeed = 0f, maxRate = 0f;
            foreach (var card in m_Cards)
            {
                var bow = card.Entry.bow;
                if (bow == null)
                    continue;
                maxDamage = Mathf.Max(maxDamage, bow.damage);
                maxSpeed = Mathf.Max(maxSpeed, bow.arrowSpeed);
                maxRate = Mathf.Max(maxRate, Rate(bow));
            }

            foreach (var card in m_Cards)
            {
                var bow = card.Entry.bow;
                if (bow == null)
                    continue;
                SetStat(card, "Dégâts", maxDamage > 0f ? bow.damage / maxDamage : 0f);
                SetStat(card, "Portée", maxSpeed > 0f ? bow.arrowSpeed / maxSpeed : 0f);
                SetStat(card, "Cadence", maxRate > 0f ? Rate(bow) / maxRate : 0f);
            }
        }

        // Plus l'anneau est court et la corde courte à tendre, plus on tire vite.
        static float Rate(BowDefinition bow) => 1f / Mathf.Max(0.1f, bow.ringDuration) * (0.2625f / Mathf.Max(0.05f, bow.maxDrawDistance));

        void SetStat(Card card, string label, float value)
        {
            var track = card.Root.Find("Jauge " + label);
            var fill = track != null ? track.Find("Remplissage") : null;
            if (fill != null)
                SetFill(fill.GetComponent<Image>(), value);
        }

        // Une jauge se remplit de gauche à droite (l'image garde ses coins nets).
        static void SetFill(Image fill, float value)
        {
            if (fill == null)
                return;
            var rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp(value, 0.04f, 1f), 1f);
        }

        // Le fond et le cadre orné de la carte, le cadre de sélection, les polices des noms ; la réaction quand on la vise.
        void StyleCard(Card card)
        {
            if (m_Theme == null)
                return;

            UiThemer.ApplyPanel(card.Background, m_Theme, 16f);
            card.Background.color = PanelColor;
            if (m_Theme.frameBorder != null)
            {
                card.Selection.sprite = m_Theme.frameBorder;
                card.Selection.type = Image.Type.Sliced;
                card.Selection.pixelsPerUnitMultiplier = m_Theme.frameScale;
                card.Selection.color = Color.Lerp(card.Entry.color, Color.white, 0.25f);
            }

            var name = card.Root.Find("Name");
            if (name != null)
                UiThemer.ApplyTitle(name.GetComponent<TMP_Text>(), m_Theme, false);
            UiThemer.ApplyLabel(card.State, m_Theme);

            foreach (var track in card.Root.GetComponentsInChildren<Image>(true))
            {
                if (track.name.StartsWith("Jauge"))
                    UiThemer.ApplyGaugeTrack(track, m_Theme);
                else if (track.name == "Remplissage" && track.transform.parent.name.StartsWith("Jauge"))
                    UiThemer.ApplyGaugeFill(track, m_Theme, card.Entry.color);
            }

            var border = card.Root.Find(UiThemer.BorderName);
            var fx = card.Root.gameObject.AddComponent<UiButtonFx>();
            fx.Configure(card.Background, border != null ? border.GetComponent<Image>() : null, false, 1.03f,
                         m_Theme.hoverBorderColor, m_Theme.hoverClip, m_Theme.hoverVolume, null, 0f);
        }

        // Le modèle de chaque arc, devant sa carte, à la même échelle pour tous.
        void BuildPreviews()
        {
            var heights = new Dictionary<Card, float>();
            var tallest = 0f;
            foreach (var card in m_Cards)
            {
                var visual = card.Entry.bow != null ? card.Entry.bow.visualPrefab : null;
                if (visual == null || card.Pivot == null)
                    continue;

                // Le temps de mesurer : 1 unité du pivot = 1 m (la carte compte en unités du Canvas).
                card.Pivot.localScale = Vector3.one / Mathf.Max(1e-5f, m_PixelSize);
                var instance = Instantiate(visual, card.Pivot, false);
                instance.name = visual.name + " (aperçu)";
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;

                card.PreviewRenderers = instance.GetComponentsInChildren<Renderer>(true);
                card.PreviewGlows = instance.GetComponentsInChildren<BowGlow>(true);
                if (!TryGetBounds(card.PreviewRenderers, out var bounds))
                    continue;

                // Centré sur le pivot, pour tourner sur lui-même.
                var scale = card.Pivot.lossyScale.y;
                var center = card.Pivot.InverseTransformPoint(bounds.center);
                instance.transform.localPosition -= center;
                var height = bounds.size.y / Mathf.Max(1e-5f, scale);
                heights[card] = height;
                tallest = Mathf.Max(tallest, height);
            }

            if (tallest <= 0f)
                return;

            var factor = m_PreviewHeight / tallest;
            foreach (var card in heights.Keys)
                card.Pivot.localScale *= factor;
        }

        static bool TryGetBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            var found = false;
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        // ---------------------------------------------------------------- outils d'interface

        RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = m_Layer;
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

        TMP_Text NewText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment)
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

        // Dans la vue Scene : la place et la taille du menu (il n'existe qu'en jeu).
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(m_Size.x * m_PixelSize, m_Size.y * m_PixelSize, 0.01f));
            // Le côté visible regarde vers -Z : une petite flèche montre où doit être le joueur.
            Gizmos.DrawLine(Vector3.zero, new Vector3(0f, 0f, -0.3f));
        }
    }
}
