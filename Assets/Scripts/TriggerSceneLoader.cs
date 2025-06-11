using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class TriggerSceneLoader : MonoBehaviour
{
    [Tooltip("Full-screen UI Image (black) for fade in.")]
    public Image fadeImage;

    [Tooltip("Duration of the fade (seconds).")]
    public float fadeDuration = 1f;

    private bool _isTriggered = false;

    private void Awake()
    {
        // Ensure trigger collider is set as trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // Start fully transparent
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.raycastTarget = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isTriggered) return;
        if (other.CompareTag("Player"))
        {
            _isTriggered = true;
            StartCoroutine(FadeInAndLoadNextScene());
        }
    }

    private IEnumerator FadeInAndLoadNextScene()
    {
        if (fadeImage != null)
        {
            fadeImage.raycastTarget = true;
            float elapsed = 0f;
            Color c = fadeImage.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }
            c.a = 1f;
            fadeImage.color = c;
        }

        // Load next scene in build settings
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextIndex);
        else
            Debug.LogWarning("No next scene to load.");
    }
}
