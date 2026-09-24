using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class Oyen : MonoBehaviour
{
    [SerializeField] private Interactable interactable;
    [SerializeField] private ItemScriptable braceletItemScriptable;
    [SerializeField] private Animator animator;

    [Header("Player")]
    [Tooltip("Assign the player's movement script here.")]
    [SerializeField] private MonoBehaviour playerMovementScript;

    [Header("Scene Transition")]
    [Tooltip("Exact name of the scene to load.")]
    [SerializeField] private string nextSceneName;

    [Tooltip("Assign the ScreenFader used to fade the screen to black.")]
    [SerializeField] private ScreenFader screenFader;

    [Tooltip("How long the screen takes to fade to black.")]
    [SerializeField] private float screenFadeOutDuration = 3f;

    [Header("Audio Fade")]
    [Tooltip("How long all audio takes to fade in when the scene starts.")]
    [SerializeField] private float audioFadeInDuration = 1f;

    [Tooltip("How long all audio takes to fade out before loading the next scene.")]
    [SerializeField] private float audioFadeOutDuration = 3f;

    [Range(0f, 1f)]
    [Tooltip("Normal audio volume after fading in.")]
    [SerializeField] private float targetAudioVolume = 1f;

    [Header("Interaction UI (Proximity Prompt) - Uses Canvas")]
    [Tooltip("Canvas containing the interaction prompt UI (no CanvasGroup needed).")]
    [SerializeField] private Canvas interactionCanvas;

    [Tooltip("Radius around this object in which the interaction UI appears.")]
    [SerializeField] private float interactionRadius = 2.0f;

    [Tooltip("Fade duration for showing/hiding the interaction UI.")]
    [SerializeField] private float interactionFadeDuration = 0.25f;

    [Tooltip("Hide the interaction UI after completing the event.")]
    [SerializeField] private bool hideInteractionUIAfterEvent = false;

    private Item[] itemReturnArray = new Item[1];

    public bool waitingForBracelet = false;
    private bool hasBracelet = false;

    // UI fade state
    private GameObject playerObject;
    private Graphic[] _uiGraphics;
    private Coroutine _uiFadeRoutine;
    private bool _isPlayerInRange = false;
    private float _currentUIAlpha = 0f;

    // Audio fade
    private Coroutine _audioFadeRoutine;

    // Prevent duplicate transitions
    private bool _isTransitioning = false;

    // Keeps the movement script reference alive during scene change
    private static MonoBehaviour movementScriptToReenable;

    private void Awake()
    {
        if (interactable != null)
            interactable.eventOnInteract += OnInteract;

        if (animator != null)
            animator.SetBool("Sad", true);

        // Prepare UI graphics for manual alpha fading
        if (interactionCanvas != null)
        {
            _uiGraphics =
                interactionCanvas.GetComponentsInChildren<Graphic>(true);

            SetUIAlpha(0f);

            interactionCanvas.gameObject.SetActive(false);
        }

        // Start the scene silent.
        AudioListener.volume = 0f;
    }

    private void Start()
    {
        if (SimpleDialogManager.Instance != null)
        {
            SimpleDialogManager.Instance.eventDialogueChanged +=
                OnDialogueAdvance;
        }

        // Fade scene audio in.
        _audioFadeRoutine = StartCoroutine(
            FadeAudio(
                0f,
                targetAudioVolume,
                audioFadeInDuration
            )
        );
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.eventOnInteract -= OnInteract;

        if (SimpleDialogManager.Instance != null)
        {
            SimpleDialogManager.Instance.eventDialogueChanged -=
                OnDialogueAdvance;
        }
    }

    private void Update()
    {
        // Find player if needed
        if (playerObject == null)
        {
            playerObject =
                GameObject.FindGameObjectWithTag("Player");
        }

        // Player range check for interaction UI
        if (interactionCanvas != null && playerObject != null)
        {
            float sqrDist =
                (playerObject.transform.position -
                 transform.position).sqrMagnitude;

            bool inRangeNow =
                sqrDist <=
                interactionRadius *
                interactionRadius;

            if (inRangeNow != _isPlayerInRange)
            {
                _isPlayerInRange = inRangeNow;

                if (_isPlayerInRange)
                {
                    StartUIFade(
                        1f,
                        interactionFadeDuration
                    );
                }
                else
                {
                    StartUIFade(
                        0f,
                        interactionFadeDuration
                    );
                }
            }
        }

        // Hide interaction prompt while dialogue is open
        SimpleDialogManager dm =
            SimpleDialogManager.Instance;

        bool dialogOpen =
            dm != null &&
            dm.dialogPanel != null &&
            dm.dialogPanel.activeSelf;

        if (interactionCanvas != null)
        {
            if (dialogOpen)
            {
                if (_currentUIAlpha > 0f)
                {
                    StartUIFade(
                        0f,
                        interactionFadeDuration
                    );
                }
            }
            else
            {
                bool shouldStayHidden =
                    hideInteractionUIAfterEvent &&
                    hasBracelet;

                if (
                    _isPlayerInRange &&
                    _currentUIAlpha <= 0f &&
                    !shouldStayHidden
                )
                {
                    StartUIFade(
                        1f,
                        interactionFadeDuration
                    );
                }
            }
        }
    }

    private void OnInteract(
        Interactor interactor,
        Interactable interactableSender,
        InteractActionType type)
    {
        if (type != InteractActionType.Interact)
            return;

        if (_isTransitioning)
            return;

        CheckBracelet(interactor);

        if (
            hideInteractionUIAfterEvent &&
            interactionCanvas != null &&
            hasBracelet
        )
        {
            StartUIFade(
                0f,
                interactionFadeDuration
            );
        }
    }

    private void OnDialogueAdvance(
        string sceneId,
        string currKey)
    {
        SimpleDialogManager dm =
            SimpleDialogManager.Instance;

        if (!(sceneId == "OyenStart" ||
              sceneId == "OyenWait" ||
              sceneId == "OyenThank"))
        {
            return;
        }

        if (dm != null && dm.dialogueStart)
        {
            if (animator != null)
                animator.SetBool("Sad", false);
        }
        else if (currKey == null && !hasBracelet)
        {
            if (animator != null)
                animator.SetBool("Sad", true);
        }

        if (sceneId == "OyenThank")
        {
            // Final dialogue has finished.
            if (currKey == null)
            {
                if (_isTransitioning)
                    return;

                // Hide interaction UI permanently.
                if (interactionCanvas != null)
                    interactionCanvas.gameObject.SetActive(false);

                // Disable player movement during transition.
                DisablePlayerMovementUntilNextScene();

                // Oyen now handles the entire transition itself.
                StartCoroutine(
                    FadeOutAndLoadScene()
                );
            }
            else if (currKey == "boo2")
            {
                if (animator != null)
                {
                    animator.SetBool("Sad", false);
                    animator.SetBool("Happy", true);
                }
            }
        }
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        _isTransitioning = true;

        // Stop the scene-start audio fade if it is somehow
        // still running.
        if (_audioFadeRoutine != null)
        {
            StopCoroutine(_audioFadeRoutine);
            _audioFadeRoutine = null;
        }

        // ----------------------------------------
        // START AUDIO FADE OUT
        // ----------------------------------------

        float currentVolume =
            AudioListener.volume;

        _audioFadeRoutine = StartCoroutine(
            FadeAudio(
                currentVolume,
                0f,
                audioFadeOutDuration
            )
        );

        Coroutine audioFade =
            _audioFadeRoutine;

        // ----------------------------------------
        // START SCREEN FADE OUT
        // ----------------------------------------

        Coroutine screenFade = null;
        float originalScreenFadeDuration = 0f;

        if (screenFader != null)
        {
            originalScreenFadeDuration =
                screenFader.fadeDuration;

            if (screenFadeOutDuration > 0f)
            {
                screenFader.fadeDuration =
                    screenFadeOutDuration;
            }

            screenFade =
                StartCoroutine(
                    screenFader.FadeOut()
                );
        }
        else
        {
            Debug.LogWarning(
                "[Oyen] No ScreenFader assigned."
            );
        }

        // Both fades are already running simultaneously.
        // Wait for the screen fade.
        if (screenFade != null)
        {
            yield return screenFade;

            screenFader.fadeDuration =
                originalScreenFadeDuration;
        }

        // Then make sure the audio fade has also finished.
        if (audioFade != null)
            yield return audioFade;

        // ----------------------------------------
        // LOAD NEXT SCENE
        // ----------------------------------------

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(
                nextSceneName
            );
        }
        else
        {
            Debug.LogError(
                "[Oyen] nextSceneName is empty!"
            );

            _isTransitioning = false;
        }
    }

    private IEnumerator FadeAudio(
        float startVolume,
        float endVolume,
        float duration)
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

            // Smooth fade
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

    private void DisablePlayerMovementUntilNextScene()
    {
        if (playerMovementScript == null)
            return;

        playerMovementScript.enabled = false;

        movementScriptToReenable =
            playerMovementScript;

        SceneManager.sceneLoaded -=
            ReenableMovementAfterSceneLoad;

        SceneManager.sceneLoaded +=
            ReenableMovementAfterSceneLoad;
    }

    private static void ReenableMovementAfterSceneLoad(
        Scene scene,
        LoadSceneMode mode)
    {
        if (movementScriptToReenable != null)
            movementScriptToReenable.enabled = true;

        movementScriptToReenable = null;

        SceneManager.sceneLoaded -=
            ReenableMovementAfterSceneLoad;
    }

    private void CheckBracelet(
        Interactor interactor)
    {
        SimpleDialogManager dm =
            SimpleDialogManager.Instance;

        // Block interaction while dialogue is active.
        if (
            dm != null &&
            dm.dialogPanel != null &&
            dm.dialogPanel.activeSelf
        )
        {
            return;
        }

        if (dm == null)
            return;

        // Hide prompt immediately when dialogue starts.
        if (interactionCanvas != null)
        {
            StartUIFade(
                0f,
                interactionFadeDuration
            );
        }

        if (!waitingForBracelet && !hasBracelet)
        {
            dm.StartDialogue("OyenStart");

            waitingForBracelet = true;
        }
        else if (
            waitingForBracelet &&
            !hasBracelet
        )
        {
            InventoryScript invScript =
                interactor
                .GetOwner()
                .GetComponent<InventoryScript>();

            int numItems =
                invScript.GetItem_WithScriptable(
                    itemReturnArray,
                    braceletItemScriptable
                );

            if (numItems > 0)
            {
                invScript.RemoveItem(
                    itemReturnArray[0],
                    -1
                );

                hasBracelet = true;

                dm.StartDialogue("OyenThank");
            }
            else
            {
                dm.StartDialogue("OyenWait");
            }
        }
        else if (
            waitingForBracelet &&
            hasBracelet
        )
        {
            dm.StartDialogue("OyenThank");
        }
    }

    // ---------- Canvas-based UI Fade ----------

    private void StartUIFade(
        float targetAlpha,
        float duration)
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

        _uiFadeRoutine =
            StartCoroutine(
                FadeUIGraphics(
                    targetAlpha,
                    duration
                )
            );
    }

    private IEnumerator FadeUIGraphics(
        float targetAlpha,
        float duration)
    {
        if (_uiGraphics == null)
        {
            _uiGraphics =
                interactionCanvas
                .GetComponentsInChildren<Graphic>(true);
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
            Graphic g =
                _uiGraphics[i];

            if (g == null)
                continue;

            Color c =
                g.color;

            c.a = a;
            g.color = c;

            CanvasRenderer cr =
                g.canvasRenderer;

            if (cr != null)
                cr.SetAlpha(a);
        }
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