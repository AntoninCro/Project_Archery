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
        /// Souffle de feu : un jet en cône devant la gueule (axe Z de <paramref name="parent"/>), épais, avec un cœur
        /// plus clair qui se voit même de face. À lancer avec <see cref="ParticleSystem.Play()"/> et arrêter avec
        /// <see cref="ParticleSystem.Stop()"/> ; <see cref="SetBreathRange"/> change sa portée.
        /// </summary>
        public static ParticleSystem CreateBreath(Transform parent, Material material, float range, float coneAngle)
        {
            var breath = Create("Souffle de feu", parent, Vector3.zero, Quaternion.identity, material);
            var main = breath.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.7f);
            main.maxParticles = 1400;
            var emission = breath.emission;
            emission.rateOverTime = 480f;
            var shape = breath.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = coneAngle * 0.85f;
            shape.radius = 0.35f;
            var size = breath.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.7f, 1f, 5.5f));

            // Le cœur : un jet plus étroit et presque blanc.
            var core = Create("Cœur du souffle", breath.transform, Vector3.zero, Quaternion.identity, material);
            var coreMain = core.main;
            coreMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.5f);
            coreMain.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            coreMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 0.85f), new Color(1f, 0.85f, 0.45f));
            coreMain.maxParticles = 600;
            var coreEmission = core.emission;
            coreEmission.rateOverTime = 300f;
            var coreShape = core.shape;
            coreShape.shapeType = ParticleSystemShapeType.Cone;
            coreShape.angle = coneAngle * 0.35f;
            coreShape.radius = 0.15f;
            var coreSize = core.sizeOverLifetime;
            coreSize.enabled = true;
            coreSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.8f, 1f, 3f));

            SetBreathRange(breath, range);
            return breath;
        }

        /// <summary>Règle la portée (m) d'un souffle fait par <see cref="CreateBreath"/> : la vitesse de ses flammes.</summary>
        public static void SetBreathRange(ParticleSystem breath, float range)
        {
            var speed = range / 0.75f;
            var main = breath.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.75f, speed);
            foreach (Transform child in breath.transform)
            {
                if (!child.TryGetComponent<ParticleSystem>(out var core))
                    continue;
                var coreMain = core.main;
                coreMain.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.9f, speed * 1.1f);
            }
        }

        /// <summary>
        /// Flammes qui lèchent une boule de feu : elles naissent sur toute sa surface et restent en arrière quand elle
        /// file. <see cref="SetFireballFlamesSize"/> suit sa taille pendant qu'elle grossit.
        /// </summary>
        public static ParticleSystem CreateFireballFlames(Transform parent, Material material, float size)
        {
            var flames = Create("Flammes", parent, Vector3.zero, Quaternion.identity, material);
            var main = flames.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            main.gravityModifier = -0.4f;
            main.maxParticles = 300;
            var emission = flames.emission;
            emission.rateOverTime = 140f;
            var shape = flames.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            SetFireballFlamesSize(flames, size);
            flames.Play();
            return flames;
        }

        public static void SetFireballFlamesSize(ParticleSystem flames, float size)
        {
            var main = flames.main;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.45f, size * 0.8f);
            var shape = flames.shape;
            shape.radius = size * 0.45f;
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

        /// <summary>
        /// Flammes d'un anneau de feu qui s'élargit, à plat sur le sol autour de <paramref name="parent"/> : elles naissent
        /// sur le bord du cercle et partent vers l'extérieur. <see cref="SetRingRadius"/> le fait grandir.
        /// </summary>
        public static ParticleSystem CreateRingFlames(Transform parent, Material material)
        {
            var flames = Create("Flammes de l'anneau", parent, Vector3.zero, Quaternion.Euler(-90f, 0f, 0f), material);
            var main = flames.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
            main.gravityModifier = -0.6f;
            main.maxParticles = 3000;
            var shape = flames.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radiusThickness = 0f;
            SetRingRadius(flames, 0.5f, 0f);
            flames.Play();
            return flames;
        }

        /// <summary>Rayon (m) de l'anneau, et nombre de flammes par mètre de bord et par seconde.</summary>
        public static void SetRingRadius(ParticleSystem flames, float radius, float density)
        {
            var shape = flames.shape;
            shape.radius = Mathf.Max(0.1f, radius);
            var emission = flames.emission;
            emission.rateOverTime = density * 2f * Mathf.PI * Mathf.Max(0.1f, radius);
        }

        /// <summary>
        /// Braises au-dessus de la lave : de petites étincelles qui montent lentement d'une zone carrée
        /// (<paramref name="area"/> m de côté). Elles brûlent dès leur création.
        /// </summary>
        public static ParticleSystem CreateEmbers(Transform parent, Vector3 localPosition, Material material, float area, float rate)
        {
            var embers = Create("Braises", parent, localPosition, Quaternion.Euler(-90f, 0f, 0f), material);
            var main = embers.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
            main.gravityModifier = -0.04f;
            main.maxParticles = 80;
            var emission = embers.emission;
            emission.rateOverTime = rate;
            var shape = embers.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(area, area, 0.1f);

            // Elles dérivent un peu, comme portées par la chaleur.
            var noise = embers.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 0.4f;
            embers.Play();
            return embers;
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
