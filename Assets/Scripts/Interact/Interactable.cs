using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public delegate void InteractDelegate(Interactor interactor, Interactable interactable);
    public event InteractDelegate eventOnInteract;
    public event InteractDelegate eventForceUnselect;

    public void Interact(Interactor interactor)
    {
        Debug.Log("Interacted with : " + gameObject.name);
        eventOnInteract?.Invoke(interactor, this);
    }

    private void OnDestroy()
    {
        eventForceUnselect?.Invoke(null, this);
        eventOnInteract = null;
        eventForceUnselect = null;
    }

    private void OnDisable()
    {
        eventForceUnselect?.Invoke(null, this);
    }
}

