using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject SettingsPanel;
    public GameObject VideoSettingsPanel;
    public GameObject AudioSettingsPanel;
    public GameObject ControlsSettingsPanel;
    public GameObject CreditsPanel;
    public GameObject MenuPanel;

    [Header("Buttons")]
    public Button ContinueButton;

    private string saveFilePath;

    void Awake()
    {
        // Build the path to the save file
        saveFilePath = Path.Combine(Application.persistentDataPath, "savefile.json");

        if (ContinueButton != null)
        {
            // If no save exists, hide the Continue button
            if (!File.Exists(saveFilePath))
            {
                ContinueButton.gameObject.SetActive(false);
            }
        }
    }

    void Start()
    {
        MenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);
    }

    // Start a new game from the beginning
    public void MainGameStart()
    {
        SceneManager.LoadScene("1City");
    }

    // Load the last saved game (scene and player position)
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

    // Open video settings (default sub‐panel)
    public void OpenVideoSettings()
    {
        MenuPanel.SetActive(false);
        SettingsPanel.SetActive(true);
        VideoSettingsPanel.SetActive(true);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(false);
    }

    // Open audio settings
    public void OpenAudioSettings()
    {
        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(true);
        ControlsSettingsPanel.SetActive(false);
    }

    // Open controls settings
    public void OpenControlsSettings()
    {
        VideoSettingsPanel.SetActive(false);
        AudioSettingsPanel.SetActive(false);
        ControlsSettingsPanel.SetActive(true);
    }

    // Close settings or credits and return to main menu
    public void ClosePanel()
    {
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);
        MenuPanel.SetActive(true);
    }

    // Open the credits panel
    public void OpenCredits()
    {
        MenuPanel.SetActive(false);
        CreditsPanel.SetActive(true);
    }

    // Quit the application
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game exited");
    }
}
