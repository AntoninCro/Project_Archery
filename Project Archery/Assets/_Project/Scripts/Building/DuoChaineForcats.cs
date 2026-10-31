using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Chaîne de forçats</b> (Tour-aimant + Lance-filet ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.17) : une tour
    /// de fer, des bobines de chaîne, un gros aimant rouge. Des chaînes aimantées lient les ennemis d'un groupe : ils avancent
    /// ensemble, plus lentement, et chaque coup sur l'un blesse tous les autres (ta flèche à la tête d'un seul : toute la chaîne).
    /// <list type="bullet">
    /// <item>1 : jusqu'à 5 ennemis liés 8 s ;</item>
    /// <item>2 : jusqu'à 8 ; un lié qui meurt fait tomber les autres ;</item>
    /// <item>3, <b>l'écrasement</b> : jusqu'à 15 ; la chaîne se resserre d'un coup : tous volent les uns contre les autres et
    /// s'écrasent au milieu (40 % de leurs PV max), sonnés.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Le partage passe par <see cref="Enemy.Damaged"/> : les dégâts reçus par un lié (sauf ceux que le duo partage lui-même) sont
    /// rendus aux autres, en part (<see cref="m_Share"/>). Les boss peuvent être liés ; les volants non. Les chaînes sont des
    /// LineRenderer au matériau de fer du modèle.
    /// </remarks>
    public class DuoChaineForcats : TowerDuo
    {
        [Header("L'aimant")]
        [Tooltip("Le gros aimant (en haut de la tour) : les chaînes en partent à la prise.")]
        [SerializeField]
        Transform m_Magnet;

        [Tooltip("Le matériau des chaînes.")]
        [SerializeField]
        Material m_ChainMaterial;

        [SerializeField]
        float m_Range = 30f;

        [Tooltip("Temps (s) entre deux chaînes, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 9f, 8f, 9f };

        [Tooltip("Combien d'ennemis liés au plus, par niveau ; rayon (m) du groupe autour du premier.")]
        [SerializeField]
        int[] m_MaxLinkedPerLevel = { 5, 8, 15 };

        [SerializeField]
        float m_GroupRadius = 7f;

        [Tooltip("Temps (s) que dure une chaîne.")]
        [SerializeField]
        float m_Duration = 8f;

        [Header("Les liés")]
        [Tooltip("Part des dégâts reçus par un lié rendue à chacun des autres.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_Share = 0.6f;

        [Tooltip("Ralentissement des liés (0,6 = 40 % plus lents).")]
        [SerializeField]
        float m_Slow = 0.6f;

        [Tooltip("Distance (m) au milieu de la chaîne au-delà de laquelle un lié est tiré vers les autres.")]
        [SerializeField]
        float m_Slack = 3f;

        [Header("L'écrasement (niveau 3)")]
        [Tooltip("Temps (s) après la prise où la chaîne se resserre.")]
        [SerializeField]
        float m_CrushDelay = 2.5f;

        [Range(0f, 1f)]
        [SerializeField]
        float m_CrushShare = 0.4f;

        [SerializeField]
        float m_CrushDamage = 40f;

        [SerializeField]
        float m_CrushStun = 2f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_BindClip;

        [SerializeField]
        AudioClip m_CrushClip;

        class Chain
        {
            public readonly List<Enemy> Members = new List<Enemy>();
            public readonly List<LineRenderer> Links = new List<LineRenderer>();
            public float Until;
            public float CrushAt;
            public float LandAt = -1f;
            public Vector3 Center;
        }

        readonly List<Chain> m_Chains = new List<Chain>();
        readonly Dictionary<Enemy, Chain> m_ChainOf = new Dictionary<Enemy, Chain>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        readonly List<Enemy> m_Shared = new List<Enemy>();
        LineRenderer m_Pull;
        float m_PullUntil;
        float m_NextChain;

        void OnEnable()
        {
            Enemy.Damaged += OnEnemyDamaged;
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            Enemy.Damaged -= OnEnemyDamaged;
            Enemy.Killed -= OnEnemyKilled;
        }

        protected override void OnDestroy()
        {
            for (var i = m_Chains.Count - 1; i >= 0; i--)
                Break(m_Chains[i]);
            if (m_Pull != null)
                Destroy(m_Pull.gameObject);
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            for (var i = m_Chains.Count - 1; i >= 0; i--)
            {
                if (!Step(m_Chains[i]))
                    Break(m_Chains[i]);
            }

            if (m_Pull != null)
                m_Pull.enabled = Time.time < m_PullUntil;
            if (!IsBuilt || Time.time < m_NextChain)
                return;

            var level = Mathf.Max(1, Level);
            m_NextChain = Time.time + 0.5f;
            if (Bind(level))
                m_NextChain = Time.time + ValueFor(m_IntervalPerLevel, level, 9f) / OverchargeRate;
        }

        // Le groupe le plus serré à portée (sans liés) : jusqu'à N ennemis autour du premier.
        bool Bind(int level)
        {
            Enemy seed = null;
            var best = 1;
            m_Scratch.Clear();
            foreach (var enemy in EnemiesNear(transform.position, m_Range))
            {
                if (!m_ChainOf.ContainsKey(enemy))
                    m_Scratch.Add(enemy);
            }

            foreach (var enemy in m_Scratch)
            {
                var count = 0;
                foreach (var other in m_Scratch)
                {
                    if ((other.transform.position - enemy.transform.position).sqrMagnitude < m_GroupRadius * m_GroupRadius)
                        count++;
                }

                if (count > best)
                {
                    best = count;
                    seed = enemy;
                }
            }

            if (seed == null)
                return false;

            var chain = new Chain { Until = Time.time + m_Duration, CrushAt = level >= 3 ? Time.time + m_CrushDelay : float.MaxValue };
            var origin = seed.transform.position;
            m_Scratch.Sort((a, b) => (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude));
            var max = ValueFor(m_MaxLinkedPerLevel, level, 5);
            foreach (var enemy in m_Scratch)
            {
                if (chain.Members.Count >= max || (enemy.transform.position - origin).sqrMagnitude > m_GroupRadius * m_GroupRadius)
                    break;
                chain.Members.Add(enemy);
                m_ChainOf[enemy] = chain;
                enemy.Slow(m_Slow, m_Duration);
                enemy.Prepare(Preparation.Netted, m_Duration);
            }

            Order(chain);
            for (var i = 0; i < chain.Members.Count; i++)
                chain.Links.Add(NewLine("Chaîne"));
            m_Chains.Add(chain);

            // L'aimant lance la chaîne : un trait de fer de la tour au groupe, un instant.
            if (m_Pull == null)
                m_Pull = NewLine("Traction");
            m_Pull.SetPosition(0, m_Magnet != null ? m_Magnet.position : transform.position + Vector3.up * 6f);
            m_Pull.SetPosition(1, Centroid(chain));
            m_PullUntil = Time.time + 0.4f;
            FloatingText.Spawn(Centroid(chain) + Vector3.up * 2.5f, "Enchaînés !", new Color(0.85f, 0.85f, 0.9f, 1f), 1.4f, 1.3f);
            Sfx.Play(m_BindClip, Centroid(chain), 1f, Random.Range(0.9f, 1.05f));
            return true;
        }

        // Les liés en ronde autour de leur milieu : chacun lié au suivant.
        static void Order(Chain chain)
        {
            var center = Centroid(chain);
            chain.Members.Sort((a, b) =>
            {
                var da = a.transform.position - center;
                var db = b.transform.position - center;
                return Mathf.Atan2(da.z, da.x).CompareTo(Mathf.Atan2(db.z, db.x));
            });
        }

        static Vector3 Centroid(Chain chain)
        {
            var sum = Vector3.zero;
            var count = 0;
            foreach (var enemy in chain.Members)
            {
                if (enemy == null)
                    continue;
                sum += enemy.Center;
                count++;
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        bool Step(Chain chain)
        {
            chain.Members.RemoveAll(enemy => enemy == null || !enemy.IsAlive);
            if (chain.Members.Count < 2 && chain.LandAt < 0f)
                return false;
            if (Time.time >= chain.Until)
                return false;

            var center = Centroid(chain);
            chain.Center = center;

            // Ils avancent ensemble : celui qui s'éloigne est tiré vers les autres.
            foreach (var enemy in chain.Members)
            {
                var away = center - enemy.Center;
                away.y = 0f;
                if (away.magnitude > m_Slack && !enemy.IsAirborne)
                    enemy.Push(away.normalized * 3f);
            }

            // L'écrasement (niveau 3) : ils volent les uns vers les autres, puis s'écrasent au milieu.
            if (Time.time >= chain.CrushAt)
            {
                chain.CrushAt = float.MaxValue;
                chain.LandAt = Time.time + 0.45f;
                foreach (var enemy in chain.Members)
                {
                    var to = center - enemy.transform.position;
                    to.y = 0f;
                    enemy.Launch(to / 0.45f + Vector3.up * 3.5f, this);
                }

                Sfx.Play(m_BindClip, center, 1f, 0.7f);
            }

            if (chain.LandAt >= 0f && Time.time >= chain.LandAt)
            {
                chain.LandAt = -1f;
                Crush(chain, center);
            }

            // Les chaînes : de chacun au suivant, en ronde.
            for (var i = 0; i < chain.Links.Count; i++)
            {
                var line = chain.Links[i];
                if (line == null)
                    continue;
                var on = i < chain.Members.Count && chain.Members.Count >= 2;
                line.enabled = on;
                if (!on)
                    continue;
                line.SetPosition(0, chain.Members[i].Center);
                line.SetPosition(1, chain.Members[(i + 1) % chain.Members.Count].Center);
            }

            return true;
        }

        void Crush(Chain chain, Vector3 center)
        {
            m_Scratch.Clear();
            m_Scratch.AddRange(chain.Members);
            foreach (var enemy in m_Scratch)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;
                var amount = m_CrushDamage + enemy.Health.Max * m_CrushShare / Mathf.Max(0.01f, Strength);
                Hurt(enemy, amount, enemy.Center, enemy.transform.position - center);
                if (enemy.IsAlive)
                {
                    enemy.Stun(m_CrushStun);
                    enemy.Prepare(Preparation.Stunned, m_CrushStun);
                }
            }

            Burst(center, new Color(0.8f, 0.82f, 0.9f, 1f), 70, 9f, 0.3f, 0.7f, 0.8f);
            Burst(center, new Color(1f, 0.85f, 0.4f, 1f), 40, 6f, 0.2f, 0.5f, 0.6f);
            FloatingText.Spawn(center + Vector3.up * 2.5f, "Écrasés !", new Color(1f, 0.85f, 0.4f, 1f), 1.8f, 1.5f);
            Sfx.Play(m_CrushClip, center, 1f, 0.6f);
        }

        // Ce que reçoit un lié, les autres le reçoivent aussi, en part (sauf ce que la chaîne partage elle-même).
        void OnEnemyDamaged(Enemy enemy, DamageInfo info)
        {
            if (enemy == null || ReferenceEquals(info.Source, this) || info.Amount <= 0f || !m_ChainOf.TryGetValue(enemy, out var chain))
                return;

            m_Shared.Clear();
            m_Shared.AddRange(chain.Members);
            foreach (var other in m_Shared)
            {
                if (other == null || other == enemy || !other.IsAlive)
                    continue;
                other.Health.TakeDamage(new DamageInfo
                {
                    Amount = info.Amount * m_Share,
                    Zone = HitZone.Body,
                    Point = other.Center,
                    Direction = Vector3.down,
                    Source = this,
                });
            }
        }

        // Niveau 2 : un lié meurt, les autres tombent.
        void OnEnemyKilled(Enemy enemy, DamageInfo info)
        {
            if (enemy == null || !m_ChainOf.TryGetValue(enemy, out var chain))
                return;
            m_ChainOf.Remove(enemy);
            if (Level != 2)
                return;

            var point = enemy.transform.position;
            foreach (var other in chain.Members)
            {
                if (other != null && other != enemy && other.IsAlive && !other.IsAirborne)
                    KnockDown(other, point, 1.5f, 4f);
            }
        }

        void Break(Chain chain)
        {
            foreach (var enemy in chain.Members)
            {
                if (enemy != null)
                {
                    m_ChainOf.Remove(enemy);
                    enemy.Unprepare(Preparation.Netted);
                }
            }

            foreach (var line in chain.Links)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }

            chain.Members.Clear();
            chain.Links.Clear();
            m_Chains.Remove(chain);
        }

        LineRenderer NewLine(string label)
        {
            var go = new GameObject(label);
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = line.endWidth = 0.14f;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (m_ChainMaterial != null)
                line.sharedMaterial = m_ChainMaterial;
            return line;
        }
    }
}
