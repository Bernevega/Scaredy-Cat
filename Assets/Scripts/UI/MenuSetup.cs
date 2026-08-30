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

    [Header("Movement & Timing")]
    [Tooltip("Where to end up (anchoredPosition)")]
    public Vector2 targetPosition;
    [Tooltip("Slide speed in units/sec")]
    public float slideSpeed = 200f;
    [Tooltip("How long buttons take to fade in (sec)")]
    public float buttonFadeDuration = 0.5f;

    [Header("Background Videos")]
    [SerializeField] RenderTextureVideoObject[] videos;
    [SerializeField] int currentVideoIndex = 0;

    [Header("Fader")]
    [SerializeField] Image faderImage;
    [Tooltip("Screen fade-out speed at start (higher = faster)")]
    public float startFadeSpeed = 0.6f; // was 0.25

    [Header("Video Crossfade")]
    [Tooltip("Crossfade speed between background videos (higher = faster)")]
    public float videoCrossfadeSpeed = 6f; // increase for faster transitions

    bool startFade = true;
    bool _started;
    CanvasGroup _pressCG;

    CanvasGroup[] _buttonCGs;
    bool[] _initialActive; // NEW: remember which buttons were active initially

    void Awake()
    {
        // Ensure the PressAnyKey text has a CanvasGroup for fading
        _pressCG = pressAnyKeyText.GetComponent<CanvasGroup>();
        if (_pressCG == null)
            _pressCG = pressAnyKeyText.gameObject.AddComponent<CanvasGroup>();

        // Prepare arrays
        int len = buttons != null ? buttons.Length : 0;
        _buttonCGs = new CanvasGroup[len];
        _initialActive = new bool[len];

        // Ensure each button has a CanvasGroup and record initial active state
        for (int i = 0; i < len; i++)
        {
            if (buttons[i] == null) continue;

            // IMPORTANT: snapshot initial active state BEFORE we hide anything.
            _initialActive[i] = buttons[i].activeSelf;

            var cg = buttons[i].GetComponent<CanvasGroup>();
            if (cg == null) cg = buttons[i].AddComponent<CanvasGroup>();
            _buttonCGs[i] = cg;
        }

        if (faderImage) faderImage.color = new Color(0, 0, 0, 1);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Start()
    {
        // “Press any key” visible; buttons hidden
        _pressCG.alpha = 1f;

        // Hide ALL buttons for the intro; we will only re-enable those that were initially active.
        for (int i = 0; i < _buttonCGs.Length; i++)
        {
            var cg = _buttonCGs[i];
            if (cg == null) continue;

            cg.alpha = 0f;
            cg.gameObject.SetActive(false);
        }

        // Video boot
        if (videos != null && videos.Length > 0 && videos[0].videoPlayer)
            videos[0].videoPlayer.Play();

        // Make only first video object active
        for (int i = 0; i < videos.Length; i++)
            if (videos[i].videoPlayer)
                videos[i].videoPlayer.gameObject.SetActive(i == 0);

        if (videos.Length > 1 && videos[1].videoPlayer)
            videos[1].videoPlayer.loopPointReached += VideoTransitionFinished;
    }

    void Update()
    {
        // accept input anytime
        if (!_started && Input.anyKeyDown)
        {
            _started = true;
            if (videos.Length > 1)
                StartCoroutine(TransitionVideo(videos[0], videos[1]));

            // Optionally start UI transition immediately:
            // StartCoroutine(DoTransition());
        }

        // keep running the fade independently
        if (startFade && faderImage && faderImage.color.a > 0f)
        {
            faderImage.color = Vector4.MoveTowards(
                faderImage.color,
                new Color(0, 0, 0, 0),
                Time.deltaTime * startFadeSpeed
            );
            if (faderImage.color.a <= 0f) startFade = false;
        }
    }

    public void MovingToNewScene() { }

    private void VideoTransitionFinished(VideoPlayer vp)
    {
        if (videos.Length > 2)
            StartCoroutine(TransitionVideo(videos[1], videos[2]));

        StartCoroutine(DoTransition()); // UI transition.
    }

    IEnumerator TransitionVideo(RenderTextureVideoObject currentVideo, RenderTextureVideoObject nextVideo)
    {
        if (nextVideo.rawImage)
        {
            var c = nextVideo.rawImage.color;
            c.a = 0f;
            nextVideo.rawImage.color = c;
            nextVideo.rawImage.gameObject.SetActive(true);
        }

        nextVideo.videoPlayer.Pause();

        if (nextVideo.videoPlayer != currentVideo.videoPlayer)
        {
            if (!nextVideo.videoPlayer.gameObject.activeSelf)
                nextVideo.videoPlayer.gameObject.SetActive(true);

            while (nextVideo.rawImage && nextVideo.rawImage.color.a < 0.999f)
            {
                float step = Time.deltaTime * Mathf.Max(0.01f, videoCrossfadeSpeed);
                var col = nextVideo.rawImage.color;
                col.a = Mathf.Min(1f, col.a + step);
                nextVideo.rawImage.color = col;
                yield return null;
            }
        }

        nextVideo.videoPlayer.Play();

        if (currentVideo.rawImage)
        {
            var curCol = currentVideo.rawImage.color;
            curCol.a = 0f;
            currentVideo.rawImage.color = curCol;
            currentVideo.rawImage.gameObject.SetActive(false);
        }

        yield return null;
    }

    IEnumerator DoTransition()
    {
        // 1) Slide panel & fade out “Press any key”
        Vector2 startPos = uiElement.anchoredPosition;
        float distance = Vector2.Distance(startPos, targetPosition);
        float duration = distance / slideSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            uiElement.anchoredPosition = Vector2.Lerp(startPos, targetPosition, t);
            _pressCG.alpha = 1f - t;
            yield return null;
        }
        // Finalize
        uiElement.anchoredPosition = targetPosition;
        _pressCG.alpha = 0f;
        pressAnyKeyText.gameObject.SetActive(false);

        // 2) Fade in ONLY the buttons that were initially active (e.g., Continue stays hidden if no save).
        for (int i = 0; i < _buttonCGs.Length; i++)
        {
            if (_buttonCGs[i] == null) continue;
            if (_initialActive[i]) _buttonCGs[i].gameObject.SetActive(true);
        }

        elapsed = 0f;
        while (elapsed < buttonFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / buttonFadeDuration);
            for (int i = 0; i < _buttonCGs.Length; i++)
            {
                if (_buttonCGs[i] == null) continue;
                if (_initialActive[i]) _buttonCGs[i].alpha = t; // only fade the ones meant to be visible
            }
            yield return null;
        }
    }

    [Serializable]
    public struct RenderTextureVideoObject
    {
        public VideoPlayer videoPlayer;
        public RawImage rawImage;
    }
}
