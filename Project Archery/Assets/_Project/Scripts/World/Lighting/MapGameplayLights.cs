using System.Collections.Generic;
using Archery.Building;
using Archery.Combat;
using Archery.Defense;
using Archery.Enemies;
using Archery.FinalBoss;
using Archery.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.World
{
    /// <summary>
    /// Les lumières qui servent au jeu (groupe « Jeu » de l'objet « Effet ») : l'anneau et le rayon de la tour (vert,
    /// orange puis rouge selon ses PV, ils clignotent quand elle est touchée), les runes des emplacements de construction
    /// libres, les âmes qui s'envolent des ennemis vaincus, une petite lumière douce autour du joueur la nuit ; en jeu,
    /// la lueur des projectiles ennemis la nuit et les remous lumineux de la rivière là où passent les ennemis (et le
    /// joueur).
    /// </summary>
    /// <remarks>
    /// Sous-groupes : « Tour » (« Anneau », « Rayon », « Lumière »), « Runes » (une par emplacement : en jeu, chacune suit
    /// l'emplacement le plus proche), « Âmes », « Lueur du joueur ».
    /// </remarks>
    public class MapGameplayLights : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_RipplesId = Shader.PropertyToID("_ArcheryRipples");
        static readonly int k_TimeId = Shader.PropertyToID("_ArcheryTime");
        static readonly int k_RippleColorId = Shader.PropertyToID("_ArcheryRippleColor");

        const string k_Tower = "Tour";
        const string k_Ring = "Anneau";
        const string k_Beam = "Rayon";
        const string k_TowerLight = "Lumière";
        const string k_Runes = "Runes";
        const string k_Souls = "Âmes";
        const string k_PlayerLight = "Lueur du joueur";

        // Au-dessus, c'est l'arène du boss final : rien de tout ça là-haut.
        const float k_MapTop = 150f;
        const int k_MaxRipples = 8;
        const int k_MaxProjectileLights = 6;

        class Rune
        {
            public BuildSocket Socket;
            public MeshRenderer Renderer;
            public float Radius;
            public float Phase;
            public float Hover;
        }

        readonly List<Rune> m_Runes = new List<Rune>();
        readonly HashSet<BuildSocket> m_RuneSockets = new HashSet<BuildSocket>();
        readonly List<Light> m_ProjectileLights = new List<Light>();
        readonly Vector4[] m_Ripples = new Vector4[k_MaxRipples];
        MapLightingSettings m_Settings;
        MaterialPropertyBlock m_Block;
        bool m_Bound;

        Tower m_Tower;
        Health m_TowerHealth;
        MeshRenderer m_TowerRing;
        MeshRenderer m_TowerBeam;
        Light m_TowerLight;
        float m_TowerLightIntensity;
        float m_TowerTop;
        float m_TowerHit;

        Mesh m_RuneMesh;
        Material m_RuneMaterial;
        float m_NextSocketScan;

        Light m_PlayerLight;
        ParticleSystem m_Souls;

        int m_NextRipple;
        float m_NextRippleCheck;
        Vector3 m_LastPlayerRipple;
        bool m_RipplesDirty;

        // ---------------------------------------------------------------- fabrication (jeu ou éditeur)

        /// <summary>Fabrique l'anneau et le rayon de la tour, les runes des emplacements, les âmes et la lueur du joueur.</summary>
        public void Build(MapLightingSettings settings)
        {
            m_Settings = settings;
            CreateTowerBeacon(EffectBuild.Group(transform, k_Tower));
            var runes = EffectBuild.Group(transform, k_Runes);
            var sockets = Application.isPlaying ? (IEnumerable<BuildSocket>)BuildSocket.All : FindObjectsByType<BuildSocket>();
            foreach (var socket in sockets)
                CreateRune(runes, socket);
            CreateSouls();

            var player = new GameObject(k_PlayerLight).AddComponent<Light>();
            player.transform.SetParent(transform, false);
            player.type = LightType.Point;
            player.shadows = LightShadows.None;
            player.color = settings.playerLightColor;
            player.range = settings.playerLightRange;
            player.intensity = settings.playerLightIntensity;
            player.enabled = false;
        }

        // Un anneau lumineux au pied de la tour, une lumière au sommet et un rayon qui monte vers le ciel, au-dessus de
        // la tête du joueur : de la couleur de ses PV. Le rayon se cache quand le joueur est sur la tour.
        void CreateTowerBeacon(Transform group)
        {
            var tower = Tower.Instance != null ? Tower.Instance : FindAnyObjectByType<Tower>();
            if (tower == null || tower.transform.position.y > k_MapTop)
                return;

            var footprint = new Bounds(tower.transform.position, Vector3.zero);
            foreach (var collider in tower.GetComponentsInChildren<Collider>())
            {
                if (!collider.isTrigger)
                    footprint.Encapsulate(collider.bounds);
            }

            var top = Application.isPlaying ? tower.TopHeight : footprint.max.y;
            var center = new Vector3(footprint.center.x, tower.transform.position.y, footprint.center.z);
            var radius = Mathf.Clamp(Mathf.Max(footprint.extents.x, footprint.extents.z) + 0.8f, 2f, 10f);
            if (MapScan.TryGround(center + Vector3.forward * radius, out var ground, 2f))
                center.y = ground.y;

            if (m_Settings.glowMaterial != null)
            {
                var builder = new MeshBuilder();
                AddRing(builder, Vector3.zero, radius, radius + 0.45f, 64, Color.white);
                AddRing(builder, Vector3.zero, radius + 0.7f, radius + 0.8f, 64, new Color(1f, 1f, 1f, 0.6f));
                var ring = CreateRenderer(k_Ring, group, builder.ToMesh("Anneau de la tour"),
                                          EffectBuild.GlowMaterial(m_Settings.glowMaterial, m_Settings.beaconFull, 1.2f, "Anneau de la tour"));
                ring.transform.position = center + Vector3.up * 0.06f;
            }

            if (m_Settings.beamMaterial != null)
            {
                const float length = 40f;
                var beam = EffectBuild.Cylinder(k_Beam, group);
                beam.transform.position = new Vector3(center.x, top + 3.5f + length * 0.5f, center.z);
                beam.transform.localScale = new Vector3(0.55f, length * 0.5f, 0.55f);
                var renderer = beam.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = m_Settings.beamMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var light = new GameObject(k_TowerLight).AddComponent<Light>();
            light.transform.SetParent(group, false);
            light.transform.position = new Vector3(center.x, top + 2.6f, center.z);
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.range = 12f;
            light.color = m_Settings.beaconFull;
            light.intensity = 1.6f;
        }

        // Un cercle de runes au sol d'un emplacement, de la taille de sa plaque.
        MeshRenderer CreateRune(Transform group, BuildSocket socket)
        {
            if (socket == null || socket.transform.position.y > k_MapTop || m_Settings.glowMaterial == null)
                return null;

            var bounds = new Bounds(socket.transform.position, Vector3.zero);
            foreach (var collider in socket.GetComponents<Collider>())
                bounds.Encapsulate(collider.bounds);

            var radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z), 0.6f, 2.5f);
            var position = socket.transform.position;
            if (MapScan.TryGround(position, out var ground, 0.6f) && Mathf.Abs(ground.y - position.y) < 0.6f)
                position = ground;

            m_RuneMesh ??= BuildRuneMesh();
            m_RuneMaterial ??= EffectBuild.GlowMaterial(m_Settings.glowMaterial, m_Settings.runeColor, 1.2f, "Runes");
            var renderer = CreateRenderer("Runes", group, m_RuneMesh, m_RuneMaterial);
            renderer.transform.position = position + Vector3.up * 0.05f;
            renderer.transform.localScale = Vector3.one * radius;
            return renderer;
        }

        // Les âmes : quelques points lumineux qui montent en tournoyant d'un ennemi vaincu.
        void CreateSouls()
        {
            if (m_Settings.fireMaterial == null)
                return;

            var material = new Material(m_Settings.fireMaterial) { name = "Âmes" };
            if (material.HasProperty(k_BaseColorId))
                material.SetColor(k_BaseColorId, new Color(2.5f, 2.5f, 2.5f, 1f));

            var souls = GlowParticles.Create(k_Souls, transform, Vector3.zero, material);
            var main = souls.main;
            main.maxParticles = 300;
            var emission = souls.emission;
            emission.rateOverTime = 0f;
            var shape = souls.shape;
            shape.enabled = false;
            var noise = souls.noise;
            noise.enabled = true;
            noise.strength = 0.9f;
            noise.frequency = 0.6f;
            var size = souls.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            var color = souls.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
        }

        // Le cercle des runes, de rayon 1 : un anneau, un anneau fin à l'intérieur et six losanges autour.
        internal static Mesh BuildRuneMesh()
        {
            var builder = new MeshBuilder();
            AddRing(builder, Vector3.zero, 0.86f, 1f, 48, Color.white);
            AddRing(builder, Vector3.zero, 0.68f, 0.72f, 48, new Color(1f, 1f, 1f, 0.7f));
            for (var i = 0; i < 6; i++)
            {
                var angle = i * Mathf.PI / 3f;
                var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                var tangent = new Vector3(-radial.z, 0f, radial.x);
                var center = radial * 1.17f;
                builder.Quad(center - radial * 0.11f, center + tangent * 0.06f, center + radial * 0.11f, center - tangent * 0.06f, Color.white,
                             new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            }

            return builder.ToMesh("Runes d'un emplacement");
        }

        // Un anneau à plat (uv.x : 0 au bord intérieur, 1 au bord extérieur, pour la douceur des bords du shader Glow).
        internal static void AddRing(MeshBuilder builder, Vector3 center, float inner, float outer, int segments, Color color)
        {
            for (var s = 0; s < segments; s++)
            {
                var a0 = Mathf.PI * 2f * s / segments;
                var a1 = Mathf.PI * 2f * (s + 1) / segments;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                builder.Quad(center + d0 * inner, center + d0 * outer, center + d1 * outer, center + d1 * inner, color,
                             new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            }
        }

        static MeshRenderer CreateRenderer(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        // ---------------------------------------------------------------- en jeu

        /// <summary>Retrouve les objets posés sous cet objet (ceux qui sont visibles) et commence à les animer.</summary>
        public void Bind(MapLightingSettings settings, MapScan scan)
        {
            m_Settings = settings;
            m_Block = new MaterialPropertyBlock();

            var tower = Find(transform, k_Tower);
            m_Tower = Tower.Instance;
            if (tower != null && m_Tower != null)
            {
                m_TowerHealth = m_Tower.Health;
                if (m_TowerHealth != null)
                    m_TowerHealth.Damaged += OnTowerDamaged;
                m_TowerTop = m_Tower.TopHeight;
                Find(tower, k_Ring)?.TryGetComponent(out m_TowerRing);
                Find(tower, k_Beam)?.TryGetComponent(out m_TowerBeam);
                Find(tower, k_TowerLight)?.TryGetComponent(out m_TowerLight);
                if (m_TowerLight != null)
                    m_TowerLightIntensity = m_TowerLight.intensity;
            }

            // Chaque rune suit l'emplacement le plus proche ; les emplacements qui apparaissent en cours de partie en reçoivent une.
            var runes = Find(transform, k_Runes);
            if (runes != null)
            {
                foreach (var renderer in runes.GetComponentsInChildren<MeshRenderer>())
                {
                    BuildSocket nearest = null;
                    var best = 2.5f * 2.5f;
                    foreach (var socket in BuildSocket.All)
                    {
                        var distance = (socket.transform.position - renderer.transform.position).sqrMagnitude;
                        if (distance >= best || m_RuneSockets.Contains(socket))
                            continue;
                        best = distance;
                        nearest = socket;
                    }

                    if (nearest == null)
                        continue;
                    m_RuneSockets.Add(nearest);
                    m_Runes.Add(new Rune { Socket = nearest, Renderer = renderer, Radius = renderer.transform.localScale.x, Phase = Random.Range(0f, 6.3f) });
                    m_RuneMaterial ??= renderer.sharedMaterial;
                    if (m_RuneMesh == null && renderer.TryGetComponent<MeshFilter>(out var filter))
                        m_RuneMesh = filter.sharedMesh;
                }
            }

            Find(transform, k_PlayerLight)?.TryGetComponent(out m_PlayerLight);
            if (Find(transform, k_Souls) is { } souls && souls.TryGetComponent(out m_Souls))
                m_Souls.Play();

            Shader.SetGlobalVectorArray(k_RipplesId, m_Ripples);
            Shader.SetGlobalColor(k_RippleColorId, settings.rippleColor);

            Enemy.Killed += OnEnemyKilled;
            EnemyProjectile.Launched += OnProjectileLaunched;
            m_Bound = true;
        }

        // Un enfant visible de ce nom (null sinon).
        static Transform Find(Transform parent, string name)
        {
            var child = parent.Find(name);
            return child != null && child.gameObject.activeInHierarchy ? child : null;
        }

        void OnDestroy()
        {
            if (!m_Bound)
                return;

            Enemy.Killed -= OnEnemyKilled;
            EnemyProjectile.Launched -= OnProjectileLaunched;
            if (m_TowerHealth != null)
                m_TowerHealth.Damaged -= OnTowerDamaged;

            foreach (var light in m_ProjectileLights)
            {
                if (light != null)
                    Destroy(light.gameObject);
            }

            System.Array.Clear(m_Ripples, 0, m_Ripples.Length);
            Shader.SetGlobalVectorArray(k_RipplesId, m_Ripples);
        }

        void Update()
        {
            if (!m_Bound)
                return;

            var lamps = SkyController.Lamps;
            var night = Mathf.SmoothStep(0f, 1f, (lamps - 0.3f) / 0.7f);

            UpdateTowerBeacon(lamps);
            UpdateRunes(lamps);
            UpdateProjectileLights(lamps);
            UpdateRipples();
            Shader.SetGlobalFloat(k_TimeId, Time.time);
            Shader.SetGlobalColor(k_RippleColorId, m_Settings.rippleColor);

            if (m_PlayerLight != null)
            {
                var head = PlayerRig.Instance != null ? PlayerRig.Instance.Head : null;
                var on = head != null && head.position.y < k_MapTop && night > 0.01f;
                m_PlayerLight.enabled = on;
                if (on)
                {
                    m_PlayerLight.transform.position = head.position + Vector3.up * 0.3f;
                    m_PlayerLight.intensity = m_Settings.playerLightIntensity * night;
                }
            }
        }

        // ---------------------------------------------------------------- tour

        void OnTowerDamaged(Health health, DamageInfo info) => m_TowerHit = 1f;

        void UpdateTowerBeacon(float lamps)
        {
            if (m_Tower == null)
                return;

            var standing = m_Tower.IsStanding;
            var health = m_TowerHealth != null ? m_TowerHealth.Normalized : 1f;
            var color = health > 0.5f
                ? Color.Lerp(m_Settings.beaconHalf, m_Settings.beaconFull, (health - 0.5f) * 2f)
                : Color.Lerp(m_Settings.beaconLow, m_Settings.beaconHalf, health * 2f);

            // Plus la tour est abîmée, plus l'anneau bat vite ; un coup reçu le fait clignoter en rouge.
            m_TowerHit = Mathf.MoveTowards(m_TowerHit, 0f, Time.deltaTime * 2.5f);
            var speed = Mathf.Lerp(7f, 1.5f, health);
            var pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * speed);
            color = Color.Lerp(color, m_Settings.beaconLow, m_TowerHit);
            var strength = pulse * (1f + m_TowerHit * 1.5f);

            if (m_TowerRing != null)
            {
                m_TowerRing.enabled = standing;
                m_Block.Clear();
                m_Block.SetColor(k_ColorId, color);
                m_Block.SetFloat(k_IntensityId, strength * Mathf.Lerp(0.7f, 1.8f, lamps));
                m_TowerRing.SetPropertyBlock(m_Block);
            }

            if (m_TowerBeam != null)
            {
                var rig = PlayerRig.Instance;
                var onTower = rig != null && rig.BodyPosition.y > m_TowerTop - 1.5f && rig.BodyPosition.y < k_MapTop;
                m_TowerBeam.enabled = standing && !onTower;
                var beamColor = color;
                beamColor.a = Mathf.Lerp(0.18f, 0.55f, lamps) * strength;
                m_Block.Clear();
                m_Block.SetColor(k_BaseColorId, beamColor);
                m_TowerBeam.SetPropertyBlock(m_Block);
            }

            if (m_TowerLight != null)
            {
                m_TowerLight.enabled = standing;
                m_TowerLight.color = color;
                m_TowerLight.intensity = m_TowerLightIntensity * Mathf.Lerp(0.25f, 1f, lamps) * strength;
            }
        }

        // ---------------------------------------------------------------- emplacements de construction

        // Un cercle de runes au sol des emplacements libres : il tourne doucement et respire ; il disparaît quand on y
        // construit. Les emplacements qui apparaissent en cours de partie en reçoivent un (toutes les 0,5 s). Depuis la refonte du
        // 27 octobre (étape 3), ceux de la grille de construction ne se montrent que pendant la pose d'un plan, autour du joueur,
        // là où le bâtiment peut aller, colorés selon leur sorte (BuildSocket.ShowsRune, RuneColor) ; celui que vise l'hologramme
        // brille plus (IsHighlighted). Leur rune n'est créée qu'à ce moment-là, pas des centaines d'un coup.
        void UpdateRunes(float lamps)
        {
            if (Time.time >= m_NextSocketScan)
            {
                m_NextSocketScan = Time.time + 0.5f;
                var runes = Find(transform, k_Runes);
                if (runes != null)
                {
                    foreach (var socket in BuildSocket.All)
                    {
                        if (m_RuneSockets.Contains(socket) || !socket.ShowsRune)
                            continue;
                        m_RuneSockets.Add(socket);
                        var renderer = CreateRune(runes, socket);
                        if (renderer != null)
                            m_Runes.Add(new Rune { Socket = socket, Renderer = renderer, Radius = renderer.transform.localScale.x, Phase = Random.Range(0f, 6.3f) });
                    }
                }
            }

            var spin = Quaternion.Euler(0f, 12f * Time.deltaTime, 0f);
            foreach (var rune in m_Runes)
            {
                if (rune.Renderer == null)
                    continue;

                var free = rune.Socket != null && rune.Socket.isActiveAndEnabled && rune.Socket.Structure == null && rune.Socket.ShowsRune;
                rune.Renderer.enabled = free;
                if (!free)
                    continue;

                // Visées par le rayon de la main, elles grossissent un peu et brillent plus (comme les anciens repères).
                rune.Hover = Mathf.Lerp(rune.Hover, rune.Socket.IsHighlighted ? 1f : 0f, 1f - Mathf.Exp(-10f * Time.deltaTime));
                var runeTransform = rune.Renderer.transform;
                runeTransform.rotation = spin * runeTransform.rotation;
                runeTransform.localScale = Vector3.one * (rune.Radius * (1f + 0.12f * rune.Hover));
                var breath = 0.75f + 0.25f * Mathf.Sin(Time.time * 1.6f + rune.Phase);
                m_Block.Clear();
                m_Block.SetColor(k_ColorId, rune.Socket.RuneColor ?? EffectBuild.GlowColor(rune.Renderer));
                m_Block.SetFloat(k_IntensityId, Mathf.Lerp(0.6f, 1.7f, lamps) * Mathf.Lerp(breath, 1.8f, rune.Hover));
                rune.Renderer.SetPropertyBlock(m_Block);
            }
        }

        // ---------------------------------------------------------------- ennemis

        void OnEnemyKilled(Enemy enemy, DamageInfo info)
        {
            if (m_Souls == null || enemy == null)
                return;

            var position = enemy.transform.position + Vector3.up;
            if (position.y > k_MapTop)
                return;

            var count = enemy.GetComponent<Boss>() != null ? 40 : 8;
            var emit = new ParticleSystem.EmitParams { startColor = m_Settings.soulColor };
            for (var i = 0; i < count; i++)
            {
                var spread = Random.insideUnitSphere * 0.4f;
                emit.position = position + spread;
                emit.velocity = new Vector3(spread.x * 1.5f, Random.Range(1.4f, 3f), spread.z * 1.5f);
                emit.startLifetime = Random.Range(1.2f, 2.2f);
                emit.startSize = Random.Range(0.08f, 0.18f);
                m_Souls.Emit(emit, 1);
            }
        }

        // La nuit, les projectiles ennemis éclairent le sol en passant (quelques-uns à la fois : ça coûte cher en VR).
        void OnProjectileLaunched(EnemyProjectile projectile)
        {
            if (projectile == null || SkyController.Lamps < 0.3f || projectile.transform.position.y > k_MapTop)
                return;

            m_ProjectileLights.RemoveAll(light => light == null);
            if (m_ProjectileLights.Count >= k_MaxProjectileLights)
                return;

            var light = new GameObject("Lueur du projectile").AddComponent<Light>();
            light.transform.SetParent(projectile.transform, false);
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.range = m_Settings.projectileLightRange;
            light.color = ProjectileColor(projectile, new Color(1f, 0.45f, 0.85f));
            m_ProjectileLights.Add(light);
        }

        void UpdateProjectileLights(float lamps)
        {
            for (var i = 0; i < m_ProjectileLights.Count; i++)
            {
                var light = m_ProjectileLights[i];
                if (light != null)
                    light.intensity = 1.6f * lamps * (0.85f + 0.15f * Mathf.Sin(Time.time * 18f + i * 2.1f));
            }
        }

        static Color ProjectileColor(EnemyProjectile projectile, Color fallback)
        {
            foreach (var renderer in projectile.GetComponentsInChildren<Renderer>())
            {
                var material = renderer.sharedMaterial;
                if (material == null)
                    continue;

                foreach (var id in new[] { k_EmissionColorId, k_BaseColorId, k_ColorId })
                {
                    if (!material.HasProperty(id))
                        continue;

                    var color = material.GetColor(id);
                    if (color.maxColorComponent <= 0.05f)
                        continue;

                    color /= color.maxColorComponent;
                    color.a = 1f;
                    return color;
                }
            }

            return fallback;
        }

        // ---------------------------------------------------------------- remous

        // Les ennemis (et le joueur) qui marchent dans l'eau y laissent des anneaux, lus par le shader de l'eau.
        void UpdateRipples()
        {
            if (Time.time >= m_NextRippleCheck)
            {
                m_NextRippleCheck = Time.time + 0.45f;
                foreach (var enemy in Enemy.Alive)
                {
                    if (enemy == null || !enemy.IsAlive)
                        continue;

                    var position = enemy.transform.position;
                    if (position.y < k_MapTop && SlowWater.InAnyWater(position + Vector3.up * 0.3f))
                        AddRipple(position, 1f);
                }

                var rig = PlayerRig.Instance;
                if (rig != null)
                {
                    var body = rig.BodyPosition;
                    if (body.y < k_MapTop && (body - m_LastPlayerRipple).sqrMagnitude > 0.25f && SlowWater.InAnyWater(body + Vector3.up * 0.3f))
                    {
                        m_LastPlayerRipple = body;
                        AddRipple(body, 0.7f);
                    }
                }
            }

            if (m_RipplesDirty)
            {
                m_RipplesDirty = false;
                Shader.SetGlobalVectorArray(k_RipplesId, m_Ripples);
            }
        }

        void AddRipple(Vector3 position, float strength)
        {
            m_Ripples[m_NextRipple] = new Vector4(position.x, position.z, Time.time, strength);
            m_NextRipple = (m_NextRipple + 1) % k_MaxRipples;
            m_RipplesDirty = true;
        }
    }
}
