using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance { get; private set; }

    private List<Quest> quests = new List<Quest>(3);

    [SerializeField] private List<QuestInfoBox> questInfos = new List<QuestInfoBox>();
    [SerializeField] private Quest[] startingQuests;
    [SerializeField] private GameObject questPanels;
    [SerializeField] private GameObject questUI;

    public Action<Quest, UpdateType> eventQuestUpdated;

    public enum UpdateType
    {
        Added,
        Removed,
        Update
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        for (int i = 0; i < startingQuests.Length; i++)
        {
            AddQuest(startingQuests[i]);
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // 🟡 Used by PlayerMovement.cs
    public void ToggleQuestPanel()
    {
        if (questPanels != null)
        {
            bool isActive = questPanels.activeSelf;
            questPanels.SetActive(!isActive);

            // Optional: Pause/unpause game or cursor here
            // Cursor.visible = !isActive;
            // Cursor.lockState = isActive ? CursorLockMode.Locked : CursorLockMode.None;
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
                break;
            }
        }

        if (shouldAddQuest)
        {
            Debug.Log("Added quest: " + quest.questName);
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
        for (int i = 0; i < quests.Count; i++)
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

    public void SetUIEnabled(bool enabled)
    {
        if (questUI != null)
        {
            questUI.SetActive(enabled);
        }
    }
}
