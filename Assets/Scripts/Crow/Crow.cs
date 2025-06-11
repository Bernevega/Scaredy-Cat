using UnityEngine;

public class Crow : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] ItemScriptable monocleLeftScriptable;
    [SerializeField] ItemScriptable monocleRightScriptable;

    [Space(25)]
    [SerializeField] GameObject monocleLeftObject;
    [SerializeField] GameObject monocleRightObject;
    [SerializeField] GameObject flowerObject;
    [SerializeField] GameObject bushObject;

    Item[] itemReturnArray = new Item[1];

    DialogueState dialogueState = DialogueState.FirstTalk;

    bool hasThanked = false;
    bool hasLeftMonocle = false;
    bool hasRightMonocle = false;

    public static bool hasBothMonocles { get; private set; } = false;

    enum DialogueState
    {
        FirstTalk,
        FirstMonocleWait,
        SecondMonocle,
        SecondMonocleWait,
        AllMonoclesGiven,
        Final,
        FinalFinal
    }

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
    }

    public void OnInteract(Interactor interactor, Interactable interactable, InteractActionType type)
    {
        if (type == InteractActionType.Interact)
        {
            if (dialogueState == DialogueState.FirstTalk)
            {
                StartDialogue();
            }
            else
            {
                checkMonocle(interactor);
            }
        }
    }

    public void checkMonocle(Interactor interactor)
    {
        if (dialogueState == DialogueState.FirstMonocleWait && !hasLeftMonocle)
        {
            InventoryScript inventoryScript = interactor.GetOwner().GetComponent<InventoryScript>();
            int numItem = inventoryScript.GetItem_WithScriptable(itemReturnArray, monocleLeftScriptable);

            if (numItem > 0)
            {
                inventoryScript.RemoveItem(itemReturnArray[0], -1);
                Destroy(itemReturnArray[0]);

                monocleLeftObject.SetActive(true);
                bushObject.SetActive(false);
                hasLeftMonocle = true;

                dialogueState = DialogueState.SecondMonocle;
            }

            StartDialogue();
        }
        else if (dialogueState == DialogueState.SecondMonocleWait && !hasRightMonocle)
        {
            InventoryScript inventoryScript = interactor.GetOwner().GetComponent<InventoryScript>();
            int numItem = inventoryScript.GetItem_WithScriptable(itemReturnArray, monocleRightScriptable);

            if (numItem > 0)
            {
                inventoryScript.RemoveItem(itemReturnArray[0], -1);
                Destroy(itemReturnArray[0]);

                monocleRightObject.SetActive(true);
                flowerObject.SetActive(true);
                hasRightMonocle = true;

                dialogueState = DialogueState.Final;
                hasBothMonocles = true;
            }

            StartDialogue();
        }
        else if (dialogueState == DialogueState.FinalFinal && !hasThanked)
        {
            StartDialogue();
            hasThanked = true;
        }
    }

    private void StartDialogue()
    {
        switch (dialogueState)
        {
            case DialogueState.FirstTalk:
                dialogueState = DialogueState.FirstMonocleWait;
                SimpleDialogManager.Instance.StartDialogue("CrowFirstTalk");
                break;
            case DialogueState.FirstMonocleWait:
                SimpleDialogManager.Instance.StartDialogue("CrowFirstMonocleWait");
                break;
            case DialogueState.SecondMonocle:
                dialogueState = DialogueState.SecondMonocleWait;
                SimpleDialogManager.Instance.StartDialogue("CrowSecondMonocle");
                break;
            case DialogueState.SecondMonocleWait:
                SimpleDialogManager.Instance.StartDialogue("CrowSecondMonocleWait");
                break;
            case DialogueState.Final:
                dialogueState = DialogueState.FinalFinal;
                SimpleDialogManager.Instance.StartDialogue("CrowFinal");
                break;
            case DialogueState.FinalFinal:
                SimpleDialogManager.Instance.StartDialogue("CrowThank");
                break;
        }
    }
}
