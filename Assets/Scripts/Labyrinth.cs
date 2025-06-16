using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Labyrinth : MonoBehaviour
{
    [Header("References")]
    public Transform spawnPoint;
    public Image fadeImage;
    public Volume volume; // Reference to the Volume component directly

    [Header("Settings")]
    public float fadeDuration = 1f;
    public float triggerTime = 10f;
    public float maxVignetteIntensity = 0.6f;
    public float vignetteFadeOutSpeed = 1f;

    private Coroutine timerCoroutine;
    private Coroutine vignetteFadeOutCoroutine;
    private Vignette vignetteEffect;
    private bool isPlayerInside = false;

    void Start()
    {
        if (volume != null)
        {
            volume.weight = 0f;
            if (volume.profile.TryGet(out Vignette vignette))
            {
                vignetteEffect = vignette;
                vignetteEffect.intensity.value = 0f;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && vignetteEffect != null)
        {
            isPlayerInside = true;

            if (vignetteFadeOutCoroutine != null)
                StopCoroutine(vignetteFadeOutCoroutine);

            vignetteEffect.intensity.value = 0f;
            volume.weight = 0f;

            timerCoroutine = StartCoroutine(TriggerTimer(other.gameObject));
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && timerCoroutine != null)
        {
            isPlayerInside = false;

            StopCoroutine(timerCoroutine);
            timerCoroutine = null;

            vignetteFadeOutCoroutine = StartCoroutine(FadeOutVignetteSmoothly());
        }
    }

    IEnumerator TriggerTimer(GameObject player)
    {
        float elapsed = 0f;

        // Gradually enable volume weight
        while (volume.weight < 1f)
        {
            volume.weight += Time.deltaTime / 0.5f;
            yield return null;
        }

        // Increase vignette intensity
        while (elapsed < triggerTime)
        {
            if (!isPlayerInside) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / triggerTime);
            vignetteEffect.intensity.value = Mathf.Lerp(0f, maxVignetteIntensity, t);
            yield return null;
        }

        yield return StartCoroutine(FadeIn());

        // ✅ Ensure respawn works correctly
        if (spawnPoint != null)
            player.transform.position = spawnPoint.position;
        else
            Debug.LogWarning("Spawn point not assigned!");

        ResetEffects();

        yield return StartCoroutine(FadeOut());
    }

    IEnumerator FadeOutVignetteSmoothly()
    {
        float start = vignetteEffect.intensity.value;

        while (vignetteEffect.intensity.value > 0f)
        {
            vignetteEffect.intensity.value -= Time.deltaTime * vignetteFadeOutSpeed;
            vignetteEffect.intensity.value = Mathf.Max(vignetteEffect.intensity.value, 0f);
            yield return null;
        }

        volume.weight = 0f;
    }

    IEnumerator FadeIn()
    {
        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0, 1, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        fadeImage.color = new Color(c.r, c.g, c.b, 1f);
    }

    IEnumerator FadeOut()
    {
        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1, 0, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        fadeImage.color = new Color(c.r, c.g, c.b, 0f);
    }

    void ResetEffects()
    {
        if (vignetteEffect != null)
            vignetteEffect.intensity.value = 0f;

        if (volume != null)
            volume.weight = 0f;
    }
}
