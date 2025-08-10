using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class DialogActor : MonoBehaviour
{
    [Tooltip("Must match a top-level key in your JSON, e.g. \"HomeScene\", \"FriendsScene\"")]
    public string sceneID;  // Dialogue sequence ID

    [Tooltip("How close the Player has to be to trigger this NPC’s dialog (in world units).")]
    public float triggerRadius = 3f;

    [Tooltip("If true, this dialog fires automatically on Start (ignoring distance).")]
    public bool autoStartOnLoad = false;

    [Tooltip("Delay (in seconds) before auto-starting the dialog when autoStartOnLoad is true.")]
    public float autoStartDelay = 0f;

    [Tooltip("If true, RequireKeyPress must be pressed while in range to trigger.")]
    public bool requireKeyPress = true;

    [Tooltip("The Key to press when requireKeyPress is true.")]
    public KeyCode interactionKey = KeyCode.F;

    [Tooltip("Drag your 'Press F to interact' UI GameObject here (initially disabled)")]
    public GameObject interactionPrompt;

    [Tooltip("If true, dialog can be triggered multiple times.")]
    public bool repeatable = false;

    [Header("Prompt Fade")]
    [Tooltip("Seconds for the interaction prompt to fade in/out.")]
    public float promptFadeDuration = 0.25f;

    private bool _hasInteracted;  // Tracks if ANY dialog has been triggered at least once
    public bool HasInteracted => _hasInteracted;  // External check

    private int _interactCount = 0; // How many times we’ve started dialog
    private Transform _playerTransform;

    // Fade internals
    private CanvasGroup _promptGroup;
    private Coroutine _fadeRoutine;

    void Start()
    {
        // Cache reference to player
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            _playerTransform = playerGO.transform;
        else
            Debug.LogWarning($"[{name}] No GameObject tagged 'Player' found.");

        // Ensure prompt + CanvasGroup
        if (interactionPrompt != null)
        {
            _promptGroup = interactionPrompt.GetComponent<CanvasGroup>();
            if (_promptGroup == null)
                _promptGroup = interactionPrompt.AddComponent<CanvasGroup>();

            // Start hidden
            _promptGroup.alpha = 0f;
            interactionPrompt.SetActive(false);
        }
        else
        {
            Debug.LogError($"[{name}] interactionPrompt not assigned.");
        }

        // Auto-start dialog on load if configured
        if (autoStartOnLoad && !_hasInteracted)
            StartCoroutine(AutoStartAfterDelay());
    }

    private IEnumerator AutoStartAfterDelay()
    {
        yield return new WaitForSeconds(autoStartDelay);

        // guard again (in case repeatable was toggled mid-delay)
        if (!_hasInteracted)
        {
            TriggerDialogue();
        }
    }

    void Update()
    {
        // Skip if missing player, if not repeatable and already interacted,
        // or (IMPORTANT) if we haven't fired the auto-start yet.
        if (_playerTransform == null ||
            ((!repeatable) && _interactCount > 0) ||
            (autoStartOnLoad && _interactCount == 0))  // <-- only block BEFORE the first auto-start fires
        {
            HidePrompt();
            return;
        }

        // Check distance for interaction
        bool inRange = Vector3.Distance(transform.position, _playerTransform.position) <= triggerRadius;

        // Hide prompt while any dialogue is open
        if (IsAnyDialogueOpen())
        {
            HidePrompt();
            return;
        }

        // Show/hide prompt based on range and key requirement
        if (inRange && requireKeyPress)
            ShowPrompt();
        else
            HidePrompt();

        // Trigger dialog when in range and input conditions met
        // Allowed if first time OR repeatable is true
        if (inRange && (!requireKeyPress || Input.GetKeyDown(interactionKey)))
        {
            if (_interactCount == 0 || repeatable)
            {
                TriggerDialogue();
            }
        }
    }

    private bool IsAnyDialogueOpen()
    {
        var dm = SimpleDialogManager.Instance;
        return dm != null && dm.dialogPanel != null && dm.dialogPanel.activeSelf;
    }

    private void TriggerDialogue()
    {
        HidePrompt();

        // Choose which sceneID to play:
        //  - First interaction   => sceneID
        //  - Second or later     => sceneID + "2"
        string idToStart = (_interactCount == 0) ? sceneID : (sceneID + "2");

        SimpleDialogManager.Instance.StartDialogue(idToStart);

        // Mark and count
        _hasInteracted = true;
        _interactCount++;
    }

    // ---- Fade helpers ----
    private void ShowPrompt()
    {
        if (interactionPrompt == null || _promptGroup == null) return;
        StartFade(1f);
    }

    private void HidePrompt()
    {
        if (interactionPrompt == null || _promptGroup == null) return;
        StartFade(0f);
    }

    private void StartFade(float target)
    {
        // Early out if already at target
        if (_promptGroup != null && Mathf.Approximately(_promptGroup.alpha, target))
        {
            if (Mathf.Approximately(target, 0f) && interactionPrompt.activeSelf)
                interactionPrompt.SetActive(false);
            else if (!Mathf.Approximately(target, 0f) && !interactionPrompt.activeSelf)
                interactionPrompt.SetActive(true);
            return;
        }

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadePromptRoutine(target));
    }

    private IEnumerator FadePromptRoutine(float target)
    {
        if (!interactionPrompt.activeSelf && target > 0f)
            interactionPrompt.SetActive(true);

        float start = _promptGroup.alpha;
        float time = 0f;
        float dur = Mathf.Max(0.01f, promptFadeDuration);

        while (time < dur)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / dur);
            _promptGroup.alpha = Mathf.Lerp(start, target, t);
            yield return null;
        }

        _promptGroup.alpha = target;

        if (Mathf.Approximately(target, 0f))
            interactionPrompt.SetActive(false);

        _fadeRoutine = null;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize trigger radius in editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
