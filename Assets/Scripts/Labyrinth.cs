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
    public Volume volume; 
    [SerializeField] private Light directionalLight; // Assign your main sun/directional light here

    [Header("Settings")]
    public float fadeDuration = 1f;
    public float triggerTime = 10f;
    public float maxVignetteIntensity = 0.6f;
    public float vignetteFadeOutSpeed = 1f;

    [Header("Jump-block Buffer")]
    [Tooltip("How far outside the trigger the jump block should still apply")]
    public float jumpDisableRadius = 1.2f;

    [Header("Light Settings")]
    [Tooltip("How fast the light intensity transitions (units per second)")]
    public float lightLerpSpeed = 1.5f;
    [Tooltip("Target light intensity while inside the zone")]
    public float insideLightIntensity = 0.2f;
    [Tooltip("Target light intensity when outside the zone")]
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

    // ---- Blackout/respawn state guard ----
    public bool reviving { get; private set; } = false;

    void Start()
    {
        zoneCollider = GetComponent<Collider>();

        if (volume != null && volume.profile.TryGet(out Vignette v))
        {
            vignetteEffect = v;
            vignetteEffect.intensity.value = 0f;
            vignetteEffect.active = false;
        }

        monoclePickup.eventOnInteract += OnMonocleInteract;

        if (directionalLight != null)
            directionalLight.intensity = outsideLightIntensity;
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
        if (trackedPM != null)
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
        if (!other.CompareTag("Player")) return;
        if (reviving) return; // Ignore while black/respawning

        isPlayerInside = true;

        if (other.TryGetComponent<PlayerMovement>(out var pm))
        {
            pm.blockJump = true;
            trackedPM = pm;
        }

        if (vignetteFadeOutCoroutine != null) StopCoroutine(vignetteFadeOutCoroutine);
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        if (vignetteEffect != null)
        {
            vignetteEffect.intensity.value = 0f;
            vignetteEffect.active = true;
        }

        StartLightLerp(insideLightIntensity);

        timerCoroutine = StartCoroutine(TriggerTimer(other.gameObject));
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (reviving) return; // Don’t touch fades while black

        isPlayerInside = false;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            StartCoroutine(FadeOut());
            timerCoroutine = null;

            if (vignetteEffect != null)
                vignetteFadeOutCoroutine = StartCoroutine(FadeOutVignetteSmoothly());
        }

        StartLightLerp(outsideLightIntensity);
    }

    IEnumerator TriggerTimer(GameObject player)
    {
        float elapsed = 0f;

        while (elapsed < triggerTime)
        {
            if (!isPlayerInside) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / triggerTime);
            if (vignetteEffect != null)
                vignetteEffect.intensity.value = Mathf.Lerp(0f, maxVignetteIntensity, t);
            yield return null;
        }

        // ---- Begin blackout/respawn section ----
        reviving = true;

        // Prevent re-triggering while black
        bool restoreCollider = false;
        if (zoneCollider != null && zoneCollider.enabled)
        {
            zoneCollider.enabled = false;
            restoreCollider = true;
        }

        // Full fade to black
        yield return StartCoroutine(FadeIn());

        // Teleport while black
        if (spawnPoint != null)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
                rb.MovePosition(spawnPoint.position);
            else
                player.transform.position = spawnPoint.position;

            // Make sure jump isn’t stuck blocked after teleport
            var pm = player.GetComponent<PlayerMovement>();
            if (pm != null) pm.blockJump = false;
        }
        else
        {
            Debug.LogWarning("Spawn point not assigned!");
        }

        // Reset vignette after teleport
        if (vignetteEffect != null)
        {
            vignetteEffect.intensity.value = 0f;
            vignetteEffect.active = false;
        }

        // Hold black
        if (respawnHoldBlackSeconds > 0f)
            yield return new WaitForSeconds(respawnHoldBlackSeconds);

        // Fade back from black (ALWAYS)
        yield return StartCoroutine(FadeOut());

        // Restore collider after we’re visible again
        if (restoreCollider && zoneCollider != null)
            zoneCollider.enabled = true;

        // Reset state
        isPlayerInside = false;
        reviving = false;

        // Softly restore outside lighting
        StartLightLerp(outsideLightIntensity);
    }

    IEnumerator FadeOutVignetteSmoothly()
    {
        if (vignetteEffect == null) yield break;

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
        if (fadeImage == null) yield break;
        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0, 1, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        // Ensure exact black
        c.a = 1f;
        fadeImage.color = c;
    }

    IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;
        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1, 0, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        // Ensure exact clear
        c.a = 0f;
        fadeImage.color = c;
    }

    public void PauseFadeTimer()
    {
        // New: don’t allow pausing while we’re black/respawning
        if (reviving) return;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        if (vignetteFadeOutCoroutine != null)
            StopCoroutine(vignetteFadeOutCoroutine);

        if (vignetteEffect != null)
            vignetteFadeOutCoroutine = StartCoroutine(FadeOutVignetteSmoothly());
    }

    public void ResumeFadeTimer(GameObject player)
    {
        // New: ignore resumes while black/respawning
        if (reviving) return;

        if (isPlayerInside && timerCoroutine == null)
        {
            if (vignetteEffect != null)
            {
                vignetteEffect.intensity.value = 0f;
                vignetteEffect.active = true;
            }
            timerCoroutine = StartCoroutine(TriggerTimer(player));
        }
    }

    // ---- Light helpers ----
    void StartLightLerp(float target)
    {
        if (directionalLight == null) return;

        if (lightCoroutine != null)
            StopCoroutine(lightCoroutine);

        lightCoroutine = StartCoroutine(LerpLight(target));
    }

    IEnumerator LerpLight(float target)
    {
        while (!Mathf.Approximately(directionalLight.intensity, target))
        {
            float current = directionalLight.intensity;
            float next = Mathf.MoveTowards(current, target, lightLerpSpeed * Time.deltaTime);
            directionalLight.intensity = next;
            yield return null;
        }
    }
}
