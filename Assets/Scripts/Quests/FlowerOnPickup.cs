using UnityEngine;

public class FlowerOnPickup : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] GameObject ghostPaperObject;
    [SerializeField] GameObject logBlocker;

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType type)
    {
        if (type == InteractActionType.Interact)
        {
            SimpleDialogManager.Instance.StartDialogue("FlowerPickup");
            if (ghostPaperObject)
                ghostPaperObject.SetActive(true);
            if (logBlocker)
                logBlocker.SetActive(false);
        }
    }
}
