using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndCutsceneController : MonoBehaviour
{
    [SerializeField] TMP_Text endText;
    [SerializeField] Image fadeImage;
    [SerializeField] AudioSource endMusicSource;

    State state = State.CUTSCENEPLAYING;

    enum State
    {
        CUTSCENEPLAYING,
        END_FADEIN,
        FADEOUT_WAIT,
        FADEOUT,
        NEXTSCENE,
    }

    private void Update()
    {
        switch(state)
        {
            case State.END_FADEIN:
                StateEnd_FadeIn(); 
                break;
            case State.FADEOUT:
                StateFadeOut();
                break;
        }
    }

    public void StateEnd_FadeIn()
    {
        if (endText.color.a < 1)
        {
            endText.color = Vector4.MoveTowards(
                endText.color,
                new Vector4(endText.color.r, endText.color.g, endText.color.b, 1),
                Time.deltaTime * 0.5f);
        }
        else
        {
            state = State.FADEOUT_WAIT;
            Invoke("StartFadeOut", 3f);
        }    
    }
    public void StateFadeOut()
    {
        if (fadeImage.color.a < 1)
        {
            fadeImage.color = Vector4.MoveTowards(fadeImage.color, new Vector4(0, 0, 0, 1), Time.deltaTime);
        }
        else
        {
            state = State.NEXTSCENE;
            SceneManager.LoadScene("MainMenu");
        }
    }
    private void StartFadeOut()
    {
        state = State.FADEOUT;
    }
    public void OnCutsceneFinish()
    {
        state = State.END_FADEIN;
        endMusicSource.Play();
    }
}
