using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private string saveFilePath;

    // When true, skip the autosave that happens on scene load
    // (used when we are loading a scene from a save to avoid overwriting).
    private bool _suppressAutoSaveThisLoad = false;

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

        // If we just initiated a LoadGame, we skip this autosave.
        if (_suppressAutoSaveThisLoad)
            return;

        // Autosave at the start of each scene, after one frame to ensure everything spawned.
        StartCoroutine(DelayedAutoSave());
    }

    private IEnumerator DelayedAutoSave(float extraDelaySeconds = 0f)
    {
        // Wait for all Awake/Start calls and one rendered frame
        yield return new WaitForEndOfFrame();

        if (extraDelaySeconds > 0f)
            yield return new WaitForSeconds(extraDelaySeconds);

        AutoSave();
    }

    #region Public Save / Load

    public void ManualSave()
    {
        Debug.Log("SaveManager: Manual save requested.");
        SaveGame();
    }

    /// <summary>
    /// Public entry to autosave. Used by scene start and after load+apply.
    /// </summary>
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

        // Suppress the autosave that happens on scene load so we don't overwrite
        // the file with default spawn position before we re-position the player.
        _suppressAutoSaveThisLoad = true;

        if (SceneManager.GetActiveScene().name != targetScene)
        {
            StartCoroutine(LoadSceneAndApply(targetScene, data));
        }
        else
        {
            ApplyPlayerPosition(data);
            // After applying, autosave on next frame at the correct position.
            StartCoroutine(DelayedAutoSave());
            _suppressAutoSaveThisLoad = false;
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

        // Wait one more frame so any Awake/Start calls finish and spawns settle
        yield return new WaitForEndOfFrame();

        ApplyPlayerPosition(data);

        // Now that the player is placed, autosave to lock in correct position.
        yield return StartCoroutine(DelayedAutoSave());

        _suppressAutoSaveThisLoad = false;
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
