using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject MenuPanel;
    public GameObject SettingsPanel;
    public GameObject VideoSettingsPanel;
    public GameObject AudioSettingsPanel;
    public GameObject ControlsSettingsPanel;

    [Header("Settings Panel Fade")]
    [Tooltip("How long the Settings panel takes to appear/disappear.")]
    public float settingsFadeDuration = 0.3f;

    [Header("Buttons")]
    public Button ContinueButton;
    public Button OpenVideoSettingsButton;
    public Button OpenAudioSettingsButton;
    public Button OpenControlsSettingsButton;
    public Button BackFromSettingsButton;

    [Header("Audio Settings UI")]
    [Tooltip("Slider range: 0..1")]
    public Slider generalVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Video Settings UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;

    private string saveFilePath;
    public MenuSetup setup;
    private bool gameStarted = false;

    private CanvasGroup settingsCanvasGroup;
    private Coroutine settingsFadeCoroutine;

    private readonly List<Vector2Int> _resOptions = new List<Vector2Int>();
    private int _selectedWidth;
    private int _selectedHeight;

    private const string PP_WIDTH = "Video_Width";
    private const string PP_HEIGHT = "Video_Height";
    private const string PP_MODE = "Video_Mode";
    private const string PP_HAS_SAVE_MARKER = "HasSave";

    private void Awake()
    {
        saveFilePath =
            Path.Combine(
                Application.persistentDataPath,
                "savefile.json"
            );

        if (SettingsPanel != null)
        {
            settingsCanvasGroup =
                SettingsPanel.GetComponent<CanvasGroup>();

            if (settingsCanvasGroup == null)
                settingsCanvasGroup =
                    SettingsPanel.AddComponent<CanvasGroup>();

            settingsCanvasGroup.alpha = 0f;
            settingsCanvasGroup.interactable = false;
            settingsCanvasGroup.blocksRaycasts = false;
        }

        if (ContinueButton != null)
            ContinueButton.gameObject.SetActive(false);

        RefreshContinueButton();
    }

    private void OnEnable()
    {
        RefreshContinueButton();
    }

    private void Start()
    {
        MenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);

        if (!resolutionDropdown ||
            !screenModeDropdown)
        {
            var drops =
                GetComponentsInChildren<TMP_Dropdown>(true);

            foreach (var d in drops)
            {
                string n =
                    d.name.ToLowerInvariant();

                if (!resolutionDropdown &&
                    (n.Contains("resolution") ||
                     n.Contains("res")))
                {
                    resolutionDropdown = d;
                }
                else if (!screenModeDropdown &&
                         (n.Contains("screen") ||
                          n.Contains("display") ||
                          n.Contains("mode")))
                {
                    screenModeDropdown = d;
                }
            }
        }

        VerifyAndPopulateDropdowns();

        if (resolutionDropdown)
            resolutionDropdown.onValueChanged.AddListener(SetResolution);

        if (screenModeDropdown)
            screenModeDropdown.onValueChanged.AddListener(SetScreenMode);

        LoadVolumeSliders();

        RefreshContinueButton();

        ClearButtonSelection();
    }

    private void ClearButtonSelection()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void RefreshContinueButton()
    {
        if (ContinueButton == null)
            return;

        bool hasSave = DetectSaveData();

        ContinueButton.gameObject.SetActive(hasSave);

        Debug.Log(
            $"[MainMenu] Save detected: {hasSave} | path: {saveFilePath}"
        );
    }

    private bool DetectSaveData()
    {
        try
        {
            if (File.Exists(saveFilePath))
                return true;
        }
        catch { }

        try
        {
            string dir =
                Application.persistentDataPath;

            if (Directory.Exists(dir))
            {
                if (Directory.GetFiles(
                    dir,
                    "*save*.*"
                ).Length > 0)
                    return true;

                if (Directory.GetFiles(
                    dir,
                    "*.sav"
                ).Length > 0)
                    return true;

                if (Directory.GetFiles(
                    dir,
                    "*.save"
                ).Length > 0)
                    return true;

                if (Directory.GetFiles(
                    dir,
                    "*.json"
                ).Any(
                    f =>
                        Path.GetFileName(f)
                            .ToLowerInvariant()
                            .Contains("save")
                ))
                    return true;
            }
        }
        catch { }

        if (PlayerPrefs.GetInt(
            PP_HAS_SAVE_MARKER,
            0
        ) == 1)
            return true;

        return false;
    }

    // ----------------------------------------------------
    // GAME
    // ----------------------------------------------------

    public void MainGameStart()
    {
        ClearButtonSelection();

        if (gameStarted)
            return;

        gameStarted = true;

        if (setup != null)
        {
            setup.MovingToNewScene(
                () =>
                {
                    SceneManager.LoadScene("1City");
                }
            );
        }
        else
        {
            SceneManager.LoadScene("1City");
        }
    }

    public void LoadGame()
    {
        ClearButtonSelection();

        if (gameStarted)
            return;

        if (!DetectSaveData())
        {
            Debug.LogWarning(
                "MainMenu: Load requested but no save data detected. Hiding Continue."
            );

            RefreshContinueButton();
            return;
        }

        gameStarted = true;

        Action loadGameAfterFade =
            () =>
            {
                bool success =
                    SaveManager.Instance.LoadGame();

                if (!success)
                {
                    Debug.LogWarning(
                        "MainMenu: No save file found to load. Hiding Continue."
                    );

                    gameStarted = false;
                    RefreshContinueButton();

                    if (setup != null)
                        setup.FadeBackFromBlack();
                }
                else
                {
                    Debug.Log(
                        "MainMenu: Loaded saved game."
                    );
                }
            };

        if (setup != null)
            setup.MovingToNewScene(
                loadGameAfterFade
            );
        else
            loadGameAfterFade();
    }

    // ----------------------------------------------------
    // SETTINGS
    // ----------------------------------------------------

    public void OpenVideoSettings()
    {
    ClearButtonSelection();

    // If Settings is already open, we're only switching tabs.
    // Do NOT restart the settings fade or reload the video settings.
    if (SettingsPanel.activeSelf)
    {
        VideoSettingsPanel.SetActive(true);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(false);

        ClearButtonSelection();
        return;
    }

    // We are opening Settings from the main menu.
    if (setup != null)
        setup.HideMainMenuVisualsForSettings();

    MenuPanel.SetActive(false);
    SettingsPanel.SetActive(true);

    VideoSettingsPanel.SetActive(true);
    AudioSettingsPanel.SetActive(false);
    ControlsSettingsPanel.SetActive(false);

    VerifyAndPopulateDropdowns();

    StartSettingsFade(1f);
    }

    public void OpenAudioSettings()
    {
        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(true);
        ControlsSettingsPanel.SetActive(false);

        ClearButtonSelection();
    }

    public void OpenControlsSettings()
    {
        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(true);

        ClearButtonSelection();
    }

    public void ClosePanel()
    {
        Debug.Log("CLOSED PANEL");

        ClearButtonSelection();

        if (settingsFadeCoroutine != null)
            StopCoroutine(settingsFadeCoroutine);

        settingsFadeCoroutine =
            StartCoroutine(
                FadeSettingsOut()
            );
    }

    private void StartSettingsFade(
        float targetAlpha)
    {
        if (settingsCanvasGroup == null)
            return;

        if (settingsFadeCoroutine != null)
            StopCoroutine(settingsFadeCoroutine);

        settingsFadeCoroutine =
            StartCoroutine(
                FadeSettingsCanvas(
                    targetAlpha
                )
            );
    }

    private IEnumerator FadeSettingsCanvas(
        float targetAlpha)
    {
        if (settingsCanvasGroup == null)
            yield break;

        settingsCanvasGroup.interactable = false;
        settingsCanvasGroup.blocksRaycasts = false;

        float startAlpha =
            settingsCanvasGroup.alpha;

        float elapsed = 0f;

        if (settingsFadeDuration <= 0f)
        {
            settingsCanvasGroup.alpha =
                targetAlpha;
        }
        else
        {
            while (elapsed <
                   settingsFadeDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        settingsFadeDuration
                    );

                settingsCanvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        targetAlpha,
                        t
                    );

                yield return null;
            }
        }

        settingsCanvasGroup.alpha =
            targetAlpha;

        if (targetAlpha >= 1f)
        {
            settingsCanvasGroup.interactable = true;
            settingsCanvasGroup.blocksRaycasts = true;
        }

        settingsFadeCoroutine = null;
    }

    private IEnumerator FadeSettingsOut()
    {
        if (settingsCanvasGroup == null)
        {
            SettingsPanel.SetActive(false);

            RefreshContinueButton();

            if (setup != null)
                setup.FadeMainMenuIn();

            MenuPanel.SetActive(true);

            yield break;
        }

        settingsCanvasGroup.interactable = false;
        settingsCanvasGroup.blocksRaycasts = false;

        float startAlpha =
            settingsCanvasGroup.alpha;

        float elapsed = 0f;

        while (elapsed <
               settingsFadeDuration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    settingsFadeDuration
                );

            settingsCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    t
                );

            yield return null;
        }

        settingsCanvasGroup.alpha = 0f;

        SettingsPanel.SetActive(false);

        // Refresh Continue BEFORE fading menu back in
        // so its visibility is correct.
        RefreshContinueButton();

        // Start at alpha 0 while MenuPanel is still hidden.
        // This prevents a one-frame flash.
        if (setup != null)
            setup.FadeMainMenuIn();

        // Then reveal the parent.
        MenuPanel.SetActive(true);

        settingsFadeCoroutine = null;
    }

    public void QuitGame()
    {
        ClearButtonSelection();

        Application.Quit();

        Debug.Log("Game exited");
    }

    // ----------------------------------------------------
    // AUDIO
    // ----------------------------------------------------

    public void SetGeneralVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(volume);
    }

    public void SetMusicVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(volume);
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
            generalVolumeSlider.value = master;

            if (AudioManager.Instance)
                AudioManager.Instance.SetMasterVolume(master);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = music;

            if (AudioManager.Instance)
                AudioManager.Instance.SetMusicVolume(music);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfx;

            if (AudioManager.Instance)
                AudioManager.Instance.SetSFXVolume(sfx);
        }
    }

    // ----------------------------------------------------
    // VIDEO SETTINGS
    // ----------------------------------------------------

    private void VerifyAndPopulateDropdowns()
    {
        if (!resolutionDropdown ||
            !screenModeDropdown)
            return;

        bool resLooksDefault =
            resolutionDropdown.options.Count > 0 &&
            resolutionDropdown.options[0] != null &&
            resolutionDropdown.options[0]
                .text
                .StartsWith(
                    "Option",
                    StringComparison.OrdinalIgnoreCase
                );

        if (resLooksDefault)
            resolutionDropdown.ClearOptions();

        if (resolutionDropdown.options.Count == 0)
            SetupResolutionOptions();

        bool modeLooksDefault =
            screenModeDropdown.options.Count > 0 &&
            screenModeDropdown.options[0] != null &&
            screenModeDropdown.options[0]
                .text
                .StartsWith(
                    "Option",
                    StringComparison.OrdinalIgnoreCase
                );

        if (screenModeDropdown.options.Count == 0 ||
            modeLooksDefault)
            SetupScreenModeOptions();

        LoadAndApplyVideoSettings();
    }

    private void SetupResolutionOptions()
    {
        if (!resolutionDropdown)
            return;

        resolutionDropdown.ClearOptions();
        _resOptions.Clear();

        _resOptions.Add(
            new Vector2Int(1280, 720)
        );

        _resOptions.Add(
            new Vector2Int(1600, 900)
        );

        _resOptions.Add(
            new Vector2Int(1920, 1080)
        );

        List<string> options =
            _resOptions
                .Select(
                    v => $"{v.x} x {v.y}"
                )
                .ToList();

        resolutionDropdown.AddOptions(options);

        int currentIndex =
            _resOptions.FindIndex(
                v =>
                    v.x == Screen.width &&
                    v.y == Screen.height
            );

        if (currentIndex < 0)
            currentIndex =
                Mathf.Max(
                    0,
                    _resOptions.Count - 1
                );

        resolutionDropdown.value =
            currentIndex;

        resolutionDropdown.RefreshShownValue();

        _selectedWidth =
            _resOptions[currentIndex].x;

        _selectedHeight =
            _resOptions[currentIndex].y;
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
        screenModeDropdown.RefreshShownValue();

        UpdateResolutionInteractable(
            GetScreenModeFromDropdown(idx)
        );
    }

    public void SetResolution(int index)
    {
        if (index < 0 ||
            index >= _resOptions.Count)
            return;

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

        ClearButtonSelection();
    }

    public void SetScreenMode(int index)
    {
        FullScreenMode mode =
            GetScreenModeFromDropdown(index);

        if (_selectedWidth <= 0 ||
            _selectedHeight <= 0)
        {
            _selectedWidth = Screen.width;
            _selectedHeight = Screen.height;
        }

        ApplyResolution(
            _selectedWidth,
            _selectedHeight,
            mode
        );

        SaveVideoSettings();

        ClearButtonSelection();
    }

    private FullScreenMode GetScreenModeFromDropdown(
        int forcedIndex = -1)
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
                return FullScreenMode.ExclusiveFullScreen;

            case 1:
                return FullScreenMode.Windowed;

            case 2:
                return FullScreenMode.FullScreenWindow;

            default:
                return FullScreenMode.FullScreenWindow;
        }
    }

    private int GetCurrentScreenModeIndex()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                return 0;

            case FullScreenMode.Windowed:
                return 1;

            case FullScreenMode.FullScreenWindow:
                return 2;

            default:
                return 2;
        }
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
                resolutionDropdown.value =
                    found;

            resolutionDropdown.RefreshShownValue();
        }

        if (screenModeDropdown)
        {
            screenModeDropdown.value =
                Mathf.Clamp(
                    modeIdx,
                    0,
                    2
                );

            screenModeDropdown.RefreshShownValue();
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

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void ApplyResolution(
        int w,
        int h,
        FullScreenMode mode)
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

        UpdateResolutionInteractable(mode);

        Debug.Log(
            $"[Video] Requested {w}x{h} {mode}, now Screen={Screen.width}x{Screen.height} mode={Screen.fullScreenMode}"
        );
    }

    private void UpdateResolutionInteractable(
        FullScreenMode mode)
    {
        if (resolutionDropdown)
        {
            resolutionDropdown.interactable =
                mode !=
                FullScreenMode.FullScreenWindow;
        }
    }
}