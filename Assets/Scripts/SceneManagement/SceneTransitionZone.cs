using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class SceneTransitionZone : MonoBehaviour
{
    [Tooltip("Exact name of the next Scene to load (as in Build Settings)")]
    public string nextSceneName;

    [Tooltip("Reference to your full-screen ScreenFader component")]
    public ScreenFader screenFader;

    [Tooltip("If true, only the player can trigger this; otherwise any collider works")]
    public bool onlyPlayer = true;

    [Header("Fade Settings")]
    [Tooltip("Override the ScreenFader's fade duration (seconds). If <= 0, the fader's own setting is used.")]
    public float fadeOutDurationOverride = -1f;

    [Tooltip("If no ScreenFader is assigned, wait this long (seconds) before loading the scene.")]
    public float fallbackWaitSeconds = 0f;

    [Header("Audio Fade Settings")]
    [Tooltip("How long all audio takes to fade in when the scene starts.")]
    public float audioFadeInDuration = 1f;

    [Tooltip("How long all audio takes to fade out during a scene transition.")]
    public float audioFadeOutDuration = 1f;

    [Range(0f, 1f)]
    [Tooltip("Normal game audio volume.")]
    public float targetAudioVolume = 1f;

    [Tooltip("Exact name of the Main Menu scene. Automatic scene-start fade-in is skipped there.")]
    public string mainMenuSceneName = "MainMenu";

    private Collider _col;
    private bool _isTransitioning = false;

    private bool _screenFadeFinished = false;
    private bool _audioFadeFinished = false;

    // Prevent several SceneTransitionZones in the same scene
    // from all starting the audio fade-in.
    private static int _lastFadeInSceneHandle = -1;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;

        Scene currentScene = SceneManager.GetActiveScene();

        // Main Menu should keep its normal audio.
        if (currentScene.name == mainMenuSceneName)
        {
            AudioListener.volume = targetAudioVolume;
            return;
        }

        // Only one SceneTransitionZone performs the fade-in.
        if (_lastFadeInSceneHandle != currentScene.handle)
        {
            _lastFadeInSceneHandle = currentScene.handle;

            // Mute immediately when the scene begins.
            AudioListener.volume = 0f;
        }
    }

    private void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.name == mainMenuSceneName)
            return;

        // Only the first transition zone in this scene should fade in.
        if (_lastFadeInSceneHandle == currentScene.handle)
        {
            StartCoroutine(FadeAudioInOnSceneStart());

            // Change the value so another SceneTransitionZone
            // doesn't also start the fade in Start().
            _lastFadeInSceneHandle = -currentScene.handle;
        }
    }

    private IEnumerator FadeAudioInOnSceneStart()
    {
        // Wait one frame so all AudioSources and scene objects
        // have finished initializing.
        yield return null;

        AudioListener.volume = 0f;

        yield return StartCoroutine(
            FadeAudioVolume(
                0f,
                targetAudioVolume,
                audioFadeInDuration
            )
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isTransitioning)
            return;

        if (onlyPlayer && !other.CompareTag("Player"))
            return;

        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        _isTransitioning = true;

        _screenFadeFinished = false;
        _audioFadeFinished = false;

        // Start both fades at the same time.
        StartCoroutine(FadeScreenOut());
        StartCoroutine(FadeAudioOut());

        // Wait for both fades to finish.
        while (!_screenFadeFinished || !_audioFadeFinished)
        {
            yield return null;
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError(
                "[SceneTransitionZone] nextSceneName is empty!"
            );

            yield break;
        }

        bool loadingMainMenu =
            nextSceneName == mainMenuSceneName;

        // Load next scene.
        SceneManager.LoadScene(nextSceneName);

        // AudioListener.volume is global, so if MainMenu does not
        // contain this script we need to restore its audio manually.
        if (loadingMainMenu)
        {
            AudioListener.volume = targetAudioVolume;
        }
    }

    private IEnumerator FadeScreenOut()
    {
        if (screenFader != null)
        {
            float originalDuration =
                screenFader.fadeDuration;

            if (fadeOutDurationOverride > 0f)
            {
                screenFader.fadeDuration =
                    fadeOutDurationOverride;
            }

            yield return StartCoroutine(
                screenFader.FadeOut()
            );

            if (fadeOutDurationOverride > 0f)
            {
                screenFader.fadeDuration =
                    originalDuration;
            }
        }
        else
        {
            if (fallbackWaitSeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    fallbackWaitSeconds
                );
            }
            else
            {
                Debug.LogWarning(
                    "[SceneTransitionZone] No ScreenFader assigned; skipping fade."
                );
            }
        }

        _screenFadeFinished = true;
    }

    private IEnumerator FadeAudioOut()
    {
        float startVolume =
            AudioListener.volume;

        yield return StartCoroutine(
            FadeAudioVolume(
                startVolume,
                0f,
                audioFadeOutDuration
            )
        );

        _audioFadeFinished = true;
    }

    private IEnumerator FadeAudioVolume(
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
    }
}