using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PickupItemOnInteractScript : MonoBehaviour
{
    [SerializeField] Renderer[] renderers;
    [SerializeField] Interactable interactable;
    [SerializeField] Item itemScript;
    [SerializeField] string sceneIDToTriggerDialog;

    [Header("Animation + Fade")]
    [SerializeField] Animator playerAnimator;
    [SerializeField] string faintTriggerName = "Faint";
    [SerializeField] ScreenFader fadeScript;
    [SerializeField] float faintDelay = 0.3f;
    [SerializeField] float fadeDelay = 1f;

    [Header("Scene Transition")]
    [Tooltip("Scene to load after faint and fade (must be added to Build Settings)")]
    public string sceneToLoad;

    [Header("Optional On Pickup")]
    [Tooltip("GameObject to activate when the item is picked up")]
    public GameObject objectToShowOnPickup;

    [Header("Optional Freeze/Disable Movement")]
    [Tooltip("Should player movement be disabled and frozen during faint?")]
    public bool disablePlayerMovementOnFaint = false;

    [Tooltip("The name of the movement script to disable (e.g. PlayerMovement). Leave blank to skip.")]
    public string movementScriptTypeName = "PlayerMovement";

    private GameObject playerObject;
    private MonoBehaviour playerMovementScript;
    private Rigidbody playerRigidbody;

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.eventOnInteract -= OnInteract;

        if (SimpleDialogManager.Instance != null)
            SimpleDialogManager.Instance.eventDialogueChanged -= OnDialogueChanged;
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType interactType)
    {
        if (interactType == InteractActionType.Interact)
        {
            InventoryScript inventoryScript = interactor.GetOwner().GetComponent<InventoryScript>();
            if (inventoryScript != null)
                inventoryScript.AddItem(itemScript);

            DisableRenderers();
            interactable.enabled = false;

            if (objectToShowOnPickup != null)
                objectToShowOnPickup.SetActive(true);

            if (!string.IsNullOrEmpty(sceneIDToTriggerDialog))
            {
                SimpleDialogManager.Instance.eventDialogueChanged += OnDialogueChanged;
                SimpleDialogManager.Instance.StartDialogue(sceneIDToTriggerDialog);
            }
        }
    }

    private void OnDialogueChanged(string treeName, string nodeKey)
    {
        if (treeName == sceneIDToTriggerDialog && string.IsNullOrEmpty(nodeKey))
        {
            SimpleDialogManager.Instance.eventDialogueChanged -= OnDialogueChanged;
            StartCoroutine(DelayedFaintAndFade());
        }
    }

    private IEnumerator DelayedFaintAndFade()
    {
        yield return new WaitForSeconds(faintDelay);

        if (disablePlayerMovementOnFaint)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                // Disable movement script
                if (!string.IsNullOrEmpty(movementScriptTypeName))
                {
                    System.Type type = System.Type.GetType(movementScriptTypeName);
                    if (type != null)
                    {
                        var movement = playerObject.GetComponent(type) as MonoBehaviour;
                        if (movement != null)
                        {
                            playerMovementScript = movement;
                            playerMovementScript.enabled = false;
                        }
                    }
                }

                // Freeze Rigidbody (if exists)
                playerRigidbody = playerObject.GetComponent<Rigidbody>();
                if (playerRigidbody != null)
                    playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        if (playerAnimator != null)
            playerAnimator.SetTrigger(faintTriggerName);

        yield return new WaitForSeconds(fadeDelay);

        if (fadeScript != null)
        {
            yield return StartCoroutine(fadeScript.FadeOut());
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void DisableRenderers()
    {
        foreach (var r in renderers)
        {
            if (r != null)
                r.enabled = false;
        }
    }
}
