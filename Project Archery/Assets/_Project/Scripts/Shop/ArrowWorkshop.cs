using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.UI;
using Archery.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Archery.Shop
{
    /// <summary>
    /// L'atelier (refonte roguelike, 26 octobre, demandé par le joueur) : à côté de la boutique, la flèche du jeu, debout, tourne
    /// en 3D avec ses pièces allumées ; chaque partie (pointe, fût, empennage, et la corde et le talisman qui l'accompagnent)
    /// est entourée d'un sceau doré animé, comme les points faibles des boss, avec son nom et sa pièce à côté. Un clic sur une
    /// partie la choisit ; à droite de la flèche, les pièces possédées pour elle, un clic pour l'équiper. Rien n'est jamais
    /// perdu : la pièce retirée attend ici, avec ses niveaux.
    /// </summary>
    /// <remarks>
    /// Construit au lancement par le <see cref="ShopPanel"/>, à droite de son panneau et tourné vers le joueur, avec des copies
    /// de ses cartes, de son titre et de ses textes : rien à poser dans la scène, et il garde le thème des menus. La flèche est
    /// une copie des maillages du modèle des flèches (<see cref="ArrowPool.Prefab"/>), sans script ni collision, faite à la
    /// première ouverture ; ses pièces lumineuses viennent de <see cref="PieceLook.Showcase"/>. La corde passe par l'encoche, le
    /// talisman est une breloque attachée au fût (grisés quand l'emplacement est vide).
    /// <para>Les sceaux (le cadre orné du thème, en losange, comme <c>Effects/WeakPointVisual</c>) tournent doucement ; celui de
    /// la partie choisie est plus grand et plus clair, bat comme un cœur et lance une onde de temps en temps ; survolé, un sceau
    /// s'éclaire ; choisir une partie ou y équiper une pièce le fait s'emballer, briller et lancer une onde. Chaque sceau a son
    /// propre Canvas : les animer ne reconstruit pas le panneau de la boutique. Ils sont un peu devant la flèche ; les clics
    /// passent par des zones invisibles, fixes, du panneau.</para>
    /// Versions du 26 octobre : 5 cartes d'emplacement en ligne ; puis la flèche couchée avec des cadres reliés par des traits ;
    /// puis (demandé par le joueur) la flèche debout, les sceaux animés, les pièces à droite.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ArrowWorkshop : MonoBehaviour
    {
        // --- Réglages (px du panneau, depuis son coin haut-gauche, sauf mention) ---

        // La flèche : l'axe (x), le haut de la pointe (y), sa longueur à l'écran, son épaisseur (× la vraie, pour la lire de
        // loin), son avance devant le panneau, sa rotation autour de son axe (°/s). Les sceaux passent encore devant elle.
        const float k_ArrowX = 440f;
        const float k_ArrowTop = 160f;
        const float k_ArrowSpan = 720f;
        const float k_Thickness = 1.4f;
        const float k_ArrowDepth = 70f;
        const float k_SpinSpeed = 40f;
        const float k_SealDepth = 130f;

        // Les pièces, à droite de la flèche : la carte de la boutique réduite, 3 colonnes et 4 lignes, à partir de là.
        const float k_PiecesLeft = 560f;
        const float k_PiecesTop = 180f;
        const float k_PieceScale = 0.66f;
        const int k_PieceColumns = 3;
        const int k_PieceRows = 4;
        const float k_Spacing = 12f;
        const float k_Margin = 26f;
        const float k_LabelHeight = 50f;

        // Les légendes, à gauche des sceaux : leur hauteur, l'écart au sceau, la taille de l'icône.
        const float k_CaptionHeight = 64f;
        const float k_CaptionGap = 10f;
        const float k_IconSize = 44f;

        // La corde (de chaque côté de l'encoche, et ses bras tirés vers la pointe), la breloque (sa taille, m, et sa rotation),
        // les largeurs des fils (m du monde), l'éclat des couleurs.
        const float k_StringHalfWidth = 75f;
        const float k_StringPull = 14f;
        const float k_StringWidth = 0.004f;
        const float k_CharmSize = 0.018f;
        const float k_CharmSpin = 70f;
        const float k_Glow = 1.6f;

        // Les sceaux : vitesse de rotation (°/s), en plus au survol, l'emballement d'un clic, le battement de la partie choisie
        // (s), l'intervalle entre ses ondes (s).
        const float k_SealSpin = 18f;
        const float k_HoverSpin = 40f;
        const float k_ClickSpin = 420f;
        const float k_SelectedBeat = 1.1f;
        const float k_IdleBeat = 1.9f;
        const float k_WaveInterval = 1.6f;

        static readonly Color k_EmptyColor = new Color(0.55f, 0.55f, 0.6f);
        static readonly Color k_DuoColor = new Color(1f, 0.8f, 0.25f);
        static readonly Color k_Gold = new Color(1f, 0.8f, 0.38f);
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        // Une partie : où elle est sur la flèche (de l'encoche, 0, à la pointe, 1 ; et de côté pour la breloque), la taille de
        // son sceau, la largeur de sa légende ; puis ce qui est construit et l'état de l'animation.
        sealed class Part
        {
            public ArrowSlot Slot;
            public float Along;
            public float Side;
            public float Seal;
            public float CaptionWidth;

            public Image SealImage;
            public Image Wave;
            public Image Icon;
            public TMP_Text SlotText;
            public TMP_Text PieceText;
            public Color Color = k_EmptyColor;
            public Color SealColor = k_Gold;
            public int Shown = -1;

            public float Angle;
            public float Spin;
            public float Beat;
            public float Flash;
            public float WaveTime = 1f;
            public float WaveAngle;
            public float NextWave;
            public int Hovers;

            public Vector2 Center => new Vector2(k_ArrowX + Side, k_ArrowTop + (1f - Along) * k_ArrowSpan);
        }

        readonly Part[] m_Parts =
        {
            new Part { Slot = ArrowSlot.Tip, Along = 0.951f, Seal = 110f, CaptionWidth = 230f },
            new Part { Slot = ArrowSlot.Shaft, Along = 0.5f, Seal = 130f, CaptionWidth = 230f },
            new Part { Slot = ArrowSlot.Talisman, Along = 0.32f, Side = -105f, Seal = 90f, CaptionWidth = 210f },
            new Part { Slot = ArrowSlot.Fletching, Along = 0.17f, Seal = 110f, CaptionWidth = 230f },
            new Part { Slot = ArrowSlot.String, Along = -0.035f, Seal = 90f, CaptionWidth = 230f },
        };

        readonly List<ShopCard> m_PieceCards = new List<ShopCard>();
        readonly List<ShopOffer> m_PieceOffers = new List<ShopOffer>();
        readonly List<Upgrade> m_Pieces = new List<Upgrade>();
        MaterialPropertyBlock m_Block;
        TMP_Text m_Label;
        Action<ShopResult> m_Report;
        ArrowSlot m_Selected = ArrowSlot.Tip;
        int m_SeenOwned;
        float m_Height;
        Color m_Gold = k_Gold;

        // La vitrine 3D : faite à la première ouverture (la réserve des flèches et les effets doivent exister).
        Transform m_Spinner;
        Transform m_Model;
        Transform m_Decor;
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
            var gridWidth = k_PieceColumns * pieceSize.x + (k_PieceColumns - 1) * k_Spacing;
            var width = k_PiecesLeft + gridWidth + k_Margin;
            var height = Mathf.Max(k_PiecesTop + k_PieceRows * pieceSize.y + (k_PieceRows - 1) * k_Spacing + 22f,
                                   k_ArrowTop + 1.035f * k_ArrowSpan + 64f + 30f);

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
            workshop.m_Height = height;
            workshop.BuildParts(rect, textTemplate, borderTemplate);

            // À droite de la flèche : la phrase, puis la grille des pièces de la partie choisie.
            if (textTemplate != null)
            {
                var label = NewText(textTemplate, rect, "Phrase", 0f);
                Place(label.rectTransform, new Vector2(k_PiecesLeft + gridWidth * 0.5f, k_PiecesTop - 8f - k_LabelHeight * 0.5f),
                      new Vector2(gridWidth, k_LabelHeight));
                label.alignment = TextAlignmentOptions.Center;
                workshop.m_Label = label;
            }

            for (var row = 0; row < k_PieceRows; row++)
            {
                for (var column = 0; column < k_PieceColumns; column++)
                {
                    var center = new Vector2(k_PiecesLeft + pieceSize.x * 0.5f + column * (pieceSize.x + k_Spacing),
                                             k_PiecesTop + pieceSize.y * 0.5f + row * (pieceSize.y + k_Spacing));
                    var card = CloneCard(cardTemplate, rect, cell, k_PieceScale, center, $"Pièce {workshop.m_PieceCards.Count + 1}");
                    card.Clicked += workshop.OnPieceClicked;
                    workshop.m_PieceCards.Add(card);
                    workshop.m_PieceOffers.Add(new ShopOffer { Verb = "Équiper" });
                }
            }

            return workshop;
        }

        // ---------------------------------------------------------------- construction : les sceaux et les légendes

        // Pour chaque partie : sa zone cliquable (le sceau et la légende, fixes, invisibles), sa légende à gauche, et son sceau
        // animé dans son propre Canvas, un peu devant la flèche.
        void BuildParts(RectTransform rect, TMP_Text textTemplate, Transform borderTemplate)
        {
            var theme = UiTheme.Load();
            var borderImage = borderTemplate != null ? borderTemplate.GetComponent<Image>() : null;
            var sprite = theme != null && theme.panelBorder != null ? theme.panelBorder : borderImage != null ? borderImage.sprite : null;
            if (theme != null)
                m_Gold = theme.panelBorderColor;

            foreach (var part in m_Parts)
            {
                part.SealColor = m_Gold;
                part.Angle = UnityEngine.Random.value * 90f;
                part.Beat = UnityEngine.Random.value;
                var center = part.Center;
                var radius = part.Seal * 0.71f;
                var name = ShopCatalog.NameOf(part.Slot);

                // Les zones cliquables : le sceau (un carré fixe, sans la rotation, à sa profondeur : le clic tombe là où on le
                // voit), et la légende (sur le panneau).
                Clickable(rect, part, "Zone : " + name, center, Vector2.one * (part.Seal * 1.15f), -k_SealDepth);
                var captionCenter = new Vector2(center.x - radius - k_CaptionGap - part.CaptionWidth * 0.5f, center.y);
                var caption = Clickable(rect, part, "Légende : " + name, captionCenter, new Vector2(part.CaptionWidth, k_CaptionHeight), 0f);

                var icon = NewRect("Icône", caption);
                Place(icon, new Vector2(k_IconSize * 0.5f, k_CaptionHeight * 0.5f), Vector2.one * k_IconSize);
                part.Icon = icon.gameObject.AddComponent<Image>();
                part.Icon.preserveAspect = true;
                part.Icon.raycastTarget = false;

                if (textTemplate != null)
                {
                    var textLeft = k_IconSize + 8f;
                    part.SlotText = NewText(textTemplate, caption, "Emplacement", 22f);
                    Place(part.SlotText.rectTransform, new Vector2((textLeft + part.CaptionWidth) * 0.5f, 16f),
                          new Vector2(part.CaptionWidth - textLeft, 30f));
                    part.PieceText = NewText(textTemplate, caption, "Pièce", 24f);
                    part.PieceText.enableAutoSizing = true;
                    part.PieceText.fontSizeMin = 15f;
                    part.PieceText.fontSizeMax = 24f;
                    Place(part.PieceText.rectTransform, new Vector2((textLeft + part.CaptionWidth) * 0.5f, 44f),
                          new Vector2(part.CaptionWidth - textLeft, 34f));
                }

                // Le sceau, et son onde (le même cadre, qui s'agrandit en s'effaçant), dans un Canvas à eux.
                var holder = NewRect("Sceau : " + name, rect);
                holder.gameObject.AddComponent<Canvas>();
                Place(holder, center, Vector2.one * part.Seal);
                holder.anchoredPosition3D = new Vector3(holder.anchoredPosition.x, holder.anchoredPosition.y, -k_SealDepth);
                part.SealImage = SealImage(holder, "Cadre", sprite, part.Seal);
                part.Wave = SealImage(holder, "Onde", sprite, part.Seal);
                part.Wave.enabled = false;
            }
        }

        // Une zone cliquable invisible (survol : le sceau s'éclaire ; clic : la partie est choisie), à « depth » px du panneau.
        RectTransform Clickable(RectTransform parent, Part part, string name, Vector2 center, Vector2 size, float depth)
        {
            var zone = NewRect(name, parent);
            Place(zone, center, size);
            zone.anchoredPosition3D = new Vector3(zone.anchoredPosition.x, zone.anchoredPosition.y, depth);
            var image = zone.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            var button = zone.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => Select(part.Slot));

            var trigger = zone.gameObject.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, () => part.Hovers++);
            AddTrigger(trigger, EventTriggerType.PointerExit, () => part.Hovers = Mathf.Max(0, part.Hovers - 1));
            return zone;
        }

        static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        // Le cadre orné du thème, en losange, de ce côté (px), comme le sceau des points faibles (épaisseur réglée pareil).
        static Image SealImage(RectTransform parent, string name, Sprite sprite, float side)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            image.sprite = sprite;
            image.type = sprite != null && sprite.border.x > 0f ? Image.Type.Sliced : Image.Type.Simple;
            image.fillCenter = false;
            if (sprite != null && sprite.border.x > 0f)
                image.pixelsPerUnitMultiplier = sprite.border.x / Mathf.Clamp(side * 0.05f, 14f, 48f) * 0.6f;
            return image;
        }

        // ---------------------------------------------------------------- construction : la flèche 3D

        // Une copie des maillages du modèle des flèches, debout (la pointe en haut), un peu devant le panneau ; ses pièces
        // lumineuses, la corde et la breloque. Réessayé à chaque ouverture tant qu'il manque quelque chose.
        void EnsureShowcase()
        {
            if (m_Spinner == null)
                BuildArrow();
            if (m_Model != null && !m_Decorated && PieceLook.Instance != null)
            {
                PieceLook.Instance.Showcase(m_Model, m_Model.localPosition.z * -2f);
                m_Decorated = true;
            }

            if (m_Decor != null && m_String == null)
                BuildStringAndCharm();
        }

        void BuildArrow()
        {
            var pool = ArrowPool.Instance;
            var prefab = pool != null ? pool.Prefab : null;
            if (prefab == null)
                return;

            var length = prefab.ModelLength;
            var scale = k_ArrowSpan / length;

            var holder = NewChild("Vitrine", transform, Local(new Vector2(k_ArrowX, k_ArrowTop + k_ArrowSpan * 0.5f), -k_ArrowDepth));
            holder.localRotation = Quaternion.LookRotation(Vector3.up, Vector3.back);
            holder.localScale = new Vector3(scale * k_Thickness, scale * k_Thickness, scale);

            m_Spinner = NewChild("Rotation", holder, Vector3.zero);
            m_Model = NewChild("Flèche", m_Spinner, new Vector3(0f, 0f, -length * 0.5f));
            m_Decor = NewChild("Corde et breloque", transform, new Vector3(0f, 0f, -k_ArrowDepth));

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

        // La corde : un fil qui passe par l'encoche, ses bras un peu tirés vers la pointe. La breloque : un losange au bout d'un
        // fil noué au fût, à gauche de la flèche, qui tourne sur lui-même. Dans le repère du panneau (px), devant lui.
        void BuildStringAndCharm()
        {
            var material = SpecialArrows.Instance != null ? SpecialArrows.Instance.EffectMaterial : null;
            if (material == null)
                return;

            var nock = k_ArrowTop + k_ArrowSpan;
            m_String = NewLine("Corde", m_Decor, material, k_StringWidth);
            m_String.positionCount = 3;
            m_String.SetPosition(0, Flat(new Vector2(k_ArrowX - k_StringHalfWidth, nock - k_StringPull)));
            m_String.SetPosition(1, Flat(new Vector2(k_ArrowX, nock)));
            m_String.SetPosition(2, Flat(new Vector2(k_ArrowX + k_StringHalfWidth, nock - k_StringPull)));

            var talisman = Array.Find(m_Parts, part => part.Slot == ArrowSlot.Talisman);
            var charmCenter = talisman.Center;
            var pixelsPerMeter = k_ArrowSpan / Mathf.Max(0.1f, m_Model.localPosition.z * -2f) * k_Thickness;
            var charmTop = charmCenter.y - k_CharmSize * pixelsPerMeter;
            m_Thread = NewLine("Fil", m_Decor, material, k_StringWidth * 0.5f);
            m_Thread.positionCount = 3;
            m_Thread.SetPosition(0, Flat(new Vector2(k_ArrowX - 6f, charmTop - 20f)));
            m_Thread.SetPosition(1, Flat(new Vector2((k_ArrowX + charmCenter.x) * 0.5f, charmTop - 6f)));
            m_Thread.SetPosition(2, Flat(new Vector2(charmCenter.x, charmTop)));

            var charmRoot = NewChild("Breloque", m_Decor, Flat(charmCenter));
            charmRoot.localScale = Vector3.one * pixelsPerMeter;
            m_Charm = NewChild("Losange", charmRoot, Vector3.zero);
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

        // La légende d'une partie (son nom, combien on en a, la pièce équipée ou « Vide » et son niveau, son icône) et la couleur
        // de son sceau. Une autre pièce équipée ici que la dernière fois : le sceau s'emballe et lance une onde.
        void ShowPart(Part part, PlayerUpgrades upgrades, ShopCatalog catalog)
        {
            var piece = upgrades.InSlot(part.Slot);
            var level = piece != null ? upgrades.Level(piece.effect) : 0;
            upgrades.OwnedIn(part.Slot, m_Pieces);
            var selected = part.Slot == m_Selected;
            part.Color = piece != null && catalog != null ? catalog.ColorOf(piece) : k_EmptyColor;
            part.SealColor = piece != null ? Color.Lerp(m_Gold, part.Color, 0.35f) : Color.Lerp(m_Gold, k_EmptyColor, 0.5f);

            var shown = piece != null ? (int)piece.effect * 10 + level : 0;
            if (part.Shown >= 0 && shown != part.Shown)
                Burst(part, 300f);
            part.Shown = shown;

            if (part.SlotText != null)
            {
                var count = m_Pieces.Count > 1 ? $"  <size=80%>({m_Pieces.Count})</size>" : "";
                part.SlotText.text = ShopCatalog.NameOf(part.Slot).ToUpperInvariant() + count;
                part.SlotText.color = selected ? Color.white : m_Gold;
            }

            if (part.PieceText != null)
            {
                part.PieceText.text = piece == null ? "Vide" : level > 1 ? $"{piece.displayName} {ShopCatalog.LevelName(level)}" : piece.displayName;
                part.PieceText.color = piece != null ? Color.Lerp(part.Color, Color.white, 0.55f) : new Color(0.7f, 0.7f, 0.75f);
            }

            if (part.Icon != null)
            {
                part.Icon.sprite = piece != null ? piece.icon : null;
                part.Icon.enabled = part.Icon.sprite != null;
                part.Icon.color = Color.Lerp(part.Color, Color.white, 0.2f);
            }
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

        // La boutique se ferme sous le rayon : plus rien n'est survolé.
        void OnDisable()
        {
            foreach (var part in m_Parts)
                part.Hovers = 0;
        }

        // La flèche et la breloque tournent ; les sceaux tournent, battent et lancent leurs ondes ; la corde bat d'or pendant un
        // duo. Seulement quand la boutique est ouverte (sinon l'atelier est caché, et ce script avec).
        void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            m_Spin = (m_Spin + k_SpinSpeed * deltaTime) % 360f;
            if (m_Spinner != null)
                m_Spinner.localRotation = Quaternion.AngleAxis(m_Spin, Vector3.forward);
            if (m_Charm != null)
                m_Charm.localRotation = Quaternion.Euler(0f, m_Spin * (k_CharmSpin / k_SpinSpeed), 0f) * Quaternion.Euler(90f, 0f, 0f);

            foreach (var part in m_Parts)
                AnimateSeal(part, deltaTime);

            if (m_DuoActive && m_String != null)
            {
                var beat = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.2f * 2f * Mathf.PI);
                m_String.startColor = m_String.endColor = Color.Lerp(m_StringColor, Glow(k_DuoColor, 0.95f), beat);
            }
        }

        // Le sceau tourne (plus vite survolé, très vite après un clic), et bat ; celui de la partie choisie est plus grand, plus
        // clair, bat comme un cœur et lance une onde toutes les 1,6 s. Comme les points faibles des boss (WeakPointVisual).
        void AnimateSeal(Part part, float deltaTime)
        {
            if (part.SealImage == null)
                return;

            var selected = part.Slot == m_Selected;
            var hovered = part.Hovers > 0;
            part.Spin *= Mathf.Exp(-deltaTime * 3f);
            part.Angle = (part.Angle + deltaTime * (k_SealSpin + (hovered ? k_HoverSpin : 0f) + part.Spin)) % 360f;
            part.Beat = (part.Beat + deltaTime / (selected ? k_SelectedBeat : k_IdleBeat)) % 1f;
            var beat = selected ? Bump(part.Beat, 0.06f) + 0.6f * Bump(part.Beat, 0.28f) : 0.35f * Bump(part.Beat, 0.06f);
            part.Flash = Mathf.MoveTowards(part.Flash, 0f, deltaTime / 0.35f);

            var seal = part.SealImage.rectTransform;
            seal.localRotation = Quaternion.Euler(0f, 0f, 45f + part.Angle);
            seal.localScale = Vector3.one * ((selected ? 1.08f : 0.94f) + (hovered ? 0.05f : 0f) + 0.05f * beat + 0.12f * part.Flash);
            var color = Color.Lerp(part.SealColor, Color.white, (selected ? 0.25f : 0f) + (hovered ? 0.2f : 0f) + 0.5f * part.Flash);
            color.a = (selected ? 0.95f : hovered ? 0.85f : 0.5f) * (0.85f + 0.15f * beat);
            part.SealImage.color = color;

            if (selected && part.WaveTime >= 1f && Time.unscaledTime >= part.NextWave)
                StartWave(part);

            if (part.WaveTime >= 1f)
            {
                if (part.Wave.enabled)
                    part.Wave.enabled = false;
                return;
            }

            part.Wave.enabled = true;
            part.WaveTime = Mathf.Min(1f, part.WaveTime + deltaTime / 0.6f);
            var ease = 1f - (1f - part.WaveTime) * (1f - part.WaveTime);
            var wave = part.Wave.rectTransform;
            wave.localRotation = Quaternion.Euler(0f, 0f, 45f + part.WaveAngle);
            wave.localScale = Vector3.one * (1f + 0.8f * ease);
            var waveColor = Color.Lerp(part.SealColor, Color.white, 0.3f);
            waveColor.a = (1f - part.WaveTime) * 0.9f;
            part.Wave.color = waveColor;
        }

        void StartWave(Part part)
        {
            part.WaveTime = 0f;
            part.WaveAngle = part.Angle;
            part.NextWave = Time.unscaledTime + k_WaveInterval;
        }

        // Un clic ou une pièce équipée : le sceau s'emballe, brille, et lance une onde.
        void Burst(Part part, float spin)
        {
            part.Spin = spin;
            part.Flash = 1f;
            StartWave(part);
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
            // Le texte au niveau de la pièce (27 octobre au soir : ses dégâts, sa zone, sa vitesse… de maintenant).
            var description = piece.DescriptionAt(level);
            offer.Description = duo != null ? $"{description}\n<color=#FFD54F>Duo : {duo.Name}</color>" : description;
            offer.Color = catalog != null ? catalog.ColorOf(piece) : Color.white;
            offer.Icon = piece.icon;
            offer.PriceText = equipped ? "Équipée" : "Équiper";
            offer.Highlight = equipped;
            return offer;
        }

        void Select(ArrowSlot slot)
        {
            var part = Array.Find(m_Parts, candidate => candidate.Slot == slot);
            if (part != null)
                Burst(part, k_ClickSpin);
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

        // Un point du panneau donné depuis son coin haut-gauche, dans le repère du panneau (son pivot est au milieu de son bord
        // gauche), à « depth » px devant lui (négatif : vers le joueur).
        Vector3 Local(Vector2 fromTopLeft, float depth) => new Vector3(fromTopLeft.x, m_Height * 0.5f - fromTopLeft.y, depth);

        // Pareil, dans le plan du décor 3D (déjà avancé devant le panneau).
        Vector3 Flat(Vector2 fromTopLeft) => Local(fromTopLeft, 0f);

        static Color Glow(Color color, float alpha) => new Color(color.r * k_Glow, color.g * k_Glow, color.b * k_Glow, alpha);

        // Une bosse douce de 0 à 1, centrée sur « center », large de 0,18 période (le battement de cœur des points faibles).
        static float Bump(float t, float center)
        {
            var x = (t - center) / 0.09f;
            if (x <= -1f || x >= 1f)
                return 0f;
            var y = 1f - x * x;
            return y * y;
        }

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
            Place(holder, center, cell);
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

        static RectTransform NewRect(string name, Transform parent)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            return rect;
        }

        // Centre (px depuis le coin haut-gauche du parent) et taille d'un rectangle.
        static void Place(RectTransform rect, Vector2 center, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(center.x, -center.y);
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
