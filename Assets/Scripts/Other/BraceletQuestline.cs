using Unity.VisualScripting;
using UnityEngine;

public class BraceletQuestline : MonoBehaviour
{
    [SerializeField] Oyen oyen;
    [SerializeField] Interactable[] npcs;

    [SerializeField] DialogueOnInteract mouse;
    [SerializeField] DialogueOnInteract jD;
    [SerializeField] DialogueOnInteract assyla;
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
                mouse.sceneId = "MouseCheese";
                break;
            case "MouseCheese":
                mouse.sceneId = "MouseCheeseWait";
                jD.sceneId = "JDCheese";
                break;
            case "JDCheese":
                assyla.sceneId = "AssylaDance";
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
