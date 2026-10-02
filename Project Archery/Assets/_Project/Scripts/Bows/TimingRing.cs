using System;
using UnityEngine;

namespace Archery.Bows
{
    /// <summary>
    /// Anneau de timing (GDD, section 4.2) : des bandes rouge, orange, vert, doré, vert, orange, rouge
    /// et un cercle d'approche qui rétrécit du bord vers le centre. La bande sous le cercle au moment
    /// du lâcher donne la qualité du tir. Le rendu est fait par le shader « Archery/TimingRing ».
    /// </summary>
    [DisallowMultipleComponent]
    public class TimingRing : MonoBehaviour
    {
        static readonly int k_GoldCenterId = Shader.PropertyToID("_GoldCenter");
        static readonly int k_GoldHalfWidthId = Shader.PropertyToID("_GoldHalfWidth");
        static readonly int k_GreenWidthId = Shader.PropertyToID("_GreenWidth");
        static readonly int k_OrangeWidthId = Shader.PropertyToID("_OrangeWidth");
        static readonly int k_ApproachId = Shader.PropertyToID("_Approach");
        static readonly int k_AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int k_FlashColorId = Shader.PropertyToID("_FlashColor");

        [SerializeField]
        Renderer m_Renderer;

        [Tooltip("Rayon (0 à 1) du milieu de la bande dorée.")]
        [Range(0.3f, 0.7f)]
        [SerializeField]
        float m_GoldCenter = 0.5f;

        [SerializeField]
        float m_FadeInSpeed = 12f;

        [SerializeField]
        float m_FadeOutSpeed = 5f;

        [Tooltip("Durée (s) pendant laquelle le résultat reste affiché après le tir.")]
        [SerializeField]
        float m_ResultDisplayTime = 0.35f;

        [SerializeField]
        bool m_FaceCamera = true;

        /// <summary>Le cercle d'approche vient d'entrer dans la bande dorée.</summary>
        public event Action GoldEntered;

        /// <summary>Le cercle a atteint le centre sans tir et repart du bord.</summary>
        public event Action Looped;

        float m_Duration = 1.2f;
        float m_GoldHalfWidth = 0.06f;
        float m_GreenWidth = 0.08f;
        float m_OrangeWidth = 0.1f;
        float m_Approach = 1f;
        float m_Alpha;
        float m_VisibleUntil;
        bool m_WasInGold;
        Color m_FlashColor = Color.clear;
        MaterialPropertyBlock m_PropertyBlock;
        Camera m_Camera;

        public bool IsRunning { get; private set; }

        /// <summary>Rayon actuel du cercle d'approche (1 = bord, 0 = centre).</summary>
        public float Approach => m_Approach;

        void Awake()
        {
            if (m_Renderer == null)
                m_Renderer = GetComponent<Renderer>();
            m_PropertyBlock = new MaterialPropertyBlock();
            if (m_Renderer != null)
                m_Renderer.enabled = false;
        }

        /// <summary>
        /// Démarre l'anneau. Les largeurs sont en fraction du rayon ; elles sont réduites
        /// si besoin pour que les bandes ne dépassent pas de l'anneau.
        /// </summary>
        public void Begin(float duration, float goldHalfWidth, float greenWidth, float orangeWidth)
        {
            m_Duration = Mathf.Max(0.1f, duration);
            var limit = Mathf.Min(m_GoldCenter, 1f - m_GoldCenter) - 0.02f;
            var total = goldHalfWidth + greenWidth + orangeWidth;
            var scale = total > limit && total > 0f ? limit / total : 1f;
            m_GoldHalfWidth = goldHalfWidth * scale;
            m_GreenWidth = greenWidth * scale;
            m_OrangeWidth = orangeWidth * scale;

            m_Approach = 1f;
            m_WasInGold = false;
            m_FlashColor = Color.clear;
            IsRunning = true;
        }

        public void Cancel()
        {
            IsRunning = false;
            m_VisibleUntil = 0f;
        }

        /// <summary>Arrête l'anneau et renvoie la qualité du tir.</summary>
        public ShotGrade Release()
        {
            if (!IsRunning)
                return ShotGrade.None;

            IsRunning = false;
            m_VisibleUntil = Time.time + m_ResultDisplayTime;
            return GradeAt(m_Approach);
        }

        /// <summary>Teinte l'anneau avec la couleur du résultat avant qu'il disparaisse.</summary>
        public void ShowResult(Color color)
        {
            m_FlashColor = color;
            m_Alpha = 1f;
        }

        public ShotGrade GradeAt(float radius)
        {
            var distance = Mathf.Abs(radius - m_GoldCenter);
            if (distance <= m_GoldHalfWidth)
                return ShotGrade.Perfect;
            if (distance <= m_GoldHalfWidth + m_GreenWidth)
                return ShotGrade.Good;
            if (distance <= m_GoldHalfWidth + m_GreenWidth + m_OrangeWidth)
                return ShotGrade.Ok;
            return ShotGrade.Miss;
        }

        void LateUpdate()
        {
            var deltaTime = Time.deltaTime;
            if (IsRunning)
            {
                m_Approach -= deltaTime / m_Duration;
                if (m_Approach <= 0f)
                {
                    m_Approach += 1f;
                    m_WasInGold = false;
                    Looped?.Invoke();
                }

                var inGold = Mathf.Abs(m_Approach - m_GoldCenter) <= m_GoldHalfWidth;
                if (inGold && !m_WasInGold)
                    GoldEntered?.Invoke();
                m_WasInGold = inGold;
            }

            var visible = IsRunning || Time.time < m_VisibleUntil;
            m_Alpha = Mathf.MoveTowards(m_Alpha, visible ? 1f : 0f, (visible ? m_FadeInSpeed : m_FadeOutSpeed) * deltaTime);

            if (m_Renderer == null)
                return;

            m_Renderer.enabled = m_Alpha > 0.001f;
            if (!m_Renderer.enabled)
                return;

            if (m_FaceCamera)
                FaceCamera();

            m_Renderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetFloat(k_GoldCenterId, m_GoldCenter);
            m_PropertyBlock.SetFloat(k_GoldHalfWidthId, m_GoldHalfWidth);
            m_PropertyBlock.SetFloat(k_GreenWidthId, m_GreenWidth);
            m_PropertyBlock.SetFloat(k_OrangeWidthId, m_OrangeWidth);
            m_PropertyBlock.SetFloat(k_ApproachId, m_Approach);
            m_PropertyBlock.SetFloat(k_AlphaId, m_Alpha);
            m_PropertyBlock.SetColor(k_FlashColorId, m_FlashColor);
            m_Renderer.SetPropertyBlock(m_PropertyBlock);
        }

        void FaceCamera()
        {
            if (m_Camera == null)
                m_Camera = Camera.main;
            if (m_Camera == null)
                return;

            var cameraTransform = m_Camera.transform;
            var toRing = transform.position - cameraTransform.position;
            if (toRing.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(toRing, cameraTransform.up);
        }
    }
}
