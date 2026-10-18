using Archery.Core;
using UnityEngine;

namespace Archery.FinalBoss
{
    // Le dragon au sol : il se place, puis choisit une attaque selon la distance du joueur.
    public partial class DragonBoss
    {
        int m_PawSide = 1;
        float m_SweepAngle;
        Vector3 m_SweepForward;
        float m_NextSpark;
        bool m_BreathFromPerch;
        DragonFireball m_Held;
        float m_HeldCharge;

        // ---------------------------------------------------------------- se placer

        void UpdateMoving(float dt)
        {
            if (CheckPhaseChange())
                return;

            // Enragé, il ne reste pas longtemps au sol.
            if (m_Phase == DragonPhase.Enraged && m_FormTimer <= 0f && ArenaPlayer.IsAlive)
            {
                BeginLift();
                return;
            }

            var target = ArenaPlayer.Feet;
            var distance = HorizontalDistance(transform.position, target);
            var facing = FacingAngle(target);
            if (m_AttackDelay <= 0f && ArenaPlayer.IsAlive && TryGroundAttack(distance, facing))
                return;

            // À mi-distance pour cracher ses boules de feu ; parfois il fonce au contact.
            var wanted = m_CloseInTimer > 0f ? m_Reach * 0.6f : m_PreferredDistance;
            var direction = Steer(transform.position, target);
            TurnTowards(direction, m_TurnSpeed, dt);
            if (distance > wanted && Vector3.Angle(transform.forward, direction) < 70f)
            {
                var speed = WalkSpeed;
                var running = speed > m_RunFrom;
                transform.position += transform.forward * (speed * dt);
                Play(running ? "Run" : "Walk");
                m_StrideRate = StrideRate(speed, running);
            }
            else
            {
                Play("Idle01");
            }

            KeepInsideArena();
        }

        bool TryGroundAttack(float distance, float facing)
        {
            // Tout contre sa gueule : il croque.
            var mouthDistance = HorizontalDistance(MouthPosition, ArenaPlayer.Feet);
            if (m_BiteTimer <= 0f && facing < 45f && (mouthDistance <= m_BiteRange || distance <= m_Reach * 0.55f))
            {
                StartBite();
                return true;
            }

            // Proche : coup de patte au ras du sol.
            if (m_SweepTimer <= 0f && facing < 60f && distance <= m_Reach)
            {
                StartSweep();
                return true;
            }

            // Plus loin : il se dresse et frappe le sol ; l'anneau de feu traverse toute l'arène, même derrière un pilier.
            if (m_SlamTimer <= 0f && distance > m_Reach * 0.5f)
            {
                StartSlam();
                return true;
            }

            if (!HasLineOfSight(MouthPosition, ArenaPlayer.Chest))
                return false;

            // Enragé, de temps en temps : le salto, depuis un perchoir.
            if (m_Phase == DragonPhase.Enraged && m_SaltoTimer <= 0f && distance > 8f && TryStartLeap())
                return true;

            if (facing > 30f)
                return false;

            if (m_BreathTimer <= 0f && distance > m_Reach * 0.8f && distance <= m_BreathRange)
            {
                StartBreathCharge(perched: false);
                return true;
            }

            // Son attaque la plus courante.
            if (m_SpitTimer <= 0f && mouthDistance > m_BiteRange)
            {
                StartSpit();
                return true;
            }

            return false;
        }

        float StrideRate(float speed, bool running)
        {
            var natural = running ? m_RunNatural : m_WalkNatural;
            return natural > 0.3f ? Mathf.Clamp(speed / natural, 0.3f, 2f) : 0.6f;
        }

        // ---------------------------------------------------------------- coup de patte : sauter pour l'esquiver

        void StartSweep()
        {
            // La patte du côté du joueur se lève, puis rafle tout le devant du dragon.
            var side = Vector3.Dot(ArenaPlayer.Feet - transform.position, transform.right);
            m_PawSide = Mathf.Abs(side) < 0.5f ? (Random.value < 0.5f ? -1 : 1) : side > 0f ? 1 : -1;
            m_HitDone = false;
            m_NextSpark = 0f;
            EnterState(State.Sweep, "Idle01", 0.2f);
        }

        // Angle (°) de la patte autour du dragon, positif à sa droite : elle part de son côté et balaie tout le devant.
        float SweepAngle(float progress) => Mathf.Lerp(80f, -80f, progress) * m_PawSide;

        void UpdateSweep(float dt)
        {
            var t = m_StateTime;
            if (t < m_SweepWindup)
            {
                // Il lève la patte bien haut, en se tournant encore un peu vers le joueur.
                TurnTowards(ArenaPlayer.Feet - transform.position, m_TurnSpeed * 0.4f, dt);
                m_PawTarget = Smooth(t / m_SweepWindup);
                m_PawDirection = (transform.right * (m_PawSide * 0.45f) + Vector3.up * 0.85f + transform.forward * 0.45f).normalized;
                m_SweepAngle = SweepAngle(0f);
                m_SweepForward = Flat(transform.forward);
                return;
            }

            var swing = (t - m_SweepWindup) / Mathf.Max(0.05f, m_SweepTime);
            if (swing <= 1f)
            {
                if (m_Step == 0)
                {
                    m_Step = 1;
                    Sfx.Play(m_ClawClip, transform.position + transform.forward * 3f, 1f, Random.Range(0.55f, 0.65f));
                }

                // La patte s'abat et rafle le sol d'un côté à l'autre ; le corps suit le geste.
                var angle = SweepAngle(swing);
                var shown = m_PawSide > 0 ? Mathf.Max(angle, -35f) : Mathf.Min(angle, 35f);
                var lowered = Mathf.Lerp(0.6f, -0.45f, Smooth(swing / 0.25f));
                m_PawTarget = 1f;
                m_PawDirection = (Quaternion.AngleAxis(shown, Vector3.up) * m_SweepForward + Vector3.up * lowered).normalized;
                transform.position += m_SweepForward * (m_SweepLunge / Mathf.Max(0.05f, m_SweepTime) * dt);
                transform.Rotate(0f, -m_PawSide * 40f / Mathf.Max(0.05f, m_SweepTime) * dt, 0f, Space.World);
                CheckSweepHit(m_SweepAngle, angle);
                m_SweepAngle = angle;
                SweepSparks(dt);
                KeepInsideArena();
                return;
            }

            if (t >= m_SweepWindup + m_SweepTime + 0.6f)
            {
                m_SweepTimer = m_SweepCooldown;
                EndGroundAttack();
            }
        }

        // La patte passe là où est le joueur : touché, sauf s'il saute par-dessus.
        void CheckSweepHit(float from, float to)
        {
            if (m_HitDone || !ArenaPlayer.IsAlive)
                return;

            var origin = m_Chest != null ? m_Chest.position : transform.position;
            var offset = ArenaPlayer.Feet - origin;
            offset.y = 0f;
            if (offset.magnitude > m_SweepReach)
                return;

            var angle = Vector3.SignedAngle(m_SweepForward, offset, Vector3.up);
            if (angle < Mathf.Min(from, to) - 2f || angle > Mathf.Max(from, to) + 2f)
                return;

            m_HitDone = true;
            if (PlayerLift >= m_JumpClearance)
            {
                ArenaPlayer.ShowDodged();
                return;
            }

            ArenaPlayer.Damage(BaseDamage * m_SweepDamage, ArenaPlayer.Feet + Vector3.up * 0.3f, Enemy);
            ArenaPlayer.Rumble(0.8f, 0.25f);
        }

        // Des étincelles là où les griffes raclent le sol.
        void SweepSparks(float dt)
        {
            m_NextSpark -= dt;
            if (m_NextSpark > 0f || m_FireMaterial == null)
                return;

            m_NextSpark = 0.04f;
            var hand = m_PawSide > 0 ? m_HandR : m_HandL;
            if (hand == null)
                return;
            var point = hand.position;
            point.y = GroundHeight(point) + 0.2f;
            FireEffects.Burst(point, m_FireMaterial, 0.5f, 6);
        }

        // ---------------------------------------------------------------- morsure : très proche, presque inévitable

        void StartBite()
        {
            m_HitDone = false;
            EnterState(State.Bite, "Basic Attack", 0.1f, restart: true);
        }

        void UpdateBite(float dt)
        {
            TurnTowards(ArenaPlayer.Feet - transform.position, m_TurnSpeed, dt);
            if (!m_HitDone && m_StateTime >= m_BiteWindup)
            {
                m_HitDone = true;
                var feet = ArenaPlayer.Feet;
                if (ArenaPlayer.IsAlive && (HorizontalDistance(MouthPosition, feet) <= m_BiteRange + 1.2f || HorizontalDistance(transform.position, feet) <= m_Reach * 0.6f))
                {
                    ArenaPlayer.Damage(BaseDamage * m_BiteDamage, ArenaPlayer.Chest, Enemy);
                    ArenaPlayer.Rumble(0.8f, 0.25f);
                }

                Sfx.Play(m_BiteClip, MouthPosition, 1f, Random.Range(0.6f, 0.75f));
            }

            if (m_StateTime >= m_BiteWindup + 0.75f)
            {
                m_BiteTimer = m_BiteCooldown;
                EndGroundAttack();
            }
        }

        // ---------------------------------------------------------------- dressé, puis il frappe le sol : anneau de feu

        Vector3 m_SlamPivot;
        Quaternion m_SlamYaw = Quaternion.identity;

        void StartSlam()
        {
            // Il se dresse en pivotant sur ses pattes arrière, en rugissant.
            m_SlamYaw = Quaternion.LookRotation(Flat(transform.forward));
            m_SlamPivot = transform.TransformPoint(m_HindPivot);
            EnterState(State.Slam, "Scream", 0.2f, restart: true);
            Sfx.Play(m_RoarClip, MouthPosition, 0.9f, 0.6f, 0.6f);
        }

        void UpdateSlam(float dt)
        {
            var t = m_StateTime;
            float angle;
            if (t < m_SlamRearTime)
            {
                // Dressé sur ses pattes arrière, il se tourne encore un peu vers le joueur.
                var toPlayer = Flat(ArenaPlayer.Feet - m_SlamPivot);
                m_SlamYaw = Quaternion.RotateTowards(m_SlamYaw, Quaternion.LookRotation(toPlayer), m_TurnSpeed * 0.5f * dt);
                angle = m_SlamRearAngle * Smooth(t / Mathf.Max(0.1f, m_SlamRearTime));
            }
            else if (t < m_SlamRearTime + m_SlamFallTime)
            {
                // Il retombe de tout son poids sur ses pattes avant, de plus en plus vite.
                var fall = (t - m_SlamRearTime) / Mathf.Max(0.05f, m_SlamFallTime);
                angle = m_SlamRearAngle * (1f - fall * fall);
            }
            else
            {
                angle = 0f;
                if (m_Step == 0)
                {
                    m_Step = 1;
                    SlamImpact();
                }

                if (t >= m_SlamRearTime + m_SlamFallTime + 0.9f)
                {
                    SetRearPose(0f);
                    m_SlamTimer = m_SlamCooldown;
                    EndGroundAttack();
                    return;
                }
            }

            m_RearTarget = angle / Mathf.Max(1f, m_SlamRearAngle);
            SetRearPose(angle);
        }

        // Le corps basculé vers l'arrière de cet angle (°), autour de ses pattes arrière restées au sol.
        void SetRearPose(float angle)
        {
            var rotation = m_SlamYaw * Quaternion.Euler(-angle, 0f, 0f);
            transform.rotation = rotation;
            transform.position = m_SlamPivot - rotation * (m_HindPivot * transform.lossyScale.x);
        }

        // Ses pattes avant frappent le sol : un anneau de feu part de là et traverse toute l'arène.
        void SlamImpact()
        {
            Play("Idle01", 0.25f);
            var paws = transform.TransformPoint(m_FrontPaws);
            paws.y = GroundHeight(paws);
            FireEffects.Burst(paws + Vector3.up * 0.3f, m_FireMaterial, 3f, 80);
            Sfx.Play(m_ExplosionClip, paws, 1f, 0.5f);
            Sfx.Play(m_StepClip, paws, 1f, 0.4f);
            ArenaPlayer.Rumble(0.9f, 0.4f);
            Slammed?.Invoke(paws);

            var reach = m_Arena != null ? HorizontalDistance(paws, Center) + m_Arena.RoamRadius + 4f : 40f;
            FireRing.Spawn(new Vector3(paws.x, FloorY, paws.z), m_SlamRingSpeed * m_Tempo, reach, m_SlamRingHeight,
                           BaseDamage * m_SlamDamage, m_SlamClearance, m_FireballMaterial, m_FireMaterial, m_Arena, Enemy);
        }

        // ---------------------------------------------------------------- boule de feu au sol

        void StartSpit()
        {
            EnterState(State.Spit, "Idle01", 0.2f);
            m_Held = CreateFireball();
            m_HeldCharge = 0f;
        }

        void UpdateSpit(float dt)
        {
            TurnTowards(ArenaPlayer.Feet - transform.position, m_TurnSpeed, dt);
            if (m_Step == 0)
            {
                // Elle se forme dans sa gueule, la tête un peu en arrière.
                var charge = Mathf.Clamp01(m_StateTime / Mathf.Max(0.05f, m_SpitCharge));
                m_NeckTarget = -18f * Smooth(charge);
                m_JawTarget = Smooth(charge);
                m_HeldCharge = charge;
                if (charge < 1f)
                    return;

                m_Step = 1;
                m_StateTime = 0f;
                LaunchHeld(m_FireballSpeed);
                return;
            }

            // Un coup de tête vers l'avant au tir, puis il se remet.
            m_NeckTarget = m_StateTime < 0.15f ? 14f : 0f;
            m_JawTarget = m_StateTime < 0.3f ? 0.6f : 0f;
            if (m_StateTime >= 0.6f)
            {
                m_SpitTimer = m_SpitCooldown * Random.Range(0.8f, 1.2f);
                EndGroundAttack(0.3f);
            }
        }

        DragonFireball CreateFireball() =>
            DragonFireball.Create(HeldPosition(0f), m_FireballSize, m_FireballMaterial, m_FireMaterial, m_ExplosionClip, this);

        // La boule qui se forme : devant la gueule, d'autant plus loin qu'elle grossit.
        Vector3 HeldPosition(float charge) => MouthPosition + SnoutDirection * (m_FireballSize * 0.5f * Mathf.Lerp(0.15f, 1f, charge));

        // Elle part tout droit vers le joueur, là où il est au tir, sans anticiper : on l'esquive en se déplaçant.
        void LaunchHeld(float speed)
        {
            if (m_Held == null)
                return;

            var from = m_Held.transform.position;
            var direction = ArenaPlayer.Chest - from;
            if (direction.sqrMagnitude < 0.01f)
                direction = transform.forward;
            m_Held.Launch(direction.normalized * speed, BaseDamage * m_FireballDamage, m_FireballSplash);
            m_Held = null;
            Sfx.Play(m_FireballClip, from, 1f, 0.6f);
        }

        void DropHeld()
        {
            if (m_Held == null)
                return;

            m_Held.Fizzle();
            m_Held = null;
        }

        // ---------------------------------------------------------------- souffle : charge tête levée, puis le feu

        void StartBreathCharge(bool perched)
        {
            m_BreathFromPerch = perched;
            EnterState(State.BreathCharge, "Idle01", 0.25f);
            StartInhale();
        }

        void UpdateBreathCharge(float dt)
        {
            TurnTowards(ArenaPlayer.Feet - transform.position, m_TurnSpeed * 0.5f, dt);

            // La tête se lève vers la voûte, la gueule s'ouvre et une lumière de plus en plus vive y brille.
            var t = Mathf.Clamp01(m_StateTime / Mathf.Max(0.1f, m_BreathCharge));
            var up = Smooth(m_StateTime / Mathf.Max(0.3f, m_BreathCharge * 0.3f));
            m_NeckTarget = -m_ChargePitch * up;
            m_JawTarget = up;
            m_GlowTarget = t * t;
            UpdateInhale(t);
            if (t >= 1f)
                StartBreath();
        }

        void StartBreath()
        {
            StopInhale();
            var mouth = MouthPosition;
            var range = BreathRange;
            // L'animation de souffle est prise au moment où le feu sort : la charge a remplacé son élan.
            EnterState(State.Breath, "Flame Attack", 0.12f, restart: true, offset: 0.7f);
            m_BreathAim = ClampToFacing((ArenaPlayer.Chest - mouth).normalized, 45f);
            m_Glow = 1f;
            StartFiring(range);
        }

        float BreathRange => m_BreathFromPerch ? m_PerchBreathRange : m_BreathRange;

        void UpdateBreath(float dt)
        {
            if (m_Firing)
            {
                // La flamme suit le joueur lentement : de près, on la distance en tournant autour du dragon.
                var wanted = ClampToFacing((ArenaPlayer.Chest - MouthPosition).normalized, 45f);
                m_BreathAim = Vector3.RotateTowards(m_BreathAim, wanted, m_BreathTracking * Mathf.Deg2Rad * dt, 0f);
                TurnTowards(ArenaPlayer.Feet - transform.position, m_BreathTracking * 0.6f, dt);
                DamageInCone(dt, MouthPosition, m_BreathAim, BreathRange, m_BreathAngle, m_BreathDamage);
            }

            if (m_StateTime >= m_BreathDuration)
                StopBreath();

            if (m_StateTime < m_BreathDuration + 0.6f)
                return;

            m_BreathTimer = m_BreathCooldown;
            if (m_BreathFromPerch)
            {
                m_PerchTimer = m_PerchCooldown;
                BeginLift();
            }
            else
            {
                EndGroundAttack();
            }
        }

        Vector3 m_BreathAim;
        float m_BreathTick;

        void StartFiring(float range)
        {
            m_Firing = true;
            m_BreathTick = 0f;
            if (m_Breath != null)
            {
                FireEffects.SetBreathRange(m_Breath, range);
                m_Breath.transform.SetPositionAndRotation(MouthPosition, Quaternion.LookRotation(m_BreathAim));
                m_Breath.Play(true);
            }

            if (m_FireSource != null && m_FireLoopClip != null)
            {
                m_FireSource.clip = m_FireLoopClip;
                m_FireSource.volume = 1f;
                m_FireSource.pitch = 0.75f;
                if (!m_FireSource.isPlaying)
                    m_FireSource.Play();
            }

            if (m_BreathLight != null)
                m_BreathLight.enabled = true;
        }

        void StopBreath()
        {
            if (!m_Firing)
                return;

            m_Firing = false;
            if (m_Breath != null)
                m_Breath.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (m_FireSource != null)
                m_FireSource.Stop();
            if (m_BreathLight != null)
                m_BreathLight.enabled = false;
        }

        // L'inspiration avant le souffle : le feu gronde au fond de la gorge, de plus en plus fort.
        void StartInhale()
        {
            if (m_FireSource == null || m_FireLoopClip == null)
                return;

            m_FireSource.clip = m_FireLoopClip;
            m_FireSource.volume = 0f;
            m_FireSource.pitch = 0.45f;
            m_FireSource.Play();
        }

        void UpdateInhale(float t)
        {
            if (m_FireSource == null || m_Firing)
                return;

            m_FireSource.volume = 0.55f * t;
            m_FireSource.pitch = 0.45f + 0.3f * t;
        }

        void StopInhale()
        {
            if (m_FireSource != null && !m_Firing)
                m_FireSource.Stop();
        }

        // Dégâts dans le cône de feu, toutes les 0,2 s, si rien (un pilier) ne protège le joueur.
        void DamageInCone(float deltaTime, Vector3 origin, Vector3 direction, float range, float halfAngle, float damagePerSecond)
        {
            m_BreathTick -= deltaTime;
            if (m_BreathTick > 0f || !ArenaPlayer.IsAlive)
                return;

            const float tick = 0.2f;
            m_BreathTick = tick;
            var chest = ArenaPlayer.Chest;
            var toPlayer = chest - origin;
            if (toPlayer.magnitude > range || Vector3.Angle(direction, toPlayer) > halfAngle)
                return;
            if (!HasLineOfSight(origin, chest))
                return;

            ArenaPlayer.Damage(BaseDamage * damagePerSecond * tick, chest, Enemy);
            ArenaPlayer.Rumble(0.35f, 0.1f);
        }
    }
}
