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

        [Tooltip("PV actuels. Affichés pour suivre la partie dans l'Inspector ; remis au maximum au lancement.")]
        [SerializeField]
        float m_Current;

        float m_LastDamageTime = float.NegativeInfinity;

        public event Action<Health, DamageInfo> Damaged;
        public event Action<Health, DamageInfo> Died;

        public float Current => m_Current;
        public float Max => m_MaxHealth;
        public float Normalized => m_MaxHealth > 0f ? m_Current / m_MaxHealth : 0f;
        public bool IsAlive => m_Current > 0f;
        public UnityEvent OnDeath => m_OnDeath;

        void OnEnable() => m_Current = m_MaxHealth;

        public void ResetHealth(float max)
        {
            m_MaxHealth = Mathf.Max(1f, max);
            m_Current = m_MaxHealth;
        }

        public void Heal(float amount)
        {
            if (IsAlive && amount > 0f)
                m_Current = Mathf.Min(m_MaxHealth, m_Current + amount);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || info.Amount <= 0f)
                return;

            m_LastDamageTime = Time.time;
            m_Current = m_Invulnerable ? Mathf.Max(1f, m_Current - info.Amount) : Mathf.Max(0f, m_Current - info.Amount);
            Damaged?.Invoke(this, info);

            if (m_Current <= 0f)
            {
                Died?.Invoke(this, info);
                m_OnDeath.Invoke();
            }
        }

        void Update()
        {
            if (m_RegenPerSecond > 0f && IsAlive && m_Current < m_MaxHealth && Time.time - m_LastDamageTime > m_RegenDelay)
                m_Current = Mathf.Min(m_MaxHealth, m_Current + m_RegenPerSecond * Time.deltaTime);
        }
    }
}
