using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class ScreenFader : MonoBehaviour
{
    [Tooltip("How long (seconds) the fade in/out takes.")]
    public float fadeDuration = 1f;

    private Image img;

    void Awake()
    {
        img = GetComponent<Image>();

        // Start fully transparent & allow clicks through
        Color c = img.color;
        c.a = 0f;
        img.color = c;
        img.raycastTarget = false;
    }

    void Start()
    {
        // Kick off fade-out coroutine properly
        StartCoroutine(FadeIn());
    }

    /// <summary>
    /// Fades the image from transparent to opaque, blocking clicks during the transition.
    /// </summary>
    public IEnumerator FadeOut()
    {
        img.raycastTarget = true;  // block everything now

        float elapsed = 0f;
        Color c = img.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            img.color = c;
            yield return null;
        }

        // ensure fully opaque
        c.a = 1f;
        img.color = c;
    }

    /// <summary>
    /// Fades the image from opaque to transparent, unblocking clicks once done.
    /// </summary>
    public IEnumerator FadeIn()
    {
        float elapsed = 0f;
        Color c = img.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            img.color = c;
            yield return null;
        }

        // ensure fully transparent & let clicks through
        c.a = 0f;
        img.color = c;
        img.raycastTarget = false;
    }
}
