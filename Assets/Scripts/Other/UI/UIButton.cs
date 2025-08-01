using System;
using UnityEngine.EventSystems;
using UnityEngine;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Action<UIButton, UIButtonAction> EventButtonAction;

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        EventButtonAction?.Invoke(this, UIButtonAction.MouseDown);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        EventButtonAction?.Invoke(this, UIButtonAction.HoverEnter);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        EventButtonAction?.Invoke(this, UIButtonAction.HoverExit);
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        EventButtonAction?.Invoke(this, UIButtonAction.MouseUp);
    }
}

public enum UIButtonAction : byte
{
    MouseDown,
    MouseUp,
    HoverEnter,
    HoverExit
}