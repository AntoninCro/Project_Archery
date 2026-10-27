using System.Collections;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Chests;
using Archery.Defense;
using Archery.Enemies;
using Archery.Player;
using Archery.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;

namespace Archery.Building
{
    /// <summary>
    /// Les zones constructibles (refonte du tower defense, étape 3, 27 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, partie 3).
    /// Au lancement, la carte est découpée en cases de 3 m (celles de la carte, bords sur les multiples de 3 m), et chaque case
    /// de sol plat près d'une voie devient un emplacement de construction (<see cref="BuildSocket"/>) d'une de ces sortes :
    /// <list type="bullet">
    /// <item>sur la voie (barricade, piques), sur le chemin des ennemis, l'avant tourné vers la forêt ;</item>
    /// <item>au bord de la voie (baliste, tour de givre), à côté, à peu près au même niveau, tourné vers la voie ;</item>
    /// <item>en hauteur (les mêmes), sur une terrasse ou une falaise au-dessus d'une voie, tourné vers elle.</item>
    /// </list>
    /// Pas dans l'eau, ni au pied de la tour, ni sous un arbre ou un rocher, ni près des points d'apparition (ennemis, caisses,
    /// coffres). Les emplacements sont invisibles : pendant la pose d'un plan ou le déplacement d'un bâtiment
    /// (<see cref="BuildPlacement"/>), les runes de ceux où il peut aller s'allument autour du joueur, dans la couleur de leur
    /// sorte, et l'hologramme se cale sur celui que vise le rayon (<see cref="SocketAt"/>).
    /// </summary>
    /// <remarks>
    /// Créée toute seule par le <see cref="BuildManager"/> : rien à poser dans la scène (posée à la main sur l'objet <c>Game</c>,
    /// ses réglages comptent). Au lancement, les 12 socles posés à la main et les 3 barricades de départ sont désactivés (la
    /// scène n'est pas modifiée : deux cases à décocher pour les garder). La recherche se fait sur plusieurs images.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BuildGrid : MonoBehaviour
    {
        static readonly RaycastHit[] s_Hits = new RaycastHit[32];

        // Les modèles de la carte qui ne sont pas du sol : on ne construit pas dessus (les arbres : MapScan.IsPlant).
        static readonly string[] k_ObstaclePrefixes =
            { "rock", "stump", "log", "fence", "statue", "tent", "campfire", "pot_", "sign", "crop", "barrel", "crate", "chest" };

        [Header("Grille")]
        [Tooltip("Taille (m) d'une case : celle de la carte (3 m), et la taille d'un emplacement.")]
        [SerializeField]
        float m_CellSize = 3f;

        [Tooltip("Écart de hauteur (m) admis sous un emplacement : au-delà, le sol n'est pas plat (marche, pente, trou).")]
        [SerializeField]
        float m_MaxStep = 0.35f;

        [Header("Sortes d'emplacement")]
        [Tooltip("Une case dont le centre est à moins de cette distance (m) du chemin des ennemis est sur la voie (voies de 5 m).")]
        [SerializeField]
        float m_LaneHalfWidth = 3f;

        [Tooltip("Largeur (m) de la bande au bord de la voie, après la voie.")]
        [SerializeField]
        float m_RoadsideWidth = 4.5f;

        [Tooltip("Une case plus haute que la voie d'au moins cette hauteur (m) est en hauteur.")]
        [SerializeField]
        float m_HeightMin = 2f;

        [Tooltip("Distance (m) au plus entre une case en hauteur et le chemin des ennemis.")]
        [SerializeField]
        float m_HeightReach = 15f;

        [Header("Interdits")]
        [Tooltip("Pas d'emplacement à moins de cette distance (m) de la tour (le téléporteur, l'escalier).")]
        [SerializeField]
        float m_TowerClearance = 7f;

        [Tooltip("Pas d'emplacement à moins de cette distance (m) d'un point d'apparition des ennemis.")]
        [SerializeField]
        float m_SpawnClearance = 6f;

        [Tooltip("Pas d'emplacement à moins de cette distance (m) d'un emplacement de caisse ou de coffre.")]
        [SerializeField]
        float m_SpotClearance = 2.5f;

        [Header("Ancien système")]
        [Tooltip("Désactive au lancement les socles posés à la main dans la scène (les 12 d'avant le 27 octobre) qui n'ont pas de bâtiment.")]
        [SerializeField]
        bool m_RemoveAuthoredSockets = true;

        [Tooltip("Désactive au lancement les barricades de départ (refonte du 27 octobre : la tour n'a que ce que le joueur construit).")]
        [SerializeField]
        bool m_RemoveStartBarricades = true;

        readonly List<BuildSocket> m_Sockets = new List<BuildSocket>();
        readonly Dictionary<Vector2Int, BuildSocket> m_Cells = new Dictionary<Vector2Int, BuildSocket>();
        readonly List<Vector3> m_SpawnPoints = new List<Vector3>();
        readonly List<Vector3> m_Spots = new List<Vector3>();
        List<MapScan.Lane> m_Lanes = new List<MapScan.Lane>();
        Transform m_Container;
        int m_TemplateLayer;
        InteractionLayerMask m_TemplateInteractionLayers = 1;
        bool m_TemplateTrigger;

        public static BuildGrid Instance { get; private set; }

        /// <summary>La recherche des emplacements est finie.</summary>
        public bool IsReady { get; private set; }

        /// <summary>Les emplacements de la grille.</summary>
        public IReadOnlyList<BuildSocket> Sockets => m_Sockets;

        /// <summary>L'emplacement de la case qui contient ce point (le rayon de la pose touche le sol), ou null.</summary>
        public BuildSocket SocketAt(Vector3 point)
        {
            if (!IsReady)
                return null;

            return m_Cells.TryGetValue(CellOf(point), out var socket) && socket != null && socket.isActiveAndEnabled ? socket : null;
        }

        Vector2Int CellOf(Vector3 point) => new Vector2Int(Mathf.FloorToInt(point.x / m_CellSize), Mathf.FloorToInt(point.z / m_CellSize));

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Build Grid dans la scène.", this);
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start() => StartCoroutine(Build());

        IEnumerator Build()
        {
            // Une image : la carte, le NavMesh et les autres composants sont prêts.
            yield return null;

            RemoveOldSystem();
            m_Lanes = MapScan.RunLanes().Lanes;
            if (m_Lanes.Count == 0)
            {
                Debug.LogWarning("BuildGrid : aucun chemin d'ennemis trouvé (Enemy Spawner, tour, NavMesh) : pas d'emplacement de construction.", this);
                yield break;
            }

            CollectForbiddenPoints();
            m_Container = new GameObject("Emplacements de construction (grille)").transform;

            // Le rectangle autour des voies, assez large pour les cases en hauteur, calé sur la grille de la carte.
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var lane in m_Lanes)
            {
                foreach (var point in lane.Points)
                {
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }
            }

            var margin = m_LaneHalfWidth + Mathf.Max(m_RoadsideWidth, m_HeightReach);
            var fromX = Mathf.Floor((min.x - margin) / m_CellSize) * m_CellSize + m_CellSize * 0.5f;
            var fromZ = Mathf.Floor((min.z - margin) / m_CellSize) * m_CellSize + m_CellSize * 0.5f;
            var top = max.y + 40f;
            var depth = top - min.y + 40f;

            var counts = new int[4];
            var checkedCells = 0;
            for (var x = fromX; x <= max.x + margin; x += m_CellSize)
            {
                for (var z = fromZ; z <= max.z + margin; z += m_CellSize)
                {
                    if (TryClassify(new Vector3(x, top, z), depth, out var ground, out var rotation, out var slot))
                    {
                        CreateSocket(ground, rotation, slot);
                        counts[(int)slot]++;
                    }

                    // Quelques dizaines de cases par image : pas d'à-coup au lancement.
                    if (++checkedCells % 48 == 0)
                        yield return null;
                }
            }

            IsReady = true;
            Debug.Log($"BuildGrid : {m_Sockets.Count} emplacements de construction ({counts[(int)BuildSlot.Lane]} sur la voie, " +
                      $"{counts[(int)BuildSlot.Roadside]} au bord, {counts[(int)BuildSlot.Height]} en hauteur), sur {checkedCells} cases de " +
                      $"{m_CellSize} m.", this);
        }

        // Les 12 socles posés à la main (sauf ceux qui portent déjà un bâtiment) et les barricades de départ. Le premier socle sert
        // de modèle aux emplacements : sa couche, ses couches d'interaction, sa plaque.
        void RemoveOldSystem()
        {
            var authored = new List<BuildSocket>(BuildSocket.All);
            var hasTemplate = false;
            foreach (var socket in authored)
            {
                if (socket == null || socket.IsCandidate)
                    continue;

                if (!hasTemplate && socket.colliders.Count > 0 && socket.colliders[0] != null)
                {
                    hasTemplate = true;
                    m_TemplateLayer = socket.gameObject.layer;
                    m_TemplateInteractionLayers = socket.interactionLayers;
                    m_TemplateTrigger = socket.colliders[0].isTrigger;
                }

                if (m_RemoveAuthoredSockets && socket.Structure == null)
                    socket.gameObject.SetActive(false);
            }

            if (!m_RemoveStartBarricades)
                return;

            foreach (var barricade in FindObjectsByType<Barricade>())
            {
                if (barricade != null && barricade.GetComponentInParent<Structure>() == null)
                    barricade.gameObject.SetActive(false);
            }
        }

        // Les points d'apparition (ennemis, caisses de l'ingénieur, coffres) : on n'y construit pas.
        void CollectForbiddenPoints()
        {
            m_SpawnPoints.Clear();
            m_Spots.Clear();
            var enemies = FindAnyObjectByType<EnemySpawner>();
            if (enemies != null)
            {
                foreach (var point in enemies.SpawnPoints)
                {
                    if (point != null)
                        m_SpawnPoints.Add(point.position);
                }
            }

            var crates = CrystalSpawner.Instance;
            if (crates != null)
            {
                foreach (var spot in crates.Spots)
                {
                    if (spot != null)
                        m_Spots.Add(spot.position);
                }
            }

            var chests = FindAnyObjectByType<ChestSpawner>();
            if (chests != null)
            {
                foreach (var spot in chests.SpawnPoints)
                {
                    if (spot != null)
                        m_Spots.Add(spot.position);
                }
            }
        }

        // Une case : son sol (le plus haut, plat, sans obstacle), sa sorte d'emplacement d'après la voie la plus proche, et le sens
        // de l'emplacement.
        bool TryClassify(Vector3 top, float depth, out Vector3 ground, out Quaternion rotation, out BuildSlot slot)
        {
            rotation = Quaternion.identity;
            slot = BuildSlot.Roadside;
            if (!TryGround(top, depth, out ground))
                return false;

            // Le sol doit être plat sous toute la case : les quatre coins au même niveau, sans arbre ni rocher.
            var half = m_CellSize * 0.4f;
            for (var i = 0; i < 4; i++)
            {
                var corner = new Vector3(top.x + (i < 2 ? -half : half), top.y, top.z + (i % 2 == 0 ? -half : half));
                if (!TryGround(corner, depth, out var cornerGround) || Mathf.Abs(cornerGround.y - ground.y) > m_MaxStep)
                    return false;
            }

            if (SlowWater.InAnyWater(ground + Vector3.up * 0.3f) || IsNear(ground, TowerPosition(), m_TowerClearance) ||
                IsNearAny(ground, m_SpawnPoints, m_SpawnClearance) || IsNearAny(ground, m_Spots, m_SpotClearance))
                return false;

            // La voie la plus proche : sa distance, sa hauteur et son sens à cet endroit.
            var distance = float.MaxValue;
            var lanePoint = ground;
            var laneDirection = Vector3.forward;
            foreach (var lane in m_Lanes)
            {
                var d = lane.DistanceTo(ground, out var along);
                if (d >= distance)
                    continue;

                distance = d;
                lanePoint = lane.PointAt(along, out laneDirection);
            }

            var rise = ground.y - lanePoint.y;
            var toLane = lanePoint - ground;
            toLane.y = 0f;
            var facingLane = toLane.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toLane) : Quaternion.identity;

            if (distance <= m_LaneHalfWidth && Mathf.Abs(rise) <= 1.2f && IsOnNavMesh(ground))
            {
                // Sur la voie : l'avant vers la forêt, d'où viennent les ennemis (le chemin va de leur apparition à la tour).
                slot = BuildSlot.Lane;
                laneDirection.y = 0f;
                rotation = laneDirection.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(-laneDirection) : Quaternion.identity;
                return true;
            }

            if (distance > m_LaneHalfWidth && distance <= m_LaneHalfWidth + m_RoadsideWidth && Mathf.Abs(rise) <= 1.5f)
            {
                slot = BuildSlot.Roadside;
                rotation = facingLane;
                return true;
            }

            if (rise >= m_HeightMin && distance <= m_HeightReach)
            {
                slot = BuildSlot.Height;
                rotation = facingLane;
                return true;
            }

            return false;
        }

        // Le plus haut sol touché sous ce point : pas les déclencheurs, ni ce que les flèches traversent, ni les plaques des autres
        // emplacements. Faux s'il n'y a rien, si c'est un obstacle (arbre, rocher, mannequin…) ou une pente.
        bool TryGround(Vector3 top, float depth, out Vector3 ground)
        {
            ground = top;
            var count = Physics.RaycastNonAlloc(top, Vector3.down, s_Hits, depth, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            Collider hitCollider = null;
            var normal = Vector3.up;
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if (hit.distance >= best || ArrowIgnore.IsIgnored(hit.collider) || hit.collider.GetComponentInParent<BuildSocket>() != null)
                    continue;

                best = hit.distance;
                ground = hit.point;
                hitCollider = hit.collider;
                normal = hit.normal;
            }

            return hitCollider != null && normal.y >= 0.85f && !IsObstacle(hitCollider);
        }

        static bool IsObstacle(Collider collider)
        {
            if (collider.GetComponentInParent<Enemy>() != null || collider.GetComponentInParent<Structure>() != null ||
                MapScan.IsPlant(collider.transform))
                return true;

            var transform = collider.transform;
            for (var i = 0; i < 3 && transform != null; i++, transform = transform.parent)
            {
                foreach (var prefix in k_ObstaclePrefixes)
                {
                    if (transform.name.StartsWith(prefix))
                        return true;
                }
            }

            return false;
        }

        static bool IsOnNavMesh(Vector3 point)
        {
            if (!NavMesh.SamplePosition(point, out var hit, 1.5f, NavMesh.AllAreas))
                return false;

            var offset = hit.position - point;
            offset.y = 0f;
            return offset.magnitude < 1.2f;
        }

        void CreateSocket(Vector3 ground, Quaternion rotation, BuildSlot slot)
        {
            // Monté désactivé : XRI ne lit la liste des colliders qu'au réveil, et ne reprend pas tout seul une plaque en
            // déclencheur (comme celle des socles posés à la main, si c'en est une) ; elle y est mise avant.
            var go = new GameObject("Emplacement · " + BuildSocket.NameOf(slot));
            go.SetActive(false);
            go.layer = m_TemplateLayer;
            go.transform.SetParent(m_Container, false);
            go.transform.SetPositionAndRotation(ground, rotation);

            // La plaque visée par le rayon de la main, au ras du sol (les flèches la traversent : BuildSocket.Start).
            var plate = go.AddComponent<BoxCollider>();
            plate.size = new Vector3(m_CellSize * 0.95f, 0.1f, m_CellSize * 0.95f);
            plate.center = Vector3.up * 0.05f;
            plate.isTrigger = m_TemplateTrigger;

            var socket = go.AddComponent<BuildSocket>();
            socket.colliders.Add(plate);
            socket.interactionLayers = m_TemplateInteractionLayers;
            socket.SetupCandidate(slot, new Vector2(m_CellSize, m_CellSize));
            go.SetActive(true);
            m_Sockets.Add(socket);
            m_Cells[CellOf(ground)] = socket;
        }

        /// <summary>
        /// Des emplacements libres de cette sorte, les plus proches de la tour d'abord, à au moins <paramref name="spacing"/> m les uns
        /// des autres (raccourci F de la démo).
        /// </summary>
        public List<BuildSocket> PickSpread(BuildSlot slot, int count, float spacing)
        {
            var tower = TowerPosition();
            var candidates = new List<BuildSocket>();
            foreach (var socket in m_Sockets)
            {
                if (socket != null && socket.isActiveAndEnabled && socket.Slot == slot && socket.Structure == null)
                    candidates.Add(socket);
            }

            candidates.Sort((a, b) => (a.transform.position - tower).sqrMagnitude.CompareTo((b.transform.position - tower).sqrMagnitude));
            var picked = new List<BuildSocket>();
            foreach (var socket in candidates)
            {
                if (picked.Count >= count)
                    break;

                var free = true;
                foreach (var other in picked)
                {
                    if (IsNear(socket.transform.position, other.transform.position, spacing))
                    {
                        free = false;
                        break;
                    }
                }

                if (free)
                    picked.Add(socket);
            }

            return picked;
        }

        static bool IsNear(Vector3 a, Vector3 b, float distance)
        {
            var offset = a - b;
            offset.y = 0f;
            return offset.sqrMagnitude < distance * distance;
        }

        static bool IsNearAny(Vector3 point, List<Vector3> others, float distance)
        {
            foreach (var other in others)
            {
                if (IsNear(point, other, distance))
                    return true;
            }

            return false;
        }

        Vector3 TowerPosition()
        {
            var tower = Tower.Instance;
            return tower != null ? tower.transform.position : transform.position;
        }
    }
}
