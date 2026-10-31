using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Tour de foudre (refonte du tower defense, étape 5, lot 1 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, partie 6.1) :
    /// au bord de la voie ou en hauteur. À intervalles réguliers, un éclair part de son sommet vers l'ennemi le plus proche à
    /// portée (volants compris), puis saute vers les suivants, 3 en tout. Rare.
    /// </summary>
    /// <remarks>L'éclair est celui de la flèche de foudre (<see cref="LightningBolt"/>).</remarks>
    public class StructureLightningTower : Structure
    {
        [Tooltip("Dégâts de chaque éclair, par niveau, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 9f, 13f, 19f };

        [Tooltip("Temps (s) entre deux décharges, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 2.6f, 2.1f, 1.6f };

        [Tooltip("Portée (m) du premier éclair, par niveau.")]
        [SerializeField]
        float[] m_RangePerLevel = { 14f, 16f, 18f };

        [Tooltip("Ennemis touchés par une décharge, le premier compris.")]
        [Min(1)]
        [SerializeField]
        int m_Targets = 3;

        [Tooltip("Distance (m) au plus entre deux ennemis pour que l'éclair saute.")]
        [SerializeField]
        float m_JumpRange = 6f;

        [SerializeField]
        Color m_BoltColor = new Color(0.55f, 0.8f, 1f);

        [SerializeField]
        AudioClip m_ZapClip;

        static readonly List<Enemy> s_Hit = new List<Enemy>();

        float m_Timer = 1f;

        // Le haut du bâtiment : l'éclair en part (la hauteur de son volume, au-dessus de sa place actuelle : il a pu être déplacé).
        Vector3 Tip => transform.position + Vector3.up * Mathf.Max(1.5f, ConstructionBounds.size.y);

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            m_Timer -= Time.deltaTime * OverchargeRate;
            if (m_Timer > 0f)
                return;

            m_Timer = ValueFor(m_IntervalPerLevel, Level, 2.6f);
            Discharge();
        }

        void Discharge()
        {
            var level = Mathf.Max(1, Level);
            var range = ValueFor(m_RangePerLevel, level, 14f) * RangeScale * OverchargeRange;
            var damage = ValueFor(m_DamagePerLevel, level, 9f) * Strength;
            var enemies = AliveEnemies();
            var from = Tip;
            var target = Nearest(enemies, from, range, null);
            if (target == null)
            {
                m_Timer = 0.3f;
                return;
            }

            s_Hit.Clear();
            var any = false;
            while (target != null && s_Hit.Count < m_Targets)
            {
                s_Hit.Add(target);
                var to = target.Center;
                LightningBolt.Spawn(from, to, m_BoltColor);
                target.Health.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Zone = HitZone.Body,
                    Point = to,
                    Direction = (to - from).normalized,
                    Source = this,
                });
                any = true;
                from = to;
                target = Nearest(enemies, from, m_JumpRange, s_Hit);
            }

            if (any)
                Sfx.Play(m_ZapClip, Tip, 0.8f, Random.Range(0.92f, 1.08f));
        }

        static Enemy Nearest(List<Enemy> enemies, Vector3 from, float range, List<Enemy> exclude)
        {
            Enemy best = null;
            var bestDistance = range * range;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.IsAlive || (exclude != null && exclude.Contains(enemy)))
                    continue;

                var distance = (enemy.Center - from).sqrMagnitude;
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                best = enemy;
            }

            return best;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.55f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 14f));
        }
    }
}
