using System;
using System.Collections;
using UnityEngine;

public class DanceTutorial : MonoBehaviour
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

    private bool tutorialActive;
    private bool pauseMenuDisabled;

    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;

    private bool hasShownTutorial = false;

    public bool HasShownTutorial => hasShownTutorial;


    private void Awake()
    {
        if (tutorialCanvas == null)
        {
            Debug.LogError(
                "[DanceTutorial] Tutorial Canvas is not assigned."
            );

            enabled = false;
            return;
        }


        tutorialCanvasGroup =
            tutorialCanvas.GetComponent<CanvasGroup>();


        if (tutorialCanvasGroup == null)
        {
            tutorialCanvasGroup =
                tutorialCanvas.gameObject
                    .AddComponent<CanvasGroup>();
        }


        tutorialCanvasGroup.alpha = 0f;
        tutorialCanvas.enabled = false;


        // Automatically find player if needed.
        if (playerMovement == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerMovement =
                    player.GetComponent<PlayerMovement>();
            }
        }


        // Automatically find PauseMenu if needed.
        if (pauseMenu == null)
        {
            pauseMenu =
                FindFirstObjectByType<PauseMenu>(
                    FindObjectsInactive.Include
                );
        }
    }


    public void ShowTutorial(Action onFinished)
    {
        if (hasShownTutorial)
        {
            onFinished?.Invoke();
            return;
        }


        if (tutorialActive)
            return;


        hasShownTutorial = true;


        StartCoroutine(
            ShowTutorialRoutine(onFinished)
        );
    }


    private IEnumerator ShowTutorialRoutine(
        Action onFinished)
    {
        /*
         * Wait one frame so the dialogue system
         * finishes restoring its normal state first.
         */
        yield return null;


        // =====================================
        // DISABLE PLAYER MOVEMENT
        // =====================================

        if (playerMovement != null)
        {
            playerMovement.enabled = false;


            Rigidbody rb =
                playerMovement.GetComponent<Rigidbody>();


            if (rb != null)
            {
                rb.linearVelocity =
                    new Vector3(
                        0f,
                        rb.linearVelocity.y,
                        0f
                    );
            }
        }


        // Disable pause menu.
        SetPauseMenuEnabled(false);


        // Lock cursor.
        SetTutorialCursorLocked(true);


        // =====================================
        // SHOW TUTORIAL
        // =====================================

        tutorialCanvas.enabled = true;


        yield return FadeCanvas(
            0f,
            1f
        );


        // =====================================
        // WAIT FOR F
        // =====================================

        while (!Input.GetKeyDown(KeyCode.F))
        {
            yield return null;
        }


        /*
         * IMPORTANT:
         *
         * Start the dance RIGHT NOW.
         *
         * The minigame starts fading in at the
         * same time as this tutorial fades out.
         *
         * This removes the empty gap between them.
         */
        onFinished?.Invoke();


        // =====================================
        // FADE TUTORIAL OUT
        // =====================================

        yield return FadeCanvas(
            1f,
            0f
        );


        tutorialCanvas.enabled = false;


        /*
         * Wait until F is released so it cannot
         * accidentally trigger another interaction.
         */
        while (Input.GetKey(KeyCode.F))
        {
            yield return null;
        }


        // Restore pause menu.
        SetPauseMenuEnabled(true);


        // Restore normal cursor state.
        SetTutorialCursorLocked(false);


        /*
         * DO NOT re-enable PlayerMovement here.
         *
         * DanceMinigame is already active and
         * keeps movement disabled.
         */
    }


    private void SetPauseMenuEnabled(bool isEnabled)
    {
        if (pauseMenu == null)
            return;


        pauseMenu.enabled =
            isEnabled;


        pauseMenuDisabled =
            !isEnabled;
    }


    private void SetTutorialCursorLocked(
        bool isLocked)
    {
        if (isLocked)
        {
            if (!tutorialActive)
            {
                previousCursorVisible =
                    Cursor.visible;


                previousCursorLockState =
                    Cursor.lockState;
            }


            tutorialActive = true;


            EnforceCursorLock();


            return;
        }


        if (!tutorialActive)
            return;


        tutorialActive = false;


        Cursor.lockState =
            previousCursorLockState;


        Cursor.visible =
            previousCursorVisible;
    }


    private void LateUpdate()
    {
        if (tutorialActive)
        {
            EnforceCursorLock();
        }
    }


    private void EnforceCursorLock()
    {
        Cursor.lockState =
            CursorLockMode.Locked;


        Cursor.visible =
            false;
    }


    private IEnumerator FadeCanvas(
        float from,
        float to)
    {
        float duration =
            Mathf.Max(
                0f,
                fadeDuration
            );


        if (duration <= 0f)
        {
            tutorialCanvasGroup.alpha =
                to;


            yield break;
        }


        float timer = 0f;


        tutorialCanvasGroup.alpha =
            from;


        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float progress =
                Mathf.Clamp01(
                    timer /
                    duration
                );


            tutorialCanvasGroup.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    progress
                );


            yield return null;
        }


        tutorialCanvasGroup.alpha =
            to;
    }


    private void OnDisable()
    {
        if (pauseMenuDisabled)
        {
            SetPauseMenuEnabled(true);
        }


        if (tutorialActive)
        {
            SetTutorialCursorLocked(false);
        }
    }
}