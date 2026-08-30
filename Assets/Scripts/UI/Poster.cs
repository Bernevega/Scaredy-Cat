using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class Poster : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("How long to wait before starting the fade out.")]
    public float startDelay = 5f;

    [Tooltip("How long the fade-out takes.")]
    public float fadeDuration = 0.5f;

    [Tooltip("Use unscaled time (ignores Time.timeScale).")]
    public bool useUnscaledTime = true;

    [Header("Behavior")]
    [Tooltip("Automatically start when this object becomes active.")]
    public bool autoStartOnEnable = true;

    [Tooltip("Destroy the object at the end instead of just deactivating it.")]
    public bool destroyInsteadOfDeactivate = false;

    private CanvasGroup _cg;
    private Coroutine _routine;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        // Make sure it starts fully visible & interactable
        _cg.alpha = 1f;
        _cg.interactable = true;
        _cg.blocksRaycasts = true;
    }

    void OnEnable()
    {
        if (autoStartOnEnable)
            StartFadeSequence();
    }

    public void StartFadeSequence()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(FadeOutThenDisable());
    }

    public void CancelFade()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
    }

    private System.Collections.IEnumerator FadeOutThenDisable()
    {
        // Delay
        if (startDelay > 0f)
        {
            float t = 0f;
            while (t < startDelay)
            {
                t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        // Fade
        float dur = Mathf.Max(0.0001f, fadeDuration);
        float elapsed = 0f;
        float startA = _cg.alpha;

        while (elapsed < dur)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float a = Mathf.Lerp(startA, 0f, Mathf.Clamp01(elapsed / dur));
            _cg.alpha = a;
            yield return null;
        }

        _cg.alpha = 0f;
        _cg.interactable = false;
        _cg.blocksRaycasts = false;

        // Finish
        if (destroyInsteadOfDeactivate)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);

        _routine = null;
    }
}
