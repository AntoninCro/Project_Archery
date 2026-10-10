using Archery.Difficulty;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Bows
{
    /// <summary>
    /// Aide à la visée pendant la tension (GDD, section 4.3), selon la difficulté :
    /// <list type="bullet">
    /// <item>Facile et Normal : la trajectoire complète de la flèche, chute comprise, si on la lâchait maintenant,
    /// jusqu'à son point d'arrivée (marqué d'un petit cercle) ;</item>
    /// <item>Difficile : une ligne droite dans l'axe de la flèche, sans la chute ;</item>
    /// <item>Impossible : rien.</item>
    /// </list>
    /// Sa couleur est celle de l'anneau de timing à cet instant. Le <see cref="Bow"/> la crée tout seul s'il n'en trouve pas.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public class AimGuide : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly RaycastHit[] s_RaycastHits = new RaycastHit[16];
        static Material s_DefaultMaterial;
        static Mesh s_MarkerMesh;

        [Header("Ligne droite (Difficile)")]
        [Tooltip("Longueur maximale de la ligne (m).")]
        [SerializeField]
        float m_MaxLength = 30f;

        [Header("Trajectoire (Facile, Normal)")]
        [Tooltip("Durée de vol (s) dessinée au plus.")]
        [SerializeField]
        float m_MaxFlightTime = 3f;

        [Tooltip("Pas de temps (s) entre deux points de la trajectoire.")]
        [SerializeField]
        float m_TimeStep = 0.03f;

        [Tooltip("Rayon (m) du cercle au point d'arrivée, à quelques mètres ; il grandit avec la distance pour rester visible.")]
        [SerializeField]
        float m_MarkerRadius = 0.15f;

        [Header("Rendu")]
        [SerializeField]
        float m_Width = 0.006f;

        [Tooltip("La ligne s'élargit avec la distance pour rester visible au loin : largeur ajoutée par mètre.")]
        [SerializeField]
        float m_WidthPerMeter = 0.0006f;

        [Tooltip("Opacité de la ligne. Sa couleur vient de l'anneau de timing.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_Opacity = 0.7f;

        [SerializeField]
        LayerMask m_HitMask = Physics.DefaultRaycastLayers;

        LineRenderer m_Line;
        Vector3[] m_Points = new Vector3[2];
        Renderer m_Marker;
        MaterialPropertyBlock m_MarkerBlock;

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

        // Un anneau plat de rayon 1, couché sur le plan XZ.
        static Mesh MarkerMesh
        {
            get
            {
                if (s_MarkerMesh != null)
                    return s_MarkerMesh;

                const int segments = 32;
                var vertices = new Vector3[segments * 2];
                var colors = new Color[segments * 2];
                var triangles = new int[segments * 6];
                for (var i = 0; i < segments; i++)
                {
                    var angle = i * Mathf.PI * 2f / segments;
                    var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    vertices[i * 2] = direction * 0.65f;
                    vertices[i * 2 + 1] = direction;
                    colors[i * 2] = colors[i * 2 + 1] = Color.white;
                    var next = (i + 1) % segments * 2;
                    var t = i * 6;
                    triangles[t] = i * 2;
                    triangles[t + 1] = next;
                    triangles[t + 2] = i * 2 + 1;
                    triangles[t + 3] = i * 2 + 1;
                    triangles[t + 4] = next;
                    triangles[t + 5] = next + 1;
                }

                s_MarkerMesh = new Mesh
                {
                    name = "Aim Guide Marker",
                    hideFlags = HideFlags.HideAndDontSave,
                    vertices = vertices,
                    colors = colors,
                    triangles = triangles,
                };
                s_MarkerMesh.RecalculateBounds();
                return s_MarkerMesh;
            }
        }

        void Awake()
        {
            m_Line = GetComponent<LineRenderer>();
            m_Line.useWorldSpace = true;
            m_Line.positionCount = 2;
            m_Line.widthMultiplier = 1f;
            m_Line.numCapVertices = 0;
            m_Line.shadowCastingMode = ShadowCastingMode.Off;
            m_Line.receiveShadows = false;
            if (m_Line.sharedMaterial == null)
                m_Line.sharedMaterial = DefaultMaterial;
            m_Line.enabled = false;

            var marker = new GameObject("Impact Marker");
            marker.transform.SetParent(transform, false);
            marker.AddComponent<MeshFilter>().sharedMesh = MarkerMesh;
            var renderer = marker.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_Line.sharedMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            m_Marker = renderer;
            m_MarkerBlock = new MaterialPropertyBlock();
        }

        /// <param name="origin">Pointe de la flèche.</param>
        /// <param name="direction">Direction de tir (normalisée).</param>
        /// <param name="speed">Vitesse (m/s) qu'aurait la flèche lâchée maintenant.</param>
        /// <param name="color">Couleur de l'anneau à cet instant (blanc avant la tension maximale).</param>
        /// <param name="strength">Opacité de 0 à 1 (augmente avec la tension).</param>
        public void Show(Vector3 origin, Vector3 direction, float speed, Color color, float strength)
        {
            var mode = DifficultyManager.Current.aimGuideMode;
            if (mode == AimGuideMode.None || strength <= 0f)
            {
                Hide();
                return;
            }

            color.a = m_Opacity * Mathf.Clamp01(strength);
            if (mode == AimGuideMode.Trajectory && speed > 0.1f)
                ShowTrajectory(origin, direction * speed, color);
            else
                ShowLine(origin, direction, color);
        }

        public void Hide()
        {
            if (m_Line != null && m_Line.enabled)
                m_Line.enabled = false;
            if (m_Marker != null && m_Marker.enabled)
                m_Marker.enabled = false;
        }

        // Ligne droite dans l'axe de la flèche, qui s'efface vers le bout ou s'arrête sur un obstacle.
        void ShowLine(Vector3 origin, Vector3 direction, Color color)
        {
            var length = TryHit(origin, direction, m_MaxLength, out var hit) ? hit.distance : m_MaxLength;
            m_Line.positionCount = 2;
            m_Line.SetPosition(0, origin);
            m_Line.SetPosition(1, origin + direction * length);
            m_Line.startWidth = m_Width;
            m_Line.endWidth = m_Width + m_WidthPerMeter * length;

            var end = color;
            end.a *= 1f - Mathf.Clamp01(length / m_MaxLength);
            m_Line.startColor = color;
            m_Line.endColor = end;
            m_Line.enabled = true;
            m_Marker.enabled = false;
        }

        // Trajectoire de la flèche (gravité, pas de frottement : comme son Rigidbody), jusqu'au premier obstacle.
        void ShowTrajectory(Vector3 origin, Vector3 velocity, Color color)
        {
            var gravity = Physics.gravity;
            var step = Mathf.Max(0.005f, m_TimeStep);
            var maxSteps = Mathf.CeilToInt(Mathf.Max(step, m_MaxFlightTime) / step);
            if (m_Points.Length < maxSteps + 1)
                m_Points = new Vector3[maxSteps + 1];

            var count = 1;
            var position = origin;
            var travelled = 0f;
            var landed = false;
            RaycastHit impact = default;
            m_Points[0] = origin;
            for (var i = 0; i < maxSteps; i++)
            {
                var next = position + velocity * step + 0.5f * step * step * gravity;
                velocity += gravity * step;
                var segment = next - position;
                var length = segment.magnitude;
                if (length > 1e-5f && TryHit(position, segment / length, length, out impact))
                {
                    m_Points[count++] = impact.point;
                    travelled += impact.distance;
                    landed = true;
                    break;
                }

                m_Points[count++] = next;
                travelled += length;
                position = next;
            }

            m_Line.positionCount = count;
            for (var i = 0; i < count; i++)
                m_Line.SetPosition(i, m_Points[i]);
            m_Line.startWidth = m_Width;
            m_Line.endWidth = m_Width + m_WidthPerMeter * travelled;

            // Elle garde sa couleur jusqu'au point d'arrivée ; sans obstacle, elle s'efface au bout.
            var end = color;
            if (!landed)
                end.a = 0f;
            m_Line.startColor = color;
            m_Line.endColor = end;
            m_Line.enabled = true;

            m_Marker.enabled = landed;
            if (!landed)
                return;

            var markerTransform = m_Marker.transform;
            markerTransform.SetPositionAndRotation(impact.point + impact.normal * 0.02f, Quaternion.FromToRotation(Vector3.up, impact.normal));
            markerTransform.localScale = Vector3.one * (m_MarkerRadius * (1f + travelled / 40f));
            m_Marker.GetPropertyBlock(m_MarkerBlock);
            m_MarkerBlock.SetColor(k_BaseColorId, new Color(color.r, color.g, color.b, Mathf.Min(1f, color.a * 1.3f)));
            m_Marker.SetPropertyBlock(m_MarkerBlock);
        }

        bool TryHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit closest)
        {
            closest = default;
            var found = false;
            var count = Physics.RaycastNonAlloc(origin, direction, s_RaycastHits, distance, m_HitMask, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = s_RaycastHits[i];
                if (hit.collider == null || ArrowIgnore.IsIgnored(hit.collider) || (found && hit.distance >= closest.distance))
                    continue;

                closest = hit;
                found = true;
            }

            return found;
        }
    }
}
