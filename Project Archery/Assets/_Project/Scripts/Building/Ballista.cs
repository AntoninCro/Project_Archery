using Archery.Bows;
using Archery.Core;
using Archery.Difficulty;
using Archery.Enemies;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Baliste sur un socle au bord de la voie (GDD, section 9) : elle pivote vers l'ennemi le plus proche à portée,
    /// volants compris, et tire seule un carreau (10 / 14,7 / 21,3 dégâts à la vague 1 en Normal, toutes les 3 / 2 / 1,25 s). Au niveau 3,
    /// le carreau traverse 2 ennemis. Elle profite de la moitié des bonus de dégâts de l'arc.
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

        [SerializeField]
        AudioClip m_FireClip;

        Bow m_Bow;
        Enemy m_Target;
        Vector3 m_TargetPrevious;
        Vector3 m_TargetVelocity;
        Quaternion m_TurretRest = Quaternion.identity;
        float m_Cooldown;
        float m_RetargetTimer;

        protected override void Awake()
        {
            base.Awake();
            if (m_Turret != null)
                m_TurretRest = m_Turret.localRotation;
            if (m_BoltPrefab == null)
                Debug.LogWarning("Ballista : aucun prefab de carreau.", this);
        }

        Vector3 Muzzle => m_Turret != null ? m_Turret.TransformPoint(m_MuzzleOffset) : transform.position + Vector3.up * 2f;

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            var deltaTime = Time.deltaTime;
            m_Cooldown -= deltaTime;
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
            if (m_Cooldown <= 0f && m_Turret != null && Vector3.Angle(m_Turret.forward, direction) < 8f)
                Fire(direction.normalized);
        }

        // L'ennemi vivant le plus proche à portée, et visible depuis la baliste (pas derrière une falaise).
        Enemy FindTarget()
        {
            var muzzle = Muzzle;
            Enemy best = null;
            var range = m_Range * RangeScale;
            var bestDistance = range * range;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || !enemy.IsAlive)
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

        bool IsVisible(Vector3 muzzle, Enemy enemy)
        {
            var toTarget = enemy.Center - muzzle;
            var distance = toTarget.magnitude;
            if (distance < 0.01f)
                return true;

            var count = Physics.RaycastNonAlloc(muzzle, toTarget / distance, s_Hits, distance, Physics.DefaultRaycastLayers,
                                                QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var collider = s_Hits[i].collider;
                if (ArrowIgnore.IsIgnored(collider) || collider.transform.IsChildOf(transform) || collider.GetComponentInParent<Enemy>() != null)
                    continue;
                return false;
            }

            return true;
        }

        Quaternion LimitPitch(Quaternion rotation)
        {
            var euler = rotation.eulerAngles;
            var pitch = Mathf.DeltaAngle(0f, euler.x);
            return Quaternion.Euler(Mathf.Clamp(pitch, -m_MaxPitch, m_MaxPitch), euler.y, 0f);
        }

        void Turn(Quaternion target, float deltaTime)
        {
            if (m_Turret != null)
                m_Turret.rotation = Quaternion.RotateTowards(m_Turret.rotation, target, m_TurnSpeed * deltaTime);
        }

        void Fire(Vector3 direction)
        {
            m_Cooldown = ValueFor(m_IntervalPerLevel, Level, 2.4f) / Mathf.Max(0.1f, DifficultyManager.Current.ballistaFireRate);
            if (m_BoltPrefab == null)
                return;

            var muzzle = Muzzle;
            var bolt = Instantiate(m_BoltPrefab, muzzle, Quaternion.LookRotation(direction));
            bolt.Launch(direction * m_BoltSpeed, Damage, ValueFor(m_PiercePerLevel, Level, 0), this);
            Sfx.Play(m_FireClip, muzzle, 0.9f, Random.Range(0.92f, 1.05f));
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

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(Muzzle, m_Range * RangeScale);
        }
    }
}
