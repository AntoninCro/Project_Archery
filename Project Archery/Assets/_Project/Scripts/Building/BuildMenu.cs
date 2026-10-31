using System.Collections.Generic;
using Archery.Core;
using Archery.Player;
using Archery.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Building
{
    /// <summary>
    /// Le menu des plans (28 octobre, demandé par le joueur après ses tests) : le bouton « Plans » de la montre ouvre devant
    /// lui un carrousel de cartes, une par bâtiment dont il a des plans, avec son aperçu 3D qui tourne, son nom, ses plans en
    /// poche et où il se pose. Depuis le 28 octobre au soir, il reste ouvert pendant la pose : la carte du milieu est le
    /// bâtiment de l'hologramme, déjà sorti (<see cref="BuildPlacement"/>), et le joystick (gauche, droite) de la main qui porte
    /// la montre en change ; la gâchette pose, sans choix avant. On peut aussi toucher une carte avec une manette et rester un
    /// court instant dessus. Il flotte devant le joueur, un peu au-dessus du regard, et le rejoint quand il se tourne.
    /// </summary>
    /// <remarks>
    /// Fabriqué par <see cref="BuildPlacement"/> (rien à poser dans la scène). Les fonds des cartes utilisent le matériau de la
    /// grille (<c>Resources/BuildGrid</c>), les textes les polices du thème des menus. Sans casque : la molette change de carte.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BuildMenu : MonoBehaviour
    {
        const string k_MaterialResource = "BuildGrid";

        class Card
        {
            public StructureDefinition Definition;
            public Transform Root;
            public Transform Preview;
            public Mesh Panel;
            public TextMeshPro Count;
            public float Touch;
            public bool Touched;
            public bool Blocked;
            public float Shown = -1f;
        }

        [Header("Place")]
        [Tooltip("Distance (m) entre la tête et le menu, en casque.")]
        [SerializeField]
        float m_Distance = 0.5f;

        [Tooltip("Distance (m) entre la caméra et le menu, sans casque.")]
        [SerializeField]
        float m_DesktopDistance = 0.85f;

        [Tooltip("Hauteur (m) du menu sous les yeux (négative : au-dessus). 28 octobre au soir : un peu au-dessus du regard (20 cm " +
                 "dessous avant), le menu reste ouvert pendant la pose et ne doit pas cacher le sol visé.")]
        [SerializeField]
        float m_Drop = -0.06f;

        [Tooltip("Lissage (s) du menu qui suit le regard : il reste collé devant le joueur, en permanence (29 octobre au soir, le " +
                 "joueur ; avant, il flottait sur place puis le rejoignait d'un coup).")]
        [SerializeField]
        float m_FollowTime = 0.06f;

        [Header("Cartes")]
        [SerializeField]
        Vector2 m_CardSize = new Vector2(0.12f, 0.16f);

        [Tooltip("Écart (m) entre deux cartes, de centre à centre.")]
        [SerializeField]
        float m_Spacing = 0.135f;

        [Tooltip("Cartes visibles de chaque côté de celle du milieu.")]
        [SerializeField]
        int m_SideCards = 2;

        [Tooltip("Taille (m) de l'aperçu 3D dans une carte.")]
        [SerializeField]
        float m_PreviewSize = 0.085f;

        [Tooltip("Temps (s) à toucher une carte avec une manette pour la choisir.")]
        [SerializeField]
        float m_TouchHold = 0.3f;

        [Header("Couleurs")]
        [SerializeField]
        Color m_PanelColor = new Color(0.08f, 0.05f, 0.16f, 1f);

        [SerializeField]
        Color m_CommonColor = new Color(0.78f, 0.8f, 0.88f, 1f);

        [SerializeField]
        Color m_RareColor = new Color(0.4f, 0.62f, 1f, 1f);

        [SerializeField]
        Color m_LegendaryColor = new Color(1f, 0.78f, 0.28f, 1f);

        readonly List<Card> m_Cards = new List<Card>();
        readonly List<Vector3> m_Vertices = new List<Vector3>();
        readonly List<Color> m_Colors = new List<Color>();
        readonly List<Vector2> m_Uv = new List<Vector2>();
        readonly List<Vector4> m_Cell = new List<Vector4>();
        readonly List<int> m_Triangles = new List<int>();

        BuildPlacement m_Owner;
        Transform m_Root;
        Material m_Material;
        UiTheme m_Theme;
        TextMeshPro m_Title;
        TextMeshPro m_Description;
        TextMeshPro m_Hint;
        TextMeshPro m_Left;
        TextMeshPro m_Right;
        XRBaseInputInteractor m_Hand;
        bool m_Desktop;
        int m_Index;
        float m_Scroll;
        int m_DescribedIndex = -1;
        bool m_Placed;
        Vector3 m_Velocity;
        float m_OpenedAt;

        /// <summary>Le menu est ouvert.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>Le bâtiment de la carte du milieu, ou null.</summary>
        public StructureDefinition Current => m_Index >= 0 && m_Index < m_Cards.Count ? m_Cards[m_Index].Definition : null;

        internal void Setup(BuildPlacement owner) => m_Owner = owner;

        /// <summary>Ouvre le menu sur ces bâtiments, la carte <paramref name="index"/> au milieu, devant le joueur.</summary>
        public void Open(IReadOnlyList<StructureDefinition> definitions, int index, XRBaseInputInteractor hand, bool desktop)
        {
            Close();
            if (definitions == null || definitions.Count == 0)
                return;

            EnsureRoot();
            m_Hand = hand;
            m_Desktop = desktop;
            foreach (var definition in definitions)
                m_Cards.Add(CreateCard(definition));

            m_Index = Mathf.Clamp(index, 0, m_Cards.Count - 1);
            m_Scroll = m_Index;
            m_DescribedIndex = -1;
            m_Placed = false;
            m_OpenedAt = Time.unscaledTime;
            m_Hint.text = desktop ? "Molette : changer · Maj + molette : tourner · E : poser · F2 : fermer"
                : "Joystick gauche, droite : changer · haut, bas : tourner" +
                  "\n<size=80%>Gâchette : poser · Poignée : fermer · ou touche une carte</size>";
            m_Root.gameObject.SetActive(true);
            IsOpen = true;
            PlaceInFront(true);
            Layout(0f);
        }

        public void Close()
        {
            IsOpen = false;
            foreach (var card in m_Cards)
            {
                if (card.Root != null)
                    Destroy(card.Root.gameObject);
                if (card.Panel != null)
                    Destroy(card.Panel);
            }

            m_Cards.Clear();
            if (m_Root != null)
                m_Root.gameObject.SetActive(false);
        }

        /// <summary>La carte suivante (1) ou précédente (-1). Faux au bout de la rangée.</summary>
        public bool Scroll(int direction)
        {
            var next = Mathf.Clamp(m_Index + direction, 0, m_Cards.Count - 1);
            if (next == m_Index)
                return false;

            m_Index = next;
            return true;
        }

        void OnDestroy()
        {
            Close();
            if (m_Root != null)
                Destroy(m_Root.gameObject);
        }

        void LateUpdate()
        {
            if (!IsOpen || m_Root == null)
                return;

            var deltaTime = Time.unscaledDeltaTime;
            m_Scroll = Mathf.Lerp(m_Scroll, m_Index, 1f - Mathf.Exp(-14f * deltaTime));
            PlaceInFront(false);
            Layout(deltaTime);
            UpdateTouch(deltaTime);
        }

        // ---------------------------------------------------------------- place et disposition

        Transform Head()
        {
            if (m_Desktop && DesktopControls.Instance != null && DesktopControls.Instance.CameraTransform != null)
                return DesktopControls.Instance.CameraTransform;
            var camera = Camera.main;
            return camera != null ? camera.transform : null;
        }

        // Devant le joueur, à hauteur des yeux, tourné vers lui. Il flotte où il est ; quand le joueur tourne la tête ou s'éloigne,
        // il attend un instant, puis le rejoint en douceur jusque devant lui (28 octobre au soir, demandé par le joueur : avant,
        // il revenait seulement au bord de son angle, et restait collé au bord de la vue).
        void PlaceInFront(bool now)
        {
            var head = Head();
            if (head == null)
                return;

            var forward = head.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            var distance = m_Desktop ? m_DesktopDistance : m_Distance;
            var target = head.position + forward * distance + Vector3.down * (m_Desktop ? m_Drop * 0.5f : m_Drop);

            if (now || !m_Placed)
            {
                m_Root.position = target;
                m_Placed = true;
                m_Velocity = Vector3.zero;
            }
            else
            {
                // Collé devant le joueur : il suit le regard à chaque image, juste lissé pour ne pas trembler avec la tête.
                m_Root.position = Vector3.SmoothDamp(m_Root.position, target, ref m_Velocity, m_FollowTime, Mathf.Infinity,
                                                     Time.unscaledDeltaTime);
            }

            var look = m_Root.position - head.position;
            if (look.sqrMagnitude > 1e-4f)
                m_Root.rotation = Quaternion.LookRotation(look, Vector3.up);
        }

        // Les cartes en arc : celle du milieu en grand, les autres plus petites, plus loin et tournées vers le joueur.
        void Layout(float deltaTime)
        {
            for (var i = 0; i < m_Cards.Count; i++)
            {
                var card = m_Cards[i];
                var offset = i - m_Scroll;
                var visible = Mathf.Abs(offset) <= m_SideCards + 0.5f;
                if (card.Root.gameObject.activeSelf != visible)
                    card.Root.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                var scale = Mathf.Lerp(1f, 0.7f, Mathf.Clamp01(Mathf.Abs(offset)));
                card.Root.localPosition = new Vector3(offset * m_Spacing, 0f, 0.025f * offset * offset);
                card.Root.localRotation = Quaternion.Euler(0f, offset * 12f, 0f);
                card.Root.localScale = Vector3.one * scale;
                if (card.Preview != null)
                    card.Preview.localRotation = Quaternion.Euler(-20f, 0f, 0f) * Quaternion.Euler(0f, Time.unscaledTime * 40f + i * 50f, 0f);

                // Le fond se refait quand la carte devient (ou cesse d'être) celle du milieu, ou quand on la touche.
                var selected = i == m_Index ? 1f : 0f;
                var shown = selected + Mathf.Round(card.Touch * 20f) * 0.05f + (card.Touched ? 10f : 0f);
                if (!Mathf.Approximately(shown, card.Shown))
                {
                    card.Shown = shown;
                    BuildPanel(card, i == m_Index);
                }
            }

            if (m_DescribedIndex != m_Index)
            {
                m_DescribedIndex = m_Index;
                Describe();
            }

            m_Left.gameObject.SetActive(m_Index > 0);
            m_Right.gameObject.SetActive(m_Index < m_Cards.Count - 1);
        }

        void Describe()
        {
            var definition = Current;
            var builds = BuildManager.Instance;
            var grid = BuildGrid.Instance;
            m_Title.text = m_Cards.Count > 1 ? $"Plans · {m_Index + 1} / {m_Cards.Count}" : "Plans";
            if (definition == null)
            {
                m_Description.text = "";
                return;
            }

            var footprint = grid != null ? grid.FootprintOf(definition) : Vector2Int.one;
            var where = definition.slot == BuildSlot.Tower ? "Sur la tour" : Capitalize(BuildGrid.WhereText(definition));
            var plans = builds != null ? builds.PlansOf(definition) : 0;
            var cells = definition.slot == BuildSlot.Tower ? "" : $" · {footprint.x} × {footprint.y} cases";
            m_Description.text = $"<color=#{ColorUtility.ToHtmlStringRGB(definition.color)}>{definition.displayName}</color>" +
                                 $"  <size=80%>{RarityName(definition.rarity)} · {where}{cells} · {(plans > 1 ? plans + " plans" : "1 plan")}</size>\n" +
                                 $"<size=85%>{definition.DescriptionFor(1)}</size>";
        }

        // ---------------------------------------------------------------- toucher une carte

        // Une manette dans une carte (devant ou derrière, de quelques centimètres) : elle se remplit, puis son bâtiment devient
        // celui de l'hologramme.
        void UpdateTouch(float deltaTime)
        {
            var rig = PlayerRig.Instance;
            for (var i = 0; i < m_Cards.Count; i++)
            {
                var card = m_Cards[i];
                var touched = false;
                XRBaseInputInteractor toucher = null;
                if (rig != null && !m_Desktop && card.Root.gameObject.activeSelf)
                {
                    foreach (var hand in rig.Hands)
                    {
                        if (hand == null || !hand.isActiveAndEnabled)
                            continue;

                        var local = card.Root.InverseTransformPoint(hand.transform.position);
                        if (Mathf.Abs(local.x) <= m_CardSize.x * 0.5f && Mathf.Abs(local.y) <= m_CardSize.y * 0.5f && Mathf.Abs(local.z) <= 0.06f)
                        {
                            touched = true;
                            toucher = hand;
                            break;
                        }
                    }
                }

                // Une main déjà dans une carte à l'ouverture (celle qui vient de toucher la montre) doit d'abord en sortir.
                if (touched && (card.Blocked || Time.unscaledTime < m_OpenedAt + 0.4f))
                {
                    card.Blocked = true;
                    touched = false;
                }
                else if (!touched)
                {
                    card.Blocked = false;
                }

                if (touched && !card.Touched)
                    Haptics.Pulse(toucher, 0.15f, 0.03f);
                card.Touched = touched;
                card.Touch = touched ? card.Touch + deltaTime / Mathf.Max(0.05f, m_TouchHold) : 0f;
                if (card.Touch < 1f)
                    continue;

                card.Touch = 0f;
                Haptics.Pulse(toucher, 0.5f, 0.08f);
                m_Index = i;
                if (m_Owner != null)
                    m_Owner.SelectFromMenu(card.Definition);
                return;
            }
        }

        // ---------------------------------------------------------------- fabrication

        void EnsureRoot()
        {
            if (m_Root != null)
                return;

            m_Theme = UiTheme.Load();
            m_Material = Resources.Load<Material>(k_MaterialResource);
            if (m_Material == null)
            {
                var shader = Shader.Find("Archery/BuildGrid");
                if (shader != null)
                    m_Material = new Material(shader) { name = "Fond des cartes" };
            }

            m_Root = new GameObject("Menu des plans").transform;
            m_Title = Label(m_Root, "Titre", new Vector3(0f, m_CardSize.y * 0.5f + 0.03f, 0f), 0.16f, 0.5f, true);
            m_Description = Label(m_Root, "Description", new Vector3(0f, -m_CardSize.y * 0.5f - 0.045f, 0f), 0.1f, 0.56f, false);
            m_Description.textWrappingMode = TextWrappingModes.Normal;
            m_Description.rectTransform.sizeDelta = new Vector2(0.56f, 0.06f);
            m_Hint = Label(m_Root, "Aide", new Vector3(0f, -m_CardSize.y * 0.5f - 0.1f, 0f), 0.075f, 0.6f, false);
            m_Hint.color = m_Theme != null ? m_Theme.labelColor : new Color(0.82f, 0.86f, 1f);
            var side = (m_SideCards + 0.75f) * m_Spacing;
            m_Left = Label(m_Root, "Gauche", new Vector3(-side, 0f, 0.05f), 0.3f, 0.06f, true);
            m_Left.text = "<";
            m_Right = Label(m_Root, "Droite", new Vector3(side, 0f, 0.05f), 0.3f, 0.06f, true);
            m_Right.text = ">";
            m_Root.gameObject.SetActive(false);
        }

        Card CreateCard(StructureDefinition definition)
        {
            var card = new Card { Definition = definition };
            var root = new GameObject("Carte · " + definition.displayName).transform;
            root.SetParent(m_Root, false);
            card.Root = root;

            // Le fond (fait par BuildPanel), l'aperçu 3D, puis les textes : où il se pose, les plans, le nom.
            card.Panel = new Mesh { name = "Fond · " + definition.displayName };
            var panel = new GameObject("Fond");
            panel.transform.SetParent(root, false);
            panel.AddComponent<MeshFilter>().sharedMesh = card.Panel;
            var renderer = panel.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = -1;

            if (definition.prefab != null)
            {
                var pivot = new GameObject("Aperçu").transform;
                pivot.SetParent(root, false);
                pivot.localPosition = new Vector3(0f, 0.008f, -0.045f);
                var model = BuildPlacement.CopyModel(definition.prefab, 1, null);
                FitPreview(model.transform, pivot);
                card.Preview = pivot;
            }

            var half = m_CardSize * 0.5f;
            var slot = Label(root, "Zone", new Vector3(-half.x + 0.008f, half.y - 0.014f, -0.002f), 0.06f, m_CardSize.x * 0.6f, false);
            slot.alignment = TextAlignmentOptions.Left;
            slot.rectTransform.pivot = new Vector2(0f, 0.5f);
            slot.text = SlotTag(definition);
            slot.color = SlotColor(definition);

            card.Count = Label(root, "Plans", new Vector3(half.x - 0.008f, half.y - 0.016f, -0.002f), 0.1f, m_CardSize.x * 0.5f, true);
            card.Count.alignment = TextAlignmentOptions.Right;
            card.Count.rectTransform.pivot = new Vector2(1f, 0.5f);
            var builds = BuildManager.Instance;
            card.Count.text = "×" + (builds != null ? builds.PlansOf(definition) : 0);

            var name = Label(root, "Nom", new Vector3(0f, -half.y + 0.022f, -0.002f), 0.1f, m_CardSize.x * 0.92f, true);
            name.enableAutoSizing = true;
            name.fontSizeMin = 0.055f;
            name.fontSizeMax = 0.1f;
            name.textWrappingMode = TextWrappingModes.Normal;
            name.rectTransform.sizeDelta = new Vector2(m_CardSize.x * 0.92f, 0.034f);
            name.text = definition.displayName;
            name.color = definition.color;
            return card;
        }

        // L'aperçu tient dans un cube de m_PreviewSize, centré sur le pivot (qui le fait tourner).
        void FitPreview(Transform model, Transform pivot)
        {
            model.SetParent(pivot, false);
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            var size = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (size < 1e-4f)
                return;

            var scale = m_PreviewSize / size;
            model.localScale *= scale;
            model.localPosition = -pivot.InverseTransformPoint(bounds.center) * scale;
            foreach (var renderer in renderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        // Le fond d'une carte : un panneau sombre, une bordure de la couleur de la rareté (plus épaisse au milieu), et la barre qui
        // se remplit quand on la touche.
        void BuildPanel(Card card, bool selected)
        {
            m_Vertices.Clear();
            m_Colors.Clear();
            m_Uv.Clear();
            m_Cell.Clear();
            m_Triangles.Clear();

            var size = m_CardSize;
            var panel = m_PanelColor;
            panel.a = selected ? 0.93f : 0.8f;
            AddQuad(Vector3.zero, size, panel, 0f, 1f, 0.004f);

            var border = RarityColor(card.Definition.rarity);
            if (card.Touched)
                border = Color.Lerp(border, Color.white, 0.5f);
            border.a = selected || card.Touched ? 1f : 0.7f;
            AddQuad(Vector3.zero, size, border, selected ? 0.0045f : 0.0025f, 0f, 0f);

            if (card.Touch > 0f)
            {
                var width = size.x * 0.84f * Mathf.Clamp01(card.Touch);
                var bar = new Vector2(width, 0.006f);
                AddQuad(new Vector3(-size.x * 0.42f + width * 0.5f, -size.y * 0.5f + 0.008f, -0.001f), bar, new Color(1f, 0.85f, 0.4f, 1f),
                        0f, 1f, 0f);
            }

            card.Panel.Clear();
            card.Panel.SetVertices(m_Vertices);
            card.Panel.SetColors(m_Colors);
            card.Panel.SetUVs(0, m_Uv);
            card.Panel.SetUVs(1, m_Cell);
            card.Panel.SetTriangles(m_Triangles, 0);
            card.Panel.RecalculateBounds();
        }

        // Un rectangle (centre, taille) dans le plan de la carte ; z vers l'avant (le joueur est du côté des z négatifs).
        void AddQuad(Vector3 center, Vector2 size, Color color, float line, float fill, float back)
        {
            var start = m_Vertices.Count;
            var half = size * 0.5f;
            var z = center.z + back;
            m_Vertices.Add(new Vector3(center.x - half.x, center.y - half.y, z));
            m_Vertices.Add(new Vector3(center.x + half.x, center.y - half.y, z));
            m_Vertices.Add(new Vector3(center.x - half.x, center.y + half.y, z));
            m_Vertices.Add(new Vector3(center.x + half.x, center.y + half.y, z));
            m_Uv.Add(new Vector2(0f, 0f));
            m_Uv.Add(new Vector2(1f, 0f));
            m_Uv.Add(new Vector2(0f, 1f));
            m_Uv.Add(new Vector2(1f, 1f));
            var cell = new Vector4(size.x, size.y, line, fill);
            for (var i = 0; i < 4; i++)
            {
                m_Colors.Add(color);
                m_Cell.Add(cell);
            }

            m_Triangles.Add(start);
            m_Triangles.Add(start + 2);
            m_Triangles.Add(start + 1);
            m_Triangles.Add(start + 1);
            m_Triangles.Add(start + 2);
            m_Triangles.Add(start + 3);
        }

        // Un texte du menu : la police des titres ou celle des libellés du thème, un contour sombre pour se lire sur le décor.
        TextMeshPro Label(Transform parent, string name, Vector3 position, float size, float width, bool title)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.fontSize = size;
            text.rectTransform.sizeDelta = new Vector2(width, size * 0.2f);
            if (m_Theme != null)
            {
                var font = title ? m_Theme.titleFont : m_Theme.labelFont;
                if (font != null)
                    text.font = font;
                text.color = title ? m_Theme.titleTop : m_Theme.textColor;
            }

            text.outlineWidth = 0.18f;
            text.outlineColor = new Color32(14, 6, 28, 230);
            text.sortingOrder = 2;
            return text;
        }

        // ---------------------------------------------------------------- textes

        static string SlotTag(StructureDefinition definition) => definition.anywhere ? "Partout" : definition.slot switch
        {
            BuildSlot.Lane => "Voie",
            BuildSlot.Tower => "Tour",
            _ => "Bord",
        };

        static Color SlotColor(StructureDefinition definition) => definition.anywhere ? new Color(0.85f, 0.7f, 1f) : definition.slot switch
        {
            BuildSlot.Lane => new Color(1f, 0.62f, 0.3f),
            BuildSlot.Tower => new Color(1f, 0.85f, 0.4f),
            _ => new Color(0.55f, 0.85f, 1f),
        };

        Color RarityColor(StructureRarity rarity) => rarity switch
        {
            StructureRarity.Legendary => m_LegendaryColor,
            StructureRarity.Rare => m_RareColor,
            _ => m_CommonColor,
        };

        static string RarityName(StructureRarity rarity) => rarity switch
        {
            StructureRarity.Legendary => "Légendaire",
            StructureRarity.Rare => "Rare",
            _ => "Commune",
        };

        static string Capitalize(string text) => string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
