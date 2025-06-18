using Unity.VisualScripting;
using UnityEngine;

public class BraceletQuestline : MonoBehaviour
{
    [SerializeField] Oyen oyen;
    [SerializeField] Interactable[] npcs; // Order of array must be the correct order of npcs to talk to

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
            if (!dm) return;

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

            }
        }
    }
}
