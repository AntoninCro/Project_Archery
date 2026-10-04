using UnityEngine;

namespace Archery.Core
{
    /// <summary>
    /// Fait « respirer » un objet : sa taille oscille doucement (points faibles du boss, repères…).
    /// </summary>
    public class Pulse : MonoBehaviour
    {
        [Tooltip("Variation de taille (0,15 = ±15 %).")]
        [SerializeField]
        float m_Amplitude = 0.15f;

        [Tooltip("Vitesse de l'oscillation.")]
        [SerializeField]
        float m_Speed = 4f;

        Vector3 m_BaseScale;
        float m_Offset;

        void Awake()
        {
            m_BaseScale = transform.localScale;
            m_Offset = Random.value * Mathf.PI * 2f;
        }

        void Update() => transform.localScale = m_BaseScale * (1f + m_Amplitude * Mathf.Sin(Time.time * m_Speed + m_Offset));
    }
}
