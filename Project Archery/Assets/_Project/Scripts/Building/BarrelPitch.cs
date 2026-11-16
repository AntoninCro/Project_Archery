using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le tube d'un mortier fait main qui se lève selon la distance de sa cible (16 novembre, la refonte visuelle ; le joueur :
    /// « la base et les supports tournent dans la direction de l'ennemi, et le canon, détaché du reste, est penché vers 90°,
    /// penché vers l'ennemi, et s'ajuste en fonction de la distance, plus comme un vrai mortier qu'un canon »). Posé par
    /// <c>Editor/ModelesFaitMain</c> sur la pièce du tube (son origine est l'axe), d'après <c>Art/Modeles/Mouvements/&lt;Nom&gt;.json</c>
    /// (« vise »). Le tournant vers l'ennemi, c'est l'affût (<see cref="StructureMortar"/>) ; ici, seulement l'élévation : presque à
    /// la verticale pour un ennemi proche, plus couché pour un ennemi loin ; au repos sans cible. Les bouches du tube (d'où partent
    /// les obus) le suivent.
    /// </summary>
    public class BarrelPitch : MonoBehaviour
    {
        [SerializeField, Tooltip("L'élévation (° au-dessus de l'horizontale) du tube tel qu'il est dans le modèle.")]
        float m_RestElevation = 70f;

        [SerializeField, Tooltip("L'élévation pour un ennemi à la portée la plus courte.")]
        float m_CloseElevation = 82f;

        [SerializeField, Tooltip("L'élévation pour un ennemi à la portée la plus longue.")]
        float m_FarElevation = 55f;

        [SerializeField, Tooltip("Vitesse du tube (°/s).")]
        float m_Speed = 60f;

        [SerializeField, Tooltip("Les bouches de ce tube : elles suivent son bout.")]
        Transform[] m_Followers = new Transform[0];

        StructureMortar m_Mortar;
        Quaternion m_Rest;
        Vector3[] m_Offsets;
        float m_Elevation;

        public void Configure(float rest, float close, float far, Transform[] followers)
        {
            m_RestElevation = rest;
            m_CloseElevation = close;
            m_FarElevation = far;
            m_Followers = followers ?? new Transform[0];
        }

        void Awake()
        {
            m_Mortar = GetComponentInParent<StructureMortar>(true);
            m_Rest = transform.localRotation;
            m_Elevation = m_RestElevation;
            m_Offsets = new Vector3[m_Followers.Length];
            for (var i = 0; i < m_Followers.Length; i++)
            {
                if (m_Followers[i] != null)
                    m_Offsets[i] = transform.InverseTransformPoint(m_Followers[i].position);
            }
        }

        void LateUpdate()
        {
            var t = m_Mortar != null ? m_Mortar.AimDistance01 : -1f;
            var want = t < 0f ? m_RestElevation : Mathf.Lerp(m_CloseElevation, m_FarElevation, t);
            m_Elevation = Mathf.MoveTowards(m_Elevation, want, m_Speed * Time.deltaTime);

            // le tube se lève autour de son axe (sa droite, au repos) : l'avant du modèle monte
            var axis = m_Rest * Vector3.right;
            transform.localRotation = Quaternion.AngleAxis(-(m_Elevation - m_RestElevation), axis) * m_Rest;

            for (var i = 0; i < m_Followers.Length; i++)
            {
                if (m_Followers[i] != null)
                    m_Followers[i].position = transform.TransformPoint(m_Offsets[i]);
            }
        }
    }
}
