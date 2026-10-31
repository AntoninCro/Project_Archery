using System.Collections;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Defense;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Feu d'artifice</b> (Mortier + Phare ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.17) : un phare rayé et sa
    /// rampe de fusées. Une fusée monte et éclate en bouquet au-dessus du groupe le plus serré : éblouis, les ennemis dessous
    /// s'arrêtent (« Sonné ! ») sous une pluie d'étincelles qui brûle ; ta flèche qui traverse un bouquet s'enflamme.
    /// <list type="bullet">
    /// <item>1 : une fusée toutes les 3 s ;</item>
    /// <item>2 : des fusées à deux étages : chaque bouquet en fait éclater deux autres, à côté ;</item>
    /// <item>3, le <b>bouquet final</b> : vingt fusées d'un coup sur tous les ennemis à portée, le ciel s'allume ; tous éblouis et
    /// brûlés.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Les fusées de la rampe sont des pièces du modèle : une copie part, l'original disparaît le temps de la recharge. Les boss
    /// ne sont éblouis qu'un instant. La flamme d'une flèche passe par <see cref="Brazier.IgniteArrow"/> (le brasier de la tour).
    /// </remarks>
    public class DuoFeuArtifice : TowerDuo
    {
        [Header("La rampe")]
        [Tooltip("Les fusées de la rampe (pièces du modèle) : celles des niveaux 2 et 3 n'en partent qu'une fois montrées.")]
        [SerializeField]
        Transform[] m_Rockets = new Transform[0];

        [Tooltip("La lanterne du phare : elle tourne, plus vite quand une fusée part.")]
        [SerializeField]
        Transform m_Lantern;

        [Tooltip("Portée (m) des fusées.")]
        [SerializeField]
        float m_Range = 38f;

        [Tooltip("Temps (s) entre deux fusées, par niveau (au niveau 3 : entre deux bouquets finals ; une fusée simple entre eux).")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 3f, 2.6f, 2.4f };

        [SerializeField]
        float m_FinaleInterval = 14f;

        [SerializeField]
        int m_FinaleRockets = 20;

        [Tooltip("Durée (s) du vol d'une fusée, et hauteur (m) de l'éclatement au-dessus des ennemis.")]
        [SerializeField]
        float m_FlightTime = 1.1f;

        [SerializeField]
        float m_BurstHeight = 7f;

        [Header("Le bouquet")]
        [Tooltip("Rayon (m, à plat) des ennemis éblouis sous un bouquet.")]
        [SerializeField]
        float m_DazzleRadius = 5f;

        [Tooltip("Temps (s) qu'ils restent éblouis, par niveau.")]
        [SerializeField]
        float[] m_DazzleTimePerLevel = { 2f, 2.2f, 3f };

        [Tooltip("Dégâts de la pluie d'étincelles (en tout, sur sa durée), par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_BurnDamagePerLevel = { 30f, 36f, 60f };

        [SerializeField]
        float m_RainTime = 2f;

        [Tooltip("Rayon (m) où une flèche qui traverse le bouquet s'enflamme, et combien de temps (s).")]
        [SerializeField]
        float m_IgniteRadius = 3.5f;

        [SerializeField]
        float m_IgniteTime = 1.5f;

        [SerializeField]
        Color[] m_Colors =
        {
            new Color(1f, 0.3f, 0.25f, 1f), new Color(1f, 0.8f, 0.25f, 1f), new Color(0.35f, 1f, 0.45f, 1f),
            new Color(0.35f, 0.6f, 1f, 1f), new Color(1f, 0.4f, 0.9f, 1f), new Color(0.75f, 0.45f, 1f, 1f),
        };

        [Header("Sons")]
        [SerializeField]
        AudioClip m_LaunchClip;

        [SerializeField]
        AudioClip m_BurstClip;

        class Rocket
        {
            public Transform Body;
            public Transform Source;
            public Vector3 From;
            public Vector3 Control;
            public Vector3 To;
            public float Time;
            public float Duration;
            public int Level;
            public bool Stage;
            public Color Color;
        }

        class Bouquet
        {
            public Vector3 Point;
            public float Until;
            public float NextTick;
            public float Tick;
        }

        static readonly Collider[] s_Hits = new Collider[32];
        readonly List<Rocket> m_Flying = new List<Rocket>();
        readonly List<Bouquet> m_Bouquets = new List<Bouquet>();
        readonly Dictionary<Transform, float> m_Reload = new Dictionary<Transform, float>();
        readonly List<Vector3> m_Targets = new List<Vector3>();
        float m_NextRocket;
        float m_NextFinale;
        float m_LanternSpin;
        int m_NextIndex;

        protected override void OnDestroy()
        {
            foreach (var rocket in m_Flying)
            {
                if (rocket.Body != null)
                    Destroy(rocket.Body.gameObject);
            }

            m_Flying.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateRockets();
            UpdateBouquets();
            UpdateReload();
            if (!IsBuilt)
                return;

            m_LanternSpin = Mathf.MoveTowards(m_LanternSpin, 40f, 200f * Time.deltaTime);
            if (m_Lantern != null)
                m_Lantern.Rotate(0f, m_LanternSpin * Time.deltaTime, 0f, Space.Self);

            var level = Mathf.Max(1, Level);
            if (level >= 3 && Time.time >= m_NextFinale && Finale(level))
            {
                m_NextFinale = Time.time + m_FinaleInterval / OverchargeRate;
                m_NextRocket = Time.time + 2f;
                return;
            }

            if (Time.time < m_NextRocket || !TryGroup(out var point))
                return;
            m_NextRocket = Time.time + ValueFor(m_IntervalPerLevel, level, 3f) / OverchargeRate;
            Launch(point, level, level >= 2, 0f);
        }

        // Le groupe le plus serré à portée : l'ennemi qui a le plus de voisins à 4 m (les volants comptent, ils sont éblouis aussi).
        bool TryGroup(out Vector3 point)
        {
            point = Vector3.zero;
            var best = 0;
            var near = EnemiesNear(transform.position, m_Range, false);
            foreach (var enemy in near)
            {
                var count = 0;
                foreach (var other in near)
                {
                    if ((other.transform.position - enemy.transform.position).sqrMagnitude < 16f)
                        count++;
                }

                if (count > best)
                {
                    best = count;
                    point = enemy.transform.position;
                }
            }

            return best > 0;
        }

        // Le bouquet final : une fusée sur chaque ennemi à portée (les plus proches d'abord), vingt au plus, en rafale.
        bool Finale(int level)
        {
            m_Targets.Clear();
            foreach (var enemy in EnemiesNear(transform.position, m_Range + 6f, false))
            {
                var tooClose = false;
                foreach (var target in m_Targets)
                {
                    if ((target - enemy.transform.position).sqrMagnitude < 9f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                    m_Targets.Add(enemy.transform.position);
                if (m_Targets.Count >= m_FinaleRockets)
                    break;
            }

            if (m_Targets.Count == 0)
                return false;

            // Le ciel plein : des bouquets de plus, au hasard autour des ennemis, jusqu'à vingt.
            var count = m_Targets.Count;
            for (var i = count; i < m_FinaleRockets; i++)
            {
                var around = m_Targets[i % count];
                m_Targets.Add(around + new Vector3(Random.Range(-7f, 7f), 0f, Random.Range(-7f, 7f)));
            }

            for (var i = 0; i < m_Targets.Count; i++)
                Launch(m_Targets[i], level, false, i * 0.06f);
            m_LanternSpin = 900f;
            FloatingText.Spawn(transform.position + Vector3.up * 11f, "Bouquet final !", new Color(1f, 0.85f, 0.35f, 1f), 2.2f, 2.5f);
            return true;
        }

        // Une fusée part d'une fusée de la rampe (prête et visible), vers le haut, puis vers le point d'éclatement.
        void Launch(Vector3 target, int level, bool stage, float delay)
        {
            var source = NextRocket();
            var from = source != null ? source.position : transform.position + Vector3.up * 6f;
            GameObject body;
            if (source != null)
            {
                body = Instantiate(source.gameObject, from, source.rotation);
                foreach (var collider in body.GetComponentsInChildren<Collider>())
                    Destroy(collider);
                source.gameObject.SetActive(false);
                m_Reload[source] = Time.time + 1.2f;
            }
            else
            {
                // Toute la rampe recharge (le bouquet final) : une copie de la première fusée, sans la cacher.
                var model = m_Rockets.Length > 0 ? m_Rockets[0] : null;
                body = model != null ? Instantiate(model.gameObject, from, model.rotation) : GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                foreach (var collider in body.GetComponentsInChildren<Collider>())
                    Destroy(collider);
                if (model == null)
                    body.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
            }

            body.name = "Fusée";
            body.SetActive(true);
            var to = target + Vector3.up * (m_BurstHeight + Random.Range(-1f, 1.5f));
            var color = m_Colors.Length > 0 ? m_Colors[Random.Range(0, m_Colors.Length)] : Color.white;
            AddTrail(body.transform, color);
            m_Flying.Add(new Rocket
            {
                Body = body.transform,
                Source = source,
                From = from,
                Control = from + Vector3.up * 9f,
                To = to,
                Time = -delay,
                Duration = m_FlightTime * Random.Range(0.9f, 1.15f),
                Level = level,
                Stage = stage,
                Color = color,
            });
            if (delay <= 0f)
                Sfx.Play(m_LaunchClip, from, 0.6f, Random.Range(0.9f, 1.2f));
        }

        Transform NextRocket()
        {
            for (var i = 0; i < m_Rockets.Length; i++)
            {
                var index = (m_NextIndex + i) % Mathf.Max(1, m_Rockets.Length);
                var rocket = m_Rockets[index];
                if (rocket == null || !rocket.gameObject.activeSelf || (rocket.parent != null && !rocket.parent.gameObject.activeInHierarchy))
                    continue;
                m_NextIndex = index + 1;
                return rocket;
            }

            return null;
        }

        void UpdateReload()
        {
            if (m_Reload.Count == 0)
                return;
            Transform ready = null;
            foreach (var pair in m_Reload)
            {
                if (Time.time >= pair.Value)
                {
                    ready = pair.Key;
                    break;
                }
            }

            if (ready == null)
                return;
            m_Reload.Remove(ready);
            if (ready != null)
                ready.gameObject.SetActive(true);
        }

        void UpdateRockets()
        {
            for (var i = m_Flying.Count - 1; i >= 0; i--)
            {
                var rocket = m_Flying[i];
                if (rocket.Body == null)
                {
                    m_Flying.RemoveAt(i);
                    continue;
                }

                rocket.Time += Time.deltaTime;
                var t = Mathf.Clamp01(rocket.Time / rocket.Duration);
                var u = 1f - t;
                var position = u * u * rocket.From + 2f * u * t * rocket.Control + t * t * rocket.To;
                var tangent = 2f * u * (rocket.Control - rocket.From) + 2f * t * (rocket.To - rocket.Control);
                rocket.Body.position = position;
                if (tangent.sqrMagnitude > 1e-4f)
                    rocket.Body.rotation = Quaternion.FromToRotation(Vector3.up, tangent.normalized);
                if (t < 1f)
                    continue;

                m_Flying.RemoveAt(i);
                Destroy(rocket.Body.gameObject, 0.6f);
                rocket.Body.localScale = Vector3.zero;
                Explode(rocket);
            }
        }

        // Le bouquet : une gerbe de couleur, un éclair de lumière ; ceux qui sont dessous sont éblouis, la pluie d'étincelles brûle.
        void Explode(Rocket rocket)
        {
            var point = rocket.To;
            var big = rocket.Level >= 3 ? 1.3f : 1f;
            Burst(point, rocket.Color, (int)(140 * big), 9f * big, 0.45f, 1.5f, 0.25f);
            Burst(point, Color.Lerp(rocket.Color, Color.white, 0.7f), 50, 4f, 0.3f, 1.8f, 0.6f);
            StartCoroutine(Flash(point, rocket.Color));
            Sfx.Play(m_BurstClip, point, 0.8f, Random.Range(0.8f, 1.2f));

            var dazzle = ValueFor(m_DazzleTimePerLevel, rocket.Level, 2f);
            var ground = new Vector3(point.x, point.y - m_BurstHeight, point.z);
            foreach (var enemy in EnemiesNear(ground, m_DazzleRadius, false))
            {
                if (enemy.IsBoss)
                {
                    enemy.Slow(0.6f, dazzle);
                    continue;
                }

                if (!enemy.IsStunned)
                    FloatingText.Spawn(enemy.Center + Vector3.up * 1.2f, "Sonné !", rocket.Color, 1.1f, 1.1f);
                enemy.Stun(dazzle);
                enemy.Prepare(Preparation.Stunned, dazzle);
            }

            m_Bouquets.Add(new Bouquet
            {
                Point = point,
                Until = Time.time + Mathf.Max(m_RainTime, m_IgniteTime),
                NextTick = Time.time + 0.4f,
                Tick = ValueFor(m_BurnDamagePerLevel, rocket.Level, 30f) / Mathf.Max(1f, m_RainTime / 0.4f),
            });

            // Le second étage (niveau 2) : deux bouquets de plus, de part et d'autre.
            if (!rocket.Stage)
                return;
            var side = Vector3.Cross(Vector3.up, (point - transform.position).normalized);
            if (side.sqrMagnitude < 1e-4f)
                side = Vector3.right;
            side.y = 0f;
            side.Normalize();
            for (var s = -1; s <= 1; s += 2)
            {
                var at = point + side * (s * 5f);
                var color = m_Colors.Length > 0 ? m_Colors[Random.Range(0, m_Colors.Length)] : Color.white;
                Explode(new Rocket { To = at, Level = rocket.Level, Stage = false, Color = color });
            }
        }

        // La pluie d'étincelles brûle ceux qui sont dessous ; les flèches qui traversent le bouquet s'enflamment.
        void UpdateBouquets()
        {
            for (var i = m_Bouquets.Count - 1; i >= 0; i--)
            {
                var bouquet = m_Bouquets[i];
                if (Time.time >= bouquet.Until)
                {
                    m_Bouquets.RemoveAt(i);
                    continue;
                }

                IgniteArrows(bouquet.Point);
                if (Time.time < bouquet.NextTick || Time.time > bouquet.Until - Mathf.Max(0f, m_IgniteTime - m_RainTime))
                    continue;
                bouquet.NextTick = Time.time + 0.4f;
                var ground = new Vector3(bouquet.Point.x, bouquet.Point.y - m_BurstHeight, bouquet.Point.z);
                foreach (var enemy in EnemiesNear(ground, m_DazzleRadius, false))
                    Hurt(enemy, bouquet.Tick, enemy.Center, Vector3.down);
            }
        }

        void IgniteArrows(Vector3 point)
        {
            var brazier = Brazier.Instance;
            if (brazier == null)
                return;
            var count = Physics.OverlapSphereNonAlloc(point, m_IgniteRadius, s_Hits, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var arrow = s_Hits[i].GetComponentInParent<Arrow>();
                if (arrow != null && !arrow.IsOnFire)
                    brazier.IgniteArrow(arrow);
            }
        }

        IEnumerator Flash(Vector3 point, Color color)
        {
            var go = new GameObject("Éclair de bouquet");
            go.transform.position = point;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 22f;
            light.shadows = LightShadows.None;
            for (var t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                light.intensity = Mathf.Lerp(12f, 0f, t / 0.6f);
                yield return null;
            }

            Destroy(go);
        }

        // Une traînée d'étincelles derrière la fusée.
        static void AddTrail(Transform body, Color color)
        {
            var arrows = Archery.Upgrades.SpecialArrows.Instance;
            var material = arrows != null ? arrows.EffectMaterial : null;
            if (material == null)
                return;

            var go = new GameObject("Traînée");
            go.transform.SetParent(body, false);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 0.5f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.2f;
            main.maxParticles = 200;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 12f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particles.Play();
        }
    }
}
