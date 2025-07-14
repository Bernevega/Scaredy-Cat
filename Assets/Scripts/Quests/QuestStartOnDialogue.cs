using UnityEngine;

public class QuestStartOnDialogue : MonoBehaviour
{
    [SerializeField] Quest questToStart;
    [SerializeField] string sceneId;
    [SerializeField] string key;

    private void Start()
    {
        SimpleDialogManager dm = SimpleDialogManager.Instance;
        if (dm)
        {
            dm.eventDialogueChanged += OnDialogueAdvance;
        }
    }

    private void OnDestroy()
    {
        SimpleDialogManager dm = SimpleDialogManager.Instance;
        if (dm)
        {
            dm.eventDialogueChanged -= OnDialogueAdvance;
        }
    }

    private void OnDialogueAdvance(string sceneId, string currentKey)
    {
        if (currentKey == null)
            currentKey = "null";

        if (sceneId == this.sceneId && currentKey == key)
        {
            QuestManager.instance.AddQuest(questToStart);

        }
    }
}
