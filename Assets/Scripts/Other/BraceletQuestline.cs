using Unity.VisualScripting;
using UnityEngine;

public class BraceletQuestline : MonoBehaviour
{
    [SerializeField] Oyen oyen;
    [SerializeField] Interactable[] npcs; // Order of array must be the correct order of npcs to talk to

    [SerializeField] GameObject mouseObject;
    [SerializeField] GameObject NObject;
    [SerializeField] GameObject bracelet;
    [SerializeField] GameObject nextSceneTransition;

    public static int step = 0;

    private void Awake()
    {
        for (int i = 0; i < npcs.Length; i++) 
        {
            npcs[i].eventOnInteract += OnInteract;
        }
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType type)
    {
        if (type == InteractActionType.Interact)
        {
            SimpleDialogManager dm = SimpleDialogManager.Instance;
            if (dm != null && dm.dialogPanel != null && dm.dialogPanel.activeSelf)
                return;

            int targetNpcIndex = -2;
            bool correctNpc = false;
            for (int i = 0; i < npcs.Length; i++)
            {
                if (interactable == npcs[i])
                {
                    targetNpcIndex = i;
                    break;
                }
            }

            if (step - 1 == targetNpcIndex)
            {
                correctNpc = true;
                step++;
            }

            switch(targetNpcIndex)
            {
                case 0:
                    if (!correctNpc)
                        dm.StartDialogue("MangleStart");
                    else
                        dm.StartDialogue("MangleMouse");
                    break;
                case 1:
                    if (!correctNpc)
                        dm.StartDialogue("JDStart");
                    else
                        dm.StartDialogue("JDMouse");
                    break;
                case 2:
                    if (!correctNpc)
                        dm.StartDialogue("AssylaStart");
                    else
                        dm.StartDialogue("AssylaMouse");
                    break;
                case 3:
                    if (!correctNpc)
                        dm.StartDialogue("ChicaStart");
                    else
                    {
                        dm.StartDialogue("ChicaMouse");
                    }
                    break;
                case 4:
                    if (!correctNpc)
                        dm.StartDialogue("NStart");
                    else
                    {
                        SimpleDialogManager.Instance.eventDialogueChanged += OnDialogAdvance;
                        dm.StartDialogue("NMouse");
                    }
                    break;
            }
        }
    }

    private void OnDialogAdvance(string sceneID, string nextNode)
    {
        if (sceneID == "NMouse")
        {
            switch (nextNode)
            {
                case null:
                    mouseObject.SetActive(false);
                    NObject.SetActive(false);
                    break;
                case "MouseAppear":
                    mouseObject.SetActive(true);
                    break;
                case "AfterMouseGivesBracelet":
                    bracelet.SetActive(true);
                    break;
                    
            }
        }
        if (sceneID == "OyenThank" && nextNode == null)
        {
            Debug.Log("GO TO THE NEXT SCENE");
            SimpleDialogManager.Instance.eventDialogueChanged -= OnDialogAdvance;
            nextSceneTransition.SetActive(true);
        }
    }
}
