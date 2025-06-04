using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    public delegate void InteractDelegate(Interactor interactor, Interactable interactable, InteractActionType interactType);
    public delegate void UnselectDelegate(Interactable interactable);
    public event InteractDelegate eventOnInteract;
    public event UnselectDelegate eventForceUnselect;
    public Collider coll;

    private void Awake()
    {
        coll = GetComponent<Collider>();
    }

    public void Interact(Interactor interactor)
    {
        Debug.Log("Interacted with : " + gameObject.name);
        eventOnInteract?.Invoke(interactor, this, InteractActionType.Interact);
    }

    public void Select(Interactor interactor)
    {
        Debug.Log("Interactable selected : " + gameObject.name);
        eventOnInteract?.Invoke(interactor, this, InteractActionType.Select);
    }

    public void Deselect(Interactor interactor)
    {
        Debug.Log("Interactable deselected : " + gameObject.name);
        eventOnInteract?.Invoke(interactor, this, InteractActionType.Deselect);
    }

    private void OnDestroy()
    {
        eventForceUnselect?.Invoke(this);
        eventOnInteract = null;
        eventForceUnselect = null;
    }

    private void OnDisable()
    {
        eventForceUnselect?.Invoke(this);

        if (coll)
            coll.enabled = false;
    }
}

