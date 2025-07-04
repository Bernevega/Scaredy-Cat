using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Crow : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("How close the Player has to be to show the prompt and interact")]
    public float triggerRadius = 3f;

    [Tooltip("If true, must press the key to interact")]
    public bool requireKeyPress = true;

    [Tooltip("Key to press when requireKeyPress is true")]
    public KeyCode interactionKey = KeyCode.F;

    [Header("Prompt UI")]
    [Tooltip("Drag your 'Press F to interact' UI GameObject here (initially disabled)")]
    public GameObject interactionPrompt;

    [Tooltip("How long (seconds) the prompt fades in/out")]
    public float promptFadeDuration = 0.25f;

    [Space(15)]
    [SerializeField] private Interactable interactable;
    [SerializeField] private ItemScriptable monocleLeftScriptable;
    [SerializeField] private ItemScriptable monocleRightScriptable;
    [SerializeField] private GameObject monocleLeftObject;
    [SerializeField] private GameObject monocleRightObject;
    [SerializeField] private GameObject flowerObject;
    [SerializeField] private GameObject bushObject;

    // Internal state
    private Transform _playerTransform;
    private bool _hasInteracted = false;
    private bool _canShowPrompt = true;

    // Graphics under the prompt, for fading
    private Graphic[] _promptGraphics;
    private Coroutine _fadeRoutine;

    private Item[] itemReturnArray = new Item[1];
    private DialogueState dialogueState = DialogueState.FirstTalk;
    private bool hasThanked = false;
    private bool hasLeftMonocle = false;
    private bool hasRightMonocle = false;
    public static bool hasBothMonocles { get; private set; } = false;

    private enum DialogueState
    {
        FirstTalk,
        FirstMonocleWait,
        SecondMonocle,
        SecondMonocleWait,
        Final,
        FinalFinal
    }

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        // Cache player
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            _playerTransform = playerGO.transform;
        else
            Debug.LogWarning("[Crow] Cannot find GameObject tagged 'Player'.");

        // Setup prompt graphics for fading
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
            _promptGraphics = interactionPrompt.GetComponentsInChildren<Graphic>(true);
            foreach (var g in _promptGraphics)
            {
                var c = g.color;
                c.a = 0f;
                g.color = c;
            }
        }
        else
        {
            Debug.LogError("[Crow] interactionPrompt not assigned.");
        }
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            HidePrompt();
            return;
        }

        // Block prompt/interaction during active dialog
        var dm = SimpleDialogManager.Instance;
        if (dm != null && dm.dialogPanel != null && dm.dialogPanel.activeSelf)
        {
            HidePrompt();
            return;
        }

        float dist = Vector3.Distance(transform.position, _playerTransform.position);
        bool inRange = dist <= triggerRadius;

        if (!inRange)
        {
            _canShowPrompt = true;
            _hasInteracted = false;
            HidePrompt();
            return;
        }

        if (requireKeyPress && inRange && _canShowPrompt)
            ShowPrompt();
        else
            HidePrompt();

        if (inRange && requireKeyPress && Input.GetKeyDown(interactionKey))
        {
            _canShowPrompt = false;
            _hasInteracted = true;
            HidePrompt();

            var interactorComp = _playerTransform.GetComponent<Interactor>();
            OnInteract(interactorComp, interactable, InteractActionType.Interact);
        }
    }

    private void ShowPrompt()
    {
        if (interactionPrompt == null) return;
        interactionPrompt.SetActive(true);
        float start = _promptGraphics.Length > 0 ? _promptGraphics[0].color.a : 0f;
        StartFadeGraphics(start, 1f);
    }

    private void HidePrompt()
    {
        if (interactionPrompt == null) return;
        float start = _promptGraphics.Length > 0 ? _promptGraphics[0].color.a : 1f;
        StartFadeGraphics(start, 0f, () => interactionPrompt.SetActive(false));
    }

    private void StartFadeGraphics(float from, float to, System.Action onComplete = null)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeGraphics(from, to, onComplete));
    }

    private IEnumerator FadeGraphics(float start, float end, System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < promptFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / promptFadeDuration);
            float alpha = Mathf.Lerp(start, end, t);
            foreach (var g in _promptGraphics)
            {
                var c = g.color;
                c.a = alpha;
                g.color = c;
            }
            yield return null;
        }
        foreach (var g in _promptGraphics)
        {
            var c = g.color;
            c.a = end;
            g.color = c;
        }
        onComplete?.Invoke();
    }

    private void OnInteract(Interactor interactor, Interactable _, InteractActionType type)
    {
        // Block if dialog active
        var dm = SimpleDialogManager.Instance;
        if (dm != null && dm.dialogPanel != null && dm.dialogPanel.activeSelf)
            return;

        if (type != InteractActionType.Interact) return;

        if (dialogueState == DialogueState.FirstTalk)
        {
            dialogueState = DialogueState.FirstMonocleWait;
            SimpleDialogManager.Instance.StartDialogue("CrowFirstTalk");
            // Disable the bust (bush) once the first dialogue starts
            if (bushObject != null)
                bushObject.SetActive(false);
        }
        else
        {
            CheckMonocle(interactor);
        }
    }

    private void CheckMonocle(Interactor interactor)
    {
        var inventory = interactor.GetOwner().GetComponent<InventoryScript>();
        if (inventory == null) return;

        if (dialogueState == DialogueState.FirstMonocleWait && !hasLeftMonocle)
        {
            int num = inventory.GetItem_WithScriptable(itemReturnArray, monocleLeftScriptable);
            if (num > 0)
            {
                inventory.RemoveItem(itemReturnArray[0], -1);
                Destroy(itemReturnArray[0].gameObject);
                monocleLeftObject.SetActive(true);
                bushObject.SetActive(false);
                hasLeftMonocle = true;
                dialogueState = DialogueState.SecondMonocleWait;
                SimpleDialogManager.Instance.StartDialogue("CrowSecondMonocle");
            }
            else
            {
                SimpleDialogManager.Instance.StartDialogue("CrowFirstMonocleWait");
            }
        }
        else if (dialogueState == DialogueState.SecondMonocle && !hasRightMonocle)
        {
            dialogueState = DialogueState.SecondMonocleWait;
            SimpleDialogManager.Instance.StartDialogue("CrowSecondMonocle");
        }
        else if (dialogueState == DialogueState.SecondMonocleWait && !hasRightMonocle)
        {
            int num = inventory.GetItem_WithScriptable(itemReturnArray, monocleRightScriptable);
            if (num > 0)
            {
                inventory.RemoveItem(itemReturnArray[0], -1);
                Destroy(itemReturnArray[0].gameObject);
                monocleRightObject.SetActive(true);
                flowerObject.SetActive(true);
                hasRightMonocle = true;
                hasBothMonocles = true;
                dialogueState = DialogueState.Final;
                SimpleDialogManager.Instance.StartDialogue("CrowFinal");
            }
            else
            {
                SimpleDialogManager.Instance.StartDialogue("CrowSecondMonocleWait");
            }
        }
        else if (dialogueState == DialogueState.Final && !hasThanked)
        {
            dialogueState = DialogueState.FinalFinal;
            SimpleDialogManager.Instance.StartDialogue("CrowFinal");
        }
        else if (dialogueState == DialogueState.FinalFinal && !hasThanked)
        {
            hasThanked = true;
            SimpleDialogManager.Instance.StartDialogue("CrowThank");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
