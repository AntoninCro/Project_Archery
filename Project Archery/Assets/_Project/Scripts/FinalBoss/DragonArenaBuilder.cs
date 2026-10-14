using System.Collections.Generic;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// L'arène du dragon (GDD, section 23) : une grande grotte ronde, sol de roche sombre, parois et plafond en dôme,
    /// un anneau de lave au pied des parois, des coulées de lave qui descendent des murs, des piliers pour s'abriter
    /// et des rochers. Tout est fabriqué par ce script (dans l'éditeur comme en jeu) à partir des réglages ci-dessous.
    /// </summary>
    /// <remarks>
    /// L'objet est le centre du sol de l'arène, axe Z bleu vers le dragon. Le joueur arrive côté -Z.
    /// Les piliers servent aussi au dragon (il les contourne) et au souffle de feu (ils l'arrêtent) : voir <see cref="Pillars"/>.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class DragonArenaBuilder : MonoBehaviour
    {
        /// <summary>Un pilier : centre au sol (monde) et rayon à la base.</summary>
        public struct Pillar
        {
            public Vector3 Position;
            public float Radius;
            public float Height;
        }

        [Header("Grotte (m)")]
        [Tooltip("Rayon du sol, jusqu'au pied des parois.")]
        [SerializeField]
        float m_FloorRadius = 25.5f;

        [Tooltip("Hauteur du plafond au centre.")]
        [SerializeField]
        float m_CeilingHeight = 17f;

        [Tooltip("Anneau de lave au pied des parois : rayon où il commence.")]
        [SerializeField]
        float m_LavaRingStart = 22f;

        [Tooltip("Relief des parois (m).")]
        [SerializeField]
        float m_WallRoughness = 0.7f;

        [SerializeField]
        int m_Segments = 56;

        [Header("Piliers")]
        [SerializeField]
        int m_PillarCount = 6;

        [Tooltip("Distance des piliers au centre.")]
        [SerializeField]
        float m_PillarRing = 12.5f;

        [Tooltip("Décalage (°) du premier pilier, pour laisser dégagée la ligne entre le joueur et le dragon.")]
        [SerializeField]
        float m_PillarAngleOffset = 30f;

        [SerializeField]
        float m_PillarRadius = 1.9f;

        [SerializeField]
        Vector2 m_PillarHeight = new Vector2(7f, 9f);

        [Header("Décor")]
        [SerializeField]
        int m_LavaFalls = 6;

        [SerializeField]
        int m_Boulders = 12;

        [SerializeField]
        int m_Stalactites = 60;

        [SerializeField]
        int m_Seed = 7;

        [Header("Matériaux")]
        [SerializeField]
        Material m_FloorMaterial;

        [SerializeField]
        Material m_RockMaterial;

        [SerializeField]
        Material m_PillarMaterial;

        [SerializeField]
        Material m_LavaMaterial;

        [Tooltip("Matériau des coulées le long des parois (lave avec « Fall Reveal » coché : elles descendent pendant l'intro).")]
        [SerializeField]
        Material m_LavaFallMaterial;

        [Tooltip("Hauteur (m au-dessus du sol) d'où part la descente des coulées pendant l'intro : au moins le haut de la plus haute coulée.")]
        [SerializeField]
        float m_LavaFallTopHeight = 10.3f;

        [Header("Parties générées (décoche celles que tu remplaces par tes propres objets)")]
        [SerializeField]
        bool m_GenerateFloor = true;

        [SerializeField]
        bool m_GenerateWalls = true;

        [SerializeField]
        bool m_GenerateLavaRing = true;

        [SerializeField]
        bool m_GenerateLavaFalls = true;

        [SerializeField]
        bool m_GeneratePillars = true;

        [SerializeField]
        bool m_GenerateBoulders = true;

        [SerializeField]
        bool m_GenerateStalactites = true;

        // Profil des parois, du pied (rayon, hauteur) jusqu'au sommet du dôme, pour un plafond de 17 m et un sol de 25,5 m.
        static readonly Vector2[] k_Profile =
        {
            new Vector2(1.00f, 0.00f), new Vector2(1.03f, 0.15f), new Vector2(1.01f, 0.30f), new Vector2(0.97f, 0.44f),
            new Vector2(0.90f, 0.59f), new Vector2(0.78f, 0.74f), new Vector2(0.61f, 0.87f), new Vector2(0.37f, 0.96f),
            new Vector2(0.16f, 0.995f), new Vector2(0f, 1f),
        };

        static readonly int k_ArenaLavaGlowId = Shader.PropertyToID("_ArenaLavaGlow");
        static readonly int k_LavaFallCutoffId = Shader.PropertyToID("_LavaFallCutoffY");

        readonly List<Pillar> m_Pillars = new List<Pillar>();
        readonly List<Pillar> m_AllPillars = new List<Pillar>();
        GeneratedRoot m_Generated;
        bool m_Dirty;

        public static DragonArenaBuilder Instance { get; private set; }

        /// <summary>Les piliers de l'arène (positions dans le monde) : ceux qui sont générés, et ceux posés à la main (<see cref="ArenaPillar"/>).</summary>
        public IReadOnlyList<Pillar> Pillars
        {
            get
            {
                m_AllPillars.Clear();
                m_AllPillars.AddRange(m_Pillars);
                foreach (var pillar in ArenaPillar.All)
                {
                    if (pillar != null)
                        m_AllPillars.Add(new Pillar { Position = pillar.transform.position, Radius = pillar.Radius, Height = pillar.Height });
                }

                return m_AllPillars;
            }
        }

        public float FloorRadius => m_FloorRadius;
        public float LavaRingStart => m_LavaRingStart;
        public float FloorY => transform.position.y;
        public Vector3 Center => transform.position;

        /// <summary>Matériau de la lave (anneau, flaques).</summary>
        public Material LavaMaterial => m_LavaMaterial;

        /// <summary>Hauteur (monde) du haut des coulées de lave.</summary>
        public float LavaFallTopY => transform.position.y + m_LavaFallTopHeight;

        void OnEnable()
        {
            if (Application.isPlaying)
                Instance = this;
            m_Generated ??= new GeneratedRoot(transform, "Grotte (générée)");

            // Lave allumée et coulées entières par défaut (l'Arena Director les règle pendant l'intro).
            Shader.SetGlobalFloat(k_ArenaLavaGlowId, 1f);
            Shader.SetGlobalFloat(k_LavaFallCutoffId, -100000f);
            Build();
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
            m_Generated?.Clear();
        }

        void OnValidate() => m_Dirty = true;

        void Update()
        {
            if (!m_Dirty)
                return;

            m_Dirty = false;
            Build();
        }

        /// <summary>Hauteur du plafond (m, au-dessus du sol) à cette distance du centre.</summary>
        public float CeilingHeightAt(float radius)
        {
            var r = Mathf.Clamp01(radius / m_FloorRadius);
            for (var i = 1; i < k_Profile.Length; i++)
            {
                var a = k_Profile[i - 1];
                var b = k_Profile[i];
                if (r <= a.x && r >= b.x)
                {
                    var t = Mathf.Approximately(a.x, b.x) ? 0f : (a.x - r) / (a.x - b.x);
                    return Mathf.Lerp(a.y, b.y, t) * m_CeilingHeight;
                }
            }

            return m_CeilingHeight;
        }

        /// <summary>Point de la voûte (monde) au-dessus de ce point du sol, et la normale qui regarde vers l'intérieur.</summary>
        public Vector3 CeilingPoint(Vector3 floorPoint, out Vector3 inward)
        {
            var local = transform.InverseTransformPoint(floorPoint);
            var radius = new Vector2(local.x, local.z).magnitude;
            var height = CeilingHeightAt(radius);
            var point = new Vector3(local.x, height, local.z);
            inward = transform.TransformDirection(-new Vector3(local.x, height * 0.6f, local.z).normalized);
            return transform.TransformPoint(point);
        }

        /// <summary>Vrai si ce point (monde) est sur le sol de l'arène, hors de l'anneau de lave et hors des piliers.</summary>
        public bool IsFreeGround(Vector3 point, float margin)
        {
            var local = transform.InverseTransformPoint(point);
            if (new Vector2(local.x, local.z).magnitude > m_LavaRingStart - margin)
                return false;

            foreach (var pillar in Pillars)
            {
                var offset = point - pillar.Position;
                offset.y = 0f;
                if (offset.magnitude < pillar.Radius + margin)
                    return false;
            }

            return true;
        }

        // ---------------------------------------------------------------- fabrication

        void Build()
        {
            if (m_Generated == null)
                return;

            var root = m_Generated.Begin();
            m_Pillars.Clear();

            // Chaque partie a son propre tirage : en décocher une ne change pas les autres.
            if (m_GenerateFloor)
                BuildFloor(root, new System.Random(m_Seed));
            if (m_GenerateWalls)
                BuildWalls(root, new System.Random(m_Seed + 1));
            if (m_GenerateLavaRing)
                BuildLavaRing(root);
            BuildLavaFalls(root, new System.Random(m_Seed + 2), m_GenerateLavaFalls);
            if (m_GeneratePillars)
                BuildPillars(root, new System.Random(m_Seed + 3));
            if (m_GenerateBoulders)
                BuildBoulders(root, new System.Random(m_Seed + 4));
            if (m_GenerateStalactites)
                BuildStalactites(root, new System.Random(m_Seed + 5));
        }

        void BuildFloor(Transform root, System.Random random)
        {
            var floor = new MeshBuilder();
            const int rings = 6;
            var segments = m_Segments;
            var color = Color.white;
            for (var ring = 0; ring < rings; ring++)
            {
                var r0 = m_FloorRadius * ring / rings;
                var r1 = m_FloorRadius * (ring + 1) / rings;
                for (var s = 0; s < segments; s++)
                {
                    var a0 = s * Mathf.PI * 2f / segments;
                    var a1 = (s + 1) * Mathf.PI * 2f / segments;
                    var p00 = FloorVertex(r0, a0);
                    var p01 = FloorVertex(r0, a1);
                    var p10 = FloorVertex(r1, a0);
                    var p11 = FloorVertex(r1, a1);
                    if (ring == 0)
                        floor.Triangle(p00, p10, p11, color);
                    else
                        floor.Quad(p00, p10, p11, p01, color);
                }
            }

            m_Generated.CreateMesh("Sol", root, floor.ToMesh("Arène (sol)"), m_FloorMaterial, collider: true);
        }

        // Petites bosses du sol, mais plat là où l'on se bat et sous la lave.
        Vector3 FloorVertex(float radius, float angle)
        {
            var bump = radius < 1f ? 0f : (Mathf.PerlinNoise(Mathf.Cos(angle) * radius * 0.35f + 50f, Mathf.Sin(angle) * radius * 0.35f + 50f) - 0.5f) * 0.25f;
            return new Vector3(Mathf.Sin(angle) * radius, bump, Mathf.Cos(angle) * radius);
        }

        void BuildWalls(Transform root, System.Random random)
        {
            var walls = new MeshBuilder();
            var segments = m_Segments;
            var color = Color.white;
            for (var i = 0; i < k_Profile.Length - 1; i++)
            {
                for (var s = 0; s < segments; s++)
                {
                    var a0 = s * Mathf.PI * 2f / segments;
                    var a1 = (s + 1) * Mathf.PI * 2f / segments;
                    var p00 = WallVertex(i, a0, s);
                    var p01 = WallVertex(i, a1, s + 1);
                    var p10 = WallVertex(i + 1, a0, s);
                    var p11 = WallVertex(i + 1, a1, s + 1);

                    // Vue de l'intérieur de la grotte.
                    if (i == k_Profile.Length - 2)
                        walls.Triangle(p00, p10, p01, color);
                    else
                        walls.Quad(p00, p10, p11, p01, color);
                }
            }

            m_Generated.CreateMesh("Parois", root, walls.ToMesh("Arène (parois)"), m_RockMaterial, collider: true);
        }

        // Le relief est le même d'une maille à l'autre (bruit selon l'angle et la hauteur) : pas de fente entre les facettes.
        Vector3 WallVertex(int profileIndex, float angle, int segment)
        {
            var profile = k_Profile[profileIndex];
            var radius = profile.x * m_FloorRadius;
            var height = profile.y * m_CeilingHeight;
            if (profileIndex > 0 && profileIndex < k_Profile.Length - 1)
            {
                var wrapped = segment % m_Segments;
                var noise = Mathf.PerlinNoise(wrapped * 0.37f + m_Seed, profileIndex * 0.91f + m_Seed * 0.5f) - 0.5f;
                radius += noise * m_WallRoughness * 2f;
                height += (Mathf.PerlinNoise(wrapped * 0.53f + 13f, profileIndex * 1.7f) - 0.5f) * m_WallRoughness;
            }

            return new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius);
        }

        void BuildLavaRing(Transform root)
        {
            // L'anneau de lave : de la fin du sol de combat jusqu'au pied des parois, à peine au-dessus du sol.
            var lava = new MeshBuilder();
            var segments = m_Segments;
            var inner = m_LavaRingStart;
            var outer = m_FloorRadius + 1f;
            for (var s = 0; s < segments; s++)
            {
                var a0 = s * Mathf.PI * 2f / segments;
                var a1 = (s + 1) * Mathf.PI * 2f / segments;
                var wobble0 = 0.6f * Mathf.Sin(a0 * 5f + m_Seed);
                var wobble1 = 0.6f * Mathf.Sin(a1 * 5f + m_Seed);
                var i0 = new Vector3(Mathf.Sin(a0) * (inner + wobble0), 0.06f, Mathf.Cos(a0) * (inner + wobble0));
                var i1 = new Vector3(Mathf.Sin(a1) * (inner + wobble1), 0.06f, Mathf.Cos(a1) * (inner + wobble1));
                var o0 = new Vector3(Mathf.Sin(a0) * outer, 0.06f, Mathf.Cos(a0) * outer);
                var o1 = new Vector3(Mathf.Sin(a1) * outer, 0.06f, Mathf.Cos(a1) * outer);
                var u0 = (float)s / segments * 12f;
                var u1 = (float)(s + 1) / segments * 12f;
                lava.Quad(i0, o0, o1, i1, Color.white, new Vector2(u0, 0f), new Vector2(u0, 1f), new Vector2(u1, 1f), new Vector2(u1, 0f));
            }

            m_Generated.CreateMesh("Lave", root, lava.ToMesh("Arène (lave)"), m_LavaMaterial, collider: false);
        }

        void BuildLavaFalls(Transform root, System.Random random, bool generate)
        {
            if (!generate)
                return;

            var lava = new MeshBuilder();

            // Les coulées : des bandes qui descendent le long des parois, du haut des murs jusqu'à l'anneau.
            for (var f = 0; f < m_LavaFalls; f++)
            {
                var angle = (f + 0.5f) * Mathf.PI * 2f / m_LavaFalls + (float)random.NextDouble() * 0.35f;
                var width = Mathf.Lerp(1.4f, 2.6f, (float)random.NextDouble());
                var halfAngle = width * 0.5f / m_FloorRadius;
                var topIndex = 3 + random.Next(2);
                for (var i = 0; i < topIndex; i++)
                {
                    var bottom = FallVertex(i, angle, -halfAngle);
                    var bottom2 = FallVertex(i, angle, halfAngle);
                    var top = FallVertex(i + 1, angle, -halfAngle);
                    var top2 = FallVertex(i + 1, angle, halfAngle);
                    var v0 = (float)i / topIndex * 3f;
                    var v1 = (float)(i + 1) / topIndex * 3f;
                    lava.Quad(bottom, top, top2, bottom2, Color.white, new Vector2(0f, v0), new Vector2(0f, v1), new Vector2(1f, v1), new Vector2(1f, v0));
                }
            }

            m_Generated.CreateMesh("Coulées de lave", root, lava.ToMesh("Arène (coulées de lave)"), m_LavaFallMaterial, collider: false);
        }

        // Point d'une coulée : un peu en avant de la paroi, pour ne pas la traverser.
        Vector3 FallVertex(int profileIndex, float angle, float angleOffset)
        {
            var profile = k_Profile[Mathf.Min(profileIndex, k_Profile.Length - 1)];
            var radius = profile.x * m_FloorRadius - m_WallRoughness - 0.25f;
            var height = profile.y * m_CeilingHeight;
            var a = angle + angleOffset;
            return new Vector3(Mathf.Sin(a) * radius, height + 0.07f, Mathf.Cos(a) * radius);
        }

        void BuildPillars(Transform root, System.Random random)
        {
            for (var p = 0; p < m_PillarCount; p++)
            {
                var angle = (m_PillarAngleOffset + p * 360f / m_PillarCount) * Mathf.Deg2Rad;
                var center = new Vector3(Mathf.Sin(angle) * m_PillarRing, 0f, Mathf.Cos(angle) * m_PillarRing);
                var height = Mathf.Lerp(m_PillarHeight.x, m_PillarHeight.y, (float)random.NextDouble());
                var mesh = new MeshBuilder();
                BuildColumn(mesh, height, m_PillarRadius, random);

                var renderer = m_Generated.CreateMesh($"Pilier {p + 1}", root, mesh.ToMesh($"Arène (pilier {p + 1})"), m_PillarMaterial, collider: false);
                renderer.transform.localPosition = center;
                renderer.transform.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);

                // Collider simple : une capsule à peine plus fine que la base (flèches, boules de feu, souffle).
                var capsule = renderer.gameObject.AddComponent<CapsuleCollider>();
                capsule.radius = m_PillarRadius * 0.9f;
                capsule.height = height + m_PillarRadius;
                capsule.center = new Vector3(0f, height * 0.5f, 0f);

                m_Pillars.Add(new Pillar { Position = transform.TransformPoint(center), Radius = m_PillarRadius * transform.lossyScale.x, Height = height });
            }
        }

        // Une colonne rocheuse : large à la base, plus fine au milieu, sommet cassé en dents.
        static void BuildColumn(MeshBuilder mesh, float height, float baseRadius, System.Random random)
        {
            const int sides = 9;
            var levels = new[] { 0f, 0.12f, 0.35f, 0.6f, 0.82f, 1f };
            var widths = new[] { 1.15f, 1f, 0.82f, 0.74f, 0.7f, 0.55f };
            var rings = new Vector3[levels.Length, sides];
            for (var l = 0; l < levels.Length; l++)
            {
                for (var s = 0; s < sides; s++)
                {
                    var angle = s * Mathf.PI * 2f / sides;
                    var radius = baseRadius * widths[l] * (0.85f + 0.3f * (float)random.NextDouble());
                    var y = levels[l] * height;
                    if (l == levels.Length - 1)
                        y += ((float)random.NextDouble() - 0.5f) * 1.2f;
                    rings[l, s] = new Vector3(Mathf.Sin(angle) * radius, y, Mathf.Cos(angle) * radius);
                }
            }

            var color = Color.white;
            for (var l = 0; l < levels.Length - 1; l++)
            {
                for (var s = 0; s < sides; s++)
                {
                    var n = (s + 1) % sides;
                    mesh.Quad(rings[l, s], rings[l, n], rings[l + 1, n], rings[l + 1, s], color);
                }
            }

            // Dessus : un éventail vers un point un peu plus bas (cassure).
            var top = new Vector3(0f, height * 0.97f, 0f);
            for (var s = 0; s < sides; s++)
            {
                var n = (s + 1) % sides;
                mesh.Triangle(rings[levels.Length - 1, s], rings[levels.Length - 1, n], top, color);
            }
        }

        void BuildBoulders(Transform root, System.Random random)
        {
            var mesh = new MeshBuilder();
            for (var i = 0; i < m_Boulders; i++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var radius = Mathf.Lerp(m_LavaRingStart - 3f, m_LavaRingStart - 0.8f, (float)random.NextDouble());
                var size = Mathf.Lerp(0.6f, 1.8f, (float)random.NextDouble());
                var center = new Vector3(Mathf.Sin(angle) * radius, size * 0.35f, Mathf.Cos(angle) * radius);

                // Pas devant l'arrivée du joueur ni sur un pilier.
                if (Vector3.Distance(center, new Vector3(0f, 0f, -m_FloorRadius * 0.68f)) < 5f || NearPillar(transform.TransformPoint(center), size + 1f))
                    continue;

                mesh.Rock(center, new Vector3(size, size * 0.7f, size * 1.1f), Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f), Color.white, random);
            }

            m_Generated.CreateMesh("Rochers", root, mesh.ToMesh("Arène (rochers)"), m_RockMaterial, collider: true);
        }

        bool NearPillar(Vector3 point, float margin)
        {
            foreach (var pillar in m_Pillars)
            {
                var offset = point - pillar.Position;
                offset.y = 0f;
                if (offset.magnitude < pillar.Radius + margin)
                    return true;
            }

            return false;
        }

        void BuildStalactites(Transform root, System.Random random)
        {
            var mesh = new MeshBuilder();
            for (var i = 0; i < m_Stalactites; i++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var radius = Mathf.Sqrt((float)random.NextDouble()) * m_FloorRadius * 0.85f;
                var floor = new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
                var top = new Vector3(floor.x, CeilingHeightAt(radius) + 0.4f, floor.z);
                var length = Mathf.Lerp(0.8f, 3.2f, (float)random.NextDouble());
                var width = length * Mathf.Lerp(0.18f, 0.3f, (float)random.NextDouble());
                mesh.Crystal(top, Vector3.down, length, width, 6, Color.white, (float)random.NextDouble() * 6f, 0f);
            }

            m_Generated.CreateMesh("Stalactites", root, mesh.ToMesh("Arène (stalactites)"), m_RockMaterial, collider: false);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Transforme la grotte générée en objets normaux, enregistrés dans la scène (leurs maillages vont dans
        /// <c>Art/Arena</c>) : on peut alors les déplacer, les modifier ou les remplacer à la main. Les parties figées ne
        /// sont plus générées (cases décochées). Les piliers figés gardent un <see cref="ArenaPillar"/> pour le dragon.
        /// Ctrl+Z annule.
        /// </summary>
        [ContextMenu("Figer la grotte en objets modifiables")]
        void FreezeIntoSceneObjects()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("DragonArenaBuilder : on fige la grotte hors du jeu (pas en Play).", this);
                return;
            }

            Build();
            var generated = m_Generated?.Root;
            if (generated == null || generated.childCount == 0)
            {
                Debug.LogWarning("DragonArenaBuilder : rien à figer, toutes les parties sont déjà décochées.", this);
                return;
            }

            const string parentFolder = "Assets/_Project/Art";
            const string folder = parentFolder + "/Arena";
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                UnityEditor.AssetDatabase.CreateFolder(parentFolder, "Arena");

            var frozen = new GameObject("Grotte (modifiable)");
            UnityEditor.Undo.RegisterCreatedObjectUndo(frozen, "Figer la grotte");
            frozen.transform.SetParent(transform, false);

            foreach (Transform part in generated)
            {
                if (!part.TryGetComponent<MeshFilter>(out var filter) || filter.sharedMesh == null || !part.TryGetComponent<MeshRenderer>(out var source))
                    continue;

                var mesh = Instantiate(filter.sharedMesh);
                mesh.name = filter.sharedMesh.name;
                mesh.hideFlags = HideFlags.None;
                UnityEditor.AssetDatabase.CreateAsset(mesh, UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{folder}/{part.name}.asset"));

                var copy = new GameObject(part.name);
                copy.transform.SetParent(frozen.transform, false);
                copy.transform.localPosition = part.localPosition;
                copy.transform.localRotation = part.localRotation;
                copy.transform.localScale = part.localScale;
                copy.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = copy.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = source.sharedMaterial;
                renderer.shadowCastingMode = source.shadowCastingMode;
                renderer.receiveShadows = source.receiveShadows;

                if (part.TryGetComponent<MeshCollider>(out _))
                    copy.AddComponent<MeshCollider>().sharedMesh = mesh;
                if (part.TryGetComponent<CapsuleCollider>(out var capsule))
                {
                    var collider = copy.AddComponent<CapsuleCollider>();
                    collider.radius = capsule.radius;
                    collider.height = capsule.height;
                    collider.center = capsule.center;
                    collider.direction = capsule.direction;
                }

                if (part.name.StartsWith("Pilier"))
                {
                    var pillar = copy.AddComponent<ArenaPillar>();
                    pillar.Radius = m_PillarRadius;
                    pillar.Height = Mathf.Lerp(m_PillarHeight.x, m_PillarHeight.y, 0.5f);
                }
            }

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.Undo.RecordObject(this, "Figer la grotte");
            m_GenerateFloor = m_GenerateWalls = m_GenerateLavaRing = m_GenerateLavaFalls = false;
            m_GeneratePillars = m_GenerateBoulders = m_GenerateStalactites = false;
            UnityEditor.EditorUtility.SetDirty(this);
            Build();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"DragonArenaBuilder : grotte figée dans « {frozen.name} » (maillages dans {folder}). Enregistre la scène.", frozen);
        }
#endif

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(Vector3.zero, m_LavaRingStart);
        }
    }
}
