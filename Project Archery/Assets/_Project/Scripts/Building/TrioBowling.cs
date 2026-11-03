using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using Archery.World;
using TMPro;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le trio <b>Bowling</b> (Avalanche + Barricade, 1er novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.19) : une machine de
    /// retour de boules géante en arche au-dessus de la voie. Plus loin vers la forêt, les ennemis qui arrivent sont <b>rangés en
    /// triangle de quilles</b> (« Rangés ! ») ; puis une boule de glace part de l'arche et roule vers la forêt le long de la voie :
    /// « STRIKE ! », ils volent dans tous les sens. Ta flèche dans la boule l'accélère : elle éclate en glace au milieu des quilles.
    /// <list type="bullet">
    /// <item>1 : une boule toutes les 8 s ;</item>
    /// <item>2 : un strike rend la boule suivante deux fois plus grosse ; une deuxième boule fait le « spare » sur les survivants ;</item>
    /// <item>3, <b>la partie parfaite</b> : en plus, toutes les 24 s, trois triangles sur la voie, douze boules d'affilée, la
    /// dernière de 4 m ; le tableau affiche 300.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Ranger un ennemi, c'est le charmer (<see cref="Enemy.Charm"/>) vers sa place dans le triangle, où il attend. Tout suit la voie
    /// la plus proche de l'arche (les places, le chemin de la boule), pas l'orientation du modèle. Les boss ne sont ni rangés ni
    /// projetés (une part de leurs PV sous la grosse boule) ; les volants ni l'un ni l'autre.
    /// </remarks>
    public class TrioBowling : TowerTrio
    {
        [Header("La machine")]
        [Tooltip("La boule du modèle (sous l'arche) : chaque boule lancée en est une copie, à la taille voulue.")]
        [SerializeField]
        Transform m_BallTemplate;

        [Tooltip("Diamètre (m) de la boule du modèle, à l'échelle du prefab.")]
        [SerializeField]
        float m_TemplateDiameter = 1.4f;

        [Tooltip("Le tableau de score : son texte s'affiche juste devant, face au joueur.")]
        [SerializeField]
        Transform m_Board;

        [Header("Les quilles")]
        [Tooltip("Distance (m, le long de la voie, vers la forêt) entre l'arche et la pointe du triangle.")]
        [SerializeField]
        float m_PinDistance = 9f;

        [Tooltip("Les ennemis au sol à moins de tant de mètres de la pointe sont rangés.")]
        [SerializeField]
        float m_GatherRadius = 7f;

        [Tooltip("Temps (s) qu'un ennemi reste rangé (le temps d'aller à sa place compris).")]
        [SerializeField]
        float m_PinTime = 4f;

        [Tooltip("Écart (m) entre deux rangs du triangle, et entre deux quilles d'un rang.")]
        [SerializeField]
        float m_RowGap = 0.9f;

        [SerializeField]
        float m_PinGap = 1.05f;

        [Header("Les boules")]
        [Tooltip("Temps (s) entre deux boules, s'il y a une quille rangée ; dès la moitié s'il y en a trois.")]
        [SerializeField]
        float m_BallInterval = 8f;

        [SerializeField]
        float m_BallSpeed = 13f;

        [Tooltip("Rayon (m) d'une boule (deux fois plus gros après un strike, au niveau 2).")]
        [SerializeField]
        float m_BallRadius = 0.7f;

        [Tooltip("Dégâts d'une boule, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 45f, 60f, 75f };

        [Tooltip("Ceux qu'elle touche volent : vers la forêt, sur les côtés et en l'air (m/s).")]
        [SerializeField]
        float m_KnockForward = 4f;

        [SerializeField]
        float m_KnockSide = 5f;

        [SerializeField]
        float m_KnockUp = 7f;

        [Header("La mise en place (2 novembre, le joueur)")]
        [Tooltip("La boule se met en place sous l'arche et attend ce temps (s) avant de rouler ; ta flèche dedans pendant ce temps la " +
                 "lance tout de suite, plus grosse et plus forte. Pas pour le spare ni la partie parfaite.")]
        [SerializeField]
        float m_WindupTime = 2f;

        [Tooltip("La boule chargée par ta flèche : sa taille et ses dégâts, multipliés.")]
        [SerializeField]
        float m_ChargedScale = 1.6f;

        [SerializeField]
        float m_ChargedDamage = 1.75f;

        [Header("La boule givrée (ta flèche, la boule déjà lancée)")]
        [Tooltip("Touchée par une flèche, elle va plus vite ; au premier ennemi, elle éclate en glace dans ce rayon.")]
        [SerializeField]
        float m_ShotSpeedFactor = 1.8f;

        [SerializeField]
        float m_ShatterRadius = 4f;

        [SerializeField]
        float m_ShatterDamage = 40f;

        [SerializeField]
        float m_FreezeTime = 2f;

        [Header("La partie parfaite (niveau 3)")]
        [Tooltip("Temps (s) entre deux parties parfaites ; il faut au moins cinq ennemis près des quilles.")]
        [SerializeField]
        float m_PerfectInterval = 24f;

        [SerializeField]
        int m_PerfectBalls = 12;

        [SerializeField]
        float m_PerfectGap = 0.4f;

        [Tooltip("Rayon (m) de la dernière boule, ses dégâts, et la part des PV max qu'elle coûte à un boss.")]
        [SerializeField]
        float m_GiantRadius = 2f;

        [SerializeField]
        float m_GiantDamage = 250f;

        [SerializeField]
        float m_GiantBossShare = 0.06f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_RollClip;

        [SerializeField]
        AudioClip m_StrikeClip;

        [SerializeField]
        AudioClip m_ShatterClip;

        class Pin
        {
            public Enemy Enemy;
            public int Slot;
            public float Since;
        }

        class Ball
        {
            public Transform Body;
            public DuoShotTarget Target;
            public float Start;
            public float Rolled;
            public float End;
            public float Speed;
            public float Radius;
            public float Damage;
            public Vector3 Scale;
            public float Wait;
            public bool Charged;
            public bool Frozen;
            public bool Spare;
            public bool Giant;
            public bool Perfect;
            public bool Judged;
            public int PinsAtLaunch;
            public int PinsHit;
            public readonly HashSet<Enemy> Hit = new HashSet<Enemy>();
        }

        static readonly Color k_Ice = new Color(0.65f, 0.9f, 1f, 1f);
        static readonly Color k_Gold = new Color(1f, 0.82f, 0.35f, 1f);

        readonly List<Pin> m_Pins = new List<Pin>();
        readonly List<Ball> m_Balls = new List<Ball>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        TextMeshPro m_BoardText;
        float m_BoardUntil;
        int m_Score;
        float m_NextBall;
        float m_NextSpare = -1f;
        bool m_NextBig;
        float m_NextPerfect;
        int m_PerfectLeft;
        float m_NextPerfectBall;
        int m_Hints;

        protected override void OnDestroy()
        {
            foreach (var ball in m_Balls)
            {
                if (ball.Body != null)
                    Destroy(ball.Body.gameObject);
            }

            foreach (var pin in m_Pins)
            {
                if (pin.Enemy != null)
                    pin.Enemy.Uncharm();
            }

            m_Balls.Clear();
            m_Pins.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateBalls();
            UpdateBoard();
            if (!IsBuilt || IsUnderConstruction)
                return;

            var lane = LaneNearest(transform.position, null, out var along, out _);
            if (lane == null)
                return;

            var level = Mathf.Max(1, Level);
            UpdatePins(lane, along);

            if (level >= 3 && UpdatePerfect(lane, along))
                return;

            Gather(lane, along, 1);
            if (m_NextSpare >= 0f && Time.time >= m_NextSpare)
            {
                m_NextSpare = -1f;
                Roll(lane, along, level, m_BallRadius, ValueFor(m_DamagePerLevel, level, 45f), true, false, false);
            }

            var waiting = Waiting();
            if ((Time.time >= m_NextBall && waiting >= 1) || (waiting >= 3 && Time.time >= m_NextBall - 0.5f * m_BallInterval / OverchargeRate))
            {
                var big = m_NextBig;
                m_NextBig = false;
                Roll(lane, along, level, m_BallRadius * (big ? 2f : 1f), ValueFor(m_DamagePerLevel, level, 45f) * (big ? 1.5f : 1f), false,
                     false, false, m_WindupTime);
                m_NextBall = Time.time + m_BallInterval / OverchargeRate;
            }
        }

        // ---------------------------------------------------------------- les quilles

        // La place d'une quille : le triangle (1, 2, 3, 4 quilles par rang), sa pointe vers l'arche ; les triangles suivants plus
        // loin vers la forêt (la partie parfaite).
        Vector3 SlotPosition(MapScan.Lane lane, float along, int slot)
        {
            var triangle = slot / 10;
            var index = slot % 10;
            var row = index < 1 ? 0 : index < 3 ? 1 : index < 6 ? 2 : 3;
            var first = row * (row + 1) / 2;
            var column = index - first;
            var distance = along - m_PinDistance - triangle * (4f * m_RowGap + 2.5f) - row * m_RowGap;
            var point = lane.PointAt(Mathf.Clamp(distance, 0f, lane.Length), out var direction);
            direction.y = 0f;
            var side = direction.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, direction.normalized) : Vector3.right;
            return point + side * ((column - row * 0.5f) * m_PinGap);
        }

        Vector3 PinPoint(MapScan.Lane lane, float along) => lane.PointAt(Mathf.Clamp(along - m_PinDistance, 0f, lane.Length), out _);

        bool IsPin(Enemy enemy)
        {
            foreach (var pin in m_Pins)
            {
                if (pin.Enemy == enemy)
                    return true;
            }

            return false;
        }

        int FreeSlot(int triangles)
        {
            for (var slot = 0; slot < triangles * 10; slot++)
            {
                var taken = false;
                foreach (var pin in m_Pins)
                {
                    if (pin.Slot == slot)
                    {
                        taken = true;
                        break;
                    }
                }

                if (!taken)
                    return slot;
            }

            return -1;
        }

        // Les ennemis au sol près des quilles prennent une place libre et y vont.
        void Gather(MapScan.Lane lane, float along, int triangles)
        {
            var center = PinPoint(lane, along);
            var radius = m_GatherRadius + (triangles - 1) * 4f;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(center, radius));
            foreach (var enemy in m_Scratch)
            {
                if (IsPin(enemy) || enemy.IsBoss || enemy.IsCharmed || !enemy.CanBeLaunched)
                    continue;

                var slot = FreeSlot(triangles);
                if (slot < 0)
                    return;
                if (!enemy.Charm(m_PinTime, SlotPosition(lane, along, slot)))
                    continue;

                m_Pins.Add(new Pin { Enemy = enemy, Slot = slot, Since = Time.time });
                FloatingText.Spawn(enemy.transform.position + Vector3.up * 2f, "Rangés !", k_Gold, 1.1f, 1.2f);
            }
        }

        void UpdatePins(MapScan.Lane lane, float along)
        {
            for (var i = m_Pins.Count - 1; i >= 0; i--)
            {
                var pin = m_Pins[i];
                if (pin.Enemy == null || !pin.Enemy.IsAlive || pin.Enemy.IsAirborne || !pin.Enemy.IsCharmed)
                {
                    m_Pins.RemoveAt(i);
                    continue;
                }

                pin.Enemy.SetCharmDestination(SlotPosition(lane, along, pin.Slot));
            }
        }

        // Les quilles qui attendent depuis 2,5 s (le temps d'aller à leur place) : la boule part à la fin de l'intervalle s'il y en
        // a une, dès la moitié s'il y en a trois.
        int Waiting()
        {
            var waiting = 0;
            foreach (var pin in m_Pins)
            {
                if (Time.time - pin.Since >= 2.5f)
                    waiting++;
            }

            return waiting;
        }

        // ---------------------------------------------------------------- les boules

        void Roll(MapScan.Lane lane, float along, int level, float radius, float damage, bool spare, bool giant, bool perfect,
                  float windup = 0f)
        {
            if (m_BallTemplate == null)
                return;

            var body = Instantiate(m_BallTemplate.gameObject).transform;
            body.name = giant ? "Boule géante" : "Boule de bowling";
            foreach (var behaviour in body.GetComponentsInChildren<MonoBehaviour>())
                Destroy(behaviour);
            body.localScale = m_BallTemplate.lossyScale * (2f * radius / Mathf.Max(0.01f, m_TemplateDiameter));
            body.gameObject.SetActive(true);
            var sphere = body.gameObject.AddComponent<SphereCollider>();
            sphere.radius = m_TemplateDiameter * 0.5f / Mathf.Max(0.01f, m_BallTemplate.lossyScale.x);
            var ball = new Ball
            {
                Body = body,
                Start = along,
                End = m_PinDistance + (perfect ? 3 : 1) * (4f * m_RowGap + 2.5f) + 4f,
                Speed = m_BallSpeed * (giant ? 0.6f : 1f),
                Radius = radius,
                Damage = damage,
                Spare = spare,
                Giant = giant,
                Perfect = perfect,
                PinsAtLaunch = m_Pins.Count,
                Scale = body.localScale,
                Wait = Mathf.Max(0f, windup),
            };
            ball.Target = body.gameObject.AddComponent<DuoShotTarget>();
            ball.Target.Shot = target => OnBallShot(ball);
            Place(ball, lane);
            m_Balls.Add(ball);
            if (ball.Wait > 0f)
            {
                // La mise en place : elle brille, et les premières fois, on dit au joueur qu'il peut tirer dedans. Les quilles
                // l'attendent (leur charme, 4 s, finirait avant qu'elle arrive).
                Burst(body.position, k_Gold, 25, 4f, 0.25f, 0.6f, 0.4f);
                foreach (var pin in m_Pins)
                {
                    if (pin.Enemy != null && pin.Enemy.IsAlive)
                        pin.Enemy.Charm(ball.Wait + 2f, SlotPosition(lane, along, pin.Slot));
                }
                if (m_Hints < 3)
                {
                    m_Hints++;
                    FloatingText.Spawn(body.position + Vector3.up * (radius + 1f), "Tire dans la boule !", k_Gold, 1.1f, ball.Wait);
                }
            }
            else
            {
                Sfx.Play(m_RollClip, body.position, 0.9f, giant ? 0.5f : Random.Range(0.85f, 1f));
            }
        }

        void OnBallShot(Ball ball)
        {
            if (ball.Body == null)
                return;

            // Pendant la mise en place (2 novembre, le joueur) : elle part tout de suite, plus grosse et plus forte.
            if (ball.Wait > 0f)
            {
                ball.Wait = 0f;
                ball.Charged = true;
                ball.Radius *= m_ChargedScale;
                ball.Damage *= m_ChargedDamage;
                ball.Scale *= m_ChargedScale;
                ball.Body.localScale = ball.Scale;
                FloatingText.Spawn(ball.Body.position + Vector3.up * (ball.Radius + 0.6f), "Boule chargée !", k_Gold, 1.4f, 1.2f);
                Burst(ball.Body.position, k_Gold, 50, 8f, 0.3f, 0.8f, 0.6f);
                Sfx.Play(m_RollClip, ball.Body.position, 1f, 0.75f);
                return;
            }

            if (ball.Frozen)
                return;
            ball.Frozen = true;
            ball.Speed *= m_ShotSpeedFactor;
            FloatingText.Spawn(ball.Body.position + Vector3.up * (ball.Radius + 0.6f), "Boule givrée !", k_Ice, 1.2f, 1.2f);
            Burst(ball.Body.position, k_Ice, 30, 6f, 0.25f, 0.6f, 0.4f);
        }

        // Elle suit la voie vers la forêt, posée sur le sol, et roule sur elle-même.
        void Place(Ball ball, MapScan.Lane lane)
        {
            var point = lane.PointAt(Mathf.Clamp(ball.Start - ball.Rolled, 0f, lane.Length), out var direction);
            point.y = GroundY(point, 1.5f) + ball.Radius;
            var previous = ball.Body.position;
            ball.Body.position = point;
            var moved = point - previous;
            moved.y = 0f;
            if (moved.sqrMagnitude > 1e-6f && moved.magnitude < 5f)
            {
                var axis = Vector3.Cross(Vector3.up, moved.normalized);
                ball.Body.Rotate(axis, moved.magnitude / Mathf.Max(0.1f, ball.Radius) * Mathf.Rad2Deg, Space.World);
            }
        }

        void UpdateBalls()
        {
            var lane = m_Balls.Count > 0 ? LaneNearest(transform.position, null, out _, out _) : null;
            for (var i = m_Balls.Count - 1; i >= 0; i--)
            {
                var ball = m_Balls[i];
                if (ball.Body == null || lane == null)
                {
                    if (ball.Body != null)
                        Destroy(ball.Body.gameObject);
                    m_Balls.RemoveAt(i);
                    continue;
                }

                // La mise en place : elle attend sous l'arche en tournant sur elle-même et en respirant, sans toucher personne.
                if (ball.Wait > 0f)
                {
                    ball.Wait -= Time.deltaTime;
                    Place(ball, lane);
                    ball.Body.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);
                    ball.Body.localScale = ball.Scale * (1f + 0.06f * Mathf.Sin(Time.time * 12f));
                    if (ball.Wait > 0f)
                        continue;

                    ball.Body.localScale = ball.Scale;
                    Sfx.Play(m_RollClip, ball.Body.position, 0.9f, Random.Range(0.85f, 1f));
                }

                ball.Rolled += ball.Speed * Time.deltaTime;
                Place(ball, lane);
                if (HitAlong(ball))
                {
                    m_Balls.RemoveAt(i);
                    continue;
                }

                if (!ball.Judged && ball.Rolled >= m_PinDistance + 4f * m_RowGap + 1f)
                    Judge(ball);

                if (ball.Rolled < ball.End && ball.Start - ball.Rolled > 0f)
                    continue;

                // Au bout : une boule givrée éclate quand même ; les autres disparaissent dans un nuage de neige.
                if (ball.Frozen)
                    Shatter(ball);
                else
                    Burst(ball.Body.position, k_Ice, 20, 4f, 0.4f, 0.6f, 0.3f);
                Destroy(ball.Body.gameObject);
                m_Balls.RemoveAt(i);
            }
        }

        // Ceux qu'elle touche volent dans tous les sens ; une boule givrée éclate au premier. Vrai si elle a disparu.
        bool HitAlong(Ball ball)
        {
            var center = ball.Body.position;
            var ground = center - Vector3.up * ball.Radius;
            var forest = ForestwardAt(center);
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(ground, ball.Radius + 0.5f));
            var shatter = false;
            foreach (var enemy in m_Scratch)
            {
                if (!ball.Hit.Add(enemy))
                    continue;

                if (ball.Frozen)
                {
                    shatter = true;
                    break;
                }

                if (IsPin(enemy))
                    ball.PinsHit++;
                Knock(ball, enemy, forest);
            }

            if (!shatter)
                return false;

            Shatter(ball);
            Destroy(ball.Body.gameObject);
            return true;
        }

        void Knock(Ball ball, Enemy enemy, Vector3 forest)
        {
            if (enemy.IsBoss)
            {
                if (ball.Giant)
                    enemy.Health.TakeDamage(new DamageInfo
                    {
                        Amount = enemy.Health.Max * m_GiantBossShare,
                        Zone = HitZone.Body,
                        Point = enemy.Center,
                        Direction = forest,
                        Source = this,
                    });
                else
                    Hurt(enemy, ball.Damage, enemy.Center, forest);
                return;
            }

            m_Score++;
            enemy.Uncharm();
            if (!Hurt(enemy, ball.Damage, enemy.Center, forest))
                return;

            var away = enemy.transform.position - ball.Body.position;
            away.y = 0f;
            var side = Vector3.Cross(Vector3.up, forest);
            var lateral = Vector3.Dot(away, side) >= 0f ? side : -side;
            var velocity = forest * m_KnockForward * Random.Range(0.6f, 1.3f) + lateral * m_KnockSide * Random.Range(0.4f, 1.2f) +
                           Vector3.up * m_KnockUp * Random.Range(0.8f, 1.25f) * (ball.Giant ? 1.4f : 1f);
            if (enemy.Launch(velocity, this))
                enemy.Tumble(new Vector3(Random.Range(-500f, 500f), Random.Range(-300f, 300f), Random.Range(-500f, 500f)));
        }

        // La boule givrée éclate : les éclats blessent et gèlent tout autour (« Brisé ! » sous ta flèche).
        void Shatter(Ball ball)
        {
            var center = ball.Body.position;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(center, m_ShatterRadius));
            foreach (var enemy in m_Scratch)
            {
                if (!Hurt(enemy, m_ShatterDamage, center, enemy.transform.position - center))
                    continue;
                if (enemy.Freeze(m_FreezeTime))
                    enemy.Prepare(Preparation.Frozen, m_FreezeTime);
            }

            Burst(center, k_Ice, 70, 11f, 0.35f, 0.9f, 0.8f);
            Burst(center, Color.white, 40, 7f, 0.2f, 0.7f, 0.5f);
            Sfx.Play(m_ShatterClip, center, 1f, Random.Range(0.9f, 1.05f));
            FloatingText.Spawn(center + Vector3.up * 1.5f, "Éclats de glace !", k_Ice, 1.4f, 1.4f);
        }

        // Passée les quilles : un strike si toutes celles qui attendaient sont tombées ; sinon, au niveau 2, le spare.
        void Judge(Ball ball)
        {
            ball.Judged = true;
            if (ball.Perfect || ball.PinsAtLaunch <= 0)
                return;

            var level = Mathf.Max(1, Level);
            var remaining = 0;
            foreach (var pin in m_Pins)
            {
                if (!ball.Hit.Contains(pin.Enemy))
                    remaining++;
            }

            if (!ball.Spare && ball.PinsAtLaunch >= 3 && remaining == 0)
            {
                Announce("STRIKE !", ball.Body.position);
                if (level >= 2)
                    m_NextBig = true;
                return;
            }

            if (ball.Spare && remaining == 0 && ball.PinsHit > 0)
            {
                Announce("SPARE !", ball.Body.position);
                return;
            }

            if (!ball.Spare && level >= 2 && remaining > 0)
                m_NextSpare = Time.time + 0.6f;
        }

        void Announce(string word, Vector3 point)
        {
            FloatingText.Spawn(point + Vector3.up * 3f, word, k_Gold, 3f, 2f);
            HeadMessages.Show(word, k_Gold, 1.5f);
            Sfx.Play(m_StrikeClip, point, 1f, Random.Range(0.95f, 1.05f));
            ShowOnBoard(word, 2f);
        }

        // ---------------------------------------------------------------- la partie parfaite (niveau 3)

        // Trois triangles, puis douze boules d'affilée, la dernière de 4 m ; vrai pendant qu'elle se joue.
        bool UpdatePerfect(MapScan.Lane lane, float along)
        {
            if (m_PerfectLeft > 0)
            {
                Gather(lane, along, 3);
                if (Time.time < m_NextPerfectBall)
                    return true;

                m_NextPerfectBall = Time.time + m_PerfectGap;
                var giant = m_PerfectLeft == 1;
                Roll(lane, along, 3, giant ? m_GiantRadius : m_BallRadius, giant ? m_GiantDamage : ValueFor(m_DamagePerLevel, 3, 75f),
                     false, giant, true);
                m_PerfectLeft--;
                if (m_PerfectLeft == 0)
                {
                    m_NextBall = Time.time + m_BallInterval / OverchargeRate;
                    ShowOnBoard("300", 6f);
                    HeadMessages.Show("Partie parfaite ! 300", k_Gold, 3f);
                }

                return true;
            }

            if (Time.time < m_NextPerfect || EnemiesNear(PinPoint(lane, along), m_GatherRadius + 8f).Count < 5)
                return false;

            m_NextPerfect = Time.time + m_PerfectInterval / OverchargeRate;
            m_PerfectLeft = Mathf.Max(1, m_PerfectBalls);
            Gather(lane, along, 3);
            // Le temps de ranger les trois triangles avant la première boule.
            m_NextPerfectBall = Time.time + 2f;
            HeadMessages.Show("La partie parfaite !", k_Gold, 2.5f);
            return true;
        }

        // ---------------------------------------------------------------- le tableau

        void ShowOnBoard(string text, float time)
        {
            EnsureBoard();
            if (m_BoardText == null)
                return;
            m_BoardText.text = text;
            m_BoardUntil = Time.time + time;
        }

        void EnsureBoard()
        {
            if (m_BoardText != null || m_Board == null)
                return;

            var go = new GameObject("Score du bowling");
            go.transform.SetParent(m_Board, false);
            m_BoardText = go.AddComponent<TextMeshPro>();
            m_BoardText.alignment = TextAlignmentOptions.Center;
            m_BoardText.textWrappingMode = TextWrappingModes.NoWrap;
            m_BoardText.fontStyle = FontStyles.Bold;
            var theme = UiTheme.Load();
            if (theme != null)
                UiThemer.ApplyTitle(m_BoardText, theme, false);
            m_BoardText.fontSize = 9f;
            m_BoardText.color = k_Gold;
            m_BoardText.outlineWidth = 0.2f;
            m_BoardText.outlineColor = new Color32(40, 22, 4, 230);
        }

        // Le score (les quilles tombées), ou le mot du moment ; le texte face au joueur.
        void UpdateBoard()
        {
            if (!IsBuilt)
                return;
            EnsureBoard();
            if (m_BoardText == null)
                return;

            if (Time.time >= m_BoardUntil)
                m_BoardText.text = m_Score.ToString();

            var camera = Camera.main;
            if (camera == null)
                return;
            var look = m_BoardText.transform.position - camera.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 1e-4f)
                m_BoardText.transform.rotation = Quaternion.LookRotation(look);
        }
    }
}
