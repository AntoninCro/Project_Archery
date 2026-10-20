using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Core
{
    /// <summary>
    /// Pour ce qu'on prend en le visant (couvercle et orbes des coffres, gisements de cristaux) : la grosse gâchette (activer)
    /// marche comme la gâchette de prise, sur le côté de la manette (sélectionner). L'objet l'appelle à chaque image : si une
    /// main qui le vise, les mains libres, vient d'appuyer sur la gâchette, il est pris par cette main, exactement comme avec
    /// la prise (mêmes conditions de distance). XRI le relâche à l'image suivante, la prise n'étant pas tenue.
    /// </summary>
    public static class TriggerPick
    {
        public static void Process(XRBaseInteractable interactable)
        {
            if (interactable == null || !interactable.isHovered || interactable.interactionManager == null)
                return;

            var hovering = interactable.interactorsHovering;
            for (var i = 0; i < hovering.Count; i++)
            {
                if (!(hovering[i] is XRBaseInputInteractor hand) || hand.hasSelection)
                    continue;

                var selector = (IXRSelectInteractor)hand;
                if (!hand.activateInput.ReadWasPerformedThisFrame() || !interactable.IsSelectableBy(selector))
                    continue;

                interactable.interactionManager.SelectEnter(selector, (IXRSelectInteractable)interactable);
                return;
            }
        }
    }
}
