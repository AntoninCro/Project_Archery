using System.Collections.Generic;
using UnityEngine;

namespace Archery.FinalBoss
{
    // Le décor de l'arène en cubes, tiré du plan : dalles, sol brûlé, parois rocheuses, rochers,
    // stalagmites, nid du dragon, porte, braseros, socles des tourelles, lumières et braises de la lave. Il se refait avec le sol ; la graine change
    // sa disposition sans toucher au plan.
    public partial class ArenaGridBuilder
    {
        enum Fit
        {
            Height,
            Footprint,
            Length,
        }

        [Header("Décor")]
        [Tooltip("Habille l'arène : dalles, sol brûlé, parois et piliers rocheux, rochers, stalagmites, nid du dragon, " +
                 "porte, braseros, socles des tourelles, lumières et braises de la lave.")]
        [SerializeField]
        bool m_Decorate = true;

        [Tooltip("Change la disposition du décor (rochers, stalagmites, armes…) sans toucher au plan.")]
        [SerializeField]
        int m_DecorSeed = 11;

        [Tooltip("Quantité de rochers et de stalagmites (1 : normal).")]
        [Range(0f, 2f)]
        [SerializeField]
        float m_DecorDensity = 1f;

        [Tooltip("Part des dalles plus sombres sur le sol (0 à 1) : on voit les cubes.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_DarkTiles = 0.3f;

        [SerializeField]
        Material m_FloorDarkMaterial;

        [Tooltip("Sol brûlé : au bord de la lave et autour du nid du dragon.")]
        [SerializeField]
        Material m_ScorchedMaterial;

        [Header("Décor : craquelures du sol")]
        [Tooltip("Surface (m) couverte par la texture du sol (ses craquelures rouges) avant qu'elle se répète.")]
        [Min(1f)]
        [SerializeField]
        float m_FloorTextureSize = 8f;

        [Tooltip("En jeu, la lueur des craquelures respire : de combien elle varie (0 : fixe).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_CrackPulse = 0.25f;

        [Tooltip("Durée (s) d'une respiration des craquelures.")]
        [Min(0.5f)]
        [SerializeField]
        float m_CrackPulsePeriod = 4f;

        [Header("Décor : modèles")]
        [Tooltip("Gros rochers, au pied des parois, du plateau et des piliers.")]
        [SerializeField]
        GameObject[] m_BigRocks = System.Array.Empty<GameObject>();

        [Tooltip("Petits rochers plats, au bord de la lave et éparpillés.")]
        [SerializeField]
        GameObject[] m_SmallRocks = System.Array.Empty<GameObject>();

        [Tooltip("Stalagmites : dans les recoins du sol et sur le plateau.")]
        [SerializeField]
        GameObject[] m_Stalagmites = System.Array.Empty<GameObject>();

        [Tooltip("Matériau des rochers et des stalagmites. Vide : celui des modèles.")]
        [SerializeField]
        Material m_DecorRockMaterial;

        [Tooltip("Colonnes brisées autour du nid du dragon.")]
        [SerializeField]
        GameObject[] m_Ruins = System.Array.Empty<GameObject>();

        [Tooltip("Statue renversée près du nid.")]
        [SerializeField]
        GameObject m_FallenStatue;

        [Tooltip("Armes et boucliers d'aventuriers tombés, autour du nid.")]
        [SerializeField]
        GameObject[] m_Remains = System.Array.Empty<GameObject>();

        [Tooltip("Matériau des armes et des boucliers (éclairé, avec la texture du pack). Ceux du pack utilisent un shader 2D qui " +
                 "ne réagit pas à la lumière : ils brilleraient dans le noir de l'intro.")]
        [SerializeField]
        Material m_RemainsMaterial;

        [Tooltip("Colonne de chaque côté de la porte.")]
        [SerializeField]
        GameObject m_DoorColumn;

        [Tooltip("Battant sombre de la porte.")]
        [SerializeField]
        Material m_DoorMaterial;

        [Tooltip("Base des braseros du couloir.")]
        [SerializeField]
        GameObject m_Brazier;

        [Tooltip("Flammes des braseros et braises de la lave (Fire_Particles).")]
        [SerializeField]
        Material m_FireMaterial;

        [Tooltip("Socle de pierre sous chaque emplacement de tourelle.")]
        [SerializeField]
        GameObject m_TurretPlinth;

        [Header("Décor : lumières")]
        [SerializeField]
        Color m_LavaLightColor = new Color(1f, 0.42f, 0.12f);

        [Tooltip("Distance minimale (m) entre deux lumières de la lave.")]
        [SerializeField]
        float m_LavaLightSpacing = 7f;

        [SerializeField]
        int m_MaxLavaLights = 14;

        [Tooltip("Braises qui montent de la lave.")]
        [SerializeField]
        bool m_Embers = true;

        [Header("Décor : parois rocheuses")]
        [Tooltip("Habille toutes les faces verticales (parois, plateau, piliers, estrade, berges de la lave) d'une roche à " +
                 "facettes, comme les falaises de la carte, au lieu de faces plates.")]
        [SerializeField]
        bool m_RockSkin = true;

        [Tooltip("De combien (m) la roche avance au plus devant la face plate.")]
        [Range(0.05f, 1f)]
        [SerializeField]
        float m_SkinDepth = 0.45f;

        [Tooltip("Taille (m) des facettes.")]
        [Range(0.2f, 1.5f)]
        [SerializeField]
        float m_SkinFacet = 0.5f;

        [Tooltip("Relief des strates horizontales (m), comme les couches des falaises de la carte.")]
        [Range(0f, 0.4f)]
        [SerializeField]
        float m_SkinStrata = 0.12f;

        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // Piliers du plan : centre (repère de l'objet) et rayon, pour poser des rochers à leur pied.
        readonly List<Vector4> m_PillarSpots = new List<Vector4>();
        readonly List<Renderer> m_CrackedFloors = new List<Renderer>();
        readonly List<Light> m_DecorLights = new List<Light>();
        readonly List<float> m_DecorLightIntensities = new List<float>();
        readonly List<ParticleSystem> m_DecorFires = new List<ParticleSystem>();
        MaterialPropertyBlock m_CrackBlock;
        int[,] m_DaisDistance = new int[0, 0];

        // Décor figé (enregistré dans la scène, liens vers les modèles gardés) plutôt que généré.
        bool m_DecorSaved;

        HideFlags DecorFlags => m_DecorSaved || Application.isPlaying ? HideFlags.None : HideFlags.DontSave;

        // ---------------------------------------------------------------- sol : dalles et sol brûlé

        // Distance (en cases) de chaque case à l'estrade du dragon.
        void MeasureDais()
        {
            m_DaisDistance = new int[m_Width, m_Height];
            var dais = new List<Vector2Int>();
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    if (m_Cells[x, y] == Cell.Dais)
                        dais.Add(new Vector2Int(x, y));
                }
            }

            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    var best = int.MaxValue;
                    foreach (var cell in dais)
                        best = Mathf.Min(best, Mathf.Max(Mathf.Abs(cell.x - x), Mathf.Abs(cell.y - y)));
                    m_DaisDistance[x, y] = best;
                }
            }
        }

        int DaisDistance(int x, int y) => x >= 0 && y >= 0 && x < m_Width && y < m_Height ? m_DaisDistance[x, y] : int.MaxValue;

        // Brûlé : à côté de la lave (diagonales comprises), et sur et autour de l'estrade du dragon.
        bool IsScorched(int x, int y)
        {
            if (DaisDistance(x, y) <= 1)
                return true;

            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (IsLava(Get(x + dx, y + dy)))
                        return true;
                }
            }

            return false;
        }

        // Le matériau du dessus d'une case de sol : brûlé, dalle sombre ou sol normal.
        Material FloorMaterialFor(int x, int y)
        {
            if (!m_Decorate)
                return m_FloorMaterial;
            if (m_ScorchedMaterial != null && IsScorched(x, y))
                return m_ScorchedMaterial;
            if (m_FloorDarkMaterial != null && Hash(x, y, m_DecorSeed) < m_DarkTiles)
                return m_FloorDarkMaterial;
            return m_FloorMaterial;
        }

        // En jeu, les craquelures du sol respirent : leur lueur monte et descend lentement, comme un sol brûlant.
        void PulseCracks()
        {
            if (!Application.isPlaying || m_CrackedFloors.Count == 0)
                return;

            // Elles suivent aussi l'allumage de l'arène : éteintes dans le noir de l'intro.
            m_CrackBlock ??= new MaterialPropertyBlock();
            var factor = (1f + m_CrackPulse * Mathf.Sin(Time.time * Mathf.PI * 2f / m_CrackPulsePeriod)) * m_LightLevel;
            foreach (var floor in m_CrackedFloors)
            {
                if (floor == null || floor.sharedMaterial == null || !floor.sharedMaterial.HasProperty(k_EmissionColorId))
                    continue;
                floor.GetPropertyBlock(m_CrackBlock);
                m_CrackBlock.SetColor(k_EmissionColorId, floor.sharedMaterial.GetColor(k_EmissionColorId) * factor);
                floor.SetPropertyBlock(m_CrackBlock);
            }
        }

        // Hauteur du haut d'une case. Avec le décor, le haut des parois est irrégulier, par demi-cubes.
        float TopAt(int x, int y)
        {
            var cell = Get(x, y);

            // Sous la voûte, les parois montent au-dessus du plus haut plafond (leurs faces s'arrêtent au plafond voisin).
            if (cell == Cell.Rock && m_Ceiling)
                return (Mathf.Max(m_CeilingLowLevels, m_CeilingHighLevels) + 2) * m_CellSize;

            var top = Top(cell);
            if (m_Decorate && cell == Cell.Rock)
                top += Mathf.Floor(Hash(x, y, m_DecorSeed + 7) * 4f) * m_CellSize * 0.5f;
            return top;
        }

        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                var h = x * 73856093 ^ y * 19349663 ^ seed * 83492791;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        // ---------------------------------------------------------------- décor

        void Decorate(Transform root)
        {
            if (!m_Decorate || m_Width == 0 || m_Height == 0)
                return;

            if (!m_DecorSaved)
            {
                m_DecorLights.Clear();
                m_DecorLightIntensities.Clear();
                m_DecorFires.Clear();
            }

            var decor = NewObject("Décor", root).transform;
            DecorateGround(NewObject("Rochers", decor).transform, new System.Random(m_DecorSeed));
            DecoratePillars(NewObject("Pied des piliers", decor).transform, new System.Random(m_DecorSeed + 1));
            DecorateNest(NewObject("Nid du dragon", decor).transform, new System.Random(m_DecorSeed + 2));
            DecorateDoor(NewObject("Porte et braseros", decor).transform);
            DecorateTurrets(NewObject("Socles des tourelles", decor).transform);
            DecorateLava(NewObject("Lave", decor).transform);
            DecorateCeiling(NewObject("Stalactites", decor).transform, new System.Random(m_DecorSeed + 3));
        }

        // Rochers au pied des parois, petits rochers au bord de la lave, stalagmites et rochers dans les recoins,
        // stalagmites et rochers sur le plateau. Rien près de l'arrivée, de la porte, du nid ni des tourelles.
        void DecorateGround(Transform parent, System.Random random)
        {
            var density = m_DecorDensity;
            var walls = new List<Vector3>();
            var banks = new List<Vector3>();
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    var cell = m_Cells[x, y];
                    var center = CellCenter(x, y);
                    walls.Clear();
                    banks.Clear();
                    foreach (var side in k_Sides)
                    {
                        var neighbour = Get(x + side.x, y + side.y);
                        var direction = new Vector3(side.x, 0f, -side.y);
                        if (neighbour == Cell.Rock || neighbour == Cell.Plateau || neighbour == Cell.Turret || neighbour == Cell.PlateauLava)
                            walls.Add(direction);
                        else if (neighbour == Cell.Lava || IsWalkable(neighbour))
                            banks.Add(direction);
                    }

                    if (cell == Cell.Floor && !IsQuiet(x, y) && DaisDistance(x, y) > 2)
                        DecorateFloorCell(parent, random, center, walls, density, x, y);
                    else if (cell == Cell.Plateau && !IsNearTurret(x, y))
                        DecoratePlateauCell(parent, random, center, banks, density, IsNearPerch(x, y));
                }
            }
        }

        void DecorateFloorCell(Transform parent, System.Random random, Vector3 center, List<Vector3> walls, float density, int x, int y)
        {
            var cs = m_CellSize;

            // Un recoin (deux parois) : une stalagmite, ou parfois un gros rocher.
            if (walls.Count >= 2)
            {
                var corner = center + (walls[0] + walls[1]).normalized * cs * 0.32f;
                if (Chance(random, 0.5f * density))
                    Place(Pick(random, m_Stalagmites), parent, corner, 0f, Range(random, 0f, 360f), Range(random, 2.4f, 4.5f), Fit.Height, 0.15f, m_DecorRockMaterial, true);
                else if (Chance(random, 0.3f * density))
                    Place(Pick(random, m_BigRocks), parent, corner, 0f, Range(random, 0f, 360f), Range(random, 1.2f, 1.9f), Fit.Footprint, 0.12f, m_DecorRockMaterial, true);
            }

            // Au pied d'une paroi ou du plateau : un gros rocher.
            if (walls.Count >= 1 && Chance(random, 0.42f * density))
            {
                var wall = walls[random.Next(walls.Count)];
                var along = new Vector3(wall.z, 0f, -wall.x) * Range(random, -0.3f, 0.3f) * cs;
                Place(Pick(random, m_BigRocks), parent, center + wall * cs * 0.3f + along, 0f, Range(random, 0f, 360f),
                      Range(random, 0.9f, 1.7f), Fit.Footprint, 0.12f, m_DecorRockMaterial, true);
            }

            // Au bord de la lave : de petits rochers plats ; ailleurs, quelques cailloux éparpillés.
            var lavaSides = 0;
            foreach (var side in k_Sides)
            {
                if (Get(x + side.x, y + side.y) == Cell.Lava)
                    lavaSides++;
            }

            if (lavaSides > 0 && Chance(random, 0.35f * density))
            {
                var toLava = Vector3.zero;
                foreach (var side in k_Sides)
                {
                    if (Get(x + side.x, y + side.y) == Cell.Lava)
                        toLava += new Vector3(side.x, 0f, -side.y);
                }

                Place(Pick(random, m_SmallRocks), parent, center + toLava.normalized * cs * 0.36f, 0f, Range(random, 0f, 360f),
                      Range(random, 0.45f, 0.85f), Fit.Footprint, 0.05f, m_DecorRockMaterial, false);
            }
            else if (walls.Count == 0 && lavaSides == 0 && Chance(random, 0.035f * density))
            {
                var offset = new Vector3(Range(random, -0.3f, 0.3f), 0f, Range(random, -0.3f, 0.3f)) * cs;
                Place(Pick(random, m_SmallRocks), parent, center + offset, 0f, Range(random, 0f, 360f),
                      Range(random, 0.35f, 0.7f), Fit.Footprint, 0.04f, m_DecorRockMaterial, false);
            }
        }

        // Près d'un perchoir du dragon (keepClear), les tirages sont les mêmes mais rien n'est posé : le reste du décor
        // ne change pas.
        void DecoratePlateauCell(Transform parent, System.Random random, Vector3 center, List<Vector3> lower, float density, bool keepClear)
        {
            var cs = m_CellSize;
            var top = PlateauTop;

            // Au bord du plateau, au-dessus de l'arène : des rochers qui débordent un peu.
            if (lower.Count > 0 && Chance(random, 0.13f * density))
            {
                var model = Pick(random, m_BigRocks);
                var position = center + lower[random.Next(lower.Count)] * cs * 0.3f;
                var yaw = Range(random, 0f, 360f);
                var size = Range(random, 1f, 1.7f);
                if (!keepClear)
                    Place(model, parent, position, top, yaw, size, Fit.Footprint, 0.12f, m_DecorRockMaterial, true);
                return;
            }

            var offset = new Vector3(Range(random, -0.25f, 0.25f), 0f, Range(random, -0.25f, 0.25f)) * cs;
            if (Chance(random, 0.15f * density))
            {
                var model = Pick(random, m_Stalagmites);
                var yaw = Range(random, 0f, 360f);
                var size = Range(random, 2f, 4.5f);
                if (!keepClear)
                    Place(model, parent, center + offset, top, yaw, size, Fit.Height, 0.15f, m_DecorRockMaterial, true);
            }
            else if (Chance(random, 0.1f * density))
            {
                var model = Pick(random, m_BigRocks);
                var yaw = Range(random, 0f, 360f);
                var size = Range(random, 1f, 1.8f);
                if (!keepClear)
                    Place(model, parent, center + offset, top, yaw, size, Fit.Footprint, 0.12f, m_DecorRockMaterial, true);
            }
        }

        // Des stalactites sous la voûte : les stalagmites, retournées. Assez courtes pour laisser voler le dragon.
        void DecorateCeiling(Transform parent, System.Random random)
        {
            if (!m_Ceiling)
                return;

            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    var cell = m_Cells[x, y];
                    if (cell == Cell.Rock || !Chance(random, 0.09f * m_DecorDensity))
                        continue;

                    var ceiling = CeilingAt(x, y);
                    var room = ceiling - Mathf.Max(0f, TopAt(x, y));
                    var length = Mathf.Min(Range(random, 2f, 5.5f), room * 0.3f);
                    var offset = new Vector3(Range(random, -0.3f, 0.3f), 0f, Range(random, -0.3f, 0.3f)) * m_CellSize;
                    if (length >= 1f)
                        Place(Pick(random, m_Stalagmites), parent, CellCenter(x, y) + offset, ceiling, Range(random, 0f, 360f), length, Fit.Height,
                              0.6f, m_DecorRockMaterial, true, hang: true);
                }
            }
        }

        // Deux ou trois rochers au pied de chaque pilier.
        void DecoratePillars(Transform parent, System.Random random)
        {
            foreach (var spot in m_PillarSpots)
            {
                var count = 2 + random.Next(2);
                for (var i = 0; i < count; i++)
                {
                    var angle = Range(random, 0f, Mathf.PI * 2f);
                    var position = new Vector3(spot.x, 0f, spot.z) + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (spot.w + 0.15f);
                    if (CellAt(position, out _) != Cell.Floor)
                        continue;
                    Place(Pick(random, m_BigRocks), parent, position, 0f, Range(random, 0f, 360f), Range(random, 0.7f, 1.2f), Fit.Footprint, 0.1f, m_DecorRockMaterial, true);
                }
            }
        }

        // Le nid : des colonnes brisées autour de l'estrade, une statue renversée, les armes et les boucliers
        // d'aventuriers qui n'en sont pas revenus, et une lueur rouge au-dessus.
        void DecorateNest(Transform parent, System.Random random)
        {
            var center = Vector3.zero;
            var count = 0;
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    if (m_Cells[x, y] != Cell.Dais)
                        continue;
                    center += CellCenter(x, y);
                    count++;
                }
            }

            if (count == 0)
                return;

            center /= count;
            var radius = Mathf.Sqrt(count) * m_CellSize * 0.6f;

            for (var i = 0; i < 3; i++)
            {
                for (var attempt = 0; attempt < 6; attempt++)
                {
                    var angle = (i * 120f + 40f + attempt * 35f + Range(random, -20f, 20f)) * Mathf.Deg2Rad;
                    var position = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (radius + Range(random, 1.2f, 2.2f));
                    if (CellAt(position, out _) != Cell.Floor)
                        continue;
                    Place(Pick(random, m_Ruins), parent, position, 0f, Range(random, 0f, 360f), Range(random, 3f, 5f), Fit.Height, 0.1f, null, true);
                    break;
                }
            }

            for (var attempt = 0; attempt < 8; attempt++)
            {
                var angle = Range(random, 0f, Mathf.PI * 2f);
                var position = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (radius + Range(random, 2f, 3.2f));
                if (CellAt(position, out _) != Cell.Floor)
                    continue;
                var statue = Place(m_FallenStatue, parent, position, 0f, Range(random, 0f, 360f), 2f, Fit.Footprint, 0.25f, null, true);
                if (statue != null)
                    statue.transform.localRotation *= Quaternion.Euler(Range(random, 10f, 25f), 0f, Range(random, -15f, 15f));
                break;
            }

            for (var i = 0; i < 10; i++)
            {
                var angle = Range(random, 0f, Mathf.PI * 2f);
                var position = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * Range(random, radius * 0.3f, radius + 1.6f);
                var cell = CellAt(position, out _);
                if (cell != Cell.Dais && cell != Cell.Floor)
                    continue;
                Place(Pick(random, m_Remains), parent, position, cell == Cell.Dais ? m_DaisHeight : 0f, Range(random, 0f, 360f),
                      Range(random, 0.8f, 1.3f), Fit.Length, 0f, m_RemainsMaterial, false, layFlat: true);
            }

            AddLight("Lueur du nid", parent, center + Vector3.up * (m_DaisHeight + 3f), new Color(1f, 0.35f, 0.2f), 9f, 1.2f);
        }

        // La porte au bout du couloir : deux colonnes, un linteau et un battant sombre contre la paroi. Des braseros
        // éclairent le couloir, à la porte et à son débouché dans l'arène.
        void DecorateDoor(Transform parent)
        {
            var minX = int.MaxValue;
            var maxX = int.MinValue;
            var row = -1;
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    if (m_Cells[x, y] != Cell.Door)
                        continue;
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    row = Mathf.Max(row, y);
                }
            }

            if (row < 0)
                return;

            var width = (maxX - minX + 1) * m_CellSize;
            var middle = (CellCenter(minX, row) + CellCenter(maxX, row)) * 0.5f;
            var wallZ = middle.z - m_CellSize * 0.5f;
            const float doorHeight = 4.5f;

            for (var side = -1; side <= 1; side += 2)
                Place(m_DoorColumn, parent, new Vector3(middle.x + side * (width * 0.5f + 0.35f), 0f, wallZ + 0.55f), 0f, 0f, doorHeight + 1.1f, Fit.Height, 0f, null, true);
            Block("Linteau", parent, new Vector3(middle.x, doorHeight + 0.5f, wallZ + 0.55f), new Vector3(width + 2.2f, 1f, 1.3f), m_RockMaterial, true);
            Block("Porte", parent, new Vector3(middle.x, doorHeight * 0.5f, wallZ + 0.04f), new Vector3(width, doorHeight, 0.06f), m_DoorMaterial, false);

            var mouthZ = m_Spawn.position.z + m_CellSize * 0.5f;
            foreach (var z in new[] { wallZ + 1.6f, mouthZ - 0.7f })
            {
                for (var side = -1; side <= 1; side += 2)
                {
                    var position = new Vector3(middle.x + side * (width * 0.5f - 0.7f), 0f, z);
                    Place(m_Brazier, parent, position, 0f, side * 30f, 1f, Fit.Footprint, 0f, null, false);
                    if (m_FireMaterial != null)
                        TrackFire(FireEffects.CreateBrazierFire(parent, position + Vector3.up * 0.3f, m_FireMaterial, 0.6f));
                    AddLight("Lumière du brasero", parent, position + Vector3.up * 1.4f, new Color(1f, 0.58f, 0.28f), 9f, 2.2f);
                }
            }
        }

        // Un socle de pierre sous chaque emplacement de tourelle.
        void DecorateTurrets(Transform parent)
        {
            foreach (var slot in m_TurretSlots)
                Place(m_TurretPlinth, parent, slot.position, slot.position.y, slot.rotation.eulerAngles.y, m_CellSize * 0.85f, Fit.Footprint, 0.02f, null, false);
        }

        // Des lumières orangées au-dessus de la lave, espacées, et des braises qui en montent. La coulée qui tombe du
        // plateau en jette davantage à son pied.
        void DecorateLava(Transform parent)
        {
            var spots = new List<Vector3>();
            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    var cell = m_Cells[x, y];
                    if (!IsLava(cell) || spots.Count >= m_MaxLavaLights)
                        continue;

                    var position = CellCenter(x, y) + Vector3.up * Top(cell);
                    var tooClose = false;
                    foreach (var spot in spots)
                        tooClose |= (spot - position).sqrMagnitude < m_LavaLightSpacing * m_LavaLightSpacing;
                    if (tooClose)
                        continue;

                    spots.Add(position);
                    AddLight("Lumière de la lave", parent, position + Vector3.up * 1.3f, m_LavaLightColor, 8f, 2.5f);
                    if (m_Embers && m_FireMaterial != null)
                        TrackFire(FireEffects.CreateEmbers(parent, position + Vector3.up * 0.05f, m_FireMaterial, m_CellSize * 1.5f, 5f));
                }
            }

            if (!m_Embers || m_FireMaterial == null)
                return;

            for (var y = 0; y < m_Height; y++)
            {
                for (var x = 0; x < m_Width; x++)
                {
                    if (m_Cells[x, y] != Cell.PlateauLava)
                        continue;
                    foreach (var side in k_Sides)
                    {
                        if (Get(x + side.x, y + side.y) == Cell.Lava)
                            TrackFire(FireEffects.CreateEmbers(parent, CellCenter(x + side.x, y + side.y) + Vector3.up * (0.1f - m_LavaDepth), m_FireMaterial, m_CellSize, 12f));
                    }
                }
            }
        }

        // ---------------------------------------------------------------- parois rocheuses

        // La roche à facettes de toutes les faces verticales assez hautes : la roche (parois, plateau, berges, estrade)
        // d'un côté, les piliers de l'autre. Les faces plates restent derrière, pour qu'on ne voie jamais au travers.
        void BuildSkins(MeshBuilder rock, MeshBuilder pillar)
        {
            if (!m_Decorate || !m_RockSkin)
                return;

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
                    foreach (var side in k_Sides)
                    {
                        var neighbour = Get(x + side.x, y + side.y);
                        var from = TopAt(x + side.x, y + side.y);
                        if (!TryGetFaceTop(cell, x, y, side, top, out var faceTop))
                            continue;
                        if (faceTop - from < 0.3f || cell == Cell.PlateauLava && IsLava(neighbour) || cell == Cell.Lava)
                            continue;

                        // Tournée vers la roche cachée, hors de l'arène : personne ne la voit.
                        if (neighbour == Cell.Rock && !IsVisibleRock(x + side.x, y + side.y))
                            continue;

                        AddSkin(cell == Cell.Pillar ? pillar : rock, side, center.x - half, center.x + half, center.z - half, center.z + half, from, faceTop);
                    }
                }
            }
        }

        // Une face couverte de facettes : une grille qui avance de la face plate selon un bruit continu (les faces voisines
        // se raccordent), un peu décalée au hasard, avec des strates. En haut, un rebord referme la roche contre le dessus
        // de la case ; sur les côtés, des bandes la referment contre la face plate.
        void AddSkin(MeshBuilder builder, Vector2Int side, float x0, float x1, float z0, float z1, float bottom, float top)
        {
            var normal = new Vector3(side.x, 0f, -side.y);
            Vector3 start;
            if (side.x > 0)
                start = new Vector3(x1, 0f, z0);
            else if (side.x < 0)
                start = new Vector3(x0, 0f, z0);
            else if (side.y < 0)
                start = new Vector3(x0, 0f, z1);
            else
                start = new Vector3(x0, 0f, z0);

            var along = side.x != 0 ? Vector3.forward : Vector3.right;
            var width = m_CellSize;
            var columns = Mathf.Max(1, Mathf.RoundToInt(width / m_SkinFacet));
            var rows = Mathf.Max(1, Mathf.CeilToInt((top - bottom) / m_SkinFacet));
            var grid = new Vector3[columns + 1, rows + 1];
            var flat = new Vector3[columns + 1, rows + 1];
            for (var i = 0; i <= columns; i++)
            {
                for (var j = 0; j <= rows; j++)
                {
                    var point = start + along * (width * i / columns) + Vector3.up * Mathf.Lerp(bottom, top, (float)j / rows);
                    flat[i, j] = point;

                    var shape = Fbm3(point * 0.8f, m_DecorSeed);
                    var detail = Noise3(point * 2.3f + new Vector3(7.1f, 3.3f, 1.7f), m_DecorSeed + 5);
                    var strata = m_SkinStrata * Mathf.Abs(Mathf.Sin((point.y + shape * 0.9f) * Mathf.PI / 1.1f));
                    var depth = m_SkinDepth * (0.12f + 0.68f * shape + 0.2f * detail) + strata;

                    // Les bords de la face restent droits, pour retrouver la face voisine ; l'intérieur se décale un peu.
                    var shiftAlong = i == 0 || i == columns ? 0f : (Noise3(point * 3.7f, m_DecorSeed + 9) - 0.5f) * 0.6f * width / columns;
                    var shiftUp = j == 0 || j == rows ? 0f : (Noise3(point * 3.7f + Vector3.one * 5f, m_DecorSeed + 13) - 0.5f) * 0.6f * (top - bottom) / rows;
                    grid[i, j] = point + normal * depth + along * shiftAlong + Vector3.up * shiftUp;
                }
            }

            for (var i = 0; i < columns; i++)
            {
                for (var j = 0; j < rows; j++)
                {
                    // Diagonales alternées : des facettes moins régulières.
                    if (((i + j) & 1) == 0)
                    {
                        Facet(builder, grid[i, j], grid[i + 1, j], grid[i + 1, j + 1], normal);
                        Facet(builder, grid[i, j], grid[i + 1, j + 1], grid[i, j + 1], normal);
                    }
                    else
                    {
                        Facet(builder, grid[i, j], grid[i + 1, j], grid[i, j + 1], normal);
                        Facet(builder, grid[i + 1, j], grid[i + 1, j + 1], grid[i, j + 1], normal);
                    }
                }

                // Le rebord du haut, jusqu'au bord du dessus de la case.
                Facet(builder, grid[i, rows], grid[i + 1, rows], flat[i + 1, rows], Vector3.up);
                Facet(builder, grid[i, rows], flat[i + 1, rows], flat[i, rows], Vector3.up);
            }

            // Les deux côtés de la bande de roche, contre la face plate.
            for (var j = 0; j < rows; j++)
            {
                Facet(builder, grid[0, j], grid[0, j + 1], flat[0, j + 1], -along);
                Facet(builder, grid[0, j], flat[0, j + 1], flat[0, j], -along);
                Facet(builder, grid[columns, j], grid[columns, j + 1], flat[columns, j + 1], along);
                Facet(builder, grid[columns, j], flat[columns, j + 1], flat[columns, j], along);
            }
        }

        // Le dessous de la voûte au-dessus d'une case : des facettes qui pendent un peu, selon le même bruit continu
        // (deux cases voisines à la même hauteur se raccordent). Contre une marche ou une paroi, le bord reste à la hauteur
        // exacte de la voûte : il rejoint la marche ou le haut du mur sans laisser de fente.
        void AddCeilingSkin(MeshBuilder builder, int x, int y, float x0, float x1, float z0, float z1, float height)
        {
            var count = Mathf.Max(1, Mathf.RoundToInt(m_CellSize / m_SkinFacet));

            // Voisines à la même hauteur : à gauche (-X), à droite (+X), devant (-Z, rangée suivante), derrière (+Z, rangée précédente).
            var left = SameCeiling(x - 1, y, height);
            var right = SameCeiling(x + 1, y, height);
            var front = SameCeiling(x, y + 1, height);
            var back = SameCeiling(x, y - 1, height);
            var grid = new Vector3[count + 1, count + 1];
            for (var i = 0; i <= count; i++)
            {
                for (var j = 0; j <= count; j++)
                {
                    var point = new Vector3(Mathf.Lerp(x0, x1, (float)i / count), height, Mathf.Lerp(z0, z1, (float)j / count));
                    var shape = Fbm3(point * 0.6f, m_DecorSeed + 31);
                    var detail = Noise3(point * 2.1f, m_DecorSeed + 37);
                    var free = VertexIsFree(i, j, count, left, right, front, back, x, y, height);
                    var depth = free ? m_SkinDepth * 1.3f * (0.1f + 0.7f * shape + 0.2f * detail) : 0f;
                    var shiftX = i == 0 || i == count ? 0f : (Noise3(point * 3.7f, m_DecorSeed + 41) - 0.5f) * 0.6f * (x1 - x0) / count;
                    var shiftZ = j == 0 || j == count ? 0f : (Noise3(point * 3.7f + Vector3.one * 5f, m_DecorSeed + 43) - 0.5f) * 0.6f * (z1 - z0) / count;
                    grid[i, j] = point + Vector3.down * depth + new Vector3(shiftX, 0f, shiftZ);
                }
            }

            for (var i = 0; i < count; i++)
            {
                for (var j = 0; j < count; j++)
                {
                    if (((i + j) & 1) == 0)
                    {
                        Facet(builder, grid[i, j], grid[i + 1, j], grid[i + 1, j + 1], Vector3.down);
                        Facet(builder, grid[i, j], grid[i + 1, j + 1], grid[i, j + 1], Vector3.down);
                    }
                    else
                    {
                        Facet(builder, grid[i, j], grid[i + 1, j], grid[i, j + 1], Vector3.down);
                        Facet(builder, grid[i + 1, j], grid[i + 1, j + 1], grid[i, j + 1], Vector3.down);
                    }
                }
            }
        }

        // Un sommet du dessous de la voûte peut pendre si toutes les cases qui le partagent sont à la même hauteur.
        bool VertexIsFree(int i, int j, int count, bool left, bool right, bool front, bool back, int x, int y, float height)
        {
            var onLeft = i == 0;
            var onRight = i == count;
            var onFront = j == 0;
            var onBack = j == count;
            if (onLeft && !left || onRight && !right || onFront && !front || onBack && !back)
                return false;
            if (onLeft && onFront)
                return SameCeiling(x - 1, y + 1, height);
            if (onLeft && onBack)
                return SameCeiling(x - 1, y - 1, height);
            if (onRight && onFront)
                return SameCeiling(x + 1, y + 1, height);
            if (onRight && onBack)
                return SameCeiling(x + 1, y - 1, height);
            return true;
        }

        bool SameCeiling(int x, int y, float height) => Get(x, y) != Cell.Rock && Mathf.Abs(CeilingAt(x, y) - height) < 0.01f;

        // Un triangle tourné vers « facing » (l'ordre de ses sommets est inversé s'il le faut).
        void Facet(MeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, Vector3 facing)
        {
            var uvA = new Vector2(a.x + a.z, a.y) / m_CellSize;
            var uvB = new Vector2(b.x + b.z, b.y) / m_CellSize;
            var uvC = new Vector2(c.x + c.z, c.y) / m_CellSize;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) >= 0f)
                builder.Triangle(a, b, c, Color.white, uvA, uvB, uvC);
            else
                builder.Triangle(a, c, b, Color.white, uvA, uvC, uvB);
        }

        // Bruit de valeur en 3D (0 à 1), continu : deux faces qui se touchent ont la même roche à leur jonction.
        static float Noise3(Vector3 p, int seed)
        {
            var ix = Mathf.FloorToInt(p.x);
            var iy = Mathf.FloorToInt(p.y);
            var iz = Mathf.FloorToInt(p.z);
            var fx = p.x - ix;
            var fy = p.y - iy;
            var fz = p.z - iz;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            fz = fz * fz * (3f - 2f * fz);
            var x00 = Mathf.Lerp(Hash(ix, iy * 7919 + iz * 104729, seed), Hash(ix + 1, iy * 7919 + iz * 104729, seed), fx);
            var x10 = Mathf.Lerp(Hash(ix, (iy + 1) * 7919 + iz * 104729, seed), Hash(ix + 1, (iy + 1) * 7919 + iz * 104729, seed), fx);
            var x01 = Mathf.Lerp(Hash(ix, iy * 7919 + (iz + 1) * 104729, seed), Hash(ix + 1, iy * 7919 + (iz + 1) * 104729, seed), fx);
            var x11 = Mathf.Lerp(Hash(ix, (iy + 1) * 7919 + (iz + 1) * 104729, seed), Hash(ix + 1, (iy + 1) * 7919 + (iz + 1) * 104729, seed), fx);
            return Mathf.Lerp(Mathf.Lerp(x00, x10, fy), Mathf.Lerp(x01, x11, fy), fz);
        }

        static float Fbm3(Vector3 p, int seed) =>
            0.6f * Noise3(p, seed) + 0.3f * Noise3(p * 2.1f + new Vector3(3.7f, 1.3f, 9.1f), seed + 1) + 0.1f * Noise3(p * 4.3f, seed + 2);

        // ---------------------------------------------------------------- zones

        static bool IsWalkable(Cell cell) => cell == Cell.Floor || cell == Cell.Spawn || cell == Cell.Door || cell == Cell.Dais;

        // Près de l'arrivée, de la porte et dans le couloir : rien ne gêne le passage.
        bool IsQuiet(int x, int y)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                for (var dx = -2; dx <= 2; dx++)
                {
                    var cell = Get(x + dx, y + dy);
                    if (cell == Cell.Spawn || cell == Cell.Door)
                        return true;
                }
            }

            return false;
        }

        // Les cases autour d'un emplacement de tourelle restent libres.
        bool IsNearTurret(int x, int y)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (Get(x + dx, y + dy) == Cell.Turret)
                        return true;
                }
            }

            return false;
        }

        // ---------------------------------------------------------------- pose des objets

        GameObject NewObject(string objectName, Transform parent)
        {
            if (!m_DecorSaved && m_Generated != null && parent == m_Generated.Root)
                return m_Generated.Create(objectName, parent);

            var go = new GameObject(objectName) { hideFlags = DecorFlags };
            go.transform.SetParent(parent, false);
            return go;
        }

        GameObject Spawn(GameObject prefab, Transform parent)
        {
            GameObject go = null;
#if UNITY_EDITOR
            if (m_DecorSaved)
                go = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
#endif
            if (go == null)
            {
                go = Instantiate(prefab, parent, false);
                go.name = prefab.name;
            }

            var flags = DecorFlags;
            if (flags != HideFlags.None)
            {
                foreach (var child in go.GetComponentsInChildren<Transform>(true))
                    child.gameObject.hideFlags = flags;
            }

            return go;
        }

        /// <summary>
        /// Pose un modèle : mis à la taille voulue (hauteur, emprise au sol ou longueur), tourné de <paramref name="yaw"/>,
        /// posé sur le sol (<paramref name="groundY"/>, repère de l'objet) et enfoncé de <paramref name="sink"/> mètres.
        /// À plat : sa plus petite dimension vers le haut (une épée, un bouclier couchés). Suspendu : retourné, son haut
        /// accroché à <paramref name="groundY"/> (le plafond) et enfoncé dedans de <paramref name="sink"/> mètres.
        /// </summary>
        GameObject Place(GameObject prefab, Transform parent, Vector3 position, float groundY, float yaw, float size, Fit fit,
            float sink, Material material, bool collider, bool layFlat = false, bool hang = false)
        {
            if (prefab == null)
                return null;

            var go = Spawn(prefab, parent);
            var t = go.transform;
            t.localPosition = new Vector3(position.x, groundY, position.z);
            t.localRotation = Quaternion.identity;
            var scale = t.localScale;
            if (!TryGetModelBounds(go, out var bounds))
                return go;

            var size3 = Vector3.Scale(bounds.size, scale);
            var baseRotation = Quaternion.identity;
            if (layFlat)
            {
                if (size3.x <= size3.y && size3.x <= size3.z)
                    baseRotation = Quaternion.Euler(0f, 0f, 90f);
                else if (size3.z <= size3.y && size3.z <= size3.x)
                    baseRotation = Quaternion.Euler(-90f, 0f, 0f);
            }
            else if (hang)
            {
                baseRotation = Quaternion.Euler(180f, 0f, 0f);
            }

            var rotation = Quaternion.Euler(0f, yaw, 0f) * baseRotation;
            var rotated = RotatedExtents(bounds, scale, baseRotation);
            var measure = fit == Fit.Height ? rotated.y : fit == Fit.Footprint ? Mathf.Max(rotated.x, rotated.z) : Mathf.Max(rotated.x, Mathf.Max(rotated.y, rotated.z));
            var factor = size / Mathf.Max(0.001f, measure);
            scale *= factor;
            t.localScale = scale;
            t.localRotation = rotation;

            // Le bas du modèle tourné posé sur le sol (ou son haut accroché au plafond).
            var lowest = float.PositiveInfinity;
            var highest = float.NegativeInfinity;
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x, (i & 2) == 0 ? bounds.min.y : bounds.max.y, (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                var cornerY = (rotation * Vector3.Scale(corner, scale)).y;
                lowest = Mathf.Min(lowest, cornerY);
                highest = Mathf.Max(highest, cornerY);
            }

            t.localPosition = new Vector3(position.x, hang ? groundY - highest + sink : groundY - lowest - sink, position.z);

            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (material == null)
                    continue;
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                    materials[i] = material;
                renderer.sharedMaterials = materials;
            }

            if (collider)
            {
                foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh != null && !filter.TryGetComponent<Collider>(out _))
                        filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                }
            }

            MarkStatic(go);
            return go;
        }

        // Boîte englobante des maillages du modèle, dans son propre repère (sans son échelle).
        static bool TryGetModelBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            var found = false;
            var root = go.transform;
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;

                var local = mesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? local.min.x : local.max.x, (i & 2) == 0 ? local.min.y : local.max.y, (i & 4) == 0 ? local.min.z : local.max.z);
                    var point = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (!found)
                        bounds = new Bounds(point, Vector3.zero);
                    else
                        bounds.Encapsulate(point);
                    found = true;
                }
            }

            return found && bounds.size.sqrMagnitude > 1e-8f;
        }

        static Vector3 RotatedExtents(Bounds bounds, Vector3 scale, Quaternion rotation)
        {
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? bounds.min.x : bounds.max.x, (i & 2) == 0 ? bounds.min.y : bounds.max.y, (i & 4) == 0 ? bounds.min.z : bounds.max.z);
                var point = rotation * Vector3.Scale(corner, scale);
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }

            return max - min;
        }

        // Une boîte (linteau, battant de la porte), avec ou sans collider.
        void Block(string objectName, Transform parent, Vector3 center, Vector3 size, Material material, bool collider)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objectName;
            go.hideFlags = DecorFlags;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            if (material != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collider)
                DestroyImmediate(go.GetComponent<Collider>());
            MarkStatic(go);
        }

        void AddLight(string objectName, Transform parent, Vector3 position, Color color, float range, float intensity)
        {
            var go = NewObject(objectName, parent);
            go.transform.localPosition = position;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            if (!m_DecorSaved)
            {
                m_DecorLights.Add(light);
                m_DecorLightIntensities.Add(intensity);
            }
        }

        void TrackFire(ParticleSystem fire)
        {
            if (!m_DecorSaved && fire != null)
                m_DecorFires.Add(fire);
        }

        // Les lumières et les feux du décor suivent l'allumage de l'arène (intro du combat ; 1 hors du combat).
        void ApplyDecorLight()
        {
            for (var i = 0; i < m_DecorLights.Count; i++)
            {
                var light = m_DecorLights[i];
                if (light == null)
                    continue;
                light.intensity = m_DecorLightIntensities[i] * m_LightLevel;
                light.enabled = m_LightLevel > 0.001f;
            }

            foreach (var fire in m_DecorFires)
            {
                if (fire == null)
                    continue;
                var emission = fire.emission;
                emission.enabled = m_LightLevel > 0.05f;
            }
        }

        // Figé dans la scène : décor statique, regroupé au rendu.
        void MarkStatic(GameObject go)
        {
#if UNITY_EDITOR
            if (!m_DecorSaved)
                return;
            foreach (var child in go.GetComponentsInChildren<Transform>(true))
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(child.gameObject, UnityEditor.StaticEditorFlags.BatchingStatic);
#endif
        }

        static GameObject Pick(System.Random random, GameObject[] choices)
        {
            if (choices == null || choices.Length == 0)
                return null;
            var choice = choices[random.Next(choices.Length)];
            return choice;
        }

        static bool Chance(System.Random random, float probability) => random.NextDouble() < probability;

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
