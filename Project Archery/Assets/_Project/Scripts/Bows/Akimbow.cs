using System;
using System.Collections.Generic;
using Archery.Core;
using Archery.Difficulty;
using Archery.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Random = UnityEngine.Random;

namespace Archery.Bows
{
    /// <summary>
    /// Les Akimbow : deux arbalètes, à la place de l'arc (une classe d'arme, débloquée par l'épreuve du toit cyberpunk ;
    /// aussi les arbalètes de l'épreuve elle-même). Elles flottent à côté du joueur, une de chaque côté (comme l'arc rangé) ;
    /// on en prend une dans chaque main avec la poignée de la manette, et on les lâche pour qu'elles retournent à leur place.
    /// La gâchette de la main qui en tient une tire une flèche tout droit. Chaque arbalète a son anneau de timing, qui repart
    /// tout seul (et vite) après chaque tir : tirer pendant le rouge, l'orange, le vert ou le vert clair donne la même qualité
    /// de tir qu'avec un arc. Chaque arbalète tenue a aussi son aide à la visée (selon la difficulté). Style cyberpunk stable
    /// (métal en plaques, néons, touches de couleur ; une arme différente de chaque côté), avec de petits bugs de temps en
    /// temps (des morceaux sombres, l'image brouillée, un sursaut), comme la porte glitchée de l'antre.
    /// </summary>
    /// <remarks>
    /// Créées par l'<see cref="Bow"/> quand sa définition est <see cref="BowDefinition.akimbo"/> : l'arc est alors caché, et
    /// ses multiplicateurs (améliorations, coffres), ses qualités de tir et son événement de tir servent aux arbalètes
    /// (les améliorations de flèches marchent donc aussi). Rangées avec lui (construction, dimensions). La gâchette ne tire
    /// pas quand la main vise un menu. Sans casque : devant la caméra, le clic gauche tire une main puis l'autre, vers le
    /// centre de l'écran. Pour l'épreuve du toit : <see cref="CreateTrial"/> (dégâts fixes, flèches « en plus », sans points
    /// ni événement de tir de l'arc).
    /// </remarks>
    public class Akimbow : MonoBehaviour
    {
        // Après un tir, le résultat reste un instant dans l'anneau, qui repart ensuite du bord.
        const float k_RingRestart = 0.15f;

        // Diamètre (m) de l'anneau, et sa place à côté de l'arbalète (côté extérieur, au-dessus), dans le repère du modèle.
        const float k_RingSize = 0.07f;
        static readonly Vector3 k_RingOffset = new Vector3(0.2f, 0.16f, 0.05f);

        // En main : la poignée de l'arbalète tombe dans la paume (au point de prise de la manette), le canon dans l'axe de
        // visée ; la main gantée se referme sur la poignée (HandGrip), et la montre de ce poignet monte au-dessus du fût.
        // Centre de la poignée, dans le repère du modèle d'origine (sous le fût, devant la crosse). Avant le 20 octobre au
        // soir : l'arbalète était posée sur la manette, la main dans le fût et la montre cachée.
        static readonly Vector3 k_GripCenter = new Vector3(0f, -0.015f, -0.022f);
        const float k_WatchLift = 0.06f;

        // Sans casque : à droite et à gauche, sous le viseur, devant la caméra.
        static readonly Vector3 k_DesktopOffset = new Vector3(0.2f, -0.16f, 0.42f);

        // Rangées : de chaque côté du joueur, à hauteur de hanche, un peu devant (repère horizontal de la tête, x vers le côté),
        // le canon vers l'avant et un peu vers le bas. Elles y reviennent à cette vitesse quand on les lâche.
        static readonly Vector3 k_HolsterOffset = new Vector3(0.3f, -0.5f, 0.32f);
        static readonly Vector3 k_HolsterEuler = new Vector3(15f, 8f, 0f);
        const float k_ReturnSpeed = 8f;

        // La boîte de prise, dans le repère du modèle d'origine (55 cm, poignée à l'origine, canon vers +Z).
        static readonly Vector3 k_BoxCenter = new Vector3(0.008f, 0.058f, 0.01f);
        static readonly Vector3 k_BoxSize = new Vector3(0.33f, 0.2f, 0.55f);

        // Les néons, dans le repère du modèle d'origine : centre, taille, couleur (0 : le premier néon de la définition,
        // 1 : le deuxième). Le rail du dessus, les branches, l'avant, les flancs.
        static readonly (Vector3 Center, Vector3 Size, int Color)[] k_Neons =
        {
            (new Vector3(0f, 0.1335f, 0.065f), new Vector3(0.008f, 0.005f, 0.29f), 0),
            (new Vector3(0.005f, 0.108f, 0.165f), new Vector3(0.32f, 0.004f, 0.006f), 0),
            (new Vector3(0.01f, 0.108f, 0.262f), new Vector3(0.22f, 0.004f, 0.006f), 1),
            (new Vector3(-0.037f, 0.09f, 0.06f), new Vector3(0.004f, 0.006f, 0.12f), 1),
            (new Vector3(0.037f, 0.09f, 0.06f), new Vector3(0.004f, 0.006f, 0.12f), 1),
        };

        // Les bugs : de temps en temps (toutes les 2,5 à 6 s), pendant 0,12 à 0,3 s, quelques morceaux sombres apparaissent
        // sur l'arbalète, le modèle se brouille (_Glitch du shader Archery/Akimbow, à 0 le reste du temps) et sursaute à peine.
        static readonly int k_GlitchId = Shader.PropertyToID("_Glitch");
        static readonly int k_ColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color[] k_FragmentColors =
        {
            new Color(0.02f, 0.16f, 0.2f, 0.92f),
            new Color(0.2f, 0.02f, 0.03f, 0.92f),
            new Color(0.01f, 0.01f, 0.015f, 0.95f),
            new Color(0.12f, 0.02f, 0.11f, 0.9f),
        };

        const int k_FragmentsPerCrossbow = 4;
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];
        static readonly List<Akimbow> s_Instances = new List<Akimbow>();

        // Une arbalète : le nœud qu'on prend (corps rigide, boîte, XRGrabInteractable), et dessous l'arbalète à l'échelle.
        sealed class Slot
        {
            public float Side;
            public Transform Root;
            public XRGrabInteractable Grab;
            public Collider[] Colliders = Array.Empty<Collider>();
            public Transform Crossbow;
            public Renderer[] Body = Array.Empty<Renderer>();
            public TimingRing Ring;
            public AimGuide Guide;
            public IXRSelectInteractor Holder;
            public float Restart = -1f;
            public float Kick;
            public Vector3 Jump;
            public float NextGlitch;
            public float GlitchEnd = -1f;
            public float GlitchFrame;
            public readonly List<Renderer> Fragments = new List<Renderer>();
            public Pose LastHolster;
            public bool Following;
            public bool Holstered = true;
            public bool NeedsIgnore = true;
        }

        readonly List<Slot> m_Slots = new List<Slot>();
        readonly List<Collider> m_AllColliders = new List<Collider>();
        Bow m_Bow;
        BowDefinition m_Definition;
        CharacterController m_PlayerBody;
        bool m_Trial;
        float m_TrialDamage;
        bool m_Desktop;
        bool m_Hidden;
        int m_NextDesktopHand;
        MaterialPropertyBlock m_Block;
        Material m_FragmentMaterial;
        Mesh m_FragmentMesh;

        /// <summary>Des Akimbow sont l'arme du joueur (pas celles de l'épreuve du toit) : le carquois ne donne plus de flèches.</summary>
        public static bool Equipped
        {
            get
            {
                foreach (var akimbow in s_Instances)
                {
                    if (akimbow != null && !akimbow.m_Trial)
                        return true;
                }

                return false;
            }
        }

        /// <summary>Une arbalète vient de tirer (départ et direction de la flèche).</summary>
        public event Action<Vector3, Vector3> Fired;

        /// <summary>Les Akimbow de l'arc (sa définition, ses multiplicateurs, ses qualités de tir).</summary>
        public static Akimbow Create(Bow bow, BowDefinition definition)
        {
            var akimbow = new GameObject("Akimbow").AddComponent<Akimbow>();
            akimbow.m_Bow = bow;
            akimbow.m_Definition = definition;
            return akimbow;
        }

        /// <summary>
        /// Les arbalètes de l'épreuve du toit : chaque flèche fait <paramref name="damage"/> (un tireur tombe en une flèche),
        /// ne rapporte pas de points et ne compte pas comme un tir de l'arc.
        /// </summary>
        public static Akimbow CreateTrial(Bow bow, BowDefinition definition, float damage)
        {
            var akimbow = Create(bow, definition);
            akimbow.name = "Akimbow (épreuve du toit)";
            akimbow.m_Trial = true;
            akimbow.m_TrialDamage = damage;
            return akimbow;
        }

        void Awake() => s_Instances.Add(this);

        void OnEnable() => Application.onBeforeRender += OnBeforeRender;

        void OnDisable() => Application.onBeforeRender -= OnBeforeRender;

        void OnDestroy()
        {
            s_Instances.Remove(this);
            ArrowIgnore.Unregister(m_AllColliders);
            foreach (var slot in m_Slots)
            {
                // Sans casque, l'arbalète est sous la caméra : elle part avec les Akimbow quand même.
                if (slot.Root != null)
                    Destroy(slot.Root.gameObject);
            }

            if (m_FragmentMaterial != null)
                Destroy(m_FragmentMaterial);
            if (m_FragmentMesh != null)
                Destroy(m_FragmentMesh);
        }

        // ---------------------------------------------------------------- construction

        bool TryBuild()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null || m_Definition == null || m_Definition.akimboModel == null)
                return false;

            m_PlayerBody = rig.Origin != null ? rig.Origin.GetComponentInChildren<CharacterController>(true) : null;
            foreach (var side in new[] { -1f, 1f })
            {
                var slot = CreateSlot(side);
                m_AllColliders.AddRange(slot.Colliders);
                m_Slots.Add(slot);
                SnapToHolster(slot);
            }

            ArrowIgnore.Register(m_AllColliders);
            return true;
        }

        Slot CreateSlot(float side)
        {
            var slot = new Slot { Side = side, NextGlitch = Time.time + Random.Range(1f, 4f) };
            var root = new GameObject(side < 0f ? "Akimbow gauche" : "Akimbow droite").transform;
            root.SetParent(transform, false);
            slot.Root = root;

            var body = root.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;

            var scale = Mathf.Max(0.05f, m_Definition.akimboScale);
            slot.Crossbow = CreateCrossbow(m_Definition, root, side > 0f, out slot.Body);
            slot.Ring = CreateRing(slot.Crossbow, side);
            BuildFragments(slot);

            // La boîte se met avant l'interactable : il prend les colliders présents à sa création.
            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = k_BoxCenter * scale;
            box.size = k_BoxSize * scale;
            slot.Colliders = new Collider[] { box };

            // La prise : la poignée au point de prise de la manette ; la main gantée s'y referme.
            var grip = new GameObject("Poignée").transform;
            grip.SetParent(root, false);
            grip.localPosition = k_GripCenter * scale;
            var handGrip = root.gameObject.AddComponent<HandGrip>();
            handGrip.Point = grip;
            handGrip.WatchLift = k_WatchLift;

            var grab = root.gameObject.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.throwOnDetach = false;
            grab.useDynamicAttach = false;
            grab.farAttachMode = InteractableFarAttachMode.Near;
            grab.attachTransform = grip;
            grab.selectEntered.AddListener(args => OnGrabbed(slot, args.interactorObject));
            grab.selectExited.AddListener(_ => OnReleased(slot));
            grab.activated.AddListener(args => OnTrigger(slot, args.interactorObject));
            slot.Grab = grab;

            slot.Guide = new GameObject("Aide à la visée " + (side < 0f ? "gauche" : "droite")).AddComponent<AimGuide>();
            slot.Guide.transform.SetParent(transform, false);
            return slot;
        }

        /// <summary>
        /// Une arbalète de la définition : le modèle à l'échelle, son matériau, ses néons (ceux de la main droite si
        /// <paramref name="right"/> : une autre arme) ; sans colliders ni ombres. Sert aussi à l'aperçu du menu des armes.
        /// <paramref name="body"/> : les rendus du modèle (pas des néons).
        /// </summary>
        public static Transform CreateCrossbow(BowDefinition definition, Transform parent, bool right, out Renderer[] body)
        {
            var bodyMaterial = right && definition.akimboMaterialRight != null ? definition.akimboMaterialRight : definition.akimboMaterial;
            var neons = right && definition.akimboNeonRight != null && definition.akimboNeonRight.Length > 0
                ? definition.akimboNeonRight
                : definition.akimboNeon;

            var root = new GameObject("Arbalète").transform;
            root.SetParent(parent, false);
            root.localScale = Vector3.one * Mathf.Max(0.05f, definition.akimboScale);

            var model = Instantiate(definition.akimboModel, root, false);
            model.name = "Modèle";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            body = model.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in body)
            {
                if (bodyMaterial != null)
                {
                    var materials = renderer.sharedMaterials;
                    for (var i = 0; i < materials.Length; i++)
                        materials[i] = bodyMaterial;
                    renderer.sharedMaterials = materials;
                }

                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            if (neons != null && neons.Length > 0)
            {
                foreach (var (center, size, color) in k_Neons)
                {
                    var neon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    neon.name = "Néon";
                    neon.transform.SetParent(root, false);
                    neon.transform.localPosition = center;
                    neon.transform.localScale = size;
                    var renderer = neon.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = neons[Mathf.Min(color, neons.Length - 1)];
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }

            // Tout de suite (pas en fin d'image) : l'interactable posé ensuite ne doit pas les compter comme les siens.
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(collider);
            return root;
        }

        // L'anneau de l'arc, en plus petit, à côté de l'arbalète (côté extérieur).
        TimingRing CreateRing(Transform crossbow, float side)
        {
            var template = m_Bow != null ? m_Bow.TimingRingTemplate : null;
            if (template == null)
                return null;

            var ring = Instantiate(template, crossbow, false);
            ring.name = "Anneau";
            var scale = Mathf.Max(0.05f, m_Definition.akimboScale);
            ring.transform.localPosition = Vector3.Scale(k_RingOffset, new Vector3(side, 1f, 1f));
            ring.transform.localScale = Vector3.one * (k_RingSize / scale);
            ring.GoldEntered += () => Haptics.Pulse(HolderOf(ring), 0.25f, 0.03f);
            return ring;
        }

        IXRSelectInteractor HolderOf(TimingRing ring)
        {
            foreach (var slot in m_Slots)
            {
                if (slot.Ring == ring)
                    return m_Desktop ? null : slot.Holder;
            }

            return null;
        }

        // Des carrés sombres (couleurs des sommets blanches : la couleur vient du bloc de propriétés), cachés hors des bugs.
        void BuildFragments(Slot slot)
        {
            if (m_FragmentMaterial == null)
            {
                var shader = Shader.Find("Archery/UnlitVertexColor");
                if (shader == null)
                    return;
                m_FragmentMaterial = new Material(shader) { name = "Akimbow (morceaux glitchés)" };
            }

            if (m_FragmentMesh == null)
            {
                m_FragmentMesh = new Mesh
                {
                    name = "Morceau glitché",
                    vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) },
                    colors = new[] { Color.white, Color.white, Color.white, Color.white },
                    triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 },
                };
                m_FragmentMesh.RecalculateBounds();
            }

            for (var i = 0; i < k_FragmentsPerCrossbow; i++)
            {
                var fragment = new GameObject("Morceau glitché");
                fragment.transform.SetParent(slot.Crossbow, false);
                fragment.AddComponent<MeshFilter>().sharedMesh = m_FragmentMesh;
                var renderer = fragment.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = m_FragmentMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                fragment.SetActive(false);
                slot.Fragments.Add(renderer);
            }
        }

        // ---------------------------------------------------------------- prise en main

        void OnGrabbed(Slot slot, IXRSelectInteractor interactor)
        {
            slot.Holder = interactor;
            slot.Holstered = false;
            slot.Following = false;
            BeginRing(slot);
            Haptics.Pulse(interactor, 0.3f, 0.05f);
        }

        // Lâchée : l'anneau s'arrête et l'arbalète retourne à sa place, à côté du joueur.
        void OnReleased(Slot slot)
        {
            if (slot.Grab != null && slot.Grab.isSelected)
                return;
            slot.Holder = null;
            slot.NeedsIgnore = true;
            slot.Restart = -1f;
            if (slot.Ring != null)
                slot.Ring.Cancel();
            if (slot.Guide != null)
                slot.Guide.Hide();
        }

        void OnTrigger(Slot slot, IXRInteractor interactor)
        {
            if (m_Desktop || m_Hidden || OverUI(interactor))
                return;
            TryFire(slot);
        }

        // La main vise un menu : la gâchette sert à cliquer, pas à tirer.
        static bool OverUI(IXRInteractor interactor)
        {
            if (interactor is NearFarInteractor nearFar && nearFar.TryGetCurrentUIRaycastResult(out _))
                return true;
            return interactor is XRRayInteractor ray && ray.TryGetCurrentUIRaycastResult(out _);
        }

        void Release(Slot slot)
        {
            if (slot.Grab != null && slot.Grab.isSelected && slot.Grab.interactionManager != null)
                slot.Grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)slot.Grab);
            OnReleased(slot);
        }

        bool Held(Slot slot) => slot.Grab != null && slot.Grab.isSelected;

        // Rangée à côté du joueur, l'arbalète ne doit ni le pousser ni le bloquer. Le joueur rend ses collisions à ce qu'il
        // lâche, et Unity les oublie quand un collider se désactive : on les recoupe à l'image d'après.
        void IgnoreBody(Slot slot)
        {
            slot.NeedsIgnore = false;
            if (m_PlayerBody == null || !m_PlayerBody.enabled || !m_PlayerBody.gameObject.activeInHierarchy)
                return;
            foreach (var collider in slot.Colliders)
            {
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy)
                    Physics.IgnoreCollision(m_PlayerBody, collider, true);
            }
        }

        // ---------------------------------------------------------------- chaque image

        void Update()
        {
            if (m_Slots.Count == 0 && !TryBuild())
                return;

            var desktop = DesktopControls.Instance != null && DesktopControls.Instance.IsActive;
            if (desktop != m_Desktop)
                SetDesktop(desktop);

            // Rangées avec l'arc (construction au marteau, dimensions).
            var hidden = m_Bow != null && m_Bow.IsStowed && !m_Trial;
            if (hidden != m_Hidden)
                SetHidden(hidden);
            if (m_Hidden)
                return;

            var deltaTime = Time.deltaTime;
            var now = Time.time;
            foreach (var slot in m_Slots)
            {
                if (slot.Crossbow == null)
                    continue;

                var ready = m_Desktop || Held(slot);
                slot.Kick = Mathf.MoveTowards(slot.Kick, 0f, deltaTime * 0.2f);
                if (ready && slot.Restart >= 0f && (slot.Restart -= deltaTime) < 0f)
                    BeginRing(slot);

                UpdateGlitch(slot, now);
                slot.Crossbow.localPosition = Vector3.back * slot.Kick + slot.Jump;
                if (!ready)
                {
                    if (slot.NeedsIgnore)
                        IgnoreBody(slot);
                    UpdateHolster(slot, deltaTime);
                }
                UpdateGuide(slot, ready && (m_Desktop || !OverUI(slot.Holder)));
            }

            if (m_Desktop)
                UpdateDesktop();
        }

        // Juste avant l'image, avec la tête à sa place la plus récente : les arbalètes rangées ne traînent pas d'une image.
        void OnBeforeRender()
        {
            if (m_Desktop || m_Hidden)
                return;
            foreach (var slot in m_Slots)
            {
                if (slot.Root != null && !Held(slot))
                    UpdateHolster(slot, 0f);
            }
        }

        bool TryHolsterPose(float side, out Pose pose)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
            {
                pose = default;
                return false;
            }

            var yaw = rig.HeadYaw;
            var offset = k_HolsterOffset;
            offset.x *= side;
            pose = new Pose(head.position + yaw * offset, yaw * Quaternion.Euler(k_HolsterEuler.x, k_HolsterEuler.y * side, 0f));
            return true;
        }

        void SnapToHolster(Slot slot)
        {
            if (!TryHolsterPose(slot.Side, out var target) || slot.Root == null)
                return;
            slot.Root.SetPositionAndRotation(target.position, target.rotation);
            slot.LastHolster = target;
            slot.Following = true;
            slot.Holstered = true;
        }

        // Comme l'arc : l'arbalète revient dans le repère du joueur (elle suit ses déplacements sans traîner), puis reste à
        // sa place.
        void UpdateHolster(Slot slot, float deltaTime)
        {
            if (!TryHolsterPose(slot.Side, out var target))
                return;

            var root = slot.Root;
            if (slot.Following)
            {
                var turn = target.rotation * Quaternion.Inverse(slot.LastHolster.rotation);
                root.SetPositionAndRotation(target.position + turn * (root.position - slot.LastHolster.position), turn * root.rotation);
            }

            slot.LastHolster = target;
            slot.Following = true;
            if (slot.Holstered)
            {
                root.SetPositionAndRotation(target.position, target.rotation);
                return;
            }

            if (deltaTime <= 0f)
                return;
            var blend = 1f - Mathf.Exp(-k_ReturnSpeed * deltaTime);
            root.SetPositionAndRotation(Vector3.Lerp(root.position, target.position, blend), Quaternion.Slerp(root.rotation, target.rotation, blend));
            if ((root.position - target.position).sqrMagnitude < 0.0004f)
                slot.Holstered = true;
        }

        void SetHidden(bool hidden)
        {
            m_Hidden = hidden;
            foreach (var slot in m_Slots)
            {
                if (hidden)
                {
                    Release(slot);
                    EndGlitch(slot);
                    if (slot.Ring != null)
                        slot.Ring.Cancel();
                    if (slot.Guide != null)
                        slot.Guide.Hide();
                    slot.Restart = -1f;
                }

                if (slot.Root != null)
                    slot.Root.gameObject.SetActive(!hidden);
                if (hidden)
                    continue;

                slot.NeedsIgnore = true;
                if (m_Desktop)
                    BeginRing(slot);
                else
                    SnapToHolster(slot);
            }
        }

        // Sans casque : les arbalètes suivent la caméra, et ne se prennent pas.
        void SetDesktop(bool desktop)
        {
            m_Desktop = desktop;
            var controls = DesktopControls.Instance;
            foreach (var slot in m_Slots)
            {
                if (slot.Root == null)
                    continue;

                if (desktop)
                    Release(slot);
                if (slot.Grab != null)
                    slot.Grab.enabled = !desktop;
                foreach (var collider in slot.Colliders)
                {
                    if (collider != null)
                        collider.enabled = !desktop;
                }

                if (desktop && controls != null && controls.CameraTransform != null)
                {
                    slot.Root.SetParent(controls.CameraTransform, false);
                    slot.Root.localPosition = DesktopPosition(slot);
                    slot.Root.localRotation = Quaternion.identity;
                    BeginRing(slot);
                }
                else
                {
                    slot.Root.SetParent(transform, true);
                    if (slot.Ring != null)
                        slot.Ring.Cancel();
                    slot.Restart = -1f;
                    slot.NeedsIgnore = true;
                    SnapToHolster(slot);
                }
            }
        }

        static Vector3 DesktopPosition(Slot slot) => Vector3.Scale(k_DesktopOffset, new Vector3(slot.Side, 1f, 1f));

        // Sans casque : les arbalètes visent le centre de l'écran ; le clic gauche tire une main puis l'autre.
        void UpdateDesktop()
        {
            var controls = DesktopControls.Instance;
            var camera = controls != null ? controls.CameraTransform : null;
            if (camera == null)
                return;

            var target = AimPoint(camera);
            foreach (var slot in m_Slots)
            {
                if (slot.Root == null)
                    continue;
                slot.Root.localPosition = DesktopPosition(slot);
                var toTarget = target - slot.Crossbow.TransformPoint(m_Definition.akimboTip);
                if (toTarget.sqrMagnitude > 1e-4f)
                    slot.Root.rotation = Quaternion.LookRotation(toTarget, camera.up);
            }

            var mouse = Mouse.current;
            if (controls.HasFocus && !controls.RefocusedThisFrame && !DesktopHand.IsTypingText && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                // L'arbalète dont l'anneau est prêt : celle qui n'a pas tiré la dernière, sinon l'autre.
                for (var i = 0; i < m_Slots.Count; i++)
                {
                    var slot = m_Slots[(m_NextDesktopHand + i) % m_Slots.Count];
                    if (slot.Restart >= 0f)
                        continue;
                    TryFire(slot);
                    m_NextDesktopHand = (m_NextDesktopHand + i + 1) % m_Slots.Count;
                    break;
                }
            }
        }

        static Vector3 AimPoint(Transform camera)
        {
            var best = 80f;
            var count = Physics.RaycastNonAlloc(camera.position, camera.forward, s_Hits, best, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if (hit.collider == null || hit.distance < 1.5f || hit.distance >= best || ArrowIgnore.IsIgnored(hit.collider) ||
                    hit.collider.GetComponentInParent<Arrow>() != null)
                    continue;
                best = hit.distance;
            }

            return camera.position + camera.forward * best;
        }

        // ---------------------------------------------------------------- le tir

        void BeginRing(Slot slot)
        {
            slot.Restart = -1f;
            if (slot.Ring == null)
                return;
            if (m_Bow != null)
                m_Bow.BeginRing(slot.Ring, m_Definition);
            else
                slot.Ring.Begin(m_Definition.ringDuration, m_Definition.goldHalfWidth * DifficultyManager.Current.goldBandWidth, m_Definition.goodWidth);
        }

        ShotTuning Tuning => m_Bow != null ? m_Bow.ShotTuning : ShotTuning.Fallback;

        // La qualité qu'aurait un tir maintenant : celle de l'anneau (sans anneau : un bon tir).
        ShotGrade CurrentGrade(Slot slot)
        {
            if (slot.Ring == null)
                return ShotGrade.Good;
            var grade = slot.Ring.CurrentGrade;
            if (m_Bow != null && m_Bow.ForcePerfect && grade != ShotGrade.None)
                grade = ShotGrade.Perfect;
            return grade;
        }

        float Speed(ShotGrade grade) =>
            m_Definition.arrowSpeed * Tuning.Get(grade).speed * (m_Bow != null && !m_Trial ? m_Bow.SpeedMultiplier : 1f);

        void TryFire(Slot slot)
        {
            // En pause (menu ouvert en cours de partie), on ne tire pas : la flèche resterait figée en l'air.
            var pool = ArrowPool.Instance;
            if (slot.Crossbow == null || slot.Restart >= 0f || pool == null || SlowMotion.IsPaused)
                return;

            ShotGrade grade;
            if (slot.Ring != null)
            {
                if (!slot.Ring.IsRunning)
                    return;
                grade = slot.Ring.Release();
                if (m_Bow != null && m_Bow.ForcePerfect && grade != ShotGrade.None)
                    grade = ShotGrade.Perfect;
            }
            else
            {
                grade = ShotGrade.Good;
            }

            var modifiers = Tuning.Get(grade);
            var speed = Speed(grade);
            var damage = m_Trial ? m_TrialDamage : m_Definition.damage * modifiers.damage * (m_Bow != null ? m_Bow.DamageMultiplier : 1f);
            var origin = slot.Crossbow.TransformPoint(m_Definition.akimboTip);
            var direction = slot.Crossbow.forward;

            var arrow = pool.Get();
            arrow.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
            arrow.Launch(new ArrowLaunch
            {
                Velocity = direction * speed,
                Damage = damage,
                Grade = grade,
                Pierce = m_Trial ? 0 : m_Definition.pierceCount + (m_Bow != null ? m_Bow.BonusPierce : 0),
                TrailColor = modifiers.color,
                IsShot = true,
                IsExtra = m_Trial,
                GravityScale = m_Definition.arrowGravity,
            });

            if (slot.Ring != null)
                slot.Ring.ShowResult(modifiers.color);
            slot.Restart = k_RingRestart;
            slot.Kick = 0.025f;

            Sfx.Play(m_Definition.akimboFireClip, origin, 0.8f, Random.Range(1.25f, 1.4f));
            if (grade == ShotGrade.Perfect && m_Bow != null)
                Sfx.Play(m_Bow.PerfectClip, origin, 0.4f, 1.15f);
            if (!m_Desktop)
                Haptics.Pulse(slot.Holder, 0.5f, 0.06f);

            if (!m_Trial && m_Bow != null)
            {
                m_Bow.ReportShot(new ShotInfo
                {
                    Arrow = arrow,
                    Grade = grade,
                    DrawRatio = 1f,
                    Speed = speed,
                    Damage = damage,
                    Origin = origin,
                    Direction = direction,
                });
            }

            Fired?.Invoke(origin, direction);
        }

        // La trajectoire qu'aurait le tir maintenant, dans la couleur de l'anneau (blanche le temps qu'il reparte) ; seulement
        // pour une arbalète en main.
        void UpdateGuide(Slot slot, bool show)
        {
            if (slot.Guide == null)
                return;
            if (!show || slot.Restart >= 0f)
            {
                slot.Guide.Hide();
                return;
            }

            var grade = CurrentGrade(slot);
            var color = grade == ShotGrade.None ? Color.white : Tuning.Get(grade).color;
            var origin = slot.Crossbow.TransformPoint(m_Definition.akimboTip);
            slot.Guide.Show(origin, slot.Crossbow.forward, Speed(grade), color, 0.75f, 0f, m_Definition.arrowGravity);
        }

        // ---------------------------------------------------------------- les bugs

        void UpdateGlitch(Slot slot, float now)
        {
            if (slot.GlitchEnd >= 0f && now >= slot.GlitchEnd)
            {
                EndGlitch(slot);
                slot.NextGlitch = now + Random.Range(2.5f, 6f);
            }

            if (slot.GlitchEnd < 0f && now >= slot.NextGlitch)
            {
                slot.GlitchEnd = now + Random.Range(0.12f, 0.3f);
                slot.GlitchFrame = now;
            }

            if (slot.GlitchEnd >= 0f && now >= slot.GlitchFrame)
            {
                slot.GlitchFrame = now + Random.Range(0.04f, 0.09f);
                GlitchFrame(slot);
            }
        }

        // Une image du bug : le modèle se brouille, sursaute parfois de quelques millimètres, et 1 à 3 morceaux sombres
        // apparaissent ailleurs à chaque image.
        void GlitchFrame(Slot slot)
        {
            m_Block ??= new MaterialPropertyBlock();
            foreach (var renderer in slot.Body)
            {
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(m_Block);
                m_Block.SetFloat(k_GlitchId, Random.Range(0.25f, 0.45f));
                renderer.SetPropertyBlock(m_Block);
            }

            slot.Jump = Random.value < 0.35f ? new Vector3(Random.Range(-0.004f, 0.004f), Random.Range(-0.002f, 0.003f), 0f) : Vector3.zero;
            foreach (var fragment in slot.Fragments)
                fragment.gameObject.SetActive(false);
            var shown = Random.Range(1, 4);
            for (var i = 0; i < shown && slot.Fragments.Count > 0; i++)
                PlaceFragment(slot.Fragments[Random.Range(0, slot.Fragments.Count)]);
        }

        // Un rectangle sombre sur une face de l'arbalète (les flancs du fût, le dessus du rail ou des branches), dans le
        // repère du modèle d'origine.
        void PlaceFragment(Renderer fragment)
        {
            Vector3 position;
            Quaternion rotation;
            switch (Random.Range(0, 4))
            {
                case 0:
                case 1:
                    var side = Random.value < 0.5f ? -1f : 1f;
                    position = new Vector3(side * 0.04f, Random.Range(0.03f, 0.12f), Random.Range(-0.24f, 0.2f));
                    rotation = Quaternion.LookRotation(new Vector3(side, 0f, 0f), Vector3.up);
                    break;
                case 2:
                    position = new Vector3(Random.Range(-0.015f, 0.015f), 0.137f, Random.Range(-0.08f, 0.2f));
                    rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                    break;
                default:
                    position = new Vector3(Random.Range(-0.14f, 0.15f), 0.111f, Random.Range(0.15f, 0.19f));
                    rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                    break;
            }

            var piece = fragment.transform;
            piece.localPosition = position;
            piece.localRotation = rotation;
            piece.localScale = new Vector3(Random.Range(0.02f, 0.06f), Random.Range(0.006f, 0.02f), 1f);

            m_Block ??= new MaterialPropertyBlock();
            fragment.GetPropertyBlock(m_Block);
            m_Block.SetColor(k_ColorId, k_FragmentColors[Random.Range(0, k_FragmentColors.Length)]);
            fragment.SetPropertyBlock(m_Block);
            fragment.gameObject.SetActive(true);
        }

        void EndGlitch(Slot slot)
        {
            slot.GlitchEnd = -1f;
            slot.Jump = Vector3.zero;
            foreach (var renderer in slot.Body)
            {
                if (renderer != null)
                    renderer.SetPropertyBlock(null);
            }

            foreach (var fragment in slot.Fragments)
            {
                if (fragment != null)
                    fragment.gameObject.SetActive(false);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Instances.Clear();
    }
}
