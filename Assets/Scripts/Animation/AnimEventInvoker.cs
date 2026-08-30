using System;
using UnityEngine;

public class AnimEventInvoker : MonoBehaviour
{
    public Action<string> stringEvent;

    public void StringEvent(string str)
    {
        stringEvent?.Invoke(str);
    }
}
