using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class SplashScreen : MonoBehaviour
{
    [SerializeField] VideoPlayer video;
    [SerializeField] string nextScene;

    private void Awake()
    {
        video.loopPointReached += OnFinished;
    }

    private void OnFinished(VideoPlayer vp)
    {
        SceneManager.LoadScene(nextScene);
    }
}
