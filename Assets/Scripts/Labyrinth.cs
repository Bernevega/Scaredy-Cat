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
    public Volume volume; // Leave this alone now

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
        // Grab the Vignette from the profile and disable it initially
        if (volume != null && volume.profile.TryGet(out Vignette v))
        {
            vignetteEffect = v;
            vignetteEffect.intensity.value = 0f;
            vignetteEffect.active = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && vignetteEffect != null)
        {
            isPlayerInside = true;

            // Block jumping & sprinting while inside
            if (other.TryGetComponent<PlayerMovement>(out var pm))
            {
                pm.blockJump = true;
                pm.blockSprint = true;
            }

            // Stop any fades/timers
            if (vignetteFadeOutCoroutine != null) StopCoroutine(vignetteFadeOutCoroutine);
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);

            // Reset vignette
            vignetteEffect.intensity.value = 0f;
            vignetteEffect.active = true;

            timerCoroutine = StartCoroutine(TriggerTimer(other.gameObject));
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && vignetteEffect != null)
        {
            isPlayerInside = false;

            // Unblock jumping & sprinting on exit
            if (other.TryGetComponent<PlayerMovement>(out var pm))
            {
                pm.blockJump = false;
                pm.blockSprint = false;
            }

            // Stop the timer and fade the vignette out
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
                vignetteFadeOutCoroutine = StartCoroutine(FadeOutVignetteSmoothly());
            }
        }
    }

    IEnumerator TriggerTimer(GameObject player)
    {
        float elapsed = 0f;

        // Ramp up vignette intensity over triggerTime
        while (elapsed < triggerTime)
        {
            if (!isPlayerInside) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / triggerTime);
            vignetteEffect.intensity.value = Mathf.Lerp(0f, maxVignetteIntensity, t);
            yield return null;
        }

        // Full-screen fade to black
        yield return StartCoroutine(FadeIn());

        // Respawn
        if (spawnPoint != null)
            player.transform.position = spawnPoint.position;
        else
            Debug.LogWarning("Spawn point not assigned!");

        // Reset vignette immediately before fading back in
        vignetteEffect.intensity.value = 0f;
        vignetteEffect.active = false;

        yield return StartCoroutine(FadeOut());
    }

    IEnumerator FadeOutVignetteSmoothly()
    {
        // Smoothly ramp intensity back to zero
        while (vignetteEffect.intensity.value > 0f)
        {
            vignetteEffect.intensity.value -= Time.deltaTime * vignetteFadeOutSpeed;
            yield return null;
        }

        vignetteEffect.intensity.value = 0f;
        vignetteEffect.active = false;
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
    }

    // Called by LightPart to pause & smoothly fade out the timer/vignette
    public void PauseFadeTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        if (vignetteFadeOutCoroutine != null)
            StopCoroutine(vignetteFadeOutCoroutine);

        vignetteFadeOutCoroutine = StartCoroutine(FadeOutVignetteSmoothly());
    }

    // Called by LightPart to restart the fade timer from zero
    public void ResumeFadeTimer(GameObject player)
    {
        if (isPlayerInside && timerCoroutine == null)
        {
            vignetteEffect.intensity.value = 0f;
            vignetteEffect.active = true;
            timerCoroutine = StartCoroutine(TriggerTimer(player));
        }
    }
}
