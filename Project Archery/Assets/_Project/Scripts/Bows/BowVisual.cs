using UnityEngine;
#if ARCHERY_ANIMATION_RIGGING
using UnityEngine.Animations.Rigging;
#endif

namespace Archery.Bows
{
    /// <summary>
    /// Décrit le modèle 3D d'un arc pour le <see cref="Bow"/> : où s'accroche la corde,
    /// où repose la flèche, et (optionnel) la contrainte Animation Rigging qui plie les branches.
    /// À placer sur la racine du modèle, rangé sous l'objet « Model » du prefab Bow.
    /// </summary>
    /// <remarks>
    /// Avec un arc riggé qui a son propre script de corde, ce composant le remplace. C'était le cas des arcs du pack Easy
    /// Weapons (<c>bowWeaponControllerAA</c>, pack supprimé le 25 octobre) : leur corde était mise à jour trop tôt dans
    /// l'image et traînait derrière l'arc en VR.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BowVisual : MonoBehaviour
    {
        [Tooltip("Extrémité de la branche du haut, où s'accroche la corde.")]
        [SerializeField]
        Transform m_StringTop;

        [Tooltip("Extrémité de la branche du bas.")]
        [SerializeField]
        Transform m_StringBottom;

        [Tooltip("Optionnel : repose-flèche propre à ce modèle. Vide : celui du prefab Bow est gardé.")]
        [SerializeField]
        Transform m_ArrowRest;

        [Tooltip("Décalage des deux extrémités de la corde, dans le repère de l'arc (Z négatif = vers l'archer). " +
                 "Sert à faire sortir la corde de l'épaisseur des branches.")]
        [SerializeField]
        Vector3 m_StringEndOffset = new Vector3(0f, 0f, -0.025f);

        [Tooltip("Décalage (m) de la flèche sur le côté de la poignée, du côté extérieur de l'arc (à gauche pour un arc " +
                 "tenu de la main gauche). 0 : la flèche passe au milieu de la poignée.")]
        [SerializeField]
        float m_ArrowSideOffset;

        [Tooltip("Optionnel : milieu de la poignée, où se ferme la main gantée. Vide : sous le repose-flèche.")]
        [SerializeField]
        Transform m_Grip;

#if ARCHERY_ANIMATION_RIGGING
        [Tooltip("Optionnel : contrainte qui fait passer la corde du repos (source 0) à la tension maximale (source 1).")]
        [SerializeField]
        MultiPositionConstraint m_DrawConstraint;

        float m_AppliedDraw = -1f;
#endif

        public Transform StringTop => m_StringTop;
        public Transform StringBottom => m_StringBottom;
        public Transform ArrowRest => m_ArrowRest;
        public Vector3 StringEndOffset => m_StringEndOffset;
        public float ArrowSideOffset => m_ArrowSideOffset;
        public Transform Grip => m_Grip;

        /// <summary>Plie l'arc : 0 = corde au repos, 1 = tension maximale.</summary>
        public void SetDraw(float ratio)
        {
#if ARCHERY_ANIMATION_RIGGING
            if (m_DrawConstraint == null)
                return;

            ratio = Mathf.Clamp01(ratio);
            if (Mathf.Abs(ratio - m_AppliedDraw) < 0.001f)
                return;

            var sources = m_DrawConstraint.data.sourceObjects;
            if (sources.Count < 2)
                return;

            sources.SetWeight(0, 1f - ratio);
            sources.SetWeight(1, ratio);
            m_DrawConstraint.data.sourceObjects = sources;
            m_AppliedDraw = ratio;
#endif
        }
    }
}
