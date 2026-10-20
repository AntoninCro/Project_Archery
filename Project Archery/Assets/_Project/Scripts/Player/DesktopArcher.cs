using Archery.Bows;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Archery.Player
{
    /// <summary>
    /// Sans casque : tirer à la souris. L'arc est tenu en bas à droite de l'écran. Clic gauche maintenu : une flèche
    /// s'encoche, l'arc se lève et la corde se tend peu à peu, comme en VR. À pleine tension, l'anneau de timing démarre
    /// en bas à gauche du viseur, et l'aide à la visée montre la trajectoire. Relâcher le clic tire.
    /// </summary>
    /// <remarks>
    /// La flèche part vers ce que vise le centre de l'écran, puis retombe comme en VR : l'aide à la visée montre sa chute.
    /// L'arc levé est sur la droite, penché vers la droite (20 octobre) : il ne cache plus le viseur ni la cible.
    /// Lâché trop tôt, le tir est annulé et la flèche retourne dans le carquois. Pendant une construction, l'arc est
    /// rangé et le clic gauche ne fait rien. Ajouté par <see cref="DesktopControls"/>, actif seulement sans casque.
    /// </remarks>
    [AddComponentMenu("Archery/Desktop Archer")]
    [DisallowMultipleComponent]
    public class DesktopArcher : MonoBehaviour
    {
        [Tooltip("Temps (s) pour tendre la corde à fond, clic gauche maintenu (une fois l'arc levé).")]
        [SerializeField]
        float m_DrawTime = 0.75f;

        [Header("Arc levé (en tirant)")]
        [Tooltip("Où passe la flèche (repose-flèche) par rapport à la caméra : X à droite, Y en haut, Z devant. Sur la " +
                 "droite, pour que l'arc ne cache pas le viseur ; la flèche part quand même vers le centre de l'écran.")]
        [SerializeField]
        Vector3 m_AimRest = new Vector3(0.24f, -0.12f, 0.7f);

        [Tooltip("Inclinaison (°) de l'arc levé, autour de la flèche : positif, le haut de l'arc penche vers la droite " +
                 "(loin du viseur) ; négatif, vers la gauche.")]
        [SerializeField]
        float m_AimCant = 20f;

        [Header("Arc baissé")]
        [Tooltip("Où passe la flèche quand on ne tire pas, par rapport à la caméra.")]
        [SerializeField]
        Vector3 m_IdleRest = new Vector3(0.28f, -0.4f, 0.55f);

        [Tooltip("Orientation (°) de l'arc baissé par rapport à la caméra (X : pointe vers le bas, Z : penche).")]
        [SerializeField]
        Vector3 m_IdleEuler = new Vector3(30f, -15f, 40f);

        [Tooltip("Temps (s) pour lever ou baisser l'arc.")]
        [SerializeField]
        float m_RaiseTime = 0.15f;

        [Tooltip("Temps (s) pendant lequel l'arc reste levé après un tir.")]
        [SerializeField]
        float m_FollowThrough = 0.35f;

        [Header("Visée")]
        [Tooltip("Distance (m) visée quand rien n'est au centre de l'écran (le ciel).")]
        [SerializeField]
        float m_MaxAimDistance = 80f;

        [Tooltip("Distance (m) de l'anneau de timing devant les yeux. 0 : à côté de la poignée, comme en VR.")]
        [SerializeField]
        float m_RingDistance = 0.45f;

        [Tooltip("Décalage (m) de l'anneau de timing par rapport au viseur, à cette distance : X à droite, Y en haut. En bas " +
                 "à gauche, il ne cache ni le viseur ni la cible, et l'arc (à droite) ne le cache pas.")]
        [SerializeField]
        Vector2 m_RingOffset = new Vector2(-0.13f, -0.06f);

        // Plus près, la flèche partirait trop en biais vers le centre de l'écran.
        const float k_MinAimDistance = 1.5f;

        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        Bow m_Bow;
        Transform m_Camera;
        Transform m_RingAnchor;
        float m_Raise;
        float m_Draw;
        float m_LowerDelay;

        void OnDisable()
        {
            if (m_Bow != null)
                m_Bow.SetDesktopHeld(false);
            m_Bow = null;
            m_Raise = 0f;
            m_Draw = 0f;
        }

        void OnDestroy()
        {
            if (m_RingAnchor != null)
                Destroy(m_RingAnchor.gameObject);
        }

        // Après le mouvement de la caméra (DesktopControls, dans Update) : l'arc reste collé à l'écran.
        void LateUpdate()
        {
            var controls = DesktopControls.Instance;
            if (controls == null || !controls.IsActive)
                return;

            if (m_Camera == null)
                m_Camera = controls.CameraTransform;
            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            if (m_Camera == null || m_Bow == null)
                return;

            // Avec une autre arme (Akimbow, katana et bouclier), l'arc n'est pas tenu devant la caméra.
            if (m_Bow.IsReplaced)
            {
                m_Bow.SetDesktopHeld(false);
                m_Raise = 0f;
                m_Draw = 0f;
                return;
            }

            m_Bow.SetDesktopHeld(true, RingAnchor());

            // En temps réel : le ralenti d'un coffre ne ralentit pas la main.
            var deltaTime = Time.unscaledDeltaTime;
            var mouse = Mouse.current;
            var focus = controls.HasFocus && !DesktopHand.IsTypingText && mouse != null;
            var held = focus && mouse.leftButton.isPressed;

            if (focus && mouse.leftButton.wasPressedThisFrame && !controls.RefocusedThisFrame && !m_Bow.IsStowed && !m_Bow.HasArrow &&
                !Archery.Core.SlowMotion.IsPaused &&
                ArrowPool.Instance != null)
            {
                var arrow = ArrowPool.Instance.Get();
                if (!m_Bow.DesktopNock(arrow))
                    arrow.Despawn();
                m_Draw = 0f;
            }

            if (m_Bow.HasArrow)
            {
                if (held)
                {
                    // La corde ne se tend qu'une fois l'arc levé : la flèche part toujours vers le viseur.
                    if (m_Raise >= 1f)
                        m_Draw = Mathf.MoveTowards(m_Draw, 1f, deltaTime / Mathf.Max(0.05f, m_DrawTime));
                }
                else
                {
                    m_Bow.DesktopRelease();
                    m_Draw = 0f;
                    m_LowerDelay = m_FollowThrough;
                }
            }
            else
            {
                m_Draw = 0f;
            }

            m_LowerDelay -= deltaTime;
            var raised = m_Bow.HasArrow || m_LowerDelay > 0f;
            m_Raise = Mathf.MoveTowards(m_Raise, raised ? 1f : 0f, deltaTime / Mathf.Max(0.01f, m_RaiseTime));
            PlaceBow(Mathf.SmoothStep(0f, 1f, m_Raise));
        }

        // L'arc baissé en bas à droite, ou levé sur la droite : la flèche passe par « Aim Rest » et vise le centre de l'écran.
        void PlaceBow(float raise)
        {
            var rest = m_Camera.TransformPoint(Vector3.Lerp(m_IdleRest, m_AimRest, raise));
            var toTarget = AimPoint() - rest;
            var aimRotation = Quaternion.LookRotation(toTarget.sqrMagnitude > 1e-4f ? toTarget : m_Camera.forward, m_Camera.up) *
                              Quaternion.Euler(0f, 0f, m_AimCant);
            var idleRotation = m_Camera.rotation * Quaternion.Euler(m_IdleEuler);
            var rotation = Quaternion.Slerp(idleRotation, aimRotation, raise);

            // La corde se tend vite au début, puis plus lentement, comme un vrai arc.
            var draw = 1f - (1f - m_Draw) * (1f - m_Draw);
            m_Bow.DesktopUpdate(rest - rotation * m_Bow.RestOffset, rotation, draw, Time.unscaledDeltaTime);
        }

        // Ce que vise le centre de l'écran : le premier obstacle, ou un point au loin.
        Vector3 AimPoint()
        {
            var origin = m_Camera.position;
            var forward = m_Camera.forward;
            var best = m_MaxAimDistance;
            var count = Physics.RaycastNonAlloc(origin, forward, s_Hits, m_MaxAimDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if (hit.collider == null || hit.distance < k_MinAimDistance || hit.distance >= best || ArrowIgnore.IsIgnored(hit.collider) ||
                    hit.collider.GetComponentInParent<Arrow>() != null)
                    continue;
                best = hit.distance;
            }

            return origin + forward * best;
        }

        Transform RingAnchor()
        {
            if (m_RingDistance <= 0f)
                return null;

            if (m_RingAnchor == null)
            {
                m_RingAnchor = new GameObject("Desktop Timing Ring Anchor").transform;
                m_RingAnchor.SetParent(m_Camera, false);
            }

            m_RingAnchor.localPosition = new Vector3(m_RingOffset.x, m_RingOffset.y, m_RingDistance);
            return m_RingAnchor;
        }
    }
}
