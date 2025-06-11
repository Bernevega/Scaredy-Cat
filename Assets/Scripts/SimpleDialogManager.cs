using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

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
    public string start;                             // e.g. "momWarn1" or "kittyAsk"
    public Dictionary<string, DialogNode> nodes;     // Maps keys to DialogNode
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

    [Tooltip("TextMeshPro for showing the speaker’s name")]
    public TMP_Text speakerNameText;

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
    [Tooltip("Portrait for The Crow")]
    public Sprite crowSprite;

    private Dictionary<string, DialogTree> allTrees;
    private DialogTree currentTree;
    private DialogNode currentNode;
    private string currentKey; // Key of the current node

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

        // Parse JSON into dictionary
        allTrees = JsonConvert.DeserializeObject<Dictionary<string, DialogTree>>(jsonFile.text);

        dialogPanel.SetActive(false);
        continueButton.onClick.AddListener(OnContinuePressed);
    }

    void Update()
    {
        // If dialog panel is up and Space is pressed, advance
        if (dialogPanel && dialogPanel.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            OnContinuePressed();
        }
    }

    /// <summary>
    /// Call this to begin a dialogue. sceneID must match a top-level key in JSON.
    /// </summary>
    public void StartDialogue(string sceneID)
    {
        if (!allTrees.ContainsKey(sceneID))
        {
            Debug.LogWarning($"[SimpleDialogManager] No dialogue data for sceneID '{sceneID}'");
            return;
        }

        currentTree = allTrees[sceneID];
        currentKey = currentTree.start;
        currentNode = currentTree.nodes[currentKey];

        dialogPanel.SetActive(true);
        ShowCurrentNode();
    }

    /// <summary>
    /// Display the current node’s text and speaker icon/name.
    /// </summary>
    private void ShowCurrentNode()
    {
        if (speakerNameText != null)
            speakerNameText.text = currentNode.speaker;

        switch (currentNode.speaker)
        {
            case "Mom Cat": speakerIcon.sprite = momCatSprite; break;
            case "Kitty": speakerIcon.sprite = kittySprite; break;
            case "Tire": speakerIcon.sprite = tireSprite; break;
            case "Ydna": speakerIcon.sprite = ydnaSprite; break;
            case "Oliver": speakerIcon.sprite = oliverSprite; break;
            case "Ydna, Tire, Oliver": speakerIcon.sprite = groupFriendsSprite; break;
            case "The Crow": speakerIcon.sprite = crowSprite; break;
            default: speakerIcon.sprite = null; break;
        }

        npcText.text = "";
        playerText.text = "";

        if (currentNode.speaker == "Kitty")
            playerText.text = currentNode.text;
        else
            npcText.text = currentNode.text;
    }

    /// <summary>
    /// Called when Continue is pressed or Space is hit.
    /// Advances to the next node or closes if there is no next.
    /// </summary>
    private void OnContinuePressed()
    {
        if (currentNode == null)
        {
            CloseDialogue();
            return;
        }

        // If there's no next node, just close
        if (string.IsNullOrEmpty(currentNode.next))
        {
            CloseDialogue();
            return;
        }

        // Advance to next
        string nextKey = currentNode.next;
        if (!currentTree.nodes.ContainsKey(nextKey))
        {
            Debug.LogError($"[SimpleDialogManager] Node '{nextKey}' not found");
            CloseDialogue();
            return;
        }

        currentKey = nextKey;
        currentNode = currentTree.nodes[nextKey];
        ShowCurrentNode();
    }

    private void CloseDialogue()
    {
        dialogPanel.SetActive(false);
        currentNode = null;
        currentTree = null;
        currentKey = null;
    }
}
