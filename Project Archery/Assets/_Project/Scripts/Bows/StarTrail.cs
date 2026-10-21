using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Bows
{
    /// <summary>
    /// La traînée d'étoiles des flèches de l'arc cosmique : de petites étoiles laissées derrière la flèche en vol, qui
    /// scintillent un instant et s'éteignent. Un seul système de particules pour toutes les flèches suivies, jusqu'à ce
    /// qu'elles se plantent ou disparaissent. Même matériau additif que le laser.
    /// </summary>
    public class StarTrail : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_RoundId = Shader.PropertyToID("_Round");

        // Une étoile tous les… (m), et au plus tant par flèche et par image.
        const float k_Spacing = 0.12f;
        const int k_MaxPerFrame = 24;

        class Tracked
        {
            public Arrow Arrow;
            public Color Color;
            public Vector3 Last;
        }

        static StarTrail s_Instance;

        readonly List<Tracked> m_Tracked = new List<Tracked>();
        ParticleSystem m_Particles;
        Material m_Material;

        /// <summary>Suit cette flèche, qui vient d'être tirée : elle laisse des étoiles de cette couleur.</summary>
        public static void Track(Arrow arrow, Color color)
        {
            if (arrow == null)
                return;
            if (s_Instance == null)
            {
                s_Instance = new GameObject("Traînée d'étoiles").AddComponent<StarTrail>();
                if (s_Instance.m_Particles == null)
                    return;
            }

            s_Instance.m_Tracked.Add(new Tracked { Arrow = arrow, Color = color, Last = arrow.TipPosition });
        }

        void Awake()
        {
            var shared = LaserBeam.SharedMaterial;
            if (shared == null)
                return;

            m_Material = new Material(shared);
            m_Material.SetColor(k_BaseColorId, Color.white * 3f);
            m_Material.SetFloat(k_RoundId, 1f);

            m_Particles = gameObject.AddComponent<ParticleSystem>();
            m_Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = m_Particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2000;
            main.startSpeed = 0f;
            var emission = m_Particles.emission;
            emission.enabled = false;
            var shape = m_Particles.shape;
            shape.enabled = false;

            var colorOverLifetime = m_Particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;
            var sizeOverLifetime = m_Particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.2f, 1.3f), new Keyframe(1f, 0f)));

            var renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = m_Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            m_Particles.Play();
        }

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
            if (m_Material != null)
                Destroy(m_Material);
        }

        void LateUpdate()
        {
            if (m_Particles == null)
                return;

            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (var i = m_Tracked.Count - 1; i >= 0; i--)
            {
                var tracked = m_Tracked[i];
                if (tracked.Arrow == null || tracked.Arrow.CurrentState != Arrow.State.Flying)
                {
                    m_Tracked.RemoveAt(i);
                    continue;
                }

                var tip = tracked.Arrow.TipPosition;
                var segment = tip - tracked.Last;
                var count = Mathf.Min(k_MaxPerFrame, Mathf.FloorToInt(segment.magnitude / k_Spacing));
                for (var j = 0; j < count; j++)
                {
                    emit.position = tracked.Last + segment * ((j + Random.value) / count) + Random.insideUnitSphere * 0.03f;
                    emit.velocity = Random.insideUnitSphere * 0.15f;
                    emit.startSize = Random.Range(0.015f, 0.05f);
                    emit.startLifetime = Random.Range(0.4f, 1f);
                    var color = Color.Lerp(tracked.Color, Color.white, Random.Range(0f, 0.6f));
                    color.a = Random.Range(0.5f, 1f);
                    emit.startColor = color;
                    m_Particles.Emit(emit, 1);
                }

                if (count > 0)
                    tracked.Last = tip;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Instance = null;
    }
}
