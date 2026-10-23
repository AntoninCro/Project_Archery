using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Combat
{
    /// <summary>
    /// Une faille dans la matrice, ouverte par un coup de katana (refaite le 23 octobre : une faille par coup, vivante ;
    /// avant, un signe de plusieurs traits). Elle est à plat dans son plan (X à droite, Y en haut, Z vers l'avant, face au
    /// joueur) : d'abord une fine fissure, puis elle s'ouvre d'un coup comme un œil, plus large au milieu ; ses bords
    /// ondulent, son tracé frémit et ses fissures rampent (shader <c>Archery/MatrixTear</c>). Ouverte, elle attend le
    /// gantelet ; frappée, elle file vers sa cible comme une flèche et la
    /// suit, et toute la faille touche : chaque ennemi qu'elle traverse prend ses dégâts (une fois), par sa zone de touche
    /// (tête ×2, gemme ×3). Elle se brise sur sa cible, ou au bout de sa course ; pas frappée à temps, elle se referme.
    /// </summary>
    /// <remarks>Créée et lancée par <see cref="KatanaGauntlet"/>.</remarks>
    [DisallowMultipleComponent]
    public class MatrixRift : MonoBehaviour
    {
        static readonly int k_OpenId = Shader.PropertyToID("_Open");
        static readonly int k_FadeId = Shader.PropertyToID("_Fade");
        static readonly int k_FlashId = Shader.PropertyToID("_Flash");
        static readonly int k_TintId = Shader.PropertyToID("_Tint");
        static readonly int k_SeedId = Shader.PropertyToID("_Seed");

        // Demi-largeur (m) du milieu de la plaie grande ouverte, selon la longueur du coup : 2 cm au moins, 6 cm au plus.
        const float k_WidthPerMeter = 0.055f;
        const float k_MinHalfWidth = 0.02f;
        const float k_MaxHalfWidth = 0.06f;

        // Elle s'ouvre comme un œil (demandé le 23 octobre) : d'abord une fine fissure qui brille (0,14 s), puis elle s'ouvre
        // d'un coup en 0,1 s, avec un léger dépassement. Elle se referme en 0,35 s.
        const float k_CrackTime = 0.14f;
        const float k_CrackOpen = 0.08f;
        const float k_SnapTime = 0.1f;
        const float k_Overshoot = 1.6f;
        const float k_CloseTime = 0.35f;

        // Zones de touche : des points tous les 7 cm le long du tracé, chacun d'un rayon de 6 cm.
        const float k_HitSpacing = 0.07f;
        const float k_HitRadius = 0.06f;

        // En vol : virage maximal (°/s) vers la cible, durée de vie, temps qu'elle continue après avoir touché sa cible.
        const float k_TurnRate = 540f;
        const float k_FlightLife = 3f;
        const float k_AfterTarget = 0.12f;
        const float k_BreakTime = 0.3f;

        static readonly Collider[] s_Overlaps = new Collider[32];
        static readonly RaycastHit[] s_Casts = new RaycastHit[32];

        enum State
        {
            Open,
            Closing,
            Flying,
            Breaking,
        }

        readonly List<Vector3> m_Spine = new List<Vector3>();
        readonly List<Vector3> m_HitPoints = new List<Vector3>();
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
        float m_Open;
        float m_Flash = 1f;
        float m_Fade = 1f;
        float m_StateTime;
        float m_BornAt;
        bool m_Snapped;
        float m_BreakScale = 1f;
        float m_Seed;

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

        /// <summary>Ouverte, en attente du gantelet.</summary>
        public bool IsOpen => m_State == State.Open;

        public bool IsFlying => m_State == State.Flying;

        /// <summary>
        /// Temps (s) depuis qu'elle s'est ouverte d'un coup (0 tant qu'elle n'est qu'une fissure) : sa fraîcheur, qui donne
        /// la qualité du tir.
        /// </summary>
        public float Age => Mathf.Max(0f, Time.time - m_BornAt - k_CrackTime);

        /// <summary>La fissure s'ouvre d'un coup (pour le son et la vibration).</summary>
        public event System.Action<MatrixRift> Snapped;

        /// <summary>
        /// Une faille le long de <paramref name="worldPoints"/> (le chemin de la lame), à plat dans un plan tourné vers
        /// <paramref name="viewer"/> (les yeux du joueur).
        /// </summary>
        public static MatrixRift Create(IReadOnlyList<Vector3> worldPoints, Vector3 viewer, Material material, int seed)
        {
            // Le plan : par le milieu du chemin, face au joueur, et contenant la direction générale du coup.
            var center = Vector3.zero;
            foreach (var point in worldPoints)
                center += point;
            center /= Mathf.Max(1, worldPoints.Count);
            var across = worldPoints[worldPoints.Count - 1] - worldPoints[0];
            var toViewer = viewer - center;
            var facing = across.sqrMagnitude > 1e-6f ? toViewer - Vector3.Project(toViewer, across) : toViewer;
            if (facing.sqrMagnitude < 1e-6f)
                facing = Vector3.back;
            var forward = -facing.normalized;
            var up = Vector3.ProjectOnPlane(Vector3.up, forward);
            if (up.sqrMagnitude < 1e-4f)
                up = Vector3.ProjectOnPlane(Vector3.forward, forward);

            var go = new GameObject("Faille du katana");
            go.transform.SetPositionAndRotation(center, Quaternion.LookRotation(forward, up.normalized));
            var rift = go.AddComponent<MatrixRift>();
            rift.Build(worldPoints, material, seed);
            return rift;
        }

        void Build(IReadOnlyList<Vector3> worldPoints, Material material, int seed)
        {
            var local = new List<Vector3>(worldPoints.Count);
            foreach (var point in worldPoints)
            {
                var p = transform.InverseTransformPoint(point);
                p.z = 0f;
                local.Add(p);
            }

            var length = 0f;
            for (var i = 1; i < local.Count; i++)
                length += Vector3.Distance(local[i - 1], local[i]);
            var halfWidth = Mathf.Clamp(length * k_WidthPerMeter, k_MinHalfWidth, k_MaxHalfWidth);

            var random = new System.Random(seed);
            m_Seed = (float)random.NextDouble();
            m_Mesh = MatrixRiftMesh.Build(local, halfWidth, random, m_Spine);
            m_Mesh.MarkDynamic();
            gameObject.AddComponent<MeshFilter>().sharedMesh = m_Mesh;
            m_Renderer = gameObject.AddComponent<MeshRenderer>();
            m_Renderer.sharedMaterial = material;
            m_Renderer.shadowCastingMode = ShadowCastingMode.Off;
            m_Renderer.receiveShadows = false;
            m_Block = new MaterialPropertyBlock();
            m_BornAt = Time.time;
            m_Flash = 0.6f;
            BuildHitPoints();
            ApplyBlock();
        }

        void OnDestroy()
        {
            if (m_Mesh != null)
                Destroy(m_Mesh);
        }

        /// <summary>La couleur de ses bords : la qualité qu'aurait le tir maintenant (puis celle du tir lancé).</summary>
        public void SetTint(Color color) => m_Tint = color;

        /// <summary>La plus petite distance (m) entre <paramref name="worldPoint"/> et le tracé de la faille.</summary>
        public float DistanceTo(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint);
            var best = float.MaxValue;
            for (var i = 1; i < m_Spine.Count; i++)
                best = Mathf.Min(best, DistanceToSegment(local, m_Spine[i - 1], m_Spine[i]));
            return best * transform.lossyScale.x;
        }

        /// <summary>Ouverte, elle glisse vers <paramref name="worldTarget"/> (à portée du gantelet), sans brusquerie.</summary>
        public void DriftTowards(Vector3 worldTarget, float rate, float deltaTime)
        {
            if (m_State != State.Open || deltaTime <= 0f)
                return;
            transform.position = Vector3.Lerp(transform.position, worldTarget, 1f - Mathf.Exp(-rate * deltaTime));
        }

        /// <summary>
        /// Elle part dans <paramref name="direction"/> et suit <paramref name="target"/> (s'il y en a une) ; elle fait
        /// <paramref name="damage"/> à chaque ennemi qu'elle traverse.
        /// </summary>
        public void Launch(Vector3 direction, float speed, Enemy target, float damage, ShotGrade grade, Color color, Object source, AudioClip hitClip)
        {
            if (m_State != State.Open)
                return;

            // Jusqu'ici, elle suivait le joueur ; lancée, elle vole dans le monde.
            transform.SetParent(null, true);
            m_State = State.Flying;
            m_StateTime = 0f;
            m_Direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : transform.forward;
            m_Speed = speed;
            m_Target = target;
            m_Damage = damage;
            m_Grade = grade;
            m_Tint = color;
            m_Source = source;
            m_HitClip = hitClip;
            m_Start = transform.position;
            m_PreviousPose = new Pose(transform.position, transform.rotation);
            m_Flash = 1f;
            m_Open = Mathf.Max(m_Open, 1.15f);
        }

        /// <summary>Pas frappée à temps (ou l'arme rangée) : la plaie se referme et disparaît.</summary>
        public void Close()
        {
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

            switch (m_State)
            {
                case State.Open:
                    UpdateOpening();
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

            ApplyBlock();
        }

        // D'abord une fine fissure qui apparaît et grésille, puis elle s'ouvre d'un coup, comme un œil.
        void UpdateOpening()
        {
            var time = Time.time - m_BornAt;
            if (time < k_CrackTime)
            {
                var flicker = 0.8f + 0.2f * Mathf.Sin(time * 90f);
                m_Open = k_CrackOpen * Mathf.SmoothStep(0f, 1f, time / 0.04f) * flicker;
                m_Flash = Mathf.Max(m_Flash, 0.5f);
                return;
            }

            if (!m_Snapped)
            {
                m_Snapped = true;
                m_Flash = 1f;
                Snapped?.Invoke(this);
            }

            m_Open = Mathf.LerpUnclamped(k_CrackOpen, 1f, OpeningCurve((time - k_CrackTime) / k_SnapTime));
        }

        // L'ouverture : de 0 à 1 avec un léger dépassement (une courbe « qui rebondit »).
        static float OpeningCurve(float x)
        {
            if (x >= 1f)
                return 1f;
            var u = Mathf.Clamp01(x) - 1f;
            return 1f + (k_Overshoot + 1f) * u * u * u + k_Overshoot * u * u;
        }

        void FixedUpdate()
        {
            if (m_State != State.Flying)
                return;

            var deltaTime = Time.fixedDeltaTime;
            if (m_Target != null && !m_Target.IsAlive)
                m_Target = null;

            // Elle suit sa cible, et son plan reste face à sa course : toute la faille balaie l'air devant elle.
            if (m_Target != null && m_EndTime < 0f)
            {
                var toTarget = m_Target.Center - transform.position;
                if (toTarget.sqrMagnitude > 1e-4f)
                    m_Direction = Vector3.RotateTowards(m_Direction, toTarget.normalized, k_TurnRate * Mathf.Deg2Rad * deltaTime, 0f);
                if (toTarget.magnitude < 0.7f)
                    m_EndTime = Time.time + k_AfterTarget;
            }

            m_PreviousPose = new Pose(transform.position, transform.rotation);
            var rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(m_Direction, Vector3.up), 1f - Mathf.Exp(-12f * deltaTime));
            transform.SetPositionAndRotation(transform.position + m_Direction * (m_Speed * deltaTime), rotation);
            Sweep();

            if (m_StateTime > k_FlightLife || (m_EndTime >= 0f && Time.time >= m_EndTime))
                Break();
        }

        // Toute la faille touche : pour chaque bout du tracé, l'espace balayé depuis le pas physique d'avant (et là où il est).
        void Sweep()
        {
            m_StepHits.Clear();
            m_StepOthers.Clear();
            var previous = Matrix4x4.TRS(m_PreviousPose.position, m_PreviousPose.rotation, transform.lossyScale);
            var current = transform.localToWorldMatrix;
            var travel = transform.position - m_PreviousPose.position;
            var distance = travel.magnitude;
            var direction = distance > 1e-5f ? travel / distance : m_Direction;

            for (var i = 1; i < m_HitPoints.Count; i++)
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

            foreach (var pair in m_StepHits)
                Damage(pair.Key, pair.Value.Hitbox, pair.Value.Point);

            foreach (var pair in m_StepOthers)
            {
                if (pair.Key == null)
                    continue;
                m_Triggered.Add(pair.Key);
                var hit = new ArrowHit
                {
                    Arrow = null,
                    Collider = pair.Value.Collider,
                    Point = pair.Value.Point,
                    Normal = -m_Direction,
                    Direction = m_Direction,
                    Speed = m_Speed,
                    Damage = m_Damage,
                    Grade = m_Grade,
                    TravelDistance = Vector3.Distance(m_Start, pair.Value.Point),
                    IsShot = true,
                };
                pair.Value.Handler.OnArrowHit(hit);
            }
        }

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

            // Sa cible touchée, elle la traverse encore un instant, puis se brise.
            if (m_Target != null && owner.GetComponentInParent<Enemy>() == m_Target && m_EndTime < 0f)
                m_EndTime = Time.time + k_AfterTarget;
        }

        // Les points qui servent de zone de touche : tous les 7 cm le long du tracé.
        void BuildHitPoints()
        {
            m_HitPoints.Clear();
            if (m_Spine.Count == 0)
                return;
            m_HitPoints.Add(m_Spine[0]);
            var carried = 0f;
            for (var i = 1; i < m_Spine.Count; i++)
            {
                carried += Vector3.Distance(m_Spine[i - 1], m_Spine[i]);
                if (carried >= k_HitSpacing || i == m_Spine.Count - 1)
                {
                    m_HitPoints.Add(m_Spine[i]);
                    carried = 0f;
                }
            }
        }

        void ApplyBlock()
        {
            if (m_Renderer == null || m_Block == null)
                return;
            m_Block.SetFloat(k_OpenId, m_Open);
            m_Block.SetFloat(k_FadeId, m_Fade);
            m_Block.SetFloat(k_FlashId, m_Flash);
            m_Block.SetColor(k_TintId, m_Tint);
            m_Block.SetFloat(k_SeedId, m_Seed);
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
