using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Bows
{
    /// <summary>
    /// La zone brûlante que laisse la pluie de flèches (<see cref="ArrowRain"/>) : un disque orangé qui palpite au sol, des
    /// braises qui montent et un crépitement. Les ennemis au sol dedans perdent des PV deux fois par seconde (pas de points
    /// de touche ni de combo, mais l'élimination compte). Elle s'éteint pendant sa dernière seconde.
    /// </summary>
    public class RainZone : MonoBehaviour
    {
        const float k_TickInterval = 0.5f;

        // Écart de hauteur (m) au-delà duquel un ennemi n'est pas sur la zone (les volants passent au-dessus).
        const float k_MaxHeightGap = 1.5f;

        // Braises par seconde pour une zone de 3,5 m de rayon (plus pour une plus grande).
        const float k_EmbersPerSecond = 30f;

        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_RoundId = Shader.PropertyToID("_Round");
        static readonly List<Enemy> s_Enemies = new List<Enemy>();
        static Material s_DiscMaterial;

        MeshRenderer m_Disc;
        MaterialPropertyBlock m_Block;
        ParticleSystem m_Embers;
        Material m_EmberMaterial;
        AudioSource m_Audio;
        Color m_Color;
        float m_Radius;
        float m_Duration;
        float m_DamagePerSecond;
        float m_Age;
        float m_TickTimer;

        static Material DiscMaterial
        {
            get
            {
                if (s_DiscMaterial == null)
                {
                    var shader = Shader.Find("Archery/UnlitVertexColor");
                    if (shader == null)
                        shader = Shader.Find("Sprites/Default");
                    s_DiscMaterial = new Material(shader) { name = "Rain Zone (runtime)" };
                }

                return s_DiscMaterial;
            }
        }

        /// <param name="damagePerSecond">Dégâts par seconde à chaque ennemi au sol dans la zone.</param>
        /// <param name="loop">Optionnel : son en boucle de la zone.</param>
        public static RainZone Spawn(Vector3 groundPoint, float radius, float duration, float damagePerSecond, Color color, AudioClip loop)
        {
            var go = new GameObject("Zone de la pluie");
            go.transform.SetPositionAndRotation(groundPoint + Vector3.up * 0.04f, Quaternion.identity);
            var zone = go.AddComponent<RainZone>();
            zone.m_Color = color;
            zone.m_Radius = Mathf.Max(0.5f, radius);
            zone.m_Duration = Mathf.Max(0.5f, duration);
            zone.m_DamagePerSecond = damagePerSecond;
            zone.Build(loop);
            return zone;
        }

        void Build(AudioClip loop)
        {
            var disc = new GameObject("Disque");
            disc.transform.SetParent(transform, false);
            disc.transform.localScale = new Vector3(m_Radius, 1f, m_Radius);
            disc.AddComponent<MeshFilter>().sharedMesh = IceZone.Disc;
            m_Disc = disc.AddComponent<MeshRenderer>();
            m_Disc.sharedMaterial = DiscMaterial;
            m_Disc.shadowCastingMode = ShadowCastingMode.Off;
            m_Disc.receiveShadows = false;
            m_Block = new MaterialPropertyBlock();
            ApplyAlpha(0f);

            BuildEmbers();

            if (loop == null)
                return;

            m_Audio = gameObject.AddComponent<AudioSource>();
            m_Audio.clip = loop;
            m_Audio.loop = true;
            m_Audio.playOnAwake = false;
            m_Audio.spatialBlend = 1f;
            m_Audio.rolloffMode = AudioRolloffMode.Linear;
            m_Audio.minDistance = 2f;
            m_Audio.maxDistance = 30f;
            m_Audio.dopplerLevel = 0f;
            m_Audio.outputAudioMixerGroup = Sfx.Output;
            m_Audio.volume = 0f;
            m_Audio.time = Random.Range(0f, loop.length * 0.9f);
            m_Audio.Play();
        }

        // Des braises qui montent de toute la zone, additives (le matériau du laser, en points ronds).
        void BuildEmbers()
        {
            var shared = LaserBeam.SharedMaterial;
            if (shared == null)
                return;

            m_EmberMaterial = new Material(shared);
            m_EmberMaterial.SetColor(k_BaseColorId, Color.white * 3f);
            m_EmberMaterial.SetFloat(k_RoundId, 1f);

            m_Embers = new GameObject("Braises").AddComponent<ParticleSystem>();
            m_Embers.transform.SetParent(transform, false);
            m_Embers.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            m_Embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = m_Embers.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(m_Color, Color.Lerp(m_Color, new Color(1f, 0.92f, 0.55f), 0.6f));
            main.maxParticles = 400;

            var emission = m_Embers.emission;
            var area = m_Radius / 3.5f;
            emission.rateOverTime = k_EmbersPerSecond * area * area;

            var shape = m_Embers.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = m_Radius;
            shape.radiusThickness = 1f;

            var velocity = m_Embers.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

            var colorOverLifetime = m_Embers.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                             new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;

            var renderer = m_Embers.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = m_EmberMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            m_Embers.Play();
        }

        void Update()
        {
            m_Age += Time.deltaTime;

            // La zone s'éteint, les dernières braises finissent de monter.
            if (m_Age >= m_Duration)
            {
                if (m_Disc.enabled)
                {
                    m_Disc.enabled = false;
                    if (m_Embers != null)
                        m_Embers.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    if (m_Audio != null)
                        m_Audio.Stop();
                }

                if (m_Age >= m_Duration + 1.8f)
                    Destroy(gameObject);
                return;
            }

            // Apparition rapide, palpitation, puis elle s'éteint pendant la dernière seconde.
            var fade = Mathf.Min(1f, m_Age / 0.2f) * Mathf.Clamp01(m_Duration - m_Age);
            var flicker = 0.85f + 0.15f * Mathf.Sin(m_Age * 9f) * Mathf.Sin(m_Age * 5.3f + 1f);
            ApplyAlpha(fade * flicker);
            if (m_Audio != null)
                m_Audio.volume = 0.6f * fade;

            m_TickTimer -= Time.deltaTime;
            if (m_TickTimer > 0f)
                return;

            m_TickTimer = k_TickInterval;
            Burn();
        }

        // Copie de la liste : les ennemis tués en sortent pendant la boucle.
        void Burn()
        {
            var amount = m_DamagePerSecond * k_TickInterval;
            if (amount <= 0f)
                return;

            var center = transform.position;
            s_Enemies.Clear();
            s_Enemies.AddRange(Enemy.Alive);
            foreach (var enemy in s_Enemies)
            {
                if (enemy == null || !enemy.IsAlive || enemy.Health == null)
                    continue;

                var offset = enemy.transform.position - center;
                if (Mathf.Abs(offset.y) > k_MaxHeightGap)
                    continue;

                offset.y = 0f;
                if (offset.sqrMagnitude > m_Radius * m_Radius)
                    continue;

                enemy.Health.TakeDamage(new DamageInfo
                {
                    Amount = amount,
                    Zone = HitZone.Body,
                    Grade = ShotGrade.None,
                    Point = enemy.Center,
                    Direction = Vector3.up,
                    Source = this,
                });
            }
        }

        void ApplyAlpha(float alpha)
        {
            var color = m_Color;
            color.a *= alpha;
            m_Disc.GetPropertyBlock(m_Block);
            m_Block.SetColor(k_BaseColorId, color);
            m_Disc.SetPropertyBlock(m_Block);
        }

        void OnDestroy()
        {
            if (m_EmberMaterial != null)
                Destroy(m_EmberMaterial);
        }
    }
}
