using Archery.Bows;
using Archery.Core;
using Archery.Defense;
using Archery.Player;
using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Ennemi volant (GDD, section 10) : il tourne en l'air autour du joueur, puis attaque en piqué.
    /// Avant chaque piqué, il fait du sur-place un instant en criant : c'est le moment de le viser.
    /// Après le piqué, touché ou raté, il remonte et recommence. À sa mort, il tombe au sol.
    /// </summary>
    /// <remarks>
    /// Il n'utilise pas le NavMesh et ne se cogne à rien : il vole au-dessus de la clairière.
    /// Ses réglages de base (PV, vitesse, dégâts, temps entre deux piqués, durée du sur-place) viennent
    /// de son <see cref="EnemyDefinition"/>.
    /// Son pivot est le centre de son corps : un modèle dont le pivot est au sol (corps en l'air) se place
    /// plus bas, en enfant, et le réglage Corpse Height indique ce décalage pour la chute.
    /// </remarks>
    public class FlyingEnemy : EnemyBehaviour
    {
        enum Phase
        {
            Circling,
            Hovering,
            Diving,
            Recovering,
            Fleeing,
            Falling,
            Landed,
        }

        [Header("Vol")]
        [Tooltip("Hauteur (m) de vol au-dessus des pieds du joueur.")]
        [SerializeField]
        float m_CruiseHeight = 7f;

        [Tooltip("Rayon (m) du cercle autour du joueur.")]
        [SerializeField]
        float m_CircleRadius = 11f;

        [Tooltip("Variation au hasard (m) de la hauteur et du rayon, pour que les volants ne volent pas tous au même endroit.")]
        [SerializeField]
        float m_Variation = 3f;

        [Tooltip("Vitesse maximale (°/s) à laquelle il tourne.")]
        [SerializeField]
        float m_TurnRate = 140f;

        [Tooltip("Accélération (m/s²).")]
        [SerializeField]
        float m_Acceleration = 8f;

        [Tooltip("Hauteur (m) à laquelle il apparaît au-dessus du point d'apparition.")]
        [SerializeField]
        float m_SpawnHeight = 4f;

        [Header("Piqué")]
        [Tooltip("Vitesse du piqué, par rapport au vol normal.")]
        [SerializeField]
        float m_DiveSpeedMultiplier = 1.8f;

        [Tooltip("Vitesse maximale (°/s) à laquelle il corrige sa trajectoire pendant le piqué : on peut l'esquiver.")]
        [SerializeField]
        float m_DiveTurnRate = 70f;

        [Tooltip("Distance (m) à la tête du joueur à laquelle le piqué touche.")]
        [SerializeField]
        float m_HitRadius = 1f;

        [Tooltip("Distance maximale (m) au joueur pour commencer un piqué.")]
        [SerializeField]
        float m_MaxDiveDistance = 25f;

        [Tooltip("Durée (s) de la remontée après un piqué.")]
        [SerializeField]
        float m_RecoverTime = 1.5f;

        [Header("Retours")]
        [Tooltip("Optionnel : ailes à faire battre.")]
        [SerializeField]
        WingFlap m_Wings;

        [Tooltip("Optionnel : cri juste avant le piqué.")]
        [SerializeField]
        AudioClip m_ScreechClip;

        [Header("Mort")]
        [Tooltip("Il tournoie en tombant. À décocher pour un modèle qui a sa propre animation de mort : il tombe alors bien droit.")]
        [SerializeField]
        bool m_SpinWhileFalling = true;

        [Tooltip("Hauteur (m) à laquelle son pivot s'arrête au-dessus du sol. 0 pour des formes simples ; pour un modèle " +
                 "placé plus bas que le pivot, ce décalage : le modèle touche alors le sol, et son animation de mort le couche.")]
        [SerializeField]
        float m_CorpseHeight;

        Phase m_Phase = Phase.Circling;
        Vector3 m_Velocity;
        float m_Angle;
        float m_Direction;
        float m_Radius;
        float m_Height;
        float m_PhaseTimer;
        float m_DiveTimer;
        float m_Bank;
        Vector3 m_FleePoint;
        Vector3 m_FleeStart;

        public override Vector3 Velocity => m_Velocity;
        public override Vector3 SpawnOffset => Vector3.up * m_SpawnHeight;

        protected override void Awake()
        {
            base.Awake();

            // Chaque volant a son cercle : sens, rayon, hauteur et premier piqué tirés au hasard.
            m_Direction = Random.value < 0.5f ? -1f : 1f;
            m_Radius = m_CircleRadius + Random.Range(-m_Variation, m_Variation);
            m_Height = m_CruiseHeight + Random.Range(-m_Variation * 0.5f, m_Variation * 0.5f);
            m_Angle = Random.Range(0f, Mathf.PI * 2f);
            m_Velocity = transform.forward * 2f;
        }

        void Start() => m_DiveTimer = NextDiveDelay() + 2f;

        float NextDiveDelay() => Enemy.Definition.attackInterval * Random.Range(0.7f, 1.3f);

        public override void Tick(float deltaTime)
        {
            var player = PlayerHealth.Instance;
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            var hasPlayer = player != null && player.IsAlive && head != null;

            // Sans joueur (mort), il tourne au-dessus de la tour.
            var feet = hasPlayer ? player.BodyPosition : TowerTop();
            var headPosition = hasPlayer ? head.position : feet + Vector3.up * 1.6f;
            var speed = Enemy.CurrentSpeed;

            switch (m_Phase)
            {
                case Phase.Circling:
                    m_Angle += m_Direction * speed / Mathf.Max(2f, m_Radius) * deltaTime;
                    var circlePoint = feet + new Vector3(Mathf.Cos(m_Angle), 0f, Mathf.Sin(m_Angle)) * m_Radius + Vector3.up * m_Height;
                    Steer(circlePoint, speed, m_TurnRate, deltaTime);

                    m_DiveTimer -= deltaTime;
                    if (hasPlayer && m_DiveTimer <= 0f)
                        TryStartDive(headPosition);
                    break;

                case Phase.Hovering:
                    // Sur place, tourné vers le joueur : il prévient avant de plonger.
                    m_Velocity = Vector3.MoveTowards(m_Velocity, Vector3.zero, m_Acceleration * 2f * deltaTime);
                    Face(headPosition - transform.position, deltaTime);
                    m_PhaseTimer -= deltaTime;
                    if (m_PhaseTimer <= 0f)
                        SetPhase(Phase.Diving, 3f);
                    break;

                case Phase.Diving:
                    UpdateDive(player, headPosition, speed, deltaTime);
                    break;

                case Phase.Recovering:
                    // Il remonte en s'éloignant du joueur.
                    var away = transform.position - feet;
                    away.y = 0f;
                    away = away.sqrMagnitude > 0.01f ? away.normalized : transform.forward;
                    Steer(transform.position + away * 6f + Vector3.up * 5f, speed, m_TurnRate, deltaTime);
                    m_PhaseTimer -= deltaTime;
                    if (m_PhaseTimer <= 0f)
                    {
                        m_Angle = Mathf.Atan2(away.z, away.x);
                        m_DiveTimer = NextDiveDelay();
                        SetPhase(Phase.Circling, 0f);
                    }

                    break;
            }

            KeepAboveGround(feet);
            Move(deltaTime);
            UpdateWings(1f, m_Phase == Phase.Hovering ? 1.6f : 1f);
        }

        void TryStartDive(Vector3 headPosition)
        {
            var toHead = headPosition - transform.position;
            if (toHead.magnitude > m_MaxDiveDistance || !HasClearPath(headPosition))
            {
                m_DiveTimer = 1f;
                return;
            }

            SetPhase(Phase.Hovering, Mathf.Max(0.2f, Enemy.Definition.attackWindup));
            Enemy.PlayAttackAnimation();
            Sfx.Play(m_ScreechClip, transform.position, 1f, Random.Range(0.9f, 1.1f));
        }

        // Piqué vers la tête du joueur, en corrigeant lentement la trajectoire : un pas de côté suffit pour l'esquiver.
        void UpdateDive(PlayerHealth player, Vector3 headPosition, float speed, float deltaTime)
        {
            Steer(headPosition, speed * m_DiveSpeedMultiplier, m_DiveTurnRate, deltaTime);

            var toHead = headPosition - transform.position;
            if (player != null && player.IsAlive && toHead.magnitude <= m_HitRadius)
            {
                Enemy.DealDamage(player.Health, headPosition);
                SetPhase(Phase.Recovering, m_RecoverTime);
                return;
            }

            // Raté : il a dépassé le joueur, ou il plonge depuis trop longtemps.
            m_PhaseTimer -= deltaTime;
            var passed = Vector3.Dot(m_Velocity, toHead) < 0f && toHead.magnitude > m_HitRadius;
            if (passed || m_PhaseTimer <= 0f)
                SetPhase(Phase.Recovering, m_RecoverTime);
        }

        void SetPhase(Phase phase, float duration)
        {
            m_Phase = phase;
            m_PhaseTimer = duration;
        }

        // Les plantes et les branches ne comptent pas : seul un vrai obstacle (tour, rocher…) empêche le piqué.
        bool HasClearPath(Vector3 target)
        {
            var from = transform.position;
            var direction = target - from;
            var distance = direction.magnitude;
            if (distance < 0.5f)
                return true;

            var hits = Physics.RaycastAll(from, direction / distance, distance - 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider == null || ArrowIgnore.IsIgnored(hit.collider) || hit.collider.GetComponentInParent<Enemy>() != null)
                    continue;
                return false;
            }

            return true;
        }

        void Steer(Vector3 target, float speed, float turnRate, float deltaTime)
        {
            var toTarget = target - transform.position;
            var desired = toTarget.sqrMagnitude > 1e-4f ? toTarget.normalized * speed : Vector3.zero;
            var previous = m_Velocity;
            m_Velocity = Vector3.RotateTowards(m_Velocity, desired, turnRate * Mathf.Deg2Rad * deltaTime, m_Acceleration * deltaTime);
            if (m_Velocity.sqrMagnitude < 0.01f && desired.sqrMagnitude > 0.01f)
                m_Velocity = desired.normalized * 0.2f;

            // Il s'incline dans les virages.
            var turn = Vector3.SignedAngle(Flat(previous), Flat(m_Velocity), Vector3.up) / Mathf.Max(deltaTime, 1e-4f);
            m_Bank = Mathf.Lerp(m_Bank, Mathf.Clamp(-turn * 0.25f, -40f, 40f), 1f - Mathf.Exp(-deltaTime * 4f));
        }

        void Move(float deltaTime)
        {
            transform.position += m_Velocity * deltaTime;
            if (m_Velocity.sqrMagnitude > 0.04f && m_Phase != Phase.Hovering)
                transform.rotation = Quaternion.LookRotation(m_Velocity) * Quaternion.Euler(0f, 0f, m_Bank);
        }

        void Face(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude < 1e-4f)
                return;
            var target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, 360f * deltaTime);
        }

        // En vol normal, jamais plus bas que 2 m au-dessus des pieds du joueur.
        void KeepAboveGround(Vector3 feet)
        {
            if (m_Phase == Phase.Diving || m_Phase == Phase.Hovering)
                return;

            var minimum = feet.y + 2f;
            if (transform.position.y < minimum && m_Velocity.y < 1f)
                m_Velocity.y = Mathf.MoveTowards(m_Velocity.y, 2f, m_Acceleration * Time.deltaTime);
        }

        void UpdateWings(float intensity, float rate)
        {
            if (m_Wings == null)
                return;

            var diving = m_Phase == Phase.Diving;
            m_Wings.Intensity = diving ? 0.15f : intensity;
            m_Wings.RateMultiplier = rate;
        }

        static Vector3 Flat(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);

        static Vector3 TowerTop()
        {
            var tower = Tower.Instance;
            if (tower == null)
                return Vector3.zero;

            // TopHeight est une hauteur dans le monde (le haut des colliders de la tour).
            var top = tower.transform.position;
            top.y = tower.TopHeight;
            return top;
        }

        // --- Fuite et mort ---

        public override void BeginFlee(Vector3 exitPoint)
        {
            m_Phase = Phase.Fleeing;
            m_FleePoint = exitPoint;
            m_FleeStart = transform.position;
            m_PhaseTimer = 0f;
        }

        // Il s'envole au loin, puis disparaît.
        public override void TickFlee(float deltaTime)
        {
            var target = m_FleePoint + Vector3.up * 20f;
            Steer(target, Enemy.CurrentSpeed * 1.5f, m_TurnRate, deltaTime);
            Move(deltaTime);
            UpdateWings(1f, 1.3f);

            m_PhaseTimer += deltaTime;
            if (Vector3.Distance(transform.position, m_FleeStart) > 40f || Vector3.Distance(transform.position, target) < 3f || m_PhaseTimer > 12f)
                Enemy.Despawn();
        }

        public override void OnDied()
        {
            m_Phase = Phase.Falling;
            UpdateWings(0f, 1f);
        }

        // Il tombe jusqu'au sol, en tournoyant, ou bien droit pour laisser jouer son animation de mort.
        public override bool TickCorpse(float deltaTime)
        {
            if (m_Phase == Phase.Landed)
                return true;

            // Mort tout près du sol (pendant un piqué) : il se pose tout de suite.
            if (m_CorpseHeight > 0f && TryFindObstacle(transform.position, Vector3.down, m_CorpseHeight, out var below))
            {
                Land(below + Vector3.up * m_CorpseHeight);
                return true;
            }

            // Le bas du corps (le pied du modèle) avance avec le pivot ; il s'arrête au premier obstacle.
            m_Velocity += Physics.gravity * deltaTime;
            var step = m_Velocity * deltaTime;
            var bottom = transform.position + Vector3.down * m_CorpseHeight;
            if (TryFindObstacle(bottom, step.normalized, step.magnitude + 0.2f, out var point))
            {
                Land(point + Vector3.up * m_CorpseHeight);
                return true;
            }

            transform.position += step;
            if (m_SpinWhileFalling)
                transform.Rotate(new Vector3(200f, 0f, 320f) * deltaTime, Space.Self);
            else
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Upright(), 240f * deltaTime);

            // Sécurité : sous le sol de la carte, il s'arrête.
            if (transform.position.y < -50f)
                m_Phase = Phase.Landed;
            return true;
        }

        void Land(Vector3 position)
        {
            transform.position = position;
            if (!m_SpinWhileFalling)
                transform.rotation = Upright();
            m_Velocity = Vector3.zero;
            m_Phase = Phase.Landed;
        }

        // Droit, tourné dans le sens de son vol.
        Quaternion Upright()
        {
            var forward = Flat(transform.forward);
            return forward.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(forward) : transform.rotation;
        }

        // Le premier obstacle sur ce trajet, sans compter les ennemis ni le joueur.
        static bool TryFindObstacle(Vector3 from, Vector3 direction, float distance, out Vector3 point)
        {
            point = default;
            var best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(from, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance >= best || ArrowIgnore.IsIgnored(hit.collider) || hit.collider.GetComponentInParent<Enemy>() != null)
                    continue;

                best = hit.distance;
                point = hit.point;
            }

            return best < float.MaxValue;
        }
    }
}
