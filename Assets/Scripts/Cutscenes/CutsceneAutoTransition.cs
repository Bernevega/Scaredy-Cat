using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class CutsceneAutoTransition : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private AnimEventInvoker animEvent;
    [SerializeField] private string nextScene;

    [Tooltip("Seconds after the scene starts before fading to black and transitioning.")]
    [SerializeField] private float transitionStartDelay = 8f;

    [Header("Screen Fade")]
    [SerializeField] private Image fadeImage;

    [Tooltip("Black to clear when the scene starts.")]
    [SerializeField] private float screenFadeInDuration = 0.5f;

    [Tooltip("Clear to black before loading the next scene.")]
    [SerializeField] private float screenFadeOutDuration = 1f;

    [Header("Audio Fade")]
    [SerializeField] private float audioFadeInDuration = 1f;

    [Tooltip("Seconds after scene start before audio begins fading out.")]
    [SerializeField] private float fadeOutStartDelay = 7f;

    [SerializeField] private float audioFadeOutDuration = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float targetAudioVolume = 1f;

    private Coroutine audioFadeCoroutine;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (animEvent != null)
            animEvent.stringEvent += OnCutsceneEnd;

        // Start audio silent.
        AudioListener.volume = 0f;

        // Start screen completely black.
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;

            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = false;
        }
    }

    private void Start()
    {
        // Fade screen in immediately.
        StartCoroutine(
            FadeScreen(
                1f,
                0f,
                screenFadeInDuration
            )
        );

        // Handle audio.
        StartCoroutine(AudioSequence());

        // Automatically transition after the specified time.
        StartCoroutine(TransitionTimer());
    }

    private void OnDestroy()
    {
        if (animEvent != null)
            animEvent.stringEvent -= OnCutsceneEnd;
    }

    private IEnumerator TransitionTimer()
    {
        yield return new WaitForSecondsRealtime(
            transitionStartDelay
        );

        StartTransition();
    }

    private IEnumerator AudioSequence()
    {
        // Fade audio in.
        audioFadeCoroutine = StartCoroutine(
            FadeAudio(
                0f,
                targetAudioVolume,
                audioFadeInDuration
            )
        );

        yield return audioFadeCoroutine;

        // fadeOutStartDelay is measured from scene start,
        // so subtract the fade-in time.
        float remainingDelay = Mathf.Max(
            0f,
            fadeOutStartDelay - audioFadeInDuration
        );

        yield return new WaitForSecondsRealtime(
            remainingDelay
        );

        // Fade audio out.
        audioFadeCoroutine = StartCoroutine(
            FadeAudio(
                AudioListener.volume,
                0f,
                audioFadeOutDuration
            )
        );

        yield return audioFadeCoroutine;
    }

    private void OnCutsceneEnd(string eventName)
    {
        // Animation event can still trigger the transition
        // if it happens before the timer.
        StartTransition();
    }

    private void StartTransition()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;

        StartCoroutine(
            FadeScreenOutAndLoad()
        );
    }

    private IEnumerator FadeScreenOutAndLoad()
    {
        // Fade screen to black.
        if (fadeImage != null)
        {
            yield return StartCoroutine(
                FadeScreen(
                    fadeImage.color.a,
                    1f,
                    screenFadeOutDuration
                )
            );
        }
        else
        {
            yield return new WaitForSecondsRealtime(
                screenFadeOutDuration
            );
        }

        // Load next scene once completely black.
        if (!string.IsNullOrEmpty(nextScene))
        {
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            Debug.LogError(
                "[CutsceneAutoTransition] nextScene is empty!"
            );
        }
    }

    private IEnumerator FadeScreen(
        float startAlpha,
        float endAlpha,
        float duration
    )
    {
        if (fadeImage == null)
            yield break;

        if (duration <= 0f)
        {
            Color instantColor = fadeImage.color;
            instantColor.a = endAlpha;
            fadeImage.color = instantColor;

            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            // Smooth fade.
            t = t * t * (3f - 2f * t);

            float alpha = Mathf.Lerp(
                startAlpha,
                endAlpha,
                t
            );

            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;

            yield return null;
        }

        Color finalColor = fadeImage.color;
        finalColor.a = endAlpha;
        fadeImage.color = finalColor;
    }

    private IEnumerator FadeAudio(
        float startVolume,
        float endVolume,
        float duration
    )
    {
        if (duration <= 0f)
        {
            AudioListener.volume = endVolume;
            yield break;
        }

        float elapsed = 0f;

        AudioListener.volume = startVolume;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / duration
            );

            // Smooth fade.
            t = t * t * (3f - 2f * t);

            AudioListener.volume = Mathf.Lerp(
                startVolume,
                endVolume,
                t
            );

            yield return null;
        }

        AudioListener.volume = endVolume;
        audioFadeCoroutine = null;
    }
}