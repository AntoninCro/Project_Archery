using System.Collections.Generic;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Yo-yo</b> (Tremplin + Tour-aimant ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.17) : une grue au-dessus de
    /// la voie ; son yo-yo de cuivre aimanté, au bout d'un fil, attrape un ennemi, le hisse à 6 m et le fait claquer au sol trois
    /// fois (des dégâts à chaque fois ; en haut, il est « au vol »), puis le lâche.
    /// <list type="bullet">
    /// <item>1 : il le lâche de tout en haut ;</item>
    /// <item>2 : il le lance sur le groupe le plus serré, qu'il renverse (les quilles) ;</item>
    /// <item>3, le <b>lustre</b> : un plateau tournant à six fils de plus sous la flèche de la grue ; sept ennemis montent et
    /// descendent en cadence, puis le lustre s'emballe et les jette tous au loin, contre le décor, où ils s'écrasent.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Tenir un ennemi passe par <see cref="Enemy.Hold"/> ; seuls ceux qu'on peut projeter sont pris (pas les boss, ni les
    /// volants). Chaque fil : un repère en haut (l'ancre), le fil (repère en haut, 1 m à l'échelle 1), le yo-yo.
    /// </remarks>
    public class DuoYoyo : TowerDuo
    {
        [Header("Les fils")]
        [Tooltip("Les ancres des fils (en haut) : la poulie du milieu, puis les six du lustre (niveau 3).")]
        [SerializeField]
        Transform[] m_Anchors = new Transform[0];

        [Tooltip("Les fils (un par ancre ; repère en haut, le fil descend de 1 m à l'échelle 1) : tournés et étirés jusqu'au yo-yo.")]
        [SerializeField]
        Transform[] m_Cables = new Transform[0];

        [Tooltip("Les yo-yos (un par ancre) : ils tournent sur leur axe X en montant et en descendant.")]
        [SerializeField]
        Transform[] m_YoYos = new Transform[0];

        [Tooltip("Le lustre (niveau 3) : il tourne doucement, puis s'emballe pour jeter les ennemis.")]
        [SerializeField]
        Transform m_Hub;

        [Tooltip("Longueur (m) du fil au repos.")]
        [SerializeField]
        float m_RestDrop = 2f;

        [Tooltip("Rayon (m, à plat) autour de l'ancre où le yo-yo va chercher un ennemi, par niveau.")]
        [SerializeField]
        float[] m_GrabRadiusPerLevel = { 5f, 6f, 7f };

        [Tooltip("Hauteur (m) au-dessus du sol où il hisse l'ennemi.")]
        [SerializeField]
        float m_HoistHeight = 6f;

        [Header("Les claques")]
        [SerializeField]
        int m_Slams = 3;

        [Tooltip("Dégâts de chaque claque au sol, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_SlamDamagePerLevel = { 30f, 40f, 45f };

        [Tooltip("Rayon (m) et dégâts du choc pour ceux qui sont autour d'une claque.")]
        [SerializeField]
        float m_SlamRadius = 2.5f;

        [SerializeField]
        float m_SlamSplash = 12f;

        [Tooltip("Temps (s) entre deux prises d'un même fil.")]
        [SerializeField]
        float m_Cooldown = 1.2f;

        [Header("Le lancer (niveau 2)")]
        [SerializeField]
        float m_ThrowRange = 16f;

        [SerializeField]
        float m_LandingRadius = 3f;

        [Header("Le lustre (niveau 3)")]
        [Tooltip("Tours (°/s) du lustre : au repos, et quand il s'emballe.")]
        [SerializeField]
        float m_HubSpin = 25f;

        [SerializeField]
        float m_HubFlingSpin = 520f;

        [Tooltip("Temps (s) où il s'emballe avant de jeter les ennemis.")]
        [SerializeField]
        float m_FlingWindup = 1.2f;

        [Tooltip("Vitesse (m/s) du jet, vers le haut au départ, et la part des PV max que coûte le choc contre le décor.")]
        [SerializeField]
        float m_FlingSpeed = 24f;

        [SerializeField]
        float m_FlingLift = 5f;

        [Range(0f, 1f)]
        [SerializeField]
        float m_FlingSmashShare = 0.45f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_SlamClip;

        [SerializeField]
        AudioClip m_GrabClip;

        [SerializeField]
        AudioClip m_FlingClip;

        enum Phase { Idle, Descend, Up, Down, Wait }

        class Hook
        {
            public Transform Anchor;
            public Transform Cable;
            public Transform YoYo;
            public Enemy Enemy;
            public Phase Phase;
            public float Time;
            public float Duration;
            public Vector3 From;
            public Vector3 To;
            public int Slams;
            public float Hang;
            public float Ready;
            public float Spin;
        }

        readonly List<Hook> m_Hooks = new List<Hook>();
        readonly HashSet<Enemy> m_Taken = new HashSet<Enemy>();
        readonly HashSet<Enemy> m_Thrown = new HashSet<Enemy>();
        float m_HubAngle;
        float m_FlingTime = -1f;

        protected override void Awake()
        {
            base.Awake();
            for (var i = 0; i < m_Anchors.Length; i++)
            {
                if (m_Anchors[i] == null || i >= m_YoYos.Length || m_YoYos[i] == null)
                    continue;
                m_Hooks.Add(new Hook
                {
                    Anchor = m_Anchors[i],
                    Cable = i < m_Cables.Length ? m_Cables[i] : null,
                    YoYo = m_YoYos[i],
                    Ready = i * 0.3f,
                });
            }
        }

        void OnEnable() => Enemy.Landed += OnLanded;

        void OnDisable() => Enemy.Landed -= OnLanded;

        protected override void OnDestroy()
        {
            foreach (var hook in m_Hooks)
                Drop(hook, Vector3.zero);
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt || m_Hooks.Count == 0)
                return;

            var level = Mathf.Max(1, Level);
            UpdateHub(level);
            for (var i = 0; i < m_Hooks.Count; i++)
            {
                var hook = m_Hooks[i];
                if (!hook.YoYo.gameObject.activeInHierarchy)
                {
                    Drop(hook, Vector3.zero);
                    continue;
                }

                Step(hook, level);
                Pose(hook);
            }
        }

        // Le lustre tourne doucement ; quand tous ses ennemis sont en haut, il s'emballe et les jette.
        void UpdateHub(int level)
        {
            if (m_Hub == null || level < 3)
                return;

            var spin = m_HubSpin;
            if (m_FlingTime >= 0f)
            {
                m_FlingTime += Time.deltaTime;
                spin = Mathf.Lerp(m_HubSpin, m_HubFlingSpin, Mathf.Clamp01(m_FlingTime / m_FlingWindup));
                if (m_FlingTime >= m_FlingWindup)
                {
                    m_FlingTime = -1f;
                    Fling();
                }
            }
            else if (AllWaiting())
            {
                m_FlingTime = 0f;
                Sfx.Play(m_FlingClip, m_Hub.position, 1f, 0.8f);
            }

            m_HubAngle = Mathf.Repeat(m_HubAngle + spin * Time.deltaTime * OverchargeRate, 360f);
            m_Hub.localRotation = Quaternion.Euler(0f, m_HubAngle, 0f);
        }

        bool AllWaiting()
        {
            var waiting = 0;
            foreach (var hook in m_Hooks)
            {
                if (hook.Phase == Phase.Wait)
                    waiting++;
                else if (hook.Phase != Phase.Idle && hook.YoYo.gameObject.activeInHierarchy)
                    return false;
            }

            if (waiting == 0)
                return false;

            // Tous ceux qui tiennent quelqu'un sont en haut ; ou l'un attend depuis trop longtemps.
            foreach (var hook in m_Hooks)
            {
                if (hook.Phase == Phase.Wait && hook.Time > 2.5f)
                    return true;
            }

            return waiting >= 2 || CountFree() == 0;
        }

        int CountFree()
        {
            var free = 0;
            foreach (var hook in m_Hooks)
            {
                if (hook.Phase == Phase.Idle && hook.YoYo.gameObject.activeInHierarchy)
                    free++;
            }

            return free;
        }

        void Step(Hook hook, int level)
        {
            hook.Time += Time.deltaTime * OverchargeRate;
            var enemy = hook.Enemy;
            if (hook.Phase != Phase.Idle && hook.Phase != Phase.Descend && (enemy == null || !enemy.IsAlive || !enemy.IsHeld))
            {
                Drop(hook, Vector3.zero);
                return;
            }

            var top = Ground(hook) + m_HoistHeight;
            switch (hook.Phase)
            {
                case Phase.Idle:
                    if (Time.time < hook.Ready || m_FlingTime >= 0f)
                        return;
                    var target = FindTarget(hook, ValueFor(m_GrabRadiusPerLevel, level, 5f));
                    if (target == null)
                    {
                        hook.Ready = Time.time + 0.3f;
                        return;
                    }

                    hook.Enemy = target;
                    m_Taken.Add(target);
                    Begin(hook, Phase.Descend, 0.45f, hook.YoYo.position, target.transform.position + Vector3.up * (TopOf(target) + 0.5f));
                    return;

                case Phase.Descend:
                    if (enemy == null || !enemy.IsAlive)
                    {
                        Drop(hook, Vector3.zero);
                        return;
                    }

                    hook.To = enemy.transform.position + Vector3.up * (TopOf(enemy) + 0.5f);
                    if (hook.Time < hook.Duration)
                        return;
                    if (enemy.IsAirborne || !enemy.Hold(this))
                    {
                        Drop(hook, Vector3.zero);
                        return;
                    }

                    hook.Hang = TopOf(enemy) + 0.5f;
                    hook.Slams = 0;
                    Sfx.Play(m_GrabClip, hook.YoYo.position, 0.8f, Random.Range(0.9f, 1.1f));
                    Begin(hook, Phase.Up, 0.5f, hook.YoYo.position, UnderAnchor(hook, top));
                    return;

                case Phase.Up:
                    if (hook.Time < hook.Duration)
                        return;
                    if (hook.Slams >= m_Slams)
                    {
                        Finish(hook, level);
                        return;
                    }

                    Begin(hook, Phase.Down, 0.18f, hook.YoYo.position, UnderAnchor(hook, Ground(hook) + hook.Hang));
                    return;

                case Phase.Down:
                    if (hook.Time < hook.Duration)
                        return;
                    Slam(hook, level);
                    hook.Slams++;
                    Begin(hook, Phase.Up, 0.38f, hook.YoYo.position, UnderAnchor(hook, top));
                    return;

                case Phase.Wait:
                    hook.From = hook.To = UnderAnchor(hook, top);
                    return;
            }
        }

        void Begin(Hook hook, Phase phase, float duration, Vector3 from, Vector3 to)
        {
            hook.Phase = phase;
            hook.Time = 0f;
            hook.Duration = duration;
            hook.From = from;
            hook.To = to;
        }

        // Le yo-yo et l'ennemi qui pend dessous ; le fil tendu de l'ancre au yo-yo.
        void Pose(Hook hook)
        {
            Vector3 position;
            if (hook.Phase == Phase.Idle)
            {
                position = Vector3.Lerp(hook.YoYo.position, hook.Anchor.position + Vector3.down * m_RestDrop, Time.deltaTime * 4f);
            }
            else
            {
                var t = hook.Duration > 0f ? Mathf.Clamp01(hook.Time / hook.Duration) : 1f;
                t = hook.Phase == Phase.Down ? t * t : Mathf.SmoothStep(0f, 1f, t);
                position = Vector3.Lerp(hook.From, hook.To, t);
                if (hook.Phase == Phase.Wait && m_FlingTime >= 0f && m_Hub != null)
                {
                    // Le lustre s'emballe : les fils s'écartent.
                    var outward = hook.Anchor.position - m_Hub.position;
                    outward.y = 0f;
                    position += outward.normalized * 2.5f * Mathf.Clamp01(m_FlingTime / m_FlingWindup) +
                                Vector3.up * 1.5f * Mathf.Clamp01(m_FlingTime / m_FlingWindup);
                }
            }

            hook.YoYo.position = position;
            hook.Spin += (hook.Phase == Phase.Idle ? 90f : 900f) * Time.deltaTime;
            hook.YoYo.rotation = Quaternion.Euler(hook.Spin, hook.Anchor.eulerAngles.y, 0f);
            if (hook.Enemy != null && hook.Enemy.IsHeld && hook.Phase != Phase.Descend)
            {
                hook.Enemy.transform.position = position - Vector3.up * hook.Hang;
                hook.Enemy.KeepHeld();
            }

            if (hook.Cable != null)
            {
                var from = hook.Cable.position;
                var span = position - from;
                var length = span.magnitude;
                if (length > 1e-3f)
                    hook.Cable.rotation = Quaternion.FromToRotation(Vector3.down, span / length);
                var scale = hook.Cable.localScale;
                scale.y = Mathf.Max(0.05f, length);
                hook.Cable.localScale = scale;
            }
        }

        Vector3 UnderAnchor(Hook hook, float height)
        {
            var anchor = hook.Anchor.position;
            return new Vector3(anchor.x, height, anchor.z);
        }

        float Ground(Hook hook) => transform.position.y;

        Enemy FindTarget(Hook hook, float radius)
        {
            Enemy best = null;
            var bestDistance = float.MaxValue;
            foreach (var enemy in EnemiesNear(hook.Anchor.position, radius))
            {
                if (m_Taken.Contains(enemy) || !enemy.CanBeLaunched)
                    continue;
                var flat = enemy.transform.position - hook.Anchor.position;
                flat.y = 0f;
                if (flat.sqrMagnitude < bestDistance)
                {
                    bestDistance = flat.sqrMagnitude;
                    best = enemy;
                }
            }

            return best;
        }

        // Claque : l'ennemi tenu prend les dégâts ; ceux qui sont autour aussi, un peu, et tombent.
        void Slam(Hook hook, int level)
        {
            var enemy = hook.Enemy;
            var point = enemy.transform.position;
            Hurt(enemy, ValueFor(m_SlamDamagePerLevel, level, 30f), point, Vector3.down);
            foreach (var other in EnemiesNear(point, m_SlamRadius, true, enemy))
            {
                if (Hurt(other, m_SlamSplash, other.Center, other.transform.position - point))
                    KnockDown(other, point, 2f, 3.5f);
            }

            Burst(point + Vector3.up * 0.1f, new Color(0.8f, 0.72f, 0.55f, 1f), 24, 5f, 0.4f, 0.7f, 1.2f, false);
            Sfx.Play(m_SlamClip, point, 0.9f, Random.Range(0.85f, 1.05f));
        }

        // Après les claques : lâché de haut (1), lancé sur un groupe (2), ou en attente du lustre (3).
        void Finish(Hook hook, int level)
        {
            if (level >= 3 && m_Hub != null)
            {
                Begin(hook, Phase.Wait, 0f, hook.YoYo.position, hook.YoYo.position);
                return;
            }

            if (level >= 2 && TryGroup(hook.Enemy, out var landing))
            {
                var start = hook.Enemy.transform.position;
                var to = landing - start;
                var flat = new Vector3(to.x, 0f, to.z);
                var time = Mathf.Clamp(flat.magnitude / 12f, 0.8f, 2f);
                var velocity = flat / time;
                velocity.y = (to.y + 0.5f * -Physics.gravity.y * time * time) / time;
                m_Thrown.Add(hook.Enemy);
                Drop(hook, velocity);
                return;
            }

            Drop(hook, Vector3.zero);
        }

        // Le groupe le plus serré à portée : l'ennemi au sol qui a le plus de voisins à 3 m.
        bool TryGroup(Enemy except, out Vector3 point)
        {
            point = Vector3.zero;
            var best = 0;
            var all = GroundEnemies();
            foreach (var enemy in all)
            {
                if (enemy == except || (enemy.transform.position - transform.position).sqrMagnitude > m_ThrowRange * m_ThrowRange)
                    continue;
                var count = 0;
                foreach (var other in all)
                {
                    if ((other.transform.position - enemy.transform.position).sqrMagnitude < 9f)
                        count++;
                }

                if (count > best)
                {
                    best = count;
                    point = enemy.transform.position;
                }
            }

            return best > 0;
        }

        // Le lustre jette tous ses ennemis : vers l'extérieur et en avant de sa rotation, à toute vitesse, contre le décor.
        void Fling()
        {
            foreach (var hook in m_Hooks)
            {
                if (hook.Phase != Phase.Wait || hook.Enemy == null)
                    continue;
                var outward = hook.Anchor.position - m_Hub.position;
                outward.y = 0f;
                if (outward.sqrMagnitude < 0.01f)
                    outward = ForestwardAt(transform.position);
                outward.Normalize();
                var tangent = Vector3.Cross(Vector3.up, outward);
                var direction = (outward + tangent * 0.6f).normalized;
                var enemy = hook.Enemy;
                Drop(hook, direction * m_FlingSpeed + Vector3.up * m_FlingLift, 0.35f);
                if (enemy != null && enemy.IsAirborne)
                {
                    enemy.Tumble(new Vector3(0f, 720f, 0f));
                    enemy.SetSmash(enemy.Health.Max * m_FlingSmashShare);
                }
            }

            Sfx.Play(m_SlamClip, m_Hub.position, 1f, 0.5f);
        }

        void Drop(Hook hook, Vector3 velocity, float gravity = 1f)
        {
            var enemy = hook.Enemy;
            hook.Enemy = null;
            hook.Phase = Phase.Idle;
            hook.Ready = Time.time + m_Cooldown;
            if (enemy == null)
                return;
            m_Taken.Remove(enemy);
            if (enemy.IsHeld)
                enemy.Release(velocity, false, gravity);
        }

        void OnLanded(Enemy enemy)
        {
            if (enemy == null || !m_Thrown.Remove(enemy))
                return;
            var point = enemy.transform.position;
            foreach (var other in EnemiesNear(point, m_LandingRadius, true, enemy))
            {
                if (Hurt(other, m_SlamSplash, other.Center, other.transform.position - point))
                    KnockDown(other, point, 3f, 4.5f);
            }
        }

        // La hauteur du haut de l'ennemi au-dessus de ses pieds (il pend sous le yo-yo).
        static float TopOf(Enemy enemy)
        {
            var renderer = enemy.GetComponentInChildren<Renderer>();
            var top = renderer != null ? renderer.bounds.max.y - enemy.transform.position.y : 1.8f;
            return Mathf.Clamp(top, 0.8f, 4f);
        }
    }
}
