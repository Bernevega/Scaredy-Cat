using System;
using UnityEngine;

[Serializable]
public class Quest : MonoBehaviour
{
    public string questName = "";
    public string description = "";
    public IQuestBehaviour questBehaviour;
    public bool completed = false;

    public void OnStart()
    {
        if (questBehaviour != null)
        {
            questBehaviour.OnStart();
        }
        
    }
    public void OnComplete()
    {
        if (questBehaviour != null)
        {
            questBehaviour.OnComplete();
        }
       
        completed = true;
    }
}

public interface IQuestBehaviour
{
    public void OnStart();
    public void OnComplete();
}