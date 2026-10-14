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
    /// Une cible à viser après la victoire contre le dragon : « Continuer en mode infini » ou « Terminer la partie ».
    /// Une flèche (ou un coup de flèche) la déclenche, comme les panneaux de difficulté.
    /// </summary>
    public class ArenaChoiceTarget : MonoBehaviour, IArrowHitHandler
    {
        Action m_OnChosen;
        Transform m_Disc;
        Color m_Color;
        bool m_Used;
        float m_Age;
        AudioClip m_Clip;

        public static ArenaChoiceTarget Create(Vector3 position, Quaternion rotation, string label, Color color, Material material, AudioClip clip, Action onChosen)
        {
            var go = new GameObject($"Choix : {label.Replace('\n', ' ')}");
            go.transform.SetPositionAndRotation(position, rotation);
            var target = go.AddComponent<ArenaChoiceTarget>();
            target.m_OnChosen = onChosen;
            target.m_Color = color;
            target.m_Clip = clip;

            // Disque à anneaux, tourné vers le joueur (-Z local).
            var disc = new MeshBuilder();
            const int sides = 40;
            var rings = new[] { 1f, 0.78f, 0.56f, 0.34f, 0.14f };
            for (var r = 0; r < rings.Length; r++)
            {
                var outer = rings[r];
                var inner = r + 1 < rings.Length ? rings[r + 1] : 0f;
                var ringColor = r % 2 == 0 ? color : Color.Lerp(color, Color.white, 0.75f);
                for (var i = 0; i < sides; i++)
                {
                    var a0 = i * Mathf.PI * 2f / sides;
                    var a1 = (i + 1) * Mathf.PI * 2f / sides;
                    var p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f);
                    var p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                    disc.Quad(p0 * outer, p0 * inner, p1 * inner, p1 * outer, ringColor);
                }
            }

            var discObject = new GameObject("Cible");
            discObject.transform.SetParent(go.transform, false);
            discObject.transform.localScale = Vector3.one * 0.55f;
            discObject.AddComponent<MeshFilter>().sharedMesh = disc.ToMesh("Cible de choix");
            var renderer = discObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var collider = discObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(2f, 2f, 0.15f);
            target.m_Disc = discObject.transform;

            var textObject = new GameObject("Texte");
            textObject.transform.SetParent(go.transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            var text = textObject.AddComponent<TextMeshPro>();
            text.text = label;
            text.fontSize = 3.2f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.Lerp(color, Color.white, 0.4f);
            text.rectTransform.sizeDelta = new Vector2(2.6f, 1f);
            return target;
        }

        void Update()
        {
            m_Age += Time.unscaledDeltaTime;
            if (m_Disc != null && !m_Used)
                m_Disc.localScale = Vector3.one * (0.55f * (1f + 0.05f * Mathf.Sin(m_Age * 4f)));
        }

        public bool OnArrowHit(in ArrowHit hit)
        {
            if (m_Used)
                return false;

            m_Used = true;
            Sfx.Play(m_Clip, transform.position, 1f, 1f, 0f);
            m_OnChosen?.Invoke();
            return true;
        }
    }
}
