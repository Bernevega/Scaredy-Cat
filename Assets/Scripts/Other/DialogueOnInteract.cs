using UnityEngine;
using UnityEngine.UI; // for Graphic
using System.Collections;

public class DialogueOnInteract : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] Interactable interactable;
    public SharedString sceneId;
    [SerializeField] RotateToPlayerOnDialogue rotateToPlayer;

    [Header("Interaction UI (Proximity Prompt) - Uses Canvas")]
    [Tooltip("Canvas containing the interaction prompt UI (no CanvasGroup needed).")]
    [SerializeField] private Canvas interactionCanvas;
    [Tooltip("Radius around this object in which the interaction UI appears.")]
    [SerializeField] private float interactionRadius = 2.0f;
    [Tooltip("Fade duration for showing/hiding the interaction UI.")]
    [SerializeField] private float interactionFadeDuration = 0.25f;
    [Tooltip("Hide the interaction UI while dialogue is open.")]
    [SerializeField] private bool hideWhileDialogueOpen = true;

    // internals
    private GameObject playerObject;
    private Graphic[] _uiGraphics;
    private Coroutine _uiFadeRoutine;
    private bool _isPlayerInRange = false;
    private float _currentUIAlpha = 0f;

    private void Awake()
    {
        if (interactable != null)
            interactable.eventOnInteract += OnInteract;

        if (rotateToPlayer != null)
            rotateToPlayer.sceneId = sceneId;

        // Prepare UI graphics for manual alpha fading
        if (interactionCanvas != null)
        {
            _uiGraphics = interactionCanvas.GetComponentsInChildren<Graphic>(true);
            SetUIAlpha(0f);
            interactionCanvas.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        var dm = SimpleDialogManager.Instance;
        if (dm != null)
            dm.eventDialogueChanged += OnDialogueChanged;
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.eventOnInteract -= OnInteract;

        var dm = SimpleDialogManager.Instance;
        if (dm != null)
            dm.eventDialogueChanged -= OnDialogueChanged;
    }

    private void Update()
    {
        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        // Show/hide prompt based on distance unless dialogue is open (handled below)
        if (interactionCanvas != null && playerObject != null)
        {
            float sqrDist = (playerObject.transform.position - transform.position).sqrMagnitude;
            bool inRangeNow = sqrDist <= interactionRadius * interactionRadius;

            if (inRangeNow != _isPlayerInRange)
            {
                _isPlayerInRange = inRangeNow;
                if (_isPlayerInRange) StartUIFade(1f, interactionFadeDuration);
                else StartUIFade(0f, interactionFadeDuration);
            }
        }

        // If dialogue is open and we want to hide the prompt, enforce it.
        if (hideWhileDialogueOpen && interactionCanvas != null)
        {
            var dm = SimpleDialogManager.Instance;
            bool isOpen = (dm != null && dm.dialogPanel != null && dm.dialogPanel.activeSelf);
            if (isOpen)
            {
                // Make sure it’s hidden regardless of range.
                if (_currentUIAlpha > 0f)
                    StartUIFade(0f, interactionFadeDuration);
            }
            else
            {
                // Dialogue closed: if player is still in range, fade back in.
                if (_isPlayerInRange && _currentUIAlpha <= 0f)
                    StartUIFade(1f, interactionFadeDuration);
            }
        }
    }

    private void OnInteract(Interactor interactor, Interactable interactableSender, InteractActionType actionType)
    {
        // Block if dialog active
        var dm = SimpleDialogManager.Instance;
        if (dm != null && dm.dialogPanel != null && dm.dialogPanel.activeSelf)
            return;

        if (actionType == InteractActionType.Interact)
        {
            if (dm != null && sceneId != null)
            {
                dm.StartDialogue(sceneId.value);

                // Hide prompt immediately if configured
                if (hideWhileDialogueOpen && interactionCanvas != null)
                    StartUIFade(0f, interactionFadeDuration);
            }
        }
    }

    private void OnDialogueChanged(string treeName, string nodeKey)
    {
        // When our dialogue starts, ensure prompt hidden; when it ends (nodeKey == null), restore if in range
        if (sceneId == null || string.IsNullOrEmpty(sceneId.value)) return;
        if (treeName != sceneId.value) return;

        bool dialogueEnded = string.IsNullOrEmpty(nodeKey);
        if (interactionCanvas == null) return;

        if (!dialogueEnded)
        {
            // Any node inside our dialogue => hide prompt
            if (hideWhileDialogueOpen)
                StartUIFade(0f, interactionFadeDuration);
        }
        else
        {
            // Dialogue ended => show again if still nearby
            if (_isPlayerInRange && hideWhileDialogueOpen)
                StartUIFade(1f, interactionFadeDuration);
        }
    }

    // -------- Canvas-based UI Fade (no CanvasGroup) --------
    private void StartUIFade(float targetAlpha, float duration)
    {
        if (interactionCanvas == null) return;

        if (_uiFadeRoutine != null)
            StopCoroutine(_uiFadeRoutine);

        if (targetAlpha > 0f && !interactionCanvas.gameObject.activeSelf)
            interactionCanvas.gameObject.SetActive(true);

        _uiFadeRoutine = StartCoroutine(FadeUIGraphics(targetAlpha, duration));
    }

    private IEnumerator FadeUIGraphics(float targetAlpha, float duration)
    {
        if (_uiGraphics == null)
            _uiGraphics = interactionCanvas.GetComponentsInChildren<Graphic>(true);

        float startAlpha = _currentUIAlpha;
        float t = 0f;
        float dur = Mathf.Max(0.01f, duration);

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / dur));
            SetUIAlpha(a);
            yield return null;
        }

        SetUIAlpha(targetAlpha);

        if (Mathf.Approximately(targetAlpha, 0f))
            interactionCanvas.gameObject.SetActive(false);

        _uiFadeRoutine = null;
    }

    private void SetUIAlpha(float a)
    {
        _currentUIAlpha = a;
        if (_uiGraphics == null) return;

        for (int i = 0; i < _uiGraphics.Length; i++)
        {
            var g = _uiGraphics[i];
            if (g == null) continue;

            var c = g.color;
            c.a = a;
            g.color = c;

            var cr = g.canvasRenderer; // optional safeguard
            if (cr != null) cr.SetAlpha(a);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
}
