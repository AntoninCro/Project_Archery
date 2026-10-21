using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Cyber
{
    /// <summary>
    /// Un tireur du toit cyberpunk : un mannequin d'entraînement (un costume raté de THUNK, Docs/Lore.md), en silhouette
    /// sombre aux yeux rouges, qui tient la même arbalète que le joueur. Il ne bouge pas : il apparaît en pixels sur son
    /// poste, se tourne vers le joueur, s'allume (yeux et pointe de l'arbalète) juste avant de tirer, et s'efface en pixels
    /// sous une flèche.
    /// </summary>
    /// <remarks>Créé et commandé par <see cref="CyberDimension"/>, qui choisit quand et comment il tire.</remarks>
    public class CyberShooter : MonoBehaviour, IArrowHitHandler, IArrowAbsorber
    {
        const float k_Height = 1.9f;
        const float k_AppearTime = 0.6f;
        const float k_TurnSpeed = 140f;

        static readonly int k_GlitchId = Shader.PropertyToID("_Glitch");
        static readonly int k_SilhouetteId = Shader.PropertyToID("_Silhouette");
        static readonly int k_ShadowColorId = Shader.PropertyToID("_ShadowColor");
        static readonly int k_RimColorId = Shader.PropertyToID("_RimColor");
        static readonly int k_RimColorAltId = Shader.PropertyToID("_RimColorAlt");
        static readonly int k_RimPowerId = Shader.PropertyToID("_RimPower");
        static readonly int k_TintId = Shader.PropertyToID("_Tint");
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_RoundId = Shader.PropertyToID("_Round");

        static readonly Color k_EyeColor = new Color(3f, 0.22f, 0.16f, 1f);

        enum State
        {
            Appearing,
            Idle,
            Aiming,
            Dead,
        }

        readonly List<Material> m_Materials = new List<Material>();
        CyberDimension m_Dimension;
        Transform m_Body;
        Transform m_Crossbow;
        Vector3 m_Tip;
        Material m_EyeMaterial;
        Material m_TipMaterial;
        Transform m_TipGlow;
        State m_State;
        float m_Timer;
        float m_Glow;
        Vector3 m_AimPoint;
        System.Action m_OnAimed;

        /// <summary>Le poste où il se tient.</summary>
        public Transform Post { get; private set; }

        public bool IsAlive => m_State != State.Dead;

        /// <summary>Prêt à tirer (apparu, et pas déjà en train de viser).</summary>
        public bool Ready => m_State == State.Idle;

        /// <summary>Le milieu du corps (pour les panneaux, qui ne le cachent pas).</summary>
        public Vector3 Point => transform.position + Vector3.up * (k_Height * 0.6f);

        /// <summary>D'où partent ses balles : la pointe de son arbalète.</summary>
        public Vector3 Muzzle => m_Crossbow != null ? m_Crossbow.TransformPoint(m_Tip) : Point;

        public static CyberShooter Spawn(CyberDimension dimension, Transform post, GameObject model, GameObject crossbow, Vector3 tip,
                                         Shader glitch, Material crossbowMaterial)
        {
            var go = new GameObject("Tireur");
            go.transform.SetPositionAndRotation(post.position, post.rotation);
            go.transform.SetParent(dimension.transform, true);
            var shooter = go.AddComponent<CyberShooter>();
            shooter.m_Dimension = dimension;
            shooter.Post = post;
            shooter.m_Tip = tip;
            shooter.BuildBody(model, glitch);
            shooter.BuildCrossbow(crossbow, crossbowMaterial);
            shooter.BuildEyes();

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = Vector3.up * (k_Height * 0.5f);
            capsule.height = k_Height;
            capsule.radius = 0.35f;

            shooter.m_State = State.Appearing;
            shooter.m_Timer = 0f;
            shooter.transform.localScale = new Vector3(1f, 0.01f, 1f);
            GlitchPixels.Burst(shooter.Point, 30, 0.6f, dimension.GlowMaterial, dimension.transform);
            return shooter;
        }

        void BuildBody(GameObject model, Shader glitch)
        {
            if (model == null)
                return;

            var holder = new GameObject("Copie");
            holder.SetActive(false);
            var copy = Instantiate(model, holder.transform);
            Strip(copy);
            copy.name = "Mannequin";
            copy.transform.SetParent(transform, false);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            Destroy(holder);
            m_Body = copy.transform;

            var renderers = copy.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            // À la bonne taille, un peu étiré, les pieds sur le poste.
            var bounds = BoundsOf(renderers);
            var scale = k_Height / Mathf.Max(0.2f, bounds.size.y);
            copy.transform.localScale = Vector3.Scale(copy.transform.localScale * scale, new Vector3(0.85f, 1f, 0.85f));
            bounds = BoundsOf(renderers);
            copy.transform.position += new Vector3(transform.position.x - bounds.center.x, transform.position.y - bounds.min.y,
                                                   transform.position.z - bounds.center.z);

            if (glitch == null)
                return;

            foreach (var renderer in renderers)
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (var i = 0; i < materials.Length; i++)
                {
                    var material = new Material(glitch) { name = "Tireur" };
                    material.SetFloat(k_SilhouetteId, 1f);
                    material.SetFloat(k_GlitchId, 0.2f);
                    material.SetColor(k_ShadowColorId, new Color(0.004f, 0.003f, 0.006f, 1f));
                    material.SetColor(k_RimColorId, new Color(0.7f, 0.03f, 0.05f, 1f));
                    material.SetColor(k_RimColorAltId, new Color(0.04f, 0.5f, 0.7f, 1f));
                    material.SetFloat(k_RimPowerId, 4.5f);
                    material.SetColor(k_TintId, new Color(0.25f, 0.02f, 0.06f, 1f));
                    materials[i] = material;
                    m_Materials.Add(material);
                }

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        // Il tient la même arbalète que le joueur, à hauteur de poitrine : il copie ses gestes.
        void BuildCrossbow(GameObject crossbow, Material material)
        {
            if (crossbow == null)
                return;

            var copy = Instantiate(crossbow, transform, false);
            copy.name = "Arbalète";
            m_Crossbow = copy.transform;
            m_Crossbow.localPosition = new Vector3(0.15f, k_Height * 0.72f, 0.25f);
            foreach (var collider in copy.GetComponentsInChildren<Collider>(true))
                Destroy(collider);
            foreach (var renderer in copy.GetComponentsInChildren<Renderer>(true))
            {
                if (material != null)
                {
                    var materials = renderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++)
                        materials[i] = material;
                    renderer.sharedMaterials = materials;
                }

                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            // La lueur de la pointe, qui grossit avant le tir.
            m_TipMaterial = GlowMaterial(k_EyeColor);
            m_TipGlow = Dot("Lueur", m_Crossbow, m_Tip, 0.01f, m_TipMaterial);
        }

        void BuildEyes()
        {
            m_EyeMaterial = GlowMaterial(k_EyeColor);
            var eyeHeight = k_Height * 0.86f;
            Dot("Œil gauche", transform, new Vector3(-0.07f, eyeHeight, 0.2f), 0.07f, m_EyeMaterial);
            Dot("Œil droit", transform, new Vector3(0.07f, eyeHeight, 0.2f), 0.07f, m_EyeMaterial);
        }

        Material GlowMaterial(Color color)
        {
            var shared = LaserBeam.SharedMaterial;
            if (shared == null)
                return null;
            var material = new Material(shared);
            material.SetColor(k_BaseColorId, color);
            material.SetFloat(k_RoundId, 1f);
            m_Materials.Add(material);
            return material;
        }

        static Transform Dot(string name, Transform parent, Vector3 position, float size, Material material)
        {
            var dot = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dot.name = name;
            Destroy(dot.GetComponent<Collider>());
            dot.transform.SetParent(parent, false);
            dot.transform.localPosition = position;
            dot.transform.localScale = Vector3.one * size;
            var renderer = dot.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            dot.AddComponent<FaceCamera>();
            return dot.transform;
        }

        /// <summary>
        /// Vise ce point : il s'allume pendant <paramref name="aimTime"/> s, puis <paramref name="onAimed"/> tire (la
        /// dimension lance les balles depuis <see cref="Muzzle"/>).
        /// </summary>
        public void Aim(Vector3 point, float aimTime, System.Action onAimed)
        {
            if (m_State != State.Idle)
                return;
            m_AimPoint = point;
            m_OnAimed = onAimed;
            m_State = State.Aiming;
            m_Timer = Mathf.Max(0.1f, aimTime);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            var head = m_Dimension != null ? m_Dimension.Head : null;
            switch (m_State)
            {
                case State.Appearing:
                    m_Timer += deltaTime;
                    transform.localScale = new Vector3(1f, Mathf.Clamp01(m_Timer / k_AppearTime), 1f);
                    if (m_Timer >= k_AppearTime)
                    {
                        transform.localScale = Vector3.one;
                        m_State = State.Idle;
                    }

                    break;

                case State.Aiming:
                    m_Timer -= deltaTime;
                    m_Glow = Mathf.MoveTowards(m_Glow, 1f, deltaTime * 2.5f);
                    if (m_Timer <= 0f)
                    {
                        m_State = State.Idle;
                        m_Glow = 0f;
                        var fire = m_OnAimed;
                        m_OnAimed = null;
                        fire?.Invoke();
                    }

                    break;
            }

            if (m_State == State.Dead)
                return;

            // Il se tourne vers le joueur (en visant, vers le point visé) ; son arbalète vise ce point.
            var target = m_State == State.Aiming ? m_AimPoint : head != null ? head.position : transform.position + transform.forward;
            var flat = target - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), k_TurnSpeed * deltaTime);
            if (m_Crossbow != null)
            {
                var aim = target - m_Crossbow.position;
                if (aim.sqrMagnitude > 0.01f)
                    m_Crossbow.rotation = Quaternion.RotateTowards(m_Crossbow.rotation, Quaternion.LookRotation(aim), k_TurnSpeed * 2f * deltaTime);
            }

            if (m_TipGlow != null)
                m_TipGlow.localScale = Vector3.one * Mathf.Lerp(0.01f, 0.22f, m_Glow * m_Glow);
            if (m_EyeMaterial != null)
                m_EyeMaterial.SetColor(k_BaseColorId, k_EyeColor * (1f + 2f * m_Glow));
        }

        public bool OnArrowHit(in ArrowHit hit)
        {
            if (m_State == State.Dead || m_State == State.Appearing)
                return false;
            Erase();
            m_Dimension.ShooterKilled(this);
            return true;
        }

        /// <summary>Il s'efface en pixels (une flèche, ou la fin de l'épreuve).</summary>
        public void Erase()
        {
            if (m_State == State.Dead)
                return;
            m_State = State.Dead;
            m_OnAimed = null;
            if (m_Dimension != null)
                GlitchPixels.Burst(Point, 70, 1f, m_Dimension.GlowMaterial, m_Dimension.transform);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            foreach (var material in m_Materials)
            {
                if (material != null)
                    Destroy(material);
            }
        }

        // Une copie sans scripts ni collisions : seuls les maillages et leur rendu restent. Un composant dont un autre
        // dépend (RequireComponent) attend que celui-ci soit parti : pas d'erreur « Can't remove ».
        static void Strip(GameObject copy)
        {
            for (var pass = 0; pass < 8; pass++)
            {
                var removed = false;
                foreach (var component in copy.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component is Transform || component is MeshFilter || component is Renderer || IsRequired(component))
                        continue;
                    DestroyImmediate(component);
                    removed = true;
                }

                if (!removed)
                    break;
            }
        }

        static bool IsRequired(Component component)
        {
            var type = component.GetType();
            foreach (var other in component.GetComponents<Component>())
            {
                if (other == null || other == component)
                    continue;
                foreach (RequireComponent require in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                {
                    if (Requires(require.m_Type0, type) || Requires(require.m_Type1, type) || Requires(require.m_Type2, type))
                        return true;
                }
            }

            return false;
        }

        static bool Requires(Type required, Type type) => required != null && required.IsAssignableFrom(type);

        static Bounds BoundsOf(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        // Les yeux et la lueur : des points ronds tournés vers la caméra.
        sealed class FaceCamera : MonoBehaviour
        {
            void LateUpdate()
            {
                var camera = Camera.main;
                if (camera != null)
                    transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position);
            }
        }
    }
}
