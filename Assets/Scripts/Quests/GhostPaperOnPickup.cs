using UnityEngine;

public class GhostPaperOnPickup : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] GameObject ghostBlocker;

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType type)
    {
        if (type == InteractActionType.Interact)
        {
            SimpleDialogManager.Instance.StartDialogue("GhostPaperPickUp");
            ghostBlocker.SetActive(false);
        }
    }
}
