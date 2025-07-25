using UnityEngine;
using UnityEngine.Audio;

public class SoundPlayer : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    public bool isPlaying = false;

    private void Update()
    {
        if (isPlaying &&
            !audioSource.isPlaying)
        {
            isPlaying = false;
            OnStopPlaying();
        }
    }

    private void OnStopPlaying()
    {
        gameObject.SetActive(false);
    }

    public void PlaySound(PlaySoundInfo soundInfo)
    {
        audioSource.clip = soundInfo.clip;
        audioSource.outputAudioMixerGroup = soundInfo.mixer;
        audioSource.volume = soundInfo.volume;
        audioSource.pitch = soundInfo.pitch;
        audioSource.loop = soundInfo.loop;

        audioSource.Play();
        isPlaying = true;
    }
}

public struct PlaySoundInfo
{
    public AudioClip clip;
    public AudioMixerGroup mixer;
    public float volume;
    public float pitch;
    public bool loop;

    public PlaySoundInfo(AudioClip clip = null)
    {
        this.clip = clip;
        mixer = null;
        volume = 1f;
        pitch = 1f;
        loop = false;
    }
}