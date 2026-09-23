using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class DialogActor : MonoBehaviour
{
    [Tooltip("Must match a top-level key in your JSON, e.g. \"HomeScene\", \"FriendsScene\"")]
    public string sceneID;

    [Tooltip("How close the Player has to be to trigger this NPC's dialog.")]
    public float triggerRadius = 3f;

    [Tooltip("If true, this dialog fires automatically on Start.")]
    public bool autoStartOnLoad = false;

    [Tooltip("Delay before automatically starting the dialog.")]
    public float autoStartDelay = 0f;

    [Tooltip("If true, the interaction key must be pressed while in range.")]
    public bool requireKeyPress = true;

    [Tooltip("The key used to start the dialogue.")]
    public KeyCode interactionKey = KeyCode.F;

    [Tooltip("Drag the interaction prompt GameObject here.")]
    public GameObject interactionPrompt;

    [Tooltip("If true, the dialogue can be triggered multiple times.")]
    public bool repeatable = false;

    [Header("Prompt Fade")]
    [Tooltip("Seconds for the interaction prompt to fade in and out.")]
    public float promptFadeDuration = 0.25f;

    private bool _hasInteracted;
    public bool HasInteracted => _hasInteracted;

    private int _interactCount;
    private Transform _playerTransform;

    private CanvasGroup _promptGroup;
    private Coroutine _fadeRoutine;

    // Prevents the fade coroutine from being restarted every frame.
    private bool _promptShouldBeVisible;

    private void Start()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");

        if (playerGO != null)
        {
            _playerTransform = playerGO.transform;
        }
        else
        {
            Debug.LogWarning($"[{name}] No GameObject tagged 'Player' found.");
        }

        SetupPrompt();

        if (autoStartOnLoad && !_hasInteracted)
        {
            StartCoroutine(AutoStartAfterDelay());
        }
    }

    private void SetupPrompt()
    {
        if (interactionPrompt == null)
        {
            Debug.LogError($"[{name}] Interaction prompt is not assigned.");
            return;
        }

        _promptGroup = interactionPrompt.GetComponent<CanvasGroup>();

        if (_promptGroup == null)
        {
            _promptGroup = interactionPrompt.AddComponent<CanvasGroup>();
        }

        _promptGroup.alpha = 0f;
        _promptGroup.interactable = false;
        _promptGroup.blocksRaycasts = false;

        _promptShouldBeVisible = false;

        // Keep the prompt active so Unity initializes it when the scene loads.
        interactionPrompt.SetActive(true);
        Canvas.ForceUpdateCanvases();
    }

    private IEnumerator AutoStartAfterDelay()
    {
        if (autoStartDelay > 0f)
        {
            yield return new WaitForSeconds(autoStartDelay);
        }

        if (!_hasInteracted)
        {
            TriggerDialogue();
        }
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            SetPromptVisible(false);
            return;
        }

        if (!repeatable && _interactCount > 0)
        {
            SetPromptVisible(false);
            return;
        }

        // Wait for the first automatic dialogue to start.
        if (autoStartOnLoad && _interactCount == 0)
        {
            SetPromptVisible(false);
            return;
        }

        if (IsAnyDialogueOpen())
        {
            SetPromptVisible(false);
            return;
        }

        // Squared distance avoids calculating a square root every frame.
        float squaredDistance =
            (transform.position - _playerTransform.position).sqrMagnitude;

        bool inRange =
            squaredDistance <= triggerRadius * triggerRadius;

        SetPromptVisible(inRange && requireKeyPress);

        if (!inRange)
        {
            return;
        }

        bool interactionRequested =
            !requireKeyPress || Input.GetKeyDown(interactionKey);

        if (interactionRequested &&
            (_interactCount == 0 || repeatable))
        {
            TriggerDialogue();
        }
    }

    private bool IsAnyDialogueOpen()
    {
        SimpleDialogManager dialogManager =
            SimpleDialogManager.Instance;

        return dialogManager != null &&
               dialogManager.dialogPanel != null &&
               dialogManager.dialogPanel.activeSelf;
    }

    private void TriggerDialogue()
    {
        SetPromptVisible(false);

        SimpleDialogManager dialogManager =
            SimpleDialogManager.Instance;

        if (dialogManager == null)
        {
            Debug.LogError(
                $"[{name}] SimpleDialogManager instance was not found."
            );

            return;
        }

        string idToStart =
            _interactCount == 0
                ? sceneID
                : sceneID + "2";

        dialogManager.StartDialogue(idToStart);

        _hasInteracted = true;
        _interactCount++;
    }

    private void SetPromptVisible(bool visible)
    {
        if (interactionPrompt == null || _promptGroup == null)
        {
            return;
        }

        // Nothing changed, so do not restart the coroutine.
        if (_promptShouldBeVisible == visible)
        {
            return;
        }

        _promptShouldBeVisible = visible;

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        if (promptFadeDuration <= 0f)
        {
            _promptGroup.alpha = visible ? 1f : 0f;
            return;
        }

        _fadeRoutine = StartCoroutine(
            FadePromptRoutine(visible)
        );
    }

    private IEnumerator FadePromptRoutine(bool show)
    {
        float targetAlpha = show ? 1f : 0f;
        float startAlpha = _promptGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < promptFadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / promptFadeDuration
            );

            // Smooth fade instead of a completely linear fade.
            progress = Mathf.SmoothStep(0f, 1f, progress);

            _promptGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                progress
            );

            yield return null;
        }

        _promptGroup.alpha = targetAlpha;
        _fadeRoutine = null;
    }

    private void OnDisable()
    {
        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _promptShouldBeVisible = false;

        if (_promptGroup != null)
        {
            _promptGroup.alpha = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            transform.position,
            triggerRadius
        );
    }
}