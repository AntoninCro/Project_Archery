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
    /// La pose d'un bâtiment (refonte du tower defense, étape 3 ; revue le 28 octobre après les tests du joueur,
    /// <c>Docs/Refonte_TowerDefense.md</c>, partie 3). On touche le bouton « Plans » de la montre (<see cref="WatchPlansButton"/>) :
    /// le menu des plans s'ouvre (<see cref="BuildMenu"/>), un carrousel de cartes avec l'aperçu 3D de chaque bâtiment, et
    /// l'hologramme bleu de la carte du milieu sort aussitôt (28 octobre au soir, demandé par le joueur : plus de gâchette pour
    /// choisir avant de poser). Il suit le rayon de la main qui porte la montre et se cale sur la grille de construction
    /// (<see cref="BuildGrid"/>), dessinée au sol autour du joueur : verte là où il se pose, rouge ailleurs
    /// (<see cref="BuildGridOverlay"/>). Le joystick de cette main change de carte (gauche, droite) ou tourne le bâtiment (haut,
    /// bas), sa gâchette le pose (les marteaux apparaissent, comme avant), sa poignée ferme tout. Viser un bâtiment du même type
    /// le fait monter d'un niveau.
    /// </summary>
    /// <remarks>
    /// Le même hologramme sert à <b>déplacer</b> un bâtiment (carte « Déplacer » de son panneau, entre les vagues), sans menu :
    /// le joystick le tourne. Pendant la pose, la rotation du joueur au joystick est coupée, puis rendue. Sans casque : F2 ouvre
    /// le menu avec l'hologramme (molette : changer, Maj + molette : tourner, E : poser), F2 le ferme. Créé tout seul par le
    /// <see cref="BuildManager"/> : rien à poser dans la scène. Le bouton de la montre est copié du bouton « Tour ».
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

        [Tooltip("Angle (°) d'un cran de rotation au joystick (ou à la molette) : la grille ne connaît que les crans de 45°.")]
        [SerializeField]
        float m_RotationStep = 45f;

        [Tooltip("Écart (m) minimal entre le bouton « Tour » de la montre (sous le cadran) et le bouton « Plans » (au-dessus), de " +
                 "centre à centre : plus que le rayon de toucher du bouton « Tour » (7 cm) plus celui du bouton « Plans » (4,5 cm), " +
                 "pour ne pas déclencher l'un en touchant l'autre.")]
        [SerializeField]
        float m_WatchButtonGap = 0.13f;

        [Tooltip("Couleurs de l'hologramme là où il ne se pose pas (vide, rempli, bords).")]
        [SerializeField]
        Color m_InvalidColor = new Color(1f, 0.28f, 0.22f, 0.25f);

        [SerializeField]
        Color m_InvalidEdgeColor = new Color(1f, 0.45f, 0.35f, 1f);

        [Header("Duos (31 octobre)")]
        [Tooltip("Couleurs de l'hologramme doré d'un duo : déplacer une tour sur une tour partenaire montre le duo qu'elles feront " +
                 "(vide et rempli, puis bords).")]
        [SerializeField]
        Color m_DuoColor = new Color(1f, 0.76f, 0.22f, 0.3f);

        [SerializeField]
        Color m_DuoEdgeColor = new Color(1f, 0.86f, 0.45f, 1f);

        [Header("Sons")]
        [SerializeField]
        AudioClip m_ErrorClip;

        readonly XRInputValueReader<Vector2> m_LeftStick = Stick("Pose (main gauche)", "<XRController>{LeftHand}/{Primary2DAxis}");
        readonly XRInputValueReader<Vector2> m_RightStick = Stick("Pose (main droite)", "<XRController>{RightHand}/{Primary2DAxis}");
        readonly List<StructureDefinition> m_Available = new List<StructureDefinition>();
        readonly List<LocomotionProvider> m_DisabledTurns = new List<LocomotionProvider>();
        readonly List<int> m_Cells = new List<int>();

        bool m_Placing;
        StructureDefinition m_Definition;
        StructureDefinition m_LastChoice;
        Structure m_Moving;
        XRBaseInputInteractor m_Hand;
        bool m_Desktop;
        int m_Steps;
        bool m_Valid;
        bool m_Upgrade;
        // La fusion visée (31 octobre) : la tour partenaire, son duo, et l'hologramme doré de ce duo.
        bool m_Fuse;
        Structure m_FuseTarget;
        TowerDuoCatalog.Entry m_FuseEntry;
        GameObject m_DuoHologram;
        Material m_DuoMaterial;
        TowerDuoCatalog.Entry m_DuoHologramEntry;
        int m_DuoHologramLevel;
        float m_DuoHologramTop = 1f;
        bool m_DuoShowsValid = true;
        BuildGrid.Spot m_Spot;
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
        BuildMenu m_Menu;
        BuildGridOverlay m_Overlay;

        public static BuildPlacement Instance { get; private set; }

        /// <summary>Le menu des plans est ouvert, ou un plan (ou un bâtiment à déplacer) est sorti.</summary>
        public bool IsActive => m_Placing || IsMenuOpen;

        /// <summary>Le menu des plans est ouvert.</summary>
        public bool IsMenuOpen => m_Menu != null && m_Menu.IsOpen;

        /// <summary>Un plan (ou un bâtiment à déplacer) est sorti : son hologramme suit le rayon.</summary>
        public bool IsPlacing => m_Placing;

        /// <summary>Un bâtiment est en cours de déplacement.</summary>
        public bool IsMoving => m_Placing && m_Moving != null;

        /// <summary>Le bâtiment visé pour l'améliorer, ou l'emplacement de la tour visé (ils brillent), ou null.</summary>
        public BuildSocket Target { get; private set; }

        /// <summary>L'hologramme se pose là où il est (pour le viseur sans casque).</summary>
        public bool CanPlace => m_Placing && m_Valid;

        /// <summary>Le texte du viseur sans casque pendant la pose.</summary>
        public string DesktopPrompt => !m_Placing ? null
            : IsMenuOpen ? $"{Plain(m_Status)} · E : poser · molette : changer · Maj + molette : tourner · F2 : fermer"
            : $"{Plain(m_Status)} · E : poser · molette : tourner · F2 : annuler";

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Build Placement dans la scène.", this);
            Instance = this;

            m_Menu = gameObject.AddComponent<BuildMenu>();
            m_Menu.Setup(this);
            m_Overlay = new GameObject("Grille de construction (dessin)").AddComponent<BuildGridOverlay>();
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

            // Le bouton « Plans » de la montre, copié du bouton « Tour », au-dessus du cadran (29 octobre).
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

            if (m_Overlay != null)
                Destroy(m_Overlay.gameObject);
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

        /// <summary>
        /// Ouvre le menu des plans (bouton de la montre, F2), avec l'hologramme de la carte du milieu (le dernier bâtiment choisi,
        /// sinon le premier) : la main donnée vise, change de carte et pose. Faux si le joueur n'a pas de plan ou qu'une
        /// construction est en cours.
        /// </summary>
        public bool OpenMenu(XRBaseInputInteractor hand)
        {
            if (hand == null || IsActive)
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

            m_Menu.Open(m_Available, Mathf.Max(0, m_Available.IndexOf(m_LastChoice)), hand, hand is DesktopHand);
            Begin(hand, m_Menu.Current, null);
            return true;
        }

        /// <summary>Une carte du menu touchée : son bâtiment devient celui de l'hologramme.</summary>
        internal void SelectFromMenu(StructureDefinition definition)
        {
            if (!IsMenuOpen || !m_Placing || m_Moving != null || definition == null || definition == m_Definition)
                return;

            m_LastChoice = definition;
            SetDefinition(definition);
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
            if (IsActive)
                Cancel(null);

            Begin(hand, structure.Definition, structure);
            return ShopResult.Done("Où le poser ?", structure.Definition != null ? structure.Definition.color : Color.white);
        }

        /// <summary>Ferme le menu, range le plan (ou abandonne le déplacement). Renvoie vrai si l'un ou l'autre était en cours.</summary>
        public bool Cancel(string message)
        {
            var was = IsActive;
            if (m_Menu != null)
                m_Menu.Close();
            StopPlacing();
            if (m_Overlay != null)
                m_Overlay.Hide();
            RestoreTurning();

            if (was && !string.IsNullOrEmpty(message))
                Announce(message);
            return was;
        }

        void StopPlacing()
        {
            m_Placing = false;
            m_Moving = null;
            m_Definition = null;
            Target = null;
            m_Fuse = false;
            m_FuseTarget = null;
            m_FuseEntry = null;
            m_DuoHologramEntry = null;
            m_Cells.Clear();
            if (m_Hologram != null)
                Destroy(m_Hologram);
            if (m_Material != null)
                Destroy(m_Material);
            if (m_DuoHologram != null)
                Destroy(m_DuoHologram);
            if (m_DuoMaterial != null)
                Destroy(m_DuoMaterial);
            m_DuoHologram = null;
            m_DuoMaterial = null;
            if (m_Label != null)
                m_Label.gameObject.SetActive(false);
            m_Hologram = null;
            m_Material = null;
            if (m_Overlay != null)
                m_Overlay.HideFootprint();
        }

        void Begin(XRBaseInputInteractor hand, StructureDefinition definition, Structure moving)
        {
            m_Placing = true;
            m_Hand = hand;
            m_Desktop = hand is DesktopHand;
            m_Moving = moving;
            m_Steps = 0;
            m_PreviousStick = Vector2.zero;
            ArmButtons(hand);
            SetDefinition(definition);
            if (!m_Desktop)
                StopTurning();
        }

        // Le bouton qui vient de servir (gâchette, poignée tenue en touchant la montre) doit être relâché avant de compter.
        void ArmButtons(XRBaseInputInteractor hand)
        {
            m_TriggerArmed = !IsPressed(hand.activateInput);
            m_GripArmed = !IsPressed(hand.selectInput);
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
                if (definition != null && definition.prefab != null && !definition.retired && builds.PlansOf(definition) > 0)
                    m_Available.Add(definition);
            }
        }

        // ---------------------------------------------------------------- chaque image

        void Update()
        {
            UpdateDesktopKey();
            if (!m_Placing)
            {
                if (IsMenuOpen)
                    Cancel(null);
                return;
            }

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

        // La carte suivante (1) ou précédente (-1) du menu : son bâtiment devient celui de l'hologramme, tourné pareil.
        void ScrollMenu(int direction)
        {
            if (m_Menu.Scroll(direction))
            {
                m_LastChoice = m_Menu.Current;
                SetDefinition(m_Menu.Current);
                if (m_Hand != null && !m_Desktop)
                    Haptics.Pulse(m_Hand, 0.15f, 0.02f);
            }
            else
            {
                Sfx.Play(m_ErrorClip, BodyPosition(), 0.4f);
            }
        }

        void LateUpdate()
        {
            if (!m_Placing || m_Label == null || !m_Label.gameObject.activeSelf)
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

        // Le rayon de la main : la place visée sur la grille (ou le bâtiment à améliorer), l'hologramme, son étiquette et la grille.
        void Aim()
        {
            var origin = m_Hand.transform.position;
            var direction = m_Hand.transform.forward;
            var point = origin + direction * 3f;
            var hasPoint = TryRaycast(origin, direction, out var hit);
            var grid = BuildGrid.Instance;
            var definition = m_Moving != null ? m_Moving.Definition : m_Definition;
            BuildSocket aimed = null;
            if (hasPoint)
            {
                point = hit.point;
                var structure = hit.collider.GetComponentInParent<Structure>();
                if (structure != null && structure != m_Moving && structure.Socket != null)
                    aimed = structure.Socket;
                else if (grid != null)
                    aimed = grid.OccupantAt(point);
                if (aimed != null && m_Moving != null && aimed == m_Moving.Socket)
                    aimed = null;
            }

            m_Cells.Clear();
            m_Spot = default;
            if (hasPoint && grid != null)
                m_Spot = grid.Evaluate(definition, point, m_Steps, m_Moving, m_Cells);
            Evaluate(aimed, hasPoint, definition);
            Target = m_Upgrade || m_Fuse ? aimed : m_Spot.TowerSocket;

            m_Overlay.Show(definition, m_Moving, BodyPosition());
            if (m_Spot.OnGrid && !m_Upgrade && !m_Fuse)
                m_Overlay.ShowFootprint(m_Cells, m_Spot.TowerSocket, m_Valid);
            else
                m_Overlay.HideFootprint();

            // Le duo visé : son hologramme doré, à la place qu'il prendra, au lieu de celui de la tour déplacée.
            var showDuo = m_Fuse && UpdateDuoHologram();
            if (m_DuoHologram != null)
                m_DuoHologram.SetActive(showDuo);
            if (showDuo)
            {
                if (m_Hologram != null)
                    m_Hologram.SetActive(false);
                var keep = BuildManager.DuoPlace(m_Moving, m_FuseTarget).transform;
                m_DuoHologram.transform.SetPositionAndRotation(keep.position, keep.rotation);
                SetDuoLook(m_Valid);
                ShowLabel(keep.position + Vector3.up * (m_DuoHologramTop + 0.45f), true);
                return;
            }

            if (m_Hologram == null)
                return;

            Vector3 position;
            Quaternion rotation;
            if (m_Upgrade && aimed != null && aimed.Structure != null)
            {
                position = aimed.Structure.transform.position;
                rotation = aimed.Structure.transform.rotation;
            }
            else if (m_Spot.OnGrid)
            {
                position = m_Spot.Position;
                rotation = m_Spot.Rotation;
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

        // Peut-on poser ici, et que dire au joueur ? Un bâtiment du même type visé (lui, ou une de ses cases) monte d'un niveau.
        void Evaluate(BuildSocket aimed, bool hasPoint, StructureDefinition definition)
        {
            m_Valid = false;
            m_Upgrade = false;
            var builds = BuildManager.Instance;
            var name = definition != null ? definition.displayName : "Bâtiment";
            var plans = builds != null ? builds.PlansOf(m_Definition) : 0;
            var header = m_Moving != null ? $"Déplacer : {m_Moving.DisplayName}" : $"{name} · {(plans > 1 ? plans + " plans" : "1 plan")}";

            m_Fuse = false;
            if (builds != null && builds.IsBuilding)
            {
                m_Status = $"{header}\n<color=#FF9080>Termine d'abord la construction en cours</color>";
                return;
            }

            var structure = aimed != null ? aimed.Structure : null;

            // Une tour déplacée sur une tour partenaire : le duo (31 octobre).
            if (m_Moving != null && structure != null && builds != null)
            {
                var duo = builds.DuoOf(m_Moving, structure, out var problem);
                if (duo != null)
                {
                    m_Fuse = true;
                    m_FuseEntry = duo;
                    m_FuseTarget = structure;
                    m_Valid = problem == null;
                    m_Status = !m_Valid ? $"Duo : {duo.displayName}\n<color=#FF9080>{problem}</color>"
                        : m_Desktop ? $"Duo : {duo.displayName}"
                        : $"Duo : {duo.displayName}\n<size=70%>gâchette : fusionner · poignée : annuler</size>";
                    return;
                }
            }

            // Un plan d'une de ses deux tours sur un duo le fait monter (31 octobre : 4 plans par niveau, des deux tours).
            var aimedDuo = m_Moving == null ? structure as TowerDuo : null;
            if (structure != null && m_Moving == null && (structure.Definition == m_Definition || (aimedDuo != null && aimedDuo.AcceptsPlansOf(m_Definition))))
            {
                if (structure.IsBuilt && !structure.IsUnderConstruction && structure.Level < structure.MaxLevel)
                {
                    // Deux plans aux niveaux 4 et 5 (29 octobre).
                    var next = structure.Level + 1;
                    var need = structure.Definition.PlansFor(next);
                    if (aimedDuo != null)
                    {
                        plans = builds != null ? builds.PlansOf(aimedDuo) : 0;
                        header = $"Duo {aimedDuo.DisplayName} · {plans} plans ({aimedDuo.First?.displayName}, {aimedDuo.Second?.displayName})";
                    }

                    m_Upgrade = true;
                    m_Valid = plans >= need;
                    m_Status = m_Valid ? $"{header}\nAméliorer : niveau {next}" + (need > 1 ? $" ({need} plans)" : "")
                        : $"{header}\n<color=#FF9080>Niveau {next} : il faut {need} plans</color>";
                    return;
                }

                m_Status = $"{header}\n<color=#FF9080>{(structure.IsUnderConstruction ? "Construction en cours" : "Niveau maximal atteint")}</color>";
                return;
            }

            if (!hasPoint)
            {
                m_Status = $"{header}\n<color=#FF9080>Vise le sol</color>";
                return;
            }

            if (!m_Spot.Valid)
            {
                var reason = string.IsNullOrEmpty(m_Spot.Reason) ? "Vise une zone constructible" : m_Spot.Reason;
                m_Status = $"{header}\n<color=#FF9080>{reason}</color>";
                return;
            }

            m_Valid = true;
            var where = BuildSocket.NameOf(m_Spot.Slot);
            m_Status = m_Desktop ? $"{header}\n{where}"
                : m_Moving != null ? $"{header}\n<size=70%>{where} · gâchette : poser · joystick : tourner · poignée : annuler</size>"
                : $"{header}\n<size=70%>{where} · gâchette : poser</size>";
        }

        void Place()
        {
            var builds = BuildManager.Instance;
            var grid = BuildGrid.Instance;
            if (builds == null || grid == null || !m_Valid)
            {
                Sfx.Play(m_ErrorClip, m_Hologram != null ? m_Hologram.transform.position : transform.position, 0.8f);
                return;
            }

            ShopResult result;
            if (m_Fuse)
            {
                result = builds.Fuse(m_Moving, m_FuseTarget);
            }
            else if (m_Upgrade)
            {
                result = builds.TryBuild(Target, m_Definition, m_Hand);
            }
            else
            {
                // L'emplacement naît là où se pose le bâtiment, avec ses cases ; il est rendu si la construction est refusée.
                var spot = m_Spot;
                var socket = grid.Claim(spot, m_Cells);
                var local = spot.TowerSocket != null ? spot.LocalRotation : Quaternion.identity;
                if (m_Moving != null)
                {
                    var from = m_Moving.Socket;
                    result = builds.MoveTo(m_Moving, socket, local);
                    if (result.Success && from != socket)
                        grid.Release(from);
                }
                else
                {
                    result = builds.TryBuild(socket, m_Definition, m_Hand, local);
                }

                if (!result.Success)
                    grid.Release(socket);
            }

            if (!result.Success)
            {
                Sfx.Play(m_ErrorClip, m_Hologram != null ? m_Hologram.transform.position : transform.position, 0.8f);
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

        // Le joystick de la main qui vise : un cran à chaque poussée franche (gauche, droite ; haut, bas).
        Vector2 ReadStick(out int flickX, out int flickY)
        {
            flickX = 0;
            flickY = 0;
            var stick = (m_Hand.handedness == InteractorHandedness.Left ? m_LeftStick : m_RightStick).ReadValue();
            const float press = 0.65f;
            const float release = 0.35f;
            if (Mathf.Abs(stick.x) > press && Mathf.Abs(m_PreviousStick.x) <= release)
                flickX = stick.x > 0f ? 1 : -1;
            if (Mathf.Abs(stick.y) > press && Mathf.Abs(m_PreviousStick.y) <= release)
                flickY = stick.y > 0f ? 1 : -1;
            if (Mathf.Abs(stick.x) <= release)
                m_PreviousStick.x = 0f;
            else if (Mathf.Abs(stick.x) > press)
                m_PreviousStick.x = stick.x;
            if (Mathf.Abs(stick.y) <= release)
                m_PreviousStick.y = 0f;
            else if (Mathf.Abs(stick.y) > press)
                m_PreviousStick.y = stick.y;
            return stick;
        }

        // Pendant la pose : avec le menu, gauche et droite changent de bâtiment, haut et bas le tournent ; pour un déplacement,
        // tout le joystick tourne. La gâchette pose, la poignée ferme tout.
        void ReadControllerInput()
        {
            ReadStick(out var flickX, out var flickY);
            if (IsMenuOpen)
            {
                if (flickX != 0)
                    ScrollMenu(flickX);
                if (flickY != 0)
                    Rotate(flickY);
            }
            else if (flickX != 0 || flickY != 0)
            {
                Rotate(flickX != 0 ? flickX : flickY);
            }

            if (!IsPressed(m_Hand.selectInput))
                m_GripArmed = true;
            else if (m_GripArmed && WasPressed(m_Hand.selectInput))
            {
                Cancel(m_Moving != null ? "Déplacement annulé" : null);
                return;
            }

            if (!IsPressed(m_Hand.activateInput))
                m_TriggerArmed = true;
            else if (m_TriggerArmed && WasPressed(m_Hand.activateInput))
                Place();
        }

        // Sans casque : la molette change de carte (Maj : tourne ; sans menu, elle tourne), E pose.
        void ReadDesktopInput()
        {
            var desktop = DesktopControls.Instance;
            if (desktop == null || !desktop.HasFocus)
                return;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(scroll) > 0.1f)
            {
                var direction = scroll > 0f ? 1 : -1;
                if (IsMenuOpen && (keyboard == null || !keyboard.shiftKey.isPressed))
                    ScrollMenu(-direction);
                else
                    Rotate(direction);
            }

            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                Place();
        }

        // Sans casque : F2 ouvre le menu avec l'hologramme ; ouvert (ou pendant un déplacement), il ferme tout.
        void UpdateDesktopKey()
        {
            var desktop = DesktopControls.Instance;
            var keyboard = Keyboard.current;
            if (desktop == null || !desktop.IsActive || !desktop.HasFocus || keyboard == null || !keyboard.f2Key.wasPressedThisFrame)
                return;

            if (IsActive)
            {
                Cancel(m_Moving != null ? "Déplacement annulé" : null);
                return;
            }

            var hand = FindAnyObjectByType<DesktopHand>();
            if (!OpenMenu(hand))
                Sfx.Play(m_ErrorClip, desktop.CameraTransform != null ? desktop.CameraTransform.position : transform.position, 0.6f);
        }

        static bool IsPressed(XRInputButtonReader reader) => reader != null && reader.ReadIsPerformed();

        static bool WasPressed(XRInputButtonReader reader) => reader != null && reader.ReadWasPerformedThisFrame();

        // Pendant le menu et la pose, le joystick fait défiler ou tourne le bâtiment : la rotation du joueur au joystick est
        // coupée, puis rendue.
        void StopTurning()
        {
            if (m_DisabledTurns.Count > 0)
                return;

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

        // Le centre de la grille dessinée : le joueur (sans casque, la caméra).
        Vector3 BodyPosition()
        {
            var rig = PlayerRig.Instance;
            if (rig != null && !m_Desktop)
                return rig.BodyPosition;
            var desktop = DesktopControls.Instance;
            if (desktop != null && desktop.CameraTransform != null)
                return desktop.CameraTransform.position;
            return m_Hand != null ? m_Hand.transform.position : transform.position;
        }

        static XRInputValueReader<Vector2> Stick(string name, string bindingPath)
        {
            var reader = new XRInputValueReader<Vector2>(name, XRInputValueReader.InputSourceMode.InputAction);
            reader.inputAction.AddBinding(bindingPath);
            return reader;
        }

        // ---------------------------------------------------------------- hologramme et étiquette

        /// <summary>
        /// Une copie des maillages visibles du bâtiment (au niveau donné), sans rien d'autre (ni comportement, ni collider, ni son) :
        /// elle ne tire pas, ne se vise pas et ne gêne pas le rayon. Avec ce matériau partout (le plan bleu de l'hologramme), ou
        /// ses matériaux à lui si <paramref name="material"/> est null (l'aperçu du menu des plans, 28 octobre).
        /// </summary>
        internal static GameObject CopyModel(Structure source, int level, Material material) => CopyModel(source, level, material, out _);

        static GameObject CopyModel(Structure source, int level, Material material, out float top)
        {
            var root = new GameObject((material != null ? "Hologramme · " : "Aperçu · ") +
                                      (source.Definition != null ? source.Definition.displayName : source.name));
            var sourceRoot = source.transform;
            root.transform.localScale = sourceRoot.localScale;
            var toLocal = sourceRoot.worldToLocalMatrix;
            top = 0.5f;
            foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || !renderer.enabled || !source.IsPreviewPart(renderer) ||
                    !source.IsShownAtLevel(renderer.transform, level))
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
                if (material != null)
                {
                    var materials = new Material[Mathf.Max(1, mesh.subMeshCount)];
                    for (var i = 0; i < materials.Length; i++)
                        materials[i] = material;
                    meshRenderer.sharedMaterials = materials;
                }
                else
                {
                    meshRenderer.sharedMaterials = renderer.sharedMaterials;
                }

                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;

                // Le haut de la copie, pour y poser l'étiquette.
                var corner = matrix.MultiplyPoint3x4(mesh.bounds.center) + Vector3.up * mesh.bounds.extents.y * Mathf.Abs(matrix.lossyScale.y);
                top = Mathf.Max(top, corner.y);
            }

            return root;
        }

        // L'hologramme de pose : le bâtiment au niveau donné, en plan bleu.
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

            var root = CopyModel(source, level, m_Material, out var top);
            m_HologramTop = top;
            return root;
        }

        // L'hologramme doré du duo visé (31 octobre) : son prefab, chaque tour au niveau qu'elle a ; refait si le duo ou les niveaux
        // changent. Faux s'il n'y en a pas.
        bool UpdateDuoHologram()
        {
            var prefab = m_FuseEntry != null && m_FuseEntry.definition != null ? m_FuseEntry.definition.prefab : null;
            if (prefab == null || m_Moving == null || m_FuseTarget == null)
                return false;

            var level = BuildManager.DuoLevelFrom(m_Moving, m_FuseTarget);
            if (m_DuoHologram != null && m_DuoHologramEntry == m_FuseEntry && m_DuoHologramLevel == level)
                return true;

            if (m_DuoHologram != null)
                Destroy(m_DuoHologram);
            var builds = BuildManager.Instance;
            var blueprint = builds != null ? builds.BlueprintMaterial : null;
            if (m_DuoMaterial == null && blueprint != null)
            {
                m_DuoMaterial = new Material(blueprint) { name = "Hologramme de duo" };
                m_DuoShowsValid = false;
                SetDuoLook(true);
            }

            m_DuoHologram = CopyModel(prefab, level, m_DuoMaterial, out m_DuoHologramTop);
            m_DuoHologramEntry = m_FuseEntry;
            m_DuoHologramLevel = level;
            return true;
        }

        void SetDuoLook(bool valid)
        {
            if (m_DuoMaterial == null || valid == m_DuoShowsValid)
                return;

            m_DuoShowsValid = valid;
            m_DuoMaterial.SetColor(k_BaseColorId, valid ? m_DuoColor : m_InvalidColor);
            m_DuoMaterial.SetColor(k_FilledColorId, valid ? m_DuoColor : m_InvalidColor);
            m_DuoMaterial.SetColor(k_EdgeColorId, valid ? m_DuoEdgeColor : m_InvalidEdgeColor);
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
