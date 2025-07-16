using UnityEngine;

public class DialogueOnInteract : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    public string sceneId;

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
    }

    private void OnDestroy()
    {
        interactable.eventOnInteract -= OnInteract;
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType actionType)
    {
        // Block if dialog active
        var dm = SimpleDialogManager.Instance;
        if (dm != null && dm.dialogPanel != null && dm.dialogPanel.activeSelf)
            return;

        if (actionType == InteractActionType.Interact)
        {
            dm.StartDialogue(sceneId);
        }
    }
}
