using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class DanceMinigame : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private Interactable interactable;
    [SerializeField] private DialogueOnInteract assyla;

    [SerializeField] private GameObject panelObject;
    [SerializeField] private GameObject canvasObject;

    [SerializeField] private DanceKey[] keyPool;
    [SerializeField] private GameObject[] lives;

    [SerializeField] private GameObject hatObject;


    [Header("Audio")]

    [SerializeField] private AudioSource musicAudio;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip danceMusic;
    [SerializeField] private AudioClip normalMusic;
    [SerializeField] private AudioMixerGroup mix;


    [Header("Tutorial")]

    [SerializeField] private DanceTutorial danceTutorial;


    [Header("Minigame Appearance")]

    [Tooltip("How long the entire dance minigame takes to fade in.")]
    public float minigameFadeDuration = 0.5f;


    [Header("Minigame Settings")]

    [Tooltip("How many keys the player must complete.")]
    public int startingKeys = 20;

    [Tooltip("How fast the shrinking Current Area moves. Bigger = faster.")]
    public float currentAreaShrinkSpeed = 50f;


    [Header("Key Spawn Speed")]

    [Tooltip("Seconds between keys at the beginning.")]
    public float startingKeySpawnInterval = 1.5f;

    [Tooltip("Fastest possible interval between keys.")]
    public float minimumKeySpawnInterval = 0.6f;

    [Tooltip("How much faster spawning becomes after each spawned key.")]
    public float spawnSpeedIncrease = 0.05f;


    [Header("Key Appearance")]

    [Tooltip("How long each individual key takes to fade in.")]
    public float keyAppearDuration = 0.2f;


    [Header("Key Placement")]

    [Tooltip("Minimum distance between active keys.")]
    public float minimumKeyDistance = 250f;

    [Tooltip("How many positions are checked before waiting for more space.")]
    public int positionSearchAttempts = 50;


    [Header("Screen Border")]

    [Range(0.1f, 0.5f)]
    public float horizontalSpawnArea = 0.35f;

    [Range(0.1f, 0.5f)]
    public float verticalSpawnArea = 0.30f;


    [Header("Screen Shake")]

    [Tooltip("How long the minigame UI shakes when a life is lost.")]
    public float shakeDuration = 0.2f;

    [Tooltip("How strong the shake is.")]
    public float shakeStrength = 15f;


    private CanvasGroup minigameCanvasGroup;

    private RectTransform panelRectTransform;
    private Vector2 panelOriginalPosition;

    private Coroutine shakeCoroutine;


    private float keyTimer;
    private float currentKeySpawnInterval;


    private int losses;
    private int keysLeft;
    private int activeKeys;


    private bool gameActive;
    private bool minigameReady;
    private bool waitingForTutorial;


    private void Awake()
    {
        if (panelObject != null)
        {
            // CanvasGroup for fading the whole minigame.
            minigameCanvasGroup =
                panelObject.GetComponent<CanvasGroup>();


            if (minigameCanvasGroup == null)
            {
                minigameCanvasGroup =
                    panelObject.AddComponent<CanvasGroup>();
            }


            minigameCanvasGroup.alpha =
                0f;


            // RectTransform for screen shake.
            panelRectTransform =
                panelObject.GetComponent<RectTransform>();


            if (panelRectTransform != null)
            {
                panelOriginalPosition =
                    panelRectTransform.anchoredPosition;
            }


            panelObject.SetActive(false);
        }
    }


    private void Start()
    {
        SimpleDialogManager dm =
            SimpleDialogManager.Instance;


        if (dm != null)
        {
            dm.eventDialogueChanged +=
                OnDialogueAdvance;
        }
    }


    private void OnDestroy()
    {
        SimpleDialogManager dm =
            SimpleDialogManager.Instance;


        if (dm != null)
        {
            dm.eventDialogueChanged -=
                OnDialogueAdvance;
        }
    }


    private void OnDialogueAdvance(
        string sceneID,
        string currentKey)
    {
        if ((sceneID == "AssylaDance" &&
             currentKey == null) ||
            (sceneID == "AssylaRestart" &&
             currentKey == null))
        {
            RequestStartMinigame();
        }
    }


    private void RequestStartMinigame()
    {
        if (gameActive)
            return;


        if (waitingForTutorial)
            return;


        // =====================================
        // FIRST ATTEMPT -> TUTORIAL
        // =====================================

        if (danceTutorial != null &&
            !danceTutorial.HasShownTutorial)
        {
            waitingForTutorial = true;


            // Disable Assyla interaction immediately.
            if (interactable != null)
            {
                interactable.enabled =
                    false;
            }


            if (assyla != null)
            {
                assyla.SetExternallyHidden(
                    true
                );
            }


            danceTutorial.ShowTutorial(
                OnDanceTutorialFinished
            );


            return;
        }


        // Retry -> skip tutorial.
        StartMinigame();
    }


    private void OnDanceTutorialFinished()
    {
        waitingForTutorial =
            false;


        /*
         * Called the moment F is pressed.
         * Tutorial fades OUT while
         * minigame fades IN.
         */
        StartMinigame();
    }


    private void StartMinigame()
    {
        if (gameActive)
            return;


        gameActive =
            true;


        minigameReady =
            false;


        // =====================================
        // PLAYER
        // =====================================

        if (PlayerManager.instance != null &&
            PlayerManager.instance.player != null)
        {
            PlayerMovement movement =
                PlayerManager.instance.player
                    .GetComponent<PlayerMovement>();


            if (movement != null)
            {
                movement.enabled =
                    false;
            }
        }


        // =====================================
        // ASSYLA
        // =====================================

        if (interactable != null)
        {
            interactable.enabled =
                false;
        }


        if (assyla != null)
        {
            assyla.SetExternallyHidden(
                true
            );
        }


        // =====================================
        // QUEST UI
        // =====================================

        if (QuestManager.instance != null)
        {
            QuestManager.instance
                .SetUIEnabled(false);
        }


        // =====================================
        // RESET GAME
        // =====================================

        losses =
            0;


        keysLeft =
            startingKeys;


        activeKeys =
            0;


        currentKeySpawnInterval =
            startingKeySpawnInterval;


        keyTimer =
            currentKeySpawnInterval;


        // Reset panel position in case shake
        // was interrupted previously.
        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition =
                panelOriginalPosition;
        }


        // =====================================
        // RESET KEYS
        // =====================================

        for (int i = 0;
             i < keyPool.Length;
             i++)
        {
            if (keyPool[i] == null)
                continue;


            keyPool[i].shrinkRate =
                currentAreaShrinkSpeed;


            keyPool[i].OnReset();


            CanvasGroup keyCanvas =
                keyPool[i]
                    .GetComponent<CanvasGroup>();


            if (keyCanvas == null)
            {
                keyCanvas =
                    keyPool[i]
                        .gameObject
                        .AddComponent<CanvasGroup>();
            }


            keyCanvas.alpha =
                1f;


            keyPool[i]
                .gameObject
                .SetActive(false);
        }


        // =====================================
        // RESET LIVES
        // =====================================

        for (int i = 0;
             i < lives.Length;
             i++)
        {
            if (lives[i] != null)
            {
                lives[i]
                    .SetActive(true);
            }
        }


        // =====================================
        // MUSIC
        // =====================================

        if (musicAudio != null &&
            danceMusic != null)
        {
            SafePlay(
                musicAudio,
                danceMusic,
                false
            );
        }


        // =====================================
        // SHOW + FADE MINIGAME IN
        // =====================================

        if (panelObject != null)
        {
            panelObject.SetActive(
                true
            );


            StartCoroutine(
                FadeInMinigame()
            );
        }
        else
        {
            minigameReady =
                true;
        }
    }


    private IEnumerator FadeInMinigame()
    {
        if (minigameCanvasGroup == null)
        {
            minigameReady =
                true;


            yield break;
        }


        minigameCanvasGroup.alpha =
            0f;


        float duration =
            Mathf.Max(
                0f,
                minigameFadeDuration
            );


        if (duration <= 0f)
        {
            minigameCanvasGroup.alpha =
                1f;


            minigameReady =
                true;


            yield break;
        }


        float timer =
            0f;


        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float progress =
                Mathf.Clamp01(
                    timer /
                    duration
                );


            minigameCanvasGroup.alpha =
                progress;


            yield return null;
        }


        minigameCanvasGroup.alpha =
            1f;


        minigameReady =
            true;


        keyTimer =
            currentKeySpawnInterval;
    }


    private void EndMinigame(
        bool win)
    {
        gameActive =
            false;


        minigameReady =
            false;


        // Stop shake if one is currently happening.
        if (shakeCoroutine != null)
        {
            StopCoroutine(
                shakeCoroutine
            );


            shakeCoroutine =
                null;
        }


        // Restore panel position.
        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition =
                panelOriginalPosition;
        }


        if (panelObject != null)
        {
            panelObject.SetActive(
                false
            );
        }


        if (minigameCanvasGroup != null)
        {
            minigameCanvasGroup.alpha =
                0f;
        }


        // =====================================
        // RESTORE PLAYER
        // =====================================

        if (PlayerManager.instance != null &&
            PlayerManager.instance.player != null)
        {
            PlayerMovement movement =
                PlayerManager.instance.player
                    .GetComponent<PlayerMovement>();


            if (movement != null)
            {
                movement.enabled =
                    true;
            }
        }


        // =====================================
        // QUEST UI
        // =====================================

        if (QuestManager.instance != null)
        {
            QuestManager.instance
                .SetUIEnabled(true);
        }


        // =====================================
        // ASSYLA
        // =====================================

        if (interactable != null)
        {
            interactable.enabled =
                true;
        }


        if (assyla != null)
        {
            assyla.SetExternallyHidden(
                false
            );
        }


        // =====================================
        // MUSIC
        // =====================================

        if (musicAudio != null &&
            normalMusic != null)
        {
            SafePlay(
                musicAudio,
                normalMusic,
                false
            );
        }


        SimpleDialogManager dm =
            SimpleDialogManager.Instance;


        // =====================================
        // WIN
        // =====================================

        if (win)
        {
            if (dm != null)
            {
                dm.SetAssylaHatless(
                    true
                );


                dm.StartDialogue(
                    "AssylaGive"
                );
            }


            if (assyla != null)
            {
                assyla.sceneId.value =
                    "AssylaThank";
            }


            if (hatObject != null)
            {
                hatObject.SetActive(
                    false
                );
            }
        }

        // =====================================
        // LOSE
        // =====================================

        else
        {
            if (dm != null)
            {
                dm.StartDialogue(
                    "AssylaBetterLuck"
                );
            }


            if (assyla != null)
            {
                assyla.sceneId.value =
                    "AssylaRestart";
            }
        }
    }


    private void FixedUpdate()
    {
        if (!gameActive ||
            !minigameReady)
        {
            return;
        }


        // =====================================
        // SPAWNING
        // =====================================

        keyTimer -=
            Time.deltaTime;


        if (keyTimer <= 0f)
        {
            bool spawned =
                SpawnKey();


            if (spawned)
            {
                currentKeySpawnInterval -=
                    spawnSpeedIncrease;


                currentKeySpawnInterval =
                    Mathf.Max(
                        currentKeySpawnInterval,
                        minimumKeySpawnInterval
                    );
            }


            keyTimer =
                currentKeySpawnInterval;
        }


        // =====================================
        // UPDATE ACTIVE KEYS
        // =====================================

        for (int i = 0;
             i < keyPool.Length;
             i++)
        {
            if (keyPool[i] == null)
                continue;


            if (!keyPool[i]
                .gameObject
                .activeInHierarchy)
            {
                continue;
            }


            keyPool[i].shrinkRate =
                currentAreaShrinkSpeed;


            keyPool[i].OnUpdate(
                Time.deltaTime
            );


            if (keyPool[i]
                .InFailZone())
            {
                keyPool[i]
                    .gameObject
                    .SetActive(false);


                activeKeys--;


                FailKey();
            }
        }
    }


    private void Update()
    {
        if (!gameActive ||
            !minigameReady)
        {
            return;
        }


        /*
         * WASD is completely ignored
         * if there is no visible dance key.
         */
        if (!HasVisibleActiveKey())
        {
            return;
        }


        KeyCode pressedKey =
            KeyCode.None;


        // =====================================
        // DETECT WASD
        // =====================================

        for (int i = 0;
             i < DanceKey.randomKeyList.Length;
             i++)
        {
            if (Input.GetKeyDown(
                DanceKey.randomKeyList[i]
            ))
            {
                pressedKey =
                    DanceKey.randomKeyList[i];


                break;
            }
        }


        if (pressedKey ==
            KeyCode.None)
        {
            return;
        }


        DanceKey latestKey =
            null;


        float keyScale =
            float.MaxValue;


        bool success =
            false;


        // =====================================
        // CHECK KEYS
        // =====================================

        for (int i = 0;
             i < keyPool.Length;
             i++)
        {
            DanceKey key =
                keyPool[i];


            if (!IsKeyVisible(key))
            {
                continue;
            }


            if (key.GetScale() <
                keyScale)
            {
                latestKey =
                    key;


                keyScale =
                    key.GetScale();
            }


            if (key.reqKey !=
                pressedKey)
            {
                continue;
            }


            if (key.InSuccessZone())
            {
                key.gameObject
                    .SetActive(false);


                success =
                    true;


                break;
            }
        }


        // =====================================
        // RESULT
        // =====================================

        if (success)
        {
            WinKey();
        }
        else
        {
            if (latestKey != null)
            {
                latestKey
                    .gameObject
                    .SetActive(false);


                activeKeys--;
            }


            FailKey();
        }
    }


    private bool HasVisibleActiveKey()
    {
        for (int i = 0;
             i < keyPool.Length;
             i++)
        {
            if (IsKeyVisible(
                keyPool[i]
            ))
            {
                return true;
            }
        }


        return false;
    }


    private bool IsKeyVisible(
        DanceKey key)
    {
        if (key == null)
            return false;


        if (!key.gameObject
            .activeInHierarchy)
        {
            return false;
        }


        CanvasGroup canvasGroup =
            key.GetComponent<CanvasGroup>();


        if (canvasGroup == null)
        {
            return true;
        }


        return canvasGroup.alpha >
               0.05f;
    }


    private void FailKey()
    {
        losses++;


        // =====================================
        // SCREEN SHAKE
        // =====================================

        if (shakeCoroutine != null)
        {
            StopCoroutine(
                shakeCoroutine
            );


            shakeCoroutine =
                null;


            if (panelRectTransform != null)
            {
                panelRectTransform.anchoredPosition =
                    panelOriginalPosition;
            }
        }


        shakeCoroutine =
            StartCoroutine(
                ShakeScreen()
            );


        // =====================================
        // FAIL SOUND
        // =====================================

        if (ObjectPool.instance != null)
        {
            GameObject spObject =
                ObjectPool.instance
                    .objPool_GetObject(
                        "2DSoundPlayer"
                    );


            if (spObject != null)
            {
                SoundPlayer splr =
                    spObject
                        .GetComponent<SoundPlayer>();


                if (splr != null)
                {
                    PlaySoundInfo soundInfo =
                        new PlaySoundInfo();


                    soundInfo.clip =
                        clickClip;


                    soundInfo.volume =
                        1f;


                    soundInfo.pitch =
                        0.5f;


                    soundInfo.mixer =
                        mix;


                    splr.PlaySound(
                        soundInfo
                    );
                }
            }
        }


        // =====================================
        // LIVES
        // =====================================

        for (int i = 0;
             i < lives.Length;
             i++)
        {
            if (lives[i] == null)
                continue;


            lives[i].SetActive(
                i <
                lives.Length -
                losses
            );
        }


        if (losses >=
            lives.Length)
        {
            EndMinigame(
                false
            );
        }
        else if (activeKeys == 0 &&
                 keysLeft == 0)
        {
            EndMinigame(
                true
            );
        }
    }


    private IEnumerator ShakeScreen()
    {
        if (panelRectTransform == null)
        {
            shakeCoroutine =
                null;


            yield break;
        }


        float timer =
            0f;


        while (timer <
               shakeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float x =
                Random.Range(
                    -shakeStrength,
                    shakeStrength
                );


            float y =
                Random.Range(
                    -shakeStrength,
                    shakeStrength
                );


            panelRectTransform.anchoredPosition =
                panelOriginalPosition +
                new Vector2(
                    x,
                    y
                );


            yield return null;
        }


        panelRectTransform.anchoredPosition =
            panelOriginalPosition;


        shakeCoroutine =
            null;
    }


    public void WinKey()
    {
        activeKeys--;


        // =====================================
        // SUCCESS SOUND
        // =====================================

        if (ObjectPool.instance != null)
        {
            GameObject spObject =
                ObjectPool.instance
                    .objPool_GetObject(
                        "2DSoundPlayer"
                    );


            if (spObject != null)
            {
                SoundPlayer splr =
                    spObject
                        .GetComponent<SoundPlayer>();


                if (splr != null)
                {
                    PlaySoundInfo soundInfo =
                        new PlaySoundInfo();


                    soundInfo.clip =
                        clickClip;


                    soundInfo.volume =
                        1f;


                    soundInfo.pitch =
                        1f;


                    soundInfo.mixer =
                        mix;


                    splr.PlaySound(
                        soundInfo
                    );
                }
            }
        }


        if (activeKeys == 0 &&
            keysLeft == 0)
        {
            EndMinigame(
                true
            );
        }
    }


    private bool SpawnKey()
    {
        if (keysLeft <= 0)
        {
            return false;
        }


        // =====================================
        // FIND UNUSED KEY
        // =====================================

        DanceKey newKey =
            null;


        for (int i = 0;
             i < keyPool.Length;
             i++)
        {
            if (keyPool[i] == null)
                continue;


            if (!keyPool[i]
                .gameObject
                .activeInHierarchy)
            {
                newKey =
                    keyPool[i];


                break;
            }
        }


        if (newKey == null)
        {
            return false;
        }


        RectTransform newRect =
            newKey.GetComponent<RectTransform>();


        if (newRect == null)
        {
            Debug.LogWarning(
                "DanceKey needs a RectTransform."
            );


            return false;
        }


        RectTransform parentRect =
            newRect.parent
                as RectTransform;


        if (parentRect == null)
        {
            Debug.LogWarning(
                "DanceKey needs a RectTransform parent."
            );


            return false;
        }


        // =====================================
        // SPAWN AREA
        // =====================================

        float halfScreenX =
            parentRect.rect.width *
            horizontalSpawnArea;


        float halfScreenY =
            parentRect.rect.height *
            verticalSpawnArea;


        Vector2 chosenPosition =
            Vector2.zero;


        bool foundPosition =
            false;


        // =====================================
        // FIND FREE POSITION
        // =====================================

        for (int attempt = 0;
             attempt <
             positionSearchAttempts;
             attempt++)
        {
            Vector2 candidatePosition =
                new Vector2(
                    Random.Range(
                        -halfScreenX,
                        halfScreenX
                    ),
                    Random.Range(
                        -halfScreenY,
                        halfScreenY
                    )
                );


            bool positionIsFree =
                true;


            for (int i = 0;
                 i < keyPool.Length;
                 i++)
            {
                DanceKey otherKey =
                    keyPool[i];


                if (otherKey == null ||
                    otherKey == newKey)
                {
                    continue;
                }


                if (!otherKey
                    .gameObject
                    .activeInHierarchy)
                {
                    continue;
                }


                RectTransform otherRect =
                    otherKey
                        .GetComponent<RectTransform>();


                if (otherRect == null)
                    continue;


                float distance =
                    Vector2.Distance(
                        candidatePosition,
                        otherRect
                            .anchoredPosition
                    );


                if (distance <
                    minimumKeyDistance)
                {
                    positionIsFree =
                        false;


                    break;
                }
            }


            if (positionIsFree)
            {
                chosenPosition =
                    candidatePosition;


                foundPosition =
                    true;


                break;
            }
        }


        // No room -> wait.
        if (!foundPosition)
        {
            return false;
        }


        // =====================================
        // SPAWN
        // =====================================

        newRect.anchoredPosition =
            chosenPosition;


        newKey.gameObject
            .SetActive(true);


        newKey.shrinkRate =
            currentAreaShrinkSpeed;


        newKey.OnReset();


        StartCoroutine(
            FadeInKey(newKey)
        );


        activeKeys++;


        keysLeft--;


        return true;
    }


    private IEnumerator FadeInKey(
        DanceKey key)
    {
        CanvasGroup canvasGroup =
            key.GetComponent<CanvasGroup>();


        if (canvasGroup == null)
        {
            canvasGroup =
                key.gameObject
                    .AddComponent<CanvasGroup>();
        }


        canvasGroup.alpha =
            0f;


        if (keyAppearDuration <= 0f)
        {
            canvasGroup.alpha =
                1f;


            yield break;
        }


        float timer =
            0f;


        while (timer <
               keyAppearDuration)
        {
            if (key == null ||
                !key.gameObject
                    .activeInHierarchy)
            {
                yield break;
            }


            timer +=
                Time.deltaTime;


            canvasGroup.alpha =
                Mathf.Clamp01(
                    timer /
                    keyAppearDuration
                );


            yield return null;
        }


        if (key != null &&
            key.gameObject
                .activeInHierarchy)
        {
            canvasGroup.alpha =
                1f;
        }
    }


    private static void SafePlay(
        AudioSource src,
        AudioClip clip,
        bool restartIfSame)
    {
        if (src == null ||
            clip == null)
        {
            return;
        }


        if (src.clip == clip)
        {
            if (src.isPlaying &&
                !restartIfSame)
            {
                return;
            }


            if (!src.isPlaying &&
                !restartIfSame)
            {
                src.UnPause();


                if (!src.isPlaying)
                {
                    src.Play();
                }


                return;
            }
        }


        src.clip =
            clip;


        src.Play();
    }
}