using System.Collections;
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
    /// Le 13 novembre, la batte de baseball (l'idée du joueur, <c>Combat/BaseballBat</c>) : lancée puis frappée par la batte,
    /// la grenade file en ligne droite sur l'ennemi que le joueur regarde, le suit, et éclate sur lui ; ses flèches ont la
    /// qualité du coup (le chemin parcouru par la grenade avant le coup), et des flammes la suivent, plus grosses à chaque
    /// qualité.
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

        // Frappée par la batte (13 novembre) : elle tourne vers sa cible de 720° par seconde au plus (elle « fonce directement
        // sur l'ennemi »), et éclate d'elle-même au bout de 3 s si elle n'a rien touché.
        const float k_BatTurnRate = 720f;
        const float k_BatFuse = 3f;

        // L'écho, avec la batte : les explosions en plus, au même endroit, à l'intervalle de l'écho des flèches (0,3 s au moins,
        // pour qu'on les entende séparées).
        const float k_EchoMinDelay = 0.3f;

        // Le lancer en l'air, avec la batte (13 novembre au soir, après l'essai du joueur : « quand on lance la grenade vers le haut,
        // elle retombe rarement sur nous, le geste de lancer en VR est rarement précis ») : une grenade lancée à peu près vers le
        // haut (à 50° au plus de la verticale, à 1,5 m/s au moins vers le haut) monte tout droit, à 4 m au plus, et redescend
        // à portée de batte ; s'il se déplace, elle suit le joueur (à 8 m/s au plus) et reste au-dessus de cet endroit.
        // Revu après le deuxième essai, le même soir (le joueur : « la balle devrait retomber du côté où le joueur tient la batte,
        // pour éviter qu'il ait à frapper en face de lui ») : 70 cm sur le côté de la main qui tient la batte, 50 cm devant les
        // yeux (avant : de 55 à 85 cm devant, là où la main l'avait lâchée). Et la course ne compte plus dans le lancer (« quand on
        // lance la balle au-dessus de nous en courant, le mouvement prend en compte la course, ce qui empêche de la lancer au bon
        // angle ») : la vitesse du joueur (son XR Origin, mesurée pendant qu'il tient la grenade) est retirée de celle du lancer.
        // Revu le 14 novembre (le joueur : « elle décolle en se calant directement sur l'axe à notre droite, puis retombe droit ;
        // elle devrait plutôt faire cela progressivement, en cloche ») : elle n'est plus ramenée tout de suite au-dessus de sa place,
        // elle y va en courbe, à la vitesse qu'il faut pour y arriver juste quand elle redescend à la hauteur où la main l'a
        // lâchée (recalculée à chaque pas : si le joueur se déplace, la courbe suit) ; passée sous cette hauteur, elle tombe.
        const float k_TossAngle = 50f;
        const float k_TossMinUpSpeed = 1.5f;
        const float k_TossMaxHeight = 4f;
        const float k_TossSide = 0.7f;
        const float k_TossForward = 0.5f;
        const float k_TossFollowSpeed = 8f;
        const int k_TossChecks = 4;
        const float k_MaxPlayerSpeed = 12f;

        // La gravité avec la batte (13 novembre au soir, le joueur : « les grenades sont super flottantes ; je l'avais fait pour qu'on
        // puisse tirer dessus à l'arc et faire une super explosion, mais ça rend le jeu à la batte moins bon : il faudrait augmenter
        // leur gravité, mais seulement à la batte ») : 1,2 fois la normale au lieu de Gravity Scale (0,7). Lancée vers le haut,
        // sa vitesse monte d'autant (la racine du rapport des gravités) : un même geste l'envoie à la même hauteur, elle y passe
        // juste moins de temps (un quart de moins) ; la qualité du coup, qui vient du chemin parcouru, ne change pas.
        const float k_BatGravityScale = 1.2f;

        static readonly Collider[] s_Overlaps = new Collider[16];
        static readonly RaycastHit[] s_Casts = new RaycastHit[16];
        static readonly List<Enemy> s_Targets = new List<Enemy>();

        // Les grenades lâchées qui volent encore : la batte les cherche là.
        static readonly List<ArrowGrenade> s_InFlight = new List<ArrowGrenade>();

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

        // La batte : le chemin parcouru depuis le lâcher (la qualité du coup en dépend), puis, frappée, sa qualité, sa cible, sa
        // vitesse et ses flammes.
        float m_PathLength;
        Vector3 m_LastPosition;
        bool m_Batted;
        ShotGrade m_BatGrade;
        Enemy m_BatTarget;
        float m_BatSpeed;
        ParticleSystem m_Flames;

        // Ce que les améliorations donnent à une grenade frappée (13 novembre) : ses dégâts (la grenade fantôme), le nombre de
        // ses flèches (la corde de siège), ses explosions en plus (l'écho) ; et la grenade fantôme, qui flotte devant le joueur
        // en attendant la batte.
        float m_DamageBonus = 1f;
        float m_ArrowBonus = 1f;
        int m_Echoes;
        bool m_Ghost;
        bool m_Floating;

        // Le lancer en l'air, avec la batte : à vérifier dès que la vitesse du lâcher est connue, puis la colonne au-dessus du
        // joueur (l'écart entre ses yeux et elle, à l'horizontale).
        bool m_TossPending;
        int m_TossTries;
        bool m_Column;
        Vector3 m_ColumnOffset;
        float m_TossHeight;

        // Lancée avec la batte en main : sa gravité est celle de la batte.
        bool m_WithBat;

        float GravityScale => m_WithBat ? k_BatGravityScale : m_GravityScale;

        // La vitesse horizontale du joueur (son XR Origin : la course aux bras, le joystick), mesurée tant qu'il tient la grenade.
        Vector3 m_PlayerVelocity;
        Vector3 m_LastOrigin;
        bool m_HasOrigin;

        /// <summary>Les grenades lâchées qui volent encore (pas encore éclatées), et la grenade fantôme qui flotte.</summary>
        public static IReadOnlyList<ArrowGrenade> InFlight => s_InFlight;

        /// <summary>Lâchée, en vol (ou la grenade fantôme qui flotte), pas encore frappée par la batte.</summary>
        public bool CanBeBatted => m_Armed && !m_Exploded && !m_Batted;

        /// <summary>La grenade fantôme de la batte, qui flotte en attendant d'être frappée (<see cref="SpawnGhost"/>).</summary>
        public bool IsFloatingGhost => m_Floating;

        /// <summary>Une grenade fantôme (ou une copie d'elle), qui flotte ou qui vole.</summary>
        public bool IsGhost => m_Ghost;

        /// <summary>Le temps (s) depuis le lâcher (la corde de siège avec la batte).</summary>
        public float AirTime => m_Armed && !m_Batted ? Time.time - m_ArmedAt : 0f;

        /// <summary>Le chemin parcouru (m) depuis le lâcher : une grenade lancée haut monte puis redescend, il compte les deux.</summary>
        public float PathLength => m_PathLength;

        public Vector3 Position => m_Body != null ? m_Body.position : transform.position;

        /// <summary>Ses colliders (la batte ne la bouscule pas : elle la frappe par son propre calcul).</summary>
        public IReadOnlyList<Collider> Colliders => m_Colliders;

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
            s_InFlight.Remove(this);
        }

        // Un tir du joueur (arc, Akimbow, déchirure du katana) pendant le vol : sa flèche est suivie jusqu'à ce qu'elle
        // touche quelque chose. Le laser de l'arc cosmique, instantané, est testé tout de suite. Une grenade frappée par la
        // batte ne devient pas une mega-bomba : le coup de batte compte comme un tir, parti d'elle.
        void OnShotFired(Bow bow, ShotInfo shot)
        {
            if (!m_Armed || m_Exploded || m_Batted || m_Floating)
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
            m_PathLength = 0f;
            m_LastPosition = m_Body.position;
            if (!s_InFlight.Contains(this))
                s_InFlight.Add(this);
            var bow = FindAnyObjectByType<Bow>();
            m_WithBat = bow != null && bow.IsBat;
            m_TossPending = m_WithBat;
            m_TossTries = 0;

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
            if (!m_Armed && m_Hand != null)
                TrackPlayer();
            if (!m_Armed || m_Exploded || m_Floating)
                return;

            var fuse = m_Batted ? k_BatFuse : m_FuseTime;
            if (fuse > 0f && Time.time - m_ArmedAt >= fuse)
                Explode();
        }

        void FixedUpdate()
        {
            if (!m_Armed || m_Exploded || m_Body.isKinematic)
                return;

            if (m_NeedsIgnore)
                IgnoreBody();

            var position = m_Body.position;
            m_PathLength += Vector3.Distance(position, m_LastPosition);
            m_LastPosition = position;

            // Lancée en l'air avec la batte : tout droit vers le haut, puis au-dessus du joueur.
            if (m_TossPending)
                TryToss();
            if (m_Column && !m_Batted)
                FollowColumn();

            // Frappée par la batte : en ligne droite, sans gravité, vers sa cible.
            if (m_Batted)
                SteerBatted();

            // Plus légère que la normale : elle porte plus loin (plus lourde avec la batte).
            else if (!Mathf.Approximately(GravityScale, 1f))
                m_Body.AddForce(Physics.gravity * (GravityScale - 1f), ForceMode.Acceleration);

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

        // Une autre grenade ne la fait pas éclater (14 novembre, le joueur : avec la corde triple, « ma grenade se cassait dès que je
        // la frappais » : les copies du multitir naissent au même endroit et se touchaient au premier pas physique).
        void OnContact(Collider other)
        {
            if (m_Armed && !m_Exploded && !m_Floating && !IsPlayerGear(other) && other.GetComponentInParent<ArrowGrenade>() == null)
                Explode();
        }

        /// <summary>Ces grenades ne se heurtent pas entre elles (les copies d'une même volée, nées au même endroit).</summary>
        public static void IgnoreTogether(IReadOnlyList<ArrowGrenade> grenades)
        {
            for (var i = 0; i < grenades.Count; i++)
            {
                for (var j = i + 1; j < grenades.Count; j++)
                {
                    if (grenades[i] == null || grenades[j] == null)
                        continue;
                    foreach (var a in grenades[i].m_Colliders)
                    {
                        foreach (var b in grenades[j].m_Colliders)
                        {
                            if (a != null && b != null)
                                Physics.IgnoreCollision(a, b, true);
                        }
                    }
                }
            }
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
            s_InFlight.Remove(this);
            StopFlames();

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

            // L'écho (frappée par la batte) : elle disparaît, mais éclate encore au même endroit, un instant après.
            if (m_Echoes > 0)
            {
                Vanish();
                StartCoroutine(EchoBursts(center, multiplier));
                return;
            }

            Destroy(gameObject);
        }

        // Plus rien ne se voit ni ne bouge : il ne reste que les explosions de l'écho à venir.
        void Vanish()
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
            if (m_FuseSparks != null)
                m_FuseSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            m_Body.isKinematic = true;
        }

        IEnumerator EchoBursts(Vector3 center, float multiplier)
        {
            var delay = Mathf.Max(k_EchoMinDelay, SpecialArrows.Instance != null ? SpecialArrows.Instance.EchoDelay : 0f);
            for (var i = 0; i < m_Echoes; i++)
            {
                yield return new WaitForSeconds(delay);
                SpawnArrows(center, multiplier);
                if (m_ExplosionEffect != null)
                {
                    var effect = Instantiate(m_ExplosionEffect, center, Quaternion.identity);
                    effect.transform.localScale *= 0.85f;
                    Destroy(effect, 5f);
                }

                Sfx.Play(m_ExplosionClip, center, 0.8f, Random.Range(1.08f, 1.16f));
            }

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

            // La corde de siège, avec la batte : plus de flèches pour une grenade restée longtemps en l'air.
            multiplier *= Mathf.Max(1f, m_ArrowBonus);
            count = Mathf.Max(1, Mathf.RoundToInt(count * multiplier));
            var damage = ArrowDamage(upgrades);

            // Frappée par la batte (13 novembre) : les flèches ont la qualité du coup (dégâts, effets, couleur) ; sinon, comme avant,
            // des flèches « moyennes » qui ont les améliorations d'un tir « très bon ».
            var launchGrade = m_Batted ? m_BatGrade : ShotGrade.Ok;
            var upgradeGrade = m_Batted ? m_BatGrade : ShotGrade.Good;
            var trailColor = m_Batted ? TuningOf(FindAnyObjectByType<Bow>()).Get(m_BatGrade).color : m_TrailColor;

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
                    Grade = launchGrade,
                    TrailColor = trailColor,
                    IsShot = true,

                    // Une flèche de grenade qui ne touche rien ne casse pas le combo du joueur.
                    IsExtra = true,
                });

                // Depuis le 12 novembre (le joueur : « la grenade devient très inutile en milieu et en fin de partie »), chaque flèche
                // a les améliorations d'une flèche tirée « très bonne » : effets à l'impact, perçage, déluge, pièces de la flèche
                // (SpecialArrows.PrepareThrown). Avant, des flèches nues, seulement aux dégâts de l'arc.
                if (SpecialArrows.Instance != null)
                    SpecialArrows.Instance.PrepareThrown(arrow, damage, upgradeGrade);
            }
        }

        // Les dégâts d'une flèche vert clair (« très bon ») de l'arc tendu à fond, fois Held Shot Share, comme le tir de
        // l'arc : dégâts de l'arc, puissance à pleine tension, qualité, améliorations et bonus des coffres. Avec les Akimbow,
        // le katana ou la batte à la place de l'arc, l'arc du début de partie. Frappée par la batte : la qualité du coup au lieu
        // du « très bon » (un parfait avec le bonus de l'Adresse, comme un tir parfait de l'arc).
        float ArrowDamage(PlayerUpgrades upgrades)
        {
            var bow = FindAnyObjectByType<Bow>();
            var definition = bow == null ? null : bow.IsReplaced || bow.Definition == null ? bow.StartingDefinition : bow.Definition;
            if (definition == null)
                return m_ArrowDamage * (upgrades != null ? upgrades.DamageMultiplier : 1f);

            var power = definition.drawToPower != null ? definition.drawToPower.Evaluate(1f) : 1f;
            var grade = m_Batted ? bow.GradeDamage(m_BatGrade) : TuningOf(bow).Get(ShotGrade.Held).damage;
            return definition.damage * power * grade * bow.DamageMultiplier * m_HeldShotShare * Mathf.Max(0f, m_DamageBonus);
        }

        static ShotTuning TuningOf(Bow bow) => bow != null && bow.ShotTuning != null ? bow.ShotTuning : ShotTuning.Fallback;

        // ---------------------------------------------------------------- la batte de baseball

        /// <summary>
        /// Frappée par la batte (13 novembre) : elle part à la vitesse du coup, file sur sa cible (s'il y en a une) en la suivant,
        /// sans gravité, et ses flèches auront la qualité du coup (et ce que les améliorations y ajoutent). Des flammes la suivent.
        /// </summary>
        public void Bat(BatShot shot)
        {
            if (!CanBeBatted)
                return;

            // La grenade fantôme quitte sa place devant le joueur et vole comme les autres.
            if (m_Floating)
            {
                m_Floating = false;
                m_Body.isKinematic = false;
                foreach (var collider in m_Colliders)
                {
                    if (collider != null)
                        collider.isTrigger = false;
                }

                m_NeedsIgnore = true;
            }

            m_Batted = true;
            m_BatGrade = shot.Grade;
            m_BatTarget = shot.Target;
            m_BatSpeed = Mathf.Max(1f, shot.Speed);
            m_DamageBonus = shot.DamageBonus > 0f ? shot.DamageBonus : 1f;
            m_ArrowBonus = shot.ArrowBonus > 0f ? shot.ArrowBonus : 1f;
            m_Echoes = Mathf.Max(0, shot.Echoes);
            m_ArmedAt = Time.time;
            m_MegaPending = false;
            m_PlayerArrows.Clear();
            m_Body.useGravity = false;
            m_Body.linearVelocity = (shot.Direction.sqrMagnitude > 1e-6f ? shot.Direction.normalized : transform.forward) * m_BatSpeed;
            m_Body.angularVelocity = Random.onUnitSphere * 25f;
            StartFlames(shot.Flames, TuningOf(FindAnyObjectByType<Bow>()).Get(shot.Grade).color);
        }

        /// <summary>
        /// Une copie de cette grenade, en vol au même endroit, prête à être frappée (le multitir de la batte : chaque copie file sur
        /// un autre ennemi). À faire avant de frapper celle-ci : la copie ne prend pas ses flammes.
        /// </summary>
        public ArrowGrenade CopyForVolley()
        {
            var copy = Instantiate(this, Position, transform.rotation);
            copy.name = name;
            copy.m_Ghost = m_Ghost;
            copy.m_Armed = true;
            copy.m_ArmedAt = Time.time;
            copy.m_LastPosition = copy.m_Body.position;
            copy.m_NeedsIgnore = true;
            copy.m_Grab.enabled = false;
            copy.m_Body.isKinematic = false;
            foreach (var collider in copy.m_Colliders)
            {
                if (collider != null)
                    collider.isTrigger = false;
            }

            if (!s_InFlight.Contains(copy))
                s_InFlight.Add(copy);
            return copy;
        }

        /// <summary>
        /// La grenade fantôme de la batte (la corde fantôme, 13 novembre, le joueur : « une balle qui flotte devant la batte, qu'on
        /// peut refrapper ») : une grenade à l'allure d'hologramme (<paramref name="look"/>), qui ne se prend pas, ne tombe pas, ne
        /// s'éclate sur rien, et attend la batte là où la place <see cref="HoldAt"/>.
        /// </summary>
        public static ArrowGrenade SpawnGhost(ArrowGrenade prefab, Material look, Vector3 position)
        {
            if (prefab == null)
                return null;

            var ghost = Instantiate(prefab, position, Quaternion.identity);
            ghost.name = prefab.name + " fantome";
            ghost.m_Ghost = true;
            ghost.m_Floating = true;
            ghost.m_Armed = true;
            ghost.m_ArmedAt = Time.time;
            ghost.m_Grab.enabled = false;
            ghost.m_Body.isKinematic = true;
            ghost.m_Body.useGravity = false;
            foreach (var collider in ghost.m_Colliders)
            {
                if (collider != null)
                    collider.isTrigger = true;
            }

            if (ghost.m_FuseSparks != null)
                ghost.m_FuseSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (look != null)
            {
                foreach (var renderer in ghost.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                        continue;
                    var materials = new Material[renderer.sharedMaterials.Length];
                    for (var i = 0; i < materials.Length; i++)
                        materials[i] = look;
                    renderer.sharedMaterials = materials;
                }
            }

            s_InFlight.Add(ghost);
            return ghost;
        }

        /// <summary>La grenade fantôme, à sa place devant le joueur.</summary>
        public void HoldAt(Vector3 position, Quaternion rotation)
        {
            if (!m_Floating)
                return;
            transform.SetPositionAndRotation(position, rotation);
            m_Body.position = position;
            m_Body.rotation = rotation;
        }

        /// <summary>Disparaît sans éclater (la grenade fantôme, quand la batte est rangée ou la corde retirée).</summary>
        public void Discard() => Destroy(gameObject);

        // XRI donne la vitesse du bras à la fin de l'image du lâcher : on l'attend (quelques pas physiques au plus). Lancée à peu
        // près vers le haut, la grenade part tout droit vers le haut avec cette vitesse (à 4 m de haut au plus), et sa colonne est
        // fixée devant le joueur, à portée de batte.
        void TryToss()
        {
            var velocity = m_Body.linearVelocity;
            if (velocity.sqrMagnitude < 0.25f && ++m_TossTries < k_TossChecks)
                return;

            // Le lancer, sans la course du joueur.
            m_TossPending = false;
            var thrown = velocity - m_PlayerVelocity;
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null || thrown.y < k_TossMinUpSpeed || Vector3.Angle(thrown, Vector3.up) > k_TossAngle)
                return;

            m_ColumnOffset = rig.HeadYaw * new Vector3(k_TossSide * BaseballBat.HeldSide, 0f, k_TossForward);

            // Plus lourde, elle part plus vite : un même geste, une même hauteur.
            var gravity = -Physics.gravity.y * GravityScale;
            var boost = Mathf.Sqrt(GravityScale / Mathf.Max(0.05f, m_GravityScale));
            var speed = Mathf.Min(thrown.magnitude * boost, Mathf.Sqrt(2f * gravity * k_TossMaxHeight));
            m_Body.linearVelocity = Vector3.up * speed;
            m_Body.angularVelocity *= 0.3f;
            m_TossHeight = m_Body.position.y;
            m_Column = true;
            FollowColumn();
        }

        // La vitesse horizontale du joueur pendant qu'il tient la grenade (lissée : un pas de la course aux bras est saccadé ; un
        // saut de plus de 12 m/s, une téléportation, ne compte pas).
        void TrackPlayer()
        {
            var rig = PlayerRig.Instance;
            var origin = rig != null && rig.Origin != null ? rig.Origin.transform : null;
            if (origin == null || Time.deltaTime <= 0f)
                return;

            var position = origin.position;
            if (m_HasOrigin)
            {
                var velocity = Vector3.ProjectOnPlane((position - m_LastOrigin) / Time.deltaTime, Vector3.up);
                if (velocity.sqrMagnitude <= k_MaxPlayerSpeed * k_MaxPlayerSpeed)
                    m_PlayerVelocity = Vector3.Lerp(m_PlayerVelocity, velocity, 0.5f);
            }

            m_LastOrigin = position;
            m_HasOrigin = true;
        }

        // En courbe vers sa place à côté du joueur : la vitesse horizontale qu'il faut pour y être quand elle redescend à la hauteur du
        // lâcher (le temps qu'il lui reste, d'après sa hauteur et sa vitesse verticale) ; recalculée à chaque pas, elle suit le
        // joueur qui se déplace. Seule sa vitesse horizontale change ; sous la hauteur du lâcher, elle tombe sans plus être guidée.
        void FollowColumn()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            var position = m_Body.position;
            var velocity = m_Body.linearVelocity;
            if (position.y < m_TossHeight && velocity.y < 0f)
            {
                m_Column = false;
                return;
            }

            var gravity = Mathf.Max(0.1f, -Physics.gravity.y * GravityScale);
            var discriminant = velocity.y * velocity.y + 2f * gravity * (position.y - m_TossHeight);
            var remaining = discriminant > 0f ? (velocity.y + Mathf.Sqrt(discriminant)) / gravity : 0f;
            remaining = Mathf.Max(remaining, Time.fixedDeltaTime);

            var target = head.position + m_ColumnOffset;
            var horizontal = Vector3.ClampMagnitude(new Vector3(target.x - position.x, 0f, target.z - position.z) / remaining, k_TossFollowSpeed);
            m_Body.linearVelocity = new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        // Elle tourne vers sa cible (tant qu'elle vit) et garde sa vitesse : la physique ne la ralentit ni ne la fait tomber.
        void SteerBatted()
        {
            var velocity = m_Body.linearVelocity;
            var direction = velocity.sqrMagnitude > 1e-4f ? velocity.normalized : transform.forward;
            if (m_BatTarget != null && m_BatTarget.IsAlive)
            {
                var toTarget = m_BatTarget.Center - m_Body.position;
                if (toTarget.sqrMagnitude > 1e-4f)
                    direction = Vector3.RotateTowards(direction, toTarget.normalized, k_BatTurnRate * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
            }

            m_Body.linearVelocity = direction * m_BatSpeed;
        }

        // Les flammes derrière la grenade frappée (le joueur : « des petites flammes toujours plus présentes à chaque fois que la
        // qualité du tir augmente ») : le matériau des étincelles de la mèche, plus de flammes, plus grosses et plus longues à
        // chaque qualité, et une traînée qui s'allonge avec la vitesse.
        void StartFlames(float strength, Color color)
        {
            var sparks = m_FuseSparks != null ? m_FuseSparks.GetComponent<ParticleSystemRenderer>() : null;
            var material = sparks != null ? sparks.sharedMaterial : null;
            if (material == null || strength <= 0f)
                return;

            var go = new GameObject("Flammes de la batte");
            go.transform.SetParent(transform, false);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.2f + 0.35f * strength);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f + 0.8f * strength);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f + 0.08f * strength, 0.12f + 0.28f * strength);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(new Color(1f, 0.45f, 0.1f), color, 0.35f), new Color(1f, 0.85f, 0.35f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;

            var emission = particles.emission;
            emission.rateOverTime = 30f + 220f * strength;
            emission.rateOverDistance = 2f + 14f * strength;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f + 0.04f * strength;

            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.5f, 0.2f), 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            particles.Play();
            m_Flames = particles;
        }

        // À l'explosion, les flammes déjà parties finissent de brûler là où elles sont.
        void StopFlames()
        {
            if (m_Flames == null)
                return;
            m_Flames.transform.SetParent(null, true);
            m_Flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(m_Flames.gameObject, 1.2f);
            m_Flames = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_InFlight.Clear();

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

    /// <summary>Un coup de batte sur une grenade (13 novembre) : ce que la batte et les améliorations lui donnent.</summary>
    public struct BatShot
    {
        public ShotGrade Grade;

        /// <summary>L'ennemi qu'elle suit (null : tout droit).</summary>
        public Enemy Target;

        public Vector3 Direction;
        public float Speed;

        /// <summary>Les flammes, de 0 (aucune) à 1 (un parfait).</summary>
        public float Flames;

        /// <summary>Multiplie les dégâts de ses flèches (la grenade fantôme : +20 %) ; 0 vaut 1.</summary>
        public float DamageBonus;

        /// <summary>Multiplie le nombre de ses flèches (la corde de siège) ; 0 vaut 1.</summary>
        public float ArrowBonus;

        /// <summary>Ses explosions en plus, au même endroit, un instant après (l'écho).</summary>
        public int Echoes;
    }
}
