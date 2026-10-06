using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Brûlure d'une flèche enflammée (GDD, section 6.2) : des dégâts en plus, étalés régulièrement sur quelques secondes,
    /// avec des flammes sur l'ennemi. Ajoutée en jeu par le <see cref="Defense.Brazier"/> : rien à placer à la main.
    /// </summary>
    /// <remarks>
    /// Chaque flèche enflammée ajoute sa propre brûlure : l'ennemi touché par deux flèches brûle des deux.
    /// Les dégâts tombent toutes les 0,5 s, et le total est exact (le reste tombe à la fin).
    /// Un ennemi tué par la brûlure rapporte ses points comme s'il était tué par la flèche.
    /// </remarks>
    [AddComponentMenu("")]
    public class Burning : MonoBehaviour
    {
        const float k_TickInterval = 0.5f;

        struct Burn
        {
            public float DamagePerSecond;
            public float TimeLeft;
        }

        readonly List<Burn> m_Burns = new List<Burn>();
        Enemy m_Enemy;
        GameObject m_Effect;
        float m_Pending;
        float m_TickTimer;
        ShotGrade m_Grade;
        float m_Distance;

        /// <summary>Ajoute une brûlure qui inflige <paramref name="totalDamage"/> en tout, étalés sur <paramref name="duration"/> secondes.</summary>
        public static void Apply(Enemy enemy, float totalDamage, float duration, ShotGrade grade, float distance, GameObject effectPrefab)
        {
            if (enemy == null || !enemy.IsAlive || duration <= 0f || totalDamage <= 0f)
                return;

            var burning = enemy.GetComponent<Burning>();
            if (burning == null)
            {
                burning = enemy.gameObject.AddComponent<Burning>();
                burning.m_Enemy = enemy;
                burning.m_TickTimer = k_TickInterval;
            }

            burning.m_Burns.Add(new Burn { DamagePerSecond = totalDamage / duration, TimeLeft = duration });
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

            // Chaque brûlure verse ses dégâts au fil du temps, jusqu'à la fin de sa durée.
            var deltaTime = Time.deltaTime;
            for (var i = m_Burns.Count - 1; i >= 0; i--)
            {
                var burn = m_Burns[i];
                var step = Mathf.Min(deltaTime, burn.TimeLeft);
                m_Pending += burn.DamagePerSecond * step;
                burn.TimeLeft -= step;
                if (burn.TimeLeft <= 0f)
                    m_Burns.RemoveAt(i);
                else
                    m_Burns[i] = burn;
            }

            m_TickTimer -= deltaTime;
            var finished = m_Burns.Count == 0;
            if (m_TickTimer <= 0f || finished)
            {
                m_TickTimer += k_TickInterval;
                DealPending();
            }

            if (finished)
                Stop();
        }

        void DealPending()
        {
            if (m_Pending <= 0f)
                return;

            var amount = m_Pending;
            m_Pending = 0f;
            m_Enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = amount,
                Zone = HitZone.Body,
                Grade = m_Grade,
                Distance = m_Distance,
                Point = m_Enemy.Center,
                Direction = Vector3.up,
                Source = this,
            });
        }

        void Stop()
        {
            if (m_Effect != null)
                Destroy(m_Effect);
            Destroy(this);
        }
    }
}
