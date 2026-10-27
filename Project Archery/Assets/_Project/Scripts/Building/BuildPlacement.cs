using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Player;
using Archery.Shop;
using Archery.UI;
using Archery.Waves;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace Archery.Building
{
    /// <summary>
    /// La pose d'un bâtiment (refonte du tower defense, étape 3 ; version choisie par le joueur le 27 octobre,
    /// <c>Docs/Refonte_TowerDefense.md</c>, partie 3). On touche le bouton « Plans » de la montre (<see cref="WatchPlansButton"/>)
    /// et le plan d'un bâtiment en sort : son hologramme bleu suit le rayon de la main qui porte la montre et se cale sur les
    /// emplacements de la <see cref="BuildGrid"/>, rouge là où il ne se pose pas. Le joystick de cette main le tourne (gauche,
    /// droite) ou passe à un autre plan (haut, bas), sa gâchette le pose (les marteaux apparaissent, comme avant), sa poignée
    /// annule. Viser un bâtiment du même type le fait monter d'un niveau. Pendant la pose, les runes des emplacements où ce
    /// bâtiment peut aller s'allument autour du joueur.
    /// </summary>
    /// <remarks>
    /// Le même hologramme sert à <b>déplacer</b> un bâtiment (carte « Déplacer » de son panneau, entre les vagues).
    /// Pendant la pose, la rotation du joueur au joystick est coupée (le joystick tourne le bâtiment), puis rendue.
    /// Sans casque : F2 sort un plan (puis passe au suivant, puis range), la molette tourne, E pose.
    /// Créé tout seul par le <see cref="BuildManager"/> : rien à poser dans la scène. Le bouton de la montre est copié du bouton « Tour ».
    /// </remarks>
    [DisallowMultipleComponent]
    public class BuildPlacement : MonoBehaviour
    {
        static readonly RaycastHit[] s_Hits = new RaycastHit[32];
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_FilledColorId = Shader.PropertyToID("_FilledColor");
        static readonly int k_EdgeColorId = Shader.PropertyToID("_EdgeColor");

        [Tooltip("Portée (m) du rayon de la main pendant la pose.")]
        [SerializeField]
        float m_MaxDistance = 14f;

        [Tooltip("Angle (°) d'un cran de rotation au joystick (ou à la molette).")]
        [SerializeField]
        float m_RotationStep = 45f;

        [Tooltip("Les runes des emplacements libres à moins de cette distance (m) du joueur s'allument pendant la pose.")]
        [SerializeField]
        float m_RevealRadius = 12f;

        [Tooltip("Écart (m) entre le bouton « Tour » de la montre et le bouton « Plans », de centre à centre : plus que le rayon de " +
                 "toucher du bouton « Tour » (7 cm) plus celui du bouton « Plans » (4,5 cm), pour ne pas déclencher l'un en touchant l'autre.")]
        [SerializeField]
        float m_WatchButtonGap = 0.13f;

        [Tooltip("Couleurs de l'hologramme là où il ne se pose pas (vide, rempli, bords).")]
        [SerializeField]
        Color m_InvalidColor = new Color(1f, 0.28f, 0.22f, 0.25f);

        [SerializeField]
        Color m_InvalidEdgeColor = new Color(1f, 0.45f, 0.35f, 1f);

        [Header("Sons")]
        [SerializeField]
        AudioClip m_ErrorClip;

        readonly XRInputValueReader<Vector2> m_LeftStick = Stick("Pose (main gauche)", "<XRController>{LeftHand}/{Primary2DAxis}");
        readonly XRInputValueReader<Vector2> m_RightStick = Stick("Pose (main droite)", "<XRController>{RightHand}/{Primary2DAxis}");
        readonly List<StructureDefinition> m_Available = new List<StructureDefinition>();
        readonly List<LocomotionProvider> m_DisabledTurns = new List<LocomotionProvider>();

        bool m_Active;
        StructureDefinition m_Definition;
        Structure m_Moving;
        XRBaseInputInteractor m_Hand;
        bool m_Desktop;
        int m_Steps;
        bool m_Valid;
        bool m_Upgrade;
        string m_Status = "";
        Vector2 m_PreviousStick;
        bool m_TriggerArmed;
        bool m_GripArmed;
        GameObject m_Hologram;
        Material m_Material;
        Color m_ValidBase;
        Color m_ValidFilled;
        Color m_ValidEdge;
        bool m_ShowsValid = true;
        TextMeshPro m_Label;
        float m_HologramTop = 1f;
        WaveManager m_Waves;
        Camera m_Camera;

        public static BuildPlacement Instance { get; private set; }

        /// <summary>Un plan (ou un bâtiment à déplacer) est sorti : son hologramme suit le rayon.</summary>
        public bool IsActive => m_Active;

        /// <summary>Un bâtiment est en cours de déplacement.</summary>
        public bool IsMoving => m_Active && m_Moving != null;

        /// <summary>L'emplacement visé en ce moment (ses runes brillent), ou null.</summary>
        public BuildSocket Target { get; private set; }

        /// <summary>L'hologramme se pose là où il est (pour le viseur sans casque).</summary>
        public bool CanPlace => m_Active && m_Valid;

        /// <summary>Le texte du viseur sans casque pendant la pose.</summary>
        public string DesktopPrompt => m_Active ? $"{Plain(m_Status)} · E : poser · molette : tourner · F2 : plan suivant" : null;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Build Placement dans la scène.", this);
            Instance = this;
        }

        void OnEnable()
        {
            m_LeftStick.EnableDirectActionIfModeUsed();
            m_RightStick.EnableDirectActionIfModeUsed();
        }

        void OnDisable()
        {
            m_LeftStick.DisableDirectActionIfModeUsed();
            m_RightStick.DisableDirectActionIfModeUsed();
            Cancel(null);
        }

        void Start()
        {
            m_Waves = WaveManager.Instance;
            if (m_Waves != null)
            {
                m_Waves.WaveStarted += OnWaveStarted;
                m_Waves.GameOver += OnGameOver;
            }

            // Le bouton « Plans » de la montre, copié du bouton « Tour » à côté de lui.
            var tower = FindAnyObjectByType<WatchTowerButton>(FindObjectsInactive.Include);
            if (tower != null)
                WatchPlansButton.Create(tower, m_WatchButtonGap);
            else
                Debug.LogWarning("BuildPlacement : pas de bouton « Tour » sur la montre, pas de bouton « Plans » non plus (F2 sans casque).", this);
        }

        void OnDestroy()
        {
            if (m_Waves != null)
            {
                m_Waves.WaveStarted -= OnWaveStarted;
                m_Waves.GameOver -= OnGameOver;
            }

            if (Instance == this)
                Instance = null;
        }

        void OnWaveStarted(int wave)
        {
            if (IsMoving)
                Cancel("Les bâtiments se déplacent entre les vagues");
        }

        void OnGameOver() => Cancel(null);

        // ---------------------------------------------------------------- début et fin

        /// <summary>Sort un plan de la montre : celui du premier bâtiment dont le joueur a des plans. Faux s'il n'en a pas.</summary>
        public bool BeginFromWatch(XRBaseInputInteractor hand)
        {
            if (hand == null || m_Active)
                return false;

            var builds = BuildManager.Instance;
            if (builds == null || builds.IsBuilding)
            {
                Announce("Termine d'abord la construction en cours");
                return false;
            }

            CollectAvailable();
            if (m_Available.Count == 0)
            {
                Announce("Pas de plan : ouvre les caisses de l'ingénieur");
                return false;
            }

            Begin(hand, m_Available[0], null);
            return true;
        }

        /// <summary>
        /// Déplace un bâtiment (carte « Déplacer » de son panneau, entre les vagues) : son hologramme suit le rayon de la main
        /// donnée, comme un plan.
        /// </summary>
        public ShopResult BeginMove(Structure structure, XRBaseInputInteractor hand)
        {
            var builds = BuildManager.Instance;
            if (structure == null || !structure.IsBuilt || structure.IsUnderConstruction)
                return ShopResult.Failed("Termine d'abord sa construction");
            if (builds == null || !builds.CanMove)
                return ShopResult.Failed("On déplace les bâtiments entre les vagues");

            hand ??= FreeHand();
            if (hand == null)
                return ShopResult.Failed("Aucune main pour viser");
            if (m_Active)
                Cancel(null);

            Begin(hand, structure.Definition, structure);
            return ShopResult.Done("Où le poser ?", structure.Definition != null ? structure.Definition.color : Color.white);
        }

        /// <summary>Range le plan (ou abandonne le déplacement). Renvoie vrai si une pose était en cours.</summary>
        public bool Cancel(string message)
        {
            if (!m_Active)
                return false;

            m_Active = false;
            m_Moving = null;
            m_Definition = null;
            Target = null;
            if (m_Hologram != null)
                Destroy(m_Hologram);
            if (m_Material != null)
                Destroy(m_Material);
            if (m_Label != null)
                m_Label.gameObject.SetActive(false);
            m_Hologram = null;
            m_Material = null;
            RestoreTurning();

            if (!string.IsNullOrEmpty(message))
                Announce(message);
            return true;
        }

        void Begin(XRBaseInputInteractor hand, StructureDefinition definition, Structure moving)
        {
            m_Active = true;
            m_Hand = hand;
            m_Desktop = hand is DesktopHand;
            m_Moving = moving;
            m_Steps = 0;
            m_PreviousStick = Vector2.zero;

            // Le bouton qui vient de servir (gâchette d'une carte, poignée) doit être relâché avant de compter.
            m_TriggerArmed = !IsPressed(hand.activateInput);
            m_GripArmed = !IsPressed(hand.selectInput);

            SetDefinition(definition);
            if (!m_Desktop)
                StopTurning();
        }

        void SetDefinition(StructureDefinition definition)
        {
            m_Definition = definition;
            if (m_Hologram != null)
                Destroy(m_Hologram);

            Structure source = m_Moving;
            var level = m_Moving != null ? Mathf.Max(1, m_Moving.Level) : 1;
            if (source == null && definition != null)
                source = definition.prefab;
            m_Hologram = source != null ? BuildHologram(source, level) : new GameObject("Hologramme");
        }

        // Les bâtiments dont le joueur a des plans, dans l'ordre du Build Manager.
        void CollectAvailable()
        {
            m_Available.Clear();
            var builds = BuildManager.Instance;
            if (builds == null)
                return;

            foreach (var definition in builds.Structures)
            {
                if (definition != null && definition.prefab != null && builds.PlansOf(definition) > 0)
                    m_Available.Add(definition);
            }
        }

        // Le plan suivant (ou précédent). Sans casque, après le dernier, le plan est rangé.
        void NextPlan(int direction, bool cancelAfterLast)
        {
            if (m_Moving != null)
                return;

            CollectAvailable();
            if (m_Available.Count == 0)
            {
                Cancel("Plus de plan");
                return;
            }

            var index = m_Available.IndexOf(m_Definition);
            var next = index + direction;
            if (cancelAfterLast && next >= m_Available.Count)
            {
                Cancel(null);
                return;
            }

            next = (next % m_Available.Count + m_Available.Count) % m_Available.Count;
            if (m_Available[next] != m_Definition)
                SetDefinition(m_Available[next]);
        }

        // ---------------------------------------------------------------- chaque image

        void Update()
        {
            UpdateDesktopKey();
            if (!m_Active)
                return;

            if (m_Hand == null || !m_Hand.isActiveAndEnabled || (m_Moving == null && m_Definition == null) ||
                (m_Moving == null && BuildManager.Instance != null && BuildManager.Instance.PlansOf(m_Definition) <= 0))
            {
                Cancel(null);
                return;
            }

            Aim();
            if (m_Desktop)
                ReadDesktopInput();
            else
                ReadControllerInput();
        }

        void LateUpdate()
        {
            if (!m_Active || m_Label == null || !m_Label.gameObject.activeSelf)
                return;

            // L'étiquette au-dessus de l'hologramme se tourne vers le joueur et garde une taille lisible de loin.
            if (m_Camera == null)
                m_Camera = Camera.main;
            if (m_Camera == null)
                return;

            var toLabel = m_Label.transform.position - m_Camera.transform.position;
            if (toLabel.sqrMagnitude > 1e-6f)
                m_Label.transform.rotation = Quaternion.LookRotation(toLabel, m_Camera.transform.up);
            m_Label.transform.localScale = Vector3.one * (0.55f * Mathf.Max(0.35f, toLabel.magnitude * 0.1f));
        }

        // Le rayon de la main : l'emplacement visé (sous le point touché, ou celui d'un bâtiment touché), l'hologramme et son étiquette.
        void Aim()
        {
            var origin = m_Hand.transform.position;
            var direction = m_Hand.transform.forward;
            var point = origin + direction * 3f;
            BuildSocket target = null;
            var hasPoint = TryRaycast(origin, direction, out var hit);
            if (hasPoint)
            {
                point = hit.point;
                var structure = hit.collider.GetComponentInParent<Structure>();
                if (structure != null && structure != m_Moving && structure.Socket != null)
                    target = structure.Socket;
                else if (BuildGrid.Instance != null)
                    target = BuildGrid.Instance.SocketAt(point);
            }

            Target = target;
            Evaluate(target, hasPoint);

            if (m_Hologram != null)
            {
                Vector3 position;
                Quaternion rotation;
                if (target != null && m_Upgrade && target.Structure != null)
                {
                    position = target.Structure.transform.position;
                    rotation = target.Structure.transform.rotation;
                }
                else if (target != null)
                {
                    position = target.transform.position;
                    rotation = target.transform.rotation * Offset;
                }
                else
                {
                    var back = -direction;
                    back.y = 0f;
                    position = point;
                    rotation = (back.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(back) : Quaternion.identity) * Offset;
                }

                m_Hologram.SetActive(hasPoint);
                m_Hologram.transform.SetPositionAndRotation(position, rotation);
                SetValidLook(m_Valid);
                ShowLabel(position + Vector3.up * (m_HologramTop + 0.45f), hasPoint);
            }
        }

        Quaternion Offset => Quaternion.Euler(0f, m_Steps * m_RotationStep, 0f);

        // Le premier objet touché par le rayon, sauf le corps du joueur et ce qu'il tient, les plaques des emplacements et les
        // déclencheurs.
        bool TryRaycast(Vector3 origin, Vector3 direction, out RaycastHit best)
        {
            best = default;
            var count = Physics.RaycastNonAlloc(origin, direction, s_Hits, m_MaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if (hit.distance >= bestDistance || ArrowIgnore.IsIgnored(hit.collider) || hit.collider.GetComponentInParent<BuildHammer>() != null)
                    continue;

                bestDistance = hit.distance;
                best = hit;
            }

            return bestDistance < float.MaxValue;
        }

        // Peut-on poser ici, et que dire au joueur ?
        void Evaluate(BuildSocket target, bool hasPoint)
        {
            m_Valid = false;
            m_Upgrade = false;
            var builds = BuildManager.Instance;
            var name = m_Definition != null ? m_Definition.displayName : "Bâtiment";
            var plans = builds != null ? builds.PlansOf(m_Definition) : 0;
            var header = m_Moving != null ? $"Déplacer : {m_Moving.DisplayName}" : $"{name} · {(plans > 1 ? plans + " plans" : "1 plan")}";

            if (builds != null && builds.IsBuilding)
            {
                m_Status = $"{header}\n<color=#FF9080>Termine d'abord la construction en cours</color>";
                return;
            }

            if (target == null || !target.isActiveAndEnabled)
            {
                m_Status = $"{header}\n<color=#FF9080>{(hasPoint ? "Vise une zone constructible" : "Vise le sol")}</color>";
                return;
            }

            var structure = target.Structure;
            if (structure != null && structure == m_Moving)
            {
                // Le bâtiment déplacé, visé sur sa propre place : il y reste, tourné comme l'hologramme.
                m_Valid = true;
                m_Status = $"{header}\nLe tourner sur place";
                return;
            }

            if (structure != null)
            {
                if (m_Moving == null && structure.Definition == m_Definition && structure.IsBuilt && !structure.IsUnderConstruction &&
                    structure.Level < structure.MaxLevel)
                {
                    m_Valid = true;
                    m_Upgrade = true;
                    m_Status = $"{header}\nAméliorer : niveau {structure.Level + 1}";
                    return;
                }

                var reason = structure.IsUnderConstruction ? "Construction en cours"
                    : m_Moving == null && structure.Definition == m_Definition ? "Niveau maximal atteint"
                    : "Emplacement pris";
                m_Status = $"{header}\n<color=#FF9080>{reason}</color>";
                return;
            }

            if (m_Definition != null && !m_Definition.Fits(target.Slot))
            {
                var where = m_Definition.slot == BuildSlot.Lane ? "sur la voie"
                    : m_Definition.slot == BuildSlot.Roadside ? "au bord de la voie ou en hauteur"
                    : BuildSocket.NameOf(m_Definition.slot).ToLowerInvariant();
                m_Status = $"{header}\n<color=#FF9080>Pas ici : se pose {where}</color>";
                return;
            }

            m_Valid = true;
            m_Status = m_Desktop ? $"{header}\n{BuildSocket.NameOf(target.Slot)}"
                : $"{header}\n<size=70%>{BuildSocket.NameOf(target.Slot)} · gâchette : poser · joystick : tourner, changer · poignée : annuler</size>";
        }

        void Place()
        {
            var builds = BuildManager.Instance;
            var target = Target;
            if (builds == null || target == null || !m_Valid)
            {
                Sfx.Play(m_ErrorClip, m_Hologram != null ? m_Hologram.transform.position : transform.position, 0.8f);
                return;
            }

            var result = m_Moving != null ? builds.MoveTo(m_Moving, target, Offset)
                : m_Upgrade ? builds.TryBuild(target, m_Definition, m_Hand)
                : builds.TryBuild(target, m_Definition, m_Hand, Offset);
            if (!result.Success)
            {
                Sfx.Play(m_ErrorClip, target.transform.position, 0.8f);
                Announce(result.Message);
                return;
            }

            Cancel(null);
        }

        void Rotate(int direction)
        {
            m_Steps += direction;
            if (m_Hand != null && !m_Desktop)
                Haptics.Pulse(m_Hand, 0.2f, 0.03f);
        }

        // ---------------------------------------------------------------- manettes et clavier

        // Le joystick de la main qui vise : gauche et droite tournent, haut et bas changent de plan ; sa gâchette pose, sa poignée annule.
        void ReadControllerInput()
        {
            var stick = (m_Hand.handedness == InteractorHandedness.Left ? m_LeftStick : m_RightStick).ReadValue();
            const float press = 0.65f;
            const float release = 0.35f;
            if (Mathf.Abs(stick.x) > press && Mathf.Abs(m_PreviousStick.x) <= release)
                Rotate(stick.x > 0f ? 1 : -1);
            if (Mathf.Abs(stick.y) > press && Mathf.Abs(m_PreviousStick.y) <= release)
                NextPlan(stick.y > 0f ? -1 : 1, false);
            if (Mathf.Abs(stick.x) <= release)
                m_PreviousStick.x = 0f;
            else if (Mathf.Abs(stick.x) > press)
                m_PreviousStick.x = stick.x;
            if (Mathf.Abs(stick.y) <= release)
                m_PreviousStick.y = 0f;
            else if (Mathf.Abs(stick.y) > press)
                m_PreviousStick.y = stick.y;
            if (!m_Active)
                return;

            if (!IsPressed(m_Hand.selectInput))
                m_GripArmed = true;
            else if (m_GripArmed && WasPressed(m_Hand.selectInput))
            {
                Cancel("Pose annulée");
                return;
            }

            if (!IsPressed(m_Hand.activateInput))
                m_TriggerArmed = true;
            else if (m_TriggerArmed && WasPressed(m_Hand.activateInput))
                Place();
        }

        void ReadDesktopInput()
        {
            var desktop = DesktopControls.Instance;
            if (desktop == null || !desktop.HasFocus)
                return;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                var scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.1f)
                    Rotate(1);
                else if (scroll < -0.1f)
                    Rotate(-1);
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                Place();
        }

        // Sans casque : F2 sort un plan, puis passe au suivant, puis le range.
        void UpdateDesktopKey()
        {
            var desktop = DesktopControls.Instance;
            var keyboard = Keyboard.current;
            if (desktop == null || !desktop.IsActive || !desktop.HasFocus || keyboard == null || !keyboard.f2Key.wasPressedThisFrame)
                return;

            if (m_Active)
            {
                NextPlan(1, true);
                return;
            }

            var hand = FindAnyObjectByType<DesktopHand>();
            if (!BeginFromWatch(hand))
                Sfx.Play(m_ErrorClip, desktop.CameraTransform != null ? desktop.CameraTransform.position : transform.position, 0.6f);
        }

        static bool IsPressed(XRInputButtonReader reader) => reader != null && reader.ReadIsPerformed();

        static bool WasPressed(XRInputButtonReader reader) => reader != null && reader.ReadWasPerformedThisFrame();

        // Pendant la pose, le joystick tourne le bâtiment : la rotation du joueur au joystick est coupée, puis rendue.
        void StopTurning()
        {
            var origin = FindAnyObjectByType<XROrigin>();
            if (origin == null)
                return;

            foreach (var provider in origin.GetComponentsInChildren<LocomotionProvider>(true))
            {
                if (provider.enabled && (provider is SnapTurnProvider || provider is ContinuousTurnProvider))
                {
                    provider.enabled = false;
                    m_DisabledTurns.Add(provider);
                }
            }
        }

        void RestoreTurning()
        {
            foreach (var provider in m_DisabledTurns)
            {
                if (provider != null)
                    provider.enabled = true;
            }

            m_DisabledTurns.Clear();
        }

        static XRBaseInputInteractor FreeHand()
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
                return FindAnyObjectByType<DesktopHand>();

            var free = rig.BowHand == InteractorHandedness.Left ? InteractorHandedness.Right : InteractorHandedness.Left;
            return rig.TryGetHand(free, out var hand) ? hand : null;
        }

        static XRInputValueReader<Vector2> Stick(string name, string bindingPath)
        {
            var reader = new XRInputValueReader<Vector2>(name, XRInputValueReader.InputSourceMode.InputAction);
            reader.inputAction.AddBinding(bindingPath);
            return reader;
        }

        // ---------------------------------------------------------------- runes

        /// <summary>
        /// Les runes de cet emplacement libre s'allument-elles ? Pendant la pose, celles où le bâtiment peut aller, autour du joueur.
        /// </summary>
        public bool Reveals(BuildSocket socket)
        {
            var rig = PlayerRig.Instance;
            var definition = m_Moving != null ? m_Moving.Definition : m_Definition;
            if (!m_Active || socket == null || socket.Structure != null || definition == null || !definition.Fits(socket.Slot))
                return false;

            var center = rig != null ? rig.BodyPosition : m_Hand != null ? m_Hand.transform.position : transform.position;
            var offset = socket.transform.position - center;
            offset.y = 0f;
            return offset.sqrMagnitude <= m_RevealRadius * m_RevealRadius;
        }

        // ---------------------------------------------------------------- hologramme et étiquette

        // Les maillages visibles du bâtiment (au niveau donné), en plan bleu, sans rien d'autre (ni comportement, ni collider, ni
        // son) : l'hologramme ne tire pas, ne se vise pas et ne gêne pas le rayon.
        GameObject BuildHologram(Structure source, int level)
        {
            var builds = BuildManager.Instance;
            var blueprint = builds != null ? builds.BlueprintMaterial : null;
            if (m_Material == null && blueprint != null)
            {
                m_Material = new Material(blueprint) { name = "Hologramme de pose" };
                m_ValidBase = m_Material.GetColor(k_BaseColorId);
                m_ValidFilled = m_Material.GetColor(k_FilledColorId);
                m_ValidEdge = m_Material.GetColor(k_EdgeColorId);
                m_ShowsValid = true;
            }

            var root = new GameObject("Hologramme · " + (source.Definition != null ? source.Definition.displayName : source.name));
            var sourceRoot = source.transform;
            root.transform.localScale = sourceRoot.localScale;
            var toLocal = sourceRoot.worldToLocalMatrix;
            var parts = source.LevelParts;
            var top = 0.5f;
            foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || !renderer.enabled || !source.IsPreviewPart(renderer) ||
                    !IsShown(renderer.transform, sourceRoot, parts, level))
                    continue;

                var mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh
                    : renderer.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
                if (mesh == null)
                    continue;

                var part = new GameObject(renderer.name);
                part.transform.SetParent(root.transform, false);
                var matrix = toLocal * renderer.transform.localToWorldMatrix;
                part.transform.localPosition = matrix.GetColumn(3);
                part.transform.localRotation = matrix.rotation;
                part.transform.localScale = matrix.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = mesh;

                var meshRenderer = part.AddComponent<MeshRenderer>();
                var materials = new Material[Mathf.Max(1, mesh.subMeshCount)];
                for (var i = 0; i < materials.Length; i++)
                    materials[i] = m_Material;
                meshRenderer.sharedMaterials = materials;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;

                // Le haut de l'hologramme, pour y poser l'étiquette.
                var corner = matrix.MultiplyPoint3x4(mesh.bounds.center) + Vector3.up * mesh.bounds.extents.y * Mathf.Abs(matrix.lossyScale.y);
                top = Mathf.Max(top, corner.y);
            }

            m_HologramTop = top;
            return root;
        }

        // Visible au niveau donné : les pièces des niveaux 2 et 3 seulement à partir de leur niveau, le reste s'il est actif.
        static bool IsShown(Transform transform, Transform root, GameObject[] parts, int level)
        {
            for (var current = transform; current != null; current = current.parent)
            {
                var index = parts != null ? System.Array.IndexOf(parts, current.gameObject) : -1;
                if (index >= 0)
                {
                    if (level < index + 2)
                        return false;
                }
                else if (!current.gameObject.activeSelf)
                {
                    return false;
                }

                if (current == root)
                    break;
            }

            return true;
        }

        void SetValidLook(bool valid)
        {
            if (m_Material == null || valid == m_ShowsValid)
                return;

            m_ShowsValid = valid;
            m_Material.SetColor(k_BaseColorId, valid ? m_ValidBase : m_InvalidColor);
            m_Material.SetColor(k_FilledColorId, valid ? m_ValidFilled : m_InvalidColor);
            m_Material.SetColor(k_EdgeColorId, valid ? m_ValidEdge : m_InvalidEdgeColor);
        }

        void ShowLabel(Vector3 position, bool visible)
        {
            if (m_Desktop)
                visible = false;
            if (m_Label == null && visible)
                m_Label = CreateLabel();
            if (m_Label == null)
                return;

            m_Label.gameObject.SetActive(visible);
            if (!visible)
                return;

            m_Label.transform.position = position;
            if (m_Label.text != m_Status)
                m_Label.text = m_Status;
            m_Label.color = m_Valid ? Color.white : new Color(1f, 0.85f, 0.8f);
        }

        // Comme les textes flottants du jeu : le thème des menus, un contour sombre.
        static TextMeshPro CreateLabel()
        {
            var go = new GameObject("Étiquette de pose");
            var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontSize = 2.5f;
            var theme = UiTheme.Load();
            if (theme != null)
                UiThemer.ApplyLabel(text, theme);
            text.outlineWidth = 0.2f;
            text.outlineColor = theme != null ? new Color32(18, 8, 34, 220) : new Color32(0, 0, 0, 200);
            return text;
        }

        static string Plain(string text) => System.Text.RegularExpressions.Regex.Replace(text ?? "", "<[^>]*>", "").Replace('\n', ' ');

        void Announce(string text) => HeadMessages.Show(text, new Color(0.55f, 0.8f, 1f), 3f);
    }
}
