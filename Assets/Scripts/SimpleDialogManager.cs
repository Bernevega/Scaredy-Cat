using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

[System.Serializable]
public class DialogNode
{
    public string speaker;
    public string text;
    public string next;
}

[System.Serializable]
public class DialogTree
{
    public string start;
    public Dictionary<string, DialogNode> nodes;
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

    // We no longer need a Continue button reference
    // public Button continueButton;

    [Header("Speaker Portrait Sprites")]
    public Sprite momCatSprite;
    public Sprite kittySprite;
    public Sprite tireSprite;
    public Sprite ydnaSprite;
    public Sprite oliverSprite;
    public Sprite groupFriendsSprite;
    public Sprite crowSprite;

    private Dictionary<string, DialogTree> allTrees;
    private DialogTree currentTree;
    private DialogNode currentNode;
    private string currentKey;

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

        allTrees = JsonConvert.DeserializeObject<Dictionary<string, DialogTree>>(jsonFile.text);

        dialogPanel.SetActive(false);

        // ▶ Remove button listener hookup
        // continueButton.onClick.AddListener(OnContinuePressed);
    }

    void Update()
    {
        // Advance only on Space
        if (dialogPanel.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            OnContinuePressed();
        }
    }

    public void StartDialogue(string sceneID)
    {
        if (!allTrees.ContainsKey(sceneID))
        {
            Debug.LogWarning($"[SimpleDialogManager] No dialogue data for sceneID '{sceneID}'");
            return;
        }

        currentTree = allTrees[sceneID];
        currentKey  = currentTree.start;
        currentNode = currentTree.nodes[currentKey];

        dialogPanel.SetActive(true);
        ShowCurrentNode();
    }

    private void ShowCurrentNode()
    {
        speakerNameText.text = currentNode.speaker;

        switch (currentNode.speaker)
        {
            case "Mom Cat":             speakerIcon.sprite = momCatSprite;       break;
            case "Kitty":               speakerIcon.sprite = kittySprite;       break;
            case "Tire":                speakerIcon.sprite = tireSprite;        break;
            case "Ydna":                speakerIcon.sprite = ydnaSprite;        break;
            case "Oliver":              speakerIcon.sprite = oliverSprite;      break;
            case "Ydna, Tire, Oliver":  speakerIcon.sprite = groupFriendsSprite;break;
            case "The Crow":            speakerIcon.sprite = crowSprite;        break;
            default:                    speakerIcon.sprite = null;              break;
        }

        npcText.text    = "";
        playerText.text = "";

        if (currentNode.speaker == "Kitty")
            playerText.text = currentNode.text;
        else
            npcText.text = currentNode.text;
    }

    private void OnContinuePressed()
    {
        if (currentNode == null || string.IsNullOrEmpty(currentNode.next))
        {
            CloseDialogue();
            return;
        }

        string nextKey = currentNode.next;
        if (!currentTree.nodes.ContainsKey(nextKey))
        {
            Debug.LogError($"[SimpleDialogManager] Node '{nextKey}' not found");
            CloseDialogue();
            return;
        }

        currentKey  = nextKey;
        currentNode = currentTree.nodes[nextKey];
        ShowCurrentNode();
    }

    private void CloseDialogue()
    {
        dialogPanel.SetActive(false);
        currentNode  = null;
        currentTree  = null;
        currentKey   = null;
    }
}
