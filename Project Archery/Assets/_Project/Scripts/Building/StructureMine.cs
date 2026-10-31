using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using Archery.Waves;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Mine (refonte du tower defense, étape 5, lot 1 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, partie 6.1) : sur la
    /// voie. Le premier ennemi au sol qui passe dessus la fait exploser : tous les ennemis au sol autour sont blessés, et au
    /// niveau 3 projetés en l'air. Elle se réarme au début de chaque vague (et quand on l'améliore). Commune, par lots de 3.
    /// </summary>
    /// <remarks>
    /// L'explosion est celle de la flèche explosive (<see cref="ExplosionFx"/>, matériaux de <see cref="SpecialArrows"/>).
    /// Désarmée, son modèle (<c>Armed Visual</c>) disparaît ; les pièces des niveaux 2 et 3 restent.
    /// </remarks>
    public class StructureMine : Structure
    {
        [Tooltip("Dégâts de l'explosion, par niveau, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 40f, 60f, 90f };

        [Tooltip("Rayon (m) de l'explosion, par niveau.")]
        [SerializeField]
        float[] m_RadiusPerLevel = { 3f, 3.5f, 4f };

        [Tooltip("Un ennemi au sol à moins de cette distance (m) du centre la déclenche.")]
        [SerializeField]
        float m_TriggerRadius = 1.3f;

        [Tooltip("Niveau à partir duquel l'explosion projette les ennemis en l'air.")]
        [SerializeField]
        int m_LaunchFromLevel = 3;

        [Tooltip("Vitesse (m/s) vers le haut des ennemis projetés.")]
        [SerializeField]
        float m_LaunchSpeed = 6f;

        [Tooltip("Ce qui se voit quand la mine est armée : caché après l'explosion, jusqu'à la vague suivante.")]
        [SerializeField]
        GameObject m_ArmedVisual;

        [Tooltip("Son de l'explosion. Vide : celui de la flèche explosive.")]
        [SerializeField]
        AudioClip m_ExplosionClip;

        bool m_Armed = true;
        WaveManager m_Waves;

        /// <summary>Armée : elle explosera sous le prochain ennemi au sol.</summary>
        public bool IsArmed => m_Armed;

        protected override void Start()
        {
            base.Start();
            m_Waves = WaveManager.Instance;
            if (m_Waves != null)
                m_Waves.WaveStarted += OnWaveStarted;
        }

        protected override void OnDestroy()
        {
            if (m_Waves != null)
                m_Waves.WaveStarted -= OnWaveStarted;
            base.OnDestroy();
        }

        void OnWaveStarted(int wave) => SetArmed(true);

        protected override void OnLevelChanged(int level)
        {
            base.OnLevelChanged(level);
            SetArmed(true);
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt || !m_Armed)
                return;

            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsAirborne || !IsOnTop(enemy.transform.position, m_TriggerRadius))
                    continue;

                Explode();
                return;
            }
        }

        /// <summary>Explose tout de suite (armée ou non), sans se désarmer si <paramref name="keepArmed"/>.</summary>
        public void Explode(bool keepArmed = false)
        {
            if (!IsBuilt)
                return;

            var level = Mathf.Max(1, Level);
            var damage = ValueFor(m_DamagePerLevel, level, 40f) * Strength;
            var radius = ValueFor(m_RadiusPerLevel, level, 3f);
            var center = transform.position;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || !IsOnTop(enemy.transform.position, radius))
                    continue;

                var away = enemy.transform.position - center;
                away.y = 0f;
                var direction = away.sqrMagnitude > 1e-4f ? away.normalized : transform.forward;
                enemy.Health.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Zone = HitZone.Body,
                    Point = enemy.Center,
                    Direction = direction,
                    Source = this,
                });
                if (level >= m_LaunchFromLevel && enemy.IsAlive)
                    enemy.Launch(Vector3.up * m_LaunchSpeed + direction * 2f, this);
            }

            var arrows = SpecialArrows.Instance;
            if (arrows != null && arrows.ExplosionFireMaterial != null && arrows.ExplosionSmokeMaterial != null)
                ExplosionFx.Play(center + Vector3.up * 0.3f, radius, arrows.ExplosionFireMaterial, arrows.ExplosionSmokeMaterial, null);
            Sfx.Play(m_ExplosionClip != null ? m_ExplosionClip : arrows != null ? arrows.ExplosionClip : null, center + Vector3.up * 0.5f);

            if (!keepArmed)
                SetArmed(false);
        }

        // Un ennemi au sol, à cette distance horizontale au plus (les volants passent bien plus haut).
        bool IsOnTop(Vector3 position, float radius)
        {
            var offset = position - transform.position;
            if (offset.y < -1f || offset.y > 1.5f)
                return false;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        void SetArmed(bool armed)
        {
            m_Armed = armed;
            if (m_ArmedVisual != null)
                m_ArmedVisual.SetActive(armed);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.55f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, m_TriggerRadius);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RadiusPerLevel, Mathf.Max(1, Level), 3f));
        }
    }
}
