using System;
using System.Collections.Generic;
using Archery.Core;
using Archery.UI;
using Archery.Waves;
using TMPro;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Le nom du jeu (<see cref="GameInfo.Name"/>), gravé dans la roche au-dessus de la porte de l'antre.
    /// Un cartouche de pierre taillée est encastré dans la falaise : une dalle en retrait dans un cadre de blocs qui
    /// avancent davantage, une clé au sommet et une oreille en losange de chaque côté, dans la pierre du porche.
    /// Dans la dalle, les lettres sont creusées (police des titres du thème des menus) : un creux sombre, sa lèvre du bas
    /// éclairée par la lave, et au fond la lave, plus claire en bas des lettres, où elle s'accumule.
    /// Quelques lumières éclairent la pierre et des braises montent.
    /// Il bat au rythme des veines de l'antre, de plus en plus fort au fil des vagues, et s'embrase quand la porte s'ouvre.
    /// </summary>
    public partial class LairGate
    {
        // La face de la montagne derrière le porche (repère de la porte) : 21 m de large ; au-dessus de 15 m, la lèvre
        // de la falaise avance.
        const float k_FaceHalfWidth = 10.3f;
        const float k_FaceTop = 14.9f;

        // Le cartouche (m) : marges de la dalle autour des lettres, largeur du cadre, oreilles, et ce qui avance devant
        // la face (la dalle un peu, le cadre davantage : la dalle est en retrait) ou s'enfonce dans la falaise.
        const float k_PanelMarginX = 0.8f;
        const float k_PanelMarginY = 0.55f;
        const float k_FrameWidth = 0.55f;
        const float k_EarSize = 0.65f;       // demi-diagonale des oreilles en losange
        const float k_SlabOut = 0.32f;
        const float k_FrameOut = 0.7f;
        const float k_Embed = 0.35f;

        // Les lettres creusées (m) : la lèvre éclairée dépasse sous le creux, la lave est au fond, un peu plus bas.
        const float k_LipDrop = 0.06f;
        const float k_LavaDrop = 0.035f;
        static readonly Color k_GrooveColor = new Color(0.045f, 0.028f, 0.022f, 1f);
        static readonly Color k_LipColor = new Color(0.5f, 0.29f, 0.16f, 1f);

        [Header("Titre gravé dans la roche")]
        [Tooltip("Graver le nom du jeu dans un cartouche de pierre, sur la face de la montagne au-dessus du porche.")]
        [SerializeField]
        bool m_EngraveTitle = true;

        [Tooltip("Autre texte à graver. Vide : le nom du jeu (GameInfo).")]
        [SerializeField]
        string m_TitleOverride = "";

        [Tooltip("Hauteur des majuscules (m). Si le titre est trop long pour la face, il rapetisse.")]
        [SerializeField]
        float m_TitleCapHeight = 2.1f;

        [Tooltip("Largeur maximale des lettres (m). Le cartouche les entoure, et la face fait 21 m de large : au-delà de " +
                 "15 m environ, elles rapetissent quand même.")]
        [SerializeField]
        float m_TitleMaxWidth = 17.5f;

        [Tooltip("Hauteur (m) du milieu des lettres : assez haut pour qu'on le voie par-dessus le porche en arrivant par la voie. " +
                 "Le cartouche ne descend jamais sur le porche.")]
        [SerializeField]
        float m_TitleHeight = 13.2f;

        [Tooltip("Espace entre les lettres (unités de TextMesh Pro).")]
        [SerializeField]
        float m_TitleSpacing = 8f;

        [Tooltip("Couleur de la lave au fond des lettres (HDR : au-dessus de 1, le bloom la fait rayonner).")]
        [ColorUsage(false, true)]
        [SerializeField]
        Color m_TitleLava = new Color(4f, 1.45f, 0.32f);

        [Tooltip("Change le dessin des pierres du cadre.")]
        [SerializeField]
        int m_TitleSeed = 11;

        [Tooltip("Force des lumières posées devant le titre (elles éclairent le cartouche et la roche autour).")]
        [SerializeField]
        float m_TitleLightIntensity = 5f;

        Material m_TitleMaterial;
        Material m_TitleLipMaterial;
        Light[] m_TitleLights = Array.Empty<Light>();
        Rect m_TitleArea;
        float m_TitleAwake;

        /// <summary>
        /// Le cartouche du titre, dans le repère de la porte (x, y) sur la face de la montagne : les veines de l'antre
        /// ne passent pas dedans. Vide s'il n'y a pas de titre.
        /// </summary>
        public Rect TitleArea => m_TitleArea;

        string EngravedTitle => !string.IsNullOrWhiteSpace(m_TitleOverride) ? m_TitleOverride.Trim() : GameInfo.Name;

        void BuildTitle(Transform root)
        {
            m_TitleMaterial = null;
            m_TitleLipMaterial = null;
            m_TitleLights = Array.Empty<Light>();
            m_TitleArea = Rect.zero;
            var title = EngravedTitle;
            if (!m_EngraveTitle || string.IsNullOrWhiteSpace(title))
                return;

            var theme = UiTheme.Load();
            var font = theme != null && theme.titleFont != null ? theme.titleFont : TMP_Settings.defaultFontAsset;
            if (font == null)
                return;

            var face = m_Depth;
            var slab = face - k_SlabOut;

            // Le creux des lettres donne la mise en page (taille et place) ; la lèvre et la lave le copient.
            var groove = CreateTitleText(root, "Titre (creux)", font, title, 1);
            var info = font.faceInfo;
            var cap = info.capLine > 0f && info.pointSize > 0f ? info.capLine / info.pointSize * Mathf.Max(0.01f, info.scale) : 0.7f;
            groove.fontSize = Mathf.Max(0.1f, m_TitleCapHeight) / (cap * 0.1f);
            var maxWidth = Mathf.Min(m_TitleMaxWidth, 2f * (k_FaceHalfWidth - 0.2f - k_PanelMarginX - k_FrameWidth - 1.55f * k_EarSize));
            groove.rectTransform.sizeDelta = new Vector2(maxWidth + 6f, m_TitleCapHeight * 3f);
            var width = groove.GetPreferredValues(title).x;
            if (width > maxWidth && width > 0f)
                groove.fontSize *= maxWidth / width;
            groove.ForceMeshUpdate();

            // Les lettres (les bornes des glyphes, pas celles de la boîte du texte), recentrées sur la face ; le cartouche
            // reste au-dessus du porche.
            var glyphs = new List<Rect>();
            var characters = groove.textInfo.characterInfo;
            for (var i = 0; i < groove.textInfo.characterCount; i++)
            {
                if (characters[i].isVisible)
                    glyphs.Add(Rect.MinMaxRect(characters[i].bottomLeft.x, characters[i].bottomLeft.y, characters[i].topRight.x, characters[i].topRight.y));
            }

            if (glyphs.Count == 0)
                return;

            var letters = glyphs[0];
            foreach (var glyph in glyphs)
                letters = Rect.MinMaxRect(Mathf.Min(letters.xMin, glyph.xMin), Mathf.Min(letters.yMin, glyph.yMin), Mathf.Max(letters.xMax, glyph.xMax), Mathf.Max(letters.yMax, glyph.yMax));

            var lowest = PorchTop + 0.35f + k_FrameWidth + k_PanelMarginY + letters.height * 0.5f;
            var shift = new Vector2(-letters.center.x, Mathf.Max(m_TitleHeight, lowest) - letters.center.y);
            letters = new Rect(letters.position + shift, letters.size);

            // Trois couches, de l'arrière vers l'avant : la lèvre éclairée (elle dépasse sous le creux), le creux, la lave.
            groove.transform.localPosition = new Vector3(shift.x, shift.y, slab - 0.008f);
            groove.fontSharedMaterial = TitleMaterial(font, "Titre de l'antre (creux)", k_GrooveColor, 0.05f) ?? groove.fontSharedMaterial;

            var lip = CopyTitleText(root, "Titre (lèvre)", groove, 0, new Vector3(shift.x, shift.y - k_LipDrop, slab - 0.004f));
            m_TitleLipMaterial = TitleMaterial(font, "Titre de l'antre (lèvre)", k_LipColor, 0.3f);
            if (m_TitleLipMaterial != null)
                lip.fontSharedMaterial = m_TitleLipMaterial;

            var lava = CopyTitleText(root, "Titre (lave)", groove, 2, new Vector3(shift.x, shift.y - k_LavaDrop, slab - 0.012f));
            var hot = new Color(1f, 0.92f, 0.55f);
            var warm = new Color(1f, 0.45f, 0.14f);
            lava.enableVertexGradient = true;
            lava.colorGradient = new VertexGradient(warm, warm, hot, hot);
            m_TitleMaterial = TitleMaterial(font, "Titre de l'antre (lave)", m_TitleLava, -0.22f);
            if (m_TitleMaterial != null)
            {
                // Une lueur douce autour de la lave : elle rougit le creux et le bord de la dalle.
                m_TitleMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.3f);
                m_TitleMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.55f);
                ShaderUtilities.UpdateShaderRatios(m_TitleMaterial);
                lava.fontSharedMaterial = m_TitleMaterial;
            }

            foreach (var layer in new[] { lip, groove, lava })
                layer.ForceMeshUpdate();

            var random = new System.Random(m_TitleSeed);
            var panel = Rect.MinMaxRect(letters.xMin - k_PanelMarginX, letters.yMin - k_PanelMarginY, letters.xMax + k_PanelMarginX, letters.yMax + k_PanelMarginY);
            var outer = BuildCartouche(root, panel, face, random);
            m_TitleArea = Rect.MinMaxRect(outer.xMin - 0.3f, outer.yMin - 0.3f, outer.xMax + 0.3f, outer.yMax + 0.3f);
            BuildTitleLights(root, letters, slab);

            // Des braises qui montent devant les lettres.
            if (m_FireMaterial != null)
            {
                var embers = FireEffects.CreateEmbers(root, new Vector3(letters.center.x, letters.center.y, slab - 0.3f), m_FireMaterial, 1f, 14f);
                embers.name = "Braises du titre";
                var shape = embers.shape;
                shape.scale = new Vector3(letters.width, 0.3f, letters.height);
                var main = embers.main;
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
                main.maxParticles = 160;
            }

            ApplyTitleGlow(1f, 0.45f);
        }

        TextMeshPro CreateTitleText(Transform root, string name, TMP_FontAsset font, string title, int order)
        {
            var text = m_Generated.Create(name, root).AddComponent<TextMeshPro>();
            text.font = font;
            text.text = title;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.characterSpacing = m_TitleSpacing;
            text.color = Color.white;
            text.extraPadding = true;
            text.sortingOrder = order;
            return text;
        }

        // Une autre couche des lettres, à la même taille, posée ailleurs (TextMesh Pro regarde vers -Z : vers la voie).
        TextMeshPro CopyTitleText(Transform root, string name, TextMeshPro source, int order, Vector3 position)
        {
            var text = CreateTitleText(root, name, source.font, source.text, order);
            text.fontSize = source.fontSize;
            text.rectTransform.sizeDelta = source.rectTransform.sizeDelta;
            text.transform.localPosition = position;
            return text;
        }

        // Une couche des lettres : le matériau de la police (ses options de contour et de fond restent dans le jeu
        // construit), de cette couleur, plus ou moins épaisse, sans contour ni fond.
        Material TitleMaterial(TMP_FontAsset font, string name, Color face, float dilate)
        {
            var material = m_Generated.CloneMaterial(font.material, name);
            if (material == null)
                return null;

            material.SetTexture(ShaderUtilities.ID_MainTex, font.atlasTexture);
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_FaceColor, face);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, dilate);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0f);
            material.SetFloat(ShaderUtilities.ID_OutlineSoftness, 0f);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, Color.clear);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
            ShaderUtilities.UpdateShaderRatios(material);
            return material;
        }

        // ---------------------------------------------------------------- le cartouche

        // Une dalle en retrait, encadrée de blocs taillés qui avancent davantage (chacun de profondeur un peu différente et
        // légèrement de travers), une clé au sommet et une oreille en losange de chaque côté. Toutes les pierres
        // s'enfoncent dans la falaise. Rend le contour du cartouche, oreilles comprises.
        Rect BuildCartouche(Transform root, Rect panel, float face, System.Random random)
        {
            var stone = new MeshBuilder();
            var back = face + k_Embed;
            var frame = face - k_FrameOut;
            var w = k_FrameWidth;

            // La dalle : elle passe un peu sous le cadre.
            Block(stone, Rect.MinMaxRect(panel.xMin - w * 0.5f, panel.yMin - w * 0.5f, panel.xMax + w * 0.5f, panel.yMax + w * 0.5f), face - k_SlabOut, back);

            // En haut, une rangée de blocs ; en bas, un appui qui avance un peu plus et déborde des côtés.
            Row(stone, panel.xMin - w, panel.xMax + w, panel.yMax, panel.yMax + w, frame, back, random, 1.1f, 1.8f);
            Row(stone, panel.xMin - w - 0.25f, panel.xMax + w + 0.25f, panel.yMin - w - 0.08f, panel.yMin, frame - 0.15f, back, random, 1.2f, 2f);

            // Les côtés : des blocs debout.
            Column(stone, panel.xMin - w, panel.xMin, panel.yMin, panel.yMax, frame, back, random);
            Column(stone, panel.xMax, panel.xMax + w, panel.yMin, panel.yMax, frame, back, random);

            // La clé, au milieu du haut, plus grosse et plus en avant (comme celle de l'arche du porche).
            var keyFront = frame - 0.18f;
            stone.Box(new Vector3(panel.center.x, panel.yMax + w * 0.5f + 0.06f, (keyFront + back) * 0.5f),
                      new Vector3(0.55f, w * 0.5f + 0.16f, (back - keyFront) * 0.5f), Quaternion.identity, Color.white);

            // Les oreilles : un losange de chaque côté, à mi-hauteur, un peu en retrait du cadre.
            var earFront = frame + 0.12f;
            var earHalf = k_EarSize * 0.7071f;
            foreach (var side in new[] { -1f, 1f })
            {
                var x = side < 0f ? panel.xMin - w - k_EarSize * 0.55f : panel.xMax + w + k_EarSize * 0.55f;
                stone.Box(new Vector3(x, panel.center.y, (earFront + back) * 0.5f), new Vector3(earHalf, earHalf, (back - earFront) * 0.5f),
                          Quaternion.Euler(0f, 0f, 45f), Color.white);
            }

            m_Generated.CreateMesh("Cartouche du titre", root, stone.ToMesh("Titre (cartouche)"), m_StoneMaterial, collider: true, castShadows: true);
            var ears = 1.55f * k_EarSize;
            return Rect.MinMaxRect(panel.xMin - w - ears, panel.yMin - w - 0.16f, panel.xMax + w + ears, panel.yMax + w + 0.22f);
        }

        static void Block(MeshBuilder builder, Rect rect, float front, float back)
        {
            builder.Box(new Vector3(rect.center.x, rect.center.y, (front + back) * 0.5f),
                        new Vector3(rect.width * 0.5f, rect.height * 0.5f, (back - front) * 0.5f), Quaternion.identity, Color.white);
        }

        // Une rangée de blocs de x0 à x1, entre y0 et y1, de longueurs tirées entre min et max (pas de tout petit bloc au
        // bout), avec un joint de 3 cm ; chacun avance un peu plus ou un peu moins que « front » et penche à peine.
        static void Row(MeshBuilder builder, float x0, float x1, float y0, float y1, float front, float back, System.Random random, float min, float max)
        {
            var x = x0;
            while (x < x1 - 0.05f)
            {
                var length = Mathf.Min(Range(random, min, max), x1 - x);
                if (x1 - (x + length) < min * 0.5f)
                    length = x1 - x;
                Hewn(builder, new Vector2(x + length * 0.5f, (y0 + y1) * 0.5f), new Vector2(length - 0.03f, y1 - y0), front, back, random);
                x += length;
            }
        }

        // Une colonne de blocs debout de y0 à y1, entre x0 et x1.
        static void Column(MeshBuilder builder, float x0, float x1, float y0, float y1, float front, float back, System.Random random)
        {
            var y = y0;
            while (y < y1 - 0.05f)
            {
                var length = Mathf.Min(Range(random, 0.8f, 1.3f), y1 - y);
                if (y1 - (y + length) < 0.4f)
                    length = y1 - y;
                Hewn(builder, new Vector2((x0 + x1) * 0.5f, y + length * 0.5f), new Vector2(x1 - x0, length - 0.03f), front, back, random);
                y += length;
            }
        }

        // Un bloc taillé : sa face avant à ±6 cm de « front », un peu de travers.
        static void Hewn(MeshBuilder builder, Vector2 center, Vector2 size, float front, float back, System.Random random)
        {
            var f = front + Range(random, -0.06f, 0.06f);
            var rotation = Quaternion.Euler(Range(random, -1.5f, 1.5f), Range(random, -1.5f, 1.5f), Range(random, -1.2f, 1.2f));
            builder.Box(new Vector3(center.x, center.y + Range(random, -0.015f, 0.015f), (f + back) * 0.5f),
                        new Vector3(size.x * 0.5f, size.y * 0.5f, (back - f) * 0.5f), rotation, Color.white);
        }

        // Quelques lumières chaudes devant les lettres : elles éclairent le cartouche, la roche et le haut du porche.
        void BuildTitleLights(Transform root, Rect letters, float slab)
        {
            var count = Mathf.Clamp(Mathf.RoundToInt(letters.width / 4.5f), 2, 6);
            m_TitleLights = new Light[count];
            for (var i = 0; i < count; i++)
            {
                var x = Mathf.Lerp(letters.xMin + 1f, letters.xMax - 1f, i / (float)(count - 1));
                var lightObject = m_Generated.Create("Lumière du titre", root);
                lightObject.transform.localPosition = new Vector3(x, letters.center.y - 0.2f, slab - 1.5f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.42f, 0.12f);
                light.range = 7.5f;
                light.intensity = m_TitleLightIntensity;
                light.shadows = LightShadows.None;
                m_TitleLights[i] = light;
            }
        }

        // ---------------------------------------------------------------- en jeu

        // Le titre bat au rythme des veines de l'antre (MapLairVeins), avec la lave qui respire ; plus fort au fil des
        // vagues, et il s'embrase quand la porte s'ouvre.
        void UpdateTitle()
        {
            if (m_TitleMaterial == null && m_TitleLights.Length == 0)
                return;

            var waves = WaveManager.Instance;
            var progress = 0f;
            var finalBoss = false;
            if (waves != null)
            {
                finalBoss = waves.IsFinalBossWave;
                progress = finalBoss ? 1f : Mathf.Clamp01(waves.WaveNumber / (float)Mathf.Max(1, waves.WavesToWin));
            }

            m_TitleAwake = Mathf.MoveTowards(m_TitleAwake, IsOpen ? 1f : 0f, Time.deltaTime * 0.5f);
            var period = Mathf.Lerp(1.7f, finalBoss ? 0.6f : 0.85f, progress);
            var t = Mathf.Repeat(Time.time, period) / period;
            var beat = Bump(t, 0.05f) + 0.6f * Bump(t, 0.24f);
            var breath = Mathf.PerlinNoise(Time.time * 0.6f, 0.37f);
            var glow = Mathf.Lerp(0.8f, 1.15f, progress) * (0.82f + 0.22f * beat + 0.12f * breath) * (1f + 0.5f * m_TitleAwake);
            ApplyTitleGlow(glow, Mathf.Clamp01(0.35f + 0.2f * beat + 0.25f * m_TitleAwake));
        }

        void ApplyTitleGlow(float glow, float halo)
        {
            if (m_TitleMaterial != null)
            {
                var lava = m_TitleLava * glow;
                lava.a = 1f;
                m_TitleMaterial.SetColor(ShaderUtilities.ID_FaceColor, lava);
                m_TitleMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(1f, 0.3f, 0.06f, halo));
            }

            // La lèvre est éclairée par la lave : elle suit son éclat.
            if (m_TitleLipMaterial != null)
            {
                var lip = k_LipColor * Mathf.Lerp(0.85f, 1.35f, Mathf.InverseLerp(0.7f, 1.8f, glow));
                lip.a = 1f;
                m_TitleLipMaterial.SetColor(ShaderUtilities.ID_FaceColor, lip);
            }

            foreach (var light in m_TitleLights)
            {
                if (light != null)
                    light.intensity = m_TitleLightIntensity * glow;
            }
        }

        void DrawTitleGizmo()
        {
            if (m_TitleArea.width <= 0f)
                return;
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(new Vector3(m_TitleArea.center.x, m_TitleArea.center.y, m_Depth), new Vector3(m_TitleArea.width, m_TitleArea.height, 0.05f));
        }

        static float Bump(float x, float center)
        {
            var d = (x - center) / 0.05f;
            return Mathf.Exp(-d * d);
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
