using Archery.Difficulty;
using UnityEngine;

namespace Archery.Bows
{
    /// <summary>
    /// Aide à la visée : une ligne droite dans l'axe de la flèche encochée.
    /// Elle ne montre pas la chute de la flèche, seulement la direction ; elle s'arrête au premier obstacle.
    /// Le <see cref="Bow"/> la crée tout seul s'il n'en trouve pas. La difficulté peut l'interdire
    /// (Difficile et Impossible).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public class AimGuide : MonoBehaviour
    {
        static readonly RaycastHit[] s_RaycastHits = new RaycastHit[16];
        static Material s_DefaultMaterial;

        [Tooltip("Longueur maximale de la ligne (m).")]
        [SerializeField]
        float m_MaxLength = 30f;

        [SerializeField]
        float m_Width = 0.006f;

        [SerializeField]
        Color m_Color = new Color(1f, 1f, 1f, 0.6f);

        [SerializeField]
        LayerMask m_HitMask = Physics.DefaultRaycastLayers;

        LineRenderer m_Line;

        static Material DefaultMaterial
        {
            get
            {
                if (s_DefaultMaterial == null)
                {
                    var shader = Shader.Find("Archery/UnlitVertexColor");
                    if (shader == null)
                        shader = Shader.Find("Sprites/Default");
                    s_DefaultMaterial = new Material(shader) { name = "Aim Guide (runtime)" };
                }

                return s_DefaultMaterial;
            }
        }

        void Awake()
        {
            m_Line = GetComponent<LineRenderer>();
            m_Line.useWorldSpace = true;
            m_Line.positionCount = 2;
            m_Line.widthMultiplier = m_Width;
            m_Line.numCapVertices = 0;
            m_Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_Line.receiveShadows = false;
            if (m_Line.sharedMaterial == null)
                m_Line.sharedMaterial = DefaultMaterial;
            m_Line.enabled = false;
        }

        /// <param name="origin">Pointe de la flèche.</param>
        /// <param name="direction">Direction de tir (normalisée).</param>
        /// <param name="strength">Opacité de 0 à 1 (augmente avec la tension).</param>
        public void Show(Vector3 origin, Vector3 direction, float strength)
        {
            if (!DifficultyManager.Current.aimGuide || strength <= 0f)
            {
                Hide();
                return;
            }

            var length = FindObstacleDistance(origin, direction, out var distance) ? distance : m_MaxLength;
            m_Line.SetPosition(0, origin);
            m_Line.SetPosition(1, origin + direction * length);

            // La ligne s'efface vers le bout ; si elle s'arrête sur un obstacle, elle garde un peu d'opacité à l'impact.
            var start = m_Color;
            start.a *= Mathf.Clamp01(strength);
            var end = start;
            end.a *= 1f - Mathf.Clamp01(length / m_MaxLength);
            m_Line.startColor = start;
            m_Line.endColor = end;
            m_Line.enabled = true;
        }

        public void Hide()
        {
            if (m_Line != null && m_Line.enabled)
                m_Line.enabled = false;
        }

        bool FindObstacleDistance(Vector3 origin, Vector3 direction, out float distance)
        {
            distance = float.MaxValue;
            var count = Physics.RaycastNonAlloc(origin, direction, s_RaycastHits, m_MaxLength, m_HitMask, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = s_RaycastHits[i];
                if (hit.collider != null && !ArrowIgnore.IsIgnored(hit.collider) && hit.distance < distance)
                    distance = hit.distance;
            }

            return distance < float.MaxValue;
        }
    }
}
