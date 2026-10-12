using Archery.Bows;
using Archery.Core;
using Archery.Defense;
using UnityEngine;

namespace Archery.Waves
{
    /// <summary>
    /// Gong à viser pour lancer la vague suivante (GDD, sections 3 et 3 bis). Il oscille quand on le touche.
    /// Pendant le chrono d'une vague, deux coups de suite font venir le boss plus tôt, contre une récompense plus grosse ;
    /// il disparaît pendant le combat contre le boss. Seule la flèche tirée par l'arc compte : les flèches en plus du multitir,
    /// de l'écho et du déluge le font sonner sans rien déclencher. Les flèches disparaissent en le touchant.
    /// Quand la tour est détruite, il descend à son pied (Ground Spot), et remonte à la reconstruction.
    /// À placer sur l'objet du gong, avec un collider.
    /// </summary>
    [DisallowMultipleComponent]
    public class WaveStartGong : MonoBehaviour, IArrowHitHandler, IArrowAbsorber
    {
        [SerializeField]
        AudioClip m_GongClip;

        [Tooltip("Cache le gong (visuel et collider) quand il ne sert à rien : pendant le combat contre le boss.")]
        [SerializeField]
        bool m_HideDuringWaves = true;

        [Tooltip("Partie qui oscille quand on touche le gong. Vide : cet objet.")]
        [SerializeField]
        Transform m_SwingPart;

        [SerializeField]
        float m_SwingAngle = 18f;

        [Tooltip("Optionnel : place du gong au pied de la tour quand elle est détruite. Il remonte quand elle est reconstruite.")]
        [SerializeField]
        Transform m_GroundSpot;

        Renderer[] m_Renderers;
        Collider[] m_Colliders;
        Quaternion m_SwingRest;
        float m_Swing;
        float m_SwingTime;
        bool m_Visible = true;
        Vector3 m_TopPosition;
        Quaternion m_TopRotation;
        Tower m_Tower;

        void Awake()
        {
            if (m_SwingPart == null)
                m_SwingPart = transform;
            m_SwingRest = m_SwingPart.localRotation;
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            m_Colliders = GetComponentsInChildren<Collider>(true);
            m_TopPosition = transform.position;
            m_TopRotation = transform.rotation;
        }

        void Start()
        {
            m_Tower = Tower.Instance;
            if (m_Tower == null || m_GroundSpot == null)
                return;

            m_Tower.Destroyed += MoveToGround;
            m_Tower.Rebuilt += MoveToTop;
            if (!m_Tower.IsStanding)
                MoveToGround();
        }

        void OnDestroy()
        {
            if (m_Tower == null)
                return;

            m_Tower.Destroyed -= MoveToGround;
            m_Tower.Rebuilt -= MoveToTop;
        }

        // La tour s'effondre : le gong descend au pied des ruines, pour qu'on puisse encore lancer les vagues.
        void MoveToGround() => transform.SetPositionAndRotation(m_GroundSpot.position, m_GroundSpot.rotation);

        void MoveToTop() => transform.SetPositionAndRotation(m_TopPosition, m_TopRotation);

        public bool OnArrowHit(in ArrowHit hit)
        {
            var waves = WaveManager.Instance;
            if (!hit.IsShot)
                return false;

            Sfx.Play(m_GongClip, hit.Point);
            m_Swing = 1f;
            m_SwingTime = 0f;

            // Les flèches en plus d'une même volée ne doivent pas valoir plusieurs coups de gong.
            if (hit.Arrow != null && hit.Arrow.IsExtra)
                return false;

            if (waves == null)
                Debug.LogWarning("WaveStartGong : aucun Wave Manager dans la scène.", this);
            else if (waves.CanStartWave)
                waves.StartNextWave();
            else
                waves.OnGongHitDuringWave();
            return false;
        }

        void Update()
        {
            var waves = WaveManager.Instance;
            SetVisible(!m_HideDuringWaves || waves == null || waves.CanStartWave || waves.CanCallBoss);

            if (m_Swing <= 0f)
                return;

            // Oscillation amortie.
            m_SwingTime += Time.deltaTime;
            m_Swing = Mathf.MoveTowards(m_Swing, 0f, Time.deltaTime * 0.8f);
            var angle = Mathf.Sin(m_SwingTime * 9f) * m_SwingAngle * m_Swing;
            m_SwingPart.localRotation = m_SwingRest * Quaternion.Euler(angle, 0f, 0f);
        }

        void SetVisible(bool visible)
        {
            if (visible == m_Visible)
                return;

            m_Visible = visible;
            foreach (var renderer in m_Renderers)
                renderer.enabled = visible;
            foreach (var collider in m_Colliders)
                collider.enabled = visible;
        }
    }
}
