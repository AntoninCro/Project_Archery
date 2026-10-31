using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Building
{
    /// <summary>
    /// Tremplin (refonte du tower defense, étape 5, lot 1 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 6.1 et
    /// 6.2), sur la voie. Depuis le 29 octobre (deuxième brainstorm, partie 12.6), cinq niveaux, chacun une capacité :
    /// <list type="bullet">
    /// <item>1 : un ennemi au sol qui passe dessus est projeté en l'air, vers la forêt (l'avant du tremplin) ;</item>
    /// <item>2 : plus haut, plus longtemps en l'air (le tir au vol rapporte plus) ;</item>
    /// <item>3, la <b>catapulte</b> (l'ancienne catapulte, que le joueur aimait) : un bras attrape l'ennemi et le lance sur le
    /// groupe le plus serré, plus loin sur la voie ; il devient un boulet qui blesse et renverse ceux qu'il percute ;</item>
    /// <item>4 : trois godets, trois ennemis lancés d'un coup ;</item>
    /// <item>5, la <b>rampe de lancement</b> (voulue par le joueur, « de la plus haute importance ») : les ennemis partent dans
    /// le ciel à une vitesse incroyable, avec une traînée de feu et un sifflement, et retombent 5 s plus tard : l'atterrissage
    /// les tue. Les boss ne s'envolent pas : ils y perdent 10 % de leurs PV max. Après le premier essai au casque (29 octobre) :
    /// dix fois plus haut (370 m, « dans la démesure totale »), toujours 5 s de vol, et ils retombent en météores sur le groupe
    /// le plus serré, qu'ils blessent comme le boulet de la catapulte.</item>
    /// </list>
    /// En l'air, l'ennemi est une cible de choix et sa chute lui coûte des PV (<c>Enemy.Launch</c>). Rare.
    /// </summary>
    public class StructureTrampoline : Structure
    {
        [Tooltip("Vitesse (m/s) vers le haut d'un ennemi projeté, aux niveaux 1 et 2.")]
        [SerializeField]
        float[] m_UpSpeedPerLevel = { 8f, 11f };

        [Tooltip("Temps (s) de recharge après chaque saut ou lancer, par niveau.")]
        [SerializeField]
        float[] m_CooldownPerLevel = { 3f, 2.2f, 3.5f, 3f, 2f };

        [Tooltip("Vitesse (m/s) vers l'avant du tremplin (la forêt) : l'ennemi recule.")]
        [SerializeField]
        float m_BackSpeed = 3f;

        [Tooltip("Taille (m) de la zone du tremplin, en X et en Z.")]
        [SerializeField]
        Vector2 m_Size = new Vector2(2.4f, 2.4f);

        [Tooltip("La planche, qui s'enfonce puis rebondit à chaque saut.")]
        [SerializeField]
        Transform m_Plate;

        [SerializeField]
        AudioClip m_BounceClip;

        [Header("Catapulte (niveaux 3 et 4)")]
        [Tooltip("Ennemis lancés d'un coup, par niveau (trois godets au niveau 4).")]
        [SerializeField]
        int[] m_ThrowsPerLevel = { 1, 1, 1, 3, 3 };

        [Tooltip("Distance (m) au centre du tremplin à laquelle le bras attrape un ennemi au sol.")]
        [SerializeField]
        float m_GrabReach = 3.5f;

        [Tooltip("Portée (m) du lancer : le groupe visé est à cette distance au plus.")]
        [SerializeField]
        float m_ThrowRange = 16f;

        [Tooltip("Distance (m) dont l'ennemi est renvoyé vers la forêt quand il n'y a pas de groupe.")]
        [SerializeField]
        float m_ThrowBack = 8f;

        [Tooltip("Durée (s) du vol d'un lancer.")]
        [SerializeField]
        float m_ThrowFlightTime = 1.3f;

        [Tooltip("Dégâts de l'impact du boulet à ceux qui sont autour, par niveau, à la vague 1 en Normal (ils suivent la force " +
                 "des ennemis).")]
        [SerializeField]
        float[] m_ImpactDamagePerLevel = { 0f, 0f, 22f, 32f, 32f };

        [Tooltip("Rayon (m) de l'impact d'un boulet.")]
        [SerializeField]
        float m_ImpactRadius = 2.5f;

        [Tooltip("Part des dégâts de l'impact que prend le boulet lui-même.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_SelfDamage = 0.5f;

        [Tooltip("Le bras de la catapulte (niveau 3), qui bascule autour de son axe X à chaque lancer.")]
        [SerializeField]
        Transform m_Arm;

        [Header("Rampe de lancement (niveau 5)")]
        [Tooltip("Hauteur (m) où montent les ennemis : 370 m, dix fois celle du premier essai (le joueur, 29 octobre : « dans " +
                 "la démesure totale »).")]
        [SerializeField]
        float m_RampHeight = 370f;

        [Tooltip("Durée (s) du vol, quelle que soit la hauteur : la gravité du vol suit (douze fois la vraie pour 370 m en 5 s).")]
        [SerializeField]
        float m_RampFlightTime = 5f;

        [Tooltip("Portée (m) à laquelle la rampe cherche un groupe d'ennemis où faire retomber ceux qu'elle lance.")]
        [SerializeField]
        float m_RampTargetRange = 30f;

        [Tooltip("Vitesse (m/s) à plat, au plus, avec laquelle un ennemi lancé se guide vers le groupe qu'il vise.")]
        [SerializeField]
        float m_RampSteerSpeed = 60f;

        [Tooltip("Vitesse (m/s) vers la forêt quand il n'y a aucun groupe à viser : ils retombent loin en arrière.")]
        [SerializeField]
        float m_RampBackSpeed = 4f;

        [Tooltip("Ennemis lancés d'un coup, au plus : tout le groupe qui est sur la rampe.")]
        [SerializeField]
        int m_RampMax = 6;

        [Tooltip("La zone de la rampe, en part de celle du tremplin (elle est plus grande).")]
        [SerializeField]
        float m_RampZoneScale = 1.4f;

        [Tooltip("Part de ses PV max qu'un boss perd sur la rampe (il ne s'envole pas).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_BossDamageShare = 0.1f;

        [Tooltip("Matériau de la traînée de feu des ennemis lancés.")]
        [SerializeField]
        Material m_TrailMaterial;

        [Tooltip("Le sifflement du départ.")]
        [SerializeField]
        AudioClip m_LaunchClip;

        [Tooltip("L'impact au sol (le boulet de la catapulte, l'atterrissage de la rampe).")]
        [SerializeField]
        AudioClip m_ImpactClip;

        [Tooltip("L'effet de l'impact (poussière, fumée), posé au point de chute.")]
        [SerializeField]
        GameObject m_ImpactEffect;

        // Un ennemi lancé par la rampe : l'heure à laquelle on l'oublie s'il ne retombe pas, le groupe qu'il vise (et quand en
        // chercher un autre s'il n'y en a pas), le temps qu'il lui reste en l'air, et s'il a déjà sifflé en tombant.
        sealed class RampFlight
        {
            public float ForgetAt;
            public Enemy Target;
            public float NextSearch;
            public float TimeLeft;
            public bool Whistled;
        }

        static readonly List<Enemy> s_Candidates = new List<Enemy>();

        // Les ennemis lancés par la catapulte (avec l'heure à laquelle on les oublie s'ils ne retombent pas), et par la rampe.
        readonly Dictionary<Enemy, float> m_Thrown = new Dictionary<Enemy, float>();
        readonly Dictionary<Enemy, RampFlight> m_Ramped = new Dictionary<Enemy, RampFlight>();
        readonly Dictionary<Enemy, TrailRenderer> m_Trails = new Dictionary<Enemy, TrailRenderer>();
        readonly List<Enemy> m_Expired = new List<Enemy>();
        float m_Cooldown;
        float m_Bounce;
        float m_Swing;
        Vector3 m_PlateRest;
        Quaternion m_ArmRest = Quaternion.identity;

        protected override void Awake()
        {
            if (m_Plate != null)
                m_PlateRest = m_Plate.localPosition;
            if (m_Arm != null)
                m_ArmRest = m_Arm.localRotation;
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            Enemy.Landed += OnLanded;
        }

        protected override void OnDestroy()
        {
            Enemy.Landed -= OnLanded;
            foreach (var trail in m_Trails.Values)
            {
                if (trail != null)
                    Destroy(trail.gameObject);
            }

            m_Trails.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            ForgetLost(m_Thrown);
            GuideRamped();
            if (!IsBuilt)
                return;

            // La planche rebondit, puis revient au repos ; le bras de la catapulte bascule d'un coup, puis revient doucement.
            var level = Mathf.Max(1, Level);
            m_Bounce = Mathf.MoveTowards(m_Bounce, 0f, Time.deltaTime * 2.5f);
            if (m_Plate != null)
                m_Plate.localPosition = m_PlateRest + Vector3.up * ((level >= 5 ? 0.7f : 0.35f) * Mathf.Sin(m_Bounce * Mathf.PI));
            m_Swing = Mathf.MoveTowards(m_Swing, 0f, Time.deltaTime * 1.5f);
            if (m_Arm != null)
                m_Arm.localRotation = m_ArmRest * Quaternion.Euler(-70f * Mathf.Sin(Mathf.Sqrt(m_Swing) * Mathf.PI), 0f, 0f);

            if (m_Cooldown > 0f)
            {
                m_Cooldown -= Time.deltaTime * OverchargeRate;
                return;
            }

            var fired = level >= 5 ? FireRamp() : level >= 3 ? Catapult(level) : Bounce(level);
            if (fired)
                m_Cooldown = ValueFor(m_CooldownPerLevel, level, 3f);
        }

        // ---------------------------------------------------------------- niveaux 1 et 2 : le saut

        bool Bounce(int level)
        {
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.CanBeLaunched || enemy.IsAirborne || !Contains(enemy.transform.position, 1f))
                    continue;

                if (!enemy.Launch(Vector3.up * ValueFor(m_UpSpeedPerLevel, level, 8f) + Forward() * m_BackSpeed, this))
                    continue;

                m_Bounce = 1f;
                Sfx.Play(m_BounceClip, transform.position + Vector3.up * 0.3f, 0.9f, Random.Range(0.9f, 1.1f));
                return true;
            }

            return false;
        }

        // ---------------------------------------------------------------- niveaux 3 et 4 : la catapulte

        // Le bras attrape les ennemis au sol les plus proches (un, trois au niveau 4) et les lance chacun sur un groupe.
        bool Catapult(int level)
        {
            var enemies = AliveEnemies();
            s_Candidates.Clear();
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.CanBeLaunched || enemy.IsAirborne || Flat(enemy.transform.position - transform.position) > m_GrabReach)
                    continue;
                s_Candidates.Add(enemy);
            }

            if (s_Candidates.Count == 0)
                return false;

            s_Candidates.Sort((a, b) => Flat(a.transform.position - transform.position).CompareTo(Flat(b.transform.position - transform.position)));
            var throws = Mathf.Min(s_Candidates.Count, Mathf.Max(1, level - 1 < m_ThrowsPerLevel.Length ? m_ThrowsPerLevel[level - 1] : 1));
            var thrown = 0;
            for (var i = 0; i < throws; i++)
            {
                if (Throw(s_Candidates[i], enemies))
                    thrown++;
            }

            if (thrown == 0)
                return false;

            m_Swing = 1f;
            m_Bounce = 1f;
            Sfx.Play(m_BounceClip, transform.position + Vector3.up * 0.5f, 1f, Random.Range(0.75f, 0.85f));
            return true;
        }

        bool Throw(Enemy grabbed, List<Enemy> enemies)
        {
            var start = grabbed.transform.position;
            var group = Densest(enemies, grabbed, m_ThrowRange * RangeScale * OverchargeRange);
            Vector3 landing;
            if (group != null)
            {
                var velocity = group.Velocity;
                velocity.y = 0f;
                landing = group.transform.position + velocity * m_ThrowFlightTime;
            }
            else
            {
                landing = start + TowardForest(grabbed) * m_ThrowBack;
            }

            if (NavMesh.SamplePosition(landing, out var hit, 3f, NavMesh.AllAreas))
                landing = hit.position;

            var throwVelocity = (landing - start) / m_ThrowFlightTime - 0.5f * m_ThrowFlightTime * Physics.gravity;
            if (!grabbed.Launch(throwVelocity, this))
                return false;

            m_Thrown[grabbed] = Time.time + m_ThrowFlightTime + 4f;
            return true;
        }

        // Le centre du groupe d'ennemis au sol le plus serré à portée (au moins 2, sans compter le boulet, loin de lui).
        Enemy Densest(List<Enemy> enemies, Enemy grabbed, float range)
        {
            Enemy best = null;
            var bestCount = 1;
            foreach (var enemy in enemies)
            {
                if (!IsGround(enemy) || enemy == grabbed || m_Thrown.ContainsKey(enemy))
                    continue;

                if (Flat(enemy.transform.position - transform.position) > range ||
                    (enemy.transform.position - grabbed.transform.position).sqrMagnitude < 9f)
                    continue;

                var count = 0;
                foreach (var other in enemies)
                {
                    if (IsGround(other) && other != grabbed &&
                        (other.transform.position - enemy.transform.position).sqrMagnitude <= m_ImpactRadius * m_ImpactRadius)
                        count++;
                }

                if (count > bestCount)
                {
                    best = enemy;
                    bestCount = count;
                }
            }

            return best;
        }

        // ---------------------------------------------------------------- niveau 5 : la rampe de lancement

        // Tout le groupe qui est sur la rampe part dans le ciel ; un boss y perd une part de ses PV. Pour monter à m_RampHeight et
        // retomber en m_RampFlightTime, il faut une gravité de 8 h / t² et une vitesse de 4 h / t (296 m/s pour 370 m en 5 s).
        bool FireRamp()
        {
            var flightTime = Mathf.Max(0.5f, m_RampFlightTime);
            var upSpeed = 4f * m_RampHeight / flightTime;
            var gravityScale = 8f * m_RampHeight / (flightTime * flightTime) / Mathf.Max(0.1f, -Physics.gravity.y);
            var launched = 0;
            var bossHit = false;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || enemy.IsAirborne || enemy.IsFlyer || !Contains(enemy.transform.position, m_RampZoneScale))
                    continue;

                if (enemy.IsBoss)
                {
                    Hit(enemy, enemy.Health.Max * m_BossDamageShare, Vector3.up);
                    enemy.Launch(Forward() * (m_RampBackSpeed * 2f), this);
                    bossHit = true;
                    continue;
                }

                if (launched >= m_RampMax || !enemy.CanBeLaunched)
                    continue;

                // Vers la forêt tant qu'il n'a pas de groupe à viser (GuideRamped lui en cherche un dès l'image suivante).
                var velocity = Vector3.up * upSpeed + Forward() * m_RampBackSpeed + Random.insideUnitSphere * 1.5f;
                if (!enemy.Launch(velocity, this, true, gravityScale))
                    continue;

                m_Ramped[enemy] = new RampFlight { ForgetAt = Time.time + flightTime + 6f, TimeLeft = flightTime };
                AttachTrail(enemy);
                launched++;
            }

            if (launched == 0 && !bossHit)
                return false;

            m_Bounce = 1f;
            Sfx.Play(m_LaunchClip != null ? m_LaunchClip : m_BounceClip, transform.position + Vector3.up, 1f, Random.Range(0.55f, 0.65f));
            Sfx.Play(m_BounceClip, transform.position + Vector3.up * 0.3f, 1f, 0.6f);
            return true;
        }

        // Une traînée de feu derrière l'ennemi lancé : on le suit des yeux jusqu'au ciel, et dans sa chute (à 296 m/s, elle fait
        // plus de 200 m de long : une colonne de feu, puis un météore).
        void AttachTrail(Enemy enemy)
        {
            if (m_TrailMaterial == null || m_Trails.ContainsKey(enemy))
                return;

            var go = new GameObject("Traînée de la rampe");
            go.transform.SetParent(enemy.transform, false);
            go.transform.localPosition = Vector3.up * 0.8f;
            var trail = go.AddComponent<TrailRenderer>();
            trail.sharedMaterial = m_TrailMaterial;
            trail.time = 0.9f;
            trail.minVertexDistance = 0.3f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1.2f, 1f, 0f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.4f),
                        new GradientColorKey(new Color(0.6f, 0.1f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            m_Trails[enemy] = trail;
        }

        // La traînée reste un instant là où elle est, puis s'efface.
        void ReleaseTrail(Enemy enemy)
        {
            if (!m_Trails.TryGetValue(enemy, out var trail))
                return;

            m_Trails.Remove(enemy);
            if (trail == null)
                return;

            trail.transform.SetParent(null, true);
            trail.emitting = false;
            Destroy(trail.gameObject, trail.time + 0.1f);
        }

        // Les ennemis de la rampe se guident, tout le vol, vers le groupe qu'ils visent, là où il sera quand ils retomberont ; une
        // cible perdue (morte, projetée, montée sur la rampe) est remplacée. Un ennemi tué en l'air retombe quand même : son impact
        // compte. Peu avant l'impact, le sifflement de la chute, là où il va tomber.
        void GuideRamped()
        {
            if (m_Ramped.Count == 0)
                return;

            m_Expired.Clear();
            List<Enemy> enemies = null;
            foreach (var pair in m_Ramped)
            {
                var enemy = pair.Key;
                var flight = pair.Value;
                if (enemy == null || !enemy.IsAirborne || Time.time > flight.ForgetAt)
                {
                    m_Expired.Add(enemy);
                    continue;
                }

                if ((!IsGround(flight.Target) || Contains(flight.Target.transform.position, m_RampZoneScale)) && Time.time >= flight.NextSearch)
                {
                    enemies ??= AliveEnemies();
                    flight.Target = RampTarget(enemies, enemy);
                    flight.NextSearch = Time.time + 0.25f;
                }

                // Sans groupe à viser, il garde son élan (vers la forêt, ou vers sa dernière cible).
                if (!IsGround(flight.Target))
                    continue;

                var drift = flight.Target.Velocity;
                drift.y = 0f;
                var landing = flight.Target.transform.position + drift * Mathf.Max(0f, flight.TimeLeft);
                flight.TimeLeft = enemy.GuideFlight(landing, m_RampSteerSpeed);
                if (!flight.Whistled && flight.TimeLeft >= 0f && flight.TimeLeft < 0.8f)
                {
                    flight.Whistled = true;
                    Sfx.Play(m_LaunchClip, landing + Vector3.up * 3f, 1f, Random.Range(0.75f, 0.85f));
                }
            }

            foreach (var enemy in m_Expired)
            {
                m_Ramped.Remove(enemy);
                ReleaseTrail(enemy);
            }
        }

        // Le groupe où faire retomber un ennemi de la rampe : le centre du groupe d'ennemis au sol le plus serré à portée (un ennemi
        // seul, à défaut), hors de la rampe (ceux-là vont s'envoler) ; un groupe que vise déjà un autre compte moins : la pluie se
        // partage entre les groupes.
        Enemy RampTarget(List<Enemy> enemies, Enemy meteor)
        {
            var range = m_RampTargetRange * RangeScale * OverchargeRange;
            var radius = m_ImpactRadius * m_ImpactRadius;
            Enemy best = null;
            var bestScore = 0f;
            foreach (var enemy in enemies)
            {
                if (!IsGround(enemy) || Flat(enemy.transform.position - transform.position) > range ||
                    Contains(enemy.transform.position, m_RampZoneScale))
                    continue;

                var count = 0;
                foreach (var other in enemies)
                {
                    if (IsGround(other) && (other.transform.position - enemy.transform.position).sqrMagnitude <= radius)
                        count++;
                }

                var shared = 1;
                foreach (var pair in m_Ramped)
                {
                    var target = pair.Value.Target;
                    if (pair.Key != meteor && target != null && (target.transform.position - enemy.transform.position).sqrMagnitude <= radius)
                        shared++;
                }

                var score = count / (float)shared;
                if (score > bestScore)
                {
                    best = enemy;
                    bestScore = score;
                }
            }

            return best;
        }

        // ---------------------------------------------------------------- retombées

        // Le boulet de la catapulte blesse ceux qui sont autour (et lui-même un peu). L'ennemi de la rampe meurt (Enemy.Launch), dans
        // un grand impact qui blesse et renverse ceux qu'il écrase, comme le boulet (le joueur, 29 octobre).
        void OnLanded(Enemy enemy)
        {
            if (enemy == null)
                return;

            var point = enemy.transform.position;
            var ramped = m_Ramped.Remove(enemy);
            if (ramped)
                ReleaseTrail(enemy);
            else if (!m_Thrown.Remove(enemy))
                return;

            var level = Mathf.Max(1, Level);
            var damage = ValueFor(m_ImpactDamagePerLevel, level, 22f) * Strength;
            foreach (var other in AliveEnemies())
            {
                if (other == null || other == enemy || !other.IsAlive || other.IsFlyer)
                    continue;

                var offset = other.transform.position - point;
                if (offset.sqrMagnitude > m_ImpactRadius * m_ImpactRadius)
                    continue;

                offset.y = 0f;
                var direction = offset.sqrMagnitude > 1e-4f ? offset.normalized : Vector3.forward;
                Hit(other, damage, direction);
                if (other.IsAlive)
                    other.Launch(direction * 3f + Vector3.up * 3f, this);
            }

            if (enemy.IsAlive)
                Hit(enemy, damage * m_SelfDamage, Vector3.down);
            Impact(point, ramped ? 1.4f : 0.8f);
        }

        void Impact(Vector3 point, float volume)
        {
            Sfx.Play(m_ImpactClip, point + Vector3.up * 0.3f, volume, Random.Range(0.85f, 1.05f));
            if (m_ImpactEffect != null)
                Destroy(Instantiate(m_ImpactEffect, point, Quaternion.identity), 5f);
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

        // Un ennemi lancé qui n'est jamais retombé (mort en l'air, détruit) : on l'oublie, avec sa traînée.
        void ForgetLost(Dictionary<Enemy, float> launched)
        {
            if (launched.Count == 0)
                return;

            m_Expired.Clear();
            foreach (var pair in launched)
            {
                if (pair.Key == null || !pair.Key.IsAlive || Time.time > pair.Value)
                    m_Expired.Add(pair.Key);
            }

            foreach (var enemy in m_Expired)
            {
                launched.Remove(enemy);
                ReleaseTrail(enemy);
            }
        }

        // ---------------------------------------------------------------- outils

        Vector3 Forward()
        {
            var forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
        }

        static bool IsGround(Enemy enemy) => enemy != null && enemy.IsAlive && !enemy.IsFlyer && !enemy.IsAirborne;

        static float Flat(Vector3 offset) => new Vector2(offset.x, offset.z).magnitude;

        bool Contains(Vector3 position, float scale)
        {
            var local = Quaternion.Inverse(transform.rotation) * (position - transform.position);
            return Mathf.Abs(local.x) <= m_Size.x * 0.5f * scale && Mathf.Abs(local.z) <= m_Size.y * 0.5f * scale && local.y > -0.6f &&
                   local.y < 1.2f;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 1f, 0.5f, 0.6f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(new Vector3(0f, 0.3f, 0f), new Vector3(m_Size.x, 0.6f, m_Size.y));
            Gizmos.DrawWireCube(new Vector3(0f, 0.3f, 0f), new Vector3(m_Size.x * m_RampZoneScale, 0.6f, m_Size.y * m_RampZoneScale));
        }
    }
}
