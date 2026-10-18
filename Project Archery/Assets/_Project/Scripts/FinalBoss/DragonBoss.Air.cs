using Archery.Core;
using Archery.Player;
using UnityEngine;

namespace Archery.FinalBoss
{
    // Le dragon en vol : boules de feu, ruées enflammées, perchoirs, et son salto (phase 3).
    public partial class DragonBoss
    {
        const float k_LeapTime = 1.2f;
        const float k_StripGlowHeight = 4f;

        static readonly Collider[] s_PlayerOverlaps = new Collider[128];

        bool m_PlayerCollisionIgnored;
        float m_OrbitAngle;
        float m_AirSpit;
        float m_SinceAirSpit = 10f;
        int m_DashIndex;
        float m_DashHeight;
        float m_DashFront;
        float m_NextFire;
        Vector3 m_DashStart;
        Vector3 m_DashEnd;
        Pose m_Perch;
        PerchPurpose m_PerchPurpose;
        bool m_RoarOnPerch;
        bool m_SaltoTargetSet;
        Vector3 m_SaltoTarget;
        Vector3 m_SaltoBase;
        Vector3 m_SaltoApex;
        Quaternion m_SaltoYaw = Quaternion.identity;
        Quaternion m_LandedRotation = Quaternion.identity;

        // ---------------------------------------------------------------- envol

        void BeginLift()
        {
            StopAttacks();
            m_FromPosition = transform.position;
            var offset = transform.position - Center;
            offset.y = 0f;
            m_OrbitAngle = offset.sqrMagnitude > 0.01f ? Mathf.Atan2(offset.x, offset.z) : 0f;
            m_ToPosition = OrbitPoint(m_OrbitAngle);
            EnterState(State.TakeOff, "Take Off", 0.2f, restart: true);
            Sfx.Play(m_WingClip, transform.position, 1f, 0.5f);
        }

        void UpdateTakeOff(float dt)
        {
            var t = Mathf.Clamp01(m_StateTime / Mathf.Max(0.1f, m_LiftTime));
            transform.position = Vector3.Lerp(m_FromPosition, m_ToPosition, Smooth(t));
            TurnTowards(Tangent(m_OrbitAngle), m_TurnSpeed, dt);
            if (t < 1f)
                return;

            if (m_Phase == DragonPhase.Enraged)
                m_FormTimer = m_RageAirTime;
            m_AirFireballTimer = m_AirFireballInterval * 0.5f;
            if (m_DashTimer < 4f)
                m_DashTimer = 4f;
            if (m_PerchTimer < 8f)
                m_PerchTimer = 8f;
            EnterState(State.Flying, "Fly Forward");
        }

        // Reprend son vol en cercle, de là où il est.
        void ResumeOrbit()
        {
            var offset = transform.position - Center;
            offset.y = 0f;
            m_OrbitAngle = offset.sqrMagnitude > 0.01f ? Mathf.Atan2(offset.x, offset.z) : 0f;
            EnterState(State.Flying, "Fly Forward", 0.3f);
        }

        void UpdateFlying(float dt)
        {
            if (CheckPhaseChange())
                return;

            // Tourne autour de l'arène, au-dessus des piliers.
            m_OrbitAngle += m_FlySpeed / Mathf.Max(1f, m_OrbitRadius) * dt;
            transform.position = Vector3.MoveTowards(transform.position, OrbitPoint(m_OrbitAngle), m_FlySpeed * 1.5f * dt);
            TurnTowards(Tangent(m_OrbitAngle), m_TurnSpeed, dt);

            if (!ArenaPlayer.IsAlive)
            {
                DropHeld();
                return;
            }

            m_SinceAirSpit += dt;
            if (m_Held == null)
            {
                // Enragé, il ne reste pas longtemps en l'air : il revient au sol par son salto.
                if (m_Phase == DragonPhase.Enraged && m_FormTimer <= 0f)
                {
                    StartSaltoFromAir();
                    return;
                }

                if (m_DashTimer <= 0f && StartDashes())
                    return;
                if (m_Phase == DragonPhase.Air && m_PerchTimer <= 0f && TryFlyToPerch(PerchPurpose.Breath))
                    return;

                if (m_AirFireballTimer <= 0f)
                {
                    m_Held = CreateFireball();
                    m_HeldCharge = 0f;
                    m_AirSpit = 0f;
                    Play("Fly Flame Attack", 0.15f, restart: true);
                }
                else if (m_SinceAirSpit > 0.9f && m_Playing == "Fly Flame Attack")
                {
                    Play("Fly Forward", 0.3f);
                }

                return;
            }

            // Une boule de feu se forme dans sa gueule, puis part vers le joueur.
            m_AirSpit += dt;
            m_HeldCharge = Mathf.Clamp01(m_AirSpit / Mathf.Max(0.05f, m_SpitCharge));
            m_JawTarget = 1f;
            if (m_HeldCharge < 1f)
                return;

            LaunchHeld(m_AirFireballSpeed);
            m_AirFireballTimer = m_AirFireballInterval;
            m_SinceAirSpit = 0f;
        }

        Vector3 OrbitPoint(float angle) =>
            Center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * m_OrbitRadius + Vector3.up * (m_FlyAltitude + Mathf.Sin(Time.time * 0.8f) * 0.6f);

        static Vector3 Tangent(float angle) => new Vector3(Mathf.Cos(angle), 0f, -Mathf.Sin(angle));

        // ---------------------------------------------------------------- ruées enflammées (trois d'affilée)

        bool StartDashes()
        {
            m_DashIndex = 0;
            if (!PlanDash())
            {
                m_DashTimer = 3f;
                return false;
            }

            EnterState(State.Dash, "Fly Glide", 0.3f);
            ShowStrip(m_DashStart, m_DashEnd, m_DashWidth);
            return true;
        }

        // Une ruée : une ligne droite qui passe par le joueur, dans l'axe dragon → joueur si possible, sans pilier dessous.
        bool PlanDash()
        {
            var player = PlayerGround();
            var toPlayer = player - transform.position;
            toPlayer.y = 0f;
            var forward = toPlayer.sqrMagnitude > 1f ? toPlayer.normalized : Flat(transform.forward);
            var half = m_DashLength * 0.5f;
            float[] turns = { 0f, 25f, -25f, 50f, -50f, 80f, -80f };
            var found = false;
            var clear = false;
            foreach (var turn in turns)
            {
                var direction = Quaternion.Euler(0f, turn, 0f) * forward;
                var start = ClampToRoam(player - direction * half);
                var end = ClampToRoam(player + direction * half);
                if (HorizontalDistance(start, end) < 8f)
                    continue;

                var isClear = LineClearOfPillars(start, end, m_BodyRadius * 0.6f);
                if (found && !isClear)
                    continue;

                m_DashStart = start;
                m_DashEnd = end;
                found = true;
                clear = isClear;
                if (clear)
                    break;
            }

            if (!found)
                return false;

            // Au-dessus des piliers s'il n'a pas trouvé de ligne libre.
            m_DashHeight = m_DashAltitude;
            if (!clear && m_Arena != null)
            {
                foreach (var pillar in m_Arena.Pillars)
                    m_DashHeight = Mathf.Max(m_DashHeight, pillar.Position.y - FloorY + pillar.Height + 2.5f);
            }

            m_DashStart.y = FloorY;
            m_DashEnd.y = FloorY;
            m_FromPosition = transform.position;
            m_HitDone = false;
            m_Step = 0;
            return true;
        }

        Vector3 ClampToRoam(Vector3 point)
        {
            var offset = point - Center;
            offset.y = 0f;
            var radius = m_Arena != null ? Mathf.Max(4f, m_Arena.RoamRadius - 1f) : 20f;
            if (offset.magnitude > radius)
                point = Center + offset.normalized * radius;
            return point;
        }

        void UpdateDash(float dt)
        {
            var direction = m_DashEnd - m_DashStart;
            direction.y = 0f;
            var length = direction.magnitude;
            direction /= Mathf.Max(0.01f, length);
            var start = m_DashStart + Vector3.up * m_DashHeight;

            if (m_Step == 0)
            {
                // La bande rouge prévient ; il se place au début de la ligne.
                var warning = m_DashIndex == 0 ? m_DashWarning : m_DashWarning * 0.7f;
                var t = Mathf.Clamp01(m_StateTime / Mathf.Max(0.1f, warning));
                transform.position = Vector3.Lerp(m_FromPosition, start, Smooth(t));
                TurnTowards(direction, m_TurnSpeed * 3f, dt);
                if (t < 1f)
                    return;

                m_Step = 1;
                m_StateTime = 0f;
                m_DashFront = 0f;
                m_NextFire = 1f;
                transform.rotation = Quaternion.LookRotation(direction);
                Play("Fly Flame Attack", 0.1f, restart: true);
                m_BreathAim = DashAim();
                StartFiring(m_DashHeight + 6f);
                Sfx.Play(m_WingClip, transform.position, 1f, 0.7f);
                return;
            }

            // Il file au-dessus de la bande en crachant le feu sous lui.
            var travelled = Mathf.Min(length, m_StateTime * m_DashSpeed);
            transform.position = start + direction * travelled;
            m_BreathAim = DashAim();
            var front = Mathf.Min(length, travelled + 2.5f);
            BurnStrip(m_DashFront, front, direction);
            m_DashFront = front;
            if (travelled < length)
                return;

            StopBreath();
            HideStrip();
            m_DashIndex++;
            if (m_DashIndex < m_DashCount && ArenaPlayer.IsAlive && PlanDash())
            {
                m_StateTime = 0f;
                Play("Fly Glide", 0.2f);
                ShowStrip(m_DashStart, m_DashEnd, m_DashWidth);
                return;
            }

            m_DashTimer = m_DashCooldown;
            m_AirFireballTimer = Mathf.Max(m_AirFireballTimer, 1f);
            ResumeOrbit();
        }

        Vector3 DashAim() => (transform.forward * 0.9f + Vector3.down).normalized;

        // Le feu touche le sol entre deux positions le long de la ligne : il y laisse des flammes, et brûle le joueur qu'il
        // rattrape dans la bande.
        void BurnStrip(float from, float to, Vector3 direction)
        {
            while (m_NextFire <= to)
            {
                var point = m_DashStart + direction * m_NextFire;
                point.y = GroundHeight(point);
                FireTrail.Spawn(point, m_DashWidth * 0.5f, m_DashFireTime, BaseDamage * m_DashFireDamage, m_FireMaterial, Enemy);
                m_NextFire += 1.5f;
            }

            if (m_HitDone || !ArenaPlayer.IsAlive)
                return;

            var offset = ArenaPlayer.Feet - m_DashStart;
            offset.y = 0f;
            var along = Vector3.Dot(offset, direction);
            var side = (offset - direction * along).magnitude;
            if (along < from || along > to || side > m_DashWidth * 0.5f + 0.3f || PlayerLift > 2.5f)
                return;

            m_HitDone = true;
            ArenaPlayer.Damage(BaseDamage * m_DashDamage, ArenaPlayer.Feet + Vector3.up * 0.5f, Enemy);
            ArenaPlayer.Rumble(0.6f, 0.25f);
        }

        // ---------------------------------------------------------------- perchoirs

        bool TryFlyToPerch(PerchPurpose purpose)
        {
            if (!FindPerch(purpose, out var perch))
            {
                if (purpose == PerchPurpose.Breath)
                    m_PerchTimer = 4f;
                return false;
            }

            StopAttacks();
            m_Perch = perch;
            m_PerchPurpose = purpose;
            m_SaltoTargetSet = false;
            m_FromPosition = transform.position;
            EnterState(State.PerchFlight, "Fly Forward", 0.3f);
            return true;
        }

        // Un perchoir à bonne distance du joueur (et d'où il le voit, pour le souffle), plutôt proche du dragon.
        bool FindPerch(PerchPurpose purpose, out Pose best)
        {
            best = default;
            if (m_Arena == null)
                return false;

            var player = ArenaPlayer.Feet;
            var bestScore = float.MaxValue;
            var found = false;
            foreach (var perch in m_Arena.Perches)
            {
                var toPlayer = HorizontalDistance(perch.position, player);
                if (toPlayer < 8f)
                    continue;
                if (purpose == PerchPurpose.Breath &&
                    (toPlayer > m_PerchBreathRange - 3f || !HasLineOfSight(perch.position + Vector3.up * 3f, ArenaPlayer.Chest)))
                    continue;

                var score = Vector3.Distance(transform.position, perch.position) * Random.Range(0.8f, 1.2f);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                best = perch;
                found = true;
            }

            return found;
        }

        void UpdatePerchFlight(float dt)
        {
            var above = m_Perch.position + Vector3.up * 4f;
            if (m_Step == 0)
            {
                // Il vole jusqu'au-dessus du perchoir…
                var flight = Mathf.Clamp(Vector3.Distance(m_FromPosition, above) / (m_FlySpeed * 2f), 1f, 3f);
                var t = Mathf.Clamp01(m_StateTime / flight);
                transform.position = Vector3.Lerp(m_FromPosition, above, Smooth(t));
                TurnTowards(above - m_FromPosition, m_TurnSpeed * 1.5f, dt);
                if (t < 1f)
                    return;

                m_Step = 1;
                m_StateTime = 0f;
                Play("Land", 0.2f, restart: true);
                return;
            }

            // … et s'y pose, face au joueur.
            const float landTime = 1f;
            var land = Mathf.Clamp01(m_StateTime / landTime);
            transform.position = Vector3.Lerp(above, m_Perch.position, land * land);
            TurnTowards(ArenaPlayer.Feet - transform.position, m_TurnSpeed * 1.5f, dt);
            if (land < 1f)
                return;

            Sfx.Play(m_StepClip, transform.position, 1f, 0.5f);
            ArenaPlayer.Rumble(0.3f, 0.2f);
            EnterState(State.Perched, "Idle01", 0.3f);
        }

        void UpdatePerched(float dt)
        {
            TurnTowards(ArenaPlayer.Feet - transform.position, m_TurnSpeed, dt);
            if (m_StateTime < m_PerchHold || !ArenaPlayer.IsAlive)
                return;

            if (m_PerchPurpose == PerchPurpose.Breath)
            {
                StartBreathCharge(perched: true);
            }
            else if (m_RoarOnPerch)
            {
                // Le passage en rage : il rugit sur son perchoir, puis s'élance.
                m_RoarOnPerch = false;
                Roar(StartSalto);
            }
            else
            {
                StartSalto();
            }
        }

        // ---------------------------------------------------------------- salto (phase 3)

        // Retour au sol depuis les airs : il se pose sur un perchoir, puis fait son salto.
        void StartSaltoFromAir()
        {
            if (!TryFlyToPerch(PerchPurpose.Salto))
                BeginLanding();
        }

        // Depuis le sol : il saute sur un perchoir pas trop loin, puis fait son salto.
        bool TryStartLeap()
        {
            if (!FindPerch(PerchPurpose.Salto, out var perch) || HorizontalDistance(transform.position, perch.position) > 24f)
            {
                m_SaltoTimer = 4f;
                return false;
            }

            m_Perch = perch;
            m_PerchPurpose = PerchPurpose.Salto;
            m_SaltoTarget = PlayerGround();
            m_SaltoTargetSet = true;
            m_FromPosition = transform.position;
            EnterState(State.Leap, "Take Off", 0.15f, restart: true);
            Sfx.Play(m_WingClip, transform.position, 1f, 0.6f);
            return true;
        }

        void UpdateLeap(float dt)
        {
            var t = Mathf.Clamp01(m_StateTime / k_LeapTime);
            var arc = 3f + Mathf.Abs(m_Perch.position.y - m_FromPosition.y) * 0.5f;
            transform.position = Vector3.Lerp(m_FromPosition, m_Perch.position, Smooth(t)) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arc);
            TurnTowards(m_Perch.position - m_FromPosition, m_TurnSpeed * 2f, dt);
            if (t < 1f)
                return;

            Sfx.Play(m_StepClip, transform.position, 1f, 0.5f);
            StartSalto();
        }

        // Le salto : il s'élance du perchoir, fait trois saltos raide comme un piquet (bras et ailes écartés), s'arrête
        // en l'air, puis pique, net, sur l'endroit où était le joueur au début de l'attaque.
        void StartSalto()
        {
            if (!m_SaltoTargetSet)
                m_SaltoTarget = PlayerGround();
            m_SaltoTargetSet = false;

            m_SaltoBase = BodyCenter;
            var ceiling = m_Arena != null ? m_Arena.CeilingPoint(transform.position, out _).y : m_SaltoBase.y + 30f;
            var rise = Mathf.Clamp(ceiling - 7.5f - m_SaltoBase.y, 2.5f, Mathf.Max(2.5f, m_SaltoRise));
            m_SaltoApex = m_SaltoBase + Vector3.up * rise;
            m_SaltoYaw = Quaternion.LookRotation(Flat(m_SaltoTarget - transform.position));
            EnterState(State.Salto, "Fly Glide", 0.2f);
            Sfx.Play(m_WingClip, transform.position, 1f, 0.55f);
        }

        void UpdateSalto(float dt)
        {
            switch (m_Step)
            {
                case 0:
                {
                    // Trois saltos en arrière, tout raide.
                    var t = Mathf.Clamp01(m_StateTime / Mathf.Max(0.2f, m_SaltoFlipTime));
                    var center = Vector3.Lerp(m_SaltoBase, m_SaltoApex, 1f - (1f - t) * (1f - t));
                    var pitch = -360f * m_SaltoFlips * Smooth(t);
                    m_TPoseTarget = 1f;
                    SetBodyPose(center, m_SaltoYaw * Quaternion.Euler(pitch, 0f, 0f));
                    if (t < 1f)
                        return;

                    m_Step = 1;
                    m_StateTime = 0f;
                    ShowCircle(m_SaltoTarget, m_SaltoRadius);
                    break;
                }
                case 1:
                {
                    // Un temps d'arrêt : il se reprend et vise.
                    var t = Mathf.Clamp01(m_StateTime / Mathf.Max(0.05f, m_SaltoPause));
                    var aim = Quaternion.LookRotation((m_SaltoTarget + Vector3.up * 2f - m_SaltoApex).normalized);
                    SetBodyPose(m_SaltoApex, Quaternion.Slerp(m_SaltoYaw, aim, Smooth(t)));
                    if (t < 1f)
                        return;

                    m_Step = 2;
                    m_StateTime = 0f;
                    Sfx.Play(m_RoarClip, MouthPosition, 0.8f, 0.6f, 0.6f);
                    break;
                }
                default:
                {
                    // Le piqué, net et rapide, droit sur la cible.
                    var goal = m_SaltoTarget + Vector3.up * 2f;
                    var toGoal = goal - BodyCenter;
                    var step = m_SaltoDiveSpeed * dt;
                    if (toGoal.magnitude <= step)
                    {
                        SaltoImpact();
                        return;
                    }

                    SetBodyPose(BodyCenter + toGoal.normalized * step, Quaternion.LookRotation(toGoal.normalized));
                    break;
                }
            }
        }

        void SaltoImpact()
        {
            HideCircle();
            var ground = m_Arena != null ? m_Arena.Confine(m_SaltoTarget, m_BodyRadius * 0.5f) : m_SaltoTarget;
            ground.y = GroundHeight(ground);
            m_LandedRotation = Quaternion.LookRotation(Flat(transform.forward.y > -0.99f ? transform.forward : m_SaltoYaw * Vector3.forward));
            transform.position = ground;

            FireEffects.Burst(m_SaltoTarget + Vector3.up * 0.5f, m_FireMaterial, m_SaltoRadius, 90);
            Sfx.Play(m_ExplosionClip, m_SaltoTarget, 1f, 0.6f);
            Sfx.Play(m_StepClip, m_SaltoTarget, 1f, 0.45f);
            var feet = ArenaPlayer.Feet;
            if (ArenaPlayer.IsAlive && HorizontalDistance(feet, m_SaltoTarget) <= m_SaltoRadius && Mathf.Abs(feet.y - m_SaltoTarget.y) < 3f)
            {
                ArenaPlayer.Damage(BaseDamage * m_SaltoDamage, m_SaltoTarget, Enemy);
                ArenaPlayer.Rumble(1f, 0.4f);
            }
            else
            {
                ArenaPlayer.Rumble(0.4f, 0.3f);
            }

            EnterState(State.Recover, "Get Hit", 0.1f, restart: true);
        }

        // Il se relève de son piqué, puis se bat au sol.
        void UpdateRecover(float dt)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, m_LandedRotation, 360f * dt);
            if (m_StateTime < m_SaltoRecover)
                return;

            m_SaltoTimer = m_SaltoCooldown;
            m_FormTimer = m_RageGroundTime;
            EndGroundAttack();
        }

        // Pendant le salto et le temps qu'il se relève, le corps du dragon ne bouscule pas le joueur : en lui retombant
        // dessus, il l'enfonçait dans le sol, et le joueur tombait à travers la carte. Le contact revient une fois le
        // joueur dégagé.
        void UpdatePlayerCollision()
        {
            var rig = PlayerRig.Instance;
            var body = rig != null ? rig.Body : null;
            if (body == null)
                return;

            var ignore = m_State == State.Salto || m_State == State.Recover;
            if (!ignore && m_PlayerCollisionIgnored && PlayerInsideBody(body))
                ignore = true;
            if (ignore == m_PlayerCollisionIgnored)
                return;

            m_PlayerCollisionIgnored = ignore;
            foreach (var hitbox in m_Hitboxes)
            {
                if (hitbox != null)
                    Physics.IgnoreCollision(body, hitbox, ignore);
            }
        }

        bool PlayerInsideBody(CharacterController body)
        {
            var scale = body.transform.lossyScale.y;
            var center = body.transform.TransformPoint(body.center);
            var radius = body.radius * scale + 0.1f;
            var half = Mathf.Max(0f, body.height * 0.5f * scale - radius);
            var count = Physics.OverlapCapsuleNonAlloc(center + Vector3.up * half, center - Vector3.up * half, radius,
                                                       s_PlayerOverlaps, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                if (m_Hitboxes.Contains(s_PlayerOverlaps[i]))
                    return true;
            }

            return false;
        }

        // Place le dragon pour que le milieu de son corps soit à ce point : il tourne sur lui-même, pas autour de ses pieds.
        void SetBodyPose(Vector3 center, Quaternion rotation)
        {
            transform.rotation = rotation;
            transform.position = center - rotation * (m_BodyCenter * transform.lossyScale.x);
        }

        Vector3 BodyCenter => transform.position + transform.rotation * (m_BodyCenter * transform.lossyScale.x);

        /// <summary>Le milieu de son corps (monde) : ce que montre la flèche au bord de la vue.</summary>
        public Vector3 BodyPoint => BodyCenter;

        /// <summary>Matériau des alertes au sol (couleurs des sommets, transparent), repris par la flèche au bord de la vue.</summary>
        public Material WarningMaterial => m_WarningMaterial;

        // ---------------------------------------------------------------- retour au sol sans perchoir

        void BeginLanding()
        {
            StopAttacks();
            var away = Center - ArenaPlayer.Feet;
            away.y = 0f;
            var landing = Center + (away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward) * 6f;
            if (m_Arena != null)
                landing = m_Arena.Confine(landing, m_BodyRadius * 0.5f);
            landing.y = GroundHeight(landing);
            m_FromPosition = transform.position;
            m_ToPosition = landing;
            EnterState(State.Landing, "Fly Glide", 0.3f);
        }

        void UpdateLanding(float dt)
        {
            const float glideTime = 2f;
            const float landTime = 1.5f;
            var above = m_ToPosition + Vector3.up * 4f;
            if (m_StateTime < glideTime)
            {
                transform.position = Vector3.Lerp(m_FromPosition, above, Smooth(m_StateTime / glideTime));
                TurnTowards(above - m_FromPosition, m_TurnSpeed, dt);
                return;
            }

            if (m_Playing != "Land")
                Play("Land", 0.2f);

            var t = Mathf.Clamp01((m_StateTime - glideTime) / landTime);
            transform.position = Vector3.Lerp(above, m_ToPosition, t * t);
            TurnTowards(ArenaPlayer.Feet - transform.position, m_TurnSpeed, dt);
            if (t < 1f)
                return;

            Sfx.Play(m_StepClip, transform.position, 1f, 0.5f);
            ArenaPlayer.Rumble(0.5f, 0.3f);
            m_FormTimer = m_RageGroundTime;
            if (m_Phase == DragonPhase.Enraged && m_RoarOnPerch)
            {
                m_RoarOnPerch = false;
                Roar(null);
                return;
            }

            EndGroundAttack();
        }

        // ---------------------------------------------------------------- alertes au sol

        Transform m_Strip;
        Transform m_Circle;
        float m_WarningAge;

        // Bande rouge le long d'une ruée, posée sur le sol (et sur l'estrade ou la lave).
        void ShowStrip(Vector3 from, Vector3 to, float width)
        {
            HideStrip();
            if (m_WarningMaterial == null)
                return;

            var direction = to - from;
            direction.y = 0f;
            var length = direction.magnitude;
            if (length < 0.5f)
                return;

            var middle = (from + to) * 0.5f;
            middle.y = FloorY;
            var rotation = Quaternion.LookRotation(direction / length);
            var builder = new MeshBuilder();
            var fill = new Color(1f, 0.15f, 0.05f, 0.35f);
            var edge = new Color(1f, 0.2f, 0.05f, 0.85f);
            var glow = new Color(1.5f, 0.38f, 0.12f, 0.28f);
            var clear = new Color(1f, 0.25f, 0.08f, 0f);
            var half = width * 0.5f;
            var segments = Mathf.Max(1, Mathf.CeilToInt(length / 2f));
            for (var i = 0; i < segments; i++)
            {
                var z0 = -length * 0.5f + length * i / segments;
                var z1 = -length * 0.5f + length * (i + 1) / segments;
                var y0 = GroundHeight(middle + rotation * new Vector3(0f, 0f, z0)) - FloorY + 0.07f;
                var y1 = GroundHeight(middle + rotation * new Vector3(0f, 0f, z1)) - FloorY + 0.07f;
                builder.Quad(new Vector3(-half, y0, z0), new Vector3(-half, y1, z1), new Vector3(half, y1, z1), new Vector3(half, y0, z0), fill);
                builder.Quad(new Vector3(-half, y0, z0), new Vector3(-half, y1, z1), new Vector3(-half + 0.2f, y1, z1), new Vector3(-half + 0.2f, y0, z0), edge);
                builder.Quad(new Vector3(half - 0.2f, y0, z0), new Vector3(half - 0.2f, y1, z1), new Vector3(half, y1, z1), new Vector3(half, y0, z0), edge);

                // Deux rideaux de lumière montent des bords de la bande : on la voit même la tête levée vers le dragon.
                foreach (var x in new[] { -half, half })
                {
                    builder.GradientQuad(new Vector3(x, y0, z0), new Vector3(x, y0 + k_StripGlowHeight, z0),
                                         new Vector3(x, y1 + k_StripGlowHeight, z1), new Vector3(x, y1, z1), glow, clear);
                }
            }

            m_Strip = CreateWarning("Alerte de ruée", builder.ToMesh("Alerte de ruée"));
            m_Strip.SetPositionAndRotation(middle, rotation);

            // Et deux projecteurs, aux bouts de la bande, rougissent la voûte au-dessus d'elle.
            foreach (var z in new[] { -length * 0.35f, length * 0.35f })
            {
                var lightObject = new GameObject("Lueur de l'alerte");
                lightObject.transform.SetParent(m_Strip, false);
                lightObject.transform.SetLocalPositionAndRotation(new Vector3(0f, 0.5f, z), Quaternion.Euler(-90f, 0f, 0f));
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = new Color(1f, 0.25f, 0.08f);
                light.spotAngle = 75f;
                light.range = 18f;
                light.intensity = 3f;
                light.shadows = LightShadows.None;
            }
        }

        // Cercle rouge sur la cible du piqué.
        void ShowCircle(Vector3 center, float radius)
        {
            HideCircle();
            if (m_WarningMaterial == null)
                return;

            var builder = new MeshBuilder();
            const int sides = 40;
            var ring = new Color(1f, 0.15f, 0.05f, 0.85f);
            var fill = new Color(1f, 0.1f, 0.05f, 0.3f);
            for (var i = 0; i < sides; i++)
            {
                var a0 = i * Mathf.PI * 2f / sides;
                var a1 = (i + 1) * Mathf.PI * 2f / sides;
                var p0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0)) * radius;
                var p1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1)) * radius;
                builder.Quad(p0 * 0.88f, p0, p1, p1 * 0.88f, ring);
                builder.Triangle(Vector3.zero, p0 * 0.88f, p1 * 0.88f, fill);
            }

            m_Circle = CreateWarning("Alerte de piqué", builder.ToMesh("Alerte de piqué"));
            m_Circle.position = new Vector3(center.x, GroundHeight(center) + 0.06f, center.z);
        }

        Transform CreateWarning(string objectName, Mesh mesh)
        {
            var go = new GameObject(objectName);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_WarningMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_WarningAge = 0f;
            return go.transform;
        }

        // Les alertes battent, de plus en plus vite.
        void UpdateWarnings(float deltaTime)
        {
            if (m_Strip == null && m_Circle == null)
                return;

            m_WarningAge += deltaTime;
            var beat = 0.92f + 0.08f * Mathf.Sin(m_WarningAge * Mathf.Lerp(10f, 30f, Mathf.Clamp01(m_WarningAge / 1.5f)));
            if (m_Strip != null)
                m_Strip.localScale = new Vector3(beat, 1f, 1f);
            if (m_Circle != null)
                m_Circle.localScale = new Vector3(beat, 1f, beat);
        }

        void HideStrip() => DestroyWarning(ref m_Strip);

        void HideCircle() => DestroyWarning(ref m_Circle);

        static void DestroyWarning(ref Transform warning)
        {
            if (warning == null)
                return;

            var filter = warning.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                Destroy(filter.sharedMesh);
            Destroy(warning.gameObject);
            warning = null;
        }
    }
}
