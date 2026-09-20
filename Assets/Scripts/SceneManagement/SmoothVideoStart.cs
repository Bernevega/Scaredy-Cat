using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class SmoothVideoStart : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage rawImage;

    private void Awake()
    {
        // Hide the video while Unity prepares it
        rawImage.enabled = false;

        videoPlayer.playOnAwake = false;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        // Video is ready, so show it and immediately play
        rawImage.enabled = true;
        source.Play();
    }

    private void OnDestroy()
    {
        videoPlayer.prepareCompleted -= OnVideoPrepared;
    }
}