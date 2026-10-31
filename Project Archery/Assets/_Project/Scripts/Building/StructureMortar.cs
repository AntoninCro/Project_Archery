using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Building
{
    /// <summary>
    /// Mortier (refonte du tower defense, étape 5, lot 2 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, partie 6.1) : au
    /// bord de la voie ou en hauteur. Il tire en cloche un obus là où sera le groupe d'ennemis au sol le plus serré, entre sa
    /// portée minimale et sa portée : l'explosion blesse tous ceux autour (moins au bord). Lent ; deux obus par salve au
    /// niveau 3 (deux tubes). Les volants passent au-dessus. Rare. Depuis le 29 octobre (deuxième brainstorm,
    /// <c>Docs/Refonte_TowerDefense.md</c>, partie 12.6), cinq niveaux :
    /// <list type="bullet">
    /// <item>2 : obus à <b>fragmentation</b>, quatre éclats retombent autour de l'explosion ;</item>
    /// <item>3, le <b>mortier de siège</b> : deux tubes, plus de portée ;</item>
    /// <item>4 : obus de <b>goudron</b>, une flaque qui ralentit ; ta flèche enflammée l'embrase ;</item>
    /// <item>5, le <b>bombardement</b> : toutes les 15 s, 12 obus pleuvent tout le long de la voie, le sol reste en feu et les
    /// ennemis volent dans tous les sens.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Les obus sont calculés par le mortier lui-même (une cloche, comme les ennemis projetés) : pas de physique, rien à
    /// régler sur un prefab d'obus. L'explosion est celle de la flèche explosive (<see cref="ExplosionFx"/>).
    /// </remarks>
    public class StructureMortar : Structure
    {
        static readonly RaycastHit[] s_Hits = new RaycastHit[8];

        [Tooltip("Dégâts au centre de l'explosion, par niveau, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 30f, 42f, 60f };

        [Tooltip("Part des dégâts au bord de l'explosion (1 au centre).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_EdgeDamage = 0.5f;

        [Tooltip("Rayon (m) de l'explosion, par niveau.")]
        [SerializeField]
        float[] m_RadiusPerLevel = { 2.5f, 3f, 3.5f };

        [Tooltip("Temps (s) entre deux salves, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 4.5f, 3.8f, 3.2f };

        [Tooltip("Obus par salve, par niveau (un par tube).")]
        [SerializeField]
        int[] m_ShellsPerLevel = { 1, 1, 2 };

        [Tooltip("Portée (m), par niveau.")]
        [SerializeField]
        float[] m_RangePerLevel = { 24f, 28f, 32f };

        [Tooltip("Portée minimale (m) : un ennemi plus près est trop près pour un tir en cloche.")]
        [SerializeField]
        float m_MinRange = 6f;

        [Tooltip("Durée (s) du vol d'un obus, de la portée minimale (x) à la portée (y).")]
        [SerializeField]
        Vector2 m_FlightTime = new Vector2(1.4f, 2.4f);

        [Header("Tourelle")]
        [Tooltip("Partie qui pivote (autour de la verticale) vers la cible.")]
        [SerializeField]
        Transform m_Turret;

        [Tooltip("Bouches des tubes, d'où partent les obus. Celles qui sont cachées (tube du niveau 3) ne tirent pas.")]
        [SerializeField]
        Transform[] m_Muzzles = new Transform[0];

        [Tooltip("Vitesse de rotation (°/s) de la tourelle.")]
        [SerializeField]
        float m_TurnSpeed = 150f;

        [Header("Obus")]
        [Tooltip("Modèle de l'obus (un boulet). Vide : une sphère.")]
        [SerializeField]
        GameObject m_ShellModel;

        [Tooltip("Diamètre (m) de l'obus.")]
        [SerializeField]
        float m_ShellSize = 0.35f;

        [Tooltip("Optionnel : matériau de la traînée de fumée.")]
        [SerializeField]
        Material m_TrailMaterial;

        [SerializeField]
        Color m_TrailColor = new Color(0.55f, 0.5f, 0.45f, 0.5f);

        [SerializeField]
        AudioClip m_FireClip;

        [Tooltip("Son de l'explosion. Vide : celui de la flèche explosive.")]
        [SerializeField]
        AudioClip m_ExplosionClip;

        [Header("Fragmentation (niveau 2)")]
        [Tooltip("Éclats qui retombent autour de chaque explosion.")]
        [SerializeField]
        int m_Fragments = 4;

        [Tooltip("Dégâts d'un éclat, en part de ceux de l'obus.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_FragmentShare = 0.35f;

        [Tooltip("Rayon (m) de l'explosion d'un éclat.")]
        [SerializeField]
        float m_FragmentRadius = 1.6f;

        [Tooltip("Distance (m), au plus, où retombent les éclats.")]
        [SerializeField]
        float m_FragmentReach = 3.5f;

        [Header("Goudron (niveau 4)")]
        [SerializeField]
        Material m_TarMaterial;

        [Tooltip("Durée (s) de la flaque de goudron.")]
        [SerializeField]
        float m_TarLife = 7f;

        [Tooltip("Vitesse gardée par les ennemis dans le goudron (0,5 = deux fois plus lents).")]
        [Range(0.1f, 1f)]
        [SerializeField]
        float m_TarSpeed = 0.5f;

        [Tooltip("Son du goudron qui s'embrase.")]
        [SerializeField]
        AudioClip m_IgniteClip;

        [Header("Bombardement (niveau 5)")]
        [Tooltip("Temps (s) entre deux bombardements.")]
        [SerializeField]
        float m_BarrageInterval = 15f;

        [Tooltip("Obus d'un bombardement.")]
        [SerializeField]
        int m_BarrageShells = 12;

        [Tooltip("Longueur (m) de voie couverte par un bombardement.")]
        [SerializeField]
        float m_BarrageLength = 24f;

        [Tooltip("Durée (s) du feu au sol.")]
        [SerializeField]
        float m_BarrageFireLife = 5f;

        [Tooltip("Dégâts par seconde du sol en feu, à la vague 1 en Normal.")]
        [SerializeField]
        float m_BarrageBurn = 10f;

        [Tooltip("Vitesse (m/s) à laquelle les explosions du bombardement projettent les ennemis.")]
        [SerializeField]
        float m_BarrageKnock = 6f;

        enum ShellKind
        {
            Normal,
            Fragment,
            Barrage,
        }

        class Shell
        {
            public ShellKind Kind;
            public Transform Body;
            public Vector3 Start;
            public Vector3 Velocity;
            public Vector3 Previous;
            public float Time;
            public float Duration;
            public float Damage;
            public float Radius;
        }

        readonly List<Shell> m_Shells = new List<Shell>();
        readonly List<Enemy> m_Targets = new List<Enemy>();
        float m_Cooldown = 1.5f;
        float m_RetargetTimer;
        float m_BarrageTimer = 5f;
        Enemy m_Target;

        float MaxRange => ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 24f) * RangeScale * OverchargeRange;

        protected override void Update()
        {
            base.Update();
            UpdateShells(Time.deltaTime);
            if (!IsBuilt)
                return;

            var deltaTime = Time.deltaTime;
            m_Cooldown -= deltaTime * OverchargeRate;
            m_RetargetTimer -= deltaTime;
            if (m_RetargetTimer <= 0f || m_Target == null || !m_Target.IsAlive)
            {
                m_RetargetTimer = 0.3f;
                PickTargets();
                m_Target = m_Targets.Count > 0 ? m_Targets[0] : null;
            }

            if (m_Target == null || m_Turret == null)
                return;

            var toTarget = m_Target.transform.position - m_Turret.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 1e-4f)
                return;

            var aim = Quaternion.LookRotation(toTarget.normalized);
            m_Turret.rotation = Quaternion.RotateTowards(m_Turret.rotation, aim, m_TurnSpeed * deltaTime);
            var aimed = Quaternion.Angle(m_Turret.rotation, aim) < 10f;
            if (Level >= 5)
            {
                m_BarrageTimer -= deltaTime * OverchargeRate;
                if (m_BarrageTimer <= 0f && aimed)
                {
                    m_BarrageTimer = m_BarrageInterval;
                    Barrage();
                    return;
                }
            }

            if (m_Cooldown <= 0f && aimed)
                Fire();
        }

        protected override void OnDestroy()
        {
            foreach (var shell in m_Shells)
            {
                if (shell.Body != null)
                    Destroy(shell.Body.gameObject);
            }

            m_Shells.Clear();
            base.OnDestroy();
        }

        // Les ennemis au sol à portée (ni trop près, ni en l'air), du groupe le plus serré au plus clairsemé ; à égalité, le
        // plus proche d'abord.
        void PickTargets()
        {
            m_Targets.Clear();
            var enemies = AliveEnemies();
            var level = Mathf.Max(1, Level);
            var radius = ValueFor(m_RadiusPerLevel, level, 2.5f);
            var max = MaxRange;
            var origin = transform.position;
            foreach (var enemy in enemies)
            {
                if (!IsGroundTarget(enemy))
                    continue;

                var offset = enemy.transform.position - origin;
                offset.y = 0f;
                var distance = offset.magnitude;
                if (distance >= m_MinRange && distance <= max)
                    m_Targets.Add(enemy);
            }

            if (m_Targets.Count < 2)
                return;

            m_Targets.Sort((a, b) =>
            {
                var crowd = Crowd(b, enemies, radius).CompareTo(Crowd(a, enemies, radius));
                return crowd != 0 ? crowd : (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude);
            });
        }

        static bool IsGroundTarget(Enemy enemy) => enemy != null && enemy.IsAlive && !enemy.IsFlyer && !enemy.IsAirborne;

        static int Crowd(Enemy center, List<Enemy> enemies, float radius)
        {
            var count = 0;
            var position = center.transform.position;
            foreach (var enemy in enemies)
            {
                if (IsGroundTarget(enemy) && (enemy.transform.position - position).sqrMagnitude <= radius * radius)
                    count++;
            }

            return count;
        }

        // Une salve : un obus par tube visible, chacun sur l'une des meilleures cibles (la même si elle est seule, un peu à côté).
        void Fire()
        {
            // Les cibles d'il y a un instant ont pu mourir.
            PickTargets();
            if (m_Targets.Count == 0)
                return;

            m_Target = m_Targets[0];
            var level = Mathf.Max(1, Level);
            m_Cooldown = ValueFor(m_IntervalPerLevel, level, 4.5f);
            var damage = ValueFor(m_DamagePerLevel, level, 30f) * Strength;
            var radius = ValueFor(m_RadiusPerLevel, level, 2.5f);
            var shells = ValueFor(m_ShellsPerLevel, level, 1);
            var fired = 0;
            for (var i = 0; i < m_Muzzles.Length && fired < shells; i++)
            {
                var muzzle = m_Muzzles[i];
                if (muzzle == null || !muzzle.gameObject.activeInHierarchy)
                    continue;

                var target = m_Targets.Count > fired ? m_Targets[fired] : m_Target;
                var spread = m_Targets.Count > fired ? Vector3.zero : Random.insideUnitSphere * radius * 0.6f;
                spread.y = 0f;
                Launch(muzzle.position, target, spread, damage, radius);
                fired++;
            }

            if (fired == 0)
                Launch(m_Turret.position + Vector3.up * 1.5f, m_Target, Vector3.zero, damage, radius);
        }

        void Launch(Vector3 start, Enemy target, Vector3 spread, float damage, float radius)
        {
            var duration = FlightTime(start, target.transform.position);

            // Là où l'ennemi sera à l'arrivée de l'obus, au sol (sur le NavMesh qu'il suit).
            var velocity = target.Velocity;
            velocity.y = 0f;
            var landing = target.transform.position + velocity * duration + spread;
            if (NavMesh.SamplePosition(landing, out var hit, 3f, NavMesh.AllAreas))
                landing = hit.position;

            LaunchAt(ShellKind.Normal, start, landing, duration, 0f, damage, radius, m_ShellSize);
            Sfx.Play(m_FireClip, start, 0.9f, Random.Range(0.85f, 1f));
        }

        float FlightTime(Vector3 start, Vector3 landing)
        {
            var flat = landing - start;
            flat.y = 0f;
            return Mathf.Lerp(m_FlightTime.x, m_FlightTime.y, Mathf.InverseLerp(m_MinRange, MaxRange, flat.magnitude));
        }

        // Un obus qui part après delay s et arrive à landing en duration s.
        void LaunchAt(ShellKind kind, Vector3 start, Vector3 landing, float duration, float delay, float damage, float radius, float size)
        {
            m_Shells.Add(new Shell
            {
                Kind = kind,
                Body = SpawnShellBody(start, size),
                Start = start,
                Previous = start,
                Velocity = (landing - start) / duration - 0.5f * duration * Physics.gravity,
                Time = -delay,
                Duration = duration,
                Damage = damage,
                Radius = radius,
            });
        }

        // Le bombardement : 12 obus, l'un après l'autre, tout le long de la voie à l'endroit du groupe visé (dans le sens de sa
        // marche) ; chacun projette les ennemis et laisse le sol en feu.
        void Barrage()
        {
            PickTargets();
            if (m_Targets.Count == 0)
                return;

            var target = m_Targets[0];
            var level = Mathf.Max(1, Level);
            var damage = ValueFor(m_DamagePerLevel, level, 30f) * Strength;
            var radius = ValueFor(m_RadiusPerLevel, level, 2.5f);
            var along = target.Velocity;
            along.y = 0f;
            if (along.sqrMagnitude < 0.25f)
            {
                along = target.transform.position - transform.position;
                along.y = 0f;
                along = Vector3.Cross(Vector3.up, along);
            }

            along = along.sqrMagnitude > 1e-4f ? along.normalized : Vector3.forward;
            var start = m_Muzzles.Length > 0 && m_Muzzles[0] != null ? m_Muzzles[0].position : m_Turret.position + Vector3.up * 1.5f;
            var count = Mathf.Max(1, m_BarrageShells);
            for (var i = 0; i < count; i++)
            {
                var offset = count > 1 ? Mathf.Lerp(-0.5f, 0.5f, i / (float)(count - 1)) * m_BarrageLength : 0f;
                var landing = target.transform.position + along * offset + Vector3.Cross(Vector3.up, along) * Random.Range(-1.5f, 1.5f);
                if (NavMesh.SamplePosition(landing, out var hit, 4f, NavMesh.AllAreas))
                    landing = hit.position;
                LaunchAt(ShellKind.Barrage, start, landing, FlightTime(start, landing) + 0.4f, i * 0.09f, damage, radius, m_ShellSize * 1.4f);
            }

            m_Cooldown = ValueFor(m_IntervalPerLevel, level, 4.5f);
            Sfx.Play(m_FireClip, start, 1f, 0.7f);
            Sfx.Play(m_FireClip, start, 1f, 0.55f);
        }

        Transform SpawnShellBody(Vector3 position, float shellSize)
        {
            GameObject body;
            if (m_ShellModel != null)
            {
                body = Instantiate(m_ShellModel, position, Random.rotation);
                var bounds = new Bounds(position, Vector3.zero);
                var any = false;
                foreach (var renderer in body.GetComponentsInChildren<Renderer>())
                {
                    if (any)
                        bounds.Encapsulate(renderer.bounds);
                    else
                        bounds = renderer.bounds;
                    any = true;
                }

                var size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                if (size > 1e-4f)
                    body.transform.localScale *= shellSize / size;
            }
            else
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.transform.position = position;
                body.transform.localScale = Vector3.one * shellSize;
            }

            body.name = "Obus de mortier";
            foreach (var collider in body.GetComponentsInChildren<Collider>())
                Destroy(collider);

            if (m_TrailMaterial != null)
            {
                var trail = body.AddComponent<TrailRenderer>();
                trail.sharedMaterial = m_TrailMaterial;
                trail.time = 0.35f;
                trail.startWidth = shellSize * 0.8f;
                trail.endWidth = 0f;
                trail.startColor = m_TrailColor;
                var end = m_TrailColor;
                end.a = 0f;
                trail.endColor = end;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            return body.transform;
        }

        // Chaque obus suit sa cloche ; il explose à l'arrivée, ou avant s'il heurte le décor (une falaise, un toit).
        void UpdateShells(float deltaTime)
        {
            for (var i = m_Shells.Count - 1; i >= 0; i--)
            {
                var shell = m_Shells[i];
                shell.Time += deltaTime;
                var t = Mathf.Clamp(shell.Time, 0f, shell.Duration);
                var position = shell.Start + shell.Velocity * t + 0.5f * t * t * Physics.gravity;
                var impact = shell.Time >= shell.Duration;
                if (!impact && shell.Time > 0.2f && HitsScenery(shell.Previous, position, out var point))
                {
                    position = point;
                    impact = true;
                }

                if (shell.Body != null)
                    shell.Body.position = position;
                shell.Previous = position;
                if (!impact)
                    continue;

                m_Shells.RemoveAt(i);
                Explode(position, shell);
                if (shell.Body != null)
                    Destroy(shell.Body.gameObject);
            }
        }

        static bool HitsScenery(Vector3 from, Vector3 to, out Vector3 point)
        {
            point = to;
            var step = to - from;
            var length = step.magnitude;
            if (length < 1e-4f)
                return false;

            var count = Physics.RaycastNonAlloc(from, step / length, s_Hits, length, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var collider = s_Hits[i].collider;
                if (collider == null || ArrowIgnore.IsIgnored(collider) || collider.GetComponentInParent<Enemy>() != null || s_Hits[i].distance >= best)
                    continue;

                best = s_Hits[i].distance;
                point = s_Hits[i].point;
            }

            return best < float.MaxValue;
        }

        void Explode(Vector3 point, Shell shell)
        {
            var damage = shell.Damage;
            var radius = shell.Radius;
            var barrage = shell.Kind == ShellKind.Barrage;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var offset = enemy.transform.position - point;
                var distance = offset.magnitude;
                if (distance > radius)
                    continue;

                offset.y = 0f;
                enemy.Health.TakeDamage(new DamageInfo
                {
                    Amount = damage * Mathf.Lerp(1f, m_EdgeDamage, distance / radius),
                    Zone = HitZone.Body,
                    Point = enemy.Center,
                    Direction = offset.sqrMagnitude > 1e-4f ? offset.normalized : Vector3.down,
                    Source = this,
                });

                // Le bombardement : ils volent dans tous les sens.
                if (barrage && enemy.IsAlive && !enemy.IsFlyer)
                {
                    var away = offset.sqrMagnitude > 1e-4f ? offset.normalized : Random.insideUnitSphere;
                    away.y = 0f;
                    enemy.Launch(away.normalized * m_BarrageKnock + Vector3.up * Random.Range(6f, 9f), this);
                }
            }

            var level = Mathf.Max(1, Level);
            // Pas d'éclats pour les obus du bombardement : 60 explosions de plus, c'est trop pour le Quest.
            if (shell.Kind == ShellKind.Normal && level >= 2)
                Fragment(point, damage);
            if (shell.Kind == ShellKind.Normal && level >= 4 && m_TarMaterial != null)
                GroundPatch.Spawn(point, radius * 0.9f, m_TarLife, m_TarSpeed, m_TarMaterial, this).IgniteClip = m_IgniteClip;
            if (barrage && m_TarMaterial != null)
                GroundPatch.Spawn(point, radius * 0.8f, m_BarrageFireLife, 1f, m_TarMaterial, this).Ignite(m_BarrageBurn * Strength, m_BarrageFireLife, m_IgniteClip);

            var arrows = SpecialArrows.Instance;
            if (arrows != null && arrows.ExplosionFireMaterial != null && arrows.ExplosionSmokeMaterial != null)
                ExplosionFx.Play(point + Vector3.up * 0.3f, radius, arrows.ExplosionFireMaterial, arrows.ExplosionSmokeMaterial, null);
            Sfx.Play(m_ExplosionClip != null ? m_ExplosionClip : arrows != null ? arrows.ExplosionClip : null, point + Vector3.up * 0.5f,
                     shell.Kind == ShellKind.Fragment ? 0.45f : 1f, shell.Kind == ShellKind.Fragment ? 1.3f : 1f);
        }

        // Les éclats : de petits obus qui retombent autour de l'explosion, chacun sa petite explosion.
        void Fragment(Vector3 point, float damage)
        {
            for (var i = 0; i < m_Fragments; i++)
            {
                var direction = Quaternion.Euler(0f, i * 360f / Mathf.Max(1, m_Fragments) + Random.Range(-25f, 25f), 0f) * Vector3.forward;
                var landing = point + direction * Random.Range(m_FragmentReach * 0.5f, m_FragmentReach);
                if (NavMesh.SamplePosition(landing, out var hit, 2f, NavMesh.AllAreas))
                    landing = hit.position;
                LaunchAt(ShellKind.Fragment, point + Vector3.up * 0.3f, landing, 0.55f, 0f, damage * m_FragmentShare, m_FragmentRadius, m_ShellSize * 0.45f);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, m_MinRange);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 24f));
        }
    }
}
