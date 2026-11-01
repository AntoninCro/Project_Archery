using System.Collections.Generic;

namespace Archery.Building
{
    /// <summary>
    /// Un trio (1er novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.19) : la tour nouvelle née de la fusion d'un duo et d'une
    /// troisième tour, au moins au niveau 3. Même formule qu'un duo : elle ne garde rien de ce qu'ils savaient faire, a son
    /// attaque à elle (les classes qui en héritent), son modèle, et ses 3 niveaux, montés avec les plans de l'une de ses trois tours.
    /// </summary>
    /// <remarks>
    /// Il naît de <see cref="BuildManager.Fuse"/> au niveau du duo, un de plus si la tour est à son niveau maximal (3 au plus) : à
    /// 4 plans par niveau, un trio coûte 9, 13 ou 17 plans par n'importe quel chemin. Il ne refusionne pas.
    /// </remarks>
    public abstract class TowerTrio : TowerDuo
    {
        StructureDefinition m_Third;

        /// <summary>La troisième tour, fondue dans le duo.</summary>
        public StructureDefinition Third => m_Third;

        public override bool AcceptsPlansOf(StructureDefinition definition) =>
            base.AcceptsPlansOf(definition) || (definition != null && definition == m_Third);

        public override IEnumerable<StructureDefinition> PlanSources
        {
            get
            {
                yield return First;
                yield return Second;
                yield return m_Third;
            }
        }

        internal void SetupTrio(TowerDuoCatalog.Entry entry, StructureDefinition first, StructureDefinition second, StructureDefinition third)
        {
            SetupDuo(entry, first, second);
            m_Third = third;
        }
    }
}
