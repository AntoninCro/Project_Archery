using Archery.Bows;
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
    /// suit dans l'arène de MK, comme les bâtiments qui tirent (<c>FinalBoss/ArenaTurrets</c>) ; les boss ne sont jamais
    /// tués d'un coup. Elle garde les trois tours de son trio (ses plans ne la font plus monter : elle n'a qu'un niveau).
    /// </remarks>
    public abstract class TowerUltimate : TowerTrio
    {
        /// <summary>
        /// Le temps (s) au moins entre deux effets uniques d'une ultime sur MK, dans l'arène (8 novembre au soir, le joueur : 15 s au
        /// lieu de 12 ; le Colosse le cloue, le Manège l'étourdit ; l'Horloge arrête le temps après ses douze heures).
        /// </summary>
        public const float MkUniqueInterval = 15f;

        const float k_MinShotPower = 0.25f;
        const float k_MaxShotPower = 3f;

        static Bow s_Bow;

        /// <summary>
        /// La force d'une flèche du joueur pour l'action « une par flèche » de l'ultime (le marteau, le cheval, l'écho), qui frappe
        /// d'autant (3 novembre, le joueur : « chaque action qui se passe à chaque flèche [...] scale en fonction de si le joueur fait
        /// un parfait, moyen, etc. ; de même avec la force de la flèche, la pluie réduit les dégâts ») : ses dégâts rapportés à ceux
        /// d'un tir « Bon » du même arc, avec les mêmes bonus (<see cref="Bow.MeleeDamage"/>). Parfait ×2, très bon ×1,28, bon ×1,
        /// moyen ×0,8, raté ×0,6 (les dégâts des qualités de tir, <c>Data/ShotTuning</c>) ; l'arc moins bandé, une flèche de la pluie
        /// ou du multishot, moins ; la corde de siège, le laser, plus ; de 0,25 à 3. Les parchemins n'y comptent pas : ils
        /// renforcent déjà les tours (<c>Structure Scroll Share</c>).
        /// </summary>
        protected static float ShotPower(ArrowHit hit)
        {
            if (s_Bow == null)
                s_Bow = FindAnyObjectByType<Bow>();
            var reference = s_Bow != null ? s_Bow.MeleeDamage(ShotGrade.Good) : 0f;
            if (reference <= 0f)
                return 1f;
            return Mathf.Clamp(hit.Damage / reference, k_MinShotPower, k_MaxShotPower);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetShotPower()
        {
            s_Bow = null;
        }

        /// <summary>Le trio dont elle est née (sa définition).</summary>
        public StructureDefinition FromTrio { get; private set; }

        internal void SetupUltimate(TowerDuoCatalog.Entry entry, StructureDefinition first, StructureDefinition second,
                                    StructureDefinition third, StructureDefinition fromTrio)
        {
            SetupTrio(entry, first, second, third);
            FromTrio = fromTrio;
        }

        /// <summary>Elle suit le joueur dans l'arène de MK, la première (2 novembre).</summary>
        public override bool FightsInArena => true;

        /// <summary>Sa taille dans l'arène de MK (fois la sienne) : la voûte de la grotte est basse près des parois.</summary>
        /// <remarks>L'entrée et la sortie de l'arène sont celles de tous les bâtiments depuis le 3 novembre (<see cref="Structure.EnterArena"/>).</remarks>
        protected override float ArenaScale => 0.65f;
    }
}
