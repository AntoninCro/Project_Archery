using System.Collections.Generic;
using Archery.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Les piliers de l'arène, plus visibles (groupe « Piliers » de l'objet « Effet » ; les piliers sont en obsidienne,
    /// matériau <c>Arena_Obsidian</c>) : une lumière froide les éclaire par en dessous depuis quelques cristaux posés à
    /// leur pied, une couronne de cristaux bleus brille à leur sommet ; en jeu, leur contour brille légèrement (on les
    /// voit de partout, même au fond du brouillard) et, pendant que le dragon prépare son souffle, l'abri derrière chaque
    /// pilier s'éclaire en bleu au sol.
    /// </summary>
    /// <remarks>
    /// Un objet « Pilier » par pilier, avec « Lumière du pilier », « Cristaux du pilier » et « Lueur de la couronne » : on
    /// peut les déplacer ou les modifier (en jeu, chacun suit le pilier le plus proche). Réglages : « Resources/MapLighting »,
    /// partie « Piliers de l'arène ».
    /// </remarks>
    public class PillarLights : MonoBehaviour
    {
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_AmountId = Shader.PropertyToID("_Amount");
        static readonly int k_TintId = Shader.PropertyToID("_Tint");

        const string k_UpLight = "Lumière du pilier";
        const string k_Crystals = "Cristaux du pilier";
        const string k_TopLight = "Lueur de la couronne";
        const int k_CoverRows = 6;

        // Hauteur (m) de la poitrine du joueur : derrière un pilier, elle est à l'abri du souffle.
        const float k_ChestHeight = 1.3f;

        class Pillar
        {
            public Vector3 Foot;
            public float Radius;
            public float Height;

            // Le quart des cristaux du plafond au-dessus du pilier : il s'allume avec lui pendant l'intro.
            public int Group;
            public Light Up;
            public float UpIntensity;
            public Light Top;
            public float TopIntensity;
            public Vector3 TopPoint;
            public MeshRenderer Crystals;
            public float CrystalIntensity = 2.2f;
            public MeshRenderer Cover;
            public Mesh CoverMesh;
            public Vector3[] CoverVertices;
            public Color[] CoverColors;
        }

        readonly List<Pillar> m_Pillars = new List<Pillar>();
        readonly List<Object> m_Owned = new List<Object>();
        MapLightingSettings m_Settings;
        ArenaDirector m_Director;
        ArenaShape m_Arena;
        DragonBoss m_Dragon;
        CeilingCrystals m_CeilingCrystals;
        MaterialPropertyBlock m_Block;
        Material m_RimMaterial;
        float m_Cover;
        bool m_Bound;

        // ---------------------------------------------------------------- fabrication (jeu ou éditeur)

        /// <summary>Pose la lumière d'en bas, les cristaux et la lueur de chaque pilier (sous cet objet).</summary>
        public void Build(MapLightingSettings settings, ArenaShape arena)
        {
            m_Settings = settings;
            m_Arena = arena;
            Material crystals = null;
            if (settings.crystalMaterial != null)
            {
                crystals = new Material(settings.crystalMaterial) { name = "Cristaux des piliers" };
                crystals.SetColor(k_TintId, Color.white);
                crystals.SetFloat(k_IntensityId, 2.2f);
            }

            // Une copie : la liste des piliers de l'arène est refaite à chaque lecture (IsFreeGround la relit).
            var random = new System.Random(131);
            foreach (var pillar in new List<ArenaShape.Pillar>(arena.Pillars))
                CreatePillar(pillar, crystals, random);
        }

        void CreatePillar(ArenaShape.Pillar pillar, Material crystals, System.Random random)
        {
            var root = new GameObject("Pilier").transform;
            root.SetParent(transform, false);
            root.position = pillar.Position;
            var builder = new MeshBuilder();

            // La lumière d'en bas, côté arène (ou d'un autre côté s'il y a de la lave ou une paroi), et ses cristaux.
            var toCenter = m_Arena.Center - pillar.Position;
            toCenter.y = 0f;
            var facing = toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector3.forward;
            var lamp = pillar.Position + facing * (pillar.Radius + 1.6f);
            foreach (var turn in new[] { 0f, 60f, -60f, 120f, -120f, 180f })
            {
                var candidate = pillar.Position + Quaternion.Euler(0f, turn, 0f) * facing * (pillar.Radius + 1.6f);
                if (!m_Arena.IsFreeGround(candidate, 0.3f))
                    continue;
                lamp = candidate;
                break;
            }

            lamp.y = m_Arena.GroundHeightAt(lamp);
            AddCluster(builder, lamp - pillar.Position, Vector3.up, 3, 0.35f, 0.8f, random);
            var up = new GameObject(k_UpLight).AddComponent<Light>();
            up.transform.SetParent(root, false);
            up.transform.position = lamp + Vector3.up * 0.4f;
            up.transform.rotation = Quaternion.LookRotation(pillar.Position + Vector3.up * (pillar.Height * 0.5f) - up.transform.position);
            up.type = LightType.Spot;
            up.spotAngle = 70f;
            up.innerSpotAngle = 35f;
            up.range = pillar.Height + 6f;
            up.color = m_Settings.pillarLightColor;
            up.intensity = m_Settings.pillarLightIntensity;
            up.shadows = LightShadows.None;

            // La couronne : des grappes de cristaux sur le pourtour du sommet, le milieu reste libre (le dragon s'y pose).
            var top = pillar.Position + Vector3.up * pillar.Height;
            if (TryTop(top, out var surface))
                top = surface;
            const int clusters = 7;
            for (var i = 0; i < clusters; i++)
            {
                var angle = (i + (float)random.NextDouble() * 0.5f) * 360f / clusters;
                var outward = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                // Pas de roche sous ce point (au-delà du bord) : pas de grappe, plutôt que des cristaux en l'air.
                if (!TryTop(top + outward * (pillar.Radius * 0.7f), out var spot) || spot.y < top.y - 2f)
                    continue;
                AddCluster(builder, spot - pillar.Position, (Vector3.up + outward * 0.4f).normalized, 2 + random.Next(3), 0.5f, 1.3f, random);
            }

            if (crystals != null && builder.VertexCount > 0)
            {
                var go = new GameObject(k_Crystals);
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = builder.ToMesh("Cristaux du pilier");
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = crystals;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            var crown = new GameObject(k_TopLight).AddComponent<Light>();
            crown.transform.SetParent(root, false);
            crown.transform.position = top + Vector3.up * 1.5f;
            crown.type = LightType.Point;
            crown.range = 8f;
            crown.color = m_Settings.pillarCrystalColor;
            crown.intensity = 1.6f;
            crown.shadows = LightShadows.None;
        }

        // Le dessus de la roche sous ce point (un rayon vers le bas, sur l'arène seulement).
        bool TryTop(Vector3 point, out Vector3 surface)
        {
            surface = point;
            var best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(point + Vector3.up * 4f, Vector3.down, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance >= best || hit.normal.y < 0.4f || !hit.collider.transform.IsChildOf(m_Arena.transform))
                    continue;
                best = hit.distance;
                surface = hit.point;
            }

            return best < float.MaxValue;
        }

        // Une grappe de cristaux, du bleu de la couronne au blanc.
        void AddCluster(MeshBuilder builder, Vector3 basePoint, Vector3 up, int count, float minLength, float maxLength, System.Random random)
        {
            for (var i = 0; i < count; i++)
            {
                var axis = (up + new Vector3(Range(random, -0.4f, 0.4f), 0f, Range(random, -0.4f, 0.4f))).normalized;
                var offset = new Vector3(Range(random, -0.25f, 0.25f), -0.05f, Range(random, -0.25f, 0.25f));
                var color = Color.Lerp(m_Settings.pillarCrystalColor, Color.white, Range(random, 0f, 0.45f)) * Range(random, 0.85f, 1.1f);
                color.a = 1f;
                builder.Crystal(basePoint + offset, axis, Range(random, minLength, maxLength), Range(random, 0.07f, 0.17f), 5, color,
                                Range(random, 0f, 6f), Range(random, 0f, 1f));
            }
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        // ---------------------------------------------------------------- en jeu

        /// <summary>Retrouve les lumières des piliers (celles qui sont visibles), ajoute le contour et l'abri, et les anime.</summary>
        public void Bind(MapLightingSettings settings, ArenaDirector director, ArenaShape arena, DragonBoss dragon, CeilingCrystals ceilingCrystals)
        {
            m_Settings = settings;
            m_Director = director;
            m_Arena = arena;
            m_Dragon = dragon;
            m_CeilingCrystals = ceilingCrystals;
            m_Block = new MaterialPropertyBlock();

            // La liste d'abord : les abris ajoutés en jeu sont aussi des enfants de cet objet.
            var sources = new List<ArenaShape.Pillar>(arena.Pillars);
            var roots = new List<Transform>();
            for (var i = 0; i < transform.childCount; i++)
                roots.Add(transform.GetChild(i));

            foreach (var root in roots)
            {
                if (!root.gameObject.activeInHierarchy || sources.Count == 0)
                    continue;

                // Le pilier de l'arène le plus proche de cet objet.
                var source = sources[0];
                var best = float.MaxValue;
                foreach (var candidate in sources)
                {
                    var offset = candidate.Position - root.position;
                    offset.y = 0f;
                    if (offset.sqrMagnitude >= best)
                        continue;
                    best = offset.sqrMagnitude;
                    source = candidate;
                }

                var pillar = new Pillar { Foot = source.Position, Radius = source.Radius, Height = source.Height };
                var local = arena.transform.InverseTransformPoint(pillar.Foot);
                pillar.Group = CeilingCrystals.GroupOf(Mathf.Atan2(local.x, local.z));
                pillar.TopPoint = pillar.Foot + Vector3.up * pillar.Height;

                var up = root.Find(k_UpLight);
                if (up != null && up.gameObject.activeInHierarchy && up.TryGetComponent(out pillar.Up))
                    pillar.UpIntensity = pillar.Up.intensity;
                var top = root.Find(k_TopLight);
                if (top != null && top.gameObject.activeInHierarchy && top.TryGetComponent(out pillar.Top))
                    pillar.TopIntensity = pillar.Top.intensity;
                var crystals = root.Find(k_Crystals);
                if (crystals != null && crystals.gameObject.activeInHierarchy && crystals.TryGetComponent(out pillar.Crystals))
                {
                    var material = pillar.Crystals.sharedMaterial;
                    if (material != null && material.HasProperty(k_IntensityId))
                        pillar.CrystalIntensity = material.GetFloat(k_IntensityId);
                }

                CreateCover(pillar);
                m_Pillars.Add(pillar);
            }

            AddRims();
            m_Bound = true;
        }

        // L'abri : une bande au sol derrière le pilier, recalculée à chaque image depuis la gueule du dragon.
        void CreateCover(Pillar pillar)
        {
            if (m_Settings.glowMaterial == null)
                return;

            pillar.CoverMesh = Own(new Mesh { name = "Abri du pilier" });
            pillar.CoverMesh.MarkDynamic();
            pillar.CoverVertices = new Vector3[k_CoverRows * 2];
            pillar.CoverColors = new Color[k_CoverRows * 2];
            var uvs = new Vector2[k_CoverRows * 2];
            var triangles = new int[(k_CoverRows - 1) * 6];
            for (var r = 0; r < k_CoverRows; r++)
            {
                uvs[r * 2] = new Vector2(0f, 0f);
                uvs[r * 2 + 1] = new Vector2(1f, 0f);
                if (r == k_CoverRows - 1)
                    continue;
                var i = r * 2;
                triangles[r * 6] = i;
                triangles[r * 6 + 1] = i + 2;
                triangles[r * 6 + 2] = i + 1;
                triangles[r * 6 + 3] = i + 1;
                triangles[r * 6 + 4] = i + 2;
                triangles[r * 6 + 5] = i + 3;
            }

            pillar.CoverMesh.vertices = pillar.CoverVertices;
            pillar.CoverMesh.colors = pillar.CoverColors;
            pillar.CoverMesh.uv = uvs;
            pillar.CoverMesh.triangles = triangles;
            var go = new GameObject("Abri du pilier (en jeu)");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = pillar.CoverMesh;
            pillar.Cover = go.AddComponent<MeshRenderer>();
            pillar.Cover.sharedMaterial = m_Settings.glowMaterial;
            pillar.Cover.shadowCastingMode = ShadowCastingMode.Off;
            pillar.Cover.receiveShadows = false;
            pillar.Cover.enabled = false;
        }

        T Own<T>(T owned) where T : Object
        {
            m_Owned.Add(owned);
            return owned;
        }

        // Le contour lumineux : un second matériau (additif, plus vif sur les bords) sur les piliers de l'arène, ceux du
        // plan comme ceux posés à la main.
        void AddRims()
        {
            if (m_Settings.pillarRimMaterial == null)
                return;

            m_RimMaterial = Own(new Material(m_Settings.pillarRimMaterial) { name = "Contour des piliers (en jeu)" });
            var renderers = new HashSet<Renderer>();
            var pillarMaterial = m_Arena is ArenaGridBuilder grid ? grid.PillarMaterial : null;
            if (pillarMaterial != null)
            {
                foreach (var renderer in m_Arena.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer.sharedMaterial == pillarMaterial)
                        renderers.Add(renderer);
                }
            }

            foreach (var pillar in ArenaPillar.All)
            {
                if (pillar != null && pillar.gameObject.scene == m_Arena.gameObject.scene)
                    renderers.UnionWith(pillar.GetComponentsInChildren<MeshRenderer>(true));
            }

            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                var withRim = new Material[materials.Length + 1];
                materials.CopyTo(withRim, 0);
                withRim[materials.Length] = m_RimMaterial;
                renderer.sharedMaterials = withRim;
            }
        }

        void OnDestroy()
        {
            foreach (var owned in m_Owned)
            {
                if (owned != null)
                    Destroy(owned);
            }
        }

        void OnDisable()
        {
            if (m_RimMaterial != null)
                m_RimMaterial.SetFloat(k_AmountId, 0f);
        }

        void Update()
        {
            if (!m_Bound)
                return;

            var level = m_Director != null ? m_Director.LightLevel : 1f;
            var beat = m_Director != null ? m_Director.Beat : -1.0;
            if (beat < 0.0)
                beat = Time.time * 140.0 / 60.0;
            var pulse = 0.85f + 0.3f * Mathf.Exp(-(float)(beat - System.Math.Floor(beat)) * 5f);

            // Le contour est le même pour tous les piliers : discret tant que la grotte n'est qu'en partie allumée.
            if (m_RimMaterial != null)
                m_RimMaterial.SetFloat(k_AmountId, level * level);

            foreach (var pillar in m_Pillars)
            {
                // La couronne s'embrase quand le dragon se pose sur le pilier.
                var perched = 0f;
                if (m_Dragon != null)
                {
                    var offset = m_Dragon.transform.position - pillar.TopPoint;
                    if (offset.y > -2f && new Vector2(offset.x, offset.z).magnitude < pillar.Radius + 3f)
                        perched = 0.8f + 0.2f * Mathf.Sin(Time.time * 25f);
                }

                // Chaque pilier s'allume avec le quart du plafond au-dessus de lui, et du même éclat (un flash, puis 1) ;
                // avant, ses cristaux sont cachés : même éteint, le shader des cristaux leur laisse une faible lueur.
                var lit = m_CeilingCrystals != null ? Mathf.Min(m_CeilingCrystals.GetLevel(pillar.Group), 2.5f) : level;
                var on = lit > 0.01f;
                if (pillar.Up != null)
                {
                    pillar.Up.enabled = on;
                    pillar.Up.intensity = pillar.UpIntensity * lit;
                }

                if (pillar.Top != null)
                {
                    pillar.Top.enabled = on;
                    pillar.Top.intensity = pillar.TopIntensity * lit * pulse * (1f + perched);
                }

                if (pillar.Crystals != null)
                {
                    pillar.Crystals.enabled = on;
                    m_Block.Clear();
                    m_Block.SetFloat(k_IntensityId, pillar.CrystalIntensity * lit * pulse * (1f + perched));
                    pillar.Crystals.SetPropertyBlock(m_Block);
                }
            }

            UpdateCover();
        }

        // Pendant que le dragon inspire (et pendant son souffle) : derrière chaque pilier à sa portée, la bande du sol où
        // la poitrine du joueur serait à l'abri s'éclaire en bleu. Elle s'élargit en s'éloignant (la gueule est un point)
        // et s'arrête là où le feu passerait par-dessus le pilier (dragon perché plus haut que lui) ou hors de portée.
        void UpdateCover()
        {
            var fighting = m_Director == null || m_Director.IsFighting;
            var charge = fighting && m_Dragon != null ? m_Dragon.BreathChargeProgress : -1f;
            var breathing = fighting && m_Dragon != null && m_Dragon.IsBreathing;
            var target = charge >= 0f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(charge * 1.6f)) : breathing ? 0.8f : 0f;
            m_Cover = Mathf.MoveTowards(m_Cover, target, Time.deltaTime * (target > m_Cover ? 4f : 1.5f));

            var visible = m_Cover > 0.01f && m_Dragon != null;
            var floor = m_Arena.FloorY;
            var mouth = visible ? m_Dragon.Mouth : Vector3.zero;
            var source = new Vector3(mouth.x, floor, mouth.z);
            var mouthHeight = mouth.y - floor;
            var reach = visible ? m_Dragon.BreathReach : 0f;
            foreach (var pillar in m_Pillars)
            {
                if (pillar.Cover == null)
                    continue;

                pillar.Cover.enabled = false;
                if (!visible)
                    continue;

                var foot = new Vector3(pillar.Foot.x, floor, pillar.Foot.z);
                var toPillar = foot - source;
                var distance = toPillar.magnitude;
                if (distance < pillar.Radius + 1.5f || distance - pillar.Radius > reach)
                    continue;

                var direction = toPillar / distance;
                var side = Vector3.Cross(Vector3.up, direction);
                var height = pillar.Foot.y + pillar.Height - floor;
                var far = reach + 1f;
                if (mouthHeight > height - 0.2f)
                    far = Mathf.Min(far, (distance - pillar.Radius) * (mouthHeight - k_ChestHeight) / Mathf.Max(0.05f, mouthHeight - height));
                var near = distance + pillar.Radius * 0.6f;
                if (far < near + 0.6f)
                    continue;

                for (var r = 0; r < k_CoverRows; r++)
                {
                    var t = r / (float)(k_CoverRows - 1);
                    var along = Mathf.Lerp(near, far, t);
                    var halfWidth = pillar.Radius * 0.85f * along / distance;
                    var center = source + direction * along + Vector3.up * 0.08f;
                    pillar.CoverVertices[r * 2] = transform.InverseTransformPoint(center - side * halfWidth);
                    pillar.CoverVertices[r * 2 + 1] = transform.InverseTransformPoint(center + side * halfWidth);
                    var alpha = Mathf.SmoothStep(0f, 1f, t / 0.2f) * (1f - Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f));
                    pillar.CoverColors[r * 2] = pillar.CoverColors[r * 2 + 1] = new Color(1f, 1f, 1f, Mathf.Max(alpha, 0.05f));
                }

                pillar.CoverMesh.vertices = pillar.CoverVertices;
                pillar.CoverMesh.colors = pillar.CoverColors;
                pillar.CoverMesh.RecalculateBounds();
                pillar.Cover.enabled = true;
                m_Block.Clear();
                m_Block.SetColor(k_ColorId, m_Settings.coverColor);
                m_Block.SetFloat(k_IntensityId, 2f * m_Cover);
                pillar.Cover.SetPropertyBlock(m_Block);
            }
        }
    }
}
