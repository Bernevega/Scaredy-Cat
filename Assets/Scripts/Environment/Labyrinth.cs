using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Collider))]
public class Labyrinth : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Interactable monoclePickup;
    public Transform spawnPoint;
    public Transform spawnPoint2;
    public Image fadeImage;
    public Volume volume;
    [SerializeField] private Light directionalLight;

    [Header("Settings")]
    public float fadeDuration = 1f;
    public float triggerTime = 10f;
    public float maxVignetteIntensity = 0.6f;
    public float vignetteFadeOutSpeed = 1f;

    [Header("Jump-block Buffer")]
    [Tooltip("How far outside the trigger the jump block should still apply")]
    public float jumpDisableRadius = 1.2f;

    [Header("Light Settings")]
    [Tooltip("How long the light transition takes in seconds")]
    public float lightTransitionDuration = 2f;

    [Tooltip("Target light intensity while inside the zone")]
    public float insideLightIntensity = 0.2f;

    [Tooltip("Automatically stores the scene's original light intensity")]
    public float outsideLightIntensity = 1f;

    [Header("Respawn")]
    [Tooltip("How long to keep the screen black after respawn before fading back")]
    public float respawnHoldBlackSeconds = 1f;

    private Coroutine timerCoroutine;
    private Coroutine vignetteFadeOutCoroutine;
    private Coroutine lightCoroutine;

    private Vignette vignetteEffect;
    private bool isPlayerInside = false;

    private Collider zoneCollider;
    private PlayerMovement trackedPM;

    private readonly HashSet<Collider> playerCollidersInside =
        new HashSet<Collider>();

    public bool reviving { get; private set; } = false;

    void Start()
    {
        zoneCollider = GetComponent<Collider>();

        if (volume != null &&
            volume.profile != null &&
            volume.profile.TryGet(out Vignette v))
        {
            vignetteEffect = v;

            // Keep vignette enabled all the time.
            // Only intensity changes.
            vignetteEffect.active = true;
            vignetteEffect.intensity.overrideState = true;
            vignetteEffect.intensity.value = 0f;
        }

        if (monoclePickup != null)
        {
            monoclePickup.eventOnInteract += OnMonocleInteract;
        }

        // Remember the current scene lighting.
        // Do NOT change the light intensity here.
        if (directionalLight != null)
        {
            outsideLightIntensity = directionalLight.intensity;

            // Make sure entering the labyrinth can only make it darker.
            insideLightIntensity = Mathf.Min(
                insideLightIntensity,
                outsideLightIntensity
            );
        }

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
    }

    private void OnDestroy()
    {
        if (monoclePickup != null)
        {
            monoclePickup.eventOnInteract -= OnMonocleInteract;
        }
    }

    void OnMonocleInteract(
        Interactor interactor,
        Interactable interactable,
        InteractActionType action)
    {
        if (action == InteractActionType.Interact)
        {
            spawnPoint = spawnPoint2;
        }
    }

    void Update()
    {
        if (reviving)
            return;

        if (trackedPM != null && zoneCollider != null)
        {
            Vector3 playerPos = trackedPM.transform.position;
            Vector3 closest = zoneCollider.ClosestPoint(playerPos);
            float dist = Vector3.Distance(closest, playerPos);

            if (dist <= jumpDisableRadius)
            {
                trackedPM.blockJump = true;
            }
            else
            {
                trackedPM.blockJump = false;
                trackedPM = null;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (reviving)
            return;

        PlayerMovement pm =
            other.GetComponentInParent<PlayerMovement>();

        if (pm == null || !pm.CompareTag("Player"))
            return;

        playerCollidersInside.Add(other);

        // Prevent multiple player colliders from triggering everything.
        if (isPlayerInside)
            return;

        isPlayerInside = true;

        trackedPM = pm;
        pm.blockJump = true;

        if (vignetteFadeOutCoroutine != null)
        {
            StopCoroutine(vignetteFadeOutCoroutine);
            vignetteFadeOutCoroutine = null;
        }

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        // Start from no vignette.
        if (vignetteEffect != null)
        {
            vignetteEffect.intensity.value = 0f;
        }

        // Smoothly make the scene darker.
        StartLightLerp(insideLightIntensity);

        timerCoroutine =
            StartCoroutine(TriggerTimer(pm.gameObject));
    }

    void OnTriggerExit(Collider other)
    {
        if (reviving)
            return;

        PlayerMovement pm =
            other.GetComponentInParent<PlayerMovement>();

        if (pm == null || pm != trackedPM)
            return;

        playerCollidersInside.Remove(other);

        // Another player collider is still inside.
        if (playerCollidersInside.Count > 0)
            return;

        isPlayerInside = false;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        if (vignetteFadeOutCoroutine != null)
        {
            StopCoroutine(vignetteFadeOutCoroutine);
            vignetteFadeOutCoroutine = null;
        }

        if (vignetteEffect != null)
        {
            vignetteFadeOutCoroutine =
                StartCoroutine(FadeOutVignetteSmoothly());
        }

        // Return smoothly to original scene brightness.
        StartLightLerp(outsideLightIntensity);
    }

    IEnumerator TriggerTimer(GameObject player)
    {
        float elapsed = 0f;

        float startIntensity = 0f;

        if (vignetteEffect != null)
        {
            startIntensity =
                vignetteEffect.intensity.value;
        }

        while (elapsed < triggerTime)
        {
            if (!isPlayerInside)
            {
                timerCoroutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(elapsed / triggerTime);

            if (vignetteEffect != null)
            {
                vignetteEffect.intensity.value =
                    Mathf.Lerp(
                        startIntensity,
                        maxVignetteIntensity,
                        t
                    );
            }

            yield return null;
        }

        timerCoroutine = null;

        reviving = true;

        playerCollidersInside.Clear();

        bool restoreCollider = false;

        if (zoneCollider != null &&
            zoneCollider.enabled)
        {
            zoneCollider.enabled = false;
            restoreCollider = true;
        }

        yield return StartCoroutine(FadeIn());

        if (spawnPoint != null)
        {
            Rigidbody rb =
                player.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.MovePosition(spawnPoint.position);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                player.transform.position =
                    spawnPoint.position;
            }

            PlayerMovement pm =
                player.GetComponent<PlayerMovement>();

            if (pm != null)
            {
                pm.blockJump = false;
            }
        }
        else
        {
            Debug.LogWarning(
                "[Labyrinth] Spawn point not assigned!"
            );
        }

        // Reset vignette without disabling the component.
        if (vignetteEffect != null)
        {
            vignetteEffect.intensity.value = 0f;
        }

        if (respawnHoldBlackSeconds > 0f)
        {
            yield return new WaitForSeconds(
                respawnHoldBlackSeconds
            );
        }

        yield return StartCoroutine(FadeOut());

        if (restoreCollider &&
            zoneCollider != null)
        {
            zoneCollider.enabled = true;
        }

        isPlayerInside = false;
        reviving = false;

        StartLightLerp(outsideLightIntensity);
    }

    IEnumerator FadeOutVignetteSmoothly()
    {
        if (vignetteEffect == null)
            yield break;

        while (vignetteEffect.intensity.value > 0f)
        {
            vignetteEffect.intensity.value =
                Mathf.MoveTowards(
                    vignetteEffect.intensity.value,
                    0f,
                    vignetteFadeOutSpeed * Time.deltaTime
                );

            yield return null;
        }

        vignetteEffect.intensity.value = 0f;
        vignetteFadeOutCoroutine = null;
    }

    IEnumerator FadeIn()
    {
        if (fadeImage == null)
            yield break;

        Color c = fadeImage.color;
        float startAlpha = c.a;

        if (fadeDuration <= 0f)
        {
            c.a = 1f;
            fadeImage.color = c;
            yield break;
        }

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            float normalized =
                Mathf.Clamp01(t / fadeDuration);

            c.a = Mathf.Lerp(
                startAlpha,
                1f,
                normalized
            );

            fadeImage.color = c;

            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    IEnumerator FadeOut()
    {
        if (fadeImage == null)
            yield break;

        Color c = fadeImage.color;
        float startAlpha = c.a;

        if (fadeDuration <= 0f)
        {
            c.a = 0f;
            fadeImage.color = c;
            yield break;
        }

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            float normalized =
                Mathf.Clamp01(t / fadeDuration);

            c.a = Mathf.Lerp(
                startAlpha,
                0f,
                normalized
            );

            fadeImage.color = c;

            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
    }

    public void PauseFadeTimer()
    {
        if (reviving)
            return;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        if (vignetteFadeOutCoroutine != null)
        {
            StopCoroutine(vignetteFadeOutCoroutine);
            vignetteFadeOutCoroutine = null;
        }

        if (vignetteEffect != null)
        {
            vignetteFadeOutCoroutine =
                StartCoroutine(
                    FadeOutVignetteSmoothly()
                );
        }
    }

    public void ResumeFadeTimer(GameObject player)
    {
        if (reviving)
            return;

        if (!isPlayerInside)
            return;

        // Stop the fade-out before making the vignette darker again.
        if (vignetteFadeOutCoroutine != null)
        {
            StopCoroutine(vignetteFadeOutCoroutine);
            vignetteFadeOutCoroutine = null;
        }

        if (timerCoroutine == null)
        {
            timerCoroutine =
                StartCoroutine(
                    TriggerTimer(player)
                );
        }
    }

    // ---------------- LIGHT ----------------

    void StartLightLerp(float target)
    {
        if (directionalLight == null)
            return;

        if (lightCoroutine != null)
        {
            StopCoroutine(lightCoroutine);
            lightCoroutine = null;
        }

        lightCoroutine =
            StartCoroutine(LerpLight(target));
    }

    IEnumerator LerpLight(float target)
    {
        float startIntensity =
            directionalLight.intensity;

        if (lightTransitionDuration <= 0f)
        {
            directionalLight.intensity = target;
            lightCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < lightTransitionDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / lightTransitionDuration
            );

            // Smooth start and smooth finish.
            t = Mathf.SmoothStep(0f, 1f, t);

            directionalLight.intensity =
                Mathf.Lerp(
                    startIntensity,
                    target,
                    t
                );

            yield return null;
        }

        directionalLight.intensity = target;
        lightCoroutine = null;
    }
}