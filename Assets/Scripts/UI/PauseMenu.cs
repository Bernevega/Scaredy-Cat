using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject PauseMenuCanvas;
    public GameObject SettingsPanel;
    public GameObject PausePanel;
    public GameObject VideoSettingsPanel;
    public GameObject AudioSettingsPanel;
    public GameObject ControlsSettingsPanel;

    [Header("Transition")]
    public float fadeDuration = 0.5f;

    [Header("Screen Fade")]
    [Tooltip("Full-screen black UI Image used when returning to the main menu or exiting.")]
    public Image screenFadeImage;

    [Tooltip("How long the screen takes to fade to black.")]
    public float screenFadeDuration = 0.75f;

    [Header("Audio UI")]
    [Tooltip("Slider range: 0..1")]
    public Slider generalVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Video UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;

    public static bool isPaused { get; private set; } = false;

    private CanvasGroup pauseCanvasGroup;
    private CanvasGroup pausePanelCanvasGroup;
    private CanvasGroup settingsPanelCanvasGroup;

    private bool isTransitioning;
    private bool isScreenFading;

    // --- Video settings state ---
    private readonly List<Vector2Int> _resOptions =
        new List<Vector2Int>();

    private int _selectedWidth;
    private int _selectedHeight;

    // PlayerPrefs keys
    private const string PP_WIDTH = "Video_Width";
    private const string PP_HEIGHT = "Video_Height";
    private const string PP_MODE = "Video_Mode";

    private void Start()
    {
        // -------------------------------------------------
        // MAIN PAUSE CANVAS
        // -------------------------------------------------

        pauseCanvasGroup =
            PauseMenuCanvas.GetComponent<CanvasGroup>();

        if (pauseCanvasGroup == null)
        {
            pauseCanvasGroup =
                PauseMenuCanvas.AddComponent<CanvasGroup>();
        }

        // -------------------------------------------------
        // PAUSE PANEL
        // -------------------------------------------------

        pausePanelCanvasGroup =
            PausePanel.GetComponent<CanvasGroup>();

        if (pausePanelCanvasGroup == null)
        {
            pausePanelCanvasGroup =
                PausePanel.AddComponent<CanvasGroup>();
        }

        // -------------------------------------------------
        // SETTINGS PANEL
        // -------------------------------------------------

        settingsPanelCanvasGroup =
            SettingsPanel.GetComponent<CanvasGroup>();

        if (settingsPanelCanvasGroup == null)
        {
            settingsPanelCanvasGroup =
                SettingsPanel.AddComponent<CanvasGroup>();
        }

        // -------------------------------------------------
        // INITIAL STATE
        // -------------------------------------------------

        pauseCanvasGroup.alpha = 0f;

        pausePanelCanvasGroup.alpha = 1f;
        pausePanelCanvasGroup.interactable = true;
        pausePanelCanvasGroup.blocksRaycasts = true;

        settingsPanelCanvasGroup.alpha = 0f;
        settingsPanelCanvasGroup.interactable = false;
        settingsPanelCanvasGroup.blocksRaycasts = false;

        PauseMenuCanvas.SetActive(false);

        PausePanel.SetActive(true);
        SettingsPanel.SetActive(false);

        // -------------------------------------------------
        // SCREEN FADER
        // -------------------------------------------------

        if (screenFadeImage != null)
        {
            Color fadeColor = screenFadeImage.color;

            fadeColor.a = 0f;

            screenFadeImage.color = fadeColor;
            screenFadeImage.raycastTarget = false;

            screenFadeImage.gameObject.SetActive(true);
        }

        // Populate video settings UI
        VerifyAndPopulateDropdowns();

        if (resolutionDropdown)
        {
            resolutionDropdown.onValueChanged.AddListener(
                SetResolution
            );
        }

        if (screenModeDropdown)
        {
            screenModeDropdown.onValueChanged.AddListener(
                SetScreenMode
            );
        }

        LoadVolumeSliders();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        isPaused = false;
    }

    private void Update()
    {
        if (isScreenFading)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (isTransitioning)
            return;

        if (!isPaused)
        {
            PauseGame();
        }
        else
        {
            ContinueGame();
        }
    }

    // =====================================================
    // PAUSE / CONTINUE
    // =====================================================

    public void PauseGame()
    {
        if (isPaused || isTransitioning)
            return;

        SettingsPanel.SetActive(false);

        settingsPanelCanvasGroup.alpha = 0f;
        settingsPanelCanvasGroup.interactable = false;
        settingsPanelCanvasGroup.blocksRaycasts = false;

        PausePanel.SetActive(true);

        pausePanelCanvasGroup.alpha = 1f;
        pausePanelCanvasGroup.interactable = true;
        pausePanelCanvasGroup.blocksRaycasts = true;

        PauseMenuCanvas.SetActive(true);

        pauseCanvasGroup.alpha = 0f;

        isPaused = true;

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        StartCoroutine(
            FadePauseCanvas(
                1f,
                false
            )
        );

        Debug.Log("Game paused");
    }

    public void ContinueGame()
    {
        if (!isPaused || isTransitioning)
            return;

        StartCoroutine(
            FadePauseCanvas(
                0f,
                true
            )
        );
    }

    private IEnumerator FadePauseCanvas(
        float targetAlpha,
        bool resumeAfterFade
    )
    {
        isTransitioning = true;

        float startAlpha =
            pauseCanvasGroup.alpha;

        float duration =
            Mathf.Max(
                0f,
                fadeDuration
            );

        if (duration > 0f)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime +=
                    Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsedTime /
                        duration
                    );

                pauseCanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        targetAlpha,
                        progress
                    );

                yield return null;
            }
        }

        pauseCanvasGroup.alpha =
            targetAlpha;

        if (resumeAfterFade)
        {
            PauseMenuCanvas.SetActive(false);

            isPaused = false;

            Time.timeScale = 1f;

            Cursor.visible = false;
            Cursor.lockState =
                CursorLockMode.Locked;

            Debug.Log("Game unpaused");
        }

        isTransitioning = false;
    }

    // =====================================================
    // SETTINGS NAVIGATION
    // =====================================================

    public void OpenVideoSettings()
    {
        // If Settings is already open,
        // this is only switching back to Video.
        if (SettingsPanel.activeSelf)
        {
            VideoSettingsPanel.SetActive(true);
            AudioSettingsPanel.SetActive(false);
            ControlsSettingsPanel.SetActive(false);

            return;
        }

        if (isTransitioning)
            return;

        VideoSettingsPanel.SetActive(true);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(false);

        VerifyAndPopulateDropdowns();

        StartCoroutine(
            FadePauseToSettings()
        );
    }

    private IEnumerator FadePauseToSettings()
    {
        isTransitioning = true;

        pausePanelCanvasGroup.interactable = false;
        pausePanelCanvasGroup.blocksRaycasts = false;

        float duration =
            Mathf.Max(
                0f,
                fadeDuration
            );

        // -------------------------------------------------
        // FADE PAUSE PANEL OUT
        // -------------------------------------------------

        if (duration > 0f)
        {
            float elapsed = 0f;

            float startAlpha =
                pausePanelCanvasGroup.alpha;

            while (elapsed < duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );

                pausePanelCanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        t
                    );

                yield return null;
            }
        }

        pausePanelCanvasGroup.alpha = 0f;

        PausePanel.SetActive(false);

        // -------------------------------------------------
        // SETTINGS
        // -------------------------------------------------

        SettingsPanel.SetActive(true);

        settingsPanelCanvasGroup.alpha = 0f;
        settingsPanelCanvasGroup.interactable = false;
        settingsPanelCanvasGroup.blocksRaycasts = false;

        // -------------------------------------------------
        // FADE SETTINGS IN
        // -------------------------------------------------

        if (duration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );

                settingsPanelCanvasGroup.alpha =
                    Mathf.Lerp(
                        0f,
                        1f,
                        t
                    );

                yield return null;
            }
        }

        settingsPanelCanvasGroup.alpha = 1f;

        settingsPanelCanvasGroup.interactable = true;
        settingsPanelCanvasGroup.blocksRaycasts = true;

        isTransitioning = false;
    }

    public void OpenAudioSettings()
    {
        if (isTransitioning)
            return;

        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(true);
        ControlsSettingsPanel.SetActive(false);
    }

    public void OpenControlsSettings()
    {
        if (isTransitioning)
            return;

        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (isTransitioning)
            return;

        StartCoroutine(
            FadeSettingsToPause()
        );
    }

    private IEnumerator FadeSettingsToPause()
    {
        isTransitioning = true;

        settingsPanelCanvasGroup.interactable = false;
        settingsPanelCanvasGroup.blocksRaycasts = false;

        float duration =
            Mathf.Max(
                0f,
                fadeDuration
            );

        // -------------------------------------------------
        // FADE SETTINGS OUT
        // -------------------------------------------------

        if (duration > 0f)
        {
            float elapsed = 0f;

            float startAlpha =
                settingsPanelCanvasGroup.alpha;

            while (elapsed < duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );

                settingsPanelCanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        t
                    );

                yield return null;
            }
        }

        settingsPanelCanvasGroup.alpha = 0f;

        SettingsPanel.SetActive(false);

        // -------------------------------------------------
        // PAUSE PANEL
        // -------------------------------------------------

        PausePanel.SetActive(true);

        pausePanelCanvasGroup.alpha = 0f;
        pausePanelCanvasGroup.interactable = false;
        pausePanelCanvasGroup.blocksRaycasts = false;

        // -------------------------------------------------
        // FADE PAUSE PANEL IN
        // -------------------------------------------------

        if (duration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );

                pausePanelCanvasGroup.alpha =
                    Mathf.Lerp(
                        0f,
                        1f,
                        t
                    );

                yield return null;
            }
        }

        pausePanelCanvasGroup.alpha = 1f;

        pausePanelCanvasGroup.interactable = true;
        pausePanelCanvasGroup.blocksRaycasts = true;

        isTransitioning = false;
    }

    // =====================================================
    // SAVE
    // =====================================================

    public void SaveGame()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError(
                "PauseMenu: Cannot save because there is no SaveManager in the scene!"
            );

            return;
        }

        SaveManager.Instance.ManualSave();

        Debug.Log(
            "PauseMenu: Game saved successfully!"
        );
    }

    // =====================================================
    // MAIN MENU
    // =====================================================

    public void BackToMenu()
    {
        if (isScreenFading)
            return;

        StartCoroutine(
            FadeToBlackAndReturnToMenu()
        );
    }

    private IEnumerator FadeToBlackAndReturnToMenu()
    {
        yield return StartCoroutine(
            FadeScreenToBlack()
        );

        Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        PlayerManager pm =
            PlayerManager.instance;

        SimpleDialogManager dm =
            SimpleDialogManager.Instance;

        if (pm)
        {
            Destroy(
                pm.player.gameObject
            );

            Destroy(
                pm.gameObject
            );
        }

        if (dm)
        {
            Destroy(
                dm.gameObject
            );
        }

        SceneManager.LoadScene(
            "MainMenu"
        );
    }

    // =====================================================
    // EXIT
    // =====================================================

    public void ExitGame()
    {
        if (isScreenFading)
            return;

        StartCoroutine(
            FadeToBlackAndExit()
        );
    }

    private IEnumerator FadeToBlackAndExit()
    {
        yield return StartCoroutine(
            FadeScreenToBlack()
        );

        Time.timeScale = 1f;

        Application.Quit();

#if UNITY_EDITOR
        Debug.Log(
            "Exit Game pressed. Application.Quit() does not close Play Mode in the Unity Editor."
        );
#endif
    }

    // =====================================================
    // SCREEN FADE
    // =====================================================

    private IEnumerator FadeScreenToBlack()
    {
        isScreenFading = true;
        isTransitioning = true;

        if (screenFadeImage == null)
        {
            Debug.LogWarning(
                "PauseMenu: No Screen Fade Image assigned."
            );

            yield break;
        }

        screenFadeImage.gameObject.SetActive(true);

        // Make sure black screen is drawn above everything else.
        screenFadeImage.transform.SetAsLastSibling();

        screenFadeImage.raycastTarget = true;

        Color color =
            screenFadeImage.color;

        float startAlpha =
            color.a;

        float duration =
            Mathf.Max(
                0f,
                screenFadeDuration
            );

        if (duration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration
                    );

                color.a =
                    Mathf.Lerp(
                        startAlpha,
                        1f,
                        t
                    );

                screenFadeImage.color =
                    color;

                yield return null;
            }
        }

        color.a = 1f;

        screenFadeImage.color =
            color;
    }

    // =====================================================
    // AUDIO
    // =====================================================

    public void SetGeneralVolume(
        Slider slider
    )
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance
                .SetMasterVolume(slider);
        }
    }

    public void SetMusicVolume(
        Slider slider
    )
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance
                .SetMusicVolume(slider);
        }
    }

    public void SetSFXVolume(
        Slider slider
    )
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance
                .SetSFXVolume(slider);
        }
    }

    private void LoadVolumeSliders()
    {
        float master =
            PlayerPrefs.GetFloat(
                "MasterVolume",
                0.75f
            );

        float music =
            PlayerPrefs.GetFloat(
                "MusicVolume",
                0.75f
            );

        float sfx =
            PlayerPrefs.GetFloat(
                "SFXVolume",
                0.75f
            );

        if (generalVolumeSlider != null)
        {
            generalVolumeSlider.value =
                master;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance
                    .SetMasterVolume(master);
            }
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value =
                music;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance
                    .SetMusicVolume(music);
            }
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value =
                sfx;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance
                    .SetSFXVolume(sfx);
            }
        }
    }

    // =====================================================
    // VIDEO SETTINGS
    // =====================================================

    private void VerifyAndPopulateDropdowns()
    {
        if (!resolutionDropdown ||
            !screenModeDropdown)
        {
            return;
        }

        if (resolutionDropdown.options.Count == 0 ||
            resolutionDropdown.options[0]
                .text
                .StartsWith("Option"))
        {
            SetupResolutionOptions();
        }

        if (screenModeDropdown.options.Count == 0 ||
            screenModeDropdown.options[0]
                .text
                .StartsWith("Option"))
        {
            SetupScreenModeOptions();
        }

        LoadAndApplyVideoSettings();
    }

    private void SetupResolutionOptions()
    {
        if (!resolutionDropdown)
            return;

        resolutionDropdown.ClearOptions();

        _resOptions.Clear();

        _resOptions.Add(
            new Vector2Int(
                1280,
                720
            )
        );

        _resOptions.Add(
            new Vector2Int(
                1600,
                900
            )
        );

        _resOptions.Add(
            new Vector2Int(
                1920,
                1080
            )
        );

        List<string> options =
            new List<string>(
                _resOptions.Count
            );

        for (int i = 0;
             i < _resOptions.Count;
             i++)
        {
            options.Add(
                $"{_resOptions[i].x} x {_resOptions[i].y}"
            );
        }

        resolutionDropdown.AddOptions(
            options
        );

        int idx =
            _resOptions.FindIndex(
                v =>
                    v.x == Screen.width &&
                    v.y == Screen.height
            );

        if (idx < 0)
        {
            idx =
                Mathf.Max(
                    0,
                    _resOptions.Count - 1
                );
        }

        resolutionDropdown.value = idx;

        resolutionDropdown
            .RefreshShownValue();

        _selectedWidth =
            _resOptions[idx].x;

        _selectedHeight =
            _resOptions[idx].y;
    }

    private void SetupScreenModeOptions()
    {
        if (!screenModeDropdown)
            return;

        screenModeDropdown.ClearOptions();

        screenModeDropdown.AddOptions(
            new List<string>
            {
                "Fullscreen",
                "Windowed",
                "Borderless"
            }
        );

        int idx =
            GetCurrentScreenModeIndex();

        screenModeDropdown.value = idx;

        screenModeDropdown
            .RefreshShownValue();

        UpdateResolutionInteractable(
            GetScreenModeFromDropdown(idx)
        );
    }

    public void SetResolution(
        int index
    )
    {
        if (index < 0 ||
            index >= _resOptions.Count)
        {
            return;
        }

        _selectedWidth =
            _resOptions[index].x;

        _selectedHeight =
            _resOptions[index].y;

        FullScreenMode mode =
            GetScreenModeFromDropdown();

        ApplyResolution(
            _selectedWidth,
            _selectedHeight,
            mode
        );

        SaveVideoSettings();
    }

    public void SetScreenMode(
        int index
    )
    {
        FullScreenMode mode =
            GetScreenModeFromDropdown(
                index
            );

        if (_selectedWidth <= 0 ||
            _selectedHeight <= 0)
        {
            _selectedWidth =
                Screen.width;

            _selectedHeight =
                Screen.height;
        }

        ApplyResolution(
            _selectedWidth,
            _selectedHeight,
            mode
        );

        SaveVideoSettings();
    }

    private void LoadAndApplyVideoSettings()
    {
        int width =
            PlayerPrefs.GetInt(
                PP_WIDTH,
                Screen.width
            );

        int height =
            PlayerPrefs.GetInt(
                PP_HEIGHT,
                Screen.height
            );

        int modeIdx =
            PlayerPrefs.GetInt(
                PP_MODE,
                GetCurrentScreenModeIndex()
            );

        _selectedWidth = width;
        _selectedHeight = height;

        if (resolutionDropdown)
        {
            int found =
                _resOptions.FindIndex(
                    v =>
                        v.x == width &&
                        v.y == height
                );

            if (found >= 0)
            {
                resolutionDropdown.value =
                    found;
            }

            resolutionDropdown
                .RefreshShownValue();
        }

        if (screenModeDropdown)
        {
            screenModeDropdown.value =
                Mathf.Clamp(
                    modeIdx,
                    0,
                    2
                );

            screenModeDropdown
                .RefreshShownValue();
        }

        FullScreenMode mode =
            GetScreenModeFromDropdown(
                modeIdx
            );

        ApplyResolution(
            width,
            height,
            mode
        );
    }

    private void SaveVideoSettings()
    {
        PlayerPrefs.SetInt(
            PP_WIDTH,
            _selectedWidth
        );

        PlayerPrefs.SetInt(
            PP_HEIGHT,
            _selectedHeight
        );

        PlayerPrefs.SetInt(
            PP_MODE,
            screenModeDropdown
                ? screenModeDropdown.value
                : GetCurrentScreenModeIndex()
        );

        PlayerPrefs.Save();
    }

    private FullScreenMode GetScreenModeFromDropdown(
        int forcedIndex = -1
    )
    {
        int idx =
            forcedIndex >= 0
                ? forcedIndex
                : (
                    screenModeDropdown
                        ? screenModeDropdown.value
                        : GetCurrentScreenModeIndex()
                );

        switch (idx)
        {
            case 0:
                return FullScreenMode
                    .ExclusiveFullScreen;

            case 1:
                return FullScreenMode
                    .Windowed;

            case 2:
                return FullScreenMode
                    .FullScreenWindow;

            default:
                return FullScreenMode
                    .FullScreenWindow;
        }
    }

    private int GetCurrentScreenModeIndex()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode
                .ExclusiveFullScreen:

                return 0;

            case FullScreenMode
                .Windowed:

                return 1;

            case FullScreenMode
                .FullScreenWindow:

                return 2;

            default:
                return 2;
        }
    }

    private void ApplyResolution(
        int w,
        int h,
        FullScreenMode mode
    )
    {
#if UNITY_2021_2_OR_NEWER

        var rr =
            Screen.currentResolution
                .refreshRateRatio;

        Screen.SetResolution(
            w,
            h,
            mode,
            rr
        );

#else

        int rr =
            Screen.currentResolution
                .refreshRate;

        Screen.SetResolution(
            w,
            h,
            mode,
            rr
        );

#endif

        UpdateResolutionInteractable(
            mode
        );

        Debug.Log(
            $"[PauseMenu] Requested {w}x{h} {mode}, now Screen={Screen.width}x{Screen.height} mode={Screen.fullScreenMode}"
        );
    }

    private void UpdateResolutionInteractable(
        FullScreenMode mode
    )
    {
        if (resolutionDropdown)
        {
            resolutionDropdown.interactable =
                mode !=
                FullScreenMode
                    .FullScreenWindow;
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}