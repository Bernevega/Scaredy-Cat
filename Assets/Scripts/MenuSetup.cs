using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MenuSetup : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The panel you want to slide")]
    public RectTransform uiElement;
    [Tooltip("The “Press any key” text")]
    public RectTransform pressAnyKeyText;
    [Tooltip("Your 4 menu buttons (assign in Inspector)")]
    public GameObject[] buttons;

    [Header("Movement & Timing")]
    [Tooltip("Where to end up (anchoredPosition)")]
    public Vector2 targetPosition;
    [Tooltip("Slide speed in units/sec")]
    public float slideSpeed = 200f;
    [Tooltip("How long buttons take to fade in (sec)")]
    public float buttonFadeDuration = 0.5f;

    bool _started;
    CanvasGroup _pressCG;
    CanvasGroup[] _buttonCGs;

    void Awake()
    {
        // Ensure the PressAnyKey text has a CanvasGroup
        _pressCG = pressAnyKeyText.gameObject.GetComponent<CanvasGroup>();
        if (_pressCG == null)
            _pressCG = pressAnyKeyText.gameObject.AddComponent<CanvasGroup>();

        // Ensure each button has a CanvasGroup
        _buttonCGs = new CanvasGroup[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            _buttonCGs[i] = buttons[i].GetComponent<CanvasGroup>();
            if (_buttonCGs[i] == null)
                _buttonCGs[i] = buttons[i].AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        // Initial states
        _pressCG.alpha = 1f;
        foreach (var cg in _buttonCGs)
        {
            cg.alpha = 0f;
            cg.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!_started && Input.anyKeyDown)
        {
            _started = true;
            StartCoroutine(DoTransition());
        }
    }

    IEnumerator DoTransition()
    {
        // 1) Slide your panel & fade out “Press any key”
        Vector2 startPos = uiElement.anchoredPosition;
        float distance = Vector2.Distance(startPos, targetPosition);
        float duration = distance / slideSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            uiElement.anchoredPosition = Vector2.Lerp(startPos, targetPosition, t);
            _pressCG.alpha = 1f - t;
            yield return null;
        }

        uiElement.anchoredPosition = targetPosition;
        _pressCG.alpha = 0f;
        pressAnyKeyText.gameObject.SetActive(false);

        // 2) Fade in buttons
        foreach (var cg in _buttonCGs)
            cg.gameObject.SetActive(true);

        elapsed = 0f;
        while (elapsed < buttonFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / buttonFadeDuration);
            foreach (var cg in _buttonCGs)
                cg.alpha = t;
            yield return null;
        }
    }
}
