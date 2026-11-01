using System;
using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Economy;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Archery.Building
{
    /// <summary>
    /// Le trio <b>Gonfleur</b> (Lanceur de baudruches + Baliste, 1er novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.19) : une
    /// pompe de foire géante sur un char, et sa baliste-harpon. Le harpon plante un tuyau dans un ennemi, la pompe le <b>gonfle
    /// comme une baudruche</b> : il enfle et s'envole doucement (« Au vol ! »). Ta flèche le crève : il éclate en confettis, qui
    /// blessent ceux du dessous, et retombe ; sinon, il crève tout seul au bout de 6 s et retombe.
    /// <list type="bullet">
    /// <item>1 : un tuyau à la fois ;</item>
    /// <item>2 : deux tuyaux ; un ennemi crevé souffle son air sur ses voisins, qui gonflent à leur tour (une fois) ;</item>
    /// <item>3, <b>le défilé</b> : en plus, toutes les 18 s, toute la portée gonfle d'un coup ; le ciel se remplit
    /// d'ennemis-ballons, plus haut et plus longtemps, et les gros deviennent des chars de parade ; un ballon crevé par ta flèche
    /// rapporte deux fois ses points.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Gonfler, c'est agrandir l'ennemi entier (sa racine, ses zones de touche avec : une cible plus facile), puis le faire flotter
    /// comme dans une bulle (<see cref="Enemy.Float"/>) : il retombe dès qu'il est blessé (la règle de la chute). Les boss et les
    /// volants ne sont jamais gonflés ; un ennemi déjà déformé par une autre fusion (aplati) non plus.
    /// </remarks>
    public class TrioGonfleur : TowerTrio
    {
        [Header("La pompe et le harpon")]
        [Tooltip("Le bout du harpon : les tuyaux en partent.")]
        [SerializeField]
        Transform m_Nozzle;

        [Tooltip("La baliste-harpon : elle se tourne vers sa cible (autour de la verticale).")]
        [SerializeField]
        Transform m_Ballista;

        [Tooltip("Le piston de la pompe : il monte et descend tant qu'elle gonfle.")]
        [SerializeField]
        Transform m_Piston;

        [Tooltip("Le matériau des tuyaux.")]
        [SerializeField]
        Material m_HoseMaterial;

        [Tooltip("Portée (m, à plat, autour du char).")]
        [SerializeField]
        float m_Range = 18f;

        [Header("Les ballons")]
        [Tooltip("Temps (s) entre deux tuyaux, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 3.5f, 3f, 2.5f };

        [Tooltip("Tuyaux à la fois, par niveau.")]
        [SerializeField]
        int[] m_HosesPerLevel = { 1, 2, 2 };

        [Tooltip("Dégâts du harpon, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_HarpoonDamagePerLevel = { 15f, 20f, 25f };

        [Tooltip("Temps (s) pour gonfler un ennemi (il ne bouge plus pendant ce temps).")]
        [SerializeField]
        float m_InflateTime = 1.5f;

        [Tooltip("Sa taille une fois gonflé (fois la sienne).")]
        [SerializeField]
        float m_InflatedSize = 1.7f;

        [Tooltip("Il monte de tant de mètres, à cette vitesse (m/s), et crève tout seul au bout de tant de secondes.")]
        [SerializeField]
        float m_FloatHeight = 7f;

        [SerializeField]
        float m_RiseSpeed = 1.6f;

        [SerializeField]
        float m_FloatTime = 6f;

        [Header("Crevé")]
        [Tooltip("Crevé (par une flèche, un coup) : les confettis blessent ceux qui sont dessous, dans ce rayon (m), par niveau.")]
        [SerializeField]
        float m_PopRadius = 3f;

        [SerializeField]
        float[] m_PopDamagePerLevel = { 30f, 40f, 50f };

        [Tooltip("Niveau 2 : un ennemi crevé souffle son air sur ses voisins dans ce rayon (m), au plus tant d'entre eux.")]
        [SerializeField]
        float m_BlowRadius = 4f;

        [SerializeField]
        int m_BlowCount = 2;

        [Header("Le défilé (niveau 3)")]
        [Tooltip("Temps (s) entre deux défilés ; il faut au moins quatre ennemis à portée.")]
        [SerializeField]
        float m_ParadeInterval = 18f;

        [SerializeField]
        int m_ParadeMax = 16;

        [SerializeField]
        float m_ParadeHeight = 10f;

        [SerializeField]
        float m_ParadeTime = 8f;

        [Tooltip("Un gros ennemi (au moins tant de mètres de haut) devient un char de parade, de cette taille (fois la sienne).")]
        [SerializeField]
        float m_BigHeight = 2.4f;

        [SerializeField]
        float m_ParadeBigSize = 2.6f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_HarpoonClip;

        [SerializeField]
        AudioClip m_InflateClip;

        [SerializeField]
        AudioClip m_PopClip;

        enum Phase
        {
            Hose,
            Inflate,
            Float,
            Deflate,
        }

        class Balloon
        {
            public Enemy Enemy;
            public Vector3 Scale;
            public float Size;
            public Phase Phase;
            public float Time;
            public float Inflate;
            public float Height;
            public float FloatTime;
            public float FloatEnd;
            public LineRenderer Hose;
            public bool Struck;
            public bool Chained;
            public bool Parade;
            public Action<Health, DamageInfo> OnDamaged;
        }

        static readonly Color[] k_Confetti =
        {
            new Color(1f, 0.3f, 0.35f, 1f), new Color(1f, 0.85f, 0.25f, 1f), new Color(0.3f, 0.8f, 1f, 1f),
            new Color(0.45f, 0.95f, 0.45f, 1f), new Color(1f, 0.5f, 0.9f, 1f),
        };

        const float k_HoseTime = 0.25f;

        readonly List<Balloon> m_Balloons = new List<Balloon>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        Vector3 m_PistonRest;
        float m_PistonPhase;
        float m_NextShot;
        float m_NextParade;

        protected override void Awake()
        {
            base.Awake();
            if (m_Piston != null)
                m_PistonRest = m_Piston.localPosition;
        }

        protected override void OnDestroy()
        {
            foreach (var balloon in m_Balloons)
                Forget(balloon, true);
            m_Balloons.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateBalloons();
            AnimatePump();
            if (!IsBuilt || IsUnderConstruction)
                return;

            var level = Mathf.Max(1, Level);
            if (level >= 3 && Time.time >= m_NextParade && Parade())
                return;

            if (Time.time < m_NextShot || HosesInUse() >= ValueFor(m_HosesPerLevel, level, 1))
                return;

            var target = FindTarget();
            if (target == null)
                return;

            Inflate(target, level, m_InflateTime, m_InflatedSize, m_FloatHeight, m_FloatTime, false, false);
            m_NextShot = Time.time + ValueFor(m_IntervalPerLevel, level, 3.5f) / OverchargeRate;
        }

        // ---------------------------------------------------------------- viser, gonfler

        bool CanInflate(Enemy enemy) =>
            enemy != null && enemy.IsAlive && enemy.CanBeLaunched && !enemy.IsAirborne && !s_Reshaped.Contains(enemy);

        // L'ennemi au sol le plus avancé vers la tour du joueur, parmi ceux à portée.
        Enemy FindTarget()
        {
            Enemy best = null;
            var bestScore = float.MaxValue;
            var tower = Archery.Defense.Tower.Instance;
            foreach (var enemy in EnemiesNear(transform.position, m_Range))
            {
                if (!CanInflate(enemy))
                    continue;

                var score = tower != null ? (enemy.transform.position - tower.transform.position).sqrMagnitude : 0f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            return best;
        }

        int HosesInUse()
        {
            var count = 0;
            foreach (var balloon in m_Balloons)
            {
                if (!balloon.Parade && !balloon.Chained && (balloon.Phase == Phase.Hose || balloon.Phase == Phase.Inflate))
                    count++;
            }

            return count;
        }

        // Le tuyau part vers lui (sauf un voisin gonflé par le souffle : pas de tuyau), puis la pompe le gonfle.
        void Inflate(Enemy enemy, int level, float inflateTime, float size, float height, float floatTime, bool chained, bool parade)
        {
            if (!s_Reshaped.Add(enemy))
                return;

            var balloon = new Balloon
            {
                Enemy = enemy,
                Scale = enemy.transform.localScale,
                Size = size,
                Phase = chained ? Phase.Inflate : Phase.Hose,
                Inflate = Mathf.Max(0.1f, inflateTime),
                Height = height,
                FloatTime = floatTime,
                Chained = chained,
                Parade = parade,
            };
            balloon.OnDamaged = (health, info) => balloon.Struck = true;
            if (!chained)
                balloon.Hose = NewHose();
            m_Balloons.Add(balloon);

            if (chained)
                StartInflating(balloon, level);
            else
                Sfx.Play(m_HarpoonClip, Nozzle, 0.8f, Random.Range(0.95f, 1.1f));
        }

        void StartInflating(Balloon balloon, int level)
        {
            balloon.Phase = Phase.Inflate;
            balloon.Time = 0f;
            var enemy = balloon.Enemy;
            if (!balloon.Chained)
                Hurt(enemy, ValueFor(m_HarpoonDamagePerLevel, level, 15f), enemy.Center, enemy.Center - Nozzle);
            if (enemy != null && enemy.IsAlive)
                enemy.Stun(balloon.Inflate + 0.1f);
            Sfx.Play(m_InflateClip, enemy != null ? enemy.Center : Nozzle, 0.7f, Random.Range(0.9f, 1.1f));
        }

        Vector3 Nozzle => m_Nozzle != null ? m_Nozzle.position : transform.position + Vector3.up * 3f;

        void UpdateBalloons()
        {
            for (var i = m_Balloons.Count - 1; i >= 0; i--)
            {
                var balloon = m_Balloons[i];
                if (balloon.Enemy == null || (!balloon.Enemy.IsAlive && balloon.Phase != Phase.Float && balloon.Phase != Phase.Deflate))
                {
                    Forget(balloon, true);
                    m_Balloons.RemoveAt(i);
                    continue;
                }

                balloon.Time += Time.deltaTime;
                if (!Step(balloon))
                {
                    Forget(balloon, false);
                    m_Balloons.RemoveAt(i);
                }
            }
        }

        // Une image de la vie d'un ballon ; faux quand il est fini.
        bool Step(Balloon balloon)
        {
            var enemy = balloon.Enemy;
            switch (balloon.Phase)
            {
                case Phase.Hose:
                {
                    // Le tuyau file vers lui, au bout du harpon.
                    var t = Mathf.Clamp01(balloon.Time / k_HoseTime);
                    SetHose(balloon, Vector3.Lerp(Nozzle, enemy.Center, t));
                    AimAt(enemy.transform.position);
                    if (t >= 1f)
                        StartInflating(balloon, Mathf.Max(1, Level));
                    return true;
                }

                case Phase.Inflate:
                {
                    // Il enfle, en tremblant, accroché au tuyau ; puis le tuyau se décroche et il s'envole.
                    var t = Mathf.Clamp01(balloon.Time / balloon.Inflate);
                    var wobble = 1f + Mathf.Sin(balloon.Time * 22f) * 0.04f * (1f - t);
                    SetScale(balloon, Mathf.Lerp(1f, balloon.Size, Mathf.SmoothStep(0f, 1f, t)) * wobble);
                    if (balloon.Hose != null)
                        SetHose(balloon, enemy.Center);
                    if (t < 1f)
                        return true;

                    DropHose(balloon);
                    if (!enemy.IsAlive || !enemy.Float(balloon.Height, balloon.FloatTime, m_RiseSpeed * (balloon.Parade ? 1.5f : 1f), this))
                    {
                        balloon.Phase = Phase.Deflate;
                        balloon.Time = 0f;
                        return true;
                    }

                    balloon.Phase = Phase.Float;
                    balloon.Time = 0f;
                    balloon.FloatEnd = Time.time + balloon.FloatTime;
                    balloon.Struck = false;
                    enemy.Health.Damaged += balloon.OnDamaged;
                    FloatingText.Spawn(enemy.Center + Vector3.up * 1.5f, balloon.Parade ? "Défilé !" : "Gonflé !",
                                       new Color(1f, 0.55f, 0.75f, 1f), 1.3f, 1.4f);
                    return true;
                }

                case Phase.Float:
                {
                    // Il se balance doucement ; crevé (blessé), ou au bout de son temps, il retombe.
                    SetScale(balloon, balloon.Size * (1f + Mathf.Sin(balloon.Time * 2.4f) * 0.03f));
                    if (enemy.IsAlive && enemy.IsFloating)
                        return true;

                    Popped(balloon);
                    balloon.Phase = Phase.Deflate;
                    balloon.Time = 0f;
                    return true;
                }

                default:
                {
                    // Il se dégonfle en 0,25 s.
                    var t = Mathf.Clamp01(balloon.Time / 0.25f);
                    SetScale(balloon, Mathf.Lerp(balloon.Size, 1f, t));
                    return t < 1f;
                }
            }
        }

        // Crevé : des confettis ; blessé avant la fin (une flèche, un coup), les confettis blessent ceux du dessous, et au niveau 2
        // il souffle son air sur ses voisins. Crevé par ta flèche pendant le défilé : deux fois ses points.
        void Popped(Balloon balloon)
        {
            var enemy = balloon.Enemy;
            var point = enemy.Center;
            var struck = balloon.Struck || Time.time < balloon.FloatEnd - 0.1f;
            var level = Mathf.Max(1, Level);
            foreach (var color in k_Confetti)
                Burst(point, color, struck ? 14 : 6, struck ? 9f : 5f, 0.18f, 1.4f, 0.5f);
            Sfx.Play(m_PopClip, point, 1f, Random.Range(0.9f, 1.15f));
            if (!struck)
                return;

            FloatingText.Spawn(point + Vector3.up * 0.6f, "Pan !", new Color(1f, 0.85f, 0.3f, 1f), 1.4f, 1.2f);
            var ground = point;
            ground.y = GroundY(point, 1.5f);
            var damage = ValueFor(m_PopDamagePerLevel, level, 30f);
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(ground, m_PopRadius, true, enemy));
            foreach (var other in m_Scratch)
                Hurt(other, damage, point, other.transform.position - point);

            if (balloon.Parade && enemy.Definition != null && ScoreManager.Instance != null &&
                enemy.HitPreparation == Preparation.Airborne)
            {
                var gained = ScoreManager.Instance.AddPoints(enemy.Definition.points);
                if (gained > 0)
                    FloatingText.Spawn(point + Vector3.up * 1.2f, $"Défilé ×2 : +{gained}", new Color(1f, 0.78f, 0.35f, 1f), 1.1f, 1.4f);
            }

            if (level >= 2 && !balloon.Chained)
                Blow(point, level);
        }

        // Le souffle (niveau 2) : ses voisins au sol gonflent vite à leur tour, sans tuyau ; eux ne soufflent plus.
        void Blow(Vector3 point, int level)
        {
            var ground = point;
            ground.y = GroundY(point, 1.5f);
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(ground, m_BlowRadius));
            var count = 0;
            foreach (var enemy in m_Scratch)
            {
                if (count >= m_BlowCount)
                    break;
                if (!CanInflate(enemy))
                    continue;
                Inflate(enemy, level, 0.6f, m_InflatedSize * 0.85f, m_FloatHeight * 0.7f, m_FloatTime * 0.7f, true, false);
                count++;
            }
        }

        // ---------------------------------------------------------------- le défilé (niveau 3)

        // Toute la portée gonfle d'un coup ; vrai s'il a eu lieu.
        bool Parade()
        {
            m_Scratch.Clear();
            foreach (var enemy in EnemiesNear(transform.position, m_Range))
            {
                if (CanInflate(enemy))
                    m_Scratch.Add(enemy);
            }

            if (m_Scratch.Count < 4)
                return false;

            m_NextParade = Time.time + m_ParadeInterval / OverchargeRate;
            var count = 0;
            foreach (var enemy in m_Scratch)
            {
                if (count++ >= m_ParadeMax)
                    break;
                var size = HeightOf(enemy) >= m_BigHeight ? m_ParadeBigSize : m_InflatedSize * 1.1f;
                Inflate(enemy, 3, 0.8f, size, m_ParadeHeight * Random.Range(0.85f, 1.15f), m_ParadeTime, false, true);
            }

            HeadMessages.Show("Le défilé !", new Color(1f, 0.55f, 0.75f), 2.5f);
            return true;
        }

        static float HeightOf(Enemy enemy)
        {
            var renderer = enemy.GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds.max.y - enemy.transform.position.y : 1.8f;
        }

        // ---------------------------------------------------------------- tuyaux, pompe, taille

        LineRenderer NewHose()
        {
            var go = new GameObject("Tuyau du Gonfleur");
            var line = go.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = 0.16f;
            line.endWidth = 0.1f;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (m_HoseMaterial != null)
                line.sharedMaterial = m_HoseMaterial;
            line.SetPosition(0, Nozzle);
            line.SetPosition(1, Nozzle);
            return line;
        }

        void SetHose(Balloon balloon, Vector3 end)
        {
            if (balloon.Hose == null)
                return;
            balloon.Hose.SetPosition(0, Nozzle);
            balloon.Hose.SetPosition(1, end);
        }

        static void DropHose(Balloon balloon)
        {
            if (balloon.Hose != null)
                Destroy(balloon.Hose.gameObject);
            balloon.Hose = null;
        }

        static void SetScale(Balloon balloon, float factor)
        {
            if (balloon.Enemy != null)
                balloon.Enemy.transform.localScale = balloon.Scale * factor;
        }

        // Fini : le tuyau part, il retrouve sa taille, et une autre fusion peut de nouveau le déformer.
        static void Forget(Balloon balloon, bool restore)
        {
            DropHose(balloon);
            s_Reshaped.Remove(balloon.Enemy);
            if (balloon.Enemy == null)
                return;
            if (balloon.OnDamaged != null && balloon.Enemy.Health != null)
                balloon.Enemy.Health.Damaged -= balloon.OnDamaged;
            if (restore || balloon.Phase == Phase.Deflate)
                balloon.Enemy.transform.localScale = balloon.Scale;
        }

        // La baliste se tourne vers sa cible ; la pompe bat tant qu'un ennemi gonfle.
        void AimAt(Vector3 point)
        {
            if (m_Ballista == null)
                return;
            var flat = point - m_Ballista.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 1e-4f)
                m_Ballista.rotation = Quaternion.Slerp(m_Ballista.rotation, Quaternion.LookRotation(flat), Time.deltaTime * 12f);
        }

        void AnimatePump()
        {
            if (m_Piston == null)
                return;

            var pumping = false;
            foreach (var balloon in m_Balloons)
            {
                if (balloon.Phase == Phase.Inflate)
                {
                    pumping = true;
                    break;
                }
            }

            if (pumping)
                m_PistonPhase += Time.deltaTime * 9f;
            else
                m_PistonPhase = Mathf.MoveTowards(m_PistonPhase, Mathf.Round(m_PistonPhase / (2f * Mathf.PI)) * 2f * Mathf.PI, Time.deltaTime * 4f);
            m_Piston.localPosition = m_PistonRest + Vector3.down * (0.35f * (1f - Mathf.Cos(m_PistonPhase)));
        }
    }
}
