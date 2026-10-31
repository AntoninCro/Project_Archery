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
    /// ses coups. Les boss et les volants ne viennent pas. La baudruche est une copie de celle du modèle (le pivot à ses pieds),
    /// avec un <see cref="DuoShotTarget"/> qui reçoit les flèches : crevée par une flèche, les confettis font moitié plus de dégâts.
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
        }

        class Lured
        {
            public Doll Doll;
            public float NextHit;
            public float NextCharm;
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
            var from = m_Muzzle != null ? m_Muzzle.position : transform.position + Vector3.up * 3f;
            var copy = Instantiate(m_DollTemplate.gameObject, from, Quaternion.identity);
            copy.name = giant ? "Char de carnaval" : "Baudruche";
            copy.SetActive(true);
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>())
                Destroy(behaviour);
            foreach (var collider in copy.GetComponentsInChildren<Collider>())
                Destroy(collider);

            var height = giant ? m_FloatHeight : ValueFor(m_HeightPerLevel, level, 2f);
            var forest = ForestwardAt(landing);
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
                Facing = Quaternion.LookRotation(forest),
                Hits = giant ? m_FloatHits : ValueFor(m_HitsPerLevel, level, 6),
                Phase = Random.value * 6f,
                Lane = lane,
                Along = along,
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
                doll.Transform.position = Vector3.Lerp(doll.From, doll.To, t) + Vector3.up * (4f * m_FlightHeight * t * (1f - t));
                doll.Transform.localScale = doll.Scale * Mathf.Lerp(0.3f, 1f, t);
                doll.Transform.rotation = doll.Facing * Quaternion.Euler(0f, 360f * 2f * (1f - t), 0f);
                if (t < 1f)
                    return true;

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

        // Les ennemis autour viennent la frapper ; le char de carnaval prend ceux des autres baudruches.
        void Lure(Doll doll)
        {
            var radius = doll.Giant ? m_FloatReach : ValueFor(m_LureRadiusPerLevel, doll.Level, 7f);
            var point = doll.Transform.position;
            foreach (var enemy in EnemiesNear(point, radius))
            {
                if (enemy.IsBoss)
                    continue;

                if (m_Lured.TryGetValue(enemy, out var lured))
                {
                    if (doll.Giant && !lured.Doll.Giant)
                        lured.Doll = doll;
                    continue;
                }

                if (enemy.Charm(1f, point))
                    m_Lured[enemy] = new Lured { Doll = doll, NextHit = Time.time + 0.4f, NextCharm = Time.time + 0.5f };
            }
        }

        // Chacun court vers sa baudruche ; à côté d'elle, il la frappe.
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
                if (Time.time >= lured.NextCharm)
                {
                    lured.NextCharm = Time.time + 0.5f;
                    enemy.Charm(1f, point);
                }

                enemy.SetCharmDestination(point);
                var flat = enemy.transform.position - point;
                flat.y = 0f;
                var reach = 1.4f + doll.Height * 0.24f;
                if (doll.Flying || doll.Bouncing || flat.sqrMagnitude > reach * reach || Time.time < lured.NextHit || enemy.IsAirborne)
                    continue;

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
            m_Scratch.AddRange(EnemiesNear(center, radius));
            foreach (var enemy in m_Scratch)
            {
                var amount = damage + (doll.Giant ? enemy.Health.Max * m_FloatShare * bonus / Mathf.Max(0.01f, Strength) : 0f);
                if (m_Lured.TryGetValue(enemy, out var lured) && lured.Doll == doll)
                {
                    enemy.Uncharm();
                    m_Lured.Remove(enemy);
                }

                if (Hurt(enemy, amount, enemy.Center, enemy.transform.position - center))
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
    }
}
