using System;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>Où un bâtiment se construit (GDD, section 9).</summary>
    public enum BuildSlot
    {
        /// <summary>Sur la voie, là où passent les ennemis : barricade, pièges à piques.</summary>
        Lane,

        /// <summary>Au bord de la voie : baliste, tour de givre.</summary>
        Roadside,

        /// <summary>
        /// En hauteur, sur une terrasse ou une falaise au-dessus d'une voie (refonte du 27 octobre, zones de la grille) : les
        /// bâtiments du bord de la voie s'y posent aussi.
        /// </summary>
        Height,

        /// <summary>Sur la tour (refonte du 27 octobre) : le seau d'huile, la baliste sur la tour (constructions à venir).</summary>
        Tower,
    }

    public enum StructureKind
    {
        Barricade,
        Spikes,
        FrostTower,
        Ballista,
    }

    /// <summary>
    /// Un bâtiment du tower defense (GDD, section 9) : son nom, son emplacement, son prefab et le texte de chaque niveau.
    /// Un asset par bâtiment, listé dans le <see cref="BuildManager"/>. Depuis la refonte du 27 octobre
    /// (<c>Docs/Refonte_TowerDefense.md</c>), on ne le paie plus en matériaux : un plan par construction ou par niveau, trouvé
    /// dans les caisses de l'ingénieur, par lots (<see cref="LotSize"/>).
    /// </summary>
    [CreateAssetMenu(fileName = "Structure", menuName = "Archery/Structure Definition")]
    public class StructureDefinition : ScriptableObject
    {
        [Serializable]
        public class Level
        {
            [Tooltip("Prix en matériaux : plus utilisé depuis la refonte du 27 octobre (un plan par niveau).")]
            public int cost;

            [TextArea]
            public string description;
        }

        public StructureKind kind;

        public string displayName = "Bâtiment";

        public BuildSlot slot;

        [Tooltip("Le bâtiment construit : un prefab avec un composant Structure à la racine, posé au centre du socle.")]
        public Structure prefab;

        [Tooltip("Couleur du titre et de la carte dans le panneau de construction.")]
        public Color color = Color.white;

        [Tooltip("Optionnel : icône de la carte.")]
        public Sprite icon;

        [Tooltip("Un élément par niveau : construction, niveau 2, niveau 3.")]
        public Level[] levels = { new Level(), new Level(), new Level() };

        [Tooltip("Plans donnés par un lot de la caisse de l'ingénieur (refonte du 27 octobre : les bâtiments communs viennent par " +
                 "lots). 0 : selon le bâtiment, 2 pour la barricade et les piques, 1 pour la tour de givre et la baliste.")]
        [Min(0)]
        public int lotSize;

        public int MaxLevel => levels != null ? levels.Length : 0;

        /// <summary>Ce bâtiment se pose-t-il sur un emplacement de cette sorte ? Ceux du bord de la voie vont aussi en hauteur.</summary>
        public bool Fits(BuildSlot zone) => slot == zone || (slot == BuildSlot.Roadside && zone == BuildSlot.Height);

        /// <summary>Plans d'un lot de la caisse de l'ingénieur.</summary>
        public int LotSize => lotSize > 0 ? lotSize : kind == StructureKind.Barricade || kind == StructureKind.Spikes ? 2 : 1;

        /// <summary>Prix du niveau donné (1 = construction), en matériaux.</summary>
        public int CostFor(int level) => level >= 1 && level <= MaxLevel ? Mathf.Max(0, levels[level - 1].cost) : 0;

        public string DescriptionFor(int level) => level >= 1 && level <= MaxLevel ? levels[level - 1].description : "";

        [ContextMenu("Valeurs du GDD")]
        void FillFromGdd()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Valeurs du GDD");
#endif
            switch (kind)
            {
                case StructureKind.Barricade:
                    displayName = "Barricade";
                    slot = BuildSlot.Lane;
                    color = new Color(0.9f, 0.65f, 0.35f);
                    levels = new[]
                    {
                        NewLevel(20, "Les ennemis au sol s'arrêtent pour la frapper.\n400 PV."),
                        NewLevel(30, "700 PV."),
                        NewLevel(45, "1 000 PV.\nDes pointes blessent ceux qui la frappent."),
                    };
                    break;

                case StructureKind.Spikes:
                    displayName = "Pièges à piques";
                    slot = BuildSlot.Lane;
                    color = new Color(0.95f, 0.5f, 0.45f);
                    levels = new[]
                    {
                        NewLevel(25, "Les ennemis au sol qui passent dessus sont blessés toutes les 0,8 s."),
                        NewLevel(35, "Dégâts ×1,5, un coup toutes les 0,65 s."),
                        NewLevel(50, "Dégâts ×2,25, un coup toutes les 0,5 s.\nIls sont aussi ralentis de 20 %."),
                    };
                    break;

                case StructureKind.FrostTower:
                    displayName = "Tour de givre";
                    slot = BuildSlot.Roadside;
                    color = new Color(0.55f, 0.85f, 1f);
                    levels = new[]
                    {
                        NewLevel(40, "Ralentit de 30 % les ennemis à 8 m, volants compris."),
                        NewLevel(50, "Ralentit de 40 % à 9 m."),
                        NewLevel(70, "Ralentit de 50 % à 10 m."),
                    };
                    break;

                case StructureKind.Ballista:
                    displayName = "Baliste";
                    slot = BuildSlot.Roadside;
                    color = new Color(1f, 0.82f, 0.4f);
                    levels = new[]
                    {
                        NewLevel(50, "Tire seule sur l'ennemi le plus proche à 20 m, volants compris, toutes les 2,4 s."),
                        NewLevel(60, "Dégâts ×1,5, un tir toutes les 1,6 s."),
                        NewLevel(80, "Dégâts ×2, un tir chaque seconde.\nLe carreau traverse 2 ennemis."),
                    };
                    break;
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        static Level NewLevel(int cost, string description) => new Level { cost = cost, description = description };
    }
}
