using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class ScreenFader : MonoBehaviour
{
    public float fadeDuration = 0.5f;
    private Image img;
	
    void Awake()
    {
        img = GetComponent<Image>();
        var c = img.color;
        c.a = 0;
        img.color = c;
    }

    /// <summary>
    /// Fades the image from transparent to opaque.
    /// </summary>
    public IEnumerator FadeOut()
    {
        float elapsed = 0f;
        Color c = img.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            img.color = c;
            yield return null;
        }
    }

    /// <summary>
    /// Fades the image from opaque to transparent.
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
    }
}
