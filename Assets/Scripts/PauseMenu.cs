using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject PauseMenuCanvas;      // Root canvas for pause UI
    public GameObject SettingsPanel;        // Parent of all settings subpanels
    public GameObject PausePanel;           // The “Paused: Continue/Settings/…” panel
    public GameObject VideoSettingsPanel;   // Sub‐panel for resolution & mode
    public GameObject AudioSettingsPanel;   // Sub‐panel for volume sliders
    public GameObject ControlsSettingsPanel;// Sub‐panel for control bindings

    [Header("Audio UI")]
    [Tooltip("Slider range: 0..1")]
    public Slider generalVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Video UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;

    public static bool isPaused { get; private set; } = false;
    private Resolution[] resolutions;

    private void Start()
    {
        // Initially hide everything except gameplay
        PauseMenuCanvas.SetActive(false);
        SettingsPanel.SetActive(false);

        SetupResolutionOptions();
        SetupScreenModeOptions();
        LoadVolumeSliders();

        // Hide cursor at start
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
                PauseGame();
            else
                ContinueGame();
        }
    }

    // ---------------- Pause / Continue ----------------

    public void PauseGame()
    {
        PauseMenuCanvas.SetActive(true);
        isPaused = true;
        Time.timeScale = 0f;

        // Show cursor when paused
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Debug.Log("Game paused");
    }

    public void ContinueGame()
    {
        PauseMenuCanvas.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;

        // Hide cursor when resuming gameplay
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
    }

    public void OpenAudioSettings()
    {
        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(true);
        ControlsSettingsPanel.SetActive(false);
    }

    public void OpenControlsSettings()
    {
        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        SettingsPanel.SetActive(false);
        PausePanel.SetActive(true);
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

    // ---------------- Video Settings Logic ----------------

    private void SetupResolutionOptions()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            if (!options.Contains(option))
            {
                options.Add(option);
            }

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void SetupScreenModeOptions()
    {
        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(new List<string> { "Fullscreen", "Windowed", "Borderless" });
        screenModeDropdown.value = GetCurrentScreenModeIndex();
        screenModeDropdown.RefreshShownValue();
    }

    public void SetResolution(int index)
    {
        string[] dims = resolutionDropdown.options[index].text.Split('x');
        int width = int.Parse(dims[0].Trim());
        int height = int.Parse(dims[1].Trim());
        FullScreenMode mode = GetScreenModeFromDropdown();

        Screen.SetResolution(width, height, mode);
    }

    public void SetScreenMode(int index)
    {
        FullScreenMode mode = GetScreenModeFromDropdown();
        Screen.fullScreenMode = mode;
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, mode);
    }

    private FullScreenMode GetScreenModeFromDropdown()
    {
        switch (screenModeDropdown.value)
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

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}
