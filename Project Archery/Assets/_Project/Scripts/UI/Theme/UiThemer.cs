using System;
using System.Collections.Generic;
using Archery.Difficulty;
using Archery.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.UI
{
    /// <summary>
    /// Habille un menu (un Canvas, ou un prefab comme la carte de la boutique) avec le <see cref="UiTheme"/> :
    /// <list type="bullet">
    /// <item>les fonds « Content » deviennent des panneaux sombres à bordure dorée ornée ;</item>
    /// <item>chaque bouton prend la couleur de son rôle (valider en émeraude, quitter en rubis, retour en prune, flèches en
    /// améthyste, les autres en saphir), une bordure ornée et une réaction quand on le vise (<see cref="UiButtonFx"/>) ;</item>
    /// <item>les cartes (boutique, constructions) ont un cadre orné, teinté en jeu de la couleur de leur offre ;</item>
    /// <item>les curseurs ont une piste sombre, un remplissage coloré et une poignée en losange dorée ;</item>
    /// <item>les titres passent en Cinzel Decorative dorée, soulignés d'un trait orné ; les boutons et étiquettes en Cinzel ;
    /// les textes longs en Inter.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// On peut le réappliquer : les ornements déjà ajoutés sont repris, pas doublés. Les objets ajoutés portent un
    /// <see cref="LayoutElement"/> « ignoré » : ils ne dérangent pas les groupes de mise en page.
    /// </remarks>
    public static class UiThemer
    {
        /// <summary>Version du thème : l'éditeur réapplique le thème aux menus marqués d'une version plus ancienne.</summary>
        public const int Version = 1;

        public const string BorderName = "Bordure";
        const string k_DividerLeft = "Ornement gauche";
        const string k_DividerRight = "Ornement droit";
        // Hauteur (px du Canvas) de la barre de PV des boss des vagues.
        const float k_HealthBarHeight = 56f;

        enum Role
        {
            Primary,
            Secondary,
            Arrow,
            Back,
            Danger,
        }

        static readonly string[] k_LabelNames =
        {
            "Label", "Difficulty", "Name Label", "Money", "Materials", "Subtitle", "Price", "Value",
            // La montre et le panneau des vagues.
            "Wave", "Score", "Timer", "Combo", "Player HP", "Tower HP", "Buffs",
        };

        /// <summary>
        /// Applique le thème sous <paramref name="root"/>. <paramref name="skip"/> écarte des objets (par exemple, dans
        /// l'éditeur, les morceaux d'un prefab placé dans la scène : on habille le prefab lui-même).
        /// </summary>
        public static void Apply(GameObject root, UiTheme theme, Predicate<GameObject> skip = null)
        {
            if (root == null || theme == null)
                return;

            bool Skipped(Component component) => skip != null && skip(component.gameObject);
            var done = new HashSet<Graphic>();

            foreach (var card in root.GetComponentsInChildren<ShopCard>(true))
            {
                if (!Skipped(card))
                    StyleCard(card, theme, done);
            }

            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (!Skipped(button) && !done.Contains(button.targetGraphic) && button.GetComponent<ShopCard>() == null)
                    StyleButton(button, theme, done);
            }

            foreach (var slider in root.GetComponentsInChildren<Slider>(true))
            {
                if (!Skipped(slider))
                    StyleSlider(slider, theme, done);
            }

            foreach (var towerButton in root.GetComponentsInChildren<WatchTowerButton>(true))
            {
                if (!Skipped(towerButton))
                    StyleWatchButton(towerButton, theme, done);
            }

            foreach (var difficulty in root.GetComponentsInChildren<DifficultyButton>(true))
            {
                if (!Skipped(difficulty))
                    StyleDifficultyPanel(difficulty, theme, done);
            }

            foreach (var healthBar in root.GetComponentsInChildren<HealthBar>(true))
            {
                if (!Skipped(healthBar))
                    StyleHealthBar(healthBar, theme, done);
            }

            // Les fonds : « Content » des menus, « Background » de la montre et du panneau des vagues.
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if ((image.name == "Content" || image.name == "Background") && !done.Contains(image) && !Skipped(image))
                    StylePanel(image, theme, done);
            }

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!Skipped(text) && !done.Contains(text))
                    StyleText(text, theme);
            }

            var marker = root.GetComponent<UiThemed>();
            if (marker == null)
                marker = root.AddComponent<UiThemed>();
            marker.version = Version;
        }

        // ---------------------------------------------------------------- panneaux

        static void StylePanel(Image image, UiTheme theme, HashSet<Graphic> done)
        {
            done.Add(image);
            var scale = PanelScale(image.rectTransform, theme);
            SetSliced(image, theme.panelFill, theme.panelColor, scale);
            Decoration(image.transform, BorderName, theme.panelBorder, theme.panelBorderColor, scale, Image.Type.Sliced);
            AddPanelFx(image.gameObject);

            // Les pages d'un menu (enfants avec une mise en page) apparaissent aussi en douceur.
            for (var i = 0; i < image.transform.childCount; i++)
            {
                var child = image.transform.GetChild(i);
                if (child.GetComponent<VerticalLayoutGroup>() != null)
                    AddPanelFx(child.gameObject);
            }
        }

        // Taille des ornements adaptée au cadre : des coins d'environ 5 % de son petit côté (de 14 à 48 unités), qu'il
        // s'agisse d'un menu d'un mètre, de la montre (18 cm) ou du panneau des vagues (8 m).
        static float PanelScale(RectTransform rect, UiTheme theme) => AdaptiveScale(rect, theme.panelBorder, 0.05f, 14f, 48f, theme.panelScale);

        static float ButtonScale(RectTransform rect, UiTheme theme) => AdaptiveScale(rect, theme.buttonBorder, 0.3f, 10f, 30f, theme.buttonScale);

        static float AdaptiveScale(RectTransform rect, Sprite border, float fraction, float min, float max, float fallback)
        {
            var size = rect != null ? Mathf.Min(rect.rect.width, rect.rect.height) : 0f;
            if (size <= 0f || border == null || border.border.x <= 0f)
                return fallback;
            return border.border.x / Mathf.Clamp(size * fraction, min, max);
        }

        // Le bouton « Tour » de la montre : un petit bouton orné saphir (gris violacé quand il n'est pas prêt), son
        // remplissage d'appui doré.
        static void StyleWatchButton(WatchTowerButton button, UiTheme theme, HashSet<Graphic> done)
        {
            var visual = button.transform.Find("Visual");
            if (visual == null || !visual.TryGetComponent<Image>(out var image))
                return;

            done.Add(image);
            var scale = ButtonScale(image.rectTransform, theme);
            SetSliced(image, theme.buttonFill, theme.secondaryButton, scale);
            Decoration(visual, BorderName, theme.buttonBorder, theme.buttonBorderColor, scale, Image.Type.Sliced);
            button.SetColors(theme.secondaryButton, new Color(0.3f, 0.26f, 0.4f, 0.85f));

            var fill = visual.Find("Fill");
            if (fill != null && fill.TryGetComponent<Image>(out var progress))
            {
                done.Add(progress);
                progress.color = new Color(1f, 0.85f, 0.4f, 0.45f);
            }

            foreach (var text in visual.GetComponentsInChildren<TMP_Text>(true))
            {
                done.Add(text);
                SetFont(text, theme.labelFont);
                text.color = theme.buttonTextColor;
                text.fontStyle &= ~FontStyles.Bold;
                ShrinkToFit(text);
            }
        }

        // Un panneau de difficulté (en 3D) : un petit Canvas juste devant la planche, avec le fond sombre, la bordure
        // ornée et deux traits sous le nom, teintés en jeu de la couleur de la difficulté (vive si elle est choisie).
        // Le nom passe en Cinzel Decorative dégradé, le résumé en Inter.
        const string k_DifficultyFrame = "Cadre";

        static void StyleDifficultyPanel(DifficultyButton panel, UiTheme theme, HashSet<Graphic> done)
        {
            const float pixel = 0.001f;
            var root = panel.transform;
            var board = root.Find("Board");
            var size = board != null ? new Vector2(board.localScale.x, board.localScale.y) : new Vector2(1.8f, 1.1f);
            var center = board != null ? board.localPosition : Vector3.zero;
            var front = board != null ? board.localPosition.z - board.localScale.z * 0.5f - 0.004f : -0.044f;

            var frame = root.Find(k_DifficultyFrame) as RectTransform;
            if (frame == null)
            {
                var go = new GameObject(k_DifficultyFrame, typeof(RectTransform), typeof(Canvas));
                go.layer = root.gameObject.layer;
                frame = (RectTransform)go.transform;
                frame.SetParent(root, false);
            }

            var canvas = frame.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;
            frame.localPosition = new Vector3(center.x, center.y, front);
            frame.localRotation = Quaternion.identity;
            frame.localScale = Vector3.one * pixel;
            frame.sizeDelta = size / pixel;

            var scale = PanelScale(frame, theme);
            var fill = Decoration(frame, "Fond", theme.panelFill, theme.panelColor, scale, Image.Type.Sliced);
            var border = Decoration(frame, BorderName, theme.panelBorder, Color.white, scale, Image.Type.Sliced);
            fill.transform.SetSiblingIndex(0);
            border.transform.SetSiblingIndex(1);

            // Deux traits ornés sous le nom.
            var tinted = new List<Graphic> { border };
            var name = panel.NameText;
            if (name != null && theme.divider != null)
            {
                var nameRect = name.rectTransform;
                var bottom = nameRect.localPosition.y - nameRect.rect.height * nameRect.pivot.y;
                var y = (bottom - center.y) / pixel - 6f;
                var width = size.x / pixel * 0.26f;
                foreach (var right in new[] { false, true })
                {
                    var divider = Decoration(frame, right ? k_DividerRight : k_DividerLeft, theme.divider, Color.white, 1f, Image.Type.Sliced);
                    var rect = divider.rectTransform;
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(1f, 0.5f);
                    rect.sizeDelta = new Vector2(width, 30f);
                    rect.anchoredPosition = new Vector2(right ? 6f : -6f, y);
                    rect.localScale = new Vector3(right ? -1f : 1f, 1f, 1f);
                    divider.transform.SetAsLastSibling();
                    tinted.Add(divider);
                }
            }

            panel.SetTintedGraphics(tinted.ToArray());

            // Les textes (3D) passent devant le Canvas.
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                done.Add(text);
                if (text is TextMeshPro mesh)
                    mesh.sortingOrder = 1;

                if (text == name)
                {
                    SetFont(text, theme.titleFont);
                    text.fontStyle &= ~FontStyles.Bold;
                    text.enableVertexGradient = true;
                    text.colorGradient = new VertexGradient(theme.titleTop, theme.titleTop, theme.titleBottom, theme.titleBottom);
                    ShrinkToFit(text);
                }
                else
                {
                    SetFont(text, theme.bodyFont);
                    text.color = theme.textColor;
                }
            }
        }

        // La barre de PV d'un boss de vague (au-dessus de sa tête) : jauge sombre à bordure dorée ornée, remplissage net,
        // nom en Cinzel Decorative doré.
        // Dans l'ordre : le fond, le remplissage (un peu en retrait), la bordure par-dessus, puis le nom, posé sur la barre.
        static void StyleHealthBar(HealthBar bar, UiTheme theme, HashSet<Graphic> done)
        {
            var root = (RectTransform)bar.transform;
            // Assez haute pour que le nom tienne dedans.
            if (root.sizeDelta.y < k_HealthBarHeight)
                root.sizeDelta = new Vector2(root.sizeDelta.x, k_HealthBarHeight);
            var scale = AdaptiveScale(root, theme.panelBorder, 0.3f, 8f, 24f, 1f);
            var background = bar.transform.Find("Background");
            if (background != null && background.TryGetComponent<Image>(out var track))
            {
                done.Add(track);
                SetSliced(track, theme.panelFill, theme.sliderTrack, scale);
                track.transform.SetSiblingIndex(0);
            }

            var fill = bar.transform.Find("Fill");
            if (fill != null && fill.TryGetComponent<Image>(out var amount))
            {
                done.Add(amount);
                ApplyGaugeFill(amount, theme, amount.color);
                var inset = 4f / scale + 1f;
                amount.rectTransform.offsetMin = new Vector2(inset, inset);
                amount.rectTransform.offsetMax = new Vector2(-inset, -inset);
                amount.transform.SetSiblingIndex(1);
            }

            var border = Decoration(root, BorderName, theme.panelBorder, theme.panelBorderColor, scale, Image.Type.Sliced);
            border.transform.SetSiblingIndex(2);
            done.Add(border);

            foreach (var text in bar.GetComponentsInChildren<TMP_Text>(true))
            {
                done.Add(text);
                ApplyTitle(text, theme, false);
                // Toute la barre, à l'intérieur de la bordure, centré sur les lettres.
                var rect = text.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(14f, 6f);
                rect.offsetMax = new Vector2(-14f, -6f);
                text.verticalAlignment = VerticalAlignmentOptions.Geometry;
                text.transform.SetAsLastSibling();
            }
        }

        // ---------------------------------------------------------------- pour l'interface fabriquée par le code

        /// <summary>Un panneau du thème (fond sombre, bordure dorée ornée) sur cette image ; <paramref name="cornerSize"/> : taille des coins.</summary>
        public static void ApplyPanel(Image image, UiTheme theme, float cornerSize, float alpha = -1f)
        {
            if (image == null || theme == null)
                return;
            var scale = theme.panelBorder != null ? theme.panelBorder.border.x / Mathf.Max(1f, cornerSize) : theme.panelScale;
            var color = theme.panelColor;
            if (alpha >= 0f)
                color.a = alpha;
            SetSliced(image, theme.panelFill, color, scale);
            Decoration(image.transform, BorderName, theme.panelBorder, theme.panelBorderColor, scale, Image.Type.Sliced);
        }

        /// <summary>Le fond d'une jauge (barre de PV) : très sombre, aux coins du thème, sans bordure.</summary>
        public static void ApplyGaugeTrack(Image track, UiTheme theme)
        {
            if (track == null || theme == null)
                return;
            SetSliced(track, theme.plainFill, theme.sliderTrack, 1.5f);
        }

        /// <summary>Le remplissage d'une jauge, de cette couleur (ses coins restent nets quand il raccourcit).</summary>
        public static void ApplyGaugeFill(Image fill, UiTheme theme, Color color)
        {
            if (fill == null || theme == null)
                return;
            SetSliced(fill, theme.plainFill, color, 1.5f);
        }

        /// <summary>Un titre : Cinzel Decorative en dégradé doré ; <paramref name="ornaments"/> : un trait orné de chaque côté, dessous.</summary>
        public static void ApplyTitle(TMP_Text text, UiTheme theme, bool ornaments)
        {
            if (text == null || theme == null)
                return;
            SetFont(text, theme.titleFont);
            text.fontStyle &= ~(FontStyles.Bold | FontStyles.SmallCaps);
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(theme.titleTop, theme.titleTop, theme.titleBottom, theme.titleBottom);
            ShrinkToFit(text);
            if (ornaments)
                AddDividers(text, theme);
        }

        /// <summary>Une étiquette ou un message : Cinzel, sans gras (la couleur du texte est gardée).</summary>
        public static void ApplyLabel(TMP_Text text, UiTheme theme)
        {
            if (text == null || theme == null)
                return;
            SetFont(text, theme.labelFont);
            text.fontStyle &= ~FontStyles.Bold;
        }

        /// <summary>
        /// Deux traits ornés de part et d'autre d'un point (un nom, un titre) : pointes vers lui, de cette longueur, à
        /// <paramref name="gap"/> de chaque côté.
        /// </summary>
        public static void AddSideOrnaments(RectTransform parent, UiTheme theme, Vector2 center, float gap, float length, float height = 26f,
                                           Vector2? anchor = null)
        {
            if (parent == null || theme == null || theme.divider == null)
                return;
            foreach (var right in new[] { false, true })
            {
                var divider = Decoration(parent, right ? k_DividerRight : k_DividerLeft, theme.divider, theme.panelBorderColor, 1f, Image.Type.Sliced);
                var rect = divider.rectTransform;
                rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.sizeDelta = new Vector2(length, height);
                rect.anchoredPosition = center + new Vector2(right ? gap : -gap, 0f);
                rect.localScale = new Vector3(right ? -1f : 1f, 1f, 1f);
                divider.transform.SetAsLastSibling();
            }
        }

        // ---------------------------------------------------------------- dans le monde (objets 3D)

        const float k_Pixel = 0.001f;

        /// <summary>
        /// Une plaque du thème dans le monde (fond sombre, bordure ornée de cette couleur), enfant de
        /// <paramref name="parent"/>, de cette taille (m), à cette place : à mettre derrière un texte 3D (dont l'ordre
        /// d'affichage doit être plus grand que 0). Reprise si elle existe déjà. Rend la bordure.
        /// </summary>
        public static Image Plaque(Transform parent, string name, UiTheme theme, Vector2 size, Vector3 localPosition, Color borderColor, float alpha = 0.92f)
        {
            var frame = WorldCanvas(parent, name, localPosition, size);
            var scale = PanelScale(frame, theme);
            var color = theme.panelColor;
            color.a = alpha;
            var fill = Decoration(frame, "Fond", theme.panelFill, color, scale, Image.Type.Sliced);
            var border = Decoration(frame, BorderName, theme.panelBorder, borderColor, scale, Image.Type.Sliced);
            fill.transform.SetSiblingIndex(0);
            border.transform.SetSiblingIndex(1);
            return border;
        }

        /// <summary>
        /// Un sceau dans le monde : un cadre orné tourné en losange (à faire tourner par l'appelant), de ce côté (m),
        /// enfant de <paramref name="parent"/>. Rend son image.
        /// </summary>
        public static Image Seal(Transform parent, string name, UiTheme theme, float size, Vector3 localPosition, Color color)
        {
            var frame = WorldCanvas(parent, name, localPosition, new Vector2(size, size));
            var image = Decoration(frame, BorderName, theme.panelBorder, color, PanelScale(frame, theme) * 0.6f, Image.Type.Sliced);
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            return image;
        }

        /// <summary>Un texte 3D (TextMeshPro) passe devant les plaques du thème.</summary>
        public static void InFrontOfPlaques(TMP_Text text)
        {
            if (text is TextMeshPro mesh)
                mesh.sortingOrder = 1;
        }

        static RectTransform WorldCanvas(Transform parent, string name, Vector3 localPosition, Vector2 size)
        {
            var frame = parent.Find(name) as RectTransform;
            if (frame == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
                go.layer = parent.gameObject.layer;
                frame = (RectTransform)go.transform;
                frame.SetParent(parent, false);
            }

            var canvas = frame.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;
            frame.localPosition = localPosition;
            frame.localRotation = Quaternion.identity;
            frame.localScale = Vector3.one * k_Pixel;
            frame.sizeDelta = size / k_Pixel;
            return frame;
        }

        static void AddPanelFx(GameObject target)
        {
            if (target.GetComponent<CanvasGroup>() == null)
                target.AddComponent<CanvasGroup>();
            if (target.GetComponent<UiPanelFx>() == null)
                target.AddComponent<UiPanelFx>();
        }

        // Une carte : fond sombre, cadre orné (teinté en jeu par la carte, de la couleur de son offre).
        static void StyleCard(ShopCard card, UiTheme theme, HashSet<Graphic> done)
        {
            var image = card.GetComponent<Image>();
            if (image == null)
                return;

            done.Add(image);
            SetSliced(image, theme.frameFill, theme.cardColor, theme.frameScale);
            var border = Decoration(card.transform, BorderName, theme.frameBorder, theme.buttonBorderColor, theme.frameScale, Image.Type.Sliced);
            card.SetBaseColor(theme.cardColor);

            var button = card.GetComponent<Button>();
            if (button != null)
            {
                SetTransition(button);
                // Pas de son au clic : la boutique joue le sien (achat ou refus).
                GetOrAdd<UiButtonFx>(card.gameObject).Configure(image, border, false, 1.03f, theme.hoverBorderColor,
                                                                theme.hoverClip, theme.hoverVolume, null, 0f);
            }
        }

        // ---------------------------------------------------------------- boutons

        static void StyleButton(Button button, UiTheme theme, HashSet<Graphic> done)
        {
            var role = RoleOf(button);
            var image = button.targetGraphic as Image;
            if (image == null)
                image = button.GetComponent<Image>();

            Image border = null;
            if (image != null)
            {
                done.Add(image);
                SetSliced(image, theme.buttonFill, ColorOf(role, theme), theme.buttonScale);
                border = Decoration(button.transform, BorderName, theme.buttonBorder, theme.buttonBorderColor, theme.buttonScale, Image.Type.Sliced);
            }

            SetTransition(button);
            var click = role == Role.Primary ? theme.confirmClip : role == Role.Back ? theme.backClip : theme.clickClip;
            GetOrAdd<UiButtonFx>(button.gameObject).Configure(image, border, true, 1.05f, theme.hoverBorderColor,
                                                              theme.hoverClip, theme.hoverVolume, click, theme.clickVolume);

            foreach (var text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                done.Add(text);
                SetFont(text, theme.labelFont);
                text.color = theme.buttonTextColor;
                text.fontStyle &= ~FontStyles.Bold;
                ShrinkToFit(text);
            }
        }

        // Le rôle d'un bouton, d'après son nom et son texte.
        static Role RoleOf(Button button)
        {
            var label = button.GetComponentInChildren<TMP_Text>(true);
            var key = (button.name + " " + (label != null ? label.text : "")).ToLowerInvariant();
            if (Has(key, "quit"))
                return Role.Danger;
            if (Has(key, "back", "retour", "close", "fermer"))
                return Role.Back;
            if (Has(key, "previous", "next") || (label != null && (label.text.Trim() == "<" || label.text.Trim() == ">")))
                return Role.Arrow;
            if (Has(key, "play", "jouer", "save", "enregistr", "finish", "termin"))
                return Role.Primary;
            return Role.Secondary;
        }

        static bool Has(string key, params string[] words)
        {
            foreach (var word in words)
            {
                if (key.Contains(word))
                    return true;
            }

            return false;
        }

        static Color ColorOf(Role role, UiTheme theme)
        {
            switch (role)
            {
                case Role.Primary:
                    return theme.primaryButton;
                case Role.Arrow:
                    return theme.arrowButton;
                case Role.Back:
                    return theme.backButton;
                case Role.Danger:
                    return theme.dangerButton;
                default:
                    return theme.secondaryButton;
            }
        }

        // Teintes passagères du bouton : rien au survol (UiButtonFx s'en charge), plus sombre quand on appuie.
        static void SetTransition(Selectable selectable)
        {
            selectable.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.55f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            selectable.colors = colors;
        }

        // ---------------------------------------------------------------- curseurs

        // Piste sombre bordée d'or, remplissage coloré (une couleur par réglage), poignée en losange doré.
        static void StyleSlider(Slider slider, UiTheme theme, HashSet<Graphic> done)
        {
            var background = slider.transform.Find("Background");
            if (background != null && background.TryGetComponent<Image>(out var track))
            {
                done.Add(track);
                SetSliced(track, theme.plainFill, theme.sliderTrack, 1f);
                Decoration(track.transform, BorderName, theme.plainBorder, theme.panelBorderColor, 1f, Image.Type.Sliced);
            }

            if (slider.fillRect != null && slider.fillRect.TryGetComponent<Image>(out var fill))
            {
                done.Add(fill);
                SetSliced(fill, theme.plainFill, FillColor(slider, theme), 1f);
            }

            if (slider.handleRect != null && slider.handleRect.TryGetComponent<Image>(out var handle))
            {
                done.Add(handle);
                SetSliced(handle, theme.plainFill, theme.sliderHandle, 1f);
                Decoration(handle.transform, BorderName, theme.plainBorder, new Color(0.45f, 0.25f, 0.05f), 1f, Image.Type.Sliced);

                // Un losange : carré, centré en hauteur, tourné de 45°.
                var size = 30f;
                if (slider.TryGetComponent<LayoutElement>(out var layout) && layout.preferredHeight > 0f)
                    size = layout.preferredHeight * 0.75f;
                var rect = slider.handleRect;
                rect.anchorMin = new Vector2(rect.anchorMin.x, 0.5f);
                rect.anchorMax = new Vector2(rect.anchorMax.x, 0.5f);
                rect.sizeDelta = new Vector2(size, size);
                rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            SetTransition(slider);
        }

        static Color FillColor(Slider slider, UiTheme theme)
        {
            var fills = theme.sliderFills;
            if (fills == null || fills.Length == 0)
                return theme.primaryButton;

            var row = slider.transform.parent != null ? slider.transform.parent.name.ToLowerInvariant() : "";
            var index = Has(row, "général", "general", "master") ? 0 : Has(row, "musique", "music") ? 1 : Has(row, "effet", "effect") ? 2 :
                        Has(row, "vignette", "confort") ? 3 : 2;
            return fills[Mathf.Min(index, fills.Length - 1)];
        }

        // ---------------------------------------------------------------- textes

        static void StyleText(TMP_Text text, UiTheme theme)
        {
            var white = text.color.r > 0.9f && text.color.g > 0.9f && text.color.b > 0.9f;
            var inCard = text.GetComponentInParent<ShopCard>(true) != null;
            if (text.name == "Title" && !inCard)
            {
                // Titre de panneau : doré, souligné d'un trait orné de part et d'autre.
                SetFont(text, theme.titleFont);
                text.fontStyle &= ~FontStyles.Bold;
                text.color = Color.white;
                text.enableVertexGradient = true;
                text.colorGradient = new VertexGradient(theme.titleTop, theme.titleTop, theme.titleBottom, theme.titleBottom);
                ShrinkToFit(text);
                AddDividers(text, theme);
                return;
            }

            if (inCard && (text.name == "Title" || text.name == "Subtitle" || text.name == "Price") || Array.IndexOf(k_LabelNames, text.name) >= 0)
            {
                SetFont(text, theme.labelFont);
                text.fontStyle &= ~FontStyles.Bold;
                ShrinkToFit(text);
                if (text.name == "Wave")
                {
                    // Le numéro de vague (montre, panneau des vagues) : doré comme les titres.
                    text.color = Color.white;
                    text.enableVertexGradient = true;
                    text.colorGradient = new VertexGradient(theme.titleTop, theme.titleTop, theme.titleBottom, theme.titleBottom);
                }
                else if (white && !inCard)
                {
                    text.color = theme.labelColor;
                }

                return;
            }

            SetFont(text, theme.bodyFont);
            if (white)
                text.color = theme.textColor;
        }

        static void AddDividers(TMP_Text title, UiTheme theme)
        {
            if (theme.divider == null)
                return;

            var width = Mathf.Clamp(title.rectTransform.rect.width * 0.32f, 120f, 320f);
            foreach (var right in new[] { false, true })
            {
                var divider = Decoration(title.transform, right ? k_DividerRight : k_DividerLeft, theme.divider, theme.panelBorderColor, 1f, Image.Type.Sliced);
                var rect = divider.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.sizeDelta = new Vector2(width, 26f);
                rect.anchoredPosition = new Vector2(right ? 4f : -4f, 2f);
                rect.localScale = new Vector3(right ? -1f : 1f, 1f, 1f);
            }
        }

        static void SetFont(TMP_Text text, TMP_FontAsset font)
        {
            if (font == null || text.font == font)
                return;
            text.font = font;
            text.fontSharedMaterial = font.material;
        }

        // Cinzel est plus large que l'ancienne police : le texte rétrécit un peu s'il ne tient pas (jamais plus grand qu'avant).
        static void ShrinkToFit(TMP_Text text)
        {
            if (text.enableAutoSizing)
                return;
            // (Les textes 3D ont de toutes petites tailles : 1,3 à 3,5 pour les panneaux de difficulté.)
            text.fontSizeMax = text.fontSize;
            text.fontSizeMin = text.fontSize * 0.6f;
            text.enableAutoSizing = true;
        }

        // ---------------------------------------------------------------- outils

        static void SetSliced(Image image, Sprite sprite, Color color, float pixelsPerUnitMultiplier)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            }

            image.color = color;
        }

        // Un ornement (bordure, trait) enfant de cet objet, étiré sur lui, sous ses autres enfants : repris s'il existe déjà.
        static Image Decoration(Transform parent, string name, Sprite sprite, Color color, float pixelsPerUnitMultiplier, Image.Type type)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                var rect = (RectTransform)go.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                go.transform.SetAsFirstSibling();
            }

            GetOrAdd<LayoutElement>(go).ignoreLayout = true;
            var image = GetOrAdd<Image>(go);
            image.raycastTarget = false;
            image.sprite = sprite;
            image.type = sprite != null ? type : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            image.color = color;
            return image;
        }

        static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }
    }
}
