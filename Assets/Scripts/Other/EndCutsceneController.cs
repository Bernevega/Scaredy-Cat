using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndCutsceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TMP_Text endText;
    [SerializeField] Image fadeImage;                 // full-screen black image

    [Header("Music")]
    [SerializeField] AudioSource endMusicSource;

    [Header("Credits (no CanvasGroup)")]
    [Tooltip("Root GameObject of your credits UI (will be SetActive(true) when needed).")]
    [SerializeField] GameObject creditsRoot;
    [Tooltip("Seconds to keep credits fully visible.")]
    [SerializeField] float creditsShowDuration = 10f;
    [Tooltip("Alpha change per second when fading credits in/out.")]
    [SerializeField] float creditsFadeSpeed = 1f;

    [Header("Scene Transition")]
    [SerializeField] string nextSceneName = "MainMenu";

    [Header("Timings")]
    [Tooltip("Alpha per second when fading the end text in.")]
    [SerializeField] float endTextFadeSpeed = 0.5f;
    [Tooltip("Alpha per second when fading the screen to black.")]
    [SerializeField] float screenFadeSpeed = 1f;
    [Tooltip("Seconds to hold end text fully visible before starting screen fade.")]
    [SerializeField] float endTextHoldSeconds = 3f;

    private State state = State.CUTSCENEPLAYING;
    private float _timer;

    // cache of all graphics under creditsRoot for manual alpha fades
    private Graphic[] _creditsGraphics;

    private enum State
    {
        CUTSCENEPLAYING,
        END_FADEIN,
        FADEOUT_TO_BLACK,
        CREDITS_FADEIN,
        CREDITS_SHOW,
        CREDITS_FADEOUT,
        NEXTSCENE
    }

    void Awake()
    {
        // init end text alpha
        if (endText != null)
        {
            var c = endText.color;
            c.a = 0f;
            endText.color = c;
        }

        // init screen fade image alpha
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        // prepare credits root
        if (creditsRoot != null)
        {
            _creditsGraphics = creditsRoot.GetComponentsInChildren<Graphic>(true);
            SetGraphicsAlpha(_creditsGraphics, 0f);
            creditsRoot.SetActive(false);
        }
    }

    void Update()
    {
        switch (state)
        {
            case State.END_FADEIN:         State_End_FadeIn();         break;
            case State.FADEOUT_TO_BLACK:   State_FadeOutToBlack();      break;
            case State.CREDITS_FADEIN:     State_Credits_FadeIn();      break;
            case State.CREDITS_SHOW:       State_Credits_Show();        break;
            case State.CREDITS_FADEOUT:    State_Credits_FadeOut();     break;
            case State.NEXTSCENE:          State_NextScene();           break;
        }
    }

    // call this from your cutscene (animation event etc.)
    public void OnCutsceneFinish()
    {
        state = State.END_FADEIN;
        _timer = 0f;
        if (endMusicSource != null) endMusicSource.Play();
    }

    private void State_End_FadeIn()
    {
        if (endText == null)
        {
            state = State.FADEOUT_TO_BLACK;
            return;
        }

        // fade end text to 1
        var c = endText.color;
        if (c.a < 1f)
        {
            c.a = Mathf.MoveTowards(c.a, 1f, endTextFadeSpeed * Time.deltaTime);
            endText.color = c;
            return;
        }

        // hold for a bit then start screen fade
        _timer += Time.deltaTime;
        if (_timer >= endTextHoldSeconds)
        {
            state = State.FADEOUT_TO_BLACK;
        }
    }

    private void State_FadeOutToBlack()
    {
        if (fadeImage == null)
        {
            PrepareCredits();
            state = State.CREDITS_FADEIN;
            return;
        }

        var c = fadeImage.color;
        if (c.a < 1f)
        {
            c.a = Mathf.MoveTowards(c.a, 1f, screenFadeSpeed * Time.deltaTime);
            fadeImage.color = c;
            return;
        }

        // fully black now; stay black and move on to credits
        PrepareCredits();
        state = State.CREDITS_FADEIN;
    }

    private void PrepareCredits()
    {
        if (creditsRoot != null)
        {
            creditsRoot.SetActive(true);
            if (_creditsGraphics == null || _creditsGraphics.Length == 0)
                _creditsGraphics = creditsRoot.GetComponentsInChildren<Graphic>(true);

            SetGraphicsAlpha(_creditsGraphics, 0f); // start invisible
        }
        _timer = 0f;
    }

    private void State_Credits_FadeIn()
    {
        if (_creditsGraphics == null || _creditsGraphics.Length == 0)
        {
            // no credits provided; proceed to next scene
            state = State.NEXTSCENE;
            return;
        }

        bool done = MoveGraphicsAlphaTowards(_creditsGraphics, 1f, creditsFadeSpeed * Time.deltaTime);
        if (done)
        {
            state = State.CREDITS_SHOW;
            _timer = 0f;
        }
    }

    private void State_Credits_Show()
    {
        _timer += Time.deltaTime;
        if (_timer >= creditsShowDuration)
            state = State.CREDITS_FADEOUT;
    }

    private void State_Credits_FadeOut()
    {
        if (_creditsGraphics == null || _creditsGraphics.Length == 0)
        {
            state = State.NEXTSCENE;
            return;
        }

        bool done = MoveGraphicsAlphaTowards(_creditsGraphics, 0f, creditsFadeSpeed * Time.deltaTime);
        if (done)
        {
            // credits hidden; screen still black -> go to next scene
            state = State.NEXTSCENE;
        }
    }

    private void State_NextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    // ---------- helpers for fading Graphics without CanvasGroup ----------

    private static void SetGraphicsAlpha(Graphic[] graphics, float alpha)
    {
        if (graphics == null) return;
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] == null) continue;
            var c = graphics[i].color;
            c.a = alpha;
            graphics[i].color = c;
        }
    }

    /// <summary>
    /// Moves all graphics' alpha toward target. Returns true when all have reached it.
    /// </summary>
    private static bool MoveGraphicsAlphaTowards(Graphic[] graphics, float target, float delta)
    {
        if (graphics == null) return true;

        bool allDone = true;
        for (int i = 0; i < graphics.Length; i++)
        {
            var g = graphics[i];
            if (g == null) continue;

            var c = g.color;
            float newA = Mathf.MoveTowards(c.a, target, delta);
            if (!Mathf.Approximately(newA, target))
                allDone = false;

            if (!Mathf.Approximately(newA, c.a))
            {
                c.a = newA;
                g.color = c;
            }
        }
        return allDone;
    }
}
