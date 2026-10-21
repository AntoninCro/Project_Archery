using System.Collections.Generic;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Bows
{
    /// <summary>
    /// Pluie de flèches (arc légendaire, <see cref="BowDefinition.arrowRain"/>) : au sommet de sa course, la flèche tirée
    /// se divise. Elle continue vers son point d'arrivée, et les nouvelles flèches retombent en pluie dans un cercle autour
    /// (celui que montre l'aide à la visée) ; à l'arrivée, une zone brûlante reste au sol (<see cref="RainZone"/>). Si la
    /// flèche se plante avant son sommet (tir tendu), la pluie tombe du ciel sur ce point.
    /// </summary>
    /// <remarks>Créé tout seul au premier tir d'un arc à pluie (<see cref="Track"/>).</remarks>
    public class ArrowRain : MonoBehaviour
    {
        // Pas de division sous le nez du joueur : la flèche vole au moins ce temps (s).
        const float k_MinFlightTime = 0.12f;

        // Calcul du point d'arrivée : durée (s) de vol suivie au plus, pas de temps (s).
        const float k_MaxPredictTime = 6f;
        const float k_PredictStep = 0.02f;

        // Tir tendu : la pluie part de cette hauteur (m) au-dessus du point touché et tombe en ce temps (s).
        const float k_SkyHeight = 14f;
        const float k_SkyFallTime = 0.55f;

        // Sécurité : une flèche suivie plus longtemps (s) est oubliée.
        const float k_ForgetAfter = 12f;

        static ArrowRain s_Instance;
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        class Shot
        {
            public BowDefinition Bow;
            public float Damage;
            public ShotGrade Grade;
            public float Time;
            public float Gravity;
        }

        struct PendingZone
        {
            public float Time;
            public Vector3 Point;
            public BowDefinition Bow;
            public float Damage;
        }

        readonly Dictionary<Arrow, Shot> m_Shots = new Dictionary<Arrow, Shot>();
        readonly List<Arrow> m_Ready = new List<Arrow>();
        readonly List<Arrow> m_Forgotten = new List<Arrow>();
        readonly List<PendingZone> m_Zones = new List<PendingZone>();

        /// <summary>Suit cette flèche, tirée par un arc à pluie : elle se divisera au sommet de sa course.</summary>
        /// <param name="damage">Dégâts du tir (qualité comprise) : la pluie et la zone en prennent une part.</param>
        public static void Track(Arrow arrow, BowDefinition bow, float damage, ShotGrade grade)
        {
            if (arrow == null || bow == null)
                return;
            if (s_Instance == null)
                s_Instance = new GameObject("Pluie de flèches").AddComponent<ArrowRain>();
            s_Instance.m_Shots[arrow] = new Shot { Bow = bow, Damage = damage, Grade = grade, Time = Time.time, Gravity = arrow.GravityScale };
        }

        void OnEnable() => Arrow.ShotEnded += OnShotEnded;

        void OnDisable()
        {
            Arrow.ShotEnded -= OnShotEnded;
            m_Shots.Clear();
            m_Zones.Clear();
        }

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        void Update()
        {
            m_Ready.Clear();
            m_Forgotten.Clear();
            foreach (var pair in m_Shots)
            {
                var arrow = pair.Key;
                if (arrow == null || Time.time - pair.Value.Time > k_ForgetAfter)
                    m_Forgotten.Add(arrow);
                else if (arrow.CurrentState == Arrow.State.Flying && Time.time - pair.Value.Time >= k_MinFlightTime &&
                         arrow.Velocity.y <= 0f)
                    m_Ready.Add(arrow);
            }

            foreach (var arrow in m_Forgotten)
                m_Shots.Remove(arrow);

            foreach (var arrow in m_Ready)
            {
                var shot = m_Shots[arrow];
                m_Shots.Remove(arrow);
                SplitAtApex(arrow, shot);
            }

            UpdateZones();
        }

        // Plantée avant son sommet : la pluie tombe du ciel sur ce point. Perdue en l'air : rien.
        void OnShotEnded(Arrow arrow)
        {
            if (!m_Shots.TryGetValue(arrow, out var shot))
                return;

            m_Shots.Remove(arrow);
            if (arrow.CurrentState == Arrow.State.Stuck)
                RainFromSky(arrow.transform.position, shot);
        }

        // Au sommet : la flèche continue vers son point d'arrivée, les autres partent d'ici vers le cercle autour, et
        // arrivent à peu près en même temps qu'elle.
        void SplitAtApex(Arrow arrow, Shot shot)
        {
            var start = arrow.transform.position;
            if (!PredictLanding(start, arrow.Velocity, Gravity(shot), out var landing, out var time))
                return;

            var bow = shot.Bow;
            for (var i = 0; i < bow.rainArrows; i++)
            {
                var from = start + Random.insideUnitSphere * 0.25f;
                var flight = Mathf.Max(0.15f, time * Random.Range(0.9f, 1.1f));
                Launch(from, Ballistic(from, landing + Disc(bow.rainRadius), flight, Gravity(shot)), shot);
            }

            Sfx.Play(bow.rainSplitClip, start, 0.9f, Random.Range(1.05f, 1.2f));
            Schedule(landing, time, shot);
        }

        void RainFromSky(Vector3 point, Shot shot)
        {
            var bow = shot.Bow;
            var ground = GroundBelow(point);
            for (var i = 0; i < bow.rainArrows; i++)
            {
                var from = ground + Vector3.up * k_SkyHeight + Disc(bow.rainRadius * 0.5f);
                Launch(from, Ballistic(from, ground + Disc(bow.rainRadius), k_SkyFallTime * Random.Range(0.9f, 1.1f), Gravity(shot)), shot);
            }

            Sfx.Play(bow.rainSplitClip, ground + Vector3.up * 4f, 0.9f, Random.Range(1.05f, 1.2f));
            Schedule(ground, k_SkyFallTime, shot);
        }

        // Une flèche de la pluie : elle ne casse pas le combo si elle rate, ne traverse pas, ne se divise pas.
        static void Launch(Vector3 from, Vector3 velocity, Shot shot)
        {
            var pool = ArrowPool.Instance;
            if (pool == null || velocity.sqrMagnitude < 1e-4f)
                return;

            var arrow = pool.Get();
            arrow.transform.SetPositionAndRotation(from, Quaternion.LookRotation(velocity.normalized));
            arrow.Launch(new ArrowLaunch
            {
                Velocity = velocity,
                Damage = shot.Damage * Mathf.Max(0f, shot.Bow.rainDamage),
                Grade = shot.Grade,
                Pierce = 0,
                TrailColor = shot.Bow.rainColor,
                IsShot = true,
                IsExtra = true,
                GravityScale = shot.Gravity,
            });
        }

        static Vector3 Gravity(Shot shot) => Physics.gravity * (shot.Gravity > 0f ? shot.Gravity : 1f);

        void Schedule(Vector3 landing, float time, Shot shot)
        {
            if (shot.Bow.rainZoneDuration <= 0f)
                return;

            m_Zones.Add(new PendingZone
            {
                Time = Time.time + time,
                Point = GroundBelow(landing),
                Bow = shot.Bow,
                Damage = shot.Damage,
            });
        }

        void UpdateZones()
        {
            for (var i = m_Zones.Count - 1; i >= 0; i--)
            {
                var zone = m_Zones[i];
                if (Time.time < zone.Time)
                    continue;

                m_Zones.RemoveAt(i);
                var bow = zone.Bow;
                RainZone.Spawn(zone.Point, bow.rainRadius, bow.rainZoneDuration, zone.Damage * Mathf.Max(0f, bow.rainZoneDamage),
                               bow.rainColor, bow.rainZoneClip);
            }
        }

        // La trajectoire de la flèche (sa gravité, pas de frottement, comme son Rigidbody et l'aide à la visée), jusqu'au
        // premier obstacle.
        static bool PredictLanding(Vector3 position, Vector3 velocity, Vector3 gravity, out Vector3 landing, out float time)
        {
            time = 0f;
            while (time < k_MaxPredictTime)
            {
                var next = position + velocity * k_PredictStep + 0.5f * k_PredictStep * k_PredictStep * gravity;
                var segment = next - position;
                var length = segment.magnitude;
                if (length > 1e-5f && TryHit(position, segment / length, length, false, out var hit))
                {
                    landing = hit.point;
                    time += k_PredictStep * hit.distance / length;
                    return true;
                }

                velocity += gravity * k_PredictStep;
                position = next;
                time += k_PredictStep;
            }

            landing = position;
            return false;
        }

        // Le sol sous ce point (pas un ennemi), sinon le sol où marchent les ennemis (NavMesh), sinon le point lui-même.
        static Vector3 GroundBelow(Vector3 point)
        {
            if (TryHit(point + Vector3.up * 1.5f, Vector3.down, 10f, true, out var hit))
                return hit.point;
            if (NavMesh.SamplePosition(point, out var navHit, 4f, NavMesh.AllAreas))
                return navHit.position;
            return point;
        }

        static bool TryHit(Vector3 origin, Vector3 direction, float distance, bool skipEnemies, out RaycastHit closest)
        {
            closest = default;
            var found = false;
            var count = Physics.RaycastNonAlloc(origin, direction, s_Hits, distance, Physics.DefaultRaycastLayers,
                                                QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if (hit.collider == null || ArrowIgnore.IsIgnored(hit.collider) || (found && hit.distance >= closest.distance))
                    continue;
                if (hit.collider.GetComponentInParent<Arrow>() != null)
                    continue;
                if (skipEnemies && hit.collider.GetComponentInParent<Enemy>() != null)
                    continue;

                closest = hit;
                found = true;
            }

            return found;
        }

        // Vitesse de départ pour aller de « from » à « to » en « time » secondes, sous cette gravité.
        static Vector3 Ballistic(Vector3 from, Vector3 to, float time, Vector3 gravity) =>
            (to - from - 0.5f * time * time * gravity) / time;

        // Un point au hasard dans un disque horizontal de ce rayon, réparti uniformément.
        static Vector3 Disc(float radius)
        {
            var angle = Random.value * Mathf.PI * 2f;
            var distance = radius * Mathf.Sqrt(Random.value);
            return new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Instance = null;
    }
}
