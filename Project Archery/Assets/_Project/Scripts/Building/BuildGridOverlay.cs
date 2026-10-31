using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Building
{
    /// <summary>
    /// La grille de construction dessinée au sol pendant la pose d'un plan (28 octobre, demandée par le joueur après ses tests :
    /// « on voit tout le grillage du sol », vert là où on peut construire, rouge ailleurs). Toutes les cases de sol autour du joueur
    /// se teintent, bordées d'une ligne nette, et épousent le terrain ; les cases du bâtiment visé brillent plus fort (vertes s'il
    /// se pose, rouges sinon). Les emplacements de la tour s'y dessinent aussi, en plus grand.
    /// </summary>
    /// <remarks>
    /// Créé tout seul par <see cref="BuildPlacement"/>. Deux maillages : la grille (refaite quand le joueur change de case, de
    /// bâtiment, ou qu'un bâtiment est posé) et les cases du bâtiment visé (petit, refait quand elles changent). Matériau :
    /// <c>Resources/BuildGrid</c> (shader <c>Archery/BuildGrid</c>).
    /// </remarks>
    [DisallowMultipleComponent]
    public class BuildGridOverlay : MonoBehaviour
    {
        const string k_MaterialResource = "BuildGrid";

        [Tooltip("Rayon (m) de la grille autour du joueur : 36 m depuis le 29 octobre au soir (le joueur : la voir de plus loin ; 16 m " +
                 "avant). Elle s'efface vers le bord.")]
        [SerializeField]
        float m_Radius = 36f;

        [Tooltip("Hauteur (m) de la grille au-dessus du sol.")]
        [SerializeField]
        float m_Lift = 0.06f;

        [Tooltip("Cases où le bâtiment choisi se pose.")]
        [SerializeField]
        Color m_FreeColor = new Color(0.25f, 1f, 0.4f, 0.95f);

        [Tooltip("Cases où il ne se pose pas (pas la bonne zone, pente, eau, obstacle, place prise).")]
        [SerializeField]
        Color m_BlockedColor = new Color(1f, 0.22f, 0.18f, 0.85f);

        [Tooltip("Les cases du bâtiment visé, là où il se pose.")]
        [SerializeField]
        Color m_ValidColor = new Color(0.7f, 1f, 0.75f, 1f);

        [Tooltip("Les cases du bâtiment visé, là où il ne se pose pas.")]
        [SerializeField]
        Color m_InvalidColor = new Color(1f, 0.4f, 0.32f, 1f);

        [Tooltip("Opacité du fond d'une case (la ligne est opaque) : le sol change de couleur sous la grille.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_FillAlpha = 0.3f;

        [Tooltip("Opacité du fond des cases du bâtiment visé.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_FootprintFillAlpha = 0.6f;

        [Tooltip("Épaisseur (m) de la ligne au bord de chaque case (vue des deux côtés : le double entre deux cases).")]
        [SerializeField]
        float m_LineWidth = 0.03f;

        readonly List<Vector3> m_Vertices = new List<Vector3>();
        readonly List<Color> m_Colors = new List<Color>();
        readonly List<Vector2> m_Uv = new List<Vector2>();
        readonly List<Vector4> m_Cell = new List<Vector4>();
        readonly List<int> m_Triangles = new List<int>();

        Mesh m_GridMesh;
        Mesh m_FootprintMesh;
        MeshRenderer m_GridRenderer;
        MeshRenderer m_FootprintRenderer;
        Material m_Material;
        bool m_Ready;
        bool m_Warned;

        StructureDefinition m_Definition;
        Structure m_Moving;
        Vector2 m_Center = new Vector2(float.MaxValue, float.MaxValue);
        int m_Version = -1;
        int m_FootprintHash;

        /// <summary>
        /// Montre la grille autour de <paramref name="center"/> pour ce bâtiment (vert là où il se pose) ; un bâtiment déplacé
        /// (<paramref name="moving"/>) ne se gêne pas lui-même.
        /// </summary>
        public void Show(StructureDefinition definition, Structure moving, Vector3 center)
        {
            var grid = BuildGrid.Instance;
            if (grid == null || !grid.IsReady || !EnsureReady())
            {
                Hide();
                return;
            }

            // Refaite quand le joueur s'est éloigné de plus d'une case et demie (pas à chaque mouvement de tête), ou quand le
            // bâtiment ou les places prises changent.
            var cell = grid.CellSize;
            var moved = new Vector2(center.x - m_Center.x, center.z - m_Center.y).sqrMagnitude > cell * cell * 2.25f;
            if (!m_GridRenderer.enabled || definition != m_Definition || moving != m_Moving || moved || grid.Version != m_Version)
            {
                m_Definition = definition;
                m_Moving = moving;
                m_Center = new Vector2(center.x, center.z);
                m_Version = grid.Version;
                BuildGridMesh(grid, center);
            }

            m_GridRenderer.enabled = true;
        }

        /// <summary>Les cases du bâtiment visé (ou l'emplacement de la tour visé), vives : vertes s'il se pose, rouges sinon.</summary>
        public void ShowFootprint(List<int> cells, BuildSocket towerSocket, bool valid)
        {
            var grid = BuildGrid.Instance;
            if (grid == null || !grid.IsReady || !EnsureReady())
                return;

            var hash = valid ? 17 : 23;
            foreach (var i in cells)
                hash = hash * 31 + i;
            if (towerSocket != null)
                hash = hash * 31 + towerSocket.GetHashCode();
            hash = hash * 31 + cells.Count;

            if (!m_FootprintRenderer.enabled || hash != m_FootprintHash)
            {
                m_FootprintHash = hash;
                BuildFootprintMesh(grid, cells, towerSocket, valid);
            }

            m_FootprintRenderer.enabled = true;
        }

        public void HideFootprint()
        {
            if (m_FootprintRenderer != null)
                m_FootprintRenderer.enabled = false;
        }

        public void Hide()
        {
            if (m_GridRenderer != null)
                m_GridRenderer.enabled = false;
            HideFootprint();
        }

        void OnDestroy()
        {
            if (m_GridMesh != null)
                Destroy(m_GridMesh);
            if (m_FootprintMesh != null)
                Destroy(m_FootprintMesh);
        }

        bool EnsureReady()
        {
            if (m_Ready)
                return true;

            m_Material = Resources.Load<Material>(k_MaterialResource);
            if (m_Material == null)
            {
                var shader = Shader.Find("Archery/BuildGrid");
                if (shader != null)
                    m_Material = new Material(shader) { name = "Grille de construction" };
            }

            if (m_Material == null)
            {
                if (!m_Warned)
                    Debug.LogWarning("BuildGridOverlay : pas de matériau Resources/BuildGrid (shader Archery/BuildGrid) : la grille ne se dessine pas.", this);
                m_Warned = true;
                return false;
            }

            m_GridRenderer = CreateLayer("Grille", 0, out m_GridMesh);
            m_FootprintRenderer = CreateLayer("Cases du bâtiment", 1, out m_FootprintMesh);
            m_Ready = true;
            return true;
        }

        MeshRenderer CreateLayer(string name, int order, out Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.sortingOrder = order;
            renderer.enabled = false;
            return renderer;
        }

        // Toutes les cases de sol dans le rayon, et les emplacements de la tour : vert ou rouge, de plus en plus transparents vers le bord.
        void BuildGridMesh(BuildGrid grid, Vector3 center)
        {
            Clear();
            var cell = grid.CellSize;
            grid.CellRange(center, m_Radius, out var fromX, out var fromZ, out var toX, out var toZ);
            var size = new Vector2(cell, cell);
            var fadeFrom = m_Radius * 0.65f;
            for (var z = fromZ; z <= toZ; z++)
            {
                for (var x = fromX; x <= toX; x++)
                {
                    var index = grid.CellIndex(x, z);
                    if (!grid.HasGround(index) || !grid.TryCellCorners(index, out var a, out var b, out var c, out var d))
                        continue;

                    var middle = (a + d) * 0.5f;
                    var distance = new Vector2(middle.x - center.x, middle.z - center.z).magnitude;
                    if (distance > m_Radius)
                        continue;

                    var color = grid.Accepts(index, m_Definition, m_Moving) ? m_FreeColor : m_BlockedColor;
                    color.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeFrom, m_Radius, distance));
                    AddQuad(a, b, c, d, m_Lift, color, size, m_LineWidth, m_FillAlpha);
                }
            }

            foreach (var socket in grid.TowerSockets)
            {
                if (socket == null || !socket.isActiveAndEnabled)
                    continue;

                var free = m_Definition != null && m_Definition.slot == BuildSlot.Tower &&
                           (socket.Structure == null || (m_Moving != null && socket == m_Moving.Socket));
                AddSquare(socket.transform, grid.TowerSlotSize, m_Lift, free ? m_FreeColor : m_BlockedColor, m_FillAlpha);
            }

            Apply(m_GridMesh);
        }

        void BuildFootprintMesh(BuildGrid grid, List<int> cells, BuildSocket towerSocket, bool valid)
        {
            Clear();
            var color = valid ? m_ValidColor : m_InvalidColor;
            var size = new Vector2(grid.CellSize, grid.CellSize);
            foreach (var index in cells)
            {
                if (grid.HasGround(index) && grid.TryCellCorners(index, out var a, out var b, out var c, out var d))
                    AddQuad(a, b, c, d, m_Lift + 0.015f, color, size, m_LineWidth * 1.5f, m_FootprintFillAlpha);
            }

            if (towerSocket != null)
                AddSquare(towerSocket.transform, grid.TowerSlotSize, m_Lift + 0.015f, color, m_FootprintFillAlpha);

            Apply(m_FootprintMesh);
        }

        void AddSquare(Transform socket, float size, float lift, Color color, float fill)
        {
            var half = size * 0.5f;
            var right = socket.right * half;
            var forward = socket.forward * half;
            var center = socket.position;
            AddQuad(center - right - forward, center + right - forward, center - right + forward, center + right + forward, lift, color,
                    new Vector2(size, size), m_LineWidth * 1.5f, fill);
        }

        // Un carré (a, b : un bord ; c, d : l'autre), un peu au-dessus du sol. UV1 : sa taille, l'épaisseur de la ligne, l'opacité du fond.
        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float lift, Color color, Vector2 size, float line, float fill)
        {
            var start = m_Vertices.Count;
            var up = Vector3.up * lift;
            m_Vertices.Add(a + up);
            m_Vertices.Add(b + up);
            m_Vertices.Add(c + up);
            m_Vertices.Add(d + up);
            m_Uv.Add(new Vector2(0f, 0f));
            m_Uv.Add(new Vector2(1f, 0f));
            m_Uv.Add(new Vector2(0f, 1f));
            m_Uv.Add(new Vector2(1f, 1f));
            var cell = new Vector4(size.x, size.y, line, fill);
            for (var i = 0; i < 4; i++)
            {
                m_Colors.Add(color);
                m_Cell.Add(cell);
            }

            m_Triangles.Add(start);
            m_Triangles.Add(start + 2);
            m_Triangles.Add(start + 1);
            m_Triangles.Add(start + 1);
            m_Triangles.Add(start + 2);
            m_Triangles.Add(start + 3);
        }

        void Clear()
        {
            m_Vertices.Clear();
            m_Colors.Clear();
            m_Uv.Clear();
            m_Cell.Clear();
            m_Triangles.Clear();
        }

        void Apply(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(m_Vertices);
            mesh.SetColors(m_Colors);
            mesh.SetUVs(0, m_Uv);
            mesh.SetUVs(1, m_Cell);
            mesh.SetTriangles(m_Triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}
