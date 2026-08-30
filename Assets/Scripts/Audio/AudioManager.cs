using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    
    void Awake()
    {
        // Scene-based singleton (no DontDestroyOnLoad)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        LoadVolumeSettings();
    }

    // -------- Volume Setters --------

    public void SetMasterVolume(Slider slider)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(slider.value) * 20);
        PlayerPrefs.SetFloat("MasterVolume", slider.value);
        Debug.Log("Set the master volume to : " + slider.value);
    }

    public void SetMusicVolume(Slider slider)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(slider.value) * 20);
        PlayerPrefs.SetFloat("MusicVolume", slider.value);
    }

    public void SetSFXVolume(Slider slider)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(slider.value) * 20);
        PlayerPrefs.SetFloat("SFXVolume", slider.value);
    }

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MasterVolume", volume);
        Debug.Log("Set the master volume to : " + volume);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    public void LoadVolumeSettings()
    {
        float master = PlayerPrefs.GetFloat("MasterVolume", 0.75f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        audioMixer.SetFloat("MasterVolume", Mathf.Log10(master) * 20);
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(music) * 20);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfx) * 20);
    }

    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}
