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

    private void Start()
    {
        PauseMenuCanvas.SetActive(false);
        SettingsPanel.SetActive(false);

        SetupResolutionOptions();
        SetupScreenModeOptions();
        LoadVolumeSliders();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        isPaused = false;
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

        // Re-focus if controller used and no UI selected
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
        QuestManager qm = QuestManager.instance;

        if (pm)
        {
            Destroy(pm.player);
            Destroy(pm.gameObject);
        }
        if (dm)
        {
            Destroy(dm.gameObject);
        }
        if (qm)
        {
            Destroy(qm.gameObject);
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
