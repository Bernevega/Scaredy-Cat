using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

// 1. Mirror the JSON structure exactly.
[System.Serializable]
public class DialogNode
{
    public string speaker;   // “Mom Cat”, “Kitty”, “Tire”, “Ydna”, “Oliver”, or "Ydna, Tire, Oliver"
    public string text;      // The actual line
    public string next;      // The key of the next node (or null if it ends)
}

[System.Serializable]
public class DialogTree
{
    public string start;                              // e.g. "momWarn1" or "kittyAsk"
    public Dictionary<string, DialogNode> nodes;      // Maps keys to DialogNode
}

public class SimpleDialogManager : MonoBehaviour
{
    public static SimpleDialogManager Instance;

    [Header("JSON Setup")]
    [Tooltip("Drag your combined_dialog.json (Mom+Friends dialogues) here")]
    public TextAsset jsonFile;

    [Header("UI References")]
    [Tooltip("The parent panel (Canvas) for dialogue")]
    public GameObject dialogPanel;

    [Tooltip("Image component to display the speaker's icon")]
    public Image speakerIcon;

    [Tooltip("TextMeshPro for showing NPC lines (Mom Cat, Tire, Ydna, Oliver, etc.)")]
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

    [Tooltip("Portrait sprite for Tire")]
    public Sprite tireSprite;

    [Tooltip("Portrait sprite for Ydna")]
    public Sprite ydnaSprite;

    [Tooltip("Portrait sprite for Oliver")]
    public Sprite oliverSprite;

    [Tooltip("Portrait sprite for all three friends together")]
    public Sprite groupFriendsSprite;

    private Dictionary<string, DialogTree> allTrees;
    private DialogTree currentTree;
    private DialogNode currentNode;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Deserialize entire JSON into Dictionary<sceneID, DialogTree>
        allTrees = JsonConvert
            .DeserializeObject<Dictionary<string, DialogTree>>(jsonFile.text);

        dialogPanel.SetActive(false);

        // Wire up Continue button
        continueButton.onClick.AddListener(OnContinuePressed);
    }

    /// <summary>
    /// Call this to start a dialogue.
    /// sceneID must match a top‐level key in your JSON (e.g., "HomeScene", "FriendsScene").
    /// </summary>
    public void StartDialogue(string sceneID)
    {
        if (!allTrees.ContainsKey(sceneID))
        {
            Debug.LogWarning($"No dialogue data found for sceneID '{sceneID}'");
            return;
        }

        currentTree = allTrees[sceneID];
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
        // 1) Choose the correct portrait based on speaker name
        if (currentNode.speaker == "Mom Cat")
        {
            speakerIcon.sprite = momCatSprite;
        }
        else if (currentNode.speaker == "Kitty")
        {
            speakerIcon.sprite = kittySprite;
        }
        else if (currentNode.speaker == "Tire")
        {
            speakerIcon.sprite = tireSprite;
        }
        else if (currentNode.speaker == "Ydna")
        {
            speakerIcon.sprite = ydnaSprite;
        }
        else if (currentNode.speaker == "Oliver")
        {
            speakerIcon.sprite = oliverSprite;
        }
        else if (currentNode.speaker == "Ydna, Tire, Oliver")
        {
            speakerIcon.sprite = groupFriendsSprite;
        }
        else
        {
            // If you add more characters later, extend this chain
            speakerIcon.sprite = null;
        }

        // 2) Clear both text fields
        npcText.text = "";
        playerText.text = "";

        // 3) Put the raw line into the appropriate field
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
        if (currentNode == null || string.IsNullOrEmpty(currentNode.next))
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
