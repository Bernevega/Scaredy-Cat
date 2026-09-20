using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickToContinue : MonoBehaviour
{
    [Header("Timing")]
    public float activationDelay = 3f;
    public float textFadeDuration = 1f;
    public float screenFadeDuration = 1.5f;

    [Header("UI")]
    public CanvasGroup continueText;
    public CanvasGroup blackScreen;

    private bool canContinue = false;
    private bool isTransitioning = false;

    private void Start()
    {
        // Hide continue text
        if (continueText != null)
            continueText.alpha = 0f;

        // Make sure screen starts transparent
        if (blackScreen != null)
            blackScreen.alpha = 0f;

        StartCoroutine(ActivateContinue());
    }

    private void Update()
    {
        if (!canContinue || isTransitioning)
            return;

        // Any keyboard key or mouse button
        if (Input.anyKeyDown ||
            Input.GetMouseButtonDown(0) ||
            Input.GetMouseButtonDown(1) ||
            Input.GetMouseButtonDown(2))
        {
            StartCoroutine(ContinueToNextScene());
        }
    }

    private IEnumerator ActivateContinue()
    {
        yield return new WaitForSeconds(activationDelay);

        if (continueText != null)
            yield return StartCoroutine(
                FadeCanvasGroup(continueText, 0f, 1f, textFadeDuration)
            );

        canContinue = true;
    }

    private IEnumerator ContinueToNextScene()
    {
        isTransitioning = true;
        canContinue = false;

        // Fade text away
        if (continueText != null)
            yield return StartCoroutine(
                FadeCanvasGroup(continueText, continueText.alpha, 0f, textFadeDuration)
            );

        // Fade screen to black
        if (blackScreen != null)
            yield return StartCoroutine(
                FadeCanvasGroup(blackScreen, blackScreen.alpha, 1f, screenFadeDuration)
            );

        // Load next scene
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.LogWarning("There is no next scene in Build Settings.");
        }
    }

    private IEnumerator FadeCanvasGroup(
        CanvasGroup canvasGroup,
        float startAlpha,
        float endAlpha,
        float duration)
    {
        float elapsed = 0f;

        canvasGroup.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth easing instead of linear fade
            t = t * t * (3f - 2f * t);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }
}