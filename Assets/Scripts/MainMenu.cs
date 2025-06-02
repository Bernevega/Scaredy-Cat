using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject MenuPanel;
    public GameObject SettingsPanel;
    public GameObject VideoSettingsPanel;
    public GameObject AudioSettingsPanel;
    public GameObject ControlsSettingsPanel;
    public GameObject CreditsPanel;

    [Header("Buttons")]
    public Button ContinueButton;

    [Header("Audio Settings UI")]
    [Tooltip("Slider range: 0..1")]
    public Slider generalVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Video Settings UI")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;

    private Resolution[] resolutions;
    private string saveFilePath;

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "savefile.json");

        if (ContinueButton != null && !File.Exists(saveFilePath))
        {
            ContinueButton.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Show main menu, hide other panels
        MenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);

        // Populate video dropdowns & load saved volumes
        SetupResolutionOptions();
        SetupScreenModeOptions();
        LoadVolumeSliders();
    }

    // ---------------- Main Menu Buttons ----------------

    public void MainGameStart()
    {
        SceneManager.LoadScene("1City");
    }

    public void LoadGame()
    {
        bool success = SaveManager.Instance.LoadGame();
        if (!success)
        {
            Debug.LogWarning("MainMenu: No save file found to load.");
        }
        else
        {
            Debug.Log("MainMenu: Loaded saved game.");
        }
    }

    public void OpenVideoSettings()
    {
        MenuPanel.SetActive(false);
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

    public void OpenCredits()
    {
        MenuPanel.SetActive(false);
        CreditsPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);
        MenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game exited");
    }

    // ---------------- Audio UI Callbacks ----------------

    /// <summary>
    /// Assign this to generalVolumeSlider.OnValueChanged(float).
    /// </summary>
    public void SetGeneralVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
    }

    /// <summary>
    /// Assign this to musicVolumeSlider.OnValueChanged(float).
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(volume);
        }
    }

    /// <summary>
    /// Assign this to sfxVolumeSlider.OnValueChanged(float).
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(volume);
        }
    }

    /// <summary>
    /// Reads saved PlayerPrefs and sets slider values accordingly.
    /// Also re-applies them via AudioManager so mixer is in sync.
    /// </summary>
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

    /// <summary>
    /// Assign this to resolutionDropdown.OnValueChanged(int).
    /// </summary>
    public void SetResolution(int index)
    {
        string[] dims = resolutionDropdown.options[index].text.Split('x');
        int width = int.Parse(dims[0].Trim());
        int height = int.Parse(dims[1].Trim());
        FullScreenMode mode = GetScreenModeFromDropdown();

        Screen.SetResolution(width, height, mode);
    }

    /// <summary>
    /// Assign this to screenModeDropdown.OnValueChanged(int).
    /// </summary>
    public void SetScreenMode(int index)
    {
        FullScreenMode mode = GetScreenModeFromDropdown();
        Screen.fullScreenMode = mode;
        // Reapply current resolution so Unity respects mode change.
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
