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

    private Collider _col;
    private bool _isTransitioning = false;

    private void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isTransitioning) return;

        if (onlyPlayer && !other.CompareTag("Player"))
            return;

        StartCoroutine(FadeAndLoad());
    }

    private IEnumerator FadeAndLoad()
    {
        _isTransitioning = true;

        // 1) Fade to black (optionally with overridden duration)
        if (screenFader != null)
        {
            float originalDuration = screenFader.fadeDuration;

            if (fadeOutDurationOverride > 0f)
                screenFader.fadeDuration = fadeOutDurationOverride;

            yield return StartCoroutine(screenFader.FadeOut());

            // restore original setting
            if (fadeOutDurationOverride > 0f)
                screenFader.fadeDuration = originalDuration;
        }
        else
        {
            if (fallbackWaitSeconds > 0f)
                yield return new WaitForSecondsRealtime(fallbackWaitSeconds);
            else
                Debug.LogWarning("[SceneTransitionZone] No ScreenFader assigned; skipping fade.");
        }

        // 2) Load the scene
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogError("[SceneTransitionZone] nextSceneName is empty!");
    }
}