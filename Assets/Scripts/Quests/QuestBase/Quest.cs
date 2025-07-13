using System;
using UnityEngine;

[Serializable]
public class Quest
{
    public string questName = "";
    public string description = "";
    public IQuestBehaviour questBehaviour;
    public bool completed = false;

    public void OnStart()
    {
        questBehaviour.OnStart();
    }
    public void OnComplete()
    {
        questBehaviour.OnComplete();
        completed = true;
    }
}

public interface IQuestBehaviour
{
    public void OnStart();
    public void OnComplete();
}