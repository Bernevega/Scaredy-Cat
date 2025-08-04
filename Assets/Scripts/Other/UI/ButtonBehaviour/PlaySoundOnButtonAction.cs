using UnityEngine;
using UnityEngine.Audio;

public class PlaySoundOnButtonAction : MonoBehaviour
{
    [SerializeField] UIButton button;
    [SerializeField] AudioMixerGroup mix;

    [Space(10)]
    [SerializeField] AudioClip mouseDownSound;
    [SerializeField] float mouseDownVolume = 1;
    [SerializeField] float mouseDownPitch = 1;

    [Space(10)]
    [SerializeField] AudioClip mouseUpSound;
    [SerializeField] float mouseUpVolume = 1;
    [SerializeField] float mouseUpPitch = 1;

    [Space(10)]
    [SerializeField] AudioClip hoverEnterSound;
    [SerializeField] float hoverEnterVolume = 1;
    [SerializeField] float hoverEnterPitch = 1;

    [Space(10)]
    [SerializeField] AudioClip hoverExitSound;
    [SerializeField] float hoverExitVolume = 1;
    [SerializeField] float hoverExitPitch = 1;

    private void Awake()
    {
        button.EventButtonAction += OnButtonAction;
    }

    private void OnDestroy()
    {
        button.EventButtonAction -= OnButtonAction;
    }

    private void OnButtonAction(UIButton b, UIButtonAction action)
    {
        AudioClip clipToPlay = null;
        float pitch = 1;
        float vol = 1;

        switch (action)
        {
            case UIButtonAction.MouseDown:
                clipToPlay = mouseDownSound;
                vol = mouseDownVolume;
                pitch = mouseDownPitch;
                break;
            case UIButtonAction.MouseUp:
                clipToPlay = mouseUpSound;
                vol = mouseUpVolume;
                pitch = mouseUpPitch;
                break;
            case UIButtonAction.HoverEnter:
                clipToPlay = hoverEnterSound;
                vol = hoverEnterVolume;
                pitch = hoverEnterPitch;
                break;
            case UIButtonAction.HoverExit:
                clipToPlay = hoverExitSound;
                vol = hoverExitVolume;
                pitch = hoverExitPitch;
                break;
        }

        if (clipToPlay == null) return;

        GameObject soundPlayer2DObj = ObjectPool.instance.objPool_GetObject("2DSoundPlayer");
        if (soundPlayer2DObj == null) return;

        SoundPlayer soundPlayer2D = soundPlayer2DObj.GetComponent<SoundPlayer>();

        PlaySoundInfo soundInfo = new PlaySoundInfo();
        soundInfo.clip = clipToPlay;
        soundInfo.pitch = pitch;
        soundInfo.volume = vol;
        soundInfo.mixer = mix;

        soundPlayer2D.PlaySound(soundInfo);
    }
}
