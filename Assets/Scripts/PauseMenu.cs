using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    public GameObject PauseMenuCanvas;
    public GameObject SettingsPanel;
    public GameObject PausePanel;
    public GameObject VideoSettingsPanel;
    public GameObject AudioSettingsPanel;
    public GameObject ControlsSettingsPanel;

    private bool isPaused;

    void Start()
    {
        isPaused = false;
        PauseMenuCanvas.SetActive(false);
        SettingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown("escape"))
        {
            if (!isPaused) 
            {
                PauseGame();
            }
            else
            {
                ContinueGame();
            }
        }
    }

    public void PauseGame()
    {
        PauseMenuCanvas.SetActive(true);
        isPaused = true;
        Time.timeScale = 0f;
        Debug.Log("Game paused");
    }

    public void ContinueGame()
    {
        PauseMenuCanvas.SetActive(false);
        isPaused = false;
        Time.timeScale = 1f;
        Debug.Log("Game unpaused");
    }

    public void OpenVideoSettings()
    {
        PausePanel.SetActive(false);
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

    public void CloseSettings()
    {
        PausePanel.SetActive(true);
        SettingsPanel.SetActive(false);
    }

    public void SaveGame()
    {
        // Make sure the SaveManager singleton exists in the scene
        if (SaveManager.Instance == null)
        {
            Debug.LogError("PauseMenu: Cannot save because there is no SaveManager in the scene!");
            return;
        }

        // Call the SaveManager’s manual‐save function. This writes out the JSON file
        SaveManager.Instance.ManualSave();

        Debug.Log("PauseMenu: Game saved successfully!");
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Game exited");
    }
}
