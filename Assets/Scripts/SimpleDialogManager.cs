using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

[Serializable]
public class DialogNode
{
    public string speaker;
    public string text;
    public string next;
}

[Serializable]
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

    [Header("Speaker Portrait Sprites")]
    public Sprite momCatSprite;
    public Sprite kittySprite;
    public Sprite tireSprite;
    public Sprite ydnaSprite;
    public Sprite oliverSprite;
    public Sprite groupFriendsSprite;
    public Sprite crowSprite;

    // New character sprites
    public Sprite assylaSprite;
    public Sprite johnDanielSprite;
    public Sprite mangleSprite;
    public Sprite nSprite;
    public Sprite chicaSprite;
    public Sprite oyenSprite;

    [Header("Transition")]
    [Tooltip("Seconds to fade in/out dialog panel")]
    public float fadeDuration = 0.25f;

    // Internals
    private Dictionary<string, DialogTree> allTrees;
    private DialogTree currentTree;
    private DialogNode currentNode;
    private string currentKey;
    private string currentTreeName;
    private Graphic[] graphicsUnderPanel;
    private bool isFading = false;
    public bool dialogueStart = false;

    /// <summary>
    /// Event invoked whenever dialogue starts, ends, or advances.
    /// Parameters: sceneID, currentNodeKey (or null if dialogue ended)
    /// </summary>
    public Action<string, string> eventDialogueChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            // Transfer references on duplicate
            Instance.jsonFile        = jsonFile;
            Instance.dialogPanel     = dialogPanel;
            Instance.speakerNameText = speakerNameText;
            Instance.speakerIcon     = speakerIcon;
            Instance.npcText         = npcText;
            Instance.playerText      = playerText;

            Instance.momCatSprite       = momCatSprite;
            Instance.kittySprite        = kittySprite;
            Instance.tireSprite         = tireSprite;
            Instance.ydnaSprite         = ydnaSprite;
            Instance.oliverSprite       = oliverSprite;
            Instance.groupFriendsSprite = groupFriendsSprite;
            Instance.crowSprite         = crowSprite;

            Instance.assylaSprite       = assylaSprite;
            Instance.johnDanielSprite   = johnDanielSprite;
            Instance.mangleSprite       = mangleSprite;
            Instance.nSprite            = nSprite;
            Instance.chicaSprite        = chicaSprite;
            Instance.oyenSprite         = oyenSprite;

            Instance.fadeDuration       = fadeDuration;

            Instance.Initialize();
            Destroy(gameObject);
            return;
        }
    }

    public void Initialize()
    {
        // Load all dialog trees
        if (allTrees != null)
            allTrees.Clear();
        allTrees = JsonConvert.DeserializeObject<Dictionary<string, DialogTree>>(jsonFile.text);

        // Prepare fading: collect all UI graphics and set invisible
        graphicsUnderPanel = dialogPanel.GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphicsUnderPanel)
        {
            var c = g.color;
            c.a = 0f;
            g.color = c;
        }
        isFading = false;

        dialogPanel.SetActive(false);
    }

    void Update()
    {
        // Only advance when dialogue is visible and not mid-fade
        if (dialogPanel.activeSelf && !isFading && Input.GetKeyDown(KeyCode.Space))
        {
            OnContinuePressed();
        }
    }

    /// <summary>
    /// Begin dialogue for a given sceneID (key in JSON).
    /// </summary>
    public void StartDialogue(string sceneID)
    {
        if (!allTrees.ContainsKey(sceneID))
        {
            Debug.LogWarning($"[SimpleDialogManager] No dialogue data for sceneID '{sceneID}'");
            return;
        }

        currentTreeName = sceneID;
        currentTree     = allTrees[sceneID];
        currentKey      = currentTree.start;
        currentNode     = currentTree.nodes[currentKey];

        // Fade in panel then show text
        StartCoroutine(FadePanel(0f, 1f));
        dialogueStart = true;
        eventDialogueChanged?.Invoke(currentTreeName, currentKey);
        ShowCurrentNode();
    }

    private void ShowCurrentNode()
    {
        speakerNameText.text = currentNode.speaker;

        // Set correct portrait
        switch (currentNode.speaker)
        {
            case "Mom Cat":             speakerIcon.sprite = momCatSprite;       break;
            case "Kitty":               speakerIcon.sprite = kittySprite;       break;
            case "Tire":                speakerIcon.sprite = tireSprite;        break;
            case "Ydna":                speakerIcon.sprite = ydnaSprite;        break;
            case "Oliver":              speakerIcon.sprite = oliverSprite;      break;
            case "Ydna, Tire, Oliver":  speakerIcon.sprite = groupFriendsSprite;break;
            case "The Crow":            speakerIcon.sprite = crowSprite;        break;

            case "Assyla":              speakerIcon.sprite = assylaSprite;      break;
            case "John Daniel":         speakerIcon.sprite = johnDanielSprite;  break;
            case "Mangle":              speakerIcon.sprite = mangleSprite;      break;
            case "N":                   speakerIcon.sprite = nSprite;           break;
            case "Chica":               speakerIcon.sprite = chicaSprite;       break;
            case "Oyen":                speakerIcon.sprite = oyenSprite;        break;

            default:                     speakerIcon.sprite = null;              break;
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
        if (PauseMenu.isPaused) return;
        if (currentNode == null || string.IsNullOrEmpty(currentNode.next))
        {
            eventDialogueChanged?.Invoke(currentTreeName, null);
            CloseDialogue();
            return;
        }

        var nextKey = currentNode.next;
        if (!currentTree.nodes.ContainsKey(nextKey))
        {
            Debug.LogError($"[SimpleDialogManager] Node '{nextKey}' not found");
            CloseDialogue();
            return;
        }

        currentKey  = nextKey;
        currentNode = currentTree.nodes[nextKey];

        dialogueStart = false;
        eventDialogueChanged?.Invoke(currentTreeName, currentKey);
        
        ShowCurrentNode();
    }

    private void CloseDialogue()
    {
        // Fade out panel
        StartCoroutine(FadePanel(1f, 0f));

        currentNode = null;
        currentTree = null;
        currentKey  = null;
    }

    private IEnumerator FadePanel(float from, float to)
    {
        isFading = true;
        dialogPanel.SetActive(true);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            foreach (var g in graphicsUnderPanel)
            {
                var c = g.color;
                c.a = alpha;
                g.color = c;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Guarantee final alpha
        foreach (var g in graphicsUnderPanel)
        {
            var c = g.color;
            c.a = to;
            g.color = c;
        }

        if (to == 0f)
            dialogPanel.SetActive(false);

        isFading = false;
    }
}