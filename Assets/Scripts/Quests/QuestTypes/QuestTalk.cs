using System;
using UnityEngine;

// For quests that only involve dialogue
public class QuestTalk : MonoBehaviour, IQuestBehaviour
{
    public Quest quest = new Quest();
    public QuestTalkStep[] steps;
    public uint step = 0;

    private void Awake()
    {
        quest.questBehaviour = this;
    }

    public void OnStart()
    {
        SimpleDialogManager dm = SimpleDialogManager.Instance;
        if (dm)
        {
            dm.eventDialogueChanged += OnQuestAdvance;
        }

        if (steps.Length > 0)
            quest.description = steps[0].description;
    }

    public void OnComplete()
    {
        SimpleDialogManager dm = SimpleDialogManager.Instance;
        if (dm)
        {
            dm.eventDialogueChanged -= OnQuestAdvance;
        }
    }

    private void OnQuestAdvance(string sceneId, string currKey)
    {
        if (step >= steps.Length) { return; }

        if (currKey == null)
            currKey = "null";

        QuestTalkStep qtStep = steps[step];
        if (sceneId == qtStep.sceneId &&
            currKey == qtStep.keyName)
        {
            step++;
            if (step < steps.Length)
            {
                qtStep = steps[step];
                quest.description = qtStep.description;
            }
            
            QuestManager qm = QuestManager.instance;
            if (qm)
            {
                if (step >= steps.Length)
                {
                    qm.CompleteQuest(quest.questName);
                }
                qm.UpdateQuest(quest);
            }
        }
    }
}

[Serializable]
public struct QuestTalkStep
{
    public string sceneId;
    public string keyName; // name of the dialogue key required to complete the step.
    public string description;
}
