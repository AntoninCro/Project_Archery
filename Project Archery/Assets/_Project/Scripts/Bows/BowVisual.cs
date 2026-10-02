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
    /// Avec les arcs du pack Easy Weapons, ce composant remplace leur script <c>bowWeaponControllerAA</c> :
    /// sa corde est mise à jour trop tôt dans l'image et traînerait derrière l'arc en VR.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BowVisual : MonoBehaviour
    {
        [Tooltip("Extrémité de la branche du haut, où s'accroche la corde. Easy Weapons : top.end.")]
        [SerializeField]
        Transform m_StringTop;

        [Tooltip("Extrémité de la branche du bas. Easy Weapons : bottom.end.")]
        [SerializeField]
        Transform m_StringBottom;

        [Tooltip("Optionnel : repose-flèche propre à ce modèle. Vide : celui du prefab Bow est gardé.")]
        [SerializeField]
        Transform m_ArrowRest;

#if ARCHERY_ANIMATION_RIGGING
        [Tooltip("Optionnel : contrainte qui fait passer la corde du repos (source 0) à la tension maximale (source 1). Easy Weapons : StringDraw.")]
        [SerializeField]
        MultiPositionConstraint m_DrawConstraint;

        float m_AppliedDraw = -1f;
#endif

        public Transform StringTop => m_StringTop;
        public Transform StringBottom => m_StringBottom;
        public Transform ArrowRest => m_ArrowRest;

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
