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
    private void Start()
    {
        for (int i = 0; i < npcs.Length; i++) 
        {
            npcs[i].eventOnInteract += OnInteract;
        }

        SimpleDialogManager dm = SimpleDialogManager.Instance;
        dm.eventDialogueChanged += OnDialogAdvance;
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType type)
    {
        
    }

    private void OnDialogAdvance(string sceneID, string nextNode)
    {
        switch (sceneID)
        {
            case "OyenStart":
                mouse.sceneId.value = "MouseCheese";
                if (nextNode == null)
                {
                    QuestManager.instance.AddQuest(quest);
                }
                break;
            case "MouseCheese":
                mouse.sceneId.value = "MouseCheeseWait";
                jD.sceneId.value = "JDCheese";
                if (nextNode == null)
                {
                    quest.description = "Find a way to get cheese for the mouse.";
                    QuestManager.instance.UpdateQuest(quest);
                }
                break;
            case "JDCheese":
                if (nextNode == null)
                {
                    quest.description = "Find a hat for John Daniel.";
                    QuestManager.instance.UpdateQuest(quest);
                }
                assyla.sceneId.value = "AssylaDance";
                jD.sceneId.value = "JDWait";
                break;
            case "AssylaGive":
                jD.sceneId.value = "JDGive";
                break;
            case "JDGive":
                jD.sceneId.value = "JDThank";
                mouse.sceneId.value = "MouseBraceletGive";
                if (nextNode == null)
                {
                    quest.description = "Get the bracelet from the mouse.";
                    QuestManager.instance.UpdateQuest(quest);
                }
                break;
            case "MouseBraceletGive":
                if (nextNode == null)
                {
                    braceletObject.SetActive(true);
                    mouse.sceneId.value = "MouseThank";

                    quest.description = "Give the bracelet to the orange cat.";
                    QuestManager.instance.UpdateQuest(quest);
                }
                break;
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i] == null) continue;
            npcs[i].eventOnInteract -= OnInteract;
        }

        if (SimpleDialogManager.Instance != null)
        {
            SimpleDialogManager.Instance.eventDialogueChanged -= OnDialogAdvance;
        }
    }
}
