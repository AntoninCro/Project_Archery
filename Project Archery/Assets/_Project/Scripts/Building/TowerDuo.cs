using System.Collections.Generic;
using Archery.Combat;
using Archery.Enemies;
using Archery.Upgrades;
using Archery.World;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Un duo (lot 7 de la refonte du tower defense, 31 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.14 et 12.15) : la tour
    /// nouvelle née de la fusion de deux tours. Elle ne garde rien de ce qu'elles savaient faire : elle a son attaque à elle (les
    /// classes qui en héritent), son modèle, et ses 3 niveaux, montés avec les plans de l'une ou l'autre de ses deux tours ; le
    /// niveau 3 est son spectacle.
    /// </summary>
    /// <remarks>
    /// Le joueur, le 31 octobre : deux tours empilées qui gardent leurs deux fonctions plus un effet, « ce n'est pas plaisant à
    /// jouer » ; chaque duo garde un élément de ses tours, le pousse à l'extrême, et apporte une mécanique qu'aucune autre tour n'a.
    /// Le duo naît de <see cref="BuildManager.Fuse"/> à la place des deux tours : au niveau 1 (deux tours au niveau 3), 2 (l'une au
    /// niveau 5) ou 3 (les deux). Il se déplace entre les vagues comme une tour ; on ne le défait pas.
    /// </remarks>
    public abstract class TowerDuo : Structure
    {
        StructureDefinition m_First;
        StructureDefinition m_Second;

        /// <summary>Son entrée du catalogue (nom, couleur, texte), donnée à la fusion.</summary>
        public TowerDuoCatalog.Entry Entry { get; private set; }

        /// <summary>Les deux tours dont il est né : leurs plans font monter ses niveaux.</summary>
        public StructureDefinition First => m_First;

        public StructureDefinition Second => m_Second;

        /// <summary>Le spectacle : son niveau 3.</summary>
        public bool IsSpectacle => Level >= 3;

        /// <summary>Les plans de ce bâtiment font monter ce duo (c'est l'une de ses deux tours).</summary>
        public bool AcceptsPlansOf(StructureDefinition definition) => definition != null && (definition == m_First || definition == m_Second);

        internal void SetupDuo(TowerDuoCatalog.Entry entry, StructureDefinition first, StructureDefinition second)
        {
            Entry = entry;
            m_First = first;
            m_Second = second;
        }

        // ---------------------------------------------------------------- Ce dont les duos se servent tous (1er novembre)

        static readonly List<Enemy> s_Near = new List<Enemy>();
        static readonly List<Enemy> s_Ground = new List<Enemy>();

        /// <summary>
        /// Les ennemis vivants à moins de <paramref name="radius"/> m (à plat) de ce point, sauf <paramref name="except"/> ; au sol
        /// seulement si <paramref name="groundOnly"/> (ni en vol, ni volants). Une liste réutilisée : ne pas l'imbriquer.
        /// </summary>
        protected static List<Enemy> EnemiesNear(Vector3 point, float radius, bool groundOnly = true, Enemy except = null)
        {
            s_Near.Clear();
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || enemy == except || !enemy.IsAlive || (groundOnly && (enemy.IsAirborne || enemy.IsFlyer)))
                    continue;
                var flat = enemy.transform.position - point;
                flat.y = 0f;
                if (flat.sqrMagnitude <= radius * radius)
                    s_Near.Add(enemy);
            }

            return s_Near;
        }

        /// <summary>Les ennemis au sol (ni en vol, ni volants) : une seconde liste réutilisée, qu'on peut parcourir avec EnemiesNear.</summary>
        protected static List<Enemy> GroundEnemies()
        {
            s_Ground.Clear();
            foreach (var enemy in AliveEnemies())
            {
                if (enemy != null && enemy.IsAlive && !enemy.IsAirborne && !enemy.IsFlyer)
                    s_Ground.Add(enemy);
            }

            return s_Ground;
        }

        /// <summary>Blesse l'ennemi (dégâts du réglage, qui suivent la force des ennemis comme ceux des tours) ; vrai s'il vit encore.</summary>
        protected bool Hurt(Enemy enemy, float damage, Vector3 point, Vector3 direction)
        {
            if (enemy == null || !enemy.IsAlive || damage <= 0f)
                return enemy != null && enemy.IsAlive;

            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = damage * Strength,
                Zone = HitZone.Body,
                Point = enemy.Center,
                Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.up,
                Source = this,
            });
            return enemy.IsAlive;
        }

        /// <summary>Renversé : un petit saut loin du point (en l'air, « Au vol ! »). Un boss est seulement poussé.</summary>
        protected bool KnockDown(Enemy enemy, Vector3 from, float push = 2.5f, float lift = 4f)
        {
            var away = enemy.transform.position - from;
            away.y = 0f;
            var flat = away.sqrMagnitude > 1e-4f ? away.normalized : new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            return enemy.Launch(flat * push + Vector3.up * lift, this);
        }

        /// <summary>La voie la plus proche de ce point (sauf <paramref name="except"/>), et où il en est le long d'elle (m depuis la forêt).</summary>
        protected static MapScan.Lane LaneNearest(Vector3 point, MapScan.Lane except, out float along, out float distance)
        {
            along = 0f;
            distance = float.MaxValue;
            var lanes = BuildGrid.Instance != null ? BuildGrid.Instance.Lanes : null;
            if (lanes == null)
                return null;

            MapScan.Lane best = null;
            foreach (var lane in lanes)
            {
                if (lane == except || lane.Points.Count < 2)
                    continue;

                var d = lane.DistanceTo(point, out var at);
                if (d < distance)
                {
                    distance = d;
                    best = lane;
                    along = at;
                }
            }

            return best;
        }

        /// <summary>Vers la forêt, le long de la voie la plus proche de ce point (à plat) ; sans voie, à l'opposé de la tour du joueur.</summary>
        protected static Vector3 ForestwardAt(Vector3 point)
        {
            var lane = LaneNearest(point, null, out var along, out _);
            if (lane != null)
            {
                lane.PointAt(along, out var direction);
                direction.y = 0f;
                if (direction.sqrMagnitude > 1e-4f)
                    return -direction.normalized;
            }

            var tower = Archery.Defense.Tower.Instance;
            var away = tower != null ? point - tower.transform.position : Vector3.forward;
            away.y = 0f;
            return away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
        }

        /// <summary>
        /// Une gerbe de particules colorées (le matériau d'effet des flèches) : <paramref name="count"/> grains, lancés à
        /// <paramref name="speed"/> m/s, qui vivent <paramref name="life"/> s et tombent selon <paramref name="gravity"/>.
        /// </summary>
        protected static void Burst(Vector3 point, Color color, int count, float speed, float size, float life, float gravity,
                                    bool sphere = true)
        {
            var arrows = SpecialArrows.Instance;
            var material = arrows != null ? arrows.EffectMaterial : null;
            if (material == null)
                return;

            var go = new GameObject("Gerbe de duo");
            go.transform.position = point;
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
            main.gravityModifier = gravity;
            main.maxParticles = count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 1000)) });
            var shape = particles.shape;
            shape.shapeType = sphere ? ParticleSystemShapeType.Sphere : ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.2f;
            if (!sphere)
                go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            var colors = particles.colorOverLifetime;
            colors.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colors.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particles.Play();
        }

        /// <summary>Une explosion de feu et de fumée (celle des flèches explosives), de ce rayon.</summary>
        protected static void Explosion(Vector3 point, float radius)
        {
            var arrows = SpecialArrows.Instance;
            if (arrows != null && arrows.ExplosionFireMaterial != null && arrows.ExplosionSmokeMaterial != null)
                ExplosionFx.Play(point, radius, arrows.ExplosionFireMaterial, arrows.ExplosionSmokeMaterial, null);
        }

        /// <summary>Le sol sous ce point (le décor, sans les ennemis ni les déclencheurs) ; sinon la hauteur du point.</summary>
        protected static float GroundY(Vector3 point)
        {
            var from = point + Vector3.up * 6f;
            var hits = Physics.RaycastAll(from, Vector3.down, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            var y = point.y;
            foreach (var hit in hits)
            {
                if (hit.distance < best && hit.collider.GetComponentInParent<Enemy>() == null &&
                    hit.collider.GetComponentInParent<Structure>() == null && !Archery.Bows.ArrowIgnore.IsIgnored(hit.collider))
                {
                    best = hit.distance;
                    y = hit.point.y;
                }
            }

            return y;
        }

        /// <summary>Les ennemis lancés par ce duo et pas encore retombés, oubliés quand ils ne sont plus là.</summary>
        protected static void ForgetDead<T>(Dictionary<Enemy, T> map, List<Enemy> scratch)
        {
            scratch.Clear();
            foreach (var pair in map)
            {
                if (pair.Key == null || !pair.Key.IsAlive)
                    scratch.Add(pair.Key);
            }

            foreach (var enemy in scratch)
                map.Remove(enemy);
        }
    }
}
