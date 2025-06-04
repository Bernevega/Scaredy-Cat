using UnityEngine;

public class PickupItemOnInteractScript : MonoBehaviour
{
    
    [SerializeField] Renderer[] renderers;
    [SerializeField] Interactable interactable;
    [SerializeField] Item itemScript;

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.eventOnInteract -= OnInteract;
        }
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType interactType)
    {
        if (interactType == InteractActionType.Interact)
        {
            InventoryScript inventoryScript = interactor.GetOwner().GetComponent<InventoryScript>();

            if (inventoryScript != null)
            {
                inventoryScript.AddItem(itemScript);
            }

            DisableRenderers();

            interactable.enabled = false;
        }
    }

    private void DisableRenderers()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }
    }
}
