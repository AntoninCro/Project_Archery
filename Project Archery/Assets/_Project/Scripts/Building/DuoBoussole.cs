using System.Collections.Generic;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Boussole folle</b> (Piques + Tour-aimant ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.18) : une rose des
    /// vents de laiton grande comme une place, posée sur la voie et hérissée de pointes ; son aiguille rouge s'affole. Un ennemi
    /// qui y entre perd le nord : il tourne en rond sur les pointes, autour de l'aiguille.
    /// <list type="bullet">
    /// <item>1 : 3 s à tourner en rond sur les pointes ;</item>
    /// <item>2 : un peu plus longtemps, puis il repart dans le mauvais sens, vers la forêt ;</item>
    /// <item>3, la <b>rose des vents</b> : toutes les 16 s, l'aiguille s'emballe et toute la voie perd le nord : chaque ennemi
    /// tourne en rond là où il est, puis toute la voie repart vers la forêt.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Perdre le nord, c'est le charme (<see cref="Enemy.Charm"/>) : le duo lui donne un point qui tourne autour d'un centre, et il
    /// le poursuit. Les boss et les volants ne le perdent pas (les boss sont ralentis sur la rose). Un ennemi qui l'a perdu ne le
    /// reperd pas avant quelques secondes : la voie ne se bloque jamais.
    /// </remarks>
    public class DuoBoussole : TowerDuo
    {
        [Header("La rose des vents")]
        [Tooltip("L'aiguille : elle tourne autour de Y (son rouge vers +Z, le nord).")]
        [SerializeField]
        Transform m_Needle;

        [Tooltip("Rayon (m) de la rose : un ennemi qui y entre perd le nord.")]
        [SerializeField]
        float m_Radius = 2.4f;

        [Header("Perdre le nord")]
        [Tooltip("Temps (s) à tourner en rond, par niveau.")]
        [SerializeField]
        float[] m_LostTimePerLevel = { 3f, 3.5f, 4f };

        [Tooltip("Rayon (m) du cercle où il tourne, autour de l'aiguille.")]
        [SerializeField]
        float m_CircleRadius = 1.6f;

        [Tooltip("Dégâts des pointes sur la rose, par niveau (vague 1, en Normal), à chaque intervalle.")]
        [SerializeField]
        float[] m_SpikeDamagePerLevel = { 7f, 9f, 11f };

        [SerializeField]
        float m_SpikeInterval = 0.5f;

        [Tooltip("Temps (s) avant qu'un ennemi puisse reperdre le nord sur la rose.")]
        [SerializeField]
        float m_Immunity = 7f;

        [Header("Le mauvais sens (niveau 2)")]
        [Tooltip("Temps (s) à repartir vers la forêt, par niveau (0 : il reprend sa route).")]
        [SerializeField]
        float[] m_BackwardTimePerLevel = { 0f, 3f, 4f };

        [Header("La rose des vents (niveau 3)")]
        [SerializeField]
        float m_RoseInterval = 16f;

        [Tooltip("Jusqu'où (m, le long de la voie, des deux côtés de la boussole) toute la voie perd le nord.")]
        [SerializeField]
        float m_RoseReach = 45f;

        [SerializeField]
        float m_RoseLostTime = 5f;

        [SerializeField]
        float m_RoseBackwardTime = 5f;

        [SerializeField]
        Color m_Color = new Color(1f, 0.38f, 0.3f, 1f);

        [Header("Sons")]
        [SerializeField]
        AudioClip m_LostClip;

        [SerializeField]
        AudioClip m_SpinClip;

        [SerializeField]
        AudioClip m_SpikeClip;

        class Lost
        {
            public Vector3 Center;
            public float Radius;
            public float Angle;
            public float Until;
            public float BackwardTime;
            public bool Backward;
            public bool Announce;
            public float NextSpike;
        }

        static readonly Color k_Gold = new Color(1f, 0.8f, 0.35f, 1f);

        readonly Dictionary<Enemy, Lost> m_Lost = new Dictionary<Enemy, Lost>();
        readonly Dictionary<Enemy, float> m_Immune = new Dictionary<Enemy, float>();
        readonly List<Enemy> m_Ended = new List<Enemy>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        float m_NextScan;
        float m_NextClean;
        float m_NextRose;
        float m_RoseUntil;
        float m_NeedleSpeed;
        float m_NextSpinSound;

        protected override void OnDestroy()
        {
            foreach (var pair in m_Lost)
            {
                if (pair.Key != null)
                {
                    pair.Key.Uncharm();
                    pair.Key.Unprepare(Preparation.Stunned);
                }
            }

            m_Lost.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateLost();
            UpdateNeedle();
            if (!IsBuilt)
                return;

            var level = Mathf.Max(1, Level);
            if (level >= 3 && Time.time >= m_NextRose)
                m_NextRose = Time.time + (Rose() ? m_RoseInterval / OverchargeRate : 1f);

            if (Time.time < m_NextScan)
                return;
            m_NextScan = Time.time + 0.1f;
            if (Time.time >= m_NextClean)
            {
                m_NextClean = Time.time + 3f;
                Clean();
            }

            // Sur la rose : il perd le nord (un boss est seulement ralenti).
            var center = transform.position;
            var lostTime = ValueFor(m_LostTimePerLevel, level, 3f);
            var backward = ValueFor(m_BackwardTimePerLevel, level, 0f);
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(center, m_Radius));
            foreach (var enemy in m_Scratch)
            {
                if (enemy.IsBoss)
                {
                    enemy.Slow(0.5f, 0.3f);
                    continue;
                }

                if (!m_Lost.ContainsKey(enemy) && !IsImmune(enemy))
                    LoseNorth(enemy, center, m_CircleRadius, lostTime, backward, true);
            }
        }

        bool IsImmune(Enemy enemy) => m_Immune.TryGetValue(enemy, out var until) && Time.time < until;

        // Il perd le nord : il tourne en rond autour de ce centre, sur ce rayon.
        bool LoseNorth(Enemy enemy, Vector3 center, float radius, float duration, float backward, bool announce)
        {
            if (!enemy.Charm(duration + 0.5f, enemy.transform.position))
                return false;

            var offset = enemy.transform.position - center;
            m_Lost[enemy] = new Lost
            {
                Center = center,
                Radius = radius,
                Angle = Mathf.Atan2(offset.z, offset.x),
                Until = Time.time + duration,
                BackwardTime = backward,
                Announce = announce,
                NextSpike = Time.time + 0.2f,
            };
            enemy.Prepare(Preparation.Stunned, duration + backward);
            if (announce)
            {
                FloatingText.Spawn(enemy.Center + Vector3.up * 1.3f, "Perdu le nord !", m_Color, 1f, 1.2f);
                Sfx.Play(m_LostClip, enemy.Center, 0.6f, Random.Range(0.9f, 1.15f));
            }

            return true;
        }

        // Chacun poursuit un point qui tourne autour de son centre, un peu devant lui : il tourne en rond. Sur la rose, les pointes.
        void UpdateLost()
        {
            if (m_Lost.Count == 0)
                return;

            var spike = ValueFor(m_SpikeDamagePerLevel, Mathf.Max(1, Level), 7f);
            var center = transform.position;
            var reach = (m_Radius + 0.3f) * (m_Radius + 0.3f);
            m_Ended.Clear();
            foreach (var pair in m_Lost)
            {
                var enemy = pair.Key;
                var lost = pair.Value;
                if (enemy == null || !enemy.IsAlive || !enemy.IsCharmed || Time.time >= lost.Until)
                {
                    m_Ended.Add(enemy);
                    continue;
                }

                if (!lost.Backward)
                {
                    var speed = Mathf.Max(1f, enemy.CurrentSpeed * 1.25f);
                    lost.Angle += speed / Mathf.Max(0.5f, lost.Radius) * Time.deltaTime;
                    var ahead = lost.Angle + 0.8f;
                    enemy.SetCharmDestination(lost.Center + new Vector3(Mathf.Cos(ahead), 0f, Mathf.Sin(ahead)) * lost.Radius);
                }

                var flat = enemy.transform.position - center;
                flat.y = 0f;
                if (Time.time < lost.NextSpike || flat.sqrMagnitude > reach)
                    continue;
                lost.NextSpike = Time.time + m_SpikeInterval;
                Hurt(enemy, spike, enemy.transform.position, Vector3.up);
                Burst(enemy.transform.position + Vector3.up * 0.2f, new Color(0.85f, 0.86f, 0.92f, 1f), 6, 2.5f, 0.12f, 0.3f, 1f);
                if (Random.value < 0.3f)
                    Sfx.Play(m_SpikeClip, enemy.transform.position, 0.4f, Random.Range(0.9f, 1.2f));
            }

            foreach (var enemy in m_Ended)
                End(enemy);
        }

        // Fini de tourner : au niveau 2, il repart dans le mauvais sens, vers la forêt ; sinon il reprend sa route.
        void End(Enemy enemy)
        {
            if (!m_Lost.TryGetValue(enemy, out var lost))
                return;

            if (enemy != null && enemy.IsAlive && !lost.Backward && lost.BackwardTime > 0f && Time.time >= lost.Until)
            {
                var back = BackPoint(enemy.transform.position);
                enemy.Charm(lost.BackwardTime + 0.3f, back);
                enemy.SetCharmDestination(back);
                lost.Backward = true;
                lost.Until = Time.time + lost.BackwardTime;
                if (lost.Announce)
                    FloatingText.Spawn(enemy.Center + Vector3.up * 1.3f, "Demi-tour !", m_Color, 0.9f, 1f);
                return;
            }

            m_Lost.Remove(enemy);
            if (enemy == null)
                return;
            enemy.Uncharm();
            enemy.Unprepare(Preparation.Stunned);
            m_Immune[enemy] = Time.time + m_Immunity;
        }

        // Un point de sa voie 20 m plus près de la forêt (sans voie : droit vers la forêt).
        static Vector3 BackPoint(Vector3 position)
        {
            var lane = LaneNearest(position, null, out var along, out var distance);
            if (lane != null && distance < 10f)
                return lane.PointAt(Mathf.Max(0f, along - 20f), out _);
            return position + ForestwardAt(position) * 20f;
        }

        // La rose des vents : toute la voie de la boussole perd le nord, chacun là où il est.
        bool Rose()
        {
            var lane = LaneNearest(transform.position, null, out var myAlong, out var myDistance);
            if (myDistance > 10f)
                lane = null;

            m_Scratch.Clear();
            foreach (var enemy in GroundEnemies())
            {
                if (lane != null)
                {
                    var own = LaneNearest(enemy.transform.position, null, out var at, out var distance);
                    if (own != lane || distance > 8f || Mathf.Abs(at - myAlong) > m_RoseReach)
                        continue;
                }
                else if ((enemy.transform.position - transform.position).sqrMagnitude > m_RoseReach * m_RoseReach)
                {
                    continue;
                }

                m_Scratch.Add(enemy);
            }

            var count = 0;
            foreach (var enemy in m_Scratch)
            {
                if (enemy.IsBoss)
                {
                    enemy.Slow(0.4f, m_RoseLostTime);
                    continue;
                }

                if (m_Lost.ContainsKey(enemy))
                    continue;
                var around = Random.insideUnitCircle.normalized * 1.5f;
                var center = enemy.transform.position - new Vector3(around.x, 0f, around.y);
                if (LoseNorth(enemy, center, 1.5f, m_RoseLostTime, m_RoseBackwardTime, count < 6))
                    count++;
            }

            if (count == 0)
                return false;

            m_RoseUntil = Time.time + m_RoseLostTime;
            FloatingText.Spawn(transform.position + Vector3.up * 5f, "La rose des vents !", m_Color, 2.2f, 2.5f);
            Burst(transform.position + Vector3.up * 0.4f, k_Gold, 140, 10f, 0.35f, 1.2f, 0.3f, false);
            Sfx.Play(m_SpinClip, transform.position + Vector3.up * 2f, 1f, 0.7f);
            return true;
        }

        // Au calme, l'aiguille cherche le nord en tremblant ; quelqu'un perd le nord, elle s'affole ; la rose des vents, elle s'emballe.
        void UpdateNeedle()
        {
            var target = Time.time < m_RoseUntil ? 1440f : m_Lost.Count > 0 ? 540f : 0f;
            m_NeedleSpeed = Mathf.MoveTowards(m_NeedleSpeed, target, 900f * Time.deltaTime);
            if (m_NeedleSpeed > 300f && Time.time >= m_NextSpinSound)
            {
                m_NextSpinSound = Time.time + (m_NeedleSpeed > 1000f ? 0.35f : 0.7f);
                Sfx.Play(m_SpinClip, transform.position + Vector3.up * 2f, 0.35f, Random.Range(1.1f, 1.3f));
            }

            if (m_Needle == null)
                return;
            if (m_NeedleSpeed > 1f)
            {
                m_Needle.Rotate(0f, m_NeedleSpeed * Time.deltaTime, 0f, Space.World);
                return;
            }

            var north = Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.7f) * 12f + Mathf.Sin(Time.time * 4.3f) * 4f, 0f);
            m_Needle.rotation = Quaternion.Slerp(m_Needle.rotation, north, Time.deltaTime * 2f);
        }

        // Oublie les ennemis partis (morts, sortis de la partie) et les immunités passées.
        void Clean()
        {
            m_Ended.Clear();
            foreach (var pair in m_Immune)
            {
                if (pair.Key == null || !pair.Key.IsAlive || Time.time >= pair.Value)
                    m_Ended.Add(pair.Key);
            }

            foreach (var enemy in m_Ended)
                m_Immune.Remove(enemy);
        }

        /// <summary>La portée dessinée pendant la pose (3 novembre, le placement lisible).</summary>
        public override float ReachAt(int level) => m_Radius;
    }
}
