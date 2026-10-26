using System;
using System.Collections.Generic;
using Archery.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Bows
{
    /// <summary>
    /// Une flèche. C'est un interactable XRI qu'on n'attrape jamais directement :
    /// le <see cref="Quiver"/> la met dans la main, le <see cref="Bow"/> l'encoche,
    /// puis elle vole grâce à son Rigidbody (gravité gérée par le moteur physique).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class Arrow : XRBaseInteractable, IFarAttachProvider
    {
        public enum State
        {
            Pooled,
            Held,
            Nocked,
            Flying,
            Stuck,
        }

        static readonly List<Arrow> s_HeldArrows = new List<Arrow>();
        static readonly RaycastHit[] s_RaycastHits = new RaycastHit[16];

        // Le laser : tout ce qu'il traverse, puis ce qu'il touche (une entrée par ennemi, sa zone la plus prioritaire).
        static readonly RaycastHit[] s_LaserHits = new RaycastHit[256];
        static readonly List<RaycastHit> s_LaserTargets = new List<RaycastHit>();
        static readonly List<object> s_LaserKeys = new List<object>();
        static readonly List<int> s_LaserPriorities = new List<int>();
        static readonly IComparer<RaycastHit> s_ByDistance = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        // Vitesse donnée aux impacts du laser (les effets et les zones de touche s'en servent comme d'une flèche très rapide).
        const float k_LaserSpeed = 150f;

        GameObject m_FireEffect;
        static Bow s_MeleeBow;
        Vector3 m_PreviousTip;
        bool m_HasPreviousTip;
        bool m_Stabbing;

        /// <summary>Flèches actuellement tenues en main, que l'arc peut encocher.</summary>
        public static IReadOnlyList<Arrow> HeldArrows => s_HeldArrows;

        /// <summary>Déclenché à chaque impact, quel que soit l'objet touché.</summary>
        public static event Action<ArrowHit> AnyHit;

        /// <summary>
        /// Déclenché une fois par flèche tirée, quand elle a fini son vol (plantée ou perdue).
        /// Sert par exemple à casser le combo si elle n'a touché aucun ennemi.
        /// </summary>
        public static event Action<Arrow> ShotEnded;

        /// <summary>
        /// La flèche vient de se planter dans ce collider (un ennemi ou le décor), juste avant <see cref="ShotEnded"/>
        /// (26 octobre : la pointe à mèche, les fûts aimant et conducteur).
        /// </summary>
        public static event Action<Arrow, Collider> Stuck;

        /// <summary>Multiplie les dégâts du coup de flèche au corps à corps (26 octobre : le talisman du duelliste).</summary>
        public static float MeleeDamageMultiplier { get; set; } = 1f;

        [Header("Pièces")]
        [Tooltip("Pointe de la flèche. La racine de l'objet est l'encoche.")]
        [SerializeField]
        Transform m_Tip;

        [SerializeField]
        TrailRenderer m_Trail;

        [Tooltip("Sifflement en boucle pendant le vol.")]
        [SerializeField]
        AudioSource m_FlightAudio;

        [Header("Réglages")]
        [Tooltip("Position de l'encoche par rapport au point de prise de la main.")]
        [SerializeField]
        Vector3 m_HoldOffset = new Vector3(0f, 0f, 0.03f);

        [Tooltip("Profondeur (m) à laquelle la pointe s'enfonce.")]
        [SerializeField]
        float m_Penetration = 0.12f;

        [Tooltip("Durée (s) avant qu'une flèche plantée disparaisse.")]
        [SerializeField]
        float m_StuckLifetime = 10f;

        [SerializeField]
        float m_MaxFlightTime = 8f;

        [SerializeField]
        LayerMask m_HitMask = Physics.DefaultRaycastLayers;

        [Tooltip("Si la flèche traverse le corps puis la tête du même ennemi sur cette distance (m), c'est la tête qui compte.")]
        [SerializeField]
        float m_ZonePreferenceDepth = 0.6f;

        [Header("Coup au corps à corps")]
        [Tooltip("Frapper un ennemi avec la flèche tenue en main (GDD, section 4.5) : elle reste plantée dedans.")]
        [SerializeField]
        bool m_MeleeEnabled = true;

        [Tooltip("Vitesse minimale (m/s) de la pointe pour que le coup porte : il faut frapper franchement.")]
        [SerializeField]
        float m_MeleeMinSpeed = 2f;

        [Tooltip("Le coup fait les dégâts d'un tir de cette qualité (Good : un tir orange).")]
        [SerializeField]
        ShotGrade m_MeleeGrade = ShotGrade.Good;

        [Header("Sons d'impact")]
        [SerializeField]
        AudioClip m_ImpactDefault;

        [Tooltip("Impact sur une cible vivante (ennemi, mannequin).")]
        [SerializeField]
        AudioClip m_ImpactLiving;

        Rigidbody m_Body;
        State m_State = State.Pooled;
        Bow m_Bow;
        IXRSelectInteractor m_Hand;
        bool m_SelectAllowed;
        float m_Length = 0.8f;
        Vector3 m_PreviousHandPosition;
        Vector3 m_HandVelocity;

        float m_Damage;
        ShotGrade m_Grade;
        float m_GravityScale = 1f;
        int m_PierceLeft;
        bool m_IsShot;
        bool m_ShotPending;
        Vector3 m_LaunchPosition;
        float m_FlightTime;
        readonly HashSet<Component> m_Pierced = new HashSet<Component>();

        Transform m_StuckTo;
        Vector3 m_StuckLocalPosition;
        Quaternion m_StuckLocalRotation;
        float m_StuckTime;

        bool m_HandlingHit;
        bool m_DeflectPending;
        Vector3 m_DeflectVelocity;
        bool m_BouncePending;
        bool m_Hovering;

        public State CurrentState => m_State;

        /// <summary>Interacteur (main) qui tient la flèche, ou null.</summary>
        public IXRSelectInteractor Hand => m_Hand;

        public Vector3 NockPosition => transform.position;
        public float Length => m_Length;

        /// <summary>La longueur de l'encoche à la pointe, lisible aussi sur le prefab (avant Awake) : pour la flèche de l'atelier.</summary>
        public float ModelLength => m_Tip != null ? Mathf.Max(0.1f, m_Tip.localPosition.z) : m_Length;
        public ArrowPool Pool { get; set; }

        /// <summary>Flèche ajoutée par le multitir ou le déluge (voir <see cref="ArrowLaunch.IsExtra"/>).</summary>
        public bool IsExtra { get; private set; }

        /// <summary>Nombre d'ennemis que la flèche peut encore traverser.</summary>
        public int PierceLeft => m_PierceLeft;

        /// <summary>La flèche est enflammée (trempée dans le brasero) : sa cible brûlera.</summary>
        public bool IsOnFire { get; private set; }

        /// <summary>Position de la pointe.</summary>
        /// <summary>Gravité de la flèche en vol, en multiple de la normale (voir <see cref="ArrowLaunch.GravityScale"/>).</summary>
        public float GravityScale => m_GravityScale;

        public Vector3 TipPosition => m_Tip != null ? m_Tip.position : transform.position + transform.forward * m_Length;

        public Color TrailColor { get; private set; }

        /// <summary>Dégâts de la flèche tirée ; modifiables en vol (26 octobre : le talisman du sniper, la corde de transe).</summary>
        public float Damage
        {
            get => m_Damage;
            set
            {
                if (m_State == State.Flying)
                    m_Damage = Mathf.Max(0f, value);
            }
        }

        /// <summary>
        /// La flèche traverse le décor (tout ce qui ne réagit pas aux flèches) : elle ne touche que les ennemis, le gong, les
        /// cibles… (26 octobre : l'empennage spectral). Remis à faux quand elle revient au pool.
        /// </summary>
        public bool PassesThroughScenery { get; set; }

        /// <summary>La flèche est arrêtée en l'air (26 octobre : le fût suspendu), sans tomber ni rien toucher.</summary>
        public bool IsHovering => m_Hovering;

        /// <summary>Vitesse en vol. Modifiable pendant le vol, par exemple pour guider la flèche.</summary>
        public Vector3 Velocity
        {
            get => m_Body != null ? m_Body.linearVelocity : Vector3.zero;
            set
            {
                if (m_State == State.Flying)
                    m_Body.linearVelocity = value;
            }
        }

        // La flèche est toujours tenue dans la main, même si le rayon de la main visait au loin.
        public InteractableFarAttachMode farAttachMode
        {
            get => InteractableFarAttachMode.Near;
            set { }
        }

        protected override void Awake()
        {
            base.Awake();
            m_Body = GetComponent<Rigidbody>();
            if (m_Tip != null)
                m_Length = Mathf.Max(0.1f, m_Tip.localPosition.z);
            ResetState();
        }

        protected override void OnDisable()
        {
            s_HeldArrows.Remove(this);
            base.OnDisable();
        }

        // On ne peut ni survoler ni attraper une flèche : seul le carquois la met dans la main.
        public override bool IsHoverableBy(IXRHoverInteractor interactor) => false;

        public override bool IsSelectableBy(IXRSelectInteractor interactor) =>
            base.IsSelectableBy(interactor) && (m_SelectAllowed || m_State == State.Held || m_State == State.Nocked);

        /// <summary>
        /// Place la flèche dans la main et la fait saisir par l'interacteur.
        /// </summary>
        public bool TryPutInHand(IXRSelectInteractor hand)
        {
            if (hand == null || interactionManager == null)
                return false;

            ResetState();
            var attach = hand.GetAttachTransform(this);
            transform.SetPositionAndRotation(attach.TransformPoint(m_HoldOffset), attach.rotation);

            m_SelectAllowed = true;
            interactionManager.SelectEnter(hand, (IXRSelectInteractable)this);
            m_SelectAllowed = false;
            return m_State == State.Held;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            m_Hand = args.interactorObject;
            m_State = State.Held;
            if (!s_HeldArrows.Contains(this))
                s_HeldArrows.Add(this);

            m_PreviousHandPosition = transform.position;
            m_HandVelocity = Vector3.zero;
            m_HasPreviousTip = false;
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            var hand = m_Hand;
            m_Hand = null;
            s_HeldArrows.Remove(this);

            // Plantée dans un ennemi par un coup au corps à corps : elle reste où elle est.
            if (m_Stabbing)
                return;

            // En pause (menu ouvert en cours de partie) : pas de tir, et une flèche lâchée ne reste pas figée en l'air,
            // elle retourne dans le carquois.
            if (SlowMotion.IsPaused && (m_State == State.Held || m_State == State.Nocked))
            {
                if (m_State == State.Nocked && m_Bow != null)
                {
                    m_Bow.CancelNock(this);
                    return;
                }

                m_State = State.Pooled;
                Despawn();
                return;
            }

            if (m_State == State.Nocked && m_Bow != null)
                m_Bow.OnNockedArrowReleased(this, hand, !args.isCanceled);
            else if (m_State == State.Held || m_State == State.Nocked)
                Drop(m_HandVelocity);
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            if (m_State != State.Held || m_Hand == null)
                return;

            var isDynamic = updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic;
            if (!isDynamic && updatePhase != XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender)
                return;

            // La flèche suit la main (dernière pose connue juste avant le rendu).
            var attach = m_Hand.GetAttachTransform(this);
            var position = attach.TransformPoint(m_HoldOffset);
            if (isDynamic && Time.deltaTime > 0f)
            {
                var velocity = (position - m_PreviousHandPosition) / Time.deltaTime;
                m_HandVelocity = Vector3.Lerp(m_HandVelocity, velocity, 0.5f);
                m_PreviousHandPosition = position;
            }

            transform.SetPositionAndRotation(position, attach.rotation);
            if (isDynamic)
                UpdateMelee();
        }

        // Coup au corps à corps : la pointe, poussée assez vite, traverse un ennemi entre deux images.
        void UpdateMelee()
        {
            var tip = TipPosition;
            var previous = m_PreviousTip;
            var hadPrevious = m_HasPreviousTip;
            m_PreviousTip = tip;
            m_HasPreviousTip = true;
            if (!m_MeleeEnabled || !hadPrevious || Time.deltaTime <= 0f)
                return;

            var step = tip - previous;
            var distance = step.magnitude;
            if (distance < 1e-4f || distance / Time.deltaTime < m_MeleeMinSpeed)
                return;

            if (!TryFindHit(previous, step / distance, distance, out var raycastHit))
                return;

            var handler = raycastHit.collider.GetComponentInParent<IArrowHitHandler>();
            if (handler != null)
                Stab(raycastHit, handler, step / distance);
        }

        // Le coup fait les dégâts d'un tir orange ; la flèche quitte la main et reste plantée.
        // Aucune flèche spéciale (multitir…) ne s'y ajoute, mais une flèche enflammée fait brûler l'ennemi.
        void Stab(RaycastHit raycastHit, IArrowHitHandler handler, Vector3 direction)
        {
            if (s_MeleeBow == null)
                s_MeleeBow = FindAnyObjectByType<Bow>();
            var damage = (s_MeleeBow != null ? s_MeleeBow.MeleeDamage(m_MeleeGrade) : 10f) * MeleeDamageMultiplier;

            m_Damage = damage;
            m_Grade = m_MeleeGrade;
            m_IsShot = true;
            m_ShotPending = true;
            m_LaunchPosition = raycastHit.point;

            var hit = new ArrowHit
            {
                Arrow = this,
                Collider = raycastHit.collider,
                Point = raycastHit.point,
                Normal = raycastHit.normal,
                Direction = direction,
                Speed = m_MeleeMinSpeed,
                Damage = damage,
                Grade = m_MeleeGrade,
                TravelDistance = 0f,
                IsShot = true,
            };

            // Pas sur quelque chose de vivant : la flèche reste en main.
            if (!handler.OnArrowHit(hit))
            {
                m_Damage = 0f;
                m_Grade = ShotGrade.None;
                m_IsShot = false;
                m_ShotPending = false;
                return;
            }

            AnyHit?.Invoke(hit);
            Sfx.Play(m_ImpactLiving, raycastHit.point, 0.9f, UnityEngine.Random.Range(0.92f, 1.08f));

            var hand = m_Hand;
            m_Stabbing = true;
            if (hand != null && interactionManager != null)
                interactionManager.SelectExit(hand, (IXRSelectInteractable)this);
            m_Stabbing = false;

            Haptics.Pulse(hand, 0.8f, 0.12f);
            StickInto(raycastHit, transform.forward);
        }

        internal void OnNocked(Bow bow)
        {
            m_Bow = bow;
            m_State = State.Nocked;
        }

        /// <summary>Sans casque (<see cref="Bow.DesktopNock"/>) : la flèche sort du carquois directement sur la corde, sans main.</summary>
        internal void NockWithoutHand(Bow bow)
        {
            ResetState();
            m_HasPreviousTip = false;
            OnNocked(bow);
        }

        internal void OnUnnocked()
        {
            m_Bow = null;
            if (m_State != State.Nocked)
                return;

            m_State = State.Held;

            // Encochée sans main (tir au clavier) : rien ne la tient, elle retourne dans le carquois.
            if (m_Hand == null)
            {
                m_State = State.Pooled;
                Despawn();
            }
        }

        internal void SetNockedPose(Vector3 nockPosition, Quaternion rotation) =>
            transform.SetPositionAndRotation(nockPosition, rotation);

        /// <summary>
        /// Lance la flèche : à partir de là, c'est le moteur physique qui gère sa trajectoire.
        /// </summary>
        public void Launch(in ArrowLaunch launch)
        {
            m_Bow = null;
            m_State = State.Flying;
            m_Damage = launch.Damage;
            m_Grade = launch.Grade;
            m_PierceLeft = Mathf.Max(0, launch.Pierce);
            m_IsShot = launch.IsShot;
            m_ShotPending = launch.IsShot;
            IsExtra = launch.IsExtra;
            m_GravityScale = launch.GravityScale > 0f ? launch.GravityScale : 1f;
            m_LaunchPosition = transform.position;
            m_FlightTime = 0f;
            m_Pierced.Clear();

            var direction = launch.Velocity.sqrMagnitude > 1e-4f ? launch.Velocity.normalized : transform.forward;
            transform.rotation = Quaternion.LookRotation(direction);
            m_Body.position = transform.position;
            m_Body.rotation = transform.rotation;
            m_Body.isKinematic = false;
            m_Body.useGravity = true;
            m_Body.interpolation = RigidbodyInterpolation.Interpolate;
            m_Body.linearVelocity = launch.Velocity;
            m_Body.angularVelocity = Vector3.zero;

            if (m_Trail != null)
            {
                m_Trail.Clear();
                m_Trail.emitting = launch.IsShot;
            }

            SetTrailColor(launch.TrailColor);

            // Seule la flèche tirée par l'arc siffle : avec des dizaines de flèches en plus, ce serait trop bruyant et trop lourd.
            if (m_FlightAudio != null && launch.IsShot && !launch.IsExtra)
            {
                m_FlightAudio.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                m_FlightAudio.Play();
            }
        }

        /// <summary>
        /// Arc cosmique, surcharge : au lieu de voler, la flèche devient un laser. Tout ce qui réagit aux flèches sur sa ligne
        /// est touché d'un coup, du plus proche au plus loin, à travers les murs et le relief ; pour chaque ennemi, la zone
        /// la plus prioritaire qu'il traverse (tête, gemme). Puis la flèche disparaît. Le score, le combo et les succès
        /// comptent ce tir comme une flèche (deux ennemis tués d'un laser : « D'une pierre deux coups »), de cette qualité.
        /// </summary>
        public void FireLaser(Vector3 origin, Vector3 direction, float damage, float range, ShotGrade grade = ShotGrade.Perfect)
        {
            m_Bow = null;
            m_State = State.Flying;
            m_Damage = damage;
            m_Grade = grade;
            m_IsShot = true;
            m_ShotPending = true;
            IsExtra = false;
            m_LaunchPosition = origin;
            m_Pierced.Clear();
            direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : transform.forward;

            var count = Physics.RaycastNonAlloc(origin, direction, s_LaserHits, Mathf.Max(0.1f, range), m_HitMask,
                                                QueryTriggerInteraction.Ignore);
            Array.Sort(s_LaserHits, 0, count, s_ByDistance);

            s_LaserTargets.Clear();
            s_LaserKeys.Clear();
            s_LaserPriorities.Clear();
            for (var i = 0; i < count; i++)
            {
                var hit = s_LaserHits[i];
                if (hit.collider == null || ArrowIgnore.IsIgnored(hit.collider))
                    continue;
                var handler = hit.collider.GetComponentInParent<IArrowHitHandler>();
                if (handler == null || handler is ILaserTransparent)
                    continue;

                var zone = hit.collider.GetComponentInParent<IArrowHitPriority>();
                var key = zone != null && zone.HitGroup != null ? zone.HitGroup : (object)PierceKey(hit);
                var priority = zone != null ? zone.HitPriority : int.MinValue;
                var index = s_LaserKeys.IndexOf(key);
                if (index < 0)
                {
                    s_LaserKeys.Add(key);
                    s_LaserTargets.Add(hit);
                    s_LaserPriorities.Add(priority);
                }
                else if (priority > s_LaserPriorities[index])
                {
                    s_LaserTargets[index] = hit;
                    s_LaserPriorities[index] = priority;
                }
            }

            var impacts = 0;
            foreach (var target in s_LaserTargets)
            {
                if (LaserHit(target, direction) && impacts++ < 4)
                    Sfx.Play(m_ImpactLiving, target.point, 0.7f, UnityEngine.Random.Range(0.85f, 1f));
            }

            s_LaserTargets.Clear();
            EndShot();
            Despawn();
        }

        // Un impact du laser, comme celui d'une flèche (zones de touche, effets d'impact) ; vrai s'il a touché du vivant.
        bool LaserHit(RaycastHit raycastHit, Vector3 direction)
        {
            var hit = new ArrowHit
            {
                Arrow = this,
                Collider = raycastHit.collider,
                Point = raycastHit.point,
                Normal = raycastHit.normal,
                Direction = direction,
                Speed = k_LaserSpeed,
                Damage = m_Damage,
                Grade = m_Grade,
                TravelDistance = Vector3.Distance(m_LaunchPosition, raycastHit.point),
                IsShot = true,
            };

            var handler = raycastHit.collider.GetComponentInParent<IArrowHitHandler>();
            var living = handler != null && handler.OnArrowHit(hit);
            AnyHit?.Invoke(hit);
            return living;
        }

        /// <summary>Lâche la flèche sans la tirer (elle tombe, ou part dans la direction du geste).</summary>
        public void Drop(Vector3 velocity) =>
            Launch(new ArrowLaunch { Velocity = velocity, IsShot = false, TrailColor = Color.clear });

        /// <summary>Change la couleur de la traînée en vol (flèches spéciales).</summary>
        public void SetTrailColor(Color color)
        {
            TrailColor = color;
            if (m_Trail == null)
                return;

            m_Trail.startColor = color;
            color.a = 0f;
            m_Trail.endColor = color;
        }

        /// <summary>Enflamme la flèche : l'effet (petites flammes) s'attache à sa pointe. Elle s'éteint en retournant dans le pool.</summary>
        public void Ignite(GameObject fireEffect)
        {
            if (IsOnFire)
                return;

            IsOnFire = true;
            if (fireEffect != null)
                m_FireEffect = Instantiate(fireEffect, m_Tip != null ? m_Tip : transform, false);
        }

        void Extinguish()
        {
            IsOnFire = false;
            if (m_FireEffect != null)
                Destroy(m_FireEffect);
            m_FireEffect = null;
        }

        /// <summary>Permet à la flèche en vol de traverser des ennemis en plus (perçage).</summary>
        public void AddPierce(int count) => m_PierceLeft += Mathf.Max(0, count);

        /// <summary>La flèche peut de nouveau toucher les ennemis qu'elle a déjà traversés (le retour de l'empennage boomerang).</summary>
        public void ResetPierced() => m_Pierced.Clear();

        /// <summary>
        /// À appeler pendant <see cref="AnyHit"/> : au lieu de se planter, la flèche repart du point d'impact
        /// avec cette vitesse (ricochet). Elle ne peut plus toucher ce qu'elle vient de toucher.
        /// </summary>
        public void Deflect(Vector3 velocity)
        {
            if (!m_HandlingHit || velocity.sqrMagnitude < 1e-4f)
                return;

            m_DeflectPending = true;
            m_DeflectVelocity = velocity;
        }

        /// <summary>
        /// À appeler pendant <see cref="AnyHit"/>, sur le décor : au lieu de se planter, la flèche rebondit avec cette vitesse
        /// (26 octobre : l'empennage rebond). Contrairement au ricochet, elle pourra retoucher la même surface.
        /// </summary>
        public void Bounce(Vector3 velocity)
        {
            if (!m_HandlingHit || velocity.sqrMagnitude < 1e-4f)
                return;

            m_BouncePending = true;
            m_DeflectVelocity = velocity;
        }

        /// <summary>
        /// Arrête la flèche en vol là où elle est (26 octobre : le fût suspendu), ou la laisse repartir ; c'est celui qui la
        /// relâche qui lui redonne sa vitesse (<see cref="Velocity"/>).
        /// </summary>
        public void Hover(bool hover)
        {
            if (m_State != State.Flying || m_Hovering == hover)
                return;

            m_Hovering = hover;
            m_Body.useGravity = !hover;
            m_Body.linearVelocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
            if (m_Trail != null)
                m_Trail.emitting = !hover && m_IsShot;
        }

        /// <summary>Déplace la flèche arrêtée en l'air (l'essaim du fût suspendu tourne autour du joueur).</summary>
        public void HoverAt(Vector3 position, Quaternion rotation)
        {
            if (!m_Hovering)
                return;

            transform.SetPositionAndRotation(position, rotation);
            m_Body.position = position;
            m_Body.rotation = rotation;
        }

        /// <summary>Renvoie la flèche dans le pool. Sans effet si elle est en main.</summary>
        public void Despawn()
        {
            if (m_State == State.Held || m_State == State.Nocked)
                return;

            EndShot();
            ResetState();
            if (Pool != null)
                Pool.Release(this);
            else
                Destroy(gameObject);
        }

        void FixedUpdate()
        {
            if (m_State != State.Flying)
                return;

            var deltaTime = Time.fixedDeltaTime;
            m_FlightTime += deltaTime;

            // Arrêtée en l'air (fût suspendu) : elle attend, sans bouger ni rien toucher.
            if (m_Hovering)
            {
                m_Body.linearVelocity = Vector3.zero;
                if (m_FlightTime > m_MaxFlightTime)
                    Despawn();
                return;
            }

            // Une flèche plus lourde (arc légendaire) : la gravité du Rigidbody, plus ce qui manque.
            if (!Mathf.Approximately(m_GravityScale, 1f))
                m_Body.AddForce(Physics.gravity * (m_GravityScale - 1f), ForceMode.Acceleration);

            var velocity = m_Body.linearVelocity;
            var speed = velocity.magnitude;
            if (speed > 0.05f)
            {
                var direction = velocity / speed;
                m_Body.MoveRotation(Quaternion.LookRotation(direction));

                // Rayon depuis la pointe jusqu'à sa position au prochain pas physique :
                // on détecte l'impact avant que la flèche ne traverse la cible.
                var tip = m_Body.position + direction * m_Length;
                if (TryFindHit(tip, direction, speed * deltaTime + 0.02f, out var hit))
                {
                    HandleHit(hit, velocity);
                    if (m_State != State.Flying)
                        return;
                }
            }

            if (m_FlightTime > m_MaxFlightTime || m_Body.position.y < -100f)
                Despawn();
        }

        void LateUpdate()
        {
            if (m_State != State.Stuck)
                return;

            m_StuckTime += Time.deltaTime;
            if (m_StuckTo == null || !m_StuckTo.gameObject.activeInHierarchy || m_StuckTime > m_StuckLifetime)
            {
                Despawn();
                return;
            }

            // Suit l'objet touché sans en devenir l'enfant (évite les déformations dues à l'échelle).
            transform.SetPositionAndRotation(m_StuckTo.TransformPoint(m_StuckLocalPosition), m_StuckTo.rotation * m_StuckLocalRotation);
        }

        bool TryFindHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit closest)
        {
            closest = default;
            var found = false;
            var bestDistance = float.MaxValue;
            var count = Physics.RaycastNonAlloc(origin, direction, s_RaycastHits, distance, m_HitMask, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = s_RaycastHits[i];
                if (hit.collider == null || ArrowIgnore.IsIgnored(hit.collider) || m_Pierced.Contains(PierceKey(hit)))
                    continue;
                if (PassesThroughScenery && hit.collider.GetComponentInParent<IArrowHitHandler>() == null)
                    continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    closest = hit;
                    found = true;
                }
            }

            if (found)
                closest = PreferPriorityZone(closest, count);
            return found;
        }

        // Le collider du corps englobe souvent un peu la tête : si le trajet traverse ensuite
        // une zone prioritaire du même ennemi, c'est elle qui est touchée.
        RaycastHit PreferPriorityZone(RaycastHit closest, int count)
        {
            var closestZone = closest.collider.GetComponentInParent<IArrowHitPriority>();
            if (closestZone == null)
                return closest;

            var best = closest;
            var bestPriority = closestZone.HitPriority;
            for (var i = 0; i < count; i++)
            {
                var hit = s_RaycastHits[i];
                if (hit.collider == null || hit.collider == closest.collider || hit.distance > closest.distance + m_ZonePreferenceDepth)
                    continue;
                if (ArrowIgnore.IsIgnored(hit.collider) || m_Pierced.Contains(PierceKey(hit)))
                    continue;

                var zone = hit.collider.GetComponentInParent<IArrowHitPriority>();
                if (zone != null && zone.HitGroup == closestZone.HitGroup && zone.HitPriority > bestPriority)
                {
                    best = hit;
                    bestPriority = zone.HitPriority;
                }
            }

            return best;
        }

        void HandleHit(RaycastHit raycastHit, Vector3 velocity)
        {
            var speed = velocity.magnitude;
            var direction = speed > 0f ? velocity / speed : transform.forward;
            var hit = new ArrowHit
            {
                Arrow = this,
                Collider = raycastHit.collider,
                Point = raycastHit.point,
                Normal = raycastHit.normal,
                Direction = direction,
                Speed = speed,
                Damage = m_IsShot ? m_Damage : 0f,
                Grade = m_Grade,
                TravelDistance = Vector3.Distance(m_LaunchPosition, raycastHit.point),
                IsShot = m_IsShot,
            };

            var handler = raycastHit.collider.GetComponentInParent<IArrowHitHandler>();
            var hitLivingThing = handler != null && handler.OnArrowHit(hit);

            // Pendant AnyHit, un script peut demander un ricochet (Deflect).
            m_HandlingHit = true;
            try
            {
                AnyHit?.Invoke(hit);
            }
            finally
            {
                m_HandlingHit = false;
            }

            var impactClip = hitLivingThing ? m_ImpactLiving : m_ImpactDefault;
            Sfx.Play(impactClip, raycastHit.point, Mathf.Clamp01(0.35f + speed / 50f), UnityEngine.Random.Range(0.92f, 1.08f));

            if (m_DeflectPending)
            {
                m_DeflectPending = false;
                m_BouncePending = false;
                Ricochet(raycastHit);
                return;
            }

            if (m_BouncePending)
            {
                m_BouncePending = false;
                BounceOff(raycastHit);
                return;
            }

            if (hitLivingThing && m_PierceLeft > 0)
            {
                m_PierceLeft--;
                m_Pierced.Add(PierceKey(raycastHit));
                m_Damage *= 0.8f;
                return;
            }

            if (handler is IArrowAbsorber)
            {
                Despawn();
                return;
            }

            StickInto(raycastHit, direction);
        }

        // Un ennemi a plusieurs colliders (tête, corps) mais un seul Rigidbody :
        // on évite ainsi qu'une flèche perçante le touche deux fois.
        static Component PierceKey(RaycastHit hit) => hit.rigidbody != null ? hit.rigidbody : hit.collider;

        // La pointe repart du point d'impact, dans la nouvelle direction, un peu moins forte (comme le perçage).
        void Ricochet(RaycastHit hit)
        {
            m_Pierced.Add(PierceKey(hit));
            m_Damage *= 0.8f;

            var direction = m_DeflectVelocity.normalized;
            var rotation = Quaternion.LookRotation(direction);
            var position = hit.point - direction * m_Length;
            transform.SetPositionAndRotation(position, rotation);
            m_Body.position = position;
            m_Body.rotation = rotation;
            m_Body.linearVelocity = m_DeflectVelocity;
        }

        // Le rebond sur le décor : la pointe repart juste au-dessus de la surface, sans la marquer comme traversée (elle doit
        // pouvoir retoucher le sol plus loin).
        void BounceOff(RaycastHit hit)
        {
            var direction = m_DeflectVelocity.normalized;
            var rotation = Quaternion.LookRotation(direction);
            var position = hit.point + hit.normal * 0.04f - direction * m_Length;
            transform.SetPositionAndRotation(position, rotation);
            m_Body.position = position;
            m_Body.rotation = rotation;
            m_Body.linearVelocity = m_DeflectVelocity;
        }

        void StickInto(RaycastHit hit, Vector3 direction)
        {
            m_State = State.Stuck;
            m_Body.linearVelocity = Vector3.zero;
            m_Body.angularVelocity = Vector3.zero;
            m_Body.isKinematic = true;
            m_Body.useGravity = false;
            m_Body.interpolation = RigidbodyInterpolation.None;

            var rotation = Quaternion.LookRotation(direction);
            var position = hit.point - direction * (m_Length - m_Penetration);
            transform.SetPositionAndRotation(position, rotation);

            m_StuckTo = hit.collider.transform;
            m_StuckLocalPosition = m_StuckTo.InverseTransformPoint(position);
            m_StuckLocalRotation = Quaternion.Inverse(m_StuckTo.rotation) * rotation;
            m_StuckTime = 0f;

            if (m_Trail != null)
                m_Trail.emitting = false;
            if (m_FlightAudio != null)
                m_FlightAudio.Stop();
            if (Pool != null)
                Pool.NotifyStuck(this);

            if (m_IsShot)
                Stuck?.Invoke(this, hit.collider);
            EndShot();
        }

        void EndShot()
        {
            if (!m_ShotPending)
                return;

            m_ShotPending = false;
            ShotEnded?.Invoke(this);
        }

        void ResetState()
        {
            m_State = State.Pooled;
            m_Bow = null;
            m_StuckTo = null;
            m_Pierced.Clear();
            m_Damage = 0f;
            m_Grade = ShotGrade.None;
            m_IsShot = false;
            m_ShotPending = false;
            m_DeflectPending = false;
            m_BouncePending = false;
            m_Hovering = false;
            PassesThroughScenery = false;
            IsExtra = false;
            m_GravityScale = 1f;
            Extinguish();

            if (m_Body != null)
            {
                if (!m_Body.isKinematic)
                {
                    m_Body.linearVelocity = Vector3.zero;
                    m_Body.angularVelocity = Vector3.zero;
                }

                m_Body.isKinematic = true;
                m_Body.useGravity = false;
                m_Body.interpolation = RigidbodyInterpolation.None;
            }

            if (m_Trail != null)
            {
                m_Trail.emitting = false;
                m_Trail.Clear();
            }

            if (m_FlightAudio != null)
                m_FlightAudio.Stop();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_HeldArrows.Clear();
            s_MeleeBow = null;
            AnyHit = null;
            ShotEnded = null;
            Stuck = null;
            MeleeDamageMultiplier = 1f;
        }
    }
}
