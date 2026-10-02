using UnityEngine;

namespace Archery.Bows
{
    /// <summary>
    /// Réglages globaux des qualités de tir (valeurs de départ du GDD, section 4.2).
    /// </summary>
    [CreateAssetMenu(fileName = "ShotTuning", menuName = "Archery/Shot Tuning")]
    public class ShotTuning : ScriptableObject
    {
        [SerializeField]
        GradeModifiers m_None = new GradeModifiers("", new Color(1f, 1f, 1f, 0.6f), 0.85f, 0.75f, 0.5f);

        [SerializeField]
        GradeModifiers m_Miss = new GradeModifiers("Raté", new Color(0.92f, 0.22f, 0.16f), 0.85f, 0.75f, 0.5f);

        [SerializeField]
        GradeModifiers m_Ok = new GradeModifiers("Moyen", new Color(1f, 0.58f, 0.12f), 1f, 1f, 1f);

        [SerializeField]
        GradeModifiers m_Good = new GradeModifiers("Bon", new Color(0.35f, 0.86f, 0.32f), 1.1f, 1.25f, 1.25f);

        [SerializeField]
        GradeModifiers m_Perfect = new GradeModifiers("Parfait !", new Color(1f, 0.82f, 0.18f), 1.25f, 2f, 2f);

        static ShotTuning s_Fallback;

        /// <summary>Réglages par défaut, si aucun asset n'est assigné.</summary>
        public static ShotTuning Fallback => s_Fallback != null ? s_Fallback : s_Fallback = CreateInstance<ShotTuning>();

        public GradeModifiers Get(ShotGrade grade) => grade switch
        {
            ShotGrade.Miss => m_Miss,
            ShotGrade.Ok => m_Ok,
            ShotGrade.Good => m_Good,
            ShotGrade.Perfect => m_Perfect,
            _ => m_None,
        };
    }
}
