using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class ScreenFader : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("How long (seconds) the actual fade takes.")]
    public float fadeDuration = 1f;

    [Tooltip("How long to stay fully black before fading to transparent (FadeIn).")]
    public float holdBlackDuration = 1f;

    [Tooltip("Use unscaled time so fades ignore timescale.")]
    public bool useUnscaledTime = true;

    [Header("Curve")]
    [Tooltip("Fade curve (0..1). EaseInOut gives a smoother look than linear).")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Startup")]
    [Tooltip("Start as fully black (opaque). If true and auto-fade is on, will hold then fade out.")]
    public bool startBlack = true;

    [Tooltip("Automatically run FadeIn() on Start if startBlack is true.")]
    public bool autoFadeInOnStart = true;

    private Image img;
    private Coroutine currentRoutine;

    void Awake()
    {
        img = GetComponent<Image>();
        var c = img.color;

        if (startBlack)
        {
            c.a = 1f;               // start fully opaque
            img.raycastTarget = true;
        }
        else
        {
            c.a = 0f;               // start transparent
            img.raycastTarget = false;
        }

        img.color = c;
    }

    void Start()
    {
        if (autoFadeInOnStart && startBlack)
            currentRoutine = StartCoroutine(FadeIn());
    }

    /// <summary>
    /// Fades from current alpha to fully opaque (black). Blocks clicks during the fade.
    /// </summary>
    public IEnumerator FadeOut()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FadeRoutine(targetAlpha: 1f, preHold: 0f, postUnblock: false));
        yield return currentRoutine;
    }

    /// <summary>
    /// Holds on black for 'holdBlackDuration', then fades to transparent. Unblocks clicks at the end.
    /// If we're not currently black, this forces alpha to 1 first (instant), then holds & fades.
    /// </summary>
    public IEnumerator FadeIn()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);

        // Ensure we're black before the hold
        var c = img.color;
        c.a = 1f;
        img.color = c;

        currentRoutine = StartCoroutine(FadeRoutine(targetAlpha: 0f, preHold: holdBlackDuration, postUnblock: true));
        yield return currentRoutine;
    }

    /// <summary>
    /// Core fade with optional pre-hold and easing curve.
    /// </summary>
    private IEnumerator FadeRoutine(float targetAlpha, float preHold, bool postUnblock)
    {
        img.raycastTarget = true;

        // Optional hold at start (used for FadeIn)
        if (preHold > 0f)
        {
            if (useUnscaledTime) yield return new WaitForSecondsRealtime(preHold);
            else yield return new WaitForSeconds(preHold);
        }

        float startAlpha = img.color.a;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float normalized = Mathf.Clamp01(t / fadeDuration);
            float eased = fadeCurve != null ? fadeCurve.Evaluate(normalized) : normalized;

            Color c = img.color;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, eased);
            img.color = c;

            yield return null;
        }

        // Snap to target alpha
        {
            Color c = img.color;
            c.a = targetAlpha;
            img.color = c;
        }

        // Optionally unblock clicks when fully transparent
        img.raycastTarget = postUnblock ? false : true;
    }
}
