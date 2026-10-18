using System.Collections.Generic;
using Archery.Core;
using Archery.FinalBoss;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Archery.Portal
{
    /// <summary>
    /// Les panneaux d'erreur de la dimension (GDD, section 23, style bugué) : des fenêtres rouges au style futuriste,
    /// volontairement hors de la DA du jeu, qui s'ouvrent et se ferment vite un peu partout autour du joueur, presque
    /// chaque seconde. Certaines affichent « ERREUR » ou une fausse erreur, d'autres le temps restant, qu'on ne voit
    /// nulle part ailleurs.
    /// </summary>
    /// <remarks>
    /// Ce ne sont que des décors : pas de collision (les flèches les traversent), et jamais devant le monstre. Ils restent
    /// dans le monde, entre 2,2 et 6 m du joueur, et s'ouvrent en s'étirant (pas de clignotement). Les textes sont
    /// modifiables dans l'Inspector de la <see cref="StarDimension"/>.
    /// </remarks>
    public class ErrorPanels : MonoBehaviour
    {
        const float k_Width = 1.2f;
        const float k_Height = 0.62f;
        const float k_Cut = 0.07f;
        const float k_BaseDistance = 3.5f;
        const int k_MaxPanels = 8;
        const float k_OpenTime = 0.14f;
        const float k_CloseTime = 0.1f;

        static readonly Color k_Red = new Color(1f, 0.12f, 0.1f);
        static readonly Color k_Dark = new Color(0.12f, 0.01f, 0.02f);

        enum Kind
        {
            Error,
            Fake,
            Timer,
        }

        class Panel
        {
            public Transform Root;
            public TextMeshPro Title;
            public TextMeshPro Body;
            public Kind Kind;
            public float Age;
            public float Life;
            public Vector3 Scale;
            public bool Closing;

            /// <summary>Dixièmes de seconde affichés (panneau du temps restant) : le texte ne change qu'avec eux.</summary>
            public int ShownTenths = -1;
        }

        // La police futuriste, fabriquée une seule fois pour toutes les épreuves (une police dynamique a sa propre texture).
        static TMP_FontAsset s_Font;

        readonly List<Panel> m_Panels = new List<Panel>();
        StarDimension m_Dimension;
        Material m_Material;
        TMP_FontAsset m_Font;
        Mesh m_Frame;
        AudioClip m_OpenClip;
        AudioClip m_CloseClip;
        string[] m_FakeErrors;
        float m_Timer;
        float m_LastTimerPanel;
        float m_Clock;

        /// <summary>Pendant l'intro : moins de panneaux, et pas encore de compte à rebours.</summary>
        public bool Intro { get; set; } = true;

        public void Configure(StarDimension dimension, Material material, AudioClip open, AudioClip close, string[] fakeErrors)
        {
            m_Dimension = dimension;
            m_Material = material;
            m_OpenClip = open;
            m_CloseClip = close;
            m_FakeErrors = fakeErrors;
            m_Font = LoadFont();
            m_Frame = BuildFrame();
            m_Timer = 0.5f;
        }

        void OnDestroy()
        {
            if (m_Frame != null)
                Destroy(m_Frame);
        }

        // La police futuriste (Share Tech Mono, dans Resources/Fonts), gardée d'une épreuve à l'autre ; sans elle, celle
        // de TextMeshPro.
        static TMP_FontAsset LoadFont()
        {
            if (s_Font != null)
                return s_Font;

            var font = Resources.Load<Font>("Fonts/ShareTechMono-Regular");
            if (font != null)
                s_Font = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            return s_Font != null ? s_Font : TMP_Settings.defaultFontAsset;
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_Clock += deltaTime;
            m_Timer -= deltaTime;
            if (m_Timer <= 0f && m_Dimension != null && m_Dimension.Running)
            {
                m_Timer = Intro ? Random.Range(1.8f, 2.6f) : Random.Range(0.75f, 1.15f);
                Spawn();
            }

            for (var i = m_Panels.Count - 1; i >= 0; i--)
            {
                var panel = m_Panels[i];
                panel.Age += deltaTime;
                if (panel.Kind == Kind.Timer)
                {
                    // Le texte n'est refait (et remis en page) que quand le dixième de seconde affiché change.
                    var tenths = TimerTenths();
                    if (tenths != panel.ShownTenths)
                    {
                        panel.ShownTenths = tenths;
                        panel.Body.text = TimerText();
                    }
                }

                if (panel.Age >= panel.Life + k_CloseTime)
                {
                    Destroy(panel.Root.gameObject);
                    m_Panels.RemoveAt(i);
                    continue;
                }

                // S'ouvre en s'étirant (largeur, puis hauteur) ; se ferme en s'écrasant, comme une fenêtre qui plante.
                Vector2 scale;
                if (panel.Age < k_OpenTime)
                {
                    var t = panel.Age / k_OpenTime;
                    scale = new Vector2(Mathf.Clamp01(t * 2f), Mathf.Max(0.04f, Mathf.Clamp01(t * 2f - 1f)));
                }
                else if (panel.Age > panel.Life)
                {
                    var t = (panel.Age - panel.Life) / k_CloseTime;
                    scale = new Vector2(Mathf.Max(0f, 1f - Mathf.Clamp01(t * 2f - 1f)), Mathf.Max(0.04f, 1f - Mathf.Clamp01(t * 2f)));
                }
                else
                {
                    scale = Vector2.one;
                }

                panel.Root.localScale = new Vector3(panel.Scale.x * scale.x, panel.Scale.y * scale.y, 1f);
                if (!panel.Closing && panel.Age > panel.Life)
                {
                    panel.Closing = true;
                    Sfx.Play(m_CloseClip, panel.Root.position, 0.25f, Random.Range(0.9f, 1.2f));
                }
            }
        }

        // Le temps restant en dixièmes de seconde (-1 pendant la découverte : pas encore de compte à rebours).
        int TimerTenths() => Intro ? -1 : Mathf.FloorToInt(Mathf.Max(0f, m_Dimension != null ? m_Dimension.TimeLeft : 0f) * 10f);

        string TimerText()
        {
            var tenths = TimerTenths();
            if (tenths < 0)
                return "--:--.-";
            return $"00:{tenths / 10:00}.{tenths % 10}";
        }

        /// <summary>Ouvre un grand panneau au centre de la vue (fin de l'épreuve), qui reste <paramref name="life"/> s.</summary>
        public void ShowCentered(string title, string body, float life)
        {
            var head = m_Dimension != null ? m_Dimension.Head : null;
            if (head == null)
                return;
            var forward = Flat(head.forward);
            var position = head.position + forward * 2.6f + Vector3.up * 0.1f;
            Open(Kind.Fake, title, body, position, head.position, life, 1.3f);
        }

        void Spawn()
        {
            if (m_Panels.Count >= k_MaxPanels)
                return;

            var head = m_Dimension.Head;
            if (head == null)
                return;

            // Le compte à rebours revient au moins toutes les 2,5 s ; sinon, un panneau « ERREUR » ou une fausse erreur.
            Kind kind;
            if (!Intro && (m_Clock - m_LastTimerPanel > 2.5f || Random.value < 0.4f))
                kind = Kind.Timer;
            else
                kind = Random.value < 0.5f ? Kind.Error : Kind.Fake;

            if (!TryPlace(head, out var position))
                return;

            switch (kind)
            {
                case Kind.Timer:
                    m_LastTimerPanel = m_Clock;
                    Open(Kind.Timer, "TEMPS RESTANT", TimerText(), position, head.position, Random.Range(1.2f, 1.8f), 1f);
                    break;
                case Kind.Error:
                    Open(Kind.Error, "ERREUR", $"ERREUR\n<size=45%>0x{Random.Range(0, 0xFFFFFF):X6}</size>", position, head.position,
                         Random.Range(0.9f, 1.5f), 1f);
                    break;
                default:
                    var fake = m_FakeErrors != null && m_FakeErrors.Length > 0
                        ? m_FakeErrors[Random.Range(0, m_FakeErrors.Length)] : "Erreur inconnue";
                    Open(Kind.Fake, Intro ? "THUNK.exe" : RandomTitle(), fake, position, head.position, Random.Range(1f, 1.7f), 1f);
                    break;
            }
        }

        static string RandomTitle()
        {
            switch (Random.Range(0, 4))
            {
                case 0: return "THUNK.exe";
                case 1: return "ERREUR SYSTÈME";
                case 2: return "AVERTISSEMENT";
                default: return "KERNEL_PANIC";
            }
        }

        // Autour du joueur, surtout devant : jamais trop près, jamais devant le monstre.
        bool TryPlace(Transform head, out Vector3 position)
        {
            var forward = Flat(head.forward);
            var monster = m_Dimension.MonsterPoint;
            for (var attempt = 0; attempt < 6; attempt++)
            {
                var yaw = Random.value < 0.6f ? Random.Range(-55f, 55f) : Random.Range(-170f, 170f);
                var direction = Quaternion.Euler(0f, yaw, 0f) * forward;
                var distance = Random.Range(2.2f, 6f);
                position = head.position + direction * distance + Vector3.up * Random.Range(-0.5f, 1.6f);
                position.y = Mathf.Max(position.y, m_Dimension.transform.position.y + 0.6f);

                if (monster.HasValue && Vector3.Angle(position - head.position, monster.Value - head.position) < 16f)
                    continue;
                return true;
            }

            position = default;
            return false;
        }

        void Open(Kind kind, string title, string body, Vector3 position, Vector3 viewer, float life, float size)
        {
            var root = new GameObject(kind == Kind.Timer ? "Panneau : temps restant" : "Panneau d'erreur").transform;
            root.SetParent(transform, true);
            root.SetPositionAndRotation(position, Quaternion.LookRotation(position - viewer, Vector3.up));

            var frame = new GameObject("Cadre");
            frame.transform.SetParent(root, false);
            frame.AddComponent<MeshFilter>().sharedMesh = m_Frame;
            var renderer = frame.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_Material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var titleText = AddText(root, "Titre", title, new Vector2(k_Width - 0.2f, 0.085f), new Vector2(-0.04f, k_Height * 0.5f - 0.065f),
                                    TextAlignmentOptions.Left, k_Dark, 0.1f, 0.6f);
            var bodyText = AddText(root, "Message", body, new Vector2(k_Width - 0.16f, k_Height - 0.2f), new Vector2(0f, -0.05f),
                                   TextAlignmentOptions.Center, k_Red, 0.1f, kind == Kind.Fake ? 1.4f : 3f);

            var scale = Vector3.one * (Vector3.Distance(position, viewer) / k_BaseDistance * size);
            root.localScale = new Vector3(0f, 0f, 1f);
            m_Panels.Add(new Panel
            {
                Root = root, Title = titleText, Body = bodyText, Kind = kind, Life = life, Scale = scale,
            });

            if (m_OpenClip != null)
                Sfx.Play(m_OpenClip, position, 0.35f, Random.Range(0.9f, 1.15f));
        }

        TextMeshPro AddText(Transform parent, string objectName, string text, Vector2 size, Vector2 offset, TextAlignmentOptions alignment,
                            Color color, float minSize, float maxSize)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshPro>();
            if (m_Font != null)
                tmp.font = m_Font;
            tmp.text = text;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = maxSize;
            tmp.rectTransform.sizeDelta = size;
            // Un peu devant le cadre, pour être dessiné par-dessus.
            tmp.rectTransform.localPosition = new Vector3(offset.x, offset.y, -0.04f);
            return tmp;
        }

        // Le cadre d'un panneau (1,2 × 0,62 m) : fond sombre aux coins coupés, bordure rouge lumineuse, barre de titre
        // rouge, lignes de balayage, et deux petits carrés dans le coin.
        static Mesh BuildFrame()
        {
            var builder = new MeshBuilder();
            var w = k_Width * 0.5f;
            var h = k_Height * 0.5f;
            var c = k_Cut;
            var outline = new[]
            {
                new Vector3(-w + c, -h), new Vector3(w - c, -h), new Vector3(w, -h + c), new Vector3(w, h - c),
                new Vector3(w - c, h), new Vector3(-w + c, h), new Vector3(-w, h - c), new Vector3(-w, -h + c),
            };

            var back = new Color(0.03f, 0f, 0.012f, 0.9f);
            for (var i = 0; i < outline.Length; i++)
                builder.Triangle(Vector3.zero, outline[i], outline[(i + 1) % outline.Length], back);

            // Lignes de balayage.
            var scan = new Color(0.7f, 0f, 0.02f, 0.14f);
            for (var y = -h + 0.04f; y < h - 0.12f; y += 0.035f)
                builder.Quad(new Vector3(-w + 0.02f, y, -0.001f), new Vector3(-w + 0.02f, y + 0.008f, -0.001f),
                             new Vector3(w - 0.02f, y + 0.008f, -0.001f), new Vector3(w - 0.02f, y, -0.001f), scan);

            // Barre de titre.
            var bar = new Color(1.8f, 0.1f, 0.08f, 0.95f);
            builder.Quad(new Vector3(-w + 0.015f, h - 0.11f, -0.002f), new Vector3(-w + 0.015f, h - 0.02f, -0.002f),
                         new Vector3(w - c - 0.01f, h - 0.02f, -0.002f), new Vector3(w - c - 0.01f, h - 0.11f, -0.002f), bar);

            // Bordure.
            var border = new Color(2.6f, 0.18f, 0.12f, 1f);
            const float thickness = 0.012f;
            for (var i = 0; i < outline.Length; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % outline.Length];
                var inward = Vector3.Cross(b - a, Vector3.forward).normalized * -thickness;
                builder.Quad(a + new Vector3(0f, 0f, -0.003f), b + new Vector3(0f, 0f, -0.003f),
                             b + inward + new Vector3(0f, 0f, -0.003f), a + inward + new Vector3(0f, 0f, -0.003f), border);
            }

            // Deux petits carrés, en bas à droite.
            for (var i = 0; i < 2; i++)
            {
                var x = w - 0.08f - i * 0.05f;
                builder.Quad(new Vector3(x, -h + 0.03f, -0.003f), new Vector3(x, -h + 0.06f, -0.003f),
                             new Vector3(x + 0.03f, -h + 0.06f, -0.003f), new Vector3(x + 0.03f, -h + 0.03f, -0.003f), border);
            }

            return builder.ToMesh("Panneau d'erreur");
        }

        static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.forward;
        }
    }
}
