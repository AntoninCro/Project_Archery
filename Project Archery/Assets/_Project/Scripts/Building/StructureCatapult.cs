using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Building
{
    /// <summary>
    /// Catapulte (refonte du tower defense, étape 5, lot 3 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 6.1 et
    /// 6.2) : au bord de la voie. Elle attrape l'ennemi au sol le plus proche et le lance sur le groupe le plus serré à
    /// portée : il devient un boulet, qui blesse ceux sur qui il retombe, et lui-même un peu. Sans groupe, elle le renvoie
    /// vers la forêt. Plus loin, plus fort et plus souvent à chaque niveau. Rare (proposée).
    /// </summary>
    /// <remarks>
    /// Le vol est celui des ennemis projetés (<c>Enemy.Launch</c>, avec sa chute et ses quilles) ; l'impact du boulet s'y
    /// ajoute à l'atterrissage (<c>Enemy.Landed</c>). Pour l'instant, l'ennemi s'envole de sa place, sans passer par la
    /// cuillère ; le modèle (<c>Arm</c>) bascule au lancer.
    /// </remarks>
    public class StructureCatapult : Structure
    {
        [Tooltip("Temps (s) entre deux lancers, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 5f, 4.2f, 3.5f };

        [Tooltip("Distance (m) à laquelle elle attrape un ennemi, par niveau.")]
        [SerializeField]
        float[] m_GrabRangePerLevel = { 6f, 6.5f, 7f };

        [Tooltip("Portée (m) du lancer, depuis la catapulte, par niveau.")]
        [SerializeField]
        float[] m_ThrowRangePerLevel = { 12f, 14f, 16f };

        [Tooltip("Distance (m) dont l'ennemi est renvoyé vers la forêt quand il n'y a pas de groupe, par niveau.")]
        [SerializeField]
        float[] m_BackDistancePerLevel = { 6f, 7f, 8f };

        [Tooltip("Dégâts de l'impact du boulet à ceux qui sont autour, par niveau, à la vague 1 en Normal (ils suivent la force des ennemis).")]
        [SerializeField]
        float[] m_ImpactDamagePerLevel = { 15f, 22f, 32f };

        [Tooltip("Rayon (m) de l'impact.")]
        [SerializeField]
        float m_ImpactRadius = 2.5f;

        [Tooltip("Part des dégâts de l'impact que prend le boulet lui-même.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_SelfDamage = 0.5f;

        [Tooltip("Durée (s) du vol.")]
        [SerializeField]
        float m_FlightTime = 1.3f;

        [Tooltip("Partie qui bascule au lancer (le bras, ou tout le modèle), autour de son axe X.")]
        [SerializeField]
        Transform m_Arm;

        [SerializeField]
        AudioClip m_ThrowClip;

        [SerializeField]
        AudioClip m_ImpactClip;

        readonly Dictionary<Enemy, float> m_Thrown = new Dictionary<Enemy, float>();
        readonly List<Enemy> m_Expired = new List<Enemy>();
        Quaternion m_ArmRest = Quaternion.identity;
        float m_Cooldown = 1.5f;
        float m_Swing;

        protected override void Awake()
        {
            if (m_Arm != null)
                m_ArmRest = m_Arm.localRotation;
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            Enemy.Landed += OnLanded;
        }

        protected override void OnDestroy()
        {
            Enemy.Landed -= OnLanded;
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            ForgetLostThrows();
            if (!IsBuilt)
                return;

            // Le bras bascule d'un coup, puis revient doucement.
            m_Swing = Mathf.MoveTowards(m_Swing, 0f, Time.deltaTime * 1.5f);
            if (m_Arm != null)
                m_Arm.localRotation = m_ArmRest * Quaternion.Euler(-35f * Mathf.Sin(Mathf.Sqrt(m_Swing) * Mathf.PI), 0f, 0f);

            m_Cooldown -= Time.deltaTime * OverchargeRate;
            if (m_Cooldown > 0f)
                return;

            var level = Mathf.Max(1, Level);
            m_Cooldown = Throw(level) ? ValueFor(m_IntervalPerLevel, level, 5f) : 0.4f;
        }

        bool Throw(int level)
        {
            var enemies = AliveEnemies();
            var grabbed = Grab(enemies, ValueFor(m_GrabRangePerLevel, level, 6f));
            if (grabbed == null)
                return false;

            var start = grabbed.transform.position;
            Vector3 landing;
            var group = Densest(enemies, grabbed, ValueFor(m_ThrowRangePerLevel, level, 12f) * RangeScale * OverchargeRange);
            if (group != null)
            {
                var velocity = group.Velocity;
                velocity.y = 0f;
                landing = group.transform.position + velocity * m_FlightTime;
            }
            else
            {
                landing = start + TowardForest(grabbed) * ValueFor(m_BackDistancePerLevel, level, 6f);
            }

            if (NavMesh.SamplePosition(landing, out var hit, 3f, NavMesh.AllAreas))
                landing = hit.position;

            var throwVelocity = (landing - start) / m_FlightTime - 0.5f * m_FlightTime * Physics.gravity;
            if (!grabbed.Launch(throwVelocity, this))
                return false;

            m_Thrown[grabbed] = Time.time + m_FlightTime + 4f;
            m_Swing = 1f;
            Sfx.Play(m_ThrowClip, transform.position + Vector3.up, 0.9f, Random.Range(0.9f, 1.05f));
            return true;
        }

        // L'ennemi au sol le plus proche, qu'on peut projeter.
        Enemy Grab(List<Enemy> enemies, float range)
        {
            Enemy best = null;
            var bestDistance = range * range;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.CanBeLaunched || enemy.IsAirborne)
                    continue;

                var offset = enemy.transform.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude < bestDistance)
                {
                    best = enemy;
                    bestDistance = offset.sqrMagnitude;
                }
            }

            return best;
        }

        // Le centre du groupe d'ennemis au sol le plus serré à portée (au moins 2, sans compter le boulet, loin de lui).
        Enemy Densest(List<Enemy> enemies, Enemy grabbed, float range)
        {
            Enemy best = null;
            var bestCount = 1;
            foreach (var enemy in enemies)
            {
                if (!IsGround(enemy) || enemy == grabbed)
                    continue;

                var offset = enemy.transform.position - transform.position;
                offset.y = 0f;
                if (offset.magnitude > range || (enemy.transform.position - grabbed.transform.position).sqrMagnitude < 9f)
                    continue;

                var count = 0;
                foreach (var other in enemies)
                {
                    if (IsGround(other) && other != grabbed && (other.transform.position - enemy.transform.position).sqrMagnitude <= m_ImpactRadius * m_ImpactRadius)
                        count++;
                }

                if (count > bestCount)
                {
                    best = enemy;
                    bestCount = count;
                }
            }

            return best;
        }

        static bool IsGround(Enemy enemy) => enemy != null && enemy.IsAlive && !enemy.IsFlyer && !enemy.IsAirborne;

        // Le boulet retombe : il blesse ceux qui sont autour, et lui-même un peu.
        void OnLanded(Enemy enemy)
        {
            if (enemy == null || !m_Thrown.Remove(enemy))
                return;

            var level = Mathf.Max(1, Level);
            var damage = ValueFor(m_ImpactDamagePerLevel, level, 15f) * Strength;
            var point = enemy.transform.position;
            foreach (var other in AliveEnemies())
            {
                if (other == null || other == enemy || !other.IsAlive || other.IsFlyer)
                    continue;

                var offset = other.transform.position - point;
                if (offset.sqrMagnitude > m_ImpactRadius * m_ImpactRadius)
                    continue;

                offset.y = 0f;
                Hit(other, damage, offset.sqrMagnitude > 1e-4f ? offset.normalized : Vector3.forward);
            }

            if (enemy.IsAlive)
                Hit(enemy, damage * m_SelfDamage, Vector3.down);
            Sfx.Play(m_ImpactClip, point + Vector3.up * 0.3f, 0.9f, Random.Range(0.9f, 1.1f));
        }

        void Hit(Enemy enemy, float damage, Vector3 direction) =>
            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = damage,
                Zone = HitZone.Body,
                Point = enemy.Center,
                Direction = direction,
                Source = this,
            });

        // Un ennemi lancé qui n'est jamais retombé (mort en l'air, détruit) : on l'oublie.
        void ForgetLostThrows()
        {
            if (m_Thrown.Count == 0)
                return;

            m_Expired.Clear();
            foreach (var pair in m_Thrown)
            {
                if (pair.Key == null || Time.time > pair.Value)
                    m_Expired.Add(pair.Key);
            }

            foreach (var enemy in m_Expired)
                m_Thrown.Remove(enemy);
        }

        void OnDrawGizmosSelected()
        {
            var level = Mathf.Max(1, Level);
            Gizmos.color = new Color(1f, 0.75f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_GrabRangePerLevel, level, 6f));
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_ThrowRangePerLevel, level, 12f));
        }
    }
}
