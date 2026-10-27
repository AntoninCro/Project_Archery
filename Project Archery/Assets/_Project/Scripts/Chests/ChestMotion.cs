using UnityEngine;

namespace Archery.Chests
{
    /// <summary>
    /// Le mouvement du coffre (27 octobre). Le coffre de Quaternius n'a pas d'animation : ce script refait celles de l'ancien
    /// coffre animé (GDD, section 13). Il tombe du ciel et s'écrase en apparaissant, puis sautille sur place en attendant ; à
    /// l'ouverture, il s'écrase encore une fois et ne sautille plus.
    /// </summary>
    /// <remarks>
    /// À placer sur l'objet qui porte le modèle et la charnière du couvercle (« Visual »), posé au sol : seul le dessin bouge,
    /// les colliders du coffre restent en place. Le couvercle, lui, s'ouvre par <see cref="ChestLid"/>. Suit le temps du jeu,
    /// comme l'ancienne animation : tout ralentit pendant le choix d'une orbe.
    /// </remarks>
    public class ChestMotion : MonoBehaviour
    {
        [Tooltip("Hauteur (m) d'où le coffre tombe en apparaissant. 0 : il apparaît posé.")]
        [Min(0f)]
        [SerializeField]
        float m_DropHeight = 3f;

        [Tooltip("Durée (s) de la chute.")]
        [Min(0.05f)]
        [SerializeField]
        float m_DropTime = 0.4f;

        [Tooltip("Écrasement à l'atterrissage et à l'ouverture (0,25 : environ 20 % moins haut, puis un petit rebond).")]
        [Range(0f, 0.6f)]
        [SerializeField]
        float m_Squash = 0.25f;

        [Tooltip("Durée (s) d'un écrasement.")]
        [Min(0.05f)]
        [SerializeField]
        float m_SquashTime = 0.4f;

        [Tooltip("Hauteur (m) d'un saut sur place, en attendant d'être ouvert. 0 : il ne sautille pas.")]
        [Min(0f)]
        [SerializeField]
        float m_HopHeight = 0.08f;

        [Tooltip("Durée (s) d'un saut.")]
        [Min(0.05f)]
        [SerializeField]
        float m_HopTime = 0.35f;

        [Tooltip("Temps (s) entre deux sauts.")]
        [Min(0.1f)]
        [SerializeField]
        float m_HopInterval = 1.6f;

        [Tooltip("Inclinaison (°) pendant un saut, une fois d'un côté, une fois de l'autre.")]
        [SerializeField]
        float m_HopTilt = 5f;

        ChestLid m_Lid;
        Vector3 m_BasePosition;
        float m_Time;
        bool m_Landed;
        bool m_Opened;
        float m_NextHop;
        float m_HopAge = -1f;
        int m_Hops;
        float m_SquashAge = -1f;
        float m_SquashAmount;

        void Awake()
        {
            m_BasePosition = transform.localPosition;
            m_Lid = GetComponentInChildren<ChestLid>(true);
            if (m_Lid != null)
                m_Lid.Opened += OnOpened;
        }

        void OnDestroy()
        {
            if (m_Lid != null)
                m_Lid.Opened -= OnOpened;
        }

        void OnOpened()
        {
            m_Opened = true;
            Squash(1f);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_Time += deltaTime;
            var height = 0f;
            var tilt = 0f;

            if (m_Time < m_DropTime && m_DropHeight > 0f)
            {
                // Une chute qui accélère, comme un objet lâché.
                var t = m_Time / m_DropTime;
                height = m_DropHeight * (1f - t * t);
            }
            else
            {
                if (!m_Landed)
                {
                    m_Landed = true;
                    m_NextHop = m_Time + m_SquashTime + 0.5f * m_HopInterval;
                    Squash(1f);
                }

                if (m_HopAge >= 0f)
                {
                    // Le saut en cours finit même si le coffre s'ouvre, pour ne pas retomber d'un coup.
                    m_HopAge += deltaTime;
                    var k = Mathf.Clamp01(m_HopAge / m_HopTime);
                    height = m_HopHeight * Mathf.Sin(Mathf.PI * k);
                    tilt = (m_Hops % 2 == 0 ? 1f : -1f) * m_HopTilt * Mathf.Sin(2f * Mathf.PI * k);
                    if (k >= 1f)
                    {
                        m_HopAge = -1f;
                        Squash(0.4f);
                    }
                }
                else if (!m_Opened && m_HopHeight > 0f && m_Time >= m_NextHop)
                {
                    m_HopAge = 0f;
                    m_Hops++;
                    m_NextHop = m_Time + m_HopInterval;
                }
            }

            // L'écrasement : plus bas et plus large, puis un petit étirement avant de revenir.
            var squash = 0f;
            if (m_SquashAge >= 0f)
            {
                var s = Mathf.Clamp01(m_SquashAge / m_SquashTime);
                squash = m_SquashAmount * Mathf.Sin(2f * Mathf.PI * s) * (1f - s);
                m_SquashAge = s >= 1f ? -1f : m_SquashAge + deltaTime;
            }

            transform.localPosition = m_BasePosition + new Vector3(0f, height, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            transform.localScale = new Vector3(1f + 0.5f * squash, 1f - squash, 1f + 0.5f * squash);
        }

        void Squash(float strength)
        {
            m_SquashAge = 0f;
            m_SquashAmount = m_Squash * strength;
        }
    }
}
