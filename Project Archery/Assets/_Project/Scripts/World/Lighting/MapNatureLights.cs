using System.Collections.Generic;
using Archery.FinalBoss;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.World
{
    /// <summary>
    /// Les lumières de la nature : lucioles dans la forêt, champignons et fleurs qui luisent la nuit, cristaux dans les
    /// parois de la jungle, pollen doré autour du joueur en plein jour, rayons du soleil couchant, brume et arc-en-ciel
    /// aux cascades. Tout suit l'heure du ciel (<see cref="SkyController.Lamps"/>).
    /// </summary>
    public class MapNatureLights : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_TintId = Shader.PropertyToID("_Tint");

        struct Glow
        {
            public Material Material;
            public Color Color;
        }

        struct Shaft
        {
            public Transform Beam;
            public MeshRenderer Renderer;
            public Vector3 Ground;
        }

        readonly List<ParticleSystem> m_Fireflies = new List<ParticleSystem>();
        readonly List<float> m_FireflyRates = new List<float>();
        readonly Dictionary<Material, Material> m_GlowClones = new Dictionary<Material, Material>();
        readonly List<Glow> m_Glows = new List<Glow>();
        readonly List<Light> m_CrystalLights = new List<Light>();
        readonly List<Shaft> m_Shafts = new List<Shaft>();
        readonly List<ParticleSystem> m_Mists = new List<ParticleSystem>();
        readonly List<Object> m_Owned = new List<Object>();
        MapLightingSettings m_Settings;
        MapScan m_Scan;
        Material m_BrightParticles;
        Material m_SoftParticles;
        Material m_CrystalMaterial;
        ParticleSystem m_Motes;
        MeshRenderer m_Rainbow;
        Vector3 m_RainbowFacing = Vector3.forward;
        MaterialPropertyBlock m_Block;
        float m_LastLamps = -1f;

        public void Init(MapLightingSettings settings, MapScan scan)
        {
            m_Settings = settings;
            m_Scan = scan;
            m_Block = new MaterialPropertyBlock();
            if (settings.fireMaterial != null)
            {
                m_BrightParticles = Tinted(settings.fireMaterial, 3f, "lucioles");
                m_SoftParticles = Tinted(settings.fireMaterial, 1f, "pollen et brume");
            }

            CreateFireflies();
            SetupGlowingPlants();
            CreateCliffCrystals();
            CreateMotes();
            CreateLightShafts();
            CreateWaterfallEffects();
        }

        void OnDestroy()
        {
            foreach (var owned in m_Owned)
            {
                if (owned != null)
                    Destroy(owned);
            }
        }

        Material Tinted(Material source, float brightness, string label)
        {
            var material = new Material(source) { name = $"{source.name} ({label})" };
            if (material.HasProperty(k_BaseColorId))
                material.SetColor(k_BaseColorId, new Color(brightness, brightness, brightness, 1f));
            m_Owned.Add(material);
            return material;
        }

        void Update()
        {
            var lamps = SkyController.Lamps;
            var night = Mathf.SmoothStep(0f, 1f, (lamps - 0.3f) / 0.7f);
            var viewer = Viewer();
            var inMap = viewer.y < 150f;

            for (var i = 0; i < m_Fireflies.Count; i++)
            {
                var emission = m_Fireflies[i].emission;
                emission.rateOverTimeMultiplier = m_FireflyRates[i] * Mathf.Clamp01((lamps - 0.2f) / 0.8f);
            }

            if (m_Motes != null)
            {
                m_Motes.transform.position = viewer;
                var emission = m_Motes.emission;
                var day = 1f - lamps;
                emission.rateOverTimeMultiplier = inMap ? 30f * day * day : 0f;
            }

            if (Mathf.Abs(lamps - m_LastLamps) > 0.004f)
            {
                m_LastLamps = lamps;
                foreach (var glow in m_Glows)
                    glow.Material.SetColor(k_EmissionColorId, glow.Color * night);
                if (m_CrystalMaterial != null)
                    m_CrystalMaterial.SetFloat(k_IntensityId, m_Settings.crystalIntensity * Mathf.Lerp(0.35f, 1f, lamps));
                foreach (var light in m_CrystalLights)
                    light.intensity = 1.1f * night;
                foreach (var mist in m_Mists)
                {
                    var main = mist.main;
                    main.startColor = new Color(1f, 1f, 1f, 0.13f * (1f - 0.65f * lamps));
                }
            }

            UpdateShafts();
            UpdateRainbow();
        }

        static Vector3 Viewer()
        {
            var rig = Player.PlayerRig.Instance;
            if (rig != null && rig.Head != null)
                return rig.Head.position;
            var camera = Camera.main;
            return camera != null ? camera.transform.position : Vector3.zero;
        }

        // ---------------------------------------------------------------- lucioles

        void CreateFireflies()
        {
            if (m_BrightParticles == null)
                return;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.15f, 0.3f),
                    new GradientAlphaKey(1f, 0.45f), new GradientAlphaKey(0.1f, 0.65f), new GradientAlphaKey(0.9f, 0.8f),
                    new GradientAlphaKey(0f, 1f),
                });

            foreach (var point in MapScan.Spread(m_Scan.ForestPoints, m_Settings.fireflyGroups, 7f, 41))
            {
                var count = m_Settings.firefliesPerGroup;
                var fireflies = GlowParticles.Create("Lucioles", transform, point + Vector3.up * 1.2f, m_BrightParticles);
                var main = fireflies.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
                main.startColor = m_Settings.fireflyColor;
                main.maxParticles = count;
                var emission = fireflies.emission;
                emission.rateOverTime = count / 5f;
                var shape = fireflies.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 3f;
                var noise = fireflies.noise;
                noise.enabled = true;
                noise.strength = 0.5f;
                noise.frequency = 0.25f;
                noise.scrollSpeed = 0.2f;
                var color = fireflies.colorOverLifetime;
                color.enabled = true;
                color.color = new ParticleSystem.MinMaxGradient(gradient);
                fireflies.Play();
                m_Fireflies.Add(fireflies);
                m_FireflyRates.Add(count / 5f);
            }
        }

        // ---------------------------------------------------------------- champignons et fleurs

        // Les chapeaux des champignons et les pétales d'une partie des fleurs reçoivent une copie de leur matériau, qui
        // brille la nuit. Les tiges ne changent pas.
        void SetupGlowingPlants()
        {
            foreach (var mushroom in m_Scan.Mushrooms)
            {
                foreach (var renderer in mushroom.GetComponentsInChildren<Renderer>())
                {
                    SwapGlow(renderer, material =>
                    {
                        var name = material.name;
                        if (name.Contains("colorRed"))
                            return m_Settings.redMushroomGlow * m_Settings.mushroomGlow;
                        if (name.Contains("colorTan"))
                            return m_Settings.tanMushroomGlow * m_Settings.mushroomGlow;
                        return Color.clear;
                    });
                }
            }

            foreach (var flower in m_Scan.Flowers)
            {
                if (Hash(flower.position) > m_Settings.flowerShare)
                    continue;

                foreach (var renderer in flower.GetComponentsInChildren<Renderer>())
                {
                    SwapGlow(renderer, material =>
                    {
                        if (!material.name.StartsWith("color") || !material.HasProperty(k_BaseColorId))
                            return Color.clear;
                        return material.GetColor(k_BaseColorId) * m_Settings.flowerGlow;
                    });
                }
            }
        }

        void SwapGlow(Renderer renderer, System.Func<Material, Color> glowOf)
        {
            var materials = renderer.sharedMaterials;
            var changed = false;
            for (var i = 0; i < materials.Length; i++)
            {
                var source = materials[i];
                if (source == null || !source.HasProperty(k_EmissionColorId))
                    continue;

                var glow = glowOf(source);
                if (glow.maxColorComponent <= 0f)
                    continue;

                if (!m_GlowClones.TryGetValue(source, out var clone))
                {
                    clone = new Material(source) { name = source.name + " (lumineux)" };
                    clone.EnableKeyword("_EMISSION");
                    clone.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                    clone.SetColor(k_EmissionColorId, Color.black);
                    m_GlowClones.Add(source, clone);
                    m_Glows.Add(new Glow { Material = clone, Color = glow });
                    m_Owned.Add(clone);
                }

                materials[i] = clone;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = materials;
        }

        static float Hash(Vector3 position)
        {
            var h = Mathf.Sin(position.x * 12.9898f + position.z * 78.233f + position.y * 37.719f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }

        // ---------------------------------------------------------------- cristaux des parois

        // Des grappes de cristaux qui sortent des parois de la jungle, à hauteur de genou : bleu-vert, plus vives la nuit.
        void CreateCliffCrystals()
        {
            if (m_Settings.crystalMaterial == null || m_Settings.cliffCrystals <= 0)
                return;

            m_CrystalMaterial = new Material(m_Settings.crystalMaterial) { name = "Cristaux des parois (en jeu)" };
            m_CrystalMaterial.SetColor(k_TintId, Color.white);
            m_Owned.Add(m_CrystalMaterial);

            var walls = new List<Vector3>();
            var normals = new List<Vector3>();
            foreach (var point in MapScan.Spread(m_Scan.ForestPoints, m_Settings.cliffCrystals * 5, 5f, 53))
            {
                for (var k = 0; k < 8; k++)
                {
                    var direction = Quaternion.Euler(0f, k * 45f + 10f, 0f) * Vector3.forward;
                    if (!Physics.Raycast(point + Vector3.up * 0.9f, direction, out var hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        continue;
                    if (Mathf.Abs(hit.normal.y) > 0.3f || hit.distance < 0.8f || MapScan.IsPlant(hit.collider.transform) || m_Scan.LaneDistance(hit.point) < 4f)
                        continue;

                    walls.Add(hit.point);
                    normals.Add(new Vector3(hit.normal.x, 0f, hit.normal.z).normalized);
                    break;
                }
            }

            var random = new System.Random(61);
            var placed = new List<Vector3>();
            for (var i = 0; i < walls.Count && placed.Count < m_Settings.cliffCrystals; i++)
            {
                var free = true;
                foreach (var other in placed)
                    free &= (other - walls[i]).sqrMagnitude >= 9f * 9f;
                if (!free)
                    continue;

                placed.Add(walls[i]);
                var cluster = CreateCluster(walls[i], normals[i], random);
                if (placed.Count % 3 == 1)
                {
                    var light = new GameObject("Lueur des cristaux").AddComponent<Light>();
                    light.transform.SetParent(cluster, false);
                    light.transform.localPosition = new Vector3(0f, 0.3f, 0.6f);
                    light.type = LightType.Point;
                    light.shadows = LightShadows.None;
                    light.color = m_Settings.crystalColor;
                    light.range = 4.5f;
                    m_CrystalLights.Add(light);
                }
            }
        }

        Transform CreateCluster(Vector3 point, Vector3 normal, System.Random random)
        {
            var root = new GameObject("Cristaux").transform;
            root.SetParent(transform, false);
            root.SetPositionAndRotation(point, Quaternion.LookRotation(normal));

            // Dans le repère de la grappe : +Z sort de la paroi.
            var builder = new MeshBuilder();
            var count = 3 + random.Next(3);
            for (var i = 0; i < count; i++)
            {
                var basePoint = new Vector3(Range(random, -0.3f, 0.3f), Range(random, -0.25f, 0.2f), -0.05f);
                var axis = new Vector3(Range(random, -0.45f, 0.45f), Range(random, 0.15f, 0.9f), 1f).normalized;
                var shade = Range(random, 0.8f, 1.1f);
                var color = m_Settings.crystalColor * shade;
                color.a = 1f;
                builder.Crystal(basePoint, axis, Range(random, 0.3f, 0.9f), Range(random, 0.06f, 0.14f), 5, color,
                                Range(random, 0f, 6f), Range(random, 0f, 1f));
            }

            var mesh = builder.ToMesh("Cristaux des parois");
            m_Owned.Add(mesh);
            var go = new GameObject("Grappe");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_CrystalMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return root;
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        // ---------------------------------------------------------------- jour : pollen et rayons

        // Pollen et poussières dorées qui flottent autour du joueur, en plein jour seulement.
        void CreateMotes()
        {
            if (m_SoftParticles == null)
                return;

            m_Motes = GlowParticles.Create("Pollen", transform, Viewer(), m_SoftParticles);
            var main = m_Motes.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
            main.startColor = m_Settings.moteColor;
            main.maxParticles = 260;
            var shape = m_Motes.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 6f, 18f);
            var noise = m_Motes.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.2f;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            var color = m_Motes.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
            m_Motes.Play();
        }

        // Rayons de lumière dans la forêt quand le soleil est bas : des faisceaux inclinés vers le soleil, très légers.
        void CreateLightShafts()
        {
            if (m_Settings.beamMaterial == null)
                return;

            foreach (var point in MapScan.Spread(m_Scan.ForestPoints, m_Settings.lightShafts, 14f, 71))
            {
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beam.name = "Rayon de soleil";
                Destroy(beam.GetComponent<Collider>());
                beam.transform.SetParent(transform, false);
                var renderer = beam.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = m_Settings.beamMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                m_Shafts.Add(new Shaft { Beam = beam.transform, Renderer = renderer, Ground = point });
            }
        }

        void UpdateShafts()
        {
            var sky = SkyController.Current;
            if (sky == null || m_Shafts.Count == 0)
                return;

            // Le soleil bas les fait apparaître ; ils montent vers lui, au moins à 35° (plus bas, on ne les verrait plus).
            var strength = sky.night ? 0f : Mathf.Clamp01(1f - sky.elevation / 40f) * 0.22f;
            var toSun = Quaternion.Euler(-Mathf.Max(35f, sky.elevation), sky.azimuth, 0f) * Vector3.forward;
            var color = sky.lightColor;
            color.a = strength;
            const float length = 16f;
            foreach (var shaft in m_Shafts)
            {
                shaft.Renderer.enabled = strength > 0.005f;
                if (!shaft.Renderer.enabled)
                    continue;

                shaft.Beam.SetPositionAndRotation(shaft.Ground + toSun * (length * 0.5f), Quaternion.FromToRotation(Vector3.up, toSun));
                shaft.Beam.localScale = new Vector3(1.4f, length * 0.5f, 1.4f);
                m_Block.Clear();
                m_Block.SetColor(k_BaseColorId, color);
                shaft.Renderer.SetPropertyBlock(m_Block);
            }
        }

        // ---------------------------------------------------------------- cascades

        // Une cascade de la rivière : son pied, le sens où l'eau s'en va (à l'horizontale), sa largeur et sa hauteur.
        struct Fall
        {
            public Vector3 Foot;
            public Vector3 Outward;
            public float Width;
            public float Height;
        }

        void CreateWaterfallEffects()
        {
            var river = FindAnyObjectByType<RiverWater>();
            if (river == null)
                return;

            // La nappe d'eau de chaque cascade, construite par RiverWater : « Waterfall <nom de la pièce> ».
            var sheets = new Dictionary<string, Bounds>();
            foreach (var filter in river.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null && filter.TryGetComponent<Renderer>(out var renderer))
                    sheets[filter.sharedMesh.name] = renderer.bounds;
            }

            var tallest = new Fall();
            var hasTallest = false;
            foreach (var piece in river.Falls)
            {
                if (piece == null)
                    continue;

                // Mêmes repères que RiverWater : la pièce du kit a l'eau qui part vers son axe Z.
                var outward = piece.forward;
                outward.y = 0f;
                var fall = new Fall
                {
                    Foot = piece.TransformPoint(new Vector3(0f, 0.1f, -0.85f)),
                    Outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.forward,
                    Width = 1.62f * piece.lossyScale.x,
                    Height = sheets.TryGetValue("Waterfall " + piece.name, out var sheet) ? sheet.size.y : 3f * piece.lossyScale.y,
                };

                if (m_Settings.waterfallMist && m_SoftParticles != null)
                    CreateMist(fall);
                if (!hasTallest || fall.Height > tallest.Height)
                {
                    tallest = fall;
                    hasTallest = true;
                }
            }

            if (m_Settings.rainbow && hasTallest)
                CreateRainbow(tallest);
        }

        // Brume au pied d'une cascade : de grosses particules pâles qui montent doucement.
        void CreateMist(Fall fall)
        {
            var mist = GlowParticles.Create("Brume", transform, fall.Foot + fall.Outward * 0.5f + Vector3.up * 0.3f, m_SoftParticles);
            var main = mist.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.2f);
            main.startColor = new Color(1f, 1f, 1f, 0.13f);
            main.gravityModifier = -0.05f;
            main.maxParticles = 40;
            var emission = mist.emission;
            emission.rateOverTime = 10f;
            var shape = mist.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = Mathf.Max(0.8f, fall.Width * 0.6f);
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            var color = mist.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
            mist.Play();
            m_Mists.Add(mist);
        }

        // Un arc-en-ciel dans la brume de la plus haute cascade, juste devant elle et parallèle à sa face (on le voit
        // de face depuis l'aval) : sept bandes qui se fondent, à la taille de la chute.
        void CreateRainbow(Fall fall)
        {
            if (m_Settings.glowMaterial == null)
                return;

            var root = new GameObject("Arc-en-ciel").transform;
            root.SetParent(transform, false);
            root.SetPositionAndRotation(fall.Foot + fall.Outward * 1.5f - Vector3.up * 0.1f, Quaternion.LookRotation(fall.Outward));
            root.localScale = Vector3.one * Mathf.Clamp(fall.Height * 0.8f, 2f, 4.5f);
            m_RainbowFacing = fall.Outward;

            Color[] bands =
            {
                new Color(1f, 0.15f, 0.1f), new Color(1f, 0.55f, 0.1f), new Color(1f, 0.95f, 0.2f), new Color(0.3f, 1f, 0.3f),
                new Color(0.2f, 0.6f, 1f), new Color(0.3f, 0.25f, 1f), new Color(0.65f, 0.25f, 1f),
            };
            var builder = new MeshBuilder();
            const int segments = 32;
            const float outer = 1f;
            const float width = 0.045f;
            for (var b = 0; b < bands.Length; b++)
            {
                var r1 = outer - b * width * 0.8f;
                var r0 = r1 - width;
                var color = bands[b];
                color.a = 0.5f;
                for (var s = 0; s < segments; s++)
                {
                    var a0 = Mathf.PI * s / segments;
                    var a1 = Mathf.PI * (s + 1) / segments;
                    var d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f);
                    var d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                    builder.Quad(d0 * r0, d1 * r0, d1 * r1, d0 * r1, color,
                                 new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f));
                }
            }

            var mesh = builder.ToMesh("Arc-en-ciel");
            m_Owned.Add(mesh);
            root.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            m_Rainbow = root.gameObject.AddComponent<MeshRenderer>();
            m_Rainbow.sharedMaterial = m_Settings.glowMaterial;
            m_Rainbow.shadowCastingMode = ShadowCastingMode.Off;
        }

        // Le jour seulement, quand le soleil n'est pas trop bas ; il s'efface quand on le voit de profil.
        void UpdateRainbow()
        {
            if (m_Rainbow == null)
                return;

            var sky = SkyController.Current;
            var strength = sky == null || sky.night ? 0f : Mathf.Clamp01((sky.elevation - 3f) / 15f) * 0.6f;
            var toViewer = Viewer() - m_Rainbow.transform.position;
            toViewer.y = 0f;
            if (toViewer.sqrMagnitude > 1e-4f)
                strength *= Mathf.SmoothStep(0f, 1f, (Mathf.Abs(Vector3.Dot(toViewer.normalized, m_RainbowFacing)) - 0.2f) / 0.5f);
            m_Rainbow.enabled = strength > 0.01f;
            m_Block.Clear();
            m_Block.SetFloat(k_IntensityId, strength);
            m_Rainbow.SetPropertyBlock(m_Block);
        }
    }

    /// <summary>Petites particules lumineuses (lucioles, pollen, brume, âmes) : les réglages communs.</summary>
    static class GlowParticles
    {
        public static ParticleSystem Create(string name, Transform parent, Vector3 position, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }
    }
}
