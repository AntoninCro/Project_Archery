using UnityEngine;

namespace Archery.Cyber
{
    /// <summary>
    /// Un hologramme du toit cyberpunk (décor 3, 24 octobre) : de temps en temps, il saute, comme un affichage qui bugue
    /// (il s'étire et se décale un instant) ; le mannequin géant, lui, se tourne lentement vers le joueur.
    /// </summary>
    /// <remarks>Le vacillement et les lignes sont dans son shader (<c>Archery/Hologram</c>).</remarks>
    [DisallowMultipleComponent]
    public class CyberHologram : MonoBehaviour
    {
        [Tooltip("Se tourne vers le joueur (sur lui-même, à l'horizontale).")]
        [SerializeField]
        bool m_FacePlayer;

        [Tooltip("Vitesse (°/s) à laquelle il se tourne vers le joueur.")]
        [SerializeField]
        float m_TurnSpeed = 5f;

        [Tooltip("Temps (s) entre deux sauts, au plus court puis au plus long.")]
        [SerializeField]
        Vector2 m_GlitchInterval = new Vector2(2.5f, 7f);

        [Tooltip("De combien (m) il se décale quand il saute.")]
        [SerializeField]
        float m_Jump = 0.3f;

        Vector3 m_Scale;
        Vector3 m_Position;
        float m_GlitchTimer;
        float m_GlitchLeft;

        void Awake()
        {
            m_Scale = transform.localScale;
            m_Position = transform.localPosition;
            m_GlitchTimer = Random.Range(m_GlitchInterval.x, m_GlitchInterval.y);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            var dimension = CyberDimension.Instance;
            var head = dimension != null ? dimension.Head : null;
            if (m_FacePlayer && head != null)
            {
                var flat = head.position - transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), m_TurnSpeed * deltaTime);
            }

            m_GlitchTimer -= deltaTime;
            if (m_GlitchTimer <= 0f)
            {
                m_GlitchTimer = Random.Range(m_GlitchInterval.x, m_GlitchInterval.y);
                m_GlitchLeft = Random.Range(0.06f, 0.2f);
            }

            if (m_GlitchLeft > 0f)
            {
                m_GlitchLeft -= deltaTime;
                transform.localScale = Vector3.Scale(m_Scale, new Vector3(Random.Range(0.88f, 1.15f), Random.Range(0.95f, 1.06f), 1f));
                transform.localPosition = m_Position + Random.insideUnitSphere * m_Jump;
            }
            else
            {
                transform.localScale = m_Scale;
                transform.localPosition = m_Position;
            }
        }
    }
}
