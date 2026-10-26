using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Combat
{
    /// <summary>Comment part une faille frappée par la main : sa cible, ses dégâts, et ce que lui donnent les améliorations.</summary>
    public struct RiftShot
    {
        public Vector3 Direction;
        public float Speed;
        public Enemy Target;
        public float Damage;
        public ShotGrade Grade;
        public Color Color;
        public AudioClip HitClip;

        /// <summary>Une faille en plus (multitir, écho, déluge) : ni points de touche ni combo.</summary>
        public bool IsExtra;

        /// <summary>Perçage : ennemis qu'elle traverse encore, tout droit, après sa cible.</summary>
        public int Pierce;

        /// <summary>Ricochet : fois où, sa cible touchée, elle repart vers l'ennemi le plus proche.</summary>
        public int Ricochets;

        /// <summary>Auto-visée : elle prend une nouvelle cible si la sienne meurt en route.</summary>
        public bool Reacquire;

        /// <summary>Déluge : petites failles en lesquelles elle se brise en vol.</summary>
        public int Splits;

        /// <summary>Foudre, explosion, glace, au premier ennemi touché.</summary>
        public SpecialArrows.Effect Effects;

        /// <summary>Failles fusionnées : elle explose au premier ennemi touché (rayon en m, dégâts au centre).</summary>
        public float BlastRadius;

        public float BlastDamage;
    }

    /// <summary>
    /// Une faille dans la matrice, ouverte par un coup de katana (refaite le 24 octobre). Elle naît dès que la lame part et
    /// s'ouvre au fil de la lame : là où elle vient de passer, une fine fissure, aussitôt écartée, puis la faille continue de
    /// s'élargir ; plus large au centre, pointue aux bouts ; ses bords ondulent, son tracé frémit et ses fissures rampent
    /// (shader <c>Archery/MatrixTear</c>). Le coup fini, elle est ouverte (<see cref="Complete"/>) et attend l'onde de choc
    /// de la main de la matrice, ou se referme en grésillant si le geste ne comptait pas (<see cref="Fizzle"/>). Deux failles
    /// qui se croisent fusionnent (<see cref="Absorb"/>) : une seule faille à plusieurs brins, qui explose à l'impact ; son
    /// brin le plus ancien peut s'en détacher (<see cref="DetachOldest"/>). Propulsée par l'onde, elle file vers
    /// sa cible comme une flèche et la suit, et toute la faille touche : chaque ennemi qu'elle traverse prend ses dégâts (une
    /// fois), par sa zone de touche (tête ×2, gemme ×3). Les améliorations des flèches lui servent aussi (<see cref="RiftShot"/>).
    /// </summary>
    /// <remarks>Créée et lancée par <see cref="Katana"/>. Elle compte comme un tir du joueur (<see cref="IPlayerShot"/>).</remarks>
    [DisallowMultipleComponent]
    public class MatrixRift : MonoBehaviour, IPlayerShot
    {
        static readonly int k_OpenId = Shader.PropertyToID("_Open");
        static readonly int k_AgeId = Shader.PropertyToID("_Age");
        static readonly int k_FadeId = Shader.PropertyToID("_Fade");
        static readonly int k_FlashId = Shader.PropertyToID("_Flash");
        static readonly int k_TintId = Shader.PropertyToID("_Tint");
        static readonly int k_SeedId = Shader.PropertyToID("_Seed");
        static readonly int k_FusionId = Shader.PropertyToID("_Fusion");
        static readonly int[] k_FuseIds =
        {
            Shader.PropertyToID("_Fuse0"), Shader.PropertyToID("_Fuse1"), Shader.PropertyToID("_Fuse2"), Shader.PropertyToID("_Fuse3"),
        };

        // Un croisement pas encore utilisé : son moment est très loin dans le futur (le shader ne le montre pas).
        static readonly Vector4 k_NoFuse = new Vector4(0f, 0f, 0f, 1e6f);

        // À la fusion, la faille bondit : +15 % de taille, puis revient, en 0,3 s.
        const float k_FusePunch = 0.15f;
        const float k_FusePunchTime = 0.3f;

        // Refermée en 0,35 s ; ratée, elle grésille en rouge et disparaît en 0,22 s.
        const float k_CloseTime = 0.35f;
        const float k_FizzleTime = 0.22f;
        static readonly Color k_FizzleTint = new Color(1f, 0.25f, 0.2f);

        // Zones de touche : des points tous les 7 cm le long de chaque brin, chacun d'un rayon de 6 cm.
        const float k_HitSpacing = 0.07f;
        const float k_HitRadius = 0.06f;

        // En vol : virage maximal (°/s) vers la cible, durée de vie (et ce que lui rend chaque ennemi traversé ou chaque
        // ricochet), temps qu'elle continue après avoir touché sa dernière cible.
        const float k_TurnRate = 540f;
        const float k_FlightLife = 3f;
        const float k_ExtraLife = 1.2f;
        const float k_AfterTarget = 0.12f;
        const float k_BreakTime = 0.3f;

        // Les petites failles du déluge : 60 % de la taille, et parties entre ces deux angles (°) de sa course.
        const float k_SplitScale = 0.6f;
        const float k_SplitSpread = 18f;

        static readonly Collider[] s_Overlaps = new Collider[32];
        static readonly RaycastHit[] s_Casts = new RaycastHit[32];
        static readonly List<Enemy> s_Exclude = new List<Enemy>();
        static readonly List<Vector2> s_ProjectedA = new List<Vector2>();
        static readonly List<Vector2> s_ProjectedB = new List<Vector2>();
        static readonly List<Vector3> s_WorldA = new List<Vector3>();
        static readonly List<Vector3> s_WorldB = new List<Vector3>();

        enum State
        {
            Growing,
            Open,
            Fizzling,
            Closing,
            Flying,
            Breaking,
        }

        readonly List<MatrixRiftMesh.Strand> m_Strands = new List<MatrixRiftMesh.Strand>();

        // Les croisements des failles fusionnées : leur point (dans le repère de la faille) et le moment de la fusion (s
        // depuis sa naissance). Le shader en fait partir une onde, et y laisse un nœud lumineux.
        readonly List<Vector4> m_Fuses = new List<Vector4>();
        readonly List<(Vector3 A, Vector3 B)> m_HitSegments = new List<(Vector3, Vector3)>();
        readonly HashSet<Health> m_Damaged = new HashSet<Health>();
        readonly Dictionary<Health, (Hitbox Hitbox, Vector3 Point)> m_StepHits = new Dictionary<Health, (Hitbox, Vector3)>();
        readonly HashSet<Object> m_Triggered = new HashSet<Object>();
        readonly Dictionary<Object, (IArrowHitHandler Handler, Collider Collider, Vector3 Point)> m_StepOthers =
            new Dictionary<Object, (IArrowHitHandler, Collider, Vector3)>();

        Mesh m_Mesh;
        MeshRenderer m_Renderer;
        MaterialPropertyBlock m_Block;
        State m_State;
        Color m_Tint = Color.white;
        float m_Open = 1f;
        float m_Flash;
        float m_Fade = 1f;
        float m_StateTime;
        float m_BornAt;
        float m_OpenedAt;
        float m_BreakScale = 1f;
        float m_Seed;
        bool m_Dirty;
        float m_PunchTime = -1f;
        Vector3 m_PunchScale = Vector3.one;

        // En vol.
        RiftShot m_Shot;
        Vector3 m_Direction;
        Enemy m_Target;
        bool m_Straight;
        bool m_Impacted;
        float m_Life;
        float m_SplitAt = -1f;
        Vector3 m_Start;
        float m_EndTime = -1f;
        Pose m_PreviousPose;

        public bool IsGrowing => m_State == State.Growing;

        /// <summary>Ouverte, en attente de la main de la matrice.</summary>
        public bool IsOpen => m_State == State.Open;

        public bool IsFlying => m_State == State.Flying;

        public bool IsExtra => m_Shot.IsExtra;

        /// <summary>Temps (s) depuis que le coup est fini et qu'elle attend (0 pendant le coup).</summary>
        public float Age => m_State == State.Open ? Time.time - m_OpenedAt : 0f;

        /// <summary>Longueur (m) de tous ses brins : sa taille, qui donne la qualité du tir.</summary>
        public float Length
        {
            get
            {
                var total = 0f;
                foreach (var strand in m_Strands)
                    total += strand.Length;
                return total;
            }
        }

        /// <summary>Nombre de failles fusionnées en elle (1 : une seule).</summary>
        public int StrandCount => m_Strands.Count;

        /// <summary>
        /// Une faille qui naît au point <paramref name="worldPoint"/> de la lame et suivra le coup (<see cref="Extend"/>),
        /// dans le repère de <paramref name="parent"/> (le XR Origin : elle suit le joueur), regardée depuis
        /// <paramref name="viewer"/> (ses yeux).
        /// </summary>
        public static MatrixRift Begin(Transform parent, Vector3 worldPoint, Vector3 viewer, Material material, int seed)
        {
            var go = new GameObject("Faille du katana");
            go.transform.SetPositionAndRotation(worldPoint, FacingRotation(worldPoint, viewer));
            if (parent != null)
                go.transform.SetParent(parent, true);
            var rift = go.AddComponent<MatrixRift>();
            rift.Setup(material, seed);
            var strand = new MatrixRiftMesh.Strand { Seed = seed, Viewer = go.transform.InverseTransformPoint(viewer) };
            strand.Path.Add(Vector3.zero);
            strand.Births.Add(0f);
            rift.m_Strands.Add(strand);
            rift.m_State = State.Growing;
            rift.m_Dirty = true;
            return rift;
        }

        void Setup(Material material, int seed)
        {
            var random = new System.Random(seed);
            m_Seed = (float)random.NextDouble();
            m_Mesh = new Mesh { name = "Faille du katana" };
            m_Mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = m_Mesh;
            m_Renderer = gameObject.AddComponent<MeshRenderer>();
            m_Renderer.sharedMaterial = material;
            m_Renderer.shadowCastingMode = ShadowCastingMode.Off;
            m_Renderer.receiveShadows = false;
            m_Block = new MaterialPropertyBlock();
            m_BornAt = Time.time;
        }

        void OnDestroy()
        {
            if (m_Mesh != null)
                Destroy(m_Mesh);
        }

        // Face au joueur, la tête en haut.
        static Quaternion FacingRotation(Vector3 point, Vector3 viewer)
        {
            var forward = point - viewer;
            if (forward.sqrMagnitude < 1e-6f)
                forward = Vector3.forward;
            var up = Vector3.ProjectOnPlane(Vector3.up, forward);
            if (up.sqrMagnitude < 1e-4f)
                up = Vector3.ProjectOnPlane(Vector3.forward, forward);
            return Quaternion.LookRotation(forward, up);
        }

        /// <summary>Pendant le coup : la lame est passée en <paramref name="worldPoint"/> ; la faille s'y allonge.</summary>
        public void Extend(Vector3 worldPoint, Vector3 viewer)
        {
            if (m_State != State.Growing || m_Strands.Count == 0)
                return;
            var strand = m_Strands[0];
            strand.Path.Add(transform.InverseTransformPoint(worldPoint));
            strand.Births.Add(Time.time - m_BornAt);
            strand.Viewer = transform.InverseTransformPoint(viewer);
            m_Dirty = true;
        }

        /// <summary>Le coup est fini et comptait : la faille est ouverte, et attend la main.</summary>
        public void Complete(Vector3 viewer)
        {
            if (m_State != State.Growing)
                return;
            Recenter(viewer);
            m_State = State.Open;
            m_OpenedAt = Time.time;
            m_Flash = Mathf.Max(m_Flash, 0.5f);
        }

        /// <summary>Le coup ne comptait pas (trop court, un aller-retour) : la fissure grésille en rouge et se referme.</summary>
        public void Fizzle()
        {
            if (m_State != State.Growing)
                return;
            m_State = State.Fizzling;
            m_StateTime = 0f;
            m_Tint = k_FizzleTint;
            m_Flash = 0.8f;
        }

        /// <summary>La couleur de ses bords : la qualité qu'aurait le tir (puis celle du tir lancé).</summary>
        public void SetTint(Color color) => m_Tint = color;

        /// <summary>La plus petite distance (m) entre <paramref name="worldPoint"/> et un de ses brins.</summary>
        public float DistanceTo(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            var best = float.MaxValue;
            foreach (var strand in m_Strands)
            {
                var spine = strand.Spine;
                for (var i = 1; i < spine.Count; i++)
                    best = Mathf.Min(best, DistanceToSegment(local, spine[i - 1], spine[i]));
            }

            return best * transform.lossyScale.x;
        }

        /// <summary>Le moment (Time.time) où la lame a ouvert son brin le plus ancien.</summary>
        public float OldestStrandTime => m_BornAt + OldestBirth(out _);

        float OldestBirth(out int index)
        {
            index = -1;
            var oldest = float.MaxValue;
            for (var i = 0; i < m_Strands.Count; i++)
            {
                var births = m_Strands[i].Births;
                if (births.Count > 0 && births[0] < oldest)
                {
                    oldest = births[0];
                    index = i;
                }
            }

            return index >= 0 ? oldest : 0f;
        }

        /// <summary>
        /// Le brin le plus ancien quitte la fusion (24 octobre : 5 failles en tout, une nouvelle referme la plus ancienne) :
        /// il devient une faille à part, ouverte, à la même place, que l'appelant referme ; la fusion garde les autres brins,
        /// et ses croisements sauf celui de ce brin. Null si elle n'a qu'un brin.
        /// </summary>
        public MatrixRift DetachOldest(Vector3 viewer)
        {
            OldestBirth(out var index);
            if (m_Strands.Count < 2 || index < 0)
                return null;
            var strand = m_Strands[index];
            m_Strands.RemoveAt(index);

            // Son croisement : celui qui est le plus près de son tracé.
            var nearest = -1;
            var best = float.MaxValue;
            for (var i = 0; i < m_Fuses.Count; i++)
            {
                var point = (Vector3)m_Fuses[i];
                for (var k = 1; k < strand.Spine.Count; k++)
                {
                    var distance = DistanceToSegment(point, strand.Spine[k - 1], strand.Spine[k]);
                    if (distance < best)
                    {
                        best = distance;
                        nearest = i;
                    }
                }
            }

            if (nearest >= 0)
                m_Fuses.RemoveAt(nearest);

            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            go.transform.localScale = transform.lossyScale;
            if (transform.parent != null)
                go.transform.SetParent(transform.parent, true);
            var rift = go.AddComponent<MatrixRift>();
            rift.Setup(m_Renderer.sharedMaterial, strand.Seed);
            rift.m_BornAt = m_BornAt;
            rift.m_Strands.Add(strand);
            rift.m_Tint = m_Tint;
            rift.m_State = State.Open;
            rift.m_OpenedAt = Time.time;
            rift.Rebuild();

            Recenter(viewer);
            return rift;
        }

        /// <summary>
        /// Vue depuis <paramref name="viewer"/> (regard <paramref name="forward"/>), un de ses brins croise un brin de
        /// <paramref name="other"/> : elles peuvent fusionner, quelle que soit leur forme. <paramref name="crossing"/> : le
        /// croisement, dans le monde (à mi-chemin entre les deux brins).
        /// </summary>
        public bool Crosses(MatrixRift other, Vector3 viewer, Vector3 forward, out Vector3 crossing)
        {
            crossing = transform.position;
            if (other == null || other == this)
                return false;
            var basis = Quaternion.LookRotation(forward, Vector3.up);
            var inverse = Quaternion.Inverse(basis);
            foreach (var strand in m_Strands)
            {
                Project(strand.Spine, transform, viewer, inverse, s_ProjectedA, s_WorldA);
                foreach (var otherStrand in other.m_Strands)
                {
                    Project(otherStrand.Spine, other.transform, viewer, inverse, s_ProjectedB, s_WorldB);
                    if (Intersect(s_ProjectedA, s_ProjectedB, out var i, out var ta, out var j, out var tb))
                    {
                        crossing = 0.5f * (Vector3.Lerp(s_WorldA[i - 1], s_WorldA[i], ta) + Vector3.Lerp(s_WorldB[j - 1], s_WorldB[j], tb));
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// La fusion : <paramref name="other"/> devient un brin de plus de cette faille (et disparaît), qui se rouvre (sa
        /// durée repart). Elle se voit : un éclat, la faille bondit, et une onde de lumière part du croisement
        /// (<paramref name="worldCrossing"/>) le long de tous ses brins ; un nœud lumineux y reste.
        /// </summary>
        public void Absorb(MatrixRift other, Vector3 viewer, Vector3 worldCrossing)
        {
            if (other == null || other == this)
                return;
            var toHere = transform.worldToLocalMatrix * other.transform.localToWorldMatrix;
            var shift = other.m_BornAt - m_BornAt;
            foreach (var strand in other.m_Strands)
            {
                var copy = strand.Copy();
                copy.Transform(toHere, shift);
                m_Strands.Add(copy);
            }

            // Ses croisements à elle (si elle était déjà fusionnée) viennent avec elle.
            foreach (var fuse in other.m_Fuses)
            {
                var point = toHere.MultiplyPoint3x4(fuse);
                m_Fuses.Add(new Vector4(point.x, point.y, point.z, fuse.w + shift));
            }

            Destroy(other.gameObject);
            var crossing = transform.InverseTransformPoint(worldCrossing);
            m_Fuses.Add(new Vector4(crossing.x, crossing.y, crossing.z, Time.time - m_BornAt));
            Recenter(viewer);
            m_OpenedAt = Time.time;
            m_Flash = 1f;
            if (m_PunchTime < 0f)
                m_PunchScale = transform.localScale;
            m_PunchTime = 0f;
        }

        /// <summary>Une copie ouverte de cette faille, à la même place (multitir, écho, déluge), à <paramref name="scale"/> de sa taille.</summary>
        public MatrixRift Clone(float scale = 1f)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            go.transform.localScale = transform.lossyScale * scale;
            var rift = go.AddComponent<MatrixRift>();
            rift.Setup(m_Renderer.sharedMaterial, Random.Range(1, 1 << 20));
            rift.m_BornAt = m_BornAt;
            foreach (var strand in m_Strands)
                rift.m_Strands.Add(strand.Copy());
            rift.m_Fuses.AddRange(m_Fuses);
            rift.m_Tint = m_Tint;
            rift.m_State = State.Open;
            rift.m_OpenedAt = Time.time;
            rift.Rebuild();
            return rift;
        }

        /// <summary>Elle part (<paramref name="shot"/>) et suit sa cible ; elle fait ses dégâts à chaque ennemi qu'elle traverse.</summary>
        public void Launch(RiftShot shot)
        {
            if (m_State != State.Open)
                return;

            // Jusqu'ici, elle suivait le joueur ; lancée, elle vole dans le monde.
            transform.SetParent(null, true);
            m_State = State.Flying;
            m_StateTime = 0f;
            m_Shot = shot;
            m_Direction = shot.Direction.sqrMagnitude > 1e-6f ? shot.Direction.normalized : transform.forward;
            m_Target = shot.Target;
            m_Tint = shot.Color;
            m_Life = k_FlightLife;
            m_Start = transform.position;
            m_PreviousPose = new Pose(transform.position, transform.rotation);
            m_Flash = 1f;
            m_Open = Mathf.Max(m_Open, 1.12f);
            if (shot.Splits > 0 && SpecialArrows.Instance != null)
            {
                var range = SpecialArrows.Instance.DelugeSplitTime;
                m_SplitAt = Random.Range(range.x, range.y);
            }
        }

        /// <summary>Pas frappée à temps (ou l'arme rangée) : la plaie se referme et disparaît.</summary>
        public void Close()
        {
            if (m_State == State.Growing)
            {
                Fizzle();
                return;
            }

            if (m_State != State.Open)
                return;
            m_State = State.Closing;
            m_StateTime = 0f;
        }

        /// <summary>Elle se brise et disparaît (fin de course).</summary>
        public void Break()
        {
            if (m_State == State.Breaking)
                return;
            m_State = State.Breaking;
            m_StateTime = 0f;
            m_Flash = 1f;
            m_BreakScale = transform.localScale.x;
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_StateTime += deltaTime;
            m_Flash = Mathf.MoveTowards(m_Flash, 0f, deltaTime * 3f);

            // La fusion la fait bondir : elle grossit d'un coup, puis revient à sa taille.
            if (m_PunchTime >= 0f)
            {
                m_PunchTime += deltaTime;
                var t = Mathf.Clamp01(m_PunchTime / k_FusePunchTime);
                if (m_State == State.Open)
                    transform.localScale = m_PunchScale * (1f + k_FusePunch * Mathf.Sin(Mathf.PI * t) * (1f - t * 0.5f));
                if (t >= 1f || m_State != State.Open)
                {
                    if (m_State != State.Breaking)
                        transform.localScale = m_PunchScale;
                    m_PunchTime = -1f;
                }
            }

            switch (m_State)
            {
                case State.Fizzling:
                    m_Open = Mathf.MoveTowards(m_Open, 0f, deltaTime / k_FizzleTime);
                    m_Fade = Mathf.Clamp01(m_Open * 2f);
                    m_Flash = Mathf.Max(m_Flash, 0.4f + 0.4f * Mathf.Sin(m_StateTime * 80f));
                    if (m_Open <= 0f)
                        Destroy(gameObject);
                    break;
                case State.Closing:
                    m_Open = Mathf.MoveTowards(m_Open, 0f, deltaTime / k_CloseTime);
                    m_Fade = Mathf.Clamp01(m_Open * 1.5f);
                    if (m_Open <= 0f)
                        Destroy(gameObject);
                    break;
                case State.Flying:
                    m_Open = Mathf.MoveTowards(m_Open, 1f, deltaTime * 2f);
                    break;
                case State.Breaking:
                    m_Fade = Mathf.Clamp01(1f - m_StateTime / k_BreakTime);
                    m_Flash = Mathf.Max(m_Flash, m_Fade);
                    transform.localScale = Vector3.one * (m_BreakScale * (1f + 0.25f * (1f - m_Fade)));
                    if (m_Fade <= 0f)
                        Destroy(gameObject);
                    break;
            }
        }

        void LateUpdate()
        {
            if (m_Dirty)
                Rebuild();
            ApplyBlock();
        }

        void Rebuild()
        {
            m_Dirty = false;
            MatrixRiftMesh.Build(m_Mesh, m_Strands);
            BuildHitSegments();
        }

        // Le pivot au milieu de ses brins, face au joueur ; les brins ne bougent pas dans le monde.
        void Recenter(Vector3 viewer)
        {
            Rebuild();
            var center = Vector3.zero;
            var count = 0;
            foreach (var strand in m_Strands)
            {
                foreach (var point in strand.Spine)
                {
                    center += point;
                    count++;
                }
            }

            if (count == 0)
                return;
            var worldCenter = transform.TransformPoint(center / count);
            var before = transform.localToWorldMatrix;
            transform.SetPositionAndRotation(worldCenter, FacingRotation(worldCenter, viewer));
            var toNew = transform.worldToLocalMatrix * before;
            foreach (var strand in m_Strands)
                strand.Transform(toNew, 0f);
            for (var i = 0; i < m_Fuses.Count; i++)
            {
                var point = toNew.MultiplyPoint3x4(m_Fuses[i]);
                m_Fuses[i] = new Vector4(point.x, point.y, point.z, m_Fuses[i].w);
            }

            Rebuild();
        }

        void FixedUpdate()
        {
            if (m_State != State.Flying)
                return;

            var deltaTime = Time.fixedDeltaTime;
            if (m_Target != null && !m_Target.IsAlive)
                m_Target = null;

            // Auto-visée : sa cible morte en route, elle en prend une autre devant elle.
            if (m_Target == null && m_Shot.Reacquire && !m_Straight && m_EndTime < 0f)
                m_Target = FindAhead();

            // Elle suit sa cible, et son plan reste face à sa course : toute la faille balaie l'air devant elle.
            if (m_Target != null && m_EndTime < 0f && !m_Straight)
            {
                var toTarget = m_Target.Center - transform.position;
                if (toTarget.sqrMagnitude > 1e-4f)
                    m_Direction = Vector3.RotateTowards(m_Direction, toTarget.normalized, k_TurnRate * Mathf.Deg2Rad * deltaTime, 0f);
                if (toTarget.magnitude < 0.7f)
                    OnTargetReached();
            }

            m_PreviousPose = new Pose(transform.position, transform.rotation);
            var rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(m_Direction, Vector3.up), 1f - Mathf.Exp(-12f * deltaTime));
            transform.SetPositionAndRotation(transform.position + m_Direction * (m_Shot.Speed * deltaTime), rotation);
            Sweep();

            if (m_SplitAt >= 0f && m_StateTime >= m_SplitAt)
                Split();

            if (m_StateTime > m_Life || (m_EndTime >= 0f && Time.time >= m_EndTime))
                Break();
        }

        // Sa cible atteinte : un ricochet la renvoie vers l'ennemi le plus proche ; le perçage la fait continuer tout droit ;
        // sinon, elle la traverse encore un instant, puis se brise.
        void OnTargetReached()
        {
            if (m_EndTime >= 0f || m_Straight)
                return;

            if (m_Shot.Ricochets > 0)
            {
                s_Exclude.Clear();
                foreach (var health in m_Damaged)
                {
                    var enemy = health != null ? health.GetComponentInParent<Enemy>() : null;
                    if (enemy != null)
                        s_Exclude.Add(enemy);
                }

                if (m_Target != null)
                    s_Exclude.Add(m_Target);
                var range = SpecialArrows.Instance != null ? SpecialArrows.Instance.RicochetRange : 12f;
                var next = SpecialArrows.NearestEnemy(transform.position, range, s_Exclude);
                if (next != null)
                {
                    m_Shot.Ricochets--;
                    m_Target = next;
                    m_Life = m_StateTime + k_ExtraLife;
                    m_Flash = 1f;
                    return;
                }
            }

            if (m_Shot.Pierce > 0)
            {
                m_Straight = true;
                m_Target = null;
                m_Life = m_StateTime + k_ExtraLife;
                return;
            }

            m_EndTime = Time.time + k_AfterTarget;
        }

        // L'ennemi le plus proche devant elle, pour l'auto-visée.
        Enemy FindAhead()
        {
            var arrows = SpecialArrows.Instance;
            var range = arrows != null ? arrows.HomingRange : 12f;
            var angle = arrows != null ? arrows.HomingAngle : 35f;
            Enemy best = null;
            var bestDistance = range;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || !enemy.IsAlive || m_Damaged.Contains(enemy.Health))
                    continue;
                var offset = enemy.Center - transform.position;
                var distance = offset.magnitude;
                if (distance < bestDistance && Vector3.Angle(m_Direction, offset) <= angle)
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }

            return best;
        }

        // Le déluge : elle se brise en petites failles qui visent chacune un autre ennemi, en éventail sinon.
        void Split()
        {
            var count = m_Shot.Splits;
            m_SplitAt = -1f;
            m_Shot.Splits = 0;
            if (count <= 0)
                return;

            s_Exclude.Clear();
            if (m_Target != null)
                s_Exclude.Add(m_Target);
            for (var i = 0; i < count; i++)
            {
                var piece = Clone(k_SplitScale);
                var target = SpecialArrows.NearestEnemy(transform.position, 30f, s_Exclude);
                if (target != null)
                    s_Exclude.Add(target);
                var side = (i % 2 == 0 ? 1f : -1f) * k_SplitSpread * (1 + i / 2);
                var direction = Quaternion.AngleAxis(side, Vector3.up) * m_Direction;
                var shot = m_Shot;
                shot.Direction = target != null ? (target.Center - transform.position).normalized : direction;
                shot.Target = target;
                shot.IsExtra = true;
                shot.Splits = 0;
                shot.Effects = SpecialArrows.Instance != null ? SpecialArrows.Instance.RollEffects(shot.Grade) : SpecialArrows.Effect.None;
                shot.BlastRadius *= 0.5f;
                shot.BlastDamage *= 0.5f;
                piece.Launch(shot);
            }
        }

        // Toute la faille touche : pour chaque bout de chaque brin, l'espace balayé depuis le pas physique d'avant (et là où
        // il est).
        void Sweep()
        {
            m_StepHits.Clear();
            m_StepOthers.Clear();
            var previous = Matrix4x4.TRS(m_PreviousPose.position, m_PreviousPose.rotation, transform.lossyScale);
            var current = transform.localToWorldMatrix;
            var travel = transform.position - m_PreviousPose.position;
            var distance = travel.magnitude;
            var direction = distance > 1e-5f ? travel / distance : m_Direction;
            var radius = k_HitRadius * Mathf.Max(0.5f, transform.lossyScale.x);

            foreach (var (a, b) in m_HitSegments)
            {
                if (distance > 1e-5f)
                {
                    var count = Physics.CapsuleCastNonAlloc(previous.MultiplyPoint3x4(a), previous.MultiplyPoint3x4(b), radius, direction,
                                                            s_Casts, distance, ~0, QueryTriggerInteraction.Collide);
                    for (var k = 0; k < count; k++)
                        Consider(s_Casts[k].collider, s_Casts[k].point.sqrMagnitude > 0f ? s_Casts[k].point : current.MultiplyPoint3x4(a));
                }

                var overlaps = Physics.OverlapCapsuleNonAlloc(current.MultiplyPoint3x4(a), current.MultiplyPoint3x4(b), radius, s_Overlaps,
                                                              ~0, QueryTriggerInteraction.Collide);
                for (var k = 0; k < overlaps; k++)
                    Consider(s_Overlaps[k], s_Overlaps[k].ClosestPoint(current.MultiplyPoint3x4((a + b) * 0.5f)));
            }

            foreach (var pair in m_StepHits)
                Damage(pair.Key, pair.Value.Hitbox, pair.Value.Point);

            foreach (var pair in m_StepOthers)
            {
                if (pair.Key == null)
                    continue;
                m_Triggered.Add(pair.Key);
                pair.Value.Handler.OnArrowHit(HitAt(pair.Value.Collider, pair.Value.Point));
            }
        }

        ArrowHit HitAt(Collider collider, Vector3 point) => new ArrowHit
        {
            Arrow = null,
            Collider = collider,
            Point = point,
            Normal = -m_Direction,
            Direction = m_Direction,
            Speed = m_Shot.Speed,
            Damage = m_Shot.Damage,
            Grade = m_Shot.Grade,
            TravelDistance = Vector3.Distance(m_Start, point),
            IsShot = true,
        };

        // La meilleure zone de touche de chaque ennemi touché sur ce pas (tête avant corps).
        void Consider(Collider collider, Vector3 point)
        {
            if (collider == null)
                return;
            if (!collider.TryGetComponent(out Hitbox hitbox))
            {
                ConsiderOther(collider, point);
                return;
            }

            var owner = hitbox.Owner;
            if (owner == null || !owner.IsAlive || m_Damaged.Contains(owner))
                return;
            if (owner.GetComponentInParent<Enemy>() == null && owner.GetComponentInParent<Boss>() == null)
                return;

            if (m_StepHits.TryGetValue(owner, out var best) && Multiplier(best.Hitbox) >= Multiplier(hitbox))
                return;
            m_StepHits[owner] = (hitbox, point);
        }

        // Ce qui réagit aux flèches sans être un ennemi (23 octobre : au katana, on ne pouvait pas lancer de vague) : le gong,
        // les cibles d'entraînement, les cibles de la victoire contre MK, une boule de feu ou un projectile. La faille le
        // touche une fois, comme une flèche tirée. Pas les boutons des menus ni les panneaux de difficulté, que le laser
        // traverse aussi.
        void ConsiderOther(Collider collider, Vector3 point)
        {
            var handler = collider.GetComponentInParent<IArrowHitHandler>();
            if (handler == null || handler is Hitbox || handler is ILaserTransparent || !(handler is Object key) || key == null)
                return;
            if (m_Triggered.Contains(key) || m_StepOthers.ContainsKey(key))
                return;
            m_StepOthers[key] = (handler, collider, point);
        }

        static float Multiplier(Hitbox hitbox) =>
            hitbox.DamageMultiplier * (hitbox.Zone == HitZone.Head ? Hitbox.HeadDamageMultiplier : 1f);

        void Damage(Health owner, Hitbox hitbox, Vector3 point)
        {
            m_Damaged.Add(owner);
            var enemy = owner.GetComponentInParent<Enemy>();
            var hit = HitAt(hitbox.GetComponent<Collider>(), point);
            owner.TakeDamage(new DamageInfo
            {
                Amount = m_Shot.Damage * Multiplier(hitbox),
                Zone = hitbox.Zone,
                Grade = m_Shot.Grade,
                Distance = hit.TravelDistance,
                Point = point,
                Direction = m_Direction,
                Source = this,
            });
            Sfx.Play(m_Shot.HitClip, point, 0.9f, Random.Range(0.9f, 1.1f));
            m_Flash = 1f;

            // Au premier ennemi touché : les effets des améliorations (foudre, explosion, glace), et l'explosion des
            // failles fusionnées.
            var firstImpact = !m_Impacted;
            if (!m_Impacted)
            {
                m_Impacted = true;
                var arrows = SpecialArrows.Instance;
                if (arrows != null)
                {
                    if (m_Shot.Effects != SpecialArrows.Effect.None)
                        arrows.Impact(m_Shot.Effects, hit, enemy);
                    if (m_Shot.BlastRadius > 0f)
                        arrows.Blast(point, m_Shot.BlastDamage, m_Shot.BlastRadius, m_Tint, hit);
                }
            }

            // Les pièces de la flèche qui ont un sens pour une faille (refonte roguelike, étape 5, 26 octobre) : pointe de givre,
            // de chasseur, bourreau ; et à son premier ennemi, la mèche et l'aimant.
            if (enemy != null && ArrowPieces.Instance != null)
                ArrowPieces.Instance.OnRiftHit(enemy, hit, hitbox.Zone, firstImpact);

            // Sa cible touchée : ricochet, perçage, ou elle la traverse encore un instant, puis se brise. En perçage, chaque
            // ennemi traversé en use un.
            if (m_Straight)
            {
                if (--m_Shot.Pierce <= 0 && m_EndTime < 0f)
                    m_EndTime = Time.time + k_AfterTarget;
            }
            else if (m_Target != null && enemy == m_Target)
            {
                OnTargetReached();
            }
        }

        // Les bouts qui servent de zone de touche : tous les 7 cm le long de chaque brin.
        void BuildHitSegments()
        {
            m_HitSegments.Clear();
            foreach (var strand in m_Strands)
            {
                var spine = strand.Spine;
                if (spine.Count < 2)
                    continue;
                var last = spine[0];
                var carried = 0f;
                for (var i = 1; i < spine.Count; i++)
                {
                    carried += Vector3.Distance(spine[i - 1], spine[i]);
                    if (carried >= k_HitSpacing || i == spine.Count - 1)
                    {
                        m_HitSegments.Add((last, spine[i]));
                        last = spine[i];
                        carried = 0f;
                    }
                }
            }
        }

        void ApplyBlock()
        {
            if (m_Renderer == null || m_Block == null)
                return;
            m_Block.SetFloat(k_OpenId, m_Open);
            m_Block.SetFloat(k_AgeId, Time.time - m_BornAt);
            m_Block.SetFloat(k_FadeId, m_Fade);
            m_Block.SetFloat(k_FlashId, m_Flash);
            m_Block.SetColor(k_TintId, m_Tint);
            m_Block.SetFloat(k_SeedId, m_Seed);
            m_Block.SetFloat(k_FusionId, Mathf.Clamp01((m_Strands.Count - 1) / 4f));
            for (var i = 0; i < k_FuseIds.Length; i++)
                m_Block.SetVector(k_FuseIds[i], i < m_Fuses.Count ? m_Fuses[i] : k_NoFuse);
            m_Renderer.SetPropertyBlock(m_Block);
        }

        // Les points d'un brin, vus depuis viewer : leur place sur un plan face au regard (rotation inverse du regard), et
        // leur place dans le monde.
        static void Project(List<Vector3> spine, Transform frame, Vector3 viewer, Quaternion inverseView, List<Vector2> result,
                            List<Vector3> world)
        {
            result.Clear();
            world.Clear();
            for (var i = 0; i < spine.Count; i += 2)
            {
                var point = frame.TransformPoint(spine[i]);
                var view = inverseView * (point - viewer);
                if (view.z <= 0.05f)
                    continue;
                result.Add(new Vector2(view.x / view.z, view.y / view.z));
                world.Add(point);
            }
        }

        // Le premier croisement entre les deux tracés : les segments (i - 1, i) et (j - 1, j), et la place du croisement le
        // long de chacun (0 à 1).
        static bool Intersect(List<Vector2> a, List<Vector2> b, out int i, out float ta, out int j, out float tb)
        {
            for (i = 1; i < a.Count; i++)
            {
                for (j = 1; j < b.Count; j++)
                {
                    if (SegmentsCross(a[i - 1], a[i], b[j - 1], b[j], out ta, out tb))
                        return true;
                }
            }

            i = j = 0;
            ta = tb = 0f;
            return false;
        }

        static bool SegmentsCross(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2, out float tp, out float tq)
        {
            static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
            var d1 = Cross(q1, q2, p1);
            var d2 = Cross(q1, q2, p2);
            var d3 = Cross(p1, p2, q1);
            var d4 = Cross(p1, p2, q2);
            tp = d1 / (d1 - d2 + 1e-12f);
            tq = d3 / (d3 - d4 + 1e-12f);
            return d1 * d2 < 0f && d3 * d4 < 0f;
        }

        static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var segment = b - a;
            var length = segment.sqrMagnitude;
            var t = length > 1e-10f ? Mathf.Clamp01(Vector3.Dot(point - a, segment) / length) : 0f;
            return Vector3.Distance(point, a + segment * t);
        }
    }
}
