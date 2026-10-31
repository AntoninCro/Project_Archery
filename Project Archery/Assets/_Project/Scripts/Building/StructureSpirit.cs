using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Esprit gardien (légendaire ; refonte du tower defense, étape 5, lot 6 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>,
    /// partie 6.1, et la liste « Après le rendu » du GDD) : partout. Un esprit flotte au-dessus de son autel et chasse les ennemis
    /// isolés : il fond sur celui qui a le moins de voisins, le frappe jusqu'à sa mort (ou sa sortie de portée), puis passe au
    /// suivant ; sans proie, il revient chez lui. Plus loin, plus fort et plus vite à chaque niveau.
    /// </summary>
    /// <remarks>
    /// L'esprit (<c>Spirit</c>, le fantôme de Quaternius) se déplace en coordonnées du monde ; chaque coup est un court éclair
    /// (<see cref="LightningBolt"/>) à sa couleur. Il touche les volants comme les autres.
    /// </remarks>
    public class StructureSpirit : Structure
    {
        [Tooltip("Portée (m) de la chasse, depuis l'autel, par niveau.")]
        [SerializeField]
        float[] m_RangePerLevel = { 25f, 30f, 35f };

        [Tooltip("Dégâts d'un coup, par niveau, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 15f, 22f, 32f };

        [Tooltip("Temps (s) entre deux coups, par niveau.")]
        [SerializeField]
        float[] m_AttackIntervalPerLevel = { 1.2f, 1f, 0.8f };

        [Tooltip("Vitesse (m/s) de l'esprit.")]
        [SerializeField]
        float m_Speed = 7f;

        [Tooltip("Distance (m) d'où il frappe.")]
        [SerializeField]
        float m_Reach = 1.3f;

        [Tooltip("Un autre ennemi à moins de cette distance (m) est un voisin : l'esprit préfère ceux qui en ont le moins.")]
        [SerializeField]
        float m_NeighbourRadius = 6f;

        [Tooltip("Hauteur (m) de l'esprit au-dessus de l'autel, au repos.")]
        [SerializeField]
        float m_RestHeight = 1.8f;

        [Tooltip("L'esprit : il quitte l'autel pour chasser.")]
        [SerializeField]
        Transform m_Spirit;

        [SerializeField]
        Color m_Color = new Color(0.6f, 1f, 0.85f);

        [SerializeField]
        AudioClip m_StrikeClip;

        Vector3 m_SpiritRest;
        Enemy m_Prey;
        float m_RetargetTimer;
        float m_AttackTimer;

        Vector3 Home => transform.position + Vector3.up * m_RestHeight;

        protected override void Awake()
        {
            if (m_Spirit != null)
                m_SpiritRest = m_Spirit.localPosition;
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
            if (m_Spirit == null)
                return;

            if (!IsBuilt)
            {
                m_Spirit.localPosition = m_SpiritRest;
                return;
            }

            var deltaTime = Time.deltaTime;
            var level = Mathf.Max(1, Level);
            var range = ValueFor(m_RangePerLevel, level, 25f) * RangeScale * OverchargeRange;
            m_RetargetTimer -= deltaTime;
            if (m_RetargetTimer <= 0f || m_Prey == null || !m_Prey.IsAlive || !InRange(m_Prey, range))
            {
                m_RetargetTimer = 0.5f;
                m_Prey = FindPrey(range);
            }

            // Vers sa proie (un peu au-dessus d'elle), ou chez lui en flottant doucement.
            var target = m_Prey != null ? m_Prey.Center + Vector3.up * 0.6f : Home + Vector3.up * (0.15f * Mathf.Sin(Time.time * 2f));
            var position = m_Spirit.position;
            var next = Vector3.MoveTowards(position, target, m_Speed * OverchargeRate * deltaTime);
            var motion = next - position;
            m_Spirit.position = next;
            motion.y = 0f;
            if (motion.sqrMagnitude > 1e-6f)
                m_Spirit.rotation = Quaternion.Slerp(m_Spirit.rotation, Quaternion.LookRotation(motion), 1f - Mathf.Exp(-8f * deltaTime));

            m_AttackTimer -= deltaTime * OverchargeRate;
            if (m_Prey == null || m_AttackTimer > 0f || (m_Prey.Center - next).sqrMagnitude > (m_Reach + 0.6f) * (m_Reach + 0.6f))
                return;

            m_AttackTimer = ValueFor(m_AttackIntervalPerLevel, level, 1.2f);
            Strike(m_Prey, next, ValueFor(m_DamagePerLevel, level, 15f) * Strength);
        }

        bool InRange(Enemy enemy, float range)
        {
            var offset = enemy.transform.position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= range * range;
        }

        // L'ennemi à portée qui a le moins de voisins ; à égalité, le plus proche de l'esprit.
        Enemy FindPrey(float range)
        {
            var enemies = AliveEnemies();
            Enemy best = null;
            var bestNeighbours = int.MaxValue;
            var bestDistance = float.MaxValue;
            var from = m_Spirit.position;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive || !InRange(enemy, range))
                    continue;

                var neighbours = 0;
                foreach (var other in enemies)
                {
                    if (other != enemy && other != null && other.IsAlive &&
                        (other.transform.position - enemy.transform.position).sqrMagnitude <= m_NeighbourRadius * m_NeighbourRadius)
                        neighbours++;
                }

                var distance = (enemy.Center - from).sqrMagnitude;
                if (neighbours < bestNeighbours || (neighbours == bestNeighbours && distance < bestDistance))
                {
                    best = enemy;
                    bestNeighbours = neighbours;
                    bestDistance = distance;
                }
            }

            return best;
        }

        void Strike(Enemy prey, Vector3 from, float damage)
        {
            var to = prey.Center;
            LightningBolt.Spawn(from, to, m_Color);
            prey.Health.TakeDamage(new DamageInfo
            {
                Amount = damage,
                Zone = HitZone.Body,
                Point = to,
                Direction = (to - from).normalized,
                Source = this,
            });
            Sfx.Play(m_StrikeClip, to, 0.8f, Random.Range(0.9f, 1.15f));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = m_Color;
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 25f));
        }
    }
}
