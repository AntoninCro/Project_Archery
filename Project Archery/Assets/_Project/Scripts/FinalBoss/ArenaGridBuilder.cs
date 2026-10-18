using System.Collections.Generic;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Le sol d'une arène en cubes, dessiné case par case dans un fichier texte (une lettre par case) : roche, plateau en
    /// hauteur, sol, lave, coulée, piliers, estrade du dragon, emplacements de tourelle, arrivée du joueur et porte.
    /// Le script fabrique les blocs, dans l'éditeur comme en jeu, et les repères (arrivée, porte, dragon, tourelles).
    /// </summary>
    /// <remarks>
    /// L'objet est le centre de la grille, au niveau du sol. La première ligne du plan est au fond (+Z, axe bleu, côté
    /// dragon), la dernière côté porte (-Z). Modifier le fichier refait le sol aussitôt ; la légende est en tête du fichier.
    /// Une rangée de roche entoure toujours le plan : les parois sont provisoires, en attendant le plafond.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [SelectionBase]
    public partial class ArenaGridBuilder : ArenaShape
    {
        enum Cell
        {
            Rock,
            Floor,
            Plateau,
            Lava,
            PlateauLava,
            Pillar,
            Dais,
            Turret,
            Spawn,
            Door,
        }

        [Header("Plan")]
        [Tooltip("Le plan : un fichier texte, une ligne par rangée et une lettre par case. Légende en tête du fichier.")]
        [SerializeField]
        TextAsset m_Layout;

        [Header("Tailles")]
        [Tooltip("Côté d'une case, donc d'un cube (m). 2,5 : 75 m pour 30 cases ; 3 : la grille de la carte (90 m), mais la lave devient trop large pour la sauter.")]
        [Min(0.5f)]
        [SerializeField]
        float m_CellSize = 2.5f;

        [Tooltip("Hauteur du plateau, en cubes.")]
        [Min(1)]
        [SerializeField]
        int m_PlateauLevels = 2;

        [Tooltip("Hauteur des parois autour du plan, en cubes (provisoire, en attendant le plafond).")]
        [Min(1)]
        [SerializeField]
        int m_WallLevels = 6;

        [Tooltip("Hauteur des piliers, en cubes.")]
        [Min(1)]
        [SerializeField]
        int m_PillarLevels = 4;

        [Tooltip("Hauteur de l'estrade du dragon (m).")]
        [SerializeField]
        float m_DaisHeight = 1f;

        [Tooltip("Profondeur de la lave sous le sol, ou sous le plateau pour une coulée (m).")]
        [SerializeField]
        float m_LavaDepth = 0.4f;

        [Header("Plafond")]
        [Tooltip("Ferme la grotte par une voûte en cubes : basse près des parois, haute au centre. Les parois montent jusqu'à elle.")]
        [SerializeField]
        bool m_Ceiling = true;

        [Tooltip("Hauteur de la voûte près des parois, en cubes (9 : 22,5 m avec des cubes de 2,5 m).")]
        [Min(2)]
        [SerializeField]
        int m_CeilingLowLevels = 9;

        [Tooltip("Hauteur de la voûte au centre, en cubes (13 : 32,5 m). Le dragon vole vers 13 m.")]
        [Min(2)]
        [SerializeField]
        int m_CeilingHighLevels = 13;

        [Tooltip("Nombre de cases, depuis la paroi, pour que la voûte monte d'un cube.")]
        [Min(1)]
        [SerializeField]
        int m_CeilingRise = 2;

        [Header("Matériaux")]
        [SerializeField]
        Material m_FloorMaterial;

        [Tooltip("Plateau, parois.")]
        [SerializeField]
        Material m_RockMaterial;

        [SerializeField]
        Material m_PillarMaterial;

        [SerializeField]
        Material m_LavaMaterial;

        [Tooltip("Face de la coulée qui tombe du plateau.")]
        [SerializeField]
        Material m_LavaFallMaterial;

        [Tooltip("Décoché : rien n'est fabriqué (par exemple une fois le sol figé en cubes modifiables).")]
        [SerializeField]
        bool m_Generate = true;

        static readonly int k_ArenaLavaGlowId = Shader.PropertyToID("_ArenaLavaGlow");
        static readonly int k_LavaFallCutoffId = Shader.PropertyToID("_LavaFallCutoffY");

        static readonly Vector2Int[] k_Sides = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1) };

        readonly List<Pose> m_TurretSlots = new List<Pose>();
        Cell[,] m_Cells = new Cell[0, 0];
        int m_Width;
        int m_Height;
        int m_UnknownLetters;
        Pose m_Spawn = Pose.identity;
        Pose m_Door = Pose.identity;
        Pose m_Dragon = Pose.identity;
        GeneratedRoot m_Generated;
        string m_BuiltText;
        bool m_Dirty;

        /// <summary>Côté d'une case (m).</summary>
        public float CellSize => m_CellSize;

        /// <summary>Matériau des piliers (obsidienne).</summary>
        public Material PillarMaterial => m_PillarMaterial;

        /// <summary>Arrivée du joueur (case S), regard vers le fond.</summary>
        public Pose SpawnPoint => ToWorld(m_Spawn);

        /// <summary>Milieu de la porte (cases P).</summary>
        public Pose DoorPoint => ToWorld(m_Door);

        /// <summary>Milieu de l'estrade du dragon (cases D), sur l'estrade, tourné vers la porte.</summary>
        public Pose DragonPoint => ToWorld(m_Dragon);

        /// <summary>Emplacements de tourelle (cases T), sur le plateau, tournés vers le centre de l'arène.</summary>
        public IEnumerable<Pose> TurretSlots
        {
            get
            {
                foreach (var slot in m_TurretSlots)
                    yield return ToWorld(slot);
            }
        }

        float PlateauTop => m_PlateauLevels * m_CellSize;

        void OnEnable()
        {
            m_Generated ??= new GeneratedRoot(transform, "Sol (généré)");

            // Lave allumée et coulées entières (l'Arena Director les règle pendant l'intro du combat).
            Shader.SetGlobalFloat(k_ArenaLavaGlowId, 1f);
            Shader.SetGlobalFloat(k_LavaFallCutoffId, -100000f);
            Build();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += EditorPoll;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorPoll;
#endif
            m_Generated?.Clear();
        }

        void OnValidate() => m_Dirty = true;

        void Update()
        {
            PulseCracks();
            if (!m_Dirty)
                return;

            m_Dirty = false;
            Build();
        }

#if UNITY_EDITOR
        double m_NextPoll;

        // Le plan a été modifié et enregistré (ou un réglage a changé) : on refait le sol.
        void EditorPoll()
        {
            if (this == null || Application.isPlaying || UnityEditor.EditorApplication.timeSinceStartup < m_NextPoll)
                return;

            m_NextPoll = UnityEditor.EditorApplication.timeSinceStartup + 0.5;
            if (m_Dirty || (m_Layout != null ? m_Layout.text : null) != m_BuiltText)
            {
                m_Dirty = false;
                Build();
            }
        }
#endif

        // ---------------------------------------------------------------- pour le combat (ArenaShape)

        float m_RoamRadius = 20f;
        float m_CeilingRadius = 24f;
        float m_LightLevel = 1f;
        readonly List<Pose> m_PlateauPerches = new List<Pose>();
        readonly List<Pose> m_Perches = new List<Pose>();
        Pose m_LavaFallTop = Pose.identity;
        bool m_HasLavaFall;

        /// <summary>Jusqu'à la case de sol (ou de lave) la plus éloignée du centre.</summary>
        public override float RoamRadius => m_RoamRadius;

        /// <summary>Jusqu'à la case la plus éloignée sous la voûte.</summary>
        public override float CeilingRadius => m_CeilingRadius;

        public override Material LavaMaterial => m_LavaMaterial;

        /// <summary>Les coulées tombent du plateau.</summary>
        public override float LavaFallTopY => transform.position.y + PlateauTop;

        /// <summary>Le décor éclaire la lave (lumières orangées) : l'Arena Director n'ajoute pas les siennes.</summary>
        public override bool HasOwnLavaLights => m_Generate && m_Decorate;

        /// <summary>Les piliers du plan portent un <see cref="ArenaPillar"/>, comme ceux posés à la main.</summary>
        public override IReadOnlyList<Pillar> Pillars => HandPlacedPillars();

        /// <summary>Dans la coulée, un peu en retrait du bord du plateau d'où elle tombe.</summary>
        public override bool TryGetLavaFallTop(out Pose pose)
        {
            pose = ToWorld(m_LavaFallTop);
            return m_HasLavaFall;
        }

        /// <summary>Le dessus des piliers, et le bord du plateau là où il domine l'arène.</summary>
        public override IReadOnlyList<Pose> Perches
        {
            get
            {
                m_Perches.Clear();
                AddPillarPerches(m_Perches);
                foreach (var perch in m_PlateauPerches)
                    m_Perches.Add(ToWorld(perch));
                return m_Perches;
            }
        }

        /// <summary>Ce point (monde) est-il dans une case de lave ?</summary>
        public override bool IsLavaAt(Vector3 worldPosition)
        {
            var local = transform.InverseTransformPoint(worldPosition);
            var cell = CellAt(local, out _);
            return IsLava(cell) && local.y < Top(cell) + 1.2f;
        }

        /// <summary>Sol de plain-pied tout autour (ni lave, ni plateau, ni paroi), loin des piliers.</summary>
        public override bool IsFreeGround(Vector3 point, float margin)
        {
            var local = transform.InverseTransformPoint(point);
            if (!AllAround(local, margin, cell => cell == Cell.Floor || cell == Cell.Dais || cell == Cell.Spawn || cell == Cell.Door))
                return false;

            foreach (var pillar in Pillars)
            {
                var offset = point - pillar.Position;
                offset.y = 0f;
                if (offset.magnitude < pillar.Radius + margin)
                    return false;
            }

            return true;
        }

        /// <summary>Le dessous de la voûte au-dessus de ce point (au-dessus d'une paroi : de la case la plus proche vers le centre).</summary>
        public override Vector3 CeilingPoint(Vector3 floorPoint, out Vector3 inward)
        {
            inward = -transform.up;
            var local = transform.InverseTransformPoint(floorPoint);
            var flat = new Vector3(local.x, 0f, local.z);
            for (var i = 0; i < 128 && CellAt(flat, out _) == Cell.Rock; i++)
                flat = Vector3.MoveTowards(flat, Vector3.zero, m_CellSize * 0.5f);

            CellAt(flat, out var coords);
            var height = CeilingAt(coords.x, coords.y);
            if (float.IsInfinity(height))
                height = (m_Ceiling ? m_CeilingHighLevels : m_WallLevels) * m_CellSize;
            if (m_Ceiling && m_Decorate && m_RockSkin)
                height -= m_SkinDepth * 0.9f;
            return transform.TransformPoint(new Vector3(flat.x, height, flat.z));
        }

        /// <summary>Ramené vers le centre tant qu'il touche le plateau, une paroi ou un pilier (la lave ne gêne pas le dragon).</summary>
        public override Vector3 Confine(Vector3 point, float margin)
        {
            var local = transform.InverseTransformPoint(point);
            var flat = new Vector3(local.x, 0f, local.z);
            for (var i = 0; i < 240 && !AllAround(flat, margin, cell => IsWalkable(cell) || cell == Cell.Lava); i++)
                flat = Vector3.MoveTowards(flat, Vector3.zero, 0.25f);
            return transform.TransformPoint(new Vector3(flat.x, local.y, flat.z));
        }

        /// <summary>Le sol sous ce point : l'estrade est plus haute, la lave un peu plus basse.</summary>
        public override float GroundHeightAt(Vector3 point)
        {
            var cell = CellAt(transform.InverseTransformPoint(point), out _);
            return transform.position.y + (cell == Cell.Dais ? m_DaisHeight : cell == Cell.Lava ? -m_LavaDepth : 0f);
        }

        /// <summary>Le dessus de chaque case de lave (monde), au fond de l'arène comme sur le plateau.</summary>
        public void GetLavaSurface(List<Vector3> points)
        {
            points.Clear();
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    if (IsLava(m_Cells[x, y]))
                        points.Add(transform.TransformPoint(CellCenter(x, y) + Vector3.up * Top(m_Cells[x, y])));
                }
            }
        }

        /// <summary>
        /// Le pied des coulées qui tombent du plateau (monde) : au bord de la lave du bas, tourné vers l'arène, une pose
        /// par case de coulée.
        /// </summary>
        public void GetLavaFallFeet(List<Pose> feet)
        {
            feet.Clear();
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    if (m_Cells[x, y] != Cell.PlateauLava)
                        continue;

                    foreach (var side in k_Sides)
                    {
                        if (Get(x + side.x, y + side.y) != Cell.Lava)
                            continue;

                        var facing = new Vector3(side.x, 0f, -side.y);
                        var foot = CellCenter(x, y) + facing * (m_CellSize * 0.5f) + Vector3.up * Top(Cell.Lava);
                        feet.Add(ToWorld(new Pose(foot, Quaternion.LookRotation(facing))));
                    }
                }
            }
        }

        /// <summary>Pendant l'intro : les lumières du décor, les feux, les braises et les craquelures suivent l'allumage.</summary>
        public override void SetLightLevel(float level)
        {
            m_LightLevel = Mathf.Max(0f, level);
            ApplyDecorLight();
        }

        // Le point et quatre points autour, à « margin » mètres : tous dans des cases qui conviennent.
        bool AllAround(Vector3 local, float margin, System.Func<Cell, bool> accepts)
        {
            if (!accepts(CellAt(local, out _)))
                return false;
            if (margin <= 0f)
                return true;

            return accepts(CellAt(local + new Vector3(margin, 0f, 0f), out _)) && accepts(CellAt(local - new Vector3(margin, 0f, 0f), out _)) &&
                   accepts(CellAt(local + new Vector3(0f, 0f, margin), out _)) && accepts(CellAt(local - new Vector3(0f, 0f, margin), out _));
        }

        // Rayons utiles au combat, tirés du plan.
        void MeasureArena()
        {
            m_RoamRadius = 0f;
            m_CeilingRadius = 0f;
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    var cell = m_Cells[x, y];
                    var center = CellCenter(x, y);
                    var distance = new Vector2(center.x, center.z).magnitude;
                    if (IsWalkable(cell) || cell == Cell.Lava)
                        m_RoamRadius = Mathf.Max(m_RoamRadius, distance);
                    if (cell != Cell.Rock)
                        m_CeilingRadius = Mathf.Max(m_CeilingRadius, distance);
                }
            }
        }

        // Perchoirs du dragon sur le plateau : une case du bord qui domine l'arène (sol ou lave devant), avec du plateau
        // derrière pour son corps, de la place sous la voûte, loin des tourelles et espacés d'au moins 9 m.
        void MeasurePerches()
        {
            m_PlateauPerches.Clear();
            var cs = m_CellSize;
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    if (m_Cells[x, y] != Cell.Plateau || IsNearTurret(x, y))
                        continue;

                    var open = Vector3.zero;
                    foreach (var side in k_Sides)
                    {
                        var neighbour = Get(x + side.x, y + side.y);
                        if (IsWalkable(neighbour) || neighbour == Cell.Lava)
                            open += new Vector3(side.x, 0f, -side.y);
                    }

                    if (open.sqrMagnitude < 0.01f)
                        continue;

                    open.Normalize();
                    var center = CellCenter(x, y);
                    if (!IsPlateau(CellAt(center - open * (cs * 1.5f), out _)) || !IsPlateau(CellAt(center - open * (cs * 2.5f), out _)))
                        continue;
                    if (CeilingAt(x, y) - PlateauTop < 10f)
                        continue;

                    var position = center - open * (cs * 0.5f) + Vector3.up * PlateauTop;
                    var spaced = true;
                    foreach (var perch in m_PlateauPerches)
                        spaced &= (perch.position - position).sqrMagnitude >= 9f * 9f;
                    foreach (var slot in m_TurretSlots)
                        spaced &= (slot.position - position).sqrMagnitude >= 6f * 6f;
                    if (spaced)
                        m_PlateauPerches.Add(new Pose(position, Quaternion.LookRotation(open)));
                }
            }
        }

        // Le haut de la coulée : les cases de lave du plateau qui tombent dans la lave du sol. Le dragon s'y tient pendant
        // l'intro, une case en retrait du bord, les pattes dans le courant, tourné vers l'arène.
        void MeasureLavaFall()
        {
            var sum = Vector3.zero;
            var facing = Vector3.zero;
            var count = 0;
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    if (m_Cells[x, y] != Cell.PlateauLava)
                        continue;

                    foreach (var side in k_Sides)
                    {
                        if (Get(x + side.x, y + side.y) != Cell.Lava)
                            continue;
                        sum += CellCenter(x, y);
                        facing += new Vector3(side.x, 0f, -side.y);
                        count++;
                    }
                }
            }

            m_HasLavaFall = count > 0 && facing.sqrMagnitude > 0.01f;
            if (!m_HasLavaFall)
                return;

            facing.Normalize();
            var position = sum / count - facing * (m_CellSize * 1.2f);
            position.y = PlateauTop - m_LavaDepth;
            m_LavaFallTop = new Pose(position, Quaternion.LookRotation(facing));
        }

        static bool IsPlateau(Cell cell) => cell == Cell.Plateau || cell == Cell.Turret;

        // Une case près d'un perchoir du plateau : le décor la laisse libre pour le dragon.
        bool IsNearPerch(int x, int y)
        {
            var center = CellCenter(x, y) + Vector3.up * PlateauTop;
            foreach (var perch in m_PlateauPerches)
            {
                if ((perch.position - center).sqrMagnitude < m_CellSize * m_CellSize * 2.3f)
                    return true;
            }

            return false;
        }
        // ---------------------------------------------------------------- plan

        void Parse()
        {
            m_BuiltText = m_Layout != null ? m_Layout.text : null;
            var rows = new List<string>();
            if (m_BuiltText != null)
            {
                foreach (var raw in m_BuiltText.Split('\n'))
                {
                    var line = raw.TrimEnd('\r');
                    if (line.Trim().Length == 0 || line.TrimStart().StartsWith("//"))
                        continue;
                    rows.Add(line);
                }
            }

            m_Height = rows.Count;
            m_Width = 0;
            foreach (var row in rows)
                m_Width = Mathf.Max(m_Width, row.Length);

            m_Cells = new Cell[m_Width, m_Height];
            m_UnknownLetters = 0;
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                    m_Cells[x, y] = x < rows[y].Length ? Letter(rows[y][x]) : Cell.Rock;
            }
        }

        Cell Letter(char letter)
        {
            switch (letter)
            {
                case '#':
                case ' ':
                    return Cell.Rock;
                case '.':
                    return Cell.Floor;
                case '^':
                    return Cell.Plateau;
                case '~':
                    return Cell.Lava;
                case '!':
                    return Cell.PlateauLava;
                case 'o':
                case 'O':
                    return Cell.Pillar;
                case 'D':
                    return Cell.Dais;
                case 'T':
                    return Cell.Turret;
                case 'S':
                    return Cell.Spawn;
                case 'P':
                    return Cell.Door;
                default:
                    m_UnknownLetters++;
                    return Cell.Rock;
            }
        }

        // Hors du plan, c'est de la roche : une rangée de parois l'entoure toujours.
        Cell Get(int x, int y) => x >= 0 && y >= 0 && x < m_Width && y < m_Height ? m_Cells[x, y] : Cell.Rock;

        // Centre de la case au niveau du sol, dans le repère de l'objet : la première ligne est au fond (+Z).
        Vector3 CellCenter(int x, int y) =>
            new Vector3((x - (m_Width - 1) * 0.5f) * m_CellSize, 0f, ((m_Height - 1) * 0.5f - y) * m_CellSize);

        Cell CellAt(Vector3 local, out Vector2Int coords)
        {
            coords = new Vector2Int(Mathf.RoundToInt(local.x / m_CellSize + (m_Width - 1) * 0.5f),
                                    Mathf.RoundToInt((m_Height - 1) * 0.5f - local.z / m_CellSize));
            return Get(coords.x, coords.y);
        }

        float Top(Cell cell)
        {
            switch (cell)
            {
                case Cell.Rock:
                    return m_WallLevels * m_CellSize;
                case Cell.Plateau:
                case Cell.Turret:
                    return PlateauTop;
                case Cell.PlateauLava:
                    return PlateauTop - m_LavaDepth;
                case Cell.Lava:
                    return -m_LavaDepth;
                case Cell.Pillar:
                    return m_PillarLevels * m_CellSize;
                case Cell.Dais:
                    return m_DaisHeight;
                default:
                    return 0f;
            }
        }

        // ---------------------------------------------------------------- voûte

        int[,] m_WallDistance = new int[0, 0];

        // Distance (en cases) de chaque case à la paroi la plus proche : la voûte monte en s'en éloignant.
        void MeasureCeiling()
        {
            m_WallDistance = new int[m_Width, m_Height];
            var queue = new Queue<Vector2Int>();
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    m_WallDistance[x, y] = int.MaxValue;
                    if (m_Cells[x, y] == Cell.Rock)
                        m_WallDistance[x, y] = 0;
                    else if (x == 0 || y == 0 || x == m_Width - 1 || y == m_Height - 1)
                        m_WallDistance[x, y] = 1;
                    else
                        continue;
                    queue.Enqueue(new Vector2Int(x, y));
                }
            }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var side in k_Sides)
                {
                    var next = cell + side;
                    if (next.x < 0 || next.y < 0 || next.x >= m_Width || next.y >= m_Height || m_WallDistance[next.x, next.y] <= m_WallDistance[cell.x, cell.y] + 1)
                        continue;
                    m_WallDistance[next.x, next.y] = m_WallDistance[cell.x, cell.y] + 1;
                    queue.Enqueue(next);
                }
            }
        }

        // Hauteur de la voûte au-dessus d'une case (m) : basse près des parois, plus haute vers le centre, par cubes, avec
        // quelques marches irrégulières. Infinie sur la roche (pleine) et sans voûte.
        float CeilingAt(int x, int y)
        {
            if (!m_Ceiling || Get(x, y) == Cell.Rock || m_WallDistance.Length == 0)
                return float.PositiveInfinity;

            var levels = m_CeilingLowLevels + (m_WallDistance[x, y] - 1) / m_CeilingRise;
            if (m_Decorate)
            {
                var jitter = Hash(x, y, m_DecorSeed + 21);
                levels += jitter < 0.2f ? -1 : jitter > 0.85f ? 1 : 0;
            }

            return Mathf.Clamp(levels, m_CeilingLowLevels, Mathf.Max(m_CeilingLowLevels, m_CeilingHighLevels)) * m_CellSize;
        }

        // Sous la voûte, une paroi s'arrête au plafond de la case voisine, et deux cases de roche n'ont pas de face entre elles.
        bool TryGetFaceTop(Cell cell, int x, int y, Vector2Int side, float top, out float faceTop)
        {
            faceTop = top;
            if (cell != Cell.Rock || !m_Ceiling)
                return true;

            if (Get(x + side.x, y + side.y) == Cell.Rock)
                return false;

            faceTop = Mathf.Min(top, CeilingAt(x + side.x, y + side.y));
            return true;
        }

        // La voûte : le dessous du plafond de chaque case, et une marche là où la voisine monte plus haut. En roche à
        // facettes avec le décor, plate sinon. Vue d'en haut, elle est invisible : on voit toujours l'arène dans la vue Scene.
        void BuildCeiling(MeshBuilder builder)
        {
            if (!m_Ceiling)
                return;

            var half = m_CellSize * 0.5f;
            var rough = m_Decorate && m_RockSkin;
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    if (m_Cells[x, y] == Cell.Rock)
                        continue;

                    var center = CellCenter(x, y);
                    var height = CeilingAt(x, y);
                    var x0 = center.x - half;
                    var x1 = center.x + half;
                    var z0 = center.z - half;
                    var z1 = center.z + half;
                    if (rough)
                    {
                        AddCeilingSkin(builder, x, y, x0, x1, z0, z1, height);
                    }
                    else
                    {
                        builder.Quad(new Vector3(x0, height, z0), new Vector3(x1, height, z0), new Vector3(x1, height, z1), new Vector3(x0, height, z1), Color.white);
                    }

                    foreach (var side in k_Sides)
                    {
                        var other = CeilingAt(x + side.x, y + side.y);
                        if (Get(x + side.x, y + side.y) == Cell.Rock || other <= height + 0.001f)
                            continue;

                        // Une face pleine, et la roche devant (comme les murs) : jamais de fente vers le dessus de la voûte.
                        AddSide(builder, side, x0, x1, z0, z1, height, other, false);
                        if (rough)
                            AddSkin(builder, side, x0, x1, z0, z1, height, other);
                    }
                }
            }
        }

        // La roche n'est fabriquée que là où on la voit : à côté d'une case qui n'est pas de la roche.
        bool IsVisibleRock(int x, int y)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (Get(x + dx, y + dy) != Cell.Rock)
                        return true;
                }
            }

            return false;
        }

        static bool IsLava(Cell cell) => cell == Cell.Lava || cell == Cell.PlateauLava;

        // ---------------------------------------------------------------- fabrication

        void Build()
        {
            if (m_Generated == null)
                return;

            var root = m_Generated.Begin();
            Parse();
            FindMarkers();
            MeasureDais();
            MeasureCeiling();
            MeasureArena();
            MeasurePerches();
            MeasureLavaFall();
            m_PillarSpots.Clear();
            if (m_UnknownLetters > 0)
                Debug.LogWarning($"ArenaGridBuilder : {m_UnknownLetters} lettre(s) inconnue(s) dans le plan, remplacée(s) par de la roche.", this);
            if (!m_Generate || m_Width == 0 || m_Height == 0)
                return;

            var floor = new MeshBuilder();
            var floorDark = new MeshBuilder();
            var scorched = new MeshBuilder();
            var rock = new MeshBuilder();
            var pillar = new MeshBuilder();
            var lava = new MeshBuilder();
            var fall = new MeshBuilder();
            var half = m_CellSize * 0.5f;

            for (var y = -1; y <= m_Height; y++)
            {
                for (var x = -1; x <= m_Width; x++)
                {
                    var cell = Get(x, y);
                    if (cell == Cell.Rock && !IsVisibleRock(x, y))
                        continue;

                    var center = CellCenter(x, y);
                    var top = TopAt(x, y);
                    var x0 = center.x - half;
                    var x1 = center.x + half;
                    var z0 = center.z - half;
                    var z1 = center.z + half;

                    // Le dessus de la case : pour le sol, dalle normale, dalle sombre ou sol brûlé.
                    var topBuilder = IsLava(cell) ? lava : cell == Cell.Pillar ? pillar : cell == Cell.Rock || cell == Cell.Plateau || cell == Cell.Turret ? rock : floor;
                    if (topBuilder == floor)
                    {
                        var floorMaterial = FloorMaterialFor(x, y);
                        topBuilder = floorMaterial != null && floorMaterial == m_ScorchedMaterial ? scorched
                            : floorMaterial != null && floorMaterial == m_FloorDarkMaterial ? floorDark
                            : floor;
                    }

                    // La texture du sol (ses craquelures) s'étale sur plusieurs cases, sans se répéter à chaque cube.
                    var isFloor = topBuilder == floor || topBuilder == floorDark || topBuilder == scorched;
                    var uvScale = IsLava(cell) ? 1f / 4f : isFloor ? 1f / m_FloorTextureSize : 1f / m_CellSize;

                    // Sous la voûte, le dessus des parois est caché : pas la peine de le fabriquer.
                    if (cell != Cell.Rock || !m_Ceiling)
                        topBuilder.Quad(new Vector3(x0, top, z1), new Vector3(x1, top, z1), new Vector3(x1, top, z0), new Vector3(x0, top, z0), Color.white,
                                        new Vector2(x0, z1) * uvScale, new Vector2(x1, z1) * uvScale, new Vector2(x1, z0) * uvScale, new Vector2(x0, z0) * uvScale);

                    // Les côtés, là où la case voisine est plus basse.
                    foreach (var side in k_Sides)
                    {
                        var neighbour = Get(x + side.x, y + side.y);
                        var from = TopAt(x + side.x, y + side.y);
                        if (!TryGetFaceTop(cell, x, y, side, top, out var faceTop) || from >= faceTop - 0.001f)
                            continue;

                        var sideBuilder = cell == Cell.PlateauLava && IsLava(neighbour) ? fall : cell == Cell.Pillar ? pillar : rock;
                        AddSide(sideBuilder, side, x0, x1, z0, z1, from, faceTop, sideBuilder == fall);

                        // Derrière la coulée, une paroi de roche (5 cm en retrait) : pendant l'intro, tant que la lave n'a
                        // pas coulé, on voit la roche et pas le vide à travers.
                        if (sideBuilder == fall)
                        {
                            const float inset = 0.05f;
                            AddSide(rock, side, side.x < 0 ? x0 + inset : x0, side.x > 0 ? x1 - inset : x1,
                                    side.y > 0 ? z0 + inset : z0, side.y < 0 ? z1 - inset : z1, from, faceTop, false);
                        }
                    }
                }
            }

            m_CrackedFloors.Clear();
            m_CrackedFloors.Add(m_Generated.CreateMesh("Sol", root, floor.ToMesh("Arène en cubes (sol)"), m_FloorMaterial, collider: true));
            if (floorDark.VertexCount > 0)
                m_CrackedFloors.Add(m_Generated.CreateMesh("Sol (dalles sombres)", root, floorDark.ToMesh("Arène en cubes (dalles sombres)"), m_FloorDarkMaterial, collider: true));
            if (scorched.VertexCount > 0)
                m_CrackedFloors.Add(m_Generated.CreateMesh("Sol (brûlé)", root, scorched.ToMesh("Arène en cubes (sol brûlé)"), m_ScorchedMaterial, collider: true));
            m_Generated.CreateMesh("Roche et plateau", root, rock.ToMesh("Arène en cubes (roche)"), m_RockMaterial, collider: true, castShadows: true);
            m_Generated.CreateMesh("Piliers", root, pillar.ToMesh("Arène en cubes (piliers)"), m_PillarMaterial, collider: true, castShadows: true);
            m_Generated.CreateMesh("Lave", root, lava.ToMesh("Arène en cubes (lave)"), m_LavaMaterial, collider: true);
            if (fall.VertexCount > 0)
                m_Generated.CreateMesh("Coulées", root, fall.ToMesh("Arène en cubes (coulées)"), m_LavaFallMaterial, collider: false);

            // Les parois et les piliers habillés de roche à facettes, comme les falaises de la carte.
            var rockSkin = new MeshBuilder();
            var pillarSkin = new MeshBuilder();
            BuildSkins(rockSkin, pillarSkin);
            if (rockSkin.VertexCount > 0)
                m_Generated.CreateMesh("Parois rocheuses", root, rockSkin.ToMesh("Arène en cubes (parois rocheuses)"), m_RockMaterial, collider: true, castShadows: true);
            if (pillarSkin.VertexCount > 0)
                m_Generated.CreateMesh("Piliers rocheux", root, pillarSkin.ToMesh("Arène en cubes (piliers rocheux)"), m_PillarMaterial, collider: true, castShadows: true);

            // La voûte, qui ferme la grotte.
            var ceiling = new MeshBuilder();
            BuildCeiling(ceiling);
            if (ceiling.VertexCount > 0)
                m_Generated.CreateMesh("Plafond", root, ceiling.ToMesh("Arène en cubes (plafond)"), m_RockMaterial, collider: true);

            CreatePillars(root);
            CreateMarkers(root);
            Decorate(root);
            ApplyDecorLight();
        }

        // Une face verticale de la case, du haut de la voisine jusqu'au haut de la case (sens horaire vu de l'extérieur).
        static void AddSide(MeshBuilder builder, Vector2Int side, float x0, float x1, float z0, float z1, float bottom, float top, bool flowing)
        {
            var v0 = flowing ? bottom * 0.5f : bottom / Mathf.Max(0.01f, x1 - x0);
            var v1 = flowing ? top * 0.5f : top / Mathf.Max(0.01f, x1 - x0);
            Vector3 a, b, c, d;
            if (side.x > 0)
            {
                a = new Vector3(x1, bottom, z0); b = new Vector3(x1, top, z0); c = new Vector3(x1, top, z1); d = new Vector3(x1, bottom, z1);
            }
            else if (side.x < 0)
            {
                a = new Vector3(x0, bottom, z1); b = new Vector3(x0, top, z1); c = new Vector3(x0, top, z0); d = new Vector3(x0, bottom, z0);
            }
            else if (side.y < 0)
            {
                // Rangée du dessus dans le plan : vers le fond (+Z).
                a = new Vector3(x1, bottom, z1); b = new Vector3(x1, top, z1); c = new Vector3(x0, top, z1); d = new Vector3(x0, bottom, z1);
            }
            else
            {
                a = new Vector3(x0, bottom, z0); b = new Vector3(x0, top, z0); c = new Vector3(x1, top, z0); d = new Vector3(x1, bottom, z0);
            }

            // Une coulée large de plusieurs cases ne fait qu'une : sa texture continue d'une case à l'autre (2 m par motif,
            // comme sur la hauteur), au lieu de recommencer à chaque case.
            var u0 = 0f;
            var u1 = 1f;
            if (flowing)
            {
                u0 = (side.x != 0 ? a.z : a.x) * 0.5f;
                u1 = (side.x != 0 ? c.z : c.x) * 0.5f;
            }

            builder.Quad(a, b, c, d, Color.white, new Vector2(u0, v0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, v0));
        }

        // Chaque groupe de cases « o » est un pilier : le dragon le contourne, le souffle et les boules de feu s'y arrêtent.
        void CreatePillars(Transform root)
        {
            var seen = new bool[m_Width, m_Height];
            var stack = new Stack<Vector2Int>();
            var index = 0;
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    if (seen[x, y] || m_Cells[x, y] != Cell.Pillar)
                        continue;

                    var sum = Vector3.zero;
                    var count = 0;
                    stack.Push(new Vector2Int(x, y));
                    seen[x, y] = true;
                    while (stack.Count > 0)
                    {
                        var cell = stack.Pop();
                        sum += CellCenter(cell.x, cell.y);
                        count++;
                        foreach (var side in k_Sides)
                        {
                            var next = cell + side;
                            if (next.x < 0 || next.y < 0 || next.x >= m_Width || next.y >= m_Height || seen[next.x, next.y] || m_Cells[next.x, next.y] != Cell.Pillar)
                                continue;
                            seen[next.x, next.y] = true;
                            stack.Push(next);
                        }
                    }

                    var go = m_Generated.Create($"Pilier {++index}", root);
                    go.transform.localPosition = sum / count;
                    var arenaPillar = go.AddComponent<ArenaPillar>();
                    arenaPillar.Radius = Mathf.Sqrt(count) * m_CellSize * 0.6f + (m_Decorate && m_RockSkin ? m_SkinDepth * 0.6f : 0f);
                    arenaPillar.Height = m_PillarLevels * m_CellSize;
                    m_PillarSpots.Add(new Vector4(go.transform.localPosition.x, 0f, go.transform.localPosition.z, arenaPillar.Radius));
                }
            }
        }

        // Les repères tirés du plan : arrivée (S), porte (P), dragon (D), tourelles (T).
        void FindMarkers()
        {
            m_TurretSlots.Clear();
            var door = Vector3.zero;
            var dais = Vector3.zero;
            var doorCount = 0;
            var daisCount = 0;
            var spawnFound = false;
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    var center = CellCenter(x, y);
                    switch (m_Cells[x, y])
                    {
                        case Cell.Spawn:
                            m_Spawn = new Pose(center, Quaternion.identity);
                            spawnFound = true;
                            break;
                        case Cell.Door:
                            door += center;
                            doorCount++;
                            break;
                        case Cell.Dais:
                            dais += center;
                            daisCount++;
                            break;
                        case Cell.Turret:
                            var position = center + Vector3.up * PlateauTop;
                            var toCenter = new Vector3(-position.x, 0f, -position.z);
                            m_TurretSlots.Add(new Pose(position, toCenter.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCenter) : Quaternion.identity));
                            break;
                    }
                }
            }

            if (!spawnFound)
                m_Spawn = new Pose(Vector3.zero, Quaternion.identity);
            m_Door = new Pose(doorCount > 0 ? door / doorCount : Vector3.zero, Quaternion.identity);
            m_Dragon = new Pose(daisCount > 0 ? dais / daisCount + Vector3.up * m_DaisHeight : Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
        }

        void CreateMarkers(Transform root)
        {
            var markers = m_Generated.Create("Repères", root).transform;
            Marker("Arrivée du joueur", markers, m_Spawn);
            Marker("Porte", markers, m_Door);
            Marker("Dragon", markers, m_Dragon);
            for (var i = 0; i < m_TurretSlots.Count; i++)
                Marker($"Tourelle {i + 1}", markers, m_TurretSlots[i]);
        }

        void Marker(string markerName, Transform parent, Pose pose)
        {
            var go = m_Generated.Create(markerName, parent);
            go.transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
        }

        Pose ToWorld(Pose local) => new Pose(transform.TransformPoint(local.position), transform.rotation * local.rotation);

        // ---------------------------------------------------------------- éditeur

#if UNITY_EDITOR
        [ContextMenu("Figer le sol en cubes modifiables")]
        void FreezeIntoCubes()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("ArenaGridBuilder : on fige le sol hors du jeu (pas en Play).", this);
                return;
            }

            Parse();
            FindMarkers();
            MeasureDais();
            MeasureCeiling();
            var frozen = new GameObject("Sol (modifiable)");
            frozen.transform.SetParent(transform, false);
            var cubes = new GameObject("Cubes").transform;
            cubes.SetParent(frozen.transform, false);

            // Un cube par case, du dessous du sol jusqu'au haut de la case : on peut le monter, le baisser, le changer.
            for (var y = -1; y <= m_Height; y++)
            {
                for (var x = -1; x <= m_Width; x++)
                {
                    var cell = Get(x, y);
                    if (cell == Cell.Rock && !IsVisibleRock(x, y))
                        continue;

                    var top = TopAt(x, y);
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"{cell} {x},{y}";
                    cube.transform.SetParent(cubes, false);
                    cube.transform.localPosition = CellCenter(x, y) + Vector3.up * (top - m_CellSize) * 0.5f;
                    cube.transform.localScale = new Vector3(m_CellSize, top + m_CellSize, m_CellSize);
                    cube.GetComponent<MeshRenderer>().sharedMaterial = IsLava(cell) ? m_LavaMaterial
                        : cell == Cell.Pillar ? m_PillarMaterial
                        : cell == Cell.Rock || cell == Cell.Plateau || cell == Cell.Turret ? m_RockMaterial
                        : x >= 0 && y >= 0 && x < m_Width && y < m_Height ? FloorMaterialFor(x, y)
                        : m_FloorMaterial;
                    UnityEditor.GameObjectUtility.SetStaticEditorFlags(cube, UnityEditor.StaticEditorFlags.BatchingStatic);
                }
            }

            var markers = new GameObject("Repères").transform;
            markers.SetParent(frozen.transform, false);
            FrozenMarker("Arrivée du joueur", markers, m_Spawn);
            FrozenMarker("Porte", markers, m_Door);
            FrozenMarker("Dragon", markers, m_Dragon);
            for (var i = 0; i < m_TurretSlots.Count; i++)
                FrozenMarker($"Tourelle {i + 1}", markers, m_TurretSlots[i]);

            // Les piliers : un repère « Arena Pillar » par groupe, comme ceux qui sont générés.
            Build();
            var generated = m_Generated?.Root;
            if (generated != null)
            {
                foreach (var source in generated.GetComponentsInChildren<ArenaPillar>())
                {
                    var copy = new GameObject(source.name);
                    copy.transform.SetParent(markers, false);
                    copy.transform.localPosition = source.transform.localPosition;
                    var arenaPillar = copy.AddComponent<ArenaPillar>();
                    arenaPillar.Radius = source.Radius;
                    arenaPillar.Height = source.Height;
                }
            }

            // La roche à facettes des parois et des piliers : ses maillages sont enregistrés dans Art/Arena.
            var rockSkin = new MeshBuilder();
            var pillarSkin = new MeshBuilder();
            BuildSkins(rockSkin, pillarSkin);
            FreezeMesh(frozen.transform, "Parois rocheuses", rockSkin, m_RockMaterial);
            FreezeMesh(frozen.transform, "Piliers rocheux", pillarSkin, m_PillarMaterial);
            var ceiling = new MeshBuilder();
            BuildCeiling(ceiling);
            FreezeMesh(frozen.transform, "Plafond", ceiling, m_RockMaterial);

            // Le décor, figé lui aussi (les modèles restent liés à leurs fichiers).
            m_DecorSaved = true;
            try
            {
                Decorate(frozen.transform);
            }
            finally
            {
                m_DecorSaved = false;
            }

            UnityEditor.Undo.RegisterCreatedObjectUndo(frozen, "Figer le sol");
            UnityEditor.Undo.RecordObject(this, "Figer le sol");
            m_Generate = false;
            UnityEditor.EditorUtility.SetDirty(this);
            Build();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("ArenaGridBuilder : sol figé dans « Sol (modifiable) », un cube par case. Enregistre la scène.", frozen);
        }

        static void FreezeMesh(Transform parent, string objectName, MeshBuilder builder, Material material)
        {
            if (builder.VertexCount == 0)
                return;

            const string folder = "Assets/_Project/Art/Arena";
            var mesh = builder.ToMesh($"Fourche ({objectName})");
            UnityEditor.AssetDatabase.CreateAsset(mesh, UnityEditor.AssetDatabase.GenerateUniqueAssetPath($"{folder}/Fourche {objectName}.asset"));
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(go, UnityEditor.StaticEditorFlags.BatchingStatic);
        }

        static void FrozenMarker(string markerName, Transform parent, Pose pose)
        {
            var go = new GameObject(markerName);
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(pose.position, pose.rotation);
        }
#endif

        void OnDrawGizmos()
        {
            if (m_Width == 0)
                return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.9f, 0.5f, 0.9f);
            Gizmos.DrawWireSphere(m_Spawn.position + Vector3.up * 0.9f, 0.6f);
            Gizmos.DrawLine(m_Spawn.position + Vector3.up * 0.9f, m_Spawn.position + Vector3.up * 0.9f + m_Spawn.forward * 2f);
            Gizmos.DrawWireCube(m_Door.position + Vector3.up * 1.5f, new Vector3(3f * m_CellSize, 3f, 0.3f));
            Gizmos.color = new Color(1f, 0.25f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(m_Dragon.position + Vector3.up * 2f, 2f);
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.9f);
            foreach (var slot in m_TurretSlots)
                Gizmos.DrawWireCube(slot.position + Vector3.up * m_CellSize * 0.5f, Vector3.one * m_CellSize * 0.8f);
        }

        // Le quadrillage des cases au niveau du sol, objet sélectionné.
        void OnDrawGizmosSelected()
        {
            if (m_Width == 0)
                return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 1f, 1f, 0.15f);
            var half = m_CellSize * 0.5f;
            var corner = CellCenter(0, 0) + new Vector3(-half, 0.02f, half);
            var width = m_Width * m_CellSize;
            var depth = m_Height * m_CellSize;
            for (var x = 0; x <= m_Width; x++)
                Gizmos.DrawLine(corner + new Vector3(x * m_CellSize, 0f, 0f), corner + new Vector3(x * m_CellSize, 0f, -depth));
            for (var y = 0; y <= m_Height; y++)
                Gizmos.DrawLine(corner + new Vector3(0f, 0f, -y * m_CellSize), corner + new Vector3(width, 0f, -y * m_CellSize));

            // Les perchoirs du dragon sur le plateau : un disque et la direction de son regard.
            Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.9f);
            foreach (var perch in m_PlateauPerches)
            {
                Gizmos.DrawWireSphere(perch.position, 1.5f);
                Gizmos.DrawLine(perch.position, perch.position + perch.rotation * Vector3.forward * 4f);
            }

            // Là où le dragon attend pendant l'intro, en haut de la coulée.
            if (m_HasLavaFall)
            {
                Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
                Gizmos.DrawWireSphere(m_LavaFallTop.position, 2f);
                Gizmos.DrawLine(m_LavaFallTop.position, m_LavaFallTop.position + m_LavaFallTop.rotation * Vector3.forward * 6f);
            }
        }
    }
}
