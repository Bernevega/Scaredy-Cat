using UnityEngine;

public class BraceletOnPickup : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] GameObject objectToDisable;

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
    }

    private void OnInteract(
        Interactor interactor,
        Interactable interactable,
        InteractActionType type)
    {
        if (type == InteractActionType.Interact)
        {
            if (objectToDisable)
                objectToDisable.SetActive(false);
        }
    }
}