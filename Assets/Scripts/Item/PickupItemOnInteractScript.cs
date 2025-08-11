using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;        // for Graphic
using System.Collections;
using System.Linq;
using System;

public class PickupItemOnInteractScript : MonoBehaviour
{
    [SerializeField] Renderer[] renderers;
    [SerializeField] Interactable interactable;
    [SerializeField] Item itemScript;
    [SerializeField] string sceneIDToTriggerDialog; // (unchanged – still used if you want dialogue AFTER pickup)

    [Header("Animation + Fade")]
    [SerializeField] Animator playerAnimator;
    [SerializeField] string faintTriggerName = "Faint";
    [SerializeField] ScreenFader fadeScript;
    [SerializeField] float faintDelay = 0.3f;
    [SerializeField] float fadeDelay = 1f;

    [Header("Scene Transition")]
    [Tooltip("Scene to load after faint and fade (must be added to Build Settings)")]
    public string sceneToLoad;

    [Header("Optional On Pickup")]
    [Tooltip("GameObject to activate when the item is picked up")]
    public GameObject objectToShowOnPickup;

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

    // UI fade state (Canvas-based)
    private Graphic[] _uiGraphics;
    private Coroutine _uiFadeRoutine;
    private bool _isPlayerInRange = false;
    private bool _hasPickedUp = false;
    private float _currentUIAlpha = 0f;

    private void Awake()
    {
        if (interactable != null)
            interactable.eventOnInteract += OnInteract;
        else
            Debug.LogError($"[{name}] Interactable reference is missing.");

        // Prepare UI graphics for manual alpha fading
        if (interactionCanvas != null)
        {
            _uiGraphics = interactionCanvas.GetComponentsInChildren<Graphic>(true);
            SetUIAlpha(0f);
            interactionCanvas.gameObject.SetActive(false);
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
        if (_hasPickedUp) return;

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
    }

    private void OnInteract(Interactor interactor, Interactable interactableSender, InteractActionType interactType)
    {
        if (_hasPickedUp) return;
        if (interactType != InteractActionType.Interact) return;

        var owner = interactor != null ? interactor.GetOwner() : null;
        var inventoryScript = owner != null ? owner.GetComponent<InventoryScript>() : null;
        if (inventoryScript != null && itemScript != null)
            inventoryScript.AddItem(itemScript);

        DisableRenderers();
        _hasPickedUp = true;

        if (interactable != null)
            interactable.enabled = false;

        if (objectToShowOnPickup != null)
            objectToShowOnPickup.SetActive(true);

        if (hideInteractionUIOnPickup && interactionCanvas != null)
            StartUIFade(0f, interactionFadeDuration);

        // If you still want to run dialogue, keep this; otherwise leave scene flow only
        if (!string.IsNullOrEmpty(sceneIDToTriggerDialog) && SimpleDialogManager.Instance != null)
        {
            SimpleDialogManager.Instance.eventDialogueChanged += OnDialogueChanged;
            SimpleDialogManager.Instance.StartDialogue(sceneIDToTriggerDialog);
        }
        else
        {
            // No dialog configured – proceed straight to faint/fade
            StartCoroutine(DelayedFaintAndFade());
        }
    }

    private void OnDialogueChanged(string treeName, string nodeKey)
    {
        if (treeName == sceneIDToTriggerDialog && string.IsNullOrEmpty(nodeKey))
        {
            var dm = SimpleDialogManager.Instance;
            if (dm != null) dm.eventDialogueChanged -= OnDialogueChanged;

            StartCoroutine(DelayedFaintAndFade());
        }
    }

    private IEnumerator DelayedFaintAndFade()
    {
        if (faintDelay > 0f)
            yield return new WaitForSeconds(faintDelay);

        if (disablePlayerMovementOnFaint)
        {
            if (playerObject == null)
                playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                // Disable movement script by type name (robust lookup)
                if (!string.IsNullOrEmpty(movementScriptTypeName))
                {
                    var type = FindTypeInAssemblies(movementScriptTypeName);
                    if (type != null)
                    {
                        var movement = playerObject.GetComponent(type) as MonoBehaviour;
                        if (movement != null)
                        {
                            playerMovementScript = movement;
                            playerMovementScript.enabled = false;
                        }
                    }
                    else
                    {
                        // Fallback: try by simple name match among all MonoBehaviours
                        var behaviours = playerObject.GetComponents<MonoBehaviour>();
                        foreach (var b in behaviours)
                        {
                            if (b != null && b.GetType().Name == movementScriptTypeName)
                            {
                                playerMovementScript = b;
                                playerMovementScript.enabled = false;
                                break;
                            }
                        }
                    }
                }

                playerRigidbody = playerObject.GetComponent<Rigidbody>();
                if (playerRigidbody != null)
                    playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }
        }

        if (playerAnimator != null && !string.IsNullOrEmpty(faintTriggerName))
            playerAnimator.SetTrigger(faintTriggerName);

        if (fadeDelay > 0f)
            yield return new WaitForSeconds(fadeDelay);

        if (fadeScript != null)
            yield return StartCoroutine(fadeScript.FadeOut());

        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
    }

    private void DisableRenderers()
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r != null) r.enabled = false;
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

    private static Type FindTypeInAssemblies(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Fully qualified first
            var t = asm.GetType(typeName, false);
            if (t != null) return t;

            // Then simple name
            try
            {
                var tt = asm.GetTypes().FirstOrDefault(x => x.Name == typeName);
                if (tt != null) return tt;
            }
            catch { /* dynamic assemblies can throw */ }
        }
        return null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
}
