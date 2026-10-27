using System.Collections.Generic;
using UnityEngine;

namespace Archery.Upgrades
{
    /// <summary>
    /// Une vraie explosion (26 octobre au soir, demandée par le joueur : avant, de petits éclairs orange) : un flash, une boule
    /// de feu qui gonfle, une fumée épaisse qui monte et s'étale, un anneau de poussière au sol et des étincelles qui
    /// retombent. Pour la flèche explosive, la pointe à mèche (et ses duos), la fusion des failles du katana.
    /// </summary>
    /// <remarks>
    /// Les systèmes de particules sont faits par le code, avec deux matériaux du projet posés sur <see cref="SpecialArrows"/>
    /// (le feu additif <c>Fire_Particles</c>, la fumée <c>Grenade fumee</c>) ; mis à l'échelle du rayon de l'explosion.
    /// Réutilisés : 8 au plus (au-delà, la plus ancienne repart). Les réglages sont les constantes en tête du script.
    /// </remarks>
    public class ExplosionFx : MonoBehaviour
    {
        // L'effet est fait pour une explosion de ce rayon (m), puis mis à l'échelle (entre ces bornes).
        const float k_ReferenceRadius = 3.5f;
        const float k_MinScale = 0.45f;
        const float k_MaxScale = 2.6f;
        const int k_MaxEffects = 8;

        // Combien de particules : boule de feu, fumée, poussière, étincelles.
        const int k_FireCount = 16;
        const int k_SmokeCount = 14;
        const int k_DustCount = 10;
        const int k_SparkCount = 24;

        // Le temps (s) après lequel l'effet est libre (la fumée la plus longue a fini).
        const float k_Lifetime = 3.4f;

        static readonly List<ExplosionFx> s_Pool = new List<ExplosionFx>();
        static int s_Next;

        readonly List<ParticleSystem> m_Systems = new List<ParticleSystem>();
        float m_FreeAt;

        /// <summary>Joue une explosion à ce point, à l'échelle de ce rayon (m). Rien sans les deux matériaux.</summary>
        public static void Play(Vector3 point, float radius, Material fire, Material smoke, Transform parent)
        {
            if (fire == null || smoke == null)
                return;

            s_Pool.RemoveAll(effect => effect == null);
            ExplosionFx chosen = null;
            foreach (var effect in s_Pool)
            {
                if (Time.time >= effect.m_FreeAt)
                {
                    chosen = effect;
                    break;
                }
            }

            if (chosen == null)
            {
                if (s_Pool.Count < k_MaxEffects)
                {
                    chosen = new GameObject("Explosion").AddComponent<ExplosionFx>();
                    chosen.transform.SetParent(parent, false);
                    chosen.Build(fire, smoke);
                    s_Pool.Add(chosen);
                }
                else
                {
                    chosen = s_Pool[s_Next++ % s_Pool.Count];
                }
            }

            chosen.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            chosen.transform.localScale = Vector3.one * Mathf.Clamp(radius / k_ReferenceRadius, k_MinScale, k_MaxScale);
            chosen.m_FreeAt = Time.time + k_Lifetime;
            foreach (var system in chosen.m_Systems)
            {
                system.Clear(true);
                system.Play(true);
            }
        }

        void Build(Material fire, Material smoke)
        {
            // Le flash : une grosse lueur très brève.
            var flash = System("Flash", fire, 0.25f);
            var main = flash.main;
            main.startLifetime = 0.14f;
            main.startSpeed = 0f;
            main.startSize = 5f;
            main.startColor = new Color(1f, 0.92f, 0.7f, 1f);
            Burst(flash, 1);
            Shape(flash, ParticleSystemShapeType.Sphere, 0f);
            Fade(flash, new Color(1f, 0.95f, 0.8f), new Color(1f, 0.6f, 0.2f), 1f, 0f);
            Grow(flash, 0.6f, 1.3f);

            // La boule de feu : jaune blanc, puis orange, puis rouge sombre, qui gonfle et ralentit.
            var fireball = System("Boule de feu", fire, 0.8f);
            main = fireball.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 2.1f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            Burst(fireball, k_FireCount);
            Shape(fireball, ParticleSystemShapeType.Sphere, 0.6f);
            FireColors(fireball);
            Grow(fireball, 0.55f, 1.5f);
            Damp(fireball, 0.35f);

            // La fumée : épaisse et sombre, elle monte, s'étale et tourne lentement.
            var smokeSystem = System("Fumée", smoke, 3.2f);
            main = smokeSystem.main;
            main.startDelay = 0.05f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.6f, 2.9f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.16f, 0.14f, 0.13f, 0.85f), new Color(0.36f, 0.33f, 0.3f, 0.7f));
            Burst(smokeSystem, k_SmokeCount);
            Shape(smokeSystem, ParticleSystemShapeType.Sphere, 0.9f);
            Fade(smokeSystem, Color.white, new Color(0.8f, 0.8f, 0.8f), 1f, 0f, 0.12f);
            Grow(smokeSystem, 0.5f, 2.3f);
            Damp(smokeSystem, 0.5f);
            Rise(smokeSystem, 1.3f);
            Spin(smokeSystem, 0.5f);

            // La poussière : un anneau qui file au ras du sol.
            var dust = System("Poussière", smoke, 1.4f);
            main = dust.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(1f, 1.7f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.47f, 0.41f, 0.33f, 0.55f);
            Burst(dust, k_DustCount);
            var shape = Shape(dust, ParticleSystemShapeType.Circle, 0.4f);
            shape.rotation = new Vector3(90f, 0f, 0f);
            Fade(dust, Color.white, Color.white, 1f, 0f);
            Grow(dust, 0.6f, 1.8f);
            Damp(dust, 0.6f);

            // Les étincelles : étirées, elles retombent.
            var sparks = System("Étincelles", fire, 1f);
            main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.95f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 17f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.5f, 0.15f));
            main.gravityModifier = 1.5f;
            Burst(sparks, k_SparkCount);
            Shape(sparks, ParticleSystemShapeType.Sphere, 0.3f);
            Fade(sparks, Color.white, Color.white, 1f, 0f);
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2f;
            renderer.velocityScale = 0.04f;
        }

        // Un système de particules qui ne joue qu'à la demande, une seule fois, à l'échelle de l'objet.
        ParticleSystem System(string systemName, Material material, float duration)
        {
            var go = new GameObject(systemName);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            m_Systems.Add(system);
            return system;
        }

        static void Burst(ParticleSystem system, int count)
        {
            var emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        }

        static ParticleSystem.ShapeModule Shape(ParticleSystem system, ParticleSystemShapeType type, float radius)
        {
            var shape = system.shape;
            shape.enabled = true;
            shape.shapeType = type;
            shape.radius = Mathf.Max(0.0001f, radius);
            return shape;
        }

        // La couleur au fil de la vie : de « from » à « to », l'opacité de « alphaFrom » à « alphaTo » (avec un fondu d'entrée).
        static void Fade(ParticleSystem system, Color from, Color to, float alphaFrom, float alphaTo, float fadeIn = 0f)
        {
            var colors = system.colorOverLifetime;
            colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                fadeIn > 0f
                    ? new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(alphaFrom, fadeIn), new GradientAlphaKey(alphaTo, 1f) }
                    : new[] { new GradientAlphaKey(alphaFrom, 0f), new GradientAlphaKey(alphaTo, 1f) });
            colors.color = gradient;
        }

        static void FireColors(ParticleSystem system)
        {
            var colors = system.colorOverLifetime;
            colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.96f, 0.75f), 0f), new GradientColorKey(new Color(1f, 0.58f, 0.15f), 0.35f),
                    new GradientColorKey(new Color(0.55f, 0.1f, 0.04f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) });
            colors.color = gradient;
        }

        static void Grow(ParticleSystem system, float from, float to)
        {
            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, from, 1f, to));
        }

        static void Damp(ParticleSystem system, float damping)
        {
            var limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 0f;
            limit.dampen = damping;
        }

        static void Rise(ParticleSystem system, float speed)
        {
            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(speed);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
        }

        static void Spin(ParticleSystem system, float radiansPerSecond)
        {
            var rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-radiansPerSecond, radiansPerSecond);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Pool.Clear();
            s_Next = 0;
        }
    }
}
