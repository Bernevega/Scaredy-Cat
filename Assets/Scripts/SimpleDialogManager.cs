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
    public string text;      // The actual line (no "Mom Cat:" prefix here)
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

    [Tooltip("Image component to display the speaker's icon")]
    public Image speakerIcon;

    [Tooltip("TextMeshPro for showing NPC lines (Mom Cat)")]
    public TMP_Text npcText;

    [Tooltip("TextMeshPro for showing Player (Kitty) lines")]
    public TMP_Text playerText;

    [Tooltip("The Continue button (just one)")]
    public Button continueButton;

    [Header("Speaker Portrait Sprites")]
    [Tooltip("Portrait sprite for Mom Cat")]
    public Sprite momCatSprite;

    [Tooltip("Portrait sprite for Kitty")]
    public Sprite kittySprite;

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
    /// Displays the current node’s text in either npcText or playerText, 
    /// and sets the speakerIcon sprite based on currentNode.speaker.
    /// </summary>
    private void ShowCurrentNode()
    {
        // 1) Set the speakerIcon sprite
        if (currentNode.speaker == "Mom Cat")
        {
            speakerIcon.sprite = momCatSprite;
        }
        else if (currentNode.speaker == "Kitty")
        {
            speakerIcon.sprite = kittySprite;
        }
        else
        {
            // If you add more speakers in the future, extend this if/else.
            speakerIcon.sprite = null;
        }

        // 2) Clear both text fields
        npcText.text = "";
        playerText.text = "";

        // 3) Put the raw line (no "Mom Cat:" prefix) into the appropriate field
        if (currentNode.speaker == "Kitty")
        {
            playerText.text = currentNode.text;
        }
        else
        {
            npcText.text = currentNode.text;
        }
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
