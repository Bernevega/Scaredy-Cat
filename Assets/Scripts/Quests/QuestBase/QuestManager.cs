using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance { get; private set; }

    List<Quest> quests = new List<Quest>(3);
    [SerializeField] List<QuestInfoBox> questInfos = new List<QuestInfoBox>();
    public Action<Quest, UpdateType> eventQuestUpdated;
    [SerializeField] Quest[] startingQuests;
    [SerializeField] GameObject questPanels;
    [SerializeField] GameObject questUI;
    public enum UpdateType
    {
        Added,
        Removed,
        Update
    }

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }

        for (int i = 0; i < startingQuests.Length; i++)
        {
            AddQuest(startingQuests[i]);
        }

        UpdateUI();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            questPanels.SetActive(!questPanels.activeSelf);
        }
    }
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    public void AddQuest(Quest quest)
    {
        bool shouldAddQuest = true;
        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i].questName == quest.questName)
            {
                shouldAddQuest = false;
            }
        }

        if (shouldAddQuest)
        {
            Debug.Log("Added quest");
            quests.Add(quest);
            quest.OnStart();
        }

        UpdateUI();
    }

    public void UpdateQuest(Quest quest)
    {
        UpdateUI();
    }

    public Quest GetQuest(string questName)
    {
        for (int i = 0; i < quests.Count; i++)
        {
            if (quests[i].questName == questName)
            {
                return quests[i];
            }
        }

        return null;
    }

    public void CompleteQuest(string questName)
    {
        for (int i = 0;i < quests.Count;i++)
        {
            if (quests[i].questName == questName)
            {

                quests[i].OnComplete();
                quests.RemoveAt(i);
                break;
            }
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        for (int i = 0; i < questInfos.Count; i++)
        {
            if (i < quests.Count)
            {
                questInfos[i].SetQuest(quests[i]);
            }
            else
            {
                questInfos[i].SetQuest(null);
            }
        }
    }

    public void SetUIEnabled(bool b)
    {
        if (questUI)
            questUI.SetActive(b);
    }
}
