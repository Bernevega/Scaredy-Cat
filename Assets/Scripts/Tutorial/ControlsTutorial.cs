using System.Collections;
using UnityEngine;

public class ControllsTutorial : MonoBehaviour
{
    [Header("Tutorial UI")]
    [SerializeField] private Canvas tutorialCanvas;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Pause Menu")]
    [SerializeField] private PauseMenu pauseMenu;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private CanvasGroup tutorialCanvasGroup;
    private bool movementLocked;
    private bool pauseMenuDisabled;

    private void Awake()
    {
        if (tutorialCanvas == null)
        {
            Debug.LogError("[ControllsTutorial] Tutorial Canvas is not assigned.");
            enabled = false;
            return;
        }

        tutorialCanvasGroup = tutorialCanvas.GetComponent<CanvasGroup>();

        if (tutorialCanvasGroup == null)
            tutorialCanvasGroup = tutorialCanvas.gameObject.AddComponent<CanvasGroup>();

        tutorialCanvasGroup.alpha = 0f;
        tutorialCanvas.enabled = false;

        if (playerMovement == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerMovement = player.GetComponent<PlayerMovement>();
        }

        if (pauseMenu == null)
        {
            pauseMenu = FindFirstObjectByType<PauseMenu>(
                FindObjectsInactive.Include
            );
        }
    }

    private void Start()
    {
        StartCoroutine(ShowAfterDialogue());
    }

    private IEnumerator ShowAfterDialogue()
    {
        SimpleDialogManager dialogManager = null;

        while (dialogManager == null || dialogManager.dialogPanel == null)
        {
            dialogManager = SimpleDialogManager.Instance;
            yield return null;
        }

        while (!dialogManager.dialogPanel.activeSelf)
            yield return null;

        while (dialogManager.dialogPanel.activeSelf)
            yield return null;

        yield return null;

        SetPlayerMovement(false);
        SetPauseMenuEnabled(false);
        tutorialCanvas.enabled = true;
        yield return FadeCanvas(0f, 1f);

        while (!Input.GetKeyDown(KeyCode.F))
            yield return null;

        yield return FadeCanvas(1f, 0f);
        tutorialCanvas.enabled = false;
        SetPlayerMovement(true);
        SetPauseMenuEnabled(true);
    }

    private void SetPlayerMovement(bool canMove)
    {
        if (playerMovement == null)
            return;

        playerMovement.SetCanMove(canMove);
        movementLocked = !canMove;
    }

    private void SetPauseMenuEnabled(bool isEnabled)
    {
        if (pauseMenu == null)
            return;

        pauseMenu.enabled = isEnabled;
        pauseMenuDisabled = !isEnabled;
    }

    private void OnDisable()
    {
        if (movementLocked)
            SetPlayerMovement(true);

        if (pauseMenuDisabled)
            SetPauseMenuEnabled(true);
    }

    private IEnumerator FadeCanvas(float from, float to)
    {
        float duration = Mathf.Max(0f, fadeDuration);

        if (duration > 0f)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                tutorialCanvasGroup.alpha = Mathf.Lerp(from, to, progress);
                yield return null;
            }
        }

        tutorialCanvasGroup.alpha = to;
    }
}