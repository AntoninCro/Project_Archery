using System.Collections.Generic;
using Archery.Enemies;
using Archery.FinalBoss;
using Archery.Waves;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.World
{
    /// <summary>
    /// Les torches de la carte (groupe « Torches » de l'objet « Effet ») : le long des voies où passent les ennemis
    /// (fixées aux parois, ou sur pied là où il n'y en a pas), et quelques-unes dans la forêt.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Au début de chaque vague, les torches des voies s'allument en chaîne, du point d'apparition des ennemis
    /// jusqu'à la tour ; entre deux vagues, elles couvent.</item>
    /// <item>Quand un ennemi apparaît, une vague rouge parcourt les torches de sa voie vers la tour : on voit d'où il
    /// arrive.</item>
    /// <item>Une vraie lumière (sans ombre) toutes les quelques torches ; les autres n'ont que leur flamme. Elles sont plus
    /// vives la nuit (<see cref="SkyController.Lamps"/>).</item>
    /// <item>On peut déplacer, dupliquer ou supprimer les torches : en jeu, une torche à moins de 9 m d'une voie en fait
    /// partie, les autres sont « de la forêt » (toujours allumées). La couleur, la portée et la force de leur lumière
    /// servent de départ.</item>
    /// </list>
    /// </remarks>
    public class MapTorches : MonoBehaviour
    {
        static readonly int k_HotColorId = Shader.PropertyToID("_HotColor");
        static readonly int k_MidColorId = Shader.PropertyToID("_MidColor");
        static readonly int k_GlowId = Shader.PropertyToID("_Glow");

        // Noms des pièces d'une torche : c'est par eux qu'on les retrouve en jeu.
        const string k_FlameName = "Feu";
        const string k_EmbersName = "Braises";
        const string k_CoreName = "Cœur du feu";

        // Une torche à moins de cette distance (m) d'une voie s'allume avec elle.
        const float k_LaneReach = 9f;

        class Torch
        {
            public Transform Root;
            public ParticleSystem Flame;
            public float FlameRate;
            public Light Light;
            public Color LightColor;
            public float LightIntensity;
            public float LightRange;
            public MeshRenderer Core;
            public Vector3 CoreScale;
            public int Lane = -1;
            public float Along;
            public float Level;
            public float Target = 1f;
            public float IgniteAt = -1f;
            public float Flare;
            public float Alarm;
            public float Seed;
        }

        struct Pulse
        {
            public int Lane;
            public float Start;
        }

        readonly List<Torch> m_Torches = new List<Torch>();
        readonly List<Pulse> m_Pulses = new List<Pulse>();
        MapLightingSettings m_Settings;
        MapScan m_Scan;
        MaterialPropertyBlock m_Block;
        WaveManager m_Waves;
        bool m_Bound;

        // ---------------------------------------------------------------- fabrication (jeu ou éditeur)

        Mesh m_PoleMesh;
        Mesh m_BasketMesh;
        Mesh m_BracketMesh;
        Mesh m_StickMesh;
        Mesh m_CoreMesh;
        readonly List<Vector3> m_Placed = new List<Vector3>();

        /// <summary>Pose les torches (sous cet objet), le long des voies puis dans la forêt.</summary>
        public void Build(MapLightingSettings settings, MapScan scan)
        {
            m_Settings = settings;
            m_Scan = scan;
            BuildMeshes();
            PlaceLaneTorches();
            PlaceForestTorches();
            if (scan.Lanes.Count == 0)
                Debug.LogWarning("MapTorches : aucune voie trouvée (NavMesh ?) : pas de torches le long des voies.", this);
        }

        // Le long de chaque voie, une torche tous les « spacing » mètres, d'un côté puis de l'autre : fixée à la paroi s'il
        // y en a une tout près, sinon sur pied au bord de la voie. Ni dans l'eau, ni au pied de la tour.
        void PlaceLaneTorches()
        {
            var spacing = Mathf.Max(3f, m_Settings.laneTorchSpacing);
            var index = 0;
            var count = 0;
            foreach (var lane in m_Scan.Lanes)
            {
                for (var along = 6f; along < lane.Length - 8f; along += spacing, index++)
                {
                    var point = lane.PointAt(along, out var direction);
                    var right = Vector3.Cross(Vector3.up, new Vector3(direction.x, 0f, direction.z).normalized);
                    var side = index % 2 == 0 ? right : -right;
                    if (SlowWater.InAnyWater(point + Vector3.up * 0.3f) || TooClose(point, 5f))
                        continue;

                    Transform torch;
                    if (Physics.Raycast(point + Vector3.up * 1.6f, side, out var wall, 7f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                        Mathf.Abs(wall.normal.y) < 0.35f && !MapScan.IsPlant(wall.collider.transform))
                    {
                        var normal = new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
                        var position = new Vector3(wall.point.x, point.y + 1.9f, wall.point.z) + normal * 0.04f;
                        torch = CreateWallTorch(position, normal);
                    }
                    else if (MapScan.TryGround(point + side * 3.3f, out var ground, 2f) && Mathf.Abs(ground.y - point.y) < 1.2f &&
                             lane.DistanceTo(ground, out _) > 2.4f && !SlowWater.InAnyWater(ground + Vector3.up * 0.3f))
                    {
                        torch = CreateStandingTorch(ground, -side);
                    }
                    else
                    {
                        continue;
                    }

                    if (count++ % Mathf.Max(1, m_Settings.lightEvery) == 0)
                        AddLight(torch);
                }
            }
        }

        // Dans la forêt : des torches sur pied au pied de quelques arbres, toutes allumées.
        void PlaceForestTorches()
        {
            var count = 0;
            foreach (var point in MapScan.Spread(m_Scan.ForestPoints, m_Settings.forestTorches, 12f, 23))
            {
                if (TooClose(point, 6f))
                    continue;
                var torch = CreateStandingTorch(point, Quaternion.Euler(0f, EffectBuild.Hash(point) * 360f, 0f) * Vector3.forward);
                if (count++ % 2 == 0)
                    AddLight(torch);
            }
        }

        bool TooClose(Vector3 point, float distance)
        {
            foreach (var placed in m_Placed)
            {
                if ((placed - point).sqrMagnitude < distance * distance)
                    return true;
            }

            return false;
        }

        // Torche sur pied : un manche, une corbeille de fer, la flamme au-dessus.
        Transform CreateStandingTorch(Vector3 ground, Vector3 facing)
        {
            facing.y = 0f;
            var root = new GameObject("Torche").transform;
            root.SetParent(transform, false);
            root.SetPositionAndRotation(ground, Quaternion.LookRotation(facing.sqrMagnitude > 1e-4f ? facing.normalized : Vector3.forward));
            AddPart(root, "Manche", m_PoleMesh, m_Settings.woodMaterial);
            AddPart(root, "Corbeille", m_BasketMesh, m_Settings.ironMaterial);
            AddFire(root, new Vector3(0f, 1.72f, 0f), 0.4f);
            m_Placed.Add(ground);
            return root;
        }

        // Torche fixée à une paroi : une platine contre la roche, un bâton incliné vers l'extérieur, la flamme au bout.
        Transform CreateWallTorch(Vector3 position, Vector3 outward)
        {
            var root = new GameObject("Torche murale").transform;
            root.SetParent(transform, false);
            root.SetPositionAndRotation(position, Quaternion.LookRotation(outward));
            AddPart(root, "Platine", m_BracketMesh, m_Settings.ironMaterial);
            AddPart(root, "Bâton", m_StickMesh, m_Settings.woodMaterial);
            AddFire(root, new Vector3(0f, 0.42f, 0.32f), 0.32f);
            m_Placed.Add(position);
            return root;
        }

        static void AddPart(Transform root, string partName, Mesh mesh, Material material)
        {
            var go = new GameObject(partName);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        // La flamme (particules), quelques braises, et un petit cœur de feu qui brille (le bloom en fait un halo).
        void AddFire(Transform root, Vector3 local, float scale)
        {
            if (m_Settings.fireMaterial != null)
            {
                FireEffects.CreateBrazierFire(root, local, m_Settings.fireMaterial, scale).name = k_FlameName;
                FireEffects.CreateEmbers(root, local + Vector3.up * 0.2f, m_Settings.fireMaterial, 0.25f, 3f).name = k_EmbersName;
            }

            if (m_Settings.flameCoreMaterial != null)
            {
                var core = new GameObject(k_CoreName);
                core.transform.SetParent(root, false);
                core.transform.localPosition = local + Vector3.up * 0.05f;
                core.transform.localScale = Vector3.one * (0.42f * scale / 0.4f);
                core.AddComponent<MeshFilter>().sharedMesh = m_CoreMesh;
                var renderer = core.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = m_Settings.flameCoreMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        void AddLight(Transform root)
        {
            var core = root.Find(k_CoreName);
            var go = new GameObject("Lumière");
            go.transform.SetParent(root, false);
            go.transform.localPosition = core != null ? core.localPosition + Vector3.up * 0.25f : Vector3.up * 1.9f;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.color = m_Settings.torchColor;
            light.range = m_Settings.torchLightRange;
            light.intensity = m_Settings.torchLightIntensity;
        }

        void BuildMeshes()
        {
            var builder = new MeshBuilder();
            builder.Box(new Vector3(0f, 0.8f, 0f), new Vector3(0.045f, 0.8f, 0.045f), Quaternion.identity, Color.white);
            builder.Box(new Vector3(0f, 0.05f, 0f), new Vector3(0.12f, 0.05f, 0.12f), Quaternion.Euler(0f, 45f, 0f), Color.white);
            m_PoleMesh = builder.ToMesh("Torche (manche)");

            builder = new MeshBuilder();
            builder.Box(new Vector3(0f, 1.6f, 0f), new Vector3(0.11f, 0.025f, 0.11f), Quaternion.identity, Color.white);
            for (var i = 0; i < 4; i++)
            {
                var angle = i * 90f;
                var offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 0.11f);
                builder.Box(new Vector3(0f, 1.68f, 0f) + offset, new Vector3(0.11f, 0.07f, 0.015f), Quaternion.Euler(0f, angle, 0f), Color.white);
            }

            m_BasketMesh = builder.ToMesh("Torche (corbeille)");

            builder = new MeshBuilder();
            builder.Box(new Vector3(0f, 0f, 0.02f), new Vector3(0.09f, 0.16f, 0.02f), Quaternion.identity, Color.white);
            builder.Box(new Vector3(0f, 0.3f, 0.32f), new Vector3(0.08f, 0.02f, 0.08f), Quaternion.identity, Color.white);
            for (var i = 0; i < 4; i++)
            {
                var angle = i * 90f;
                var offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 0.08f);
                builder.Box(new Vector3(0f, 0.36f, 0.32f) + offset, new Vector3(0.08f, 0.05f, 0.012f), Quaternion.Euler(0f, angle, 0f), Color.white);
            }

            m_BracketMesh = builder.ToMesh("Torche murale (fer)");

            builder = new MeshBuilder();
            builder.Box(new Vector3(0f, 0.14f, 0.15f), new Vector3(0.035f, 0.035f, 0.2f), Quaternion.Euler(-40f, 0f, 0f), Color.white);
            m_StickMesh = builder.ToMesh("Torche murale (bâton)");

            m_CoreMesh = EffectBuild.SphereMesh();
        }

        // ---------------------------------------------------------------- en jeu

        /// <summary>Retrouve les torches posées sous cet objet (celles qui sont visibles) et commence à les animer.</summary>
        public void Bind(MapLightingSettings settings, MapScan scan)
        {
            m_Settings = settings;
            m_Scan = scan;
            m_Block = new MaterialPropertyBlock();

            for (var i = 0; i < transform.childCount; i++)
            {
                var root = transform.GetChild(i);
                if (!root.gameObject.activeInHierarchy)
                    continue;

                var torch = new Torch { Root = root, Seed = EffectBuild.Hash(root.position) * 100f };
                var flame = root.Find(k_FlameName);
                if (flame != null && flame.TryGetComponent(out torch.Flame))
                    torch.FlameRate = torch.Flame.emission.rateOverTimeMultiplier;

                var embers = root.Find(k_EmbersName);
                if (embers != null && embers.TryGetComponent<ParticleSystem>(out var embersSystem))
                    embersSystem.Play();

                torch.Light = root.GetComponentInChildren<Light>();
                if (torch.Light != null)
                {
                    torch.LightColor = torch.Light.color;
                    torch.LightIntensity = torch.Light.intensity;
                    torch.LightRange = torch.Light.range;
                }

                var core = root.Find(k_CoreName);
                if (core != null && core.TryGetComponent(out torch.Core))
                    torch.CoreScale = core.localScale;

                // De quelle voie est-elle (la plus proche, à moins de 9 m) ?
                var best = k_LaneReach;
                for (var lane = 0; lane < scan.Lanes.Count; lane++)
                {
                    var distance = scan.Lanes[lane].DistanceTo(root.position, out var along);
                    if (distance >= best)
                        continue;
                    best = distance;
                    torch.Lane = lane;
                    torch.Along = along;
                }

                m_Torches.Add(torch);
            }

            m_Waves = WaveManager.Instance;
            if (m_Waves != null)
            {
                m_Waves.WaveStarted += OnWaveStarted;
                m_Waves.WaveEnded += OnWaveEnded;
            }

            EnemySpawner.Spawned += OnEnemySpawned;

            // Avant la première vague, les torches des voies sont éteintes ; pendant une vague, allumées.
            var fighting = m_Waves == null || m_Waves.Phase == WavePhase.Wave;
            foreach (var torch in m_Torches)
            {
                torch.Target = torch.Lane < 0 || fighting ? 1f : 0f;
                torch.Level = torch.Target;
            }

            m_Bound = true;
        }

        void OnDestroy()
        {
            if (!m_Bound)
                return;

            if (m_Waves != null)
            {
                m_Waves.WaveStarted -= OnWaveStarted;
                m_Waves.WaveEnded -= OnWaveEnded;
            }

            EnemySpawner.Spawned -= OnEnemySpawned;
        }

        // Début de vague : chaque voie s'allume en chaîne, de l'apparition des ennemis jusqu'à la tour.
        void OnWaveStarted(int wave)
        {
            var now = Time.time;
            foreach (var torch in m_Torches)
            {
                if (torch.Lane < 0)
                    continue;
                var length = Mathf.Max(1f, m_Scan.Lanes[torch.Lane].Length);
                torch.IgniteAt = now + torch.Along / length * m_Settings.igniteDuration;
            }
        }

        // Fin de vague : elles couvent jusqu'à la suivante.
        void OnWaveEnded(int wave)
        {
            foreach (var torch in m_Torches)
            {
                if (torch.Lane < 0)
                    continue;
                torch.IgniteAt = -1f;
                torch.Target = 0.3f;
            }
        }

        // Un ennemi apparaît : une vague rouge part de son point d'apparition, sur sa voie.
        void OnEnemySpawned(Enemy enemy, Transform point)
        {
            for (var i = 0; i < m_Scan.Lanes.Count; i++)
            {
                if (m_Scan.Lanes[i].Spawn != point)
                    continue;

                // Plusieurs ennemis d'affilée : une seule vague.
                foreach (var pulse in m_Pulses)
                {
                    if (pulse.Lane == i && Time.time - pulse.Start < 1.2f)
                        return;
                }

                m_Pulses.Add(new Pulse { Lane = i, Start = Time.time });
                return;
            }
        }

        void Update()
        {
            if (!m_Bound)
                return;

            var now = Time.time;
            var deltaTime = Time.deltaTime;
            var lamps = SkyController.Lamps;
            m_Pulses.RemoveAll(pulse => pulse.Lane >= m_Scan.Lanes.Count || (now - pulse.Start) * m_Settings.alarmSpeed > m_Scan.Lanes[pulse.Lane].Length + 15f);

            foreach (var torch in m_Torches)
            {
                if (torch.Root == null)
                    continue;

                if (torch.IgniteAt >= 0f && now >= torch.IgniteAt)
                {
                    // Elle s'embrase d'un coup, puis se calme.
                    torch.IgniteAt = -1f;
                    torch.Target = 1f;
                    torch.Flare = 1f;
                }

                torch.Level = Mathf.MoveTowards(torch.Level, torch.Target, deltaTime * (torch.Target > torch.Level ? 4f : 0.6f));
                torch.Flare = Mathf.MoveTowards(torch.Flare, 0f, deltaTime * 1.5f);
                torch.Alarm = Mathf.MoveTowards(torch.Alarm, AlarmAt(torch, now), deltaTime * 6f);
                Apply(torch, lamps, now);
            }
        }

        // La vague rouge : une bosse qui avance le long de la voie.
        float AlarmAt(Torch torch, float now)
        {
            if (torch.Lane < 0)
                return 0f;

            var alarm = 0f;
            foreach (var pulse in m_Pulses)
            {
                if (pulse.Lane != torch.Lane)
                    continue;
                var front = (now - pulse.Start) * m_Settings.alarmSpeed;
                var gap = (torch.Along - front) / 7f;
                alarm = Mathf.Max(alarm, Mathf.Exp(-gap * gap));
            }

            return alarm;
        }

        void Apply(Torch torch, float lamps, float now)
        {
            var level = torch.Level;
            var burning = level > 0.02f;
            if (torch.Flame != null)
            {
                var emission = torch.Flame.emission;
                emission.rateOverTimeMultiplier = torch.FlameRate * level * (1f + torch.Flare);
                if (burning && !torch.Flame.isPlaying)
                    torch.Flame.Play();
                else if (!burning && torch.Flame.isPlaying)
                    torch.Flame.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            var flicker = 0.85f + 0.3f * Mathf.PerlinNoise(torch.Seed, now * 5f);
            if (torch.Light != null)
            {
                torch.Light.enabled = burning;
                torch.Light.color = Color.Lerp(torch.LightColor, m_Settings.alarmColor, torch.Alarm);
                torch.Light.intensity = torch.LightIntensity * Mathf.Lerp(0.2f, 1f, lamps) * level * flicker * (1f + torch.Flare * 1.5f + torch.Alarm);
                torch.Light.range = torch.LightRange * (1f + torch.Flare * 0.5f);
            }

            if (torch.Core != null)
            {
                torch.Core.enabled = burning;
                torch.Core.transform.localScale = torch.CoreScale * (0.6f + 0.4f * level + 0.5f * torch.Flare);
                m_Block.Clear();
                m_Block.SetFloat(k_GlowId, (1.8f + 1.5f * torch.Flare) * Mathf.Lerp(0.7f, 1.2f, lamps));
                m_Block.SetColor(k_HotColorId, Color.Lerp(new Color(1.6f, 1.25f, 0.6f), new Color(2f, 0.35f, 0.25f), torch.Alarm));
                m_Block.SetColor(k_MidColorId, Color.Lerp(new Color(1.4f, 0.45f, 0.06f), new Color(1.6f, 0.06f, 0.03f), torch.Alarm));
                torch.Core.SetPropertyBlock(m_Block);
            }
        }
    }
}
