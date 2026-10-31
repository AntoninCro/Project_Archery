using Archery.Bows;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Ce qu'un duo lance et que ta flèche peut crever (31 octobre : les baudruches du Lanceur de baudruches, la boule de verre de
    /// la Boule à neige) : la flèche disparaît dedans et le duo est prévenu (<see cref="Shot"/>). Comme la boule de l'Avalanche
    /// (<see cref="AvalancheBall"/>), mais pour n'importe quel duo.
    /// </summary>
    public class DuoShotTarget : MonoBehaviour, IArrowHitHandler, IArrowAbsorber
    {
        /// <summary>Une flèche tirée l'a touché.</summary>
        internal System.Action<DuoShotTarget> Shot;

        public bool OnArrowHit(in ArrowHit hit)
        {
            if (hit.IsShot)
                Shot?.Invoke(this);
            return false;
        }
    }
}
