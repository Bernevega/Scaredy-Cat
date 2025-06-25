using UnityEngine;

public class SpeechBubble : MonoBehaviour
{
    [SerializeField] Interactable interactable;

    Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;

        interactable.eventOnInteract += OnInteract;
        gameObject.SetActive(false);
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType action)
    {
        if (action == InteractActionType.Select)
        {
            Vector3 dirToCamera = (mainCam.transform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(dirToCamera);
            gameObject.SetActive(true);
        }
        else if (action == InteractActionType.Deselect)
        {
            gameObject.SetActive(false);
        }
    }
}
