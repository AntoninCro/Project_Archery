using UnityEngine;

namespace Archery.FinalBoss
{
    // Étourdi (2 novembre, tard) : la charge des chevaux du Grand Manège, l'ultime de la fête foraine, qui suit le joueur dans
    // l'arène (Building/UltimeManege). Le choix du joueur pour MK (le Colosse le plaque au sol, DragonBoss.Pin.cs) : sonné, il
    // n'attaque plus ; en vol, il flotte sur place en tournant sur lui-même et dodeline ; au sol, il reste planté et tourne en
    // titubant. Puis il reprend le combat là où il en était (en vol, son cercle au-dessus de l'arène). Les étoiles et les petits
    // chevaux qui tournent autour de sa tête sont ceux du Manège. Docs/Refonte_TowerDefense.md, 12.51.
    public partial class DragonBoss
    {
        // Il tourne sur lui-même (°/s au début, de moins en moins vite) ; en vol, il dodeline de … m, à … Hz.
        const float k_DazeSpin = 150f;
        const float k_DazeBob = 0.6f;
        const float k_DazeBobRate = 0.9f;

        float m_DazeTime;
        bool m_DazeInAir;
        Vector3 m_DazeAt;

        /// <summary>Étourdi en ce moment (la charge du Grand Manège).</summary>
        public bool IsDazed => m_State == State.Dazed;

        /// <summary>Le milieu de sa tête (le Grand Manège y fait tourner ses étoiles).</summary>
        public Vector3 HeadPoint => m_Head != null ? m_Head.position : BodyCenter + Vector3.up * 2f;

        /// <summary>
        /// Étourdit le dragon pendant <paramref name="duration"/> s. Seulement en plein vol en cercle, ou au sol pendant le combat
        /// (pas pendant une ruée, un perchoir, un salto, un envol ou un atterrissage, l'intro, un rugissement) : faux sinon.
        /// </summary>
        public bool Daze(float duration)
        {
            if (duration <= 0f || IsHidden || InIntro || m_Phase == DragonPhase.Asleep || m_Phase == DragonPhase.Dead)
                return false;

            var air = m_State == State.Flying;
            var ground = m_State == State.Moving || m_State == State.Idle || m_State == State.Sweep || m_State == State.Bite ||
                         m_State == State.Slam || m_State == State.Spit || m_State == State.BreathCharge || m_State == State.Breath;
            if (!air && !ground)
                return false;

            StopAttacks();
            m_DazeInAir = air;
            m_DazeAt = transform.position;
            m_DazeTime = duration;
            EnterState(State.Dazed, "Get Hit", 0.1f, restart: true);
            ArenaPlayer.Rumble(0.5f, 0.3f);
            return true;
        }

        void UpdateDazed(float dt)
        {
            // Le coup reçu, puis il flotte (en vol) ou reste debout (au sol), sonné.
            if (m_Step == 0 && m_StateTime >= 0.45f)
            {
                m_Step = 1;
                Play(m_DazeInAir ? "Fly Float" : "Idle01", 0.3f);
            }

            var left = Mathf.Clamp01(1f - m_StateTime / Mathf.Max(0.1f, m_DazeTime));
            transform.Rotate(Vector3.up, k_DazeSpin * (0.3f + 0.7f * left) * dt, Space.World);
            if (m_DazeInAir)
                transform.position = m_DazeAt + Vector3.up * (Mathf.Sin(m_StateTime * 2f * Mathf.PI * k_DazeBobRate) * k_DazeBob);

            if (m_StateTime < m_DazeTime)
                return;

            // Il se reprend : en vol, son cercle au-dessus de l'arène ; au sol, le combat.
            if (m_DazeInAir)
                ResumeOrbit();
            else
                EndGroundAttack();
        }
    }
}
