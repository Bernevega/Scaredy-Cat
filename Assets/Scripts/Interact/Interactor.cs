using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Interactor : MonoBehaviour
{
    [SerializeField] GameObject owner;
    [SerializeField] GameObject[] interactablesInBounds = new GameObject[5];

    Collider interactCollider;
    GameObject interactTarget;
    LayerMask interactLayer;

    private void Awake()
    {
        interactCollider = GetComponent<Collider>();

        interactLayer = LayerMask.GetMask("Interactable");
        interactCollider.includeLayers = interactLayer;
        interactCollider.excludeLayers = ~interactLayer;

        if (owner == null && transform.parent != null)
            owner = transform.parent.gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        int nullIndex = -1;
        for (int i = 0; i < interactablesInBounds.Length; i++)
        {
            if (nullIndex == -1 && interactablesInBounds[i] == null)
                nullIndex = i;
            if (interactablesInBounds[i] == other.gameObject)
            {
                nullIndex = -1;
                break;
            }
        }

        if (nullIndex != -1)
            interactablesInBounds[nullIndex] = other.gameObject;
        RecalculateClosestInteractable();
    }

    private void OnTriggerExit(Collider other)
    {
        for (int i = 0; i < interactablesInBounds.Length; i++)
        {
            if (interactablesInBounds[i] == other.gameObject)
            {
                interactablesInBounds[i] = null;
                break;
            }
        }

        RecalculateClosestInteractable();
    }

    private void Update()
    {
        // Only fire on F—dialog‐blocking happens in Crow/DialogActor
        if (Input.GetKeyDown(KeyCode.F))
            InteractPressed();
    }

    private void RecalculateClosestInteractable()
    {
        float closestDist =
            interactTarget == null
                ? float.MaxValue
                : (transform.position - interactTarget.transform.position).sqrMagnitude;

        int closestIndex = -1;

        for (int i = 0; i < interactablesInBounds.Length; i++)
        {
            if (interactablesInBounds[i] == interactTarget ||
                interactablesInBounds[i] == null)
                continue;

            float dist = (transform.position - interactablesInBounds[i].transform.position).sqrMagnitude;
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }

        InteractableTargetChanged(closestIndex);
    }

    private void InteractPressed()
    {
        if (interactTarget)
        {
            var interactable = interactTarget.GetComponent<Interactable>();
            interactable?.Interact(this);
        }
    }

    private void InteractableTargetChanged(int targetIndex)
    {
        if (interactTarget != null)
        {
            var previous = interactTarget.GetComponent<Interactable>();
            if (previous != null)
            {
                previous.Deselect(this);
            }
        }

        if (targetIndex != -1)
        {
            interactTarget = interactablesInBounds[targetIndex];
            var next = interactTarget.GetComponent<Interactable>();
            if (next != null)
            {
                next.Select(this);
                next.eventForceUnselect += InteractableDisabledOrDestroyed;
            }
        }
        else
        {
            interactTarget = null;
        }
    }

    private void InteractableDisabledOrDestroyed(Interactable interactable)
    {
        for (int i = 0; i < interactablesInBounds.Length; i++)
        {
            if (interactablesInBounds[i] == interactable.gameObject)
            {
                interactable.eventForceUnselect -= InteractableDisabledOrDestroyed;
                interactable.Deselect(this);
                interactablesInBounds[i] = null;
                interactTarget = null;
                Debug.Log("Set the thing to null!");
                break;
            }
        }
        for (int i = 0; i < interactablesInBounds.Length; i++)
        {
            if (interactablesInBounds[i] != null)
            {
                interactablesInBounds[0] = interactablesInBounds[i];
                interactablesInBounds[0].GetComponent<Interactable>().Select(this);
                interactTarget = interactablesInBounds[0];
                interactablesInBounds[i] = null;
                break;
            }
        }
    }

    public GameObject GetOwner() => owner;
}
