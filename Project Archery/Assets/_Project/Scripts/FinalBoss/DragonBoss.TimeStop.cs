using UnityEngine;

namespace Archery.FinalBoss
{
    // Le temps arrêté (2 novembre, tard) : Minuit, le grand moment de la Grande Horloge, l'ultime de la glace et du temps
    // (Building/UltimeHorloge), joue aussi dans l'arène : le choix du joueur. MK s'arrête là où il est, en vol ou au sol, son
    // animation figée, ses boules de feu en vol aussi (DragonFireball.TimeStopped) ; ce qu'il préparait (le souffle, la boule de feu
    // dans la gueule) s'éteint. Puis il reprend exactement où il en était. Docs/Refonte_TowerDefense.md, 12.52.
    public partial class DragonBoss
    {
        float m_TimeStopped;

        /// <summary>Le temps est arrêté pour lui en ce moment (Minuit).</summary>
        public bool IsTimeStopped => m_TimeStopped > 0f;

        /// <summary>
        /// Arrête le temps pour le dragon pendant <paramref name="duration"/> s (et ses boules de feu en vol). Faux s'il est caché ou
        /// mort.
        /// </summary>
        public bool StopTime(float duration)
        {
            if (duration <= 0f || IsHidden || m_State == State.Dead || m_Phase == DragonPhase.Dead)
                return false;

            if (m_TimeStopped <= 0f)
                StopAttacks();
            m_TimeStopped = Mathf.Max(m_TimeStopped, duration);
            DragonFireball.TimeStopped = true;
            return true;
        }

        /// <summary>Le temps reprend son cours tout de suite (la fin de Minuit).</summary>
        public void ResumeTime()
        {
            m_TimeStopped = 0f;
            DragonFireball.TimeStopped = false;
        }

        // Vrai tant que le temps est arrêté : le reste de Tick attend, l'animation est figée.
        bool UpdateTimeStop(float deltaTime)
        {
            if (m_TimeStopped <= 0f)
                return false;

            m_TimeStopped -= deltaTime;
            if (m_Animator != null)
                m_Animator.speed = 0f;
            m_Velocity = Vector3.zero;
            m_LastPosition = transform.position;
            if (m_TimeStopped <= 0f)
                DragonFireball.TimeStopped = false;
            return true;
        }
    }
}
