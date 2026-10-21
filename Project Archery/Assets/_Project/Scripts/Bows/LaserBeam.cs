using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Bows
{
    /// <summary>
    /// Le rayon du laser de l'arc cosmique (surcharge), en couches bien séparées : un cœur blanc, un rayon bleu autour, un
    /// large halo violet, et deux spirales, une bleue et une violette, enroulées l'une dans l'autre, qui tournent et
    /// s'écartent en s'éteignant (0,55 s). Puis une traînée de particules bleues et violettes qui reste un peu le long du
    /// passage et s'éparpille en flottant ; un éclat au départ. Tout est additif (shader « Archery/LaserGlow », matériau
    /// <c>Resources/LaserGlow</c>) : le bloom le fait rayonner. Près de l'arc, le rayon est plus fin : pas de flash sur toute
    /// la vue (confort en VR).
    /// </summary>
    public class LaserBeam : MonoBehaviour
    {
        const string k_MaterialResource = "LaserGlow";
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_RoundId = Shader.PropertyToID("_Round");

        // Le rayon : durée (s), largeurs (m) au départ, et éclat (HDR) de chaque couche.
        const float k_BeamTime = 0.55f;
        const float k_CoreWidth = 0.07f;
        const float k_InnerWidth = 0.22f;
        const float k_HaloWidth = 0.6f;
        const float k_CoreGlow = 7f;
        const float k_InnerGlow = 6f;
        const float k_HaloGlow = 4.5f;

        // Près de l'arc, sur cette distance (m), le rayon et les spirales s'affinent jusqu'à cette part de leur largeur.
        const float k_NearDistance = 2.5f;
        const float k_NearWidth = 0.3f;

        // Les spirales : rayon (m) de l'enroulement, pas (m par tour), largeur (m), vitesse (rad/s), éclat, points par tour.
        const float k_SpiralRadius = 0.16f;
        const float k_SpiralPitch = 2f;
        const float k_SpiralWidth = 0.06f;
        const float k_SpiralSpin = 18f;
        const float k_SpiralGlow = 7f;
        const int k_SpiralPointsPerTurn = 10;
        const int k_MaxSpiralPoints = 800;

        // La traînée : particules par mètre (bornées), durée de vie (s), éclat.
        const float k_ParticlesPerMeter = 7f;
        const int k_MinParticles = 40;
        const int k_MaxParticles = 840;
        const float k_Lifetime = 2.4f;
        const float k_ParticleGlow = 3.5f;

        static Material s_Material;

        readonly List<Material> m_Materials = new List<Material>();
        readonly LineRenderer[] m_Spirals = new LineRenderer[2];
        LineRenderer m_Core;
        LineRenderer m_Inner;
        LineRenderer m_Halo;
        Vector3[] m_SpiralPoints;
        Vector3 m_Start;
        Vector3 m_Direction;
        Vector3 m_Side;
        Vector3 m_Up;
        float m_Length;
        float m_Age;

        /// <summary>
        /// Tire le rayon de <paramref name="start"/> à <paramref name="end"/> : le halo et une spirale de la première
        /// couleur (violet), le rayon autour du cœur et l'autre spirale de la deuxième (bleu) ; les particules des deux.
        /// </summary>
        public static void Fire(Vector3 start, Vector3 end, Color color, Color secondColor)
        {
            if (s_Material == null)
                s_Material = Resources.Load<Material>(k_MaterialResource);
            if (s_Material == null)
            {
                Debug.LogWarning("LaserBeam : matériau Resources/LaserGlow introuvable, pas de rayon.");
                return;
            }

            var beam = new GameObject("Laser").AddComponent<LaserBeam>();
            beam.Build(start, end, color, secondColor);
        }

        void Build(Vector3 start, Vector3 end, Color color, Color secondColor)
        {
            transform.position = start;
            m_Start = start;
            m_Length = Vector3.Distance(start, end);
            m_Direction = m_Length > 1e-4f ? (end - start) / m_Length : Vector3.forward;
            m_Side = Vector3.Cross(m_Direction, Vector3.up);
            if (m_Side.sqrMagnitude < 1e-4f)
                m_Side = Vector3.Cross(m_Direction, Vector3.right);
            m_Side.Normalize();
            m_Up = Vector3.Cross(m_Side, m_Direction);

            var near = new AnimationCurve(new Keyframe(0f, k_NearWidth),
                                          new Keyframe(Mathf.Clamp(k_NearDistance / Mathf.Max(m_Length, 0.01f), 0.001f, 0.5f), 1f),
                                          new Keyframe(1f, 1f));
            m_Halo = Line("Halo", Instance(color * k_HaloGlow, false), near, 2);
            m_Inner = Line("Rayon", Instance(secondColor * k_InnerGlow, false), near, 2);
            m_Core = Line("Cœur", Instance(Color.white * k_CoreGlow, false), near, 2);
            foreach (var line in new[] { m_Halo, m_Inner, m_Core })
            {
                line.SetPosition(0, start);
                line.SetPosition(1, end);
            }

            var turns = m_Length / k_SpiralPitch;
            m_SpiralPoints = new Vector3[Mathf.Clamp(Mathf.CeilToInt(turns * k_SpiralPointsPerTurn) + 1, 2, k_MaxSpiralPoints)];
            m_Spirals[0] = Line("Spirale violette", Instance(color * k_SpiralGlow, false), near, m_SpiralPoints.Length);
            m_Spirals[1] = Line("Spirale bleue", Instance(secondColor * k_SpiralGlow, false), near, m_SpiralPoints.Length);
            UpdateLines();

            var particles = BuildParticles(Instance(Color.white * k_ParticleGlow, true));
            EmitTrail(particles, start, end, color, secondColor);
            EmitBurst(particles, start, m_Direction, color, secondColor);
        }

        Material Instance(Color color, bool round)
        {
            var material = new Material(s_Material);
            material.SetColor(k_BaseColorId, color);
            material.SetFloat(k_RoundId, round ? 1f : 0f);
            m_Materials.Add(material);
            return material;
        }

        LineRenderer Line(string name, Material material, AnimationCurve widthCurve, int points)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.useWorldSpace = true;
            line.positionCount = points;
            line.widthCurve = widthCurve;
            line.numCapVertices = 4;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = material;
            return line;
        }

        // Les spirales tournent autour du rayon ; elles partent de son axe, devant l'arc.
        void UpdateSpirals(float radius, float phase)
        {
            var last = m_SpiralPoints.Length - 1;
            for (var strand = 0; strand < m_Spirals.Length; strand++)
            {
                var offset = phase + strand * Mathf.PI;
                for (var i = 0; i <= last; i++)
                {
                    var distance = m_Length * i / last;
                    var angle = offset + distance / k_SpiralPitch * 2f * Mathf.PI;
                    var around = radius * Mathf.Clamp01(distance / k_NearDistance);
                    m_SpiralPoints[i] = m_Start + m_Direction * distance +
                                        (m_Side * Mathf.Cos(angle) + m_Up * Mathf.Sin(angle)) * around;
                }

                m_Spirals[strand].SetPositions(m_SpiralPoints);
            }
        }

        ParticleSystem BuildParticles(Material material)
        {
            var particles = new GameObject("Particules").AddComponent<ParticleSystem>();
            particles.transform.SetParent(transform, false);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = k_MaxParticles + 64;
            main.startSpeed = 0f;
            main.gravityModifier = -0.015f;

            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;

            // Elles s'éteignent et rapetissent.
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.75f, 0.35f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;
            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.25f)));

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            particles.Play();
            return particles;
        }

        // Le long du passage : des grains bleus et violets qui restent un instant, puis s'éparpillent en flottant. Plus gros
        // au loin, pour qu'on suive le trajet du rayon jusqu'au bout.
        static void EmitTrail(ParticleSystem particles, Vector3 start, Vector3 end, Color color, Color secondColor)
        {
            var length = Vector3.Distance(start, end);
            var count = Mathf.Clamp(Mathf.RoundToInt(length * k_ParticlesPerMeter), k_MinParticles, k_MaxParticles);
            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (var i = 0; i < count; i++)
            {
                var t = Random.value;
                var distance = t * length;
                emit.position = Vector3.Lerp(start, end, t) + Random.insideUnitSphere * 0.08f;
                emit.velocity = Random.insideUnitSphere * 0.3f + Vector3.up * 0.04f;
                emit.startSize = Random.Range(0.04f, 0.09f) * (1f + distance / 30f);
                emit.startLifetime = Random.Range(0.8f, k_Lifetime);
                var tint = Color.Lerp(i % 2 == 0 ? color : secondColor, Color.white, Random.Range(0f, 0.25f));
                tint.a = Random.Range(0.6f, 1f);
                emit.startColor = tint;
                particles.Emit(emit, 1);
            }
        }

        // L'éclat au départ, devant l'arc.
        static void EmitBurst(ParticleSystem particles, Vector3 start, Vector3 direction, Color color, Color secondColor)
        {
            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (var i = 0; i < 40; i++)
            {
                emit.position = start;
                emit.velocity = (direction * Random.Range(0.5f, 1.5f) + Random.insideUnitSphere).normalized * Random.Range(0.8f, 3.2f);
                emit.startSize = Random.Range(0.05f, 0.12f);
                emit.startLifetime = Random.Range(0.25f, 0.65f);
                emit.startColor = Color.Lerp(Color.white, i % 2 == 0 ? color : secondColor, Random.Range(0.4f, 1f));
                particles.Emit(emit, 1);
            }
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            UpdateLines();
            if (m_Age > k_Lifetime + 0.2f)
                Destroy(gameObject);
        }

        // Le rayon s'affine et pâlit ; les spirales tournent et s'écartent en s'éteignant.
        void UpdateLines()
        {
            var beam = 1f - Mathf.Clamp01(m_Age / k_BeamTime);
            var fade = Mathf.Sqrt(beam);
            Fade(m_Core, k_CoreWidth * beam * beam, fade);
            Fade(m_Inner, k_InnerWidth * beam, fade);
            Fade(m_Halo, k_HaloWidth * fade, fade);
            foreach (var spiral in m_Spirals)
                Fade(spiral, k_SpiralWidth * beam, fade);
            if (beam > 0f)
                UpdateSpirals(k_SpiralRadius * (1f + (1f - beam) * 0.8f), m_Age * k_SpiralSpin);
        }

        static void Fade(LineRenderer line, float width, float alpha)
        {
            if (line == null)
                return;
            line.enabled = alpha > 0f;
            line.widthMultiplier = width;
            // Plus pâle au loin : le rayon se perd dans la distance.
            line.startColor = new Color(1f, 1f, 1f, alpha);
            line.endColor = new Color(1f, 1f, 1f, 0.35f * alpha);
        }

        void OnDestroy()
        {
            foreach (var material in m_Materials)
            {
                if (material != null)
                    Destroy(material);
            }
        }

        /// <summary>Le matériau additif du laser (pour d'autres effets de l'arc cosmique), ou null.</summary>
        internal static Material SharedMaterial
        {
            get
            {
                if (s_Material == null)
                    s_Material = Resources.Load<Material>(k_MaterialResource);
                return s_Material;
            }
        }
    }
}
