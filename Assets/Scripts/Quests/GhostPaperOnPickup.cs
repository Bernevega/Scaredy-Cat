using UnityEngine;
using System.Collections;

public class GhostPaperOnPickup : MonoBehaviour
{
    [SerializeField] Interactable interactable;
    [SerializeField] GameObject ghostBlocker;
    [SerializeField] GameObject objectToDisable;

    [Header("Paper UI")]
    [SerializeField] GameObject paperUI;
    [SerializeField] float paperFadeDuration = 1f;

    private bool paperIsOpen = false;
    private bool canClosePaper = false;

    private CanvasGroup paperCanvasGroup;
    private Coroutine paperFadeRoutine;

    private void Awake()
    {
        interactable.eventOnInteract += OnInteract;

        if (paperUI)
        {
            paperCanvasGroup = paperUI.GetComponent<CanvasGroup>();

            if (paperCanvasGroup == null)
                paperCanvasGroup = paperUI.AddComponent<CanvasGroup>();

            paperCanvasGroup.alpha = 0f;
            paperUI.SetActive(false);
        }
    }

    private void Start()
    {
        if (SimpleDialogManager.Instance != null)
            SimpleDialogManager.Instance.eventDialogueChanged += OnDialogueChanged;
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
        if (paperIsOpen && canClosePaper && Input.GetKeyDown(KeyCode.F))
        {
            PutPaperDown();
        }
    }

    private void OnInteract(
        Interactor interactor,
        Interactable interactable,
        InteractActionType type)
    {
        if (type != InteractActionType.Interact)
            return;

        SimpleDialogManager.Instance.StartDialogue("GhostPaperPickUp");

        if (ghostBlocker)
            ghostBlocker.SetActive(false);

        if (objectToDisable)
            objectToDisable.SetActive(false);

        ShowPaper();
    }

    private void ShowPaper()
    {
        if (!paperUI)
            return;

        paperUI.SetActive(true);

        paperIsOpen = true;
        canClosePaper = false;

        StartPaperFade(1f, false);

        StartCoroutine(WaitForFRelease());
    }

    private void PutPaperDown()
    {
        if (!paperIsOpen)
            return;

        paperIsOpen = false;
        canClosePaper = false;

        StartPaperFade(0f, true);
    }

    private void StartPaperFade(float targetAlpha, bool disableAfter)
    {
        if (paperFadeRoutine != null)
            StopCoroutine(paperFadeRoutine);

        paperFadeRoutine =
            StartCoroutine(FadePaper(targetAlpha, disableAfter));
    }

    private IEnumerator FadePaper(float targetAlpha, bool disableAfter)
    {
        float startAlpha = paperCanvasGroup.alpha;
        float time = 0f;

        while (time < paperFadeDuration)
        {
            time += Time.unscaledDeltaTime;

            paperCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                time / paperFadeDuration
            );

            yield return null;
        }

        paperCanvasGroup.alpha = targetAlpha;

        if (disableAfter && paperUI)
            paperUI.SetActive(false);

        paperFadeRoutine = null;
    }

    private void OnDialogueChanged(string treeName, string nodeKey)
    {
        if (treeName != "GhostPaperPickUp")
            return;

        if (string.IsNullOrEmpty(nodeKey))
        {
            PutPaperDown();
        }
    }

    private IEnumerator WaitForFRelease()
    {
        while (Input.GetKey(KeyCode.F))
            yield return null;

        canClosePaper = true;
    }
}