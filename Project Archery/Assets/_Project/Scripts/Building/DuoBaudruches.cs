using System.Collections.Generic;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using Archery.World;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Lanceur de baudruches</b> (Mortier + Bulle ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.18) : une grosse
    /// pompe de foire en cuivre et un canon à air, des baudruches attachées comme des ballons de foire. Il gonfle des poupées à ton
    /// image et les lance au milieu de la voie, devant les ennemis : ils s'arrêtent pour les frapper. Crevées, elles éclatent en
    /// confettis piquants ; ta flèche peut les crever avant, quand tout le monde est autour.
    /// <list type="bullet">
    /// <item>1 : une baudruche de 2 m, 6 coups avant d'éclater ;</item>
    /// <item>2 : frappée, elle rebondit plus loin vers la forêt et sur le côté, et les entraîne hors de leur chemin ;</item>
    /// <item>3, le <b>char de carnaval</b> : toutes les 20 s, une baudruche de 7 m que tous les ennemis à 40 m viennent frapper ;
    /// elle éclate en une pluie de confettis qui leur coûte une grosse part de leurs PV.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Venir la frapper, c'est le charme (<see cref="Enemy.Charm"/>) : l'ennemi court vers la baudruche, et c'est le duo qui compte
    /// ses coups. Les boss ne viennent pas. La baudruche est une copie de celle du modèle (le pivot à ses pieds), avec un
    /// <see cref="DuoShotTarget"/> qui reçoit les flèches : crevée par une flèche, les confettis font moitié plus de dégâts.
    /// Depuis le 1er novembre (le joueur : « les baudruches devraient aussi attirer les volants et les tireurs »), les volants
    /// viennent tourner autour de sa tête et la picorer (<see cref="Enemy.LureFlyer"/>), et les tireurs viennent de plus loin et
    /// lui tirent dessus de quelques mètres (<see cref="Enemy.ShootAt"/>) ; ils sont attirés de plus loin que les autres.
    /// </remarks>
    public class DuoBaudruches : TowerDuo
    {
        [Header("Le canon à air")]
        [Tooltip("Ce qui tourne vers la cible (autour de Y).")]
        [SerializeField]
        Transform m_Turret;

        [Tooltip("La gueule : la baudruche en part.")]
        [SerializeField]
        Transform m_Muzzle;

        [Tooltip("La baudruche du modèle (le pivot à ses pieds) : copiée à chaque tir.")]
        [SerializeField]
        Transform m_DollTemplate;

        [Tooltip("Hauteur (m) de la baudruche du modèle, à l'échelle 1.")]
        [SerializeField]
        float m_TemplateHeight = 1.8f;

        [SerializeField]
        float m_Range = 38f;

        [Tooltip("Temps (s) entre deux tirs, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 7f, 6f, 6f };

        [Tooltip("Baudruches posées en même temps, au plus (sans le char de carnaval).")]
        [SerializeField]
        int m_MaxDolls = 3;

        [SerializeField]
        float m_FlightTime = 1.1f;

        [SerializeField]
        float m_FlightHeight = 8f;

        [Header("La baudruche")]
        [Tooltip("Hauteur (m) de la baudruche gonflée, par niveau.")]
        [SerializeField]
        float[] m_HeightPerLevel = { 2f, 2.3f, 2.5f };

        [Tooltip("Rayon (m) d'où les ennemis viennent la frapper, par niveau.")]
        [SerializeField]
        float[] m_LureRadiusPerLevel = { 7f, 9f, 9f };

        [Tooltip("Coups qu'elle encaisse avant d'éclater, par niveau.")]
        [SerializeField]
        int[] m_HitsPerLevel = { 6, 8, 8 };

        [Tooltip("Temps (s) entre deux coups d'un même ennemi.")]
        [SerializeField]
        float m_HitInterval = 0.9f;

        [Tooltip("Au bout de ce temps (s), elle éclate d'elle-même.")]
        [SerializeField]
        float m_Lifetime = 12f;

        [Header("Les volants et les tireurs (1er novembre)")]
        [Tooltip("Les volants sont attirés de tant de fois plus loin que les autres (pas pour le char de carnaval, qui attire de " +
                 "partout).")]
        [SerializeField]
        float m_FlyerLureFactor = 2.5f;

        [Tooltip("Les tireurs aussi.")]
        [SerializeField]
        float m_RangedLureFactor = 2f;

        [Tooltip("Distance (m) d'où un tireur lui tire dessus.")]
        [SerializeField]
        float m_RangedReach = 8f;

        [Header("Les confettis")]
        [Tooltip("Dégâts des confettis piquants, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_ConfettiDamagePerLevel = { 35f, 45f, 50f };

        [SerializeField]
        float m_ConfettiRadius = 3.5f;

        [Tooltip("Les dégâts quand une flèche la crève (fois).")]
        [SerializeField]
        float m_ShotBonus = 1.5f;

        [Header("Le rebond (niveau 2)")]
        [Tooltip("Elle rebondit tous les tant de coups.")]
        [SerializeField]
        int m_HitsPerBounce = 3;

        [Tooltip("Distance (m) du rebond vers la forêt ; elle part aussi sur le côté, jusqu'à la moitié.")]
        [SerializeField]
        float m_BounceDistance = 4f;

        [Header("Le char de carnaval (niveau 3)")]
        [SerializeField]
        float m_FloatInterval = 20f;

        [SerializeField]
        float m_FloatHeight = 7f;

        [Tooltip("Rayon (m) d'où tous les ennemis viennent le frapper.")]
        [SerializeField]
        float m_FloatReach = 40f;

        [SerializeField]
        int m_FloatHits = 40;

        [SerializeField]
        float m_FloatLifetime = 20f;

        [SerializeField]
        float m_FloatConfettiRadius = 9f;

        [Tooltip("Dégâts des confettis du char ; plus cette part de leurs PV max.")]
        [SerializeField]
        float m_FloatDamage = 60f;

        [Range(0f, 1f)]
        [SerializeField]
        float m_FloatShare = 0.35f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_FireClip;

        [SerializeField]
        AudioClip m_SqueakClip;

        [SerializeField]
        AudioClip m_PopClip;

        static readonly Color[] k_Confetti =
        {
            new Color(1f, 0.3f, 0.35f, 1f), new Color(1f, 0.85f, 0.25f, 1f), new Color(0.3f, 0.8f, 1f, 1f),
            new Color(0.45f, 0.95f, 0.45f, 1f), new Color(1f, 0.5f, 0.9f, 1f),
        };

        class Doll
        {
            public Transform Transform;
            public DuoShotTarget Target;
            public bool Giant;
            public int Level;
            public bool Flying;
            public float Time;
            public Vector3 From;
            public Vector3 To;
            public float Height;
            public Vector3 Scale;
            public Quaternion Facing;
            public int Hits;
            public int Taken;
            public float Until;
            public float Squash;
            public float NextLure;
            public float NextSqueak;
            public float Phase;
            public MapScan.Lane Lane;
            public float Along;
            public bool Bouncing;
            public float BounceTime;
            public Vector3 BounceFrom;
            public Vector3 BounceTo;

            // Dans l'arène (3 novembre) : lancée sur MK, elle le suit en vol et éclate sur lui.
            public bool AtBoss;
            public Enemy Follow;
        }

        class Lured
        {
            public Doll Doll;
            public float NextHit;
            public float NextCharm;
            public bool Flyer;
            public bool Ranged;
        }

        readonly List<Doll> m_Dolls = new List<Doll>();
        readonly Dictionary<Enemy, Lured> m_Lured = new Dictionary<Enemy, Lured>();
        readonly List<Enemy> m_Ended = new List<Enemy>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        Enemy m_Target;
        MapScan.Lane m_TargetLane;
        float m_TargetAlong;
        float m_NextScan;
        float m_NextShot;
        float m_NextFloat;

        protected override void OnDestroy()
        {
            // Retiré (la vitrine, F4) : les baudruches s'en vont sans éclater.
            foreach (var pair in m_Lured)
            {
                if (pair.Key != null)
                    pair.Key.Uncharm();
            }

            m_Lured.Clear();
            foreach (var doll in m_Dolls)
            {
                if (doll.Transform != null)
                    Destroy(doll.Transform.gameObject);
            }

            m_Dolls.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            for (var i = m_Dolls.Count - 1; i >= 0; i--)
            {
                if (!Step(m_Dolls[i]))
                    m_Dolls.RemoveAt(i);
            }

            UpdateLured();
            if (!IsBuilt || m_DollTemplate == null)
                return;

            var level = Mathf.Max(1, Level);
            if (InArena)
            {
                UpdateArena(level);
                return;
            }

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

            // Le char de carnaval passe avant les baudruches.
            if (level >= 3 && Time.time >= m_NextFloat && !HasGiant())
            {
                m_NextFloat = Time.time + m_FloatInterval / OverchargeRate;
                m_NextShot = Time.time + 2f;
                Fire(m_TargetLane, Mathf.Min(m_TargetLane.Length, m_TargetAlong + 6f), level, true);
                return;
            }

            if (Time.time < m_NextShot || SmallCount() >= m_MaxDolls)
                return;
            m_NextShot = Time.time + ValueFor(m_IntervalPerLevel, level, 7f) / OverchargeRate;
            Fire(m_TargetLane, Mathf.Min(m_TargetLane.Length, m_TargetAlong + 5f), level, false);
            m_Target = null;
        }

        // Dans l'arène de MK (3 novembre) : pas de voie où les poser, et un boss ne vient pas les frapper. Le canon les lance sur MK,
        // où elles éclatent en confettis piquants ; au niveau 3, le char de carnaval aussi (une part de ses PV max : 2 % au plus).
        void UpdateArena(int level)
        {
            if (Time.time >= m_NextScan)
            {
                m_NextScan = Time.time + 0.25f;
                m_Target = NearestEnemy(m_Range * RangeScale);
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

            if (level >= 3 && Time.time >= m_NextFloat)
            {
                m_NextFloat = Time.time + m_FloatInterval / OverchargeRate;
                m_NextShot = Time.time + 2f;
                FireAt(m_Target, level, true);
                return;
            }

            if (Time.time < m_NextShot)
                return;
            m_NextShot = Time.time + ValueFor(m_IntervalPerLevel, level, 7f) / OverchargeRate;
            FireAt(m_Target, level, false);
        }

        bool HasGiant()
        {
            foreach (var doll in m_Dolls)
            {
                if (doll.Giant)
                    return true;
            }

            return false;
        }

        int SmallCount()
        {
            var count = 0;
            foreach (var doll in m_Dolls)
            {
                if (!doll.Giant)
                    count++;
            }

            return count;
        }

        // Le premier ennemi au sol à portée, pas déjà occupé à frapper : le plus avancé vers la tour sur sa voie.
        Enemy FrontEnemy(out MapScan.Lane lane, out float along)
        {
            lane = null;
            along = 0f;
            Enemy best = null;
            var bestAlong = float.MinValue;
            foreach (var enemy in EnemiesNear(transform.position, m_Range))
            {
                if (enemy.IsBoss || m_Lured.ContainsKey(enemy))
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

        // Une baudruche gonflée en vol, lancée devant l'ennemi (vers la tour) : elle se pose face à la forêt.
        void Fire(MapScan.Lane lane, float along, int level, bool giant)
        {
            var landing = lane.PointAt(along, out _);
            landing.y = GroundY(landing, 1.5f);
            var doll = SpawnDoll(level, giant, landing, Quaternion.LookRotation(ForestwardAt(landing)));
            doll.Lane = lane;
            doll.Along = along;
        }

        // Dans l'arène (3 novembre) : lancée sur MK ; son milieu le rejoint (là où elle éclate).
        void FireAt(Enemy target, int level, bool giant)
        {
            var height = giant ? m_FloatHeight : ValueFor(m_HeightPerLevel, level, 2f);
            var flat = target.transform.position - transform.position;
            flat.y = 0f;
            var doll = SpawnDoll(level, giant, target.Center - Vector3.up * (height * 0.4f),
                                 Quaternion.LookRotation(flat.sqrMagnitude > 1e-4f ? -flat.normalized : Vector3.forward));
            doll.AtBoss = true;
            doll.Follow = target;
        }

        Doll SpawnDoll(int level, bool giant, Vector3 landing, Quaternion facing)
        {
            var from = m_Muzzle != null ? m_Muzzle.position : transform.position + Vector3.up * 3f;
            var copy = Instantiate(m_DollTemplate.gameObject, from, Quaternion.identity);
            copy.name = giant ? "Char de carnaval" : "Baudruche";
            copy.SetActive(true);
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>())
                Destroy(behaviour);
            foreach (var collider in copy.GetComponentsInChildren<Collider>())
                Destroy(collider);

            var height = giant ? m_FloatHeight : ValueFor(m_HeightPerLevel, level, 2f);
            var doll = new Doll
            {
                Transform = copy.transform,
                Giant = giant,
                Level = level,
                Flying = true,
                From = from,
                To = landing,
                Height = height,
                Scale = m_DollTemplate.lossyScale * (height / Mathf.Max(0.01f, m_TemplateHeight)),
                Facing = facing,
                Hits = giant ? m_FloatHits : ValueFor(m_HitsPerLevel, level, 6),
                Phase = Random.value * 6f,
            };

            // Le collider, dans les unités du modèle (la copie est mise à l'échelle) : une gélule de toute sa hauteur.
            var local = m_TemplateHeight / Mathf.Max(0.01f, m_DollTemplate.lossyScale.y);
            var capsule = copy.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, local * 0.5f, 0f);
            capsule.height = local;
            capsule.radius = local * 0.24f;
            doll.Target = copy.AddComponent<DuoShotTarget>();
            doll.Target.Shot = OnDollShot;
            copy.transform.localScale = doll.Scale * 0.3f;
            m_Dolls.Add(doll);
            Sfx.Play(m_FireClip, from, 1f, giant ? 0.6f : Random.Range(0.95f, 1.1f));
            return doll;
        }

        bool Step(Doll doll)
        {
            if (doll.Transform == null)
                return false;

            // En vol : elle se gonfle et tournoie.
            if (doll.Flying)
            {
                doll.Time += Time.deltaTime;
                var t = Mathf.Clamp01(doll.Time / m_FlightTime);
                if (doll.AtBoss && doll.Follow != null && doll.Follow.IsAlive)
                    doll.To = doll.Follow.Center - Vector3.up * (doll.Height * 0.4f);
                doll.Transform.position = Arc(doll.From, doll.To, t, m_FlightHeight);
                doll.Transform.localScale = doll.Scale * Mathf.Lerp(0.3f, 1f, t);
                doll.Transform.rotation = doll.Facing * Quaternion.Euler(0f, 360f * 2f * (1f - t), 0f);
                if (t < 1f)
                    return true;
                if (doll.AtBoss)
                {
                    Pop(doll, 1f);
                    return false;
                }

                doll.Flying = false;
                doll.Until = Time.time + (doll.Giant ? m_FloatLifetime : m_Lifetime);
                Confetti(doll.To + Vector3.up * 0.3f, doll.Giant ? 30 : 10, 4f, 0.2f);
                Sfx.Play(m_SqueakClip, doll.To, 0.8f, doll.Giant ? 0.6f : 1.1f);
                if (doll.Giant)
                {
                    FloatingText.Spawn(doll.To + Vector3.up * (doll.Height + 0.8f), "Le char de carnaval !", k_Confetti[1], 2.2f, 2.5f);
                    HeadMessages.Show("Le char de carnaval !", k_Confetti[1], 2.5f, 0.8f);
                }

                return true;
            }

            // Un rebond (niveau 2) : une cloche vers sa nouvelle place.
            var ground = doll.Transform.position;
            if (doll.Bouncing)
            {
                doll.BounceTime += Time.deltaTime;
                var t = Mathf.Clamp01(doll.BounceTime / 0.5f);
                ground = Vector3.Lerp(doll.BounceFrom, doll.BounceTo, t) + Vector3.up * (4f * 2f * t * (1f - t));
                if (t >= 1f)
                    doll.Bouncing = false;
            }

            // Posée : elle se balance, et s'écrase à chaque coup.
            doll.Squash = Mathf.MoveTowards(doll.Squash, 0f, Time.deltaTime * 3f);
            var wobble = Mathf.Sin(Time.time * 3f + doll.Phase) * (doll.Giant ? 4f : 7f) + doll.Squash * 12f * Mathf.Sin(Time.time * 25f);
            doll.Transform.position = ground;
            doll.Transform.rotation = doll.Facing * Quaternion.Euler(wobble, 0f, wobble * 0.5f);
            doll.Transform.localScale = Vector3.Scale(doll.Scale,
                new Vector3(1f + doll.Squash * 0.22f, 1f - doll.Squash * 0.18f, 1f + doll.Squash * 0.22f));

            if (Time.time >= doll.NextLure)
            {
                doll.NextLure = Time.time + 0.25f;
                Lure(doll);
            }

            if (doll.Hits > 0 && Time.time < doll.Until)
                return true;

            Pop(doll, 1f);
            return false;
        }

        // Les ennemis autour viennent la frapper ; le char de carnaval prend ceux des autres baudruches. Les volants et les tireurs
        // viennent de plus loin (sauf pour le char, qui attire de partout).
        void Lure(Doll doll)
        {
            var radius = doll.Giant ? m_FloatReach : ValueFor(m_LureRadiusPerLevel, doll.Level, 7f);
            var widest = doll.Giant ? 1f : Mathf.Max(1f, Mathf.Max(m_FlyerLureFactor, m_RangedLureFactor));
            var point = doll.Transform.position;
            foreach (var enemy in EnemiesNear(point, radius * widest, false))
            {
                if (enemy.IsBoss || enemy.IsAirborne)
                    continue;

                var flyer = enemy.IsFlyer;
                var ranged = !flyer && enemy.IsRanged;
                var reach = radius * (doll.Giant ? 1f : flyer ? m_FlyerLureFactor : ranged ? m_RangedLureFactor : 1f);
                var flat = enemy.transform.position - point;
                flat.y = 0f;
                if (flat.sqrMagnitude > reach * reach)
                    continue;

                if (m_Lured.TryGetValue(enemy, out var lured))
                {
                    if (doll.Giant && !lured.Doll.Giant)
                        lured.Doll = doll;
                    continue;
                }

                var drawn = flyer ? enemy.LureFlyer(AirPoint(doll), 1f) : enemy.Charm(1f, point, ranged ? m_RangedReach - 1f : -1f);
                if (drawn)
                    m_Lured[enemy] = new Lured
                    {
                        Doll = doll, NextHit = Time.time + 0.4f, NextCharm = Time.time + 0.5f, Flyer = flyer, Ranged = ranged,
                    };
            }
        }

        // Là où les volants tournent : au-dessus de sa tête.
        static Vector3 AirPoint(Doll doll) => doll.Transform.position + Vector3.up * (doll.Height + 1.2f);

        // Chacun court vers sa baudruche ; à côté d'elle, il la frappe (un volant : il la picore en passant ; un tireur : il lui
        // tire dessus de quelques mètres, et le coup compte quand le projectile arrive).
        void UpdateLured()
        {
            if (m_Lured.Count == 0)
                return;

            m_Ended.Clear();
            foreach (var pair in m_Lured)
            {
                var enemy = pair.Key;
                var lured = pair.Value;
                var doll = lured.Doll;
                if (enemy == null || !enemy.IsAlive || doll.Transform == null)
                {
                    m_Ended.Add(enemy);
                    continue;
                }

                var point = doll.Transform.position;
                var flat = enemy.transform.position - point;
                flat.y = 0f;
                var reach = 1.4f + doll.Height * 0.24f;
                if (lured.Flyer)
                {
                    // Le volant suit la baudruche (elle rebondit) ; il la picore quand il passe près de sa tête.
                    if (!enemy.LureFlyer(AirPoint(doll), 1f))
                    {
                        m_Ended.Add(enemy);
                        continue;
                    }

                    reach += 2.5f;
                }
                else
                {
                    if (Time.time >= lured.NextCharm)
                    {
                        lured.NextCharm = Time.time + 0.5f;
                        enemy.Charm(1f, point, lured.Ranged ? m_RangedReach - 1f : -1f);
                    }

                    enemy.SetCharmDestination(point);
                    if (lured.Ranged)
                        reach = m_RangedReach + 1f;
                }

                if (doll.Flying || doll.Bouncing || flat.sqrMagnitude > reach * reach || Time.time < lured.NextHit || enemy.IsAirborne)
                    continue;

                if (lured.Ranged)
                {
                    lured.NextHit = Time.time + m_HitInterval * Random.Range(1.4f, 1.8f);
                    var hitter = enemy;
                    var target = doll;
                    enemy.ShootAt(point + Vector3.up * doll.Height * 0.5f, () =>
                    {
                        if (target.Transform != null && !target.Flying && target.Hits > 0)
                            HitDoll(target, hitter);
                    });
                    continue;
                }

                lured.NextHit = Time.time + m_HitInterval * Random.Range(0.85f, 1.15f);
                enemy.PlayAttackAnimation();
                HitDoll(doll, enemy);
            }

            foreach (var enemy in m_Ended)
            {
                if (enemy != null)
                    enemy.Uncharm();
                m_Lured.Remove(enemy);
            }
        }

        void HitDoll(Doll doll, Enemy hitter)
        {
            doll.Hits--;
            doll.Taken++;
            doll.Squash = 1f;
            var point = doll.Transform.position + Vector3.up * doll.Height * 0.5f;
            Confetti(point, 3, 2f, 0.12f);
            if (Time.time >= doll.NextSqueak)
            {
                doll.NextSqueak = Time.time + 0.15f;
                Sfx.Play(m_SqueakClip, point, 0.6f, doll.Giant ? Random.Range(0.55f, 0.7f) : Random.Range(1f, 1.3f));
            }

            if (doll.Level >= 2 && !doll.Giant && doll.Hits > 0 && doll.Taken % Mathf.Max(1, m_HitsPerBounce) == 0)
                Bounce(doll);
        }

        // Elle rebondit vers la forêt et sur le côté de la voie ; ceux qui la frappaient sont entraînés avec elle.
        void Bounce(Doll doll)
        {
            doll.Along = Mathf.Max(0f, doll.Along - m_BounceDistance);
            var point = doll.Transform.position;
            var direction = Vector3.forward;
            if (doll.Lane != null)
                point = doll.Lane.PointAt(doll.Along, out direction);
            direction.y = 0f;
            var side = direction.sqrMagnitude > 1e-4f ? Vector3.Cross(Vector3.up, direction.normalized) : Vector3.right;
            point += side * Random.Range(-0.5f, 0.5f) * m_BounceDistance;
            point.y = GroundY(point, 1.5f);
            doll.Bouncing = true;
            doll.BounceTime = 0f;
            doll.BounceFrom = doll.Transform.position;
            doll.BounceTo = point;

            var drag = point - doll.BounceFrom;
            drag.y = 0f;
            foreach (var pair in m_Lured)
            {
                if (pair.Value.Doll == doll && pair.Key != null && drag.sqrMagnitude > 0.01f)
                    pair.Key.Push(drag.normalized * 5f);
            }

            FloatingText.Spawn(doll.BounceFrom + Vector3.up * (doll.Height + 0.4f), "Boing !", k_Confetti[Random.Range(0, k_Confetti.Length)],
                               1f, 0.9f);
        }

        void OnDollShot(DuoShotTarget target)
        {
            for (var i = 0; i < m_Dolls.Count; i++)
            {
                var doll = m_Dolls[i];
                if (doll.Target != target || doll.Flying)
                    continue;
                FloatingText.Spawn(doll.Transform.position + Vector3.up * (doll.Height + 0.6f), "Pop !", k_Confetti[0], 1.6f, 1.2f);
                Pop(doll, m_ShotBonus);
                m_Dolls.RemoveAt(i);
                return;
            }
        }

        // Elle éclate en confettis piquants : tous ceux qui sont autour sont blessés et renversés.
        void Pop(Doll doll, float bonus)
        {
            if (doll.Transform == null)
                return;

            var center = doll.Transform.position + Vector3.up * doll.Height * 0.4f;
            var radius = doll.Giant ? m_FloatConfettiRadius : m_ConfettiRadius;
            var damage = (doll.Giant ? m_FloatDamage : ValueFor(m_ConfettiDamagePerLevel, doll.Level, 35f)) * bonus;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(center, radius, false));
            foreach (var enemy in m_Scratch)
            {
                // Les volants qui tournaient autour sont blessés aussi (ni renversés, ni ceux qui sont déjà en l'air).
                if (enemy.IsAirborne)
                    continue;

                var amount = damage + (doll.Giant ? enemy.Health.Max * ShareFor(enemy, m_FloatShare) * bonus / Mathf.Max(0.01f, Strength) : 0f);
                if (m_Lured.TryGetValue(enemy, out var lured) && lured.Doll == doll)
                {
                    enemy.Uncharm();
                    m_Lured.Remove(enemy);
                }

                if (Hurt(enemy, amount, enemy.Center, enemy.transform.position - center) && !(InArena && enemy.IsBoss))
                    KnockDown(enemy, center, doll.Giant ? 6f : 3f, doll.Giant ? 7f : 4f);
            }

            // Ceux qui venaient la frapper de plus loin reprennent leur route.
            m_Ended.Clear();
            foreach (var pair in m_Lured)
            {
                if (pair.Value.Doll == doll)
                    m_Ended.Add(pair.Key);
            }

            foreach (var enemy in m_Ended)
            {
                if (enemy != null)
                    enemy.Uncharm();
                m_Lured.Remove(enemy);
            }

            Confetti(center, doll.Giant ? 90 : 28, doll.Giant ? 14f : 7f, doll.Giant ? 0.5f : 0.28f);
            Sfx.Play(m_PopClip, center, 1f, doll.Giant ? 0.8f : Random.Range(1.5f, 1.8f));
            if (doll.Giant)
            {
                FloatingText.Spawn(center + Vector3.up * 3f, "Confettis !", k_Confetti[1], 2.4f, 2.5f);
                Confetti(center + Vector3.up * 2f, 60, 20f, 0.35f);
            }

            Destroy(doll.Transform.gameObject);
            doll.Transform = null;
        }

        // Des confettis de toutes les couleurs (une gerbe par couleur).
        static void Confetti(Vector3 point, int perColor, float speed, float size)
        {
            foreach (var color in k_Confetti)
                Burst(point, color, perColor, speed, size, 1.4f, 0.5f);
        }

        /// <summary>La portée dessinée pendant la pose (3 novembre, le placement lisible).</summary>
        public override float ReachAt(int level) => m_Range;

        /// <summary>Il rejoint l'arène de MK (3 novembre) : ses baudruches y éclatent sur MK (<see cref="UpdateArena"/>).</summary>
        public override bool FightsInArena => true;
    }
}
