using System;
using System.Collections.Generic;
using Archery.Core;
using Archery.Economy;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Random = UnityEngine.Random;

namespace Archery.Building
{
    /// <summary>
    /// Un gisement de cristaux (GDD, section 9). On le vise et on appuie sur la gâchette de prise, à quelques mètres
    /// au plus, comme un coffre : il éclate et donne ses matériaux.
    /// </summary>
    /// <remarks>
    /// Il pousse du sol en apparaissant, sa lueur pulse, et il disparaît à la fin de la vague s'il n'a pas été récolté
    /// (<see cref="CrystalSpawner"/>). Ses cristaux sont les modèles enfants (par exemple <c>detail-crystal-large</c>
    /// du Tower Defense Kit) ; sans modèle, un amas de cristaux low-poly est généré au lancement.
    /// </remarks>
    [DisallowMultipleComponent]
    public class CrystalDeposit : XRBaseInteractable
    {
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Optionnel : objet qui contient les modèles des cristaux. Vide : tous les enfants. Sans modèle, un amas low-poly est généré.")]
        [SerializeField]
        Transform m_Model;

        [Tooltip("Matériau lumineux des cristaux (émissif), mis sur tous leurs modèles et sur les éclats.")]
        [SerializeField]
        Material m_GlowMaterial;

        [Tooltip("Optionnel : lumière qui pulse avec les cristaux.")]
        [SerializeField]
        Light m_Light;

        [Tooltip("Distance maximale (m) entre la main et le gisement pour le récolter en le visant.")]
        [SerializeField]
        float m_MaxHarvestDistance = 5f;

        [Tooltip("Couleur de la lueur (émission du matériau, lumière, éclats).")]
        [SerializeField]
        Color m_GlowColor = new Color(0.62f, 0.4f, 1f);

        [Tooltip("Nombre d'éclats projetés quand le gisement éclate.")]
        [SerializeField]
        int m_ShardCount = 16;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_HarvestClip;

        [SerializeField]
        AudioClip m_VanishClip;

        const float k_GrowTime = 0.6f;
        const float k_ShrinkTime = 0.35f;

        /// <summary>Un gisement vient d'être récolté.</summary>
        public static event Action<CrystalDeposit> Harvested;

        /// <summary>Matériaux donnés à la récolte (réglé par le <see cref="CrystalSpawner"/>).</summary>
        public int Amount { get; set; } = 10;

        public bool IsHarvested { get; private set; }

        Renderer[] m_Renderers;
        MaterialPropertyBlock m_Block;
        Vector3 m_BaseScale;
        float m_LightIntensity;
        float m_Age;
        float m_Phase;
        float m_ShrinkAge = -1f;

        protected override void Awake()
        {
            base.Awake();
            m_BaseScale = transform.localScale;
            m_Phase = Random.Range(0f, 6f);
            var root = m_Model != null ? m_Model : transform;
            m_Renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            if (m_Renderers.Length == 0)
                m_Renderers = new Renderer[] { BuildGeneratedModel(root) };
            if (m_GlowMaterial != null)
            {
                foreach (var renderer in m_Renderers)
                {
                    var materials = renderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++)
                        materials[i] = m_GlowMaterial;
                    renderer.sharedMaterials = materials;
                }
            }

            if (m_Light != null)
                m_LightIntensity = m_Light.intensity;
            transform.localScale = Vector3.zero;
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_Age += deltaTime;

            if (m_ShrinkAge >= 0f)
            {
                m_ShrinkAge += deltaTime;
                transform.localScale = m_BaseScale * Mathf.Max(0f, 1f - m_ShrinkAge / k_ShrinkTime);
                if (m_ShrinkAge >= k_ShrinkTime)
                    Destroy(gameObject);
                return;
            }

            // Il pousse du sol, avec un léger dépassement.
            var t = Mathf.Clamp01(m_Age / k_GrowTime);
            var grow = 1f + 0.12f * Mathf.Sin(t * Mathf.PI) - (1f - t) * (1f - t);
            transform.localScale = m_BaseScale * Mathf.Max(0f, grow);

            // La lueur pulse.
            var pulse = 0.75f + 0.25f * Mathf.Sin((m_Age + m_Phase) * 2.2f);
            if (m_Light != null)
                m_Light.intensity = m_LightIntensity * pulse;
            m_Block ??= new MaterialPropertyBlock();
            foreach (var renderer in m_Renderers)
            {
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(m_Block);
                m_Block.SetColor(k_EmissionColorId, m_GlowColor * (0.8f + 0.9f * pulse));
                renderer.SetPropertyBlock(m_Block);
            }
        }

        public override bool IsSelectableBy(IXRSelectInteractor interactor) =>
            base.IsSelectableBy(interactor) && !IsHarvested && m_ShrinkAge < 0f && m_Age >= k_GrowTime * 0.5f && IsInReach(interactor);

        // La main (la manette, pas le point visé) doit être à moins de quelques mètres du gisement.
        bool IsInReach(IXRInteractor interactor)
        {
            var hand = interactor.transform.position;
            foreach (var collider in colliders)
            {
                if (collider != null && collider.enabled &&
                    (collider.ClosestPoint(hand) - hand).sqrMagnitude <= m_MaxHarvestDistance * m_MaxHarvestDistance)
                    return true;
            }

            return false;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            if (IsHarvested || m_ShrinkAge >= 0f)
                return;

            IsHarvested = true;
            var score = ScoreManager.Instance;
            if (score != null)
                score.AddMaterials(Amount);

            Haptics.Pulse(args.interactorObject, 0.7f, 0.15f);
            Sfx.Play(m_HarvestClip, transform.position + Vector3.up * 0.6f);
            Shatter();
            Harvested?.Invoke(this);
            Leave();
        }

        /// <summary>Le gisement disparaît sans être récolté (fin de la vague).</summary>
        public void Vanish()
        {
            if (m_ShrinkAge >= 0f)
                return;

            Sfx.Play(m_VanishClip, transform.position + Vector3.up * 0.5f, 0.6f);
            Leave();
        }

        void Leave()
        {
            foreach (var collider in colliders)
            {
                if (collider != null)
                    collider.enabled = false;
            }

            m_ShrinkAge = 0f;
        }

        // Des éclats de cristal jaillissent et retombent.
        void Shatter()
        {
            var go = new GameObject("Crystal Shards");
            go.transform.SetPositionAndRotation(transform.position + Vector3.up * 0.5f, Quaternion.LookRotation(Vector3.up));
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 1.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)m_ShardCount) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 50f;
            shape.radius = 0.25f;

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Archery.World.RiverWater.Octahedron();
            renderer.sharedMaterial = m_GlowMaterial != null ? m_GlowMaterial
                : m_Renderers.Length > 0 && m_Renderers[0] != null ? m_Renderers[0].sharedMaterial : null;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            particles.Play();
        }

        Renderer BuildGeneratedModel(Transform parent)
        {
            var go = new GameObject("Generated Crystals");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = BuildCluster(Mathf.RoundToInt(transform.position.x * 13f + transform.position.z * 7f));
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_GlowMaterial;
            return renderer;
        }

        // Un amas de 5 à 7 cristaux : des prismes à six pans, pointus, inclinés vers l'extérieur. Facettes plates.
        static Mesh BuildCluster(int seed)
        {
            var state = Random.state;
            Random.InitState(seed);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var count = Random.Range(5, 8);
            for (var c = 0; c < count; c++)
            {
                var main = c == 0;
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var offset = main ? Vector2.zero : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(0.15f, 0.38f);
                var radius = main ? 0.2f : Random.Range(0.08f, 0.16f);
                var height = main ? 1.3f : Random.Range(0.45f, 0.95f);
                var tilt = main ? Quaternion.Euler(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f))
                    : Quaternion.AngleAxis(Random.Range(15f, 35f), new Vector3(offset.y, 0f, -offset.x).normalized);
                var spin = Random.Range(0f, 60f);
                AddCrystal(vertices, triangles, new Vector3(offset.x, -0.05f, offset.y), tilt, radius, height, spin);
            }

            Random.state = state;
            var mesh = new Mesh { name = "Crystal Cluster" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddCrystal(List<Vector3> vertices, List<int> triangles, Vector3 basePoint, Quaternion tilt,
            float radius, float height, float spinDegrees)
        {
            const int sides = 6;
            var ring0 = new Vector3[sides];
            var ring1 = new Vector3[sides];
            var shaft = height * 0.72f;
            for (var i = 0; i < sides; i++)
            {
                var a = (spinDegrees + i * 360f / sides) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                ring0[i] = basePoint + tilt * (dir * radius * 0.85f);
                ring1[i] = basePoint + tilt * (dir * radius + Vector3.up * shaft);
            }

            var tip = basePoint + tilt * (Vector3.up * height);
            for (var i = 0; i < sides; i++)
            {
                var j = (i + 1) % sides;
                AddQuad(vertices, triangles, ring0[i], ring1[i], ring1[j], ring0[j]);
                AddTriangle(vertices, triangles, ring1[i], tip, ring1[j]);
            }
        }

        // Sommets séparés par face : le maillage reste à facettes plates, comme le reste du kit.
        static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTriangle(vertices, triangles, a, b, c);
            AddTriangle(vertices, triangles, a, c, d);
        }
    }
}
