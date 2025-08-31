using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Newtonsoft.Json;
using UnityEngine.Audio;

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
    public TextAsset jsonFile;

    [Header("UI References")]
    public GameObject dialogPanel;
    public Image speakerIcon;
    public TMP_Text npcText;
    public TMP_Text playerText;

    [Header("Speaker Portrait Sprites")]
    public Sprite momCatSprite;
    public NPCVoiceScriptable kittyMomVoice;
    public Sprite kittySprite;
    public NPCVoiceScriptable kittyVoice;
    public Sprite tireSprite;
    public Sprite ydnaSprite;
    public Sprite oliverSprite;
    public Sprite groupFriendsSprite;
    public NPCVoiceScriptable friendsVoice;
    public Sprite crowSprite;
    public NPCVoiceScriptable crowVoice;

    public Sprite assylaSprite;
    public Sprite johnDanielSprite;
    public NPCVoiceScriptable johnDanielVoice;
    public Sprite mangleSprite;
    public Sprite nSprite;
    public Sprite chicaSprite;
    public Sprite oyenSprite;
    public Sprite mousieSprite;
    public NPCVoiceScriptable mousieVoice;

    [Tooltip("Fallback portrait for NPCs without a specific sprite")]
    public Sprite defaultNpcSprite;

    [Header("Transition")]
    public float fadeDuration = 0.25f;

    [Header("Audio Routing")]
    [Tooltip("Assign your 'Sounds' mixer group here so dialog SFX route correctly.")]
    public AudioMixerGroup soundsOutputGroup;

    [Tooltip("Optional: existing AudioSource. If empty, one is created automatically.")]
    public AudioSource voiceSource;

    // Input System
    private Controls controls;
    private bool interactInput;

    // Internals
    private Dictionary<string, DialogTree> allTrees;
    private DialogTree currentTree;
    private DialogNode currentNode;
    private string currentKey;
    private string currentTreeName;
    private Graphic[] graphicsUnderPanel;
    private bool isFading = false;
    public bool dialogueStart = false;

    public Action<string, string> eventDialogueChanged;

    void Awake()
    {
        controls = new Controls();
        controls.Player.Interact.performed += ctx => interactInput = true;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
            EnsureVoiceSourceConfigured();
        }
        else
        {
            // Copy inspector refs into existing singleton
            Instance.jsonFile = jsonFile;
            Instance.dialogPanel = dialogPanel;
            Instance.speakerIcon = speakerIcon;
            Instance.npcText = npcText;
            Instance.playerText = playerText;

            Instance.momCatSprite = momCatSprite;
            Instance.kittySprite = kittySprite;
            Instance.tireSprite = tireSprite;
            Instance.ydnaSprite = ydnaSprite;
            Instance.oliverSprite = oliverSprite;
            Instance.groupFriendsSprite = groupFriendsSprite;
            Instance.crowSprite = crowSprite;

            Instance.assylaSprite = assylaSprite;
            Instance.johnDanielSprite = johnDanielSprite;
            Instance.mangleSprite = mangleSprite;
            Instance.nSprite = nSprite;
            Instance.chicaSprite = chicaSprite;
            Instance.oyenSprite = oyenSprite;

            Instance.defaultNpcSprite = defaultNpcSprite;

            Instance.fadeDuration = fadeDuration;

            // Audio routing
            if (soundsOutputGroup != null) Instance.soundsOutputGroup = soundsOutputGroup;
            if (voiceSource != null) Instance.voiceSource = voiceSource;
            Instance.EnsureVoiceSourceConfigured();

            Instance.Initialize();
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable() => controls.Enable();
    void OnDisable() => controls.Disable();

    private void EnsureVoiceSourceConfigured()
    {
        if (voiceSource == null)
        {
            voiceSource = GetComponent<AudioSource>();
            if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();
        }

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f; // 2D
        voiceSource.dopplerLevel = 0f;
        voiceSource.rolloffMode = AudioRolloffMode.Linear;

        if (soundsOutputGroup != null)
            voiceSource.outputAudioMixerGroup = soundsOutputGroup;
        else
            Debug.LogWarning("[SimpleDialogManager] 'soundsOutputGroup' not assigned; dialog SFX won't be routed to a mixer group.");
    }

    public void Initialize()
    {
        if (jsonFile == null)
        {
            Debug.LogError("[SimpleDialogManager] jsonFile not assigned.");
            return;
        }

        if (allTrees != null) allTrees.Clear();
        allTrees = JsonConvert.DeserializeObject<Dictionary<string, DialogTree>>(jsonFile.text);

        graphicsUnderPanel = dialogPanel.GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphicsUnderPanel)
        {
            var c = g.color; c.a = 0f; g.color = c;
        }
        isFading = false;
        dialogPanel.SetActive(false);
    }

    void Update()
    {
        if (!dialogPanel.activeSelf || isFading) return;

        if (Input.GetKeyDown(KeyCode.Space) || interactInput)
        {
            interactInput = false;
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

        currentTreeName = sceneID;
        currentTree = allTrees[sceneID];
        currentKey = currentTree.start;
        currentNode = currentTree.nodes[currentKey];

        StartCoroutine(FadePanel(0f, 1f));
        dialogueStart = true;
        eventDialogueChanged?.Invoke(currentTreeName, currentKey);
        ShowCurrentNode();
    }

    private void ShowCurrentNode()
    {
        // Portrait + voice set
        NPCVoiceScriptable vtp = null;
        switch (currentNode.speaker)
        {
            case "Mom Cat":             speakerIcon.sprite = momCatSprite;        vtp = kittyMomVoice;      break;
            case "Kitty":               speakerIcon.sprite = kittySprite;         vtp = kittyVoice;         break;
            case "Tire":                speakerIcon.sprite = tireSprite;          vtp = friendsVoice;       break;
            case "Ydna":                speakerIcon.sprite = ydnaSprite;          vtp = friendsVoice;       break;
            case "Oliver":              speakerIcon.sprite = oliverSprite;        vtp = friendsVoice;       break;
            case "Ydna, Tire, Oliver":  speakerIcon.sprite = groupFriendsSprite;  vtp = friendsVoice;       break;
            case "The Crow":            speakerIcon.sprite = crowSprite;          vtp = crowVoice;          break;

            case "Assyla":              speakerIcon.sprite = assylaSprite;        vtp = friendsVoice;       break;
            case "John Daniel":         speakerIcon.sprite = johnDanielSprite;    vtp = johnDanielVoice;    break;
            case "Mangle":              speakerIcon.sprite = mangleSprite;        vtp = friendsVoice;       break;
            case "N":                   speakerIcon.sprite = nSprite;             vtp = friendsVoice;       break;
            case "Chica":               speakerIcon.sprite = chicaSprite;         vtp = friendsVoice;       break;
            case "Oyen":                speakerIcon.sprite = oyenSprite;          vtp = friendsVoice;       break;
            case "Mousie":              speakerIcon.sprite = mousieSprite;        vtp = mousieVoice;        break;
            case "Gravestone":          speakerIcon.sprite = groupFriendsSprite;  /* no voice */            break;

            default:
                speakerIcon.sprite = defaultNpcSprite;
                break;
        }

        PlayVoiceFromScriptable(vtp);

        npcText.text = "";
        playerText.text = (currentNode.speaker == "Kitty") ? currentNode.text : "";
        if (currentNode.speaker != "Kitty")
            npcText.text = currentNode.text;
    }

    // ---- AUDIO: Scriptable adapter without PlaySoundInfo ----
    private void PlayVoiceFromScriptable(NPCVoiceScriptable voice)
    {
        if (voice == null) return;

        AudioClip clip = TryGetRandomAudioClip(voice);
        if (clip == null) return;

        if (voiceSource == null) EnsureVoiceSourceConfigured();
        if (soundsOutputGroup != null && voiceSource.outputAudioMixerGroup != soundsOutputGroup)
            voiceSource.outputAudioMixerGroup = soundsOutputGroup;

        voiceSource.PlayOneShot(clip);
    }

    private AudioClip TryGetRandomAudioClip(object scriptable)
    {
        if (scriptable == null) return null;
        var t = scriptable.GetType();

        // 1) Try methods that return AudioClip (no PlaySoundInfo!)
        // e.g., GetRandomVoiceClip(), GetRandomClip()
        MethodInfo[] candidateMethods = {
            t.GetMethod("GetRandomVoiceClip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            t.GetMethod("GetRandomClip",      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        };
        foreach (var m in candidateMethods)
        {
            if (m != null && m.ReturnType == typeof(AudioClip))
            {
                try
                {
                    var result = m.Invoke(scriptable, null) as AudioClip;
                    if (result != null) return result;
                }
                catch { /* ignore and fall through */ }
            }
        }

        // 2) Try to fetch arrays/lists of AudioClip from common fields/properties
        string[] names = { "clips", "voiceClips", "audioClips", "samples", "sounds" };

        // Fields
        foreach (var name in names)
        {
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                var clip = PickFromObjectAsClipCollection(f.GetValue(scriptable));
                if (clip != null) return clip;
            }
        }

        // Properties
        foreach (var name in names)
        {
            var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanRead)
            {
                var clip = PickFromObjectAsClipCollection(p.GetValue(scriptable, null));
                if (clip != null) return clip;
            }
        }

        // Nothing usable found
        return null;
    }

    private AudioClip PickFromObjectAsClipCollection(object val)
    {
        if (val == null) return null;

        // AudioClip[]
        if (val is AudioClip[] arr && arr.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, arr.Length);
            return arr[idx];
        }

        // List<AudioClip>
        var valType = val.GetType();
        if (valType.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(valType))
        {
            var genDef = valType.GetGenericTypeDefinition();
            if (genDef == typeof(List<>))
            {
                var arg = valType.GetGenericArguments()[0];
                if (arg == typeof(AudioClip))
                {
                    var countProp = valType.GetProperty("Count");
                    var indexer   = valType.GetProperty("Item");
                    if (countProp != null && indexer != null)
                    {
                        int count = (int)countProp.GetValue(val, null);
                        if (count > 0)
                        {
                            int idx = UnityEngine.Random.Range(0, count);
                            return indexer.GetValue(val, new object[] { idx }) as AudioClip;
                        }
                    }
                }
            }
        }

        return null;
    }
    // ---- END AUDIO ----

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

        currentKey = nextKey;
        currentNode = currentTree.nodes[nextKey];

        dialogueStart = false;
        eventDialogueChanged?.Invoke(currentTreeName, currentKey);

        ShowCurrentNode();
    }

    private void CloseDialogue()
    {
        StartCoroutine(FadePanel(1f, 0f));
        currentNode = null;
        currentTree = null;
        currentKey = null;
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
                var c = g.color; c.a = alpha; g.color = c;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        foreach (var g in graphicsUnderPanel)
        {
            var c = g.color; c.a = to; g.color = c;
        }

        if (to == 0f) dialogPanel.SetActive(false);
        isFading = false;
    }
}
