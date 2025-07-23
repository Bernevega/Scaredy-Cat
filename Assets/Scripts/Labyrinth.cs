using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Labyrinth : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Interactable monoclePickup;
    public Transform spawnPoint;
    public Transform spawnPoint2;
    public Image fadeImage;
    public Volume volume; // Leave this alone now

    [Header("Settings")]
    public float fadeDuration = 1f;
    public float triggerTime = 10f;
    public float maxVignetteIntensity = 0.6f;
    public float vignetteFadeOutSpeed = 1f;

    [Header("Jump-block Buffer")]
    [Tooltip("How far outside the trigger the jump/sprint block should still apply")]
    public float jumpDisableRadius = 1.2f;

    private Coroutine timerCoroutine;
    private Coroutine vignetteFadeOutCoroutine;
    private Vignette vignetteEffect;
    private bool isPlayerInside = false;

    // for buffering the jump block outside the collider
    private Collider zoneCollider;
    private PlayerMovement trackedPM;
    public bool reviving { get; private set; } = false;
    void Start()
    {
        // cache our trigger collider
        zoneCollider = GetComponent<Collider>();

        // Grab the Vignette from the profile and disable it initially
        if (volume != null && volume.profile.TryGet(out Vignette v))
        {
            vignetteEffect = v;
            vignetteEffect.intensity.value = 0f;
            vignetteEffect.active = false;
        }

        monoclePickup.eventOnInteract += OnMonocleInteract;
    }

    private void OnDestroy()
    {
        if (monoclePickup != null)
        {
            monoclePickup.eventOnInteract -= OnMonocleInteract;
        }
    }

    void OnMonocleInteract(Interactor interactor, Interactable interactable, InteractActionType action)
    {
        if (action == InteractActionType.Interact)
        {
            spawnPoint = spawnPoint2;
        }
    }

    void Update()
    {
        // advance fade/timer
        if (fadeImage != null && fadeImage.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            // not needed here, your triggers handle Space in Fade coroutines
        }

        // If we have a PlayerMovement to track, enforce jump/sprint block until they're outside buffer
        if (trackedPM != null)
        {
            Vector3 playerPos = trackedPM.transform.position;
            // get closest point on the trigger bounds
            Vector3 closest = zoneCollider.ClosestPoint(playerPos);
            float dist = Vector3.Distance(closest, playerPos);

            if (dist <= jumpDisableRadius)
            {
                // still within buffer: keep blocked
                trackedPM.blockJump = true;
                trackedPM.blockSprint = true;
            }
            else
            {
                // outside buffer: restore
                trackedPM.blockJump = false;
                trackedPM.blockSprint = false;
                trackedPM = null;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || vignetteEffect == null) return;

        isPlayerInside = true;

        // block jump/sprint immediately
        if (other.TryGetComponent<PlayerMovement>(out var pm))
        {
            pm.blockJump = true;
            pm.blockSprint = true;
            trackedPM = pm; // start tracking for buffer
        }

        // Stop any fades/timers
        if (vignetteFadeOutCoroutine != null) StopCoroutine(vignetteFadeOutCoroutine);
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        // Reset vignette
        vignetteEffect.intensity.value = 0f;
        vignetteEffect.active = true;

        timerCoroutine = StartCoroutine(TriggerTimer(other.gameObject));
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || vignetteEffect == null) return;

        isPlayerInside = false;

        // we no longer immediately unblock here—Update will wait until outside jumpDisableRadius
        // Stop the timer and fade the vignette out
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            StartCoroutine(FadeOut());
            timerCoroutine = null;
            vignetteFadeOutCoroutine = StartCoroutine(FadeOutVignetteSmoothly());
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
        reviving = true;
        yield return StartCoroutine(FadeIn());
        reviving = false;
        // Respawn
        if (spawnPoint != null)
        {
            //player.transform.position = spawnPoint.position;
            player.GetComponent<Rigidbody>().Move(spawnPoint.position, player.transform.rotation);
        }
            
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
