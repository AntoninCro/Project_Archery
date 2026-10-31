using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Building
{
    /// <summary>
    /// Un emplacement de construction (GDD, section 9). On le vise et on appuie sur la gâchette de prise, à quelques
    /// mètres au plus : le <see cref="BuildPanel"/> propose les bâtiments possibles, ou l'amélioration de celui qui
    /// est déjà construit. Le bâtiment se pose au centre du socle, tourné comme lui.
    /// </summary>
    /// <remarks>
    /// Le socle a besoin d'un collider plat à sa racine (la plaque visée par le rayon de la main). Une fois construit,
    /// les colliders du bâtiment s'y ajoutent : viser le bâtiment ouvre aussi le panneau.
    /// L'avant du socle (axe Z, bleu) compte pour les barricades : il doit pointer vers la forêt, d'où viennent les ennemis.
    /// Depuis la refonte du 27 octobre (étape 3), les 12 socles posés à la main sont remplacés par les emplacements de la
    /// <see cref="BuildGrid"/> (<see cref="IsCandidate"/>). Depuis le 28 octobre, un emplacement de la grille naît à la pose
    /// d'un bâtiment, sur les cases qu'il occupe (<see cref="BuildPlacement"/>), et disparaît quand on le déplace ; seuls les
    /// petits emplacements de la tour sont faits d'avance. Vides, ils sont invisibles et ne se visent pas. Construits, ils se
    /// visent comme avant (panneau : améliorer, déplacer).
    /// </remarks>
    [DisallowMultipleComponent]
    public class BuildSocket : XRBaseInteractable
    {
        static readonly List<BuildSocket> s_All = new List<BuildSocket>();
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        [Tooltip("Sur la voie : barricade, pièges à piques. Au bord de la voie : baliste, tour de givre.")]
        [SerializeField]
        BuildSlot m_Slot;

        [Tooltip("Optionnel : repères visibles du socle (les coins du kit), cachés une fois le bâtiment construit.")]
        [SerializeField]
        Transform m_Marker;

        [Tooltip("Distance maximale (m) entre la main et le socle pour ouvrir le panneau en le visant.")]
        [SerializeField]
        float m_MaxReach = 5f;

        [Tooltip("Taille de l'emplacement au sol (m), en X et en Z : sert au gizmo et à la vérification du sol au lancement.")]
        [SerializeField]
        Vector2 m_Size = new Vector2(3f, 3f);

        [Tooltip("Optionnel : nom affiché dans le panneau. Vide : « Sur la voie » ou « Au bord de la voie ».")]
        [SerializeField]
        string m_DisplayName;

        Vector3 m_MarkerScale = Vector3.one;
        Collider[] m_PlateColliders = System.Array.Empty<Collider>();
        float m_Phase;

        /// <summary>Tous les socles de la scène.</summary>
        public static IReadOnlyList<BuildSocket> All => s_All;

        public BuildSlot Slot => m_Slot;

        /// <summary>Le bâtiment construit (ou en construction) sur ce socle, ou null.</summary>
        public Structure Structure { get; private set; }

        public string DisplayName => !string.IsNullOrEmpty(m_DisplayName) ? m_DisplayName : NameOf(m_Slot);

        /// <summary>Une construction est en cours sur ce socle.</summary>
        public bool IsBusy => Structure != null && Structure.IsUnderConstruction;

        /// <summary>Emplacement créé par la <see cref="BuildGrid"/> (refonte du 27 octobre), et non socle posé à la main.</summary>
        public bool IsCandidate { get; private set; }

        /// <summary>Visé par le rayon de la main, ou par l'hologramme de la pose : ses runes grossissent et brillent plus.</summary>
        public bool IsHighlighted => isHovered || (BuildPlacement.Instance != null && BuildPlacement.Instance.Target == this);

        /// <summary>
        /// Les runes de l'emplacement s'affichent (<c>MapGameplayLights</c>) : pour un socle posé à la main seulement. Depuis le
        /// 28 octobre, les emplacements de la grille n'en ont plus : la grille dessinée au sol pendant la pose les remplace
        /// (<see cref="BuildGridOverlay"/>).
        /// </summary>
        public bool ShowsRune => !IsCandidate;

        /// <summary>
        /// La couleur des runes d'un emplacement de la grille, selon sa sorte : orange sur la voie, vert en hauteur, doré sur la
        /// tour ; null (le bleu habituel des runes) au bord de la voie et pour un socle posé à la main.
        /// </summary>
        public Color? RuneColor => !IsCandidate ? (Color?)null : m_Slot switch
        {
            BuildSlot.Lane => new Color(1f, 0.55f, 0.2f),
            BuildSlot.Height => new Color(0.4f, 1f, 0.45f),
            BuildSlot.Tower => new Color(1f, 0.85f, 0.35f),
            _ => null,
        };

        public static string NameOf(BuildSlot slot) => slot switch
        {
            BuildSlot.Lane => "Sur la voie",
            BuildSlot.Height => "En hauteur",
            BuildSlot.Tower => "Sur la tour",
            _ => "Au bord de la voie",
        };

        protected override void Awake()
        {
            base.Awake();
            if (colliders.Count == 0)
                Debug.LogWarning("BuildSocket : ajoute un collider plat à la racine du socle, sinon on ne peut pas le viser.", this);
            if (m_Marker != null)
                m_MarkerScale = m_Marker.localScale;
            m_Phase = Random.Range(0f, 6f);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!s_All.Contains(this))
                s_All.Add(this);
        }

        protected override void OnDisable()
        {
            s_All.Remove(this);
            base.OnDisable();
        }

        void Start()
        {
            // Les flèches ne se plantent pas dans la plaque du socle : elle est au ras du sol.
            m_PlateColliders = colliders.ToArray();
            ArrowIgnore.Register(m_PlateColliders);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Les emplacements de la grille ont déjà été choisis sur un sol plat.
            if (!IsCandidate)
                CheckGround();
#endif
        }

        protected override void OnDestroy()
        {
            ArrowIgnore.Unregister(m_PlateColliders);
            base.OnDestroy();
        }

        /// <summary>En fait un emplacement de la grille, de cette sorte et de cette taille. Appelé par la grille, juste après sa création.</summary>
        internal void SetupCandidate(BuildSlot slot, Vector2 size)
        {
            IsCandidate = true;
            m_Slot = slot;
            m_Size = size;
        }

        void Update()
        {
            if (m_Marker == null || !m_Marker.gameObject.activeSelf)
                return;

            // Les repères respirent doucement, et grossissent quand le rayon de la main les vise.
            var target = isHovered ? 1.12f : 1f + 0.03f * Mathf.Sin((Time.time + m_Phase) * 2f);
            var current = m_Marker.localScale.x / Mathf.Max(0.0001f, m_MarkerScale.x);
            m_Marker.localScale = m_MarkerScale * Mathf.Lerp(current, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        // Un emplacement vide de la grille ne se vise pas au rayon (on y pose un hologramme) : sinon le rayon de la main
        // s'accrocherait au sol partout.
        public override bool IsHoverableBy(IXRHoverInteractor interactor) =>
            base.IsHoverableBy(interactor) && (!IsCandidate || Structure != null);

        public override bool IsSelectableBy(IXRSelectInteractor interactor)
        {
            if (!base.IsSelectableBy(interactor) || IsBusy || !IsInReach(interactor) || (IsCandidate && Structure == null))
                return false;

            // Pendant la pose d'un plan, la poignée annule la pose : elle n'ouvre pas de panneau.
            var placement = BuildPlacement.Instance;
            if (placement != null && placement.IsActive)
                return false;

            // Le panneau ouvert sur ce socle le relâche aussitôt : la main reste libre pour cliquer.
            var panel = BuildPanel.Instance;
            if (panel != null && panel.Socket == this)
                return false;

            var manager = BuildManager.Instance;
            return manager == null || !manager.IsBuilding;
        }

        // La main (la manette, pas le point visé) doit être à moins de quelques mètres du socle ou du bâtiment.
        bool IsInReach(IXRInteractor interactor)
        {
            var hand = interactor.transform.position;
            var reach = m_MaxReach * m_MaxReach;
            foreach (var collider in colliders)
            {
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy &&
                    (collider.ClosestPoint(hand) - hand).sqrMagnitude <= reach)
                    return true;
            }

            return false;
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            Haptics.Pulse(args.interactorObject, 0.3f, 0.05f);

            var panel = BuildPanel.Instance;
            if (panel != null)
                panel.Open(this, args.interactorObject as XRBaseInputInteractor);
            else
                Debug.LogWarning("BuildSocket : aucun Build Panel dans la scène.", this);
        }

        /// <summary>Pose le bâtiment sur le socle (en plan, avant sa construction).</summary>
        internal Structure Place(StructureDefinition definition)
        {
            var structure = Instantiate(definition.prefab, transform.position, transform.rotation, transform);
            structure.name = definition.prefab.name;
            structure.Setup(definition, this);
            Attach(structure);
            return structure;
        }

        /// <summary>
        /// Le bâtiment se pose sur ce socle, centré et tourné comme lui (construction, ou déplacement depuis un autre socle).
        /// Ses colliders rejoignent ceux du socle : le viser ouvre le panneau d'amélioration.
        /// </summary>
        internal void Attach(Structure structure)
        {
            structure.transform.SetParent(transform, false);
            structure.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Structure = structure;
            if (m_Marker != null)
                m_Marker.gameObject.SetActive(false);

            // XRI ne relit la liste qu'à l'enregistrement, d'où la désactivation le temps de l'ajout.
            enabled = false;
            foreach (var collider in structure.GetComponentsInChildren<Collider>(true))
            {
                if (!colliders.Contains(collider))
                    colliders.Add(collider);
            }

            enabled = true;
        }

        /// <summary>Le bâtiment quitte ce socle (déplacement) : ses colliders aussi, et le socle redevient libre.</summary>
        internal Structure Detach()
        {
            var structure = Structure;
            if (structure == null)
                return null;

            enabled = false;
            foreach (var collider in structure.GetComponentsInChildren<Collider>(true))
                colliders.Remove(collider);
            enabled = true;

            Structure = null;
            if (m_Marker != null)
                m_Marker.gameObject.SetActive(true);
            return structure;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Prévient dans la console si le sol sous l'emplacement n'est pas plat (marche, pente, trou).
        void CheckGround()
        {
            var lowest = float.PositiveInfinity;
            var highest = float.NegativeInfinity;
            for (var x = -1; x <= 1; x++)
            {
                for (var z = -1; z <= 1; z++)
                {
                    var local = new Vector3(x * m_Size.x * 0.45f, 0f, z * m_Size.y * 0.45f);
                    var origin = transform.TransformPoint(local) + Vector3.up * 3f;
                    if (!TryGetGround(origin, out var height))
                    {
                        Debug.LogWarning($"BuildSocket « {name} » : pas de sol sous un coin de l'emplacement.", this);
                        return;
                    }

                    lowest = Mathf.Min(lowest, height);
                    highest = Mathf.Max(highest, height);
                }
            }

            var y = transform.position.y;
            if (highest - lowest > 0.35f || Mathf.Abs(highest - y) > 0.35f || Mathf.Abs(lowest - y) > 0.35f)
                Debug.LogWarning($"BuildSocket « {name} » : le sol n'est pas plat sous l'emplacement " +
                                 $"(de {lowest - y:+0.00;-0.00} à {highest - y:+0.00;-0.00} m par rapport au socle).", this);
        }

        bool TryGetGround(Vector3 origin, out float height)
        {
            height = 0f;
            var count = Physics.RaycastNonAlloc(origin, Vector3.down, s_Hits, 8f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if (hit.collider.transform.IsChildOf(transform) || ArrowIgnore.IsIgnored(hit.collider) || hit.distance >= best)
                    continue;
                best = hit.distance;
                height = hit.point.y;
            }

            return best < float.PositiveInfinity;
        }
#endif

        void OnDrawGizmos()
        {
            Gizmos.color = m_Slot switch
            {
                BuildSlot.Lane => new Color(1f, 0.6f, 0.2f, 0.8f),
                BuildSlot.Height => new Color(0.4f, 1f, 0.4f, 0.8f),
                _ => new Color(0.3f, 0.85f, 1f, 0.8f),
            };
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.up * 0.05f, new Vector3(m_Size.x, 0.1f, m_Size.y));
            Gizmos.DrawLine(Vector3.up * 0.1f, new Vector3(0f, 0.1f, m_Size.y * 0.5f + 1f));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_All.Clear();
    }
}
