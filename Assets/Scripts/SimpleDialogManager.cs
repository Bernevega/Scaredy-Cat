using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
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

    [Tooltip("Alternate portrait for Assyla without a hat.")]
    public Sprite assylaNoHatSprite;

    public Sprite johnDanielSprite;
    public NPCVoiceScriptable johnDanielVoice;

    public Sprite mangleSprite;
    public Sprite nSprite;
    public Sprite chicaSprite;
    public Sprite oyenSprite;

    public Sprite mousieSprite;
    public NPCVoiceScriptable mousieVoice;

    [Tooltip("Fallback portrait for NPCs without a specific sprite.")]
    public Sprite defaultNpcSprite;

    [Header("Transition")]
    public float fadeDuration = 0.25f;

    [Header("Audio Routing")]
    [Tooltip("Assign the Sounds mixer group here.")]
    public AudioMixerGroup soundsOutputGroup;

    [Tooltip("Optional AudioSource. One is created automatically if empty.")]
    public AudioSource voiceSource;

    [Header("Portrait State")]
    [Tooltip("Use Assyla's no-hat portrait.")]
    public bool useAssylaNoHatPortrait = false;

    private Controls controls;
    private bool interactInput;

    private Dictionary<string, DialogTree> allTrees;
    private DialogTree currentTree;
    private DialogNode currentNode;
    private string currentKey;
    private string currentTreeName;

    private Graphic[] graphicsUnderPanel;
    private bool isFading;

    public bool dialogueStart = false;

    public Action<string, string> eventDialogueChanged;

    private void Awake()
    {
        controls = new Controls();

        controls.Player.Interact.performed +=
            context => interactInput = true;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
            EnsureVoiceSourceConfigured();
        }
        else
        {
            CopySceneReferencesToInstance();

            Instance.Initialize();
            Instance.EnsureVoiceSourceConfigured();

            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        controls?.Enable();
    }

    private void OnDisable()
    {
        controls?.Disable();
    }

    private void CopySceneReferencesToInstance()
    {
        Instance.jsonFile = jsonFile;

        Instance.dialogPanel = dialogPanel;
        Instance.speakerIcon = speakerIcon;
        Instance.npcText = npcText;
        Instance.playerText = playerText;

        Instance.momCatSprite = momCatSprite;
        Instance.kittyMomVoice = kittyMomVoice;

        Instance.kittySprite = kittySprite;
        Instance.kittyVoice = kittyVoice;

        Instance.tireSprite = tireSprite;
        Instance.ydnaSprite = ydnaSprite;
        Instance.oliverSprite = oliverSprite;
        Instance.groupFriendsSprite = groupFriendsSprite;
        Instance.friendsVoice = friendsVoice;

        Instance.crowSprite = crowSprite;
        Instance.crowVoice = crowVoice;

        Instance.assylaSprite = assylaSprite;
        Instance.assylaNoHatSprite = assylaNoHatSprite;

        Instance.johnDanielSprite = johnDanielSprite;
        Instance.johnDanielVoice = johnDanielVoice;

        Instance.mangleSprite = mangleSprite;
        Instance.nSprite = nSprite;
        Instance.chicaSprite = chicaSprite;
        Instance.oyenSprite = oyenSprite;

        Instance.mousieSprite = mousieSprite;
        Instance.mousieVoice = mousieVoice;

        Instance.defaultNpcSprite = defaultNpcSprite;
        Instance.fadeDuration = fadeDuration;

        if (soundsOutputGroup != null)
            Instance.soundsOutputGroup = soundsOutputGroup;

        if (voiceSource != null)
            Instance.voiceSource = voiceSource;

        Instance.useAssylaNoHatPortrait =
            useAssylaNoHatPortrait;
    }

    private void EnsureVoiceSourceConfigured()
    {
        if (voiceSource == null)
        {
            voiceSource = GetComponent<AudioSource>();

            if (voiceSource == null)
                voiceSource = gameObject.AddComponent<AudioSource>();
        }

        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.dopplerLevel = 0f;
        voiceSource.rolloffMode = AudioRolloffMode.Linear;

        if (soundsOutputGroup != null)
        {
            voiceSource.outputAudioMixerGroup =
                soundsOutputGroup;
        }
        else
        {
            Debug.LogWarning(
                "[SimpleDialogManager] Sounds mixer group is not assigned."
            );
        }
    }

    public void Initialize()
    {
        if (jsonFile == null)
        {
            Debug.LogError(
                "[SimpleDialogManager] jsonFile is not assigned."
            );

            return;
        }

        if (dialogPanel == null)
        {
            Debug.LogError(
                "[SimpleDialogManager] dialogPanel is not assigned."
            );

            return;
        }

        if (allTrees != null)
            allTrees.Clear();

        allTrees =
            JsonConvert.DeserializeObject
            <Dictionary<string, DialogTree>>(
                jsonFile.text
            );

        graphicsUnderPanel =
            dialogPanel.GetComponentsInChildren<Graphic>(true);

        SetDialogGraphicsAlpha(0f);

        isFading = false;
        dialogPanel.SetActive(false);
    }

    private void Update()
    {
        if (dialogPanel == null ||
            !dialogPanel.activeSelf ||
            isFading)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space) ||
            interactInput)
        {
            interactInput = false;
            OnContinuePressed();
        }
    }

    public void StartDialogue(string sceneID)
    {
        if (allTrees == null ||
            !allTrees.ContainsKey(sceneID))
        {
            Debug.LogWarning(
                $"[SimpleDialogManager] No dialogue data for sceneID '{sceneID}'."
            );

            return;
        }

        // Clear any interaction input left from before the dialogue.
        interactInput = false;

        // Hide the entire Quest Canvas before dialogue appears.
        QuestManager.instance?.NotifyDialogueStarted();

        currentTreeName = sceneID;
        currentTree = allTrees[sceneID];
        currentKey = currentTree.start;
        currentNode = currentTree.nodes[currentKey];

        StartCoroutine(FadePanel(0f, 1f));

        dialogueStart = true;

        eventDialogueChanged?.Invoke(
            currentTreeName,
            currentKey
        );

        ShowCurrentNode();
    }

    private void ShowCurrentNode()
    {
        NPCVoiceScriptable voice = null;

        switch (currentNode.speaker)
        {
            case "Mom Cat":
                speakerIcon.sprite = momCatSprite;
                voice = kittyMomVoice;
                break;

            case "Kitty":
                speakerIcon.sprite = kittySprite;
                voice = kittyVoice;
                break;

            case "Tire":
                speakerIcon.sprite = tireSprite;
                voice = friendsVoice;
                break;

            case "Ydna":
                speakerIcon.sprite = ydnaSprite;
                voice = friendsVoice;
                break;

            case "Oliver":
                speakerIcon.sprite = oliverSprite;
                voice = friendsVoice;
                break;

            case "Ydna, Tire, Oliver":
                speakerIcon.sprite = groupFriendsSprite;
                voice = friendsVoice;
                break;

            case "The Crow":
                speakerIcon.sprite = crowSprite;
                voice = crowVoice;
                break;

            case "Assyla":
                speakerIcon.sprite =
                    useAssylaNoHatPortrait &&
                    assylaNoHatSprite != null
                        ? assylaNoHatSprite
                        : assylaSprite;

                voice = friendsVoice;
                break;

            case "Assyla (No Hat)":
            case "Assyla (no hat)":
            case "Assyla no hat":
            case "Assyla_NoHat":
            case "AssylaNoHat":
                speakerIcon.sprite =
                    assylaNoHatSprite != null
                        ? assylaNoHatSprite
                        : assylaSprite;

                voice = friendsVoice;
                break;

            case "John Daniel":
                speakerIcon.sprite = johnDanielSprite;
                voice = johnDanielVoice;
                break;

            case "Mangle":
                speakerIcon.sprite = mangleSprite;
                voice = friendsVoice;
                break;

            case "N":
                speakerIcon.sprite = nSprite;
                voice = friendsVoice;
                break;

            case "Chica":
                speakerIcon.sprite = chicaSprite;
                voice = friendsVoice;
                break;

            case "Oyen":
                speakerIcon.sprite = oyenSprite;
                voice = friendsVoice;
                break;

            case "Mousie":
                speakerIcon.sprite = mousieSprite;
                voice = mousieVoice;
                break;

            case "Gravestone":
                speakerIcon.sprite = groupFriendsSprite;
                break;

            default:
                speakerIcon.sprite = defaultNpcSprite;
                break;
        }

        PlayVoiceFromScriptable(voice);

        npcText.text = string.Empty;
        playerText.text = string.Empty;

        if (currentNode.speaker == "Kitty")
            playerText.text = currentNode.text;
        else
            npcText.text = currentNode.text;
    }

    private void PlayVoiceFromScriptable(
        NPCVoiceScriptable voice
    )
    {
        if (voice == null)
            return;

        AudioClip clip = TryGetRandomAudioClip(voice);

        if (clip == null)
            return;

        if (voiceSource == null)
            EnsureVoiceSourceConfigured();

        if (soundsOutputGroup != null &&
            voiceSource.outputAudioMixerGroup !=
            soundsOutputGroup)
        {
            voiceSource.outputAudioMixerGroup =
                soundsOutputGroup;
        }

        voiceSource.PlayOneShot(clip);
    }

    private AudioClip TryGetRandomAudioClip(
        object scriptable
    )
    {
        if (scriptable == null)
            return null;

        Type type = scriptable.GetType();

        MethodInfo[] candidateMethods =
        {
            type.GetMethod(
                "GetRandomVoiceClip",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            ),

            type.GetMethod(
                "GetRandomClip",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            )
        };

        foreach (MethodInfo method in candidateMethods)
        {
            if (method == null ||
                method.ReturnType != typeof(AudioClip))
            {
                continue;
            }

            try
            {
                AudioClip result =
                    method.Invoke(
                        scriptable,
                        null
                    ) as AudioClip;

                if (result != null)
                    return result;
            }
            catch
            {
                // Try the next method.
            }
        }

        string[] names =
        {
            "clips",
            "voiceClips",
            "audioClips",
            "samples",
            "sounds"
        };

        foreach (string name in names)
        {
            FieldInfo field = type.GetField(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (field != null)
            {
                AudioClip clip =
                    PickFromObjectAsClipCollection(
                        field.GetValue(scriptable)
                    );

                if (clip != null)
                    return clip;
            }
        }

        foreach (string name in names)
        {
            PropertyInfo property = type.GetProperty(
                name,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (property != null &&
                property.CanRead)
            {
                AudioClip clip =
                    PickFromObjectAsClipCollection(
                        property.GetValue(
                            scriptable,
                            null
                        )
                    );

                if (clip != null)
                    return clip;
            }
        }

        return null;
    }

    private AudioClip PickFromObjectAsClipCollection(
        object value
    )
    {
        if (value == null)
            return null;

        if (value is AudioClip[] clips &&
            clips.Length > 0)
        {
            int index = UnityEngine.Random.Range(
                0,
                clips.Length
            );

            return clips[index];
        }

        Type valueType = value.GetType();

        if (!valueType.IsGenericType ||
            !typeof(IEnumerable).IsAssignableFrom(valueType))
        {
            return null;
        }

        Type genericDefinition =
            valueType.GetGenericTypeDefinition();

        if (genericDefinition != typeof(List<>))
            return null;

        Type argument =
            valueType.GetGenericArguments()[0];

        if (argument != typeof(AudioClip))
            return null;

        PropertyInfo countProperty =
            valueType.GetProperty("Count");

        PropertyInfo indexer =
            valueType.GetProperty("Item");

        if (countProperty == null ||
            indexer == null)
        {
            return null;
        }

        int count = (int)countProperty.GetValue(
            value,
            null
        );

        if (count <= 0)
            return null;

        int randomIndex =
            UnityEngine.Random.Range(0, count);

        return indexer.GetValue(
            value,
            new object[] { randomIndex }
        ) as AudioClip;
    }

    private void OnContinuePressed()
    {
        if (PauseMenu.isPaused)
            return;

        if (currentNode == null ||
            string.IsNullOrEmpty(currentNode.next))
        {
            eventDialogueChanged?.Invoke(
                currentTreeName,
                null
            );

            CloseDialogue();
            return;
        }

        string nextKey = currentNode.next;

        if (!currentTree.nodes.ContainsKey(nextKey))
        {
            Debug.LogError(
                $"[SimpleDialogManager] Node '{nextKey}' not found."
            );

            CloseDialogue();
            return;
        }

        currentKey = nextKey;
        currentNode = currentTree.nodes[nextKey];

        dialogueStart = false;

        eventDialogueChanged?.Invoke(
            currentTreeName,
            currentKey
        );

        ShowCurrentNode();
    }

    private void CloseDialogue()
    {
        StartCoroutine(FadePanel(1f, 0f));

        currentNode = null;
        currentTree = null;
        currentKey = null;
    }

    private IEnumerator FadePanel(
        float from,
        float to
    )
    {
        isFading = true;
        dialogPanel.SetActive(true);

        float duration = Mathf.Max(0f, fadeDuration);

        if (duration > 0f)
        {
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress = Mathf.Clamp01(
                    elapsedTime / duration
                );

                float alpha = Mathf.Lerp(
                    from,
                    to,
                    progress
                );

                SetDialogGraphicsAlpha(alpha);

                yield return null;
            }
        }

        SetDialogGraphicsAlpha(to);

        if (to <= 0f)
        {
            dialogPanel.SetActive(false);

            // The dialogue has completely finished.
            // The first dialogue makes the Quest Canvas
            // visible for the first time.
            QuestManager.instance?.NotifyDialogueFinished();
        }

        isFading = false;
    }

    private void SetDialogGraphicsAlpha(float alpha)
    {
        if (graphicsUnderPanel == null)
            return;

        foreach (Graphic graphic in graphicsUnderPanel)
        {
            if (graphic == null)
                continue;

            Color color = graphic.color;
            color.a = alpha;
            graphic.color = color;
        }
    }

    public void SetAssylaHatless(bool value)
    {
        useAssylaNoHatPortrait = value;
    }
}