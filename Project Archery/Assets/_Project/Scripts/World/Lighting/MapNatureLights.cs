using System.Collections.Generic;
using Archery.FinalBoss;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.World
{
    /// <summary>
    /// Les lumières de la nature (groupe « Nature » de l'objet « Effet ») : lucioles dans la forêt, cristaux dans les
    /// parois de la jungle, pollen doré autour du joueur en plein jour, rayons du soleil couchant, brume et arc-en-ciel
    /// aux cascades ; en jeu, les champignons et une partie des fleurs luisent la nuit. Tout suit l'heure du ciel
    /// (<see cref="SkyController.Lamps"/>).
    /// </summary>
    /// <remarks>
    /// Sous-groupes : « Lucioles », « Cristaux », « Pollen », « Rayons de soleil » (chaque rayon part de son objet, au
    /// sol, vers le soleil), « Cascades » (brumes et arc-en-ciel, tourné comme son objet). On peut les déplacer, les
    /// dupliquer ou les supprimer ; leur force de départ est celle de leur lumière ou de leur matériau.
    /// </remarks>
    public class MapNatureLights : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_TintId = Shader.PropertyToID("_Tint");

        const string k_Fireflies = "Lucioles";
        const string k_Crystals = "Cristaux";
        const string k_Motes = "Pollen";
        const string k_Shafts = "Rayons de soleil";
        const string k_Falls = "Cascades";
        const string k_Rainbow = "Arc-en-ciel";

        struct Glow
        {
            public Material Material;
            public Color Color;
        }

        struct Shaft
        {
            public Transform Anchor;
            public Transform Beam;
            public MeshRenderer Renderer;
        }

        readonly List<ParticleSystem> m_Fireflies = new List<ParticleSystem>();
        readonly List<float> m_FireflyRates = new List<float>();
        readonly Dictionary<Material, Material> m_GlowClones = new Dictionary<Material, Material>();
        readonly List<Glow> m_Glows = new List<Glow>();
        readonly Dictionary<Material, float> m_CrystalMaterials = new Dictionary<Material, float>();
        readonly List<Light> m_CrystalLights = new List<Light>();
        readonly List<float> m_CrystalLightIntensities = new List<float>();
        readonly List<Shaft> m_Shafts = new List<Shaft>();
        readonly List<ParticleSystem> m_Mists = new List<ParticleSystem>();
        readonly List<float> m_MistAlphas = new List<float>();
        readonly List<Object> m_Owned = new List<Object>();
        MapLightingSettings m_Settings;
        MapScan m_Scan;
        ParticleSystem m_Motes;
        MeshRenderer m_Rainbow;
        float m_RainbowIntensity = 0.6f;
        MaterialPropertyBlock m_Block;
        float m_LastLamps = -1f;
        bool m_Bound;

        // ---------------------------------------------------------------- fabrication (jeu ou éditeur)

        Material m_BrightParticles;
        Material m_SoftParticles;

        /// <summary>Fabrique les lucioles, les cristaux, le pollen, les rayons et les effets des cascades sous cet objet.</summary>
        public void Build(MapLightingSettings settings, MapScan scan)
        {
            m_Settings = settings;
            m_Scan = scan;
            if (settings.fireMaterial != null)
            {
                m_BrightParticles = Tinted(settings.fireMaterial, 3f, "lucioles");
                m_SoftParticles = Tinted(settings.fireMaterial, 1f, "pollen et brume");
            }

            CreateFireflies(EffectBuild.Group(transform, k_Fireflies));
            CreateCliffCrystals(EffectBuild.Group(transform, k_Crystals));
            CreateMotes();
            CreateLightShafts(EffectBuild.Group(transform, k_Shafts));
            CreateWaterfallEffects(EffectBuild.Group(transform, k_Falls));
        }

        static Material Tinted(Material source, float brightness, string label)
        {
            var material = new Material(source) { name = $"{source.name} ({label})" };
            if (material.HasProperty(k_BaseColorId))
                material.SetColor(k_BaseColorId, new Color(brightness, brightness, brightness, 1f));
            return material;
        }

        void CreateFireflies(Transform group)
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
                var fireflies = GlowParticles.Create("Lucioles", group, point + Vector3.up * 1.2f, m_BrightParticles);
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
            }
        }

        // Des grappes de cristaux qui sortent des parois de la jungle, à hauteur de genou : bleu-vert, plus vives la nuit.
        void CreateCliffCrystals(Transform group)
        {
            if (m_Settings.crystalMaterial == null || m_Settings.cliffCrystals <= 0)
                return;

            var material = new Material(m_Settings.crystalMaterial) { name = "Cristaux des parois" };
            material.SetColor(k_TintId, Color.white);
            material.SetFloat(k_IntensityId, m_Settings.crystalIntensity);

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
                var cluster = CreateCluster(group, walls[i], normals[i], material, random);
                if (placed.Count % 3 == 1)
                {
                    var light = new GameObject("Lueur des cristaux").AddComponent<Light>();
                    light.transform.SetParent(cluster, false);
                    light.transform.localPosition = new Vector3(0f, 0.3f, 0.6f);
                    light.type = LightType.Point;
                    light.shadows = LightShadows.None;
                    light.color = m_Settings.crystalColor;
                    light.range = 4.5f;
                    light.intensity = 1.1f;
                }
            }
        }

        Transform CreateCluster(Transform group, Vector3 point, Vector3 normal, Material material, System.Random random)
        {
            var root = new GameObject("Cristaux").transform;
            root.SetParent(group, false);
            root.SetPositionAndRotation(point, Quaternion.LookRotation(normal));

            // Dans le repère de la grappe : +Z sort de la paroi.
            var builder = new MeshBuilder();
            var count = 3 + random.Next(3);
            for (var i = 0; i < count; i++)
            {
                var basePoint = new Vector3(Range(random, -0.3f, 0.3f), Range(random, -0.25f, 0.2f), -0.05f);
                var axis = new Vector3(Range(random, -0.45f, 0.45f), Range(random, 0.15f, 0.9f), 1f).normalized;
                var color = m_Settings.crystalColor * Range(random, 0.8f, 1.1f);
                color.a = 1f;
                builder.Crystal(basePoint, axis, Range(random, 0.3f, 0.9f), Range(random, 0.06f, 0.14f), 5, color,
                                Range(random, 0f, 6f), Range(random, 0f, 1f));
            }

            var go = new GameObject("Grappe");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = builder.ToMesh("Cristaux des parois");
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return root;
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        // Pollen et poussières dorées qui flottent autour du joueur, en plein jour seulement.
        void CreateMotes()
        {
            if (m_SoftParticles == null)
                return;

            var motes = GlowParticles.Create(k_Motes, transform, Vector3.zero, m_SoftParticles);
            var main = motes.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
            main.startColor = m_Settings.moteColor;
            main.maxParticles = 260;
            var shape = motes.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 6f, 18f);
            var noise = motes.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.2f;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            var color = motes.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        // Rayons de lumière dans la forêt quand le soleil est bas : un objet au sol par rayon, le faisceau en enfant.
        void CreateLightShafts(Transform group)
        {
            if (m_Settings.beamMaterial == null)
                return;

            foreach (var point in MapScan.Spread(m_Scan.ForestPoints, m_Settings.lightShafts, 14f, 71))
            {
                var anchor = new GameObject("Rayon de soleil").transform;
                anchor.SetParent(group, false);
                anchor.position = point;
                var beam = EffectBuild.Cylinder("Faisceau", anchor);
                // Dans l'éditeur, un rayon vertical : en jeu, il se tourne vers le soleil.
                beam.transform.localPosition = Vector3.up * 8f;
                beam.transform.localScale = new Vector3(1.4f, 8f, 1.4f);
                var renderer = beam.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = m_Settings.beamMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        // Une cascade de la rivière : son pied, le sens où l'eau s'en va (à l'horizontale), sa largeur et sa hauteur.
        struct Fall
        {
            public Vector3 Foot;
            public Vector3 Outward;
            public float Width;
            public float Height;
        }

        void CreateWaterfallEffects(Transform group)
        {
            var river = FindAnyObjectByType<RiverWater>();
            if (river == null)
                return;

            // La nappe d'eau de chaque cascade, construite par RiverWater en jeu : « Waterfall <nom de la pièce> ».
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
                    CreateMist(group, fall);
                if (!hasTallest || fall.Height > tallest.Height)
                {
                    tallest = fall;
                    hasTallest = true;
                }
            }

            if (m_Settings.rainbow && hasTallest)
                CreateRainbow(group, tallest);
        }

        // Brume au pied d'une cascade : de grosses particules pâles qui montent doucement.
        void CreateMist(Transform group, Fall fall)
        {
            var mist = GlowParticles.Create("Brume", group, fall.Foot + fall.Outward * 0.5f + Vector3.up * 0.3f, m_SoftParticles);
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
        }

        // Un arc-en-ciel dans la brume de la plus haute cascade, juste devant elle et parallèle à sa face (on le voit
        // de face depuis l'aval) : sept bandes qui se fondent, à la taille de la chute.
        void CreateRainbow(Transform group, Fall fall)
        {
            if (m_Settings.glowMaterial == null)
                return;

            var root = new GameObject(k_Rainbow).transform;
            root.SetParent(group, false);
            root.SetPositionAndRotation(fall.Foot + fall.Outward * 1.5f - Vector3.up * 0.1f, Quaternion.LookRotation(fall.Outward));
            root.localScale = Vector3.one * Mathf.Clamp(fall.Height * 0.8f, 2f, 4.5f);

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

            root.gameObject.AddComponent<MeshFilter>().sharedMesh = builder.ToMesh(k_Rainbow);
            var renderer = root.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = EffectBuild.GlowMaterial(m_Settings.glowMaterial, Color.white, 0.6f, k_Rainbow);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- en jeu

        /// <summary>Retrouve les effets posés sous cet objet (ceux qui sont visibles) et commence à les animer.</summary>
        public void Bind(MapLightingSettings settings, MapScan scan)
        {
            m_Settings = settings;
            m_Scan = scan;
            m_Block = new MaterialPropertyBlock();

            var fireflies = transform.Find(k_Fireflies);
            if (fireflies != null)
            {
                foreach (var system in fireflies.GetComponentsInChildren<ParticleSystem>())
                {
                    m_Fireflies.Add(system);
                    m_FireflyRates.Add(system.emission.rateOverTimeMultiplier);
                    system.Play();
                }
            }

            SetupGlowingPlants();

            var crystals = transform.Find(k_Crystals);
            if (crystals != null)
            {
                foreach (var renderer in crystals.GetComponentsInChildren<MeshRenderer>())
                {
                    var material = renderer.sharedMaterial;
                    if (material != null && material.HasProperty(k_IntensityId) && !m_CrystalMaterials.ContainsKey(material))
                        m_CrystalMaterials.Add(material, material.GetFloat(k_IntensityId));
                }

                foreach (var light in crystals.GetComponentsInChildren<Light>())
                {
                    m_CrystalLights.Add(light);
                    m_CrystalLightIntensities.Add(light.intensity);
                }
            }

            var motes = transform.Find(k_Motes);
            if (motes != null && motes.gameObject.activeInHierarchy && motes.TryGetComponent(out m_Motes))
                m_Motes.Play();

            var shafts = transform.Find(k_Shafts);
            if (shafts != null)
            {
                for (var i = 0; i < shafts.childCount; i++)
                {
                    var anchor = shafts.GetChild(i);
                    var renderer = anchor.gameObject.activeInHierarchy ? anchor.GetComponentInChildren<MeshRenderer>() : null;
                    if (renderer != null)
                        m_Shafts.Add(new Shaft { Anchor = anchor, Beam = renderer.transform, Renderer = renderer });
                }
            }

            var falls = transform.Find(k_Falls);
            if (falls != null)
            {
                foreach (var system in falls.GetComponentsInChildren<ParticleSystem>())
                {
                    m_Mists.Add(system);
                    m_MistAlphas.Add(system.main.startColor.color.a);
                    system.Play();
                }

                var rainbow = falls.Find(k_Rainbow);
                if (rainbow != null && rainbow.gameObject.activeInHierarchy && rainbow.TryGetComponent(out m_Rainbow))
                {
                    var material = m_Rainbow.sharedMaterial;
                    if (material != null && material.HasProperty(k_IntensityId))
                        m_RainbowIntensity = material.GetFloat(k_IntensityId);
                }
            }

            m_Bound = true;
        }

        void OnDestroy()
        {
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

            var lamps = SkyController.Lamps;
            var night = Mathf.SmoothStep(0f, 1f, (lamps - 0.3f) / 0.7f);
            var viewer = Viewer();
            var inMap = viewer.y < 150f;

            for (var i = 0; i < m_Fireflies.Count; i++)
            {
                if (m_Fireflies[i] == null)
                    continue;
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
                foreach (var crystal in m_CrystalMaterials)
                    crystal.Key.SetFloat(k_IntensityId, crystal.Value * Mathf.Lerp(0.35f, 1f, lamps));
                for (var i = 0; i < m_CrystalLights.Count; i++)
                {
                    if (m_CrystalLights[i] != null)
                        m_CrystalLights[i].intensity = m_CrystalLightIntensities[i] * night;
                }

                for (var i = 0; i < m_Mists.Count; i++)
                {
                    if (m_Mists[i] == null)
                        continue;
                    var main = m_Mists[i].main;
                    main.startColor = new Color(1f, 1f, 1f, m_MistAlphas[i] * (1f - 0.65f * lamps));
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
                if (EffectBuild.Hash(flower.position) > m_Settings.flowerShare)
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

        // Le soleil bas les fait apparaître ; ils montent vers lui, au moins à 35° (plus bas, on ne les verrait plus).
        void UpdateShafts()
        {
            var sky = SkyController.Current;
            if (sky == null || m_Shafts.Count == 0)
                return;

            var strength = sky.night ? 0f : Mathf.Clamp01(1f - sky.elevation / 40f) * 0.22f;
            var toSun = Quaternion.Euler(-Mathf.Max(35f, sky.elevation), sky.azimuth, 0f) * Vector3.forward;
            var color = sky.lightColor;
            color.a = strength;
            const float length = 16f;
            foreach (var shaft in m_Shafts)
            {
                if (shaft.Renderer == null)
                    continue;

                shaft.Renderer.enabled = strength > 0.005f;
                if (!shaft.Renderer.enabled)
                    continue;

                shaft.Beam.SetPositionAndRotation(shaft.Anchor.position + toSun * (length * 0.5f), Quaternion.FromToRotation(Vector3.up, toSun));
                shaft.Beam.localScale = new Vector3(1.4f, length * 0.5f, 1.4f);
                m_Block.Clear();
                m_Block.SetColor(k_BaseColorId, color);
                shaft.Renderer.SetPropertyBlock(m_Block);
            }
        }

        // Le jour seulement, quand le soleil n'est pas trop bas ; il s'efface quand on le voit de profil.
        void UpdateRainbow()
        {
            if (m_Rainbow == null)
                return;

            var sky = SkyController.Current;
            var strength = sky == null || sky.night ? 0f : Mathf.Clamp01((sky.elevation - 3f) / 15f) * m_RainbowIntensity;
            var facing = m_Rainbow.transform.forward;
            facing.y = 0f;
            var toViewer = Viewer() - m_Rainbow.transform.position;
            toViewer.y = 0f;
            if (toViewer.sqrMagnitude > 1e-4f && facing.sqrMagnitude > 1e-4f)
                strength *= Mathf.SmoothStep(0f, 1f, (Mathf.Abs(Vector3.Dot(toViewer.normalized, facing.normalized)) - 0.2f) / 0.5f);
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
