using Archery.Bows;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// La boule de neige du duo Avalanche (31 octobre), créée par <see cref="DuoAvalanche"/> : elle reçoit les flèches et les
    /// passe au duo, qui la fait éclater. La flèche disparaît dans la neige (elle ne se plante pas : la boule éclatera).
    /// </summary>
    public class AvalancheBall : MonoBehaviour, IArrowHitHandler, IArrowAbsorber
    {
        internal DuoAvalanche Owner;

        public bool OnArrowHit(in ArrowHit hit)
        {
            if (Owner != null && hit.IsShot)
                Owner.OnBallShot(this);
            return false;
        }
    }
}
