using Archery.Bows;
using Archery.Core;
using UnityEngine;

namespace Archery.Waves
{
    /// <summary>
    /// Gong à viser pour lancer la vague suivante (GDD, section 3). Il oscille quand on le touche
    /// et disparaît pendant les vagues. À placer sur l'objet du gong, avec un collider.
    /// </summary>
    [DisallowMultipleComponent]
    public class WaveStartGong : MonoBehaviour, IArrowHitHandler
    {
        [SerializeField]
        AudioClip m_GongClip;

        [Tooltip("Cache le gong (visuel et collider) pendant les vagues.")]
        [SerializeField]
        bool m_HideDuringWaves = true;

        [Tooltip("Partie qui oscille quand on touche le gong. Vide : cet objet.")]
        [SerializeField]
        Transform m_SwingPart;

        [SerializeField]
        float m_SwingAngle = 18f;

        Renderer[] m_Renderers;
        Collider[] m_Colliders;
        Quaternion m_SwingRest;
        float m_Swing;
        float m_SwingTime;
        bool m_Visible = true;

        void Awake()
        {
            if (m_SwingPart == null)
                m_SwingPart = transform;
            m_SwingRest = m_SwingPart.localRotation;
            m_Renderers = GetComponentsInChildren<Renderer>(true);
            m_Colliders = GetComponentsInChildren<Collider>(true);
        }

        public bool OnArrowHit(in ArrowHit hit)
        {
            var waves = WaveManager.Instance;
            if (!hit.IsShot)
                return false;

            Sfx.Play(m_GongClip, hit.Point);
            m_Swing = 1f;
            m_SwingTime = 0f;

            if (waves == null)
                Debug.LogWarning("WaveStartGong : aucun Wave Manager dans la scène.", this);
            else
                waves.StartNextWave();
            return false;
        }

        void Update()
        {
            var waves = WaveManager.Instance;
            SetVisible(!m_HideDuringWaves || waves == null || waves.CanStartWave);

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
