using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.Player
{
    /// <summary>
    /// Sans casque : le viseur au centre de l'écran, l'action de la touche E sous le viseur (« E · Récolter »…),
    /// et un rappel des touches en bas de l'écran. Créé par la <see cref="DesktopHand"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class DesktopCrosshair : MonoBehaviour
    {
        static readonly Color k_Ready = Color.white;
        static readonly Color k_Unavailable = new Color(1f, 1f, 1f, 0.45f);

        Canvas m_Canvas;
        Image m_Dot;
        TMP_Text m_Prompt;
        TMP_Text m_Help;
        TMP_Text m_Center;
        string m_PromptText;
        bool m_PromptReady;

        /// <summary>Crée le viseur, caché tant que <see cref="SetVisible"/> ne l'affiche pas.</summary>
        public static DesktopCrosshair Create(Transform parent)
        {
            var go = new GameObject("Desktop Crosshair", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var crosshair = go.AddComponent<DesktopCrosshair>();
            crosshair.Build();
            crosshair.SetVisible(false);
            return crosshair;
        }

        void Build()
        {
            m_Canvas = gameObject.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_Canvas.sortingOrder = 500;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            // Le point du viseur, cerclé de noir pour se voir sur le ciel comme sur la neige.
            var outline = CreateImage("Crosshair Outline", new Vector2(10f, 10f), new Color(0f, 0f, 0f, 0.6f));
            m_Dot = CreateImage("Crosshair", new Vector2(6f, 6f), k_Ready);
            m_Dot.transform.SetParent(outline.transform, false);

            m_Prompt = CreateText("Prompt", new Vector2(0f, -46f), 30f, TextAlignmentOptions.Center);
            m_Center = CreateText("Center Message", new Vector2(0f, 80f), 34f, TextAlignmentOptions.Center);

            m_Help = CreateText("Help", Vector2.zero, 20f, TextAlignmentOptions.Bottom);
            var helpRect = m_Help.rectTransform;
            helpRect.anchorMin = new Vector2(0f, 0f);
            helpRect.anchorMax = new Vector2(1f, 0f);
            helpRect.pivot = new Vector2(0.5f, 0f);
            helpRect.anchoredPosition = new Vector2(0f, 14f);
            helpRect.sizeDelta = new Vector2(0f, 40f);
            m_Help.color = new Color(1f, 1f, 1f, 0.55f);
            m_Help.text = "ZQSD : marcher · Maj : courir · Espace : sauter · Souris : regarder · " +
                          "Clic gauche maintenu : tendre l'arc, relâcher : tirer · E : interagir · Échap : libérer la souris";
        }

        Image CreateImage(string objectName, Vector2 size, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        TMP_Text CreateText(string objectName, Vector2 position, float size, TextAlignmentOptions alignment)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(0, 0, 0, 200);
            text.rectTransform.sizeDelta = new Vector2(1400f, 60f);
            text.rectTransform.anchoredPosition = position;
            return text;
        }

        public void SetVisible(bool visible)
        {
            if (m_Canvas != null && m_Canvas.enabled != visible)
                m_Canvas.enabled = visible;
        }

        /// <summary>
        /// L'action de la touche E sur ce qui est visé (vide : rien). Pas prête (trop loin, pas assez d'or…) : grisée,
        /// sans la touche.
        /// </summary>
        public void SetPrompt(string text, bool ready)
        {
            if (text == m_PromptText && ready == m_PromptReady)
                return;

            m_PromptText = text;
            m_PromptReady = ready;
            m_Prompt.text = string.IsNullOrEmpty(text) ? "" : ready ? "<b>E</b>  ·  " + text : text;
            m_Prompt.color = ready ? k_Ready : k_Unavailable;
            m_Dot.color = string.IsNullOrEmpty(text) || !ready ? k_Ready : new Color(1f, 0.85f, 0.35f);
        }

        /// <summary>Message au-dessus du viseur (« Clic gauche pour jouer »), vide pour le cacher.</summary>
        public void SetCenterMessage(string text)
        {
            if (m_Center.text != text)
                m_Center.text = text;
        }
    }
}
