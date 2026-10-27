using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Grenades
{
    /// <summary>
    /// Grenade à flèches : sortie du bas du dos par la <see cref="GrenadeBelt"/>, lancée à la main,
    /// elle éclate au premier impact et projette des flèches dans toutes les directions.
    /// Elle ne fait aucun dégât elle-même : ce sont les flèches, tirées de l'<see cref="ArrowPool"/>.
    /// </summary>
    /// <remarks>
    /// À placer sur le prefab de la grenade, à côté d'un XR Grab Interactable :
    /// c'est lui qui fait suivre la main et qui donne la vitesse du bras au lâcher.
    /// Revue le 22 octobre après les tests : elle suit la main sans retard (prise instantanée, plus de collision avec le
    /// corps du joueur), vole plus loin, éclate au contact d'un ennemi (ou en le frôlant), et ses flèches partent du
    /// centre de l'explosion, une partie visant les ennemis proches : avant, elles naissaient à plus d'un mètre du
    /// centre, au-delà de l'ennemi touché. Puis, le même jour : le moindre contact la fait éclater, deux fois plus de
    /// flèches, aux dégâts d'une flèche vert clair de l'arc × 0,8, et la mega-bomba : une flèche du joueur qui passe dans
    /// une grosse zone autour d'elle en vol la fait éclater en trois fois plus de flèches, avec « MEGA-BOMBA ! » au-dessus.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ArrowGrenade : MonoBehaviour
    {
        // Le talisman de la super grenade (26 octobre) : les flèches en plus par niveau.
        const int k_SuperGrenadeArrows = 12;

        [Header("Flèches projetées")]
        [Tooltip("Nombre de flèches, avant l'amélioration « Grenade sauvage » (14 jusqu'au 22 octobre).")]
        [Min(1)]
        [SerializeField]
        int m_ArrowCount = 28;

        [Tooltip("Vitesse de départ des flèches (m/s). L'arc de base tire à 35.")]
        [SerializeField]
        float m_ArrowSpeed = 16f;

        [Tooltip("Dégâts d'une flèche : cette part de ceux d'une flèche vert clair (« très bon ») de l'arc tendu à fond, " +
                 "améliorations et bonus des coffres compris (0,8 depuis le 22 octobre). Avec les Akimbow ou le katana en " +
                 "main, ceux de l'arc du début de partie.")]
        [Range(0f, 3f)]
        [SerializeField]
        float m_HeldShotShare = 0.8f;

        [Tooltip("Dégâts d'une flèche s'il n'y a pas d'arc dans la scène, avant les améliorations de dégâts.")]
        [SerializeField]
        float m_ArrowDamage = 6f;

        [Tooltip("Flèches tirées droit sur les ennemis les plus proches (2 au plus par ennemi) ; les autres partent tout autour.")]
        [Min(0)]
        [SerializeField]
        int m_AimedArrows = 12;

        [Tooltip("Distance (m) jusqu'à laquelle la grenade vise les ennemis.")]
        [SerializeField]
        float m_AimRadius = 7f;

        [Tooltip("Hauteur des flèches qui partent tout autour, en degrés au-dessus de l'horizontale (de x à y) : à hauteur " +
                 "d'ennemi, ni dans le sol ni dans le ciel.")]
        [SerializeField]
        Vector2 m_ElevationRange = new Vector2(-8f, 30f);

        [Tooltip("Couleur de la traînée des flèches de la grenade.")]
        [SerializeField]
        Color m_TrailColor = new Color(1f, 0.55f, 0.15f);

        [Header("Lancer")]
        [Tooltip("Multiplie la vitesse du bras au lâcher (XR Grab Interactable, 1,5 avant le 22 octobre).")]
        [SerializeField]
        float m_ThrowVelocityScale = 2f;

        [Tooltip("Gravité de la grenade en vol, en multiple de la normale : sous 1, elle vole plus loin.")]
        [SerializeField]
        float m_GravityScale = 0.7f;

        [Header("Explosion")]
        [Tooltip("Délai (s) après le lâcher avant qu'un ennemi frôlé la fasse éclater. Un contact, lui, la fait éclater " +
                 "tout de suite (sauf avec le joueur, son arc et ses armes).")]
        [SerializeField]
        float m_ArmDelay = 0.12f;

        [Tooltip("Éclate d'elle-même si elle n'a rien touché au bout de ce temps (0 = jamais).")]
        [SerializeField]
        float m_FuseTime = 6f;

        [Tooltip("Distance (m) à laquelle un ennemi frôlé la fait éclater, même sans la toucher.")]
        [SerializeField]
        float m_ProximityRadius = 0.35f;

        [Tooltip("Optionnel : effet visuel joué au point d'explosion.")]
        [SerializeField]
        GameObject m_ExplosionEffect;

        [SerializeField]
        AudioClip m_ExplosionClip;

        [Tooltip("Vibration de la manette qui vient de la lancer.")]
        [SerializeField]
        float m_ThrowHapticAmplitude = 0.35f;

        [Header("Mèche")]
        [Tooltip("Optionnel : les étincelles de la mèche (la mèche du modèle est éteinte, elles la font brûler).")]
        [SerializeField]
        ParticleSystem m_FuseSparks;

        [Tooltip("Multiplie les étincelles de la mèche une fois la grenade lancée.")]
        [SerializeField]
        float m_ThrownSparkBoost = 2.5f;

        [Header("Mega-bomba")]
        [Tooltip("Rayon (m) d'une zone bien plus grosse que la grenade, active pendant son vol : une flèche tirée par le " +
                 "joueur qui y passe la fait éclater en mega-bomba.")]
        [SerializeField]
        float m_MegaRadius = 0.6f;

        [Tooltip("Flèches de la mega-bomba, en multiple d'une explosion normale (améliorations comprises).")]
        [SerializeField]
        float m_MegaMultiplier = 3f;

        [Tooltip("Message affiché au-dessus de la bombe.")]
        [SerializeField]
        string m_MegaText = "MEGA-BOMBA !";

        [SerializeField]
        Color m_MegaColor = new Color(1f, 0.3f, 0.75f);

        // Les pointes partent un peu derrière le centre : un ennemi collé à la grenade est devant elles, pas derrière.
        const float k_TipBehindCenter = 0.1f;

        // Portée du laser de l'arc cosmique, pour savoir s'il traverse la zone de la mega-bomba.
        const float k_LaserRange = 120f;

        static readonly Collider[] s_Overlaps = new Collider[16];
        static readonly RaycastHit[] s_Casts = new RaycastHit[16];
        static readonly List<Enemy> s_Targets = new List<Enemy>();

        // Les flèches tirées par le joueur pendant le vol de la grenade : celles qui peuvent la toucher.
        readonly List<Arrow> m_PlayerArrows = new List<Arrow>();

        XRGrabInteractable m_Grab;
        Rigidbody m_Body;
        Collider[] m_Colliders;
        IXRSelectInteractor m_Hand;
        bool m_Armed;
        bool m_Exploded;
        bool m_NeedsIgnore;
        bool m_MegaPending;
        float m_ArmedAt;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Body = GetComponent<Rigidbody>();
            m_Colliders = GetComponentsInChildren<Collider>(true);

            // Instantanée : collée à la main même quand on se déplace (en « Velocity Tracking », la physique la tirait vers
            // la main avec du retard, et elle décrochait).
            m_Grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            m_Grab.attachEaseInTime = 0f;
            m_Grab.useDynamicAttach = false;
            m_Grab.throwOnDetach = true;
            m_Grab.throwVelocityScale = m_ThrowVelocityScale;
            m_Body.interpolation = RigidbodyInterpolation.Interpolate;
            m_Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        void OnEnable()
        {
            m_Grab.selectEntered.AddListener(OnSelectEntered);
            m_Grab.selectExited.AddListener(OnSelectExited);
            Bow.ShotFired += OnShotFired;
        }

        void OnDisable()
        {
            m_Grab.selectEntered.RemoveListener(OnSelectEntered);
            m_Grab.selectExited.RemoveListener(OnSelectExited);
            Bow.ShotFired -= OnShotFired;
        }

        // Un tir du joueur (arc, Akimbow, déchirure du katana) pendant le vol : sa flèche est suivie jusqu'à ce qu'elle
        // touche quelque chose. Le laser de l'arc cosmique, instantané, est testé tout de suite.
        void OnShotFired(Bow bow, ShotInfo shot)
        {
            if (!m_Armed || m_Exploded)
                return;

            if (shot.Arrow != null && shot.Arrow.CurrentState == Arrow.State.Flying)
            {
                m_PlayerArrows.Add(shot.Arrow);
                return;
            }

            var direction = shot.Direction.sqrMagnitude > 1e-6f ? shot.Direction.normalized : Vector3.forward;
            var along = Mathf.Clamp(Vector3.Dot(m_Body.position - shot.Origin, direction), 0f, k_LaserRange);
            if (Vector3.Distance(shot.Origin + direction * along, m_Body.position) <= m_MegaRadius)
                m_MegaPending = true;
        }

        /// <summary>
        /// Place la grenade dans la main et la fait saisir : la ceinture ne passe pas par un survol.
        /// </summary>
        public bool TryPutInHand(IXRSelectInteractor hand)
        {
            if (hand == null || m_Grab.interactionManager == null)
                return false;

            var attach = hand.GetAttachTransform(m_Grab);
            transform.SetPositionAndRotation(attach.position, attach.rotation);

            m_Grab.interactionManager.SelectEnter(hand, (IXRSelectInteractable)m_Grab);
            return m_Grab.isSelected;
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            m_Hand = args.interactorObject;
            IgnoreBody();
        }

        // Lâchée : elle part avec la vitesse du bras (XRI s'en charge) et devient dangereuse.
        void OnSelectExited(SelectExitEventArgs args)
        {
            if (m_Armed)
                return;

            // Lâchée pendant la pause : elle resterait figée en l'air. Elle retourne à la ceinture, sans recharge.
            if (SlowMotion.IsPaused)
            {
                if (GrenadeBelt.Instance != null)
                    GrenadeBelt.Instance.OnGrenadeReturned(this);
                m_Hand = null;
                Destroy(gameObject);
                return;
            }

            m_Armed = true;
            m_ArmedAt = Time.time;

            // Le joueur rend ses collisions à ce qu'il lâche : on les recoupe au pas physique suivant.
            m_NeedsIgnore = true;

            // Lancée, la mèche crache plus fort.
            if (m_FuseSparks != null)
            {
                var emission = m_FuseSparks.emission;
                emission.rateOverTimeMultiplier *= m_ThrownSparkBoost;
            }

            Haptics.Pulse(m_Hand, m_ThrowHapticAmplitude, 0.06f);
            m_Hand = null;

            if (GrenadeBelt.Instance != null)
                GrenadeBelt.Instance.OnGrenadeThrown(this);
        }

        void Update()
        {
            if (!m_Armed || m_Exploded || m_FuseTime <= 0f)
                return;

            if (Time.time - m_ArmedAt >= m_FuseTime)
                Explode();
        }

        void FixedUpdate()
        {
            if (!m_Armed || m_Exploded || m_Body.isKinematic)
                return;

            if (m_NeedsIgnore)
                IgnoreBody();

            // Plus légère que la normale : elle porte plus loin.
            if (!Mathf.Approximately(m_GravityScale, 1f))
                m_Body.AddForce(Physics.gravity * (m_GravityScale - 1f), ForceMode.Acceleration);

            if (m_MegaPending || ArrowCrossesMegaZone())
            {
                ExplodeAt(m_Body.position, true);
                return;
            }

            if (Time.time - m_ArmedAt >= m_ArmDelay && TouchesEnemy(out var point))
                ExplodeAt(point);
        }

        // Une flèche du joueur passe dans la grosse zone autour de la grenade, sur ce pas physique (avant ou après le
        // déplacement de la flèche : l'ordre des scripts n'est pas connu).
        bool ArrowCrossesMegaZone()
        {
            var center = m_Body.position;
            var deltaTime = Time.fixedDeltaTime;
            for (var i = m_PlayerArrows.Count - 1; i >= 0; i--)
            {
                var arrow = m_PlayerArrows[i];
                if (arrow == null || arrow.CurrentState != Arrow.State.Flying)
                {
                    m_PlayerArrows.RemoveAt(i);
                    continue;
                }

                var tip = arrow.TipPosition;
                var step = arrow.Velocity * deltaTime;
                if (DistanceToSegment(center, tip - step, tip + step) <= m_MegaRadius)
                    return true;
            }

            return false;
        }

        static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var segment = b - a;
            var length = segment.sqrMagnitude;
            var t = length > 1e-8f ? Mathf.Clamp01(Vector3.Dot(point - a, segment) / length) : 0f;
            return Vector3.Distance(point, a + segment * t);
        }

        // Un ennemi à portée de la grenade, ou sur son chemin jusqu'au prochain pas physique (triggers compris) : elle
        // éclate dessus au lieu de rebondir sur lui ou de passer à côté.
        bool TouchesEnemy(out Vector3 point)
        {
            point = m_Body.position;
            var count = Physics.OverlapSphereNonAlloc(point, m_ProximityRadius, s_Overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                if (IsEnemy(s_Overlaps[i]))
                    return true;
            }

            var velocity = m_Body.linearVelocity;
            var distance = velocity.magnitude * Time.fixedDeltaTime;
            if (distance < 1e-3f)
                return false;

            var direction = velocity.normalized;
            count = Physics.SphereCastNonAlloc(point, m_ProximityRadius, direction, s_Casts, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                if (!IsEnemy(s_Casts[i].collider))
                    continue;

                point += direction * Mathf.Max(0f, s_Casts[i].distance);
                return true;
            }

            return false;
        }

        static bool IsEnemy(Collider collider)
        {
            if (collider == null || ArrowIgnore.IsIgnored(collider))
                return false;

            var enemy = collider.GetComponentInParent<Enemy>();
            if (enemy != null)
                return enemy.IsAlive;

            // Les boss sans Enemy (le dragon) : une zone de touche qui n'est pas celle du joueur.
            var hitbox = collider.GetComponentInParent<Hitbox>();
            var player = PlayerHealth.Instance;
            return hitbox != null && (player == null || hitbox.Owner != player.Health);
        }

        // Le moindre contact la fait éclater (sol, mur, barricade, ennemi…), sans délai : elle ne rebondit plus.
        void OnCollisionEnter(Collision collision) => OnContact(collision.collider);

        void OnCollisionStay(Collision collision) => OnContact(collision.collider);

        void OnContact(Collider other)
        {
            if (m_Armed && !m_Exploded && !IsPlayerGear(other))
                Explode();
        }

        // Le joueur, son arc, ses flèches et ses armes : on ne s'éclate pas dessus.
        static bool IsPlayerGear(Collider collider)
        {
            if (collider == null || collider.GetComponentInParent<Bow>() != null || collider.GetComponentInParent<Arrow>() != null ||
                collider.GetComponentInParent<Akimbow>() != null || collider.GetComponentInParent<FloatingGrab>() != null)
                return true;

            var rig = PlayerRig.Instance;
            return rig != null && rig.Origin != null && collider.transform.IsChildOf(rig.Origin.transform);
        }

        // Plus de collision avec le corps du joueur : tenue, elle ne pousse plus contre lui ; lancée, elle ne lui éclate
        // pas dessus.
        void IgnoreBody()
        {
            m_NeedsIgnore = false;
            var rig = PlayerRig.Instance;
            var body = rig != null && rig.Origin != null ? rig.Origin.GetComponentInChildren<CharacterController>(true) : null;
            if (body == null || !body.enabled || !body.gameObject.activeInHierarchy)
                return;

            foreach (var collider in m_Colliders)
            {
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy)
                    Physics.IgnoreCollision(body, collider, true);
            }
        }

        /// <summary>Fait éclater la grenade tout de suite.</summary>
        [ContextMenu("Faire éclater")]
        public void Explode() => ExplodeAt(m_Body != null ? m_Body.position : transform.position);

        void ExplodeAt(Vector3 center, bool mega = false)
        {
            if (m_Exploded)
                return;

            m_Exploded = true;

            // Ses flèches ne doivent pas se planter dans elle avant qu'elle disparaisse, à la fin de l'image.
            foreach (var collider in m_Colliders ?? GetComponentsInChildren<Collider>())
            {
                if (collider != null)
                    collider.enabled = false;
            }

            // Mega-bomba : trois fois plus de flèches, une explosion plus grosse et plus grave, et le message au-dessus.
            var multiplier = mega ? Mathf.Max(1f, m_MegaMultiplier) : 1f;
            SpawnArrows(center, multiplier);

            if (m_ExplosionEffect != null)
            {
                var effect = Instantiate(m_ExplosionEffect, center, Quaternion.identity);
                if (mega)
                    effect.transform.localScale *= 1.8f;
                Destroy(effect, 5f);
            }

            Sfx.Play(m_ExplosionClip, center, mega ? 1f : 0.9f, mega ? Random.Range(0.75f, 0.8f) : Random.Range(0.95f, 1.05f));
            if (mega)
                FloatingText.Spawn(center + Vector3.up * 0.6f, m_MegaText, m_MegaColor, 4f, 2.2f);
            Destroy(gameObject);
        }

        void SpawnArrows(Vector3 center, float multiplier)
        {
            var pool = ArrowPool.Instance;
            if (pool == null)
            {
                Debug.LogWarning("Pas d'Arrow Pool dans la scène : la grenade n'a pas projeté de flèches.", this);
                return;
            }

            // Grenade sauvage (retirée le 26 octobre) et le talisman de la super grenade : 12 flèches de plus par niveau.
            var upgrades = PlayerUpgrades.Instance;
            var count = m_ArrowCount;
            if (upgrades != null)
                count += Mathf.RoundToInt(upgrades.Total(UpgradeEffect.GrenadeArrows)) + k_SuperGrenadeArrows * upgrades.Stacks(UpgradeEffect.SuperGrenade);

            count = Mathf.Max(1, Mathf.RoundToInt(count * multiplier));
            var damage = ArrowDamage(upgrades);

            // D'abord les ennemis les plus proches, 2 flèches chacun au plus ; le reste part tout autour.
            var aimedArrows = Mathf.RoundToInt(m_AimedArrows * multiplier);
            FindTargets(center, aimedArrows);
            var aimed = Mathf.Min(count, aimedArrows, s_Targets.Count * 2);
            var around = count - aimed;
            for (var i = 0; i < count; i++)
            {
                var direction = i < aimed ? AimAt(center, s_Targets[i % s_Targets.Count]) : AroundDirection(i - aimed, around);
                var arrow = pool.Get();
                var tip = center - direction * k_TipBehindCenter;
                arrow.transform.SetPositionAndRotation(tip - direction * arrow.Length, Quaternion.LookRotation(direction));
                arrow.Launch(new ArrowLaunch
                {
                    Velocity = direction * m_ArrowSpeed,
                    Damage = damage,
                    Grade = ShotGrade.Ok,
                    TrailColor = m_TrailColor,
                    IsShot = true,

                    // Une flèche de grenade qui ne touche rien ne casse pas le combo du joueur.
                    IsExtra = true,
                });
            }
        }

        // Les dégâts d'une flèche vert clair (« très bon ») de l'arc tendu à fond, fois Held Shot Share, comme le tir de
        // l'arc : dégâts de l'arc, puissance à pleine tension, qualité, améliorations et bonus des coffres. Avec les Akimbow
        // ou le katana à la place de l'arc, l'arc du début de partie.
        float ArrowDamage(PlayerUpgrades upgrades)
        {
            var bow = FindAnyObjectByType<Bow>();
            var definition = bow == null ? null : bow.IsReplaced || bow.Definition == null ? bow.StartingDefinition : bow.Definition;
            if (definition == null)
                return m_ArrowDamage * (upgrades != null ? upgrades.DamageMultiplier : 1f);

            var tuning = bow.ShotTuning != null ? bow.ShotTuning : ShotTuning.Fallback;
            var power = definition.drawToPower != null ? definition.drawToPower.Evaluate(1f) : 1f;
            return definition.damage * power * tuning.Get(ShotGrade.Held).damage * bow.DamageMultiplier * m_HeldShotShare;
        }

        void FindTargets(Vector3 center, int aimedArrows)
        {
            s_Targets.Clear();
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy != null && enemy.IsAlive && Vector3.Distance(enemy.Center, center) <= m_AimRadius)
                    s_Targets.Add(enemy);
            }

            s_Targets.Sort((a, b) => Vector3.SqrMagnitude(a.Center - center).CompareTo(Vector3.SqrMagnitude(b.Center - center)));
            var keep = Mathf.CeilToInt(aimedArrows * 0.5f);
            if (s_Targets.Count > keep)
                s_Targets.RemoveRange(keep, s_Targets.Count - keep);
        }

        // Droit sur le centre de l'ennemi, un peu au-dessus pour compenser la chute de la flèche.
        Vector3 AimAt(Vector3 center, Enemy enemy)
        {
            var target = enemy.Center;
            var time = Vector3.Distance(center, target) / Mathf.Max(1f, m_ArrowSpeed);
            target += Vector3.up * (0.5f * -Physics.gravity.y * time * time);
            var direction = target - center;
            return direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.down;
        }

        /// <summary>
        /// Répartit régulièrement des directions tout autour (spirale de Fibonacci), entre les deux hauteurs de
        /// « Elevation Range » : à hauteur d'ennemi, ni dans le sol ni dans le ciel.
        /// </summary>
        Vector3 AroundDirection(int index, int count)
        {
            const float goldenAngle = 2.39996323f;

            var low = Mathf.Sin(Mathf.Min(m_ElevationRange.x, m_ElevationRange.y) * Mathf.Deg2Rad);
            var high = Mathf.Sin(Mathf.Max(m_ElevationRange.x, m_ElevationRange.y) * Mathf.Deg2Rad);
            var y = Mathf.Lerp(high, low, (index + 0.5f) / Mathf.Max(1, count));
            var radius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            var theta = goldenAngle * index;
            return new Vector3(Mathf.Cos(theta) * radius, y, Mathf.Sin(theta) * radius);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, m_ProximityRadius);
            Gizmos.color = new Color(1f, 0.3f, 0.75f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, m_MegaRadius);
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, m_AimRadius);
        }
    }
}
