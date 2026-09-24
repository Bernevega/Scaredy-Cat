using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System;

public class PickupItemOnInteractScript : MonoBehaviour
{
    [SerializeField] Renderer[] renderers;
    [SerializeField] Interactable interactable;
    [SerializeField] Item itemScript;
    [SerializeField] string sceneIDToTriggerDialog;

    [Header("Animation + Fade")]
    [SerializeField] Animator playerAnimator;
    [SerializeField] string faintTriggerName = "Faint";
    [SerializeField] ScreenFader fadeScript;
    [SerializeField] float faintDelay = 0.3f;
    [SerializeField] float fadeDelay = 1f;

    [Header("Scene Transition")]
    [Tooltip("Scene to load after faint and fade (must be added to Build Settings)")]
    public string sceneToLoad;

    [Header("Audio Fade")]
    [Tooltip("If enabled, all audio fades in when the scene starts and fades out before the scene transition.")]
    [SerializeField] private bool fadeAudio = true;

    [Tooltip("How long all audio takes to fade in when this scene starts.")]
    [SerializeField] private float audioFadeInDuration = 1f;

    [Tooltip("How long all audio takes to fade out before loading the next scene.")]
    [SerializeField] private float audioFadeOutDuration = 1f;

    [Range(0f, 1f)]
    [Tooltip("Normal audio volume after fading in.")]
    [SerializeField] private float targetAudioVolume = 1f;

    [Header("Optional Freeze/Disable Movement")]
    [Tooltip("Should player movement be disabled and frozen during faint?")]
    public bool disablePlayerMovementOnFaint = false;

    [Tooltip("The name of the movement script to disable (e.g. PlayerMovement). Leave blank to skip.")]
    public string movementScriptTypeName = "PlayerMovement";

    [Header("Interaction UI (Proximity Prompt) - Uses Canvas")]
    [Tooltip("Canvas containing the interaction prompt UI (no CanvasGroup needed).")]
    [SerializeField] private Canvas interactionCanvas;

    [Tooltip("Radius around this object in which the interaction UI appears.")]
    [SerializeField] private float interactionRadius = 2.0f;

    [Tooltip("Fade duration for showing/hiding the interaction UI.")]
    [SerializeField] private float interactionFadeDuration = 0.25f;

    [Tooltip("Hide the interaction UI after pickup.")]
    [SerializeField] private bool hideInteractionUIOnPickup = true;

    private GameObject playerObject;
    private MonoBehaviour playerMovementScript;
    private Rigidbody playerRigidbody;

    // UI fade state
    private Graphic[] _uiGraphics;
    private Coroutine _uiFadeRoutine;
    private bool _isPlayerInRange = false;
    private bool _hasPickedUp = false;
    private float _currentUIAlpha = 0f;

    // Audio fade
    private Coroutine _audioFadeRoutine;

    private void Awake()
    {
        if (interactable != null)
            interactable.eventOnInteract += OnInteract;
        else
            Debug.LogError($"[{name}] Interactable reference is missing.");

        // Prepare UI graphics for manual alpha fading
        if (interactionCanvas != null)
        {
            _uiGraphics =
                interactionCanvas.GetComponentsInChildren<Graphic>(true);

            SetUIAlpha(0f);
            interactionCanvas.gameObject.SetActive(false);
        }

        // Only start silent if audio fading is enabled.
        if (fadeAudio)
            AudioListener.volume = 0f;
    }

    private void Start()
    {
        // Only fade scene audio in if enabled.
        if (fadeAudio)
        {
            _audioFadeRoutine = StartCoroutine(
                FadeAudio(
                    0f,
                    targetAudioVolume,
                    audioFadeInDuration
                )
            );
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.eventOnInteract -= OnInteract;

        if (SimpleDialogManager.Instance != null)
            SimpleDialogManager.Instance.eventDialogueChanged -= OnDialogueChanged;
    }

    private void Update()
    {
        if (_hasPickedUp)
            return;

        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        if (interactionCanvas != null && playerObject != null)
        {
            float sqrDist =
                (playerObject.transform.position - transform.position)
                .sqrMagnitude;

            bool inRangeNow =
                sqrDist <= interactionRadius * interactionRadius;

            if (inRangeNow != _isPlayerInRange)
            {
                _isPlayerInRange = inRangeNow;

                if (_isPlayerInRange)
                    StartUIFade(1f, interactionFadeDuration);
                else
                    StartUIFade(0f, interactionFadeDuration);
            }
        }
    }

    private void OnInteract(
        Interactor interactor,
        Interactable interactableSender,
        InteractActionType interactType
    )
    {
        if (_hasPickedUp)
            return;

        if (interactType != InteractActionType.Interact)
            return;

        var owner =
            interactor != null
                ? interactor.GetOwner()
                : null;

        var inventoryScript =
            owner != null
                ? owner.GetComponent<InventoryScript>()
                : null;

        if (inventoryScript != null && itemScript != null)
            inventoryScript.AddItem(itemScript);

        DisableRenderers();

        _hasPickedUp = true;

        if (interactable != null)
            interactable.enabled = false;

        if (
            hideInteractionUIOnPickup &&
            interactionCanvas != null
        )
        {
            StartUIFade(
                0f,
                interactionFadeDuration
            );
        }

        // If dialogue is configured, wait until it finishes.
        if (
            !string.IsNullOrEmpty(sceneIDToTriggerDialog) &&
            SimpleDialogManager.Instance != null
        )
        {
            SimpleDialogManager.Instance.eventDialogueChanged +=
                OnDialogueChanged;

            SimpleDialogManager.Instance.StartDialogue(
                sceneIDToTriggerDialog
            );
        }
        else
        {
            StartCoroutine(
                DelayedFaintAndFade()
            );
        }
    }

    private void OnDialogueChanged(
        string treeName,
        string nodeKey
    )
    {
        if (
            treeName == sceneIDToTriggerDialog &&
            string.IsNullOrEmpty(nodeKey)
        )
        {
            var dm = SimpleDialogManager.Instance;

            if (dm != null)
            {
                dm.eventDialogueChanged -=
                    OnDialogueChanged;
            }

            StartCoroutine(
                DelayedFaintAndFade()
            );
        }
    }

    private IEnumerator DelayedFaintAndFade()
    {
        if (faintDelay > 0f)
        {
            yield return new WaitForSeconds(
                faintDelay
            );
        }

        if (disablePlayerMovementOnFaint)
        {
            if (playerObject == null)
            {
                playerObject =
                    GameObject.FindGameObjectWithTag(
                        "Player"
                    );
            }

            if (playerObject != null)
            {
                if (!string.IsNullOrEmpty(movementScriptTypeName))
                {
                    var type =
                        FindTypeInAssemblies(
                            movementScriptTypeName
                        );

                    if (type != null)
                    {
                        var movement =
                            playerObject.GetComponent(type)
                            as MonoBehaviour;

                        if (movement != null)
                        {
                            playerMovementScript = movement;
                            playerMovementScript.enabled = false;
                        }
                    }
                    else
                    {
                        var behaviours =
                            playerObject.GetComponents<MonoBehaviour>();

                        foreach (var b in behaviours)
                        {
                            if (
                                b != null &&
                                b.GetType().Name ==
                                movementScriptTypeName
                            )
                            {
                                playerMovementScript = b;
                                playerMovementScript.enabled = false;
                                break;
                            }
                        }
                    }
                }

                playerRigidbody =
                    playerObject.GetComponent<Rigidbody>();

                if (playerRigidbody != null)
                {
                    playerRigidbody.constraints =
                        RigidbodyConstraints.FreezeAll;
                }
            }
        }

        if (
            playerAnimator != null &&
            !string.IsNullOrEmpty(faintTriggerName)
        )
        {
            playerAnimator.SetTrigger(
                faintTriggerName
            );
        }

        if (fadeDelay > 0f)
        {
            yield return new WaitForSeconds(
                fadeDelay
            );
        }

        bool screenFadeFinished = false;
        bool audioFadeFinished = false;

        // Screen fade always happens.
        StartCoroutine(
            FadeScreenAndMarkFinished(
                () => screenFadeFinished = true
            )
        );

        // Audio fade only happens when enabled.
        if (fadeAudio)
        {
            StartCoroutine(
                FadeAudioOutAndMarkFinished(
                    () => audioFadeFinished = true
                )
            );
        }
        else
        {
            audioFadeFinished = true;
        }

        // Wait for required fades.
        while (!screenFadeFinished || !audioFadeFinished)
        {
            yield return null;
        }

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(
                sceneToLoad
            );
        }
    }

    private IEnumerator FadeScreenAndMarkFinished(
        Action onFinished
    )
    {
        if (fadeScript != null)
        {
            yield return StartCoroutine(
                fadeScript.FadeOut()
            );
        }

        onFinished?.Invoke();
    }

    private IEnumerator FadeAudioOutAndMarkFinished(
        Action onFinished
    )
    {
        if (_audioFadeRoutine != null)
        {
            StopCoroutine(_audioFadeRoutine);
            _audioFadeRoutine = null;
        }

        float currentVolume =
            AudioListener.volume;

        _audioFadeRoutine = StartCoroutine(
            FadeAudio(
                currentVolume,
                0f,
                audioFadeOutDuration
            )
        );

        yield return _audioFadeRoutine;

        onFinished?.Invoke();
    }

    private IEnumerator FadeAudio(
        float startVolume,
        float endVolume,
        float duration
    )
    {
        if (duration <= 0f)
        {
            AudioListener.volume =
                endVolume;

            _audioFadeRoutine = null;

            yield break;
        }

        float elapsed = 0f;

        AudioListener.volume =
            startVolume;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            t =
                t * t *
                (3f - 2f * t);

            AudioListener.volume =
                Mathf.Lerp(
                    startVolume,
                    endVolume,
                    t
                );

            yield return null;
        }

        AudioListener.volume =
            endVolume;

        _audioFadeRoutine = null;
    }

    private void DisableRenderers()
    {
        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];

            if (r != null)
                r.enabled = false;
        }
    }

    // -------- Canvas-based UI Fade --------

    private void StartUIFade(
        float targetAlpha,
        float duration
    )
    {
        if (interactionCanvas == null)
            return;

        if (_uiFadeRoutine != null)
            StopCoroutine(_uiFadeRoutine);

        if (
            targetAlpha > 0f &&
            !interactionCanvas.gameObject.activeSelf
        )
        {
            interactionCanvas.gameObject.SetActive(true);
        }

        _uiFadeRoutine = StartCoroutine(
            FadeUIGraphics(
                targetAlpha,
                duration
            )
        );
    }

    private IEnumerator FadeUIGraphics(
        float targetAlpha,
        float duration
    )
    {
        if (_uiGraphics == null)
        {
            _uiGraphics =
                interactionCanvas.GetComponentsInChildren<Graphic>(
                    true
                );
        }

        float startAlpha =
            _currentUIAlpha;

        float t = 0f;

        float dur =
            Mathf.Max(
                0.01f,
                duration
            );

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;

            float a =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    Mathf.Clamp01(t / dur)
                );

            SetUIAlpha(a);

            yield return null;
        }

        SetUIAlpha(targetAlpha);

        if (
            Mathf.Approximately(
                targetAlpha,
                0f
            )
        )
        {
            interactionCanvas.gameObject.SetActive(false);
        }

        _uiFadeRoutine = null;
    }

    private void SetUIAlpha(float a)
    {
        _currentUIAlpha = a;

        if (_uiGraphics == null)
            return;

        for (int i = 0; i < _uiGraphics.Length; i++)
        {
            var g = _uiGraphics[i];

            if (g == null)
                continue;

            var c = g.color;
            c.a = a;
            g.color = c;

            var cr = g.canvasRenderer;

            if (cr != null)
                cr.SetAlpha(a);
        }
    }

    private static Type FindTypeInAssemblies(
        string typeName
    )
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        foreach (
            var asm in
            AppDomain.CurrentDomain.GetAssemblies()
        )
        {
            var t =
                asm.GetType(
                    typeName,
                    false
                );

            if (t != null)
                return t;

            try
            {
                var tt =
                    asm.GetTypes()
                    .FirstOrDefault(
                        x => x.Name == typeName
                    );

                if (tt != null)
                    return tt;
            }
            catch
            {
                // Dynamic assemblies can throw.
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            new Color(
                0.2f,
                0.8f,
                1f,
                0.35f
            );

        Gizmos.DrawWireSphere(
            transform.position,
            interactionRadius
        );
    }
#endif
}