using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance { get; private set; }

    private readonly List<Quest> quests = new List<Quest>(3);

    [Header("Quests")]
    [SerializeField]
    private List<QuestInfoBox> questInfos =
        new List<QuestInfoBox>();

    [SerializeField]
    private Quest[] startingQuests;

    [Header("Quest UI")]
    [Tooltip("The QuestInfoPanel toggled by pressing Q.")]
    [FormerlySerializedAs("questPanels")]
    [SerializeField]
    private GameObject questInfoPanel;

    [Tooltip("The entire Quest Canvas that is hidden during dialogue.")]
    [SerializeField]
    private GameObject questUI;

    [Tooltip("Fade duration when pressing Q.")]
    [SerializeField]
    private float questPanelFadeDuration = 0.3f;

    [Tooltip("Fade duration for the entire Quest Canvas.")]
    [SerializeField]
    private float questUIFadeDuration = 0.3f;

    private CanvasGroup questInfoCanvasGroup;
    private CanvasGroup questUICanvasGroup;

    private Coroutine questInfoFade;
    private Coroutine questUIFade;

    private bool questInfoPanelIsVisible;
    private bool questUIIsVisible;
    private bool firstDialogueFinished;

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
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupQuestInfoPanel();
        SetupQuestUI();
    }

    private void Start()
    {
        if (startingQuests != null)
        {
            for (int i = 0; i < startingQuests.Length; i++)
            {
                AddQuest(startingQuests[i]);
            }
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void SetupQuestInfoPanel()
    {
        if (questInfoPanel == null)
        {
            Debug.LogWarning(
                "QuestInfoPanel has not been assigned.",
                this
            );

            return;
        }

        questInfoCanvasGroup =
            questInfoPanel.GetComponent<CanvasGroup>();

        if (questInfoCanvasGroup == null)
        {
            questInfoCanvasGroup =
                questInfoPanel.AddComponent<CanvasGroup>();
        }

        questInfoPanelIsVisible = true;
        questInfoPanel.SetActive(true);

        questInfoCanvasGroup.alpha = 1f;
        questInfoCanvasGroup.interactable = true;
        questInfoCanvasGroup.blocksRaycasts = true;
    }

    private void SetupQuestUI()
    {
        if (questUI == null)
        {
            Debug.LogWarning(
                "The complete Quest UI has not been assigned.",
                this
            );

            return;
        }

        // Keep it active so CanvasGroup fading still works.
        questUI.SetActive(true);

        questUICanvasGroup =
            questUI.GetComponent<CanvasGroup>();

        if (questUICanvasGroup == null)
        {
            questUICanvasGroup =
                questUI.AddComponent<CanvasGroup>();
        }

        // Hide the entire Quest UI before the first frame.
        firstDialogueFinished = false;
        questUIIsVisible = false;

        questUICanvasGroup.alpha = 0f;
        questUICanvasGroup.interactable = false;
        questUICanvasGroup.blocksRaycasts = false;
    }

    // Called by PlayerMovement when Q is pressed.
    public void ToggleQuestPanel()
    {
        // Do not allow the panel to appear before
        // the first dialogue has finished.
        if (!firstDialogueFinished)
            return;

        SetQuestInfoPanelVisible(
            !questInfoPanelIsVisible
        );
    }

    public void ShowQuestPanel()
    {
        SetQuestInfoPanelVisible(true);
    }

    public void HideQuestPanel()
    {
        SetQuestInfoPanelVisible(false);
    }

    private void SetQuestInfoPanelVisible(bool show)
    {
        if (questInfoPanel == null ||
            questInfoCanvasGroup == null)
        {
            return;
        }

        questInfoPanelIsVisible = show;

        if (questInfoFade != null)
            StopCoroutine(questInfoFade);

        questInfoFade = StartCoroutine(
            FadeQuestInfoPanel(show)
        );
    }

    private IEnumerator FadeQuestInfoPanel(bool show)
    {
        if (show)
            questInfoPanel.SetActive(true);

        questInfoCanvasGroup.interactable = false;
        questInfoCanvasGroup.blocksRaycasts = false;

        float startAlpha =
            questInfoCanvasGroup.alpha;

        float targetAlpha =
            show ? 1f : 0f;

        float duration =
            Mathf.Max(0f, questPanelFadeDuration);

        if (duration > 0f)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsedTime / duration
                );

                questInfoCanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        targetAlpha,
                        progress
                    );

                yield return null;
            }
        }

        questInfoCanvasGroup.alpha = targetAlpha;
        questInfoCanvasGroup.interactable = show;
        questInfoCanvasGroup.blocksRaycasts = show;

        if (!show)
            questInfoPanel.SetActive(false);

        questInfoFade = null;
    }

    // Called whenever a dialogue begins.
    public void NotifyDialogueStarted()
    {
        HideQuestUI();
    }

    // Called after a dialogue has completely faded away.
    public void NotifyDialogueFinished()
    {
        firstDialogueFinished = true;

        // Make sure the Q panel is visible again.
        ShowQuestInfoPanelImmediately();

        ShowQuestUI();
    }

    public void ShowQuestUI()
    {
        if (!firstDialogueFinished)
            return;

        SetQuestUIVisible(true);
    }

    public void HideQuestUI()
    {
        SetQuestUIVisible(false);
    }

    private void SetQuestUIVisible(bool show)
    {
        if (questUI == null ||
            questUICanvasGroup == null)
        {
            return;
        }

        questUIIsVisible = show;

        if (questUIFade != null)
            StopCoroutine(questUIFade);

        questUIFade = StartCoroutine(
            FadeQuestUI(show)
        );
    }

    private IEnumerator FadeQuestUI(bool show)
    {
        questUI.SetActive(true);

        questUICanvasGroup.interactable = false;
        questUICanvasGroup.blocksRaycasts = false;

        float startAlpha =
            questUICanvasGroup.alpha;

        float targetAlpha =
            show ? 1f : 0f;

        float duration =
            Mathf.Max(0f, questUIFadeDuration);

        if (duration > 0f)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsedTime / duration
                );

                questUICanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        targetAlpha,
                        progress
                    );

                yield return null;
            }
        }

        questUICanvasGroup.alpha = targetAlpha;
        questUICanvasGroup.interactable = show;
        questUICanvasGroup.blocksRaycasts = show;

        // Do not deactivate questUI because QuestManager
        // might be attached to it.
        questUIFade = null;
    }

    private void ShowQuestInfoPanelImmediately()
    {
        if (questInfoPanel == null ||
            questInfoCanvasGroup == null)
        {
            return;
        }

        if (questInfoFade != null)
        {
            StopCoroutine(questInfoFade);
            questInfoFade = null;
        }

        questInfoPanelIsVisible = true;
        questInfoPanel.SetActive(true);

        questInfoCanvasGroup.alpha = 1f;
        questInfoCanvasGroup.interactable = true;
        questInfoCanvasGroup.blocksRaycasts = true;
    }

    public void AddQuest(Quest quest)
    {
        if (quest == null)
            return;

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
            Debug.Log(
                "Added quest: " + quest.questName
            );

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
                return quests[i];
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
                questInfos[i].SetQuest(quests[i]);
            else
                questInfos[i].SetQuest(null);
        }
    }

    // Used by things such as the dance minigame.
    public void SetUIEnabled(bool enabled)
    {
        if (enabled)
        {
            if (firstDialogueFinished)
                ShowQuestUI();
        }
        else
        {
            HideQuestUI();
        }
    }
}