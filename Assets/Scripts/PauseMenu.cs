using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject PauseMenuCanvas;
    public GameObject SettingsPanel;
    public GameObject PausePanel;
    public GameObject VideoSettingsPanel;
    public GameObject AudioSettingsPanel;
    public GameObject ControlsSettingsPanel;

    [Header("Default Selected Buttons")]
    public Button DefaultPauseMenuButton;
    public Button DefaultVideoSettingsButton;
    public Button DefaultAudioSettingsButton;
    public Button DefaultControlsSettingsButton;

    [Header("Audio UI")]
    [Tooltip("Slider range: 0..1")]
    public Slider generalVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Video UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;

    public static bool isPaused { get; private set; } = false;

    private Controls inputControls;

    // --- Video settings state (same approach as MainMenu) ---
    private readonly List<Vector2Int> _resOptions = new List<Vector2Int>();
    private int _selectedWidth;
    private int _selectedHeight;

    // PlayerPrefs keys (shared with main menu if desired)
    private const string PP_WIDTH = "Video_Width";
    private const string PP_HEIGHT = "Video_Height";
    private const string PP_MODE = "Video_Mode"; // 0=ExclusiveFS,1=Windowed,2=Borderless

    private void Awake()
    {
        inputControls = new Controls();
    }

    private void OnEnable()
    {
        inputControls.UI.Enable();
        inputControls.UI.Pause.performed += OnPauseInput;
    }

    private void OnDisable()
    {
        inputControls.UI.Pause.performed -= OnPauseInput;
        inputControls.UI.Disable();
    }

    private void Start()
    {
        PauseMenuCanvas.SetActive(false);
        SettingsPanel.SetActive(false);

        // Populate & hook video settings UI
        VerifyAndPopulateDropdowns();
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (screenModeDropdown) screenModeDropdown.onValueChanged.AddListener(SetScreenMode);

        LoadVolumeSliders();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        isPaused = false;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }

        // Re-focus if controller used and no UI selected
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                ReselectButtonForCurrentPanel();
            }
        }
    }

    private void OnPauseInput(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    private void TogglePause()
    {
        if (!isPaused)
            PauseGame();
        else
            ContinueGame();
    }

    private void ReselectButtonForCurrentPanel()
    {
        if (PausePanel.activeInHierarchy && DefaultPauseMenuButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultPauseMenuButton.gameObject);
        }
        else if (VideoSettingsPanel.activeInHierarchy && DefaultVideoSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultVideoSettingsButton.gameObject);
        }
        else if (AudioSettingsPanel.activeInHierarchy && DefaultAudioSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultAudioSettingsButton.gameObject);
        }
        else if (ControlsSettingsPanel.activeInHierarchy && DefaultControlsSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultControlsSettingsButton.gameObject);
        }
    }

    // ---------------- Pause / Continue ----------------

    public void PauseGame()
    {
        PauseMenuCanvas.SetActive(true);
        isPaused = true;
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (DefaultPauseMenuButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultPauseMenuButton.gameObject);
        }

        Debug.Log("Game paused");
    }

    public void ContinueGame()
    {
        PauseMenuCanvas.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("Game unpaused");
    }

    // ---------------- Settings Navigation ----------------

    public void OpenVideoSettings()
    {
        PausePanel.SetActive(false);
        SettingsPanel.SetActive(true);

        VideoSettingsPanel.SetActive(true);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(false);

        // Ensure options/UI reflect current state when panel opens
        VerifyAndPopulateDropdowns();

        if (DefaultVideoSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultVideoSettingsButton.gameObject);
        }
    }

    public void OpenAudioSettings()
    {
        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(true);
        ControlsSettingsPanel.SetActive(false);

        if (DefaultAudioSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultAudioSettingsButton.gameObject);
        }
    }

    public void OpenControlsSettings()
    {
        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(true);

        if (DefaultControlsSettingsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultControlsSettingsButton.gameObject);
        }
    }

    public void CloseSettings()
    {
        SettingsPanel.SetActive(false);
        PausePanel.SetActive(true);

        if (DefaultPauseMenuButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultPauseMenuButton.gameObject);
        }
    }

    public void SaveGame()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("PauseMenu: Cannot save because there is no SaveManager in the scene!");
            return;
        }

        SaveManager.Instance.ManualSave();
        Debug.Log("PauseMenu: Game saved successfully!");
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        PlayerManager pm = PlayerManager.instance;
        SimpleDialogManager dm = SimpleDialogManager.Instance;

        if (pm)
        {
            Destroy(pm.player.gameObject);
            Destroy(pm.gameObject);
        }
        if (dm)
        {
            Destroy(dm.gameObject);
        }

        SceneManager.LoadScene("MainMenu");
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Game exited");
    }

    // ---------------- Audio UI Callbacks ----------------

    public void SetGeneralVolume(Slider slider)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMasterVolume(slider);
    }

    public void SetMusicVolume(Slider slider)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(slider);
    }

    public void SetSFXVolume(Slider slider)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(slider);
    }

    private void LoadVolumeSliders()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        if (generalVolumeSlider != null)
        {
            generalVolumeSlider.value = master;
            AudioManager.Instance.SetMasterVolume(master);
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = music;
            AudioManager.Instance.SetMusicVolume(music);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfx;
            AudioManager.Instance.SetSFXVolume(sfx);
        }
    }

    // ---------------- Video Settings Logic (same behavior as in MainMenu) ----------------

    private void VerifyAndPopulateDropdowns()
    {
        if (!resolutionDropdown || !screenModeDropdown) return;

        // Populate resolution list (keep the three common presets)
        if (resolutionDropdown.options.Count == 0 ||
            resolutionDropdown.options[0].text.StartsWith("Option"))
        {
            SetupResolutionOptions();
        }

        // Populate screen mode list
        if (screenModeDropdown.options.Count == 0 ||
            screenModeDropdown.options[0].text.StartsWith("Option"))
        {
            SetupScreenModeOptions();
        }

        // Load saved (or current) settings and apply to UI + screen
        LoadAndApplyVideoSettings();
    }

    private void SetupResolutionOptions()
    {
        if (!resolutionDropdown) return;

        resolutionDropdown.ClearOptions();
        _resOptions.Clear();

        _resOptions.Add(new Vector2Int(1280, 720));
        _resOptions.Add(new Vector2Int(1600, 900));
        _resOptions.Add(new Vector2Int(1920, 1080));

        List<string> options = new List<string>(_resOptions.Count);
        for (int i = 0; i < _resOptions.Count; i++)
            options.Add($"{_resOptions[i].x} x {_resOptions[i].y}");

        resolutionDropdown.AddOptions(options);

        // Select current screen res if found, else default to largest
        int idx = _resOptions.FindIndex(v => v.x == Screen.width && v.y == Screen.height);
        if (idx < 0) idx = Mathf.Max(0, _resOptions.Count - 1);
        resolutionDropdown.value = idx;
        resolutionDropdown.RefreshShownValue();

        _selectedWidth = _resOptions[idx].x;
        _selectedHeight = _resOptions[idx].y;
    }

    private void SetupScreenModeOptions()
    {
        if (!screenModeDropdown) return;

        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(new List<string> { "Fullscreen", "Windowed", "Borderless" });

        int idx = GetCurrentScreenModeIndex();
        screenModeDropdown.value = idx;
        screenModeDropdown.RefreshShownValue();

        UpdateResolutionInteractable(GetScreenModeFromDropdown(idx));
    }

    // Dropdown callback
    public void SetResolution(int index)
    {
        if (index < 0 || index >= _resOptions.Count) return;

        _selectedWidth = _resOptions[index].x;
        _selectedHeight = _resOptions[index].y;

        FullScreenMode mode = GetScreenModeFromDropdown();

        // Apply reliably (preserve refresh rate) and save
        ApplyResolution(_selectedWidth, _selectedHeight, mode);
        SaveVideoSettings();
    }

    // Dropdown callback
    public void SetScreenMode(int index)
    {
        FullScreenMode mode = GetScreenModeFromDropdown(index);

        if (_selectedWidth <= 0 || _selectedHeight <= 0)
        {
            _selectedWidth = Screen.width;
            _selectedHeight = Screen.height;
        }

        ApplyResolution(_selectedWidth, _selectedHeight, mode);
        SaveVideoSettings();
    }

    private void LoadAndApplyVideoSettings()
    {
        // Defaults: current screen values
        int width = PlayerPrefs.GetInt(PP_WIDTH, Screen.width);
        int height = PlayerPrefs.GetInt(PP_HEIGHT, Screen.height);
        int modeIdx = PlayerPrefs.GetInt(PP_MODE, GetCurrentScreenModeIndex());

        _selectedWidth = width;
        _selectedHeight = height;

        if (resolutionDropdown)
        {
            int found = _resOptions.FindIndex(v => v.x == width && v.y == height);
            if (found >= 0) resolutionDropdown.value = found;
            resolutionDropdown.RefreshShownValue();
        }
        if (screenModeDropdown)
        {
            screenModeDropdown.value = Mathf.Clamp(modeIdx, 0, 2);
            screenModeDropdown.RefreshShownValue();
        }

        var mode = GetScreenModeFromDropdown(modeIdx);
        ApplyResolution(width, height, mode);
    }

    private void SaveVideoSettings()
    {
        PlayerPrefs.SetInt(PP_WIDTH, _selectedWidth);
        PlayerPrefs.SetInt(PP_HEIGHT, _selectedHeight);
        PlayerPrefs.SetInt(PP_MODE, screenModeDropdown ? screenModeDropdown.value : GetCurrentScreenModeIndex());
        PlayerPrefs.Save();
    }

    // Map dropdown index -> Unity mode
    private FullScreenMode GetScreenModeFromDropdown(int forcedIndex = -1)
    {
        int idx = forcedIndex >= 0
            ? forcedIndex
            : (screenModeDropdown ? screenModeDropdown.value : GetCurrentScreenModeIndex());

        switch (idx)
        {
            case 0: return FullScreenMode.ExclusiveFullScreen;
            case 1: return FullScreenMode.Windowed;
            case 2: return FullScreenMode.FullScreenWindow;
            default: return FullScreenMode.FullScreenWindow;
        }
    }

    private int GetCurrentScreenModeIndex()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen: return 0;
            case FullScreenMode.Windowed: return 1;
            case FullScreenMode.FullScreenWindow: return 2;
            default: return 2;
        }
    }

    private void ApplyResolution(int w, int h, FullScreenMode mode)
    {
#if UNITY_2021_2_OR_NEWER
        var rr = Screen.currentResolution.refreshRateRatio;
        Screen.SetResolution(w, h, mode, rr);
#else
        int rr = Screen.currentResolution.refreshRate;
        Screen.SetResolution(w, h, mode, rr);
#endif
        UpdateResolutionInteractable(mode);
        Debug.Log($"[PauseMenu] Requested {w}x{h} {mode}, now Screen={Screen.width}x{Screen.height} mode={Screen.fullScreenMode}");
    }

    // Disable the resolution dropdown when using Borderless; it ignores width/height.
    private void UpdateResolutionInteractable(FullScreenMode mode)
    {
        if (resolutionDropdown)
            resolutionDropdown.interactable = (mode != FullScreenMode.FullScreenWindow);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}
