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
    /// Les lumières qui servent au jeu : l'anneau et le rayon de la tour (vert, orange puis rouge selon ses PV, ils
    /// clignotent quand elle est touchée), les runes des emplacements de construction libres, les âmes qui s'envolent
    /// des ennemis vaincus, la lueur des projectiles ennemis la nuit, une petite lumière douce autour du joueur la nuit,
    /// et les remous lumineux de la rivière là où passent les ennemis (et le joueur).
    /// </summary>
    public class MapGameplayLights : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_RipplesId = Shader.PropertyToID("_ArcheryRipples");
        static readonly int k_TimeId = Shader.PropertyToID("_ArcheryTime");
        static readonly int k_RippleColorId = Shader.PropertyToID("_ArcheryRippleColor");

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
        readonly List<Object> m_Owned = new List<Object>();
        MapLightingSettings m_Settings;
        MaterialPropertyBlock m_Block;

        Tower m_Tower;
        Health m_TowerHealth;
        MeshRenderer m_TowerRing;
        MeshRenderer m_TowerBeam;
        Light m_TowerLight;
        float m_TowerTop;
        float m_TowerHit;

        Mesh m_RuneMesh;
        float m_NextSocketScan;

        Light m_PlayerLight;
        ParticleSystem m_Souls;

        int m_NextRipple;
        float m_NextRippleCheck;
        Vector3 m_LastPlayerRipple;
        bool m_RipplesDirty;

        public void Init(MapLightingSettings settings, MapScan scan)
        {
            m_Settings = settings;
            m_Block = new MaterialPropertyBlock();

            CreateTowerBeacon();
            m_RuneMesh = BuildRuneMesh();
            m_Owned.Add(m_RuneMesh);
            ScanSockets();
            CreatePlayerLight();
            CreateSouls();

            Shader.SetGlobalVectorArray(k_RipplesId, m_Ripples);
            Shader.SetGlobalColor(k_RippleColorId, settings.rippleColor);

            Enemy.Killed += OnEnemyKilled;
            EnemyProjectile.Launched += OnProjectileLaunched;
        }

        void OnDestroy()
        {
            Enemy.Killed -= OnEnemyKilled;
            EnemyProjectile.Launched -= OnProjectileLaunched;
            if (m_TowerHealth != null)
                m_TowerHealth.Damaged -= OnTowerDamaged;

            foreach (var light in m_ProjectileLights)
            {
                if (light != null)
                    Destroy(light.gameObject);
            }

            foreach (var owned in m_Owned)
            {
                if (owned != null)
                    Destroy(owned);
            }

            System.Array.Clear(m_Ripples, 0, m_Ripples.Length);
            Shader.SetGlobalVectorArray(k_RipplesId, m_Ripples);
        }

        void Update()
        {
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
                    m_PlayerLight.range = m_Settings.playerLightRange;
                    m_PlayerLight.color = m_Settings.playerLightColor;
                }
            }
        }

        // ---------------------------------------------------------------- tour

        // Un anneau lumineux au pied de la tour, une lumière au sommet et un rayon qui monte vers le ciel, au-dessus de
        // la tête du joueur : de la couleur de ses PV. Le rayon se cache quand le joueur est sur la tour.
        void CreateTowerBeacon()
        {
            m_Tower = Tower.Instance != null ? Tower.Instance : FindAnyObjectByType<Tower>();
            if (m_Tower == null || m_Tower.transform.position.y > k_MapTop)
                return;

            m_TowerHealth = m_Tower.Health;
            if (m_TowerHealth != null)
                m_TowerHealth.Damaged += OnTowerDamaged;

            var footprint = new Bounds(m_Tower.transform.position, Vector3.zero);
            foreach (var collider in m_Tower.GetComponentsInChildren<Collider>())
            {
                if (!collider.isTrigger)
                    footprint.Encapsulate(collider.bounds);
            }

            m_TowerTop = m_Tower.TopHeight;
            var center = new Vector3(footprint.center.x, m_Tower.transform.position.y, footprint.center.z);
            var radius = Mathf.Clamp(Mathf.Max(footprint.extents.x, footprint.extents.z) + 0.8f, 2f, 10f);
            if (MapScan.TryGround(center + Vector3.forward * radius, out var ground, 2f))
                center.y = ground.y;

            if (m_Settings.glowMaterial != null)
            {
                var builder = new MeshBuilder();
                AddRing(builder, Vector3.zero, radius, radius + 0.45f, 64, Color.white);
                AddRing(builder, Vector3.zero, radius + 0.7f, radius + 0.8f, 64, new Color(1f, 1f, 1f, 0.6f));
                var mesh = builder.ToMesh("Anneau de la tour");
                m_Owned.Add(mesh);
                m_TowerRing = CreateRenderer("Anneau de la tour", center + Vector3.up * 0.06f, mesh, m_Settings.glowMaterial);
            }

            if (m_Settings.beamMaterial != null)
            {
                const float length = 40f;
                var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                beam.name = "Rayon de la tour";
                Destroy(beam.GetComponent<Collider>());
                beam.transform.SetParent(transform, false);
                beam.transform.position = new Vector3(center.x, m_TowerTop + 3.5f + length * 0.5f, center.z);
                beam.transform.localScale = new Vector3(0.55f, length * 0.5f, 0.55f);
                m_TowerBeam = beam.GetComponent<MeshRenderer>();
                m_TowerBeam.sharedMaterial = m_Settings.beamMaterial;
                m_TowerBeam.shadowCastingMode = ShadowCastingMode.Off;
                m_TowerBeam.receiveShadows = false;
            }

            m_TowerLight = new GameObject("Lumière de la tour").AddComponent<Light>();
            m_TowerLight.transform.SetParent(transform, false);
            m_TowerLight.transform.position = new Vector3(center.x, m_TowerTop + 2.6f, center.z);
            m_TowerLight.type = LightType.Point;
            m_TowerLight.shadows = LightShadows.None;
            m_TowerLight.range = 12f;
        }

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
                m_TowerLight.intensity = Mathf.Lerp(0.4f, 1.6f, lamps) * strength;
            }
        }

        // ---------------------------------------------------------------- emplacements de construction

        // Un cercle de runes au sol des emplacements libres : il tourne doucement et respire ; il disparaît quand on
        // y construit. Les emplacements qui apparaissent en cours de partie sont pris toutes les 2 s.
        void ScanSockets()
        {
            if (m_Settings.glowMaterial == null)
                return;

            foreach (var socket in BuildSocket.All)
            {
                if (socket == null || m_RuneSockets.Contains(socket) || socket.transform.position.y > k_MapTop)
                    continue;

                m_RuneSockets.Add(socket);
                var bounds = new Bounds(socket.transform.position, Vector3.zero);
                foreach (var collider in socket.colliders)
                {
                    if (collider != null)
                        bounds.Encapsulate(collider.bounds);
                }

                var radius = Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z), 0.6f, 2.5f);
                var position = socket.transform.position;
                if (MapScan.TryGround(position, out var ground, 0.6f) && Mathf.Abs(ground.y - position.y) < 0.6f)
                    position = ground;

                var renderer = CreateRenderer("Runes", position + Vector3.up * 0.05f, m_RuneMesh, m_Settings.glowMaterial);
                renderer.transform.localScale = Vector3.one * radius;
                m_Runes.Add(new Rune { Socket = socket, Renderer = renderer, Radius = radius, Phase = Random.Range(0f, 6.3f) });
            }
        }

        void UpdateRunes(float lamps)
        {
            if (Time.time >= m_NextSocketScan)
            {
                m_NextSocketScan = Time.time + 2f;
                ScanSockets();
            }

            var spin = Quaternion.Euler(0f, 12f * Time.deltaTime, 0f);
            foreach (var rune in m_Runes)
            {
                var free = rune.Socket != null && rune.Socket.isActiveAndEnabled && rune.Socket.Structure == null;
                rune.Renderer.enabled = free;
                if (!free)
                    continue;

                // Visées par le rayon de la main, elles grossissent un peu et brillent plus (comme les anciens repères).
                rune.Hover = Mathf.Lerp(rune.Hover, rune.Socket.isHovered ? 1f : 0f, 1f - Mathf.Exp(-10f * Time.deltaTime));
                var runeTransform = rune.Renderer.transform;
                runeTransform.rotation = spin * runeTransform.rotation;
                runeTransform.localScale = Vector3.one * (rune.Radius * (1f + 0.12f * rune.Hover));
                var breath = 0.75f + 0.25f * Mathf.Sin(Time.time * 1.6f + rune.Phase);
                m_Block.Clear();
                m_Block.SetColor(k_ColorId, m_Settings.runeColor);
                m_Block.SetFloat(k_IntensityId, Mathf.Lerp(0.6f, 1.7f, lamps) * Mathf.Lerp(breath, 1.8f, rune.Hover));
                rune.Renderer.SetPropertyBlock(m_Block);
            }
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

        MeshRenderer CreateRenderer(string name, Vector3 position, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = position;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        // ---------------------------------------------------------------- joueur et ennemis

        void CreatePlayerLight()
        {
            m_PlayerLight = new GameObject("Lueur du joueur").AddComponent<Light>();
            m_PlayerLight.transform.SetParent(transform, false);
            m_PlayerLight.type = LightType.Point;
            m_PlayerLight.shadows = LightShadows.None;
            m_PlayerLight.enabled = false;
        }

        // Les âmes : quelques points lumineux qui montent en tournoyant d'un ennemi vaincu.
        void CreateSouls()
        {
            if (m_Settings.fireMaterial == null)
                return;

            var material = new Material(m_Settings.fireMaterial) { name = "Âmes (en jeu)" };
            if (material.HasProperty(k_BaseColorId))
                material.SetColor(k_BaseColorId, new Color(2.5f, 2.5f, 2.5f, 1f));
            m_Owned.Add(material);

            m_Souls = GlowParticles.Create("Âmes", transform, Vector3.zero, material);
            var main = m_Souls.main;
            main.maxParticles = 300;
            var emission = m_Souls.emission;
            emission.rateOverTime = 0f;
            var shape = m_Souls.shape;
            shape.enabled = false;
            var noise = m_Souls.noise;
            noise.enabled = true;
            noise.strength = 0.9f;
            noise.frequency = 0.6f;
            var size = m_Souls.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            var color = m_Souls.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(gradient);
            m_Souls.Play();
        }

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
