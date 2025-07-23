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

    private bool _hasInteracted;  // Tracks if dialog has already been triggered
    public bool HasInteracted => _hasInteracted;  // External check

    private Transform _playerTransform;

    void Start()
    {
        // Cache reference to player
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            _playerTransform = playerGO.transform;
        else
            Debug.LogWarning($"[{name}] No GameObject tagged 'Player' found.");

        // Hide prompt initially
        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
        else
            Debug.LogError($"[{name}] interactionPrompt not assigned.");

        // Auto-start dialog on load if configured
        if (autoStartOnLoad && !_hasInteracted)
            StartCoroutine(AutoStartAfterDelay());
    }

    private IEnumerator AutoStartAfterDelay()
    {
        // wait the specified delay
        yield return new WaitForSeconds(autoStartDelay);

        // guard again (in case repeatable was toggled mid-delay)
        if (!_hasInteracted)
        {
            _hasInteracted = true;
            SimpleDialogManager.Instance.StartDialogue(sceneID);
        }
    }

    void Update()
    {
        // Skip if already interacted (non-repeatable), missing player, or auto-start mode
        if ((_hasInteracted && !repeatable) || _playerTransform == null || autoStartOnLoad)
        {
            HidePrompt();
            return;
        }

        // Check distance for interaction
        bool inRange = Vector3.Distance(transform.position, _playerTransform.position) <= triggerRadius;

        // Show/hide prompt based on range and key requirement
        if (inRange && requireKeyPress)
            ShowPrompt();
        else
            HidePrompt();

        // Trigger dialog when in range and input conditions met
        if (inRange && (!requireKeyPress || Input.GetKeyDown(interactionKey)))
        {
            if (!_hasInteracted || repeatable)
            {
                _hasInteracted = true;
                HidePrompt();
                SimpleDialogManager.Instance.StartDialogue(sceneID);
            }
        }
    }

    private void ShowPrompt()
    {
        interactionPrompt?.SetActive(true);
    }

    private void HidePrompt()
    {
        if (interactionPrompt != null)
            interactionPrompt?.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        // Visualize trigger radius in editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
