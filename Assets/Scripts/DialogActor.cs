using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class DialogActor : MonoBehaviour
{
    [Tooltip("Must match a top-level key in your JSON, e.g. \"HomeScene\", \"FriendsScene\"")]
    public string sceneID;

    [Tooltip("How close the Player has to be to trigger this NPC’s dialog (in world units).")]
    public float triggerRadius = 3f;

    [Tooltip("If true, this dialog fires immediately on Start (ignoring distance).")]
    public bool autoStartOnLoad = false;

    [Tooltip("Delay (in seconds) before auto-starting dialog on load.")]
    public float autoStartDelay = 1f;

    [Tooltip("If true, RequireKeyPress must be pressed while in range to trigger.")]
    public bool requireKeyPress = true;

    [Tooltip("The Key to press when requireKeyPress is true.")]
    public KeyCode interactionKey = KeyCode.F;

    [Tooltip("If true, the Player will face the NPC when dialog starts.")]
    public bool facePlayerOnInteract = true;

    [Tooltip("Speed at which the player rotates to face the NPC.")]
    public float rotationSpeed = 5f;

    [Header("Prompt UI (must have a CanvasGroup)")]
    [Tooltip("Drag your 'Press F to interact' UI GameObject here (initially disabled)")]
    public GameObject interactionPrompt;

    [Tooltip("How long (seconds) the prompt fades in/out")]    
    public float promptFadeDuration = 0.25f;

    [Tooltip("If true, dialog can be triggered multiple times.")]
    public bool repeatable = false;

    // Internal state
    private bool _hasInteracted = false;
    public bool HasInteracted => _hasInteracted;

    private Transform _playerTransform;
    private CanvasGroup _promptCanvasGroup;
    private Coroutine _fadeRoutine;
    private Coroutine _rotateRoutine;
    private Collider _npcCollider;

    void Start()
    {
        // Cache components
        _npcCollider = GetComponent<Collider>();
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

        // Auto-play override with delay
        if (autoStartOnLoad && !_hasInteracted)
        {
            _hasInteracted = true;
            StartCoroutine(AutoStartAfterDelay());
        }
    }

    void Update()
    {
        if (_hasInteracted || _playerTransform == null || autoStartOnLoad)
        {
            HidePrompt();
            return;
        }

        float dist = Vector3.Distance(GetNPCenter(), _playerTransform.position);
        bool inRange = dist <= triggerRadius;

        if (requireKeyPress && inRange)
            ShowPrompt();
        else
            HidePrompt();

        if (inRange)
        {
            if (requireKeyPress && Input.GetKeyDown(interactionKey))
                TriggerDialog();
            else if (!requireKeyPress)
                TriggerDialog();
        }
    }

    private IEnumerator AutoStartAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoStartDelay);
        if (facePlayerOnInteract)
            StartSmoothLook();
        SimpleDialogManager.Instance.StartDialogue(sceneID);
    }

    private void TriggerDialog()
    {
        if (!repeatable)
            _hasInteracted = true;

        HidePrompt();

        if (facePlayerOnInteract)
            StartSmoothLook();

        SimpleDialogManager.Instance.StartDialogue(sceneID);
    }

    private void StartSmoothLook()
    {
        if (_playerTransform == null) return;
        if (_rotateRoutine != null)
            StopCoroutine(_rotateRoutine);

        _rotateRoutine = StartCoroutine(SmoothLookAtCenter());
    }

    private IEnumerator SmoothLookAtCenter()
    {
        Vector3 npcCenter = GetNPCenter();
        Vector3 direction = npcCenter - _playerTransform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) yield break;

        Quaternion startRot = _playerTransform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(direction);

        float angle = Quaternion.Angle(startRot, targetRot);
        float duration = angle / rotationSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _playerTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }
        _playerTransform.rotation = targetRot;
    }

    private Vector3 GetNPCenter()
    {
        if (_npcCollider != null)
            return _npcCollider.bounds.center;
        return transform.position;
    }

    private void ShowPrompt()
    {
        if (interactionPrompt == null) return;
        interactionPrompt.SetActive(true);
        StartFade(_promptCanvasGroup.alpha, 1f);
    }

    private void HidePrompt()
    {
        if (interactionPrompt == null) return;
        StartFade(_promptCanvasGroup.alpha, 0f);
    }

    private void StartFade(float from, float to)
    {
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
        if (Mathf.Approximately(end, 0f))
            interactionPrompt.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
