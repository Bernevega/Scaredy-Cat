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

    [Header("Player Spawn After Load")]
    [Tooltip("Position to place the player at after the new scene loads")]
    public Vector3 playerSpawnPosition;

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

        if (playerAnimator != null)
            playerAnimator.SetTrigger(faintTriggerName);

        yield return new WaitForSeconds(fadeDelay);

        if (fadeScript != null)
        {
            yield return StartCoroutine(fadeScript.FadeOut());
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = playerSpawnPosition;
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
