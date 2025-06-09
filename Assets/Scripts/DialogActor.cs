using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DialogActor : MonoBehaviour
{
    [Tooltip("Must match a top-level key in your JSON, e.g. \"HomeScene\", \"FriendsScene\"")]
    public string sceneID;

    [Tooltip("How close the Player has to be to trigger this NPC’s dialog (in world units).")]
    public float triggerRadius = 3f;

    [Tooltip("If true, this dialog fires immediately on Start (ignoring distance).")]
    public bool autoStartOnLoad = false;

    [Tooltip("If true, RequireKeyPress must be pressed while in range to trigger.")]
    public bool requireKeyPress = true;

    [Tooltip("The Key to press when requireKeyPress is true.")]
    public KeyCode interactionKey = KeyCode.F;

    [Header("Prompt UI (must have a CanvasGroup)")]
    [Tooltip("Drag your 'Press F to interact' UI GameObject here (initially disabled)")]
    public GameObject interactionPrompt;

    [Tooltip("How long (seconds) the prompt fades in/out")]
    public float promptFadeDuration = 0.25f;

    // Internal state
    private bool _hasInteracted = false;
    public bool HasInteracted => _hasInteracted;

    private Transform _playerTransform;
    private CanvasGroup _promptCanvasGroup;
    private Coroutine _fadeRoutine;

    void Start()
    {
        // Cache the Player transform
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            _playerTransform = playerGO.transform;
        else
            Debug.LogWarning($"[{name}] Cannot find GameObject tagged 'Player'.");

        // Setup CanvasGroup on prompt
        if (interactionPrompt != null)
        {
            _promptCanvasGroup = interactionPrompt.GetComponent<CanvasGroup>();
            if (_promptCanvasGroup == null)
                _promptCanvasGroup = interactionPrompt.AddComponent<CanvasGroup>();

            _promptCanvasGroup.alpha = 0f;
            interactionPrompt.SetActive(false);
        }
        else
        {
            Debug.LogError($"[{name}] interactionPrompt not assigned.");
        }

        // Auto-play override
        if (autoStartOnLoad && !_hasInteracted)
        {
            _hasInteracted = true;
            SimpleDialogManager.Instance.StartDialogue(sceneID);
        }
    }

    void Update()
    {
        // If we've already run this or there's no player, bail
        if (_hasInteracted || _playerTransform == null || autoStartOnLoad)
        {
            HidePrompt();
            return;
        }

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        bool inRange = dist <= triggerRadius;

        // Show or hide prompt based on state
        if (requireKeyPress && inRange)
            ShowPrompt();
        else
            HidePrompt();

        // Trigger logic
        if (inRange)
        {
            if (requireKeyPress && Input.GetKeyDown(interactionKey))
                TriggerDialog();
            else if (!requireKeyPress)
                TriggerDialog();
        }
    }

    private void TriggerDialog()
    {
        _hasInteracted = true;
        HidePrompt();
        SimpleDialogManager.Instance.StartDialogue(sceneID);
    }

    private void ShowPrompt()
    {
        if (interactionPrompt == null) return;

        interactionPrompt.SetActive(true);
        StartFade( _promptCanvasGroup.alpha, 1f );
    }

    private void HidePrompt()
    {
        if (interactionPrompt == null) return;

        StartFade( _promptCanvasGroup.alpha, 0f );
    }

    private void StartFade(float from, float to)
    {
        // stop any ongoing fade
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeCanvasGroup(from, to));
    }

    private IEnumerator FadeCanvasGroup(float start, float end)
    {
        float elapsed = 0f;
        while (elapsed < promptFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / promptFadeDuration);
            _promptCanvasGroup.alpha = Mathf.Lerp(start, end, t);
            yield return null;
        }
        _promptCanvasGroup.alpha = end;

        // if we've faded out fully, deactivate
        if (Mathf.Approximately(end, 0f))
            interactionPrompt.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
