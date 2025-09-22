using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject MenuPanel;
    public GameObject SettingsPanel;
    public GameObject VideoSettingsPanel;
    public GameObject AudioSettingsPanel;
    public GameObject ControlsSettingsPanel;

    [Header("Buttons")]
    public Button ContinueButton;
    public Button OpenVideoSettingsButton;
    public Button OpenAudioSettingsButton;
    public Button OpenControlsSettingsButton;
    public Button BackFromSettingsButton;

    [Header("Default Selected Buttons")]
    public Button DefaultMainMenuButton;
    public Button DefaultVideoSettingsButton;
    public Button DefaultAudioSettingsButton;
    public Button DefaultControlsSettingsButton;

    [Header("Audio Settings UI")]
    [Tooltip("Slider range: 0..1")]
    public Slider generalVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Video Settings UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;

    private string saveFilePath;
    private Controls inputControls;
    public MenuSetup setup;
    private bool gameStarted = false;

    // --- Video settings state ---
    private readonly List<Vector2Int> _resOptions = new List<Vector2Int>();
    private int _selectedWidth;
    private int _selectedHeight;

    // PlayerPrefs keys
    private const string PP_WIDTH = "Video_Width";
    private const string PP_HEIGHT = "Video_Height";
    private const string PP_MODE = "Video_Mode"; // 0=ExclusiveFS,1=Windowed,2=Borderless

    // Optional PlayerPrefs marker some save systems set when a save exists
    private const string PP_HAS_SAVE_MARKER = "HasSave";

    private void Awake()
    {
        // Build the expected save path (adjust the file name if your SaveManager uses another one)
        saveFilePath = Path.Combine(Application.persistentDataPath, "savefile.json");

        inputControls = new Controls();
        inputControls.UI.Enable();
        inputControls.Player.Disable();

        // Hide Continue by default to avoid showing it on first boot / fresh installs.
        if (ContinueButton != null)
            ContinueButton.gameObject.SetActive(false);

        // Then check for real save data and show it if present.
        RefreshContinueButton();
    }

    private void OnEnable()
    {
        // In case the object is re-enabled, keep the button state in sync.
        RefreshContinueButton();
    }

    private void OnDestroy()
    {
        if (inputControls != null)
            inputControls.UI.Disable();
    }

    private void Start()
    {
        MenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);

        // Auto-wire dropdowns if missing in Inspector
        if (!resolutionDropdown || !screenModeDropdown)
        {
            var drops = GetComponentsInChildren<TMP_Dropdown>(true);
            foreach (var d in drops)
            {
                var n = d.name.ToLowerInvariant();
                if (!resolutionDropdown && (n.Contains("resolution") || n.Contains("res")))
                    resolutionDropdown = d;
                else if (!screenModeDropdown && (n.Contains("screen") || n.Contains("display") || n.Contains("mode")))
                    screenModeDropdown = d;
            }
        }

        VerifyAndPopulateDropdowns();
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (screenModeDropdown) screenModeDropdown.onValueChanged.AddListener(SetScreenMode);

        LoadVolumeSliders();

        // Ensure Continue reflects current disk state on first frame, too.
        RefreshContinueButton();

        if (DefaultMainMenuButton != null)
            EventSystem.current.SetSelectedGameObject(DefaultMainMenuButton.gameObject);
    }

    private void Update()
    {
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            if (EventSystem.current.currentSelectedGameObject == null)
                ReselectButtonForCurrentPanel();
        }
    }

    private void ReselectButtonForCurrentPanel()
    {
        if (MenuPanel.activeInHierarchy && DefaultMainMenuButton != null)
            EventSystem.current.SetSelectedGameObject(DefaultMainMenuButton.gameObject);
        else if (VideoSettingsPanel.activeInHierarchy && DefaultVideoSettingsButton != null)
            EventSystem.current.SetSelectedGameObject(DefaultVideoSettingsButton.gameObject);
        else if (AudioSettingsPanel.activeInHierarchy && DefaultAudioSettingsButton != null)
            EventSystem.current.SetSelectedGameObject(DefaultAudioSettingsButton.gameObject);
        else if (ControlsSettingsPanel.activeInHierarchy && DefaultControlsSettingsButton != null)
            EventSystem.current.SetSelectedGameObject(DefaultControlsSettingsButton.gameObject);
    }

    // ---------- Continue button visibility ----------

    private void RefreshContinueButton()
    {
        if (ContinueButton == null) return;
        bool hasSave = DetectSaveData();
        ContinueButton.gameObject.SetActive(hasSave);
        // Optional log to verify on player builds (remove later if noisy).
        Debug.Log($"[MainMenu] Save detected: {hasSave} | path: {saveFilePath}");
    }

    private bool DetectSaveData()
    {
        // 1) Direct, exact file
        try
        {
            if (File.Exists(saveFilePath))
                return true;
        }
        catch { /* ignore */ }

        // 2) Any common “save” file names in the persistent data folder
        try
        {
            string dir = Application.persistentDataPath;
            if (Directory.Exists(dir))
            {
                // Any file with "save" in its name
                if (Directory.GetFiles(dir, "*save*.*").Length > 0) return true;
                // Common patterns
                if (Directory.GetFiles(dir, "*.sav").Length > 0) return true;
                if (Directory.GetFiles(dir, "*.save").Length > 0) return true;
                if (Directory.GetFiles(dir, "*.json").Any(f => Path.GetFileName(f).ToLowerInvariant().Contains("save")))
                    return true;
            }
        }
        catch { /* ignore */ }

        // 3) Optional PlayerPrefs marker (use this if your SaveManager sets it when saving)
        if (PlayerPrefs.GetInt(PP_HAS_SAVE_MARKER, 0) == 1)
            return true;

        // If none of the above hit, assume no save.
        return false;
    }

    // ---------------- Main Menu Buttons ----------------

    public void MainGameStart()
    {
        if (!gameStarted)
        {
            SceneManager.LoadScene("1City");
            gameStarted = true;
            if (setup) setup.MovingToNewScene();
        }
    }

    public void LoadGame()
    {
        if (gameStarted) return;

        // Extra guard: if no save detected, keep the button hidden.
        if (!DetectSaveData())
        {
            Debug.LogWarning("MainMenu: Load requested but no save data detected. Hiding Continue.");
            RefreshContinueButton();
            return;
        }

        bool success = SaveManager.Instance.LoadGame();
        if (!success)
        {
            Debug.LogWarning("MainMenu: No save file found to load. Hiding Continue.");
            // If load failed (e.g., corrupted/missing), hide the button.
            RefreshContinueButton();
        }
        else
        {
            Debug.Log("MainMenu: Loaded saved game.");
            gameStarted = true;
            if (setup) setup.MovingToNewScene();
        }
    }

    public void OpenVideoSettings()
    {
        MenuPanel.SetActive(false);
        SettingsPanel.SetActive(true);

        VideoSettingsPanel.SetActive(true);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(false);

        VerifyAndPopulateDropdowns();

        if (DefaultVideoSettingsButton != null)
            EventSystem.current.SetSelectedGameObject(DefaultVideoSettingsButton.gameObject);
    }

    public void OpenAudioSettings()
    {
        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(true);
        ControlsSettingsPanel.SetActive(false);

        if (DefaultAudioSettingsButton != null)
            EventSystem.current.SetSelectedGameObject(DefaultAudioSettingsButton.gameObject);
    }

    public void OpenControlsSettings()
    {
        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(true);

        if (DefaultControlsSettingsButton != null)
            EventSystem.current.SetSelectedGameObject(DefaultControlsSettingsButton.gameObject);
    }

    public void ClosePanel()
    {
        Debug.Log("CLOSED PANEL");
        SettingsPanel.SetActive(false);
        MenuPanel.SetActive(true);

        // Keep Continue button visibility fresh when returning
        RefreshContinueButton();

        if (DefaultMainMenuButton != null)
            EventSystem.current.SetSelectedGameObject(DefaultMainMenuButton.gameObject);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game exited");
    }

    // ---------------- Audio UI Callbacks ----------------

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
        float master = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        if (generalVolumeSlider != null)
        {
            generalVolumeSlider.value = master;
            if (AudioManager.Instance) AudioManager.Instance.SetMasterVolume(master);
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = music;
            if (AudioManager.Instance) AudioManager.Instance.SetMusicVolume(music);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfx;
            if (AudioManager.Instance) AudioManager.Instance.SetSFXVolume(sfx);
        }
    }

    // ---------------- Video Settings Logic ----------------

    private void VerifyAndPopulateDropdowns()
    {
        if (!resolutionDropdown || !screenModeDropdown) return;

        bool resLooksDefault =
            resolutionDropdown.options.Count > 0 &&
            resolutionDropdown.options[0] != null &&
            resolutionDropdown.options[0].text.StartsWith("Option", StringComparison.OrdinalIgnoreCase);

        if (resLooksDefault)
            resolutionDropdown.ClearOptions();

        if (resolutionDropdown.options.Count == 0)
            SetupResolutionOptions();

        bool modeLooksDefault =
            screenModeDropdown.options.Count > 0 &&
            screenModeDropdown.options[0] != null &&
            screenModeDropdown.options[0].text.StartsWith("Option", StringComparison.OrdinalIgnoreCase);

        if (screenModeDropdown.options.Count == 0 || modeLooksDefault)
            SetupScreenModeOptions();

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

        List<string> options = _resOptions.Select(v => $"{v.x} x {v.y}").ToList();
        resolutionDropdown.AddOptions(options);

        int currentIndex = _resOptions.FindIndex(v => v.x == Screen.width && v.y == Screen.height);
        if (currentIndex < 0) currentIndex = Mathf.Max(0, _resOptions.Count - 1);

        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();

        _selectedWidth = _resOptions[currentIndex].x;
        _selectedHeight = _resOptions[currentIndex].y;
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

    public void SetResolution(int index)
    {
        if (index < 0 || index >= _resOptions.Count) return;

        _selectedWidth = _resOptions[index].x;
        _selectedHeight = _resOptions[index].y;

        FullScreenMode mode = GetScreenModeFromDropdown();
        ApplyResolution(_selectedWidth, _selectedHeight, mode);
        SaveVideoSettings();
    }

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

    private void LoadAndApplyVideoSettings()
    {
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

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
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
        Debug.Log($"[Video] Requested {w}x{h} {mode}, now Screen={Screen.width}x{Screen.height} mode={Screen.fullScreenMode}");
    }

    private void UpdateResolutionInteractable(FullScreenMode mode)
    {
        if (resolutionDropdown)
            resolutionDropdown.interactable = (mode != FullScreenMode.FullScreenWindow);
    }
}
