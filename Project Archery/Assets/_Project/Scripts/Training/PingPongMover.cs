using UnityEngine;

namespace Archery.Training
{
    /// <summary>
    /// Fait aller et venir un objet entre sa position de départ et un décalage (cibles mobiles). Sur un mannequin
    /// d'entraînement, c'est le <see cref="TrainingDummy"/> qui s'en sert (2 novembre au soir : des mannequins qui font des
    /// droite-gauche sur la voie du milieu) : il le désactive et va et vient lui-même, pour revenir à sa place après une poussée.
    /// </summary>
    public class PingPongMover : MonoBehaviour
    {
        [Tooltip("Déplacement maximal par rapport à la position de départ, dans le repère du monde.")]
        [SerializeField]
        Vector3 m_Offset = new Vector3(4f, 0f, 0f);

        [Tooltip("Vitesse moyenne (m/s).")]
        [SerializeField]
        float m_Speed = 1.5f;

        Vector3 m_Start;
        float m_Phase;

        /// <summary>Déplacement maximal par rapport à la position de départ, dans le repère du monde.</summary>
        public Vector3 Offset => m_Offset;

        public float Speed
        {
            get => m_Speed;
            set => m_Speed = value;
        }

        void Start() => m_Start = transform.position;

        void Update()
        {
            var length = m_Offset.magnitude;
            if (length < 1e-3f)
                return;

            m_Phase += m_Speed * Time.deltaTime / length;
            var t = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(m_Phase, 1f));
            transform.position = m_Start + m_Offset * t;
        }
    }
}
