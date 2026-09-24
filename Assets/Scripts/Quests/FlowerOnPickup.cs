using UnityEngine;
using System.Collections;

public class FlowerOnPickup : MonoBehaviour
{
    [SerializeField] private Interactable interactable;

    [Header("Flower")]
    [Tooltip("Parent/object containing the flower visuals.")]
    [SerializeField] private GameObject flowerObject;

    [Tooltip("Another object that should appear together with the flower.")]
    [SerializeField] private GameObject additionalObjectToShow;

    [SerializeField] private GameObject logBlocker;
    [SerializeField] private GameObject objectToDisable;

    [Header("Dialogue Requirement")]
    [SerializeField] private string dialogueTreeName = "KittyCrowResque_";
    [SerializeField] private string finalNodeName = "kitty13final";

    private Renderer[] flowerRenderers;

    private bool hasBeenPickedUp = false;
    private bool flowerShown = false;
    private bool waitingForDialogueEnd = false;

    private void Awake()
    {
        // If no separate flower object is assigned,
        // use this object and all of its child renderers.
        if (flowerObject == null)
            flowerObject = gameObject;

        flowerRenderers =
            flowerObject.GetComponentsInChildren<Renderer>(true);

        HideFlower();

        // Hide the additional object at the start.
        if (additionalObjectToShow != null)
            additionalObjectToShow.SetActive(false);

        // Keep the flower object alive, but don't allow interaction yet.
        if (interactable != null)
        {
            interactable.enabled = false;
            interactable.eventOnInteract += OnInteract;
        }
    }

    private void Start()
    {
        // Subscribe here because the dialogue manager may not
        // exist yet during this object's Awake().
        if (SimpleDialogManager.Instance != null)
        {
            SimpleDialogManager.Instance.eventDialogueChanged +=
                OnDialogueChanged;
        }
        else
        {
            StartCoroutine(WaitForDialogueManager());
        }
    }

    private IEnumerator WaitForDialogueManager()
    {
        while (SimpleDialogManager.Instance == null)
            yield return null;

        SimpleDialogManager.Instance.eventDialogueChanged +=
            OnDialogueChanged;
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.eventOnInteract -= OnInteract;

        if (SimpleDialogManager.Instance != null)
        {
            SimpleDialogManager.Instance.eventDialogueChanged -=
                OnDialogueChanged;
        }
    }

    private void OnDialogueChanged(
        string treeName,
        string nodeKey
    )
    {
        if (flowerShown)
            return;

        if (waitingForDialogueEnd)
            return;

        if (treeName != dialogueTreeName)
            return;

        // Once the final dialogue line begins,
        // wait for the dialogue window to actually close.
        if (nodeKey == finalNodeName)
        {
            waitingForDialogueEnd = true;

            StartCoroutine(
                WaitUntilDialogueCloses()
            );
        }
    }

    private IEnumerator WaitUntilDialogueCloses()
    {
        // Wait one frame so the final line is definitely displayed.
        yield return null;

        SimpleDialogManager dialogueManager =
            SimpleDialogManager.Instance;

        if (dialogueManager == null)
        {
            waitingForDialogueEnd = false;
            yield break;
        }

        // Wait while the dialogue panel is still open.
        while (
            dialogueManager.dialogPanel != null &&
            dialogueManager.dialogPanel.activeSelf
        )
        {
            yield return null;
        }

        ShowFlower();

        waitingForDialogueEnd = false;
    }

    private void HideFlower()
    {
        if (flowerRenderers != null)
        {
            foreach (Renderer renderer in flowerRenderers)
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        flowerShown = false;
    }

    private void ShowFlower()
    {
        if (flowerShown)
            return;

        flowerShown = true;

        // Show flower renderers.
        if (flowerRenderers != null)
        {
            foreach (Renderer renderer in flowerRenderers)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
        }

        // Show the additional object.
        if (additionalObjectToShow != null)
            additionalObjectToShow.SetActive(true);

        // Flower can now be picked up.
        if (interactable != null)
            interactable.enabled = true;
    }

    private void OnInteract(
        Interactor interactor,
        Interactable interactableSender,
        InteractActionType type
    )
    {
        if (type != InteractActionType.Interact)
            return;

        if (!flowerShown)
            return;

        if (hasBeenPickedUp)
            return;

        hasBeenPickedUp = true;

        // FlowerPickup dialogue is handled by
        // PickupItemOnInteractScript.

        if (logBlocker != null)
            logBlocker.SetActive(false);

        if (objectToDisable != null)
            objectToDisable.SetActive(false);
    }
}