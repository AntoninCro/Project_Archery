using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Cyber
{
    /// <summary>
    /// Une gerbe de pixels rouges et cyan qui s'éparpillent et s'éteignent : ce qui est effacé (un tireur, un éclat pris).
    /// </summary>
    public static class GlitchPixels
    {
        /// <param name="material">Matériau des effets (couleurs des sommets, transparent : <c>Materials/Effects</c>).</param>
        public static void Burst(Vector3 position, int count, float spread, Material material, Transform parent = null)
        {
            if (material == null || count <= 0)
                return;

            var go = new GameObject("Pixels");
            if (parent != null)
                go.transform.SetParent(parent, true);
            go.transform.position = position;
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * spread, 3f * spread);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(2.5f, 0.15f, 0.15f, 1f), new Color(0.2f, 2f, 2.5f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.gravityModifier = 0f;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Min(count, 500)) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f * spread;

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            particles.Play();
        }
    }
}
