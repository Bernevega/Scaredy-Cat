using UnityEngine;

[CreateAssetMenu(fileName = "NPCVoiceScriptable", menuName = "Scriptable Objects/NPCVoiceScriptable")]
public class NPCVoiceScriptable : ScriptableObject
{
    [SerializeField] AudioClip[] voiceClips;
    [SerializeField] float pitch = 1f;
    [SerializeField] float vol = 1f;

    public AudioClip GetVoiceClip(int index)
    {
        if (index < 0 || index >= voiceClips.Length)
            return null;
        return voiceClips[index];
    }

    public PlaySoundInfo GetRandomVoiceClip()
    {
        PlaySoundInfo soundInfo = new PlaySoundInfo();

        soundInfo.clip = voiceClips[Random.Range(0, voiceClips.Length)];
        soundInfo.pitch = pitch;
        soundInfo.volume = vol;

        return soundInfo;
    }
}
