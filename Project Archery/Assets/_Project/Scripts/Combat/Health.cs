using System;
using UnityEngine;
using UnityEngine.Events;

namespace Archery.Combat
{
    /// <summary>
    /// Points de vie d'un ennemi, de la tour ou d'un mannequin d'entraînement.
    /// </summary>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField]
        float m_MaxHealth = 100f;

        [Tooltip("Ne descend jamais à 0 (mannequins d'entraînement).")]
        [SerializeField]
        bool m_Invulnerable;

        [Tooltip("PV regagnés par seconde après le délai (0 = aucune régénération).")]
        [SerializeField]
        float m_RegenPerSecond;

        [SerializeField]
        float m_RegenDelay = 2f;

        [SerializeField]
        UnityEvent m_OnDeath = new UnityEvent();

        float m_LastDamageTime = float.NegativeInfinity;

        public event Action<Health, DamageInfo> Damaged;
        public event Action<Health, DamageInfo> Died;

        public float Current { get; private set; }
        public float Max => m_MaxHealth;
        public float Normalized => m_MaxHealth > 0f ? Current / m_MaxHealth : 0f;
        public bool IsAlive => Current > 0f;
        public UnityEvent OnDeath => m_OnDeath;

        void OnEnable() => Current = m_MaxHealth;

        public void ResetHealth(float max)
        {
            m_MaxHealth = Mathf.Max(1f, max);
            Current = m_MaxHealth;
        }

        public void Heal(float amount)
        {
            if (IsAlive && amount > 0f)
                Current = Mathf.Min(m_MaxHealth, Current + amount);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || info.Amount <= 0f)
                return;

            m_LastDamageTime = Time.time;
            Current = m_Invulnerable ? Mathf.Max(1f, Current - info.Amount) : Mathf.Max(0f, Current - info.Amount);
            Damaged?.Invoke(this, info);

            if (Current <= 0f)
            {
                Died?.Invoke(this, info);
                m_OnDeath.Invoke();
            }
        }

        void Update()
        {
            if (m_RegenPerSecond > 0f && IsAlive && Current < m_MaxHealth && Time.time - m_LastDamageTime > m_RegenDelay)
                Current = Mathf.Min(m_MaxHealth, Current + m_RegenPerSecond * Time.deltaTime);
        }
    }
}
