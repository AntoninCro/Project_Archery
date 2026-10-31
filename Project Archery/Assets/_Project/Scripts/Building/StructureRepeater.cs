using Archery.Enemies;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Arbalète à répétition (refonte du tower defense, étape 5, lot 2 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>,
    /// partie 6.1) : au bord de la voie. Une petite baliste qui tire des rafales de carreaux faibles (3 / 4 / 5 par rafale),
    /// et qui vise d'abord les volants, plus blessés (×1,5). Commune.
    /// </summary>
    /// <remarks>
    /// Elle hérite de la <see cref="Ballista"/> (visée, carreaux, cadence de la difficulté, part des bonus de l'arc) : ses
    /// réglages de baliste (portée, dégâts d'un carreau, temps entre deux rafales) sont ceux de son prefab.
    /// </remarks>
    public class StructureRepeater : Ballista
    {
        [Header("Rafale")]
        [Tooltip("Carreaux par rafale, par niveau.")]
        [SerializeField]
        int[] m_BurstPerLevel = { 3, 4, 5 };

        [Tooltip("Temps (s) entre deux carreaux d'une rafale.")]
        [SerializeField]
        float m_BurstSpacing = 0.12f;

        [Tooltip("Écart (°) au hasard de chaque carreau autour de la visée.")]
        [SerializeField]
        float m_Spread = 1.5f;

        [Tooltip("Multiplie les dégâts contre un volant (visé en premier).")]
        [SerializeField]
        float m_FlyerDamageMultiplier = 1.5f;

        int m_BurstLeft;
        float m_BurstTimer;

        protected override void Update()
        {
            base.Update();
            if (m_BurstLeft <= 0)
                return;

            // La cible est morte ou partie : la rafale s'arrête.
            if (Target == null || !Target.IsAlive)
            {
                m_BurstLeft = 0;
                return;
            }

            m_BurstTimer -= Time.deltaTime * OverchargeRate;
            if (m_BurstTimer > 0f)
                return;

            m_BurstTimer = m_BurstSpacing;
            m_BurstLeft--;
            Shoot(Scatter(TurretForward));
        }

        // Le premier carreau part avec la cadence de la baliste ; les suivants, un par un, pendant que la tourelle suit la cible.
        protected override void Fire(Vector3 direction)
        {
            base.Fire(Scatter(direction));
            m_BurstLeft = Mathf.Max(0, ValueFor(m_BurstPerLevel, Level, 3) - 1);
            m_BurstTimer = m_BurstSpacing;
        }

        protected override float ShotDamage => Damage * (Target != null && Target.IsFlyer ? m_FlyerDamageMultiplier : 1f);

        Vector3 Scatter(Vector3 direction) =>
            m_Spread > 0f ? Quaternion.Euler(Random.Range(-m_Spread, m_Spread), Random.Range(-m_Spread, m_Spread), 0f) * direction : direction;

        // Le volant le plus proche à portée et visible ; sans volant, l'ennemi le plus proche (comme la baliste).
        protected override Enemy FindTarget()
        {
            var muzzle = Muzzle;
            Enemy best = null;
            var range = Range;
            var bestDistance = range * range;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || !enemy.IsFlyer)
                    continue;

                var distance = (enemy.Center - muzzle).sqrMagnitude;
                if (distance < bestDistance && IsVisible(muzzle, enemy))
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }

            return best != null ? best : base.FindTarget();
        }
    }
}
