using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Archery.UI
{
    /// <summary>
    /// Texte qui apparaît dans le monde et s'efface (points, dégâts, « Parfait ! »).
    /// Sa taille suit la distance pour rester lisible de loin.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        // Le budget d'effets (27 octobre au soir, jusqu'à 1000 flèches en vol) : tant de textes affichés au plus ; et les chiffres
        // de dégâts se taisent dès que la moitié est prise, pour laisser la place aux autres (points, « Chargée ! », « Parfait ! »).
        const int k_MaxTexts = 48;
        const int k_CrowdedTexts = 24;

        static readonly Stack<FloatingText> s_Pool = new Stack<FloatingText>();
        static Transform s_Root;
        static Camera s_Camera;
        static int s_Created;

        /// <summary>Beaucoup de textes à l'écran : les chiffres de dégâts (<see cref="DamagePopups"/>) attendent.</summary>
        public static bool Crowded => s_Root != null && s_Created - s_Pool.Count >= k_CrowdedTexts;

        TextMeshPro m_Text;
        Color m_Color;
        bool m_Themed;
        Vector3 m_Velocity;
        float m_Age;
        float m_Duration;
        float m_Size;

        public static void Spawn(Vector3 position, string text, Color color, float size = 1f, float duration = 1.2f)
        {
            // Une nouvelle scène a détruit les textes : on recompte.
            if (s_Root == null)
            {
                s_Pool.Clear();
                s_Created = 0;
            }

            FloatingText item = null;
            while (item == null && s_Pool.Count > 0)
                item = s_Pool.Pop();
            if (item == null)
            {
                if (s_Created >= k_MaxTexts)
                    return;
                item = Create();
                s_Created++;
            }

            item.Show(position, text, color, size, duration);
        }

        static FloatingText Create()
        {
            if (s_Root == null)
                s_Root = new GameObject("[Floating Texts]").transform;

            var go = new GameObject("Floating Text");
            go.transform.SetParent(s_Root, false);
            var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontSize = 2.5f;
            text.fontStyle = FontStyles.Bold;

            // Le thème des menus : en Cinzel, dégradé du clair vers la couleur du texte, contour violet très sombre.
            var theme = UiTheme.Load();
            if (theme != null)
                UiThemer.ApplyLabel(text, theme);
            text.outlineWidth = 0.2f;
            text.outlineColor = theme != null ? new Color32(18, 8, 34, 220) : new Color32(0, 0, 0, 200);

            var item = go.AddComponent<FloatingText>();
            item.m_Text = text;
            item.m_Themed = theme != null;
            return item;
        }

        void Show(Vector3 position, string text, Color color, float size, float duration)
        {
            gameObject.SetActive(true);
            transform.position = position;
            m_Text.text = text;
            m_Color = color;
            m_Text.color = color;
            if (m_Themed)
            {
                // La teinte passe dans le dégradé ; la couleur du texte ne garde que la transparence (le fondu).
                var top = Color.Lerp(color, Color.white, 0.55f);
                m_Text.enableVertexGradient = true;
                m_Text.colorGradient = new VertexGradient(top, top, color, color);
                m_Color = new Color(1f, 1f, 1f, color.a);
                m_Text.color = m_Color;
            }
            m_Size = size;
            m_Duration = Mathf.Max(0.1f, duration);
            m_Age = 0f;
            m_Velocity = Vector3.up * 0.6f;
            UpdateTransform(0f);
        }

        void LateUpdate()
        {
            m_Age += Time.deltaTime;
            var t = m_Age / m_Duration;
            if (t >= 1f)
            {
                gameObject.SetActive(false);
                s_Pool.Push(this);
                return;
            }

            transform.position += m_Velocity * Time.deltaTime;
            m_Velocity *= 1f - Mathf.Clamp01(2f * Time.deltaTime);
            UpdateTransform(t);

            var color = m_Color;
            color.a *= t < 0.7f ? 1f : 1f - (t - 0.7f) / 0.3f;
            m_Text.color = color;
        }

        void UpdateTransform(float t)
        {
            if (s_Camera == null)
                s_Camera = Camera.main;

            var scale = m_Size;
            if (s_Camera != null)
            {
                var cameraTransform = s_Camera.transform;
                var toText = transform.position - cameraTransform.position;
                if (toText.sqrMagnitude > 1e-6f)
                    transform.rotation = Quaternion.LookRotation(toText, cameraTransform.up);
                scale *= Mathf.Max(0.35f, toText.magnitude * 0.1f);
            }

            // Petit effet de « pop » à l'apparition.
            var pop = t < 0.15f ? Mathf.Lerp(0.6f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.15f) / 0.15f));
            transform.localScale = Vector3.one * (scale * pop);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Pool.Clear();
            s_Root = null;
            s_Camera = null;
            s_Created = 0;
        }
    }
}
