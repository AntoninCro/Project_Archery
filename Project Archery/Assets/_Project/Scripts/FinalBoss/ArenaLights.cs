using System.Collections.Generic;
using Archery.Combat;
using Archery.Player;
using Archery.World;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Archery.FinalBoss
{
    /// <summary>
    /// L'objet « Effet » de l'arène du dragon : toutes ses petites lumières, rangées par groupe et enregistrées avec la
    /// scène (on peut les déplacer, les modifier, les supprimer ; cacher « Effet » ou un groupe les éteint) :
    /// <list type="bullet">
    /// <item>« Veines » : des veines de lave dans les parois et la voûte, qui battent sur la musique ; il y en a plus à
    /// chaque phase (dans l'ordre des objets du groupe) ;</item>
    /// <item>« Braises de la grotte » : des braises qui montent dans toute la grotte, plus nombreuses à chaque phase ;</item>
    /// <item>« Coulées » : des gerbes d'étincelles, un halo et une lueur au pied des coulées de lave ;</item>
    /// <item>« Soleil » : les rayons de soleil qui percent la voûte à la victoire (cachés jusque-là) ;</item>
    /// <item>« Piliers » (<see cref="PillarLights"/>) : lumière d'en bas, couronne de cristaux, abri au sol ;</item>
    /// <item>en jeu seulement, sous « En jeu » : bulles et vapeur de la lave, sol brûlant, sol qui chauffe avant le
    /// souffle, onde de lumière aux changements de phase, runes sous les tourelles, lueur du joueur.</item>
    /// </list>
    /// En jeu aussi : flèches traçantes, craquelures qui flashent avant le slam, braseros et lave qui s'embrasent à
    /// l'envol et à la rage ; à la victoire, la lave s'éteint et les cristaux blanchissent.
    /// </summary>
    /// <remarks>
    /// L'objet est créé par l'éditeur à l'ouverture de la scène s'il n'existe pas (menu Archery > Effets pour le
    /// refaire) ; sans lui, tout est fabriqué au lancement par l'<see cref="ArenaDirector"/>. Réglages :
    /// « Resources/MapLighting », partie « Arène du boss final ».
    /// </remarks>
    [DisallowMultipleComponent]
    public class ArenaLights : MonoBehaviour
    {
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_ArenaId = Shader.PropertyToID("_ArcheryArena");
        static readonly int k_FallCutoffId = Shader.PropertyToID("_LavaFallCutoffY");

        const string k_Veins = "Veines";
        const string k_Embers = "Braises de la grotte";
        const string k_Feet = "Coulées";
        const string k_Sun = "Soleil";
        const string k_Pillars = "Piliers";
        const string k_Runtime = "En jeu";

        const int k_ScorchCount = 24;
        const int k_BubbleCount = 6;
        const int k_FlashCount = 3;
        const int k_WaveSegments = 72;

        class Scorch
        {
            public MeshRenderer Renderer;
            public float Age = 999f;
            public float Life = 1f;
            public float Strength;
        }

        class Bubble
        {
            public Transform Transform;
            public Vector3 Point;
            public float Age;
            public float Life;
            public float Size;
            public bool Active;
        }

        class Flash
        {
            public Light Light;
            public float Age = 999f;
            public float Peak;
        }

        class Foot
        {
            public Vector3 Position;
            public ParticleSystem Sparks;
            public float SparkRate;
            public MeshRenderer Halo;
            public Light Light;
            public float LightIntensity;
        }

        class Rune
        {
            public MeshRenderer Renderer;
            public float Age;
        }

        class Sunbeam
        {
            public MeshRenderer Beam;
            public MeshRenderer Spot;
            public Light Light;
            public float LightIntensity;
            public ParticleSystem Dust;
            public float DustRate;
        }

        readonly List<MeshRenderer> m_Veins = new List<MeshRenderer>();
        readonly List<Scorch> m_Scorches = new List<Scorch>();
        readonly List<Bubble> m_Bubbles = new List<Bubble>();
        readonly List<Flash> m_Flashes = new List<Flash>();
        readonly List<Foot> m_Feet = new List<Foot>();
        readonly List<Rune> m_Runes = new List<Rune>();
        readonly List<Sunbeam> m_Sunbeams = new List<Sunbeam>();
        readonly List<Vector3> m_LavaPoints = new List<Vector3>();
        readonly List<Object> m_Owned = new List<Object>();

        MapLightingSettings m_Settings;
        ArenaDirector m_Director;
        ArenaShape m_Arena;
        ArenaGridBuilder m_Grid;
        DragonBoss m_Dragon;
        Health m_DragonHealth;
        CeilingCrystals m_Crystals;
        ArenaTurrets m_Turrets;
        MaterialPropertyBlock m_Block;
        Transform m_RuntimeGroup;
        Material m_Soft;
        Mesh[] m_Blobs;
        Mesh m_RuneMesh;
        Material m_RuneMaterial;
        float m_CeilingHeight = 15f;
        bool m_Bound;

        ParticleSystem m_Embers;
        ParticleSystem m_Vapour;
        float m_BubbleTimer;
        float m_VapourBudget;

        MeshRenderer m_Warning;
        float m_WarningLevel;
        float m_BreathScorchTimer;

        MeshRenderer m_Wave;
        Mesh m_WaveMesh;
        Vector3[] m_WaveVertices;
        Vector3 m_WaveOrigin;
        float m_WaveAge = 999f;

        float m_CrackFlash;
        float m_DecorFlash;
        float m_LavaFlash;
        float m_AppliedDecor = -1f;
        float m_ShownVeins;

        Light m_PlayerLight;
        float m_VictoryTime = -1f;
        float m_AppliedCool = -1f;

        static MapLightingSettings LoadSettings() => Resources.Load<MapLightingSettings>(MapLightingSettings.ResourceName);

        /// <summary>L'objet « Effet » de cette scène, même caché ; null s'il n'y en a pas.</summary>
        public static ArenaLights FindIn(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent<ArenaLights>(out var lights))
                    return lights;
            }

            return null;
        }

        /// <summary>
        /// Au lancement (par l'<see cref="ArenaDirector"/>) : l'objet « Effet » de la scène de l'arène s'il existe (caché :
        /// rien ne s'allume), sinon un neuf, fabriqué tout de suite ; puis on commence à animer ses lumières.
        /// </summary>
        public static ArenaLights Attach(ArenaDirector director, ArenaShape arena, DragonBoss dragon, CeilingCrystals crystals, ArenaTurrets turrets)
        {
            var settings = LoadSettings();
            if (settings == null || director == null || arena == null)
                return null;

            var scene = director.gameObject.scene;
            var lights = FindIn(scene);
            if (lights == null)
            {
                lights = CreateRoot(scene);
                lights.Build(settings, arena);
            }

            if (lights.isActiveAndEnabled)
                lights.Bind(settings, director, arena, dragon, crystals, turrets);
            return lights;
        }

        /// <summary>
        /// Éditeur : crée l'objet « Effet » dans cette scène et y fabrique les lumières de l'arène (elles seront enregistrées
        /// avec la scène). Null sans arène ou sans réglages.
        /// </summary>
        public static ArenaLights Bake(Scene scene)
        {
            var settings = LoadSettings();
            ArenaShape arena = null;
            foreach (var candidate in FindObjectsByType<ArenaShape>())
            {
                if (candidate.gameObject.scene == scene && candidate.isActiveAndEnabled)
                    arena = candidate;
            }

            if (settings == null || arena == null)
                return null;

            var lights = CreateRoot(scene);
            lights.Build(settings, arena);
            return lights;
        }

        static ArenaLights CreateRoot(Scene scene)
        {
            var lights = new GameObject(MapLighting.RootName).AddComponent<ArenaLights>();
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.MoveGameObjectToScene(lights.gameObject, scene);
            return lights;
        }

        // ---------------------------------------------------------------- fabrication (jeu ou éditeur)

        void Build(MapLightingSettings settings, ArenaShape arena)
        {
            m_Settings = settings;
            m_Arena = arena;
            m_Grid = arena as ArenaGridBuilder;
            m_CeilingHeight = Mathf.Max(4f, m_Arena.CeilingPoint(m_Arena.Center, out _).y - m_Arena.FloorY);
            m_Blobs = new[] { BuildBlob(3, 0.3f), BuildBlob(7, 0.4f), BuildBlob(11, 0.25f) };

            // Les colliders de l'arène viennent peut-être d'être fabriqués : la physique doit les connaître avant les rayons.
            Physics.SyncTransforms();
            BuildVeins(EffectBuild.Group(transform, k_Veins));
            BuildEmbers();
            BuildFallFeet(EffectBuild.Group(transform, k_Feet));
            BuildSunbeams(EffectBuild.Group(transform, k_Sun));
            EffectBuild.Group(transform, k_Pillars).gameObject.AddComponent<PillarLights>().Build(settings, arena);
        }

        static Material Tinted(Material source, float brightness, string label)
        {
            var material = new Material(source) { name = $"{source.name} (arène, {label})" };
            if (material.HasProperty(k_BaseColorId))
                material.SetColor(k_BaseColorId, new Color(brightness, brightness, brightness, 1f));
            return material;
        }

        static MeshRenderer CreateRenderer(string objectName, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        static Light CreateLight(string objectName, Transform parent, Color color, float range, float intensity)
        {
            var light = new GameObject(objectName).AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            return light;
        }

        // Des veines qui serpentent sur la roche, parties de points pris sur les parois (rayons horizontaux depuis le
        // centre) et sur la voûte (rayons vers le haut). Rangées au hasard : à chaque phase, d'autres s'allument.
        void BuildVeins(Transform group)
        {
            if (m_Settings.glowMaterial == null)
                return;

            var random = new System.Random(97);
            var center = m_Arena.Center;
            var starts = new List<(Vector3 Point, Vector3 Normal, bool Ceiling)>();
            for (var attempt = 0; attempt < m_Settings.arenaWallVeins * 8 && starts.Count < m_Settings.arenaWallVeins; attempt++)
            {
                var origin = center + Vector3.up * Range(random, 1.2f, Mathf.Max(3f, m_CeilingHeight * 0.8f));
                var direction = Quaternion.Euler(0f, Range(random, 0f, 360f), 0f) * Vector3.forward;
                if (!TrySurface(origin, direction, m_Arena.RoamRadius + 40f, out var point, out var normal))
                    continue;
                if (Mathf.Abs(normal.y) > 0.5f || (point - origin).sqrMagnitude < 16f || TooClose(starts, point, 3f))
                    continue;
                starts.Add((point, normal, false));
            }

            var ceilingStart = starts.Count;
            for (var attempt = 0; attempt < m_Settings.arenaCeilingVeins * 8 && starts.Count - ceilingStart < m_Settings.arenaCeilingVeins; attempt++)
            {
                var offset = Random2(random) * (m_Arena.CeilingRadius * 0.85f);
                var origin = center + new Vector3(offset.x, 1.5f, offset.y);
                if (!TrySurface(origin, Vector3.up, m_CeilingHeight + 25f, out var point, out var normal))
                    continue;
                if (normal.y > -0.4f || TooClose(starts, point, 4f))
                    continue;
                starts.Add((point, normal, true));
            }

            // Au hasard, parois et voûte mêlées : c'est l'ordre dans lequel elles s'allument.
            for (var i = starts.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (starts[i], starts[j]) = (starts[j], starts[i]);
            }

            var material = EffectBuild.GlowMaterial(m_Settings.glowMaterial, m_Settings.arenaVeinColor, 1.4f, "Veines de l'arène");
            foreach (var start in starts)
            {
                var builder = new MeshBuilder();
                var steps = (start.Ceiling ? 9 : 8) + random.Next(6);
                var width = Range(random, 0.14f, 0.22f);
                if (Grow(builder, start.Point, start.Normal, steps, width, random) < 1.5f)
                    continue;
                if (random.NextDouble() < 0.5)
                    Grow(builder, start.Point, start.Normal, 3 + random.Next(4), width * 0.6f, random);

                CreateRenderer(start.Ceiling ? "Veine de la voûte" : "Veine de la paroi", group, builder.ToMesh("Veine de l'arène"), material);
            }
        }

        // Une veine qui suit la surface pas à pas, en tournant un peu à chaque pas ; plus fine au bout. Rend sa longueur.
        float Grow(MeshBuilder builder, Vector3 point, Vector3 normal, int steps, float width, System.Random random)
        {
            var direction = Vector3.ProjectOnPlane(new Vector3(Range(random, -1f, 1f), Range(random, -1f, 1f), Range(random, -1f, 1f)), normal);
            if (direction.sqrMagnitude < 1e-4f)
                direction = Vector3.ProjectOnPlane(Vector3.right, normal);
            direction.Normalize();

            var points = new List<Vector3> { point };
            var normals = new List<Vector3> { normal };
            var length = 0f;
            for (var s = 0; s < steps; s++)
            {
                direction = Vector3.ProjectOnPlane(Quaternion.AngleAxis(Range(random, -35f, 35f), normal) * direction, normal);
                if (direction.sqrMagnitude < 1e-4f)
                    break;
                direction.Normalize();

                var candidate = point + direction * Range(random, 0.5f, 0.95f);
                if (!TrySurface(candidate + normal * 1f, -normal, 2.2f, out var next, out var nextNormal) || Vector3.Dot(nextNormal, normal) < 0.3f)
                    break;

                length += Vector3.Distance(point, next);
                point = next;
                normal = nextNormal;
                points.Add(point);
                normals.Add(normal);
            }

            if (points.Count < 2)
                return 0f;

            var sides = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                var along = points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)];
                sides[i] = Vector3.Cross(along, normals[i]).normalized * (width * Mathf.Lerp(1f, 0.25f, i / (float)(points.Count - 1)));
            }

            for (var i = 0; i < points.Count - 1; i++)
            {
                var a = points[i] + normals[i] * 0.05f;
                var b = points[i + 1] + normals[i + 1] * 0.05f;
                var color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.4f, i / (float)(points.Count - 1)));
                builder.Quad(a - sides[i], a + sides[i], b + sides[i + 1], b - sides[i + 1], color,
                             new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            }

            return length;
        }

        // La surface de l'arène devant ce rayon (pas le dragon, ni une tourelle, ni le joueur).
        bool TrySurface(Vector3 origin, Vector3 direction, float distance, out Vector3 point, out Vector3 normal)
        {
            point = origin;
            normal = -direction;
            var best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(origin, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance >= best || !hit.collider.transform.IsChildOf(m_Arena.transform))
                    continue;
                best = hit.distance;
                point = hit.point;
                normal = hit.normal;
            }

            return best < float.MaxValue;
        }

        static bool TooClose(List<(Vector3 Point, Vector3 Normal, bool Ceiling)> starts, Vector3 point, float spacing)
        {
            foreach (var start in starts)
            {
                if ((start.Point - point).sqrMagnitude < spacing * spacing)
                    return true;
            }

            return false;
        }

        // Des braises dans tout le volume de la grotte.
        void BuildEmbers()
        {
            if (m_Settings.fireMaterial == null)
                return;

            var size = m_Arena.RoamRadius * 2f + 6f;
            var embers = GlowParticles.Create(k_Embers, transform, m_Arena.Center + Vector3.up * (m_CeilingHeight * 0.45f),
                                              Tinted(m_Settings.fireMaterial, 3f, "braises"));
            var main = embers.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(7f, 11f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = m_Settings.emberColor;
            main.maxParticles = 500;
            var emission = embers.emission;
            emission.rateOverTime = 20f;
            var shape = embers.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size, m_CeilingHeight * 0.9f, size);
            var velocity = embers.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = embers.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.2f;
            noise.scrollSpeed = 0.15f;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.5f, 0.4f),
                    new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f),
                });
            var color = embers.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        // Au pied de chaque coulée : une gerbe d'étincelles, un halo sur la lave, et une lueur pour deux pieds (en VR, les
        // lumières coûtent cher).
        void BuildFallFeet(Transform group)
        {
            if (m_Grid == null || m_Settings.fireMaterial == null || m_Settings.glowMaterial == null)
                return;

            var poses = new List<Pose>();
            m_Grid.GetLavaFallFeet(poses);
            var cell = m_Grid.CellSize;
            var sparksMaterial = Tinted(m_Settings.fireMaterial, 3f, "étincelles");
            var haloMaterial = EffectBuild.GlowMaterial(m_Settings.glowMaterial, new Color(1f, 0.45f, 0.1f), 1.5f, "Halo des coulées");
            for (var i = 0; i < poses.Count; i++)
            {
                var pose = poses[i];
                var root = new GameObject("Pied de coulée").transform;
                root.SetParent(group, false);
                root.SetPositionAndRotation(pose.position, pose.rotation);

                var sparks = GlowParticles.Create("Étincelles", root, pose.position + Vector3.up * 0.15f, sparksMaterial);
                sparks.transform.rotation = Quaternion.LookRotation(Vector3.up * 0.8f + pose.forward * 0.6f);
                var main = sparks.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                main.startColor = m_Settings.emberColor;
                main.gravityModifier = 0.9f;
                main.maxParticles = 60;
                var emission = sparks.emission;
                emission.rateOverTime = 16f;
                var shape = sparks.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 35f;
                shape.radius = cell * 0.4f;

                var halo = CreateRenderer("Halo", root, m_Blobs[i % m_Blobs.Length], haloMaterial);
                halo.transform.SetPositionAndRotation(pose.position + Vector3.up * 0.05f + pose.forward * (cell * 0.3f), pose.rotation);
                halo.transform.localScale = new Vector3(cell * 0.9f, 1f, cell * 0.9f);

                if (i % 2 == 0)
                {
                    var light = CreateLight("Lueur", root, new Color(1f, 0.45f, 0.12f), 7f, 2f);
                    light.transform.position = pose.position + Vector3.up * 1.2f + pose.forward * 1f;
                }
            }
        }

        // Les rayons de soleil de la victoire : du centre et de trois points autour, de la voûte au sol. Dans l'éditeur, ils
        // sont visibles (pour les placer) ; en jeu, cachés jusqu'à la mort du dragon.
        void BuildSunbeams(Transform group)
        {
            if (m_Settings.beamMaterial == null || m_Settings.glowMaterial == null)
                return;

            var center = m_Arena.Center;
            var spots = new List<Vector3> { center };
            for (var i = 0; i < 3; i++)
            {
                var angle = (30f + i * 120f) * Mathf.Deg2Rad;
                spots.Add(center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (m_Arena.RoamRadius * 0.45f));
            }

            var spotMaterial = EffectBuild.GlowMaterial(m_Settings.glowMaterial, m_Settings.sunColor, 1.3f, "Tache de soleil");
            var dustMaterial = Tinted(m_Settings.fireMaterial != null ? m_Settings.fireMaterial : m_Settings.beamMaterial, 1f, "poussière");
            var index = 0;
            foreach (var spot in spots)
            {
                var ceiling = m_Arena.CeilingPoint(spot, out _);
                var floor = new Vector3(ceiling.x, m_Arena.GroundHeightAt(ceiling), ceiling.z);
                var height = ceiling.y - floor.y;
                if (height < 4f)
                    continue;

                // L'objet du rayon est au sol ; le faisceau, la tache, la lumière et la poussière le suivent.
                var root = new GameObject("Rayon de soleil").transform;
                root.SetParent(group, false);
                root.position = floor;

                // Retourné : le rayon s'efface vers le sol, pas vers la voûte.
                var beam = EffectBuild.Cylinder("Faisceau", root);
                beam.transform.SetPositionAndRotation(floor + Vector3.up * (height * 0.5f), Quaternion.Euler(180f, 0f, 0f));
                beam.transform.localScale = new Vector3(2.4f, height * 0.5f, 2.4f);
                var beamRenderer = beam.GetComponent<MeshRenderer>();
                beamRenderer.sharedMaterial = m_Settings.beamMaterial;
                beamRenderer.shadowCastingMode = ShadowCastingMode.Off;
                beamRenderer.receiveShadows = false;

                var spotRenderer = CreateRenderer("Tache", root, m_Blobs[index++ % m_Blobs.Length], spotMaterial);
                spotRenderer.transform.position = floor + Vector3.up * 0.04f;
                spotRenderer.transform.localScale = new Vector3(1.9f, 1f, 1.9f);

                var light = new GameObject("Soleil").AddComponent<Light>();
                light.transform.SetParent(root, false);
                light.transform.SetPositionAndRotation(ceiling - Vector3.up * 0.3f, Quaternion.LookRotation(Vector3.down));
                light.type = LightType.Spot;
                light.spotAngle = 35f;
                light.range = height + 4f;
                light.color = m_Settings.sunColor;
                light.shadows = LightShadows.None;
                light.intensity = 5f;

                var dust = GlowParticles.Create("Poussière", root, floor + Vector3.up * (height * 0.5f), dustMaterial);
                var main = dust.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
                main.startColor = m_Settings.sunColor;
                main.maxParticles = 150;
                var emission = dust.emission;
                emission.rateOverTime = 20f;
                var shape = dust.shape;
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(1.8f, height, 1.8f);
                var noise = dust.noise;
                noise.enabled = true;
                noise.strength = 0.15f;
                noise.frequency = 0.3f;
            }
        }

        // ---------------------------------------------------------------- en jeu : retrouver et compléter

        void Bind(MapLightingSettings settings, ArenaDirector director, ArenaShape arena, DragonBoss dragon, CeilingCrystals crystals, ArenaTurrets turrets)
        {
            m_Settings = settings;
            m_Director = director;
            m_Arena = arena;
            m_Grid = arena as ArenaGridBuilder;
            m_Dragon = dragon;
            m_Crystals = crystals;
            m_Turrets = turrets;
            m_Block = new MaterialPropertyBlock();
            m_CeilingHeight = Mathf.Max(4f, m_Arena.CeilingPoint(m_Arena.Center, out _).y - m_Arena.FloorY);
            m_Blobs ??= new[] { Own(BuildBlob(3, 0.3f)), Own(BuildBlob(7, 0.4f)), Own(BuildBlob(11, 0.25f)) };
            if (settings.fireMaterial != null)
                m_Soft = Own(Tinted(settings.fireMaterial, 1f, "vapeur"));

            if (Find(transform, k_Veins) is { } veins)
                m_Veins.AddRange(veins.GetComponentsInChildren<MeshRenderer>());
            foreach (var vein in m_Veins)
                vein.enabled = false;

            if (Find(transform, k_Embers) is { } embers && embers.TryGetComponent(out m_Embers))
                m_Embers.Play();

            if (Find(transform, k_Feet) is { } feet)
            {
                for (var i = 0; i < feet.childCount; i++)
                {
                    var root = feet.GetChild(i);
                    if (!root.gameObject.activeInHierarchy)
                        continue;

                    var foot = new Foot { Position = root.position };
                    if (Find(root, "Étincelles") is { } sparks && sparks.TryGetComponent(out foot.Sparks))
                    {
                        foot.SparkRate = foot.Sparks.emission.rateOverTimeMultiplier;
                        foot.Sparks.Play();
                    }

                    if (Find(root, "Halo") is { } halo)
                        halo.TryGetComponent(out foot.Halo);
                    foot.Light = root.GetComponentInChildren<Light>();
                    if (foot.Light != null)
                        foot.LightIntensity = foot.Light.intensity;
                    m_Feet.Add(foot);
                }
            }

            if (Find(transform, k_Sun) is { } sun)
            {
                for (var i = 0; i < sun.childCount; i++)
                {
                    var root = sun.GetChild(i);
                    if (!root.gameObject.activeInHierarchy)
                        continue;

                    var sunbeam = new Sunbeam();
                    if (Find(root, "Faisceau") is { } beam)
                        beam.TryGetComponent(out sunbeam.Beam);
                    if (Find(root, "Tache") is { } spot)
                        spot.TryGetComponent(out sunbeam.Spot);
                    sunbeam.Light = root.GetComponentInChildren<Light>();
                    if (sunbeam.Light != null)
                        sunbeam.LightIntensity = sunbeam.Light.intensity;
                    if (Find(root, "Poussière") is { } dust && dust.TryGetComponent(out sunbeam.Dust))
                    {
                        sunbeam.DustRate = sunbeam.Dust.emission.rateOverTimeMultiplier;
                        sunbeam.Dust.Play();
                    }

                    m_Sunbeams.Add(sunbeam);
                }
            }

            if (Find(transform, k_Pillars) is { } pillars && pillars.TryGetComponent<PillarLights>(out var pillarLights))
                pillarLights.Bind(settings, director, arena, dragon, crystals);

            // Ce qui n'existe qu'en jeu, rangé à part.
            m_RuntimeGroup = EffectBuild.Group(transform, k_Runtime);
            Physics.SyncTransforms();
            CreateLava();
            CreateScorches();
            CreateWarning();
            CreateWave();
            m_PlayerLight = CreateLight("Lueur du joueur", m_RuntimeGroup, m_Settings.playerLightColor, 6f, 0f);
            m_PlayerLight.enabled = false;

            Shader.SetGlobalFloat(k_ArenaId, 1f);
            if (m_Dragon != null)
            {
                m_Dragon.PhaseChanged += OnPhaseChanged;
                m_Dragon.Slammed += OnSlammed;
                if (m_Dragon.TryGetComponent(out m_DragonHealth))
                    m_DragonHealth.Died += OnDragonDied;
            }

            DragonFireball.Exploded += OnFireballExploded;
            if (m_Turrets != null)
                m_Turrets.TurretLanded += OnTurretLanded;
            m_Bound = true;
            UpdateVictory(0f, 0f);
        }

        // Un enfant visible de ce nom (null sinon).
        static Transform Find(Transform parent, string name)
        {
            var child = parent.Find(name);
            return child != null && child.gameObject.activeInHierarchy ? child : null;
        }

        T Own<T>(T owned) where T : Object
        {
            m_Owned.Add(owned);
            return owned;
        }

        void OnEnable()
        {
            if (m_Bound)
                Shader.SetGlobalFloat(k_ArenaId, 1f);
        }

        // Caché en jeu : plus de traceurs, ni d'embrasement des craquelures, des braseros ou de la lave.
        void OnDisable()
        {
            if (!m_Bound)
                return;

            Shader.SetGlobalFloat(k_ArenaId, 0f);
            if (m_Grid != null)
            {
                m_Grid.CrackBoost = 0f;
                m_Grid.SetDecorBoost(0f);
            }

            m_AppliedDecor = -1f;
            if (m_Director != null)
                m_Director.LavaBoost = 0f;
        }

        void OnDestroy()
        {
            if (!m_Bound)
                return;

            DragonFireball.Exploded -= OnFireballExploded;
            if (m_Dragon != null)
            {
                m_Dragon.PhaseChanged -= OnPhaseChanged;
                m_Dragon.Slammed -= OnSlammed;
            }

            if (m_DragonHealth != null)
                m_DragonHealth.Died -= OnDragonDied;
            if (m_Turrets != null)
                m_Turrets.TurretLanded -= OnTurretLanded;

            foreach (var owned in m_Owned)
            {
                if (owned != null)
                    Destroy(owned);
            }
        }

        void Update()
        {
            if (!m_Bound)
                return;

            var deltaTime = Time.deltaTime;
            var level = m_Director != null ? m_Director.LightLevel : 1f;
            var victory = m_VictoryTime >= 0f;
            var sinceVictory = victory ? Time.time - m_VictoryTime : 0f;
            var cool = victory ? Smooth01(sinceVictory / 5f) : 0f;
            var phase = m_Dragon != null ? m_Dragon.Phase : DragonPhase.Ground;
            var heat = phase == DragonPhase.Enraged ? 1f : phase == DragonPhase.Air ? 0.55f : 0.2f;
            var beat = BeatPhase(out var downbeat);
            var pulse = Mathf.Exp(-beat * 5f) * (downbeat ? 1.5f : 1f);

            m_CrackFlash *= Mathf.Exp(-deltaTime * 2.2f);
            m_DecorFlash *= Mathf.Exp(-deltaTime * 1.5f);
            m_LavaFlash *= Mathf.Exp(-deltaTime * 1.2f);

            UpdateVeins(level, cool, phase, pulse, deltaTime);
            UpdateEmbers(level, cool, heat);
            UpdateLava(level, cool, heat, deltaTime);
            UpdateFeet(level, cool);
            UpdateFlashes(deltaTime);
            UpdateScorches(deltaTime);
            UpdateBreath(deltaTime);
            UpdateWave(deltaTime);
            UpdateRunes(level, deltaTime);
            UpdatePlayerLight(level);
            if (victory)
                UpdateVictory(sinceVictory, cool);
            ApplyBoosts(cool);
        }

        // Position dans le temps de la musique (0 sur le temps, vers 1 juste avant le suivant) ; temps fort tous les 4.
        float BeatPhase(out bool downbeat)
        {
            var beat = m_Director != null ? m_Director.Beat : -1.0;
            if (beat < 0.0)
                beat = Time.time * (m_Director != null ? m_Director.Bpm : 140f) / 60.0;
            var whole = System.Math.Floor(beat);
            downbeat = (long)whole % 4 == 0;
            return (float)(beat - whole);
        }

        // Les craquelures, les braseros et la lave : leurs embrasements passagers, et la lave qui s'éteint à la victoire.
        void ApplyBoosts(float cool)
        {
            var slam = m_Dragon != null ? m_Dragon.SlamProgress : -1f;
            var slamGlow = slam >= 0f ? 3f * slam * slam * (0.8f + 0.2f * Mathf.Sin(Time.time * 40f)) : 0f;
            if (m_Grid != null)
            {
                m_Grid.CrackBoost = m_CrackFlash + slamGlow;
                if (Mathf.Abs(m_DecorFlash - m_AppliedDecor) > 0.02f || (m_DecorFlash < 0.02f && m_AppliedDecor > 0f))
                {
                    m_AppliedDecor = m_DecorFlash < 0.02f ? 0f : m_DecorFlash;
                    m_Grid.SetDecorBoost(m_AppliedDecor);
                }
            }

            if (m_Director != null)
                m_Director.LavaBoost = m_LavaFlash - 0.8f * cool;
        }

        void SetGlow(Renderer renderer, Color color, float intensity)
        {
            m_Block.Clear();
            m_Block.SetColor(k_ColorId, color);
            m_Block.SetFloat(k_IntensityId, intensity);
            renderer.SetPropertyBlock(m_Block);
        }

        // ---------------------------------------------------------------- veines et braises

        // Une part des veines par phase (au sol, en vol, enragé) ; elles battent sur chaque temps, plus fort sur le temps fort.
        void UpdateVeins(float level, float cool, DragonPhase phase, float pulse, float deltaTime)
        {
            if (m_Veins.Count == 0)
                return;

            var share = phase == DragonPhase.Enraged || phase == DragonPhase.Dead ? 1f : phase == DragonPhase.Air ? 0.75f : 0.45f;
            m_ShownVeins = Mathf.MoveTowards(m_ShownVeins, share * m_Veins.Count, deltaTime * 3f);
            var enraged = phase == DragonPhase.Enraged ? 0.5f : 0f;
            var intensity = Mathf.Lerp(0.9f, 1.8f, share) * level * (1f - cool) * (0.55f + 0.9f * pulse);
            for (var i = 0; i < m_Veins.Count; i++)
            {
                var vein = m_Veins[i];
                if (vein == null)
                    continue;

                var fade = Mathf.Clamp01(m_ShownVeins - i);
                vein.enabled = fade > 0.01f && intensity > 0.01f;
                if (vein.enabled)
                    SetGlow(vein, Color.Lerp(EffectBuild.GlowColor(vein), new Color(1f, 0.55f, 0.15f), enraged), intensity * fade);
            }
        }

        void UpdateEmbers(float level, float cool, float heat)
        {
            if (m_Embers == null)
                return;

            var emission = m_Embers.emission;
            emission.rateOverTimeMultiplier = Mathf.Lerp(14f, 45f, heat) * level * (1f - cool);
        }

        // ---------------------------------------------------------------- lave : bulles et vapeur

        void CreateLava()
        {
            if (m_Grid == null)
                return;

            m_Grid.GetLavaSurface(m_LavaPoints);
            if (m_LavaPoints.Count == 0)
                return;

            var group = EffectBuild.Group(m_RuntimeGroup, "Lave");
            if (m_Settings.flameCoreMaterial != null)
            {
                var sphereMesh = EffectBuild.SphereMesh();
                for (var i = 0; i < k_BubbleCount; i++)
                {
                    var renderer = CreateRenderer("Bulle de lave", group, sphereMesh, m_Settings.flameCoreMaterial);
                    renderer.gameObject.SetActive(false);
                    m_Bubbles.Add(new Bubble { Transform = renderer.transform });
                }
            }

            for (var i = 0; i < k_FlashCount; i++)
            {
                var light = CreateLight("Éclat de bulle", group, new Color(1f, 0.5f, 0.15f), 5f, 0f);
                light.enabled = false;
                m_Flashes.Add(new Flash { Light = light });
            }

            if (m_Soft != null)
            {
                m_Vapour = GlowParticles.Create("Vapeur de la lave", group, m_Arena.Center, m_Soft);
                var main = m_Vapour.main;
                main.maxParticles = 120;
                var emission = m_Vapour.emission;
                emission.rateOverTime = 0f;
                var shape = m_Vapour.shape;
                shape.enabled = false;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
                var color = m_Vapour.colorOverLifetime;
                color.enabled = true;
                color.color = new ParticleSystem.MinMaxGradient(gradient);
                var size = m_Vapour.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
                m_Vapour.Play();
            }
        }

        Vector3 RandomLavaPoint()
        {
            var point = m_LavaPoints[Random.Range(0, m_LavaPoints.Count)];
            var spread = m_Grid.CellSize * 0.4f;
            return point + new Vector3(Random.Range(-spread, spread), 0f, Random.Range(-spread, spread));
        }

        // Des bulles qui gonflent puis éclatent (étincelles, petit éclat de lumière), plus souvent quand le dragon s'énerve ;
        // une vapeur de chaleur qui monte doucement.
        void UpdateLava(float level, float cool, float heat, float deltaTime)
        {
            if (m_LavaPoints.Count == 0)
                return;

            var active = level > 0.5f && cool < 0.8f;
            m_BubbleTimer -= deltaTime;
            if (active && m_BubbleTimer <= 0f)
            {
                m_BubbleTimer = Mathf.Lerp(0.7f, 0.22f, heat) * Random.Range(0.6f, 1.4f);
                foreach (var bubble in m_Bubbles)
                {
                    if (bubble.Active)
                        continue;
                    bubble.Active = true;
                    bubble.Point = RandomLavaPoint();
                    bubble.Age = 0f;
                    bubble.Life = Random.Range(0.5f, 0.9f);
                    bubble.Size = Random.Range(0.35f, 0.7f);
                    bubble.Transform.gameObject.SetActive(true);
                    break;
                }
            }

            foreach (var bubble in m_Bubbles)
            {
                if (!bubble.Active)
                    continue;

                bubble.Age += deltaTime;
                if (bubble.Age >= bubble.Life)
                {
                    bubble.Active = false;
                    bubble.Transform.gameObject.SetActive(false);
                    if (m_Settings.fireMaterial != null)
                        FireEffects.SparkBurst(bubble.Point + Vector3.up * 0.1f, m_Settings.fireMaterial, 0.6f, 14, m_Settings.emberColor);
                    FlashAt(bubble.Point + Vector3.up * 0.6f, 2.5f, 5f);
                    continue;
                }

                var size = bubble.Size * Smooth01(bubble.Age / bubble.Life);
                bubble.Transform.position = bubble.Point - Vector3.up * (size * 0.3f);
                bubble.Transform.localScale = Vector3.one * Mathf.Max(0.01f, size);
            }

            if (m_Vapour == null)
                return;

            m_VapourBudget += deltaTime * 6f * level * (1f - cool * 0.7f);
            var emit = new ParticleSystem.EmitParams();
            while (m_VapourBudget >= 1f)
            {
                m_VapourBudget -= 1f;
                emit.position = RandomLavaPoint() + Vector3.up * 0.3f;
                emit.velocity = new Vector3(0f, Random.Range(0.3f, 0.6f), 0f);
                emit.startSize = Random.Range(1.5f, 3f);
                emit.startLifetime = Random.Range(3f, 5f);
                emit.startColor = new Color(1f, 0.42f, 0.12f, 0.07f);
                m_Vapour.Emit(emit, 1);
            }
        }

        void FlashAt(Vector3 position, float intensity, float range)
        {
            Flash oldest = null;
            foreach (var flash in m_Flashes)
            {
                if (oldest == null || flash.Age > oldest.Age)
                    oldest = flash;
            }

            if (oldest == null)
                return;

            oldest.Age = 0f;
            oldest.Peak = intensity;
            oldest.Light.range = range;
            oldest.Light.transform.position = position;
        }

        void UpdateFlashes(float deltaTime)
        {
            foreach (var flash in m_Flashes)
            {
                flash.Age += deltaTime;
                var intensity = flash.Peak * Mathf.Exp(-flash.Age * 8f);
                flash.Light.enabled = intensity > 0.02f;
                flash.Light.intensity = intensity;
            }
        }

        // ---------------------------------------------------------------- pied des coulées

        // Les coulées n'arrivent en bas qu'à la fin de l'intro : leur pied s'anime à ce moment-là.
        void UpdateFeet(float level, float cool)
        {
            if (m_Feet.Count == 0)
                return;

            var cutoff = Shader.GetGlobalFloat(k_FallCutoffId);
            var flicker = 0.85f + 0.15f * Mathf.Sin(Time.time * 7f) * Mathf.Sin(Time.time * 3.1f);
            foreach (var foot in m_Feet)
            {
                var active = level > 0.05f && cutoff < foot.Position.y + 1f;
                var strength = active ? level * (1f - cool) : 0f;
                if (foot.Sparks != null)
                {
                    var emission = foot.Sparks.emission;
                    emission.rateOverTimeMultiplier = foot.SparkRate * strength;
                }

                if (foot.Halo != null)
                {
                    foot.Halo.enabled = strength > 0.01f;
                    if (foot.Halo.enabled)
                        SetGlow(foot.Halo, EffectBuild.GlowColor(foot.Halo), 1.5f * strength * flicker);
                }

                if (foot.Light != null)
                {
                    foot.Light.enabled = strength > 0.01f;
                    foot.Light.intensity = foot.LightIntensity * strength * flicker;
                }
            }
        }

        // ---------------------------------------------------------------- sol brûlant

        void CreateScorches()
        {
            if (m_Settings.glowMaterial == null)
                return;

            var group = EffectBuild.Group(m_RuntimeGroup, "Sol brûlant");
            for (var i = 0; i < k_ScorchCount; i++)
            {
                var renderer = CreateRenderer("Sol brûlant", group, m_Blobs[i % m_Blobs.Length], m_Settings.glowMaterial);
                renderer.enabled = false;
                m_Scorches.Add(new Scorch { Renderer = renderer });
            }
        }

        // Le sol sous ce point reste brûlant : orange vif, puis rouge sombre, puis plus rien (en 5 à 7 s).
        void ScorchAt(Vector3 point, float radius, float strength)
        {
            if (m_Scorches.Count == 0 || !TrySurface(point + Vector3.up * 2.5f, Vector3.down, 6f, out var ground, out var normal))
                return;
            if (normal.y < 0.5f || m_Arena.IsLavaAt(ground))
                return;

            Scorch oldest = null;
            foreach (var scorch in m_Scorches)
            {
                if (oldest == null || scorch.Age > oldest.Age)
                    oldest = scorch;
            }

            oldest.Age = 0f;
            oldest.Life = Random.Range(5f, 7f);
            oldest.Strength = strength;
            var rotation = Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            oldest.Renderer.transform.SetPositionAndRotation(ground + normal * 0.03f, rotation);
            oldest.Renderer.transform.localScale = new Vector3(radius, 1f, radius);
            oldest.Renderer.enabled = true;
        }

        void UpdateScorches(float deltaTime)
        {
            var hot = m_Settings.scorchColor;
            var cold = new Color(0.55f, 0.06f, 0.02f);
            foreach (var scorch in m_Scorches)
            {
                if (!scorch.Renderer.enabled)
                    continue;

                scorch.Age += deltaTime;
                var t = scorch.Age / scorch.Life;
                if (t >= 1f)
                {
                    scorch.Renderer.enabled = false;
                    continue;
                }

                var cooling = Mathf.Pow(1f - t, 1.5f);
                SetGlow(scorch.Renderer, Color.Lerp(hot, cold, Mathf.Sqrt(t)), 1.8f * scorch.Strength * cooling);
            }
        }

        void OnFireballExploded(Vector3 point, float radius)
        {
            // Une boule qui éclate en l'air (sur un pilier, contre une paroi) ne brûle pas le sol.
            if (isActiveAndEnabled && point.y - m_Arena.GroundHeightAt(point) < 2f)
                ScorchAt(point, Mathf.Clamp(radius * 0.6f, 1f, 3f), 1f);
        }

        // ---------------------------------------------------------------- souffle : le sol chauffe avant

        // Un secteur au sol, de la portée et de l'angle du souffle : il rougit de plus en plus pendant que le dragon
        // inspire, là où il va cracher (vers le joueur), puis reste faible pendant le souffle.
        void CreateWarning()
        {
            if (m_Dragon == null || m_Settings.glowMaterial == null)
                return;

            m_Warning = CreateRenderer("Sol qui chauffe", m_RuntimeGroup, Own(BuildSector(m_Dragon.BreathHalfAngle)), m_Settings.glowMaterial);
            m_Warning.enabled = false;
        }

        void UpdateBreath(float deltaTime)
        {
            if (m_Dragon == null)
                return;

            // Pas pendant l'intro, où il crache vers la voûte.
            var fighting = m_Director == null || m_Director.IsFighting;
            var charge = fighting ? m_Dragon.BreathChargeProgress : -1f;
            var breathing = fighting && m_Dragon.IsBreathing;
            var target = charge >= 0f ? charge * charge : breathing ? 0.35f : 0f;
            m_WarningLevel = target > m_WarningLevel ? target : Mathf.MoveTowards(m_WarningLevel, target, deltaTime * 2.5f);

            if (m_Warning != null)
            {
                m_Warning.enabled = m_WarningLevel > 0.01f;
                if (m_Warning.enabled)
                {
                    var mouth = m_Dragon.Mouth;
                    var aim = breathing ? m_Dragon.BreathDirection : ArenaPlayer.Chest - mouth;
                    aim.y = 0f;
                    var forward = m_Dragon.transform.forward;
                    forward.y = 0f;
                    if (!breathing && forward.sqrMagnitude > 1e-4f && aim.sqrMagnitude > 1e-4f)
                        aim = Quaternion.AngleAxis(Mathf.Clamp(Vector3.SignedAngle(forward, aim, Vector3.up), -45f, 45f), Vector3.up) * forward;
                    if (aim.sqrMagnitude < 1e-4f)
                        aim = forward.sqrMagnitude > 1e-4f ? forward : Vector3.forward;

                    var reach = m_Dragon.BreathReach;
                    m_Warning.transform.SetPositionAndRotation(new Vector3(mouth.x, m_Arena.FloorY + 0.06f, mouth.z), Quaternion.LookRotation(aim.normalized));
                    m_Warning.transform.localScale = new Vector3(reach, 1f, reach);
                    var flicker = 0.85f + 0.15f * Mathf.Sin(Time.time * (10f + 30f * Mathf.Max(0f, charge)));
                    SetGlow(m_Warning, m_Settings.warningColor, 2.2f * m_WarningLevel * flicker);
                }
            }

            // Pendant le souffle, là où les flammes touchent le sol, il reste brûlant.
            if (!breathing)
                return;

            m_BreathScorchTimer -= deltaTime;
            if (m_BreathScorchTimer > 0f)
                return;

            m_BreathScorchTimer = 0.18f;
            var origin = m_Dragon.Mouth;
            var axis = m_Dragon.BreathDirection.normalized;
            var right = Vector3.Cross(Vector3.up, axis);
            var lower = right.sqrMagnitude > 1e-4f ? Quaternion.AngleAxis(m_Dragon.BreathHalfAngle * 0.8f, right.normalized) * axis : axis;
            foreach (var direction in new[] { axis, lower })
            {
                if (Physics.Raycast(origin, direction, out var hit, m_Dragon.BreathReach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                    hit.normal.y > 0.5f && hit.collider.transform.IsChildOf(m_Arena.transform))
                {
                    ScorchAt(hit.point, Random.Range(0.9f, 1.4f), 0.8f);
                    break;
                }
            }
        }

        void OnSlammed(Vector3 paws)
        {
            if (!isActiveAndEnabled)
                return;

            ScorchAt(paws, 3.2f, 1.3f);
            FlashAt(paws + Vector3.up * 1f, 6f, 14f);
            m_CrackFlash = Mathf.Max(m_CrackFlash, 5f);
        }

        // ---------------------------------------------------------------- changements de phase

        void CreateWave()
        {
            if (m_Settings.glowMaterial == null)
                return;

            m_WaveMesh = Own(new Mesh { name = "Onde de lumière" });
            m_WaveMesh.MarkDynamic();
            m_WaveVertices = new Vector3[k_WaveSegments * 2];
            var uvs = new Vector2[k_WaveSegments * 2];
            var colors = new Color[k_WaveSegments * 2];
            var triangles = new int[k_WaveSegments * 6];
            for (var i = 0; i < k_WaveSegments; i++)
            {
                uvs[i * 2] = new Vector2(0f, 0f);
                uvs[i * 2 + 1] = new Vector2(1f, 0f);
                colors[i * 2] = colors[i * 2 + 1] = Color.white;
                var next = (i + 1) % k_WaveSegments;
                triangles[i * 6] = i * 2;
                triangles[i * 6 + 1] = next * 2;
                triangles[i * 6 + 2] = i * 2 + 1;
                triangles[i * 6 + 3] = i * 2 + 1;
                triangles[i * 6 + 4] = next * 2;
                triangles[i * 6 + 5] = next * 2 + 1;
            }

            m_WaveMesh.vertices = m_WaveVertices;
            m_WaveMesh.uv = uvs;
            m_WaveMesh.colors = colors;
            m_WaveMesh.triangles = triangles;
            m_Wave = CreateRenderer("Onde de lumière", m_RuntimeGroup, m_WaveMesh, m_Settings.glowMaterial);
            m_Wave.enabled = false;
        }

        // L'envol et la rage : une onde de lumière part du dragon sur tout le sol, les craquelures flashent, les braseros
        // et la lave s'embrasent.
        void OnPhaseChanged(DragonPhase phase)
        {
            if (!isActiveAndEnabled || (phase != DragonPhase.Air && phase != DragonPhase.Enraged))
                return;

            var origin = m_Dragon != null ? m_Dragon.transform.position : m_Arena.Center;
            m_WaveOrigin = new Vector3(origin.x, m_Arena.FloorY + 0.08f, origin.z);
            m_WaveAge = 0f;
            m_CrackFlash = Mathf.Max(m_CrackFlash, 4f);
            m_DecorFlash = 2.5f;
            m_LavaFlash = 1f;
            if (m_Grid == null || m_Settings.fireMaterial == null)
                return;

            var bursts = 0;
            foreach (var fire in m_Grid.DecorFires)
            {
                if (fire == null || bursts++ >= 12)
                    continue;
                FireEffects.SparkBurst(fire.transform.position + Vector3.up * 0.3f, m_Settings.fireMaterial, 1.2f, 30, m_Settings.emberColor);
            }
        }

        void UpdateWave(float deltaTime)
        {
            if (m_Wave == null)
                return;

            m_WaveAge += deltaTime;
            var maxRadius = m_Arena.RoamRadius + 8f;
            var radius = m_WaveAge * 16f;
            m_Wave.enabled = radius < maxRadius;
            if (!m_Wave.enabled)
                return;

            const float width = 1.6f;
            var inner = Mathf.Max(0f, radius - width * 0.5f);
            var outer = radius + width * 0.5f;
            for (var i = 0; i < k_WaveSegments; i++)
            {
                var angle = i * Mathf.PI * 2f / k_WaveSegments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                m_WaveVertices[i * 2] = direction * inner;
                m_WaveVertices[i * 2 + 1] = direction * outer;
            }

            m_WaveMesh.vertices = m_WaveVertices;
            m_WaveMesh.RecalculateBounds();
            m_Wave.transform.position = m_WaveOrigin;
            SetGlow(m_Wave, new Color(1f, 0.6f, 0.25f), 3f * (1f - radius / maxRadius));
        }

        // ---------------------------------------------------------------- tourelles

        void OnTurretLanded(Vector3 position)
        {
            if (!isActiveAndEnabled || m_Settings.glowMaterial == null)
                return;

            m_RuneMesh ??= Own(MapGameplayLights.BuildRuneMesh());
            m_RuneMaterial ??= Own(EffectBuild.GlowMaterial(m_Settings.glowMaterial, m_Settings.runeColor, 1.4f, "Runes des tourelles"));
            var renderer = CreateRenderer("Rune de la tourelle", EffectBuild.Group(m_RuntimeGroup, "Runes des tourelles"), m_RuneMesh, m_RuneMaterial);
            renderer.transform.SetPositionAndRotation(position + Vector3.up * 0.06f, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            renderer.transform.localScale = Vector3.one * 1.5f;
            m_Runes.Add(new Rune { Renderer = renderer });
        }

        void UpdateRunes(float level, float deltaTime)
        {
            var spin = Quaternion.Euler(0f, 10f * deltaTime, 0f);
            foreach (var rune in m_Runes)
            {
                if (rune.Renderer == null)
                    continue;

                rune.Age += deltaTime;
                rune.Renderer.transform.rotation = spin * rune.Renderer.transform.rotation;
                var breath = 0.85f + 0.15f * Mathf.Sin(Time.time * 1.6f + rune.Age);
                SetGlow(rune.Renderer, m_Settings.runeColor, (1.4f * breath + 3f * Mathf.Exp(-rune.Age * 3f)) * Mathf.Max(level, 0.3f));
            }
        }

        // ---------------------------------------------------------------- joueur

        void UpdatePlayerLight(float level)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            var inside = head != null && head.position.y > m_Arena.FloorY - 5f && level > 0.01f;
            m_PlayerLight.enabled = inside;
            if (!inside)
                return;

            m_PlayerLight.transform.position = head.position + Vector3.up * 0.3f;
            m_PlayerLight.intensity = m_Settings.arenaPlayerLightIntensity * level;
        }

        // ---------------------------------------------------------------- victoire

        void OnDragonDied(Health health, DamageInfo info)
        {
            if (m_VictoryTime < 0f)
                m_VictoryTime = Time.time;
        }

        // La lave s'éteint et la grotte s'assombrit, les cristaux blanchissent, puis le soleil entre (au lancement : caché).
        void UpdateVictory(float since, float cool)
        {
            if (m_VictoryTime >= 0f && Mathf.Abs(cool - m_AppliedCool) > 0.005f)
            {
                m_AppliedCool = cool;
                m_Arena.SetLightLevel(Mathf.Lerp(1f, 0.4f, cool));
            }

            if (m_VictoryTime >= 0f && m_Crystals != null && since < 6f)
                m_Crystals.SetTint(Color.Lerp(new Color(1f, 0.85f, 0.6f), new Color(0.92f, 0.96f, 1f), Smooth01((since - 0.5f) / 3f)));

            var sun = m_VictoryTime >= 0f ? Smooth01((since - 1.5f) / 3.5f) : 0f;
            foreach (var sunbeam in m_Sunbeams)
            {
                if (sunbeam.Beam != null)
                {
                    sunbeam.Beam.enabled = sun > 0.005f;
                    if (sunbeam.Beam.enabled)
                    {
                        var color = m_Settings.sunColor;
                        color.a = 0.4f * sun;
                        m_Block.Clear();
                        m_Block.SetColor(k_BaseColorId, color);
                        sunbeam.Beam.SetPropertyBlock(m_Block);
                    }
                }

                if (sunbeam.Spot != null)
                {
                    sunbeam.Spot.enabled = sun > 0.005f;
                    if (sunbeam.Spot.enabled)
                        SetGlow(sunbeam.Spot, EffectBuild.GlowColor(sunbeam.Spot), 1.3f * sun);
                }

                if (sunbeam.Light != null)
                {
                    sunbeam.Light.enabled = sun > 0.005f;
                    sunbeam.Light.intensity = sunbeam.LightIntensity * sun;
                }

                if (sunbeam.Dust != null)
                {
                    var emission = sunbeam.Dust.emission;
                    emission.rateOverTimeMultiplier = sunbeam.DustRate * sun;
                }
            }
        }

        // ---------------------------------------------------------------- maillages

        // Une tache ronde aux bords irréguliers, de rayon 1, à plat : vive au centre, effacée au bord.
        static Mesh BuildBlob(int seed, float jag)
        {
            const int segments = 24;
            var random = new System.Random(seed);
            var vertices = new List<Vector3> { Vector3.zero };
            var colors = new List<Color> { Color.white };
            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var radius = 1f - jag * (float)random.NextDouble();
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices.Add(direction * (radius * 0.55f));
                colors.Add(new Color(1f, 1f, 1f, 0.75f));
                vertices.Add(direction * radius);
                colors.Add(new Color(1f, 1f, 1f, 0f));
            }

            var triangles = new List<int>();
            for (var i = 0; i < segments; i++)
            {
                var next = (i + 1) % segments;
                int middle = 1 + i * 2, edge = 2 + i * 2, nextMiddle = 1 + next * 2, nextEdge = 2 + next * 2;
                triangles.AddRange(new[] { 0, nextMiddle, middle, middle, nextMiddle, nextEdge, middle, nextEdge, edge });
            }

            // uv.x à 0,5 partout : le shader Glow n'efface pas les bords, c'est l'alpha des sommets qui le fait.
            var uvs = new List<Vector2>();
            for (var i = 0; i < vertices.Count; i++)
                uvs.Add(new Vector2(0.5f, 0f));

            var mesh = new Mesh { name = "Tache lumineuse" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Un secteur à plat de rayon 1 vers +Z, d'un demi-angle donné : effacé près du dragon, au bout et sur les côtés.
        static Mesh BuildSector(float halfAngle)
        {
            const int radial = 8;
            const int angular = 12;
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var uvs = new List<Vector2>();
            for (var r = 0; r <= radial; r++)
            {
                var distance = r / (float)radial;
                var alpha = Mathf.SmoothStep(0f, 1f, distance / 0.15f) * (1f - Mathf.SmoothStep(0f, 1f, (distance - 0.7f) / 0.3f));
                for (var a = 0; a <= angular; a++)
                {
                    var across = a / (float)angular;
                    var angle = Mathf.Lerp(-halfAngle, halfAngle, across) * Mathf.Deg2Rad;
                    vertices.Add(new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * distance);
                    colors.Add(new Color(1f, 1f, 1f, alpha));
                    uvs.Add(new Vector2(across, distance));
                }
            }

            var triangles = new List<int>();
            for (var r = 0; r < radial; r++)
            {
                for (var a = 0; a < angular; a++)
                {
                    var i = r * (angular + 1) + a;
                    triangles.AddRange(new[] { i, i + angular + 1, i + 1, i + 1, i + angular + 1, i + angular + 2 });
                }
            }

            var mesh = new Mesh { name = "Secteur du souffle" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static float Smooth01(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        static Vector2 Random2(System.Random random)
        {
            var angle = (float)random.NextDouble() * Mathf.PI * 2f;
            var radius = Mathf.Sqrt((float)random.NextDouble());
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
    }
}
