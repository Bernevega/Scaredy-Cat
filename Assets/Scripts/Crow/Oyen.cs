using UnityEngine;
using UnityEngine.UI; // for Graphic
using System;
using System.Collections;
using System.Linq;

public class Oyen : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] ItemScriptable braceletItemScriptable;
    [SerializeField] GameObject nextSceneTransition;
    [SerializeField] Animator animator;

    [Header("Interaction UI (Proximity Prompt) - Uses Canvas")]
    [Tooltip("Canvas containing the interaction prompt UI (no CanvasGroup needed).")]
    [SerializeField] private Canvas interactionCanvas;
    [Tooltip("Radius around this object in which the interaction UI appears.")]
    [SerializeField] private float interactionRadius = 2.0f;
    [Tooltip("Fade duration for showing/hiding the interaction UI.")]
    [SerializeField] private float interactionFadeDuration = 0.25f;
    [Tooltip("Hide the interaction UI after completing the event.")]
    [SerializeField] private bool hideInteractionUIAfterEvent = false;

    Item[] itemReturnArray = new Item[1];

    public bool waitingForBracelet = false;
    bool hasBracelet = false;

    // UI fade state
    private GameObject playerObject;
    private Graphic[] _uiGraphics;
    private Coroutine _uiFadeRoutine;
    private bool _isPlayerInRange = false;
    private float _currentUIAlpha = 0f;

    private void Awake()
    {
        if (interactable != null)
            interactable.eventOnInteract += OnInteract;

        animator.SetBool("Sad", true);

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
        SimpleDialogManager.Instance.eventDialogueChanged += OnDialogueAdvance;
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.eventOnInteract -= OnInteract;

        if (SimpleDialogManager.Instance != null)
            SimpleDialogManager.Instance.eventDialogueChanged -= OnDialogueAdvance;
    }

    private void Update()
    {
        // Player range check for interaction UI
        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

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

        // >>> NEW: enforce hiding while dialogue is open; re-show when it closes (unless we've finished and want it hidden)
        var dm = SimpleDialogManager.Instance;
        bool dialogOpen = (dm != null && dm.dialogPanel != null && dm.dialogPanel.activeSelf);

        if (interactionCanvas != null)
        {
            if (dialogOpen)
            {
                if (_currentUIAlpha > 0f)
                    StartUIFade(0f, interactionFadeDuration);
            }
            else
            {
                // Only re-show if player is in range AND we aren't supposed to keep it hidden after completing the event
                bool shouldStayHidden = hideInteractionUIAfterEvent && hasBracelet;
                if (_isPlayerInRange && _currentUIAlpha <= 0f && !shouldStayHidden)
                    StartUIFade(1f, interactionFadeDuration);
            }
        }
    }

    private void OnInteract(Interactor interactor, Interactable interactable, InteractActionType type)
    {
        if (type == InteractActionType.Interact)
        {
            CheckBracelet(interactor);

            if (hideInteractionUIAfterEvent && interactionCanvas != null && hasBracelet)
                StartUIFade(0f, interactionFadeDuration);
        }
    }

    private void OnDialogueAdvance(string sceneId, string currKey)
    {
        var dm = SimpleDialogManager.Instance;

        if (!(sceneId == "OyenStart" || sceneId == "OyenWait" || sceneId == "OyenThank"))
            return;

        if (dm.dialogueStart)
        {
            animator.SetBool("Sad", false);
        }
        else if (currKey == null && !hasBracelet)
        {
            animator.SetBool("Sad", true);
        }

        if (sceneId == "OyenThank")
        {
            if (currKey == null)
            {
                nextSceneTransition.SetActive(true);
            }
            else if (currKey == "boo2")
            {
                animator.SetBool("Sad", false);
                animator.SetBool("Happy", true);
            }
        }
    }

    private void CheckBracelet(Interactor interactor)
    {
        // Block if dialog active
        SimpleDialogManager dm = SimpleDialogManager.Instance;
        if (dm != null && dm.dialogPanel != null && dm.dialogPanel.activeSelf)
            return;

        // >>> NEW: hide the prompt immediately when starting any dialogue from here
        if (interactionCanvas != null)
            StartUIFade(0f, interactionFadeDuration);

        if (!waitingForBracelet && !hasBracelet)
        {
            dm.StartDialogue("OyenStart");
            waitingForBracelet = true;
        }
        else if (waitingForBracelet && !hasBracelet)
        {
            InventoryScript invScript = interactor.GetOwner().GetComponent<InventoryScript>();
            int numItems = invScript.GetItem_WithScriptable(itemReturnArray, braceletItemScriptable);

            if (numItems > 0)
            {
                invScript.RemoveItem(itemReturnArray[0], -1);
                hasBracelet = true;

                dm.StartDialogue("OyenThank");
            }
            else
            {
                dm.StartDialogue("OyenWait");
            }
        }
        else if (waitingForBracelet && hasBracelet)
        {
            dm.StartDialogue("OyenThank");
        }
    }

    // ---------- Canvas-based UI Fade ----------
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

            var cr = g.canvasRenderer;
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
