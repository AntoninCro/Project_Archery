using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.World;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Enemies
{
    // L'ennemi projeté (refonte du tower defense, étape 4, 27 octobre ; Docs/Refonte_TowerDefense.md, partie 6.2) : la base de
    // toutes les constructions qui jouent avec la physique des ennemis (tremplin, catapulte, soufflerie, pendule, geyser, bulle,
    // lance-filet, trappe). Pas de vraie physique de corps (ragdoll) : une cloche calculée, légère pour le Quest et prévisible.
    // Le NavMeshAgent est coupé pendant le vol, puis l'ennemi retombe et revient sur le NavMesh. À l'atterrissage :
    // - une chute de plus de 1,5 m fait des dégâts selon sa hauteur (5 % des PV max par mètre au-delà, la moitié au plus) ;
    // - retomber sur un autre ennemi, ou le percuter en vol, blesse les deux et renverse l'autre (les quilles) ;
    // - contre une falaise ou un mur, il rebondit un peu et reste étourdi 1 s de plus en se relevant ;
    // - dans l'eau, pas de dégâts de chute, mais il est ralenti 2 s (le courant de la rivière ne l'emporte pas encore) ;
    // - retombé sur un bout de NavMesh d'où il ne peut pas rejoindre sa voie (le lit de la rivière), il est replacé au plus près
    //   d'où il le peut (30 octobre : des ennemis retombés d'une bulle restaient plantés dans la rivière) ;
    // - lancé par la rampe de lancement (tremplin niveau 5, 29 octobre), l'atterrissage le tue, où qu'il retombe ; il monte à
    //   370 m et retombe en 5 s : son vol a sa propre gravité, et la rampe le guide vers un groupe (GuideFlight).
    // Les boss ne sont jamais projetés, seulement poussés, moitié moins ; les volants (et les mannequins) non plus.
    // On peut le toucher en vol : en l'air, il est préparé pour les coups du joueur (plus de dégâts et de points, « Au vol ! » ;
    // Enemy.Prepared).
    public partial class Enemy
    {
        const float k_FlightRadius = 0.35f;
        const float k_SafeFall = 1.5f;
        const float k_FallDamagePerMeter = 0.05f;
        const float k_MaxFallDamage = 0.5f;
        const float k_BumpDamage = 0.08f;
        const float k_SelfBumpDamage = 0.04f;
        const float k_LandingStun = 0.5f;
        const float k_WallStun = 1f;
        // La rampe de lancement (tremplin niveau 5, 29 octobre) fait voler 5 s, plus si on la règle ainsi : 6 s avant.
        const float k_MaxFlightTime = 9f;
        const float k_NavMeshSearch = 4f;
        const float k_NavMeshFallback = 15f;

        // Hauteur (m) du centre d'un volant pris au filet au-dessus du sol (son pivot est le centre de son corps).
        const float k_NetHover = 0.7f;

        static readonly RaycastHit[] s_FlightHits = new RaycastHit[16];
        static NavMeshPath s_Path;

        readonly HashSet<Enemy> m_Bumped = new HashSet<Enemy>();

        // D'où il est parti, sur le NavMesh (30 octobre) : en retombant, il doit pouvoir rejoindre ce point à pied.
        Vector3 m_LaunchFrom;
        bool m_HasLaunchFrom;
        bool m_Airborne;
        Vector3 m_FlightVelocity;
        float m_FlightTime;
        float m_FlightPeak;
        float m_StunTimer;
        float m_PendingStun;
        UnityEngine.Object m_LaunchSource;

        // L'atterrissage le tuera (la rampe de lancement, 29 octobre).
        bool m_LethalLanding;

        // Le choc (le Boulet fou au niveau 3, 31 octobre) : lancé à toute vitesse, le premier mur, la première falaise ou le sol
        // qu'il heurte lui coûte ces dégâts.
        float m_SmashDamage;

        // Il tourne sur lui-même en vol (°/s, axes du monde : la glissade sur le verglas, 29 octobre au soir) ; il retombe debout,
        // tourné comme au départ.
        Vector3 m_Spin;
        float m_SpinYaw;

        // La gravité du vol, en part de la vraie : 1, sauf la rampe de lancement (29 octobre), qui fait monter à 370 m et retomber
        // en 5 s ; il y faut douze fois la vraie.
        float m_FlightGravity = 1f;

        // La bulle (lot 4 de l'étape 5, 28 octobre) : il monte doucement, puis retombe quand elle crève.
        float m_FloatTimer;
        float m_FloatHeight;
        float m_FloatSpeed;

        // Le filet : un volant pris tombe au sol et y reste étourdi.
        bool m_Netted;
        float m_NetGround;
        float m_NetFallSpeed;
        float m_NetPeak;
        Boss m_Boss;
        bool m_BossChecked;

        /// <summary>Un ennemi projeté vient de retomber (à sa place sur le NavMesh, ou mort).</summary>
        public static event Action<Enemy> Landed;

        /// <summary>Un ennemi lancé à toute vitesse (<see cref="SetSmash"/>) vient de heurter le décor ou le sol, à ce point.</summary>
        public static event Action<Enemy, Vector3> Smashed;

        /// <summary>En l'air : projeté (tremplin, catapulte…), il retombe en cloche.</summary>
        public bool IsAirborne => m_Airborne;

        /// <summary>Étourdi (il se relève d'une chute, il a heurté un mur) : il ne bouge ni n'attaque.</summary>
        public bool IsStunned => m_StunTimer > 0f && m_State != State.Dead;

        /// <summary>Peut être projeté : un ennemi au sol vivant, pas un boss, ni un volant, ni un mannequin.</summary>
        public bool CanBeLaunched => m_State != State.Dead && m_Agent != null && m_Behaviour == null && !IsBoss;

        /// <summary>Un volant (<see cref="FlyingEnemy"/>) : l'arbalète à répétition le vise d'abord, l'obus du mortier ne l'atteint pas.</summary>
        public bool IsFlyer => m_Behaviour is FlyingEnemy;

        /// <summary>Vitesse actuelle (m/s), en vol compris : les tourelles s'en servent pour viser devant lui.</summary>
        public Vector3 Velocity => m_Airborne ? m_FlightVelocity : CurrentVelocity;

        /// <summary>Un boss : jamais projeté, poussé moitié moins (le vent, l'aimant aussi).</summary>
        public bool IsBoss
        {
            get
            {
                if (!m_BossChecked)
                {
                    m_Boss = GetComponent<Boss>();
                    m_BossChecked = true;
                }

                return m_Boss != null;
            }
        }

        /// <summary>
        /// Projette l'ennemi à cette vitesse (m/s, vers le haut pour une cloche). <paramref name="source"/> reçoit les dégâts de sa
        /// chute (le bilan des bâtiments). Un boss est seulement poussé, moitié moins ; un volant ou un mannequin ne bouge pas.
        /// Déjà en l'air, il repart si le nouvel élan est plus fort (le jonglage). Renvoie vrai s'il vole.
        /// </summary>
        public bool Launch(Vector3 velocity, UnityEngine.Object source) => Launch(velocity, source, false);

        /// <summary>
        /// Comme <see cref="Launch(Vector3, UnityEngine.Object)"/> ; <paramref name="lethal"/> : l'atterrissage le tue, où qu'il
        /// retombe, l'eau comprise (la rampe de lancement, tremplin niveau 5, 29 octobre : « extrêmement drôle », le joueur).
        /// </summary>
        public bool Launch(Vector3 velocity, UnityEngine.Object source, bool lethal) => Launch(velocity, source, lethal, 1f);

        /// <summary>
        /// Comme <see cref="Launch(Vector3, UnityEngine.Object, bool)"/>, avec la gravité du vol en part de la vraie
        /// (<paramref name="gravityScale"/> : la rampe de lancement fait monter très haut et retomber vite, 29 octobre).
        /// </summary>
        public bool Launch(Vector3 velocity, UnityEngine.Object source, bool lethal, float gravityScale)
        {
            if (m_State == State.Dead)
                return false;

            if (IsBoss)
            {
                var flat = velocity;
                flat.y = 0f;
                Push(flat * 0.5f);
                return false;
            }

            if (m_Agent == null || m_Behaviour != null)
                return false;

            if (m_Airborne)
            {
                if (velocity.y > m_FlightVelocity.y)
                {
                    m_FlightVelocity = velocity;
                    m_FlightGravity = Mathf.Max(0.1f, gravityScale);
                    m_LaunchSource = source != null ? source : m_LaunchSource;
                    m_LethalLanding |= lethal;
                }

                return true;
            }

            m_Airborne = true;
            m_LethalLanding = lethal;
            m_SmashDamage = 0f;
            m_Spin = Vector3.zero;
            m_FlightGravity = Mathf.Max(0.1f, gravityScale);
            m_HoldTimer = 0f;
            m_FlightVelocity = velocity;
            m_FlightTime = 0f;
            m_FlightPeak = transform.position.y;
            m_PendingStun = 0f;
            m_LaunchSource = source;
            m_Bumped.Clear();
            m_Push = Vector3.zero;

            m_HasLaunchFrom = m_Agent.enabled && m_Agent.isOnNavMesh;
            if (m_HasLaunchFrom)
                m_LaunchFrom = m_Agent.nextPosition;

            if (m_Agent.enabled)
            {
                if (m_Agent.isOnNavMesh)
                    m_Agent.isStopped = true;
                m_Agent.enabled = false;
            }

            if (m_HasHit && !BehaviourControlsAnimator)
                m_Animator.SetTrigger(k_HitId);
            return true;
        }

        /// <summary>
        /// Guide l'ennemi en vol vers ce point du sol (la rampe de lancement, 29 octobre : il retombe sur un groupe) : sa vitesse à
        /// plat est réglée pour qu'il y arrive quand il sera retombé à sa hauteur, <paramref name="maxFlatSpeed"/> m/s au plus ; à
        /// appeler à chaque image, la cible bouge. Renvoie le temps (s) qu'il lui reste avant d'y être, ou -1 s'il ne vole pas.
        /// </summary>
        public float GuideFlight(Vector3 landing, float maxFlatSpeed)
        {
            if (!m_Airborne || m_FloatTimer > 0f)
                return -1f;

            var gravity = -Physics.gravity.y * m_FlightGravity;
            var vertical = m_FlightVelocity.y;
            var square = vertical * vertical + 2f * gravity * (transform.position.y - landing.y);
            if (gravity <= 0f || square < 0f)
                return -1f;

            var time = (vertical + Mathf.Sqrt(square)) / gravity;
            if (time < 0.05f)
                return time;

            var flat = (landing - transform.position) / time;
            flat.y = 0f;
            flat = Vector3.ClampMagnitude(flat, maxFlatSpeed);
            m_FlightVelocity.x = flat.x;
            m_FlightVelocity.z = flat.z;
            return time;
        }

        /// <summary>En l'air : il tourne sur lui-même (°/s, axes du monde) jusqu'à l'atterrissage, où il se relève (le verglas).</summary>
        public void Tumble(Vector3 degreesPerSecond)
        {
            if (!m_Airborne || m_State == State.Dead)
                return;

            if (m_Spin == Vector3.zero)
                m_SpinYaw = transform.eulerAngles.y;
            m_Spin = degreesPerSecond;
        }

        /// <summary>
        /// En l'air : le premier choc de ce vol (un mur, une falaise, le sol) lui coûte <paramref name="damage"/>, et il le signale
        /// (<see cref="Smashed"/>). Le Boulet fou au niveau 3 (31 octobre) : l'ennemi part à toute vitesse jusqu'au mur d'en face.
        /// </summary>
        public void SetSmash(float damage)
        {
            if (m_Airborne && m_State != State.Dead)
                m_SmashDamage = Mathf.Max(m_SmashDamage, damage);
        }

        // Le choc : ses dégâts, une fois.
        void Smash(Vector3 point, Vector3 direction)
        {
            if (m_SmashDamage <= 0f)
                return;

            var damage = m_SmashDamage;
            m_SmashDamage = 0f;
            if (m_State != State.Dead)
                TakeImpact(damage, point, direction, m_LaunchSource);
            Smashed?.Invoke(this, point);
        }

        /// <summary>En l'air, bulle comprise : son atterrissage le tuera (l'orbe céleste, bulle niveau 5, 29 octobre).</summary>
        public void MarkLethalLanding()
        {
            if (m_Airborne && m_State != State.Dead)
                m_LethalLanding = true;
        }

        // Tenu en l'air par un bâtiment (la singularité, tour-aimant niveau 5, 29 octobre) : c'est lui qui le place, jusqu'à ce
        // qu'il le lâche ; au bout de k_MaxHoldTime, il retombe seul (le bâtiment a pu disparaître).
        const float k_MaxHoldTime = 8f;
        float m_HoldTimer;

        /// <summary>Tenu en l'air par un bâtiment, qui le déplace (<c>transform.position</c>) jusqu'à <see cref="Release"/>.</summary>
        public bool IsHeld => m_Airborne && m_HoldTimer > 0f;

        /// <summary>
        /// Saisit l'ennemi (la singularité) : il ne tombe plus, le bâtiment le déplace lui-même, puis le lâche (<see cref="Release"/>).
        /// Seulement un ennemi qu'on peut projeter, au sol. Renvoie vrai s'il est tenu.
        /// </summary>
        public bool Hold(UnityEngine.Object source)
        {
            if (!CanBeLaunched || m_Airborne || !Launch(Vector3.zero, source))
                return false;

            m_HoldTimer = k_MaxHoldTime;
            return true;
        }

        /// <summary>Lâche un ennemi tenu, à cette vitesse : il retombe en cloche (la règle de la chute).</summary>
        public void Release(Vector3 velocity)
        {
            if (m_HoldTimer <= 0f)
                return;

            m_HoldTimer = 0f;
            m_FlightVelocity = velocity;
            m_FlightPeak = transform.position.y;
        }

        /// <summary>
        /// Tenu, il le reste : le temps de tenue (8 s au plus, une sécurité) repart de zéro. Pour un bâtiment qui le tient longtemps
        /// (les duos, 31 octobre : la boule de l'Avalanche roule jusqu'à 15 s ; le lustre du Yo-yo).
        /// </summary>
        public void KeepHeld()
        {
            if (m_HoldTimer > 0f)
                m_HoldTimer = k_MaxHoldTime;
        }

        /// <summary>
        /// Comme <see cref="Release(Vector3)"/>, avec la pesanteur du vol en part de la vraie et, si <paramref name="lethal"/>, un
        /// atterrissage qui le tue (les duos, 31 octobre : le saut à ski du Toboggan, le lustre du Yo-yo).
        /// </summary>
        public void Release(Vector3 velocity, bool lethal, float gravityScale)
        {
            if (m_HoldTimer <= 0f)
                return;

            Release(velocity);
            m_FlightGravity = Mathf.Max(0.1f, gravityScale);
            m_LethalLanding |= lethal;
        }

        /// <summary>Dans une bulle : il monte doucement, et retombe quand elle crève.</summary>
        public bool IsFloating => m_Airborne && m_FloatTimer > 0f;

        /// <summary>Un volant pris dans un filet : il tombe, puis reste au sol, étourdi.</summary>
        public bool IsNetted => m_Netted && m_State != State.Dead;

        /// <summary>
        /// Enferme l'ennemi dans une bulle (la bulle, lot 4) : il monte de <paramref name="height"/> m à <paramref name="riseSpeed"/>
        /// m/s, flotte, puis la bulle crève au bout de <paramref name="duration"/> s, ou dès qu'il est blessé : il retombe (la
        /// règle de la chute). Seulement un ennemi qu'on peut projeter, au sol. Renvoie vrai s'il flotte.
        /// </summary>
        public bool Float(float height, float duration, float riseSpeed, UnityEngine.Object source)
        {
            if (!CanBeLaunched || m_Airborne || !Launch(Vector3.zero, source))
                return false;

            m_FloatTimer = Mathf.Max(0.1f, duration);
            m_FloatHeight = transform.position.y + height;
            m_FloatSpeed = Mathf.Max(0.1f, riseSpeed);
            m_Health.Damaged -= OnFloatDamaged;
            m_Health.Damaged += OnFloatDamaged;
            return true;
        }

        /// <summary>Crève la bulle : l'ennemi retombe.</summary>
        public void Pop()
        {
            m_Health.Damaged -= OnFloatDamaged;
            if (m_FloatTimer <= 0f)
                return;

            m_FloatTimer = 0f;
            m_FlightVelocity = Vector3.zero;
        }

        void OnFloatDamaged(Health health, DamageInfo info) => Pop();

        /// <summary>
        /// Prend un volant dans un filet (le lance-filet, lot 4) : il tombe au sol (la règle de la chute) et y reste étourdi
        /// <paramref name="duration"/> s, puis il repart. Renvoie vrai s'il est pris.
        /// </summary>
        public bool Net(float duration, UnityEngine.Object source)
        {
            if (m_State == State.Dead || !IsFlyer || m_Netted || duration <= 0f)
                return false;

            m_Netted = true;
            m_NetFallSpeed = 0f;
            m_NetPeak = transform.position.y;
            m_LaunchSource = source;
            m_NetGround = GroundBelow(transform.position) + k_NetHover;
            Stun(duration);
            return true;
        }

        // La hauteur du sol sous ce point (40 m au plus) : ni lui-même, ni un autre ennemi, ni ce que les flèches traversent.
        float GroundBelow(Vector3 from) => TryGroundBelow(from, out var ground) ? ground : from.y;

        bool TryGroundBelow(Vector3 from, out float ground)
        {
            var count = Physics.RaycastNonAlloc(from, Vector3.down, s_FlightHits, 40f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            ground = from.y;
            for (var i = 0; i < count; i++)
            {
                var hit = s_FlightHits[i];
                if (hit.distance >= best || Array.IndexOf(m_Colliders, hit.collider) >= 0 || ArrowIgnore.IsIgnored(hit.collider) ||
                    hit.collider.GetComponentInParent<Enemy>() != null)
                    continue;

                best = hit.distance;
                ground = hit.point.y;
            }

            return best < float.MaxValue;
        }

        /// <summary>
        /// Renvoie un ennemi au sol à ce point (la trappe, lot 4), sur le NavMesh, étourdi <paramref name="stun"/> s. Renvoie
        /// vrai s'il y est.
        /// </summary>
        public bool Teleport(Vector3 point, float stun)
        {
            if (!CanBeLaunched || m_Airborne || !m_Agent.enabled || !NavMesh.SamplePosition(point, out var hit, k_NavMeshSearch, NavMesh.AllAreas))
                return false;

            transform.position = hit.position;
            m_Agent.Warp(hit.position);
            m_Push = Vector3.zero;
            if (m_State == State.Fleeing && m_Agent.isOnNavMesh)
            {
                m_Agent.isStopped = false;
                m_Agent.SetDestination(m_FleePoint);
            }

            Stun(stun);
            return true;
        }

        /// <summary>Étourdit l'ennemi : il s'arrête, sans bouger ni attaquer, pendant ce temps (s).</summary>
        public void Stun(float duration)
        {
            if (m_State == State.Dead || duration <= 0f)
                return;

            m_StunTimer = Mathf.Max(m_StunTimer, duration);
        }

        // En l'air : la cloche, les chocs, l'atterrissage. Vrai tant qu'il vole (le reste de l'Update attend), même mort.
        bool UpdateAirborne(float deltaTime)
        {
            if (!m_Airborne)
                return false;

            // Tenu par un bâtiment : il ne bouge pas de lui-même.
            if (m_HoldTimer > 0f)
            {
                m_HoldTimer -= deltaTime;
                m_FlightTime = 0f;
                m_FlightPeak = Mathf.Max(m_FlightPeak, transform.position.y);
                if (m_HoldTimer <= 0f)
                    m_FlightVelocity = Vector3.zero;
                return true;
            }

            // Dans sa bulle : il monte doucement jusqu'à sa hauteur, puis flotte ; à la fin, la bulle crève et il tombe.
            if (m_FloatTimer > 0f)
            {
                m_FloatTimer -= deltaTime;
                var rising = transform.position;
                rising.y = Mathf.MoveTowards(rising.y, m_FloatHeight, m_FloatSpeed * deltaTime);
                transform.position = rising;
                m_FlightPeak = Mathf.Max(m_FlightPeak, rising.y);
                m_FlightTime = 0f;
                if (m_FloatTimer <= 0f || m_State == State.Dead)
                    Pop();
                return true;
            }

            m_FlightTime += deltaTime;
            m_FlightVelocity += Physics.gravity * (m_FlightGravity * deltaTime);
            var position = transform.position;
            var step = m_FlightVelocity * deltaTime;
            var feet = position + Vector3.up * (k_FlightRadius + 0.05f);

            var overlapped = false;
            if (step.sqrMagnitude > 1e-8f && TrySweep(feet, step, out var hit, out overlapped))
            {
                var other = hit.collider.GetComponentInParent<Enemy>();
                if (other != null)
                {
                    // Un autre ennemi : les quilles. Il est renversé, celui-ci continue, ralenti.
                    if (m_Bumped.Add(other))
                        Bump(other, hit.point);
                }
                else if (hit.normal.y >= 0.6f)
                {
                    Land(hit.point);
                    return true;
                }
                else
                {
                    // Une falaise, un mur : il rebondit un peu, tombe, et se relèvera étourdi. Lancé à toute vitesse, il s'y écrase
                    // (le choc, 31 octobre).
                    Smash(hit.point, m_FlightVelocity.normalized);
                    m_FlightVelocity = new Vector3(-m_FlightVelocity.x * 0.25f, Mathf.Min(m_FlightVelocity.y, 0f), -m_FlightVelocity.z * 0.25f);
                    m_PendingStun = Mathf.Max(m_PendingStun, k_WallStun);
                    return true;
                }
            }

            // Déjà dans le décor au départ du pas (30 octobre : la bulle crevée tout de suite, au ras d'une berge de la rivière) : le
            // balayage ne voit pas ce qu'il touche déjà ; s'il descend, il se pose sur le sol sous lui quand il l'atteint.
            if (overlapped && step.y < 0f && TryGroundBelow(position + Vector3.up * (2f * k_FlightRadius + 0.1f), out var floor) &&
                position.y + step.y <= floor)
            {
                Land(new Vector3(position.x, floor, position.z));
                return true;
            }

            position += step;
            m_FlightPeak = Mathf.Max(m_FlightPeak, position.y);
            transform.position = position;
            if (m_Spin != Vector3.zero)
                transform.Rotate(m_Spin * deltaTime, Space.World);

            // Sécurité : passé à travers le sol, ou trop longtemps en l'air.
            if (m_FlightTime > k_MaxFlightTime)
                Land(position);
            return true;
        }

        // Le premier obstacle sur le trajet des pieds : pas lui-même, ni les déclencheurs, ni ce que les flèches traversent (le corps
        // du joueur, les plaques des emplacements), ni un ennemi mort ou déjà percuté. overlapped : il touchait déjà le décor au
        // départ (voir plus bas).
        bool TrySweep(Vector3 from, Vector3 step, out RaycastHit best, out bool overlapped)
        {
            best = default;
            overlapped = false;
            var distance = step.magnitude;
            var count = Physics.SphereCastNonAlloc(from, k_FlightRadius, step / distance, s_FlightHits, distance,
                                                   Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var hit = s_FlightHits[i];
                if (hit.distance >= bestDistance || Array.IndexOf(m_Colliders, hit.collider) >= 0 || ArrowIgnore.IsIgnored(hit.collider))
                    continue;

                var other = hit.collider.GetComponentInParent<Enemy>();
                if (other != null && (other == this || !other.IsAlive || m_Bumped.Contains(other)))
                    continue;

                // Déjà en contact au départ (distance 0, point nul). Un autre ennemi : le choc, au point le plus proche de lui. Le
                // décor, on n'en tient pas compte ici (30 octobre) : on le prenait pour le sol, et Physics.ClosestPoint ne sait pas
                // faire avec un MeshCollider concave (le sol, les berges) ; des ennemis retombés d'une bulle restaient plantés
                // dans la rivière. UpdateAirborne cherche alors le sol sous lui.
                if (hit.distance <= 0f && hit.point == Vector3.zero)
                {
                    if (other == null)
                    {
                        overlapped = true;
                        continue;
                    }

                    hit.point = CanClosestPoint(hit.collider) ? hit.collider.ClosestPoint(from) : from;
                    hit.normal = Vector3.up;
                }

                bestDistance = hit.distance;
                best = hit;
            }

            return bestDistance < float.MaxValue;
        }

        static bool CanClosestPoint(Collider collider) =>
            collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider || collider is MeshCollider { convex: true };

        void Bump(Enemy other, Vector3 point)
        {
            var direction = m_FlightVelocity;
            direction.y = 0f;
            var flat = direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward;

            other.TakeImpact(k_BumpDamage * other.m_Health.Max, point, flat, m_LaunchSource);
            TakeImpact(k_SelfBumpDamage * m_Health.Max, point, -flat, m_LaunchSource);

            // L'autre est renversé : un petit saut dans le sens du choc (un boss est seulement poussé).
            other.Launch(flat * Mathf.Min(4f, direction.magnitude * 0.5f) + Vector3.up * 3f, m_LaunchSource);
            m_FlightVelocity = new Vector3(m_FlightVelocity.x * 0.6f, m_FlightVelocity.y, m_FlightVelocity.z * 0.6f);
        }

        void TakeImpact(float amount, Vector3 point, Vector3 direction, UnityEngine.Object source)
        {
            if (m_State == State.Dead || amount <= 0f)
                return;

            m_Health.TakeDamage(new DamageInfo
            {
                Amount = amount,
                Zone = HitZone.Body,
                Point = point,
                Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.down,
                Source = source,
            });
        }

        // Au sol : les dégâts de la chute (pas dans l'eau), le retour sur le NavMesh, et il se relève, étourdi un instant.
        void Land(Vector3 ground)
        {
            m_Airborne = false;
            m_FlightGravity = 1f;
            var yaw = m_Spin != Vector3.zero ? m_SpinYaw : transform.eulerAngles.y;
            m_Spin = Vector3.zero;
            transform.SetPositionAndRotation(ground, m_State == State.Dead ? transform.rotation : Quaternion.Euler(0f, yaw, 0f));

            // Lancé à toute vitesse sans avoir heurté de mur : c'est le sol qui l'arrête (le choc).
            Smash(ground, Vector3.down);

            var inWater = SlowWater.InAnyWater(ground + Vector3.up * 0.3f);
            var fall = m_FlightPeak - ground.y;
            if (!inWater && fall > k_SafeFall)
                TakeImpact(Mathf.Min(k_MaxFallDamage, (fall - k_SafeFall) * k_FallDamagePerMeter) * m_Health.Max, ground, Vector3.down, m_LaunchSource);
            else if (inWater)
                Slow(0.5f, 2f);

            if (m_LethalLanding)
            {
                m_LethalLanding = false;
                if (m_State != State.Dead)
                    TakeImpact(m_Health.Current + 1f, ground, Vector3.down, m_LaunchSource);
            }

            if (m_State != State.Dead && m_Agent != null)
            {
                // Retombé hors des voies (la jungle, une terrasse) : il revient au plus près du NavMesh ; trop loin, il est perdu.
                if (NavMesh.SamplePosition(ground, out var navHit, k_NavMeshSearch, NavMesh.AllAreas) ||
                    NavMesh.SamplePosition(ground, out navHit, k_NavMeshFallback, NavMesh.AllAreas))
                {
                    var standing = Reachable(navHit.position);
                    transform.position = standing;
                    m_Agent.enabled = true;
                    m_Agent.Warp(standing);
                    if (m_State == State.Fleeing && m_Agent.isOnNavMesh)
                    {
                        m_Agent.isStopped = false;
                        m_Agent.SetDestination(m_FleePoint);
                    }
                }
                else
                {
                    TakeImpact(m_Health.Current + 1f, ground, Vector3.down, m_LaunchSource);
                }
            }

            if (m_State != State.Dead)
                Stun(k_LandingStun + m_PendingStun);

            m_PendingStun = 0f;
            m_Push = Vector3.zero;
            Landed?.Invoke(this);
        }

        // Retombé sur un bout de NavMesh qui ne mène pas à sa voie (le lit de la rivière, une terrasse ; 30 octobre, des ennemis
        // restaient plantés dans la rivière) : le point le plus proche d'où il peut rejoindre à pied l'endroit d'où il est parti,
        // à 10 m au plus, sinon cet endroit même.
        Vector3 Reachable(Vector3 point)
        {
            if (!m_HasLaunchFrom || CanWalkBack(point))
                return point;

            for (var ring = 1; ring <= 4; ring++)
            {
                var found = false;
                var best = point;
                var bestDistance = float.MaxValue;
                for (var i = 0; i < 8; i++)
                {
                    var angle = i * Mathf.PI * 0.25f;
                    var probe = point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (ring * 2.5f);
                    if (!NavMesh.SamplePosition(probe, out var hit, 2f, NavMesh.AllAreas) || !CanWalkBack(hit.position))
                        continue;

                    var distance = (hit.position - point).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        found = true;
                        best = hit.position;
                        bestDistance = distance;
                    }
                }

                if (found)
                    return best;
            }

            return NavMesh.SamplePosition(m_LaunchFrom, out var back, 2f, NavMesh.AllAreas) ? back.position : point;
        }

        bool CanWalkBack(Vector3 point)
        {
            s_Path ??= new NavMeshPath();
            return NavMesh.CalculatePath(point, m_LaunchFrom, NavMesh.AllAreas, s_Path) && s_Path.status == NavMeshPathStatus.PathComplete;
        }

        // Étourdi : il reste sur place (la poussée le déplace quand même). Vrai tant qu'il l'est.
        bool UpdateStun(float deltaTime)
        {
            if (m_StunTimer <= 0f)
            {
                m_Netted = false;
                return false;
            }

            m_StunTimer -= deltaTime;
            if (m_Netted)
                FallNetted(deltaTime);
            Stop();
            return true;
        }

        // Un volant pris dans le filet tombe, de plus en plus vite, jusqu'au sol ; sa chute le blesse en arrivant.
        void FallNetted(float deltaTime)
        {
            var position = transform.position;
            if (position.y <= m_NetGround + 0.01f)
                return;

            m_NetFallSpeed += -Physics.gravity.y * deltaTime;
            position.y = Mathf.MoveTowards(position.y, m_NetGround, m_NetFallSpeed * deltaTime);
            transform.position = position;
            if (position.y > m_NetGround + 0.01f)
                return;

            var fall = m_NetPeak - m_NetGround;
            if (fall > k_SafeFall)
                TakeImpact(Mathf.Min(k_MaxFallDamage, (fall - k_SafeFall) * k_FallDamagePerMeter) * m_Health.Max, position, Vector3.down, m_LaunchSource);
        }
    }
}
