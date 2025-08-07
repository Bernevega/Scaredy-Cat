using System.IO;
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
    public GameObject CreditsPanel;

    [Header("Buttons")]
    public Button ContinueButton;
    public Button OpenVideoSettingsButton;
    public Button OpenAudioSettingsButton;
    public Button OpenControlsSettingsButton;
    public Button BackFromSettingsButton;
    public Button BackFromCreditsButton;

    [Header("Default Selected Buttons")]
    public Button DefaultMainMenuButton;
    public Button DefaultVideoSettingsButton;
    public Button DefaultAudioSettingsButton;
    public Button DefaultControlsSettingsButton;
    public Button DefaultCreditsButton;

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

    private void Awake()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "savefile.json");

        if (ContinueButton != null && !File.Exists(saveFilePath))
        {
            ContinueButton.gameObject.SetActive(false);
        }

        inputControls = new Controls();
        inputControls.UI.Enable();
        inputControls.Player.Disable();
    }

    private void OnDestroy()
    {
        if (inputControls != null)
        {
            inputControls.UI.Disable();
        }
    }

    private void Start()
    {
        MenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);

        SetupResolutionOptions();
        SetupScreenModeOptions();
        LoadVolumeSliders();

        if (DefaultMainMenuButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultMainMenuButton.gameObject);
        }
    }

    private void Update()
    {
        // Detect gamepad usage and reselect button if lost focus
        if (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame)
        {
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                ReselectButtonForCurrentPanel();
            }
        }
    }

    private void ReselectButtonForCurrentPanel()
    {
        if (MenuPanel.activeInHierarchy && DefaultMainMenuButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultMainMenuButton.gameObject);
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
        else if (CreditsPanel.activeInHierarchy && DefaultCreditsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultCreditsButton.gameObject);
        }
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

    public void OpenCredits()
    {
        MenuPanel.SetActive(false);
        CreditsPanel.SetActive(true);

        if (DefaultCreditsButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultCreditsButton.gameObject);
        }
    }

    public void ClosePanel()
    {
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);
        MenuPanel.SetActive(true);

        if (DefaultMainMenuButton != null)
        {
            EventSystem.current.SetSelectedGameObject(DefaultMainMenuButton.gameObject);
        }
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
        {
            AudioManager.Instance.SetMasterVolume(volume);
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(volume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(volume);
        }
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
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>
        {
            "1280 x 720",
            "1600 x 900",
            "1920 x 1080"
        };

        int currentResolutionIndex = 0;

        string currentRes = Screen.currentResolution.width + " x " + Screen.currentResolution.height;
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] == currentRes)
            {
                currentResolutionIndex = i;
                break;
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
