using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Flammes fabriquées par le code (souffle du dragon, boules de feu, braseros de la porte, flaques de lave) :
    /// des systèmes de particules réglés ici, avec le matériau additif « Fire_Particles ».
    /// </summary>
    public static class FireEffects
    {
        /// <summary>Feu de brasero : flammes qui montent doucement. Il brûle dès sa création.</summary>
        public static ParticleSystem CreateBrazierFire(Transform parent, Vector3 localPosition, Material material, float scale)
        {
            var fire = Create("Feu", parent, localPosition, Quaternion.Euler(-90f, 0f, 0f), material);
            var main = fire.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * scale, 2.2f * scale);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f * scale, 0.9f * scale);
            main.maxParticles = 120;
            var emission = fire.emission;
            emission.rateOverTime = 45f;
            var shape = fire.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.35f * scale;
            fire.Play();
            return fire;
        }

        /// <summary>
        /// Souffle de feu : un jet en cône devant la gueule (axe Z de <paramref name="parent"/>). À lancer avec
        /// <see cref="ParticleSystem.Play()"/> et arrêter avec <see cref="ParticleSystem.Stop()"/>.
        /// </summary>
        public static ParticleSystem CreateBreath(Transform parent, Material material, float range, float coneAngle)
        {
            var breath = Create("Souffle de feu", parent, Vector3.zero, Quaternion.identity, material);
            var speed = range / 0.7f;
            var main = breath.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.8f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
            main.maxParticles = 600;
            var emission = breath.emission;
            emission.rateOverTime = 260f;
            var shape = breath.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle * 0.8f;
            shape.radius = 0.25f;
            var size = breath.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 4.5f));
            return breath;
        }

        /// <summary>Traînée d'une boule de feu (en espace monde : elle reste derrière la boule).</summary>
        public static ParticleSystem CreateTrail(Transform parent, Material material, float size)
        {
            var trail = Create("Traînée", parent, Vector3.zero, Quaternion.identity, material);
            var main = trail.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.8f, size * 1.4f);
            main.maxParticles = 160;
            var emission = trail.emission;
            emission.rateOverTime = 90f;
            var shape = trail.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = size * 0.3f;
            trail.Play();
            return trail;
        }

        /// <summary>Gerbe de flammes d'une explosion (boule de feu, rocher), détruite toute seule.</summary>
        public static void Burst(Vector3 position, Material material, float radius, int count = 60)
        {
            var burst = Create("Explosion", null, position, Quaternion.identity, material);
            var main = burst.main;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 1.5f, radius * 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.35f, radius * 0.7f);
            main.maxParticles = count;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = burst.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = burst.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = radius * 0.2f;
            burst.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            burst.Play();
        }

        /// <summary>Flammèches au ras du sol (flaques de lave, traînée de feu d'un piqué).</summary>
        public static ParticleSystem CreateGroundFlames(Transform parent, Material material, float radius, float rate)
        {
            var flames = Create("Flammes", parent, Vector3.zero, Quaternion.Euler(-90f, 0f, 0f), material);
            var main = flames.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.maxParticles = 200;
            var emission = flames.emission;
            emission.rateOverTime = rate;
            var shape = flames.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            flames.Play();
            return flames;
        }

        // Réglages communs : flammes jaunes qui virent à l'orange, puis au rouge sombre, et grossissent un peu.
        static ParticleSystem Create(string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Material material)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.hideFlags = parent.gameObject.hideFlags;
                go.transform.SetParent(parent, false);
            }

            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;

            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.12f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.35f), new Color(1f, 0.55f, 0.15f));

            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.35f),
                    new GradientColorKey(new Color(0.8f, 0.12f, 0.05f), 0.75f),
                    new GradientColorKey(new Color(0.25f, 0.05f, 0.03f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(0.7f, 0.6f),
                    new GradientAlphaKey(0f, 1f),
                });
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.3f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }
    }
}
