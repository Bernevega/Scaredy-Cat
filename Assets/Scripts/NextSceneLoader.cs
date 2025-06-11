using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class NextSceneLoader : MonoBehaviour
{
    [Tooltip("Seconds to wait before starting fade and loading next scene.")]
    public float delaySeconds = 28f;

    [Tooltip("How long (in seconds) the fade-to-black takes.")]
    public float fadeOutDuration = 1f;

    [Tooltip("How long (in seconds) the fade-in takes at start.")]
    public float fadeInDuration = 1f;

    [Tooltip("A full-screen UI Image (black) whose alpha we'll animate.")]
    public Image fadeImage;

    private void Start()
    {
        if (fadeImage != null)
        {
            // Start fully black, block input during fade-in
            var c = fadeImage.color;
            c.a = 1f;
            fadeImage.color = c;
            fadeImage.raycastTarget = true;

            // Fade in to transparent
            StartCoroutine(FadeInImage());
        }

        // Begin scene load sequence after fade-in completes
        StartCoroutine(LoadNextSceneAfterDelay());
    }

    private IEnumerator LoadNextSceneAfterDelay()
    {
        // 1) Wait the initial delay plus fade-in time
        yield return new WaitForSeconds(delaySeconds + fadeInDuration);

        // 2) Fade to black
        if (fadeImage != null)
            yield return StartCoroutine(FadeOutImage());

        // 3) Load next scene
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex    = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextIndex);
        else
            Debug.LogWarning(
                $"NextSceneLoader: No scene at build index {nextIndex} (only {SceneManager.sceneCountInBuildSettings} scenes)."
            );
    }

    private IEnumerator FadeInImage()
    {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(elapsed / fadeInDuration);
            fadeImage.color = c;
            yield return null;
        }

        // Ensure fully transparent and allow input
        c.a = 0f;
        fadeImage.color = c;
        fadeImage.raycastTarget = false;
    }

    private IEnumerator FadeOutImage()
    {
        fadeImage.raycastTarget = true; // block input while fading
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeOutDuration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }
}
