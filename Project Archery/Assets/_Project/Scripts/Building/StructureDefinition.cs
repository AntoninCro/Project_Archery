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

        /// <summary>
        /// Sur la tour (refonte du 27 octobre) : le seau d'huile, la baliste sur la tour. Quelques petits emplacements autour du
        /// joueur, faits par la grille au lancement (28 octobre).
        /// </summary>
        Tower,
    }

    // Enregistré par son numéro dans les assets : ajouter à la fin, sans retirer ni déplacer.
    public enum StructureKind
    {
        Barricade,
        Spikes,
        FrostTower,
        Ballista,

        // Refonte du tower defense, étape 5, lot 1 (28 octobre).
        Mine,
        Tar,
        Trampoline,
        LightningTower,

        // Lot 2 (28 octobre).
        Repeater,
        Mortar,

        // Lot 3 (28 octobre) : les constructions physiques.
        Geyser,
        Blower,
        Magnet,
        Catapult,

        // Lot 4 (28 octobre).
        Trapdoor,
        Pendulum,
        Bubble,
        NetLauncher,

        // Lot 5 (28 octobre) : le soutien.
        Flamethrower,
        Lighthouse,
        Mirror,
        Ring,

        // Lot 6 (28 octobre) : sur la tour, et les légendaires.
        OilBucket,
        TowerBallista,
        Spirit,

        // Lot 7 de la refonte (31 octobre) : un duo, la tour nouvelle née de la fusion de deux tours (TowerDuo). Les sortes
        // sont enregistrées par leur numéro : ajouter les nouvelles à la fin.
        Duo,
    }

    /// <summary>Rareté d'un bâtiment dans les caisses de l'ingénieur (refonte du 28 octobre, étape 5).</summary>
    public enum StructureRarity
    {
        Common,
        Rare,
        Legendary,
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

            [Tooltip("Plans pour construire ce niveau (29 octobre). 0 : 1 plan jusqu'au niveau 3, 2 plans aux niveaux 4 et 5.")]
            [Min(0)]
            public int plans;

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

        [Tooltip("Un élément par niveau : construction, niveau 2, niveau 3 (jusqu'au niveau 5 depuis le 29 octobre, à mesure que " +
                 "chaque tour reçoit ses évolutions).")]
        public Level[] levels = { new Level(), new Level(), new Level() };

        [Tooltip("Plans donnés par un lot de la caisse de l'ingénieur (refonte du 27 octobre : les bâtiments communs viennent par " +
                 "lots). 0 : selon le bâtiment, 2 pour la barricade et les piques, 1 pour la tour de givre et la baliste.")]
        [Min(0)]
        public int lotSize;

        [Tooltip("Rareté dans les caisses de l'ingénieur (28 octobre) : chaque orbe tire d'abord une rareté (60 / 30 / 10 %), puis un " +
                 "bâtiment de cette rareté. Une légendaire vient toujours seule.")]
        public StructureRarity rarity;

        [Tooltip("Se pose sur n'importe quel emplacement de la grille : voie, bord ou hauteur (28 octobre : les anneaux élémentaires).")]
        public bool anywhere;

        [Tooltip("Cases de la grille de construction occupées (largeur, profondeur ; 28 octobre). 0 : d'après la taille du modèle " +
                 "au niveau 3 (la console donne le compte de chaque bâtiment à sa première pose).")]
        public Vector2Int footprint;

        [Tooltip("Retiré du jeu (29 octobre, deuxième brainstorm du tower defense : 10 tours seulement) : les caisses de " +
                 "l'ingénieur ne le proposent plus, ni le raccourci B, ni le menu des plans. Son code et son prefab restent : " +
                 "décocher pour le remettre.")]
        public bool retired;

        public int MaxLevel => levels != null ? levels.Length : 0;

        /// <summary>
        /// Ce bâtiment se pose-t-il sur un emplacement de cette sorte ? Ceux du bord de la voie vont aussi en hauteur ; ceux qui vont
        /// partout, sur la voie, au bord et en hauteur (pas sur la tour).
        /// </summary>
        public bool Fits(BuildSlot zone) => slot == zone || (slot == BuildSlot.Roadside && zone == BuildSlot.Height) ||
                                            (anywhere && zone != BuildSlot.Tower);

        /// <summary>Plans d'un lot de la caisse de l'ingénieur.</summary>
        public int LotSize => rarity == StructureRarity.Legendary ? 1
            : lotSize > 0 ? lotSize
            : kind == StructureKind.Barricade || kind == StructureKind.Spikes ? 2 : 1;

        /// <summary>
        /// Plans pour construire le niveau donné (1 = construction) : 1 jusqu'au niveau 3, 2 aux niveaux 4 et 5 (deuxième brainstorm
        /// du tower defense, 29 octobre : monter haut devient un vrai choix), sauf réglage du niveau.
        /// </summary>
        public int PlansFor(int level)
        {
            if (level >= 1 && level <= MaxLevel && levels[level - 1].plans > 0)
                return levels[level - 1].plans;
            return level >= 4 ? 2 : 1;
        }

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
