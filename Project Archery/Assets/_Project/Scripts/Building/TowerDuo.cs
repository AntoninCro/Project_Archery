using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Un duo (lot 7 de la refonte du tower defense, 31 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.14 et 12.15) : la tour
    /// nouvelle née de la fusion de deux tours. Elle ne garde rien de ce qu'elles savaient faire : elle a son attaque à elle (les
    /// classes qui en héritent), son modèle, et ses 3 niveaux, montés avec les plans de l'une ou l'autre de ses deux tours ; le
    /// niveau 3 est son spectacle.
    /// </summary>
    /// <remarks>
    /// Le joueur, le 31 octobre : deux tours empilées qui gardent leurs deux fonctions plus un effet, « ce n'est pas plaisant à
    /// jouer » ; chaque duo garde un élément de ses tours, le pousse à l'extrême, et apporte une mécanique qu'aucune autre tour n'a.
    /// Le duo naît de <see cref="BuildManager.Fuse"/> à la place des deux tours : au niveau 1 (deux tours au niveau 3), 2 (l'une au
    /// niveau 5) ou 3 (les deux). Il se déplace entre les vagues comme une tour ; on ne le défait pas.
    /// </remarks>
    public abstract class TowerDuo : Structure
    {
        StructureDefinition m_First;
        StructureDefinition m_Second;

        /// <summary>Son entrée du catalogue (nom, couleur, texte), donnée à la fusion.</summary>
        public TowerDuoCatalog.Entry Entry { get; private set; }

        /// <summary>Les deux tours dont il est né : leurs plans font monter ses niveaux.</summary>
        public StructureDefinition First => m_First;

        public StructureDefinition Second => m_Second;

        /// <summary>Le spectacle : son niveau 3.</summary>
        public bool IsSpectacle => Level >= 3;

        /// <summary>Les plans de ce bâtiment font monter ce duo (c'est l'une de ses deux tours).</summary>
        public bool AcceptsPlansOf(StructureDefinition definition) => definition != null && (definition == m_First || definition == m_Second);

        internal void SetupDuo(TowerDuoCatalog.Entry entry, StructureDefinition first, StructureDefinition second)
        {
            Entry = entry;
            m_First = first;
            m_Second = second;
        }
    }
}
