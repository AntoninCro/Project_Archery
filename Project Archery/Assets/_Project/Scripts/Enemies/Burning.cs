using Archery.Bows;
using Archery.Combat;
using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Brûlure d'une flèche enflammée (GDD, section 6.2) : des dégâts réguliers pendant quelques secondes,
    /// avec des flammes sur l'ennemi. Ajoutée en jeu par le <see cref="Defense.Brazier"/> : rien à placer à la main.
    /// </summary>
    /// <remarks>
    /// Une nouvelle flèche enflammée prolonge la brûlure (la plus longue et la plus forte l'emportent).
    /// Un ennemi tué par la brûlure rapporte ses points comme s'il était tué par la flèche.
    /// </remarks>
    [AddComponentMenu("")]
    public class Burning : MonoBehaviour
    {
        const float k_TickInterval = 0.5f;

        Enemy m_Enemy;
        GameObject m_Effect;
        float m_TimeLeft;
        float m_DamagePerSecond;
        float m_TickTimer;
        ShotGrade m_Grade;
        float m_Distance;

        public static void Apply(Enemy enemy, float damagePerSecond, float duration, ShotGrade grade, float distance, GameObject effectPrefab)
        {
            if (enemy == null || !enemy.IsAlive || duration <= 0f || damagePerSecond <= 0f)
                return;

            var burning = enemy.GetComponent<Burning>();
            if (burning == null)
            {
                burning = enemy.gameObject.AddComponent<Burning>();
                burning.m_Enemy = enemy;
                burning.m_TickTimer = k_TickInterval;
            }

            burning.m_TimeLeft = Mathf.Max(burning.m_TimeLeft, duration);
            burning.m_DamagePerSecond = Mathf.Max(burning.m_DamagePerSecond, damagePerSecond);
            burning.m_Grade = grade;
            burning.m_Distance = distance;

            if (burning.m_Effect == null && effectPrefab != null)
                burning.m_Effect = Instantiate(effectPrefab, enemy.Center, Quaternion.identity, enemy.transform);
        }

        void Update()
        {
            if (m_Enemy == null || !m_Enemy.IsAlive)
            {
                Stop();
                return;
            }

            var deltaTime = Time.deltaTime;
            m_TimeLeft -= deltaTime;
            m_TickTimer -= deltaTime;
            if (m_TickTimer <= 0f)
            {
                m_TickTimer = k_TickInterval;
                m_Enemy.Health.TakeDamage(new DamageInfo
                {
                    Amount = m_DamagePerSecond * k_TickInterval,
                    Zone = HitZone.Body,
                    Grade = m_Grade,
                    Distance = m_Distance,
                    Point = m_Enemy.Center,
                    Direction = Vector3.up,
                    Source = this,
                });
            }

            if (m_TimeLeft <= 0f)
                Stop();
        }

        void Stop()
        {
            if (m_Effect != null)
                Destroy(m_Effect);
            Destroy(this);
        }
    }
}
