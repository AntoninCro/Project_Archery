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

        /// <summary>
        /// Ne descend jamais à 0. Les mannequins d'entraînement le sont pendant les vagues, et redeviennent mortels entre deux
        /// (26 octobre : ils comptent alors comme des ennemis, <c>Training/TrainingDummy</c>).
        /// </summary>
        public bool Invulnerable
        {
            get => m_Invulnerable;
            set => m_Invulnerable = value;
        }

        /// <summary>
        /// Figé (la carte pendant l'épreuve du portail des étoiles) : il ne prend plus de dégâts et ne se régénère plus.
        /// Ses scripts sont alors désactivés : un ennemi tué pendant ce temps ne serait jamais compté comme mort.
        /// </summary>
        public bool Frozen { get; set; }

        /// <summary>
        /// Optionnel : les dégâts qui restent d'une attaque, avant qu'ils comptent (0 : rien). Le bouclier du katana s'en
        /// servait pour bloquer les coups (retiré le 22 octobre, remplacé par le gantelet).
        /// </summary>
        public Func<DamageInfo, float> DamageFilter { get; set; }

        void OnEnable() => m_Current = m_MaxHealth;

        public void ResetHealth(float max)
        {
            m_MaxHealth = Mathf.Max(1f, max);
            m_Current = m_MaxHealth;
        }

        /// <summary>
        /// Change les PV max sans remettre la vie au maximum : les PV gagnés s'ajoutent aux PV actuels
        /// (amélioration Vitalité).
        /// </summary>
        public void SetMaxHealth(float max)
        {
            max = Mathf.Max(1f, max);
            var gained = max - m_MaxHealth;
            m_MaxHealth = max;
            if (IsAlive)
                m_Current = Mathf.Clamp(m_Current + Mathf.Max(0f, gained), 1f, m_MaxHealth);
        }

        public void Heal(float amount)
        {
            if (IsAlive && amount > 0f)
                m_Current = Mathf.Min(m_MaxHealth, m_Current + amount);
        }

        public void TakeDamage(in DamageInfo info)
        {
            if (!IsAlive || info.Amount <= 0f || Frozen)
                return;

            var received = info;
            if (DamageFilter != null)
            {
                received.Amount = DamageFilter(info);
                if (received.Amount <= 0f)
                    return;
            }

            m_LastDamageTime = Time.time;
            m_Current = m_Invulnerable ? Mathf.Max(1f, m_Current - received.Amount) : Mathf.Max(0f, m_Current - received.Amount);
            Damaged?.Invoke(this, received);

            if (m_Current <= 0f)
            {
                Died?.Invoke(this, received);
                m_OnDeath.Invoke();
            }
        }

        void Update()
        {
            if (m_RegenPerSecond > 0f && !Frozen && IsAlive && m_Current < m_MaxHealth && Time.time - m_LastDamageTime > m_RegenDelay)
                m_Current = Mathf.Min(m_MaxHealth, m_Current + m_RegenPerSecond * Time.deltaTime);
        }
    }
}
