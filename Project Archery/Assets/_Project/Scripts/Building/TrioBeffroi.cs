using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le trio <b>Beffroi</b> (Boussole folle + Phare, 1er novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.19) : un beffroi de
    /// pierre, son grand cadran doré aux aiguilles qui tournent à l'envers, sa cloche, la rose des vents à son pied. Quand la
    /// cloche sonne, les ennemis du rayon <b>rembobinent</b> leurs 4 dernières secondes : ils repartent à reculons sur le chemin
    /// qu'ils viennent de faire (« Rembobiné ! ») et perdent leur avance.
    /// <list type="bullet">
    /// <item>1 : la cloche toutes les 10 s ;</item>
    /// <item>2 : à l'arrivée, ils restent figés 2 s (étourdis : préparés) ;</item>
    /// <item>3, <b>Minuit</b> : en plus, toutes les 30 s, douze coups ; le temps s'arrête 6 s dans tout le rayon, sauf pour toi :
    /// les ennemis sont figés, tes flèches s'arrêtent en l'air, et au douzième coup elles repartent toutes ensemble.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Le beffroi note le chemin des ennemis au sol près de lui (une position tous les dixièmes de seconde, 4 s). Rembobiner, c'est
    /// tenir l'ennemi (<see cref="Enemy.Hold"/>) et le ramener le long de ce chemin, à l'envers, en 1,2 s, puis le lâcher. Le temps
    /// arrêté : les ennemis gelés (<see cref="Enemy.Freeze"/>, sans glace), les flèches arrêtées comme celles du fût suspendu
    /// (<see cref="Arrow.Hover"/>). Les boss ne sont jamais rembobinés, et figés moins longtemps ; les volants pas rembobinés.
    /// </remarks>
    public class TrioBeffroi : TowerTrio
    {
        const float k_Sample = 0.1f;

        [Header("Le beffroi")]
        [Tooltip("La cloche (son repère en haut, à l'attache) : elle se balance quand elle sonne.")]
        [SerializeField]
        Transform m_Bell;

        [Tooltip("Les aiguilles (repère au centre du cadran, l'aiguille vers le haut) : petites et grandes, l'une après l'autre ; " +
                 "elles tournent à l'envers, plus vite quand le temps recule.")]
        [SerializeField]
        Transform[] m_Hands = new Transform[0];

        [Tooltip("Portée (m, à plat, autour du beffroi).")]
        [SerializeField]
        float m_Range = 15f;

        [Header("Rembobiner")]
        [Tooltip("Temps (s) entre deux coups de cloche, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 10f, 8.5f, 7f };

        [Tooltip("Combien de secondes de leur chemin ils rembobinent, et en combien de temps (s).")]
        [SerializeField]
        float m_RewindSeconds = 4f;

        [SerializeField]
        float m_RewindTime = 1.2f;

        [Tooltip("Ennemis rembobinés au plus par coup de cloche.")]
        [SerializeField]
        int m_MaxRewound = 12;

        [Tooltip("Niveau 2 : figés (étourdis) tant de secondes à l'arrivée.")]
        [SerializeField]
        float m_ArrivalStun = 2f;

        [Header("Minuit (niveau 3)")]
        [Tooltip("Temps (s) entre deux Minuit ; il faut au moins cinq ennemis à portée.")]
        [SerializeField]
        float m_MidnightInterval = 30f;

        [Tooltip("Rayon (m) où le temps s'arrête, et durée (s) des douze coups.")]
        [SerializeField]
        float m_MidnightRadius = 20f;

        [SerializeField]
        float m_MidnightTime = 6f;

        [Tooltip("Un boss n'est figé que tant de secondes.")]
        [SerializeField]
        float m_BossStopTime = 2f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_BellClip;

        [SerializeField]
        AudioClip m_RewindClip;

        class Track
        {
            public readonly Vector3[] Points = new Vector3[64];
            public int Head;
            public int Count;
            public float Seen;
        }

        class Rewind
        {
            public Enemy Enemy;
            public readonly List<Vector3> Path = new List<Vector3>();
            public float Time;
            public float NextFx;
        }

        class Stopped
        {
            public Arrow Arrow;
            public Vector3 Velocity;
        }

        static readonly Color k_Sepia = new Color(0.78f, 0.62f, 0.4f, 1f);
        static readonly Color k_Gold = new Color(1f, 0.82f, 0.4f, 1f);
        static readonly Color k_Night = new Color(0.55f, 0.65f, 1f, 1f);

        readonly Dictionary<Enemy, Track> m_Tracks = new Dictionary<Enemy, Track>();
        readonly List<Rewind> m_Rewinds = new List<Rewind>();
        readonly List<Stopped> m_Stopped = new List<Stopped>();
        readonly HashSet<Enemy> m_Frozen = new HashSet<Enemy>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        float m_NextSample;
        float m_NextRing;
        float m_Swing;
        float m_HandSpeed = 1f;
        float m_NextMidnight;
        int m_Strokes;
        float m_NextStroke;
        float m_MidnightEnd;
        float m_NextArrowScan;

        protected override void OnDestroy()
        {
            foreach (var rewind in m_Rewinds)
            {
                if (rewind.Enemy != null && rewind.Enemy.IsHeld)
                    rewind.Enemy.Release(Vector3.zero);
            }

            EndMidnight();
            m_Rewinds.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateRewinds();
            AnimateClock();
            if (!IsBuilt || IsUnderConstruction)
                return;

            Record();
            var level = Mathf.Max(1, Level);
            if (level >= 3 && UpdateMidnight())
                return;

            if (Time.time < m_NextRing || EnemiesNear(transform.position, m_Range).Count == 0)
                return;

            Ring(1f);
            RewindAll();
            m_NextRing = Time.time + ValueFor(m_IntervalPerLevel, level, 10f) / OverchargeRate;
        }

        // ---------------------------------------------------------------- le chemin des ennemis

        // Une position tous les dixièmes de seconde pour chaque ennemi au sol près du beffroi ; les autres sont oubliés.
        void Record()
        {
            if (Time.time < m_NextSample)
                return;
            m_NextSample = Time.time + k_Sample;

            foreach (var enemy in EnemiesNear(transform.position, m_Range + 8f))
            {
                if (enemy.IsHeld || enemy.IsBoss)
                    continue;
                if (!m_Tracks.TryGetValue(enemy, out var track))
                {
                    track = new Track();
                    m_Tracks[enemy] = track;
                }

                track.Points[track.Head] = enemy.transform.position;
                track.Head = (track.Head + 1) % track.Points.Length;
                track.Count = Mathf.Min(track.Count + 1, track.Points.Length);
                track.Seen = Time.time;
            }

            m_Scratch.Clear();
            foreach (var pair in m_Tracks)
            {
                if (pair.Key == null || !pair.Key.IsAlive || Time.time - pair.Value.Seen > 1f)
                    m_Scratch.Add(pair.Key);
            }

            foreach (var enemy in m_Scratch)
                m_Tracks.Remove(enemy);
        }

        // ---------------------------------------------------------------- rembobiner

        void RewindAll()
        {
            var samples = Mathf.Clamp(Mathf.RoundToInt(m_RewindSeconds / k_Sample), 2, 63);
            var count = 0;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(transform.position, m_Range));
            foreach (var enemy in m_Scratch)
            {
                if (count >= m_MaxRewound)
                    break;
                if (!enemy.CanBeLaunched || enemy.IsAirborne || IsRewinding(enemy) || !m_Tracks.TryGetValue(enemy, out var track) ||
                    track.Count < 3)
                    continue;

                // Son chemin, du plus récent au plus ancien, en commençant là où il est.
                var rewind = new Rewind { Enemy = enemy };
                rewind.Path.Add(enemy.transform.position);
                for (var k = 1; k <= Mathf.Min(samples, track.Count); k++)
                {
                    var index = (track.Head - k + track.Points.Length * 2) % track.Points.Length;
                    rewind.Path.Add(track.Points[index]);
                }

                if (!enemy.Hold(this))
                    continue;

                track.Count = 0;
                m_Rewinds.Add(rewind);
                count++;
                FloatingText.Spawn(enemy.transform.position + Vector3.up * 2f, "<< Rembobiné !", k_Sepia, 1.2f, 1.3f);
            }

            if (count > 0)
            {
                m_HandSpeed = -24f;
                Sfx.Play(m_RewindClip, transform.position + Vector3.up * 4f, 0.8f, 0.7f);
            }
        }

        bool IsRewinding(Enemy enemy)
        {
            foreach (var rewind in m_Rewinds)
            {
                if (rewind.Enemy == enemy)
                    return true;
            }

            return false;
        }

        // Il repart à reculons le long de son chemin, sans se retourner ; au bout, il est lâché là (figé au niveau 2).
        void UpdateRewinds()
        {
            for (var i = m_Rewinds.Count - 1; i >= 0; i--)
            {
                var rewind = m_Rewinds[i];
                var enemy = rewind.Enemy;
                if (enemy == null || !enemy.IsAlive || !enemy.IsHeld)
                {
                    m_Rewinds.RemoveAt(i);
                    continue;
                }

                rewind.Time += Time.deltaTime;
                var t = Mathf.Clamp01(rewind.Time / Mathf.Max(0.1f, m_RewindTime));
                var along = t * (rewind.Path.Count - 1);
                var index = Mathf.Min(rewind.Path.Count - 2, Mathf.FloorToInt(along));
                enemy.KeepHeld();
                enemy.transform.position = Vector3.Lerp(rewind.Path[index], rewind.Path[index + 1], along - index);
                if (Time.time >= rewind.NextFx)
                {
                    rewind.NextFx = Time.time + 0.08f;
                    Burst(enemy.Center, k_Sepia, 5, 1f, 0.3f, 0.5f, -0.1f);
                }

                if (t < 1f)
                    continue;

                enemy.Release(Vector3.zero);
                if (Level >= 2)
                {
                    enemy.Stun(m_ArrivalStun);
                    FloatingText.Spawn(enemy.transform.position + Vector3.up * 2f, "Figé !", k_Sepia, 1f, 1f);
                }

                m_Rewinds.RemoveAt(i);
            }
        }

        // ---------------------------------------------------------------- Minuit (niveau 3)

        // Douze coups ; vrai pendant qu'ils sonnent.
        bool UpdateMidnight()
        {
            if (m_Strokes > 0)
            {
                FreezeTime();
                if (Time.time < m_NextStroke)
                    return true;

                var stroke = 13 - m_Strokes;
                m_Strokes--;
                m_NextStroke = Time.time + m_MidnightTime / 12f;
                Ring(1.2f);
                FloatingText.Spawn(Top + Vector3.up * 1.5f, "Dong ! " + stroke, k_Night, 1.4f, 1f);
                if (m_Strokes == 0)
                {
                    EndMidnight();
                    HeadMessages.Show("Minuit !", k_Gold, 2f);
                    m_NextRing = Time.time + 2f;
                }

                return true;
            }

            if (Time.time < m_NextMidnight || EnemiesNear(transform.position, m_Range).Count < 5)
                return false;

            m_NextMidnight = Time.time + m_MidnightInterval / OverchargeRate;
            m_Strokes = 12;
            m_NextStroke = Time.time;
            m_MidnightEnd = Time.time + m_MidnightTime + 0.2f;
            m_HandSpeed = -240f;
            HeadMessages.Show("Le temps s'arrête…", k_Night, 2.5f);
            return true;
        }

        Vector3 Top => m_Bell != null ? m_Bell.position : transform.position + Vector3.up * 10f;

        // Le temps arrêté : les ennemis du rayon figés (un boss un instant), tes flèches arrêtées en l'air.
        void FreezeTime()
        {
            var left = Mathf.Max(0.1f, m_MidnightEnd - Time.time);
            foreach (var enemy in EnemiesNear(transform.position, m_MidnightRadius, false))
            {
                if (!m_Frozen.Add(enemy))
                    continue;
                enemy.Freeze(enemy.IsBoss ? Mathf.Min(left, m_BossStopTime) : left);
            }

            if (Time.time < m_NextArrowScan)
                return;
            m_NextArrowScan = Time.time + 0.05f;
            var radius = m_MidnightRadius * m_MidnightRadius;
            foreach (var arrow in FindObjectsByType<Arrow>())
            {
                if (arrow == null || arrow.IsHovering || Stops(arrow))
                    continue;
                var flat = arrow.transform.position - transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > radius)
                    continue;

                var velocity = arrow.Velocity;
                if (velocity.sqrMagnitude < 1f)
                    continue;
                arrow.Hover(true);
                if (arrow.IsHovering)
                    m_Stopped.Add(new Stopped { Arrow = arrow, Velocity = velocity });
            }
        }

        bool Stops(Arrow arrow)
        {
            foreach (var stopped in m_Stopped)
            {
                if (stopped.Arrow == arrow)
                    return true;
            }

            return false;
        }

        // Le douzième coup : les flèches repartent toutes ensemble, les ennemis reprennent.
        void EndMidnight()
        {
            foreach (var stopped in m_Stopped)
            {
                if (stopped.Arrow == null || !stopped.Arrow.IsHovering)
                    continue;
                stopped.Arrow.Hover(false);
                stopped.Arrow.Velocity = stopped.Velocity;
            }

            foreach (var enemy in m_Frozen)
            {
                if (enemy != null)
                    enemy.Thaw();
            }

            if (m_Stopped.Count > 0)
                Burst(Top, k_Gold, 60, 10f, 0.3f, 0.9f, 0.3f);
            m_Stopped.Clear();
            m_Frozen.Clear();
            m_Strokes = 0;
        }

        // ---------------------------------------------------------------- la cloche, l'horloge

        void Ring(float volume)
        {
            m_Swing = 1f;
            Sfx.Play(m_BellClip, Top, volume, Random.Range(0.95f, 1.02f));
            Burst(Top, k_Gold, 20, 4f, 0.25f, 0.8f, 0f);
        }

        // La cloche se balance et s'apaise ; les aiguilles tournent à l'envers, plus vite quand le temps recule.
        void AnimateClock()
        {
            if (m_Bell != null)
            {
                m_Swing = Mathf.MoveTowards(m_Swing, 0f, Time.deltaTime * 0.5f);
                m_Bell.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 7f) * 28f * m_Swing, 0f, 0f);
            }

            m_HandSpeed = Mathf.MoveTowards(m_HandSpeed, -1f, Time.deltaTime * (m_Strokes > 0 ? 0f : 40f));
            for (var i = 0; i < m_Hands.Length; i++)
            {
                if (m_Hands[i] == null)
                    continue;
                // Les grandes aiguilles (impaires) douze fois plus vite que les petites.
                var speed = m_HandSpeed * (i % 2 == 1 ? 12f : 1f);
                m_Hands[i].Rotate(Vector3.forward, -speed * Time.deltaTime * (i >= 2 ? -1f : 1f), Space.Self);
            }
        }
    }
}
