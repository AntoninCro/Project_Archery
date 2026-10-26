using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.Shop
{
    /// <summary>
    /// L'atelier (refonte roguelike, 26 octobre, demandé par le joueur) : à côté de la boutique, la flèche du jeu tourne en 3D,
    /// avec ses pièces allumées, et un petit cadre relié à chaque partie (pointe, empennage, fût, et la corde et le talisman
    /// qui l'accompagnent). Un clic sur un cadre, ou sur la partie de la flèche, la choisit ; en dessous, les pièces possédées
    /// pour cette partie, un clic pour l'équiper. Rien n'est jamais perdu : la pièce retirée attend ici, avec ses niveaux.
    /// </summary>
    /// <remarks>
    /// Construit au lancement par le <see cref="ShopPanel"/>, à droite de son panneau et tourné vers le joueur, avec des copies
    /// de ses cartes, de son titre et de ses textes : rien à poser dans la scène, et il garde le thème des menus. La flèche est
    /// une copie des maillages du modèle des flèches (<see cref="ArrowPool.Prefab"/>), sans script ni collision, faite à la
    /// première ouverture ; ses pièces lumineuses viennent de <see cref="PieceLook.Showcase"/>. La corde est un V lumineux à
    /// l'encoche, le talisman une breloque qui pend sous le fût (grisés quand l'emplacement est vide). Une pièce prise quand
    /// son emplacement est libre s'équipe toute seule (<see cref="PlayerUpgrades.Add"/>) ; sinon, elle attend ici, et l'atelier
    /// s'ouvre sur sa partie. Une carte de pièce dit le duo que l'équiper formerait (<see cref="ArrowDuos.WouldActivate"/>).
    /// Première version le même jour : 5 cartes d'emplacement en ligne, sans flèche.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ArrowWorkshop : MonoBehaviour
    {
        // --- Réglages (px du panneau, sauf mention) ---

        // Les cartes des pièces : la carte de la boutique réduite (4 colonnes, 3 lignes).
        const float k_PieceScale = 0.66f;
        const int k_PieceColumns = 4;
        const int k_PieceRows = 3;
        const float k_Spacing = 12f;
        const float k_Margin = 26f;

        // De haut en bas : le titre, la vitrine (la flèche et ses cadres), la phrase, la grille des pièces.
        const float k_TitleHeight = 110f;
        const float k_ShowcaseHeight = 360f;
        const float k_LabelHeight = 50f;
        const float k_LabelGap = 8f;

        // La flèche : sa longueur à l'écran, son épaisseur (× la vraie, pour la lire de loin), son avance devant le panneau, sa
        // rotation autour de son axe (°/s).
        const float k_ArrowSpan = 820f;
        const float k_Thickness = 1.6f;
        const float k_ArrowDepth = 90f;
        const float k_SpinSpeed = 40f;

        // La corde (m, à l'échelle de la flèche) : la hauteur de chaque bras du V, son ouverture vers la pointe, sa largeur (m du
        // monde). La breloque : sa chute sous le fût, sa taille, sa rotation (°/s).
        const float k_StringHalfHeight = 0.04f;
        const float k_StringOpening = 0.07f;
        const float k_StringWidth = 0.005f;
        const float k_CharmDrop = 0.035f;
        const float k_CharmSize = 0.014f;
        const float k_CharmSpin = 70f;
        const float k_Glow = 1.6f;

        // Les cadres : leur taille, leur écart vertical à la flèche, l'épaisseur des traits qui les relient à elle.
        static readonly Vector2 k_FrameSize = new Vector2(196f, 92f);
        const float k_FrameOffset = 125f;
        const float k_LineWidth = 3f;

        static readonly Color k_EmptyColor = new Color(0.55f, 0.55f, 0.6f);
        static readonly Color k_DuoColor = new Color(1f, 0.8f, 0.25f);
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        // Chaque partie : son cadre (x, au-dessus ou en dessous de la flèche) et ce qu'il montre sur la flèche (de l'encoche, 0,
        // à la pointe, 1), puis la zone cliquable sur la flèche (centre x, y depuis l'axe, largeur, hauteur).
        sealed class Part
        {
            public ArrowSlot Slot;
            public Vector2 Frame;
            public Vector2 Target;
            public Rect Hotspot;

            public Button Button;
            public Image Background;
            public Image Border;
            public Image Icon;
            public TMP_Text SlotText;
            public TMP_Text PieceText;
            public Image Line;
            public Image Glow;
            public Color Color;
        }

        readonly Part[] m_Parts =
        {
            // La corde d'abord : sa zone passe sous celle de l'empennage, qui gagne là où elles se touchent.
            new Part { Slot = ArrowSlot.String, Frame = new Vector2(-290f, -1f), Target = new Vector2(-0.5f, 0f), Hotspot = new Rect(-392f, 0f, 125f, 190f) },
            new Part { Slot = ArrowSlot.Fletching, Frame = new Vector2(-300f, 1f), Target = new Vector2(-0.375f, 0f), Hotspot = new Rect(-320f, 0f, 180f, 90f) },
            new Part { Slot = ArrowSlot.Shaft, Frame = new Vector2(0f, 1f), Target = new Vector2(0f, 0f), Hotspot = new Rect(45f, 0f, 550f, 70f) },
            new Part { Slot = ArrowSlot.Tip, Frame = new Vector2(300f, 1f), Target = new Vector2(0.45f, 0f), Hotspot = new Rect(375f, 0f, 110f, 90f) },
            new Part { Slot = ArrowSlot.Talisman, Frame = new Vector2(190f, -1f), Target = new Vector2(0f, -72f), Hotspot = new Rect(0f, -72f, 80f, 70f) },
        };

        readonly List<ShopCard> m_PieceCards = new List<ShopCard>();
        readonly List<ShopOffer> m_PieceOffers = new List<ShopOffer>();
        readonly List<Upgrade> m_Pieces = new List<Upgrade>();
        MaterialPropertyBlock m_Block;
        TMP_Text m_Label;
        Action<ShopResult> m_Report;
        ArrowSlot m_Selected = ArrowSlot.Tip;
        int m_SeenOwned;
        float m_Width;
        float m_Height;
        float m_ArrowY;

        // La vitrine 3D : faite à la première ouverture (la réserve des flèches et les effets doivent exister).
        Transform m_Spinner;
        Transform m_Model;
        Transform m_Still;
        Transform m_Charm;
        MeshRenderer m_CharmRenderer;
        LineRenderer m_String;
        LineRenderer m_Thread;
        bool m_Decorated;
        bool m_DuoActive;
        Color m_StringColor;
        float m_Spin;

        /// <summary>Le panneau de l'atelier (pour éviter les murs quand la boutique se place).</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>
        /// Construit l'atelier à droite de <paramref name="parent"/> (le contenu du panneau de la boutique), écarté de
        /// <paramref name="gap"/> px et tourné de <paramref name="angle"/>° vers le joueur. Les modèles sont copiés : une carte
        /// de la boutique, son titre, un de ses textes, son fond et son cadre (optionnels, sauf la carte).
        /// <paramref name="report"/> reçoit le message d'un clic.
        /// </summary>
        public static ArrowWorkshop Create(RectTransform parent, ShopCard cardTemplate, TMP_Text titleTemplate, TMP_Text textTemplate,
                                           Image background, Transform borderTemplate, float gap, float angle, Action<ShopResult> report)
        {
            if (parent == null || cardTemplate == null)
                return null;

            var cell = CellSize(cardTemplate);
            var pieceSize = cell * k_PieceScale;
            var width = Mathf.Max(k_ArrowSpan + 2f * 60f, k_PieceColumns * pieceSize.x + (k_PieceColumns - 1) * k_Spacing + 2f * k_Margin);
            var labelTop = k_TitleHeight + k_ShowcaseHeight + k_LabelGap;
            var piecesTop = labelTop + k_LabelHeight + k_LabelGap;
            var height = piecesTop + k_PieceRows * pieceSize.y + (k_PieceRows - 1) * k_Spacing + 20f;

            var root = new GameObject("L'atelier", typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            var rect = (RectTransform)root.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(gap, 0f);
            rect.localRotation = Quaternion.Euler(0f, angle, 0f);

            // Le fond et le cadre du panneau de la boutique.
            if (background != null)
                CopyImage(root.AddComponent<Image>(), background);

            if (borderTemplate != null)
                Instantiate(borderTemplate.gameObject, rect, false).name = borderTemplate.name;

            if (titleTemplate != null)
            {
                var title = Instantiate(titleTemplate, rect, false);
                title.name = "Titre";
                title.text = "L'atelier";
            }

            var workshop = root.AddComponent<ArrowWorkshop>();
            workshop.m_Report = report;
            workshop.m_Width = width;
            workshop.m_Height = height;
            workshop.m_ArrowY = -(k_TitleHeight + k_ShowcaseHeight * 0.5f);
            workshop.BuildParts(rect, cardTemplate, textTemplate);

            // La phrase, puis la grille des pièces de la partie choisie.
            if (textTemplate != null)
            {
                var label = NewText(textTemplate, rect, "Phrase", 0f);
                var labelRect = label.rectTransform;
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 1f);
                labelRect.sizeDelta = new Vector2(width - 2f * k_Margin, k_LabelHeight);
                labelRect.anchoredPosition = new Vector2(0f, -(labelTop + k_LabelHeight * 0.5f));
                label.alignment = TextAlignmentOptions.Center;
                workshop.m_Label = label;
            }

            for (var row = 0; row < k_PieceRows; row++)
            {
                for (var column = 0; column < k_PieceColumns; column++)
                {
                    var x = (column - (k_PieceColumns - 1) * 0.5f) * (pieceSize.x + k_Spacing);
                    var y = -(piecesTop + pieceSize.y * 0.5f + row * (pieceSize.y + k_Spacing));
                    var card = CloneCard(cardTemplate, rect, cell, k_PieceScale, new Vector2(x, y), $"Pièce {workshop.m_PieceCards.Count + 1}");
                    card.Clicked += workshop.OnPieceClicked;
                    workshop.m_PieceCards.Add(card);
                    workshop.m_PieceOffers.Add(new ShopOffer { Verb = "Équiper" });
                }
            }

            return workshop;
        }

        // ---------------------------------------------------------------- construction : les cadres

        // Pour chaque partie : la zone cliquable sur la flèche (qui s'allume quand elle est choisie), le trait, puis le cadre.
        void BuildParts(RectTransform rect, ShopCard cardTemplate, TMP_Text textTemplate)
        {
            var cardImage = cardTemplate.GetComponent<Image>();
            var cardBorder = cardTemplate.transform.Find(UI.UiThemer.BorderName);

            foreach (var part in m_Parts)
            {
                var slot = part.Slot;
                var hotspot = NewRect("Zone : " + ShopCatalog.NameOf(slot), rect);
                hotspot.anchoredPosition = new Vector2(part.Hotspot.x, m_ArrowY + part.Hotspot.y);
                hotspot.sizeDelta = new Vector2(part.Hotspot.width, part.Hotspot.height);
                part.Glow = hotspot.gameObject.AddComponent<Image>();
                if (cardImage != null)
                    CopyImage(part.Glow, cardImage);
                part.Glow.color = Color.clear;
                var hotspotButton = hotspot.gameObject.AddComponent<Button>();
                hotspotButton.transition = Selectable.Transition.None;
                hotspotButton.navigation = new Navigation { mode = Navigation.Mode.None };
                hotspotButton.onClick.AddListener(() => Select(slot));
            }

            foreach (var part in m_Parts)
            {
                var line = NewRect("Trait : " + ShopCatalog.NameOf(part.Slot), rect);
                part.Line = line.gameObject.AddComponent<Image>();
                part.Line.raycastTarget = false;
                PlaceLine(line, FrameEdge(part), TargetPoint(part));
            }

            foreach (var part in m_Parts)
            {
                var slot = part.Slot;
                var frame = NewRect("Cadre : " + ShopCatalog.NameOf(slot), rect);
                frame.anchoredPosition = FrameCenter(part);
                frame.sizeDelta = k_FrameSize;
                part.Background = frame.gameObject.AddComponent<Image>();
                if (cardImage != null)
                    CopyImage(part.Background, cardImage);

                if (cardBorder != null)
                {
                    var border = Instantiate(cardBorder.gameObject, frame, false);
                    border.name = cardBorder.name;
                    part.Border = border.GetComponent<Image>();
                }

                var icon = NewRect("Icône", frame);
                icon.anchorMin = icon.anchorMax = new Vector2(0f, 0.5f);
                icon.anchoredPosition = new Vector2(42f, 0f);
                icon.sizeDelta = new Vector2(60f, 60f);
                part.Icon = icon.gameObject.AddComponent<Image>();
                part.Icon.preserveAspect = true;
                part.Icon.raycastTarget = false;

                if (textTemplate != null)
                {
                    part.SlotText = NewText(textTemplate, frame, "Emplacement", 22f);
                    Stretch(part.SlotText.rectTransform, new Vector2(80f, 46f), new Vector2(-10f, -8f));
                    part.PieceText = NewText(textTemplate, frame, "Pièce", 26f);
                    part.PieceText.enableAutoSizing = true;
                    part.PieceText.fontSizeMin = 16f;
                    part.PieceText.fontSizeMax = 26f;
                    Stretch(part.PieceText.rectTransform, new Vector2(80f, 8f), new Vector2(-10f, -44f));
                }

                part.Button = frame.gameObject.AddComponent<Button>();
                part.Button.targetGraphic = part.Background;
                part.Button.navigation = new Navigation { mode = Navigation.Mode.None };
                var colors = part.Button.colors;
                colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
                colors.colorMultiplier = 1.2f;
                part.Button.colors = colors;
                part.Button.onClick.AddListener(() => Select(slot));
            }
        }

        // Le centre d'un cadre (px depuis le haut du panneau, au milieu).
        Vector2 FrameCenter(Part part) => new Vector2(part.Frame.x, m_ArrowY + part.Frame.y * k_FrameOffset);

        // Le bord du cadre tourné vers la flèche.
        Vector2 FrameEdge(Part part) => FrameCenter(part) - new Vector2(0f, part.Frame.y * k_FrameSize.y * 0.5f);

        // Le point de la flèche relié : le long de l'axe (Target.x, de -0,5 l'encoche à 0,5 la pointe), ou sous lui (la breloque).
        Vector2 TargetPoint(Part part) => new Vector2(part.Target.x * k_ArrowSpan, m_ArrowY + part.Target.y);

        // Un trait fin d'un point à l'autre (une image tournée).
        static void PlaceLine(RectTransform line, Vector2 from, Vector2 to)
        {
            var delta = to - from;
            line.anchoredPosition = (from + to) * 0.5f;
            line.sizeDelta = new Vector2(delta.magnitude, k_LineWidth);
            line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        // ---------------------------------------------------------------- construction : la flèche 3D

        // Une copie des maillages du modèle des flèches, couchée en travers du panneau (la pointe à droite), un peu devant lui ;
        // ses pièces lumineuses, la corde et la breloque. Réessayé à chaque ouverture tant qu'il manque quelque chose.
        void EnsureShowcase()
        {
            if (m_Spinner == null)
                BuildArrow();
            if (m_Model != null && !m_Decorated && PieceLook.Instance != null)
            {
                PieceLook.Instance.Showcase(m_Model, m_Model.localPosition.z * -2f);
                m_Decorated = true;
            }

            if (m_Still != null && m_String == null)
                BuildStringAndCharm();
        }

        void BuildArrow()
        {
            var pool = ArrowPool.Instance;
            var prefab = pool != null ? pool.Prefab : null;
            if (prefab == null)
                return;

            var layer = gameObject.layer;
            var length = prefab.ModelLength;
            var scale = k_ArrowSpan / length;

            var holder = new GameObject("Vitrine").transform;
            holder.gameObject.layer = layer;
            holder.SetParent(transform, false);
            holder.localPosition = Local(new Vector2(0f, m_ArrowY), -k_ArrowDepth);
            holder.localRotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
            holder.localScale = new Vector3(scale * k_Thickness, scale * k_Thickness, scale);

            m_Spinner = NewChild("Rotation", holder, Vector3.zero);
            m_Model = NewChild("Flèche", m_Spinner, new Vector3(0f, 0f, -length * 0.5f));
            m_Still = NewChild("Corde et breloque", holder, new Vector3(0f, 0f, -length * 0.5f));

            var root = prefab.transform;
            foreach (var filter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var source = filter.GetComponent<MeshRenderer>();
                if (source == null || filter.sharedMesh == null)
                    continue;

                var matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var copy = NewChild(filter.name, m_Model, matrix.GetColumn(3));
                copy.localRotation = matrix.rotation;
                copy.localScale = matrix.lossyScale;
                copy.gameObject.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = copy.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = source.sharedMaterials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        // La corde : un V à l'encoche, ouvert vers la pointe (la corde tendue qui pousse la flèche). La breloque : un losange au
        // bout d'un fil, sous le milieu du fût, dans un repère sans l'épaisseur grossie (pour qu'elle tourne sans se déformer).
        void BuildStringAndCharm()
        {
            var material = SpecialArrows.Instance != null ? SpecialArrows.Instance.EffectMaterial : null;
            if (material == null)
                return;

            m_String = NewLine("Corde", m_Still, material, k_StringWidth);
            m_String.positionCount = 3;
            m_String.SetPosition(0, new Vector3(0f, k_StringHalfHeight, k_StringOpening));
            m_String.SetPosition(1, Vector3.zero);
            m_String.SetPosition(2, new Vector3(0f, -k_StringHalfHeight, k_StringOpening));

            var length = m_Still.localPosition.z * -2f;
            var charmRoot = NewChild("Breloque", m_Still, new Vector3(0f, 0f, length * 0.5f));
            charmRoot.localScale = new Vector3(1f, 1f, k_Thickness);
            m_Thread = NewLine("Fil", charmRoot, material, k_StringWidth * 0.5f);
            m_Thread.positionCount = 2;
            m_Thread.SetPosition(0, new Vector3(0f, -0.004f, 0f));
            m_Thread.SetPosition(1, new Vector3(0f, -k_CharmDrop + k_CharmSize, 0f));

            m_Charm = NewChild("Losange", charmRoot, new Vector3(0f, -k_CharmDrop, 0f));
            m_Charm.localRotation = Quaternion.Euler(90f, 0f, 0f);
            m_Charm.gameObject.AddComponent<MeshFilter>().sharedMesh = PieceLook.Diamond("Breloque de l'atelier", k_CharmSize, k_CharmSize * 0.7f, 4);
            m_CharmRenderer = m_Charm.gameObject.AddComponent<MeshRenderer>();
            m_CharmRenderer.sharedMaterial = material;
            m_CharmRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_CharmRenderer.receiveShadows = false;
            m_Block = new MaterialPropertyBlock();
        }

        // ---------------------------------------------------------------- mise à jour

        /// <summary>Met à jour l'atelier (à chaque changement de la boutique ou des pièces, pendant la pause).</summary>
        public void Refresh()
        {
            var upgrades = PlayerUpgrades.Instance;
            var catalog = ShopManager.Instance != null ? ShopManager.Instance.Catalog : null;
            if (upgrades == null)
                return;

            EnsureShowcase();

            // Une pièce nouvelle rangée ici (son emplacement était pris) : l'atelier s'ouvre sur sa partie.
            var owned = upgrades.Owned;
            for (var i = m_SeenOwned; i < owned.Count; i++)
            {
                var upgrade = owned[i];
                if (upgrade.kind == UpgradeKind.Object && upgrade.slot != ArrowSlot.None && !upgrades.IsEquipped(upgrade))
                    m_Selected = upgrade.slot;
            }

            m_SeenOwned = owned.Count;

            foreach (var part in m_Parts)
                ShowPart(part, upgrades, catalog);
            ShowStringAndCharm(upgrades);

            upgrades.OwnedIn(m_Selected, m_Pieces);
            if (m_Label != null)
            {
                var slot = ShopCatalog.NameOf(m_Selected);
                m_Label.text = m_Pieces.Count == 0 ? $"{slot} : aucune pièce pour l'instant (boutique, coffres)"
                             : m_Pieces.Count > m_PieceCards.Count ? $"{slot} : choisis la pièce à équiper ({m_PieceCards.Count} premières)"
                             : $"{slot} : choisis la pièce à équiper";
            }

            for (var i = 0; i < m_PieceCards.Count; i++)
                m_PieceCards[i].Show(i < m_Pieces.Count ? PieceOffer(i, m_Pieces[i], upgrades, catalog) : null, 0);
        }

        // Le cadre d'une partie : son nom, la pièce équipée (ou « Vide ») et son niveau, combien on en a ; mis en avant si choisi.
        void ShowPart(Part part, PlayerUpgrades upgrades, ShopCatalog catalog)
        {
            var piece = upgrades.InSlot(part.Slot);
            var level = piece != null ? upgrades.Level(piece.effect) : 0;
            upgrades.OwnedIn(part.Slot, m_Pieces);
            var selected = part.Slot == m_Selected;
            part.Color = piece != null && catalog != null ? catalog.ColorOf(piece) : k_EmptyColor;

            if (part.SlotText != null)
            {
                var count = m_Pieces.Count > 1 ? $"  <size=80%>({m_Pieces.Count})</size>" : "";
                part.SlotText.text = ShopCatalog.NameOf(part.Slot) + count;
                part.SlotText.color = Color.Lerp(part.Color, Color.white, 0.3f);
            }

            if (part.PieceText != null)
            {
                part.PieceText.text = piece == null ? "Vide" : level > 1 ? $"{piece.displayName} {ShopCatalog.LevelName(level)}" : piece.displayName;
                part.PieceText.color = piece != null ? Color.white : new Color(0.75f, 0.75f, 0.8f);
            }

            if (part.Icon != null)
            {
                part.Icon.sprite = piece != null ? piece.icon : null;
                part.Icon.enabled = part.Icon.sprite != null;
                part.Icon.color = Color.Lerp(part.Color, Color.white, 0.2f);
            }

            if (part.Background != null)
            {
                var color = Color.Lerp(new Color(0.12f, 0.12f, 0.14f, 0.95f), part.Color, selected ? 0.55f : 0.22f);
                color.a = 0.95f;
                part.Background.color = color;
            }

            if (part.Border != null)
                part.Border.color = selected ? Color.white : Color.Lerp(part.Color, Color.white, 0.15f);

            var line = part.Color;
            line.a = 0.55f;
            part.Line.color = selected ? new Color(1f, 1f, 1f, 0.95f) : line;

            // La zone de la partie choisie s'allume derrière la flèche (elle bat dans Update, par son alpha).
            var glow = part.Color;
            glow.a = selected ? 0.3f : 0f;
            part.Glow.color = glow;
        }

        // La corde et la breloque prennent la couleur de leur pièce (grisées sans pièce) ; la corde bat d'or pendant un duo.
        void ShowStringAndCharm(PlayerUpgrades upgrades)
        {
            m_DuoActive = ArrowDuos.AnyActive();
            var corde = upgrades.InSlot(ArrowSlot.String);
            m_StringColor = corde != null ? Glow(PieceLook.ColorOf(corde), 0.9f) : new Color(1f, 1f, 1f, 0.3f);
            if (m_String != null)
                m_String.startColor = m_String.endColor = m_StringColor;

            if (m_CharmRenderer == null)
                return;

            var talisman = upgrades.InSlot(ArrowSlot.Talisman);
            var color = talisman != null ? Glow(PieceLook.ColorOf(talisman), 1f) : new Color(0.7f, 0.7f, 0.75f, 0.3f);
            m_Block.SetColor(k_BaseColorId, color);
            m_CharmRenderer.SetPropertyBlock(m_Block);
            m_Thread.startColor = m_Thread.endColor = new Color(0.9f, 0.85f, 0.7f, talisman != null ? 0.7f : 0.25f);
        }

        // La flèche tourne sur elle-même, la breloque aussi ; la zone de la partie choisie bat doucement ; la corde bat d'or
        // pendant un duo. Seulement quand la boutique est ouverte (sinon l'atelier est caché, et ce script avec).
        void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            if (m_Spinner != null)
            {
                m_Spin = (m_Spin + k_SpinSpeed * deltaTime) % 360f;
                m_Spinner.localRotation = Quaternion.AngleAxis(m_Spin, Vector3.forward);
            }

            if (m_Charm != null)
                m_Charm.localRotation = Quaternion.Euler(0f, m_Spin * (k_CharmSpin / k_SpinSpeed), 0f) * Quaternion.Euler(90f, 0f, 0f);

            // Par l'alpha de son rendu, pas sa couleur : rien à reconstruire dans le panneau à chaque image.
            var pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            foreach (var part in m_Parts)
            {
                if (part.Glow != null && part.Slot == m_Selected)
                    part.Glow.canvasRenderer.SetAlpha(Mathf.Lerp(0.4f, 1f, pulse));
            }

            if (m_DuoActive && m_String != null)
            {
                var beat = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.2f * 2f * Mathf.PI);
                m_String.startColor = m_String.endColor = Color.Lerp(m_StringColor, Glow(k_DuoColor, 0.95f), beat);
            }
        }

        // La carte d'une pièce possédée : équipée (mise en avant, sans clic), ou à équiper, avec le duo que ça formerait.
        ShopOffer PieceOffer(int index, Upgrade piece, PlayerUpgrades upgrades, ShopCatalog catalog)
        {
            var level = upgrades.Level(piece.effect);
            var equipped = upgrades.IsEquipped(piece);
            var duo = ArrowDuos.WouldActivate(piece, upgrades);
            var subtitle = ShopCatalog.NameOf(piece.rarity);

            var offer = m_PieceOffers[index];
            offer.Kind = equipped ? ShopOfferKind.Unavailable : ShopOfferKind.Upgrade;
            offer.Upgrade = piece;
            offer.Title = level > 1 ? $"{piece.displayName} {ShopCatalog.LevelName(level)}" : piece.displayName;
            offer.Subtitle = piece.trigger == UpgradeTrigger.PerfectShot ? subtitle + " · tir parfait" : subtitle;
            offer.Description = duo != null ? $"{piece.description}\n<color=#FFD54F>Duo : {duo.Name}</color>" : piece.description;
            offer.Color = catalog != null ? catalog.ColorOf(piece) : Color.white;
            offer.Icon = piece.icon;
            offer.PriceText = equipped ? "Équipée" : "Équiper";
            offer.Highlight = equipped;
            return offer;
        }

        void Select(ArrowSlot slot)
        {
            if (slot == m_Selected)
                return;

            m_Selected = slot;
            Refresh();
        }

        // Le son de la pièce (le motif de son emplacement) vient de PieceLook ; le duo, s'il se forme, s'annonce tout seul.
        void OnPieceClicked(ShopCard card)
        {
            var piece = card.Offer != null ? card.Offer.Upgrade : null;
            var upgrades = PlayerUpgrades.Instance;
            if (piece == null || upgrades == null)
                return;

            var catalog = ShopManager.Instance != null ? ShopManager.Instance.Catalog : null;
            m_Report?.Invoke(upgrades.Equip(piece)
                                 ? ShopResult.Done("Sur la flèche : " + piece.displayName, catalog != null ? catalog.ColorOf(piece) : Color.white)
                                 : ShopResult.Failed("Déjà équipée"));
        }

        // ---------------------------------------------------------------- outils

        // Un point du panneau donné depuis le haut, au milieu (comme les éléments ancrés en haut), dans le repère du panneau
        // (son pivot est sur son bord gauche), à « depth » px devant lui (négatif : vers le joueur).
        Vector3 Local(Vector2 fromTop, float depth) => new Vector3(m_Width * 0.5f + fromTop.x, m_Height * 0.5f + fromTop.y, depth);

        static Color Glow(Color color, float alpha) => new Color(color.r * k_Glow, color.g * k_Glow, color.b * k_Glow, alpha);

        // La taille d'une carte de la boutique : la case de sa grille, sinon la sienne.
        static Vector2 CellSize(ShopCard template)
        {
            var grid = template.transform.parent != null ? template.transform.parent.GetComponent<GridLayoutGroup>() : null;
            if (grid != null)
                return grid.cellSize;

            var size = ((RectTransform)template.transform).rect.size;
            return size.x > 1f && size.y > 1f ? size : new Vector2(305f, 300f);
        }

        // Une copie de la carte, dans un support réduit : la carte garde son échelle (le survol des boutons l'agrandit à partir de
        // l'échelle qu'elle a au lancement).
        static ShopCard CloneCard(ShopCard template, RectTransform parent, Vector2 cell, float scale, Vector2 center, string name)
        {
            var holder = NewRect(name, parent);
            holder.sizeDelta = cell;
            holder.anchoredPosition = center;
            holder.localScale = Vector3.one * scale;

            var card = Instantiate(template, holder, false);
            card.name = "Carte";
            var rect = (RectTransform)card.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return card;
        }

        // Un rectangle vide ancré en haut, au milieu de son parent.
        static RectTransform NewRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        // Une copie d'un texte de la boutique (sa police, son matériau), vidée ; « size » 0 garde sa taille.
        static TMP_Text NewText(TMP_Text template, Transform parent, string name, float size)
        {
            var text = Instantiate(template, parent, false);
            text.name = name;
            text.text = "";
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.Left;
            if (size > 0f)
                text.fontSize = size;
            return text;
        }

        // Étire un texte dans son cadre : « min » depuis le coin bas-gauche, « max » depuis le coin haut-droit.
        static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = min;
            rect.offsetMax = max;
        }

        static void CopyImage(Image image, Image source)
        {
            image.sprite = source.sprite;
            image.type = source.type;
            image.color = source.color;
            image.material = source.material;
            image.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
            image.fillCenter = source.fillCenter;
        }

        static Transform NewChild(string name, Transform parent, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.gameObject.layer = parent.gameObject.layer;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        static LineRenderer NewLine(string name, Transform parent, Material material, float width)
        {
            var line = NewChild(name, parent, Vector3.zero).gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }
    }
}
