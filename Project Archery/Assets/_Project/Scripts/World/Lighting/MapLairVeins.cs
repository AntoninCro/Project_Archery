using System.Collections.Generic;
using Archery.FinalBoss;
using Archery.Waves;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.World
{
    /// <summary>
    /// Des veines de lave dans la face de la montagne, autour de la porte de l'antre (<see cref="LairGate"/>), groupe
    /// « Antre » de l'objet « Effet » : une seule, faible, au début de la partie ; à chaque vague, d'autres s'allument
    /// (dans l'ordre des objets du groupe : de la plus proche de la porte à la plus lointaine), et toutes battent comme
    /// un cœur, de plus en plus vite. À la vague du boss final, elles sont toutes au plus fort.
    /// </summary>
    public class MapLairVeins : MonoBehaviour
    {
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");

        const float k_MapTop = 150f;
        const string k_LightName = "Lueur de l'antre";

        /// <summary>
        /// Version de la fabrication : l'éditeur refait les veines enregistrées avec une version plus ancienne
        /// (1 : porche en arche, plus bas, et titre gravé au-dessus, que les veines évitent ; 2 : le titre est dans un
        /// cartouche de pierre, plus large ; 3 : la porte remplit l'arche, le porche est plus bas).
        /// </summary>
        public const int CurrentVersion = 3;

        [SerializeField]
        [HideInInspector]
        int m_Version;

        readonly List<MeshRenderer> m_Veins = new List<MeshRenderer>();
        readonly HashSet<Collider> m_GateColliders = new HashSet<Collider>();
        MapLightingSettings m_Settings;
        MaterialPropertyBlock m_Block;
        Transform m_Gate;
        Rect m_TitleArea;
        Light m_Light;
        float m_LightIntensity = 1f;
        float m_Shown;
        bool m_Bound;

        /// <summary>Faux si ces veines ont été fabriquées pour l'ancienne porte (à refaire dans l'éditeur).</summary>
        public bool IsCurrent => m_Version >= CurrentVersion;

        /// <summary>Fabrique les veines (sous cet objet) sur la roche autour de la porte de l'antre, et leur lumière.</summary>
        public void Build(MapLightingSettings settings)
        {
            m_Settings = settings;
            m_Version = CurrentVersion;
            LairGate gate = null;
            foreach (var candidate in FindObjectsByType<LairGate>())
            {
                if (candidate.transform.position.y < k_MapTop)
                    gate = candidate;
            }

            if (gate == null || settings.glowMaterial == null || settings.veins <= 0)
                return;

            m_Gate = gate.transform;
            foreach (var collider in gate.GetComponentsInChildren<Collider>(true))
                m_GateColliders.Add(collider);

            // Le titre gravé au-dessus du porche, et ses fissures : les veines n'y passent pas.
            var title = gate.TitleArea;
            m_TitleArea = title.width > 0f ? Rect.MinMaxRect(title.xMin - 0.4f, title.yMin - 0.4f, title.xMax + 0.4f, title.yMax + 0.4f) : Rect.zero;

            BuildVeins(gate.PorchHalfWidth, gate.PorchTop);

            var light = new GameObject(k_LightName).AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.transform.position = m_Gate.TransformPoint(0f, 3f, -4f);
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.color = settings.veinColor;
            light.range = 12f;
            light.intensity = 1f;
        }

        /// <summary>Retrouve les veines posées sous cet objet (celles qui sont visibles) et commence à les animer.</summary>
        public void Bind(MapLightingSettings settings)
        {
            m_Settings = settings;
            m_Block = new MaterialPropertyBlock();
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                    continue;
                if (child.TryGetComponent<MeshRenderer>(out var vein))
                    m_Veins.Add(vein);
                else if (child.TryGetComponent(out m_Light))
                    m_LightIntensity = m_Light.intensity;
            }

            m_Bound = true;
        }

        void Update()
        {
            if (!m_Bound || m_Veins.Count == 0)
                return;

            // Avancement de la partie : 0 avant la première vague, 1 à la dernière (et pendant le boss final).
            var waves = WaveManager.Instance;
            var progress = 0f;
            var finalBoss = false;
            if (waves != null)
            {
                finalBoss = waves.IsFinalBossWave;
                progress = finalBoss ? 1f : Mathf.Clamp01(waves.WaveNumber / (float)Mathf.Max(1, waves.WavesToWin));
            }

            var target = Mathf.Max(1f, progress * m_Veins.Count);
            m_Shown = Mathf.MoveTowards(m_Shown, target, Time.deltaTime * 0.6f);

            // Un battement de cœur (deux coups rapprochés), plus rapide quand la partie avance.
            var period = Mathf.Lerp(1.7f, finalBoss ? 0.6f : 0.85f, progress);
            var t = Mathf.Repeat(Time.time, period) / period;
            var beat = Gauss(t, 0.05f, 0.05f) + 0.6f * Gauss(t, 0.24f, 0.05f);
            var lamps = SkyController.Lamps;
            var intensity = (0.5f + 1.5f * progress) * Mathf.Lerp(0.6f, 1.4f, lamps) * (0.65f + 0.6f * beat);

            for (var i = 0; i < m_Veins.Count; i++)
            {
                var vein = m_Veins[i];
                if (vein == null)
                    continue;

                var fade = Mathf.Clamp01(m_Shown - i);
                vein.enabled = fade > 0.01f;
                if (!vein.enabled)
                    continue;

                m_Block.Clear();
                m_Block.SetColor(k_ColorId, EffectBuild.GlowColor(vein));
                m_Block.SetFloat(k_IntensityId, intensity * fade);
                vein.SetPropertyBlock(m_Block);
            }

            if (m_Light != null)
            {
                m_Light.intensity = m_LightIntensity * (0.2f + 1.4f * progress) * Mathf.Lerp(0.3f, 1f, lamps) * (0.6f + 0.5f * beat);
                m_Light.enabled = m_Light.intensity > 0.02f;
            }
        }

        static float Gauss(float x, float center, float width)
        {
            var d = (x - center) / width;
            return Mathf.Exp(-d * d);
        }

        // ---------------------------------------------------------------- fabrication

        // Les veines partent de points pris autour du porche (sur les côtés et au-dessus), posés sur la roche, et
        // serpentent en s'éloignant de la porte, sans entrer dans le titre. Elles sont rangées de la plus proche à la plus
        // lointaine : c'est l'ordre dans lequel elles s'allument.
        void BuildVeins(float halfWidth, float top)
        {
            var random = new System.Random(83);
            var veins = new List<(float Distance, Mesh Mesh)>();
            for (var attempt = 0; attempt < m_Settings.veins * 4 && veins.Count < m_Settings.veins; attempt++)
            {
                var side = attempt % 2 == 0 ? -1f : 1f;
                Vector3 start;
                Vector2 heading;
                if (attempt % 5 == 4)
                {
                    // Au-dessus du porche, sous le titre : elles partent sur les côtés.
                    var ceiling = m_TitleArea.width > 0f ? m_TitleArea.yMin : top + 3f;
                    start = new Vector3(Range(random, -halfWidth, halfWidth), top + Range(random, 0.3f, Mathf.Max(0.4f, ceiling - top)), -3f);
                    heading = new Vector2(start.x < 0f ? -1f : 1f, Range(random, -0.2f, 0.5f));
                }
                else
                {
                    start = new Vector3(side * (halfWidth + Range(random, 0.2f, 3.5f)), Range(random, 0.4f, top), -3f);
                    heading = new Vector2(side, Range(random, -0.3f, 0.9f));
                }

                if (!TryFace(m_Gate.TransformPoint(start), m_Gate.forward, 18f, out var point, out var normal) || InTitle(point))
                    continue;

                var builder = new MeshBuilder();
                var length = Grow(builder, point, normal, heading.normalized, 7 + random.Next(5), 0.13f, random);
                if (length < 1.5f)
                    continue;

                // Une branche, une fois sur deux.
                if (random.NextDouble() < 0.5)
                {
                    var branchHeading = new Vector2(heading.x + Range(random, -0.8f, 0.8f), heading.y + Range(random, 0.2f, 0.9f)).normalized;
                    Grow(builder, point + Vector3.up * 0.05f, normal, branchHeading, 3 + random.Next(3), 0.08f, random);
                }

                var mesh = builder.ToMesh("Veine de l'antre");
                var local = m_Gate.InverseTransformPoint(point);
                var distance = new Vector2(local.x, local.y - top * 0.5f).magnitude;
                veins.Add((distance, mesh));
            }

            veins.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            var material = EffectBuild.GlowMaterial(m_Settings.glowMaterial, m_Settings.veinColor, 1.5f, "Veines de l'antre");
            foreach (var vein in veins)
            {
                var go = new GameObject("Veine de l'antre");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = vein.Mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        // Une veine qui serpente sur la roche, en suivant sa surface pas à pas ; plus fine au bout. Rend sa longueur.
        float Grow(MeshBuilder builder, Vector3 point, Vector3 normal, Vector2 heading, int steps, float width, System.Random random)
        {
            var points = new List<Vector3> { point };
            var normals = new List<Vector3> { normal };
            var length = 0f;
            for (var s = 0; s < steps; s++)
            {
                var angle = Range(random, -35f, 35f) * Mathf.Deg2Rad;
                heading = new Vector2(heading.x * Mathf.Cos(angle) - heading.y * Mathf.Sin(angle), heading.x * Mathf.Sin(angle) + heading.y * Mathf.Cos(angle));
                var along = m_Gate.right * heading.x + Vector3.up * heading.y;
                along -= normal * Vector3.Dot(along, normal);
                if (along.sqrMagnitude < 1e-4f)
                    break;

                var step = Range(random, 0.45f, 0.85f);
                var candidate = point + along.normalized * step;
                if (!TryFace(candidate + normal * 1.2f, -normal, 2.6f, out var next, out var nextNormal) || InTitle(next))
                    break;

                length += Vector3.Distance(point, next);
                point = next;
                normal = nextNormal;
                points.Add(point);
                normals.Add(normal);
            }

            if (points.Count < 2)
                return 0f;

            // Une bande le long de la ligne : chaque point a son propre écart sur les côtés, les morceaux se raccordent.
            var sides = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                var direction = points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)];
                var taper = Mathf.Lerp(1f, 0.25f, i / (float)(points.Count - 1));
                sides[i] = Vector3.Cross(direction, normals[i]).normalized * (width * taper);
            }

            for (var i = 0; i < points.Count - 1; i++)
            {
                var a = points[i] + normals[i] * 0.04f;
                var b = points[i + 1] + normals[i + 1] * 0.04f;
                var color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.45f, i / (float)(points.Count - 1)));
                builder.Quad(a - sides[i], a + sides[i], b + sides[i + 1], b - sides[i + 1], color,
                             new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            }

            return length;
        }

        bool InTitle(Vector3 point)
        {
            if (m_TitleArea.width <= 0f)
                return false;
            var local = m_Gate.InverseTransformPoint(point);
            return m_TitleArea.Contains(new Vector2(local.x, local.y));
        }

        // La roche devant ce rayon : le premier collider qui n'est ni la porte ni une plante. Faux s'il n'y en a pas, ou
        // s'il ne fait pas face au rayon (sol, dessus d'un rocher…).
        bool TryFace(Vector3 origin, Vector3 direction, float distance, out Vector3 point, out Vector3 normal)
        {
            point = origin;
            normal = -direction;
            var hits = Physics.RaycastAll(origin, direction, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.distance >= best || m_GateColliders.Contains(hit.collider) || MapScan.IsPlant(hit.collider.transform))
                    continue;

                best = hit.distance;
                point = hit.point;
                normal = hit.normal;
            }

            return best < float.MaxValue && Mathf.Abs(normal.y) <= 0.75f && Vector3.Dot(normal, direction) <= -0.3f;
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
