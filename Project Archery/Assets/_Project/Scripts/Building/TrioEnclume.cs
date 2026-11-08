using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le trio <b>Enclume</b> (Marteau-pilon + Mortier, 1er novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.19) : une forge sur
    /// pilotis au-dessus de la voie ; son mortier en cloche de bronze tire des enclumes en cloche. L'ennemi touché est <b>aplati
    /// comme une crêpe</b> (« Aplati ! ») : il avance tout plat, deux fois moins vite, préparé pour les flèches du joueur.
    /// <list type="bullet">
    /// <item>1 : une enclume toutes les 4 s sur l'ennemi visé ;</item>
    /// <item>2 : l'enclume rebondit et aplatit un deuxième ennemi ; l'onde du choc fait tomber autour ;</item>
    /// <item>3, <b>la pluie d'enclumes</b> : en plus, toutes les 16 s, vingt enclumes tombent du ciel sur les ennemis à portée, puis
    /// une enclume de 10 m au milieu du groupe le plus serré : les petits tués net, tous les autres aplatis ; elle reste plantée 6 s.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Aplatir, c'est écraser l'ennemi entier (sa racine, ses zones de touche avec) : sa hauteur à <see cref="m_FlatHeight"/>, sa
    /// largeur un peu plus grande ; il reprend sa forme d'un coup, comme un ressort, à la fin. Les boss ne sont jamais aplatis (une
    /// part de leurs PV sous la grande enclume) ; les volants ne sont pas visés. Le modèle : x en travers de la voie, z le long.
    /// </remarks>
    public class TrioEnclume : TowerTrio
    {
        [Header("Le mortier")]
        [Tooltip("La bouche du mortier : les enclumes en partent.")]
        [SerializeField]
        Transform m_Muzzle;

        [Tooltip("Le fût du mortier : il recule à chaque tir.")]
        [SerializeField]
        Transform m_Barrel;

        [Tooltip("Une enclume du modèle (celle du haut du tas, sur la plate-forme ; son repère en bas, au milieu) : chaque tir en " +
                 "fait une copie, la grande enclume aussi, agrandie.")]
        [SerializeField]
        Transform m_AnvilTemplate;

        [Tooltip("Portée (m, à plat, autour de la forge).")]
        [SerializeField]
        float m_Range = 16f;

        [Header("Les enclumes")]
        [Tooltip("Temps (s) entre deux tirs, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 4f, 3.2f, 2.4f };

        [Tooltip("Dégâts de l'enclume, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 40f, 55f, 70f };

        [Tooltip("Temps (s) qu'un ennemi reste aplati, par niveau.")]
        [SerializeField]
        float[] m_FlatTimePerLevel = { 6f, 6f, 7f };

        [Tooltip("Vitesse d'un ennemi aplati (0,5 : deux fois moins vite).")]
        [SerializeField]
        float m_FlatSpeed = 0.5f;

        [Tooltip("Hauteur d'un ennemi aplati (part de la sienne), et sa largeur (il s'étale).")]
        [SerializeField]
        float m_FlatHeight = 0.18f;

        [SerializeField]
        float m_FlatWidth = 1.45f;

        [Tooltip("Rayon (m) écrasé sous une enclume.")]
        [SerializeField]
        float m_SplashRadius = 1.4f;

        [Tooltip("Durée (s) du vol d'une enclume tirée par le mortier.")]
        [SerializeField]
        float m_FlightTime = 1.1f;

        [Header("Le rebond et l'onde (niveau 2)")]
        [Tooltip("Jusqu'où (m) l'enclume rebondit vers un deuxième ennemi.")]
        [SerializeField]
        float m_BounceRange = 6f;

        [SerializeField]
        float m_WaveRadius = 3.5f;

        [SerializeField]
        float m_WaveDamage = 15f;

        [Header("La pluie d'enclumes (niveau 3)")]
        [Tooltip("Temps (s) entre deux pluies ; il faut au moins trois ennemis à portée.")]
        [SerializeField]
        float m_RainInterval = 16f;

        [SerializeField]
        int m_RainCount = 20;

        [Tooltip("Durée (s) de la pluie, avant la grande enclume.")]
        [SerializeField]
        float m_RainDuration = 2.5f;

        [SerializeField]
        float m_RainDamage = 30f;

        [Tooltip("Taille de la grande enclume (fois une enclume) : 8 fait environ 10 m.")]
        [SerializeField]
        float m_GiantScale = 8f;

        [Tooltip("Rayon (m) écrasé sous la grande enclume.")]
        [SerializeField]
        float m_GiantRadius = 6f;

        [Tooltip("Dégâts de la grande enclume (les petits ennemis en meurent net).")]
        [SerializeField]
        float m_GiantDamage = 350f;

        [Tooltip("Un boss sous la grande enclume perd cette part de ses PV max (il n'est jamais aplati).")]
        [SerializeField]
        float m_GiantBossShare = 0.08f;

        [Tooltip("Temps (s) que la grande enclume reste plantée, avant de s'enfoncer dans le sol.")]
        [SerializeField]
        float m_GiantStay = 6f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_FireClip;

        [SerializeField]
        AudioClip m_ClangClip;

        [SerializeField]
        AudioClip m_FlatClip;

        class Anvil
        {
            public Transform Body;
            public Vector3 From;
            public Vector3 To;
            public float Height;
            public float Duration;
            public float Time;
            public Enemy Target;
            public int Bounces;
            public bool Giant;
            public Vector3 Spin;

            // Dans l'arène (3 novembre) : elle suit le corps de sa cible (à cet écart) ; après l'avoir frappée en l'air, elle retombe au
            // sol (sans blesser), puis s'y couche ce temps-là.
            public bool Follow;
            public Vector3 Offset;
            public bool Falling;
            public float LieStay;
            public float LieSink;
        }

        class Lying
        {
            public Transform Body;
            public float Until;
            public float Sink;
            public Vector3 Scale;
        }

        class Flat
        {
            public Vector3 Scale;
            public float Since;
            public float Until;
        }

        static readonly Color k_Dust = new Color(0.78f, 0.7f, 0.56f, 1f);
        static readonly Color k_Sparks = new Color(1f, 0.72f, 0.3f, 1f);

        readonly List<Anvil> m_Flying = new List<Anvil>();
        readonly List<Lying> m_Lying = new List<Lying>();
        readonly Dictionary<Enemy, Flat> m_Flat = new Dictionary<Enemy, Flat>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        readonly List<Enemy> m_Hit = new List<Enemy>();
        Vector3 m_BarrelRest;
        float m_Recoil;
        float m_NextShot;
        float m_NextRain;
        float m_NextRainDrop;
        int m_RainLeft;

        protected override void Awake()
        {
            base.Awake();
            if (m_Barrel != null)
                m_BarrelRest = m_Barrel.localPosition;
        }

        protected override void OnDestroy()
        {
            foreach (var anvil in m_Flying)
            {
                if (anvil.Body != null)
                    Destroy(anvil.Body.gameObject);
            }

            foreach (var lying in m_Lying)
            {
                if (lying.Body != null)
                    Destroy(lying.Body.gameObject);
            }

            foreach (var pair in m_Flat)
                Restore(pair.Key, pair.Value);
            m_Flying.Clear();
            m_Lying.Clear();
            m_Flat.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateFlat();
            UpdateFlying();
            UpdateLying();
            AnimateBarrel();
            if (!IsBuilt || IsUnderConstruction)
                return;

            var level = Mathf.Max(1, Level);
            if (level >= 3)
                UpdateRain();

            if (Time.time < m_NextShot || m_RainLeft > 0)
                return;

            var target = FindTarget(null, transform.position, Range);
            if (target == null)
                return;

            Fire(target, level);
            m_NextShot = Time.time + ValueFor(m_IntervalPerLevel, level, 4f) / OverchargeRate;
        }

        // ---------------------------------------------------------------- le tir

        // L'ennemi au sol, ni aplati ni en l'air, le plus avancé vers la tour du joueur parmi ceux à portée de ce point.
        Enemy FindTarget(Enemy except, Vector3 around, float radius)
        {
            Enemy best = null;
            var bestScore = float.MaxValue;
            var tower = Archery.Defense.Tower.Instance;
            foreach (var enemy in EnemiesNear(around, radius, true, except))
            {
                if (m_Flat.ContainsKey(enemy) || IsAimedAt(enemy))
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

        bool IsAimedAt(Enemy enemy)
        {
            foreach (var anvil in m_Flying)
            {
                if (anvil.Target == enemy)
                    return true;
            }

            return false;
        }

        // Où sera l'ennemi à l'arrivée de l'enclume : il avance vers la tour, le long de sa voie.
        static Vector3 Lead(Enemy enemy, float time)
        {
            var position = enemy.transform.position;
            if (enemy.IsStunned || enemy.IsFrozen || enemy.IsHeld)
                return position;
            return position - ForestwardAt(position) * enemy.CurrentSpeed * time;
        }

        void Fire(Enemy target, int level)
        {
            var from = m_Muzzle != null ? m_Muzzle.position : transform.position + Vector3.up * 6f;
            // Dans l'arène, l'enclume rattrape MK, en vol comme au sol (3 novembre : elle tombait au sol sous lui).
            var to = InArena ? target.Center : Lead(target, m_FlightTime);
            if (!InArena)
                to.y = GroundY(to, 1.5f);
            var flat = to - from;
            flat.y = 0f;
            var anvil = Launch(from, to, m_FlightTime, 5f + flat.magnitude * 0.3f, target, level >= 2 ? 1 : 0, false, 1f);
            if (anvil != null)
                anvil.Follow = InArena;
            m_Recoil = 1f;
            Burst(from, new Color(0.5f, 0.48f, 0.45f, 1f), 18, 4f, 0.6f, 0.8f, -0.1f);
            Sfx.Play(m_FireClip, from, 0.9f, Random.Range(0.85f, 1f));
        }

        Anvil Launch(Vector3 from, Vector3 to, float duration, float height, Enemy target, int bounces, bool giant, float scale)
        {
            if (m_AnvilTemplate == null)
                return null;

            var body = Instantiate(m_AnvilTemplate.gameObject).transform;
            body.name = giant ? "Grande enclume" : "Enclume";
            body.localScale = m_AnvilTemplate.lossyScale * scale;
            body.position = from;
            body.gameObject.SetActive(true);
            var anvil = new Anvil
            {
                Body = body,
                From = from,
                To = to,
                Height = height,
                Duration = Mathf.Max(0.1f, duration),
                Target = target,
                Bounces = bounces,
                Giant = giant,
                Spin = giant ? Vector3.zero : new Vector3(Random.Range(-400f, 400f), Random.Range(-200f, 200f), Random.Range(-400f, 400f)),
            };
            m_Flying.Add(anvil);
            return anvil;
        }

        void UpdateFlying()
        {
            for (var i = m_Flying.Count - 1; i >= 0; i--)
            {
                var anvil = m_Flying[i];
                if (anvil.Body == null)
                {
                    m_Flying.RemoveAt(i);
                    continue;
                }

                anvil.Time += Time.deltaTime;
                var t = Mathf.Clamp01(anvil.Time / anvil.Duration);
                if (anvil.Follow && anvil.Target != null && anvil.Target.IsAlive)
                    anvil.To = anvil.Target.Center + anvil.Offset;
                var position = Arc(anvil.From, anvil.To, t, anvil.Height);
                if (anvil.Giant || anvil.Falling)
                    position = Vector3.Lerp(anvil.From, anvil.To, t * t);
                anvil.Body.position = position;

                // Elle tourne en l'air, et se pose à plat (la table en haut) en touchant le sol.
                if (anvil.Giant || anvil.Falling || t > 0.8f)
                    anvil.Body.rotation = Quaternion.Slerp(anvil.Body.rotation, Quaternion.Euler(0f, anvil.Body.eulerAngles.y, 0f), Time.deltaTime * 12f);
                else
                    anvil.Body.Rotate(anvil.Spin * Time.deltaTime, Space.Self);

                if (t < 1f)
                    continue;

                m_Flying.RemoveAt(i);
                Land(anvil);
            }
        }

        // L'enclume touche le sol : elle aplatit ce qui est dessous ; au niveau 2, elle rebondit vers un deuxième ennemi.
        void Land(Anvil anvil)
        {
            var point = anvil.To;
            anvil.Body.position = point;
            if (anvil.Falling)
            {
                LieDown(anvil.Body, anvil.LieStay, anvil.LieSink);
                return;
            }

            if (anvil.Giant)
            {
                GiantLand(anvil);
                return;
            }

            var level = Mathf.Max(1, Level);
            var damage = anvil.Bounces < 0 ? m_RainDamage : ValueFor(m_DamagePerLevel, level, 40f);
            m_Hit.Clear();
            foreach (var enemy in EnemiesNear(point, m_SplashRadius))
                m_Hit.Add(enemy);
            if (anvil.Target != null && anvil.Target.IsAlive && !anvil.Target.IsAirborne && !m_Hit.Contains(anvil.Target))
            {
                var flat = anvil.Target.transform.position - point;
                flat.y = 0f;
                if (flat.magnitude < m_SplashRadius + 0.8f)
                    m_Hit.Add(anvil.Target);
            }

            foreach (var enemy in m_Hit)
            {
                if (Hurt(enemy, damage, point, Vector3.down))
                    Flatten(enemy, ValueFor(m_FlatTimePerLevel, level, 6f));
            }

            Burst(point + Vector3.up * 0.15f, k_Dust, 30, 6f, 0.5f, 0.8f, 1.2f, false);
            Burst(point + Vector3.up * 0.3f, k_Sparks, 14, 7f, 0.12f, 0.4f, 1.5f, false);
            Sfx.Play(m_ClangClip, point, 1f, Random.Range(0.75f, 0.95f));

            // Au niveau 2, l'onde fait tomber ceux qui restent autour (pas sous la pluie), puis l'enclume rebondit une fois vers
            // l'ennemi le plus avancé à côté.
            if (anvil.Bounces >= 0 && level >= 2)
                Wave(point);
            if (anvil.Bounces > 0)
            {
                var next = FindTarget(anvil.Target, point, m_BounceRange);
                if (next != null)
                {
                    var to = Lead(next, 0.55f);
                    to.y = GroundY(to, 1.5f);
                    anvil.From = point + Vector3.up * 0.3f;
                    anvil.To = to;
                    anvil.Height = 2.5f;
                    anvil.Duration = 0.55f;
                    anvil.Time = 0f;
                    anvil.Target = next;
                    anvil.Bounces--;
                    anvil.Follow = false;
                    m_Flying.Add(anvil);
                    return;
                }
            }

            if (!FallIfInAir(anvil, 1.2f, 0.4f))
                LieDown(anvil.Body, 1.2f, 0.4f);
        }

        // Dans l'arène, une enclume qui a frappé MK en l'air retombe au sol, puis s'y couche (3 novembre) ; vrai si elle tombe.
        bool FallIfInAir(Anvil anvil, float stay, float sink)
        {
            if (!InArena || anvil.Body == null)
                return false;

            var point = anvil.Body.position;
            var ground = new Vector3(point.x, GroundY(point, 1f), point.z);
            var drop = point.y - ground.y;
            if (drop < 0.5f)
                return false;

            anvil.From = point;
            anvil.To = ground;
            anvil.Time = 0f;
            anvil.Duration = Mathf.Clamp(Mathf.Sqrt(2f * drop / 9.81f), 0.25f, 1.5f);
            anvil.Height = 0f;
            anvil.Target = null;
            anvil.Follow = false;
            anvil.Falling = true;
            anvil.LieStay = stay;
            anvil.LieSink = sink;
            m_Flying.Add(anvil);
            return true;
        }

        void Wave(Vector3 point)
        {
            foreach (var enemy in EnemiesNear(point, m_WaveRadius))
            {
                if (m_Hit.Contains(enemy) || m_Flat.ContainsKey(enemy))
                    continue;
                if (Hurt(enemy, m_WaveDamage, point, enemy.transform.position - point))
                    KnockDown(enemy, point, 2f, 3.5f);
            }

            Burst(point + Vector3.up * 0.1f, k_Dust, 24, 9f, 0.6f, 0.7f, 1.2f, false);
        }

        // Une enclume posée reste un moment au sol, puis s'y enfonce et disparaît.
        void LieDown(Transform body, float stay, float sink)
        {
            if (body == null)
                return;
            m_Lying.Add(new Lying { Body = body, Until = Time.time + stay, Sink = sink, Scale = body.localScale });
        }

        void UpdateLying()
        {
            for (var i = m_Lying.Count - 1; i >= 0; i--)
            {
                var lying = m_Lying[i];
                if (lying.Body == null)
                {
                    m_Lying.RemoveAt(i);
                    continue;
                }

                var left = lying.Until - Time.time;
                if (left > 0f)
                    continue;

                // Elle s'enfonce dans le sol (sa hauteur : le modèle d'enclume fait environ 1,1 m à l'échelle 1) en Sink secondes.
                var t = -left / Mathf.Max(0.05f, lying.Sink);
                lying.Body.position += Vector3.down * lying.Scale.y * 1.1f * Time.deltaTime / Mathf.Max(0.05f, lying.Sink);
                if (t < 1f)
                    continue;

                Destroy(lying.Body.gameObject);
                m_Lying.RemoveAt(i);
            }
        }

        void AnimateBarrel()
        {
            if (m_Barrel == null)
                return;
            m_Recoil = Mathf.MoveTowards(m_Recoil, 0f, Time.deltaTime * 3f);
            m_Barrel.localPosition = m_BarrelRest + Vector3.down * 0.35f * Mathf.Sin(m_Recoil * Mathf.PI * 0.5f);
        }

        // ---------------------------------------------------------------- la pluie d'enclumes (niveau 3)

        void UpdateRain()
        {
            if (m_RainLeft > 0)
            {
                if (Time.time < m_NextRainDrop)
                    return;

                m_NextRainDrop = Time.time + m_RainDuration / Mathf.Max(1, m_RainCount);
                m_RainLeft--;
                RainDrop();
                if (m_RainLeft == 0)
                    DropGiant();
                return;
            }

            // Il faut trois ennemis à portée ; dans l'arène, MK seul suffit (3 novembre).
            if (Time.time < m_NextRain || EnemiesNear(transform.position, Range).Count < (InArena ? 1 : 3))
                return;

            m_NextRain = Time.time + m_RainInterval / OverchargeRate;
            m_RainLeft = Mathf.Max(1, m_RainCount);
            m_NextRainDrop = Time.time;
            HeadMessages.Show("Pluie d'enclumes !", new Color(1f, 0.78f, 0.35f), 2.5f);
            Sfx.Play(m_FireClip, transform.position + Vector3.up * 6f, 1f, 0.6f);
        }

        // Une enclume du ciel : sur un ennemi à portée (un peu à côté), sinon sur la voie près de la forge.
        void RainDrop()
        {
            var near = EnemiesNear(transform.position, Range);
            Vector3 to;
            Enemy target = null;

            // Dans l'arène : sur MK, de moins haut (la voûte), et elle le suit (3 novembre).
            if (InArena && near.Count > 0)
            {
                target = near[Random.Range(0, near.Count)];
                var offset = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-0.5f, 1f), Random.Range(-1.5f, 1.5f));
                to = target.Center + offset;
                var rain = Launch(to + new Vector3(Random.Range(-3f, 3f), 10f + Random.Range(0f, 3f), Random.Range(-3f, 3f)), to,
                                  0.7f + Random.Range(0f, 0.2f), 0f, target, -1, false, 1f);
                if (rain != null)
                {
                    rain.Follow = true;
                    rain.Offset = offset;
                }

                return;
            }

            if (near.Count > 0)
            {
                target = near[Random.Range(0, near.Count)];
                to = Lead(target, 0.8f) + new Vector3(Random.Range(-1.2f, 1.2f), 0f, Random.Range(-1.2f, 1.2f));
            }
            else
            {
                to = transform.position + new Vector3(Random.Range(-m_Range, m_Range), 0f, Random.Range(-m_Range, m_Range)) * 0.5f;
            }

            to.y = GroundY(to, 1.5f);
            var from = to + new Vector3(Random.Range(-4f, 4f), 22f + Random.Range(0f, 6f), Random.Range(-4f, 4f));
            Launch(from, to, 0.8f + Random.Range(0f, 0.25f), 0f, target, -1, false, 1f);
        }

        // La grande enclume : au-dessus du groupe le plus serré, elle tombe tout droit.
        void DropGiant()
        {
            // Dans l'arène : sur MK, de moins haut (la voûte), et elle le suit (3 novembre).
            var boss = InArena ? NearestEnemy(Range) : null;
            var center = boss != null ? boss.Center : DensestPoint();
            if (boss == null)
                center.y = GroundY(center, 1.5f);
            var anvil = Launch(center + Vector3.up * (boss != null ? 16f : 45f), center, 1.1f, 0f, boss, 0, true, m_GiantScale);
            if (anvil == null)
                return;
            anvil.Body.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + 90f, 0f);
            anvil.Follow = boss != null;
        }

        Vector3 DensestPoint()
        {
            var near = EnemiesNear(transform.position, Range);
            m_Scratch.Clear();
            m_Scratch.AddRange(near);
            var best = transform.position;
            var bestCount = -1;
            foreach (var enemy in m_Scratch)
            {
                var count = 0;
                foreach (var other in m_Scratch)
                {
                    var flat = other.transform.position - enemy.transform.position;
                    flat.y = 0f;
                    if (flat.sqrMagnitude < 25f)
                        count++;
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    best = enemy.transform.position;
                }
            }

            return best;
        }

        void GiantLand(Anvil anvil)
        {
            var point = anvil.To;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(point, m_GiantRadius));
            foreach (var enemy in m_Scratch)
            {
                if (enemy.IsBoss)
                {
                    // MK, dans l'arène : 2 % au plus (ShareFor, 3 novembre).
                    enemy.Health.TakeDamage(new DamageInfo
                    {
                        Amount = enemy.Health.Max * ShareFor(enemy, m_GiantBossShare),
                        Zone = HitZone.Body,
                        Point = enemy.Center,
                        Direction = Vector3.down,
                        Source = this,
                    });
                    continue;
                }

                if (Hurt(enemy, m_GiantDamage, point, Vector3.down))
                    Flatten(enemy, ValueFor(m_FlatTimePerLevel, 3, 7f));
            }

            for (var i = 0; i < 10; i++)
            {
                var angle = i * Mathf.PI / 5f;
                var at = point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * m_GiantRadius * 0.7f;
                Burst(at + Vector3.up * 0.2f, k_Dust, 34, 11f, 0.9f, 1.3f, 1.2f, false);
            }

            Burst(point + Vector3.up * 1f, k_Sparks, 90, 16f, 0.3f, 1f, 0.8f);
            Sfx.Play(m_ClangClip, point, 1f, 0.35f);
            HeadMessages.Show("BONG !", new Color(1f, 0.78f, 0.35f), 1.5f);
            if (!FallIfInAir(anvil, m_GiantStay * 0.5f, 1.2f))
                LieDown(anvil.Body, m_GiantStay, 1.2f);
        }

        // ---------------------------------------------------------------- aplatis

        void Flatten(Enemy enemy, float duration)
        {
            if (enemy == null || !enemy.IsAlive || enemy.IsBoss || enemy.IsFlyer)
                return;

            if (!m_Flat.TryGetValue(enemy, out var flat))
            {
                // Déjà déformé par une autre fusion (gonflé, aplati par une autre enclume) : seulement les dégâts.
                if (!s_Reshaped.Add(enemy))
                    return;
                flat = new Flat { Scale = enemy.transform.localScale, Since = Time.time };
                m_Flat[enemy] = flat;
                FloatingText.Spawn(enemy.transform.position + Vector3.up * 1.2f, "Aplati !", new Color(1f, 0.8f, 0.35f, 1f), 1.4f, 1.4f);
                Sfx.Play(m_FlatClip, enemy.transform.position, 0.9f, Random.Range(0.9f, 1.15f));
            }

            flat.Until = Mathf.Max(flat.Until, Time.time + duration);
            enemy.Slow(m_FlatSpeed, flat.Until - Time.time);
            enemy.Prepare(Preparation.Flattened, flat.Until - Time.time);
        }

        // Écrasé en 0,1 s ; à la fin, il reprend sa forme d'un coup, en rebondissant (0,3 s).
        void UpdateFlat()
        {
            if (m_Flat.Count == 0)
                return;

            m_Scratch.Clear();
            foreach (var pair in m_Flat)
            {
                var enemy = pair.Key;
                var flat = pair.Value;
                if (enemy == null)
                {
                    s_Reshaped.Remove(enemy);
                    m_Scratch.Add(enemy);
                    continue;
                }

                var squash = Mathf.Clamp01((Time.time - flat.Since) / 0.1f);
                var unsquash = Time.time - flat.Until;
                float height, width;
                if (unsquash < 0f)
                {
                    height = Mathf.Lerp(1f, m_FlatHeight, squash);
                    width = Mathf.Lerp(1f, m_FlatWidth, squash);
                }
                else
                {
                    var t = Mathf.Clamp01(unsquash / 0.3f);
                    var spring = 1f + Mathf.Sin(t * Mathf.PI * 2.5f) * (1f - t) * 0.25f;
                    height = Mathf.Lerp(m_FlatHeight, 1f, Mathf.Min(1f, t * 3f)) * spring;
                    width = Mathf.Lerp(m_FlatWidth, 1f, Mathf.Min(1f, t * 3f));
                    if (t >= 1f)
                    {
                        Restore(enemy, flat);
                        m_Scratch.Add(enemy);
                        continue;
                    }
                }

                enemy.transform.localScale = new Vector3(flat.Scale.x * width, flat.Scale.y * height, flat.Scale.z * width);
            }

            foreach (var enemy in m_Scratch)
                m_Flat.Remove(enemy);
        }

        static void Restore(Enemy enemy, Flat flat)
        {
            s_Reshaped.Remove(enemy);
            if (enemy == null)
                return;
            enemy.transform.localScale = flat.Scale;
            enemy.Unprepare(Preparation.Flattened);
        }

        /// <summary>La portée dessinée pendant la pose (3 novembre, le placement lisible).</summary>
        public override float ReachAt(int level) => m_Range;

        // Sa portée : plus grande dans l'arène de MK, où elle tire du haut du plateau (RangeScale, 3 novembre).
        float Range => m_Range * RangeScale;

        /// <summary>
        /// Elle rejoint l'arène de MK (3 novembre) : ses enclumes y rattrapent MK, en vol comme au sol, puis retombent ; la pluie part
        /// sur lui seul, et la grande enclume ne lui coûte que 2 % de ses PV max. Il n'est jamais aplati.
        /// </summary>
        public override bool FightsInArena => true;
    }
}
