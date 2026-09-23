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
    [Tooltip("How long the minigame takes to fade in when there is NO tutorial.")]
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
    private CanvasGroup[] keyCanvasGroups;

    private RectTransform panelRectTransform;
    private Vector2 panelOriginalPosition;

    private Coroutine shakeCoroutine;
    private Coroutine fadeCoroutine;
    private Coroutine prewarmCoroutine;


    private float keyTimer;
    private float currentKeySpawnInterval;


    private int losses;
    private int keysLeft;
    private int activeKeys;


    private bool gameActive;
    private bool minigameReady;
    private bool waitingForTutorial;

    // True once all of the expensive/reset work has
    // already been done for the upcoming attempt.
    private bool minigamePrepared;


    private PlayerMovement cachedPlayerMovement;


    private void Awake()
    {
        // =====================================
        // MINIGAME PANEL
        // =====================================

        if (panelObject != null)
        {
            minigameCanvasGroup =
                panelObject.GetComponent<CanvasGroup>();

            if (minigameCanvasGroup == null)
            {
                minigameCanvasGroup =
                    panelObject.AddComponent<CanvasGroup>();
            }

            minigameCanvasGroup.alpha = 0f;


            panelRectTransform =
                panelObject.GetComponent<RectTransform>();

            if (panelRectTransform != null)
            {
                panelOriginalPosition =
                    panelRectTransform.anchoredPosition;
            }


            panelObject.SetActive(false);
        }


        // =====================================
        // CACHE KEY CANVAS GROUPS
        // =====================================
        //
        // We do this here instead of when the tutorial
        // closes, so Unity does not need to GetComponent /
        // AddComponent for every key during the transition.
        // =====================================

        if (keyPool != null)
        {
            keyCanvasGroups =
                new CanvasGroup[keyPool.Length];

            for (int i = 0; i < keyPool.Length; i++)
            {
                if (keyPool[i] == null)
                    continue;

                CanvasGroup canvasGroup =
                    keyPool[i].GetComponent<CanvasGroup>();

                if (canvasGroup == null)
                {
                    canvasGroup =
                        keyPool[i]
                            .gameObject
                            .AddComponent<CanvasGroup>();
                }

                keyCanvasGroups[i] =
                    canvasGroup;

                canvasGroup.alpha =
                    1f;

                keyPool[i]
                    .gameObject
                    .SetActive(false);
            }
        }
    }


    private void Start()
    {
        // =====================================
        // DIALOGUE
        // =====================================

        SimpleDialogManager dm =
            SimpleDialogManager.Instance;

        if (dm != null)
        {
            dm.eventDialogueChanged +=
                OnDialogueAdvance;
        }


        // =====================================
        // CACHE PLAYER MOVEMENT
        // =====================================

        CachePlayerMovement();


        // =====================================
        // PRELOAD AUDIO
        // =====================================
        //
        // Helps prevent the first dance-music switch
        // from causing a little hitch.
        // =====================================

        if (danceMusic != null)
            danceMusic.LoadAudioData();

        if (normalMusic != null)
            normalMusic.LoadAudioData();

        if (clickClip != null)
            clickClip.LoadAudioData();
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


    private void CachePlayerMovement()
    {
        if (cachedPlayerMovement != null)
            return;

        if (PlayerManager.instance == null ||
            PlayerManager.instance.player == null)
        {
            return;
        }

        cachedPlayerMovement =
            PlayerManager.instance.player
                .GetComponent<PlayerMovement>();
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
            waitingForTutorial =
                true;


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


            // Show the tutorial FIRST.
            danceTutorial.ShowTutorial(
                OnDanceTutorialFinished
            );


            // Prepare the minigame while the player
            // is looking at the tutorial.
            //
            // This removes the hitch between tutorial
            // and minigame.
            if (prewarmCoroutine != null)
            {
                StopCoroutine(
                    prewarmCoroutine
                );
            }

            prewarmCoroutine =
                StartCoroutine(
                    PrewarmMinigameDuringTutorial()
                );


            return;
        }


        // =====================================
        // RETRY -> NO TUTORIAL
        // =====================================

        StartMinigame(false);
    }


    private IEnumerator PrewarmMinigameDuringTutorial()
    {
        // Give the tutorial one frame to appear first.
        yield return null;


        PrepareMinigame();


        // =====================================
        // KEEP MINIGAME UNDER THE TUTORIAL
        // =====================================
        //
        // The minigame is already rendered behind
        // the tutorial.
        //
        // When the tutorial fades away, there is
        // therefore NOTHING blank in between.
        // =====================================

        if (panelObject != null)
        {
            panelObject.SetActive(
                true
            );


            if (minigameCanvasGroup != null)
            {
                minigameCanvasGroup.alpha =
                    1f;
            }


            // Force Unity to build the UI layout NOW,
            // while the tutorial is still visible.
            Canvas.ForceUpdateCanvases();
        }


        prewarmCoroutine =
            null;
    }


    private void OnDanceTutorialFinished()
    {
        waitingForTutorial =
            false;


        /*
         * Minigame has already been prepared
         * underneath the tutorial.
         *
         * Therefore all we need to do here is
         * activate gameplay.
         */
        StartMinigame(true);
    }


    private void PrepareMinigame()
    {
        if (minigamePrepared)
            return;


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


        // =====================================
        // RESET PANEL
        // =====================================

        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition =
                panelOriginalPosition;
        }


        // =====================================
        // RESET KEYS
        // =====================================

        if (keyPool != null)
        {
            for (int i = 0;
                 i < keyPool.Length;
                 i++)
            {
                if (keyPool[i] == null)
                    continue;


                keyPool[i].shrinkRate =
                    currentAreaShrinkSpeed;


                keyPool[i].OnReset();


                if (keyCanvasGroups != null &&
                    i < keyCanvasGroups.Length &&
                    keyCanvasGroups[i] != null)
                {
                    keyCanvasGroups[i].alpha =
                        1f;
                }


                keyPool[i]
                    .gameObject
                    .SetActive(false);
            }
        }


        // =====================================
        // RESET LIVES
        // =====================================

        if (lives != null)
        {
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
        }


        minigamePrepared =
            true;
    }


    private void StartMinigame(
        bool comingFromTutorial)
    {
        if (gameActive)
            return;


        // If the tutorial was closed extremely quickly
        // before prewarming finished, make sure everything
        // is prepared now.
        if (!minigamePrepared)
        {
            PrepareMinigame();
        }


        gameActive =
            true;


        minigameReady =
            false;


        // =====================================
        // PLAYER
        // =====================================

        CachePlayerMovement();


        if (cachedPlayerMovement != null)
        {
            cachedPlayerMovement.enabled =
                false;
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
        // SHOW MINIGAME
        // =====================================

        if (panelObject != null)
        {
            panelObject.SetActive(
                true
            );


            if (comingFromTutorial)
            {
                /*
                 * IMPORTANT:
                 *
                 * The minigame was already visible
                 * BEHIND the tutorial.
                 *
                 * Do NOT reset alpha to 0 here.
                 *
                 * Doing so would create exactly the
                 * blank lag / flash we are trying
                 * to remove.
                 */

                if (minigameCanvasGroup != null)
                {
                    minigameCanvasGroup.alpha =
                        1f;
                }


                minigameReady =
                    true;


                keyTimer =
                    currentKeySpawnInterval;
            }
            else
            {
                // Retry / no tutorial:
                // use normal smooth fade-in.

                if (fadeCoroutine != null)
                {
                    StopCoroutine(
                        fadeCoroutine
                    );
                }


                fadeCoroutine =
                    StartCoroutine(
                        FadeInMinigame()
                    );
            }
        }
        else
        {
            minigameReady =
                true;


            keyTimer =
                currentKeySpawnInterval;
        }
    }


    private IEnumerator FadeInMinigame()
    {
        if (minigameCanvasGroup == null)
        {
            minigameReady =
                true;


            keyTimer =
                currentKeySpawnInterval;


            fadeCoroutine =
                null;


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


            keyTimer =
                currentKeySpawnInterval;


            fadeCoroutine =
                null;


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


        fadeCoroutine =
            null;
    }


    private void EndMinigame(
        bool win)
    {
        gameActive =
            false;


        minigameReady =
            false;


        minigamePrepared =
            false;


        // =====================================
        // STOP FADE
        // =====================================

        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );


            fadeCoroutine =
                null;
        }


        // =====================================
        // STOP SHAKE
        // =====================================

        if (shakeCoroutine != null)
        {
            StopCoroutine(
                shakeCoroutine
            );


            shakeCoroutine =
                null;
        }


        // =====================================
        // RESTORE PANEL POSITION
        // =====================================

        if (panelRectTransform != null)
        {
            panelRectTransform.anchoredPosition =
                panelOriginalPosition;
        }


        // =====================================
        // HIDE MINIGAME
        // =====================================

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

        CachePlayerMovement();


        if (cachedPlayerMovement != null)
        {
            cachedPlayerMovement.enabled =
                true;
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