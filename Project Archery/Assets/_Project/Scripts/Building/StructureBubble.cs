using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Bulle (refonte du tower defense, étape 5, lot 4 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 6.1 et 6.2) :
    /// au bord de la voie. Elle enferme l'ennemi au sol le plus proche dans une bulle qui monte doucement (4 / 5 / 6 m) et
    /// flotte : la crever d'une flèche (ou de n'importe quel coup) le fait retomber, et la chute le blesse ; sinon elle crève
    /// seule au bout de 5 / 6 / 7 s. Depuis le 29 octobre (deuxième brainstorm, partie 12.6), cinq niveaux :
    /// <list type="bullet">
    /// <item>2 : deux bulles à la fois ;</item>
    /// <item>3, la <b>bulle géante</b> : chacune enferme un petit groupe (jusqu'à 3) ;</item>
    /// <item>4 : ta flèche qui crève une bulle rebondit vers la suivante, et la crève aussi, de bulle en bulle ;</item>
    /// <item>5, l'<b>orbe céleste</b> : toutes les 15 s, une bulle immense emporte tout un groupe très haut (30 m) ; crevée par
    /// ta flèche (ou au bout de 6 s), ils s'écrasent et meurent, comme avec la rampe de lancement.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// L'ennemi flotte par <c>Enemy.Float</c> (il compte comme en l'air : le tir au vol rapporte plus). La bulle elle-même
    /// n'est qu'une sphère sans collider au matériau <c>Bubble Material</c>, qui suit ceux qu'elle enferme : les flèches la
    /// traversent et les touchent, ce qui la crève (pour tout le groupe).
    /// </remarks>
    public class StructureBubble : Structure
    {
        [Tooltip("Portée (m), par niveau.")]
        [SerializeField]
        float[] m_RangePerLevel = { 10f, 11f, 12f };

        [Tooltip("Temps (s) entre deux bulles, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 7f, 6f, 5f };

        [Tooltip("Hauteur (m) à laquelle monte la bulle, par niveau.")]
        [SerializeField]
        float[] m_HeightPerLevel = { 4f, 5f, 6f };

        [Tooltip("Durée (s) avant que la bulle crève seule, par niveau.")]
        [SerializeField]
        float[] m_DurationPerLevel = { 5f, 6f, 7f };

        [Tooltip("Bulles lancées à la fois, par niveau.")]
        [SerializeField]
        int[] m_BubblesPerLevel = { 1, 2, 2, 2, 2 };

        [Tooltip("Vitesse (m/s) de montée.")]
        [SerializeField]
        float m_RiseSpeed = 1.5f;

        [Tooltip("Diamètre (m) de la bulle.")]
        [SerializeField]
        float m_BubbleSize = 2f;

        [SerializeField]
        Material m_BubbleMaterial;

        [SerializeField]
        AudioClip m_BlowClip;

        [SerializeField]
        AudioClip m_PopClip;

        [Header("Bulle géante (niveau 3)")]
        [Tooltip("Ennemis qu'enferme une bulle géante, au plus.")]
        [SerializeField]
        int m_GroupMax = 3;

        [Tooltip("Distance (m) au premier ennemi des autres qu'elle enferme.")]
        [SerializeField]
        float m_GroupRadius = 2.5f;

        [Header("Rebond (niveau 4)")]
        [Tooltip("Distance (m) au plus d'une bulle à la suivante pour le rebond de la flèche.")]
        [SerializeField]
        float m_BounceRange = 20f;

        [SerializeField]
        Color m_BounceColor = new Color(0.75f, 0.95f, 1f);

        [Header("Orbe céleste (niveau 5)")]
        [Tooltip("Temps (s) entre deux orbes.")]
        [SerializeField]
        float m_OrbInterval = 15f;

        [Tooltip("Hauteur (m) où monte l'orbe.")]
        [SerializeField]
        float m_OrbHeight = 30f;

        [Tooltip("Vitesse (m/s) de montée de l'orbe.")]
        [SerializeField]
        float m_OrbRise = 8f;

        [Tooltip("Durée (s) avant que l'orbe crève seule.")]
        [SerializeField]
        float m_OrbDuration = 6f;

        [Tooltip("Rayon (m) du groupe qu'emporte l'orbe, autour du plus serré.")]
        [SerializeField]
        float m_OrbRadius = 4f;

        [Tooltip("Ennemis qu'emporte l'orbe, au plus.")]
        [SerializeField]
        int m_OrbMax = 8;

        class Bubble
        {
            public Transform Body;
            public readonly List<Enemy> Targets = new List<Enemy>();
            public float Size;
            public float Grow;
            public float Burst;
        }

        readonly List<Bubble> m_Bubbles = new List<Bubble>();
        readonly List<Enemy> m_Group = new List<Enemy>();
        readonly List<Enemy> m_Candidates = new List<Enemy>();
        readonly ArrowWatch m_Arrows = new ArrowWatch();
        readonly HashSet<Bubble> m_Visited = new HashSet<Bubble>();
        float m_Cooldown = 2f;
        float m_OrbTimer = 6f;

        void OnEnable()
        {
            Arrow.AnyHit += OnArrowHit;
            m_Arrows.Begin();
        }

        void OnDisable()
        {
            Arrow.AnyHit -= OnArrowHit;
            m_Arrows.End();
        }

        protected override void Update()
        {
            base.Update();
            UpdateBubbles(Time.deltaTime);
            WatchArrows();
            if (!IsBuilt)
                return;

            var level = Mathf.Max(1, Level);
            if (level >= 5)
            {
                m_OrbTimer -= Time.deltaTime * OverchargeRate;
                if (m_OrbTimer <= 0f)
                    m_OrbTimer = Orb() ? m_OrbInterval : 0.5f;
            }

            m_Cooldown -= Time.deltaTime * OverchargeRate;
            if (m_Cooldown > 0f)
                return;

            m_Cooldown = Blow(level) ? ValueFor(m_IntervalPerLevel, level, 7f) : 0.4f;
        }

        float Range => ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 10f) * RangeScale * OverchargeRange;

        // Une bulle sur chacun des ennemis au sol les plus proches (au plus le nombre du niveau) ; au niveau 3, chacune enferme
        // aussi ses voisins. Vrai si au moins une est partie.
        bool Blow(int level)
        {
            var range = Range;
            var count = ValueFor(m_BubblesPerLevel, level, 1);
            var height = ValueFor(m_HeightPerLevel, level, 4f);
            var duration = ValueFor(m_DurationPerLevel, level, 5f);
            var blown = 0;
            for (var i = 0; i < count; i++)
            {
                var target = Nearest(transform.position, range);
                if (target == null)
                    break;

                m_Group.Clear();
                m_Group.Add(target);
                if (level >= 3)
                    Gather(target.transform.position, m_GroupRadius, m_GroupMax, m_Group);
                if (!Enclose(m_Group, height, duration, m_RiseSpeed, false, level >= 3 ? m_BubbleSize * 1.7f : m_BubbleSize))
                    break;
                blown++;
            }

            if (blown > 0)
                Sfx.Play(m_BlowClip, transform.position + Vector3.up, 0.8f, Random.Range(0.95f, 1.15f));
            return blown > 0;
        }

        // L'orbe céleste : le groupe au sol le plus serré à portée part très haut ; à sa chute, ils meurent.
        bool Orb()
        {
            var range = Range * 1.5f;
            Enemy center = null;
            var best = 0;

            // Une copie : Gather relit la liste partagée des ennemis (AliveEnemies), qu'on ne peut pas parcourir en même temps (le
            // jeu plantait, 29 octobre).
            m_Candidates.Clear();
            m_Candidates.AddRange(AliveEnemies());
            foreach (var enemy in m_Candidates)
            {
                if (!CanFloat(enemy) || (enemy.transform.position - transform.position).sqrMagnitude > range * range)
                    continue;

                m_Group.Clear();
                Gather(enemy.transform.position, m_OrbRadius, m_OrbMax, m_Group);
                if (m_Group.Count > best)
                {
                    best = m_Group.Count;
                    center = enemy;
                }
            }

            if (center == null)
                return false;

            m_Group.Clear();
            Gather(center.transform.position, m_OrbRadius, m_OrbMax, m_Group);
            if (!Enclose(m_Group, m_OrbHeight, m_OrbDuration, m_OrbRise, true, m_OrbRadius * 2f + 2f))
                return false;

            Sfx.Play(m_BlowClip, center.Center, 1f, 0.6f);
            return true;
        }

        // Les ennemis au sol libres autour de ce point, jusqu'à max (ceux de la liste n'y sont pas ajoutés deux fois).
        void Gather(Vector3 point, float radius, int max, List<Enemy> group)
        {
            foreach (var enemy in AliveEnemies())
            {
                if (group.Count >= max)
                    return;
                if (CanFloat(enemy) && !group.Contains(enemy) && (enemy.transform.position - point).sqrMagnitude <= radius * radius)
                    group.Add(enemy);
            }
        }

        // Une bulle autour du groupe : chacun flotte (Enemy.Float) ; lethal : leur chute les tuera.
        bool Enclose(List<Enemy> group, float height, float duration, float rise, bool lethal, float size)
        {
            var bubble = new Bubble { Size = size };
            foreach (var enemy in group)
            {
                if (!enemy.Float(height, duration, rise, this))
                    continue;
                if (lethal)
                    enemy.MarkLethalLanding();
                bubble.Targets.Add(enemy);
            }

            if (bubble.Targets.Count == 0)
                return false;

            bubble.Body = SpawnBubble(Centroid(bubble));
            m_Bubbles.Add(bubble);
            return true;
        }

        static bool CanFloat(Enemy enemy) => enemy != null && enemy.CanBeLaunched && !enemy.IsAirborne;

        Enemy Nearest(Vector3 point, float range)
        {
            Enemy best = null;
            var bestDistance = range * range;
            foreach (var enemy in AliveEnemies())
            {
                if (!CanFloat(enemy))
                    continue;

                var distance = (enemy.transform.position - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }

            return best;
        }

        Transform SpawnBubble(Vector3 position)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Bulle";
            Destroy(body.GetComponent<Collider>());
            body.transform.position = position;
            body.transform.localScale = Vector3.one * 0.1f;
            var renderer = body.GetComponent<Renderer>();
            if (m_BubbleMaterial != null)
                renderer.sharedMaterial = m_BubbleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return body.transform;
        }

        static Vector3 Centroid(Bubble bubble)
        {
            var sum = Vector3.zero;
            var count = 0;
            foreach (var enemy in bubble.Targets)
            {
                if (enemy == null)
                    continue;
                sum += enemy.Center;
                count++;
            }

            return count > 0 ? sum / count : bubble.Body != null ? bubble.Body.position : Vector3.zero;
        }

        static bool IsFloating(Bubble bubble)
        {
            foreach (var enemy in bubble.Targets)
            {
                if (enemy == null || !enemy.IsFloating)
                    return false;
            }

            return true;
        }

        // La bulle grandit autour du groupe et le suit ; dès que l'un d'eux retombe (elle a crevé), tous retombent, et elle gonfle
        // d'un coup et disparaît.
        void UpdateBubbles(float deltaTime)
        {
            for (var i = m_Bubbles.Count - 1; i >= 0; i--)
            {
                var bubble = m_Bubbles[i];
                if (bubble.Body == null)
                {
                    m_Bubbles.RemoveAt(i);
                    continue;
                }

                if (bubble.Burst <= 0f && !IsFloating(bubble))
                    Burst(bubble);

                if (bubble.Burst > 0f)
                {
                    bubble.Burst += deltaTime;
                    bubble.Body.localScale = Vector3.one * bubble.Size * (1f + bubble.Burst * 3f);
                    if (bubble.Burst >= 0.12f)
                    {
                        Destroy(bubble.Body.gameObject);
                        m_Bubbles.RemoveAt(i);
                    }

                    continue;
                }

                bubble.Grow = Mathf.MoveTowards(bubble.Grow, 1f, deltaTime * 3f);
                bubble.Body.position = Centroid(bubble);
                bubble.Body.localScale = Vector3.one * bubble.Size * (0.2f + 0.8f * bubble.Grow) * (1f + 0.03f * Mathf.Sin(Time.time * 7f));
            }
        }

        void Burst(Bubble bubble)
        {
            if (bubble.Burst > 0f)
                return;

            bubble.Burst = 0.0001f;
            foreach (var enemy in bubble.Targets)
            {
                if (enemy != null)
                    enemy.Pop();
            }

            Sfx.Play(m_PopClip, bubble.Body.position, 0.9f, Random.Range(0.9f, 1.2f) / Mathf.Max(1f, bubble.Size / m_BubbleSize));
        }

        // ---------------------------------------------------------------- toucher la bulle, le rebond (niveau 4)

        // Toucher la bulle, c'est toucher ceux qu'elle enferme (29 octobre, le joueur : la bulle est bien plus grosse qu'eux) : une
        // flèche qui la traverse leur fait ses dégâts, et la crève ; au niveau 4, elle rebondit ensuite vers les autres bulles.
        void WatchArrows()
        {
            if (m_Bubbles.Count == 0)
                return;

            m_Arrows.Reset();
            while (m_Arrows.Next(out var arrow, out var from, out var to))
            {
                foreach (var bubble in m_Bubbles)
                {
                    if (bubble.Burst > 0f || bubble.Body == null)
                        continue;

                    var radius = bubble.Body.localScale.x * 0.5f;
                    var center = bubble.Body.position;
                    if (ArrowWatch.SegmentDistance(from, to, center, center) > radius)
                        continue;

                    var damage = arrow.Damage;
                    foreach (var target in bubble.Targets)
                    {
                        if (target != null && target.IsAlive)
                            Hurt(target, damage, (to - from).normalized, arrow);
                    }

                    Burst(bubble);
                    if (IsBuilt && Level >= 4)
                        Bounce(bubble, center, damage);
                    break;
                }
            }
        }

        // Ta flèche crève une bulle en touchant l'ennemi : au niveau 4, elle rebondit vers les autres.
        void OnArrowHit(ArrowHit hit)
        {
            if (!IsBuilt || Level < 4 || !hit.IsShot || hit.Collider == null)
                return;

            var enemy = hit.Collider.GetComponentInParent<Enemy>();
            var current = enemy != null ? Holding(enemy) : null;
            if (current == null)
                return;

            Burst(current);
            Bounce(current, hit.Point, hit.Damage);
        }

        // De bulle en bulle : la plus proche, à m_BounceRange au plus, prend les dégâts de la flèche et crève, et ainsi de suite.
        void Bounce(Bubble first, Vector3 point, float damage)
        {
            m_Visited.Clear();
            m_Visited.Add(first);
            var visited = m_Visited;
            while (true)
            {
                Bubble next = null;
                var bestDistance = m_BounceRange * m_BounceRange;
                foreach (var bubble in m_Bubbles)
                {
                    if (visited.Contains(bubble) || bubble.Burst > 0f || bubble.Body == null)
                        continue;
                    var distance = (bubble.Body.position - point).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        next = bubble;
                        bestDistance = distance;
                    }
                }

                if (next == null)
                    return;

                visited.Add(next);
                var to = next.Body.position;
                LightningBolt.Spawn(point, to, m_BounceColor, null, 0.05f, 0.3f);
                foreach (var target in next.Targets)
                {
                    if (target != null && target.IsAlive)
                        Hurt(target, damage, (to - point).normalized, this);
                }

                Burst(next);
                point = to;
            }
        }

        static void Hurt(Enemy target, float damage, Vector3 direction, Object source) =>
            target.Health.TakeDamage(new DamageInfo
            {
                Amount = damage,
                Zone = HitZone.Body,
                Point = target.Center,
                Direction = direction,
                Source = source,
            });

        Bubble Holding(Enemy enemy)
        {
            foreach (var bubble in m_Bubbles)
            {
                if (bubble.Burst <= 0f && bubble.Targets.Contains(enemy))
                    return bubble;
            }

            return null;
        }

        protected override void OnDestroy()
        {
            foreach (var bubble in m_Bubbles)
            {
                foreach (var enemy in bubble.Targets)
                {
                    if (enemy != null)
                        enemy.Pop();
                }

                if (bubble.Body != null)
                    Destroy(bubble.Body.gameObject);
            }

            m_Bubbles.Clear();
            base.OnDestroy();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.7f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 10f));
        }
    }
}
