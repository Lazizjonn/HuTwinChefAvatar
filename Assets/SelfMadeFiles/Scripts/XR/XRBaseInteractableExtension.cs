using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;


// You can use your project's namespace or a specific namespace for extensions

public static class XRBaseInteractableExtension
{
    public static void ForceDeselect(this UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable)
    {
        if (interactable.interactionManager != null && interactable is UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable selectable)
        {
            interactable.interactionManager.CancelInteractableSelection(selectable);
        }
        Assert.IsFalse(interactable.isSelected);
    }
}

public static class XRBaseInteractorExtension
{
    public static void ForceDeselect(this UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor interactor)
    {
        if (interactor.interactionManager != null && interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor selectInteractor)
        {
            interactor.interactionManager.CancelInteractorSelection(selectInteractor);
        }
        Assert.IsFalse(interactor.isSelectActive);
    }
}