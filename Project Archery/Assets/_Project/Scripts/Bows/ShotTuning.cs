using UnityEngine;

namespace Archery.Bows
{
    /// <summary>
    /// Réglages globaux des qualités de tir (GDD, section 4.2) : rouge = raté, orange = bon, vert = parfait,
    /// vert pastel = très bon (le cercle a dépassé le vert et attend).
    /// La portée varie comme le carré de la vitesse : ×1,5 en vitesse donne ×2,25 en portée.
    /// </summary>
    /// <remarks>« Moyen » (Ok) n'est plus donné par l'anneau ; ses réglages restent pour plus tard.</remarks>
    [CreateAssetMenu(fileName = "ShotTuning", menuName = "Archery/Shot Tuning")]
    public class ShotTuning : ScriptableObject
    {
        [SerializeField]
        GradeModifiers m_None = new GradeModifiers("", new Color(1f, 1f, 1f, 0.6f), 0.6f, 0.75f, 0.5f);

        [SerializeField]
        GradeModifiers m_Miss = new GradeModifiers("Raté", new Color(0.92f, 0.22f, 0.16f), 0.6f, 0.75f, 0.5f);

        [SerializeField]
        GradeModifiers m_Ok = new GradeModifiers("Moyen", new Color(1f, 0.58f, 0.12f), 0.9f, 1f, 1f);

        [SerializeField]
        GradeModifiers m_Good = new GradeModifiers("Bon", new Color(1f, 0.55f, 0.1f), 1.15f, 1.25f, 1.25f);

        [SerializeField]
        GradeModifiers m_Perfect = new GradeModifiers("Parfait !", new Color(0.3f, 0.92f, 0.35f), 1.5f, 2f, 2f);

        [Tooltip("Vert pastel : le cercle a dépassé le vert et attend au bout de l'anneau. Même portée qu'un tir parfait, " +
                 "dégâts et points entre l'orange et le vert.")]
        [SerializeField]
        GradeModifiers m_Held = new GradeModifiers("Très bon", new Color(0.62f, 0.95f, 0.72f), 1.5f, 1.6f, 1.6f);

        static ShotTuning s_Fallback;

        /// <summary>Réglages par défaut, si aucun asset n'est assigné.</summary>
        public static ShotTuning Fallback => s_Fallback != null ? s_Fallback : s_Fallback = CreateInstance<ShotTuning>();

        public GradeModifiers Get(ShotGrade grade) => grade switch
        {
            ShotGrade.Miss => m_Miss,
            ShotGrade.Ok => m_Ok,
            ShotGrade.Good => m_Good,
            ShotGrade.Perfect => m_Perfect,
            ShotGrade.Held => m_Held,
            _ => m_None,
        };
    }
}
