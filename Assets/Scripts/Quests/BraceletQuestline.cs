using Unity.VisualScripting;
using UnityEngine;

public class BraceletQuestline : MonoBehaviour
{
    [SerializeField] Oyen oyen;
    [SerializeField] Interactable[] npcs;

    [SerializeField] DialogueOnInteract mouse;
    [SerializeField] DialogueOnInteract jD;
    [SerializeField] DialogueOnInteract assyla;
    [SerializeField] GameObject braceletObject;
    [SerializeField] GameObject nextSceneTransition;
    [SerializeField] Quest quest;

    [Header("Player Control")]
    [SerializeField] PlayerMovement playerMovement;
    [SerializeField] Canvas interactionCanvas;

    private Interactable braceletInteractable;

    private void Start()
    {
        // NPC interactions
        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i] != null)
                npcs[i].eventOnInteract += OnInteract;
        }

        // Find the bracelet's Interactable,
        // even if the bracelet object starts disabled.
        if (braceletObject != null)
        {
            braceletInteractable =
                braceletObject.GetComponentInChildren<Interactable>(true);

            if (braceletInteractable != null)
            {
                braceletInteractable.eventOnInteract += OnBraceletInteract;
            }
        }

        // Dialogue events
        SimpleDialogManager dm = SimpleDialogManager.Instance;

        if (dm != null)
        {
            dm.eventDialogueChanged += OnDialogAdvance;
        }
    }

    private void OnInteract(
        Interactor interactor,
        Interactable interactable,
        InteractActionType type)
    {
    }

    private void OnBraceletInteract(
        Interactor interactor,
        Interactable interactable,
        InteractActionType type)
    {
        if (type != InteractActionType.Interact)
            return;

        // Player has actually picked up the bracelet.
        quest.description = "Bring the bracelet to Oyen!";
        QuestManager.instance.UpdateQuest(quest);
    }

    private void OnDialogAdvance(string sceneID, string nextNode)
    {
        switch (sceneID)
        {
            case "OyenStart":

                mouse.sceneId.value = "MouseCheese";

                if (nextNode == null)
                {
                    quest.description =
                        "Get Oyen's bracelet from the mouse!";

                    QuestManager.instance.AddQuest(quest);
                }

                break;


            case "MouseCheese":

                mouse.sceneId.value = "MouseCheeseWait";
                jD.sceneId.value = "JDCheese";

                if (nextNode == null)
                {
                    quest.description =
                        "Find a way to get cheese for Mousie!";

                    QuestManager.instance.UpdateQuest(quest);
                }

                break;


            case "JDCheese":

                if (nextNode == null)
                {
                    quest.description =
                        "Find a funny hat for John Daniel!";

                    QuestManager.instance.UpdateQuest(quest);
                }

                assyla.sceneId.value = "AssylaDance";
                jD.sceneId.value = "JDWait";

                break;


            case "AssylaGive":

                // Assyla gives the player the funny hat.
                if (nextNode == null)
                {
                    quest.description =
                        "Give the funny hat to John Daniel!";

                    QuestManager.instance.UpdateQuest(quest);
                }

                jD.sceneId.value = "JDGive";

                break;


            case "JDGive":

                jD.sceneId.value = "JDThank";
                mouse.sceneId.value = "MouseBraceletGive";

                // JD has received the hat and gives the player cheese.
                if (nextNode == null)
                {
                    quest.description =
                        "Give the cheese to Mousie!";

                    QuestManager.instance.UpdateQuest(quest);
                }

                break;


            case "MouseBraceletGive":

                // When the dialogue reaches boo17,
                // the next objective becomes picking up the bracelet.
                if (nextNode == "boo17")
                {
                    quest.description =
                        "Pick up Oyen's bracelet!";

                    QuestManager.instance.UpdateQuest(quest);
                }

                // Dialogue finished.
                if (nextNode == null)
                {
                    // Make the bracelet available to pick up.
                    if (braceletObject != null)
                        braceletObject.SetActive(true);

                    mouse.sceneId.value = "MouseThank";

                    // Fallback in case boo17 was skipped for any reason.
                    quest.description =
                        "Pick up Oyen's bracelet!";

                    QuestManager.instance.UpdateQuest(quest);
                }

                break;


            case "OyenThank":

                if (nextNode == null)
                {
                    // Bracelet returned to Oyen.
                    StartSceneTransition();
                }

                break;
        }
    }

    private void StartSceneTransition()
    {
        // Hide interaction canvas.
        if (interactionCanvas != null)
        {
            interactionCanvas.gameObject.SetActive(false);
        }

        // Activate next scene transition object.
        if (nextSceneTransition != null)
        {
            nextSceneTransition.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe NPC interactions.
        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i] == null)
                continue;

            npcs[i].eventOnInteract -= OnInteract;
        }

        // Unsubscribe bracelet interaction.
        if (braceletInteractable != null)
        {
            braceletInteractable.eventOnInteract -= OnBraceletInteract;
        }

        // Unsubscribe dialogue.
        if (SimpleDialogManager.Instance != null)
        {
            SimpleDialogManager.Instance.eventDialogueChanged -=
                OnDialogAdvance;
        }
    }
}