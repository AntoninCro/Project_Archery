using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Difficulty;
using Archery.Enemies;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Baliste sur un socle au bord de la voie (GDD, section 9) : elle pivote vers l'ennemi le plus proche à portée,
    /// volants compris, et tire seule un carreau (10 / 14,7 / 21,3 dégâts à la vague 1 en Normal, toutes les 3 / 2 / 1,25 s). Au niveau 3,
    /// le carreau traverse 2 ennemis. Elle profite de la moitié des bonus de dégâts de l'arc. Depuis le 29 octobre (deuxième
    /// brainstorm, <c>Docs/Refonte_TowerDefense.md</c>, partie 12.6), cinq niveaux :
    /// <list type="bullet">
    /// <item>2 : carreaux perforants (3 ennemis) ;</item>
    /// <item>3, le <b>scorpion</b> : double arc, un deuxième carreau sur une deuxième cible ;</item>
    /// <item>4, le <b>harpon</b> : le carreau cloue sa cible au sol 1 s, et fait tomber les volants ;</item>
    /// <item>5, la <b>baliste titan</b> : toutes les 6 s, un pieu géant traverse toute la voie, embroche les ennemis et les
    /// emporte avec lui.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Les éliminations de la baliste rapportent leurs points et leur or, mais ses touches ne comptent ni pour les
    /// points de touche ni pour le combo (le score ne les compte que pour les flèches de l'arc).
    /// </remarks>
    public class Ballista : Structure
    {
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        [Tooltip("Portée (m).")]
        [SerializeField]
        float m_Range = 20f;

        [Tooltip("Dégâts d'un carreau, par niveau, à la vague 1 en Normal (ils suivent ensuite la force des ennemis), avant le bonus de l'arc.")]
        [SerializeField]
        float[] m_DamagePerLevel = { 10f, 14.67f, 21.33f };

        [Tooltip("Temps (s) entre deux tirs, par niveau : lente au début, la baliste garde toute sa cadence au niveau 3.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 3f, 2f, 1.25f };

        [Tooltip("Ennemis qu'un carreau traverse avant de s'arrêter, par niveau (0 = il s'arrête au premier).")]
        [SerializeField]
        int[] m_PiercePerLevel = { 0, 0, 2 };

        [Tooltip("Part des bonus de dégâts de l'arc (améliorations, bonus des coffres) dont profite la baliste.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_BowBonusShare = 0.5f;

        [Header("Évolutions (29 octobre)")]
        [Tooltip("Niveau du scorpion : un deuxième carreau sur une deuxième cible.")]
        [SerializeField]
        int m_ScorpionFromLevel = 3;

        [Tooltip("Niveau du harpon : le carreau cloue sa cible au sol.")]
        [SerializeField]
        int m_HarpoonFromLevel = 4;

        [Tooltip("Temps (s) pendant lequel le harpon cloue un ennemi au sol.")]
        [SerializeField]
        float m_HarpoonPin = 1f;

        [Tooltip("Temps (s) au sol d'un volant harponné.")]
        [SerializeField]
        float m_HarpoonFlyerHold = 1.5f;

        [Tooltip("Niveau de la baliste titan : un pieu géant de temps en temps.")]
        [SerializeField]
        int m_TitanFromLevel = 5;

        [Tooltip("Temps (s) entre deux pieux géants.")]
        [SerializeField]
        float m_TitanInterval = 6f;

        [Tooltip("Dégâts du pieu géant, en carreaux.")]
        [SerializeField]
        float m_TitanDamageScale = 6f;

        [Tooltip("Vitesse (m/s) du pieu géant.")]
        [SerializeField]
        float m_TitanSpeed = 55f;

        [Tooltip("Taille du pieu géant, en carreaux.")]
        [SerializeField]
        float m_TitanScale = 4f;

        [Tooltip("Vitesse (m/s) à laquelle le pieu emporte ceux qu'il embroche.")]
        [SerializeField]
        float m_TitanCarry = 12f;

        [Tooltip("D'où part le pieu géant (la bouche du grand modèle) ; vide : le point de départ des carreaux.")]
        [SerializeField]
        Transform m_TitanMuzzle;

        [SerializeField]
        AudioClip m_TitanClip;

        [Header("Tir")]
        [SerializeField]
        BallistaBolt m_BoltPrefab;

        [Tooltip("Vitesse (m/s) du carreau. Il vole en ligne droite.")]
        [SerializeField]
        float m_BoltSpeed = 45f;

        [Tooltip("Partie qui pivote vers la cible (le modèle de la baliste). Son axe Z est la direction de tir.")]
        [SerializeField]
        Transform m_Turret;

        [Tooltip("Point de départ du carreau, dans le repère de la tourelle (avant l'échelle du modèle).")]
        [SerializeField]
        Vector3 m_MuzzleOffset = new Vector3(0f, 0.41f, 0.45f);

        [Tooltip("Vitesse de rotation (°/s) de la tourelle.")]
        [SerializeField]
        float m_TurnSpeed = 220f;

        [Tooltip("Inclinaison maximale (°) de la tourelle, vers le haut ou vers le bas.")]
        [SerializeField]
        float m_MaxPitch = 40f;

        [Tooltip("Inclinaison maximale (°) vers le bas sur la tour et en hauteur (28 octobre : la baliste sur la tour ne pouvait " +
                 "pas viser les ennemis collés au pied de la tour, plus bas que son inclinaison maximale).")]
        [SerializeField]
        float m_MaxPitchDownElevated = 80f;

        [SerializeField]
        AudioClip m_FireClip;

        Bow m_Bow;
        Enemy m_Target;
        Vector3 m_TargetPrevious;
        Vector3 m_TargetVelocity;
        Quaternion m_TurretRest = Quaternion.identity;
        float m_Cooldown;
        float m_RetargetTimer;
        float m_TitanTimer = 2f;

        /// <summary>Vrai pour la baliste et ses carreaux ; faux pour le lance-filet, qui a ses propres évolutions.</summary>
        protected virtual bool FiresBolts => true;

        protected override void Awake()
        {
            base.Awake();
            if (m_Turret != null)
                m_TurretRest = m_Turret.localRotation;
            if (m_BoltPrefab == null)
                Debug.LogWarning("Ballista : aucun prefab de carreau.", this);
        }

        /// <summary>Point de départ des carreaux.</summary>
        protected Vector3 Muzzle => m_Turret != null ? m_Turret.TransformPoint(m_MuzzleOffset) : transform.position + Vector3.up * 2f;

        /// <summary>Direction où pointe la tourelle (celle des carreaux).</summary>
        protected Vector3 TurretForward => m_Turret != null ? m_Turret.forward : transform.forward;

        /// <summary>Ennemi visé (null si aucun à portée).</summary>
        protected Enemy Target => m_Target;

        /// <summary>Portée actuelle (m), arène et surcharge comprises.</summary>
        protected float Range => m_Range * RangeScale * OverchargeRange;

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            var deltaTime = Time.deltaTime;
            m_Cooldown -= deltaTime * OverchargeRate;
            m_RetargetTimer -= deltaTime;
            if (m_RetargetTimer <= 0f || m_Target == null || !m_Target.IsAlive)
            {
                m_RetargetTimer = 0.25f;
                var previous = m_Target;
                m_Target = FindTarget();
                if (m_Target != previous && m_Target != null)
                {
                    m_TargetPrevious = m_Target.Center;
                    m_TargetVelocity = Vector3.zero;
                }
            }

            if (m_Target == null)
            {
                Turn(transform.rotation * m_TurretRest, deltaTime);
                return;
            }

            // Vitesse de la cible, lissée, pour tirer là où elle sera.
            var center = m_Target.Center;
            if (deltaTime > 0f)
                m_TargetVelocity = Vector3.Lerp(m_TargetVelocity, (center - m_TargetPrevious) / deltaTime, 1f - Mathf.Exp(-8f * deltaTime));
            m_TargetPrevious = center;

            var muzzle = Muzzle;
            var aim = center + m_TargetVelocity * (Vector3.Distance(muzzle, center) / Mathf.Max(1f, m_BoltSpeed));
            var direction = aim - muzzle;
            if (direction.sqrMagnitude < 1e-4f)
                return;

            Turn(LimitPitch(Quaternion.LookRotation(direction.normalized)), deltaTime);
            var aimed = m_Turret != null && Vector3.Angle(m_Turret.forward, direction) < 8f;
            if (FiresBolts && Level >= m_TitanFromLevel)
            {
                m_TitanTimer -= deltaTime * OverchargeRate;
                if (m_TitanTimer <= 0f && aimed)
                {
                    m_TitanTimer = m_TitanInterval;
                    ShootTitan(direction.normalized);
                }
            }

            if (m_Cooldown <= 0f && aimed)
                Fire(direction.normalized);
        }

        // L'ennemi vivant le plus proche à portée, et visible depuis la baliste (pas derrière une falaise).
        protected virtual Enemy FindTarget()
        {
            var muzzle = Muzzle;
            Enemy best = null;
            var range = Range;
            var bestDistance = range * range;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsTrainingDummy)
                    continue;

                var distance = (enemy.Center - muzzle).sqrMagnitude;
                if (distance < bestDistance && IsVisible(muzzle, enemy))
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }

            return best;
        }

        protected bool IsVisible(Vector3 muzzle, Enemy enemy)
        {
            var toTarget = enemy.Center - muzzle;
            var distance = toTarget.magnitude;
            if (distance < 0.01f)
                return true;

            var count = Physics.RaycastNonAlloc(muzzle, toTarget / distance, s_Hits, distance, Physics.DefaultRaycastLayers,
                                                QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                // Comme le carreau (BallistaBolt), la vue passe au travers de ce qui a des PV : la tour, une barricade (28 octobre :
                // depuis la tour, le bord de la plate-forme cachait les ennemis collés à son pied, que le carreau aurait touchés).
                var collider = s_Hits[i].collider;
                if (ArrowIgnore.IsIgnored(collider) || collider.transform.IsChildOf(transform) || collider.GetComponentInParent<Enemy>() != null ||
                    collider.GetComponentInParent<Health>() != null)
                    continue;
                return false;
            }

            return true;
        }

        // Le cabrage de la tourelle : jusqu'à m_MaxPitch, et plus bas sur la tour et en hauteur (une valeur positive vise vers le bas).
        Quaternion LimitPitch(Quaternion rotation)
        {
            var euler = rotation.eulerAngles;
            var pitch = Mathf.DeltaAngle(0f, euler.x);
            var elevated = Socket != null && (Socket.Slot == BuildSlot.Tower || Socket.Slot == BuildSlot.Height);
            var down = elevated ? Mathf.Max(m_MaxPitch, m_MaxPitchDownElevated) : m_MaxPitch;
            return Quaternion.Euler(Mathf.Clamp(pitch, -m_MaxPitch, down), euler.y, 0f);
        }

        void Turn(Quaternion target, float deltaTime)
        {
            if (m_Turret != null)
                m_Turret.rotation = Quaternion.RotateTowards(m_Turret.rotation, target, m_TurnSpeed * deltaTime);
        }

        // La tourelle est pointée : un tir, puis l'attente. L'arbalète à répétition (28 octobre) y ajoute sa rafale.
        protected virtual void Fire(Vector3 direction)
        {
            m_Cooldown = ValueFor(m_IntervalPerLevel, Level, 2.4f) / Mathf.Max(0.1f, DifficultyManager.Current.ballistaFireRate);
            Shoot(direction);
        }

        /// <summary>Dégâts du prochain carreau (l'arbalète à répétition blesse plus les volants).</summary>
        protected virtual float ShotDamage => Damage;

        /// <summary>Un carreau dans cette direction, sans toucher à la cadence (le lance-filet lance un filet à la place).</summary>
        protected virtual void Shoot(Vector3 direction)
        {
            if (m_BoltPrefab == null)
                return;

            var muzzle = Muzzle;
            System.Action<Enemy, Vector3> harpoon = FiresBolts && Level >= m_HarpoonFromLevel ? Harpoon : null;
            var bolt = Instantiate(m_BoltPrefab, muzzle, Quaternion.LookRotation(direction));
            bolt.Launch(direction * m_BoltSpeed, ShotDamage, ValueFor(m_PiercePerLevel, Level, 0), this, harpoon);
            Sfx.Play(m_FireClip, muzzle, 0.9f, Random.Range(0.92f, 1.05f));

            // Le scorpion : le deuxième arc tire sur la cible visible la plus proche après la première.
            if (!FiresBolts || Level < m_ScorpionFromLevel)
                return;

            var second = SecondTarget(muzzle);
            if (second == null)
                return;

            var toSecond = second.Center + second.Velocity * (Vector3.Distance(muzzle, second.Center) / Mathf.Max(1f, m_BoltSpeed)) - muzzle;
            var other = Instantiate(m_BoltPrefab, muzzle, Quaternion.LookRotation(toSecond));
            other.Launch(toSecond.normalized * m_BoltSpeed, ShotDamage, ValueFor(m_PiercePerLevel, Level, 0), this, harpoon);
        }

        Enemy SecondTarget(Vector3 muzzle)
        {
            Enemy best = null;
            var range = Range;
            var bestDistance = range * range;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || enemy == m_Target || !enemy.IsAlive)
                    continue;

                var distance = (enemy.Center - muzzle).sqrMagnitude;
                if (distance < bestDistance && IsVisible(muzzle, enemy))
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }

            return best;
        }

        // Le harpon : un ennemi au sol est cloué (étourdi), un volant tombe au sol (comme pris dans un filet). Cloué, il est
        // préparé pour ta flèche (« Cloué ! », 30 octobre).
        void Harpoon(Enemy enemy, Vector3 direction)
        {
            if (enemy == null || !enemy.IsAlive)
                return;
            if (enemy.IsFlyer)
            {
                enemy.Net(m_HarpoonFlyerHold, this);
            }
            else
            {
                enemy.Stun(m_HarpoonPin);
                enemy.Prepare(Preparation.Pinned, m_HarpoonPin);
            }
        }

        // La baliste titan : un pieu géant qui traverse tout, et emporte ceux qu'il embroche (ils volent avec lui).
        void ShootTitan(Vector3 direction)
        {
            if (m_BoltPrefab == null)
                return;

            var muzzle = m_TitanMuzzle != null && m_TitanMuzzle.gameObject.activeInHierarchy ? m_TitanMuzzle.position : Muzzle;
            if (m_Target != null && (m_Target.Center - muzzle).sqrMagnitude > 1f)
                direction = (m_Target.Center - muzzle).normalized;
            var bolt = Instantiate(m_BoltPrefab, muzzle, Quaternion.LookRotation(direction));
            bolt.transform.localScale *= m_TitanScale;
            bolt.Launch(direction * m_TitanSpeed, ShotDamage * m_TitanDamageScale, 99, this, Skewer);
            if (bolt.TryGetComponent<TrailRenderer>(out var trail))
            {
                trail.widthMultiplier = m_TitanScale * 1.5f;
                trail.time = 0.35f;
            }

            Sfx.Play(m_TitanClip != null ? m_TitanClip : m_FireClip, muzzle, 1f, 0.6f);
        }

        void Skewer(Enemy enemy, Vector3 direction)
        {
            if (enemy == null || !enemy.IsAlive)
                return;

            var flat = direction;
            flat.y = 0f;
            enemy.Launch(flat.normalized * m_TitanCarry + Vector3.up * 4f, this);
        }

        /// <summary>Dégâts d'un carreau : ceux du niveau, suivant la force des ennemis, plus une part des bonus de dégâts de l'arc.</summary>
        public float Damage
        {
            get
            {
                if (m_Bow == null)
                    m_Bow = FindAnyObjectByType<Bow>();
                var bonus = m_Bow != null ? Mathf.Max(0f, m_Bow.DamageMultiplier - 1f) : 0f;
                return ValueFor(m_DamagePerLevel, Mathf.Max(1, Level), 15f) * Strength * (1f + bonus * m_BowBonusShare);
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(Muzzle, m_Range * RangeScale);
        }
    }
}
