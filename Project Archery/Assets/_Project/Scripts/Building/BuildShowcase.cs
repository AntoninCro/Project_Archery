#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Archery.Defense;
using Archery.Player;
using Archery.UI;
using Archery.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Archery.Building
{
    /// <summary>
    /// La vitrine des bâtiments (raccourci de test du 31 octobre, demandé par le joueur) : F4 pose un bâtiment à plusieurs niveaux
    /// d'un coup, pour les voir tous sans plans ni marteau. Un bâtiment de la voie se pose sur les trois voies : niveau 3 à gauche,
    /// 4 au centre, 5 à droite. Un bâtiment hors de la voie se pose au centre des deux clairières près de la tour : niveau 3 à
    /// droite, 4 à gauche (un bâtiment de la tour, sur deux emplacements de la tour). F4 encore retire ceux que la vitrine vient de
    /// poser et passe au bâtiment suivant ; après les bâtiments de base viennent les duos, aux niveaux 1, 2 et 3 au lieu de 3, 4
    /// et 5. Après le dernier duo, F4 retire tout, puis recommence ; Maj + F4 revient en arrière.
    /// </summary>
    /// <remarks>
    /// Seulement dans l'éditeur et les builds de développement. Créé par le <see cref="BuildManager"/> : rien à poser dans la
    /// scène. Chaque bâtiment prend la place libre qui lui convient la plus proche du point visé : un bâtiment du joueur déjà là
    /// pousse celui de la vitrine à côté. Les bâtiments du joueur ne sont jamais retirés.
    /// </remarks>
    [DisallowMultipleComponent]
    public class BuildShowcase : MonoBehaviour
    {
        // Le point visé sur chaque voie, à cette distance (m, à vol d'oiseau) de la tour : là où étaient les premiers socles de la
        // voie (de 15 à 16 m), juste devant les anciennes barricades de départ.
        const float k_LaneDistance = 16f;

        // Le centre d'une clairière : à mi-chemin entre les deux voies qui la bordent, à cette distance de la tour (les clairières
        // de gauche et de droite, de chaque côté de la voie du milieu).
        const float k_ClearingDistance = 20f;

        // Jusqu'où (m) on cherche une place autour du point visé : le long de la voie, et en rond dans la clairière.
        const int k_LaneSearch = 10;
        const float k_ClearingSearch = 12f;

        static readonly float[] k_LaneSides = { 0f, 1.5f, -1.5f, 3f, -3f };
        static readonly string[] k_LaneNames = { "voie de gauche", "voie du centre", "voie de droite" };
        static readonly Color k_Color = new Color(1f, 0.85f, 0.3f);

        // Une étape de la vitrine : un bâtiment de base, ou un duo.
        struct Step
        {
            public StructureDefinition Definition;
            public TowerDuoCatalog.Entry Duo;
        }

        readonly List<Structure> m_Placed = new List<Structure>();
        readonly List<int> m_Cells = new List<int>();
        readonly List<string> m_Missed = new List<string>();

        // L'étape montrée ; -1 : aucune (au lancement, ou tout retiré).
        int m_Index = -1;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || DesktopHand.IsTypingText || !keyboard.f4Key.wasPressedThisFrame)
                return;

            Show(keyboard.shiftKey.isPressed ? -1 : 1);
        }

        void Show(int direction)
        {
            var manager = BuildManager.Instance;
            var grid = BuildGrid.Instance;
            if (manager == null || grid == null || !grid.IsReady)
            {
                HeadMessages.Show("Vitrine : la grille se prépare…", k_Color, 2f);
                return;
            }

            manager.FinishNow();
            Clear(grid);

            var steps = Steps(manager);
            if (steps.Count == 0)
            {
                HeadMessages.Show("Vitrine : aucun bâtiment", k_Color, 2f);
                return;
            }

            // Les étapes, puis une étape vide (tout retiré), en boucle.
            var count = steps.Count + 1;
            var current = m_Index < 0 ? steps.Count : m_Index;
            current = ((current + direction) % count + count) % count;
            m_Index = current < steps.Count ? current : -1;
            if (m_Index < 0)
            {
                HeadMessages.Show("Vitrine : tout est retiré\n<size=60%>F4 : on recommence</size>", k_Color, 2.5f);
                return;
            }

            var step = steps[m_Index];
            var definition = step.Duo != null ? step.Duo.definition : step.Definition;
            var where = PlaceAll(grid, step, definition, step.Duo != null ? 1 : 3);
            var title = step.Duo != null ? "Duo : " + step.Duo.displayName : definition.displayName;
            var text = $"Vitrine {m_Index + 1}/{steps.Count} : {title}\n<size=60%>{where}";
            if (m_Missed.Count > 0)
                text += "\npas de place : " + string.Join(", ", m_Missed);
            HeadMessages.Show(text + "</size>", step.Duo != null ? step.Duo.color : definition.color, 4f);
        }

        // Les bâtiments de base (ceux du menu, pas les retirés), dans l'ordre du Build Manager, puis les duos du catalogue.
        static List<Step> Steps(BuildManager manager)
        {
            var steps = new List<Step>();
            foreach (var definition in manager.Structures)
            {
                if (definition != null && definition.prefab != null && !(definition.prefab is TowerDuo) && !definition.retired &&
                    definition.MaxLevel > 0)
                    steps.Add(new Step { Definition = definition });
            }

            var catalog = TowerDuoCatalog.Instance;
            if (catalog == null)
                return steps;

            foreach (var entry in catalog.Duos)
            {
                if (entry != null && entry.definition != null && entry.definition.prefab is TowerDuo && entry.definition.MaxLevel > 0)
                    steps.Add(new Step { Duo = entry });
            }

            return steps;
        }

        // Pose le bâtiment de l'étape partout où il va ; le texte dit où. Les places introuvables vont dans m_Missed.
        string PlaceAll(BuildGrid grid, Step step, StructureDefinition definition, int low)
        {
            m_Missed.Clear();
            var tower = Tower.Instance != null ? Tower.Instance.transform.position : grid.transform.position;
            var lanes = SortedLanes(grid, tower);

            if (definition.slot == BuildSlot.Lane)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (i >= lanes.Count || !PlaceOnLane(grid, lanes[i], tower, step, definition, low + i))
                        m_Missed.Add(k_LaneNames[i]);
                }

                return $"niveaux {low}, {low + 1} et {low + 2} : voies de gauche, du centre et de droite";
            }

            if (definition.slot == BuildSlot.Tower)
            {
                var placed = 0;
                foreach (var socket in grid.TowerSockets)
                {
                    if (placed >= 2)
                        break;
                    if (socket == null || !socket.isActiveAndEnabled || socket.Structure != null)
                        continue;

                    Build(socket, step, definition, low + placed);
                    placed++;
                }

                if (placed < 2)
                    m_Missed.Add(placed == 0 ? "sur la tour" : $"sur la tour (niveau {low + 1})");
                return $"niveaux {low} et {low + 1} : sur la tour";
            }

            // Hors de la voie : au centre des clairières qui bordent la voie du centre, celle de droite d'abord.
            if (lanes.Count < 2)
            {
                m_Missed.Add("les clairières (moins de deux voies)");
                return $"niveaux {low} et {low + 1} : clairières de droite et de gauche";
            }

            var last = lanes.Count - 1;
            if (!PlaceAround(grid, ClearingCenter(lanes[last - 1], lanes[last], tower), k_ClearingSearch, step, definition, low))
                m_Missed.Add("clairière de droite");
            if (!PlaceAround(grid, ClearingCenter(lanes[0], lanes[1], tower), k_ClearingSearch, step, definition, low + 1))
                m_Missed.Add("clairière de gauche");
            return $"niveau {low} dans la clairière de droite, {low + 1} dans celle de gauche";
        }

        // Sur la voie, vers k_LaneDistance m de la tour : le point visé, puis de mètre en mètre vers la forêt et vers la tour, au
        // milieu du chemin puis sur ses côtés.
        bool PlaceOnLane(BuildGrid grid, MapScan.Lane lane, Vector3 tower, Step step, StructureDefinition definition, int level)
        {
            var along = AlongFromTower(lane, tower, k_LaneDistance);
            for (var k = 0; k <= k_LaneSearch * 2; k++)
            {
                // 0, puis 1 m vers la forêt, 1 m vers la tour, 2 m vers la forêt… (la distance le long de la voie part de la forêt).
                var offset = (k + 1) / 2 * (k % 2 == 1 ? 1f : -1f);
                var point = lane.PointAt(Mathf.Clamp(along - offset, 0f, lane.Length), out var direction);
                direction.y = 0f;
                var side = direction.sqrMagnitude > 1e-6f ? Vector3.Cross(Vector3.up, direction.normalized) : Vector3.right;
                foreach (var lateral in k_LaneSides)
                {
                    if (TryPlace(grid, point + side * lateral, step, definition, level))
                        return true;
                }
            }

            return false;
        }

        // En rond autour du point, de case en case, du centre vers l'extérieur : la première place qui convient.
        bool PlaceAround(BuildGrid grid, Vector3 center, float radius, Step step, StructureDefinition definition, int level)
        {
            var cell = Mathf.Max(0.5f, grid.CellSize);
            for (var r = 0f; r <= radius; r += cell)
            {
                var count = r < 0.01f ? 1 : Mathf.CeilToInt(2f * Mathf.PI * r / cell);
                for (var i = 0; i < count; i++)
                {
                    var angle = i * 2f * Mathf.PI / count;
                    if (TryPlace(grid, center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r), step, definition, level))
                        return true;
                }
            }

            return false;
        }

        // Le bâtiment se pose ici si la grille l'accepte (dans le sens de la case, comme l'hologramme sans le tourner).
        bool TryPlace(BuildGrid grid, Vector3 point, Step step, StructureDefinition definition, int level)
        {
            var spot = grid.Evaluate(definition, point, 0, null, m_Cells);
            if (!spot.Valid || spot.TowerSocket != null)
                return false;

            Build(grid.Claim(spot, m_Cells), step, definition, level);
            return true;
        }

        void Build(BuildSocket socket, Step step, StructureDefinition definition, int level)
        {
            var structure = socket.Place(definition);
            level = Mathf.Clamp(level, 1, Mathf.Max(1, structure.MaxLevel));
            if (step.Duo != null && structure is TowerDuo duo)
            {
                // Comme une fusion (BuildManager.Fuse) : ses deux tours, puis son niveau d'un coup.
                duo.SetupDuo(step.Duo, DefinitionOf(step.Duo.first), DefinitionOf(step.Duo.second));
                duo.SetLevelImmediate(level);
            }
            else
            {
                // Niveau par niveau, comme au marteau, mais sans plan bleu ni coups (comme le raccourci F, Core/DemoBoost).
                for (var next = 1; next <= level; next++)
                {
                    structure.BeginConstruction(next, null);
                    structure.FinishConstruction();
                }
            }

            m_Placed.Add(structure);
        }

        // Retire ce que la vitrine a posé (pas ce que le joueur a construit), et rend leurs cases à la grille.
        void Clear(BuildGrid grid)
        {
            foreach (var structure in m_Placed)
            {
                // Déjà parti (fusionné en duo par le joueur…) : rien à faire.
                if (structure == null)
                    continue;

                var socket = structure.Socket;
                if (socket != null && socket.Structure == structure)
                    socket.Detach();
                Destroy(structure.gameObject);
                if (socket != null)
                    grid.Release(socket);
            }

            m_Placed.Clear();
        }

        static StructureDefinition DefinitionOf(StructureKind kind)
        {
            var manager = BuildManager.Instance;
            if (manager == null)
                return null;

            foreach (var definition in manager.Structures)
            {
                if (definition != null && definition.kind == kind && !definition.retired)
                    return definition;
            }

            return null;
        }

        // Les voies de gauche à droite, vues de la tour vers la forêt : d'après l'angle (autour de la verticale) entre le sens
        // moyen des voies et chacune, à k_LaneDistance m de la tour.
        static List<MapScan.Lane> SortedLanes(BuildGrid grid, Vector3 tower)
        {
            var lanes = new List<MapScan.Lane>();
            var mean = Vector3.zero;
            foreach (var lane in grid.Lanes)
            {
                if (lane == null || lane.Points.Count < 2)
                    continue;

                lanes.Add(lane);
                mean += Flat(PointFromTower(lane, tower, k_LaneDistance) - tower).normalized;
            }

            if (mean.sqrMagnitude < 1e-4f)
                mean = Vector3.forward;
            lanes.Sort((a, b) => Angle(a).CompareTo(Angle(b)));
            return lanes;

            float Angle(MapScan.Lane lane) =>
                Vector3.SignedAngle(mean, Flat(PointFromTower(lane, tower, k_LaneDistance) - tower), Vector3.up);
        }

        static Vector3 ClearingCenter(MapScan.Lane a, MapScan.Lane b, Vector3 tower) =>
            (PointFromTower(a, tower, k_ClearingDistance) + PointFromTower(b, tower, k_ClearingDistance)) * 0.5f;

        static Vector3 PointFromTower(MapScan.Lane lane, Vector3 tower, float distance) =>
            lane.PointAt(AlongFromTower(lane, tower, distance), out _);

        // La distance le long de la voie (depuis la forêt) du premier point, en partant de la tour, à cette distance d'elle (à vol
        // d'oiseau) : le chemin coupe les virages, et celui de la voie du milieu va en escalier.
        static float AlongFromTower(MapScan.Lane lane, Vector3 tower, float distance)
        {
            for (var along = lane.Length; along > 0f; along -= 0.5f)
            {
                if (Flat(lane.PointAt(along, out _) - tower).sqrMagnitude >= distance * distance)
                    return along;
            }

            return 0f;
        }

        static Vector3 Flat(Vector3 offset) => new Vector3(offset.x, 0f, offset.z);
    }
}
#endif
