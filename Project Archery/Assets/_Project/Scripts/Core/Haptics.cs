using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Core
{
    /// <summary>
    /// Raccourcis pour faire vibrer la manette associée à un interacteur XRI.
    /// </summary>
    public static class Haptics
    {
        public static void Pulse(IXRInteractor interactor, float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f)
                return;

            if (interactor is XRBaseInputInteractor inputInteractor)
                inputInteractor.SendHapticImpulse(Mathf.Clamp01(amplitude), duration);
        }
    }
}
