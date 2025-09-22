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

    private Coroutine timerCoroutine;
    private Coroutine vignetteFadeOutCoroutine;
    private Coroutine lightCoroutine;

    private Vignette vignetteEffect;
    private bool isPlayerInside = false;

    private Collider zoneCollider;
    private PlayerMovement trackedPM;
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

        reviving = true;
        yield return StartCoroutine(FadeIn());
        reviving = false;

        if (spawnPoint != null)
        {
            var rb = player.GetComponent<Rigidbody>();
            if (rb != null)
                rb.MovePosition(spawnPoint.position);
            else
                player.transform.position = spawnPoint.position;
        }
        else
        {
            Debug.LogWarning("Spawn point not assigned!");
        }

        if (vignetteEffect != null)
        {
            vignetteEffect.intensity.value = 0f;
            vignetteEffect.active = false;
        }

        yield return StartCoroutine(FadeOut());
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

    public void PauseFadeTimer()
    {
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