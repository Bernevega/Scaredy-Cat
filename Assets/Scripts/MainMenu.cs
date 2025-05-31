using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject SettingsPanel;
    public GameObject VideoSettingsPanel;
    public GameObject AudioSettingsPanel;
    public GameObject ControlsSettingsPanel;
    public GameObject CreditsPanel;
    public GameObject MenuPanel;

    // Initially hide all of the menu panels
    void Start()
    {
        MenuPanel.SetActive(true);
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);
    }

    // Load the game and start from the beginning
    public void MainGameStart()
    {
        SceneManager.LoadScene("1City");
    }

    // Open game settings (video settings as default)
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

    // Close game settings
    public void ClosePanel()
    {
        SettingsPanel.SetActive(false);
        CreditsPanel.SetActive(false);
        MenuPanel.SetActive(true);
    }

    // Open game credits
    public void OpenCredits()
    {
        MenuPanel.SetActive(false);
        CreditsPanel.SetActive(true);
    }

    // Exit the game
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game exited");
    }
}
