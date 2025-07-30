using UnityEngine;

public class Oyen : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] ItemScriptable braceletItemScriptable;
    [SerializeField] GameObject nextSceneTransition;
    [SerializeField] Animator animator;


    Item[] itemReturnArray = new Item[1];

    public bool waitingForBracelet = false;
    bool hasBracelet = false;
    bool firstNode = false;

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
        animator.SetBool("Sad", true);
    }

    private void Start()
    {
        SimpleDialogManager.Instance.eventDialogueChanged += OnDialogueAdvance;
    }

    private void OnDestroy()
    {
        SimpleDialogManager.Instance.eventDialogueChanged -= OnDialogueAdvance;
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType type)
    {
        if (type == InteractActionType.Interact)
        {
            CheckBracelet(interactor);
        }
    }

    private void OnDialogueAdvance(string sceneId, string currKey)
    {
        var dm = SimpleDialogManager.Instance;

        if (!(
            sceneId == "OyenStart" ||
            sceneId == "OyenWait" ||
            sceneId == "OyenThank"))
        {
            return;
        }

        if (dm.dialogueStart)
        {
            animator.SetBool("Sad", false);
        }
        else if (currKey == null && !hasBracelet)
        {
            animator.SetBool("Sad", true);
        }
        if (sceneId == "OyenThank")
        {
            if (currKey == null)
            {
                nextSceneTransition.SetActive(true);
            }
            else if (currKey == "boo2")
            {
                animator.SetBool("Sad", false);
                animator.SetBool("Happy", true);
            }
        }
        
    }

    private void CheckBracelet(Interactor interactor)
    {
        // Block if dialog active
        SimpleDialogManager dm = SimpleDialogManager.Instance;
        if (dm != null && dm.dialogPanel != null && dm.dialogPanel.activeSelf)
            return;

        if (!waitingForBracelet && !hasBracelet)
        {
            dm.StartDialogue("OyenStart");
            waitingForBracelet = true;
        }
        else if (waitingForBracelet && !hasBracelet)
        {
            InventoryScript invScript = interactor.GetOwner().GetComponent<InventoryScript>();
            int numItems = invScript.GetItem_WithScriptable(itemReturnArray, braceletItemScriptable);

            if (numItems > 0)
            {
                invScript.RemoveItem(itemReturnArray[0], -1);
                hasBracelet = true;

                dm.StartDialogue("OyenThank");
            }
            else
            {
                dm.StartDialogue("OyenWait");
            }
        }
        else if (waitingForBracelet && hasBracelet)
        {
            dm.StartDialogue("OyenThank");
        }
    }
}
