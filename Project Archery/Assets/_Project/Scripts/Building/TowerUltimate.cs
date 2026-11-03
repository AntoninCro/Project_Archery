using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Une ultime (2 novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.44) : le niveau 4 d'un trio au niveau 3. On pose dessus des
    /// plans de l'une de ses trois tours, comme pour un niveau ordinaire (10 plans), entre les vagues : le trio se transforme, et on
    /// bâtit l'ultime au marteau (<see cref="BuildManager"/>). Une par famille de trios, une seule par partie.
    /// </summary>
    /// <remarks>
    /// Le joueur, le 2 novembre : pas de règle nouvelle, une ultime intuitive, pas à chaque partie, et méritée (on ne doit pas pouvoir
    /// l'avoir facilement avant la vague 5). Elle grandit (environ deux fois un trio), a une action avec la flèche du joueur, et le
    /// suit dans l'arène de MK, comme les balistes et les tours de givre (<c>FinalBoss/ArenaTurrets</c>) ; les boss ne sont jamais
    /// tués d'un coup. Elle garde les trois tours de son trio (ses plans ne la font plus monter : elle n'a qu'un niveau).
    /// </remarks>
    public abstract class TowerUltimate : TowerTrio
    {
        Vector3 m_ArenaScale = Vector3.one;

        /// <summary>Le trio dont elle est née (sa définition).</summary>
        public StructureDefinition FromTrio { get; private set; }

        /// <summary>Dans l'arène de MK (entre son arrivée et la fin du combat).</summary>
        public bool InArena { get; private set; }

        internal void SetupUltimate(TowerDuoCatalog.Entry entry, StructureDefinition first, StructureDefinition second,
                                    StructureDefinition third, StructureDefinition fromTrio)
        {
            SetupTrio(entry, first, second, third);
            FromTrio = fromTrio;
        }

        /// <summary>Sa taille dans l'arène de MK (fois la sienne) : la voûte de la grotte est basse près des parois.</summary>
        protected virtual float ArenaScale => 0.65f;

        /// <summary>Elle descend dans l'arène de MK (<c>ArenaTurrets</c>), réduite à <see cref="ArenaScale"/>.</summary>
        public virtual void EnterArena()
        {
            if (!InArena)
                m_ArenaScale = transform.localScale;
            InArena = true;
            transform.localScale = m_ArenaScale * Mathf.Max(0.1f, ArenaScale);
        }

        /// <summary>L'arène se ferme : elle retrouve sa taille (et sa place, rendue par <c>ArenaTurrets</c>).</summary>
        public virtual void LeaveArena()
        {
            if (!InArena)
                return;
            InArena = false;
            transform.localScale = m_ArenaScale;
        }
    }
}
