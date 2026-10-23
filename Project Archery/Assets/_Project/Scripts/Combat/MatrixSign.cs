using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Combat
{
    /// <summary>
    /// Un signe tracé au katana (22 octobre) : les fissures que la lame a ouvertes dans la matrice, trait après trait, à
    /// plat dans son plan (X à droite, Y en haut, Z vers l'avant). Terminé, il attend que le gantelet le touche ; lancé, il
    /// file vers sa cible comme une flèche et la suit, et tout le signe touche : chaque ennemi qu'il traverse prend ses
    /// dégâts (une fois), par sa zone de touche (tête ×2, gemme ×3). Il se brise sur sa cible, ou au bout de sa course.
    /// </summary>
    /// <remarks>Créé et rempli par <see cref="KatanaGauntlet"/>.</remarks>
    [DisallowMultipleComponent]
    public class MatrixSign : MonoBehaviour
    {
        static readonly int k_FadeId = Shader.PropertyToID("_Fade");
        static readonly int k_FlashId = Shader.PropertyToID("_Flash");

        // Demi-largeur (m) d'une fissure en son milieu ; écart entre les points qui servent de zone de touche.
        const float k_HalfWidth = 0.02f;
        const float k_HitSpacing = 0.07f;
        const float k_HitRadius = 0.05f;

        // En vol : virage maximal (°/s) vers la cible, durée de vie, et temps qu'il continue après avoir touché sa cible.
        const float k_TurnRate = 540f;
        const float k_FlightLife = 3f;
        const float k_AfterTarget = 0.12f;
        const float k_BreakTime = 0.3f;

        static readonly Collider[] s_Overlaps = new Collider[32];
        static readonly RaycastHit[] s_Casts = new RaycastHit[32];

        enum State
        {
            Drawing,
            Ready,
            Flying,
            Breaking,
        }

        sealed class Stroke
        {
            public List<Vector3> Points;
            public int Seed;
        }

        readonly List<Stroke> m_Strokes = new List<Stroke>();
        readonly List<Vector3> m_HitPoints = new List<Vector3>();
        readonly List<int> m_HitStarts = new List<int>();
        readonly HashSet<Health> m_Damaged = new HashSet<Health>();
        readonly Dictionary<Health, (Hitbox Hitbox, Vector3 Point)> m_StepHits = new Dictionary<Health, (Hitbox, Vector3)>();
        readonly MatrixCrackMesh m_Builder = new MatrixCrackMesh();

        Mesh m_Mesh;
        MeshRenderer m_Renderer;
        MaterialPropertyBlock m_Block;
        State m_State;
        Color m_Color = Color.white;
        float m_Flash;
        float m_Fade = 1f;
        float m_StateTime;
        float m_BreakScale = 1f;
        int m_Seed;

        // En vol.
        Vector3 m_Direction;
        float m_Speed;
        Enemy m_Target;
        float m_Damage;
        ShotGrade m_Grade;
        Object m_Source;
        AudioClip m_HitClip;
        Vector3 m_Start;
        float m_EndTime = -1f;
        Pose m_PreviousPose;

        public bool IsReady => m_State == State.Ready;
        public bool IsFlying => m_State == State.Flying;
        public int StrokeCount => m_Strokes.Count;

        /// <summary>Un signe vide, posé à <paramref name="pose"/> (son plan face au joueur, Z vers l'avant).</summary>
        public static MatrixSign Create(Pose pose, Material material, int seed)
        {
            var go = new GameObject("Signe du katana");
            go.transform.SetPositionAndRotation(pose.position, pose.rotation);
            var sign = go.AddComponent<MatrixSign>();
            sign.m_Seed = seed;
            sign.m_Mesh = new Mesh { name = "Signe du katana" };
            sign.m_Mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = sign.m_Mesh;
            sign.m_Renderer = go.AddComponent<MeshRenderer>();
            sign.m_Renderer.sharedMaterial = material;
            sign.m_Renderer.shadowCastingMode = ShadowCastingMode.Off;
            sign.m_Renderer.receiveShadows = false;
            sign.m_Block = new MaterialPropertyBlock();
            sign.ApplyBlock();
            return sign;
        }

        void OnDestroy()
        {
            if (m_Mesh != null)
                Destroy(m_Mesh);
        }

        /// <summary>Un trait de plus : la fissure suit <paramref name="worldPoints"/>, ramenés dans le plan du signe.</summary>
        public void AddStroke(IReadOnlyList<Vector3> worldPoints)
        {
            if (m_State != State.Drawing || worldPoints.Count < 2)
                return;

            var points = new List<Vector3>(worldPoints.Count);
            foreach (var point in worldPoints)
            {
                var local = transform.InverseTransformPoint(point);
                local.z = 0f;
                points.Add(local);
            }

            m_Strokes.Add(new Stroke { Points = points, Seed = m_Seed * 31 + m_Strokes.Count * 7919 });
            Rebuild();
            m_Flash = 1f;
        }

        /// <summary>Le signe est complet : il prend la couleur de sa qualité et attend le gantelet.</summary>
        public void SetReady(Color color)
        {
            m_State = State.Ready;
            m_StateTime = 0f;
            m_Color = color;
            Rebuild();
            m_Flash = 1f;
        }

        /// <summary>
        /// La plus petite distance (m) entre <paramref name="worldPoint"/> et les fissures du signe (en vol ou non).
        /// </summary>
        public float DistanceTo(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            var best = float.MaxValue;
            foreach (var stroke in m_Strokes)
            {
                var points = stroke.Points;
                for (var i = 1; i < points.Count; i++)
                    best = Mathf.Min(best, DistanceToSegment(local, points[i - 1], points[i]));
            }

            return best * transform.lossyScale.x;
        }

        /// <summary>
        /// Le signe part dans <paramref name="direction"/> et suit <paramref name="target"/> (s'il y en a une) ; il fait
        /// <paramref name="damage"/> à chaque ennemi qu'il traverse.
        /// </summary>
        public void Launch(Vector3 direction, float speed, Enemy target, float damage, ShotGrade grade, Object source, AudioClip hitClip)
        {
            if (m_State != State.Ready)
                return;

            // Jusqu'ici, il suivait le joueur ; lancé, il vole dans le monde.
            transform.SetParent(null, true);
            m_State = State.Flying;
            m_StateTime = 0f;
            m_Direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : transform.forward;
            m_Speed = speed;
            m_Target = target;
            m_Damage = damage;
            m_Grade = grade;
            m_Source = source;
            m_HitClip = hitClip;
            m_Start = transform.position;
            m_PreviousPose = new Pose(transform.position, transform.rotation);
            m_Flash = 1f;
            BuildHitPoints();
        }

        /// <summary>Le signe se brise et disparaît (fin de course, ou abandonné).</summary>
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

            switch (m_State)
            {
                case State.Ready:
                    // Prêt : il palpite doucement, en attendant le gantelet.
                    m_Flash = Mathf.Max(m_Flash, 0.25f + 0.2f * Mathf.Sin(m_StateTime * 6f));
                    break;
                case State.Breaking:
                    m_Fade = Mathf.Clamp01(1f - m_StateTime / k_BreakTime);
                    m_Flash = Mathf.Max(m_Flash, m_Fade);
                    transform.localScale = Vector3.one * (m_BreakScale * (1f + 0.25f * (1f - m_Fade)));
                    if (m_Fade <= 0f)
                        Destroy(gameObject);
                    break;
            }

            ApplyBlock();
        }

        void FixedUpdate()
        {
            if (m_State != State.Flying)
                return;

            var deltaTime = Time.fixedDeltaTime;
            if (m_Target != null && !m_Target.IsAlive)
                m_Target = null;

            // Il suit sa cible, et son plan reste face à sa course : tout le signe balaie l'air devant lui.
            if (m_Target != null && m_EndTime < 0f)
            {
                var toTarget = m_Target.Center - transform.position;
                if (toTarget.sqrMagnitude > 1e-4f)
                    m_Direction = Vector3.RotateTowards(m_Direction, toTarget.normalized, k_TurnRate * Mathf.Deg2Rad * deltaTime, 0f);
                if (toTarget.magnitude < 0.7f)
                    m_EndTime = Time.time + k_AfterTarget;
            }

            m_PreviousPose = new Pose(transform.position, transform.rotation);

            // Réduit pour le gantelet, il reprend sa taille en partant : tout le signe touche.
            transform.localScale = Vector3.one * Mathf.MoveTowards(transform.localScale.x, 1f, deltaTime * 4f);
            var rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(m_Direction, Vector3.up), 1f - Mathf.Exp(-12f * deltaTime));
            transform.SetPositionAndRotation(transform.position + m_Direction * (m_Speed * deltaTime), rotation);
            Sweep();

            if (m_StateTime > k_FlightLife || (m_EndTime >= 0f && Time.time >= m_EndTime))
                Break();
        }

        // Tout le signe touche : pour chaque bout de fissure, l'espace balayé depuis le pas physique d'avant (et là où il est).
        void Sweep()
        {
            m_StepHits.Clear();
            var previous = Matrix4x4.TRS(m_PreviousPose.position, m_PreviousPose.rotation, transform.lossyScale);
            var current = transform.localToWorldMatrix;
            var travel = transform.position - m_PreviousPose.position;
            var distance = travel.magnitude;
            var direction = distance > 1e-5f ? travel / distance : m_Direction;

            for (var s = 0; s < m_HitStarts.Count; s++)
            {
                var start = m_HitStarts[s];
                var end = s + 1 < m_HitStarts.Count ? m_HitStarts[s + 1] : m_HitPoints.Count;
                for (var i = start + 1; i < end; i++)
                {
                    var a = m_HitPoints[i - 1];
                    var b = m_HitPoints[i];
                    if (distance > 1e-5f)
                    {
                        var count = Physics.CapsuleCastNonAlloc(previous.MultiplyPoint3x4(a), previous.MultiplyPoint3x4(b), k_HitRadius, direction,
                                                                s_Casts, distance, ~0, QueryTriggerInteraction.Collide);
                        for (var k = 0; k < count; k++)
                            Consider(s_Casts[k].collider, s_Casts[k].point.sqrMagnitude > 0f ? s_Casts[k].point : current.MultiplyPoint3x4(a));
                    }

                    var overlaps = Physics.OverlapCapsuleNonAlloc(current.MultiplyPoint3x4(a), current.MultiplyPoint3x4(b), k_HitRadius, s_Overlaps,
                                                                  ~0, QueryTriggerInteraction.Collide);
                    for (var k = 0; k < overlaps; k++)
                        Consider(s_Overlaps[k], s_Overlaps[k].ClosestPoint(current.MultiplyPoint3x4((a + b) * 0.5f)));
                }
            }

            foreach (var pair in m_StepHits)
                Damage(pair.Key, pair.Value.Hitbox, pair.Value.Point);
        }

        // La meilleure zone de touche de chaque ennemi touché sur ce pas (tête avant corps).
        void Consider(Collider collider, Vector3 point)
        {
            if (collider == null || !collider.TryGetComponent(out Hitbox hitbox))
                return;
            var owner = hitbox.Owner;
            if (owner == null || !owner.IsAlive || m_Damaged.Contains(owner))
                return;
            if (owner.GetComponentInParent<Enemy>() == null && owner.GetComponentInParent<Boss>() == null)
                return;

            if (m_StepHits.TryGetValue(owner, out var best) && Multiplier(best.Hitbox) >= Multiplier(hitbox))
                return;
            m_StepHits[owner] = (hitbox, point);
        }

        static float Multiplier(Hitbox hitbox) =>
            hitbox.DamageMultiplier * (hitbox.Zone == HitZone.Head ? Hitbox.HeadDamageMultiplier : 1f);

        void Damage(Health owner, Hitbox hitbox, Vector3 point)
        {
            m_Damaged.Add(owner);
            owner.TakeDamage(new DamageInfo
            {
                Amount = m_Damage * Multiplier(hitbox),
                Zone = hitbox.Zone,
                Grade = m_Grade,
                Distance = Vector3.Distance(m_Start, point),
                Point = point,
                Direction = m_Direction,
                Source = m_Source,
            });
            Sfx.Play(m_HitClip, point, 0.9f, Random.Range(0.9f, 1.1f));
            m_Flash = 1f;

            // Sa cible touchée, il la traverse encore un instant, puis se brise.
            if (m_Target != null && owner.GetComponentInParent<Enemy>() == m_Target && m_EndTime < 0f)
                m_EndTime = Time.time + k_AfterTarget;
        }

        // Les points qui servent de zone de touche : tous les 7 cm le long de chaque fissure.
        void BuildHitPoints()
        {
            m_HitPoints.Clear();
            m_HitStarts.Clear();
            foreach (var stroke in m_Strokes)
            {
                m_HitStarts.Add(m_HitPoints.Count);
                var points = stroke.Points;
                m_HitPoints.Add(points[0]);
                var carried = 0f;
                for (var i = 1; i < points.Count; i++)
                {
                    carried += Vector3.Distance(points[i - 1], points[i]);
                    if (carried >= k_HitSpacing || i == points.Count - 1)
                    {
                        m_HitPoints.Add(points[i]);
                        carried = 0f;
                    }
                }
            }
        }

        void Rebuild()
        {
            m_Builder.Clear();
            foreach (var stroke in m_Strokes)
                m_Builder.AddCrack(stroke.Points, k_HalfWidth, new System.Random(stroke.Seed), m_Color);
            m_Builder.Apply(m_Mesh);
        }

        void ApplyBlock()
        {
            if (m_Renderer == null || m_Block == null)
                return;
            m_Block.SetFloat(k_FadeId, m_Fade);
            m_Block.SetFloat(k_FlashId, m_Flash);
            m_Renderer.SetPropertyBlock(m_Block);
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
