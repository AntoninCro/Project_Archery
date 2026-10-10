using System;
using UnityEngine;

namespace Archery.Bows
{
    /// <summary>
    /// Anneau de timing (GDD, section 4.2) : un anneau épais, vide au centre, avec des bandes de l'extérieur vers
    /// l'intérieur : rouge, orange, vert, puis vert pastel. Un cercle d'approche part du bord et rétrécit ; arrivé au
    /// bout de l'anneau, dans le vert pastel, il s'arrête et attend le tir. La bande sous le cercle au moment du lâcher
    /// donne la qualité du tir : rouge = raté, orange = bon, vert = parfait, vert pastel = très bon.
    /// Le rendu est fait par le shader « Archery/TimingRing ».
    /// </summary>
    [DisallowMultipleComponent]
    public class TimingRing : MonoBehaviour
    {
        static readonly int k_InnerRadiusId = Shader.PropertyToID("_InnerRadius");
        static readonly int k_PerfectInnerId = Shader.PropertyToID("_PerfectInner");
        static readonly int k_PerfectOuterId = Shader.PropertyToID("_PerfectOuter");
        static readonly int k_GoodOuterId = Shader.PropertyToID("_GoodOuter");
        static readonly int k_ApproachId = Shader.PropertyToID("_Approach");
        static readonly int k_ApproachThicknessId = Shader.PropertyToID("_ApproachThickness");
        static readonly int k_AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int k_FlashColorId = Shader.PropertyToID("_FlashColor");

        // Le bord extérieur de l'anneau : les bandes s'arrêtent juste avant.
        const float k_OuterEdge = 0.98f;

        [SerializeField]
        Renderer m_Renderer;

        [Tooltip("Rayon (0 à 1) du milieu de la bande verte (parfait).")]
        [Range(0.3f, 0.8f)]
        [SerializeField]
        float m_GoldCenter = 0.5f;

        [Tooltip("Rayon (0 à 1) du bord intérieur : le centre de l'anneau est vide. Le cercle d'approche s'arrête juste avant.")]
        [Range(0f, 0.6f)]
        [SerializeField]
        float m_InnerRadius = 0.3f;

        [Tooltip("Épaisseur du cercle d'approche, en fraction du rayon.")]
        [Range(0.01f, 0.2f)]
        [SerializeField]
        float m_ApproachThickness = 0.05f;

        [SerializeField]
        float m_FadeInSpeed = 12f;

        [SerializeField]
        float m_FadeOutSpeed = 5f;

        [Tooltip("Durée (s) pendant laquelle le résultat reste affiché après le tir.")]
        [SerializeField]
        float m_ResultDisplayTime = 0.35f;

        [SerializeField]
        bool m_FaceCamera = true;

        /// <summary>Le cercle d'approche vient d'entrer dans la bande verte (parfait).</summary>
        public event Action GoldEntered;

        /// <summary>Le cercle vient de quitter le vert : il finit sa course dans le vert pastel et y attend le tir.</summary>
        public event Action HoldEntered;

        float m_Duration = 1.2f;
        float m_PerfectInner = 0.44f;
        float m_PerfectOuter = 0.56f;
        float m_GoodOuter = 0.7f;
        float m_Approach = 1f;
        float m_Alpha;
        float m_VisibleUntil;
        bool m_WasInGold;
        bool m_WasHeld;
        Color m_FlashColor = Color.clear;
        MaterialPropertyBlock m_PropertyBlock;
        Camera m_Camera;

        public bool IsRunning { get; private set; }

        /// <summary>Rayon actuel du cercle d'approche (1 = bord extérieur).</summary>
        public float Approach => m_Approach;

        /// <summary>Qualité qu'aurait un tir lâché maintenant (None si l'anneau ne tourne pas).</summary>
        public ShotGrade CurrentGrade => IsRunning ? GradeAt(m_Approach) : ShotGrade.None;

        // Le cercle s'arrête au bord intérieur de l'anneau, sans en sortir.
        float HoldRadius => m_InnerRadius + m_ApproachThickness * 0.5f;

        void Awake()
        {
            if (m_Renderer == null)
                m_Renderer = GetComponent<Renderer>();
            m_PropertyBlock = new MaterialPropertyBlock();
            if (m_Renderer != null)
                m_Renderer.enabled = false;
        }

        /// <summary>
        /// Démarre l'anneau. Les largeurs sont en fraction du rayon : demi-largeur du vert (parfait), puis largeur de
        /// l'orange (bon), au-dessus du vert. Sous le vert, jusqu'au bord intérieur, c'est le vert pastel.
        /// Les bandes trop larges sont coupées aux bords de l'anneau.
        /// </summary>
        public void Begin(float duration, float goldHalfWidth, float goodWidth)
        {
            m_Duration = Mathf.Max(0.1f, duration);
            goldHalfWidth = Mathf.Max(0f, goldHalfWidth);
            m_PerfectOuter = Mathf.Min(m_GoldCenter + goldHalfWidth, k_OuterEdge);
            m_PerfectInner = Mathf.Max(m_GoldCenter - goldHalfWidth, m_InnerRadius);
            m_GoodOuter = Mathf.Min(m_PerfectOuter + Mathf.Max(0f, goodWidth), k_OuterEdge);

            m_Approach = 1f;
            m_WasInGold = false;
            m_WasHeld = false;
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
            if (radius > m_GoodOuter)
                return ShotGrade.Miss;
            if (radius > m_PerfectOuter)
                return ShotGrade.Good;
            if (radius >= m_PerfectInner)
                return ShotGrade.Perfect;
            return ShotGrade.Held;
        }

        void LateUpdate()
        {
            var deltaTime = Time.deltaTime;
            if (IsRunning)
            {
                // Le cercle rétrécit à vitesse constante, puis attend au bout de l'anneau : il ne recommence plus.
                m_Approach = Mathf.Max(HoldRadius, m_Approach - deltaTime / m_Duration);

                var grade = GradeAt(m_Approach);
                var inGold = grade == ShotGrade.Perfect;
                if (inGold && !m_WasInGold)
                    GoldEntered?.Invoke();
                m_WasInGold = inGold;

                var held = grade == ShotGrade.Held;
                if (held && !m_WasHeld)
                    HoldEntered?.Invoke();
                m_WasHeld = held;
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
            m_PropertyBlock.SetFloat(k_InnerRadiusId, m_InnerRadius);
            m_PropertyBlock.SetFloat(k_PerfectInnerId, m_PerfectInner);
            m_PropertyBlock.SetFloat(k_PerfectOuterId, m_PerfectOuter);
            m_PropertyBlock.SetFloat(k_GoodOuterId, m_GoodOuter);
            m_PropertyBlock.SetFloat(k_ApproachId, m_Approach);
            m_PropertyBlock.SetFloat(k_ApproachThicknessId, m_ApproachThickness);
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
