using UnityEngine;

public class Gravestone : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] string dialogSceneID;

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType interactType)
    {
        var dm = SimpleDialogManager.Instance;

        if (interactType == InteractActionType.Interact &&
            dm != null &&
            !(dm.dialogPanel != null && dm.dialogPanel.activeSelf))
        {
            dm.StartDialogue(dialogSceneID);
        }
    }

    public void SetSceneID(string s)
    { dialogSceneID = s; }
}
