using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Defense;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le trio <b>Volcan</b> (Feu d'artifice + Tremplin, 1er novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.19) : un petit volcan
    /// sur la voie, couronné de fusées dorées. À chaque éruption, les ennemis sur son cône partent en <b>bombes de lave</b> qui
    /// retombent sur les groupes autour (« Au vol ! » ; à l'impact, ils blessent et le sol brûle). Ta flèche qui traverse le
    /// panache s'enflamme.
    /// <list type="bullet">
    /// <item>1 : une éruption toutes les 6 s, s'il y a quelqu'un sur le cône ;</item>
    /// <item>2 : la lave coule autour du cône (le sol y brûle), et les bombes laissent de la lave là où elles tombent ;</item>
    /// <item>3, <b>l'éruption</b> : en plus, toutes les 20 s, la montagne grandit sous tes yeux, la lumière rougit, et une coulée de
    /// lave descend la voie à contre-sens sur 30 m : elle repousse vers la forêt tout ce qu'elle touche et le brûle.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Une bombe, c'est un ennemi projeté (<see cref="Enemy.Launch"/>) et guidé vers un groupe (<see cref="Enemy.GuideFlight"/>,
    /// comme la rampe de lancement) ; son atterrissage (<see cref="Enemy.Landed"/>) fait l'explosion. Le sol qui brûle est une
    /// flaque en feu (<see cref="GroundPatch"/>). Les boss ne sont jamais projetés ni repoussés (ils prennent les dégâts) ; les
    /// volants pas touchés.
    /// </remarks>
    public class TrioVolcan : TowerTrio
    {
        [Header("La montagne")]
        [Tooltip("La montagne (son repère au sol, au milieu) : elle grandit pendant l'éruption du niveau 3.")]
        [SerializeField]
        Transform m_Mountain;

        [Tooltip("Le cratère : les bombes en partent, le panache en monte.")]
        [SerializeField]
        Transform m_Crater;

        [Tooltip("Le matériau de la lave au sol (les flaques en feu).")]
        [SerializeField]
        Material m_LavaMaterial;

        [Tooltip("Rayon (m) du cône : ceux qui sont dessus partent à l'éruption.")]
        [SerializeField]
        float m_ConeRadius = 4f;

        [Header("Les éruptions")]
        [Tooltip("Temps (s) entre deux éruptions, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 6f, 5f, 4.5f };

        [Tooltip("Vitesse (m/s) vers le haut des bombes, et jusqu'où (m) elles vont chercher un groupe.")]
        [SerializeField]
        float m_BombLift = 14f;

        [SerializeField]
        float m_ThrowRange = 14f;

        [Tooltip("À l'impact d'une bombe : dégâts dans ce rayon (m), par niveau (vague 1, en Normal).")]
        [SerializeField]
        float m_ImpactRadius = 2.5f;

        [SerializeField]
        float[] m_ImpactDamagePerLevel = { 35f, 45f, 55f };

        [Tooltip("Le sol brûle là où tombe une bombe : dégâts par seconde, et durée (s) au niveau 1, puis aux niveaux 2 et 3.")]
        [SerializeField]
        float m_BurnPerSecond = 10f;

        [SerializeField]
        float m_ImpactFireTime = 2f;

        [SerializeField]
        float m_LavaFireTime = 5f;

        [Header("Le panache")]
        [Tooltip("Hauteur (m) du panache au-dessus du cratère, son rayon, et combien de temps (s) il reste après l'éruption : une " +
                 "flèche qui le traverse s'enflamme.")]
        [SerializeField]
        float m_PlumeHeight = 9f;

        [SerializeField]
        float m_PlumeRadius = 2f;

        [SerializeField]
        float m_PlumeTime = 3f;

        [Header("La lave autour du cône (niveau 2)")]
        [SerializeField]
        float m_RingRadius = 5f;

        [Header("L'éruption (niveau 3)")]
        [Tooltip("Temps (s) entre deux grandes éruptions ; il faut au moins quatre ennemis sur la voie, vers la forêt.")]
        [SerializeField]
        float m_EruptionInterval = 20f;

        [Tooltip("La montagne grandit de tant (fois sa taille).")]
        [SerializeField]
        float m_GrowScale = 2.2f;

        [Tooltip("La coulée : sa longueur (m, le long de la voie), sa vitesse (m/s), sa largeur (m).")]
        [SerializeField]
        float m_FlowLength = 30f;

        [SerializeField]
        float m_FlowSpeed = 6f;

        [SerializeField]
        float m_FlowWidth = 6f;

        [SerializeField]
        float m_FlowDamage = 60f;

        [Tooltip("Ceux qu'elle touche sont repoussés vers la forêt (m/s), en l'air.")]
        [SerializeField]
        float m_FlowPush = 9f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_EruptClip;

        [SerializeField]
        AudioClip m_ImpactClip;

        [SerializeField]
        AudioClip m_FlowClip;

        class Bomb
        {
            public Enemy Enemy;
            public Vector3 Target;
            public float NextSpark;
        }

        static readonly Color k_Lava = new Color(1f, 0.45f, 0.1f, 1f);
        static readonly Color k_Ember = new Color(1f, 0.75f, 0.25f, 1f);
        static readonly Color k_Smoke = new Color(0.3f, 0.28f, 0.27f, 1f);
        static readonly Collider[] s_Hits = new Collider[32];

        readonly List<Bomb> m_Bombs = new List<Bomb>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        readonly HashSet<Enemy> m_FlowHit = new HashSet<Enemy>();
        float m_NextEruption;
        float m_PlumeUntil;
        float m_NextRing;
        float m_NextGreat;
        Vector3 m_MountainScale = Vector3.one;
        float m_Grow;
        float m_FlowAt = -1f;
        float m_FlowStart;
        float m_NextPatch;
        float m_NextFlowFx;
        Light m_Glow;

        protected override void Awake()
        {
            base.Awake();
            if (m_Mountain != null)
                m_MountainScale = m_Mountain.localScale;
        }

        void OnEnable() => Enemy.Landed += OnLanded;

        void OnDisable() => Enemy.Landed -= OnLanded;

        protected override void OnDestroy()
        {
            m_Bombs.Clear();
            if (m_Glow != null)
                Destroy(m_Glow.gameObject);
            base.OnDestroy();
        }

        Vector3 Crater => m_Crater != null ? m_Crater.position : transform.position + Vector3.up * 3f;

        protected override void Update()
        {
            base.Update();
            UpdateBombs();
            UpdatePlume();
            UpdateGrowth();
            if (!IsBuilt || IsUnderConstruction)
                return;

            var level = Mathf.Max(1, Level);
            if (level >= 2)
                UpdateRing();
            if (level >= 3)
            {
                UpdateFlow();
                if (m_FlowAt >= 0f)
                    return;
                if (Time.time >= m_NextGreat && TryGreatEruption())
                    return;
            }

            if (Time.time < m_NextEruption || EnemiesNear(transform.position, m_ConeRadius).Count == 0)
                return;

            Erupt(level);
            m_NextEruption = Time.time + ValueFor(m_IntervalPerLevel, level, 6f) / OverchargeRate;
        }

        // ---------------------------------------------------------------- l'éruption, les bombes

        // Ceux qui sont sur le cône partent en bombes de lave, chacun vers un groupe (ou au loin, s'il n'y en a pas).
        void Erupt(int level)
        {
            var crater = Crater;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(transform.position, m_ConeRadius));
            foreach (var enemy in m_Scratch)
            {
                if (enemy.IsBoss || !enemy.CanBeLaunched)
                {
                    Hurt(enemy, ValueFor(m_ImpactDamagePerLevel, level, 35f), enemy.Center, Vector3.up);
                    continue;
                }

                var target = LandingFor(enemy);
                var flat = target - enemy.transform.position;
                flat.y = 0f;
                var velocity = flat.normalized * Mathf.Min(8f, flat.magnitude * 0.4f) + Vector3.up * m_BombLift * Random.Range(0.9f, 1.15f);
                if (!enemy.Launch(velocity, this))
                    continue;

                enemy.Tumble(new Vector3(Random.Range(-360f, 360f), Random.Range(-180f, 180f), Random.Range(-360f, 360f)));
                m_Bombs.Add(new Bomb { Enemy = enemy, Target = target });
                FloatingText.Spawn(enemy.Center + Vector3.up, "Bombe de lave !", k_Lava, 1.1f, 1.2f);
            }

            Burst(crater, k_Lava, 60, 12f, 0.35f, 1.2f, 1f, false);
            Burst(crater + Vector3.up * 1.5f, k_Smoke, 40, 4f, 1.2f, 2.5f, -0.15f, false);
            Sfx.Play(m_EruptClip, crater, 1f, Random.Range(0.8f, 0.95f));
            m_PlumeUntil = Time.time + m_PlumeTime;
            Flash(1.5f);
        }

        // Le groupe le plus serré à portée (sauf sur le cône) ; sinon, un point au hasard autour.
        Vector3 LandingFor(Enemy bomb)
        {
            Vector3? best = null;
            var bestCount = 0;
            var all = GroundEnemies();
            foreach (var enemy in all)
            {
                if (enemy == bomb)
                    continue;
                var flat = enemy.transform.position - transform.position;
                flat.y = 0f;
                if (flat.magnitude < m_ConeRadius || flat.magnitude > m_ThrowRange)
                    continue;

                var count = 0;
                foreach (var other in all)
                {
                    var gap = other.transform.position - enemy.transform.position;
                    gap.y = 0f;
                    if (gap.sqrMagnitude < 9f)
                        count++;
                }

                // Un peu de hasard : les bombes ne tombent pas toutes au même endroit.
                count += Random.Range(0, 2);
                if (count > bestCount)
                {
                    bestCount = count;
                    best = enemy.transform.position;
                }
            }

            if (best.HasValue)
                return best.Value;

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var point = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * m_ThrowRange * 0.6f;
            point.y = GroundY(point, 1.5f);
            return point;
        }

        // En vol : guidées vers leur groupe, une traînée de braises derrière elles.
        void UpdateBombs()
        {
            for (var i = m_Bombs.Count - 1; i >= 0; i--)
            {
                var bomb = m_Bombs[i];
                if (bomb.Enemy == null || !bomb.Enemy.IsAirborne)
                {
                    m_Bombs.RemoveAt(i);
                    continue;
                }

                bomb.Enemy.GuideFlight(bomb.Target, 14f);
                if (Time.time < bomb.NextSpark)
                    continue;
                bomb.NextSpark = Time.time + 0.08f;
                Burst(bomb.Enemy.Center, k_Ember, 6, 1.5f, 0.35f, 0.5f, -0.2f);
            }
        }

        // Une bombe retombée : l'explosion, les dégâts autour, le sol qui brûle.
        void OnLanded(Enemy enemy)
        {
            Bomb found = null;
            foreach (var bomb in m_Bombs)
            {
                if (bomb.Enemy == enemy)
                {
                    found = bomb;
                    break;
                }
            }

            if (found == null)
                return;

            m_Bombs.Remove(found);
            var level = Mathf.Max(1, Level);
            var point = enemy.transform.position;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(point, m_ImpactRadius, true, enemy));
            foreach (var other in m_Scratch)
            {
                if (Hurt(other, ValueFor(m_ImpactDamagePerLevel, level, 35f), point, other.transform.position - point))
                    KnockDown(other, point, 2f, 3f);
            }

            Explosion(point + Vector3.up * 0.3f, m_ImpactRadius);
            Burst(point + Vector3.up * 0.2f, k_Lava, 30, 7f, 0.3f, 0.8f, 1.2f, false);
            Sfx.Play(m_ImpactClip, point, 0.9f, Random.Range(0.85f, 1.05f));
            Lava(point, m_ImpactRadius * 0.8f, level >= 2 ? m_LavaFireTime : m_ImpactFireTime);
        }

        // Une flaque de lave en feu.
        void Lava(Vector3 point, float radius, float time)
        {
            if (m_LavaMaterial == null)
                return;
            point.y = GroundY(point, 1.5f);
            GroundPatch.Spawn(point, radius, time, 1f, m_LavaMaterial, this).Ignite(m_BurnPerSecond * Strength, time);
        }

        // ---------------------------------------------------------------- le panache, la lave autour

        // Les flèches qui traversent le panache s'enflamment (comme au brasier de la tour).
        void UpdatePlume()
        {
            if (Time.time >= m_PlumeUntil)
                return;

            var brazier = Brazier.Instance;
            if (brazier == null)
                return;
            var crater = Crater;
            for (var y = 1f; y <= m_PlumeHeight; y += m_PlumeRadius)
            {
                var count = Physics.OverlapSphereNonAlloc(crater + Vector3.up * y, m_PlumeRadius, s_Hits, Physics.AllLayers,
                                                          QueryTriggerInteraction.Collide);
                for (var i = 0; i < count; i++)
                {
                    var arrow = s_Hits[i].GetComponentInParent<Arrow>();
                    if (arrow != null && !arrow.IsOnFire)
                        brazier.IgniteArrow(arrow);
                }
            }

            if (Random.value < 0.3f)
                Burst(crater + Vector3.up * Random.Range(1f, m_PlumeHeight), k_Smoke, 4, 1.5f, 1f, 1.5f, -0.1f);
        }

        // Niveau 2 : un anneau de lave autour du cône, refait avant qu'il ne s'éteigne.
        void UpdateRing()
        {
            if (Time.time < m_NextRing)
                return;

            m_NextRing = Time.time + 5.5f;
            for (var i = 0; i < 6; i++)
            {
                var angle = i * Mathf.PI / 3f + Random.Range(-0.2f, 0.2f);
                var point = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * m_RingRadius;
                Lava(point, 1.6f, 6f);
            }
        }

        // ---------------------------------------------------------------- la grande éruption (niveau 3)

        bool TryGreatEruption()
        {
            var lane = LaneNearest(transform.position, null, out var along, out _);
            if (lane == null)
                return false;

            var ahead = lane.PointAt(Mathf.Max(0f, along - m_FlowLength * 0.5f), out _);
            if (EnemiesNear(ahead, m_FlowLength * 0.6f).Count < 4)
                return false;

            m_NextGreat = Time.time + m_EruptionInterval / OverchargeRate;
            m_FlowAt = 0f;
            m_FlowStart = along;
            m_FlowHit.Clear();
            m_Grow = Mathf.Max(m_Grow, 0.001f);
            Erupt(3);
            HeadMessages.Show("Éruption !", k_Lava, 2.5f);
            Sfx.Play(m_FlowClip, Crater, 1f, 0.8f);
            return true;
        }

        // La coulée descend la voie vers la forêt : elle repousse et brûle ceux qu'elle touche, et laisse de la lave derrière elle.
        void UpdateFlow()
        {
            if (m_FlowAt < 0f)
                return;

            var lane = LaneNearest(transform.position, null, out _, out _);
            if (lane == null)
            {
                m_FlowAt = -1f;
                return;
            }

            m_FlowAt += m_FlowSpeed * Time.deltaTime;
            var front = lane.PointAt(Mathf.Clamp(m_FlowStart - m_FlowAt, 0f, lane.Length), out var direction);
            front.y = GroundY(front, 1.5f);
            var forest = -direction;
            forest.y = 0f;
            forest = forest.sqrMagnitude > 1e-4f ? forest.normalized : ForestwardAt(front);

            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(front, m_FlowWidth * 0.5f));
            foreach (var enemy in m_Scratch)
            {
                if (!m_FlowHit.Add(enemy))
                    continue;
                if (!Hurt(enemy, m_FlowDamage, front, forest) || enemy.IsBoss)
                    continue;
                enemy.Launch(forest * m_FlowPush + Vector3.up * 4f, this);
            }

            if (Time.time >= m_NextFlowFx)
            {
                m_NextFlowFx = Time.time + 0.1f;
                Burst(front + Vector3.up * 0.3f, k_Lava, 20, 5f, 0.6f, 0.8f, 0.6f, false);
                Burst(front + Vector3.up * 0.8f, k_Smoke, 6, 2f, 1.2f, 1.6f, -0.1f);
            }

            if (Time.time >= m_NextPatch)
            {
                m_NextPatch = Time.time + 2.5f / Mathf.Max(0.1f, m_FlowSpeed);
                Lava(front, m_FlowWidth * 0.45f, 6f);
            }

            if (m_FlowAt >= m_FlowLength || m_FlowStart - m_FlowAt <= 0f)
                m_FlowAt = -1f;
        }

        // La montagne grandit pendant la coulée, puis redescend ; la lumière rougit avec elle.
        void UpdateGrowth()
        {
            var growing = m_FlowAt >= 0f;
            m_Grow = Mathf.MoveTowards(m_Grow, growing ? 1f : 0f, Time.deltaTime / (growing ? 1.5f : 2.5f));
            if (m_Mountain != null)
                m_Mountain.localScale = m_MountainScale * Mathf.Lerp(1f, m_GrowScale, Mathf.SmoothStep(0f, 1f, m_Grow));
            if (m_Grow > 0f || m_Glow != null)
                Flash(m_Grow * 6f);
        }

        // Une lueur rouge au-dessus du cratère, de cette intensité (elle s'éteint seule).
        void Flash(float intensity)
        {
            if (m_Glow == null)
            {
                if (intensity <= 0.01f)
                    return;
                var go = new GameObject("Lueur du volcan");
                go.transform.SetParent(transform, false);
                m_Glow = go.AddComponent<Light>();
                m_Glow.type = LightType.Point;
                m_Glow.color = new Color(1f, 0.35f, 0.12f);
                m_Glow.range = 30f;
                m_Glow.shadows = LightShadows.None;
            }

            m_Glow.transform.position = Crater + Vector3.up * 2f;
            m_Glow.intensity = Mathf.Max(intensity, m_Glow.intensity - Time.deltaTime * 3f);
            if (m_Glow.intensity <= 0.01f && m_Grow <= 0f)
            {
                Destroy(m_Glow.gameObject);
                m_Glow = null;
            }
        }
    }
}
