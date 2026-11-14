using System.Collections.Generic;
using Archery.Enemies;
using Archery.UI;
using TMPro;
using UnityEngine;

namespace Archery.Chests
{
    /// <summary>
    /// Le sceau d'un coffre gardé (12 novembre, le joueur ; <c>Docs/Refonte_TowerDefense.md</c>, 12.74) : le coffre ne s'ouvre qu'une
    /// fois ses gardiens tués (par tes flèches ou tes bâtiments). Le joueur : depuis que la course est rapide, « c'est trop simple de
    /// juste courir et prendre tous les coffres sans prendre de dégâts » ; et le sceau remet l'arc au cœur du jeu : « sans bon arc, on
    /// ne peut avoir de bonne tourelle, puisqu'on ne peut ouvrir de coffre ». Il remplace le coffre libre du 4 novembre (12.61).
    /// </summary>
    /// <remarks>
    /// Ajouté par <see cref="Chest.SealWith"/>. Tant qu'il tient : le rayon du coffre est rouge, une étiquette au-dessus dit combien de
    /// gardiens restent, et le couvercle refuse de s'ouvrir (<see cref="ChestLid"/>). Un gardien passé de ton côté (le Colisée) ne
    /// garde plus rien.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ChestSeal : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        static readonly Color k_SealColor = new Color(1f, 0.3f, 0.22f, 1f);
        static readonly Color k_FreeColor = new Color(1f, 0.84f, 0.3f, 1f);

        // Le rayon scellé (12 novembre, le joueur : « le rayon du coffre doit être plus visible, puisqu'il est devenu rouge ») : le rayon
        // s'ajoute à la lumière derrière lui (shader Archery/LightBeam), et un rouge à la même intensité que le blanc d'origine (1,5) se
        // perdait sur le ciel. Scellé, il est d'un rouge vif plus intense, plus plein (ses bords moins effacés), plus large, et il
        // respire lentement.
        static readonly Color k_BeamColor = new Color(1f, 0.16f, 0.08f, 1f);
        const float k_BeamIntensity = 4.5f;
        const float k_BeamEdgeSoftness = 1.1f;
        const float k_BeamWidth = 1.7f;
        const float k_BeamBreath = 0.15f;
        const float k_BeamBreathSpeed = 3f;

        // L'étiquette : sa hauteur au-dessus du coffre (m), sa taille ; le rappel, après un essai d'ouverture, reste … s.
        const float k_LabelHeight = 2.4f;
        const float k_LabelSize = 7f;
        const float k_ReminderTime = 2.5f;

        readonly List<Enemy> m_Guards = new List<Enemy>();
        Renderer[] m_BeamRenderers = System.Array.Empty<Renderer>();
        Transform m_Beam;
        Vector3 m_BeamScale;
        MaterialPropertyBlock m_Block;
        TextMeshPro m_Label;
        int m_Left = -1;
        float m_RefusedAt = float.NegativeInfinity;
        bool m_Reminding;

        /// <summary>Il reste au moins un gardien en vie : le coffre ne s'ouvre pas.</summary>
        public bool IsSealed => m_Left > 0;

        /// <summary>Les gardiens encore en vie.</summary>
        public int GuardsLeft => Mathf.Max(0, m_Left);

        public void Setup(IEnumerable<Enemy> guards, GameObject beam)
        {
            m_Guards.Clear();
            foreach (var guard in guards)
            {
                if (guard != null)
                    m_Guards.Add(guard);
            }

            m_BeamRenderers = beam != null ? beam.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>();
            m_Beam = beam != null ? beam.transform : null;
            if (m_Beam != null)
                m_BeamScale = m_Beam.localScale;
            m_Block ??= new MaterialPropertyBlock();
            CreateLabel();
            Refresh();
        }

        /// <summary>
        /// Le joueur a voulu l'ouvrir encore scellé : l'étiquette s'agrandit un instant et rappelle ce qu'il faut faire.
        /// </summary>
        public void Refuse()
        {
            m_RefusedAt = Time.unscaledTime;
            m_Reminding = true;
            if (m_Label != null)
                m_Label.text = LabelText();
        }

        void Update()
        {
            Refresh();

            // Le rayon scellé respire : plus large, puis un peu moins.
            if (m_Beam != null && IsSealed)
            {
                var width = k_BeamWidth * (1f + k_BeamBreath * Mathf.Sin(Time.time * k_BeamBreathSpeed));
                m_Beam.localScale = new Vector3(m_BeamScale.x * width, m_BeamScale.y, m_BeamScale.z * width);
            }

            if (m_Label == null)
                return;

            // Elle suit le coffre et regarde le joueur, et grossit un instant quand il a voulu ouvrir le coffre.
            var label = m_Label.transform;
            label.position = transform.position + Vector3.up * k_LabelHeight;
            var camera = Camera.main;
            if (camera != null)
            {
                var away = label.position - camera.transform.position;
                if (away.sqrMagnitude > 0.01f)
                    label.rotation = Quaternion.LookRotation(away, Vector3.up);
            }

            var since = Time.unscaledTime - m_RefusedAt;
            var pop = since < 0.6f ? 1f + 0.35f * Mathf.Sin(Mathf.Clamp01(since / 0.6f) * Mathf.PI) : 1f;
            label.localScale = Vector3.one * pop;
            if (m_Reminding && since >= k_ReminderTime)
            {
                m_Reminding = false;
                m_Label.text = LabelText();
            }
        }

        void OnDestroy()
        {
            if (m_Label != null)
                Destroy(m_Label.gameObject);
        }

        // Compte les gardiens qui restent ; au dernier tombé, le sceau se brise.
        void Refresh()
        {
            var left = 0;
            foreach (var guard in m_Guards)
            {
                if (guard != null && guard.IsAlive && !guard.IsAlly)
                    left++;
            }

            if (left == m_Left)
                return;

            var wasSealed = m_Left > 0;
            m_Left = left;
            if (left > 0)
            {
                Tint(k_BeamColor);
                if (m_Label != null)
                    m_Label.text = LabelText();
                return;
            }

            Tint(null);
            if (m_Label != null)
                Destroy(m_Label.gameObject);
            m_Label = null;
            if (wasSealed)
                FloatingText.Spawn(transform.position + Vector3.up * k_LabelHeight, "Sceau brisé !", k_FreeColor, 2.4f, 2f);
        }

        string LabelText()
        {
            var guards = m_Left == 1 ? "1 gardien" : $"{GuardsLeft} gardiens";
            return m_Reminding
                ? $"Scellé\n<size=60%>Tue ses gardiens pour l'ouvrir : encore {guards}</size>"
                : $"Scellé\n<size=60%>{guards}</size>";
        }

        // Le rayon du coffre : rouge vif, intense et large tant qu'il est scellé ; sa couleur et sa taille d'origine ensuite.
        void Tint(Color? color)
        {
            if (color == null && m_Beam != null)
                m_Beam.localScale = m_BeamScale;

            foreach (var renderer in m_BeamRenderers)
            {
                if (renderer == null)
                    continue;
                if (color == null)
                {
                    renderer.SetPropertyBlock(null);
                    continue;
                }

                renderer.GetPropertyBlock(m_Block);
                m_Block.SetColor(k_BaseColorId, color.Value);
                m_Block.SetColor(k_ColorId, color.Value);
                m_Block.SetFloat(k_IntensityId, k_BeamIntensity);
                m_Block.SetFloat(k_EdgeSoftnessId, k_BeamEdgeSoftness);
                renderer.SetPropertyBlock(m_Block);
            }
        }

        void CreateLabel()
        {
            if (m_Label != null)
                return;

            // À part du coffre (pas son enfant) : sa taille ne suit pas celle du coffre ; il la détruit avec lui (OnDestroy).
            var go = new GameObject("Sceau du coffre");
            go.transform.position = transform.position + Vector3.up * k_LabelHeight;
            var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontStyle = FontStyles.Bold;
            var theme = UiTheme.Load();
            if (theme != null)
                UiThemer.ApplyLabel(text, theme);
            text.fontSize = k_LabelSize;
            var top = Color.Lerp(k_SealColor, Color.white, 0.5f);
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(top, top, k_SealColor, k_SealColor);
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(30, 6, 4, 230);
            m_Label = text;
        }
    }
}
