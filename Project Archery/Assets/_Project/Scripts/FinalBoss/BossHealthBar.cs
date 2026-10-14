using System;
using Archery.Bows;
using Archery.Core;
using Archery.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.FinalBoss
{
    /// <summary>
    /// La grande barre de PV du dragon, avec son nom, en haut du champ de vision (GDD, section 23). Pendant l'intro,
    /// elle apparaît et se remplit au rythme de la musique ; pendant le combat, elle suit les PV, avec une traînée
    /// claire qui rattrape la barre après chaque coup et deux repères aux changements de phase.
    /// </summary>
    /// <remarks>Rien à monter : elle se crée toute seule (<see cref="Create"/>), comme le bandeau des messages.</remarks>
    public class BossHealthBar : MonoBehaviour
    {
        const float k_Distance = 2.4f;         // m devant les yeux
        const float k_Height = 0.42f;          // m au-dessus du regard
        const float k_PixelSize = 0.001f;
        const float k_Width = 1300f;
        const float k_BarHeight = 46f;
        const float k_FollowAngle = 22f;
        const float k_SettleAngle = 4f;
        const float k_Smoothing = 0.25f;
        const float k_MaxPitch = 25f;

        RectTransform m_Fill;
        RectTransform m_Trail;
        TMP_Text m_Name;
        CanvasGroup m_Group;
        float m_Value;
        float m_TrailValue;
        float m_Alpha;
        float m_TargetAlpha;
        bool m_Recentering;
        bool m_Placed;

        public static BossHealthBar Create(string bossName, float[] markers)
        {
            var go = new GameObject("[Barre de PV du boss]", typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 90;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(k_Width + 80f, 220f);
            rect.localScale = Vector3.one * k_PixelSize;

            var bar = go.AddComponent<BossHealthBar>();
            bar.m_Group = go.AddComponent<CanvasGroup>();
            bar.m_Group.alpha = 0f;

            // Nom au-dessus de la barre.
            var nameObject = new GameObject("Nom", typeof(RectTransform));
            var nameRect = (RectTransform)nameObject.transform;
            nameRect.SetParent(rect, false);
            nameRect.anchoredPosition = new Vector2(0f, 62f);
            nameRect.sizeDelta = new Vector2(k_Width, 80f);
            var text = nameObject.AddComponent<TextMeshProUGUI>();
            text.text = bossName;
            text.fontSize = 56f;
            text.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
            text.alignment = TextAlignmentOptions.Bottom;
            text.color = new Color(1f, 0.82f, 0.7f);
            text.characterSpacing = 6f;
            text.raycastTarget = false;
            bar.m_Name = text;

            // Cadre, fond, traînée, remplissage.
            CreateBox(rect, "Cadre", new Vector2(k_Width + 16f, k_BarHeight + 16f), new Color(0.05f, 0.02f, 0.02f, 0.85f));
            CreateBox(rect, "Fond", new Vector2(k_Width, k_BarHeight), new Color(0.2f, 0.04f, 0.03f, 0.9f));
            bar.m_Trail = CreateBox(rect, "Traînée", new Vector2(k_Width, k_BarHeight), new Color(1f, 0.8f, 0.45f, 0.9f), leftAligned: true);
            bar.m_Fill = CreateBox(rect, "PV", new Vector2(k_Width, k_BarHeight), new Color(0.85f, 0.08f, 0.06f, 1f), leftAligned: true);

            // Repères des changements de phase.
            if (markers != null)
            {
                foreach (var marker in markers)
                {
                    var tick = CreateBox(rect, "Repère", new Vector2(6f, k_BarHeight + 10f), new Color(1f, 0.9f, 0.8f, 0.8f));
                    tick.anchoredPosition = new Vector2((marker - 0.5f) * k_Width, 0f);
                }
            }

            bar.SetValue(0f, instant: true);
            return bar;
        }

        static RectTransform CreateBox(RectTransform parent, string name, Vector2 size, Color color, bool leftAligned = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            if (leftAligned)
            {
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(-size.x * 0.5f, 0f);
            }

            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        /// <summary>Fait apparaître ou disparaître la barre en fondu.</summary>
        public void SetVisible(bool visible)
        {
            m_TargetAlpha = visible ? 1f : 0f;
            if (visible && !m_Placed)
                SnapToHead();
        }

        /// <summary>Remplissage, de 0 à 1. Sans <paramref name="instant"/>, la traînée claire rattrape la barre.</summary>
        public void SetValue(float value, bool instant = false)
        {
            m_Value = Mathf.Clamp01(value);
            if (instant || m_Value > m_TrailValue)
                m_TrailValue = m_Value;
            Apply();
        }

        public void SetNameColor(Color color)
        {
            if (m_Name != null)
                m_Name.color = color;
        }

        void Apply()
        {
            if (m_Fill != null)
                m_Fill.localScale = new Vector3(m_Value, 1f, 1f);
            if (m_Trail != null)
                m_Trail.localScale = new Vector3(m_TrailValue, 1f, 1f);
        }

        void LateUpdate()
        {
            var deltaTime = Time.unscaledDeltaTime;
            m_Alpha = Mathf.MoveTowards(m_Alpha, m_TargetAlpha, deltaTime * 2f);
            if (m_Group != null)
                m_Group.alpha = m_Alpha;

            if (m_TrailValue > m_Value)
            {
                m_TrailValue = Mathf.MoveTowards(m_TrailValue, m_Value, deltaTime * 0.35f);
                Apply();
            }

            if (m_Alpha > 0f)
                Follow(deltaTime);
        }

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
                transform.position = head + Vector3.Slerp(current.normalized, wanted.normalized, t) * k_Distance;
            }
            else
            {
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
        }

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
            target = head + direction * k_Distance + Vector3.up * k_Height;
            return true;
        }
    }
}
