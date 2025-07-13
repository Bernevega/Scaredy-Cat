using UnityEngine;
using TMPro;
public class QuestInfoBox : MonoBehaviour
{
    public RectTransform backGround;
    public TMP_Text questNameText;
    public TMP_Text questDescriptionText;

    public void SetQuest(Quest quest)
    {
        if (quest == null)
        {
            gameObject.SetActive(false);
            return;
        }
        else
        {
            gameObject.SetActive(true);
        }

        questNameText.text = quest.questName;
        questDescriptionText.text = quest.description;
    }
}
