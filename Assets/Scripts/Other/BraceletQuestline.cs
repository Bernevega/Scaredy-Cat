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
                break;
            case "MouseCheese":
                mouse.sceneId.value = "MouseCheeseWait";
                jD.sceneId.value = "JDCheese";
                break;
            case "JDCheese":
                assyla.sceneId.value = "AssylaDance";
                break;
            case "AssylaGive":
                jD.sceneId.value = "JDGive";
                break;
            case "JDGive":
                jD.sceneId.value = "JDThank";
                mouse.sceneId.value = "MouseBraceletGive";
                break;
            case "MouseBraceletGive":
                if (nextNode == null)
                {
                    braceletObject.SetActive(true);
                    mouse.sceneId.value = "MouseThank";
                    mouse.gameObject.SetActive(false);
                }
                break;
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i] == null) continue;
            npcs[i].eventOnInteract += OnInteract;
        }

        if (SimpleDialogManager.Instance != null)
        {
            SimpleDialogManager.Instance.eventDialogueChanged -= OnDialogAdvance;
        }
    }
}
