using UnityEngine;

public class OyenGraveyard : MonoBehaviour
{
    [SerializeField] GameObject firstDialogActor;

    private void Awake()
    {
        SimpleDialogManager dm = SimpleDialogManager.Instance;
        if (!dm)
        {
            throw new System.Exception("dialogue manager is null!");
        }    

        dm.eventDialogueChanged += OnDialogueChanged;
    }

    private void OnDialogueChanged(string sceneID, string nextKey)
    {
        switch (sceneID)
        {
            case "OyenStart":
                if (nextKey == null)
                {
                    firstDialogActor.SetActive(false);
                }
                break;
        }
    }
}
