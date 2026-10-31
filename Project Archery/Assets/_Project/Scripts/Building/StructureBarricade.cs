using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Defense;
using Archery.Enemies;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Barricade construite sur la voie (GDD, section 9) : une <see cref="Barricade"/> comme celles de départ, que les ennemis au
    /// sol frappent avant de passer. Depuis le 29 octobre (deuxième brainstorm, <c>Docs/Refonte_TowerDefense.md</c>, partie 12.6),
    /// cinq niveaux :
    /// <list type="bullet">
    /// <item>2, le <b>rempart</b> : PV ×2, et des pointes blessent ceux qui la frappent (au niveau 3 avant) ;</item>
    /// <item>3, le <b>pendule</b> (l'ancien pendule, que le joueur aimait) : un portique au-dessus d'elle, d'où un boulet hérissé
    /// se balance en travers de la voie, devant elle, et fauche ceux qui l'assiègent ;</item>
    /// <item>4, la <b>herse</b> : toutes les 8 s, elle s'abat devant la barricade et étourdit ceux qui frappent ; ta flèche qui
    /// traverse la barricade devient perforante ;</item>
    /// <item>5, la <b>forteresse</b> : un mur géant, deux tours de guet qui criblent la voie de carreaux, et chaque coup reçu
    /// lâche une onde qui renvoie les assaillants 10 m en arrière, en vol.</item>
    /// </list>
    /// La réparation de la boutique la remet debout avec les autres.
    /// </summary>
    /// <remarks>
    /// La <see cref="Barricade"/> reste désactivée tant que le plan n'est pas construit : un plan n'arrête personne.
    /// L'avant du bâtiment (axe Z) pointe vers la forêt, comme le socle ; le mur va le long de son axe X.
    /// </remarks>
    [RequireComponent(typeof(Barricade), typeof(Health))]
    public class StructureBarricade : Structure
    {
        [Tooltip("PV de la barricade à chaque niveau.")]
        [SerializeField]
        float[] m_HealthPerLevel = { 400f, 800f, 1000f, 1300f, 2500f };

        [Tooltip("Niveau à partir duquel des pointes blessent les ennemis qui frappent la barricade.")]
        [SerializeField]
        int m_ThornsFromLevel = 2;

        [Tooltip("Dégâts des pointes à chaque coup reçu au corps à corps, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float m_ThornsDamage = 10f;

        [SerializeField]
        AudioClip m_ThornsClip;

        [Header("Pendule (niveau 3)")]
        [Tooltip("Balancier du pendule : tourne autour de son axe Z (le portique).")]
        [SerializeField]
        Transform m_PendulumArm;

        [Tooltip("Centre du boulet.")]
        [SerializeField]
        Transform m_PendulumBall;

        [Tooltip("Dégâts d'un coup de boulet, par niveau, à la vague 1 en Normal.")]
        [SerializeField]
        float[] m_PendulumDamagePerLevel = { 0f, 0f, 30f, 36f, 45f };

        [Tooltip("Durée (s) d'un aller-retour du boulet.")]
        [SerializeField]
        float m_PendulumPeriod = 2.4f;

        [Tooltip("Angle (°) du balancement de chaque côté.")]
        [SerializeField]
        float m_PendulumAmplitude = 65f;

        [Tooltip("Rayon (m) du boulet, pour toucher.")]
        [SerializeField]
        float m_PendulumRadius = 0.9f;

        [Tooltip("Vitesse (m/s) sur le côté des ennemis fauchés.")]
        [SerializeField]
        float m_PendulumKnock = 6f;

        [SerializeField]
        AudioClip m_PendulumClip;

        [Header("Herse (niveau 4)")]
        [Tooltip("La herse : elle tombe de sa hauteur au repos jusqu'au sol (axe Y).")]
        [SerializeField]
        Transform m_Portcullis;

        [Tooltip("Hauteur (m) dont la herse descend en s'abattant.")]
        [SerializeField]
        float m_PortcullisDrop = 1.6f;

        [Tooltip("Temps (s) entre deux coups de herse.")]
        [SerializeField]
        float m_PortcullisInterval = 8f;

        [Tooltip("Étourdissement (s) de ceux qu'elle frappe.")]
        [SerializeField]
        float m_PortcullisStun = 1.5f;

        [Tooltip("Dégâts de la herse, à la vague 1 en Normal.")]
        [SerializeField]
        float m_PortcullisDamage = 20f;

        [Tooltip("Distance (m) devant la barricade où elle frappe.")]
        [SerializeField]
        float m_PortcullisReach = 2.5f;

        [Tooltip("Ennemis en plus que traverse ta flèche quand elle passe à travers la barricade.")]
        [SerializeField]
        int m_ArrowPierce = 2;

        [Tooltip("Volume (m) que la flèche doit traverser, autour du mur (demi-largeur, demi-hauteur, demi-épaisseur).")]
        [SerializeField]
        Vector3 m_ArrowGate = new Vector3(2.3f, 1.4f, 0.8f);

        [SerializeField]
        Color m_PierceTrail = new Color(1f, 0.85f, 0.4f, 0.8f);

        [SerializeField]
        AudioClip m_PortcullisClip;

        [Header("Forteresse (niveau 5)")]
        [Tooltip("Les tourelles en haut des tours de guet (une petite baliste chacune) : elles se tournent vers leur cible.")]
        [SerializeField]
        Transform[] m_WatchTowers = new Transform[0];

        [Tooltip("Bouche des petites balistes (m), dans le repère de leur tourelle : les carreaux en partent.")]
        [SerializeField]
        Vector3 m_TowerMuzzle = new Vector3(0f, 0.37f, 0.4f);

        [SerializeField]
        BallistaBolt m_BoltPrefab;

        [Tooltip("Temps (s) entre deux carreaux d'une tour de guet.")]
        [SerializeField]
        float m_TowerShotInterval = 0.45f;

        [Tooltip("Portée (m) des tours de guet.")]
        [SerializeField]
        float m_TowerRange = 26f;

        [Tooltip("Dégâts d'un carreau des tours de guet, à la vague 1 en Normal.")]
        [SerializeField]
        float m_TowerDamage = 8f;

        [Tooltip("Distance (m) dont l'onde renvoie les assaillants, en vol, vers la forêt.")]
        [SerializeField]
        float m_WaveDistance = 10f;

        [Tooltip("Portée (m) de l'onde devant la barricade.")]
        [SerializeField]
        float m_WaveReach = 5f;

        [Tooltip("Temps (s) minimal entre deux ondes.")]
        [SerializeField]
        float m_WaveCooldown = 1.5f;

        [SerializeField]
        GameObject m_WaveEffect;

        [SerializeField]
        AudioClip m_WaveClip;

        Barricade m_Barricade;
        Health m_Health;
        readonly ArrowWatch m_Arrows = new ArrowWatch();
        readonly Dictionary<Enemy, float> m_LastSwing = new Dictionary<Enemy, float>();
        readonly List<Enemy> m_Forget = new List<Enemy>();
        Quaternion m_ArmRest = Quaternion.identity;
        Vector3 m_PortcullisRest;
        Vector3 m_PreviousBall;
        float m_Phase;
        float m_PortcullisTimer = 3f;
        float m_PortcullisFall;
        float[] m_TowerTimers = new float[0];
        Quaternion[] m_TowerAims = new Quaternion[0];
        float m_WaveTimer;

        protected override void Awake()
        {
            if (m_PendulumArm != null)
                m_ArmRest = m_PendulumArm.localRotation;
            if (m_Portcullis != null)
                m_PortcullisRest = m_Portcullis.localPosition;
            base.Awake();
            m_Barricade = GetComponent<Barricade>();
            m_Health = GetComponent<Health>();

            // Posée à la main dans la scène, elle est debout dès le départ (Structure.Start).
            m_Barricade.enabled = false;
        }

        void OnEnable()
        {
            m_Health.Damaged += OnDamaged;
            m_Arrows.Begin();
        }

        void OnDisable()
        {
            m_Health.Damaged -= OnDamaged;
            m_Arrows.End();
        }

        protected override void OnLevelChanged(int level)
        {
            base.OnLevelChanged(level);
            m_Barricade.enabled = true;
            m_Health.ResetHealth(ValueFor(m_HealthPerLevel, level, 400f));
            m_Barricade.Rebuild(false);
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            var level = Level;
            var deltaTime = Time.deltaTime;
            m_WaveTimer -= deltaTime;
            if (level >= 3)
                Swing(level, deltaTime);
            if (level >= 4)
            {
                UpdatePortcullis(deltaTime);
                WatchArrows();
            }

            if (level >= 5)
                ShootFromTowers(deltaTime);
        }

        // Les pointes : un ennemi qui frappe la barricade (corps à corps) se blesse ; au niveau 5, l'onde le renvoie. Les
        // projectiles ne comptent pas.
        void OnDamaged(Health health, DamageInfo info)
        {
            if (Level < m_ThornsFromLevel || !(info.Source is Enemy enemy) || !enemy.IsAlive)
                return;

            var direction = enemy.Center - info.Point;
            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = m_ThornsDamage * Strength,
                Zone = HitZone.Body,
                Point = enemy.Center,
                Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward,
                Source = this,
            });
            Sfx.Play(m_ThornsClip, info.Point, 0.7f, Random.Range(0.9f, 1.1f));

            if (Level >= 5 && m_WaveTimer <= 0f)
                Wave();
        }

        // ---------------------------------------------------------------- niveau 3 : le pendule

        // Comme le pendule (StructurePendulum) : le boulet fauche les ennemis au sol qu'il croise, et les projette sur le côté.
        void Swing(int level, float deltaTime)
        {
            if (m_PendulumArm == null || m_PendulumBall == null)
                return;

            m_Phase = (m_Phase + deltaTime * OverchargeRate / Mathf.Max(0.5f, m_PendulumPeriod)) % 1f;
            m_PendulumArm.localRotation = m_ArmRest * Quaternion.Euler(0f, 0f, m_PendulumAmplitude * Mathf.Sin(m_Phase * 2f * Mathf.PI));

            var ball = m_PendulumBall.position;
            var motion = deltaTime > 0f ? (ball - m_PreviousBall) / deltaTime : Vector3.zero;
            m_PreviousBall = ball;
            motion.y = 0f;
            if (motion.sqrMagnitude < 1f || motion.sqrMagnitude > 900f)
                return;

            var damage = ValueFor(m_PendulumDamagePerLevel, level, 30f) * Strength;
            var side = motion.normalized;
            var reach = (m_PendulumRadius + 0.4f) * (m_PendulumRadius + 0.4f);
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsFlyer || enemy.IsAirborne || (enemy.Center - ball).sqrMagnitude > reach)
                    continue;
                if (m_LastSwing.TryGetValue(enemy, out var last) && Time.time - last < 0.8f)
                    continue;

                m_LastSwing[enemy] = Time.time;
                Hit(enemy, damage, side);
                if (enemy.IsAlive)
                    enemy.Launch(side * m_PendulumKnock + Vector3.up * 4f, this);
                Sfx.Play(m_PendulumClip, enemy.Center, 0.9f, Random.Range(0.9f, 1.1f));
            }

            if (m_LastSwing.Count < 16)
                return;

            m_Forget.Clear();
            foreach (var pair in m_LastSwing)
            {
                if (pair.Key == null || Time.time - pair.Value > 0.8f)
                    m_Forget.Add(pair.Key);
            }

            foreach (var enemy in m_Forget)
                m_LastSwing.Remove(enemy);
        }

        // ---------------------------------------------------------------- niveau 4 : la herse, la flèche perforante

        // Elle s'abat d'un coup, frappe ceux qui sont devant, puis remonte doucement.
        void UpdatePortcullis(float deltaTime)
        {
            m_PortcullisFall = Mathf.MoveTowards(m_PortcullisFall, 0f, deltaTime * 0.8f);
            if (m_Portcullis != null)
                m_Portcullis.localPosition = m_PortcullisRest + Vector3.down * (m_PortcullisDrop * Mathf.Clamp01(m_PortcullisFall * 1.5f));

            m_PortcullisTimer -= deltaTime * OverchargeRate;
            if (m_PortcullisTimer > 0f)
                return;

            var struck = 0;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsFlyer || !InFront(enemy.transform.position, m_PortcullisReach))
                    continue;

                Hit(enemy, m_PortcullisDamage * Strength, transform.forward);
                enemy.Stun(m_PortcullisStun);
                struck++;
            }

            // Personne devant : elle attend qu'on vienne frapper.
            if (struck == 0)
            {
                m_PortcullisTimer = 0.3f;
                return;
            }

            m_PortcullisTimer = m_PortcullisInterval;
            m_PortcullisFall = 1f;
            Sfx.Play(m_PortcullisClip, transform.position + transform.forward * 0.8f + Vector3.up, 1f, Random.Range(0.8f, 0.9f));
        }

        // Une flèche qui traverse la barricade (le volume autour du mur) devient perforante, une fois par tir.
        void WatchArrows()
        {
            m_Arrows.Reset();
            while (m_Arrows.Next(out var arrow, out var from, out var to))
            {
                if (!CrossesGate(from, to) || !m_Arrows.Once(arrow))
                    continue;

                arrow.AddPierce(m_ArrowPierce);
                arrow.ResetPierced();
                arrow.SetTrailColor(m_PierceTrail);
            }
        }

        bool CrossesGate(Vector3 from, Vector3 to)
        {
            var center = new Vector3(0f, m_ArrowGate.y, 0f);
            var a = transform.InverseTransformPoint(from) - center;
            var b = transform.InverseTransformPoint(to) - center;
            var enter = 0f;
            var exit = 1f;
            for (var axis = 0; axis < 3; axis++)
            {
                var start = a[axis];
                var delta = b[axis] - start;
                var half = m_ArrowGate[axis];
                if (Mathf.Abs(delta) < 1e-6f)
                {
                    if (Mathf.Abs(start) > half)
                        return false;
                    continue;
                }

                var t0 = (-half - start) / delta;
                var t1 = (half - start) / delta;
                if (t0 > t1)
                    (t0, t1) = (t1, t0);
                enter = Mathf.Max(enter, t0);
                exit = Mathf.Min(exit, t1);
                if (enter > exit)
                    return false;
            }

            return true;
        }

        // ---------------------------------------------------------------- niveau 5 : la forteresse

        // Chaque tour de guet tire un carreau sur un ennemi à portée, l'un après l'autre ; sa petite baliste se tourne vers lui
        // (30 octobre : les deux tours fines à la place de la forteresse).
        void ShootFromTowers(float deltaTime)
        {
            if (m_BoltPrefab == null || m_WatchTowers.Length == 0)
                return;

            if (m_TowerTimers.Length != m_WatchTowers.Length)
            {
                m_TowerTimers = new float[m_WatchTowers.Length];
                m_TowerAims = new Quaternion[m_WatchTowers.Length];
                for (var i = 0; i < m_WatchTowers.Length; i++)
                    m_TowerAims[i] = m_WatchTowers[i] != null ? m_WatchTowers[i].rotation : Quaternion.identity;
            }

            var enemies = AliveEnemies();
            for (var i = 0; i < m_WatchTowers.Length; i++)
            {
                m_TowerTimers[i] -= deltaTime * OverchargeRate;
                var tower = m_WatchTowers[i];
                if (tower != null)
                    tower.rotation = Quaternion.RotateTowards(tower.rotation, m_TowerAims[i], 360f * deltaTime);
                if (m_TowerTimers[i] > 0f || tower == null || enemies.Count == 0)
                    continue;

                m_TowerTimers[i] = m_TowerShotInterval * Random.Range(0.8f, 1.2f);
                var muzzle = tower.TransformPoint(m_TowerMuzzle);
                var range = m_TowerRange * RangeScale * OverchargeRange;
                Enemy target = null;
                for (var tries = 0; tries < 6 && target == null; tries++)
                {
                    var candidate = enemies[Random.Range(0, enemies.Count)];
                    if (candidate != null && candidate.IsAlive && (candidate.Center - muzzle).sqrMagnitude <= range * range)
                        target = candidate;
                }

                if (target == null)
                    continue;

                var aim = target.Center + target.Velocity * ((target.Center - muzzle).magnitude / 40f) - muzzle;
                var flat = new Vector3(aim.x, 0f, aim.z);
                if (flat.sqrMagnitude > 1e-4f)
                    m_TowerAims[i] = Quaternion.LookRotation(flat);
                var bolt = Instantiate(m_BoltPrefab, muzzle, Quaternion.LookRotation(aim));
                bolt.transform.localScale *= 0.6f;
                bolt.Launch(aim.normalized * 40f, m_TowerDamage * Strength, 0, this);
            }
        }

        // L'onde : les assaillants devant la barricade repartent en vol vers la forêt, 10 m plus loin.
        void Wave()
        {
            m_WaveTimer = m_WaveCooldown;
            const float up = 7f;
            var flight = 2f * up / Mathf.Max(0.1f, -Physics.gravity.y);
            var forward = Forward();
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsFlyer || enemy.IsAirborne || !InFront(enemy.transform.position, m_WaveReach))
                    continue;

                enemy.Launch(forward * (m_WaveDistance / flight) + Vector3.up * up, this);
            }

            var point = transform.position + forward * 1.5f;
            if (m_WaveEffect != null)
                Destroy(Instantiate(m_WaveEffect, point, Quaternion.identity), 5f);
            Sfx.Play(m_WaveClip, point + Vector3.up, 1f, Random.Range(0.7f, 0.8f));
        }

        // ---------------------------------------------------------------- outils

        // Devant la barricade (côté forêt), à moins de reach mètres, sur sa largeur.
        bool InFront(Vector3 position, float reach)
        {
            var local = transform.InverseTransformPoint(position);
            return local.z > -0.5f && local.z < reach && Mathf.Abs(local.x) <= m_ArrowGate.x + 0.5f && local.y > -1f && local.y < 2.5f;
        }

        Vector3 Forward()
        {
            var forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
        }

        void Hit(Enemy enemy, float damage, Vector3 direction) =>
            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = damage,
                Zone = HitZone.Body,
                Point = enemy.Center,
                Direction = direction,
                Source = this,
            });
    }
}
