using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Archery.Bows;
using Archery.Defense;
using Archery.Enemies;
using Archery.Player;
using Archery.Training;
using Archery.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;
using Debug = UnityEngine.Debug;

namespace Archery.Building
{
    /// <summary>
    /// Les zones constructibles (refonte du tower defense, étape 3, 27 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, partie 3).
    /// Au lancement, la carte est découpée en petites cases (1 m depuis les retours du joueur du 28 octobre : 3 m avant, des
    /// bâtiments dépassaient sur la voie), et chaque case de sol plat près d'une voie est classée :
    /// <list type="bullet">
    /// <item>sur la voie (barricade, piques), sur la terre de la voie (lue dans le matériau du sol depuis le 28 octobre au soir :
    /// le chemin des ennemis coupe les virages ; depuis le 2 novembre, seulement celle des tuiles des voies, rangées sous
    /// « Lanes », et des passages de la rivière, et toute la case : les voies restent droites), l'avant tourné vers la forêt ;</item>
    /// <item>au bord de la voie (baliste, tour de givre), tout sol plat au niveau des voies, tourné vers la voie la plus proche
    /// (jusqu'au 2 novembre au soir, à moins de 4,5 m des cases de la voie, puis de 12 m : les clairières étaient refusées) ; les
    /// demi-cases du bord de la voie du milieu, mi-terre mi-herbe, en sont depuis le 2 novembre au soir (de la clairière) ;</item>
    /// <item>en hauteur (les mêmes), sur une terrasse de 1,5 à 7,5 m plus haute que la voie (pas sur les montagnes), tournée vers
    /// elle (jusqu'au 2 novembre au soir, à 15 m au plus du chemin des ennemis).</item>
    /// </list>
    /// Pas au pied de la tour, ni sous un arbre ou un grand rocher (les cailloux et les mannequins d'entraînement disparaissent sous
    /// un bâtiment posé dessus, depuis le 2 novembre), ni près des points d'apparition (ennemis, caisses, coffres). L'habillage des
    /// falaises (faces et rebords des kits, posés devant les blocs du terrain) ne compte pas depuis le 2 novembre : il débordait en
    /// l'air au-dessus des voies et bloquait leurs bords. Le lit de la rivière se construit depuis le 28 octobre au soir, à la
    /// hauteur de l'eau (seuls les passages des voies sont la voie, depuis le 2 novembre). Un bâtiment occupe plusieurs cases, d'après la taille de son modèle (<see cref="FootprintOf"/>) : il se pose
    /// là où toutes ses cases lui conviennent et sont libres (<see cref="Evaluate"/>). Pendant la pose, la grille se voit au
    /// sol autour du joueur, verte là où le bâtiment choisi se pose, rouge ailleurs (<see cref="BuildGridOverlay"/>).
    /// </summary>
    /// <remarks>
    /// Créée toute seule par le <see cref="BuildManager"/> : rien à poser dans la scène. Au lancement, les 12 socles posés à la
    /// main et les 3 barricades de départ sont désactivés (la scène n'est pas modifiée : deux cases à décocher pour les garder).
    /// La recherche se fait sur plusieurs images, quelques millisecondes par image. Depuis le 28 octobre, l'emplacement
    /// (<see cref="BuildSocket"/>) d'un bâtiment n'existe qu'une fois le bâtiment posé (<see cref="Claim"/>) : il prend ses cases,
    /// rendues quand il est déplacé (<see cref="Release"/>). Seuls les petits emplacements sur la tour sont faits d'avance.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BuildGrid : MonoBehaviour
    {
        enum CellKind : byte
        {
            // Pas de sol trouvé : la case ne se dessine pas.
            Void,
            // Du sol où l'on ne construit pas (pente, eau, obstacle, trop loin d'une voie…) : rouge.
            Blocked,
            Lane,
            Roadside,
            Height,
        }

        // Ce qu'il y a sous un point (28 octobre au soir) : la terre des voies, l'herbe, ou autre chose (un pont, de la pierre, un
        // sol sans matériau connu). Lu dans le matériau de la tuile touchée (« dirt », « dirtDark », « grass » des kits de Kenney).
        // River : le lit de la rivière hors des passages sur les voies (ses tuiles sont aussi de la terre, RiverPieceOf). Earth (3
        // novembre) : de la terre hors des voies, celle des marches et des blocs rocheux du kit (les escaliers qui montent de la
        // voie de droite vers la jungle l'élargissaient).
        enum Surface : byte
        {
            Other,
            Dirt,
            Grass,
            River,
            Earth,
        }

        // Une pièce du lit de la rivière : son niveau des berges (son pivot), et si elle est un passage sur une voie.
        struct RiverPiece
        {
            public bool IsRiver;
            public bool Crossing;
            public float Bank;
        }

        // L'eau de la rivière est 20 cm sous les berges (Map > River Water, Docs/Guide_Carte.md, section 7 bis).
        const float k_WaterDepth = 0.2f;

        // Ce que montre chaque sous-maillage d'un collider, et l'indice du triangle qui suit le dernier de chacun (null : un seul
        // matériau, pas besoin du triangle touché). Lu une fois par collider.
        sealed class SurfaceInfo
        {
            public Surface[] Kinds;
            public int[] TriangleEnds;
        }

        /// <summary>Une place pour un bâtiment : où il irait, tourné comment, et s'il peut s'y poser.</summary>
        public struct Spot
        {
            /// <summary>Le point visé est sur la grille (ou près d'un emplacement de la tour).</summary>
            public bool OnGrid;
            public bool Valid;
            public Vector3 Position;
            public Quaternion Rotation;
            public BuildSlot Slot;

            /// <summary>Ce qui ne va pas, pour l'étiquette de l'hologramme (vide si tout va bien).</summary>
            public string Reason;

            /// <summary>Un emplacement de la tour (fait d'avance), sinon null : l'emplacement sera créé à la pose.</summary>
            public BuildSocket TowerSocket;

            /// <summary>La rotation, à poser sur un emplacement de la tour (par rapport à lui).</summary>
            public Quaternion LocalRotation;

            /// <summary>Taille (m) de la place, en X et en Z, dans le sens du bâtiment.</summary>
            public Vector2 Size;
        }

        static readonly RaycastHit[] s_Hits = new RaycastHit[32];

        // Les modèles de la carte qui ne sont pas du sol : on ne construit pas dessus (les arbres : MapScan.IsPlant). « stone »
        // depuis le 2 novembre au soir : les grandes pierres dressées (stone_tall…, stone_large…) étaient prises pour du sol, « pas
        // plat », sur les terrasses (les petites, stone_small…, disparaissent sous un bâtiment, plus bas).
        static readonly string[] k_ObstaclePrefixes =
            { "rock", "stone", "stump", "log", "fence", "statue", "tent", "campfire", "pot_", "sign", "crop", "barrel", "crate", "chest" };

        // Les cailloux du kit (2 novembre au soir, premier essai du joueur : ils bloquaient des places, dans la rivière surtout ;
        // au ×3 du kit, beaucoup sont plus gros que les petits cailloux d'IsPebble). Comme les mannequins et les cibles
        // d'entraînement : ni sol ni obstacle pour la grille, et un bâtiment posé dessus les fait disparaître (rendus s'il est
        // déplacé). Les grands rochers (stone_tall…) bloquent toujours.
        static readonly string[] k_ClearablePrefixes = { "rock_small", "stone_small" };

        // Le groupe des tuiles de terre des voies (Map > Lanes, Docs/Guide_Carte.md, section 0) : seule leur terre est la voie.
        const string k_LanesGroup = "Lanes";

        // Pourquoi une case n'est pas constructible (2 novembre au soir : l'étiquette de l'hologramme le dit).
        enum BlockReason : byte
        {
            None,
            Obstacle,
            Slope,
            Tower,
            Spawn,
            ChestSpot,
            Overflow,
            // Plus de 7,5 m au-dessus des voies (les montagnes), ou 1,5 m sous elles. (« Trop loin d'une voie » jusqu'au 2 novembre au
            // soir : la grille n'a plus de limite de distance.)
            TooHigh,
            TooLow,
        }

        [Header("Grille")]
        [Tooltip("Taille (m) d'une case (28 octobre : 1 m, demandé par le joueur ; 3 m avant). Un bâtiment en occupe plusieurs.")]
        [SerializeField]
        float m_GridCellSize = 1f;

        [Tooltip("Écart de hauteur (m) admis sous une case, et entre les cases d'un bâtiment : au-delà, le sol n'est pas plat.")]
        [SerializeField]
        float m_MaxStep = 0.3f;

        [Tooltip("Temps (ms) de recherche par image au lancement : la grille se fait en quelques secondes sans à-coup.")]
        [SerializeField]
        float m_FrameBudget = 3f;

        [Header("Sortes d'emplacement")]
        [Tooltip("Une case dont le centre est sur la terre de la voie, à moins de cette distance (m) du chemin des ennemis et à son " +
                 "niveau, est sur la voie (28 octobre au soir : la voie du milieu fait 8,5 m de large, et le chemin coupe les virages).")]
        [SerializeField]
        float m_LaneReach = 8f;

        // Lane Half Width (3 m), retiré le 2 novembre au soir : un sol ni terre ni herbe (un pont…) à moins de 3 m du chemin était
        // sur la voie, la seule règle avant le 28 octobre au soir. Les ponts de la carte passent au-dessus de la voie du milieu.
        // Roadside Width (4,5 m, puis 12 m) et Height Reach (15 m, puis 25 m), retirés le même soir : la grille n'a plus de limite
        // de distance (le joueur : « les hauteurs, c'est un peu le bordel » ; les terrasses à 6 m près de l'antre étaient trop
        // loin du chemin des ennemis, qui part de l'apparition du milieu). Tout sol plat se construit, au sol ou sur les terrasses.

        [Tooltip("Une case plus haute que la voie d'au moins cette hauteur (m) est en hauteur (1,5 m depuis le 2 novembre au soir, " +
                 "2 m avant : entre les deux, rien n'allait).")]
        [SerializeField]
        float m_HeightMin = 1.5f;

        [Tooltip("Une case plus haute que la voie de plus de cette hauteur (m) n'est pas constructible : les montagnes (les " +
                 "terrasses de la jungle sont à 3 et 6 m).")]
        [SerializeField]
        float m_HeightMax = 7.5f;

        [Tooltip("Marge (m) de la grille autour des chemins des ennemis : assez pour toute la carte, jusqu'aux montagnes.")]
        [SerializeField]
        float m_GridMargin = 15f;

        [Header("Interdits")]
        [Tooltip("Pas de construction à moins de cette distance (m) de la tour (le téléporteur, l'escalier).")]
        [SerializeField]
        float m_TowerClearance = 7f;

        [Tooltip("Pas de construction à moins de cette distance (m) d'un point d'apparition des ennemis.")]
        [SerializeField]
        float m_SpawnClearance = 6f;

        [Tooltip("Pas de construction à moins de cette distance (m) d'un emplacement de caisse ou de coffre.")]
        [SerializeField]
        float m_SpotClearance = 2.5f;

        [Header("Sur la tour")]
        [Tooltip("Emplacements sur la plate-forme du joueur (28 octobre : le seau d'huile, la baliste sur la tour), au plus.")]
        [SerializeField]
        int m_TowerSlots = 3;

        [Tooltip("Taille (m) d'un emplacement sur la tour.")]
        [SerializeField]
        float m_TowerSlotSize = 1.2f;

        [Tooltip("Distances (m) essayées entre le joueur (au lancement) et un emplacement sur la tour.")]
        [SerializeField]
        float[] m_TowerSlotRadii = { 1.5f, 1.2f, 1.9f };

        [Header("Ancien système")]
        [Tooltip("Désactive au lancement les socles posés à la main dans la scène (les 12 d'avant le 27 octobre) qui n'ont pas de bâtiment.")]
        [SerializeField]
        bool m_RemoveAuthoredSockets = true;

        [Tooltip("Désactive au lancement les barricades de départ (refonte du 27 octobre : la tour n'a que ce que le joueur construit).")]
        [SerializeField]
        bool m_RemoveStartBarricades = true;

        readonly List<BuildSocket> m_TowerSockets = new List<BuildSocket>();
        readonly List<Vector3> m_SpawnPoints = new List<Vector3>();
        readonly List<Vector3> m_Spots = new List<Vector3>();
        readonly Dictionary<Collider, bool> m_ObstacleCache = new Dictionary<Collider, bool>();
        readonly Dictionary<Collider, bool> m_PebbleCache = new Dictionary<Collider, bool>();
        readonly Dictionary<Collider, Transform> m_ClearableCache = new Dictionary<Collider, Transform>();
        readonly Dictionary<Transform, Bounds> m_Clearables = new Dictionary<Transform, Bounds>();
        readonly Dictionary<BuildSocket, List<GameObject>> m_Cleared = new Dictionary<BuildSocket, List<GameObject>>();
        readonly Dictionary<Collider, SurfaceInfo> m_SurfaceCache = new Dictionary<Collider, SurfaceInfo>();
        readonly Dictionary<Collider, RiverPiece> m_RiverCache = new Dictionary<Collider, RiverPiece>();
        readonly Dictionary<Collider, bool> m_CladdingCache = new Dictionary<Collider, bool>();
        readonly Dictionary<Collider, bool> m_LaneTileCache = new Dictionary<Collider, bool>();
        readonly Dictionary<BuildSocket, int[]> m_Claims = new Dictionary<BuildSocket, int[]>();
        readonly Dictionary<StructureDefinition, Vector2Int> m_Footprints = new Dictionary<StructureDefinition, Vector2Int>();
        readonly List<int> m_Scratch = new List<int>();
        readonly List<int> m_Roadside = new List<int>();
        List<MapScan.Lane> m_Lanes = new List<MapScan.Lane>();
        Transform m_Container;
        int m_TemplateLayer;
        InteractionLayerMask m_TemplateInteractionLayers = 1;
        bool m_TemplateTrigger;

        // La grille : l'origine (coin des X et Z les plus petits), sa taille en cases, et pour chaque case sa sorte, la hauteur du
        // sol en son centre, le sens d'un bâtiment posé là (cap, en degrés) et le bâtiment qui l'occupe. Les hauteurs du sol aux
        // coins des cases servent au dessin de la grille (elle épouse le terrain).
        Vector2 m_Origin;
        int m_Width;
        int m_Depth;
        CellKind[] m_Kind = System.Array.Empty<CellKind>();
        BlockReason[] m_Why = System.Array.Empty<BlockReason>();

        // Ce qui bloque une case (l'obstacle, ou le sol qui n'est pas plat) : son nom s'affiche en test (2 novembre).
        Collider[] m_Culprit = System.Array.Empty<Collider>();
        float[] m_Height = System.Array.Empty<float>();
        float[] m_Facing = System.Array.Empty<float>();
        float[] m_Corner = System.Array.Empty<float>();
        BuildSocket[] m_Occupant = System.Array.Empty<BuildSocket>();

        public static BuildGrid Instance { get; private set; }

        /// <summary>Le chemin des ennemis de chaque voie, de leur apparition à la tour (le phare y pose son anneau de feu, 29 octobre).</summary>
        public IReadOnlyList<MapScan.Lane> Lanes => m_Lanes;

        /// <summary>La recherche des cases est finie.</summary>
        public bool IsReady { get; private set; }

        /// <summary>Taille (m) d'une case.</summary>
        public float CellSize => m_GridCellSize;

        /// <summary>Change à chaque bâtiment posé ou déplacé : le dessin de la grille se refait.</summary>
        public int Version { get; private set; }

        /// <summary>Les petits emplacements sur la tour.</summary>
        public IReadOnlyList<BuildSocket> TowerSockets => m_TowerSockets;

        /// <summary>Taille (m) d'un emplacement sur la tour.</summary>
        public float TowerSlotSize => m_TowerSlotSize;

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

        // ---------------------------------------------------------------- la recherche, au lancement

        IEnumerator Build()
        {
            // Une image : la carte, le NavMesh et les autres composants sont prêts.
            yield return null;

            RemoveOldSystem();
            m_Lanes = MapScan.RunLanes().Lanes;
            if (m_Lanes.Count == 0)
            {
                Debug.LogWarning("BuildGrid : aucun chemin d'ennemis trouvé (Enemy Spawner, tour, NavMesh) : pas de zone de construction.", this);
                yield break;
            }

            CollectForbiddenPoints();
            m_Container = new GameObject("Emplacements de construction (grille)").transform;

            // Le rectangle autour des voies, assez large pour toute la carte, calé sur la grille.
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

            var cell = m_GridCellSize = Mathf.Max(0.25f, m_GridCellSize);
            var margin = Mathf.Max(m_GridMargin, m_LaneReach) + cell;
            m_Origin = new Vector2(Mathf.Floor((min.x - margin) / cell) * cell, Mathf.Floor((min.z - margin) / cell) * cell);
            m_Width = Mathf.CeilToInt((max.x + margin - m_Origin.x) / cell);
            m_Depth = Mathf.CeilToInt((max.z + margin - m_Origin.y) / cell);
            var count = m_Width * m_Depth;
            m_Kind = new CellKind[count];
            m_Why = new BlockReason[count];
            m_Culprit = new Collider[count];
            m_Height = new float[count];
            m_Facing = new float[count];
            m_Occupant = new BuildSocket[count];
            m_Corner = new float[(m_Width + 1) * (m_Depth + 1)];
            var cornerBlocked = new bool[m_Corner.Length];
            var cornerCulprit = new Collider[m_Corner.Length];
            var top = max.y + 40f;
            var depth = top - min.y + 40f;
            var tower = TowerPosition();
            var watch = Stopwatch.StartNew();

            // Les coins des cases : la hauteur du sol, et s'il y a un obstacle (arbre, rocher) au-dessus.
            for (var z = 0; z <= m_Depth; z++)
            {
                for (var x = 0; x <= m_Width; x++)
                {
                    var i = z * (m_Width + 1) + x;
                    var found = SampleGround(m_Origin.x + x * cell, m_Origin.y + z * cell, top, depth, out var height, out var blocked, out _,
                                             out _, out cornerCulprit[i]);
                    m_Corner[i] = found ? height : float.NaN;
                    cornerBlocked[i] = blocked;
                }

                if (watch.Elapsed.TotalMilliseconds > m_FrameBudget)
                {
                    yield return null;
                    watch.Restart();
                }
            }

            // Les cases : le sol en leur centre, plat sous toute la case, puis leur sorte d'après la terre de la voie et la voie la
            // plus proche. (Jusqu'au 2 novembre au soir, une seconde passe refusait les cases au bord trop loin de la voie.)
            var counts = new int[5];
            var dirtLane = 0;
            for (var z = 0; z < m_Depth; z++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    var i = z * m_Width + x;
                    m_Kind[i] = Classify(x, z, top, depth, tower, cornerBlocked, cornerCulprit, out m_Height[i], out m_Facing[i],
                                         out var onDirt, out m_Why[i], out m_Culprit[i]);
                    counts[(int)m_Kind[i]]++;
                    if (onDirt && m_Kind[i] == CellKind.Lane)
                        dirtLane++;
                }

                if (watch.Elapsed.TotalMilliseconds > m_FrameBudget)
                {
                    yield return null;
                    watch.Restart();
                }
            }

            // Les cases au bord de la voie, de la plus proche de la tour à la plus lointaine (raccourci F de la démo).
            m_Roadside.Clear();
            for (var i = 0; i < count; i++)
            {
                if (m_Kind[i] == CellKind.Roadside)
                    m_Roadside.Add(i);
            }

            m_Roadside.Sort((a, b) => Flat(CellCenter(a) - tower).CompareTo(Flat(CellCenter(b) - tower)));

            var towerSlots = CreateTowerSockets();
            m_ObstacleCache.Clear();
            m_ClearableCache.Clear();
            m_SurfaceCache.Clear();
            m_RiverCache.Clear();
            m_CladdingCache.Clear();
            m_LaneTileCache.Clear();
            IsReady = true;
            Version++;
            Debug.Log($"BuildGrid : {m_Width} × {m_Depth} cases de {cell} m ; constructibles : {counts[(int)CellKind.Lane]} sur la voie, " +
                      $"{counts[(int)CellKind.Roadside]} au bord, {counts[(int)CellKind.Height]} en hauteur ; {towerSlots} emplacement(s) " +
                      $"sur la tour.", this);
            if (dirtLane == 0)
                Debug.LogWarning("BuildGrid : aucune case sur la terre des voies (matériau « dirt » des tuiles rangées sous un objet " +
                                 "« " + k_LanesGroup + " », ou des passages de la rivière) : rien ne se pose sur la voie.", this);
#if UNITY_EDITOR
            WriteDebugImage();
#endif
        }

#if UNITY_EDITOR
        // Pour Claude (2 novembre) : la grille vue de dessus, dans Logs/BuildGrid.png (hors de Git), à chaque lancement dans
        // l'éditeur. Unity ouvert, Claude ne voit pas la scène : l'image lui montre ce que la grille a compris de la carte. Une case
        // = 4 × 4 pixels ; X vers la droite, Z vers le haut ; un trait sombre tous les 10 m, le chemin des ennemis en blanc. Orange :
        // la voie ; bleu : le bord ; vert : en hauteur ; rouge : un obstacle ; brun : pas plat ; rose : déborde sur la voie ; brun
        // sombre : trop haut ou trop bas ; gris : la tour ; violet : une apparition ; jaune : un coffre ; noir : pas de sol.
        void WriteDebugImage()
        {
            const int scale = 4;
            var width = m_Width * scale;
            var height = m_Depth * scale;
            var pixels = new Color32[width * height];
            for (var z = 0; z < m_Depth; z++)
            {
                var lineZ = Mathf.RoundToInt(m_Origin.y + z * m_GridCellSize) % 10 == 0;
                for (var x = 0; x < m_Width; x++)
                {
                    var lineX = Mathf.RoundToInt(m_Origin.x + x * m_GridCellSize) % 10 == 0;
                    var color = DebugColor(z * m_Width + x);
                    for (var dz = 0; dz < scale; dz++)
                    {
                        for (var dx = 0; dx < scale; dx++)
                        {
                            var line = lineX && dx == 0 || lineZ && dz == 0;
                            pixels[(z * scale + dz) * width + x * scale + dx] =
                                line ? new Color32((byte)(color.r / 2), (byte)(color.g / 2), (byte)(color.b / 2), 255) : color;
                        }
                    }
                }
            }

            foreach (var lane in m_Lanes)
            {
                for (var i = 1; i < lane.Points.Count; i++)
                {
                    var from = lane.Points[i - 1];
                    var to = lane.Points[i];
                    var steps = Mathf.CeilToInt(Mathf.Sqrt(Flat(to - from)) * scale / m_GridCellSize) + 1;
                    for (var s = 0; s <= steps; s++)
                    {
                        var point = Vector3.Lerp(from, to, (float)s / steps);
                        var px = Mathf.FloorToInt((point.x - m_Origin.x) / m_GridCellSize * scale);
                        var pz = Mathf.FloorToInt((point.z - m_Origin.y) / m_GridCellSize * scale);
                        if (px >= 0 && px < width && pz >= 0 && pz < height)
                            pixels[pz * width + px] = new Color32(255, 255, 255, 255);
                    }
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply();
                var path = System.IO.Path.Combine(Application.dataPath, "..", "Logs", "BuildGrid.png");
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("BuildGrid : l'image de la grille (Logs/BuildGrid.png) n'a pas pu s'écrire : " + exception.Message, this);
            }
            finally
            {
                Destroy(texture);
            }
        }

        Color32 DebugColor(int i)
        {
            switch (m_Kind[i])
            {
                case CellKind.Void: return new Color32(15, 15, 15, 255);
                case CellKind.Lane: return new Color32(240, 140, 40, 255);
                case CellKind.Roadside: return new Color32(60, 160, 240, 255);
                case CellKind.Height: return new Color32(60, 200, 90, 255);
            }

            switch (m_Why[i])
            {
                case BlockReason.Obstacle: return new Color32(200, 30, 30, 255);
                case BlockReason.Slope: return new Color32(140, 90, 60, 255);
                case BlockReason.Tower: return new Color32(150, 150, 150, 255);
                case BlockReason.Spawn: return new Color32(170, 60, 200, 255);
                case BlockReason.ChestSpot: return new Color32(240, 220, 40, 255);
                case BlockReason.Overflow: return new Color32(255, 130, 170, 255);
                default: return new Color32(80, 50, 40, 255);
            }
        }
#endif

        // Les 12 socles posés à la main (sauf ceux qui portent déjà un bâtiment) et les barricades de départ. Le premier socle sert
        // de modèle aux emplacements : sa couche, ses couches d'interaction, sa plaque. Depuis le 28 octobre au soir, ils sont
        // supprimés de la scène (Editor/OldBuildSystemCleanupBaker) : il n'y a plus rien à désactiver, et le modèle garde ses
        // valeurs par défaut, celles du prefab Build Socket (couche Default, couche d'interaction Default, plaque pleine).
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

        // Les points d'apparition (ennemis, coffres : ceux du Crystal Spawner, qui reprend ceux du Chest Spawner depuis le
        // 2 novembre) : on n'y construit pas. Et les mannequins et cibles d'entraînement, qu'un bâtiment posé dessus fait disparaître.
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

            foreach (var dummy in FindObjectsByType<TrainingDummy>(FindObjectsInactive.Include))
                RegisterClearable(dummy.transform);
            foreach (var target in FindObjectsByType<TrainingTarget>(FindObjectsInactive.Include))
                RegisterClearable(target.transform);
        }

        // Une case : son sol (plat sous toute la case, sans obstacle), sa sorte d'après la terre de la voie et la voie la plus
        // proche, et le cap d'un bâtiment posé là (vers la forêt sur la voie, vers la voie ailleurs). onDirt : la case est sur la
        // terre d'une voie. culprit : ce qui bloque la case (un obstacle, un sol pas plat), sinon null.
        CellKind Classify(int x, int z, float top, float depth, Vector3 tower, bool[] cornerBlocked, Collider[] cornerCulprit,
                          out float height, out float facing, out bool onDirt, out BlockReason why, out Collider culprit)
        {
            facing = 0f;
            onDirt = false;
            why = BlockReason.None;
            culprit = null;
            var cell = m_GridCellSize;
            var center = new Vector3(m_Origin.x + (x + 0.5f) * cell, 0f, m_Origin.y + (z + 0.5f) * cell);
            if (!SampleGround(center.x, center.z, top, depth, out height, out var blocked, out var normalY, out var surface, out var hit))
            {
                height = float.NaN;
                return CellKind.Void;
            }

            onDirt = surface == Surface.Dirt;
            var ground = new Vector3(center.x, height, center.z);

            // L'herbe tolère ses petites bosses (29 octobre au soir, le joueur : des bouts de prairie au niveau du sol étaient
            // refusés) : une pente jusqu'à 45°, 60 cm d'écart entre le centre et un coin.
            var grass = surface == Surface.Grass;
            if (blocked || normalY < (grass ? 0.7f : 0.85f))
            {
                why = blocked ? BlockReason.Obstacle : BlockReason.Slope;
                culprit = hit;
                return CellKind.Blocked;
            }

            var maxStep = grass ? Mathf.Max(m_MaxStep, 0.6f) : m_MaxStep;
            for (var i = 0; i < 4; i++)
            {
                var corner = (z + i / 2) * (m_Width + 1) + x + i % 2;
                var cornerHeight = m_Corner[corner];
                if (cornerBlocked[corner] || float.IsNaN(cornerHeight) || Mathf.Abs(cornerHeight - height) > maxStep)
                {
                    why = cornerBlocked[corner] ? BlockReason.Obstacle : BlockReason.Slope;
                    culprit = cornerBlocked[corner] || cornerHeight > height ? cornerCulprit[corner] : hit;
                    return CellKind.Blocked;
                }
            }

            // L'eau ne bloque plus (28 octobre au soir, demandé par le joueur) : le lit de la rivière se construit comme l'herbe,
            // un gué comme la voie.
            why = IsNear(ground, tower, m_TowerClearance) ? BlockReason.Tower
                : IsNearAny(ground, m_SpawnPoints, m_SpawnClearance) ? BlockReason.Spawn
                : IsNearAny(ground, m_Spots, m_SpotClearance) ? BlockReason.ChestSpot
                : BlockReason.None;
            if (why != BlockReason.None)
                return CellKind.Blocked;

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
            var towardLane = toLane.sqrMagnitude > 0.01f ? Mathf.Atan2(toLane.x, toLane.z) * Mathf.Rad2Deg : 0f;
            var atLaneLevel = Mathf.Abs(rise) <= 1.2f;

            // Sur la voie (28 octobre au soir) : la terre de la voie sous toute la case, près du chemin des ennemis et à son niveau.
            // Avant, toute case à moins de 3 m du chemin : il coupe les virages et la voie du milieu fait 8,5 m de large, la
            // grille débordait sur l'herbe d'un côté et laissait de la terre « au bord » de l'autre. Le 2 novembre au soir (deuxième
            // essai du joueur, deux fois) : seulement la terre des tuiles des voies et des passages de la rivière (Surface.Dirt ; celle
            // des marches et des rochers ne compte plus, ni le lit de la rivière à côté d'un gué, ni un sol inconnu à 3 m du chemin) :
            // la voie de droite s'élargissait. Et une case à moitié sur la terre (les demi-cases de la bordure en diagonale de la voie
            // du milieu) est de la clairière, au bord de la voie : on y pose les tours, pas les pièges.
            var halfCell = surface == Surface.Dirt && atLaneLevel && IsHalfCell(center, height, cell, top, depth);
            if (surface == Surface.Dirt && !halfCell && atLaneLevel && distance <= m_LaneReach && IsOnNavMesh(ground))
            {
                // Sur la voie : l'avant vers la forêt, d'où viennent les ennemis (le chemin va de leur apparition à la tour).
                laneDirection.y = 0f;
                facing = laneDirection.sqrMagnitude > 1e-4f ? Mathf.Atan2(-laneDirection.x, -laneDirection.z) * Mathf.Rad2Deg : 0f;
                return CellKind.Lane;
            }

            // De la terre de la voie, à son niveau, mais loin du chemin ou hors du NavMesh : ni sur la voie, ni au bord (un bâtiment
            // y dépasserait sur le chemin des ennemis).
            if (surface == Surface.Dirt && !halfCell && atLaneLevel)
            {
                why = BlockReason.Overflow;
                return CellKind.Blocked;
            }

            // Ailleurs, tout sol plat, sans limite de distance depuis le 2 novembre au soir : au niveau des voies (au bord), ou sur
            // une terrasse jusqu'à 7,5 m plus haut (en hauteur ; pas les montagnes). Tourné vers la voie la plus proche.
            facing = towardLane;
            if (Mathf.Abs(rise) <= m_HeightMin)
                return CellKind.Roadside;
            if (rise > m_HeightMin && rise <= m_HeightMax)
                return CellKind.Height;

            why = rise > 0f ? BlockReason.TooHigh : BlockReason.TooLow;
            return CellKind.Blocked;
        }

        // Le sol sous ce point : le plus haut touché qui n'est ni un obstacle (arbre, rocher…), ni un déclencheur, ni ce que les
        // flèches traversent, ni une plaque d'emplacement, ni un petit caillou, un caillou du kit ou un mannequin d'entraînement
        // (qu'un bâtiment fait disparaître), ni l'habillage d'une falaise (2 novembre). blocked : un obstacle se dresse au-dessus de
        // ce sol. surface : de la terre, de l'herbe ou autre chose (d'après le matériau touché). culprit : l'obstacle s'il y en a
        // un, sinon le sol touché.
        bool SampleGround(float x, float z, float top, float depth, out float height, out bool blocked, out float normalY,
                          out Surface surface, out Collider culprit)
        {
            height = 0f;
            normalY = 1f;
            culprit = null;
            var count = Physics.RaycastNonAlloc(new Vector3(x, top, z), Vector3.down, s_Hits, depth, Physics.DefaultRaycastLayers,
                                                QueryTriggerInteraction.Ignore);
            var ground = float.MaxValue;
            var obstacle = float.MaxValue;
            var groundHit = -1;
            Collider obstacleCollider = null;
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if (ArrowIgnore.IsIgnored(hit.collider) || hit.collider.GetComponentInParent<BuildSocket>() != null || IsPebble(hit.collider) ||
                    ClearableRoot(hit.collider) != null || IsCladding(hit.collider))
                    continue;

                if (IsObstacle(hit.collider))
                {
                    if (hit.distance < obstacle)
                    {
                        obstacle = hit.distance;
                        obstacleCollider = hit.collider;
                    }

                    continue;
                }

                if (hit.distance >= ground)
                    continue;

                ground = hit.distance;
                height = hit.point.y;
                normalY = hit.normal.y;
                groundHit = i;
            }

            blocked = obstacle < ground;
            surface = Surface.Other;
            if (groundHit < 0)
                return false;

            var groundCollider = s_Hits[groundHit].collider;
            culprit = blocked ? obstacleCollider : groundCollider;
            surface = SurfaceOf(s_Hits[groundHit]);
            if (surface == Surface.Dirt && !IsLaneTile(groundCollider))
                surface = Surface.Earth;
            var river = RiverPieceOf(groundCollider);
            if (river.IsRiver)
            {
                // Le lit de la rivière (37,5 cm sous les berges, avec des talus) : un sol plat au niveau de l'eau, pour construire
                // dessus ; les berges gardent leur hauteur (20 cm plus haut : moins que l'écart admis entre deux cases).
                height = Mathf.Max(height, river.Bank - k_WaterDepth);
                normalY = 1f;
                if (river.Crossing)
                    surface = Surface.Dirt;
                else if (surface != Surface.Grass)
                    surface = Surface.River;
            }

            return true;
        }

        // Une tuile de chemin rangée sous « River » (Map > River, Docs/Guide_Carte.md, section 7) : le pivot est au niveau des
        // berges ; « …_lane » : un passage sur une voie (ses berges sont en terre).
        RiverPiece RiverPieceOf(Collider collider)
        {
            if (m_RiverCache.TryGetValue(collider, out var piece))
                return piece;

            piece = default;
            for (var transform = collider.transform; transform != null && transform.parent != null; transform = transform.parent)
            {
                if (transform.parent.name != "River")
                    continue;

                if (transform.name.StartsWith("ground_", System.StringComparison.Ordinal))
                {
                    piece.IsRiver = true;
                    piece.Crossing = transform.name.IndexOf("_lane", System.StringComparison.Ordinal) >= 0;
                    piece.Bank = transform.position.y;
                }

                break;
            }

            m_RiverCache[collider] = piece;
            return piece;
        }

        // La surface touchée : le matériau du sous-maillage du triangle touché (une tuile de chemin a de la terre et de l'herbe,
        // une case coupée en diagonale aussi). Il faut le même maillage pour le collider et le dessin : c'est le cas des modèles
        // des kits, dont les colliders sont faits à l'import.
        Surface SurfaceOf(RaycastHit hit)
        {
            var collider = hit.collider;
            if (!m_SurfaceCache.TryGetValue(collider, out var info))
            {
                info = ReadSurfaces(collider);
                m_SurfaceCache[collider] = info;
            }

            if (info == null)
                return Surface.Other;
            if (info.TriangleEnds == null)
                return info.Kinds[0];

            var triangle = hit.triangleIndex;
            for (var i = 0; i < info.TriangleEnds.Length && triangle >= 0; i++)
            {
                if (triangle < info.TriangleEnds[i])
                    return info.Kinds[i];
            }

            return Surface.Other;
        }

        static SurfaceInfo ReadSurfaces(Collider collider)
        {
            var renderer = collider.GetComponent<MeshRenderer>();
            if (renderer == null)
                return null;

            var materials = renderer.sharedMaterials;
            var count = materials.Length;
            if (count == 0)
                return null;

            var info = new SurfaceInfo { Kinds = new Surface[count] };
            var same = true;
            for (var i = 0; i < count; i++)
            {
                info.Kinds[i] = SurfaceOf(materials[i]);
                same &= info.Kinds[i] == info.Kinds[0];
            }

            if (same)
                return info;

            // Plusieurs sortes de sol : le triangle touché dit lequel (les sous-maillages se suivent dans l'ordre des triangles).
            var meshCollider = collider as MeshCollider;
            var filter = collider.GetComponent<MeshFilter>();
            var mesh = meshCollider != null ? meshCollider.sharedMesh : null;
            if (mesh == null || filter == null || filter.sharedMesh != mesh || mesh.subMeshCount < count)
                return null;

            info.TriangleEnds = new int[count];
            var end = 0;
            for (var i = 0; i < count; i++)
            {
                end += mesh.GetSubMesh(i).indexCount / 3;
                info.TriangleEnds[i] = end;
            }

            return info;
        }

        static Surface SurfaceOf(Material material)
        {
            var name = material != null ? material.name : string.Empty;
            if (name.StartsWith("dirt", System.StringComparison.OrdinalIgnoreCase))
                return Surface.Dirt;
            return name.StartsWith("grass", System.StringComparison.OrdinalIgnoreCase) ? Surface.Grass : Surface.Other;
        }

        // Une case de la voie à moitié sur autre chose que sa terre, à sa hauteur (2 novembre : les demi-cases de la bordure en
        // diagonale de la voie du milieu, mi-terre mi-herbe) : 4 points près de ses coins, à 10 % du bord. Les voies droites ont
        // leurs bords sur les lignes de la grille : leurs cases sont entières. (Jusqu'au 2 novembre, TouchesDirt faisait l'inverse
        // pour les cases d'herbe qui touchaient la terre : elles débordaient sur la voie, puis furent la voie un jour.)
        bool IsHalfCell(Vector3 center, float height, float cell, float top, float depth)
        {
            for (var i = 0; i < 4; i++)
            {
                var x = center.x + (i % 2 == 0 ? -0.4f : 0.4f) * cell;
                var z = center.z + (i < 2 ? -0.4f : 0.4f) * cell;
                if (SampleGround(x, z, top, depth, out var sampled, out _, out _, out var surface, out _) && surface != Surface.Dirt &&
                    Mathf.Abs(sampled - height) <= 0.6f)
                    return true;
            }

            return false;
        }

        // La terre de cette tuile est-elle celle d'une voie (une tuile rangée sous Map > Lanes, 2 novembre) ? Les marches et les
        // blocs rocheux du kit ont aussi de la terre (matériau « dirt ») : au pied de la jungle, ils élargissaient la voie de droite.
        bool IsLaneTile(Collider collider)
        {
            if (m_LaneTileCache.TryGetValue(collider, out var cached))
                return cached;

            var lane = false;
            var transform = collider.transform;
            for (var i = 0; i < 5 && transform != null && !lane; i++, transform = transform.parent)
                lane = transform.name == k_LanesGroup;

            m_LaneTileCache[collider] = lane;
            return lane;
        }

        // Un petit caillou, une touffe (29 octobre au soir, le joueur : ils bloquaient des cases de la prairie) : moins de 1,2 m de
        // large et de 70 cm de haut. Ni sol ni obstacle : on construit par-dessus.
        bool IsPebble(Collider collider)
        {
            if (m_PebbleCache.TryGetValue(collider, out var cached))
                return cached;

            var size = collider.bounds.size;
            var pebble = !(collider is TerrainCollider) && size.x <= 1.2f && size.z <= 1.2f && size.y <= 0.7f &&
                         collider.GetComponentInParent<Enemy>() == null && collider.GetComponentInParent<Structure>() == null;
            m_PebbleCache[collider] = pebble;
            return pebble;
        }

        // Le caillou du kit, le mannequin ou la cible d'entraînement de ce collider (ce qu'un bâtiment posé dessus fait disparaître),
        // ou null.
        Transform ClearableRoot(Collider collider)
        {
            if (m_ClearableCache.TryGetValue(collider, out var root))
                return root;

            root = null;
            var dummy = collider.GetComponentInParent<TrainingDummy>(true);
            var target = dummy == null ? collider.GetComponentInParent<TrainingTarget>(true) : null;
            if (dummy != null)
                root = dummy.transform;
            else if (target != null)
                root = target.transform;

            var transform = collider.transform;
            for (var i = 0; i < 3 && transform != null && root == null; i++, transform = transform.parent)
            {
                foreach (var prefix in k_ClearablePrefixes)
                {
                    if (transform.name.StartsWith(prefix, System.StringComparison.Ordinal))
                    {
                        root = transform;
                        break;
                    }
                }
            }

            m_ClearableCache[collider] = root;
            if (root != null)
                RegisterClearable(root);
            return root;
        }

        // Retient la place d'un caillou ou d'un mannequin (ses colliders, tant qu'il est actif), pour le cacher sous un bâtiment.
        void RegisterClearable(Transform root)
        {
            if (root == null || m_Clearables.ContainsKey(root))
                return;

            var found = false;
            var bounds = new Bounds(root.position, Vector3.zero);
            foreach (var collider in root.GetComponentsInChildren<Collider>())
            {
                if (collider.isTrigger || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    continue;
                if (found)
                    bounds.Encapsulate(collider.bounds);
                else
                    bounds = collider.bounds;
                found = true;
            }

            // Un mannequin qui va et vient (2 novembre au soir) : toute sa place, d'un bout à l'autre du trajet.
            if (root.TryGetComponent<PingPongMover>(out var patrol))
            {
                var end = bounds;
                end.center += patrol.Offset;
                bounds.Encapsulate(end);
            }

            m_Clearables[root] = found ? bounds : new Bounds(root.position, Vector3.one);
        }

        // Les cailloux et les mannequins sous un bâtiment posé là disparaissent ; ils reviennent quand il s'en va (Release).
        void ClearUnder(BuildSocket socket, Vector3 center, Quaternion rotation, Vector2 size)
        {
            var hidden = new List<GameObject>();
            var inverse = Quaternion.Inverse(rotation);
            foreach (var pair in m_Clearables)
            {
                var root = pair.Key;
                if (root == null || !root.gameObject.activeSelf)
                    continue;

                var bounds = pair.Value;
                var local = inverse * (bounds.center - center);
                var reach = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.7f;
                if (Mathf.Abs(local.x) > size.x * 0.5f + reach || Mathf.Abs(local.z) > size.y * 0.5f + reach ||
                    bounds.max.y < center.y - 1f || bounds.min.y > center.y + 3f)
                    continue;

                root.gameObject.SetActive(false);
                hidden.Add(root.gameObject);
            }

            if (hidden.Count > 0)
                m_Cleared[socket] = hidden;
        }

        // L'habillage d'une falaise (2 novembre, deuxième essai du joueur) : les faces et les rebords des kits (cliff_rock,
        // cliff_top_stone, cliff_corner…), posés devant les blocs du terrain (cliff_block…) et les escaliers (cliff_steps…). Ni sol
        // ni obstacle : la grille prend le terrain dessous. Au bord des voies, ils débordaient en l'air au-dessus des cases (le
        // joueur : « un caillou qui est en l'air pour le décor ») et les bloquaient.
        bool IsCladding(Collider collider)
        {
            if (m_CladdingCache.TryGetValue(collider, out var cached))
                return cached;

            var cladding = false;
            var transform = collider.transform;
            for (var i = 0; i < 3 && transform != null; i++, transform = transform.parent)
            {
                var name = transform.name;
                if (!name.StartsWith("cliff_", System.StringComparison.Ordinal))
                    continue;

                cladding = !name.StartsWith("cliff_block", System.StringComparison.Ordinal) &&
                           !name.StartsWith("cliff_steps", System.StringComparison.Ordinal);
                break;
            }

            m_CladdingCache[collider] = cladding;
            return cladding;
        }

        bool IsObstacle(Collider collider)
        {
            if (m_ObstacleCache.TryGetValue(collider, out var cached))
                return cached;

            var obstacle = collider.GetComponentInParent<Enemy>() != null || collider.GetComponentInParent<Structure>() != null ||
                           MapScan.IsPlant(collider.transform);
            var transform = collider.transform;
            for (var i = 0; i < 3 && transform != null && !obstacle; i++, transform = transform.parent)
            {
                foreach (var prefix in k_ObstaclePrefixes)
                {
                    if (transform.name.StartsWith(prefix, System.StringComparison.Ordinal))
                    {
                        obstacle = true;
                        break;
                    }
                }
            }

            m_ObstacleCache[collider] = obstacle;
            return obstacle;
        }

        static bool IsOnNavMesh(Vector3 point)
        {
            if (!NavMesh.SamplePosition(point, out var hit, 1.5f, NavMesh.AllAreas))
                return false;

            var offset = hit.position - point;
            offset.y = 0f;
            return offset.magnitude < 1.2f;
        }

        // Les emplacements sur la tour (28 octobre : le seau d'huile, les légendaires) : sur la plate-forme où se tient le joueur
        // au lancement, à 1,2 à 1,9 m de lui, sur les côtés et derrière (pas devant : il y vise), tournés vers l'extérieur.
        // Seulement sur un sol plat à la hauteur de ses pieds (pas sur un créneau). Renvoie leur nombre.
        int CreateTowerSockets()
        {
            var head = Camera.main;
            var tower = Tower.Instance;
            if (head == null || tower == null || m_TowerSlots <= 0 || !AnyTowerBuilding())
                return 0;

            var center = head.transform.position;
            var flat = center - tower.transform.position;
            flat.y = 0f;
            if (flat.magnitude > 8f || !TryTowerFloor(center + Vector3.up * 0.2f, 4f, out var floor))
            {
                Debug.LogWarning("BuildGrid : le joueur n'est pas sur la tour au lancement : pas d'emplacement sur la tour.", this);
                return 0;
            }

            // Devant le joueur : vers les ennemis (le départ moyen des voies).
            var forward = Vector3.zero;
            foreach (var lane in m_Lanes)
            {
                if (lane.Points.Count > 0)
                    forward += (lane.Points[0] - floor).normalized;
            }

            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            var placed = new List<Vector3>();
            foreach (var angle in new[] { 70f, -70f, 110f, -110f, 180f, 40f, -40f })
            {
                foreach (var radius in m_TowerSlotRadii)
                {
                    if (placed.Count >= m_TowerSlots)
                        break;

                    var direction = Quaternion.Euler(0f, angle, 0f) * forward;
                    var probe = floor + direction * radius;
                    if (!TryTowerFloor(probe + Vector3.up * 1.6f, 2.2f, out var ground) || Mathf.Abs(ground.y - floor.y) > 0.35f)
                        continue;

                    var free = true;
                    foreach (var other in placed)
                        free &= !IsNear(ground, other, m_TowerSlotSize * 1.1f);
                    if (!free)
                        continue;

                    placed.Add(ground);
                    m_TowerSockets.Add(CreateSocket(ground, Quaternion.LookRotation(direction), BuildSlot.Tower,
                                                    new Vector2(m_TowerSlotSize, m_TowerSlotSize)));
                    break;
                }
            }

            return placed.Count;
        }

        // Un bâtiment en jeu se pose-t-il sur la tour ? Depuis le 29 octobre (10 tours), le seau d'huile et la baliste sur la tour
        // sont retirés : sans eux, pas d'emplacement sur la tour (ils se dessinaient en rouge pour tous les autres).
        static bool AnyTowerBuilding()
        {
            var builds = BuildManager.Instance;
            if (builds == null)
                return false;

            foreach (var definition in builds.Structures)
            {
                if (definition != null && !definition.retired && definition.slot == BuildSlot.Tower)
                    return true;
            }

            return false;
        }

        // Le sol sous ce point (le plus haut touché) : ni le corps du joueur, ni les plaques, ni les déclencheurs, ni un ennemi ; plat.
        static bool TryTowerFloor(Vector3 from, float depth, out Vector3 ground)
        {
            ground = from;
            var count = Physics.RaycastNonAlloc(from, Vector3.down, s_Hits, depth, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;
            var normal = Vector3.up;
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if (hit.distance >= best || ArrowIgnore.IsIgnored(hit.collider) || hit.collider.GetComponentInParent<BuildSocket>() != null ||
                    hit.collider.GetComponentInParent<Enemy>() != null)
                    continue;

                best = hit.distance;
                ground = hit.point;
                normal = hit.normal;
            }

            return best < float.MaxValue && normal.y > 0.8f;
        }

        BuildSocket CreateSocket(Vector3 ground, Quaternion rotation, BuildSlot slot, Vector2 size)
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
            plate.size = new Vector3(size.x * 0.95f, 0.1f, size.y * 0.95f);
            plate.center = Vector3.up * 0.05f;
            plate.isTrigger = m_TemplateTrigger;

            var socket = go.AddComponent<BuildSocket>();
            socket.colliders.Add(plate);
            socket.interactionLayers = m_TemplateInteractionLayers;
            socket.SetupCandidate(slot, size);
            go.SetActive(true);
            return socket;
        }

        // ---------------------------------------------------------------- la pose

        /// <summary>
        /// Les cases (largeur, profondeur) d'un bâtiment : celles de son réglage <see cref="StructureDefinition.footprint"/>, sinon
        /// d'après la taille de son modèle au niveau 3 (ce qui pousse aux niveaux suivants compris).
        /// </summary>
        public Vector2Int FootprintOf(StructureDefinition definition)
        {
            if (definition == null)
                return Vector2Int.one;
            if (definition.footprint.x > 0 && definition.footprint.y > 0)
                return definition.footprint;
            if (m_Footprints.TryGetValue(definition, out var footprint))
                return footprint;

            var size = definition.prefab != null ? definition.prefab.ModelSize() : Vector2.one;
            var cell = Mathf.Max(0.25f, m_GridCellSize);
            footprint = new Vector2Int(Mathf.Clamp(Mathf.CeilToInt(size.x / cell - 0.15f), 1, 6),
                                       Mathf.Clamp(Mathf.CeilToInt(size.y / cell - 0.15f), 1, 6));
            m_Footprints[definition] = footprint;
            Debug.Log($"BuildGrid : {definition.displayName} occupe {footprint.x} × {footprint.y} cases (modèle de {size.x:0.0} × " +
                      $"{size.y:0.0} m ; réglage Footprint de Data/Structures pour changer).", definition);
            return footprint;
        }

        /// <summary>
        /// Où irait ce bâtiment si on visait ce point, tourné de <paramref name="steps"/> crans de 45° depuis le sens de la case, et
        /// peut-il s'y poser ? <paramref name="cells"/> reçoit ses cases. Un bâtiment déplacé (<paramref name="moving"/>) ne se
        /// gêne pas lui-même.
        /// </summary>
        public Spot Evaluate(StructureDefinition definition, Vector3 point, int steps, Structure moving, List<int> cells)
        {
            cells.Clear();
            var spot = new Spot { Rotation = Quaternion.identity, LocalRotation = Quaternion.identity, Reason = "" };
            if (!IsReady || definition == null)
            {
                spot.Reason = IsReady ? "" : "La grille se prépare…";
                return spot;
            }

            var mine = moving != null ? moving.Socket : null;
            if (definition.slot == BuildSlot.Tower)
                return EvaluateTower(definition, point, steps, mine);

            if (!TryCellAt(point, out var x, out var z) || m_Kind[z * m_Width + x] == CellKind.Void)
            {
                spot.Reason = "Vise le sol près d'une voie";
                return spot;
            }

            // Le sens : celui de la case (arrondi au cran le plus proche), puis les crans du joystick.
            var index = z * m_Width + x;
            var kind = m_Kind[index];
            var baseStep = kind == CellKind.Blocked ? Mathf.RoundToInt(FacingToward(point) / 45f) : Mathf.RoundToInt(m_Facing[index] / 45f);
            var quarter = ((baseStep + steps) % 8 + 8) % 8;
            return EvaluateAt(definition, point, index, quarter, mine, null, cells, spot);
        }

        /// <summary>
        /// Où irait ce bâtiment à la place de cet emplacement, tourné comme lui, et peut-il s'y poser ? Un duo naît à la place d'une
        /// des deux tours (31 octobre), sur ses cases à lui (celles de la tour du dessous) : <paramref name="alsoFree"/> est
        /// l'emplacement de l'autre tour, qui sera libéré.
        /// </summary>
        public Spot EvaluateInPlace(StructureDefinition definition, BuildSocket socket, BuildSocket alsoFree, List<int> cells)
        {
            cells.Clear();
            var spot = new Spot { Rotation = Quaternion.identity, LocalRotation = Quaternion.identity, Reason = "" };
            if (!IsReady || definition == null || socket == null || !TryCellAt(socket.transform.position, out var x, out var z))
            {
                spot.Reason = "Pas constructible ici";
                return spot;
            }

            var quarter = (Mathf.RoundToInt(socket.transform.eulerAngles.y / 45f) % 8 + 8) % 8;
            return EvaluateAt(definition, socket.transform.position, z * m_Width + x, quarter, socket, alsoFree, cells, spot);
        }

        // La place d'un bâtiment centré près de ce point (sur la case index), tourné de quarter crans de 45° : ses cases, et ce qui
        // l'empêche. Les cases de mine et alsoMine (le bâtiment déplacé, l'autre tour d'un duo) comptent comme libres.
        Spot EvaluateAt(StructureDefinition definition, Vector3 point, int index, int quarter, BuildSocket mine, BuildSocket alsoMine,
                        List<int> cells, Spot spot)
        {
            var cell = m_GridCellSize;
            var yaw = quarter * 45f;
            var footprint = FootprintOf(definition);
            var aligned = quarter % 2 == 0;
            var across = quarter % 4 == 2;
            var extentX = across ? footprint.y : footprint.x;
            var extentZ = across ? footprint.x : footprint.y;

            // Le centre : calé pour que les cases du bâtiment tombent juste (au milieu d'une case pour un nombre impair, sur une
            // ligne de la grille pour un nombre pair) ; en biais, au milieu de la case visée.
            var center = CellCenter(index);
            if (aligned)
            {
                if (extentX % 2 == 0)
                    center.x = m_Origin.x + Mathf.Round((point.x - m_Origin.x) / cell) * cell;
                if (extentZ % 2 == 0)
                    center.z = m_Origin.y + Mathf.Round((point.z - m_Origin.y) / cell) * cell;
            }

            var rotation = Quaternion.Euler(0f, yaw, 0f);
            CollectCells(center, rotation, footprint, cells);
            spot.OnGrid = true;
            spot.Rotation = rotation;
            spot.Size = new Vector2(footprint.x * cell, footprint.y * cell);

            // Chaque case doit convenir au bâtiment, être libre et à peu près au même niveau que les autres.
            var low = float.MaxValue;
            var high = float.MinValue;
            var sum = 0f;
            var heights = 0;
            var taken = false;
            var wrongKind = false;
            var blocked = false;
            var why = BlockReason.None;
            Collider culprit = null;
            var fitting = BuildSlot.Roadside;
            var hasFitting = false;
            if (cells.Count == 0)
                blocked = true;
            foreach (var i in cells)
            {
                if (i < 0)
                {
                    blocked = true;
                    continue;
                }

                var cellKind = m_Kind[i];
                if (cellKind == CellKind.Void || cellKind == CellKind.Blocked)
                {
                    blocked = true;
                    if (why == BlockReason.None || i == index)
                    {
                        why = m_Why[i];
                        culprit = m_Culprit[i];
                    }
                }
                else if (!definition.Fits(SlotOf(cellKind)))
                    wrongKind = true;
                else if (!hasFitting || i == index)
                {
                    fitting = SlotOf(cellKind);
                    hasFitting = true;
                }

                var occupant = m_Occupant[i];
                if (occupant != null && occupant != mine && occupant != alsoMine && occupant.Structure != null)
                    taken = true;

                if (!float.IsNaN(m_Height[i]))
                {
                    low = Mathf.Min(low, m_Height[i]);
                    high = Mathf.Max(high, m_Height[i]);
                    sum += m_Height[i];
                    heights++;
                }
            }

            spot.Position = new Vector3(center.x, heights > 0 ? sum / heights : point.y, center.z);
            spot.Slot = fitting;
            if (taken)
                spot.Reason = "Place prise";
            else if (blocked)
                spot.Reason = ReasonText(why) + CulpritText(culprit);
            else if (wrongKind || !hasFitting)
                spot.Reason = "Pas ici : se pose " + WhereText(definition);
            else if (high - low > m_MaxStep)
                spot.Reason = "Le sol n'est pas plat";
            else
                spot.Valid = true;
            return spot;
        }

        // Ce qui empêche de construire sur une case (2 novembre au soir : le joueur voit ce qui bloque, et peut le dire).
        static string ReasonText(BlockReason why) => why switch
        {
            BlockReason.Obstacle => "Pas ici : un arbre ou un rocher",
            BlockReason.Slope => "Pas ici : le sol n'est pas plat",
            BlockReason.Tower => "Pas ici : trop près de la tour",
            BlockReason.Spawn => "Pas ici : les ennemis arrivent là",
            BlockReason.ChestSpot => "Pas ici : la place d'un coffre",
            BlockReason.Overflow => "Pas ici : ça déborde sur la voie",
            BlockReason.TooHigh => "Pas ici : trop haut",
            BlockReason.TooLow => "Pas ici : trop bas",
            _ => "Pas constructible ici",
        };

        // En test (2 novembre) : le nom de ce qui bloque, et de son parent, pour que le joueur puisse le dire (« cliff_rock (3) ·
        // Cliffs »). Rien dans le jeu final.
        static string CulpritText(Collider culprit)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (culprit == null)
                return "";
            var parent = culprit.transform.parent;
            return " (" + culprit.name + (parent != null ? " · " + parent.name : "") + ")";
#else
            return "";
#endif
        }

        Spot EvaluateTower(StructureDefinition definition, Vector3 point, int steps, BuildSocket mine)
        {
            var spot = new Spot { Rotation = Quaternion.identity, LocalRotation = Quaternion.Euler(0f, steps * 45f, 0f), Reason = "" };
            var socket = TowerSocketAt(point, 2.5f);
            if (socket == null)
            {
                spot.Reason = m_TowerSockets.Count > 0 ? "Se pose sur la tour : vise une case dorée" : "Pas d'emplacement sur la tour";
                return spot;
            }

            spot.OnGrid = true;
            spot.TowerSocket = socket;
            spot.Position = socket.transform.position;
            spot.Rotation = socket.transform.rotation * spot.LocalRotation;
            spot.Slot = BuildSlot.Tower;
            spot.Size = new Vector2(m_TowerSlotSize, m_TowerSlotSize);
            if (socket.Structure != null && socket != mine)
                spot.Reason = "Place prise";
            else
                spot.Valid = true;
            return spot;
        }

        // Les cases dont le centre est dans le rectangle du bâtiment (posé là, tourné ainsi). -1 pour une case hors de la grille.
        void CollectCells(Vector3 center, Quaternion rotation, Vector2Int footprint, List<int> cells)
        {
            var cell = m_GridCellSize;
            var halfX = footprint.x * cell * 0.5f - 0.01f;
            var halfZ = footprint.y * cell * 0.5f - 0.01f;
            var reach = Mathf.CeilToInt(Mathf.Max(footprint.x, footprint.y) * 0.75f) + 1;
            var inverse = Quaternion.Inverse(rotation);
            var cx = Mathf.FloorToInt((center.x - m_Origin.x) / cell);
            var cz = Mathf.FloorToInt((center.z - m_Origin.y) / cell);
            for (var z = cz - reach; z <= cz + reach; z++)
            {
                for (var x = cx - reach; x <= cx + reach; x++)
                {
                    var local = inverse * new Vector3(m_Origin.x + (x + 0.5f) * cell - center.x, 0f, m_Origin.y + (z + 0.5f) * cell - center.z);
                    if (Mathf.Abs(local.x) > halfX || Mathf.Abs(local.z) > halfZ)
                        continue;
                    cells.Add(x >= 0 && z >= 0 && x < m_Width && z < m_Depth ? z * m_Width + x : -1);
                }
            }
        }

        /// <summary>
        /// L'emplacement d'un bâtiment posé à cette place : celui de la tour, ou un emplacement créé là, qui prend ces cases
        /// jusqu'à ce que le bâtiment soit déplacé (<see cref="Release"/>).
        /// </summary>
        public BuildSocket Claim(Spot spot, List<int> cells)
        {
            if (spot.TowerSocket != null)
                return spot.TowerSocket;

            var socket = CreateSocket(spot.Position, spot.Rotation, spot.Slot, spot.Size);
            var claimed = new List<int>();
            foreach (var i in cells)
            {
                if (i < 0)
                    continue;
                m_Occupant[i] = socket;
                claimed.Add(i);
            }

            m_Claims[socket] = claimed.ToArray();
            ClearUnder(socket, spot.Position, spot.Rotation, spot.Size);
            Version++;
            return socket;
        }

        /// <summary>
        /// Rend les cases de cet emplacement s'il est vide (après un déplacement, ou une construction refusée) et le retire. Les
        /// emplacements de la tour restent.
        /// </summary>
        public void Release(BuildSocket socket)
        {
            if (socket == null || socket.Structure != null || !m_Claims.TryGetValue(socket, out var cells))
                return;

            foreach (var i in cells)
            {
                if (m_Occupant[i] == socket)
                    m_Occupant[i] = null;
            }

            m_Claims.Remove(socket);
            if (m_Cleared.TryGetValue(socket, out var hidden))
            {
                foreach (var cleared in hidden)
                {
                    if (cleared != null)
                        cleared.SetActive(true);
                }

                m_Cleared.Remove(socket);
            }

            Destroy(socket.gameObject);
            Version++;
        }

        /// <summary>
        /// L'emplacement prend ces cases à la place des siennes, et se place au centre de cette place (un duo, 31 octobre : il a
        /// les cases de la tour du dessous, qui ne sont pas toujours celles de la tour qu'il remplace).
        /// </summary>
        public void Reclaim(BuildSocket socket, Spot spot, List<int> cells)
        {
            if (socket == null || !m_Claims.TryGetValue(socket, out var old))
                return;

            foreach (var i in old)
            {
                if (m_Occupant[i] == socket)
                    m_Occupant[i] = null;
            }

            var claimed = new List<int>();
            foreach (var i in cells)
            {
                if (i < 0)
                    continue;
                m_Occupant[i] = socket;
                claimed.Add(i);
            }

            m_Claims[socket] = claimed.ToArray();
            socket.transform.SetPositionAndRotation(spot.Position, spot.Rotation);
            socket.SetupCandidate(spot.Slot, spot.Size);
            if (socket.TryGetComponent<BoxCollider>(out var plate))
                plate.size = new Vector3(spot.Size.x * 0.95f, plate.size.y, spot.Size.y * 0.95f);
            Version++;
        }

        /// <summary>L'emplacement du bâtiment posé sur la case de ce point (pour l'améliorer en visant le sol), ou null.</summary>
        public BuildSocket OccupantAt(Vector3 point)
        {
            if (!TryCellAt(point, out var x, out var z))
                return null;

            var socket = m_Occupant[z * m_Width + x];
            return socket != null && socket.Structure != null ? socket : null;
        }

        /// <summary>L'emplacement de la tour le plus proche de ce point, à moins de <paramref name="reach"/> m (à plat), ou null.</summary>
        public BuildSocket TowerSocketAt(Vector3 point, float reach)
        {
            BuildSocket best = null;
            var bestDistance = reach * reach;
            foreach (var socket in m_TowerSockets)
            {
                if (socket == null || !socket.isActiveAndEnabled)
                    continue;

                var offset = point - socket.transform.position;
                if (Mathf.Abs(offset.y) > 2f)
                    continue;
                var distance = Flat(offset);
                if (distance < bestDistance)
                {
                    best = socket;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// Une place libre pour ce bâtiment au bord de la voie, la plus proche de la tour, à au moins <paramref name="spacing"/> m
        /// de celles déjà prises dans <paramref name="taken"/> ; son emplacement est créé (raccourci F de la démo). Null s'il n'y
        /// en a plus.
        /// </summary>
        public BuildSocket ClaimFree(StructureDefinition definition, List<Vector3> taken, float spacing)
        {
            if (!IsReady || definition == null)
                return null;

            foreach (var index in m_Roadside)
            {
                var center = CellCenter(index);
                if (m_Occupant[index] != null || IsNearAny(center, taken, spacing))
                    continue;

                var spot = Evaluate(definition, center, 0, null, m_Scratch);
                if (!spot.Valid)
                    continue;

                taken.Add(spot.Position);
                return Claim(spot, m_Scratch);
            }

            return null;
        }

        // ---------------------------------------------------------------- pour le dessin de la grille

        /// <summary>Les cases autour d'un point : la première et le nombre en X et en Z (bornées à la grille).</summary>
        internal void CellRange(Vector3 center, float radius, out int fromX, out int fromZ, out int toX, out int toZ)
        {
            var cell = m_GridCellSize;
            fromX = Mathf.Max(0, Mathf.FloorToInt((center.x - radius - m_Origin.x) / cell));
            fromZ = Mathf.Max(0, Mathf.FloorToInt((center.z - radius - m_Origin.y) / cell));
            toX = Mathf.Min(m_Width - 1, Mathf.FloorToInt((center.x + radius - m_Origin.x) / cell));
            toZ = Mathf.Min(m_Depth - 1, Mathf.FloorToInt((center.z + radius - m_Origin.y) / cell));
        }

        internal int CellIndex(int x, int z) => z * m_Width + x;

        /// <summary>La case existe et a du sol (elle se dessine).</summary>
        internal bool HasGround(int index) => index >= 0 && index < m_Kind.Length && m_Kind[index] != CellKind.Void;

        /// <summary>Ce bâtiment peut aller sur cette case (sa sorte lui convient, elle est libre).</summary>
        internal bool Accepts(int index, StructureDefinition definition, Structure moving)
        {
            var kind = m_Kind[index];
            if (definition == null || kind == CellKind.Void || kind == CellKind.Blocked || !definition.Fits(SlotOf(kind)))
                return false;

            var occupant = m_Occupant[index];
            return occupant == null || occupant.Structure == null || (moving != null && occupant == moving.Socket);
        }

        /// <summary>Les quatre coins d'une case au ras du sol (sud-ouest, sud-est, nord-ouest, nord-est). Faux pour une marche.</summary>
        internal bool TryCellCorners(int index, out Vector3 a, out Vector3 b, out Vector3 c, out Vector3 d)
        {
            var x = index % m_Width;
            var z = index / m_Width;
            var cell = m_GridCellSize;
            var height = m_Height[index];
            a = Corner(x, z, height);
            b = Corner(x + 1, z, height);
            c = Corner(x, z + 1, height);
            d = Corner(x + 1, z + 1, height);
            var low = Mathf.Min(Mathf.Min(a.y, b.y), Mathf.Min(c.y, d.y));
            var high = Mathf.Max(Mathf.Max(a.y, b.y), Mathf.Max(c.y, d.y));
            return !float.IsNaN(height) && high - low <= Mathf.Max(0.8f, cell);
        }

        Vector3 Corner(int x, int z, float fallback)
        {
            var height = m_Corner[z * (m_Width + 1) + x];
            if (float.IsNaN(height) || Mathf.Abs(height - fallback) > 1.5f)
                height = fallback;
            return new Vector3(m_Origin.x + x * m_GridCellSize, height, m_Origin.y + z * m_GridCellSize);
        }

        // ---------------------------------------------------------------- outils

        bool TryCellAt(Vector3 point, out int x, out int z)
        {
            x = Mathf.FloorToInt((point.x - m_Origin.x) / m_GridCellSize);
            z = Mathf.FloorToInt((point.z - m_Origin.y) / m_GridCellSize);
            return IsReady && x >= 0 && z >= 0 && x < m_Width && z < m_Depth;
        }

        Vector3 CellCenter(int index)
        {
            var x = index % m_Width;
            var z = index / m_Width;
            var height = m_Height[index];
            return new Vector3(m_Origin.x + (x + 0.5f) * m_GridCellSize, float.IsNaN(height) ? 0f : height,
                               m_Origin.y + (z + 0.5f) * m_GridCellSize);
        }

        static BuildSlot SlotOf(CellKind kind) => kind switch
        {
            CellKind.Lane => BuildSlot.Lane,
            CellKind.Height => BuildSlot.Height,
            _ => BuildSlot.Roadside,
        };

        /// <summary>Où se pose ce bâtiment, en toutes lettres (« sur la voie », « au bord de la voie ou en hauteur »…).</summary>
        public static string WhereText(StructureDefinition definition) =>
            definition == null ? "" : definition.anywhere ? "sur la voie, au bord ou en hauteur"
            : definition.slot == BuildSlot.Lane ? "sur la voie"
            : definition.slot == BuildSlot.Roadside ? "au bord de la voie ou en hauteur"
            : BuildSocket.NameOf(definition.slot).ToLowerInvariant();

        // Un bâtiment posé sur une case qui n'a pas de sens à elle (hors des zones) regarde le joueur.
        static float FacingToward(Vector3 point)
        {
            var rig = PlayerRig.Instance;
            var from = rig != null ? rig.BodyPosition : point + Vector3.back;
            var toward = from - point;
            return toward.sqrMagnitude > 1e-4f ? Mathf.Atan2(toward.x, toward.z) * Mathf.Rad2Deg : 0f;
        }

        static float Flat(Vector3 offset) => offset.x * offset.x + offset.z * offset.z;

        static bool IsNear(Vector3 a, Vector3 b, float distance) => Flat(a - b) < distance * distance;

        static bool IsNearAny(Vector3 point, List<Vector3> others, float distance)
        {
            foreach (var other in others)
            {
                if (IsNear(point, other, distance))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Le point de la voie la plus proche, <paramref name="back"/> m plus près de l'apparition des ennemis (la trappe,
        /// 28 octobre). Faux si la carte n'a pas de voie.
        /// </summary>
        public bool TryPointBack(Vector3 from, float back, out Vector3 point)
        {
            point = from;
            MapScan.Lane nearest = null;
            var best = float.MaxValue;
            var along = 0f;
            foreach (var lane in m_Lanes)
            {
                var distance = lane.DistanceTo(from, out var laneAlong);
                if (distance >= best)
                    continue;

                best = distance;
                along = laneAlong;
                nearest = lane;
            }

            if (nearest == null)
                return false;

            point = nearest.PointAt(Mathf.Max(0f, along - back), out _);
            return true;
        }

        Vector3 TowerPosition()
        {
            var tower = Tower.Instance;
            return tower != null ? tower.transform.position : transform.position;
        }
    }
}
