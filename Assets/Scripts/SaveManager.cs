using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private string saveFilePath;

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build the path: persistentDataPath/savefile.json
        saveFilePath = Path.Combine(Application.persistentDataPath, "savefile.json");

        // Subscribe to sceneLoaded for auto-saving
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Don't auto-save in the MainMenu scene
        if (scene.name == "MainMenu")
            return;

        AutoSave();
    }

    #region Public Save / Load

    public void ManualSave()
    {
        Debug.Log("SaveManager: Manual save requested.");
        SaveGame();
    }

    public void AutoSave()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"SaveManager: Auto-saving at scene start: {sceneName}");
        SaveGame();
    }

    public bool LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("SaveManager: No save file found at " + saveFilePath);
            return false;
        }

        string json = File.ReadAllText(saveFilePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        string targetScene = data.sceneName;
        if (SceneManager.GetActiveScene().name != targetScene)
        {
            StartCoroutine(LoadSceneAndApply(targetScene, data));
        }
        else
        {
            ApplyPlayerPosition(data);
        }

        return true;
    }

    #endregion

    #region Internal: Save & Apply

    private void SaveGame()
    {
        // Find the GameObject tagged "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("SaveManager: Could not find any GameObject tagged 'Player'. Save aborted.");
            return;
        }

        // Read its position
        Vector3 playerPos = playerObj.transform.position;
        string currentScene = SceneManager.GetActiveScene().name;

        // Build SaveData
        SaveData data = new SaveData(currentScene, playerPos);

        // Serialize to JSON
        string json = JsonUtility.ToJson(data, prettyPrint: true);

        // Write to disk
        try
        {
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"SaveManager: Game saved to {saveFilePath}");
        }
        catch (IOException e)
        {
            Debug.LogError("SaveManager: Failed to write save file. " + e.Message);
        }
    }

    private IEnumerator LoadSceneAndApply(string sceneName, SaveData data)
    {
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;

        // Wait one more frame so any Awake/Start calls finish
        yield return null;

        ApplyPlayerPosition(data);
    }

    private void ApplyPlayerPosition(SaveData data)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerObj.transform.position = data.playerPosition.ToVector3();
            Debug.Log($"SaveManager: Moved Player to {data.playerPosition.x}, {data.playerPosition.y}, {data.playerPosition.z}");
        }
        else
        {
            Debug.LogWarning("SaveManager: Could not find Player GameObject when applying saved position.");
        }
    }

    #endregion
}