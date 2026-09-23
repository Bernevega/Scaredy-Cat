using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Video;
using System;

public class MenuSetup : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The whole panel (title + company name) you want to slide")]
    public RectTransform uiElement;

    [Tooltip("The “Press any key” text")]
    public RectTransform pressAnyKeyText;

    [Tooltip("Your 4 menu button GameObjects (assign in Inspector)")]
    public GameObject[] buttons;

    [Tooltip("Background that appears and fades in together with the menu buttons")]
    public GameObject menuBackground;

    [Header("Movement & Timing")]
    [Tooltip("Where to end up (anchoredPosition)")]
    public Vector2 targetPosition;

    [Tooltip("Slide speed in units/sec")]
    public float slideSpeed = 200f;

    [Tooltip("How long buttons/background take to fade in")]
    public float buttonFadeDuration = 0.5f;

    [Header("Background Videos")]
    [SerializeField] RenderTextureVideoObject[] videos;
    [SerializeField] int currentVideoIndex = 0;

    [Header("Fader")]
    [SerializeField] Image faderImage;

    [Tooltip("Screen fade-out speed at start")]
    public float startFadeSpeed = 0.6f;

    [Tooltip("How long the screen takes to fade to black when starting/loading a game")]
    public float sceneFadeDuration = 0.75f;

    [Header("Video Crossfade")]
    [Tooltip("Crossfade speed between background videos")]
    public float videoCrossfadeSpeed = 2f;

    bool startFade = false;
    bool _started;
    bool _firstVideoReady = false;
    bool _finalVideoTransitionStarted;
    bool _uiTransitionStarted;
    bool _sceneFadeRunning;

    CanvasGroup _pressCG;

    CanvasGroup[] _buttonCGs;
    bool[] _initialActive;

    CanvasGroup _backgroundCG;

    Coroutine _mainMenuFadeCoroutine;

    void Awake()
    {
        // Press Any Key CanvasGroup
        _pressCG = pressAnyKeyText.GetComponent<CanvasGroup>();

        if (_pressCG == null)
            _pressCG = pressAnyKeyText.gameObject.AddComponent<CanvasGroup>();

        // Buttons
        int len = buttons != null ? buttons.Length : 0;

        _buttonCGs = new CanvasGroup[len];
        _initialActive = new bool[len];

        for (int i = 0; i < len; i++)
        {
            if (buttons[i] == null)
                continue;

            _initialActive[i] = buttons[i].activeSelf;

            CanvasGroup cg = buttons[i].GetComponent<CanvasGroup>();

            if (cg == null)
                cg = buttons[i].AddComponent<CanvasGroup>();

            _buttonCGs[i] = cg;
        }

        // Menu background
        if (menuBackground != null)
        {
            _backgroundCG = menuBackground.GetComponent<CanvasGroup>();

            if (_backgroundCG == null)
                _backgroundCG = menuBackground.AddComponent<CanvasGroup>();
        }

        // Start completely black.
        // IMPORTANT: this black stays until the first video is actually playing.
        if (faderImage != null)
        {
            faderImage.color = new Color(0f, 0f, 0f, 1f);
            faderImage.raycastTarget = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Start()
    {
        _pressCG.alpha = 1f;

        // Hide menu buttons for intro
        for (int i = 0; i < _buttonCGs.Length; i++)
        {
            CanvasGroup cg = _buttonCGs[i];

            if (cg == null)
                continue;

            cg.alpha = 0f;
            cg.gameObject.SetActive(false);
        }

        // Hide button background
        if (_backgroundCG != null)
        {
            _backgroundCG.alpha = 0f;
            menuBackground.SetActive(false);
        }

        SetupVideos();

        // First video is booted separately.
        // Startup black fade will NOT begin until this is ready.
        StartCoroutine(BootFirstVideo());
    }

    private void SetupVideos()
    {
        if (videos == null)
            return;

        for (int i = 0; i < videos.Length; i++)
        {
            VideoPlayer vp = videos[i].videoPlayer;
            RawImage rawImage = videos[i].rawImage;

            if (vp != null)
            {
                // Keep them active so Unity can prepare them without
                // activating/deactivating VideoPlayers during transitions.
                vp.gameObject.SetActive(true);

                vp.playOnAwake = false;
                vp.waitForFirstFrame = true;
                vp.skipOnDrop = true;

                vp.Stop();
            }

            if (rawImage != null)
            {
                rawImage.gameObject.SetActive(true);
                rawImage.raycastTarget = false;

                Color c = rawImage.color;
                c.a = 0f;
                rawImage.color = c;
            }
        }
    }

    private IEnumerator BootFirstVideo()
    {
        if (videos == null ||
            videos.Length == 0 ||
            videos[0].videoPlayer == null)
        {
            _firstVideoReady = true;
            startFade = true;
            yield break;
        }

        RenderTextureVideoObject firstVideo = videos[0];
        VideoPlayer vp = firstVideo.videoPlayer;

        // Fully prepare video before showing anything.
        yield return StartCoroutine(
            PrepareVideo(firstVideo)
        );

        bool firstFrameReady = false;

        VideoPlayer.FrameReadyEventHandler frameHandler =
            (source, frameIndex) =>
            {
                firstFrameReady = true;
            };

        vp.sendFrameReadyEvents = true;
        vp.frameReady += frameHandler;

        // Start video behind the black screen.
        vp.Play();

        float waitTime = 0f;

        // Wait for Unity to actually decode a frame.
        while (!firstFrameReady && waitTime < 3f)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }

        vp.frameReady -= frameHandler;
        vp.sendFrameReadyEvents = false;

        // Give the video a tiny bit of time to actually start advancing.
        // This prevents the initial decoded frame from looking frozen.
        waitTime = 0f;

        while (vp.isPlaying &&
               vp.time < 0.05 &&
               waitTime < 0.5f)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }

        // Ensure the RenderTexture has been updated.
        yield return new WaitForEndOfFrame();

        if (firstVideo.rawImage != null)
        {
            Color c = firstVideo.rawImage.color;
            c.a = 1f;
            firstVideo.rawImage.color = c;
        }

        currentVideoIndex = 0;

        _firstVideoReady = true;

        // NOW allow the black startup screen to fade away.
        startFade = true;

        // Prepare video 2 while video 1 is playing.
        if (videos.Length > 1)
        {
            StartCoroutine(
                PrepareVideo(videos[1])
            );
        }
    }

    void Update()
    {
        // Don't accept the intro input until the first background
        // video is actually ready and moving.
        if (_firstVideoReady &&
            !_started &&
            Input.anyKeyDown)
        {
            _started = true;

            if (videos != null && videos.Length > 1)
            {
                StartCoroutine(
                    TransitionToSecondVideo()
                );
            }
            else
            {
                StartUITransition();
            }
        }

        // Startup fade from black.
        // This only begins after BootFirstVideo has finished.
        if (startFade &&
            faderImage != null &&
            faderImage.color.a > 0f)
        {
            faderImage.color =
                Vector4.MoveTowards(
                    faderImage.color,
                    new Color(0f, 0f, 0f, 0f),
                    Time.unscaledDeltaTime * startFadeSpeed
                );

            if (faderImage.color.a <= 0f)
                startFade = false;
        }

        // Begin final transition slightly before video 2 finishes
        if (_started &&
            !_finalVideoTransitionStarted &&
            videos != null &&
            videos.Length > 2 &&
            videos[1].videoPlayer != null &&
            videos[1].videoPlayer.isPlaying)
        {
            VideoPlayer vp = videos[1].videoPlayer;

            if (vp.length > 0.0)
            {
                double remaining = vp.length - vp.time;

                float transitionDuration =
                    1f / Mathf.Max(0.01f, videoCrossfadeSpeed);

                if (remaining <= transitionDuration + 0.05f)
                {
                    _finalVideoTransitionStarted = true;

                    StartCoroutine(
                        TransitionToFinalVideo()
                    );
                }
            }
        }

        // Only two videos
        if (_started &&
            !_uiTransitionStarted &&
            videos != null &&
            videos.Length == 2 &&
            videos[1].videoPlayer != null)
        {
            VideoPlayer vp = videos[1].videoPlayer;

            if (vp.length > 0.0)
            {
                double remaining = vp.length - vp.time;

                if (remaining <= 0.05 ||
                    (!vp.isPlaying && vp.time > 0.1))
                {
                    StartUITransition();
                }
            }
        }
    }

    // ----------------------------------------------------
    // VIDEO
    // ----------------------------------------------------

    private IEnumerator PrepareVideo(
        RenderTextureVideoObject video)
    {
        if (video.videoPlayer == null)
            yield break;

        VideoPlayer vp = video.videoPlayer;

        vp.gameObject.SetActive(true);

        if (vp.isPrepared)
            yield break;

        vp.Prepare();

        float timeout = 0f;

        while (!vp.isPrepared)
        {
            timeout += Time.unscaledDeltaTime;

            if (timeout >= 10f)
            {
                Debug.LogWarning(
                    $"Video '{vp.name}' took too long to prepare."
                );

                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator TransitionToSecondVideo()
    {
        yield return StartCoroutine(
            TransitionVideo(
                videos[0],
                videos[1]
            )
        );

        currentVideoIndex = 1;

        if (videos.Length > 2)
        {
            StartCoroutine(
                PrepareVideo(videos[2])
            );
        }
    }

    private IEnumerator TransitionToFinalVideo()
    {
        yield return StartCoroutine(
            TransitionVideo(
                videos[1],
                videos[2]
            )
        );

        currentVideoIndex = 2;

        StartUITransition();
    }

    private IEnumerator TransitionVideo(
        RenderTextureVideoObject currentVideo,
        RenderTextureVideoObject nextVideo)
    {
        if (nextVideo.videoPlayer == null)
            yield break;

        // Video should already normally be prepared,
        // but make absolutely sure before showing it.
        yield return StartCoroutine(
            PrepareVideo(nextVideo)
        );

        if (nextVideo.rawImage != null)
        {
            nextVideo.rawImage.gameObject.SetActive(true);

            Color nextColor = nextVideo.rawImage.color;
            nextColor.a = 0f;
            nextVideo.rawImage.color = nextColor;
        }

        VideoPlayer nextPlayer = nextVideo.videoPlayer;

        bool firstFrameReady = false;

        VideoPlayer.FrameReadyEventHandler frameHandler =
            (source, frameIndex) =>
            {
                firstFrameReady = true;
            };

        nextPlayer.sendFrameReadyEvents = true;
        nextPlayer.frameReady += frameHandler;

        nextPlayer.Play();

        float frameWait = 0f;

        while (!firstFrameReady &&
               frameWait < 1f)
        {
            frameWait += Time.unscaledDeltaTime;
            yield return null;
        }

        nextPlayer.frameReady -= frameHandler;
        nextPlayer.sendFrameReadyEvents = false;

        // Let RenderTexture receive the frame
        yield return new WaitForEndOfFrame();

        // Crossfade
        float fade = 0f;

        while (fade < 1f)
        {
            fade +=
                Time.unscaledDeltaTime *
                Mathf.Max(0.01f, videoCrossfadeSpeed);

            float t = Mathf.Clamp01(fade);

            if (nextVideo.rawImage != null)
            {
                Color nextColor =
                    nextVideo.rawImage.color;

                nextColor.a = t;

                nextVideo.rawImage.color =
                    nextColor;
            }

            if (currentVideo.rawImage != null)
            {
                Color currentColor =
                    currentVideo.rawImage.color;

                currentColor.a = 1f - t;

                currentVideo.rawImage.color =
                    currentColor;
            }

            yield return null;
        }

        if (nextVideo.rawImage != null)
        {
            Color c = nextVideo.rawImage.color;
            c.a = 1f;
            nextVideo.rawImage.color = c;
        }

        if (currentVideo.rawImage != null)
        {
            Color c = currentVideo.rawImage.color;
            c.a = 0f;
            currentVideo.rawImage.color = c;
        }

        if (currentVideo.videoPlayer != null)
            currentVideo.videoPlayer.Stop();
    }

    // ----------------------------------------------------
    // MAIN MENU INTRO
    // ----------------------------------------------------

    private void StartUITransition()
    {
        if (_uiTransitionStarted)
            return;

        _uiTransitionStarted = true;

        StartCoroutine(DoTransition());
    }

    IEnumerator DoTransition()
    {
        Vector2 startPos =
            uiElement.anchoredPosition;

        float distance =
            Vector2.Distance(
                startPos,
                targetPosition
            );

        float duration =
            slideSpeed > 0f
                ? distance / slideSpeed
                : 0f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            uiElement.anchoredPosition =
                Vector2.Lerp(
                    startPos,
                    targetPosition,
                    t
                );

            _pressCG.alpha = 1f - t;

            yield return null;
        }

        uiElement.anchoredPosition =
            targetPosition;

        _pressCG.alpha = 0f;

        pressAnyKeyText.gameObject.SetActive(false);

        // Enable original buttons
        for (int i = 0; i < _buttonCGs.Length; i++)
        {
            if (_buttonCGs[i] == null)
                continue;

            if (_initialActive[i])
                _buttonCGs[i].gameObject.SetActive(true);
        }

        // Enable background
        if (_backgroundCG != null)
        {
            _backgroundCG.alpha = 0f;
            menuBackground.SetActive(true);
        }

        // Fade buttons/background together
        elapsed = 0f;

        while (elapsed < buttonFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                buttonFadeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed / buttonFadeDuration
                    );

            for (int i = 0;
                 i < _buttonCGs.Length;
                 i++)
            {
                if (_buttonCGs[i] == null)
                    continue;

                if (_initialActive[i])
                    _buttonCGs[i].alpha = t;
            }

            if (_backgroundCG != null)
                _backgroundCG.alpha = t;

            yield return null;
        }

        for (int i = 0;
             i < _buttonCGs.Length;
             i++)
        {
            if (_buttonCGs[i] == null)
                continue;

            if (_initialActive[i])
                _buttonCGs[i].alpha = 1f;
        }

        if (_backgroundCG != null)
            _backgroundCG.alpha = 1f;
    }

    // ----------------------------------------------------
    // RETURN FROM SETTINGS
    // ----------------------------------------------------

    public void HideMainMenuVisualsForSettings()
    {
        if (_mainMenuFadeCoroutine != null)
        {
            StopCoroutine(_mainMenuFadeCoroutine);
            _mainMenuFadeCoroutine = null;
        }

        // Buttons themselves are normally hidden by MenuPanel,
        // but reset alpha so there are no selected/faded leftovers.
        for (int i = 0; i < _buttonCGs.Length; i++)
        {
            if (_buttonCGs[i] == null)
                continue;

            _buttonCGs[i].alpha = 0f;
        }

        if (_backgroundCG != null)
        {
            _backgroundCG.alpha = 0f;
            menuBackground.SetActive(false);
        }
    }

    public void FadeMainMenuIn()
    {
        if (_mainMenuFadeCoroutine != null)
            StopCoroutine(_mainMenuFadeCoroutine);

        _mainMenuFadeCoroutine =
            StartCoroutine(
                FadeMainMenuInRoutine()
            );
    }

    private IEnumerator FadeMainMenuInRoutine()
    {
        // Anything that is currently meant to be visible
        // starts completely transparent.
        for (int i = 0;
             i < _buttonCGs.Length;
             i++)
        {
            if (_buttonCGs[i] == null)
                continue;

            if (_buttonCGs[i].gameObject.activeSelf)
                _buttonCGs[i].alpha = 0f;
        }

        if (_backgroundCG != null)
        {
            menuBackground.SetActive(true);
            _backgroundCG.alpha = 0f;
        }

        float elapsed = 0f;

        while (elapsed < buttonFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                buttonFadeDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        buttonFadeDuration
                    );

            for (int i = 0;
                 i < _buttonCGs.Length;
                 i++)
            {
                if (_buttonCGs[i] == null)
                    continue;

                if (_buttonCGs[i].gameObject.activeSelf)
                    _buttonCGs[i].alpha = t;
            }

            if (_backgroundCG != null)
                _backgroundCG.alpha = t;

            yield return null;
        }

        for (int i = 0;
             i < _buttonCGs.Length;
             i++)
        {
            if (_buttonCGs[i] == null)
                continue;

            if (_buttonCGs[i].gameObject.activeSelf)
                _buttonCGs[i].alpha = 1f;
        }

        if (_backgroundCG != null)
            _backgroundCG.alpha = 1f;

        _mainMenuFadeCoroutine = null;
    }

    // ----------------------------------------------------
    // FADE TO GAME
    // ----------------------------------------------------

    public void MovingToNewScene()
    {
        MovingToNewScene(null);
    }

    public void MovingToNewScene(
        Action onFadeComplete)
    {
        if (_sceneFadeRunning)
            return;

        StartCoroutine(
            FadeToBlack(onFadeComplete)
        );
    }

    private IEnumerator FadeToBlack(
        Action onFadeComplete)
    {
        _sceneFadeRunning = true;
        startFade = false;

        if (faderImage == null)
        {
            onFadeComplete?.Invoke();
            _sceneFadeRunning = false;
            yield break;
        }

        faderImage.gameObject.SetActive(true);
        faderImage.transform.SetAsLastSibling();
        faderImage.raycastTarget = true;

        Color color = faderImage.color;

        float startAlpha = color.a;
        float elapsed = 0f;

        if (sceneFadeDuration <= 0f)
        {
            color.a = 1f;
            faderImage.color = color;
        }
        else
        {
            while (elapsed < sceneFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        sceneFadeDuration
                    );

                color.a =
                    Mathf.Lerp(
                        startAlpha,
                        1f,
                        t
                    );

                faderImage.color = color;

                yield return null;
            }
        }

        color.a = 1f;
        faderImage.color = color;

        onFadeComplete?.Invoke();

        _sceneFadeRunning = false;
    }

    public void FadeBackFromBlack()
    {
        if (faderImage == null)
            return;

        StartCoroutine(
            FadeBackFromBlackRoutine()
        );
    }

    private IEnumerator FadeBackFromBlackRoutine()
    {
        Color color = faderImage.color;

        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < sceneFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    sceneFadeDuration
                );

            color.a =
                Mathf.Lerp(
                    startAlpha,
                    0f,
                    t
                );

            faderImage.color = color;

            yield return null;
        }

        color.a = 0f;
        faderImage.color = color;

        faderImage.raycastTarget = false;

        _sceneFadeRunning = false;
    }

    [Serializable]
    public struct RenderTextureVideoObject
    {
        public VideoPlayer videoPlayer;
        public RawImage rawImage;
    }
}