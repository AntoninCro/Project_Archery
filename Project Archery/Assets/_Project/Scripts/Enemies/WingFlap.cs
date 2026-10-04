using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Fait battre deux ailes : chaque aile tourne autour de l'axe Z (bleu) de son pivot, de haut en bas.
    /// Pour un volant fait de formes simples, en attendant un modèle animé.
    /// </summary>
    public class WingFlap : MonoBehaviour
    {
        [Tooltip("Pivot de l'aile gauche (à l'épaule). L'aile est son enfant.")]
        [SerializeField]
        Transform m_LeftWing;

        [Tooltip("Pivot de l'aile droite.")]
        [SerializeField]
        Transform m_RightWing;

        [Tooltip("Battements par seconde.")]
        [SerializeField]
        float m_Rate = 3f;

        [Tooltip("Amplitude (°) d'un battement.")]
        [SerializeField]
        float m_Amplitude = 40f;

        Quaternion m_LeftRest;
        Quaternion m_RightRest;
        float m_Phase;
        float m_Intensity = 1f;

        /// <summary>De 0 (ailes immobiles, repliées) à 1 (battement complet).</summary>
        public float Intensity { get; set; } = 1f;

        /// <summary>Multiplie la fréquence des battements (plus vite en sur-place).</summary>
        public float RateMultiplier { get; set; } = 1f;

        void Awake()
        {
            if (m_LeftWing != null)
                m_LeftRest = m_LeftWing.localRotation;
            if (m_RightWing != null)
                m_RightRest = m_RightWing.localRotation;
            m_Phase = Random.Range(0f, Mathf.PI * 2f);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_Intensity = Mathf.MoveTowards(m_Intensity, Intensity, deltaTime * 3f);
            m_Phase += deltaTime * m_Rate * RateMultiplier * Mathf.PI * 2f;

            var angle = Mathf.Sin(m_Phase) * m_Amplitude * m_Intensity;
            if (m_LeftWing != null)
                m_LeftWing.localRotation = m_LeftRest * Quaternion.Euler(0f, 0f, angle);
            if (m_RightWing != null)
                m_RightWing.localRotation = m_RightRest * Quaternion.Euler(0f, 0f, -angle);
        }
    }
}
