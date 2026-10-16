using System.Collections.Generic;
using Archery.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.UI
{
    /// <summary>
    /// Messages importants (vagues, boss, coffres, cristaux, constructions, mort) : un bandeau sombre et lisible qui reste
    /// dans le champ de vision le temps du message. Un texte posé dans le monde se ratait dès qu'on tournait la tête.
    /// </summary>
    /// <remarks>
    /// Rien à monter dans la scène : le bandeau se crée au premier message. Il se tient un peu au-dessus du regard,
    /// ne bouge pas tant que le joueur regarde à peu près dans sa direction, puis glisse pour revenir devant lui.
    /// Les messages s'empilent vers le haut (3 au plus) et s'effacent tout seuls. Il suit le temps réel (ralenti des coffres).
    /// </remarks>
    public class HeadMessages : MonoBehaviour
    {
        const float k_Distance = 2f;            // m devant les yeux
        const float k_Height = 0.12f;           // m : bas de la pile, au-dessus du regard
        const float k_PixelSize = 0.001f;       // m par unité du canevas
        const float k_FontSize = 80f;           // 8 cm à 2 m, pour un message de taille 1
        const float k_MaxWidth = 1300f;         // largeur maximale d'une ligne (1,3 m)
        const float k_FollowAngle = 20f;        // écart entre le regard et le bandeau avant qu'il revienne
        const float k_SettleAngle = 4f;
        const float k_Smoothing = 0.2f;         // s
        const float k_MaxPitch = 30f;           // le bandeau ne suit pas un regard tourné vers le ciel ou les pieds
        const float k_Fade = 0.35f;             // s
        const int k_MaxMessages = 3;

        static readonly Color k_Backdrop = new Color(0.04f, 0.04f, 0.07f, 0.72f);

        class Entry
        {
            public GameObject Root;
            public CanvasGroup Group;
            public float Age;
            public float Duration;
        }

        static HeadMessages s_Instance;
        static UiTheme s_Theme;
        static bool s_ThemeLoaded;

        // Le thème des menus (panneau sombre à bordure dorée ornée, texte en Cinzel) ; sans lui, l'ancien bandeau.
        static UiTheme Theme
        {
            get
            {
                if (!s_ThemeLoaded)
                {
                    s_ThemeLoaded = true;
                    s_Theme = UiTheme.Load();
                }

                return s_Theme;
            }
        }

        readonly List<Entry> m_Entries = new List<Entry>();
        RectTransform m_Stack;
        bool m_Recentering;
        bool m_Placed;

        /// <summary>
        /// Affiche un message devant le joueur.
        /// <paramref name="size"/> : 1 pour un message courant, 1,4 pour un titre (début de vague, boss, victoire).
        /// </summary>
        public static void Show(string text, Color color, float duration = 4f, float size = 1f)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (s_Instance == null)
                s_Instance = Create();
            s_Instance.Add(text, color, duration, size);
        }

        static HeadMessages Create()
        {
            var go = new GameObject("[Head Messages]", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(k_MaxWidth + 200f, 1200f);
            rect.localScale = Vector3.one * k_PixelSize;

            var stack = new GameObject("Stack", typeof(RectTransform));
            var stackRect = (RectTransform)stack.transform;
            stackRect.SetParent(rect, false);
            stackRect.anchorMin = stackRect.anchorMax = new Vector2(0.5f, 0.5f);
            stackRect.pivot = new Vector2(0.5f, 0f);
            stackRect.anchoredPosition = Vector2.zero;
            var layout = stack.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.spacing = 14f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = stack.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var messages = go.AddComponent<HeadMessages>();
            messages.m_Stack = stackRect;
            return messages;
        }

        void Add(string text, Color color, float duration, float size)
        {
            while (m_Entries.Count >= k_MaxMessages)
                Remove(m_Entries[0]);

            var root = new GameObject("Message", typeof(RectTransform));
            root.transform.SetParent(m_Stack, false);
            root.transform.SetAsLastSibling();
            var backdrop = root.AddComponent<Image>();
            backdrop.color = k_Backdrop;
            backdrop.raycastTarget = false;
            var padding = Mathf.RoundToInt(28f * size);
            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(padding * 2, padding * 2, padding, padding);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var label = new GameObject("Text", typeof(RectTransform));
            label.transform.SetParent(root.transform, false);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = k_FontSize * size;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;

            var theme = Theme;
            if (theme != null)
            {
                // Coins à la mesure du message (plus grands pour un titre), et deux traits ornés sous les grands messages.
                UiThemer.ApplyPanel(backdrop, theme, 18f * size, 0.86f);
                UiThemer.ApplyLabel(tmp, theme);
                if (size >= 1.3f)
                    UiThemer.AddSideOrnaments((RectTransform)root.transform, theme, new Vector2(0f, padding * 0.5f), 8f, 220f * size, 24f,
                                              new Vector2(0.5f, 0f));
            }

            var element = label.AddComponent<LayoutElement>();
            element.preferredWidth = Mathf.Min(k_MaxWidth, tmp.GetPreferredValues(text, float.PositiveInfinity, float.PositiveInfinity).x + 4f);

            var entry = new Entry { Root = root, Group = root.AddComponent<CanvasGroup>(), Duration = Mathf.Max(1f, duration) };
            entry.Group.alpha = 0f;
            m_Entries.Add(entry);

            // Un nouveau message revient toujours devant le regard.
            m_Recentering = true;
            if (!m_Placed)
                SnapToHead();
        }

        void Remove(Entry entry)
        {
            m_Entries.Remove(entry);
            if (entry.Root != null)
                Destroy(entry.Root);
        }

        void LateUpdate()
        {
            var deltaTime = Time.unscaledDeltaTime;
            for (var i = m_Entries.Count - 1; i >= 0; i--)
            {
                var entry = m_Entries[i];
                entry.Age += deltaTime;
                if (entry.Age >= entry.Duration)
                {
                    Remove(entry);
                    continue;
                }

                var fadeIn = Mathf.Clamp01(entry.Age / k_Fade);
                var fadeOut = Mathf.Clamp01((entry.Duration - entry.Age) / k_Fade);
                entry.Group.alpha = Mathf.Min(fadeIn, fadeOut);
            }

            if (m_Entries.Count == 0)
            {
                m_Placed = false;
                return;
            }

            Follow(deltaTime);
        }

        // Le bandeau reste en place tant que le joueur le voit ; s'il tourne la tête, il glisse pour revenir devant lui.
        void Follow(float deltaTime)
        {
            if (!TryGetTarget(out var head, out var target))
                return;

            var current = transform.position - head;
            var wanted = target - head;
            var angle = Vector3.Angle(current, wanted);
            if (angle > k_FollowAngle)
                m_Recentering = true;
            else if (angle < k_SettleAngle)
                m_Recentering = false;

            if (m_Recentering)
            {
                var t = 1f - Mathf.Exp(-deltaTime / k_Smoothing);
                var direction = Vector3.Slerp(current.normalized, wanted.normalized, t);
                transform.position = head + direction * k_Distance;
            }
            else
            {
                // Garde sa direction, mais suit les déplacements du joueur.
                transform.position = head + current.normalized * k_Distance;
            }

            transform.rotation = Quaternion.LookRotation(transform.position - head, Vector3.up);
        }

        void SnapToHead()
        {
            if (!TryGetTarget(out var head, out var target))
                return;

            transform.SetPositionAndRotation(target, Quaternion.LookRotation(target - head, Vector3.up));
            m_Placed = true;
            m_Recentering = false;
        }

        // Point visé : devant le regard (inclinaison limitée), un peu au-dessus.
        static bool TryGetTarget(out Vector3 head, out Vector3 target)
        {
            head = target = default;
            var rig = PlayerRig.Instance;
            var headTransform = rig != null ? rig.Head : null;
            if (headTransform == null && Camera.main != null)
                headTransform = Camera.main.transform;
            if (headTransform == null)
                return false;

            head = headTransform.position;
            var forward = headTransform.forward;
            var flat = new Vector3(forward.x, 0f, forward.z);
            if (flat.sqrMagnitude < 1e-4f)
                flat = headTransform.up;
            flat.y = 0f;
            flat.Normalize();
            var pitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg, -k_MaxPitch, k_MaxPitch);
            var direction = Quaternion.AngleAxis(-pitch, Vector3.Cross(Vector3.up, flat)) * flat;
            var up = Vector3.Cross(direction, Vector3.Cross(Vector3.up, direction)).normalized;
            if (Vector3.Dot(up, Vector3.up) < 0f)
                up = -up;
            target = head + direction * k_Distance + up * k_Height;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Instance = null;
            s_Theme = null;
            s_ThemeLoaded = false;
        }
    }
}
