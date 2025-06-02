using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

// 1. Mirror the JSON structure exactly.
[System.Serializable]
public class DialogNode
{
    public string speaker;   // “Mom Cat” or “Kitty”
    public string text;      // The actual line
    public string next;      // The key of the next node (or null if it ends)
}

[System.Serializable]
public class DialogTree
{
    public string start;                              // e.g. "momWarn1"
    public Dictionary<string, DialogNode> nodes;      // Maps keys to DialogNode
}

public class SimpleDialogManager : MonoBehaviour
{
    public static SimpleDialogManager Instance;

    [Header("JSON Setup")]
    [Tooltip("Drag your home_scene.json here")]
    public TextAsset jsonFile;

    [Header("UI References")]
    [Tooltip("The parent panel (Canvas) for dialogue")]
    public GameObject dialogPanel;

    [Tooltip("TextMeshPro for showing NPC lines")]
    public TMP_Text npcText;

    [Tooltip("TextMeshPro for showing Player (Kitty) lines")]
    public TMP_Text playerText;

    [Tooltip("The Continue button (just one)")]
    public Button continueButton;

    private Dictionary<string, DialogTree> allTrees;
    private DialogTree currentTree;
    private DialogNode currentNode;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Deserialize entire JSON into Dictionary<sceneID, DialogTree>
        allTrees = JsonConvert
            .DeserializeObject<Dictionary<string, DialogTree>>(jsonFile.text);

        dialogPanel.SetActive(false);

        // Wire up Continue button
        continueButton.onClick.AddListener(OnContinuePressed);
    }

    /// <summary>
    /// Call this to start a dialogue. 
    /// sceneID must match a top‐level key in home_scene.json (e.g., "HomeScene").
    /// </summary>
    public void StartDialogue(string sceneID)
    {
        if (!allTrees.ContainsKey(sceneID))
        {
            Debug.LogWarning($"No dialogue data found for '{sceneID}'");
            return;
        }

        currentTree = allTrees[sceneID];
        // Load the first node
        currentNode = currentTree.nodes[currentTree.start];

        dialogPanel.SetActive(true);
        ShowCurrentNode();
    }

    /// <summary>
    /// Displays either in the NPC text field or the player text field,
    /// depending on who is the speaker. Hides the other field.
    /// </summary>
    private void ShowCurrentNode()
    {
        // Clear both fields first
        npcText.text = "";
        playerText.text = "";

        // If this node’s speaker is "Kitty", show in playerText; otherwise npcText.
        if (currentNode.speaker == "Kitty")
        {
            playerText.text = $"Kitty: {currentNode.text}";
        }
        else
        {
            npcText.text = $"{currentNode.speaker}: {currentNode.text}";
        }

        // If next == null, this is the last line → keep Continue to close the panel.
        // If next != null, Continue will advance to that node.
        // (No need to enable/disable the button, since Continue always does the same thing.)
    }

    /// <summary>
    /// Called whenever the single Continue button is pressed.
    /// </summary>
    private void OnContinuePressed()
    {
        // If there's no next node, close dialogue
        if (string.IsNullOrEmpty(currentNode.next))
        {
            CloseDialogue();
            return;
        }

        // Otherwise, step to the next node and refresh UI
        string nextKey = currentNode.next;
        if (!currentTree.nodes.ContainsKey(nextKey))
        {
            Debug.LogError($"Node '{nextKey}' not found in current tree");
            CloseDialogue();
            return;
        }

        currentNode = currentTree.nodes[nextKey];
        ShowCurrentNode();
    }

    private void CloseDialogue()
    {
        dialogPanel.SetActive(false);
        currentNode = null;
        currentTree = null;
    }
}
