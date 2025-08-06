using UnityEngine;

public class ScaleOnButtonAction : MonoBehaviour
{
    [SerializeField] UIButton button;
    [SerializeField] Transform targetTransform;

    [Space(10)]
    [SerializeField] Vector3 mouseDownScale = Vector3.one;

    [Space(10)]
    [SerializeField] Vector3 mouseUpScale = Vector3.one;

    [Space(10)]
    [SerializeField] Vector3 hoveEnterScale = Vector3.one;

    [Space(10)]
    [SerializeField] Vector3 hoveExitScale = Vector3.one;

    [Space(10)]
    [SerializeField] Vector3 targetScale = Vector3.one;
    [SerializeField] float transitionSpeed = 1;
    [SerializeField] bool isDirty = false;

    private void Awake()
    {
        button.EventButtonAction += OnButtonAction;
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.EventButtonAction -= OnButtonAction;
        }
    }

    private void Update()
    {
        if (!isDirty) return;

        targetTransform.localScale =
            Vector3.MoveTowards(targetTransform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);



        if (targetTransform.localScale == targetScale)
        {
            isDirty = false;
        }
    }

    private void OnButtonAction(UIButton b, UIButtonAction action)
    {
        switch (action)
        {
            case UIButtonAction.MouseDown:
                targetScale = mouseDownScale;
                break;
            case UIButtonAction.MouseUp:
                targetScale = mouseUpScale;
                break;
            case UIButtonAction.HoverEnter:
                targetScale = hoveEnterScale;
                break;
            case UIButtonAction.HoverExit:
                targetScale = hoveExitScale;
                break;
        }

        isDirty = true;
    }
}
