using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Defense;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>L'effet d'un anneau élémentaire : un anneau par effet.</summary>
    public enum RingElement
    {
        /// <summary>La flèche s'enflamme (comme au brasero) : sa cible brûle.</summary>
        Fire,

        /// <summary>La flèche se charge : à l'impact, un éclair saute vers les ennemis proches.</summary>
        Lightning,

        /// <summary>La flèche se dédouble : une flèche en plus part à côté d'elle.</summary>
        Split,
    }

    /// <summary>
    /// Anneau élémentaire (refonte du tower defense, étape 5, lot 5 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, partie
    /// 6.1) : partout (voie, bord, hauteur), debout, tourné vers le joueur. Une flèche tirée qui passe dedans prend son effet :
    /// feu, foudre, ou se dédouble (trois anneaux, un par effet). Ses dégâts augmentent aussi aux niveaux 2 et 3. Rare.
    /// </summary>
    /// <remarks>
    /// Chaque anneau suit les flèches du pool (<see cref="ArrowPool"/>) : il regarde, d'une image à l'autre, si la pointe d'une
    /// flèche en vol traverse son disque. Une flèche ne prend l'effet d'un même anneau qu'une fois par tir.
    /// </remarks>
    public class StructureRing : Structure
    {
        [SerializeField]
        RingElement m_Element;

        [Tooltip("Rayon (m) de l'ouverture : une flèche qui passe à cette distance du centre au plus prend l'effet.")]
        [SerializeField]
        float m_Radius = 0.8f;

        [Tooltip("Centre de l'anneau ; il se tourne vers la tête du joueur (son axe Z est l'axe de l'anneau).")]
        [SerializeField]
        Transform m_Hoop;

        [Tooltip("Pièces qui tournent sur elles-mêmes autour de leur axe Z (sens alterné).")]
        [SerializeField]
        Transform[] m_Spinners = new Transform[0];

        [Tooltip("Dégâts en plus de la flèche qui passe (0,15 = 15 %), par niveau.")]
        [SerializeField]
        float[] m_DamageBonusPerLevel = { 0f, 0.15f, 0.3f };

        [Header("Feu")]
        [Tooltip("Dégâts de la brûlure (en tout), en part des dégâts de la flèche, par niveau.")]
        [SerializeField]
        float[] m_BurnSharePerLevel = { 0.3f, 0.4f, 0.5f };

        [Tooltip("Durée (s) de la brûlure.")]
        [SerializeField]
        float m_BurnDuration = 3f;

        [Header("Foudre")]
        [Tooltip("Ennemis touchés par l'éclair à l'impact, par niveau.")]
        [SerializeField]
        int[] m_ChainPerLevel = { 2, 3, 4 };

        [Tooltip("Dégâts de chaque éclair, en part des dégâts de la flèche.")]
        [SerializeField]
        float m_ChainShare = 0.5f;

        [Tooltip("Distance (m) au plus entre deux ennemis pour que l'éclair saute.")]
        [SerializeField]
        float m_ChainRange = 6f;

        [Header("Dédoublement")]
        [Tooltip("Flèches en plus, par niveau.")]
        [SerializeField]
        int[] m_SplitPerLevel = { 1, 1, 2 };

        [Tooltip("Dégâts d'une flèche en plus, en part de ceux de la flèche, par niveau.")]
        [SerializeField]
        float[] m_SplitDamagePerLevel = { 0.6f, 0.8f, 0.8f };

        [Tooltip("Écart (°) des flèches en plus.")]
        [SerializeField]
        float m_SplitAngle = 6f;

        [SerializeField]
        Color m_Color = new Color(1f, 0.55f, 0.2f);

        [SerializeField]
        AudioClip m_PassClip;

        readonly List<Arrow> m_Arrows = new List<Arrow>();
        readonly Dictionary<Arrow, Vector3> m_LastTip = new Dictionary<Arrow, Vector3>();
        readonly HashSet<Arrow> m_Done = new HashSet<Arrow>();
        readonly HashSet<Arrow> m_Charged = new HashSet<Arrow>();
        readonly HashSet<Arrow> m_Ignited = new HashSet<Arrow>();
        readonly List<Enemy> m_Chained = new List<Enemy>();
        ArrowPool m_Pool;
        Camera m_Head;

        void OnEnable()
        {
            ArrowPool.Taken += OnArrowTaken;
            Arrow.AnyHit += OnArrowHit;
        }

        void OnDisable()
        {
            ArrowPool.Taken -= OnArrowTaken;
            Arrow.AnyHit -= OnArrowHit;
        }

        protected override void Start()
        {
            base.Start();
            m_Pool = ArrowPool.Instance;
            if (m_Pool != null)
            {
                foreach (var arrow in m_Pool.AllArrows())
                    OnArrowTaken(arrow);
            }
        }

        void OnArrowTaken(Arrow arrow)
        {
            if (arrow != null && !m_Arrows.Contains(arrow))
                m_Arrows.Add(arrow);
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            FaceThePlayer();
            for (var i = 0; i < m_Spinners.Length; i++)
            {
                if (m_Spinners[i] != null)
                    m_Spinners[i].Rotate(0f, 0f, (i % 2 == 0 ? 40f : -70f) * Time.deltaTime, Space.Self);
            }

            WatchArrows();
        }

        void FaceThePlayer()
        {
            if (m_Hoop == null)
                return;

            if (m_Head == null)
                m_Head = Camera.main;
            var target = m_Head != null ? m_Head.transform.position : Tower.Instance != null ? Tower.Instance.transform.position : m_Hoop.position + transform.forward;
            var toPlayer = target - m_Hoop.position;
            if (toPlayer.sqrMagnitude > 1e-3f)
                m_Hoop.rotation = Quaternion.RotateTowards(m_Hoop.rotation, Quaternion.LookRotation(toPlayer), 120f * Time.deltaTime);
        }

        // D'une image à l'autre, la pointe de chaque flèche en vol traverse-t-elle le disque de l'anneau ?
        void WatchArrows()
        {
            var center = m_Hoop != null ? m_Hoop.position : transform.position + Vector3.up * 1.5f;
            var normal = m_Hoop != null ? m_Hoop.forward : transform.forward;
            for (var i = m_Arrows.Count - 1; i >= 0; i--)
            {
                var arrow = m_Arrows[i];
                if (arrow == null)
                {
                    m_Arrows.RemoveAt(i);
                    continue;
                }

                if (arrow.CurrentState != Arrow.State.Flying || arrow.IsHovering)
                {
                    m_LastTip.Remove(arrow);
                    m_Done.Remove(arrow);
                    m_Charged.Remove(arrow);
                    m_Ignited.Remove(arrow);
                    continue;
                }

                var tip = arrow.TipPosition;
                if (m_LastTip.TryGetValue(arrow, out var last) && !m_Done.Contains(arrow) && Crosses(last, tip, center, normal, out var point))
                {
                    m_Done.Add(arrow);
                    Apply(arrow, point);
                }

                m_LastTip[arrow] = tip;
            }
        }

        bool Crosses(Vector3 from, Vector3 to, Vector3 center, Vector3 normal, out Vector3 point)
        {
            point = to;
            var before = Vector3.Dot(from - center, normal);
            var after = Vector3.Dot(to - center, normal);
            if (before * after > 0f || Mathf.Approximately(before, after))
                return false;

            point = Vector3.Lerp(from, to, before / (before - after));
            return (point - center).sqrMagnitude <= m_Radius * m_Radius;
        }

        void Apply(Arrow arrow, Vector3 point)
        {
            var level = Mathf.Max(1, Level);
            arrow.Damage *= 1f + ValueFor(m_DamageBonusPerLevel, level, 0f);
            arrow.SetTrailColor(m_Color);
            switch (m_Element)
            {
                case RingElement.Fire:
                    // Le brasero (s'il revient dans la scène) fait brûler les flèches enflammées ; sinon, c'est l'anneau.
                    if (Brazier.Instance != null)
                    {
                        Brazier.Instance.IgniteArrow(arrow);
                    }
                    else
                    {
                        arrow.Ignite(BurnFlames.Arrow);
                        m_Ignited.Add(arrow);
                    }

                    break;

                case RingElement.Lightning:
                    m_Charged.Add(arrow);
                    break;

                case RingElement.Split:
                    Split(arrow, point, level);
                    break;
            }

            Sfx.Play(m_PassClip, point, 0.8f, Random.Range(0.95f, 1.1f));
        }

        // Des flèches en plus, de part et d'autre de la flèche, qui comptent comme des tirs (elles ne cassent pas le combo).
        void Split(Arrow arrow, Vector3 point, int level)
        {
            var arrows = SpecialArrows.Instance;
            if (arrows == null)
                return;

            var velocity = arrow.Velocity;
            var speed = velocity.magnitude;
            if (speed < 1f)
                return;

            var count = ValueFor(m_SplitPerLevel, level, 1);
            var damage = arrow.Damage * ValueFor(m_SplitDamagePerLevel, level, 0.6f);
            var side = Vector3.Cross(velocity, Vector3.up);
            if (side.sqrMagnitude < 1e-4f)
                side = Vector3.right;
            side.Normalize();
            for (var i = 0; i < count; i++)
            {
                var angle = m_SplitAngle * (i % 2 == 0 ? 1f : -1f) * (1 + i / 2);
                var direction = Quaternion.AngleAxis(angle, Vector3.up) * (velocity / speed);
                var extra = arrows.LaunchExtraShot(point + side * (angle > 0f ? 0.05f : -0.05f), direction, speed, damage, ShotGrade.None,
                                                   m_Color, arrow.GravityScale);
                if (extra == null)
                    break;

                // La flèche en plus est déjà passée par cet anneau.
                OnArrowTaken(extra);
                m_Done.Add(extra);
            }
        }

        // La flèche enflammée touche un ennemi : il brûle. La flèche chargée : l'éclair saute vers les ennemis les plus proches.
        void OnArrowHit(ArrowHit hit)
        {
            if (hit.Arrow == null || hit.Collider == null)
                return;

            var ignited = m_Ignited.Remove(hit.Arrow);
            var charged = m_Charged.Remove(hit.Arrow);
            if (!ignited && !charged)
                return;

            var first = hit.Collider.GetComponentInParent<Enemy>();
            if (first == null)
                return;

            if (ignited)
                Burning.Apply(first, hit.Damage * ValueFor(m_BurnSharePerLevel, Mathf.Max(1, Level), 0.3f), m_BurnDuration, hit.Grade,
                              hit.TravelDistance, BurnFlames.Enemy);
            if (charged)
                Chain(first, hit);
        }

        void Chain(Enemy first, ArrowHit hit)
        {
            var count = ValueFor(m_ChainPerLevel, Mathf.Max(1, Level), 2);
            var damage = hit.Damage * m_ChainShare;
            var from = hit.Point;
            m_Chained.Clear();
            m_Chained.Add(first);
            var enemies = AliveEnemies();
            for (var i = 0; i < count; i++)
            {
                Enemy next = null;
                var best = m_ChainRange * m_ChainRange;
                foreach (var enemy in enemies)
                {
                    if (enemy == null || !enemy.IsAlive || m_Chained.Contains(enemy))
                        continue;
                    var distance = (enemy.Center - from).sqrMagnitude;
                    if (distance < best)
                    {
                        best = distance;
                        next = enemy;
                    }
                }

                if (next == null)
                    break;

                m_Chained.Add(next);
                var to = next.Center;
                LightningBolt.Spawn(from, to, m_Color);
                next.Health.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Zone = HitZone.Body,
                    Point = to,
                    Direction = (to - from).normalized,
                    Source = this,
                });
                from = to;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = m_Color;
            var center = m_Hoop != null ? m_Hoop.position : transform.position + Vector3.up * 1.5f;
            Gizmos.DrawWireSphere(center, m_Radius);
        }
    }
}
