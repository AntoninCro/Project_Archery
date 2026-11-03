using Archery.Core;
using UnityEngine;

namespace Archery.FinalBoss
{
    // Plaqué au sol (2 novembre) : le marteau du Colosse de forge, l'ultime de la forge et de la poudre, qui suit le joueur dans
    // l'arène (Building/UltimeColosse). En vol ou au sol, MK tombe à terre, y reste étendu, puis se relève et reprend le combat
    // (en vol, s'il est dans sa phase en vol). Docs/Refonte_TowerDefense.md, 12.46.
    public partial class DragonBoss
    {
        // Temps (s) de sa chute jusqu'au sol, et après combien de temps à terre il s'étend (l'animation « Sleep »).
        const float k_PinFall = 0.35f;
        const float k_PinLieDown = 0.45f;

        float m_PinTime;
        Vector3 m_PinFrom;
        Vector3 m_PinTo;

        /// <summary>Plaqué au sol en ce moment (le marteau du Colosse de forge).</summary>
        public bool IsPinned => m_State == State.Pinned;

        /// <summary>
        /// Plaque le dragon au sol pendant <paramref name="duration"/> s (en vol, il tombe). Faux s'il ne peut pas l'être en ce
        /// moment : endormi, pendant l'intro, en plein rugissement (il change de phase), mort, ou déjà plaqué.
        /// </summary>
        public bool Pin(float duration)
        {
            if (duration <= 0f || IsHidden || InIntro || m_State == State.Waking || m_State == State.Roar || m_State == State.Dead ||
                m_State == State.Pinned || m_Phase == DragonPhase.Asleep || m_Phase == DragonPhase.Dead)
                return false;

            StopAttacks();

            // Sous lui, sur le sol de l'arène, hors des piliers (comme KeepInsideArena).
            var ground = m_Arena != null ? m_Arena.Confine(transform.position, m_BodyRadius * 0.5f) : transform.position;
            if (m_Arena != null)
            {
                foreach (var pillar in m_Arena.Pillars)
                {
                    var away = ground - pillar.Position;
                    away.y = 0f;
                    var minimum = pillar.Radius + m_BodyRadius * 0.6f;
                    if (away.magnitude < minimum && away.sqrMagnitude > 1e-4f)
                        ground = pillar.Position + away.normalized * minimum;
                }
            }

            ground.y = GroundHeight(ground);
            m_PinFrom = transform.position;
            m_PinTo = ground;
            m_PinTime = duration;
            m_LandedRotation = Quaternion.LookRotation(Flat(transform.forward));
            EnterState(State.Pinned, "Get Hit", 0.1f, restart: true);
            ArenaPlayer.Rumble(0.6f, 0.4f);
            return true;
        }

        void UpdatePinned(float dt)
        {
            // Il tombe, se remet à plat, puis s'étend à terre.
            var fall = Mathf.Clamp01(m_StateTime / k_PinFall);
            transform.position = Vector3.Lerp(m_PinFrom, m_PinTo, fall * fall);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, m_LandedRotation, 360f * dt);
            if (fall >= 1f && m_Step == 0)
            {
                m_Step = 1;
                Sfx.Play(m_StepClip, transform.position, 1f, 0.35f);
                ArenaPlayer.Rumble(0.8f, 0.4f);
            }

            if (m_Step == 1 && m_StateTime >= k_PinLieDown)
            {
                m_Step = 2;
                Play("Sleep", 0.35f, restart: true);
            }

            if (m_StateTime < m_PinTime)
                return;

            // Il se relève : dans sa phase en vol, il repart dans les airs ; sinon il reprend le combat au sol.
            if (m_Phase == DragonPhase.Air)
                BeginLift();
            else
                EndGroundAttack();
        }
    }
}
