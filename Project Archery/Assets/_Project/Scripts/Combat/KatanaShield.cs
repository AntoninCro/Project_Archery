using System.Collections.Generic;
using Archery.BowClasses;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Combat
{
    /// <summary>
    /// Le katana et le bouclier, une arme du joueur (à la place de l'arc ; carte du menu des armes, débloquée par « Rōnin »).
    /// Les deux flottent à côté du joueur et se prennent avec la poignée de la manette. Le katana fait de lourds dégâts au
    /// corps à corps quand on le balance vite, et déchire l'air : la déchirure (une fente dans la matrice du jeu) reste un
    /// instant sur place. Frappée avec le bouclier, elle fonce vers l'avant et agit comme une flèche : une flèche part de son
    /// milieu (qualité selon sa fraîcheur : vert si on la frappe tout de suite, orange un peu après, rouge à la fin), la
    /// déchirure la suit. Le bouclier tenu arrête les coups et les projectiles qu'il couvre ; le katana coupe aussi les
    /// projectiles.
    /// </summary>
    /// <remarks>
    /// Créés par l'<see cref="Bow"/> quand sa définition est <see cref="BowDefinition.katana"/> : l'arc est caché, et ses
    /// multiplicateurs (améliorations, coffres) servent ici (dégâts du katana et de la déchirure, vitesse, perçage, largeur
    /// des fenêtres de fraîcheur) ; la déchirure lancée compte comme un tir de l'arc (améliorations de flèches, score).
    /// Rangés avec lui (construction, dimensions). Sans casque : clic gauche, un coup de katana devant soi ; clic droit
    /// maintenu, le bouclier levé ; un clic droit lance la dernière déchirure vers le viseur. Raccourci de test :
    /// <b>O</b> (éditeur et builds Development) équipe le katana et le bouclier, même verrouillés, et les rend.
    /// </remarks>
    public class KatanaShield : MonoBehaviour, IMeleeSource
    {
        // Le katana, dans son repère (le milieu de la poignée à l'origine, la lame vers +Y) : le début et la pointe de la lame.
        static readonly Vector3 k_BladeStart = new Vector3(0f, 0.135f, 0f);
        static readonly Vector3 k_BladeTip = new Vector3(0f, 0.87f, 0f);

        // En main : la poignée dans la paume, la lame le long du poing (la poignée des manettes Quest est inclinée de 45°).
        static readonly Quaternion k_KatanaAttach = Quaternion.Euler(-45f, 0f, 0f);
        static readonly Vector3 k_KatanaBoxCenter = new Vector3(0f, 0.37f, 0f);
        static readonly Vector3 k_KatanaBoxSize = new Vector3(0.07f, 1f, 0.07f);

        // Le bouclier : rayon (m) ; la poignée au dos, la face vers l'avant de la manette.
        const float k_ShieldRadius = 0.2f;
        static readonly Vector3 k_ShieldBoxCenter = new Vector3(0f, 0f, 0.045f);
        static readonly Vector3 k_ShieldBoxSize = new Vector3(0.42f, 0.42f, 0.09f);

        // Rangés : de chaque côté du joueur, à hauteur de hanche. Le katana poignée en haut, lame vers le bas et l'arrière ;
        // le bouclier de chant, face vers l'extérieur.
        static readonly Vector3 k_KatanaHolster = new Vector3(0.32f, -0.42f, 0.2f);
        static readonly Vector3 k_ShieldHolster = new Vector3(0.34f, -0.45f, 0.18f);

        // Le coup : dès que la pointe va à cette vitesse (m/s) ; dégâts ×1 à ×1,6 jusqu'à 7 m/s ; un ennemi n'est touché
        // qu'une fois par délai (s).
        const float k_HitSpeed = 2.5f;
        const float k_HitFastSpeed = 7f;
        const float k_HitCooldown = 0.4f;
        const float k_BladeRadius = 0.035f;

        // La déchirure : dès que la pointe va à cette vitesse, de 35 % de la lame à la pointe ; chaque bout reste ce temps (s).
        const float k_TrailSpeed = 3.5f;
        const float k_TrailFrom = 0.35f;
        const float k_TrailLife = 1.2f;
        const float k_TrailSpacing = 0.015f;
        const int k_TrailMaxSamples = 64;

        // Le bouclier lance une déchirure qu'il touche en allant au moins à cette vitesse (m/s). Sa fraîcheur donne la
        // qualité du tir : parfaite jusqu'à 0,3 s, bonne jusqu'à 0,8 s, ratée ensuite (les améliorations d'anneau les élargissent).
        const float k_ShieldHitSpeed = 1.2f;
        const float k_ShieldReach = 0.08f;
        const float k_PerfectAge = 0.3f;
        const float k_GoodAge = 0.8f;
        const float k_LaunchFade = 0.6f;

        // Le bouclier arrête un coup qui passe à moins de ce rayon de son centre, s'il lui fait face ; un projectile qui
        // le traverse.
        const float k_BlockRadius = 0.26f;
        const float k_ProjectileReach = 0.12f;

        // Sans casque : le katana et le bouclier devant la caméra ; un coup dure ce temps (s), un coup de bouclier aussi.
        static readonly Vector3 k_DesktopKatana = new Vector3(0.14f, -0.24f, 0.28f);
        static readonly Vector3 k_DesktopShield = new Vector3(-0.24f, -0.2f, 0.42f);
        const float k_DesktopSwing = 0.22f;
        const float k_DesktopBash = 0.3f;

        static readonly Color k_TrailColor = new Color(1f, 1f, 1f, 1f);
        static readonly Collider[] s_Overlaps = new Collider[32];
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        sealed class Sample
        {
            public Vector3 Base;
            public Vector3 Tip;
            public float Time;
        }

        sealed class Trail
        {
            public readonly List<Sample> Samples = new List<Sample>();
            public GameObject Object;
            public Mesh Mesh;
            public bool Launched;
            public Arrow Arrow;
            public Vector3 ArrowStart;
            public Vector3 Offset;
            public Vector3 Velocity;
            public float LaunchTime;
            public Color Color = k_TrailColor;
        }

        readonly List<Trail> m_Trails = new List<Trail>();
        readonly Dictionary<Health, float> m_LastHit = new Dictionary<Health, float>();
        readonly List<EnemyProjectile> m_Projectiles = new List<EnemyProjectile>();
        readonly List<Vector3> m_Vertices = new List<Vector3>();
        readonly List<Color> m_Colors = new List<Color>();
        readonly List<Vector2> m_Uvs = new List<Vector2>();
        readonly List<int> m_Triangles = new List<int>();

        Bow m_Bow;
        BowDefinition m_Definition;
        FloatingGrab m_Katana;
        FloatingGrab m_Shield;
        float m_KatanaSide = 1f;
        Health m_PlayerHealth;
        Vector3 m_PreviousTip;
        Vector3 m_PreviousShield;
        bool m_HasPrevious;
        Trail m_Current;
        Material m_FallbackTrailMaterial;
        bool m_Desktop;
        bool m_Hidden;
        float m_SwingTime = -1f;
        float m_SwingSide = 1f;
        float m_BashTime = -1f;
        float m_LastBlockTime = -1f;

        /// <summary>Le katana et le bouclier de l'arc (sa définition, ses multiplicateurs, ses qualités de tir).</summary>
        public static KatanaShield Create(Bow bow, BowDefinition definition)
        {
            var weapon = new GameObject("Katana et bouclier").AddComponent<KatanaShield>();
            weapon.m_Bow = bow;
            weapon.m_Definition = definition;
            return weapon;
        }

        void OnEnable() => EnemyProjectile.Launched += OnProjectileLaunched;

        void OnDisable() => EnemyProjectile.Launched -= OnProjectileLaunched;

        void OnDestroy()
        {
            if (m_PlayerHealth != null && m_PlayerHealth.DamageFilter == (System.Func<DamageInfo, float>)Filter)
                m_PlayerHealth.DamageFilter = null;
            if (m_Katana != null)
                Destroy(m_Katana.gameObject);
            if (m_Shield != null)
                Destroy(m_Shield.gameObject);
            foreach (var trail in m_Trails)
            {
                if (trail.Object != null)
                    Destroy(trail.Object);
                if (trail.Mesh != null)
                    Destroy(trail.Mesh);
            }

            if (m_FallbackTrailMaterial != null)
                Destroy(m_FallbackTrailMaterial);
        }

        void OnProjectileLaunched(EnemyProjectile projectile)
        {
            if (projectile != null)
                m_Projectiles.Add(projectile);
        }

        // ---------------------------------------------------------------- construction

        bool TryBuild()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null || m_Definition == null)
                return false;

            // Le bouclier du côté de la main de l'arc, le katana de l'autre.
            var shieldSide = rig.BowHand == InteractorHandedness.Right ? 1f : -1f;
            m_KatanaSide = -shieldSide;

            var katana = new GameObject("Katana").transform;
            katana.SetParent(transform, false);
            AddModel(katana, m_Definition.katanaModel, true);
            m_Katana = FloatingGrab.Create(katana.gameObject, Vector3.zero, k_KatanaAttach, k_KatanaBoxCenter, k_KatanaBoxSize,
                                           Vector3.Scale(k_KatanaHolster, new Vector3(m_KatanaSide, 1f, 1f)),
                                           Quaternion.Euler(-140f, 0f, -12f * m_KatanaSide));
            m_Katana.Grabbed += interactor => m_HasPrevious = false;

            var shield = new GameObject("Bouclier").transform;
            shield.SetParent(transform, false);
            AddModel(shield, m_Definition.shieldModel, false);
            m_Shield = FloatingGrab.Create(shield.gameObject, Vector3.zero, Quaternion.identity, k_ShieldBoxCenter, k_ShieldBoxSize,
                                           Vector3.Scale(k_ShieldHolster, new Vector3(shieldSide, 1f, 1f)),
                                           Quaternion.Euler(0f, 70f * shieldSide, 0f));

            m_Katana.SnapToHolster();
            m_Shield.SnapToHolster();

            var player = PlayerHealth.Instance;
            m_PlayerHealth = player != null ? player.Health : null;
            if (m_PlayerHealth != null)
                m_PlayerHealth.DamageFilter = Filter;
            return true;
        }

        // Le modèle de la définition (sans ses colliders), ou des formes simples s'il manque.
        static void AddModel(Transform root, GameObject model, bool katana)
        {
            if (model != null)
            {
                var instance = Instantiate(model, root, false);
                instance.name = model.name;
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    DestroyImmediate(collider);
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                return;
            }

            if (katana)
            {
                Box(root, new Vector3(0f, 0f, 0f), new Vector3(0.032f, 0.24f, 0.032f), new Color(0.08f, 0.06f, 0.06f));
                Box(root, new Vector3(0f, 0.125f, 0f), new Vector3(0.09f, 0.012f, 0.07f), new Color(0.75f, 0.6f, 0.2f));
                Box(root, new Vector3(0f, 0.5f, 0f), new Vector3(0.006f, 0.74f, 0.03f), new Color(0.8f, 0.84f, 0.9f));
            }
            else
            {
                Box(root, new Vector3(0f, 0f, 0.05f), new Vector3(k_ShieldRadius * 2f, k_ShieldRadius * 2f, 0.02f), new Color(0.35f, 0.22f, 0.12f));
            }
        }

        static void Box(Transform parent, Vector3 position, Vector3 size, Color color)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            var renderer = box.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            renderer.sharedMaterial = material;
        }

        // ---------------------------------------------------------------- chaque image

        void Update()
        {
            if (m_Katana == null && !TryBuild())
                return;

            var desktop = DesktopControls.Instance != null && DesktopControls.Instance.IsActive;
            if (desktop != m_Desktop)
                SetDesktop(desktop);

            var hidden = m_Bow != null && m_Bow.IsStowed;
            if (hidden != m_Hidden)
                SetHidden(hidden);

            var deltaTime = Time.deltaTime;
            if (!m_Hidden)
            {
                if (m_Desktop)
                    UpdateDesktop(deltaTime);
                UpdateKatana(deltaTime);
                UpdateShield(deltaTime);
            }

            UpdateTrails();
        }

        void SetHidden(bool hidden)
        {
            m_Hidden = hidden;
            foreach (var weapon in new[] { m_Katana, m_Shield })
            {
                weapon.Release();
                weapon.gameObject.SetActive(!hidden);
                if (!hidden && !m_Desktop)
                    weapon.SnapToHolster();
            }

            m_HasPrevious = false;
            m_Current = null;
        }

        bool KatanaActive => m_Desktop || m_Katana.Held;
        bool ShieldRaised => m_Desktop ? Mouse.current != null && Mouse.current.rightButton.isPressed : m_Shield.Held;

        IXRInteractor KatanaHand => m_Desktop ? null : m_Katana.Holder;
        IXRInteractor ShieldHand => m_Desktop ? null : m_Shield.Holder;

        float DamageMultiplier => m_Bow != null ? m_Bow.DamageMultiplier : 1f;
        ShotTuning Tuning => m_Bow != null ? m_Bow.ShotTuning : ShotTuning.Fallback;

        // ---------------------------------------------------------------- le katana

        void UpdateKatana(float deltaTime)
        {
            var root = m_Katana.transform;
            var tip = root.TransformPoint(k_BladeTip);
            var bladeBase = root.TransformPoint(k_BladeStart);
            if (!KatanaActive || !m_HasPrevious || deltaTime <= 0f)
            {
                m_PreviousTip = tip;
                m_PreviousShield = m_Shield.transform.position;
                m_HasPrevious = KatanaActive;
                m_Current = null;
                return;
            }

            var tipVelocity = (tip - m_PreviousTip) / deltaTime;
            var speed = tipVelocity.magnitude;
            if (speed >= k_HitSpeed)
            {
                Strike(bladeBase, tip, m_PreviousTip, tipVelocity, speed);
                CutProjectiles(bladeBase, tip);
            }

            // La déchirure : tant que la lame va vite, un bout de plus à chaque centimètre et demi de la pointe.
            if (speed >= k_TrailSpeed)
            {
                if (m_Current == null || m_Current.Launched || m_Current.Samples.Count >= k_TrailMaxSamples)
                {
                    m_Current = NewTrail();
                    m_Trails.Add(m_Current);
                }

                var samples = m_Current.Samples;
                if (samples.Count == 0 || (samples[samples.Count - 1].Tip - tip).sqrMagnitude > k_TrailSpacing * k_TrailSpacing)
                    samples.Add(new Sample { Base = Vector3.Lerp(bladeBase, tip, k_TrailFrom), Tip = tip, Time = Time.time });
            }
            else
            {
                m_Current = null;
            }

            m_PreviousTip = tip;
        }

        // Le coup : la lame (une capsule) et le chemin de la pointe depuis l'image d'avant ; chaque ennemi touché une fois
        // par délai, par sa zone de touche (la tête compte double, comme pour une flèche).
        void Strike(Vector3 bladeBase, Vector3 tip, Vector3 previousTip, Vector3 velocity, float speed)
        {
            var damage = m_Definition.katanaDamage * DamageMultiplier * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(k_HitSpeed, k_HitFastSpeed, speed));
            var direction = velocity / speed;
            var count = Physics.OverlapCapsuleNonAlloc(bladeBase, tip, k_BladeRadius, s_Overlaps, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
                TryHit(s_Overlaps[i], s_Overlaps[i].ClosestPoint(tip), direction, damage);

            var step = tip - previousTip;
            var distance = step.magnitude;
            if (distance < 1e-4f)
                return;
            var hits = Physics.RaycastNonAlloc(previousTip, step / distance, s_Hits, distance, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits; i++)
                TryHit(s_Hits[i].collider, s_Hits[i].point, direction, damage);
        }

        void TryHit(Collider collider, Vector3 point, Vector3 direction, float damage)
        {
            if (collider == null || !collider.TryGetComponent(out Hitbox hitbox))
                return;
            var owner = hitbox.Owner;
            if (owner == null || !owner.IsAlive || (owner.GetComponentInParent<Enemy>() == null && owner.GetComponentInParent<Boss>() == null))
                return;
            if (m_LastHit.TryGetValue(owner, out var last) && Time.time - last < k_HitCooldown)
                return;

            m_LastHit[owner] = Time.time;
            var zone = hitbox.Zone;
            owner.TakeDamage(new DamageInfo
            {
                Amount = damage * hitbox.DamageMultiplier * (zone == HitZone.Head ? Hitbox.HeadDamageMultiplier : 1f),
                Zone = zone,
                Grade = ShotGrade.Good,
                Distance = 0f,
                Point = point,
                Direction = direction,
                Source = this,
            });
            Sfx.Play(m_Definition.katanaHitClip, point, 0.9f, Random.Range(0.9f, 1.1f));
            Haptics.Pulse(KatanaHand, 0.8f, 0.08f);
        }

        // La lame lancée coupe les projectiles qu'elle croise.
        void CutProjectiles(Vector3 bladeBase, Vector3 tip)
        {
            var blade = tip - bladeBase;
            for (var i = m_Projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = m_Projectiles[i];
                if (projectile == null)
                {
                    m_Projectiles.RemoveAt(i);
                    continue;
                }

                var position = projectile.transform.position;
                var t = Mathf.Clamp01(Vector3.Dot(position - bladeBase, blade) / Mathf.Max(1e-4f, blade.sqrMagnitude));
                if ((position - (bladeBase + blade * t)).sqrMagnitude < k_ProjectileReach * k_ProjectileReach && Stop(projectile))
                    Haptics.Pulse(KatanaHand, 0.6f, 0.06f);
            }
        }

        // ---------------------------------------------------------------- le bouclier

        void UpdateShield(float deltaTime)
        {
            var center = m_Shield.transform.position;
            var velocity = deltaTime > 0f ? (center - m_PreviousShield) / deltaTime : Vector3.zero;
            m_PreviousShield = center;

            if (ShieldRaised)
                InterceptProjectiles(center, m_Shield.transform.forward);

            if (m_Desktop || (!m_Shield.Held) || velocity.magnitude < k_ShieldHitSpeed)
                return;

            var reach = k_ShieldRadius + k_ShieldReach;
            foreach (var trail in m_Trails)
            {
                if (trail.Launched || trail.Samples.Count < 2 || !Touches(trail, center, reach))
                    continue;
                Launch(trail, (m_Shield.transform.forward * 0.6f + velocity.normalized * 0.4f).normalized);
                return;
            }
        }

        static bool Touches(Trail trail, Vector3 center, float reach)
        {
            var sqrReach = reach * reach;
            foreach (var sample in trail.Samples)
            {
                for (var t = 0f; t <= 1.001f; t += 0.5f)
                {
                    if ((Vector3.Lerp(sample.Base, sample.Tip, t) - center).sqrMagnitude < sqrReach)
                        return true;
                }
            }

            return false;
        }

        // Un projectile qui traverse le bouclier levé s'y brise.
        void InterceptProjectiles(Vector3 center, Vector3 normal)
        {
            for (var i = m_Projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = m_Projectiles[i];
                if (projectile == null)
                {
                    m_Projectiles.RemoveAt(i);
                    continue;
                }

                var offset = projectile.transform.position - center;
                var depth = Vector3.Dot(offset, normal);
                if (Mathf.Abs(depth) > k_ProjectileReach || (offset - normal * depth).sqrMagnitude > k_ShieldRadius * k_ShieldRadius)
                    continue;
                if (Stop(projectile))
                    Blocked(center);
            }
        }

        bool Stop(EnemyProjectile projectile)
        {
            var position = projectile.transform.position;
            return projectile.OnArrowHit(new ArrowHit { Collider = null, Point = position, Direction = Vector3.zero, IsShot = true, Grade = ShotGrade.Good });
        }

        // Les dégâts qui restent d'une attaque sur le joueur : rien si elle passe par le bouclier levé, face à elle.
        float Filter(DamageInfo info)
        {
            if (m_Hidden || m_Shield == null || !ShieldRaised || info.Direction.sqrMagnitude < 1e-4f)
                return info.Amount;

            var direction = info.Direction.normalized;
            var center = m_Shield.transform.position;
            if (Vector3.Dot(m_Shield.transform.forward, -direction) < 0.2f)
                return info.Amount;

            // Le chemin de l'attaque, sur les 3 derniers mètres avant le joueur : passe-t-il par le bouclier ?
            var start = info.Point - direction * 3f;
            var t = Mathf.Clamp(Vector3.Dot(center - start, direction), 0f, 3f);
            var onPath = t < 2.98f && (center - (start + direction * t)).sqrMagnitude <= k_BlockRadius * k_BlockRadius;
            if (!onPath && !Covers(center, direction))
                return info.Amount;

            Blocked(center);
            return 0f;
        }

        // Un coup d'ennemi au sol vise parfois les pieds : le bouclier levé devant soi, face à l'attaque, couvre tout ce
        // côté (à 35° près, vu d'en haut).
        bool Covers(Vector3 center, Vector3 direction)
        {
            var rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null)
                return false;
            var toAttacker = Vector3.ProjectOnPlane(-direction, Vector3.up);
            var toShield = Vector3.ProjectOnPlane(center - rig.Head.position, Vector3.up);
            var facing = Vector3.ProjectOnPlane(m_Shield.transform.forward, Vector3.up);
            if (toAttacker.sqrMagnitude < 1e-4f || toShield.sqrMagnitude < 0.01f || facing.sqrMagnitude < 1e-4f)
                return false;
            return Vector3.Angle(toShield, toAttacker) < 35f && Vector3.Dot(facing.normalized, toAttacker.normalized) > 0.5f;
        }

        void Blocked(Vector3 center)
        {
            if (Time.time - m_LastBlockTime < 0.1f)
                return;
            m_LastBlockTime = Time.time;
            Sfx.Play(m_Definition.shieldBlockClip, center, 0.9f, Random.Range(0.9f, 1.1f));
            Haptics.Pulse(ShieldHand, 0.9f, 0.1f);
            FloatingText.Spawn(center + Vector3.up * 0.25f, "Bloqué !", new Color(0.7f, 0.85f, 1f), 0.8f, 0.8f);
        }

        // La déchirure fonce dans cette direction : une flèche part de son milieu, comme un tir (qualité selon sa fraîcheur),
        // et la déchirure la suit en s'effaçant.
        void Launch(Trail trail, Vector3 direction)
        {
            // En pause (menu ouvert en cours de partie), la déchirure ne part pas.
            if (SlowMotion.IsPaused)
                return;

            var middle = Vector3.zero;
            var newest = 0f;
            foreach (var sample in trail.Samples)
            {
                middle += (sample.Base + sample.Tip) * 0.5f;
                newest = Mathf.Max(newest, sample.Time);
            }

            middle /= trail.Samples.Count;
            var grade = Freshness(Time.time - newest);
            var modifiers = Tuning.Get(grade);
            var speed = m_Definition.arrowSpeed * modifiers.speed * (m_Bow != null ? m_Bow.SpeedMultiplier : 1f);
            var damage = m_Definition.damage * modifiers.damage * DamageMultiplier;

            trail.Launched = true;
            trail.LaunchTime = Time.time;
            trail.Velocity = direction * speed;
            trail.Color = modifiers.color;
            if (m_Current == trail)
                m_Current = null;

            var pool = ArrowPool.Instance;
            if (pool != null)
            {
                var arrow = pool.Get();
                arrow.transform.SetPositionAndRotation(middle, Quaternion.LookRotation(direction));
                arrow.Launch(new ArrowLaunch
                {
                    Velocity = direction * speed,
                    Damage = damage,
                    Grade = grade,
                    Pierce = m_Definition.pierceCount + (m_Bow != null ? m_Bow.BonusPierce : 0),
                    TrailColor = modifiers.color,
                    IsShot = true,
                    GravityScale = m_Definition.arrowGravity,
                });
                trail.Arrow = arrow;
                trail.ArrowStart = arrow.transform.position;

                if (m_Bow != null)
                {
                    m_Bow.ReportShot(new ShotInfo
                    {
                        Arrow = arrow,
                        Grade = grade,
                        DrawRatio = 1f,
                        Speed = speed,
                        Damage = damage,
                        Origin = middle,
                        Direction = direction,
                    });
                }
            }

            if (!string.IsNullOrEmpty(modifiers.label))
                FloatingText.Spawn(middle + Vector3.up * 0.2f, modifiers.label, modifiers.color, 0.7f, 0.9f);
            Sfx.Play(m_Definition.katanaLaunchClip, middle, 1f, Random.Range(0.9f, 1.05f));
            if (grade == ShotGrade.Perfect && m_Bow != null)
                Sfx.Play(m_Bow.PerfectClip, middle, 0.7f);
            Haptics.Pulse(ShieldHand, 0.9f, 0.12f);
            Haptics.Pulse(KatanaHand, 0.4f, 0.06f);
        }

        // Frappée tout de suite : parfaite (vert) ; un peu après : bonne (orange) ; trop tard : ratée (rouge).
        ShotGrade Freshness(float age)
        {
            if (m_Bow != null && m_Bow.ForcePerfect)
                return ShotGrade.Perfect;
            var gold = m_Bow != null ? Mathf.Max(0.1f, m_Bow.GoldWidthMultiplier) : 1f;
            var band = m_Bow != null ? Mathf.Max(0.1f, m_Bow.BandWidthMultiplier) : 1f;
            if (age <= k_PerfectAge * gold)
                return ShotGrade.Perfect;
            return age <= k_GoodAge * band ? ShotGrade.Good : ShotGrade.Miss;
        }

        // ---------------------------------------------------------------- sans casque

        void SetDesktop(bool desktop)
        {
            m_Desktop = desktop;
            var camera = DesktopControls.Instance != null ? DesktopControls.Instance.CameraTransform : null;
            foreach (var weapon in new[] { m_Katana, m_Shield })
            {
                weapon.Suspend(desktop);
                if (desktop && camera != null)
                    weapon.transform.SetParent(camera, false);
                else
                    weapon.transform.SetParent(transform, true);
            }

            if (!desktop)
            {
                m_Katana.SnapToHolster();
                m_Shield.SnapToHolster();
            }

            m_SwingTime = -1f;
            m_BashTime = -1f;
            m_HasPrevious = false;
        }

        // Clic gauche : un coup de katana devant soi, d'un côté puis de l'autre. Clic droit maintenu : le bouclier levé ;
        // un clic droit le pousse en avant et lance la dernière déchirure vers le viseur.
        void UpdateDesktop(float deltaTime)
        {
            var controls = DesktopControls.Instance;
            var mouse = Mouse.current;
            var focus = controls != null && controls.HasFocus && !controls.RefocusedThisFrame && !DesktopHand.IsTypingText && mouse != null;
            if (focus && mouse.leftButton.wasPressedThisFrame && m_SwingTime < 0f)
            {
                m_SwingTime = 0f;
                m_SwingSide = -m_SwingSide;
            }

            if (focus && mouse.rightButton.wasPressedThisFrame && m_BashTime < 0f)
            {
                m_BashTime = 0f;
                LaunchNewestTrail(controls.CameraTransform);
            }

            // Le katana : au repos, lame dressée vers l'avant ; pendant le coup, elle balaie la vue en travers.
            var bladeDirection = new Vector3(0.15f, 0.55f, 1f);
            if (m_SwingTime >= 0f)
            {
                m_SwingTime += deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_SwingTime / k_DesktopSwing));
                var from = new Vector3(0.9f * m_SwingSide, 0.35f, 0.8f);
                var to = new Vector3(-0.9f * m_SwingSide, -0.25f, 0.8f);
                bladeDirection = Vector3.Slerp(from.normalized, to.normalized, t);
                if (m_SwingTime >= k_DesktopSwing)
                    m_SwingTime = -1f;
            }

            m_Katana.transform.localPosition = k_DesktopKatana;
            m_Katana.transform.localRotation = Quaternion.FromToRotation(Vector3.up, bladeDirection.normalized);

            // Le bouclier : abaissé sur le côté, levé devant soi (clic droit maintenu), poussé en avant (clic droit).
            var raised = mouse != null && mouse.rightButton.isPressed;
            var position = raised ? new Vector3(-0.1f, -0.12f, 0.42f) : k_DesktopShield;
            if (m_BashTime >= 0f)
            {
                m_BashTime += deltaTime;
                var push = Mathf.Sin(Mathf.Clamp01(m_BashTime / k_DesktopBash) * Mathf.PI);
                position += Vector3.forward * 0.25f * push;
                if (m_BashTime >= k_DesktopBash)
                    m_BashTime = -1f;
            }

            m_Shield.transform.localPosition = position;
            m_Shield.transform.localRotation = raised || m_BashTime >= 0f ? Quaternion.identity : Quaternion.Euler(20f, -35f, 0f);
        }

        // Sans casque, viser une déchirure avec le bouclier serait trop dur : le clic droit lance la plus récente, vers le
        // centre de l'écran, si elle est à portée de bras.
        void LaunchNewestTrail(Transform camera)
        {
            if (camera == null)
                return;
            Trail newest = null;
            var newestTime = float.MinValue;
            foreach (var trail in m_Trails)
            {
                if (trail.Launched || trail.Samples.Count < 2)
                    continue;
                var time = trail.Samples[trail.Samples.Count - 1].Time;
                var near = (trail.Samples[trail.Samples.Count / 2].Tip - camera.position).sqrMagnitude < 2.25f;
                if (near && time > newestTime)
                {
                    newest = trail;
                    newestTime = time;
                }
            }

            if (newest != null)
                Launch(newest, camera.forward);
        }

        // ---------------------------------------------------------------- les déchirures

        Trail NewTrail()
        {
            var material = m_Definition.katanaTearMaterial;
            if (material == null)
            {
                if (m_FallbackTrailMaterial == null)
                    m_FallbackTrailMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Déchirure (secours)" };
                material = m_FallbackTrailMaterial;
            }

            var trail = new Trail { Object = new GameObject("Déchirure du katana"), Mesh = new Mesh { name = "Déchirure du katana" } };
            trail.Mesh.MarkDynamic();
            trail.Object.AddComponent<MeshFilter>().sharedMesh = trail.Mesh;
            var renderer = trail.Object.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return trail;
        }

        // Chaque bout s'efface en restant sur place ; une déchirure lancée suit sa flèche (ou file tout droit) et s'efface.
        void UpdateTrails()
        {
            var now = Time.time;
            for (var i = m_Trails.Count - 1; i >= 0; i--)
            {
                var trail = m_Trails[i];
                float fade;
                if (trail.Launched)
                {
                    var age = now - trail.LaunchTime;
                    fade = 1f - age / k_LaunchFade;
                    trail.Offset = trail.Arrow != null && trail.Arrow.isActiveAndEnabled
                        ? trail.Arrow.transform.position - trail.ArrowStart
                        : trail.Velocity * age;
                }
                else
                {
                    trail.Samples.RemoveAll(sample => now - sample.Time > k_TrailLife);
                    fade = 1f;
                }

                if (fade <= 0f || trail.Samples.Count == 0)
                {
                    if (trail == m_Current)
                        m_Current = null;
                    Destroy(trail.Object);
                    Destroy(trail.Mesh);
                    m_Trails.RemoveAt(i);
                    continue;
                }

                BuildMesh(trail, now, fade);
            }
        }

        // Un ruban entre le milieu de la lame et la pointe : U le long de la déchirure, V en travers (0 côté poignée), la
        // couleur (alpha : le temps qui reste à chaque bout) pour le shader de la déchirure.
        void BuildMesh(Trail trail, float now, float fade)
        {
            m_Vertices.Clear();
            m_Colors.Clear();
            m_Uvs.Clear();
            m_Triangles.Clear();
            var samples = trail.Samples;
            var last = Mathf.Max(1, samples.Count - 1);
            for (var i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                var color = trail.Color;
                color.a = trail.Launched ? fade : Mathf.Clamp01(1f - (now - sample.Time) / k_TrailLife);
                m_Vertices.Add(sample.Base + trail.Offset);
                m_Vertices.Add(sample.Tip + trail.Offset);
                m_Colors.Add(color);
                m_Colors.Add(color);
                var u = i / (float)last;
                m_Uvs.Add(new Vector2(u, 0f));
                m_Uvs.Add(new Vector2(u, 1f));
                if (i == 0)
                    continue;
                var a = (i - 1) * 2;
                m_Triangles.Add(a);
                m_Triangles.Add(a + 1);
                m_Triangles.Add(a + 3);
                m_Triangles.Add(a);
                m_Triangles.Add(a + 3);
                m_Triangles.Add(a + 2);
            }

            var mesh = trail.Mesh;
            mesh.Clear();
            mesh.SetVertices(m_Vertices);
            mesh.SetColors(m_Colors);
            mesh.SetUVs(0, m_Uvs);
            mesh.SetTriangles(m_Triangles, 0);
            mesh.RecalculateBounds();
        }

        // ---------------------------------------------------------------- raccourci de test

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // O : le katana et le bouclier tout de suite (même verrouillés, même pendant la partie) ; O encore : l'arme choisie.
        sealed class TestKey : MonoBehaviour
        {
            BowDefinition m_Previous;

            void Update()
            {
                var keyboard = Keyboard.current;
                if (keyboard == null || !keyboard.oKey.wasPressedThisFrame)
                    return;
                var bow = FindAnyObjectByType<Bow>();
                var loadout = BowLoadout.Instance;
                if (bow == null || loadout == null)
                    return;

                if (bow.IsKatana)
                {
                    var back = m_Previous != null && !m_Previous.katana ? m_Previous : loadout.Selected;
                    if (back != null && back.katana)
                        back = bow.StartingDefinition;
                    bow.SetDefinition(back);
                    HeadMessages.Show((back != null ? back.displayName : "Arc") + " équipé", new Color(0.55f, 0.9f, 1f), 2f);
                    return;
                }

                foreach (var entry in loadout.Entries)
                {
                    if (entry == null || entry.bow == null || !entry.bow.katana)
                        continue;
                    m_Previous = bow.Definition;
                    bow.SetDefinition(entry.bow);
                    HeadMessages.Show(entry.bow.displayName + " équipés (test)\n<size=60%>O : retour à l'arme d'avant</size>", entry.color, 3f);
                    return;
                }

                HeadMessages.Show("Pas de katana dans la liste des armes (menu Archery > Katana et bouclier)", new Color(1f, 0.6f, 0.55f), 3f);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallTestKey()
        {
            var go = new GameObject("Raccourci katana et bouclier (touche O)");
            DontDestroyOnLoad(go);
            go.AddComponent<TestKey>();
        }
#endif
    }
}
