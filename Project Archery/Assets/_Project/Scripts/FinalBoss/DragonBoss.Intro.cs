using Archery.Core;
using UnityEngine;

namespace Archery.FinalBoss
{
    // Avant le combat, aux signaux de l'Arena Director : le dragon attend debout en haut de la coulée de lave pendant que
    // les cristaux s'allument, s'envole puis pique sur le centre de l'arène avant que sa barre de PV apparaisse, puis
    // crache son feu vers la voûte pendant qu'elle se remplit. Sans coulée (ancienne grotte ronde), il dort à sa place et
    // se réveille en rugissant.
    public partial class DragonBoss
    {
        const float k_IntroHeadUp = 0.4f;
        const float k_IntroApexHeight = 12f;   // hauteur (m) au-dessus du sol d'où il pique
        const float k_IntroRiseShare = 0.6f;   // part de la durée passée à monter (le reste : le piqué)

        bool m_AsleepAtStart;
        bool m_IntroBreathPending;
        float m_IntroDuration = 1f;
        Vector3 m_IntroStart;
        Vector3 m_IntroApex;
        Vector3 m_IntroEnd;
        Quaternion m_IntroYaw = Quaternion.identity;
        Quaternion m_IntroDive = Quaternion.identity;
        Quaternion m_IntroLanded = Quaternion.identity;

        bool InIntro => m_State == State.Sleeping || m_State == State.IntroDash || m_State == State.IntroWait || m_State == State.IntroBreath;

        void Start()
        {
            if (m_Arena != null && m_Arena.TryGetLavaFallTop(out var top))
            {
                transform.SetPositionAndRotation(top.position, top.rotation);
                Play("Idle01", 0f);
            }
            else
            {
                m_AsleepAtStart = true;
                Play("Sleep", 0f);
            }

            m_LastPosition = transform.position;
        }

        /// <summary>
        /// Les cristaux sont allumés : il s'envole du haut de la coulée, puis pique sur le centre de l'arène, en
        /// <paramref name="duration"/> s en tout.
        /// </summary>
        public void IntroDash(float duration)
        {
            if (m_State != State.Sleeping || m_AsleepAtStart)
                return;

            // Il monte au-dessus de l'arène, un bon tiers du chemin vers le centre, puis pique : le milieu de son corps
            // arrive au point où il sera, posé, face au joueur.
            var landing = IntroLanding();
            m_ToPosition = landing.position;
            m_IntroLanded = landing.rotation;
            m_IntroStart = BodyCenter;
            m_IntroEnd = landing.position + landing.rotation * (m_BodyCenter * transform.lossyScale.x);
            var above = Vector3.Lerp(transform.position, landing.position, 0.35f);
            var height = FloorY + k_IntroApexHeight;
            if (m_Arena != null)
                height = Mathf.Min(height, m_Arena.CeilingPoint(above, out _).y - 7f);
            m_IntroApex = new Vector3(above.x, Mathf.Max(height, m_IntroStart.y + 3f), above.z);
            m_IntroYaw = Quaternion.LookRotation(Flat(m_IntroEnd - m_IntroStart));
            m_IntroDive = Quaternion.LookRotation((m_IntroEnd - m_IntroApex).normalized);
            m_IntroDuration = Mathf.Max(0.6f, duration);
            m_IntroBreathPending = false;
            EnterState(State.IntroDash, "Fly Forward", 0.15f, restart: true);
            Sfx.Play(m_WingClip, transform.position, 1f, 0.55f);
        }

        /// <summary>La barre de PV se remplit : il crache son feu vers la voûte (dès qu'il est posé), jusqu'au début du combat.</summary>
        public void IntroBreath()
        {
            if (m_AsleepAtStart)
                return;
            if (m_State == State.IntroDash)
            {
                m_IntroBreathPending = true;
                return;
            }

            if (m_State == State.Sleeping)
                LandAtCenter(snap: true);
            if (m_State == State.IntroWait)
                EnterState(State.IntroBreath, "Idle01", 0.2f);
        }

        /// <summary>Fin de l'intro : le combat commence (en rugissant, s'il dormait).</summary>
        public void WakeUp()
        {
            if (!InIntro)
                return;

            StopBreath();

            // Ce qu'il aurait reçu pendant l'intro ne compte pas.
            Enemy.Health.ResetHealth(Enemy.Health.Max);
            SetHitboxesEnabled(true);
            SetPhase(DragonPhase.Ground);
            m_SlamTimer = 8f;
            if (m_AsleepAtStart)
            {
                EnterState(State.Waking, "Scream", 0.2f, restart: true);

                // Au réveil, il rugit en silence, sauf si « Wake Roar Sound » est coché.
                if (m_WakeRoarSound)
                    PlayRoarSound();
                return;
            }

            // Intro passée (touche N) : il est tout de suite au centre.
            if (m_State == State.Sleeping || m_State == State.IntroDash)
                LandAtCenter(snap: true);
            m_SpitTimer = 1.5f;
            m_BreathTimer = 6f;
            EnterState(State.Moving, "Idle01", 0.3f);
        }

        void UpdateWaking()
        {
            if (m_StateTime < m_WakeRoarTime)
                return;

            m_SpitTimer = 1f;
            m_BreathTimer = 5f;
            EnterState(State.Moving);
        }

        // L'envol, puis le piqué. Il tourne sur le milieu de son corps, comme pour le salto.
        void UpdateIntroDash(float dt)
        {
            var rise = m_IntroDuration * k_IntroRiseShare;
            if (m_StateTime < rise)
            {
                // Il monte en battant des ailes, le nez levé, puis se met à plat en haut.
                var t = Smooth(m_StateTime / rise);
                SetBodyPose(Vector3.Lerp(m_IntroStart, m_IntroApex, t), m_IntroYaw * Quaternion.Euler(-25f * Mathf.Sin(t * Mathf.PI), 0f, 0f));
                return;
            }

            if (m_Step == 0)
            {
                m_Step = 1;
                Play("Fly Glide", 0.12f);
            }

            // Le piqué : lent au départ, de plus en plus rapide, le nez vers le point d'arrivée ; il se redresse juste
            // avant de toucher le sol.
            var u = Mathf.Clamp01((m_StateTime - rise) / Mathf.Max(0.1f, m_IntroDuration - rise));
            var rotation = u < 0.7f
                ? Quaternion.Slerp(m_IntroYaw, m_IntroDive, Smooth(u / 0.25f))
                : Quaternion.Slerp(m_IntroDive, m_IntroLanded, Smooth((u - 0.7f) / 0.3f));
            SetBodyPose(Vector3.Lerp(m_IntroApex, m_IntroEnd, u * u), rotation);
            if (u >= 1f)
                LandAtCenter(snap: false);
        }

        void UpdateIntroWait(float dt) => TurnTowards(TowardsPlayer, m_TurnSpeed, dt);

        // Tête levée vers la voûte, gueule grande ouverte : un long jet de feu qui monte, sans danger.
        void UpdateIntroBreath(float dt)
        {
            TurnTowards(TowardsPlayer, m_TurnSpeed * 0.5f, dt);
            var up = Smooth(m_StateTime / k_IntroHeadUp);
            m_NeckTarget = -75f * up;
            m_JawTarget = up;
            m_BreathAim = (Flat(transform.forward) * 0.3f + Vector3.up).normalized;
            if (!m_Firing && m_StateTime >= k_IntroHeadUp)
                StartFiring(m_BreathRange);
        }

        // Il se pose au centre (d'un coup si l'intro est écourtée, sans bruit ni secousse).
        void LandAtCenter(bool snap)
        {
            var landing = IntroLanding();
            if (snap)
            {
                transform.SetPositionAndRotation(landing.position, landing.rotation);
            }
            else
            {
                // L'impact du piqué : une gerbe de flammes et de poussière, le sol qui tremble.
                transform.SetPositionAndRotation(m_ToPosition, m_IntroLanded);
                FireEffects.Burst(m_ToPosition + Vector3.up * 0.3f, m_FireMaterial, 3f, 70);
                Sfx.Play(m_StepClip, m_ToPosition, 1f, 0.45f);
                Sfx.Play(m_ExplosionClip, m_ToPosition, 0.8f, 0.55f);
                ArenaPlayer.Rumble(0.7f, 0.35f);
            }

            EnterState(State.IntroWait, "Idle01", 0.25f);
            if (m_IntroBreathPending)
            {
                m_IntroBreathPending = false;
                EnterState(State.IntroBreath, "Idle01", 0.2f);
            }
        }

        // Le centre de l'arène, au sol, tourné vers le joueur.
        Pose IntroLanding()
        {
            var center = Center;
            if (m_Arena != null)
                center = m_Arena.Confine(center, m_BodyRadius * 0.5f);
            center.y = GroundHeight(center);
            var toPlayer = ArenaPlayer.IsAlive ? ArenaPlayer.Feet - center : m_Arena != null ? -m_Arena.transform.forward : transform.forward;
            return new Pose(center, Quaternion.LookRotation(Flat(toPlayer)));
        }

        Vector3 TowardsPlayer => ArenaPlayer.IsAlive ? ArenaPlayer.Feet - transform.position : transform.forward;
    }
}
