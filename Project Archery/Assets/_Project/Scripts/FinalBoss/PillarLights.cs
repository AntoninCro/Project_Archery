using System.Collections.Generic;
using Archery.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Les piliers de l'arène, plus visibles (ils sont en obsidienne, matériau <c>Arena_Obsidian</c>) : une lumière
    /// froide les éclaire par en dessous depuis quelques cristaux posés à leur pied, leur contour brille légèrement (on les
    /// voit de partout, même au fond du brouillard), une couronne de cristaux bleus brille à leur sommet ; et pendant que
    /// le dragon prépare son souffle, l'abri derrière chaque pilier s'éclaire en bleu au sol.
    /// </summary>
    /// <remarks>Créé par <see cref="ArenaLights"/> ; réglages dans « Resources/MapLighting », partie « Piliers de l'arène ».</remarks>
    public class PillarLights : MonoBehaviour
    {
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int k_AmountId = Shader.PropertyToID("_Amount");
        static readonly int k_TintId = Shader.PropertyToID("_Tint");

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
            public Light Top;
            public Vector3 TopPoint;
            public MeshRenderer Crystals;
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
        Material m_CrystalMaterial;
        Material m_RimMaterial;
        float m_Cover;

        public void Init(MapLightingSettings settings, ArenaDirector director, ArenaShape arena, DragonBoss dragon, CeilingCrystals ceilingCrystals)
        {
            m_CeilingCrystals = ceilingCrystals;
            m_Settings = settings;
            m_Director = director;
            m_Arena = arena;
            m_Dragon = dragon;
            m_Block = new MaterialPropertyBlock();

            if (settings.crystalMaterial != null)
            {
                m_CrystalMaterial = Own(new Material(settings.crystalMaterial) { name = "Cristaux des piliers (en jeu)" });
                m_CrystalMaterial.SetColor(k_TintId, Color.white);
            }

            // Une copie : la liste des piliers de l'arène est refaite à chaque lecture (IsFreeGround la relit).
            var random = new System.Random(131);
            foreach (var pillar in new List<ArenaShape.Pillar>(arena.Pillars))
                m_Pillars.Add(CreatePillar(pillar, random));

            AddRims();
        }

        void OnDestroy()
        {
            foreach (var owned in m_Owned)
            {
                if (owned != null)
                    Destroy(owned);
            }
        }

        T Own<T>(T owned) where T : Object
        {
            m_Owned.Add(owned);
            return owned;
        }

        Pillar CreatePillar(ArenaShape.Pillar source, System.Random random)
        {
            var pillar = new Pillar { Foot = source.Position, Radius = source.Radius, Height = source.Height };
            var local = m_Arena.transform.InverseTransformPoint(pillar.Foot);
            pillar.Group = CeilingCrystals.GroupOf(Mathf.Atan2(local.x, local.z));
            var root = new GameObject("Pilier (lumières)").transform;
            root.SetParent(transform, false);
            root.position = pillar.Foot;
            var builder = new MeshBuilder();

            // La lumière d'en bas, côté arène (ou d'un autre côté s'il y a de la lave ou une paroi), et ses cristaux.
            var toCenter = m_Arena.Center - pillar.Foot;
            toCenter.y = 0f;
            var facing = toCenter.sqrMagnitude > 0.01f ? toCenter.normalized : Vector3.forward;
            var lamp = pillar.Foot + facing * (pillar.Radius + 1.6f);
            foreach (var turn in new[] { 0f, 60f, -60f, 120f, -120f, 180f })
            {
                var candidate = pillar.Foot + Quaternion.Euler(0f, turn, 0f) * facing * (pillar.Radius + 1.6f);
                if (!m_Arena.IsFreeGround(candidate, 0.3f))
                    continue;
                lamp = candidate;
                break;
            }

            lamp.y = m_Arena.GroundHeightAt(lamp);
            AddCluster(builder, lamp - pillar.Foot, Vector3.up, 3, 0.35f, 0.8f, random);
            pillar.Up = new GameObject("Lumière du pilier").AddComponent<Light>();
            pillar.Up.transform.SetParent(root, false);
            pillar.Up.transform.position = lamp + Vector3.up * 0.4f;
            pillar.Up.transform.rotation = Quaternion.LookRotation(pillar.Foot + Vector3.up * (pillar.Height * 0.5f) - pillar.Up.transform.position);
            pillar.Up.type = LightType.Spot;
            pillar.Up.spotAngle = 70f;
            pillar.Up.innerSpotAngle = 35f;
            pillar.Up.range = pillar.Height + 6f;
            pillar.Up.color = m_Settings.pillarLightColor;
            pillar.Up.shadows = LightShadows.None;

            // La couronne : des grappes de cristaux sur le pourtour du sommet, le milieu reste libre (le dragon s'y pose).
            var top = pillar.Foot + Vector3.up * pillar.Height;
            if (TryTop(top, out var surface))
                top = surface;
            pillar.TopPoint = top;
            var clusters = 7;
            for (var i = 0; i < clusters; i++)
            {
                var angle = (i + (float)random.NextDouble() * 0.5f) * 360f / clusters;
                var outward = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                // Pas de roche sous ce point (au-delà du bord) : pas de grappe, plutôt que des cristaux en l'air.
                if (!TryTop(top + outward * (pillar.Radius * 0.7f), out var spot) || spot.y < top.y - 2f)
                    continue;
                AddCluster(builder, spot - pillar.Foot, (Vector3.up + outward * 0.4f).normalized, 2 + random.Next(3), 0.5f, 1.3f, random);
            }

            if (m_CrystalMaterial != null && builder.VertexCount > 0)
            {
                var go = new GameObject("Cristaux du pilier");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = Own(builder.ToMesh("Cristaux du pilier"));
                pillar.Crystals = go.AddComponent<MeshRenderer>();
                pillar.Crystals.sharedMaterial = m_CrystalMaterial;
                pillar.Crystals.shadowCastingMode = ShadowCastingMode.Off;
            }

            pillar.Top = new GameObject("Lueur de la couronne").AddComponent<Light>();
            pillar.Top.transform.SetParent(root, false);
            pillar.Top.transform.position = top + Vector3.up * 1.5f;
            pillar.Top.type = LightType.Point;
            pillar.Top.range = 8f;
            pillar.Top.color = m_Settings.pillarCrystalColor;
            pillar.Top.shadows = LightShadows.None;

            // L'abri : une bande au sol derrière le pilier, recalculée à chaque image depuis la gueule du dragon.
            if (m_Settings.glowMaterial != null)
            {
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
                var go = new GameObject("Abri du pilier");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = pillar.CoverMesh;
                pillar.Cover = go.AddComponent<MeshRenderer>();
                pillar.Cover.sharedMaterial = m_Settings.glowMaterial;
                pillar.Cover.shadowCastingMode = ShadowCastingMode.Off;
                pillar.Cover.receiveShadows = false;
                pillar.Cover.enabled = false;
            }

            return pillar;
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

        void Update()
        {
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
                pillar.Up.enabled = on;
                pillar.Up.intensity = m_Settings.pillarLightIntensity * lit;
                pillar.Top.enabled = on;
                pillar.Top.intensity = 1.6f * lit * pulse * (1f + perched);
                if (pillar.Crystals != null)
                {
                    pillar.Crystals.enabled = on;
                    m_Block.Clear();
                    m_Block.SetFloat(k_IntensityId, 2.2f * lit * pulse * (1f + perched));
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

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
