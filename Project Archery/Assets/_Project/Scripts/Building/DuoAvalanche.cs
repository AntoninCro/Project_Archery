using System.Collections.Generic;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using Archery.World;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Avalanche</b> (Mortier + Tour de givre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.17) : un mortier de glace
    /// tire une boule de neige devant le premier ennemi ; elle roule sur la voie vers la forêt, grossit et avale les ennemis qu'elle
    /// touche (ils y restent pris, gelés, et tournent avec elle) ; au bout de sa course, ou sous ta flèche, elle éclate et tous
    /// volent en l'air, blessés.
    /// <list type="bullet">
    /// <item>1 : une boule qui roule 22 m ; les gros ennemis ne sont que renversés ;</item>
    /// <item>2 : elle roule plus loin, plus grosse, et avale aussi les gros ;</item>
    /// <item>3, <b>l'avalanche</b> : une boule géante (jusqu'à 9 m) qui dévale toute la voie jusqu'à la forêt, avale tout ce qu'elle
    /// croise et éclate en une explosion de neige qui coûte la moitié de leurs PV à tous.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Avaler, c'est tenir (<see cref="Enemy.Hold"/>) : seuls ceux qu'on peut projeter sont pris ; les boss sont poussés et
    /// blessés. La boule est une copie de la boule chargée dans la gueule du mortier (pièce du modèle), avec une
    /// <see cref="AvalancheBall"/> qui reçoit les flèches : crevée par une flèche, l'éclatement fait moitié plus de dégâts.
    /// </remarks>
    public class DuoAvalanche : TowerDuo
    {
        [Header("Le mortier")]
        [Tooltip("Ce qui tourne vers la cible (autour de Y).")]
        [SerializeField]
        Transform m_Turret;

        [Tooltip("La gueule : la boule en part.")]
        [SerializeField]
        Transform m_Muzzle;

        [Tooltip("La boule chargée dans la gueule (pièce du modèle) : copiée à chaque tir.")]
        [SerializeField]
        Transform m_BallTemplate;

        [Tooltip("Diamètre (m) de la boule chargée, à l'échelle 1.")]
        [SerializeField]
        float m_TemplateDiameter = 1f;

        [Tooltip("Portée (m) du tir.")]
        [SerializeField]
        float m_Range = 40f;

        [Tooltip("Temps (s) entre deux tirs, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 6f, 5f, 11f };

        [Tooltip("Durée (s) du vol de la boule et hauteur (m) de sa cloche.")]
        [SerializeField]
        float m_FlightTime = 1.1f;

        [SerializeField]
        float m_FlightHeight = 7f;

        [Header("La boule qui roule")]
        [Tooltip("Rayon (m) de la boule à l'arrivée et au bout de sa course, par niveau.")]
        [SerializeField]
        float[] m_StartRadiusPerLevel = { 0.7f, 0.8f, 2.2f };

        [SerializeField]
        float[] m_EndRadiusPerLevel = { 1.6f, 2.2f, 4.5f };

        [Tooltip("Distance (m) qu'elle roule, par niveau (au niveau 3 : toute la voie).")]
        [SerializeField]
        float[] m_RollDistancePerLevel = { 22f, 35f, 200f };

        [Tooltip("Vitesse (m/s) de la boule, par niveau.")]
        [SerializeField]
        float[] m_RollSpeedPerLevel = { 6f, 7f, 8.5f };

        [Tooltip("Combien d'ennemis elle avale au plus, par niveau.")]
        [SerializeField]
        int[] m_CapacityPerLevel = { 6, 10, 30 };

        [Tooltip("Au-dessus de cette taille (m), un ennemi est « gros » : avalé seulement à partir du niveau 2.")]
        [SerializeField]
        float m_BigHeight = 2.6f;

        [Header("L'éclatement")]
        [Tooltip("Dégâts pour chaque ennemi avalé, par niveau ; au niveau 3, plus cette part de ses PV max.")]
        [SerializeField]
        float[] m_BurstDamagePerLevel = { 60f, 80f, 80f };

        [Range(0f, 1f)]
        [SerializeField]
        float m_GiantShare = 0.5f;

        [Tooltip("Les dégâts quand une flèche crève la boule (fois).")]
        [SerializeField]
        float m_ShotBonus = 1.5f;

        [Tooltip("Dégâts pour ceux qui sont autour, sans avoir été avalés.")]
        [SerializeField]
        float m_SplashDamage = 25f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_FireClip;

        [SerializeField]
        AudioClip m_BurstClip;

        class Swallowed
        {
            public Enemy Enemy;
            public Vector3 Direction;
        }

        class Ball
        {
            public AvalancheBall Body;
            public Transform Transform;
            public SphereCollider Collider;
            public int Level;
            public bool Flying;
            public float Time;
            public Vector3 From;
            public Vector3 To;
            public MapScan.Lane Lane;
            public float Along;
            public float Rolled;
            public float Radius;
            public Vector3 Scale;
            public readonly List<Swallowed> Inside = new List<Swallowed>();
        }

        readonly List<Ball> m_Balls = new List<Ball>();
        readonly HashSet<Enemy> m_Taken = new HashSet<Enemy>();
        Enemy m_Target;
        MapScan.Lane m_TargetLane;
        float m_TargetAlong;
        float m_NextScan;
        float m_NextShot;

        protected override void OnDestroy()
        {
            for (var i = m_Balls.Count - 1; i >= 0; i--)
                Burst(m_Balls[i], 0.5f);
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            for (var i = m_Balls.Count - 1; i >= 0; i--)
            {
                if (!Step(m_Balls[i]))
                    m_Balls.RemoveAt(i);
            }

            if (!IsBuilt || m_BallTemplate == null)
                return;

            var level = Mathf.Max(1, Level);
            if (Time.time >= m_NextScan)
            {
                m_NextScan = Time.time + 0.25f;
                m_Target = FrontEnemy(out m_TargetLane, out m_TargetAlong);
            }

            if (m_Target == null || !m_Target.IsAlive)
                return;

            if (m_Turret != null)
            {
                var look = m_Target.transform.position - m_Turret.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f)
                    m_Turret.rotation = Quaternion.RotateTowards(m_Turret.rotation, Quaternion.LookRotation(look), 120f * Time.deltaTime);
            }

            if (Time.time < m_NextShot)
                return;

            m_NextShot = Time.time + ValueFor(m_IntervalPerLevel, level, 6f) / OverchargeRate;
            Fire(m_TargetLane, Mathf.Min(m_TargetLane.Length, m_TargetAlong + 4f), level);
            m_Target = null;
        }

        // Le premier ennemi au sol à portée : le plus avancé vers la tour sur sa voie.
        Enemy FrontEnemy(out MapScan.Lane lane, out float along)
        {
            lane = null;
            along = 0f;
            Enemy best = null;
            var bestAlong = float.MinValue;
            foreach (var enemy in EnemiesNear(transform.position, m_Range))
            {
                if (enemy.IsBoss || m_Taken.Contains(enemy))
                    continue;
                var own = LaneNearest(enemy.transform.position, null, out var at, out var distance);
                if (own == null || distance > 6f || at <= bestAlong)
                    continue;
                bestAlong = at;
                best = enemy;
                lane = own;
                along = at;
            }

            return best;
        }

        void Fire(MapScan.Lane lane, float along, int level)
        {
            var landing = lane.PointAt(along, out _);
            landing.y = GroundY(landing);
            var radius = ValueFor(m_StartRadiusPerLevel, level, 0.7f);
            var from = m_Muzzle != null ? m_Muzzle.position : transform.position + Vector3.up * 4f;
            var copy = Instantiate(m_BallTemplate.gameObject, from, Quaternion.identity);
            copy.name = level >= 3 ? "Avalanche" : "Boule de neige";
            copy.SetActive(true);
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>())
                Destroy(behaviour);
            foreach (var collider in copy.GetComponentsInChildren<Collider>())
                Destroy(collider);

            var ball = new Ball
            {
                Transform = copy.transform,
                Level = level,
                Flying = true,
                From = from,
                To = landing + Vector3.up * radius,
                Lane = lane,
                Along = along,
                Radius = radius,
                Scale = m_BallTemplate.lossyScale / Mathf.Max(0.01f, m_TemplateDiameter),
            };
            ball.Collider = copy.AddComponent<SphereCollider>();
            ball.Collider.radius = 0.5f;
            ball.Body = copy.AddComponent<AvalancheBall>();
            ball.Body.Owner = this;
            Resize(ball);
            m_Balls.Add(ball);
            Sfx.Play(m_FireClip, from, 1f, level >= 3 ? 0.6f : Random.Range(0.9f, 1.05f));
        }

        void Resize(Ball ball)
        {
            ball.Transform.localScale = ball.Scale * (2f * ball.Radius);
        }

        bool Step(Ball ball)
        {
            if (ball.Transform == null)
                return false;

            if (ball.Flying)
            {
                ball.Time += Time.deltaTime;
                var t = Mathf.Clamp01(ball.Time / m_FlightTime);
                ball.Transform.position = Vector3.Lerp(ball.From, ball.To, t) + Vector3.up * (4f * m_FlightHeight * t * (1f - t));
                if (t < 1f)
                    return true;
                ball.Flying = false;
                Burst(ball.To - Vector3.up * ball.Radius, Color.white, 30, 5f, 0.6f, 0.8f, 0.8f, false);
                return true;
            }

            // Elle roule vers la forêt, le long de la voie, et grossit.
            var level = ball.Level;
            var step = ValueFor(m_RollSpeedPerLevel, level, 6f) * Time.deltaTime;
            var distance = ValueFor(m_RollDistancePerLevel, level, 22f);
            ball.Along -= step;
            ball.Rolled += step;
            var point = ball.Lane.PointAt(Mathf.Max(0f, ball.Along), out var direction);
            ball.Radius = Mathf.Lerp(ValueFor(m_StartRadiusPerLevel, level, 0.7f), ValueFor(m_EndRadiusPerLevel, level, 1.6f),
                                     Mathf.Clamp01(ball.Rolled / Mathf.Min(distance, 60f)));
            Resize(ball);
            point.y = GroundY(point) + ball.Radius;
            ball.Transform.position = point;
            var axis = Vector3.Cross(Vector3.up, -direction);
            if (axis.sqrMagnitude > 1e-4f)
                ball.Transform.Rotate(axis.normalized, step / Mathf.Max(0.1f, ball.Radius) * Mathf.Rad2Deg, Space.World);

            Swallow(ball);
            Carry(ball);
            if (ball.Rolled < distance && ball.Along > 0f)
                return true;

            Burst(ball, 1f);
            return false;
        }

        // Ceux qu'elle touche y restent pris ; les boss et les gros (niveau 1) sont renversés et blessés.
        void Swallow(Ball ball)
        {
            var center = ball.Transform.position;
            var capacity = ValueFor(m_CapacityPerLevel, ball.Level, 6);
            foreach (var enemy in EnemiesNear(center, ball.Radius + 0.4f))
            {
                if (m_Taken.Contains(enemy))
                    continue;
                if (ball.Inside.Count >= capacity || !enemy.CanBeLaunched || (ball.Level < 2 && Height(enemy) > m_BigHeight))
                {
                    if (enemy.IsBoss)
                        enemy.Push(ForestwardAt(center) * 4f);
                    else
                        KnockDown(enemy, center, 4f, 4f);
                    continue;
                }

                if (!enemy.Hold(this))
                    continue;
                m_Taken.Add(enemy);
                var direction = Random.onUnitSphere;
                if (direction.y < -0.2f)
                    direction.y = -direction.y;
                ball.Inside.Add(new Swallowed { Enemy = enemy, Direction = Quaternion.Inverse(ball.Transform.rotation) * direction });
                enemy.Prepare(Preparation.Frozen, 20f);
            }
        }

        // Les avalés tournent avec la boule, les pieds dans la neige.
        void Carry(Ball ball)
        {
            for (var i = ball.Inside.Count - 1; i >= 0; i--)
            {
                var inside = ball.Inside[i];
                var enemy = inside.Enemy;
                if (enemy == null || !enemy.IsAlive || !enemy.IsHeld)
                {
                    // Mort dans la neige (une flèche) : il en tombe tout de suite.
                    if (enemy != null)
                    {
                        m_Taken.Remove(enemy);
                        if (enemy.IsHeld)
                            enemy.Release(Vector3.zero);
                    }

                    ball.Inside.RemoveAt(i);
                    continue;
                }

                var outward = ball.Transform.rotation * inside.Direction;
                enemy.transform.position = ball.Transform.position + outward * (ball.Radius * 0.8f);
                enemy.transform.rotation = Quaternion.FromToRotation(Vector3.up, outward);
                enemy.KeepHeld();
            }
        }

        internal void OnBallShot(AvalancheBall body)
        {
            for (var i = 0; i < m_Balls.Count; i++)
            {
                if (m_Balls[i].Body != body || m_Balls[i].Flying)
                    continue;
                FloatingText.Spawn(body.transform.position + Vector3.up * (m_Balls[i].Radius + 0.8f), "Avalanche !",
                                   new Color(0.75f, 0.92f, 1f, 1f), 1.6f, 1.4f);
                Burst(m_Balls[i], m_ShotBonus);
                m_Balls.RemoveAt(i);
                return;
            }
        }

        // Elle éclate : les avalés volent en l'air, blessés ; ceux qui sont autour aussi.
        void Burst(Ball ball, float bonus)
        {
            if (ball.Transform == null)
                return;

            var center = ball.Transform.position;
            var damage = ValueFor(m_BurstDamagePerLevel, ball.Level, 60f) * bonus;
            foreach (var inside in ball.Inside)
            {
                var enemy = inside.Enemy;
                if (enemy == null)
                    continue;
                m_Taken.Remove(enemy);
                enemy.Unprepare(Preparation.Frozen);
                if (!enemy.IsHeld)
                    continue;
                var away = enemy.transform.position - center;
                away.y = 0f;
                var flat = away.sqrMagnitude > 1e-4f ? away.normalized : Random.insideUnitSphere;
                flat.y = 0f;
                enemy.transform.rotation = Quaternion.LookRotation(flat.sqrMagnitude > 1e-4f ? flat : Vector3.forward);
                enemy.Release(flat * Random.Range(3f, 6f) + Vector3.up * Random.Range(7f, 11f));
                var amount = damage + (ball.Level >= 3 ? enemy.Health.Max * m_GiantShare * bonus / Mathf.Max(0.01f, Strength) : 0f);
                Hurt(enemy, amount, enemy.Center, Vector3.up);
            }

            ball.Inside.Clear();
            foreach (var enemy in EnemiesNear(center, ball.Radius * 1.6f + 1f))
            {
                if (Hurt(enemy, m_SplashDamage * bonus, enemy.Center, enemy.transform.position - center))
                    KnockDown(enemy, center, 4f, 5f);
            }

            var count = ball.Level >= 3 ? 220 : 80;
            Burst(center, Color.white, count, 6f + ball.Radius * 3f, 0.4f + ball.Radius * 0.25f, 1.2f, 1f);
            Burst(center, new Color(0.7f, 0.9f, 1f, 1f), count / 2, 4f + ball.Radius * 2f, 0.25f + ball.Radius * 0.1f, 1f, 0.6f);
            Sfx.Play(m_BurstClip, center, 1f, ball.Level >= 3 ? 0.55f : Random.Range(0.9f, 1.1f));
            Destroy(ball.Transform.gameObject);
            ball.Transform = null;
        }

        static float Height(Enemy enemy)
        {
            var renderer = enemy.GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds.max.y - enemy.transform.position.y : 1.8f;
        }
    }
}
