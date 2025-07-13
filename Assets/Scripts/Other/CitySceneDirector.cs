using UnityEngine;

public class CitySceneDirector : MonoBehaviour
{
    [SerializeField] QuestTalk startingQuest;

    private void Start()
    {
        QuestManager qm = QuestManager.instance;
        if (qm)
        {
            qm.AddQuest(startingQuest.quest);
        }
    }
}
