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
    /// <para>Arc à surcharge (arc cosmique) : le vert devient doré, et quand le cercle a fini sa course, un deuxième
    /// cercle doré part du bord, plus vite. Lâché dans le doré de ce deuxième cercle, ou après, dans le vert pastel où il
    /// finit sa course : <see cref="Overcharged"/> (le laser, plus fort dans le doré) ; avant le doré, un tir « très bon »,
    /// comme si on avait lâché à la fin du premier.</para>
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
        static readonly int k_PerfectColorId = Shader.PropertyToID("_PerfectColor");
        static readonly int k_ApproachColorId = Shader.PropertyToID("_ApproachColor");
        static readonly int k_AllPerfectId = Shader.PropertyToID("_AllPerfect");

        // La vitesse (par seconde) du passage à l'anneau tout vert, et du retour aux bandes.
        const float k_AllPerfectFadeSpeed = 8f;

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

        [Header("Surcharge (arc cosmique)")]
        [Tooltip("Couleur du parfait, et du deuxième cercle, avec un arc à surcharge.")]
        [SerializeField]
        Color m_OverchargeColor = new Color(1f, 0.78f, 0.22f, 1f);

        [Tooltip("Pause (s) entre la fin du premier cercle et le départ du deuxième.")]
        [SerializeField]
        float m_OverchargeDelay = 0.2f;

        /// <summary>Le cercle d'approche vient d'entrer dans la bande verte (parfait).</summary>
        public event Action GoldEntered;

        /// <summary>Le cercle vient de quitter le vert : il finit sa course dans le vert pastel et y attend le tir.</summary>
        public event Action HoldEntered;

        /// <summary>Arc à surcharge : le deuxième cercle, doré, vient de partir du bord.</summary>
        public event Action OverchargeStarted;

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
        bool m_Overcharge;
        float m_OverchargeDuration = 1f;
        bool m_SecondPass;
        float m_SecondPassTimer = -1f;
        Color m_DefaultPerfectColor = new Color(0.3f, 0.92f, 0.35f, 1f);
        Color m_DefaultApproachColor = Color.white;
        float m_AllPerfectBlend;
        float m_HeldSince = -1f;

        public bool IsRunning { get; private set; }

        /// <summary>
        /// Depuis combien de temps (s) le cercle attend au bout de l'anneau, dans le vert clair (0 sinon) : la charge de la corde de
        /// siège pour les arbalètes des Akimbow (27 octobre). À lire avant <see cref="Release"/>.
        /// </summary>
        public float HeldDuration => IsRunning && m_HeldSince >= 0f ? Time.time - m_HeldSince : 0f;

        /// <summary>
        /// Le prochain tir sera parfait quoi qu'il arrive (27 octobre, demandé par le joueur : un parfait en réserve du duelliste
        /// ou du boomerang, le talisman d'acrobate en l'air, le bonus de coffre) : l'anneau s'affiche entièrement dans la couleur du
        /// parfait. Seulement l'affichage : les bandes, <see cref="CurrentGrade"/> et le vert pastel ne changent pas (la corde de
        /// siège et le deuxième cercle de l'arc cosmique en dépendent). L'arc le met à chaque image (<c>Bow.NextShotPerfect</c>).
        /// </summary>
        public bool AllPerfect { get; set; }

        /// <summary>
        /// La zone du parfait est dorée au lieu de verte (27 octobre, demandé par le joueur : la corde de transe, à partir de la
        /// cinquième flèche parfaite d'affilée, celle qui traverse tout). Seulement la couleur ; l'arc le met à chaque image.
        /// </summary>
        public bool GoldenPerfect { get; set; }

        /// <summary>
        /// Le dernier tir a été lâché dans le doré ou le vert pastel du deuxième cercle (arc à surcharge) : c'est le laser.
        /// <see cref="Release"/> a rendu Parfait pour le doré, Très bon pour le vert pastel (un peu moins de dégâts).
        /// </summary>
        public bool Overcharged { get; private set; }

        /// <summary>Couleur dorée de la surcharge.</summary>
        public Color OverchargeColor => m_OverchargeColor;

        /// <summary>Rayon actuel du cercle d'approche (1 = bord extérieur).</summary>
        public float Approach => m_Approach;

        /// <summary>Qualité qu'aurait un tir lâché maintenant (None si l'anneau ne tourne pas).</summary>
        public ShotGrade CurrentGrade => !IsRunning ? ShotGrade.None : m_SecondPass ? SecondPassGrade() : GradeAt(m_Approach);

        /// <summary>Arc à surcharge : lâché maintenant, le tir serait le laser (deuxième cercle, dans le doré ou le vert pastel).</summary>
        public bool WouldOvercharge
        {
            get
            {
                if (!IsRunning || !m_SecondPass)
                    return false;
                var band = GradeAt(m_Approach);
                return band == ShotGrade.Perfect || band == ShotGrade.Held;
            }
        }

        // Deuxième cercle : le doré est un tir parfait ; ailleurs (avant le doré, ou le vert pastel après), « très bon ».
        ShotGrade SecondPassGrade() => GradeAt(m_Approach) == ShotGrade.Perfect ? ShotGrade.Perfect : ShotGrade.Held;

        // Le cercle s'arrête au bord intérieur de l'anneau, sans en sortir.
        float HoldRadius => m_InnerRadius + m_ApproachThickness * 0.5f;

        void Awake()
        {
            if (m_Renderer == null)
                m_Renderer = GetComponent<Renderer>();
            m_PropertyBlock = new MaterialPropertyBlock();
            if (m_Renderer != null)
            {
                m_Renderer.enabled = false;
                var material = m_Renderer.sharedMaterial;
                if (material != null && material.HasProperty(k_PerfectColorId))
                    m_DefaultPerfectColor = material.GetColor(k_PerfectColorId);
                if (material != null && material.HasProperty(k_ApproachColorId))
                    m_DefaultApproachColor = material.GetColor(k_ApproachColorId);
            }
        }

        /// <summary>
        /// Démarre l'anneau. Les largeurs sont en fraction du rayon : demi-largeur du vert (parfait), puis largeur de
        /// l'orange (bon), au-dessus du vert. Sous le vert, jusqu'au bord intérieur, c'est le vert pastel.
        /// Les bandes trop larges sont coupées aux bords de l'anneau.
        /// </summary>
        /// <param name="overchargeDuration">Arc à surcharge : durée (s) du deuxième cercle ; 0 : pas de surcharge.</param>
        public void Begin(float duration, float goldHalfWidth, float goodWidth, float overchargeDuration = 0f)
        {
            m_Duration = Mathf.Max(0.1f, duration);
            goldHalfWidth = Mathf.Max(0f, goldHalfWidth);
            m_PerfectOuter = Mathf.Min(m_GoldCenter + goldHalfWidth, k_OuterEdge);
            m_PerfectInner = Mathf.Max(m_GoldCenter - goldHalfWidth, m_InnerRadius);
            m_GoodOuter = Mathf.Min(m_PerfectOuter + Mathf.Max(0f, goodWidth), k_OuterEdge);

            m_Approach = 1f;
            m_WasInGold = false;
            m_WasHeld = false;
            m_HeldSince = -1f;
            m_FlashColor = Color.clear;
            m_Overcharge = overchargeDuration > 0f;
            m_OverchargeDuration = Mathf.Max(0.1f, overchargeDuration);
            m_SecondPass = false;
            m_SecondPassTimer = -1f;
            Overcharged = false;
            IsRunning = true;
            m_AllPerfectBlend = AllPerfect ? 1f : 0f;
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
            if (m_SecondPass)
            {
                var band = GradeAt(m_Approach);
                Overcharged = band == ShotGrade.Perfect || band == ShotGrade.Held;
                return SecondPassGrade();
            }

            Overcharged = false;
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
                // Le cercle rétrécit à vitesse constante, puis attend au bout de l'anneau : il ne recommence plus (sauf la
                // surcharge : un deuxième cercle, plus rapide).
                m_Approach = Mathf.Max(HoldRadius, m_Approach - deltaTime / (m_SecondPass ? m_OverchargeDuration : m_Duration));

                var grade = GradeAt(m_Approach);
                var inGold = grade == ShotGrade.Perfect;
                if (inGold && !m_WasInGold)
                    GoldEntered?.Invoke();
                m_WasInGold = inGold;

                var held = grade == ShotGrade.Held;
                if (held && !m_WasHeld)
                {
                    m_HeldSince = Time.time;
                    HoldEntered?.Invoke();
                }
                else if (!held)
                {
                    m_HeldSince = -1f;
                }

                m_WasHeld = held;

                // Surcharge : le premier cercle a fini sa course ; un instant plus tard, le deuxième part du bord.
                if (m_Overcharge && !m_SecondPass && held)
                {
                    if (m_SecondPassTimer < 0f)
                        m_SecondPassTimer = m_OverchargeDelay;
                    else if ((m_SecondPassTimer -= deltaTime) <= 0f)
                        StartSecondPass();
                }

                // Tout vert : le deuxième cercle de l'arc cosmique garde ses bandes (la fenêtre du laser dépend de la vraie bande).
                // Après le lâcher, la valeur reste figée : le résultat reste vert pendant le fondu.
                m_AllPerfectBlend = Mathf.MoveTowards(m_AllPerfectBlend, AllPerfect && !m_SecondPass ? 1f : 0f,
                                                      k_AllPerfectFadeSpeed * Time.unscaledDeltaTime);
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
            m_PropertyBlock.SetFloat(k_AllPerfectId, m_AllPerfectBlend);
            m_PropertyBlock.SetColor(k_PerfectColorId, m_Overcharge || GoldenPerfect ? m_OverchargeColor : m_DefaultPerfectColor);
            m_PropertyBlock.SetColor(k_ApproachColorId, m_Overcharge && m_SecondPass ? m_OverchargeColor : m_DefaultApproachColor);
            m_Renderer.SetPropertyBlock(m_PropertyBlock);
        }

        void StartSecondPass()
        {
            m_SecondPass = true;
            m_Approach = 1f;
            m_WasInGold = false;
            m_WasHeld = false;
            OverchargeStarted?.Invoke();
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
