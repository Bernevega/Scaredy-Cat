using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class SplashScreen : MonoBehaviour
{
    [SerializeField] VideoPlayer video;
    [SerializeField] string nextScene;
    [SerializeField] Image faderImage;

    bool fadingOut;

    private void Awake()
    {
        video.loopPointReached += OnFinished;
    }

    private void Update()
    {
        if (fadingOut && faderImage.color.a < 1f)
        {
            faderImage.color = Vector4.MoveTowards(faderImage.color, new Color(0, 0, 0, 1), Time.deltaTime * 0.5f);
            if (faderImage.color.a >= 1f)
            {
                SceneManager.LoadScene(nextScene);
            }
        }
    }

    private void OnFinished(VideoPlayer vp)
    {
        fadingOut = true;
    }
}
