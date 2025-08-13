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
    [SerializeField] Image faderImage;

    bool startFade = true;
    bool _started;
    CanvasGroup _pressCG;
    CanvasGroup[] _buttonCGs;

    void Awake()
    {
        // Ensure the PressAnyKey text has a CanvasGroup for fading
        _pressCG = pressAnyKeyText.GetComponent<CanvasGroup>();
        if (_pressCG == null)
            _pressCG = pressAnyKeyText.gameObject.AddComponent<CanvasGroup>();

        // Ensure each button has a CanvasGroup for fading
        _buttonCGs = new CanvasGroup[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            _buttonCGs[i] = buttons[i].GetComponent<CanvasGroup>();
            if (_buttonCGs[i] == null)
                _buttonCGs[i] = buttons[i].AddComponent<CanvasGroup>();
        }

        faderImage.color = new Color(0, 0, 0, 1);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Start()
    {
        // “Press any key” visible; buttons hidden
        _pressCG.alpha = 1f;
        foreach (var cg in _buttonCGs)
        {
            cg.alpha = 0f;
            cg.gameObject.SetActive(false);
            videos[0].videoPlayer.Play();
        }

        for (int i = 0; i < videos.Length; i++)
        {
            videos[i].videoPlayer.gameObject.SetActive(false);
        }
        videos[0].videoPlayer.gameObject.SetActive(true);
        videos[1].videoPlayer.loopPointReached += VideoTransitionFinished;
    }

    void Update()
    {
        if (startFade && faderImage.color.a > 0)
        {
            faderImage.color = Vector4.MoveTowards(faderImage.color, new Color(0, 0, 0, 0), Time.deltaTime * 0.25f);
            if (faderImage.color.a <= 0)
            {
                startFade = false;
            }
        }
        else if (!_started && Input.anyKeyDown)
        {
            _started = true;
            StartCoroutine(TransitionVideo(videos[0], videos[1]));
        }
    }
    public void MovingToNewScene()
    {

    }
    private void VideoTransitionFinished(VideoPlayer vp)
    {
        StartCoroutine(TransitionVideo(videos[1], videos[2]));
        StartCoroutine(DoTransition()); // UI transition.
    }

    IEnumerator TransitionVideo(RenderTextureVideoObject currentVideo, RenderTextureVideoObject nextVideo)
    {
        nextVideo.rawImage.color = new Vector4(1, 1, 1, 0);
        nextVideo.rawImage.gameObject.SetActive(true);
        nextVideo.videoPlayer.Pause();

        if (nextVideo.videoPlayer != currentVideo.videoPlayer)
        {
            while (nextVideo.rawImage.color.a < 1)
            {
                nextVideo.rawImage.color =
                    Vector4.MoveTowards(
                        nextVideo.rawImage.color,
                        new Color(1, 1, 1, 1),
                        Time.fixedDeltaTime * 1f
                        );

                yield return new WaitForFixedUpdate();
            }
        }

        nextVideo.videoPlayer.Play();
        currentVideo.rawImage.color = new Vector4(1, 1, 1, 0);
        currentVideo.rawImage.gameObject.SetActive(false);

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

        // 2) Fade in buttons
        foreach (var cg in _buttonCGs)
            cg.gameObject.SetActive(true);

        elapsed = 0f;
        while (elapsed < buttonFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / buttonFadeDuration);
            foreach (var cg in _buttonCGs)
                cg.alpha = t;
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
