using UnityEngine;

[RequireComponent (typeof(Collider))]
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
        {
            owner = transform.parent.gameObject;
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        int nullIndex = -1;
        for (int i = 0; i < interactablesInBounds.Length; i++)
        {
            if (nullIndex == -1 && interactablesInBounds[i] == null)
            {
                nullIndex = i;
            }
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
        for (int i = 0;i < interactablesInBounds.Length;i++)
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
        if (Input.GetKeyDown(KeyCode.E))
        {
            InteractPressed();
        }
    }



    private void RecalculateClosestInteractable()
    {
        float closestDist = 
            interactTarget == null ? 
            float.MaxValue : 
            (transform.position - interactTarget.transform.position).sqrMagnitude;

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
            Interactable interactable = interactTarget.GetComponent<Interactable>();
            interactable?.Interact(this);
        }
    }

    private void InteractableTargetChanged(int targetIndex)
    {
        if (interactTarget != null)
        {
            Interactable interactable = interactTarget.GetComponent<Interactable>();

            if (interactable != null)
            {
                interactable.Deselect(this);
                interactable.eventForceUnselect -= InteractableDisabledOrDestroyed;
            }
        }

        if (targetIndex != -1)
        {
            interactTarget = interactablesInBounds[targetIndex];

            Interactable targetInteractable = interactTarget.GetComponent<Interactable>();

            if (targetInteractable != null)
            {
                targetInteractable.Select(this);
                targetInteractable.eventForceUnselect += InteractableDisabledOrDestroyed;
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
                interactable.Deselect(this);
                interactablesInBounds[i] = null;
                interactTarget = null;
            }
        }
    }

    public GameObject GetOwner() { return owner; }
}
