using UnityEngine;

public class InteractableRenderWhenSelected : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] Renderer targetRenderer;

    private void Awake()
    {
        if (interactable == null )
        {
            interactable = GetComponent<Interactable>();
        }

        if (interactable != null)
        {
            interactable.eventOnInteract += OnInteract;
        }

        targetRenderer.enabled = false;
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType type)
    {
        if (type == InteractActionType.Select)
        {
            targetRenderer.enabled = true;
        }
        else if (type == InteractActionType.Deselect)
        {
            targetRenderer.enabled = false;
        }
    }
}
