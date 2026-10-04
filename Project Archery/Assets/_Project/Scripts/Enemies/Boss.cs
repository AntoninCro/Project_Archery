using System;
using Archery.Combat;
using Archery.Core;
using Archery.Defense;
using Archery.UI;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Enemies
{
    /// <summary>
    /// Boss (GDD, sections 3 et 10) : un <see cref="Enemy"/> lent et très résistant qui frappe la tour.
    /// Il a des points faibles (Hitbox « WeakPoint », dégâts ×3) et appelle des Rampants en renfort.
    /// Comme les autres ennemis, il suit la courbe exponentielle des vagues : le boss de la vague 10 est bien plus solide.
    /// </summary>
    /// <remarks>
    /// Le <see cref="Waves.WaveManager"/> le fait apparaître toutes les 5 vagues ; la vague ne se termine qu'à sa mort.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Enemy))]
    public class Boss : MonoBehaviour
    {
        [SerializeField]
        string m_DisplayName = "Seigneur de guerre";

        [Tooltip("PV en plus à chaque nouveau boss, en plus de la courbe des vagues (0,5 = +50 % au deuxième boss…). " +
                 "0 par défaut : la courbe exponentielle des vagues suffit.")]
        [SerializeField]
        float m_HealthGrowthPerBoss;

        [Header("Renforts")]
        [Tooltip("Ennemi appelé en renfort (le Rampant). Vide : pas de renforts.")]
        [SerializeField]
        Enemy m_SummonPrefab;

        [Tooltip("Délai (s) avant les premiers renforts.")]
        [SerializeField]
        float m_FirstSummonDelay = 8f;

        [Tooltip("Temps (s) entre deux appels de renforts.")]
        [SerializeField]
        float m_SummonInterval = 14f;

        [SerializeField]
        int m_SummonCount = 2;

        [Tooltip("Distance (m) autour du boss où apparaissent les renforts.")]
        [SerializeField]
        float m_SummonRadius = 4f;

        [Tooltip("Pas de renforts s'il y a déjà autant d'ennemis en vie.")]
        [SerializeField]
        int m_MaxEnemiesAlive = 18;

        [SerializeField]
        AudioClip m_SummonClip;

        [Header("Arrivée")]
        [Tooltip("Son de l'arrivée du boss, entendu de partout.")]
        [SerializeField]
        AudioClip m_ArrivalClip;

        [SerializeField]
        Color m_TextColor = new Color(1f, 0.35f, 0.25f);

        Enemy m_Enemy;
        float m_SummonTimer;

        /// <summary>Le boss en vie, s'il y en a un.</summary>
        public static Boss Current { get; private set; }

        public static event Action<Boss> Appeared;
        public static event Action<Boss> Defeated;

        public string DisplayName => m_DisplayName;
        public Enemy Enemy => m_Enemy;
        public Health Health => m_Enemy.Health;

        /// <summary>Numéro du boss dans la partie (1 = le premier), réglé par le Wave Manager à son apparition.</summary>
        public int BossNumber { get; set; } = 1;

        void Awake() => m_Enemy = GetComponent<Enemy>();

        void Start()
        {
            // Enemy a déjà appliqué la difficulté ; on ajoute la croissance d'un boss à l'autre.
            var growth = 1f + m_HealthGrowthPerBoss * Mathf.Max(0, BossNumber - 1);
            if (!Mathf.Approximately(growth, 1f))
                Health.ResetHealth(Health.Max * growth);

            m_SummonTimer = m_FirstSummonDelay;
            Health.Died += OnDied;
            Current = this;
            Sfx.Play(m_ArrivalClip, transform.position + Vector3.up * 3f, 1f, 1f, 0.3f);
            Appeared?.Invoke(this);
        }

        void OnDestroy()
        {
            if (m_Enemy != null && m_Enemy.Health != null)
                m_Enemy.Health.Died -= OnDied;
            if (Current == this)
                Current = null;
        }

        void Update()
        {
            if (!m_Enemy.IsAlive || m_SummonPrefab == null)
                return;

            m_SummonTimer -= Time.deltaTime;
            if (m_SummonTimer > 0f)
                return;

            m_SummonTimer = m_SummonInterval;
            Summon();
        }

        // Les renforts sortent du sol autour du boss, tournés vers la tour.
        void Summon()
        {
            if (Enemy.Alive.Count >= m_MaxEnemiesAlive)
                return;

            var tower = Tower.Instance;
            var summoned = 0;
            for (var i = 0; i < m_SummonCount; i++)
            {
                var offset = UnityEngine.Random.insideUnitCircle.normalized * m_SummonRadius;
                var point = transform.position + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(point, out var hit, 3f, NavMesh.AllAreas))
                    continue;

                var facing = tower != null ? tower.transform.position - hit.position : transform.forward;
                facing.y = 0f;
                var rotation = facing.sqrMagnitude > 0.01f ? Quaternion.LookRotation(facing) : transform.rotation;
                Instantiate(m_SummonPrefab, hit.position, rotation);
                summoned++;
            }

            if (summoned == 0)
                return;

            Sfx.Play(m_SummonClip, transform.position + Vector3.up * 3f);
            FloatingText.Spawn(transform.position + Vector3.up * 5f, "Renforts !", m_TextColor, 1.2f, 1.5f);
        }

        void OnDied(Health health, DamageInfo info)
        {
            if (Current == this)
                Current = null;
            Defeated?.Invoke(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = null;
            Appeared = null;
            Defeated = null;
        }
    }
}
